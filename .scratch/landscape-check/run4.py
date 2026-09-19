#!/usr/bin/env python3
"""跑 jsontest.c（不开窗，纯输出）并读回 stdout。"""
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
d.start_app(); time.sleep(18)
print("命令行页:", d.goto_shell_tab(tries=12))

before = d.output_label()
for attempt in range(4):
    sender.send("vml run jsontest.c ")
    for _ in range(60):
        time.sleep(4)
        ns = d.nodes()
        if d.accept_host_dialog(ns):
            continue
        now = d.output_label()
        if now != before and "⏳" not in now[-260:]:
            print("--- 输出 ---")
            print(now[len(before):])
            sys.exit(0)
    print("  重试…")
print("✘ 没拿到输出")
print(d.output_label()[-400:])
