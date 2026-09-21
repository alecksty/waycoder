' 登月（LUNAR LANDER）—— 用 **BASIC** 写的手机游戏
'
' 出处与版权：原型是 1969 年那版文字登月（后来收进 David Ahl 的《BASIC Computer Games》，
' 前身是 1969 年 7 月登月前后流传的 "Lunar Landing Game"）。**这份是照玩法规则重写的**：
' 结构、变量名、注释、绘制方式都是自己的，没有抄原件的一行代码 ——
' 与 `gorilla.bas` 同一条规矩（`Examples/` 会随 APK 分发）。
'
' 玩法：月面重力下把登月舱放下去。**按住「点火」推进**，左右两个键调姿态角。
' 落地判据与原版一致：**垂直速度够小 + 水平速度够小 + 角度接近直立**，三条都满足才算成功。
' 燃料有限（100 拍），烧完就只能听天由命。
'
' ◆ 手机上怎么玩
'
' 原版是每回合打字输入"烧多少燃料"（离散的），手机上改成**长按点火**（连续），
' 手感更直接、也更好按。左右各一个姿态键，按一次转 15°。
' 注意用 `ui_poll_msg` + **按下/抬起两个事件**维护"键还按着没有" ——
' 只认"按下"的话，拇指按住不放时没有任何后续事件，推进就只生效一帧
' （tetris 的连发定时器踩过同一个坑，见 CLAUDE.md 移动端 ⑰）。
'
' ◆ 写法提醒
'
'   · 这份用**真数组**存地形（`DIM terr(COLS)`，维度直接写 CONST）——
'     数组、动态下标、SUB 内读写、用 CONST 当维度都已确认可用
'     （判据 `scripts/vml-basic-probe`）。
'   · 外部过程必须 `NATIVE SUB` / `NATIVE FUNCTION` + 空体。
'   · **有返回值的必须写 `FUNCTION`**：写成 `SUB … AS INTEGER` 会去链一个 `func_integer`。
'   · `FUNCTION` 必须用 `END FUNCTION` 收尾（写成 `END SUB` 会让整个解析错位）。
'   · 颜色写 `&HFFrrggbb`。

NATIVE SUB ui_clear(c AS INTEGER)
END SUB
NATIVE SUB ui_rect(x AS INTEGER, y AS INTEGER, w AS INTEGER, h AS INTEGER, c AS INTEGER, f AS INTEGER, lw AS INTEGER, r AS INTEGER)
END SUB
NATIVE SUB ui_circle(cx AS INTEGER, cy AS INTEGER, r AS INTEGER, c AS INTEGER, f AS INTEGER, lw AS INTEGER)
END SUB
NATIVE SUB ui_line(x1 AS INTEGER, y1 AS INTEGER, x2 AS INTEGER, y2 AS INTEGER, c AS INTEGER, lw AS INTEGER)
END SUB
NATIVE SUB ui_text(x AS INTEGER, y AS INTEGER, s AS STRING, c AS INTEGER, size AS INTEGER, anchor AS INTEGER)
END SUB
NATIVE SUB ui_present()
END SUB
NATIVE FUNCTION ui_scr_w() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_scr_h() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_win_open_ex(t AS STRING, w AS INTEGER, h AS INTEGER, rot AS INTEGER, pad AS INTEGER) AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_win_closed() AS INTEGER
END FUNCTION
NATIVE SUB ui_win_close()
END SUB
NATIVE FUNCTION ui_wait_msg(timeout AS INTEGER) AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_poll_msg() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_msg_a() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_msg_b() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_rand(n AS INTEGER) AS INTEGER
END FUNCTION
NATIVE SUB ui_beep(freq AS INTEGER, ms AS INTEGER)
END SUB
NATIVE SUB ui_vibrate(ms AS INTEGER, strength AS INTEGER)
END SUB
' ⚠ 形参名不能叫 on —— BASIC 关键字，会把 NATIVE 声明弄坏（实测）
NATIVE SUB ui_keep_on(v AS INTEGER)
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  手感数值（都在这儿，改手感只动这一段）
' ══════════════════════════════════════════════════════════════════════════
CONST NTICK = 40             ' 物理拍（毫秒）
CONST PACE = 40              ' 主循环最长睡眠（把重绘锁在 ~25fps）
CONST GRAV = 1               ' 重力（单位：1/8 像素 / 拍²）
CONST THRUST = 3             ' 推进加速度（同单位）
CONST FUEL0 = 100            ' 起始燃料（拍）
CONST COLS = 30              ' 地形列数
CONST COLW = 14              ' 每列宽（像素）—— 见 layout 里按屏幕重算
CONST TURN = 15              ' 一次按键转多少度
CONST SAFE_VY = 26           ' 允许的着陆垂直速度上限（同单位，26 ≈ 3.2 像素/拍）
CONST SAFE_VX = 16           ' 允许的着陆水平速度上限（≈ 2 像素/拍）
CONST SAFE_ANG = 12          ' 允许的着陆倾角（度）
CONST MAXSTEPS = 900         ' 自检里一发最多推这么多拍（防死循环）

