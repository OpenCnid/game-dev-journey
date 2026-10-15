"""Read-only archive and preserved-failure audit. No capture or performance claim."""
import csv
import hashlib
import json
from pathlib import Path
import sys

repo = Path(__file__).resolve().parents[4]
manifest_path = repo / 'docs/playtests/evidence/gdj-3-weapon-stack/release-manifest-v3.json'
manifest = json.loads(manifest_path.read_text(encoding='utf-8-sig'))
archive = Path(manifest['archive'])
errors = []
for entry in manifest['files']:
    file = archive / entry['path']
    if not file.is_file():
        errors.append({'path': entry['path'], 'error': 'missing'})
        continue
    data = file.read_bytes()
    if len(data) != entry['bytes'] or hashlib.sha256(data).hexdigest() != entry['sha256']:
        errors.append({'path': entry['path'], 'error': 'size/hash mismatch'})
expected = {f['path'].replace('\\', '/') for f in manifest['files']}
extra = sorted(p.relative_to(archive).as_posix() for p in archive.rglob('*')
               if p.is_file() and p.relative_to(archive).as_posix() not in expected)
assert not errors and not extra, (errors, extra)
artifacts = archive.parent.parent / 'artifacts'
failures = []
for name in ('gdj3-v2-solo', 'gdj3-v2-reference-all'):
    folder = artifacts / name / 'solo'
    summary = json.loads((folder / 'summary-run-1.json').read_text(encoding='utf-8-sig'))
    with (folder / 'frames.csv').open(encoding='utf-8-sig', newline='') as f:
        rows = list(csv.DictReader(f))
    hashes = {p.name: {'bytes': p.stat().st_size, 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()}
              for p in folder.iterdir() if p.is_file()}
    failures.append({
        'folder': str(folder), 'source': json.loads(summary['source']),
        'reason': summary.get('reason'), 'ticks': summary['ticks'],
        'catchupLimits': summary['catchupLimits'], 'previewEnabled': summary.get('previewEnabled'),
        'scripted': summary.get('scripted'), 'updateMaxMs': summary.get('updateMaxMs'),
        'abortFrameMs': summary.get('abortFrameMs'),
        'lastTracedTick': rows[-1]['tick'], 'lastTracedFrameMs': rows[-1]['frame_ms'],
        'maxTracedFrameMs': max(float(r['frame_ms']) for r in rows),
        'frames': len(rows), 'rawFiles': hashes,
        'limit': 'Running-only trace excludes the abort frame; no cause can be established. Preview was enabled.'})
result = {'control': {k: manifest[k] for k in ('runtimeSource', 'dirty', 'builtUtc', 'archive', 'fileCount', 'totalBytes')},
          'archiveHashCheck': 'PASS', 'unexpectedFiles': extra, 'preservedFailures': failures,
          'newPerformanceCapture': False, 'stallCause': 'unresolved'}
output = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).with_name('control-audit.json')
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'archiveHashCheck': 'PASS', 'fileCount': manifest['fileCount'],
                  'failures': [{k: f[k] for k in ('ticks', 'reason', 'lastTracedTick', 'maxTracedFrameMs', 'abortFrameMs')}
                               for f in failures], 'output': str(output)}))
