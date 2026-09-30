param(
    [ValidateRange(0,20000)][int]$Count = 20000,
    [ValidateRange(1,20)][int]$Repeats = 3,
    [int]$WarmupSeconds = 30,
    [int]$CaptureSeconds = 60,
    [switch]$Staggered,
    [switch]$Visible,
    [ValidateSet('native','octa','indirect-cube','indirect-octa','characters','dedup','cached','combined')]
    [string[]]$Modes = @('native','octa','indirect-cube','indirect-octa','characters','dedup','cached','combined'),
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
if (-not $Visible) { throw 'Pass -Visible and keep the player unobstructed.' }
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path (Split-Path -Parent $PSScriptRoot) ('prototypes/001-stacked-weapons/BenchmarkResults/renderers-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
}
$taskOptions = @{
    'native' = @{}
    'octa' = @{ProjectileRenderer='octa'}
    'indirect-cube' = @{ProjectileRenderer='indirect-cube'}
    'indirect-octa' = @{ProjectileRenderer='indirect-octa'}
    'characters' = @{CombinedCharacters=$true}
    'dedup' = @{DeduplicateWarnings=$true}
    'cached' = @{DeduplicateWarnings=$true;CacheWarnings=$true}
    'combined' = @{ProjectileRenderer='indirect-octa';CombinedCharacters=$true;DeduplicateWarnings=$true;CacheWarnings=$true}
}
for ($taskRepeat = 0; $taskRepeat -lt $Repeats; $taskRepeat++) {
    for ($taskIndex = 0; $taskIndex -lt $Modes.Count; $taskIndex++) {
        $taskMode = $Modes[($taskIndex + $taskRepeat) % $Modes.Count]
        $taskFlags = $taskOptions[$taskMode]
        $taskFolder = Join-Path $OutputDirectory "r$($taskRepeat + 1)-$taskMode"
        Write-Output "Starting $taskMode, repeat $($taskRepeat + 1), $(Get-Date -Format o)"
        & (Join-Path $PSScriptRoot 'benchmark.ps1') -Visible -Counts $Count -Repeats 1 -WarmupSeconds $WarmupSeconds -CaptureSeconds $CaptureSeconds -SynchronizedAbilities:(-not $Staggered) -NativeState -Simulation burst -Rendering culled -OutputDirectory $taskFolder @taskFlags
        if (-not $?) { throw "Renderer experiment $taskMode failed." }
    }
}
Write-Output "Renderer reports: $OutputDirectory"
