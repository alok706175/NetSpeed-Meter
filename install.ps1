<#
.SYNOPSIS
    Installs NetSpeed Meter for the current Windows user without requiring Administrator privileges.
#>

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "     NetSpeed Meter Windows Installer   " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Ensure DOTNET_ROOT is registered in User environment
$dotnetPath = "$env:LOCALAPPDATA\Microsoft\dotnet"
if (Test-Path $dotnetPath) {
    [Environment]::SetEnvironmentVariable("DOTNET_ROOT", $dotnetPath, "User")
    $env:DOTNET_ROOT = $dotnetPath
    $env:PATH = "$dotnetPath;$env:PATH"
}

# Close any running instance
Write-Host "[1/5] Checking for running instances..." -ForegroundColor Yellow
Stop-Process -Name "NetSpeedMeter" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# Define Target Installation Directory
$installDir = "$env:LOCALAPPDATA\Programs\NetSpeedMeter"
Write-Host "[2/5] Installing to: $installDir" -ForegroundColor Yellow

if (-not (Test-Path $installDir)) {
    New-Item -ItemType Directory -Force -Path $installDir | Out-Null
}

Write-Host "[2/5] Building & Installing latest NetSpeed Meter..." -ForegroundColor Yellow
& dotnet publish "$PSScriptRoot\NetSpeedMeter\NetSpeedMeter.csproj" -c Release -r win-x64 --self-contained false -o "$installDir"
if (-not (Test-Path "$installDir\NetSpeedMeter.exe")) {
    throw "Build failed: NetSpeedMeter.exe was not created in $installDir"
}

$publishDir = "$PSScriptRoot\publish\NetSpeedMeter"
if (-not (Test-Path $publishDir)) { New-Item -ItemType Directory -Force -Path $publishDir | Out-Null }
Copy-Item -Path "$installDir\*" -Destination $publishDir -Recurse -Force

# Create Uninstall Script
$uninstallScript = @"
`$ErrorActionPreference = "SilentlyContinue"
Write-Host "Uninstalling NetSpeed Meter..." -ForegroundColor Yellow
Stop-Process -Name "NetSpeedMeter" -Force
Start-Sleep -Seconds 1

# Remove Shortcuts
`$desktopShortcut = [System.IO.Path]::Combine([Environment]::GetFolderPath("Desktop"), "NetSpeed Meter.lnk")
`$startMenuShortcut = [System.IO.Path]::Combine([Environment]::GetFolderPath("Programs"), "NetSpeed Meter.lnk")
Remove-Item `$desktopShortcut -Force
Remove-Item `$startMenuShortcut -Force

# Remove Registry Entry
Remove-Item -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\NetSpeedMeter" -Recurse -Force
Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "NetSpeedMeter" -Force

# Self-delete directory after exit
Start-Process cmd.exe -ArgumentList "/c timeout /t 2 & rmdir /s /q `"`$PSScriptRoot`"" -WindowStyle Hidden
Write-Host "NetSpeed Meter uninstalled successfully." -ForegroundColor Green
"@

Set-Content -Path "$installDir\uninstall.ps1" -Value $uninstallScript -Force

# Create uninstaller batch helper
$uninstallBat = @"
@echo off
powershell -ExecutionPolicy Bypass -File "%~dp0uninstall.ps1"
"@
Set-Content -Path "$installDir\uninstall.bat" -Value $uninstallBat -Force

Write-Host "[3/5] Creating Windows Shortcuts..." -ForegroundColor Yellow
$wshShell = New-Object -ComObject WScript.Shell

$exePath = "$installDir\NetSpeedMeter.exe"
$iconPath = "$installDir\Assets\app.ico"

# Desktop Shortcut
$desktopPath = [Environment]::GetFolderPath("Desktop")
$desktopShortcut = $wshShell.CreateShortcut("$desktopPath\NetSpeed Meter.lnk")
$desktopShortcut.TargetPath = $exePath
$desktopShortcut.WorkingDirectory = $installDir
$desktopShortcut.IconLocation = "$iconPath,0"
$desktopShortcut.Description = "NetSpeed Meter - Windows 11 Live Network Speed Monitor"
$desktopShortcut.Save()

# Start Menu Shortcut
$startMenuPath = [Environment]::GetFolderPath("Programs")
$startMenuShortcut = $wshShell.CreateShortcut("$startMenuPath\NetSpeed Meter.lnk")
$startMenuShortcut.TargetPath = $exePath
$startMenuShortcut.WorkingDirectory = $installDir
$startMenuShortcut.IconLocation = "$iconPath,0"
$startMenuShortcut.Description = "NetSpeed Meter - Windows 11 Live Network Speed Monitor"
$startMenuShortcut.Save()

Write-Host "[4/5] Registering with Windows Apps & Features..." -ForegroundColor Yellow
$uninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\NetSpeedMeter"
if (-not (Test-Path $uninstallKey)) {
    New-Item -Path $uninstallKey -Force | Out-Null
}

Set-ItemProperty -Path $uninstallKey -Name "DisplayName" -Value "NetSpeed Meter"
Set-ItemProperty -Path $uninstallKey -Name "DisplayVersion" -Value "1.0.0"
Set-ItemProperty -Path $uninstallKey -Name "Publisher" -Value "NetSpeed Meter"
Set-ItemProperty -Path $uninstallKey -Name "DisplayIcon" -Value "$iconPath"
Set-ItemProperty -Path $uninstallKey -Name "InstallLocation" -Value "$installDir"
Set-ItemProperty -Path $uninstallKey -Name "UninstallString" -Value "powershell -ExecutionPolicy Bypass -File `"$installDir\uninstall.ps1`""
Set-ItemProperty -Path $uninstallKey -Name "NoModify" -Value 1 -Type DWord
Set-ItemProperty -Path $uninstallKey -Name "NoRepair" -Value 1 -Type DWord

# Register auto-startup with Windows
Set-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "NetSpeedMeter" -Value "`"$exePath`" --minimized"

Write-Host "[5/5] Launching NetSpeed Meter..." -ForegroundColor Green
Start-Process -FilePath $exePath -WorkingDirectory $installDir

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "   Installation Complete! NetSpeed Meter is now running.  " -ForegroundColor Green
Write-Host "   - Desktop Shortcut created: NetSpeed Meter             " -ForegroundColor Green
Write-Host "   - Start Menu entry added: NetSpeed Meter               " -ForegroundColor Green
Write-Host "   - Windows Apps list registered                         " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
