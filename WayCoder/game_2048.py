#!/usr/bin/env python3
"""终端版 2048 —— 纯标准库，无需任何第三方依赖。

运行: python game_2048.py
操作: 方向键 或 WASD 移动，Q 退出，R 重新开始。
"""

import os
import random
import sys

SIZE = 4
TARGET = 2048

# 每个数值对应的显示颜色（ANSI 背景色）
COLORS = {
    0: ("\033[90m", "      "),
    2: ("\033[97m\033[100m", "  2   "),
    4: ("\033[97m\033[40m", "  4   "),
    8: ("\033[97m\033[41m", "  8   "),
    16: ("\033[97m\033[45m", "  16  "),
    32: ("\033[97m\033[44m", "  32  "),
    64: ("\033[97m\033[43m", "  64  "),
    128: ("\033[97m\033[42m", " 128  "),
    256: ("\033[97m\033[46m", " 256  "),
    512: ("\033[97m\033[101m", " 512  "),
    1024: ("\033[97m\033[103m\033[30m", " 1024 "),
    2048: ("\033[97m\033[105m", " 2048 "),
}
RESET = "\033[0m"


class Game:
    def __init__(self):
        self.reset()

    def reset(self):
        self.board = [[0] * SIZE for _ in range(SIZE)]
        self.score = 0
        self.won = False
        self.spawn()
        self.spawn()

    def spawn(self):
        empties = [
            (r, c)
            for r in range(SIZE)
            for c in range(SIZE)
            if self.board[r][c] == 0
        ]
        if not empties:
            return
        r, c = random.choice(empties)
        self.board[r][c] = 4 if random.random() < 0.1 else 2

    # ---- 移动逻辑 ----
    def _slide(self, line):
        """将一行向左压缩合并，返回 (新行, 本行得分)。"""
        nums = [v for v in line if v != 0]
        merged = []
        score = 0
        i = 0
        while i < len(nums):
            if i + 1 < len(nums) and nums[i] == nums[i + 1]:
                merged.append(nums[i] * 2)
                score += nums[i] * 2
                i += 2
            else:
                merged.append(nums[i])
                i += 1
        return merged + [0] * (SIZE - len(merged)), score

    def move(self, direction):
        """direction: 'left'/'right'/'up'/'down'。返回是否发生移动。"""
        moved = False
        gained = 0
        board = self.board

        def set_col(col, newcol):
            nonlocal moved
            if board and any(board[r][c] != newcol[r] for r in range(SIZE)):
                moved = True
            for r in range(SIZE):
                board[r][c] = newcol[r]

        def set_row(row, newrow):
            nonlocal moved
            if any(board[row][c] != newrow[c] for c in range(SIZE)):
                moved = True
            board[row] = newrow

        for idx in range(SIZE):
            if direction in ("left", "right"):
                line = board[idx][:]
                if direction == "right":
                    line = line[::-1]
                newline, s = self._slide(line)
                if direction == "right":
                    newline = newline[::-1]
                gained += s
                set_row(idx, newline)
            else:  # up / down
                line = [board[r][idx] for r in range(SIZE)]
                if direction == "down":
                    line = line[::-1]
                newline, s = self._slide(line)
                if direction == "down":
                    newline = newline[::-1]
                gained += s
                set_col(idx, newline)

        if moved:
            self.score += gained
            self.spawn()
        return moved

    def can_move(self):
        for r in range(SIZE):
            for c in range(SIZE):
                if self.board[r][c] == 0:
                    return True
                if c + 1 < SIZE and self.board[r][c] == self.board[r][c + 1]:
                    return True
                if r + 1 < SIZE and self.board[r][c] == self.board[r + 1][c]:
                    return True
        return False

    def max_tile(self):
        return max(max(row) for row in self.board)

    # ---- 渲染 ----
    def render(self):
        lines = []
        lines.append("\033[2J\033[H")  # 清屏并回到左上角
        lines.append("      终端 2048       ")
        lines.append(f"   得分: {self.score:<8}")
        lines.append("")
        lines.append("  " + "+------" * SIZE + "+")
        for r in range(SIZE):
            row = "  |"
            for c in range(SIZE):
                v = self.board[r][c]
                color, text = COLORS.get(v, COLORS[2048])
                row += color + text + RESET + "|"
            lines.append(row)
            lines.append("  " + "+------" * SIZE + "+")
        lines.append("")
        lines.append("  方向键/WASD 移动  R 重开  Q 退出")
        if self.won:
            lines.append("\033[1;32m  恭喜！你合成 2048 了！\033[0m")
        return "\n".join(lines)


# ---- 跨平台按键读取 ----
def get_key():
    """读取单个方向/动作键。返回 'left'/'right'/'up'/'down'/'quit'/'restart'/None。"""
    if os.name == "nt":
        import msvcrt

        ch = msvcrt.getch()
        if ch in (b"\x00", b"\xe0"):  # 方向键前缀
            ch = msvcrt.getch()
            return {
                b"H": "up",
                b"P": "down",
                b"K": "left",
                b"M": "right",
            }.get(ch)
        return _map_char(ch)
    else:
        import termios
        import tty

        fd = sys.stdin.fileno()
        old = termios.tcgetattr(fd)
        try:
            tty.setraw(fd)
            ch = sys.stdin.read(1)
            if ch == "\x1b":  # ESC 序列（方向键）
                seq = sys.stdin.read(2)
                return {
                    "[A": "up",
                    "[B": "down",
                    "[D": "left",
                    "[C": "right",
                }.get(seq)
            return _map_char(ch.encode())
        finally:
            termios.tcsetattr(fd, termios.TCSADRAIN, old)


def _map_char(ch):
    if isinstance(ch, bytes):
        ch = ch.decode("latin-1").lower()
    else:
        ch = ch.lower()
    mapping = {
        "w": "up",
        "s": "down",
        "a": "left",
        "d": "right",
        "q": "quit",
        "r": "restart",
    }
    return mapping.get(ch)


def main():
    game = Game()
    try:
        while True:
            print(game.render())
            if not game.can_move():
                print("\033[1;31m  游戏结束！按 R 重新开始，Q 退出\033[0m")

            key = get_key()
            if key is None:
                continue
            if key == "quit":
                break
            if key == "restart":
                game.reset()
                continue
            if key in ("left", "right", "up", "down"):
                game.move(key)
                if not game.won and game.max_tile() >= TARGET:
                    game.won = True
    except KeyboardInterrupt:
        pass
    finally:
        print(RESET)
        print("再见！")


if __name__ == "__main__":
    main()
