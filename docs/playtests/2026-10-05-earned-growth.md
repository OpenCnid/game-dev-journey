# First earned growth: owner feedback and revised candidate

GDJ-23 implements one ordinary earned level-up in the fixed solo/two-player rifle fixture. It tests perceptible power growth, distinct personal choices and readable combat. Base main is `bd2bd1438b1029426e2c0234ff640502dc0e25ea`; the isolated branch is `codex/gdj-23-earned-growth`. Unity 6000.3.25f1 and Transport 2.7.4 are unchanged. Main integration is pending.

## Human result: revise

The owner first clicked Host A in the solo comparison and could not leave Waiting. The v5 navigation correction adds Back to menu, closes session resources/ports, preserves existing menu settings and starts fresh solo/pair sessions. Its clean runtime is `654d5298d91f49ca2c5caf7729214feabf8bd534`; [the v5 manifest](evidence/gdj-23-earned-growth/release-manifest-v5.json) preserves all 167 player files (97,886,694 bytes).

The owner's following native solo attempt completed 60 active seconds with zero catch-up limits, 73 kills, 64 collected XP and nine uncollected orbs. Level 2 was offered at tick 1734 (**28.9 seconds**); Faster fire applied at tick 2888 (**48.133 seconds**). The owner reported that the increased rate was noticeable and worked. They initially did not identify the blue pickups as XP, did not notice leveling up, discovered the lower-right cards late and disliked choosing while combat continued and Tab hiding the cards. **Stronger hits was not tested.** Another attempt ended in defeat at 12.5 seconds with eight XP and no offer.

Evidence is preserved under `prototypes/001-stacked-weapons/artifacts/gdj23-owner-navigation-v5-20261005-001858/session-13ad00216ded4ed6960b10c81da7ae45`; the launch folder records the native invocation and exact source. The prior blocked-host screenshot/logs remain in `artifacts/gdj23-owner-solo-20261005-001126`. These are owner observations, not scripted opinions.

This human attempt missed the contract's 10–25-second earning target and left only about twelve seconds after selection. Earlier scripted perfect-aim attempts earned at about eight seconds, also outside that target. Neither establishes intended pacing for ordinary players. The first revision addresses missed XP/offer feedback and safe choice access; keep threshold 10 unchanged for the next observation. If earning still runs late after pickups are understood, test one smaller threshold as a separate declared tuning change.

## Owner-selected revision

[Decision 0013](../decisions/0013-readable-xp-and-safe-first-level-up.md) supersedes revision 1's live, hideable corner cards and stationary single-value carriers. The fixture remains one rifle, one earned level, 35 respawning 60-HP drones, seed 137, 100 player HP and 60 active seconds. Baseline is 20 body/40 head damage at .12 seconds and 65-metre range; choices remain Stronger hits 30/60 at .12 or Faster fire 20/40 at .08.

- Level 2 enters an authority-owned Choosing phase. Centered cards, a distinct level sound and an explicit selected-build confirmation give unlimited reading time. Click/1/2 selects, then Continue/Enter marks personal readiness. Both fixed pair members, including dead members, must explicitly Continue. Tab cannot hide the chooser.
- Physical ticks, shots, contact damage, return/immunity clocks and XP movement/collection freeze. Reliable networking, choices and readiness continue on a separate control revision. Resume clears paused wall-time accumulation; loss still aborts. Retry resets phase/revision/readiness as well as combat and progression.
- The HUD labels XP, displays 0–10 progress and the actual two-metre collection distance; a ground ring shows that radius. Local pickup and level-up cues are synthesized in code.
- The owner explicitly requested **both gravity-like pulling and merging**. Within 1.5 metres, carriers attract at 1.5 m/s toward a higher-value or older carrier; within .3 metres they merge. Death still creates one XP; collection awards the carrier's full value. Merging awards nothing and conserves value. Death/incarnation deduplication and original event provenance remain.
- Flat camera-facing instanced discs replace per-orb objects. Blue means 1 XP, larger teal 2–4 and gold 5+. Three material groups submit batches of up to 250, with reusable matrix buffers and one range-ring object.

These are first-fixture rules, not the full banked/planned/invoked Zone. Additional levels, loadouts/respec, authored varied enemy drops, magnets toward players, loot/draw probabilities, shared rares and recovery retain separate scope. Carrier value, control phase and distinct choice/Retry readiness leave those consumers explicit without implementing a universal modifier system.

## Verification and boundaries

