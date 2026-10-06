# Document catalogue

Generated from [registry.json](registry.json) by `python tools/docs.py --write-index`. Edit the registry, then regenerate this file.

[Start here](README.md) · [Placement and traceability rules](documentation.md) · [GDD chapters](design/README.md)

Maintenance describes the document, not product approval or Linear status. Upstream links explain its basis; the command `python tools/docs.py --trace <id>` also shows downstream consumers. Read dates, sections and source revisions before treating a historical rule as current.

## index

Navigation into owned records; indexing does not grant product authority.

| ID and document | Why it exists | Maintenance | Upstream basis |
| --- | --- | --- | --- |
| [catalog](catalog.md) | Generated complete inventory with document purposes and upstream links. | current | Entry / original record |
| [decision-index](decisions/README.md) | Locate significant choices and their rationale. | current | Entry / original record |
| [document-map](README.md) | Locate authoritative answers and show how to trace from intention to evidence. | current | Entry / original record |
| [evidence-index](playtests/README.md) | Explain how to record observations and locate dated reports. | current | Entry / original record |
| [gdd-index](design/README.md) | Map GDD chapters to existing homes and expose missing connected design. | current | [foundation](foundation.md) |
| [journal-index](devlog/README.md) | Locate the historical development journal. | current | Entry / original record |

## foundation

Accepted game identity, player promise, pillars and boundaries.

| ID and document | Why it exists | Maintenance | Upstream basis |
| --- | --- | --- | --- |
| [foundation](foundation.md) | Own the accepted game definition while linking its decision history. | current | [decision-0001](decisions/0001-engine-recommendation.md), [decision-0002](decisions/0002-cooperative-pve-ai-world.md), [decision-0003](decisions/0003-shared-xp-personal-upgrade-draws.md), [decision-0004](decisions/0004-party-thinking-space.md), [decision-0005](decisions/0005-technology-magic-and-cultivation.md), [decision-0006](decisions/0006-capacity-mastery-and-prepared-power.md), [decision-0007](decisions/0007-roguelike-run-identity-and-camera-exploration.md), [decision-0008](decisions/0008-bullet-heaven-solo-play-and-core-experience.md), [decision-0009](decisions/0009-weapon-control-and-accessible-combat.md), [decision-0010](decisions/0010-xp-pickups-and-death-participation.md), [decision-0011](decisions/0011-weapon-traits-and-upgrade-eligibility.md), [decision-0012](decisions/0012-disconnect-recovery-and-preserved-return.md), [decision-0013](decisions/0013-readable-xp-and-safe-first-level-up.md) |

## design

Player-facing rules, interactions, alternatives and open design questions.

