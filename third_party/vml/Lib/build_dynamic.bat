@echo off
chcp 65001 >nul
REM ============================================================
REM Lib/build_dynamic.bat — 编译动态库 (.dll/.so → .vml)
REM ============================================================
cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -File "%~dp0build_dynamic.ps1" %*
