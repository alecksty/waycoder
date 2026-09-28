' ══════════════════════════════════════════════════════════════════════════
'  gorilla.bas —— 大猩猩扔香蕉（手机端 VML / BASIC 前端）
'  gorilla.bas — Gorilla Throws Bananas (mobile VML / BASIC front end)
'
'  两只大猩猩站在城市两端的楼顶上互扔香蕉。轮流调「角度」与「力度」，
'  Two gorillas stand on the rooftops at opposite ends of the city, throwing bananas at each other. Players take turns setting 「Angle」 and 「Power」,
'  香蕉在重力与风里划出抛物线，砸中对面得分，先拿满 3 分的人赢。
'  the banana arcs under gravity and wind, a hit on the opponent scores, and the first one to 3 points wins.
'
'  ## 关于玩法来源
'  ## About where the gameplay comes from
'
'  玩法规则（回合制、输角度与力度、重力 + 每回合随机的风、香蕉抛物线、
'  The rules (turn-based, enter angle and power, gravity + per-round random wind, the banana's parabola,
'  命中计分）是从经典炮击游戏那儿学的 —— 那是**玩法**，不是代码。
'  score on hit) are learned from the classic artillery games — that is **gameplay**, not code.
'  本文件的程序结构、变量命名、界面几何、手感数值全部重新设计，
'  This file's program structure, variable naming, UI geometry and feel numbers are all designed from scratch,
'  没有抄原版（GORILLA.BAS，Microsoft 1990，有版权）的任何一行。
'  without copying a single line of the original (GORILLA.BAS, Microsoft 1990, copyrighted).
'
'  ## 为什么只能走 ui_* 这条路（这条是**验过的**，不是猜的）
'  ## Why the ui_* route is the only one (this is **verified**, not guessed)
'
'  QBasic 原生的图形语句（SCREEN / LINE / CIRCLE / PAINT）在这条链上**上不了屏**：
'  QBasic's native graphics statements (SCREEN / LINE / CIRCLE / PAINT) **never reach the screen** on this chain:
'  它们的代码生成（`VMLPrepares/BasicCompiler/CodeGenerator.Qbasic.Graphics*.cs`）
'  their code generation (`VMLPrepares/BasicCompiler/CodeGenerator.Qbasic.Graphics*.cs`)
'  和共享库（`Lib/shared/src/graphics.c`）走的都是一套 **DOS VGA 帧缓冲模型** ——
'  and the shared library (`Lib/shared/src/graphics.c`) both use a **DOS VGA framebuffer model** —
'  `GetConfig #1` 拿到 `0xA0000` 当帧缓冲（`VMLRuntime/VMLRuntime.cs:288`）、
'  `GetConfig #1` gets `0xA0000` as the framebuffer (`VMLRuntime/VMLRuntime.cs:288`),
'  把像素写进那段内存，而**手机 App 从来不读那一段**：
'  writes pixels into that memory, and **the mobile App never reads that region at all**:
'     · 在 `WayCoder.Maui/` 下搜 `0xA0000` / `GetConfig` —— 一条命中都没有；
'         · search `0xA0000` / `GetConfig` under `WayCoder.Maui/` — not a single hit;
'     · `graphics.c` 里也**没有任何一处**调 `ui_present` 之类把它送到窗口。
'         · and `graphics.c` has **no place at all** calling `ui_present` or the like to send it to the window.
'  ⇒ 画了等于没画（连报错都不会有）。能上屏的只有 `ui_*` 那套宿主接口
'  ⇒ drawing equals not drawing (not even an error). Only the `ui_*` host interface can reach the screen
'  （syscall 500–599），实现在 `Lib/shared/src/vmlui.c` → `Lib/shared/vmlui.vml`，
'  (syscall 500-599), implemented in `Lib/shared/src/vmlui.c` → `Lib/shared/vmlui.vml`,
'  由 `vmltool.config.xml` 的 `<Language Name="basic" Libs="...,vmlui.vml">` 挂上来。
'  hooked up by `<Language Name="basic" Libs="...,vmlui.vml">` in `vmltool.config.xml`.
'
'  ## 写这份时实测出来的 BASIC 前端缺陷（每条都有最小复现，改这份别踩回去）
'  ## BASIC front-end defects measured while writing this file (each has a minimal repro; do not step back into them)
'
'   ① **`SIN` / `COS` / `SQR` 一律返回 0**。反汇编可见调用之后多出一条 `f2i R0 R0`：
'   ① **`SIN` / `COS` / `SQR` always return 0**. Disassembly shows an extra `f2i R0 R0` after the call:
'      把返回的**整数**当成浮点位型又转了一次 ⇒ 4 变成 0。底下的库函数本身是对的。
'      it converts the returned **integer** a second time as if it were a float bit pattern ⇒ 4 becomes 0. The library function underneath is itself correct.
'      `ABS` / `SGN` / `/` / `INT` / `AND` / `OR` 都正常（没有那条 f2i）。
'      `ABS` / `SGN` / `/` / `INT` / `AND` / `OR` are all fine (no such f2i).
'      最小复现：`PRINT SQR(16)` 打出 0（应为 4）。
'      Minimal repro: `PRINT SQR(16)` prints 0 (it should be 4).
'
'      ⚠ 直接 `NATIVE FUNCTION basic_sin(...)` 绕开内建路径**能拿到非零值**，
'      ⚠ Calling `NATIVE FUNCTION basic_sin(...)` directly to bypass the builtin path **does give a non-zero value**,
'      但那个库函数本身也是错的：`Lib/shared/src/basiclib.c` 的 `_sin_lookup`
'      but that library function is itself wrong: `_sin_lookup` in `Lib/shared/src/basiclib.c`
'      分段线性的常量抄成了**每段上界**的值（`if (deg <= 30) return 5000 + (deg-15)*167`
'      copied the piecewise-linear constants as the **upper bound of each segment** (`if (deg <= 30) return 5000 + (deg-15)*167`
'      ⇒ 30° 返回 7505，真值 5000；60° 甚至返回 10250 > 10000）。
'      ⇒ 30° returns 7505 where the true value is 5000; 60° even returns 10250 > 10000).
'      ⇒ 本文件的角度→单位向量改用**开机装表**（见 loadTrig）。
'      ⇒ this file builds the angle→unit-vector mapping as a **table loaded at boot** instead (see loadTrig).
'   ② **SUB 体的表达式是重灾区**（顶层基本是好的）。四条独立的坑，都**不报错**：
'   ② **Expressions inside a SUB body are the disaster zone** (the top level is mostly fine). Four independent traps, none of which **reports an error**:
'      (a) **`\` 和 `MOD` 编不出代码**。同一个 `g = 100 \ 2`，写在主程序里得 50，
'      (a) **`\` and `MOD` generate no code**. The very same `g = 100 \ 2` gives 50 in the main program,
'          写在 `SUB` 里得 **0**；`1000 \ 3` 得 10、`100 MOD 7` 得 10、
'          but gives **0** inside a `SUB`; `1000 \ 3` gives 10, `100 MOD 7` gives 10,
'          `46334659 \ 65536` 得 10。看汇编很清楚 —— SUB 体里 `\` 只发出
'          and `46334659 \ 65536` gives 10. The assembly is clear — inside a SUB body `\` only emits
'              move R1 #100 / move R2 #2 / **move R1 R0**      ← 本该是 div R1 R2
'              move R1 #100 / move R2 #2 / **move R1 R0**      ← this should be div R1 R2
'          顶层才是 `move R0 R11 / div R0 R10`。
'          Only at the top level is it `move R0 R11 / div R0 R10`.
'          ⇒ 除法一律写 `INT(a / b)`（`/` 与 `INT` 在 SUB 里是对的），取模一律
'          ⇒ always write division as `INT(a / b)` (`/` and `INT` are correct inside a SUB), and always
'            改成**减法计数循环**。
'            rewrite modulo as a **subtraction counting loop**.
'      (b) **带括号的复合子表达式算错**。`bw * (NB - 1)` 得 0（`bw * 5` 得 325）；
'      (b) **Parenthesised compound sub-expressions are computed wrong**. `bw * (NB - 1)` gives 0 (`bw * 5` gives 325);
'          `1 + (2 * 3)` 得 6。⇒ 表达式一律摊平、不套括号，中间量先落到变量上。
'          `1 + (2 * 3)` gives 6. ⇒ always flatten expressions, never nest parentheses, and land intermediate values in variables first.
'      (c) **CONST 参与算术就出错**。`IF bi > NB - 1 THEN bi = NB - 1` 会**无条件成立**
'      (c) **A CONST taking part in arithmetic goes wrong**. `IF bi > NB - 1 THEN bi = NB - 1` is **unconditionally true**
'          （`bi = 0` 也会把 bi 改成 5）⇒ 所有参与算术的常量先在主程序里落成普通变量
'          (`bi = 0` still sets bi to 5) ⇒ every constant used in arithmetic is first assigned to an ordinary variable in the main program
'          （见「CONST 的替身变量」那一段）。`WHILE i < NB` 这种"直接比较"是好的。
'          (see the 「stand-in variables for CONST」 section). A 「direct comparison」 like `WHILE i < NB` is fine.
'      (d) **大整数**（十万以上）的 `-` / `*` 也不可靠
'      (d) `-` / `*` on **large integers** (above one hundred thousand) is unreliable too
'          （`900000 - INT(900000/7)*7` 得 650267143）。
'          (`900000 - INT(900000/7)*7` gives 650267143).
'      ⇒ 本文件把中间量全压在 3 万以内：命中判定因此用**方形盒**而不是距离平方，
'      ⇒ this file keeps every intermediate value below 30000: hit tests therefore use a **square box** rather than squared distance,
'        位置全部换成 1/16 像素的整数单位（最大也就六千多）。
'        and all positions use integer 1/16-pixel units (at most six thousand or so).
'      (e) **`AND` 在 SUB 的条件里是坏的**：`IF a > 0 AND b > 50 THEN` 恒不成立
'      (e) **`AND` is broken in a SUB condition**: `IF a > 0 AND b > 50 THEN` is never true
'          （顶层 `v = 5 AND 3` 是好的）⇒ 条件一律拆成嵌套 IF 或用标志位。
'          (at the top level `v = 5 AND 3` is fine) ⇒ always split conditions into nested IFs or use flag variables.
'   ③ ~~**数组不能用**~~ —— **这条已过期（v0.96.487 实测）**。
'   ③ ~~**Arrays cannot be used**~~ — **this one has expired (measured in v0.96.487)**.
'      两种写法都验过，下标读写都对：`DIM a(8) AS INTEGER` 与不带类型的 `DIM b(8)`，
'      Both forms were checked and indexed reads/writes are correct: `DIM a(8) AS INTEGER` and untyped `DIM b(8)`,
'      在 SUB 里遍历赋值后 `a(3)` 读出 13、`b(3)` 读出 23（正是写进去的值）。
'      after assigning in a loop inside a SUB, `a(3)` reads back 13 and `b(3)` reads back 23 (exactly the values written).
'      当年那张音效表就是靠它做的（音效现已改走 `ui_beep` 单音，见下文音色表）。
'      That year's sound-effect table was built on it (sound effects now go through single-tone `ui_beep`; see the tone table below).
'      ⚠ 本文件里楼房高度、星星坐标、三角函数表**仍然**走 `ui_gset`/`ui_gget` 网格 ——
'      ⚠ In this file the building heights, star coordinates and trig table **still** go through the `ui_gset`/`ui_gget` grid —
'        那是当年为绕过这条而写的，**能跑就别动**（改它们与音效无关，风险白担）。
'        that was written back then to work around this trap, and **if it runs, leave it alone** (changing them has nothing to do with sound effects; the risk buys nothing).
'        但新写的代码**不必**再绕。
'        But newly written code **need not** work around it any more.
'   ④ **字符串拼接是坏的**：`b$ = "x" + "y"` 得到**空串**，不报错。
'   ④ **String concatenation is broken**: `b$ = "x" + "y"` yields an **empty string**, with no error.
'      `STR$(n)` 本身是好的 ⇒ 数字上屏走 `n$ = STR$(v)` 再交给 `ui_text`。
'      `STR$(n)` itself is fine ⇒ put numbers on screen via `n$ = STR$(v)` and hand that to `ui_text`.
'   ⑤ **`DIM x AS STRING` 是坏的**：随后 `x = "..."` 会编成 `func_x`、链接期找不到。
'   ⑤ **`DIM x AS STRING` is broken**: a later `x = "..."` compiles to `func_x` and is unresolved at link time.
'      要字符串变量就用后缀写法 `x$`，**不要 DIM**。
'      For a string variable use the suffix form `x$`; **do not DIM it**.
'   ⑥ **SUB 的形参不能是字符串**：传进去读到的是垃圾（实测打出 `1024`）。
'   ⑥ **A SUB parameter cannot be a string**: reading it back inside yields garbage (measured: it printed `1024`).
'      ⇒ 本文件所有子过程的形参**全是整数**，字符串一律在子过程里写字面量。
'      ⇒ every subprogram in this file takes **integers only**; strings are always written as literals inside the subprogram.
'      （模块级变量在 SUB 里读写是好的 —— tetris.bas 头部「SUB 读不到模块级变量」
'      (Reading and writing module-level variables inside a SUB is fine — the note 「a SUB cannot see module-level variables」
'       那段话**已经过期**，实测读写都对。）
'       at the top of tetris.bas **has expired**; both reads and writes measured correct.)
'   ⑦ 外部过程必须 `NATIVE SUB/FUNCTION` + **空体**才是裸标签，否则会被加上
'   ⑦ An external procedure needs `NATIVE SUB/FUNCTION` + an **empty body** to be a bare label; otherwise it gets
'      `sub_`/`func_` 前缀、链接期找不到。
'      a `sub_`/`func_` prefix and is unresolved at link time.
'   ⑧ 形参名不能叫关键字（`on` 之类），会把 NATIVE 声明弄坏。
'   ⑧ A parameter name must not be a keyword (`on` and the like); it corrupts the NATIVE declaration.
'   ⑨ 颜色写 `&HFFrrggbb`（词法器认 `&H`，不认 `0x`）。
'   ⑨ Write colours as `&HFFrrggbb` (the lexer knows `&H`, not `0x`).
'   ⑪ **写在 `SUB ... END SUB` 之后的主程序语句会被当成函数调用**：
'   ⑪ **Main-program statements written after `SUB ... END SUB` are treated as function calls**:
'      `nbv = NB` 编成 `func_nbv`，链接期报「未定义的函数 'func_nbv'」。
'      `nbv = NB` compiles to `func_nbv`, and the linker reports 「undefined function 'func_nbv'」.
'      最小复现：`SUB t() / END SUB` 之后再写 `q = 1`。
'      Minimal repro: write `q = 1` after `SUB t() / END SUB`.
'      ⇒ 主程序里那些**赋值**一律放在所有 SUB 定义**之前**（调用可以放后面，
'      ⇒ those **assignments** in the main program all go **before** every SUB definition (calls may come later;
'        `ui_gclear()` / `physicsCheck()` 那种调用写在末尾是好的 —— 坏的只有赋值）。
'        a call like `ui_gclear()` / `physicsCheck()` at the end is fine — only assignments are broken).
'   ⑩ 本文件的「弹道自检」故意做成**开机就跑**：桌面脚手架（vmlcli）把
'   ⑩ This file's 「ballistics self-check」 is deliberately made to **run at boot**: the desktop scaffold (vmlcli) treats
'      500–599 号全当空操作、也没有真窗口（`ui_win_open_ex` 返回 0），
'      opcodes 500-599 as no-ops and has no real window (`ui_win_open_ex` returns 0),
'      只有把那几行积分结果打出来，才能在桌面上证明弹道是对的 ——
'      so only by printing those integration results can the ballistics be proved correct on the desktop —
'      光看编译绿灯什么也证明不了。
'      a green compile proves nothing at all.
' ══════════════════════════════════════════════════════════════════════════

