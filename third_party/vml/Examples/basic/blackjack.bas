' 21 点（BLACKJACK）—— 用 **BASIC** 写的手机游戏
'
' 出处与版权：原型是 1970 年代流传最广的那份 21 点 BASIC 程序（收进 David Ahl 的
' 《BASIC Computer Games》，原版叫 BLACKJACK 或 21）。**这份是照玩法规则重写的**：
' 结构、变量名、注释、绘制方式都是自己的，没有抄原件的一行代码 ——
' 与 `gorilla.bas` 同一条规矩（`Examples/` 会随 APK 分发）。
'
' 玩法：标准赌场规则简化版 —— 庄家 17 点停（软 17 也停）、A 可当 1 或 11、
' 天生 21 点（两张就 21）赔 3:2、其余赢赔 1:1、和局退还赌注。筹码有限，输光就结束。
'
' ◆ 手机上怎么玩
'
' 原版是打字问「要不要牌（H/S）」，这里改成**三个大按钮**：要牌 / 停牌 / 加倍。
' 赌注用 −/+ 调（下注阶段），发牌后按钮换成要牌/停牌/加倍。
' —— 回合制游戏在手机上就该这么处理：**别让玩家调键盘**。
'
' ◆ 写法提醒（这几条是本前端**当前**的硬约束，都不是"建议"）
'
'   · **数组维度必须写字面量**：`DIM a(N) AS INTEGER`（N 是 CONST）**不生效** ——
'     实测 `a(4)` 恒为 0，而 `DIM a(5)` 正常（见 FRONTEND_DEFECTS.md 的 BASIC 一节）。
'   · **有返回值的必须写 `FUNCTION`**（`SUB … AS INTEGER` 会去链一个 `func_integer`），
'     且收尾必须是 `END FUNCTION`（写成 `END SUB` 会让整个解析错位）。
'   · **不要写无参 `FUNCTION`** —— 它恒返回 0（另有一条缺陷）。
'   · 外部过程必须 `NATIVE SUB` / `NATIVE FUNCTION` + 空体。
'   · 颜色写 `&HFFrrggbb`。

NATIVE SUB ui_clear(c AS INTEGER)
END SUB
NATIVE SUB ui_rect(x AS INTEGER, y AS INTEGER, w AS INTEGER, h AS INTEGER, c AS INTEGER, f AS INTEGER, lw AS INTEGER, r AS INTEGER)
END SUB
NATIVE SUB ui_circle(cx AS INTEGER, cy AS INTEGER, r AS INTEGER, c AS INTEGER, f AS INTEGER, lw AS INTEGER)
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
'  规则常量
' ══════════════════════════════════════════════════════════════════════════
CONST DECKN = 52             ' 一副牌
CONST MAXCARD = 12           ' 一手最多记这么多张（再多早就爆了）
CONST DEALER_STOP = 17       ' 庄家 17 点停（软 17 也停，简化）
CONST WIN = 1
CONST LOSE = 2
CONST PUSH = 3
CONST BJPAY_N = 3            ' 天生 21 点赔 3:2
CONST BJPAY_D = 2
CONST HUDH = 64
CONST CARDW = 54
CONST CARDH = 76

CONST C_FELT = &HFF0E3A22
CONST C_FELT_D = &HFF0A2A1A
CONST C_HUD = &HFF14140E
CONST C_TEXT = &HFFEDEDF2
CONST C_DIM = &HFF9AA0B0
CONST C_GOLD = &HFFE8C468
CONST C_RED = &HFFE05252
CONST C_DARK = &HFF1A1A22
CONST C_CARD = &HFFF6F4EE
CONST C_CARDBK = &HFF2A3A2A
CONST C_BTN = &HFF2E4A38
CONST C_BTN_HOT = &HFF4A7A5A
CONST C_FLAT = &HFF555A66

' 阶段
CONST PH_BET = 0
CONST PH_PLAY = 1
CONST PH_DEALER = 2
CONST PH_OVER = 3

