#!/usr/bin/env python3
"""真机验收：**五个 QBasic 移植游戏**能不能在手机上跑起来。

判据只有一条 —— **绘图窗口真的开了**（看得见手柄键或折叠条）。
理由：这五个游戏的自检**桌面 vmlcli 上全绿**（那是另一条链），而手机上走的是
`MauiVml`：编译参数、库解包、宿主 syscall 三条都不一样，
**桌面全绿、手机上是坏的**在本仓出现过不止一次。所以这里不重复判游戏逻辑，
只判"到手机上还活着"。

⚠ 手机上编译一个游戏要一两分钟（前端 + 汇编 + 链接几万条指令），五个加起来七八分钟，
   别当成卡死。超时给足。

用法：
    python3 scripts/maui-vml-verify/games_run.py                  # 默认小米
    SERIAL=emulator-5554 python3 scripts/maui-vml-verify/games_run.py
    python3 scripts/maui-vml-verify/games_run.py nibbles          # 只跑一个
"""

import os
import sys
import time

sys.path.insert(0, __file__.rsplit("/", 1)[0])
from driver import Driver      # noqa: E402

SERIAL = os.environ.get("SERIAL", "cd53cb14")

GAMES = [
    ("donkey", "examples/basic/donkey.bas", 420),
    ("hammurabi", "examples/basic/hammurabi.bas", 420),
    ("lander", "examples/basic/lander.bas", 480),
    ("blackjack", "examples/basic/blackjack.bas", 420),
    ("nibbles", "examples/basic/nibbles.bas", 480),
]

fail = 0


def check(name, ok, detail=""):
    global fail
    print(f"  {'✅' if ok else '❌'} {name}{('  ' + detail) if detail else ''}")
    if not ok:
        fail += 1


def main():
    want = sys.argv[1:] if len(sys.argv) > 1 else None
    d = Driver(serial=SERIAL)
    print(f"设备: {SERIAL}")

    print("=== 0. 冷启动到命令行页 ===")
    if not d.restart_app():
        print("✘ App 起不来")
        return 1
    d.disable_ime()

    for name, path, timeout in GAMES:
        if want and name not in want:
            continue
        print(f"=== {name} ===")
        r = d.run(f"vml run {path}", timeout=timeout)
        time.sleep(1.0)
        ns = d.nodes()
        # ⚠ 判"窗口开了"必须看见手柄/折叠条 —— `uiautomator dump` 偶尔返回空树，
        #   用"命令行页不见了"当兜底会把"还在编译"误读成"窗口开了"（rot_run.py 的教训）。
        opened = any(n["text"] in ("SELECT", "START", "▲ 收起手柄") for n in ns)
        check(f"{name} 绘图窗口已打开", opened or r.get("window"))
        if not (opened or r.get("window")):
            tail = (r.get("output") or "").strip()[-400:]
            print("     输出尾部:", tail if tail else "(空)")
        # 关掉它，免得挡住下一个
        d.close_window()
        time.sleep(1.5)

    print("─" * 60)
    print("全部通过" if fail == 0 else f"失败 {fail} 项")
    return 1 if fail else 0


if __name__ == "__main__":
    sys.exit(main())
