# Game brief

**MVP/alpha priority clarification, October 3:** keep the first-person cooperative run bounded. The permanent matrix/preparation/power proposals and optional third-person/dual-camera support are parked beyond MVP/alpha as extremely low-priority ideas in [GDJ-9](https://linear.app/cnid/issue/GDJ-9/idea-permanent-progression-matrix)/[GDJ-10](https://linear.app/cnid/issue/GDJ-10/idea-optional-first-and-third-person-views). Persistent discoveries remain a future direction; a full meta system is not an initial-run requirement.

The [project foundation](../foundation.md) defines the game we are building, its player experience and core systems. This brief turns that definition into a testable design; its proposed mechanics, counts and timings can change within that foundation. Current implementation facts are summarized in [current state](../current-state.md).

**Direction:** an original bullet-heaven survival roguelite for 1-4 players, solo or cooperative PvE, with two-player cooperative testing first. [Decision 0008](../decisions/0008-bullet-heaven-solo-play-and-core-experience.md) records quick entry, pleasant level-ups, future non-kill progression possibilities and performance as the core priority. Exact run structure and detailed weapon-growth taxonomy remain open; inner energy is a player-facing naming recommendation. References include Vampire Hunters, Risk of Rain and Megabonk. First person is the current prototype view; third person alongside it is a proposal to evaluate. [Decision 0007](../decisions/0007-roguelike-run-identity-and-camera-exploration.md) records this clarification and persistent weapon/upgrade discoveries. Exact unlock/reset/ownership rules and permanent power remain open. The [permanent matrix exploration](permanent-progression-matrix.md) preserves the owner's tower-defense/Path of Exile inspiration as a proposal.

**World premise:** The Engine Kingdoms, selected by the owner: a failing system accidentally pulls outsiders into a physical AI-maintained magical world where kingdoms, people and magic are real and teammates can combine forbidden powers. They initially seek survival and a way home. The AI tries to prevent another catastrophe; forbidden combinations threaten the world, but outsiders may need them to save it. [Decision 0002](../decisions/0002-cooperative-pve-ai-world.md) records the accepted direction; the [world comparison](ai-world-options.md) preserves setting alternatives and proposed mechanics, and the [first encounter record](engine-kingdoms-first-encounter.md) preserves the developing world discussion. Describe a small solo/cooperative core test before implementation; short durations are test settings, not a final-game format.

**Confirmed priority:** enter combat quickly with one gun, survive, enjoy leveling up, choose improvements and grow a large simultaneous weapon stack. Advanced AI/technology, mage-level-dependent magic and murim/dantian cultivation coexist, and mixing all three in one gun is central to builds. [Decision 0005](../decisions/0005-technology-magic-and-cultivation.md) records that world direction. [Decision 0006](../decisions/0006-capacity-mastery-and-prepared-power.md) accepts usable baseline prepared power, capacity expanded through earned upgrades and personal mastery that strengthens/specializes expression. Detailed rules will be developed later in [GDJ-8](https://linear.app/cnid/issue/GDJ-8/develop-hybrid-gun-capacity-mastery-and-prepared-power-rules). Extensive history and scripted introductions remain deferred.

**Accepted progression:** shared team XP with independent personal random upgrade choices and player-specific pools/modifiers; teammate draws may overlap. Selecting a legendary or other qualifying high-rarity upgrade grants it to the team and credits its original player; appearance in an offer alone does not grant it. Qualifying tiers and cross-build applicability remain open. Upgrades must accommodate future effects on loot and draws as well as weapons. [Decision 0003](../decisions/0003-shared-xp-personal-upgrade-draws.md) and [progression design](progression-and-upgrade-pools.md) distinguish accepted direction from proposed draw/grant rules and undefined magic-find semantics.

**XP and death clarification:** [decision 0010](../decisions/0010-xp-pickups-and-death-participation.md) accepts enemy-drop XP orbs requiring pickup, shared team credit on collection, magnet collection and percentage pickup-range upgrades. Dead players keep gear-upgrade access through earned choices and should remain involved and return to combat. Timed revival/lives/eligibility and solo/full-party endings remain to research in future High-priority [GDJ-13](https://linear.app/cnid/issue/GDJ-13/research-death-revival-and-continued-player-participation); shared-rare applicability/bookkeeping belongs to future [GDJ-14](https://linear.app/cnid/issue/GDJ-14/research-shared-legendary-compatibility-and-reward-rules). Exact timers and grant policies are not selected.

**Accepted future selection feature:** [party thinking space](../decisions/0004-party-thinking-space.md) combines banked choices at planned breaks with earlier invocation by a player holding an unspent choice. Physical combat/time freezes for unlimited mental upgrade/loadout decisions, and resumes when all active players are ready. The Party thinking space milestone and [GDJ-7](https://linear.app/cnid/issue/GDJ-7/build-party-thinking-space-for-cooperative-upgrades) preserve later work; no implementation is requested now. Exact cadence, presentation, loadout/respec scope and detailed draw/lifecycle handling remain open.

**Control/readability direction:** [decision 0009](../decisions/0009-weapon-control-and-accessible-combat.md) accepts configurable auto-shoot, crosshair/selected/guided targeting and through-wall enemy visibility. Automatic secondary/passive activation is a possible later extension. The [control/readability record](weapon-control-and-combat-readability.md) distinguishes firing, targeting and presentation and links future GDJ-11 positioning and High-priority GDJ-12 accessibility research. Exact settings, default target priorities and outline range/style remain open; attacks remain prohibited during thinking space.

**Late-game workload:** more than 10,000 active enemies, potentially all bosses with special abilities. The [boss workload specification](late-game-workload.md) defines synthetic tests up to 20,000 while the final encounter envelope remains open.

**Confirmed performance requirement:** at least 144 FPS within the supported hardware/settings and encounter envelope, with higher caps and uncapped rendering available. Benchmark current development on the owner's i7-10700K / RTX 3090 desktop with approximately 64 GB RAM, retaining the existing 1080p configuration for comparisons. GTX 1080 Ti and older-CPU testing comes later, once the game is more polished. The [performance plan](../performance.md) defines the measurements; final visual settings and peak load are not finalized and the full gameplay target is not yet verified.

**Status:** the local one-weapon combat slice and crowd benchmark are implemented. Networking, cooperative combat and world presentation are not implemented. The earlier local three-weapon/upgrade proposal below needs cooperative re-scoping before implementation; its counts and mechanics are experiments, not accepted multiplayer rules.

## Experience to test

Build a growing arsenal while maneuvering through a crowd with teammates. Each added weapon should feel like a meaningful increase in power. Different upgrade paths should produce recognizable strengths and useful team contributions, rather than merely increasing one damage number.

The [reference study](vampire-hunters-study.md) distinguishes the source game's advertised features from our hypotheses.

## Cooperative design and feasibility gate

Describe a 60-second two-player survival encounter that starts each player with one gun and provides earned level-up choices to grow the arsenal. Show a readable contribution from each player's build, such as one clearing approaching enemies while the other covers another direction or protects a teammate. A special combination and narrative escape objective are deferred. Resolve progression and team lifecycle rules needed for that encounter, then scope one minimal networked-combat feasibility task: consistent player/enemy state, hits, level rewards and chosen upgrades, shared ending/restart and responsive player control. Hosting, networking package, replication approach, latency/bandwidth targets and supported crowd count require explicit investigation; no library or topology is selected yet.

Cooperative progression has accepted team XP, personal independent draws, shared qualifying high-rarity rewards and the future thinking-space selection direction. It must still decide XP reward amounts, qualifying rare tiers/applicability, detailed cadence/loadout/draw ordering, downed/revive and team wipe/win conditions. Existing local global pause and player-death precedence do not implement those cooperative rules. Keep the first content slice small while preserving known weapon/loot/draw effects; richer content follows evidence.

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
