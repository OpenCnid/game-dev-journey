# Playtest: current rifle baseline

- **Date:** October 3, 2026 (America/Chicago).
- **Candidate:** `GDJ-2-rifle-2026-10-03`, release Windows player from clean source `28ca1bce4d71d0af82326a93f65a528ebfd1cd5c`.
- **Platform and input:** Windows desktop, keyboard and mouse; i7-10700K, RTX 3090, 63.83 GiB RAM.
- **Participant label:** P1, developer; at least two active attempts and the pause/reset checks confirmed.
- **Question:** are movement/aim and rifle feedback promising enough to expand, or should one concrete problem be revised first?
- **Issue:** [GDJ-2](https://linear.app/cnid/issue/GDJ-2/play-the-current-rifle-slice-and-choose-the-next-gameplay-change).
- **Disposition:** GDJ-2 acceptance completed; revise damage readability before stacking. [GDJ-16](https://linear.app/cnid/issue/GDJ-16/show-enemy-health-and-enemyplayer-damage-clearly) is the one next gameplay change.

## Setup

The candidate preserves the existing local slice: 35 drones, a 60-second run, seed 137 and one automatically firing rifle. Jobs/Burst uses staged state; rendering uses culled instances, cube projectiles and separate character meshes. Requested player resolution is 1920x1080 windowed, with a 144 FPS cap and VSync off. The standalone log confirms Direct3D 11 and the RTX 3090, driver `32.0.15.9186`. The desktop is 3840x1600; Windows reports 143 Hz. Player adjustments remain unreported.

The standalone was opened for technical smoke, then left available for P1. P1 was asked to click the game and press R for a fresh run, move with WASD, aim with the mouse and play two short attempts. During the second attempt, deliberately pause/resume with Escape and verify R resets health, the clock and kills. The rifle fires automatically; no firing input is required. Survive until the minute ends or health reaches zero. Focus loss also pauses this slice; resume after returning to its window.

After P1 reported having taken no control, the instructions were clarified: automatic fire can kill drones while standing still, but P1 must move and aim for these human observations. No active run is credited from the launch smoke.

P1 subsequently reported actively trying the game and unpausing it. Further comments describe swiveling aim and the rifle's visual behavior. P1 confirmed at least two active attempts, instantaneous R restart, countdown freeze with Escape, and R restoring 100 HP, 60 seconds and 0 kills. Run endings/time/kills and settings changes were not supplied.

## Exact candidate and reproduction

- Worktree: `C:\Users\Darian\.codex\worktrees\gdj-2-rifle-playtest\new-game-dev`.
- Report branch: `review/gdj-2-rifle-baseline`, based on the saved [handoff commit](https://github.com/OpenCnid/game-dev-journey/commit/28ca1bce4d71d0af82326a93f65a528ebfd1cd5c). Later commits on this branch record evidence; the player embeds the source above.
- Unity: `6000.3.25f1` (`e1dba0a9aba4`). Packages and renderer defaults are unchanged.
- Embedded build time: October 3, 21:40:51 CDT (`2026-10-04T02:40:51.7367682Z`), dirty flag `false`. Build log completed at 21:41:58 CDT (`2026-10-04T02:41:58.7014811Z`).
- Full build folder: `C:\Users\Darian\.codex\worktrees\gdj-2-rifle-playtest\new-game-dev\prototypes\001-stacked-weapons\build\StackLab` (166 files, approximately 92.41 MiB). Keep the whole folder with the player.
- Player log: `prototypes/001-stacked-weapons/artifacts/GDJ-2-player.log` under that worktree.
- [Configuration and test summary](evidence/gdj-2-rifle/candidate.json); [full build SHA-256 manifest](evidence/gdj-2-rifle/build-manifest.json).
- EXE SHA-256: `3070e580e019576b9afcdeb4fec18d03cebc119f6ae84d597412905c749ad653`. The manifest also fingerprints the game assembly, serialized assets and runtime libraries; the EXE hash alone does not identify gameplay.

With the project Editor closed, build an isolated checkout of the source revision using `.\tools\unity.ps1 -Action Build`. Launch from the worktree root:

```powershell
$candidatePlayer = Join-Path (Get-Location).Path 'prototypes\001-stacked-weapons\build\StackLab\StackLab.exe'
$candidateLog = Join-Path (Get-Location).Path 'prototypes\001-stacked-weapons\artifacts\GDJ-2-player.log'
Start-Process -FilePath $candidatePlayer -ArgumentList @(
    '-playtest', '-seed', '137', '-simulation', 'burst', '-rendering', 'culled',
    '-screen-width', '1920', '-screen-height', '1080', '-screen-fullscreen', '0',
    '-logFile', ('"' + $candidateLog + '"')
)
```

The initial technical launch additionally used `-screenshot` with the ignored artifact path `artifacts/GDJ-2-standalone-smoke.png` under the Unity project. This saves a frame after three simulation seconds without scripting movement or aim. Use R to begin each human attempt from a fresh state. Avoid launching another instance while this player is open; copy its log before relaunching if the prior log is needed.

## Technical checks

These checks were run for this candidate, separately from historical benchmark evidence:

| Check | Result and limit |
| --- | --- |
| `tools/unity.ps1 -Action Build` | Release Windows build succeeded; fresh output, no older EXE reused. |
| `tools/unity.ps1 -Action Test -TestPlatform PlayMode` | [2/2 passed](evidence/gdj-2-rifle/PlayMode.xml), including start, automatic firing and shot reset on restart. These tests do not verify human input feel. |
| `tools/unity.ps1 -Action Test -TestPlatform EditMode` | [33/33 passed](evidence/gdj-2-rifle/EditMode.xml), including fixed-tick movement/cadence, pause and run reset rules. |
| Standalone launch/rendering | The [three-second capture](evidence/gdj-2-rifle/standalone-smoke.png) shows the arena, rifle trail, 35 drones, HP 100, 2 kills and 57.0 seconds remaining. This confirms firing and rendered state. |
| Standalone log | The [log snapshot](evidence/gdj-2-rifle/standalone-smoke.txt), rechecked after human play, contains no runtime exceptions, shader errors or warnings. Its line endings are normalized; the original remains in the ignored artifacts folder. |
| Source/configuration | No gameplay, scene, renderer or pinned-package change. The performance-test package temporarily generated resource metadata during Build; the subsequent test lifecycle removed it. Git was clean again before adding this report/evidence. |

No new performance capture was run. The screenshot's FPS readout is one moment, not percentile or performance acceptance. September 30 synthetic rendering results remain historical and concern a different workload. No preparation/runtime blocker required a gameplay fix.

## Observations

**Human evidence:** the initial launch involved no movement/aim or pause/resume attempts. P1 later confirmed at least two active attempts, successful unpause, countdown freeze with Escape and instantaneous R restart to 100 HP/60 seconds/0 kills. P1 called the slice pretty solid and described laggy/multiple shot lines while swiveling and hits that felt like traveling projectiles. P1 described the current threat as running away from enemies. Their most specific tactical friction was being unable to recognize wounded two-hit drones well enough to lure the group effectively. These are participant reports; no video or timing measurement establishes the visual artifact's cause. The unattended launch does not count toward the two active attempts. Endings/time/kills were not reported. Player damage-source recognition remains an identified feedback problem, not an accepted readability result.

**Technical observation:** the standalone progressed and fired at the initial aim during smoke. No AI movement or aiming was performed. This supports launch/firing checks only.

## Player feedback

P1: “I didn't do any runs, it just played for me i thought. I didn't aim or anything.” P1: “I didn't try pausing or resuming”. These comments identify confusion about the automatic firing and immediate launch; they do not establish a combat acceptance result.

After active play, P1 called the game “pretty solid” and requested:

- Ammo to prevent continuous unlimited firing; magazine/reserve and reload rules were not specified.
- Enemy HP bars, followed by an explicit request for a particle effect or color blink when an enemy/player takes damage. P1 linked enemy health visibility to useful group-kiting decisions.
- Automatic fire only when the crosshair is on an enemy; no shots when aiming away.
- Threatening enemy behavior beyond chasing; consider MegaBonk from a ranged-combat perspective.
- Immediate rifle aim/hit feedback like Soldier: 76. P1 specifically reported shot lines lagging/showing multiple lines during swivels and hits feeling like traveling projectiles.
- A way to jump.

P1 also reported: “pressing R instantly resets the game”. P1 subsequently confirmed at least two active attempts and answered yes to Escape stopping the countdown and R restoring 100 HP, 60 seconds and 0 kills. The requested features are not implemented capability. Endings/time/kills and settings remain unreported; no values are invented.

## Interpretation

The initial delivery did not make P1's role in the playtest clear enough; start/control instructions were clarified. Later feedback supports a promising basic slice with a concrete aim/hit-presentation problem. It does not establish broad fun, build variety, cooperative acceptance or complete lifecycle validation.

Source inspection shows [TryFire](../../prototypes/001-stacked-weapons/Assets/Prototype/Runtime/CombatSimulation.cs) already applies ray/sphere hit damage immediately in its firing tick, without rifle projectile travel. [ShowShot](../../prototypes/001-stacked-weapons/Assets/Prototype/Runtime/ArenaPresentation.cs) samples the muzzle from the current camera transform, while [PrototypeGame.Update](../../prototypes/001-stacked-weapons/Assets/Prototype/Runtime/PrototypeGame.cs) calls it before Draw updates that transform for the new aim. The world-space tracer remains visible for 45 ms. A previous camera/muzzle pose and lingering world-space line could contribute to the perceived lag during fast turns. This is a source-based hypothesis, not a verified explanation for the reported multiple lines; there is one rifle LineRenderer in the inspected code. Render interpolation versus simulation hit geometry also needs examination.

P1's Soldier: 76 comparison is interpreted as immediate crosshair hits with a brief cosmetic tracer. Blizzard's [February 2024 patch notes](https://overwatch.blizzard.com/en-gb/news/patch-notes/live/2024/02/) explicitly classify Heavy Pulse Rifle sizing as hitscan. Exact Overwatch tuning, assets and automatic target tracking are not selected for this project.

## Ranged-threat reference and later experiment

MegaBonk's [official developer announcements](https://steamcommunity.com/app/3405340/announcements/) describe evadable cactus projectiles, Anubis lasers, boss orbs that can freeze, and visible spheres marking enemy attack locations. Those primary-source examples support pressure through projectiles, affected space and temporary movement impairment; they do not establish its complete AI behavior or timings. No MegaBonk hands-on comparison was performed here.

**Our proposed later experiment:** retain a small chasing crowd and add a small number of ranged attackers that visibly wind up, mark a shot/impact location, commit to it, and then allow a dodge or an opportunity to kill the attacker. Test whether the player changes direction or target priority for an understandable reason. Damage should connect visibly to the attack source. Counts, prediction, cadence, damage and warning style are unselected. First-person visibility and fair off-screen cues need evaluation; the existing synthetic boss area/burst harness is not accepted gameplay tuning. This is design inference from the source and P1's complaint, not a claim that MegaBonk uses this exact proposed sequence.

## Next changes

**Proceed/revise: revise before stacking. One next gameplay change: readable damage state**, scoped in [GDJ-16](https://linear.app/cnid/issue/GDJ-16/show-enemy-health-and-enemyplayer-damage-clearly). Add enemy HP bars and a brief enemy hit flash/impact, and verify a clear player damage cue. P1's latest feedback gives a concrete reason: recognizing wounded enemies should help deliberate group-kiting decisions. In the next standalone comparison, P1 should be able to identify a wounded drone after the first hit, its death after the second, and the moment the player takes damage. Inspect the existing 0.2-second red screen flash before deciding whether to strengthen or replace it. Keep presentation tied to actual damage, without changing combat rules.

The separately scoped [GDJ-15 rifle aim/tracer revision](https://linear.app/cnid/issue/GDJ-15/align-rifle-hitscan-and-tracer-feedback-during-fast-turns) preserves the earlier observed issue and source hypothesis; it is a follow-up to this latest damage-readability priority. Target-gated autofire, ammo/reload, jumping and the ranged-threat experiment remain saved requests. Their detailed rules and relative order are open. No rifle trait, upgrade pool or new weapon is assigned. GDJ-3 also waits for the scoped GDJ-16/GDJ-15 revisions, alongside the separate GDJ-1 preset decision. This iteration decision does not imply that every requested feature has been implemented.

## Completed handoff and next action

GDJ-2's required human attempts, control checks, concrete feedback and iteration decision are recorded. Preserve this build and worktree as the comparison control. Recheck any open StackLab process before launching another instance; its live log is under the recorded artifacts path. The next implementation task is GDJ-16, with a fresh standalone human comparison against this candidate. GDJ-15 and the other requests remain saved. Neither follow-up implementation was started by this report. This two-attempt developer check supports the next revision; it does not prove broad enjoyment, build variety or cooperative acceptance.
