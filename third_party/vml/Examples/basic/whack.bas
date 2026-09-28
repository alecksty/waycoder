' 打地鼠 —— 用 **BASIC** 写的手机游戏
' Whack-a-mole -- a mobile game written in **BASIC**
'
' 玩法：3×3 的洞里随机冒出一只地鼠，用方向键把光标移到那个洞、敲它。
' How to play: a mole pops out of one of the 3x3 holes at random; move the cursor onto that hole with the arrow keys and whack it.
' 敲中 +10 分并换一个洞；**没敲中 5 次这一局就结束**（倒计时 90 秒兜底）。
' A hit scores +10 and moves the mole to another hole; **5 misses end the round** (a 90-second countdown is the fallback).
'
' 与前面几份刻意不同：这份靠 **ui_rand + 光标式输入 + 秒级倒计时**，
' Deliberately unlike the earlier examples: this one leans on **ui_rand + cursor-style input + a seconds countdown**,
' 照的是「随机数 / 离散光标 / 计时器」这条路径，不涉及球类连续位移。
' following the "random numbers / discrete cursor / timer" path, with no continuous ball-style motion.
'
' ◆ 手机那套 UI
' ◆ The mobile UI layer
'
' 开窗 / 绘图 / 输入 / 定时器是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
' Window opening / drawing / input / timers are written in C (`Lib/shared/src/vmlui.c` -> `vmlui.vml`),
' 由 `vmltool.config.xml` 的 `<Language Name="basic" Libs="vmlui.vml">` 挂上来。
' hooked up by `<Language Name="basic" Libs="vmlui.vml">` in `vmltool.config.xml`.
'
' ◆ 三条实测踩出来的坑（都写在这里，改这份别改回去）
' ◆ Three gotchas found by measurement (all written down here; do not undo them)
'
'   ① **弹框期间必须把定时器停掉**。`ui_dlg_msg` 是模态的、会阻塞住主循环，
'   ① **Timers must be stopped while a dialog box is up**. `ui_dlg_msg` is modal and blocks the main loop,
'      而 `ui_timer_set(1000, 0)` 是**重复**定时器 —— 弹框挂多久，队列里就积压多少条
'      while `ui_timer_set(1000, 0)` is a **repeating** timer -- however long the box stays up, that many
'      `t = 9`。返回后 `left = 90` 刚重置，积压的消息被**瞬间抽干**，新一局立刻又是
'      `t = 9` messages pile up in the queue. After it returns, `left = 90` has just been reset, the backlog
'      「时间到」⇒ **弹框再也关不掉**（实测：连点两次「允许」，框都还在）。
'      is **drained in an instant**, and the new round hits "time up" again at once => **the dialog can
'      症状就是用户报的「时间太短、打不着」。修法是**宿主侧**在弹框期间把定时器全停掉
'      never be closed again** (measured: tap "Allow" twice and the box is still there). The symptom is
'      （`VmlUiCalls.WithTimersPaused`）—— 一处顶二十份例程，别在每份例程里各打一遍补丁。
'      what the user reported as "the time is too short, I cannot hit anything". The fix is on the **host side** -- all timers are stopped while a dialog is up (`VmlUiCalls.WithTimersPaused`), one place instead of twenty examples, so do not patch it into each example.
'   ② **动作键要认 A**。手机屏幕手柄把 `START` 映射成回车（VK 13）、`A` 映射成字母键
'   ② **The action key must accept A**. The on-screen mobile gamepad maps `START` to Enter (VK 13) and `A` to a letter key
'      （VK 65，见 `VmlKeys`）。前一版只认 13，而玩家在手机上自然会去按那个圆形的 A ——
'      (VK 65, see `VmlKeys`). The previous version only accepted 13, yet on a phone the player naturally presses that round A button --
'      按下去什么都没发生。现在 13 / 65 / 32 三个都当「敲」。
'      and nothing happened at all. Now 13 / 65 / 32 are all treated as "whack".
'   ③ **裸调函数的语句此前会被静默丢弃**（`ui_win_open "打地鼠", w, h` 编不出任何代码，
'   ③ **A statement that bare-calls a function used to be silently dropped** (`ui_win_open <title>, w, h` compiled to no code,
'      窗口根本不弹、只剩对话框）—— v0.96.204 已修，`CodeGenerator.Sub.cs` 现在
'      so the window never opened and only the dialog box remained) -- fixed in v0.96.204, `CodeGenerator.Sub.cs` now
'      SUB / FUNCTION 两种声明都查。
'      checks both SUB and FUNCTION declarations.
'
' ◆ 写法要求（每条都是本前端的要求或实测）
' ◆ How this must be written (every item is a requirement of this frontend or a measured fact)
'
'   · 外部过程必须 `NATIVE SUB` + **空体** 才是裸标签，否则会被加上 sub_/func_ 前缀、
'   · An external procedure must be `NATIVE SUB` + an **empty body** to become a bare label, otherwise it gets a sub_/func_ prefix and
'     链接期找不到（`CodeGenerator.Sub.cs:1456-1463`）。
'     cannot be resolved at link time (`CodeGenerator.Sub.cs:1456-1463`).
'   · 模块级变量必须在赋值前 `DIM`。
'   · Module-level variables must be `DIM`ed before they are assigned.
'   · 多个条件不写 `AND`/`OR` —— 用嵌套 IF + 标志位（本前端对逻辑运算符的支持未验证）。
'   · Do not write `AND`/`OR` for multiple conditions -- use nested IF plus flag variables (support for logical operators is unverified in this frontend).
'   · 颜色用 `&HFFFF0000` 形式：BASIC 词法器认 `&H`，不认 `0x`。
'   · Colors use the `&HFFFF0000` form: the BASIC lexer understands `&H` but not `0x`.
'   · `PRINT "…"; s` 分号不加分隔符、末尾自动补换行。
'   · In `PRINT "..."; s` the semicolon adds no separator and a newline is appended automatically at the end.

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
NATIVE FUNCTION ui_win_open(t AS STRING, w AS INTEGER, h AS INTEGER) AS INTEGER
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
NATIVE FUNCTION ui_msg_a() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_rand(n AS INTEGER) AS INTEGER
END FUNCTION
NATIVE SUB ui_beep(freq AS INTEGER, ms AS INTEGER)
END SUB
' ⚠ 形参名不能叫 `on` —— 那是 BASIC 的关键字，会把 NATIVE 声明弄坏（实测）
' ⚠ A parameter must not be named `on` -- that is a BASIC keyword and it breaks the NATIVE declaration (measured)
NATIVE SUB ui_keep_on(v AS INTEGER)
END SUB
NATIVE FUNCTION ui_dlg_msg(title AS STRING, body AS STRING, style AS INTEGER) AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_get_language() AS INTEGER
END FUNCTION

