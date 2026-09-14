@echo off
chcp 65001 >nul
cd /d "%~dp0.."

echo === VML 设备头文件生成器 ===
echo.

REM 委托 PowerShell 脚本执行（构建/缓存逻辑已内置）
pwsh -ExecutionPolicy Bypass -File "%~dp0MakeDevice.ps1" %*
exit /b %ERRORLEVEL%
