# 0007: Roguelike identity permanent discovery and camera exploration

- **Date:** 2026-10-03
- **Status:** accepted roguelike/roguelite identity, reference direction and permanent weapon/upgrade discovery; third-person support and permanent-power matrix rules are proposals.

## Context

The owner clarified that the game is a roguelike/roguelite, combining inspiration from Risk of Rain and Megabonk with cooperative multiplayer. The current prototype is first person, and the owner suggested that third person might also be possible. The earlier Vampire Hunters reference remains relevant to simultaneous weapon stacking. The owner then confirmed unlocking new weapons/upgrades to discover between runs, while requesting deeper exploration of possible permanent power and a matrix inspired by tower-defense progression and Path of Exile's leveling tree.

## Decision and reasoning

Record **cooperative PvE action roguelike/roguelite horde survival** as the intended genre. Retain the one-gun start, shared XP, independent personal random choices, selected team rare grants and large simultaneous weapon-stack growth. Repeatable runs and different build choices are central. **New weapons and upgrades can remain unlocked for discovery across runs.** Exact unlock conditions, ownership and how unlocked content enters later runs remain open; exact run resets are not yet selected. Permanent stat gains, procedural maps and a particular difficulty-scaling formula are not selected.

Add **Risk of Rain and Megabonk** as owner-selected references. Their public descriptions support relevant areas to study: [Risk of Rain 2](https://store.steampowered.com/app/632360/Risk_of_Rain_2/) describes combining item effects and online co-op; [Megabonk](https://store.steampowered.com/app/3405340/Megabonk/) describes horde survival, earned XP and random upgrades of different rarities. These are developer/publisher descriptions accessed October 3, 2026, not our own gameplay observations. The owner named Risk of Rain without choosing a specific entry; using its sequel as a research example does not select its whole design.

## Permanent progression matrix proposal

The owner wants to explore a branching matrix for permanent upgrades, inspired by tower-defense progression and Path of Exile's leveling. The [official Path of Exile passive-tree description](https://www.pathofexile.com/passive-skill-tree) describes connected passive choices and larger nodes that change play rules. This is a structural reference, not acceptance of its classes, costs, exact tree or progression model for our game. No specific tower-defense title or tower-defense combat mode is selected.

The [permanent progression exploration](../design/permanent-progression-matrix.md) distinguishes accepted persistent discoveries from proposed draw/loot preparation and permanent power. A matrix, its branches and effects remain proposals. Its future content should support technology/magic/qi hybrids and personal pools without treating every permanent unlock as a free starting weapon or an automatic in-run grant.

## Camera proposal to explore

First person remains the current prototype view. **Third person alongside it is an option to evaluate**, with no implementation or commitment to ship both views. The design chat recommends investigating a per-player toggle so teammates could choose different views within one shared encounter. That recommendation remains unselected.

| View | Potential benefit to evaluate | Questions to test |
| --- | --- | --- |
| First person | Close weapon feedback and direct aiming; preserves the current combat baseline | Can players read nearby threats and their growing arsenal without excessive visual obstruction? |
| Third person | See the character, martial expression and weapon stack while reading nearby space | Do the character, weapons or scenery obscure threats? How do camera collision and aiming behave near walls and cover? |

If both views are later selected, preserve consistent shared combat, XP, draw ownership and upgrade effects. Distinguish camera framing/weapon presentation from combat rules so changing a view does not silently change damage, grant upgrades or reset state. Define crosshair targeting versus muzzle obstruction, camera clipping, avatar/weapon visibility, animation needs and input behavior. These are investigation boundaries, not a chosen code architecture.

Compare equivalent encounters with familiar and unfamiliar builds, nearby enemies, cover and tight spaces. Observe threat recognition, aiming confidence, obstruction and preferred view; no camera benefit or accessibility outcome is validated yet. Scope this comparison separately from the current encounter's necessary progression/lifecycle decisions.

## Remaining decisions

- Between runs: new weapon/upgrade discoveries are accepted. Unlock conditions, pool inclusion, account/character ownership, exact resets and permanent-power expansion remain to define.
- Camera: keep first person only, support a per-player toggle or offer another arrangement? Third-person interest does not settle this choice.
- Exact movement, content and run-ending rules remain to define within the accepted cooperative identity. Preserve the bounded first two-player encounter and known future weapon/player/loot/draw interactions.

## Revisit when

Play evidence shows that the run structure fails to encourage new builds, camera framing obscures threats or maintaining two views costs more than their demonstrated value. This record adds genre clarity and an investigation option; it establishes no implementation, validated fun or multiplayer performance.
