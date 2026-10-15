# Accept Hexwake publication

**Accepted October 15, 2026, by the project owner.**

The owner judged the current game ready to share and requested upload to the
existing GitHub repository, a README that explains it at first glance, and an
agent-selected fun name. **Hexwake** is the publication name.

## Decision

- Publish the playtested J/R6 solo Windows game, with window/menu/package branding
  changed to Hexwake and no gameplay revision.
- Use `prototypes/002-hexwake/` for its selected C# source and rebuild instructions.
  Preserve `prototypes/001-stacked-weapons/`, its Unity pin and previous outcomes.
- Put the complete Windows player in a GitHub release ZIP, not Git history.
  Source and an actual gameplay screenshot remain in the repository.
- Retain the repository's existing private visibility. Download access follows
  repository permissions.
- Document the retained Unity 6000.6.3f1 Mono-template build dependency accurately.
  Do not describe this snapshot as a clean Editor project export.
- Distinguish the current solo prototype from longer-term co-op and performance
  goals. This is a publication decision, not approval of every experiment as
  final full-game design.

The [landing page](../../README.md) is the player entry point. The
[source route](../../prototypes/002-hexwake/README.md) and
[verification note](../../prototypes/002-hexwake/VERIFICATION.md) record the
reproduction boundary and exercised checks.
