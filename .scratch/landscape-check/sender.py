#!/usr/bin/env python3
"""自己送命令 —— 绕开两个实测坑：
  ① `input text` 从第一个空格就被截断（要逐键码发）；
  ② 逐键码发时**最后一个字符会丢**（这里"读回→补发缺的尾巴"直到一字不差）。
"""
import sys, time
REPO = "/Users/shitanyu/Desktop/source/mycoder/my-coder"
sys.path.insert(0, REPO + "/scripts/maui-vml-verify")
from driver import Driver, keycodes_for

d = Driver(serial="emulator-5554")


def entry():
    for n in d.nodes():
        if n["cls"] == "android.widget.EditText":
            return n
    return None


def entry_text():
    e = entry()
    return None if e is None else e["text"]


def send(cmd, rounds=8):
    for r in range(rounds):
        e = entry()
        if e is None:
            time.sleep(2); continue
        d.sh("shell", "input", "tap", *d._center(e))
        time.sleep(0.6)
        for _ in range(70):
            d.sh("shell", "input", "keyevent", "67")
        time.sleep(0.4)
        codes = keycodes_for(cmd)
        for i in range(0, len(codes), 6):
            d.sh("shell", "input", "keyevent", *codes[i:i + 6])
            time.sleep(0.35)
        # 补发缺的尾巴（最后一个字符实测会丢）
        for _ in range(6):
            time.sleep(0.7)
            got = entry_text() or ""
            if got == cmd:
                break
            if not cmd.startswith(got):
                break
            tail = cmd[len(got):]
            d.sh("shell", "input", "keyevent", *keycodes_for(tail))
        time.sleep(0.5)
        if entry_text() == cmd:
            break
        print("  第%d轮不符：%r" % (r + 1, entry_text()))
    else:
        return False
    b = d.run_button()
    d.sh("shell", "input", "tap", *d._center(b))
    time.sleep(1.0)
    d.sh("shell", "input", "keyevent", "66")   # 兜底再来一次（重复提交会被 _busy 挡掉）
    time.sleep(1.5)
    return True


if __name__ == "__main__":
    cmd = sys.argv[1]
    print("送出:", send(cmd), repr(entry_text()))
