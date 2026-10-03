# GDJ-6: The Engine Kingdoms first cooperative encounter

Design discussion checkpoint, October 2, 2026. [GDJ-6](https://linear.app/cnid/issue/GDJ-6/design-the-first-engine-kingdoms-cooperative-encounter) owns acceptance and live status. The dedicated design chat has started; owner evaluation of the proposals below is pending. This is an unfinished design handoff, not an encounter approved for implementation.

## Source and accepted direction

Read from accepted planning revision `c1a933eea86dde769d1de1eae6d6b95b8deb5d37`, on `docs/gdj-2-playtest-brief`, in [draft PR #1](https://github.com/OpenCnid/game-dev-journey/pull/1). The primary checkout was clean at `7414fac24d4660472f3088d9c80b69ed03bba150`; the authoritative planning checkout was clean at the accepted revision. This discussion uses a separate worktree and preserves both checkouts.

The owner already selected cooperative PvE for 2-4 players, testing two first, and arrival from another world into **The Engine Kingdoms**, an AI-maintained magical world where outsiders can combine powers the governing system normally forbids. During this design discussion, the owner selected **prevent another catastrophe** as the AI's purpose: forbidden combinations threaten the world, but outsiders may need them to save it. The owner also chose a **physical world**: kingdoms, people and magic are real, and the AI maintains the systems that sustain them. For arrival, the owner chose **an accident**: a failing world system pulls outsiders in, and they initially seek survival and a way home. Retain first-person horde survival, simultaneously active weapons, powerful builds and the three [foundation](../foundation.md) pillars. [Decision 0002](../decisions/0002-cooperative-pve-ai-world.md) records these commitments; [the world comparison](ai-world-options.md) preserves setting reasoning and alternatives. The setting choice is not being reopened.

The existing game remains a local one-player 60-second arena. Networking, cooperative sessions, world presentation, three weapons and upgrades are unimplemented. Historical synthetic crowd results establish neither multiplayer performance nor enjoyment. This task changes no gameplay, Unity configuration or packages.

## First discussion: why does the AI forbid combinations?

These interpretations concern the purpose of the AI within the selected world. The owner selected the first purpose; its detailed history, enforcement and playable consequences remain proposals. The other two purposes are unselected alternatives.

| Interpretation | Purpose and conflict | What gaining power could mean in play |
| --- | --- | --- |
| Prevent another catastrophe | A past disaster led the AI to separate powers that become unstable together. Its enforcement has become too rigid; outsiders may need those same combinations to save the world. | Players deliberately cross a readable limit. Guardians contain the combination; the resulting power could overcome a threat that ordinary sanctioned weapons cannot. The AI has a reason to resist, and inhabitants have a reason to need the players. |
| Preserve perfect order | The AI maintains an ideal arrangement of kingdoms, roles and magic, treating change as a threat even when people want it. Outsiders escape its assigned roles. | Growing an arsenal feels like liberation. A team breaks an enforced combat restriction, creating a clear opening against the guardians. The conflict is control versus the inhabitants' ability to choose. |
| Secretly recruit outsiders | The AI needs a repair or intervention its own laws prevent, and brought outsiders to perform it. Its guardians still obey the older enforcement rules. | Forbidden combinations feel like discovering a hidden invitation. Environmental responses can reveal that one part of the system wants the team to succeed while another tries to contain them. This introduces a mystery and needs a clear explanation for the conflicting responses. |

The design chat initially preferred **prevent another catastrophe**, then the owner selected it explicitly. It gives the restrictions a reason while leaving room for the players to challenge their enforcement. It does not establish that the AI is benevolent, that its explanation is true, or that the world is simulated. The owner's choice is creative direction, not a conclusion from play evidence.

Do not convert fictional instability into an arbitrary punishment for using the central weapon-stacking mechanic. The first encounter should reward a successful forbidden combination and make any enemy response understandable. That is a design recommendation, not an accepted rule.

## Pending owner preference and next design step

**Current question:** Which first cooperative combination appeals to the owner: gravity plus force, or lightning plus fire?

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

**Progression proposal:** each player begins with one weapon and gains a second during the encounter while the first keeps firing. The combination creates breathing room that the starting weapons could not. Fixed weapon grants are sufficient to test the first moment; menus, currency and a broader upgrade economy can remain deferred. Exact grants, timing and lifecycle rules require the encounter specification and owner evaluation.

**Next discussion:** choose or revise one combination, then write the encounter's actions, cues, enemy response and ending. Develop only the progression, downed/revive and shared restart rules needed to play and repeat it.

Once that is understood, write one concrete 60-second two-player encounter. Explain what each player aims at or moves toward, how their simultaneously active weapons contribute, and the observable cooperative moment. The combination must change what the team can do, with a result both players can attribute to their actions. Select only the progression and lifecycle rules required to reach and repeat that moment.

## Encounter specification still to select

| Element | Current status |
| --- | --- |
| AI purpose | Accepted: prevent another catastrophe; forbidden combinations threaten the world, but outsiders may need them to save it. Specific history and enforcement remain open. |
| Player arrival | Accepted: accidental arrival through a failing world system; initial motivation is survival and finding a way home. Inhabitants' relationship to the system remains open. |
| Nature of the world | Accepted: physical, with real kingdoms, people and magic; the AI maintains systems that sustain them. |
| Starting weapons and simultaneous weapon growth | Open; existing rifle is an available control, not a creative obligation. |
| Forbidden combination, contributions and enemy response | Owner comparing gravity + force with lightning + fire; neither selected yet. Marking/consuming remains a prior proposal. |
| Arena, timing, threats and success action | Open; target is one 60-second encounter for two players. |
| Visual/audio treatment | Open; first prototype should communicate a few distinct player/enemy cues with placeholders. |
| Progression/rewards and upgrade timing | Open; local global pause is not an accepted cooperative rule. |
| Downed/revive, shared ending and restart | Open; local death precedence is not an accepted cooperative rule. |

## Pillars, scope and risks to resolve

- **Perceptible power growth:** the encounter should show a before/after capability change when the forbidden combination becomes available. Numbers alone are insufficient.
- **Distinct builds and contribution:** each player must have useful independent combat and a recognizable contribution to the combination. Avoid making one player wait to enable the other's enjoyment.
- **Readable, responsive combat:** communicate the combination's setup, result and hostile response without covering aim or hiding incoming threats. Distinguish friendly effects with shape/motion as well as color. Readability at large crowd counts remains an evidence question.
- **Production scope:** keep the first candidate to one graybox arena, a small authored enemy response and one combination. Extensive story, dynamic terrain, arbitrary spell composition, polished assets and broader content are deferred. A complex AI motive can remain background context rather than adding systems to the first encounter.

## Later bounded feasibility question

**Provisional question, to specialize after encounter selection:** Can two players receive one consistent forbidden-combination result and the same shared ending/restart, while movement and aiming remain responsive?

Use only the selected interaction and the smallest crowd needed to observe it. Inspect existing combat ownership, state and lifecycle requirements; then propose a bounded feasibility experiment to planning/integration. Hosting, networking package, replication approach and measured latency/bandwidth/encounter envelope remain unresolved. This record does not authorize implementation or choose a package.

## Resume

- **Completed:** accepted context and current issue/Resume read; dedicated design chat started; comparisons recorded. Owner selected preventing another catastrophe, a physical world with real inhabitants and magic, and accidental arrival through a failing system. Foundation revision 4, decision 0002 and the game brief reconcile these accepted choices. Two combination proposals and an opening/progression proposal are offered for discussion.
- **Owner evaluation outstanding:** choose or revise a combination, then develop and evaluate the concrete encounter and its necessary lifecycle rules. Keep GDJ-6 In Review while required owner evaluation remains outstanding.
- **Checks:** 73 local Markdown links/anchors across the eight changed documents passed; `git diff --cached --check` passed. Only documentation changed. Exact source/PR belong in GDJ-6 after commit. No Unity operations, benchmarks, human play observation or networking experiment occurred.
- **Next action:** continue from the pending gravity/force versus lightning/fire question. Save owner choices explicitly before treating them as rules; develop the encounter afterward.
- **Integration:** planning/integration reviews this durable record and accepted choices. No automatic message to the originating chat is required or authorized by receipt of its delegation alone.
