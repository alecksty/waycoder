' ══════════════════════════════════════════════════════════════════════════
'  gorilla.bas —— 大猩猩扔香蕉（手机端 VML / BASIC 前端）
'
'  两只大猩猩站在城市两端的楼顶上互扔香蕉。轮流调「角度」与「力度」，
'  香蕉在重力与风里划出抛物线，砸中对面得分，先拿满 3 分的人赢。
'
'  ## 关于玩法来源
'
'  玩法规则（回合制、输角度与力度、重力 + 每回合随机的风、香蕉抛物线、
'  命中计分）是从经典炮击游戏那儿学的 —— 那是**玩法**，不是代码。
'  本文件的程序结构、变量命名、界面几何、手感数值全部重新设计，
'  没有抄原版（GORILLA.BAS，Microsoft 1990，有版权）的任何一行。
'
'  ## 为什么只能走 ui_* 这条路（这条是**验过的**，不是猜的）
'
'  QBasic 原生的图形语句（SCREEN / LINE / CIRCLE / PAINT）在这条链上**上不了屏**：
'  它们的代码生成（`VMLPrepares/BasicCompiler/CodeGenerator.Qbasic.Graphics*.cs`）
'  和共享库（`Lib/shared/src/graphics.c`）走的都是一套 **DOS VGA 帧缓冲模型** ——
'  `GetConfig #1` 拿到 `0xA0000` 当帧缓冲（`VMLRuntime/VMLRuntime.cs:288`）、
'  把像素写进那段内存，而**手机 App 从来不读那一段**：
'     · 在 `WayCoder.Maui/` 下搜 `0xA0000` / `GetConfig` —— 一条命中都没有；
'     · `graphics.c` 里也**没有任何一处**调 `ui_present` 之类把它送到窗口。
'  ⇒ 画了等于没画（连报错都不会有）。能上屏的只有 `ui_*` 那套宿主接口
'  （syscall 500–599），实现在 `Lib/shared/src/vmlui.c` → `Lib/shared/vmlui.vml`，
'  由 `vmltool.config.xml` 的 `<Language Name="basic" Libs="...,vmlui.vml">` 挂上来。
'
'  ## 写这份时实测出来的 BASIC 前端缺陷（每条都有最小复现，改这份别踩回去）
'
'   ① **`SIN` / `COS` / `SQR` 一律返回 0**。反汇编可见调用之后多出一条 `f2i R0 R0`：
'      把返回的**整数**当成浮点位型又转了一次 ⇒ 4 变成 0。底下的库函数本身是对的。
'      `ABS` / `SGN` / `/` / `INT` / `AND` / `OR` 都正常（没有那条 f2i）。
'      最小复现：`PRINT SQR(16)` 打出 0（应为 4）。
'
'      ⚠ 直接 `NATIVE FUNCTION basic_sin(...)` 绕开内建路径**能拿到非零值**，
'      但那个库函数本身也是错的：`Lib/shared/src/basiclib.c` 的 `_sin_lookup`
'      分段线性的常量抄成了**每段上界**的值（`if (deg <= 30) return 5000 + (deg-15)*167`
'      ⇒ 30° 返回 7505，真值 5000；60° 甚至返回 10250 > 10000）。
'      ⇒ 本文件的角度→单位向量改用**开机装表**（见 loadTrig）。
'   ② **SUB 体的表达式是重灾区**（顶层基本是好的）。四条独立的坑，都**不报错**：
'      (a) **`\` 和 `MOD` 编不出代码**。同一个 `g = 100 \ 2`，写在主程序里得 50，
'          写在 `SUB` 里得 **0**；`1000 \ 3` 得 10、`100 MOD 7` 得 10、
'          `46334659 \ 65536` 得 10。看汇编很清楚 —— SUB 体里 `\` 只发出
'              move R1 #100 / move R2 #2 / **move R1 R0**      ← 本该是 div R1 R2
'          顶层才是 `move R0 R11 / div R0 R10`。
'          ⇒ 除法一律写 `INT(a / b)`（`/` 与 `INT` 在 SUB 里是对的），取模一律
'            改成**减法计数循环**。
'      (b) **带括号的复合子表达式算错**。`bw * (NB - 1)` 得 0（`bw * 5` 得 325）；
'          `1 + (2 * 3)` 得 6。⇒ 表达式一律摊平、不套括号，中间量先落到变量上。
'      (c) **CONST 参与算术就出错**。`IF bi > NB - 1 THEN bi = NB - 1` 会**无条件成立**
'          （`bi = 0` 也会把 bi 改成 5）⇒ 所有参与算术的常量先在主程序里落成普通变量
'          （见「CONST 的替身变量」那一段）。`WHILE i < NB` 这种"直接比较"是好的。
'      (d) **大整数**（十万以上）的 `-` / `*` 也不可靠
'          （`900000 - INT(900000/7)*7` 得 650267143）。
'      ⇒ 本文件把中间量全压在 3 万以内：命中判定因此用**方形盒**而不是距离平方，
'        位置全部换成 1/16 像素的整数单位（最大也就六千多）。
'      (e) **`AND` 在 SUB 的条件里是坏的**：`IF a > 0 AND b > 50 THEN` 恒不成立
'          （顶层 `v = 5 AND 3` 是好的）⇒ 条件一律拆成嵌套 IF 或用标志位。
'   ③ **数组不能用**：`DIM a(10)` 会被拆成 10 个互不相干的标量，
'      随后 `a(3) = 42` 编成一次调用 `func_a`，链接期报「未定义的函数 'func_a'」。
'      最小复现：两行 —— `DIM a(10)` / `a(3) = 42`。
'      ⇒ 楼房高度、星星坐标、三角函数表全放共享库的整数网格 `ui_gset`/`ui_gget`
'        （那一对是 `vmlui.c` 里的真数组，不走 syscall，桌面上也是好的）。
'   ④ **字符串拼接是坏的**：`b$ = "x" + "y"` 得到**空串**，不报错。
'      `STR$(n)` 本身是好的 ⇒ 数字上屏走 `n$ = STR$(v)` 再交给 `ui_text`。
'   ⑤ **`DIM x AS STRING` 是坏的**：随后 `x = "..."` 会编成 `func_x`、链接期找不到。
'      要字符串变量就用后缀写法 `x$`，**不要 DIM**。
'   ⑥ **SUB 的形参不能是字符串**：传进去读到的是垃圾（实测打出 `1024`）。
'      ⇒ 本文件所有子过程的形参**全是整数**，字符串一律在子过程里写字面量。
'      （模块级变量在 SUB 里读写是好的 —— tetris.bas 头部「SUB 读不到模块级变量」
'       那段话**已经过期**，实测读写都对。）
'   ⑦ 外部过程必须 `NATIVE SUB/FUNCTION` + **空体**才是裸标签，否则会被加上
'      `sub_`/`func_` 前缀、链接期找不到。
'   ⑧ 形参名不能叫关键字（`on` 之类），会把 NATIVE 声明弄坏。
'   ⑨ 颜色写 `&HFFrrggbb`（词法器认 `&H`，不认 `0x`）。
'   ⑪ **写在 `SUB ... END SUB` 之后的主程序语句会被当成函数调用**：
'      `nbv = NB` 编成 `func_nbv`，链接期报「未定义的函数 'func_nbv'」。
'      最小复现：`SUB t() / END SUB` 之后再写 `q = 1`。
'      ⇒ 主程序里那些**赋值**一律放在所有 SUB 定义**之前**（调用可以放后面，
'        `ui_gclear()` / `physicsCheck()` 那种调用写在末尾是好的 —— 坏的只有赋值）。
'   ⑩ 本文件的「弹道自检」故意做成**开机就跑**：桌面脚手架（vmlcli）把
'      500–599 号全当空操作、也没有真窗口（`ui_win_open_ex` 返回 0），
'      只有把那几行积分结果打出来，才能在桌面上证明弹道是对的 ——
'      光看编译绿灯什么也证明不了。
' ══════════════════════════════════════════════════════════════════════════

