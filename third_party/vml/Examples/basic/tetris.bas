' tetris.bas —— 俄罗斯方块（VML 的 BASIC 前端）
'
' 界面与几何都走**共享调用库** Lib/shared/src/vmlui.c（编成 vmlui.vml，由 vmltool.config.xml
' 挂到 basic 的 Libs 上）。BASIC 声明外部库函数用 `NATIVE SUB/FUNCTION` —— 只有带 NATIVE
' 才是**裸标签**（否则会被加上 sub_/func_ 前缀，链接期找不到）。
'
' 几条实测出来的前端限制，本程序是绕开它们写的：
'   1. **数组不可靠**：`DIM a(10)` 之后 `a(5)` 读出来是野值 ⇒ 棋盘不用 BASIC 数组，
'      改用共享库的整数网格 ui_gset/ui_gget（C 侧的真实数组）。
'   2. **没有移位/取位**（只有 AND/OR）⇒ 方块旋转不在 BASIC 里算，
'      用 ui_piece_cell(pid, rot, which) 直接问 C 要格子的坐标。
'   3. 外部函数必须 NATIVE 声明，且**要写空体**（NATIVE ... / END SUB）。
'
' 操作（触摸）：点屏幕左 1/3 左移、右 1/3 右移、中间**旋转**；点最下面那条**直落到底**。
' 返回箭头退出。
'
' ⚠⚠ **本文件目前跑不起来** —— 卡在 BASIC 前端自身，不在共享库这一层。实测（见 CHANGELOG）：
'   · `\`（整除）**不生效**：`c = 100 \ 20` 得到的 c 是 100（`\ b` 整段被丢），
'     于是本程序里所有 `\ BH` / `\ 16` 的分母变成 0 → 运行时"整数除零"；
'   · `INT()` 解析成库函数 `basic_int`，而它**没有链进来**（未找到标签）；
'   · 数组不可靠（`arr(2)=7` 之后读回 65556），本程序已改走共享库网格绕开。
'
' 保留下来的价值：① 它记下了"BASIC 调 C 共享库"的**正确写法**
' （`NATIVE SUB/FUNCTION` + 空体 = 裸标签，其余写法会被加上 sub_/func_ 前缀而在链接期找不到）；
' ② 它记下了踩到的三个前端缺陷与绕法。等 BASIC 前端补齐后，这份应当能直接跑。

' ── 外部库声明（全部 NATIVE：裸标签）────────────────────
NATIVE FUNCTION ui_win_open(t AS STRING, w AS INTEGER, h AS INTEGER) AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_win_closed() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_scr_w() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_scr_h() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_wait_msg(ms AS INTEGER) AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_msg_a() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_msg_b() AS INTEGER
END FUNCTION
NATIVE SUB ui_win_close()
END SUB
NATIVE SUB ui_clear(c AS INTEGER)
END SUB
NATIVE SUB ui_rect(x AS INTEGER, y AS INTEGER, w AS INTEGER, h AS INTEGER, c AS INTEGER, f AS INTEGER, lw AS INTEGER, r AS INTEGER)
END SUB
NATIVE SUB ui_line(x1 AS INTEGER, y1 AS INTEGER, x2 AS INTEGER, y2 AS INTEGER, c AS INTEGER, lw AS INTEGER)
END SUB
NATIVE SUB ui_set_font(size AS INTEGER, style AS INTEGER, c AS INTEGER, anchor AS INTEGER)
END SUB
NATIVE SUB ui_text_cur(x AS INTEGER, y AS INTEGER, s AS STRING)
END SUB
NATIVE SUB ui_present()
END SUB
NATIVE SUB ui_gclear()
END SUB
NATIVE SUB ui_gset(idx AS INTEGER, v AS INTEGER)
END SUB
NATIVE FUNCTION ui_gget(idx AS INTEGER) AS INTEGER
END FUNCTION
NATIVE SUB ui_piece_init()
END SUB
NATIVE FUNCTION ui_piece_cell(pid AS INTEGER, rot AS INTEGER, which AS INTEGER) AS INTEGER
END FUNCTION

