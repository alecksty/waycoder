#!/usr/bin/env python3
"""命中判定验收 —— 按「力度扇面」连打，看**比分圆点**有没有真的变。

## 为什么不闭环跟着落点修力度

第一版是闭环的（从爆炸色的包围盒读落点、按"射程 ∝ 力度"修）。`adb exec-out screencap`
一次要 0.4~0.9 秒，而爆炸只持续 `boomT >= 8` 拍 × 30ms ≈ **240ms** ⇒ **大多数时候根本
采不到爆炸**，采到的那几次也是碰运气。结果是一串自相矛盾的落点（力度 0.46 打 91px、
0.30 反而打 205px），闭环据此修力度只会越修越偏。

⇒ 判据要挑**持续存在**的东西：比分圆点是 `drawScene` 每帧按 `sc0/sc1` 重画的，
**一直在那儿**，采到就一定准。于是改成：45° 固定，力度扫一遍，每发之后读圆点。
风每回合随机，所以单发不保证中；扇面覆盖过去，中一发就足以证明命中判定在工作。

⚠ 圆点**两边都要读**（玩家一在左上、玩家二在右上）—— 只读半边会把"对手得分"
整个漏掉，而漏掉的样子是"八发全没中"这种看起来很合理的失败（实测栽过）。
"""

import sys
import time

sys.path.insert(0, __file__.rsplit("/", 1)[0])
from gorilla_touch import Geom, hud_pips, hud_side, sh   # noqa: E402
from screen import grab                                  # noqa: E402

SERIAL = "emulator-5554"
ANGLE_FRAC = 0.5          # 45°
POWERS = [0.70, 0.62, 0.78, 0.66, 0.74, 0.58, 0.82, 0.50]


def main():
    shot = grab(SERIAL)
    g = Geom(shot)
    print("标定：缩放 %.3f 画布宽 %.0f" % (g.s, g.sw))
    pips0 = hud_pips(shot, g)
    print("起始圆点:", pips0)

    for n, fp in enumerate(POWERS, 1):
        s = grab(SERIAL)
        pips = hud_pips(s, g)
        if any(p != "off" for p in pips):
            print("✔ 第 %d 发之前就已经有分：%s —— 命中判定确认工作" % (n, pips))
            return 0
        side = hud_side(s, g)
        print("#%d 回合在%s，力度=%.2f" % (n, side, fp))

        # 设角度与力度（拖条），再点发射键
        sh("shell", "input", "swipe", g.x_for(0.20), g.y_ang, g.x_for(ANGLE_FRAC), g.y_ang, 800)
        sh("shell", "input", "swipe", g.x_for(0.30), g.y_power(), g.x_for(fp), g.y_power(), 800)
        time.sleep(0.7)
        sh("shell", "input", "tap", g.sx(g.sw / 2), g.fire_y)

        # 等这一发结算完（飞行 + 爆炸 + resolveShot）；圆点是持续存在的，慢慢读没关系
        deadline = time.time() + 25
        while time.time() < deadline:
            time.sleep(2.5)
            p2 = hud_pips(grab(SERIAL), g)
            if any(p != "off" for p in p2):
                print("✔ 第 %d 发命中并计分：六点 = %s（P1=玩家一绿 / P2=玩家二蓝）" % (n, p2))
                return 0
            if hud_side(grab(SERIAL), g) != side:
                break        # 回合换了 = 这一发已结算
    print("✘ 扇面打完仍未得分（圆点仍是 %s）" % hud_pips(grab(SERIAL), g))
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
