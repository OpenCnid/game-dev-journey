# Cooperative upgrade selection: time, access and social pressure

GDJ-6 exploration, October 2, 2026. The owner requested scenario simulation before choosing between live banked choices and a shared level-up pause, specifically considering frustration with friends who take longer and access for players with reading or sight disabilities. **Neither selection mode is accepted.** The [progression contracts](progression-and-upgrade-pools.md) and [first encounter proposal](engine-kingdoms-first-encounter.md) remain the context. This is a thought experiment, not a playtest or a prediction of individual behavior.

## What can be simulated honestly

Compute waiting, menu/combat overlap and interruption counts from hypothetical inputs. Annoyance, guilt, perceived fairness and combat outcomes require player observation; assigning them percentages would invent evidence. Selection time can vary with familiarity, localization, build complexity, narration, vision, reading or input needs. Do not assign a fixed speed to a disability or assume everyone with the same disability wants the same mode.

The examples use two players and six earned choices each, independent of the proposed 60 seconds of active combat. They do not specify the encounter's final progression rate. Every choice takes the stated time, both players start each comparison round together, and the same player takes longer in each round. Breaks contain two sequential choices; overhead, optional activities and combat interruptions are excluded. Safe-break duration is additional wall time, with combat/progression timers stopped. Live selection assumes combat continues throughout the menu time and does not predict whether the team survives.

| Hypothetical scenario | Shared pause after each choice | Live personal choices | Safe breaks, two choices each |
| --- | --- | --- | --- |
| Both choose in 5 seconds | Six stops total 30 seconds; neither waits after finishing | No shared stop; each reads during 30 seconds of combat | Three stops total 30 seconds; neither waits after finishing |
| A takes 3 seconds, B takes 25 seconds | Six stops total 150 seconds; A spends 132 seconds waiting | No shared stop; B reads during 150 seconds of combat; A can cover B for 132 seconds after finishing | Three stops total 150 seconds; A still spends 132 seconds waiting |
| A takes 3 seconds each time; B takes 5 seconds five times and 90 seconds once | Six stops total 115 seconds; A spends 97 seconds waiting | No shared stop; B reads during 115 seconds of combat, including one long decision | Three stops total 115 seconds; A spends 97 seconds waiting |

Batching cuts interruption frequency in these examples, not total reading time. It may remove transition overhead in an actual game, but that is unmeasured. Different players being the bottleneck on different choices could change the arithmetic. Safe breaks also delay access to earned power until the checkpoint; this is a real cost to the owner's immediate growth loop.

## Fictional reactions to test

**Repeated shared pauses:** A already knows their build and selects immediately. B is inspecting unfamiliar effects. A could say, “We just got going and stopped again.” B could start choosing poor fits to avoid holding friends up. A visible per-player waiting timer or repeated ready pings could focus frustration on B. Safety alone does not remove social pressure.

**Live choices under attack:** A says, “Take your time, I'll cover.” It could feel cooperative once. Repeatedly losing combat help could make A feel responsible for carrying, while B could skip descriptions or leave choices unspent. Invulnerability alone would protect B's health but would not restore B's combat contribution, timely power or freedom from guilt. Reading/narration and threat tracking may also compete for attention. An unlimited menu is not sufficient protection if combat punishes using it.

**One consequential rare offer:** B needs to compare a legendary's effect with a draw-luck upgrade. A wants the team reward immediately. Because the team grant occurs on selection, social pressure could become “pick the legendary so we all get it.” Highlight the shared effect clearly, keep pending offers stable, and preserve the player's choice; never convert the group reward into automatic selection. This tension needs observation even if ordinary choices are fast.

**Planned safe breaks:** Both enter a predictable preparation phase. A chooses, reviews the team's builds and sees the upcoming wave while B compares offers. Waiting may feel more purposeful and less like repeated interruption, but A could still become bored or pressure B. A firing-range preview is a possible later activity, not a prerequisite or a demonstrated solution. Overlarge batches could create a tiring reading backlog.

These are competing hypotheses, not claims that players will behave this way. Warm friend groups, strangers and mixed-experience groups may react differently. Four-player parties can have more collective idle time and more opportunities for one long decision even though simultaneous pause time is governed by the longest decision, rather than the sum of everyone's reading time.

## Proposed first comparison

Compare untimed shared pauses with **banked choices resolved in short, planned safe breaks**. Keep live selection as an alternative whose combat/reading burden must also be evaluated; it is not chosen as the accessibility default. The safe-break candidate:

1. During a bounded combat segment, shared levels earn personal choice opportunities without forcing a menu open.
2. At a predictable checkpoint, stop combat, enemy threats and encounter progression timers. All players can read and choose without taking combat damage or missing XP. This is a real shared safe state, not merely a damage shield while a teammate fights.
3. Resolve queued opportunities sequentially, preserving independent draws and player decisions. Do not place a deadline on reading, narration, comparing or confirming, or let a host/majority force another player's selection. Continue once everyone is ready. Handle an actual departure or disconnected player through explicit lifecycle rules, still pending.
4. Let finished players inspect their own/team builds and the next threat; evaluate whether that is useful enough before adding extra activities. Use a neutral group preparation status without public individual delay timers or automatic nagging. This can reduce targeting, but does not prevent interpersonal pressure.
5. Keep the number of choices per break small enough to compare experimentally. Two is only a simulation input. Frequent earned power and manageable menu workload may favor shorter segments; exact wave length and batching are open.