' ══════════════════════════════════════════════════════════════════════════
'  状态（数组维度一律字面量，见文件头）
' ══════════════════════════════════════════════════════════════════════════
DIM deck(52) AS INTEGER
' ⚠ 两手牌**合成一个数组**用偏移区分（玩家 0..11 / 庄家 12..23）——
'   本前端**不支持数组形参**：`FUNCTION f(a(12) AS INTEGER)` 会把 `a` 当成函数调用
'   去链接，报 `未定义的函数 'func_a'`。用偏移就同时避开了"数组形参"和"把算点数
'   这条规则写两份"两件事。
DIM hand(24) AS INTEGER      ' 编码：点数*4 + 花色（点数 1..13）
DIM pn AS INTEGER            ' 玩家手牌数
DIM dn AS INTEGER            ' 庄家手牌数
DIM di AS INTEGER            ' 下一张牌在牌堆里的下标
DIM chips AS INTEGER
DIM bet AS INTEGER
DIM phase AS INTEGER
DIM dealerHidden AS INTEGER
DIM result AS INTEGER
DIM msg AS STRING
DIM quit AS INTEGER
DIM doubled AS INTEGER

DIM sw AS INTEGER
DIM sh AS INTEGER
DIM ctlY AS INTEGER
DIM b1x AS INTEGER
DIM b1y AS INTEGER
DIM b2x AS INTEGER
DIM b2y AS INTEGER
DIM b3x AS INTEGER
DIM b3y AS INTEGER
DIM bwy AS INTEGER
DIM bwh AS INTEGER
DIM bw AS INTEGER

DIM i AS INTEGER
DIM j AS INTEGER
DIM t AS INTEGER
DIM mt AS INTEGER
DIM rx AS INTEGER
DIM ry AS INTEGER
DIM tmp AS INTEGER
DIM txt AS STRING

' ══════════════════════════════════════════════════════════════════════════
'  规则（纯函数，自检直接打它们的返回值）
' ══════════════════════════════════════════════════════════════════════════

' 牌编码 → 点数（1..13）
FUNCTION rankOf(c AS INTEGER) AS INTEGER
    rankOf = c / 4
END FUNCTION

' 牌编码 → 花色（0..3）
FUNCTION suitOf(c AS INTEGER) AS INTEGER
    DIM r AS INTEGER
    r = c / 4
    suitOf = c - r * 4
END FUNCTION

' 一手牌的**点数**（A 先按 11 算，爆了再逐张降成 1）—— 这是全部玩法的核心
FUNCTION handValue(off AS INTEGER, n AS INTEGER) AS INTEGER
    DIM k AS INTEGER
    DIM r AS INTEGER
    DIM sum AS INTEGER
    DIM aces AS INTEGER
    k = 0
    sum = 0
    aces = 0
    WHILE k < n
        r = hand(off + k) / 4
        IF r = 1 THEN
            aces = aces + 1
            sum = sum + 11
        ELSE
            IF r > 10 THEN
                sum = sum + 10
            ELSE
                sum = sum + r
            END IF
        END IF
        k = k + 1
    WEND
    ' A 从 11 降成 1：每降一张少 10 点，降够就停
    ' ⚠ 用**标志位**收尾而不是 `EXIT WHILE` —— 后者在这个前端上实测**退不出来**
    '   （自检卡死在第一个 `handValue` 调用上，整轮超时）。
    WHILE sum > 21
        IF aces > 0 THEN
            aces = aces - 1
            sum = sum - 10
        ELSE
            EXIT WHILE
        END IF
    WEND
    handValue = sum
END FUNCTION

' 天生 21 点（只有两张）
FUNCTION isBlackjack(off AS INTEGER, n AS INTEGER) AS INTEGER
    IF n = 2 THEN
        IF handValue(off, n) = 21 THEN
            isBlackjack = 1
            EXIT FUNCTION
        END IF
    END IF
    isBlackjack = 0
END FUNCTION

' 庄家该不该继续要牌
FUNCTION dealerHits(v AS INTEGER) AS INTEGER
    IF v < DEALER_STOP THEN
        dealerHits = 1
    ELSE
        dealerHits = 0
    END IF
END FUNCTION

' 比大小
FUNCTION compare(pv AS INTEGER, dv AS INTEGER) AS INTEGER
    IF pv > 21 THEN
        compare = LOSE
        EXIT FUNCTION
    END IF
    IF dv > 21 THEN
        compare = WIN
        EXIT FUNCTION
    END IF
    IF pv > dv THEN
        compare = WIN
    ELSE
        IF pv < dv THEN
            compare = LOSE
        ELSE
            compare = PUSH
        END IF
    END IF
