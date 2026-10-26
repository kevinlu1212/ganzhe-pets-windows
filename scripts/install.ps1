[CmdletBinding()]
param([switch]$NoLaunch)
$ErrorActionPreference = 'Stop'
$petRoot = Split-Path $PSScriptRoot -Parent
$petExe = Join-Path $petRoot 'GanzhePets.exe'
$petPackage = $petRoot
if (-not (Test-Path -LiteralPath $petExe)) {
    & (Join-Path $PSScriptRoot 'build.ps1')
    $petPackage = Join-Path $petRoot 'build\package'
} else {
    & (Join-Path $PSScriptRoot 'prepare-assets.ps1') -ProjectRoot $petRoot
}
$petInstall = Join-Path $env:LOCALAPPDATA 'Programs\GanzhePets'
if (Get-CimInstance Win32_Process -Filter "Name='GanzhePets.exe'" | Where-Object { $_.ExecutablePath -eq (Join-Path $petInstall 'GanzhePets.exe') }) { throw 'Please exit all running GanzhePets windows before installing.' }
New-Item -ItemType Directory -Force -Path $petInstall | Out-Null
foreach ($petItem in @('GanzhePets.exe','assets','licenses','LICENSE','NOTICE.md','使用说明.txt')) {
    Copy-Item -LiteralPath (Join-Path $petPackage $petItem) -Destination $petInstall -Recurse -Force
}
[IO.File]::WriteAllText((Join-Path $petInstall 'uninstall.ps1'),[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'uninstall.ps1')),(New-Object Text.UTF8Encoding($true)))
$petShell = New-Object -ComObject WScript.Shell
$petMenu = Join-Path ([Environment]::GetFolderPath('Programs')) '三人桌宠'
New-Item -ItemType Directory -Force -Path $petMenu | Out-Null
foreach ($petLink in @((Join-Path ([Environment]::GetFolderPath('Desktop')) '三人桌宠.lnk'),(Join-Path $petMenu '三人桌宠.lnk'))) {
    $petShortcut = $petShell.CreateShortcut($petLink)
    $petShortcut.TargetPath = Join-Path $petInstall 'GanzhePets.exe'
    $petShortcut.WorkingDirectory = $petInstall
    $petShortcut.Description = '同时启动薄肌、太对了、好事三个桌宠 · Windows 非官方移植版'
    $petShortcut.Save()
}
$petHelp = $petShell.CreateShortcut((Join-Path $petMenu '使用说明.lnk'))
$petHelp.TargetPath = Join-Path $petInstall '使用说明.txt'
$petHelp.Save()
$petRegistry = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\GanzhePetsLocalPort'
New-Item -Path $petRegistry -Force | Out-Null
$petValues = @{
    DisplayName = '三人桌宠（Windows 本地移植版）'
    DisplayVersion = '1.0.0'
    Publisher = 'kevinlu1212; upstream Ganzhe2028'
    InstallLocation = $petInstall
    DisplayIcon = (Join-Path $petInstall 'GanzhePets.exe')
    UninstallString = ('"' + (Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe') + '" -NoProfile -WindowStyle Hidden -File "' + (Join-Path $petInstall 'uninstall.ps1') + '"')
}
foreach ($petEntry in $petValues.GetEnumerator()) { New-ItemProperty -Path $petRegistry -Name $petEntry.Key -Value $petEntry.Value -PropertyType String -Force | Out-Null }
foreach ($petName in @('NoModify','NoRepair')) { New-ItemProperty -Path $petRegistry -Name $petName -Value 1 -PropertyType DWord -Force | Out-Null }
Write-Output "INSTALLED $petInstall"
if (-not $NoLaunch) { Start-Process -FilePath (Join-Path $petInstall 'GanzhePets.exe') }
