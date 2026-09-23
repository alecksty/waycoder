# ═══════════════════════════════════════════════════════════════
# WayCoder 清除编译垃圾（PowerShell 版）
# 用法: .\scripts\clean.ps1 [-DryRun] [-NoGc] [-Scratch]
#
# 覆盖的工程：
#   WayCoder/            主 CLI + TUI          —— bin/obj + 旁路输出 bin2/obj2
#   WayCoder.Gui/        Avalonia GUI          —— bin/obj
#   WayCoder.Maui/       .NET MAUI (Android/iOS) —— bin/obj、.gradle、*.apk/*.aab
#   WayCoder.Preview/    预览宿主              —— bin/obj
#   third_party/vml/     vendored VML 的 35 个 C# 工程 —— bin/obj
#                        + 本地中间产物 *.gen.vml / *.vmb
#                        （**不**动它 Lib/ 下已跟踪的 2041 个 .vml，见下方「刻意不碰」）
#   vscode-extension/    VS Code 扩展 (TS)     —— node_modules、out/、*.vsix
#   WayCoder/3d-game/    零构建前端游戏         —— 无构建产物（仅 __pycache__）
#
# 说明: 删除 C# 项目的 bin/obj、IDE 缓存 .vs/.idea（隐藏目录用 -Force 才可见）、
#       *.user/*.suo/*.binlog 用户配置、node_modules 依赖、StarGo 五子棋的 MSVC 产物、
#       独立 publish 目录、MAUI 的 .gradle 与 *.apk/*.aab、Python 的 __pycache__/*.pyc/.venv/venv、
#       vscode-extension 的 out/ 与 *.vsix、TestResults / BenchmarkDotNet.Artifacts / AppPackages /
#       .store / verify_build / stress-test-output 测试产物、dist/ 下的陈旧发布产物、
#       **VML 本地中间产物 *.gen.vml / *.vmb**。
#
#       关于 bin2/obj2：主 bin/ 被运行中的 exe 锁住时，构建会改用 -p:OutputPath=bin2/
#       落到这里（见 .gitignore），属构建产物，一并清理。
#
#       关于 VML 中间产物：判据是「没有任何构建把它们当源读」——
#       *.gen.vml 被 .gitignore 明写，且 make-vml-lib.sh 与 vml-diag-probe/examples-build.sh
#       都显式把它排除出遍历（不然会被当成待编译的例子）；*.vmb 是 VMLTool 的编译终点
#       （Program.Compile.cs「→ VML 二进制 → 停止」）。这两个 pattern 已用
#       `git ls-files '*.gen.vml' '*.vmb'` 验证过「已跟踪 0 个」，脚本里还留了同一句做闸门。
#
#       关于 -Scratch：.gitignore 把 .scratch/ 定义为「草稿区：验证截图、一次性探针源码、
#       交接草稿」——是**有意保留**的本地材料（CLAUDE.md 多处验证脚手架就在这儿，如
#       .scratch/vmlround 的 --play/--lines/--best），故默认不动，要清得显式给 -Scratch。
#
# 刻意不碰（防误伤，勿加进来）：
#   third_party/vml/Lib/**/*.vml  —— 2041 个文件是**已跟踪**的 vendored 源。vml 自带的
#                                    Lib/cleanup.sh 会删掉它们（那是上游仓库的语义，
#                                    在我们这边等于删源码并弄脏 git 状态）。
#                                    ⚠ 同理**绝不用 `*.vml` 通配**：Examples/ 下还有 2 个
#                                    .vml 是例子源码，且 `vmlcli --rebuild-lib` 会往
#                                    Lib/<语言>/ 写 .vml —— 那些是**产物但已跟踪**，
#                                    删了就是删源码，只能让 git 报 dirty，不能靠 clean 清。
#   .scratch/                     —— 草稿区，有意保留的本地材料（-Scratch 才清）
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
    [switch]$NoGc,
    [switch]$Scratch
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