DIM cx AS INTEGER
DIM cy AS INTEGER
DIM mx AS INTEGER
DIM my AS INTEGER
DIM score AS INTEGER
DIM left AS INTEGER
DIM miss AS INTEGER
DIM over AS INTEGER
DIM w AS INTEGER
DIM h AS INTEGER
DIM cell AS INTEGER
DIM ox AS INTEGER
DIM oy AS INTEGER
DIM t AS INTEGER
DIM k AS INTEGER
DIM tid AS INTEGER
DIM hit AS INTEGER
DIM i AS INTEGER
DIM dlg AS INTEGER
DIM c2 AS INTEGER
DIM hx AS INTEGER
DIM hy AS INTEGER
DIM mi AS INTEGER

' ── 界面语言（0 = 中文 / 1 = 英文，跟随系统语言）──────────────────────
' ── UI language (0 = Chinese / 1 = English, follows the system language) ──────────────────────
' `ui_get_language()` 是一次 syscall ⇒ **开局查一次存进 LANG**，文案也在这里一次算好，
' `ui_get_language()` is one syscall => **query it once at startup and store it in LANG**, and compute the strings here in one go;
' 之后每帧绘制只用变量（别在绘制路径上再调它）。
' afterwards each draw frame only reads variables (do not call it again on the draw path).
DIM LANG AS INTEGER
DIM sTitle AS STRING
DIM sScore AS STRING
DIM sTime AS STRING
DIM sLives AS STRING
DIM sHint AS STRING
DIM sRestart AS STRING
DIM sTimeUp AS STRING
DIM sMiss5 AS STRING

