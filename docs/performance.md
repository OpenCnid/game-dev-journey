# Performance target and benchmark plan

**Confirmed requirement:** target at least 144 FPS during supported gameplay within a declared hardware/settings/encounter envelope, including crowded fights with stacked weapons. Allow higher frame rates when the user's hardware supports them.

**Owner priority clarification, October 3:** the performant, responsive core comes before surrounding run structure. [Decision 0008](decisions/0008-bullet-heaven-solo-play-and-core-experience.md) names League of Legends/Valorant as aspirations for broad hardware reach, high FPS on capable rigs and a useful low-end experience. Exact minimum hardware, low-end settings/FPS and supported workloads remain open. Retain the desktop benchmark plan below; this adds no measured parity or validated support. Frame pacing, input response and network latency require separate evidence across 1-4 players. High FPS alone does not establish low ping.

**Late-game scope:** the owner wants more than 10,000 simultaneously active enemies, potentially all bosses with special abilities. The original 1,000-drone capture was only an initial diagnostic. It does not cover this requirement. The current stress harness supports up to 20,000 enemies; this is a test capacity, not an agreed final gameplay cap or a performance promise. See [the late-game workload](design/late-game-workload.md).

**Status:** benchmark all current development work on the owner's desktop: Intel Core i7-10700K, NVIDIA GeForce RTX 3090, and approximately 64 GB RAM. The owner revised the hardware plan on 2026-09-30; GTX 1080 Ti and older-CPU testing is deferred until the game is more polished. The 144 FPS goal remains, but the complete gameplay workload has not been validated. The prototype pins Unity 6000.3.25f1 and URP 17.3.0; see [the accepted engine decision](decisions/0001-engine-recommendation.md).

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

## Current development benchmark configuration

| Component | Current baseline |
| --- | --- |
| GPU | NVIDIA GeForce RTX 3090 |
| Resolution | Retain the existing 1920 x 1080 benchmark at 100% render scale for comparable captures |
| CPU | Intel Core i7-10700K; record actual clocks and power settings rather than assuming stock operation |
| RAM | Approximately 64 GB; record reported capacity and memory configuration |
| OS | Windows 11; record exact build and graphics driver with captures |
| Visual preset | Current prototype: shadows off; record explicit pipeline/effect settings and fix them across comparisons |
| Presentation | Visible window, uncapped, VSync off for throughput; test frame caps separately |
| Peak encounter | To be defined through the prototype's crowd and weapon benchmarks |

### Later hardware coverage

Once the game is more polished, benchmark representative gameplay on a GTX 1080 Ti and older-generation CPUs. The previously selected i7-8700K is a candidate for that later test matrix, not a current development requirement. Choose the exact configurations, settings, and supported encounter envelope at that stage; published minimum/recommended specifications remain undecided.

Earlier dated playtests, devlogs, and raw captures retain the hardware assumptions in force when they were recorded. They remain valid measurements of their recorded machines; their references to an older-hardware target are superseded by this plan.

Record the motherboard, BIOS/power settings, actual CPU clocks, memory configuration, and cooling condition with captures. Keep machine settings consistent across comparisons. Current desktop results establish our development baseline; older-hardware conclusions will require measurements on those machines later. The supported workload still needs explicit limits before the 144 FPS goal can be validated.

Record all of these before declaring the target met:

- CPU, GPU, RAM, OS, and graphics driver.
- Resolution, render scale, graphics preset, display refresh rate, and synchronization settings.
- Engine/version, commit, build configuration, and measurement tool.
- Supported peak enemy count, active weapon loadout and upgrade levels, projectile/pickup counts, and effect density.

Avoid promises about every PC or an unlimited number of enemies. Define an encounter envelope that still delivers the intended horde experience; measure that envelope on the selected hardware. Record any change to the envelope or visual preset explicitly so it cannot hide a regression.

## Benchmark alongside the first prototype

