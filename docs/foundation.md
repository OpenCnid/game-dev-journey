# Project foundation: the game we are building

**Product definition, revision 9 — October 3, 2026.** This page defines the intended game. Read it first to understand the project; implementation progress lives in [current state](current-state.md), and immediate work lives in the [planning handoff](planning/README.md).

## What this project is

We are building an **original, cooperative multiplayer PvE, first-person horde-survival game centered on stacking simultaneous weapons and creating powerful builds**. Design for **2-4 players on the same team**, with **two-player testing first**. Players move and aim through crowded encounters while expanding arsenals operate together. Choosing how those arsenals grow and contribute to the team is central to the experience.

The selected world direction is **The Engine Kingdoms**: a failing world system accidentally pulls players from another world into a **physical magical world** maintained by artificial intelligence. They initially seek survival and a way home. Kingdoms, people and magic are real; the AI maintains the systems that sustain them. As outsiders, players can combine powers the governing system normally forbids, giving cooperative weapon stacking a place in the world's fiction. The owner selected the AI's purpose: **prevent another catastrophe**. Forbidden combinations threaten the world, but outsiders may need them to save it. Details of the earlier catastrophe and enforcement, exact visual treatment and playable rule-breaking effects remain design questions. The [world comparison](design/ai-world-options.md) preserves the selected setting and alternatives; the [first encounter record](design/engine-kingdoms-first-encounter.md) records the world choices and developing design. [Decision 0002](decisions/0002-cooperative-pve-ai-world.md) records the owner's change from the earlier single-player scope and subsequent world choices.

The owner established that this world supports **advanced AI and broad technological possibilities, fantasy magic depending on mage level, and murim martial artists with dantian cultivation**. **A single gun can mix technology, magic and cultivated qi, and this mixing is central to weapon builds.** [Decision 0005](decisions/0005-technology-magic-and-cultivation.md) records this direction. The exact AI label, power/rank rules and gun limits remain to define; technological openness is a fiction premise, not a real-world claim of omnipotence.

The intended power fantasy is to **enter combat with one gun, level up, choose how to improve the build, and grow into a large simultaneous weapon stack**. The owner wants players dropped straight into the game and has now authorized focused gun-lore/boundary design. Extensive world history and a scripted narrative opening remain deferred. The Engine Kingdoms remains background context. A special cooperative combination is a later possibility, rather than a prerequisite for this basic loop. Late encounters may contain more than 10,000 enemies, including large numbers of bosses with special abilities. The player must still be able to read threats and respond; smooth performance is part of how the combat should feel.

Vampire Hunters is our starting reference for this concept. We will develop our own game, mechanics, presentation and assets, informed by what we enjoy and what we want to improve through play.

## The player's experience

The game is aimed at players who enjoy fast action, visible power growth and experimenting with weapon combinations alongside teammates. We want a player to finish a run thinking: **“That combination became powerful. I want to try a different build with my team.”** This is our design intention; human cooperative playtests must establish whether we deliver it.

Our intended run loop is:

1. **Enter combat as a team**, each player starting with one gun.
2. **Move, aim and fight together** to survive the surrounding horde.
3. **Level up and make build choices** that grow the simultaneous arsenal and improve its capabilities.
4. **Use the growing combinations** and support teammates against increasing pressure and enemy abilities.
5. **Reach a shared ending and restart**, carrying the lesson into another team build attempt.

Starting with one gun and growing through player-selected level-ups is accepted. **XP is shared by the team; random upgrade offers and choices belong to each player.** Each player has their own draw-pool state and can develop a different build. Draws are independent with possible overlap between teammates; upgrades are not reserved exclusively to one player's pool. **Selecting a legendary or other qualifying high-rarity upgrade grants the same upgrade to everyone, with credit to the original player**, making it a group event. Appearing in an offer does not grant it. Exact qualifying tiers remain open. The owner wants upgrades to support future effects on loot and upgrade draws as well as weapons, such as a magic-find percentage; exact effects and probabilities remain to define. Shared XP aligns level-up opportunities, but equal combat power is a balance goal to test, not a guaranteed result of shared levels. See [decision 0003](decisions/0003-shared-xp-personal-upgrade-draws.md) and [progression design](design/progression-and-upgrade-pools.md). Exact reward amounts, within-offer uniqueness/repeat rules, weapon limits, downed/revive and team ending rules remain open.

**Party thinking space is accepted as a future feature:** bank earned choices for planned preparation breaks, with earlier invocation by a player who has an unspent choice. The physical encounter and its clocks freeze while players have unlimited time for mental upgrade/loadout decisions, without spatial movement or combat actions. Resume when all active players are ready. Baseline access belongs to every party; exact ability/item presentation, checkpoint cadence, loadout/respec scope and detailed draw/lifecycle rules remain open. [Decision 0004](decisions/0004-party-thinking-space.md) records acceptance and the deferred Linear milestone. No implementation or validated accessibility/enjoyment is implied.

## The three design pillars

### 1. Perceptible power growth

Adding a weapon or upgrade should change what the player can do, see and feel. A stronger arsenal should visibly handle situations that were difficult earlier. We judge growth through the player's experience as well as damage and survival measurements.

### 2. Distinct builds

Simultaneously active weapons should contribute recognizable behaviors and strengths. Combinations and upgrade paths should produce different results, giving the player a reason to choose and experiment. Players should understand their own contribution and opportunities to complement teammates. Weapon count alone does not establish build variety.

### 3. Readable, responsive combat

