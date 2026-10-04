# GDJ-15: immediate rifle feedback during fast turns

## Outcome

Make the existing rifle's displayed shot agree with an immediate crosshair hit while moving and turning. [GDJ-15](https://linear.app/cnid/issue/GDJ-15/align-rifle-hitscan-and-tracer-feedback-during-fast-turns) is selected next on October 4, 2026 after P1 accepts [GDJ-16's overhead-HP player](../../playtests/2026-10-03-overhead-health.md). This is a follow-up brief, not implementation.

## Context and evidence

P1 reported lagging shot lines, multiple lines when swiveling and a projectile-like rifle feel in the [baseline](../../playtests/2026-10-03-rifle-baseline.md). They asked for Soldier: 76-like immediate target hits. That selects hitscan feel, not aim assist or target locking. Authoritative `CombatSimulation.TryFire` already resolves ray/sphere damage in the firing tick.

On accepted runtime source `0deb52d8dd7e2790b1651985f8a57642b37ce8d2`, `PrototypeGame` calls `ArenaPresentation.ShowShot` before `Draw` updates the camera. `ShowShot` samples the existing weapon pose and displays one world-space LineRenderer for 45 ms. Stale origin and camera/interpolation mismatch are plausible contributors; the multiple-line symptom has not been causally reproduced. Inspect the actual firing/display frame rather than assuming the rifle has a traveling damage projectile or multiple renderers.

## Before implementation

Start from current merged main in an isolated checkout. Read AGENTS, foundation/planning/current state and the full Linear issue. Preserve the accepted v7 whole player at `C:/Users/Darian/.codex/worktrees/gdj-16-damage-feedback/new-game-dev/prototypes/001-stacked-weapons/build/StackLab-GDJ16-v7` and its [configuration/manifest](../../playtests/evidence/gdj-16-damage-v7/candidate.json); retain the original GDJ-2 control too. Reproduce fast yaw/pitch and movement while tracking a drone, then record the firing pose, displayed camera interpolation, muzzle origin, resolved endpoint and cosmetic lifetime. Finalize one small interaction/timing contract before code changes.

## Done when

- Camera aim, resolved endpoint, muzzle/tracer origin and immediate hit cue agree for the displayed firing frame, including fast turns and movement.
- The reported extra-line appearance has an observed reproduction and a readable correction; a brief tracer does not imply damage travel or retain an unexplained stale origin.
- Damage/cadence remain authoritative and simulation-timed. Cosmetic lifetime cannot change hit selection. Preserve wounded overhead HP, pose and independent hit/kill/player cues.
- Pause/restart clears transient shot feedback and retains accepted run rules. Preserve the benchmark workload or explicitly retain a reference path for comparable captures.
- Run focused regression/render checks with the pinned serialized Unity tooling, build an exact standalone and record P1's fast-turn/tracking comparison against the preserved accepted player. Tests and screenshots alone do not establish shooting feel.

Resolve this foundation before GDJ-17 target coloring and pose-aligned ×2 critical regions. GDJ-18 customization/sharing, ammo/reload, target-gated autofire, jumping, ranged threats and progression remain separate. No external asset copying, automatic target lock, shader/backend promotion or unverified weapon tuning is required.
