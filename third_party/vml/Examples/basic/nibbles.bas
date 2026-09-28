' 贪吃蛇（NIBBLES）—— 用 **BASIC** 写的手机游戏
' Nibbles -- a mobile game written in **BASIC**
'
' 出处与版权：原型是当年随 QBasic 1.1 一起发的那份 NIBBLES.BAS（微软版权，
' Origin and copyright: the prototype is the NIBBLES.BAS that shipped with QBasic 1.1 back then (Microsoft copyright,
' 「吃数字、逐关推进、撞墙或撞自己就死」那套玩法）。**这份是照玩法规则重写的**：
' the eat-numbers, level-by-level, die-on-a-wall-or-on-yourself ruleset). **This one is rewritten from the rules**:
' 结构、变量名、关卡布局、注释、绘制方式都是自己的，没有抄原件的一行代码 ——
' structure, variable names, level layout, comments and drawing are all our own; not one line of the original code was copied --
' 与 `gorilla.bas` 同一条规矩（`Examples/` 会随 APK 分发，不能夹带别人的东西）。
' the same rule as `gorilla.bas` (`Examples/` ships with the APK, so it must not carry third-party material).
'
' 玩法：蛇在格子里走，吃掉出现的数字就变长、变快。**吃满本关要求就进下一关**，
' Gameplay: the snake walks on the grid; eating a number that appears makes it longer and faster. **Fill the level quota to advance**,
' 关卡越高要求越多、蛇也越快。吃到**与本关编号相同**的数字给双倍分并算两个。
' higher levels ask for more and run faster. Eating the number **equal to this level number** scores double and counts as two.
' 撞墙 / 撞到自己 = 结束。
' Hitting a wall / hitting yourself = over.
'
' ◆ 手机上怎么玩（这一条是照仓库自己踩过的坑定的）
' ◆ How to play on a phone (this one follows gotchas this repo already measured)
'
' **不自己画手柄** —— 绘图窗口底部本来就有一排屏幕手柄，程序只该收按键消息。
' **Do not draw your own gamepad** -- the drawing window already has a row of on-screen gamepad buttons at the bottom, so the program should only receive key messages.
' 自绘那套的代价是三重：占画面高度、几何要在"画"与"命中"两处各算一遍、
' Drawing it yourself costs three things: it eats screen height, the geometry must be computed twice (drawing and hit testing),
' 每个游戏还各画一套风格（tetris 那里已经删过一次自绘手柄）。
' and every game then draws its own style (the self-drawn gamepad was already deleted once in tetris).
' 所以这里用 `ui_win_open_ex(..., 0, 1)` 要**系统手柄区**，方向键走 `VML_MSG_KEYDOWN`；
' So this calls `ui_win_open_ex(..., 0, 1)` to ask for the **system gamepad area**, and the arrow keys arrive as `VML_MSG_KEYDOWN`;
' 另外补一个**滑动改向**给纯触摸的场合（手指在画布上划一下即可）。
' a **swipe to steer** is added for touch-only cases (one finger swipe on the canvas is enough).
'
' ◆ 写法提醒（本前端当前的硬约束，都不是"建议"）
' ◆ Syntax reminders (hard constraints of this frontend today, none of them are suggestions)
'
'   · 数组维度可以写 CONST（`DIM bd(GW*GH)`）—— v0.96.331 起解析期会查常量表。
'   · Array dimensions may use CONST (`DIM bd(GW*GH)`) -- since v0.96.331 the parser consults the constant table.
'   · **数组不能当形参**（`FUNCTION f(a(12))` 会去链 `func_a`）⇒ 需要"几张表"时
'   · **An array cannot be a parameter** (`FUNCTION f(a(12))` would link `func_a`) => when you need several tables
'     宁可**合成一个数组用偏移区分**（本份的棋盘就是一个一维数组，行优先下标自己算）。
'     prefer to **merge them into one array and tell them apart by offset** (this board is a single 1-D array, row-major index computed by hand).
'   · 有返回值的必须写 `FUNCTION` 且用 `END FUNCTION` 收尾。
'   · Anything with a return value must be a `FUNCTION` closed with `END FUNCTION`.
'   · 外部过程必须 `NATIVE SUB` / `NATIVE FUNCTION` + 空体。
'   · External routines must be `NATIVE SUB` / `NATIVE FUNCTION` plus an empty body.
'   · 颜色写 `&HFFrrggbb`。
'   · Colors are written `&HFFrrggbb`.

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
NATIVE FUNCTION ui_get_language() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_wait_msg(timeout AS INTEGER) AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_poll_msg() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_msg_a() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_msg_b() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_timer_set(ms AS INTEGER, tag AS INTEGER) AS INTEGER
END FUNCTION
NATIVE SUB ui_timer_kill(id AS INTEGER)
END SUB
NATIVE FUNCTION ui_rand(n AS INTEGER) AS INTEGER
END FUNCTION
' 音效：**用 `ui_beep` 单音**（v0.96.509 统一换回来）。
' Sound: **use the single-tone `ui_beep`** (switched back everywhere in v0.96.509).
' ⚠⚠ 一度走共享库的音序器（`ui_sfx_add`），**真机上破音**（多个音叠着响 + 长音拖尾）
' ⚠⚠ It once went through the shared-library sequencer (`ui_sfx_add`) and **broke up on a real device** (several tones ringing on top of each other plus a long tail)
'   ⇒ 全换回单音的 beep：它是**单通道**的（后一个音掐掉前一个），结构上不可能削波。
'   ⇒ all switched back to the single-tone beep: it is **single-channel** (the next tone cuts off the previous one), so clipping is structurally impossible.
' ⚠ 频率取原音型的**首音**；「大餐」「升级」这两个"更好的事"取整块**最高音**
' ⚠ Frequency takes the **first note** of the original pattern; the two better things, big meal and level-up, take the **highest note** of the whole block
'   （好与高频的"吃食"分得开）；**死取最低音** —— 五子棋/象棋两版也是这么配的。
'   (so they stay separable from the high-pitched eat tone); **death takes the lowest note** -- the gomoku/chess versions are voiced the same way.
'   低音不低于 C3(131Hz)（手机外放 200Hz 以下衰减很快，听着像没响）。
'   Lows never go below C3(131Hz) (a phone speaker rolls off fast under 200Hz and then it sounds like nothing played).
NATIVE SUB ui_beep(freq AS INTEGER, ms AS INTEGER)
END SUB
' ⚠ `ui_sfx_panic` 留着 —— 它是"全停"，退出前调一次把所有正在响的声音一起掐掉。
' ⚠ `ui_sfx_panic` is kept -- it means stop everything; call it once before exiting to cut every sounding tone at the same time.
NATIVE SUB ui_sfx_panic()
END SUB
NATIVE SUB ui_vibrate(ms AS INTEGER, strength AS INTEGER)
END SUB
' ⚠ 形参名不能叫 on —— BASIC 关键字，会把 NATIVE 声明弄坏（实测）
' ⚠ A parameter may not be named on -- that is a BASIC keyword and it breaks the NATIVE declaration (measured)
NATIVE SUB ui_keep_on(v AS INTEGER)
END SUB
NATIVE FUNCTION ui_dlg_msg(title AS STRING, body AS STRING, style AS INTEGER) AS INTEGER
END FUNCTION

