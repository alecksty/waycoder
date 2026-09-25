' ══════════════════════════════════════════════════════════════════════════
'  gorilla_pro.bas —— 大猩猩扔香蕉 · **PRO 版**（手机端 VML / BASIC 前端）
'
'  与 gorilla.bas 的关系：**玩法、物理、手感数值一字未动**，
'  这一版只做画面 —— 昼夜循环、日月运行、天空/城市随光变、香蕉是香蕉的形状。
'  旧版**保留**（`gorilla.bas`），两版并存，想玩哪版玩哪版。
'
'  ## 这一版加的东西
'
'   · **游戏时钟**：1 真实秒 = 1 游戏分钟 ⇒ 24 真实分钟走完一天（用户定）。
'     走的是**独立的一秒定时器**（消息 A 是定时器 id，见 runGame 里那个分支），
'     不是"数帧" —— 数帧的话玩家拖动滑条时的连发消息会把时钟一起带快。
'   · **太阳 / 月亮**按游戏时间沿同一条弧线走（6:00 升、12:00 顶、18:00 落；
'     月亮差 12 小时）。弧线高度用 `SIN` 算，位置按时钟线性插值。
'   · **天空**是整块渐变（`ui_gradient` + `ui_rect_grad`），
'     天顶/地平线两个颜色随「太阳高度」在夜色与日色之间插值；
'     日出日落时地平线**偏向橙色**（暖色系数在太阳低的时候最大）。
'   · **白天有云**（几团椭圆，随游戏时间缓慢飘、出画环绕），**夜里有星星**
'     （亮点 + 四角星两种，按游戏时间闪烁）。两者都按天光系数淡入淡出 ——
'     云在天光为 0 时正好等于天空色（自然消失），不需要"该不该画"的开关。
'   · **楼房白天鲜艳、夜里压暗**：每栋一个色相（6 色调色板），
'     夜色 = 同一色相压到 16%，按天光系数插值 —— 所以夜里仍认得出是哪栋楼。
'     另加受光面高光条、檐口、门、带边框的窗户（夜里点灯率高、灯是暖黄的），
'     以及按 `bi % 3` 分的屋顶细节（水箱 / 天线 / 平的）。
'   · **香蕉是香蕉**：开机把一段月牙（`ui_path`，两段三次贝塞尔）录成**图块**
'     （`ui_create_block`/`ui_end_block`），飞行时按速度方向 `ui_draw_block` 旋转贴出。
'
'  ## PRO 版依赖的前端能力（都**实测过**，不是照抄旧注释）
'
'  旧版头部那一长串「BASIC 前端缺陷」**大半已经修好了**，
'  动手前用 `.scratch/probe_arith.bas` / `probe2.bas` / `probe3.bas` 逐条复验过，实测：
'
'   · `\` / `MOD` **在 SUB 里已经是对的**（旧注释说编不出来）
'   · 带括号的复合子表达式**是对的**（`1+(2*3)` = 7）
'   · CONST 参与算术**是对的**（`KA-1` = 99、`KA*2` = 200）
'   · SUB 条件里的 `AND` **是对的**
'   · 大整数乘除**是对的**（`70000*3` = 210000；本文件里最大到 4×10⁵，实测无误）
'   · **`SIN` / `COS` / `SQR` 是对的**：`SIN(30)` = 5000、`SIN(180)` = 0、`COS(60)` = 5000
'     —— **度数制、返回值 ×10000**（旧注释说一律返回 0，也过期了）
'   · **字符串拼接是对的**（`"ab" + "cd"` = `"abcd"`）
'   · **数组（`DIM a(10)`）是对的**，各下标互不干扰 —— 本文件用它存调色板与云
'   · **颜色可以逐通道算出来再打包**：`&HFF000000 + r*65536 + g*256 + b` 与字面量
'     `&HFFrrggbb` **逐位相等**（实测 `= 1`）。⚠ 所以**只打包、不解包** ——
'     反解要处理 0xFF 前缀带来的负数，而"分量各自算完再打包"根本不需要反解。
'
'  仍然**有意避开**的两条（没验、也不值得为它冒险）：
'   · 形参是字符串的 SUB（旧注释说读到垃圾）⇒ 本文件所有子过程形参全是整数，
'     字符串一律在子过程里写字面量。
'   · 主程序里写在 `SUB ... END SUB` **之后**的赋值语句 ⇒ 本文件的赋值一律在 SUB 之前。
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
'  ## 旧版头部那些「BASIC 前端缺陷」清单：**别照抄过来**
'
'  gorilla.bas 头部记着十来条前端缺陷（`\`/MOD 编不出、括号算错、CONST 参与算术出错、
'  `AND` 恒假、大整数不可靠、数组不能用、字符串拼接得空串、`SIN`/`COS`/`SQR` 一律返回 0…），
'  每条都附了最小复现。**这些在今天的编译链上大半已经不成立了** ——
'  本文件开头那段「PRO 版依赖的前端能力」就是逐条复验的结果（`.scratch/probe*.bas`）。
'  留着那串旧注释会让人按已经不存在的约束去写代码（我就差点又去手搓一张正弦表）。
'  ⇒ **本文件按"现代"前端写**；旧版 gorilla.bas 原样保留，它的注释是**写它那个年代**的实况。
'
'  仍然照旧遵守的两条（本次没验、也不打算验）：
'    · 主程序里的**赋值**一律写在所有 SUB 之前（调用可以放后面）；
'    · SUB 的形参不用字符串。
'
'  ## 自检（缺陷 ⑩ 的现代版）
'
'  桌面脚手架（vmlcli）把 500–599 号全当空操作、也开不出真窗口
'  （`ui_win_open_ex` 返回 0），所以**画面对不对在桌面上看不出来**。
'  能在桌面上证明的只有**数**：本文件的 `physicsCheck` / `simCheck`（弹道）
'  与 `skyCheck`（昼夜：天光系数、日月位置、天空色）都开机就跑、把数打出来 ——
'  光看编译绿灯什么也证明不了。
' ══════════════════════════════════════════════════════════════════════════
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
' ⚠ 用**带竖对齐**的版本：本游戏的 y 一律按**顶线**给（老契约）。
'   v0.96.394 把 ui_text 的 y 统一成了**基线**，于是所有文字整体上移约 0.8×字号
'   （实测：y=100/字号20 的字，墨迹落在 88..100 ⇒ 全在 y 上方）。
'   3 = VML_VANCHOR_TOP（盒顶落在 y）⇒ 恢复老行为。
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

' ── PRO 版另外要用的绘图接口（签名照抄 `Lib/shared/src/vmlui.c`）──────
NATIVE SUB ui_ellipse(cx AS INTEGER, cy AS INTEGER, rx AS INTEGER, ry AS INTEGER, c AS INTEGER, f AS INTEGER, lw AS INTEGER)
END SUB
' 渐变：几何是**千分之一的整数**（0..1000），线性版 a1a2 = 起点、a3a4 = 终点。
' ⚠ 两端都别自己再除一次 1000（本仓记过"两端各写一次换算"的血账）。
NATIVE SUB ui_gradient(gid AS STRING, radial AS INTEGER, ca AS INTEGER, cb AS INTEGER, a1 AS INTEGER, a2 AS INTEGER, a3 AS INTEGER, a4 AS INTEGER)
END SUB
NATIVE SUB ui_rect_grad(x AS INTEGER, y AS INTEGER, w AS INTEGER, h AS INTEGER, gid AS STRING, rr AS INTEGER)
END SUB
NATIVE SUB ui_circle_grad(cx AS INTEGER, cy AS INTEGER, rr AS INTEGER, gid AS STRING)
END SUB
' 四角星（`ui_draw_star(cx,cy,外半径,内半径,角数,旋转)`）—— 夜里的大星星用它
NATIVE SUB ui_draw_star(cx AS INTEGER, cy AS INTEGER, ro AS INTEGER, ri AS INTEGER, pn AS INTEGER, rot AS INTEGER)
END SUB
' SVG 路径。fill 是填充色、gid 是渐变名（不给就传空串）
NATIVE SUB ui_path(d AS STRING, stroke AS INTEGER, width AS INTEGER, fill AS INTEGER, gid AS STRING, cap AS INTEGER, dash AS INTEGER)
END SUB
' 图块：create 之后画的都录进块里，end 收尾并**返回句柄**；
'  draw_block 把块贴到 (x,y)（**中心**），sx/sy 是千分比缩放、rot 是**度**。
NATIVE FUNCTION ui_create_block(w AS INTEGER, h AS INTEGER, c AS INTEGER) AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_end_block() AS INTEGER
END FUNCTION
NATIVE SUB ui_draw_block(blk AS INTEGER, x AS INTEGER, y AS INTEGER, sx AS INTEGER, sy AS INTEGER, rot AS INTEGER)
END SUB
' **释放**图块（#593）：块的内容/尺寸都不可改，"更新一个块"只能重录 ——
' 不释放就是在漏句柄（块表 128 格，满了之后 create 返回 0、画面悄悄退回逐帧画）。
NATIVE FUNCTION ui_free_block(blk AS INTEGER) AS INTEGER
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
CONST PACE_MS = 5         ' 主循环最长睡眠。**帧周期 ≈ 每帧工作量 + 这个数**（等待与绘制是相加的）：
                          '   原来 40 ⇒ 就算一帧算得再快也只有 25fps 上限；真机上量到"每帧 432 图元、
                          '   宿主只花 3ms、实际 17fps"之后，两处一起改：图元砍到 190 以下（城市/星空/云
                          '   各录成图块）+ 这个数放到 15 ⇒ 目标 25fps 以上。
                          '   ⚠ 它是"最长睡眠"，不是硬节拍：有消息（触摸/定时器）时立刻返回，
                          '     所以拖滑条时不受它限制，只有在真空闲时才按它走。
                          '   再放到 5：真机上量到"宿主 3ms、实际 20fps、帧周期 ≈ 43ms"
                          '   ⇒ 剩下的 28ms 是 VM 跑游戏自己的 BASIC（不是睡眠），
                          '   把睡眠从 15 压到 5 只是把这 10ms 让出来，让帧率贴到 VM 的真实产帧速度。
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
' 大猩猩配色。两只靠**色相**分开，不靠深浅 —— 用户报过「蓝色猴子白天看不清，
' 和天空一样了」，所以选色的判据是**跟天空（蓝）和楼群都要分得开**：
' 一只暖橙、一只紫，白天夜里都立得住。每只再配亮面（口鼻/肚子）与暗面（描边/手脚）。
CONST C_APE0 = &HFFE8A33D
CONST C_APE0L = &HFFF4CE8C
CONST C_APE0D = &HFFA96B1E
CONST C_APE1 = &HFFC758D6
CONST C_APE1L = &HFFE9A6F0
CONST C_APE1D = &HFF7A2A87

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

