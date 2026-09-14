@echo off
chcp 65001 >nul
REM ============================================================
REM Lib/cleannup.bat — clean up
REM ============================================================
cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -File "%~dp0cleanup.ps1" %*