' ══════════════════════════════════════════════════════════════════════════
'  手感数值
'  Feel / tuning numbers
' ══════════════════════════════════════════════════════════════════════════
CONST GW = 20                ' 棋盘宽（格）
                             ' board width (in cells)
CONST GH = 22                ' 棋盘高（格）
                             ' board height (in cells)
CONST GCELLS = GW * GH       ' 棋盘总格数（一维数组，行优先）
                             ' total board cells (1-D array, row-major)
CONST MAXBODY = 200          ' 蛇最长这么多格（再多本来也赢定了）
                             ' max snake length (any longer and you have already won anyway)
CONST SPEED0 = 220           ' 起始每步毫秒
                             ' milliseconds per step at the start
CONST SPEEDMIN = 80          ' 最快每步
                             ' fastest milliseconds per step
CONST SPEEDUP = 6            ' 每吃一个提速多少毫秒
                             ' how many milliseconds faster per food eaten
CONST NEED0 = 5              ' 第一关要吃几个数字
                             ' how many numbers the first level asks for
CONST PACE = 40              ' 主循环最长睡眠
                             ' longest sleep of the main loop

' 格子类型
' Cell types
CONST CELL_FREE = 0
CONST CELL_WALL = 1
CONST CELL_SNAKE = 2

CONST C_BG = &HFF101A14
CONST C_GRID = &HFF18241C
CONST C_WALL = &HFF3A5A46
CONST C_WALLLO = &HFF6A8A76
CONST C_SNAKE = &HFF6FD3FF
CONST C_HEAD = &HFFE8F4FF
CONST C_FOOD = &HFFE8C468
CONST C_BONUS = &HFFFF6A5C
CONST C_HUD = &HFF0C140E
CONST C_TEXT = &HFFEDEDF2
CONST C_DIM = &HFF9AA0B0
CONST C_OK = &HFF4ADE80

' 方向
' Directions
CONST D_UP = 0
CONST D_RIGHT = 1
CONST D_DOWN = 2
CONST D_LEFT = 3

' ══════════════════════════════════════════════════════════════════════════
'  状态
'  State
' ══════════════════════════════════════════════════════════════════════════
DIM bd(GCELLS) AS INTEGER       ' 棋盘（行优先）
                                ' board (row-major)
DIM body(MAXBODY) AS INTEGER    ' 蛇身，body(0) 是头
                                ' snake body, body(0) is the head
DIM snakeLen AS INTEGER
DIM dir AS INTEGER
DIM nextDir AS INTEGER
DIM foodCell AS INTEGER
DIM foodVal AS INTEGER
DIM score AS INTEGER
DIM level AS INTEGER
DIM eaten AS INTEGER
DIM need AS INTEGER
DIM best AS INTEGER
DIM over AS INTEGER
DIM quit AS INTEGER
DIM tickMs AS INTEGER
DIM tid AS INTEGER
DIM hint AS STRING

