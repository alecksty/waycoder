#!/usr/bin/env python3
"""窗口已经开着的前提下，只做转屏 + 取证。"""
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
    out = d.sh("logcat", "-d", "-s", "WC-DRAW")
    lines = [l for l in out.splitlines() if "host=" in l]
    print("\n===== %s =====" % label)
    for l in lines[-2:]:
        print("  " + l.split("]: ", 1)[-1] if "]: " in l else "  " + l)
    if not lines:
        print("  （无 WC-DRAW 日志）")
    p = os.path.join(HERE, "shot-%s.png" % label)
    with open(p, "wb") as f:
        subprocess.run(["adb", "-s", SERIAL, "exec-out", "screencap", "-p"], stdout=f, check=True)
    print("  截图 %s" % os.path.basename(p))

d.sh("logcat", "-c")
time.sleep(1)
probe("0-portrait")
rot(1); probe("1-landscape")
rot(0); probe("2-portrait-back")
rot(1);
probe("3-landscape-again")