1. Establish an empty-arena baseline in a standalone build.
2. Compare regular crowds, ability-heavy bosses, and synchronized boss attacks at 1,000, 5,000, 10,000, 15,000, and 20,000 enemies. Use 100/500 for quick regressions and an empty scene for overhead. The 15,000/20,000 scenarios exercise the requirement to exceed 10,000; the exact supported upper bound remains open.
3. Add movement and avoidance, then weapon targeting/damage, then animation and visual/audio feedback. Measure each increment to locate growing costs.
4. Stress the most expensive supported build: maximum firing rates, spread/piercing, spawn/death bursts, pickups, damage feedback, and repeated restarts. Track actual live counts as enemies die or spawn.
5. Use a recorded input route or scripted camera and a fixed random seed where supported. Keep the scenario comparable across commits; a seed alone does not guarantee identical execution.
6. For repeatable steady-state captures, warm up for 30 seconds and capture at least 60 seconds, three times per scenario. Also capture a full five-minute run and cold-start/first-use events separately; do not hide combat stutters through warm-up.

Use representative standalone release builds for acceptance and profiled builds to diagnose causes. Record profiling overhead and frame-cap/VSync settings. Measure uncapped throughput separately from presentation with the chosen cap. An editor result or a scene of idle enemy meshes does not validate the playable crowd.

## Reporting and proposed acceptance checks

Report each capture's frame-time median, p95, p99, maximum, and count of frames over 6.944 ms and 13.889 ms. Keep raw traces when useful and describe the scene at spikes. Average FPS alone is insufficient.

Proposed initial gate: p99 frame time at or below the 144 FPS budget in every required uncapped gameplay capture on the agreed baseline configuration, with every repeatable hitch investigated. This percentile gate is a diagnostic threshold, not a claim that every frame meets a hard minimum. Report slower frames explicitly. Refine acceptance once we have baseline measurements; do not silently relax the owner's 144 FPS target.

Also verify correct gameplay timing across frame caps, smooth camera/movement presentation, and complete state reset. A fast crowd that breaks weapon behavior is not a passing result.

Boss captures must record the workload version, boss count, projectile/telegraph peaks, ability activations, projectile spawns, ability/projectile update counts, simulated capture duration, and dropped projectiles. Any dropped projectiles or simulation catch-up limit events invalidate an acceptance pass. Check ability activity against the configured cadence: removing attacks or skipping simulation is not an optimization pass. Sampled peaks are end-of-frame observations, not exact within-tick maxima.

When a scenario fails, identify the limiting CPU task, GPU workload, allocation spike, or synchronization wait before changing architecture. Candidate techniques include batched crowd updates, local spatial queries, parallel work, reused objects, and cheaper distant visuals/decisions. Apply and measure them where evidence supports the cost; no particular engine or architecture guarantees the target.

## Results

See [initial prototype validation](playtests/2026-09-30-prototype-performance.md). Hidden-window smoke captures validate launch/export only. Full rendering captures must keep the player visible and unobstructed; the CLI helper requires `-Visible` for those runs. GPU timings may be unavailable and are explicitly marked as such in the JSON report.

The later [boss-scale diagnostic](playtests/2026-09-30-boss-scale.md) supersedes the 1,000-drone workload as the relevant direction for optimization. It does not meet the performance target.

[Jobs/Burst and rendering comparisons](playtests/2026-09-30-jobs-burst.md) measure the next implementation against the selectable managed reference on our now-selected desktop baseline. Follow [the profiling guide](profiling.md) to reproduce stage measurements and binary traces. The optimized 15,000-boss synthetic captures meet the proposed p99 threshold, with occasional slower frames; the 20,000 synchronized-boss capture misses it. Representative final gameplay remains to be validated.

[Frame-spike experiments](playtests/2026-09-30-frame-spike-experiments.md) investigate the remaining 20,000-boss costs with independently selectable modes. Hidden-effect diagnostics preserve gameplay but omit visuals and cannot count as acceptance. Current captures also align simulation warm-up and record simulation phase to avoid startup delays shifting the workload between comparisons.

The later [renderer experiments](playtests/2026-09-30-renderer-experiments.md) meet the proposed p99 gate in all three 20,000-synchronized-boss captures with compact projectiles, combined character meshes, and cached deduplicated warnings: 3.743–4.297 ms p99 on the current desktop. Two of 77,380 frames exceeded budget; the worst was 7.502 ms. This supersedes the earlier failure for this particular synthetic configuration, not the final-game performance requirement.