The player should understand incoming threats, damage and the contribution of their weapons even when combat becomes crowded. Movement and aiming must remain responsive as players, enemies, projectiles and effects multiply. Teammate effects and network behavior must preserve the information and responsiveness needed to play.

## The game's core systems

These describe the intended product, not a list of features already implemented.

| System | Its role in the game |
| --- | --- |
| Player movement and aiming | Give the player direct control over positioning and combat in first person. |
| Simultaneous weapon arsenal | Allow multiple distinct weapons to operate together as one growing build. |
| Build progression | Share team XP with independent personal choices and pool modifiers; selecting a qualifying high-rarity upgrade grants it to the team and credits the original player. Upgrades can affect combat, loot and future draws. |
| Party thinking space | Bank earned choices for planned breaks or invoke earlier; freeze physical combat for unlimited mental upgrade/loadout decisions and resume when all active players are ready. Accepted future feature, implementation deferred. |
| Dense enemy encounters | Create pressure and opportunities to experience the build's increasing power. |
| Enemy and boss abilities | Create threats that require attention and response beyond approaching the player. |
| Combat feedback | Communicate weapon behavior, hits, damage and danger amid overlapping action. |
| Cooperative session and shared combat | Keep players on one team in a consistent encounter, with understandable teammate contributions and shared authoritative outcomes. |
| Run lifecycle | Provide a clear team start, an understandable shared ending and a reliable restart. |

## Scale, performance and scope

Design for **144 FPS or higher** within a declared hardware/settings/encounter envelope, with higher caps and uncapped options. Our late-game ambition includes **more than 10,000 active enemies**, potentially ability-heavy bosses. Those are requirements to validate with representative cooperative gameplay; synthetic local crowd results support only their measured workload. Network topology, latency/bandwidth targets and the supported multiplayer encounter envelope remain unvalidated. The [performance policy](performance.md) defines the current local benchmark hardware and protocol.

Finish a small, complete cooperative version of this loop before expanding its content. Three weapons, six upgrades and five minutes are the [earlier local prototype's test settings](design/game-brief.md), rather than the definition of the finished game. Networking is now part of the core game; validate a minimal two-player shared encounter before expanding multiplayer systems or content. Accounts, public matchmaking services, monetization, extensive procedural content and custom-engine work remain deferred.

The AI's enforcement and the earlier catastrophe, underlying world technology, exact visual style, weapon roster, enemy roster, run duration, cooperative progression rules, hosting/connection approach and release destination remain open. They should develop around The Engine Kingdoms direction, the game's identity and the three pillars above.

## How we are building it

This is a software engineer's first game, built with AI assistance. We want to finish a fun, original game and understand the systems we retain. The developer owns product direction; AI helps investigate, implement and verify it.

Design each feature with its known long-term interactions and effects on future builds/content in mind, while implementing bounded, testable slices. The [workflow](workflow.md#design-for-the-games-future) records this owner instruction.

**Unity, C# and URP** are the accepted first-prototype tools. Exact versions belong to the project's pinned configuration; rationale and revisit conditions belong to the [engine decision](decisions/0001-engine-recommendation.md). Technical methods can improve as we measure them.

The [game brief](design/game-brief.md) turns this product definition into specific experiments. The [roadmap](roadmap.md) sequences the work. Changes to the game's identity or agreed boundaries follow the [project-direction process](workflow.md#changing-project-direction).

| Revision | Date | Change |
| --- | --- | --- |
| 1 | 2026-10-02 | Consolidated existing commitments as a session foundation. |
| 2 | 2026-10-02 | Refocused the foundation on the game's identity, player experience, run loop and core systems following the owner's clarification. Moved documentation rules into the workflow. |
| 3 | 2026-10-02 | Owner changed the game to cooperative multiplayer PvE for 2-4 players, testing two first, and selected The Engine Kingdoms: isekai into an AI-maintained magical world where teammates combine forbidden powers. Detailed mechanics and technology remain under design. |
| 4 | 2026-10-02 | Owner selected a physical world with real kingdoms, people and magic sustained by AI systems, the AI's purpose of preventing another catastrophe, and accidental arrival through a failing world system with survival and finding a way home as the initial motivation. Forbidden combinations threaten the world, but outsiders may need them to save it. Specific history, enforcement and encounter mechanics remain open. |
| 5 | 2026-10-02 | Owner redirected design to immediate combat: start with one gun, level up, choose improvements and grow a large weapon stack. Further lore, a scripted opening and special cooperative-combination selection are deferred; prior world choices remain background context. |
| 6 | 2026-10-02 | Owner accepted shared XP, independent personal random draws/pool modifiers with possible overlap, and team-wide grants of legendary/qualifying high-rarity upgrades with credit to the original player. Upgrades must accommodate future loot/draw effects as well as weapons; features must consider long-term interactions. Exact grant trigger/tiers, repeat rules, magic-find behavior and lifecycle remain open. |
| 7 | 2026-10-02 | Owner selected the shared rare-reward trigger: grant to the team when the original player selects the qualifying upgrade. Appearance in an offer alone does not grant it. |
| 8 | 2026-10-03 | Owner accepted party thinking space as a future feature and requested a Linear milestone: bank choices for planned breaks or invoke earlier, unlimited mental upgrade/loadout time during physical freeze, and resume when all active players are ready. Detailed cadence, presentation, loadout/draw/lifecycle rules and implementation remain deferred. |
| 9 | 2026-10-03 | Owner established advanced AI/technology, mage-level-dependent fantasy magic and murim/dantian cultivation together, and confirmed that mixing all three in a single gun is central to builds. Focused gun lore is the current design discussion; exact limits, ranks, sources and implementation remain open. |
