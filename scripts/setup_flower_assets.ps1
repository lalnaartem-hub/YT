param(
    [string]$ZipUrl = "https://www.dropbox.com/scl/fi/pu3jmdo0cyt7rt12fguaf/flower.zip?rlkey=ux0xwp8hvxfzqn64bb5u7wgzk&st=42hqy1br&dl=1",
    [string]$ProjectRoot = ".\SecureMessenger"
)

$ErrorActionPreference = 'Stop'

$modelsRoot = Join-Path $ProjectRoot "3d_models"
$flowerRoot = Join-Path $modelsRoot "flower"
$tempZip = Join-Path $modelsRoot "flower.zip"

New-Item -ItemType Directory -Force -Path $flowerRoot | Out-Null

Write-Host "[MARX] Downloading flower pack..."
Invoke-WebRequest -Uri $ZipUrl -OutFile $tempZip

Write-Host "[MARX] Extracting flower pack..."
Expand-Archive -Path $tempZip -DestinationPath $flowerRoot -Force

Remove-Item $tempZip -Force

$glb = Get-ChildItem -Path $flowerRoot -Filter *.glb -Recurse -ErrorAction SilentlyContinue
Write-Host "[MARX] Imported GLB count: $($glb.Count)" -ForegroundColor Green
