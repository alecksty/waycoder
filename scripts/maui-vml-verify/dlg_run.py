#!/usr/bin/env python3
"""跑 `probes/dlg_probe.c` 并**按题作答** —— MAUI 端的四种对话框都要真弹、真答。

为什么不能复用 `driver.run()`：那个函数只会点「允许/确定」这一类按钮。本探针要考的是
**每个对话框各自的返回值**（下标 / 位掩码 / 写入缓冲区），所以答案必须按题给：
选择框点「C」、多选框勾「A 和 C」、输入框敲 `VML`。

⚠ **认题靠"屏幕形状"，不靠题目原文** —— 实测（v0.96.326 模拟器）MAUI 宿主把
`ui_dlg_select`/`ui_dlg_multi` 的 **body（R1）整个丢掉了**（只把 title 传给
`DisplayActionSheetAsync`/`MultiSelectPage`），`ui_dlg_input` 反过来丢掉 title。
按原文找锚点会永远找不到那一题，然后傻等到超时。
形状：消息框 = 允许/拒绝；选择框 = 动作单（只有取消 + A/B/C）；多选页 = 确定+取消+A/B/C；
输入框 = 有 EditText + 确定/取消。

用法：python3 scripts/maui-vml-verify/dlg_run.py [命令]
"""

import sys
import time

sys.path.insert(0, __file__.rsplit("/", 1)[0])
from driver import Driver      # noqa: E402

SERIAL = "emulator-5554"
CMD = "vml run probe/dlg_probe.c"


class DialogRunner:
    def __init__(self, d, verbose=True):
        self.d = d
        self.verbose = verbose
        self.done = set()
        self.log = []

    def nodes(self):
        return self.d.nodes()

    def _shape(self, ns):
        texts = [n["text"] for n in ns if n["text"]]
        buttons = [n["text"] for n in ns if n["cls"] == "android.widget.Button"]
        edits = [n for n in ns if n["cls"] == "android.widget.EditText"]
        return texts, buttons, edits

    def tap_text(self, ns, text, cls=None):
        for n in ns:
            if n["text"] == text and (cls is None or n["cls"] == cls):
                x, y = self.d._center(n)
                self.d.sh("shell", "input", "tap", x, y)
                return True
        return False

    def which(self, ns):
        """屏幕上这一屏是哪一道题（按形状判）。返回题号 1..5 或 None。"""
        texts, buttons, edits = self._shape(ns)
        blob = " ".join(texts)
        if "允许" in buttons or "拒绝" in buttons:
            if "D5 定时器暂停" in blob:
                return 5
            if "D1 消息框" in blob:
                return 1
            return 0            # 认不出的消息框：只报告，不乱点
        if edits and "确定" in buttons:
            return 4
        if "确定" in buttons and "取消" in buttons:
            return 3
        if "取消" in buttons and any(t in ("A", "B", "C") for t in texts):
            return 2
        return None

    def handle(self, n, ns):
        d = self.d
        if n == 0:
            if self.verbose:
                print("   [弹框] 认不出的消息框：%s" % [x["text"] for x in ns if x["text"]][:6])
            return False
        if n in self.done:
            return False
        if n == 1 or n == 5:
            if n == 5:
                # **故意等 3 秒再点** —— 这正是这道题的判据：宿主若没在弹框期间停表，
                # 这 3 秒会往队列里堆几十条 Timer 消息。
                self.log.append("D5 等 3 秒（弹框期间定时器应当停着）")
                time.sleep(3.0)
                ns = self.d.nodes()
            self.done.add(n)
            ok = self.tap_text(ns, "允许")
            self.log.append("D%d 点「允许」=%s" % (n, ok))
            return ok
        if n == 2:
            self.done.add(n)
            ok = self.tap_text(ns, "C")
            self.log.append("D2 点「C」=%s" % ok)
            return ok
        if n == 3:
            self.done.add(n)
            a = self.tap_text(ns, "A")
            time.sleep(0.9)
            ns = self.d.nodes()
            c = self.tap_text(ns, "C")
            time.sleep(0.9)
            ns = self.d.nodes()
            ok = self.tap_text(ns, "确定", cls="android.widget.Button")
            self.log.append("D3 勾A=%s 勾C=%s 确定=%s" % (a, c, ok))
            return a and c and ok
        if n == 4:
            self.done.add(n)
            _, _, edits = self._shape(ns)
            if edits:
                self.d.sh("shell", "input", "tap", *self.d._center(edits[0]))
                time.sleep(0.6)
            self.d.sh("shell", "input", "text", "VML")
            time.sleep(0.8)
            ns = self.d.nodes()
            ok = self.tap_text(ns, "确定", cls="android.widget.Button")
            self.log.append("D4 敲 VML 后点「确定」=%s" % ok)
            return ok
        return False

    def run(self, cmd, timeout=600):
        d = self.d
        d.recover()
        for _ in range(3):
            if d.is_idle():
                break
            d.abort_running()
        pre = d.output_label()
        if not d.submit(cmd):
            return dict(ok=False, output="", error="命令未能送进输入框")
        t0 = time.time()
        last = None
        while time.time() - t0 < timeout:
            ns = self.nodes()
            out = d.output_label(ns)
            if "DLG-DONE" in (out or ""):
                time.sleep(1.0)
                out = d.output_label()
                delta = out[len(pre):] if out.startswith(pre) else out
                return dict(ok=True, output=delta, error=None)
            n = self.which(ns)
            if n != last:
                if n and self.verbose:
                    print("   [弹框] 第 %s 题" % n, flush=True)
                last = n
            if n:
                self.handle(n, ns)
                time.sleep(1.2)
                continue
            time.sleep(1.5)
        out = d.output_label()
        delta = out[len(pre):] if out.startswith(pre) else out
        return dict(ok=False, output=delta, error="超时（没等到 DLG-DONE）")


def main():
    d = Driver(serial=SERIAL)
    if not d.restart_app():
        print("✘ 起不来")
        return 1
    d.disable_ime()
    d.enable_external_storage()
    r = DialogRunner(d).run(CMD)
    print("ok=%s err=%s" % (r["ok"], r["error"]))
    print("---- output ----")
    print(r["output"])
    print("---- end ----")
    return 0 if r["ok"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
