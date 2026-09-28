' 登月（LUNAR LANDER）—— 用 **BASIC** 写的手机游戏
' Lunar lander -- a mobile game written in **BASIC**
'
' 出处与版权：原型是 1969 年那版文字登月（后来收进 David Ahl 的《BASIC Computer Games》，
' Origin and copyright: the prototype is the 1969 text-mode lunar landing game (later collected into David Ahl's "BASIC Computer Games",
' 前身是 1969 年 7 月登月前后流传的 "Lunar Landing Game"）。**这份是照玩法规则重写的**：
' whose predecessor was the "Lunar Landing Game" circulating around the July 1969 moon landing). **This file is rewritten from the gameplay rules**:
' 结构、变量名、注释、绘制方式都是自己的，没有抄原件的一行代码 ——
' the structure, variable names, comments and drawing approach are all its own -- not one line of the original was copied --
' 与 `gorilla.bas` 同一条规矩（`Examples/` 会随 APK 分发）。
' the same rule as `gorilla.bas` (`Examples/` ships inside the APK).
'
' 玩法：月面重力下把登月舱放下去。**按住「点火」推进**，左右两个键调姿态角。
' Gameplay: put the lander down under lunar gravity. **Hold FIRE to thrust**, the two side buttons set the attitude angle.
' 落地判据与原版一致：**垂直速度够小 + 水平速度够小 + 角度接近直立**，三条都满足才算成功。
' The landing probe matches the original: **vertical speed low enough + horizontal speed low enough + angle near upright**, all three must hold to count as a success.
' 燃料有限（100 拍），烧完就只能听天由命。
' Fuel is limited (100 ticks); once it is burned up you are at the mercy of gravity.
'
' ◆ 手机上怎么玩
' ◆ How to play on a phone
'
' 原版是每回合打字输入"烧多少燃料"（离散的），手机上改成**长按点火**（连续），
' The original had you type in "how much fuel to burn" each turn (discrete); on a phone this becomes **holding FIRE** (continuous),
' 手感更直接、也更好按。左右各一个姿态键，按一次转 15°。
' which feels more direct and is easier to press. One attitude button on each side, and one press turns 15 degrees.
' 注意用 `ui_poll_msg` + **按下/抬起两个事件**维护"键还按着没有" ——
' Note that `ui_poll_msg` plus **both the down and up events** is how "is the key still held" is tracked --
' 只认"按下"的话，拇指按住不放时没有任何后续事件，推进就只生效一帧
' if only "down" were recognised, holding a thumb down would produce no further events and thrust would last a single frame
' （tetris 的连发定时器踩过同一个坑，见 CLAUDE.md 移动端 ⑰）。
' (the tetris auto-repeat timer hit this same gotcha; see CLAUDE.md mobile section 17).
'
' ◆ 写法提醒
' ◆ Syntax notes
'
'   · 这份用**真数组**存地形（`DIM terr(COLS)`，维度直接写 CONST）——
'   · This one stores the terrain in a **real array** (`DIM terr(COLS)`, with the dimension written straight as a CONST) --
'     数组、动态下标、SUB 内读写、用 CONST 当维度都已确认可用
'     arrays, dynamic subscripts, read/write inside a SUB, and using a CONST as a dimension are all confirmed working
'     （判据 `scripts/vml-basic-probe`）。
'     (probe `scripts/vml-basic-probe`).
'   · 外部过程必须 `NATIVE SUB` / `NATIVE FUNCTION` + 空体。
'   · External procedures must be `NATIVE SUB` / `NATIVE FUNCTION` with an empty body.
'   · **有返回值的必须写 `FUNCTION`**：写成 `SUB … AS INTEGER` 会去链一个 `func_integer`。
'   · **Anything with a return value must be written `FUNCTION`**: written as `SUB ... AS INTEGER` it goes off and links a `func_integer`.
'   · `FUNCTION` 必须用 `END FUNCTION` 收尾（写成 `END SUB` 会让整个解析错位）。
'   · A `FUNCTION` must be closed with `END FUNCTION` (writing `END SUB` throws the whole parse out of alignment).
'   · 颜色写 `&HFFrrggbb`。
'   · Colors are written `&HFFrrggbb`.

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
' 音效：**用 `ui_beep` 单音**（v0.96.509 统一换回来）。
' Sound: **single-tone `ui_beep`** (switched back over uniformly in v0.96.509).
' ⚠⚠ 一度走共享库的音序器（`ui_sfx_add`），**真机上破音**（多个音叠着响 + 长音拖尾）
' ⚠⚠ We once went through the shared library's sequencer (`ui_sfx_add`) and it **broke up on a real device** (several tones sounding together plus long tones trailing).
'   ⇒ 全换回单音的 beep：它是**单通道**的（后一个音掐掉前一个），结构上不可能削波。
'   => everything went back to the single-tone beep: it is **single-channel** (each tone cuts off the previous one), so clipping is structurally impossible.
' ⚠ 频率取原音型的**首音**；**结局取两端的极值**（着陆成功取最高音、坠毁取最低音 ——
' ⚠ The frequency takes the **first tone** of the original motif; **the outcome takes the extremes of both ends** (a successful landing takes the highest tone, a crash the lowest --
'   五子棋/象棋两版也是这么配的：赢 1320 / 输 240）。
'   the gomoku and chess versions are scored the same way: win 1320 / lose 240).
'   低音不低于 C3(131Hz)（手机外放 200Hz 以下衰减很快，听着像没响）。
'   Lows stay at or above C3 (131Hz) (a phone speaker rolls off fast below 200Hz and it sounds like nothing played).
NATIVE SUB ui_beep(freq AS INTEGER, ms AS INTEGER)
END SUB
' ⚠ `ui_sfx_panic` 留着 —— 它是"全停"，退出前调一次把所有正在响的声音一起掐掉。
' ⚠ `ui_sfx_panic` is kept -- it is "stop everything": call it once before exiting to cut off all the tones still sounding together.
NATIVE SUB ui_sfx_panic()
END SUB
NATIVE SUB ui_vibrate(ms AS INTEGER, strength AS INTEGER)
END SUB
' ⚠ 形参名不能叫 on —— BASIC 关键字，会把 NATIVE 声明弄坏（实测）
' ⚠ A parameter must not be named `on` -- that is a BASIC keyword and breaks the NATIVE declaration (measured)
NATIVE SUB ui_keep_on(v AS INTEGER)
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  手感数值（都在这儿，改手感只动这一段）
'  Feel numbers (all here; changing the feel only touches this block)
' ══════════════════════════════════════════════════════════════════════════
CONST NTICK = 40             ' 物理拍（毫秒）
' Physical tick (ms)
CONST PACE = 40              ' 主循环最长睡眠（把重绘锁在 ~25fps）
' Longest sleep of the main loop (locks redraw near ~25fps)
CONST GRAV = 1               ' 重力（单位：1/8 像素 / 拍²）
' Gravity (unit: 1/8 pixel per tick squared)
CONST THRUST = 3             ' 推进加速度（同单位）
' Thrust acceleration (same unit)
CONST FUEL0 = 100            ' 起始燃料（拍）
' Starting fuel (ticks)
CONST COLS = 30              ' 地形列数
' Number of terrain columns
CONST COLW = 14              ' 每列宽（像素）—— 见 layout 里按屏幕重算
' Width of each column (px) -- see layout, recomputed per screen
CONST TURN = 15              ' 一次按键转多少度
' How many degrees one key press turns
CONST SAFE_VY = 26           ' 允许的着陆垂直速度上限（同单位，26 ≈ 3.2 像素/拍）
' Allowed landing vertical-speed ceiling (same unit, 26 is about 3.2 pixels per tick)
CONST SAFE_VX = 16           ' 允许的着陆水平速度上限（≈ 2 像素/拍）
' Allowed landing horizontal-speed ceiling (about 2 pixels per tick)
CONST SAFE_ANG = 12          ' 允许的着陆倾角（度）
' Allowed landing tilt (degrees)
CONST MAXSTEPS = 900         ' 自检里一发最多推这么多拍（防死循环）
' Most ticks a single self-check run may step (guards against an endless loop)

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
'  State
' ══════════════════════════════════════════════════════════════════════════
DIM sw AS INTEGER
DIM sh AS INTEGER
DIM colw AS INTEGER
' 维度用 CONST 写 —— `DIM a(N)` 曾经**不生效**（只分 2 格、写就越界，
' The dimension is written as a CONST -- `DIM a(N)` once **had no effect** (only 2 slots were allocated, so writes went out of bounds,
' 让循环变量一路跑到 760），v0.96.331 起解析期会查常量表，现在是对的。
' letting the loop variable run all the way to 760); since v0.96.331 the parser consults the constant table, so it is correct now.
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
' Bottom bar geometry
DIM ctlY AS INTEGER
DIM fireY AS INTEGER
DIM fireH AS INTEGER
DIM rotY AS INTEGER
DIM rotH AS INTEGER

