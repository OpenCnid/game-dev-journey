# GDJ-25: a readable ranged threat in the growing arsenal run

Prepared October 6, 2026. [Linear issue](https://linear.app/cnid/issue/GDJ-25/add-a-telegraphed-ranged-enemy-to-the-soloco-op-run) owns live status, dependencies, exact worker ID and Resume.

## Outcome

Build one ranged enemy alongside existing chasers so the player can recognize an attack, dodge it and choose which threat to kill while earning a weapon stack. The owner requests a new implementation session and useful gameplay layers after GDJ-24. This is an orchestrator-selected prototype within the existing product, with tuning to discover through play.

The affected foundation system is [enemy and boss abilities](../../foundation.md#the-games-core-systems). The primary pillar is [readable, responsive combat](../../foundation.md#3-readable-responsive-combat); mixed threats also give the existing distinct weapons different situations to handle. The underlying MVP goal remains a coherent solo/cooperative loop: one gun, combat, collected shared XP, personal growth, simultaneous weapons, ending and Retry.

## Start and source

Read [the foundation](../../foundation.md) first, then [planning](../README.md), [current state](../../current-state.md), AGENTS.md and [workflow](../../workflow.md). Read this brief and the issue's latest Resume before implementation. Only follow the context needed for this slice.

- Runtime/upstream base: `742ca2a8eb0186fee93c56ffe7eefc62727da0ba`, branch `codex/gdj-24-coop-scale`, [draft PR #16](https://github.com/OpenCnid/game-dev-journey/pull/16). This brief is saved in a documentation descendant; the dispatch prompt/Linear Resume records its exact starting commit.
- Main remains `e8acedb107905ade056241e978dace6bdaa0bb92` at dispatch. PR #16 has not merged. Work on a separate branch/worktree from the exact dispatch commit. While PR #16 is open, target a stacked draft PR at `codex/gdj-24-coop-scale`; recheck ancestry and retarget/rebase as needed if upstream integrates. No automatic merge.
- Active project: [Unity prototype](../../../prototypes/001-stacked-weapons/README.md). Preserve pinned Unity/C#/URP versions and serialize its Editor/test/build operations.
- Existing behavior: [earned weapon-stack report](../../playtests/2026-10-05-weapon-stack.md). The `weapon-stack` encounter runs 120 active seconds with 35 enemies; collected XP at 10/25 grants personal scatter/piercer addition or specialization choices, physical pause/Continue, retained growth through death/return and full Retry.
- Latest constraint: [GDJ-24 current Resume](GDJ-24.md#current-resume--owner-prioritizes-mvp-october-6) and [proportional verification](../../workflow.md#match-verification-to-the-change). The owner stopped repeated captures; old profile/capture plans are superseded.

## Bounded slice

1. Add one named mixed encounter using the existing 120-second, 35-enemy earned-growth loop. Start with a small ranged minority (for example, five ranged enemies and 30 chasers); record final values as test hypotheses. Preserve pure-chaser and diagnostic reference scenarios.
2. Add one distinguishable graybox ranged enemy. It approaches to a useful range, visibly winds up with a readable committed aim, then fires one visible straight projectile the player can dodge and enters cooldown. Give the player time to respond. Use simple silhouette/motion cues as well as color. Choose and save practical damage, timing, speed and range settings; detailed lore/art is unnecessary.
3. Use the existing authority path for solo and fixed two-player play. The host owns targets, attack state, projectile hits and player damage; both views show the same attack. Enemy deaths feed the existing XP collection/progression system. Keep the existing weapon behaviors and lifecycle intact.
4. Carry the new state through the complete loop: choice pause freezes wind-ups/projectiles, Continue resumes them coherently, current death/return protection remains effective, terminal states stop combat and Retry clears attacks/projectiles as well as progression.

Before runtime edits, add a short implementation note here identifying enemy definitions, attack/projectile ownership, replication/presentation and interactions with future enemy abilities. Decide how aim locks, enemy death, player life changes and projectile cleanup work. Use bounded projectile lifetime/capacity and a hit check that cannot skip a player between ticks; avoid duplicate or stale damage across reset/life boundaries. Keep extension points concrete; a general AI framework or networking rewrite is outside this outcome.

Start inspection with the prototype's `ScenarioConfiguration.cs`, `SharedEncounter.cs`, `SharedGame.cs` and `SharedConnection.cs`, then the affected renderer and tests. Existing local boss/projectile experiments can inform the implementation but do not establish cooperative support. Reuse only what fits this slice.

Bosses, additional weapon content, encounter directors/waves, permanent progression, full party thinking space, recovery, range/falloff rebalance and broad presentation polish stay separate.

## Done when

- In the named solo and two-player encounter, a ranged enemy is recognizable, its wind-up predicts a dodgeable shot, an actual hit damages the appropriate player once, and killing it yields normal collectible XP.
- Both roles agree on attack/damage outcomes. The existing earned weapon choices, physical pause, death/return, ending and Retry work with the new threats; projectiles neither move during choice pause nor leak into a new run. Focused automated behavior/regression checks cover the affected boundaries.
- A short actual solo and two-player functional smoke exercises the combined loop. Use Editor/Play Mode for iteration and create one clearly identified standalone checkpoint for owner play; record exact source/scenario and reuse it while unchanged. Follow `tools/unity.ps1` and preserve owner-controlled windows.
- The owner gets a short action: **spot the wind-up, dodge a shot, choose a threat to kill, earn an upgrade and Retry**. Ask whether the shot was readable and whether the new threat changed movement or target choice. Until that observation is recorded, label readability/enjoyment unverified and leave the candidate In Review.

Stop at a coherent playable candidate plus the required review. Do not run another workload ladder, repeated performance captures or a publication investigation to finish this feature. An observed issue that actually blocks playing this slice can receive a bounded fix/diagnosis; preserve prior GDJ-24 failures without claiming they were solved. Automated checks do not establish fun.

## Durable handoff

## Implementation ownership and interactions — October 6

Worker chat `01a11023-fdee-7921-b208-e693a0f1ac09` uses managed checkout `C:/Users/Darian/.codex/worktrees/gdj-25-ranged-enemy/new-game-dev`, branch `codex/gdj-25-ranged-enemy`, from exact start `0cd77ad4600d21ce812df1c52e0cb40149df831b`. PR #16 remains draft/open; the implementation PR will target `codex/gdj-24-coop-scale`.

The named `mixed-threats` configuration retains the 120-second/35-enemy weapon-stack loop: the first five stable enemy slots are ranged sentinels, the remaining 30 chase. A concrete ranged definition owns range, wind-up, cooldown, bolt speed/damage/radius/lifetime and a bounded pool. SharedEncounter owns all attack timing and swept bolt hits in solo and pair; snapshots carry the five committed attacks and live bolts for read-only presentation. Pure-chaser snapshots/reference workloads retain their existing enemy state layout. New players intentionally reject old protocol versions.

Sentinels approach to 14 metres, stop during a 1.25-second wind-up and lock a straight horizontal aim at its start. A bright cannon/crown, expanding charge and fixed aim beam supplement color. Initial cooldowns stagger attacks; the post-shot cooldown is 2.75 seconds. Bolts initially use 10 m/s, 15 damage, .26-metre radius and 3.5-second lifetime; capacity is 20. These are practical hypotheses for play, not accepted balance/readability. Enemy death cancels its pending attack; a released bolt survives its source's death. A bolt can hit either player once, but only the player lives present at launch; return immunity and the existing shared damage grace also apply. Swept segment collision consumes the nearest intersected bolt even when protection blocks damage. Choice pause freezes physical attack/bolt clocks; terminal states clear bolts/wind-ups, and Retry resets pools, counters and enemy incarnations.

Future enemy abilities can add concrete definitions/state beside this bounded attack without changing weapon damage or XP ownership. Ranged deaths use the normal collectible XP path and personal arsenal offers. Range pressure may favor piercer focus or scatter crowd clearing, and future slows, interrupts, loot changes or draw modifiers must respect authority/life boundaries; none is implemented in this slice. No general AI framework or new progression system is required.

The first v1 pair smoke reached both upgrades, then a 236.5 ms frame gap tripped the existing hard eight-tick catch-up abort at tick 1090; the prior Update CPU was .463 ms and the abort frame's simulation .586 ms. This is a real blocker to playing this slice, not a ranged-cost diagnosis or a new performance conclusion. The bounded revision lets only `mixed-threats` carry up to one second of physical backlog across frames, still advancing at most eight ticks per frame and aborting a longer backlog. It does not discard active time or increase one-frame publication work. Reference scenarios retain their strict abort behavior; counters preserve every catch-up event. The final candidate must repeat functional checks after this change. No workload ladder or profiler investigation is opened.

Save the exact commit, branch/worktree, PR/base dependency, scenario/tuning and changed behavior; list the relevant checks, one current build path and short reproduction steps. Record actual results in a dated playtest record and link it from Linear. Separate automated correctness, human observations and assumptions. Update [current state](../../current-state.md) when capabilities change and [planning](../README.md) when intent changes; the foundation changes only for an accepted product decision.

If unfinished, keep completed steps, uncommitted changes, failed/unrun checks, remaining acceptance, blockers and the next concrete action in the issue's Resume or this linked plan. Finish the implementation handoff for orchestrator review without starting another feature or merging upstream.

## Resume — v2 ready for review, October 6

**Subsequent owner clarification:** the tested combined stack is sufficient for MVP layering; do not benchmark this candidate or each small feature. Rough implementations and shortcuts are acceptable while testing whether the layers fit together. The workflow's leftover routine performance-check row is removed. No measured-capture follow-up is required or planned for GDJ-25; existing human-readability observations remain unverified, without implying a new performance gate.

Worker `01a11023-fdee-7921-b208-e693a0f1ac09` remains on `codex/gdj-25-ranged-enemy` in `C:/Users/Darian/.codex/worktrees/gdj-25-ranged-enemy/new-game-dev`. Exact start `0cd77ad4600d21ce812df1c52e0cb40149df831b`; runtime candidate `02bddffcff2d66e67ef9ff2861169fd9030afe83`; [stacked draft PR #17](https://github.com/OpenCnid/game-dev-journey/pull/17) targets `codex/gdj-24-coop-scale`. PR #16 remains draft/open at `742ca2a`, main `e8acedb`, no merge. Later evidence/docs commits leave that runtime unchanged.

**Current build: `prototypes/001-stacked-weapons/build/StackLab-GDJ25-v2/Play-Mixed-Solo.cmd` in this worktree**, scenario `mixed-threats`. V1 is superseded. [The current report](../../playtests/2026-10-06-ranged-enemy.md#current-review-candidate--v2) saves exact source, clean build stamp/DLL hash, tuning, build/reproduction and the original pair failure. Checks: 162 Edit / 20 Play, zero failures, clean release; actual v2 solo growth/ranged freeze/defeat/Retry and 120-second pair with exact terminal players/enemies/XP/ranged parity, conserved XP, retained growth on return and both-ready Retry. Both functional runner desktops are closed, with owner input desktop unchanged. No scale capture/diagnosis was resumed.

Implementation and functional verification are complete; remaining acceptance is **owner readability/enjoyment observation and orchestrator diff/integration review**. Keep GDJ-25 In Review. Owner action: spot the wind-up, dodge a bolt, choose a threat, earn an upgrade and Retry; report whether the shot was readable and affected movement/target choice. Do not infer human approval from scripted inputs or screenshots. Do not merge or begin another feature from this handoff. Production recovery, large ranged density, tuning/balance and prior GDJ-24 timing findings remain unverified/outside the slice.
