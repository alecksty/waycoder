// demo_ui.kt —— **第 4 层：最新 UI 接口**（`ui_*` / `Lib/c/waycoder_ui.h`）
//
// 与第 3 层（BGI）正好相反：
//
//   · **没有固定分辨率**：画布多大由宿主给（`ui_scr_w()` / `ui_scr_h()`），程序现排版。
//   · **真彩 0xAARRGGBB**，不是 16 个索引色。
//   · **有消息循环**：触摸 / 按键 / 定时器 / 窗口被关 / 被转屏都从消息里出来。
//
// ## ⚠ 两条写法约束（都是实测出来的）
//
// ### ① 消息用**标量**版读，不把数组传给库函数
//
// `catch.kt` 记的是「文件级 `arrayOf` 读回 0」；更普遍的一条是：**数组传给库的 `int*`
// 形参时指针落在数据起点前 4 字节**（"长度头"）—— 实测 `ui_wait(msg,…)` 时库里写
// `msg=(类型,A,B,时间戳)`，Kotlin 这边读到 `(A,B,时间戳,旧值)`（整体错位一格）。
// 所以这一层在 Kotlin 里只用**标量**版：`ui_wait_msg` / `ui_msg_a` / `ui_msg_b`
// —— `waycoder_ui.h` 里那组"不碰指针的消息读取（非 C 语言用）"正是为这个准备的。
// 同理 `ui_polygon(int* pts, …)` 这类收数组的图元在 Kotlin 里不能用，本 demo 不碰。
//
// ### ② 状态放**文件级 `var`**，数组放 `main` 里
//
// 实测（2026-09-24）：文件级 `val TOPVAL = 9` / `var topA` 都能正常读写（读回 9），
// 所以标量状态放文件级没问题；而**数组**必须留在 `main` 内（`catch.kt` 那条）。
// 本 demo 的数组一个也不用 —— 标量状态正好够。
//
// ## 退出是有界的
//
// 主循环数**定时器拍数**，到 60 拍自己停；按任意键 / 点任意处也能提前退。
//
// 跑法：手机 `vml run examples/kotlin/demo_ui.kt`；
//       桌面 `vmlcli Examples/kotlin/demo_ui.kt --frames /tmp/fr`

// ── 状态（文件级标量；数字写死，旁边注释给名字 —— 源：Lib/c/waycoder_ui.h）──
//   MSG_NONE=0 KEYDOWN=1 TIMER=9 TOUCHDOWN=6 MOUSEDOWN=4
//   WINDOWCLOSE=10 WINDOWRESIZE=11 WINDOWORIENT=12
//   ANCHOR_LEFT=0 CENTER=1 FONT_BOLD=1 ORIENT_PORTRAIT=0 LANDSCAPE=1
//   WIN_ROTATABLE=1 WIN_NEED_GAMEPAD=1
var SW = 0
var SH = 0
var GY = 14
var gFrames = 0
var gKeys = 0
var gTouches = 0
var gOrient = 0
var gTx = -1
var gTy = -1

// 颜色写负数十进制（`0xAARRGGBB` 的十进制负数形式）。对照表：
//   BG     0xFF101018 = -15724520      PANEL  0xFF1A1A24 = -15066588
//   BLUE   0xFF4A90D9 = -11890471      ORANGE 0xFFE06C50 =  -2069424
//   YELLOW 0xFFD9B44A =  -2509750      GREEN  0xFF50C878 = -11483016
//   TITLE  0xFFE8E8F0 =  -1513232      ACCENT 0xFF51E86E = -11409298
//   MUTED  0xFF9AA0B0 =  -6643536      GOLD   0xFFFFE060 =    -8096

fun relayout() {
    SW = ui_scr_w()
    SH = ui_scr_h()
    if (SW <= 0) { SW = 360 }
    if (SH <= 0) { SH = 620 }
    GY = 14
}