' ── 外部库声明（全 NATIVE：裸标签）────────────────────────────────────
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
NATIVE SUB ui_msg_clear()
END SUB
NATIVE FUNCTION ui_msg_count() AS INTEGER
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
NATIVE SUB ui_gclear()
END SUB
NATIVE SUB ui_gset(idx AS INTEGER, v AS INTEGER)
END SUB
NATIVE FUNCTION ui_gget(idx AS INTEGER) AS INTEGER
END FUNCTION

' ── 网格编号表（共享库的整数网格一共 256 格，三段互不重叠）────────────
'   0 .. 5      楼房顶部的 y（NB 个）
'   6 .. 19     星星 x
'   20 .. 33    星星 y
'   100 .. 190  三角函数表：sin(a)*1000，a = 0..90
'               cos 不另存 —— cos(a) = sin(90-a)，一次查表就够
'   208 .. 255  弹坑表：每坑 3 格（x, y, r），16 坑见底
'
' ⚠ 网格一共 **256** 格（`Lib/shared/src/vmlui.c` 的 `UI_GRID_N`），而
'   **200 .. 206 是俄罗斯方块的方块掩码**（那里的 `MASK_AT`，跨语言约定）⇒
'   弹坑表从 208 起、16 坑 ×3 = 48 格到 255 **正好塞满**，一格不越界。
'   要加坑先算一遍 `G_HOLE + MAXHOLE*3 - 1 <= 255`，越界是**静默丢弃**
'   （`ui_gset` 越界不报错），表现是"后面的坑不生效"，很难查。
CONST G_ROOF = 0
CONST G_STARX = 6
CONST G_STARY = 20
CONST G_TRIG = 100
CONST G_HOLE = 208
CONST MAXHOLE = 16        ' 一局最多记 16 个坑（环形复用，满了盖最老的）
CONST HOLE_R = 22         ' 一发炸掉的半径（像素）。楼宽约 63，约 1/3 栋

' ── 手感数值（都在这儿，改手感只动这一段）────────────────────────────
CONST S = 16              ' 子像素刻度：1 像素 = 16 个单位
CONST GRAV = 13           ' 重力（单位 / 步²）
CONST NB = 6              ' 楼房数量
CONST NSTAR = 14          ' 星星数量
CONST WINSCORE = 3        ' 先拿满几分获胜
CONST APE_R = 21          ' 大猩猩命中盒的半边长（像素，方形盒）
CONST STEP_MS = 30        ' 飞行时的定时器间隔
CONST IDLE_MS = 120       ' 瞄准时的定时器间隔（画面基本静止，省电）
CONST PACE_MS = 40        ' 主循环最长睡眠：把重绘锁在 ~25fps，也保证输入延迟 ≤40ms
CONST PANH = 132          ' 底部操作区高度
CONST HUDH = 40           ' 顶部信息带高度

' ── 配色（0xAARRGGBB）────────────────────────────────────────────────
CONST C_SKY = &HFF151228
CONST C_STAR = &HFF8A88B0
CONST C_MOON = &HFFF2E9C8
CONST C_GROUND = &HFF0B0A14
CONST C_ROOF = &HFF3E3E5E
CONST C_WIN_ON = &HFFF0C868
CONST C_WIN_OFF = &HFF171726
CONST C_HUD = &HFF1E1B33
CONST C_HUD_ON = &HFF37415E
CONST C_TEXT = &HFFEDEDF2
CONST C_DIM = &HFF9AA0B0
CONST C_PANEL = &HFF171528
CONST C_TRACK = &HFF2A2A44
CONST C_PIP_OFF = &HFF3A3A52
CONST C_ANGLE = &HFF4ADE80
CONST C_POWER = &HFFFFB020
CONST C_FIRE = &HFFD8443C
CONST C_FIRE_T = &HFFFFF0EC
CONST C_FIRE_B = &HFFB08A88
CONST C_BANANA = &HFFFFE066
CONST C_BOOM1 = &HFFFF6A1E
CONST C_BOOM2 = &HFFFFD24A
CONST C_TRAIL = &HFF6A6690
CONST C_MARKER = &HFF6FD3FF
CONST C_APE0 = &HFFE8A33D
CONST C_APE1 = &HFF6FA8DC

' ── 模块级变量（全部 DIM 在赋值之前）──────────────────────────────────
DIM sw AS INTEGER
DIM sh AS INTEGER
DIM wh AS INTEGER
DIM cxc AS INTEGER
DIM bw AS INTEGER
DIM ground AS INTEGER
DIM panY AS INTEGER
DIM barX AS INTEGER
DIM barW AS INTEGER
DIM barH AS INTEGER
DIM barAy AS INTEGER
DIM barPy AS INTEGER
DIM fireY AS INTEGER
DIM fireH AS INTEGER
DIM topMin AS INTEGER
DIM topMax AS INTEGER
DIM midLo AS INTEGER
DIM midHi AS INTEGER

DIM turn AS INTEGER
DIM sc0 AS INTEGER
DIM sc1 AS INTEGER
DIM wind AS INTEGER
DIM aimA AS INTEGER
DIM aimP AS INTEGER
DIM st AS INTEGER
DIM quit AS INTEGER

DIM bx AS INTEGER
DIM by AS INTEGER
DIM vx AS INTEGER
DIM vy AS INTEGER
DIM ebx AS INTEGER
DIM eby AS INTEGER
DIM boomT AS INTEGER
DIM hitFlag AS INTEGER
DIM ended AS INTEGER
DIM gx AS INTEGER
DIM gy AS INTEGER

DIM g0x AS INTEGER
DIM g0y AS INTEGER
DIM g1x AS INTEGER
DIM g1y AS INTEGER

DIM cosv AS INTEGER
DIM sinv AS INTEGER
DIM spd AS INTEGER
DIM spdN AS INTEGER
DIM rr AS INTEGER

' 弹坑（楼被打掉的那一块，见 clearHoles/addHole）
DIM nHole AS INTEGER
DIM holeNext AS INTEGER
DIM inHole AS INTEGER
DIM hitBld AS INTEGER
DIM hx AS INTEGER
DIM hy AS INTEGER
DIM hr AS INTEGER
DIM hr2 AS INTEGER

DIM pvx AS INTEGER
DIM pvy AS INTEGER
DIM pbx AS INTEGER
DIM pby AS INTEGER

