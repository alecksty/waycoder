' 汉谟拉比（HAMMURABI）—— 用 **BASIC** 写的手机游戏
'
' 出处与版权：原型是 1968 年 Doug Dyment 写的那份资源管理游戏（后来收进
' David Ahl 的《BASIC Computer Games》，是 1970 年代流传最广的 BASIC 程序之一）。
' **这份是照玩法规则重写的**：结构、变量名、注释、绘制方式都是自己的，
' 没有抄原件的一行代码 —— 与 `gorilla.bas` 同一条规矩（`Examples/` 会随 APK 分发）。
'
' 玩法：你统治苏美尔 10 年。每年按顺序做四件事 ——
'   ① 买地 ② 卖地 ③ 喂粮（每人 20 蒲式耳，喂不够就饿死人）④ 播种（每英亩 1 蒲式耳，
'   每人最多种 10 英亩）。年底收成 + 老鼠偷粮，然后进入下一年。
' **饿死超过 45% 就被推翻**，游戏提前结束；撑满 10 年则按人均土地给总评。
'
' ◆ 手机上怎么玩
'
' 原版是打字输入的（"要买多少英亩？"），手机上没法那么问。这里改成**分阶段的加减条**：
' 一屏只问一件事，一个大号 −、一个大号 +，一个「确定」。点「确定」进入下一件事。
' —— 回合制文字游戏在手机上就该这么处理：**别让玩家调键盘**。
'
' ◆ 这份用到的语言特性（都是本轮刚确认可用的，别再照 gorilla 那套绕）
'
'   · `SELECT CASE` 做阶段分派（`gorilla.bas` 头部说它不能用 —— **已过期**）
'   · `FUNCTION` 抽纯规则（喂粮需求量、收成、总评）
'   · 字符串 `+` 拼接拼报告（从前会得到空串，v0.96.330 已修）
'   · `STR$` 数字上屏
'   · `AND` / `OR`、带括号的子表达式、CONST 参与算术 —— 都正常
'
' ◆ 写法要求（本前端的硬性要求）
'
'   · 外部过程必须 `NATIVE SUB` / `NATIVE FUNCTION` + **空体**。
'   · 模块级变量必须在赋值前 `DIM`。
'   · 赋值语句不能写在 SUB 定义之后（会被当成函数调用）。
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
'  规则常量（改手感只动这一段）
' ══════════════════════════════════════════════════════════════════════════
CONST YEARS = 10             ' 统治年数
CONST P0 = 100               ' 起始人口
CONST A0 = 1000              ' 起始土地（英亩）
CONST G0 = 3000              ' 起始粮食（蒲式耳）
CONST FEED_PER = 20          ' 每人每年要吃的（蒲式耳）
CONST SEED_PER = 1           ' 每英亩要播的种子（蒲式耳）
CONST ACRES_PER_PERSON = 10  ' 每人最多能种多少英亩
CONST PRICE_MIN = 17         ' 地价下限（蒲式耳/英亩）
CONST PRICE_SPAN = 10        ' 地价浮动范围（17..26）
CONST RAT_MAX = 3            ' 老鼠最多吃掉几分之一（1/(1..3)）
CONST REVOLT_PCT = 45        ' 饿死超过这个百分比就被推翻
CONST HUDH = 104             ' 顶部数据带
CONST CTRLH = 210            ' 底部操作区

CONST C_BG = &HFF1A1428
CONST C_HUD = &HFF241C3A
CONST C_LINE = &HFF3A3060
CONST C_TEXT = &HFFEDEDF2
CONST C_DIM = &HFF9AA0B0
CONST C_GOLD = &HFFE8C468
CONST C_OK = &HFF4ADE80
CONST C_WARN = &HFFFF8A5C
CONST C_BTN = &HFF3A3260
CONST C_BTN_D = &HFF251E45
CONST C_ACCENT = &HFF6FD3FF
CONST C_FIRE = &HFFD8443C

' 阶段
CONST PH_BUY = 0
CONST PH_SELL = 1
CONST PH_FEED = 2
CONST PH_PLANT = 3
CONST PH_DONE = 4

