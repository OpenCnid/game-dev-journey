"""Read-only compact index of audited scale captures; does not declare acceptance."""
import argparse
import json
from pathlib import Path

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('audits', type=Path)
p.add_argument('output', type=Path)
a = p.parse_args()
captures = []
for file in sorted(a.audits.glob('*/audit.json')):
    audit = json.loads(file.read_text(encoding='utf-8'))
    roles = []
    for role in audit['roles']:
        report = role['report']
        window = role.get('workWindow') or role.get('coldWorkWindow')
        roles.append(dict(role=report['role'], source=json.loads(report['source']), scenario=report['scenario'],
            state=report['state'], ticks=report['ticks'], reason=report['reason'],
            frameMs=role['frameMs'], coldFrameMs=role['coldFrameMs'], confirmationMs=role['confirmationMs'],
            rttMs=role['rttMs'], catchupLimits=report['catchupLimits'],
            authorityWork=report.get('authorityWork'), loggingMs=report.get('loggingMs'),
            managedHeapBytes=report.get('managedHeapBytes'), workingSetBytes=report.get('workingSetBytes'),
            terminalQueueRecords=report['outgoingPending'], queueHighWater=report['outgoingHighWater'],
            queueMaxAgeMs=report.get('outgoingMaxAgeMs'), correctness=role['correctness'],
            stageFrameMs=window['stageFrameMs'] if window else None,
            payloadRates=window['payloadRatesPerSecond'] if window else None,
            queueAgeGate=window['queueAgeGate'] if window else None))
    captures.append(dict(capture=file.parent.name, audit=str(file), roles=roles,
                         terminalParity=audit.get('terminalParity'), replicaEventParity=audit.get('replicaEventParity')))
result = dict(classification='exploratory; no acceptance inferred from numerical results', captures=captures)
a.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'captures': len(captures), 'output': str(a.output)}))