DIM i AS INTEGER
DIM c AS INTEGER
DIM rw AS INTEGER
DIM tx AS INTEGER
DIM ty AS INTEGER
DIM idx AS INTEGER
DIM ddx AS INTEGER
DIM ddy AS INTEGER
DIM bi AS INTEGER
DIM floorY AS INTEGER
DIM inApe AS INTEGER
DIM bxLim AS INTEGER
DIM colr AS INTEGER
DIM wx AS INTEGER
DIM wy AS INTEGER

DIM mt AS INTEGER
DIM n AS INTEGER
DIM tid AS INTEGER
DIM curMs AS INTEGER
DIM wantMs AS INTEGER
DIM dlg AS INTEGER
DIM tA AS INTEGER
DIM tP AS INTEGER
DIM tvx AS INTEGER
DIM tvy AS INTEGER
DIM rng AS INTEGER
DIM ptx AS INTEGER
DIM pty AS INTEGER
DIM simMode AS INTEGER

' 下面这几个是 CONST 的**替身变量**，赋值见紧随其后的那一段（必须在 SUB 之前）。
DIM nbv AS INTEGER
DIM nb1 AS INTEGER
DIM apeR AS INTEGER
DIM wscore AS INTEGER
DIM nstar AS INTEGER
DIM hudh AS INTEGER
DIM panh AS INTEGER
DIM groof AS INTEGER
DIM gstarx AS INTEGER
DIM gstary AS INTEGER
DIM gtrig AS INTEGER
DIM ghole AS INTEGER
DIM maxHole AS INTEGER
DIM holeR AS INTEGER
DIM stepMs AS INTEGER
DIM idleMs AS INTEGER
DIM paceMs AS INTEGER

' ── CONST 的替身变量（SUB 里只用这些普通变量）──────────────────────────
' 缺陷 ② 的第四种形态：CONST 一旦在 SUB 体里**参与算术**就会出错 ——
'   `IF bi > NB - 1 THEN bi = NB - 1` 会**无条件成立**（最小复现见文件头），
'   而 `WHILE i < NB`（CONST 直接当比较对象，没有算术）是好的。
' 所以凡是要算的常量，都在这里先落成普通变量。
'
' ⚠ 这个赋值块**必须放在所有 SUB 定义之前** —— 缺陷 ⑪：写在 SUB 之后的主程序语句
'   会被当成函数调用（`nbv = NB` 编成 `func_nbv`，链接期报「未定义的函数」）。
nbv = NB
nb1 = NB - 1
apeR = APE_R
wscore = WINSCORE
nstar = NSTAR
panh = PANH
hudh = HUDH
groof = G_ROOF
gstarx = G_STARX
gstary = G_STARY
gtrig = G_TRIG
stepMs = STEP_MS
idleMs = IDLE_MS
paceMs = PACE_MS
ghole = G_HOLE
maxHole = MAXHOLE
holeR = HOLE_R

' 城市的地形带（都要先算成普通变量，理由同上）
'   · 楼房：楼顶 y 落在 [topMin, topMax)
'   · 大猩猩站的两栋：另外收在一个中间带上，免得猿被顶到信息带里、
'     或者矮到让对射变成一条直线
topMin = hudh + 150
midLo = hudh + 205
midHi = midLo + 70

' ══════════════════════════════════════════════════════════════════════════
'  子过程
'
'  ⚠ 写在 SUB 里的除法一律用 `INT(a / b)`、取模一律用减法计数 ——
'    见文件头缺陷 ②，`\` 和 `MOD` 在 SUB 体里会静默给出垃圾值。
' ══════════════════════════════════════════════════════════════════════════

' 正弦表：sin(a°) × 1000，a = 0..90，存在 ui_gget(G_TRIG + a)。
' 装一次管一整局（是常数表）。值按 sin 精确算出来写死，
' **不是**运行时算的 —— 前端的 SIN 是坏的，库里的 basic_sin 也不准（缺陷 ①）。
SUB loadTrig()
    ui_gset(100, 0)
    ui_gset(101, 17)
    ui_gset(102, 35)
    ui_gset(103, 52)
    ui_gset(104, 70)
    ui_gset(105, 87)
    ui_gset(106, 105)
    ui_gset(107, 122)
    ui_gset(108, 139)
    ui_gset(109, 156)
    ui_gset(110, 174)
    ui_gset(111, 191)
    ui_gset(112, 208)
    ui_gset(113, 225)
    ui_gset(114, 242)
    ui_gset(115, 259)
    ui_gset(116, 276)
    ui_gset(117, 292)
    ui_gset(118, 309)
    ui_gset(119, 326)
    ui_gset(120, 342)
    ui_gset(121, 358)
    ui_gset(122, 375)
    ui_gset(123, 391)
    ui_gset(124, 407)
    ui_gset(125, 423)
    ui_gset(126, 438)
    ui_gset(127, 454)
    ui_gset(128, 469)
    ui_gset(129, 485)
    ui_gset(130, 500)
    ui_gset(131, 515)
    ui_gset(132, 530)
    ui_gset(133, 545)
    ui_gset(134, 559)
    ui_gset(135, 574)
    ui_gset(136, 588)
    ui_gset(137, 602)
    ui_gset(138, 616)
    ui_gset(139, 629)
    ui_gset(140, 643)
    ui_gset(141, 656)
    ui_gset(142, 669)
    ui_gset(143, 682)
    ui_gset(144, 695)
    ui_gset(145, 707)
    ui_gset(146, 719)
    ui_gset(147, 731)
    ui_gset(148, 743)
    ui_gset(149, 755)
    ui_gset(150, 766)
    ui_gset(151, 777)
    ui_gset(152, 788)
    ui_gset(153, 799)
    ui_gset(154, 809)
    ui_gset(155, 819)
    ui_gset(156, 829)
    ui_gset(157, 839)
    ui_gset(158, 848)
    ui_gset(159, 857)
    ui_gset(160, 866)
    ui_gset(161, 875)
    ui_gset(162, 883)
    ui_gset(163, 891)
    ui_gset(164, 899)
    ui_gset(165, 906)
    ui_gset(166, 914)
    ui_gset(167, 921)
    ui_gset(168, 927)
    ui_gset(169, 934)
    ui_gset(170, 940)
    ui_gset(171, 946)
    ui_gset(172, 951)
    ui_gset(173, 956)
    ui_gset(174, 961)
    ui_gset(175, 966)
    ui_gset(176, 970)
    ui_gset(177, 974)
    ui_gset(178, 978)
    ui_gset(179, 982)
    ui_gset(180, 985)
    ui_gset(181, 988)
    ui_gset(182, 990)
    ui_gset(183, 993)
    ui_gset(184, 995)
    ui_gset(185, 996)
    ui_gset(186, 998)
    ui_gset(187, 999)
    ui_gset(188, 999)
    ui_gset(189, 1000)
    ui_gset(190, 1000)
END SUB

' 当前角度 → 单位向量（×1000）。cos 用 sin 的对称性拿：cos a = sin(90-a)。
SUB aimAngles()
    sinv = ui_gget(gtrig + aimA)
    cosv = ui_gget(gtrig + 90 - aimA)
END SUB

