# 0008: Bullet heaven solo play and the core experience

- **Date:** 2026-10-03
- **Status:** accepted owner clarification of genre, 1-4 player support and experience/performance priorities; martial-energy naming and detailed weapon-growth rules remain proposals.

## Context

The owner reviewed the design synthesis and corrected its genre and player count. The intended category is bullet heaven with survival and roguelike/roguelite identity. Cooperative play with friends is preferred, and solo players should also receive an accessible, welcoming core experience. The owner confirmed quick entry with one gun and the simple objective of surviving, emphasized pleasant level-ups and perceptible growth, and prioritized the performant core before designing the surrounding game structure.

This explicitly extends the earlier 2-4 player commitment in [decision 0002](0002-cooperative-pve-ai-world.md). The world, shared XP in cooperative play, independent personal offers, selected shared rare rewards, future thinking space and hybrid guns remain accepted.

## Accepted direction

- Describe the game as an **original bullet-heaven survival roguelite for 1-4 players**, playable solo or cooperatively. First person remains the MVP/alpha direction. Keep two-player cooperative testing as the initial networking investigation, while retaining a solo path through the same core loop.
- Enter quickly with one gun, survive, level up and choose build growth. Defeating enemies is an expected progression route; it is not a permanent rule excluding future non-kill XP sources. Those sources are not selected content yet.
- Prioritize the player's experience of leveling up: a pleasant reward, meaningful choice and perceptible capability change. The owner described a blessing-like feeling, euphoria and discovery. These express an emotional target, not a measured psychological result or a promise that every choice produces euphoria.
- Focus first on the responsive, high-framerate combat/progression core, then develop the surrounding game. Exact run duration, waves, endless/extraction/boss structure and final objectives remain open. Short test durations are experimental settings, not final-game commitments.
- Use **League of Legends and Valorant as owner-named performance aspirations**, particularly broad hardware reach, responsive play and high FPS on capable machines. Exact low-end hardware, settings, supported workload and network targets remain to define and measure. The existing declared 144 FPS development target and desktop benchmark plan remain; no competitive-game performance parity or low-end support has been demonstrated.
- Keep the proposed distinction between adding capabilities and deepening capabilities open for expansion. It is not a fixed two-part upgrade taxonomy, a weapon cap or an approved growth formula. Perceptible, satisfying improvement is the accepted requirement.

## Reference labels

Checked October 3, 2026: the Steam pages for [Megabonk](https://store.steampowered.com/app/3405340/Megabonk/) and [Vampire Hunters](https://store.steampowered.com/app/2206270/Vampire_Hunters/) both display **Bullet Heaven**, **Survival**, **Action Roguelike** and **Roguelite** among their popular user-defined tags. These tags support the owner's preferred audience language; they are distinct from Steam's broad Genre fields and feature categories. They do not select automatic firing, content counts or another game's complete rules for this game.

## Martial energy naming remains open

Technology, mage magic and the martial-cultivation tradition can still mix in one gun. The owner asked for a more approachable term than cultivated qi. **Inner energy** is the design chat's recommended player-facing wording; **life force**, **vital energy** and **energy** are alternatives, not accepted names. Retain qi/murim/dantian as internal lore references until terminology is selected. A proposed vocabulary reserves dantian for the internal reservoir and uses inner energy for the power; this does not select a resource bar, health cost or cultivation ladder.

## Consequences and follow-up

The same core progression must make sense with one player. Party sharing, readiness and endings require a sensible solo case; solo legendary selection must not create duplicate grants. Exact solo-versus-party enemy/reward tuning remains open. Evaluate rarity opportunity and draw modifiers across 1-4 players rather than assuming one configuration fits every party size.

Preserve future XP sources through the team-progression boundary, without implementing speculative sources now. Evaluate proposed reward presentation through anticipation, understandable choice and observable change after selection; no specific animations, sounds, UI sequence or reward cadence are accepted here.

Frame pacing, responsive input and network response need separate evidence. High render FPS alone cannot establish low ping. Keep scalable presentation costs separate from combat outcomes, and measure representative upgraded gameplay rather than inferring final performance from the existing local synthetic harness.

Record this in [foundation revision 12](../foundation.md), reconcile the current brief and Linear handoff, and continue only the core design necessary for a small test. The permanent matrix/power and optional cameras remain parked beyond MVP/alpha. Full thinking-space behavior and detailed capacity/mastery/prepared-power work remain later tasks. No gameplay code, Unity operations, benchmarks or human evidence is added by this decision.
