#!/usr/bin/env python3
"""用户报的重现路径：**进游戏 → 横屏 → 转回竖屏 → 退出 → 再进**。
拆成两段各自验：① 横屏里退出、竖屏里再进；② 竖屏里退出、横屏里再进。"""
import os, subprocess, sys, time
REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
sys.path.insert(0, os.path.join(REPO, "scripts", "maui-vml-verify"))
from driver import Driver
HERE = os.path.dirname(os.path.abspath(__file__))
SERIAL = "emulator-5554"
GAME = sys.argv[1] if len(sys.argv) > 1 else "vml run examples/lua/life.lua"
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
    p = os.path.join(HERE, "shot-%s.png" % label)
    with open(p, "wb") as f:
        subprocess.run(["adb", "-s", SERIAL, "exec-out", "screencap", "-p"], stdout=f, check=True)
    print("  截图 %s" % os.path.basename(p))

def tap(text, tries=4):
    for _ in range(tries):
        for n in d.nodes():
            if n["text"] == text:
                d.sh("shell", "input", "tap", *d._center(n)); return True
        time.sleep(2)
    return False

def close_window():
    d.sh("shell", "input", "keyevent", "4"); time.sleep(2.5)
    if tap("强制停止"):
        time.sleep(2)
    for _ in range(6):
        if d.on_shell_page(): return True
        d.sh("shell", "input", "keyevent", "4"); time.sleep(2)
    return False

def open_game():
    """跑命令并等绘图窗口（对话框也顺手点掉）。"""
    d.sh("logcat", "-c")
    d.submit(GAME)
    for _ in range(80):
        time.sleep(4)
        ns = d.nodes()
        if d.accept_host_dialog(ns):
            continue
        if d.window_open(ns):
            return True
        if d.on_shell_page(ns) and d.is_idle(ns):
            print("  ⚠ 命令已结束但没开窗"); return False
    return False

# ① 横屏里退出
rot(1)
print("关窗（横屏）:", close_window())
rot(0)
print("① 竖屏里再进 ...")
print("  开窗:", open_game())
probe("6-reenter-from-landscape-exit")

# ② 竖屏里退出 → 横屏里再进
print("关窗（竖屏）:", close_window())
rot(1)
print("② 横屏里再进 ...")
print("  开窗:", open_game())
probe("7-reenter-from-portrait-exit")
