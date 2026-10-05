# Earned simultaneous weapon-stack MVP — October 5

## Question and scope

The owner selects MVP proof over further presentation refinement: can one rifle grow into distinct simultaneous arsenals in solo and a fixed pair? [GDJ-3's task](../planning/tasks/GDJ-3.md) records the consumers, numerical fixture and larger-game limits before implementation. The v7 human blocking check is clear, with [effect-only card text requested](2026-10-05-earned-growth.md#owner-v7-smoke-and-minimal-choice-text-october-5). This branch carries that correction; the original v7 archive and PR 14 remain frozen.

## Implementation

`weapon-stack` lasts 120 active seconds with seed 137, 35 respawning 60-HP drones and 100 player HP. Rifle starts at 20/40 damage, .12 seconds, 65 metres. Ten collected shared XP offers each player Add scattergun or Add piercer. Twenty-five cumulative XP offers the missing secondary or enhancement of the first addition. Scattergun fires five horizontal fan rays, 12/24 damage, .75 seconds, 30 metres, ±8°; its enhancement adds two pellets. Piercer fires one 65-metre ray through up to three distinct enemies, 16/32 damage, .65 seconds; enhancement adds two targets. Every installed slot fires together on an independent host-owned schedule. Test settings are not final balance/content.

The chooser freezes physical combat at both stages; personal selection and all-roster Continue use control revisions. Banked excess XP cannot overwrite the first unspent offer. Dead members still choose and retain all slots on return. Retry clears offers, arsenal, enhancements and schedules. Original one-level growth and local crowd modes remain.

Compact build state stores installed slots, first addition and enhancement separately from the primary rifle settings. Explicit offer cards distinguish installing a compatible slot from enhancing its existing trait. Player/loot/draw consumers remain separate future extension points. Pose/build validation and stable six owned weapon IDs govern the fixed roster. Each scatter pellet and piercing target keeps pre-trigger enemy incarnation; a killed slot's fresh respawn cannot be damaged by an older ray. Piercing sorts unique body/head winners by distance. One death creates one XP drop. Reports/frame traces retain triggers, rays, target tests, hits, damage, kills and source weapon; multiple outcomes do not become multiple triggers.

`stack-reference` is an explicitly labelled 60-second diagnostic comparison with the same enemy/rifle settings, no XP/offers and a frozen `rifle`, `all`, `scatter` or `pierce` starting loadout in its identity. Specialized references use two weapons. No reference grant enters ordinary earned progression.

## Checks and discovered limit

The initial pair check exposed a real failure: a burst of XP events filled Transport 2.7.4's reliable send window (`NetworkSendQueueFull`, -5), causing an abort. A FIFO now retains complete records under temporary backpressure; Poll retries them in order. It is bounded to 1024 messages and explicitly aborts on backlog exhaustion rather than discarding XP/hits. A 200-event loopback regression verifies full ordered delivery past the transport window. Summaries record pending/high-water/deferred counts. This repairs burst handling, not sustained dense-network capacity.

Serialized Unity and standalone check results/source will be added after the candidate build. The background checker in [check_stack.py](evidence/gdj-3-weapon-stack/check_stack.py) uses ordinary scripted movement/firing/collection and explicit choices, holds both pauses, verifies complete runs, independent builds, conserved XP, exact pair outcomes/state and per-weapon work, Retry/stale rejection and loss during Choosing. Diagnostic synthetic deaths occur only in isolated rule/transport tests, never in the ordinary player fixture.

## Reproduction and remaining evidence

Use the whole archived candidate after its manifest is recorded. Select Weapon stack then Start; WASD/mouse aim and automatic installed weapons. Click/1/2 selects; Continue/Enter resumes when the roster is ready. Orange spread and purple piercing are minimal behavior cues. Test one broad three-weapon path and one specialized two-weapon path. The owner must establish whether added power/behaviors are perceptible and worth choosing.

Native representative performance and human co-op/build comparison remain unverified. The shared fixture still has only 35 drones, managed pairwise steering and full snapshots. Original Jobs/Burst local crowd controls are separate. This slice cannot establish the intended >10,000-enemy cooperative/ability-heavy encounter or 144+ FPS envelope. A later combined dense stacked workload and scalable replication measurement remain necessary before claiming larger-game feasibility. No new content, rare rewards, recovery or full banked/planned thinking-space system.
