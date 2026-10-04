# Roadmap

This is a sequence of learning milestones, not a release-date promise. Update it as playtests change what we know.

These checkboxes describe capabilities and milestone evidence. Linear owns individual task priority, status and dependencies; see [our workflow](workflow.md).

October 2 direction update: [decision 0002](decisions/0002-cooperative-pve-ai-world.md) changes the product to cooperative PvE for 2-4 players, testing two first, in an AI-controlled isekai world. Historical local milestones remain completed local capabilities; they do not establish multiplayer acceptance.

October 3 clarification: [decision 0008](decisions/0008-bullet-heaven-solo-play-and-core-experience.md) extends support to **1-4 players, solo or cooperative**, using bullet-heaven survival roguelite language. Prioritize quick entry, pleasant level-ups and the performant core. League of Legends/Valorant are broad hardware/responsiveness aspirations; exact low-end support and surrounding run structure remain open.

## 0. Establish the starting point

- [x] Capture the motivation and scope.
- [x] Create a repository structure and working documents.
- [x] Select a reference direction: first-person horde survival inspired by Vampire Hunters.
- [x] Identify the owner's priority: stacking weapons and creating powerful builds.
- [x] Accept cooperative PvE and select The Engine Kingdoms AI-world isekai direction.
- [ ] Define one small cooperative encounter from the [selected direction](design/ai-world-options.md).
- [x] Set the baseline performance goal to at least 144 FPS, with higher frame-rate options.
- [x] Use the current desktop as the development benchmark machine: i7-10700K, RTX 3090, approximately 64 GB RAM; retain existing 1080p captures for comparisons.
- [ ] Finalize visual settings and a supported peak encounter for [performance validation](performance.md) on this desktop.
- [ ] Record a focused reference play session and refine the [stacked-weapons experiment](../prototypes/001-stacked-weapons/README.md).
- [x] Choose Unity with C# and URP; accept [the engine decision](decisions/0001-engine-recommendation.md).
- [x] Implement the initial prototype for Windows desktop.
- [x] Create the Unity project; pin editor 6000.3.25f1, URP 17.3.0, Input System 1.20.0, and Test Framework 1.6.0 plus transitive dependencies.

**Done when:** we can name the player's main action, the question our first prototype will answer, and the tools needed to build it.

## 1. Make one interaction playable

- [x] Build a one-minute movement and shooting test with placeholder visuals.
- [x] Implement standalone crowd benchmarks and frame-time reports; validate the capture path locally, recording actual hardware and explicitly marking missing GPU timings.
- [x] Extend the stress harness to 20,000 enemies with synthetic boss area attacks, projectile bursts, and synchronized attack spikes.
- [x] Add stage profiling, a batched Jobs/Burst simulation path, culled instanced rendering, and selectable reference paths for controlled comparisons.
- [x] Add controlled experiments for effect rendering, identical-warning deduplication, concurrent rendering preparation, and native state ownership; preserve reference paths and gameplay parity checks.
- [ ] Verify a minimal two-player shared encounter: responsive control, consistent hits/state, team ending and restart; record latency/network and performance limits.
- [ ] Revise progression and lifecycle rules for 1-4 player solo/cooperative play before implementing the later slices.
- [ ] Validate more than 10,000 ability-heavy bosses at 144 FPS on the current desktop, including representative boss visuals and the most expensive stacked weapon build.
- [ ] Compare one weapon against three simultaneously firing weapons.
- [ ] Add the proposed five-minute survival and upgrade loop if the combat test is promising.
- [x] Add rifle feedback, shot audio, health/damage cues, kill count, and a run-ending state.
- [ ] Play it and record what is promising or confusing.
- [ ] Decide to continue, revise, or try another idea.

**Done when:** the prototype answers its stated question, including the new cooperative feasibility and contribution questions, and we have a written next decision. A discarded prototype can still satisfy this milestone.

## 2. Complete a tiny game loop

- [ ] Move the chosen approach into `game/`.
- [ ] Add a starting state, an objective, an ending, and a restart.
- [ ] Make controls and feedback understandable without explanation.
- [ ] Observe someone else playing and record their difficulties.

**Done when:** a small team can complete and restart a cooperative session without developer intervention.

## 3. Refine the experience

