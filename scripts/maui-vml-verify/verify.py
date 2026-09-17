#!/usr/bin/env python3
"""WayCoder MAUI —— 设备端 VML 验收装置。

## 它解决什么

在这之前，"手机上 VML 能不能跑" 这件事是**手敲 adb** 一条条试的：安装、推文件、
敲命令、看输出、记结论。结果是①不可重复（同样的输入两次结论可能不同）；
②失败分不清是"编译失败 / 运行失败 / 输出不符"；③每次都要重新想一遍引号怎么加。

本脚本把这件事变成一条命令：

    scripts/maui-vml-verify.sh

## 一次运行做四件事

1. **装机**（给了 `--apk` 才装；否则用设备上已有的包）
2. **布语料** —— `adb push` 到 workspace。这一步之所以能这么直接，是因为先授了
   `MANAGE_EXTERNAL_STORAGE`：workspace 会落到 `/sdcard/waycoder/workspace`，
   adb 可以直接写。没有它 workspace 在 app 私有目录里，只能靠在命令行页里
   base64 拼文件，而那条路要过 `input text` 吞字符 + 设备 shell 吃引号两道坑。
3. **逐条跑** `vml run <设备路径>`，靠命令行页的忙碌指示灯判结束（不是 sleep 固定秒数）
4. **比对 + 出表**，每一行都带**原始输出的首行**，失败能一眼看出是哪一类

## 判定分类（从左到右，先命中先算）

  INSTALL/DRIVER  装机或驱动失败（不是 VML 的问题）
  TIMEOUT         等超时了（命令可能还在跑）
  COMPILE_FAIL    输出含 `⚠️ 编译失败` / `编译超时` / `认不出这个扩展名` / `标准库`
  LINK_FAIL       输出含 `未找到标签` / `未解析标签`（前端产出的标签链不上标准库）
  RUN_FAIL        输出含其它 `⚠️` 或非零 `[退出码：N]`
  OUTPUT_MISMATCH 跑完了，但输出与期望不符
  PASS            跑完且输出符合期望

## 可重复性

- 每条命令跑之前**清屏**，读的是"这一条命令"的输出，不掺上一轮的残留
- 命令必须送进输入框且**回读校验**，送不进去直接判失败（而不是当成"没输出"）
- 同一份语料跑两次，同一判据、同一读法 —— 差异只可能来自产品本身

用法见 `--help`。
"""

import argparse
import os
import re
import subprocess
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from driver import Driver  # noqa: E402

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
DEFAULT_APK = os.path.join(REPO, "WayCoder.Maui", "bin", "Release", "net10.0-android",
                           "publish", "com.tanso.waycoder-Signed.apk")
DEVICE_WS = "/sdcard/waycoder/workspace"

# ── 判定用的模式（顺序即优先级）─────────────────────────────────────────
COMPILE_FAIL = ("⚠️ 编译失败", "编译超时", "认不出这个扩展名", "标准库清单为空", "VML 标准库")
LINK_FAIL = ("未找到标签", "未解析标签", "Unresolved")
# ⚠ 运行时崩在 VM 上是**另一套文案**（`VMLRuntime` 的寄存器转储），不带 ⚠️：
#   实测 `skel-r` 打出 `内存错误(PC=00000047): MOVE R0, @1 — 内存访问越界：地址=FFFFFFFC`。
#   不认这几行的话，一次 VM 崩溃会被判成"输出不符"—— 归错类比不判还糟。
RUN_FAIL = ("⚠️", "运行时错误", "[退出码：", "SYSCALL", "内存越界", "内存访问越界",
            "内存错误", "除零", "除零错误", "非法指令", "VML 错误")