' 力度 → 初速（单位 / 步）。
'
' 这里是**让射程与力度成正比**：射程 ∝ 速度²，所以速度取 √力度。
' 直接用「速度 ∝ 力度」的话，力度条上 40 以下几乎全打不到人、大半根条是废的
' —— 这是照着射程算过之后改的，不是随手写的。
'
' 开方用整数牛顿迭代（从 100 起，12 次足够收敛到 n ≤ 10000 的整数根）。
SUB aimSpeed()
    spdN = aimP * 100
    IF spdN <= 0 THEN
        spd = 0
    ELSE
        rr = 100
        i = 0
        WHILE i < 12
            IF rr < 1 THEN
                rr = 1
            END IF
            ' 拆成两句、不加括号：SUB 里带括号的子表达式会被算错（缺陷 ②）
            idx = INT(spdN / rr)
            idx = rr + idx
            rr = INT(idx / 2)
            i = i + 1
        WEND
        spd = rr * 3
    END IF
END SUB

' 纯算术：算一条无风弹道回到出发高度的水平距离（像素），结果放 rng。
' 与实弹**同一套积分**，所以它是弹道的回归判据。
SUB shotRange(a0 AS INTEGER, p0 AS INTEGER)
    tA = aimA
    tP = aimP
    aimA = a0
    aimP = p0
    aimAngles()
    aimSpeed()
    tvx = INT(spd * cosv / 1000)
    tvy = 0 - INT(spd * sinv / 1000)
    pbx = 0
    pby = 0
    i = 0
    ended = 0
    WHILE ended = 0
        pbx = pbx + tvx
        pby = pby + tvy
        tvy = tvy + GRAV
        i = i + 1
        IF pby >= 0 THEN
            ended = 1
        END IF
        IF i > 4000 THEN
            ended = 1
        END IF
    WEND
    rng = INT(pbx / S)
    aimA = tA
    aimP = tP
    aimAngles()
    aimSpeed()
END SUB

' 开机自检：几条典型弹道的射程。射程与力度成正比 ⇒ 100/50/25 应约为 4 : 2 : 1。
SUB physicsCheck()
    PRINT "── 大猩猩扔香蕉 · 开机自检（与实弹同一套积分）──"
    aimA = 45
    aimAngles()
    PRINT "  查表 角度45 -> cos/sin（应 707/707）:"; cosv; sinv
    aimA = 30
    aimAngles()
    PRINT "  查表 角度30 -> cos/sin（应 866/500）:"; cosv; sinv
    shotRange(45, 100)
    PRINT "  45度 力度100 -> 射程(px)"; rng
    shotRange(45, 50)
    PRINT "  45度 力度050 -> 射程(px)"; rng
    shotRange(45, 25)
    PRINT "  45度 力度025 -> 射程(px)"; rng
    shotRange(90, 100)
    PRINT "  90度 力度100 -> 射程(px)"; rng
    PRINT "  （射程应近似与力度成正比：100/50/25 约 4:2:1；90 度应约 0）"
END SUB

' 打一发「实弹」但不画屏：把香蕉架好、一路 stepFlight 到结束，结果留在
' hitFlag / ebx / eby / st 里。碰撞规则的自检就靠它。
SUB simShot(a0 AS INTEGER, p0 AS INTEGER)
    aimA = a0
    aimP = p0
    st = 1
    boomT = 0
    armBanana()
    i = 0
    WHILE st = 1
        stepFlight()
        i = i + 1
        ' 一发飞不出这么多步；卡住就判自检失败，免得死循环把整机拖住
        IF i > 900 THEN
            st = 9
        END IF
    WEND
END SUB

' 碰撞规则自检：一座**固定的**城市 + 几发已知的弹。
' 只用共享库的网格和纯算术，所以桌面脚手架（没有真窗口）也跑得起来 ——
' 「弹道自检」只证明了积分，「这一发算不算命中」得靠这一段。
SUB simCheck()
    PRINT "── 碰撞规则自检（固定城市：中间略高，两头 90 高）──"
    sw = 390
    sh = 660
    bw = INT(sw / nbv)
    ground = sh - panh - 22
    topMin = hudh + 150
    topMax = ground - 90
    ui_gset(0, ground - 90)
    ui_gset(1, ground - 106)
    ui_gset(2, ground - 146)
    ui_gset(3, ground - 146)
    ui_gset(4, ground - 106)
    ui_gset(5, ground - 90)
    rw = nb1
    g0x = INT(bw / 2)
    g0y = ui_gget(0)
    g1x = bw * rw
    g1x = g1x + INT(bw / 2)
    g1y = ui_gget(5)
    turn = 0
    wind = 0
    simMode = 1
    PRINT "  两猿 x/楼顶 y:"; g0x; g0y
    PRINT "  两猿 x/楼顶 y:"; g1x; g1y
    PRINT "  画布"; sw; sh
    PRINT "  地面 y（发射点在此之上 44）:"; ground

    ' ① 角度 0：平着扔出去，会撞上中间那栋高楼
    simShot(0, 100)
    PRINT "  ① 0度 力度100 -> 命中猿?"; hitFlag
    PRINT "     落点 x/y:"; ebx; eby

    ' ② 角度 90：垂直向上，原路落回自己站的那栋楼
    simShot(90, 100)
    PRINT "  ② 90度 力度100 -> 命中猿?"; hitFlag
    PRINT "     落点 x/y:"; ebx; eby

    ' ③ 角度 45、力度全开：飞过头，出界脱靶
    simShot(45, 100)
    PRINT "  ③ 45度 力度100 -> 命中猿?"; hitFlag
    PRINT "     落点 x/y:"; ebx; eby

    ' ④ 角度 45、力度 68：射程约 306，弹道下坠时正好穿过对面那只
    '    （两猿相距 325、命中盒 y 在 378~420）—— 这一发必须命中
    simShot(45, 68)
    PRINT "  ④ 45度 力度068 -> 命中猿?"; hitFlag
    PRINT "     落点 x/y:"; ebx; eby
    ' ⚠ `hitBld` 每发都会在 stepFlight 开头清零 ⇒ **必须当场打印**，
    '   攒到后面再打拿到的是最后一发的值（这里第一版就写错过一次）。
    PRINT "     打猿不留坑 -> hitBld(应 0):"; hitBld

    ' ⑤ 角度 45、力度三成：射程约 135，落在城里某栋楼上
    '    ⚠ 先清空弹坑：前面 ①② 也都打在楼上、各留了一个坑，
    '      不清的话下面 `ui_gget(ghole)` 读到的是**最早**那个坑，判据对不上落点。
    clearHoles()
    simShot(45, 30)
    PRINT "  ⑤ 45度 力度030 -> 命中猿?"; hitFlag
    PRINT "     落点 x/y:"; ebx; eby

    ' ⑥ 弹坑判据：这一段钉的就是用户报的那件事「炸了建筑，炸完又还原了」——
    '    从前爆炸只是一层特效、楼体数据一个字节没动，所以缺口下一帧就被重画抹平。
    PRINT "  ⑥ 打楼留坑 -> hitBld(应 1):"; hitBld
    PRINT "     坑数 nHole(应 1):"; nHole
    PRINT "     坑 x/y/r(应 = ⑤ 的落点, 22):"
    PRINT "       "; ui_gget(ghole); ui_gget(ghole + 1); ui_gget(ghole + 2)

    ' ⑦ 同一发**再打一遍**：这一次要从刚才那个缺口里穿过去 ⇒ 落点必然更低。
    '    这是"楼被打穿"的判据 —— 穿不过去的话新落点会与 ⑤ 逐像素相同。
    pby = eby
    simShot(45, 30)
    PRINT "  ⑦ 再打一发 -> 新落点 y(应 > ⑤ 的 y):"; eby
    PRINT "     ⑤ 的 y:"; pby
    PRINT "     坑数 nHole(应 2，穿过去之后又炸了一层):"; nHole

    ' ⑧ 换局要清空：城市都重排了，旧坑的位置毫无意义
    '    （不清的话上一局的洞会以天空色的圆出现在新楼上，像贴了几块补丁）
    clearHoles()
    PRINT "  ⑧ 换局后 nHole(应 0):"; nHole

    simMode = 0