Preserve the underlying earned opportunities independently of the presentation mode so future alternatives need not rewrite XP, draw modifiers or shared grants. Party-selected pause/live variants may be useful later, but a setting is not an accessibility solution if a player has to repeatedly justify their needs or accept an unusable party default. The first implementation should support one approved bounded flow, not all variants at once.

## Reading and sight access

Microsoft's [XAG 116: Time limits](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/116) recommends enough time for UI reading and interaction, with avoidable time limits removed or adjustable. It distinguishes UI from core timed gameplay and contains multiplayer exceptions; it does not prescribe a specific cooperative upgrade mode. Applying that principle here supports proposing untimed, safe choice access rather than treating a short countdown as the solution.

Microsoft's [XAG 101: Text display](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/101) supports configurable, readable text that retains content/functionality when enlarged. [XAG 106: Screen narration](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/106) supports voiced UI labels, state and interaction information. Proposed upgrade UI should provide scalable high-contrast text, stable keyboard/controller navigation, concise effect summaries with optional detail, and narration of each option's name, rarity, relevant effect and confirmation. Show rarity as text as well as color. Shared grants should have a reviewable/narratable history and should not steal focus mid-read. These are design recommendations, not completed accessibility support or an accessibility audit of the whole FPS.

Timing access, text access and narration are separate needs. Faster menus do not establish that a player can read them; a paused menu does not establish that it can be navigated without sight. Include players who use the relevant access features in evaluation, and ask which barriers each person encounters rather than inferring needs from a diagnosis.

## Future draw modifiers and sharing

**New proposal:** earn a choice token at the shared level-up, generate its offers when first opened, and freeze them once revealed. Generate later queued choices only after resolving the previous one. This lets a chosen draw modifier affect later unrevealed choices, while closing/reopening preserves the original offer and is not a free reroll. It creates a timing/build strategy and therefore needs owner selection and balance evaluation. Generating all offers at earning instead remains a viable alternative; menu timing does not settle this rule.

When a qualifying rare is selected during a safe break, grant immediately to the team with original-player credit, even if some teammates are still reading. Preserve their revealed offers and focus. A shared draw modifier would affect only their later unrevealed draws under the proposed rule. Ordering simultaneous choices, recipients with incompatible builds and repeated grants remain unresolved; document them before implementation. Do not make someone's patience or reading speed an unstated rarity mechanic.

## Owner proposal: unlimited thinking within frozen time

The owner next requested comparison of frequent shared pauses and planned safe breaks, including a hybrid gameplay mechanic connected to the lore. Their example is a player ability or item that freezes time around the party: players can upgrade or change loadouts, but cannot move through space or otherwise act physically. They have unlimited thinking time. **This is an owner-authored concept to explore, not selection of a final trigger, item, character, pause mode or free-respec rule.** The earlier lore deferral still applies; only enough fiction to explain the selection mechanic is needed.

One proposed Engine Kingdoms explanation: the outsiders can share a single suspended moment. Their minds keep working while the physical encounter remains still. A shared starting artifact could embody that ability. Exact origin and visual treatment are open. Fiction can make the pause understandable; it does not remove the real-world waiting measured above.

### Comparison using the same thinking-space rules

| Candidate | When thinking time opens | Benefit to investigate | Cost to investigate |
| --- | --- | --- | --- |
| Automatic freeze at every shared level | A team level-up immediately freezes the encounter | Earned growth can be selected and used promptly; nobody must ask for safe reading time | Repeated interruptions, including during satisfying combat sequences; fastest chooser still waits |
| Planned frozen preparation | Shared levels bank choices until a predictable wave/checkpoint break | Longer continuous combat segments and predictable selection phases | Earned power is delayed; queued choices can become a reading backlog |
| Requested freeze plus planned breaks | Level-ups bank choices without opening menus; any player with an unspent choice can invoke a party freeze, and planned breaks also resolve choices | Group can bank familiar choices or stop for one consequential/unfamiliar draw, using one consistent fiction and safe state | An invocation can still interrupt friends; someone may feel guilty about requesting it; the party may still freeze every level |

The requested/planned hybrid is the design chat's **candidate to compare**, not a proven winner or accepted rule. A player should not need permission from a host/majority to access the proposed thinking space. A normal planned break also opens it, so requesting extra time is not the only route. Exact trigger/input, checkpoint cadence and departure/AFK handling remain open. Accessibility timing is not limited by a scarce charge, cooldown, rarity or character selection in this proposal. If the fiction uses an item, give every party baseline access; an optional tactical time-stop combat item would need separate rules.

### Arithmetic and concrete situations

