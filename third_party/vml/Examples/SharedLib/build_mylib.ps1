#!/usr/bin/env pwsh
# ============================================================
# build_mylib.ps1 — 用户自定义共享库一键构建 + 16语言绑定
# ============================================================
# 用法:
#   pwsh Examples/SharedLib/build_mylib.ps1        # 编译+生成绑定
#   pwsh Examples/SharedLib/build_mylib.ps1 -run   # 编译+运行测试
# ============================================================
param([switch]$Run)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path))
$libDir = "$root/Examples/SharedLib"

Write-Host "=== 构建 mylib 共享库 ===" -ForegroundColor Cyan

# 1. 编译 C 源码 → mylib.vml
Write-Host "`n[1/3] 编译 mylib.c → mylib.vml" -ForegroundColor Yellow
dotnet run --project "$root/VMLPrepares/CCompiler" -- "$libDir/mylib.c" -o "$libDir/mylib.vml" --no-link
if ($LASTEXITCODE -ne 0) { throw "mylib 编译失败" }
Write-Host "  已生成: mylib.vml ($((Get-Item "$libDir/mylib.vml").Length) bytes)" -ForegroundColor Green

# 2. 生成 16 种语言的绑定代码
Write-Host "`n[2/3] 生成 16 种语言的绑定..." -ForegroundColor Yellow

# 先复制 mylib 源文件到 Lib/shared/src/ 供 GenLib 扫描
Copy-Item "$libDir/mylib.c" "$root/Lib/shared/src/mylib.c" -Force
Copy-Item "$libDir/mylib.h" "$root/Lib/shared/src/mylib.h" -Force
Copy-Item "$libDir/mylib.vml" "$root/Lib/shared/mylib.vml" -Force

# 扫描更新函数清单
Write-Host "  扫描函数..."
dotnet run --project "$root/tools/GenLib" -- scan "$root" 2>&1 | Out-Null

# 生成各语言绑定
Write-Host "  生成绑定..."
dotnet run --project "$root/tools/GenLib" -- gen all "$root" 2>&1 | Out-Null

# 3. 生成使用示例
Write-Host "`n[3/3] 生成使用示例..." -ForegroundColor Yellow

# === C 调用示例 ===
@"
// test_mylib.c — C 语言调用 mylib
#include "mylib.h"

int main() {
    int sum = my_add(1, 2);           // sum = 3
    int max = my_max(1, 3, 2);        // max = 3
    my_print("Hello from mylib!");
    int ver = my_version();           // ver = 100
    return sum + max;                 // R0 = 6
}
"@ | Out-File -Encoding utf8 "$libDir/test_mylib.c"

# === BASIC 调用示例 ===
@"
' test_mylib.bas — BASIC 语言调用 mylib
DECLARE FUNCTION my_add(a AS INTEGER, b AS INTEGER) AS INTEGER
    asm("CALL my_add")
    my_add = 0
END FUNCTION

DECLARE FUNCTION my_max(a AS INTEGER, b AS INTEGER, c AS INTEGER) AS INTEGER
    asm("CALL my_max")
    my_max = 0
END FUNCTION

DECLARE SUB my_print(msg AS INTEGER)
    asm("CALL my_print")
END SUB

DECLARE FUNCTION my_version() AS INTEGER
    asm("CALL my_version")
    my_version = 0
END FUNCTION

sum = my_add(1, 2)
maxVal = my_max(1, 3, 2)
my_print("Hello from BASIC!")
ver = my_version()
PRINT sum; maxVal; ver
"@ | Out-File -Encoding utf8 "$libDir/test_mylib.bas"

# === Python 调用示例 ===
@"
# test_mylib.py — Python 语言调用 mylib

def my_add(a, b):
    asm(f"MOVE R0, #0")  # load a
    asm(f"PUSH R0")
    asm(f"MOVE R0, #1")  # load b
    asm(f"PUSH R0")
    asm("CALL my_add")
    return 0  # result in R0

def my_max(a, b, c):
    asm("CALL my_max")
    return 0

def my_print(msg):
    asm("CALL my_print")

def my_version():
    asm("CALL my_version")
    return 0

x = my_add(1, 2)
m = my_max(1, 3, 2)
my_print("Hello from Python!")
v = my_version()
"@ | Out-File -Encoding utf8 "$libDir/test_mylib.py"

Write-Host "  已生成: test_mylib.c / test_mylib.bas / test_mylib.py" -ForegroundColor Green

# 4. (可选) 运行测试
if ($Run) {
    Write-Host "`n=== 运行测试 ===" -ForegroundColor Cyan
    
    # C 测试
    Write-Host "  C 测试..." -ForegroundColor Gray
    dotnet run --project "$root/VMLPrepares/CCompiler" -- "$libDir/test_mylib.c" -o "$libDir/test_mylib_c.vml"
    if ($LASTEXITCODE -eq 0) {
        dotnet run --project "$root/VMLEmulators/ConsoleEmulator" -- -r "$libDir/test_mylib_c.vml" --timeout 10
    }
    
    # BASIC 测试
    Write-Host "  BASIC 测试..." -ForegroundColor Gray
    dotnet run --project "$root/VMLPrepares/BasicCompiler" -- "$libDir/test_mylib.bas" -o "$libDir/test_mylib_bas.vml"
    if ($LASTEXITCODE -eq 0) {
        dotnet run --project "$root/VMLEmulators/ConsoleEmulator" -- -r "$libDir/test_mylib_bas.vml" --timeout 10
    }
}

Write-Host "`n=== 完成 ===" -ForegroundColor Green
Write-Host "共享库: $libDir/mylib.vml" -ForegroundColor Cyan
Write-Host "头文件: $libDir/mylib.h" -ForegroundColor Cyan
Write-Host "绑定已集成到: Lib/shared/ (16种语言)" -ForegroundColor Cyan
Write-Host "测试文件: test_mylib.c / test_mylib.bas / test_mylib.py" -ForegroundColor Cyan
Write-Host ""
Write-Host "在其他语言中使用: 将 mylib.vml 链接到你的项目中即可" -ForegroundColor Gray