END FUNCTION

' ══════════════════════════════════════════════════════════════════════════
'  牌堆与发牌
' ══════════════════════════════════════════════════════════════════════════

SUB shuffle()
    DIM k AS INTEGER
    DIM s AS INTEGER
    k = 0
    WHILE k < DECKN
        deck(k) = k
        k = k + 1
    WEND
    ' Fisher-Yates：从后往前，与随机位置交换
    k = DECKN - 1
    WHILE k > 0
        s = ui_rand(k + 1)
        tmp = deck(k)
        deck(k) = deck(s)
        deck(s) = tmp
        k = k - 1
    WEND
    di = 0
END SUB

' 发一张（牌不够就重洗 —— 手机上一局打不了那么多轮，但别让它越界）
FUNCTION deal() AS INTEGER
    IF di >= DECKN THEN
        shuffle()
    END IF
    deal = deck(di)
    di = di + 1
END FUNCTION

SUB dealRound()
    pn = 0
    dn = 0
    hand(0) = deal()
    hand(12) = deal()
    hand(1) = deal()
    hand(13) = deal()
    pn = 2
    dn = 2
    dealerHidden = 1
    doubled = 0
    result = 0
    phase = PH_PLAY
    msg = "要牌 / 停牌 / 加倍？"
    IF isBlackjack(0, pn) = 1 THEN
        settle()
    END IF
END SUB

SUB playerHit()
    IF pn >= MAXCARD THEN
        EXIT SUB
    END IF
    hand(pn) = deal()
    pn = pn + 1
    ui_beep(880, 25)
    IF handValue(0, pn) > 21 THEN
        settle()
    END IF
END SUB

SUB playerStand()
    dealerHidden = 0
    phase = PH_DEALER
END SUB

SUB playerDouble()
    IF chips < bet THEN
        msg = "筹码不够加倍"
        ui_beep(200, 120)
        EXIT SUB
    END IF
    chips = chips - bet
    bet = bet * 2
    doubled = 1
    hand(pn) = deal()
    pn = pn + 1
    ui_beep(1200, 40)
    playerStand()
END SUB

' 庄家一路要到 17 点
SUB dealerPlay()
    WHILE dealerHits(handValue(12, dn)) = 1
        IF dn >= MAXCARD THEN
            EXIT WHILE
        END IF
        hand(12 + dn) = deal()
        dn = dn + 1
    WEND
    settle()
END SUB

' 结算
SUB settle()
    DIM pv AS INTEGER
    DIM dv AS INTEGER
    dealerHidden = 0
    pv = handValue(0, pn)
    dv = handValue(12, dn)

    IF isBlackjack(0, pn) = 1 THEN
        IF isBlackjack(12, dn) = 1 THEN
            result = PUSH
        ELSE
            result = WIN
            chips = chips + bet * BJPAY_N / BJPAY_D
        END IF
    ELSE
        result = compare(pv, dv)
        IF result = WIN THEN
            chips = chips + bet
        END IF
        IF result = PUSH THEN
            chips = chips + bet
        END IF
    END IF

    SELECT CASE result
        CASE WIN
            msg = "你赢了！+" + STR$(bet)
            ui_beep(1318, 120)
            ui_vibrate(60, 120)
        CASE LOSE
            msg = "庄家赢 " + STR$(pv) + " : " + STR$(dv)
            ui_beep(220, 220)
            ui_vibrate(180, 200)
        CASE PUSH
            msg = "和局，退还赌注"
            ui_beep(660, 90)
        CASE ELSE
            msg = "（未结算）"
    END SELECT

    phase = PH_OVER
    IF chips <= 0 THEN
        msg = "筹码输光了 —— 点【重开】再来"
    END IF
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  绘制
' ══════════════════════════════════════════════════════════════════════════

' 点数 → 显示文本
FUNCTION rankText(r AS INTEGER) AS STRING
    SELECT CASE r
        CASE 1
            rankText = "A"
        CASE 11
            rankText = "J"
        CASE 12
            rankText = "Q"
        CASE 13
            rankText = "K"
        CASE ELSE
            rankText = STR$(r)
    END SELECT
END FUNCTION

FUNCTION suitText(s AS INTEGER) AS STRING
    SELECT CASE s
        CASE 0
            suitText = "S"
        CASE 1
            suitText = "H"
        CASE 2
            suitText = "D"
        CASE ELSE
            suitText = "C"
    END SELECT
