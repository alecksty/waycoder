#!/usr/bin/env python3
"""在**当前方向**下重开一个绘图窗口，直到真的开出来（送命令那条链偶发丢字符，重试到底）。"""
import sys, time
REPO = "/Users/shitanyu/Desktop/source/mycoder/my-coder"
sys.path.insert(0, REPO + "/scripts/maui-vml-verify")
from driver import Driver
d = Driver(serial="emulator-5554")
cmd = sys.argv[1] if len(sys.argv) > 1 else "vml run examples/lua/life.lua"

for i in range(8):
    before = d.output_label()
    d.submit(cmd)
    for _ in range(90):
        time.sleep(4)
        ns = d.nodes()
        if d.accept_host_dialog(ns):
            continue
        if d.window_open(ns):
            print("✔ 第 %d 次尝试开出了窗口" % (i + 1))
            sys.exit(0)
        now = d.output_label()
        if now != before and "⏳" not in now[-200:] and ("⚠" in now[len(before):] or "~>" in now[len(before):]):
            print("  第%d次：命令没跑起来 → %r" % (i + 1, now[len(before):][:120]))
            break
    else:
        print("  第%d次：等超时" % (i + 1))
print("✘ 八次都没开出窗口")
sys.exit(1)
