@echo off
title Installing NetSpeed Meter...
echo ========================================
echo     NetSpeed Meter Windows Installer    
echo ========================================
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Installation encountered an issue.
    pause
) else (
    echo.
    echo Setup finished successfully!
    timeout /t 3 /nobreak >nul 2>nul || ping -n 4 127.0.0.1 >nul
)
