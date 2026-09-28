' 跑车躲驴（DONKEY）—— 用 **BASIC** 写的手机游戏
' Donkey Dodge (DONKEY) -- a mobile game written in **BASIC**
'
' 出处与版权：原型是 1981 年随 IBM PC 附带的 BASIC 程序（那个年代最有名的一份——
' Origin and copyright: the prototype is the BASIC program shipped with the IBM PC in 1981 (the most famous one of its era --
' 它被广泛认为是 PC 上第一个"图形游戏"）。**这份是照玩法规则重写的**：
' it is widely regarded as the first "graphics game" on the PC). **This one is rewritten from the gameplay rules**:
' 结构、变量名、注释、绘制方式都是自己的，没有抄原件的一行代码 ——
' the structure, the variable names, the comments and the drawing are all our own -- not one line of the original was copied --
' 与 `gorilla.bas` 同一条规矩（`Examples/` 会随 APK 分发，不能夹带别人的东西）。
' the same rule as `gorilla.bas` (`Examples/` ships with the APK, so nothing of someone else's may be bundled).
' 原版的画面是 40×25 字符模式、靠字符块拼车和驴；这里按手机重做：
' the original drew in 40x25 text mode, building the car and the donkey out of character blocks; here it is redone for phones:
' 竖屏、像素绘制、方向盘换成两个大按钮。
' portrait, pixel drawing, and two big buttons instead of a steering wheel.
'
' 玩法：车在最下面的一条道上，驴从上面下来。按 ◀ / ▶ 换道躲开它。
' Gameplay: the car sits on the bottom lane and donkeys come down from above. Press left / right to change lane and dodge them.
' 撞上就结束。每过一头驴加一分，**过得越多跑得越快**（2 → 12 像素/拍）。
' Hitting one ends the run. Each donkey dodged adds a point, and **the more you dodge the faster it gets** (2 -> 12 pixels per tick).
'
' ◆ 手机那套 UI
' ◆ The mobile UI layer
'
' 开窗 / 绘图 / 输入 / 定时器是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
' Window / drawing / input / timers are written in C (`Lib/shared/src/vmlui.c` -> `vmlui.vml`),
' 由 `vmltool.config.xml` 的 `<Language Name="basic" Libs="vmlui.vml">` 挂上来。
' attached through `<Language Name="basic" Libs="vmlui.vml">` in `vmltool.config.xml`.
' 写法上与 `gorilla.bas` 同一套：`ui_win_open_ex` 锁竖屏 + 不要手柄区（全程触摸）。
' Same style as `gorilla.bas`: `ui_win_open_ex` locks portrait + asks for no gamepad area (touch all the way).
'
' ◆ 定下的三条输入口径（照抄 gorilla / whack 的结论，别再试一遍）
' ◆ The three settled input conventions (copied from the gorilla / whack conclusions - do not re-test them)
'
'   ① **只用触摸 + 键盘两路**，不依赖手柄区 —— 手柄区会吃掉一百多像素的画面高度，
'   ① **Only touch + keyboard**, no gamepad area -- the gamepad area eats over a hundred pixels of canvas height,
'      而这里两个按钮本来就在屏幕底部，自绘比系统手柄更贴手（也更好看）。
'      and the two buttons here already sit at the bottom of the screen, so drawing them ourselves fits the hand better than the system gamepad (and looks better).
'   ② **键盘要认三个键**：方向键左右（VK 37/39）、A/D（65/68）、以及手柄映射过来的
'   ② **The keyboard must accept three keys**: arrow left / right (VK 37/39), A/D (65/68), and the
'      同一个 VK —— 手机外接键盘 / 模拟器上才好操作。`handleKey` 里一次判全。
'      same VKs coming from a gamepad -- so an external phone keyboard or an emulator is usable. `handleKey` tests them all in one pass.
'   ③ **换道是"点一下走一格"**，不做长按连发。三个道口距很大，点一下一格最准；
'   ③ **A lane change is "one tap, one lane"**, no press-and-hold repeat. The three lanes are far apart, so one tap per lane is the most accurate;
'      长按连发要额外维护"谁负责停它"（见 CLAUDE.md 里 tetris 那三条刹车），
'      hold-to-repeat would need extra machinery for "who stops it" (see the three brakes noted for tetris in CLAUDE.md),
'      在这个玩法上是纯负担。
'      which is pure overhead in this gameplay.
'
' ◆ 写法要求（本前端的硬性要求，改这份别踩回去）
' ◆ Writing requirements (hard requirements of this frontend - do not step back into these when editing)
'
'   · 外部过程必须 `NATIVE SUB` / `NATIVE FUNCTION` + **空体**，否则会被加上
'   · External procedures must be `NATIVE SUB` / `NATIVE FUNCTION` + **an empty body**, otherwise they get a
'     sub_/func_ 前缀、链接期找不到。
'     sub_/func_ prefix and cannot be found at link time.
'   · 模块级变量必须在赋值前 `DIM`。
'   · Module-level variables must be `DIM`-ed before assignment.
'   · 颜色写 `&HFFRRGGBB`（词法器认 `&H`，不认 `0x`）。
'   · Write colours as `&HFFRRGGBB` (the tokenizer accepts `&H`, not `0x`).
'   · **赋值语句不能写在 SUB 定义之后**（会被当成函数调用）—— 这份的顶层赋值都在
'   · **An assignment statement cannot appear after a SUB definition** (it is taken for a function call) -- every top-level assignment here lives in
'     `runGame` 里或 SUB 之前。
'     `runGame` or before the SUBs.
'
' ⚠ **`gorilla.bas` 头部那份"必须绕开"的清单已经过期，别照抄**（v0.96.330 逐条重测）：
' ⚠ **The "must avoid" list at the top of `gorilla.bas` is out of date - do not copy it** (re-measured item by item in v0.96.330):
'   数组（含动态下标 / 二维 / SUB 内）、`\` 与 `MOD`、带括号的子表达式、CONST 参与算术、
'   arrays (including dynamic indices / 2-D / inside a SUB), `\` and `MOD`, parenthesised subexpressions, CONST in arithmetic,
'   `AND`/`OR`、字符串拼接、`STR$`、SUB 的字符串形参、`SIN`/`COS`（×10000 定标）、
'   `AND`/`OR`, string concatenation, `STR$`, STRING parameters of a SUB, `SIN`/`COS` (x10000 scaling),
'   `FOR…NEXT`（含正负 `STEP`）、`SELECT CASE`、`DATA`/`READ`/`RESTORE`、用户 `FUNCTION`、
'   `FOR…NEXT` (including a negative `STEP`), `SELECT CASE`, `DATA`/`READ`/`RESTORE`, user `FUNCTION`,
'   `ELSEIF`、`GOTO` 标签 —— **现在全都对**（判据是 `scripts/vml-basic-probe/`，19/19）。
'   `ELSEIF`, `GOTO` labels -- **all of them work now** (the probe is `scripts/vml-basic-probe/`, 19/19).
'   那份清单是 v0.96.3xx 期间的实测记录，当时的缺陷后来都修掉了；这份文件因此写得比较"正常"。
'   That list was a measured record from the v0.96.3xx era and those defects have since been fixed; this file is therefore written fairly "normally".

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
' 音效：**用 `ui_beep` 单音**（v0.96.509 统一换回来）。
' Sound: **a single-tone `ui_beep`** (switched back to uniformly in v0.96.509).
' ⚠⚠ 一度走共享库的音序器（`ui_sfx_add`），**真机上破音**（多个音叠着响 + 长音拖尾）
' ⚠⚠ It once went through the shared-library sequencer (`ui_sfx_add`), which **clips on a real device** (several tones sounding at once + long tones trailing)
'   ⇒ 全换回单音的 beep：它是**单通道**的（后一个音掐掉前一个），结构上不可能削波。
'   => everything went back to single-tone beeps: that path is **single-channel** (each tone cuts off the previous one), so it structurally cannot clip.
' ⚠ 频率取原音型的**首音**；**结局（撞车）取最低音** —— 五子棋/象棋两版也是这么配的
' ⚠ The frequency is the **first tone** of the original pattern; **the ending (the crash) takes the lowest tone** -- the gomoku/chess versions are scored the same way
'   （赢 1320 / 输 240）。低音不低于 C3(131Hz)（手机外放 200Hz 以下衰减很快）。
'   (win 1320 / lose 240). Low tones stay at or above C3 (131 Hz) (phone speakers roll off fast below 200 Hz).
NATIVE SUB ui_beep(freq AS INTEGER, ms AS INTEGER)
END SUB
' ⚠ `ui_sfx_panic` 留着 —— 它是"全停"，退出前调一次把所有正在响的声音一起掐掉。
' ⚠ `ui_sfx_panic` is kept -- it means "stop everything": calling it once before exit cuts off every sound that is still playing.
NATIVE SUB ui_sfx_panic()
END SUB
NATIVE SUB ui_vibrate(ms AS INTEGER, strength AS INTEGER)
END SUB
' ⚠ 形参名不能叫 on —— BASIC 关键字，会把 NATIVE 声明弄坏（实测）
' ⚠ The parameter must not be named on -- it is a BASIC keyword and breaks the NATIVE declaration (measured)
NATIVE SUB ui_keep_on(v AS INTEGER)
END SUB
NATIVE FUNCTION ui_dlg_msg(title AS STRING, body AS STRING, style AS INTEGER) AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_get_language() AS INTEGER
END FUNCTION

