' 跑车躲驴（DONKEY）—— 用 **BASIC** 写的手机游戏
'
' 出处与版权：原型是 1981 年随 IBM PC 附带的 BASIC 程序（那个年代最有名的一份——
' 它被广泛认为是 PC 上第一个"图形游戏"）。**这份是照玩法规则重写的**：
' 结构、变量名、注释、绘制方式都是自己的，没有抄原件的一行代码 ——
' 与 `gorilla.bas` 同一条规矩（`Examples/` 会随 APK 分发，不能夹带别人的东西）。
' 原版的画面是 40×25 字符模式、靠字符块拼车和驴；这里按手机重做：
' 竖屏、像素绘制、方向盘换成两个大按钮。
'
' 玩法：车在最下面的一条道上，驴从上面下来。按 ◀ / ▶ 换道躲开它。
' 撞上就结束。每过一头驴加一分，**过得越多跑得越快**（2 → 12 像素/拍）。
'
' ◆ 手机那套 UI
'
' 开窗 / 绘图 / 输入 / 定时器是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
' 由 `vmltool.config.xml` 的 `<Language Name="basic" Libs="vmlui.vml">` 挂上来。
' 写法上与 `gorilla.bas` 同一套：`ui_win_open_ex` 锁竖屏 + 不要手柄区（全程触摸）。
'
' ◆ 定下的三条输入口径（照抄 gorilla / whack 的结论，别再试一遍）
'
'   ① **只用触摸 + 键盘两路**，不依赖手柄区 —— 手柄区会吃掉一百多像素的画面高度，
'      而这里两个按钮本来就在屏幕底部，自绘比系统手柄更贴手（也更好看）。
'   ② **键盘要认三个键**：方向键左右（VK 37/39）、A/D（65/68）、以及手柄映射过来的
'      同一个 VK —— 手机外接键盘 / 模拟器上才好操作。`handleKey` 里一次判全。
'   ③ **换道是"点一下走一格"**，不做长按连发。三个道口距很大，点一下一格最准；
'      长按连发要额外维护"谁负责停它"（见 CLAUDE.md 里 tetris 那三条刹车），
'      在这个玩法上是纯负担。
'
' ◆ 写法要求（本前端的硬性要求，改这份别踩回去）
'
'   · 外部过程必须 `NATIVE SUB` / `NATIVE FUNCTION` + **空体**，否则会被加上
'     sub_/func_ 前缀、链接期找不到。
'   · 模块级变量必须在赋值前 `DIM`。
'   · 颜色写 `&HFFRRGGBB`（词法器认 `&H`，不认 `0x`）。
'   · **赋值语句不能写在 SUB 定义之后**（会被当成函数调用）—— 这份的顶层赋值都在
'     `runGame` 里或 SUB 之前。
'
' ⚠ **`gorilla.bas` 头部那份"必须绕开"的清单已经过期，别照抄**（v0.96.330 逐条重测）：
'   数组（含动态下标 / 二维 / SUB 内）、`\` 与 `MOD`、带括号的子表达式、CONST 参与算术、
'   `AND`/`OR`、字符串拼接、`STR$`、SUB 的字符串形参、`SIN`/`COS`（×10000 定标）、
'   `FOR…NEXT`（含正负 `STEP`）、`SELECT CASE`、`DATA`/`READ`/`RESTORE`、用户 `FUNCTION`、
'   `ELSEIF`、`GOTO` 标签 —— **现在全都对**（判据是 `scripts/vml-basic-probe/`，19/19）。
'   那份清单是 v0.96.3xx 期间的实测记录，当时的缺陷后来都修掉了；这份文件因此写得比较"正常"。

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
NATIVE FUNCTION ui_timer_set(ms AS INTEGER, tag AS INTEGER) AS INTEGER
END FUNCTION
NATIVE SUB ui_timer_kill(id AS INTEGER)
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
NATIVE FUNCTION ui_dlg_msg(title AS STRING, body AS STRING, style AS INTEGER) AS INTEGER
END FUNCTION

