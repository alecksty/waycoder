#!/usr/bin/env python3
"""真机画面采样 —— 取**原始帧缓冲**并在指定点上比色。

## 为什么不用 PNG

`adb exec-out screencap -p` 出来的 PNG 要一个解码器（本机没有 PIL / numpy），
而 `screencap` **不带 `-p` 时吐的是裸帧缓冲**：12 字节头（w/h/format，各一个
little-endian uint32）+ `w*h*4` 字节 RGBA。一次 `zlib` 都不用，逐点取色就是
`buf[12 + (y*w + x)*4 : +4]`。

（这条是本仓 `Examples/c/draw_colors.c` 与编辑器排版那几轮验下来的老做法：
 **别靠肉眼说"看着对"**，把"期望什么色 / 实得什么色"打印出来。）

## 判据的形状

`sample()` 只回答"这一点是什么色"，**不做任何判断** —— 期望值是调用方给的。
这样同一份工具既能查"天空是不是那个紫"，也能查"这条按键有没有被渐变污染"。
"""

import re
import subprocess
import sys

# `screencap` 裸输出的头：3 个 little-endian uint32
_HDR = 12


class Shot:
    """一帧原始帧缓冲。"""

    def __init__(self, raw):
        if len(raw) < _HDR:
            raise ValueError("帧缓冲太短（%d 字节）—— screencap 失败？" % len(raw))
        self.w = int.from_bytes(raw[0:4], "little")
        self.h = int.from_bytes(raw[4:8], "little")
        self.fmt = int.from_bytes(raw[8:12], "little")
        need = _HDR + self.w * self.h * 4
        if len(raw) < need:
            # 有些设备会多带一行 padding；短了才是真错
            raise ValueError("帧缓冲不完整：期望 %d，实得 %d" % (need, len(raw)))
        self.buf = raw

    def pixel(self, x, y):
        """返回 (r, g, b, a)；越界抛 IndexError。"""
        if not (0 <= x < self.w and 0 <= y < self.h):
            raise IndexError("(%d,%d) 不在 %dx%d 里" % (x, y, self.w, self.h))
        i = _HDR + (y * self.w + x) * 4
        return tuple(self.buf[i:i + 4])

    def hexat(self, x, y):
        r, g, b, _ = self.pixel(x, y)
        return "#%02X%02X%02X" % (r, g, b)

    # ── 找色块（用来定位，不写死坐标）────────────────────────────────────
    def find_color(self, rgb, tol=6, step=2):
        """返回第一个与 rgb 足够接近的像素坐标；找不到返回 None。

        扫描步长默认 2px —— 只用来**定位**（找出画面在屏幕上的哪一块），
        不需要逐像素精确。
        """
        want = tuple(rgb)
        for y in range(0, self.h, step):
            for x in range(0, self.w, step):
                r, g, b, _ = self.pixel(x, y)
                if abs(r - want[0]) <= tol and abs(g - want[1]) <= tol and abs(b - want[2]) <= tol:
                    return (x, y)
        return None

    def bbox_of(self, rgb, tol=6, step=2, y_from=0, y_to=None):
        """`[y_from, y_to)` 里所有接近 rgb 的像素的包围盒 → (x1,y1,x2,y2)，没有则 None。

        ⚠ **限区域不是优化，是正确性**：同一种颜色在画面上常常出现两次 ——
        比如 `gorilla.bas` 里得分圆点用的正是角度条那个绿（`C_ANGLE`），
        不分区域就会把"左上角那颗亮起来的圆点"和"底部那根条"算进同一个包围盒，
        于是量出来的"条"横跨整屏、定标全废。**而它不报错** ——
        只是后面每一次点击都落在错的地方（看起来像"程序不响应触摸"）。
        """
        want = tuple(rgb)
        hi = self.h if y_to is None else min(y_to, self.h)
        xs, ys = [], []
        for y in range(max(0, y_from), hi, step):
            for x in range(0, self.w, step):
                r, g, b, _ = self.pixel(x, y)
                if abs(r - want[0]) <= tol and abs(g - want[1]) <= tol and abs(b - want[2]) <= tol:
                    xs.append(x)
                    ys.append(y)
        if not xs:
            return None
        return (min(xs), min(ys), max(xs), max(ys))


def parse_hex(s):
    """`#RRGGBB` / `FFRRGGBB` / `#AARRGGBB` → (r,g,b)。"""
    s = s.strip().lstrip("#")
    if len(s) == 8:      # AARRGGBB（本仓配色常量就是这个形状）
        s = s[2:]
    if len(s) != 6:
        raise ValueError("认不出颜色 %r" % s)
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16))


def grab(serial="emulator-5554"):
    """抓一帧裸帧缓冲。"""
    raw = subprocess.run(["adb", "-s", serial, "exec-out", "screencap"],
                         capture_output=True).stdout
    return Shot(raw)


def _demo(argv):
    """用法：screen.py [serial] [x,y=#RRGGBB …] —— 带上期望值就顺便比一比。"""
    serial = argv[1] if len(argv) > 1 else "emulator-5554"
    shot = grab(serial)
    print("帧缓冲 %dx%d fmt=%d" % (shot.w, shot.h, shot.fmt))
    bad = 0
    for spec in argv[2:]:
        m = re.match(r"(\d+),(\d+)=(\S+)", spec)
        if not m:
            print("✘ 认不出 %r（要 x,y=#RRGGBB）" % spec)
            bad += 1
            continue
        x, y, want = int(m.group(1)), int(m.group(2)), parse_hex(m.group(3))
        got = shot.pixel(x, y)[:3]
        ok = all(abs(a - b) <= 8 for a, b in zip(got, want))
        print("%s (%d,%d) 期望 #%02X%02X%02X 实得 #%02X%02X%02X"
              % ("✔" if ok else "✘", x, y, *want, *got))
        bad += 0 if ok else 1
    return 1 if bad else 0


if __name__ == "__main__":
    raise SystemExit(_demo(sys.argv))