DIM sw AS INTEGER
DIM sh AS INTEGER
DIM cell AS INTEGER
DIM boardX AS INTEGER
DIM boardY AS INTEGER
DIM hudh AS INTEGER

' 滑动
' Swipe
DIM downX AS INTEGER
DIM downY AS INTEGER
DIM touching AS INTEGER

' 临时量
' Temporaries
DIM i AS INTEGER
DIM j AS INTEGER
DIM k AS INTEGER
DIM n AS INTEGER
DIM mt AS INTEGER
DIM rx AS INTEGER
DIM ry AS INTEGER
DIM cx AS INTEGER
DIM cy AS INTEGER
DIM nx AS INTEGER
DIM ny AS INTEGER
DIM freeN AS INTEGER
DIM pick AS INTEGER
DIM txt AS STRING

' ══════════════════════════════════════════════════════════════════════════
'  界面语言（0 = 中文 / 1 = 英文，跟随系统语言）
'  UI language (0 = Chinese / 1 = English, follows the system language)
'
'  `ui_get_language()` 是一次 syscall ⇒ **开局查一次存进 LANG**，文案也在这里一次算好，
'  `ui_get_language()` is a syscall => **query it once at startup and store it in LANG**, and the texts are computed here in one go,
'  之后每帧绘制只用变量（别在绘制路径上再调它）。
'  after that each drawn frame only uses the variables (do not call it again on the drawing path).
' ⚠ 这段**必须待在 SUB 里、模块级只留一句调用** —— 实测（whack.bas 同款写法）把 IF/ELSE
' ⚠ This block **must stay inside a SUB, with only a single call left at module level** -- measured (same shape as whack.bas): once the IF/ELSE
'   摊到模块级之后，模块级代码流里那条 `ui_rect … 0, 3, 8` 的后两个实参被读成垃圾、
'   was spread out at module level, the last two arguments of the module-level `ui_rect … 0, 3, 8` were read as garbage and
'   整屏画花（与基线帧比 diff_px 0 → 130571）；搬进 SUB 后与改动前**逐像素相同**。
'   the whole screen was painted wrong (diff_px 0 -> 130571 against the baseline frame); moved into a SUB it is **pixel-identical** to before the change.
' ══════════════════════════════════════════════════════════════════════════
DIM LANG AS INTEGER
DIM sTitle AS STRING
DIM sHintStart AS STRING
DIM sWall AS STRING
DIM sSelf AS STRING
DIM sLevelPre AS STRING
DIM sLevelSuf AS STRING
DIM sDashScore AS STRING
DIM sScore AS STRING
DIM sLevel AS STRING
DIM sProg AS STRING
DIM sPad AS STRING
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
DIM sChk14 AS STRING
DIM sNoWin AS STRING

