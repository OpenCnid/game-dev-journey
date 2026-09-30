# Game brief

**Direction:** an original first-person horde-survival game inspired by playing Vampire Hunters.

**Confirmed priority:** stacking weapons and creating powerful builds.

**Status:** the scope and mechanics below are proposals for the first experiment, not finalized requirements. No playable implementation exists yet.

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
| Initial platform and input | Proposed Windows desktop, keyboard and mouse. Engine/language remain undecided. |
| Content limit | One graybox arena, one player, one approaching enemy type, three weapon behaviors, six upgrades. |
| Presentation | Placeholder geometry, distinct shot feedback, readable enemy and damage cues. Theme remains open. |

All counts, timings, and rules here are our starting test settings, not reconstructed Vampire Hunters values.

## First question and stopping condition

**Question:** does growing from one to three simultaneous weapons create a satisfying increase in power, with at least two upgrade paths that feel different?

First build a 60-second combat test with one weapon and one enemy type. Stop and adjust if movement, aiming, or feedback feels poor. Only then add stacking and the five-minute loop.

Stop expanding the prototype after three short documented playtests. Decide to continue, revise, or discard the approach based on whether players notice weapon differences, can describe their build's strengths, and want to try another combination. Three sessions guide the next iteration; they do not establish broad market demand.

## Deferred

Additional arenas, a full arsenal, bosses, character classes, permanent unlocks, story, polished art, multiplayer, and storefront integration. The [prototype specification](../../prototypes/001-stacked-weapons/README.md) defines the proposed technical slices and checks.

## Decisions still needed

- Which engine and exact version fit the selected prototype and our development environment?
- What do focused play sessions reveal about the weapon combinations we enjoy most?
- What original theme and signature interaction should distinguish the finished game?

No gameplay observations have been recorded yet.
