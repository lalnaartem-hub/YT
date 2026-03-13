@echo off
setlocal

if "%~1"=="" (
  set CONFIG=Release
) else (
  set CONFIG=%~1
)

if not exist .dotnet\dotnet.exe (
  echo [MARX] dotnet not found locally. Installing SDK...
  call scripts\install_dotnet_sdk.bat -Version 8.0.100 -InstallDir .\.dotnet
  if errorlevel 1 goto :fail
)

echo [MARX] Preparing local flower assets...
if exist scripts\setup_flower_assets.bat (
  call scripts\setup_flower_assets.bat
)

echo [MARX] Restoring packages...
.dotnet\dotnet.exe restore MARX.Windows.sln
if errorlevel 1 goto :fail

echo [MARX] Building solution (%CONFIG%)...
.dotnet\dotnet.exe build MARX.Windows.sln -c %CONFIG% --no-restore
if errorlevel 1 goto :fail

echo [MARX] Running tests (if any)...
.dotnet\dotnet.exe test MARX.Windows.sln -c %CONFIG% --no-build
if errorlevel 1 goto :fail

echo [MARX] Done.
exit /b 0

:fail
echo [MARX] Build pipeline failed.
exit /b 1