' ══════════════════════════════════════════════════════════════════════════
'  常量
' ══════════════════════════════════════════════════════════════════════════
CONST NFLOOR = 3           ' 车道数
CONST TICK = 40            ' 逻辑拍（毫秒）：位移与出驴都按它走
CONST PACE = 40            ' 主循环最长睡眠 —— 把重绘锁在 ~25fps，输入延迟 ≤40ms
CONST HUDH = 44            ' 顶部信息带高
CONST PADH = 108           ' 底部按钮区高
CONST CARW = 46            ' 车宽（像素）
CONST CARH = 74            ' 车高
CONST DONW = 40            ' 驴宽
CONST DONH = 52            ' 驴高
CONST SPEED0 = 2           ' 起始速度（像素/拍）
CONST SPEEDMAX = 12        ' 速度上限
CONST PERSPEED = 8         ' 每过几头驴加一格速度
CONST GAP0 = 16            ' 出驴间隔（拍），随速度收紧
CONST GAPMIN = 7

CONST C_SKY = &HFF101828
CONST C_ROAD = &HFF2A2A32
CONST C_ROADEDGE = &HFF14141C
CONST C_DASH = &HFF6A6A78
CONST C_CAR = &HFFD8443C
CONST C_CARTOP = &HFFF07A6E
CONST C_CARWIN = &HFF9FD8FF
CONST C_WHEEL = &HFF14141C
CONST C_DON = &HFF9A7B4F
CONST C_DONMANE = &HFF6E5636
CONST C_DONEYE = &HFFFFF4D8
CONST C_HUD = &HFF1E1B33
CONST C_TEXT = &HFFEDEDF2
CONST C_DIM = &HFF9AA0B0
CONST C_BTN = &HFF37415E
CONST C_BTN_D = &HFF232A40
CONST C_BOOM1 = &HFFFF6A1E
CONST C_BOOM2 = &HFFFFD24A

' ══════════════════════════════════════════════════════════════════════════
'  模块级变量
' ══════════════════════════════════════════════════════════════════════════
DIM sw AS INTEGER
DIM sh AS INTEGER
DIM roadTop AS INTEGER
DIM roadBot AS INTEGER
DIM laneW AS INTEGER
DIM lane0 AS INTEGER
DIM carLane AS INTEGER
DIM carX AS INTEGER
DIM carY AS INTEGER
DIM donLane AS INTEGER
DIM donX AS INTEGER
DIM donY AS INTEGER
DIM donOn AS INTEGER
DIM speed AS INTEGER
DIM score AS INTEGER
DIM best AS INTEGER
DIM passed AS INTEGER
DIM gapLeft AS INTEGER
DIM dashY AS INTEGER
DIM crashed AS INTEGER
DIM quit AS INTEGER
DIM boomT AS INTEGER
DIM hitLane AS INTEGER

' 按钮几何（每帧重算一次，屏幕尺寸变了也跟着走）
DIM btnY AS INTEGER
DIM btnH AS INTEGER
DIM btnW AS INTEGER
DIM btnLx AS INTEGER
DIM btnRx AS INTEGER

' 临时量
DIM i AS INTEGER
DIM k AS INTEGER
DIM t AS INTEGER
DIM n AS INTEGER
DIM mt AS INTEGER
DIM rx AS INTEGER
DIM ry AS INTEGER
DIM tid AS INTEGER

' CONST 的替身变量（SUB 里要参与算术的，必须先落成普通变量）
DIM nfloor AS INTEGER
DIM hudh AS INTEGER
DIM padh AS INTEGER
DIM carw AS INTEGER
DIM carh AS INTEGER
DIM donw AS INTEGER
DIM donh AS INTEGER
DIM speedMax AS INTEGER
DIM perSpeed AS INTEGER
DIM gap0 AS INTEGER
DIM gapMin AS INTEGER
DIM tickMs AS INTEGER
DIM paceMs AS INTEGER