' ── 常量与全局变量 ─────────────────────────────────────
' ⚠ **DIM 必须排在赋值之前**：BASIC 里 `BW = 10` 写在 `DIM BW AS INTEGER` 之前，
' 那个赋值不会落到整型变量上（实测现象是后面 `\ BH` 直接整数除零）。
DIM BW AS INTEGER
DIM BH AS INTEGER
DIM TICK AS INTEGER
DIM C_BG AS INTEGER
DIM C_GRID AS INTEGER
DIM C_WALL AS INTEGER
DIM C_TEXT AS INTEGER
DIM C_SCORE AS INTEGER
DIM C_BLOCK AS INTEGER
DIM C_CUR AS INTEGER
DIM score AS INTEGER
DIM over AS INTEGER
DIM pid AS INTEGER
DIM rot AS INTEGER
DIM px AS INTEGER
DIM py AS INTEGER
DIM sw AS INTEGER
DIM sh AS INTEGER
DIM t AS INTEGER
DIM tx AS INTEGER
DIM ty AS INTEGER
DIM i AS INTEGER
DIM j AS INTEGER
DIM k AS INTEGER
DIM v AS INTEGER
DIM cell AS INTEGER
DIM ox AS INTEGER
DIM oy AS INTEGER
DIM bwid AS INTEGER
DIM bhei AS INTEGER
DIM full AS INTEGER
DIM hit AS INTEGER
DIM cx AS INTEGER
DIM cy AS INTEGER
DIM packed AS INTEGER

BW = 10
BH = 20
TICK = 450

C_BG = -16120744            ' 0xFF101018
C_GRID = -14009032          ' 0xFF2A2A38
C_WALL = -12961116          ' 0xFF3A3A4C
C_TEXT = -1184270           ' 0xFFEDEDF2   （负数就是 0xAARRGGBB 的有符号读法）
C_SCORE = -11842944         ' 0xFF4ADE80
C_BLOCK = -10975745         ' 0xFF58A6FF
C_CUR = -996807             ' 0xFFF0B429

' ── 棋盘读写（棋盘在共享库网格里；越界当"墙"）────────────
FUNCTION bget(x AS INTEGER, y AS INTEGER) AS INTEGER
    IF x < 0 OR x >= BW OR y < 0 OR y >= BH THEN
        bget = 1
    ELSE
        bget = ui_gget(y * BW + x)
    END IF
END FUNCTION

SUB bset(x AS INTEGER, y AS INTEGER, v AS INTEGER)
    IF x >= 0 AND x < BW AND y >= 0 AND y < BH THEN
        ui_gset(y * BW + x, v)
    END IF
END SUB

SUB board_clear()
    DIM n AS INTEGER
    n = 0
    WHILE n < BW * BH
        ui_gset(n, 0)
        n = n + 1
    WEND
END SUB

' 方块当前落点是否与墙/已有块冲突
FUNCTION collide(p AS INTEGER, rr AS INTEGER, fx AS INTEGER, fy AS INTEGER) AS INTEGER
    DIM q AS INTEGER
    DIM pk AS INTEGER
    DIM gx AS INTEGER
    DIM gy AS INTEGER
    collide = 0
    q = 0
    WHILE q < 4
        pk = ui_piece_cell(p, rr, q)
        IF pk >= 0 THEN
            gx = fx + pk \ 16
            gy = fy + (pk - (pk \ 16) * 16)
            IF bget(gx, gy) <> 0 THEN
                collide = 1
            END IF
        END IF
        q = q + 1
    WEND
END FUNCTION

SUB lock_piece(p AS INTEGER, rr AS INTEGER, fx AS INTEGER, fy AS INTEGER)
    DIM q AS INTEGER
    DIM pk AS INTEGER
    q = 0
    WHILE q < 4
        pk = ui_piece_cell(p, rr, q)
        IF pk >= 0 THEN
            bset(fx + pk \ 16, fy + (pk - (pk \ 16) * 16), 1)
        END IF
        q = q + 1
    WEND
END SUB

' 消行，返回消掉的行数
FUNCTION clear_lines() AS INTEGER
    DIM n AS INTEGER
    DIM yy AS INTEGER
    DIM xx AS INTEGER
    DIM allf AS INTEGER
    n = 0
    yy = BH - 1
    WHILE yy >= 0
        allf = 1
        xx = 0
        WHILE xx < BW
            IF bget(xx, yy) = 0 THEN
                allf = 0
            END IF
            xx = xx + 1
        WEND
        IF allf = 1 THEN
            n = n + 1
            DIM mv AS INTEGER
            mv = yy
            WHILE mv > 0
                xx = 0
                WHILE xx < BW
                    bset(xx, mv, bget(xx, mv - 1))
                    xx = xx + 1
                WEND
                mv = mv - 1
            WEND
            xx = 0
            WHILE xx < BW
                bset(xx, 0, 0)
                xx = xx + 1
            WEND
        ELSE
            yy = yy - 1
        END IF
    WEND
    clear_lines = n
END FUNCTION

