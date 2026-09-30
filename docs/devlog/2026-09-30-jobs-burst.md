# Measuring Jobs/Burst and rendering changes

The previous 15,000-boss capture averaged 6.7 FPS. We added separate release-stage timers and Unity Profiler markers before introducing selectable optimized paths, so we could measure simulation and rendering changes independently.

Crowd steering/integration and projectile movement/collision now use batched Burst jobs. The renderer uses Burst interpolation, conservative frustum checks, stable matrix compaction, and native instance batches. All enemies and abilities still simulate at 60 Hz. The managed simulation and legacy renderer remain available as controls.

On the developer i7-10700K/RTX 3090, full release comparisons measured about 7.3 FPS for the managed reference, 133 FPS with Jobs/Burst and the original renderer, and 252–257 FPS across three runs with both improvements. Optimized 15,000-boss p99 frame times were 6.40–6.52 ms. All optimized runs retained simulation time and dropped no projectiles, but occasional slower frames remained, including a 20.67 ms outlier.

Synchronized 20,000-boss attacks reached 80,000 active projectiles. The optimized run averaged 222 FPS but had an 11.68 ms p99, so it still failed the proposed frame-time threshold. This is why we report frame-time tails and work counters rather than celebrating average FPS alone.

Twenty-one EditMode tests and one PlayMode test passed. Binary profiler traces were captured and reopened successfully; they confirm named Burst jobs on worker threads and expose remaining preparation/completion-wait costs. The capture timer was also corrected to give a full recording window after warm-up actually ends.

This establishes a much better synthetic prototype baseline, not acceptance on the i7-8700K/GTX 1080 Ti or proof about final boss content. Next investigate synchronized effect spikes, the isolated hitch, and real target-hardware performance. See [measurements and evidence](../playtests/2026-09-30-jobs-burst.md) and [reproduction commands](../profiling.md).
