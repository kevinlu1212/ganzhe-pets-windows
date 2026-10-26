[CmdletBinding()]
param([ValidatePattern('^\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?$')][string]$Version = '1.0.0')
$ErrorActionPreference = 'Stop'
$petRoot = Split-Path $PSScriptRoot -Parent
$petBuild = Join-Path $petRoot 'build\package'
if (-not (Test-Path -LiteralPath (Join-Path $petBuild 'GanzhePets.exe'))) { & (Join-Path $PSScriptRoot 'build.ps1') }
$petDist = Join-Path $petRoot 'dist'
$petStage = Join-Path $petDist ('stage-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $petDist,$petStage,(Join-Path $petStage 'scripts') | Out-Null
Copy-Item -LiteralPath (Join-Path $petBuild 'GanzhePets.exe') -Destination $petStage
foreach ($petItem in @('README.md','LICENSE','NOTICE.md','CHANGELOG.md','使用说明.txt','Install.cmd','upstream.lock.json','licenses')) {
    Copy-Item -LiteralPath (Join-Path $petRoot $petItem) -Destination $petStage -Recurse -Force
}
# Include only installation tools. No original assets, build cache or local logs.
foreach ($petScript in @('install.ps1','uninstall.ps1','prepare-assets.ps1')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $petScript) -Destination (Join-Path $petStage 'scripts')
}
$petZip = Join-Path $petDist ("GanzhePets-Windows-$Version.zip")
Add-Type -AssemblyName System.IO.Compression,System.IO.Compression.FileSystem
$petStream = [IO.File]::Open($petZip, [IO.FileMode]::Create)
$petArchive = New-Object IO.Compression.ZipArchive($petStream, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($petFile in Get-ChildItem -LiteralPath $petStage -File -Recurse | Sort-Object FullName) {
        $petRelative = $petFile.FullName.Substring($petStage.Length + 1).Replace('\', '/')
        [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($petArchive, $petFile.FullName, $petRelative, [IO.Compression.CompressionLevel]::Optimal)
    }
} finally { $petArchive.Dispose(); $petStream.Dispose() }
& (Join-Path $PSScriptRoot 'verify-package.ps1') -ArchivePath $petZip
$petHash = (Get-FileHash -LiteralPath $petZip -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText(($petZip + '.sha256'),($petHash + '  ' + [IO.Path]::GetFileName($petZip) + [Environment]::NewLine),(New-Object Text.UTF8Encoding($false)))
Write-Output "PACKAGE_OK $petZip"
Write-Output "SHA256 $petHash"
