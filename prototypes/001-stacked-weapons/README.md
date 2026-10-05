# Experiment 001: stacked weapons

**Status:** one-weapon combat, fixed solo/two-player earned-growth candidate and crowd benchmark implemented. The remaining stacked-weapon and upgrade slices below are future work. See the repository's validation notes for measured results and limitations.

**Product direction update, October 2:** the [foundation](../../docs/foundation.md) now defines cooperative PvE for 2-4 players with two-player testing first, in an AI-controlled isekai world. This project currently implements only local one-player combat. Slices B/C and their global pause/progression/end rules below preserve the earlier local proposal; re-scope them after [world/encounter design](../../docs/planning/tasks/GDJ-6.md) and two-player feasibility. Their local checks do not establish multiplayer acceptance.

**Question:** does adding simultaneous weapons create satisfying power growth and distinct builds while keeping combat readable?

**Pinned tools:** Unity **6000.3.25f1** (`e1dba0a9aba4`), URP **17.3.0**, Input System **1.20.0**, Test Framework **1.6.0**. `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, and the Unity-generated `Packages/packages-lock.json` are authoritative. Transitive packages are locked too; do not regenerate the lockfile just to take newer versions.

Burst **1.8.30**, Collections **2.6.8**, and Mathematics **1.3.3** are pinned direct dependencies. Jobs/Burst simulation and culled instance rendering are enabled by default; the managed simulation and original renderer remain selectable for comparisons. See [profiling and A/B instructions](../../docs/profiling.md).

## Open and play

1. Install Unity Hub, sign in, and activate a valid Unity license.
2. Install [Unity 6000.3.25f1](https://unity.com/releases/editor/whats-new/6000.3.25f1) with Windows editor/player support. The initial build uses Mono and Direct3D 11; IL2CPP and Visual Studio build tools are not required for this slice.
3. Add **this folder**, `prototypes/001-stacked-weapons/`, as the Unity project in Hub. Do not add the repository root.
4. Open `Assets/Prototype/Scenes/Arena.unity` and press Play. Choose **Start / Restart Combat**.

WASD moves; the mouse aims; the rifle fires automatically. Escape pauses/releases the cursor; R restarts. Survive 60 seconds against 35 drones. Two body hits (20 damage each) or one fresh head hit (40 damage) kill a 40-HP drone, which respawns at the perimeter to maintain crowd pressure. Contact damage has a shared 0.4-second grace period. This is a bounded flat arena with simple local steering and geometric hit detection.

Combat rifle hits resolve immediately against the displayed camera ray and interpolated enemy positions. Fixed simulation time owns the configured cadence (default 0.12 seconds); frames that only render cannot emit extra shots. A fresh tracer runs from the current recoiled muzzle to the resolved endpoint for that firing frame, while the white muzzle pulse lasts 45 ms. Later turns do not retain an old world line. Pause hides the transient tracer and freezes the other remaining cues; restart clears all of them. Catch-up shots resolve in order at one displayed pose. The synthetic benchmark consumes due shots per tick and retains its 45 ms legacy tracer for comparison. See [the aiming contract](../../docs/planning/tasks/GDJ-15.md).

One read-only nearest-surface query serves target preview and firing. Combat body/head regions match the visible healthy and wounded cube transforms, including their shear; the first intersected surface wins, with head precedence only for exact body/head ties. The reticle is green over a body, gold over a head and white on a miss, including during cooldown. A short **CRIT ×2** cue accompanies an actual head hit independently of the existing hit/KILL cues. Misses still consume due shots; there is no aim assist or target-gated autofire. Static appearance customization is separate future work. Benchmark mode explicitly retains its fixed-state sphere and body-only 20 damage, without wounded poses or critical feedback, preserving the reference workload. The combined-character experiment draws only in benchmark mode; combat uses paired meshes to preserve region alignment. See [the precision contract](../../docs/planning/tasks/GDJ-17.md).

The start/pause/end menu has **Rifle rate (shots/sec)**: enter a custom value and Apply, or use Slow 2/s, Baseline 8.33/s, Rapid 30/s and Stress 120/s. Rates from 0.5–120 are supported. The active rate appears in the top subtitle, affects combat and subsequent benchmark repeats, and survives R/restart for this session. Rate changes preserve fractional cooldown progress. These presets simulate firing intensity for testing; no progression balance or automatic weapon growth is implemented. The menu scrolls when controls exceed the available height. See [the rate contract](../../docs/planning/tasks/GDJ-19.md).

Normal-enemy HP bars appear only while wounded; aiming at a healthy enemy does not show a bar or reserve label space. Body brackets and cyan selected-HP decoration are removed. Aimed and background wounded bars share dark borders/amber fill, floating 0.45 metres above their posed heads with at least 12 logical HUD units of clearance above the highest projected head corner. Future boss types may explicitly opt into always-visible health; no combat boss exception is implemented yet. There is no central enemy-HP panel or retained defeated/recent-target display. Up to three additional wounded bars retain owner/incarnation slots across aim/distance changes, without duplicating the aimed enemy; overlap suppresses a label and requires 0.25 seconds of clear space before it returns. Up to 64 wounded/background candidates within 40 metres are considered; healthy background labels are hidden. The aimed enemy uses the shared read-only rifle-range query. A wounded body/core becomes squashed and skewed until death or restart; its precision regions follow those same matrices. White hit markers and outer gold KILL markers can appear together, and death sparks are larger. A white muzzle pulse accompanies every shot. Player health loss produces red screen-edge pulses and a brief damage amount. Cues freeze on pause and clear on restart. This flat arena does not test bar occlusion or alter physical crowd spacing. Synthetic benchmarks omit the new combat cues and wounded pose.

The pause/start menu includes 144/240/custom/uncapped frame limits and a separate VSync setting. Benchmark mode temporarily disables both the cap and VSync, then restores them.

## Two-player shared rifle feasibility (GDJ-20 candidate)

The main menu also offers **Host (A)** and **Join (B)**. Direct IPv4 UDP port 7777, fixed two-player roster, 12 shared 60-HP drones, one baseline 20/40-damage rifle each, 60 seconds. Host owns damage, incarnations, personal health/contact grace and endings. No friendly obstruction/damage. One death follows the living teammate's view and returns after five active seconds with one second of visible contact immunity. All-dead defeat precedes return/completion. After defeat/completion both players click Retry (or R); R does nothing during shared combat. Escape/focus loss releases local input while the encounter continues. Click **Resume local input** to resume. Connection loss aborts; restart both processes to form a fresh pair.

Use two separate games on this desktop without a second person/machine:

```powershell
.\tools\shared-combat.ps1                        # both manual; switch windows
.\tools\shared-combat.ps1 -ScriptedCompanion     # human host A, automated client B
.\tools\shared-combat.ps1 -ScriptedCompanion -HumanRole client
.\tools\shared-combat.ps1 -Scripted -QuitAtEnd -Uncapped  # actual shared posed combat capture
```

Run these from the repo root after building. `-PlayerFolder` selects an archived whole player; `-Results`, `-Port`, `-Width`, `-Height` override paths/settings. Launch only one pair capture at a time. The automated companion is labelled diagnostic input in evidence, not a human teammate observation. CLI equivalents are `-host` / `-join 127.0.0.1`, `-sharedScripted`, `-sharedUncapped`, `-sharedQuit`, `-sharedResults <path>`. The new capture writes per-frame shared counters, attributed hit/rejection events and a terminal source/settings summary. Frame distributions omit the first three active seconds; hit delay is software emission-to-confirmed-feedback, not a physical input/display measurement. Verify window visibility before accepting performance results. Two processes share CPU/GPU; loopback cannot establish LAN/internet support or four-player/dense feasibility.

For a background pair with a local viewer, use `./tools/shared-combat.ps1 -Background`. This uses Python 3 from the bundled Codex runtime; pass `-PythonPath` to select another interpreter. It creates a same-login Win32 desktop, launches suspended players onto it, binds their process tree to a kill-on-close job, then resumes them. Your input desktop stays active; no OS mouse/keyboard injection or desktop switching. The session stops after 15 minutes, on input-desktop context loss, or through **Stop both games** in the viewer. Closing the launcher also kills its owned pair. This shares your existing CPU/GPU and same-user permissions.

The local pair helpers add `-sharedLoopback` to the host: current players bind `127.0.0.1:<port>` and save the actual `hostBindEndpoint` in their report. Direct `-host` without that flag retains the original all-interface bind; LAN/internet behavior remains unverified. Players archived before v7 ignore the new flag, so inspect source/report rather than assuming their binding changed. Ending cards distinguish **PARTY DEFEATED**, **RUN COMPLETE** and **SESSION STOPPED**; only completion/defeat expose both-ready Retry. Connection loss still aborts this fixture and needs a fresh pair; future reconnect rules are separate.

`-sharedPreview` enables Stack Lab's own test bridge: live snapshots, two-second-bounded movement/aim/fire, autoplay/idle, terminal Retry and quit. Commands use run/life/sequence identities and ordinary authoritative combat. Its camera renders into a texture and supplies 960×540 JPEG images targeting 30 Hz over continuous multipart streams; the viewer displays live state alongside the camera because the window's immediate-mode HUD is separate. Preview overhead and other desktop work exclude this mode from performance acceptance. Original native/manual launch and reference modes remain available. The loopback viewer address and command receipts are saved in the new results folder; it serves only this pair's state/images. This is application-specific control, not a generic desktop automation service.

Launcher integration checks: `python tools/tests/test_background_desktop.py` verifies actual child desktop placement, exact Unicode/quoted arguments, descendant cleanup and rejection when input context changes. The Unity Edit Mode suite covers bridge input/generation/terminal validation.

For a human A and supervised agent B, use `./tools/shared-combat.ps1 -Background -HumanHost -WaitForJoin`. A opens on the current input desktop with native WASD/mouse and the full game HUD; B uses the background bridge. **Start test** in the viewer launches B and begins the round. Return to A and click **Resume local input** if shown. Stop/deadline/input-desktop loss or closing either owned player cleans up the pair. The launcher starts B idle, so attach a controller before joining: `python tools/tests/play_shared_companion.py --viewer <printed-viewer-url> --results <new-results-directory>/agent`. This supervised policy follows A and covers nearby drones using expiring ordinary inputs, logs lifecycle/state and readies only B at the ending. A's Retry/R supplies the other readiness. It is an automated teammate, and this preview-enabled session remains human/functional evidence rather than a native performance capture.

Unity Transport **2.7.4** is the one added pinned package; existing editor/dependency versions remain unchanged. [Authority, history/reset policies, owner-agreed budgets and pending evidence](../../docs/planning/tasks/GDJ-20.md) define the bounded candidate. Earned progression is available in the GDJ-23 fixture below; thinking space remains absent. The original shared-rifle control has no progression. Original solo and synthetic benchmarks below retain their controls and workload.

## Scenario configuration (GDJ-22)

## First earned level-up (GDJ-23 candidate)

Select **Earned growth for solo / Host**, then Start or Host; Join adopts Host's settings. CLI solo: `-playtest -scenario earned-growth`. The fixed accepted fixture has 35 respawning 60-HP drones, seed 137, 60 seconds, one 20/40-damage rifle at .12 seconds, and 100 player HP. Overrides and rate edits are disabled for this fixture. Original solo/reference modes retain their controls and rate preferences.

Collect the blue ground orbs within 2 metres: kills alone grant no XP. Ten collected team XP earns level 2 and one independent personal offer for every roster member. **1/2** selects the visible card; **Tab** hides/reopens the same offer. Combat continues. Stronger hits gives 30/60 damage at .12 seconds; Faster fire gives 20/40 at .08 seconds. Both choices can appear in either order. Death retains offers/builds; a dead teammate can choose while another lives. Pair returns take five seconds with one second of contact protection. All-dead defeat precedes return/completion. Retry/R after ending starts a fully reset run when the fixed roster is ready; solo needs one click. Escape/focus loss releases input while the growth encounter continues.

```powershell
./tools/shared-combat.ps1 -Scenario earned-growth -ScriptedCompanion
./tools/shared-combat.ps1 -Scenario earned-growth -Scripted -HostChoice stronger -ClientChoice faster -QuitAtEnd
./tools/shared-combat.ps1 -Scenario earned-growth -Background -HumanHost -WaitForJoin
```

The background companion can explicitly select its personal card with `tools/tests/play_shared_companion.py --choice faster` (or `stronger`). Without that option it retains the offer. `-growthChoice stronger|faster` selects only when diagnostic scripted input has normally earned an offer; it grants no XP. Use swapped choices for the second comparison. Scripts are functional evidence, not enjoyment observations. `growth.jsonl` records run/tick/sequence, death/orb/collector, cumulative XP, offer and applied choice; summaries/preview state include XP/level/personal builds. Each run keeps its frozen starting scenario identity.

Protocol 3 adds validated earned state and bounded reliable ordered orb/choice events. Snapshots remain under 1100 bytes; the dynamic orb ledger preserves every uncollected orb independently of packet size. The supported experiment is solo/two players for 60 seconds; dense/four-player/recovery and extra levels/content remain separate. Full rules and owner comparison: [accepted contract](../../docs/design/first-earned-level-up.md).

### Starting configuration controls

Solo and shared rifle play consume the same validated version-1 starting configuration. The menu shows separate **Next solo** and **Host** settings; selecting a named scenario for both makes their starting enemy HP, damage and interval match. The default controls remain intentionally different:

| CLI name | Drones / HP | Body / head | Interval | Seed / duration |
| --- | --- | --- | --- | --- |
| `solo-precision` | 35 / 40 | 20 / 40 (x2) | .12 s | 137 / 60 s |
| `shared-rifle` | 12 / 60 | 20 / 40 (x2) | .12 s | 137 / 60 s |
| `developer-parity` | 12 / 90 | 15 / 45 (x3) | .25 s | 271 / 60 s |
| `earned-growth` | 35 / 60 | 20 / 40 (x2) | .12 s | 137 / 60 s |

With no `-scenario`, solo selects solo precision and Host selects shared rifle. `-scenario <name>` selects the same definition for either path. Explicit overrides: `-scenarioEnemies` (1–35), `-scenarioHp` (1–10000), `-scenarioDamage` (1–1000), `-scenarioHead` (integer 1–4), `-scenarioDuration` (finite 1–600 seconds), `-seed` (signed 32-bit integer), and the retained `-rifleRate` (finite .5–120 shots/sec). CLI numbers use invariant decimal notation. Unsupported names/versions, missing/malformed values and out-of-range settings reject launch. Overrides are labelled in the native HUD/menu and saved evidence. The maximum 35-drone snapshot fits the unchanged 1100-byte packet limit; this is a functional bound, not a new network-performance envelope.

Host transfers the full bounded scenario and a new session ID. Client validates and acknowledges that exact content before Start; local client tuning is ignored. Shared settings are frozen for the entire session, including both-ready Retry, which increments run identity and resets state. Changing shared settings requires a fresh pair. Protocol 3 intentionally rejects older players. Stale/mismatched configuration and malformed snapshots cannot replace active state. Disconnect still aborts.

Solo rate edits still preserve fractional cooldown, paused timing and session/restart retention; the chosen rate also applies to the next Host session. Scenario selection applies on Start/Host. Native HUD and JSON show the scenario name, content hash and effective HP/body/head/rate/interval. Shared output includes `scenario.json`, per-run summaries with `scenario`, and preview state with the same values. Solo Start/Restart writes `solo-scenario-<run-id>.json` under `-results` or the usual BenchmarkResults directory. Synthetic benchmarks retain their historical 40-HP/body-only workload and independent crowd controls; their existing rate/interval metadata remains authoritative.

Example direct launches from the repository root after the release build (use a fresh results folder for each pair):

```powershell
& ./prototypes/001-stacked-weapons/build/StackLab/StackLab.exe -playtest -scenario developer-parity -results C:/captures/gdj22-solo
& ./prototypes/001-stacked-weapons/build/StackLab/StackLab.exe -host -sharedLoopback -scenario developer-parity -sharedResults C:/captures/gdj22-pair/host
& ./prototypes/001-stacked-weapons/build/StackLab/StackLab.exe -join 127.0.0.1 -sharedResults C:/captures/gdj22-pair/client
```

Add `-sharedScripted` for an automated companion or `-sharedPreview` for the existing game-owned command/state bridge. Those are functional/diagnostic modes. [The task contract](../../docs/planning/tasks/GDJ-22.md) separates starting definitions from future per-player earned builds and the GDJ-21 test interface.

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
| `Runtime/EnemyGeometry.cs` | Shared body/head transforms, posed cube intersection and read-only region result |
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
