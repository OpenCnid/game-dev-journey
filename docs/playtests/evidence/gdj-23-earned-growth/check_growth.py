"""Standalone earned-growth checks using the existing game-owned preview bridge.
Scripted ordinary movement/fire/collection; hidden preview is functional evidence only.
"""
import argparse
import csv
import json
import math
import subprocess
import time
from pathlib import Path

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--player', type=Path, required=True)
p.add_argument('--results', type=Path, required=True)
p.add_argument('--solo', action='store_true')
p.add_argument('--port', default='17823')
p.add_argument('--lifecycle', action='store_true')
a = p.parse_args()
a.results.mkdir(parents=True, exist_ok=False)
roles = ['solo'] if a.solo else ['host', 'client']
processes, history, comparisons = [], [], []
sequences = [0] * len(roles)


def read(path):
    try:
        return json.loads(path.read_text(encoding='utf-8-sig'))
    except (OSError, ValueError):
        return None


def states():
    return [read(a.results / role / 'state.json') for role in roles]


def wait(predicate, seconds=12):
    deadline = time.monotonic() + seconds
    while time.monotonic() < deadline:
        values = states()
        if all(values):
            if not history or values[0]['tick'] != history[-1][0]['tick'] or values[0]['run'] != history[-1][0]['run']:
                history.append(values)
            if predicate(values):
                return values
        for process in processes:
            if process.poll() is not None:
                raise RuntimeError(f'Player exited: {process.pid}, {process.returncode}')
        time.sleep(.05)
    raise TimeoutError(str(states()))


def command(i, action, state, **fields):
    sequences[i] += 1
    item = dict(sequence=sequences[i], action=action, run=state['run'], life=state['life'], **fields)
    directory = a.results / roles[i]
    temporary = directory / 'command-next.json'
    temporary.write_text(json.dumps(item), encoding='utf-8')
    temporary.replace(directory / 'command.json')
    return sequences[i]


def terminal():
    result = wait(lambda s: all(v['state'] in ('Complete', 'Defeat', 'Aborted') for v in s), 80)
    assert all(v['state'] != 'Aborted' for v in result), result
    if not a.solo:
        assert result[0]['players'] == result[1]['players']
        assert result[0]['enemies'] == result[1]['enemies']
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
        assert value['xp'] == 0 and value['level'] == 1
        for player in value['players'][:len(roles)]:
            assert player['Offer']['Id'] == 0 and player['BodyDamage'] == 20 and math.isclose(player['RifleInterval'], .12, abs_tol=1e-7)
        assert value['scenario']['identity'] == last[0]['scenario']['identity']
    command(0, 'choice', last[0], upgrade=1)
    wait(lambda s: s[0]['commandResult'] == 'Stale run/life')
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
        assert len({e['Orb']['Id'] for e in pickups}) == len(pickups)
        summary = read(a.results / role / f'summary-run-{run}.json')
        assert summary['xp'] == len(pickups)
        assert summary['uncollectedOrbs'] == len(deaths) - len(pickups)
        assert summary['catchupLimits'] == 0
        assert len([e for e in events if e['Kind'] == 2]) == len(roles)
        assert len([e for e in events if e['Kind'] == 3]) == len(roles)
        assert summary['level'] == 2
        with (a.results / role / 'events.csv').open(newline='') as stream:
            feedback.append([{k: v for k, v in row.items() if k not in ('wall_seconds', 'confirmed_delay_ms')}
                             for row in csv.DictReader(stream) if int(row['run']) == run and row['decision'] != 'Terminal'])
    if not a.solo:
        assert logs[0] == logs[1], 'Progression replica differs'
        assert feedback[0] == feedback[1], 'Nonterminal combat feedback differs'
    level_time = next(e['Tick'] / 60 for e in logs[0] if e['Kind'] == 2)
    print(f'Run {run}: earned level at {level_time:.3f}s; {len(logs[0])} identical growth events', flush=True)
    return dict(levelSeconds=level_time, earningTargetMet=10 <= level_time <= 25,
                growthEvents=len(logs[0]), feedbackEvents=len(feedback[0]), terminal=summary)


