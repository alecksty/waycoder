# -*- coding: utf-8 -*-
"""终端版俄罗斯方块（零依赖，标准库实现）"""
import json
import os
import random
import sys
import threading
import time

try:
    import msvcrt          # Windows 按键
except ImportError:
    msvcrt = None

SCORE_FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "tetris_highscore.json")

# 俄罗斯方块标准七种形状
SHAPES = [
    [[1, 1, 1, 1]],                                            # I
    [[1, 1], [1, 1]],                                          # O
    [[0, 1, 0], [1, 1, 1]],                                    # T
    [[1, 0, 0], [1, 1, 1]],                                    # L
    [[0, 0, 1], [1, 1, 1]],                                    # J
    [[0, 1, 1], [1, 1, 0]],                                    # S
    [[1, 1, 0], [0, 1, 1]],                                    # Z
]

COLS, ROWS = 10, 20
CELL = "██"
EMPTY = "  "

COLORS = ["\033[91m", "\033[96m", "\033[95m", "\033[93m",
          "\033[92m", "\033[94m", "\033[91m"]  # 每种方块的颜色
RESET = "\033[0m"


class Tetris:
    def __init__(self):
        self.board = [[0] * COLS for _ in range(ROWS)]
        self.score = 0
        self.level = 1
        self.high_score = load_high_score()
        self.new_record = False
        self.cur = self.new_piece()
        self.next = self.new_piece()
        self.drop_interval = 1.0

    def new_piece(self):
        idx = random.randrange(len(SHAPES))
        return {"shape": SHAPES[idx], "x": COLS // 2 - len(SHAPES[idx][0]) // 2,
                "y": 0, "color": idx}

    def valid(self, shape, x, y):
        for r, row in enumerate(shape):
            for c, cell in enumerate(row):
                if cell:
                    nx, ny = x + c, y + r
                    if nx < 0 or nx >= COLS or ny >= ROWS:
                        return False
                    if ny >= 0 and self.board[ny][nx]:
                        return False
        return True

    def rotate(self):
        s = self.cur["shape"]
        r = [list(row) for row in zip(*s[::-1])]  # 顺时针旋转
        return r

    def lock(self):
        for r, row in enumerate(self.cur["shape"]):
            for c, cell in enumerate(row):
                if cell:
                    y, x = self.cur["y"] + r, self.cur["x"] + c
                    if y < 0:
                        return False                      # 顶部溢出，游戏结束
                    self.board[y][x] = self.cur["color"] + 1
        self.clear_lines()
        self.cur, self.next = self.next, self.new_piece()
        return True

    def clear_lines(self):
        remaining = [row for row in self.board if any(v == 0 for v in row)]
        cleared = ROWS - len(remaining)
        self.board = [[0] * COLS for _ in range(cleared)] + remaining
        if cleared:
            play_sfx("clear")
            self.score += [0, 100, 300, 500, 800][cleared] * self.level
            self.level = self.score // 500 + 1
            self.drop_interval = max(0.1, 1.0 - (self.level - 1) * 0.1)
            if self.score > self.high_score:
                self.high_score = self.score
                self.new_record = True

    def draw(self):
        os.system("cls" if os.name == "nt" else "clear")
        top = "┌" + "─" * (COLS * 2) + "┐"
        bottom = "└" + "─" * (COLS * 2) + "┘"
        print(top)
        for r in range(ROWS):
            line = "│"
            for c in range(COLS):
                v = self.board[r][c]
                if v:
                    line += COLORS[v - 1] + CELL + RESET
                else:
                    line += EMPTY
            print(line + "│")
        print(bottom)
        print(f"分数: {self.score}    等级: {self.level}    最高分: {self.high_score}")
        if self.new_record:
            print("\033[93m★ 新纪录！ ★\033[0m")

        # 预览下一个方块
        print("\n下一个:")
        for row in self.next["shape"]:
            print("  " + "".join(COLORS[self.next["color"]] + CELL + RESET
                                 if v else EMPTY for v in row))
        print("\n← → 移动   ↑ 旋转   ↓ 加速   空格 硬降   P 暂停   B 音乐   Q 退出")


def get_key():
    if msvcrt is None:
        return ""
    if not msvcrt.kbhit():
        return ""
    return msvcrt.getwch()


# ---------- 最高分记录 ----------
def load_high_score():
    try:
        with open(SCORE_FILE, "r", encoding="utf-8") as f:
            return int(json.load(f).get("high_score", 0))
    except Exception:
        return 0


def save_high_score(score):
    try:
        with open(SCORE_FILE, "w", encoding="utf-8") as f:
            json.dump({"high_score": score}, f)
    except OSError:
        pass


