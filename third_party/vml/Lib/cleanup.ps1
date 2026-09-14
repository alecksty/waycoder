# Lib/cleanup.ps1 — 清理所有编译产生的 VML 文件和临时文件 (PowerShell)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $ScriptDir

$TOTAL = 0

function Clean-Vml($file) {
    if (Test-Path $file) {
        Remove-Item $file -Force
        $script:TOTAL++
        Write-Host "  删除: $file" -ForegroundColor Gray
    }
}

Write-Host "=== 清理 Lib/ 编译产物 ===" -ForegroundColor Green

# 1. 清理 shared/ 编译自 src/*.c 的 .vml
Write-Host "[1/6] 清理 Lib/shared/ 编译模块 ..." -ForegroundColor Cyan
Get-ChildItem -Path shared/src -Filter *.c | ForEach-Object {
    $name = $_.BaseName
    Clean-Vml "shared/$name.vml"
}
Get-ChildItem -Path shared -Filter *.c | ForEach-Object {
    $name = $_.BaseName
    Clean-Vml "shared/$name.vml"
}

# 2. 清理各语言模块 vml
Write-Host "[2/6] 清理各语言模块 vml ..." -ForegroundColor Cyan
$langs = @("c", "cpp", "csharp", "forth", "go", "java", "javascript", "kotlin", "lua", "python", "rust", "scheme", "swift", "ruby", "dart", "objc", "r", "d", "fortran", "basic", "pascal", "ladder")
foreach ($lang in $langs) {
    Get-ChildItem -Path $lang -Filter *.vml | ForEach-Object {
        # 跳过手写文件
        $skip = $false
        if ($lang -eq "Basic" -and $_.Name -eq "stdlib.vml") { $skip = $true }
        if ($lang -eq "Pascal" -and $_.Name -eq "stdlib.vml") { $skip = $true }
        if ($lang -eq "ladder" -and $_.Name -eq "stdlib.vml") { $skip = $true }
        if (-not $skip) { Clean-Vml $_.FullName }
    }
}

# 3. 清理 dynamic/ 编译模块
Write-Host "[3/6] 清理 Lib/dynamic/ 编译模块 ..." -ForegroundColor Cyan
Get-ChildItem -Path dynamic -Filter *.c | ForEach-Object {
    Clean-Vml "dynamic/$($_.BaseName).vml"
}

# 4. 清理 c/ 额外聚合
Write-Host "[4/6] 清理 c/ 额外文件 ..." -ForegroundColor Cyan
Clean-Vml "c/stdlib_complete.vml"
Clean-Vml "c/stdio_funcs.vml"
Clean-Vml "c/encoding.vml"

# 5. 清理各语言聚合文件
Write-Host "[5/6] 清理各语言聚合 ..." -ForegroundColor Cyan
foreach ($lang in $langs) {
    Clean-Vml "$lang/stdlib.vml"
    Clean-Vml "$lang/stdlib_complete.vml"
    Clean-Vml "$lang/builtin.vml"
    Clean-Vml "$lang/vmllib.vml"
}

# 6. 清理临时目录
Write-Host "[6/6] 清理临时目录 ..." -ForegroundColor Cyan
if (Test-Path "dynamic/lang") { Remove-Item "dynamic/lang" -Recurse -Force }
if (Test-Path "shared/backup") { Remove-Item "shared/backup" -Recurse -Force }

Pop-Location
Write-Host "=== 完成。共清理 $TOTAL 个文件 ===" -ForegroundColor Green
