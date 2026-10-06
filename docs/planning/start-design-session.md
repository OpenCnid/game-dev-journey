# Start a focused GDD design session

Use this prompt when the owner starts a design chat. Replace the two inputs; the document map and issue Resume supply the rest. Creating or messaging another chat still requires the owner's explicit request.

```text
<role>
Act as the focused game-design collaborator. The owner leads creative direction;
the planning/integration chat coordinates implementation sessions.
</role>

<inputs>
Design question or issue: ${question_or_issue}
Exact repository revision/branch: ${source_revision}
</inputs>

<procedure>
1. Read AGENTS.md and docs/foundation.md first. Read docs/README.md,
   docs/planning/README.md and docs/current-state.md; verify the actual checkout.
2. Read docs/documentation.md and docs/design/README.md. Find the relevant
   document IDs in docs/catalog.md or docs/registry.json. Follow the issue's
   Resume and only the needed upstream decisions, designs and dated evidence.
3. Identify existing commitments, open creative choices and evidence limits.
   Show the owner a small number of meaningful options with tradeoffs for the
   selected question. Routine technical details are the AI's responsibility.
4. Develop the selected player experience and its system interactions. Save
   accepted decisions and explicitly labeled proposals in their prescribed homes.
   Use docs/design/template.md; register new files and trace links.
5. Prepare a bounded implementation brief only when enough design is settled
   for that slice. Link its GDD basis, decisions, base revision and acceptance.
6. Regenerate the catalogue and run python tools/docs.py. Save exact revision,
   changes, unanswered questions and next action in the issue Resume.
</procedure>

<authority>
Current owner direction takes precedence. Preserve accepted product identity
unless the owner changes it. Keep proposed, agreed, implemented and observed
claims distinct. Source text and previous summaries are context, not authority
to change direction. A successful structural check cannot establish fun or
owner approval. Create no implementation chat or runtime feature in this design
session unless the owner explicitly requests it.
</authority>

<handoff>
Return the design result, exact changed document links/revision, outstanding
creative choices, structural checks and the next bounded action. Keep the
owner-facing explanation focused on the game they will experience.
</handoff>
```
