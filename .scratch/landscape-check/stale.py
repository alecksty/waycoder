#!/usr/bin/env python3
"""复现并验证「跨方向陈旧」：竖屏跑一局并关掉 → 转到横屏 → 再开一局。"""
import sys, time
REPO = "/Users/shitanyu/Desktop/source/mycoder/my-coder"
sys.path.insert(0, REPO + "/.scratch/landscape-check")
sys.path.insert(0, REPO + "/scripts/maui-vml-verify")
import sender
from driver import Driver

d = Driver(serial="emulator-5554")


def rot(n, w=5.0):
    d.sh("shell", "settings", "put", "system", "accelerometer_rotation", "0")
    d.sh("shell", "settings", "put", "system", "user_rotation", str(n))
    time.sleep(w)


def run_and_close(label):
    before = d.output_label()
    d.sh("logcat", "-c")
    for _ in range(4):
        sender.send("vml run otest.c ")
        for _ in range(60):
            time.sleep(4)
            ns = d.nodes()
            if d.accept_host_dialog(ns):
                continue
            if d.window_open(ns):
                break
        else:
            print("  %s: 没开窗，重试" % label); continue
        time.sleep(4)
        out = d.sh("logcat", "-d", "-s", "WC-DRAW")
        lines = [x for x in out.splitlines() if "host=" in x]
        if lines:
            print("  %s: %s" % (label, lines[-1].split("WC-DRAW : ", 1)[-1]))
        d.sh("shell", "input", "keyevent", "4"); time.sleep(3)
        for n in d.nodes():
            if n["cls"] == "android.widget.Button" and n["text"] in ("强制停止", "停止"):
                d.sh("shell", "input", "tap", *d._center(n)); time.sleep(2); break
        time.sleep(2)
        print("  %s 程序输出: %r" % (label, d.output_label()[len(before):].replace("\n", " | ")[:220]))
        return
    print("  %s: 失败" % label)


d.sh("shell", "am", "force-stop", d.pkg); time.sleep(2)
rot(0)
d.start_app(); time.sleep(18)
print("命令行页:", d.goto_shell_tab(tries=12))

print("① 竖屏跑一局（让 MeasuredViewport 记成竖屏）")
run_and_close("竖屏")
print("② 在**命令行页**上转到横屏")
rot(1)
print("③ 横屏再开一局 —— 期望 scene 是横屏形状（约 380x301），不是 411x525")
run_and_close("横屏")
