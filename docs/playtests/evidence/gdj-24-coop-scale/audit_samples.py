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

def work_window(rows):
    if not rows or 'sim_ms' not in rows[0]:
        return None
    first, last = rows[0], rows[-1]
    seconds = float(last['wall_seconds']) - float(first['wall_seconds'])
    cumulative = ['steering_checks', 'contact_tests', 'attraction_queries', 'presentation_rays',
                  'presentation_probes', 'sent_bytes', 'received_bytes', 'sent_messages',
                  'received_messages', 'snapshot_bytes', 'snapshot_messages', 'serialization_ms',
                  'allocated_thread_bytes', 'gc0', 'gc1', 'gc2', 'xp_drops', 'xp_merges',
                  'xp_move_events', 'xp_collections']
    cumulative += [k for k in first if k.startswith('weapon') and k[6:7].isdigit()]
    deltas = {k: float(last[k]) - float(first[k]) for k in cumulative}
    stages = {k: distribution([float(r[k]) for r in rows]) for k in
              ('sim_ms', 'crowd_ms', 'weapon_ms', 'xp_ms', 'presentation_ms',
               'render_prepare_ms', 'render_submit_ms', 'pickup_render_ms', 'net_poll_ms')}
    # Authority stages are zero on frames without a tick; also report cost per executed tick.
    stages['crowd_ms_per_tick'] = distribution([float(r['crowd_ms']) / int(r['sim_ticks'])
                                              for r in rows if int(r['sim_ticks'])])
    gauges = {k: {'first': float(first[k]), 'last': float(last[k]),
                  'min': min(float(r[k]) for r in rows), 'max': max(float(r[k]) for r in rows)}
              for k in ('enemies', 'installed_instances', 'active_instances', 'submitted_instances',
                        'instance_batches', 'pickup_carriers', 'secondary_cues', 'uncollected_xp',
                        'queue_records', 'queue_bytes', 'queue_age_ms') if k in first}
    longest, start = 0, None
    for r in rows:
        wall = float(r['wall_seconds'])
        if float(r['queue_age_ms']) > 100:
            if start is None: start = wall
            longest = max(longest, wall - start)
        else:
            start = None
    return {'wallSeconds': seconds, 'tickSpan': int(last['tick']) - int(first['tick']),
            'cumulativeDeltas': deltas, 'stageFrameMs': stages, 'gauges': gauges,
            'payloadRatesPerSecond': {k: deltas[k] / seconds for k in
                ('sent_bytes', 'received_bytes', 'sent_messages', 'received_messages') if seconds > 0},
            'threadAllocationBytesPerSecond': deltas['allocated_thread_bytes'] / seconds
                if seconds > 0 and float(last['allocated_thread_bytes']) > 0 else None,
            'allocationNote': 'Mono returned zero for the entire thread allocation counter; unavailable, not zero allocation'
                if float(last['allocated_thread_bytes']) == 0 else 'Main thread only; capture delta',
            'longestQueueAgeOver100msSeconds': longest, 'queueAgeGate': longest < 1}

def correctness(folder, report, events):
    ledger, deaths, errors = {}, set(), []
    growth = [json.loads(line) for line in (folder / 'growth.jsonl').read_text(encoding='utf-8-sig').splitlines()]
    try:
        for e in growth:
            if e['Run'] != 1: continue
            orb = e['Orb']
            if e['Kind'] == 0:
                identity = (orb['Enemy'], orb['Incarnation'])
                assert identity not in deaths and orb['Value'] == 1
                deaths.add(identity); ledger[orb['Id']] = orb
            elif e['Kind'] == 1:
                assert ledger.pop(orb['Id'])['Value'] == orb['Value']
            elif e['Kind'] == 4:
                assert ledger[orb['Id']]['Value'] == orb['Value']; ledger[orb['Id']] = orb
            elif e['Kind'] == 5:
                consumed = ledger.pop(e['ConsumedOrb'])
                assert ledger[orb['Id']]['Value'] + consumed['Value'] == orb['Value']; ledger[orb['Id']] = orb
        assert len(deaths) == report['xp'] + report['uncollectedXp']
        assert sum(o['Value'] for o in ledger.values()) == report['uncollectedXp']
        assert len(ledger) == report['uncollectedOrbs']
    except (AssertionError, KeyError) as error:
        errors.append('XP ledger: ' + repr(error))
    outcomes = [{k: v for k, v in e.items() if k not in ('wall_seconds', 'confirmed_delay_ms')}
                for e in events if e['run'] == '1' and e['decision'] != 'Terminal']
    for w in report['weaponWork']:
        rows = [e for e in outcomes if int(e['weapon']) == w['weapon']]
        if sum(e['trigger'] == 'True' for e in rows) != w['triggers']:
            errors.append('Trigger attribution ' + str(w['weapon']))
        rays = sum(int(e['rays']) for e in rows)
        if rays != w['rays'] or sum(int(e['target_tests']) for e in rows) != w['targetTests'] or w['targetTests'] != rays * report['enemies']:
            errors.append('Ray/probe attribution ' + str(w['weapon']))
    for p in range(report['roster']):
        for metric, counter in [('triggers', 'Shots'), ('hits', 'Hits'), ('damage', 'Damage'), ('kills', 'Kills')]:
            if sum(w[metric] for w in report['weaponWork'] if w['player'] == p) != report['players'][p][counter]:
                errors.append('Player attribution ' + str((p, metric)))
    return {'errors': errors, 'xpDeaths': len(deaths), 'outcomeEvents': len(outcomes),
            'outcomesSha256': hashlib.sha256(json.dumps(outcomes, sort_keys=True).encode()).hexdigest(),
            'growthSha256': hashlib.sha256(json.dumps(growth, sort_keys=True).encode()).hexdigest(),
            'terminalQueueEmpty': report['outgoingPending'] == 0 and report.get('outgoingBytes', 0) == 0}

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
    for name in ('frames.csv', 'events.csv', 'growth.jsonl', 'player.log', 'summary-run-1.json', 'invocation.json', 'visibility.json'):
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
            'noCatchup': report['catchupLimits'] == 0, 'rawHashes': raw_hashes,
            'workWindow': work_window(rows), 'coldWorkWindow': work_window(running),
            'terminalRow': all_rows[-1] if all_rows else None, 'correctness': correctness(folder, report, events)}

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
    result['replicaEventParity'] = {key: result['roles'][0]['correctness'][key] == result['roles'][1]['correctness'][key]
                                   for key in ('outcomesSha256', 'growthSha256')}
(args.output / 'audit.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'output': str(args.output), 'terminalParity': result.get('terminalParity'),
                  'roles': [{k: r[k] for k in ('frameMs', 'terminalComplete', 'noCatchup', 'firstTick', 'lastTick')}
                            for r in result['roles']]}))
