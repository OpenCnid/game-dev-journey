param(
    [ValidateRange(0,20000)][int]$Count = 20000,
    [ValidateRange(1,20)][int]$Repeats = 3,
    [int]$WarmupSeconds = 30,
    [int]$CaptureSeconds = 60,
    [switch]$Staggered,
    [switch]$Visible,
    [ValidateSet('control','no-warnings','no-projectiles','no-effects','dedup','overlap','native','combined')]
    [string[]]$Modes = @('control','no-warnings','no-projectiles','no-effects','dedup','overlap','native','combined'),
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
if (-not $Visible) { throw 'Pass -Visible and keep the player unobstructed.' }
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path (Split-Path -Parent $PSScriptRoot) ('prototypes/001-stacked-weapons/BenchmarkResults/experiments-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
}
$taskOptions = @{
    'control' = @{}
    'no-warnings' = @{HideWarnings=$true}
    'no-projectiles' = @{HideProjectiles=$true}
    'no-effects' = @{HideWarnings=$true;HideProjectiles=$true}
    'dedup' = @{DeduplicateWarnings=$true}
    'overlap' = @{OverlapPreparation=$true}
    'native' = @{NativeState=$true}
    'combined' = @{DeduplicateWarnings=$true;OverlapPreparation=$true;NativeState=$true}
}
for ($taskRepeat = 0; $taskRepeat -lt $Repeats; $taskRepeat++) {
    for ($taskIndex = 0; $taskIndex -lt $Modes.Count; $taskIndex++) {
        $taskMode = $Modes[($taskIndex + $taskRepeat) % $Modes.Count]
        $taskFlags = $taskOptions[$taskMode]
        $taskFolder = Join-Path $OutputDirectory "r$($taskRepeat + 1)-$taskMode"
        Write-Output "Starting $taskMode, repeat $($taskRepeat + 1), $(Get-Date -Format o)"
        & (Join-Path $PSScriptRoot 'benchmark.ps1') -Visible -Counts $Count -Repeats 1 -WarmupSeconds $WarmupSeconds -CaptureSeconds $CaptureSeconds -SynchronizedAbilities:(-not $Staggered) -Simulation burst -Rendering culled -OutputDirectory $taskFolder @taskFlags
        if (-not $?) { throw "Experiment $taskMode failed." }
    }
}
Write-Output "Experiment reports: $OutputDirectory"
