"""Audit GDJ-20 native raw captures; visibility is separately observed evidence.

Usage: python audit-capture.py RESULTS OUTPUT EXPECTED_REVISION
The full Running trace is used, including each run's first three seconds.
Native summary percentiles omit those seconds and remain preserved separately.
"""
import csv
import gzip
import hashlib
import json
import math
import shutil
import sys
from collections import Counter
from pathlib import Path


def sha(data):
    return hashlib.sha256(data).hexdigest()


def distribution(values):
    values = sorted(values)
    assert values, 'Empty measurement'
    return {'samples': len(values), **{
        key: values[max(0, math.ceil(p * len(values)) - 1)]
        for key, p in [('p50', .5), ('p95', .95), ('p99', .99), ('max', 1)]}}


root, out, expected = Path(sys.argv[1]), Path(sys.argv[2]), sys.argv[3]
out.mkdir(parents=True, exist_ok=True)
counter_fields = ['A_shots', 'B_shots', 'A_hits', 'B_hits', 'A_damage',
                  'B_damage', 'A_kills', 'B_kills', 'A_hp', 'B_hp', 'enemies']
event_fields = ['run', 'player', 'weapon', 'event', 'pose_tick', 'life',
                'decision', 'enemy', 'incarnation', 'region', 'requested',
                'before', 'actual', 'after']
traces, events, hashes = {}, {}, {}
for role in ('host', 'client'):
    for name in ('frames.csv', 'events.csv'):
        data = (root / role / name).read_bytes()
        hashes[f'{role}/{name}'] = {'bytes': len(data), 'sha256': sha(data)}
        (out / f'v9-{role}-{name}.gz').write_bytes(gzip.compress(data, mtime=0))
    with (root / role / 'frames.csv').open(newline='', encoding='utf-8-sig') as f:
        traces[role] = list(csv.DictReader(f))
    with (root / role / 'events.csv').open(newline='', encoding='utf-8-sig') as f:
        events[role] = list(csv.DictReader(f))
    for run in (1, 2, 3, 4):
        shutil.copyfile(root / role / f'summary-run-{run}.json',
                        out / f'v9-run{run}-{role}.json')
