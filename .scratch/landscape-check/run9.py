#!/usr/bin/env python3
"""① 在 L3（int main(void) {）行尾回车 → 看新行缩进；② 点 L3 的 ( 附近 → 看括号高亮。"""
import os, re, subprocess, sys, time
REPO = "/Users/shitanyu/Desktop/source/mycoder/my-coder"
sys.path.insert(0, REPO + "/scripts/maui-vml-verify")
from driver import Driver
HERE = REPO + "/.scratch/landscape-check"
d = Driver(serial="emulator-5554")

# 代码区几何（从 E4 截图上量出来的：显示坐标 ×1.2 = 原始像素）
LINE3_Y = 594          # int main(void) {
LINE1_Y = 478          # /* ... { 收尾


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


print("① 点 L3 再点行尾回车")
d.sh("shell", "input", "tap", 480, LINE3_Y); time.sleep(1.8)
print("  点后状态:", status(), "输入框:", repr(entry_text()))
d.sh("shell", "input", "keyevent", "123")   # MOVE_END
time.sleep(0.8)
d.sh("shell", "input", "keyevent", "66")    # Enter
time.sleep(2)
print("  回车后状态:", status(), "输入框:", repr(entry_text()))
shot("E5-after-enter")

print("② 点 L1 的 `{` 附近看配对高亮（L1 与 L2/L6 的括号）")
# L1 的 `{` 在「以 { 收尾」里，屏幕 x 约 560（显示 467×1.2）
d.sh("shell", "input", "tap", 566, LINE1_Y); time.sleep(1.8)
print("  状态:", status(), "输入框:", repr(entry_text()))
shot("E6-bracket-match")

print("③ 保存（工具条上的磁盘图标）")
d.sh("shell", "input", "tap", 862, 412); time.sleep(2.5)
print("  状态:", status())
