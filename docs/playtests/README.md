# Playtests

Copy [the session template](template.md) to `YYYY-MM-DD-short-title.md` for a useful play session. Record which build or commit was tested so observations can be reproduced.

Observe before explaining. Note where a player hesitates, what they try, and whether they can recover or restart. Ask open questions afterward. Separate what actually happened from our interpretation of why it happened.

Use anonymous participant labels and leave personal information out of the repository.

## Records

- [2026-10-04: Rifle fire-rate controls](2026-10-04-rifle-rate-controls.md) - custom 0.5–120/s and four presets for combat/benchmarks; cadence/reset/input/render/startup checks and slow/baseline/stress report counts pass; developer rate comparison remains open.
- [2026-10-04: Rifle shot alignment during fast turns](2026-10-04-shot-alignment.md) - stale muzzle/retained line reproduced; displayed-pose hits and one-frame tracers pass checks/build/startup; P1 accepts smooth, undelayed aiming.
- [2026-10-03: Overhead HP without cyan brackets](2026-10-03-overhead-health.md) - P1 accepts on October 4 and authorizes merge; clear head gap/consistent wounded colors, head-clearance/visibility/render checks pass; GDJ-15 selected next.
- [2026-10-03: Normal-enemy HP only after damage](2026-10-03-damaged-only-health.md) - 49 checks; P1 rejects overlapping cyan brackets/HP and head-level placement, prompting higher overhead bars.
- [2026-10-03: Body brackets without central HP panels](2026-10-03-aimed-health-feedback.md) - 49 checks; P1 rejected full cyan HP on healthy normal targets, prompting damaged-only visibility.
- [2026-10-03: Fixed target HP and retained wounded bars](2026-10-03-focused-health-feedback.md) - 49 passes; P1 liked body brackets but rejected the central defeated/HP panel as distracting at high kill rates.
- [2026-10-03: Attached-bar readability revision](2026-10-03-damage-readability-revision.md) - 48 automated passes; P1 rejected target ownership/clutter/flicker and prompted fixed target HP with retained wounded owners.
- [2026-10-03: First damage-readability candidate](2026-10-03-damage-readability.md) - 42 automated passes; P1 identified wounds/player damage and requested improved enemy effects/stacked bars, then found the intermediate floating-bar layout unclear.
- [2026-10-03: Current rifle baseline](2026-10-03-rifle-baseline.md) - exact candidate, 35 current test passes, two active developer attempts and pause/reset confirmation; selects enemy health/damage readability before stacking.
- [2026-09-30: First prototype validation](2026-09-30-prototype-performance.md) - automated tests and developer-machine benchmark diagnostics; not a user playtest.
- [2026-09-30: Boss-scale stress testing](2026-09-30-boss-scale.md) - larger synthetic workloads and a failed performance baseline.
- [2026-09-30: Jobs/Burst and rendering comparison](2026-09-30-jobs-burst.md) - controlled backend comparisons, stage costs, and profiler evidence.
- [2026-09-30: Frame-spike experiments](2026-09-30-frame-spike-experiments.md) - effect isolation, exact warning deduplication, preparation scheduling, native ownership, and corrected warm-up alignment.
- [2026-09-30: Renderer experiments](2026-09-30-renderer-experiments.md) - compact indirect projectiles, combined character meshes, cached warnings, and CPU/GPU profiling against native-state simulation.
