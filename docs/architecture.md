# Repository architecture

## Current shape

The repository separates the game we intend to finish from experiments and the record of how we learned. The first self-contained Unity/C#/URP project lives under `prototypes/001-stacked-weapons/`; `game/` remains reserved for the maintained game after the experiment is evaluated.

| Location | Owns | Placement rule |
| --- | --- | --- |
| `game/` | The chosen game and its runtime assets | Keep one authoritative engine project here. |
| `prototypes/` | Isolated experiments | One named subfolder per experiment, each with a question and result. |
| `assets/` | Editable originals and the asset register | Put runtime exports in the game; track their source here. |
| `docs/design/` | Current intended player experience | Keep the brief aligned with what we are actually building. |
| `docs/decisions/` | Significant decisions and their reasoning | Add a record when a choice is costly to reverse or likely to be questioned later. |
| `docs/devlog/` | Chronological progress and lessons | Use dated entries; link to relevant experiments and decisions. |
| `docs/playtests/` | Session observations and resulting actions | Separate observed behavior from our interpretation. |
| `tools/` | Reusable development helpers | Add a script after a recurring task justifies it. |
| `.github/` | Collaboration templates | Keep them short enough to use. |

## Growing the game folder

The prototype uses Unity's native `Assets/`, `Packages/`, and `ProjectSettings/` layout. Its runtime separates combat simulation, presentation, application/input flow, and benchmark export. Editor utilities own scene/pipeline setup and Windows builds. See [the project code map](../prototypes/001-stacked-weapons/README.md#code-map) and its pinned editor/package files.

Where the engine allows it, keep files for a feature together: for example, a player scene, its behavior, and its specific resources. Create shared areas only when there are actual shared consumers. Keep tests close to their owning project or in the engine's conventional test location.

Do not create separate packages, a shared engine layer, or a generalized plugin system just because they might be useful later.

## Moving from experiment to game

1. Write the experiment's outcome in its README.
2. Choose which behavior or implementation is worth keeping.
3. Move or adapt the selected work into `game/`; verify it there.
4. Mark the original experiment as concluded and identify the maintained version.

The game folder becomes the source of truth for promoted work. Experiments are allowed to be rough and disposable.

## Asset and generated-file boundaries

- Commit source code, engine project settings, required small runtime assets, and reproducibility files.
- Keep editable art and audio originals under `assets/` when practical. Record external storage links if originals are too large for ordinary Git.
- Commit runtime exports under the engine's asset directory; do not make the game depend on an undocumented path outside its project.
- Keep generated builds, caches, logs, and local secrets out of Git. Unity-specific exclusions are in `.gitignore`; source asset metadata is tracked. Build provenance is generated into a Resources asset during export and excluded from source control.
- Decide on large-file storage before committing large binary assets. No Git LFS setup is assumed yet.

## Documentation boundaries

The [project foundation](foundation.md) defines stable purpose, player promise and boundaries; every session reads it first. The planning handoff holds near-term intent and context routing; current state summarizes verified capabilities. The design brief describes the current testable design. Decision records explain why major choices were made and preserve superseded reasoning. The devlog describes the journey. Playtest notes preserve evidence. Link between them instead of copying the same information into every document. See the foundation's authority table and revision process when these records disagree or a proposal changes project direction.
