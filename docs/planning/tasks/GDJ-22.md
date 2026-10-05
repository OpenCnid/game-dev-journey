# GDJ-22 — shared scenario configuration

October 4, 2026. Bounded implementation plan, recorded before runtime edits. Serves readable, responsive combat and gives future perceptible growth a consistent starting definition. Base: merged main `e6434ec7575e2d7e3e81e3d0b799bc15299bb6e1`, foundation revision 18. GDJ-20's fixed local acceptance is complete; older pending-gate notes are superseded.

## Contract and compatibility

- One immutable version-1 scenario describes identity, seed, duration, enemy count/starting HP, rifle body damage, integer head multiplier and interval. Fixed 60 Hz, geometry, movement/contact and return rules remain existing rules, identified by the schema/rules version. Endpoints and local presentation stay outside it.
- Named defaults retain solo precision (35 drones, 40 HP, 20/40 damage, .12 s, seed 137, 60 s) and shared rifle (12 drones, 60 HP, otherwise matching). A named developer parity configuration plus explicit CLI overrides support reproducible comparisons. Labels disclose overrides; canonical content identity covers every field.
- Validate before allocation, launch or adoption: supported version/name, finite duration/interval, integral HP/damage/multiplier/count and bounded ranges. Shared supports at most 35 drones within the existing 1100-byte packet envelope; solo synthetic benchmark retains its separate 20,000-enemy reference workload. No dense replication promise.
- Host selects and freezes configuration for the connection/session. Client validates a bounded offer and acknowledges its session/content identity before host Start. Snapshots/Retry are bound to that identity and increasing run generation. Parse into temporary values; reject malformed, unsupported, stale or changed configuration before committing state. A connection/session identity prevents a different session's binding from being adopted.
- Both-ready Retry creates the next run with the same frozen scenario. Changing shared tuning requires a fresh host/client session; no live shared setter or lobby editor is added. Existing death/return, defeat-before-return/completion and disconnect-abort remain.
- Solo GDJ-19 controls keep 0.5–120 shots/s, presets, fractional cooldown progress and session/restart retention. Selected rate is explicitly forwarded when hosting, rather than silently replaced. Named solo/shared defaults remain distinct in the menu and HUD. Client local values never override host settings.
- Cadence means due shots at the selected interval, including multiple shots due in a fixed tick/frame at rates above 60/s. Verify both authority scheduling and presentation submission; do not equate exact client wall-clock timing with network latency or performance.
- Shared projection, precision geometry, wounded pose and HP bars use configured maximum HP. Native HUD, startup evidence and terminal reports expose scenario/content identity and effective values.

## Bounded execution

1. Extract configuration/validation and wire solo reset/damage/rate, shared reset/damage/cadence, projection and protocol agreement.
2. Add native selection/settings presentation and explicit developer launch settings; keep existing benchmark controls/reporting.
3. Focused Edit/Play tests cover defaults/override parity, invalid/version/nonfinite/range/malformed/stale rejection, acknowledgement, run reset, retained controls and lifecycle. Serialize all Unity operations through `tools/unity.ps1` with this project's Editor closed.
4. Commit clean runtime/test source, release-build and preserve a complete player archive/manifest. Run actual loopback defaults and override, reset/loss and solo/reference reporting smokes. Functional checks imply no new performance result.
5. Present the exact candidate for brief owner visibility/expectation feedback; keep that separate from agent checks. Save report, open/attach draft PR, push and refresh Linear Resume. Do not merge.

## Future consumers and limits

GDJ-6 can derive initial per-player weapon/build state from these definitions. Earned personal state, loot/draw modifiers, offer state and team rewards must remain authority-owned run state, never edits to starting configuration or client uploads. This extraction does not add a modifier system. GDJ-21 can consume the same scenario construction/validation and identity in its game-owned test interface; OS runner placement remains external. Common command ownership, full lobby, progression/thinking space, recovery/migration, four-player/dense replication and runner/viewer redesign are excluded.

## Resume

Implementation source `ba71678da53dbe9e1d1354b4a8dcb43a61f0b3f2`, branch `codex/gdj-22-scenario-config`, managed worktree `C:/Users/Darian/.codex/worktrees/gdj-22-scenario-config/new-game-dev`. [Exact report, checks, source/archive manifest and reproduction](../../playtests/2026-10-04-scenario-configuration.md) preserve the candidate. 108 Edit Mode / 10 Play Mode, clean release, actual default/developer/120-per-second pair agreement/reset/loss, invalid CLI launches and retained regular/boss reporting smokes pass. All owned players/project Unity operations are stopped; accepted archives are untouched. Starting definitions and session binding are implemented; full architecture/progression/recovery/runner work is excluded.

Owner accepts the native scenario labels/settings for developer testing after the rubric and prototype scope were clarified: “Yes, for testing they are fine. Since they're just for developers AKA myself.” The visibility/expectation gate is complete for developer-facing controls; final product UI/balance/enjoyment remain separate. [Recorded acceptance](../../playtests/2026-10-04-scenario-configuration.md#owner-acceptance-for-developer-testing) preserves the scope and earlier clarification. Draft [PR #13](https://github.com/OpenCnid/game-dev-journey/pull/13) remains open and attached. Next: coordinator code/integration review. No merge authorization; do not merge automatically. High-rate stale-enemy rejection and late terminal-log asymmetry remain documented. Runtime/build/checks are unchanged; this acceptance update needs documentation checks only.
