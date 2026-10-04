# Candidate: enemy health and damage readability

- **Date:** October 3, 2026 (America/Chicago).
- **Issue:** [GDJ-16](https://linear.app/cnid/issue/GDJ-16/show-enemy-health-and-enemyplayer-damage-clearly).
- **Candidate:** `GDJ-16-damage-2026-10-03`, release Windows player from clean source `52650ce35aebd3ac43311a547218cd1d317fb200`.
- **Question:** can the developer recognize wounded drones, deaths and player damage, and use that information while kiting a group?
- **Disposition:** implementation and automated checks complete; standalone human comparison pending. This is not human acceptance or a performance result.
- **Control:** the exact [GDJ-2 rifle player](2026-10-03-rifle-baseline.md), built from `28ca1bce4d71d0af82326a93f65a528ebfd1cd5c`, remains untouched in its separate worktree.

## Behavior and ownership

The [task brief](../planning/tasks/GDJ-16.md) records the future-interaction contract before implementation. The rifle still fires every 0.12 simulation seconds, applies 20 damage immediately and kills a 40-HP drone in two hits. Contact damage, movement, enemy behavior, counts, seed and run duration retain the baseline rules. Pinned Unity/packages and the default staged Burst/culled renderer are preserved.

- Two-segment bars show actual health: full mint at 40 HP, half-filled amber at 20 HP. They select the nearest 64 on-screen enemies within 40 metres, above the head and outside the top/bottom HUD. The flat arena has no interior occluders; projected bars do not perform occlusion or overlap avoidance.
- White impact sparks and a short white hit marker confirm a successful hit. A larger gold burst, gold marker and KILL label distinguish death. Misses and cooldown rejection produce no enemy damage cue.
- The old 0.2-second full-screen red wash becomes a 0.35-second red edge pulse plus a 0.65-second actual-health-loss label, keeping the center clear.
- Damage cues freeze during pause, clear on restart/benchmark entry and expire after terminal states. Bars remain eligible during pause and disappear after a win/loss; the pause menu can cover central bars.

Simulation owns health and returns an immutable hit/death snapshot before respawn. A target incarnation prevents a wound following a newly respawned slot; death sparks stay at the old hit point. Presentation uses a fixed 16-impact pool (up to eight instanced cubes each), one marker and bounded bar storage, without an object per enemy. Weapon bursts may replace older visual effects, never authoritative damage. New enemy types need per-entity/type health maxima; cooperative outcomes can add source/owner attribution.

The new feedback path is disabled in synthetic benchmarks. Bar selection still scans the local combat crowd; no 20,000-enemy cost or performance acceptance is claimed. Dense-group decluttering, through-wall visibility, accessibility settings and different damage sources remain later work.

## Exact candidate and reproduction

- Worktree: `C:\Users\Darian\.codex\worktrees\gdj-16-damage-feedback\new-game-dev`.
- Branch: `feat/gdj-16-damage-feedback`, based on GDJ-2 report commit `2fc86696f88656272b94207eea145805f0f61631`. Later evidence commits do not change the embedded player revision.
- Unity: `6000.3.25f1` (`e1dba0a9aba4`), release Windows x64 Mono/Direct3D 11.
- Embedded build time: `2026-10-04T03:20:40.4026084Z`; dirty flag `false`.
- Configuration: 35 drones, 60 seconds, seed 137, staged Jobs/Burst, culled instances, separate character meshes, cube projectiles, 1920x1080 windowed, 144 FPS cap, VSync off.
- Whole build folder: `prototypes\001-stacked-weapons\build\StackLab` under that worktree; 166 files, approximately 92.42 MiB. Distribute the whole folder.
- [Configuration](evidence/gdj-16-damage/candidate.json), [complete SHA-256 build manifest](evidence/gdj-16-damage/build-manifest.json) and [startup log](evidence/gdj-16-damage/standalone-startup.txt).
- Live log under that worktree: `prototypes/001-stacked-weapons/artifacts/GDJ-16-player.log`.

With the Editor closed, build an isolated checkout of the source revision using `.\tools\unity.ps1 -Action Build`. Launch the human candidate at its menu:

```powershell
$candidatePlayer = Join-Path (Get-Location).Path 'prototypes\001-stacked-weapons\build\StackLab\StackLab.exe'
$candidateLog = Join-Path (Get-Location).Path 'prototypes\001-stacked-weapons\artifacts\GDJ-16-player.log'
$candidateScreenshot = Join-Path (Get-Location).Path 'prototypes\001-stacked-weapons\artifacts\GDJ-16-human-run.png'
Start-Process -FilePath $candidatePlayer -ArgumentList @(
    '-seed', '137', '-simulation', 'burst', '-rendering', 'culled',
    '-screen-width', '1920', '-screen-height', '1080', '-screen-fullscreen', '0',
    '-logFile', ('"' + $candidateLog + '"'),
    '-screenshot', ('"' + $candidateScreenshot + '"')
)
```

Click the newest Stack Lab window and press R or choose Start / Restart Combat. Move with WASD and aim with the mouse; firing is automatic. Escape pauses/resumes; focus loss pauses. R restores the baseline run and clears feedback. The optional screenshot captures after three simulation seconds; it is technical evidence, not an active human attempt.

## Checks and limits

- **39/39 Edit Mode passes:** [results](evidence/gdj-16-damage/EditMode.xml). Six new cases cover managed/staged/native hit outcomes, wounded health, death before respawn, rejection/miss behavior, player damage/lifecycle and bounded projected bars. Existing cadence, contact grace, restart and terminal tests also pass.
- **3/3 Play Mode passes:** [results](evidence/gdj-16-damage/PlayMode.xml). The new test renders wound/death effects through culled and reference paths, checks a visible wounded bar, exercises the HUD, pause/reset and unexpected logs. Existing renderer experiment and arena smoke checks pass.
- Fresh release build succeeded through `tools/unity.ps1`; embedded source is clean. Generated caches and package test resources are not committed.
- Standalone startup loaded on Direct3D 11 / RTX 3090 / driver `32.0.15.9186`, without reported runtime errors. No three-second screenshot was produced by the unfocused auto-start smoke; focus loss pauses combat by design. It was closed and the human candidate reopened at its menu. Startup alone does not establish visual readability or gameplay acceptance.
- All 166 candidate and 166 preserved-control build file hashes verified, along with the manifest fingerprint and 89 local Markdown paths in the touched documentation. Whitespace checks passed.
- No new performance capture, hardware comparison, networking result or inferred human observation.

## Human comparison and resume

Pending. The developer has been asked whether they could identify a first-hit wound, a second-hit death and a player damage moment, whether these helped group-kiting and whether the bars/effects were cluttered or difficult to see. Record the answer here and inspect the current log/capture before completing GDJ-16; revise if necessary.

The candidate was left at its start menu. There may be two Stack Lab windows; select the most recently opened one. Rifle aim/tracer alignment remains [GDJ-15](https://linear.app/cnid/issue/GDJ-15/align-rifle-hitscan-and-tracer-feedback-during-fast-turns); ammo, target-gated autofire, jump, ranged threats and progression remain saved follow-ups.
