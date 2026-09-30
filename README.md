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
| Engine and language | Unity 6.3 LTS + C# with URP; [accepted decision](docs/decisions/0001-engine-recommendation.md), exact patch pending project setup |
| Target platform | Windows desktop proposed; not finalized |
| Playable build | Not yet |
| Current milestone | Study the reference and test a small stacked-weapon combat loop |
| Performance target | At least 144 FPS in supported crowded fights; higher caps and uncapped rendering supported by design; unmeasured |

There is no game to run yet. Setup and launch instructions will live in [game/README.md](game/README.md) when the first project exists.

Our benchmark CPU is an Intel Core i7-8700K at stock settings, paired with the provisional GTX 1080 Ti GPU baseline; 1080p is the proposed starting resolution. See [the performance plan](docs/performance.md) for the 6.944 ms frame budget, benchmark scenarios, and remaining settings to define.

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
|-- .github/                 # Issue and pull request templates
|-- docs/
|   |-- vision.md            # Motivation, principles, and scope
|   |-- roadmap.md           # Milestones with observable completion criteria
|   |-- architecture.md      # Folder ownership and how the structure can grow
|   |-- workflow.md          # Development loop and use of AI
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

1. Read [the vision](docs/vision.md) and [roadmap](docs/roadmap.md).
2. Read [the Vampire Hunters study](docs/design/vampire-hunters-study.md) and [proposed game brief](docs/design/game-brief.md).
3. Set up the Unity prototype using [the accepted engine decision](docs/decisions/0001-engine-recommendation.md), and pin the exact editor and package versions.
4. Start with the one-minute combat slice in [the stacked-weapons experiment](prototypes/001-stacked-weapons/README.md).
5. Play it, capture observations, and decide whether to keep, change, or discard it.

Follow along in [the development journal](docs/devlog/README.md), starting with [day zero](docs/devlog/2026-09-30-day-zero.md). Our [workflow](docs/workflow.md) explains how we turn those lessons into the next change.

## Contributing and reuse

This starts as a personal learning project with AI assistance. Suggestions and focused contributions are welcome; see [CONTRIBUTING.md](CONTRIBUTING.md).

A project license has not been selected yet. Any third-party assets or code must retain their own attribution and terms; record asset sources in [the asset register](assets/README.md).
