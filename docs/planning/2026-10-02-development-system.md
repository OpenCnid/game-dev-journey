# Our journey and proposed development system

Reviewed on October 2, 2026, against source `7414fac`, the available original conversation, all 18 repository commits, and the design, roadmap and benchmark reports. This is a proposed operating model; it does not promote renderer defaults, implement new gameplay, or create/reorganize chats.

The subsequent solo/AI workflow discussion established [our current working system](../workflow.md): Linear for the live queue, GitHub for code/evidence, bounded task chats, and checks matched to each change. The developer then replaced the permanent planning-chat idea with [a planning handoff](README.md) and fresh sessions; [the saved discussion](sessions/2026-10-02-solo-ai-system.md) records that correction. The proposal below preserves the reasoning and original suggested queue. Its planning-hub references describe the earlier proposal, not the current session policy.

## What the journey tells us

We began with a software engineer's first game and selected weapon stacking and powerful builds as the experience to test. Concern about Vampire Hunters slowing down led us to investigate crowd performance early. The user then clarified that the intended late game could contain more than 10,000 ability-heavy bosses. That changed the technical risk substantially and justified a synthetic stress harness.

Jobs/Burst, native state and cheaper rendering produced large measured improvements. Our most recent combined configuration passed the 6.944 ms p99 diagnostic gate in all three full 20,000-boss runs on the current desktop. The first gameplay question remains unanswered: does growing from one weapon to three feel satisfying? The prototype still has one rifle, and human enjoyment/readability observations are outstanding.

The conversation and repository show a productive engineering investigation, followed by a need to return to gameplay. We should preserve the performance harness and put the next milestone around a playable experience with a stopping condition.

## Evidence timeline

These stages show order, not elapsed effort. All existing commits were recorded on September 30, 2026. Different workload rows are not one controlled before/after benchmark.

| Stage | Trigger and outcome | Repository evidence |
| --- | --- | --- |
| Starting point | Private repo, first-game motivation, folders and AI-assisted workflow. | `2965084`; [README](../../README.md), [workflow](../workflow.md) |
| Core promise | Vampire Hunters inspiration narrowed to weapon stacking and powerful builds; three weapons/six upgrades/five minutes proposed. | `d9a504e`; [game brief](../design/game-brief.md) |
| Performance goal | 144 FPS or higher; initial GTX 1080 Ti + stock i7-8700K baseline proposed. | `6b342e9`, `8159773`; [performance policy](../performance.md) |
| Engine decision | Unity/C# chosen for available crowd tools and a familiar language; exact editor/packages subsequently pinned. | `390e403`, `2a6f1c5`, `934d976`; [decision](../decisions/0001-engine-recommendation.md) |
| First executable slice | Movement, rifle, feedback, health, pause/restart/end and standalone crowd benchmark. | `934d976`; [first playable](../devlog/2026-09-30-first-playable.md) |
| Workload correction | 1,000 mobs was insufficient; expand to 20,000 with area warnings and projectile abilities. Large managed runs overloaded. | `a8e0a0e`, `8477765`; [boss report](../playtests/2026-09-30-boss-scale.md) |
| Batch simulation/rendering | 15,000 bosses: about 7.3 FPS managed, 133 with Burst, 252–257 with Burst and culled rendering; 20,000 synchronized bosses still missed p99. | `ac3bf1c`, `1aadf1f`; [Jobs/Burst comparison](../playtests/2026-09-30-jobs-burst.md) |
| Hardware correction | User changed development benchmarking to i7-10700K / RTX 3090 / approximately 64 GB at 1080p; older hardware deferred. | `c43bc19`; [current policy](../performance.md) |
| Controlled experiments | Native state reduced projectile CPU cost; exact warning dedup helped warning-heavy phases; overlapping preparation had no dependable total win. Corrected warm-up alignment. | `0295424`, `b44fce7`, `51d40e9`; [experiment report](../playtests/2026-09-30-frame-spike-experiments.md) |
| Cheaper rendering | Combined renderer: 400–468 average FPS, 3.74–4.30 ms p99, 3/3 passes at 20,000 synchronized bosses. 35 automated tests passed. All options still opt-in. | `c5ed740`, `8fa326a`, `7414fac`; [renderer report](../playtests/2026-09-30-renderer-experiments.md) |
| Return to gameplay | Proposed: winning preset validation, current-combat playtest, three distinct weapons, then the five-minute upgrade loop. | Latest original-chat recommendation; [roadmap](../roadmap.md), [brief](../design/game-brief.md) |

