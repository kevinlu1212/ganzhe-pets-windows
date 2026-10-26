$ErrorActionPreference = 'Stop'
$petExpectedRoot = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'Programs\GanzhePets'))
$petActualRoot = [IO.Path]::GetFullPath($PSScriptRoot)
if ($petActualRoot -ne $petExpectedRoot) { throw '卸载位置校验失败，未删除任何文件。' }
Add-Type -AssemblyName System.Windows.Forms
$petAnswer = [System.Windows.Forms.MessageBox]::Show('卸载三人桌宠及其安装文件？个人设置会保留。','卸载三人桌宠',[System.Windows.Forms.MessageBoxButtons]::YesNo)
if ($petAnswer -ne [System.Windows.Forms.DialogResult]::Yes) { return }
Get-CimInstance Win32_Process -Filter "Name='GanzhePets.exe'" | Where-Object { $_.ExecutablePath -eq (Join-Path $petExpectedRoot 'GanzhePets.exe') } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }
$petDesktopShortcut = Join-Path ([Environment]::GetFolderPath('Desktop')) '三人桌宠.lnk'
if (Test-Path -LiteralPath $petDesktopShortcut) { Remove-Item -LiteralPath $petDesktopShortcut }
$petMenuRoot = [IO.Path]::GetFullPath([Environment]::GetFolderPath('Programs'))
$petMenu = [IO.Path]::GetFullPath((Join-Path $petMenuRoot '三人桌宠'))
if (-not $petMenu.StartsWith($petMenuRoot + [IO.Path]::DirectorySeparatorChar)) { throw '开始菜单位置校验失败。' }
if (Test-Path -LiteralPath $petMenu) { Remove-Item -LiteralPath $petMenu -Recurse -Force }
Remove-Item -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\GanzhePetsLocalPort' -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $petExpectedRoot -Recurse -Force
