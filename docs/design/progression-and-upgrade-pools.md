# Shared progression and personal upgrade pools

GDJ-6 design checkpoint, October 3, 2026. [Decision 0003](../decisions/0003-shared-xp-personal-upgrade-draws.md) records accepted progression; [decision 0004](../decisions/0004-party-thinking-space.md) adds the accepted future selection feature. The [first encounter](engine-kingdoms-first-encounter.md) uses this direction. These are design contracts and proposals, not implemented systems.

## Owner direction

[Decision 0008](../decisions/0008-bullet-heaven-solo-play-and-core-experience.md) clarifies 1-4 player solo/cooperative bullet-heaven survival, pleasant level-ups and a performant core. Killing is an expected XP route; future non-kill XP sources remain possible. The adding/deepening-capabilities distinction remains exploratory.

- Enter combat with one gun, earn levels, choose improvements and grow a large simultaneous weapon stack.
- Share XP so friends receive aligned progression opportunities rather than competing for XP.
- Give each player individual random upgrade choices and their own draw-pool state.
- Draw independently with possible overlap between teammates; upgrade definitions are not exclusive to a player's pool.
- When a player selects a legendary or another qualifying high-rarity upgrade, give that same upgrade to the team and visibly credit the original player. Appearance in an offer alone does not grant it. Qualifying tiers remain to define.
- Account for future upgrades affecting loot pools and upgrade draw pools as well as weapons; magic-find percentage is the owner's example, with exact meaning still open.
- Consider known long-term interactions, build strategies and scalability when creating features, while keeping each implementation bounded.
- Accept party thinking space as a future feature: bank choices for planned breaks, with earlier invocation by a player who has an unspent choice; unlimited mental upgrade/loadout time during physical freeze; resume when all active players are ready. Baseline access belongs to every party. Exact cadence, presentation, loadout/draw/lifecycle handling remain open.

**Owner clarification:** independent offers and player pool modifiers may overlap between teammates. Exclusive upgrade allocation was considered and left unselected. A reusable content catalog with player-specific eligibility and weights remains a proposed implementation boundary; no code structure is selected.

Shared XP aligns opportunities and level counts. Random choices, synergies and reward-modifying upgrades can still create power differences. The balance goal is for teammates to remain useful, without erasing distinct builds; no balancing formula is selected yet.

## Accepted XP collection

[Decision 0010](../decisions/0010-xp-pickups-and-death-participation.md) records the owner's pickup rule: enemies drop XP orbs where they die. Collecting the orb credits shared team XP; the kill itself does not immediately credit that XP. Magnet abilities can vacuum/collect orbs, and pickup distance can increase through percentage upgrades during runs. Baseline range, additive/multiplicative composition, caps, magnet range/activation and travel versus immediate collection remain open.

A collected orb credits the shared pool once, including overlapping collectors or magnets. This preserves personal draws without a private XP ladder for the collector. Personal pickup-range/magnet effects govern collection, not personal level counts. World pickups, range/attraction effects, team XP and personal offers should remain distinct; exact pooling/aggregation, networking and orb-density limits are not selected. Future non-kill XP sources remain possible, with their collection presentation still to define.

## Death participation and future research

