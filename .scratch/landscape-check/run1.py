#!/usr/bin/env python3
"""跑 spacetest.c：竖屏开窗 → 转横屏 → 转回竖屏，每步截图。"""
import os, subprocess, sys, time
REPO = "/Users/shitanyu/Desktop/source/mycoder/my-coder"
sys.path.insert(0, REPO + "/.scratch/landscape-check")
sys.path.insert(0, REPO + "/scripts/maui-vml-verify")
import sender
from driver import Driver
HERE = REPO + "/.scratch/landscape-check"
d = Driver(serial="emulator-5554")


def rot(n, w=6.0):
    d.sh("shell", "settings", "put", "system", "accelerometer_rotation", "0")
    d.sh("shell", "settings", "put", "system", "user_rotation", str(n))
    time.sleep(w)


def shot(name):
    p = os.path.join(HERE, name + ".png")
    open(p, "wb").write(subprocess.run(["adb", "-s", "emulator-5554", "exec-out", "screencap", "-p"],
                                       capture_output=True).stdout)
    print("  截图", name)
    out = d.sh("logcat", "-d", "-s", "WC-DRAW")
    for l in [x for x in out.splitlines() if "host=" in x][-1:]:
        print("  " + l.split("WC-DRAW : ", 1)[-1])


def run(cmd):
    before = d.output_label()
    for _ in range(4):
        sender.send(cmd + " ")
        for _ in range(60):
            time.sleep(4)
            ns = d.nodes()
            if d.accept_host_dialog(ns):
                continue
            if d.window_open(ns):
                time.sleep(5)
                return before
        print("  重试…")
    raise SystemExit("没开出窗口")


d.sh("shell", "am", "force-stop", d.pkg); time.sleep(2)
rot(0)
d.start_app(); time.sleep(18)
print("命令行页:", d.goto_shell_tab(tries=12))
before = run("vml run spacetest.c")
shot("S1-portrait")
rot(1); shot("S2-landscape")
rot(0); shot("S3-portrait-back")
d.sh("shell", "input", "keyevent", "4"); time.sleep(3)
for n in d.nodes():
    if n["cls"] == "android.widget.Button" and n["text"] in ("强制停止", "停止"):
        d.sh("shell", "input", "tap", *d._center(n)); time.sleep(2); break
time.sleep(2)
print("--- 程序输出 ---")
print(d.output_label()[len(before):])
