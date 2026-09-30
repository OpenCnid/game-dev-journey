# Selecting Unity and C#

**Date:** 2026-09-30

The project owner accepted Unity with C# after considering the performance target and the use of LLMs during development. [Decision 0001](../decisions/0001-engine-recommendation.md) now records the accepted direction: Unity 6.3 LTS with URP, with an exact editor patch and compatible packages to be pinned at setup.

The first project will live in `prototypes/001-stacked-weapons/`. We will build the one-weapon combat slice alongside a repeatable crowd benchmark, targeting 144 FPS on the i7-8700K / GTX 1080 Ti baseline. No Unity project has been created and no performance has been measured yet.

LLM-generated changes will be checked against the pinned versions, compiled, and verified through execution. Jobs and Burst remain available for measured bottlenecks; full ECS adoption is not required by this decision.