' ══════════════════════════════════════════════════════════════════════════
'  常量
'  constants
' ══════════════════════════════════════════════════════════════════════════
CONST NFLOOR = 3           ' 车道数
' Number of lanes
CONST TICK = 40            ' 逻辑拍（毫秒）：位移与出驴都按它走
' Logic tick (ms): movement and donkey spawning advance on it
CONST PACE = 40            ' 主循环最长睡眠 —— 把重绘锁在 ~25fps，输入延迟 ≤40ms
' Longest sleep of the main loop -- caps redraw at ~25fps, input latency <= 40ms
CONST HUDH = 44            ' 顶部信息带高
' Height of the top info band
CONST PADH = 108           ' 底部按钮区高
' Height of the bottom button pad
CONST CARW = 46            ' 车宽（像素）
' Car width (pixels)
CONST CARH = 74            ' 车高
' Car height
CONST DONW = 40            ' 驴宽
' Donkey width
CONST DONH = 52            ' 驴高
' Donkey height
CONST SPEED0 = 2           ' 起始速度（像素/拍）
' Starting speed (pixels per tick)
CONST SPEEDMAX = 12        ' 速度上限
' Speed cap
CONST PERSPEED = 8         ' 每过几头驴加一格速度
' How many donkeys dodged per extra speed step
CONST GAP0 = 16            ' 出驴间隔（拍），随速度收紧
' Donkey spawn interval (ticks), tightening with speed
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
'  module-level variables
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
' Button geometry (recomputed every frame, so it follows a screen size change)
DIM btnY AS INTEGER
DIM btnH AS INTEGER
DIM btnW AS INTEGER
DIM btnLx AS INTEGER
DIM btnRx AS INTEGER