CONST C_SKY = &HFF0A0A14
CONST C_STAR = &HFFB8B4D0
CONST C_EARTH = &HFF6FD3FF
CONST C_ROCK = &HFF3A3A48
CONST C_ROCKTOP = &HFF6A6A80
CONST C_PAD = &HFF4ADE80
CONST C_HUD = &HFF16142A
CONST C_TEXT = &HFFEDEDF2
CONST C_DIM = &HFF9AA0B0
CONST C_OK = &HFF4ADE80
CONST C_WARN = &HFFFF8A5C
CONST C_BAD = &HFFD8443C
CONST C_LANDER = &HFFD8D8E8
CONST C_FLAME = &HFFFFB020
CONST C_BTN = &HFF34305A
CONST C_BTN_HOT = &HFF6A5ACD

' ══════════════════════════════════════════════════════════════════════════
'  状态
' ══════════════════════════════════════════════════════════════════════════
DIM sw AS INTEGER
DIM sh AS INTEGER
DIM colw AS INTEGER
' 维度用 CONST 写 —— `DIM a(N)` 曾经**不生效**（只分 2 格、写就越界，
' 让循环变量一路跑到 760），v0.96.331 起解析期会查常量表，现在是对的。
DIM terr(COLS) AS INTEGER
DIM padCol AS INTEGER
DIM hx AS INTEGER
DIM hy AS INTEGER
DIM vx AS INTEGER
DIM vy AS INTEGER
DIM ang AS INTEGER
DIM fuel AS INTEGER
DIM thrusting AS INTEGER
DIM leftHeld AS INTEGER
DIM rightHeld AS INTEGER
DIM state AS INTEGER
DIM quit AS INTEGER
DIM boomT AS INTEGER
DIM ground AS INTEGER
DIM bestScore AS INTEGER
DIM lastMsg AS STRING

' 底栏几何
DIM ctlY AS INTEGER
DIM fireY AS INTEGER
DIM fireH AS INTEGER
DIM rotY AS INTEGER
DIM rotH AS INTEGER

' 临时量
DIM i AS INTEGER
DIM n AS INTEGER
DIM mt AS INTEGER
DIM rx AS INTEGER
DIM ry AS INTEGER
DIM cx AS INTEGER
DIM vv AS INTEGER
DIM txt AS STRING

' 状态
CONST ST_FLY = 0
CONST ST_DOWN = 1
CONST ST_OVER = 2


' ══════════════════════════════════════════════════════════════════════════
'  纯规则（自检直接打它们的返回值）
' ══════════════════════════════════════════════════════════════════════════

' 角度 → 正弦 × 1000（整数表，避免依赖前端的三角函数实现）
FUNCTION sind(deg AS INTEGER) AS INTEGER
    SELECT CASE deg
        CASE 0
            sind = 0
        CASE 15
            sind = 259
        CASE 30
            sind = 500
        CASE 45
            sind = 707
        CASE ELSE
            sind = 1000
    END SELECT
END FUNCTION

FUNCTION cosd(deg AS INTEGER) AS INTEGER
    SELECT CASE deg
        CASE 0
            cosd = 1000
        CASE 15
            cosd = 966
        CASE 30
            cosd = 866
        CASE 45
            cosd = 707
        CASE ELSE
            cosd = 0
    END SELECT
END FUNCTION

' 角度取绝对值
FUNCTION absi(v AS INTEGER) AS INTEGER
    IF v < 0 THEN
        absi = 0 - v
    ELSE
        absi = v
    END IF