nfloor = NFLOOR
hudh = HUDH
padh = PADH
carw = CARW
carh = CARH
donw = DONW
donh = DONH
speedMax = SPEEDMAX
perSpeed = PERSPEED
gap0 = GAP0
gapMin = GAPMIN
tickMs = TICK
paceMs = PACE

' ══════════════════════════════════════════════════════════════════════════
'  子过程
' ══════════════════════════════════════════════════════════════════════════

' 屏幕尺寸 / 车道几何 —— 只在开局与 resize 后调
SUB layout()
    roadTop = hudh
    roadBot = sh - padh
    laneW = sw - 24
    laneW = laneW / nfloor
    lane0 = 12
    btnH = 62
    btnY = sh - padh + 32
    btnW = (sw - 60) / 2
    btnLx = 20
    btnRx = sw - 20 - btnW
    carX = laneCenter(carLane)
    carX = carX - carw / 2
    carY = roadBot - carh - 18
END SUB

' 第 i 条车道的中心 x（车道 0 在最左）
FUNCTION laneCenter(i AS INTEGER) AS INTEGER
    tx = lane0 + laneW * i
    tx = tx + laneW / 2
    laneCenter = tx
END FUNCTION

' 画一辆车：车身 + 车顶 + 挡风 + 两个轮子 + 一对前灯
SUB drawCar(px AS INTEGER, py AS INTEGER)
    ui_rect(px, py + 12, carw, carh - 18, C_CAR, 1, 0, 8)
    ui_rect(px + 7, py, carw - 14, 26, C_CARTOP, 1, 0, 6)
    ui_rect(px + 11, py + 5, carw - 22, 14, C_CARWIN, 1, 0, 4)
    ui_rect(px - 2, py + 16, 6, 20, C_WHEEL, 1, 0, 2)
    ui_rect(px + carw - 4, py + 16, 6, 20, C_WHEEL, 1, 0, 2)
    ui_rect(px - 2, py + carh - 22, 6, 20, C_WHEEL, 1, 0, 2)
    ui_rect(px + carw - 4, py + carh - 22, 6, 20, C_WHEEL, 1, 0, 2)
    ui_rect(px + 6, py + carh - 8, 10, 6, &HFFFFE066, 1, 0, 2)
    ui_rect(px + carw - 16, py + carh - 8, 10, 6, &HFFFFE066, 1, 0, 2)
END SUB

' 画一头驴（朝下走，所以头在下面）：身子 + 鬃毛 + 头 + 耳朵 + 四条腿
SUB drawDonkey(px AS INTEGER, py AS INTEGER)
    ui_rect(px + 6, py, donw - 12, 30, C_DON, 1, 0, 8)
    ui_rect(px + 6, py + 2, donw - 12, 8, C_DONMANE, 1, 0, 4)
    ui_rect(px + 10, py + 26, donw - 20, 18, C_DON, 1, 0, 6)
    ui_rect(px + 8, py + 24, 5, 9, C_DONMANE, 1, 0, 2)
    ui_rect(px + donw - 13, py + 24, 5, 9, C_DONMANE, 1, 0, 2)
    ui_circle(px + 15, py + 34, 3, C_DONEYE, 1, 0)
    ui_circle(px + donw - 15, py + 34, 3, C_DONEYE, 1, 0)
    ui_rect(px + 8, py + 42, 5, 10, C_DONMANE, 1, 0, 2)
    ui_rect(px + donw - 13, py + 42, 5, 10, C_DONMANE, 1, 0, 2)
END SUB

' 顶部信息带
SUB drawHud()
    ui_rect(0, 0, sw, hudh, C_HUD, 1, 0, 0)
    ui_text(14, 13, "得分", C_DIM, 13, 0)
    ui_text(52, 8, STR$(score), C_TEXT, 22, 0)
    ui_text(sw / 2, 13, "最快", C_DIM, 13, 1)
    ui_text(sw / 2, 8, STR$(speed), C_TEXT, 22, 1)
    ui_text(sw - 14, 13, "最高", C_DIM, 13, 2)
    ui_text(sw - 14, 8, STR$(best), C_TEXT, 22, 2)