' ── PRO 版：游戏时钟 / 昼夜 ───────────────────────────────────────────
DIM ghour AS INTEGER         ' 游戏时（0..23）
DIM gmin AS INTEGER          ' 游戏分（0..59）
DIM totMin AS INTEGER
DIM sysMin AS INTEGER        ' 系统分（起始时钟用）
DIM sysSec AS INTEGER        ' 系统秒        ' 开局以来的游戏分钟总数（云的漂移用它，不随复位回绕）
DIM ctid AS INTEGER          ' **时钟定时器**的 id —— 与飞行/待机那个 tid 是两只表
DIM gmins AS INTEGER
DIM mm AS INTEGER
DIM sunUp AS INTEGER
DIM moonUp AS INTEGER
DIM dayL AS INTEGER          ' 天光系数 0..100（= 太阳高度，0 = 夜）
DIM warm AS INTEGER          ' 日出/日落的地平线暖色系数 0..100
DIM w2 AS INTEGER
DIM tp AS INTEGER
DIM tp2 AS INTEGER
DIM arcX0 AS INTEGER
DIM arcW AS INTEGER
DIM arcBot AS INTEGER
DIM arcH AS INTEGER
DIM sunX AS INTEGER
DIM sunY AS INTEGER
DIM moonX AS INTEGER
DIM moonY AS INTEGER

' 天空色**按分量存**，用时现打包成 `&HFFrrggbb`。
' ⚠ **只打包、不解包**：0xFF 打头的颜色在 int32 里是负数，反解要处理符号，
'   而"分量各自算完再打包"根本不需要反解（打包与字面量逐位相等，已实测）。
DIM zenR AS INTEGER
DIM zenG AS INTEGER
DIM zenB AS INTEGER
DIM horR AS INTEGER
DIM horG AS INTEGER
DIM horB AS INTEGER
DIM zen AS INTEGER
DIM hor AS INTEGER
DIM colM AS INTEGER
DIM mixR AS INTEGER
DIM mixG AS INTEGER
DIM mixB AS INTEGER

' 楼房：6 个色相（白天用；夜里 = 同色相压到 16%）
DIM bldR(6)
DIM bldG(6)
DIM bldB(6)

' 云：ncloud 团，每团的基准位置 / 大小百分比 / 漂移速度
DIM ncloud AS INTEGER
DIM clx(8)
DIM cly(8)
DIM csz(8)
DIM cld(8)
DIM cxp AS INTEGER
DIM cyp AS INTEGER
DIM csize AS INTEGER

' 香蕉：图块句柄 + 方向角 + tan 表
DIM bid AS INTEGER
DIM cid AS INTEGER          ' 城市层图块句柄（窗户有三百多个矩形，逐帧重画是帧率杀手）
DIM cityDirty AS INTEGER    ' 城市图块要不要重录
DIM lastMin AS INTEGER      ' 保留：不再用分钟判重录（理由见 buildCityBlock 的注释）
DIM sid AS INTEGER          ' 星空图块句柄
DIM qid AS INTEGER          ' 云图块句柄
DIM skyMin AS INTEGER       ' 上一次录天空图块时的游戏分钟
DIM cityDayL AS INTEGER     ' 上一次录城市图块时的天光
DIM bang AS INTEGER
DIM spin AS INTEGER        ' 飞行中的自转累计角（度）—— 见 bananaAngle/drawBanana
DIM tanT(91)
DIM ax AS INTEGER
DIM ay AS INTEGER
DIM ratio AS INTEGER
DIM best AS INTEGER
DIM df AS INTEGER
DIM k AS INTEGER
DIM tr AS INTEGER
DIM tc AS INTEGER

' 星星闪烁 / 通用临时量
DIM night AS INTEGER
DIM stx AS INTEGER
DIM sty AS INTEGER
DIM sw2 AS INTEGER
DIM litN AS INTEGER
DIM hlist AS INTEGER
DIM ph AS INTEGER
DIM tw AS INTEGER
DIM f AS INTEGER
DIM s AS INTEGER
DIM j AS INTEGER
DIM r3 AS INTEGER
DIM sx AS INTEGER
DIM poleC AS INTEGER
DIM treeC AS INTEGER
DIM nightF AS INTEGER
' ── 天上飞的（小鸟 / 飞碟 / 飞机）—— 被香蕉砸中就在空中炸，这一发白扔 ──
DIM flyOn AS INTEGER        ' 场上有没有
DIM flyKind AS INTEGER      ' 1 鸟 / 2 飞碟 / 3 飞机
DIM flyX AS INTEGER
DIM flyY AS INTEGER
DIM flyV AS INTEGER         ' 每拍位移（带符号；待机拍是飞行拍的 4 倍，见 moveFlyer）
DIM flyR AS INTEGER         ' 命中半径（方形盒）
DIM fdx AS INTEGER
DIM fdy AS INTEGER
DIM apeC AS INTEGER
DIM apeL AS INTEGER
DIM apeD AS INTEGER
DIM flyC AS INTEGER
DIM flyT AS INTEGER        ' 已飞了多久（单位=飞行拍，30ms）
DIM flyK AS INTEGER        ' 这一拍算几拍（待机 4 / 飞行 1）
DIM flyWob AS INTEGER      ' 小鸟本段的高度增量 -1/0/1
DIM flyWobT AS INTEGER     ' 上次重掷高度增量的时刻
DIM flyRev AS INTEGER      ' 小鸟还剩几次掉头机会
DIM flyRevT AS INTEGER     ' 上次掉头的时刻
DIM flyTx AS INTEGER       ' 飞碟的悬停点 x
DIM flyPhase AS INTEGER    ' 飞碟阶段：0 飞来 / 1 悬停 / 2 飞走
DIM flyHold AS INTEGER     ' 飞碟已悬停多少拍
DIM planeT AS INTEGER      ' "定期飞机"的计时器
' ── 流星（只在夜里，偶尔一颗；纯装饰，**不参与碰撞**）──
DIM metOn AS INTEGER
DIM metX AS INTEGER
DIM metY AS INTEGER
DIM metVX AS INTEGER
DIM metVY AS INTEGER
DIM metT AS INTEGER        ' 活了多久（单位=飞行拍）
DIM metWait AS INTEGER     ' 距离下次掷签
DIM metC AS INTEGER
DIM br AS INTEGER
DIM bg AS INTEGER
DIM bb AS INTEGER
DIM bodyC AS INTEGER
DIM hiC AS INTEGER
DIM litC AS INTEGER
DIM offC AS INTEGER
DIM frmC AS INTEGER
DIM gndC AS INTEGER

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

' ── PRO 版：开机装表（顶层，算术随便用）─────────────────────────────
'
' 楼房色相（白天）。夜里的那份不另存 —— 按 16% 压暗即可（见 drawBuilding），
' 这样"夜里是哪栋楼"仍然认得出，而不用维护第二张表（本仓头号坑是平行表）。
bldR(0) = 232
bldG(0) = 115
bldB(0) = 90
bldR(1) = 79
bldG(1) = 179
bldB(1) = 166
bldR(2) = 224
bldG(2) = 178
bldB(2) = 60
bldR(3) = 142
bldG(3) = 123
bldB(3) = 216
bldR(4) = 94
bldG(4) = 156
bldB(4) = 88
bldR(5) = 91
bldG(5) = 143
bldB(5) = 214

' 云：固定几团（位置/大小/漂移都不随机 —— 随机的话每次进游戏云的疏密差很多，
' 而它纯粹是背景，不该有"这局云真少"这种运气成分）
ncloud = 4
clx(0) = 40
cly(0) = hudh + 52
csz(0) = 100
cld(0) = 5
clx(1) = 210
cly(1) = hudh + 96
csz(1) = 74
cld(1) = 3
clx(2) = 130
cly(2) = hudh + 150
csz(2) = 58
cld(2) = 8
clx(3) = 300
cly(3) = hudh + 34
csz(3) = 84
cld(3) = 4

' tan 表（千分之一）：香蕉的朝向按速度方向查它。
' 现在 `SIN`/`COS` 是好的（实测 SIN(30)=5000），所以这张表可以**算出来** ——
' 旧版那张 sin 表是手抄的死数据，抄错一格没人看得出来。
k = 0
WHILE k < 90
    tanT(k) = INT(SIN(k) * 1000 / COS(k))
    k = k + 1
WEND
tanT(90) = 999999        ' tan90 无穷大：用一个大数代表"竖直"，查表时自然总被选中

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
    flyOn = 0                ' 自检只验弹道：天上飞的东西会让它误判成"打中了"
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
    cityDirty = 1        ' 楼高/配色都变了 ⇒ 城市图块要重录
    st = 0
    spawnFlyer()
    armBanana()
END SUB

