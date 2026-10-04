# 0010: XP pickups and death participation

- **Date:** 2026-10-03
- **Status:** accepted XP-orb pickup, magnet collection, upgradable pickup range and gear-upgrade access during death; detailed revival/participation and shared-rare policies remain future research.

## Context

The owner clarified XP collection after reviewing shared progression: enemies drop XP orbs where they die, and players must collect them. Magnet abilities can vacuum them up, and pickup distance can increase through percentage upgrades during a run. Megabonk and Vampire Hunters are owner-named references; this records our selected mechanic rather than independent measurements of those games.

The owner also requested later research into keeping dead players involved, made it a priority for multiplayer, and explicitly requested a shared legendary edge-case research ticket. These extend the [shared XP and personal draw direction](0003-shared-xp-personal-upgrade-draws.md) within the [1-4 player product](0008-bullet-heaven-solo-play-and-core-experience.md).

## Accepted progression direction

- Enemies drop XP orbs at their death location. Killing creates collectible XP; collection credits progression rather than an immediate kill-based XP grant.
- In cooperative play, a collected orb feeds the shared team XP pool. Players keep their independent upgrade choices; collecting an orb does not create a separate private XP ladder.
- Magnet abilities can vacuum/collect XP orbs. Exact range, timing, activation and cooldown behavior remain open.
- Pickup distance is upgradable during runs through percentage modifiers. Baseline distance, units, composition, caps and the relationship to magnets remain open.
- Keep future non-kill progression possible. Its sources and collection presentation are not selected by this enemy-drop rule.

## Death participation direction

Dead players should remain meaningfully involved, retain access to gear upgrades through earned choices and return to active play. Timed revival is the direction to investigate; multiple lives and longer death timers are possibilities. Exact timing, limits, penalties and solo/full-party defeat conditions remain open. Whether dead players receive newly earned team choices, how their selections affect ongoing combat and whether they can invoke thinking space still need explicit rules.

[GDJ-13: Research death, revival and continued player participation](https://linear.app/cnid/issue/GDJ-13/research-death-revival-and-continued-player-participation) is future Backlog at High priority for the multiplayer checkpoint. Compare a few candidates, include periods without available upgrades, and record actual participation/comprehension evidence when a playable investigation is authorized. League of Legends, Valorant and Among Us express the owner's experience references; this record asserts no universal player boredom or shared death rules across those games/modes.

## Shared legendary research

[GDJ-14: Research shared legendary compatibility and reward rules](https://linear.app/cnid/issue/GDJ-14/research-shared-legendary-compatibility-and-reward-rules) is future Backlog at Medium. Resolve incompatible/missing prerequisites, already-owned/max-rank rewards, independent duplicate selections, downed/dead/disconnected recipients, dead selectors and pending choices/modifiers. Preserve grant on selection, original-player credit and the solo case; no conversion reward or permanent unlock is selected here.

## Consequences and follow-up

Keep collectible world state, collection/range/magnet effects, shared XP and personal offers logically distinct. Orb collection must credit the shared pool once, including competing collectors or magnet overlap; this is a correctness requirement, not a selected networking architecture. Preserve collection behavior across freeze/resume, endings and restarts without selecting pooling, aggregation or orb limits now. Measure eventual orb density and collection costs alongside the growing weapon/crowd workload.

The [progression record](../design/progression-and-upgrade-pools.md) reconciles kill/drop/collection semantics and links both research tasks. [Foundation revision 13](../foundation.md) and the encounter/brief preserve accepted rules. GDJ-13/GDJ-14 have no dates or immediate implementation commitment. Full research remains later; a bounded first test still needs explicit minimal lifecycle and reward rules. No gameplay code, Unity operation, benchmark or human study accompanies this checkpoint.
