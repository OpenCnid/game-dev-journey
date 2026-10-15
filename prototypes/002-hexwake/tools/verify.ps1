param(
    [Parameter(Mandatory = $true)][string]$BuildRoot,
    [Parameter(Mandatory = $true)][string]$OutputRoot,
    [ValidateSet('regressions', 'capture')][string]$Mode = 'regressions',
    [int]$Width = 1280,
    [int]$Height = 720
)
$ErrorActionPreference = 'Stop'
$build = (Resolve-Path $BuildRoot).Path
$output = [IO.Path]::GetFullPath($OutputRoot)
if (Test-Path $output) { throw 'Preserve prior evidence; choose a new OutputRoot.' }
New-Item -ItemType Directory -Force $output | Out-Null
$log = Join-Path $output 'player.log'
$arguments = @('-screen-width', "$Width", '-screen-height', "$Height",
    '-screen-fullscreen', '0', '-logFile', $log, '-rebornOutput', $output)
if ($Mode -eq 'regressions') {
    $tests = Join-Path $build 'tests/Hexwake.Tests.dll'
    if (!(Test-Path $tests)) { throw 'Rebuild with NUnitPath to produce the test assembly.' }
    $arguments += @('-rebornTests', $tests)
}
$quoted = @($arguments | ForEach-Object { '"' + $_ + '"' })
$process = Start-Process (Join-Path $build 'player/Hexwake.exe') -ArgumentList $quoted `
    -WorkingDirectory $build -PassThru
Write-Output "HEXWAKE_VERIFY_STARTED pid=$($process.Id)"
if (!$process.WaitForExit(180000)) {
    $process.Kill()
    throw 'Owned verification player exceeded its three-minute bound.'
}
$process.Refresh()
Get-Content $log
if ($process.ExitCode -ne 0) { throw "Verification player exited $($process.ExitCode)." }
Write-Output "HEXWAKE_VERIFY_EXIT code=$($process.ExitCode)"
exit 0