The recent combined benchmark recorded two frames over 6.944 ms out of 77,380. It supports a synthetic-workload percentile result on this desktop. It does not establish a hard every-frame minimum, older-hardware support, representative final boss content, or player enjoyment.

## Keep and improve

| Keep | Why it helped | Adjustment |
| --- | --- | --- |
| Small executable slices | Real builds exposed costs that engine/language speculation could not settle. | Put each new slice around a player-visible outcome. |
| Exact versions, source stamps and raw evidence | Results can be traced back to configurations and code. | Record the same provenance in every task handoff. |
| Reference paths and parity tests | Rendering/simulation experiments retained gameplay work. | Preserve controls while promoting a measured configuration deliberately. |
| Controlled hypotheses | We distinguished useful changes from plausible but unproven algorithms. | Stop an experiment when its stated question is answered; park extra ideas. |
| User corrections | Workload and hardware clarifications improved the benchmark plan. | Update one current-state entry and link it from tasks; mark superseded assumptions explicitly. |
| One connected conversation | Continuity accelerated the early investigation. | Split feature/experiment work into bounded chats, then return outcomes to the planning hub and repo. |

Three current gaps matter most:

1. **Gameplay evidence:** tests and screenshots show execution, but cannot establish satisfying combat or readable threats. Record a short current-rifle playtest before expansion and three short sessions once stacking exists.
2. **Decision-to-implementation closure:** recommendations to carry forward native state and rendering are still selectable experiments. A dedicated task should promote a named development preset after staggered and endurance checks, with the reference path retained.
3. **Representative validation:** synthetic synchronized bosses exclude finished animation, navigation, transparent VFX, pickups and the fully upgraded player build. Add those costs to measured scenarios as the features land.

We have no evidence that Bayesian algorithms, asset lazy loading, another language or a custom engine would improve the measured steady-state workload. Reopen an optimization only with a concrete bottleneck and an equivalent comparison. Asset preloading becomes a separate question when substantial content is introduced.

## Chat organization

Use one Codex project connected to this repository. Keep one planning/integration chat and create a temporary chat for a bounded feature, experiment, design question or review when needed. These are roles, not a requirement to run five agents or chats simultaneously.

The existing project chats are **“Set up game development repo”** and **“Chat Branching”**. Treat the former as historical context. This chat can establish the planning system; do not depend on either conversation as the only record of current state. The following names are examples for future tasks, not chats created by this document.

| Role | Example title | Finish line | Durable output |
| --- | --- | --- | --- |
| Planning/integration | Game plan — current milestone | Pick one next priority and accept completed outcomes. | Current state, priority, decisions and links. |
| Gameplay feature | Gameplay — three simultaneous weapons | Implement the scoped behavior, run checks and hand off a playable build. | Source commit, checks, controls and changed behavior. |
| Performance experiment | Performance — combined renderer endurance | Compare stated configurations and decide keep/revise/park. | Reproduction protocol, results, workload counts, evidence and limits. |
| Design/reference study | Design — weapon differences and upgrade paths | Answer one player-facing question. | Short spec or playtest observations separated from interpretation. |
| Review/playtest | Review — first stacked-weapons slice | Evaluate an exact candidate and report concrete findings. | Locations/reproduction steps, observations and follow-up task. |

Start a fresh task with a brief by default. Fork a chat when earlier dialogue is essential to the question; inherited history does not replace checking the current commit and docs. Continue the same chat while pursuing the same outcome. Start a new task when the objective changes materially. Finish with a repository handoff, and close/archive completed task chats when their results are preserved and no work remains. Chat names are for navigation; stable task IDs and commits connect the evidence.

This uses Codex's documented [worktrees and chat forks](https://developers.openai.com/blog/mastering-codex-remote-for-engineering) and [durable project memory](https://developers.openai.com/blog/run-long-horizon-tasks-with-codex). The role structure and concurrency policy are our proposed choices. Keep future project instructions compact, following the official guidance to [avoid bloated agent context](https://developers.openai.com/blog/rethinking-skills-and-prompts-for-gpt-6-astra).

## Repository memory and task briefs

Use existing records whenever they fit:

