# How we work

## The development loop

1. Pick one observable improvement or question.
2. Describe what a player should be able to do and how we'll check it.
3. Make the smallest useful change.
4. Run it and play through the affected behavior.
5. Keep, revise, or remove the change based on what happened.
6. Commit a coherent result and record any useful lesson.

Use issues when work needs tracking. Keep the current focus small enough to complete in a focused session. A prototype should have a stopping condition so experiments do not quietly become permanent projects.

## Working with AI and LLMs

Use AI for explanations, alternatives, small implementations, debugging hypotheses, code review, and repetitive work. Give it the current goal, engine/version when known, relevant files, constraints, and a way to check success.

For changes we keep:

- Read the diff and understand the important behavior.
- Verify APIs against the installed engine/version when there is uncertainty.
- Run the affected code; a plausible explanation is not proof that it works.
- Play the result; tests alone cannot establish game feel.
- Record a useful lesson when AI saves time, gets stuck, or sends us in the wrong direction.

Keep useful prompt summaries with the relevant experiment or devlog entry. We do not need to archive every chat. Never commit credentials or private conversation content. Track the origin and usage terms of external assets, including generated ones, in the asset register.

## Verification

For a gameplay change, check the normal path, likely failure states, and restarting or repeating the action. Add automated tests for deterministic rules or recurring bugs when they offer lasting value. Use playtests for clarity, challenge, and enjoyment.

For a build-related change, verify the exported game outside the editor. For documentation-only changes, check links, paths, and whether instructions describe the repository as it actually exists.

The prototype has Edit Mode simulation/metrics tests, a Play Mode runtime smoke test, and a Windows build helper. Use `tools/unity.ps1` as described in [the project instructions](../prototypes/001-stacked-weapons/README.md). Run relevant automated checks after code changes; verify rendering and gameplay separately. There is no hosted CI pipeline yet.

## A change is ready when

- Its intended behavior has been verified, or its limitations are explicitly recorded.
- It introduces no unexplained generated files or dependencies.
- Any changed setup or run steps are documented.
- Relevant credits and source records are updated.
- The commit explains what changed and why.

Small changes can go directly to the main branch during initial solo exploration. Use a short-lived branch and pull request when a change benefits from review or isolation. Keep unfinished experiments in their own prototype folders.
