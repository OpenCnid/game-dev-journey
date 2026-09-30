# Experiment 001: stacked weapons

**Status:** specification only; no code, engine project, or executable yet.

**Question:** does adding simultaneous weapons create satisfying power growth and distinct builds while keeping combat readable?

**Tools and run instructions:** pending engine selection. This folder will own a self-contained prototype project once selected.

## Build in three slices

### A. One minute of combat

- A flat bounded arena with room to circle enemies and no navigation obstacles initially.
- Mouse look, keyboard movement, pointer capture/release, and pause.
- One enemy type that approaches, deals contact damage at a limited cadence, and can die.
- One aim-directed hitscan weapon that fires automatically on a timer.
- Health, clear hit/damage feedback, and restart after death.

**Check:** movement and aiming work; shots hit the intended target; contact damage cannot apply every rendered frame; pause freezes combat; restarting resets the encounter. Play for a minute before adding more systems.

### B. A visible increase in firepower

- Three independent weapon slots, initially using a single-shot, spread-shot, and piercing-shot behavior.
- Distinct cadence and feedback for each behavior, built from simple raycast attacks first.
- Debug controls to switch between one and three weapons in comparable encounters.
- Small on-screen slot indicators or placeholder models that leave the center of view clear.

**Check:** each weapon has its own cooldown; spread pellets cannot unintentionally hit the player; piercing counts each target once per ray and stops at walls; stacking has no unexplained shared cooldown. Compare readability and power with one versus three weapons.

### C. A five-minute build experiment

- Enemies drop energy pickups on defeat. A collector consumes each pickup at most once.
- Crossing a tunable energy threshold pauses the action and offers three distinct eligible upgrades.
- Six initial upgrade definitions: add spread weapon, add piercing weapon, increase single-shot damage, widen spread while adding pellets, increase piercing target count, shorten all firing intervals.
- Use initial caps of one purchase for each weapon addition and three purchases for each other upgrade. Weapon-specific upgrades require owning that weapon. Tune later; these are our rules.
- Offer only eligible choices, or all remaining choices if fewer than three exist. When none remain, stop offering upgrades and keep excess energy as score.
- If several thresholds are crossed together, process pending choices sequentially before resuming combat.
- Gradually increase spawn pressure within a hard active-enemy cap. Reaching the cap delays spawns instead of exceeding it.
- End at five minutes survived or zero health. If both occur in the same update, death takes precedence. Show time, kills, and loadout, then allow restart.

**Check:** paused time does not advance the run or spawn enemies; an upgrade applies once; invalid or capped choices never appear; the run cannot get stuck on an empty offer; restarting clears enemies, pickups, upgrades, timers, and counters.

**Build comparison:** use debug-selected loadouts and the same encounter settings to compare a wide crowd-clearing spread build with a focused piercing build. Then test earning upgrades during a normal run. Record whether the difference comes from an interesting strength or merely a larger damage total. Debug loadouts ensure the comparison is possible before tuning the random offer distribution.

## Proposed system responsibilities

These describe our implementation, not Vampire Hunters' internal architecture. Start with simple engine-native objects and direct references; add abstraction only where behavior warrants it.

| System | Owns | Boundary |
| --- | --- | --- |
| Player controller | Movement, look, aim origin, input capture | Does not schedule enemies or choose upgrades. |
| Weapon rack | Slots, per-weapon cooldowns, shot emission | Reads weapon definitions and effective stats. |
| Damage receiver | Health, hit response, one-time death notification | Prevents duplicate death rewards. |
| Enemy behavior | Approach movement, separation, contact attacks | Reuses damage handling where practical. |
| Spawn director | Run-time pressure curve, legal spawn positions, active cap | Uses configurable pacing; no unbounded spawn queue. |
| Pickup / progression | Collection, energy thresholds, pending upgrades | Keeps run progress separate from UI rendering. |
| Upgrade application | Eligibility, stack caps, effective stat calculation | Derives effects from base values and selected upgrades. |
| Run controller | Run state, clock, pause, end result, complete reset | Controls when gameplay systems may advance. |
| HUD and choice menu | Health, time, slots, offers, result display | Displays state and submits choices. |

Lifecycle: `ready -> running -> upgrade choice -> running -> won/lost -> ready`. Manual pause returns to its previous state. Menus do not consume gameplay time. Consume pending upgrades before returning to `running`.

Use simple approach steering on the open arena initially. Measure crowd bunching and frame time before adding obstacles, pathfinding, pooling, or more enemy behaviors. Prefer improving an observed bottleneck to building a generalized crowd framework.

## Parameters to keep easy to tune

Movement speed, player health, enemy health/speed/contact interval, each weapon's damage/cooldown/range/spread/piercing count, pickup value/radius, energy thresholds, upgrade caps, spawn cadence, active-enemy cap, and run duration.

Store definitions in the chosen engine's native data format. Label baseline values as our tuning choices. Log engine version, build/commit, machine, resolution, and active-enemy count when profiling; choose a measured performance target before scaling the crowd.

## Evidence and exit decision

For each of three short playtests, record:

- Whether weapon additions are perceptible without a stats explanation.
- Whether different weapons visibly contribute and upgrade paths change strengths.
- Whether the player can identify danger and understand damage taken.
- Whether movement decisions remain useful after gaining power.
- Whether the player wants another run, and which combination they would try.
- Any hitching and the approximate enemy count when it occurs.

Use [playtest notes](../../docs/playtests/README.md). If weapons feel interchangeable, change their behavior before adding more. If combat becomes unreadable, reduce effects and crowd density before expanding scope. If the core is promising, document the result and promote selected work to `game/`.

**Result:** not tested.

**Disposition:** proposed experiment; awaiting engine selection and reference gameplay observations.
