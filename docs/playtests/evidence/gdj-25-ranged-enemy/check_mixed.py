"""One real mixed-threat solo/pair smoke on an owned background desktop.
Ordinary input, choices and Retry only; mechanics evidence, not human readability or FPS.
"""
import argparse
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
p.add_argument('--port', default='17845')
a = p.parse_args()
a.player, a.results = a.player.resolve(), a.results.resolve()
a.results.mkdir(parents=True, exist_ok=False)
roles = ['solo'] if a.solo else ['host', 'client']
desktop = OwnedDesktop()
sequences = [0] * len(roles)
observations, pauses = [], []


def read(path):
    try:
        return json.loads(read_shared(path).decode('utf-8-sig'))
    except (OSError, ValueError):
        return None


def states():
    return [read(a.results / role / 'state.json') for role in roles]


def wait(predicate, seconds=20, act=None):
    end = time.monotonic() + seconds
    while time.monotonic() < end:
        desktop.verify_input()
        values = states()
        if all(values):
            if not observations or (values[0]['run'], values[0]['tick'], values[0]['controlRevision']) != (observations[-1][0]['run'], observations[-1][0]['tick'], observations[-1][0]['controlRevision']):
                observations.append(values)
            if any(v['state'] == 'Aborted' for v in values):
                raise RuntimeError('Player aborted: ' + str([(v['tick'], v['state']) for v in values]))
            if predicate(values):
                return values
            if act:
                act(values)
        time.sleep(.05)
    raise TimeoutError(str([(v['tick'], v['state'], v['level']) if v else None for v in states()]))


def command(i, action, value, **fields):
    sequences[i] += 1
    content = dict(sequence=sequences[i], action=action, run=value['run'], life=value['life'], **fields)
    folder = a.results / roles[i]
    temporary = folder / 'next.json'
    temporary.write_text(json.dumps(content), encoding='utf-8')
    temporary.replace(folder / 'command.json')


def choose(level, upgrades):
    earned = wait(lambda s: all(v['state'] == 'Choosing' and v['level'] == level for v in s), 70)
    assert all(v['tick'] == earned[0]['tick'] and v['ranged'] == earned[0]['ranged'] for v in earned)
    time.sleep(1)
    held = states()
    assert all(v['tick'] == earned[0]['tick'] and v['ranged'] == earned[0]['ranged'] for v in held), 'Attacks/bolts moved during choices'
    for i, upgrade in enumerate(upgrades[:len(roles)]):
        command(i, 'choice', held[i], upgrade=upgrade)
    selected = wait(lambda s: all(all(v['players'][i]['Offer']['Selected'] == upgrade for i, upgrade in enumerate(upgrades[:len(roles)])) for v in s))
    for i in range(len(roles)):
        command(i, 'continue', selected[i], phase=selected[i]['choicePhase'])
    resumed = wait(lambda s: all(v['state'] == 'Running' for v in s))
    pauses.append(dict(earned=earned, held=held, selected=selected, resumed=resumed))
    print(f'Level {level}: frozen ranged state, personal choices and Continue pass', flush=True)


