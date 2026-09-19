#!/usr/bin/env python3
"""从设备抓**原始帧缓冲**并采样像素颜色 —— 颜色校对用。

为什么不用 PNG：`adb exec-out screencap -p` 要解码 PNG，而本机 python 既没有 PIL
也没有 numpy。裸 `screencap` 输出是「12/16 字节头 + RGBA8888」，直接索引即可。

用法：
    python3 sample.py shot.raw W H x1,y1 x2,y2 ...       # 打印每个点的颜色
    python3 sample.py shot.raw W H --scan y x0 x1 step    # 沿一条横向扫描线报色段
"""
import subprocess, sys, os

def grab(serial="emulator-5554", out="/tmp/colortest/shot.raw"):
    subprocess.run(["adb", "-s", serial, "exec-out", "screencap", "-p"], check=True,
                   stdout=open(out, "wb"))
    return out

def load(path):
    d = open(path, "rb").read()
    # screencap -p 给的是 PNG（-p 就是 png）。裸格式要用不带 -p 的。
    if d[:8] == b"\x89PNG\r\n\x1a\n":
        return None, d
    w = int.from_bytes(d[0:4], "little")
    h = int.from_bytes(d[4:8], "little")
    fmt = int.from_bytes(d[8:12], "little")
    off = 12 if fmt == 1 else 16   # 1 = RGBA_8888（带 4 字节 colorspace）
    return (w, h, off), d

def load_raw(serial="emulator-5554", out="/tmp/colortest/shot.raw"):
    """裸帧缓冲（不带 -p）—— 无 PNG 解码依赖。"""
    subprocess.run(["adb", "-s", serial, "exec-out", "screencap"], check=True,
                   stdout=open(out, "wb"))
    return load(out)

def px(d, off, w, x, y):
    i = off + (y * w + x) * 4
    r, g, b, a = d[i], d[i+1], d[i+2], d[i+3]
    return (r, g, b, a)

def hexs(c):
    return "#%02X%02X%02X" % (c[0], c[1], c[2])

def main():
    args = sys.argv[1:]
    serial = os.environ.get("SERIAL", "emulator-5554")
    meta, d = load_raw(serial)
    if meta is None:
        print("✘ 拿到的是 PNG，screencap 没走裸格式"); return 1
    w, h, off = meta
    print("帧缓冲 %dx%d，头 %d 字节，共 %d 字节" % (w, h, off, len(d)))

    if args and args[0] == "--scan":
        y = int(args[1]); x0 = int(args[2]); x1 = int(args[3]); step = int(args[4])
        prev = None
        for x in range(x0, x1, step):
            c = px(d, off, w, x, y)
            if c != prev:
                print("  x=%-5d %s" % (x, hexs(c)))
                prev = c
        return 0

    for spec in args:
        x, y = (int(v) for v in spec.split(","))
        c = px(d, off, w, x, y)
        print("  (%4d,%4d) %s  a=%d" % (x, y, hexs(c), c[3]))
    return 0

if __name__ == "__main__":
    sys.exit(main())