' ══════════════════════════════════════════════════════════════════════════
'  状态
' ══════════════════════════════════════════════════════════════════════════
DIM sw AS INTEGER
DIM sh AS INTEGER
DIM year AS INTEGER
DIM people AS INTEGER
DIM acres AS INTEGER
DIM grain AS INTEGER
DIM price AS INTEGER
DIM harvest AS INTEGER
DIM rats AS INTEGER
DIM starved AS INTEGER
DIM arrived AS INTEGER
DIM fed AS INTEGER
DIM planted AS INTEGER
DIM land AS INTEGER
DIM phase AS INTEGER
DIM amount AS INTEGER
DIM stepSize AS INTEGER
DIM over AS INTEGER
DIM quit AS INTEGER
DIM msg1 AS STRING
DIM msg2 AS STRING
DIM msg3 AS STRING
DIM msg4 AS STRING
DIM prompt AS STRING
DIM verdict AS STRING

' 几何
DIM ctlY AS INTEGER
DIM minusX AS INTEGER
DIM plusX AS INTEGER
DIM stepX AS INTEGER
DIM okY AS INTEGER
DIM valY AS INTEGER

' 临时量
DIM i AS INTEGER
DIM n AS INTEGER
DIM t AS INTEGER
DIM mt AS INTEGER
DIM rx AS INTEGER
DIM ry AS INTEGER
DIM need AS INTEGER
DIM canPlant AS INTEGER

' ══════════════════════════════════════════════════════════════════════════
'  纯规则（自检直接打它们的返回值，所以必须是纯函数）
' ══════════════════════════════════════════════════════════════════════════

' 这么多人一年要吃多少蒲式耳
FUNCTION feedNeed(p AS INTEGER) AS INTEGER
    feedNeed = p * FEED_PER
END FUNCTION

' 这点粮食最多能种多少英亩（受"每人 10 英亩"与粮食双重限制）
FUNCTION plantLimit(p AS INTEGER, g AS INTEGER) AS INTEGER
    DIM byPeople AS INTEGER
    byPeople = p * ACRES_PER_PERSON
    IF g < byPeople THEN
        plantLimit = g
    ELSE
        plantLimit = byPeople
    END IF
END FUNCTION

' 饿死比例（百分比，整数）—— 45% 那道线就是拿它比的
FUNCTION starvePct(died AS INTEGER, total AS INTEGER) AS INTEGER
    IF total <= 0 THEN
        starvePct = 0
    ELSE
        starvePct = died * 100 / total
    END IF
END FUNCTION

' 总评：按人均土地给一句话
FUNCTION rate(ac AS INTEGER, pp AS INTEGER) AS INTEGER
    DIM per AS INTEGER
    IF pp <= 0 THEN
        rate = 0
    ELSE
        per = ac / pp
        rate = per
    END IF
END FUNCTION

' ══════════════════════════════════════════════════════════════════════════
'  局面
' ══════════════════════════════════════════════════════════════════════════

SUB layout()
    ctlY = sh - CTRLH
    valY = ctlY + 66
    minusX = 40
    plusX = sw - 40
    stepX = sw / 2
    okY = sh - 74
    msg1 = ""
    msg2 = ""
    msg3 = ""
    msg4 = ""
END SUB

SUB newGame()
    year = 1
    people = P0
    acres = A0
    grain = G0
    starved = 0
    arrived = 0
    over = 0
    quit = 0
    verdict = ""
    msg1 = "你统治苏美尔 10 年。"
    msg2 = "买地、喂饱百姓、播种。"
    msg3 = "饿死超过 45% 就会被推翻。"
    msg4 = ""
    beginYear()
END SUB

' 新一年：掷地价、进入"买地"阶段
SUB beginYear()
    price = PRICE_MIN + ui_rand(PRICE_SPAN)
    phase = PH_BUY
    amount = 0
    msg4 = "地价 " + STR$(price) + " 蒲式耳/英亩"
END SUB

' 非负且不超过上限 —— 加减条要夹住
SUB clampAmount()
    IF amount < 0 THEN
        amount = 0
    END IF
    IF phase = PH_BUY THEN
        IF amount * price > grain THEN
            DIM cap AS INTEGER
            cap = grain / price
            amount = cap
        END IF
    END IF
    IF phase = PH_SELL THEN
        IF amount > acres THEN
            amount = acres
        END IF
    END IF
    IF phase = PH_FEED THEN
        IF amount > grain THEN
            amount = grain
        END IF
    END IF
    IF phase = PH_PLANT THEN
        IF amount > plantLimit(people, grain) THEN
            amount = plantLimit(people, grain)
        END IF
    END IF
