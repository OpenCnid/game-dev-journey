# GDJ-2: observe the current rifle before expansion

Planning recommendation, October 2, 2026. Live status and dependencies remain in [GDJ-2](https://linear.app/cnid/issue/GDJ-2/play-the-current-rifle-slice-and-choose-the-next-gameplay-change). This brief prepares a review/playtest session; no task session or gameplay implementation was started by the planning inspection.

**Priority clarification:** the owner subsequently chose [theme/identity design first](GDJ-6.md). Preserve this playtest brief for later; it is no longer the immediate next session. The checkpoint below records the earlier inspection, not the current priority.

**Scope clarification:** [decision 0002](../../decisions/0002-cooperative-pve-ai-world.md) changes the intended game to cooperative PvE. This brief still evaluates the existing local rifle as a control; it cannot establish shared-combat, cooperative pause/progression or multiplayer performance acceptance.

## Why this outcome

The largest immediate player-experience gap is whether moving, aiming and reading damage in the existing combat slice feel promising enough to expand. Two short human runs can identify a specific problem before more weapons obscure its cause. This serves player movement/aiming, combat feedback and run lifecycle, especially the foundation's [readable, responsive combat pillar](../../foundation.md#3-readable-responsive-combat). It is an entry check for the later [power-growth and distinct-build experiment](../../design/game-brief.md#first-question-and-stopping-condition), not evidence that those pillars are already delivered.

GDJ-2 explicitly permits the current default renderer while GDJ-1 is pending. Both the playtest direction and rendering-preset decision must be accepted before GDJ-3 begins. If play finds a concrete blocker, scope one revision before stacking. Keep one implementation task active.

## Inspection checkpoint

- **Source:** clean detached HEAD `d135c3043cc05dd4497ed165f68c206e4d17ca5b`, matching remote `main` when inspected, in `C:\Users\Darian\.codex\worktrees\48b7\new-game-dev`. Gameplay project and tools are unchanged from `7414fac`; all subsequent changes through this inspected revision are documentation/configuration for the working system.
- **Implemented:** `PrototypeGame.StartCombat` starts 35 drones with seed 137, one autofiring rifle, 60 seconds, health, pause, terminal state and restart. `CombatSimulation` and the existing runtime smoke test support these narrower claims. Three weapons, pickups and upgrade choices are proposed work.
- **Default configuration:** Jobs/Burst simulation with staged state, culled instances, cube projectiles, separate character meshes, 144 FPS cap and VSync off. Native ownership and combined rendering require opt-in flags. Pinned Unity/package versions match the [prototype README](../../../prototypes/001-stacked-weapons/README.md).
- **Historical automated evidence:** the [renderer report](../../playtests/2026-09-30-renderer-experiments.md) and [test summary](../../playtests/evidence/renderers/test-summary.json) record 33 Edit Mode and two Play Mode passes. Full release captures identify source `8fa326a`; their three combined runs recorded 400.4-467.6 average FPS and 3.743-4.297 ms p99 at 20,000 synchronized synthetic bosses. The [analysis](../../playtests/evidence/renderers/full-analysis.json) confirms 3,600 ticks and 720,000 projectile spawns per run. These results do not measure current human play or the eventual stacked build; occasional frames exceeded the 144 FPS budget.
- **Board:** GDJ-1 and GDJ-2 have no blockers or existing Resume sections/comments. No issue in this project was In Progress or In Review, and no open repository PR was returned at inspection. This is an inspection snapshot, not a task queue.
- **Unverified:** human combat observations, staggered/endurance preset validation and representative upgraded-game performance. No Unity tests/builds or captures were rerun in this planning session. No standalone player exists at this worktree's documented release output path.
- **Next action when resumed after design:** start the review/playtest session below, build the exact candidate, then collect two developer runs. Recheck source and board before acting. No playable capability changed during the planning inspection.

## Copy-ready session prompt

```text
Act as the review/playtest session for GDJ-2: Play the current rifle slice and choose the next gameplay change.
Issue: https://linear.app/cnid/issue/GDJ-2/play-the-current-rifle-slice-and-choose-the-next-gameplay-change
Repository: https://github.com/OpenCnid/game-dev-journey
Inspected base: d135c3043cc05dd4497ed165f68c206e4d17ca5b. The saved brief may be on a later documentation-only commit; confirm the actual HEAD and diff before selecting the build revision. Worktree inspected: C:\Users\Darian\.codex\worktrees\48b7\new-game-dev.

Read AGENTS.md and docs/foundation.md first, then docs/planning/README.md and docs/current-state.md. Fetch GDJ-2 with its dependencies and any Resume section. Follow docs/workflow.md, docs/design/game-brief.md#first-question-and-stopping-condition, prototypes/001-stacked-weapons/README.md (setup, controls and slice A), and docs/playtests/README.md plus template.md. This task serves the movement/aiming, feedback and run lifecycle systems, especially readable, responsive combat. Decide whether the rifle slice supports testing perceptible power growth through additional weapons.

Outcome: two short developer runs of the current standalone rifle slice, a proceed/revise decision, and one next change supported by specific observations.

Scope: prepare and evaluate the existing 60-second/35-drone slice. Use the default renderer; GDJ-2 need not wait for GDJ-1. Preserve Unity 6000.3.25f1 and pinned packages. Do not add weapons, upgrades, enemy content or optimizations in this session. If play exposes a problem, save the evidence and scope one bounded implementation revision for planning/integration.

Candidate: close the Editor for this project before batch operations; serialize Unity tests/builds. From the repo root run .\tools\unity.ps1 -Action Build. Record exact source, dirty state, build timestamp, full build-folder path, launch command and player log. If tracked setup files change, inspect them and resolve the candidate revision/dirty state before play. Never silently reuse an older EXE. Existing tests are historical; run relevant checks if preparation exposes errors or behavior changes, and label any new results separately.

Launch the player at prototypes/001-stacked-weapons/build/StackLab/StackLab.exe with -playtest -seed 137 -simulation burst -rendering culled -screen-width 1920 -screen-height 1080 -screen-fullscreen 0. Record actual hardware/settings, including the 144 FPS default cap, VSync off and any player adjustment. Distribute the whole build folder if needed. Do not supply experimental renderer or benchmark flags.

Acceptance:
1. Confirm standalone launch, firing and usable controls; review the player log for runtime/shader errors. This technical smoke check is separate from human evaluation.
2. The developer plays at least two short runs of that exact candidate. Check movement/aim response, useful positioning, recognizable rifle hits/kills, damage/threat clarity and the run's goal/ending. Deliberately check Escape pause/resume and R restart, including clock, health and kill reset. Identify the participant by an anonymous label.
3. Capture specific moments and the player's own feedback separately from AI interpretation. Identify the largest friction and whether another run/weapon experiment sounds appealing. AI control, screenshots and passing tests cannot substitute for these observations.
4. Record proceed if the basic controls and combat feedback are promising enough for the next experiment; otherwise record revise and one concrete problem to address. Do not treat two runs as proof of fun, build variety or broad demand. Do not launch GDJ-3 until the GDJ-1 preset decision is also accepted.

Handoff: save the dated report in docs/playtests/ using the existing template, add it to that directory's README, and link the commit/PR, candidate/configuration, checks, observations, decision and one next change from GDJ-2. Update planning intent/current state only where their facts change. Mark GDJ-2 Done only after its human observations and decision are recorded and reviewed; a built candidate alone belongs In Review. If unfinished, leave a Resume section with exact source/branch/worktree, build/configuration, completed steps, checks, missing acceptance evidence, blockers and next action. If human feedback is pending, preserve the candidate and ask for those observations without inventing them.
```