| Record | Owns |
| --- | --- |
| `docs/roadmap.md` | Milestone sequence and observable completion criteria. |
| `docs/design/game-brief.md` | Intended player experience and scope. |
| `docs/performance.md`, `docs/profiling.md` | Current hardware/settings policy and measurement protocol. |
| `docs/decisions/` | Accepted costly or recurring decisions, with reasons. |
| `docs/playtests/` | Existing benchmark evidence and human session notes, clearly labeled. |
| `docs/devlog/` | Chronological lessons linked to the authoritative records. |
| Issue or task brief | One outcome, scope, owner, acceptance and handoff. |

A compact current-state page is the next recommended addition: current verified commit, active milestone, accepted configuration, next task and unresolved questions. It should link to design/results rather than copy whole reports. A small `AGENTS.md` can later point new chats at the current-state page, pinned project and verification tools; add project-specific rules only when they earn their place. Neither is required to understand this proposal.

Use the existing [idea/experiment issue template](../../.github/ISSUE_TEMPLATE/experiment.md) or the [task brief and handoff template](task-brief-template.md). Do not paste entire private conversations into Git. Capture only the facts, constraints, decisions and evidence needed to continue.

## Build and integration loop

Open the [interactive development loop](diagrams/development-loop.html), authored from the [workflow specification](diagrams/development-loop.json).

For code tasks:

1. State one outcome and the relevant base commit/configuration. Define how to verify it and when to stop.
2. Implement on a short-lived branch/worktree when isolation helps. Review the actual diff.
3. Run relevant Edit Mode and Play Mode checks using `tools/unity.ps1`. Keep tests tied to behavior, parity, regression risks and resource lifetime.
4. Build the standalone player with pinned Unity. Use release builds for performance conclusions; use development builds for focused profiler diagnosis.
5. Play the affected behavior and restart path. If simulation/rendering changes, run the relevant controlled workload with counters as well as frame-time distributions.
6. Record source/configuration, checks, observed results and limitations. Integrate a coherent result, update affected docs and hand off one next decision. Failed checks return to implementation; keep/revise/park decisions follow the evidence.

Documentation-only tasks check paths, links and accuracy; they do not require a Unity rebuild. We already have local helpers; there is no hosted CI pipeline. Add CI only as a separate task after choosing a runner/licensing/artifact policy. GPU performance acceptance remains on the named desktop, even if automated compilation/tests later run elsewhere.

Start with **one code task in progress**. Additional read-only design/review work can be separate when explicitly requested. Allow one writer per Unity project and serialize Unity editor/build/test operations for a project. Run only one performance capture on the desktop at a time, with competing builds/benchmarks stopped. Isolated source worktrees do not isolate CPU/GPU load. Integrate an upstream dependency before benchmarking its downstream feature, and record the combined commit.

## Next queue, in dependency order

These are the original proposed groupings, not live task statuses or completed milestones. Proposal IDs below are local to this retrospective. The [Linear project](https://linear.app/cnid/project/first-weapon-stacking-playtest-89b5952f722e) now separates the rifle playtest from weapon implementation and owns the actual GDJ issue IDs and dependencies.

| ID | Outcome | Acceptance |
| --- | --- | --- |
| P1 | Establish a named combined development preset, retaining reference controls. | Relevant parity/runtime checks; visible synchronized and staggered comparison; five-minute endurance check with source/configuration and workload counters. Document the exact preset and its limits. |
| P2 | Observe movement/rifle and implement three distinct simultaneously firing weapons if current combat is promising. | Independent timers, focused/spread/piercing behavior, distinct feedback and repeatable restart; recorded human combat observations. Re-measure the changed workload. |
| P3 | Complete five-minute survival/upgrade experiment. | One arena, energy pickups, three weapon slots, six upgrades, win/loss/restart and at least two recognizable build paths. |
| P4 | Evaluate the actual stacked-weapons loop. | Three short documented playtests; representative fully upgraded workload captures; keep/revise/discard decision before expanding content. |

Treat performance and playability as separate evidence requirements attached to the same feature milestone. Stop speculative optimization now; reopen a focused experiment when the new slice shows a measured regression or an unmet frame budget.

## Visualization verification

The standalone workflow is generated with Archify 2.16. The exact specification/artifact receipts and browser/perceptual verification status are recorded in [the diagram verification record](diagrams/verification.md). Interactive journey and chat-role views accompany this proposal in the conversation; their selections do not create tasks, modify project settings or implement the proposed game changes.
