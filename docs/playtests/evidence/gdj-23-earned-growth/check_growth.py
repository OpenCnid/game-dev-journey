"""Ordinary earned-growth checks on the existing owned background desktop.
Game-owned input/state only; functional evidence, not human play or native FPS.
"""
import argparse
import csv
import json
import math
import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[4] / 'tools'))
from background_desktop import OwnedDesktop, read_shared

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--player', type=Path, required=True)
p.add_argument('--results', type=Path, required=True)
p.add_argument('--solo', action='store_true')
p.add_argument('--port', default='17823')
a = p.parse_args()
a.results.mkdir(parents=True, exist_ok=False)
roles = ['solo'] if a.solo else ['host', 'client']
history, comparisons, pauses = [], [], []
sequences = [0] * len(roles)
desktop = OwnedDesktop()


def read(path):
    try:
        return json.loads(read_shared(path).decode('utf-8-sig'))
    except (OSError, ValueError):
        return None


def states():
    return [read(a.results / role / 'state.json') for role in roles]


def wait(predicate, seconds=12):
    deadline = time.monotonic() + seconds
    while time.monotonic() < deadline:
        desktop.verify_input()
        values = states()
        if all(values):
            keys = lambda v: (v['run'], v['tick'], v['controlRevision'], v['state'])
            if not history or keys(values[0]) != keys(history[-1][0]):
                history.append(values)
            if predicate(values):
                return values
        if any(desktop.kernel.WaitForSingleObject(c.process, 0) == 0 for c in desktop.children):
            raise RuntimeError('An owned player exited')
        time.sleep(.05)
    raise TimeoutError(str(states()))


def command(i, action, state, **fields):
    sequences[i] += 1
    item = dict(sequence=sequences[i], action=action, run=state['run'], life=state['life'], **fields)
    directory = a.results / roles[i]
    temporary = directory / 'command-next.json'
    temporary.write_text(json.dumps(item), encoding='utf-8')
    temporary.replace(directory / 'command.json')


def frozen(before, seconds):
    end = time.monotonic() + seconds
    physical = lambda v: (v['tick'], v['elapsed'], v['enemies'],
        [(x['Position'], x['Health'], x['Shots'], x['ReturnAt'], x['ImmuneUntil']) for x in v['players']])
    while time.monotonic() < end:
        values = wait(lambda s: all(v['state'] == 'Choosing' for v in s))
        assert all(physical(v) == physical(before[i]) for i, v in enumerate(values)), 'Physical state advanced in choice pause'
        time.sleep(.1)


def choose_and_continue(run):
    earned = wait(lambda s: all(v['state'] == 'Choosing' and v['level'] == 2 for v in s), 65)
    frozen(earned, 2)
    command(0, 'continue', earned[0], phase=earned[0]['choicePhase'])
    wait(lambda s: s[0]['lastSequence'] == sequences[0])
    assert states()[0]['state'] == 'Choosing'
    command(0, 'choice', earned[0], upgrade=run)
    selected = wait(lambda s: all(v['players'][0]['Offer']['Selected'] == run for v in s))
    assert all(v['tick'] == earned[0]['tick'] for v in selected)
    if not a.solo:
        assert all(v['players'][1]['Offer']['Selected'] == 0 for v in selected), 'A consumed B offer'
        command(1, 'choice', selected[1], upgrade=3-run)
        selected = wait(lambda s: all(v['players'][1]['Offer']['Selected'] == 3-run for v in s))
    command(0, 'choice', selected[0], upgrade=3-run)
    wait(lambda s: s[0]['commandResult'] == 'No valid pending choice')
    command(0, 'continue', selected[0], phase=selected[0]['choicePhase'] + 1)
    wait(lambda s: s[0]['lastSequence'] == sequences[0])
    assert states()[0]['state'] == 'Choosing' and not states()[0]['players'][0]['ChoiceReady']
    command(0, 'continue', selected[0], phase=selected[0]['choicePhase'])
    if not a.solo:
        ready = wait(lambda s: all(v['players'][0]['ChoiceReady'] for v in s))
        frozen(ready, 1)
        command(1, 'continue', ready[1], phase=ready[1]['choicePhase'])
    resumed = wait(lambda s: all(v['state'] == 'Running' for v in s))
    pauses.append(dict(earned=earned, selected=selected, resumed=resumed, heldWallSeconds=2 if a.solo else 3))
    return resumed


def terminal():
    result = wait(lambda s: all(v['state'] in ('Complete', 'Defeat', 'Aborted') for v in s), 80)
    assert all(v['state'] != 'Aborted' for v in result), result
    if not a.solo:
        assert result[0]['players'] == result[1]['players'] and result[0]['enemies'] == result[1]['enemies']
        assert result[0]['xp'] == result[1]['xp']
    return result


def retry(last):
    old = last[0]['run']
    command(0, 'retry', last[0])
    if not a.solo:
        ready = wait(lambda s: all(v['players'][0]['Ready'] for v in s))
        assert all(v['run'] == old for v in ready)
        command(1, 'retry', ready[1])
    reset = wait(lambda s: all(v['run'] == old + 1 and v['state'] == 'Running' for v in s))
    for value in reset:
        assert value['xp'] == 0 and value['level'] == 1 and value['choicePhase'] == value['controlRevision'] == 0
        for player in value['players'][:len(roles)]:
            assert player['Offer']['Id'] == 0 and player['BodyDamage'] == 20 and not player['ChoiceReady']
            assert math.isclose(player['RifleInterval'], .12, abs_tol=1e-7)
        assert value['scenario']['identity'] == last[0]['scenario']['identity']
    command(0, 'choice', last[0], upgrade=1)
    wait(lambda s: s[0]['commandResult'] == 'Stale run/life')
    command(0, 'continue', last[0], phase=last[0]['choicePhase'])
    wait(lambda s: s[0]['lastSequence'] == sequences[0])
    assert states()[0]['state'] == 'Running'
    return reset


