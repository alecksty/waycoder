#!/usr/bin/env bash
# `PALETTE idx, color` 的**两参形式**：第二个参数是「颜色号」，不是「属性号」。
#
# ## 这个闸门压的缺陷（v0.96.407 发现，v0.96.408 修）
#
# UI 后端原来把两参形式实现成「**把第 `color & 15` 项复制到第 `idx` 项**」
# （`CodeGenerator.Qbasic.UiGfx.cs` 的 `UiEmitPaletteStatement`）—— 把颜色号当成了属性下标。
# 颜色号 0–15 恰好蒙对（EGA 默认调色板是恒等映射），**16–63 静默串到 `& 15` 那一项上**：
#
#     PALETTE 0, 1     '  1  → 复制第 1 项（蓝）              ✅ 蒙对
#     PALETTE 1, 46    ' 46 & 15 = 14 ⇒ 复制第 14 项（黄）    ❌ 应为 #FFAA55
#     PALETTE 3, 54    ' 54 & 15 =  6 ⇒ 复制第  6 项（棕）    ❌ 应为 #FFFF00
#
# 症状不是"某块颜色不对"而是**整屏一个色**：`GORILLAS.BAS` 是**动态**用它的 ——
# 同一个属性 1 在"天空色"与"物体色"之间来回改（`GORILLAS.BAS:604/614` 的
# `PALETTE OBJECTCOLOR, BackColor` / `, 46`），串一次之后天空、大猩猩、城市全变成同一个色。
#
# ## 修法
#
# 颜色号按 **EGA 64 色**解（`UiEga64Argb`）：bit0/1/2 = 蓝/绿/红「次级」、
# bit3/4/5 = 蓝/绿/红「主级」，档位 **1→170、2→85、3→255**（非单调，别"顺手改成 档位×85"）。
# 解码表被**整个 16 色默认调色板**验证过：把默认寄存器值
# `{0,1,2,3,4,5,20,7,56,57,58,59,60,61,62,63}` 逐项喂进去，得到的 RGB
# 与前端已有的 EGA 16 色表**逐项相同**（16/16）—— 这条断言钉在 `WayCoder/Test` 里。
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
