// demo_ui.cs —— **第 4 层：最新 UI 接口**（`ui_*` / `Lib/c/waycoder_ui.h`）
//
// 与第 3 层（BGI）正好相反：
//
//   · **没有固定分辨率**：画布多大由宿主给（`ui_scr_w()` / `ui_scr_h()`），程序现排版。
//   · **真彩 0xAARRGGBB**，不是 16 个索引色。
//   · **有消息循环**：触摸 / 按键 / 定时器 / 窗口被关 / 被转屏都从消息里出来。
//
// ## ⚠ 消息用**标量**版读，不把数组传给库函数
//
// C# 的 `int[]` 传给库里 `int*` 形参时**指针落在数据起点前 4 字节**（"长度头"）上。
// 实测（2026-09-24）：`callwithint8({1,11,22,…})` —— C 侧返回 `84197531`、
// **C# 侧返回 -6**（宿主"没有这个调用号" ⇒ 它读到的调用号不是 1）。同理 `ui_wait(msg,…)`
// 里宿主写的 `msg=(类型,A,B,时间戳)` 会被错位读到。
//
// 所以这一层在 C# 里只用**标量**版：`ui_wait_msg` / `ui_msg_a` / `ui_msg_b`
// —— `waycoder_ui.h` 里那组"不碰指针的消息读取（非 C 语言用）"正是为这个准备的。
// 同理 `ui_polygon(int* pts, …)` 这类收数组的图元在 C# 里不能用，本 demo 不碰。
//
// ## 退出是有界的
//
// 主循环数**定时器拍数**，到 60 拍自己停；按任意键 / 点任意处也能提前退。
//
// 跑法：手机 `vml run examples/csharp/demo_ui.cs`；
//       桌面 `vmlcli Examples/csharp/demo_ui.cs --frames /tmp/fr`

class DemoUi
{
    // ── 消息类型 / 锚点 / 方向 / 字体样式（源：Lib/c/waycoder_ui.h）──
    const int MSG_NONE = 0;
    const int MSG_KEYDOWN = 1;
    const int MSG_TOUCHDOWN = 6;
    const int MSG_MOUSEDOWN = 4;
    const int MSG_TIMER = 9;
    const int MSG_WINDOWCLOSE = 10;
    const int MSG_WINDOWRESIZE = 11;
    const int MSG_WINDOWORIENT = 12;

    const int ANCHOR_LEFT = 0;
    const int ANCHOR_CENTER = 1;
    const int FONT_BOLD = 1;
    const int ORIENT_LANDSCAPE = 1;
    const int WIN_ROTATABLE = 1;
    const int WIN_NEED_GAMEPAD = 1;
    const int MAX_FRAMES = 60;             // 到点自己停：60 拍 × 60ms ≈ 3.6 秒

    // ── 颜色（`0xAARRGGBB` 的十进制负数形式，与仓库里其它语言例程一致）──
    const int BG = -15724520;              // 0xFF101018
    const int PANEL = -15066588;           // 0xFF1A1A24
    const int BLUE = -11890471;            // 0xFF4A90D9
    const int ORANGE = -2069424;           // 0xFFE06C50
    const int YELLOW = -2509750;           // 0xFFD9B44A
    const int GREEN = -11483016;           // 0xFF50C878
    const int TITLE = -1513232;            // 0xFFE8E8F0
    const int ACCENT = -11409298;          // 0xFF51E86E
    const int MUTED = -6643536;            // 0xFF9AA0B0

    // ── 状态 ──
    static int sw;
    static int sh;
    static int gy;
    static int frames;
    static int keys;
    static int touches;
    static int orient;
    static int tx = -1;
    static int ty = -1;
    static int tid;

    static void relayout()
    {
        sw = ui_scr_w();
        sh = ui_scr_h();
        if (sw <= 0) sw = 360;
        if (sh <= 0) sh = 620;
        gy = 14;
    }

