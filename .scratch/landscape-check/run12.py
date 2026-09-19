#!/usr/bin/env python3
"""设备上跑 sysinfo 自检（C / Python），读回打印的 JSON。"""
import sys, time
REPO = "/Users/shitanyu/Desktop/source/mycoder/my-coder"
sys.path.insert(0, REPO + "/.scratch/landscape-check")
sys.path.insert(0, REPO + "/scripts/maui-vml-verify")
import sender
from driver import Driver

d = Driver(serial="emulator-5554")
d.sh("shell", "settings", "put", "system", "accelerometer_rotation", "0")
d.sh("shell", "settings", "put", "system", "user_rotation", "0")
time.sleep(3)
d.sh("shell", "am", "force-stop", d.pkg); time.sleep(2)
d.start_app(); time.sleep(22)
print("命令行页:", d.goto_shell_tab(tries=12))

for cmd in ("vml run examples/c/sysinfo.c", "vml run examples/python/sysinfo.py"):
    print("\n=== %s ===" % cmd)
    before = d.output_label()
    sent = False
    for _ in range(4):
        sender.send(cmd + " ")
        for _ in range(70):
            time.sleep(4)
            ns = d.nodes()
            if d.accept_host_dialog(ns):
                continue
            now = d.output_label()
            if now != before and "⏳" not in now[-300:]:
                print(now[len(before):].strip()[:900])
                sent = True
                break
        if sent:
            break
        print("  重试…")
    if not sent:
        print("  ✘ 没拿到输出")
