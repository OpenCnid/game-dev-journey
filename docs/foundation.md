# Project foundation: the game we are building

**Product definition, revision 2 — October 2, 2026.** This page defines the intended game. Read it first to understand the project; implementation progress lives in [current state](current-state.md), and immediate work lives in the [planning handoff](planning/README.md).

## What this project is

We are building an **original, single-player, first-person horde-survival game centered on stacking simultaneous weapons and creating powerful builds**. The player moves and aims through crowded encounters while an expanding arsenal operates together. Choosing how that arsenal grows is central to the experience.

The intended power fantasy is to begin with limited firepower and grow into a formidable build that can handle enormous crowds. Late encounters may contain more than 10,000 enemies, including large numbers of bosses with special abilities. The player must still be able to read threats and respond; smooth performance is part of how the combat should feel.

Vampire Hunters is our starting reference for this concept. We will develop our own game, mechanics, presentation and assets, informed by what we enjoy and what we want to improve through play.

## The player's experience

The game is aimed at players who enjoy fast action, visible power growth and experimenting with weapon combinations. We want a player to finish a run thinking: **“That combination became powerful. I want to try a different build.”** This is our design intention; human playtests must establish whether we deliver it.

Our intended run loop is:

1. **Enter an encounter** with a modest starting arsenal.
2. **Move, aim and fight** to survive the surrounding horde.
3. **Make build choices** that add weapons or improve their behavior.
4. **Use the growing combination** against increasing pressure and enemy abilities.
5. **Reach an ending and restart**, carrying the lesson into another build attempt.

How choices are earned, presented and balanced is still a design question. The current experiment proposes pickups and upgrade choices; those mechanisms can change while preserving weapon stacking and build creation.

## The three design pillars

### 1. Perceptible power growth

Adding a weapon or upgrade should change what the player can do, see and feel. A stronger arsenal should visibly handle situations that were difficult earlier. We judge growth through the player's experience as well as damage and survival measurements.

### 2. Distinct builds

Simultaneously active weapons should contribute recognizable behaviors and strengths. Combinations and upgrade paths should produce different results, giving the player a reason to choose and experiment. Weapon count alone does not establish build variety.

### 3. Readable, responsive combat

The player should understand incoming threats, damage and the contribution of their weapons even when combat becomes crowded. Movement and aiming must remain responsive as enemies, projectiles and effects multiply. Visual spectacle must preserve the information needed to play.

## The game's core systems

These describe the intended product, not a list of features already implemented.

| System | Its role in the game |
| --- | --- |
| Player movement and aiming | Give the player direct control over positioning and combat in first person. |
| Simultaneous weapon arsenal | Allow multiple distinct weapons to operate together as one growing build. |
| Build progression | Give choices that change the arsenal's capabilities and strengths during a run. |
| Dense enemy encounters | Create pressure and opportunities to experience the build's increasing power. |
| Enemy and boss abilities | Create threats that require attention and response beyond approaching the player. |
| Combat feedback | Communicate weapon behavior, hits, damage and danger amid overlapping action. |
| Run lifecycle | Provide a clear start, an understandable ending and a reliable restart. |

## Scale, performance and scope

Design for **144 FPS or higher** within a declared hardware/settings/encounter envelope, with higher caps and uncapped options. Our late-game ambition includes **more than 10,000 active enemies**, potentially ability-heavy bosses. Those are requirements to validate with representative gameplay; synthetic crowd results support only their measured workload. The [performance policy](performance.md) defines the current benchmark hardware and protocol.

Finish a small, complete version of this loop before expanding its content. Three weapons, six upgrades and five minutes are the [current prototype's test settings](design/game-brief.md), rather than the definition of the finished game. Multiplayer, accounts/online services, monetization, extensive procedural content and custom-engine work are deferred until that first complete loop exists.

The final theme, visual style, weapon roster, enemy roster, run duration, progression rules and release destination remain open. They should develop around the game's identity and the three pillars above.

## How we are building it

This is a software engineer's first game, built with AI assistance. We want to finish a fun, original game and understand the systems we retain. The developer owns product direction; AI helps investigate, implement and verify it.

**Unity, C# and URP** are the accepted first-prototype tools. Exact versions belong to the project's pinned configuration; rationale and revisit conditions belong to the [engine decision](decisions/0001-engine-recommendation.md). Technical methods can improve as we measure them.

The [game brief](design/game-brief.md) turns this product definition into specific experiments. The [roadmap](roadmap.md) sequences the work. Changes to the game's identity or agreed boundaries follow the [project-direction process](workflow.md#changing-project-direction).

| Revision | Date | Change |
| --- | --- | --- |
| 1 | 2026-10-02 | Consolidated existing commitments as a session foundation. |
| 2 | 2026-10-02 | Refocused the foundation on the game's identity, player experience, run loop and core systems following the owner's clarification. Moved documentation rules into the workflow. |
