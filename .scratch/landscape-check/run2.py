#!/usr/bin/env python3
"""三份对照：老接口 / 声明可旋转 / 固定方向+无手柄。"""
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


def scene_line():
    out = d.sh("logcat", "-d", "-s", "WC-DRAW")
    ls = [x for x in out.splitlines() if "host=" in x]
    return ls[-1].split("WC-DRAW : ", 1)[-1] if ls else "(无)"


def shot(name):
    open(os.path.join(HERE, name + ".png"), "wb").write(
        subprocess.run(["adb", "-s", "emulator-5554", "exec-out", "screencap", "-p"],
                       capture_output=True).stdout)


def nodes_text():
    return sorted(n["text"] for n in d.nodes() if n["text"])


def open_window(cmd):
    d.sh("logcat", "-c")
    for _ in range(4):
        sender.send(cmd + " ")
        for _ in range(60):
            time.sleep(4)
            ns = d.nodes()
            if d.accept_host_dialog(ns):
                continue
            if d.window_open(ns):
                time.sleep(5)
                return
        print("  重试…")
    raise SystemExit("没开出窗口: " + cmd)


def close_window():
    d.sh("shell", "input", "keyevent", "4"); time.sleep(3)
    for n in d.nodes():
        if n["cls"] == "android.widget.Button" and n["text"] in ("强制停止", "停止"):
            d.sh("shell", "input", "tap", *d._center(n)); time.sleep(2); break
    time.sleep(2)


d.sh("shell", "am", "force-stop", d.pkg); time.sleep(2)
rot(0)
d.start_app(); time.sleep(18)
print("命令行页:", d.goto_shell_tab(tries=12))

for label, cmd, do_rot in (("① 老接口 legacytest", "vml run legacytest.c", True),
                           ("② 声明可旋转 spacetest", "vml run spacetest.c", True),
                           ("③ 固定+无手柄 flagstest", "vml run flagstest.c", True)):
    print("\n===== %s =====" % label)
    open_window(cmd)
    print("  竖屏:", scene_line())
    texts = nodes_text()
    print("  有手柄按钮:", any(t in texts for t in ("SELECT", "START", "▲ 收起手柄")))
    shot(label[:2].strip() + "-portrait")
    rot(1)
    print("  转横屏后:", scene_line())
    shot(label[:2].strip() + "-rotated")
    rot(0)
    close_window()
