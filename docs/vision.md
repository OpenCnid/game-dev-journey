# Vision

The [project foundation](foundation.md) is the authoritative session starting point for purpose, player promise, agreed constraints and scope. This page explains the motivation; update its wording consistently with the foundation. Routine planning belongs in the planning handoff, while changes to foundation commitments follow its revision process.

## Why this exists

We want to discover whether a software engineer with no game-development experience can use AI assistance to learn the craft and finish a fun, small game. The finished game matters, and so does understanding how we got there.

The interesting question is not just whether we can generate working code. It is whether we can turn an idea into a playable experience, notice what feels wrong, and improve it.

## What success looks like

- Someone can start, understand the basic action, complete a short session, and restart without developer help.
- The core interaction gives us a reason to keep playing.
- A build runs on the chosen target platform using documented steps.
- Crowded combat targets at least 144 FPS on the agreed baseline configuration, with higher frame rates available on capable hardware; see [performance requirements](performance.md).
- We can explain the important systems and make changes with confidence.
- The journal records both progress and lessons from failed attempts.

## Working principles

1. **Play early.** A rough interaction teaches us things a long design document cannot.
2. **Keep the first game small.** Start with one mechanic and one complete loop.
3. **Understand what we keep.** Ask AI for explanations, inspect changes, and verify behavior.
4. **Make decisions reversible when practical.** Prototype uncertainty before committing to infrastructure.
5. **Record the useful lessons.** A short note about a surprise beats an exhaustive transcript.
6. **Let the player experience guide us.** Technical elegance serves responsiveness, clarity, and fun.

## Initial boundaries

Until the first complete playable loop exists, defer multiplayer, accounts, online services, monetization, extensive procedural content, and custom engine work. These are scope choices for this project, not judgments about what a game should be.

The starting direction is first-person horde survival, inspired by the owner's time playing Vampire Hunters, with a focus on stacking weapons and creating powerful builds. The [game brief](design/game-brief.md) proposes a small test of growing firepower. Unity with C# and URP is the [accepted engine choice](decisions/0001-engine-recommendation.md). Visual style, final platform, and release destination remain open; choose them around the experiment and the time we can realistically spend.
