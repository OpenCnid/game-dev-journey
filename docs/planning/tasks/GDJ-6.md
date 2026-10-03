# GDJ-6: theme, visual identity and signature interaction

Owner direction, October 2, 2026: work on design first, specifically theme/identity. This supersedes the earlier rifle-playtest recommendation. [GDJ-6](https://linear.app/cnid/issue/GDJ-6/explore-theme-visual-identity-and-a-signature-interaction) owns live status/acceptance. No design task chat has been created or messaged.

The owner subsequently accepted **cooperative PvE for 2-4 players, testing two first**, with **isekai into an AI-controlled world**, then selected **The Engine Kingdoms**: an AI maintains a magical world and teammates combine forbidden powers. [Decision 0002](../../decisions/0002-cooperative-pve-ai-world.md) and foundation revision 3 supersede the earlier single-player constraint. The [world comparison](../../design/ai-world-options.md) preserves the choice and alternatives; detailed mechanics remain proposals.

## Role and outcome

The planning/integration chat orchestrates other task chats: scope their work, prepare briefs, coordinate explicitly authorized sessions, review saved results and integrate accepted decisions. The design task chat develops The Engine Kingdoms with the owner. The immediate outcome is one concrete 60-second two-player encounter with an explicit cooperative signature, or one precise unresolved question with a next investigation.

The [foundation](../../foundation.md) accepts The Engine Kingdoms world direction and cooperative identity while leaving exact AI motives/world technology, visual treatment and content roster open. Retain first-person horde survival, simultaneous weapons and powerful builds. Explain how the encounter expresses perceptible power growth, distinct builds, useful team contribution and readable, responsive combat. A written pitch is design reasoning, not evidence of enjoyment or performance.

## Copy-ready design-session prompt

```text
Act as the design task session for GDJ-6: Explore theme, visual identity and a signature interaction.
Issue: https://linear.app/cnid/issue/GDJ-6/explore-theme-visual-identity-and-a-signature-interaction
Repository: https://github.com/OpenCnid/game-dev-journey
Inspected base before this scope update: eb4be0b20af5fbf9adbdae7023fa60673d6de1ef. Use the latest committed foundation revision 3 and this revised brief; check actual HEAD, changes and the issue's Resume section before working. Do not resume the old single-player brief from an earlier commit.

Read AGENTS.md and docs/foundation.md first, then docs/planning/README.md, docs/current-state.md and this brief. Read docs/decisions/0002-cooperative-pve-ai-world.md, docs/design/ai-world-options.md and docs/design/game-brief.md. Follow further reference links only if needed. The owner chose world/signature design before implementation, performance or playtesting. The planning/integration chat is the orchestrator; return a durable handoff for its review.

Outcome: describe one 60-second two-player cooperative encounter in the owner-selected Engine Kingdoms world. Accepted identity: 2-4 players on the same team against AI, two-player testing first, first-person horde survival, simultaneous weapon builds, and arrival from another world into an AI-maintained magical world where teammates combine forbidden powers. The marking/consuming effect is a proposal to assess, not an accepted weapon rule. The earlier weapon behaviors are prototype hypotheses, not permanent creative constraints.

Use the option comparison rather than repeating it from scratch. Describe arrival/player fantasy, AI control and enemy response, starting weapons, visual/audio cues, the cooperative action and a clear encounter ending. Define only the progression/upgrade timing, downed/revive and shared ending/restart rules needed for that encounter. Explain how each player contributes and how the signature differs from merely adding damage. Record readability/scale and production risks. Treat hosting, authoritative state, compact crowd updates and local presentation as technical questions to investigate afterward; select no package or unmeasured network budget in this design task. No gameplay code, benchmarking, finished assets or broader content production.

Acceptance: the encounter can be explained through concrete player actions and one observable cooperative moment; necessary team/progression/lifecycle rules are explicit; pillars and solo-development constraints are addressed; assumptions and preferences are labeled; the owner accepts the encounter or identifies one precise unresolved question with a next investigation. Follow docs/workflow.md#changing-project-direction for any additional commitment change. Existing owner authorization already covers cooperative PvE, 2-4 players and The Engine Kingdoms world direction.

Handoff: save the comparison and owner decision/reasoning in the relevant docs/design/ record; link exact commit/PR, accepted choices, open questions and one next task from GDJ-6. Update the game brief/planning intent only for accepted choices, and update the foundation plus decision record only if an accepted commitment changes. Do not claim implementation or validated fun/performance. If unfinished, preserve exact source, completed discussion/options, outstanding owner decision, blockers and next action in Resume. Keep the issue In Review while its required owner evaluation is outstanding.
```

## Resume checkpoint

- **Source:** inspected clean planning revision `eb4be0b20af5fbf9adbdae7023fa60673d6de1ef` before this scope update, on `docs/gdj-2-playtest-brief`, in `C:\Users\Darian\.codex\worktrees\48b7\new-game-dev`; gameplay remains unchanged from `7414fac`.
- **Completed:** owner accepted cooperative PvE, 2-4 players with two-player testing first, and The Engine Kingdoms AI-world isekai direction. Decision/foundation and the planning comparison are recorded; brief updated for a cooperative encounter.
- **Checks:** documentation consistency and local path/anchor checks apply. No Unity build/test, human play observation or network experiment has been performed.
- **Remaining:** owner starts or explicitly authorizes the design chat; define the 60-second two-player encounter and return its accepted rules plus one bounded technical-feasibility brief to planning.
- **Limits:** The Engine Kingdoms direction is accepted; exact visual treatment, signature effect and network approach are not. Existing local evidence is retained; later task acceptance must be re-scoped for cooperative play before implementation.
