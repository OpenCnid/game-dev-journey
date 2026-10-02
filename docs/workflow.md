# How we work

We are one developer working with AI. Keep one implementation task active, shorten the path from an edit to useful feedback, and finish small playable improvements. Change this system when it creates friction.

## Give each tool one job

| Tool | Owns |
| --- | --- |
| Linear | Priorities, task status, acceptance criteria, dependencies and links to results. |
| GitHub | Code, versioned design/decisions, reproducible checks and evidence. |
| AI chats | Working discussion, implementation and investigation for a bounded task. |
| Unity and standalone builds | What the change actually does, looks like and feels like. |

Use [current state](current-state.md) to orient a new chat, then follow only the relevant design or evidence links. The [roadmap](roadmap.md) describes milestone outcomes; Linear owns the live task queue. Do not maintain a second copy of that queue in GitHub Issues or a Markdown checklist. GitHub Issues can still collect an outside report; link the item we choose to work on to Linear.

## The small board

Use the **Game Dev (GDJ)** team and one project: [First weapon-stacking playtest](https://linear.app/cnid/project/first-weapon-stacking-playtest-89b5952f722e). Its existing statuses mean:

| Stage | Meaning |
| --- | --- |
| Backlog | An idea or later step, with no commitment to start it. |
| Todo | A small task with an outcome and a way to check it. Keep about three ready. |
| In Progress | The one implementation task currently being worked on. |
| In Review | The candidate is ready for review, a build check or a required human playtest. |
| Done | The task's acceptance checks are satisfied and its result is linked. |

A blocked task stays in its current stage with the blocking dependency or a short explanation. Do not add a status for every situation. Use no cycles, estimates or deadlines until a real scheduling problem calls for them. Split a task if it cannot reach a useful stopping point in a focused session or two; an experiment can finish with a measured answer instead of a shipped feature.

## From issue to result

1. Pick one ready task. Its brief needs **Outcome, Done when, Context**; use the [brief template](planning/task-brief-template.md) when helpful.
2. Give an AI task chat the issue link, starting commit and relevant files. Continue that chat while pursuing the same outcome. Keep one planning chat for priorities and accepting results; open another task chat when the objective changes.
3. Make the smallest useful change. Use a short-lived branch/worktree when isolation helps. One writer owns a Unity project at a time; serialize its Editor, build and test operations.
4. Review the diff and run the checks appropriate to the change. Fix failures before handoff.
5. Link the commit or PR, checks, build/reproduction instructions and any observations to the issue. Put durable measurements or decisions in the existing repo documents, with a short result and link in Linear.
6. Finish the required review/playtest, mark Done, and choose the next task. A merged PR does not establish that a change feels good.

Use the Linear issue ID in task-chat titles, branch names and PR titles. [Linear's GitHub integration](https://linear.app/docs/github) can link them automatically. The Game Dev team's merge rule is configured to **In Review** so completion stays deliberate, including required human playtests. The GitHub integration's connection to this repository has not been verified; use explicit links until it is. Mark Done manually when the issue's checks are satisfied.

## Make game iteration easy for AI and humans

The useful analogy to a web preview is a reproducible game scenario: a known arena, seed, loadout and starting state that we can launch quickly, reset and compare. Build on our existing helpers rather than creating an agent orchestration service.

- Keep combat rules separable from rendering, input and UI so important rules can be checked cheaply.
- Make weapon/enemy tuning and scenario settings explicit, easy to edit and review. Use Unity assets or code where they fit; use the visual editor when it helps author the result.
- Prefer a script that launches a specific scenario and reports its build/source over a long list of manual setup steps. Our player already supports `-playtest`, `-screenshot` and repeatable benchmarks.
- Let AI run repeatable checks and summarize evidence. You review consequential behavior and play the exact candidate for feel, readability and enjoyment. Screenshots and automated tests answer narrower questions.

## Match verification to the change

| Change | Useful check |
| --- | --- |
| Documentation | Paths, links and consistency with the current repository. |
| Combat rule or recurring bug | Focused Edit Mode tests for behavior and edge cases. |
| Input, UI, visuals or lifecycle | Play Mode smoke check, affected scenario and pause/restart; human observation where required. |
| Build or delivery | Standalone player launched outside the Editor; distribute its whole build folder. |
| Simulation, rendering or dense content | Relevant visible release workload, equivalent work counters and frame-time distributions. Use a development build for focused profiler diagnosis. |
| Playable checkpoint | A standalone candidate, brief human playtest and representative performance check. |

Use `tools/unity.ps1` and the [prototype instructions](../prototypes/001-stacked-weapons/README.md). Do not run the full crowd matrix for every small edit. Run one performance capture at a time on the named desktop, with competing builds/benchmarks stopped. An isolated worktree does not isolate CPU/GPU load. There is no hosted CI pipeline yet.

Tests must check meaningful behavior, parity, regressions or resource lifetime; avoid tests that simply repeat the implementation. Preserve reference paths for performance experiments. Record hardware, source/configuration, workload, repetitions and limitations with a measured conclusion. Keep external asset origins/terms in the [asset register](../assets/README.md).

## Improve the workflow from evidence

At a checkpoint, spend a few minutes asking: how long did an edit take to reach a useful test, which steps were manual, and how much review or rework did AI output need? Fix the largest repeated friction and remove unused process. Investigate import/reload times only if they are a measured bottleneck. Unity's [configurable Play Mode](https://docs.unity3d.com/6000.3/Documentation/Manual/configurable-enter-play-mode.html) can shorten waits, but disabled reloads require correct state reset; we have not changed those settings.

The [October 2 journey review](planning/2026-10-02-development-system.md) preserves how we reached this point. Its [interactive development loop](planning/diagrams/development-loop.html) is a visual reference; this page defines our current working system.
