@echo off
echo Building project...
cd /d %~dp0
dotnet build
if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b %errorlevel%
)
echo Build successful! Starting game...
start cmd /k "cd /d %~dp0rubens-psx-engine\bin\game\net9.0-windows7.0\win-x64 && derelict.exe"