END SUB

' ── 弹坑：楼被炸掉的那一块 ─────────────────────────────────────────────
'
' 为什么必须**记成状态**、而不是"爆炸时画一下"：`drawScene` 每帧先 `ui_clear`
' 再把每栋楼**整栋**重画（见那里的循环），爆炸要是只画在楼上面，下一帧就被
' 盖回原样 —— 用户看到的正是「猴子炸了建筑，炸完又还原了」。
' 缺口得跟着这一局留住，所以存进网格，由 `drawBuilding` 之后的那一趟统一涂回去。
'
' 存法沿用全仓"没有可靠数组"的惯例（文件头缺陷 ③）：`ui_gget/ui_gset` 的整数网格，
' 每坑 3 格 x/y/r。**环形复用**（满了从最老的开始盖）—— 这一点是刻意的：
' 宁可让老坑消失，也不能让"满了之后的新伤害不生效"（那会变成"打不动了"）。
SUB clearHoles()
    nHole = 0
    holeNext = 0
END SUB

SUB addHole(hx0 AS INTEGER, hy0 AS INTEGER, hr0 AS INTEGER)
    hi = holeNext * 3
    hi = ghole + hi
    ui_gset(hi, hx0)
    ui_gset(hi + 1, hy0)
    ui_gset(hi + 2, hr0)
    holeNext = holeNext + 1
    IF holeNext >= maxHole THEN
        holeNext = 0
    END IF
    IF nHole < maxHole THEN
        nHole = nHole + 1
    END IF
END SUB

' 新一局：重排城市、把大猩猩放到两头的楼顶、撒星星、把香蕉放回手上
SUB newCity()
    ' 中间那几栋是"掩体"，高度随机；两头（大猩猩站的）收在中间带里
    i = 0
    WHILE i < nbv
        idx = topMin + ui_rand(topMax - topMin)
        IF i = 0 THEN
            idx = midLo + ui_rand(midHi - midLo)
        END IF
        IF i = nb1 THEN
            idx = midLo + ui_rand(midHi - midLo)
        END IF
        ui_gset(groof + i, idx)
        i = i + 1
    WEND
    rw = nb1
    g0x = INT(bw / 2)
    g0y = ui_gget(groof)
    g1x = bw * rw
    g1x = g1x + INT(bw / 2)
    g1y = ui_gget(groof + rw)

    i = 0
    WHILE i < nstar
        ui_gset(gstarx + i, ui_rand(sw))
        idx = topMin - hudh - 12
        ui_gset(gstary + i, hudh + 6 + ui_rand(idx))
        i = i + 1
    WEND

    boomT = 0
    hitFlag = 0
    ended = 0
    ' 城市重排了 ⇒ 旧弹坑的位置全无意义，必须清掉
    ' （不清的话上一局的洞会以天空色的圆出现在新楼上，像贴了几块补丁）
    clearHoles()
    st = 0
    armBanana()
END SUB

' 把香蕉放回当前玩家的手上（大猩猩头顶再高一点，免得一起手就砸自家楼顶）
SUB armBanana()
    aimAngles()
    aimSpeed()
    IF turn = 0 THEN
        bx = g0x * S
        ty = g0y - 44
        by = ty * S
        vx = INT(spd * cosv / 1000)
    ELSE
        bx = g1x * S
        ty = g1y - 44
        by = ty * S
        vx = 0 - INT(spd * cosv / 1000)
    END IF
    vy = 0 - INT(spd * sinv / 1000)
END SUB

' 一帧飞行：走一步、判碰撞，命中/落地就切到爆炸状态
'
' ⚠ 这一段的写法是**刻意的**，别"顺手优化"回去（缺陷 ②的第三种形态）：
'   · 每个变量只干一件事 —— 早先 `idx` 先当"出界阈值"（= 6624）又当"楼号"，
'     结果楼号那句被排到了钳位之后，`idx` 永远是钳出来的 5，
'     所有楼都被当成最后一栋、香蕉一起手就炸（实测落点 (50,372)，第一步就结束）。
'   · 判断一律**摊平**，不套 IF、不写 AND —— `AND` 在 SUB 的条件里是坏的。
SUB stepFlight()
    bx = bx + vx
    by = by + vy
    vy = vy + GRAV
    vx = vx + wind

    tx = INT(bx / S)
    ty = INT(by / S)
    ended = 0
    hitFlag = 0
    hitBld = 0

    ' ① 左右出界（各留 24 像素余量，飞出去就判脱靶）
    IF bx < -384 THEN
        ended = 1
    END IF
    bxLim = sw + 24
    bxLim = bxLim * S
    IF bx > bxLim THEN
        ended = 1
    END IF

    ' ② 目标大猩猩。用**方形盒**而不是距离平方：SUB 里大整数乘法不可靠，
    '    而且盒子本来就更贴那只方头方身的大猩猩。
    '    先判猿、后判楼 —— 大猩猩站在楼顶上，顺序反了的话
    '    「擦着对面头皮过去」会被算成打在墙上。
    gx = g0x
    gy = g0y - 17
    IF turn = 0 THEN
        gx = g1x
        gy = g1y - 17
    END IF
    ddx = tx - gx
    IF ddx < 0 THEN
        ddx = 0 - ddx
    END IF
    ddy = ty - gy
    IF ddy < 0 THEN
        ddy = 0 - ddy
    END IF
    inApe = 0
    IF ddx <= apeR THEN
        IF ddy <= apeR THEN
            inApe = 1
        END IF
    END IF
    IF inApe = 1 THEN
        hitFlag = 1
    END IF
    IF hitFlag = 1 THEN
        ended = 1
    END IF

    ' ③ 撞地：这一列的地面高度 = 城里那栋楼的顶，出了城就是地平线。
    '    摊成一条直线：先算楼号（城外的记 -1）、再钳、再取高度。
    bi = INT(tx / bw)
    IF tx < 0 THEN
        bi = -1
    END IF
    IF tx >= sw THEN
        bi = -1
    END IF
    IF bi > nb1 THEN
        bi = nb1
    END IF
    floorY = ground
    IF bi >= 0 THEN
        floorY = ui_gget(groof + bi)
    END IF
    IF ty >= floorY THEN
        ' 已经降到自己那一列的楼顶以下了 —— 但若正落在**之前炸出来的缺口**里，
        ' 那儿是空的，香蕉该穿过去继续飞（这就是"打了几个洞之后能打穿楼"那件事）。
        inHole = 0
        i = 0
        WHILE i < nHole
            hi = i * 3
            hi = ghole + hi
            hx = ui_gget(hi)
            hy = ui_gget(hi + 1)
            hr = ui_gget(hi + 2)
            ' 方形盒判定，与上面猿命中盒同一个理由（SUB 里大整数乘法不可靠）。
            ' ⚠ 盒子取圆的**内接**正方形（半径 × 7/10 ≈ 0.707），不是外接 ——
            '   取外接的话香蕉能从缺口的**四个角**穿过去，那看起来就是穿墙；
            '   取内接最多是"炸点比看到的洞口低一点点"，方向是对的。
            hr2 = INT(hr * 7 / 10)
            ddx = tx - hx
            IF ddx < 0 THEN
                ddx = 0 - ddx
            END IF
            ddy = ty - hy
            IF ddy < 0 THEN
                ddy = 0 - ddy
            END IF
            IF ddx <= hr2 THEN
                IF ddy <= hr2 THEN
                    inHole = 1
                END IF
            END IF
            i = i + 1
        WEND
        IF inHole = 0 THEN
            ended = 1
            IF bi >= 0 THEN
                hitBld = 1
            END IF
        END IF
    END IF

    IF ended = 1 THEN
        ebx = tx
        eby = ty
        ' 打在楼上 ⇒ 炸出一个缺口（记成状态，见 clearHoles 那段说明）。
        ' 打在猿身上 / 落在地平线上不算 —— 那些地方本来就没有楼。
        IF hitBld = 1 THEN
            addHole(tx, ty, holeR)
        END IF
        ' simMode = 1 时不发声 —— 开机自检会在真机上连放几炮，
        ' 那几声不该让用户在见到画面之前先听一串蜂鸣。
        IF simMode = 0 THEN
            IF hitFlag = 1 THEN
                ui_beep(1046, 90)
                ui_vibrate(45, 120)
            ELSE
                ui_beep(280, 60)
                ui_vibrate(20, 60)
            END IF
        END IF
        st = 2
        boomT = 0
    END IF
