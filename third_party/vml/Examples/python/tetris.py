# tetris.py —— 俄罗斯方块（VML 的 Python 前端）
#
# 界面走**共享调用库** `Lib/shared/vmlui.c`（编成 `vmlui.vml`，由 vmltool.config.xml 挂到
# python 的 Libs 上）：Python 前端不支持内联 asm()，只能按标签调 C 函数 —— 这正是那座共享库
# 存在的意义。
#
# ⚠ 两条**实测出来的前端限制**，本程序是绕开它们写的：
#   1. **列表不能写**：`b[i] = v` 之后读回来还是 0（字面量列表读是对的，写不生效），
#      嵌套列表也错。所以棋盘不放 Python 里，改用共享库的整数网格 `ui_gset/ui_gget`。
#   2. 没有 asm()（`PythonCompiler/CodeGenerator.Expressions.cs:154` 注明仅 C/ObjC/C++）。
#
# 操作（触摸）：点屏幕左 1/3 左移、右 1/3 右移、中间**旋转**；点最下面那条**直落到底**。
# 返回箭头退出。

# ── 常量 ───────────────────────────────────────────────
W = 10
H = 20
TICK = 450          # 下落一格的间隔（毫秒）；ui_wait_msg 超时即视为一次下落

C_BG = 0xFF101018
C_GRID = 0xFF2A2A38
C_WALL = 0xFF3A3A4C
C_TEXT = 0xFFEDEDF2
C_SCORE = 0xFF4ADE80

# 7 种方块：4×4 位掩码（行优先，第 r 行第 c 位 = r*4+c）
# 旋转用位运算现场算，不存 28 张表（前端列表不可靠，少存一份少一个坑）
P_O = 0x0066
P_I = 0x0F00
P_S = 0x006C
P_Z = 0x00C6
P_T = 0x00E4
P_L = 0x0062
P_J = 0x00E8

# 棋盘/掩码在共享库网格里的布局
BOARD_AT = 0
MASK_AT = 200

# ── 共享库网格上的棋盘读写 ──────────────────────────────
def bget(x, y):
    if x < 0 or x >= W or y < 0 or y >= H:
        return 1              # 墙：越界一律当"有块"，碰撞判定就不用再判边界
    return ui_gget(BOARD_AT + y * W + x)

def bset(x, y, v):
    if x >= 0 and x < W and y >= 0 and y < H:
        ui_gset(BOARD_AT + y * W + x, v)

def board_clear():
    i = 0
    while i < W * H:
        ui_gset(BOARD_AT + i, 0)
        i = i + 1

# ── 方块：某块的某次旋转，取它的 4 个格子的坐标 ──────────
# 旋转 4×4 掩码 90°：位 (r,c) → (c, 3-r)
def rot90(m):
    out = 0
    r = 0
    while r < 4:
        c = 0
        while c < 4:
            if (m >> (r * 4 + c)) & 1:
                out = out | (1 << (c * 4 + (3 - r)))
            c = c + 1
        r = r + 1
    return out

def piece_mask(pid, rot):
    m = ui_gget(MASK_AT + pid)
    k = 0
    while k < rot:
        m = rot90(m)
        k = k + 1
    return m

# 判断某块在 (px,py) 处是否与已有块/墙冲突
def collide(pid, rot, px, py):
    m = piece_mask(pid, rot)
    r = 0
    while r < 4:
        c = 0
        while c < 4:
            if (m >> (r * 4 + c)) & 1:
                if bget(px + c, py + r) != 0:
                    return 1
            c = c + 1
        r = r + 1
    return 0

# 把方块固定进棋盘
def lock_piece(pid, rot, px, py):
    m = piece_mask(pid, rot)
    r = 0
    while r < 4:
        c = 0
        while c < 4:
            if (m >> (r * 4 + c)) & 1:
                bset(px + c, py + r, 1)
            c = c + 1
        r = r + 1

# 消行：返回消掉的行数
def clear_lines():
    n = 0
    y = H - 1
    while y >= 0:
        full = 1
        x = 0
        while x < W:
            if bget(x, y) == 0:
                full = 0
            x = x + 1
        if full == 1:
            n = n + 1
            # 上面的整体下移一行
            yy = y
            while yy > 0:
                x = 0
                while x < W:
                    bset(x, yy, bget(x, yy - 1))
                    x = x + 1
                yy = yy - 1
            x = 0
            while x < W:
                bset(x, 0, 0)
                x = x + 1
            # 就地重判同一行（新落下来的那行）
        else:
            y = y - 1
    return n