# ---------- 背景音乐（winsound 后台线程播放旋律） ----------
try:
    import winsound          # 仅 Windows 可用
except ImportError:
    winsound = None

# 《货郎》(Korobeiniki) 经典旋律片段: (频率Hz, 时长ms)，0 表示休止
MELODY = [
    (659, 160), (494, 160), (523, 160), (587, 160), (523, 160), (494, 160), (440, 160),
    (440, 160), (523, 160), (659, 160), (587, 160), (523, 160), (494, 160),
    (494, 160), (523, 160), (587, 160), (659, 160), (523, 160), (440, 160),
    (440, 240), (0, 160),
]

# 音效开关（与音乐开关联动，由 main 设置）
sfx_enabled = True
_beep_lock = threading.Lock()


def _beep(freq, dur):
    """线程安全的蜂鸣（winsound.Beep 是阻塞的，用锁避免与音乐冲突）"""
    if winsound is None:
        return
    with _beep_lock:
        try:
            winsound.Beep(freq, dur)
        except Exception:
            pass


def play_sfx(kind):
    """播放简短音效: land=落地  clear=消行  hard_drop=硬降"""
    if not sfx_enabled:
        return
    seq = {
        "land":      [(220, 40), (180, 60)],        # 低沉落地声
        "clear":     [(523, 60), (659, 60), (784, 80)],  # 上行琶音
        "hard_drop": [(180, 50), (140, 70)],        # 重击声
    }.get(kind)
    if not seq:
        return
    for freq, dur in seq:
        _beep(freq, dur)


class MusicPlayer:
    def __init__(self):
        self._stop = threading.Event()
        self._thread = None

    def start(self):
        if winsound is None or self._thread and self._thread.is_alive():
            return
        self._stop.clear()
        self._thread = threading.Thread(target=self._play_loop, daemon=True)
        self._thread.start()

    def stop(self):
        self._stop.set()

    def _play_loop(self):
        while not self._stop.is_set():
            for freq, dur in MELODY:
                if self._stop.is_set():
                    return
                if freq:
                    _beep(freq, dur)
                else:
                    time.sleep(dur / 1000.0)


def main():
    game = Tetris()
    music = MusicPlayer()
    music_on = True
    music.start()
    paused = False
    last = time.time()

    def game_over(final_score):
        if final_score > load_high_score():
            save_high_score(final_score)
            print(f"\n🎉 新纪录！最高分: {final_score}")
        else:
            print(f"\n游戏结束！最终分数: {final_score}    最高分: {load_high_score()}")
        music.stop()

    while True:
        key = get_key()
        if key == "q" or key == "Q":
            if game.score > load_high_score():
                save_high_score(game.score)
            print("\n再见！最终分数:", game.score, "最高分:", load_high_score())
            music.stop()
            return
        if key == "p" or key == "P":
            paused = not paused
        elif key in ("b", "B"):
            music_on = not music_on
            sfx_enabled = music_on
            music.start() if music_on else music.stop()
        if not paused:
            if key in ("a", "A", "\xe0K", "\x1b[D"):          # 左移
                if game.valid(game.cur["shape"], game.cur["x"] - 1, game.cur["y"]):
                    game.cur["x"] -= 1
            elif key in ("d", "D", "\xe0M", "\x1b[C"):        # 右移
                if game.valid(game.cur["shape"], game.cur["x"] + 1, game.cur["y"]):
                    game.cur["x"] += 1
            elif key in ("w", "W", "\xe0H", "\x1b[A"):        # 旋转
                r = game.rotate()
                if game.valid(r, game.cur["x"], game.cur["y"]):
                    game.cur["shape"] = r
            elif key in ("s", "S", "\xe0P", "\x1b[B"):        # 加速下落
                if game.valid(game.cur["shape"], game.cur["x"], game.cur["y"] + 1):
                    game.cur["y"] += 1
                    game.score += 1
            elif key == " ":                                  # 硬降：直接落底
                dist = 0
                while game.valid(game.cur["shape"], game.cur["x"], game.cur["y"] + 1):
                    game.cur["y"] += 1
                    dist += 1
                game.score += dist * 2
                play_sfx("hard_drop")
                if not game.lock():
                    game_over(game.score)
                    return
                last = time.time()

            if time.time() - last >= game.drop_interval:
                last = time.time()
                if game.valid(game.cur["shape"], game.cur["x"], game.cur["y"] + 1):
                    game.cur["y"] += 1
                else:
                    play_sfx("land")
                    if not game.lock():
                        game_over(game.score)
                        return

        game.draw()
        time.sleep(0.03)


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n再见！")
        sys.exit(0)
