param([string]$Executable = (Join-Path $PSScriptRoot '..\src\PhysicsLab.App\bin\Release\net10.0-windows\PhysicsLab.exe'))
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    dotnet build PhysicsLab.sln -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    dotnet run --project tests/PhysicsLab.Tests -c Release --no-build -- --report artifacts/core-tests.json
    if ($LASTEXITCODE -ne 0) { throw 'Core tests failed.' }
    $reportDirectory = Join-Path $root 'artifacts\ui'
    $process = Start-Process -FilePath $Executable -ArgumentList @('--verify-ui', ('"' + $reportDirectory + '"')) -WindowStyle Hidden -PassThru -Wait
    if ($process.ExitCode -ne 0) { throw "UI verification failed: $reportDirectory\ui-verification.json" }
    Get-Content -LiteralPath (Join-Path $reportDirectory 'ui-verification.json')
} finally { Pop-Location }
