#!/usr/bin/env python3
"""重进编辑器并定位代码区（不按 BACK）。"""
import os, subprocess, sys, time
REPO = "/Users/shitanyu/Desktop/source/mycoder/my-coder"
sys.path.insert(0, REPO + "/scripts/maui-vml-verify")
from driver import Driver
HERE = REPO + "/.scratch/landscape-check"
d = Driver(serial="emulator-5554")


def shot(name):
    open(os.path.join(HERE, name + ".png"), "wb").write(
        subprocess.run(["adb", "-s", "emulator-5554", "exec-out", "screencap", "-p"],
                       capture_output=True).stdout)
    print("  截图", name)


def tap(text, wait=1.8):
    for n in d.nodes():
        if n["text"] == text:
            d.sh("shell", "input", "tap", *d._center(n)); time.sleep(wait); return True
    return False


d.sh("shell", "am", "force-stop", d.pkg); time.sleep(2)
d.start_app(); time.sleep(15)
print("命令行页:", d.goto_shell_tab(tries=12))
for n in d.nodes():
    if n["desc"] == "文件":
        d.sh("shell", "input", "tap", *d._center(n)); break
time.sleep(2.5)
print("点文件:", tap("indentdemo.c", 2))
print("点打开:", tap("打开", 4))
print("☰:", tap("☰", 2))
print("切编辑:", tap("✎  切换 编辑/只读", 2))
print("关菜单（点取消，不按 BACK）:", tap("取消", 1.5))
print("状态:", [n["text"] for n in d.nodes() if n["text"] and ("编辑" in n["text"] or "只读" in n["text"] or "行" in n["text"])][:3])
shot("E4-editor-editmode")
