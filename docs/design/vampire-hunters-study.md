# Design study: Vampire Hunters

**Status:** initial desk research; gameplay measurements pending.

**Reference:** [Vampire Hunters on Steam](https://store.steampowered.com/app/2206270/Vampire_Hunters/), developer-authored description, accessed 2026-09-30.

The project owner has been playing this game and wants to explore a similar concept, especially **stacking weapons and creating powerful builds**. This study works backward from public information and observable play to an original implementation. We will write our own code and use original or appropriately sourced assets, names, presentation, and level designs.

## What we know

The developer describes a first-person survivors-style roguelite with enemy hordes, up to 14 stacked weapons, upgrades, and hunters with different abilities. The setting includes supernatural enemies and steampunk-inspired environments. These are advertised features, not results of our own testing. [Source: Steam](https://store.steampowered.com/app/2206270/Vampire_Hunters/).

We have not inspected the game's code, measured a run, or verified its internal algorithms. Its exact damage math, targeting, spawn scheduling, upgrade probabilities, and progression rules remain unknown. The systems and values proposed elsewhere in this repository are our design hypotheses.

## Trait-specific and trait-granting effects — October 3 desk check

The developer's [official announcement notes](https://steamcommunity.com/app/2206270/announcements/), accessed 2026-10-03, describe Bounce of Wrath removing ricochet damage reduction and Drill Bullet Charm removing piercing damage reduction. They also describe Cryo Sigil giving weapons freeze and Toxic Infusion giving weapons poison. These are developer-described effects, not our gameplay measurements.

This supports distinguishing enhancement of an existing behavior from effects that grant behavior. It does not establish which guns accept each effect, whether an offer is hidden when nothing benefits, the current complete weapon roster or the hidden offer-generation algorithm. Our accepted compatibility/eligibility rule is in [decision 0011](../decisions/0011-weapon-traits-and-upgrade-eligibility.md); do not label it a verified copy of Vampire Hunters.

## What might make this combination work

These are interpretations to test, not verified facts about the reference:

| Design hypothesis | What to look for during play | Implication for our experiment |
| --- | --- | --- |
| Simultaneous weapons make increased power visible and audible. | Does adding a weapon change combat more than a damage increase? | Compare one weapon with a three-weapon loadout. |
| Complementary weapons create recognizable builds. | Which combinations solve different combat problems? | Test focused damage, crowd coverage, and piercing. |
| Periodic choices create anticipation. | Which choices change behavior, and which feel automatic? | Offer a few distinct upgrades with readable consequences. |
| Enemy pressure makes power gains meaningful. | When does the player feel weak, comfortable, or overwhelmed? | Tune spawn pressure and damage together. |
| Failure suggests another experiment. | After losing, does the player want to change a build or strategy? | Show a useful run summary and allow immediate restart. |

## Study the game through play

Use the same mode, difficulty, character, and map where possible. Record the visible game version and loadout. Treat small samples as observations, not proof of a hidden formula.

1. **Baseline run:** note movement, firing inputs, aiming requirements, pickup behavior, and what ends the run. Record timestamps for the first new weapon, first upgrade, and first confusing death.
2. **Weapon comparison:** compare a similar early section with different loadouts. Note firing cadence, target selection, visual obstruction, and whether weapons work independently. Record other changes that prevent a fair comparison.
3. **Build comparison:** try two contrasting combinations and record what each handles well or poorly. Identify whether an apparent synergy is an explicit rule or simply complementary behavior.
4. **Progression and pressure:** record when choices appear, what triggers them if observable, and how enemy density or behavior changes. Distinguish within-run changes from anything that persists afterward.
5. **Experience review:** identify the three most satisfying moments and the three most frustrating. Describe what happened before proposing an explanation.

Suggested observation table, to paste into a [playtest note](../playtests/template.md):

| Time / context | Visible action or event | Player response | Hypothesis | What would test it? |
| --- | --- | --- | --- | --- |

Questions still open: automatic versus manual firing; aiming and target-selection behavior; weapon-slot rules; upgrade triggers; enemy roles; run objectives; persistent progression. Do not fill these in from genre assumptions.

## Turn observations into our own game

For each promising behavior, write: **observation -> intended player experience -> smallest implementation -> playtest**. For example, if an additional weapon feels like a significant power gain, our first test can add a second independently firing weapon and ask whether the player notices the gain without reading a stat panel.

Keep growing firepower as the starting priority. Explore our own theme, weapon interactions, upgrade tradeoffs, enemy silhouettes, and arena layout as evidence develops. A shared-energy weapon rig is one possible later variation: choosing which weapons receive power could trade coverage for focused damage. It is an unselected idea, outside the first prototype.

The first concrete proposal is in [the game brief](game-brief.md), with an [implementation experiment](../../prototypes/001-stacked-weapons/README.md). We are testing whether the core interaction is worth developing before committing to a full content roster or persistent progression system.
