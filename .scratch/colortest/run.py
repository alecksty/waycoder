#!/usr/bin/env python3
"""把 .scratch/colortest/<名>.c 推到设备上跑起来，等绘图窗口出现，抓原始帧缓冲。

用法:  python3 run.py probe2 [观察秒数]

⚠ 判据不能只看手柄按钮：声明了 `VML_WIN_NO_GAMEPAD` 的程序**根本没有那排按钮**
（`driver.window_open` 的判据，见 CHANGELOG ㉚⑥），会一直认为"窗口没开"。
这里补一条兜底：**命令行页不见了**也算窗口开了。

⚠ 送命令**不依赖"输入框是否被清空"当判据** —— 那个判据要读 uiautomator，而 dump
会返回上一张树，偶尔读到旧图就误判成"没送进去"。这里改成：注入了就点运行，
**成没成看窗口开没开**（程序的真实效果），不成再重来一轮。
"""
import os, subprocess, sys, time

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(ROOT, "scripts", "maui-vml-verify"))
from driver import Driver, keycodes_for  # noqa: E402

SERIAL = os.environ.get("SERIAL", "emulator-5554")
DEV_DIR = "/storage/emulated/0/waycoder/workspace/examples/c"


def opened(d, ns=None):
    ns = ns if ns is not None else d.nodes()
    return d.window_open(ns) or not d.on_shell_page(ns)


def wait_open(d, timeout):
    t0 = time.time()
    while time.time() - t0 < timeout:
        ns = d.nodes()
        d.accept_host_dialog(ns)
        if opened(d, ns):
            return True, time.time() - t0
        time.sleep(1.5)
    return False, time.time() - t0


def main():
    name = sys.argv[1]
    settle = float(sys.argv[2]) if len(sys.argv) > 2 else 3.0
    local = os.path.join(HERE, name + ".c")
    if not os.path.exists(local):
        print("✘ 找不到", local); return 1

    d = Driver(serial=SERIAL)
    d.disable_ime()
    d.enable_external_storage()
    d.sh("push", local, DEV_DIR + "/" + name + ".c")

    cmd = "vml run examples/c/%s.c" % name
    for attempt in range(1, 5):
        if not d.restart_app():
            print("✘ App 起不来"); return 1
        if not d.focus_entry():
            print("  第%d轮：聚焦失败" % attempt); continue
        d.clear_entry()
        d.sh("shell", "input", "keyevent", *keycodes_for(cmd))
        time.sleep(1.2)
        got = d.entry_text_stable()
        print("  第%d轮：注入后输入框 = %r" % (attempt, got))
        if got != cmd:
            continue
        b = d.run_button()
        if b is not None:
            d.sh("shell", "input", "tap", *d._center(b))
        time.sleep(1.5)
        ok, dt = wait_open(d, 240)
        print("  第%d轮：绘图窗口 %s (%.0fs)" % (attempt, ok, dt))
        if ok:
            break
        print("  输出区:", d.output_label()[:300])
    else:
        print("✘ 四轮都没开起来"); return 1

    time.sleep(settle)
    raw = "/tmp/colortest/%s.raw" % name
    subprocess.run(["adb", "-s", SERIAL, "exec-out", "screencap"], check=True,
                   stdout=open(raw, "wb"))
    print("✔", raw, os.path.getsize(raw), "字节")
    return 0


if __name__ == "__main__":
    sys.exit(main())
