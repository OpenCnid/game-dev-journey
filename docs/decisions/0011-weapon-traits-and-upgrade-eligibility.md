# 0011: Weapon traits and upgrade eligibility

- **Date:** 2026-10-03
- **Status:** accepted existing-trait enhancement and compatible special trait unlocks; detailed content, draw timing and shared-recipient policies remain open.

## Context

The owner asked whether piercing and ricochet should belong to particular guns and whether weapons without those traits should receive their upgrades. They then selected allowing special unlocks: ordinary upgrades improve existing traits, while a specific compatible item or transformation can add a trait and open its upgrades.

This extends [shared XP and personal draws](0003-shared-xp-personal-upgrade-draws.md) and the steady-rifle exploration. Vampire Hunters is a reference, not proof of its hidden offer-filtering algorithm; the [reference study](../design/vampire-hunters-study.md) distinguishes developer-described effects from unverified eligibility rules.

## Accepted rule

- Ordinary trait upgrades improve a trait the target weapon already has. A gun without active piercing is not a target for an ordinary piercing enhancement; the same applies to ricochet and other declared traits.
- A specific compatible item or transformation can add a trait the gun did not start with. This can make that trait's enhancements eligible in future draws.
- Compatibility remains specific to the authored weapon/item/transformation. Allowing special unlocks does not make every trait available to every gun.
- An offer can still be useful to a player who owns several weapons: a piercing enhancement may target their piercing gun even though another gun lacks piercing. Its affected weapon(s) must be understandable.
- Keep trait enhancement distinct from trait acquisition. A gain-piercing item and an increase-piercing upgrade have different requirements and meanings.

The steady rifle's starting traits, compatible additions and exact upgrade pool are not selected by this rule. Piercing, ricochet and area-impact examples are not a completed roster or fixed upgrade taxonomy. Special trait unlocks are not automatically legendary, permanent account unlocks or free rewards.

## Design consequences and open details

The [progression design](../design/progression-and-upgrade-pools.md#weapon-traits-and-offer-eligibility) separates what a weapon can accept, what it currently has and what an upgrade does. Ordinary eligibility must respect current traits before personal draw weights are applied. Probability modifiers alone should not bypass a trait requirement; a declared capability/eligibility-changing effect needs an explicit compatible rule. Weapon/player/loot/draw upgrades remain distinct, so pickup range or draw luck need not inherit projectile-trait requirements.

After gaining a trait, its enhancements can participate in future offers. When eligibility is evaluated for banked choices, what happens to already-generated offers, how removed traits/weapons affect pending selections, rank/cap rules, targeting one weapon versus all compatible weapons and empty-pool handling remain to specify. Opening another upgrade family changes the build's available choices; exact probabilities and balance need later investigation.

Selecting a qualifying high-rarity reward still grants it to the party with original-player credit. Receiving an enhancement does not automatically supply its missing trait. [GDJ-14](https://linear.app/cnid/issue/GDJ-14/research-shared-legendary-compatibility-and-reward-rules) must resolve teammates lacking a compatible weapon or active trait while preserving useful shared rewards. No conversion, banking, substitute reward or universal trait grant is selected here.

Piercing/ricochet counts, damage falloff, enemy versus surface bounces, targeting and combined effect order remain open. Later work should measure hit/target/effect costs and readability with growing arsenals; no numeric cap or universal modifier architecture is selected now.

## Next design step and limits

Define the steady rifle's baseline traits, which additions it can accept, and a few distinguishable earned choices. Use a small authored subset for the first test. The capacity/mastery/prepared-power framework remains later [GDJ-8](https://linear.app/cnid/issue/GDJ-8/develop-hybrid-gun-capacity-mastery-and-prepared-power-rules) work; this record does not select training costs or a full weapon tree.

No gameplay implementation, Unity operation, benchmark or human play evidence accompanies this decision.
