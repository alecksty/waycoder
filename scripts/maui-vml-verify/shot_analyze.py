#!/usr/bin/env python3
"""截图里**某个颜色**的连通域分析 —— 用来客观比对"同一个程序在两条渲染路径下的输出"。

## 为什么不用肉眼

矢量后端的"画布变换"那条路（把刚体变换交给平台、图元走原生）写错了**不会崩、不会报错**，
只会画得不对（矩阵参数顺序错一位就是转置 ⇒ 镜像/错切）。而它在屏幕上长什么样，
靠着一眼截图判断"看着差不多"是靠不住的 —— 本仓的规矩是**把判据变成可测的数**。

所以这里只做一件事：把某颜色的连通域找出来，输出每个域的**包围盒 + 像素数**。
同一个程序、两种构建，这两组数必须对得上。

## 输入格式

`adb exec-out screencap`（**不带 `-p`**）给的是**裸帧缓冲**：
前 4 字节宽、接着 4 字节高、再 4 字节格式，之后是 `w*h*4` 的 RGBA。

    adb exec-out screencap > /tmp/a.raw
    python3 scripts/maui-vml-verify/shot_analyze.py /tmp/a.raw --color 44AAFF

⚠ 别用 `screencap -p`（PNG）：本机没有 PIL/numpy，裸 RGBA 反而省事（也不依赖解码器）。
"""

import argparse
import struct
import sys
from collections import deque


def read_raw(path):
    with open(path, "rb") as f:
        data = f.read()
    if len(data) < 16:
        raise SystemExit("文件太小：%s" % path)
    w, h, fmt = struct.unpack("<III", data[:12])
    body = data[12:]
    if len(body) < w * h * 4:
        raise SystemExit("像素数据不足：期望 %d，实际 %d（是不是用了 screencap -p？）"
                         % (w * h * 4, len(body)))
    return w, h, fmt, body


def find_regions(w, h, body, rgb, tol, step):
    """返回 [{'box': (x0,y0,x1,y1), 'count': n}]，坐标是**原图像素**。"""
    tr, tg, tb = rgb
    sw, sh = w // step, h // step
    mask = bytearray(sw * sh)
    for sy in range(sh):
        row = (sy * step) * w * 4
        base = sy * sw
        for sx in range(sw):
            o = row + (sx * step) * 4
            if (abs(body[o] - tr) <= tol and abs(body[o + 1] - tg) <= tol
                    and abs(body[o + 2] - tb) <= tol):
                mask[base + sx] = 1

    seen = bytearray(sw * sh)
    regions = []
    for sy in range(sh):
        for sx in range(sw):
            i = sy * sw + sx
            if mask[i] == 0 or seen[i]:
                continue
            q = deque([(sx, sy)])
            seen[i] = 1
            n = 0
            x0 = x1 = sx
            y0 = y1 = sy
            while q:
                cx, cy = q.popleft()
                n += 1
                if cx < x0: x0 = cx
                if cx > x1: x1 = cx
                if cy < y0: y0 = cy
                if cy > y1: y1 = cy
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = cx + dx, cy + dy
                    if 0 <= nx < sw and 0 <= ny < sh:
                        k = ny * sw + nx
                        if mask[k] and not seen[k]:
                            seen[k] = 1
                            q.append((nx, ny))
            # 太小的域是抗锯齿碎点，忽略
            if n * step * step < 400:
                continue
            regions.append({
                "box": (x0 * step, y0 * step, (x1 + 1) * step, (y1 + 1) * step),
                "count": n * step * step,
            })
    regions.sort(key=lambda r: (r["box"][1], r["box"][0]))
    return regions


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("raw", help="裸帧缓冲文件")
    ap.add_argument("--color", default="44AAFF", help="目标颜色 RRGGBB（十六进制）")
    ap.add_argument("--tol", type=int, default=24, help="每通道容差")
    ap.add_argument("--step", type=int, default=2, help="降采样步长（越大越快）")
    ap.add_argument("--quiet", action="store_true")
    args = ap.parse_args()

    try:
        rgb = tuple(int(args.color[i:i + 2], 16) for i in (0, 2, 4))
    except Exception:
        raise SystemExit("--color 要写成 RRGGBB，例如 44AAFF")

    w, h, fmt, body = read_raw(args.raw)
    regions = find_regions(w, h, body, rgb, args.tol, args.step)
    if not args.quiet:
        print("画面 %dx%d fmt=%d ｜ 颜色 #%s ｜ %d 个连通域"
              % (w, h, fmt, args.color.upper(), len(regions)))
    for r in regions:
        x0, y0, x1, y1 = r["box"]
        print("  box=(%4d,%4d)-(%4d,%4d)  %3dx%-3d  像素 %d"
              % (x0, y0, x1, y1, x1 - x0, y1 - y0, r["count"]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
