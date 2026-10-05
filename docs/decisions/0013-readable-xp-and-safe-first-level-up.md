# 0013: Readable XP and safe first level-up

- **Date:** 2026-10-05
- **Status:** owner-requested revision after native GDJ-23 play.

The owner initially did not recognize the blue pickups as XP, missed the level-up and discovered its lower-right cards only while looking around after becoming bored. They selected card 1 (Faster fire) and reported that its fire-rate increase was noticeable and worked. Stronger hits remains untested. The v5 log offered the choice at 28.9 seconds and applied it at 48.133 seconds; the earlier scripted 8-second earning time did not represent this human attempt.

Revise the first fixture so level-up freezes physical combat and displays the choice in the center, with unlimited reading time. Preserve independent personal choices and explicit all-roster Continue readiness, including dead members. Network/control processing continues; encounter ticks, firing, damage, return/immunity timers, XP movement/collection and elapsed combat time freeze. This bounded automatic first-level pause brings safe choice access into GDJ-23; the full planned/invoked/banked Zone, extra loadouts, rares and recovery remain GDJ-7/later work.

Make blue pickups explicitly recognizable as XP, show the actual collection distance and add pickup/level sounds. The collection radius remains two metres in this fixture. Replace individual sphere objects with flat camera-facing batched visuals; no measured thousands-of-pickups performance claim follows from this representation change alone.

The owner explicitly clarified XP clumping as **both merging and a gravity-like pulling effect**. Nearby unclaimed pickups may draw together and merge into higher-value carriers. Preserve total XP exactly, original enemy-death deduplication and collector attribution. A death still creates one earned XP before aggregation; merging never awards XP by itself or deletes uncollected value. Larger carriers must be visually distinguishable. New authored enemy/drop values, magnets/player-directed vacuum, loot probabilities and a dense-load performance envelope remain separate.

This supersedes revision 1's live, hideable corner choice and stationary single-value carrier simplifications for the next test. It preserves one rifle, one earned level, the starting combat values, 60 active seconds, seed 137 and solo/two-player scope. It serves perceptible power growth and readable, responsive combat.
