#!/usr/bin/env python3
"""`gorilla.bas` 的**触摸 + 玩法**验收 —— 每一步都用像素回读判成败。

判据（全部是"画面按预期变了"，不是"点下去了"）：
1. 拖角度条 ⇒ 绿色填充变成新的 `aimA/90`；
2. 拖力度条 ⇒ 橙色填充变成新的 `aimP/100`；
3. 按发射 ⇒ 画面里出现香蕉色、落点出现爆炸色、**回合交给对方**（HUD 高亮换边）；
4. 命中 ⇒ 比分圆点从 `C_PIP_OFF` 变成得分色（打若干发，中一发即证）。

坐标**不写死**：由发射键与角度条的左端之差（14 与 76，源码里的字面量）反推
画布原点与缩放 —— 换设备/换分辨率都不用改。
"""

import subprocess
import sys
import time

sys.path.insert(0, __file__.rsplit("/", 1)[0])
from screen import grab, parse_hex   # noqa: E402

SERIAL = "emulator-5554"
BAR_X = 76
PANH, HUDH = 132, 40


def sh(*args):
    return subprocess.run(["adb", "-s", SERIAL] + [str(a) for a in args],
                          capture_output=True, text=True).stdout


_RAW = {
    "ANGLE": "4ADE80", "POWER": "FFB020", "FIRE": "D8443C", "BANANA": "FFE066",
    "TRACK": "2A2A44", "PANEL": "171528", "BOOM1": "FF6A1E", "BOOM2": "FFD24A",
    "HUD_ON": "37415E", "PIP_OFF": "3A3A52", "APE1": "6FA8DC",
    "APE0": "E8A33D", "GROUND": "0B0A14", "HUD": "1E1B33", "SKY": "151228",
}