' 临时量
'  temporaries
DIM i AS INTEGER
DIM k AS INTEGER
DIM t AS INTEGER
DIM n AS INTEGER
DIM mt AS INTEGER
DIM rx AS INTEGER
DIM ry AS INTEGER
DIM tid AS INTEGER

' CONST 的替身变量（SUB 里要参与算术的，必须先落成普通变量）
' Stand-in variables for the CONSTs (any CONST that takes part in arithmetic inside a SUB must first be copied into an ordinary variable)
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
'  界面语言（0 = 中文 / 1 = 英文，跟随系统语言）
'  UI language (0 = Chinese / 1 = English, follows the system language)
'
'  `ui_get_language()` 是一次 syscall ⇒ **开局查一次存进 LANG**，文案也在这里一次算好，
'  `ui_get_language()` is a syscall => **query it once at startup and store it in LANG**, and the strings are computed here in one pass too,
'  之后每帧绘制只用变量（别在绘制路径上再调它）。
'  so every frame afterwards only reads the variables (do not call it again on the drawing path).
' ⚠ 这段**必须待在 SUB 里、模块级只留一句调用** —— 实测（whack.bas 同款写法）把 IF/ELSE
' ⚠ This block **must stay inside a SUB, with only a single call left at module level** -- measured (same writing as whack.bas): spreading the IF/ELSE
'   摊到模块级之后，模块级代码流里那条 `ui_rect … 0, 3, 8` 的后两个实参被读成垃圾、
'   out to module level makes the last two arguments of the `ui_rect … 0, 3, 8` line in the module-level code flow read as garbage and
'   整屏画花（与基线帧比 diff_px 0 → 130571）；搬进 SUB 后与改动前**逐像素相同**。
'   smears the whole screen (diff_px 0 -> 130571 against the baseline frame); moved into a SUB it is **pixel-for-pixel identical** to before the change.
' ══════════════════════════════════════════════════════════════════════════
DIM LANG AS INTEGER
DIM sTitle AS STRING
DIM sScore AS STRING
DIM sSpeed AS STRING
DIM sBest AS STRING
DIM sLeft AS STRING
DIM sRight AS STRING
DIM sCrash AS STRING
DIM sAgain AS STRING
DIM sSimHdr AS STRING
DIM sSim1 AS STRING
DIM sSim2 AS STRING
DIM sSim3 AS STRING
DIM sSim4 AS STRING
DIM sSim5 AS STRING
DIM sSim6 AS STRING
DIM sSim7 AS STRING
DIM sNoWin AS STRING

