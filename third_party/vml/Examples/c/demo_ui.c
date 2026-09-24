/* demo_ui.c —— **第 4 层：最新 UI 接口**（`ui_*` / `Lib/c/waycoder_ui.h`）
 *
 * 这一层和第 3 层（BGI）正好相反：
 *
 *   · **没有固定分辨率**。画布多大由宿主给（`ui_scr_w()` / `ui_scr_h()`），
 *     程序按拿到的尺寸现排版 —— 手机上还有手柄区收放、横竖屏切换会让它变。
 *   · **颜色是真彩**（`0xAARRGGBB`），不是 16 个索引色。`0xFFE06C50` 就是那个橙。
 *   · **有消息循环**。触摸、按键、定时器、窗口被关 / 被转屏都从 `ui_wait` 出来，
 *     程序按 `msg[0]` 分派 —— 这是"能交互的程序"和"画完就死"的分水岭。
 *
 * ## 这个 demo 演示什么
 *
 *   ① **开窗前先问屏幕方向**（`ui_orientation()`）—— 程序据此决定"上下排还是左右排"。
 *      方向是**设备**的属性，别拿 `ui_scr_w() > ui_scr_h()` 去推（那是可用绘图区，
 *      手柄收起/展开会变）。
 *   ② 开窗（`ui_win_open_ex` 声明"支持旋转 + 要手柄"）→ 拿画布尺寸 → 按尺寸排版。
 *   ③ 画图元：矩形 / 圆角矩形 / 圆 / 椭圆 / 直线 / 文字，**每个位置都由 sw/sh 算出来**。
 *   ④ `ui_present()` 交帧 —— 它是"这一帧画完了"的唯一信号（不出图判据也认它）。
 *   ⑤ **消息循环**：定时器驱动重画、按键 / 触摸有响应、**有界退出**
 *      （收够 N 帧 或 按任意键 / 点任意处）。
 *
 * ## 退出是有界的，这一点是刻意的
 *
 * 「按 N 帧或收到键就退出」——没有输入也要能自己停下来。所以主循环数**定时器拍数**，
 * 到 `MAX_FRAMES` 就收尾。手机上按任意处 / 任意键可以提前退出。
 *
 * 跑法：
 *   手机    vml run examples/c/demo_ui.c
 *   桌面    vmlcli Examples/c/demo_ui.c --frames /tmp/fr        （每帧一张 PNG）
 * 桌面喂输入：加 `--input 脚本`，一行一条，如 `touchdown 100 200` / `keydown 27`。
 */

#include <waycoder_ui.h>
#include <stdio.h>

#define MAX_FRAMES 60          /* 到点自己停：60 拍 × 60ms ≈ 3.6 秒 */

/* 版面变量：每次重排都由当前画布尺寸算出来（**不写死**，见文件头第 ① ② 条）*/
static int SW;                 /* 画布宽 */
static int SH;                 /* 画布高 */
static int GY;                 /* 标题行基线 */

/* 计数：用来证明这些事件真的到过 */
static int g_frames;
static int g_keys;
static int g_touches;
static int g_orient;

/* 最近一次触摸/点击的位置（没点过就是 -1）*/
static int g_tx = -1;
static int g_ty = -1;

/* 数字 → 字符串。**不用 sprintf**：这一层要能在任意前端形态下跑，
 * 自己按位拆最省事（各语言的例程都这么做）。 */
static char *numstr(int v)
{
    static char buf[12];
    int n = 0;
    if (v < 0) { buf[0] = '-'; v = -v; n = 1; }
    if (v == 0) { buf[n] = '0'; buf[n + 1] = 0; return buf; }
    {
        char tmp[12];
        int m = 0;
        while (v > 0) { tmp[m] = (char)('0' + v % 10); v = v / 10; m = m + 1; }
        while (m > 0) { m = m - 1; buf[n] = tmp[m]; n = n + 1; }
    }
    buf[n] = 0;
    return buf;
}

static void relayout(void)
{
    SW = ui_scr_w();
    SH = ui_scr_h();
    if (SW <= 0) SW = 360;
    if (SH <= 0) SH = 620;
    GY = 14;
}

