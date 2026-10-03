# GDJ-6: The Engine Kingdoms first cooperative encounter

Design discussion checkpoint, October 3, 2026. [GDJ-6](https://linear.app/cnid/issue/GDJ-6/design-the-first-engine-kingdoms-cooperative-encounter) owns acceptance and live status. Owner evaluation of the remaining encounter proposals is pending. The thinking-space selection feature is accepted for later work; the complete encounter is still an unfinished design handoff.

## Source and accepted direction

Read from accepted planning revision `c1a933eea86dde769d1de1eae6d6b95b8deb5d37`, on `docs/gdj-2-playtest-brief`, in [draft PR #1](https://github.com/OpenCnid/game-dev-journey/pull/1). The primary checkout was clean at `7414fac24d4660472f3088d9c80b69ed03bba150`; the authoritative planning checkout was clean at the accepted revision. This discussion uses a separate worktree and preserves both checkouts.

The owner already selected cooperative PvE for 2-4 players, testing two first, and arrival from another world into **The Engine Kingdoms**, an AI-maintained magical world where outsiders can combine powers the governing system normally forbids. During this design discussion, the owner selected **prevent another catastrophe** as the AI's purpose: forbidden combinations threaten the world, but outsiders may need them to save it. The owner also chose a **physical world**: kingdoms, people and magic are real, and the AI maintains the systems that sustain them. For arrival, the owner chose **an accident**: a failing world system pulls outsiders in, and they initially seek survival and a way home. Retain first-person horde survival, simultaneously active weapons, powerful builds and the three [foundation](../foundation.md) pillars. [Decision 0002](../decisions/0002-cooperative-pve-ai-world.md) records these commitments; [the world comparison](ai-world-options.md) preserves setting reasoning and alternatives. The setting choice is not being reopened.

The existing game remains a local one-player 60-second arena. Networking, cooperative sessions, world presentation, three weapons and upgrades are unimplemented. Historical synthetic crowd results establish neither multiplayer performance nor enjoyment. This task changes no gameplay, Unity configuration or packages.

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

**Revised first encounter proposal:** two players enter a simple arena directly with one gun each and survive 60 seconds against approaching enemies. Defeating enemies awards shared team XP; each player receives independent random offers and chooses their own upgrade on level-up. Existing weapons keep operating as guns are added or improved. Tune the short encounter to offer at least two earned choices per player; this is a prototype exposure target, not a final pace, cap or fixed weapon grant. One player's growing build can clear an approach while the other covers another direction or protects a teammate. A controlled qualifying rare draw can expose the accepted shared-reward/credit rule without claiming its eventual frequency. Test whether both notice growth, understand the shared rare event and explain their contributions. No authored elemental combination is needed.

**Accepted rare-grant trigger:** selecting the qualifying upgrade grants it to the team with original-player credit. Appearance in an offer alone does not grant it. Next resolve how a player selects an upgrade while a teammate fights, plus qualifying tiers/build applicability, downed/revive and shared ending/restart. Do not carry local global pause or death precedence into co-op by default.

The opening, combination and fixed-grant ideas below are parked proposals from before this correction; they are not current encounter requirements.

**Proposed world image:** kingdoms have lived under emergency restrictions for so long they treat them as laws of nature. A town depends on a guardian that also attacks outsiders for combining powers. The AI's rules have both a sustaining purpose and a human cost. This illustration has been offered to the owner; it is not selected lore or required encounter content.

The design chat recommended a physical world with real inhabitants and AI-maintained systems; the owner selected it. A simulated world was considered and left unselected. Specific maintenance systems, such as weather or magical infrastructure, remain to be authored.

**Arrival comparison:** deliberate summons could connect outsiders immediately to inhabitants needing help. Accidental arrival instead starts with disorientation, survival and finding a way home. The owner selected accidental arrival through a failing world system. Deliberate summons remains an unselected alternative.

**Opening proposal:** players land in a damaged transit courtyard. The system that brought them here is failing; its guardians try to contain the outsiders. The first objective is to survive and escape this courtyard, not to solve the world crisis or reach home immediately. Weathered stone, vegetation and visible magical mechanisms could communicate a lived-in physical world. This location and treatment remain proposals.

## Cooperative combination proposals

| Combination | Player contributions and observable result | First-test risk |
| --- | --- | --- |
| Gravity + force | One weapon gathers enemies. A teammate aims an impact into the gathered group, reversing the pull into an outward blast that clears space. Both weapons remain useful independently. | The gathering and blast must preserve incoming-threat cues and make both contributions visible. Forced crowd movement adds a feasibility question; keep one bounded field and one burst without recursive effects. |
| Lightning + fire | One weapon links enemies. A teammate's flame travels through those links in a spreading ignition. Both weapons remain useful independently. | Links and propagation may obscure enemies or feel like bonus damage without an understandable setup. Bound the link count and authored ignition; avoid unrestricted chains. |

Neither combination is selected yet. The earlier marking/consuming proposal remains available reasoning, not an accepted rule. These proposals do not establish permanent classes or require a teammate for all future builds.

**Superseded progression proposal:** fixed second-weapon grants were proposed to expose a special combination. The owner's latest direction instead requires earning levels and choosing improvements. Fixed grants do not substitute for that loop.

**Current next discussion:** define only the level-up selection and team lifecycle rules needed to play and repeat the simple progression encounter. Further lore and special-combination design are deferred.

The owner requested [selection/accessibility scenarios](upgrade-selection-accessibility.md), then **accepted the party thinking-space hybrid as a future feature**: bank choices for planned breaks, with earlier invocation by a player who has an unspent choice; unlimited mental upgrade/loadout decisions while physical time freezes; resume when all active players are ready. [Decision 0004](../decisions/0004-party-thinking-space.md) records acceptance and [GDJ-7](https://linear.app/cnid/issue/GDJ-7/build-party-thinking-space-for-cooperative-upgrades) holds later implementation. Exact cadence, loadout scope and detailed draw/lifecycle handling remain open. Specify active combat seconds separately from untimed thinking-space wall time. This gameplay-linked fiction does not require further world history or a scripted opening.

Write one concrete 60-second two-player encounter around moving, aiming, leveling and choosing build growth. Each player's simultaneously active weapons should visibly contribute to team survival. Select only the progression and lifecycle rules required to reach and repeat that moment.

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
| Downed/revive, shared ending and restart | Open; local death precedence is not an accepted cooperative rule. |

## Pillars, scope and risks to resolve

- **Perceptible power growth:** each chosen level-up should produce a noticeable capability change as the arsenal grows. Numbers alone are insufficient.
- **Distinct builds and contribution:** personal random choices should lead to recognizable strengths and useful coverage/protection. Shared high-rarity rewards celebrate the original player's contribution without replacing personal builds. Future draw/loot modifiers can create present-versus-future tradeoffs; their balance needs evidence.
- **Readable, responsive combat:** keep threats, aim and teammate contributions understandable as weapon count rises. Earn choices during active combat and resolve them in the accepted thinking-space direction; later work must verify a consistent shared freeze/resume and responsive menu interaction. Readability at large counts remains an evidence question.
- **Production scope:** one graybox arena, one approaching enemy type and a small choice pool are sufficient for the initial experiment. A controlled rare reward can exercise sharing. Keep combat effects, player pools, offer generation and loot modifiers logically distinct so the current content slice does not prohibit known future effects. A full rarity/loot system or universal modifier engine is not required. Small prototype content is not the final stack limit.

## Later bounded feasibility question

**Provisional question, to specialize after progression/lifecycle selection:** Can two players fight in one shared encounter, receive the same team levels and independent personal offers, apply their selections consistently, share one qualifying rare upgrade with correct credit, and reach the same ending/restart while movement and aiming remain responsive?

Use the one-gun start, one earned team level-up with personal choices, a controlled qualifying rare reward and the smallest crowd needed to observe results. Inspect combat ownership, progression and lifecycle requirements; then propose a bounded feasibility experiment to planning/integration. Player A's ordinary selection must leave player B's offer intact; the rare reward must grant once per recipient without recursively re-triggering. Hosting, package, replication and measured envelope remain unresolved. This record does not authorize implementation or choose a package.

## Resume

- **Completed:** prior world choices preserved as background; immediate one-gun/level-up/large-stack loop, shared XP/personal draws and selected team rare grants accepted. Foundation revision 8 and decisions 0003/0004 also record the accepted future thinking-space hybrid; its Linear milestone and GDJ-7 preserve later work. Long-term feature-design instruction remains in AGENTS/workflow. Fixed grants, narrative escape and special-combination selection remain superseded as requirements.
- **Owner evaluation outstanding:** resolve qualifying tiers/build applicability and necessary downed/end/restart rules; evaluate the small progression encounter. Detailed thinking-space cadence/loadout/draw/lifecycle handling belongs to later specification. Keep GDJ-6 In Review.
- **Checks:** documentation link/anchor and whitespace checks apply to this revision; exact results and source belong in GDJ-6 after commit. No gameplay changes, Unity operations, benchmarks, human play observation or networking experiment occurred.
- **Next action:** continue qualifying-tier/applicability and downed/end/restart discussion. The thinking-space hybrid is accepted as a future feature; do not repeat its selection vote or start its implementation now. Offer-generation timing and detailed cadence/loadout handling remain open. Do not reopen deeper lore or special-combination design as prerequisites.
- **Integration:** planning/integration reviews this durable record and accepted choices. No automatic message to the originating chat is required or authorized by receipt of its delegation alone.
