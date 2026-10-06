# Roadmap

Updated October 6, 2026. This roadmap organizes construction of the chosen MVP. The [foundation](foundation.md) defines the game; [current state](current-state.md) records verified capabilities; Linear owns task priority, status and dependencies. These milestones are not a second task board or a release-date promise.

**Owner clarification:** the core survival/growth loop is an enjoyable starting premise, inspired by Vampire Hunters and Vampire Survivors. We are building our version of that game. Small playable changes should advance a coherent MVP milestone. Rough implementations and shortcuts are acceptable; our own controls, builds, threats and interactions still develop through play. The [verification policy](workflow.md#match-verification-to-the-change) removes routine benchmark gates.

## Existing foundation

The active project remains [prototypes/001-stacked-weapons](../prototypes/001-stacked-weapons/README.md), with pinned Unity/C#/URP versions. There is no planned move into `game/`.

Main already contains first-person movement/aiming, rifle combat/feedback, collected shared XP, personal paused choices, simultaneous rifle/scattergun/piercer weapons, and solo/two-player ending, return and Retry behavior. The earned stack is currently a short fixture with two growth decisions, rather than run-long progression.

The GDJ-24/GDJ-25 candidate stack adds scale experiments and mixed chaser/sentinel threats. [PR 16](https://github.com/OpenCnid/game-dev-journey/pull/16) and [PR 17](https://github.com/OpenCnid/game-dev-journey/pull/17) remain draft/unmerged at this checkpoint; main is `e8acedb`. Keep this distinction when selecting a task's baseline. Existing combined-run checks support continued construction. Review/integration remains separate from selecting the next gameplay capability.

## Proposed MVP build sequence

This sequence is a planning recommendation following the owner's request for organization. No new implementation task, final run duration or content count is selected here. Scope each milestone into small connected tasks through Linear.

| Milestone | Capability to complete | Why it comes here |
| --- | --- | --- |
| 1. Run progression | Earn repeated levels, draw personal choices from a small eligible upgrade pool, add/improve weapons and retain the resulting build through the run. | The current fixed two-choice fixture is the clearest gap between the existing stack and the accepted game. Reuse the current weapons, XP, chooser and authority path. |
| 2. Encounter structure | Author when enemies appear, how pressure changes and how a run reaches its ending, using the existing enemy roles first. | Tune pressure around the progression/build capabilities that now exist. Duration and exact ending structure remain design choices. |
| 3. Initial build content | Fill the implemented rules with a small coherent weapon/upgrade roster and enough combinations to support different builds. Add enemy content where it gives those builds useful situations. | Content should use the systems already built and expose specific missing interactions, rather than grow as isolated features. |
| 4. Playable MVP delivery | Make entry, controls, choices, party participation, ending and Retry understandable in one clearly identified distributable. Fix the problems that interrupt the full run. | Finish a usable whole and refine it through play. Broader hardware/network validation belongs to a specifically scoped follow-up. |

Solo/cooperative behavior, readable feedback and run reset are part of each affected task. Do not build a separate solo game and postpone attaching co-op. Preserve the accepted 1-4 player direction while continuing two-player implementation/checks first; expanded player counts need their own bounded work.

The owner subsequently accepts a **creative-direction-first working agreement**: select the next weapon behavior, ability, enemy, combination or player moment they want to create, then identify the small supporting progression/encounter changes needed to make it playable. AI handles the implementation sequence and technical work. The earlier standalone progression-slice recommendation is not a selected task. Keep supporting systems bounded, preserve known future interactions and use these milestones as background guidance.

## How tasks advance the roadmap

Start each task with a creative outcome: what new thing the player will be able to do, encounter or combine. Identify the existing systems it uses, the supporting capability to add and a concrete playable stopping point. Implement it in the connected stack and handle affected functional checks as routine engineering work. The owner directs the experience and plays the result; AI maintains the supporting plan and records. Fix breakage that blocks play. A new reference experiment is useful for a genuinely unresolved decision, not a default prerequisite for each addition.

Earlier learning milestones, completed experiments and their evidence remain in Git history, [playtest reports](playtests/README.md) and current state. The old unchecked experiment list no longer determines the queue. Long-term performance goals remain in the foundation and [performance policy](performance.md), subject to the current MVP verification instruction.

## Accepted future feature: Party thinking space

The owner accepted the [thinking-space hybrid](decisions/0004-party-thinking-space.md) on October 3, 2026 and requested a Linear milestone for later work. Bank earned choices for planned breaks, allow earlier player invocation, freeze physical combat for unlimited mental upgrade/loadout decisions, then resume when all active players are ready. The **Party thinking space** milestone in the [Linear project](https://linear.app/cnid/project/first-weapon-stacking-playtest-89b5952f722e) and [GDJ-7](https://linear.app/cnid/issue/GDJ-7/build-party-thinking-space-for-cooperative-upgrades) own later planning and implementation. Begin with two players after minimal shared combat/progression/lifecycle feasibility; preserve 1-4 player and future draw/loot interactions, including single-player readiness and reward behavior. No date or immediate implementation is scheduled. Detailed behavior and human validation remain pending.

## Future weapon presentation and accessibility research

[Decision 0009](decisions/0009-weapon-control-and-accessible-combat.md) accepts configurable auto-shoot, varied targeting and through-wall enemy visibility, with detailed rules open. The owner requested [GDJ-11: weapon-position experiments](https://linear.app/cnid/issue/GDJ-11/experiment-with-weapon-positioning-and-arsenal-visibility) and [GDJ-12: deep combat-readability/accessibility research](https://linear.app/cnid/issue/GDJ-12/research-combat-readability-and-accessibility). They are future Backlog work at Medium and High respectively, with no date or implementation commitment. Readability remains a core design priority; these tasks will produce bounded recommendations and human evidence, preserving the performant 1-4 player loop.

## Future death participation and shared reward research

[Decision 0010](decisions/0010-xp-pickups-and-death-participation.md) accepts collected enemy-drop XP orbs, magnets, percentage pickup-range upgrades and gear-upgrade access while dead. [GDJ-13](https://linear.app/cnid/issue/GDJ-13/research-death-revival-and-continued-player-participation) is future High-priority research when focusing on multiplayer, with timed revival/lives/participation, participant eligibility/event ordering and restart coordination/reset open. Retry-button restart is accepted. All players dead and waiting to respawn is the accepted run-ending condition, including solo, despite pending timers. [GDJ-14](https://linear.app/cnid/issue/GDJ-14/research-shared-legendary-compatibility-and-reward-rules) researches rare compatibility, duplicates, player-state eligibility and pending choices. No dates, immediate implementation or completed research; a bounded first test still needs minimal explicit rules.

## Parking lot

Put tempting ideas here without treating them as commitments. For each one, note the player benefit and what evidence would justify adding it.

Accepted future features are recorded above; this space is for further uncommitted ideas.

The owner parked these beyond MVP/alpha on October 3, 2026 at extremely low priority. Linear owns their live status/priority; they do not gate the first-person cooperative run.

- [Permanent progression matrix](https://linear.app/cnid/issue/GDJ-9/idea-permanent-progression-matrix): could give long-term build planning through discoveries, pool preparation and possible power. [Prior exploration](design/permanent-progression-matrix.md) retains details. Revisit after the core run is evaluated if the owner prioritizes it; require evidence about mixed-progression friends and draw-pool effects.
- [Optional first-/third-person views](https://linear.app/cnid/issue/GDJ-10/idea-optional-first-and-third-person-views): could improve character/weapon visibility and view preference. [Camera exploration](decisions/0007-roguelike-run-identity-and-camera-exploration.md#camera-proposal-to-explore) retains questions. Revisit if later player evidence justifies aiming/collision/animation effort; keep first person for MVP/alpha.
