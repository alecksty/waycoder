// demo_ui.java —— **第 4 层：最新 UI 接口**（`ui_*` / `Lib/c/waycoder_ui.h`）
//
// 与第 3 层（BGI）正好相反：
//
//   · **没有固定分辨率**：画布多大由宿主给（`ui_scr_w()` / `ui_scr_h()`），程序现排版。
//   · **真彩 0xAARRGGBB**，不是 16 个索引色。
//   · **有消息循环**：触摸 / 按键 / 定时器 / 窗口被关 / 被转屏都从消息里出来。
//
// ## ⚠ 这份例程的两条写法都是**被 Java 前端的缺口逼出来的**，不是风格
//
// ### ① 不用 `static` 标量字段，也不定义常量 —— 全部状态放 `static int[] A`
//
// 实测（2026-09-24）：Java 的**类字段读出来是它的地址、不是它的值**。
//
//     static int A = 9;        读回 1024
//     static final int B = 9;  读回 1028
//     static int C; C = 9;     读回 1032     ← 赋过值也一样
//     局部变量 int local = 9;  读回 9        ✔
//
// 后果不只是"常量不准"：`if (t == MSG_TIMER)` 会**静默不成立**，主循环变成死循环
// （本 demo 的第一版就是这样 —— 跑满 30 秒超时被杀，屏幕上只出 1 帧）。
// 所以这里一个常量都不定义，数字写死在代码里、注释给出名字。
//
// ⚠ 但 `static int[] A` 是**能用**的：数组字段要的正好是一个地址，而字段读出来的就是地址。
//   `catch.java` 一直用这个模式，本文件照它写。
//
// ### ② 不把数组传给库函数 —— 用标量版的消息读取
//
// Java 把 `int[]` 传给库的 `int*` 形参时，指针落在数据起点**前 4 字节**（"长度头"）上：
//
//   · **写**：库写 `msg[0]` 落在数组的 `msg[-1]`，我们读到的 `msg[0]` 是库写的 `msg[1]`
//     （实测 `ui_wait(msg,…)`：库里 `msg=(类型,A,B,时间戳)`，Java 读到 `(A,B,时间戳,旧值)`）。
//   · **读**：库读到的 `pts[0]` 是数组的长度头 ⇒ `callwithint8({1,11,22,…})`
//     C 侧返回 `84197531`、Java 侧返回 **-6**（宿主"没有这个调用号"）。
//
// 所以这一层在 Java 里只用**标量**版：`ui_wait_msg` / `ui_msg_a` / `ui_msg_b`
// —— `waycoder_ui.h` 里那组"不碰指针的消息读取（非 C 语言用）"正是为这个准备的。
// 同理 `ui_polygon(int* pts, …)` 这类收数组的图元在 Java 里不能用，本 demo 不碰。
//
// ## 退出是有界的
//
// 主循环数**定时器拍数**，到 60 拍自己停；按任意键 / 点任意处也能提前退。
//
// 跑法：手机 `vml run examples/java/demo_ui.java`；
//       桌面 `vmlcli Examples/java/demo_ui.java --frames /tmp/fr`

public class DemoUi {

    // ── 窗体 ──
    static native int  ui_win_open_ex(String title, int w, int h, int rotatable, int gamepad);
    static native int  ui_win_close();
    static native int  ui_win_closed();
    static native int  ui_scr_w();
    static native int  ui_scr_h();
    static native int  ui_orientation();

    // ── 绘图 ──
    static native void ui_clear(int color);
    static native void ui_rect(int x, int y, int w, int h, int color, int fill, int lw, int radius);
    static native void ui_circle(int cx, int cy, int r, int color, int fill, int lw);
    static native void ui_ellipse(int cx, int cy, int rx, int ry, int color, int fill, int lw);
    static native void ui_line(int x1, int y1, int x2, int y2, int color, int lw);
    static native void ui_text(int x, int y, String s, int color, int size, int anchor);
    static native void ui_text_styled(int x, int y, String s, int color, int size, int anchor, int style);
    static native void ui_present();

    // ── 消息（**标量**版，见文件头 ②）──
    static native int  ui_wait_msg(int timeout_ms);
    static native int  ui_msg_a();
    static native int  ui_msg_b();
    static native int  ui_timer_set(int interval_ms, int tag);
    static native int  ui_timer_kill(int id);