SUB initLang()
    LANG = ui_get_language()
    IF LANG = 0 THEN sTitle = "贪吃蛇" ELSE sTitle = "Nibbles"
    IF LANG = 0 THEN sHintStart = "吃光本关数字就进下一关" ELSE sHintStart = "Eat every number to clear the level"
    IF LANG = 0 THEN sWall = "撞墙了" ELSE sWall = "Hit a wall"
    IF LANG = 0 THEN sSelf = "咬到自己了" ELSE sSelf = "Bit yourself"
    IF LANG = 0 THEN sLevelPre = "第 " ELSE sLevelPre = "Level "
    IF LANG = 0 THEN sLevelSuf = " 关！" ELSE sLevelSuf = "!"
    IF LANG = 0 THEN sDashScore = " —— 得分 " ELSE sDashScore = " - score "
    IF LANG = 0 THEN sScore = "分数" ELSE sScore = "Score"
    IF LANG = 0 THEN sLevel = "第几关" ELSE sLevel = "Level"
    IF LANG = 0 THEN sProg = "进度" ELSE sProg = "Progress"
    IF LANG = 0 THEN sPad = "手柄方向键 / 在画布上滑一下" ELSE sPad = "D-pad / swipe to steer"
    IF LANG = 0 THEN sChkHdr = "── 贪吃蛇 · 规则自检 ──" ELSE sChkHdr = "--- Nibbles: rules self-check ---"
    IF LANG = 0 THEN sChk1 = "  行列→下标→列 往返（应 5 5）:" ELSE sChk1 = "  row/col -> idx -> col roundtrip (want 5 5):"
    IF LANG = 0 THEN sChk2 = "  方向增量：上/右/下/左的行（应 -1 0 1 0）:" ELSE sChk2 = "  dRow up/right/down/left (want -1 0 1 0):"
    IF LANG = 0 THEN sChk3 = "  方向增量：上/右/下/左的列（应 0 1 0 -1）:" ELSE sChk3 = "  dCol up/right/down/left (want 0 1 0 -1):"
    IF LANG = 0 THEN sChk4 = "  不许反向：右→左（应 0）/ 右→上（应 1）:" ELSE sChk4 = "  no reverse: right->left (want 0) / right->up (want 1):"
    IF LANG = 0 THEN sChk5 = "  同向不算转向（应 0）:" ELSE sChk5 = "  same dir is not a turn (want 0):"
    IF LANG = 0 THEN sChk6 = "  关卡需求 1/2/3（应 5 7 9）:" ELSE sChk6 = "  level need 1/2/3 (want 5 7 9):"
    IF LANG = 0 THEN sChk7 = "  速度 1 关 0 吃（应 220）/ 1 关吃 10（应 160）:" ELSE sChk7 = "  speed L1 eat 0 (want 220) / L1 eat 10 (want 160):"
    IF LANG = 0 THEN sChk8 = "  左上角是墙吗（应 1）:" ELSE sChk8 = "  top-left is wall (want 1):"
    IF LANG = 0 THEN sChk9 = "  (1,1) 是空的吗（应 0）:" ELSE sChk9 = "  (1,1) is free (want 0):"
    IF LANG = 0 THEN sChk10 = "  右下角是墙吗（应 1）:" ELSE sChk10 = "  bottom-right is wall (want 1):"
    IF LANG = 0 THEN sChk11 = "  第 1 关空格数（应 360 = 20*22 - 边框 80，此时还没放蛇）:" ELSE sChk11 = "  free cells at level 1 (want 360 = 20*22 - 80 border, before the snake):"
    IF LANG = 0 THEN sChk12 = "  普通数字 3（应 3 分 / 吃 1 个）:" ELSE sChk12 = "  plain number 3 (want 3 pts / 1 eaten):"
    IF LANG = 0 THEN sChk13 = "  奖励数字 1（应 2 分 / 吃 2 个）:" ELSE sChk13 = "  bonus number 1 (want 2 pts / 2 eaten):"
    IF LANG = 0 THEN sChk14 = "  一路向上撞墙（应 1=结束）:" ELSE sChk14 = "  straight up into the wall (want 1 = over):"
    IF LANG = 0 THEN sNoWin = "（桌面脚手架：没有真窗口，规则自检打完就退出）" ELSE sNoWin = "(desktop scaffold: no real window - self-check done, exiting)"
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  纯规则（自检直接打它们的返回值）
'  Pure rules (the self-check prints their return values directly)
' ══════════════════════════════════════════════════════════════════════════

' 行列 → 一维下标
' row/col -> 1-D index
FUNCTION cellOf(r AS INTEGER, c AS INTEGER) AS INTEGER
    cellOf = r * GW + c
END FUNCTION

' 一维下标 → 列
' 1-D index -> column
FUNCTION colOf(idx AS INTEGER) AS INTEGER
    DIM r AS INTEGER
    r = idx / GW
    colOf = idx - r * GW
END FUNCTION

' 方向 → 行增量
' direction -> row delta
FUNCTION dRow(d AS INTEGER) AS INTEGER
    IF d = D_UP THEN
        dRow = 0 - 1
        EXIT FUNCTION
    END IF
    IF d = D_DOWN THEN
        dRow = 1
        EXIT FUNCTION
    END IF
    dRow = 0
END FUNCTION

' 方向 → 列增量
' direction -> column delta
FUNCTION dCol(d AS INTEGER) AS INTEGER
    IF d = D_RIGHT THEN
        dCol = 1
        EXIT FUNCTION
    END IF
    IF d = D_LEFT THEN
        dCol = 0 - 1
        EXIT FUNCTION
    END IF
    dCol = 0
END FUNCTION

' 能不能转向：不允许**直接反向**（会把脖子咬断，那是玩家最恨的一类死法）
' Can we turn: a **direct reverse** is not allowed (it snaps the neck, the death players hate most)
FUNCTION canTurn(cur AS INTEGER, want AS INTEGER) AS INTEGER
    IF want = cur THEN
        canTurn = 0
        EXIT FUNCTION
    END IF
    IF cur + want = 2 THEN
        canTurn = 0
        EXIT FUNCTION
    END IF
    IF cur + want = 4 THEN
        canTurn = 0
        EXIT FUNCTION
    END IF
    canTurn = 1
END FUNCTION

' 这一关要吃几个数
' how many numbers this level asks for
FUNCTION needOf(lv AS INTEGER) AS INTEGER
    needOf = NEED0 + (lv - 1) * 2
END FUNCTION

' 这一关每步多少毫秒
' milliseconds per step at this level
FUNCTION speedOf(lv AS INTEGER, ate AS INTEGER) AS INTEGER
    DIM t AS INTEGER
    t = SPEED0 - (lv - 1) * 10 - ate * SPEEDUP
    IF t < SPEEDMIN THEN
        t = SPEEDMIN
    END IF
    speedOf = t
END FUNCTION

' ══════════════════════════════════════════════════════════════════════════
'  局面
'  Board state
' ══════════════════════════════════════════════════════════════════════════

