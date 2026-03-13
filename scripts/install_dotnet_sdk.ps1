param(
    [string]$Version = '8.0.100',
    [string]$InstallDir = '.\.dotnet'
)

$ErrorActionPreference = 'Stop'

if (!(Test-Path $InstallDir)) {
    New-Item -Path $InstallDir -ItemType Directory | Out-Null
}

$dotnetExe = Join-Path $InstallDir 'dotnet.exe'
if (Test-Path $dotnetExe) {
    Write-Host "[MARX] Local dotnet already installed: $dotnetExe"
    & $dotnetExe --info | Out-Host
    exit 0
}

$installer = Join-Path $env:TEMP 'dotnet-install.ps1'
Write-Host "[MARX] Downloading dotnet-install script..."
Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer

Write-Host "[MARX] Installing .NET SDK $Version to $InstallDir ..."
& powershell -ExecutionPolicy Bypass -File $installer -Version $Version -InstallDir $InstallDir

Write-Host '[MARX] Local SDK installed.' -ForegroundColor Green
& $dotnetExe --list-sdks
