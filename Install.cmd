@echo off
setlocal
powershell.exe -NoProfile -File "%~dp0scripts\install.ps1"
if errorlevel 1 (
  echo Installation failed. See the message above.
  pause
  exit /b 1
)
echo Installed. Open the desktop shortcut to start all three pets.
pause