' ── 外部库声明（全 NATIVE：裸标签）────────────────────────────────────
' ── External library declarations (all NATIVE: bare labels) ────────────────────────────────────
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
' ⚠ 用**带竖对齐**的版本：本游戏的 y 一律按**顶线**给（老契约）。
' ⚠ Use the **vertical-alignment** version: this game always gives y as the **top line** (old contract).
'   v0.96.394 把 ui_text 的 y 统一成了**基线**，于是所有文字整体上移约 0.8×字号
'   v0.96.394 changed ui_text's y to be the **baseline** everywhere, which shifted all text up by about 0.8 × font size
'   （实测：y=100/字号20 的字，墨迹落在 88..100 ⇒ 全在 y 上方）。
'   (measured: for y=100 and font size 20, the ink lands at 88..100 ⇒ all above y).
'   3 = VML_VANCHOR_TOP（盒顶落在 y）⇒ 恢复老行为。
'   3 = VML_VANCHOR_TOP (the box top lands on y) ⇒ restores the old behaviour.
NATIVE SUB ui_text_v(x AS INTEGER, y AS INTEGER, s AS STRING, c AS INTEGER, size AS INTEGER, anchor AS INTEGER, valign AS INTEGER, style AS INTEGER)
END SUB
NATIVE SUB ui_present()
END SUB
NATIVE FUNCTION ui_scr_w() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_scr_h() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_win_open_ex(t AS STRING, w AS INTEGER, h AS INTEGER, rot AS INTEGER, pad AS INTEGER) AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_get_language() AS INTEGER
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
' 复音（v0.96.485）：一次起一个音、可以同时响好几个 —— 音效音序器用它。
' Polyphony (v0.96.485): start one tone per call, and several can sound at once — the sound sequencer uses it.
' ⚠ 形参名避开 BASIC 关键字（`on` 那种），全用短名。
' ⚠ Parameter names avoid BASIC keywords (like `on`); all are short names.
NATIVE FUNCTION ui_tone_on(ch AS INTEGER, note AS INTEGER, vel AS INTEGER) AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_tone_off(ch AS INTEGER, note AS INTEGER) AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_tone_wave(ch AS INTEGER, wave AS INTEGER) AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_tone_panic() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_tick() AS INTEGER
END FUNCTION
' 音效：现在只用 `ui_beep`（单音）。⚠ `ui_sfx_panic` 留着 —— 它是"全停"，
' Sound: only `ui_beep` (single tone) is used now. ⚠ `ui_sfx_panic` is kept — it is the 「stop everything」,
' 退出前调一次把所有正在响的声音（含 `ui_beep` 那条声道）一起掐掉。
' called once before exit to cut off every sounding voice at once (including the `ui_beep` channel).
NATIVE SUB ui_sfx_panic()
END SUB
NATIVE SUB ui_vibrate(ms AS INTEGER, strength AS INTEGER)
END SUB
' ⚠ 形参名不能叫 on —— BASIC 关键字，会把 NATIVE 声明弄坏（实测）
' ⚠ A parameter must not be named on — a BASIC keyword; it corrupts the NATIVE declaration (measured)
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
' ── Grid index table (the shared library's integer grid has 256 slots, three ranges that do not overlap) ────────────
'   0 .. 5      楼房顶部的 y（NB 个）
'   0 .. 5      rooftop y (NB of them)
'   6 .. 19     星星 x
'   6 .. 19     star x
'   20 .. 33    星星 y
'   20 .. 33    star y
'   100 .. 190  三角函数表：sin(a)*1000，a = 0..90
'   100 .. 190  trig table: sin(a)*1000, a = 0..90
'               cos 不另存 —— cos(a) = sin(90-a)，一次查表就够
'                           cos is not stored — cos(a) = sin(90-a), one lookup is enough
'   208 .. 255  弹坑表：每坑 3 格（x, y, r），16 坑见底
'   208 .. 255  crater table: 3 slots per crater (x, y, r); 16 craters fill it up
'
' ⚠ 网格一共 **256** 格（`Lib/shared/src/vmlui.c` 的 `UI_GRID_N`），而
' ⚠ The grid has **256** slots in total (`UI_GRID_N` in `Lib/shared/src/vmlui.c`), and
'   **200 .. 206 是俄罗斯方块的方块掩码**（那里的 `MASK_AT`，跨语言约定）⇒
'   **200 .. 206 are the tetris piece masks** (that file's `MASK_AT`, a cross-language convention) ⇒
'   弹坑表从 208 起、16 坑 ×3 = 48 格到 255 **正好塞满**，一格不越界。
'   the crater table starts at 208, and 16 craters × 3 = 48 slots ends at 255, **exactly filling it**, without overrunning a single slot.
'   要加坑先算一遍 `G_HOLE + MAXHOLE*3 - 1 <= 255`，越界是**静默丢弃**
'   To add craters, first check `G_HOLE + MAXHOLE*3 - 1 <= 255`; going out of range is a **silent drop**
'   （`ui_gset` 越界不报错），表现是"后面的坑不生效"，很难查。
'   (`ui_gset` reports nothing when out of range), which shows up as 「the later craters do not take effect」 and is hard to track down.
CONST G_ROOF = 0
CONST G_STARX = 6
CONST G_STARY = 20
CONST G_TRIG = 100
CONST G_HOLE = 208
CONST MAXHOLE = 16        ' 一局最多记 16 个坑（环形复用，满了盖最老的）
' At most 16 craters per round (ring reuse; the oldest is overwritten when full)
CONST HOLE_R = 22         ' 一发炸掉的半径（像素）。楼宽约 63，约 1/3 栋
' Blast radius of one shot (pixels). A building is about 63 wide, so roughly 1/3 of one

