@echo off
echo Building WorkOrderBlender Installer using WiX v6.0...
echo.

REM Check if WiX v6.0 is installed
where wix >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: WiX Toolset v6.0 not found!
    echo Please install WiX Toolset v6.0 from: https://wixtoolset.org/releases/
    echo.
    echo After installation, you may need to restart your command prompt.
    pause
    exit /b 1
)

echo WiX Toolset v6.0 found.
echo.
REM Build the project first
echo Building project...
dotnet build -c Release
if %errorlevel% neq 0 (
    echo ERROR: Project build failed!
    pause
    exit /b 1
)

REM Create output directory
if not exist "installer" mkdir installer

REM Build WiX v6.0 installer
echo Building WiX v6.0 installer...
wix build WorkOrderBlender.Installer.wxs -out installer\WorkOrderBlender.msi
if %errorlevel% neq 0 (
    echo ERROR: WiX v6.0 build failed!
    pause
    exit /b 1
)

echo.
echo SUCCESS: Installer created at installer\WorkOrderBlender.msi
echo WiX Version: 6.0
echo.
pause
