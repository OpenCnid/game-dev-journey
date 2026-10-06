# GDJ-25 ranged-enemy candidate — October 6, 2026

This bounded layer serves enemy abilities and readable, responsive combat. Owner readability/enjoyment remains unverified. The [task brief](../planning/tasks/GDJ-25.md#implementation-ownership-and-interactions--october-6) records ownership and future interactions before implementation. No GDJ-24 performance ladder or publication diagnosis was resumed.

## Source and implementation

Worker `01a11023-fdee-7921-b208-e693a0f1ac09`, managed worktree `C:/Users/Darian/.codex/worktrees/gdj-25-ranged-enemy/new-game-dev`, branch `codex/gdj-25-ranged-enemy`, exact start `0cd77ad4600d21ce812df1c52e0cb40149df831b`. Upstream `742ca2a8eb0186fee93c56ffe7eefc62727da0ba` is still in draft PR #16. A stacked draft PR targets `codex/gdj-24-coop-scale`; main is not replaced or merged.

`mixed-threats` retains the 120 active seconds/35-enemy earned weapon-stack fixture: 30 chasers and five sentinels. Each sentinel approaches to 14 m, holds position during a committed 1.25-second wind-up, fires one straight bolt and cools down for 2.75 seconds. Initial cooldowns stagger by .45 seconds from .5 seconds. Bolts use 10 m/s, 15 damage, .26 m radius, 3.5-second lifetime and capacity 20. These values are hypotheses. Crown/cannon geometry, a growing charge, an aim beam and a bright sphere/tail distinguish the threat; adornments retain ordinary body/head weapon hit regions.

Solo and pair use the host simulation. Ranged snapshots piggyback on bounded atomic chunks; configuration v4/protocol 7 reject old players. Pure-chaser snapshot payloads retain their former layout. Choice freeze, attack cancellation on enemy death, surviving released bolts, once-only swept hits against either original player life, grace/immunity, terminal cleanup and complete Retry use the existing run path. Enemy deaths yield the normal collectible XP; personal arsenal choices remain independent.

## Verification checkpoint

Pinned Unity 6000.3.25f1, URP 17.3.0, Input System 1.20.0 and Test Framework 1.6.0 retained. `tools/unity.ps1 -Action Test -TestPlatform EditMode`: **162 passed, zero failed**. `-TestPlatform PlayMode`: **19 passed, zero failed**. New checks exercise committed aim, stationary wind-up/dodge, solo and either-player damage, relative swept collision/nearest player, life/immunity/grace, choice freeze/resume, enemy death/XP, bounded lifetime/capacity, deep/atomic parsing and real loopback wind-up/flight/damage/Retry delivery. The pre-existing prototype/scale regression suites run as part of those totals, without a new benchmark capture.

Release build and actual standalone solo/pair smoke are the next checkpoint. The [smoke runner](evidence/gdj-25-ranged-enemy/check_mixed.py) uses ordinary scripted input and game-owned commands/state on an owned background desktop; it checks earned choices, ranged freeze, pair death/return, terminal authority parity and Retry. Its evidence is functional only.

## V1 evidence and bounded blocker fix

Clean release `44e998ce4ec535b8a1db550cdae22c58b2205efa`, build stamp `2026-10-06T07:44:06.5326120Z`, executable SHA-256 `3070E580E019576B9AFCDEB4FEC18D03CEBC119F6AE84D597412905C749AD653`. Solo passes both choices/physical freezes, a natural defeat at tick 3216 (53.6 seconds), collected XP 170 plus one uncollected XP from 171 kills, 31 fired bolts/one hit/30 expired/no overflow and complete Retry. Raw archive: `prototypes/001-stacked-weapons/artifacts/gdj25-v1-solo`.

The first pair run reached both upgrades but aborted at tick 1090 (18.167 seconds) on the existing hard catch-up guard after a 236.504 ms frame gap. Host prior Update CPU was .4627 ms and abort-frame simulation .5859 ms; application queue was empty. This does not diagnose the gap or establish a ranged/transport performance cost. Raw failed archive: `artifacts/gdj25-v1-pair`, with both summaries, commands, state history and unchanged-desktop cleanup. V1 is superseded for review.

Because the abort prevents this playable test, only `mixed-threats` now retains up to one second of physical backlog, advanced over subsequent frames with the same eight-tick per-frame cap. It preserves active time and counts catch-up events; a longer backlog still aborts. Pure reference/scale scenarios keep the strict guard. A Play Mode regression checks bounded carry and the long-backlog guard alongside the existing strict reference test. The smoke deliberately idles through one opening shot before automated combat, then verifies a new death/return after the second choice. Catch-up counts are recorded, without making zero events a feature gate. This is a bounded MVP tolerance change, not a fix for the saved GDJ-24 diagnosis.

After the runtime revision, repeated serialized checks pass **162 Edit Mode / 20 Play Mode**, zero failures. To make the pair death/return check decisive, the runner uses ordinary no-fire movement toward a nearby threat for A while B remains an ordinary automated combat companion, then observes a new life with retained arsenal. It does not mutate fixture health, enemies or authority state. A clean v2 release and its solo/pair smokes follow this checkpoint.

## Remaining review

Human action: **spot the wind-up, dodge a shot, choose a threat, earn an upgrade and Retry**. Was the shot readable, and did the threat change movement or target choice? Owner play and orchestrator review remain required; no merge is authorized. Existing GDJ-24 timing/publication findings are neither re-run nor claimed fixed. LAN/internet, production roster/recovery, accessibility, tuning/balance, large ranged crowds and fun remain outside this evidence.
