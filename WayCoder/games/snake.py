#!/usr/bin/env python3
"""终端贪吃蛇游戏 — 使用 Python 标准库 curses 实现，零依赖。"""

import curses
import random
import sys


def draw_border(stdscr, game_h, game_w):
    """绘制游戏区域边框。"""
    stdscr.addch(0, 0, '+')
    stdscr.addch(0, game_w, '+')
    stdscr.addch(game_h, 0, '+')
    stdscr.addch(game_h, game_w, '+')
    for x in range(1, game_w):
        stdscr.addch(0, x, '-')
        stdscr.addch(game_h, x, '-')
    for y in range(1, game_h):
        stdscr.addch(y, 0, '|')
        stdscr.addch(y, game_w, '|')


def spawn_food(snake, game_h, game_w):
    """在蛇身之外的随机位置生成食物。"""
    while True:
        y = random.randint(1, game_h - 1)
        x = random.randint(1, game_w - 1)
        if (y, x) not in snake:
            return y, x


def game_over_screen(stdscr, score):
    """游戏结束画面。"""
    h, w = stdscr.getmaxyx()
    msg = f"游戏结束！最终得分: {score}"
    sub = "按 R 重新开始，按 Q 退出"
    stdscr.clear()
    stdscr.addstr(h // 2 - 1, max(0, (w - len(msg)) // 2), msg)
    stdscr.addstr(h // 2 + 1, max(0, (w - len(sub)) // 2), sub)
    stdscr.refresh()
    stdscr.timeout(-1)  # 阻塞等待按键
    while True:
        key = stdscr.getch()
        if key in (ord('r'), ord('R')):
            return True
        if key in (ord('q'), ord('Q'), 27):
            return False


def run_game(stdscr):
    """运行一局游戏，返回得分；终端太小时返回 None。"""
    curses.curs_set(0)
    h, w = stdscr.getmaxyx()

    # 最小终端尺寸检查
    if h < 15 or w < 30:
        stdscr.clear()
        stdscr.addstr(0, 0, "终端太小！请放大窗口到至少 30x15。")
        stdscr.refresh()
        stdscr.timeout(-1)
        stdscr.getch()
        return None

    game_h, game_w = h - 3, w - 1  # 底部保留 3 行显示信息

    # 蛇：列表存坐标 (y, x)，头部在末尾
    cy, cx = game_h // 2, game_w // 2
    snake = [(cy, cx), (cy, cx - 1), (cy, cx - 2)]
    direction = (0, 1)  # 初始向右
    food = spawn_food(snake, game_h, game_w)
    score = 0

    stdscr.nodelay(True)
    stdscr.timeout(100)  # 每 100ms 移动一格

    while True:
        # ---- 读取输入 ----
        key = stdscr.getch()
        if key == ord('q') or key == ord('Q') or key == 27:  # Q / ESC 退出
            break
        if key == curses.KEY_UP and direction != (1, 0):
            direction = (-1, 0)
        elif key == curses.KEY_DOWN and direction != (-1, 0):
            direction = (1, 0)
        elif key == curses.KEY_LEFT and direction != (0, 1):
            direction = (0, -1)
        elif key == curses.KEY_RIGHT and direction != (0, -1):
            direction = (0, 1)
        elif key == ord('p') or key == ord('P'):  # 暂停
            stdscr.timeout(-1)
            stdscr.addstr(h - 2, 0, "已暂停，按任意键继续...")
            stdscr.refresh()
            stdscr.getch()
            stdscr.nodelay(True)
            stdscr.timeout(100)

        # ---- 移动蛇 ----
        ny, nx = snake[-1][0] + direction[0], snake[-1][1] + direction[1]

        # 撞墙或撞自己 → 游戏结束
        if (ny <= 0 or ny >= game_h or nx <= 0 or nx >= game_w
                or (ny, nx) in snake):
            return score

        snake.append((ny, nx))

        if (ny, nx) == food:  # 吃到食物
            score += 10
            food = spawn_food(snake, game_h, game_w)
        else:  # 没吃到，去掉尾巴
            snake.pop(0)

        # ---- 渲染 ----
        stdscr.clear()
        draw_border(stdscr, game_h, game_w)
        for y, x in snake:
            ch = '@' if (y, x) == snake[-1] else 'o'
            stdscr.addch(y, x, ch)
        stdscr.addch(food[0], food[1], '*')
        stdscr.addstr(h - 2, 0, f" 得分: {score}   蛇长: {len(snake)}   "
                               f"[方向键]移动  [P]暂停  [Q]退出")
        stdscr.refresh()

    return score


def main():
    stdscr = curses.initscr()
    try:
        curses.noecho()
        curses.cbreak()
        while True:
            result = run_game(stdscr)
            if result is None:      # 终端太小，无法游玩
                break
            score = result
            if not game_over_screen(stdscr, score):   # 选择退出
                break
    finally:
        curses.nocbreak()
        curses.echo()
        curses.endwin()
    print(f"感谢游玩！最终得分: {score}")


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n再见！")
        sys.exit(0)
