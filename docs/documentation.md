# Documentation contract

Established October 6, 2026 for [GDJ-26](https://linear.app/cnid/issue/GDJ-26/consolidate-the-living-gdd-and-ai-assisted-design-workflow), following the owner's request for a stricter GDD harness with explicit document locations, purposes and traceability. This is a repository process rule. It does not approve new gameplay decisions.

## One entry point and one registry

[docs/README.md](README.md) is the human/AI entry point after the foundation. [registry.json](registry.json) is the complete machine-readable document register; [catalog.md](catalog.md) is generated from it. Every repository Markdown file, including templates and historical records, must have exactly one registry entry. Every non-Markdown file under `docs/`, except the registry itself, belongs to exactly one supporting-file group with a registered owner. Local ignored builds/caches and game source are outside this document inventory.

Each entry has a stable `id`, exact repository-relative `path`, `kind`, short `purpose`, document `maintenance` state and `upstream` IDs. Keep the ID when moving a document. An upstream reference means that the document derives context or rationale from that record; it does not assert that every statement there is accepted or current. Reverse consumers are derived automatically. The register contains no Linear status, copied build results or second copy of design rules.

## Locations and responsibilities

| Material | Required home | Authority and reason |
| --- | --- | --- |
| Document navigation | `docs/README.md`, `docs/catalog.md`, topic `README.md` files | Route readers; do not restate changing product rules. |
| Product identity and pillars | `docs/foundation.md` | The accepted product definition. Keep one authoritative version. |
| GDD chapter map | `docs/design/README.md` | Names chapters, current sources and gaps before detailed planning. |
| Player-facing system/content design | `docs/design/<topic>.md` | Intended behavior, interactions, alternatives and open choices. Use [the design template](design/template.md). |
| Consequential accepted/proposed decisions | `docs/decisions/NNNN-<topic>.md` | Why a choice was made, who authorized it and what it supersedes. Use [the decision template](decisions/template.md). |
| Technical designs | `docs/technical/<topic>.md` for new records | Implementation ownership, protocols and constraints. Existing `architecture.md`, `performance.md`, `profiling.md`, `design/multiplayer-boundaries.md` and `design/late-game-workload.md` retain their registered paths for link continuity. |
| Construction sequence | `docs/roadmap.md` | Milestones and dependencies in the intended game; no live task checklist. |
| Immediate planning intent | `docs/planning/README.md` | Selected next outcome and context routing. Date superseded intent clearly. |
| Bounded task and Resume | `docs/planning/tasks/GDJ-N.md`, linked from the Linear issue | Design basis, exact starting revision, scope, acceptance, implementation note and handoff. Short tasks can keep their full plan in Linear, with source/design/evidence links there. |
| Verified capability summary | `docs/current-state.md` | Link to the exact source/evidence and distinguish main from candidates. Historical checkpoints do not override newer evidence. |
| Test/play evidence | `docs/playtests/YYYY-MM-DD-<topic>.md` | Reproduction, observed results and limits; separate automation from human observations. |
| Raw evidence | `docs/playtests/evidence/<experiment>/` | Files owned and explained by a dated report. Register a supporting-file group; Markdown notes still get entries. |
| Historical development/process notes | `docs/devlog/YYYY-MM-DD-<topic>.md`, dated planning/session records | Past reasoning and learning; never current product authority. |
| Session/process rules and reusable prompts | `AGENTS.md`, `docs/workflow.md`, this contract, `docs/planning/start-*-session.md` | Tell sessions how to find, interpret and update the records. |
| Build/setup commands and asset provenance | Project/tool `README.md` files and `assets/README.md` | Keep operational instructions beside their project/tool and asset terms beside their register. |

The registry's `kinds.paths` enforces these locations, including named legacy exceptions. A new category or exception requires an explicit reason in this contract and the registry. Existing files are classified, not silently promoted into current design. The active game project stays at `prototypes/001-stacked-weapons/`; `game/` is a placeholder without an authorized migration.

## Authority and conflicts

Current owner direction takes precedence. Record consequential product changes using [changing project direction](workflow.md#changing-project-direction), updating the foundation and affected design/decision records together. Routine implementation details remain within the selected task.

- Product questions: consult the foundation and accepted, dated decisions; use the relevant GDD chapter for detail. Reconcile an apparent conflict before basing new work on it.
- Implementation questions: inspect the actual branch/source, pinned configuration and dated evidence. A GDD describes intention; a task marked complete does not prove every product ambition is implemented.
- Scheduling questions: consult Linear. The roadmap, registry and catalogue cannot override its live status/dependencies.
- History and proposals: preserve their reasoning and dates. A newer AI suggestion, working prototype or successful automated check does not by itself constitute owner acceptance.

The `maintenance` field classifies the document: `current` (maintained reference), `draft` (unfinished proposal), `mixed` (accepted direction plus proposals/historical sections), `historical` (dated record), or `template` (authoring form). It never means a game feature is approved, built or verified. Record design status, MVP/later scope and implementation/play evidence separately in the document.

## Traceability required before implementation

A task must identify the player outcome, foundation/GDD section, relevant accepted decisions or explicitly proposed test hypothesis, affected system interactions and exact source base. Its registry `upstream` points to those design/context records. A new system needs a design home before a feature brief can quietly become its permanent specification.

The handoff links the implementation commit/PR and evidence record back to that task. The report names source/build/configuration, checks and unverified claims. Link the report from Linear and current state when capability facts change. After owner feedback changes an accepted rule, update the decision/GDD chapter and impacted plans. Register the new trace edges in the same change.

The trace is **intent/decision → design → task → source and evidence → updated design/current state**. Feedback can produce reference cycles; the trace command visits each record once. Dates and immutable commit links preserve chronology. Cross-links explain relationships, not automatic approvals.

## Authoring and handoff protocol

1. Find the relevant document in the map/catalogue and inspect its upstream basis. Amend its existing home when it owns the subject.
2. For new material, choose its kind and required location. Add a registry entry with a concrete purpose and upstream IDs. Use the relevant template; mark unresolved creative choices explicitly.
3. Maintain links from decisions to affected designs and from task/evidence records to their context. For a move, preserve the ID and update all incoming paths in the same change. Mark superseded reasoning instead of deleting historical evidence.
4. Run `python tools/docs.py --write-index`, then `python tools/docs.py`. Commit the registry, catalogue and affected documents together. The standard library checker needs Python 3.10+ and Git; [tools guide](../tools/README.md#documentation-harness) records invocation on this machine.
5. Save exact revision, changed facts, checks, open decisions and next action in the issue Resume. The owner reviews creative choices; AI handles document upkeep and routine verification.

Validation fails for unregistered Markdown, duplicate IDs/paths, wrong locations, missing purposes/upstream records, missing files, non-UTF-8 Markdown, unowned/ambiguously owned supporting files, broken local Markdown links/anchors and a stale generated catalogue. It checks inline Markdown links and reference definitions outside fenced examples. External URLs, semantic contradictions, source claims, acceptance and fun require review; a structural pass does not certify them. There is no automatic hosted CI gate yet; running the checker is required by AGENTS.md and the PR template.

## Current migration boundary

Existing design records contain accepted rules, unchosen alternatives and superseded fixtures. They remain registered as `mixed`, `draft` or `historical`; [the GDD map](design/README.md) exposes their unresolved chapter work. Registration makes them discoverable, not complete. New or substantively revised designs use the template's status/interaction/trace fields. Older dated reports retain their format and results.

The harness is ready when navigation, placement, trace edges and structural checks work. Writing a coherent representative run and settling remaining creative decisions is the subsequent GDD content work under GDJ-26.
