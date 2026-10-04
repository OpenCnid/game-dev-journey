# GDJ-19: rifle rate testing controls

**October 4, 2026 — developer-tested and accepted.** P1 accepted [GDJ-15's aiming fix](2026-10-04-shot-alignment.md) as smooth and undelayed, then requested an option to test slow early-game-like shooting and continuous mid/late-game-like shooting. This adds a local testing configuration, not progression balance or upgrades.

## Behavior

The start/pause/end menu exposes **Rifle rate (shots/sec)** with Apply and four presets: **Slow 2/s, Baseline 8.33/s, Rapid 30/s, Stress 120/s**. Custom rates from **0.5–120/s** are supported. The subtitle and menu show the active rate. It applies to combat and subsequent benchmark repeats; R resets the encounter/cooldown while keeping the rate for this application session. Changing rates preserves fractional cooldown progress; invalid input leaves the previous setting intact. The menu scrolls when needed.

Simulation time owns cadence and damage. Benchmark ticks now consume all due shots, permitting rates above 60/s. Combat retains GDJ-15's displayed-pose hits and one-frame tracer; multiple shots in a frame share its cosmetic line, and impact pools remain bounded. Benchmark retains its legacy 45 ms tracer. Damage, movement, range, enemy health, health-bar policy and package/render settings are unchanged. No cross-session preference persistence is implemented.

Player `-rifleRate 30` and helper `-RifleRate 30` select the same setting. Additive v5 JSON fields `rifleShotsPerSecond` / `rifleIntervalSeconds`, invocation metadata and summary output identify the workload; the helper rejects mismatches. Historical v5 players with no rate fields remain allowed only at the known baseline. Compare equal rates/settings and live workload counters before drawing performance conclusions.

## Exact candidate and preserved controls

- Clean runtime/build source **`bb7a0c6dae1906b2e8baf7ef7656eca9b0f1e841`**, built **`2026-10-04T05:57:26.7313990Z`**. Release Windows x64 Mono/D3D11, Unity 6000.3.25f1; pinned packages unchanged.
- [Candidate metadata](evidence/gdj-19-rate-v2/candidate.json) / [whole-player manifest](evidence/gdj-19-rate-v2/build-manifest.json): **166 files / 96,916,770 bytes**, manifest SHA256 `637bc99257e402c0925be7261959f311c0212361fe3380532d17db59825609ad`. Live output and full archive hashes match.
- Working player: `C:/Users/Darian/.codex/worktrees/gdj-16-damage-feedback/new-game-dev/prototypes/001-stacked-weapons/build/StackLab/StackLab.exe`. Complete archive: sibling **`StackLab-GDJ19-v2`**. Default combat: seed 137, 35 drones / 60 s, Burst/culled, separate meshes, 1920×1080 windowed, cap 144 / VSync off.
- Accepted aiming control **`StackLab-GDJ15-v1`**, source `d93e867`, was reverified before replacing live output. GDJ-16 v7 and the original GDJ-2 control remain preserved. The first internal rate build **`StackLab-GDJ19-v1`**, source `c1bb5bf`, also remains preserved with [its evidence](evidence/gdj-19-rate-v1/candidate.json).

That first internal build exposed a decimal baseline-rate round trip landing on an adjacent float. Final v2 normalizes inputs expressing the baseline to its original **0.12f** interval and uses a double cooldown accumulator to avoid frame-batching roundoff at fast rates. This is numerical timing/configuration correction, not a new damage or balance choice. Report/evidence commits after the build do not change its runtime source.

## Checks and observed results

Serialized pinned-editor checks passed **65 Edit Mode / 6 Play Mode cases**, including **12 focused new cases** beyond the accepted aiming suite:

- Eight rate/frame combinations compare frame-batched combat shots with per-tick benchmark shots, including 0.5/2/8.33/30/120 rates and 8/30/60/144/240 FPS. Both paths produce equal due-shot counts; 120/s yields 1,201 shots over 600 ticks, including the initial due shot, rather than a 60/s cap.
- Cooldown progress, pause, retained rate on reset, invalid/nonfinite rates and exact baseline decimal input are checked. Existing default cadence remains 84 shots in ten seconds.
- A runtime test uses the same validation/application method as the menu: invalid entries preserve the previous rate, slow fire emits one shot in 0.3 s, high-rate combat exceeds 60/s, and restart keeps 120/s while clearing shots.

[Edit Mode XML](evidence/gdj-19-rate-v2/EditMode.xml), [Play Mode XML](evidence/gdj-19-rate-v2/PlayMode.xml) and the release build passed. Three hidden functional smokes used 35 regular drones, one-second warm-up and five-second capture:

| Requested rate | Reported nominal interval | Capture shots | Capture ticks | Raw report |
| --- | --- | --- | --- | --- |
| 2/s | 500 ms | 10 | 300 | [Slow](evidence/gdj-19-rate-v2/smoke-slow.json) |
| Baseline 8.33/s | original 120 ms | 42 | 300 | [Baseline](evidence/gdj-19-rate-v2/smoke-baseline.json) |
| 120/s | 8.33 ms | 600 | 300 | [Stress](evidence/gdj-19-rate-v2/smoke-stress.json) |

All three reports match clean source, requested rate/effective interval and backends, with zero catch-up-limit frames or dropped projectiles. Their [slow](evidence/gdj-19-rate-v2/invocation-slow.json), [baseline](evidence/gdj-19-rate-v2/invocation-baseline.json) and [stress](evidence/gdj-19-rate-v2/invocation-stress.json) invocations are saved. These shortened hidden runs validate functionality; their frame timings are **not performance evidence**. The old-player compatibility branch is retained by inspection, not a new historical-player run.

The [1920×1080 startup-menu capture](evidence/gdj-19-rate-v2/menu.png) was visually inspected: controls, preset labels and active rate are readable above the Start button, with the existing benchmark/FPS controls below. [Startup metadata](evidence/gdj-19-rate-v2/menu-startup.json) records a manual-start player and no Exception/ERROR/Crash matches. Its startup FPS display is not steady gameplay measurement. `-screenshotAtMenu` extends the existing screenshot hook for this UI verification. Benchmark PowerShell parsing and documentation links/whitespace are checked separately.

## Review and limits

Implementation lives on `feat/rifle-fire-rate-controls` in [PR #6](https://github.com/OpenCnid/game-dev-journey/pull/6), originally stacked on PR #5. The accepted aiming parent merged at `522640716d6627c7b9837f638436a921798301ad` before rate-control integration. Choose Slow or Stress, press R, then Escape/change/Resume to compare intensity without resetting the run. Presets are diagnostic samples. Future ammo/reload, weapon racks/upgrades and networking need their own per-weapon timing contracts. The [bounded brief](../planning/tasks/GDJ-19.md) preserves this scope.

## Human acceptance — October 4

P1 reports: “Okay we tested it and it looks good.” They explicitly request committing, making a PR and merging it. This accepts the presented v2 candidate; no detailed rate selections, run counts or performance measurements are supplied. The acceptance updates documentation and metadata only: runtime/build source and preserved whole-player hashes above remain unchanged. Recommend GDJ-17 next to align healthy/wounded critical hit regions and target coloring with the accepted aim, then compare precision at slow and continuous rates.