' 临时量
' Temporaries
DIM i AS INTEGER
DIM n AS INTEGER
DIM mt AS INTEGER
DIM rx AS INTEGER
DIM ry AS INTEGER
DIM cx AS INTEGER
DIM vv AS INTEGER
DIM txt AS STRING

' 状态
'  State
CONST ST_FLY = 0
CONST ST_DOWN = 1
CONST ST_OVER = 2

' ══════════════════════════════════════════════════════════════════════════
'  界面语言（0 = 中文 / 1 = 英文，跟随系统语言）
'  UI language (0 = Chinese / 1 = English, following the system language)
'
'  `ui_get_language()` 是一次 syscall ⇒ **开局查一次存进 LANG**，文案也在这里一次算好，
'  `ui_get_language()` is a syscall => **query it once at start-up and store it in LANG**, and the strings are computed here in one go,
'  之后每帧绘制只用变量（别在绘制路径上再调它）。
'  so every later frame only reads variables (do not call it again on the draw path).
' ⚠ 这段**必须待在 SUB 里、模块级只留一句调用** —— 实测（whack.bas 同款写法）把 IF/ELSE
' ⚠ This block **must stay inside a SUB, with only one call left at module level** -- measured (the same style as whack.bas): spreading the IF/ELSE
'   摊到模块级之后，模块级代码流里那条 `ui_rect … 0, 3, 8` 的后两个实参被读成垃圾、
'   out at module level made the last two arguments of that `ui_rect ... 0, 3, 8` in the module-level code flow read as garbage and
'   整屏画花（与基线帧比 diff_px 0 → 130571）；搬进 SUB 后与改动前**逐像素相同**。
'   painted the whole screen into a mess (diff_px 0 -> 130571 against the baseline frame); moved into a SUB it is **pixel-for-pixel identical** to before the change.
' ══════════════════════════════════════════════════════════════════════════
DIM LANG AS INTEGER
DIM sTitle AS STRING
DIM sHint AS STRING
DIM sEdgeL AS STRING
DIM sEdgeR AS STRING
DIM sHard AS STRING
DIM sLanded AS STRING
DIM sPts AS STRING
DIM sAlt AS STRING
DIM sVy AS STRING
DIM sVx AS STRING
DIM sFuel AS STRING
DIM sAngle AS STRING
DIM sDegBest AS STRING
DIM sTurnL AS STRING
DIM sTurnR AS STRING
DIM sFiring AS STRING
DIM sNoFuel AS STRING
DIM sFire AS STRING
DIM sChkHdr AS STRING
DIM sChk1 AS STRING
DIM sChk2 AS STRING
DIM sChk3 AS STRING
DIM sChk4 AS STRING
DIM sChk5 AS STRING
DIM sChk6 AS STRING
DIM sChk7 AS STRING
DIM sChk8 AS STRING
DIM sChk9 AS STRING
DIM sChk10 AS STRING
DIM sChk11 AS STRING
DIM sChk12 AS STRING
DIM sChk13 AS STRING
DIM sNoWin AS STRING

