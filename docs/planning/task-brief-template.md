# Task brief and handoff

Use this in a Linear issue or bounded AI task. Most tasks need only the three fields below; expand when the work warrants it. Link to existing docs instead of duplicating them.

## Default brief

**Outcome:** one observable improvement or one question to answer.

State briefly which game system and foundation pillar the outcome serves, and the player benefit or uncertainty being investigated. Link the relevant foundation section rather than copying the product definition into the brief.

**Done when:** the behavior/measurement and checks that establish completion; say whether a human playtest is required.

**Context:** the relevant foundation section and files/design/evidence links. Add the starting commit and worktree when giving the task to AI. Read the foundation first to understand the game, then the planning handoff, current state and this task's context.

The task's status, assignee and dependencies live in Linear's fields. A short completion note normally needs just the result, commit/build and checks/evidence links.

## Resume across fresh sessions

Add this to the issue when replacing a session with unfinished work. Use a linked `docs/planning/tasks/<issue-id>.md` only for a task that needs a longer plan. Keep live status/dependencies in Linear.

- **Source:** exact revision and branch/worktree; identify any uncommitted work and relevant build/configuration.
- **Completed:** the useful changes/findings already made, with file/evidence links.
- **Checks:** what passed/failed, reproduction commands and what remains unverified.
- **Remaining:** the next concrete action and unfinished acceptance criteria.
- **Blockers/decisions:** open questions, current constraints and assumptions requiring verification.

Read the project foundation, planning handoff and current state first, then this task's resume note and relevant files. Do not require earlier chat history to continue. Write notes at useful checkpoints and before ending; a stopped task need not be marked Done to preserve its progress.

## Extra context when needed

- **Task ID and outcome:** one player-visible improvement or one measurable question.
- **Base:** repository, branch/worktree and exact starting commit.
- **Context:** relevant design/decision/report links; pinned Unity project if coding.
- **Scope:** smallest useful change, files/system ownership, explicit exclusions.
- **Done when:** observable behavior, required checks and stopping condition.
- **Dependencies:** upstream task/commit; shared files; build/benchmark scheduling constraints.

For performance work also name hardware/resolution, workload/version/seed, configurations, warm-up/capture/repeats, acceptance metrics and counters that establish equal gameplay work. Distinguish a screening experiment from repeat-validated acceptance.

## Extended handoff when needed

- **Delivered:** behavior or answer, exact commit/branch/build and configuration.
- **Verified:** relevant test/build results; reproduction command; source/evidence links.
- **Observed:** human play observations or benchmark measurements, labeled separately.
- **Decision:** keep, revise or park; accepted reasoning and destination record.
- **Limits:** unresolved findings, untested cases and measured tradeoffs.
- **Next:** one recommended task and any dependency it must start from.

Example starting message:

> Work on GDJ-1 using its Linear acceptance criteria. Start fresh: read AGENTS.md, docs/foundation.md, docs/planning/README.md and docs/current-state.md, check the current committed revision, then follow the renderer report and relevant runtime setup. This task supports readable, responsive combat. Read any existing Resume section before changing code. Preserve reference modes and keep gameplay scope unchanged. Finish with the commit, checks, reproduction command and durable evidence linked from the issue; save a resume note if work remains.

This is a reusable prompt example, not a request to begin [GDJ-1](https://linear.app/cnid/issue/GDJ-1/validate-and-name-the-measured-rendering-preset).