END SUB

' 底部两个按钮
SUB drawPads()
    ui_rect(0, sh - padh, sw, padh, C_HUD, 1, 0, 0)
    ui_rect(btnLx, btnY, btnW, btnH, C_BTN, 1, 0, 14)
    ui_rect(btnRx, btnY, btnW, btnH, C_BTN, 1, 0, 14)
    ' 箭头用两条线拼（多边形要 int 数组，而数组不能用）
    rx = btnLx + btnW / 2
    ry = btnY + btnH / 2
    ui_line(rx + 12, ry - 16, rx - 12, ry, C_TEXT, 6)
    ui_line(rx - 12, ry, rx + 12, ry + 16, C_TEXT, 6)
    rx = btnRx + btnW / 2
    ui_line(rx - 12, ry - 16, rx + 12, ry, C_TEXT, 6)
    ui_line(rx + 12, ry, rx - 12, ry + 16, C_TEXT, 6)
    ui_text(btnLx + btnW / 2, btnY + btnH + 4, "← 或 A", C_DIM, 12, 1)
    ui_text(btnRx + btnW / 2, btnY + btnH + 4, "→ 或 D", C_DIM, 12, 1)
END SUB

SUB drawScene()
    ui_clear(C_SKY)
    ui_rect(0, roadTop, sw, roadBot - roadTop, C_ROAD, 1, 0, 0)
    ui_rect(0, roadTop, 12, roadBot - roadTop, C_ROADEDGE, 1, 0, 0)
    ui_rect(sw - 12, roadTop, 12, roadBot - roadTop, C_ROADEDGE, 1, 0, 0)

    ' 车道分隔线：两排滚动的虚线。`dashY` 每拍前进 speed，绕一圈回到顶
    i = 1
    WHILE i < nfloor
        tx = lane0 + laneW * i
        ty = roadTop + dashY
        WHILE ty < roadBot
            ui_rect(tx - 2, ty, 4, 26, C_DASH, 1, 0, 0)
            ty = ty + 64
        WEND
        i = i + 1
    WEND

    IF donOn = 1 THEN
        drawDonkey(donX, donY)
    END IF

    IF crashed = 0 THEN
        drawCar(carX, carY)
    ELSE
        ' 撞了：车画在原地，叠一团扩散的火
        drawCar(carX, carY)
        rr = 8 + boomT * 5
        ui_circle(carX + carw / 2, carY + carh / 2, rr + 10, C_BOOM1, 1, 0)
        ui_circle(carX + carw / 2, carY + carh / 2, rr, C_BOOM2, 1, 0)
    END IF

    drawHud()
    drawPads()
    ui_present()
END SUB

' 把车挪到第 L 条道（越界就夹住，不绕圈）
SUB moveCar(d AS INTEGER)
    carLane = carLane + d
    IF carLane < 0 THEN
        carLane = 0
    END IF
    IF carLane > nfloor - 1 THEN
        carLane = nfloor - 1
    END IF
    carX = laneCenter(carLane)
    carX = carX - carw / 2
    ui_beep(880, 22)
END SUB

