@echo off
echo Building self-contained release for 32-bit Windows...
echo.

echo Cleaning previous builds...
dotnet clean -c Release

echo Building and publishing self-contained executable for x86...
dotnet publish -c Release -r win-x86 --self-contained true --output "bin\standalone-x86"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Build failed! Check the errors above.
    pause
    exit /b 1
)

echo.
echo Success! Self-contained 32-bit executable created in bin\standalone-x86\
echo The derelict.exe file can now run on any 32-bit or 64-bit Windows PC without .NET installed.
echo.
pause