' ── 手感数值（都在这儿，改手感只动这一段）────────────────────────────
' ── Feel numbers (all here; to change the feel, touch only this block) ────────────────────────────
CONST S = 16              ' 子像素刻度：1 像素 = 16 个单位
' Sub-pixel scale: 1 pixel = 16 units
CONST GRAV = 13           ' 重力（单位 / 步²）
' Gravity (units / step²)
CONST NB = 6              ' 楼房数量
' Number of buildings
CONST NSTAR = 14          ' 星星数量
' Number of stars
CONST WINSCORE = 3        ' 先拿满几分获胜
' Points needed to win
CONST APE_R = 21          ' 大猩猩命中盒的半边长（像素，方形盒）
' Half-side of the gorilla hit box (pixels, square box)
CONST STEP_MS = 30        ' 飞行时的定时器间隔
' Timer interval while in flight
CONST IDLE_MS = 120       ' 瞄准时的定时器间隔（画面基本静止，省电）
' Timer interval while aiming (the picture is nearly static; saves power)
CONST PACE_MS = 40        ' 主循环最长睡眠：把重绘锁在 ~25fps，也保证输入延迟 ≤40ms
' Longest sleep of the main loop: caps redraws at ~25fps and also keeps input latency ≤40ms
CONST PANH = 132          ' 底部操作区高度
' Height of the bottom control area
CONST HUDH = 40           ' 顶部信息带高度
' Height of the top info band

' ── 配色（0xAARRGGBB）────────────────────────────────────────────────
' ── Palette (0xAARRGGBB) ────────────────────────────────────────────────
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
' ── Module-level variables (all DIMmed before any assignment) ──────────────────────────────────
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
' Craters (the piece blasted out of a building; see clearHoles/addHole)
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
' The variables below are **stand-ins for CONSTs**; their assignments are in the block right after this one (and must come before the SUBs).
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

' ── 音效音序器：模块级状态 ─────────────────────────────────────────────
' ── Sound sequencer: module-level state ─────────────────────────────────────────────
' ⚠ 数组**能用**（文件头缺陷 ③「`DIM a(10)` 会被拆成 10 个标量」已经过期：
' ⚠ Arrays **do work** (header defect ③, 「`DIM a(10)` gets split into 10 scalars」, has expired:
'   实测 `DIM a(8) AS INTEGER` 与不带的 `DIM b(8)` 两种写法，下标读写都对）。
'   measured on both `DIM a(8) AS INTEGER` and plain `DIM b(8)`, indexed reads and writes are correct).
'   这里仍然把下标循环写成 `DO WHILE`，是为了照文件里其它 SUB 的样子来。
'   Index loops are still written as `DO WHILE` here, just to match the other SUBs in this file.
DIM sfxCh(16) AS INTEGER
DIM sfxNote(16) AS INTEGER
DIM sfxDel(16) AS INTEGER
DIM sfxDur(16) AS INTEGER
DIM sfxVel(16) AS INTEGER

' ── CONST 的替身变量（SUB 里只用这些普通变量）──────────────────────────
' ── Stand-in variables for CONSTs (the SUBs use only these ordinary variables) ──────────────────────────
' 缺陷 ② 的第四种形态：CONST 一旦在 SUB 体里**参与算术**就会出错 ——
' The fourth form of defect ②: a CONST **taking part in arithmetic** inside a SUB body goes wrong —
'   `IF bi > NB - 1 THEN bi = NB - 1` 会**无条件成立**（最小复现见文件头），
'   `IF bi > NB - 1 THEN bi = NB - 1` is **unconditionally true** (minimal repro in the file header),
'   而 `WHILE i < NB`（CONST 直接当比较对象，没有算术）是好的。
'   while `WHILE i < NB` (the CONST used directly as a comparison operand, no arithmetic) is fine.
' 所以凡是要算的常量，都在这里先落成普通变量。
' So every constant that needs computing is first dropped into an ordinary variable here.
'
' ⚠ 这个赋值块**必须放在所有 SUB 定义之前** —— 缺陷 ⑪：写在 SUB 之后的主程序语句
' ⚠ This assignment block **must come before every SUB definition** — defect ⑪: main-program statements after a SUB
'   会被当成函数调用（`nbv = NB` 编成 `func_nbv`，链接期报「未定义的函数」）。
'   are treated as function calls (`nbv = NB` compiles to `func_nbv`; the linker reports 「undefined function」).
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
' The city's terrain bands (all computed into ordinary variables first, same reason as above)
'   · 楼房：楼顶 y 落在 [topMin, topMax)
'     · Buildings: the rooftop y lands in [topMin, topMax)
'   · 大猩猩站的两栋：另外收在一个中间带上，免得猿被顶到信息带里、
'     · The two the gorillas stand on: pulled into a middle band instead, so the apes are not pushed up into the info band
'     或者矮到让对射变成一条直线
'         or left so low that the exchange of fire becomes a straight line
topMin = hudh + 150
midLo = hudh + 205
midHi = midLo + 70

