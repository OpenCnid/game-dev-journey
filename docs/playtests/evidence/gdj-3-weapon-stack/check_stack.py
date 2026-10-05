"""Ordinary two-stage stacking and frozen reference checks, on the owned background desktop.
Game-owned input/state only. This establishes mechanics, not fun or native FPS.
"""
import argparse
import csv
import json
import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[4] / 'tools'))
from background_desktop import OwnedDesktop, read_shared

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--player', type=Path, required=True)
p.add_argument('--results', type=Path, required=True)
p.add_argument('--solo', action='store_true')
p.add_argument('--reference', choices=['rifle', 'all', 'scatter', 'pierce'])
p.add_argument('--port', default='17833')
p.add_argument('--runs', type=int, choices=[1, 2], default=2)
a = p.parse_args()
a.player, a.results = a.player.resolve(), a.results.resolve()
if not a.player.is_file():
    p.error('Player executable does not exist')
a.results.mkdir(parents=True, exist_ok=False)
roles = ['solo'] if a.solo else ['host', 'client']
desktop = OwnedDesktop()
sequences = [0] * len(roles)
history, pauses, comparisons = [], [], []


def read(path):
    try:
        return json.loads(read_shared(path).decode('utf-8-sig'))
    except (OSError, ValueError):
        return None


def states():
    return [read(a.results / role / 'state.json') for role in roles]


def wait(predicate, seconds=15):
    end = time.monotonic() + seconds
    while time.monotonic() < end:
        desktop.verify_input()
        values = states()
        if all(values):
            key = lambda v: (v['run'], v['tick'], v['controlRevision'], v['state'])
            if not history or key(values[0]) != key(history[-1][0]):
                history.append(values)
            if any(v['state'] == 'Aborted' for v in values):
                raise RuntimeError('Unexpected abort: ' + str([(roles[i], v['run'], v['tick'], v.get('exitReason'))
                                                               for i, v in enumerate(values)]))
            if predicate(values):
                return values
        if any(desktop.kernel.WaitForSingleObject(c.process, 0) == 0 for c in desktop.children):
            raise RuntimeError('An owned player exited')
        time.sleep(.05)
    raise TimeoutError(str([None if v is None else {k: v.get(k) for k in ('run', 'tick', 'state', 'level', 'exitReason')}
                            for v in states()]))


def command(i, action, state, **fields):
    sequences[i] += 1
    item = dict(sequence=sequences[i], action=action, run=state['run'], life=state['life'], **fields)
    folder = a.results / roles[i]
    temporary = folder / 'command-next.json'
    temporary.write_text(json.dumps(item), encoding='utf-8')
    temporary.replace(folder / 'command.json')


def frozen(before, seconds=2):
    physical = lambda v: (v['tick'], v['elapsed'], v['enemies'],
                         [(x['Position'], x['Health'], x['Life'], x['Shots'], x['Hits'], x['Damage'], x['Kills'],
                           x['ReturnAt'], x['ImmuneUntil'], x['ContactUntil']) for x in v['players']])
    end = time.monotonic() + seconds
    while time.monotonic() < end:
        values = wait(lambda s: all(v['state'] == 'Choosing' for v in s))
        assert all(physical(v) == physical(before[i]) for i, v in enumerate(values)), 'Physical state advanced while choosing'
        time.sleep(.1)


def choose(stage, upgrades):
    earned = wait(lambda s: all(v['state'] == 'Choosing' and v['level'] == stage for v in s), 70)
    frozen(earned)
    command(0, 'choice', earned[0], upgrade=1)
    wait(lambda s: s[0]['lastSequence'] == sequences[0])
    assert states()[0]['commandResult'] == 'No valid pending choice'
    command(0, 'continue', earned[0], phase=earned[0]['choicePhase'])
    wait(lambda s: s[0]['lastSequence'] == sequences[0])
    assert not states()[0]['players'][0]['ChoiceReady']
    for i, upgrade in enumerate(upgrades[:len(roles)]):
        command(i, 'choice', earned[i], upgrade=upgrade)
    selected = wait(lambda s: all(all(v['players'][i]['Offer']['Selected'] == upgrade
                          for i, upgrade in enumerate(upgrades[:len(roles)])) for v in s))
    assert all(v['tick'] == earned[0]['tick'] for v in selected)
    command(0, 'continue', selected[0], phase=selected[0]['choicePhase'] + 1)
    wait(lambda s: s[0]['lastSequence'] == sequences[0])
    assert not states()[0]['players'][0]['ChoiceReady']
    command(0, 'continue', selected[0], phase=selected[0]['choicePhase'])
    if not a.solo:
        ready = wait(lambda s: all(v['players'][0]['ChoiceReady'] for v in s))
        frozen(ready, 1)
        command(1, 'continue', ready[1], phase=ready[1]['choicePhase'])
    resumed = wait(lambda s: all(v['state'] == 'Running' for v in s))
    pauses.append(dict(earned=earned, selected=selected, resumed=resumed))
    print(f'Run {earned[0]["run"]}, level {stage}: frozen explicit choices {upgrades[:len(roles)]} pass', flush=True)