- [ ] Improve the most important playtest problems.
- [ ] Tune controls, timing, difficulty, sound, and visual feedback.
- [ ] Check readable text, input clarity, and relevant accessibility settings.
- [ ] Create a build and test it outside the editor on the chosen platform.
- [ ] Validate the 144 FPS target in the supported peak encounter and verify gameplay timing at different frame caps and uncapped.
- [ ] Once the game is more polished, benchmark a GTX 1080 Ti and older-generation CPUs; use measured results to define hardware support and scalable settings.
- [ ] Add useful automated checks for systems that now justify them.

**Done when:** the small game plays consistently, its remaining problems are documented, and another playtest supports the direction.

## 4. Finish and share

- [ ] Settle the project's license and verify credits and asset provenance.
- [ ] Document exact setup, run, test, and export steps.
- [ ] Smoke-test the distributable from a clean download or install.
- [ ] Publish a versioned build at the chosen release destination.
- [ ] Write a retrospective: what we learned, what AI helped with, and what we'd change.

**Done when:** someone else can obtain and play the game, and we have recorded the outcome.

## Accepted future feature: Party thinking space

The owner accepted the [thinking-space hybrid](decisions/0004-party-thinking-space.md) on October 3, 2026 and requested a Linear milestone for later work. Bank earned choices for planned breaks, allow earlier player invocation, freeze physical combat for unlimited mental upgrade/loadout decisions, then resume when all active players are ready. The **Party thinking space** milestone in the [Linear project](https://linear.app/cnid/project/first-weapon-stacking-playtest-89b5952f722e) and [GDJ-7](https://linear.app/cnid/issue/GDJ-7/build-party-thinking-space-for-cooperative-upgrades) own later planning and implementation. Begin with two players after minimal shared combat/progression/lifecycle feasibility; preserve 1-4 player and future draw/loot interactions, including single-player readiness and reward behavior. No date or immediate implementation is scheduled. Detailed behavior and human validation remain pending.

## Future weapon presentation and accessibility research

[Decision 0009](decisions/0009-weapon-control-and-accessible-combat.md) accepts configurable auto-shoot, varied targeting and through-wall enemy visibility, with detailed rules open. The owner requested [GDJ-11: weapon-position experiments](https://linear.app/cnid/issue/GDJ-11/experiment-with-weapon-positioning-and-arsenal-visibility) and [GDJ-12: deep combat-readability/accessibility research](https://linear.app/cnid/issue/GDJ-12/research-combat-readability-and-accessibility). They are future Backlog work at Medium and High respectively, with no date or implementation commitment. Readability remains a core design priority; these tasks will produce bounded recommendations and human evidence, preserving the performant 1-4 player loop.

## Future death participation and shared reward research

[Decision 0010](decisions/0010-xp-pickups-and-death-participation.md) accepts collected enemy-drop XP orbs, magnets, percentage pickup-range upgrades and gear-upgrade access while dead. [GDJ-13](https://linear.app/cnid/issue/GDJ-13/research-death-revival-and-continued-player-participation) is future High-priority research when focusing on multiplayer, with timed revival/lives/participation, participant eligibility/event ordering and restart coordination/reset open. Retry-button restart is accepted. All players dead and waiting to respawn is the accepted run-ending condition, including solo, despite pending timers. [GDJ-14](https://linear.app/cnid/issue/GDJ-14/research-shared-legendary-compatibility-and-reward-rules) researches rare compatibility, duplicates, player-state eligibility and pending choices. No dates, immediate implementation or completed research; a bounded first test still needs minimal explicit rules.

## Parking lot

Put tempting ideas here without treating them as commitments. For each one, note the player benefit and what evidence would justify adding it.

Accepted future features are recorded above; this space is for further uncommitted ideas.

The owner parked these beyond MVP/alpha on October 3, 2026 at extremely low priority. Linear owns their live status/priority; they do not gate the first-person cooperative run.

- [Permanent progression matrix](https://linear.app/cnid/issue/GDJ-9/idea-permanent-progression-matrix): could give long-term build planning through discoveries, pool preparation and possible power. [Prior exploration](design/permanent-progression-matrix.md) retains details. Revisit after the core run is evaluated if the owner prioritizes it; require evidence about mixed-progression friends and draw-pool effects.
- [Optional first-/third-person views](https://linear.app/cnid/issue/GDJ-10/idea-optional-first-and-third-person-views): could improve character/weapon visibility and view preference. [Camera exploration](decisions/0007-roguelike-run-identity-and-camera-exploration.md#camera-proposal-to-explore) retains questions. Revisit if later player evidence justifies aiming/collision/animation effort; keep first person for MVP/alpha.