' ══════════════════════════════════════════════════════════════════════════
'  子过程
'  Subprograms
'
'  ⚠ 写在 SUB 里的除法一律用 `INT(a / b)`、取模一律用减法计数 ——
'    ⚠ Division written inside a SUB always uses `INT(a / b)`, and modulo always uses a subtraction count —
'    见文件头缺陷 ②，`\` 和 `MOD` 在 SUB 体里会静默给出垃圾值。
'        see header defect ②: `\` and `MOD` silently give garbage inside a SUB body.
' ══════════════════════════════════════════════════════════════════════════

' 正弦表：sin(a°) × 1000，a = 0..90，存在 ui_gget(G_TRIG + a)。
' Sine table: sin(a°) × 1000 for a = 0..90, stored at ui_gget(G_TRIG + a).
' 装一次管一整局（是常数表）。值按 sin 精确算出来写死，
' Loaded once per round (it is a constant table). The values are exact sin results hard-coded in,
' **不是**运行时算的 —— 前端的 SIN 是坏的，库里的 basic_sin 也不准（缺陷 ①）。
' **not** computed at run time — the front end's SIN is broken and the library's basic_sin is inaccurate (defect ①).
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
' Current angle → unit vector (×1000). cos comes from sin's symmetry: cos a = sin(90-a).
SUB aimAngles()
    sinv = ui_gget(gtrig + aimA)
    cosv = ui_gget(gtrig + 90 - aimA)
END SUB

' 力度 → 初速（单位 / 步）。
' Power → initial speed (units / step).
'
' 这里是**让射程与力度成正比**：射程 ∝ 速度²，所以速度取 √力度。
' Here the goal is to **make range proportional to power**: range ∝ speed², so speed is taken as √power.
' 直接用「速度 ∝ 力度」的话，力度条上 40 以下几乎全打不到人、大半根条是废的
' With a plain 「speed ∝ power」, almost everything below 40 on the power bar misses and most of the bar is wasted
' —— 这是照着射程算过之后改的，不是随手写的。
' — this was changed after working the ranges out, not written off the cuff.
'
' 开方用整数牛顿迭代（从 100 起，12 次足够收敛到 n ≤ 10000 的整数根）。
' The square root uses integer Newton iteration (starting at 100; 12 rounds are enough to converge to the integer root for n ≤ 10000).
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
            ' Split into two statements, no parentheses: a parenthesised sub-expression inside a SUB is computed wrong (defect ②)
            idx = INT(spdN / rr)
            idx = rr + idx
            rr = INT(idx / 2)
            i = i + 1
        WEND
        spd = rr * 3
    END IF
END SUB

' 纯算术：算一条无风弹道回到出发高度的水平距离（像素），结果放 rng。
' Pure arithmetic: the horizontal distance (pixels) a windless trajectory covers before returning to its launch height; the result goes in rng.
' 与实弹**同一套积分**，所以它是弹道的回归判据。
' It uses the **same integrator** as live shots, so it is the regression check for ballistics.
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
' Boot self-check: the ranges of a few typical trajectories. Range is proportional to power ⇒ 100/50/25 should be about 4 : 2 : 1.
SUB physicsCheck()
    IF LANG = 0 THEN PRINT "── 大猩猩扔香蕉 · 开机自检（与实弹同一套积分）──" ELSE PRINT "-- Gorilla - boot self-check (same integrator as live shots) --"
    aimA = 45
    aimAngles()
    IF LANG = 0 THEN PRINT "  查表 角度45 -> cos/sin（应 707/707）:"; cosv; sinv ELSE PRINT "  table 45deg -> cos/sin (want 707/707):"; cosv; sinv
    aimA = 30
    aimAngles()
    IF LANG = 0 THEN PRINT "  查表 角度30 -> cos/sin（应 866/500）:"; cosv; sinv ELSE PRINT "  table 30deg -> cos/sin (want 866/500):"; cosv; sinv
    shotRange(45, 100)
    IF LANG = 0 THEN PRINT "  45度 力度100 -> 射程(px)"; rng ELSE PRINT "  45deg power100 -> range(px)"; rng
    shotRange(45, 50)
    IF LANG = 0 THEN PRINT "  45度 力度050 -> 射程(px)"; rng ELSE PRINT "  45deg power050 -> range(px)"; rng
    shotRange(45, 25)
    IF LANG = 0 THEN PRINT "  45度 力度025 -> 射程(px)"; rng ELSE PRINT "  45deg power025 -> range(px)"; rng
    shotRange(90, 100)
    IF LANG = 0 THEN PRINT "  90度 力度100 -> 射程(px)"; rng ELSE PRINT "  90deg power100 -> range(px)"; rng
    IF LANG = 0 THEN PRINT "  （射程应近似与力度成正比：100/50/25 约 4:2:1；90 度应约 0）" ELSE PRINT "  (range should scale with power: 100/50/25 ~ 4:2:1; 90deg should be ~0)"
END SUB

' 打一发「实弹」但不画屏：把香蕉架好、一路 stepFlight 到结束，结果留在
' Fire one 「live」 shot without drawing: arm the banana, run stepFlight to the end, and leave the result in
' hitFlag / ebx / eby / st 里。碰撞规则的自检就靠它。
' hitFlag / ebx / eby / st. The collision-rule self-check relies on it.
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
        ' One shot cannot fly this many steps; if it gets stuck the self-check fails, so an endless loop cannot hang the whole machine
        IF i > 900 THEN
            st = 9
        END IF
    WEND
END SUB

