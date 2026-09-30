# First playable: one rifle and a repeatable crowd

**Date:** 2026-09-30

The [first Unity project](../../prototypes/001-stacked-weapons/README.md) is now implemented and builds as a Windows player. It contains a 60-second survival encounter, automatic hitscan rifle, approaching drones, hit/damage feedback, health, pause, and restart. Input and rendering are separate from fixed-rate combat simulation.

The crowd uses preallocated arrays and local spatial buckets, with instanced placeholder rendering. It remains a main-thread implementation; we have not introduced Jobs or claimed a Burst speedup. Keeping a measurable simulation boundary lets us evaluate those options when needed.

The benchmark runs a scripted orbit with continuous firing at 100, 500, or 1,000 enemies. It writes frame samples and a summary with machine/settings, source provenance, percentiles, spikes, and timing availability. A test with a hidden window produced misleadingly high throughput, so full scripted rendering measurements now require an explicitly visible window.

All 12 simulation/metrics test cases and the runtime smoke test passed. A visible 1,000-drone capture ran on this development machine; [the validation note](../playtests/2026-09-30-prototype-performance.md) records the evidence and limits. We have not validated 144 FPS on the older target PC.

AI helped create the initial code and tooling. The first Unity compile caught a type-name ambiguity, and the editor's actual package resolution corrected the test framework pin. Execution and visual inspection were necessary to verify the generated project.

Next: play the one-weapon encounter, evaluate feel, and profile a representative crowd before adding more weapons and upgrades.
