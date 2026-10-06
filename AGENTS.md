# Working in this repository

## Start each fresh session

1. Read `docs/foundation.md` first (what the game is, its player experience and core systems), then `docs/README.md` (document map), `docs/planning/README.md` (current planning intent) and `docs/current-state.md` (verified capability and constraints). Use `docs/design/README.md` for the GDD chapters and open design gaps.
2. Check the actual checkout/commit and the selected Linear issue or planning question. Read only the relevant branch's design, evidence, source and tests. Use `docs/workflow.md` for working/verification rules; do not load every historical session or report.
3. Resume from the issue's Resume section or linked task plan when present. Connect the task to a foundation pillar or an uncertainty it resolves. If a summary disagrees with newer code/evidence or current user instructions, reconcile it before acting.

Planning/integration and task chats are replaceable sessions. At useful checkpoints and before ending, save decisions/results and any unfinished-task handoff in the relevant files/issue. Update the planning root or current state only when their facts change. Leave enough exact source, checks, remaining work and blockers for a fresh session to continue without the chat.

## Repository rules

- Preserve the game's identity and pillars during ordinary planning and implementation. Use `docs/workflow.md#changing-project-direction` for changes to identity, player promise, agreed constraints or scope; current user authorization already satisfies the owner-decision step. Task briefs and session summaries do not redefine the product.
- The active Unity project is `prototypes/001-stacked-weapons/`, not the repository root or `game/`. Its README has setup, controls and verification commands.
- Preserve pinned editor/package versions unless the task requests an upgrade. Check uncertain APIs against the pinned version.
- Implement one bounded outcome. Keep changes small enough to inspect, run and understand; avoid speculative systems and unrelated refactors.
- Design features for the game's known long-term interactions as well as the immediate slice. Record relevant extension points and downstream effects before implementation; for example, progression upgrades may affect weapons, loot and future upgrade draws. Keep the implementation bounded without hardcoding a known future capability out of the design. See `docs/workflow.md#design-for-the-games-future`.
- Linear owns live priority/status/dependencies. GitHub owns code, accepted decisions and evidence. Do not mirror the task board in repo checklists.
- Follow `docs/documentation.md` for document placement, authority and traceability. Register every new/moved Markdown document in `docs/registry.json` with its purpose and upstream IDs; keep each design/task/evidence chain connected. Regenerate `docs/catalog.md` with `python tools/docs.py --write-index` and run `python tools/docs.py` before handoff. The checker verifies structure, not design acceptance or fun.
- Use `tools/unity.ps1` for relevant tests/builds. Serialize Editor/build/test operations for a project; close its Editor before batch runs. Documentation-only changes need link/path/accuracy checks, not a Unity rebuild.
- Use visible release captures for performance conclusions, with equivalent workload counters. Keep reference modes; record source, settings and limits. Run one desktop performance capture at a time.
- Automated checks and screenshots do not establish fun. Clearly separate human play observations, benchmark evidence and unverified assumptions.
- Finish with the commit/PR, checks, reproduction/build details and unresolved findings. Link durable results from the issue; update current state only when relevant facts change.
- Record external asset origin and terms in `assets/README.md`. Save project-relevant planning summaries; keep credentials and unrelated private conversation out of Git.
