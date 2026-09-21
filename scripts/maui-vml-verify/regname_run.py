#!/usr/bin/env python3
"""真机验收：**寄存器形变量名**（`regname.c`）与**猴子扔香蕉**（`gorilla.bas`）。

为什么要单独立一份：这两条都**只有在真机上才成立**——
  · `regname.c` 钉的是「变量名就叫 f1/d1/l1/R1」在**手机端那套 API 级别/解释路径**下也对
    （桌面走 vmlcli、手机走 MauiVml，两条链的编译参数与库解包方式不同）；
  · `gorilla.bas` 钉的是**绘图窗口真的开出来**（要看得见 `SELECT`/`START` 手柄键，
    见 `rot_run.py` 里那条教训：`uiautomator dump` 偶尔返回空树，
    用"命令行页不见了"当兜底判据会把"编译中"误读成"窗口开了"）。

用法：
    python3 scripts/maui-vml-verify/regname_run.py            # 默认小米（cd53cb14）
    SERIAL=emulator-5554 python3 scripts/maui-vml-verify/regname_run.py

⚠ 手机上编译 C / BASIC 都要一两分钟（3~6 万条指令），超时给足，别当成卡死。
"""

import os
import sys
import time

sys.path.insert(0, __file__.rsplit("/", 1)[0])
from driver import Driver      # noqa: E402

SERIAL = os.environ.get("SERIAL", "cd53cb14")

# 判据写死成期望值：`regname.c` 自己会打印「期望合计 = … = 831」与「实测合计」，
# 这里再对一次，免得只靠肉眼看输出。
REGNAME_EXPECT = ["实测合计 = 831", "✅ 通过：寄存器形变量名全部正常"]

fail = 0


def check(name, ok, detail=""):
    global fail
    print(f"  {'✅' if ok else '❌'} {name}{('  ' + detail) if detail else ''}")
    if not ok:
        fail += 1


def main():
    d = Driver(serial=SERIAL)
    print(f"设备: {SERIAL}")

    print("=== 0. 冷启动到命令行页 ===")
    if not d.restart_app():
        print("✘ App 起不来")
        return 1
    d.disable_ime()
    d.enable_external_storage()

    # ── 1. 寄存器形变量名 ────────────────────────────────────────────────
    print("=== 1. 寄存器形变量名（regname.c）===")
    r = d.run("vml run examples/c/regname.c", timeout=420)
    out = r.get("output") or ""
    print(out.strip()[-500:] if out.strip() else "(无输出)")
    for want in REGNAME_EXPECT:
        check(f"输出含「{want}」", want in out)

    # ── 2. 猴子扔香蕉 ────────────────────────────────────────────────────
    # 判据只看「窗口真的开了」：这是绘图程序能不能玩的最低门槛，
    # 手感/计分另有 gorilla_all.py 那一套（依赖同一局的状态，不在这里重复）。
    print("=== 2. 猴子扔香蕉（gorilla.bas）===")
    r = d.run("vml run examples/basic/gorilla.bas", timeout=600)
    time.sleep(1.0)
    ns = d.nodes()
    has_pad = any(n["text"] in ("SELECT", "START", "▲ 收起手柄") for n in ns)
    check("绘图窗口已打开（看得见手柄/折叠条）", r.get("window") or has_pad)
    if not (r.get("window") or has_pad):
        print("     输出尾部:", (r.get("output") or "").strip()[-400:])

    print("─" * 60)
    print(f"{'全部通过' if fail == 0 else f'失败 {fail} 项'}")
    return 1 if fail else 0


if __name__ == "__main__":
    sys.exit(main())
