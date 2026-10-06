# How we work

We are one developer working with AI. Keep one implementation task active, shorten the path from an edit to useful feedback, and finish small playable improvements. Change this system when it creates friction.

**Working agreement, October 6:** the owner leads creative direction: the desired player experience, weapons, abilities, enemies, combinations, world and presentation. AI turns that direction into bounded playable changes and handles implementation planning, integration, routine checks and bookkeeping. Frame tasks around creative outcomes the owner can play with. Present consequential creative choices and real tradeoffs clearly; keep routine technical work in the background. The roadmap supports that process. Rough MVP implementations remain acceptable.

## Give each tool one job

| Tool | Owns |
| --- | --- |
| Linear | Priorities, task status, acceptance criteria, dependencies and links to results. |
| GitHub | Code, versioned design/decisions, reproducible checks and evidence. |
| AI chats | Working discussion, implementation and investigation for a bounded task. |
| Unity and standalone builds | What the change actually does, looks like and feels like. |

Start a fresh session with the [project foundation](foundation.md), [planning handoff](planning/README.md) and [current state](current-state.md), then follow only the relevant issue, design or evidence links. The foundation defines the game we are building; plans can evolve within it. The [roadmap](roadmap.md) describes milestone outcomes; Linear owns the live task queue. Do not maintain a second copy of that queue in GitHub Issues or a Markdown checklist. GitHub Issues can still collect an outside report; link the item we choose to work on to Linear.

## Documentation responsibilities

The [document map](README.md) is the navigation entry point. The [documentation contract](documentation.md) owns exact placement, authority and traceability rules; [registry.json](registry.json) inventories every document and generates the [catalogue](catalog.md). New and moved documents must be registered and pass `python tools/docs.py`. This structure is required by the owner's October 6 request for a stricter GDD harness.

| Record | Owns |
| --- | --- |
| [Project foundation](foundation.md) | What the game is: identity, intended player experience, design pillars, core systems and agreed boundaries. |
| [GDD chapter index](design/README.md) | Connected system designs, their existing homes and explicit gaps in the complete player experience. |
| [Decisions](decisions/README.md) | Accepted consequential choices and their reasons; preserve superseded reasoning. |
| [Game brief](design/game-brief.md) and [roadmap](roadmap.md) | Specific design experiments, test settings and milestone sequence. |
| [Planning handoff](planning/README.md) | Near-term intent, open questions and context routing. |
| [Current state](current-state.md) | What exists, checked against actual source and evidence. |
| Playtest/benchmark reports | Dated evidence for exact candidates; preserve history and label corrections. |

A task brief or session summary cannot silently redefine the game. Source and measurements establish facts; the foundation defines the intended product.

## Changing project direction

For a change to the game's identity, player promise, agreed constraints or scope:

1. Name the affected commitment, proposed replacement, reason/evidence and impact on existing work.
2. Follow an explicit decision from the project owner. A current user instruction already authorizing the change is sufficient. Routine implementation, tuning, clearer wording and factual corrections need no new product decision.
3. Record accepted consequential decisions in `docs/decisions/`, update the foundation revision/history and reconcile affected plans/specs. Preserve previous rationale through Git history and superseded records.

Bounded alternatives can be investigated without adopting a new direction. Save the result and the decision it informs.

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

1. Pick one ready task that serves a foundation pillar or resolves an uncertainty about delivering it. Its brief needs **Outcome, Done when, Context**; use the [brief template](planning/task-brief-template.md) when helpful.
2. Start a fresh planning or task session with the issue/question, starting commit and relevant files. Planning/integration is a role; the repo's handoff preserves continuity between sessions. Keep the session bounded, and save a resume note at checkpoints before replacing a session that still has unfinished work.
3. Make the smallest useful change. Use a short-lived branch/worktree when isolation helps. One writer owns a Unity project at a time; serialize its Editor, build and test operations.
4. Review the diff and run the checks appropriate to the change. Fix failures before handoff.
5. Link the commit or PR, checks, build/reproduction instructions and any observations to the issue. Put durable measurements or decisions in the existing repo documents, with a short result and link in Linear. Update the planning handoff/current state when their facts change. For unfinished work, preserve exact revision, completed steps, remaining work, checks and blockers in the [Resume section](planning/task-brief-template.md).
6. Finish the required review/playtest, mark Done, and choose the next task. A merged PR does not establish that a change feels good.

