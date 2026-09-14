@echo off
chcp 65001 >nul
REM ============================================================
REM Lib/build_static.bat — 编译静态库 (.vml)
REM ============================================================
cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -File "%~dp0build_static.ps1" %*
