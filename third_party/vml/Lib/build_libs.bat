@echo off
chcp 65001 >nul
REM ============================================================
REM Lib/build_libs.bat — Windows 批处理包装器
REM 调用 build_libs.ps1 编译所有 VML 库文件
REM
REM 用法:
REM   build_libs             全部编译
REM   build_libs -Shared      仅编译共享库模块
REM   build_libs -Lang        仅生成各语言聚合文件
REM   build_libs -C           仅编译 C 标准库
REM ============================================================
setlocal
cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -File "%~dp0build_libs.ps1" %*
