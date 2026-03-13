param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Release',
    [switch]$SkipAssets
)

$ErrorActionPreference = 'Stop'

if (-not $SkipAssets -and (Test-Path .\scripts\setup_flower_assets.ps1)) {
    Write-Host '[MARX] Preparing local flower assets...'
    & .\scripts\setup_flower_assets.ps1
}

Write-Host '[MARX] Restoring packages...'
dotnet restore .\MARX.Windows.sln

Write-Host "[MARX] Building solution ($Configuration)..."
dotnet build .\MARX.Windows.sln -c $Configuration --no-restore

Write-Host '[MARX] Running tests (if any)...'
dotnet test .\MARX.Windows.sln -c $Configuration --no-build

Write-Host '[MARX] Done.' -ForegroundColor Green
