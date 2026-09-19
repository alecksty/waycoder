#!/usr/bin/env python3
"""切到编辑态 → 找到「以 { 收尾」那一行 → 回车 → 看新行缩进。"""
import os, re, subprocess, sys, time
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


def tap(text, wait=1.6):
    for n in d.nodes():
        if n["text"] == text:
            d.sh("shell", "input", "tap", *d._center(n)); time.sleep(wait); return True
    return False


def entry_text():
    for n in d.nodes():
        if n["cls"] == "android.widget.EditText":
            return n["text"]
    return None


def caret_line():
    for n in d.nodes():
        m = re.search(r"光标 L(\d+)", n["text"] or "")
        if m:
            return int(m.group(1))
    return -1


print("① 切到编辑态")
for n in d.nodes():
    if n["text"] in ("☰", "☰ "):
        d.sh("shell", "input", "tap", *d._center(n)); break
time.sleep(2)
print("  切编辑/只读:", tap("✎  切换 编辑/只读", 2))
d.sh("shell", "input", "keyevent", "4"); time.sleep(1.5)   # 收菜单
print("  状态栏:", [n["text"] for n in d.nodes() if "只读" in (n["text"] or "") or "编辑" in (n["text"] or "")][:2])

print("② 找一行以 { 收尾的（逐 y 试，读状态栏的光标行）")
target = None
for y in (700, 760, 820, 880, 940, 1000, 1060, 1120):
    d.sh("shell", "input", "tap", 500, y); time.sleep(1.8)
    ln = caret_line()
    txt = entry_text()
    print("   y=%d → 光标 L%d, 输入框=%r" % (y, ln, txt))
    if txt is not None and txt.rstrip().endswith("{"):
        target = (y, ln, txt)
        break
    # 点进去就退出编辑态，继续试
    d.sh("shell", "input", "keyevent", "4"); time.sleep(0.6)

if target is None:
    print("✘ 没找到以 { 收尾的行"); shot("E3-notfound")
    sys.exit(0)

y, ln, txt = target
print("③ 在 L%d（%r）行尾回车" % (ln, txt))
d.sh("shell", "input", "tap", 500, y); time.sleep(1.8)
d.sh("shell", "input", "keyevent", "123")   # MOVE_END
time.sleep(0.8)
d.sh("shell", "input", "keyevent", "66")    # Enter
time.sleep(2)
print("  回车后：光标 L%d，输入框=%r" % (caret_line(), entry_text()))
shot("E3-after-enter")
