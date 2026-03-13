@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0install_dotnet_sdk.ps1" %*
