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

First import failed because a new metadata GUID was invalid; corrected before tests ran. Failed log stays at `artifacts/gdj24-initial-import-failure.log`. Following correction, **148 Edit Mode / 18 Play Mode pass**, including existing progression/lifecycle/precision checks and new diagnostic bounds, eight owned schedules, XP conservation/contacts, malformed/atomic chunk checks and real reliable loopback delivery beyond the transport window. A small additive logging-cost field follows those checks and will compile in the release build. No optimization has yet been selected; pairwise steering is still the reference.

## Required measurements and remaining work

| Rung | Enemies / instances each | Solo | Actual pair |
| --- | --- | --- | --- |
| S1 | 35 / 1 | Not measured | Not measured |
| S2 | 35 / 3 | Not measured | Not measured |
| S3 | 35 / 8 | Not measured | Not measured |
| S4 | 250 / 3 | Not measured | Not measured |
| S5 | 1000 / 3 | Not measured | Not measured |
| S6 | 250 / 8 | Not measured | Not measured |
| S7 | 1000 / 8 | Not measured | Not measured |

Next: clean instrumented release and whole manifest, ordinary fixture regression, then sequential 10+30 exploratory screens to locate the first limiting cost. Preserve an equivalent-work reference before one justified improvement. Standard selected 30+60×3 acceptance, cold evidence, a separate five-minute S7 pair attempt and a verified human candidate remain required. Native acceptance needs a quiet desktop and declared complete rendering. Owner co-op/readability observations, intermittent-stall cause and GDJ-23's remaining human gates stay open. No LAN/internet, four-player, final boss/VFX or >10,000-enemy support is claimed. Conclusion remains pending.