SUB initLang()
    LANG = ui_get_language()
    IF LANG = 0 THEN sTitle = "登月" ELSE sTitle = "Lander"
    IF LANG = 0 THEN sHint = "按住【点火】减速，左右键调姿态" ELSE sHint = "Hold FIRE to slow, arrows to tilt"
    IF LANG = 0 THEN sEdgeL = "撞上了屏幕左缘" ELSE sEdgeL = "Hit the left edge"
    IF LANG = 0 THEN sEdgeR = "撞上了屏幕右缘" ELSE sEdgeR = "Hit the right edge"
    IF LANG = 0 THEN sHard = "落得太重" ELSE sHard = "Came down too hard"
    IF LANG = 0 THEN sLanded = "着陆成功！剩余燃料 " ELSE sLanded = "Landed! Fuel left "
    IF LANG = 0 THEN sPts = " 分" ELSE sPts = " pts"
    IF LANG = 0 THEN sAlt = "高度" ELSE sAlt = "Alt"
    IF LANG = 0 THEN sVy = "垂速" ELSE sVy = "V/S"
    IF LANG = 0 THEN sVx = "横速" ELSE sVx = "H/S"
    IF LANG = 0 THEN sFuel = "燃料" ELSE sFuel = "Fuel"
    IF LANG = 0 THEN sAngle = "角度 " ELSE sAngle = "Angle "
    IF LANG = 0 THEN sDegBest = "°   分数 " ELSE sDegBest = " deg   Best "
    IF LANG = 0 THEN sTurnL = "◀ 左转" ELSE sTurnL = "◀ LEFT"
    IF LANG = 0 THEN sTurnR = "右转 ▶" ELSE sTurnR = "RIGHT ▶"
    IF LANG = 0 THEN sFiring = "点 火 中" ELSE sFiring = "FIRING"
    IF LANG = 0 THEN sNoFuel = "燃料耗尽" ELSE sNoFuel = "NO FUEL"
    IF LANG = 0 THEN sFire = "点 火" ELSE sFire = "FIRE"
    IF LANG = 0 THEN sChkHdr = "── 登月 · 物理与着陆判据自检 ──" ELSE sChkHdr = "--- Lander: physics & landing self-check ---"
    IF LANG = 0 THEN sChk1 = "  三角函数表 0/30/45/90 度正弦（应 0/500/707/1000）:" ELSE sChk1 = "  sin 0/30/45/90 deg (want 0/500/707/1000):"
    IF LANG = 0 THEN sChk2 = "  余弦 0/90 度（应 1000/0）:" ELSE sChk2 = "  cos 0/90 deg (want 1000/0):"
    IF LANG = 0 THEN sChk3 = "  自由落体 10 拍后 vy（应 10）:" ELSE sChk3 = "  free fall: vy after 10 ticks (want 10):"
    IF LANG = 0 THEN sChk4 = "  点火 10 拍后 vy（应 -20，负=向上）:" ELSE sChk4 = "  thrust: vy after 10 ticks (want -20, neg = up):"
    IF LANG = 0 THEN sChk5 = "  燃料（应 90）:" ELSE sChk5 = "  fuel (want 90):"
    IF LANG = 0 THEN sChk6 = "  空油箱点火 5 拍后 vy（应 5，只剩重力）:" ELSE sChk6 = "  empty tank: vy after 5 ticks (want 5, gravity only):"
    IF LANG = 0 THEN sChk7 = "  轻放直立（应 1）:" ELSE sChk7 = "  gentle upright (want 1):"
    IF LANG = 0 THEN sChk8 = "  落太快（应 0）:" ELSE sChk8 = "  too fast (want 0):"
    IF LANG = 0 THEN sChk9 = "  横速太大（应 0）:" ELSE sChk9 = "  side speed too high (want 0):"
    IF LANG = 0 THEN sChk10 = "  倾角太大（应 0）:" ELSE sChk10 = "  tilt too steep (want 0):"
    IF LANG = 0 THEN sChk11 = "  边界：正好 SAFE_VY（应 1）:" ELSE sChk11 = "  edge: exactly SAFE_VY (want 1):"
    IF LANG = 0 THEN sChk12 = "  边界：超一点（应 0）:" ELSE sChk12 = "  edge: a little over (want 0):"
    IF LANG = 0 THEN sChk13 = "  桌面脚手架无窗口" ELSE sChk13 = "  desktop scaffold: no window"
    IF LANG = 0 THEN sNoWin = "（桌面脚手架：没有真窗口，物理自检打完就退出）" ELSE sNoWin = "(desktop scaffold: no real window - self-check done, exiting)"