' 清棋盘：四周围墙 + 按关卡摆几段对称的隔墙
' Clear the board: walls around the edge plus a few symmetric inner walls per level
SUB buildBoard()
    DIM r AS INTEGER
    DIM c AS INTEGER
    DIM idx AS INTEGER

    idx = 0
    WHILE idx < GCELLS
        bd(idx) = CELL_FREE
        idx = idx + 1
    WEND

    ' 四周边框
    ' the surrounding border
    r = 0
    WHILE r < GH
        bd(cellOf(r, 0)) = CELL_WALL
        bd(cellOf(r, GW - 1)) = CELL_WALL
        r = r + 1
    WEND
    c = 0
    WHILE c < GW
        bd(cellOf(0, c)) = CELL_WALL
        bd(cellOf(GH - 1, c)) = CELL_WALL
        c = c + 1
    WEND

    ' 关卡越高、隔墙越多（但永远留出足够通路：只在固定的几处摆）
    ' higher levels get more inner walls (but always leave enough passage: they are placed only at a few fixed spots)
    IF level >= 2 THEN
        r = 5
        WHILE r <= GH - 6
            bd(cellOf(r, 6)) = CELL_WALL
            bd(cellOf(r, GW - 7)) = CELL_WALL
            r = r + 1
        WEND
        ' 断开两处，别把棋盘切成两半
        ' break two places open so the board is not cut in half
        bd(cellOf(6, 6)) = CELL_FREE
        bd(cellOf(GH - 7, 6)) = CELL_FREE
        bd(cellOf(6, GW - 7)) = CELL_FREE
        bd(cellOf(GH - 7, GW - 7)) = CELL_FREE
    END IF
    IF level >= 4 THEN
        c = 8
        WHILE c <= GW - 9
            bd(cellOf(8, c)) = CELL_WALL
            bd(cellOf(GH - 9, c)) = CELL_WALL
            c = c + 1
        WEND
        bd(cellOf(8, 9)) = CELL_FREE
        bd(cellOf(8, GW - 10)) = CELL_FREE
        bd(cellOf(GH - 9, 9)) = CELL_FREE
        bd(cellOf(GH - 9, GW - 10)) = CELL_FREE
    END IF
END SUB

' 找一个空格子（从空格子里随机挑一个）—— 先数、再挑，避免"挑到墙就重试"的死等
' Find a free cell (pick one at random among the free cells) -- count first, then pick, avoiding the retry-until-not-a-wall spin
FUNCTION freeCell() AS INTEGER
    DIM idx AS INTEGER
    DIM cnt AS INTEGER
    DIM target AS INTEGER
    cnt = 0
    idx = 0
    WHILE idx < GCELLS
        IF bd(idx) = CELL_FREE THEN
            cnt = cnt + 1
        END IF
        idx = idx + 1
    WEND
    IF cnt = 0 THEN
        freeCell = 0 - 1
        EXIT FUNCTION
    END IF
    target = ui_rand(cnt)
    idx = 0
    WHILE idx < GCELLS
        IF bd(idx) = CELL_FREE THEN
            IF target = 0 THEN
                freeCell = idx
                EXIT FUNCTION
            END IF
            target = target - 1
        END IF
        idx = idx + 1
    WEND
    freeCell = 0 - 1
END FUNCTION

' 放一个数字：数值取 1..9；**与本关编号相同**的那个是奖励（双倍分、算两个）
' Place a number: the value is 1..9; the one **equal to this level number** is the bonus (double points, counts as two)
SUB putFood()
    DIM idx AS INTEGER
    idx = freeCell()
    IF idx < 0 THEN
        EXIT SUB
    END IF
    foodCell = idx
    foodVal = 1 + ui_rand(9)
    ' 奖励数字要真的有机会出现：每关固定塞几次
    ' the bonus number must really have a chance to appear: seed it a fixed number of times per level
    IF ui_rand(3) = 0 THEN
        DIM lv9 AS INTEGER
        lv9 = level
        WHILE lv9 > 9
            lv9 = lv9 - 9
        WEND
        foodVal = lv9
    END IF
END SUB

' 按当前 tickMs 重新起搏。
' Re-arm the timer for the current tickMs.
' ⚠ **吃东西会提速**，而定时器的间隔是"设下去就定了"的 —— 不重新起搏的话
' ⚠ **Eating speeds you up**, and a timer interval is fixed the moment it is set -- without re-arming,
'   `tickMs` 改了也没用，蛇还是按第一档的速度爬（这是最容易被当成"没生效"的一处）。
'   changing `tickMs` does nothing and the snake still crawls at the first speed (this is the easiest one to mistake for not taking effect).
SUB rearmTimer()
    IF tid > 0 THEN
        ui_timer_kill(tid)
    END IF
    tid = ui_timer_set(tickMs, 0)
END SUB

' 重开一局
' Start a new game
SUB newGame()
    score = 0
    level = 1
    eaten = 0
    over = 0
    quit = 0
    best = 0
    startLevel()
END SUB

