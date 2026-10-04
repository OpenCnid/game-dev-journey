# GDJ-17: aimed target color and critical head hits

**October 4, 2026 — tested candidate; P1 comparison accepted.** This serves readable, responsive combat after the accepted GDJ-15 aiming and GDJ-19 rate controls. [Task contract](../planning/tasks/GDJ-17.md) records ownership, region ordering and downstream consumers before runtime edits. [Linear](https://linear.app/cnid/issue/GDJ-17/highlight-aimed-enemies-and-add-2-critical-head-hits) owns live status.

## Candidate behavior

The reticle is **green over a body, gold over a head and white on a miss**. Preview remains available during cooldown and clears on the next displayed miss. A shared read-only `QueryAim` checks the displayed position and the same transformed body/head cubes used by rendering, including wounded compression, offset and shear. Firing queries again; UI state never grants damage. Select the first surface across all regions/enemies: nearer body blocks farther head, exactly coincident regions prefer head, exactly coincident enemies prefer lower slot index. Inside starts at distance zero; boundaries are inclusive; behind, zero-direction and beyond-65-m rays miss.

Body deals **20 ×1**, head **20 ×2** against 40 HP. A fresh head hit kills once. A wounded critical requests 40 but records 20 actual damage, with health before/after and the pre-respawn incarnation. Outcomes carry region and actual damage for later weapons/attribution; no cooperative authority or attribution system is added. The head/core's final lore name stays provisional.

A short **CRIT ×2** label appears above the reticle independently of the existing white hit marker and outer gold KILL confirmation. Wounded-only amber HP, separate player damage cues, displayed-pose firing, current-muzzle one-frame combat tracer and all rate/pause/restart controls remain. Pause hides target color and freezes transient cues; restart clears them. No body brackets, healthy HP, retained defeated panel, aim assist, target-gated autofire or crosshair appearance editor is added. GDJ-18 can later consume the dynamic region state while owning static appearance/import/persistence.

Combat draws paired meshes, including when the combined-character experiment is selected. That experiment's alternative wound matrix does not match the precision regions. Benchmark mode preserves the combined/reference rendering path, fixed-state sphere, body-only 20 damage and legacy tracer, and omits precision cues/wounded poses. Benchmark results therefore remain reference workload evidence, not precision-combat performance evidence.

## Exact release and controls

- Clean source **`2e721214a51f7a8b5bb5ce22305abed4bb3dc48b`**, stamped **`2026-10-04T06:25:15.8539409Z`**, built using `tools/unity.ps1 -Action Build`. Unity **6000.3.25f1**, release Windows x64 Mono/D3D11; pinned packages/render settings unchanged. Later evidence commits do not change this runtime.
- [Candidate metadata](evidence/gdj-17-precision-v1/candidate.json) / [whole-player manifest](evidence/gdj-17-precision-v1/build-manifest.json): **166 files / 96,919,497 bytes**, manifest SHA256 `bd0816f623232030c91da066fdfb4363d5360ea99cb59f0380a545d470a91198`. Every archived file matches the live player's manifest.
- Player: `C:/Users/Darian/.codex/worktrees/gdj-17-precision-hits/new-game-dev/prototypes/001-stacked-weapons/build/StackLab/StackLab.exe`. Whole-player archive: sibling **`StackLab-GDJ17-v1`**. Distribute its whole folder.
- Launch configuration: seed 137, manual-start menu, default 35-drone/60-second combat, Burst/culled, paired meshes, baseline 8.33/s, 1920×1080 windowed, cap 144/VSync off. [Startup metadata](evidence/gdj-17-precision-v1/menu-startup.json) records the invocation. Candidate was opened for P1 and the comparison was accepted below.
- Accepted **StackLab-GDJ19-v2 / StackLab-GDJ15-v1 / StackLab-GDJ16-v7** archives in the GDJ-16 worktree were read and fully reverified before opening this candidate: **166 files each, zero hash mismatches** against saved manifests. [Verification](evidence/gdj-17-precision-v1/preserved-controls.json) records their sources/paths. That other checkout/build output was not rewritten.

## Checks

Serialized pinned-editor checks pass **77 Edit Mode / 7 Play Mode** cases. [Edit Mode XML](evidence/gdj-17-precision-v1/EditMode.xml) / [Play Mode XML](evidence/gdj-17-precision-v1/PlayMode.xml). The 12 new Edit Mode cases and one new Play Mode case cover:

- Body/head edges and side views, wounded shear/compression and moved head, restored healthy geometry on reset, inside/behind/zero/range behavior.
- Near body/far head, healthy and wounded region overlap, exact region/enemy ties, read-only preview and misses consuming cooldown.
- Fresh/wounded critical snapshots, actual versus requested damage, one death/respawn/incarnation and cooldown-independent preview across managed/Burst/native state. Existing displayed-interpolation checks now test the actual cube silhouette and hit-spark surface rather than the replaced sphere.
- Runtime body/gold-head/white-miss states, damaged-only HP, critical rendering alongside independent hit/KILL cues, cue expiry, pause freeze and reset. Existing aim/tracer, rate validation/cadence and renderer/HP tests remain passing.

The initial fresh-import Edit run passed 74/76: the Burst probe ran during compilation, and a decimal range fixture rounded its surface slightly beyond 65 m. The fixture now straddles the boundary by a millimetre; the separate unit-cube test checks exact inclusive boundaries. An intermediate `Prepare` reached `PROJECT_READY` then crashed during editor shutdown (exit -1073741819). Final settled Edit/Play checks and release Build, including its Prepare, exit successfully. Initial Play failures were the new test's unregistered diagnostic log and an old sphere-era render test aiming through the newly compressed silhouette; its body-ray fixture now tests both healthy/wounded bodies. No runtime rule was weakened to make those fixtures pass. Final startup log has no Exception/ERROR/Crash matches.

Two hidden standalone functional smokes use 35 regular drones, one-second warm-up/five-second capture:

| Rate | Captured shots | Captured ticks | Catch-up/dropped projectiles | Evidence |
| --- | --- | --- | --- | --- |
| 2/s | 10 | 300 | 0 / 0 | [Report](evidence/gdj-17-precision-v1/smoke-slow.json), [invocation](evidence/gdj-17-precision-v1/invocation-slow.json) |
| 120/s | 600 | 300 | 0 / 0 | [Report](evidence/gdj-17-precision-v1/smoke-stress.json), [invocation](evidence/gdj-17-precision-v1/invocation-stress.json) |

Both report the exact clean source, requested rate and Burst/culled backends with Burst compiled. These exercise the preserved reference benchmark, not critical gameplay. **No new performance conclusion** comes from these shortened hidden runs. The [startup menu capture](evidence/gdj-17-precision-v1/menu.png) was inspected: precision instructions and all rate/start/benchmark/FPS controls are legible. Startup FPS is not a gameplay measurement.

## Reproduction and human comparison

From the task worktree, use `tools/unity.ps1 -Action Test -TestPlatform EditMode`, then PlayMode, then Build, with that project's Editor closed. To reproduce the archived runtime exactly, build source `2e72121` in a clean checkout. Candidate launch:

```powershell
& 'C:/Users/Darian/.codex/worktrees/gdj-17-precision-hits/new-game-dev/prototypes/001-stacked-weapons/build/StackLab-GDJ17-v1/StackLab.exe' -seed 137 -simulation burst -rendering culled -screen-width 1920 -screen-height 1080 -screen-fullscreen 0
```

Comparison brief supplied to P1:

1. Select **Slow 2/s**, Start. Compare white miss, green body and gold head while turning. Body should wound/show amber HP; fresh exposed head should kill with CRIT/KILL. Aim at the wounded shifted head and squashed body, then leave the target to check immediate preview clearing.
2. Escape, choose **Rapid 30/s**, Resume. Compare hit/critical/kill readability with accepted GDJ-19 aiming/cadence. Stress 120/s is available as an optional cue-density check. R resets HP/time/kills/cues while retaining the selected rate.
3. Report whether aim and visible regions agree, whether precision feels rewarding/clear, any troublesome overlap or delayed cues, and accept or request one bounded revision.

## Human acceptance — October 4

After opening this exact candidate, P1 was asked to compare Slow 2/s and Rapid 30/s, target color against aim, wounded-region agreement and critical/rapid-fire cue clarity. P1 responds: **“Yes, that all looks good.”** This accepts the presented aiming/region/feedback comparison. No detailed rate selections, run counts or performance measurements were supplied. The acceptance changes documentation/metadata only; runtime source and whole-player hashes remain unchanged.

Keep GDJ-17 **In Review** with a draft PR for coordinator code review/integration. P1's comparison is satisfied; no merge instruction was supplied in this chat. This is local single-player combat; high-count precision gameplay cost, multiplayer/weapon attribution, accessibility/color alternatives and final critical naming remain unverified/deferred. Historical crowd throughput cannot validate the new posed query at late-game scale.