| ID and document | Why it exists | Maintenance | Upstream basis |
| --- | --- | --- | --- |
| [design-ai-world-options](design/ai-world-options.md) | Compare setting alternatives and preserve the selected Engine Kingdoms premise. | mixed | [foundation](foundation.md), [decision-0002](decisions/0002-cooperative-pve-ai-world.md) |
| [design-cooperative-session-recovery](design/cooperative-session-recovery.md) | Separate accepted disconnect allowance from proposed recovery/lifecycle policies. | mixed | [foundation](foundation.md), [decision-0012](decisions/0012-disconnect-recovery-and-preserved-return.md), [decision-0004](decisions/0004-party-thinking-space.md) |
| [design-crosshair-customization-and-imports](design/crosshair-customization-and-imports.md) | Specify proposed crosshair customization and assess import compatibility. | draft | [foundation](foundation.md), [design-weapon-control-and-combat-readability](design/weapon-control-and-combat-readability.md) |
| [design-engine-kingdoms-first-encounter](design/engine-kingdoms-first-encounter.md) | Preserve world/encounter design discussion and route to accepted test rules. | mixed | [foundation](foundation.md), [decision-0002](decisions/0002-cooperative-pve-ai-world.md), [decision-0005](decisions/0005-technology-magic-and-cultivation.md) |
| [design-first-earned-level-up](design/first-earned-level-up.md) | Specify the accepted first growth fixture and preserve later supersession context. | mixed | [foundation](foundation.md), [decision-0003](decisions/0003-shared-xp-personal-upgrade-draws.md), [decision-0010](decisions/0010-xp-pickups-and-death-participation.md), [decision-0011](decisions/0011-weapon-traits-and-upgrade-eligibility.md) |
| [design-game-brief](design/game-brief.md) | Preserve early experiment proposals with explicit current-status routing. | mixed | [foundation](foundation.md) |
| [design-permanent-progression-matrix](design/permanent-progression-matrix.md) | Explore the parked permanent-matrix idea without selecting MVP scope. | draft | [foundation](foundation.md), [decision-0007](decisions/0007-roguelike-run-identity-and-camera-exploration.md) |
| [design-progression-and-upgrade-pools](design/progression-and-upgrade-pools.md) | Own accepted progression direction and distinguish unsettled draw/grant rules. | mixed | [foundation](foundation.md), [decision-0003](decisions/0003-shared-xp-personal-upgrade-draws.md), [decision-0010](decisions/0010-xp-pickups-and-death-participation.md), [decision-0011](decisions/0011-weapon-traits-and-upgrade-eligibility.md) |
| [design-upgrade-selection-accessibility](design/upgrade-selection-accessibility.md) | Preserve proposed comparisons of choice access, time pressure and party needs. | draft | [foundation](foundation.md), [decision-0004](decisions/0004-party-thinking-space.md), [design-progression-and-upgrade-pools](design/progression-and-upgrade-pools.md) |
| [design-vampire-hunters-study](design/vampire-hunters-study.md) | Preserve cited reference observations and design hypotheses, not product commitments. | mixed | [foundation](foundation.md) |
| [design-weapon-control-and-combat-readability](design/weapon-control-and-combat-readability.md) | Connect accepted firing/targeting/readability direction to proposed controls. | mixed | [foundation](foundation.md), [decision-0009](decisions/0009-weapon-control-and-accessible-combat.md) |

## decision

Dated consequential choices, authorization, rationale and supersession.

| ID and document | Why it exists | Maintenance | Upstream basis |
| --- | --- | --- | --- |
| [decision-0001](decisions/0001-engine-recommendation.md) | Preserve the owner decision and consequences: Use Unity and C# for the first prototype. | mixed | Entry / original record |
| [decision-0002](decisions/0002-cooperative-pve-ai-world.md) | Preserve the owner decision and consequences: Cooperative PvE in an AI-controlled world. | mixed | Entry / original record |
| [decision-0003](decisions/0003-shared-xp-personal-upgrade-draws.md) | Preserve the owner decision and consequences: Shared XP and personal upgrade draws. | mixed | Entry / original record |
| [decision-0004](decisions/0004-party-thinking-space.md) | Preserve the owner decision and consequences: Party thinking space for cooperative choices. | mixed | Entry / original record |
| [decision-0005](decisions/0005-technology-magic-and-cultivation.md) | Preserve the owner decision and consequences: Technology magic and cultivation in weapon builds. | mixed | Entry / original record |
| [decision-0006](decisions/0006-capacity-mastery-and-prepared-power.md) | Preserve the owner decision and consequences: Capacity mastery and prepared power for hybrid guns. | mixed | Entry / original record |
| [decision-0007](decisions/0007-roguelike-run-identity-and-camera-exploration.md) | Preserve the owner decision and consequences: Roguelike identity permanent discovery and camera exploration. | mixed | Entry / original record |
| [decision-0008](decisions/0008-bullet-heaven-solo-play-and-core-experience.md) | Preserve the owner decision and consequences: Bullet heaven solo play and the core experience. | mixed | Entry / original record |
| [decision-0009](decisions/0009-weapon-control-and-accessible-combat.md) | Preserve the owner decision and consequences: Weapon control and accessible combat. | mixed | Entry / original record |
| [decision-0010](decisions/0010-xp-pickups-and-death-participation.md) | Preserve the owner decision and consequences: XP pickups and death participation. | mixed | Entry / original record |
| [decision-0011](decisions/0011-weapon-traits-and-upgrade-eligibility.md) | Preserve the owner decision and consequences: Weapon traits and upgrade eligibility. | mixed | Entry / original record |
| [decision-0012](decisions/0012-disconnect-recovery-and-preserved-return.md) | Preserve the owner decision and consequences: Disconnect recovery and preserved return. | mixed | Entry / original record |
| [decision-0013](decisions/0013-readable-xp-and-safe-first-level-up.md) | Preserve the owner decision and consequences: Readable XP and safe first level-up. | mixed | [evidence-earned-growth](playtests/2026-10-05-earned-growth.md) |

