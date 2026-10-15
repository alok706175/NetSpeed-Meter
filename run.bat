@echo off
setlocal
set "DOTNET_ROOT=%LOCALAPPDATA%\Microsoft\dotnet"
set "PATH=%DOTNET_ROOT%;%PATH%"

echo Starting NetSpeed Meter...
start "" "%~dp0publish\NetSpeedMeter\NetSpeedMeter.exe"
