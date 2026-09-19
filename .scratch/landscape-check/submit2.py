#!/usr/bin/env python3
"""自己送命令：**读回输入框确认一字不差**再按回车（`input text` 会静默丢字符）。"""
import sys, time
REPO = "/Users/shitanyu/Desktop/source/mycoder/my-coder"
sys.path.insert(0, REPO + "/scripts/maui-vml-verify")
from driver import Driver
d = Driver(serial="emulator-5554")

def entry():
    for n in d.nodes():
        if n["cls"] == "android.widget.EditText":
            return n
    return None

def send(cmd, tries=6):
    for i in range(tries):
        e = entry()
        if e is None:
            time.sleep(2); continue
        d.sh("shell", "input", "tap", *d._center(e))
        time.sleep(0.6)
        # 清空
        for _ in range(45):
            d.sh("shell", "input", "keyevent", "67")
        d.sh("shell", "input", "text", cmd)
        time.sleep(0.8)
        got = (entry() or {}).get("text", "")
        if got == cmd:
            d.sh("shell", "input", "keyevent", "66")
            return True, got
        print("  第%d次送的与预期不符：%r" % (i + 1, got))
        time.sleep(0.5)
    return False, got

if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "vml run examples/lua/life.lua"
    ok, got = send(cmd)
    print("送出:", ok, repr(got))
