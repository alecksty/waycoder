' ═══════════════════════════════════════════════════════════════════════
' 文本控制台垫层（_tty.bas）—— 给 1970~80 年代那批"打字机式"老 BASIC 程序用
' Text console shim (_tty.bas) -- for the 1970s-80s "typewriter style" legacy BASIC programs
'
' **为什么需要它**：老程序的输出模型是「`PRINT` 顺序打字 + `INPUT` 读一行」，
' **Why it is needed**: those programs output with "`PRINT` types in order + `INPUT` reads one line",
' 而本平台这两条在手机上都不成立（都是实测过的，不是推测）：
' and on this platform neither one works on mobile (both measured, not guessed):
'   · `PRINT` 走 stdout，而窗口化运行时 stdout 被宿主重定向进内存缓冲
'   · `PRINT` goes to stdout, but in windowed mode the host redirects stdout into an in-memory buffer
'     ⇒ **游戏窗口里一个字都看不见**
'     => **not one character shows up in the game window**
'   · `LOCATE`/`COLOR`/`CLS` 写的是 `0xB8000` 那段 VGA 文本显存，宿主把 VGA
'   · `LOCATE`/`COLOR`/`CLS` write to the VGA text memory at `0xB8000`, and the host squashes VGA
'     压成 1×1 ⇒ 同样不可见
'     down to 1x1 => equally invisible
'   · `INPUT` 在窗口路径下 `ReadString()` 返回空串、`ReadInt()` 返回 0
'   · on the windowed path `INPUT` gets an empty string from `ReadString()` and 0 from `ReadInt()`
'
' 这层用 `ui_*` 重建一个**等宽字符网格**，把老程序那三件事接管掉，
' This layer rebuilds a **monospaced character grid** on `ui_*` and takes over those three things,
' 于是老源码可以**保留行号 + GOTO + GOSUB + FOR/NEXT 的原结构**直接跑。
' so the old source can run as-is while **keeping its line numbers + GOTO + GOSUB + FOR/NEXT structure**.
'
' ◆ 设计：**不缓冲，即时上屏**
' ◆ Design: **no buffering, paint immediately**
'   第一版用字符串数组做屏缓，整个失效 —— 实测是两个前端缺陷叠加：
'   The first version buffered the screen in a string array and failed completely -- measured as two frontend bugs stacked:
'     · `cases/31`：字符串数组在 **SUB 内**"写后即读"读到的是**旧值**
'     · `cases/31`: a string array read right after a write **inside a SUB** returns the **stale value**
'     · `cases/29`：字符串数组元素**直接进表达式**被读成整数（地址）
'     · `cases/29`: a string array element **used directly in an expression** is read as an integer (its address)
'   两个都只影响 SUB 内的读写（顶层是好的），而垫层的缓冲逻辑按定义全在 SUB 里。
'   Both hit only reads/writes inside a SUB (top level is fine), and this layer buffers entirely inside SUBs by definition.
'   ⇒ 改成**不缓冲**：`ttyP` 画完立刻 `ui_text` 上屏，只用一个行号计数器。
'   => switched to **no buffering**: `ttyP` calls `ui_text` immediately and keeps only a row counter.
'   代价是**没有回滚历史**（超屏就清屏重来）—— 对本批"一屏装得下"的老游戏无影响。
'   The cost is **no scrollback history** (clear and restart when past the last row) -- harmless for this batch of old games that fit on one screen.
'
' ◆ 用法（见同目录 hurkle.bas）
' ◆ Usage (see hurkle.bas in the same directory)
'     ttyOpen("HURKLE", 0, 0, 20)   ' 标题 / 旋转 / 手柄 / 字号
'     args: title / rotation / gamepad / font size
'     ttyCls
'     ttyP("A HURKLE IS HIDING...")
'     x = ttyAsk("X 坐标 (0-9)", 0, 9)
'     x = ttyAsk("X coordinate (0-9)", 0, 9)
'
' ◆ 三条硬约束（前两条来自本前端，第三条是这类老代码的通行雷）
' ◆ Three hard constraints (the first two come from this frontend, the third is a common trap in this kind of legacy code)
'   ① `NATIVE SUB/FUNCTION` 必须带**空体**
'   ① `NATIVE SUB/FUNCTION` must carry an **empty body**
'   ② 形参名不能是 BASIC 关键字
'   ② a parameter name must not be a BASIC keyword
'   ③ **`ELSEIF` 分支的最后一条不能是单行 `IF … THEN <语句>`** —— 它会把下一行
'   ③ **the last statement of an `ELSEIF` branch must not be a single-line `IF ... THEN <statement>`** -- it swallows the next line
'      吞进自己的 THEN 分支 ⇒ 块永不闭合 ⇒ **其后整个文件被静默丢弃**。
'      into its own THEN branch => the block never closes => **everything after it is silently dropped**.
'      `cases/30` 是最小复现；转换层遇到这种形态要展开成三行块。
'      `cases/30` is the minimal repro; a converter layer must expand this shape into a three-line block.
' ═══════════════════════════════════════════════════════════════════════

