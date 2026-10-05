# Asset sources and provenance

Store editable originals here as needed, using descriptive folders such as `art/`, `audio/`, and `fonts/`. Create those folders when we have assets for them.

Export game-ready files into the selected engine project's own asset directory under `game/`. Avoid duplicate runtime copies with unclear ownership.

## Asset register

Add a row for each external or generated asset we keep. Group an asset pack into one row if it shares a source and terms. Save required attribution text or license files alongside the source asset when permitted.

| Asset or pack | Source / creator | License or usage terms | Changes / generation notes | Runtime path |
| --- | --- | --- | --- | --- |

The first prototype uses original runtime-generated primitive geometry, flat materials and synthesized shot/pickup/level-up sounds implemented in `ArenaPresentation.cs`. October 5's XP discs/range ring are generated in `XpPresentation.cs`; the weapon-stack experiment's orange/purple trails reuse original procedural materials in `SharedGame.cs`. No external art or audio is imported. These are repository-original procedural assets created with Codex assistance. Validation screenshots in `docs/playtests/evidence/` are captured from our own prototype. Editable external asset sources will be registered here when introduced.

For generated assets, record the tool/model if known, generation date, a useful prompt summary, and the applicable usage terms. For original work, identify the creator. Check large-file storage needs before adding large audio or art source files.