' 这一拍：出驴、推驴、判撞、计分
SUB stepWorld()
    IF donOn = 0 THEN
        gapLeft = gapLeft - 1
        IF gapLeft <= 0 THEN
            donLane = ui_rand(nfloor)
            donX = laneCenter(donLane)
            donX = donX - donw / 2
            donY = roadTop - donh
            donOn = 1
            ' 间隔随速度收紧 —— 跑得快、驴也来得密
            gapLeft = gap0 - (speed - SPEED0)
            IF gapLeft < gapMin THEN
                gapLeft = gapMin
            END IF
        END IF
    ELSE
        donY = donY + speed
        ' 判撞：同一条道、且竖直方向重叠
        IF donLane = carLane THEN
            ty = donY + donh
            IF ty >= carY THEN
                ty = donY
                IF ty <= carY + carh THEN
                    crash()
                END IF
            END IF
        END IF
        ' 走到底 = 躲过去了
        IF donOn = 1 THEN
            IF donY > roadBot THEN
                donOn = 0
                score = score + 1
                passed = passed + 1
                IF score > best THEN
                    best = score
                END IF
                IF passed >= perSpeed THEN
                    passed = 0
                    IF speed < speedMax THEN
                        speed = speed + 1
                        ui_beep(1200, 30)
                    END IF
                END IF
            END IF
        END IF
    END IF

    ' 路面虚线滚动
    dashY = dashY + speed
    IF dashY >= 64 THEN
        dashY = dashY - 64
    END IF
END SUB

SUB crash()
    crashed = 1
    boomT = 0
    hitLane = donLane
    ui_beep(180, 220)
    ui_vibrate(120, 200)
END SUB

' 撞车之后：让爆炸播完再问「再来一局」
SUB handleCrashEnd()
    boomT = boomT + 1
    IF boomT >= 10 THEN
        IF quit = 0 THEN
            ' ⚠ 弹框期间必须把定时器停掉：`ui_dlg_msg` 是模态的，而这是**重复**定时器，
            '   挂多久就积压多少条 —— 返回后会被瞬间抽干，新一局立刻又被当成"撞了"。
            '   （whack.bas 头部记的就是这个坑；宿主侧 `WithTimersPaused` 已统一兜住，
            '     这里再显式停一次，是"两处都做对"而不是"指望某一处"。）
            ui_timer_kill(tid)
            k = ui_dlg_msg("撞车了", "得分 " + STR$(score) + "，再来一局？", 1)
            IF k = 0 THEN
                quit = 1
            ELSE
                resetRound()
                tid = ui_timer_set(tickMs, 0)
            END IF
        END IF
    END IF
END SUB

SUB resetRound()
    carLane = 1
    carX = laneCenter(carLane)
    carX = carX - carw / 2
    donOn = 0
    donLane = -1
    speed = SPEED0
    score = 0
    passed = 0
    gapLeft = gap0
    dashY = 0
    crashed = 0
    boomT = 0
END SUB

' 键盘：方向键 / A / D 换道；Esc 退出；回车 = 再来一局（撞车后）
SUB handleKey()
    k = ui_msg_a()
    IF k = 27 THEN
        quit = 1
    END IF
    IF crashed = 0 THEN
        IF k = 37 THEN
            moveCar(0 - 1)
        END IF
        IF k = 39 THEN
            moveCar(1)
        END IF
        IF k = 65 THEN
            moveCar(0 - 1)
        END IF
        IF k = 68 THEN
            moveCar(1)
        END IF
    ELSE
        IF k = 13 THEN
            boomT = 99
        END IF
    END IF
END SUB

' 触摸：落在左半 / 右半就换道。按钮与路面都算 —— 手指本来就会乱点，
' 与其让玩家去瞄那个矩形，不如"点屏幕哪边就往哪边"。
SUB handlePoint(isDown AS INTEGER)
    IF isDown = 0 THEN
        RETURN
    END IF
    IF crashed = 1 THEN
        RETURN
    END IF
    rx = ui_msg_a()
    IF rx < sw / 2 THEN
        moveCar(0 - 1)
    ELSE
        moveCar(1)
    END IF
END SUB