## planning

Near-term intent and construction sequence; Linear owns live scheduling.

| ID and document | Why it exists | Maintenance | Upstream basis |
| --- | --- | --- | --- |
| [planning](planning/README.md) | Preserve current intent and route sessions to relevant context branches. | current | [foundation](foundation.md), [gdd-index](design/README.md), [current-state](current-state.md) |
| [roadmap](roadmap.md) | Connect proposed MVP milestones to the accepted game and current design priority. | current | [foundation](foundation.md), [gdd-index](design/README.md), [current-state](current-state.md) |

## task

Bounded work, its design basis, acceptance and durable Resume.

| ID and document | Why it exists | Maintenance | Upstream basis |
| --- | --- | --- | --- |
| [task-gdj-15](planning/tasks/GDJ-15.md) | Bounded outcome, context, acceptance and handoff: immediate rifle feedback during fast turns. | mixed | [design-weapon-control-and-combat-readability](design/weapon-control-and-combat-readability.md), [evidence-rifle-baseline](playtests/2026-10-03-rifle-baseline.md) |
| [task-gdj-16](planning/tasks/GDJ-16.md) | Bounded outcome, context, acceptance and handoff: readable damage in the rifle arena. | mixed | [design-weapon-control-and-combat-readability](design/weapon-control-and-combat-readability.md), [evidence-rifle-baseline](playtests/2026-10-03-rifle-baseline.md) |
| [task-gdj-17](planning/tasks/GDJ-17.md) | Bounded outcome, context, acceptance and handoff: precision aim and critical head hits. | mixed | [design-weapon-control-and-combat-readability](design/weapon-control-and-combat-readability.md), [evidence-overhead-health](playtests/2026-10-03-overhead-health.md) |
| [task-gdj-19](planning/tasks/GDJ-19.md) | Bounded outcome, context, acceptance and handoff: rifle fire-rate controls for testing. | mixed | [design-weapon-control-and-combat-readability](design/weapon-control-and-combat-readability.md) |
| [task-gdj-2](planning/tasks/GDJ-2.md) | Bounded outcome, context, acceptance and handoff: observe the current rifle before expansion. | mixed | [foundation](foundation.md) |
| [task-gdj-20](planning/tasks/GDJ-20.md) | Bounded outcome, context, acceptance and handoff: shared rifle combat and lifecycle. | mixed | [design-first-earned-level-up](design/first-earned-level-up.md), [design-multiplayer-boundaries](design/multiplayer-boundaries.md) |
| [task-gdj-22](planning/tasks/GDJ-22.md) | Bounded outcome, context, acceptance and handoff: GDJ-22 — shared scenario configuration. | mixed | [design-multiplayer-boundaries](design/multiplayer-boundaries.md), [evidence-shared-rifle](playtests/2026-10-04-shared-rifle.md) |
| [task-gdj-23](planning/tasks/GDJ-23.md) | Bounded outcome, context, acceptance and handoff: first earned level-up implementation. | mixed | [design-first-earned-level-up](design/first-earned-level-up.md), [design-progression-and-upgrade-pools](design/progression-and-upgrade-pools.md), [decision-0013](decisions/0013-readable-xp-and-safe-first-level-up.md) |
| [task-gdj-24](planning/tasks/GDJ-24.md) | Bounded outcome, context, acceptance and handoff: larger arsenal, enemy crowd and co-op performance. | mixed | [evidence-weapon-stack](playtests/2026-10-05-weapon-stack.md), [performance](performance.md), [workflow](workflow.md) |
| [task-gdj-25](planning/tasks/GDJ-25.md) | Bounded outcome, context, acceptance and handoff: a readable ranged threat in the growing arsenal run. | mixed | [foundation](foundation.md), [design-weapon-control-and-combat-readability](design/weapon-control-and-combat-readability.md), [evidence-weapon-stack](playtests/2026-10-05-weapon-stack.md), [workflow](workflow.md) |
| [task-gdj-26](planning/tasks/GDJ-26.md) | Bounded outcome, context, acceptance and handoff: GDD structure and traceability. | current | [foundation](foundation.md), [document-contract](documentation.md), [gdd-index](design/README.md) |
| [task-gdj-3](planning/tasks/GDJ-3.md) | Bounded outcome, context, acceptance and handoff: Earn a simultaneous weapon stack. | mixed | [design-progression-and-upgrade-pools](design/progression-and-upgrade-pools.md), [decision-0011](decisions/0011-weapon-traits-and-upgrade-eligibility.md) |
| [task-gdj-6](planning/tasks/GDJ-6.md) | Bounded outcome, context, acceptance and handoff: Design the first Engine Kingdoms cooperative encounter. | mixed | [design-engine-kingdoms-first-encounter](design/engine-kingdoms-first-encounter.md), [design-first-earned-level-up](design/first-earned-level-up.md) |