END SUB


' ══════════════════════════════════════════════════════════════════════════
'  纯规则（自检直接打它们的返回值）
'  Pure rules (the self-check prints their return values directly)
' ══════════════════════════════════════════════════════════════════════════

' 角度 → 正弦 × 1000（整数表，避免依赖前端的三角函数实现）
' Angle -> sine x 1000 (integer table, avoiding any dependence on the frontend's trigonometry implementation)
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
' Absolute value of an angle
FUNCTION absi(v AS INTEGER) AS INTEGER
    IF v < 0 THEN
        absi = 0 - v
    ELSE
        absi = v
    END IF
END FUNCTION

' 落地判据：三条都满足才算成功 —— 这是这个游戏唯一的胜负规则
' Landing probe: all three must hold to count as a success -- this is the game's only win/lose rule
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
'  Situation
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
' New round: re-roll the lunar surface + find a flat stretch to serve as the landing pad
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
        ' Height difference between neighbouring columns stays within 2 -- terrain that steep reads as a bug
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
    ' Find a flat stretch 3 wide to serve as the landing pad (if none is found, flatten one by hand)
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
    ' ⚠ The start height must be **below** the HUD (74), otherwise the lander is covered by the info bar and the player cannot see it
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
    lastMsg = sHint
END SUB