' ── 主循环 ─────────────────────────────────────────────────────────────
SUB runGame()
    ui_keep_on(1)
    layout()
    resetRound()
    tid = ui_timer_set(tickMs, 0)

    WHILE ui_win_closed() = 0
        IF quit = 1 THEN
            ui_win_close()
        END IF

        ' 一次滑动能来几十条 TOUCHMOVE，一条一画的话重绘会被输入拖垮。
        ' 位置是幂等的 ⇒ 把队列清空、然后画一帧就够（gorilla 同款）。
        mt = ui_wait_msg(paceMs)
        n = 0
        WHILE mt <> 0
            IF mt = 10 THEN
                quit = 1
            END IF
            IF mt = 9 THEN
                IF crashed = 0 THEN
                    stepWorld()
                ELSE
                    handleCrashEnd()
                END IF
            END IF
            IF mt = 1 THEN
                handleKey()
            END IF
            IF mt = 6 THEN
                handlePoint(1)
            END IF
            IF mt = 4 THEN
                handlePoint(1)
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
'  开机自检（无头，桌面脚手架就能跑）
'
'  判据是**行为**，不是"编过了"：同一条道上会撞、不同道不会撞、
'  躲过去会加分并（攒够之后）加速。这三条正是这个玩法仅有的三件事。
' ══════════════════════════════════════════════════════════════════════════
SUB simCheck()
    PRINT "── 跑车躲驴 · 碰撞自检（固定尺寸，不看屏幕，只算逻辑）──"
    sw = 390
    sh = 660
    layout()
    resetRound()
    PRINT "  画布 / 车道宽 / 车中心:"; sw; laneW; carX

    ' ① 同一条道 ⇒ 必撞
    carLane = 1
    carX = laneCenter(1)
    carX = carX - carw / 2
    donOn = 1
    donLane = 1
    donX = laneCenter(1)
    donX = donX - donw / 2
    donY = carY - donh + 4
    stepWorld()
    PRINT "  ① 同道相撞 -> crashed(应 1):"; crashed

    ' ② 换一条道 ⇒ 不该撞
    resetRound()
    carLane = 0
    carX = laneCenter(0)
    carX = carX - carw / 2
    donOn = 1
    donLane = 2
    donX = laneCenter(2)
    donX = donX - donw / 2
    donY = carY - donh + 4
    stepWorld()
    PRINT "  ② 异道不撞 -> crashed(应 0):"; crashed

    ' ③ 驴走到底 ⇒ 加分、驴消失
    resetRound()
    score = 0
    donOn = 1
    donLane = 2
    donY = roadBot - 2
    stepWorld()
    PRINT "  ③ 躲过去 -> 得分(应 1) / 驴还在吗(应 0):"; score; donOn

    ' ④ 攒够 perSpeed 头 ⇒ 加速一格
    resetRound()
    speed = SPEED0
    passed = perSpeed - 1
    donOn = 1
    donLane = 2
    donY = roadBot - 2
    stepWorld()
    PRINT "  ④ 攒够就加速 -> speed(应 3):"; speed

    ' ⑤ 换道要夹住边界，不能跑到道外
    resetRound()
    moveCar(0 - 1)
    moveCar(0 - 1)
    PRINT "  ⑤ 向左夹住 -> carLane(应 0):"; carLane
    k = 0
    WHILE k < 9
        moveCar(1)
        k = k + 1
    WEND
    PRINT "  ⑤ 向右夹住 -> carLane(应 2):"; carLane
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  主程序
' ══════════════════════════════════════════════════════════════════════════
simCheck()

' 尺寸：先问设备，再开窗。**顺序照抄 gorilla** —— 排版用的那两个数必须与交给
' 窗口的那两个数同源，否则内容会画到画布外面。
sw = ui_scr_w()
sh = ui_scr_h()
IF sw <= 0 THEN
    sw = 390
END IF
IF sh <= 0 THEN
    sh = 660
END IF

' 锁竖屏 + 不要手柄区（全程触摸，手柄区白吃一百多像素的画面高度）
wh = ui_win_open_ex("跑车躲驴", sw, sh, 0, 0)

' 宿主把窗口开出来了才开跑；桌面脚手架这里返回 0 —— 上面自检已打完，直接收工。
' ⚠ 别把这条判断当"平台探测"去别处复用，它只说明"这一轮有没有真窗口"。
IF wh < 1 THEN
    PRINT "（桌面脚手架：没有真窗口，碰撞自检打完就退出）"
ELSE
    runGame()
END IF
