@echo off
REM Simple batch wrapper for the PowerShell version update script

if "%~1"=="" (
    echo Usage: update-version.bat ^<version^> [commit-message]
    echo Example: update-version.bat 1.0.3
    echo Example: update-version.bat 1.0.3 "Added special column sorting feature"
    exit /b 1
)

set NEW_VERSION=%~1
set COMMIT_MSG=%~2

if "%COMMIT_MSG%"=="" (
    set COMMIT_MSG=Version bump to %NEW_VERSION%
)

echo Running PowerShell version update script...
powershell.exe -ExecutionPolicy Bypass -File "update-version.ps1" -NewVersion "%NEW_VERSION%" -CommitMessage "%COMMIT_MSG%"

if %ERRORLEVEL% neq 0 (
    echo Script failed with error code %ERRORLEVEL%
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo Press any key to continue...
pause >nul
