# Larger simultaneous arsenals and cooperative crowds — October 5

[GDJ-24](https://linear.app/cnid/issue/GDJ-24/measure-larger-arsenals-and-enemy-crowds-in-two-player-co-op) investigates the integrated operating envelope, not the known isolated value of three weapon archetypes. [The frozen pre-implementation contract](../planning/tasks/GDJ-24.md#frozen-experiment-contract--october-5-before-runtime-edits) owns the matrix, gates, counters and ownership seams. This report is an ongoing evidence record; no dense performance or human acceptance is established yet.

## Baseline reproduction and preserved failures

Managed checkout `C:/Users/Darian/.codex/worktrees/gdj-24-coop-scale/new-game-dev`, branch `codex/gdj-24-coop-scale`, from brief `9663ffc03ff504647e7d180a39e54c098c761a9e` / main ancestor `e8acedb107905ade056241e978dace6bdaa0bb92`. Contract committed before runtime edits at `2c8bbdd`. The entire v3 control at clean runtime source `d8c3eee6ad6b4b8109c8d63c524512efb9a918b3` rehashes: 167 files / 97,909,030 bytes; [read-only audit and earlier failure hashes](evidence/gdj-24-coop-scale/control-audit.json).

The earlier v2 aborts remain unresolved. They stop at ticks 152 and 3266; their Running-only traces end at 144 and 3258, omitting the aborting frame. Maximum recorded intervals of 21.3322/27.3107 ms therefore do not explain those aborts. Both used the preview bridge. No exceptions or adequate abort timing establish a cause. Successful new native controls cannot be called a fix.

New cold v3 controls use visible native 1920×1080, uncapped/VSync off, D3D11, three frozen weapons per player, 35 respawning 60-HP drones, seed 137, ordinary health, 60 active seconds, no XP, scripted movement/aim and game-owned mute. Preview is disabled. They are **exploratory reproduction, not standard 30+60 acceptance**. Owner v7 PID 31288 still competes and remains under owner control; permission to close it is pending. Pair windows overlap and are inspected separately, which does not prove continuous unobstructed pair visibility. Scripted play provides no human opinion.

| Capture/role | Ending / ticks | All Running-frame p99 | Maximum | >6.944 / >13.889 ms | Catch-up |
| --- | --- | ---: | ---: | ---: | ---: |
| Solo | Complete / 3600 | 1.1483 ms | 11.3147 ms | 2 / 0 | 0 |
| Pair with auto-quit, host | Complete / 3600 | 1.7647 ms | 39.4119 ms | 7 / 5 | 0 |
| Pair with auto-quit, client | **Aborted** / 3600 | 1.8123 ms | 20.7778 ms | 8 / 6 | 0 |
| Pair retained through reporting, host | Complete / 3600 | 1.7299 ms | 18.0907 ms | 2 / 1 | 0 |
| Pair retained through reporting, client | Complete / 3600 | 1.8052 ms | 16.7200 ms | 1 / 1 | 0 |

The auto-quit pair **fails terminal agreement**: the host's reliable `Peer player closed` message reaches the client after completion but before its Save in the same Poll. Preserve [failed pair audit/raw traces](evidence/gdj-24-coop-scale/baseline-pair-autoquit-failure/audit.json). Repeating with both players held until both reports exist gives [complete matching terminal state, players, enemies and weapon counters](evidence/gdj-24-coop-scale/baseline-pair-held/audit.json), zero application backlog/high-water/deferred sends and no catch-up. [Solo audit/raw traces](evidence/gdj-24-coop-scale/baseline-solo/audit.json) also complete. Capture procedure now keeps both owned processes through the reporting barrier; no gameplay or gate was relaxed. Close only those owned processes afterward. Earlier owner processes/archives are preserved.

Raw traces and terminal summaries are compressed alongside each audit. `audit_samples.py` recomputes nearest-rank distributions from all Running rows, including the first three seconds omitted by legacy report percentiles. Legacy RTT values repeat in every frame and are correlated; do not interpret that row count as independent ping samples. Source, invocation and original logs remain in the named artifact directories.

## Bounded workload implementation checkpoint

Schema 3 / protocol 6 adds the explicitly diagnostic `co-op-scale` scenario. Ordinary fixtures retain their 35-enemy bound, content/tuning, earned choices, physical freeze, Continue and lifecycle. The diagnostic keeps 60-HP drones, current ranges/damage/cadences, fixed roster of one or two, frozen 1/3/8 instances and invulnerability with contact work retained. XP death/collection/gravity/merging/rendering use the ordinary conserved ledger, with offers omitted for continuous measurement. This does not establish ordinary high-density survival or balance.

Instance identities remain interleaved by owner; IDs 1–6 retain the existing slots, IDs 7–16 represent diagnostic repeats. Type derives from the repeated rifle/scatter/pierce sequence while cooldown and attribution use instance identity. Independent rifle repeats have separate schedules. Tracer storage permits every diagnostic emission's intended queries/cues; no gameplay query is skipped to stay inside the old eight-line pool.

Full reliable snapshots remain. Larger payloads use ordered chunks below the existing 1,100-byte cap, one 32,768-byte assembly, strict total/offset/sequence validation and atomic parse/adoption after completion. An incomplete assembly times out explicitly. The outgoing FIFO remains 1,024 records and aborts rather than dropping outcomes. Chunking permits the experiment; it is not scalable replication. Record payload messages/bytes, pending bytes/records/age/high-water and serialization time. Transport wire overhead/retransmissions remain outside those payload counters.

Release frame traces now include authority/steering/weapon/XP stage times, actual steering/contact/attraction tests, per-instance work, presentation query/probe counts, renderer instances/batches, XP activity/value/carriers, networking and main-thread allocation/GC counters. Logging/serialization cost is reported; stage parents overlap children. Presentation counters include the regular head/body focus query and emission queries, not every arithmetic operation. Absolute allocation counters are process-thread totals; use capture deltas. These measurements do not supply GPU time or physical input/display latency.

First import failed because a new metadata GUID was invalid; corrected before tests ran. Failed log stays at `artifacts/gdj24-initial-import-failure.log`. Following correction, **148 Edit Mode / 18 Play Mode pass**, including existing progression/lifecycle/precision checks and new diagnostic bounds, eight owned schedules, XP conservation/contacts, malformed/atomic chunk checks and real reliable loopback delivery beyond the transport window. The additive logging-cost field compiles in the clean release. Pairwise steering is still the measured reference.

## Required measurements and remaining work

| Rung | Enemies / instances each | Solo | Actual pair |
| --- | --- | --- | --- |
| S1 | 35 / 1 | Complete; p99 1.445 ms | Complete; host/client p99 2.356 / 2.146 ms |
| S2 | 35 / 3 | Complete; 1.475 ms | Complete; 1.965 / 2.007 ms |
| S3 | 35 / 8 | Complete; 1.470 ms | Complete; 2.141 / 1.959 ms |
| S4 | 250 / 3 | Complete; 3.439 ms | Complete; 4.572 / 3.038 ms |
| S5 | 1000 / 3 | **Catch-up abort / tick 33** | **Catch-up abort / tick 24** |
| S6 | 250 / 8 | Complete; 3.709 ms | Complete; 5.913 / 2.910 ms |
| S7 | 1000 / 8 | **Catch-up abort / tick 24** | **Catch-up abort / tick 23; terminal reject counters differ** |

These are one 10+30 **contended exploratory screen** per rung, with complete intended workload on the successful cases. They do not establish acceptance or a supported peak. [Compact matrix index](evidence/gdj-24-coop-scale/matrix-v1.json) links every full audit and compressed raw frame/events/growth/source/settings/visibility record. All completed solo/pair cases conserve XP and reconcile per-instance triggers/rays/probes with per-player damage/kills. Every completed pair has matching nonterminal combat and XP event streams, terminal players/enemies/progression and weapon counters, empty application queues, no queue-age violation and no catch-up. Pair S4 also contains a window-placement UI attempt and remains exploratory. Frame tails/cold data remain in each audit; p99 alone does not erase misses.

Release v1 is clean source **`4a6b5b024be285d00c2de190b2ab451313888456`**, built **2026-10-05T09:08:54.2308920Z** with Unity 6000.3.25f1 / URP 17.3.0 / Transport 2.7.4, Windows x64 Mono / D3D11 release. Whole archive `build/StackLab-GDJ24-v1` has 167 files / 97,921,579 bytes; its ZIP is 36,056,200 bytes, SHA-256 `730bd571125187047efb895b554a9d735d8de21c1c530777b6d44d300bd9ab48`. [Whole-file manifest](evidence/gdj-24-coop-scale/release-manifest-v1.json), [hardware/settings](evidence/gdj-24-coop-scale/hardware-v1.json). Windows 11 build 26200, RTX 3090 driver 32.0.15.9186, 3840×1600 / reported 143 Hz desktop at 150% scale, Balanced power plan, 8-core/16-thread i7-10700K and approximately 64 GB. Actual sustained clocks, cooling and BIOS settings are unknown.

[Ordinary pair regression](evidence/gdj-24-coop-scale/ordinary-pair-v1.json) passes two runs on the owned background desktop: physically frozen independent choices, explicit Continue, XP conservation and identical event attribution, complete reset/stale rejection and client loss while Choosing. This is functional evidence, not native performance or human observation; death/return rules are additionally covered in the existing passing suite.

The measured first limit is pairwise steering. In solo S5, crowd work costs **690.5542 ms across 33 ticks** (about **20.93 ms per 16.67 ms tick**), against 703.9258 ms total simulation and 33,000,000 separation candidates. The abort row retains 154.4307 ms frame interval, 29.9724 ms remaining accumulator and the preceding 153.5185 ms Update. The other upper references similarly abort before warm-up. This clear overload does not explain or fix the unrelated old v2 stalls.

Pair S7's overload also leaves 440 host application records at its saved abort barrier. Nonterminal combat and XP hashes and physical state agree, but the client receives later `Terminal` rejection records after the host's first report; only rejection counters differ. This remains a failed capture with pending work and mismatching terminal counters, not evidence of a supported rung or permission to discard records.

The release Mono thread-allocation API returns zero throughout despite known frame/event string allocations. Allocation rate is therefore **unavailable**; GC counts and terminal managed/process memory are retained but are not a precise allocation profile. Add low-frequency heap/process-memory samples for endurance. The one justified improvement is an optional exact spatial neighbor query over the same positions, all neighbors and stable accumulation order, with pairwise reference retained. Fixing queries, replication or presentation remains a separate decision after its next measurements.

## Exact-grid implementation and parity

The optional `-sharedSteering grid` host path builds 2-metre cells from every pre-move enemy position after pending shots/respawns, gathers all candidates from adjacent cells, sorts IDs and applies the unchanged separation expression in reference order. There is no neighbor cap or approximate rule. Default `pairwise` remains selectable. Grid build/gather/sort cost is inside the crowd stage. Both paths keep contact/precision/XP/presentation and full reliable snapshots. A dense-cell test deliberately retains all 250×250 candidate checks; broad phase acceleration has no reduced-work promise in a coincident crowd.

**152 Edit Mode / 18 Play Mode pass**. New fixed-tick fixtures compare byte-identical full snapshots, serialized combat outcomes and XP event streams across two Retry generations at 35/1, 250/3 and 1000/8. They include actual death/respawn/pickup work and identical contact/attraction counters; reduced candidate checks occur only where they cannot contribute. Cell/radius edges, coincident positions and the complete dense case also match. These are semantic parity tests, not native performance or human evidence. One-second managed-heap/process-memory samples and their measured overhead supplement the unavailable Mono allocation counter.

Next: build the clean candidate, screen S2/S5/S6/S7 in both modes to test the changed crowd cost and combined upper limit, then selected comparisons and five-minute S7 pair attempt. The reference matrix already covers every rung; this bounded rescreen avoids repeating unaffected arsenal-only screens. Standard 30+60×3 acceptance needs a quiet desktop and declared complete rendering; permission for the old owner player remains pending. A verified human candidate and owner co-op/readability observations, intermittent-stall cause and GDJ-23's remaining human gates stay open. No LAN/internet, four-player, final boss/VFX or >10,000-enemy support is claimed. Supported-limit conclusion remains pending.