END FUNCTION

FUNCTION suitColor(s AS INTEGER) AS INTEGER
    IF s = 1 THEN
        suitColor = C_RED
        EXIT FUNCTION
    END IF
    IF s = 2 THEN
        suitColor = C_RED
        EXIT FUNCTION
    END IF
    suitColor = C_DARK
END FUNCTION

' 画一张牌。faceDown=1 时画牌背。
SUB drawCard(x AS INTEGER, y AS INTEGER, c AS INTEGER, faceDown AS INTEGER)
    IF faceDown = 1 THEN
        ui_rect(x, y, CARDW, CARDH, C_CARDBK, 1, 0, 7)
        ui_rect(x + 6, y + 6, CARDW - 12, CARDH - 12, C_DARK, 1, 0, 5)
        RETURN
    END IF
    ui_rect(x, y, CARDW, CARDH, C_CARD, 1, 0, 7)
    ui_text(x + 7, y + 6, rankText(rankOf(c)), suitColor(suitOf(c)), 20, 0)
    ui_text(x + CARDW / 2, y + CARDH - 26, suitText(suitOf(c)), suitColor(suitOf(c)), 18, 1)
END SUB

SUB drawHand(off AS INTEGER, n AS INTEGER, y AS INTEGER, hideFirst AS INTEGER)
    DIM k AS INTEGER
    DIM x AS INTEGER
    DIM overlap AS INTEGER
    DIM total AS INTEGER
    DIM startX AS INTEGER

    ' 手牌多了就压着叠 —— 别让它画出屏幕
    overlap = CARDW + 6
    total = n * overlap
    IF total > sw - 24 THEN
        IF n > 1 THEN
            overlap = (sw - 24) / n
        END IF
    END IF
    total = n * overlap
    startX = (sw - total) / 2
    IF startX < 8 THEN
        startX = 8
    END IF

    k = 0
    WHILE k < n
        x = startX + k * overlap
        IF k = 0 THEN
            IF hideFirst = 1 THEN
                drawCard(x, y, hand(off + k), 1)
            ELSE
                drawCard(x, y, hand(off + k), 0)
            END IF
        ELSE
            drawCard(x, y, hand(off + k), 0)
        END IF
        k = k + 1
    WEND
END SUB

SUB drawHud()
    ui_rect(0, 0, sw, HUDH, C_HUD, 1, 0, 0)
    ui_text(12, 10, "筹码", C_DIM, 13, 0)
    ui_text(12, 28, STR$(chips), C_GOLD, 22, 0)
    ui_text(sw / 2, 10, "赌注", C_DIM, 13, 1)
    ui_text(sw / 2, 28, STR$(bet), C_GOLD, 22, 1)

    IF phase = PH_PLAY THEN
        txt = "?"
        IF dealerHidden = 0 THEN
            txt = STR$(handValue(12, dn))
        END IF
        ui_text(sw - 12, 10, "庄家", C_DIM, 13, 2)
        ui_text(sw - 12, 28, txt, C_TEXT, 22, 2)
    ELSE
        ui_text(sw - 12, 10, "玩家", C_DIM, 13, 2)
        ui_text(sw - 12, 28, STR$(handValue(0, pn)), C_TEXT, 22, 2)
    END IF
END SUB

SUB drawTable()
    DIM dealerY AS INTEGER
    DIM playerY AS INTEGER
    dealerY = HUDH + 44
    playerY = sh - 300

    ui_text(sw / 2, HUDH + 12, "庄家", C_DIM, 13, 1)
    drawHand(12, dn, dealerY, dealerHidden)

    ui_text(sw / 2, playerY - 22, "你", C_DIM, 13, 1)
    drawHand(0, pn, playerY, 0)
END SUB

SUB drawButton(x AS INTEGER, y AS INTEGER, w AS INTEGER, label AS STRING, enabled AS INTEGER)
    IF enabled = 1 THEN
        ui_rect(x, y, w, bwh, C_BTN, 1, 0, 12)
    ELSE
        ui_rect(x, y, w, bwh, C_FLAT, 1, 0, 12)
    END IF
    ui_text(x + w / 2, y + 18, label, C_TEXT, 18, 1)
END SUB

