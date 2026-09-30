param(
    [int]$Count = 15000,
    [int]$Repeats = 3,
    [int]$WarmupSeconds = 30,
    [int]$CaptureSeconds = 60,
    [switch]$Visible,
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
if (-not $Visible) { throw 'Pass -Visible and keep each player window unobstructed for rendering measurements.' }
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path (Split-Path -Parent $PSScriptRoot) ('prototypes/001-stacked-weapons/BenchmarkResults/compare-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
# Rotate the order each repeat to reduce a fixed warm-machine/order bias.
$taskModes = @(
    @{Simulation='managed';Rendering='legacy'},
    @{Simulation='burst';Rendering='legacy'},
    @{Simulation='managed';Rendering='culled'},
    @{Simulation='burst';Rendering='culled'}
)
for ($taskRepeat = 0; $taskRepeat -lt $Repeats; $taskRepeat++) {
    for ($taskIndex = 0; $taskIndex -lt $taskModes.Count; $taskIndex++) {
        $taskMode = $taskModes[($taskIndex + $taskRepeat) % $taskModes.Count]
        $taskFolder = Join-Path $OutputDirectory ("r$($taskRepeat + 1)-$($taskMode.Simulation)-$($taskMode.Rendering)")
        & (Join-Path $PSScriptRoot 'benchmark.ps1') -Visible -Counts $Count -Repeats 1 -WarmupSeconds $WarmupSeconds -CaptureSeconds $CaptureSeconds -Workload bosses -Simulation $taskMode.Simulation -Rendering $taskMode.Rendering -OutputDirectory $taskFolder
        if (-not $?) { throw 'Backend comparison failed.' }
    }
}
Write-Output "Comparison reports: $OutputDirectory"
