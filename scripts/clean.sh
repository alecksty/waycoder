#!/bin/bash
# ═══════════════════════════════════════════════════════════════
# WayCoder 清除编译垃圾
# 用法: ./scripts/clean.sh [--dry-run|-n]
# 说明: 删除 C# 项目的 bin/obj、.vs IDE 缓存、node_modules 依赖、
#       StarGo 五子棋的 MSVC 产物（x64/Release/Debug + exe/obj/ilk/pdb）、
#       dist/ 下的陈旧发布产物 —— 保留 dist/.waycoder 运行时用户数据。
#       全部为 .gitignore 忽略的构建产物，不影响源码与 git 状态。
#       --dry-run 只列出将删除项，不实际删除。
# ═══════════════════════════════════════════════════════════════
set -uo pipefail

DRY_RUN=0
if [[ "${1:-}" == "--dry-run" || "${1:-}" == "-n" ]]; then
    DRY_RUN=1
fi

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