SUB startLevel()
    DIM r AS INTEGER
    DIM c AS INTEGER
    need = needOf(level)
    eaten = 0
    buildBoard()
    ' 蛇放在中间偏上、朝右，长度 3
    ' the snake starts above the middle facing right, length 3
    r = GH / 2
    c = GW / 4
    snakeLen = 3
    body(0) = cellOf(r, c)
    body(1) = cellOf(r, c - 1)
    body(2) = cellOf(r, c - 2)
    k = 0
    WHILE k < snakeLen
        bd(body(k)) = CELL_SNAKE
        k = k + 1
    WEND
    dir = D_RIGHT
    nextDir = D_RIGHT
    tickMs = speedOf(level, 0)
    putFood()
    IF tid > 0 THEN
        rearmTimer()
    END IF
    hint = sHintStart
END SUB

' 这一拍走一步
' Advance one step for this tick
SUB stepSnake()
    DIM hr AS INTEGER
    DIM hc AS INTEGER
    DIM nr AS INTEGER
    DIM nc AS INTEGER
    DIM ni AS INTEGER
    DIM tail AS INTEGER
    DIM grow AS INTEGER

    IF canTurn(dir, nextDir) = 1 THEN
        dir = nextDir
    END IF

    hr = body(0) / GW
    hc = body(0) - hr * GW
    nr = hr + dRow(dir)
    nc = hc + dCol(dir)

    ' 出界（其实四周围墙会先挡住，这里兜底）
    ' out of bounds (the surrounding walls normally block first; this is a fallback)
    IF nr < 0 THEN
        die(sWall)
        EXIT SUB
    END IF
    IF nr >= GH THEN
        die(sWall)
        EXIT SUB
    END IF
    IF nc < 0 THEN
        die(sWall)
        EXIT SUB
    END IF
    IF nc >= GW THEN
        die(sWall)
        EXIT SUB
    END IF

    ni = nr * GW + nc
    IF bd(ni) = CELL_WALL THEN
        die(sWall)
        EXIT SUB
    END IF

    ' 撞自己：**尾巴那一格不算** —— 它这一步正好会挪走，
    ' hitting yourself: **the tail cell does not count** -- it moves away on this very step,
    ' 把它算上会让"追着自己尾巴走"这种正常走位莫名判死
    ' and counting it would kill the perfectly normal move of chasing your own tail
    grow = 0
    IF ni = foodCell THEN
        grow = 1
    END IF
    IF bd(ni) = CELL_SNAKE THEN
        IF grow = 0 THEN
            tail = body(snakeLen - 1)
            IF ni <> tail THEN
                die(sSelf)
                EXIT SUB
            END IF
        ELSE
            die(sSelf)
            EXIT SUB
        END IF
    END IF

    ' 前进：整体后移一格，新头放 body(0)
    ' move forward: shift the whole body one cell and put the new head at body(0)
    IF grow = 1 THEN
        IF snakeLen < MAXBODY THEN
            snakeLen = snakeLen + 1
        END IF
    ELSE
        tail = body(snakeLen - 1)
        bd(tail) = CELL_FREE
    END IF
    k = snakeLen - 1
    WHILE k > 0
        body(k) = body(k - 1)
        k = k - 1
    WEND
    body(0) = ni
    bd(ni) = CELL_SNAKE

    IF grow = 1 THEN
        eatFood()
    END IF
END SUB

SUB eatFood()
    DIM isBonus AS INTEGER
    isBonus = 0
    DIM lv9 AS INTEGER
    lv9 = level
    WHILE lv9 > 9
        lv9 = lv9 - 9
    WEND
    IF foodVal = lv9 THEN
        isBonus = 1
    END IF

    IF isBonus = 1 THEN
        score = score + foodVal * 2
        eaten = eaten + 2
        ui_beep 1976, 165   ' 大餐：比"吃食"更高更长（取整块最高音）
                            ' big meal: higher and longer than the eat tone (takes the highest note of the block)
        ui_vibrate(40, 120)
    ELSE
        score = score + foodVal
        eaten = eaten + 1
        ui_beep 1319, 33   ' 吃食：极短（高频动作，不能吵）
                           ' eat: very short (a high-frequency action, it must not get noisy)
    END IF
    IF score > best THEN
        best = score
    END IF

    ' 提速：不是每关重算，而是**边吃边快**（原版手感）
    ' speed-up: not recomputed per level but **faster as you eat** (the original feel)
    tickMs = speedOf(level, eaten)
    IF tickMs < SPEEDMIN THEN
        tickMs = SPEEDMIN
    END IF
    rearmTimer()

    IF eaten >= need THEN
        level = level + 1
        ui_beep 1319, 320   ' 升级：长音（与"吃食"同音高但长 10 倍，一听就分得出）
                            ' level up: a long tone (same pitch as the eat tone but 10 times longer, unmistakable)
        ui_vibrate(80, 160)
        startLevel()
        hint = sLevelPre + STR$(level) + sLevelSuf
        EXIT SUB
    END IF

    putFood()
END SUB

SUB die(why AS STRING)
    over = 1
    hint = why + sDashScore + STR$(score)
    ui_beep 131, 320   ' 死：最低音、最长
                       ' death: lowest note, longest
    ui_vibrate(220, 240)
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  绘制
'  Drawing
' ══════════════════════════════════════════════════════════════════════════

