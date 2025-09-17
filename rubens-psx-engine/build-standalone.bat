@echo off
echo Building self-contained release...
echo.

echo Cleaning previous builds...
dotnet clean -c Release

echo Building and publishing self-contained executable...
dotnet publish -c Release -r win-x64 --self-contained true --output "bin\standalone"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Build failed! Check the errors above.
    pause
    exit /b 1
)

echo.
echo Success! Self-contained executable created in bin\standalone\
echo The derelict.exe file can now run on any Windows PC without .NET installed.
echo.
pause