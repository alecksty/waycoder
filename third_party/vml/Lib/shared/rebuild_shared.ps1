#!/usr/bin/env pwsh
<#
.SYNOPSIS
    重建 Lib/shared/ 下所有共享库的 .vml 文件
    使用 C 编译器的 --no-link 模式，确保每个 .vml 只包含自身代码（无自动链接）
.DESCRIPTION
    扫描 Lib/shared/src/*.c，用 CCompiler 重新编译每个文件到 Lib/shared/<name>.vml。
    --no-link 禁止自动链接 Lib/c/ 中的库，仅输出纯编译结果。
    备份原 .vml 文件到 Lib/shared/backup/。
.NOTES
    用法: pwsh -File Lib/shared/rebuild_shared.ps1
           pwsh -File Lib/shared/rebuild_shared.ps1 -Modules sysinfo,io,string
           pwsh -File Lib/shared/rebuild_shared.ps1 -SkipBackup
#>
param(
    [string[]]$Modules = @(),          # 只重建指定模块（不含 .c 扩展名）
    [switch]$SkipBackup,               # 跳过备份
    [string]$ProjectRoot               # 项目根目录（默认自动检测）
)

$ErrorActionPreference = "Stop"

if (-not $ProjectRoot) {
    $ProjectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}
$SrcDir    = Join-Path $ProjectRoot "Lib\shared\src"
$OutDir    = Join-Path $ProjectRoot "Lib\shared"
$BackupDir = Join-Path $OutDir "backup"
$VMLTool   = Join-Path $ProjectRoot "VMLTool"

# 自动发现 src/ 下所有 .c 文件
$AllModules = Get-ChildItem -Path $SrcDir -Filter "*.c" | ForEach-Object { $_.BaseName }

if ($Modules.Count -gt 0) {
    $ModuleList = $Modules
} else {
    $ModuleList = $AllModules
}

Write-Host "=== VML 共享库重建脚本 ===" -ForegroundColor Cyan
Write-Host "源文件目录: $SrcDir"
Write-Host "输出目录:   $OutDir"
Write-Host "发现模块:   $($AllModules.Count) 个, 将编译 $($ModuleList.Count) 个"
Write-Host ""

# 验证 VMLTool 可用
if (-not (Test-Path "$VMLTool\VMLTool.csproj")) {
    Write-Error "未找到 VMLTool 项目: $VMLTool"
    exit 1
}

# 备份
if (-not $SkipBackup) {
    if (-not (Test-Path $BackupDir)) {
        New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
    }
    $backupCount = 0
    foreach ($mod in $ModuleList) {
        $src = Join-Path $OutDir "$mod.vml"
        $dst = Join-Path $BackupDir "$mod.vml"
        if (Test-Path $src) {
            Copy-Item -Path $src -Destination $dst -Force
            $backupCount++
        }
    }
    Write-Host "已备份 $backupCount 个 .vml 文件到 $BackupDir" -ForegroundColor Yellow
}

# 编译每个模块
$okCount = 0
$failCount = 0
foreach ($mod in $ModuleList) {
    $srcFile = Join-Path $SrcDir "$mod.c"
    $outFile = Join-Path $OutDir "$mod.vml"

    if (-not (Test-Path $srcFile)) {
        Write-Warning "源文件不存在，跳过: $srcFile"
        $failCount++
        continue
    }

    Write-Host "[$mod] 编译中..." -ForegroundColor Green
    $result = & dotnet run --project "$VMLTool" -- -c "$srcFile" --lang c -o "$outFile" --no-link -I "$(Join-Path $ProjectRoot Lib\c)" 2>&1

    # 检查编译结果
    if ($LASTEXITCODE -eq 0 -and (Test-Path $outFile)) {
        $lines = (Get-Content $outFile | Where-Object { $_ -match '^\s+(?:PUSH|MOVE|STORE|LOAD|CMP|JMP|JE|JNE|JL|JLE|JG|JGE|JZ|JNZ|ADD|SUB|MUL|DIV|MOD|AND|OR|XOR|NOT|SHL|SHR|ASM|SYSCALL|CALL|RET|NOP|ENTER|LEA|TEST)' }).Count
        Write-Host "  -> $outFile ($lines 指令)" -ForegroundColor Green
        $okCount++
    } else {
        Write-Host "  -> 编译失败!" -ForegroundColor Red
        Write-Host $result
        $failCount++
    }
}

Write-Host ""
Write-Host "=== 编译完成 ===" -ForegroundColor Cyan
Write-Host "成功: $okCount  失败: $failCount"

if ($failCount -gt 0) {
    exit 1
}