' 碰撞规则自检：一座**固定的**城市 + 几发已知的弹。
' Collision-rule self-check: one **fixed** city + a few shots whose outcome is known.
' 只用共享库的网格和纯算术，所以桌面脚手架（没有真窗口）也跑得起来 ——
' It uses only the shared library's grid and pure arithmetic, so it also runs on the desktop scaffold (no real window) —
' 「弹道自检」只证明了积分，「这一发算不算命中」得靠这一段。
' the 「ballistics self-check」 only proves the integrator; 「does this shot count as a hit」 needs this block.
SUB simCheck()
    IF LANG = 0 THEN PRINT "── 碰撞规则自检（固定城市：中间略高，两头 90 高）──" ELSE PRINT "-- collision rules self-check (fixed city: taller middle, 90 at both ends) --"
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
    IF LANG = 0 THEN PRINT "  两猿 x/楼顶 y:"; g0x; g0y ELSE PRINT "  apes x/roof y:"; g0x; g0y
    IF LANG = 0 THEN PRINT "  两猿 x/楼顶 y:"; g1x; g1y ELSE PRINT "  apes x/roof y:"; g1x; g1y
    IF LANG = 0 THEN PRINT "  画布"; sw; sh ELSE PRINT "  canvas"; sw; sh
    IF LANG = 0 THEN PRINT "  地面 y（发射点在此之上 44）:"; ground ELSE PRINT "  ground y (launch point sits 44 above):"; ground

    ' ① 角度 0：平着扔出去，会撞上中间那栋高楼
    ' ① Angle 0: thrown flat, it hits the tall building in the middle
    simShot(0, 100)
    IF LANG = 0 THEN PRINT "  ① 0度 力度100 -> 命中猿?"; hitFlag ELSE PRINT "  (1) 0deg power100 -> hit ape?"; hitFlag
    IF LANG = 0 THEN PRINT "     落点 x/y:"; ebx; eby ELSE PRINT "     impact x/y:"; ebx; eby

    ' ② 角度 90：垂直向上，原路落回自己站的那栋楼
    ' ② Angle 90: straight up, it falls back onto the building it launched from
    simShot(90, 100)
    IF LANG = 0 THEN PRINT "  ② 90度 力度100 -> 命中猿?"; hitFlag ELSE PRINT "  (2) 90deg power100 -> hit ape?"; hitFlag
    IF LANG = 0 THEN PRINT "     落点 x/y:"; ebx; eby ELSE PRINT "     impact x/y:"; ebx; eby

    ' ③ 角度 45、力度全开：飞过头，出界脱靶
    ' ③ Angle 45, full power: it overshoots, leaves the field and misses
    simShot(45, 100)
    IF LANG = 0 THEN PRINT "  ③ 45度 力度100 -> 命中猿?"; hitFlag ELSE PRINT "  (3) 45deg power100 -> hit ape?"; hitFlag
    IF LANG = 0 THEN PRINT "     落点 x/y:"; ebx; eby ELSE PRINT "     impact x/y:"; ebx; eby

    ' ④ 角度 45、力度 68：射程约 306，弹道下坠时正好穿过对面那只
    ' ④ Angle 45, power 68: range about 306, and the descending arc passes right through the opposite ape
    '    （两猿相距 325、命中盒 y 在 378~420）—— 这一发必须命中
    '        (the two apes are 325 apart, the hit box y is 378~420) — this shot must hit
    simShot(45, 68)
    IF LANG = 0 THEN PRINT "  ④ 45度 力度068 -> 命中猿?"; hitFlag ELSE PRINT "  (4) 45deg power068 -> hit ape?"; hitFlag
    IF LANG = 0 THEN PRINT "     落点 x/y:"; ebx; eby ELSE PRINT "     impact x/y:"; ebx; eby
    ' ⚠ `hitBld` 每发都会在 stepFlight 开头清零 ⇒ **必须当场打印**，
    ' ⚠ `hitBld` is cleared at the start of stepFlight for every shot ⇒ it **must be printed on the spot**;
    '   攒到后面再打拿到的是最后一发的值（这里第一版就写错过一次）。
    '   printing it later gives the last shot's value (the first version of this got exactly that wrong once).
    IF LANG = 0 THEN PRINT "     打猿不留坑 -> hitBld(应 0):"; hitBld ELSE PRINT "     ape hit leaves no hole -> hitBld (want 0):"; hitBld

    ' ⑤ 角度 45、力度三成：射程约 135，落在城里某栋楼上
    ' ⑤ Angle 45, power 30 percent: range about 135, landing on one of the city's buildings
    '    ⚠ 先清空弹坑：前面 ①② 也都打在楼上、各留了一个坑，
    '        ⚠ Clear the craters first: shots ①② above also hit buildings and each left a crater,
    '      不清的话下面 `ui_gget(ghole)` 读到的是**最早**那个坑，判据对不上落点。
    '            and without clearing, the `ui_gget(ghole)` below reads the **earliest** crater, so the check would not match the impact point.
    clearHoles()
    simShot(45, 30)
    IF LANG = 0 THEN PRINT "  ⑤ 45度 力度030 -> 命中猿?"; hitFlag ELSE PRINT "  (5) 45deg power030 -> hit ape?"; hitFlag
    IF LANG = 0 THEN PRINT "     落点 x/y:"; ebx; eby ELSE PRINT "     impact x/y:"; ebx; eby

    ' ⑥ 弹坑判据：这一段钉的就是用户报的那件事「炸了建筑，炸完又还原了」——
    ' ⑥ Crater check: this block pins down exactly what the user reported, 「the building blows up and then comes back」 —
    '    从前爆炸只是一层特效、楼体数据一个字节没动，所以缺口下一帧就被重画抹平。
    '        the explosion used to be a visual effect only, with not one byte of building data changed, so the gap was repainted flat on the next frame.
    IF LANG = 0 THEN PRINT "  ⑥ 打楼留坑 -> hitBld(应 1):"; hitBld ELSE PRINT "  (6) building hit leaves a hole -> hitBld (want 1):"; hitBld
    IF LANG = 0 THEN PRINT "     坑数 nHole(应 1):"; nHole ELSE PRINT "     hole count nHole (want 1):"; nHole
    IF LANG = 0 THEN PRINT "     坑 x/y/r(应 = ⑤ 的落点, 22):" ELSE PRINT "     hole x/y/r (want = impact of (5), 22):"
    PRINT "       "; ui_gget(ghole); ui_gget(ghole + 1); ui_gget(ghole + 2)

    ' ⑦ 同一发**再打一遍**：这一次要从刚才那个缺口里穿过去 ⇒ 落点必然更低。
    ' ⑦ Fire the **same shot again**: this time it goes through the gap just made ⇒ the impact must be lower.
    '    这是"楼被打穿"的判据 —— 穿不过去的话新落点会与 ⑤ 逐像素相同。
    '        This is the 「punched through the building」 check — if it cannot pass, the new impact is pixel-identical to ⑤.
    pby = eby
    simShot(45, 30)
    IF LANG = 0 THEN PRINT "  ⑦ 再打一发 -> 新落点 y(应 > ⑤ 的 y):"; eby ELSE PRINT "  (7) fire again -> new impact y (want > y of (5)):"; eby
    IF LANG = 0 THEN PRINT "     ⑤ 的 y:"; pby ELSE PRINT "     y of (5):"; pby
    IF LANG = 0 THEN PRINT "     坑数 nHole(应 2，穿过去之后又炸了一层):"; nHole ELSE PRINT "     hole count nHole (want 2 - punched through and blasted another layer):"; nHole

    ' ⑧ 换局要清空：城市都重排了，旧坑的位置毫无意义
    ' ⑧ A new round must clear it: the city is re-laid out, so old crater positions mean nothing
    '    （不清的话上一局的洞会以天空色的圆出现在新楼上，像贴了几块补丁）
    '        (without clearing, last round's holes appear as sky-coloured circles on the new buildings, like patches stuck on)
    clearHoles()
    IF LANG = 0 THEN PRINT "  ⑧ 换局后 nHole(应 0):"; nHole ELSE PRINT "  (8) nHole after a new round (want 0):"; nHole

    simMode = 0
END SUB

' ── 弹坑：楼被炸掉的那一块 ─────────────────────────────────────────────
' ── Craters: the piece blasted out of a building ─────────────────────────────────────────────
'
' 为什么必须**记成状态**、而不是"爆炸时画一下"：`drawScene` 每帧先 `ui_clear`
' Why it must be **recorded as state** rather than 「just drawn at the moment of the explosion」: `drawScene` calls `ui_clear` first every frame
' 再把每栋楼**整栋**重画（见那里的循环），爆炸要是只画在楼上面，下一帧就被
' and then redraws each building **whole** (see the loop there); an explosion drawn only on top of the building is
' 盖回原样 —— 用户看到的正是「猴子炸了建筑，炸完又还原了」。
' painted back over on the next frame — which is exactly the 「the monkey blows up the building and it comes back」 the user saw.
' 缺口得跟着这一局留住，所以存进网格，由 `drawBuilding` 之后的那一趟统一涂回去。
' The gap has to persist for the round, so it is stored in the grid and repainted in one pass after `drawBuilding`.
'
' 存法沿用全仓"没有可靠数组"的惯例（文件头缺陷 ③）：`ui_gget/ui_gset` 的整数网格，
' Storage follows the repo-wide 「no reliable arrays」 convention (header defect ③): the `ui_gget/ui_gset` integer grid,
' 每坑 3 格 x/y/r。**环形复用**（满了从最老的开始盖）—— 这一点是刻意的：
' 3 slots x/y/r per crater. **Ring reuse** (once full, the oldest is overwritten) — this is deliberate:
' 宁可让老坑消失，也不能让"满了之后的新伤害不生效"（那会变成"打不动了"）。
' better to lose an old crater than to have 「new damage stops taking effect once full」 (that turns into 「the building cannot be damaged any more」).
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
' A new round: re-lay the city, put the gorillas on the rooftops at both ends, scatter the stars, put the banana back in hand
SUB newCity()
    ' 中间那几栋是"掩体"，高度随机；两头（大猩猩站的）收在中间带里
    ' The middle buildings are 「cover」 with random heights; the two at the ends (where the gorillas stand) are pulled into the middle band
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
    ' The city has been re-laid out ⇒ old crater positions mean nothing and must be cleared
    ' （不清的话上一局的洞会以天空色的圆出现在新楼上，像贴了几块补丁）
    ' (without clearing, last round's holes appear as sky-coloured circles on the new buildings, like patches stuck on)
    clearHoles()
    st = 0
    armBanana()
END SUB

