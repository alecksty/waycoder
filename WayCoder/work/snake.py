#!/usr/bin/env python3
"""贪吃蛇小游戏 — 纯标准库实现 (curses)
操作：方向键 / WASD 控制方向，空格暂停，Q 退出
"""
import curses
import random
import time

# 游戏区域尺寸
WIDTH, HEIGHT = 40, 20

# 方向键映射
KEYS = {
    ord('w'): (0, -1), ord('W'): (0, -1), curses.KEY_UP: (0, -1),
    ord('s'): (0, 1),  ord('S'): (0, 1),  curses.KEY_DOWN: (0, 1),
    ord('a'): (-1, 0), ord('A'): (-1, 0), curses.KEY_LEFT: (-1, 0),
    ord('d'): (1, 0),  ord('D'): (1, 0),  curses.KEY_RIGHT: (1, 0),
}


def draw_border(stdscr):
    """绘制边界"""
    stdscr.border(0)


def spawn_food(snake):
    """随机生成食物，避免与蛇身重叠"""
    while True:
        food = (random.randint(1, WIDTH - 2), random.randint(1, HEIGHT - 2))
        if food not in snake:
            return food


def game_loop(stdscr):
    # 初始化
    curses.curs_set(0)                      # 隐藏光标
    stdscr.nodelay(True)                    # 非阻塞输入
    stdscr.timeout(100)

    snake = [(WIDTH // 2, HEIGHT // 2)]     # 蛇身（头在前）
    direction = (1, 0)                      # 初始向右
    food = spawn_food(snake)
    score = 0
    paused = False
    game_over = False

    while True:
        stdscr.clear()
        draw_border(stdscr)

        # 标题与分数
        stdscr.addstr(0, 2, " SNAKE ")
        stdscr.addstr(0, WIDTH - 12, f" SCORE: {score} ")
        if paused:
            stdscr.addstr(HEIGHT // 2, WIDTH // 2 - 5, "PAUSED")

        # 绘制食物
        stdscr.addch(food[1], food[0], '@')

        # 绘制蛇（头用 O，身体用 o）
        for i, (x, y) in enumerate(snake):
            ch = 'O' if i == 0 else 'o'
            try:
                stdscr.addch(y, x, ch)
            except curses.error:
                pass

        # 游戏结束提示
        if game_over:
            msg = f"GAME OVER! SCORE: {score}  (Q 退出 / R 重玩)"
            stdscr.addstr(HEIGHT // 2, max(0, WIDTH // 2 - len(msg) // 2), msg)
        stdscr.refresh()

        # ---- 输入处理 ----
        key = stdscr.getch()
        if key == ord('q') or key == ord('Q'):
            break
        if key == ord('r') or key == ord('R'):
            return game_loop(stdscr)        # 重玩
        if key == ord(' '):
            paused = not paused

        if game_over or paused:
            continue

        if key in KEYS:
            new_dir = KEYS[key]
            # 禁止直接反向
            if (new_dir[0] != -direction[0] or new_dir[1] != -direction[1]):
                direction = new_dir

        # ---- 移动 ----
        head_x, head_y = snake[0]
        new_head = (head_x + direction[0], head_y + direction[1])

        # 撞墙检测
        if not (1 <= new_head[0] < WIDTH - 1 and 1 <= new_head[1] < HEIGHT - 1):
            game_over = True
            continue

        # 撞自己检测
        if new_head in snake:
            game_over = True
            continue

        snake.insert(0, new_head)

        # 吃到食物
        if new_head == food:
            score += 10
            food = spawn_food(snake)
            # 随分数加速
            stdscr.timeout(max(40, 100 - score // 2))
        else:
            snake.pop()

        time.sleep(0.001)


def main():
    curses.wrapper(game_loop)
    print("谢谢游玩！")


if __name__ == "__main__":
    main()
