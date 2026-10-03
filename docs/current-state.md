# Current state

Updated October 2, 2026. This is an orientation page, not a copy of the task board. Update it when the playable capability, accepted configuration or a major constraint changes.

Start a fresh session with the [project foundation](foundation.md), [planning handoff](planning/README.md), then this page and the selected task's relevant context. The foundation defines the game; this page reports how much of it exists. Planning and task sessions write durable outcomes back before ending.

## What exists

The active Unity project is [prototypes/001-stacked-weapons](../prototypes/001-stacked-weapons/README.md). Unity **6000.3.25f1** and its packages are pinned. `game/` is not the active project.

Gameplay is a **local, one-player** one-minute arena with one automatically firing rifle, movement/aim, health, feedback, pause/restart and a run-ending state. Networking, cooperative sessions, teammate interactions and AI-world presentation are not implemented. Three simultaneous weapons, pickups and upgrades are not implemented yet. Human observations of fun and readability are still outstanding.

The stress harness supports up to **20,000 synthetic ability-heavy bosses**. Jobs/Burst simulation and culled instancing are defaults. Native state, compact indirect projectiles and combined character rendering remain selectable experiments; a combined development preset has not been promoted.

## What the evidence supports

The latest code/report baseline is `7414fac`. The [September 30 renderer report](playtests/2026-09-30-renderer-experiments.md) records 35 passing automated tests and three full combined-renderer captures at 20,000 synchronized bosses: **400–468 average FPS**, **3.743–4.297 ms p99**, with equal gameplay work and no dropped projectiles/catch-up limit events. These are historical results, not checks rerun by this documentation change.

This establishes a local synthetic percentile result. It does not establish multiplayer synchronization/latency, final-game performance, a hard every-frame minimum, older-hardware support or enjoyment. Warning caching had no isolated win; overlapping preparation had no reliable overall win. Keep optimization work tied to measured costs.

## Direction and constraints

The accepted product is **cooperative PvE for 2-4 players, testing two first**, with The Engine Kingdoms as background. The owner's latest priority is immediate combat: start with one gun, earn levels, choose improvements and grow a large simultaneous weapon stack. Further lore, scripted introductions and special-combination selection are deferred. Prior world choices remain in [decision 0002](decisions/0002-cooperative-pve-ai-world.md) and the [world comparison](design/ai-world-options.md). The developing [encounter record](design/engine-kingdoms-first-encounter.md) and [session brief](planning/tasks/GDJ-6.md) describe the current simple progression proposal; rewards, selection timing and team lifecycle rules still require evaluation. These are design commitments, not new playable capabilities. Minimal two-player shared-combat/level-up feasibility precedes broader expansion. The one-versus-three weapon and five-minute progression experiments require cooperative re-scoping; local pause/restart and upgrade rules are not accepted multiplayer rules. Preserve existing rifle/preset evidence. Live priority/status belong in the [Linear project](https://linear.app/cnid/project/first-weapon-stacking-playtest-89b5952f722e). See [workflow](workflow.md), [roadmap](roadmap.md) and [game brief](design/game-brief.md).

The development benchmark machine is **i7-10700K / RTX 3090 / approximately 64 GB RAM**, using the existing 1080p protocol. The goal remains 144 FPS or higher in the eventual supported encounter. GTX 1080 Ti/older-CPU testing is deferred until the game is more polished. [Performance policy](performance.md) and [profiling](profiling.md) define the checks.