static void draw(void)
{
    int cx = SW / 2;
    int pad = SW / 16;
    int i;

    ui_clear(0xFF101018);                                   /* 背景 */

    /* ── 标题 + 屏宽高 + 方向 ── */
    ui_text_styled(cx, GY, "UI 接口 / demo_ui.c", 0xFFE8E8F0, 16,
                   VML_ANCHOR_CENTER, VML_FONT_BOLD);
    ui_text(cx, GY + 26, g_orient == VML_ORIENT_LANDSCAPE
                          ? "屏幕方向 = 横屏 (LANDSCAPE)"
                          : "屏幕方向 = 竖屏 (PORTRAIT)",
            0xFF51E86E, 13, VML_ANCHOR_CENTER);
    ui_text(cx, GY + 46, "画布按宿主给的尺寸现排", 0xFF9AA0B0, 12, VML_ANCHOR_CENTER);

    /* ── 一个跟随尺寸的方框（旋转后它会跟着变宽变矮 —— 这就是"不写死坐标"的证明）── */
    ui_rect(pad, GY + 70, SW - pad * 2, 90, 0xFF1A1A24, 1, 0, 10);
    ui_rect(pad + 6, GY + 76, SW - pad * 2 - 12, 30, 0xFF4A90D9, 1, 0, 6);
    ui_text(pad + 16, GY + 84, "rect / 圆角矩形（随屏宽伸缩）", 0xFF101018, 12,
            VML_ANCHOR_LEFT);

    /* ── 圆 / 椭圆 / 直线：三个基本形 ── */
    ui_circle(cx - SW / 6, GY + 140, SW / 12, 0xFFE06C50, 1, 0);
    ui_ellipse(cx + SW / 6, GY + 140, SW / 9, SW / 18, 0xFFD9B44A, 1, 0);
    ui_line(pad, GY + 172, SW - pad, GY + 172, 0xFF50C878, 3);

    /* ── 一条 8 格色带（真彩：这些都是 RGB，不是调色板索引）── */
    for (i = 0; i < 8; i++) {
        int w = (SW - pad * 2) / 8;
        ui_rect(pad + i * w, GY + 186, w - 2, 16,
                (i & 1) ? 0xFF4A90D9 : 0xFFE06C50, 1, 0, 2);
    }
    ui_text(cx, GY + 210, "真彩 0xAARRGGBB（不是索引色）", 0xFF9AA0B0, 12, VML_ANCHOR_CENTER);

    /* ── 事件计数：按模拟器和真机都看得出"消息到了没有" ── */
    ui_text(pad, GY + 236, "帧", 0xFF9AA0B0, 12, VML_ANCHOR_LEFT);
    ui_text(pad + 22, GY + 236, numstr(g_frames), 0xFFFFFFFF, 13, VML_ANCHOR_LEFT);
    ui_text(pad, GY + 256, "按键", 0xFF9AA0B0, 12, VML_ANCHOR_LEFT);
    ui_text(pad + 44, GY + 256, numstr(g_keys), 0xFFFFFFFF, 13, VML_ANCHOR_LEFT);
    ui_text(pad, GY + 276, "触摸", 0xFF9AA0B0, 12, VML_ANCHOR_LEFT);
    ui_text(pad + 44, GY + 276, numstr(g_touches), 0xFFFFFFFF, 13, VML_ANCHOR_LEFT);

    /* ── 触摸标记：点哪儿就在哪儿留一个圈（证明坐标真的能用）── */
    if (g_tx >= 0) {
        ui_circle(g_tx, g_ty, 18, 0xFFFFE060, 0, 2);
        ui_circle(g_tx, g_ty, 4, 0xFFFFE060, 1, 0);
        ui_text(cx, SH - 44, "触摸坐标已经用上了", 0xFFFFE060, 12, VML_ANCHOR_CENTER);
    } else {
        ui_text(cx, SH - 44, "点一下屏幕 / 按任意键退出", 0xFF9AA0B0, 12, VML_ANCHOR_CENTER);
    }

    ui_text(cx, SH - 24, "退出：按任意键或点任意处（或等 N 帧到点）",
            0xFF9AA0B0, 12, VML_ANCHOR_CENTER);

    ui_present();
}

int main(void)
{
    int msg[4];
    int tid;
    int done = 0;

    /* ── ① 开窗**之前**就问方向：程序据此决定排版 ── */
    g_orient = ui_orientation();

    /* 开窗：声明"支持旋转 + 要手柄"。
     * 支持旋转 = 转屏时宿主会把**新的坐标空间**整个给过来（发 WINDOWORIENT +
     * WINDOWRESIZE），所以下面的排版必须按当前尺寸算、并且收到消息后重排。 */
    ui_win_open_ex("demo_ui.c", ui_scr_w() > 0 ? ui_scr_w() : 360,
                                  ui_scr_h() > 0 ? ui_scr_h() : 620,
                   VML_WIN_ROTATABLE, VML_WIN_NEED_GAMEPAD);
    relayout();
    draw();

    /* ── ⑤ 消息循环：定时器驱动重画，有界退出 ── */
    tid = ui_timer_set(60, 1);
    while (!done && ui_win_closed() == 0) {
        int t = ui_wait(msg, 0);          /* 阻塞等一条（桌面最多等 --timeout 秒）*/
        if (t == VML_MSG_NONE) continue;

        if (t == VML_MSG_TIMER) {
            g_frames = g_frames + 1;
            draw();
            if (g_frames >= MAX_FRAMES) done = 1;    /* ← 有界：到点自己停 */
            continue;
        }

        if (t == VML_MSG_KEYDOWN) {
            g_keys = g_keys + 1;
            draw();
            done = 1;                                 /* ← 有界：收到键就退 */
            continue;
        }

        if (t == VML_MSG_TOUCHDOWN || t == VML_MSG_MOUSEDOWN) {
            g_touches = g_touches + 1;
            g_tx = msg[1];                            /* msg[1]=x msg[2]=y */
            g_ty = msg[2];
            draw();
            done = 1;                                 /* ← 有界：点到就退 */
            continue;
        }

        if (t == VML_MSG_WINDOWORIENT) {
            g_orient = msg[1];                        /* A = 新方向 */
            continue;
        }

        if (t == VML_MSG_WINDOWRESIZE) {
            /* 宿主要求换坐标系（声明了 ROTATABLE 才会来）。重新问尺寸、重排版。 */
            relayout();
            draw();
            continue;
        }

        if (t == VML_MSG_WINDOWCLOSE) break;          /* 用户把窗口关了 */
    }

    /* ── 收尾：定时器要杀，屏幕常亮要还回去（开了才要还）── */
    ui_timer_kill(tid);
    ui_win_close();

    /* 给无头验证留一行**确定性**的判据（图形部分只能肉眼看，这几个数能自动比）。 */
    printf("demo_ui: orient=%s\n", g_orient == VML_ORIENT_LANDSCAPE ? "LANDSCAPE" : "PORTRAIT");
    printf("demo_ui: canvas=%dx%d\n", SW, SH);
    printf("demo_ui: frames=%d keys=%d touches=%d\n", g_frames, g_keys, g_touches);
    printf("demo_ui: done\n");
    return 0;
}
