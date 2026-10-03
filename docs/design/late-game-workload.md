# Late-game crowd and boss workload

The owner reports that late Vampire Hunters runs can consist entirely of bosses and estimates counts above 10,000. We have not independently measured that game's count or inspected its implementation. Our requirement is to investigate **more than 10,000 active, ability-using enemies**, while retaining the 144 FPS target on the current i7-10700K / RTX 3090 desktop. GTX 1080 Ti and older-CPU testing is deferred until the game is more polished.

Unity/C# remains the chosen stack. A custom engine is not required by this count alone. The implementation and measured costs of AI, avoidance, abilities, collision, animation, and rendering decide whether the target is feasible. We will not infer feasibility from a lower-count cube benchmark.

The October 2 [cooperative direction](../decisions/0002-cooperative-pve-ai-world.md) adds 2-4 players, with two-player testing first. These existing scenarios remain local one-player controls; network synchronization, latency, additional player/weapon load and cooperative enemy targeting are missing costs. The governing AI world premise does not establish an enemy implementation or a networking architecture.

## Current synthetic scenarios

| Scenario | Work |
| --- | --- |
| `regular-v2` | Movement, bounded approximate separation, contact checks, one hitscan rifle, respawns, instanced bodies/heads |
| `boss-mixed-v1` | Regular workload plus a per-enemy ability state machine; every enemy alternates targeted area attacks and eight-way projectile bursts |
| Bosses with synchronized abilities | Same logic, initially aligned cooldowns, to stress simultaneous activation/rendering instead of relying only on staggered scheduling |

Run 1,000 / 5,000 / 10,000 / 15,000 / 20,000 enemies, with an empty scene and small crowds as controls. The capacity of 20,000 is a harness bound, not the final game's agreed maximum. The player follows the same orbit and fires throughout; killed enemies respawn to keep the active count constant. Normal playable combat remains the 35-drone learning slice.

All enemies and projectiles simulate at 60 Hz. Every boss alternates a radius-2.5 area attack with a 0.75-second warning and an eight-projectile radial burst. There is a six-second cooldown after each activation; the area warning adds 0.75 seconds to that attack's cycle. Half start with each ability, with seeded initial phases over six seconds or an aligned one-second initial delay in spike mode. Area targets are locked at warning start. Projectiles move at eight units/second for up to three seconds and use swept relative-motion collision against the player. Benchmarks calculate hits while keeping the player invulnerable.

The projectile pool has 131,072 slots. Overflow is counted, never treated as successful throughput. Projectiles are removed on player collision or lifetime expiry. Area rings and projectiles are instanced and submitted for rendering, including off-camera instances in conservative batch bounds. There is no distance-based ability suppression. Reports retain ability update/activation counts and projectile work so workload reductions are visible.

## Explicit approximations and missing costs

- The arena remains about 60 by 60 units. Ten thousand bodies cannot maintain a realistic personal-space radius in that area. Dense overlap is expected and visible.
- Avoidance samples up to four candidates in each of nine adjacent spatial cells, with rotating insertion order. This bounds neighbor checks to 36 per enemy per tick. It is soft steering, not collision-free packing or obstacle navigation. `regular-v2` changes the earlier algorithm, so earlier `v1` timings are historical observations rather than identical workloads.
- Boss geometry and health are still the same simple bodies and two-hit health as drones. “Boss” identifies the added ability workload, not a finished boss implementation.
- No skeletal animation, ragdolls, complex colliders, transparent particle effects, shadows, summons, chained damage, pickups, or stacked-player-weapon load is represented yet. Opaque warning rings and projectile cubes do not establish the cost of final VFX.
- Both managed-reference and Jobs/Burst paths are now available, along with legacy and culled renderers. The workload semantics remain the same. See [profiling and comparison instructions](../profiling.md); do not treat a backend name alone as proof of speed or correctness.

## Acceptance and next evidence

The requirement remains 144 FPS, with higher caps and uncapped operation available. Use the [performance protocol](../performance.md): release player, visible 1080p window, uncapped throughput, 30-second warm-up and three 60-second captures per scenario. Include aligned-attack spikes and later a full run with spawning/death bursts.

Report frame-time tails and budget misses, not only average FPS. A run with dropped projectiles or simulation time is invalid for acceptance. Compare simulated and wall-clock capture duration. Use this desktop for current comparisons and report later older-hardware results separately. Passing this synthetic workload would still leave final boss behaviors, visual complexity, and fully upgraded weapon combinations to validate.

Next, use examples from the owner's actual runs to add expensive ability families and establish the final encounter envelope. Do not silently reduce active bosses or disable attacks to make a result pass.
