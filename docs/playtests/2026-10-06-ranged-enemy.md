# GDJ-25 ranged-enemy candidate — October 6, 2026

This bounded layer serves enemy abilities and readable, responsive combat. Owner readability/enjoyment remains unverified. The [task brief](../planning/tasks/GDJ-25.md#implementation-ownership-and-interactions--october-6) records ownership and future interactions before implementation. No GDJ-24 performance ladder or publication diagnosis was resumed.

## Source and implementation

Worker `01a11023-fdee-7921-b208-e693a0f1ac09`, managed worktree `C:/Users/Darian/.codex/worktrees/gdj-25-ranged-enemy/new-game-dev`, branch `codex/gdj-25-ranged-enemy`, exact start `0cd77ad4600d21ce812df1c52e0cb40149df831b`. Upstream `742ca2a8eb0186fee93c56ffe7eefc62727da0ba` is still in draft PR #16. A stacked draft PR targets `codex/gdj-24-coop-scale`; main is not replaced or merged.

`mixed-threats` retains the 120 active seconds/35-enemy earned weapon-stack fixture: 30 chasers and five sentinels. Each sentinel approaches to 14 m, holds position during a committed 1.25-second wind-up, fires one straight bolt and cools down for 2.75 seconds. Initial cooldowns stagger by .45 seconds from .5 seconds. Bolts use 10 m/s, 15 damage, .26 m radius, 3.5-second lifetime and capacity 20. These values are hypotheses. Crown/cannon geometry, a growing charge, an aim beam and a bright sphere/tail distinguish the threat; adornments retain ordinary body/head weapon hit regions.

Solo and pair use the host simulation. Ranged snapshots piggyback on bounded atomic chunks; configuration v4/protocol 7 reject old players. Pure-chaser snapshot payloads retain their former layout. Choice freeze, attack cancellation on enemy death, surviving released bolts, once-only swept hits against either original player life, grace/immunity, terminal cleanup and complete Retry use the existing run path. Enemy deaths yield the normal collectible XP; personal arsenal choices remain independent.

## Verification checkpoint

Pinned Unity 6000.3.25f1, URP 17.3.0, Input System 1.20.0 and Test Framework 1.6.0 retained. `tools/unity.ps1 -Action Test -TestPlatform EditMode`: **162 passed, zero failed**. `-TestPlatform PlayMode`: **19 passed, zero failed**. New checks exercise committed aim, stationary wind-up/dodge, solo and either-player damage, relative swept collision/nearest player, life/immunity/grace, choice freeze/resume, enemy death/XP, bounded lifetime/capacity, deep/atomic parsing and real loopback wind-up/flight/damage/Retry delivery. The pre-existing prototype/scale regression suites run as part of those totals, without a new benchmark capture.

Release build and actual standalone solo/pair smoke are the next checkpoint. The [smoke runner](evidence/gdj-25-ranged-enemy/check_mixed.py) uses ordinary scripted input and game-owned commands/state on an owned background desktop; it checks earned choices, ranged freeze, pair death/return, terminal authority parity and Retry. Its evidence is functional only.

## Remaining review

Human action: **spot the wind-up, dodge a shot, choose a threat, earn an upgrade and Retry**. Was the shot readable, and did the threat change movement or target choice? Owner play and orchestrator review remain required; no merge is authorized. Existing GDJ-24 timing/publication findings are neither re-run nor claimed fixed. LAN/internet, production roster/recovery, accessibility, tuning/balance, large ranged crowds and fun remain outside this evidence.
