#!/usr/bin/env python3
"""跑一个 VML 程序、等绘图窗口出现、截屏拉回来 —— 颜色校对用的最小装置。

与 scripts/maui-vml-verify/driver.py 的分工：那个是"跑语料、读输出"，这个是
"跑一个开窗的程序、**把画面留下来**"。它等的是绘图页出现（不是等空闲），
因为要的就是窗口开着的那一帧。

用法：
    python3 run_and_shot.py examples/c/calc.c /tmp/colortest/calc.png
"""
import sys, os, time
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "..", "scripts", "maui-vml-verify"))
from driver import Driver  # noqa: E402

def main():
    cmd_path = sys.argv[1]
    out_png = sys.argv[2]
    settle = float(sys.argv[3]) if len(sys.argv) > 3 else 4.0
    serial = os.environ.get("SERIAL", "emulator-5554")

    d = Driver(serial=serial)
    d.disable_ime()
    d.enable_external_storage()
    if not d.restart_app():
        print("✘ 起不来")
        return 1
    # 命令去掉前缀目录，命令行页的 cwd 就是 workspace
    rel = cmd_path
    if not rel.startswith("examples/"):
        rel = "examples/" + rel.split("/")[-1] if "/" not in rel else rel
    full = "vml run " + (cmd_path if cmd_path.startswith("examples/") else rel)
    print("命令:", full)
    if not d.submit(full):
        print("✘ 命令没送进去")
        return 1

    t0 = time.time()
    seen = False
    while time.time() - t0 < 200:
        ns = d.nodes()
        d.accept_host_dialog(ns)
        if d.window_open(ns):
            seen = True
            break
        time.sleep(2)
    if not seen:
        print("✘ 绘图窗口没出现（%.0fs）" % (time.time() - t0))
        print("输出区:", d.output_label()[:500])
        return 1
    print("✔ 窗口已开（%.0fs），等 %.1fs 让它画完" % (time.time() - t0, settle))
    time.sleep(settle)
    tmp = "/sdcard/_shot.png"
    d.sh("shell", "screencap", "-p", tmp)
    d.sh("pull", tmp, out_png)
    print("✔ 截屏:", out_png, os.path.getsize(out_png), "字节")
    return 0

if __name__ == "__main__":
    sys.exit(main())
