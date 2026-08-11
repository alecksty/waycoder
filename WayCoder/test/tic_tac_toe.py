#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""井字棋 (Tic-Tac-Toe) — 终端人机对战小游戏
玩家执 X，电脑执 O。输入 1-9 落子（数字对应九宫格位置）。
"""

import random
import sys

EMPTY = " "
PLAYER = "X"
COMPUTER = "O"

# 九宫格位置 → 棋盘坐标
POSITIONS = {
    1: (0, 0), 2: (0, 1), 3: (0, 2),
    4: (1, 0), 5: (1, 1), 6: (1, 2),
    7: (2, 0), 8: (2, 1), 9: (2, 2),
}


def new_board():
    """返回一个 3x3 空棋盘。"""
    return [[EMPTY] * 3 for _ in range(3)]


def render(board):
    """把棋盘渲染成字符串。"""
    lines = []
    for i, row in enumerate(board):
        cells = " │ ".join(c if c != EMPTY else str(i * 3 + j + 1)
                           for j, c in enumerate(row))
        lines.append(" " + cells + " ")
        if i < 2:
            lines.append("───┼───┼───")
    return "\n".join(lines)


def winner(board):
    """返回获胜方 ('X'/'O')，无获胜返回 None。"""
    # 行、列、两条对角线
    lines = board + [list(col) for col in zip(*board)]
    lines.append([board[0][0], board[1][1], board[2][2]])
    lines.append([board[0][2], board[1][1], board[2][0]])
    for line in lines:
        if line[0] != EMPTY and line[0] == line[1] == line[2]:
            return line[0]
    return None


def is_full(board):
    """棋盘是否已下满。"""
    return all(cell != EMPTY for row in board for cell in row)


def empty_cells(board):
    """返回所有空格的 (行, 列) 列表。"""
    return [(r, c) for r in range(3) for c in range(3)
            if board[r][c] == EMPTY]


def computer_move(board):
    """电脑走棋：简单 AI。
    1. 能赢就赢；2. 堵玩家；3. 占中心；4. 随机。
    """
    # 1. 自己赢
    for r, c in empty_cells(board):
        board[r][c] = COMPUTER
        if winner(board) == COMPUTER:
            return r, c
        board[r][c] = EMPTY
    # 2. 堵玩家
    for r, c in empty_cells(board):
        board[r][c] = PLAYER
        if winner(board) == PLAYER:
            board[r][c] = EMPTY
            return r, c
        board[r][c] = EMPTY
    # 3. 占中心
    if board[1][1] == EMPTY:
        return 1, 1
    # 4. 随机落子（优先角落）
    cells = empty_cells(board)
    corners = [(0, 0), (0, 2), (2, 0), (2, 2)]
    available_corners = [c for c in corners if c in cells]
    pool = available_corners or cells
    return random.choice(pool)


def play_round():
    """进行一局游戏，返回胜负结果 ('win'/'lose'/'draw')。"""
    board = new_board()
    print("\n" + render(board))
    print("  你执 X，电脑执 O，先手。输入 1-9 落子。\n")

    while True:
        # —— 玩家回合 ——
        while True:
            try:
                raw = input("你的落子 (1-9): ").strip()
                pos = int(raw)
                r, c = POSITIONS[pos]
                if board[r][c] != EMPTY:
                    print("该位置已占用，重新选择。")
                    continue
                board[r][c] = PLAYER
                break
            except (KeyError, ValueError):
                print("请输入 1-9 的数字。")

        print("\n" + render(board))
        if winner(board) == PLAYER:
            return "win"
        if is_full(board):
            return "draw"

        # —— 电脑回合 ——
        print("\n电脑思考中...")
        r, c = computer_move(board)
        board[r][c] = COMPUTER
        print("\n" + render(board))
        if winner(board) == COMPUTER:
            return "lose"
        if is_full(board):
            return "draw"


def main():
    print("=" * 28)
    print("   井字棋 · Tic-Tac-Toe")
    print("=" * 28)

    score = {"win": 0, "lose": 0, "draw": 0}
    while True:
        result = play_round()
        score[result] += 1
        if result == "win":
            print("\n🎉 你赢了！")
        elif result == "lose":
            print("\n😅 电脑赢了，再来一局？")
        else:
            print("\n🤝 平局。")

        print(f"\n战绩: 胜 {score['win']} / 负 {score['lose']} / 平 {score['draw']}")

        again = input("\n再来一局? (y/n): ").strip().lower()
        if again not in ("y", "yes", "是", ""):
            print("再见，感谢游玩！")
            break


if __name__ == "__main__":
    try:
        main()
    except (KeyboardInterrupt, EOFError):
        print("\n已退出。")
        sys.exit(0)
