#!/usr/bin/env python3
"""`gorilla.bas` 的画面验收 —— **逐点取色**，不靠肉眼。

## 为什么色值要按比例比，而不是按相等比

模拟器把整帧按 ~0.925 缩放（#4ADE80 量到 #44CD76、#FFB020 量到 #ECA31E …
每个通道的比例一致），这是设备色彩管理/色域转换，**不是程序画错了**。
所以判据是「实得 ≈ 期望 × k」，k 由同一帧里一个已知色块现算 —— 用**同一帧**的 k
做基准，就不会把"整帧偏一点"误判成"某个图元画错了"。

## 采哪些点，期望什么

采的点全部照 `gorilla.bas` 的 `drawScene` 推出来（源码里有确切坐标与颜色常量）：
角度条绿 `C_ANGLE`、力度条橙 `C_POWER`、发射键红 `C_FIRE`、HUD 高亮块 `C_HUD_ON`、
楼顶 `C_ROOF`、地面 `C_GROUND`、两只猿 `C_APE0`/`C_APE1`。
"""

import sys

sys.path.insert(0, __file__.rsplit("/", 1)[0])
from driver import Driver          # noqa: E402
from screen import grab, parse_hex  # noqa: E402

# gorilla.bas 的配色常量（0xAARRGGBB）
PALETTE = {
    "SKY":    "151228", "GROUND": "0B0A14", "HUD": "1E1B33", "HUD_ON": "37415E",
    "TRACK":  "2A2A44", "PANEL":  "171528", "ANGLE": "4ADE80", "POWER": "FFB020",
    "FIRE":   "D8443C", "ROOF":   "3E3E5E", "APE0":  "E8A33D", "APE1": "6FA8DC",
    "BANANA": "FFE066", "MARKER": "6FD3FF",
}


