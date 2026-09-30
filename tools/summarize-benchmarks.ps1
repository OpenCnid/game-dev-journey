param([Parameter(Mandatory=$true)][string]$InputDirectory)
$ErrorActionPreference = 'Stop'
Get-ChildItem -LiteralPath $InputDirectory -Recurse -Filter '*-crowd*-r*.json' | ForEach-Object {
    $taskReport = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
    if ($taskReport.schema -ne 'journey-benchmark-v3' -or $taskReport.profilerCapture) { return }
    [pscustomobject]@{
        Mode = "$($taskReport.simulationBackend)/$($taskReport.renderingBackend)"
        Crowd = $taskReport.crowd
        Repeat = $taskReport.repeat
        AverageFps = [math]::Round($taskReport.averageFps, 2)
        P99Ms = [math]::Round($taskReport.p99Ms, 3)
        MaxMs = [math]::Round($taskReport.maxMs, 3)
        FramesOverBudget = $taskReport.framesOverBudget
        CrowdMsPerTick = [math]::Round($taskReport.meanCrowdMsPerTick, 3)
        ProjectileMsPerTick = [math]::Round($taskReport.meanProjectileMsPerTick, 3)
        RenderPrepareMs = [math]::Round($taskReport.meanRenderPrepareMs, 3)
        RenderSubmitMs = [math]::Round($taskReport.meanRenderSubmitMs, 3)
        GpuMs = if ($taskReport.gpuTimingAvailable) { [math]::Round($taskReport.meanGpuFrameMs, 3) } else { $null }
        CatchupFrames = $taskReport.catchupLimitFrames
        DroppedProjectiles = $taskReport.droppedProjectilesIncludingWarmup
        SimulationSeconds = [math]::Round($taskReport.simulatedCaptureSeconds, 3)
        ActualCaptureSeconds = [math]::Round($taskReport.actualCaptureSeconds, 3)
        WithinP99Budget = $taskReport.p99Ms -le (1000.0 / 144)
        SimulationKeptUp = $taskReport.catchupLimitFrames -eq 0 -and $taskReport.droppedProjectilesIncludingWarmup -eq 0 -and [math]::Abs($taskReport.actualCaptureSeconds - $taskReport.simulatedCaptureSeconds) -lt .05
        Report = $_.FullName
    }
}
