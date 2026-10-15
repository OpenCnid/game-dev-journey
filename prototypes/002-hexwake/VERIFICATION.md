# Hexwake v0.1.0 verification

The baseline is the owner-playtested
Crucible Clarity J/R6 Windows player. The publication source changes only its
window/menu title and executable/Data-directory naming to Hexwake.

## Baseline evidence

J/R6 passed 146 engine cases and both 18-case actual-hit presentation runs.
Its 220 rendered frames at 1280x720 and 1920x800 received scoped acceptance.
Coverage includes sixteen gun identities and dark/bright/firing contexts, six
relic identities and collection states, simple gain/cost choices, empty/full/
duplicate frozen inventories, full-slot replacement, and blocked/restored
nearest gun/relic prompts.

The source snapshot selects the same 39 runtime/bootstrap files and 12 test
files. `source-manifest.json` retains upstream hashes without exporting
machine-local diagnostic paths. Original builds and owner game sessions are
not modified by publication.

The original provenance manifest also listed four preserved, overridden base
files. Those were not compiler-selected inputs and are not included in this
snapshot; the published manifest follows the actual compiler argument lists.

## Release checks

The named release compiled through `tools/build.ps1`, including its test assembly,
with exit code 0. The actual player returned:

| Check | Result |
| --- | --- |
| Engine regressions | 146 passed, 0 failed |
| 1280x720 presentation/actual hits | 18 passed, 0 failed; 110 PNGs |
| 1920x800 presentation/actual hits | 18 passed, 0 failed; 110 PNGs |
| Branding | Both named-menu captures directly inspected; readable Hexwake title |
| Complete package | 212 files; every ZIP-extracted file matched the build manifest |
| Extracted normal startup | Hexwake window observed without automation flags; owned smoke player closed cleanly |

The 220 baseline frames had already received scoped visual acceptance. The fresh
publication captures were generated again; only the changed menu/title surface
is claimed as freshly visually inspected here.

Archive: `Hexwake-v0.1.0-windows-x64.zip`, **185,805,883 bytes**.
SHA-256:

```text
2ca44594fb229afb4b47f7fbe6d82a389909546a62f5587b9c1a3798d317e121
```

The release also supplies `SHA256SUMS.txt` and a portable file-hash manifest.
Debug symbols, ownership records, player logs, machine-specific build-input
records and local caches are excluded from the download.

## Limits

- Windows x64, single-player, keyboard and mouse; no current cooperative mode.
- Development prototype; fun, balance and arbitrary-hardware performance are
  owner/playtester judgments, not established by automated checks.
- Source rebuild uses the complete retained Unity Mono player from the release,
  not a fresh Editor project export.
- Native J E/1-3 re-verification was interrupted when the owner game regained
  foreground; no fresh physical-input pass is claimed. Ordinary bindings and
  collection rules remain unchanged; packaged scenarios execute those actions.
- C# language-server checks were unavailable in the original environment.
  Compiler/runtime checks provide the executable evidence. Retained mapped-
  shader tests have existing Unity CS0618 deprecation warnings.
- Full OS accessibility, reduced motion, focus order, localization, 200% text
  scaling and other platforms have not been certified.
