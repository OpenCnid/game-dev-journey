# Frame-spike experiment evidence

Files prefixed `prealignment-` are exploratory captures from source `029542486cf3ff26d6079cedcdb41fa4e57eacc3`, before the simulation warm-up alignment fix. They are retained for audit, not used as the final controlled comparison. Startup catch-up losses shifted the gameplay phase: the control recorded 320,000 projectile spawns while other 30-second captures recorded 399,984. Their equal wall-clock durations did not ensure equivalent attack phases.

Subsequent captures wait for both simulation and wall-clock warm-up and report start/end simulation times. Invocation metadata records the mode flags and visible-window setting. Hidden-effect modes are diagnostic only; they preserve gameplay simulation but omit a category's rendering preparation and drawing.

## Aligned results

- `screen-r1-*.json`: eight aligned 10-second warm-up / 30-second screening captures, one per mode.
- `full-r1-*.json`, `full-r2-*.json`, `full-r3-*.json`: nine aligned 30-second warm-up / 60-second captures for control, native ownership, and the combined mode. Outer repeat is in the filename; each fresh player internally records repeat 1.
- Matching `*-invocation.json` files record requested flags and capture settings.
- [aligned-frame-traces.zip](aligned-frame-traces.zip): all 17 aligned raw frame CSV files, named to match the summaries.
- [phase-analysis.json](phase-analysis.json): intervals grouped by the preceding frame's workload (>40,000 projectiles; otherwise >5,000 warnings; otherwise other). This is approximate phase association, not GPU causality.
- [test-summary.json](test-summary.json): 25 EditMode and two PlayMode tests passed for the corrected runtime source.
- [graphics-driver.json](graphics-driver.json): installed graphics driver metadata; the player reports which GPU it used. The active Windows power plan was Balanced.
- `visual-smoke.json`, its invocation, and [combined-visual.png](combined-visual.png): separate visual smoke check, excluded from performance conclusions.

All 17 aligned performance captures used clean source `b44fce7`, verified native Burst compilation, and preserved simulation time with zero projectile drops and zero catch-up-limit frames. The eight short captures each spawned 400,000 projectiles; the nine full captures each spawned 720,000. Actual ticks and counters remain in each JSON. No fully visible configuration met the p99 target.

Read [the experiment report](../../2026-09-30-frame-spike-experiments.md) for interpretation and limits.
