# GDJ-26: GDD structure and traceability

[Linear issue](https://linear.app/cnid/issue/GDJ-26/consolidate-the-living-gdd-and-ai-assisted-design-workflow) owns live status. This task starts from `43a89dd9dae5a30796f459a155cd2180ba4d425d` on `docs/gdd-design-first`, above the GDJ-25 candidate branch. The owner first requested a GDD, then clarified that the immediate need is a strict document harness naming where records live, why they exist and how a plan traces to them.

## Outcome and scope

Make the intended game and the rationale for each piece of work recoverable from the repository without chat history. This supports all foundation pillars through coherent design/planning; it adds no gameplay behavior or product decision.

The structural checkpoint is a [document map](../../README.md), [placement/authority contract](../../documentation.md), complete [registry](../../registry.json) and generated [catalogue](../../catalog.md), a [GDD chapter map](../../design/README.md), session/templates and a local structural checker. Preserve existing evidence and separate historical/mixed material from maintained references. Correct conflicting entry-point routing as part of the migration.

## Acceptance and handoff

- Every repository Markdown document has exactly one registered home, purpose, kind and maintenance classification; design/tasks/evidence name upstream records.
- Every supporting file under docs has one registered owning report/guide. The readable catalogue and trace command locate upstream rationale and downstream consumers.
- Local document paths/anchors and generated catalogue pass validation. Negative fixtures demonstrate detection of unregistered/misplaced files, duplicate IDs, broken links/anchors, missing upstream basis and ambiguous resource ownership.
- A fresh session follows AGENTS → foundation → document map → current plan/state → relevant GDD/decision → task/evidence. Registry classification does not assert approval, implementation or fun.
- Save exact commit, checks, reproduction and remaining content work in Linear. No Unity build is needed for this documentation/tooling change.

## Resume

The structural harness is implemented in the orchestrator session: all 107 Markdown files are registered, 827 supporting files have owners, the catalogue is generated and 14 fault-injection/positive checks pass. The local-link/anchor and stale-catalogue audit passes. Reproduce with `python tools/docs.py` and `python -m unittest discover -s tools/tests -p test_docs.py`; use `python tools/docs.py --trace task-gdj-23` for a real trace. The issue Resume records the exact delivered commit/PR.

Entry-point guidance now agrees on the active Unity location. One historical report's isolated CP1252 dash bytes were normalized to UTF-8; measured numbers/results are unchanged. The checker verifies document structure locally, with no hosted CI gate or semantic-design certification.

The GDD chapter map identifies existing homes and design gaps; a representative complete run, coherent encounter pacing and presentation choices still need design with the owner. This checkpoint must not be reported as a finished GDD. GDJ-25's candidate and unmerged PR dependency remain intact; no other chat is launched here. Next: use the design-session prompt to develop the representative-run chapter from accepted direction, preserving unresolved choices and updating trace links.
