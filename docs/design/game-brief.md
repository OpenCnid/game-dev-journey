# Game brief

The [project foundation](../foundation.md) defines the game we are building, its player experience and core systems. This brief turns that definition into a testable design; its proposed mechanics, counts and timings can change within that foundation. Current implementation facts are summarized in [current state](../current-state.md).

**Direction:** an original cooperative multiplayer PvE first-person horde-survival game, designed for 2-4 players with two-player testing first, inspired by playing Vampire Hunters.

**World premise:** The Engine Kingdoms, selected by the owner: a failing system accidentally pulls outsiders into a physical AI-maintained magical world where kingdoms, people and magic are real and teammates can combine forbidden powers. They initially seek survival and a way home. The AI tries to prevent another catastrophe; forbidden combinations threaten the world, but outsiders may need them to save it. [Decision 0002](../decisions/0002-cooperative-pve-ai-world.md) records the accepted direction; the [world comparison](ai-world-options.md) preserves setting alternatives and proposed mechanics, and the [first encounter record](engine-kingdoms-first-encounter.md) preserves the developing world discussion. Design one small cooperative encounter before implementation.

**Confirmed priority:** enter combat with one gun, level up, choose improvements and grow a large simultaneous weapon stack. The owner wants immediate gameplay; further lore and scripted introductions are deferred. The selected world is background context.

**Late-game workload:** more than 10,000 active enemies, potentially all bosses with special abilities. The [boss workload specification](late-game-workload.md) defines synthetic tests up to 20,000 while the final encounter envelope remains open.

**Confirmed performance requirement:** at least 144 FPS within the supported hardware/settings and encounter envelope, with higher caps and uncapped rendering available. Benchmark current development on the owner's i7-10700K / RTX 3090 desktop with approximately 64 GB RAM, retaining the existing 1080p configuration for comparisons. GTX 1080 Ti and older-CPU testing comes later, once the game is more polished. The [performance plan](../performance.md) defines the measurements; final visual settings and peak load are not finalized and the full gameplay target is not yet verified.

**Status:** the local one-weapon combat slice and crowd benchmark are implemented. Networking, cooperative combat and world presentation are not implemented. The earlier local three-weapon/upgrade proposal below needs cooperative re-scoping before implementation; its counts and mechanics are experiments, not accepted multiplayer rules.

## Experience to test

Build a growing arsenal while maneuvering through a crowd with teammates. Each added weapon should feel like a meaningful increase in power. Different upgrade paths should produce recognizable strengths and useful team contributions, rather than merely increasing one damage number.

The [reference study](vampire-hunters-study.md) distinguishes the source game's advertised features from our hypotheses.

## Cooperative design and feasibility gate

Describe a 60-second two-player survival encounter that starts each player with one gun and provides earned level-up choices to grow the arsenal. Show a readable contribution from each player's build, such as one clearing approaching enemies while the other covers another direction or protects a teammate. A special combination and narrative escape objective are deferred. Resolve progression and team lifecycle rules needed for that encounter, then scope one minimal networked-combat feasibility task: consistent player/enemy state, hits, level rewards and chosen upgrades, shared ending/restart and responsive player control. Hosting, networking package, replication approach, latency/bandwidth targets and supported crowd count require explicit investigation; no library or topology is selected yet.

Cooperative progression must decide personal/shared rewards, how upgrade choices occur while teammates are active, downed/revive behavior and team wipe/win conditions. Existing local global pause and player-death precedence do not answer those questions. Keep this gate small; richer classes, infrastructure and content follow evidence.

## Earlier local playable proposal

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
| Content limit | Earlier local test: one graybox arena, one player, one approaching enemy type, three weapon behaviors, six upgrades; cooperative scope to be revised. |
| Presentation | Placeholder geometry, distinct shot feedback, readable enemy and damage cues; specific AI-world style to be selected. |

All counts, timings, and rules here are our starting test settings, not reconstructed Vampire Hunters values.

## First question and stopping condition

**Question:** does growing from one to three simultaneous weapons create a satisfying increase in power, with at least two upgrade paths that feel different?

The existing local 60-second combat test remains a control for movement, aiming and feedback. The cooperative direction adds a two-player shared-combat gate before broad weapon/progression implementation. Stop and adjust if player response, state consistency or readable contribution is poor. Only then expand stacking and the proposed five-minute loop under the revised rules.

Run repeatable crowd benchmarks alongside these slices. A fun small encounter alone does not demonstrate that crowded, fully upgraded combat meets the frame budget.

Stop expanding the prototype after three short documented playtests. Decide to continue, revise, or discard the approach based on whether players notice weapon differences, can describe their build's strengths, and want to try another combination. Three sessions guide the next iteration; they do not establish broad market demand.

## Deferred

Additional arenas, a full arsenal, finished boss content, character classes, permanent unlocks, extensive story content, polished art, public matchmaking/account services and storefront integration. Cooperative multiplayer is core scope. Synthetic boss ability workloads remain local performance controls. The [prototype specification](../../prototypes/001-stacked-weapons/README.md) preserves the earlier local slices and checks, pending cooperative re-scoping.

## Decisions still needed

- What do the initial combat playtests and target-hardware captures tell us to change before adding weapons?
- What do focused play sessions reveal about the weapon combinations we enjoy most?
- Which level-up choices make the growing stack feel powerful and create distinct builds? Further lore and special rule-breaking mechanics are deferred.
- Which cooperative rules and networking approach can support the selected encounter with readable, responsive shared combat?

Automated checks and developer-machine benchmark diagnostics are recorded separately from player enjoyment observations. Reference-game observations and user playtests remain outstanding.
