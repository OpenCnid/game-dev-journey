# Planning session: solo development with replaceable AI chats

October 2, 2026. Project-specific summary of this planning discussion, preserving decisions, reasons and open questions. Repository baseline before this correction: `3ca9f6c`; gameplay/report baseline: `7414fac`. Verify the current checkout when resuming. This is historical context; [the planning handoff](../README.md), [current state](../../current-state.md) and Linear describe the current direction and task state.

## What we decided and why

The developer already works through playable checkpoints and wants a lightweight system for one developer assisted by AI. Linear was selected for the live queue, GitHub for source and durable evidence, and task chats for bounded implementation/investigation.

The **Game Dev** team uses **GDJ** issue IDs. Its **First weapon-stacking playtest** project contains preset validation, current-rifle playtesting, three-weapon comparison, the five-minute upgrade loop and evaluation. Live status/dependencies belong in [Linear](https://linear.app/cnid/project/first-weapon-stacking-playtest-89b5952f722e). Merged PRs move to In Review so required human checks can finish before Done. Automatic GitHub linking to this repository has not been verified.

An earlier recommendation proposed one long-running planning/integration chat alongside temporary gameplay, performance, design and review chats. The developer corrected this: limited context, information buried in long dialogue and compaction make a permanent chat an unsuitable continuity mechanism for this project. Fresh sessions are our preferred boundary. This records a workflow choice, not a claim that every long session necessarily fails.

The requested "broccoli" model became a small common root with relevant branches: `AGENTS.md` directs each fresh session to the planning handoff and current state, then the selected issue/question and only the needed design, source, tests or evidence. A session studies/builds on that branch and writes back the result. Planning remains a role we can start afresh; it does not depend on a particular conversation.

## Context to carry forward

- The performance investigation was useful, but the core promise remains weapon stacking and powerful builds. The current executable has one rifle; human enjoyment/readability evidence is outstanding.
- Unity/C# and pinned packages remain selected. Current desktop benchmarks retain the i7-10700K/RTX 3090/1080p protocol; older hardware is deferred.
- Native state and compact/combined rendering have measured promise and remain experiments until their preset decision is made. Preserve equivalent workload counters and reference controls.
- Prefer a fast edit/known-scenario/reset loop. AI supplies implementation and repeatable verification; human playtesting judges feel/readability/enjoyment.
- Keep one implementation task active and serialize Unity operations and desktop performance captures. Use existing helpers before building workflow infrastructure.

## Resume and stopping conditions

This session changes planning guidance, memory routing and the chat-role visualization. It does not implement gameplay, promote renderer defaults or start a new task chat.

The next planning/task session should read the root files, check current source and consult the selected Linear brief. Carry forward the questions about current combat feel, preset validation and distinct/readable weapon stacking. For a partially completed task, preserve exact working revision, completed steps, remaining work, checks and blockers in a Resume section before replacing its session.

## Documentation basis

[Official AGENTS.md guidance](https://learn.chatgpt.com/docs/agent-configuration/agents-md) describes project instructions loaded at session start. The required read order here is our repository policy; linked Markdown files need to be read explicitly. No new session or agent infrastructure is required for this arrangement.
