# Profiling and backend comparisons

The prototype keeps a managed reference path alongside Jobs/Burst simulation and culled rendering. Both run the same boss workload, tick rate, neighbor cap, projectile lifetime, collision tests, and capacity limits. Visibility never controls simulation activity.

## Pinned tools and implementation

Unity 6000.3.25f1, Burst 1.8.30, Collections 2.6.8, Mathematics 1.3.3, and URP 17.3.0. Burst/Collections/Mathematics were already resolved transitively; they are now explicit direct dependencies with the same exact versions. Jobs is part of Unity.

`BurstSimulation.cs` builds the spatial grid in a Burst job, then runs crowd steering/integration through `IJobParallelFor`, followed by a reduction of contact/check counts. Projectile movement and swept collision are parallel jobs, followed by a Burst compaction job. Descending swap-remove preserves projectile order. Cheap boss cooldown/state transitions and spawning remain in managed C#; the profiler exposes their cost separately.

Persistent native staging buffers retain the managed reference state for comparison. Timing includes copies, scheduling, reductions, and completion waits. Buffers are reused across resets and disposed when the simulation/presentation owner is destroyed. This is an incremental job migration, not an ECS rewrite. Jobs are completed before main-thread state is consumed. No job disables safety checks through attributes. Floating-point comparisons use tolerances; cross-platform bit-identical simulation is not promised.

`BurstInstances.cs` interpolates positions, performs conservative sphere/frustum checks, builds matrices, and compacts visible instances in stable order. Enemy bounds encompass both body and head. Rings and projectiles retain their meshes, materials, and sizes. Native matrices are submitted directly in batches of up to 500, versus 250 on the legacy path. This removes off-screen submissions, not enemies or attacks. There is no distance LOD or hidden collision reduction.

## Reproduce an A/B comparison

Use the owner's current i7-10700K / RTX 3090 desktop (approximately 64 GB RAM) for development benchmarks, retaining the existing 1080p configuration. GTX 1080 Ti and older-CPU testing is deferred until the game is more polished; see the updated [hardware plan](performance.md). Rebuild before new captures to include the updated report note. Existing binaries and archived reports retain the earlier hardware wording.

```powershell
.\tools\unity.ps1 -Action Test -TestPlatform EditMode
.\tools\unity.ps1 -Action Test -TestPlatform PlayMode
.\tools\unity.ps1 -Action Build

# Four combinations: managed/legacy, Burst/legacy, managed/culled, Burst/culled.
# Default: 30-second warm-up + 60-second capture, three repeats each (~18 minutes).
# Rotates combination order each repeat. Run only one player at a time.
.\tools\compare-backends.ps1 -Visible -Count 15000
.\tools\summarize-benchmarks.ps1 -InputDirectory 'path/to/comparison-reports' | Format-Table Mode,Crowd,AverageFps,P99Ms,SimulationKeptUp

# Individual controlled run:
.\tools\benchmark.ps1 -Visible -Counts 15000 -Repeats 3 -Simulation burst -Rendering culled -JobBatch 128
# Aligned ability spikes, not only staggered steady-state work:
.\tools\benchmark.ps1 -Visible -Counts 20000 -Repeats 1 -SynchronizedAbilities
```

The game defaults to Burst simulation and culled rendering. Menu toggles and CLI `-simulation managed|burst`, `-rendering legacy|culled`, and `-jobBatch 16..1024` select paths. The PowerShell helper validates these choices and verifies the report says Burst compiled; an actual job probe distinguishes native Burst execution from managed fallback. Hidden `-Smoke` validates functionality only.

Use identical resolution, machine, seed, population, abilities, and warm-up/capture durations. Keep the game visible and unobstructed, and do not compile or run competing benchmarks during measurement. Release-player frame-time reports are the performance evidence. Baselines that lose simulation time are failures; compare per-tick costs and simulated durations as well as FPS, because a slowed simulation executes less gameplay work per wall-clock second.

The capture window starts when both wall-clock and simulation warm-up complete, preventing startup stalls from shifting the attack phase substantially between runs. Reset also resets the frame clock so report-export time does not enter the next run. Schema v4 records `captureStartSimulationSeconds` and `captureEndSimulationSeconds`; check those alongside actual wall-clock capture duration. Simulation accumulates the same monotonic wall-clock intervals used for frame reporting. Catch-up remains capped at eight ticks per rendered frame; reaching that bound during capture is reported and invalidates acceptance.

## Stage timing in release players

`Journey.Simulation` contains the fixed-step loop and weapon work. Additional named markers cover `Journey.Crowd`, `Journey.Abilities`, `Journey.Projectiles`, `Journey.Weapon`, `Journey.RenderPrepare`, and `Journey.RenderSubmit`.

Schema v3 CSV contains per-frame stage times, tick counts, submitted instance counts, and instance batch counts. JSON adds average crowd/ability/projectile **milliseconds per simulation tick**, renderer **milliseconds per rendered frame**, backend names, job batch size, actual capture duration, and Burst compilation status. Copies and job completion waits belong to the measured stage. Stage times are elapsed wall time around work, not summed worker CPU consumption. Parent/child measurements overlap; do not add a parent to its children. Unity frame/GPU timings can lag the custom stage samples.

## Effect, scheduling, and native-state experiments

