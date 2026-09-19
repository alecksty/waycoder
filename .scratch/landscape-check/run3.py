#!/usr/bin/env python3
"""验五子棋的新声明：只竖屏 + 不要手柄区。"""
import os, subprocess, sys, time
REPO = "/Users/shitanyu/Desktop/source/mycoder/my-coder"
sys.path.insert(0, REPO + "/.scratch/landscape-check")
sys.path.insert(0, REPO + "/scripts/maui-vml-verify")
import sender
from driver import Driver
HERE = REPO + "/.scratch/landscape-check"
d = Driver(serial="emulator-5554")


def rot(n, w=7.0):
    d.sh("shell", "settings", "put", "system", "accelerometer_rotation", "0")
    d.sh("shell", "settings", "put", "system", "user_rotation", str(n))
    time.sleep(w)


def shot(name):
    open(os.path.join(HERE, name + ".png"), "wb").write(
        subprocess.run(["adb", "-s", "emulator-5554", "exec-out", "screencap", "-p"],
                       capture_output=True).stdout)
    print("  截图", name)


def scene_line():
    out = d.sh("logcat", "-d", "-s", "WC-DRAW")
    ls = [x for x in out.splitlines() if "host=" in x]
    return ls[-1].split("WC-DRAW : ", 1)[-1] if ls else "(无)"


def window_up():
    """⚠ `driver.window_open()` 的判据是**手柄按钮**（SELECT/START/收起手柄）——
    声明了 NO_GAMEPAD 的程序它永远认不出来（本脚本第一次就是这么卡住的）。
    这里补一条：**命令行页不见了 = 绘图页已经上来了**。"""
    ns = d.nodes()
    if d.window_open(ns):
        return True
    return (not d.on_shell_page(ns)) and any(n["text"] == "五子棋" for n in ns)


def pad_texts():
    ts = sorted(n["text"] for n in d.nodes()
                if n["cls"] == "android.widget.Button" and n["text"])
    return ts


d.sh("shell", "am", "force-stop", d.pkg); time.sleep(2)
rot(0)
d.start_app(); time.sleep(18)
print("命令行页:", d.goto_shell_tab(tries=12))
d.sh("logcat", "-c")
for _ in range(4):
    sender.send("vml run gomoku.c ")
    ok = False
    for _ in range(70):
        time.sleep(4)
        ns = d.nodes()
        if d.accept_host_dialog(ns):
            continue
        if window_up():
            ok = True
            break
    if ok:
        break
    print("  重试…")
time.sleep(5)
print("竖屏:", scene_line())
print("  按钮:", pad_texts()[:10])
shot("G1-portrait")
rot(1)
print("转到横屏后:", scene_line())
print("  按钮:", pad_texts()[:10])
shot("G2-after-rotate")
print("  device user_rotation 现在 =", d.sh("shell", "settings", "get", "system", "user_rotation").strip())
