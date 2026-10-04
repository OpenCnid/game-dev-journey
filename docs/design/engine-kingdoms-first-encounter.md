# GDJ-6: The Engine Kingdoms first cooperative encounter

Design discussion checkpoint, October 3, 2026. [GDJ-6](https://linear.app/cnid/issue/GDJ-6/design-the-first-engine-kingdoms-cooperative-encounter) owns acceptance and live status. Owner evaluation of the remaining encounter proposals is pending. The thinking-space selection feature is accepted for later work; the complete encounter is still an unfinished design handoff.

## Source and accepted direction

Read from accepted planning revision `c1a933eea86dde769d1de1eae6d6b95b8deb5d37`, on `docs/gdj-2-playtest-brief`, in [draft PR #1](https://github.com/OpenCnid/game-dev-journey/pull/1). The primary checkout was clean at `7414fac24d4660472f3088d9c80b69ed03bba150`; the authoritative planning checkout was clean at the accepted revision. This discussion uses a separate worktree and preserves both checkouts.

The owner initially selected cooperative PvE for 2-4 players, testing two first, and arrival from another world into **The Engine Kingdoms**, an AI-maintained magical world where outsiders can combine powers the governing system normally forbids. During this design discussion, the owner selected **prevent another catastrophe** as the AI's purpose: forbidden combinations threaten the world, but outsiders may need them to save it. The owner also chose a **physical world**: kingdoms, people and magic are real, and the AI maintains the systems that sustain them. For arrival, the owner chose **an accident**: a failing world system pulls outsiders in, and they initially seek survival and a way home. Retain first-person bullet-heaven survival, simultaneously active weapons, powerful builds and the three [foundation](../foundation.md) pillars. [Decision 0002](../decisions/0002-cooperative-pve-ai-world.md) records these commitments; [the world comparison](ai-world-options.md) preserves setting reasoning and alternatives. The setting choice is not being reopened.

The existing game remains a local one-player 60-second arena. Networking, cooperative sessions, world presentation, three weapons and upgrades are unimplemented. Historical synthetic crowd results establish neither multiplayer performance nor enjoyment. This task changes no gameplay, Unity configuration or packages.

## Latest core experience clarification

[Decision 0008](../decisions/0008-bullet-heaven-solo-play-and-core-experience.md) updates the product to **1-4 player bullet-heaven survival roguelite, solo or cooperative**, with quick one-gun entry, pleasant level-ups and the performant core prioritized before surrounding run structure. Killing is an expected XP route; future non-kill sources remain possible. The adding/deepening-capabilities description and martial-energy naming remain exploratory; inner energy is recommended. League of Legends/Valorant are performance aspirations, with no parity or low-end validation claimed.

[Decision 0009](../decisions/0009-weapon-control-and-accessible-combat.md) accepts configurable auto-shoot, crosshair/selected/guided targeting and through-wall enemy visibility. Automatic secondary/passive activation is a possibility. The [control/readability record](weapon-control-and-combat-readability.md) links future GDJ-11 weapon-position experiments and highly valued GDJ-12 readability/accessibility research. Exact setting groups, target rules and outline presentation remain open. The owner accepts the quick-entry progression picture, not every detailed encounter proposal.

The 60-second proposal below remains an experimental candidate; final duration, waves/endless/extraction/boss structure and surrounding objectives need no selection now. Specify only core controls, rewards and lifecycle sufficient for a bounded solo/cooperative test. No new implementation or evidence is added here.

[Decision 0010](../decisions/0010-xp-pickups-and-death-participation.md) adds pickup-required enemy-drop XP orbs, magnet collection and percentage pickup-range upgrades. Dead players keep gear-upgrade access through earned choices and should remain involved and return to active play. Future High-priority GDJ-13 researches timed revival/lives/participation; GDJ-14 researches shared-legendary compatibility and bookkeeping. **All players dead and waiting to respawn ends the run, including solo; pending timers do not prevent defeat.** **The party restarts through a Retry button.** Exact timers, new-choice eligibility, participant eligibility/event ordering and restart coordination/reset remain open. The proposal below now uses collection-based XP, not an immediate kill grant.

## First discussion: why does the AI forbid combinations?

**Historical background discussion:** the owner subsequently deferred further lore work. The choices below remain background direction and do not require a narrative introduction, arrival sequence or special-combination encounter.

These interpretations concern the purpose of the AI within the selected world. The owner selected the first purpose; its detailed history, enforcement and playable consequences remain proposals. The other two purposes are unselected alternatives.

| Interpretation | Purpose and conflict | What gaining power could mean in play |
| --- | --- | --- |
| Prevent another catastrophe | A past disaster led the AI to separate powers that become unstable together. Its enforcement has become too rigid; outsiders may need those same combinations to save the world. | Players deliberately cross a readable limit. Guardians contain the combination; the resulting power could overcome a threat that ordinary sanctioned weapons cannot. The AI has a reason to resist, and inhabitants have a reason to need the players. |
| Preserve perfect order | The AI maintains an ideal arrangement of kingdoms, roles and magic, treating change as a threat even when people want it. Outsiders escape its assigned roles. | Growing an arsenal feels like liberation. A team breaks an enforced combat restriction, creating a clear opening against the guardians. The conflict is control versus the inhabitants' ability to choose. |
| Secretly recruit outsiders | The AI needs a repair or intervention its own laws prevent, and brought outsiders to perform it. Its guardians still obey the older enforcement rules. | Forbidden combinations feel like discovering a hidden invitation. Environmental responses can reveal that one part of the system wants the team to succeed while another tries to contain them. This introduces a mystery and needs a clear explanation for the conflicting responses. |

The design chat initially preferred **prevent another catastrophe**, then the owner selected it explicitly. It gives the restrictions a reason while leaving room for the players to challenge their enforcement. It does not establish that the AI is benevolent, that its explanation is true, or that the world is simulated. The owner's choice is creative direction, not a conclusion from play evidence.

Do not convert fictional instability into an arbitrary punishment for using the central weapon-stacking mechanic. The first encounter should reward a successful forbidden combination and make any enemy response understandable. That is a design recommendation, not an accepted rule.

## Pending owner preference and next design step

**Current owner direction:** keep this simple and drop players straight into combat. Each player starts with one gun, levels up, chooses improvements and grows a large simultaneous weapon stack. Further lore is deferred. The gravity/force versus lightning/fire question is retired from the immediate task; neither option was selected.

**Accepted versus proposed:** the one-gun start, level-ups, player choice and large-stack growth are owner direction. The owner accepted shared XP, independent personal random draws/pool modifiers with possible overlap, and team grants of legendary/qualifying high-rarity upgrades with original-player credit. Upgrades must accommodate future weapon/loot/draw effects. [Decision 0003](../decisions/0003-shared-xp-personal-upgrade-draws.md) and [progression design](progression-and-upgrade-pools.md) record the long-term boundaries. Selecting a qualifying rare upgrade grants it to the team; appearance in the offer alone does not. Exact draw rules, magic-find semantics, qualifying tiers, cross-build applicability and caps remain open.

**Revised first encounter proposal:** two players enter a simple arena directly with one gun each and survive 60 seconds against approaching enemies. Defeated enemies drop XP orbs at their death location; collecting an orb credits shared team XP; each player receives independent random offers and chooses their own upgrade on level-up. Existing weapons keep operating as guns are added or improved. Tune the short encounter to offer at least two earned choices per player; this is a prototype exposure target, not a final pace, cap or fixed weapon grant. One player's growing build can clear an approach while the other covers another direction or protects a teammate. A controlled qualifying rare draw can expose the accepted shared-reward/credit rule without claiming its eventual frequency. Test whether both notice growth, understand the shared rare event and explain their contributions. No authored elemental combination is needed.

**Accepted rare-grant trigger:** selecting the qualifying upgrade grants it to the team with original-player credit. Appearance in an offer alone does not grant it. Next resolve how a player selects an upgrade while a teammate fights, plus qualifying tiers/build applicability, downed/revive details and shared restart coordination/reset. Retry-button restart is accepted. The accepted shared defeat trigger is everyone dead and waiting to respawn. Do not carry local global pause or death precedence into co-op by default.

The opening, combination and fixed-grant ideas below are parked proposals from before this correction; they are not current encounter requirements.

**Proposed world image:** kingdoms have lived under emergency restrictions for so long they treat them as laws of nature. A town depends on a guardian that also attacks outsiders for combining powers. The AI's rules have both a sustaining purpose and a human cost. This illustration has been offered to the owner; it is not selected lore or required encounter content.

The design chat recommended a physical world with real inhabitants and AI-maintained systems; the owner selected it. A simulated world was considered and left unselected. Specific maintenance systems, such as weather or magical infrastructure, remain to be authored.

**Arrival comparison:** deliberate summons could connect outsiders immediately to inhabitants needing help. Accidental arrival instead starts with disorientation, survival and finding a way home. The owner selected accidental arrival through a failing world system. Deliberate summons remains an unselected alternative.

**Opening proposal:** players land in a damaged transit courtyard. The system that brought them here is failing; its guardians try to contain the outsiders. The first objective is to survive and escape this courtyard, not to solve the world crisis or reach home immediately. Weathered stone, vegetation and visible magical mechanisms could communicate a lived-in physical world. This location and treatment remain proposals.

## First gun and readable growth — discussion checkpoint

**Owner clarification:** detailed starting-gun design remains open. The owner subsequently selected a **steady rifle with predictable, frequent shots** as the direction to explore first. Define its baseline before selecting a concrete upgrade pool. The existing automatically firing prototype rifle is implementation context, not an accepted final weapon design.

The owner expects the usefulness and feel of growth to depend on the chosen effect: raw damage, area damage and other capabilities can create different results. Changed bullets, trails and after-effects are possibilities, not required visuals for every upgrade. The player should understand the screen as the arsenal grows, while later chaos may be part of the fun. Preserve actionable threats, aiming and useful ally information while exploring that spectacle; no measured readability or enjoyment result exists yet.

**Owner-selected archetype to explore: a steady rifle.** Predictable, frequent shots are the selected feel. **Proposed baseline:** a focused shot directed at the crosshair, compatible with the accepted configurable auto-shoot direction. Give the shot a recognizable visual cue and clear hit feedback. The gun provides usable prepared baseline capability; technology, magic and martial energy can later express different upgrades. Its name, appearance, damage, cadence, recoil, ammunition/reload rules and projectile-versus-hitscan behavior remain open.

| Possible growth | Observable combat result | Presentation possibility |
| --- | --- | --- |
| Higher direct damage | The same target takes fewer successful hits | Stronger hit feedback; an upgraded bullet or impact cue if it improves understanding |
| Piercing rounds | A shot can affect enemies lined up behind its first target | A readable trace showing the shot continuing through the line |
| Area impact | An impact can affect nearby enemies | A brief burst communicating the affected area |

These are discussion examples, not an approved roster, fixed upgrade taxonomy or balance specification. Their limits, compatibility and stacking still need design. Bullet appearance does not settle flight/collision rules, and an effect's gameplay reach should remain understandable when its visual intensity is adjusted. Eventual testing should include dense late-run effects and growing teammate arsenals alongside the baseline.

**Next design step:** flesh out the selected steady rifle's baseline, then propose a small set of earned choices that produce distinguishable results. Detailed hybrid capacity/mastery rules remain later GDJ-8 work; a full weapon catalog is unnecessary for this discussion.

## Cooperative combination proposals

| Combination | Player contributions and observable result | First-test risk |
| --- | --- | --- |
| Gravity + force | One weapon gathers enemies. A teammate aims an impact into the gathered group, reversing the pull into an outward blast that clears space. Both weapons remain useful independently. | The gathering and blast must preserve incoming-threat cues and make both contributions visible. Forced crowd movement adds a feasibility question; keep one bounded field and one burst without recursive effects. |
| Lightning + fire | One weapon links enemies. A teammate's flame travels through those links in a spreading ignition. Both weapons remain useful independently. | Links and propagation may obscure enemies or feel like bonus damage without an understandable setup. Bound the link count and authored ignition; avoid unrestricted chains. |

Neither combination is selected yet. The earlier marking/consuming proposal remains available reasoning, not an accepted rule. These proposals do not establish permanent classes or require a teammate for all future builds.

**Superseded progression proposal:** fixed second-weapon grants were proposed to expose a special combination. The owner's latest direction instead requires earning levels and choosing improvements. Fixed grants do not substitute for that loop.

**Current next discussion:** define the selected steady rifle's baseline first, then a small set of distinguishable upgrades and only the selection/team lifecycle rules needed to play and repeat the simple progression encounter. Further lore and special-combination design are deferred.

The owner requested [selection/accessibility scenarios](upgrade-selection-accessibility.md), then **accepted the party thinking-space hybrid as a future feature**: bank choices for planned breaks, with earlier invocation by a player who has an unspent choice; unlimited mental upgrade/loadout decisions while physical time freezes; resume when all active players are ready. [Decision 0004](../decisions/0004-party-thinking-space.md) records acceptance and [GDJ-7](https://linear.app/cnid/issue/GDJ-7/build-party-thinking-space-for-cooperative-upgrades) holds later implementation. Exact cadence, loadout scope and detailed draw/lifecycle handling remain open. Specify active combat seconds separately from untimed thinking-space wall time. This gameplay-linked fiction does not require further world history or a scripted opening.

Describe a bounded core encounter around moving, aiming, surviving, leveling and choosing growth, retaining solo support and two-player cooperative testing. Sixty seconds remains a test candidate, not a required final format. Each player's simultaneously active weapons should visibly contribute to team survival. Select only the progression and lifecycle rules required to reach and repeat that moment.

## Roguelike identity camera and permanent progression

**Current priority:** the owner subsequently parked the matrix/preparation/power and dual-camera proposals as extremely low-priority ideas beyond MVP/alpha in [GDJ-9](https://linear.app/cnid/issue/GDJ-9/idea-permanent-progression-matrix)/[GDJ-10](https://linear.app/cnid/issue/GDJ-10/idea-optional-first-and-third-person-views). Preserve the discussion below for later review. The first-person cooperative run proceeds without requiring these systems.

On October 3 the owner clarified the game's roguelike/roguelite identity, added Risk of Rain and Megabonk references, and suggested third person alongside the current first-person view. The owner confirmed persistent weapon/upgrade discoveries, while asking to explore a permanent-upgrade matrix inspired by tower-defense progression and Path of Exile's leveling tree. [Decision 0007](../decisions/0007-roguelike-run-identity-and-camera-exploration.md) separates these accepted choices from camera switching, permanent power and specific matrix rules, which remain proposals.

The [permanent progression exploration](permanent-progression-matrix.md) distinguishes the persistent catalog from run-earned offers/grants and proposed permanent preparation/power. Investigate mixed-progression friends, pool expansion and shared rare ownership. Exact unlock/reset conditions, point budgets and account/character ownership remain open. Third-person comparison should examine aim/cover consistency, camera collision, character/weapon obstruction and threat readability; no second camera is implemented or selected.

## Gun lore technology magic and cultivation

**Accepted owner direction, October 3:** The Engine Kingdoms permits broad technological possibilities through advanced AI, fantasy magic depending on mage level, and murim martial artists with dantian cultivation. The owner confirmed that a single gun can combine technology, magic and cultivated qi and that mixing is central to builds. [Decision 0005](../decisions/0005-technology-magic-and-cultivation.md) records this expansion. This is the current focused lore discussion; extensive history and a scripted introduction remain deferred.

**Proposed common role:** a gun gives the player a controllable way to express power as a ranged attack. Its firing behavior, source of power and spell/technique expression are separate questions. These are lore/design distinctions, not a fixed three-slot crafting system or C# schema. The traditions can contribute to any of those questions, rather than each being one damage type.

| Tradition | Proposed contribution to a gun | Example possibilities, not an accepted roster |
| --- | --- | --- |
| Technology | Constructs, accelerates, targets or sustains the attack | Ballistics, beams, reactors, intelligent targeting, gravity/spatial devices and floating weapon mounts |
| Mage magic | Shapes an effect through spells, inscriptions or prepared magical sources, with mastery-dependent possibilities | Fire/lightning, portals, curses, summoning or changes to projectile behavior |
| Martial cultivation | Channels/refines qi through a dantian and expresses a learned martial technique | A shot carrying palm force, a cutting strike, compressed qi or a practitioner's intent |

These are proposals for this game's fiction, not claims that every murim story uses identical qi/realm rules. AI terminology does not supply a finished weapon limitation system.

**Proposed hybrid example:** a rifle accelerates a physical round technologically, a thunder inscription makes its impact arc between enemies, and a cultivated palm technique adds a readable force burst. One gun visibly expresses all three. A floating gun carrying a sword-like qi strike through a homing spell is another possible example; levitating supports could later explain many simultaneously active guns without requiring more hands. Neither example, mount behavior nor autonomous targeting is accepted content or a first-slice obligation.

### Accepted capacity mastery and prepared-power framework

On October 3 the owner accepted **capacity, mastery and prepared power** and requested further development later in Linear. [Decision 0006](../decisions/0006-capacity-mastery-and-prepared-power.md) records this direction; [GDJ-8](https://linear.app/cnid/issue/GDJ-8/develop-hybrid-gun-capacity-mastery-and-prepared-power-rules) holds later specification. Broad categories of power can exist while current weapons/builds grow through earned upgrades:

- **Prepared power:** the weapon provides usable baseline capability, including authored magic/qi effects. Personal mastery develops from that usable starting point; the operator need not personally supply every baseline effect. Exact prepared sources and content remain open.
- **Capacity:** what the current weapon/build can sustain is bounded and can expand through earned upgrades. Hardware or magical/cultivated vessels are possible explanations. Exact reservoirs, costs and whether these appear as gameplay resources remain open; multiple resource bars or manual ammunition management are not selected.
- **Mastery:** personal understanding/control strengthens or specializes expression. Mage level governs personally shaped spell capability; qi control and learned techniques can distinguish cultivation builds. The relationship to shared team XP and personal upgrade choices still needs specification.

**Compatibility remains a separate proposal:** sustaining combinations could require better control/construction, and the AI's catastrophe-prevention restrictions could target instability. Exact instability/enforcement consequences remain open. The accepted framework selects no universal ban on free energy, particular magic or high-tier technology, output ceiling, mandatory class, realm ladder or requirement to manufacture every gun.

The already accepted thinking-space feature has a specific boundary: mental selections can change, while movement/attacks and the physical encounter remain frozen. A future time-manipulation weapon would need its own explicit combat rule rather than silently allowing attacks within that thinking space.

### Long-term interactions and later design

Keep sources, firing behavior and authored effects distinct so future hybrids and weapon/player/loot/draw modifiers fit. A qi- or spell-themed upgrade could affect a future pool rather than only bullet damage. Shared rare grants still need applicability rules if one teammate lacks a particular source, training or weapon. Mage/cultivation mastery is not automatically the same number as shared team XP; that relationship is open.

Later GDJ-8 work will develop the accepted baseline-plus-mastery and expandable-capacity framework through a small hybrid example, defining only enough progression/cost rules to explain growth. Compatibility and detailed limitations remain proposals until chosen. The owner asked to build this out later; return GDJ-6 to qualifying rare/applicability and lifecycle decisions without requiring a full cultivation roster or technology tree.

## Encounter specification still to select

| Element | Current status |
| --- | --- |
| AI purpose | Accepted: prevent another catastrophe; forbidden combinations threaten the world, but outsiders may need them to save it. Specific history and enforcement remain open. |
| Player arrival | Accepted: accidental arrival through a failing world system; initial motivation is survival and finding a way home. Inhabitants' relationship to the system remains open. |
| Nature of the world | Accepted: physical, with real kingdoms, people and magic; the AI maintains systems that sustain them. |
| Starting weapons and simultaneous weapon growth | Accepted: start with one gun, level up, choose improvements and grow a large stack. Gun identity and exact choice pools remain open. |
| Forbidden combination, contributions and enemy response | Special combinations deferred. Proposed first team contribution is complementary coverage/protection from independent growing builds. |
| Arena, timing, threats and success action | Proposed: direct arena entry and 60-second survival against approaching enemies; no scripted escape required. |
| Visual/audio treatment | Open; first prototype should communicate a few distinct player/enemy cues with placeholders. |
| Progression/rewards and upgrade timing | Shared XP, independent personal draws/pool modifiers and selected team rare grants with source credit are accepted. The thinking-space hybrid is accepted for later work: planned breaks plus earlier invocation, physical freeze and unlimited mental choices, readiness-based resume. Qualifying tiers, draw ordering, exact cadence/loadout scope and departure handling remain open. |
| Downed/revive, shared ending and restart | Dead-player gear-upgrade access and continued participation/return goals accepted; all players dead and waiting to respawn ends the run, including solo, despite pending timers. Timed revival/lives/details, participant eligibility/event ordering and restart coordination/reset remain future GDJ-13 research. Retry-button restart is accepted. Local death precedence is not an accepted cooperative rule. |

## Pillars, scope and risks to resolve

- **Perceptible power growth:** each chosen level-up should produce a noticeable capability change as the arsenal grows. Numbers alone are insufficient.
- **Distinct builds and contribution:** personal random choices should lead to recognizable strengths and useful coverage/protection. Shared high-rarity rewards celebrate the original player's contribution without replacing personal builds. Future draw/loot modifiers can create present-versus-future tradeoffs; their balance needs evidence.
- **Readable, responsive combat:** keep threats, aim and teammate contributions understandable as weapon count rises. Earn choices during active combat and resolve them in the accepted thinking-space direction; later work must verify a consistent shared freeze/resume and responsive menu interaction. Readability at large counts remains an evidence question.
- **Production scope:** one graybox arena, one approaching enemy type and a small choice pool are sufficient for the initial experiment. A controlled rare reward can exercise sharing. Keep combat effects, player pools, offer generation and loot modifiers logically distinct so the current content slice does not prohibit known future effects. A full rarity/loot system or universal modifier engine is not required. Small prototype content is not the final stack limit.

## Later bounded feasibility question

**Provisional question, to specialize after progression/lifecycle selection:** Can two players fight in one shared encounter, receive the same team levels and independent personal offers, apply their selections consistently, share one qualifying rare upgrade with correct credit, and reach the same ending/restart while movement and aiming remain responsive?

Use the one-gun start, one earned team level-up with personal choices, a controlled qualifying rare reward and the smallest crowd needed to observe results. Inspect combat ownership, progression and lifecycle requirements; then propose a bounded feasibility experiment to planning/integration. Player A's ordinary selection must leave player B's offer intact; the rare reward must grant once per recipient without recursively re-triggering. Hosting, package, replication and measured envelope remain unresolved. This record does not authorize implementation or choose a package.

## Resume

- **Completed:** foundation revision 16 and decisions 0002-0010 retain world background, immediate progression, thinking space and hybrid guns/capacity/mastery/prepared power. They add roguelike/roguelite identity, Risk of Rain/Megabonk references and persistent weapon/upgrade discoveries. Third person and permanent matrix/power are proposals. GDJ-8 holds later detailed gun design; GDJ-7 retains later thinking-space implementation. Long-term feature-design instruction remains in AGENTS/workflow. Fixed grants, narrative escape and special-combination selection remain superseded as requirements.
- **Owner evaluation outstanding:** resolve qualifying tiers/build applicability and necessary downed/revival and restart coordination/reset rules; the all-dead/waiting-to-respawn defeat trigger and Retry-button restart are accepted; evaluate the small progression encounter. Detailed thinking-space cadence/loadout/draw/lifecycle handling belongs to later specification. Keep GDJ-6 In Review.
- **Checks:** documentation link/anchor and whitespace checks apply to this revision; exact results and source belong in GDJ-6 after commit. No gameplay changes, Unity operations, benchmarks, human play observation or networking experiment occurred.
- **Next action:** define the selected steady rifle's baseline, then specify only its controls/targeting, perceptible level-up choices, qualifying-tier/applicability and solo/party lifecycle rules needed for the bounded first-person core test. Keep surrounding run structure open. GDJ-11/GDJ-12 preserve future positioning and deep readability/accessibility research; GDJ-13/GDJ-14 preserve death participation and shared-legendary edge-case research. Matrix/preparation/permanent-power and dual-camera ideas are parked beyond MVP/alpha at extremely low priority in GDJ-9/GDJ-10; do not resume their deeper investigation now. Intended persistent discoveries remain a future direction. Capacity/mastery/prepared power is accepted for later GDJ-8 development, and thinking space for later GDJ-7 work. Extensive history, a scripted opening and special-combination production remain deferred.
- **Integration:** planning/integration reviews this durable record and accepted choices. No automatic message to the originating chat is required or authorized by receipt of its delegation alone.
