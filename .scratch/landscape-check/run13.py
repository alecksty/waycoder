#!/usr/bin/env python3
"""`.vmb` 端到端：sysinfo.c → .vml → .vmb，两条路各跑一次，比对打印的 JSON。"""
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


def run(cmd, wait_rounds=80, settle=4):
    before = d.output_label()
    for _ in range(3):
        sender.send(cmd + " ")
        for _ in range(wait_rounds):
            time.sleep(settle)
            ns = d.nodes()
            if d.accept_host_dialog(ns):
                continue
            now = d.output_label()
            if now != before and "⏳" not in now[-300:]:
                return now[len(before):].strip()
        print("  重试…")
    return "(没拿到输出)"


for cmd in ("vml build examples/c/sysinfo.c",
            "vml run examples/c/sysinfo.c",
            "vml build examples/c/sysinfo.vml",
            "vml run examples/c/sysinfo.vmb"):
    print("\n=== %s ===" % cmd)
    out = run(cmd)
    print(out[:1100])
