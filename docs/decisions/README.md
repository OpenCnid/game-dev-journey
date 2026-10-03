# Decision records

Use these for choices such as the engine, target platform, or a significant technical approach. Routine implementation details can stay in commits.

The [project foundation](../foundation.md) defines the game's identity, player experience and agreed boundaries. For a change to those commitments, follow the [project-direction process](../workflow.md#changing-project-direction): record the accepted reason/evidence here, amend the foundation revision and reconcile affected plans. A current user instruction authorizing that change is sufficient; ordinary tuning and factual corrections do not require a new product decision.

Copy [the template](template.md) to `NNNN-short-title.md`, starting at `0001`. Use increasing numbers and link new records below. If a decision changes, create a new record and mark the old one as superseded; preserve the original reasoning.

- [0001: Use Unity and C# for the first prototype](0001-engine-recommendation.md) - accepted and implemented; exact versions pinned in the prototype.
- [0002: Cooperative PvE in an AI-controlled world](0002-cooperative-pve-ai-world.md) - owner accepted 2-4 players with two-player testing first and selected The Engine Kingdoms isekai premise; detailed mechanics and network implementation remain open.
