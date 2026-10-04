# GDJ-15: rifle shots during fast turns

**October 4, 2026 — technical candidate; human comparison pending.** P1 selected this revision after accepting and merging [GDJ-16's overhead health](2026-10-03-overhead-health.md). It addresses the earlier lagging/multiple-line and projectile-like rifle report. It serves responsive aiming and crowded-combat readability; no new weapon traits or critical regions are introduced.

## Exact candidate and controls

- Source: `d93e867837915c9dae4a5b68947a7ecdfad703d3`, clean release Windows x64 Mono/D3D11 player built `2026-10-04T05:26:22.9890758Z`.
- Unity 6000.3.25f1 (`e1dba0a9aba4`), URP 17.3.0, Input System 1.20.0, Test Framework 1.6.0, Burst 1.8.30; package/editor settings unchanged.
- Seed 137, 35 drones, 60 seconds, 40 enemy HP / 20 rifle damage, 65 m range / 0.12 s interval. Burst simulation, culled separate meshes, cube projectile renderer, 1920×1080 windowed, 144 cap, VSync off.
- [Candidate metadata](evidence/gdj-15-shot-v1/candidate.json) and [whole-player manifest](evidence/gdj-15-shot-v1/build-manifest.json): 166 files / 96,914,690 bytes; manifest SHA256 `d2dd82da8258c6efe620ade58a5a98ad2fca450899a7d1d140b2df34e02b70b7`. Live output and complete archive hashes match.
- Working player: `C:/Users/Darian/.codex/worktrees/gdj-16-damage-feedback/new-game-dev/prototypes/001-stacked-weapons/build/StackLab/StackLab.exe`. Complete candidate archive: sibling `StackLab-GDJ15-v1`.
- Accepted GDJ-16 control: sibling `StackLab-GDJ16-v7`, source `0deb52d8dd7e2790b1651985f8a57642b37ce8d2`. All 166 archived files reverified before replacing the live output. Original GDJ-2 control remains in its baseline worktree, source `28ca1bce4d71d0af82326a93f65a528ebfd1cd5c`.

The candidate opens at the menu without `-playtest`; P1 starts with R. It includes an optional screenshot after three simulation seconds, which is not an automated play observation. Evidence/report commits after the build do not change its runtime source.

## Reproduction and correction

On merged main `076a98c`, an added [control regression](evidence/gdj-15-shot-v1/control-regression.cs.txt) moved one tick, turned 70° yaw / −20° pitch, fired and rendered with interpolation. [The failing result](evidence/gdj-15-shot-v1/control-regression.xml) recorded a **1.319243 m** gap from the line's origin to the displayed muzzle. The old world line remained enabled after a further turn. Exactly one LineRenderer was present. This reproduces stale geometry across turning frames; it does not independently establish the full perceived multiple-line symptom.

Normal combat now advances fixed ticks, prepares the displayed camera, then consumes due rifle shots against the same interpolated enemy positions. Simulation still selects the nearest hit, applies damage immediately and owns cadence. Catch-up shots resolve sequentially at one displayed pose; frames without simulation ticks cannot add shots. The camera/muzzle/endpoint agree on the firing frame. A combat tracer appears for that frame only, while the 45 ms white muzzle pulse and hit/death markers provide continuing feedback. Existing cues age before new shot events, so a long frame cannot erase a fresh shot.

Wound sparks anchor to the queried displayed incarnation; old-incarnation sparks are suppressed immediately on respawn. Health policy, overhead clearance, stable bar owners, wound shape and independent white/gold/player cues remain. Pause hides the transient tracer and freezes remaining cues; R clears them. The benchmark retains its original per-tick firing, fixed-state hit query and legacy 45 ms tracer. No preset or performance result is promoted.

The [timing contract](../planning/tasks/GDJ-15.md#investigation-and-timing-contract--october-4) was recorded before runtime edits. Current hit detection is still an approximate sphere; this revision aligns its temporal pose, not precise healthy/wounded critical regions. Low-frame-rate catch-up batches use one displayed pose rather than reconstructing historical input. Future weapons/networking must retain independent shot events and define their own timing; no generalized rack or rewind was added.

## Useful technical checks and their limits

The serialized pinned-editor checks passed **54 Edit Mode and 5 Play Mode cases**. Of these, **10 new cases** cover this outcome; the other 49 retain existing damage/simulation/render coverage.

- Three simulation backends query a displayed silhouette-edge hit that the later fixed pose misses; damage and fresh spark positions agree with the resolved surface. A kill cannot put an old wound spark on its respawn.
- Five frame-rate cases (8/30/60/144/240 FPS) compare batched due-shot consumption with the original per-tick cadence over ten simulated seconds: identical movement and 84 shots, including catch-up frames.
- Two runtime cases check sharp yaw/pitch plus movement, current recoiled muzzle origin, centered miss/hit endpoints, one line, next-frame removal, fresh cues after a 200 ms frame, pause/reset and legacy benchmark lifetime. The corrected reproduction records **0.000000 m** muzzle error, viewport endpoint **(0.5, 0.5)** and `retainedAfterTurn=False`.

Exact [Edit Mode](evidence/gdj-15-shot-v1/EditMode.xml) / [Play Mode](evidence/gdj-15-shot-v1/PlayMode.xml) results are saved. The release build succeeded. A hidden [startup smoke invocation](evidence/gdj-15-shot-v1/startup-invocation.json) / [raw report](evidence/gdj-15-shot-v1/startup-smoke.json) verified embedded clean source, Burst/culled backends and reporting: 35 regular drones, one-second warm-up/five-second capture, 300 capture ticks, 42 shots, no dropped projectiles or catch-up-limit frames. Its frame timings are **not performance evidence**. Candidate menu startup is checked separately.

These checks establish timing, geometry, lifecycle and functional startup. They do not establish subjective immediacy, streak visibility at high FPS, enjoyment, actual human run metrics or a performance improvement. These are local Unity checks; no GitHub CI result is claimed.

## Human comparison still needed

P1 should press R, sweep sharply while strafing, then track a moving drone. Compare against the accepted v7 control if useful. Record whether old/extra lines remain, whether damage feels immediate under the crosshair and whether the single-frame tracer is too faint. Confirm accepted wounded HP and white/gold cues remain readable. GDJ-15 stays in review until this comparison supports acceptance; no merge is authorized for this candidate yet.