SUB drawControls()
    ui_rect(0, ctlY, sw, sh - ctlY, C_HUD, 1, 0, 0)
    ui_rect(0, ctlY, sw, 2, C_FLAT, 1, 0, 0)

    IF phase = PH_BET THEN
        DIM canMinus AS INTEGER
        canMinus = 0
        IF bet > 10 THEN
            canMinus = 1
        END IF
        drawButton(b1x, b1y, bw, "−", canMinus)
        drawButton(b2x, b2y, bw, "+", 1)
        drawButton(b3x, b3y, sw - 48, "发 牌", 1)
        RETURN
    END IF

    IF phase = PH_PLAY THEN
        drawButton(b1x, b1y, bw, "要牌", 1)
        drawButton(b2x, b2y, bw, "停牌", 1)
        DIM canDouble AS INTEGER
        canDouble = 0
        IF chips >= bet THEN
            canDouble = 1
        END IF
        IF pn > 2 THEN
            canDouble = 0
        END IF
        drawButton(b3x, b3y, sw - 48, "加倍", canDouble)
        RETURN
    END IF

    drawButton(b1x, b1y, sw - 48, "重 开", 1)
END SUB

SUB drawScene()
    ui_clear(C_FELT)
    ' 桌面感：中间一道深色带
    ui_rect(0, sh / 2 - 40, sw, 80, C_FELT_D, 1, 0, 0)
    drawHud()
    drawTable()
    ui_text(sw / 2, ctlY - 30, msg, C_GOLD, 15, 1)
    drawControls()
    ui_present()
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  输入
' ══════════════════════════════════════════════════════════════════════════
FUNCTION inRect(x AS INTEGER, y AS INTEGER, w AS INTEGER, h AS INTEGER, px AS INTEGER, py AS INTEGER) AS INTEGER
    DIM ok AS INTEGER
    ok = 0
    IF px >= x THEN
        IF px <= x + w THEN
            IF py >= y THEN
                IF py <= y + h THEN
                    ok = 1
                END IF
            END IF
        END IF
    END IF
    inRect = ok
END FUNCTION

SUB newRound()
    bet = 10
    IF bet > chips THEN
        bet = chips
    END IF
    phase = PH_BET
    msg = "下注 —— 调好赌注点【发牌】"
    pn = 0
    dn = 0
END SUB

SUB handlePoint(isDown AS INTEGER)
    IF isDown = 0 THEN
        EXIT SUB
    END IF
    rx = ui_msg_a()
    ry = ui_msg_b()

    IF phase = PH_BET THEN
        IF inRect(b1x, b1y, bw, bwh, rx, ry) = 1 THEN
            bet = bet - 10
            IF bet < 10 THEN
                bet = 10
            END IF
            IF bet > chips THEN
                bet = chips
            END IF
            ui_beep(520, 20)
            EXIT SUB
        END IF
        IF inRect(b2x, b2y, bw, bwh, rx, ry) = 1 THEN
            bet = bet + 10
            IF bet > chips THEN
                bet = chips
            END IF
            ui_beep(760, 20)
            EXIT SUB
        END IF
        IF inRect(b3x, b3y, sw - 48, bwh, rx, ry) = 1 THEN
            IF chips > 0 THEN
                chips = chips - bet
                shuffle()
                dealRound()
                ui_beep(980, 40)
            END IF
        END IF
        EXIT SUB
    END IF

    IF phase = PH_PLAY THEN
        IF inRect(b1x, b1y, bw, bwh, rx, ry) = 1 THEN
            playerHit()
            EXIT SUB
        END IF
        IF inRect(b2x, b2y, bw, bwh, rx, ry) = 1 THEN
            playerStand()
            EXIT SUB
        END IF
        IF inRect(b3x, b3y, sw - 48, bwh, rx, ry) = 1 THEN
            IF pn = 2 THEN
                IF chips >= bet THEN
                    playerDouble()
                END IF
            END IF
        END IF
        EXIT SUB
    END IF

    IF phase = PH_OVER THEN
        IF inRect(b1x, b1y, sw - 48, bwh, rx, ry) = 1 THEN
            IF chips <= 0 THEN
                chips = 200
                msg = "重新给你 200 筹码"
            END IF
            newRound()
            ui_beep(880, 40)
        END IF
    END IF
END SUB

