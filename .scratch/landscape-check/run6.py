#!/usr/bin/env python3
"""接着 run5：打开文件 → 看 ☰ 菜单有没有「替换」→ 试自动缩进 / 括号高亮。"""
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


def nodes():
    return d.nodes()


def tap(text, wait=1.5):
    for n in nodes():
        if n["text"] == text:
            d.sh("shell", "input", "tap", *d._center(n)); time.sleep(wait); return True
    return False


def entry():
    for n in nodes():
        if n["cls"] == "android.widget.EditText":
            return n
    return None


print("① 打开文件")
print("  点「打开」:", tap("打开", 4))
print("  编辑器里的文字:", [n["text"] for n in nodes() if n["text"]][:8])
shot("E1-editor-open")

print("② ☰ 菜单里有没有「替换」")
for n in nodes():
    if n["cls"] == "android.widget.Button" and n["text"] in ("☰", "☰") :
        d.sh("shell", "input", "tap", *d._center(n)); break
else:
    # 菜单按钮可能是 ImageButton / 无文字 —— 按坐标点右上角
    d.sh("shell", "input", "tap", 1000, 200)
time.sleep(2)
menu = [n["text"] for n in nodes() if n["text"]]
print("  菜单项:", menu[:16])
has_replace = any("替换" in (t or "") for t in menu)
print("  含「替换…」:", has_replace)
shot("E2-menu")
# 关掉菜单
d.sh("shell", "input", "keyevent", "4"); time.sleep(1.5)

print("③ 自动缩进：点一行以 { 收尾的代码，光标移到行尾回车")
e = entry()
print("  当前输入框:", None if e is None else repr(e["text"]))
if e is not None:
    # 光标移到行尾再回车
    d.sh("shell", "input", "keyevent", "123")   # KEYCODE_MOVE_END
    time.sleep(0.8)
    d.sh("shell", "input", "keyevent", "66")    # Enter
    time.sleep(1.5)
    print("  回车后输入框:", repr(entry()["text"] if entry() else None))
    shot("E3-after-enter")
