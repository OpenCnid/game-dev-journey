# Candidate: fixed target HP and retained wounded bars

- **Date:** October 3, 2026 (America/Chicago).
- **Issue / review:** [GDJ-16](https://linear.app/cnid/issue/GDJ-16/show-enemy-health-and-enemyplayer-damage-clearly), [draft PR #4](https://github.com/OpenCnid/game-dev-journey/pull/4).
- **Candidate:** `GDJ-16-damage-v4-2026-10-03`, clean source `42f6a5d10f77627532cf669cfee4f534138ed112`.
- **Disposition:** replacement candidate ready; human bracket/panel ownership and wounded-bar stability comparison pending. No new performance result.

## Why this replaces the previous layout

P1 [rejected the attached-bar candidate](2026-10-03-damage-readability-revision.md): they could not identify the wide target bar's owner, found the scene really cluttered, and reported confusing/flickering labels. Wounded bars helped somewhat, but they warned that flicker would be worse for higher-health enemies. Earlier P1 feedback already established first-hit wound and player damage/death readability; their positive reaction to the simple wounded shape is retained.

The [third revision contract](../planning/tasks/GDJ-16.md#third-human-revision-contract) records the replacement before code. It preserves authoritative damage, read-only targeting, incarnation handling and the simple wound cue.

## Behavior and tradeoffs

- One fixed panel below the crosshair shows the aimed enemy's actual HP. Matching cyan corner brackets surround that enemy's projected body/core. Both parts of the deformed pose contribute to the brackets. The crosshair itself retains its existing color.
- On an aim miss, the panel keeps its previous health for up to 0.35 seconds, fades, and says LAST TARGET or DEFEATED. Brackets disappear immediately. This is recent information, not aim assistance, a target lock or permission to fire.
- At most three small amber wounded bars appear above their own cores. Up to 64 projected candidates within 40 metres are considered. Retained owner/incarnation slots get first refusal; aim/distance changes do not replace live eligible owners. New wounded enemies fill free slots. Healthy background bars stay hidden.
- Overlap suppresses a label in retained slot order. It needs 0.25 seconds of clear space before returning. Losing an eligible owner also delays replacement. Bars stay attached rather than shifting into floating rows.

This favors stable ownership over showing the nearest three wounds at every instant. A retained bar may be hidden while overlap persists; wounded silhouettes remain visible. Physical enemy/body overlap and occlusion are unchanged. Higher-health enemies are not added by this change; health display reads the current authoritative value. Future per-type maxima and precision body/head regions still need implementation.

The simple wound pose is a visual skew/compression, not separate physics pieces. The existing spherical hit volume remains approximate. [GDJ-17](https://linear.app/cnid/issue/GDJ-17/highlight-aimed-enemies-and-add-2-critical-head-hits) must align critical regions with visible poses before precision acceptance. Independent hit/kill cues, death sparks, muzzle pulses and player damage cues retain the previous revision. Pause freezes timers, restart clears ownership/focus, and synthetic benchmarks omit the new presentation. Rifle cadence/damage, movement, contact grace and crowd rules are unchanged.

## Exact player and reproduction

- Worktree: `C:\Users\Darian\.codex\worktrees\gdj-16-damage-feedback\new-game-dev`; branch `feat/gdj-16-damage-feedback`.
- Embedded source `42f6a5d10f77627532cf669cfee4f534138ed112`, built `2026-10-04T04:03:50.3148129Z`, dirty `false`. Revision/time were verified in player resources.
- Pinned Unity `6000.3.25f1` (`e1dba0a9aba4`), release Windows x64 Mono / Direct3D 11.
- 35 drones, 60 seconds, seed 137; staged Jobs/Burst, culled instances, separate character meshes, cube projectiles; 1920×1080 windowed, 144 FPS cap, VSync off.
- Whole build `prototypes/001-stacked-weapons/build/StackLab` in that worktree: 166 files, 96,914,658 bytes. Preserve/distribute the whole folder.
- [Configuration](evidence/gdj-16-damage-v4/candidate.json), [SHA-256 build manifest](evidence/gdj-16-damage-v4/build-manifest.json), [startup log](evidence/gdj-16-damage-v4/standalone-startup.txt).

With the Editor and output player closed, build an isolated checkout of the exact revision using `.\tools\unity.ps1 -Action Build`. Launch `build/StackLab/StackLab.exe` from the prototype with seed 137, simulation burst, rendering culled and 1920×1080 windowed arguments, following the [previous reproduction command](2026-10-03-damage-readability-revision.md#exact-player-and-reproduction). Use `artifacts/GDJ-16-v4-player.log` and `artifacts/GDJ-16-v4-human-run.png` for this candidate. It opens at the menu; focus its window and press R or choose Start / Restart Combat. WASD moves, mouse aims, firing is automatic, Escape pauses, and R resets. Focus loss pauses; the optional capture occurs after three simulation seconds.

The GDJ-2 control is preserved in its separate worktree. Earlier GDJ-16 builds remain complete local archives `build/StackLab-GDJ16-v1`, `-v2` and `-v3`. The v2/v3 archive hashes were checked against their manifests before replacement.

## Checks and remaining comparison

- **46/46 Edit Mode passes:** [results](evidence/gdj-16-damage-v4/EditMode.xml). Includes authoritative damage/respawn across managed/Burst/native state, damage lifecycle, independent cues, wound matrices, scaled GUI anchors and target/core projection. The dense crowd test verifies retained owners under movement, overlap suppression, delayed reappearance with the same owners and no damage from layout. A new focus test verifies actual health, no full-health floating label, recent-health retention on an aim miss, pause freeze, expiry and reset.
- **3/3 Play Mode passes:** [results](evidence/gdj-16-damage-v4/PlayMode.xml). The HUD renders with the fixed panel, both reference/culled damage outcomes pass, and focus/reset/benchmark omission are checked along with existing runtime/renderer smoke tests.
- Fresh release build succeeded from clean source. Standalone startup loaded on Direct3D 11 / RTX 3090 without reported runtime exceptions. All 166 build hashes and the manifest fingerprint verified. Documentation paths and whitespace were checked before the evidence commit.
- No performance capture, inferred enjoyment or human acceptance from automated checks. Bracket ownership and in-motion stability still need the focused human comparison.

The player opened at its menu (PID 25612 at handoff; recheck before acting). A concise question asks whether cyan brackets make the fixed HP panel's owner clear and whether wounded bars remain stable while kiting. Record that answer and remaining clutter before marking GDJ-16 Done. PR #4 is still stacked on open draft PR #3; retarget after #3 merges. GDJ-15 retains tracer alignment, GDJ-17 critical targeting, and other saved requests remain separate.