' 把香蕉放回当前玩家的手上（大猩猩头顶再高一点，免得一起手就砸自家楼顶）
SUB armBanana()
    aimAngles()
    aimSpeed()
    spin = 0                  ' 每一发重新开始翻
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
    ' 翻跟头：**按物理步**累加（不是按帧）—— 按帧的话同一发在快机/慢机上转的圈数不一样。
    ' 18°/步 × 三四十步 ≈ 两圈，用户要的"幅度大"就是这个。
    spin = spin + 18
    IF spin >= 360 THEN
        spin = spin - 360
    END IF

    tx = INT(bx / S)
    ty = INT(by / S)
    ended = 0
    hitFlag = 0
    hitBld = 0

    ' **空中拦截**：撞上小鸟/飞碟/飞机 ⇒ 空中爆炸，这一发白扔（不得分、直接换人）。
    ' 判定放在楼与猿之前 —— 它们飞在楼**前面**，视觉上先撞到的就是它。
    '
    ' ⚠ 这里**必须 EXIT SUB 就地返回**，不能让流程落到下面去：
    '   末尾那句 `IF ended = 1 THEN ebx = tx / eby = ty` 会把爆炸点改写成**香蕉自己**的位置，
    '   而且撞地那一段还会顺手 `addHole` —— 于是"空中炸掉一只鸟"会变成
    '   "在香蕉位置炸出一个洞、楼也被扣掉一块"。第一版就踩了这个（爆炸画在发射手上）。
    IF flyOn = 1 THEN
        fdx = tx - flyX
        IF fdx < 0 THEN
            fdx = 0 - fdx
        END IF
        fdy = ty - flyY
        IF fdy < 0 THEN
            fdy = 0 - fdy
        END IF
        IF fdx < flyR THEN
            IF fdy < flyR THEN
                ebx = flyX
                eby = flyY
                hitFlag = 0          ' 不得分：这是"浪费一个香蕉"
                flyOn = 0
                st = 2
                boomT = 0
                IF simMode = 0 THEN
                    ui_beep(220, 70) ' 比撞地那声（280）低，一听就知道是打到东西了
                    ui_vibrate(35, 90)
                END IF
                EXIT SUB
            END IF
        END IF
    END IF

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
            spawnFlyer()          ' 新回合重新掷一次"天上有没有东西飞过"
            armBanana()
        END IF
    END IF
END SUB

' 一栋楼（含窗户）。窗户的明暗按 (楼号, 列, 行) 算出来的确定性图案 ——
' 不能用随机数：drawScene 每帧都跑，随机会变成满屏闪烁。
' 相位用减法计数拿，不用 MOD（缺陷 ②）。
' ══════════════════════════════════════════════════════════════════════════
'  PRO：昼夜
' ══════════════════════════════════════════════════════════════════════════

' 起始时钟 —— **按系统时钟算**（用户定）：
'   游戏「时」= 系统「分」**mod 24**、游戏「分」= 系统「秒」。
'   例：系统 10:35:20 ⇒ 游戏 11:20（35 ≥ 24 取余）；系统 22:50:40 ⇒ 游戏 2:40。
' ⇒ 每局开局的昼夜**都不一样**，不再固定从 8:00 的早上开始。
' ⚠ 取值用 `TIME$`（返回 "HH:MM:SS"），**不是 `TIMER`** —— 后者在本平台恒为 0（实测）。
SUB initClock()
    t$ = TIME$
    sysMin = VAL(MID$(t$, 4, 2))
    sysSec = VAL(MID$(t$, 7, 2))
    ' ⚠ 取余要**反复减到 < 24**：只减一次的话 48..59 分会剩下 24..35
    '   （实测：系统 10:50 打出「26:38」，26 时已经越界，日月位置跟着算飞）。
    '   现在 `MOD` 是好用的（见文件头的能力清单），直接用它。
    ghour = sysMin MOD 24
    gmin = sysSec
END SUB

' 时钟走一格。**1 真实秒 = 1 游戏分钟** ⇒ 24 真实分钟走完一天（用户定）。
'
' 为什么单独一只定时器（而不是"每画一帧加一点"）：主循环在玩家拖动滑条时
' 会一次抽干几十条 TOUCHMOVE，帧数**取决于输入有多密** —— 拿帧数当时钟，
' 滑得越勤时钟跑得越快。定时器消息才是与画面无关的时间。
SUB tickClock()
    totMin = totMin + 1
    gmin = gmin + 1
    IF gmin >= 60 THEN
        gmin = 0
        ghour = ghour + 1
        IF ghour >= 24 THEN
            ghour = 0
        END IF
    END IF
END SUB

' 算这一帧的：天光系数、日月位置。
' 太阳 6:00 升、12:00 顶、18:00 落；月亮把时钟拨 12 小时走同一条弧。
SUB skyClock()
    gmins = ghour * 60 + gmin

    sunUp = 0
    tp = 0
    IF gmins >= 360 THEN
        IF gmins <= 1080 THEN
            sunUp = 1
            tp = INT((gmins - 360) * 1000 / 720)          ' 0..1000
        END IF
    END IF
    dayL = 0
    IF sunUp = 1 THEN
        ' 高度角 → 0..100，再**提亮 1.4 倍**（封顶 100）。
        ' 不提的话 `SIN` 的曲线让"看着像白天"只有 9:00–15:00 那一小段
        ' （8:00 才 48%、16:00 就剩 51%）—— 而玩家 24 分钟才看完一天，
        ' 白天的观感被压缩到中间那几帧很不划算。1.4 倍之后大约 7:00–17:00 都是满亮。
        dayL = INT(SIN(INT(tp * 180 / 1000)) * 14 / 1000)
        IF dayL > 100 THEN
            dayL = 100
        END IF
    END IF
    sunX = arcX0 + INT(arcW * tp / 1000)
    sunY = arcBot - INT(arcH * SIN(INT(tp * 180 / 1000)) / 10000)

    mm = gmins + 720
    IF mm >= 1440 THEN
        mm = mm - 1440
    END IF
    moonUp = 0
    tp2 = 0
    IF mm >= 360 THEN
        IF mm <= 1080 THEN
            moonUp = 1
            tp2 = INT((mm - 360) * 1000 / 720)
        END IF
    END IF
    moonX = arcX0 + INT(arcW * tp2 / 1000)
    moonY = arcBot - INT(arcH * SIN(INT(tp2 * 180 / 1000)) / 10000)
END SUB

' 两个颜色按 f%（0..100）插值：A(nr,ng,nb) → B(dr,dg,db)。
' 结果三通道进 mixR/G/B、打包色进 colM。
'
' ⚠ **只打包、不解包**（见变量区那段）：0xFF 打头的颜色是负数，
'   反解一个"颜色变量"要先处理符号，而分量各自算完再打包根本不需要反解。
SUB mix2(nr AS INTEGER, ng AS INTEGER, nb AS INTEGER, dr AS INTEGER, dg AS INTEGER, db AS INTEGER, f AS INTEGER)
    mixR = nr + INT((dr - nr) * f / 100)
    mixG = ng + INT((dg - ng) * f / 100)
    mixB = nb + INT((db - nb) * f / 100)
    colM = &HFF000000 + mixR * 65536 + mixG * 256 + mixB
END SUB

' 某个 y 处的天空色（与天空渐变**同一口径**：天顶→地平线线性）。
' 光晕、月亮、云、弹坑都要用它 —— 天空是渐变之后，
' 任何"用天空色盖一下"的地方都得问一句"哪个 y 的天空色"。
SUB skyAt(y AS INTEGER)
    f = INT(y * 100 / ground)
    IF f < 0 THEN
        f = 0
    END IF
    IF f > 100 THEN
        f = 100
    END IF
    mixR = zenR + INT((horR - zenR) * f / 100)
    mixG = zenG + INT((horG - zenG) * f / 100)
    mixB = zenB + INT((horB - zenB) * f / 100)
    colM = &HFF000000 + mixR * 65536 + mixG * 256 + mixB
END SUB

' 天顶 + 地平线两个颜色：按天光在「夜」「昼」之间插值，
' 再按"日出/日落窗口"给地平线加暖色。
' ⚠ 与 `drawSky` 分开，是因为 `ui_clear(底色)` 要在这之前用上当天色 ——
'   合成一个的话第一帧会拿上一次的色（或者 0）去清屏。
SUB skyPal()
    ' 天顶：夜(7,10,24) → 昼(46,111,208)
    zenR = 7 + INT((46 - 7) * dayL / 100)
    zenG = 10 + INT((111 - 10) * dayL / 100)
    zenB = 24 + INT((208 - 24) * dayL / 100)
    ' 地平线：夜(18,19,42) → 昼(168,216,245)
    horR = 18 + INT((168 - 18) * dayL / 100)
    horG = 19 + INT((216 - 19) * dayL / 100)
    horB = 42 + INT((245 - 42) * dayL / 100)

    ' 暖色窗口：以 6:00 与 18:00 为中心各一个 ±60 分钟的三角窗。
    ' ⚠ **不能用 dayL 算暖色**：dayL 在日落那一刻直接归 0，
    '   而"0 ⇒ 最暖"会让地平线在 18:00 整从橙**跳**回蓝（实测过，肉眼可见的一跳）。
    '   按**时钟**给窗、窗口重叠取大，黄昏的余晖就是连续收尾的。
    warm = 0
    IF gmins >= 300 THEN
        IF gmins <= 420 THEN
            warm = 100 - INT(ABS(gmins - 360) * 100 / 60)
        END IF
    END IF
    IF gmins >= 1020 THEN
        IF gmins <= 1140 THEN
            w2 = 100 - INT(ABS(gmins - 1080) * 100 / 60)
            IF w2 > warm THEN
                warm = w2
            END IF
        END IF
    END IF
    horR = horR + INT((236 - horR) * warm / 100)
    horG = horG + INT((126 - horG) * warm / 100)
    horB = horB + INT((64 - horB) * warm / 100)

    zen = &HFF000000 + zenR * 65536 + zenG * 256 + zenB
    hor = &HFF000000 + horR * 65536 + horG * 256 + horB
END SUB

' 把当天那份天空铺出来（颜色已由 `skyPal` 算好）
SUB drawSky()
    ui_gradient("sky", 0, zen, hor, 0, 0, 0, 1000)
    ui_rect_grad(0, 0, sw, ground, "sky", 0)
END SUB