SUB initLang()
    LANG = ui_get_language()
    IF LANG = 0 THEN sTitle = "跑车躲驴" ELSE sTitle = "Donkey"
    IF LANG = 0 THEN sScore = "得分" ELSE sScore = "Score"
    IF LANG = 0 THEN sSpeed = "最快" ELSE sSpeed = "Speed"
    IF LANG = 0 THEN sBest = "最高" ELSE sBest = "Best"
    IF LANG = 0 THEN sLeft = "← 或 A" ELSE sLeft = "<- or A"
    IF LANG = 0 THEN sRight = "→ 或 D" ELSE sRight = "-> or D"
    IF LANG = 0 THEN sCrash = "撞车了" ELSE sCrash = "Crashed"
    IF LANG = 0 THEN sAgain = "，再来一局？" ELSE sAgain = ", play again?"
    IF LANG = 0 THEN sSimHdr = "── 跑车躲驴 · 碰撞自检（固定尺寸，不看屏幕，只算逻辑）──" ELSE sSimHdr = "--- Donkey dodge - collision self-check (fixed size, logic only) ---"
    IF LANG = 0 THEN sSim1 = "  画布 / 车道宽 / 车中心:" ELSE sSim1 = "  canvas / lane width / car center:"
    IF LANG = 0 THEN sSim2 = "  ① 同道相撞 -> crashed(应 1):" ELSE sSim2 = "  1) same lane -> crashed (want 1):"
    IF LANG = 0 THEN sSim3 = "  ② 异道不撞 -> crashed(应 0):" ELSE sSim3 = "  2) other lane -> crashed (want 0):"
    IF LANG = 0 THEN sSim4 = "  ③ 躲过去 -> 得分(应 1) / 驴还在吗(应 0):" ELSE sSim4 = "  3) passed -> score (want 1) / donkey here (want 0):"
    IF LANG = 0 THEN sSim5 = "  ④ 攒够就加速 -> speed(应 3):" ELSE sSim5 = "  4) enough dodges -> speed (want 3):"
    IF LANG = 0 THEN sSim6 = "  ⑤ 向左夹住 -> carLane(应 0):" ELSE sSim6 = "  5) clamp left -> carLane (want 0):"
    IF LANG = 0 THEN sSim7 = "  ⑤ 向右夹住 -> carLane(应 2):" ELSE sSim7 = "  5) clamp right -> carLane (want 2):"
    IF LANG = 0 THEN sNoWin = "（桌面脚手架：没有真窗口，碰撞自检打完就退出）" ELSE sNoWin = "(desktop scaffold: no real window - self-check done, exiting)"
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  子过程
'  subroutines
' ══════════════════════════════════════════════════════════════════════════

