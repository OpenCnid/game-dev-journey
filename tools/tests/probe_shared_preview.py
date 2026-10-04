"""Measure actual frames from both running local MJPEG streams, without OS input."""
import argparse
import hashlib
import json
import time
import urllib.request
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from urllib.parse import urlparse


def measure(base, role, seconds):
    with urllib.request.urlopen(base + role + "/stream", timeout=5) as response:
        start = time.perf_counter()
        frames = changed = total_bytes = 0
        previous = None
        while time.perf_counter() - start < seconds:
            if response.readline().strip() != b"--frame":
                raise ValueError("Invalid multipart boundary")
            headers = {}
            while True:
                line = response.readline().strip()
                if not line:
                    break
                key, value = line.split(b":", 1)
                headers[key.lower()] = value.strip()
            size = int(headers[b"content-length"])
            if not 0 < size <= 4 * 1024 * 1024:
                raise ValueError("Invalid frame size")
            frame = response.read(size)
            if len(frame) != size or not frame.startswith(b"\xff\xd8") or not frame.endswith(b"\xff\xd9"):
                raise ValueError("Invalid JPEG frame")
            if response.read(2) != b"\r\n":
                raise ValueError("Invalid frame delimiter")
            digest = hashlib.sha256(frame).hexdigest()
            changed += digest != previous
            previous = digest
            total_bytes += size
            frames += 1
        elapsed = time.perf_counter() - start
    return {"role": role, "frames": frames, "changedFrames": changed, "seconds": elapsed,
            "deliveredFps": frames / elapsed, "bytes": total_bytes, "lastFrameSha256": previous}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("session", type=Path)
    parser.add_argument("--seconds", type=float, default=4)
    args = parser.parse_args()
    invocation = json.loads((args.session / "invocation.json").read_text())
    base = invocation["viewer"]
    parsed = urlparse(base)
    if parsed.scheme != "http" or parsed.hostname != "127.0.0.1" or not 1 <= args.seconds <= 10:
        parser.error("Expected a local preview and a 1..10 second measurement.")
    with ThreadPoolExecutor(max_workers=2) as pool:
        reports = list(pool.map(lambda role: measure(base, role, args.seconds), ["host", "client"]))
    report = {"targetFps": 30, "players": reports, "performanceAcceptanceEligible": False}
    (args.session / "stream-verification.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))
    if min(r["deliveredFps"] for r in reports) < 20:
        raise RuntimeError("Delivered rate is below the smooth-preview check.")


if __name__ == "__main__":
    main()