# VML 本地中间产物 —— *.gen.vml（Examples/ 下跑出来的汇编）+ *.vmb（编译终点）
# 闸门：这两个 pattern 一旦命中 git 已跟踪文件，就整体跳过 —— 说明 pattern 写错了
# （比如误写成 *.vml），此时删下去等于删源码（Lib/ 下 2041 个 .vml 是 vendored 源）。
Write-Host '── VML 本地中间产物 (*.gen.vml / *.vmb) ──'
$vmlTracked = @(git ls-files -- '*.gen.vml' '*.vmb' 2>$null | Where-Object { $_ })
if ($vmlTracked.Count -gt 0) {
    Write-Host '  ✘ 已跳过：清理 pattern 命中了 git 已跟踪文件（先修 pattern，别删）'
    $vmlTracked | ForEach-Object { Write-Host "      $_" }
} else {
    Get-ChildItem -Path . -Recurse -File -Force -ErrorAction SilentlyContinue |
        Where-Object { ($_.Name -like '*.gen.vml' -or $_.Name -like '*.vmb') -and $_.FullName -notmatch '\\\.git\\' } |
        ForEach-Object { $targets.Add($_.FullName) }
}

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

# 草稿区 .scratch/ —— 默认保留（.gitignore 定义为有意保留的本地草稿材料），-Scratch 才清
if ($Scratch) {
    Write-Host '── 草稿区 .scratch/（-Scratch 显式开启）──'
    Get-ChildItem -Path . -Recurse -Directory -Force -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -eq '.scratch' -and $_.FullName -notmatch '\\\.git\\' } |
        ForEach-Object { $targets.Add($_.FullName) }
} else {
    Write-Host '── 草稿区 .scratch/（默认保留，要清加 -Scratch）──'
}

# dist/ 陈旧发布产物（保留 .waycoder 用户数据）
Write-Host '── dist/ 陈旧发布产物（保留 .waycoder 用户数据）──'
if (Test-Path dist) {
    Get-ChildItem -Path dist -Force -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -ne '.waycoder' } |
        ForEach-Object { $targets.Add($_.FullName) }
} else {
    Write-Host '  (无 dist 目录)'
}

# ── .scratch/ 统一闸门（放这里，别散到上面各段去）─────────────────────────
# 文件头把 .scratch/ 定义为「有意保留的本地草稿材料」（-Scratch 才清），但上面各段是按
# **目录名 / 扩展名**全树匹配的，会伸进 .scratch 内部。2026-09-23 实测踩到：
#   · bin/obj 那一段删掉了 .scratch/oldprogs/src/number/ 下 **70 个 NetBSD 源码目录** ——
#     NetBSD 的 bin/ 里放的是**源码**（external/bsd/flex/bin、external/apache2/llvm/bin…），
#     不是构建产物；同理 obj/（那些树里也有同名源码目录）。
#   · *.user 那一段删掉了 unbound 的**测试夹具** external/bsd/unbound/dist/…/bad.user。
# ⚠ 闸门放在**收集之后统一过滤**，而不是给上面 14 个扫描各补一条 -notmatch：漏一处就是
#    这个坑重演（本仓头号教训 —— 规则要收口成唯一实现，别指望调用点各自记得）。
if (-not $Scratch) {
    $kept = New-Object System.Collections.Generic.List[string]
    foreach ($t in $targets) {
        if ($t -match '\\\.scratch\\') { continue }
        $kept.Add($t)
    }
    $skippedScratch = $targets.Count - $kept.Count
    if ($skippedScratch -gt 0) {
        Write-Host '── .scratch/ 闸门 ──'
        Write-Host "  .scratch/ 下 $skippedScratch 项按约定保留（要清加 -Scratch）"
    }
    $targets = $kept
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
    Write-Host '         WayCoder/works|saves、logs/、.waycoder/、dist/.waycoder、*.keystore、'
    Write-Host '         .scratch/（草稿区，-Scratch 才清）'
}
