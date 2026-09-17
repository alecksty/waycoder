' 打地鼠 —— 用 **BASIC** 写的手机游戏
'
' 玩法：3×3 的洞里随机冒出一只地鼠，用方向键把光标移到那个洞、回车敲它。
' 敲中 +10 分并换一个洞；敲空 -1 秒。30 秒倒计时，时间到弹框报分。
'
' 与前面几份刻意不同：这份靠 **ui_rand + 光标式输入 + 秒级倒计时**，
' 照的是「随机数 / 离散光标 / 计时器」这条路径，不涉及球类连续位移。
'
' ◆ 手机那套 UI
'
' 开窗 / 绘图 / 输入 / 定时器是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
' 由 `vmltool.config.xml` 的 `<Language Name="basic" Libs="vmlui.vml">` 挂上来。
'
' ◆ 写法要求（每条都是本前端的要求或实测）
'
'   · 外部过程必须 `NATIVE SUB` + **空体** 才是裸标签，否则会被加上 sub_/func_ 前缀、
'     链接期找不到（`CodeGenerator.Sub.cs:1456-1463`）。
'   · 模块级变量必须在赋值前 `DIM`。
'   · 多个条件不写 `AND` —— 用嵌套 IF（本前端对 `AND` 的支持未验证，不走没依据的路）。
'   · 颜色用 `&HFFFF0000` 形式：BASIC 词法器认 `&H`，不认 `0x`。
'   · `PRINT "…"; s` 分号不加分隔符、末尾自动补换行。

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
NATIVE SUB ui_keep_on(v AS INTEGER)
END SUB
NATIVE SUB ui_dlg_msg(title AS STRING, body AS STRING, style AS INTEGER)
END SUB

DIM cx AS INTEGER
DIM cy AS INTEGER
DIM mx AS INTEGER
DIM my AS INTEGER
DIM score AS INTEGER
DIM left AS INTEGER
DIM over AS INTEGER
DIM w AS INTEGER
DIM h AS INTEGER
DIM cell AS INTEGER
DIM ox AS INTEGER
DIM oy AS INTEGER
DIM t AS INTEGER
DIM k AS INTEGER
DIM tid AS INTEGER

w = ui_scr_w()
h = ui_scr_h()
IF w <= 0 THEN w = 360
IF h <= 0 THEN h = 620
ui_win_open "打地鼠", w, h
ui_keep_on 1

cell = (w - 40) / 3
IF cell > 150 THEN cell = 150
ox = (w - cell * 3) / 2
oy = 90

cx = 1
cy = 1
score = 0
left = 30
over = 0
mx = ui_rand(3)
my = ui_rand(3)

tid = ui_timer_set(1000, 0)

WHILE ui_win_closed() = 0
    ' ── draw ──
    ui_clear &HFF10E818
    ui_text 8, 8, "得分", &HFF9AA0B0, 13, 0
    ui_rect 58, 11, score, 10, &HFF5238A8, 1, 0, 0
    ui_text w / 2, 8, "剩余秒", &HFF9AA0B0, 13, 1
    ui_rect w / 2 + 60, 11, left * 6, 10, &HFF00F800, 1, 0, 0

    DIM r AS INTEGER
    DIM c2 AS INTEGER
    r = 0
    WHILE r < 3
        c2 = 0
        WHILE c2 < 3
            DIM hx AS INTEGER
            DIM hy AS INTEGER
            hx = ox + c2 * cell
            hy = oy + r * cell
            ' 洞：深色圆
            ui_circle hx + cell / 2, hy + cell / 2, cell / 2 - 8, &HFF241C10, 1, 0
            ' 地鼠：亮圆（只在它那一格）
            IF r = my THEN
                IF c2 = mx THEN
                    IF over = 0 THEN
                        ui_circle hx + cell / 2, hy + cell / 2, cell / 2 - 20, &HFFFF8A00, 1, 0
                    END IF
                END IF
            END IF
            ' 光标：方框
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
        ui_text w / 2, h - 40, "方向键移动，回车敲", &HFF9AA0B0, 14, 1
    ELSE
        ui_text w / 2, h - 40, "按回车重开", &HFFFFE000, 16, 1
    END IF
    ui_present

    t = ui_wait_msg(0)
    IF t = 10 THEN
        EXIT WHILE
    END IF

    IF t = 9 THEN
        IF over = 0 THEN
            left = left - 1
            IF left <= 0 THEN
                over = 1
                ui_beep 220, 300
                ui_present
                ui_dlg_msg "打地鼠", "时间到！这一局结束。再来一局？（选「否」退出）", 0
                score = 0
                left = 30
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
        IF k = 13 THEN
            IF over = 0 THEN
                IF cx = mx THEN
                    IF cy = my THEN
                        score = score + 10
                        ui_beep 1318, 40
                        mx = ui_rand(3)
                        my = ui_rand(3)
                    ELSE
                        left = left - 1
                        ui_beep 330, 60
                    END IF
                ELSE
                    left = left - 1
                    ui_beep 330, 60
                END IF
            ELSE
                score = 0
                left = 30
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
