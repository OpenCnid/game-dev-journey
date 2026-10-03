# 0002: Cooperative PvE in an AI-controlled world

- **Date:** 2026-10-02
- **Status:** accepted by explicit owner direction; implementation pending.

## Context

Foundation revision 2 defined a single-player weapon-stacking horde-survival game and deferred multiplayer. During design planning the owner instead requested multiplayer, all players on the same team against AI in PvE, and an isekai premise in an AI-controlled world. The owner then selected 2-4 players with two-player testing first. These instructions supply the owner decision required by the [direction-change workflow](../workflow.md#changing-project-direction); no additional approval of these commitments is needed.

## Decision and reasoning

Adopt cooperative multiplayer PvE, intended for 2-4 players, while retaining first-person horde survival, simultaneous weapon stacking, powerful builds and the existing three pillars. Start cooperative verification with two players. Adopt arrival from another world into one governed by AI as the world premise. The owner subsequently selected **The Engine Kingdoms** from the three options: an AI maintains a magical world, and teammates combine forbidden powers to break its rules. The change follows the owner's desired game and creative direction; it is not a conclusion from playtest or benchmark evidence.

The [world comparison](../design/ai-world-options.md) preserves that selection and the unselected alternatives. In the dedicated GDJ-6 design discussion, the owner subsequently selected **prevent another catastrophe** as the AI's purpose: forbidden combinations threaten the world, but outsiders may need them to save it. This establishes a reason for the restrictions and a conflict between enforced safety and the players' power growth. The owner also chose a **physical world**: kingdoms, people and magic are real, and the AI maintains the systems that sustain them. Its maintenance therefore has consequences for the inhabitants. For arrival, the owner chose **an accident**: a failing world system pulls outsiders in, and they initially seek survival and a way home. Deliberate summons was considered and left unselected. The [first encounter record](../design/engine-kingdoms-first-encounter.md) preserves the interpretations and owner choices. Specific catastrophe history, whether the AI's assessment is correct, its enforcement, exact visual style and signature effect remain open. Marking/consuming targets is a proposed first rule-breaking test, not an accepted weapon mechanic. Hosting, networking library, resource/progression ownership, upgrade timing, revive rules and team failure/restart rules are also unresolved.

## Consequences

**Later world expansion, October 3:** [decision 0005](0005-technology-magic-and-cultivation.md) adds advanced AI/technology, mage-level-dependent magic and murim/dantian cultivation to the accepted physical world. The owner confirmed that mixing all three in a single gun is central to builds and authorized focused gun lore. This extends the setting without replacing the earlier catastrophe-prevention purpose or requiring a narrative introduction.

**Later owner clarification, October 2:** keep the selected world as background and defer deeper lore. Players should enter combat with one gun, level up, choose improvements and grow a large simultaneous weapon stack. A scripted arrival/escape and choosing a special cooperative combination are not gates for the first encounter. This clarification preserves the earlier setting choices while prioritizing the core progression fantasy; [foundation revision 5](../foundation.md) and the [encounter record](../design/engine-kingdoms-first-encounter.md) reflect it.

- Multiplayer moves from deferred expansion into the core product definition. Accounts, public matchmaking services and commercial infrastructure remain deferred.
- The existing local Unity prototype and dense-crowd evidence remain useful controls. They implement neither a cooperative session nor multiplayer performance/latency acceptance. Preserve them rather than rewriting their historical results.
- Re-scope the weapon and upgrade experiment for team contribution, shared combat outcomes and progression that works while other players are active. The local pause/restart rules cannot silently become cooperative rules.
- After design selection, scope a minimal two-player feasibility task before the broader weapon/progression implementation. Investigate authoritative simulation ownership, player response, compact crowd synchronization and consistent hits/end/restart before adopting a networking solution.
- Keep Unity/C#/URP and pinned versions. This decision does not itself select a networking package or demonstrate that the large encounter target can be networked.

## Revisit when

Review prototype cost, cooperative play observations, latency and representative performance evidence at the first two-player checkpoint. Any change to the accepted team/PvE identity follows the same owner-direction process. Record unresolved technical evidence without redefining the game from implementation convenience.
