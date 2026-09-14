@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0"
echo ==========================================
echo   VML Toolchain: Publish Tools
echo ==========================================
echo.
echo Usage:
echo   publish_tools.ps1
echo.
echo Running publish_tools.ps1...
pwsh -File "%~dp0publish_tools.ps1"
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: publish_tools.ps1 failed with exit code %ERRORLEVEL%
    pause
    exit /b %ERRORLEVEL%
)
echo Done.
if "%CI%"=="" pause
