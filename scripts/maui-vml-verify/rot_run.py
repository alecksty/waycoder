#!/usr/bin/env python3
"""跑 `probes/rot_probe.c`：**等它把窗口开出来 → 转屏 → 等它自己关窗 → 读结果**。

为什么不用 `driver.run()`：那个函数一看见绘图窗口就把它关掉（`close_window()` 按 BACK），
而这道题要的正是"窗口活着的时候转屏"。转屏走 `settings put system user_rotation`
（先 `accelerometer_rotation 0` 把自动旋转关掉，否则设置不生效）。

用法：python3 scripts/maui-vml-verify/rot_run.py [--no-rotate]
"""

import subprocess
import sys
import time

sys.path.insert(0, __file__.rsplit("/", 1)[0])
from driver import Driver      # noqa: E402

SERIAL = "emulator-5554"
CMD = "vml run probe/rot_probe.c"


def rotate(d, n):
    d.sh("shell", "settings", "put", "system", "accelerometer_rotation", "0")
    d.sh("shell", "settings", "put", "system", "user_rotation", str(n))
    time.sleep(2.5)


def main():
    do_rotate = "--no-rotate" not in sys.argv
    d = Driver(serial=SERIAL)
    if not d.restart_app():
        print("✘ 起不来")
        return 1
    d.disable_ime()
    d.enable_external_storage()
    d.sh("shell", "settings", "put", "system", "accelerometer_rotation", "0")
    d.sh("shell", "settings", "put", "system", "user_rotation", "0")
    time.sleep(2)

    pre = d.output_label()
    if not d.submit(CMD):
        print("✘ 命令没送进去")
        return 1

    t0 = time.time()
    opened = False
    while time.time() - t0 < 300:
        time.sleep(3)
        if d.window_open():
            opened = True
            break
        if "ROT-DONE" in (d.output_label() or ""):
            break
    print("绘图窗口：%s（%.0fs）" % (opened, time.time() - t0))
    if do_rotate and opened:
        print("→ 转横屏")
        rotate(d, 1)
        time.sleep(4)
        print("→ 转回竖屏")
        rotate(d, 0)

    # 等程序自己关窗并打完结果
    t0 = time.time()
    while time.time() - t0 < 120:
        time.sleep(2)
        if "ROT-DONE" in (d.output_label() or ""):
            break
    time.sleep(1)
    out = d.output_label()
    delta = out[len(pre):] if out.startswith(pre) else out
    print("---- output ----")
    print(delta)
    print("---- end ----")
    ok = "ROT-DONE" in out
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
