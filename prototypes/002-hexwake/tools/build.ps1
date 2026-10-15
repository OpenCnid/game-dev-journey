param(
    [Parameter(Mandatory = $true)][string]$TemplatePath,
    [Parameter(Mandatory = $true)][string]$UnityEditor,
    [Parameter(Mandatory = $true)][string]$OutputRoot,
    [string]$NUnitPath
)
$ErrorActionPreference = 'Stop'
function FileHash([string]$path) {
    $sha = [Security.Cryptography.SHA256]::Create()
    $stream = [IO.File]::OpenRead($path)
    try { return [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '').ToLowerInvariant() }
    finally { $stream.Dispose(); $sha.Dispose() }
}
$prototype = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$template = (Resolve-Path $TemplatePath).Path
$editor = (Resolve-Path $UnityEditor).Path
$output = [IO.Path]::GetFullPath($OutputRoot)
if (Test-Path $output) { throw 'Preserve previous builds; choose a new OutputRoot.' }
$version = (Get-Item (Join-Path $editor 'Unity.exe')).VersionInfo.ProductVersion
if (!$version.StartsWith('6000.6.3')) { throw "Unity 6000.6.3f1 is required, found $version." }
$dotnet = Join-Path $editor 'Data/DotNetSdk/dotnet.exe'
$compiler = Get-ChildItem (Join-Path $editor 'Data/DotNetSdk/sdk') -Recurse -Filter 'csc.dll' |
    Sort-Object FullName | Select-Object -First 1
if (!$compiler) { throw 'The pinned Unity bundled Roslyn compiler is missing.' }
$templateName = if (Test-Path (Join-Path $template 'Hexwake.exe')) { 'Hexwake' }
    elseif (Test-Path (Join-Path $template 'CrucibleClarity.exe')) { 'CrucibleClarity' }
    else { throw 'Use an extracted Hexwake release or its verified J/R6 template.' }
$player = Join-Path $output 'player'
New-Item -ItemType Directory -Force $player | Out-Null
foreach ($file in Get-ChildItem $template -Recurse -File) {
    $relative = $file.FullName.Substring($template.Length + 1)
    if ($relative -match '(?i)\.pdb$|\.log$|BurstDebugInformation') { continue }
    $destination = Join-Path $player $relative
    New-Item -ItemType Directory -Force (Split-Path $destination -Parent) | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $destination
}
if ($templateName -ne 'Hexwake') {
    Rename-Item (Join-Path $player ($templateName + '.exe')) 'Hexwake.exe'
    Rename-Item (Join-Path $player ($templateName + '_Data')) 'Hexwake_Data'
}
$manifest = Get-Content -Raw (Join-Path $prototype 'source-manifest.json') | ConvertFrom-Json
$managed = Join-Path $player 'Hexwake_Data/Managed'
$refs = @(Get-ChildItem $managed -Filter '*.dll' |
    Where-Object { !$_.Name.StartsWith('GameDevHarness.') } |
    ForEach-Object { '/reference:' + $_.FullName })
$runtimeSources = @($manifest.sources | Where-Object role -eq 'runtime' |
    ForEach-Object { Join-Path $prototype $_.path })
$runtime = Join-Path $managed 'GameDevHarness.SwordTrial.Runtime.dll'
$runtimeArgs = @($compiler.FullName, '/nologo', '/target:library', '/nostdlib+',
    '/langversion:latest', "/out:$runtime") + $refs + $runtimeSources
& $dotnet @runtimeArgs
if ($LASTEXITCODE -ne 0) { throw "Runtime compilation failed: $LASTEXITCODE" }
if ($NUnitPath) {
    $nunit = (Resolve-Path $NUnitPath).Path
    $tests = Join-Path $output 'tests'
    New-Item -ItemType Directory $tests | Out-Null
    Copy-Item $nunit (Join-Path $tests 'nunit.framework.dll')
    $testDll = Join-Path $tests 'Hexwake.Tests.dll'
    $testSources = @($manifest.sources | Where-Object role -eq 'test' |
        ForEach-Object { Join-Path $prototype $_.path })
    $testArgs = @($compiler.FullName, '/nologo', '/target:library', '/nostdlib+',
        '/langversion:latest', "/out:$testDll", "/reference:$runtime",
        "/reference:$nunit") + $refs + $testSources
    & $dotnet @testArgs
    if ($LASTEXITCODE -ne 0) { throw "Test compilation failed: $LASTEXITCODE" }
}
Copy-Item (Join-Path $prototype 'PLAY.txt') (Join-Path $player 'README.txt')
Copy-Item (Join-Path $prototype 'THIRD_PARTY_NOTICES.md') $player
Copy-Item (Join-Path $prototype 'MONO-LICENSE.txt') $player
@{
    schemaVersion = 1
    name = 'Hexwake'
    version = $manifest.version
    unity = $manifest.unityVersion
    freshUnityProjectExport = $false
    runtimeSha256 = FileHash $runtime
    sources = @($manifest.sources | ForEach-Object {
        @{ path = $_.path; role = $_.role
           sha256 = FileHash (Join-Path $prototype $_.path) }
    })
    package = @(Get-ChildItem $player -Recurse -File | ForEach-Object {
        @{ path = $_.FullName.Substring($player.Length + 1).Replace('\', '/')
           sha256 = FileHash $_.FullName }
    })
} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'build-info.json') -Encoding UTF8
Write-Output "HEXWAKE_BUILD_PASS player=$player sources=$($runtimeSources.Count)"
exit 0
