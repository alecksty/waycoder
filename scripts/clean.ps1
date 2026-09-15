# ═══════════════════════════════════════════════════════════════
# WayCoder 清除编译垃圾（PowerShell 版）
# 用法: .\scripts\clean.ps1 [-DryRun] [-NoGc]
#
# 覆盖的工程：
#   WayCoder/            主 CLI + TUI          —— bin/obj + 旁路输出 bin2/obj2
#   WayCoder.Gui/        Avalonia GUI          —— bin/obj
#   WayCoder.Maui/       .NET MAUI (Android/iOS) —— bin/obj、.gradle、*.apk/*.aab
#   WayCoder.Preview/    预览宿主              —— bin/obj
#   third_party/vml/     vendored VML 的 6 个 C# 工程 —— bin/obj
#                        （**不**动它 Lib/ 下已跟踪的 .vml，见下方「刻意不碰」）
#   vscode-extension/    VS Code 扩展 (TS)     —— node_modules、out/、*.vsix
#   WayCoder/3d-game/    零构建前端游戏         —— 无构建产物（仅 __pycache__）
#
# 说明: 删除 C# 项目的 bin/obj、IDE 缓存 .vs/.idea（隐藏目录用 -Force 才可见）、
#       *.user/*.suo/*.binlog 用户配置、node_modules 依赖、StarGo 五子棋的 MSVC 产物、
#       独立 publish 目录、MAUI 的 .gradle 与 *.apk/*.aab、Python 的 __pycache__/*.pyc/.venv/venv、
#       vscode-extension 的 out/ 与 *.vsix、TestResults / BenchmarkDotNet.Artifacts / AppPackages /
#       .store / verify_build / stress-test-output 测试产物、dist/ 下的陈旧发布产物。
#
#       关于 bin2/obj2：主 bin/ 被运行中的 exe 锁住时，构建会改用 -p:OutputPath=bin2/
#       落到这里（见 .gitignore），属构建产物，一并清理。
#
# 刻意不碰（防误伤，勿加进来）：
#   third_party/vml/Lib/**/*.vml  —— 2224 个文件是**已跟踪**的 vendored 源。vml 自带的
#                                    Lib/cleanup.sh 会删掉它们（那是上游仓库的语义，
#                                    在我们这边等于删源码并弄脏 git 状态）
#   WayCoder/works/、WayCoder/saves/  —— 智能体工作区与存档，是用户数据不是垃圾
#   logs/、.waycoder/、dist/.waycoder/、.claude/、.crush/、.codex/  —— 运行时与 AI 工具状态
#   WayCoder.Maui/Resources/Raw/vml_lib.zip、WayCoder/UI/WEB/WebAssets.Generated.cs
#                                    —— 已跟踪/被引用的产物，删了要重新生成才能构建
#
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

# C# 构建产物 bin/obj —— 含旁路输出 bin2/obj2（主 bin/ 被运行中实例锁住时的改道目标）
Write-Host '── C# 构建产物 bin/obj（含旁路 bin2/obj2）──'
Get-ChildItem -Path . -Recurse -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -in @('bin', 'obj', 'bin2', 'obj2') -and $_.FullName -notmatch '\\\.git\\|\\node_modules\\' } |
    ForEach-Object { $targets.Add($_.FullName) }

# IDE 缓存 .vs（.vs 是隐藏目录，必须 -Force 才能枚举到）
Write-Host '── IDE 缓存 .vs ──'
Get-ChildItem -Path . -Recurse -Directory -Filter '.vs' -Force -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\\.git\\' } |
    ForEach-Object { $targets.Add($_.FullName) }

# node_modules 依赖（vscode-extension 等）
Write-Host '── node_modules 依赖 ──'
Get-ChildItem -Path . -Recurse -Directory -Filter node_modules -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\\.git\\' } |
    ForEach-Object { $targets.Add($_.FullName) }

# StarGo 五子棋 MSVC 产物（目录已不在仓库内，保留兼容）
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

# MAUI/Android 打包产物 *.apk / *.aab（.gitignore 已忽略，体积可达数百 MB）
Write-Host '── MAUI/Android 打包产物 (*.apk / *.aab) ──'
Get-ChildItem -Path . -Recurse -File -Include '*.apk', '*.aab' -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\\.git\\' } |
    ForEach-Object { $targets.Add($_.FullName) }

# vscode-extension 编译/打包产物（tsc 的 out/ 与 vsce 的 *.vsix）
Write-Host '── vscode-extension 产物 (out/ 与 *.vsix) ──'
if (Test-Path vscode-extension) {
    Get-ChildItem -Path vscode-extension -Recurse -Directory -Filter 'out' -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '\\node_modules\\' } |
        ForEach-Object { $targets.Add($_.FullName) }
    Get-ChildItem -Path vscode-extension -Recurse -File -Filter '*.vsix' -ErrorAction SilentlyContinue |
        ForEach-Object { $targets.Add($_.FullName) }
}

# Python 缓存与虚拟环境（scripts/*.py、WayCoder/3d-game/start.py）
Write-Host '── Python 缓存 (__pycache__ / *.pyc / .venv / venv) ──'
Get-ChildItem -Path . -Recurse -Directory -Force -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -in @('__pycache__', '.venv', 'venv') -and $_.FullName -notmatch '\\\.git\\|\\node_modules\\' } |
    ForEach-Object { $targets.Add($_.FullName) }
Get-ChildItem -Path . -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Extension -eq '.pyc' -and $_.FullName -notmatch '\\\.git\\' } |
    ForEach-Object { $targets.Add($_.FullName) }

# 测试/打包产物
Write-Host '── 测试/打包产物 (TestResults / BenchmarkDotNet.Artifacts / AppPackages / .store / verify_build / stress-test-output) ──'
Get-ChildItem -Path . -Recurse -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -in @('TestResults', 'BenchmarkDotNet.Artifacts', 'AppPackages', '.store', 'verify_build', 'stress-test-output') -and
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
    Write-Host '   保留：third_party/vml/Lib/**/*.vml（已跟踪的 vendored 源）、'
    Write-Host '         WayCoder/works|saves、logs/、.waycoder/、dist/.waycoder、*.keystore'
}