def classify(output, expect_kind, expect, ok, err, window=False):
    """把原始输出 + 期望判成一个结论。返回 (verdict, detail)。"""
    if not ok:
        # 游戏类：主循环跑到用户关窗才退出，超时是**正常形态**，
        # 真正的判据是"有没有进到绘图页"（= 程序真的走到了 ui_win_open）。
        if expect_kind == "window":
            return ("PASS", "已开出绘图窗口（未关窗，按预期超时）") if window else (
                "RUN_FAIL", "既没关窗也没进绘图页 —— 没走到 ui_win_open")
        return "TIMEOUT", err or "等待超时"
    if expect_kind == "window":
        return "PASS", "开出绘图窗口后自行退出"
    stripped = strip_echo(output)
    # ⚠ 判 COMPILE_FAIL 必须在**剔除进度行之后**：进度行 `⏳ 正在解压 VML 标准库（39 MB / …）…`
    #   含子串 `VML 标准库`，而它是 COMPILE_FAIL 的关键字之一 ⇒ **库指纹一变，之后跑的第一条
    #   必被误判成 COMPILE_FAIL**，哪怕它已经打出了正确输出。
    #   （实测：第二轮 `skel-c` 的原文里 `SKEL-SUM=14` 明明在，却被判 COMPILE_FAIL。）
    #   注意上面几行原先用的是**未清洗的** `output`，而下面的 LINK_FAIL 用的是 `stripped`——
    #   同一个函数里两套口径，本身就是这个 bug 的温床。
    no_progress = "\n".join(
        l for l in stripped.splitlines() if not l.lstrip().startswith("⏳"))
    if any(k in no_progress for k in COMPILE_FAIL):
        return "COMPILE_FAIL", first_meaningful(stripped)
    if any(k in stripped for k in LINK_FAIL):
        return "LINK_FAIL", first_meaningful(stripped)
    m = re.search(r"\[退出码：(-?\d+)\]", stripped)
    if m and m.group(1) != "0":
        return "RUN_FAIL", first_meaningful(stripped)
    if any(k in stripped for k in ("⚠️", "运行时错误")):
        return "RUN_FAIL", first_meaningful(stripped)
    if expect_kind == "any":
        return "PASS", "(不比对输出)"
    if expect_kind == "nonempty":
        return ("PASS", "") if stripped.strip() else ("OUTPUT_MISMATCH", "输出为空")
    if expect_kind == "exact":
        got = stripped.strip()
        return ("PASS", "") if got == expect.strip() else (
            "OUTPUT_MISMATCH", "期望 %r 实得 %r" % (expect.strip(), got[:200]))
    if expect_kind == "contains":
        return ("PASS", "") if expect in stripped else (
            "OUTPUT_MISMATCH", "输出里没有 %r；实得 %r" % (expect, stripped[:200]))
    if expect_kind == "regex":
        return ("PASS", "") if re.search(expect, stripped) else (
            "OUTPUT_MISMATCH", "输出不匹配 /%s/；实得 %r" % (expect, stripped[:200]))
    return "PASS", ""


def strip_echo(output):
    """去掉命令行页的回显外壳：第一行 `~> vml run x`，以及结尾那行提示符。

    命令行页每次执行都会打印 `{提示符} {命令}` 起头、`{提示符}` 收尾
    （ShellPage.RunWithPromptAsync），比对的是**中间那段**。
    """
    lines = output.split("\n")
    while lines and lines[0].strip() == "":
        lines.pop(0)
    if lines and re.match(r"^[~/\w.]+> ", lines[0]):
        lines = lines[1:]
    while lines and (lines[-1].strip() == "" or re.match(r"^[~/\w.]+>$", lines[-1].strip())):
        lines.pop()
    return "\n".join(lines)


def first_meaningful(text):
    """第一行**有意义**的输出 —— 跳过命令行页自己的回显行与进度行。

    回显（`~> vml run x`）和进度（`⏳ 正在编译…`）都不是程序输出，拿它们当"具体表现"
    等于什么都没说（第一版就是这样，`COMPILE_FAIL` 的说明栏里躺着一条回显）。
    """
    for line in text.split("\n"):
        t = line.strip()
        if not t or re.match(r"^[~/\w.]+> ", t) or t.startswith("⏳"):
            continue
        return t[:220]
    return "(无输出)"


def load_corpus(path):
    items = []
    with open(path, encoding="utf-8") as f:
        for lineno, raw in enumerate(f, 1):
            line = raw.rstrip("\n")
            if not line.strip() or line.lstrip().startswith("#"):
                continue
            parts = line.split("\t")
            if len(parts) < 4:
                sys.exit("%s:%d 字段不足（需要 id/local/device/expect_kind[/expect[/note]]）" % (path, lineno))
            parts += [""] * (7 - len(parts))
            items.append(dict(id=parts[0], local=parts[1], device=parts[2],
                              expect_kind=parts[3], expect=parts[4], note=parts[5],
                              timeout=int(parts[6]) if parts[6].strip() else 0))
    return items