def scale_factor(shot):
    """整帧的统一缩放系数 k：拿**面板底色**（每帧都占一大片、且是纯色）现算。

    取中位数而不是均值 —— 抗锯齿边缘会混进别的颜色，中位数把它挡在外面。
    """
    want = parse_hex(PALETTE["PANEL"])
    ratios = []
    for y in range(shot.h // 2, shot.h, 7):
        for x in range(0, shot.w, 11):
            r, g, b, _ = shot.pixel(x, y)
            if r < 12 and g < 12 and b < 12:
                continue                      # 全黑是别的区域，不是面板
            for got, w in ((r, want[0]), (g, want[1]), (b, want[2])):
                if w and abs(got - w * 0.9) <= 12:
                    ratios.append(got / w)
    if not ratios:
        return None
    ratios.sort()
    return ratios[len(ratios) // 2]


def expect(rgb, k, tol=3):
    return tuple(max(0, min(255, round(c * k))) for c in rgb[:3])


def near(a, b, tol):
    return all(abs(x - y) <= tol for x, y in zip(a, b))


def main():
    serial = sys.argv[1] if len(sys.argv) > 1 else "emulator-5554"
    shot = grab(serial)
    print("帧缓冲 %dx%d fmt=%d" % (shot.w, shot.h, shot.fmt))

    npix = {}
    for name, hx in PALETTE.items():
        npix[name] = parse_hex(hx)
    k = scale_factor(shot)
    print("整帧缩放系数 k = %.4f" % (k if k else -1))

    ok = bad = 0

    def chk(cond, msg):
        nonlocal ok, bad
        if cond:
            ok += 1
            print("  ✔ " + msg)
        else:
            bad += 1
            print("  ✘ " + msg)

    # ── ② 每个图元：先找"整帧里有没有这个色"，再核"在不在它该在的位置" ──
    print("\n[1] 颜色是否出现（容忍整帧缩放 k）")
    found = {}
    for name in ("ANGLE", "POWER", "FIRE", "ROOF", "APE0", "APE1", "TRACK", "GROUND", "HUD_ON"):
        want = expect(npix[name], k, 0)
        bb = shot.bbox_of(want, tol=4, step=1)
        found[name] = bb
        chk(bb is not None, "%-7s #%s → %s" % (name, PALETTE[name], bb))

    # ── ③ 相对几何（照 drawScene 的坐标推，与设备分辨率无关）──────────────
    print("\n[2] 相对几何")
    # ⚠ **别拿某个颜色的包围盒当画布** —— 面板 / 轨道 / HUD 条这三个色本来就非常接近
    #   （#171528 / #2A2A44 / #1E1B33），整帧再乘一个 ~0.925，互相之间只差几个灰阶，
    #   按色找出来的 bbox 会横跨整幅。画布必须从**程序自己的几何**反推：
    #   发射键是 `ui_rect(14, …, sw-28, …)`、两根条是 `ui_rect(barX=76, …, sw-132, …)`
    #   ⇒ 两者左端相差 `(76-14)` 个场景单位，这就是一把现成的尺子。
    if found["FIRE"] and found["ANGLE"]:
        f, a = found["FIRE"], found["ANGLE"]
        s = (a[0] - f[0]) / (76 - 14)          # 每个场景单位 = 几屏幕像素
        x0 = f[0] - 14 * s
        x1 = f[2] + 14 * s
        y1 = f[3] + 8 * s                      # 发射键下沿离画布底 8
        y0 = found["HUD_ON"][1] - 5 * s if found["HUD_ON"] else None
        sw, sh = (x1 - x0) / s, (y1 - y0) / s if y0 else 0
        print("  推出来的画布：x %.1f..%.1f  y %.1f..%.1f（缩放 %.3f px/单位）"
              % (x0, x1, y0 or -1, y1, s))
        print("  推导出的场景尺寸：sw≈%.0f sh≈%.0f" % (sw, sh))
        chk(abs(x0 - 22) < 6, "画布左缘落在屏幕边距内（x0=%.1f）" % x0)

        p = found["POWER"]
        chk(abs(a[0] - p[0]) <= 6, "角度条与力度条左端对齐（%d vs %d）" % (a[0], p[0]))
        chk(p[1] > a[1], "力度条在角度条下方（y %d > %d）" % (p[1], a[1]))
        chk(f[1] > p[1], "发射键在力度条下方（y %d > %d）" % (f[1], p[1]))
        chk(abs(f[0] - x0 - 14 * s) < 4 and abs(x1 - f[2] - 14 * s) < 4,
            "发射键左右留白都等于 14 个场景单位")

        # 角度条：轨道 `ui_rect(76, ·, barW=sw-132, ·)`、绿填充 `barW*45/90`
        # ⇒ 应当**正好**半条（判据收到 ±2%：量出来的就是 0.500）。
        fill = a[2] - a[0]
        track = (sw - 132) * s
        ratio = fill / track if track else 0
        chk(0.48 < ratio < 0.52,
            "角度条填充 = 半条（aimA=45/90）：fill %d / track %.0f = %.3f" % (fill, track, ratio))
        # 力度条：aimP=70 ⇒ 正好七成（±2%）
        ratioP = (p[2] - p[0]) / track if track else 0
        chk(0.68 < ratioP < 0.72,
            "力度条填充 = 七成（aimP=70/100）：%.3f" % ratioP)

        # HUD_ON：turn=0 ⇒ 左上角 (6,5,100,30)
        h = found["HUD_ON"]
        chk(h[0] - x0 < 0.15 * (x1 - x0),
            "HUD 高亮块在左侧（turn=0）：偏移 %.0f / 画布宽 %.0f" % (h[0] - x0, x1 - x0))
        chk(abs((h[2] - h[0]) - 100 * s) < 8 * s, "HUD 高亮块宽 ≈ 100 单位（%.0f px）" % (h[2] - h[0]))
        # 地面在面板之上、楼顶不越过地面
        chk(found["GROUND"][3] < found["TRACK"][3], "地面在面板之上")
        chk(found["ROOF"][3] <= found["GROUND"][3] + 4, "楼顶不越过地面")
    else:
        chk(False, "找不齐发射键/角度条，无法定标")

    # ── ④ 两只猿各在一个水平半区 ─────────────────────────────────────────
    print("\n[3] 两只大猩猩（颜色不同 ⇒ 不在同一处、也不是同色）")
    if found["APE0"] and found["APE1"]:
        a0, a1 = found["APE0"], found["APE1"]
        chk(a0[2] < a1[0] or a1[2] < a0[0],
            "猿0 与 猿1 水平不重叠：%s vs %s" % (a0, a1))
        chk(a0[3] - a0[1] > 4 and a0[2] - a0[0] > 4, "猿0 有可见面积（%dx%d）"
            % (a0[2] - a0[0], a0[3] - a0[1]))

    print("\n通过 %d / 失败 %d" % (ok, bad))
    return 1 if bad else 0


if __name__ == "__main__":
    raise SystemExit(main())
