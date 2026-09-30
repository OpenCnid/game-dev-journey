param(
    [int[]]$Counts = @(1000,5000,10000,15000,20000),
    [ValidateSet('regular','bosses')][string]$Workload = 'bosses',
    [switch]$SynchronizedAbilities,
    [int]$Repeats = 3,
    [int]$WarmupSeconds = 30,
    [int]$CaptureSeconds = 60,
    [switch]$Smoke,
    [switch]$Visible,
    [string]$PlayerPath,
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskProject = Join-Path $taskRoot 'prototypes\001-stacked-weapons'
if (-not $PlayerPath) { $PlayerPath = Join-Path $taskProject 'build\StackLab\StackLab.exe' }
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $taskProject ('BenchmarkResults\' + (Get-Date -Format 'yyyyMMdd-HHmmss')) }
if (-not (Test-Path -LiteralPath $PlayerPath)) { throw 'Build the Windows player first with tools/unity.ps1 -Action Build.' }
if ($Counts | Where-Object { $_ -lt 0 -or $_ -gt 20000 }) { throw 'Counts must be between 0 and 20000.' }
if ($Smoke) { $WarmupSeconds = 1; $CaptureSeconds = 5; $Repeats = 1 }
if (-not $Smoke -and -not $Visible) { throw 'Full rendering captures require -Visible. Hidden-window runs are reserved for -Smoke and are not performance evidence.' }
if ($Repeats -lt 1 -or $Repeats -gt 20 -or $WarmupSeconds -lt 0 -or $WarmupSeconds -gt 300 -or $CaptureSeconds -lt 1 -or $CaptureSeconds -gt 600) { throw 'Parameters exceed supported capture limits.' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path
foreach ($taskCount in $Counts) {
    $taskRunFolder = Join-Path $OutputDirectory "crowd-$taskCount"
    New-Item -ItemType Directory -Path $taskRunFolder -Force | Out-Null
    [pscustomobject]@{startedUtc=[DateTime]::UtcNow.ToString('o');visible=[bool]$Visible;smoke=[bool]$Smoke;
        crowd=$taskCount;workload=$Workload;synchronizedAbilities=[bool]$SynchronizedAbilities;
        warmupSeconds=$WarmupSeconds;captureSeconds=$CaptureSeconds;repeats=$Repeats;
        note='Keep the game visible and unobstructed for rendering measurements.'} |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskRunFolder 'invocation.json')
    $taskArgs = @('-benchmark','-workload',$Workload,'-crowd',$taskCount,'-warmup',$WarmupSeconds,'-seconds',$CaptureSeconds,
        '-repeats',$Repeats,'-results',"`"$taskRunFolder`"",'-quitAfterBenchmark',
        '-screen-width','1920','-screen-height','1080','-screen-fullscreen','0',
        '-logFile',"`"$taskRunFolder\player.log`"")
    if ($SynchronizedAbilities) { $taskArgs += '-synchronizedAbilities' }
    $taskWindowStyle = if ($Visible) { 'Normal' } else { 'Hidden' }
    $taskProcess = Start-Process -FilePath $PlayerPath -ArgumentList $taskArgs -WindowStyle $taskWindowStyle -PassThru
    $taskTimeoutMs = (($WarmupSeconds + $CaptureSeconds) * $Repeats + 120) * 1000
    if (-not $taskProcess.WaitForExit($taskTimeoutMs)) {
        Stop-Process -Id $taskProcess.Id
        throw "Benchmark timed out for $taskCount enemies; see $taskRunFolder\player.log"
    }
    if ($taskProcess.ExitCode -ne 0) { throw "Benchmark failed: exit $($taskProcess.ExitCode)" }
    $taskReports = @(Get-ChildItem -LiteralPath $taskRunFolder -Filter '*-crowd*-r*.json')
    if ($taskReports.Count -ne $Repeats) { throw "Expected $Repeats reports, found $($taskReports.Count). Use a fresh output directory per run." }
    foreach ($taskReport in $taskReports) {
        $taskResult = Get-Content -LiteralPath $taskReport.FullName -Raw | ConvertFrom-Json
        if ($taskResult.crowd -ne $taskCount -or ($Workload -eq 'bosses' -and $taskResult.bossCount -ne $taskCount)) { throw 'Player did not run the requested workload. Rebuild the player.' }
        [pscustomobject]@{Crowd=$taskResult.crowd;Workload=$taskResult.workload;Repeat=$taskResult.repeat;MedianMs=$taskResult.medianMs;P99Ms=$taskResult.p99Ms;MaxMs=$taskResult.maxMs;PeakProjectiles=$taskResult.peakProjectiles;DroppedProjectiles=$taskResult.droppedProjectilesIncludingWarmup;CatchupLimitFrames=$taskResult.catchupLimitFrames;Samples=$taskResult.samples;Report=$taskReport.FullName}
    }
}
Write-Output "Reports: $OutputDirectory"
if ($Smoke) { Write-Output 'Smoke test only: short capture validates reporting, not the 144 FPS requirement.' }
