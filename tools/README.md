# Development tools

Put small reusable scripts here when a repeated task warrants automation: for example, asset validation or a build helper after an engine is chosen.

Each tool should document its purpose, prerequisites, invocation, and output. Prefer the engine's existing tools where they already solve the problem.

## Unity prototype helpers

- `unity.ps1`: prepare the pinned Unity project, run Edit Mode or Play Mode tests, build the Windows player, or open the editor. Requires an installed, licensed Unity 6000.3.25f1 editor. See [project instructions](../prototypes/001-stacked-weapons/README.md).
- `benchmark.ps1`: run the built player at repeatable crowd loads and check that each repeat writes a report. `-RifleRate` selects 0.5–120 shots/sec for slow-to-continuous firing tests (default 8.33/s); invocation and JSON record the rate, and the helper verifies rate/interval agreement. Default historical players remain supported; custom rates require a player that reports them. `-Smoke` uses short captures to check the reporting path. Full captures and smoke captures serve different purposes.
- `summarize-benchmarks.ps1`: list report metrics with their rifle rate (historical fixed-rate reports use the 8.33/s baseline). Compare equal rates and workloads; changed firing intensity changes the benchmark's work.

Run helpers from PowerShell. Each helper resolves the project relative to its own location; generated outputs remain under the prototype unless an explicit output path is supplied.
