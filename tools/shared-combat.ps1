param(
    [string]$PlayerFolder,
    [string]$Results,
    [ushort]$Port = 7777,
    [int]$Width = 1920,
    [int]$Height = 1080,
    [switch]$Scripted,
    [switch]$ScriptedCompanion,
    [ValidateSet('host','client')][string]$HumanRole = 'host',
    [switch]$QuitAtEnd,
    [switch]$Uncapped
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
if (-not $PlayerFolder) { $PlayerFolder = Join-Path $taskRoot 'prototypes/001-stacked-weapons/build/StackLab' }
$taskPlayer = Join-Path $PlayerFolder 'StackLab.exe'
if (-not (Test-Path -LiteralPath $taskPlayer)) { throw "No player at $taskPlayer. Build with tools/unity.ps1 first." }
if (-not $Results) { $Results = Join-Path $taskRoot ('prototypes/001-stacked-weapons/artifacts/shared-' + (Get-Date -Format 'yyyyMMdd-HHmmss')) }
New-Item -ItemType Directory -Path $Results -Force | Out-Null
$taskResults = (Resolve-Path -LiteralPath $Results).Path
$taskProcesses = @()
foreach ($taskRole in @('host','client')) {
    $taskRoleResults = Join-Path $taskResults $taskRole
    New-Item -ItemType Directory -Path $taskRoleResults -Force | Out-Null
    $taskArgs = @('-screen-fullscreen','0','-screen-width',$Width,'-screen-height',$Height,'-force-d3d11',
        '-port',$Port,'-sharedResults',('"' + $taskRoleResults + '"'),'-logFile',('"' + (Join-Path $taskRoleResults 'player.log') + '"'))
    if ($taskRole -eq 'host') { $taskArgs += '-host' } else { $taskArgs += @('-join','127.0.0.1') }
    if ($Scripted -or ($ScriptedCompanion -and $taskRole -ne $HumanRole)) { $taskArgs += '-sharedScripted' }
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
