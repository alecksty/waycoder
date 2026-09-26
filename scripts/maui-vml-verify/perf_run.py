#!/usr/bin/env python3
"""跑一个 VML 绘图程序并**收集宿主的每帧分段耗时日志**（tag `WCVML`）。

## 为什么单独一个脚本（`verify.py` 不行）

`verify.py` 判的是**程序自己的输出**（屏幕上那几行），跑完就关窗走人。
而"绘图慢"这件事在程序输出里看不出来 —— 它只知道自己的帧率，
不知道时间花在了**宿主的哪一段**（DSL 拼接 / 解析 / 平台绘制 / 各指令类型）。

`DrawWindowPage` 每 30 帧往 logcat 打一行分段耗时，所以这里只需要：
**把命令送进去 → 让它跑够久 → 把那些行捞回来**。

## ⚠ 判据一律用 logcat，不用 UI 树

第一版照着 `rot_run.py` 写，靠 `driver.nodes()`（`uiautomator dump`）轮询
"手柄按钮出现了没有" —— 在性能采集里这是**多余的**：窗口开没开，
`WCVML` 日志出现与否是**更直接**的证据（而且 dump 偶发返回空树，会让脚本空转到超时，
实测就是这么卡住的）。所以这里只认日志。

## 用法

    python3 scripts/maui-vml-verify/perf_run.py                       # 默认 block_bench
    python3 scripts/maui-vml-verify/perf_run.py --prog examples/cpp/gorilla.cpp
    python3 scripts/maui-vml-verify/perf_run.py --secs 90             # 采集 90 秒
    python3 scripts/maui-vml-verify/perf_run.py --local .scratch/draw_bench.c --dev bench.c
"""

import argparse
import os
import re
import subprocess
import sys
import time

sys.path.insert(0, __file__.rsplit("/", 1)[0])
from driver import Driver      # noqa: E402

SERIAL = "emulator-5554"   # 默认模拟器；真机用 --serial（见 `adb devices -l`）
DEV_WS = "/storage/emulated/0/waycoder/workspace"
REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))


def wc_lines(d, tag="WCVML"):
    """取 logcat 里该 tag 的全部消息正文。"""
    raw = d.sh("shell", "logcat", "-d", "-s", tag)
    out = []
    for ln in raw.splitlines():
        if tag not in ln:
            continue
        out.append(ln.split(tag, 1)[1].lstrip(" :").strip())
    return out


def fps_of(lines):
    vals = []
    for ln in lines:
        m = re.search(r"实际\s+([\d.]+)\s*fps", ln)
        if m:
            vals.append(float(m.group(1)))
    return vals


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--serial", default=SERIAL, help="设备序列号（真机也是这个参数）")
    ap.add_argument("--prog", default="examples/c/block_bench.c")
    ap.add_argument("--local", default=None, help="先把仓库里这个文件推到设备再跑")
    ap.add_argument("--dev", default="bench.c")
    ap.add_argument("--secs", type=int, default=60, help="开始出帧后再采集多久")
    ap.add_argument("--boot-wait", type=int, default=420, help="等编译+出首帧的上限（秒）")
    ap.add_argument("--shot", default=None, help="采集结束时截一张 PNG 存到这里（给人看）")
    ap.add_argument("--shot-raw", default=None,
                    help="同一次截屏另存一份**裸帧缓冲**（给 shot_analyze.py 做连通域比对）")
    args = ap.parse_args()

    d = Driver(serial=args.serial)
    if not d.restart_app():
        print("✘ 起不来命令行页")
        return 1
    d.disable_ime()
    d.enable_external_storage()

    cmd = "vml run " + args.prog
    if args.local:
        src = args.local if os.path.isabs(args.local) else os.path.join(REPO, args.local)
        d.sh("shell", "mkdir", "-p", DEV_WS)
        d.push(src, "%s/%s" % (DEV_WS, args.dev))
        cmd = "vml run " + args.dev
        print("▶ 推 %s → %s/%s" % (args.local, DEV_WS, args.dev))
    print("▶ 命令：%s" % cmd)

    d.sh("shell", "logcat", "-c")
    if not d.submit(cmd):
        print("✘ 命令没送进去")
        return 1

    # ── 等首帧：判据是 **WCVML 日志出现**（不看 UI 树）──
    t0 = time.time()
    while time.time() - t0 < args.boot_wait:
        time.sleep(5)
        if wc_lines(d):
            break
    else:
        # ⚠ **先别急着报"编译失败"**（v0.96.483 实测踩到）：程序里有 `ui_dlg_msg`
        # 开场说明框的（gorilla_pro / 一堆移植游戏）会**停在原生弹框上等用户点**，
        # 那时一张帧都没出 —— 日志为空、退出码 0，看着像编译挂了，其实只是没人点。
        # 所以失败时**自动截一张屏**：弹框在截图里一眼可见（编译失败则是黑屏或报错文字）。
        shot = args.shot or "/tmp/perf_stuck.png"
        # ⚠ 用 subprocess 直取**二进制**（与下面正常截图那条同一个理由）：
        #   `driver.sh()` 把 stdout 解成文本，PNG 会被毁掉。
        with open(shot, "wb") as f:
            f.write(subprocess.run(["adb", "-s", args.serial, "exec-out", "screencap", "-p"],
                                   capture_output=True).stdout)
        print("✘ 等不到渲染日志 —— 已截图：%s" % shot)
        print("  ⚠ 常见原因：程序有**开场说明弹框**（ui_dlg_msg）停在等用户点。")
        print("    看一眼截图：有弹框就 `adb shell input tap <允许的坐标>` 点掉它再重跑。")
        print("    没弹框才是真的编译失败，那就要看设备屏幕上的报错文字了。")
        return 1
    boot = time.time() - t0
    print("▶ 开始出帧（编译+启动 %.0fs），采集 %ds …" % (boot, args.secs), flush=True)

    time.sleep(args.secs)
    lines = wc_lines(d)
    fps = fps_of(lines)
    print("\n── 宿主每帧分段（共 %d 行，末 12 行）──" % len(lines))
    for ln in lines[-12:]:
        print("  " + ln)
    if fps:
        print("\n▶ 实际 fps：min %.1f / avg %.1f / max %.1f（%d 个采样）"
              % (min(fps), sum(fps) / len(fps), max(fps), len(fps)))

    # 截屏：性能数字只说明"快不快"，画面还得人看一眼（画布变换写错会镜像/错切，
    # 那种错不会崩、也不会报错，只会画得不对）。
    # ⚠ 用 subprocess 直取**二进制**：`driver.sh()` 解成文本，PNG 会被毁掉。
    # ⚠ 两次 screencap 之间画面会变（动态程序）⇒ **一次抓两份**，别抓两回。
    if args.shot or args.shot_raw:
        png = subprocess.run(["adb", "-s", args.serial, "exec-out", "screencap", "-p"],
                             capture_output=True).stdout
        raw = subprocess.run(["adb", "-s", args.serial, "exec-out", "screencap"],
                             capture_output=True).stdout
        if args.shot:
            with open(args.shot, "wb") as f:
                f.write(png)
            print("▶ PNG：%s（%d 字节）" % (args.shot, len(png)))
        if args.shot_raw:
            with open(args.shot_raw, "wb") as f:
                f.write(raw)
            print("▶ 裸帧缓冲：%s（%d 字节）" % (args.shot_raw, len(raw)))

    d.close_window()
    return 0


if __name__ == "__main__":
    sys.exit(main())