' ⚠ 这段**必须待在 SUB 里、模块级只留一句调用** —— 实测（whack.bas 同款写法）把 IF/ELSE
' ⚠ This block **must stay inside a SUB, with only a single call at module level** -- measured (the same style as this whack.bas): once the IF/ELSE
'   摊到模块级之后，模块级代码流里那条 `ui_rect … 0, 3, 8` 的后两个实参被读成垃圾、
'   is spread out at module level, the last two arguments of that `ui_rect ... 0, 3, 8` call in the module-level code are read as garbage and
'   整屏画花（与基线帧比 diff_px 0 → 130571）；搬进 SUB 后与改动前**逐像素相同**。
'   the whole screen is smeared (diff_px vs the baseline frame went 0 -> 130571); moved back into a SUB it is **pixel-for-pixel identical** to before the change.
SUB initLang()
    LANG = ui_get_language()
    IF LANG = 0 THEN sTitle = "打地鼠" ELSE sTitle = "Whack-a-Mole"
    IF LANG = 0 THEN sScore = "得分" ELSE sScore = "Score"
    IF LANG = 0 THEN sTime = "剩余秒" ELSE sTime = "Time"
    IF LANG = 0 THEN sLives = "机会" ELSE sLives = "Lives"
    IF LANG = 0 THEN sHint = "方向键移动，A 或 START 敲" ELSE sHint = "Arrows move, A whacks"
    IF LANG = 0 THEN sRestart = "按 A 或 START 重开" ELSE sRestart = "Press A to restart"
    IF LANG = 0 THEN sTimeUp = "时间到！得分见左上角。再来一局？（选「否」退出）" ELSE sTimeUp = "Time up! Score is top-left. Play again? (choose 'No' to quit)"
    IF LANG = 0 THEN sMiss5 = "5 次没打着，这一局结束。再来一局？（选「否」退出）" ELSE sMiss5 = "5 misses, round over. Play again? (choose 'No' to quit)"
END SUB

initLang

w = ui_scr_w()
h = ui_scr_h()
IF w <= 0 THEN w = 360
IF h <= 0 THEN h = 620
ui_win_open sTitle, w, h
ui_keep_on 1

cell = (w - 40) / 3
IF cell > 150 THEN cell = 150
ox = (w - cell * 3) / 2
oy = 110

cx = 1
cy = 1
score = 0
miss = 0
left = 90
over = 0
mx = ui_rand(3)
my = ui_rand(3)

tid = ui_timer_set(1000, 0)

