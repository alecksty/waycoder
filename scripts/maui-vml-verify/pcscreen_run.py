#!/usr/bin/env python3
"""**电脑屏窗口**（`WIN_OPEN_PC` #582）的真机验收。

语料是 `Examples/c/pcscreen.c`（体检程序，刻意只画固定分辨率的东西）。

## 判据全在像素上，不靠肉眼

  ① **窗口真的开了**（命令行页让位）；
  ② **没有手柄区** —— 电脑屏的输入是键盘 + 鼠标，屏幕上不该有方向键那排；
  ③ 程序画的 640×480 边框**画出来了**（青色 `40C0FF`）；
  ④ **点一下画面 ⇒ 十字准星出现**（红色 `FF4040`）。

⚠ **判据 ④ 是这条链上最要紧的一条**：`pcscreen.c` **只监听 `MOUSEDOWN`/`MOUSEMOVE`**，
   页面那层若没把触摸转成鼠标消息，程序收不到 `MouseDown`，准星就**一动不动** ——
   而屏幕上其它一切（边框、格线、标题）看着全都正常。这正是"看起来对、其实没通"的形态。

⚠ 判据 ② 与 `games_run.py` 那条**正好相反**：那边靠"看得见手柄"认窗口开了，
   而电脑屏**本来就没有手柄** —— 所以这里必须用"命令行页让位"来判窗（`driver.window_open`
   已经把这条兜进去了，它的注释记着同类教训）。
"""

import os
import sys
import time

sys.path.insert(0, __file__.rsplit("/", 1)[0])
from driver import Driver              # noqa: E402
from screen import grab, parse_hex     # noqa: E402

SERIAL = os.environ.get("SERIAL", "emulator-5554")
PROG = "examples/c/pcscreen.c"

# `pcscreen.c` 的配色（0xAARRGGBB）
FRAME = "40C0FF"      # 外框（青）
MARK = "FF4040"       # 十字准星（红）—— 只有收到鼠标消息才画

# ⚠ 模拟器会把整帧按 ~0.925 缩放着色（色彩管理，不是程序画错），
#   所以容差要给足；两个色在这一帧里都足够独特，放宽不会误伤。
TOL = 34

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

    print(f"=== 1. 跑 {PROG} ===")
    if not d.submit(f"vml run {PROG}"):
        print("✘ 命令送不进输入框")
        return 1
    # 手机上编译一个 C 程序要一两分钟（前端 + 汇编 + 链接几万条指令），别当成卡死
    _done, _busy, saw_window = d.wait_idle(300)
    check("电脑屏窗口已打开", saw_window, "（判据：命令行页让位 —— 电脑屏没有手柄可看）")
    if not saw_window:
        return 1
    time.sleep(1.2)          # 让第一帧画上去

    print("=== 2. 界面：没有手柄区 ===")
    ns = d.nodes()
    pad = [n["text"] for n in ns if n["text"] in ("SELECT", "START", "▲ 收起手柄")]
    check("屏幕上没有手柄键/折叠条", not pad, f"实得 {pad}" if pad else "")

    print("=== 3. 画面：固定分辨率的框画出来了 ===")
    shot = grab(SERIAL)
    print(f"     截图 {shot.w}×{shot.h}")
    # ⚠ `find_color` 返回的是**第一个匹配像素的坐标**（或 None），不是像素列表 ——
    #   第一版按列表用（`len()`），拿到的是坐标元组的长度 2，读起来像"只有 2 个像素"。
    frame = shot.find_color(parse_hex(FRAME), tol=TOL, step=1)
    check("看得到青色外框（程序画的 640×480 边框）", frame is not None, f"首个像素 @{frame}")

    red_before = shot.find_color(parse_hex(MARK), tol=TOL, step=1)
    check("点之前**没有**十字准星", red_before is None, f"实得 @{red_before}")

    print("=== 4. 触摸 ⇒ 鼠标（这条链最要紧的一环）===")
    tx, ty = shot.w // 2, shot.h // 2
    d.sh("shell", "input", "tap", str(tx), str(ty))
    time.sleep(1.2)
    shot2 = grab(SERIAL)
    red_after = shot2.find_color(parse_hex(MARK), tol=TOL, step=1)
    check("点一下 ⇒ 十字准星出现（触摸当鼠标生效）", red_after is not None,
          f"首个像素 @{red_after}")

    # ── 5. **抑制触摸**：报警块不该亮 ──
    #
    # `pcscreen.c` 在"收到过触摸消息"时会在**右上角亮一块实心红**。
    # 电脑屏窗口只该发鼠标 ⇒ 那块**永远不该出现**。
    #
    # ⚠ 这一条**画面上看不出异常**：准星照旧跟着手指走（鼠标那对在发），
    #   只有这一块会亮 —— 而"同时收两对消息"正是老程序把一次点击算成两次的根因。
    print("=== 5. 抑制触摸：报警块不该亮 ===")
    bb = shot2.bbox_of(parse_hex(FRAME), tol=TOL, step=1)
    if not bb:
        check("能用青框反算出画面位置", False, "找不到外框")
    else:
        # 用**外框自己的包围盒**反算缩放，不写死设备坐标（换机器/换分辨率都成立）
        k = (bb[2] - bb[0]) / 640.0
        ax = int(bb[0] + 614 * k)      # 报警块中心（场景 614,18）
        ay = int(bb[1] + 18 * k)
        r, g, b, _ = shot2.pixel(ax, ay)
        is_red = abs(r - 0xFF) <= TOL and abs(g - 0x40) <= TOL and abs(b - 0x40) <= TOL
        check("没有触摸消息泄漏（右上角报警块未亮）", not is_red,
              f"采样 @({ax},{ay}) = #{r:02X}{g:02X}{b:02X}")

    if red_after:
        # 准星该落在**点的位置附近**，不是随便哪儿
        bb = shot2.bbox_of(parse_hex(MARK), tol=TOL, step=1)
        if bb:
            cx = (bb[0] + bb[2]) // 2
            cy = (bb[1] + bb[3]) // 2
            check("准星落在点的位置附近", abs(cx - tx) < shot.w // 8 and abs(cy - ty) < shot.h // 8,
                  f"准星 @({cx},{cy})，点的是 ({tx},{ty})")

    d.close_window()
    print("─" * 60)
    print("全部通过" if fail == 0 else f"失败 {fail} 项")
    return 1 if fail else 0


if __name__ == "__main__":
    sys.exit(main())