shutil.copyfile(root / 'invocation.json', out / 'v9-invocation.json')
results = []
for run in (2, 3, 4):
    reports = {role: json.loads((root / role / f'summary-run-{run}.json').read_text(encoding='utf-8-sig'))
               for role in ('host', 'client')}
    parity = {key: reports['host'][key] == reports['client'][key]
              for key in ('run', 'ticks', 'state', 'players', 'enemyStates')}
    assert all(parity.values()), f'Run {run}: terminal discrepancy'
    role_results, tick_views, event_views = {}, {}, {}
    for role in ('host', 'client'):
        report = reports[role]
        source = json.loads(report['source'])
        assert source['revision'] == expected and source['dirty'] is False
        assert report['run'] == run and report['ticks'] == 3600 and report['state'] == 2
        assert report['catchupLimits'] == 0
        assert (report['width'], report['height'], report['frameCap'], report['vsync']) == (1920, 1080, -1, 0)
        assert report['scripted'] and not report['previewEnabled'] and not report['development']
        rows = [row for row in traces[role] if int(row['run']) == run]
        assert rows and all(row['state'] == 'Running' for row in rows)
        ticks = {}
        for row in rows:
            tick = int(row['tick'])
            counters = tuple(int(row[field]) for field in counter_fields)
            assert tick not in ticks or ticks[tick] == counters, f'Run {run}: unstable tick counters'
            ticks[tick] = counters
        assert min(ticks) <= 1 and max(ticks) >= 3598
        assert ticks.get(0) == (0, 0, 0, 0, 0, 0, 0, 0, 100, 100, 12), f'Run {run}: reset discrepancy'
        span = float(rows[-1]['wall_seconds']) - float(rows[0]['wall_seconds'])
        assert span >= 59.94, f'Run {run}: incomplete trace duration {span}'
        tick_views[role] = ticks
        hits = [row for row in events[role] if int(row['run']) == run]
        identities = [(row['run'], row['player'], row['weapon'], row['life'], row['event']) for row in hits]
        assert len(identities) == len(set(identities)), f'Run {run}: duplicate event identity'
        event_views[role] = Counter(tuple(row[field] for field in event_fields) for row in hits)
        damage = [row for row in hits if row['decision'] == 'Damage']
        assert all(int(row['actual']) == min(int(row['requested']), int(row['before']))
                   and int(row['after']) == int(row['before']) - int(row['actual'])
                   for row in damage), f'Run {run}: damage arithmetic mismatch'
        deaths_by_enemy = [(row['enemy'], row['incarnation']) for row in damage if row['after'] == '0']
        assert len(deaths_by_enemy) == len(set(deaths_by_enemy)), f'Run {run}: duplicate enemy death'
        for player, state in enumerate(report['players']):
            personal = [row for row in hits if row['player'] == str(player)]
            personal_damage = [row for row in personal if row['decision'] == 'Damage']
            assert sum(row['decision'] in ('Damage', 'Miss') for row in personal) == state['Shots']
            assert len(personal_damage) == state['Hits']
            assert sum(int(row['actual']) for row in personal_damage) == state['Damage']
            assert sum(row['after'] == '0' for row in personal_damage) == state['Kills']
        local = '0' if role == 'host' else '1'
        confirmed = distribution([float(row['confirmed_delay_ms']) for row in damage
                                  if row['player'] == local and float(row['confirmed_delay_ms']) >= 0])
        frame = distribution([float(row['frame_ms']) for row in rows])
        rtt = distribution([float(row['rtt_ms']) for row in rows if float(row['rtt_ms']) > 0])
        frame['over6_944'] = sum(float(row['frame_ms']) > 6.944 for row in rows)
        frame['over13_889'] = sum(float(row['frame_ms']) > 13.889 for row in rows)
        deaths = []
        for player, field in enumerate(('A_hp', 'B_hp')):
            previous, death_tick = 100, None
            for tick, counters in sorted(ticks.items()):
                health = counters[counter_fields.index(field)]
                if previous > 0 and health == 0:
                    death_tick = tick
                    deaths.append({'player': player, 'deathTick': tick, 'returnTick': None})
                if previous == 0 and health > 0:
                    deaths[-1]['returnTick'] = tick
                    assert death_tick is not None and 300 <= tick - death_tick <= 301
                previous = health
        role_results[role] = {
            'frameMs': frame, 'confirmedHitMs': confirmed, 'rttMs': rtt,
            'traceSpanSeconds': span, 'firstTick': min(ticks), 'lastTick': max(ticks),
            'events': len(hits), 'damageEvents': len(damage), 'deaths': deaths,
            'tickZeroResetCountersAgree': True,
            'uniqueEnemyDeaths': len(deaths_by_enemy), 'eventCountersMatchTerminal': True,
            'decisionCounts': dict(Counter(row['decision'] for row in hits)),
            'terminalPlayers': [{key: p[key] for key in ('Health', 'Life', 'Shots', 'Hits', 'Damage', 'Kills')}
                                for p in report['players']],
            'reportedFrameWindowStartsAtSimulationSecond': 3,
            'frameGatePass': frame['p95'] <= 6.94 and frame['p99'] <= 10,
            'confirmedHitGatePass': confirmed['p95'] <= 100,
            'rttWithin20MsThroughoutSamples': rtt['max'] <= 20,
        }
    common = sorted(set(tick_views['host']) & set(tick_views['client']))
    mismatches = [tick for tick in common if tick_views['host'][tick] != tick_views['client'][tick]]
    assert len(common) >= 3500 and not mismatches, f'Run {run}: sampled counter discrepancy'
    assert event_views['host'] == event_views['client'], f'Run {run}: event outcome discrepancy'
    results.append({'run': run, 'terminalParity': parity, 'commonSampledTicks': len(common),
                    'counterDiscrepancies': len(mismatches), 'eventOutcomesAgree': True,
                    'roles': role_results})
result = {'sourceRevision': expected, 'rawTraceHashes': hashes, 'runs': results,
          'frameWindow': 'All Running rows, including first three seconds; terminal state confirms 3600 ticks/60 seconds.',
          'percentileMethod': 'Nearest rank: ceil(p*n)-1 in sorted samples.',
          'evidenceLimits': 'Common ticks verify sampled counters/HP, terminal snapshots verify full state. Visibility separately observed; no OS present/CPU-work/GPU timing, physical photon latency or LAN/internet conclusion.'}
(out / 'v9-performance-verification.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
for item in results:
    print(json.dumps({'run': item['run'], 'commonTicks': item['commonSampledTicks'],
                      'roles': {role: {key: data[key] for key in ('frameMs', 'confirmedHitMs', 'rttMs', 'terminalPlayers', 'deaths')}
                                for role, data in item['roles'].items()}}, separators=(',', ':')))
