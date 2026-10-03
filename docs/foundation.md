# Project foundation

**Revision 1 — October 2, 2026.** This is the stable starting point for every session. It consolidates the project's existing agreed purpose, player promise and boundaries. Plans, task status and implementation details belong in the documents linked below.

## Purpose

A software engineer making a first game, assisted by AI, will learn the craft and finish a small, original game that is fun to play and understandable to maintain. Working code, impressive crowd counts and a well-organized process support that outcome; they cannot establish it by themselves.

## Player promise

**Grow a powerful arsenal of distinct simultaneous weapons while maneuvering through a readable first-person horde encounter.**

Three pillars guide decisions:

1. **Perceptible power growth:** adding weapons and upgrades changes what the player can do and feel.
2. **Distinct builds:** combinations have recognizable strengths and support meaningful choices.
3. **Readable, responsive combat:** the player can understand threats, damage and weapon contributions while acting smoothly.

Evaluate features and experiments by the pillar they improve or the uncertainty they resolve. Theme, tuning and implementation can evolve while preserving this promise. Human play observations determine whether the experience delivers it.

## Agreed constraints and scope

- Design for **144 FPS or higher** within a declared hardware/settings/encounter envelope, with higher caps and uncapped options. Late-game ambition includes **more than 10,000 active enemies**, potentially ability-heavy bosses. These remain goals requiring representative validation; a synthetic benchmark is evidence for its stated workload. [Performance policy](performance.md) owns the current protocol and hardware.
- **Unity, C# and URP** are the accepted first-prototype tools. Preserve the project's pinned versions during ordinary work. The [engine decision](decisions/0001-engine-recommendation.md) records rationale and revisit conditions; implementation techniques remain open to measured improvement.
- Finish one small complete loop before expanding content. The [game brief](design/game-brief.md) specifies the current experiment; three weapons, six upgrades and five minutes are test settings we can revise.
- Defer multiplayer, accounts/online services, monetization, extensive procedural content and custom-engine work until the first complete playable loop. Build an original game informed by reference play; keep asset origins and terms recorded.
- The developer owns product direction and what is worth keeping. AI assists with investigation, implementation and verification. Keep retained systems understandable and evidence traceable.

## What each document can change

| Record | Authority and expected change |
| --- | --- |
| This foundation | Purpose, player promise, agreed constraints and scope. Revised through an explicit project decision. |
| [Decisions](decisions/README.md) | Accepted consequential choices and reasons; supersede records when choices change. |
| [Design brief](design/game-brief.md) and [roadmap](roadmap.md) | Mechanics, experiment settings and milestone sequence; refine from evidence within the foundation. |
| [Planning handoff](planning/README.md) | Near-term intent, open questions and context routing; update as work progresses. |
| [Current state](current-state.md) | Capability/configuration summaries; correct against actual source and evidence. |
| Linear | Live priority, status, dependencies and task acceptance. |
| Playtest/benchmark reports | Dated evidence for exact candidates; preserve history and label corrections. |

A session summary or task brief cannot silently replace a foundation commitment. Source and measurements establish what exists and what works; this document establishes what we are trying to achieve. Resolve a mismatch by identifying the affected commitment and checking the relevant evidence or user direction.

## Deliberate revisions

For a change to purpose, player promise, agreed constraints or scope:

1. Name the affected commitment, proposed replacement, reason/evidence and impact on existing work.
2. Follow an explicit decision from the project owner. A current user instruction that already authorizes the change is sufficient; routine implementation, tuning and factual corrections need no foundation amendment.
3. Record the accepted consequential decision in `docs/decisions/`, update the foundation revision/history and reconcile affected plans/specs. Preserve the previous rationale through Git history and superseded records.

An alternative can be investigated as a clearly bounded proposal without making it the accepted direction. A faster benchmark does not lower our performance ambition, and a new task does not turn the project into an engine/framework project.

| Revision | Date | Change |
| --- | --- | --- |
| 1 | 2026-10-02 | Consolidated existing commitments and established this foundation as every session's first project document. |
