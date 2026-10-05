# GDJ-3: Earn a simultaneous weapon stack

## Outcome

The owner selected MVP proof over further detail refinement on October 5: give v7 a quick blocking-flow check, then test weapon stacking with cooperative progression. One rough graybox run must grow a rifle into a recognizably different arsenal and expose two build paths. This serves perceptible power growth and distinct builds; measure the changed workload and preserve readability.

## Base and blocking gate

Reuse the attached managed checkout `C:/Users/Darian/.codex/worktrees/gdj-23-earned-growth/new-game-dev` on new branch `codex/gdj-3-weapon-stack`, from `cc00db7` / PR 14. Main is still `bd2bd14`; no merge is authorized. PR 14 and the exact v7 archive stay frozen. New review will be stacked on that candidate so its changes are inspectable.

v7 runtime `f7084023ae6ff3988efe384421c8a809a1aade8b` has 131 Edit / 15 Play passes and four ordinary 60-second solo/pair runs covering start, earned choice, physical freeze, explicit Continue, Retry/reset/stale rejection and loss. Its full archive rehashes unchanged. The owner now reports “Everything else seemed fine” after playing v7: the quick human blocking check is clear, with one text-only correction to show the upgrade's effect and omit unchanged/previous values. [Exact observations and native log limits](../../playtests/2026-10-05-earned-growth.md#owner-v7-smoke-and-minimal-choice-text-october-5) preserve this result. The correction is on this branch for the next candidate; it does not modify the frozen v7 archive. Keep future cards similarly concise. The bridge screenshot limitation is not a gameplay blocker. Additional XP/audio/presentation polish is parked. GDJ-23 remains In Review; no detailed two-build/co-op acceptance or performance result is implied.

## Bounded test settings

Use a new `weapon-stack` fixture: same arena, seed 137, 35 respawning 60-HP drones, 100 player HP, baseline rifle 20/40 at .12 seconds, **120 active seconds**, two earned choices at **10 and 25 cumulative collected XP**, test level cap 3. These are experimental settings, not final balance/run format. Preserve the existing one-level earned-growth fixture.

At level 2, each player independently chooses **Add scattergun** or **Add piercer**. At level 3, choose the missing secondary (three simultaneous weapons) or specialize the previously added one (a distinct two-weapon build). Scattergun: five horizontal fan rays, 12 body/24 head damage, .75-second cadence, 30-metre range, ±8-degree fan. Its ordinary specialization adds two pellets. Piercer: one 65-metre ray, 16/32 damage, .65-second cadence, at most three distinct enemies in distance order; its ordinary specialization raises that to five. No wall penetration policy is selected by this empty bounded arena.

All installed weapons fire together with independent cooldowns, following the current camera aim. Keep the paused chooser, all-roster Continue, shared collected XP, independent offers, dead participation, return/ending/Retry and loss-abort. No extra enemies, skills, rares, magnets, recovery, generalized modifier engine or presentation refinement.

An explicit `stack-reference` scenario with a frozen starting arsenal supports one-versus-three and specialized loadout comparisons without pretending debug grants were earned. Same workload/settings, no XP/choices, clearly marked diagnostic in configuration/reporting. No diagnostic setup may enter the ordinary earned run.

## Ownership and source plan before implementation

`ScenarioConfiguration` owns named starting fixture/reference arsenal and a versioned identity. `SharedPlayer` owns a compact authoritative arsenal mask, first addition and specialization, independent from the primary rifle settings. Extend offer card identity rather than relying on the old two-option complement. Definitions distinguish installing a slot from enhancing an existing trait; both cards have explicit legal consumers. Preserve room for later weapon/player/loot/draw effects without implementing them.

`SharedEncounter` owns six player/slot weapon IDs, per-slot cooldowns, complete historical pose/build eligibility, trigger identity, multi-ray/piercing damage, immutable per-ray/target feedback and enemy-incarnation death deduplication. Derive all rays/targets/damage on authority; client submits only owned weapon plus aim/identity. A piercing trigger hits an incarnation once; scatter rays may hit the same live incarnation but a reused slot cannot receive another ray from its previous incarnation. The killing ray creates one death/drop. Capture the complete pre-trigger pose before applying multi-ray damage.

Growth grants one offer per earned level/roster member with run/level-scoped IDs and independent order streams. Reuse the physical Choosing/control revision barrier for both levels. Recompute cooldown eligibility per slot without retroactive bursts; clear queued fire at the pause, retain builds on return and reset every slot/counter/offer on Retry.

`SharedGame` emits installed slots independently, projects authority outcomes and shows minimal colored slot/tracer cues. Reports record trigger/ray/target/weapon attribution so one-versus-three comparisons have equivalent work counters. Compact build state stays in bounded snapshots; reliable hit/growth records stay individually bounded. Test actual packet sizes after extensions. Never discard XP or hits to fit a packet.

## Meaningful checks and stopping point

- Independent weapon cadence/ownership, scatter geometry, piercing distance/unique targets, head/body damage, historical incarnation reuse and one death/drop.
- Two personal growth stages, legal card targets/build divergence, same-tick paused commits, all-roster readiness, dead retention and complete Retry/stale/duplicate rejection.
- Actual standalone solo and pair earning, broad/specialized paths and a controlled reference comparison; preserve original fixtures/tests and whole v7 build.
- Clean release, exact source/manifest/report and short owner comparison. Scripts establish mechanics; the owner establishes perceived power/differences and blockers.

The current shared fixture remains limited to 35 drones and its managed pair simulation/full snapshots do not scale to thousands. Existing Jobs/Burst local crowd controls remain separate. Record this as an unresolved MVP risk: a later representative dense stacked workload and scalable cooperative replication must be measured before claiming the larger game is feasible. GDJ-1's preset naming/20,000-boss endurance task no longer blocks this owner-selected gameplay experiment; its unverified claims remain unverified. Native visible performance capture requires a quiet dedicated desktop window and is not inferred from the background preview.

## Resume

Owner authorization is the October 5 “Okay let's do that then” reply to the quick-v7-check/weapon-stack recommendation. Worktree reused; branch/base above; no other agent/foreground input/new chat. v7's quick human check is clear and its archive is frozen. Draft [PR 15](https://github.com/OpenCnid/game-dev-journey/pull/15) is stacked on PR 14. Review candidate is the whole `prototypes/001-stacked-weapons/build/StackLab-GDJ3-v3` folder, clean source `d8c3eee6ad6b4b8109c8d63c524512efb9a918b3`, manifest and [verification/report](../../playtests/2026-10-05-weapon-stack.md) linked there. Final checks: 143 Edit / 17 Play / two runner tests; ordinary solo/pair earning, both pauses, attributed work/XP conservation, Retry/stale rejection/loss and all four 60-second frozen references pass. v1 additionally covers both personal build-path combinations in solo/pair. Two failed v2 attempts hit the unchanged eight-tick catch-up safeguard; v3 adds timing diagnostics but successful repeats do not establish a cause/fix. Preserve those failures and diagnose any recurrence; do not weaken the safeguard to pass. Next owner comparison: one run adding both secondaries, then Retry and enhance the first addition; report noticeable contributions, different play and blockers/hitches. Human co-op feel and a quiet visible representative performance capture remain open; no larger-game/dense-co-op feasibility claim. All owned automated children stopped. Owner's visible v7 PID 31288 remains under their control. No merge/Done authorization; GDJ-3 is ready for review/human play.
