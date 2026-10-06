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

Save the exact commit, branch/worktree, PR/base dependency, scenario/tuning and changed behavior; list the relevant checks, one current build path and short reproduction steps. Record actual results in a dated playtest record and link it from Linear. Separate automated correctness, human observations and assumptions. Update [current state](../../current-state.md) when capabilities change and [planning](../README.md) when intent changes; the foundation changes only for an accepted product decision.

If unfinished, keep completed steps, uncommitted changes, failed/unrun checks, remaining acceptance, blockers and the next concrete action in the issue's Resume or this linked plan. Finish the implementation handoff for orchestrator review without starting another feature or merging upstream.