END FUNCTION

' 落地判据：三条都满足才算成功 —— 这是这个游戏唯一的胜负规则
FUNCTION landed(dvy AS INTEGER, dvx AS INTEGER, deg AS INTEGER) AS INTEGER
    IF absi(dvy) > SAFE_VY THEN
        landed = 0
        EXIT FUNCTION
    END IF
    IF absi(dvx) > SAFE_VX THEN
        landed = 0
        EXIT FUNCTION
    END IF
    IF absi(deg) > SAFE_ANG THEN
        landed = 0
        EXIT FUNCTION
    END IF
    landed = 1
END FUNCTION

' ══════════════════════════════════════════════════════════════════════════
'  局面
' ══════════════════════════════════════════════════════════════════════════

SUB layout()
    colw = sw / COLS
    IF colw < 4 THEN
        colw = 4
    END IF
    ground = sh - 268
    ctlY = sh - 168
    rotY = ctlY + 62
    rotH = 62
    fireY = ctlY + 8
    fireH = 48
END SUB

' 新一局：重掷月面 + 找一块平地当着陆坪
SUB newRound()
    DIM base AS INTEGER
    DIM c AS INTEGER
    DIM prev AS INTEGER
    DIM d AS INTEGER

    base = ground
    c = 0
    prev = base
    WHILE c < COLS
        ' 相邻列高差不超过 2 —— 太陡的地形会让人以为是 bug
        d = ui_rand(5) - 2
        prev = prev + d
        IF prev > ground + 26 THEN
            prev = ground + 26
        END IF
        IF prev < ground - 56 THEN
            prev = ground - 56
        END IF
        terr(c) = prev
        c = c + 1
    WEND

    ' 找一块宽度 3 的平地当着陆坪（找不到就人为压平一段）
    padCol = 0 - 1
    c = 2
    WHILE c < COLS - 4
        IF padCol < 0 THEN
            IF terr(c) = terr(c + 1) THEN
                IF terr(c) = terr(c + 2) THEN
                    padCol = c
                END IF
            END IF
        END IF
        c = c + 1
    WEND
    IF padCol < 0 THEN
        padCol = COLS / 2
        terr(padCol) = base
        terr(padCol + 1) = base
        terr(padCol + 2) = base
    END IF

    hx = sw / 2
    ' ⚠ 起始高度必须在 HUD（74）**之下**，否则登月舱被信息带盖住、玩家看不见自己
    hy = 104
    vx = ui_rand(7) - 3
    vy = 0
    ang = 0
    fuel = FUEL0
    thrusting = 0
    leftHeld = 0
    rightHeld = 0
    state = ST_FLY
    boomT = 0
    lastMsg = "按住【点火】减速，左右键调姿态"
END SUB

' 这一拍上的物理
SUB stepLander()
    IF thrusting = 1 THEN
        IF fuel > 0 THEN
            DIM s AS INTEGER
            DIM c2 AS INTEGER
            s = sind(ang)
            c2 = cosd(ang)
            vx = vx + s * THRUST / 1000
            vy = vy - c2 * THRUST / 1000
            fuel = fuel - 1
        END IF
    END IF

    vy = vy + GRAV
    hx = hx + vx / 8
    hy = hy + vy / 8

    ' 左右出界：撞到屏幕边就算坠毁（原版是"飞出画面"，这里更明确）
    IF hx < 6 THEN
        crash("撞上了屏幕左缘")
        EXIT SUB
    END IF
    IF hx > sw - 6 THEN
        crash("撞上了屏幕右缘")
        EXIT SUB
    END IF

    ' 撞地判定：拿所在列的地形高度比
    DIM col AS INTEGER
    DIM top AS INTEGER
    col = hx / colw
    IF col < 0 THEN
        col = 0
    END IF
    IF col > COLS - 1 THEN
        col = COLS - 1
    END IF
    top = terr(col)
    IF hy >= top THEN
        hy = top
        IF landed(vy, vx, ang) = 1 THEN
            touchDown()
        ELSE
            crash("落得太重")
        END IF
    END IF
END SUB

SUB touchDown()
    state = ST_OVER
    DIM score AS INTEGER
    score = fuel * 10
    IF score > bestScore THEN
        bestScore = score
    END IF
    lastMsg = "着陆成功！剩余燃料 " + STR$(fuel) + " → " + STR$(score) + " 分"
    ui_beep(1046, 120)
    ui_vibrate(60, 120)