## state

Current capability orientation backed by source and dated evidence.

| ID and document | Why it exists | Maintenance | Upstream basis |
| --- | --- | --- | --- |
| [current-state](current-state.md) | Distinguish integrated capabilities, candidates and dated evidence limits. | current | [evidence-ranged-enemy](playtests/2026-10-06-ranged-enemy.md), [evidence-coop-scale](playtests/2026-10-05-coop-scale.md), [evidence-coop-publication-diagnosis](playtests/2026-10-06-coop-publication-diagnosis.md), [evidence-weapon-stack](playtests/2026-10-05-weapon-stack.md), [evidence-earned-growth](playtests/2026-10-05-earned-growth.md), [evidence-scenario-configuration](playtests/2026-10-04-scenario-configuration.md), [evidence-shared-rifle](playtests/2026-10-04-shared-rifle.md), [evidence-rifle-baseline](playtests/2026-10-03-rifle-baseline.md), [evidence-precision-aim](playtests/2026-10-04-precision-aim.md), [evidence-overhead-health](playtests/2026-10-03-overhead-health.md), [evidence-shot-alignment](playtests/2026-10-04-shot-alignment.md), [evidence-rifle-rate-controls](playtests/2026-10-04-rifle-rate-controls.md), [evidence-renderer-experiments](playtests/2026-09-30-renderer-experiments.md) |

## evidence

Exact tested source, results and limits; human and automated evidence remain distinct.

