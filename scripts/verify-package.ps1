[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$ArchivePath)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression,System.IO.Compression.FileSystem
$petArchive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $ArchivePath).Path)
try {
    $petEntries = @($petArchive.Entries | Select-Object -ExpandProperty FullName)
    $petForbidden = @($petEntries | Where-Object { $_ -match '(?i)(\.(wav|m4a|png|log|lnk)$|(^|/)assets/|(^|/)\.cache/|(^|/)test-results/|\\|(^|/)\.\./)' })
    if ($petForbidden.Count) { throw ('Unexpected release contents: ' + ($petForbidden -join ', ')) }
    foreach ($petRequired in @('GanzhePets.exe','Install.cmd','scripts/install.ps1','scripts/uninstall.ps1','scripts/prepare-assets.ps1','upstream.lock.json','LICENSE','NOTICE.md','README.md')) {
        if ($petRequired -notin $petEntries) { throw "Missing release file: $petRequired" }
    }
    Write-Output "PACKAGE_VERIFIED $($petEntries.Count) files; no original media or local data."
} finally { $petArchive.Dispose() }