END SUB

SUB crash(why AS STRING)
    state = ST_DOWN
    boomT = 0
    lastMsg = why
    ui_beep(140, 320)
    ui_vibrate(200, 240)
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  绘制
' ══════════════════════════════════════════════════════════════════════════

SUB drawStars()
    DIM k AS INTEGER
    DIM sx AS INTEGER
    DIM sy AS INTEGER
    k = 0
    WHILE k < 20
        sx = ui_rand(sw)
        sy = ui_rand(sh / 3)
        ui_circle(sx, sy, 1, C_STAR, 1, 0)
        k = k + 1
    WEND
END SUB

SUB drawTerrain()
    DIM c AS INTEGER
    DIM x AS INTEGER
    DIM y AS INTEGER
    c = 0
    WHILE c < COLS
        x = c * colw
        y = terr(c)
        ui_rect(x, y, colw, sh - y, C_ROCK, 1, 0, 0)
        ui_rect(x, y, colw, 3, C_ROCKTOP, 1, 0, 0)
        c = c + 1
    WEND
    ' 着陆坪标绿
    ui_rect(padCol * colw, terr(padCol) - 2, colw * 3, 5, C_PAD, 1, 0, 0)
END SUB

SUB drawLander()
    DIM lx AS INTEGER
    DIM ly AS INTEGER
    lx = hx
    ly = hy
    ' 舱体
    ui_rect(lx - 9, ly - 14, 18, 14, C_LANDER, 1, 0, 3)
    ui_rect(lx - 5, ly - 20, 10, 7, C_LANDER, 1, 0, 2)
    ' 支腿（成功/失败都照画，只是姿态不同）
    ui_line(lx - 9, ly, lx - 15, ly + 9, C_LANDER, 2)
    ui_line(lx + 9, ly, lx + 15, ly + 9, C_LANDER, 2)
    ' 喷口方向：用一条短线表示当前姿态（角度为 0 时垂直向下）
    ui_line(lx, ly, lx + sind(ang) / 40, ly + cosd(ang) / 40, C_DIM, 3)
    IF thrusting = 1 THEN
        IF fuel > 0 THEN
            ui_circle(lx + sind(ang) / 40, ly + 12 + cosd(ang) / 40, 5, C_FLAME, 1, 0)
        END IF
    END IF
END SUB

SUB drawHud()
    ui_rect(0, 0, sw, 74, C_HUD, 1, 0, 0)
    ui_text(10, 8, "高度", C_DIM, 12, 0)
    txt = STR$(terr(hx / colw) - hy)
    ui_text(10, 22, txt, C_TEXT, 18, 0)

    ui_text(sw / 2 - 40, 8, "垂速", C_DIM, 12, 0)
    txt = STR$(vy)
    IF absi(vy) > SAFE_VY THEN
        ui_text(sw / 2 - 40, 22, txt, C_WARN, 18, 0)
    ELSE
        ui_text(sw / 2 - 40, 22, txt, C_OK, 18, 0)
    END IF

    ui_text(sw / 2 + 30, 8, "横速", C_DIM, 12, 0)
    txt = STR$(vx)
    IF absi(vx) > SAFE_VX THEN
        ui_text(sw / 2 + 30, 22, txt, C_WARN, 18, 0)
    ELSE
        ui_text(sw / 2 + 30, 22, txt, C_OK, 18, 0)
    END IF

    ui_text(sw - 10, 8, "燃料", C_DIM, 12, 2)
    txt = STR$(fuel)
    IF fuel > 20 THEN
        ui_text(sw - 10, 22, txt, C_OK, 18, 2)
    ELSE
        ui_text(sw - 10, 22, txt, C_BAD, 18, 2)
    END IF

    ui_text(10, 50, "角度 " + STR$(ang) + "°   分数 " + STR$(bestScore), C_DIM, 13, 0)
    ui_rect(0, 72, sw, 2, C_ROCKTOP, 1, 0, 0)
END SUB