SUB drawHud()
    ui_rect(0, 0, sw, hudh, C_HUD, 1, 0, 0)
    ui_text(12, 8, sScore, C_DIM, 12, 0)
    ui_text(12, 24, STR$(score), C_TEXT, 20, 0)
    ui_text(sw / 2, 8, sLevel, C_DIM, 12, 1)
    ui_text(sw / 2, 24, STR$(level), C_OK, 20, 1)
    ui_text(sw - 12, 8, sProg, C_DIM, 12, 2)
    ' ⚠ 这里**不能**写 `STR$(eaten) + "/" + STR$(need)`：同一条表达式里两次 `STR$`
    ' ⚠ You **cannot** write `STR$(eaten) + "/" + STR$(need)` here: two `STR$` in the same expression
    '   会互相覆盖（字符串函数都返回同一个 `_buf1`，谁后写谁赢），实测显示成 `0/0`。
    '   overwrite each other (all string functions return the same `_buf1`, last writer wins), and it measured as `0/0`.
    '   拆成两次 `ui_text` 摆一摆就绕开了 —— 顺带还免了拼接。
    '   Splitting it into two `ui_text` calls lays it out and sidesteps that -- and it saves the concatenation too.
    ui_text(sw - 12, 24, STR$(need), C_DIM, 20, 2)
    ui_text(sw - 34, 24, "/", C_DIM, 20, 2)
    ui_text(sw - 54, 24, STR$(eaten), C_TEXT, 20, 2)
END SUB

SUB drawBoard()
    DIM r AS INTEGER
    DIM c AS INTEGER
    DIM idx AS INTEGER
    DIM x AS INTEGER
    DIM y AS INTEGER

    ' 底色
    ' background
    ui_rect(boardX, boardY, GW * cell, GH * cell, C_GRID, 1, 0, 0)

    r = 0
    WHILE r < GH
        c = 0
        WHILE c < GW
            idx = cellOf(r, c)
            x = boardX + c * cell
            y = boardY + r * cell
            IF bd(idx) = CELL_WALL THEN
                ui_rect(x, y, cell, cell, C_WALL, 1, 0, 0)
                ui_rect(x, y, cell, 2, C_WALLLO, 1, 0, 0)
            END IF
            c = c + 1
        WEND
        r = r + 1
    WEND

    ' 蛇身
    ' the snake body
    k = snakeLen - 1
    WHILE k >= 0
        DIM sr AS INTEGER
        DIM sc AS INTEGER
        sr = body(k) / GW
        sc = body(k) - sr * GW
        x = boardX + sc * cell
        y = boardY + sr * cell
        IF k = 0 THEN
            ui_rect(x + 1, y + 1, cell - 2, cell - 2, C_HEAD, 1, 0, 3)
        ELSE
            ui_rect(x + 1, y + 1, cell - 2, cell - 2, C_SNAKE, 1, 0, 3)
        END IF
        k = k - 1
    WEND

    ' 食物
    ' the food
    IF foodCell >= 0 THEN
        DIM fr AS INTEGER
        DIM fc AS INTEGER
        fr = foodCell / GW
        fc = foodCell - fr * GW
        x = boardX + fc * cell
        y = boardY + fr * cell
        DIM col AS INTEGER
        col = C_FOOD
        DIM lv9b AS INTEGER
        lv9b = level
        WHILE lv9b > 9
            lv9b = lv9b - 9
        WEND
        IF foodVal = lv9b THEN
            col = C_BONUS
        END IF
        ui_circle(x + cell / 2, y + cell / 2, cell / 2 - 2, col, 1, 0)
        ui_text(x + cell / 2, y + cell / 2 - cell / 2 + 2, STR$(foodVal), C_HUD, cell - 6, 1)
    END IF
END SUB

SUB drawScene()
    ui_clear(C_BG)
    drawHud()
    drawBoard()
    ui_text(sw / 2, sh - 26, hint, C_DIM, 13, 1)
    ui_text(sw / 2, hudh + GH * cell + 6, sPad, C_DIM, 12, 1)
    ui_present()
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  输入
'  Input
' ══════════════════════════════════════════════════════════════════════════
SUB handleKey()
    DIM k AS INTEGER
    k = ui_msg_a()
    IF k = 27 THEN
        quit = 1
    END IF
    IF over = 1 THEN
        IF k = 13 THEN
            newGame()
        END IF
        EXIT SUB
    END IF
    IF k = 38 THEN
        nextDir = D_UP
    END IF
    IF k = 39 THEN
        nextDir = D_RIGHT
    END IF
    IF k = 40 THEN
        nextDir = D_DOWN
    END IF
    IF k = 37 THEN
        nextDir = D_LEFT
    END IF
    ' 字母键也认（外接键盘/模拟器）
    ' letter keys are accepted too (external keyboard / emulator)
    IF k = 87 THEN
        nextDir = D_UP
    END IF
    IF k = 68 THEN
        nextDir = D_RIGHT
    END IF
    IF k = 83 THEN
        nextDir = D_DOWN
    END IF
    IF k = 65 THEN
        nextDir = D_LEFT
    END IF