| ID and document | Why it exists | Maintenance | Upstream basis |
| --- | --- | --- | --- |
| [evidence-aimed-health-feedback](playtests/2026-10-03-aimed-health-feedback.md) | Preserve dated source, results and limits: Candidate: useful body brackets without central HP panels. | historical | [task-gdj-16](planning/tasks/GDJ-16.md) |
| [evidence-boss-scale](playtests/2026-09-30-boss-scale.md) | Preserve dated source, results and limits: Boss-scale diagnostic — 2026-09-30. | historical | [performance](performance.md), [unity-project](../prototypes/001-stacked-weapons/README.md) |
| [evidence-coop-publication-diagnosis](playtests/2026-10-06-coop-publication-diagnosis.md) | Preserve dated source, results and limits: Large co-op host publication diagnosis — October 6. | historical | [task-gdj-24](planning/tasks/GDJ-24.md) |
| [evidence-coop-scale](playtests/2026-10-05-coop-scale.md) | Preserve dated source, results and limits: Larger simultaneous arsenals and cooperative crowds — October 5. | historical | [task-gdj-24](planning/tasks/GDJ-24.md) |
| [evidence-damage-readability](playtests/2026-10-03-damage-readability.md) | Preserve dated source, results and limits: Candidate: enemy health and damage readability. | historical | [task-gdj-16](planning/tasks/GDJ-16.md) |
| [evidence-damage-readability-revision](playtests/2026-10-03-damage-readability-revision.md) | Preserve dated source, results and limits: Candidate: attached enemy health bars and wounded silhouettes. | historical | [task-gdj-16](planning/tasks/GDJ-16.md) |
| [evidence-damaged-only-health](playtests/2026-10-03-damaged-only-health.md) | Preserve dated source, results and limits: Candidate: normal-enemy HP only after damage. | historical | [task-gdj-16](planning/tasks/GDJ-16.md) |
| [evidence-earned-growth](playtests/2026-10-05-earned-growth.md) | Preserve dated source, results and limits: First earned growth: owner feedback and revised candidate. | historical | [task-gdj-23](planning/tasks/GDJ-23.md) |
| [evidence-focused-health-feedback](playtests/2026-10-03-focused-health-feedback.md) | Preserve dated source, results and limits: Candidate: fixed target HP and retained wounded bars. | historical | [task-gdj-16](planning/tasks/GDJ-16.md) |
| [evidence-frame-spike-experiments](playtests/2026-09-30-frame-spike-experiments.md) | Preserve dated source, results and limits: Effect rendering, job scheduling, and native-state experiments. | historical | [performance](performance.md), [unity-project](../prototypes/001-stacked-weapons/README.md) |
| [evidence-jobs-burst](playtests/2026-09-30-jobs-burst.md) | Preserve dated source, results and limits: Jobs/Burst and rendering comparison — 2026-09-30. | historical | [performance](performance.md), [unity-project](../prototypes/001-stacked-weapons/README.md) |
| [evidence-overhead-health](playtests/2026-10-03-overhead-health.md) | Preserve dated source, results and limits: Candidate: overhead HP without cyan brackets. | historical | [task-gdj-16](planning/tasks/GDJ-16.md) |
| [evidence-precision-aim](playtests/2026-10-04-precision-aim.md) | Preserve dated source, results and limits: GDJ-17: aimed target color and critical head hits. | historical | [task-gdj-17](planning/tasks/GDJ-17.md) |
| [evidence-prototype-performance](playtests/2026-09-30-prototype-performance.md) | Preserve dated source, results and limits: First prototype: build and performance validation. | historical | [performance](performance.md), [unity-project](../prototypes/001-stacked-weapons/README.md) |
| [evidence-ranged-enemy](playtests/2026-10-06-ranged-enemy.md) | Preserve dated source, results and limits: GDJ-25 ranged-enemy candidate — October 6, 2026. | historical | [task-gdj-25](planning/tasks/GDJ-25.md) |
| [evidence-renderer-experiments](playtests/2026-09-30-renderer-experiments.md) | Preserve dated source, results and limits: Cheaper projectile and character renderers. | historical | [performance](performance.md), [unity-project](../prototypes/001-stacked-weapons/README.md) |
| [evidence-rifle-baseline](playtests/2026-10-03-rifle-baseline.md) | Preserve dated source, results and limits: Playtest: current rifle baseline. | historical | [task-gdj-2](planning/tasks/GDJ-2.md) |
| [evidence-rifle-rate-controls](playtests/2026-10-04-rifle-rate-controls.md) | Preserve dated source, results and limits: GDJ-19: rifle rate testing controls. | historical | [task-gdj-19](planning/tasks/GDJ-19.md) |
| [evidence-scenario-configuration](playtests/2026-10-04-scenario-configuration.md) | Preserve dated source, results and limits: GDJ-22 — scenario configuration candidate. | historical | [task-gdj-22](planning/tasks/GDJ-22.md) |
| [evidence-shared-rifle](playtests/2026-10-04-shared-rifle.md) | Preserve dated source, results and limits: GDJ-20 shared rifle and background preview. | historical | [task-gdj-20](planning/tasks/GDJ-20.md) |
| [evidence-shot-alignment](playtests/2026-10-04-shot-alignment.md) | Preserve dated source, results and limits: GDJ-15: rifle shots during fast turns. | historical | [task-gdj-15](planning/tasks/GDJ-15.md) |
| [evidence-support-frame-spikes](playtests/evidence/frame-spikes/README.md) | Preserve dated source, results and limits: Frame-spike experiment evidence. | historical | [evidence-frame-spike-experiments](playtests/2026-09-30-frame-spike-experiments.md) |
| [evidence-support-renderers](playtests/evidence/renderers/README.md) | Preserve dated source, results and limits: Renderer experiment evidence. | historical | [evidence-renderer-experiments](playtests/2026-09-30-renderer-experiments.md) |
| [evidence-weapon-stack](playtests/2026-10-05-weapon-stack.md) | Preserve dated source, results and limits: Earned simultaneous weapon-stack MVP — October 5. | historical | [task-gdj-3](planning/tasks/GDJ-3.md) |

## technical

Engineering boundaries and measurement methods, including named legacy locations.

