# 0001: Use Unity and C# for the first prototype

- **Date:** 2026-09-30
- **Status:** accepted by the project owner on 2026-09-30; implemented in the first prototype.

## Context

We are building a first game around stacking weapons and creating powerful builds in a first-person horde encounter. Our [performance target](../performance.md) is at least 144 FPS. The original i7-8700K / GTX 1080 Ti requirement was revised by the owner on 2026-09-30: current benchmarking uses the i7-10700K / RTX 3090 desktop, with existing 1080p captures retained for comparisons. Older-hardware testing is deferred until the game is more polished. This changes the hardware plan, not the accepted engine decision. LLM assistance must work well with the language and development workflow.

## Decision

Use **Unity with C# and the Universal Render Pipeline (URP)**. The first prototype pins **6000.3.25f1** (`e1dba0a9aba4`), **URP 17.3.0**, **Input System 1.20.0**, and **Test Framework 1.6.0**, with transitive versions preserved in Unity's generated lockfile. URP and the test framework versions were verified against the installed editor's bundled packages. [Pinned editor release](https://unity.com/releases/editor/whats-new/6000.3.25f1).

This version choice favors keeping our first prototype on a consistent editor/package baseline. Unity lists 6.3 LTS support through December 2027. Its own guidance generally recommends Supported Update releases for new projects; choosing LTS here is our workflow preference, not a claim that Update releases are unstable or unsuitable. Recheck available patches and relevant fixes at setup. [Unity release support](https://unity.com/releases/unity-6/support).

Use URP for the initial rendering setup and tune lighting, shadows, animation, and effects to our hardware budget. URP supports a broad range of target platforms; selecting it alone does not establish a frame rate. [URP documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/universal-render-pipeline.html).

## Implementation approach

- Keep player controls, UI, run state, and authoring in ordinary Unity C# components initially.
- Give the crowd simulation a clear boundary, compact state, and explicit update ownership so expensive loops can be measured and optimized.
- Use Jobs and Burst for suitable CPU bottlenecks identified in the crowd benchmarks. Jobs can distribute work over worker threads; Burst compiles a supported subset of C# for CPU performance. Burst-compatible code has restrictions, including on managed objects. [Jobs](https://docs.unity3d.com/Manual/job-system-overview.html), [Burst](https://docs.unity3d.com/jp/current/Manual/script-compilation-burst.html).
- Jobs/Burst do not require converting the entire game to Entities/ECS. Introduce Entities only if a prototype demonstrates that its benefits justify its integration and learning cost.
- Benchmark representative rendering, animation, collisions, and maximum weapon activity as well as enemy movement. Faster simulation alone cannot establish the 144 FPS target.

## LLM suitability

C# is my recommended language for this workflow because typed interfaces, compiler diagnostics, small text diffs, and conventional code organization make generated changes easy to inspect and verify. This is an engineering judgment, not a measured claim that an unspecified model performs better in C# than C++, GDScript, or another language.

The model still needs engine-specific context. Common failure cases to check include stale APIs, incompatible package examples, incorrect engine lifecycle assumptions, missing scene references, allocation-heavy update loops, and Burst-incompatible code.

For each meaningful change:

1. Provide the exact editor/package versions, relevant existing code, intended behavior, and a verification step.
2. Keep behavior in readable scripts and explicit data. Make scene/prefab setup inspectable and reproducible; text code does not remove the need to validate editor wiring.
3. Compile in Unity, run focused checks where useful, then play the affected interaction.
4. Measure relevant crowd/weapon changes in a standalone build. A plausible explanation or passing unit test does not demonstrate frame-time performance.

Once the project exists, assess the actual LLM on small representative tasks such as adding a weapon definition, fixing a cooldown bug, and optimizing a measured loop. Judge working behavior and review effort rather than assuming language familiarity guarantees correctness.

## Alternatives considered

| Option | Assessment for this project |
| --- | --- |
| Godot with C# | Viable: Godot supports C#, so LLM language familiarity does not rule it out. Unity's Jobs/Burst route is the deciding advantage in my recommendation for this particular CPU-heavy crowd experiment. [Godot C# documentation](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html). |
| Unreal with C++ and Blueprints | Viable: Unreal supports both, and its MassEntity framework addresses batched entity processing. I expect more workflow and integration overhead for our first text-driven prototype; this is a project-fit judgment, not a claim that Unreal cannot meet the target. [Coding in Unreal](https://dev.epicgames.com/documentation/en-us/unreal-engine/coding-in-unreal-engine-blueprint-vs-cplusplus), [MassEntity](https://dev.epicgames.com/documentation/unreal-engine/overview-of-mass-entity-in-unreal-engine). |
| Custom engine | Adds rendering, editor, asset pipeline, input, audio, and platform work before we can evaluate the game. Custom crowd systems inside an existing engine are sufficient to investigate our current performance questions. |

## Consequences and revisit conditions

We must learn Unity's component lifecycle and asset workflow as well as C#. Performance work may require a more constrained style of C# than ordinary application code. Neither engine choice nor LLM assistance guarantees the frame budget.

Revisit if the representative benchmark reveals an engine limitation that cannot reasonably be addressed, the actual LLM workflow causes persistent integration problems, or project requirements change. Initial local benchmark diagnostics exist; no cross-engine comparison or target-hardware validation has been performed.