def main():
    ap = argparse.ArgumentParser(description="设备端 VML 验收装置")
    ap.add_argument("--serial", default=os.environ.get("ANDROID_SERIAL", "emulator-5554"))
    ap.add_argument("--pkg", default="com.tanso.waycoder")
    ap.add_argument("--apk", nargs="?", const=DEFAULT_APK, default=None,
                    help="装这个 APK（不给 = 用设备上已有的包；只用 --apk 不带值 = 用默认 Release 产物）")
    ap.add_argument("--corpus", default=os.path.join(HERE, "corpus.tsv"))
    ap.add_argument("--only", default=None, help="只跑 id 含这个子串的条目")
    ap.add_argument("--timeout", type=int, default=420, help="单条命令的等待上限（秒）")
    ap.add_argument("--report", default=os.path.join(HERE, "report.tsv"))
    ap.add_argument("--env", default=os.path.join(HERE, "env.txt"),
                    help="把设备/包/语料指纹等环境信息写这里（同输入两次跑，这份应完全一致）")
    ap.add_argument("--list", action="store_true", help="只列语料，不跑")
    ap.add_argument("--fresh", action="store_true",
                    help="忽略已有报告从头跑（默认**续跑**：报告里已有结论的 id 直接跳过）")
    ap.add_argument("--deploy-only", action="store_true", help="只推语料不跑")
    args = ap.parse_args()

    items = load_corpus(args.corpus)
    if args.only:
        items = [i for i in items if args.only in i["id"]]
    if args.list:
        for i in items:
            print("%-28s %-44s %-10s %s" % (i["id"], i["device"], i["expect_kind"], i["expect"]))
        return 0

    drv = Driver(serial=args.serial, pkg=args.pkg)

    # ── 0. 环境 ────────────────────────────────────────────────────────
    print("▶ 设备 %s / 包 %s" % (args.serial, args.pkg), flush=True)
    devices = drv.sh("devices").strip().splitlines()
    if not any(args.serial in d and "device" in d for d in devices[1:]):
        print("✘ 设备 %s 不在线：\n%s" % (args.serial, "\n".join(devices)), file=sys.stderr)
        return 2

    if args.apk:
        if not os.path.exists(args.apk):
            print("✘ 找不到 APK：%s" % args.apk, file=sys.stderr)
            return 2
        print("▶ 安装 %s" % os.path.basename(args.apk), flush=True)
        out = drv.sh("install", "-r", args.apk)
        print("  " + out.strip().splitlines()[-1], flush=True)
        if "Success" not in out:
            print("✘ 安装失败", file=sys.stderr)
            return 2

    drv.disable_ime()
    drv.enable_external_storage()
    drv.sh("shell", "am", "force-stop", args.pkg)
    time.sleep(1)

    # ── 1. 布语料 ──────────────────────────────────────────────────────
    pushed, missing = [], []
    for it in items:
        if it["local"] in ("", "-"):
            continue
        src = os.path.join(REPO, it["local"])
        if not os.path.exists(src):
            missing.append(it["local"])
            continue
        dst = "%s/%s" % (DEVICE_WS, it["device"])
        drv.sh("shell", "mkdir", "-p", os.path.dirname(dst))
        drv.push(src, dst)
        pushed.append(it["device"])
    print("▶ 语料：%d 条（新推 %d，已在设备上 %d）"
          % (len(items), len(pushed), len(items) - len(pushed) - len(missing)), flush=True)
    if missing:
        print("⚠ 仓库里找不到（跳过推送）：%s" % ", ".join(missing), flush=True)

    # ── 环境指纹（可重复性self-check：同输入两次跑，这份必须一致）──────
    ver = ""
    for ln in drv.sh("shell", "dumpsys", "package", args.pkg).splitlines():
        if "versionName=" in ln:
            ver = ln.strip()
            break
    env = ["serial=%s" % args.serial, "pkg=%s" % args.pkg, ver,
           "corpus=%s" % os.path.basename(args.corpus),
           "items=%d" % len(items)]
    with open(args.env, "w", encoding="utf-8") as f:
        f.write("\n".join(env) + "\n")

    if args.deploy_only:
        print("▶ 只布语料，结束")
        return 0

    # ── 2. 起界面 ──────────────────────────────────────────────────────
    if not drv.restart_app():
        print("✘ 起不来命令行页（app 没启动 / 界面不对）", file=sys.stderr)
        return 2
    print("▶ 命令行页就绪", flush=True)

    # ── 3. 逐条跑（可续跑：报告里已有结论的不重跑）────────────────────
    rows, t_all = [], time.time()
    done_ids = set()
    if not args.fresh and os.path.exists(args.report):
        with open(args.report, encoding="utf-8") as f:
            for i, ln in enumerate(f):
                if i == 0 or not ln.strip():
                    continue
                done_ids.add(ln.split("\t", 1)[0])
        if done_ids:
            print("▶ 续跑：跳过已完成的 %d 条（--fresh 可从头跑）" % len(done_ids), flush=True)
        # 把已有结论读回来，最后一起汇总
        with open(args.report, encoding="utf-8") as f:
            for i, ln in enumerate(f):
                if i == 0 or not ln.strip():
                    continue
                c = ln.rstrip("\n").split("\t")
                rows.append(dict(id=c[0], target=c[1], verdict=c[2],
                                 secs=float(c[3]), detail=c[4], note=c[5] if len(c) > 5 else "",
                                 output=""))

    def flush_report():
        """**每条跑完就落盘**。整轮可能跑一个多小时、中途被打断很正常
        （编译死循环、机器要回收），只在整个循环结束后才写报告的话，
        一次中断就等于前面全白跑。"""
        with open(args.report, "w", encoding="utf-8") as f:
            f.write("id\ttarget\tverdict\tsecs\tdetail\tnote\n")
            for r in rows:
                f.write("%s\t%s\t%s\t%.0f\t%s\t%s\n"
                        % (r["id"], r["target"], r["verdict"], r["secs"],
                           r["detail"].replace("\t", " "), r["note"]))
        with open(os.path.splitext(args.report)[0] + ".raw.txt", "w", encoding="utf-8") as f:
            for r in rows:
                f.write("=" * 70 + "\n### %s  [%s]  %s\n" % (r["id"], r["verdict"], r["target"]))
                f.write(r["output"].rstrip() + "\n")

    for n, it in enumerate(items, 1):
        if it["id"] in done_ids:
            print("[%2d/%2d] %-26s （已完成，跳过）" % (n, len(items), it["id"]), flush=True)
            continue
        target = ("vml run %s" % it["device"]) if it["device"] and it["device"] != "-" \
            else (it["note"].split("（")[0] or "vml test")
        print("[%2d/%2d] %-26s %s" % (n, len(items), it["id"], target), flush=True)
        t0 = time.time()
        r = drv.run(target, timeout=it["timeout"] or args.timeout)
        dt = time.time() - t0
        verdict, detail = classify(r["output"], it["expect_kind"], it["expect"],
                                   r["ok"], r["error"], r.get("window", False))
        print("         → %-16s %.0fs  %s" % (verdict, dt, detail), flush=True)
        rows.append(dict(id=it["id"], target=target, verdict=verdict, secs=dt,
                         detail=detail, note=it["note"], output=r["output"]))
        flush_report()

    # ── 4. 报告 ────────────────────────────────────────────────────────
    flush_report()

    tally = {}
    for r in rows:
        tally[r["verdict"]] = tally.get(r["verdict"], 0) + 1
    print("\n═══ 汇总（%.0fs 总计）═══" % (time.time() - t_all))
    for k in ("PASS", "OUTPUT_MISMATCH", "LINK_FAIL", "RUN_FAIL", "COMPILE_FAIL", "TIMEOUT"):
        if k in tally:
            print("  %-16s %d" % (k, tally[k]))
    print("\n报告：%s（原始输出：%s）"
          % (args.report, os.path.splitext(args.report)[0] + ".raw.txt"))
    return 0 if tally.get("PASS", 0) == len(rows) else 1


if __name__ == "__main__":
    sys.exit(main())