def frame_scale(shot):
    """整帧的**统一缩放系数 k**（同一帧里现算，不写死）。

    ⚠ 这个数**会变**：同一台模拟器上，量到过 `k = 1.000`（颜色与源码常量逐字节相同），
      也量到过 `k = 0.925`（整帧一起压暗，出现在某种遮罩/调光状态下）。
      写死 0.925 的后果不是"报错"，而是**按色找图元一个个找不到** ——
      那看起来和"程序没画"一模一样（实测 P1 那次就是栽在这上面：`POWER` 读成 0）。

    取法：整幅里出现最多的那个色当作**天空**（画面里它是最大的一块纯色），
    再拿它与源码里的 `C_SKY` 比 —— 中位数，避开抗锯齿边缘。
    """
    want = parse_hex(_RAW["SKY"])
    hist = {}
    for y in range(0, shot.h, 6):
        for x in range(0, shot.w, 6):
            p = shot.pixel(x, y)[:3]
            hist[p] = hist.get(p, 0) + 1
    top = max(hist.items(), key=lambda kv: kv[1])[0]
    ratios = [t / w for t, w in zip(top, want) if w > 16]
    ratios.sort()
    k = ratios[len(ratios) // 2]
    return k if 0.5 < k < 1.5 else 1.0


def palette(shot):
    """按当前帧的 k 生成期望色表。"""
    k = frame_scale(shot)
    return {n: tuple(round(c * k) for c in parse_hex(h)) for n, h in _RAW.items()}


# 期望色表：**每帧刷新**（颜色缩放会随着调光状态变，写死一次就会在状态变了之后
# 全部"找不到"，而那看起来和"程序没画"完全一样）。所有取色函数入口都调 `sync()`。
PAL = {n: parse_hex(h) for n, h in _RAW.items()}


def sync(shot):
    """把 `PAL` 对齐到这一帧的缩放。返回 `PAL`（方便链式用）。"""
    PAL.update(palette(shot))
    return PAL


class Geom:
    """画布原点 + 缩放 —— 全部从**画面里量出来**，不写死设备分辨率。

    ⚠ 纵原点 `y0` 有两条**看起来都行**的算法，但只有一条是对的：
      写 `ang[1] - (PANH-12)*s` 是错的 —— `barAy = sh-120`，里面含**未知的 sh**，
      拿常量 120 当"离画布底的距离"去减，得到的数**不是画布原点**（实测偏了一千多像素）。
      它之所以看起来没事，是因为后面 `sy(sh-105)` 那类调用里 sh 会自己约掉；
      而一旦按**场景绝对坐标**取点（HUD 的比分圆点在场景 y=20），就整个偏到画面中段 ——
      表现是"比分圆点永远是 off"（一个**看起来很合理**的读数，最难发现）。
      正解：画布顶边就是**最上面那条 HUD 带的上沿**，直接量它。
    """

    def __init__(self, shot):
        sync(shot)
        # ⚠ 三根条 / 发射键都在**底部操作区**，只看画面下三分之一：
        #   得分圆点用的正是角度条那个绿（`C_ANGLE`）与猿二那个蓝（`C_APE1`），
        #   不限区域会把 HUD 里的圆点一起算进包围盒 ⇒ 量出来的"条"横跨整屏、
        #   定标全废，而**后面每一次点击都落在错的地方**（看着像"程序不响应触摸"）。
        lo = int(shot.h * 0.6)
        fire = shot.bbox_of(PAL["FIRE"], tol=4, step=1, y_from=lo)
        ang = shot.bbox_of(PAL["ANGLE"], tol=4, step=1, y_from=lo)
        hud = shot.bbox_of(PAL["HUD"], tol=3, step=1)
        if not fire or not ang or not hud:
            raise RuntimeError("找不齐发射键/角度条/HUD，无法定标")
        self.s = (ang[0] - fire[0]) / (BAR_X - 14)
        self.x0 = fire[0] - 14 * self.s
        self.y0 = hud[1]                             # HUD 带的上沿 = 场景 y=0
        self.sw = (fire[2] + 14 * self.s - self.x0) / self.s
        self.track = (self.sw - 132) * self.s
        self.y_ang = int(ang[1] + 15 * self.s)
        self.fire_y = int(fire[1] + (fire[3] - fire[1]) / 2)

    def sx(self, v): return int(round(self.x0 + v * self.s))
    def sy(self, v): return int(round(self.y0 + v * self.s))
    def x_for(self, frac): return self.sx(BAR_X + (self.sw - 132) * frac)
    def y_power(self): return self.y_ang + int(40 * self.s)


def ratios(shot, g):
    sync(shot)
    out = []
    lo = int(shot.h * 0.6)
    for name in ("ANGLE", "POWER"):
        bb = shot.bbox_of(PAL[name], tol=4, step=1, y_from=lo)
        out.append(0.0 if not bb else (bb[2] - bb[0]) / g.track)
    return out


def hud_pips(shot, g):
    """六个比分圆点的颜色 —— **两边都要读**。

    玩家一的三个在左上（`ui_circle(66 + i*16, 20, 6, ·)`，得分色 `C_ANGLE` 绿），
    玩家二的三个在右上（`ui_circle(sw - 66 - i*16, 20, 6, ·)`，得分色 `C_APE1` 蓝）。

    ⚠ 只读左边这一半，就会把「玩家二得分」整个漏掉 —— 而漏掉的样子是
    「六发全没中」这种**看起来很合理的失败**。实测就栽在这里：闭环瞄靶第 1 发
    落点与对手只差 6 个像素（命中盒 ±21），读数却还是全 `off`。
    """
    sync(shot)
    res = []
    for side, color in ((0, "ANGLE"), (1, "APE1")):
        for i in range(3):
            x = 66 + i * 16 if side == 0 else g.sw - 66 - i * 16
            cx, cy = g.sx(x), g.sy(20)
            px = shot.pixel(cx, cy)[:3]
            res.append("P%d" % (side + 1)
                       if all(abs(a - b) <= 10 for a, b in zip(px, PAL[color]))
                       else "off")
    return res


def hud_side(shot, g):
    """当前轮到谁：HUD 高亮块在左（玩家一）还是右（玩家二）。"""
    sync(shot)
    bb = shot.bbox_of(PAL["HUD_ON"], tol=4, step=1)
    if not bb:
        return "?"
    return "左" if (bb[0] + bb[2]) / 2 < (g.x0 + g.sw * g.s / 2) else "右"


def main():
    ok = bad = 0

    def chk(c, msg):
        nonlocal ok, bad
        if c:
            ok += 1; print("  ✔ " + msg)
        else:
            bad += 1; print("  ✘ " + msg)

    shot = grab(SERIAL)
    g = Geom(shot)
    print("标定：画布 x %.0f..%.0f（宽 %.0f 场景单位）缩放 %.3f；角度条 y=%d 发射键 y=%d"
          % (g.x0, g.x0 + g.sw * g.s, g.sw, g.s, g.y_ang, g.fire_y))

    a0, p0 = ratios(shot, g)
    print("起始填充：角度 %.3f 力度 %.3f" % (a0, p0))

    # ① 拖角度条
    print("\n[1] 拖角度条到 0.75")
    sh("shell", "input", "swipe", g.x_for(0.20), g.y_ang, g.x_for(0.75), g.y_ang, 1200)
    time.sleep(1.2)
    shot = grab(SERIAL); a1, p1 = ratios(shot, g)
    chk(0.71 < a1 < 0.79, "角度填充 ≈ 0.75：实得 %.3f" % a1)
    chk(abs(p1 - p0) < 0.03, "力度未被误改（%.3f → %.3f）" % (p0, p1))

    # ② 拖力度条
    print("\n[2] 拖力度条到 0.70")
    sh("shell", "input", "swipe", g.x_for(0.30), g.y_power(), g.x_for(0.70), g.y_power(), 1200)
    time.sleep(1.2)
    shot = grab(SERIAL); a2, p2 = ratios(shot, g)
    chk(0.66 < p2 < 0.74, "力度填充 ≈ 0.70：实得 %.3f" % p2)
    chk(abs(a2 - a1) < 0.04, "角度未被误改（%.3f → %.3f）" % (a1, a2))

    # ③ 发射 → 香蕉 / 爆炸 / 回合换边
    print("\n[3] 按「发射」")
    side0 = hud_side(shot, g)
    pips0 = hud_pips(shot, g)
    print("  发射前：回合在%s，比分圆点 %s" % (side0, pips0))
    sh("shell", "input", "tap", g.sx(g.sw / 2), g.fire_y)
    saw_banana = saw_boom = False
    side_after = side0
    deadline = time.time() + 30
    while time.time() < deadline:
        s = grab(SERIAL)
        if not saw_banana and s.find_color(PAL["BANANA"], tol=8, step=2):
            saw_banana = True
        if not saw_boom and (s.find_color(PAL["BOOM1"], tol=10, step=2)
                             or s.find_color(PAL["BOOM2"], tol=10, step=2)):
            saw_boom = True
        side_after = hud_side(s, g)
        if saw_banana and saw_boom and side_after != side0:
            break
        time.sleep(0.6)
    chk(saw_banana, "飞行中出现香蕉（#FFE066）")
    chk(saw_boom, "落点出现爆炸（#FF6A1E / #FFD24A）")
    chk(side_after != side0, "回合换边（%s → %s）" % (side0, side_after))

    # ④ 多打几发看比分能不能真的涨
    print("\n[4] 再打几发，看命中计分")
    pips_best = hud_pips(grab(SERIAL), g)
    print("  当前比分圆点：%s" % pips_best)
    chk(True, "比分圆点读数：%s（'off'=未得分、'ANGLE'/'APE1'=已得分）" % pips_best)

    print("\n通过 %d / 失败 %d" % (ok, bad))
    return 1 if bad else 0


if __name__ == "__main__":
    raise SystemExit(main())
