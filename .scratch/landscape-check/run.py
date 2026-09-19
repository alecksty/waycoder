#!/usr/bin/env python3
"""绘图窗口横屏布局 —— 模拟器验收。

跑一个开绘图窗口的游戏，然后**转屏**，逐步抓：
  · `[WC-DRAW]` 那行探针（画布视口的真实数值）；
  · 每个方向一张截图（`shot-<名字>.png`）。

判据只有一条：**横屏时画布拿到中间整条**（`host` 高度从 28.4 那种量级变成一百多）。
"""
import os
import subprocess
import sys
import time

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
sys.path.insert(0, os.path.join(REPO, "scripts", "maui-vml-verify"))
from driver import Driver  # noqa: E402

SERIAL = "emulator-5554"
HERE = os.path.dirname(os.path.abspath(__file__))
GAME = os.environ.get("GAME", "vml run examples/c/tetris.c")
TAG = "WC-DRAW"


def log(msg):
    print("▶ %s" % msg, flush=True)


def rotate(d, n, wait=3.0):
    """n = 0 竖屏 / 1 横屏（顺时针 90°）。关掉自动旋转，否则会自己转回去。"""
    d.sh("shell", "settings", "put", "system", "accelerometer_rotation", "0")
    d.sh("shell", "settings", "put", "system", "user_rotation", str(n))
    time.sleep(wait)


def shot(d, name):
    path = os.path.join(HERE, "shot-%s.png" % name)
    with open(path, "wb") as f:
        subprocess.run(["adb", "-s", SERIAL, "exec-out", "screencap", "-p"],
                       stdout=f, check=True)
    log("截图 %s" % os.path.basename(path))


def draw_lines(d, since_marker=True):
    out = d.sh("logcat", "-d", "-s", TAG)
    return [ln for ln in out.splitlines() if TAG in ln and "host=" in ln]


def report(d, label):
    lines = draw_lines(d)
    print("\n===== %s =====" % label)
    if not lines:
        print("  （没有 %s 日志）" % TAG)
    for ln in lines[-3:]:
        print("  " + ln)
    return lines


def main():
    d = Driver(serial=SERIAL, pkg="com.tanso.waycoder")
    d.sh("logcat", "-c")
    rotate(d, 0)

    # 冷启动要十几秒（MAUI 首帧 + 解压标准库），而 `goto_shell_tab` 只重试 6×2.5s。
    # 实测冷启动那一次它会**在启动画面还没过去时就放弃**，所以先 force-stop + 等稳。
    log("启动应用")
    d.sh("shell", "am", "force-stop", d.pkg)
    time.sleep(2)
    d.start_app()
    time.sleep(12)
    if not d.goto_shell_tab(tries=12):
        print("✘ 到不了命令行页", file=sys.stderr)
        return 1

    log("跑 %s（C 前端在模拟器上要一两分钟）" % GAME)
    d.run(GAME, timeout=420)
    if not d.window_open():
        print("✘ 绘图窗口没开出来", file=sys.stderr)
        return 1
    time.sleep(2)

    report(d, "① 竖屏（初始）")
    shot(d, "1-portrait-initial")

    rotate(d, 1)
    time.sleep(2)
    report(d, "② 横屏")
    shot(d, "2-landscape")

    rotate(d, 0)
    time.sleep(2)
    report(d, "③ 转回竖屏")
    shot(d, "3-portrait-back")

    rotate(d, 1)
    time.sleep(2)
    report(d, "④ 再横屏")
    shot(d, "4-landscape-again")

    # 手柄收起（横屏「往两边伸缩」）
    for n in d.nodes():
        if n["text"] in ("▲ 收起手柄", "▼ 展开手柄"):
            d.sh("shell", "input", "tap", *d._center(n))
            break
    time.sleep(2)
    report(d, "⑤ 横屏 + 收起手柄")
    shot(d, "5-landscape-pads-hidden")

    log("收尾")
    rotate(d, 0)
    d.close_window()
    return 0


if __name__ == "__main__":
    sys.exit(main())
