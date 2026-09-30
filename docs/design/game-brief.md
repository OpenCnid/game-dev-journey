# Game brief

**Direction:** an original first-person horde-survival game inspired by playing Vampire Hunters.

**Confirmed priority:** stacking weapons and creating powerful builds.

**Late-game workload:** more than 10,000 active enemies, potentially all bosses with special abilities. The [boss workload specification](late-game-workload.md) defines synthetic tests up to 20,000 while the final encounter envelope remains open.

**Confirmed performance requirement:** at least 144 FPS within the supported hardware/settings and encounter envelope, with higher caps and uncapped rendering available. The benchmark CPU is a stock Intel Core i7-8700K, paired with the owner's provisional GTX 1080 Ti GPU baseline; 1080p is a proposed starting resolution. The [performance plan](../performance.md) defines the measurements; remaining system settings and peak load are not finalized and the target is not yet verified.

**Status:** the one-weapon combat slice and crowd benchmark are implemented. The three-weapon build and upgrade loop below remain proposed follow-up work.

## Experience to test

Build a growing arsenal while maneuvering through a crowd. Each added weapon should feel like a meaningful increase in power. Different upgrade paths should produce recognizable strengths, rather than merely increasing one damage number.

The [reference study](vampire-hunters-study.md) distinguishes the source game's advertised features from our hypotheses.

## Proposed first playable

| Question | Proposed answer |
| --- | --- |
| Working title | Undecided; use `stacked-weapons` as the experiment name. |
| Pitch | Survive a short arena encounter as a weapon rig grows from one firing module to three. |
| Main action | Move, aim, and combine weapons and upgrades into a powerful build. |
| Core loop | Defeat enemies -> collect energy -> choose an upgrade -> face increasing pressure. |
| Firing | Automatic firing on independent timers; player controls aim. Test this assumption early. |
| Objective and ending | Survive five minutes; lose when health reaches zero; restart from either result. |
| Engine and language | Unity 6000.3.25f1, C#, URP 17.3.0; exact dependencies in the project manifest/lockfile. |
| Initial platform and input | Windows desktop, keyboard and mouse. |
| Content limit | One graybox arena, one player, one approaching enemy type, three weapon behaviors, six upgrades. |
| Presentation | Placeholder geometry, distinct shot feedback, readable enemy and damage cues. Theme remains open. |

All counts, timings, and rules here are our starting test settings, not reconstructed Vampire Hunters values.

## First question and stopping condition

**Question:** does growing from one to three simultaneous weapons create a satisfying increase in power, with at least two upgrade paths that feel different?

First build a 60-second combat test with one weapon and one enemy type. Stop and adjust if movement, aiming, or feedback feels poor. Only then add stacking and the five-minute loop.

Run repeatable crowd benchmarks alongside these slices. A fun small encounter alone does not demonstrate that crowded, fully upgraded combat meets the frame budget.

Stop expanding the prototype after three short documented playtests. Decide to continue, revise, or discard the approach based on whether players notice weapon differences, can describe their build's strengths, and want to try another combination. Three sessions guide the next iteration; they do not establish broad market demand.

## Deferred

Additional arenas, a full arsenal, finished boss content, character classes, permanent unlocks, story, polished art, multiplayer, and storefront integration. Synthetic boss ability workloads are included now for performance investigation. The [prototype specification](../../prototypes/001-stacked-weapons/README.md) defines the proposed technical slices and checks.

## Decisions still needed

- What do the initial combat playtests and target-hardware captures tell us to change before adding weapons?
- What do focused play sessions reveal about the weapon combinations we enjoy most?
- What original theme and signature interaction should distinguish the finished game?

Automated checks and developer-machine benchmark diagnostics are recorded separately from player enjoyment observations. Reference-game observations and user playtests remain outstanding.
