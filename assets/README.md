# Asset sources and provenance

Store editable originals here as needed, using descriptive folders such as `art/`, `audio/`, and `fonts/`. Create those folders when we have assets for them.

Export game-ready files into the selected engine project's own asset directory under `game/`. Avoid duplicate runtime copies with unclear ownership.

## Asset register

Add a row for each external or generated asset we keep. Group an asset pack into one row if it shares a source and terms. Save required attribution text or license files alongside the source asset when permitted.

| Asset or pack | Source / creator | License or usage terms | Changes / generation notes | Runtime path |
| --- | --- | --- | --- | --- |

The first prototype uses runtime-generated primitive geometry, flat materials, and a synthesized pulse sound implemented in `ArenaPresentation.cs`. No external art or audio is imported. The validation screenshot in `docs/playtests/evidence/` is captured from our own prototype. Editable external asset sources will be registered here when introduced.

For generated assets, record the tool/model if known, generation date, a useful prompt summary, and the applicable usage terms. For original work, identify the creator. Check large-file storage needs before adding large audio or art source files.
