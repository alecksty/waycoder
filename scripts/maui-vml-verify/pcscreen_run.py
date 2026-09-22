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


def _key_region_sig(shot, bb, step=2):
    """画面左下角那行 `KEY <n>` 的像素签名（**场景坐标**经外框包围盒换算，不写死设备坐标）。

    为什么用签名而不是"找某个颜色的像素"：那行是**文字**，没有专属颜色
    （`RGB_TEXT` 与标题同色）。要判"变了没有"，逐点采样的元组本身就够 ——
    而"变了"正是"程序收到键并重画"的等价说法。
    """
    k = (bb[2] - bb[0]) / 640.0          # 用**宽**算缩放：640 是程序声明的场景宽
    # ⚠ 框取**宽**一点，别去猜 `ui_text` 的 y 是顶/基线/底 ——
    #   第一版按"y=416 那一行"收得很紧，结果框里只有网格线、一个字都没有，
    #   两次签名相同 ⇒ 把"没框住"读成了"没重画"。
    #   框大一点不会误判：网格线两帧完全一致，准星在画面中心（y≈240）够不到这里。
    x1 = int(bb[0] + 10 * k);  x2 = int(bb[0] + 320 * k)
    y1 = int(bb[1] + 368 * k); y2 = int(bb[1] + 478 * k)
    sig = []
    for y in range(y1, y2, step):
        for x in range(x1, x2, step):
            r, g, b, _ = shot.pixel(x, y)
            sig.append((r >> 4, g >> 4, b >> 4))          # 粗到 16 级，抗模拟器色彩管理的抖动
    return tuple(sig)


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

    # ── 6~8. 屏幕键盘（电脑屏窗口的输入端）──
    #
    # 判据挑的是**"点 Esc 能让程序退出"**，而不是"点了之后画面上某处变了"。
    # 理由：`pcscreen.c` 收到 `VML_KEY_ESCAPE` 时会 `break` 出主循环、关窗返回 ——
    # 这一条把整条链一次性证明完（点击 → Button → PostKeyDown → 消息队列 → `ui_wait`
    # → 程序手里的键码**就是 27**），而且结论只有"退出/没退出"两种，不含糊。
    #
    # ⚠ 别指望用"KEY 那一行文字变了"来判：屏幕键盘是**浮在画布上**的，
    #   正好盖住画面底部 —— `pcscreen.c` 的 KEY / MOUSE 两行就在那儿，看不见。
    #   （键盘能收起，但一条判据不该依赖用户先收起键盘。）
    print("=== 6. 屏幕键盘画出来了 ===")
    labels = d.button_labels()
    want = ("Esc", "Tab", "Ctrl", "Shift", "Enter", "空格", "←", "PgUp")
    have = [w for w in want if w in labels]
    check("屏幕键盘是 PC 布局", len(have) >= 6, f"找到 {have}（共 {len(labels)} 个按钮）")
    check("有收起键盘的开关", "⌨ 收起键盘" in labels)

    # ── 6.5 **键盘不盖画面**（用户明确要求：键盘区不要盖住显示屏幕）──
    #
    # 判据用两个**已经能测到的东西**比大小，不靠肉眼：
    #   · 画面下沿 = 青框包围盒的 y2（程序画的 640×480 边框就是它的显示范围）；
    #   · 键盘上沿 = 最上面那一行键的 y1（`Esc` 那一排）。
    # 画面下沿必须**在键盘上沿之上**。第一版把键盘做成浮层时，这两个值必然相反 ——
    # 而画面上看着"只是底部被压了一点"，很容易被当成没关系（实测：`KEY`/`MOUSE`
    # 两行整行看不见，而那正是老程序放状态与提示的地方）。
    print("=== 6.5 键盘不盖画面 ===")
    ks = [n for n in d.nodes() if n["cls"] == "android.widget.Button" and n["text"] == "Esc"]
    shot_kb = grab(SERIAL)
    bb0 = shot_kb.bbox_of(parse_hex(FRAME), tol=TOL, step=1)
    if not ks or not bb0:
        check("能同时量到画面与键盘", False, f"Esc 按钮 {len(ks)} 个 / 青框 {bb0}")
    else:
        keyboard_top = min(n["y1"] for n in ks)
        check("画面下沿在键盘上沿之上（键盘没盖住显示屏幕）",
              bb0[3] < keyboard_top,
              f"画面下沿 y={bb0[3]}，键盘上沿 y={keyboard_top}")

    print("=== 7. 点字母键 ⇒ 窗口**不**关（说明没把所有键都当成 Esc）===")
    if not d.tap_text("a"):
        check("能点到 a 键", False, "按钮表里没有 a")
    else:
        time.sleep(0.8)
        still = d.window_open()
        check("点 a 之后窗口还在", still)

    # ── 7.5 **程序自己重画了** —— 这一条才真正排除了"页面自己响应按键" ──
    #
    # 判据 8（点 Esc 关窗）单独看有个漏洞：**页面自己**把某个键当成"返回"也会关窗，
    # 现象一模一样。要排除它，就得要一个**只有程序才能产生的**证据 ——
    # `pcscreen.c` 每收到一个 `KEYDOWN` 都会把 `lastKey` 写进画面左下角那行 `KEY <n>`
    # 并重画。画布上的文字不是 UI 节点（读不出），但**像素变了**这件事读得出。
    #
    # ⚠ 那行字平时被屏幕键盘盖着 —— 所以先收起键盘再比。
    #   两次比较**都在收起状态**下拍（开关按钮的遮挡也一样），差异只可能来自程序。
    print("=== 7.5 点键 ⇒ **程序自己**重画了画面（排除'页面自己响应'）===")
    # ⚠ 点的是**还没按过的** `z`，不是 `a` —— 判据 7 已经点过一次 `a`，
    #   再点同一个键 `lastKey` 不变、画面自然不变，会把"没重画"判成失败。
    #   （实测踩到：加判据 6.5 时顺带动了两条判据的先后，7.5 就红了，而功能是好的 ——
    #   **判据之间会互相影响，改顺序时要把这种耦合想一遍。**）
    if labels.count("z") != 1:
        check("键位表里 z 唯一", False, f"实得 {labels.count('z')} 个")
    else:
        shot_kb = grab(SERIAL)
        bb = shot_kb.bbox_of(parse_hex(FRAME), tol=TOL, step=1)
        if not bb:
            check("能定位画面", False, "找不到青框")
        else:
            sig0 = _key_region_sig(shot_kb, bb)
            d.tap_text("z")
            time.sleep(0.8)
            sig1 = _key_region_sig(grab(SERIAL), bb)
            check("点 z ⇒ 程序重画了 KEY 行（键真的到了程序手里）", sig0 != sig1,
                  f"{len(sig0)} 点里变化 {sum(1 for p, q in zip(sig0, sig1) if p != q)} 处")

    print("=== 8. 点 Esc ⇒ 程序退出（整条链的判据）===")
    if not d.tap_text("Esc"):
        check("能点到 Esc 键", False, "按钮表里没有 Esc")
    else:
        time.sleep(2.5)
        check("点屏幕键盘的 Esc ⇒ 窗口关闭、回到命令行页", d.on_shell_page())

    d.close_window()
    print("─" * 60)
    print("全部通过" if fail == 0 else f"失败 {fail} 项")
    return 1 if fail else 0


if __name__ == "__main__":
    sys.exit(main())
