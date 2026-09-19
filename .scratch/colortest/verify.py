#!/usr/bin/env python3
"""跑 probe5 + calc，采样像素，与期望色逐条比 —— 颜色回归的最小判据。

用法:  python3 verify.py
"""
import os, subprocess, sys, time

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(ROOT, "scripts", "maui-vml-verify"))
from driver import Driver, keycodes_for  # noqa: E402

SERIAL = os.environ.get("SERIAL", "emulator-5554")
DEV_DIR = "/storage/emulated/0/waycoder/workspace/examples/c"
KEY_NUM, KEY_FN, KEY_OP, KEY_EQ = 0x2A3346, 0x9E4630, 0x7A5A28, 0x2E7DD1


def load(path):
    d = open(path, "rb").read()
    w = int.from_bytes(d[0:4], "little")
    h = int.from_bytes(d[4:8], "little")
    fmt = int.from_bytes(d[8:12], "little")
    off = 12 if fmt == 1 else 16
    return d, w, off


def run(name, settle=3.0):
    d = Driver(serial=SERIAL)
    d.disable_ime(); d.enable_external_storage()
    src = os.path.join(HERE, name + ".c")
    d.sh("push", src, DEV_DIR + "/" + name + ".c")
    cmd = "vml run examples/c/%s.c" % name
    for _ in range(4):
        if not d.restart_app():
            continue
        if not d.focus_entry():
            continue
        d.clear_entry()
        d.sh("shell", "input", "keyevent", *keycodes_for(cmd))
        time.sleep(1.2)
        if d.entry_text_stable() != cmd:
            continue
        b = d.run_button()
        if b is not None:
            d.sh("shell", "input", "tap", *d._center(b))
        time.sleep(1.5)
        t0 = time.time()
        while time.time() - t0 < 240:
            ns = d.nodes()
            d.accept_host_dialog(ns)
            if d.window_open(ns) or not d.on_shell_page(ns):
                time.sleep(settle)
                raw = "/tmp/colortest/%s.raw" % name
                subprocess.run(["adb", "-s", SERIAL, "exec-out", "screencap"], check=True,
                               stdout=open(raw, "wb"))
                return load(raw)
            time.sleep(1.5)
    raise SystemExit("✘ %s 跑不起来" % name)


def main():
    fails = []

    # ── probe5：六条同色横带，中间插一次渐变 ──
    data, w, off = run("probe5")
    def px(x, y):
        i = off + (y * w + x) * 4
        return (data[i], data[i+1], data[i+2])
    print("probe5 —— 六条带都必须是 #%06X" % KEY_NUM)
    for k in range(6):
        y = 448 + 285 * k + 142
        c = px(200, y)
        ok = c == ((KEY_NUM >> 16) & 255, (KEY_NUM >> 8) & 255, KEY_NUM & 255)
        print("   band %d  #%02X%02X%02X  %s" % (k, *c, "✔" if ok else "✘"))
        if not ok:
            fails.append("probe5 band %d" % k)

    # ── calc：四档按键色 ──
    data, w, off = run("calc")
    print("calc —— 四档按键色")
    # 键面采样点（原图坐标）：取键心**偏上**避开文字
    samples = [("数字键 7", 167, 1160, KEY_NUM), ("功能键 C", 167, 940, KEY_FN),
               ("运算符 x", 900, 1170, KEY_OP), ("等号", 900, 1900, KEY_EQ)]
    for name, x, y, want in samples:
        c = px(x, y)
        exp = ((want >> 16) & 255, (want >> 8) & 255, want & 255)
        ok = c == exp
        print("   %-8s (%4d,%4d) 期望 #%06X 实测 #%02X%02X%02X  %s"
              % (name, x, y, want, *c, "✔" if ok else "✘"))
        if not ok:
            fails.append("calc %s" % name)

    print()
    if fails:
        print("✘ 失败：", "; ".join(fails)); return 1
    print("✔ 全部通过")
    return 0


if __name__ == "__main__":
    sys.exit(main())
