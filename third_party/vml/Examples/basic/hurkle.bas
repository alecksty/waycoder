' ═══════════════════════════════════════════════════════════════════════
' HURKLE —— 由 scripts/basic-port.py 从老 BASIC 源码**机械转换**而来
'
' 原始来源：hurkle.bas
' 版权：David Ahl《BASIC Computer Games》合集已进入**公有领域**（UNLICENSE），
'       可随包分发 —— 与 gorilla/nibbles 那些"只参考玩法、代码自己写"的不同。
'
' 转换保留：**行号、GOTO、GOSUB、FOR/NEXT 的原结构**（逐行对应）
' 转换改的：PRINT→ttyP（本平台 PRINT 在游戏窗口里不可见）、INPUT→ttyAsk（手机上不敲键盘）、
'           CLS/LOCATE/COLOR→垫层（它们写的是被宿主关掉的 VGA 显存）、INT(n*RND(1))→ui_rand(n)
' 垫层说明见 _tty.bas 头部。重新生成请跑：
'     python3 scripts/basic-port.py <原始.bas> <本文件> --title "HURKLE"
' ═══════════════════════════════════════════════════════════════════════

' ═══════════════════════════════════════════════════════════════════════
' 文本控制台垫层（_tty.bas）—— 给 1970~80 年代那批"打字机式"老 BASIC 程序用
'
' **为什么需要它**：老程序的输出模型是「`PRINT` 顺序打字 + `INPUT` 读一行」，
' 而本平台这两条在手机上都不成立（都是实测过的，不是推测）：
'   · `PRINT` 走 stdout，而窗口化运行时 stdout 被宿主重定向进内存缓冲
'     ⇒ **游戏窗口里一个字都看不见**
'   · `LOCATE`/`COLOR`/`CLS` 写的是 `0xB8000` 那段 VGA 文本显存，宿主把 VGA
'     压成 1×1 ⇒ 同样不可见
'   · `INPUT` 在窗口路径下 `ReadString()` 返回空串、`ReadInt()` 返回 0
'
' 这层用 `ui_*` 重建一个**等宽字符网格**，把老程序那三件事接管掉，
' 于是老源码可以**保留行号 + GOTO + GOSUB + FOR/NEXT 的原结构**直接跑。
'
' ◆ 设计：**不缓冲，即时上屏**
'   第一版用字符串数组做屏缓，整个失效 —— 实测是两个前端缺陷叠加：
'     · `cases/31`：字符串数组在 **SUB 内**"写后即读"读到的是**旧值**
'     · `cases/29`：字符串数组元素**直接进表达式**被读成整数（地址）
'   两个都只影响 SUB 内的读写（顶层是好的），而垫层的缓冲逻辑按定义全在 SUB 里。
'   ⇒ 改成**不缓冲**：`ttyP` 画完立刻 `ui_text` 上屏，只用一个行号计数器。
'   代价是**没有回滚历史**（超屏就清屏重来）—— 对本批"一屏装得下"的老游戏无影响。
'
' ◆ 用法（见同目录 hurkle.bas）
'     ttyOpen("HURKLE", 0, 0, 20)   ' 标题 / 旋转 / 手柄 / 字号
'     ttyCls
'     ttyP("A HURKLE IS HIDING...")
'     x = ttyAsk("X 坐标 (0-9)", 0, 9)
'
' ◆ 三条硬约束（前两条来自本前端，第三条是这类老代码的通行雷）
'   ① `NATIVE SUB/FUNCTION` 必须带**空体**
'   ② 形参名不能是 BASIC 关键字
'   ③ **`ELSEIF` 分支的最后一条不能是单行 `IF … THEN <语句>`** —— 它会把下一行
'      吞进自己的 THEN 分支 ⇒ 块永不闭合 ⇒ **其后整个文件被静默丢弃**。
'      `cases/30` 是最小复现；转换层遇到这种形态要展开成三行块。
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
NATIVE SUB ui_keep_on(on AS INTEGER)
END SUB

' ── 状态（全部是模块级标量：不用数组，见头部说明）─────────────────────
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
SUB ttyBarClear()
    IF ttyBarOn = 0 THEN
        EXIT SUB
    END IF
    ui_rect(0, ttyBarTop - 6, ttyW, ttyH - ttyBarTop + 6, ttyBg, 1, 0, 0)
    ttyBarOn = 0
END SUB

' 行号推进；超屏就清屏重来（不缓冲 ⇒ 没有回滚历史，见头部说明）
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
SUB ttyP(s AS STRING)
    ttyPn(s)
    ttyNl
END SUB

