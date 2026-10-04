# Candidate: useful body brackets without central HP panels

- **Date / issue:** October 3, 2026 (America/Chicago), [GDJ-16](https://linear.app/cnid/issue/GDJ-16/show-enemy-health-and-enemyplayer-damage-clearly), [draft PR #4](https://github.com/OpenCnid/game-dev-journey/pull/4).
- **Candidate:** `GDJ-16-damage-v5-2026-10-03`, clean source `25d8157c97c58fe66331c2401c1e4cac58c91a6f`, built `2026-10-04T04:14:57.6233079Z`, dirty `false`.
- **Disposition:** P1 rejected full HP bars on healthy normal targets and requested damage-only visibility. Brackets remain helpful; supersede the bar policy. No performance result.

## Human finding and resulting behavior

P1 found the [preceding candidate's cyan body brackets](2026-10-03-focused-health-feedback.md) helpful for knowing who to target. The central black DEFEATED/HP panel was far too distracting when killing hundreds of mobs per minute. Wounded-bar stability was not explicitly confirmed. The [fourth revision contract](../planning/tasks/GDJ-16.md#fourth-human-revision-contract) retains the useful cue and removes the distracting one.

P1's next comparison disliked the healthy target's full cyan bar and could not tell whether hover or shooting triggered it. The aim query enabled the full bar before damage. They requested bars only after normal enemies become damaged, with bosses as a future always-visible exception, and requested research into easily imported custom crosshairs. The fifth revision changes HP visibility independently of body brackets and saves crosshair research separately.

The currently aimed living enemy has cyan brackets around its projected body/core and one small matching HP bar above its core. The brackets use both healthy/wounded pose matrices. There is no central HP panel, per-kill HP message or recent/dead-target health retention. On a miss or death, that target display ends; the existing brief hit/kill cues communicate outcomes.

Up to three additional amber wounded bars retain owner/incarnation slots across aim and distance changes. They do not duplicate the aimed enemy. Its bar reserves space first; overlap suppresses wounded labels, which require 0.25 seconds of clear space before returning. Losing an eligible owner delays replacement. Bars stay attached to their own cores. Candidate storage remains bounded at 64 within 40 metres; the focused target follows the existing rifle-range query. Healthy background bars stay hidden. Stable selection trades showing every nearest wound for consistent ownership; hidden wounds still have the simple deformed silhouette.

The simple body/core skew/compression remains a prototype cue, not separate physics pieces. The original sphere remains an approximate hit volume; precision head/body regions must align with the visible pose in [GDJ-17](https://linear.app/cnid/issue/GDJ-17/highlight-aimed-enemies-and-add-2-critical-head-hits). Health/damage remain simulation-owned. This change adds no critical damage, target-gated firing, crowd spacing or new enemy health rules. Pause/reset/incarnation cleanup and benchmark omission retain their contracts. Body overlap and future occlusion/higher-health content still need assessment.

## Exact build and checks

- Worktree `C:\Users\Darian\.codex\worktrees\gdj-16-damage-feedback\new-game-dev`, branch `feat/gdj-16-damage-feedback`; whole player folder `prototypes/001-stacked-weapons/build/StackLab`.
- Pinned Unity `6000.3.25f1` (`e1dba0a9aba4`), release Windows x64 Mono / Direct3D 11; embedded revision/time verified in player resources.
- 35 drones, 60 seconds, seed 137, staged Jobs/Burst, culled instances, separate character meshes, cube projectiles; 1920×1080 windowed, 144 FPS cap, VSync off.
- [Configuration](evidence/gdj-16-damage-v5/candidate.json), [complete 166-file SHA-256 manifest](evidence/gdj-16-damage-v5/build-manifest.json), [startup log](evidence/gdj-16-damage-v5/standalone-startup.txt). Whole build size 96,914,658 bytes. Distribute the whole folder.
- **46/46 Edit Mode passes:** [results](evidence/gdj-16-damage-v5/EditMode.xml). Damage/respawn across managed/Burst/native state, wound matrices, GUI scaling, retained owners, target-bar reservation, overlap recovery delay, no target UI on miss/death, pause/reset and benchmark filtering pass. Free label slots may fill while previously suppressed owners wait; existing owners must remain stable.
- **3/3 Play Mode passes:** [results](evidence/gdj-16-damage-v5/PlayMode.xml). Live target/damage HUD rendering through reference/culled paths, cleanup, benchmark omission and existing renderer/runtime smoke checks pass.
- Fresh release build/startup succeeded without reported runtime exceptions. All 166 hashes and the manifest fingerprint verified. Local documentation paths and whitespace checked before the evidence commit. Automated checks do not establish in-motion readability or fun.

Build an isolated checkout of the exact source with `.\tools\unity.ps1 -Action Build`, Editor and output player closed. Follow the [standalone launch command](2026-10-03-damage-readability-revision.md#exact-player-and-reproduction), using `artifacts/GDJ-16-v5-player.log` and optional `artifacts/GDJ-16-v5-human-run.png`. It opens at its menu: focus the window and press R or choose Start / Restart Combat. WASD moves, mouse aims, firing is automatic, Escape pauses; R clears the encounter and feedback. Focus loss pauses. The optional three-second screenshot is technical context, not an inferred complete attempt.

The exact GDJ-2 control remains untouched. Earlier GDJ-16 players v1–v4 are preserved as complete local archives; v2–v4 hashes match their saved manifests. The current player opened at the menu (PID 32412 at handoff; recheck before acting). No fresh acceptance question repeats P1's positive bracket finding. Record whether retained wounded bars stay stable while kiting and whether removing the panel leaves sufficient health clarity; keep GDJ-16 In Review until the latest human result is recorded. PR #4 remains stacked on open draft #3. GDJ-15 owns tracer alignment, GDJ-17 critical targeting, and other saved requests remain separate.
