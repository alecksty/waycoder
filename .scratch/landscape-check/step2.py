#!/usr/bin/env python3
"""横屏 + 收起手柄；再把窗口关掉、转横屏、用一门**编译快**的语言重开一次
（用户报的重现路径：进游戏 → 横屏 → 转回 → 退出 → 再进）。"""
import os, subprocess, sys, time
REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
sys.path.insert(0, os.path.join(REPO, "scripts", "maui-vml-verify"))
from driver import Driver
HERE = os.path.dirname(os.path.abspath(__file__))
SERIAL = "emulator-5554"
d = Driver(serial=SERIAL)

def rot(n, wait=4.0):
    d.sh("shell", "settings", "put", "system", "accelerometer_rotation", "0")
    d.sh("shell", "settings", "put", "system", "user_rotation", str(n))
    time.sleep(wait)

def probe(label):
    time.sleep(3)
    out = d.sh("logcat", "-d", "-s", "WC-DRAW")
    lines = [l for l in out.splitlines() if "host=" in l]
    print("\n===== %s =====" % label)
    for l in lines[-1:]:
        print("  " + (l.split("WC-DRAW : ", 1)[-1] if "WC-DRAW : " in l else l))
    if not lines:
        print("  （无 WC-DRAW 日志）")
    p = os.path.join(HERE, "shot-%s.png" % label)
    with open(p, "wb") as f:
        subprocess.run(["adb", "-s", SERIAL, "exec-out", "screencap", "-p"], stdout=f, check=True)
    print("  截图 %s" % os.path.basename(p))

def state():
    ns = d.nodes()
    return dict(window=d.window_open(ns),
                btns=[n["text"] for n in ns if n["cls"] == "android.widget.Button"],
                shell=d.on_shell_page(ns))

print("当前:", state())

# ① 横屏下收起手柄
for n in d.nodes():
    if n["text"] == "▲ 收起手柄":
        d.sh("shell", "input", "tap", *d._center(n)); break
probe("4-landscape-pads-hidden")

# ② 展开回来
for n in d.nodes():
    if n["text"] == "▼ 展开手柄":
        d.sh("shell", "input", "tap", *d._center(n)); break
probe("5-landscape-pads-back")