END SUB

SUB handlePoint(isDown AS INTEGER)
    IF isDown = 1 THEN
        downX = ui_msg_a()
        downY = ui_msg_b()
        touching = 1
        EXIT SUB
    END IF
    IF touching = 0 THEN
        EXIT SUB
    END IF
    touching = 0
    IF over = 1 THEN
        newGame()
        EXIT SUB
    END IF
    rx = ui_msg_a() - downX
    ry = ui_msg_b() - downY
    DIM ax AS INTEGER
    DIM ay AS INTEGER
    ax = rx
    IF ax < 0 THEN
        ax = 0 - ax
    END IF
    ay = ry
    IF ay < 0 THEN
        ay = 0 - ay
    END IF
    ' 划得太短当点击，不改向
    ' too short a swipe counts as a tap and does not steer
    IF ax < 16 THEN
        IF ay < 16 THEN
            EXIT SUB
        END IF
    END IF
    IF ax > ay THEN
        IF rx > 0 THEN
            nextDir = D_RIGHT
        ELSE
            nextDir = D_LEFT
        END IF
    ELSE
        IF ry > 0 THEN
            nextDir = D_DOWN
        ELSE
            nextDir = D_UP
        END IF
    END IF
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  主循环
'  Main loop
' ══════════════════════════════════════════════════════════════════════════
SUB runGame()
    ui_keep_on(1)

    hudh = 52
    DIM availW AS INTEGER
    DIM availH AS INTEGER
    availW = (sw - 16) / GW
    availH = (sh - hudh - 48) / GH
    cell = availW
    IF availH < cell THEN
        cell = availH
    END IF
    IF cell < 4 THEN
        cell = 4
    END IF
    boardX = (sw - GW * cell) / 2
    boardY = hudh + 6

    tid = 0
    newGame()
    rearmTimer()

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
            IF mt = 6 THEN
                handlePoint(1)
            END IF
            IF mt = 8 THEN
                handlePoint(0)
            END IF
            IF mt = 4 THEN
                handlePoint(1)
            END IF
            IF mt = 5 THEN
                handlePoint(0)
            END IF
            IF mt = 9 THEN
                IF over = 0 THEN
                    stepSnake()
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
'  Power-on self-check
' ══════════════════════════════════════════════════════════════════════════
SUB simCheck()
    PRINT sChkHdr
    PRINT sChk1; cellOf(5, 5); colOf(cellOf(5, 5))
    PRINT sChk2; dRow(D_UP); dRow(D_RIGHT); dRow(D_DOWN); dRow(D_LEFT)
    PRINT sChk3; dCol(D_UP); dCol(D_RIGHT); dCol(D_DOWN); dCol(D_LEFT)
    PRINT sChk4; canTurn(D_RIGHT, D_LEFT); canTurn(D_RIGHT, D_UP)
    PRINT sChk5; canTurn(D_UP, D_UP)
    PRINT sChk6; needOf(1); needOf(2); needOf(3)
    PRINT sChk7; speedOf(1, 0); speedOf(1, 10)

    ' 棋盘：边框必须是墙、中间必须是空的
    ' board: the border must be wall and the inside must be free
    level = 1
    buildBoard()
    PRINT sChk8; bd(cellOf(0, 0))
    PRINT sChk9; bd(cellOf(1, 1))
    PRINT sChk10; bd(cellOf(GH - 1, GW - 1))

    ' 空格子够不够放蛇 + 食物
    ' are there enough free cells for the snake + the food
    freeN = 0
    i = 0
    WHILE i < GCELLS
        IF bd(i) = CELL_FREE THEN
            freeN = freeN + 1
        END IF
        i = i + 1
    WEND
    PRINT sChk11; freeN

    ' 吃食物的计分：普通 vs 奖励（foodVal == 关卡号时双倍）
    ' scoring for eating: plain vs bonus (double when foodVal == the level number)
    score = 0
    eaten = 0
    level = 1
    need = needOf(1)
    foodVal = 3
    eatFood()
    PRINT sChk12; score; eaten
    score = 0
    eaten = 0
    startLevel()
    level = 1
    need = needOf(1)
    foodVal = 1
    eatFood()
    PRINT sChk13; score; eaten

    ' 撞墙必死
    ' running into a wall must die
    startLevel()
    dir = D_UP
    nextDir = D_UP
    i = 0
    WHILE i < GH + 4
        IF over = 0 THEN
            stepSnake()
        END IF
        i = i + 1
    WEND
    PRINT sChk14; over
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

' 锁竖屏 + **要系统手柄区**（第 5 个参数 1）—— 方向键交给它，程序不自己画一套
' Lock portrait + **ask for the system gamepad area** (5th argument 1) -- the arrow keys go to it, the program draws none of its own
wh = ui_win_open_ex(sTitle, sw, sh, 0, 1)

IF wh < 1 THEN
    PRINT sNoWin
ELSE
    runGame()
END IF