| ID and document | Why it exists | Maintenance | Upstream basis |
| --- | --- | --- | --- |
| [architecture](architecture.md) | Locate the active Unity project and distinguish code, assets and documents. | current | [workflow](workflow.md), [document-contract](documentation.md) |
| [design-late-game-workload](design/late-game-workload.md) | Describe synthetic density assumptions and what representative workload still lacks. | mixed | [foundation](foundation.md), [performance](performance.md) |
| [design-multiplayer-boundaries](design/multiplayer-boundaries.md) | Propose authority, configuration, lifecycle and replication responsibilities. | draft | [design-first-earned-level-up](design/first-earned-level-up.md), [decision-0012](decisions/0012-disconnect-recovery-and-preserved-return.md) |
| [performance](performance.md) | Preserve performance goals and protocols for explicitly selected investigations. | current | [foundation](foundation.md), [workflow](workflow.md) |
| [profiling](profiling.md) | Reproduce a bounded profiler investigation when needed. | current | [performance](performance.md) |

## workflow

Session behavior, document ownership, update rules and reusable instructions.

| ID and document | Why it exists | Maintenance | Upstream basis |
| --- | --- | --- | --- |
| [agent-entry](../AGENTS.md) | Require source-aware startup, bounded work and the documentation contract. | current | [workflow](workflow.md), [document-contract](documentation.md) |
| [document-contract](documentation.md) | Define mandatory placement, authority, registration and traceability. | current | [workflow](workflow.md) |
| [start-design](planning/start-design-session.md) | Resume a focused design conversation from the registered sources. | current | [document-contract](documentation.md), [gdd-index](design/README.md) |
| [start-planning](planning/start-planning-session.md) | Resume coordination without relying on prior chat history. | current | [document-contract](documentation.md), [planning](planning/README.md) |
| [workflow](workflow.md) | Own role boundaries, implementation/review procedure and proportional verification. | current | Entry / original record |

## template

Forms for creating traceable records; placeholder content is not a decision.

| ID and document | Why it exists | Maintenance | Upstream basis |
| --- | --- | --- | --- |
| [template-bug-report](../.github/ISSUE_TEMPLATE/bug_report.md) | Author build and environment with context, source and handoff fields. | template | [document-contract](documentation.md) |
| [template-decisions](decisions/template.md) | Author nnnn: decision title with context, source and handoff fields. | template | [document-contract](documentation.md) |
| [template-design](design/template.md) | Author design: topic with context, source and handoff fields. | template | [document-contract](documentation.md) |
| [template-experiment](../.github/ISSUE_TEMPLATE/experiment.md) | Author player benefit or question with context, source and handoff fields. | template | [document-contract](documentation.md) |
| [template-playtests](playtests/template.md) | Author playtest: short title with context, source and handoff fields. | template | [document-contract](documentation.md) |
| [template-pr](../.github/pull_request_template.md) | Author what changed and why? with context, source and handoff fields. | template | [document-contract](documentation.md) |
| [template-task](planning/task-brief-template.md) | Author task brief and handoff with context, source and handoff fields. | template | [document-contract](documentation.md) |

## history

Dated learning/process context with no authority to schedule current work.

| ID and document | Why it exists | Maintenance | Upstream basis |
| --- | --- | --- | --- |
| [history-2026-10-02-development-system](planning/2026-10-02-development-system.md) | Preserve earlier process context: Our journey and proposed development system. | historical | [workflow](workflow.md) |
| [history-2026-10-02-solo-ai-system](planning/sessions/2026-10-02-solo-ai-system.md) | Preserve earlier process context: Planning session: solo development with replaceable AI chats. | historical | [workflow](workflow.md) |
| [history-verification](planning/diagrams/verification.md) | Preserve earlier process context: Development loop verification. | historical | [workflow](workflow.md) |
| [journal-boss-scale](devlog/2026-09-30-boss-scale.md) | Record the dated development lesson: Testing the intended late-game scale. | historical | [foundation](foundation.md) |
| [journal-day-zero](devlog/2026-09-30-day-zero.md) | Record the dated development lesson: Day zero: learning to make a game. | historical | [foundation](foundation.md) |
| [journal-first-playable](devlog/2026-09-30-first-playable.md) | Record the dated development lesson: First playable: one rifle and a repeatable crowd. | historical | [foundation](foundation.md) |
| [journal-frame-spike-experiments](devlog/2026-09-30-frame-spike-experiments.md) | Record the dated development lesson: Four experiments after Jobs/Burst. | historical | [foundation](foundation.md) |
| [journal-jobs-burst](devlog/2026-09-30-jobs-burst.md) | Record the dated development lesson: Measuring Jobs/Burst and rendering changes. | historical | [foundation](foundation.md) |
| [journal-reference-direction](devlog/2026-09-30-reference-direction.md) | Record the dated development lesson: Choosing a reference: Vampire Hunters. | historical | [foundation](foundation.md) |
| [journal-renderer-experiments](devlog/2026-09-30-renderer-experiments.md) | Record the dated development lesson: Rendering fewer submissions, keeping every attack. | historical | [foundation](foundation.md) |
| [journal-unity-selected](devlog/2026-09-30-unity-selected.md) | Record the dated development lesson: Selecting Unity and C#. | historical | [foundation](foundation.md) |

