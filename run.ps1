$dotnetPath = "$env:LOCALAPPDATA\Microsoft\dotnet"
if (Test-Path $dotnetPath) {
    $env:DOTNET_ROOT = $dotnetPath
    $env:PATH = "$dotnetPath;$env:PATH"
}

Write-Host "Starting NetSpeed Meter..." -ForegroundColor Cyan
Start-Process "$PSScriptRoot\publish\NetSpeedMeter\NetSpeedMeter.exe"
