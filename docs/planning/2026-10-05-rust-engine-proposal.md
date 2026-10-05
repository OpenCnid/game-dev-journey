# Rust engine proposal — deferred

October 5, 2026 (America/Chicago). **The owner has chosen to focus on Unity for now.** The Rust proposal and suggested comparison are deferred. The earlier discussion below is retained as context, not scheduled work.

## Owner decision

After considering the assessment, the owner says: "No we should just focus on unity for now. I don't know enough about game dev and Rust to guide you through building it." Continue the accepted Unity/C#/URP approach and GDJ-23's earned-growth implementation. This supersedes the recommendation to evaluate Rust after that checkpoint. No Rust comparison, migration or new task is selected. The engine decision and foundation already match this direction and require no product-definition change.

The owner's original proposal concerned AI-assisted coding, fast iteration and runtime scalability, suggesting AI may remove the need to personally read implementation code. The assessment below preserves the technical considerations for a future owner-requested revisit.

## Evidence at the discussion checkpoint

[Foundation revision 18](../foundation.md) and [decision 0001](../decisions/0001-engine-recommendation.md) select Unity/C#/URP for the first prototype, with explicit revisit conditions. Merged main remains `bd2bd1438b1029426e2c0234ff640502dc0e25ea`; the local planning branch includes the GDJ-23 dispatch brief `f5263aa067e6c8caf8656d7c34ea3e2065d0ed66`. Our relevant comparison is Unity/C# versus a Rust engine and its workflow.

[GDJ-23](https://linear.app/cnid/issue/GDJ-23/implement-the-first-earned-level-up-in-solo-and-co-op) remains In Progress. A compact snapshot of worker chat `01a10a3b-29f3-74a3-a7a2-f75e9d4e5717` reports 126 Edit Mode / 14 Play Mode passes and a v5 build for owner retest. Those are worker-reported progress, not independently reviewed final acceptance or merged capability. Its exact final source/build must be established before using it as a new comparison baseline. Main's [current-state evidence](../current-state.md) establishes existing bounded Unity capabilities; neither representative cross-engine comparison nor a demonstrated Unity engine limit exists here. Local synthetic crowd results and small loopback checks do not prove dense multiplayer scalability.

## Earlier assessment

Rust is a credible candidate. Ownership/type checks and the absence of a garbage collector offer useful control over memory and concurrency; compiler feedback can constrain AI-generated implementations. These are language properties, not measured gains for our game. [Rust overview](https://rust-lang.org/), [concurrency documentation](https://doc.rust-lang.org/book/ch16-00-concurrency.html).

AI can reduce manual implementation and review effort, but tests sample specified behavior and may share a mistaken assumption with generated code. We still need understandable state ownership, source review, profiling and human play observations. Safe Rust excludes data races while still allowing general race conditions, deadlocks and logic errors. Duplicate XP, stale-run acceptance or dropping valid feedback at Retry require explicit design and verification in either language. [Rustonomicon](https://doc.rust-lang.org/nomicon/races.html).

Bevy is a plausible first candidate for a code-oriented ECS workflow with parallel scheduling and 3D rendering. Its current guide warns of missing features, sparse documentation and breaking API releases approximately every three months; its setup guide documents compile/link tradeoffs and faster incremental-build configurations. Engine/package maturity and asset, UI, debugging, networking and packaging workflows must be included in the assessment. No claim of faster AI development, frame times or networking follows from the language choice. [Bevy features](https://bevy.org/), [stability guidance](https://bevy.org/learn/quick-start/introduction/), [build setup](https://bevy.org/learn/quick-start/getting-started/setup/). Sources checked October 5.

## Unselected comparison idea

The earlier recommendation was to preserve GDJ-23's candidate and gather its intended growth feedback, then consider one bounded Rust/Bevy comparison while retaining the Unity control. The owner has now deferred that investigation. If explicitly revisited later, the proposed fixture was the existing 60-second, 12-drone shared-rifle encounter: two actual local players, aiming/hits, authoritative enemy state, death/return, shared ending and Retry. Use comparable visible presentation and the same event/behavior checks. A headless simulation benchmark alone cannot decide a client engine.

Measure clean setup/build, small-edit-to-playable time, test/fix cycles, release packaging, comparable frame-time distributions/memory and network correctness. Include equivalent visible workload and a staged higher-count case only where both implementations perform the same work. Name missing tools/assets, estimated remaining port effort and a concrete next gameplay change to compare authoring friction. Set decision targets before implementation; improve iteration or a demonstrated runtime constraint enough to justify the remaining port and maintenance cost. A tiny-scene win cannot establish the >10,000-enemy cooperative goal.

This would resolve a delivery uncertainty for perceptible growth and readable, responsive combat. It would not change the game's pillars or finish networking research. A successful comparison can justify switching while the product is small; prior effort alone is not a reason to keep Unity. An inconclusive result leaves no demonstrated benefit for a rewrite.

## Resume

Owner decision recorded: continue Unity and defer Rust. No Rust code, package installation, benchmark, new Linear ticket, chat dispatch, board change or interruption of GDJ-23 resulted from this discussion. The next action remains GDJ-23's implementation/playtest/handoff under its existing scope; check its live Linear Resume for progress. Unity's accepted decision and the foundation are unchanged. Reopen this investigation only on renewed owner direction. Any later accepted migration would follow [changing project direction](../workflow.md#changing-project-direction), preserving contracts, builds and evidence as controls.
