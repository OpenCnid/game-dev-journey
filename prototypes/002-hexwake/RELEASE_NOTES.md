# Hexwake v0.1.0

The first shared Windows build of **Hexwake**, a first-person horde-survival
prototype with a six-gun arsenal, strange relics, and a ten-minute survival run.

## Play

Download **Hexwake-v0.1.0-windows-x64.zip**, extract the entire folder and run
**Hexwake.exe**. Choose a starter and click **Begin**. No Unity installation is
required. Windows x64, single-player, keyboard and mouse.

WASD to move, mouse to aim, Space to hop, Shift for mobility, E for gun/relic
loot, F for automatic/manual fire, R to reload, and Escape for frozen inventory.
The repository README has the full controls and a gameplay screenshot.

## In this build

- Six independently firing weapon slots, including duplicate gun copies.
- Sixteen distinct gun identities, enemy-dropped gear and six relic types.
- Simple upgrade gain/cost cards, visible gun/relic pickups and frozen inventory.
- A dark fantasy arena, pursuit pressure and class-specific movement abilities.

Published from the owner-playtested J/R6 game. Changes for publication are the
Hexwake window/menu title and package naming, not a gameplay revision.

The named build passed 146 engine regressions and 18 actual-hit presentation
checks at each of 1280x720 and 1920x800. All 212 extracted package files matched
the build manifest. See `prototypes/002-hexwake/VERIFICATION.md` for limits.

`SHA256SUMS.txt` verifies the ZIP; `Hexwake-v0.1.0-manifest.json` records relative
source/package hashes. The separate C# source snapshot documents its retained
Unity 6000.6.3f1 Mono-player rebuild route; it is not a fresh Editor project export.

This is an early solo prototype, not a finished game or multiplayer release.
Download access follows this repository's existing private permissions.