SUB drawControls()
    ui_rect(0, ctlY, sw, sh - ctlY, C_HUD, 1, 0, 0)
    ui_rect(0, ctlY, sw, 2, C_ROCKTOP, 1, 0, 0)

    DIM half AS INTEGER
    half = sw / 2
    IF leftHeld = 1 THEN
        ui_rect(20, rotY, half - 30, rotH, C_BTN_HOT, 1, 0, 12)
    ELSE
        ui_rect(20, rotY, half - 30, rotH, C_BTN, 1, 0, 12)
    END IF
    IF rightHeld = 1 THEN
        ui_rect(half + 10, rotY, half - 30, rotH, C_BTN_HOT, 1, 0, 12)
    ELSE
        ui_rect(half + 10, rotY, half - 30, rotH, C_BTN, 1, 0, 12)
    END IF
    ui_text(20 + (half - 30) / 2, rotY + 18, "◀ 左转", C_TEXT, 16, 1)
    ui_text(half + 10 + (half - 30) / 2, rotY + 18, "右转 ▶", C_TEXT, 16, 1)

    IF thrusting = 1 THEN
        IF fuel > 0 THEN
            ui_rect(20, fireY, sw - 40, fireH, C_FLAME, 1, 0, 14)
            ui_text(sw / 2, fireY + 12, "点 火 中", &HFF2A1A00, 20, 1)
        ELSE
            ui_rect(20, fireY, sw - 40, fireH, C_BTN, 1, 0, 14)
            ui_text(sw / 2, fireY + 12, "燃料耗尽", C_BAD, 20, 1)
        END IF
    ELSE
        ui_rect(20, fireY, sw - 40, fireH, C_BAD, 1, 0, 14)
        ui_text(sw / 2, fireY + 12, "点 火", &HFFFFF0EC, 20, 1)
    END IF
END SUB

SUB drawScene()
    ui_clear(C_SKY)
    drawStars()
    ' 地球：右上角一个小蓝圆，纯装饰
    ui_circle(sw - 44, 110, 22, C_EARTH, 1, 0)
    drawTerrain()

    IF state = ST_FLY THEN
        drawLander()
    ELSE
        IF state = ST_DOWN THEN
            DIM rr AS INTEGER
            rr = 6 + boomT * 4
            ui_circle(hx, hy, rr + 8, C_BAD, 1, 0)
            ui_circle(hx, hy, rr, C_FLAME, 1, 0)
        ELSE
            drawLander()
        END IF
    END IF

    drawHud()
    ui_text(sw / 2, ctlY - 26, lastMsg, C_TEXT, 14, 1)
    drawControls()
    ui_present()
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  输入
' ══════════════════════════════════════════════════════════════════════════
SUB handlePoint(isDown AS INTEGER)
    rx = ui_msg_a()
    ry = ui_msg_b()

    IF isDown = 0 THEN
        ' 抬起：三个键都可能被松开，一次性全清（手指滑走也要能停）
        thrusting = 0
        leftHeld = 0
        rightHeld = 0
        EXIT SUB
    END IF

    IF state = ST_OVER THEN
        newRound()
        EXIT SUB
    END IF
    IF state = ST_DOWN THEN
        EXIT SUB
    END IF

    IF ry >= fireY THEN
        IF ry <= fireY + fireH THEN
            thrusting = 1
            EXIT SUB
        END IF
    END IF
    IF ry >= rotY THEN
        IF ry <= rotY + rotH THEN
            IF rx < sw / 2 THEN
                leftHeld = 1
                ang = ang - TURN
                IF ang < 0 - 90 THEN
                    ang = 0 - 90
                END IF
                ui_beep(520, 20)
            ELSE
                rightHeld = 1
                ang = ang + TURN
                IF ang > 90 THEN
                    ang = 90
                END IF
                ui_beep(760, 20)
            END IF
        END IF
    END IF
END SUB

SUB handleKey()
    DIM k AS INTEGER
    k = ui_msg_a()
    IF k = 27 THEN
        quit = 1
    END IF
    IF state = ST_OVER THEN
        IF k = 13 THEN
            newRound()
        END IF
        EXIT SUB
    END IF
    IF k = 32 THEN
        thrusting = 1
    END IF
    IF k = 37 THEN
        ang = ang - TURN
    END IF
    IF k = 39 THEN
        ang = ang + TURN
    END IF
    IF ang < 0 - 90 THEN
        ang = 0 - 90
    END IF
    IF ang > 90 THEN
        ang = 90
    END IF