' 屏幕尺寸 / 车道几何 —— 只在开局与 resize 后调
' Screen size / lane geometry -- called only at startup and after a resize
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
' Centre x of lane i (lane 0 is the leftmost)
FUNCTION laneCenter(i AS INTEGER) AS INTEGER
    tx = lane0 + laneW * i
    tx = tx + laneW / 2
    laneCenter = tx
END FUNCTION

' 画一辆车：车身 + 车顶 + 挡风 + 两个轮子 + 一对前灯
' Draw a car: body + roof + windscreen + two wheels + a pair of headlights
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
' Draw a donkey (it walks downwards, so its head is at the bottom): body + mane + head + ears + four legs
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
' The top info band
SUB drawHud()
    ui_rect(0, 0, sw, hudh, C_HUD, 1, 0, 0)
    ui_text(14, 13, sScore, C_DIM, 13, 0)
    ui_text(52, 8, STR$(score), C_TEXT, 22, 0)
    ui_text(sw / 2, 13, sSpeed, C_DIM, 13, 1)
    ui_text(sw / 2, 8, STR$(speed), C_TEXT, 22, 1)
    ui_text(sw - 14, 13, sBest, C_DIM, 13, 2)
    ui_text(sw - 14, 8, STR$(best), C_TEXT, 22, 2)
END SUB

' 底部两个按钮
' The two bottom buttons
SUB drawPads()
    ui_rect(0, sh - padh, sw, padh, C_HUD, 1, 0, 0)
    ui_rect(btnLx, btnY, btnW, btnH, C_BTN, 1, 0, 14)
    ui_rect(btnRx, btnY, btnW, btnH, C_BTN, 1, 0, 14)
    ' 箭头用两条线拼（多边形要 int 数组，而数组不能用）
    ' The arrow is built from two lines (a polygon needs an int array, and arrays cannot be used)
    rx = btnLx + btnW / 2
    ry = btnY + btnH / 2
    ui_line(rx + 12, ry - 16, rx - 12, ry, C_TEXT, 6)
    ui_line(rx - 12, ry, rx + 12, ry + 16, C_TEXT, 6)
    rx = btnRx + btnW / 2
    ui_line(rx - 12, ry - 16, rx + 12, ry, C_TEXT, 6)
    ui_line(rx + 12, ry, rx - 12, ry + 16, C_TEXT, 6)
    ui_text(btnLx + btnW / 2, btnY + btnH + 4, sLeft, C_DIM, 12, 1)
    ui_text(btnRx + btnW / 2, btnY + btnH + 4, sRight, C_DIM, 12, 1)
END SUB

