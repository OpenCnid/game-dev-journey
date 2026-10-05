"""Functional standalone check through the existing game-owned preview file bridge.
Hidden windows/preview/short runs are not performance or human-play evidence.
"""
import argparse
import csv
import json
import subprocess
import time
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('--player', type=Path, required=True)
p.add_argument('--results', type=Path, required=True)
p.add_argument('--scenario', default='shared-rifle')
p.add_argument('--rate')
p.add_argument('--duration')
p.add_argument('--port', default='17822')
a = p.parse_args()
a.results.mkdir(parents=True, exist_ok=False)
processes = []
sequences = [0, 0]
history = []

def read(path):
    try:
        return json.loads(path.read_text(encoding='utf-8-sig'))
    except (OSError, ValueError):
        return None

def states():
    return [read(a.results / role / 'state.json') for role in ('host', 'client')]

def wait(predicate, seconds=10):
    deadline = time.monotonic() + seconds
    while time.monotonic() < deadline:
        values = states()
        if all(values):
            if not history or values[0]['tick'] != history[-1]['host']['tick'] or values[0]['run'] != history[-1]['host']['run']:
                history.append(dict(host=values[0], client=values[1]))
            if predicate(values):
                return values
        for process in processes:
            if process.poll() is not None:
                raise RuntimeError(f'Player exited early: {process.pid}, {process.returncode}')
        time.sleep(.05)
    raise TimeoutError(str(states()))

def command(i, action, state):
    sequences[i] += 1
    item = dict(sequence=sequences[i], action=action, run=state['run'], life=state['life'])
    directory = a.results / ('host' if i == 0 else 'client')
    temporary = directory / 'command-next.json'
    temporary.write_text(json.dumps(item), encoding='utf-8')
    temporary.replace(directory / 'command.json')

def audit_run_one_events():
    outcomes = []
    for role in ('host', 'client'):
        with (a.results / role / 'events.csv').open(encoding='utf-8-sig', newline='') as stream:
            outcomes.append([
                {key: value for key, value in row.items() if key not in ('wall_seconds', 'confirmed_delay_ms')}
                for row in csv.DictReader(stream) if row['run'] == '1' and row['decision'] != 'Terminal'
            ])
    assert outcomes[0] and outcomes[0] == outcomes[1], 'Host/client nonterminal feedback differs, including startup hits.'
    return dict(result='PASS', identicalOutcomes=len(outcomes[0]), firstOutcome=outcomes[0][0])

try:
    for i, role in enumerate(('host', 'client')):
        folder = a.results / role
        folder.mkdir()
        args = [str(a.player), '-force-d3d11', '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720',
                '-port', a.port, '-sharedScripted', '-sharedPreview', '-sharedResults', str(folder), '-logFile', str(folder / 'player.log')]
        if i == 0:
            args += ['-host', '-sharedLoopback', '-scenario', a.scenario]
            if a.rate: args += ['-rifleRate', a.rate]
            if a.duration: args += ['-scenarioDuration', a.duration]
        else:
            # Deliberately conflicting local settings must never override the host.
            args += ['-join', '127.0.0.1', '-scenario', 'solo-precision', '-rifleRate', '2']
        startup = subprocess.STARTUPINFO()
        startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        startup.wShowWindow = 0
        processes.append(subprocess.Popen(args, startupinfo=startup))
        (folder / 'invocation.json').write_text(json.dumps(dict(args=args, pid=processes[-1].pid), indent=2), encoding='utf-8')
    current = wait(lambda s: all(v['state'] == 'Running' and v['run'] == 1 for v in s), 25)
    assert current[0]['scenario'] == current[1]['scenario']
    print('Agreement:', current[0]['scenario'], flush=True)
    start = current
    current = wait(lambda s: all(v['state'] in ('Complete', 'Defeat', 'Aborted') for v in s), 80)
    assert current[0]['state'] != 'Aborted' and current[1]['state'] != 'Aborted', current
    assert current[0]['players'] == current[1]['players']
    assert current[0]['enemies'] == current[1]['enemies']
    terminal = current
    command(0, 'retry', current[0])
    current = wait(lambda s: s[0]['players'][0]['Ready'] and s[1]['players'][0]['Ready'])
    assert all(v['run'] == 1 for v in current)
    one_ready = current
    command(1, 'retry', current[1])
    current = wait(lambda s: all(v['state'] == 'Running' and v['run'] == 2 for v in s))
    assert current[0]['scenario'] == current[1]['scenario']
    assert current[0]['scenario']['identity'] == start[0]['scenario']['identity']
    assert current[0]['scenario']['session'] == start[0]['scenario']['session']
    reset = current
    command(1, 'retry', terminal[1])
    current = wait(lambda s: s[1]['commandResult'] == 'Stale run/life')
    stale = current
    processes[1].kill()
    processes[1].wait()
    # Only surviving host may be polled after deliberately killing our client.
    deadline = time.monotonic() + 8
    while time.monotonic() < deadline:
        host = states()[0]
        if host and host['state'] == 'Aborted': break
        time.sleep(.05)
    assert host['state'] == 'Aborted', host
    event_audit = audit_run_one_events()
    result = dict(result='PASS', note='Functional only; hidden preview pair, automated input, no performance claim.',
                  start=start, terminal=terminal, oneReady=one_ready, reset=reset, stale=stale, afterClientLoss=host, eventAudit=event_audit)
    (a.results / 'verification.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print('PASS: agreement, exact terminal parity, one/both-ready Retry, stale rejection, hard client-loss abort, nonterminal feedback parity.', flush=True)
finally:
    (a.results / 'observations.json').write_text(json.dumps(history), encoding='utf-8')
    for process in processes:
        if process.poll() is None:
            process.kill()
            process.wait()