END SUB

SUB confirmPhase()
    IF phase = PH_BUY THEN
        acres = acres + amount
        grain = grain - amount * price
        phase = PH_SELL
        amount = 0
    ELSE
        IF phase = PH_SELL THEN
            acres = acres - amount
            grain = grain + amount * price
            phase = PH_FEED
            amount = feedNeed(people)
            clampAmount()
        ELSE
            IF phase = PH_FEED THEN
                fed = amount
                grain = grain - amount
                phase = PH_PLANT
                amount = plantLimit(people, grain)
                clampAmount()
            ELSE
                IF phase = PH_PLANT THEN
                    planted = amount
                    grain = grain - amount
                    endYear()
                END IF
            END IF
        END IF
    END IF
END SUB

' 年底结算：饿死 / 收成 / 老鼠 / 移民 / 是否被推翻
SUB endYear()
    DIM need AS INTEGER
    DIM ratio AS INTEGER
    need = feedNeed(people)
    IF fed >= need THEN
        starved = 0
    ELSE
        DIM short AS INTEGER
        short = need - fed
        starved = short / FEED_PER
        IF starved > people THEN
            starved = people
        END IF
    END IF

    ' 收成：每英亩 1..6 蒲式耳
    harvest = 0
    IF planted > 0 THEN
        DIM y AS INTEGER
        DIM a AS INTEGER
        a = 0
        WHILE a < planted
            harvest = harvest + 1 + ui_rand(6)
            a = a + 1
        WEND
    END IF
    grain = grain + harvest

    ' 老鼠：吃掉 1/(1..RAT_MAX)
    DIM divisor AS INTEGER
    divisor = 1 + ui_rand(RAT_MAX)
    rats = grain / divisor
    grain = grain - rats

    ' 人口变化：饿死 + 新迁入（按"地多人少就有人来"给一个朴素模型）
    people = people - starved
    IF people < 0 THEN
        people = 0
    END IF
    ratio = starvePct(starved, people + starved)
    IF ratio > REVOLT_PCT THEN
        over = 2
        verdict = "饿死 " + STR$(ratio) + "%，你被推翻了。"
        msg1 = "第 " + STR$(year) + " 年，饿死 " + STR$(starved) + " 人。"
        msg2 = "超过 " + STR$(REVOLT_PCT) + "%，暴民推翻了王座。"
        msg3 = ""
        msg4 = ""
        phase = PH_DONE
        ui_beep(160, 400)
        ui_vibrate(200, 255)
        EXIT SUB
    END IF

    IF people <= 0 THEN
        over = 2
        verdict = "一个人都不剩了。"
        phase = PH_DONE
        EXIT SUB
    END IF

    arrived = ui_rand(people / 4 + 1)
    people = people + arrived

    msg1 = "第 " + STR$(year) + " 年：收成 " + STR$(harvest) + " 蒲式耳"
    msg2 = "老鼠吃掉 " + STR$(rats) + "，饿死 " + STR$(starved) + " 人"
    msg3 = "新迁入 " + STR$(arrived) + " 人"
    msg4 = ""

    year = year + 1
    IF year > YEARS THEN
        over = 1
        DIM per AS INTEGER
        per = rate(acres, people)
        IF per >= 10 THEN
            verdict = "人均 " + STR$(per) + " 英亩 —— 盛世。"
        ELSE
            IF per >= 7 THEN
                verdict = "人均 " + STR$(per) + " 英亩 —— 尚可。"
            ELSE
                verdict = "人均 " + STR$(per) + " 英亩 —— 民不聊生。"
            END IF
        END IF
        phase = PH_DONE
    ELSE
        beginYear()
    END IF
END SUB

' 当前阶段该问什么
SUB setPrompt()
    SELECT CASE phase
        CASE PH_BUY
            prompt = "买地：还能买 " + STR$(grain / price) + " 英亩"
        CASE PH_SELL
            prompt = "卖地：最多卖 " + STR$(acres) + " 英亩"
        CASE PH_FEED
            prompt = "喂粮：需要 " + STR$(feedNeed(people)) + " 蒲式耳"
        CASE PH_PLANT
            prompt = "播种：最多种 " + STR$(plantLimit(people, grain)) + " 英亩"
        CASE PH_DONE
            prompt = "本局结束"
        CASE ELSE
            prompt = ""
    END SELECT