SUB drawScene()
    ui_clear(C_SKY)
    ui_rect(0, roadTop, sw, roadBot - roadTop, C_ROAD, 1, 0, 0)
    ui_rect(0, roadTop, 12, roadBot - roadTop, C_ROADEDGE, 1, 0, 0)
    ui_rect(sw - 12, roadTop, 12, roadBot - roadTop, C_ROADEDGE, 1, 0, 0)

    ' 车道分隔线：两排滚动的虚线。`dashY` 每拍前进 speed，绕一圈回到顶
    ' Lane divider: two rows of scrolling dashes. `dashY` advances by speed each tick, wrapping back to the top
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
        ' Crashed: the car is drawn where it stands, with an expanding ball of fire on top
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
' Move the car to lane L (out of range is clamped, not wrapped around)
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
    ui_beep 523, 33   ' 前进
    ' moving forward
END SUB

' 这一拍：出驴、推驴、判撞、计分
' One tick: spawn a donkey, push it, test for a hit, score
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
            ' The interval tightens with speed -- the faster you go, the denser the donkeys
            gapLeft = gap0 - (speed - SPEED0)
            IF gapLeft < gapMin THEN
                gapLeft = gapMin
            END IF
        END IF
    ELSE
        donY = donY + speed
        ' 判撞：同一条道、且竖直方向重叠
        ' Hit test: same lane and vertical overlap
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
        ' Reached the bottom = dodged
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
                        ui_beep 1047, 66   ' 加速
                        ' speed up
                    END IF
                END IF
            END IF
        END IF
    END IF

    ' 路面虚线滚动
    ' scroll the road dashes
    dashY = dashY + speed
    IF dashY >= 64 THEN
        dashY = dashY - 64
    END IF
END SUB

SUB crash()
    crashed = 1
    boomT = 0
    hitLane = donLane
    ui_beep 131, 320   ' 撞车（结局）：最低音、最长
    ' crash (the ending): lowest tone, longest duration
    ui_vibrate(120, 200)
END SUB

