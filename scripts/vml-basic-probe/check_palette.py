#!/usr/bin/env python3
"""`palette_repro.bas` 的判据 —— `PALETTE idx, color` 里的**颜色号**解得对不对。

判据是**确切 RGB**，不是"看着像"。两组：

## A 行（16 格）—— 解码规则的验证

期望值 = **标准 EGA 16 色**（下面 EGA_R/G/B 三行），取样的是
`PALETTE i, REG(i)` 之后用属性 i 画出来的格子，其中 `REG` 是
**EGA 默认调色板寄存器值** `{0,1,2,3,4,5,20,7,56,57,58,59,60,61,62,63}`
（⚠ **不是 0..15** —— 棕的寄存器是 20、暗灰是 56，这正是"把颜色号当属性号"那种实现会错的根源）。

16 条里**对 15 条**。唯一不同的是**属性 14 的蓝分量**：默认寄存器 62 = `111110`
按 EGA 硬件解出 B = 85 → `#FFFF55`，而标准十六色表写的是 `#FFFF00`（VGA 习惯）。
**这条差别是如实钉住的，不是放宽**：期望值直接写成 `#FFFF55`，谁把它改成 0 谁就得解释。

## B 行（3 格）—— GORILLAS 真正用到的三个颜色号

`PALETTE 0, 1` / `PALETTE 1, 46` / `PALETTE 3, 54`（天空 / 大猩猩 / 太阳）。
期望值由位布局手算：bit0/1/2 = 蓝/绿/红「次级」、bit3/4/5 = 蓝/绿/红「主级」，
档位 1→170、2→85、3→255（非单调，见 `CodeGenerator.Qbasic.UiGfx.cs` 的 `UiEgaLevel`）：

    1  = 0b000001 → 蓝次级                  → (  0,  0,170)
    46 = 0b101110 → 红主+红次, 绿次, 蓝主   → (255,170, 85)
    54 = 0b110110 → 红主+红次, 绿主+绿次    → (255,255,  0)

修前这三格是 `#FFFF00` / `#AA5500` / `#0000AA` —— 前两个是 `46 & 15` / `54 & 15`
那一项，也就是"颜色号被掩成 4 位"的指纹。

用法： check_palette.py <帧.png> [本目录]      退出码：0 = 全对，1 = 有出入
"""

import sys

sys.path.insert(0, sys.argv[2] if len(sys.argv) > 2 else '.')
from png_pixels import read_png, near, hexs  # noqa: E402

# 标准 EGA 16 色（与前端 `UiEgaR/G/B` 同一张表）
EGA_R = [0, 0, 0, 0, 170, 170, 170, 170, 85, 85, 85, 85, 255, 255, 255, 255]
EGA_G = [0, 0, 170, 170, 0, 0, 85, 170, 85, 85, 255, 255, 85, 85, 255, 255]
EGA_B = [0, 170, 0, 170, 0, 170, 0, 170, 85, 255, 85, 255, 85, 255, 0, 255]

# ⚠ 属性 14：EGA 默认寄存器 62 解出的蓝分量是 85（硬件黄 #FFFF55），
#   而上面那张标准十六色表写的是 0（VGA 黄 #FFFF00）。**期望值按硬件写**。
EGA_B_HW = list(EGA_B)
EGA_B_HW[14] = 85

# B 行：GORILLAS 的三个颜色号
GORILLA_CASES = [
    (0, 29, 0, 'PALETTE 0, 1 ', 1, (0x00, 0x00, 0xAA)),
    (1, 89, 1, 'PALETTE 1, 46', 46, (0xFF, 0xAA, 0x55)),
    (2, 149, 3, 'PALETTE 3, 54', 54, (0xFF, 0xFF, 0x00)),
]


def main():
    W, H, px = read_png(sys.argv[1])
    bad = 0

    print(f'调色板用例 帧 {W}x{H}')
    print('  A 行：PALETTE i, REG(i) 应当还原属性 i 的默认色（EGA 默认寄存器）')
    for i in range(16):
        got = px(i * 40 + 19, 40)
        want = (EGA_R[i], EGA_G[i], EGA_B_HW[i])
        ok = near(got, want)
        bad += 0 if ok else 1
        mark = '✅' if ok else '❌'
        print(f'    {mark} 属性 {i:2d}：实得 {hexs(got)}，期望 {hexs(want)}')

    print('  B 行：GORILLAS 用的三个颜色号（46 / 54 是"颜色号被掩成 4 位"的指纹）')
    for _, sx, attr, desc, num, want in GORILLA_CASES:
        got = px(sx, 150)
        ok = near(got, want)
        bad += 0 if ok else 1
        mark = '✅' if ok else '❌'
        print(f'    {mark} {desc} → 属性 {attr}，颜色号 {num:2d}：'
              f'实得 {hexs(got)}，期望 {hexs(want)}')

    print('─' * 46)
    if bad:
        print(f'❌ PALETTE 颜色号解码：{bad}/19 条不符')
    else:
        print('✅ PALETTE 颜色号解码：A 行 16/16（属性 14 按 EGA 硬件 #FFFF55）＋ B 行 3/3')
    return 1 if bad else 0


if __name__ == '__main__':
    sys.exit(main())
