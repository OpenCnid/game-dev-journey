param(
    [string]$PlayerFolder,
    [string]$Results,
    [ValidateSet('solo-precision','shared-rifle','developer-parity','earned-growth','weapon-stack')][string]$Scenario = 'shared-rifle',
    [ValidateSet('none','stronger','faster')][string]$HostChoice = 'none',
    [ValidateSet('none','stronger','faster')][string]$ClientChoice = 'none',
    [ValidateSet('none','broad','scatter','pierce')][string]$HostStackPath = 'none',
    [ValidateSet('none','broad','scatter','pierce')][string]$ClientStackPath = 'none',
    [ushort]$Port = 7777,
    [int]$Width = 1920,
    [int]$Height = 1080,
    [switch]$Scripted,
    [switch]$ScriptedCompanion,
    [ValidateSet('host','client')][string]$HumanRole = 'host',
    [switch]$QuitAtEnd,
    [switch]$Uncapped,
    [switch]$Background,
    [switch]$HumanHost,
    [switch]$WaitForJoin,
    [string]$PythonPath
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
if (-not $PlayerFolder) { $PlayerFolder = Join-Path $taskRoot 'prototypes/001-stacked-weapons/build/StackLab' }
$taskPlayer = Join-Path $PlayerFolder 'StackLab.exe'
if (($HumanHost -and -not $Background) -or ($WaitForJoin -and -not $HumanHost)) { throw 'HumanHost requires Background; WaitForJoin requires HumanHost.' }
if (-not (Test-Path -LiteralPath $taskPlayer)) { throw "No player at $taskPlayer. Build with tools/unity.ps1 first." }
if (-not $Results) { $Results = Join-Path $taskRoot ('prototypes/001-stacked-weapons/artifacts/shared-' + (Get-Date -Format 'yyyyMMdd-HHmmss')) }
if ($Background) {
    if ($HostStackPath -ne 'none' -or $ClientStackPath -ne 'none') { throw 'Use the background companion --stack-path policy or explicit viewer choices.' }
    if ($ScriptedCompanion -or $QuitAtEnd) { throw 'Background preview starts both scripted players and exposes per-player controls in its viewer.' }
    if (-not $PythonPath) {
        $PythonPath = Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
        if (-not (Test-Path -LiteralPath $PythonPath)) { throw 'Pass -PythonPath for a Python 3 interpreter. No installation is required by the launcher.' }
    }
    $taskResults = [IO.Path]::GetFullPath($Results)
    if (Test-Path -LiteralPath $taskResults) { throw 'Use a new Results directory for every background session.' }
    New-Item -ItemType Directory -Path (Split-Path -Parent $taskResults) -Force | Out-Null
    $taskLauncher = Join-Path $PSScriptRoot 'background_desktop.py'
    $taskArgs = @(('"' + $taskLauncher + '"'),'--player-folder',('"' + [IO.Path]::GetFullPath($PlayerFolder) + '"'),
        '--results',('"' + $taskResults + '"'),'--port',$Port,'--width',$Width,'--height',$Height)
    $taskArgs += @('--scenario',$Scenario)
    if ($Uncapped) { $taskArgs += '--uncapped' }
    if ($HumanHost) { $taskArgs += '--human-host' }
    if ($WaitForJoin) { $taskArgs += '--wait-for-join' }
    $taskProcess = Start-Process -FilePath $PythonPath -ArgumentList $taskArgs -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput ($taskResults + '-launcher.log') -RedirectStandardError ($taskResults + '-launcher-error.log')
    $taskDeadline = (Get-Date).AddSeconds(30)
    while (-not (Test-Path -LiteralPath (Join-Path $taskResults 'invocation.json')) -and -not $taskProcess.HasExited -and (Get-Date) -lt $taskDeadline) { Start-Sleep -Milliseconds 200 }
    if (-not (Test-Path -LiteralPath (Join-Path $taskResults 'invocation.json'))) { throw "Background launch failed or timed out. Read $taskResults-launcher-error.log" }
    $taskInvocation = Get-Content -Raw -LiteralPath (Join-Path $taskResults 'invocation.json') | ConvertFrom-Json
    Write-Output "Background pair started. Viewer: $($taskInvocation.viewer)"
    Write-Output "Launcher process: $($taskProcess.Id). Results: $taskResults"
    return
}
New-Item -ItemType Directory -Path $Results -Force | Out-Null
$taskResults = (Resolve-Path -LiteralPath $Results).Path
$taskProcesses = @()
foreach ($taskRole in @('host','client')) {
    $taskRoleResults = Join-Path $taskResults $taskRole
    New-Item -ItemType Directory -Path $taskRoleResults -Force | Out-Null
    $taskArgs = @('-screen-fullscreen','0','-screen-width',$Width,'-screen-height',$Height,'-force-d3d11',
        '-port',$Port,'-scenario',$Scenario,'-sharedResults',('"' + $taskRoleResults + '"'),'-logFile',('"' + (Join-Path $taskRoleResults 'player.log') + '"'))
    if ($taskRole -eq 'host') { $taskArgs += @('-host','-sharedLoopback') } else { $taskArgs += @('-join','127.0.0.1') }
    if ($Scripted -or ($ScriptedCompanion -and $taskRole -ne $HumanRole)) { $taskArgs += '-sharedScripted' }
    $taskChoice = if ($taskRole -eq 'host') { $HostChoice } else { $ClientChoice }
    if ($taskChoice -ne 'none') { $taskArgs += @('-growthChoice',$taskChoice) }
    $taskStackPath = if ($taskRole -eq 'host') { $HostStackPath } else { $ClientStackPath }
    if ($taskStackPath -ne 'none') { $taskArgs += @('-stackPath',$taskStackPath) }
    if ($QuitAtEnd) { $taskArgs += '-sharedQuit' }
    if ($Uncapped) { $taskArgs += '-sharedUncapped' }
    # The owner explicitly requested opening two interactive games. Keep both visible.
    $taskProcess = Start-Process -FilePath $taskPlayer -ArgumentList $taskArgs -PassThru
    $taskProcesses += @{ role = $taskRole; pid = $taskProcess.Id; arguments = $taskArgs }
}
@{ player = (Resolve-Path -LiteralPath $taskPlayer).Path; started = (Get-Date).ToString('o');
    connection = 'two processes on one desktop, IPv4 loopback UDP'; width = $Width; height = $Height;
    scripted = [bool]$Scripted; scriptedCompanion = [bool]$ScriptedCompanion; humanRole = $HumanRole;
    uncapped = [bool]$Uncapped; processes = $taskProcesses;
    visibility = 'Verify both unobstructed windows before treating capture as performance evidence.'
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $taskResults 'invocation.json') -Encoding utf8
Write-Output "Opened host A and client B. Results: $taskResults"
$taskProcesses | ForEach-Object { Write-Output "$($_.role): process $($_.pid)" }