NATIVE SUB ui_clear(c AS INTEGER)
END SUB
NATIVE SUB ui_rect(x AS INTEGER, y AS INTEGER, w AS INTEGER, h AS INTEGER, c AS INTEGER, f AS INTEGER, lw AS INTEGER, r AS INTEGER)
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
NATIVE FUNCTION ui_msg_a() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_msg_b() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_rand(n AS INTEGER) AS INTEGER
END FUNCTION
NATIVE SUB ui_beep(f AS INTEGER, ms AS INTEGER)
END SUB
NATIVE SUB ui_sfx_add(ch AS INTEGER, note AS INTEGER, delay AS INTEGER, dur AS INTEGER, vel AS INTEGER, wave AS INTEGER)
END SUB
NATIVE SUB ui_sfx_tick()
END SUB
NATIVE SUB ui_sfx_panic()
END SUB
NATIVE SUB ui_keep_on(on AS INTEGER)
END SUB

' ── 状态（全部是模块级标量：不用数组，见头部说明）─────────────────────
' ── State (all module-level scalars: no arrays, see the header note)─────────────────────
DIM ttyW AS INTEGER
DIM ttyH AS INTEGER
DIM ttyFont AS INTEGER
DIM ttyLH AS INTEGER
DIM ttyCharW AS INTEGER
DIM ttyCols AS INTEGER
DIM ttyRows AS INTEGER
DIM ttyTop AS INTEGER
DIM ttyLeft AS INTEGER
DIM ttyBarTop AS INTEGER
DIM ttyR AS INTEGER
DIM ttyC AS INTEGER
DIM ttyBarOn AS INTEGER
DIM ttyM AS INTEGER
DIM ttyA AS INTEGER
DIM ttyB AS INTEGER
DIM ttyK AS INTEGER
DIM ttyVal AS INTEGER
DIM ttyLo AS INTEGER
DIM ttyHi AS INTEGER
DIM ttyI AS INTEGER
DIM ttyBg AS INTEGER
DIM ttyFg AS INTEGER
DIM ttyDim AS INTEGER
DIM ttyAccent AS INTEGER
DIM ttyBarBg AS INTEGER
DIM ttyNum AS STRING
DIM ttyPrompt AS STRING

SUB ttyOpen(title AS STRING, rot AS INTEGER, pad AS INTEGER, font AS INTEGER)
    DIM r AS INTEGER
    ttyFont = font
    ttyBg = &HFF101018
    ttyFg = &HFFD8D8E0
    ttyDim = &HFF8888A0
    ttyAccent = &HFF7CC4FF
    ttyBarBg = &HFF1C1C28
    ttyCharW = ttyFont / 2
    r = ui_win_open_ex(title, 0, 0, rot, pad)
    ui_keep_on(1)
    ttyW = ui_scr_w()
    ttyH = ui_scr_h()
    ttyLH = ttyFont + 8
    ttyCols = ttyW / ttyCharW
    IF ttyCols > 88 THEN
        ttyCols = 88
    END IF
    ttyBarTop = ttyH - 150
    ttyRows = (ttyBarTop - 12) / ttyLH
    IF ttyRows > 40 THEN
        ttyRows = 40
    END IF
    IF ttyRows < 6 THEN
        ttyRows = 6
    END IF
    ttyTop = 10
    ttyLeft = 10
    ttyBarOn = 0
    ttyCls
END SUB

SUB ttyShow()
    ui_present
END SUB

SUB ttyCls()
    ui_clear(ttyBg)
    ttyR = 0
    ttyC = 0
    ttyBarOn = 0
    ui_present
END SUB

' 擦掉提示条（回到"只有正文"的画面）
' Erase the prompt bar (back to a screen with only the body text)
SUB ttyBarClear()
    IF ttyBarOn = 0 THEN
        EXIT SUB
    END IF
    ui_rect(0, ttyBarTop - 6, ttyW, ttyH - ttyBarTop + 6, ttyBg, 1, 0, 0)
    ttyBarOn = 0
END SUB

' 行号推进；超屏就清屏重来（不缓冲 ⇒ 没有回滚历史，见头部说明）
' Advance the row counter; clear and restart past the last row (no buffering => no scrollback, see the header note)
SUB ttyNl()
    ttyR = ttyR + 1
    ttyC = 0
    IF ttyR >= ttyRows THEN
        ttyCls
    END IF
