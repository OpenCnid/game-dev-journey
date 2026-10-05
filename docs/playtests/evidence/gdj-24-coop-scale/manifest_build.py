"""Hash a whole frozen player directory and optionally ZIP it for delivery."""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile

parser = argparse.ArgumentParser()
parser.add_argument('player', type=Path)
parser.add_argument('stamp', type=Path)
parser.add_argument('output', type=Path)
parser.add_argument('--zip', type=Path)
args = parser.parse_args()
player = args.player.resolve()
stamp = json.loads(args.stamp.read_text(encoding='utf-8-sig'))
assert stamp['dirty'] is False, 'Use a clean source build'
files = [{'path': p.relative_to(player).as_posix(), 'bytes': p.stat().st_size,
          'sha256': hashlib.sha256(p.read_bytes()).hexdigest()}
         for p in sorted(player.rglob('*')) if p.is_file()]
result = {'runtimeSource': stamp['revision'], 'dirty': stamp['dirty'], 'builtUtc': stamp['builtUtc'],
          'unity': '6000.3.25f1', 'transport': '2.7.4', 'configuration': 'Windows x64 Mono release D3D11',
          'archive': str(player), 'files': files, 'fileCount': len(files), 'totalBytes': sum(f['bytes'] for f in files)}
if args.zip:
    assert not args.zip.exists(), 'Do not replace an existing archive'
    with zipfile.ZipFile(args.zip, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as z:
        for file in files:
            z.write(player / file['path'], player.name + '/' + file['path'])
    result['zip'] = {'path': str(args.zip.resolve()), 'bytes': args.zip.stat().st_size,
                     'sha256': hashlib.sha256(args.zip.read_bytes()).hexdigest()}
args.output.parent.mkdir(parents=True, exist_ok=True)
args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps({k: v for k, v in result.items() if k != 'files'}))
