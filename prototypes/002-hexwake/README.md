# Hexwake source snapshot

The current Windows release is [Hexwake v0.1.0](https://github.com/OpenCnid/game-dev-journey/releases/tag/hexwake-v0.1.0).
For playing and controls, start with [the repository README](../../README.md).

This folder contains the **exact compiled C# source closure** of the playtested
J/R6 game, with only window/menu branding changed for publication. Earlier
prototype sources and engine pins are not replaced.

## What is here

- `source/`: 39 runtime/bootstrap C# files and 12 test files, retaining their
  original logical paths and namespaces.
- `source-manifest.json`: selected files, upstream hashes and the explicit
  retained-template build route.
- `tools/build.ps1`: compile the source into a complete existing Unity player.
- `tools/verify.ps1`: execute the packaged regression or capture scenarios.
- `VERIFICATION.md`: exercised scenarios and limits.
- `THIRD_PARTY_NOTICES.md`: original work and inherited resource/runtime terms.

## Build route

**This is not a fresh Unity Editor project export.** The current fixture uses
a retained Windows x64 Unity **6000.6.3f1 Mono player**, including its complete
shader variants and runtime resources. The C# assembly is rebuilt against that
player's managed assemblies. Do not open this folder as an Editor project or
substitute the older URP prototype's player.

To recompile:

1. Download and extract the current Hexwake release as a player template.
2. Install or locate Unity **6000.6.3f1** with Windows Mono support.
3. Run PowerShell from the repository root:

```powershell
.\prototypes\002-hexwake\tools\build.ps1 `
  -TemplatePath "C:\Games\Hexwake" `
  -UnityEditor "C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor" `
  -OutputRoot "$env:TEMP\hexwake-rebuild"
```

The output is `player/Hexwake.exe` and the complete adjacent runtime. Use a new
output directory for each build. No engine installation is required simply to
play the downloadable release.

### Tests and captures

For the 146-case regression assembly, pass `-NUnitPath` to `build.ps1`, pointing
to the `nunit.framework.dll` supplied by Unity Test Framework's package cache.
The test dependency is not required by, or included in, the playable ZIP.

```powershell
.\prototypes\002-hexwake\tools\verify.ps1 `
  -BuildRoot "$env:TEMP\hexwake-rebuild" `
  -OutputRoot "$env:TEMP\hexwake-regressions" -Mode regressions

.\prototypes\002-hexwake\tools\verify.ps1 `
  -BuildRoot "$env:TEMP\hexwake-rebuild" `
  -OutputRoot "$env:TEMP\hexwake-captures" -Mode capture
```

Generated diagnostics belong outside Git. They can contain local paths.
The normal player has no test-automation flags enabled.

## Scope

The public name is Hexwake; internal namespaces retain the earlier trial names
to avoid a gameplay refactor during publication. The game is currently solo.
This snapshot does not promise the repository's longer-term cooperative design,
an arbitrary-hardware performance target, or a clean-project Unity export.