END SUB

SUB ttyScrollCheck()
    IF ttyR >= ttyRows THEN
        ttyCls
    END IF
END SUB

' 在当前行追加一段（不换行）
' Append a segment to the current line (no newline)
SUB ttyPn(s AS STRING)
    DIM x AS INTEGER
    IF LEN(s) = 0 THEN
        EXIT SUB
    END IF
    ttyBarClear
    ttyScrollCheck
    x = ttyLeft + ttyC * ttyCharW
    ui_text(x, ttyTop + ttyR * ttyLH, s, ttyFg, ttyFont, 0)
    ttyC = ttyC + LEN(s)
    IF ttyC >= ttyCols THEN
        ttyNl
    END IF
    ui_present
END SUB

' 打一整行（老 BASIC 的 `PRINT "..."` 对应这个）
' Print a whole line (this is what an old BASIC `PRINT "..."` maps to)
SUB ttyP(s AS STRING)
    ttyPn(s)
    ttyNl
END SUB

' 居中打一行 —— 老 BASIC 的 `TAB(n)` 在等宽 80 列下就是"居中"，
' Print one line centered -- an old BASIC `TAB(n)` under 80 monospaced columns just means "centered",
' 而 TAB 在本平台是**静默失效**的（只有一个空实现，不移动光标）。
' while on this platform TAB **silently does nothing** (there is only an empty stub, it never moves the cursor).
' ⚠ 本 SUB 的**形状**是对着 `ttyPn` 抄的，不是随便写的：
' ⚠ The **shape** of this SUB is copied from `ttyPn` on purpose, not written casually:
'   · 开头有 `IF LEN(s) = 0 THEN EXIT SUB` 早退（与 ttyPn 一致）
'   · it early-returns with `IF LEN(s) = 0 THEN EXIT SUB` at the top (same as ttyPn)
'   · 先算进**局部变量 x**，再拿 x 去调 `ui_text`（不把表达式直接当实参）
'   · it computes into a **local variable x** first, then passes x to `ui_text` (never an expression as the argument)
' 实测（2026-09-21）：缺这两条的写法**编译全绿、函数确实被调用（插 PRINT 探针能打出来）、
' Measured (2026-09-21): without those two, the code **compiles green and the function really is called (a PRINT probe fires),
' 但整屏一个字都不显示**，而且**连它之后的 ttyP 也一起不显示**。
' but not one character appears on screen**, and **even the ttyP calls after it stop showing**.
' 逐条排除过：EXIT SUB 在多行 IF 里 ✅、局部变量与形参同名 ✅、`#include` ✅、
' Ruled out one by one: EXIT SUB inside a multi-line IF ✅, a local variable sharing the parameter name ✅, `#include` ✅,
' 实参传表达式 ✅（顶层与 SUB 内都测过）、`LEN(参数)` ✅、ttyRows/ttyR 数值全对 ✅。
' an expression as the argument ✅ (tested at top level and inside a SUB), `LEN(param)` ✅, ttyRows/ttyR all correct ✅.
' 改成同形之后正常。**机制没查清** —— 所以这条按"已知可用的写法"钉住，别再改回去。
' Matching the shape fixed it. **The mechanism was never pinned down** -- so treat this as a known-good shape, do not change it back.
SUB ttyPc(s AS STRING)
    DIM x AS INTEGER
    DIM ind AS INTEGER
    IF LEN(s) = 0 THEN
        EXIT SUB
    END IF
    ttyBarClear
    ttyScrollCheck
    ind = (ttyCols - LEN(s)) / 2
    IF ind < 0 THEN
        ind = 0
    END IF
    x = ttyLeft + ind * ttyCharW
    ui_text(x, ttyTop + ttyR * ttyLH, s, ttyFg, ttyFont, 0)
    ttyNl
    ui_present
END SUB