' 太阳 / 月亮。都带一圈"朝当地天空色淡出"的光晕。
SUB drawSunMoon()
    IF sunUp = 1 THEN
        skyAt(sunY)
        ' 径向渐变：中心日色 → 外缘 = 当地天空色（这样光晕不会在渐变天上留一个色斑）
        ui_gradient("sunglow", 1, &HFFFFE9A8, colM, 500, 500, 500)
        ui_circle_grad(sunX, sunY, 34, "sunglow")
        ui_circle(sunX, sunY, 11, &HFFFFF0B4, 1, 0)
        ui_circle(sunX - 3, sunY - 3, 4, &HFFFFFCE6, 1, 0)
    END IF
    IF moonUp = 1 THEN
        skyAt(moonY)
        ui_gradient("moonglow", 1, &HFFEDE9D6, colM, 500, 500, 500)
        ui_circle_grad(moonX, moonY, 26, "moonglow")
        ui_circle(moonX, moonY, 14, &HFFF4F0DE, 1, 0)
        ' 环形山（三个暗一点的小圆）—— 没有它们月亮就是一个白饼
        ui_circle(moonX - 5, moonY - 4, 3, &HFFDCD6BE, 1, 0)
        ui_circle(moonX + 4, moonY + 2, 2, &HFFDCD6BE, 1, 0)
        ui_circle(moonX + 1, moonY - 7, 2, &HFFE4DEC6, 1, 0)
    END IF
END SUB

' 云：几团椭圆拼一朵，随游戏时间慢慢飘、出画从左边回来。
'
' 可见度**跟着天光走**：天光 0 时云正好等于当地天空色 ⇒ 自然消失。
' 这样就不用写"天黑该不该画云"那个判断 —— 少一个能写错的开关。
SUB drawClouds()
    i = 0
    WHILE i < ncloud
        cxp = clx(i) + totMin * cld(i)
        cxp = cxp - INT(cxp / sw2) * sw2 - 90
        cyp = cly(i)
        ' 出屏的云**在程序这边就跳过**：宿主确实会丢掉完全在屏外的图元，
        ' 但那要先把 syscall 走完 —— 省的是 VM 那一侧的开销（每朵云 5 个椭圆）。
        ' 出屏的云**在程序这边就跳过**：宿主确实会丢掉完全在屏外的图元，
        ' 但那要先把 syscall 走完 —— 省的是 VM 那一侧的开销（每朵云 5 个椭圆）。
        IF cxp >= -70 THEN
            IF cxp <= sw + 70 THEN
                skyAt(cyp)
                mix2(mixR, mixG, mixB, 252, 253, 255, INT(dayL * 86 / 100))
                csize = csz(i)
                ui_ellipse(cxp, cyp, INT(26 * csize / 100), INT(11 * csize / 100), colM, 1, 0)
                ui_ellipse(cxp - INT(20 * csize / 100), cyp + INT(4 * csize / 100), INT(14 * csize / 100), INT(8 * csize / 100), colM, 1, 0)
                ui_ellipse(cxp + INT(20 * csize / 100), cyp + INT(5 * csize / 100), INT(16 * csize / 100), INT(9 * csize / 100), colM, 1, 0)
                ui_ellipse(cxp - INT(8 * csize / 100), cyp - INT(7 * csize / 100), INT(15 * csize / 100), INT(10 * csize / 100), colM, 1, 0)
                ui_ellipse(cxp + INT(9 * csize / 100), cyp - INT(6 * csize / 100), INT(17 * csize / 100), INT(11 * csize / 100), colM, 1, 0)
            END IF
        END IF
        i = i + 1
    WEND
END SUB

' 星星：按天光淡出（白天看不见），按游戏时间闪烁（三角波）。
' 每三颗里有一颗是"亮星"（十字星芒），其余是小方点 —— 一整片一样大反而假。
SUB drawStars()
    ' 可见度按**平方**衰减：线性的话 dayL=60 还剩四成 ⇒ 大白天满天的星星。
    ' 平方之后 dayL=60 只剩 16%、dayL=80 只剩 4%，天一亮就干净了。
    night = 100 - dayL
    night = INT(night * night / 100)
    IF night > 0 THEN
        i = 0
        WHILE i < nstar
            stx = ui_gget(gstarx + i)
            sty = ui_gget(gstary + i)
            skyAt(sty)
            ' 每颗星一个相位（i*37），周期 60 游戏分钟
            ph = i * 37 + totMin
            ph = ph - INT(ph / 60) * 60
            tw = 55 + INT(ABS(30 - ph) * 45 / 30)
            mix2(mixR, mixG, mixB, 255, 255, 255, INT(night * tw / 100))
            j = i
            WHILE j >= 3
                j = j - 3
            WEND
            IF j = 0 THEN
                ui_rect(stx - 3, sty, 7, 1, colM, 1, 0, 0)
                ui_rect(stx, sty - 3, 1, 7, colM, 1, 0, 0)
                ui_rect(stx - 1, sty - 1, 3, 3, colM, 1, 0, 0)
            ELSE
                ui_rect(stx, sty, 2, 2, colM, 1, 0, 0)
            END IF
            i = i + 1
        WEND
    END IF
END SUB

' ── 香蕉 ────────────────────────────────────────────────────────────────

' 速度方向 → 贴块角度（度，屏幕坐标：0 = 朝右，正 = 顺时针）。
' 查 tan 表（0..90 度）定夹角，再看 vx/vy 的符号补象限。
SUB bananaAngle()
    ax = vx
    ay = vy
    IF ax < 0 THEN
        ax = 0 - ax
    END IF
    IF ay < 0 THEN
        ay = 0 - ay
    END IF
    IF ax = 0 THEN
        bang = 90
    ELSE
        ratio = INT(ay * 1000 / ax)
        best = 9999999
        bang = 0
        k = 0
        WHILE k <= 90
            df = tanT(k) - ratio
            IF df < 0 THEN
                df = 0 - df
            END IF
            IF df < best THEN
                best = df
                bang = k
            END IF
            k = k + 1
        WEND
    END IF
    ' 补象限：表中只给了"与水平线的夹角"
    IF vx < 0 THEN
        IF vy < 0 THEN
            bang = 180 + bang
        ELSE
            bang = 180 - bang
        END IF
    ELSE
        IF vy < 0 THEN
            bang = 0 - bang
        END IF
    END IF
END SUB

' 把月牙录成**图块**（开机一次，之后每帧只是贴一次）。
' 图块的好处：形状随便画（路径曲线都行），贴的时候能**按速度方向转** ——
' 而直接画路径的话，旋转得把角度算进每一段贝塞尔控制点里，那是一场噩梦。
SUB makeBanana()
    bid = ui_create_block(30, 20, 0)
    ' 月牙：两段三次贝塞尔，两个尖端在 (3,14) 与 (27,9)，凹面朝下
    ui_path("M 3 14 C 8 2, 22 -1, 27 9 C 20 4, 10 6, 3 14 Z", &HFF8A7418, 1, &HFFFFE066, "", 1, 0)
    ' 两个蒂（深一点的圆点）—— 两端有蒂才像香蕉，不然像月牙
    ui_circle(3, 14, 2, &HFF6E5A14, 1, 0)
    ui_circle(27, 9, 2, &HFF6E5A14, 1, 0)
    bid = ui_end_block()
END SUB

' 在当前位置把香蕉贴出来（按速度方向转）
SUB drawBanana()
    IF bid > 0 THEN
        bananaAngle()
        ' 姿态 = **弹道方向**（新月顺着飞行方向）+ **自转**（翻跟头）。
        ' 只跟弹道的话，一发平射全程只转 ±45°，22px 的小月牙看着几乎没动 ——
        ' 用户的原话是"转动幅度太小了"。
        bang = bang + spin
        ' 缩放 720‰：图块本身 30x20，原尺寸贴出来比大猩猩（约 28px 宽）还大一圈 ——
        ' 用户看着像"扔出去一个球拍"。720 之后约 22px，比猿小、又还看得出是香蕉。
        ui_draw_block(bid, INT(bx / S), INT(by / S), 720, 720, bang)
    ELSE
        ' 图块没建起来（有真窗口时不该发生）—— 退回一个圆点。
        ' **宁可画得糙，也不能让香蕉看不见**：看不见 = 这一局没法玩，
        ' 而且屏幕上没有任何东西提示你是哪儿坏了。
        ui_circle(INT(bx / S), INT(by / S), 4, C_BANANA, 1, 0)
    END IF
END SUB

