# Task brief and handoff

Use this in a Linear issue or bounded AI task. Most tasks need only the three fields below; expand when the work warrants it. Link to existing docs instead of duplicating them.

## Default brief

**Outcome:** one observable improvement or one question to answer.

**Done when:** the behavior/measurement and checks that establish completion; say whether a human playtest is required.

**Context:** relevant files/design/evidence links. Add the starting commit and worktree when giving the task to AI.

The task's status, assignee and dependencies live in Linear's fields. A short completion note normally needs just the result, commit/build and checks/evidence links.

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

> Work on GDJ-1 using its Linear acceptance criteria. Start from the current committed revision and read docs/current-state.md, the renderer report and the relevant runtime setup. Preserve reference modes and keep gameplay scope unchanged. Finish with the commit, checks, reproduction command and durable evidence linked from the issue.

This is a reusable prompt example, not a request to begin [GDJ-1](https://linear.app/cnid/issue/GDJ-1/validate-and-name-the-measured-rendering-preset).
