# Large co-op host publication diagnosis — October 6

Follow-up requested by the owner to [draft PR #16](https://github.com/OpenCnid/game-dev-journey/pull/16), serving the foundation's readable, responsive combat pillar. Starting handoff is `5d3f560a8af02ba2165cdd12d6700d8558bb1352`; recorded runtime is clean `32497266d711d031ae129903b5616a37fa1ea060` / whole `StackLab-GDJ24-v2` build `2026-10-05T09:49:18.0072903Z`. [Previous measurements and preserved failures](2026-10-05-coop-scale.md). This outcome is supported diagnosis and a proposed next experiment, not a performance pass or merge authorization.

## Measurement contract, before instrumentation

October 6 process check finds no `StackLab.exe` and no Unity Editor. Unity Hub's existing service remains untouched. Owner-controlled games require permission if one appears later. Reuse the attached isolated PR worktree and pinned Unity 6000.3.25f1 / URP 17.3.0 / Transport 2.7.4. Rehash the whole v2 build before reuse; preserve it as the uninstrumented control.

Keep S7: two actual native local UDP processes, 1000 active 60-HP enemies, eight independent instances each, seed 137, fixed combat tuning, exact steering grid, frozen invulnerable diagnostic with conserved collected XP and no offers. Keep 1920×1080 / render scale 1 / current effects / uncapped / VSync off / D3D11 / scripted input / game-owned mute / no preview. Same-machine contention and actual role visibility are declared; overlapping views cannot establish acceptance. All old failures remain.

One fresh uninstrumented v2 S7 pair provides current reproduction. Add observational Stopwatch stage/counter measurements only, then three instrumented S7 pair captures, each 30-second warm-up + 60-second capture, retaining cold data. Ninety seconds is the existing selected-capture duration; enemy/weapon/gameplay rules are unchanged. This is a stage diagnosis, not a complete rotated acceptance comparison. Serialize all native captures and Unity operations.

Measure whole Publish, local authority-to-display copy, snapshot body serialization, packet construction (including chunk copy), queue retention copy, BeginSend, transport byte copy, EndSend, queue flush, driver update and receive dispatch. Keep nested/inclusive stages explicit and use current-update CPU work separately from the preceding frame interval. Also time scripted input/aim and diagnostic output overhead to avoid assigning uncovered frame work to networking. Actual bytes/records, failed Begin/EndSend attempts, flush attempts and snapshot-copy share accompany times.

Preserve the original first-terminal summary and failure gates. Observe both owned processes for at least two more seconds without changing simulation, send order or report behavior. Add a separate bounded post-terminal diagnostic trace/observation: pending message types/bytes/age at first Save, later drain time, late rejection counters and retained physical state. Application queue zero is not proof of transport acknowledgement. No fixed reporting delay or network optimization is implemented as a fix in this investigation.

Validate unchanged serialization/authority/XP/lifecycle with the existing meaningful suites and reliable large-snapshot delivery fixture; add a narrow assertion only if instrumentation reveals a behavior/resource concern. Build and hash one clean instrumented release, reuse it unchanged. Report failing frame/RTT/backlog/attribution conditions, instrumentation overhead and absent GPU/precise allocation data. Propose the smallest next change only after measured attribution and terminal observations.

## Results and Resume

Measurement contract recorded before edits. Next: uninstrumented current reproduction, stage instrumentation, serialized tests/build, three instrumented S7 captures, raw audits, supported explanation and next experiment. PR #16 remains draft; main/current-state unchanged. Human and broader performance acceptance gates remain separate.