END SUB

' 一发打完：计分、换人、够分就终局
SUB resolveShot()
    IF hitFlag = 1 THEN
        IF turn = 0 THEN
            sc0 = sc0 + 1
        ELSE
            sc1 = sc1 + 1
        END IF
    END IF
    IF sc0 >= wscore THEN
        st = 3
    ELSE
        IF sc1 >= wscore THEN
            st = 3
        ELSE
            turn = 1 - turn
            ' 风按「回合」换：轮到玩家一才重掷，这样同一回合两人面对同一阵风
            IF turn = 0 THEN
                wind = ui_rand(5) - 2
            END IF
            st = 0
            armBanana()
        END IF
    END IF
END SUB

' 一栋楼（含窗户）。窗户的明暗按 (楼号, 列, 行) 算出来的确定性图案 ——
' 不能用随机数：drawScene 每帧都跑，随机会变成满屏闪烁。
' 相位用减法计数拿，不用 MOD（缺陷 ②）。
SUB drawBuilding(bi AS INTEGER)
    tx = bi * bw
    ty = ui_gget(groof + bi)

    ' 楼体颜色：bi 对 3 取模，用减法循环
    rw = bi
    WHILE rw >= 3
        rw = rw - 3
    WEND
    colr = &HFF23233A
    IF rw = 1 THEN
        colr = &HFF2A2A44
    END IF
    IF rw = 2 THEN
        colr = &HFF1C1C30
    END IF
    ui_rect(tx, ty, bw, ground - ty, colr, 1, 0, 0)
    ui_rect(tx, ty, bw, 3, C_ROOF, 1, 0, 0)

    ' 窗户列间距先算好（不在表达式里套括号，见缺陷 ②）
    rw = bw - 22
    rw = INT(rw / 2)
    c = 0
    WHILE c < 2
        wx = tx + 9 + c * rw
        wy = ty + 12
        ' 每列起手的相位不同，整排窗户才会有点亮暗错落
        idx = bi * 2 + c
        WHILE idx >= 5
            idx = idx - 5
        WEND
        WHILE wy < ground - 12
            IF idx < 2 THEN
                ui_rect(wx, wy, 6, 8, C_WIN_ON, 1, 0, 0)
            ELSE
                ui_rect(wx, wy, 6, 8, C_WIN_OFF, 1, 0, 0)
            END IF
            idx = idx + 1
            IF idx >= 5 THEN
                idx = 0
            END IF
            wy = wy + 18
        WEND
        c = c + 1
    WEND
END SUB

' 一只大猩猩：(ax, ay) 是它站的那块楼顶（脚底），who 只用来选颜色/朝向。
' 全部用矩形和圆拼，不依赖任何图片资源。
SUB drawApe(ax AS INTEGER, ay AS INTEGER, who AS INTEGER)
    colr = C_APE0
    IF who = 1 THEN
        colr = C_APE1
    END IF
    ' 腿
    ui_rect(ax - 8, ay - 8, 6, 8, colr, 1, 0, 2)
    ui_rect(ax + 2, ay - 8, 6, 8, colr, 1, 0, 2)
    ' 身体
    ui_rect(ax - 9, ay - 23, 18, 16, colr, 1, 0, 4)
    ' 手臂
    ui_rect(ax - 14, ay - 21, 5, 14, colr, 1, 0, 2)
    ui_rect(ax + 9, ay - 21, 5, 14, colr, 1, 0, 2)
    ' 头
    ui_circle(ax, ay - 28, 7, colr, 1, 0)
    ' 两只眼睛（朝向对面：玩家一往右看，玩家二往左看）
    IF who = 0 THEN
        ui_circle(ax + 1, ay - 29, 2, C_TEXT, 1, 0)
        ui_circle(ax + 5, ay - 29, 2, C_TEXT, 1, 0)
    ELSE
        ui_circle(ax - 5, ay - 29, 2, C_TEXT, 1, 0)
        ui_circle(ax - 1, ay - 29, 2, C_TEXT, 1, 0)
    END IF
END SUB

' 把整数画到屏幕上（字符串拼接是坏的，只能用 STR$ 整份赋值 —— 缺陷 ④）
SUB drawNum(x AS INTEGER, y AS INTEGER, v AS INTEGER, col AS INTEGER, sz AS INTEGER, ac AS INTEGER)
    n$ = STR$(v)
    ui_text(x, y, n$, col, sz, ac)
END SUB

' 瞄准时的预览虚线：只画前十几步，给个方向感，不把落点泄露出去
SUB drawAim()
    pvx = INT(spd * cosv / 1000)
    pvy = 0 - INT(spd * sinv / 1000)
    IF turn = 1 THEN
        pvx = 0 - pvx
    END IF
    pbx = bx
    pby = by
    i = 0
    idx = 0
    WHILE i < 15
        pbx = pbx + pvx
        pby = pby + pvy
        pvy = pvy + GRAV
        pvx = pvx + wind
        idx = idx + 1
        IF idx >= 3 THEN
            idx = 0
            IF INT(pby / S) < ground THEN
                ui_circle(INT(pbx / S), INT(pby / S), 3, C_TRAIL, 1, 0)
            END IF
        END IF
        i = i + 1
    WEND
