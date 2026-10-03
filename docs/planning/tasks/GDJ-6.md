# GDJ-6: theme, visual identity and signature interaction

Owner direction, October 2, 2026: work on design first, specifically theme/identity. This supersedes the earlier rifle-playtest recommendation. [GDJ-6](https://linear.app/cnid/issue/GDJ-6/explore-theme-visual-identity-and-a-signature-interaction) owns live status/acceptance. No design task chat has been created or messaged.

## Role and outcome

The planning/integration chat orchestrates other task chats: scope their work, prepare briefs, coordinate explicitly authorized sessions, review saved results and integrate accepted decisions. The design task chat develops options with the owner. The immediate outcome is a small comparison of original world/visual directions and signature-interaction hypotheses, followed by a selected direction or one precise unresolved question.

The [foundation](../../foundation.md) leaves theme, visual style and content roster open. This task explores those choices while retaining original single-player first-person horde survival, simultaneous weapons and powerful builds. It should explain how each direction expresses perceptible power growth, distinct builds and readable, responsive combat. A written pitch is design reasoning, not evidence of enjoyment or performance.

## Copy-ready design-session prompt

```text
Act as the design task session for GDJ-6: Explore theme, visual identity and a signature interaction.
Issue: https://linear.app/cnid/issue/GDJ-6/explore-theme-visual-identity-and-a-signature-interaction
Repository: https://github.com/OpenCnid/game-dev-journey
Inspected base: ff540e88c6e0d7eb8f2e9f3604728f06c39984b2. The brief may be on a later planning-only commit; check actual HEAD, changes and the issue's Resume section before working.

Read AGENTS.md and docs/foundation.md first, then docs/planning/README.md, docs/current-state.md and this brief. Read docs/design/game-brief.md and only relevant context from docs/design/vampire-hunters-study.md. The owner chose theme/identity design before implementation, performance or playtesting. The planning/integration chat is the orchestrator; return a durable handoff for its review.

Outcome: compare three meaningfully different original theme/visual directions and identify a signature interaction worth investigating. Preserve the core weapon-stacking horde-survival identity. Theme, visual style and roster remain open. Begin with a short discussion of the owner's tastes and disliked directions, then develop options; the first three weapon behaviors are prototype hypotheses, not a permanent creative constraint.

For each option describe: a one-sentence world premise; who the player is and how the simultaneous arsenal fits that fantasy; visual/audio cues that communicate growing power; one signature interaction hypothesis; contribution to each foundation pillar; the largest readability/scale or production risk; and the smallest later graybox experiment. Keep the comparison short enough for the owner to choose. No gameplay code, benchmarking, finished assets or broader content production in this task.

Acceptance: options differ in player experience as well as appearance; each connects to the game's pillars and solo-development constraints; assumptions and owner preferences are labeled; the owner selects a direction or identifies one precise unresolved question and next investigation. Do not silently adopt a change to agreed identity/scope. Follow docs/workflow.md#changing-project-direction if an option would change a commitment, and carry any existing explicit owner authorization forward.

Handoff: save the comparison and owner decision/reasoning in the relevant docs/design/ record; link exact commit/PR, accepted choices, open questions and one next task from GDJ-6. Update the game brief/planning intent only for accepted choices, and update the foundation plus decision record only if an accepted commitment changes. Do not claim implementation or validated fun/performance. If unfinished, preserve exact source, completed discussion/options, outstanding owner decision, blockers and next action in Resume. Keep the issue In Review while its required owner evaluation is outstanding.
```

## Resume checkpoint

- **Source:** inspected clean planning revision `ff540e88c6e0d7eb8f2e9f3604728f06c39984b2` on `docs/gdj-2-playtest-brief`, in `C:\Users\Darian\.codex\worktrees\48b7\new-game-dev`; gameplay remains unchanged from `7414fac`.
- **Completed:** owner selected theme/identity as the next design focus; orchestration role and task boundaries recorded; issue and copy-ready brief prepared.
- **Checks:** documentation consistency and local path/anchor checks apply. No new Unity build, test, play observation or design experiment has been performed.
- **Remaining:** owner starts or explicitly authorizes creation of the design chat; then discuss tastes, compare options and record the direction or next investigation.
- **Limits:** no theme, visual style or signature interaction is selected yet. Existing implementation/playtest acceptance is retained for subsequent work.
