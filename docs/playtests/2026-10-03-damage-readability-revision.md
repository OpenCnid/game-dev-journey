# Candidate: attached enemy health bars and wounded silhouettes

- **Date:** October 3, 2026 (America/Chicago).
- **Issue / review:** [GDJ-16](https://linear.app/cnid/issue/GDJ-16/show-enemy-health-and-enemyplayer-damage-clearly), [draft PR #4](https://github.com/OpenCnid/game-dev-journey/pull/4).
- **Candidate:** `GDJ-16-damage-v3-2026-10-03`, release Windows player from clean source `34f5244648d4df77090bd3b9d0e59c71697c424e`.
- **Question:** can the player identify bar ownership and wounded enemies in a crowd without floating rows of labels?
- **Disposition:** P1 rejected the attached-bar ownership/readability approach as cluttered and flickering. Revise selection/target presentation before acceptance. No new performance result.

## Evidence driving the revision

The [first candidate report](2026-10-03-damage-readability.md) records P1 recognizing a first-hit wound, death through particles, and clear player damage/death. Death sparks were small, rapid-fire white cues were masked by gold, and clustered bars merged into strips. Kiting benefit was not explicitly confirmed.

Intermediate source `35ba4bb3c75fbed811137c28d95160eeb07f8fcc` added a wounded pose, independent hit/kill markers, larger death bursts, muzzle pulses and twelve labels with upward offsets. P1 liked the wounded shape but still found the grouped bars hard to recognize in their [floating-label screenshot](evidence/gdj-16-damage/human-floating-bars-35ba.png). They asked whether the apparent split affects hitboxes or requires more complex geometry. Technical QA also caught [misplaced HUD markers](evidence/gdj-16-damage/pre-fix-marker-offset-35ba.png) under the 1080p GUI scale. Both findings prompted this replacement candidate.

## Resulting behavior and limits

- The nearest enemy intersected by the shared read-only aim ray gets a wider white-bordered HP bar. Wounded enemies get smaller amber bars at their own core position, ordered by distance. Healthy background labels are hidden.
- Up to 64 projected candidates within 40 metres are considered; at most six labels appear. If a label overlaps a retained higher-priority label, it is suppressed. Bars never shift into floating rows or combine different enemies' health. A short connector ends at the actual posed core.
- A wounded body/core remains skewed and compressed until death/reset. This is a simple visual transformation, not separate physics pieces. The existing sphere at body height remains an approximate hit volume and does not match every visible edge. Retain the simple cue for this prototype; [GDJ-17](https://linear.app/cnid/issue/GDJ-17/highlight-aimed-enemies-and-add-2-critical-head-hits) must align head/body regions with the visible pose before accepting precision critical hits.
- White hit markers and outer gold kill markers have separate timers/positions. Gold death sparks are larger and last 0.5 seconds; every shot has a brief white muzzle pulse. GUI rotation now composes around logical coordinates before screen scaling. Player damage cues retain the first candidate's behavior.

The [task contract](../planning/tasks/GDJ-16.md#second-human-revision-contract) preserves simulation ownership and target incarnation. Damage remains immediate at 20 per shot against 40 HP, firing every 0.12 simulation seconds. Physical crowd separation, movement, contact grace, pause/reset and encounter configuration are unchanged. Reference and culled render paths share the wound transform; synthetic benchmarks omit the new feedback/pose. No target-gated firing or critical damage is implemented here.

Suppressing labels trades complete simultaneous HP display for identifiable nearby state. Wounded silhouettes remain available when bars are suppressed. Overlapping bodies, occlusion, label switching near equal distances and future ranged threat readability still need assessment; this flat arena does not establish through-wall visibility or 20,000-enemy presentation cost.

## Exact player and reproduction

- Worktree: `C:\Users\Darian\.codex\worktrees\gdj-16-damage-feedback\new-game-dev`; branch `feat/gdj-16-damage-feedback`.
- Embedded source: `34f5244648d4df77090bd3b9d0e59c71697c424e`, built `2026-10-04T03:54:52.4248649Z`, dirty `false`. Source/time were checked in the player resources.
- Unity `6000.3.25f1` (`e1dba0a9aba4`), pinned packages, release Windows x64 Mono / Direct3D 11.
- 35 drones, 60 seconds, seed 137; staged Jobs/Burst, culled instances, separate character meshes, cube projectiles; 1920×1080 windowed, 144 FPS cap, VSync off.
- Whole build: `prototypes/001-stacked-weapons/build/StackLab` in that worktree, 166 files / 96,911,586 bytes. Preserve/distribute the whole folder.
- [Configuration](evidence/gdj-16-damage-v3/candidate.json), [complete SHA-256 manifest](evidence/gdj-16-damage-v3/build-manifest.json), [startup log](evidence/gdj-16-damage-v3/standalone-startup.txt), [three-second runtime capture](evidence/gdj-16-damage-v3/standalone-run.png).

Build an isolated checkout of the source revision with `.\tools\unity.ps1 -Action Build`, Editor and output player closed. Launch at its menu:

```powershell
$candidateRoot = Join-Path (Get-Location).Path 'prototypes/001-stacked-weapons'
$candidatePlayer = Join-Path $candidateRoot 'build/StackLab/StackLab.exe'
$candidateLog = Join-Path $candidateRoot 'artifacts/GDJ-16-v3-player.log'
$candidateScreenshot = Join-Path $candidateRoot 'artifacts/GDJ-16-v3-human-run.png'
Start-Process -FilePath $candidatePlayer -ArgumentList @(
    '-seed', '137', '-simulation', 'burst', '-rendering', 'culled',
    '-screen-width', '1920', '-screen-height', '1080', '-screen-fullscreen', '0',
    '-logFile', ('"' + $candidateLog + '"'),
    '-screenshot', ('"' + $candidateScreenshot + '"')
)
```

Focus the newest Stack Lab window and press R or choose Start / Restart Combat. Move with WASD and aim with the mouse; firing remains automatic. Escape pauses/resumes; focus loss pauses. The capture is made after three simulation seconds and is technical context, not proof of a complete active attempt.

The exact GDJ-2 control remains untouched in its separate worktree. Earlier GDJ-16 players are preserved locally as complete `build/StackLab-GDJ16-v1` and `build/StackLab-GDJ16-v2` folders. The intermediate archive's 166 hashes match its pre-fix manifest saved under ignored `artifacts/GDJ-16-v2-pre-fix-evidence`.

## Checks and remaining comparison

- **45/45 Edit Mode passes:** [results](evidence/gdj-16-damage-v3/EditMode.xml). Checks cover authoritative hit/health/respawn across managed/Burst/native state, independent rapid-fire cues, player lifecycle, wounded matrices, core anchors and HUD rotation at 1×/1.5×/2× scale. A dense crowd fixture wounds multiple enemies and verifies that overlapping eligible labels yield space, retained bars do not overlap, target priority is preserved, and layout neither shifts static labels nor changes combat state.
- **3/3 Play Mode passes:** [results](evidence/gdj-16-damage-v3/PlayMode.xml). Wound/death rendering through reference/culled paths, HUD invocation, pause/reset and benchmark omission pass, along with existing runtime smoke/renderer checks. The later test-fixture change did not change runtime code.
- A first build attempt encountered a locked Burst DLL because the previous player was still open. After closing that exact candidate, the fresh release build succeeded. The played intermediate build was archived before replacement.
- Standalone startup loaded on Direct3D 11 / RTX 3090 without reported runtime exceptions. The three-second capture shows the gold marker centered on the crosshair and the white muzzle pulse. It contains no wounded crowd and cannot establish bar ownership. Its visible FPS number is not a performance capture.
- All 166 new player file hashes and the manifest fingerprint verified. Documentation paths and whitespace were checked before the evidence commit.

P1 answered the focused comparison: they could not identify the wide white-bordered bar's owner and found the scene really cluttered. Wounded bars helped, they thought, but labels remained confusing and flickered a lot; they specifically raised the concern that this would be worse on higher-health enemies. This rejects the current ownership/selection approach. Move target HP into a fixed HUD location with matching body brackets, retain wounded-label owners across distance/aim changes and delay reappearance after overlap. Keep the issue In Progress for that correction. Kiting usefulness and the replacement layout still need comparison. Do not ask P1 to reconfirm already established first-candidate player damage/death unless a regression appears.

PR #4 remains stacked on open draft baseline PR #3. Retarget it after #3 merges. Tracer alignment remains GDJ-15; crosshair color and 2× critical hits remain GDJ-17. Ammo, jumping, target-gated autofire, ranged enemies and progression remain saved follow-ups.
