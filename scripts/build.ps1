param(
    [string]$Output = (Join-Path $PSScriptRoot '..\artifacts\publish'),
    [switch]$CopyToDesktop
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot
try {
    dotnet run --project tests/PhysicsLab.Tests -c Release -- --report artifacts/core-tests.json
    if ($LASTEXITCODE -ne 0) { throw 'Physics tests failed.' }
    dotnet publish src/PhysicsLab.App/PhysicsLab.App.csproj -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true -p:DebugType=embedded -o $Output
    if ($LASTEXITCODE -ne 0) { throw 'Publishing failed.' }
    $exe = Join-Path $Output 'PhysicsLab.exe'
    if (!(Test-Path -LiteralPath $exe)) { throw 'Published executable missing.' }
    Get-FileHash -Algorithm SHA256 -LiteralPath $exe
    if ($CopyToDesktop) {
        $destination = Join-Path ([Environment]::GetFolderPath('Desktop')) '绳索与粒子实验室.exe'
        if (Test-Path -LiteralPath $destination) {
            $backup = $destination + '.backup-' + (Get-Date -Format 'yyyyMMdd-HHmmss')
            Copy-Item -LiteralPath $destination -Destination $backup
        }
        Copy-Item -LiteralPath $exe -Destination $destination
        Write-Output "Desktop executable: $destination"
    }
} finally { Pop-Location }