# ── 绘制 ──────────────────────────────────────────────
def draw(px, py, rot, pid, score, over):
    sw = ui_scr_w()
    sh = ui_scr_h()
    if sw <= 0:
        sw = 360
    if sh <= 0:
        sh = 620

    # 棋盘按可用高度铺满，宽度按 1:2 比例
    area = sh - 70
    cell = area / H
    if cell * W > sw - 20:
        cell = (sw - 20) / W
    bw = cell * W
    bh = cell * H
    ox = (sw - bw) / 2
    oy = 44

    ui_clear(C_BG)

    # 棋盘底
    ui_rect(ox, oy, bw, bh, C_WALL, 1, 0, 4)

    # 网格
    i = 1
    while i < W:
        ui_line(ox + i * cell, oy, ox + i * cell, oy + bh, C_GRID, 1)
        i = i + 1
    i = 1
    while i < H:
        ui_line(ox, oy + i * cell, ox + bw, oy + i * cell, C_GRID, 1)
        i = i + 1

    # 已固定的块
    y = 0
    while y < H:
        x = 0
        while x < W:
            if bget(x, y) != 0:
                ui_rect(ox + x * cell + 1, oy + y * cell + 1, cell - 2, cell - 2, 0xFF58A6FF, 1, 0, 2)
            x = x + 1
        y = y + 1

    # 当前方块
    m = piece_mask(pid, rot)
    r = 0
    while r < 4:
        c = 0
        while c < 4:
            if (m >> (r * 4 + c)) & 1:
                ui_rect(ox + (px + c) * cell + 1, oy + (py + r) * cell + 1, cell - 2, cell - 2, 0xFFF0B429, 1, 0, 2)
            c = c + 1
        r = r + 1

    # 状态行
    ui_set_font(15, 1, C_TEXT, 0)
    ui_text_cur(ox, 12, "俄罗斯方块")
    ui_set_font(14, 0, C_SCORE, 2)
    ui_text_cur(ox + bw, 12, str(score))

    if over == 1:
        ui_set_font(20, 3, 0xFFFF6B6B, 0)
        ui_text_cur(ox + bw / 2, oy + bh / 2, "游戏结束")
        ui_set_font(14, 0, C_TEXT, 0)
        ui_text_cur(ox + bw / 2, oy + bh / 2 + 28, "点任意处重开")

    ui_present()

# ── 主程序 ────────────────────────────────────────────
def main():
    # 把 7 种方块的基准掩码放进共享库网格（前端列表不可靠，数据也放这边）
    ui_gset(MASK_AT + 0, P_O)
    ui_gset(MASK_AT + 1, P_I)
    ui_gset(MASK_AT + 2, P_S)
    ui_gset(MASK_AT + 3, P_Z)
    ui_gset(MASK_AT + 4, P_T)
    ui_gset(MASK_AT + 5, P_L)
    ui_gset(MASK_AT + 6, P_J)

    ui_win_open("俄罗斯方块", ui_scr_w(), ui_scr_h())

    board_clear()

    score = 0
    over = 0
    pid = 0
    rot = 0
    px = 3
    py = 0

    ui_dlg_msg("俄罗斯方块", "点左边左移、右边右移、中间旋转；点最下面直落。", 0)

    draw(px, py, rot, pid, score, over)

    while ui_win_closed() == 0:
        t = ui_wait_msg(TICK)

        if t == 10:            # WindowClose
            break

        if t == 6:             # TouchDown
            tx = ui_msg_a()
            ty = ui_msg_b()
            sw = ui_scr_w()
            sh = ui_scr_h()

            if over == 1:
                # 重开
                board_clear()
                score = 0
                over = 0
                pid = 0
                rot = 0
                px = 3
                py = 0
            elif ty > sh * 3 / 4:
                # 直落：一直往下直到碰住
                while collide(pid, rot, px, py + 1) == 0:
                    py = py + 1
                lock_piece(pid, rot, px, py)
                score = score + clear_lines() * 100
                pid = (pid + 1) % 7
                rot = 0
                px = 3
                py = 0
                if collide(pid, rot, px, py) != 0:
                    over = 1
            elif tx < sw / 3:
                if collide(pid, rot, px - 1, py) == 0:
                    px = px - 1
            elif tx > sw * 2 / 3:
                if collide(pid, rot, px + 1, py) == 0:
                    px = px + 1
            else:
                nr = (rot + 1) % 4
                if collide(pid, nr, px, py) == 0:
                    rot = nr

        else:
            # 超时（或其它消息）＝ 一次自然下落
            if over == 0:
                if collide(pid, rot, px, py + 1) == 0:
                    py = py + 1
                else:
                    lock_piece(pid, rot, px, py)
                    score = score + clear_lines() * 100
                    pid = (pid + 1) % 7
                    rot = 0
                    px = 3
                    py = 0
                    if collide(pid, rot, px, py) != 0:
                        over = 1

        draw(px, py, rot, pid, score, over)

    ui_win_close()

main()
