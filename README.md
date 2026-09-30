# Game Dev Journey

**A software engineer's first game, built with curiosity, experimentation, and AI.**

I've never made a game before. I know how to build software, but making something fun is a new challenge: controls, game feel, pacing, sound, visual feedback, and all the little decisions that make someone want to play again.

This repository documents our journey from that starting point to a finished, small game. I'll bring the ideas, judgment, and persistence. AI and LLMs will help me explore unfamiliar territory, learn the tools, write and review code, and iterate.

Two or three years ago, I wouldn't have felt ready to take this on this way. Today, I think the tools have made it worth trying. That's the motivation behind this project, and the journey will show what actually works, where AI helps, and where we still have to figure things out ourselves.

## Where we are

| Area | Status |
| --- | --- |
| Experience | Software engineering background; first game |
| Game concept | To be explored |
| Engine and language | Not selected |
| Target platform | Not selected |
| Playable build | Not yet |
| Current milestone | Find one small interaction worth playing |

There is no game to run yet. Setup and launch instructions will live in [game/README.md](game/README.md) when the first project exists.

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
2. Sketch three tiny ideas using the prompts in [the game brief](docs/design/game-brief.md).
3. Pick one mechanic to test, choose tools for that experiment, and record the reasoning in [decisions](docs/decisions/README.md).
4. Build the smallest playable version under [prototypes](prototypes/README.md).
5. Play it, capture observations, and decide whether to keep, change, or discard it.

Follow along in [the development journal](docs/devlog/README.md), starting with [day zero](docs/devlog/2026-09-30-day-zero.md). Our [workflow](docs/workflow.md) explains how we turn those lessons into the next change.

## Contributing and reuse

This starts as a personal learning project with AI assistance. Suggestions and focused contributions are welcome; see [CONTRIBUTING.md](CONTRIBUTING.md).

A project license has not been selected yet. Any third-party assets or code must retain their own attribution and terms; record asset sources in [the asset register](assets/README.md).