' ── 昼夜自检（桌面就能跑，见文件头「自检」）────────────────────────────
' 画面对不对桌面看不出来（没有真窗口），但**数**能验：
' 天光系数该在 6:00 起、12:00 顶、18:00 归零；日月位置该单调地从左走到右。
SUB skyCheck()
    PRINT "── 昼夜自检（1 真实秒 = 1 游戏分钟）──"
    ' 弧线参数用一组固定的假尺寸，自检与真实屏幕无关
    arcX0 = 20
    arcW = 360
    arcBot = 500
    arcH = 300
    PRINT "tan 表：tan(0) 应 0 实 "; tanT(0); "；tan(45) 应 1000 实 "; tanT(45); "；tan(90) 应 999999 实 "; tanT(90)

    ' ① 天光系数（0..100）
    hlist = 0
    WHILE hlist < 24
        ghour = hlist
        gmin = 0
        skyClock()
        PRINT "  时 "; ghour; " 天光 "; dayL; " 日 x/y "; sunX; "/"; sunY; " 月 x/y "; moonX; "/"; moonY
        hlist = hlist + 1
    WEND

    ' ② 判据：6:00 前与 18:00 后必须全黑；12:00 必须是全天最亮
    ' ⚠ 每处都要**连 gmin 一起置位** —— 只改 ghour 的话 gmin 还是上一条留下的 59，
    '   查的就成了"12:59 天光"，而它当然不是 100。第一版就是这么"测"出假失败的。
    ghour = 5
    gmin = 59
    skyClock()
    IF dayL = 0 THEN
        PRINT "  ✔ 5:59 天光 = 0"
    ELSE
        PRINT "  ✘ 5:59 天光应为 0，实得 "; dayL
    END IF
    ghour = 12
    gmin = 0
    skyClock()
    IF dayL >= 99 THEN
        PRINT "  ✔ 12:00 天光 = 100"
    ELSE
        PRINT "  ✘ 12:00 天光应为 100，实得 "; dayL
    END IF
    ' 日出日落：太阳应该在地平线附近（y 接近 arcBot），正午接近弧顶
    ghour = 6
    gmin = 0
    skyClock()
    IF sunX = arcX0 THEN
        PRINT "  ✔ 6:00 太阳在东端 x="; sunX
    ELSE
        PRINT "  ✘ 6:00 太阳应在 x="; arcX0; "，实得 "; sunX
    END IF
    ghour = 18
    gmin = 0
    skyClock()
    IF sunX = arcX0 + arcW THEN
        PRINT "  ✔ 18:00 太阳在西端 x="; sunX
    ELSE
        PRINT "  ✘ 18:00 太阳应在 x="; arcX0 + arcW; "，实得 "; sunX
    END IF
    ' 月亮**差 12 小时**：0:00 时月亮应在天上（12:00 的位置换过来）
    ghour = 0
    gmin = 0
    skyClock()
    IF moonUp = 1 THEN
        PRINT "  ✔ 0:00 月亮在天上（y="; moonY; "，应接近弧顶）"
    ELSE
        PRINT "  ✘ 0:00 月亮应在天上"
    END IF
    ghour = 12
    gmin = 0
    skyClock()
    IF moonUp = 0 THEN
        PRINT "  ✔ 12:00 月亮不在天上"
    ELSE
        PRINT "  ✘ 12:00 月亮不该在天上"
    END IF

    ' ③ 配色：把各段算出来的分量打出来（画面在桌面上看不到，但**颜色是数**）
    ground = 506
    ghour = 8
    gmin = 19
    skyClock()
    skyPal()
    PRINT "  天顶 zenR/G/B = "; zenR; "/"; zenG; "/"; zenB
    PRINT "  地平 horR/G/B = "; horR; "/"; horG; "/"; horB
    skyAt(90)
    PRINT "  y=90 处天空 = "; mixR; "/"; mixG; "/"; mixB
    mix2(mixR, mixG, mixB, 252, 253, 255, 8 + INT(dayL * 78 / 100))
    PRINT "  云色（应偏蓝白，b 最大）= "; mixR; "/"; mixG; "/"; mixB
    PRINT "  天光 dayL = "; dayL
END SUB

' 星空 / 云也各录一个图块 —— 它们的**内容本来就只在游戏分钟这一档上变**
' （星的闪烁相位 `i*37+totMin`、云的漂移 `totMin*cld` 用的都是 `totMin`），
' 所以每分钟重录一次与逐帧重画**画面完全一致**，而每帧省下 18 次混色 + 40 个图元。
' 两块钱分开录是为了保住遮挡次序：星在日月**后面**、云在日月**前面**。
SUB buildSkyBlock()
    skyMin = gmin
    IF sid > 0 THEN
        idx = ui_free_block(sid)      ' 先释放旧的，再录新的（块表只有 128 格）
        sid = 0
    END IF
    IF qid > 0 THEN
        idx = ui_free_block(qid)
        qid = 0
    END IF
    sid = ui_create_block(sw, ground, 0)
    drawStars()
    sid = ui_end_block()
    qid = ui_create_block(sw, ground, 0)
    drawClouds()
    qid = ui_end_block()
END SUB

' 把整座城市（六栋楼 + 三百多个窗户矩形 + 屋顶细节）**录成一个图块**。
'
' 为什么：真机实测一帧 **432 个图元**，其中约 **340 个是窗户**（6 栋 × 2 列 × 十几行 × 2 个矩形），
' 而宿主侧其实很快（解析 + 矢量光栅合计 3ms）—— 每帧的代价几乎全在**VM 发四百多次 syscall**
' 加上随之而来的 BASIC 算术（每栋楼一堆混色）。录成图块之后：
'   · 每帧只贴一次（1 个图元）；
'   · `drawBuilding` 那套混色每个游戏分钟才跑一遍（配色本来就跟着游戏时间走）。
' ⚠ 图块的 (x,y) 是**中心**，所以贴回原位取 (sw/2, ground/2)；
'   块内内容按**绝对坐标**画（0..sw, 0..ground），尺寸要与之一致。
SUB buildCityBlock()
    cityDirty = 0
    lastMin = gmin
    IF cid > 0 THEN
        idx = ui_free_block(cid)      ' 先释放旧的，再录新的（否则块表 43 秒就满）
        cid = 0
    END IF
    cid = ui_create_block(sw, ground, 0)
    i = 0
    WHILE i < nbv
        drawBuilding(i)
        i = i + 1
    WEND
    drawStreet()                 ' 街景画在楼之后 ⇒ 挡在楼前（见 drawStreet 的注释）
    cid = ui_end_block()
END SUB

' ══ 天上飞的（小鸟 / 飞碟 / 飞机）══════════════════════════════════════
' 用户定的玩法：**打到就在空中爆炸，等于白白浪费一个香蕉**（不得分、直接换人）。
' 三种东西三种走法（用户逐条点的名）：
'   · **飞机** —— 定期飞过、**匀速**直线：不抖、不停，看到就是一条横线
'   · **小鸟** —— **方向不确定**：进哪边随机、高度每几拍抖一下、偶尔还会掉头
'   · **飞碟** —— **飞来 → 停一下 → 飞走**（三阶段，悬停是它的招牌）
'
' ⚠ 计时一律记在"*飞行* 拍"（30ms）上：待机拍（120ms）算 **4 拍**（见 moveFlyer 的 flyK）。
'   按"消息条数"计会让"悬停 60 拍"在瞄准时是 7 秒、飞行中只有 1.8 秒 —— 同一个数两种时长。

' 定期飞过的飞机。**单独一条通道**（不走 spawnFlyer 的每回合抽签），这才叫"定期"：
' 天上空着才放（不打断正在飞的鸟/飞碟），放完重新计时。
SUB spawnPlane()
    flyKind = 3
    flyOn = 1
    flyT = 0
    flyY = hudh + 30 + ui_rand(120)
    flyR = 20
    IF ui_rand(2) = 0 THEN
        flyX = 0 - 40
        flyV = 3
    ELSE
        flyX = sw + 40
        flyV = 0 - 3
    END IF
END SUB

' 每回合抽一次签：只掷**小鸟和飞碟**，飞机走 spawnPlane 那条定期通道。
SUB spawnFlyer()
    flyOn = 0
    planeT = 0                    ' 新回合重新给"定期飞机"计时
    IF ui_rand(100) < 55 THEN
        flyOn = 1
        flyT = 0
        flyKind = 1 + ui_rand(2)
        flyY = hudh + 36 + ui_rand(150)
        IF ui_rand(2) = 0 THEN
            flyX = 0 - 30
            flyV = 1 + ui_rand(2)
        ELSE
            flyX = sw + 30
            flyV = 0 - (1 + ui_rand(2))
        END IF
        IF flyKind = 1 THEN
            flyR = 10
            flyWob = 0
            flyWobT = 0
            flyRev = 2            ' 最多掉两次头 —— 不限的话它会赖在天上不走
            flyRevT = 0
        ELSE
            flyR = 16
            flyPhase = 0          ' 飞碟：先定个悬停点（屏幕中段），走到那儿就停
            flyHold = 0
            flyTx = INT(sw * 3 / 10) + ui_rand(INT(sw * 4 / 10))
        END IF
    END IF
END SUB

' 每拍挪一点。**待机拍（120ms）当 4 拍** —— 否则同一个速度在飞行（30ms/拍）与
' 瞄准（120ms/拍）两档下差四倍，看着忽快忽慢。
SUB moveFlyer()
    IF st = 1 THEN
        flyK = 1
    ELSE
        flyK = 4
    END IF

    IF flyOn = 0 THEN
        ' 天上空着 ⇒ 给"定期飞过的飞机"计时（600 拍 × 30ms ≈ 18 秒一架）
        planeT = planeT + flyK
        IF planeT >= 600 THEN
            planeT = 0
            spawnPlane()
        END IF
        EXIT SUB
    END IF

    flyT = flyT + flyK

    IF flyKind = 1 THEN
        ' ── 小鸟：方向不确定 ────────────────────────────────────────────
        flyX = flyX + flyV * flyK
        ' 高度每 12 拍（约 0.36 秒）重掷成 -1/0/1 —— 不是直线，像在扑腾
        IF flyT - flyWobT >= 12 THEN
            flyWobT = flyT
            flyWob = ui_rand(3) - 1
        END IF
        flyY = flyY + flyWob * flyK
        IF flyY < hudh + 20 THEN
            flyY = hudh + 20
            flyWob = 0
        END IF
        IF flyY > hudh + 210 THEN
            flyY = hudh + 210
            flyWob = 0
        END IF
        ' 偶尔掉头 —— "方向不确定"就落在这一条上
        IF flyT - flyRevT >= 90 THEN
            flyRevT = flyT
            IF flyRev > 0 THEN
                flyRev = flyRev - 1
                flyV = 0 - flyV
            END IF
        END IF
    END IF

    IF flyKind = 2 THEN
        ' ── 飞碟：飞来 → 停一下 → 飞走 ─────────────────────────────────
        IF flyPhase = 0 THEN
            flyX = flyX + flyV * flyK
            ' 按行进方向判"到点了没有"，从左进从右进同一套
            IF flyV > 0 THEN
                IF flyX >= flyTx THEN
                    flyX = flyTx
                    flyPhase = 1
                    flyHold = 0
                END IF
            ELSE
                IF flyX <= flyTx THEN
                    flyX = flyTx
                    flyPhase = 1
                    flyHold = 0
                END IF
            END IF
        END IF
        IF flyPhase = 1 THEN
            flyHold = flyHold + flyK   ' 悬停 60 拍 ≈ 1.8 秒，原地不动
            IF flyHold >= 60 THEN
                flyPhase = 2
            END IF
        END IF
        IF flyPhase = 2 THEN
            flyX = flyX + flyV * flyK  ' 飞走：沿原方向离开屏幕
        END IF
    END IF

    IF flyKind = 3 THEN
        ' ── 飞机：匀速直线 ─────────────────────────────────────────────
        flyX = flyX + flyV * flyK
    END IF

    ' 出界（两边各留 40 像素余量）
    IF flyX < 0 - 40 THEN
        flyOn = 0
    END IF
    IF flyX > sw + 40 THEN
        flyOn = 0
    END IF
