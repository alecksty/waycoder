#!/usr/bin/env python3
"""验编辑器这批：☰ 菜单里有「替换」/ 回车自动缩进 / 括号配对高亮 / 自动配对。
驱动方式：文件页打开一个文件 → 编辑器里操作 → 截图 + 读输入框文本。"""
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


def texts():
    return [n["text"] for n in d.nodes() if n["text"]]


def find(text, cls=None):
    for n in d.nodes():
        if n["text"] == text and (cls is None or n["cls"] == cls):
            return n
    return None


def tap_node(n, wait=1.5):
    d.sh("shell", "input", "tap", *d._center(n)); time.sleep(wait)


def tap(text, cls=None, wait=1.5):
    n = find(text, cls)
    if n is None:
        print("  ✘ 找不到可点的：%r" % text); return False
    tap_node(n, wait); return True


def entry_text():
    for n in d.nodes():
        if n["cls"] == "android.widget.EditText":
            return n["text"]
    return None


d.sh("shell", "settings", "put", "system", "accelerometer_rotation", "0")
d.sh("shell", "settings", "put", "system", "user_rotation", "0")
time.sleep(3)
d.sh("shell", "am", "force-stop", d.pkg); time.sleep(2)
d.start_app(); time.sleep(18)
print("命令行页:", d.goto_shell_tab(tries=12))

# ⚠ 不能按 BACK 导航 —— 会把 App 退到桌面（文档里记过这条）。直接点「文件」Tab 进文件页。
d.sh("shell", "am", "start", "-n", "com.tanso.waycoder/crc64a928b3a971e69625.MainActivity")
time.sleep(4)
for n in d.nodes():
    if n["desc"] == "文件":
        tap_node(n, 2.5); break
print("文件页文字:", [t for t in texts() if "indent" in t or "examples" in t or t.endswith(".c")][:8])
tap("indentdemo.c", wait=4)
print("编辑器标题/文字:", [t for t in texts() if t][:10])
shot("E1-editor-open")