For A taking 3 seconds and B taking 25 seconds on each of six choices, automatic freezing still produces six stops and 150 seconds of additional wall time, including 132 seconds A spends waiting. Planned pairs produce three stops with the same 150/132 seconds. A hypothetical hybrid where they bank the first two pairs, request a separate freeze for choice five, then take choice six at a planned break produces **four stops, 150 seconds stopped and 132 seconds of A waiting**. This schedule is an invented example of player behavior, not a guaranteed reduction. It ignores menu transitions and extra deliberation caused by batching.

**A new or complex effect:** a player wants to hear all options and compare details. Automatic freezing grants access immediately. Planned freezing requires reaching the break first. Requested freezing grants safe access now, but can create “I'm stopping my friends again” pressure. A standard shared notification and a default preparation phase could help normalize it; whether they do needs observation.

**An exciting combat sequence:** a player is clearing a large pack with a new weapon combination. Automatic freezing may break that momentum. Planned freezing lets the sequence continue. The hybrid permits banking the choice, while preserving another player's ability to request a stop. It offers control over timing rather than a guarantee of uninterrupted combat.

**One shared legendary:** the originator selects the legendary during thinking time; teammates receive its grant and source-player credit without advancing the physical encounter. Their own offers and navigation focus remain intact. A group may enjoy discussing its effect before resuming, or pressure the originator to take it. Do not infer that the lore makes this social tension disappear.

**Four-player party:** with hypothetical per-choice times of 3, 5, 8 and 25 seconds, six simultaneous selection rounds still stop the world for 150 seconds, not the sum of all players' choosing times. Combined idle time is 354 player-seconds (132 + 120 + 102 + 0). Fewer stops alone cannot remove that idle time. Evaluating two players first must not treat their social/pacing results as four-player proof.

### Proposed frozen-state contract

- Suspend the shared combat encounter and its combat/progression clocks, including enemies, movement, attacks, projectiles, damage-over-time, regeneration, cooldowns, timed buffs and spawn schedules. A literal small-radius bubble with an active outside world would create additional threat/boundary rules; use the encounter-wide interpretation for the first comparison. These are proposed behavior requirements, not a networking implementation.
- Players navigate, read/listen, compare earned options, select upgrades and inspect or configure an already available loadout. No spatial movement, firing, retargeting an attack or collecting loot occurs. The allowed scope of loadout changes, respecs and refunds is not yet selected; no unlimited free rebuild is implied.
- Logical selections, pool modifiers and selection-triggered shared reward grants can be recorded while physical simulation is stopped. Weapon/projectile behavior changes become observable when combat resumes. A shared grant is still triggered on selection, not deferred until resume; receiving it still does not create a new pull.
- Preserve revealed offers and focus when a teammate selects a shared reward. First-reveal versus earning-time offer generation remains open. If later queued draws depend on newly selected modifiers, define shared-event ordering without making reading speed an unstated rarity mechanic.
- Players explicitly mark themselves ready. Resume after every active participant is ready, or after explicit lifecycle handling for a departed player. No countdown, forced selection or host/majority override is part of this candidate. A finished player may keep reading build/loadout information or cancel readiness before resumption. These are proposals, not an accepted cooperative lifecycle.
- Keep useful waiting activities inside the owner's mental-only premise: build comparison, effect information and party discussion. A firing range would break this premise unless separately presented as a mental preview; it is unnecessary for the first slice. Retain scalable text, narration and predictable navigation within the thinking space.

### Next comparison to run if implementation is later authorized

Use the same arena, XP events, upgrade content and random seed schedule for two-player comparisons, varying only automatic level freezes, planned freezes and requested/planned freezes. For fairness, freeze offers or use controlled draws in this comparison so opening time does not change the test's content; this is an experimental control, not selection of the final draw rule. Exercise one unfamiliar choice and one controlled shared rare grant. Then investigate four-player pacing separately.

Measure interruption count, real-world selection time, delay from earning a choice to using its power, and time other players spend ready. Collect player reports of interrupted momentum, boredom, pressure to hurry or choose the team reward, ability to understand choices, and whether they could use their preferred text/narration/input settings. Aggregate internal readiness timing for evaluation without a public waiting timer. Do not fake these reports with computed annoyance scores. Include players who use relevant access features. Define a success criterion with the owner before a playtest; no universal winner is established by this document.

## Evidence and next action

The inline scenario calculator was checked for the stated arithmetic, scenario interaction and responsive presentation. This establishes only the calculator's behavior. No gameplay implementation, observed annoyance, accessibility validation, multiplayer experiment or enjoyment evidence exists here.

Next owner discussion: evaluate automatic level freezes, planned frozen preparation and the requested/planned thinking-space hybrid. The owner's frozen-time/mental-only example is preserved; the design chat recommends comparing the hybrid without claiming it is best. Decide trigger/availability and loadout scope before specifying the selected rule in the first encounter and handing off one bounded feasibility question. Keep GDJ-6 In Review while encounter evaluation remains outstanding. Foundation revision 7 and accepted progression direction are unchanged.