def audit(run, terminal):
    logs, outcomes, summaries = [], [], []
    for role in roles:
        summary = read(a.results / role / f'summary-run-{run}.json')
        assert summary and summary['catchupLimits'] == 0
        duration = 3600 if a.reference else 7200
        assert summary['state'] in (2, 3) and 0 < summary['ticks'] <= duration
        if summary['state'] == 2:
            assert summary['ticks'] == duration
        else:
            assert all(player['Health'] == 0 for player in summary['players'][:len(roles)]), 'Defeat requires an actual roster wipe'
        summaries.append(summary)
        events = [json.loads(line) for line in (a.results / role / 'growth.jsonl').read_text().splitlines()]
        events = [e for e in events if e['Run'] == run]
        logs.append(events)
        ledger = {}
        deaths = set()
        for e in events:
            orb = e['Orb']
            if e['Kind'] == 0:
                identity = (orb['Enemy'], orb['Incarnation'])
                assert identity not in deaths and orb['Value'] == 1
                deaths.add(identity)
                ledger[orb['Id']] = orb
            elif e['Kind'] == 1:
                assert ledger.pop(orb['Id'])['Value'] == orb['Value']
            elif e['Kind'] == 4:
                assert ledger[orb['Id']]['Value'] == orb['Value']
                ledger[orb['Id']] = orb
            elif e['Kind'] == 5:
                consumed = ledger.pop(e['ConsumedOrb'])
                assert ledger[orb['Id']]['Value'] + consumed['Value'] == orb['Value']
                ledger[orb['Id']] = orb
        assert len(deaths) == summary['xp'] + summary['uncollectedXp']
        assert sum(o['Value'] for o in ledger.values()) == summary['uncollectedXp']
        assert len(ledger) == summary['uncollectedOrbs']
        assert len([e for e in events if e['Kind'] == 2]) == (0 if a.reference else len(roles) * 2)
        assert len([e for e in events if e['Kind'] == 3]) == (0 if a.reference else len(roles) * 2)
        assert summary['level'] == (1 if a.reference else 3)
        with (a.results / role / 'events.csv').open(newline='') as f:
            rows = [{k: v for k, v in row.items() if k not in ('wall_seconds', 'confirmed_delay_ms')}
                    for row in csv.DictReader(f) if int(row['run']) == run and row['decision'] != 'Terminal']
        outcomes.append(rows)
        for player in range(len(roles)):
            work = [w for w in summary['weaponWork'] if w['player'] == player]
            for stat, counter in [('triggers', 'Shots'), ('hits', 'Hits'), ('damage', 'Damage'), ('kills', 'Kills')]:
                assert sum(w[stat] for w in work) == summary['players'][player][counter], (stat, work, summary['players'][player])
        for work in summary['weaponWork']:
            weapon_rows = [r for r in rows if int(r['weapon']) == work['weapon']]
            triggers = [r for r in weapon_rows if r['trigger'] == 'True']
            assert len(triggers) == work['triggers']
            assert sum(int(r['rays']) for r in weapon_rows) == work['rays']
            assert sum(int(r['target_tests']) for r in weapon_rows) == work['targetTests'] == work['rays'] * 35
        piercing = {}
        for row in rows:
            if int(row['weapon']) in (5, 6) and row['decision'] == 'Damage':
                key = (row['player'], row['event'])
                target = (row['enemy'], row['incarnation'])
                assert target not in piercing.setdefault(key, set()), 'Piercer struck one incarnation twice'
                piercing[key].add(target)
    if len(roles) == 2:
        assert logs[0] == logs[1], 'Growth event replica differs'
        assert outcomes[0] == outcomes[1], 'Nonterminal weapon outcomes differ'
        for field in ['players', 'enemyStates', 'xp', 'level', 'choicePhase', 'controlRevision']:
            assert summaries[0][field] == summaries[1][field], field
    comparison = dict(run=run, terminal=summaries[0], growthEvents=len(logs[0]), outcomeEvents=len(outcomes[0]),
                      levelSeconds=[e['Tick'] / 60 for e in logs[0] if e['Kind'] == 2 and e['Player'] == 0])
    comparisons.append(comparison)
    print(f'Run {run}: valid {"completion" if summaries[0]["state"] == 2 else "defeat"} at {summaries[0]["ticks"] / 60:.3f}s, conserved XP and attributed weapon work pass', flush=True)


