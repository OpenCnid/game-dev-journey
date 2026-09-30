param(
    [ValidateSet('Prepare','Test','Build','Open')][string]$Action = 'Prepare',
    [ValidateSet('EditMode','PlayMode')][string]$TestPlatform = 'EditMode',
    [string]$UnityPath = $env:UNITY_EDITOR_PATH
)
$ErrorActionPreference = 'Stop'
$taskVersion = '6000.3.25f1'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskProject = Join-Path $taskRoot 'prototypes\001-stacked-weapons'
$taskOutput = Join-Path $taskProject 'artifacts'
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
if (-not $UnityPath) {
    $taskCandidates = @(
        "$env:LOCALAPPDATA\Unity\$taskVersion\Editor\Unity.exe",
        "$env:ProgramFiles\Unity\Hub\Editor\$taskVersion\Editor\Unity.exe"
    )
    $UnityPath = $taskCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $UnityPath -or -not (Test-Path -LiteralPath $UnityPath)) {
    throw "Install Unity $taskVersion and activate a license in Unity Hub. Pass -UnityPath or set UNITY_EDITOR_PATH if installed elsewhere."
}
$taskLogName = if ($Action -eq 'Test') { "Test-$TestPlatform" } else { $Action }
$taskArguments = @('-projectPath', "`"$taskProject`"", '-logFile', "`"$taskOutput\$taskLogName.log`"")
if ($Action -eq 'Open') {
    # Explicit interactive action requested by the caller.
    Start-Process -FilePath $UnityPath -ArgumentList $taskArguments | Out-Null
    exit 0
}
$taskArguments += '-batchmode'
if ($Action -ne 'Test' -or $TestPlatform -eq 'EditMode') { $taskArguments += '-nographics' }
switch ($Action) {
    'Prepare' { $taskArguments += @('-quit','-executeMethod','Journey.Editor.ProjectSetup.Prepare') }
    'Build' { $taskArguments += @('-quit','-executeMethod','Journey.Editor.ProjectSetup.BuildWindows') }
    'Test' {
        $taskResultPath = Join-Path $taskOutput "$TestPlatform.xml"
        if (Test-Path -LiteralPath $taskResultPath) { Remove-Item -LiteralPath $taskResultPath }
        $taskArguments += @('-runTests','-testPlatform',$TestPlatform,'-testResults',"`"$taskResultPath`"")
    }
}
$taskProcess = Start-Process -FilePath $UnityPath -ArgumentList $taskArguments -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
if ($taskProcess.ExitCode -ne 0) { throw "Unity $Action failed ($($taskProcess.ExitCode)). Read $taskOutput\$taskLogName.log" }
if ($Action -eq 'Test') {
    if (-not (Test-Path -LiteralPath $taskResultPath)) { throw 'Unity returned without writing test results.' }
    [xml]$taskTests = Get-Content -LiteralPath $taskResultPath
    if ($taskTests.'test-run'.result -ne 'Passed') { throw "Tests did not pass: $($taskTests.'test-run'.result)" }
}
Write-Output "Unity $Action succeeded. Log: $taskOutput\$taskLogName.log"