END SUB

' ══ 流星 ══════════════════════════════════════════════════════════════
' 用户："晚上偶尔有流星"。**纯装饰、不参与碰撞** —— 它在大气层外，香蕉够不着。
' 只在夜里出现（白天看不见流星），也不是每轮都有：掷签 + 长间隔。
SUB spawnMeteor()
    metOn = 1
    metT = 0
    metY = hudh + 6
    ' 从上方斜着划过：一半从左往右下、一半从右往左下
    IF ui_rand(2) = 0 THEN
        metX = INT(sw * 3 / 10) + ui_rand(INT(sw * 7 / 10))
        metVX = 0 - (3 + ui_rand(2))
    ELSE
        metX = ui_rand(INT(sw * 7 / 10))
        metVX = 3 + ui_rand(2)
    END IF
    metVY = 2 + ui_rand(2)
END SUB

SUB moveMeteor()
    IF metOn = 1 THEN
        metX = metX + metVX * flyK
        metY = metY + metVY * flyK
        metT = metT + flyK
        ' 活了 100 拍（约 3 秒）就熄灭 —— 不给它"永远划下去"的机会
        IF metT > 100 THEN
            metOn = 0
        END IF
        IF metY > ground THEN
            metOn = 0
        END IF
        IF metX < 0 - 40 THEN
            metOn = 0
        END IF
        IF metX > sw + 40 THEN
            metOn = 0
        END IF
    ELSE
        metWait = metWait + flyK
        IF metWait >= 900 THEN        ' 约 27 秒掷一次签
            metWait = 0
            IF dayL < 45 THEN         ' 只在夜里
                IF ui_rand(100) < 50 THEN
                    spawnMeteor()
                END IF
            END IF
        END IF
    END IF
END SUB

' 一颗亮点 + 四段越来越淡的尾巴（没尾巴就不是流星，是一条直线）
SUB drawMeteor()
    IF metOn = 1 THEN
        mix2(255, 236, 180, 255, 255, 255, 45)
        metC = colM
        ui_circle(metX, metY, 2, metC, 1, 0)
        i = 1
        WHILE i <= 4
            mix2(255, 232, 170, 96, 132, 190, 96 - i * 22)
            metC = colM
            ui_line(metX - metVX * i, metY - metVY * i, metX - metVX * (i - 1), metY - metVY * (i - 1), metC, 2)
            i = i + 1
        WEND
    END IF
END SUB

' 天上飞的三种造型（都是剪影：白天灰、夜里更暗）
SUB drawFlyer()
    IF flyOn = 1 THEN
        mix2(26, 26, 34, 66, 68, 80, dayL)
        flyC = colM
        IF flyKind = 1 THEN
            ' 小鸟：两笔 V 形翅膀 + 一个小身子
            ui_line(flyX - 9, flyY - 4, flyX, flyY + 2, flyC, 2)
            ui_line(flyX, flyY + 2, flyX + 9, flyY - 4, flyC, 2)
            ui_circle(flyX, flyY + 1, 2, flyC, 1, 0)
        END IF
        IF flyKind = 2 THEN
            ' 飞碟：碟身 + 舱盖 + 两个舷窗灯（夜里亮）
            ui_ellipse(flyX, flyY, 16, 5, flyC, 1, 0)
            ui_circle(flyX, flyY - 5, 7, flyC, 1, 0)
            IF dayL < 55 THEN
                ui_circle(flyX - 8, flyY + 1, 2, &HFFFFD24A, 1, 0)
                ui_circle(flyX, flyY + 4, 2, &HFFFFD24A, 1, 0)
                ui_circle(flyX + 8, flyY + 1, 2, &HFFFFD24A, 1, 0)
            END IF
        END IF
        IF flyKind = 3 THEN
            ' 飞机：机身 + 一对机翼 + 尾翼（照飞行的方向朝前）
            IF flyV >= 0 THEN
                ui_rect(flyX - 16, flyY - 3, 32, 7, flyC, 1, 0, 3)
                ui_rect(flyX - 4, flyY - 12, 9, 24, flyC, 1, 0, 2)
                ui_rect(flyX - 20, flyY - 10, 6, 8, flyC, 1, 0, 0)
            ELSE
                ui_rect(flyX - 16, flyY - 3, 32, 7, flyC, 1, 0, 3)
                ui_rect(flyX - 5, flyY - 12, 9, 24, flyC, 1, 0, 2)
                ui_rect(flyX + 14, flyY - 10, 6, 8, flyC, 1, 0, 0)
            END IF
        END IF
    END IF
END SUB

' 一栋楼。几何与旧版**完全一致**（楼顶 y 取自同一张表、宽度同为 bw）——
' 变的只有配色与细节，所以弹道/命中判定一个字都不用动。
' 两个圆相不相交（用平方距离，避免开方）
FUNCTION circlesHit(ax AS INTEGER, ay AS INTEGER, ar AS INTEGER, bx AS INTEGER, by AS INTEGER, br AS INTEGER) AS INTEGER
    dx = ax - bx
    dy = ay - by
    rr = ar + br
    circlesHit = 0
    IF dx * dx + dy * dy <= rr * rr THEN
        circlesHit = 1
    END IF
END FUNCTION

' b 圆是不是**整个**落在 a 圆里（用于 clamp 快路径：整块都在里面就不用逐行裁）
FUNCTION circlesInside(ax AS INTEGER, ay AS INTEGER, ar AS INTEGER, bx AS INTEGER, by AS INTEGER, br AS INTEGER) AS INTEGER
    dx = ax - bx
    dy = ay - by
    rr = ar - br
    circlesInside = 0
    IF rr >= 0 THEN
        IF dx * dx + dy * dy <= rr * rr THEN
            circlesInside = 1
        END IF
    END IF
END FUNCTION

' 把「以 (bx,by) 为心、半径 br 的圆」**裁剪到**「以 (cx,cy) 为心、半径 cr 的圆」之内，
' 用逐行扫描画成一串 1 像素高的矩形。
' ⚠ 为什么不直接画整圆：本平台的 DSL **没有裁剪**（没有 clip/scissor），
'   画整圆会让月亮从被炸开的小洞里"溢"到楼面上，比不画还糟。
'   逐行求交是最省事又准确的近似：每一行的交集就是一段、正好一条 rect。
SUB clipCircle(cx AS INTEGER, cy AS INTEGER, cr AS INTEGER, bx AS INTEGER, by AS INTEGER, br AS INTEGER, col AS INTEGER)
    y = by - br
    WHILE y <= by + br
        dy = y - by
        IF dy < 0 THEN
            dy = 0 - dy
        END IF
        IF dy <= br THEN
            w1 = INT(SQR(br * br - dy * dy))
            dy2 = y - cy
            IF dy2 < 0 THEN
                dy2 = 0 - dy2
            END IF
            IF dy2 < cr THEN
                w2 = INT(SQR(cr * cr - dy2 * dy2))
                lo = bx - w1
                IF cx - w2 > lo THEN
                    lo = cx - w2
                END IF
                hi = bx + w1
                IF cx + w2 < hi THEN
                    hi = cx + w2
                END IF
                IF hi >= lo THEN
                    ui_rect(lo, y, hi - lo + 1, 1, col, 1, 0, 0)
                END IF
            END IF
        END IF
        y = y + 1
    WEND
END SUB

' 一个弹坑 = **把炸掉的那块涂回它后面真正有的东西**。
' 后面依次是：天空（渐变）→ 星星 → 日月 → 云。
' 用户报的正是日月那一层："房子本来就是挡着月亮的，炸穿一个洞就该看见月亮"。
' 星星是 2 像素的点、云在被炸开的小洞里也基本看不出 —— 只补日月（月面 / 日面）。
SUB drawCrater(hx0 AS INTEGER, hy0 AS INTEGER, hr0 AS INTEGER)
    skyAt(hy0)
    ui_circle(hx0, hy0, hr0, colM, 1, 0)
    IF moonUp = 1 THEN
        IF circlesHit(hx0, hy0, hr0, moonX, moonY, 14) = 1 THEN
            ' **快路径**：月面整个落在坑里 ⇒ 直接画圆（4 个图元），不必逐行裁。
            ' 逐行裁一次要写 ~28 条 1 像素矩形 —— 多炸几个坑就抵得上整座楼的窗户数
            ' （实测一帧图元从 120 涨到 505、帧率掉回 18）。用户炸的就是月亮那一带，
            ' 所以这条快路径覆盖的是**最常见**的那种情况。
            IF circlesInside(hx0, hy0, hr0, moonX, moonY, 14) = 1 THEN
                ui_circle(moonX, moonY, 14, &HFFF4F0DE, 1, 0)
                ui_circle(moonX - 5, moonY - 4, 3, &HFFDCD6BE, 1, 0)
                ui_circle(moonX + 4, moonY + 2, 2, &HFFDCD6BE, 1, 0)
            ELSE
                clipCircle(hx0, hy0, hr0, moonX, moonY, 14, &HFFF4F0DE)
                clipCircle(hx0, hy0, hr0, moonX - 5, moonY - 4, 3, &HFFDCD6BE)
                clipCircle(hx0, hy0, hr0, moonX + 4, moonY + 2, 2, &HFFDCD6BE)
            END IF
        END IF
    END IF
    IF sunUp = 1 THEN
        IF circlesHit(hx0, hy0, hr0, sunX, sunY, 11) = 1 THEN
            clipCircle(hx0, hy0, hr0, sunX, sunY, 11, &HFFFFF0B4)
        END IF
    END IF
