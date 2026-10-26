[CmdletBinding()]
param([string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$petLock = Get-Content -LiteralPath (Join-Path $ProjectRoot 'upstream.lock.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($petLock.schemaVersion -ne 1) { throw 'Unsupported asset lock schema.' }
$petCache = Join-Path $ProjectRoot '.cache\upstream'
New-Item -ItemType Directory -Force -Path $petCache | Out-Null
foreach ($petSource in $petLock.sources) {
    if ($petSource.repository -notmatch '^Ganzhe2028/(SunPet|ShaolanPet|FengGe-Desktop-Pet)$' -or $petSource.commit -notmatch '^[a-f0-9]{40}$' -or $petSource.directory -notin @('SunPet','ShaolanPet','FengGe-Desktop-Pet')) { throw 'Invalid upstream source in lock file.' }
    $petTarget = Join-Path $ProjectRoot ('assets\' + $petSource.directory)
    $petVerified = $true
    foreach ($petFile in $petSource.files) {
        if ($petFile.path -notmatch '^(face\.png|clips\.json|Clips/[0-9]{2}\.(wav|m4a))$' -or $petFile.sha256 -notmatch '^[a-f0-9]{64}$') { throw 'Invalid asset entry in lock file.' }
        $petPath = Join-Path $petTarget $petFile.path
        if (-not (Test-Path -LiteralPath $petPath) -or (Get-FileHash -LiteralPath $petPath -Algorithm SHA256).Hash -ne $petFile.sha256) { $petVerified = $false }
    }
    if ($petVerified) { Write-Output "Verified cached assets: $($petSource.directory)"; continue }
    $petArchive = Join-Path $petCache ($petSource.directory + '-' + $petSource.commit + '.zip')
    if (-not (Test-Path -LiteralPath $petArchive)) {
        $petDownload = $petArchive + '.download'
        $petUrl = 'https://api.github.com/repos/' + $petSource.repository + '/zipball/' + $petSource.commit
        Write-Output "Downloading $($petSource.repository) at $($petSource.commit)"
        Invoke-WebRequest -UseBasicParsing -Headers @{ 'User-Agent' = 'GanzhePets-Windows-Asset-Preparation' } -Uri $petUrl -OutFile $petDownload
        Move-Item -LiteralPath $petDownload -Destination $petArchive -Force
    }
    $petExtraction = Join-Path $petCache ($petSource.directory + '-' + $petSource.commit)
    Expand-Archive -LiteralPath $petArchive -DestinationPath $petExtraction -Force
    $petRoots = @(Get-ChildItem -LiteralPath $petExtraction -Directory)
    if ($petRoots.Count -ne 1) { throw 'Unexpected upstream archive layout.' }
    foreach ($petFile in $petSource.files) {
        $petInput = Join-Path $petRoots[0].FullName ('Assets\' + $petFile.path)
        if (-not (Test-Path -LiteralPath $petInput) -or (Get-FileHash -LiteralPath $petInput -Algorithm SHA256).Hash -ne $petFile.sha256) { throw "Asset missing or hash mismatch: $($petSource.directory)/$($petFile.path)" }
        $petOutput = Join-Path $petTarget $petFile.path
        New-Item -ItemType Directory -Force -Path (Split-Path $petOutput -Parent) | Out-Null
        Copy-Item -LiteralPath $petInput -Destination $petOutput -Force
    }
    Write-Output "Prepared and verified: $($petSource.directory)"
}
Write-Output 'ASSETS_OK'