    // ── 全部状态放这一个数组（见文件头 ①）──
    //   0=sw 1=sh 2=gy 3=frames 4=keys 5=touches 6=orient 7=tx 8=ty 9=tid
    static int[] A = new int[10];

    // 消息类型 / 锚点 / 方向 / 字体样式（源：Lib/c/waycoder_ui.h；这里只能写字面量）
    //   MSG_NONE=0 KEYDOWN=1 TIMER=9 TOUCHDOWN=6 MOUSEDOWN=4
    //   WINDOWCLOSE=10 WINDOWRESIZE=11 WINDOWORIENT=12
    //   ANCHOR_LEFT=0 CENTER=1 FONT_BOLD=1 ORIENT_PORTRAIT=0 LANDSCAPE=1
    //   WIN_ROTATABLE=1 WIN_NEED_GAMEPAD=1      MAX_FRAMES=60
    //
    // 颜色一律写**负数十进制**（Java 前端对十六进制字面量有解读差异，见 catch.java 的注记）。
    // 下面是本文件用到的那几个，名字 ↔ 0xAARRGGBB ↔ 十进制 三者对照：
    //   BG     0xFF101018 = -15724520      PANEL  0xFF1A1A24 = -15066588
    //   BLUE   0xFF4A90D9 = -11890471      ORANGE 0xFFE06C50 =  -2069424
    //   YELLOW 0xFFD9B44A =  -2509750      GREEN  0xFF50C878 = -11483016
    //   TITLE  0xFFE8E8F0 =  -1513232      ACCENT 0xFF51E86E = -11409298
    //   MUTED  0xFF9AA0B0 =  -6643536      GOLD   0xFFFFE060 =    -8096

    static void relayout() {
        A[0] = ui_scr_w();
        A[1] = ui_scr_h();
        if (A[0] <= 0) { A[0] = 360; }
        if (A[1] <= 0) { A[1] = 620; }
        A[2] = 14;                      // GY
    }

    static void draw() {
        int sw = A[0];
        int sh = A[1];
        int gy = A[2];
        int cx = sw / 2;
        int pad = sw / 16;
        int i;
        int w;

        ui_clear(-15724520);                                    // 0xFF101018

        ui_text_styled(cx, gy, "UI 接口 / demo_ui.java", -1513232, 16, 1, 1);
        if (A[6] == 1) {                                        // ORIENT_LANDSCAPE
            ui_text(cx, gy + 26, "屏幕方向 = 横屏 (LANDSCAPE)", -11409298, 13, 1);
        } else {
            ui_text(cx, gy + 26, "屏幕方向 = 竖屏 (PORTRAIT)", -11409298, 13, 1);
        }
        ui_text(cx, gy + 46, "画布按宿主给的尺寸现排（旋转后跟着变）", -6643536, 12, 1);

        // 跟随尺寸的方框：旋转后跟着变宽变矮（这就是"不写死坐标"的证明）
        ui_rect(pad, gy + 70, sw - pad * 2, 90, -15066588, 1, 0, 10);
        ui_rect(pad + 6, gy + 76, sw - pad * 2 - 12, 30, -11890471, 1, 0, 6);
        ui_text(pad + 16, gy + 84, "rect / round-rect（随屏宽伸缩）", -15724520, 12, 0);

        ui_circle(cx - sw / 6, gy + 140, sw / 12, -2069424, 1, 0);
        ui_ellipse(cx + sw / 6, gy + 140, sw / 9, sw / 18, -2509750, 1, 0);
        ui_line(pad, gy + 172, sw - pad, gy + 172, -11483016, 3);

        // 8 格真彩色带
        i = 0;
        while (i < 8) {
            w = (sw - pad * 2) / 8;
            if (i % 2 == 0) {
                ui_rect(pad + i * w, gy + 186, w - 2, 16, -2069424, 1, 0, 2);
            } else {
                ui_rect(pad + i * w, gy + 186, w - 2, 16, -11890471, 1, 0, 2);
            }
            i = i + 1;
        }
        ui_text(cx, gy + 210, "真彩 0xAARRGGBB（不是索引色）", -6643536, 12, 1);

        // ⚠ 屏上不写数字：Java 侧 `int` → `String` 要靠 `+` 拼接，而那个是坏的
        //   （见 demo_std.java 的文件头）。这里用**条形长度**表达进度，
        //   具体数字走 stdout —— 那边 `println(int)` 是好的。
        ui_text(pad, gy + 236, "已跑帧数（条形）", -6643536, 12, 0);
        ui_rect(pad, gy + 254, sw - pad * 2, 14, -15066588, 1, 0, 4);
        ui_rect(pad, gy + 254, (sw - pad * 2) * A[3] / 60, 14, -11483016, 1, 0, 4);

        ui_text(pad, gy + 278, "按键 / 触摸来了就画一个标记", -6643536, 12, 0);
        if (A[4] > 0) { ui_circle(pad + 20, gy + 306, 14, -2509750, 1, 0); }
        if (A[5] > 0) { ui_circle(pad + 60, gy + 306, 14, -11409298, 1, 0); }

        // 触摸标记：点哪儿就在哪儿留个圈（证明坐标真能用）
        if (A[7] >= 0) {
            ui_circle(A[7], A[8], 18, -2509750, 0, 2);
            ui_circle(A[7], A[8], 4, -2509750, 1, 0);
            ui_text(cx, sh - 44, "触摸坐标已经用上了", -2509750, 12, 1);
        } else {
            ui_text(cx, sh - 44, "点一下屏幕 / 按任意键退出", -6643536, 12, 1);
        }

        ui_text(cx, sh - 24, "退出：按任意键或点任意处（或等 N 帧到点）", -6643536, 12, 1);

        ui_present();
    }

