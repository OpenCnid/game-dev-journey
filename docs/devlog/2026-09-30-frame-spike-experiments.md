# Four experiments after Jobs/Burst

We added independently selectable effect-visibility diagnostics, exact warning deduplication, overlapping renderer preparation, and native crowd/projectile state ownership. Twenty-seven automated tests pass, including gameplay parity and rendered combinations. The original optimized configuration remains the default.

An early comparison exposed a harness problem: wall-clock warm-up alone allowed startup stalls to shift the simulated attack phase. The harness now waits for both clocks, records simulation start/end times, and resets its frame clock between runs. The old exploratory captures remain labeled in the evidence; the final comparison uses the corrected build.

On our current i7-10700K / RTX 3090 desktop, the aligned experiments show useful local gains without solving the overall 20,000-boss spikes. Native ownership reduced projectile CPU cost from roughly 0.46–0.52 to 0.18 ms/tick and had lower p99 across three full captures. Warning deduplication improved warning-heavy intervals in the short screen. Overlapping preparation did not establish an overall win, and combining every change did not consistently beat native ownership alone.

Hiding projectile visuals while preserving all simulation substantially reduced frame-time tails, making projectile rendering the strongest next lead. Those hidden-effect captures are diagnostic only. No fully visible mode met the 6.944 ms p99 target, and isolated hitches remain unexplained. We kept the experimental modes opt-in and documented the memory tradeoff of separate preparation buffers.

See [the full report and raw evidence](../playtests/2026-09-30-frame-spike-experiments.md) and [reproduction commands](../profiling.md#effect-scheduling-and-native-state-experiments). Next profile dense projectile volleys on the CPU render thread and GPU before choosing the next rendering change.
