# Renderer experiment evidence

See [the report](../../2026-09-30-renderer-experiments.md) for interpretation and workload limits.

- `screen-*`: eight short visible release comparisons from clean source `c5ed740`, 10s warm-up / 30s capture.
- `full-*`: three rotated repeats of four configurations, 30s warm-up / 60s capture, from clean source `8fa326a`. Each fresh player reports internal repeat 1; the `r1`/`r2`/`r3` filename prefix identifies the outer repeat.
- `profile-*`: separate development captures and exported CPU/GPU hierarchy summaries from player source `c5ed740`. GPU summaries were exported with the tool in `8fa326a`. Five-second CSV capture and two-second binary profile are different windows. Inclusive GPU/CPU markers overlap and must not be added together.
- `visual-*` and `combined-visual.png`: separate five-second smoke check; not acceptance evidence.
- `test-summary.json`: 33 edit-mode tests and two play-mode tests passed. Play-mode rendering exercises all four projectile modes, combined characters, overlapping preparation, native state, warning caching/deduplication, and hidden diagnostic combinations.
- `screen-analysis.json` / `full-analysis.json`: summaries from `tools/analyze-renderers.py`, including corrected valid GPU means and approximate workload phases. Phase assignment uses the previous CSV row and does not align delayed GPU timings exactly.
- `frame-traces.zip`: original frame CSVs, organized by source capture folder.
- `profile-manifest.json`: paths, sizes, and hashes for local binary traces. Large raw Unity profile files remain in ignored `BenchmarkResults` output; their exported summaries are committed here.

The original indirect-octahedron screening JSON contains a corrupted mean GPU time caused by one disjoint timestamp query. It is retained unmodified. Use its raw CSV and `screen-analysis.json` for corrected GPU statistics. Validation added in `8fa326a` excludes invalid engine durations from summary means and counts rejected samples; frame intervals and percentile results are unaffected. Missing engine timings do not mean the GPU did no work.

No attacks are removed in these renderer experiments. Compare simulation ticks, projectile spawns, dropped-projectile counters, and catch-up limits alongside frame times. These traces characterize a procedural synthetic workload on one current desktop, not the final game or older hardware.