Serialized `tools/unity.ps1` checks pass **131 Edit Mode / 15 Play Mode**. Coverage includes conserved death/create/merge/collect/reset value, duplicate/stale/forged mutations, expected damage thresholds/cadence, independent alive/dead offers, frozen physical/return clocks, same-tick control replication, all-roster Continue, pause/resume without catch-up, actual loopback pair configuration/builds and menu/port/resource lifetime.

A diagnostic 20,000-XP same-cell setup merges to one carrier without losing value and with bounded candidate visits. A diagnostic 1,004-carrier render checks fixed GameObject ownership and an exact two-metre ring. **These are rule/resource tests, not native gameplay FPS measurements.** Spatial attraction uses a reusable local grid; candidate scans and reliable movement traffic can still grow with dense moving carriers. No supported thousands-of-pickups performance envelope follows.

Protocol 4 intentionally rejects older players. The current 35-drone snapshot is **985 bytes** and each growth event **101 bytes**, including message type, within the existing 1100-byte limit. The ledger is dynamically retained rather than capped to a packet. Ordered events carry create/move/merge/collect and per-offer commits; movement samples at 10 Hz. This bounded local fixture does not validate four players, LAN/internet, reconnect or dense replication.

Earlier v3 checks preserve two ordinary 60-second solo and pair runs with contrasting/swapped choices, retained growth/death/return, wipe/Retry, stale/duplicate rejection, hard loss and reference controls. Those results apply to their recorded source, not the revised candidate. Earlier native startup captures revealed a prepared-render stall; v4 fixed startup ordering, and native v5 completed with zero catch-up limits. Native input/visible timing for the current revision still needs owner testing.

## Candidate and reproduction

The first revised v6 clean build, source `1dc2465fe784c098bcadfc8d4c298ead48a529f2`, is preserved with [its manifest](evidence/gdj-23-earned-growth/release-manifest-v6.json). Its standalone checker caught an unconfigured scripted-player bug: after an explicit card command, scripted movement automatically marked Continue. The physical freeze held before selection, but the checker rejected the implicit resume. Restrict automatic Continue to a companion explicitly configured with `-growthChoice`; the Play Mode regression now also exercises scripted movement without that configuration. All 15 Play Mode tests pass after the correction. The failed run/desktop lifetime remain in `artifacts/gdj23-v6-solo` (input desktop Default before/after, owned player stopped). v6 is not the delivery candidate.

The corrected v7 build, complete archive, background standalone results and draft PR are being finalized. The next checkpoint will record their exact source and paths before delivery.

All automated standalone interaction uses the existing owned Win32 background desktop and game-owned command/state bridge. It never switches the input desktop. The camera bridge changes rendering/timing and omits the native HUD, so its checks establish functionality only. `-screenshotAtLevelUp -screenshot <absolute-path>` requests a game-owned native chooser capture for visual inspection; it does not establish native input or performance.

Reproduce the functional checker with the bundled Python (or a Windows Python installation):

```powershell
python docs/playtests/evidence/gdj-23-earned-growth/check_growth.py --player <absolute-v7-StackLab.exe> --results <new-absolute-results-folder> --solo
python docs/playtests/evidence/gdj-23-earned-growth/check_growth.py --player <absolute-v7-StackLab.exe> --results <another-new-results-folder> --port 17823
```

It earns XP through ordinary diagnostic movement/firing, holds the choice for wall time, checks frozen state and independent selections, tests invalid/duplicate Continue, contrasting/swapped builds, event/value parity and Retry/stale rejection. Pair loss is tested during the choice freeze. It retains commands, states, growth/combat traces, summaries, invocation and desktop lifetime. No forced XP, hidden immunity or synthetic enjoyment score.

## Remaining owner comparison

Launch v7, choose Earned growth for solo and Start. Watch whether XP/range/audio are immediately clear. At level-up, leave the centered chooser open for several seconds; confirm combat/time stop, read both cards, choose Stronger hits first and explicitly Continue. Assess whether fewer shots to kill is noticeable before reading stats and whether the interruption helps. Retry and compare Faster fire, then report XP pickup sound comfort, attraction/merging clarity and any missed/blocked controls. Native co-op contrast/swapped choices and second-human impressions remain unobserved.

Required remaining gates are owner readability/choice/sound observations, Stronger hits comparison, native co-op flow and representative visible release timing with equivalent workload counters, then coordinator handoff review. Automated passes or a draft PR do not mark GDJ-23 Done. The owner-controlled v5 window is left alone while the revised build is prepared.