## guide

Repository/project/tool navigation, motivation or asset provenance at its owning location.

| ID and document | Why it exists | Maintenance | Upstream basis |
| --- | --- | --- | --- |
| [asset-register](../assets/README.md) | Record external asset origins, licensing and editable-source provenance. | current | [architecture](architecture.md) |
| [contributing](../CONTRIBUTING.md) | Explain how focused contributions and review fit this repository. | current | [workflow](workflow.md) |
| [game-placeholder](../game/README.md) | Explain that this reserved directory is not the active Unity project. | current | [architecture](architecture.md) |
| [project-motivation](vision.md) | Explain the learning goals behind the project; foundation owns game identity. | current | [foundation](foundation.md) |
| [prototype-index](../prototypes/README.md) | Locate the active project and explain separately scoped experiments. | current | [architecture](architecture.md) |
| [repo-guide](../README.md) | Orient readers and route them to maintained game, planning and document records. | current | [document-map](README.md), [current-state](current-state.md) |
| [tools-guide](../tools/README.md) | Locate reusable commands, prerequisites and documentation validation. | current | [workflow](workflow.md), [document-contract](documentation.md) |
| [unity-project](../prototypes/001-stacked-weapons/README.md) | Own pinned setup, controls, scenario commands, tests and build reproduction. | current | [foundation](foundation.md), [architecture](architecture.md) |

## Supporting files

Raw files are indexed by their owning report or guide rather than hundreds of separate navigation entries. Markdown within these groups still requires its own document entry.

