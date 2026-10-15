param(
    [Parameter(Mandatory=$true)][string]$Name,
    [int]$Duration = 90,
    [switch]$Diagnostics,
    [switch]$AllowContended
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$project = Join-Path $repo 'prototypes/001-stacked-weapons'
$player = (Resolve-Path (Join-Path $project 'build/StackLab-GDJ24-v3-diagnosis/StackLab.exe')).Path
$inventory = @(Get-CimInstance Win32_Process | Where-Object { $_.Name -in @('StackLab.exe','Unity.exe','Vampire Hunters.exe') } | Select-Object ProcessId,Name,ExecutablePath)
if ($inventory | Where-Object { $_.Name -eq 'StackLab.exe' -or ($_.Name -eq 'Unity.exe' -and $_.ExecutablePath -notlike '*UnityHub*') }) { throw 'An existing StackLab or Unity Editor is running; inspect ownership before proceeding.' }
if (!$AllowContended -and ($inventory | Where-Object { $_.Name -eq 'Vampire Hunters.exe' })) { throw 'Owner game present; obtain permission to close or explicitly label a contended diagnostic.' }
$root = Join-Path $project ('artifacts/gdj24-publication-' + $Name)
if (Test-Path -LiteralPath $root) { throw 'Capture directory already exists; preserve it.' }
New-Item -ItemType Directory -Path $root | Out-Null
$inventory | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $root 'process-preflight.json') -Encoding utf8
$owned = @()
try {
    foreach ($role in @('host','client')) {
        $output = Join-Path $root $role
        New-Item -ItemType Directory -Path $output | Out-Null
        $arguments = @('-scenario','co-op-scale','-scenarioEnemies','1000','-scaleWeapons','8','-scenarioDuration',"$Duration",'-sharedSteering','grid','-sharedScripted','-sharedUncapped','-sharedMute','-force-d3d11','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-port','7798','-sharedResults',$output,'-logFile',(Join-Path $output 'player.log'))
        if ($Diagnostics) { $arguments += '-sharedDiagnostics' }
        if ($role -eq 'host') { $arguments += @('-host','-sharedLoopback') } else { $arguments += @('-join','127.0.0.1') }
        # Visible native players are explicitly requested for these captures.
        $child = Start-Process -FilePath $player -ArgumentList ($arguments | ForEach-Object { if ($_ -match '\s') { '"' + $_ + '"' } else { $_ } }) -PassThru
        $owned += $child
        @{player=$player;pid=$child.Id;role=$role;args=$arguments;startedUtc=[DateTime]::UtcNow.ToString('o');classification='S7 diagnostic; owner processes untouched; UI observations recorded separately; not acceptance';diagnostics=[bool]$Diagnostics;allowContended=[bool]$AllowContended} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'invocation.json') -Encoding utf8
        'elapsed_seconds,cpu_seconds,working_set_bytes,private_bytes,get_process_ms' | Set-Content -LiteralPath (Join-Path $output 'process-memory.csv') -Encoding utf8
        Start-Sleep -Milliseconds 500
    }
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $sampleAt = 0
    $finishedAt = -1
    while ($watch.Elapsed.TotalSeconds -lt ($Duration + 45)) {
        if ($watch.Elapsed.TotalSeconds -ge $sampleAt) {
            foreach ($role in @('host','client')) {
                $index = [int]($role -eq 'client')
                $cost = [Diagnostics.Stopwatch]::StartNew()
                $observed = Get-Process -Id $owned[$index].Id -ErrorAction SilentlyContinue
                $cost.Stop()
                if ($observed) { '{0:F6},{1:F6},{2},{3},{4:F6}' -f $watch.Elapsed.TotalSeconds,$observed.CPU,$observed.WorkingSet64,$observed.PrivateMemorySize64,$cost.Elapsed.TotalMilliseconds | Add-Content -LiteralPath (Join-Path $root "$role/process-memory.csv") -Encoding utf8 }
            }
            $sampleAt += 10
        }
        $done = $true
        foreach ($role in @('host','client')) {
            $end = if ($Diagnostics) { 'terminal-after-2s.json' } else { 'summary-run-1.json' }
            if (!(Test-Path -LiteralPath (Join-Path $root "$role/$end"))) { $done = $false }
        }
        if ($done -and $finishedAt -lt 0) { $finishedAt = $watch.Elapsed.TotalSeconds }
        if ($finishedAt -ge 0 -and $watch.Elapsed.TotalSeconds -ge ($finishedAt + 2)) { break }
        Start-Sleep -Milliseconds 500
    }
    @{elapsedSeconds=$watch.Elapsed.TotalSeconds;bothReportsObserved=($finishedAt -ge 0);timedOut=($finishedAt -lt 0);stoppedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'completion.json') -Encoding utf8
} finally {
    foreach ($child in $owned) {
        $observed = Get-CimInstance Win32_Process -Filter "ProcessId=$($child.Id)" -ErrorAction SilentlyContinue
        if ($observed -and $observed.ExecutablePath -eq $player) { Stop-Process -Id $child.Id }
    }
}
Write-Output $root
