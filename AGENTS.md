# Working in this repository

- Start with `docs/current-state.md`, then read the task's relevant design/evidence links. `docs/workflow.md` defines our lightweight solo workflow.
- The active Unity project is `prototypes/001-stacked-weapons/`, not the repository root or `game/`. Its README has setup, controls and verification commands.
- Preserve pinned editor/package versions unless the task requests an upgrade. Check uncertain APIs against the pinned version.
- Implement one bounded outcome. Keep changes small enough to inspect, run and understand; avoid speculative systems and unrelated refactors.
- Linear owns live priority/status/dependencies. GitHub owns code, accepted decisions and evidence. Do not mirror the task board in repo checklists.
- Use `tools/unity.ps1` for relevant tests/builds. Serialize Editor/build/test operations for a project; close its Editor before batch runs. Documentation-only changes need link/path/accuracy checks, not a Unity rebuild.
- Use visible release captures for performance conclusions, with equivalent workload counters. Keep reference modes; record source, settings and limits. Run one desktop performance capture at a time.
- Automated checks and screenshots do not establish fun. Clearly separate human play observations, benchmark evidence and unverified assumptions.
- Finish with the commit/PR, checks, reproduction/build details and unresolved findings. Link durable results from the issue; update current state only when relevant facts change.
- Record external asset origin and terms in `assets/README.md`. Never commit credentials or private chat transcripts.