def audit(run):
    logs, feedback = [], []
    for role in roles:
        events = [json.loads(line) for line in (a.results / role / 'growth.jsonl').read_text().splitlines()]
        events = [e for e in events if e['Run'] == run]
        logs.append(events)
        deaths = [e for e in events if e['Kind'] == 0]
        pickups = [e for e in events if e['Kind'] == 1]
        assert len({(e['Orb']['Enemy'], e['Orb']['Incarnation']) for e in deaths}) == len(deaths)
        ledger = {}
        for e in events:
            orb = e['Orb']
            if e['Kind'] == 0:
                assert orb['Value'] == 1 and orb['Id'] not in ledger
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
        summary = read(a.results / role / f'summary-run-{run}.json')
        assert summary['xp'] == sum(e['Orb']['Value'] for e in pickups)
        assert summary['uncollectedOrbs'] == len(ledger)
        assert summary['uncollectedXp'] == sum(o['Value'] for o in ledger.values())
        assert len(deaths) == summary['xp'] + summary['uncollectedXp']
        assert summary['catchupLimits'] == 0 and summary['level'] == 2
        assert len([e for e in events if e['Kind'] == 2]) == len(roles)
        assert len([e for e in events if e['Kind'] == 3]) == len(roles)
        with (a.results / role / 'events.csv').open(newline='') as stream:
            feedback.append([{k: v for k, v in row.items() if k not in ('wall_seconds', 'confirmed_delay_ms')}
                             for row in csv.DictReader(stream) if int(row['run']) == run and row['decision'] != 'Terminal'])
    if not a.solo:
        assert logs[0] == logs[1], 'Progression replica differs'
        assert feedback[0] == feedback[1], 'Nonterminal combat feedback differs'
    level_time = next(e['Tick'] / 60 for e in logs[0] if e['Kind'] == 2)
    print(f'Run {run}: level at {level_time:.3f}s; {len(logs[0])} growth events; XP conserved', flush=True)
    return dict(levelSeconds=level_time, earningTargetMet=10 <= level_time <= 25,
                growthEvents=len(logs[0]), merges=sum(e['Kind'] == 5 for e in logs[0]),
                feedbackEvents=len(feedback[0]), terminal=summary)


try:
    for i, role in enumerate(roles):
        folder = a.results / role
        folder.mkdir()
        argv = [str(a.player), '-force-d3d11', '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720',
                '-port', a.port, '-sharedScripted', '-sharedPreview', '-sharedResults', str(folder), '-logFile', str(folder / 'player.log'),
                '-screenshot', str(folder / 'level-up.png'), '-screenshotAtLevelUp']
        argv += ['-playtest', '-scenario', 'earned-growth'] if a.solo else (
            ['-host', '-sharedLoopback', '-scenario', 'earned-growth'] if i == 0 else
            ['-join', '127.0.0.1', '-scenario', 'solo-precision', '-rifleRate', '2'])
        pid = desktop.launch(argv, a.player.parent)
        (folder / 'invocation.json').write_text(json.dumps(dict(args=argv, pid=pid, desktop=desktop.name,
            inputDesktop=desktop.initial_input, onInputDesktop=False), indent=2))
    wait(lambda s: all(v['state'] == 'Running' and v['run'] == 1 for v in s), 30)
    for run in (1, 2):
        choose_and_continue(run)
        ended = terminal()
        comparisons.append(audit(run))
        current = retry(ended)
    loss = None
    if not a.solo:
        wait(lambda s: all(v['state'] == 'Choosing' for v in s), 65)
        desktop.kernel.TerminateProcess(desktop.children[1].process, 125)
        deadline = time.monotonic() + 8
        while time.monotonic() < deadline:
            desktop.verify_input()
            loss = states()[0]
            if loss and loss['state'] == 'Aborted':
                break
            time.sleep(.05)
        assert loss and loss['state'] == 'Aborted', loss
    report = dict(result='PASS', source=comparisons[0]['terminal']['source'],
                  note='Owned background desktop, automated game-owned input/preview. Functional only; no human enjoyment/native performance claim.',
                  comparisons=comparisons, pauses=pauses, afterClientLoss=loss)
    (a.results / 'verification.json').write_text(json.dumps(report, indent=2))
    print('PASS: freeze/explicit readiness, ordinary XP, contrasting/swapped choices, event parity, complete reset, stale/duplicate/loss rejection.', flush=True)
finally:
    (a.results / 'observations.json').write_text(json.dumps(history))
    after = desktop.input_name()
    desktop.close()
    (a.results / 'desktop-lifetime.json').write_text(json.dumps(dict(desktop=desktop.name,
        inputBefore=desktop.initial_input, inputAfter=after, unchanged=after == desktop.initial_input,
        children=[c.pid for c in desktop.children], stopped=True), indent=2))