fun draw() {
    var cx = SW / 2
    var pad = SW / 16
    var i = 0
    var w = 0

    ui_clear(-15724520)

    ui_text_styled(cx, GY, "UI 接口 / demo_ui.kt", -1513232, 16, 1, 1)
    if (gOrient == 1) {
        ui_text(cx, GY + 26, "屏幕方向 = 横屏 (LANDSCAPE)", -11409298, 13, 1)
    } else {
        ui_text(cx, GY + 26, "屏幕方向 = 竖屏 (PORTRAIT)", -11409298, 13, 1)
    }
    ui_text(cx, GY + 46, "画布按宿主给的尺寸现排（旋转后跟着变）", -6643536, 12, 1)

    // 跟随尺寸的方框：旋转后跟着变宽变矮（这就是"不写死坐标"的证明）
    ui_rect(pad, GY + 70, SW - pad * 2, 90, -15066588, 1, 0, 10)
    ui_rect(pad + 6, GY + 76, SW - pad * 2 - 12, 30, -11890471, 1, 0, 6)
    ui_text(pad + 16, GY + 84, "rect / round-rect（随屏宽伸缩）", -15724520, 12, 0)

    ui_circle(cx - SW / 6, GY + 140, SW / 12, -2069424, 1, 0)
    ui_ellipse(cx + SW / 6, GY + 140, SW / 9, SW / 18, -2509750, 1, 0)
    ui_line(pad, GY + 172, SW - pad, GY + 172, -11483016, 3)

    // 8 格真彩色带
    i = 0
    while (i < 8) {
        w = (SW - pad * 2) / 8
        if (i % 2 == 0) {
            ui_rect(pad + i * w, GY + 186, w - 2, 16, -2069424, 1, 0, 2)
        } else {
            ui_rect(pad + i * w, GY + 186, w - 2, 16, -11890471, 1, 0, 2)
        }
        i = i + 1
    }
    ui_text(cx, GY + 210, "真彩 0xAARRGGBB（不是索引色）", -6643536, 12, 1)

    // ⚠ 屏上不写数字：Kotlin 侧的 `Int` → `String` 要靠 `+` 拼接，而那个是坏的
    //   （见 demo_std.kt 的文件头）。这里用**条形长度**表达进度，
    //   具体数字走 stdout —— 那边 `println(Int)` 是好的。
    ui_text(pad, GY + 236, "已跑帧数（条形）", -6643536, 12, 0)
    ui_rect(pad, GY + 254, SW - pad * 2, 14, -15066588, 1, 0, 4)
    ui_rect(pad, GY + 254, (SW - pad * 2) * gFrames / 60, 14, -11483016, 1, 0, 4)

    ui_text(pad, GY + 278, "按键 / 触摸来了就画一个标记", -6643536, 12, 0)
    if (gKeys > 0) { ui_circle(pad + 20, GY + 306, 14, -2509750, 1, 0) }
    if (gTouches > 0) { ui_circle(pad + 60, GY + 306, 14, -11409298, 1, 0) }

    // 触摸标记：点哪儿就在哪儿留个圈（证明坐标真能用）
    if (gTx >= 0) {
        ui_circle(gTx, gTy, 18, -2509750, 0, 2)
        ui_circle(gTx, gTy, 4, -2509750, 1, 0)
        ui_text(cx, SH - 44, "触摸坐标已经用上了", -2509750, 12, 1)
    } else {
        ui_text(cx, SH - 44, "点一下屏幕 / 按任意键退出", -6643536, 12, 1)
    }

    ui_text(cx, SH - 24, "退出：按任意键或点任意处（或等 N 帧到点）", -6643536, 12, 1)

    ui_present()
}

fun main() {
    var done = 0
    var t = 0
    var tid = 0
    var w = 0
    var h = 0

    // ── ① 开窗**之前**就问屏幕方向 ──
    gOrient = ui_orientation()

    // 开窗：声明"支持旋转 + 要手柄"（转屏时宿主会把新坐标空间整个给过来）
    w = ui_scr_w()
    h = ui_scr_h()
    if (w <= 0) { w = 360 }
    if (h <= 0) { h = 620 }
    ui_win_open_ex("demo_ui.kt", w, h, 1, 1)
    relayout()
    draw()

    // ── 消息循环：定时器驱动重画，**有界退出** ──
    tid = ui_timer_set(60, 1)
    while (done == 0 && ui_win_closed() == 0) {
        t = ui_wait_msg(200)
        if (t == 0) { continue }                    // MSG_NONE

        if (t == 9) {                               // MSG_TIMER
            gFrames = gFrames + 1
            draw()
            if (gFrames >= 60) { done = 1 }          // 有界：到点自己停
            continue
        }

        if (t == 1) {                               // MSG_KEYDOWN
            gKeys = gKeys + 1
            draw()
            done = 1                                // 有界：收到键就退
            continue
        }

        if (t == 6 || t == 4) {                     // MSG_TOUCHDOWN / MSG_MOUSEDOWN
            gTouches = gTouches + 1
            gTx = ui_msg_a()                        // A=x B=y
            gTy = ui_msg_b()
            draw()
            done = 1                                // 有界：点到就退
            continue
        }

        if (t == 12) {                              // MSG_WINDOWORIENT
            gOrient = ui_msg_a()                    // A = 新方向
            continue
        }

        if (t == 11) {                              // MSG_WINDOWRESIZE
            relayout()                              // 换坐标系了 ⇒ 重排 + 重画
            draw()
            continue
        }

        if (t == 10) { break }                      // MSG_WINDOWCLOSE
    }

    ui_timer_kill(tid)
    ui_win_close()

    // 给无头验证留确定性判据（图形只能肉眼看，这几个数能自动比）
    if (gOrient == 1) {
        println("demo_ui: orient=LANDSCAPE")
    } else {
        println("demo_ui: orient=PORTRAIT")
    }
    print("demo_ui: canvas="); print(SW); print("x"); println(SH)
    print("demo_ui: frames="); println(gFrames)
    print("demo_ui: keys=");   println(gKeys)
    print("demo_ui: touches="); println(gTouches)
    println("demo_ui: done")
}
