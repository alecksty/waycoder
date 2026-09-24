#!/usr/bin/env python3
"""`palette_repro.bas` 的判据 —— **颜色号 ≥16 时有没有串到 `& 15` 那一项上**。

⚠ **判据刻意只钉"机制"、不钉"目标解码"。** 颜色号 46 / 54 到底该解成哪个 RGB
还没查实（见 `palette.sh` 文件头的"修之前必须先回答的问题"）—— 钉一个我猜的值
等于把猜测写进闸门，而闸门最大的用处就是"**能响**"。这里钉的是**已实测的那个事实**：
两个块的颜色**正好等于 `color & 15` 那一项的默认色**，也就是颜色号被掩成了 4 位。

坐标对照 `palette_repro.bas`：
    (5,5)   背景     ← PALETTE 0, 1
    (50,30) 第二个块 ← PALETTE 1, 46
    (50,90) 第三个块 ← PALETTE 3, 54

用法： check_palette.py <帧.png> <本目录>      退出码：0 = 合格，1 = 命中缺陷
"""

import sys

sys.path.insert(0, sys.argv[2] if len(sys.argv) > 2 else '.')
from png_pixels import read_png, near, hexs  # noqa: E402

SKY_BLUE = (0x00, 0x00, 0xAA)   # 默认第 1 项（EGA 16 色的蓝）
ALIAS_46 = (0xFF, 0xFF, 0x00)   # 默认第 14 项 —— `46 & 15` 指到的那一项
ALIAS_54 = (0xAA, 0x55, 0x00)   # 默认第  6 项 —— `54 & 15` 指到的那一项


def main():
    W, H, px = read_png(sys.argv[1])
    bad = 0
    print(f'调色板用例 帧 {W}x{H}')

    # ① 正向对照：颜色号 1 < 16 ⇒ 复制第 1 项，这条**本来就该对**
    got = px(5, 5)
    ok = near(got, SKY_BLUE)
    bad += 0 if ok else 1
    print(f'  {"✅" if ok else "❌"} (5,5)   PALETTE 0, 1 → 第 1 项：'
          f'实得 {hexs(got)}，期望 {hexs(SKY_BLUE)}')

    # ②③ 缺陷本体：颜色号 ≥ 16 时**不该**串到 `& 15` 那一项上
    for x, y, desc, alias, num in [
        (50, 30, 'PALETTE 1, 46', ALIAS_46, 46),
        (50, 90, 'PALETTE 3, 54', ALIAS_54, 54),
    ]:
        got = px(x, y)
        aliased = near(got, alias)
        bad += 1 if aliased else 0
        tail = (f'（= 第 {num & 15} 项，**串了**）' if aliased
                else '（不是 `& 15` 那一项，合格）')
        print(f'  {"❌" if aliased else "✅"} ({x},{y}) {desc} → 颜色号 {num}：'
              f'实得 {hexs(got)} {tail}')

    print('─' * 46)
    if bad:
        print(f'❌ PALETTE 两参形式：{bad}/3 条不符（**已知红**，缺陷未修）')
    else:
        print('✅ PALETTE 两参形式：颜色号 ≥16 不再串到 `& 15` 那一项 —— 缺陷已修；')
        print('   把它从"已知红"里摘出来（palette.sh 退 0 且不接聚合 runner 的两条说明一并去掉）')
    return 1 if bad else 0


if __name__ == '__main__':
    sys.exit(main())
