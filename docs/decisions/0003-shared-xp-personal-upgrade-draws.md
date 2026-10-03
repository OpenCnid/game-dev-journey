# 0003: Shared XP and personal upgrade draws

- **Date:** 2026-10-02
- **Status:** accepted by explicit owner direction; implementation pending.

## Context

The owner wants immediate combat with one gun, earned levels, player-selected improvements and a large simultaneous weapon stack. In GDJ-6 they accepted the proposed shared XP and individual choices, then specified personal random upgrade draws/pools and the need to consider future game interactions when designing features.

## Decision and reasoning

Share XP across the team to align progression opportunities and reduce the risk that one friend monopolizes XP and outlevels the others. Give each player their own random upgrade choices and draw-pool state so builds can develop differently. The owner clarified **independent draws with possible overlap between teammates**: upgrade definitions are not exclusively assigned to one player.

The owner then chose a team reward for **legendary or other qualifying high-rarity pulls**: everyone receives the same upgrade, while presentation credits the original player. The intent is a shared special moment rather than a large reward experienced by one player alone. Whether the grant triggers on appearance in an offer or on selection, and which high-rarity tiers qualify, remain open. This is an exception to ordinary personal rewards; the other players' independent pools are preserved.

Upgrades must be able to influence future loot and upgrade draws as well as weapons. The owner's magic-find percentage example establishes a known future kind of effect; it does not yet choose a stat formula, rarity system or probability. Design bounded features around known future interactions rather than making the current slice a permanent architectural limit. [Progression design](../design/progression-and-upgrade-pools.md) records the proposed separation of team progression, player pools, offers and effects.

This is owner-selected design, not a conclusion from playtest evidence. Shared levels do not guarantee equal power: random outcomes, synergies and effects that improve future draws can produce differences requiring balance evaluation.

## Consequences and open details

- The one-minute encounter uses shared XP and personal random choices. XP rewards, selection timing and team lifecycle still need explicit rules.
- Distinguish an upgrade's gameplay effects from its eligibility and probability of appearing. Player-specific loot modifiers and upgrade-draw modifiers need separately understandable targets.
- Preserve a small initial content scope while avoiding a weapon-only upgrade model. No all-purpose effect engine or complete future content roster is selected.
- Shared high-rarity rewards need a clear original-player identity, one grant per recipient for the same reward event, and defined applicability across different builds. Exact trigger, tier threshold, handling of two genuine repeat pulls, repeat ranks, pool availability, magic-find scope, rarity formulas and join/downed behavior remain open.
- Unity/C#/URP and pinned versions remain accepted. This decision selects no network package or replication method and establishes no performance or fun result.

## Revisit when

Evaluate the first two-player progression candidate for readable growth, distinct choices and whether both players remain useful. Test future reward-modifier interactions before assuming that shared XP alone meets the owner's power-gap goal.