The owner wants dead players to remain involved, access gear upgrades through earned choices and return to active play. Timed revival is the direction to investigate, with multiple lives/longer timers as possibilities. **All players dead and waiting to respawn ends the run, including solo; pending respawn timers do not keep it active.** One living party member keeps this defeat condition from being met. New XP/choice eligibility during death, active-versus-banked choices, effects on living teammates, thinking-space invocation/readiness, participant eligibility/event ordering and restart coordination/reset details remain open. **The party restarts through a Retry button** after the run ends. [GDJ-13](https://linear.app/cnid/issue/GDJ-13/research-death-revival-and-continued-player-participation) is prioritized future multiplayer research; no death timer or lives count is selected now.

## Proposed separation of responsibilities

| Responsibility | What it owns | Future interaction to preserve |
| --- | --- | --- |
| Team progression | Collected orb XP, future declared XP events and level thresholds, usable with one player or a party | Personal draw/loot luck should not silently grant extra personal levels. Rules for join/downed states still need design. |
| Player build | Selected upgrades, weapon instances and active player effects | Upgrades can affect a weapon, the player, loot generation or future upgrade draws. |
| Player draw pool | Upgrades currently eligible for that player, with their draw weights | Unlocks, exclusions, ranks, tags or luck can change later offers without rewriting combat effects. |
| Offer generation | A concrete random offer for one player and level-up opportunity | Random state and offer ownership belong to that player; consuming another player's draw must not consume this player's sequence. |
| Loot generation | Loot candidates and loot probabilities, with explicit ownership | A loot modifier and an upgrade-offer modifier can use different rules; shared XP does not settle loot sharing. |
| Shared high-rarity reward | The upgrade, original player and team recipients for one qualifying pull | Sharing a reward must not reroll other players' pools, re-trigger itself on receipt or create duplicate grants of the same event. |
| Upgrade definition | Authored identity, eligibility information and declared effects | Keep reusable content separate from each player's current state; new effects do not require every upgrade to be a gun stat. |

These are logical boundaries, not a selected C# architecture, storage schema or networking technology. Only known effect types and initial content need implementation when planning authorizes a bounded task.

## Proposed draw flow

At a team level-up, each player receives their own choice opportunity. Build that player's eligible pool, apply the player's current draw modifiers, then sample the configured number of offers. The player chooses one; apply its declared effects to the player build. Ordinary upgrades affect that player. Selecting a qualifying high-rarity upgrade shares that upgrade definition with the team. Generating or viewing the offer does not grant it. A draw modifier changes its recipient's subsequent offers, rather than rerolling existing offers or implicitly editing every teammate's pool. If that modifier is itself a shared high-rarity reward, its declared effects apply to all recipients.

Offer count, uniqueness rules, rank/duplicate rules, exclusions and weighted sampling are still to define. The design chat recommends generating a concrete offer once per opportunity and preserving it while the menu is closed, so reopening does not become an unpriced reroll. When a pool-modifying choice starts affecting draws, and how queued choices behave, must be explicit before implementation.

The owner requested [selection/accessibility exploration](upgrade-selection-accessibility.md), then accepted party thinking space as a future feature: planned breaks plus earlier player invocation, unlimited mental choice time while physical time freezes, and all-active-player readiness before resume. [GDJ-7](https://linear.app/cnid/issue/GDJ-7/build-party-thinking-space-for-cooperative-upgrades) tracks later work. Exact cadence, item/ability presentation and loadout scope remain open. Sequential reveal of banked choices remains a proposal; generating offers at earning versus first opening is not settled by feature acceptance.

Do not regenerate offers every combat frame. Combat effects operate with the simulation; pool calculation and sampling happen at progression/draw events. Use independent, reproducible player random streams as a proposal for debugging and future shared-state consistency. Randomness does not imply an upgrade is exclusive to a teammate, and shared seeds do not prove whole-game simulation determinism.

## Effect examples and choices still open

| Example | Target and player tradeoff | Still to decide |
| --- | --- | --- |
| Faster firing | Immediate weapon output | Which weapons, stacking rules and tuning |
| Pickup-distance percentage | Expand the player's XP collection reach, benefiting shared progression | Baseline distance, modifier composition, caps and magnet interaction |
| Magic find for loot | Invest in improved later loot outcomes | Loot ownership, affected rarity/quantity and relative percent versus percentage points |
| Better upgrade rarity odds | Invest in improved future personal draws | Weighting/rarity model, applicability and limits |
| Change draw eligibility | Open, exclude or emphasize a content family in later offers | Availability, cost and how removed entries affect existing offers |

These examples are not an accepted roster or stat specification. A magic-find label must describe whether it affects loot, level-up offers or both; exact semantics cannot be inferred from the name. Raising weights, changing pool membership and increasing offer count are different effects and should remain distinguishable.

An early reward modifier creates a present-versus-future power decision. Multiple such effects may compound, changing which builds become strong. Evaluate the resulting strategies and power gaps across a run; do not declare the optimal meta from a written design. Caps, diminishing returns, rarity guarantees or other balancing methods are possible investigations, not accepted rules.

## Shared high-rarity moment

The owner requested [GDJ-14: shared legendary compatibility/reward research](https://linear.app/cnid/issue/GDJ-14/research-shared-legendary-compatibility-and-reward-rules) for the unresolved cases below. Coordinate dead selectors/recipients and return-to-play timing with GDJ-13; research remains future work.

**Accepted:** one player selecting a legendary or qualifying high-rarity upgrade grants that upgrade to everyone and retains credit for who originally pulled and selected it. Personal offers/pool modifiers otherwise remain independent. The threshold must be an explicit rarity rule, not a hardcoded check for one named item. A proposed banner is: “Darian found [Legendary Upgrade] — granted to the team.”

**Owner choice:** grant on selection. The original player must choose the qualifying upgrade; merely seeing it in their random offers does not grant it. This preserves the meaning of choosing. Appearance-triggered sharing is unselected.

**Proposed grant rules:** treat this as one team reward event with the original player and explicit recipients. Give each recipient the upgrade once for that event. Receiving a shared grant does not count as a new pull and trigger another round of sharing. Keep teammates' pending personal offers and choices intact. The effect must tell each player what they received; a common upgrade definition does not guarantee identical strength in different builds.

Weapon-specific prerequisites, already-owned/max-rank upgrades, incompatible choices, disconnected/downed players, two independent pulls of the same upgrade and detailed choice/stacking bookkeeping remain to specify. Prefer effects that every recipient can use in the first shared-rare test; this is a content recommendation, not an accepted universal eligibility rule. Do not silently grant a weapon-only upgrade that has no effect for a teammate, nor invent a replacement reward without a rule.

A player who invests in draw luck may create a team benefit by generating more shared high-rarity moments. Evaluate this alongside individual early combat strength: shared rewards may make luck builds useful, but neither their strength nor team fairness is established by writing the rule. Exact sharing tiers, frequency and modifier formulas are balance questions.

Account for party size: four independent player draws create more opportunities for a qualifying team reward than two, and every such reward benefits more recipients. Evaluate rarity frequency and compounding draw modifiers across the accepted 1-4 player range rather than assuming two-player tuning transfers unchanged. Preserve original-pull credit and one grant per recipient as party size grows; recipients never generate additional pull events from the shared grant. Solo selection should apply the upgrade once without a second self-grant. Keep the core choice experience available alone; exact party-size reward/difficulty tuning remains open.

## Small first slice and later feasibility

For the 60-second two-player proposal, implement only the eventual approved one-gun start, enemy-drop XP orbs collected into shared XP and personal random choices from a small content pool. Keep the upgrade/effect design compatible with the known loot/draw modifiers. A complete loot system, rarity ladder or all-purpose modifier engine is not required for that slice. The proposed exposure target is at least two earned choices per player, not a final progression rate or weapon cap.

Next bounded feasibility question after selection/lifecycle rules are chosen: can both players receive the same team level-up, keep their own random offers and selected build effects consistent, and share one qualifying rare reward with the right original-player credit before a shared ending/restart? Minimal evidence should include an ordinary choice leaving the teammate's offer intact and one controlled qualifying reward granted once per recipient without retriggering. Controls must remain responsive. Hosting, package and replication details remain open.

## Resume

Accepted: pickup-required enemy-drop XP orbs, magnet collection, percentage pickup-range upgrades, gear-upgrade access during death, all-dead/waiting-to-respawn defeat (including solo), Retry-button restart, and shared XP, independent personal draws/pool modifiers with overlap, selected team rare grants with source credit, future weapon/loot/draw effects, and the party thinking-space hybrid as a future feature. Pending: qualifying tiers, cross-build applicability, repeats, detailed thinking-space cadence/loadout/draw handling and downed/revival details, event ordering and restart coordination/reset details. The selection/accessibility reasoning is saved; later feature work belongs to GDJ-7's milestone. GDJ-13 prioritizes death/revival participation research for multiplayer; GDJ-14 covers shared legendary edge cases. Keep GDJ-6 In Review while encounter evaluation remains outstanding. No gameplay implementation, Unity operations, benchmarks or validated balance/enjoyment evidence in this checkpoint.
