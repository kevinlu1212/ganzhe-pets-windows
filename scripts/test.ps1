[CmdletBinding()]
param([string]$PackagePath = '', [string]$ResultsPath = '')
$ErrorActionPreference = 'Stop'
$petRoot = Split-Path $PSScriptRoot -Parent
if (-not $PackagePath) { $PackagePath = Join-Path $petRoot 'build\package' }
if (-not $ResultsPath) { $ResultsPath = Join-Path $petRoot 'test-results' }
$petExe = Join-Path $PackagePath 'GanzhePets.exe'
if (-not (Test-Path -LiteralPath $petExe)) { throw 'Build first using scripts/build.ps1.' }
New-Item -ItemType Directory -Force -Path $ResultsPath | Out-Null
foreach ($petMode in @('self-test','integration-test')) {
    # A unique report name prevents stale successes from masking a failed run.
    $petReport = Join-Path $ResultsPath ($petMode + '-' + [guid]::NewGuid().ToString('N') + '.json')
    $petProcess = Start-Process -FilePath $petExe -ArgumentList @(('--' + $petMode),'--report',('"' + $petReport + '"')) -WindowStyle Hidden -PassThru
    if (-not $petProcess.WaitForExit(360000)) { $petProcess.Kill(); throw "$petMode timed out." }
    if ($petProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $petReport)) { throw "$petMode failed. Inspect the local error log." }
    $petResult = Get-Content -LiteralPath $petReport -Raw -Encoding UTF8 | ConvertFrom-Json
    if (-not $petResult.pass) { throw "$petMode reported failure: $petReport" }
    if ($petMode -eq 'self-test' -and ($petResult.audioFiles -ne 76 -or $petResult.images -ne 3)) { throw 'Incomplete media verification.' }
    Write-Output "PASS $petMode $petReport"
}
Write-Output 'ALL_TESTS_PASSED'