' 这一拍上的物理
' Physics for one tick
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
    ' Off the sides: touching a screen edge counts as a crash (the original "flew off the picture"; this is more explicit)
    IF hx < 6 THEN
        crash(sEdgeL)
        EXIT SUB
    END IF
    IF hx > sw - 6 THEN
        crash(sEdgeR)
        EXIT SUB
    END IF

    ' 撞地判定：拿所在列的地形高度比
    ' Ground hit test: compare against the terrain height of the column the lander is in
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
            crash(sHard)
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
    lastMsg = sLanded + STR$(fuel) + " → " + STR$(score) + sPts
    ui_beep 1175, 320   ' 着陆成功（赢方）：最高音、最长
    ' Landing success (the winning side): highest tone, longest
    ui_vibrate(60, 120)
END SUB

SUB crash(why AS STRING)
    state = ST_DOWN
    boomT = 0
    lastMsg = why
    ui_beep 131, 320   ' 坠毁（输方）：最低音、最长
    ' Crash (the losing side): lowest tone, longest
    ui_vibrate(200, 240)
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  绘制
'  Drawing
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
    ' Mark the landing pad green
    ui_rect(padCol * colw, terr(padCol) - 2, colw * 3, 5, C_PAD, 1, 0, 0)
END SUB

SUB drawLander()
    DIM lx AS INTEGER
    DIM ly AS INTEGER
    lx = hx
    ly = hy
    ' 舱体
    ' Cabin body
    ui_rect(lx - 9, ly - 14, 18, 14, C_LANDER, 1, 0, 3)
    ui_rect(lx - 5, ly - 20, 10, 7, C_LANDER, 1, 0, 2)
    ' 支腿（成功/失败都照画，只是姿态不同）
    ' Legs (drawn for success and failure alike, only the attitude differs)
    ui_line(lx - 9, ly, lx - 15, ly + 9, C_LANDER, 2)
    ui_line(lx + 9, ly, lx + 15, ly + 9, C_LANDER, 2)
    ' 喷口方向：用一条短线表示当前姿态（角度为 0 时垂直向下）
    ' Nozzle direction: a short line shows the current attitude (straight down when the angle is 0)
    ui_line(lx, ly, lx + sind(ang) / 40, ly + cosd(ang) / 40, C_DIM, 3)
    IF thrusting = 1 THEN
        IF fuel > 0 THEN
            ui_circle(lx + sind(ang) / 40, ly + 12 + cosd(ang) / 40, 5, C_FLAME, 1, 0)
        END IF
    END IF
END SUB

SUB drawHud()
    ui_rect(0, 0, sw, 74, C_HUD, 1, 0, 0)
    ui_text(10, 8, sAlt, C_DIM, 12, 0)
    txt = STR$(terr(hx / colw) - hy)
    ui_text(10, 22, txt, C_TEXT, 18, 0)

    ui_text(sw / 2 - 40, 8, sVy, C_DIM, 12, 0)
    txt = STR$(vy)
    IF absi(vy) > SAFE_VY THEN
        ui_text(sw / 2 - 40, 22, txt, C_WARN, 18, 0)
    ELSE
        ui_text(sw / 2 - 40, 22, txt, C_OK, 18, 0)
    END IF

    ui_text(sw / 2 + 30, 8, sVx, C_DIM, 12, 0)
    txt = STR$(vx)
    IF absi(vx) > SAFE_VX THEN
        ui_text(sw / 2 + 30, 22, txt, C_WARN, 18, 0)
    ELSE
        ui_text(sw / 2 + 30, 22, txt, C_OK, 18, 0)
    END IF

    ui_text(sw - 10, 8, sFuel, C_DIM, 12, 2)
    txt = STR$(fuel)
    IF fuel > 20 THEN
        ui_text(sw - 10, 22, txt, C_OK, 18, 2)
    ELSE
        ui_text(sw - 10, 22, txt, C_BAD, 18, 2)
    END IF

    ui_text(10, 50, sAngle + STR$(ang) + sDegBest + STR$(bestScore), C_DIM, 13, 0)
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
    ui_text(20 + (half - 30) / 2, rotY + 18, sTurnL, C_TEXT, 16, 1)
    ui_text(half + 10 + (half - 30) / 2, rotY + 18, sTurnR, C_TEXT, 16, 1)

    IF thrusting = 1 THEN
        IF fuel > 0 THEN
            ui_rect(20, fireY, sw - 40, fireH, C_FLAME, 1, 0, 14)
            ui_text(sw / 2, fireY + 12, sFiring, &HFF2A1A00, 20, 1)
        ELSE
            ui_rect(20, fireY, sw - 40, fireH, C_BTN, 1, 0, 14)
            ui_text(sw / 2, fireY + 12, sNoFuel, C_BAD, 20, 1)
        END IF
    ELSE
        ui_rect(20, fireY, sw - 40, fireH, C_BAD, 1, 0, 14)
        ui_text(sw / 2, fireY + 12, sFire, &HFFFFF0EC, 20, 1)
    END IF
