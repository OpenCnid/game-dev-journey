# Testing the intended late-game scale

The owner corrected the benchmark scope: late-game runs can involve more than 10,000 mobs, potentially all bosses using special abilities. The initial 1,000-drone result therefore did not address the intended workload.

We raised the harness capacity to 20,000, bounded the neighbor search, and added a synthetic per-enemy ability state machine, telegraphed area attacks, radial projectile bursts, swept player collision, instanced effects, and an aligned-attack spike mode. Reports now include ability activity and update counts, projectile load, overflow, and simulated duration. The normal one-minute playable encounter remains small while we investigate scaling separately.

Fifteen EditMode tests and one PlayMode test pass, and the pinned Unity release player builds. The first large smoke runs exposed severe CPU simulation cost, including lost simulation time at 20,000 bosses. A visible 15,000-boss diagnostic records the larger rendered workload. See [the measured results](../playtests/2026-09-30-boss-scale.md).

The [workload specification](../design/late-game-workload.md) records what is represented and what is still absent: final boss visuals, skeletal animation, complex navigation, summons, chain effects, pickups, and fully upgraded player weapons. This is a failed performance baseline, not evidence that 144 FPS is possible or impossible in Unity.

The next performance work is to profile movement/avoidance, projectile collision, render submission, and frame catch-up separately; test batched Jobs/Burst where CPU simulation dominates; and compare identical workloads before/after. Validate representative boss content on the target hardware before making a performance claim. Also assess crowd overlap and effect readability: a faster unreadable scene would not meet the gameplay goal.