| Location | Owner | Why it lives here |
| --- | --- | --- |
| `docs/planning/diagrams/*` | [history-2026-10-02-development-system](planning/2026-10-02-development-system.md) | Saved planning visualization, its source and visual-check outputs. |
| `docs/playtests/evidence/frame-spikes/*` | [evidence-frame-spike-experiments](playtests/2026-09-30-frame-spike-experiments.md) | Raw captures/manifests explained by the owning dated report. |
| `docs/playtests/evidence/renderers/*` | [evidence-renderer-experiments](playtests/2026-09-30-renderer-experiments.md) | Raw captures/manifests explained by the owning dated report. |
| `docs/playtests/evidence/jobs-burst/*` | [evidence-jobs-burst](playtests/2026-09-30-jobs-burst.md) | Raw captures/manifests explained by the owning dated report. |
| `docs/playtests/evidence/gdj-2-rifle/*` | [evidence-rifle-baseline](playtests/2026-10-03-rifle-baseline.md) | Raw captures/manifests explained by the owning dated report. |
| `docs/playtests/evidence/gdj-3-weapon-stack/*` | [evidence-weapon-stack](playtests/2026-10-05-weapon-stack.md) | Raw captures/manifests explained by the owning dated report. |
| `docs/playtests/evidence/gdj-15-shot-v1/*` | [evidence-shot-alignment](playtests/2026-10-04-shot-alignment.md) | Raw captures/manifests explained by the owning dated report. |
| `docs/playtests/evidence/gdj-16-damage/*` | [evidence-damage-readability](playtests/2026-10-03-damage-readability.md) | Raw captures/manifests explained by the owning dated report. |
| `docs/playtests/evidence/gdj-16-damage-v3/*` | [evidence-damage-readability-revision](playtests/2026-10-03-damage-readability-revision.md) | Raw captures/manifests explained by the owning dated report. |
| `docs/playtests/evidence/gdj-16-damage-v4/*` | [evidence-focused-health-feedback](playtests/2026-10-03-focused-health-feedback.md) | Raw captures/manifests explained by the owning dated report. |
| `docs/playtests/evidence/gdj-16-damage-v5/*` | [evidence-aimed-health-feedback](playtests/2026-10-03-aimed-health-feedback.md) | Raw captures/manifests explained by the owning dated report. |
| `docs/playtests/evidence/gdj-16-damage-v6/*` | [evidence-damaged-only-health](playtests/2026-10-03-damaged-only-health.md) | Raw captures/manifests explained by the owning dated report. |
| `docs/playtests/evidence/gdj-16-damage-v7/*` | [evidence-overhead-health](playtests/2026-10-03-overhead-health.md) | Raw captures/manifests explained by the owning dated report. |
| `docs/playtests/evidence/gdj-17-precision-v1/*` | [evidence-precision-aim](playtests/2026-10-04-precision-aim.md) | Raw captures/manifests explained by the owning dated report. |
| `docs/playtests/evidence/gdj-19-rate-v1/*` | [evidence-rifle-rate-controls](playtests/2026-10-04-rifle-rate-controls.md) | Raw captures/manifests explained by the owning dated report. |
| `docs/playtests/evidence/gdj-19-rate-v2/*` | [evidence-rifle-rate-controls](playtests/2026-10-04-rifle-rate-controls.md) | Raw captures/manifests explained by the owning dated report. |
| `docs/playtests/evidence/gdj-20-*/*` | [evidence-shared-rifle](playtests/2026-10-04-shared-rifle.md) | Raw captures/manifests explained by the owning dated report. |
| `docs/playtests/evidence/gdj-22-scenarios/*` | [evidence-scenario-configuration](playtests/2026-10-04-scenario-configuration.md) | Raw captures/manifests explained by the owning dated report. |
| `docs/playtests/evidence/gdj-23-earned-growth/*` | [evidence-earned-growth](playtests/2026-10-05-earned-growth.md) | Raw captures/manifests explained by the owning dated report. |
| `docs/playtests/evidence/gdj-24-coop-scale/*` | [evidence-coop-scale](playtests/2026-10-05-coop-scale.md) | Raw captures/manifests explained by the owning dated report. |
| `docs/playtests/evidence/gdj-24-publication/*` | [evidence-coop-publication-diagnosis](playtests/2026-10-06-coop-publication-diagnosis.md) | Raw captures/manifests explained by the owning dated report. |
| `docs/playtests/evidence/gdj-25-ranged-enemy/*` | [evidence-ranged-enemy](playtests/2026-10-06-ranged-enemy.md) | Raw captures/manifests explained by the owning dated report. |
| `docs/playtests/evidence/2026-09-30-arena.png` | [evidence-prototype-performance](playtests/2026-09-30-prototype-performance.md) | Original local diagnostic image or capture; read the report limits. |
| `docs/playtests/evidence/2026-09-30-bosses.png` | [evidence-boss-scale](playtests/2026-09-30-boss-scale.md) | Original local diagnostic image or capture; read the report limits. |
| `docs/playtests/evidence/2026-09-30-hidden-10000-bosses.json` | [evidence-boss-scale](playtests/2026-09-30-boss-scale.md) | Original local diagnostic image or capture; read the report limits. |
| `docs/playtests/evidence/2026-09-30-hidden-20000-bosses.json` | [evidence-boss-scale](playtests/2026-09-30-boss-scale.md) | Original local diagnostic image or capture; read the report limits. |
| `docs/playtests/evidence/2026-09-30-hidden-20000-synchronized.json` | [evidence-boss-scale](playtests/2026-09-30-boss-scale.md) | Original local diagnostic image or capture; read the report limits. |
| `docs/playtests/evidence/2026-09-30-visible-1000.json` | [evidence-prototype-performance](playtests/2026-09-30-prototype-performance.md) | Original local diagnostic image or capture; read the report limits. |
| `docs/playtests/evidence/2026-09-30-visible-15000-bosses.json` | [evidence-boss-scale](playtests/2026-09-30-boss-scale.md) | Original local diagnostic image or capture; read the report limits. |
| `docs/playtests/evidence/2026-09-30-visible-15000-invocation.json` | [evidence-boss-scale](playtests/2026-09-30-boss-scale.md) | Original local diagnostic image or capture; read the report limits. |