Use the Linear issue ID in task-chat titles, branch names and PR titles. [Linear's GitHub integration](https://linear.app/docs/github) can link them automatically. The Game Dev team's merge rule is configured to **In Review** so completion stays deliberate, including required human playtests. The GitHub integration's connection to this repository has not been verified; use explicit links until it is. Mark Done manually when the issue's checks are satisfied.

## Make game iteration easy for AI and humans

The useful analogy to a web preview is a reproducible game scenario: a known arena, seed, loadout and starting state that we can launch quickly, reset and compare. Build on our existing helpers rather than creating an agent orchestration service.

- Keep combat rules separable from rendering, input and UI so important rules can be checked cheaply.
- Make weapon/enemy tuning and scenario settings explicit, easy to edit and review. Use Unity assets or code where they fit; use the visual editor when it helps author the result.
- Prefer a script that launches a specific scenario and reports its build/source over a long list of manual setup steps. Our player already supports `-playtest`, `-screenshot` and repeatable benchmarks.
- Let AI run repeatable checks and summarize evidence. You review consequential behavior and play the exact candidate for feel, readability and enjoyment. Screenshots and automated tests answer narrower questions.

Owner feedback, October 5: reduce human reading burden. Upgrade cards show the benefit and changed effects; omit unchanged stats, previous values and unnecessary explanatory sentences. Keep detailed diagnostics in evidence reports. Human test instructions should give a short action and the few observations needed for the current experiment.

Use Editor/Play Mode and affected automated checks for ordinary development iteration. Create a standalone for a playable checkpoint, native co-op/performance measurement or an executable-specific investigation; reuse that exact candidate across tests while its source is unchanged. Keep one clearly identified current human candidate and verify the selected scenario when delivering it. The October 5 v7/v3 delivery mix-up shows that naming a new build in chat alone is insufficient.

## Design for the game's future

Owner clarification, October 6: the core loop and genre are chosen, drawing on Vampire Hunters and Vampire Survivors. Build the MVP through a coherent capability sequence rather than repeatedly asking whether that genre loop is fun. Each small task should name the missing capability it completes, its dependencies and how it joins the current playable stack. Play remains useful for improving our implementation, choices and interactions. Rough implementations and shortcuts are compatible with this organized sequence; routine benchmarks remain governed by the proportional checks below.

The owner asks that new features account for the future game and its interactions, not only the current prototype. Before implementing a feature, identify its relevant consumers, state ownership and known extension needs in the design or task record. Explain how it could affect future content, build strategies and balance. Progression is the current example: upgrades can affect weapons, loot generation and future upgrade draws; a weapon-only definition would exclude an already identified need.

Implement a bounded slice with explicit places for those known extensions. Expand through ordinary content and focused effect types as needs become concrete; an all-purpose rules engine is not required. Validate current behavior and the boundaries affected by the known future cases. Preserve measured scalability requirements and describe risks without claiming that extensibility proves performance or balance.

## Match verification to the change

Owner direction, October 6: prioritize the MVP's combined playable stack. Combat, simultaneous weapons, progression and co-op need to work together so we can keep adding useful layers; rough implementation and shortcuts are acceptable at this stage. The existing successful combined run is sufficient evidence to continue MVP development. Do not benchmark each addition or make measured captures, publication targets or frame/network targets a prerequisite for it. The owner describes a Jenga tower or broccoli: keep connecting useful layers to discover whether the whole game remains viable, even before the structure is polished. Test the combined behavior, fix breakage that blocks play, and continue layering. Profile only a concrete issue that blocks the playable experiment or an explicitly requested investigation. Preserve existing measurements/failures and long-term goals for later validation. The owner stopped the October 6 repeated-capture investigation; do not resume it automatically from an older handoff.

| Change | Useful check |
| --- | --- |
| Documentation | Paths, links and consistency with the current repository. |
| Combat rule or recurring bug | Focused Edit Mode tests for behavior and edge cases. |
| Input, UI, visuals or lifecycle | Play Mode smoke check, affected scenario and pause/restart; human observation where required. |
| Build or delivery | Standalone player launched outside the Editor; distribute its whole build folder. |
| Simulation, rendering or dense content | For MVP layers, the combined playable scenario and affected behavior checks. Measure workload/counters/frame times for an explicitly requested investigation or a concrete problem blocking play. |
| Playable checkpoint | A standalone candidate and a short combined-loop playtest. No routine benchmark or measured-capture requirement. |

Use `tools/unity.ps1` and the [prototype instructions](../prototypes/001-stacked-weapons/README.md). A working combined MVP test does not need a follow-up performance capture. When measurement is specifically needed under the rule above, run one performance capture at a time on the named desktop, with competing builds/benchmarks stopped. An isolated worktree does not isolate CPU/GPU load. There is no hosted CI pipeline yet.

Tests must check meaningful behavior, parity, regressions or resource lifetime; avoid tests that simply repeat the implementation. Preserve reference paths for performance experiments. Record hardware, source/configuration, workload, repetitions and limitations with a measured conclusion. Keep external asset origins/terms in the [asset register](../assets/README.md).

## Improve the workflow from evidence

At a checkpoint, spend a few minutes asking: how long did an edit take to reach a useful test, which steps were manual, and how much review or rework did AI output need? Fix the largest repeated friction and remove unused process. Investigate import/reload times only if they are a measured bottleneck. Unity's [configurable Play Mode](https://docs.unity3d.com/6000.3/Documentation/Manual/configurable-enter-play-mode.html) can shorten waits, but disabled reloads require correct state reset; we have not changed those settings.

The [October 2 journey review](planning/2026-10-02-development-system.md) preserves how we reached this point. Its [interactive development loop](planning/diagrams/development-loop.html) is a visual reference; this page defines our current working system.
