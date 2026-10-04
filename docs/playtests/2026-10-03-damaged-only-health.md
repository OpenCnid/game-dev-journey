# Candidate: normal-enemy HP only after damage

- **Date / issue:** October 3, 2026 (America/Chicago), [GDJ-16](https://linear.app/cnid/issue/GDJ-16/show-enemy-health-and-enemyplayer-damage-clearly), [draft PR #4](https://github.com/OpenCnid/game-dev-journey/pull/4).
- **Candidate:** `GDJ-16-damage-v6-2026-10-03`, clean source `586b1f5b9a655ad8bd0684771088b050c55a5897`, built `2026-10-04T04:25:48.0323038Z`, dirty `false`.
- **Disposition:** technical checks/build pass; latest damage-only crowd comparison pending. No new performance result or crosshair-editor implementation.

## Human finding and correction

P1 disliked the [preceding candidate's full cyan normal-target HP bar](2026-10-03-aimed-health-feedback.md) and could not tell whether aiming or shooting triggered it. It was enabled by the aim query before damage. They request bars only after normal enemies become damaged, with a future full-health exception for bosses. They still find the cyan body brackets useful and request easily imported custom crosshairs. The [fifth revision contract](../planning/tasks/GDJ-16.md#fifth-human-revision-contract) was recorded before implementation.

Aiming at a healthy normal enemy now shows the cyan body brackets without an HP bar. An actual health loss enables its attached bar; only wounded background enemies are eligible for the retained small bars. Healthy target brackets neither reserve HP-label space nor hide an eligible wounded owner. Full-health respawns/reset hide bars immediately. Miss/death clears aimed-target UI; existing hit/kill cues communicate outcomes. The accepted brackets, simple wounded silhouette and independent damage cues remain. There is no central or recent/dead-target HP panel.

Up to three additional wounded owners remain stable by incarnation, with bounded 64-candidate storage within 40 metres and 0.25-second overlap recovery delay. A visible wounded target bar reserves its own bounds first and avoids a duplicate label. Physical crowd overlap and the approximate sphere hit volume remain; future precision critical regions need visible-pose alignment in GDJ-17. Boss HP visibility is a saved explicit future enemy policy, not a new combat boss implementation or a use of the synthetic benchmark's boss flag. Health/damage/collision remain authoritative and unchanged; pause/reset and benchmark omission retain their contracts.

The requested three-agent [crosshair research](../design/crosshair-customization-and-imports.md) records reference workflows, native codes, transparent PNGs, compatibility limits and a later implementation proposal. It does not add customization to this player.

## Exact build and checks

- Worktree `C:\Users\Darian\.codex\worktrees\gdj-16-damage-feedback\new-game-dev`, branch `feat/gdj-16-damage-feedback`; whole player folder `prototypes/001-stacked-weapons/build/StackLab`.
- Pinned Unity `6000.3.25f1` (`e1dba0a9aba4`), release Windows x64 Mono / Direct3D 11; source revision/time verified in player resources.
- 35 drones, 60 seconds, seed 137, Jobs/Burst, culled instances, separate character meshes, cube projectiles; 1920×1080 windowed, 144 FPS cap, VSync off.
- [Configuration](evidence/gdj-16-damage-v6/candidate.json), [complete 166-file SHA-256 manifest](evidence/gdj-16-damage-v6/build-manifest.json), [startup log](evidence/gdj-16-damage-v6/standalone-startup.txt). Whole build 96,914,658 bytes. Manifest SHA-256 `c2afff493eecdbd8072e3bc38715b2c0baf63f4cdb5e999d5a21bbb90c64eb39`. Distribute the whole folder.
- **46/46 Edit Mode passes:** [results](evidence/gdj-16-damage-v6/EditMode.xml). Checks cover healthy aim without HP, first-hit visibility, full-health respawn hiding, healthy brackets not reserving label space, actual health snapshots and the retained feedback/layout/lifecycle contracts.
- **3/3 Play Mode passes:** [results](evidence/gdj-16-damage-v6/PlayMode.xml). Live rendering checks healthy aim before any shot, HP after damage, cleanup and benchmark filtering; existing runtime/render smoke checks pass.
- Fresh release build/startup succeeded without reported runtime exceptions. All 166 build hashes and embedded clean source metadata verified. Documentation paths/whitespace checked before the report commit. These checks do not establish crowd readability or fun.

Build an isolated checkout of the exact source with `.\tools\unity.ps1 -Action Build`, Editor and output player closed. Follow the [standalone launch command](2026-10-03-damage-readability-revision.md#exact-player-and-reproduction) with `artifacts/GDJ-16-v6-player.log` and optional `artifacts/GDJ-16-v6-human-run.png`. The player opens at its menu: focus and press R or choose Start / Restart Combat. WASD moves, mouse aims, automatic firing continues, Escape pauses; R resets the encounter. Focus loss pauses. No optional screenshot was present at evidence collection; no active attempt is inferred from launching.

The GDJ-2 control and earlier GDJ-16 players v1–v5 are preserved as complete local archives; v2–v5 archive hashes match their manifests. The current player opened at the menu (PID 29468 at handoff; recheck). Keep GDJ-16 In Review until the latest damaged-only wounded-bar stability/clutter result is recorded. Do not repeat the already positive bracket question. PR #4 remains stacked on open draft #3. GDJ-15 tracer alignment, GDJ-17 critical targeting and the requested customization implementation remain separate.
