# Large co-op host publication diagnosis — October 6

Follow-up requested by the owner to [draft PR #16](https://github.com/OpenCnid/game-dev-journey/pull/16), serving the foundation's readable, responsive combat pillar. Starting handoff is `5d3f560a8af02ba2165cdd12d6700d8558bb1352`; recorded runtime is clean `32497266d711d031ae129903b5616a37fa1ea060` / whole `StackLab-GDJ24-v2` build `2026-10-05T09:49:18.0072903Z`. [Previous measurements and preserved failures](2026-10-05-coop-scale.md). This outcome is supported diagnosis and a proposed next experiment, not a performance pass or merge authorization.

## Measurement contract, before instrumentation

**Historical contract, superseded October 6 by [owner direction below](#owner-direction--mvp-progress).** No further captures are planned.

October 6 process check finds no `StackLab.exe` and no Unity Editor. Unity Hub's existing service remains untouched. Owner-controlled games require permission if one appears later. Reuse the attached isolated PR worktree and pinned Unity 6000.3.25f1 / URP 17.3.0 / Transport 2.7.4. Rehash the whole v2 build before reuse; preserve it as the uninstrumented control.

Keep S7: two actual native local UDP processes, 1000 active 60-HP enemies, eight independent instances each, seed 137, fixed combat tuning, exact steering grid, frozen invulnerable diagnostic with conserved collected XP and no offers. Keep 1920×1080 / render scale 1 / current effects / uncapped / VSync off / D3D11 / scripted input / game-owned mute / no preview. Same-machine contention and actual role visibility are declared; overlapping views cannot establish acceptance. All old failures remain.

One fresh uninstrumented v2 S7 pair provides current reproduction. Add observational Stopwatch stage/counter measurements only, then three instrumented S7 pair captures, each 30-second warm-up + 60-second capture, retaining cold data. Ninety seconds is the existing selected-capture duration; enemy/weapon/gameplay rules are unchanged. This is a stage diagnosis, not a complete rotated acceptance comparison. Serialize all native captures and Unity operations.

Measure whole Publish, local authority-to-display copy, snapshot body serialization, packet construction (including chunk copy), queue retention copy, BeginSend, transport byte copy, EndSend, queue flush, driver update and receive dispatch. Keep nested/inclusive stages explicit and use current-update CPU work separately from the preceding frame interval. Also time scripted input/aim and diagnostic output overhead to avoid assigning uncovered frame work to networking. Actual bytes/records, failed Begin/EndSend attempts, flush attempts and snapshot-copy share accompany times.

Preserve the original first-terminal summary and failure gates. Observe both owned processes for at least two more seconds without changing simulation, send order or report behavior. Add a separate bounded post-terminal diagnostic trace/observation: pending message types/bytes/age at first Save, later drain time, late rejection counters and retained physical state. Application queue zero is not proof of transport acknowledgement. No fixed reporting delay or network optimization is implemented as a fix in this investigation.

Validate unchanged serialization/authority/XP/lifecycle with the existing meaningful suites and reliable large-snapshot delivery fixture; add a narrow assertion only if instrumentation reveals a behavior/resource concern. Build and hash one clean instrumented release, reuse it unchanged. Report failing frame/RTT/backlog/attribution conditions, instrumentation overhead and absent GPU/precise allocation data. Propose the smallest next change only after measured attribution and terminal observations.

## Results and Resume

Measurement contract recorded before edits. Next: uninstrumented current reproduction, stage instrumentation, serialized tests/build, three instrumented S7 captures, raw audits, supported explanation and next experiment. PR #16 remains draft; main/current-state unchanged. Human and broader performance acceptance gates remain separate.

## Interrupted checkpoint

Owner stopped Computer Use with physical Escape during R2 window inventory. Work stopped; only verified agent-owned R2 players were closed. Vampire Hunters was left untouched. Instrumented clean release source 140a046a83bbe47aebbebaaca184e6a466ee58db built 2026-10-06T07:19:50.0460414Z; manifest release-manifest-v3.json. EditMode 152/152 and PlayMode 18/18 passed; initial compile failure retained. R1 aborted before warm-up at host tick 1303 with full 1024-record FIFO; after two seconds both queues remained pending (host 1024/client 445). EndSend queue-full host 1797/client 1078, BeginSend queue-full both zero. This differs from the original completed five-minute capture and cannot yet explain its final three pending records. Raw R1 audit is preserved in evidence/gdj-24-publication/r1; R2 interrupted raw remains in ignored artifacts/gdj24-publication-r2. Resume: preserve/audit R2 interruption, finish publication timing analysis, obtain complete terminal observations where feasible and update task/Linear/PR handoff. No fix, gate pass, push or merge performed; diagnostic report remains incomplete.

## Owner direction — MVP progress

October 6: the owner explicitly stopped repeated benchmarking and prioritizes testing whether the game's layers stack together into a viable MVP. Rough implementation and shortcuts are acceptable while building that combined playable proof. The original five-minute run is sufficient to continue MVP work; meeting frame/network targets is not the next feature's entry gate. Preserve the original misses and this failed/interrupted investigation, without claiming a performance pass or automatically resuming captures. This supersedes the unfinished capture plan above. No performance fix, merge or new gameplay feature was performed by this clarification. Existing instrumentation is optional and defaults off.

Final status: stopped, diagnosis inconclusive. The original completed run's pending ending records have not been explained by a complete measured terminal observation. R1 failed with a full retained FIFO; R2 was interrupted by the owner and timed out waiting for reports after its owned players were closed. Both remain evidence, not an MVP acceptance obligation. No StackLab player remains running.