    public static void main(String[] args) {
        int done = 0;
        int t;
        int w;
        int h;

        A[7] = -1;                      // gTx = -1（还没点过）
        A[3] = 0;
        A[4] = 0;
        A[5] = 0;

        // ── ① 开窗**之前**就问屏幕方向 ──
        A[6] = ui_orientation();

        // 开窗：声明"支持旋转 + 要手柄"（转屏时宿主会把新坐标空间整个给过来）
        w = ui_scr_w();
        h = ui_scr_h();
        if (w <= 0) { w = 360; }
        if (h <= 0) { h = 620; }
        ui_win_open_ex("demo_ui.java", w, h, 1, 1);
        relayout();
        draw();

        // ── 消息循环：定时器驱动重画，**有界退出** ──
        A[9] = ui_timer_set(60, 1);
        while (done == 0 && ui_win_closed() == 0) {
            t = ui_wait_msg(200);
            if (t == 0) { continue; }                  // MSG_NONE

            if (t == 9) {                              // MSG_TIMER
                A[3] = A[3] + 1;
                draw();
                if (A[3] >= 60) { done = 1; }          // 有界：到点自己停
                continue;
            }

            if (t == 1) {                              // MSG_KEYDOWN
                A[4] = A[4] + 1;
                draw();
                done = 1;                              // 有界：收到键就退
                continue;
            }

            if (t == 6 || t == 4) {                    // MSG_TOUCHDOWN / MSG_MOUSEDOWN
                A[5] = A[5] + 1;
                A[7] = ui_msg_a();                     // A=x B=y
                A[8] = ui_msg_b();
                draw();
                done = 1;                              // 有界：点到就退
                continue;
            }

            if (t == 12) {                             // MSG_WINDOWORIENT
                A[6] = ui_msg_a();                     // A = 新方向
                continue;
            }

            if (t == 11) {                             // MSG_WINDOWRESIZE
                relayout();                            // 换坐标系了 ⇒ 重排 + 重画
                draw();
                continue;
            }

            if (t == 10) { break; }                    // MSG_WINDOWCLOSE
        }

        ui_timer_kill(A[9]);
        ui_win_close();

        // 给无头验证留确定性判据（图形只能肉眼看，这几个数能自动比）
        if (A[6] == 1) {
            System.out.println("demo_ui: orient=LANDSCAPE");
        } else {
            System.out.println("demo_ui: orient=PORTRAIT");
        }
        System.out.print("demo_ui: canvas="); System.out.print(A[0]);
        System.out.print("x");                 System.out.println(A[1]);
        System.out.print("demo_ui: frames=");  System.out.println(A[3]);
        System.out.print("demo_ui: keys=");    System.out.println(A[4]);
        System.out.print("demo_ui: touches="); System.out.println(A[5]);
        System.out.println("demo_ui: done");
    }
}
