#!/usr/bin/env python3
"""点 L3 → MOVE_END（光标正好落在 `{` 后面）→ 截图看配对高亮 → 点 Go 看自动缩进。"""
import os, subprocess, sys, time
REPO = "/Users/shitanyu/Desktop/source/mycoder/my-coder"
sys.path.insert(0, REPO + "/scripts/maui-vml-verify")
from driver import Driver
HERE = REPO + "/.scratch/landscape-check"
d = Driver(serial="emulator-5554")
L3_Y = 594
GO_X, GO_Y = 978, 2170


def shot(name):
    open(os.path.join(HERE, name + ".png"), "wb").write(
        subprocess.run(["adb", "-s", "emulator-5554", "exec-out", "screencap", "-p"],
                       capture_output=True).stdout)
    print("  截图", name)


def status():
    for n in d.nodes():
        if n["text"] and "光标 L" in n["text"]:
            return n["text"]
    return "(无)"


def entry_text():
    for n in d.nodes():
        if n["cls"] == "android.widget.EditText":
            return n["text"]
    return None


d.sh("shell", "input", "tap", 480, L3_Y); time.sleep(2)
print("点 L3 →", status(), repr(entry_text()))
d.sh("shell", "input", "keyevent", "123"); time.sleep(1.2)   # MOVE_END：光标落到 `{` 之后
print("MOVE_END →", status(), repr(entry_text()))
shot("E9-caret-after-brace")
print("点 Go →")
d.sh("shell", "input", "tap", GO_X, GO_Y); time.sleep(3)
print("  ", status(), repr(entry_text()))
shot("E10-after-go")
