#!/usr/bin/env python3
"""① 光标点到 L3 的 `{` 上 → 看配对高亮；② 点键盘的 ✓（Go）→ 看自动缩进。"""
import os, subprocess, sys, time
REPO = "/Users/shitanyu/Desktop/source/mycoder/my-coder"
sys.path.insert(0, REPO + "/scripts/maui-vml-verify")
from driver import Driver
HERE = REPO + "/.scratch/landscape-check"
d = Driver(serial="emulator-5554")

L3_Y = 594
BRACE_X = 372      # L3 上 `{` 那一格（从截图量的）
GO_X, GO_Y = 978, 2170   # 软键盘右下角的 ✓（ReturnType=Go ⇒ 触发 Completed）


def shot(name):
    open(os.path.join(HERE, name + ".png"), "wb").write(
        subprocess.run(["adb", "-s", "emulator-5554", "exec-out", "screencap", "-p"],
                       capture_output=True).stdout)
    print("  截图", name)


def status():
    for n in d.nodes():
        if n["text"] and "光标 L" in n["text"]:
            return n["text"]
    return "(无状态栏)"


def entry_text():
    for n in d.nodes():
        if n["cls"] == "android.widget.EditText":
            return n["text"]
    return None


print("① 光标点到 L3 的 `{` 上")
d.sh("shell", "input", "tap", BRACE_X, L3_Y); time.sleep(1.8)
print("  ", status(), "输入框:", repr(entry_text()))
shot("E7-bracket-on-brace")

print("② 点键盘 ✓（Go）触发回车断行")
d.sh("shell", "input", "tap", GO_X, GO_Y); time.sleep(2.5)
print("  ", status(), "输入框:", repr(entry_text()))
shot("E8-after-go")
