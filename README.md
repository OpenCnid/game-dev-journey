# Game Dev Journey

**A software engineer's first game, built with curiosity, experimentation, and AI.**

I've never made a game before. I know how to build software, but making something fun is a new challenge: controls, game feel, pacing, sound, visual feedback, and all the little decisions that make someone want to play again.

This repository documents our journey from that starting point to a finished, small game. I'll bring the ideas, judgment, and persistence. AI and LLMs will help me explore unfamiliar territory, learn the tools, write and review code, and iterate.

Two or three years ago, I wouldn't have felt ready to take this on this way. Today, I think the tools have made it worth trying. That's the motivation behind this project, and the journey will show what actually works, where AI helps, and where we still have to figure things out ourselves.

## Where we are

| Area | Status |
| --- | --- |
| Experience | Software engineering background; first game |
| Game concept | Bullet-heaven survival roguelite for 1-4 players, solo or cooperative PvE, focused on pleasant level-ups, stacking weapons and powerful builds; first person for MVP/alpha, optional third person parked for later; The Engine Kingdoms AI-world isekai direction |
| Engine and language | Unity **6000.3.25f1** + C#, URP **17.3.0**; editor and packages pinned |
| Target platform | First prototype: Windows desktop, keyboard and mouse |
| Playable capability | See [current state](docs/current-state.md) for integrated solo/two-player combat, earned weapon growth and separately identified candidate branches |
| Current work | See [planning](docs/planning/README.md) and Linear; the document map links the GDD, decisions, tasks and evidence |
| Performance target | At least 144 FPS in the declared supported envelope; broad hardware reach inspired by League of Legends/Valorant; final-game and low-end targets not validated |

Open [the active Unity project](prototypes/001-stacked-weapons/README.md) for setup, controls and build commands. It remains under `prototypes/001-stacked-weapons/`; `game/` is a placeholder, with no authorized migration.

Our current benchmark machine is the owner's desktop: Intel Core i7-10700K, NVIDIA GeForce RTX 3090, and approximately 64 GB RAM. Keep the existing 1080p benchmark configuration for comparisons. GTX 1080 Ti and older-CPU testing is deferred until the game is more polished. See [the performance plan](docs/performance.md) for the 6.944 ms frame budget and benchmark scenarios.

Late-game ambition includes **more than 10,000 active enemies, potentially all bosses with special abilities**. The [boss workload specification](docs/design/late-game-workload.md) separates this goal from what the current graybox has actually demonstrated. Unity/C# remains our chosen engine and language; target performance is not yet validated.

The prototype now includes Jobs/Burst simulation, culled instanced rendering, and selectable reference paths. See [the measured comparison](docs/playtests/2026-09-30-jobs-burst.md) and [profiling instructions](docs/profiling.md).

Further [frame-spike experiments](docs/playtests/2026-09-30-frame-spike-experiments.md) compare effect rendering, identical-warning deduplication, concurrent preparation, and native state ownership. These experiments retain the original optimized path as the default and record visual diagnostics separately from performance acceptance.

The next [renderer comparison](docs/playtests/2026-09-30-renderer-experiments.md) tests compact indirect projectiles, combined character meshes, and cached warning collection against native-state simulation. It includes separate CPU/GPU profiles and repeated visible release captures on the current desktop. Renderer variants remain selectable experiments; final-game performance still requires representative content.

## What we're aiming for

- Finish a small game with a clear beginning, playable loop, and ending.
- Learn enough to understand and maintain what we build.
- Test ideas by playing them, including with people who didn't build them.
- Keep an honest record of experiments, mistakes, decisions, and progress.
- Use AI to accelerate the work while taking responsibility for the result.

Our first scope guardrail: **one core mechanic, one compact play space, and one complete loop.** Bigger ideas can wait until that works.

## Repository map

```text
.
|-- README.md
|-- CONTRIBUTING.md
|-- AGENTS.md               # Short entry point for AI tasks
|-- .github/                 # Issue and pull request templates
|-- docs/
|   |-- README.md            # Document map: where to read and write
|   |-- documentation.md     # Placement, authority and traceability contract
|   |-- registry.json        # Every document's ID, purpose and upstream basis
|   |-- catalog.md           # Generated complete document index
|   |-- foundation.md        # What the game is: identity, player experience and systems
|   |-- vision.md            # Motivation, principles, and scope
|   |-- roadmap.md           # Milestones with observable completion criteria
|   |-- architecture.md      # Folder ownership and how the structure can grow
|   |-- workflow.md          # Development loop and use of AI
|   |-- current-state.md     # Current capability, evidence and constraints
|   |-- design/              # GDD chapter index, designs and labeled proposals
|   |-- decisions/           # Reasons behind significant choices
|   |-- devlog/              # Dated progress and lessons
|   `-- playtests/           # Observations from real play sessions
|-- prototypes/             # Small, disposable experiments
|-- game/                   # Reserved placeholder; no active project here
|-- assets/                 # Editable source assets and provenance
`-- tools/                  # Small scripts that earn their place
```

See [the architecture guide](docs/architecture.md) for placement rules.

## Start here

1. Read [the project foundation](docs/foundation.md), then [the document map](docs/README.md).
2. Read [planning](docs/planning/README.md) and [current state](docs/current-state.md); verify the actual checkout and selected Linear issue.
3. Use the [GDD chapter index](docs/design/README.md) and [complete catalogue](docs/catalog.md) to find the relevant design, decisions and evidence.
4. For implementation, follow [the active project's instructions](prototypes/001-stacked-weapons/README.md) and the bounded task brief.
5. Save changed design/results in their prescribed homes and run `python tools/docs.py` before handoff.

Follow along in [the development journal](docs/devlog/README.md), starting with [day zero](docs/devlog/2026-09-30-day-zero.md). Start every fresh planning/task session with the [project foundation](docs/foundation.md), [planning handoff](docs/planning/README.md) and [current state](docs/current-state.md), then the selected task's relevant context. Our [workflow](docs/workflow.md) explains how Linear, GitHub and replaceable AI sessions connect with little overhead. The live board is [First weapon-stacking playtest](https://linear.app/cnid/project/first-weapon-stacking-playtest-89b5952f722e), in the **Game Dev (GDJ)** Linear team.

The [journey review and proposed AI-chat/build system](docs/planning/2026-10-02-development-system.md) connects our progress to a bounded task workflow, with an [interactive development loop](docs/planning/diagrams/development-loop.html) and a reusable task handoff template.

## Contributing and reuse

This starts as a personal learning project with AI assistance. Suggestions and focused contributions are welcome; see [CONTRIBUTING.md](CONTRIBUTING.md).

A project license has not been selected yet. Any third-party assets or code must retain their own attribution and terms; record asset sources in [the asset register](assets/README.md).
