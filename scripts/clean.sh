#!/bin/bash
# ═══════════════════════════════════════════════════════════════
# WayCoder 清除编译垃圾
# 用法: ./scripts/clean.sh [--dry-run|-n] [--no-gc]
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
# 说明: 删除 C# 项目的 bin/obj、IDE 缓存 .vs/.idea、*.user/*.suo/*.binlog 用户配置、
#       node_modules 依赖、StarGo 五子棋的 MSVC 产物、独立 publish 目录、
#       MAUI 的 .gradle 与 *.apk/*.aab、Python 的 __pycache__/*.pyc/.venv/venv、
#       vscode-extension 的 out/ 与 *.vsix、TestResults / BenchmarkDotNet.Artifacts /
#       AppPackages / .store / verify_build / stress-test-output 测试产物、
#       dist/ 下的陈旧发布产物；清理完成后自动 git gc --aggressive 压缩仓库。
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
#       --dry-run 只列出将删除项，不实际删除；--no-gc 跳过 gc。
# ═══════════════════════════════════════════════════════════════
set -uo pipefail

DRY_RUN=0
NO_GC=0
for arg in "$@"; do
    case "$arg" in
        --dry-run|-n) DRY_RUN=1 ;;
        --no-gc) NO_GC=1 ;;
    esac
done

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_DIR="$(dirname "$SCRIPT_DIR")"
cd "$REPO_DIR"

if [[ "$DRY_RUN" -eq 1 ]]; then
    echo "🔎 干跑模式：仅列出将删除项，不实际删除"
else
    BEFORE=$(du -sk . 2>/dev/null | awk '{print $1}')
fi

del() {
    if [[ "$DRY_RUN" -eq 1 ]]; then
        echo "  [将删] $1"
    else
        rm -rf -- "$1" && echo "  [已删] $1"
    fi
}

# 通用目录查找：$1 = find 的匹配表达式（已括号化），$2 = 额外排除（可选）
find_dirs() {
    local match="$1" extra="${2:-}"
    find . -type d \( $match \) -not -path "./.git/*" -not -path "*/node_modules/*" $extra -print0 2>/dev/null
}

# 通用文件查找：$1 = find 的匹配表达式，$2 = 额外排除（可选）
find_files() {
    local match="$1" extra="${2:-}"
    find . -type f \( $match \) -not -path "./.git/*" $extra -print0 2>/dev/null
}

echo "── C# 构建产物 bin/obj（含旁路 bin2/obj2）──"
while IFS= read -r -d '' d; do del "$d"; done < \
    <(find_dirs "-name bin -o -name obj -o -name bin2 -o -name obj2")

echo "── IDE 缓存 .vs ──"
while IFS= read -r -d '' d; do del "$d"; done < \
    <(find . -type d -name ".vs" -not -path "./.git/*" -print0 2>/dev/null)

echo "── node_modules 依赖 ──"
while IFS= read -r -d '' d; do del "$d"; done < \
    <(find . -type d -name node_modules -not -path "./.git/*" -print0 2>/dev/null)

echo "── StarGo 五子棋 MSVC 产物 ──"
if [[ -d StarGo ]]; then
    while IFS= read -r -d '' d; do del "$d"; done < \
        <(find StarGo -type d \( -name x64 -o -name Release -o -name Debug -o -name Win32 \) -print0 2>/dev/null)
    while IFS= read -r -d '' f; do del "$f"; done < \
        <(find StarGo -type f \( -name "*.exe" -o -name "*.obj" -o -name "*.ilk" -o -name "*.pdb" -o -name "*.lib" -o -name "*.exp" \) -print0 2>/dev/null)
fi

echo "── JetBrains Rider/IntelliJ 缓存 .idea ──"
while IFS= read -r -d '' d; do del "$d"; done < \
    <(find . -type d -name ".idea" -not -path "./.git/*" -print0 2>/dev/null)

echo "── VS/Rider 用户级配置 (*.user / *.suo) ──"
while IFS= read -r -d '' f; do del "$f"; done < \
    <(find . -type f \( -name "*.user" -o -name "*.suo" -o -name "*.binlog" \) \
        -not -path "./.git/*" -not -path "*/.vs/*" -not -path "*/bin/*" -not -path "*/obj/*" -print0 2>/dev/null)