' 把香蕉放回当前玩家的手上（大猩猩头顶再高一点，免得一起手就砸自家楼顶）
' Put the banana back in the current player's hand (a little above the gorilla's head, so it does not smash its own rooftop right away)
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
' One flight frame: take a step, test collisions, and switch to the exploding state on a hit or a landing
'
' ⚠ 这一段的写法是**刻意的**，别"顺手优化"回去（缺陷 ②的第三种形态）：
' ⚠ The way this block is written is **deliberate**; do not 「tidy it up」 back (the third form of defect ②):
'   · 每个变量只干一件事 —— 早先 `idx` 先当"出界阈值"（= 6624）又当"楼号"，
'     · Every variable does exactly one job — earlier `idx` served first as the 「out-of-bounds threshold」 (= 6624) and then as the 「building index」,
'     结果楼号那句被排到了钳位之后，`idx` 永远是钳出来的 5，
'         so the building-index statement ended up after the clamp, `idx` was always the clamped 5,
'     所有楼都被当成最后一栋、香蕉一起手就炸（实测落点 (50,372)，第一步就结束）。
'         every building was treated as the last one, and the banana exploded the moment it was thrown (measured impact (50,372), over on the first step).
'   · 判断一律**摊平**，不套 IF、不写 AND —— `AND` 在 SUB 的条件里是坏的。
'     · Conditions are always **flattened**, never nested IFs and never AND — `AND` is broken in a SUB condition.
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
    ' ① Out of bounds left/right (24 pixels of margin each side; once out it counts as a miss)
    IF bx < -384 THEN
        ended = 1
    END IF
    bxLim = sw + 24
    bxLim = bxLim * S
    IF bx > bxLim THEN
        ended = 1
    END IF

    ' ② 目标大猩猩。用**方形盒**而不是距离平方：SUB 里大整数乘法不可靠，
    ' ② The target gorilla. A **square box** rather than squared distance: large-integer multiplication inside a SUB is unreliable,
    '    而且盒子本来就更贴那只方头方身的大猩猩。
    '        and a box fits that square-headed, square-bodied gorilla better anyway.
    '    先判猿、后判楼 —— 大猩猩站在楼顶上，顺序反了的话
    '        Test the ape first and the building second — the gorilla stands on the rooftop, and with the order reversed
    '    「擦着对面头皮过去」会被算成打在墙上。
    '        「grazing the opponent's scalp」 would be counted as hitting a wall.
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
    ' ③ Ground collision: the ground height for this column = the top of that building in the city; outside the city it is the horizon.
    '    摊成一条直线：先算楼号（城外的记 -1）、再钳、再取高度。
    '        Flattened into a straight line: compute the building index (recorded as -1 outside the city), clamp it, then take the height.
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
        ' It has dropped below the rooftop of its own column — but if it is landing in a **gap blasted out earlier**,
        ' 那儿是空的，香蕉该穿过去继续飞（这就是"打了几个洞之后能打穿楼"那件事）。
        ' that spot is empty and the banana should pass through and keep flying (this is the 「punch through the building after a few holes」 behaviour).
        inHole = 0
        i = 0
        WHILE i < nHole
            hi = i * 3
            hi = ghole + hi
            hx = ui_gget(hi)
            hy = ui_gget(hi + 1)
            hr = ui_gget(hi + 2)
            ' 方形盒判定，与上面猿命中盒同一个理由（SUB 里大整数乘法不可靠）。
            ' Square-box test, for the same reason as the ape hit box above (large-integer multiplication inside a SUB is unreliable).
            ' ⚠ 盒子取圆的**内接**正方形（半径 × 7/10 ≈ 0.707），不是外接 ——
            ' ⚠ The box is the circle's **inscribed** square (radius × 7/10 ≈ 0.707), not the circumscribed one —
            '   取外接的话香蕉能从缺口的**四个角**穿过去，那看起来就是穿墙；
            '       with the circumscribed one the banana could slip through the gap's **four corners**, which looks like walking through walls;
            '   取内接最多是"炸点比看到的洞口低一点点"，方向是对的。
            '       with the inscribed one the worst case is 「the blast point is a little lower than the visible opening」, which errs the right way.
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
        ' Hit a building ⇒ blast out a gap (recorded as state; see the explanation at clearHoles).
        ' 打在猿身上 / 落在地平线上不算 —— 那些地方本来就没有楼。
        ' A hit on the ape / a landing on the horizon does not count — there is no building there to begin with.
        IF hitBld = 1 THEN
            addHole(tx, ty, holeR)
        END IF
        ' simMode = 1 时不发声 —— 开机自检会在真机上连放几炮，
        ' No sound while simMode = 1 — the boot self-check fires several shots in a row on a real device,
        ' 那几声不该让用户在见到画面之前先听一串蜂鸣。
        ' and the user should not hear a string of beeps before ever seeing the picture.
        IF simMode = 0 THEN
            IF hitFlag = 1 THEN
                sfxHit
                ui_vibrate(45, 120)
            ELSE
                sfxGroundBoom
                ui_vibrate(20, 60)
            END IF
        END IF
        st = 2
        boomT = 0
    END IF
END SUB

' 一发打完：计分、换人、够分就终局
' A shot is over: score it, pass the turn, and end the round if the score is reached
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
            ' Wind changes per 「round」: it is re-rolled only when it is player 1's turn, so both players face the same wind within a round
            IF turn = 0 THEN
                wind = ui_rand(5) - 2
            END IF
            st = 0
            armBanana()
        END IF
    END IF
END SUB

' 一栋楼（含窗户）。窗户的明暗按 (楼号, 列, 行) 算出来的确定性图案 ——
' One building (windows included). Window brightness follows a deterministic pattern computed from (building index, column, row) —
' 不能用随机数：drawScene 每帧都跑，随机会变成满屏闪烁。
' random numbers cannot be used: drawScene runs every frame, and randomness would turn into a screen full of flicker.
' 相位用减法计数拿，不用 MOD（缺陷 ②）。
' The phase comes from a subtraction count, not MOD (defect ②).
SUB drawBuilding(bi AS INTEGER)
    tx = bi * bw
    ty = ui_gget(groof + bi)

    ' 楼体颜色：bi 对 3 取模，用减法循环
    ' Building body colour: bi modulo 3, via a subtraction loop
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
    ' The window column spacing is computed up front (no parentheses nested inside an expression; see defect ②)
    rw = bw - 22
    rw = INT(rw / 2)
    c = 0
    WHILE c < 2
        wx = tx + 9 + c * rw
        wy = ty + 12
        ' 每列起手的相位不同，整排窗户才会有点亮暗错落
        ' Each column starts at a different phase, which is what gives the window rows their scattered lit/unlit look
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
' One gorilla: (ax, ay) is the rooftop it stands on (sole of the feet); who only selects colour/facing.
' 全部用矩形和圆拼，不依赖任何图片资源。
' Everything is assembled from rectangles and circles, with no image assets.
SUB drawApe(ax AS INTEGER, ay AS INTEGER, who AS INTEGER)
    colr = C_APE0
    IF who = 1 THEN
        colr = C_APE1
    END IF
    ' 腿
    ' Legs
    ui_rect(ax - 8, ay - 8, 6, 8, colr, 1, 0, 2)
    ui_rect(ax + 2, ay - 8, 6, 8, colr, 1, 0, 2)
    ' 身体
    ' Body
    ui_rect(ax - 9, ay - 23, 18, 16, colr, 1, 0, 4)
    ' 手臂
    ' Arms
    ui_rect(ax - 14, ay - 21, 5, 14, colr, 1, 0, 2)
    ui_rect(ax + 9, ay - 21, 5, 14, colr, 1, 0, 2)
    ' 头
    ' Head
    ui_circle(ax, ay - 28, 7, colr, 1, 0)
    ' 两只眼睛（朝向对面：玩家一往右看，玩家二往左看）
    ' Two eyes (facing the opponent: player 1 looks right, player 2 looks left)
    IF who = 0 THEN
        ui_circle(ax + 1, ay - 29, 2, C_TEXT, 1, 0)
        ui_circle(ax + 5, ay - 29, 2, C_TEXT, 1, 0)
    ELSE
        ui_circle(ax - 5, ay - 29, 2, C_TEXT, 1, 0)
        ui_circle(ax - 1, ay - 29, 2, C_TEXT, 1, 0)
    END IF
END SUB

' 把整数画到屏幕上（字符串拼接是坏的，只能用 STR$ 整份赋值 —— 缺陷 ④）
' Draw an integer on screen (string concatenation is broken; only a whole-string assignment via STR$ works — defect ④)
SUB drawNum(x AS INTEGER, y AS INTEGER, v AS INTEGER, col AS INTEGER, sz AS INTEGER, ac AS INTEGER)
    n$ = STR$(v)
    ui_text_v(x, y, n$, col, sz, ac, 3, 0)
END SUB

' 瞄准时的预览虚线：只画前十几步，给个方向感，不把落点泄露出去
' The dotted aiming preview: only the first dozen or so steps, to give a sense of direction without giving the impact point away
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
' Full-screen redraw
SUB drawScene()
    cxc = INT(sw / 2)

    ' ── 天空、星星、月亮 ──
    ' ── Sky, stars, moon ──
    ui_clear(C_SKY)
    i = 0
    WHILE i < nstar
        ui_rect(ui_gget(gstarx + i), ui_gget(gstary + i), 2, 2, C_STAR, 1, 0, 0)
        i = i + 1
    WEND
    ' 月亮：一个亮圆 + 一个「咬掉」的天空色圆（天空是纯色，所以直接盖）
    ' Moon: a bright circle plus a 「bitten-out」 sky-coloured circle (the sky is a flat colour, so it can just be painted over)
    ui_circle(sw - 54, hudh + 48, 20, C_MOON, 1, 0)
    ui_circle(sw - 45, hudh + 41, 18, C_SKY, 1, 0)

    ' ── 城市 ──
    ' ── City ──
    i = 0
    WHILE i < nbv
        drawBuilding(i)
        i = i + 1
    WEND

    ' ── 弹坑：把炸掉的那块涂回天空色 ──
    ' ── Craters: paint the blasted-out part back to sky colour ──
    ' 位置很讲究：**必须在画完所有楼之后**（画在楼前面会被下一栋楼盖住，
    ' Placement matters: it **must come after every building is drawn** (drawn earlier it would be covered by the next building,
    ' 而缺口本来就可能横跨两栋的交界），**必须在地面之前**（否则会把地面啃掉一块）。
    ' and a gap can straddle the boundary between two buildings), and **must come before the ground** (otherwise it bites a chunk out of the ground).
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
    ' ── The two gorillas ──
    drawApe(g0x, g0y, 0)
    drawApe(g1x, g1y, 1)

    ' 轮到谁：在它头顶画个朝下的小箭头
    ' Whose turn it is: draw a small downward arrow above its head
    ' （用两条线拼 —— 多边形要 int 数组，而数组不能用，见缺陷 ③）
    ' (assembled from two lines — a polygon needs an int array, and arrays cannot be used; see defect ③)
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
    ' ── Banana / trail / preview / explosion ──
    IF st = 0 THEN
        ' 先按**当前**的角度/力度重算单位向量与初速再画预览 —— 少了这一步，
        ' Recompute the unit vector and initial speed from the **current** angle/power before drawing the preview — without this step
        ' 玩家拖条的时候预览还是上一发的方向（只在发射那一刻才更新），
        ' the preview still points along the previous shot's direction while the player drags the bars (it only updated at the moment of firing),
        ' 看起来就是"改了没反应"。
        ' which looks like 「I changed it and nothing happened」.
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
    ' ── Top info band ──
    ui_rect(0, 0, sw, hudh, C_HUD, 1, 0, 0)
    IF turn = 0 THEN
        ui_rect(6, 5, 100, 30, C_HUD_ON, 1, 0, 8)
    ELSE
        ui_rect(sw - 106, 5, 100, 30, C_HUD_ON, 1, 0, 8)
    END IF

    IF LANG = 0 THEN s$ = "玩家一" ELSE s$ = "P1"
    ui_text_v(12, 11, s$, C_TEXT, 14, 0, 3, 0)
    i = 0
    WHILE i < wscore
        colr = C_PIP_OFF
        IF i < sc0 THEN
            colr = C_ANGLE
        END IF
        ui_circle(66 + i * 16, 20, 6, colr, 1, 0)
        i = i + 1
    WEND

    IF LANG = 0 THEN s$ = "玩家二" ELSE s$ = "P2"
    ui_text_v(sw - 12, 11, s$, C_TEXT, 14, 2, 3, 0)
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
    ' Wind: a track plus a small block that runs left and right (+2 at the far right, -2 at the far left)
    IF LANG = 0 THEN s$ = "风" ELSE s$ = "Wind"
    ui_text_v(cxc, 5, s$, C_DIM, 13, 1, 3, 0)
    ui_rect(cxc - 46, 27, 92, 8, C_TRACK, 1, 0, 4)
    ui_rect(cxc - 1, 25, 3, 12, &HFF6A6A8C, 1, 0, 0)
    ui_rect(cxc + wind * 17 - 5, 23, 10, 16, C_MARKER, 1, 0, 3)

    ' ── 底部操作区 ──
    ' ── Bottom control area ──
    ui_rect(0, panY, sw, panh, C_PANEL, 1, 0, 0)
    ui_rect(0, panY, sw, 2, &HFF2E2B45, 1, 0, 0)

    IF LANG = 0 THEN s$ = "角度" ELSE s$ = "Angle"
    ui_text_v(14, barAy + 8, s$, C_DIM, 14, 0, 3, 0)
    ui_rect(barX, barAy, barW, barH, C_TRACK, 1, 0, 6)
    ui_rect(barX, barAy, INT(barW * aimA / 90), barH, C_ANGLE, 1, 0, 6)
    drawNum(sw - 14, barAy + 6, aimA, C_TEXT, 16, 2)

    IF LANG = 0 THEN s$ = "力度" ELSE s$ = "Power"
    ui_text_v(14, barPy + 8, s$, C_DIM, 14, 0, 3, 0)
    ui_rect(barX, barPy, barW, barH, C_TRACK, 1, 0, 6)
    ui_rect(barX, barPy, INT(barW * aimP / 100), barH, C_POWER, 1, 0, 6)
    drawNum(sw - 14, barPy + 6, aimP, C_TEXT, 16, 2)

    ui_rect(14, fireY, sw - 28, fireH, C_FIRE, 1, 0, 8)
    IF st = 0 THEN
        IF LANG = 0 THEN s$ = "发 射" ELSE s$ = "FIRE"
        ui_text_v(cxc, fireY + 7, s$, C_FIRE_T, 18, 1, 3, 0)
    ELSE
        IF LANG = 0 THEN s$ = "飞 行 中" ELSE s$ = "IN FLIGHT"
        ui_text_v(cxc, fireY + 7, s$, C_FIRE_B, 18, 1, 3, 0)
    END IF

    ui_present()
END SUB

' 触摸 / 鼠标：按下的那一点落在哪根条上就改哪个值；发射键只在按下那一刻认
' Touch / mouse: whichever bar the pressed point lands on is the value that changes; the fire button only counts at the moment of the press
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
    ' The fire button only counts at the moment of the **press**; dragging across it does not (so adjusting the bars does not fire by accident)
    IF isDown = 1 THEN
        IF st = 0 THEN
            IF pty >= fireY THEN
                IF pty <= fireY + fireH THEN
                    IF ptx >= 14 THEN
                        IF ptx <= sw - 14 THEN
                            armBanana()
                            st = 1
                            sfxFire
                        END IF
                    END IF
                END IF
            END IF
        END IF
    END IF
END SUB

' 键盘：Esc / 返回退出；回车或手柄 A 也能发射（接了物理键盘/手柄也能玩）
' Keyboard: Esc / back exits; Enter or gamepad A also fires (so a physical keyboard/gamepad works too)
SUB handleKey()
    i = ui_msg_a()
    IF i = 27 THEN
        quit = 1
    END IF
    IF i = 13 THEN
        IF st = 0 THEN
            armBanana()
            st = 1
            sfxFire
        END IF
    END IF
    IF i = 65 THEN
        IF st = 0 THEN
            armBanana()
            st = 1
            sfxFire
        END IF
    END IF
END SUB

' ── 主循环 ─────────────────────────────────────────────────────────────
' ── Main loop ─────────────────────────────────────────────────────────────

' ══════════════════════════════════════════════════════════════════════════
'  音效：音色表
'  Sound: tone table
' ══════════════════════════════════════════════════════════════════════════
'
' **用 `ui_beep` 单音**（v0.96.509 统一换回来）。
' **Single-tone `ui_beep`** is used (switched back universally in v0.96.509).
'
' ⚠⚠ 这些音一度走共享库的音序器（`ui_sfx_add` / `ui_sfx_tick`），**真机上破音**，
' ⚠⚠ These sounds once went through the shared library's sequencer (`ui_sfx_add` / `ui_sfx_tick`) and **clipped audibly on real devices**,
'   全部换回来了。破音的是**这里配的音** —— 那一版同时踩了两条：
'     so they were all switched back. What clipped was **the sound configured here** — that version tripped two things at once:
'     · **多个声部同时响**：命中是 3 声部和弦、撞楼是三个低音一起轰，
'         · **Several voices sounding at once**: a hit was a 3-voice chord and a building hit was three low notes thundering together;
'       多声部混音一叠加，音量就顶到削波；
'             mixing several voices pushed the level up to clipping;
'     · **长音拖尾**：获胜那个末音拖了 12 拍（一拍 33ms ≈ 400ms）。
'         · **Long note tails**: the final note on a win dragged for 12 beats (one beat 33ms ≈ 400ms).
'   `ui_beep` 是**单通道**的（后一个音掐掉前一个）⇒ 一个事件永远只有一个音在响，
'     `ui_beep` is **single-channel** (the next note cuts the previous one) ⇒ one event only ever has one note sounding,
'   **结构上不可能削波、也不会长音叠加**。代价是没有和弦、没有音色 —— 对
'     so **clipping and long-note stacking are structurally impossible**. The cost is no chords and no timbre — good enough for
'   "打中/爆炸/胜负"这类**一次性提示音**够用。
'     **one-shot cues** like 「hit / explosion / win or lose」.
'
' ── 频率怎么定的（机械规则，别随手改，改了几处要一起改）─────────────────
' ── How the frequencies are chosen (a mechanical rule; do not change casually, and change the affected spots together) ─────────────────
'   · 取整块的**首音**（MIDI → Hz）；低音不低于 **C3(131Hz)** —— 手机外放在
'     · Take the **first note** of the whole block (MIDI → Hz); no note lower than **C3 (131Hz)** — a phone speaker rolls off
'     200Hz 以下衰减很快，36（C2=65Hz）出来是"噗"一声闷响，玩家听着像**没响**。
'         fast below 200Hz, so 36 (C2 = 65Hz) comes out as a muffled 「puff」 that players hear as **nothing at all**.
'   · **胜负取两端的极值**：赢取整块**最高音**。「不看屏幕也分得出输赢」就靠这个
'     · **Win and loss take the extremes at each end**: a win takes the **highest note** of the block. 「Tell win from loss without looking at the screen」 rests on this
'     —— 五子棋/象棋两版也是这么配的（赢 1320 / 输 240）。
'         — the gomoku/chess versions are configured the same way (win 1320 / loss 240).
'   · 时长 = 整块总时长（拍 × 33ms），封顶 320ms。
'     · Duration = the block's total duration (beats × 33ms), capped at 320ms.
'
' 音符号是真 MIDI 语义（中央 C = 60、A4 = 69 = 440Hz）—— 下面的 Hz 由它换算来。
' Note numbers use real MIDI semantics (middle C = 60, A4 = 69 = 440Hz) — the Hz values below are converted from them.

' 发射：短促的一记「嗖」（原来两个音快速下行；单音只留起手那一下）
' Fire: a short 「whoosh」 (it used to be two notes descending fast; with one tone only the opening hit is kept)
SUB sfxFire()
    ui_beep 698, 99        ' note 77
END SUB

' 命中得分：一声清亮的「叮」
' Hit and score: a bright 「ding」
SUB sfxHit()
    ui_beep 523, 264       ' note 72
END SUB

' 撞楼 / 落地：「轰」—— **最低的一档**
' Building hit / landing: a 「boom」 — **the lowest step**
SUB sfxGroundBoom()
    ui_beep 131, 231       ' note 48
END SUB


' 获胜：**最亮最高的那一档**（取整块最高音，见上面"胜负取两端"）
' Win: **the brightest, highest step** (the block's highest note; see 「win and loss take the extremes」 above)
SUB sfxWin()
    ui_beep 1047, 320      ' note 84（原上行 do–sol–do 的顶点）
    ' note 84 (the top of the original rising do–sol–do)