END SUB

' 街景：**路灯与行道树** —— 贴在街道那条线上，画在楼**前面**。
' 为什么放这儿：楼是等宽铺满的（`bw` 一格一栋），中间没有缝，所以"街"就是地面那一条 ——
' 街上的东西只能画在楼**前面**（这也正是真实街景的透视关系：路灯、行道树挡在楼前）。
' ⚠ 它们进的是**城市图块**（随楼一起每分钟重录一次）⇒ 每帧零成本。
' 反面：弹坑会把它们一起"炸掉"（洞画在楼之后）。稀有、且看着还算合理，接受。
SUB drawStreet()
    i = 0
    WHILE i < 4
        ' 0/2 是灯、1/3 是树，交替；位置按 bw 分摊（不随机 —— 每局观感别差太多）
        sx = INT(bw * 15 / 10) + i * bw
        IF i = 0 THEN
            CALL drawLamp(sx)
        END IF
        IF i = 1 THEN
            CALL drawTree(sx + INT(bw / 3))
        END IF
        IF i = 2 THEN
            CALL drawTree(sx - INT(bw / 4))
        END IF
        IF i = 3 THEN
            CALL drawLamp(sx)
        END IF
        i = i + 1
    WEND
END SUB

' 一盏路灯：杆 + 灯头 + **夜里才亮**的灯
SUB drawLamp(lx AS INTEGER)
    mix2(30, 30, 40, 96, 100, 112, dayL)
    poleC = colM
    ui_rect(lx - 1, ground - 36, 3, 36, poleC, 1, 0, 0)          ' 杆
    ui_rect(lx - 7, ground - 40, 15, 5, poleC, 1, 0, 3)          ' 灯头横过来
    ' 天黑就亮：灯罩下一个小暖点（不画光晕 —— 光晕要混当地底色，而底下是楼不是天空）
    nightF = 100 - dayL
    IF nightF > 40 THEN
        ui_circle(lx, ground - 38, 3, &HFFFFE9A8, 1, 0)
    END IF
END SUB

' 一棵行道树：树干 + 两团树冠（白天绿、夜里压暗）
SUB drawTree(tx0 AS INTEGER)
    mix2(40, 28, 18, 92, 64, 40, dayL)
    ui_rect(tx0 - 2, ground - 16, 5, 16, colM, 1, 0, 0)
    mix2(10, 26, 14, 62, 138, 74, dayL)
    treeC = colM
    ui_circle(tx0, ground - 24, 11, treeC, 1, 0)
    ui_circle(tx0 - 8, ground - 19, 8, treeC, 1, 0)
    ui_circle(tx0 + 8, ground - 20, 8, treeC, 1, 0)
END SUB

SUB drawBuilding(bi AS INTEGER)
    tx = bi * bw
    ty = ui_gget(groof + bi)

    ' 色相：bi 对 6 取模，用减法循环
    rw = bi
    WHILE rw >= 6
        rw = rw - 6
    WEND
    br = bldR(rw)
    bg = bldG(rw)
    bb = bldB(rw)

    ' 楼体：夜色 = **同一个色相压到 16%**，按天光在两者间插值。
    ' 不另存一张夜色调色板 —— 那样"哪栋是哪栋"得靠人保持两张表同步（本仓的平行表坑）。
    mix2(INT(br * 16 / 100), INT(bg * 16 / 100), INT(bb * 16 / 100), br, bg, bb, dayL)
    bodyC = colM
    br = mixR
    bg = mixG
    bb = mixB
    ui_rect(tx, ty, bw, ground - ty, bodyC, 1, 0, 0)

    ' 受光面：左侧 3px 亮一点（白天更明显）；右侧 2px 压暗 —— 楼才有体积感
    mix2(br, bg, bb, 255, 255, 255, 6 + INT(dayL * 12 / 100))
    hiC = colM
    ui_rect(tx, ty, 3, ground - ty, hiC, 1, 0, 0)
    mix2(br, bg, bb, 0, 0, 0, 14 + INT(dayL * 10 / 100))
    ui_rect(tx + bw - 2, ty, 2, ground - ty, colM, 1, 0, 0)

    ' 檐口：比楼体亮一档（白天像被阳光打亮，夜里像被月光勾了一道边）
    mix2(br, bg, bb, 255, 255, 255, 18 + INT(dayL * 22 / 100))
    ui_rect(tx, ty, bw, 4, colM, 1, 0, 0)

    ' 窗：白天是玻璃（冷色反光），夜里点灯（暖黄）；**夜里亮的窗更多**。
    '   亮窗 昼(226,236,244) → 夜(255,198,104)
    '   暗窗 昼(52,74,96)   → 夜(14,14,24)
    mix2(255, 198, 104, 226, 236, 244, dayL)
    litC = colM
    mix2(14, 14, 24, 52, 74, 96, dayL)
    offC = colM
    mix2(br, bg, bb, 0, 0, 0, 45)
    frmC = colM
    litN = 4 - INT(dayL * 2 / 100)

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
            ' 窗框 + 玻璃（有框才像窗，没框就是一排色块）
            ui_rect(wx - 1, wy - 1, 8, 10, frmC, 1, 0, 0)
            IF idx < litN THEN
                ui_rect(wx, wy, 6, 8, litC, 1, 0, 0)
            ELSE
                ui_rect(wx, wy, 6, 8, offC, 1, 0, 0)
            END IF
            idx = idx + 1
            IF idx >= 5 THEN
                idx = 0
            END IF
            wy = wy + 18
        WEND
        c = c + 1
    WEND

    ' 门（贴楼底；被弹坑盖住是对的 —— 坑就是"打没了"）
    ui_rect(tx + INT(bw / 2) - 6, ground - 16, 12, 16, frmC, 1, 0, 3)

    ' 屋顶细节：水箱 / 天线 / 平的。**大猩猩站的那两栋不给**（会跟猿抢地方）
    IF bi > 0 THEN
        IF bi < nb1 THEN
            r3 = bi
            WHILE r3 >= 3
                r3 = r3 - 3
            WEND
            IF r3 = 0 THEN
                mix2(br, bg, bb, 255, 255, 255, 10)
                ui_rect(tx + 7, ty - 15, 15, 11, colM, 1, 0, 2)
                ui_line(tx + 10, ty - 4, tx + 10, ty, frmC, 2)
                ui_line(tx + 19, ty - 4, tx + 19, ty, frmC, 2)
            END IF
            IF r3 = 1 THEN
                ' 天线：一根细杆 + 两道横档。**顶上不要亮点** ——
                ' 用户一眼就把它当成了"屋子上的路灯"（真实的路灯搬到街上去了，见 drawStreet）。
                ui_line(tx + bw - 15, ty - 22, tx + bw - 15, ty, frmC, 2)
                ui_line(tx + bw - 19, ty - 18, tx + bw - 11, ty - 18, frmC, 2)
                ui_line(tx + bw - 17, ty - 12, tx + bw - 13, ty - 12, frmC, 2)
            END IF
        END IF
    END IF
END SUB

' 一只大猩猩：(ax, ay) 是它站的那块楼顶（脚底），who 只用来选颜色/朝向。
' 全部用矩形和圆拼，不依赖任何图片资源。
' 大猩猩。⚠ 外轮廓尺寸**一个没动**：命中盒 APE_R 是按它算的，
'   加描边只是往外多 2 像素（22 vs 42 的盒子），改大小会连带改碰撞规则。
SUB drawApe(ax AS INTEGER, ay AS INTEGER, who AS INTEGER)
    apeC = C_APE0
    apeL = C_APE0L
    apeD = C_APE0D
    IF who = 1 THEN
        apeC = C_APE1
        apeL = C_APE1L
        apeD = C_APE1D
    END IF

    ' 影子：脚下一道压暗的椭圆，把猩猩"钉"在楼顶上
    ui_ellipse(ax, ay - 1, 13, 3, apeD, 1, 0)

    ' 腿 + 脚
    ui_rect(ax - 8, ay - 9, 6, 9, apeC, 1, 0, 2)
    ui_rect(ax + 2, ay - 9, 6, 9, apeC, 1, 0, 2)
    ui_rect(ax - 11, ay - 3, 8, 3, apeD, 1, 0, 1)
    ui_rect(ax + 3, ay - 3, 8, 3, apeD, 1, 0, 1)

    ' 身体：先深色一层当描边，再压上本体色 —— 贴着天也不糊成一团
    ui_rect(ax - 11, ay - 25, 22, 18, apeD, 1, 0, 6)
    ui_rect(ax - 9, ay - 24, 18, 16, apeC, 1, 0, 5)
    ' 肚子：浅色一块，身体就不是个方块了
    ui_ellipse(ax, ay - 15, 7, 6, apeL, 1, 0)

    ' 手臂 + 手
    ui_rect(ax - 15, ay - 22, 6, 14, apeD, 1, 0, 3)
    ui_rect(ax + 9, ay - 22, 6, 14, apeD, 1, 0, 3)
    ui_rect(ax - 14, ay - 21, 4, 12, apeC, 1, 0, 2)
    ui_rect(ax + 10, ay - 21, 4, 12, apeC, 1, 0, 2)
    ui_circle(ax - 12, ay - 9, 3, apeD, 1, 0)
    ui_circle(ax + 12, ay - 9, 3, apeD, 1, 0)

    ' 耳朵：先画，被头压住一半
    ui_circle(ax - 8, ay - 27, 3, apeD, 1, 0)
    ui_circle(ax + 8, ay - 27, 3, apeD, 1, 0)

    ' 头（深色描边 + 本体）
    ui_circle(ax, ay - 28, 9, apeD, 1, 0)
    ui_circle(ax, ay - 28, 7, apeC, 1, 0)
    ' 口鼻：浅色一块 —— 有它才有脸
    ui_ellipse(ax, ay - 25, 5, 4, apeL, 1, 0)

    ' 两只眼睛（朝向对面：玩家一往右看，玩家二往左看）
    IF who = 0 THEN
        ui_circle(ax + 1, ay - 30, 2, C_TEXT, 1, 0)
        ui_circle(ax + 5, ay - 30, 2, C_TEXT, 1, 0)
    ELSE
        ui_circle(ax - 5, ay - 30, 2, C_TEXT, 1, 0)
        ui_circle(ax - 1, ay - 30, 2, C_TEXT, 1, 0)
    END IF
