@echo off
setlocal

if "%~1"=="" (
  set CONFIG=Release
) else (
  set CONFIG=%~1
)

echo [MARX] Restoring packages...
dotnet restore MARX.Windows.sln
if errorlevel 1 goto :fail

echo [MARX] Building solution (%CONFIG%)...
dotnet build MARX.Windows.sln -c %CONFIG% --no-restore
if errorlevel 1 goto :fail

echo [MARX] Running tests (if any)...
dotnet test MARX.Windows.sln -c %CONFIG% --no-build
if errorlevel 1 goto :fail

echo [MARX] Done.
exit /b 0

:fail
echo [MARX] Build pipeline failed.
exit /b 1
