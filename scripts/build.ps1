[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$petRoot = Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'prepare-assets.ps1') -ProjectRoot $petRoot
$petPackage = Join-Path $petRoot 'build\package'
New-Item -ItemType Directory -Force -Path $petPackage | Out-Null
Copy-Item -LiteralPath (Join-Path $petRoot 'assets') -Destination $petPackage -Recurse -Force
Copy-Item -LiteralPath (Join-Path $petRoot 'licenses') -Destination $petPackage -Recurse -Force
foreach ($petFile in @('LICENSE','NOTICE.md','使用说明.txt')) { Copy-Item -LiteralPath (Join-Path $petRoot $petFile) -Destination $petPackage -Force }
$petFramework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if (-not (Test-Path -LiteralPath (Join-Path $petFramework 'csc.exe'))) { $petFramework = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319' }
$petCompiler = Join-Path $petFramework 'csc.exe'
if (-not (Test-Path -LiteralPath $petCompiler)) { throw '.NET Framework C# compiler not found.' }
$petRefs = @('PresentationCore','PresentationFramework','WindowsBase') | ForEach-Object { '/reference:' + (Join-Path $petFramework "WPF\$_.dll") }
$petArgs = @('/nologo','/target:winexe','/platform:anycpu','/optimize+','/utf8output','/reference:System.Web.Extensions.dll','/reference:System.Core.dll','/reference:System.Xaml.dll',('/out:' + (Join-Path $petPackage 'GanzhePets.exe'))) + $petRefs + (Join-Path $petRoot 'src\Pets.cs')
& $petCompiler @petArgs
if ($LASTEXITCODE -ne 0) { throw 'C# compilation failed.' }
Write-Output "BUILD_OK $petPackage"
