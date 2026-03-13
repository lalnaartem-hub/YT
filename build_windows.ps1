param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Release',
    [switch]$SkipAssets,
    [string]$SdkVersion = '8.0.100'
)

$ErrorActionPreference = 'Stop'

$localDotnet = Join-Path (Resolve-Path .).Path '.dotnet\dotnet.exe'
if (-not (Test-Path $localDotnet)) {
    Write-Host '[MARX] dotnet not found locally. Installing local SDK...'
    & .\scripts\install_dotnet_sdk.ps1 -Version $SdkVersion -InstallDir .\.dotnet
}

if (-not (Test-Path $localDotnet)) {
    throw 'Unable to find local dotnet after installation attempt.'
}

if (-not $SkipAssets -and (Test-Path .\scripts\setup_flower_assets.ps1)) {
    Write-Host '[MARX] Preparing local flower assets...'
    & .\scripts\setup_flower_assets.ps1
}

Write-Host '[MARX] Restoring packages...'
& $localDotnet restore .\MARX.Windows.sln

Write-Host "[MARX] Building solution ($Configuration)..."
& $localDotnet build .\MARX.Windows.sln -c $Configuration --no-restore

Write-Host '[MARX] Running tests (if any)...'
& $localDotnet test .\MARX.Windows.sln -c $Configuration --no-build

Write-Host '[MARX] Done.' -ForegroundColor Green
