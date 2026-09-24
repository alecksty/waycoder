#!/usr/bin/env bash
# `PALETTE idx, color` 的**两参形式**：颜色号该按什么编码解？
#
# ⚠ **本闸门当前是红的（已知红）** —— 它压的是一个**已确认、尚未修**的缺陷。
#   留它在套件里的价值是**钉住机制**（修好那天它会自己变绿），所以它**不进**
#   `run.sh` 的通过/失败统计，退出码也固定 0（别把它接进任何聚合 runner）。
#
# ## 现象
#
# 原版 `GORILLAS.BAS` 的城市画出来了，但**大猩猩是黑的**、太阳是棕的。
# 查下来不是绘制的问题，是 `SetScreen` 里那组 `PALETTE` 没落到该落的颜色上。
#
# ## 机制（实测，不是猜）
#
# UI 后端把两参形式实现成「**把第 `color & 15` 项复制到第 `idx` 项**」
# （`CodeGenerator.Qbasic.UiGfx.cs` 的 `UiEmitPaletteStatement`）：
#
#     EvalIntCoord(stmt.Red, val);
#     AddRI(OpCode.MOVE, tmp, 15);
#     AddRR(OpCode.AND, val, tmp);      // ← 颜色号一律掩到 4 位
#
# 而 GORILLAS 传的是 **EGA 64 色**编号：
#
#     PALETTE 0, 1     '  1  → 恰好 < 16 ⇒ 复制第 1 项（蓝）    ✅ 蒙对
#     PALETTE 1, 46    ' 46 & 15 = 14 ⇒ 复制第 14 项（黄）      ❌
#     PALETTE 3, 54    ' 54 & 15 =  6 ⇒ 复制第 6 项（棕）       ❌
#
# ⇒ 只有"颜色号恰好小于 16"的那几条生效，其余静默串到别的项上。
#
# ## ⚠ 修之前必须先回答的问题
#
# **16 色索引**（`LINE …, 14` 这种）与 **64 色号**（`PALETTE` 的第二个参数）
# 在这个前端里是不是同一套编码？本后端的 `UiEgaR/G/B` 是**16 色**表
# （`blue = #0000AA` = 170），而 EGA 64 色编号里 1 给的是 85。
# 直接把 46 按 EGA-64 解成 RGB 就会**改变所有 BASIC 程序的配色** ——
# 判据要落在"同一屏里两种写法互相对得上"，不是"看着像原始 GORILLAS"。
#
# 用法： scripts/vml-basic-probe/palette.sh
# 依赖： scripts/vmlcli（先 `dotnet build scripts/vmlcli/vmlcli.csproj -c Release`）
set -u

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
CLI="${VMLCLI:-$ROOT/scripts/vmlcli/bin/Release/net10.0/vmlcli.dll}"
SRC="$ROOT/scripts/vml-basic-probe/palette_repro.bas"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

if [ ! -f "$CLI" ]; then
    echo "✘ 找不到 $CLI —— 先跑： dotnet build scripts/vmlcli/vmlcli.csproj -c Release"
    exit 2
fi

dotnet "$CLI" "$SRC" --timeout 10 --frame "$TMP/pal.png" --screen 640x480 \
    >/dev/null 2>"$TMP/err.txt"
if [ ! -s "$TMP/pal.png" ]; then
    echo "✘ 没抓到帧 —— 程序没跑起来？"
    tail -5 "$TMP/err.txt"
    exit 1
fi

python3 "$ROOT/scripts/vml-basic-probe/check_palette.py" "$TMP/pal.png" "$ROOT/scripts/vml-basic-probe"

# 已知红：无论结果如何都退 0，且**不接进任何聚合 runner**（见文件头第 3 行起的说明）
exit 0