END SUB

SUB drawScene()
    ui_clear(C_SKY)
    drawStars()
    ' 地球：右上角一个小蓝圆，纯装饰
    ' Earth: a small blue circle in the top-right corner, purely decorative
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
'  Input
' ══════════════════════════════════════════════════════════════════════════
SUB handlePoint(isDown AS INTEGER)
    rx = ui_msg_a()
    ry = ui_msg_b()

    IF isDown = 0 THEN
        ' 抬起：三个键都可能被松开，一次性全清（手指滑走也要能停）
        ' Up: any of the three buttons may have been released, so clear them all at once (a finger sliding away must be able to stop too)
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
                ui_beep 392, 33   ' 左转（低一点）'
                ' Turn left (a little lower)
            ELSE
                rightHeld = 1
                ang = ang + TURN
                IF ang > 90 THEN
                    ang = 90
                END IF
                ui_beep 523, 33   ' 右转（高一点，与左转分得开）'
                ' Turn right (a little higher, so it is told apart from turning left)
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
'  Main loop
' ══════════════════════════════════════════════════════════════════════════
SUB runGame()
    ui_keep_on(1)
    layout()
    newRound()

    WHILE ui_win_closed() = 0
        IF quit = 1 THEN
            ui_sfx_panic
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
'  Start-up self-check
' ══════════════════════════════════════════════════════════════════════════
SUB simCheck()
    PRINT sChkHdr
    PRINT sChk1; sind(0); sind(30); sind(45); sind(90)
    PRINT sChk2; cosd(0); cosd(90)

    ' 自由落体：起手 vy=0，走 10 拍必须**变快**（vy 从 0 涨到 10）
    ' Free fall: starting at vy=0, ten ticks must make it **faster** (vy climbs from 0 to 10)
    vy = 0
    vx = 0
    thrusting = 0
    fuel = FUEL0
    i = 0
    WHILE i < 10
        vy = vy + GRAV
        i = i + 1
    WEND
    PRINT sChk3; vy
    vy = 0

    ' 点火反推：垂直姿态下净加速度应为 GRAV - THRUST = -2 ⇒ 10 拍后 vy = -20
    ' Thrust braking: upright, the net acceleration should be GRAV - THRUST = -2 => after 10 ticks vy = -20
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
    PRINT sChk4; vy
    PRINT sChk5; fuel

    ' 燃料耗尽后点火无效
    ' Thrust does nothing once the fuel is exhausted
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
    PRINT sChk6; vy

    ' 着陆判据：三条都满足才算成功
    ' Landing probe: all three must hold to count as a success
    PRINT sChk7; landed(10, 8, 0)
    PRINT sChk8; landed(40, 0, 0)
    PRINT sChk9; landed(5, 30, 0)
    PRINT sChk10; landed(5, 2, 30)
    PRINT sChk11; landed(SAFE_VY, SAFE_VX, SAFE_ANG)
    PRINT sChk12; landed(SAFE_VY + 1, 0, 0)
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  主程序
'  Main program
' ══════════════════════════════════════════════════════════════════════════
initLang
simCheck()

sw = ui_scr_w()
sh = ui_scr_h()
IF sw <= 0 THEN
    sw = 390
END IF
IF sh <= 0 THEN
    sh = 660
END IF

wh = ui_win_open_ex(sTitle, sw, sh, 0, 0)

IF wh < 1 THEN
    PRINT sNoWin
ELSE
    runGame()
END IF