' ── 绘制 ──────────────────────────────────────────────
SUB draw_board()
    sw = ui_scr_w()
    sh = ui_scr_h()
    IF sw <= 0 THEN
        sw = 360
    END IF
    IF sh <= 0 THEN
        sh = 620
    END IF

    IF (sh - 70) / BH < (sw - 20) / BW THEN
        cell = (sh - 70) \ BH
    ELSE
        cell = (sw - 20) \ BW
    END IF
    IF cell < 4 THEN
        cell = 4
    END IF

    bwid = cell * BW
    bhei = cell * BH
    ox = (sw - bwid) \ 2
    oy = 44

    ui_clear(C_BG)
    ui_rect(ox, oy, bwid, bhei, C_WALL, 1, 0, 4)

    i = 1
    WHILE i < BW
        ui_line(ox + i * cell, oy, ox + i * cell, oy + bhei, C_GRID, 1)
        i = i + 1
    WEND
    i = 1
    WHILE i < BH
        ui_line(ox, oy + i * cell, ox + bwid, oy + i * cell, C_GRID, 1)
        i = i + 1
    WEND

    ' 已固定的块
    j = 0
    WHILE j < BH
        i = 0
        WHILE i < BW
            IF bget(i, j) <> 0 THEN
                ui_rect(ox + i * cell + 1, oy + j * cell + 1, cell - 2, cell - 2, C_BLOCK, 1, 0, 2)
            END IF
            i = i + 1
        WEND
        j = j + 1
    WEND

    ' 当前方块
    k = 0
    WHILE k < 4
        packed = ui_piece_cell(pid, rot, k)
        IF packed >= 0 THEN
            cx = ox + (px + packed \ 16) * cell + 1
            cy = oy + (py + (packed - (packed \ 16) * 16)) * cell + 1
            ui_rect(cx, cy, cell - 2, cell - 2, C_CUR, 1, 0, 2)
        END IF
        k = k + 1
    WEND

    ui_set_font(15, 1, C_TEXT, 0)
    ui_text_cur(ox, 12, "俄罗斯方块(BASIC)")
    ui_set_font(14, 0, C_SCORE, 2)
    ui_text_cur(ox + bwid, 12, STR$(score))

    IF over = 1 THEN
        ui_set_font(20, 3, -1146901, 0)
        ui_text_cur(ox + bwid \ 2, oy + bhei \ 2, "游戏结束")
        ui_set_font(14, 0, C_TEXT, 0)
        ui_text_cur(ox + bwid \ 2, oy + bhei \ 2 + 28, "点任意处重开")
    END IF

    ui_present()
END SUB

' ── 主程序 ────────────────────────────────────────────
ui_piece_init()
board_clear()

score = 0
over = 0
pid = 0
rot = 0
px = 3
py = 0

ui_win_open("俄罗斯方块", ui_scr_w(), ui_scr_h())
draw_board()

WHILE ui_win_closed() = 0
    t = ui_wait_msg(TICK)

    IF t = 10 THEN
        EXIT WHILE
    END IF

    IF t = 6 THEN
        tx = ui_msg_a()
        ty = ui_msg_b()
        sw = ui_scr_w()
        sh = ui_scr_h()

        IF over = 1 THEN
            board_clear()
            score = 0
            over = 0
            pid = 0
            rot = 0
            px = 3
            py = 0
        ELSEIF ty > sh * 3 \ 4 THEN
            WHILE collide(pid, rot, px, py + 1) = 0
                py = py + 1
            WEND
            lock_piece(pid, rot, px, py)
            score = score + clear_lines() * 100
            pid = (pid + 1) MOD 7
            rot = 0
            px = 3
            py = 0
            IF collide(pid, rot, px, py) <> 0 THEN
                over = 1
            END IF
        ELSEIF tx < sw \ 3 THEN
            IF collide(pid, rot, px - 1, py) = 0 THEN
                px = px - 1
            END IF
        ELSEIF tx > sw * 2 \ 3 THEN
            IF collide(pid, rot, px + 1, py) = 0 THEN
                px = px + 1
            END IF
        ELSE
            IF collide(pid, rot + 1, px, py) = 0 THEN
                rot = (rot + 1) MOD 4
            END IF
        END IF
    ELSE
        ' 超时（或其它消息）＝ 一次自然下落
        IF over = 0 THEN
            IF collide(pid, rot, px, py + 1) = 0 THEN
                py = py + 1
            ELSE
                lock_piece(pid, rot, px, py)
                score = score + clear_lines() * 100
                pid = (pid + 1) MOD 7
                rot = 0
                px = 3
                py = 0
                IF collide(pid, rot, px, py) <> 0 THEN
                    over = 1
                END IF
            END IF
        END IF
    END IF

    draw_board()
WEND

ui_win_close()