echo "── 独立 publish 目录（bin/obj 内已由上面清理）──"
while IFS= read -r -d '' d; do del "$d"; done < \
    <(find . -type d -name publish -not -path "./.git/*" -not -path "*/bin/*" -not -path "*/obj/*" -print0 2>/dev/null)

echo "── MAUI/Android Gradle 构建缓存 .gradle ──"
while IFS= read -r -d '' d; do del "$d"; done < \
    <(find . -type d -name ".gradle" -not -path "./.git/*" -print0 2>/dev/null)

echo "── MAUI/Android 打包产物 (*.apk / *.aab) ──"
while IFS= read -r -d '' f; do del "$f"; done < \
    <(find_files "-name '*.apk' -o -name '*.aab'")

echo "── vscode-extension 产物 (out/ 与 *.vsix) ──"
if [[ -d vscode-extension ]]; then
    while IFS= read -r -d '' d; do del "$d"; done < \
        <(find vscode-extension -type d -name out -not -path "*/node_modules/*" -print0 2>/dev/null)
    while IFS= read -r -d '' f; do del "$f"; done < \
        <(find vscode-extension -type f -name "*.vsix" -print0 2>/dev/null)
fi

echo "── Python 缓存 (__pycache__ / *.pyc / .venv / venv) ──"
while IFS= read -r -d '' d; do del "$d"; done < \
    <(find . -type d \( -name __pycache__ -o -name .venv -o -name venv \) \
        -not -path "./.git/*" -not -path "*/node_modules/*" -print0 2>/dev/null)
while IFS= read -r -d '' f; do del "$f"; done < \
    <(find_files "-name '*.pyc'")

echo "── 测试/打包产物 (TestResults / BenchmarkDotNet.Artifacts / AppPackages / .store / verify_build / stress-test-output) ──"
while IFS= read -r -d '' d; do del "$d"; done < \
    <(find . -type d \( -name TestResults -o -name BenchmarkDotNet.Artifacts -o -name AppPackages -o -name ".store" \
        -o -name verify_build -o -name stress-test-output \) \
        -not -path "./.git/*" -not -path "*/bin/*" -not -path "*/obj/*" -print0 2>/dev/null)

echo "── dist/ 陈旧发布产物（保留 .waycoder 用户数据）──"
if [[ -d dist ]]; then
    while IFS= read -r -d '' item; do
        if [[ "$(basename "$item")" != ".waycoder" ]]; then
            del "$item"
        fi
    done < <(find dist -mindepth 1 -maxdepth 1 -print0 2>/dev/null)
else
    echo "  (无 dist 目录)"
fi

if [[ "$NO_GC" -eq 1 ]]; then
    echo "── Git 仓库压缩（--no-gc 已跳过）──"
else
    echo "── Git 仓库压缩（gc --aggressive + prune）──"
    if git rev-parse --git-dir >/dev/null 2>&1; then
        if [[ "$DRY_RUN" -eq 1 ]]; then
            echo "  [将执行] git gc --aggressive --prune=now"
        else
            GIT_BEFORE=$(du -sk .git 2>/dev/null | awk '{print $1}')
            git gc --aggressive --prune=now >/dev/null && echo "  [已压缩] git gc --aggressive --prune=now"
            GIT_AFTER=$(du -sk .git 2>/dev/null | awk '{print $1}')
            echo "  .git: ${GIT_BEFORE} KB → ${GIT_AFTER} KB"
        fi
    else
        echo "  (无 .git 仓库)"
    fi
fi

if [[ "$DRY_RUN" -eq 1 ]]; then
    echo "───────────────────────────────────────────"
    echo "🔎 干跑结束：以上为将删除项，未实际删除。"
else
    AFTER=$(du -sk . 2>/dev/null | awk '{print $1}')
    FREED=$(( (BEFORE - AFTER) / 1024 ))
    echo "───────────────────────────────────────────"
    echo "✅ 清理完成，释放约 ${FREED} MB。"
    echo "   （需重建：dotnet build 重新生成 bin/obj；扩展开发再 npm install）"
    echo "   保留：third_party/vml/Lib/**/*.vml（已跟踪的 vendored 源）、"
    echo "         WayCoder/works|saves、logs/、.waycoder/、dist/.waycoder、*.keystore"
fi
