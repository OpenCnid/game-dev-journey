# Development tools

Put small reusable scripts here when a repeated task warrants automation: for example, asset validation or a build helper after an engine is chosen.

Each tool should document its purpose, prerequisites, invocation, and output. Prefer the engine's existing tools where they already solve the problem.

## Unity prototype helpers

- `unity.ps1`: prepare the pinned Unity project, run Edit Mode or Play Mode tests, build the Windows player, or open the editor. Requires an installed, licensed Unity 6000.3.25f1 editor. See [project instructions](../prototypes/001-stacked-weapons/README.md).
- `benchmark.ps1`: run the built player at repeatable crowd loads and check that each repeat writes a report. `-RifleRate` selects 0.5–120 shots/sec for slow-to-continuous firing tests (default 8.33/s); invocation and JSON record the rate, and the helper verifies rate/interval agreement. Default historical players remain supported; custom rates require a player that reports them. `-Smoke` uses short captures to check the reporting path. Full captures and smoke captures serve different purposes.
- `summarize-benchmarks.ps1`: list report metrics with their rifle rate (historical fixed-rate reports use the 8.33/s baseline). Compare equal rates and workloads; changed firing intensity changes the benchmark's work.

Run helpers from PowerShell. Each helper resolves the project relative to its own location; generated outputs remain under the prototype unless an explicit output path is supplied.

## Documentation harness

`docs.py` validates [the document registry](../docs/registry.json), canonical locations, trace IDs, UTF-8 Markdown, local references, supporting-file ownership and the generated catalogue. It uses Git's tracked/non-ignored file inventory and Python 3.10+ standard library; no Unity or network operation is involved. A structural pass does not validate design decisions or evidence claims.

```powershell
python tools/docs.py --write-index
python tools/docs.py
python tools/docs.py --trace task-gdj-23
python -m unittest discover -s tools/tests -p test_docs.py
```

If `python` is not on PATH in Codex's Windows workspace, use `& "$env:USERPROFILE/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe"` in its place. Commands can run from the repo root; the checker itself resolves paths from its script location. Exit 1 identifies structural failures; exit 0 reports document/reference/resource counts. Follow the [authoring contract](../docs/documentation.md#authoring-and-handoff-protocol) to fix a failure. Templates include the required trace fields. External URL availability and semantic consistency need separate review.
