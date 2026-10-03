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

## Evidence and next action

The inline scenario calculator was checked for the stated arithmetic, scenario interaction and responsive presentation. This establishes only the calculator's behavior. No gameplay implementation, observed annoyance, accessibility validation, multiplayer experiment or enjoyment evidence exists here.

Next owner discussion: compare the experience of frequent untimed shared pauses with small planned safe breaks, including delayed power, social pressure and reading/narration needs. Then specify the chosen rule in the first encounter and hand off one bounded feasibility question. Keep GDJ-6 In Review while encounter evaluation remains outstanding. Foundation revision 7 and accepted progression direction are unchanged.
