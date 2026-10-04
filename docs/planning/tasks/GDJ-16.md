# GDJ-16: readable damage in the rifle arena

## Outcome and reason

Implement [GDJ-16](https://linear.app/cnid/issue/GDJ-16/show-enemy-health-and-enemyplayer-damage-clearly) for the foundation's readable, responsive combat pillar. In the [GDJ-2 baseline](../../playtests/2026-10-03-rifle-baseline.md), the developer could not identify wounded two-hit drones when kiting a group. Preserve that exact candidate as the control.

## Implementation contract, recorded before code changes

- Simulation remains the only writer of health, hits, kills and respawns. Expose read-only enemy health and an immutable shot result containing the hit point, target incarnation, actual damage and health after damage, captured before a killed target respawns. Preserve the existing firing API and cadence for benchmark/reference callers.
- Presentation reads health every frame; it never estimates it from shot count. Show full and wounded bars above on-screen enemies within 40 metres, with two segments for the current two-hit drone. Limit the list to the nearest 64 visible enemies. Do not create an object per enemy. This flat arena has no interior occluders; depth-aware visibility and dense-group decluttering remain future work.
- Use a bounded pool of brief world-space impact sparks (white hit, larger gold kill), a short hit marker and longer gold kill marker. Capture death effects at the old hit point, so they cannot follow a respawned slot. No shader/backend promotion or tracer change is required.
- Replace the existing 0.2-second full-screen red wash with a 0.35-second edge pulse and 0.65-second actual-health-loss label, keeping the aim area readable. Health loss, not contact proximity, triggers it.
- Freeze transient damage cues during pause. Let the last cues expire after a win/loss; clear them on restart, ready state and benchmark entry. Enemy health bars remain inspectable while paused and disappear after the run ends. Disable the new feedback path in synthetic benchmarks to preserve their workload definition.

## Known future interactions

Shot results carry target incarnation and health outcome rather than encoding a rifle-specific animation. Later weapons can use the same damage-result contract, and future authoritative cooperative events can add source/owner attribution. Health maxima currently belong to the one drone type; new enemy types need per-type/per-entity maxima. The bounded presentation pool may replace older effects under simultaneous weapon bursts; it must never drop authoritative damage. Later through-wall visibility, ranged threats, upgrades and accessibility settings should revise presentation without moving health ownership.

Ammo, crosshair-gated autofire, jump, ranged enemies, traits and rifle tracer alignment are outside this change. GDJ-15 owns the tracer follow-up. A 35-drone check cannot establish performance at 20,000 enemies.

## Verification and acceptance

Check hit/miss/cooldown outcomes, wounded health, lethal health snapshot and fresh incarnation, restart, pause/terminal feedback and visible-bar filtering. Run relevant Edit/Play Mode checks and a fresh standalone build with `tools/unity.ps1`; inspect the player log and captures. Human comparison must establish first-hit wound, second-hit death, player damage, usefulness for kiting and clutter. Save exact source/configuration and checks in a playtest report; keep the issue In Review until the human result is recorded.