END SUB

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
    ' On a very short screen (rare) the gorilla band is pulled up,
    ' 免得楼顶落到地面以下 —— 那样 `ground - ty` 会变负数
    ' so a rooftop cannot end up at or below the ground — `ground - ty` would go negative
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

    IF LANG = 0 THEN tt$ = "大猩猩扔香蕉" ELSE tt$ = "Gorilla"
    IF LANG = 0 THEN bd$ = "两只大猩猩站在城市两头，轮流把香蕉扔到对面。拖「角度」和「力度」两根条调好，按「发射」。香蕉会被重力和风带着走 —— 风每回合变一次，看顶上那根风的指示。先拿满 3 分的人赢。" ELSE bd$ = "Two gorillas on the rooftops trade bananas. Drag the Angle and Power bars, then tap FIRE. Gravity and wind carry the shot - wind changes every round, watch the wind gauge at the top. First to 3 points wins."
    dlg = ui_dlg_msg(tt$, bd$, 0)
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
        ' Wait for one message, then drain **everything already queued** in one go:
        ' 一次滑动每秒能来几十条 TOUCHMOVE，一条一画的话重绘会被输入拖垮。
        ' one swipe can deliver dozens of TOUCHMOVE messages per second, and drawing one per message would let input drag the redraw down.
        ' 位置是幂等的（后一条覆盖前一条），所以只要把队列清空、然后画一帧就够。
        ' Position is idempotent (the later message overwrites the earlier one), so it is enough to empty the queue and then draw one frame.
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
                ' Endgame: draw one more frame with the final score first (the dialog covers the picture, so the player must see the freeze-frame)
                drawScene()
                sfxWin
                ' ⚠ 从前这里有一段"等 18 拍让胜利音放完再弹框"的循环 —— 那是给音序器
                ' ⚠ There used to be a loop here that 「waited 18 beats for the win sound to finish before showing the dialog」 — that was for the sequencer
                '   准备的（对话框**阻塞**，主循环一停，后面的音就永远等不到下一拍）。
                '     (the dialog **blocks**; once the main loop stops, later notes never get their next beat).
                '   现在胜利音是一声 `ui_beep`：它在**音频线程**上响完，与主循环无关，
                '     The win sound is now one `ui_beep`: it finishes on the **audio thread**, independently of the main loop,
                '   所以直接弹框即可。
                '     so the dialog can be shown right away.
                IF sc0 >= wscore THEN
                    IF LANG = 0 THEN bd$ = "玩家一 先拿满 3 分，赢了！再来一局？（选「否」退出）" ELSE bd$ = "Player 1 reached 3 first - wins! Play again? (choose No to quit)"
                    dlg = ui_dlg_msg(tt$, bd$, 0)
                ELSE
                    IF LANG = 0 THEN bd$ = "玩家二 先拿满 3 分，赢了！再来一局？（选「否」退出）" ELSE bd$ = "Player 2 reached 3 first - wins! Play again? (choose No to quit)"
                    dlg = ui_dlg_msg(tt$, bd$, 0)
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
                    ui_sfx_panic
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

    ' ⚠ **退出前必须静音**：声部是宿主的资源，进程退出前不关就会一直响下去
    ' ⚠ **Silence before exiting**: voices are a host resource; if they are not shut down before the process exits they keep sounding
    '   （手机上表现为"切回桌面还有声音"）。
    '     (on a phone this shows up as 「there is still sound after switching back to the home screen」).
    ui_sfx_panic
    ui_win_close()
