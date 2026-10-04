# GDJ-15: immediate rifle feedback during fast turns

## Outcome

Make the existing rifle's displayed shot agree with an immediate crosshair hit while moving and turning. [GDJ-15](https://linear.app/cnid/issue/GDJ-15/align-rifle-hitscan-and-tracer-feedback-during-fast-turns) was selected on October 4, 2026 after P1 accepted [GDJ-16's overhead-HP player](../../playtests/2026-10-03-overhead-health.md). Implementation follows the investigation and timing contract below; human acceptance remains pending.

## Context and evidence

P1 reported lagging shot lines, multiple lines when swiveling and a projectile-like rifle feel in the [baseline](../../playtests/2026-10-03-rifle-baseline.md). They asked for Soldier: 76-like immediate target hits. That selects hitscan feel, not aim assist or target locking. Authoritative `CombatSimulation.TryFire` already resolves ray/sphere damage in the firing tick.

On accepted runtime source `0deb52d8dd7e2790b1651985f8a57642b37ce8d2`, `PrototypeGame` calls `ArenaPresentation.ShowShot` before `Draw` updates the camera. `ShowShot` samples the existing weapon pose and displays one world-space LineRenderer for 45 ms. Stale origin and camera/interpolation mismatch are plausible contributors; the multiple-line symptom has not been causally reproduced. Inspect the actual firing/display frame rather than assuming the rifle has a traveling damage projectile or multiple renderers.

## Investigation and timing contract — October 4

Before runtime edits, the [control regression source](../../playtests/evidence/gdj-15-shot-v1/control-regression.cs.txt) on main `076a98c` reproduced a 70-degree yaw / -20-degree pitch turn plus one movement tick. The [Unity result](../../playtests/evidence/gdj-15-shot-v1/control-regression.xml) reports a **1.319243 m** gap between tracer start and current muzzle, and an old world line still enabled after the next turn. Exactly one LineRenderer exists. This reproduces stale shot geometry across turning frames; P1's perceived multiple-line appearance still needs a new human comparison. No traveling damage projectile or duplicated renderer was found.

Implementation contract, recorded before runtime changes:

- Advance fixed simulation ticks first. In normal combat only, prepare the interpolated camera once and consume due rifle shots using that camera ray and the same interpolated enemy positions. Health, nearest-hit selection, 20 damage, 65 m range and 0.12 s cadence remain simulation-owned. Consume accumulated cooldown debt only on frames that advanced simulation; render-only frames cannot create extra shots. Catch-up shots resolve in order against one displayed pose, without adding lines.
- Render each combat tracer immediately from the current recoiled muzzle to the resolved endpoint, for its emission frame only. Later camera turns must not drag an old line or re-aim its old endpoint. Age existing effects before new events so a long frame cannot erase a newly emitted shot. The white muzzle pulse keeps its 45 ms cosmetic duration; hit/death/player cues and wounded overhead HP retain their accepted rules. Pause hides the transient tracer, freezes remaining muzzle/health feedback, and restart clears both.
- Keep the benchmark's original per-tick firing, non-interpolated hit query and 45 ms legacy tracer. No benchmark renderer preset, package, workload or performance claim changes. Read-only HP aim selection uses the same displayed pose as combat. Wound impact anchors use the queried incarnation's displayed position, avoiding a second interpolation offset; death snapshots remain fixed before respawn.
- Later precision/critical regions must replace the current approximate sphere with pose-aligned queries. Future weapons must retain their own authoritative shot results/cooldowns; this one-rifle cosmetic line is not a general weapon rack or delayed-damage system. Networking/rewind, ammo, target-gated fire and imports remain separate.

## Before implementation

Start from current merged main in an isolated checkout. Read AGENTS, foundation/planning/current state and the full Linear issue. Preserve the accepted v7 whole player at `C:/Users/Darian/.codex/worktrees/gdj-16-damage-feedback/new-game-dev/prototypes/001-stacked-weapons/build/StackLab-GDJ16-v7` and its [configuration/manifest](../../playtests/evidence/gdj-16-damage-v7/candidate.json); retain the original GDJ-2 control too. Reproduce fast yaw/pitch and movement while tracking a drone, then record the firing pose, displayed camera interpolation, muzzle origin, resolved endpoint and cosmetic lifetime. Finalize one small interaction/timing contract before code changes.

## Done when

- Camera aim, resolved endpoint, muzzle/tracer origin and immediate hit cue agree for the displayed firing frame, including fast turns and movement.
- The reported extra-line appearance has an observed reproduction and a readable correction; a brief tracer does not imply damage travel or retain an unexplained stale origin.
- Damage/cadence remain authoritative and simulation-timed. Cosmetic lifetime cannot change hit selection. Preserve wounded overhead HP, pose and independent hit/kill/player cues.
- Pause/restart clears transient shot feedback and retains accepted run rules. Preserve the benchmark workload or explicitly retain a reference path for comparable captures.
- Run focused regression/render checks with the pinned serialized Unity tooling, build an exact standalone and record P1's fast-turn/tracking comparison against the preserved accepted player. Tests and screenshots alone do not establish shooting feel.

Resolve this foundation before GDJ-17 target coloring and pose-aligned ×2 critical regions. GDJ-18 customization/sharing, ammo/reload, target-gated autofire, jumping, ranged threats and progression remain separate. No external asset copying, automatic target lock, shader/backend promotion or unverified weapon tuning is required.

## Resume — technical candidate ready

Runtime source `d93e867837915c9dae4a5b68947a7ecdfad703d3` implements the contract on `feat/gdj-15-shot-alignment` in the reused managed GDJ-16 checkout. [The October 4 report](../../playtests/2026-10-04-shot-alignment.md) records exact build/control archives, failing control reproduction, passing query/cadence/render checks and startup evidence. Human fast-turn/tracking comparison remains required; retain GDJ-15 in review and preserve accepted v7. Do not merge or move on to critical regions based on automated passes alone.