' 撞车之后：让爆炸播完再问「再来一局」
' After a crash: let the explosion finish, then ask "play again"
SUB handleCrashEnd()
    boomT = boomT + 1
    IF boomT >= 10 THEN
        IF quit = 0 THEN
            ' ⚠ 弹框期间必须把定时器停掉：`ui_dlg_msg` 是模态的，而这是**重复**定时器，
            ' ⚠ The timer must be stopped while the dialog is up: `ui_dlg_msg` is modal and this is a **repeating** timer,
            '   挂多久就积压多少条 —— 返回后会被瞬间抽干，新一局立刻又被当成"撞了"。
            '   so however long it hangs, that many messages pile up -- they are drained instantly on return and the new round is immediately taken for "crashed" again.
            '   （whack.bas 头部记的就是这个坑；宿主侧 `WithTimersPaused` 已统一兜住，
            '   (the top of whack.bas records this very gotcha; the host side already catches it uniformly through `WithTimersPaused`,
            '     这里再显式停一次，是"两处都做对"而不是"指望某一处"。）
            '     and stopping it explicitly again here is "both places correct" rather than "hoping one of them is".)
            ui_timer_kill(tid)
            k = ui_dlg_msg(sCrash, sScore + " " + STR$(score) + sAgain, 1)
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
' Keyboard: arrow keys / A / D change lane; Esc quits; Enter = play again (after a crash)
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
' Touch: landing on the left / right half changes lane. Both the buttons and the road count -- fingers tap anywhere,
' 与其让玩家去瞄那个矩形，不如"点屏幕哪边就往哪边"。
' so rather than making the player aim at that rectangle, "tap whichever side you want to go".
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
' ── main loop ─────────────────────────────────────────────────────────────
SUB runGame()
    ui_keep_on(1)
    layout()
    resetRound()
    tid = ui_timer_set(tickMs, 0)

    WHILE ui_win_closed() = 0
        IF quit = 1 THEN
            ui_sfx_panic
            ui_win_close()
        END IF

        ' 一次滑动能来几十条 TOUCHMOVE，一条一画的话重绘会被输入拖垮。
        ' One swipe can deliver dozens of TOUCHMOVE messages, and drawing a frame per message lets input drown the redraw.
        ' 位置是幂等的 ⇒ 把队列清空、然后画一帧就够（gorilla 同款）。
        ' The position is idempotent => draining the queue and then drawing one frame is enough (same as gorilla).
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
'  startup self-check (headless, runs on the desktop scaffold)
'
'  判据是**行为**，不是"编过了"：同一条道上会撞、不同道不会撞、
'  The probe is **behaviour**, not "it compiled": the same lane crashes, a different lane does not,
'  躲过去会加分并（攒够之后）加速。这三条正是这个玩法仅有的三件事。
'  dodging scores and (once enough have accumulated) speeds up. Those three are the only three things this gameplay has.
' ══════════════════════════════════════════════════════════════════════════
SUB simCheck()
    PRINT sSimHdr
    sw = 390
    sh = 660
    layout()
    resetRound()
    PRINT sSim1; sw; laneW; carX

    ' ① 同一条道 ⇒ 必撞
    ' ① same lane => must crash
    carLane = 1
    carX = laneCenter(1)
    carX = carX - carw / 2
    donOn = 1
    donLane = 1
    donX = laneCenter(1)
    donX = donX - donw / 2
    donY = carY - donh + 4
    stepWorld()
    PRINT sSim2; crashed

    ' ② 换一条道 ⇒ 不该撞
    ' ② change lane => must not crash
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
    PRINT sSim3; crashed

    ' ③ 驴走到底 ⇒ 加分、驴消失
    ' ③ the donkey reaches the bottom => score up, donkey gone
    resetRound()
    score = 0
    donOn = 1
    donLane = 2
    donY = roadBot - 2
    stepWorld()
    PRINT sSim4; score; donOn

    ' ④ 攒够 perSpeed 头 ⇒ 加速一格
    ' ④ enough dodges for perSpeed => one speed step up
    resetRound()
    speed = SPEED0
    passed = perSpeed - 1
    donOn = 1
    donLane = 2
    donY = roadBot - 2
    stepWorld()
    PRINT sSim5; speed

    ' ⑤ 换道要夹住边界，不能跑到道外
    ' ⑤ lane changes clamp at the boundary and never leave the road
    resetRound()
    moveCar(0 - 1)
    moveCar(0 - 1)
    PRINT sSim6; carLane
    k = 0
    WHILE k < 9
        moveCar(1)
        k = k + 1
    WEND
    PRINT sSim7; carLane
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  主程序
'  main program
' ══════════════════════════════════════════════════════════════════════════
initLang
simCheck()

' 尺寸：先问设备，再开窗。**顺序照抄 gorilla** —— 排版用的那两个数必须与交给
' Size: ask the device first, then open the window. **The order is copied from gorilla** -- the two numbers used for layout must come from the same source as
' 窗口的那两个数同源，否则内容会画到画布外面。
' the two handed to the window, otherwise the content is drawn outside the canvas.
sw = ui_scr_w()
sh = ui_scr_h()
IF sw <= 0 THEN
    sw = 390
END IF
IF sh <= 0 THEN
    sh = 660
END IF

' 锁竖屏 + 不要手柄区（全程触摸，手柄区白吃一百多像素的画面高度）
' Lock portrait + no gamepad area (touch all the way; the gamepad area wastes over a hundred pixels of canvas height)
wh = ui_win_open_ex(sTitle, sw, sh, 0, 0)

' 宿主把窗口开出来了才开跑；桌面脚手架这里返回 0 —— 上面自检已打完，直接收工。
' Only start running once the host has opened the window; on the desktop scaffold this returns 0 -- the self-check above has already finished, so just stop here.
' ⚠ 别把这条判断当"平台探测"去别处复用，它只说明"这一轮有没有真窗口"。
' ⚠ Do not reuse this test elsewhere as "platform detection"; it only says whether this run has a real window.
IF wh < 1 THEN
    PRINT sNoWin
ELSE
    runGame()
END IF