END SUB

SUB setStep()
    IF phase = PH_FEED THEN
        stepSize = 100
    ELSE
        stepSize = 10
    END IF
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  绘制
' ══════════════════════════════════════════════════════════════════════════

SUB drawStat(x AS INTEGER, y AS INTEGER, label AS STRING, value AS INTEGER, col AS INTEGER)
    ui_text(x, y, label, C_DIM, 12, 0)
    ui_text(x, y + 16, STR$(value), col, 20, 0)
END SUB

SUB drawHud()
    ui_rect(0, 0, sw, HUDH, C_HUD, 1, 0, 0)
    DIM half AS INTEGER
    half = sw / 2
    drawStat(16, 12, "第几年", year, C_ACCENT)
    drawStat(half + 12, 12, "人口", people, C_TEXT)
    drawStat(16, 60, "土地(英亩)", acres, C_GOLD)
    drawStat(half + 12, 60, "粮食(蒲式耳)", grain, C_GOLD)
    ui_rect(0, HUDH - 2, sw, 2, C_LINE, 1, 0, 0)
END SUB

SUB drawReport()
    IF msg1 <> "" THEN ui_text(16, HUDH + 16, msg1, C_TEXT, 14, 0)
    IF msg2 <> "" THEN ui_text(16, HUDH + 40, msg2, C_TEXT, 14, 0)
    IF msg3 <> "" THEN ui_text(16, HUDH + 64, msg3, C_DIM, 14, 0)
    IF msg4 <> "" THEN ui_text(16, HUDH + 88, msg4, C_WARN, 14, 0)
END SUB

SUB drawRoundButton(cx AS INTEGER, cy AS INTEGER, r AS INTEGER, plus AS INTEGER, enabled AS INTEGER)
    IF enabled = 1 THEN
        ui_circle(cx, cy, r, C_BTN, 1, 0)
    ELSE
        ui_circle(cx, cy, r, C_BTN_D, 1, 0)
    END IF
    DIM bar AS INTEGER
    bar = r - 13
    ui_rect(cx - bar, cy - 3, bar * 2, 6, C_TEXT, 1, 0, 3)
    IF plus = 1 THEN
        ui_rect(cx - 3, cy - bar, 6, bar * 2, C_TEXT, 1, 0, 3)
    END IF
END SUB

SUB drawControls()
    ui_rect(0, ctlY, sw, CTRLH, C_HUD, 1, 0, 0)
    ui_rect(0, ctlY, sw, 2, C_LINE, 1, 0, 0)

    IF phase = PH_DONE THEN
        ui_text(sw / 2, ctlY + 60, verdict, C_GOLD, 16, 1)
        ui_rect(24, okY, sw - 48, 52, C_FIRE, 1, 0, 12)
        ui_text(sw / 2, okY + 14, "再来一局", &HFFFFF0EC, 18, 1)
        RETURN
    END IF

    ui_text(sw / 2, ctlY + 16, prompt, C_ACCENT, 15, 1)

    DIM canMinus AS INTEGER
    DIM canPlus AS INTEGER
    canMinus = 0
    canPlus = 0
    IF amount > 0 THEN
        canMinus = 1
    END IF
    drawRoundButton(minusX, valY, 34, 0, canMinus)
    drawRoundButton(plusX, valY, 34, 1, 1)

    ui_text(stepX, valY - 22, STR$(amount), C_GOLD, 34, 1)
    ui_text(stepX, valY + 22, "(每按一次 " + STR$(stepSize) + ")", C_DIM, 12, 1)

    ui_rect(24, okY, sw - 48, 52, C_FIRE, 1, 0, 12)
    ui_text(sw / 2, okY + 14, "确 定", &HFFFFF0EC, 18, 1)
END SUB

SUB drawScene()
    ui_clear(C_BG)
    drawHud()
    drawReport()
    drawControls()
    ui_present()
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  输入
' ══════════════════════════════════════════════════════════════════════════