Schema v4 retains the v3 measurements and adds explicit experiment flags, `diagnosticOnly`, and per-frame/mean warning candidate and retained-warning counts (before frustum culling). Counts are zero when warning preparation is disabled. The summarizer reads both schemas and prevents hidden-effect diagnostics from passing its p99 budget check. The benchmark helper requires a rebuilt v4 player and validates each requested flag against the report.

```powershell
.\tools\unity.ps1 -Action Build
# Short exploration only: eight modes, 10s warm-up + 30s capture each.
.\tools\compare-experiments.ps1 -Visible -Repeats 1 -WarmupSeconds 10 -CaptureSeconds 30
# Full controlled comparison: 20,000 synchronized bosses, 3 x (30s + 60s) per mode.
.\tools\compare-experiments.ps1 -Visible -Modes control,combined
# Use -Staggered to check the ordinary ability distribution too.
# Individual experiments can be combined on benchmark.ps1:
.\tools\benchmark.ps1 -Visible -Counts 20000 -SynchronizedAbilities -DeduplicateWarnings -OverlapPreparation -NativeState
```

| Mode / helper flag | Change |
| --- | --- |
| `control` | Existing Burst simulation, managed state with staging copies, culled rendering, serial category preparation |
| `no-warnings` / `-HideWarnings` | Omit warning preparation and drawing; keep every cast, hit, and activation |
| `no-projectiles` / `-HideProjectiles` | Omit projectile preparation and drawing; keep every projectile and collision |
| `no-effects` | Both diagnostic omissions |
| `dedup` / `-DeduplicateWarnings` | Retain one warning per exact target position and remaining cast time |
| `overlap` / `-OverlapPreparation` | Separate enemy/projectile/warning buffers; schedule independent preparations before completing them |
| `native` / `-NativeState` | Own crowd/projectile motion state in persistent native arrays; render directly from those arrays |
| `combined` | Deduplication, overlapped preparation, and native ownership together |

All switches are opt-in; the control remains the default until measurements justify adoption. Rendering experiments require the culled renderer; native state requires Burst. Native ownership is selected on reset. Boss cooldown/state transitions, spawning, and hitscan remain managed C#; this is not a full ECS migration. Interpolation history still requires writes, and warnings still use a managed staging array. Native ownership removes redundant staging round trips, not all memory transfers.

Hidden-effect runs are visibly labeled and are diagnostic only. They measure the combined preparation/submission/GPU cost of a visual category, not an isolated GPU cost. They must never be presented as acceptance results or normal gameplay settings. Regular combat keeps dangerous effects visible even after a diagnostic benchmark.

Deduplication uses exact equality, preserves stable first-occurrence order, and does not merge nearby targets or distinct attack deadlines. Current rings share a fixed mesh, material, radius, and appearance. Extend the identity key or disable deduplication if future warnings introduce per-attack visuals, different damage types, or intensity cues. Every underlying attack still executes independently.

Parallel preparation uses additional persistent buffers and therefore trades memory for scheduling freedom. All preparation jobs complete before the next simulation update, buffer reuse, or disposal. `RenderPrepareMs` includes main-thread scheduling and completion waits but is not the sum of worker execution times; work may overlap submission and other preparation. The native path retains reference/staging allocations for A/B comparisons, so it is not a memory-minimized implementation.

Compare controls and experiments from the same build on the same desktop. Rotate order over repeated runs and inspect p99, maximum, budget misses, simulation time, dropped projectiles, and stage costs. Short screening captures are exploratory. Wall-clock capture boundaries can differ by a simulation tick; fixed-tick parity tests check correctness separately from FPS comparisons.

## Binary Unity Profiler captures

```powershell
# Separate output: build/StackLab-Development; leaves the release player intact.
.\tools\unity.ps1 -Action Build -Development

# Five seconds of binary tracing inside a short diagnostic capture.
.\tools\benchmark.ps1 -Visible -Profile -Counts 15000 -Repeats 1 -WarmupSeconds 5 -CaptureSeconds 10 -ProfileSeconds 5 -Simulation burst -Rendering culled -OutputDirectory 'prototypes/001-stacked-weapons/BenchmarkResults/profile-burst'

# Validate and summarize the binary capture without opening the editor UI:
.\tools\unity.ps1 -Action ProfileSummary -ProfilePath 'prototypes/001-stacked-weapons/BenchmarkResults/profile-burst/crowd-15000/profile.raw'
```

Load `profile.raw` in the Unity Profiler's CPU Timeline to inspect worker jobs and completion waits. The exporter creates `profile.summary.json` containing Journey markers, job waits, and graphics waits by thread, and fails if no readable markers exist. Summed worker/inclusive sample durations are not wall-clock frame latency. Development captures are marked and excluded from release acceptance; Deep Profiling is not enabled. Binary logs stay in ignored benchmark output because they can be large.

The exporter summarizes Unity's loaded frame window and records its first/last frame indices. Long or high-FPS recordings can exceed the editor's Profiler history limit; do not confuse the exported window with the entire raw recording. Inspect the raw trace in the Profiler with an appropriate history size when investigating events outside that window.

Unity references: [binary profiler logging](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.Profiler-enableBinaryLog.html), [raw frame data](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.RawFrameDataView.html), [instanced rendering and batch culling](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Graphics.RenderMeshInstanced.html).