    static void draw()
    {
        int cx = sw / 2;
        int pad = sw / 16;
        int i = 0;
        int w;

        ui_clear(BG);

        ui_text_styled(cx, gy, "UI 接口 / demo_ui.cs", TITLE, 16, ANCHOR_CENTER, FONT_BOLD);
        if (orient == ORIENT_LANDSCAPE)
        {
            ui_text(cx, gy + 26, "屏幕方向 = 横屏 (LANDSCAPE)", ACCENT, 13, ANCHOR_CENTER);
        }
        else
        {
            ui_text(cx, gy + 26, "屏幕方向 = 竖屏 (PORTRAIT)", ACCENT, 13, ANCHOR_CENTER);
        }
        ui_text(cx, gy + 46, "画布按宿主给的尺寸现排（旋转后跟着变）", MUTED, 12, ANCHOR_CENTER);

        // 跟随尺寸的方框：旋转后跟着变宽变矮
        ui_rect(pad, gy + 70, sw - pad * 2, 90, PANEL, 1, 0, 10);
        ui_rect(pad + 6, gy + 76, sw - pad * 2 - 12, 30, BLUE, 1, 0, 6);
        ui_text(pad + 16, gy + 84, "rect / round-rect（随屏宽伸缩）", BG, 12, ANCHOR_LEFT);

        ui_circle(cx - sw / 6, gy + 140, sw / 12, ORANGE, 1, 0);
        ui_ellipse(cx + sw / 6, gy + 140, sw / 9, sw / 18, YELLOW, 1, 0);
        ui_line(pad, gy + 172, sw - pad, gy + 172, GREEN, 3);

        // 8 格真彩色带
        while (i < 8)
        {
            w = (sw - pad * 2) / 8;
            if (i % 2 == 0) ui_rect(pad + i * w, gy + 186, w - 2, 16, ORANGE, 1, 0, 2);
            else            ui_rect(pad + i * w, gy + 186, w - 2, 16, BLUE, 1, 0, 2);
            i = i + 1;
        }
        ui_text(cx, gy + 210, "真彩 0xAARRGGBB（不是索引色）", MUTED, 12, ANCHOR_CENTER);

        // 进度用**条形长度**表达（C# 前端没有 int→string，见 demo_std.cs）；
        // 具体数字走 stdout —— 那边 Console 打印是好的。
        ui_text(pad, gy + 236, "已跑帧数（条形）", MUTED, 12, ANCHOR_LEFT);
        ui_rect(pad, gy + 254, sw - pad * 2, 14, PANEL, 1, 0, 4);
        ui_rect(pad, gy + 254, (sw - pad * 2) * frames / MAX_FRAMES, 14, BLUE, 1, 0, 4);

        ui_text(pad, gy + 278, "按键 / 触摸来了就画一个标记", MUTED, 12, ANCHOR_LEFT);
        if (keys > 0) ui_circle(pad + 20, gy + 306, 14, YELLOW, 1, 0);
        if (touches > 0) ui_circle(pad + 60, gy + 306, 14, ACCENT, 1, 0);

        // 触摸标记：点哪儿就在哪儿留个圈（证明坐标真能用）
        if (tx >= 0)
        {
            ui_circle(tx, ty, 18, YELLOW, 0, 2);
            ui_circle(tx, ty, 4, YELLOW, 1, 0);
            ui_text(cx, sh - 44, "触摸坐标已经用上了", YELLOW, 12, ANCHOR_CENTER);
        }
        else
        {
            ui_text(cx, sh - 44, "点一下屏幕 / 按任意键退出", MUTED, 12, ANCHOR_CENTER);
        }

        ui_text(cx, sh - 24, "退出：按任意键或点任意处（或等 N 帧到点）", MUTED, 12, ANCHOR_CENTER);

        ui_present();
    }

    static void Main()
    {
        int done = 0;
        int t;
        int w;
        int h;

        // ── ① 开窗**之前**就问屏幕方向 ──
        orient = ui_orientation();

        // 开窗：声明"支持旋转 + 要手柄"（转屏时宿主会把新坐标空间整个给过来）
        w = ui_scr_w();
        h = ui_scr_h();
        if (w <= 0) w = 360;
        if (h <= 0) h = 620;
        ui_win_open_ex("demo_ui.cs", w, h, WIN_ROTATABLE, WIN_NEED_GAMEPAD);
        relayout();
        draw();

        // ── 消息循环：定时器驱动重画，**有界退出** ──
        tid = ui_timer_set(60, 1);
        while (done == 0 && ui_win_closed() == 0)
        {
            t = ui_wait_msg(200);
            if (t == MSG_NONE) continue;

            if (t == MSG_TIMER)
            {
                frames = frames + 1;
                draw();
                if (frames >= MAX_FRAMES) done = 1;     // 有界：到点自己停
                continue;
            }

            if (t == MSG_KEYDOWN)
            {
                keys = keys + 1;
                draw();
                done = 1;                              // 有界：收到键就退
                continue;
            }

            if (t == MSG_TOUCHDOWN || t == MSG_MOUSEDOWN)
            {
                touches = touches + 1;
                tx = ui_msg_a();                       // A=x B=y
                ty = ui_msg_b();
                draw();
                done = 1;                              // 有界：点到就退
                continue;
            }

            if (t == MSG_WINDOWORIENT)
            {
                orient = ui_msg_a();                   // A = 新方向
                continue;
            }

            if (t == MSG_WINDOWRESIZE)
            {
                relayout();                            // 换坐标系了 ⇒ 重排 + 重画
                draw();
                continue;
            }

            if (t == MSG_WINDOWCLOSE) break;
        }

        ui_timer_kill(tid);
        ui_win_close();

        // 给无头验证留确定性判据（图形只能肉眼看，这几个数能自动比）
        if (orient == ORIENT_LANDSCAPE) println_str("demo_ui: orient=LANDSCAPE");
        else                            println_str("demo_ui: orient=PORTRAIT");
        print_str("demo_ui: canvas="); print_int(sw); print_str("x"); println_int(sh);
        print_str("demo_ui: frames="); println_int(frames);
        print_str("demo_ui: keys=");   println_int(keys);
        print_str("demo_ui: touches="); println_int(touches);
        println_str("demo_ui: done");
    }
}