try:
    for i, role in enumerate(roles):
        output = a.results / role
        output.mkdir()
        argv = [str(a.player), '-force-d3d11', '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720',
                '-port', a.port, '-sharedScripted', '-sharedPreview', '-sharedMute', '-sharedResults', str(output), '-logFile', str(output / 'player.log')]
        argv += ['-playtest', '-scenario', 'mixed-threats'] if a.solo else (['-host', '-sharedLoopback', '-scenario', 'mixed-threats'] if i == 0 else ['-join', '127.0.0.1'])
        pid = desktop.launch(argv, a.player.parent)
        (output / 'invocation.json').write_text(json.dumps(dict(args=argv, pid=pid, desktop=desktop.name, inputDesktop=desktop.initial_input), indent=2))
    opening = wait(lambda s: all(v['state'] == 'Running' for v in s), 40)
    # Allow one actual telegraph/flight before the automated rifle route clears the room.
    for i in range(len(roles)):
        command(i, 'idle', opening[i])
    incoming = wait(lambda s: all(v['ranged']['Fired'] > 0 for v in s), 20)
    for i in range(len(roles)):
        command(i, 'scripted', incoming[i])
    choose(2, [3, 4])
    choose(3, [4, 6])
    if not a.solo:
        active = states()
        prior_life = active[0]['players'][0]['Life']
        command(0, 'idle', active[0])
        next_push = [0]

        def approach_threat(values):
            value = values[0]
            player = value['players'][0]
            if value['state'] != 'Running' or player['Health'] <= 0 or player['Life'] > prior_life or time.monotonic() < next_push[0]:
                return
            position = player['Position']
            enemy = min(value['enemies'], key=lambda e: (e['Position']['x'] - position['x']) ** 2 + (e['Position']['z'] - position['z']) ** 2)['Position']
            dx, dz = enemy['x'] - position['x'], enemy['z'] - position['z']
            distance = max(.001, (dx * dx + dz * dz) ** .5)
            command(0, 'input', value, moveX=dx / distance, moveZ=dz / distance, yaw=0, pitch=0, duration=1, fire=False)
            next_push[0] = time.monotonic() + .5

        returned = wait(lambda s: all(v['players'][0]['Life'] > prior_life and v['players'][0]['Health'] > 0 for v in s), 65, act=approach_threat)
        assert all(v['players'][0]['Arsenal'] == 7 for v in returned), 'Growth lost on return'
        command(0, 'scripted', returned[0])
        print('Pair: death/return retained earned arsenal', flush=True)
    ended = wait(lambda s: all(v['state'] in ('Complete', 'Defeat') for v in s), 140)
    wait(lambda s: all(read(a.results / role / 'summary-run-1.json') for role in roles))
    summaries = [read(a.results / role / 'summary-run-1.json') for role in roles]
    for summary in summaries:
        assert summary['scenario']['name'] == 'Mixed threats' and summary['level'] == 3
        assert summary['ranged']['Fired'] > 0 and summary['ranged']['Overflow'] == 0 and summary['ranged']['Count'] == 0
        assert summary['xpDrops'] == sum(x['Kills'] for x in summary['players'])
        assert summary['xp'] + summary['uncollectedXp'] == summary['xpDrops']
    if not a.solo:
        for field in ['players', 'enemyStates', 'ranged', 'xp', 'level', 'choicePhase', 'controlRevision']:
            assert summaries[0][field] == summaries[1][field], 'Terminal mismatch: ' + field
        logs = [[json.loads(line) for line in (a.results / role / 'growth.jsonl').read_text().splitlines()] for role in roles]
        assert logs[0] == logs[1], 'Growth replica mismatch'
    windups = sum(any(x['Phase'] == 1 for x in v[0]['ranged']['Attacks']) for v in observations if v[0].get('ranged'))
    flights = sum(v[0]['ranged']['Count'] > 0 for v in observations if v[0].get('ranged'))
    assert windups > 0 and flights > 0, 'No real windup/flight observations'
    for i in range(len(roles)):
        command(i, 'retry', ended[i])
    reset = wait(lambda s: all(v['state'] == 'Running' and v['run'] == 2 for v in s))
    assert all(v['level'] == 1 and v['xp'] == 0 and v['ranged']['Fired'] == 0 and v['ranged']['Count'] == 0 for v in reset)
    assert all(all(x['Arsenal'] == 1 and x['Offer']['Id'] == 0 and x['Life'] == 1 for x in v['players'][:len(roles)]) for v in reset)
    report = dict(result='PASS', note=__doc__, source=summaries[0]['source'], summaries=summaries, pauses=pauses,
                  windupObservations=windups, flightObservations=flights, reset=reset)
    (a.results / 'verification.json').write_text(json.dumps(report, indent=2))
    print('PASS: mixed-threat solo/pair loop and Retry', flush=True)
except Exception as error:
    (a.results / 'failure.json').write_text(json.dumps(dict(error=str(error), states=states()), indent=2))
    raise
finally:
    (a.results / 'observations.json').write_text(json.dumps(observations))
    after = desktop.input_name()
    desktop.close()
    (a.results / 'desktop-lifetime.json').write_text(json.dumps(dict(inputBefore=desktop.initial_input, inputAfter=after,
        unchanged=after == desktop.initial_input, children=[c.pid for c in desktop.children], stopped=True), indent=2))
