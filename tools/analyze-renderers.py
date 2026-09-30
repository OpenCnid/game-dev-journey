"""Summarize renderer captures, preserving invalid GPU timestamp counts.

Usage: python tools/analyze-renderers.py BenchmarkResults/render-screen
Frame intervals measure acceptance. Engine CPU/GPU timings arrive asynchronously.
Phase membership uses the preceding row's workload, an approximation, not a GPU trace.
"""
import csv
import json
import math
import statistics
import sys
from pathlib import Path


def stats(values):
    values = sorted(values)
    if not values:
        return None
    return {"samples": len(values), "mean": statistics.mean(values),
            "p99": values[math.ceil(len(values) * .99) - 1], "max": values[-1]}


def analyze(root):
    results = []
    for path in sorted(root.rglob("*-crowd*-r*.json")):
        report = json.loads(path.read_text(encoding="utf-8-sig"))
        with path.with_suffix(".csv").open(encoding="utf-8-sig", newline="") as source:
            rows = list(csv.DictReader(source))
        raw_gpu = [float(r["gpu_frame_ms"]) for r in rows if r["gpu_frame_ms"]]
        valid_gpu = [v for v in raw_gpu if math.isfinite(v) and 0 < v < 1000]
        phase = {"denseProjectiles": [], "warnings": [], "other": []}
        for previous, row in zip(rows, rows[1:]):
            key = "denseProjectiles" if int(previous["projectiles"]) > 40000 else (
                "warnings" if int(previous["telegraphs"]) > 5000 else "other")
            phase[key].append(float(row["frame_ms"]))
        results.append({"run": path.parent.parent.name, "report": path.name,
            "source": report["source"], "fps": report["averageFps"], "p99Ms": report["p99Ms"],
            "maxMs": report["maxMs"], "missPercent": 100 * report["framesOverBudget"] / report["samples"],
            "gpuMs": stats(valid_gpu), "rejectedGpuTimings": len(raw_gpu) - len(valid_gpu),
            "prepareMs": report["meanRenderPrepareMs"], "submitMs": report["meanRenderSubmitMs"],
            "batches": report["meanInstanceBatches"], "mainThreadMs": report.get("meanMainThreadMs"),
            "renderThreadMs": report.get("meanRenderThreadMs"),
            "phases": {key: stats(values) for key, values in phase.items()},
            "simulationKeptUp": report["catchupLimitFrames"] == 0 and
                report["droppedProjectilesIncludingWarmup"] == 0 and
                abs(report["actualCaptureSeconds"] - report["simulatedCaptureSeconds"]) < .05,
            "projectilesSpawned": report["projectilesSpawned"], "simulationTicks": report["simulationTicks"]})
    return results


if __name__ == "__main__":
    print(json.dumps(analyze(Path(sys.argv[1])), indent=2, allow_nan=False))