END SUB

SUB handleKeyUp()
    DIM k AS INTEGER
    k = ui_msg_a()
    IF k = 32 THEN
        thrusting = 0
    END IF
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  主循环
' ══════════════════════════════════════════════════════════════════════════
SUB runGame()
    ui_keep_on(1)
    layout()
    newRound()

    WHILE ui_win_closed() = 0
        IF quit = 1 THEN
            ui_win_close()
        END IF

        mt = ui_wait_msg(PACE)
        n = 0
        WHILE mt <> 0
            IF mt = 10 THEN
                quit = 1
            END IF
            IF mt = 1 THEN
                handleKey()
            END IF
            IF mt = 2 THEN
                handleKeyUp()
            END IF
            IF mt = 6 THEN
                handlePoint(1)
            END IF
            IF mt = 4 THEN
                handlePoint(1)
            END IF
            IF mt = 8 THEN
                handlePoint(0)
            END IF
            IF mt = 5 THEN
                handlePoint(0)
            END IF
            IF mt = 9 THEN
                IF state = ST_FLY THEN
                    stepLander()
                ELSE
                    boomT = boomT + 1
                END IF
            END IF
            n = n + 1
            IF n >= 40 THEN
                mt = 0
            ELSE
                mt = ui_poll_msg()
            END IF
        WEND

        drawScene()
    WEND
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  开机自检
' ══════════════════════════════════════════════════════════════════════════
SUB simCheck()
    PRINT "── 登月 · 物理与着陆判据自检 ──"
    PRINT "  三角函数表 0/30/45/90 度正弦（应 0/500/707/1000）:"; sind(0); sind(30); sind(45); sind(90)
    PRINT "  余弦 0/90 度（应 1000/0）:"; cosd(0); cosd(90)

    ' 自由落体：起手 vy=0，走 10 拍必须**变快**（vy 从 0 涨到 10）
    vy = 0
    vx = 0
    thrusting = 0
    fuel = FUEL0
    i = 0
    WHILE i < 10
        vy = vy + GRAV
        i = i + 1
    WEND
    PRINT "  自由落体 10 拍后 vy（应 10）:"; vy
    vy = 0

    ' 点火反推：垂直姿态下净加速度应为 GRAV - THRUST = -2 ⇒ 10 拍后 vy = -20
    vy = 0
    thrusting = 1
    ang = 0
    fuel = FUEL0
    i = 0
    WHILE i < 10
        vy = vy - cosd(ang) * THRUST / 1000
        vy = vy + GRAV
        fuel = fuel - 1
        i = i + 1
    WEND
    PRINT "  点火 10 拍后 vy（应 -20，负=向上）:"; vy
    PRINT "  燃料（应 90）:"; fuel

    ' 燃料耗尽后点火无效
    fuel = 0
    thrusting = 1
    vy = 0
    i = 0
    WHILE i < 5
        IF fuel > 0 THEN
            vy = vy - cosd(ang) * THRUST / 1000
            fuel = fuel - 1
        END IF
        vy = vy + GRAV
        i = i + 1
    WEND
    PRINT "  空油箱点火 5 拍后 vy（应 5，只剩重力）:"; vy

    ' 着陆判据：三条都满足才算成功
    PRINT "  轻放直立（应 1）:"; landed(10, 8, 0)
    PRINT "  落太快（应 0）:"; landed(40, 0, 0)
    PRINT "  横速太大（应 0）:"; landed(5, 30, 0)
    PRINT "  倾角太大（应 0）:"; landed(5, 2, 30)
    PRINT "  边界：正好 SAFE_VY（应 1）:"; landed(SAFE_VY, SAFE_VX, SAFE_ANG)
    PRINT "  边界：超一点（应 0）:"; landed(SAFE_VY + 1, 0, 0)
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  主程序
' ══════════════════════════════════════════════════════════════════════════
simCheck()

sw = ui_scr_w()
sh = ui_scr_h()
IF sw <= 0 THEN
    sw = 390
END IF
IF sh <= 0 THEN
    sh = 660
END IF

wh = ui_win_open_ex("登月", sw, sh, 0, 0)

IF wh < 1 THEN
    PRINT "（桌面脚手架：没有真窗口，物理自检打完就退出）"
ELSE
    runGame()
END IF
