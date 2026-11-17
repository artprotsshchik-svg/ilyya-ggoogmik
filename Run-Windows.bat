@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET SDK not found. Install .NET 11 or .NET 10 SDK from https://dotnet.microsoft.com/download
  pause
  exit /b 1
)

set "SDK_MAJOR="
for /f "tokens=1 delims=." %%a in ('dotnet --version') do set "SDK_MAJOR=%%a"
if "%SDK_MAJOR%"=="" set "SDK_MAJOR=8"
set "TFM=net%SDK_MAJOR%.0"

echo Detected .NET SDK major version %SDK_MAJOR% - using %TFM%
echo Restoring packages from nuget.org...
dotnet restore ModpackWorkbench\ModpackWorkbench.csproj -p:TargetFramework=%TFM% --source https://api.nuget.org/v3/index.json
if errorlevel 1 (
  echo.
  echo Package restore failed. Check internet connection / proxy access to https://api.nuget.org/v3/index.json
  pause
  exit /b 1
)

echo Starting ModpackWorkbench (Avalonia UI)...
dotnet run --project ModpackWorkbench -p:TargetFramework=%TFM% --no-restore
if errorlevel 1 (
  echo.
  echo Launch failed. Check errors above.
  pause
)