SUB handleKey()
    DIM k AS INTEGER
    k = ui_msg_a()
    IF k = 27 THEN
        quit = 1
    END IF
    IF phase = PH_PLAY THEN
        IF k = 72 THEN
            playerHit()
        END IF
        IF k = 83 THEN
            playerStand()
        END IF
        IF k = 68 THEN
            IF pn = 2 THEN
                IF chips >= bet THEN
                    playerDouble()
                END IF
            END IF
        END IF
    END IF
    IF k = 13 THEN
        IF phase = PH_OVER THEN
            IF chips <= 0 THEN
                chips = 200
            END IF
            newRound()
        END IF
        IF phase = PH_BET THEN
            IF chips > 0 THEN
                chips = chips - bet
                shuffle()
                dealRound()
            END IF
        END IF
    END IF
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  主循环
' ══════════════════════════════════════════════════════════════════════════
SUB runGame()
    ui_keep_on(1)

    ctlY = sh - 190
    bwh = 56
    bw = (sw - 72) / 2
    b1x = 24
    b2x = 24 + bw + 24
    b3x = 24
    b1y = ctlY + 20
    b2y = ctlY + 20
    b3y = ctlY + 92

    chips = 200
    newRound()

    WHILE ui_win_closed() = 0
        IF quit = 1 THEN
            ui_win_close()
        END IF

        mt = ui_wait_msg(60)
        n = 0
        WHILE mt <> 0
            IF mt = 10 THEN
                quit = 1
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
            IF phase = PH_DEALER THEN
                dealerPlay()
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
    PRINT "── 21 点 · 规则自检 ──"

    ' 编码往返
    PRINT "  牌 30 的点数/花色（应 7 / 2）:"; rankOf(30); suitOf(30)

    ' 手牌点数：A 的两面性
    hand(0) = 1 * 4 + 0
    pn = 1
    PRINT "  单张 A（应 11）:"; handValue(0, pn)
    hand(1) = 13 * 4 + 0
    pn = 2
    PRINT "  A + K（应 21，天生 21 点应 1）:"; handValue(0, pn); isBlackjack(0, pn)
    hand(1) = 9 * 4 + 0
    pn = 2
    PRINT "  A + 9（应 20）:"; handValue(0, pn)
    hand(1) = 13 * 4 + 0
    hand(2) = 13 * 4 + 0
    pn = 3
    PRINT "  A + K + K（应 21 —— A 降成 1）:"; handValue(0, pn)
    hand(2) = 5 * 4 + 0
    pn = 3
    PRINT "  A + K + 5（应 16 —— A 仍按 11）:"; handValue(0, pn)

    ' 爆牌
    hand(0) = 10 * 4 + 0
    hand(1) = 10 * 4 + 0
    hand(2) = 5 * 4 + 0
    pn = 3
    PRINT "  10+10+5（应 25，已爆）:"; handValue(0, pn)

    ' 庄家规则
    PRINT "  庄家 16 要不要牌（应 1）:"; dealerHits(16)
    PRINT "  庄家 17 要不要牌（应 0）:"; dealerHits(17)
    PRINT "  庄家 21 要不要牌（应 0）:"; dealerHits(21)

    ' 比大小
    PRINT "  玩家 20 庄家 19（应 1=赢）:"; compare(20, 19)
    PRINT "  玩家 18 庄家 19（应 2=输）:"; compare(18, 19)
    PRINT "  玩家 19 庄家 19（应 3=和）:"; compare(19, 19)
    PRINT "  玩家爆牌 22 庄家 5（应 2=输）:"; compare(22, 5)
    PRINT "  庄家爆牌 22 玩家 5（应 1=赢）:"; compare(5, 22)

    ' 发牌不重样：洗一副，前 10 张互不相同
    shuffle()
    DIM dup AS INTEGER
    dup = 0
    i = 0
    WHILE i < 10
        j = i + 1
        WHILE j < 10
            IF deck(i) = deck(j) THEN
                dup = dup + 1
            END IF
            j = j + 1
        WEND
        i = i + 1
    WEND
    PRINT "  洗牌后前 10 张的重复对数（应 0）:"; dup
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

wh = ui_win_open_ex("21 点", sw, sh, 0, 0)

IF wh < 1 THEN
    PRINT "（桌面脚手架：没有真窗口，规则自检打完就退出）"
ELSE
    runGame()
END IF
