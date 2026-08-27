# ═══════════════════════════════════════════════════════════════
# WayCoder 清除编译垃圾（PowerShell 版）
# 用法: .\scripts\clean.ps1 [-DryRun] [-NoGc]
# 说明: 删除 C# 项目的 bin/obj、.vs/.idea IDE 缓存（隐藏目录用 -Force 才可见）、*.user 用户配置、
#       node_modules 依赖、StarGo 五子棋的 MSVC 产物（x64/Release/Debug + exe/obj/ilk/pdb）、
#       独立 publish 目录、MAUI 的 .gradle / .apk 产物、TestResults / BenchmarkDotNet.Artifacts /
#       AppPackages / .store 测试打包产物、dist/ 下的陈旧发布产物
#       —— 保留 dist/.waycoder 与项目 .waycoder 运行时用户数据。
#       全部为 .gitignore 忽略的构建产物，不影响源码与 git 状态。
#       -DryRun 只列出将删除项，不实际删除；-NoGc 跳过最后的 git gc --aggressive（大仓库较慢）。
# ═══════════════════════════════════════════════════════════════
param(
    [switch]$DryRun,
    [switch]$NoGc
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
Set-Location $repo

if ($DryRun) { Write-Host '🔎 干跑模式：仅列出将删除项，不实际删除' }
else { $before = (Get-ChildItem -Recurse -File -Force -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum }

# 注意：函数名不能叫 Del/del —— PowerShell 内置别名 del = Remove-Item 会遮蔽函数（别名优先级高于函数），
# 导致删除逻辑从未执行。统一用 Remove-Target。
function Remove-Target([string]$path) {
    if ([string]::IsNullOrWhiteSpace($path)) { return }
    if ($DryRun) { Write-Host "  [将删] $path" }
    else {
        Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  [已删] $path"
    }
}

# 收集后再删，避免遍历过程中目录被删导致的枚举异常
$targets = New-Object System.Collections.Generic.List[string]

# C# 构建产物 bin/obj
Write-Host '── C# 构建产物 bin/obj ──'
Get-ChildItem -Path . -Recurse -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -in @('bin', 'obj') -and $_.FullName -notmatch '\\\.git\\|\\node_modules\\' } |
    ForEach-Object { $targets.Add($_.FullName) }

# IDE 缓存 .vs（.vs 是隐藏目录，必须 -Force 才能枚举到）
Write-Host '── IDE 缓存 .vs ──'
Get-ChildItem -Path . -Recurse -Directory -Filter '.vs' -Force -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\\.git\\' } |
    ForEach-Object { $targets.Add($_.FullName) }

# node_modules 依赖
Write-Host '── node_modules 依赖 ──'
Get-ChildItem -Path . -Recurse -Directory -Filter node_modules -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\\.git\\' } |
    ForEach-Object { $targets.Add($_.FullName) }

# StarGo 五子棋 MSVC 产物
Write-Host '── StarGo 五子棋 MSVC 产物 ──'
if (Test-Path StarGo) {
    Get-ChildItem -Path StarGo -Recurse -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -in @('x64', 'Release', 'Debug', 'Win32') } |
        ForEach-Object { $targets.Add($_.FullName) }
    Get-ChildItem -Path StarGo -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -in @('.exe', '.obj', '.ilk', '.pdb', '.lib', '.exp') } |
        ForEach-Object { $targets.Add($_.FullName) }
}

# JetBrains Rider/IntelliJ 缓存 .idea（可能隐藏，-Force 保险）
Write-Host '── JetBrains Rider/IntelliJ 缓存 .idea ──'
Get-ChildItem -Path . -Recurse -Directory -Filter '.idea' -Force -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\\.git\\' } |
    ForEach-Object { $targets.Add($_.FullName) }

# VS/Rider 用户级配置 (*.user / *.suo)
Write-Host '── VS/Rider 用户级配置 (*.user / *.suo) ──'
Get-ChildItem -Path . -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { ($_.Name -like '*.user' -or $_.Name -like '*.suo' -or $_.Name -like '*.binlog') -and
                   $_.FullName -notmatch '\\\.git\\|\\\.vs\\|\\bin\\|\\obj\\' } |
    ForEach-Object { $targets.Add($_.FullName) }

# 独立 publish 目录（bin/obj 内已由上面清理）
Write-Host '── 独立 publish 目录 ──'
Get-ChildItem -Path . -Recurse -Directory -Filter 'publish' -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\\.git\\|\\bin\\|\\obj\\' } |
    ForEach-Object { $targets.Add($_.FullName) }

# MAUI/Android Gradle 构建缓存 .gradle（可能隐藏，-Force 保险）
Write-Host '── MAUI/Android Gradle 构建缓存 .gradle ──'
Get-ChildItem -Path . -Recurse -Directory -Filter '.gradle' -Force -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\\.git\\' } |
    ForEach-Object { $targets.Add($_.FullName) }

# 测试/打包产物 (TestResults / BenchmarkDotNet.Artifacts / AppPackages / .store)
Write-Host '── 测试/打包产物 (TestResults / BenchmarkDotNet.Artifacts / AppPackages / .store) ──'
Get-ChildItem -Path . -Recurse -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -in @('TestResults', 'BenchmarkDotNet.Artifacts', 'AppPackages', '.store') -and
                   $_.FullName -notmatch '\\\.git\\|\\bin\\|\\obj\\' } |
    ForEach-Object { $targets.Add($_.FullName) }

# dist/ 陈旧发布产物（保留 .waycoder 用户数据）
Write-Host '── dist/ 陈旧发布产物（保留 .waycoder 用户数据）──'
if (Test-Path dist) {
    Get-ChildItem -Path dist -Force -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -ne '.waycoder' } |
        ForEach-Object { $targets.Add($_.FullName) }
} else {
    Write-Host '  (无 dist 目录)'
}

foreach ($t in $targets) { Remove-Target $t }

# Git 仓库压缩（gc --aggressive + prune）
if ($NoGc) {
    Write-Host '── Git 仓库压缩（-NoGc 已跳过）──'
} else {
    Write-Host '── Git 仓库压缩（gc --aggressive + prune）──'
    git rev-parse --git-dir *> $null
    if ($LASTEXITCODE -eq 0) {
        if ($DryRun) {
            Write-Host '  [将执行] git gc --aggressive --prune=now'
        } else {
            $gitBefore = (Get-ChildItem -Path .git -Recurse -File -Force -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
            git gc --aggressive --prune=now *> $null
            $gitAfter = (Get-ChildItem -Path .git -Recurse -File -Force -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
            Write-Host '  [已压缩] git gc --aggressive --prune=now'
            Write-Host "  .git: $([math]::Round($gitBefore / 1MB, 0)) MB → $([math]::Round($gitAfter / 1MB, 0)) MB"
        }
    } else {
        Write-Host '  (无 .git 仓库)'
    }
}

if ($DryRun) {
    Write-Host '───────────────────────────────────────────'
    Write-Host '🔎 干跑结束：以上为将删除项，未实际删除。'
} else {
    $after = (Get-ChildItem -Recurse -File -Force -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
    $freedMB = [math]::Round(($before - $after) / 1MB, 0)
    Write-Host '───────────────────────────────────────────'
    Write-Host "✅ 清理完成，释放约 $freedMB MB。"
    Write-Host '   （需重建：dotnet build 重新生成 bin/obj；扩展开发再 npm install）'
}
