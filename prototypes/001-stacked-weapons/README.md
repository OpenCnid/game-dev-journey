# Experiment 001: stacked weapons

**Status:** one-weapon combat and crowd benchmark implemented. The remaining stacked-weapon and upgrade slices below are future work. See the repository's validation notes for measured results and limitations.

**Product direction update, October 2:** the [foundation](../../docs/foundation.md) now defines cooperative PvE for 2-4 players with two-player testing first, in an AI-controlled isekai world. This project currently implements only local one-player combat. Slices B/C and their global pause/progression/end rules below preserve the earlier local proposal; re-scope them after [world/encounter design](../../docs/planning/tasks/GDJ-6.md) and two-player feasibility. Their local checks do not establish multiplayer acceptance.

**Question:** does adding simultaneous weapons create satisfying power growth and distinct builds while keeping combat readable?

**Pinned tools:** Unity **6000.3.25f1** (`e1dba0a9aba4`), URP **17.3.0**, Input System **1.20.0**, Test Framework **1.6.0**. `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, and the Unity-generated `Packages/packages-lock.json` are authoritative. Transitive packages are locked too; do not regenerate the lockfile just to take newer versions.

Burst **1.8.30**, Collections **2.6.8**, and Mathematics **1.3.3** are pinned direct dependencies. Jobs/Burst simulation and culled instance rendering are enabled by default; the managed simulation and original renderer remain selectable for comparisons. See [profiling and A/B instructions](../../docs/profiling.md).

## Open and play

1. Install Unity Hub, sign in, and activate a valid Unity license.
2. Install [Unity 6000.3.25f1](https://unity.com/releases/editor/whats-new/6000.3.25f1) with Windows editor/player support. The initial build uses Mono and Direct3D 11; IL2CPP and Visual Studio build tools are not required for this slice.
3. Add **this folder**, `prototypes/001-stacked-weapons/`, as the Unity project in Hub. Do not add the repository root.
4. Open `Assets/Prototype/Scenes/Arena.unity` and press Play. Choose **Start / Restart Combat**.

WASD moves; the mouse aims; the rifle fires automatically. Escape pauses/releases the cursor; R restarts. Survive 60 seconds against 35 drones. Each drone takes two hits, then respawns at the perimeter to maintain crowd pressure. Contact damage has a shared 0.4-second grace period. This is a bounded flat arena with simple local steering and geometric hit detection.

Combat rifle hits resolve immediately against the displayed camera ray and interpolated enemy positions. Fixed simulation time owns the configured cadence (default 0.12 seconds); frames that only render cannot emit extra shots. A fresh tracer runs from the current recoiled muzzle to the resolved endpoint for that firing frame, while the white muzzle pulse lasts 45 ms. Later turns do not retain an old world line. Pause hides the transient tracer and freezes the other remaining cues; restart clears all of them. Catch-up shots resolve in order at one displayed pose. The synthetic benchmark consumes due shots per tick and retains its 45 ms legacy tracer for comparison. The existing spherical approximation remains; precise critical regions are future work. See [the aiming contract](../../docs/planning/tasks/GDJ-15.md).

The start/pause/end menu has **Rifle rate (shots/sec)**: enter a custom value and Apply, or use Slow 2/s, Baseline 8.33/s, Rapid 30/s and Stress 120/s. Rates from 0.5–120 are supported. The active rate appears in the top subtitle, affects combat and subsequent benchmark repeats, and survives R/restart for this session. Rate changes preserve fractional cooldown progress. These presets simulate firing intensity for testing; no progression balance or automatic weapon growth is implemented. The menu scrolls when controls exceed the available height. See [the rate contract](../../docs/planning/tasks/GDJ-19.md).

Normal-enemy HP bars appear only while wounded; aiming at a healthy enemy does not show a bar or reserve label space. Body brackets and cyan selected-HP decoration are removed. Aimed and background wounded bars share dark borders/amber fill, floating 0.45 metres above their posed heads with at least 12 logical HUD units of clearance above the highest projected head corner. Future boss types may explicitly opt into always-visible health; no combat boss exception is implemented yet. There is no central enemy-HP panel or retained defeated/recent-target display. Up to three additional wounded bars retain owner/incarnation slots across aim/distance changes, without duplicating the aimed enemy; overlap suppresses a label and requires 0.25 seconds of clear space before it returns. Up to 64 wounded/background candidates within 40 metres are considered; healthy background labels are hidden. The aimed enemy uses the existing read-only rifle-range query. A wounded body/core becomes squashed and skewed until death or restart; the existing spherical hit approximation stays unchanged. This simple visual cue is sufficient for the prototype, but future head/body critical regions must align with the visible pose. White hit markers and outer gold KILL markers can appear together, and death sparks are larger. A white muzzle pulse accompanies every shot. Player health loss produces red screen-edge pulses and a brief damage amount. Cues freeze on pause and clear on restart. This flat arena does not test bar occlusion or alter physical crowd spacing. Synthetic benchmarks omit the new combat cues and wounded pose.

The pause/start menu includes 144/240/custom/uncapped frame limits and a separate VSync setting. Benchmark mode temporarily disables both the cap and VSync, then restores them.

## Prepare, test, and build

From the repository root in PowerShell:

```powershell
.\tools\unity.ps1 -Action Prepare
.\tools\unity.ps1 -Action Test -TestPlatform EditMode
.\tools\unity.ps1 -Action Test -TestPlatform PlayMode
.\tools\unity.ps1 -Action Build
```

The helper locates the pinned editor in the standard Hub directory or the local install directory. Pass `-UnityPath 'C:\path\to\Editor\Unity.exe'` or set `UNITY_EDITOR_PATH` for another installation. Close any editor instance using this project before running batch commands.

`Prepare` creates missing scene/pipeline assets and applies the prototype's documented baseline settings. It preserves an existing arena scene. The same actions are available under the editor's **Journey** menu. Build output is `build/StackLab/StackLab.exe`; distribute its entire containing folder, not only the EXE. Logs and test XML go under `artifacts/`. Generated caches, builds, and captures are ignored by Git.

## Capture a crowd benchmark

The in-game menu runs 1,000, 10,000, 15,000, or 20,000 enemies with a scripted circular player route, continuous firing, and invulnerability. Select ordinary drones or the default synthetic boss workload (area attacks and projectile bursts); optionally synchronize attacks to stress spikes. Each choice runs three repeats of 30 seconds warm-up plus 60 seconds capture. Seed 137 and fixed 60 Hz simulation make comparisons repeatable without claiming cross-platform determinism.

From a standalone player (default sweep: 1,000 / 5,000 / 10,000 / 15,000 / 20,000 bosses, about 23 minutes):

```powershell
.\tools\benchmark.ps1 -Visible
# Focused regular/boss comparison and aligned attack spikes:
.\tools\benchmark.ps1 -Visible -Counts 10000,15000 -Workload regular
.\tools\benchmark.ps1 -Visible -Counts 10000,15000 -Workload bosses
.\tools\benchmark.ps1 -Visible -Counts 20000 -Workload bosses -SynchronizedAbilities
# Short verification of launch/capture/reporting, not performance acceptance:
.\tools\benchmark.ps1 -Smoke -Counts 10000,20000
# Slow/continuous firing checks (short hidden functional smoke, not performance):
.\tools\benchmark.ps1 -Smoke -Counts 35 -Workload regular -RifleRate 2
.\tools\benchmark.ps1 -Smoke -Counts 35 -Workload regular -RifleRate 120
```

The script reports output paths and saves invocation metadata. Full scripted captures require `-Visible`: keep the game window visible and unobstructed, since hidden or minimized windows can skip rendering and produce misleading throughput. Hidden `-Smoke` runs validate reporting only. In-game captures default to Unity's persistent data directory (`Application.persistentDataPath/BenchmarkResults`). Each repeat writes raw CSV samples and a JSON summary with the build revision/dirty flag, machine/settings, median/p95/p99/max frame times, budget misses, CPU/GPU timing availability, simulation time, and tick catch-up limit events. A shortened capture is marked nonstandard. Missing GPU timings remain unavailable rather than being interpreted as zero cost. FrameTimingManager samples can lag the current frame; raw timing columns are not a synchronized CPU/GPU trace.

Advanced player arguments:

```text
-benchmark -workload bosses -crowd 15000 -seed 137 -warmup 30 -seconds 60 -repeats 3
-results "C:\captures\run-001" -quitAfterBenchmark
```

Use `-playtest` to start combat immediately, `-rifleRate 30` to choose shots/second, or `-screenshot "C:\captures\arena.png"` to capture after three simulation seconds. Add `-screenshotAtMenu` for a startup-menu visual check instead. Standard Unity player arguments can set resolution. A benchmark contains rendering, steering, firing, hits, respawns, sound, and HUD; it has placeholder meshes, no skeletal animation, and only one weapon. It cannot validate the eventual fully upgraded game's peak load. Use the current desktop for development benchmarks; older-hardware coverage comes later.

`-workload regular` disables abilities; `-workload bosses` enables the per-enemy ability state machine. Add `-synchronizedAbilities` for the aligned spike test. Supported CLI counts are 0–20,000. JSON schema v5 and CSV include live projectile/telegraph counts, ability activations and update totals, projectile spawns/updates, actual and simulated capture duration, stage timings, backend selections, and pool overflow. Any dropped projectiles or catch-up limit events invalidate a performance pass. See [exact workload definitions and limitations](../../docs/design/late-game-workload.md).

## Code map

| Path under `Assets/Prototype/` | Responsibility |
| --- | --- |
| `Runtime/CombatSimulation.cs` | Preallocated enemy state, spatial buckets, movement, health, hits, cooldowns, run lifecycle |
| `Runtime/BossAbilities.cs` | Per-boss area/burst state machine, pooled projectile movement and swept collision, workload counters |
| `Runtime/BurstSimulation.cs` | Batched crowd and projectile jobs, deterministic grid/removal order, persistent staging buffers |
| `Runtime/BurstInstances.cs` | Burst interpolation, conservative frustum culling, matrix generation and stable compaction |
| `Runtime/StageProfile.cs` / `BurstStatus.cs` | Release stage timing and verification of native Burst execution |
| `Runtime/ArenaPresentation.cs` | Arena geometry, instanced drones, camera, rifle, procedural shot audio |
| `Runtime/CombatFeedback.cs` | Bounded health-bar selection, impact sparks, hit/kill markers and player damage cues; reads simulation outcomes |
| `Runtime/PrototypeGame.cs` | Input, fixed simulation stepping, menus, HUD, benchmark orchestration |
| `Runtime/BenchmarkCapture.cs` | Frame samples, percentile calculations, CSV/JSON export |
| `Editor/ProjectSetup.cs` | Reproducible setup, scene creation, shader references, Windows build |
| `Tests/` | Simulation/metric tests and runtime smoke test |

The selectable Jobs/Burst path parallelizes crowd and projectile updates. Boss state transitions, input, hit resolution, and submission orchestration remain managed C#. Local separation samples four rotating candidates per adjacent cell, bounding checks to 36 per enemy per tick. This approximates avoidance and allows dense overlap. Tick catch-up is bounded and reported; a run that reaches that bound cannot pass performance acceptance. Backend comparisons must use the same workload and record actual simulated time.

**Performance requirement:** target at least 144 FPS (6.944 ms per frame) in the supported encounter, with higher caps and uncapped rendering available. Follow the [performance plan](../../docs/performance.md): i7-10700K / RTX 3090 desktop, approximately 64 GB RAM, and the existing 1080p comparison configuration. GTX 1080 Ti and older-CPU testing is deferred until the game is more polished. Final visual settings and the supported peak load remain open; existing synthetic captures do not validate the full gameplay workload.

## Build in three slices

### A. One minute of combat

- A flat bounded arena with room to circle enemies and no navigation obstacles initially.
- Mouse look, keyboard movement, pointer capture/release, and pause.
- One enemy type that approaches, deals contact damage at a limited cadence, and can die.
- One aim-directed hitscan weapon that fires automatically on a timer.
- Health, clear hit/damage feedback, and restart after death.

**Check:** movement and aiming work; shots hit the intended target; contact damage cannot apply every rendered frame; pause freezes combat; restarting resets the encounter. Play for a minute before adding more systems.

### B. A visible increase in firepower

- Three independent weapon slots, initially using a single-shot, spread-shot, and piercing-shot behavior.
- Distinct cadence and feedback for each behavior, built from simple raycast attacks first.
- Debug controls to switch between one and three weapons in comparable encounters.
- Small on-screen slot indicators or placeholder models that leave the center of view clear.

**Check:** each weapon has its own cooldown; spread pellets cannot unintentionally hit the player; piercing counts each target once per ray and stops at walls; stacking has no unexplained shared cooldown. Compare readability and power with one versus three weapons.

### C. A five-minute build experiment

- Enemies drop energy pickups on defeat. A collector consumes each pickup at most once.
- Crossing a tunable energy threshold pauses the action and offers three distinct eligible upgrades.
- Six initial upgrade definitions: add spread weapon, add piercing weapon, increase single-shot damage, widen spread while adding pellets, increase piercing target count, shorten all firing intervals.
- Use initial caps of one purchase for each weapon addition and three purchases for each other upgrade. Weapon-specific upgrades require owning that weapon. Tune later; these are our rules.
- Offer only eligible choices, or all remaining choices if fewer than three exist. When none remain, stop offering upgrades and keep excess energy as score.
- If several thresholds are crossed together, process pending choices sequentially before resuming combat.
- Gradually increase spawn pressure within a hard active-enemy cap. Reaching the cap delays spawns instead of exceeding it.
- End at five minutes survived or zero health. If both occur in the same update, death takes precedence. Show time, kills, and loadout, then allow restart.

**Check:** paused time does not advance the run or spawn enemies; an upgrade applies once; invalid or capped choices never appear; the run cannot get stuck on an empty offer; restarting clears enemies, pickups, upgrades, timers, and counters.

**Build comparison:** use debug-selected loadouts and the same encounter settings to compare a wide crowd-clearing spread build with a focused piercing build. Then test earning upgrades during a normal run. Record whether the difference comes from an interesting strength or merely a larger damage total. Debug loadouts ensure the comparison is possible before tuning the random offer distribution.

## Proposed system responsibilities

These describe our implementation, not Vampire Hunters' internal architecture. Start with simple engine-native objects and direct references; add abstraction only where behavior warrants it.

| System | Owns | Boundary |
| --- | --- | --- |
| Player controller | Movement, look, aim origin, input capture | Does not schedule enemies or choose upgrades. |
| Weapon rack | Slots, per-weapon cooldowns, shot emission | Reads weapon definitions and effective stats. |
| Damage receiver | Health, hit response, one-time death notification | Prevents duplicate death rewards. |
| Enemy behavior | Approach movement, separation, contact attacks | Reuses damage handling where practical. |
| Spawn director | Run-time pressure curve, legal spawn positions, active cap | Uses configurable pacing; no unbounded spawn queue. |
| Pickup / progression | Collection, energy thresholds, pending upgrades | Keeps run progress separate from UI rendering. |
| Upgrade application | Eligibility, stack caps, effective stat calculation | Derives effects from base values and selected upgrades. |
| Run controller | Run state, clock, pause, end result, complete reset | Controls when gameplay systems may advance. |
| HUD and choice menu | Health, time, slots, offers, result display | Displays state and submits choices. |

Lifecycle: `ready -> running -> upgrade choice -> running -> won/lost -> ready`. Manual pause returns to its previous state. Menus do not consume gameplay time. Consume pending upgrades before returning to `running`.

Use simple approach steering on the open arena initially. Measure crowd bunching and frame time before adding obstacles, pathfinding, pooling, or more enemy behaviors. Prefer improving an observed bottleneck to building a generalized crowd framework.

## Parameters to keep easy to tune

Movement speed, player health, enemy health/speed/contact interval, each weapon's damage/cooldown/range/spread/piercing count, pickup value/radius, energy thresholds, upgrade caps, spawn cadence, active-enemy cap, and run duration.

Store definitions in the chosen engine's native data format. Label baseline values as our tuning choices. Record hardware, settings, build, and live counts with each capture. Benchmark regular crowds and ability-heavy bosses through 20,000 enemies alongside the three implementation slices, then test the most expensive supported weapon build. The owner wants late-game encounters above 10,000 active bosses; the final upper limit remains open and no supported performance count is yet established. Verify gameplay timing across frame caps and report frame-time spikes, not just average FPS.

## Evidence and exit decision

For each of three short playtests, record:

- Whether weapon additions are perceptible without a stats explanation.
- Whether different weapons visibly contribute and upgrade paths change strengths.
- Whether the player can identify danger and understand damage taken.
- Whether movement decisions remain useful after gaining power.
- Whether the player wants another run, and which combination they would try.
- Any hitching and the approximate enemy count when it occurs.

Use [playtest notes](../../docs/playtests/README.md). If weapons feel interchangeable, change their behavior before adding more. If combat becomes unreadable, reduce effects and crowd density before expanding scope. If the core is promising, document the result and promote selected work to `game/`.

**Result:** implementation and automated verification are recorded in the development journal; player enjoyment and target-hardware performance still require playtesting.

**Disposition:** first combat and benchmark slice implemented; next evaluate feel and crowded performance before adding simultaneous weapons.
