# Performance target and benchmark plan

**Confirmed requirement:** target at least 144 FPS during supported gameplay, including crowded fights with stacked weapons. Allow higher frame rates when the user's hardware supports them.

**Status:** requirement and proposed measurement plan only. No game build exists and no performance results have been collected. The owner suggested a GTX 1080 Ti as an older-hardware reference; use it as the provisional GPU baseline. CPU, final resolution, graphics preset, and supported peak encounter remain to be selected. Engine selection is still open.

## Frame budget

| Frame rate | Time per frame |
| --- | --- |
| 144 FPS baseline | 6.944 ms |
| 165 FPS | 6.061 ms |
| 240 FPS | 4.167 ms |
| 360 FPS | 2.778 ms |

These are budgets calculated as `1000 / FPS`, not measured capabilities. The higher rates are supported user choices, not baseline performance promises.

The target concerns natively rendered gameplay frames. Frame generation does not count toward meeting the baseline. Measure CPU frame work and GPU time separately alongside actual frame intervals. CPU and GPU work may overlap; do not treat their times as a simple sum. Leave headroom below the frame budget for variability.

## Player-facing behavior

- Provide a frame limiter with 144 FPS, higher presets, a custom cap, and an uncapped option.
- Expose VSync separately; actual presentation is also constrained by the display, driver, and synchronization settings.
- Keep movement, damage, firing cadence, spawn pacing, and run duration independent of rendered FPS.
- Choose and document a simulation tick rate separately from rendering. Use interpolation where appropriate and update camera/input presentation responsively at high render rates.
- Verify gameplay at 60, 144, and 240 FPS plus uncapped operation. Lower test rates check correctness; they do not lower the 144 FPS baseline target.
- Offer scalable visual costs such as shadows and effects without silently changing combat outcomes.

## Baseline configuration to agree

| Component | Current baseline |
| --- | --- |
| GPU | NVIDIA GeForce GTX 1080 Ti, provisionally selected by the owner |
| Resolution | 1920 x 1080 at 100% render scale, proposed starting point, not yet confirmed |
| CPU and RAM | Not selected; essential to validating crowded combat |
| Visual preset | To be defined and fixed for comparisons |
| Peak encounter | To be defined through the prototype's crowd and weapon benchmarks |

The CPU and workload also need explicit limits before we can validate the complete system. Test on an actual GTX 1080 Ti when checking this baseline; a result from a faster GPU does not establish performance on the target.

Record all of these before declaring the target met:

- CPU, GPU, RAM, OS, and graphics driver.
- Resolution, render scale, graphics preset, display refresh rate, and synchronization settings.
- Engine/version, commit, build configuration, and measurement tool.
- Supported peak enemy count, active weapon loadout and upgrade levels, projectile/pickup counts, and effect density.

Avoid promises about every PC or an unlimited number of enemies. Define an encounter envelope that still delivers the intended horde experience; measure that envelope on the selected hardware. Record any change to the envelope or visual preset explicitly so it cannot hide a regression.

## Benchmark alongside the first prototype

1. Establish an empty-arena baseline in a standalone build.
2. Add repeatable crowd scenarios at 100, 500, and 1,000 enemies. These are exploration loads, not commitments to a supported count.
3. Add movement and avoidance, then weapon targeting/damage, then animation and visual/audio feedback. Measure each increment to locate growing costs.
4. Stress the most expensive supported build: maximum firing rates, spread/piercing, spawn/death bursts, pickups, damage feedback, and repeated restarts. Track actual live counts as enemies die or spawn.
5. Use a recorded input route or scripted camera and a fixed random seed where supported. Keep the scenario comparable across commits; a seed alone does not guarantee identical execution.
6. For repeatable steady-state captures, warm up for 30 seconds and capture at least 60 seconds, three times per scenario. Also capture a full five-minute run and cold-start/first-use events separately; do not hide combat stutters through warm-up.

Use representative standalone release builds for acceptance and profiled builds to diagnose causes. Record profiling overhead and frame-cap/VSync settings. Measure uncapped throughput separately from presentation with the chosen cap. An editor result or a scene of idle enemy meshes does not validate the playable crowd.

## Reporting and proposed acceptance checks

Report each capture's frame-time median, p95, p99, maximum, and count of frames over 6.944 ms and 13.889 ms. Keep raw traces when useful and describe the scene at spikes. Average FPS alone is insufficient.

Proposed initial gate: p99 frame time at or below the 144 FPS budget in every required uncapped gameplay capture on the agreed baseline configuration, with every repeatable hitch investigated. This percentile gate is a diagnostic threshold, not a claim that every frame meets a hard minimum. Report slower frames explicitly. Refine acceptance once we have baseline measurements; do not silently relax the owner's 144 FPS target.

Also verify correct gameplay timing across frame caps, smooth camera/movement presentation, and complete state reset. A fast crowd that breaks weapon behavior is not a passing result.

When a scenario fails, identify the limiting CPU task, GPU workload, allocation spike, or synchronization wait before changing architecture. Candidate techniques include batched crowd updates, local spatial queries, parallel work, reused objects, and cheaper distant visuals/decisions. Apply and measure them where evidence supports the cost; no particular engine or architecture guarantees the target.

## Results

None yet. Save future benchmark reports under `docs/playtests/` with a `performance` suffix, link their build and scenario configuration, and add them here.
