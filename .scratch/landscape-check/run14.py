#!/usr/bin/env python3
"""`.vmb` 端到端（文件页入口）：
   sysinfo.c --编译--> sysinfo.vml --编译--> sysinfo.vmb
   然后分别「VML 运行」.vml 与 .vmb，比对打印的 JSON。"""
import sys, time
REPO = "/Users/shitanyu/Desktop/source/mycoder/my-coder"
sys.path.insert(0, REPO + "/scripts/maui-vml-verify")
from driver import Driver

d = Driver(serial="emulator-5554")
d.sh("shell", "settings", "put", "system", "accelerometer_rotation", "0")
d.sh("shell", "settings", "put", "system", "user_rotation", "0")
time.sleep(3)
d.sh("shell", "am", "force-stop", d.pkg); time.sleep(2)
d.start_app(); time.sleep(22)
print("命令行页:", d.goto_shell_tab(tries=12))


def tap(text, wait=2.0, exact=True):
    for n in d.nodes():
        t = n["text"] or ""
        if (t == text) if exact else (text in t):
            d.sh("shell", "input", "tap", *d._center(n)); time.sleep(wait); return True
    return False


def go_files():
    for n in d.nodes():
        if n["desc"] == "文件":
            d.sh("shell", "input", "tap", *d._center(n)); time.sleep(2.5); return True
    return False


def output_now():
    return d.sh("shell", "cat", "/sdcard/vmlverify.xml") or ""


def wait_idle_text(marker_before, timeout=200):
    """等命令行页的输出区出现新内容。"""
    for _ in range(timeout // 4):
        time.sleep(4)
        ns = d.nodes()
        if d.accept_host_dialog(ns):
            continue
        o = d.output_label()
        if o != marker_before and "⏳" not in o[-300:]:
            return o
    return None


def build(file_name, menu="VML 编译", timeout=260):
    print("  发 '%s' → %s" % (file_name, menu))
    before = d.output_label()
    if not go_files():
        print("    ✘ 到不了文件页"); return None
    tap(file_name)
    if not tap(menu, 3):
        print("    ✘ 菜单里没有 %s" % menu); return None
    get = wait_idle_text(before, timeout)
    print("    结果:", (get or "(超时)")[-400:].replace("\n", " | ")[:300])
    return get


print("① .c → .vml")
build("sysinfo.c")
print("② .vml → .vmb")
build("sysinfo.vml")
print("③ 跑 .vml")
a = build("sysinfo.vml", menu="VML 运行", timeout=200)
print("④ 跑 .vmb")
b = build("sysinfo.vmb", menu="VML 运行", timeout=200)


def json_of(t):
    i = (t or "").find('{"ok"')
    return t[i:i + 600] if i >= 0 else None


ja, jb = json_of(a), json_of(b)
print("\n=== 对比 ===")
print(".vml :", (ja or "(没拿到)")[:200])
print(".vmb :", (jb or "(没拿到)")[:200])
print("一致:", ja is not None and ja == jb)