END SUB

' ══════════════════════════════════════════════════════════════════════════
'  主程序
'  Main program
' ══════════════════════════════════════════════════════════════════════════

' CONST 的替身变量（SUB 里只用这些普通变量，见上面缺陷 ② 的第四条）
' Stand-in variables for CONSTs (the SUBs use only these ordinary variables; see the fourth item of defect ② above)
' 顶层是好的，所以算术放在这里做。
' The top level is fine, so the arithmetic is done here.
' 网格清零（正弦表随后装）—— 必须在最前面，newCity 要用它存楼高
' Clear the grid (the sine table is loaded right after) — this must come first, since newCity uses it to store building heights
' 界面语言：**开局查一次**存进 LANG（ui_get_language 是 syscall，别每帧调）
' UI language: **queried once at start-up** into LANG (ui_get_language is a syscall; do not call it every frame)
LANG = ui_get_language()
ui_gclear()
loadTrig()

' 开机自检：桌面脚手架（500–599 号全是空操作、没有真窗口）也能跑，
' Boot self-check: this also runs on the desktop scaffold (opcodes 500-599 are all no-ops and there is no real window),
' 这是**唯一**能在桌面上证明弹道是对的的手段（缺陷 ⑩）。
' and it is the **only** way to prove the ballistics correct on the desktop (defect ⑩).
physicsCheck()
simCheck()
simMode = 0

' 尺寸：先问设备，再开窗。**顺序照抄 gomoku.c** —— 排版用的那两个数
' Size: ask the device first, then open the window. **The order is copied from gomoku.c** — the two numbers used for layout
' 必须与交给窗口的那两个数**同源**，否则内容会画到画布外面。
' must be **the same source** as the two numbers handed to the window, otherwise content is drawn outside the canvas.
sw = ui_scr_w()
sh = ui_scr_h()
IF sw <= 0 THEN
    sw = 380
END IF
IF sh <= 0 THEN
    sh = 660
END IF

' 只支持竖屏 + 不要手柄区：
' Portrait only + no gamepad area:
'   · 竖屏 —— 城市是横着排的，转屏只会重排一次、玩家还得转回来，锁竖屏更省心；
'     · Portrait — the city is laid out sideways, so rotating would only force one re-layout and the player would have to rotate back; locking portrait is simpler;
'   · 不要手柄区 —— 全程触摸（两根条 + 一个发射键），手柄一个都用不到，
'     · No gamepad area — the game is all touch (two bars + one fire button) and uses no gamepad at all,
'     留着那一整块等于白吃掉一百多像素的画面高度（与 gomoku.c 同一处置）。
'         so keeping that whole block would waste over a hundred pixels of screen height for nothing (the same treatment as gomoku.c).
'   两个常量在这里写字面量：BASIC 侧没有 C 头文件那套宏，
'     The two constants are written as literals here: the BASIC side has no C-header macro set,
'   第 4 个 0 = VML_WIN_PORTRAIT、第 5 个 0 = VML_WIN_NO_GAMEPAD（见 waycoder_ui.h）。
'     the 4th 0 = VML_WIN_PORTRAIT and the 5th 0 = VML_WIN_NO_GAMEPAD (see waycoder_ui.h).
IF LANG = 0 THEN tt$ = "大猩猩扔香蕉" ELSE tt$ = "Gorilla"
wh = ui_win_open_ex(tt$, sw, sh, 0, 0)

' 宿主把窗口开出来了（返回 1）才开跑；桌面脚手架这里返回 0 —— 上面自检已打完，
' Only run once the host has opened the window (returns 1); the desktop scaffold returns 0 here — the self-check above has finished,
' 直接收工。**别**把这条判断当"平台探测"去别处复用，它只说明"这一轮有没有真窗口"。
' so just stop. **Do not** reuse this test elsewhere as 「platform detection」; it only says whether this run has a real window.
IF wh < 1 THEN
    IF LANG = 0 THEN PRINT "（桌面脚手架：ui_* 号段是空操作、没有真窗口，弹道自检打完就退出）" ELSE PRINT "(desktop scaffold: ui_* opcodes are no-ops, no real window; ballistic self-check done, exiting)"
ELSE
    runGame()
END IF
