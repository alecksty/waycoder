@echo off
setlocal enabledelayedexpansion

REM Auto-detect Visual Studio 2022 vcvarsall via vswhere
for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -property installationPath 2^>nul`) do set "VS_PATH=%%i"
if "%VS_PATH%"=="" (
    echo ERROR: Visual Studio 2022 not found. Install VS 2022 or set VS_PATH manually.
    exit /b 1
)
call "%VS_PATH%\VC\Auxiliary\Build\vcvarsall.bat" x64 >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: vcvarsall.bat failed.
    exit /b %ERRORLEVEL%
)

cd /d "%~dp0"
cl /nologo /O2 /MD /LD /Fe:libglhelper.dll libglhelper_stub.c
if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%
echo SUCCESS: libglhelper.dll compiled
