#!/usr/bin/env python3
"""猜数字游戏 — 使用 Python 标准库实现，零依赖。

玩法：程序随机想一个数，玩家不断猜，程序提示"太大/太小"，
      直到猜中。支持多难度、多轮对战、历史最佳记录。
"""

import random
import sys

# ---- ANSI 颜色（非交互终端下自动禁用） ----
try:
    _COLORS = sys.stdout.isatty()
except Exception:
    _COLORS = False


def color(text, code):
    """给文本加上 ANSI 颜色；非 TTY 时原样返回。"""
    return f"\033[{code}m{text}\033[0m" if _COLORS else text


# ---- 常量 ----
RED = 31
GREEN = 32
YELLOW = 33
BLUE = 34
MAGENTA = 35
CYAN = 36

# 难度表: (名称, 数字范围, 最大尝试次数)
DIFFICULTIES = [
    ("简单", 50, 10),
    ("普通", 100, 7),
    ("困难", 200, 8),
]

# 各种结果的颜色映射
RESULT_COLORS = {"太大": RED, "太小": BLUE, "猜中": GREEN, "超范围": YELLOW}


def show_banner():
    """打印游戏标题横幅。"""
    print(color("=" * 52, CYAN))
    print(color("  猜 数 字  🎯", CYAN))
    print(color("  电脑想了一个数，你能在限定次数内猜中吗？", CYAN))
    print(color("=" * 52, CYAN))
    print()


def choose_difficulty():
    """让玩家选择难度，返回 (名称, 上限, 最大次数)。"""
    print(color("请选择难度：", YELLOW))
    for i, (name, limit, tries) in enumerate(DIFFICULTIES, 1):
        print(f"  {i}. {name}（1-{limit}，最多 {tries} 次）")
    while True:
        choice = input(color("输入 1/2/3 选择：", YELLOW)).strip()
        if choice in ("1", "2", "3"):
            return DIFFICULTIES[int(choice) - 1]
        print(color("无效选择，请重新输入。", RED))


def play_round(difficulty, history):
    """玩一局，返回 (是否猜中, 所用次数)。"""
    name, limit, max_tries = difficulty
    target = random.randint(1, limit)
    print()
    print(color(f"【{name}】已想好一个 1-{limit} 之间的数字，开始吧！", MAGENTA))
    print(color(f"剩余 {max_tries} 次机会。输入 0 可随时退出。", MAGENTA))

    for attempt in range(1, max_tries + 1):
        # ---- 读取并校验输入 ----
        while True:
            raw = input(color(f"[第 {attempt}/{max_tries} 次] 你的猜测：", GREEN)).strip()
            if raw == "0":
                print(color(f"认输啦？答案是 {target}，下次加油！", YELLOW))
                return False, attempt - 1
            try:
                guess = int(raw)
            except ValueError:
                print(color("请输入一个整数！", RED))
                continue
            if guess < 1 or guess > limit:
                print(color(f"数字必须在 1-{limit} 之间！", RED))
                continue
            break

        # ---- 判定 ----
        if guess == target:
            print(color(f"🎉 猜中了！答案就是 {target}，共用了 {attempt} 次。", GREEN))
            history.append(attempt)
            return True, attempt
        hint = "太大" if guess > target else "太小"
        print(f"  {color(hint, RESULT_COLORS[hint])}了，再试试。")

    print(color(f"次数用完了！答案是 {target}。", RED))
    return False, max_tries


def show_history(history):
    """显示本局会话的历史战绩。"""
    if not history:
        return
    best = min(history)
    avg = sum(history) / len(history)
    print()
    print(color("📊 战绩统计", CYAN))
    print(f"  总局数: {len(history)}    最好成绩: {best} 次    平均: {avg:.1f} 次")


def main():
    """游戏主循环。"""
    history = []
    try:
        show_banner()
        while True:
            difficulty = choose_difficulty()
            play_round(difficulty, history)
            show_history(history)
            # ---- 是否再来一局 ----
            again = input(color("\n再来一局？(y/n)：", YELLOW)).strip().lower()
            if again not in ("y", "yes", "是", ""):
                break
            print()
    except (KeyboardInterrupt, EOFError):
        print()
    finally:
        show_history(history)
        print(color("感谢游玩，再见！👋", CYAN))


if __name__ == "__main__":
    sys.exit(main())
