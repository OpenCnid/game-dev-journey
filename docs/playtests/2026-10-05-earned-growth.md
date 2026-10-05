# First earned growth: owner feedback and revised candidate

GDJ-23 implements one ordinary earned level-up in the fixed solo/two-player rifle fixture. It tests perceptible power growth, distinct personal choices and readable combat. Base main is `bd2bd1438b1029426e2c0234ff640502dc0e25ea`; the isolated branch is `codex/gdj-23-earned-growth`. Unity 6000.3.25f1 and Transport 2.7.4 are unchanged. Main integration is pending.

[PR #14](https://github.com/OpenCnid/game-dev-journey/pull/14) is merged as an included dependency of owner-authorized PR 15; see the integration record below. GDJ-23's remaining human/native observations stay distinct from this integration.

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

**Delivery candidate v7:** clean release source `f7084023ae6ff3988efe384421c8a809a1aade8b`, built `2026-10-05T05:58:30.5060442Z`. The whole player is archived at `C:\Users\Darian\.codex\worktrees\gdj-23-earned-growth\new-game-dev\prototypes\001-stacked-weapons\build\StackLab-GDJ23-v7`; [manifest v7](evidence/gdj-23-earned-growth/release-manifest-v7.json) records 167 files / 97,896,070 bytes. v2/v5/v6/v7 archives rehash against every recorded SHA-256; prior archives are preserved.

The exact v7 ordinary standalone checks pass in `artifacts/gdj23-v7-solo` and `artifacts/gdj23-v7-pair`. [Compact verification](evidence/gdj-23-earned-growth/verification-v7.json) retains source, counters, desktop lifetime and raw-trace locations. All four comparisons reach tick 3600 / 60 active seconds, level 2 and zero catch-up limits. Host/client full terminal player/enemy state, ordered growth events and nonterminal combat events agree exactly.

| Mode/run | Choices | Level at active seconds | Kills | Collected XP | Uncollected XP | Merge events |
| --- | --- | --- | --- | --- | --- | --- |
| Solo 1 | Stronger hits | 8.133 | 238 | 216 | 22 | 24 |
| Solo 2 | Faster fire | 8.300 | 239 | 217 | 22 | 29 |
| Pair 1 | A stronger / B faster | 7.250 | 566 | 438 | 128 | 191 |
| Pair 2 | A faster / B stronger | 8.267 | 590 | 428 | 162 | 202 |

In every run, collected plus uncollected value equals unique deaths. Merged carriers reduce count without deleting XP; pair endings retain 89/97 carriers holding 128/162 XP. The checker holds the automatic pause for at least two wall seconds, applies choices at the same frozen physical tick, rejects duplicate choice and invalid-phase readiness, holds pair combat for another second after only A Continues, then resumes after B. Both-ready Retry resets XP, offers, builds, phase/revision/readiness and rejects old-run choice/Continue. Client termination during run 3's Choosing phase aborts the host on the control clock. Both owned desktops record input `Default` before/after and all owned children stopped; the owner's native v5 PID 22212 is untouched.

This still misses the 10–25-second earning target under scripted perfect aim. Keep the human 28.9-second result separate; no hidden XP/tuning claim. Current native owner observations are pending.

All automated standalone interaction uses the existing owned Win32 background desktop and game-owned command/state bridge. It never switches the input desktop. The camera bridge changes rendering/timing and omits the native HUD, so its checks establish functionality only. A v6 game-owned chooser capture was inspected: both centered cards and before/after text fit at 1280×720. Current v7 screenshot requests with the bridge failed because its active 960×540 render target is smaller than the requested screen region; this is a capture limitation, not a passed native HUD check. Raw player logs retain the failures. `-screenshotAtLevelUp -screenshot <absolute-path>` can request a native chooser capture without that bridge; native input/readability/timing still need owner verification. The bridge capture repair belongs with GDJ-21, not a new gameplay/performance claim.

Reproduce the functional checker with the bundled Python (or a Windows Python installation):

```powershell
python docs/playtests/evidence/gdj-23-earned-growth/check_growth.py --player <absolute-v7-StackLab.exe> --results <new-absolute-results-folder> --solo
python docs/playtests/evidence/gdj-23-earned-growth/check_growth.py --player <absolute-v7-StackLab.exe> --results <another-new-results-folder> --port 17823
```

It earns XP through ordinary diagnostic movement/firing, holds the choice for wall time, checks frozen state and independent selections, tests invalid/duplicate Continue, contrasting/swapped builds, event/value parity and Retry/stale rejection. Pair loss is tested during the choice freeze. It retains commands, states, growth/combat traces, summaries, invocation and desktop lifetime. No forced XP, hidden immunity or synthetic enjoyment score.

## Remaining owner comparison

Launch v7, choose Earned growth for solo and Start. Watch whether XP/range/audio are immediately clear. At level-up, leave the centered chooser open for several seconds; confirm combat/time stop, read both cards, choose Stronger hits first and explicitly Continue. Assess whether fewer shots to kill is noticeable before reading stats and whether the interruption helps. Retry and compare Faster fire, then report XP pickup sound comfort, attraction/merging clarity and any missed/blocked controls. Native co-op contrast/swapped choices and second-human impressions remain unobserved.

Required remaining gates are owner readability/choice/sound observations, Stronger hits comparison, native co-op flow and representative visible release timing with equivalent workload counters, then coordinator handoff review. Automated passes or a draft PR do not mark GDJ-23 Done. The owner-controlled v5 window is left alone while the revised build is prepared.

## Owner v7 smoke and minimal choice text, October 5

The owner played the exact v7 archive and reports **“Everything else seemed fine.”** Their screenshot shows the centered paused chooser. This satisfies the selected quick human blocking check before GDJ-3; it is not a detailed two-build feel comparison, native co-op acceptance or performance result. The owner asks that cards show only the change, without previous or unchanged values, because reading time matters. Remove the unnecessary ownership sentence; keep the short click/1/2 instruction.

Native invocation and raw logs remain in `prototypes/001-stacked-weapons/artifacts/gdj23-owner-v7-20261005-012902`; [compact owner evidence](evidence/gdj-23-earned-growth/owner-smoke-v7.json) preserves observations and counters. Run 1 records level 2 at tick 1010 (16.833 active seconds), Stronger hits selected/applied at that frozen tick, explicit Continue, 30 kills and 19 collected XP before solo defeat. Run 2 starts with baseline stats/zero XP before menu exit. These logs establish selection/reset behavior, not whether the owner felt the damage increase. The blue ground ring is the actual inclusive **two-metre collection radius**; the separate 1.5-metre attraction radius governs carrier-to-carrier clumping.

On `codex/gdj-3-weapon-stack`, pending the next playable build, the two cards and selected confirmation now show only **+50% fire rate** or **+50% damage**. This is a text-only correction; gameplay tuning and the frozen v7 archive/manifest remain unchanged. Diff/static accuracy checks suffice here; no new copy-only tests or presentation-only build. GDJ-3's next candidate will carry this correction. Native co-op, detailed sound/feel observations and representative dense/performance evidence remain separate open gates. Further presentation work is parked under the owner's MVP priority.

## Owner follow-up and delivery correction, October 5

The owner later calls the test “horrible”: cards still contain too much information, progression cannot pass level 2, and only Stronger hits/Faster fire are available. Their screenshot (`codex-clipboard-c57c791f-a6fc-4ed7-bd95-2fc3ac8151cc.png`) shows v7's original before/after copy and ownership sentence. Read-only process inspection confirms the only running player before correction is the frozen GDJ-23 v7, PID 31288. Its level-2 cap/two rifle choices are that fixture's intended scope; this observation is not a failed native test of the GDJ-3 weapon-stack candidate. The delivery was confusing: the old window remained open, while the newer candidate had only been offered through a launch action.

The owner observes gravity working but cannot clearly understand its action and suggests an effect showing XP gravity clusters. Record this as remaining human readability feedback; successful mechanics do not establish readable attraction/merging. No further XP effect implementation is selected here under the owner's MVP priority.

Corrected delivery opens exact clean GDJ-3 v3 (`d8c3eee6ad6b4b8109c8d63c524512efb9a918b3`) visibly at the already-selected Weapon stack menu, native input/audio and saved logs, without scripted movement, preview, automatic start or XP grants. New process PID 34308; raw invocation/log folder `prototypes/001-stacked-weapons/artifacts/gdj3-owner-v3-20261005-080944Z`. v7 remains under the owner's control. v3 contains concise cards and earned levels 2/3 for adding scatter/piercing and either a third slot or enhancement. Human stack observations remain pending; [the stack report](2026-10-05-weapon-stack.md) preserves its exact candidate and limitations.

## Owner-authorized integration through PR 15, October 5

The owner authorizes PR 15 into main. Its exact tested candidate includes the PR 14 earned-growth foundation; merge `92d72f620b23b22b0e9eb34e62942225232e6f84` preserves that ancestry, and GitHub marks both PRs merged. [The stack integration record](2026-10-05-weapon-stack.md#owner-authorized-integration-october-5) contains tree/source equality, reused checks and the next-session handoff. The v7 archive remains frozen. Native co-op feel/timing and complete XP-gravity readability remain unverified; integration does not waive or invent those observations.
