#!/usr/bin/env python3
"""`gorilla.bas` 一次跑完全套验收：上设备 → 跑起来 → 画面采样 → 触摸 → 瞄靶。

拆成一份是因为这中间几步**都依赖同一个"这一局"的状态**：冷启动会重跑一次程序，
角度/力度/比分全归零，前面量到的读数就全对不上了。

用法：python3 scripts/maui-vml-verify/gorilla_all.py
"""

import subprocess
import sys
import time

sys.path.insert(0, __file__.rsplit("/", 1)[0])
from driver import Driver      # noqa: E402

SERIAL = "emulator-5554"
CMD = "vml run examples/basic/gorilla.bas"
HERE = __file__.rsplit("/", 1)[0]


def main():
    d = Driver(serial=SERIAL)
    print("=== 0. 冷启动到命令行页 ===")
    if not d.restart_app():
        print("✘ 起不来")
        return 1
    d.disable_ime()
    d.enable_external_storage()

    print("=== 1. 送命令并等编译+开窗（BASIC 在模拟器上要一两分钟）===")
    # ⚠ **不要用 `d.run()`** —— 它在看见绘图窗口后会调 `close_window()`（按 BACK），
    #   那会发一条 `WindowClose` 给程序、程序据此退出主循环，后面的画面采样就全落空了。
    #   `run()` 的语义是"跑一条命令拿它的输出"，而这里要的是"把程序跑起来别动它"。
    assert d.submit(CMD), "命令没送进输入框"
    t0 = time.time()
    opened = False
    while time.time() - t0 < 420:
        time.sleep(3)
        if d.window_open():
            opened = True
            break
        d.accept_host_dialog()          # 编译期间可能弹权限/确认框
    print("   绘图窗口：%s（耗时 %.0fs）" % (opened, time.time() - t0))
    print("   输出区尾部：%s" % d.output_label()[-500:].replace("\n", " | "))
    if not opened:
        print("✘ 没看到绘图窗口")
        return 1

    # 开局那个 `ui_dlg_msg` 介绍框会挡住画面 —— 点「允许」（= 确定）过掉
    time.sleep(4)
    d.accept_host_dialog()
    time.sleep(4)

    rc = {}
    for name, script in (("画面", "gorilla_pixels.py"),
                         ("触摸", "gorilla_touch.py"),
                         ("命中", "gorilla_hit.py")):
        print("\n=== %s：%s ===" % (name, script))
        rc[name] = subprocess.call([sys.executable, "%s/%s" % (HERE, script), SERIAL]
                                   if script != "gorilla_hit.py" else
                                   [sys.executable, "%s/%s" % (HERE, script)])

    print("\n各步退出码：%s" % rc)
    return 0 if rc.get("画面") == 0 and rc.get("触摸") == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())
