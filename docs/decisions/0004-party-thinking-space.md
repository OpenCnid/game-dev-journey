# 0004: Party thinking space for cooperative choices

- **Date:** 2026-10-03
- **Status:** accepted by explicit owner direction as a future feature; implementation deferred.

## Context

Shared team XP creates simultaneous personal choice opportunities. The owner asked how to preserve combat momentum and avoid frustration with waiting friends while giving players with reading or sight needs enough time to understand their options. The [selection/accessibility exploration](../design/upgrade-selection-accessibility.md) compared automatic pauses, planned breaks and live selection through hypothetical scenarios. The owner proposed a lore-linked ability/item that freezes physical time while the party can think, upgrade and change loadouts.

After reviewing the requested/planned hybrid, the owner explicitly said to make it a feature, record it and create a Linear milestone for later work. That accepts the feature direction; it does not request gameplay implementation now or establish a measured winner.

## Decision and reasoning

Accept a **party thinking space** combining banked choices at planned preparation breaks with earlier player invocation. A player with an unspent earned choice can invoke the same space during combat. Inside it, the physical encounter and its clocks are frozen while players have unlimited time to read/listen, compare, select upgrades and configure available loadouts. They cannot move through space or perform combat actions. Resume when all active players are ready.

Provide baseline access to every party. A shared starting artifact is one possible Engine Kingdoms explanation, with exact item/ability identity and presentation still open. Keep fiction focused on explaining this gameplay rule rather than requiring a larger lore production task.

Retain [decision 0003](0003-shared-xp-personal-upgrade-draws.md): independent personal choices/pool modifiers, possible teammate overlap and selected qualifying rares granting to the team with original-player credit. The shared grant occurs on selection during thinking time; its combat effect becomes observable when physical time resumes. Other players keep their own choices.

The intended benefit is safe, unhurried decision-making with control over interruption timing. Faster players still experience real-world waiting. Banking also delays earned power. Social pressure, clarity and combat momentum require human evidence; lore and hypothetical arithmetic do not prove that these problems are solved.

## Working name and theme: The Zone

On October 3 the owner suggested **The Zone** as a name for the thinking space, inspired by intense focus, absorption in a meaningful activity and mental practice. Preserve it as a working name and thematic proposal; the final name and its exact fictional explanation remain open.

Proposed fiction: the party enters a shared state of focus where thought continues while the physical encounter stands still. Choosing an earned upgrade represents understanding and mentally rehearsing a change until it is ready to use. This could connect [technology, magic and cultivation](0005-technology-magic-and-cultivation.md): understanding a weapon's operation, shaping a spell pattern or refining a qi technique. These are examples of presentation, not new mastery prerequisites or selected resource rules.

The brain-as-muscle and mental-muscle-memory language is a design metaphor. The game can make this state reliably available through its controls; entering a real-world focus state is not required. Unlimited decision time preserves the earned-choice loop rather than generating extra XP or upgrades through waiting. The accepted physical freeze, personal choices and all-player readiness rules remain in force.

Proposed flavor line: **“The Zone: thought continues; the world stands still.”**

## Consequences and follow-up

- Future work is recorded in the **Party thinking space** milestone in the [Linear project](https://linear.app/cnid/project/first-weapon-stacking-playtest-89b5952f722e), with [GDJ-7](https://linear.app/cnid/issue/GDJ-7/build-party-thinking-space-for-cooperative-upgrades) as its initial implementation task. Work begins after the necessary shared-combat/progression/lifecycle foundation is ready; no date or immediate implementation is scheduled.
- Exact activation input, checkpoint cadence, allowed loadout changes/respecs, narration/navigation behavior, offer-generation/modifier ordering and departure/downed/AFK handling need specification before implementation. Acceptance does not imply unrestricted free rebuilding or selection of the first-reveal draw proposal.
- Preserve progression, offer ownership, selected effects and shared reward events independently of freeze presentation. Account for future loot/draw modifiers and 2-4 player behavior, while keeping the first implementation two-player scope.
- Define and validate all time-dependent encounter behavior across freeze/resume. This selects no networking package, data schema or performance budget.
- Record accessible UI and human findings about understanding, waiting and pressure. Frozen time alone does not establish readable text, usable narration or whole-game accessibility.

## Revisit when

Play evidence shows that player invocation creates pressure, planned breaks delay power too much, frequent freezes disrupt combat, or players cannot safely understand and navigate their choices. Tune cadence and presentation against equivalent encounters; preserve the need for unhurried, safe access while evaluating alternatives.