try:
    for i, role in enumerate(roles):
        folder = a.results / role
        folder.mkdir()
        argv = [str(a.player), '-force-d3d11', '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720',
                '-port', a.port, '-sharedScripted', '-sharedPreview', '-sharedMute', '-sharedResults', str(folder), '-logFile', str(folder / 'player.log')]
        scenario = ['-scenario', 'stack-reference', '-stackLoadout', a.reference] if a.reference else ['-scenario', 'weapon-stack']
        argv += ['-playtest'] + scenario if a.solo else (['-host', '-sharedLoopback'] + scenario if i == 0 else ['-join', '127.0.0.1', '-scenario', 'solo-precision', '-rifleRate', '2'])
        pid = desktop.launch(argv, a.player.parent)
        (folder / 'invocation.json').write_text(json.dumps(dict(args=argv, pid=pid, desktop=desktop.name, inputDesktop=desktop.initial_input), indent=2))
    wait(lambda s: all(v['state'] == 'Running' and v['run'] == 1 for v in s), 30)
    for run in (1,) if a.reference else range(1, a.runs + 1):
        if not a.reference:
            choose(2, [3, 4] if run == 1 else [3, 4])
            choose(3, [4, 6] if run == 1 else [5, 3])
        ended = wait(lambda s: all(v['state'] in ('Complete', 'Defeat', 'Aborted') for v in s), 130)
        wait(lambda s: all(read(a.results / role / f'summary-run-{run}.json') for role in roles))
        audit(run, ended)
        if not a.reference:
            for i in range(len(roles)):
                command(i, 'retry', ended[i])
            reset = wait(lambda s: all(v['state'] == 'Running' and v['run'] == run + 1 for v in s))
            for value in reset:
                assert value['xp'] == 0 and value['level'] == 1 and value['choicePhase'] == 0
                for player in value['players'][:len(roles)]:
                    assert player['Arsenal'] == 1 and player['FirstAddition'] == player['Specialization'] == player['Offer']['Id'] == 0
            command(0, 'choice', ended[0], upgrade=3)
            wait(lambda s: s[0]['lastSequence'] == sequences[0])
            assert states()[0]['commandResult'] == 'Stale run/life'
    loss = None
    if not a.solo and not a.reference:
        wait(lambda s: all(v['state'] == 'Choosing' for v in s), 70)
        desktop.kernel.TerminateProcess(desktop.children[1].process, 125)
        end = time.monotonic() + 8
        while time.monotonic() < end:
            desktop.verify_input()
            loss = states()[0]
            if loss and loss['state'] == 'Aborted':
                break
            time.sleep(.05)
        assert loss and loss['state'] == 'Aborted'
    report = dict(result='PASS', source=comparisons[0]['terminal']['source'], reference=a.reference,
                  note='Owned background desktop; game-owned scripted input/preview. Functional evidence only.',
                  comparisons=comparisons, pauses=pauses, afterClientLoss=loss)
    (a.results / 'verification.json').write_text(json.dumps(report, indent=2))
    print('PASS: weapon-stack ordinary progression/reference checks', flush=True)
except Exception as error:
    (a.results / 'failure.json').write_text(json.dumps(dict(result='FAIL', error=str(error),
        states=[None if value is None else {k: value.get(k) for k in ('run', 'tick', 'state', 'level', 'exitReason', 'source')}
                for value in states()]), indent=2))
    raise
finally:
    (a.results / 'observations.json').write_text(json.dumps(history))
    after = desktop.input_name()
    desktop.close()
    (a.results / 'desktop-lifetime.json').write_text(json.dumps(dict(desktop=desktop.name, inputBefore=desktop.initial_input,
        inputAfter=after, unchanged=after == desktop.initial_input, children=[c.pid for c in desktop.children], stopped=True), indent=2))
