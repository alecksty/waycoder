#!/bin/bash
# ═══════════════════════════════════════════════════════════════
# WayCoder 清除编译垃圾
# 用法: ./scripts/clean.sh [--dry-run|-n]
# 说明: 删除 C# 项目的 bin/obj、.vs/.idea IDE 缓存、*.user 用户配置、
#       node_modules 依赖、StarGo 五子棋的 MSVC 产物（x64/Release/Debug + exe/obj/ilk/pdb）、
#       独立 publish 目录、MAUI 的 .gradle / .apk 产物、TestResults / BenchmarkDotNet.Artifacts /
#       AppPackages / .store 测试打包产物、dist/ 下的陈旧发布产物
#       —— 保留 dist/.waycoder 与项目 .waycoder 运行时用户数据；
#       清理完成后自动 git gc --aggressive 压缩仓库（大仓库较慢，可用 --no-gc 跳过）。
#       全部为 .gitignore 忽略的构建产物，不影响源码与 git 状态。
#       --dry-run 只列出将删除项，不实际删除。
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

# 在脚本目录内安全查找构建目录（自动为多条件加括号，防 -o 优先级丢失）
safe_find_dirs() {
    # $1 = 匹配名称片段（如 "-name bin -o -name obj"），括号化后与 type/排除条件 AND
    local match="$1"
    find . -type d \( $match \) -not -path "./.git/*" -not -path "*/node_modules/*" -print0 2>/dev/null
}

echo "── C# 构建产物 bin/obj ──"
while IFS= read -r -d '' d; do del "$d"; done < \
    <(safe_find_dirs "-name bin -o -name obj")

echo "── IDE 缓存 .vs ──"
while IFS= read -r -d '' d; do del "$d"; done < \
    <(find . -type d -name ".vs" -not -path "./.git/*" -print0 2>/dev/null)

echo "── node_modules 依赖 ──"
while IFS= read -r -d '' d; do del "$d"; done < \
    <(find . -type d -name node_modules -not -path "./.git/*" -print0 2>/dev/null)

echo "── StarGo 五子棋 MSVC 产物 ──"
while IFS= read -r -d '' d; do del "$d"; done < \
    <(find StarGo -type d \( -name x64 -o -name Release -o -name Debug -o -name Win32 \) -print0 2>/dev/null)
while IFS= read -r -d '' f; do del "$f"; done < \
    <(find StarGo -type f \( -name "*.exe" -o -name "*.obj" -o -name "*.ilk" -o -name "*.pdb" -o -name "*.lib" -o -name "*.exp" \) -print0 2>/dev/null)

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

echo "── 测试/打包产物 (TestResults / BenchmarkDotNet.Artifacts / AppPackages / .store) ──"
while IFS= read -r -d '' d; do del "$d"; done < \
    <(find . -type d \( -name TestResults -o -name BenchmarkDotNet.Artifacts -o -name AppPackages -o -name ".store" \) \
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
fi
