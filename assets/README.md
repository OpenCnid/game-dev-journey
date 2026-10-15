# Asset sources and provenance

Store editable originals here as needed, using descriptive folders such as `art/`, `audio/`, and `fonts/`. Create those folders when we have assets for them.

Export game-ready files into the selected engine project's own asset directory under `game/`. Avoid duplicate runtime copies with unclear ownership.

## Asset register

Add a row for each external or generated asset we keep. Group an asset pack into one row if it shares a source and terms. Save required attribution text or license files alongside the source asset when permitted.

| Asset or pack | Source / creator | License or usage terms | Changes / generation notes | Runtime path |
| --- | --- | --- | --- | --- |
| Hexwake retained sky, rock and material resources | Poly Haven; individual sources/authors in [resource notices](../prototypes/002-hexwake/THIRD_PARTY_NOTICES.md) | CC0-1.0; links and notice retained with release | Recolored fantasy ivory/hide; inherited streamed resource bundles, including some unused resources | Release `Hexwake_Data/StreamingAssets/FantasyTrialB/` and `InfernalRealism/` |
| Hexwake procedural arena, enemies, guns, loot glyphs and generated effects | Repository prototype work with AI assistance | Project-original; no project-wide open-source license selected | C# source snapshot and original generated geometry/materials | [Hexwake source](../prototypes/002-hexwake/source/) |
| Hexwake landing-page screenshot | Captured from our J/R6 player | Project gameplay capture | Actual 1920x800 loot-approach scenario; no generated promotional artwork | [Gameplay screenshot](hexwake/gameplay.png) |

The first prototype uses original runtime-generated primitive geometry, flat materials and synthesized shot/pickup/level-up sounds implemented in `ArenaPresentation.cs`. October 5's XP discs/range ring are generated in `XpPresentation.cs`; the weapon-stack experiment's orange/purple trails reuse original procedural materials in `SharedGame.cs`. That earlier prototype imports no external art or audio; Hexwake's later retained resources are registered above. The earlier procedural assets are repository-original work created with Codex assistance. Validation screenshots in `docs/playtests/evidence/` are captured from our own prototype.

For generated assets, record the tool/model if known, generation date, a useful prompt summary, and the applicable usage terms. For original work, identify the creator. Check large-file storage needs before adding large audio or art source files.
