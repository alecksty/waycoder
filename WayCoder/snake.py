"""贪吃蛇小游戏 —— 用标准库 tkinter 实现，无需安装任何第三方依赖。

运行:  python snake.py
操作:  方向键 或 WASD 控制，空格 暂停/继续，R 重新开始
"""
import random
import tkinter as tk

# ============ 配置 ============
GRID = 20          # 网格数（20x20）
CELL = 25          # 每格像素
WIDTH = GRID * CELL
HEIGHT = GRID * CELL
SPEED = 150        # 初始速度（毫秒/步）

COL_BG = "#1e1e2e"
COL_GRID = "#2a2a3e"
COL_SNAKE = "#4ade80"
COL_SNAKE_HEAD = "#22c55e"
COL_FOOD = "#f87171"
COL_TEXT = "#e5e7eb"

DIRS = {
    "up": (0, -1), "down": (0, 1),
    "left": (-1, 0), "right": (1, 0),
}
OPPOSITE = {"up": "down", "down": "up", "left": "right", "right": "left"}
KEYS = {
    "Up": "up", "Down": "down", "Left": "left", "Right": "right",
    "w": "up", "s": "down", "a": "left", "d": "right",
    "W": "up", "S": "down", "A": "left", "D": "right",
}


class SnakeGame:
    def __init__(self):
        self.root = tk.Tk()
        self.root.title("贪吃蛇 🐍")
        self.root.resizable(False, False)

        self.canvas = tk.Canvas(self.root, width=WIDTH, height=HEIGHT,
                                bg=COL_BG, highlightthickness=0)
        self.canvas.pack()

        self.root.bind("<KeyPress>", self.on_key)
        self.reset()
        self.root.after(SPEED, self.step)

    # ---------- 游戏状态 ----------
    def reset(self):
        mid = GRID // 2
        self.snake = [(mid, mid), (mid - 1, mid), (mid - 2, mid)]
        self.dir = "right"
        self.next_dir = "right"
        self.score = 0
        self.running = True
        self.game_over = False
        self.spawn_food()
        self.draw()

    def spawn_food(self):
        while True:
            p = (random.randrange(GRID), random.randrange(GRID))
            if p not in self.snake:
                self.food = p
                return

    # ---------- 主循环 ----------
    def step(self):
        if not self.game_over:
            if self.running:
                self.tick()
                self.draw()
        self.root.after(SPEED, self.step)

    def tick(self):
        self.dir = self.next_dir
        dx, dy = DIRS[self.dir]
        head = self.snake[0]
        new_head = ((head[0] + dx) % GRID, (head[1] + dy) % GRID)  # 穿墙

        if new_head == self.food:
            self.snake.insert(0, new_head)
            self.score += 1
            self.spawn_food()
        else:
            self.snake.insert(0, new_head)
            self.snake.pop()

        # 撞到自己（新头之外的身体）则结束
        if new_head in self.snake[1:]:
            self.game_over = True

    # ---------- 绘制 ----------
    def draw(self):
        self.canvas.delete("all")

        # 网格
        for i in range(GRID + 1):
            self.canvas.create_line(i * CELL, 0, i * CELL, HEIGHT,
                                    fill=COL_GRID)
            self.canvas.create_line(0, i * CELL, WIDTH, i * CELL,
                                    fill=COL_GRID)

        # 食物
        fx, fy = self.food
        self.canvas.create_oval(fx * CELL + 3, fy * CELL + 3,
                                fx * CELL + CELL - 3, fy * CELL + CELL - 3,
                                fill=COL_FOOD, outline="")

        # 蛇
        for i, (x, y) in enumerate(self.snake):
            color = COL_SNAKE_HEAD if i == 0 else COL_SNAKE
            pad = 1 if i == 0 else 2
            self.canvas.create_rectangle(
                x * CELL + pad, y * CELL + pad,
                x * CELL + CELL - pad, y * CELL + CELL - pad,
                fill=color, outline="")

        # 文字
        if self.game_over:
            self.text(f"游戏结束  得分: {self.score}", "#f87171")
            self.text("按 R 重新开始", COL_TEXT, y_offset=40)
        elif not self.running:
            self.text("暂停中  按空格继续", "#facc15")

        self.canvas.create_text(10, 10, anchor="nw",
                                text=f"得分: {self.score}", fill=COL_TEXT,
                                font=("Consolas", 14, "bold"))

    def text(self, msg, color, y_offset=0):
        self.canvas.create_text(WIDTH // 2, HEIGHT // 2 + y_offset,
                                text=msg, fill=color,
                                font=("Consolas", 20, "bold"))

    # ---------- 输入 ----------
    def on_key(self, event):
        key = event.keysym if event.keysym in KEYS else event.char
        if key in KEYS:
            d = KEYS[key]
            if d != OPPOSITE[self.dir]:
                self.next_dir = d
        elif key == "space":
            self.running = not self.running
        elif key in ("r", "R"):
            self.reset()

    def run(self):
        self.root.mainloop()


if __name__ == "__main__":
    SnakeGame().run()