WHILE ui_win_closed() = 0
    ' ── draw ──
    ui_clear &HFF10E818
    ui_text 8, 8, sScore, &HFF9AA0B0, 13, 0
    ui_rect 58, 11, score, 10, &HFF5238A8, 1, 0, 0
    ui_text w / 2 + 20, 8, sTime, &HFF9AA0B0, 13, 1
    ui_rect w / 2 + 80, 11, left * 2, 10, &HFF00F800, 1, 0, 0

    ' 剩余机会：5 个点，打空一次暗一个
    ' Lives left: 5 dots; each miss dims one
    ui_text 8, 34, sLives, &HFF9AA0B0, 13, 0
    mi = 0
    WHILE mi < 5
        IF mi < 5 - miss THEN
            ui_circle 62 + mi * 20, 40, 7, &HFFFF8A00, 1, 0
        ELSE
            ui_circle 62 + mi * 20, 40, 7, &HFF4A4040, 1, 0
        END IF
        mi = mi + 1
    WEND

    r = 0
    WHILE r < 3
        c2 = 0
        WHILE c2 < 3
            hx = ox + c2 * cell
            hy = oy + r * cell
            ' 洞：深色圆
            ' Hole: a dark circle
            ui_circle hx + cell / 2, hy + cell / 2, cell / 2 - 8, &HFF241C10, 1, 0
            ' 地鼠：亮圆（只在它那一格）
            ' Mole: a bright circle (only in its own cell)
            IF r = my THEN
                IF c2 = mx THEN
                    IF over = 0 THEN
                        ui_circle hx + cell / 2, hy + cell / 2, cell / 2 - 20, &HFFFF8A00, 1, 0
                    END IF
                END IF
            END IF
            ' 光标：方框
            ' Cursor: a square outline
            IF r = cy THEN
                IF c2 = cx THEN
                    ui_rect hx + 6, hy + 6, cell - 12, cell - 12, &HFFFFE000, 0, 3, 8
                END IF
            END IF
            c2 = c2 + 1
        WEND
        r = r + 1
    WEND

    IF over = 0 THEN
        ui_text w / 2, h - 40, sHint, &HFF9AA0B0, 14, 1
    ELSE
        ui_text w / 2, h - 40, sRestart, &HFFFFE000, 16, 1
    END IF
    ui_present

    t = ui_wait_msg(0)
    IF t = 10 THEN
        EXIT WHILE
    END IF

    ' ── 秒表：90 秒兜底 ──
    ' ── Stopwatch: the 90-second fallback ──
    IF t = 9 THEN
        IF over = 0 THEN
            left = left - 1
            IF left <= 0 THEN
                over = 1
                ui_beep 220, 300
                ui_present
                ' 弹框期间**宿主会把定时器停掉**（VmlUiCalls.WithTimersPaused）——
                ' While the dialog is up the **host stops the timers** (VmlUiCalls.WithTimersPaused) --
                ' 这里不必自己 kill/restart，那会变成同一件事的第二套机制（见文件头 ①）
                ' there is no need to kill/restart it here, that would be a second mechanism for the same thing (see ① at the top of the file)
                ' 选「否/拒绝」→ 关窗退出（ui_dlg_msg 返回 0=是 / 1=否）
                ' Choosing "No"/reject -> close the window and quit (ui_dlg_msg returns 0=yes / 1=no)
                dlg = ui_dlg_msg(sTitle, sTimeUp, 0)
                IF dlg <> 0 THEN
                    ui_win_close
                    EXIT WHILE
                END IF
                score = 0
                miss = 0
                left = 90
                over = 0
                mx = ui_rand(3)
                my = ui_rand(3)
            END IF
        END IF
    END IF

    IF t = 1 THEN
        k = ui_msg_a()
        IF k = 27 THEN
            EXIT WHILE
        END IF

        ' 动作键：回车 13 / A 65 / 空格 32（手机手柄上 A 才是"那个圆键"，见文件头 ②）
        ' Action keys: Enter 13 / A 65 / Space 32 (on the mobile gamepad A is that round button, see ② at the top of the file)
        hit = 0
        IF k = 13 THEN hit = 1
        IF k = 65 THEN hit = 1
        IF k = 32 THEN hit = 1

        IF hit = 1 THEN
            IF over = 0 THEN
                IF cx = mx THEN
                    IF cy = my THEN
                        ' 敲中：加分 + 换洞
                        ' Hit: add score + move the mole
                        score = score + 10
                        ui_beep 1318, 40
                        mx = ui_rand(3)
                        my = ui_rand(3)
                    ELSE
                        miss = miss + 1
                        ui_beep 330, 60
                    END IF
                ELSE
                    miss = miss + 1
                    ui_beep 330, 60
                END IF

                ' ★ 没打到 5 次 → 这一局结束
                ' ★ 5 misses -> the round is over
                IF miss >= 5 THEN
                    over = 1
                    ui_beep 220, 300
                    ui_present
                    ' 选「否/拒绝」→ 关窗退出（同上）
                    ' Choosing "No"/reject -> close the window and quit (same as above)
                    dlg = ui_dlg_msg(sTitle, sMiss5, 0)
                    IF dlg <> 0 THEN
                        ui_win_close
                        EXIT WHILE
                    END IF
                    score = 0
                    miss = 0
                    left = 90
                    over = 0
                    mx = ui_rand(3)
                    my = ui_rand(3)
                END IF
            ELSE
                score = 0
                miss = 0
                left = 90
                over = 0
                mx = ui_rand(3)
                my = ui_rand(3)
            END IF
        END IF

        IF k = 37 THEN
            cx = cx - 1
            IF cx < 0 THEN cx = 0
        END IF
        IF k = 39 THEN
            cx = cx + 1
            IF cx > 2 THEN cx = 2
        END IF
        IF k = 38 THEN
            cy = cy - 1
            IF cy < 0 THEN cy = 0
        END IF
        IF k = 40 THEN
            cy = cy + 1
            IF cy > 2 THEN cy = 2
        END IF
    END IF
WEND

ui_timer_kill tid
ui_keep_on 0
ui_win_close
