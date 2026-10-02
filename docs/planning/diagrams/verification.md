# Development loop verification

Verified October 2, 2026 with Archify 2.16. This receipt concerns the diagram, not Unity gameplay or performance acceptance.

```text
diagram_type: workflow
output: C:/Users/Darian/.codex/worktrees/f01d/new-game-dev/docs/planning/diagrams/development-loop.html
specification_sha256: 8d0ec379237119273fcf289a3727f8c50db4bb613df4a35d47589acfd7220b52
specification_bytes: 2478
artifact_sha256: 6b670264a41069afc353d762d081f8ea0e8c5f7ba4c6385d44d9feb1ca738916
artifact_bytes: 708643
validation: 9/9 showcase, 0 errors, 0 warnings
browser_evidence: passed
visual_review: passed
correction_rounds: 2
```

The final `deliver` command returned success. Its exact artifact passed the packaged `visual-check` using installed Chromium headless shell after the initial Chrome/Edge attempts could not complete. No packaged renderer/checker code was changed. The final artifact-bound [automated browser receipt](development-loop.visual-check.json) records containment at 1440×900, 1600×1000, 1920×1080 and 2048×1320, with light/dark captures at both endpoint sizes. It reports `visualReview: pending` because automation cannot establish perceptual quality.

Separately, an image-capable review inspected all four final endpoint/theme screenshots: the main path, routes, labels, cards and viewer controls are visible without clipping. Two focused vertical-spacing repairs were made and the candidate was validated/delivered again before collecting this final evidence. See the [contact sheet](development-loop.visual-check.html). The source specification is frozen at the checksum above.

The accompanying inline journey and chat-role views were checked at 320, 360, 560 and 736 px content widths in light/dark themes. All 11 milestone and five chat-role selections updated the selected detail without JavaScript errors; no horizontal overflow was observed. Desktop and narrow screenshots were inspected. Those conversation views are separate from this standalone diagram and its receipt.
