@echo off
REM ═══════════════════════════════════════════════════════════════
REM WayCoder 快速对比测试 (CMD)
REM 双击运行，或用: bench-quick.bat [模型1] [模型2] ...
REM ═══════════════════════════════════════════════════════════════
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "REPO_DIR=%SCRIPT_DIR%.."
set "PUBLISH=%REPO_DIR%\publish"
set "RESULT_DIR=%REPO_DIR%\bench-results"
set "TIMESTAMP=%date:~0,4%%date:~5,2%%date:~8,2%_%time:~0,2%%time:~3,2%%time:~6,2%"
set "TIMESTAMP=%TIMESTAMP: =0%"

if not exist "%RESULT_DIR%" mkdir "%RESULT_DIR%"

REM ── 默认模型列表 ──
if "%~1"=="" (
    set MODELS=deepseek-v4-flash deepseek-chat deepseek-v4-pro
) else (
    set MODELS=%*
)

set TASK=写一个 C# 控制台程序，经典贪吃蛇游戏，支持 WASD 控制，纯 ANSI 渲染，200 行以内。用 write_file 写入文件。

echo ═══════════════════════════════════════════
echo   WayCoder 快速对比测试 (CMD^)
echo   时间: %TIMESTAMP%
echo ═══════════════════════════════════════════
echo.

REM 编译
echo [编译中...]
cd /d "%REPO_DIR%\WayCoder"
dotnet publish -c Release -o "%PUBLISH%" --nologo -v q >nul 2>&1
echo.

for %%M in (%MODELS%) do (
    set "MODEL=%%M"
    set "LOG=%RESULT_DIR%\%TIMESTAMP%_%%M.log"
    set "LOG=!LOG::=_!"

    echo ─────────────────────────────────────
    echo 测试: %%M
    echo 日志: !LOG!

    set START=%time%
    "%PUBLISH%\WayCoder.exe" -m %%M -p "!TASK!" --yolo >"!LOG!" 2>&1
    set END=%time%

    echo 结果: 查看 !LOG!
    echo.
)

echo ═══════════════════════════════════════════
echo 全部完成！结果目录: %RESULT_DIR%
echo ═══════════════════════════════════════════
endlocal
pause
