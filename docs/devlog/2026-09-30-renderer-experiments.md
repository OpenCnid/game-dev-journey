# Rendering fewer submissions, keeping every attack

The native simulation baseline made projectile rendering the next lead. We tested simpler projectile geometry, a compact indirect renderer, combined character meshes, and cached warning collection independently before repeating the strongest options.

The compact renderer writes interpolated position/scale records instead of matrices and submits projectiles together. Character bodies and heads now have an optional shared mesh/material while retaining their geometry. These changes leave every enemy, attack, and collision active. Simpler projectile geometry helped modestly by itself; warning caching did not establish an independent speedup.

Across three full captures per configuration, native-state baseline p99 was 11.209–11.925 ms. The combined renderer achieved 3.743–4.297 ms, averaging 400–468 FPS on our i7-10700K / RTX 3090 at 1080p. Each full capture simulated 3,600 ticks and spawned 720,000 projectiles, with no dropped projectiles or catch-up-limit events. Two frames out of the combined runs' 77,380 exceeded the 144 FPS budget; the worst was 7.502 ms.

CPU/render-thread and GPU profiles both showed savings. A disjoint GPU timestamp query produced one impossible duration in the short screen, so timing validation now rejects invalid engine samples and preserves the raw evidence. Frame-interval results were unaffected. Thirty-five automated tests pass.

We should carry forward native state, compact projectile rendering, and combined character meshes while retaining comparison switches. The synthetic 20,000-boss configuration now passes the proposed p99 gate on this desktop. Real animation, richer boss abilities, stacked weapons, readability, long sessions, and older hardware still need validation. More algorithms are not the immediate priority; representative gameplay is.

See [the complete report](../playtests/2026-09-30-renderer-experiments.md), [raw evidence](../playtests/evidence/renderers/), and [reproduction instructions](../profiling.md#projectile-and-character-renderer-experiments).