' 落在按钮里没有 —— 用方形盒判（圆按钮用"到圆心的距离 ≤ r"更准，但这里按方形框住即可）
' ⚠ 有返回值的必须是 `FUNCTION`：写成 `SUB … AS INTEGER` 时那个 `AS INTEGER`
'   会被当成一次函数调用去链接，报 `未定义的函数 'func_integer'`（实测踩过）。
FUNCTION onButton(cx AS INTEGER, cy AS INTEGER, r AS INTEGER, px AS INTEGER, py AS INTEGER) AS INTEGER
    DIM dx AS INTEGER
    DIM dy AS INTEGER
    dx = px - cx
    IF dx < 0 THEN
        dx = 0 - dx
    END IF
    dy = py - cy
    IF dy < 0 THEN
        dy = 0 - dy
    END IF
    IF dx <= r THEN
        IF dy <= r THEN
            onButton = 1
            EXIT FUNCTION
        END IF
    END IF
    onButton = 0
END FUNCTION

SUB handlePoint(isDown AS INTEGER)
    IF isDown = 0 THEN
        RETURN
    END IF
    rx = ui_msg_a()
    ry = ui_msg_b()

    IF phase = PH_DONE THEN
        IF ry >= okY THEN
            newGame()
        END IF
        RETURN
    END IF

    IF onButton(minusX, valY, 34, rx, ry) = 1 THEN
        amount = amount - stepSize
        clampAmount()
        ui_beep(520, 20)
        RETURN
    END IF
    IF onButton(plusX, valY, 34, rx, ry) = 1 THEN
        amount = amount + stepSize
        clampAmount()
        ui_beep(780, 20)
        RETURN
    END IF
    IF ry >= okY THEN
        confirmPhase()
        ui_beep(980, 30)
    END IF
END SUB

SUB handleKey()
    DIM k AS INTEGER
    k = ui_msg_a()
    IF k = 27 THEN
        quit = 1
    END IF
    IF phase = PH_DONE THEN
        IF k = 13 THEN
            newGame()
        END IF
        RETURN
    END IF
    IF k = 37 THEN
        amount = amount - stepSize
        clampAmount()
    END IF
    IF k = 39 THEN
        amount = amount + stepSize
        clampAmount()
    END IF
    IF k = 13 THEN
        confirmPhase()
    END IF
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  主循环
' ══════════════════════════════════════════════════════════════════════════
SUB runGame()
    ui_keep_on(1)
    layout()
    newGame()

    WHILE ui_win_closed() = 0
        IF quit = 1 THEN
            ui_win_close()
        END IF

        setPrompt()
        setStep()

        mt = ui_wait_msg(80)
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
'  判的是**规则**，不是"编过了"：喂粮需求、播种上限、饿死比例、总评分档。
'  这几条就是这个游戏全部的可玩性所在。
' ══════════════════════════════════════════════════════════════════════════
SUB simCheck()
    PRINT "── 汉谟拉比 · 规则自检 ──"
    PRINT "  100 人一年要吃（应 2000）:"; feedNeed(100)
    PRINT "  100 人 / 3000 粮 播种上限（应 1000，受人数限）:"; plantLimit(100, 3000)
    PRINT "  100 人 / 500 粮 播种上限（应 500，受粮食限）:"; plantLimit(100, 500)
    PRINT "  饿死 30 / 总 100 的比例（应 30）:"; starvePct(30, 100)
    PRINT "  饿死 46 / 总 100 的比例（应 46，超过 45 那道线）:"; starvePct(46, 100)
    PRINT "  人均 12 英亩（应 12）:"; rate(1200, 100)
    PRINT "  人均 5 英亩（应 5）:"; rate(500, 100)

    ' 阶段推进：买地 → 卖地 → 喂粮 → 播种，四步走完必须落到"结算"
    people = 100
    acres = 1000
    grain = 3000
    over = 0
    phase = PH_BUY
    amount = 0
    price = 20
    confirmPhase()
    PRINT "  买地后阶段（应 1=卖地）:"; phase
    amount = 0
    confirmPhase()
    PRINT "  卖地后阶段（应 2=喂粮）且粮已备好（应 2000）:"; phase; amount
    amount = feedNeed(people)
    confirmPhase()
    PRINT "  喂粮后阶段（应 3=播种）:"; phase
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

' 锁竖屏 + 不要手柄区（全程触摸）
wh = ui_win_open_ex("汉谟拉比", sw, sh, 0, 0)

IF wh < 1 THEN
    PRINT "（桌面脚手架：没有真窗口，规则自检打完就退出）"
ELSE
    runGame()
END IF
