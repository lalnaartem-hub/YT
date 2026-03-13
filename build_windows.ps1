param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

Write-Host '[MARX] Restoring packages...'
dotnet restore .\MARX.Windows.sln

Write-Host "[MARX] Building solution ($Configuration)..."
dotnet build .\MARX.Windows.sln -c $Configuration --no-restore

Write-Host '[MARX] Running tests (if any)...'
dotnet test .\MARX.Windows.sln -c $Configuration --no-build

Write-Host '[MARX] Done.' -ForegroundColor Green
