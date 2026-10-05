"""Summarize raw native traces; visibility/quiet-desktop evidence is separate.

Usage: audit_samples.py RESULTS OUTPUT --warmup 30
No run is promoted to acceptance by this numerical audit.
"""
import argparse
import csv
import gzip
import hashlib
import json
import math
from pathlib import Path

def distribution(values):
    values = sorted(values)
    if not values:
        return {'samples': 0}
    return {'samples': len(values), **{key: values[math.ceil(p * len(values)) - 1]
        for key, p in [('p50', .5), ('p95', .95), ('p99', .99), ('max', 1)]}}

def audit_role(folder, warmup, out):
    report = json.loads((folder / 'summary-run-1.json').read_text(encoding='utf-8-sig'))
    with (folder / 'frames.csv').open(encoding='utf-8-sig', newline='') as f:
        all_rows = list(csv.DictReader(f))
    running = [r for r in all_rows if r['state'] == 'Running']
    first_wall = float(running[0]['wall_seconds']) if running else 0
    rows = [r for r in running if int(r['tick']) >= math.ceil(warmup * 60)
            and float(r['wall_seconds']) >= first_wall + warmup]
    frame = distribution([float(r['frame_ms']) for r in rows])
    frame.update(over6_944=sum(float(r['frame_ms']) > 6.944 for r in rows),
                 over13_889=sum(float(r['frame_ms']) > 13.889 for r in rows))
    with (folder / 'events.csv').open(encoding='utf-8-sig', newline='') as f:
        events = list(csv.DictReader(f))
    local = '0' if report['role'].startswith('host') else '1'
    min_wall = float(rows[0]['wall_seconds']) if rows else math.inf
    confirms = [float(e['confirmed_delay_ms']) for e in events if e['player'] == local
                and e['decision'] == 'Damage' and float(e['confirmed_delay_ms']) >= 0
                and float(e['wall_seconds']) >= min_wall]
    # Legacy traces repeat the latest RTT each frame; explicitly mark that limitation.
    rtts, seen = [], set()
    for row in rows:
        key = row.get('rtt_sequence', row['wall_seconds'])
        if key not in seen and float(row['rtt_ms']) > 0:
            seen.add(key); rtts.append(float(row['rtt_ms']))
    raw_hashes = {}
    for name in ('frames.csv', 'events.csv', 'growth.jsonl', 'player.log', 'summary-run-1.json'):
        file = folder / name
        if not file.exists():
            continue
        data = file.read_bytes()
        raw_hashes[name] = {'bytes': len(data), 'sha256': hashlib.sha256(data).hexdigest()}
        (out / (folder.name + '-' + name + '.gz')).write_bytes(gzip.compress(data, mtime=0))
    return {'folder': str(folder), 'report': report, 'frameMs': frame,
            'coldFrameMs': distribution([float(r['frame_ms']) for r in running]),
            'confirmationMs': distribution(confirms), 'rttMs': distribution(rtts),
            'rttMethod': 'one sample per RTT sequence' if rows and 'rtt_sequence' in rows[0]
                         else 'latest RTT repeated in legacy frame trace; correlated samples',
            'firstTick': int(rows[0]['tick']) if rows else None,
            'lastTick': int(rows[-1]['tick']) if rows else None,
            'traceWallSpan': float(rows[-1]['wall_seconds']) - float(rows[0]['wall_seconds']) if rows else 0,
            'terminalComplete': report['state'] == 2,
            'noCatchup': report['catchupLimits'] == 0, 'rawHashes': raw_hashes}

parser = argparse.ArgumentParser()
parser.add_argument('results', type=Path)
parser.add_argument('output', type=Path)
parser.add_argument('--warmup', type=float, default=0)
args = parser.parse_args()
args.output.mkdir(parents=True, exist_ok=True)
roles = [args.results] if (args.results / 'summary-run-1.json').exists() else [args.results / r for r in ('host', 'client')]
result = {'warmupSeconds': args.warmup, 'classification': 'numerical audit; acceptance requires separate visibility/correctness gates',
          'roles': [audit_role(r, args.warmup, args.output) for r in roles]}
if len(result['roles']) == 2:
    a, b = [r['report'] for r in result['roles']]
    result['terminalParity'] = {key: a[key] == b[key] for key in
        ('run', 'ticks', 'state', 'players', 'enemyStates', 'xp', 'uncollectedXp', 'level', 'choicePhase', 'controlRevision', 'weaponWork')}
(args.output / 'audit.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'output': str(args.output), 'terminalParity': result.get('terminalParity'),
                  'roles': [{k: r[k] for k in ('frameMs', 'terminalComplete', 'noCatchup', 'firstTick', 'lastTick')}
                            for r in result['roles']]}))
