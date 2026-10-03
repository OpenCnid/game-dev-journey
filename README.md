# Game Dev Journey

**A software engineer's first game, built with curiosity, experimentation, and AI.**

I've never made a game before. I know how to build software, but making something fun is a new challenge: controls, game feel, pacing, sound, visual feedback, and all the little decisions that make someone want to play again.

This repository documents our journey from that starting point to a finished, small game. I'll bring the ideas, judgment, and persistence. AI and LLMs will help me explore unfamiliar territory, learn the tools, write and review code, and iterate.

Two or three years ago, I wouldn't have felt ready to take this on this way. Today, I think the tools have made it worth trying. That's the motivation behind this project, and the journey will show what actually works, where AI helps, and where we still have to figure things out ourselves.

## Where we are

| Area | Status |
| --- | --- |
| Experience | Software engineering background; first game |
| Game concept | First-person horde survival inspired by Vampire Hunters, focused on stacking weapons and powerful builds |
| Engine and language | Unity **6000.3.25f1** + C#, URP **17.3.0**; editor and packages pinned |
| Target platform | First prototype: Windows desktop, keyboard and mouse |
| Playable build | One-weapon arena with health, pause/restart, and crowd/boss stress scenarios up to 20,000 enemies |
| Current milestone | Evaluate one rifle, then compare it with three distinct simultaneous weapons and a small upgrade loop |
| Performance target | At least 144 FPS in supported crowded fights; higher caps and uncapped rendering available; target not validated |

Open [the prototype project](prototypes/001-stacked-weapons/README.md) for setup, controls, build commands, and benchmark instructions. The maintained game will move into `game/` after the experiment is evaluated.

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
|   |-- foundation.md        # What the game is: identity, player experience and systems
|   |-- vision.md            # Motivation, principles, and scope
|   |-- roadmap.md           # Milestones with observable completion criteria
|   |-- architecture.md      # Folder ownership and how the structure can grow
|   |-- workflow.md          # Development loop and use of AI
|   |-- current-state.md     # Current capability, evidence and constraints
|   |-- design/              # Current game brief
|   |-- decisions/           # Reasons behind significant choices
|   |-- devlog/              # Dated progress and lessons
|   `-- playtests/           # Observations from real play sessions
|-- prototypes/             # Small, disposable experiments
|-- game/                   # The selected game's engine project
|-- assets/                 # Editable source assets and provenance
`-- tools/                  # Small scripts that earn their place
```

See [the architecture guide](docs/architecture.md) for placement rules.

## Start here

1. Read [the project foundation](docs/foundation.md), then [the vision](docs/vision.md) and [roadmap](docs/roadmap.md) as needed.
2. Read [the Vampire Hunters study](docs/design/vampire-hunters-study.md) and [proposed game brief](docs/design/game-brief.md).
3. Open the prototype with its pinned editor, following [the project instructions](prototypes/001-stacked-weapons/README.md).
4. Play the one-minute combat slice and run its repeatable crowd benchmark.
5. Play it, capture observations, and decide whether to keep, change, or discard it.

Follow along in [the development journal](docs/devlog/README.md), starting with [day zero](docs/devlog/2026-09-30-day-zero.md). Start every fresh planning/task session with the [project foundation](docs/foundation.md), [planning handoff](docs/planning/README.md) and [current state](docs/current-state.md), then the selected task's relevant context. Our [workflow](docs/workflow.md) explains how Linear, GitHub and replaceable AI sessions connect with little overhead. The live board is [First weapon-stacking playtest](https://linear.app/cnid/project/first-weapon-stacking-playtest-89b5952f722e), in the **Game Dev (GDJ)** Linear team.

The [journey review and proposed AI-chat/build system](docs/planning/2026-10-02-development-system.md) connects our progress to a bounded task workflow, with an [interactive development loop](docs/planning/diagrams/development-loop.html) and a reusable task handoff template.

## Contributing and reuse

This starts as a personal learning project with AI assistance. Suggestions and focused contributions are welcome; see [CONTRIBUTING.md](CONTRIBUTING.md).

A project license has not been selected yet. Any third-party assets or code must retain their own attribution and terms; record asset sources in [the asset register](assets/README.md).
