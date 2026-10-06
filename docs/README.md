# Documentation map

Read [the foundation](foundation.md) first. This index then tells a human or AI session where to find each kind of answer and where to save new work. The [documentation contract](documentation.md) defines the rules; the [complete catalogue](catalog.md) lists every Markdown document, its purpose and upstream basis.

## Find an answer

| Question | Authoritative location | Why it lives there |
| --- | --- | --- |
| What game are we making? | [Foundation](foundation.md) | Accepted identity, player promise, pillars and boundaries. |
| How does the game fit together? | [GDD chapter index](design/README.md) | Connects the player journey and system designs; exposes missing design decisions. |
| What did we decide, and why? | [Decision records](decisions/README.md) | Dated owner decisions, alternatives, consequences and supersession. |
| What should we work on next? | [Planning handoff](planning/README.md), [roadmap](roadmap.md) and [Linear](https://linear.app/cnid/project/first-weapon-stacking-playtest-89b5952f722e) | Repo files explain intent and sequence; Linear owns live priority/status/dependencies. |
| What does this task need to deliver? | [Task briefs in the catalogue](catalog.md#task) | A bounded outcome linked to design, acceptance and a Resume. |
| What actually exists? | [Current state](current-state.md) | Revision-aware orientation pointing to source and evidence. |
| What happened when we tested it? | [Evidence catalogue](catalog.md#evidence) and [playtest guide](playtests/README.md) | Exact source/build, automated results, human observations and limitations. |
| How is it built or run? | [Technical documents](catalog.md#technical), [Unity project guide](../prototypes/001-stacked-weapons/README.md), [tools](../tools/README.md) | Engineering constraints and reproducible commands, separate from product intent. |
| How should a session work and save results? | [Workflow](workflow.md) and [documentation contract](documentation.md) | Responsibilities, change control, placement and handoff rules. |
| What is historical context? | [Historical records](catalog.md#history) | Preserves previous reasoning without turning it into current instructions. |

## Start a session

1. Read foundation, this map, planning handoff and current state. Check checkout/commit and Linear Resume.
2. Use the GDD index or catalogue to select only the relevant branch. Follow its upstream decisions and downstream evidence when the question needs them.
3. Use the [design-session prompt](planning/start-design-session.md), [planning-session prompt](planning/start-planning-session.md) or [task template](planning/task-brief-template.md).
4. Save changed facts in their owning document; register new/moved Markdown and regenerate the catalogue. Run `python tools/docs.py` before handoff.

## Trace an example

Shared XP and personal growth can be followed from [decision 0003](decisions/0003-shared-xp-personal-upgrade-draws.md), through [progression design](design/progression-and-upgrade-pools.md) and the [first earned-choice contract](design/first-earned-level-up.md), to [GDJ-23](planning/tasks/GDJ-23.md), its [growth evidence](playtests/2026-10-05-earned-growth.md) and [current state](current-state.md). [Decision 0013](decisions/0013-readable-xp-and-safe-first-level-up.md) records the later owner revision after play. The original live-choice rule is therefore historical.

Run `python tools/docs.py --trace task-gdj-23` for the indexed upstream chain and direct downstream consumers. Trace links locate the reasoning; read the dates and explicit acceptance in those records before applying a rule.