END SUB

' 整屏重画
SUB drawScene()
    cxc = INT(sw / 2)

    ' ── 天空、星星、月亮 ──
    ui_clear(C_SKY)
    i = 0
    WHILE i < nstar
        ui_rect(ui_gget(gstarx + i), ui_gget(gstary + i), 2, 2, C_STAR, 1, 0, 0)
        i = i + 1
    WEND
    ' 月亮：一个亮圆 + 一个「咬掉」的天空色圆（天空是纯色，所以直接盖）
    ui_circle(sw - 54, hudh + 48, 20, C_MOON, 1, 0)
    ui_circle(sw - 45, hudh + 41, 18, C_SKY, 1, 0)

    ' ── 城市 ──
    i = 0
    WHILE i < nbv
        drawBuilding(i)
        i = i + 1
    WEND

    ' ── 弹坑：把炸掉的那块涂回天空色 ──
    ' 位置很讲究：**必须在画完所有楼之后**（画在楼前面会被下一栋楼盖住，
    ' 而缺口本来就可能横跨两栋的交界），**必须在地面之前**（否则会把地面啃掉一块）。
    i = 0
    WHILE i < nHole
        hi = i * 3
        hi = ghole + hi
        hx = ui_gget(hi)
        hy = ui_gget(hi + 1)
        hr = ui_gget(hi + 2)
        ui_circle(hx, hy, hr, C_SKY, 1, 0)
        i = i + 1
    WEND

    ui_rect(0, ground, sw, sh - ground, C_GROUND, 1, 0, 0)

    ' ── 两只大猩猩 ──
    drawApe(g0x, g0y, 0)
    drawApe(g1x, g1y, 1)

    ' 轮到谁：在它头顶画个朝下的小箭头
    ' （用两条线拼 —— 多边形要 int 数组，而数组不能用，见缺陷 ③）
    IF turn = 0 THEN
        tx = g0x
        ty = g0y - 52
    ELSE
        tx = g1x
        ty = g1y - 52
    END IF
    ui_line(tx - 9, ty - 8, tx, ty, C_MARKER, 3)
    ui_line(tx, ty, tx + 9, ty - 8, C_MARKER, 3)

    ' ── 香蕉 / 尾迹 / 预览 / 爆炸 ──
    IF st = 0 THEN
        ' 先按**当前**的角度/力度重算单位向量与初速再画预览 —— 少了这一步，
        ' 玩家拖条的时候预览还是上一发的方向（只在发射那一刻才更新），
        ' 看起来就是"改了没反应"。
        aimAngles()
        aimSpeed()
        drawAim()
        ui_circle(INT(bx / S), INT(by / S), 4, C_BANANA, 1, 0)
    END IF
    IF st = 1 THEN
        ui_circle(INT(bx / S), INT(by / S), 4, C_BANANA, 1, 0)
        wx = INT(bx / S) - INT(vx / S)
        wy = INT(by / S) - INT(vy / S)
        ui_circle(wx, wy, 2, C_TRAIL, 1, 0)
    END IF
    IF st = 2 THEN
        rr = 4 + boomT * 3
        ui_circle(ebx, eby, rr + 7, C_BOOM1, 1, 0)
        ui_circle(ebx, eby, rr, C_BOOM2, 1, 0)
    END IF

    ' ── 顶部信息带 ──
    ui_rect(0, 0, sw, hudh, C_HUD, 1, 0, 0)
    IF turn = 0 THEN
        ui_rect(6, 5, 100, 30, C_HUD_ON, 1, 0, 8)
    ELSE
        ui_rect(sw - 106, 5, 100, 30, C_HUD_ON, 1, 0, 8)
    END IF

    ui_text(12, 11, "玩家一", C_TEXT, 14, 0)
    i = 0
    WHILE i < wscore
        colr = C_PIP_OFF
        IF i < sc0 THEN
            colr = C_ANGLE
        END IF
        ui_circle(66 + i * 16, 20, 6, colr, 1, 0)
        i = i + 1
    WEND

    ui_text(sw - 12, 11, "玩家二", C_TEXT, 14, 2)
    i = 0
    WHILE i < wscore
        colr = C_PIP_OFF
        IF i < sc1 THEN
            colr = C_APE1
        END IF
        ui_circle(sw - 66 - i * 16, 20, 6, colr, 1, 0)
        i = i + 1
    WEND

    ' 风：一根轨道 + 一个会左右跑的小方块（+2 在最右、-2 在最左）
    ui_text(cxc, 5, "风", C_DIM, 13, 1)
    ui_rect(cxc - 46, 27, 92, 8, C_TRACK, 1, 0, 4)
    ui_rect(cxc - 1, 25, 3, 12, &HFF6A6A8C, 1, 0, 0)
    ui_rect(cxc + wind * 17 - 5, 23, 10, 16, C_MARKER, 1, 0, 3)

    ' ── 底部操作区 ──
    ui_rect(0, panY, sw, panh, C_PANEL, 1, 0, 0)
    ui_rect(0, panY, sw, 2, &HFF2E2B45, 1, 0, 0)

    ui_text(14, barAy + 8, "角度", C_DIM, 14, 0)
    ui_rect(barX, barAy, barW, barH, C_TRACK, 1, 0, 6)
    ui_rect(barX, barAy, INT(barW * aimA / 90), barH, C_ANGLE, 1, 0, 6)
    drawNum(sw - 14, barAy + 6, aimA, C_TEXT, 16, 2)

    ui_text(14, barPy + 8, "力度", C_DIM, 14, 0)
    ui_rect(barX, barPy, barW, barH, C_TRACK, 1, 0, 6)
    ui_rect(barX, barPy, INT(barW * aimP / 100), barH, C_POWER, 1, 0, 6)
    drawNum(sw - 14, barPy + 6, aimP, C_TEXT, 16, 2)

    ui_rect(14, fireY, sw - 28, fireH, C_FIRE, 1, 0, 8)
    IF st = 0 THEN
        ui_text(cxc, fireY + 7, "发 射", C_FIRE_T, 18, 1)
    ELSE
        ui_text(cxc, fireY + 7, "飞 行 中", C_FIRE_B, 18, 1)
    END IF

    ui_present()
END SUB

' 触摸 / 鼠标：按下的那一点落在哪根条上就改哪个值；发射键只在按下那一刻认
SUB handlePoint(isDown AS INTEGER)
    ptx = ui_msg_a()
    pty = ui_msg_b()
    IF pty >= barAy THEN
        IF pty < barAy + barH THEN
            IF ptx >= barX - 14 THEN
                tx = ptx - barX
                tx = tx * 90
                aimA = INT(tx / barW)
                IF aimA < 0 THEN
                    aimA = 0
                END IF
                IF aimA > 90 THEN
                    aimA = 90
                END IF
            END IF
        END IF
    END IF
    IF pty >= barPy THEN
        IF pty < barPy + barH THEN
            IF ptx >= barX - 14 THEN
                tx = ptx - barX
                tx = tx * 100
                aimP = INT(tx / barW)
                IF aimP < 0 THEN
                    aimP = 0
                END IF
                IF aimP > 100 THEN
                    aimP = 100
                END IF
            END IF
        END IF
    END IF
    ' 发射键只在**按下**那一刻认，拖动路过不算（免得调条的时候误射）
    IF isDown = 1 THEN
        IF st = 0 THEN
            IF pty >= fireY THEN
                IF pty <= fireY + fireH THEN
                    IF ptx >= 14 THEN
                        IF ptx <= sw - 14 THEN
                            armBanana()
                            st = 1
                            ui_beep(660, 40)
                        END IF
                    END IF
                END IF
            END IF
        END IF
    END IF
