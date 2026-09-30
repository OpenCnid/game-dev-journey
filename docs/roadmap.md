# Roadmap

This is a sequence of learning milestones, not a release-date promise. Update it as playtests change what we know.

## 0. Establish the starting point

- [x] Capture the motivation and scope.
- [x] Create a repository structure and working documents.
- [x] Select a reference direction: first-person horde survival inspired by Vampire Hunters.
- [x] Identify the owner's priority: stacking weapons and creating powerful builds.
- [x] Set the baseline performance goal to at least 144 FPS, with higher frame-rate options.
- [x] Identify a provisional GPU baseline: GTX 1080 Ti.
- [x] Choose an older high-end CPU baseline: Intel Core i7-8700K at stock settings.
- [ ] Define baseline hardware, resolution, visual settings, and a supported peak encounter for [performance validation](performance.md).
- [ ] Record a focused reference play session and refine the [stacked-weapons experiment](../prototypes/001-stacked-weapons/README.md).
- [x] Choose Unity with C# and URP; accept [the engine decision](decisions/0001-engine-recommendation.md).
- [x] Implement the initial prototype for Windows desktop.
- [x] Create the Unity project; pin editor 6000.3.25f1, URP 17.3.0, Input System 1.20.0, and Test Framework 1.6.0 plus transitive dependencies.

**Done when:** we can name the player's main action, the question our first prototype will answer, and the tools needed to build it.

## 1. Make one interaction playable

- [x] Build a one-minute movement and shooting test with placeholder visuals.
- [x] Implement standalone crowd benchmarks and frame-time reports; validate the capture path locally, explicitly marking missing GPU timings and non-target hardware.
- [x] Extend the stress harness to 20,000 enemies with synthetic boss area attacks, projectile bursts, and synchronized attack spikes.
- [ ] Validate more than 10,000 ability-heavy bosses at 144 FPS on target hardware, including representative boss visuals and the most expensive stacked weapon build.
- [ ] Compare one weapon against three simultaneously firing weapons.
- [ ] Add the proposed five-minute survival and upgrade loop if the combat test is promising.
- [x] Add rifle feedback, shot audio, health/damage cues, kill count, and a run-ending state.
- [ ] Play it and record what is promising or confusing.
- [ ] Decide to continue, revise, or try another idea.

**Done when:** the prototype answers its stated question and we have a written next decision. A discarded prototype can still satisfy this milestone.

## 2. Complete a tiny game loop

- [ ] Move the chosen approach into `game/`.
- [ ] Add a starting state, an objective, an ending, and a restart.
- [ ] Make controls and feedback understandable without explanation.
- [ ] Observe someone else playing and record their difficulties.

**Done when:** a new player can complete and restart a session without developer intervention.

## 3. Refine the experience

- [ ] Improve the most important playtest problems.
- [ ] Tune controls, timing, difficulty, sound, and visual feedback.
- [ ] Check readable text, input clarity, and relevant accessibility settings.
- [ ] Create a build and test it outside the editor on the chosen platform.
- [ ] Validate the 144 FPS target in the supported peak encounter and verify gameplay timing at different frame caps and uncapped.
- [ ] Add useful automated checks for systems that now justify them.

**Done when:** the small game plays consistently, its remaining problems are documented, and another playtest supports the direction.

## 4. Finish and share

- [ ] Settle the project's license and verify credits and asset provenance.
- [ ] Document exact setup, run, test, and export steps.
- [ ] Smoke-test the distributable from a clean download or install.
- [ ] Publish a versioned build at the chosen release destination.
- [ ] Write a retrospective: what we learned, what AI helped with, and what we'd change.

**Done when:** someone else can obtain and play the game, and we have recorded the outcome.

## Parking lot

Put tempting ideas here without treating them as commitments. For each one, note the player benefit and what evidence would justify adding it.

No deferred feature ideas yet.