' 居中打一行 —— 老 BASIC 的 `TAB(n)` 在等宽 80 列下就是"居中"，
' 而 TAB 在本平台是**静默失效**的（只有一个空实现，不移动光标）。
' ⚠ 本 SUB 的**形状**是对着 `ttyPn` 抄的，不是随便写的：
'   · 开头有 `IF LEN(s) = 0 THEN EXIT SUB` 早退（与 ttyPn 一致）
'   · 先算进**局部变量 x**，再拿 x 去调 `ui_text`（不把表达式直接当实参）
' 实测（2026-09-21）：缺这两条的写法**编译全绿、函数确实被调用（插 PRINT 探针能打出来）、
' 但整屏一个字都不显示**，而且**连它之后的 ttyP 也一起不显示**。
' 逐条排除过：EXIT SUB 在多行 IF 里 ✅、局部变量与形参同名 ✅、`#include` ✅、
' 实参传表达式 ✅（顶层与 SUB 内都测过）、`LEN(参数)` ✅、ttyRows/ttyR 数值全对 ✅。
' 改成同形之后正常。**机制没查清** —— 所以这条按"已知可用的写法"钉住，别再改回去。
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
' 老游戏的 `INPUT X` 在这里变成「− / + 调数 + 确定」。
' 注意**不调 ui_clear** —— 正文要留在屏上，只重画底部那一条。
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
                            ui_beep(660, 20)
                        END IF
                    END IF
                    IF ttyA >= bx3 THEN
                        IF ttyA <= bx3 + bw THEN
                            ttyVal = ttyVal + 1
                            IF ttyVal > ttyHi THEN
                                ttyVal = ttyHi
                            END IF
                            ui_beep(880, 20)
                        END IF
                    END IF
                    IF ttyA >= bx2 THEN
                        IF ttyA <= bx2 + 140 THEN
                            quit = 1
                            ui_beep(1180, 40)
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
FUNCTION ttyKey() AS INTEGER
    DIM got AS INTEGER
    got = 0
    ttyKey = 0
    WHILE got = 0
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

ttyOpen("HURKLE", 0, 0, 20)

10 ttyPc("HURKLE")
20 ttyPc("CREATIVE COMPUTING  MORRISTOWN, NEW JERSEY")
30 ttyP("")
ttyP("")
ttyP("")
110 N=5
120 G=10
210 ttyP("")
220 ttyP("A HURKLE IS HIDING ON A" + " " + STR$(G) + " " + "BY" + " " + STR$(G) + " " + "GRID. HOMEBASE")
230 ttyP("ON THE GRID IS POINT 0,0 IN THE SOUTHWEST CORNER,")
235 ttyP("AND ANY POINT ON THE GRID IS DESIGNATED BY A")
240 ttyP("PAIR OF WHOLE NUMBERS SEPERATED BY A COMMA. THE FIRST")
245 ttyP("NUMBER IS THE HORIZONTAL POSITION AND THE SECOND NUMBER")
246 ttyP("IS THE VERTICAL POSITION. YOU MUST TRY TO")
250 ttyP("GUESS THE HURKLE'S GRIDPOINT. YOU GET" + " " + STR$(N) + " " + "TRIES.")
260 ttyP("AFTER EACH TRY, I WILL TELL YOU THE APPROXIMATE")
270 ttyP("DIRECTION TO GO TO LOOK FOR THE HURKLE.")
280 ttyP("")
285 A=ui_rand(G)
286 B=ui_rand(G)
310 FOR K=1 TO N
320 ttyPn("GUESS #" + " " + STR$(K) + " ")
330 X = ttyAsk("X=", 0, 9)
IF ui_win_closed() <> 0 THEN GOTO 99998
Y = ttyAsk("Y=", 0, 9)
IF ui_win_closed() <> 0 THEN GOTO 99998
340 IF ABS(X-A)+ABS(Y-B)=0 THEN
    GOTO 500
END IF
350 REM PRINT INFO
360 GOSUB 610
370 ttyP("")
380 NEXT K
410 ttyP("")
420 ttyP("SORRY, THAT'S" + " " + STR$(N) + " " + "GUESSES.")
430 ttyP("THE HURKLE IS AT " + " " + STR$(A) + " " + "," + " " + STR$(B) + " ")
440 ttyP("")
450 ttyP("LET'S PLAY AGAIN, HURKLE IS HIDING.")
460 ttyP("")
470 GOTO 285
500 REM
510 ttyP("")
520 ttyP("YOU FOUND HIM IN" + " " + STR$(K) + " " + "GUESSES!")
540 GOTO 440
610 ttyPn("GO ")
620 IF Y=B THEN
    GOTO 670
END IF
630 IF Y<B THEN
    GOTO 660
END IF
640 ttyPn("SOUTH")
650 GOTO 670
660 ttyPn("NORTH")
670 IF X=A THEN
    GOTO 720
END IF
680 IF X<A THEN
    GOTO 710
END IF
690 ttyPn("WEST")
700 GOTO 720
710 ttyPn("EAST")
720 ttyP("")
730 RETURN
999 END

99998 END