END SUB

' 把整数画到屏幕上（字符串拼接是坏的，只能用 STR$ 整份赋值 —— 缺陷 ④）
SUB drawNum(x AS INTEGER, y AS INTEGER, v AS INTEGER, col AS INTEGER, sz AS INTEGER, ac AS INTEGER)
    n$ = STR$(v)
    ui_text_v(x, y, n$, col, sz, ac, 3, 0)
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

    ' ── 先算这一帧的天光与日月位置（后面所有配色都依赖 dayL）──
    skyClock()
    skyPal()

    ' ── 天空：整块渐变 + 星星 + 日月 + 云 ──
    ' ⚠ 顺序就是**从远到近**：天空 → 星星 → 日月 → 云。
    '   （月亮/太阳画在建筑之前 ⇒ 落山时会被城市挡住，那正是想要的效果。）
    ui_clear(zen)
    drawSky()
    ' 重录判据：**游戏分钟一变就重录**（配色跟着时间走）。
    ' ⚠ 这在块能**手动释放**之后才成立：从前 `ui_create_block` 只增不减，
    '   每秒 3 块 ⇒ 43 秒撑满 128 格的表，之后 create 一律返回 0、
    '   画面**悄悄**退回逐帧画（实测图元 369、帧率 18.5，屏幕上完全看不出图块没了）。
    '   现在 `buildSkyBlock`/`buildCityBlock` 都是**先 `ui_free_block` 旧的再录新的**。
    IF skyMin <> gmin THEN
        buildSkyBlock()
    END IF
    IF sid > 0 THEN
        ui_draw_block(sid, INT(sw / 2), INT(ground / 2), 1000, 1000, 0)
    ELSE
        drawStars()
    END IF
    drawSunMoon()
    drawMeteor()
    IF qid > 0 THEN
        ui_draw_block(qid, INT(sw / 2), INT(ground / 2), 1000, 1000, 0)
    ELSE
        drawClouds()
    END IF
    drawFlyer()                  ' 天上飞的：画在云之后（贴天空那一层，楼之前）

    ' 右上角：游戏时间（用两个数字拼，不经 STR$ —— 它的前导空格在各实现里不一致）
    drawNum(sw - 74, hudh + 8, ghour, C_DIM, 13, 2)
    ui_text_v(sw - 68, hudh + 8, ":", C_DIM, 13, 0, 3, 0)
    drawNum(sw - 62, hudh + 8, gmin, C_DIM, 13, 0)

    ' ── 城市（整层一个图块，见 buildCityBlock）──
    ' 换局必然要重录（楼高/颜色都变了）；配色跟着游戏时间走，所以**每分钟**也重录一次。
    ' （每秒重录一次的话，那点开销与逐帧重画相比仍然可以忽略：6 次绘制/秒 vs 340 次/帧。）
    IF cityDirty = 1 THEN
        buildCityBlock()
    END IF
    IF lastMin <> gmin THEN
        buildCityBlock()
    END IF
    IF cid > 0 THEN
        ui_draw_block(cid, INT(sw / 2), INT(ground / 2), 1000, 1000, 0)
    ELSE
        ' 图块没建起来（不该发生）—— 退回逐栋画，宁可慢也别少了城市
        i = 0
        WHILE i < nbv
            drawBuilding(i)
            i = i + 1
        WEND
    END IF

    ' ── 弹坑：把炸掉的那块涂回天空色 ──
    ' 位置很讲究：**必须在画完所有楼之后**（画在楼前面会被下一栋楼盖住，
    ' 而缺口本来就可能横跨两栋的交界），**必须在地面之前**（否则会把地面啃掉一块）。
    '
    ' ⚠ 天空现在是**渐变**，"涂回天空色"必须问一句**哪个 y 的天空色**：
    '   取坑心的颜色（`skyAt(hy)`）。整块天空一个色的年代可以写死 `C_SKY`，
    '   现在写死就会在楼中间留下一块颜色不对的圆 —— 而且只在某些时段看得出来。
    i = 0
    WHILE i < nHole
        hi = i * 3
        hi = ghole + hi
        hx = ui_gget(hi)
        hy = ui_gget(hi + 1)
        hr = ui_gget(hi + 2)
        drawCrater(hx, hy, hr)
        i = i + 1
    WEND

    ' 地面：白天是灰亮的街面，夜里压暗
    mix2(12, 12, 22, 74, 78, 86, dayL)
    ui_rect(0, ground, sw, sh - ground, colM, 1, 0, 0)
    ' 街沿：地面顶上一条亮线（把楼和地分开，不然楼像插在雾里）
    mix2(24, 24, 38, 128, 134, 146, dayL)
    ui_rect(0, ground, sw, 2, colM, 1, 0, 0)

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
        drawBanana()
    END IF
    IF st = 1 THEN
        ' 尾迹：沿来路甩一串小点，越远越淡（按当地天空色淡出）
        k = 1
        WHILE k <= 3
            wx = INT(bx / S) - INT(vx * k / S)
            wy = INT(by / S) - INT(vy * k / S)
            skyAt(wy)
            mix2(mixR, mixG, mixB, 150, 146, 180, 70 - k * 18)
            ui_circle(wx, wy, 3 - k, colM, 1, 0)
            k = k + 1
        WEND
        drawBanana()
    END IF
    IF st = 2 THEN
        rr = 4 + boomT * 3
        ' 外圈留着不填（一个扩散的环）—— 只画两个实心圆的话，
        ' 爆炸看着像"换了个颜色的球"而不是"炸开了"
        ui_circle(ebx, eby, rr + 14, C_BOOM1, 0, 3)
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

    ui_text_v(12, 11, "玩家一", C_TEXT, 14, 0, 3, 0)
    i = 0
    WHILE i < wscore
        colr = C_PIP_OFF
        IF i < sc0 THEN
            colr = C_ANGLE
        END IF
        ui_circle(66 + i * 16, 20, 6, colr, 1, 0)
        i = i + 1
    WEND

    ui_text_v(sw - 12, 11, "玩家二", C_TEXT, 14, 2, 3, 0)
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
    ui_text_v(cxc, 5, "风", C_DIM, 13, 1, 3, 0)
    ui_rect(cxc - 46, 27, 92, 8, C_TRACK, 1, 0, 4)
    ui_rect(cxc - 1, 25, 3, 12, &HFF6A6A8C, 1, 0, 0)
    ui_rect(cxc + wind * 17 - 5, 23, 10, 16, C_MARKER, 1, 0, 3)

    ' ── 底部操作区 ──
    ui_rect(0, panY, sw, panh, C_PANEL, 1, 0, 0)
    ui_rect(0, panY, sw, 2, &HFF2E2B45, 1, 0, 0)

    ui_text_v(14, barAy + 8, "角度", C_DIM, 14, 0, 3, 0)
    ui_rect(barX, barAy, barW, barH, C_TRACK, 1, 0, 6)
    ui_rect(barX, barAy, INT(barW * aimA / 90), barH, C_ANGLE, 1, 0, 6)
    drawNum(sw - 14, barAy + 6, aimA, C_TEXT, 16, 2)

    ui_text_v(14, barPy + 8, "力度", C_DIM, 14, 0, 3, 0)
    ui_rect(barX, barPy, barW, barH, C_TRACK, 1, 0, 6)
    ui_rect(barX, barPy, INT(barW * aimP / 100), barH, C_POWER, 1, 0, 6)
    drawNum(sw - 14, barPy + 6, aimP, C_TEXT, 16, 2)

    ui_rect(14, fireY, sw - 28, fireH, C_FIRE, 1, 0, 8)
    IF st = 0 THEN
        ui_text_v(cxc, fireY + 7, "发 射", C_FIRE_T, 18, 1, 3, 0)
    ELSE
        ui_text_v(cxc, fireY + 7, "飞 行 中", C_FIRE_B, 18, 1, 3, 0)
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

    ' ── PRO：昼夜的弧线与时钟 ──
    ' 弧线两端各留 arcX0，弧顶在信息带下方，最低点略高于地面
    ' （最低点高于地面 ⇒ 日月"落"在城市后面而不是掉进街里）
    arcX0 = 40
    arcW = sw - 80
    arcBot = ground - 46
    arcH = arcBot - hudh - 46
    IF arcH < 40 THEN
        arcH = 40
    END IF
    sw2 = sw + 200               ' 云出画环绕的宽度
    initClock()                  ' 起始时钟按系统时间算（见 initClock）
    totMin = 0

    makeBanana()                 ' 香蕉图块（必须在窗口开出来之后）
    ctid = ui_timer_set(1000, 7) ' **时钟**：1 秒一格 = 1 游戏分钟

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
                ' ⚠ **两只定时器都发 9 号消息**（飞行/待机那只 + 时钟那只），
                '   靠 A 里的定时器 id 分开。不分的话时钟每一跳都会顺手推进一次弹道，
                '   而它偏偏是 1 秒一次 —— 症状是"香蕉偶尔多走一步"，几乎不可能查。
                IF ui_msg_a() = ctid THEN
                    tickClock()
                ELSE
                    moveFlyer()
                    moveMeteor()
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
    ' 时钟那只也要收 —— `ui_timer_set` 是**重复**定时器，不杀就一直在发消息
    IF ctid <> 0 THEN
        ui_timer_kill(ctid)
        ctid = 0
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
' 昼夜自检：同样桌面就能跑。画面对不对要真机看，但"天光/日月位置/配色"这些**数**
' 在这儿就能验 —— 昼夜的 bug 全是"某个时刻色不对/日月跳一下"这类，
' 等到真机上拿眼睛找，一天才走一遍，找一轮二十四分钟。
skyCheck()
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