END SUB

' 键盘：Esc / 返回退出；回车或手柄 A 也能发射（接了物理键盘/手柄也能玩）
SUB handleKey()
    i = ui_msg_a()
    IF i = 27 THEN
        quit = 1
    END IF
    IF i = 13 THEN
        IF st = 0 THEN
            armBanana()
            st = 1
            ui_beep(660, 40)
        END IF
    END IF
    IF i = 65 THEN
        IF st = 0 THEN
            armBanana()
            st = 1
            ui_beep(660, 40)
        END IF
    END IF
END SUB

' ── 主循环 ─────────────────────────────────────────────────────────────
SUB runGame()
    ui_keep_on(1)

    bw = INT(sw / nbv)
    ground = sh - panh - 22
    panY = sh - panh
    topMax = ground - 90
    IF topMax < topMin + 30 THEN
        topMax = topMin + 30
    END IF
    ' 屏幕特别矮时（极少见）把大猩猩那一档往上收，
    ' 免得楼顶落到地面以下 —— 那样 `ground - ty` 会变负数
    IF midHi > ground - 60 THEN
        midHi = ground - 60
    END IF
    IF midLo > midHi - 40 THEN
        midLo = midHi - 40
    END IF

    barX = 76
    barW = sw - 132
    barH = 30
    barAy = panY + 12
    barPy = panY + 52
    fireY = panY + 92
    fireH = 32

    turn = 0
    sc0 = 0
    sc1 = 0
    aimA = 45
    aimP = 70
    wind = ui_rand(5) - 2
    quit = 0

    newCity()

    dlg = ui_dlg_msg("大猩猩扔香蕉", "两只大猩猩站在城市两头，轮流把香蕉扔到对面。拖「角度」和「力度」两根条调好，按「发射」。香蕉会被重力和风带着走 —— 风每回合变一次，看顶上那根风的指示。先拿满 3 分的人赢。", 0)
    ui_msg_clear()

    curMs = 0
    tid = 0
    WHILE ui_win_closed() = 0
        drawScene()

        IF st = 1 THEN
            wantMs = stepMs
        ELSE
            wantMs = idleMs
        END IF
        IF wantMs <> curMs THEN
            IF tid <> 0 THEN
                ui_timer_kill(tid)
            END IF
            tid = ui_timer_set(wantMs, 9)
            curMs = wantMs
        END IF

        ' 先等一条，再把**已经排队的**一次抽干：
        ' 一次滑动每秒能来几十条 TOUCHMOVE，一条一画的话重绘会被输入拖垮。
        ' 位置是幂等的（后一条覆盖前一条），所以只要把队列清空、然后画一帧就够。
        mt = ui_wait_msg(paceMs)
        n = 0
        WHILE mt <> 0
            IF mt = 10 THEN
                quit = 1
            END IF
            IF mt = 9 THEN
                IF st = 1 THEN
                    stepFlight()
                ELSE
                    IF st = 2 THEN
                        boomT = boomT + 1
                        IF boomT >= 8 THEN
                            resolveShot()
                        END IF
                    END IF
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
            IF mt = 7 THEN
                handlePoint(0)
            END IF
            IF mt = 3 THEN
                handlePoint(0)
            END IF
            n = n + 1
            IF n >= 40 THEN
                mt = 0
            ELSE
                mt = ui_poll_msg()
            END IF
        WEND

        IF st = 3 THEN
            IF quit = 0 THEN
                ' 终局：先按最终比分再画一帧（对话框会盖住画面，得让玩家看到定格）
                drawScene()
                ui_beep(1568, 200)
                IF sc0 >= wscore THEN
                    dlg = ui_dlg_msg("大猩猩扔香蕉", "玩家一 先拿满 3 分，赢了！再来一局？（选「否」退出）", 0)
                ELSE
                    dlg = ui_dlg_msg("大猩猩扔香蕉", "玩家二 先拿满 3 分，赢了！再来一局？（选「否」退出）", 0)
                END IF
                IF dlg <> 0 THEN
                    quit = 1
                ELSE
                    turn = 0
                    sc0 = 0
                    sc1 = 0
                    aimA = 45
                    aimP = 70
                    wind = ui_rand(5) - 2
                    newCity()
                    ui_msg_clear()
                END IF
            END IF
        END IF

        IF quit = 1 THEN
            EXIT WHILE
        END IF
    WEND

    IF tid <> 0 THEN
        ui_timer_kill(tid)
        tid = 0
    END IF
    ui_keep_on(0)
    ui_win_close()
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  主程序
' ══════════════════════════════════════════════════════════════════════════

' CONST 的替身变量（SUB 里只用这些普通变量，见上面缺陷 ② 的第四条）
' 顶层是好的，所以算术放在这里做。
' 网格清零（正弦表随后装）—— 必须在最前面，newCity 要用它存楼高
ui_gclear()
loadTrig()

' 开机自检：桌面脚手架（500–599 号全是空操作、没有真窗口）也能跑，
' 这是**唯一**能在桌面上证明弹道是对的的手段（缺陷 ⑩）。
physicsCheck()
simCheck()
simMode = 0

' 尺寸：先问设备，再开窗。**顺序照抄 gomoku.c** —— 排版用的那两个数
' 必须与交给窗口的那两个数**同源**，否则内容会画到画布外面。
sw = ui_scr_w()
sh = ui_scr_h()
IF sw <= 0 THEN
    sw = 380
END IF
IF sh <= 0 THEN
    sh = 660
END IF

' 只支持竖屏 + 不要手柄区：
'   · 竖屏 —— 城市是横着排的，转屏只会重排一次、玩家还得转回来，锁竖屏更省心；
'   · 不要手柄区 —— 全程触摸（两根条 + 一个发射键），手柄一个都用不到，
'     留着那一整块等于白吃掉一百多像素的画面高度（与 gomoku.c 同一处置）。
'   两个常量在这里写字面量：BASIC 侧没有 C 头文件那套宏，
'   第 4 个 0 = VML_WIN_PORTRAIT、第 5 个 0 = VML_WIN_NO_GAMEPAD（见 waycoder_ui.h）。
wh = ui_win_open_ex("大猩猩扔香蕉", sw, sh, 0, 0)

' 宿主把窗口开出来了（返回 1）才开跑；桌面脚手架这里返回 0 —— 上面自检已打完，
' 直接收工。**别**把这条判断当"平台探测"去别处复用，它只说明"这一轮有没有真窗口"。
IF wh < 1 THEN
    PRINT "（桌面脚手架：ui_* 号段是空操作、没有真窗口，弹道自检打完就退出）"
ELSE
    runGame()
END IF