' ── 输入：数字步进条 ──────────────────────────────────────────────────
' ── Input: numeric stepper ──────────────────────────────────────────────────
' 老游戏的 `INPUT X` 在这里变成「− / + 调数 + 确定」。
' The old `INPUT X` becomes "- / + to adjust plus a confirm button" here.
' 注意**不调 ui_clear** —— 正文要留在屏上，只重画底部那一条。
' Note it does **not call ui_clear** -- the body text must stay on screen, only the bottom strip is repainted.
FUNCTION ttyAsk(prompt AS STRING, lo AS INTEGER, hi AS INTEGER) AS INTEGER
    DIM bw AS INTEGER
    DIM bh AS INTEGER
    DIM bx1 AS INTEGER
    DIM bx2 AS INTEGER
    DIM bx3 AS INTEGER
    DIM by AS INTEGER
    DIM quit AS INTEGER
    DIM lab AS STRING
    ttyVal = lo
    ttyLo = lo
    ttyHi = hi
    ttyPrompt = prompt
    bw = 104
    bh = 72
    by = ttyH - 96
    bx1 = 16
    bx3 = ttyW - 16 - bw
    bx2 = (ttyW - 140) / 2
    quit = 0
    WHILE quit = 0
        ui_sfx_tick
        lab = STR$(ttyVal)
        ui_rect(0, ttyBarTop - 6, ttyW, ttyH - ttyBarTop + 6, ttyBarBg, 1, 0, 0)
        ui_text(ttyW / 2, ttyBarTop + 4, ttyPrompt, ttyFg, ttyFont, 1)
        ui_text(ttyW / 2, ttyBarTop + 34, lab, ttyAccent, ttyFont + 12, 1)
        ui_rect(bx1, by, bw, bh, &HFF303048, 1, 0, 10)
        ui_text(bx1 + bw / 2, by + 22, "-", &HFFFFFFFF, ttyFont + 10, 1)
        ui_rect(bx2, by, 140, bh, &HFF2A6E3A, 1, 0, 10)
        ui_text(bx2 + 70, by + 22, "确定", &HFFFFFFFF, ttyFont, 1)
        ui_rect(bx3, by, bw, bh, &HFF303048, 1, 0, 10)
        ui_text(bx3 + bw / 2, by + 22, "+", &HFFFFFFFF, ttyFont + 10, 1)
        ttyBarOn = 1
        ui_present
        ttyM = ui_wait_msg(0)
        IF ttyM = 10 THEN
            ui_win_close
            quit = 1
        ELSEIF ttyM = 6 THEN
            ttyA = ui_msg_a()
            ttyB = ui_msg_b()
            IF ttyB >= by THEN
                IF ttyB <= by + bh THEN
                    IF ttyA >= bx1 THEN
                        IF ttyA <= bx1 + bw THEN
                            ttyVal = ttyVal - 1
                            IF ttyVal < ttyLo THEN
                                ttyVal = ttyLo
                            END IF
                            ui_sfx_add 0, 67, 0, 1, 40, 3   ' 减
                            ' Minus
                        END IF
                    END IF
                    IF ttyA >= bx3 THEN
                        IF ttyA <= bx3 + bw THEN
                            ttyVal = ttyVal + 1
                            IF ttyVal > ttyHi THEN
                                ttyVal = ttyHi
                            END IF
                            ui_sfx_add 0, 72, 0, 1, 40, 3   ' 加
                            ' Plus
                        END IF
                    END IF
                    IF ttyA >= bx2 THEN
                        IF ttyA <= bx2 + 140 THEN
                            quit = 1
                            ui_sfx_add 1, 84, 0, 2, 65, 1   ' 确定
                            ' Confirm
                        END IF
                    END IF
                END IF
            END IF
        ELSEIF ttyM = 1 THEN
            ttyK = ui_msg_a()
            IF ttyK = 37 THEN
                ttyVal = ttyVal - 1
                IF ttyVal < ttyLo THEN
                    ttyVal = ttyLo
                END IF
            ELSEIF ttyK = 39 THEN
                ttyVal = ttyVal + 1
                IF ttyVal > ttyHi THEN
                    ttyVal = ttyHi
                END IF
            ELSEIF ttyK = 13 THEN
                quit = 1
            ELSEIF ttyK = 65 THEN
                quit = 1
            END IF
        END IF
    WEND
    ttyBarClear
    ttyAsk = ttyVal
    ttyPn("> ")
    lab = STR$(ttyVal)
    ttyP(lab)
END FUNCTION

' ── 输入：等一个键 / 一次点击（老程序的「按任意键继续」）──────────────
' ── Input: wait for one key / one tap (the old "press any key to continue")──────────────
FUNCTION ttyKey() AS INTEGER
    DIM got AS INTEGER
    got = 0
    ttyKey = 0
    WHILE got = 0
        ui_sfx_tick
        ttyM = ui_wait_msg(0)
        IF ttyM = 10 THEN
            ui_win_close
            got = 1
        ELSEIF ttyM = 1 THEN
            ttyKey = ui_msg_a()
            got = 1
        ELSEIF ttyM = 6 THEN
            ttyKey = 32
            got = 1
        END IF
    WEND
END FUNCTION

SUB ttyWait()
    ttyI = ttyKey()
END SUB
