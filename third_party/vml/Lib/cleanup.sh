#!/bin/bash
# ============================================================
# Lib/cleanup.sh — 清理所有编译产生的 VML 文件和临时文件
#
# 保留手写的 VML 源文件:
#   Lib/vml/**/*.vml      — BIOS / Device 定义
#   Lib/shared/syscall.inc.vml — 系统调用常量定义
#   Lib/shared/include.vml     — 函数别名定义
#   Lib/shared/shared.vml      — 主聚合参考文件
#   Lib/shared/shared_simple.vml — 简化聚合参考文件
#   Lib/Basic/stdlib.vml   — 手写 BASIC 运行时
#   Lib/Pascal/stdlib.vml  — 手写 Pascal 运行时
#   Lib/ladder/stdlib.vml  — 手写 Ladder 运行时
# ============================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

TOTAL=0

clean_vml() {
    local file="$1"
    if [ -f "$file" ]; then
        rm -f "$file"
        TOTAL=$((TOTAL + 1))
    fi
}

echo "=== 清理 Lib/ 编译产物 ==="

# ---- 1. 清理 shared/ 编译自 src/*.c 的 .vml ----
echo "[1/6] 清理 Lib/shared/ 编译模块 ..."
for cfile in shared/src/*.c; do
    [ -f "$cfile" ] || continue
    name=$(basename "$cfile" .c)
    clean_vml "shared/${name}.vml"
done
# 清理 shared/ 根级 .c 编译产物 (graphics.c, browser_gfx.c)
for cfile in shared/*.c; do
    [ -f "$cfile" ] || continue
    name=$(basename "$cfile" .c)
    clean_vml "shared/${name}.vml"
done

# ---- 2. 清理 c/ 编译自 .c 的 .vml ----
echo "[2/6] 清理 Lib/c/ 编译模块 ..."
for cfile in c/*.c; do
    [ -f "$cfile" ] || continue
    name=$(basename "$cfile" .c)
    clean_vml "c/${name}.vml"
done
# c/ 下的聚合/特殊文件
clean_vml "c/stdlib_complete.vml"
clean_vml "c/stdio_funcs.vml"

# ---- 3. 清理 dynamic/ 编译自 .c 的 .vml ----
echo "[3/6] 清理 Lib/dynamic/ 编译模块 ..."
for cfile in dynamic/*.c; do
    [ -f "$cfile" ] || continue
    name=$(basename "$cfile" .c)
    clean_vml "dynamic/${name}.vml"
done

# ---- 4. 清理各语言聚合 .vml ----
echo "[4/6] 清理各语言聚合文件 ..."
# 纯聚合语言的 stdlib.vml (只含 .linked  指令的)
AGGR_LANGS="cpp csharp forth go java javascript Kotlin lua python rust Scheme swift ruby dart objc r d fortran"
for lang in $AGGR_LANGS; do
    clean_vml "${lang}/stdlib.vml"
    clean_vml "${lang}/stdlib_complete.vml"
    clean_vml "${lang}/builtin.vml"
    clean_vml "${lang}/vmllib.vml"
done
# 手写语言只清理聚合文件，保留手写 stdlib.vml
for lang in Basic Pascal ladder; do
    clean_vml "${lang}/stdlib_complete.vml"
    clean_vml "${lang}/builtin.vml"
    clean_vml "${lang}/vmllib.vml"
done

# ---- 5. 清理 dynamic/ 额外聚合文件 ----
echo "[5/6] 清理额外文件 ..."
# 清理 dynamic/lang/ 下的文件
rm -rf dynamic/lang/ 2>/dev/null || true
# 清理 shared/backup (如还存在)
rm -rf shared/backup/ 2>/dev/null || true

# ---- 6. 汇总 ----
echo "[6/6] 完成。共清理 $TOTAL 个 .vml 文件。"
echo ""
echo "保留的手写文件:"
echo "  Lib/vml/Bios/*.vml        — BIOS 实现"
echo "  Lib/vml/Device/*.vml      — 设备定义"
echo "  Lib/shared/syscall.inc.vml — 系统调用定义"
echo "  Lib/shared/include.vml     — 函数别名"
echo "  Lib/shared/shared.vml      — 主聚合参考"
echo "  Lib/shared/shared_simple.vml — 简化聚合参考"
echo "  Lib/Basic/stdlib.vml       — 手写 BASIC 运行时"
echo "  Lib/Pascal/stdlib.vml      — 手写 Pascal 运行时"
echo "  Lib/ladder/stdlib.vml      — 手写 Ladder 运行时"
