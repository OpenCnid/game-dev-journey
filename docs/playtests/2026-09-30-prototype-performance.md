# First prototype: build and performance validation

**Date:** 2026-09-30. **Purpose:** verify the first playable Windows build, automated checks, and crowd capture path. This is not a player enjoyment study or validation on the target i7-8700K / GTX 1080 Ti.

## Build and checks

- Unity 6000.3.25f1, URP 17.3.0, Input System 1.20.0, Test Framework 1.6.0; transitive versions in the committed lockfile.
- Windows x64 standalone release build succeeded, Mono backend, Direct3D 11.
- **12 Edit Mode cases passed:** deterministic reset/stepping, pause, frame-rate-independent movement/firing, damage cooldown, kill/respawn, run ending/restart, ray intersection edge cases, and percentile calculations.
- **1 Play Mode case passed:** create the arena, start combat, emit shots, restart, and exit without unexpected runtime errors.
- Hidden-window smoke captures at 100, 500, and 1,000 enemies completed and wrote reports. Their high throughput and missing GPU timings are not treated as rendering-performance evidence.
- A visible screenshot was inspected for the arena, enemies, rifle/tracer, HUD, and benchmark status.

## Visible developer-machine capture

| Setting | Value |
| --- | --- |
| CPU | Intel Core i7-10700K |
| GPU | NVIDIA GeForce RTX 3090 |
| Reported system memory | 65,358 MB |
| Output | 1920 x 1080, windowed, native render scale |
| Scene | 1,000 instanced graybox drones, flat arena, one rifle, local separation |
| Rendering | URP, no dynamic shadows, no MSAA; Unity quality label remains `Ultra`, with prototype overrides applied |
| Simulation | Fixed 60 Hz, seed 137, scripted orbit, autofire, invulnerable player |
| Pacing | Uncapped, VSync off |
| Duration | 30-second warm-up, 60-second capture, one repeat |

| Metric | Observed |
| --- | --- |
| Samples | 35,126 |
| Average FPS | 585.45 |
| Median frame time | 1.342 ms |
| p95 frame time | 4.078 ms |
| p99 frame time | 5.419 ms |
| Maximum frame time | 8.884 ms |
| Frames over 6.944 ms | 15 |
| Frames over 13.889 ms | 0 |
| Frames hitting the simulation catch-up limit | 0 |
| Shots / kills during capture | 500 / 251 |

GPU timings were available for 30,267 of the 35,126 samples. FrameTimingManager measurements can be delayed relative to frame interval samples; they are diagnostic values, not synchronized traces. No runtime errors appeared in the player log.

The capture came from the initial implementation worktree based on commit `2a6f1c5`, explicitly marked dirty in the report. Subsequent packaging/documentation work preserves this report as initial evidence rather than rewriting its build stamp. The final deliverable is rebuilt from the committed implementation.

Evidence: [JSON summary](evidence/2026-09-30-visible-1000.json), [screenshot](evidence/2026-09-30-arena.png). Full raw CSV and logs remain in the local ignored `prototypes/001-stacked-weapons/BenchmarkResults/visible-1000/` directory. Commands and repeat settings are in [the prototype README](../../prototypes/001-stacked-weapons/README.md).

## Interpretation and next checks

The simple scene runs and the metrics distinguish hidden from visible execution. The 15 slower frames mean even this capture is not a hard 144 FPS minimum. One repeat on a faster PC with placeholder meshes, no skeletal animation, and one weapon does not establish performance on the baseline or in the planned full encounter.

Next: playtest movement/aiming, repeat measurements on the actual baseline hardware, investigate repeatable frame-time spikes, and measure the cost of each additional weapon and visual system. A full acceptance session still needs three repeats per required scenario and a full gameplay run as defined in [the performance plan](../performance.md).