try:
    for i, role in enumerate(roles):
        folder = a.results / role
        folder.mkdir()
        args = [str(a.player), '-force-d3d11', '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720',
                '-port', a.port, '-sharedScripted', '-sharedPreview', '-sharedResults', str(folder), '-logFile', str(folder / 'player.log')]
        args += ['-playtest', '-scenario', 'earned-growth'] if a.solo else (
            ['-host', '-sharedLoopback', '-scenario', 'earned-growth'] if i == 0 else
            ['-join', '127.0.0.1', '-scenario', 'solo-precision', '-rifleRate', '2'])
        startup = subprocess.STARTUPINFO()
        startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        startup.wShowWindow = 0
        processes.append(subprocess.Popen(args, startupinfo=startup))
        (folder / 'invocation.json').write_text(json.dumps(dict(args=args, pid=processes[-1].pid), indent=2))
    current = wait(lambda s: all(v['state'] == 'Running' and v['run'] == 1 for v in s), 30)
    for run in (1, 2):
        earned = wait(lambda s: all(v['level'] == 2 for v in s) or any(v['state'] != 'Running' for v in s), 65)
        assert all(v['state'] == 'Running' and v['players'][i]['Offer']['Id'] > 0 for i, v in enumerate(earned)), earned
        command(0, 'choice', earned[0], upgrade=run)
        applied = wait(lambda s: all(v['players'][0]['Offer']['Selected'] == run for v in s))
        if not a.solo:
            assert all(v['players'][1]['Offer']['Selected'] == 0 for v in applied), 'A consumed B offer'
            command(1, 'choice', applied[1], upgrade=3-run)
            applied = wait(lambda s: all(v['players'][1]['Offer']['Selected'] == 3-run for v in s))
        command(0, 'choice', applied[0], upgrade=3-run)
        wait(lambda s: s[0]['commandResult'] == 'No valid pending choice')
        ended = terminal()
        comparisons.append(audit(run))
        current = retry(ended)
    lifecycle = {}
    if a.lifecycle:
        # Diagnostic ordinary idle inputs, with no forced HP, XP or immunity.
        command(0, 'idle', current[0])
        if not a.solo:
            dead = wait(lambda s: s[0]['players'][0]['Health'] == 0 or s[0]['state'] != 'Running', 65)
            assert dead[0]['state'] == 'Running' and dead[0]['players'][1]['Health'] > 0, dead
            earned = wait(lambda s: s[0]['players'][0]['Offer']['Id'] > 0, 35)
            command(0, 'choice', earned[0], upgrade=1)
            returned = wait(lambda s: s[0]['players'][0]['Life'] > 1 and s[0]['players'][0]['Offer']['Selected'] == 1, 15)
            assert returned[0]['players'][0]['BodyDamage'] == 30
            lifecycle = dict(dead=dead, returned=returned)
            command(1, 'idle', returned[1])
        wipe = terminal()
        assert wipe[0]['state'] == 'Defeat', wipe
        lifecycle['wipe'] = wipe
        current = retry(wipe)
    loss = None
    if not a.solo:
        processes[1].kill(); processes[1].wait()
        deadline = time.monotonic() + 8
        while time.monotonic() < deadline:
            loss = states()[0]
            if loss and loss['state'] == 'Aborted': break
            time.sleep(.05)
        assert loss['state'] == 'Aborted', loss
    report = dict(result='PASS', source=comparisons[0]['terminal']['source'],
                  note='Functional only. Automated input and preview; no human enjoyment/performance claim.',
                  comparisons=comparisons, lifecycle=lifecycle, afterClientLoss=loss)
    (a.results / 'verification.json').write_text(json.dumps(report, indent=2))
    print('PASS: ordinary earned XP, both/swapped choices, exact replica/event parity, reset/stale/duplicate rejection.', flush=True)
finally:
    (a.results / 'observations.json').write_text(json.dumps(history))
    for process in processes:
        if process.poll() is None:
            process.kill(); process.wait()
