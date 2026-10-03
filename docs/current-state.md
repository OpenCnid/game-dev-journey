# Current state

Updated October 3, 2026. This is an orientation page, not a copy of the task board. Update it when the playable capability, accepted configuration or a major constraint changes.

Start a fresh session with the [project foundation](foundation.md), [planning handoff](planning/README.md), then this page and the selected task's relevant context. The foundation defines the game; this page reports how much of it exists. Planning and task sessions write durable outcomes back before ending.

## What exists

The active Unity project is [prototypes/001-stacked-weapons](../prototypes/001-stacked-weapons/README.md). Unity **6000.3.25f1** and its packages are pinned. `game/` is not the active project.

Gameplay is a **local, one-player** one-minute arena with one automatically firing rifle, movement/aim, health, feedback, pause/restart and a run-ending state. Networking, cooperative sessions, teammate interactions and AI-world presentation are not implemented. Three simultaneous weapons, pickups and upgrades are not implemented yet. Human observations of fun and readability are still outstanding.

The stress harness supports up to **20,000 synthetic ability-heavy bosses**. Jobs/Burst simulation and culled instancing are defaults. Native state, compact indirect projectiles and combined character rendering remain selectable experiments; a combined development preset has not been promoted.

## What the evidence supports

The latest code/report baseline is `7414fac`. The [September 30 renderer report](playtests/2026-09-30-renderer-experiments.md) records 35 passing automated tests and three full combined-renderer captures at 20,000 synchronized bosses: **400–468 average FPS**, **3.743–4.297 ms p99**, with equal gameplay work and no dropped projectiles/catch-up limit events. These are historical results, not checks rerun by this documentation change.

This establishes a local synthetic percentile result. It does not establish multiplayer synchronization/latency, final-game performance, a hard every-frame minimum, older-hardware support or enjoyment. Warning caching had no isolated win; overlapping preparation had no reliable overall win. Keep optimization work tied to measured costs.

## Direction and constraints

The accepted product is **cooperative PvE for 2-4 players, testing two first**, with The Engine Kingdoms as background. Immediate combat starts with one gun and grows through shared XP and independent personal random choices/pool modifiers; draws may overlap between teammates. Selecting a qualifying high-rarity upgrade grants it to everyone and credits the original player; appearing in an offer alone does not grant it. Upgrades must accommodate future weapon/loot/draw effects; [decision 0003](decisions/0003-shared-xp-personal-upgrade-draws.md) and [progression design](design/progression-and-upgrade-pools.md) record these commitments. **Party thinking space is accepted for later work** in [decision 0004](decisions/0004-party-thinking-space.md): planned breaks plus earlier invocation, unlimited mental choices during physical freeze, and readiness-based resume. The milestone and [GDJ-7](https://linear.app/cnid/issue/GDJ-7/build-party-thinking-space-for-cooperative-upgrades) track implementation. None of these systems is implemented yet. Qualifying rare tiers/applicability, detailed cadence/loadout/draw behavior and lifecycle remain open in the [encounter record](design/engine-kingdoms-first-encounter.md) and [brief](planning/tasks/GDJ-6.md). Further lore and special combinations are deferred; previous world choices remain in [decision 0002](decisions/0002-cooperative-pve-ai-world.md). Minimal two-player combat/progression feasibility precedes broader expansion. Preserve existing local evidence; live priority/status belong in [Linear](https://linear.app/cnid/project/first-weapon-stacking-playtest-89b5952f722e). See [workflow](workflow.md), [roadmap](roadmap.md) and [game brief](design/game-brief.md).

The development benchmark machine is **i7-10700K / RTX 3090 / approximately 64 GB RAM**, using the existing 1080p protocol. The goal remains 144 FPS or higher in the eventual supported encounter. GTX 1080 Ti/older-CPU testing is deferred until the game is more polished. [Performance policy](performance.md) and [profiling](profiling.md) define the checks.

Foundation revision 11 retains [decisions 0005](decisions/0005-technology-magic-and-cultivation.md)/[0006](decisions/0006-capacity-mastery-and-prepared-power.md): advanced AI/technology, mage magic and murim/dantian cultivation in one gun, with usable baseline prepared power, capacity expanded through earned upgrades and personal mastery that strengthens/specializes expression. [GDJ-8](https://linear.app/cnid/issue/GDJ-8/develop-hybrid-gun-capacity-mastery-and-prepared-power-rules) holds later detailed design. Costs, progression mappings and compatibility remain open. No technological, magic or cultivation gameplay system is implemented by this design checkpoint.

[Decision 0007](decisions/0007-roguelike-run-identity-and-camera-exploration.md) adds roguelike/roguelite identity, Risk of Rain/Megabonk references and accepted persistent weapon/upgrade discoveries. None of that persistence is implemented. The local prototype remains first person. Third-person support/switching, exact unlock/reset/ownership rules and permanent power remain open. The [permanent matrix](design/permanent-progression-matrix.md) is an exploration of the owner's tower-defense/Path of Exile inspiration.
