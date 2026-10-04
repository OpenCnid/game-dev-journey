# GDJ-19: rifle fire-rate controls for testing

## Outcome and context

[GDJ-19](https://linear.app/cnid/issue/GDJ-19/adjust-rifle-fire-rate-in-testing-and-benchmarks) lets the developer compare slow early-game-like rifle fire with continuous mid/late-game-like fire in the combat realm and benchmark. P1 selected it after accepting [GDJ-15](../../playtests/2026-10-04-shot-alignment.md): aiming looked good, had no delay and felt smooth. Presets are testing values, not progression balance. This supports readable, responsive weapon growth before adding a real upgrade system.

## Contract recorded before runtime edits

- One simulation-owned rifle rate, **0.5–120 shots/second**; exact **0.12 s** baseline interval remains default. Menu presets: slow 2/s, baseline 8.33/s, rapid 30/s, stress 120/s; custom input has validation and a visible effective value. Controls are available at start/pause/end, with scrolling if needed. R resets the encounter but retains this session's chosen rate.
- Rate changes preserve fractional cooldown progress; paused time stays frozen. Reject invalid, NaN, infinite and out-of-range values without corrupting cadence. No changes to movement speed, projectile travel speed, damage, aim, global time or progression. No cross-session preference persistence is required.
- Combat already consumes due shots at its displayed pose. Benchmark must consume all due shots per fixed tick so >60/s is not silently capped. Default shot counts and legacy benchmark visuals stay equivalent. Cosmetic pools stay bounded; high-rate shots in one frame can share its one tracer.
- Add invariant-culture player `-rifleRate` and helper `-RifleRate`; record rate/interval in benchmark JSON and requested rate in invocation metadata. Validate report/configuration agreement. Preserve old baseline reports/players at default; reject unverified nondefault rates on an old player. Compare equal rates/workloads for performance conclusions; shortened/hidden smoke only validates functionality.
- This is a local testing override for the current rifle. Future weapon rack/upgrades need per-weapon effective stats/cooldowns, and ammo/reload needs a separate cadence/resource contract. Do not introduce those systems or tune damage to compensate here.

## Verification and resume

Start `feat/rifle-fire-rate-controls` from accepted GDJ-15 source/report branch at `04267ebccc4dfb9eeea3aa32fc77571caff9d43f`, stacked on unmerged PR #5. Reuse the existing managed checkout; preserve complete `StackLab-GDJ15-v1`, GDJ-16 v7 and GDJ-2 players. One Unity writer; pinned editor/packages/renderer settings unchanged.

Check default equivalence, slow/high cadence across frame rates, >60/s benchmark firing, pause/reset/rate-change behavior, invalid input and metadata. Use serialized `tools/unity.ps1` tests/build, record exact release source/configuration, then run slow/high standalone reporting smoke. Open the candidate at the menu so P1 can change rates and resume/start manually. Save result and remaining observations in a dated report; no new performance or progression-balance conclusion follows from this control.
