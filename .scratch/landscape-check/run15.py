#!/usr/bin/env python3
"""真机：关于页 → 分类 → 主题列表 → 正文；以及文件页的 .vml/.vmb 图标。"""
import os, subprocess, sys, time
REPO = "/Users/shitanyu/Desktop/source/mycoder/my-coder"
sys.path.insert(0, REPO + "/scripts/maui-vml-verify")
from driver import Driver
HERE = REPO + "/.scratch/landscape-check"
SER = "cd53cb14"
d = Driver(serial=SER)


def shot(name):
    open(os.path.join(HERE, name + ".png"), "wb").write(
        subprocess.run(["adb", "-s", SER, "exec-out", "screencap", "-p"],
                       capture_output=True).stdout)
    print("  截图", name)


def tap(substr, wait=2.5):
    for n in d.nodes():
        if substr in (n["text"] or ""):
            d.sh("shell", "input", "tap", *d._center(n)); time.sleep(wait); return True
    return False


def texts():
    return [n["text"] for n in d.nodes() if n["text"]]


d.sh("shell", "am", "force-stop", d.pkg); time.sleep(2)
d.start_app(); time.sleep(18)

print("① 进关于页")
for n in d.nodes():
    if n["desc"] == "首页":
        d.sh("shell", "input", "tap", *d._center(n)); break
time.sleep(2.5)
print("  点「关于」:", tap("关于", 3))
print("  关于页文字:", texts()[:14])
shot("H1-about")

print("② 点「VML 编译器」分类")
print("  点了:", tap("VML 编译器", 3))
print("  二级列表:", texts()[:12])
shot("H2-vml-topics")

print("③ 点「UI 开发」进正文")
print("  点了:", tap("UI 开发", 3.5))
print("  正文页文字:", texts()[:6])
shot("H3-ui-doc")

print("④ 文件页看图标（.vml vs .vmb）")
for n in d.nodes():
    if n["desc"] == "文件":
        d.sh("shell", "input", "tap", *d._center(n)); break
time.sleep(3)
print("  文件页:", texts()[:16])
shot("H4-files-icons")
