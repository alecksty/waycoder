/* test_font.c —— 竖对齐四档的**可视判据**（v0.96.399）
 *
 * 为什么要有这个程序：竖对齐（`ui_set_valign`，状态式，四档）是新加的能力，而它的偏差
 * **只在真机上看得见** —— 桌面的自绘（光栅 TrueType / SVG）与手机的**平台字体**度量不同，
 * 构建全绿、自测全绿都证明不了"字在方框里到底居中没有"。所以把四种对齐**并排画出来**，
 * 每格中心画一个黄色十字当**准心**，用眼睛（或截屏量像素）判。
 *
 * 判据（四格用的是**同一个字、同一个字号、同一个 y** = 格心 ⇒ 差别只来自档位）：
 *
 *   BASE   —— `y` 就是**基线**：字形坐在准心上（字的底边贴着十字的横线）
 *   MIDDLE —— 盒的竖直中心落在准心（"在方框/按钮里居中"用这一档）
 *   TOP    —— 盒顶落在准心（字形整体落在准心**下方**）
 *   BOTTOM —— 盒底落在准心（字形整体落在准心**上方**）
 *
 * ⚠ 盒高 = 字号（单行）⇒ BASE 与 MIDDLE 之间**理论上**差 0.3×字号、TOP/BOTTOM 各差一格。
 *   真机上量到的偏差就是"平台字体真实上升"与共享层那个近似值之差 —— 那条曾在算盘按键上
 *   表现为"文字靠下、不居中"（0.245×字号，肉眼一眼可见）。
 */
#include <waycoder_ui.h>

#define BG      0xFF10141C
#define CELL    0xFF1E2735
#define EDGE    0xFF3A4763
#define MARK    0xFFFFC83D      /* 准心十字：黄 */
#define TXT     0xFFF2F6FA
#define LBL     0xFF8FA3BF

static int W;
static int H;

/* 四格的档位与名字（顺序 = 画出来的顺序）*/
static int  modes[4];
static char *names[4];

/* 一格：底、边框、准心十字、一个字。`valign` 决定这个字怎么落在 (cx, cy) 上。*/
static void cell(int x, int y, int w, int h, int valign, char *name)
{
    int cx;
    int cy;
    int arm;

    cx = x + w / 2;
    cy = y + h / 2;
    arm = 20;

    ui_rect(x, y, w, h, CELL, 1, 0, 12);
    ui_rect(x, y, w, h, EDGE, 0, 2, 12);

    /* 准心十字：横线略长，便于看出"字压在线上"还是"字坐在线上" */
    ui_line(cx - arm * 2, cy, cx + arm * 2, cy, MARK, 2);
    ui_line(cx, cy - arm, cx, cy + arm, MARK, 2);
    /* 准心中央一个小点：字号大时横线会被字形盖住，留个小标记便于定位 */
    ui_circle(cx, cy, 3, MARK, 1, 0);

    /* 档位名（左上角，一律 BASE 画，免得标签自己也在动）*/
    ui_set_valign(VML_VANCHOR_BASE);
    ui_set_font(20, VML_FONT_BOLD, LBL, VML_ANCHOR_LEFT);
    ui_text_cur(x + 10, y + 28, name);

    /* 被测的那个字：同一字号、同一坐标，只有竖对齐不同 */
    ui_set_font(h * 30 / 100, VML_FONT_BOLD, TXT, VML_ANCHOR_CENTER);
    ui_set_valign(valign);
    ui_text_cur(cx, cy, "中");
}

int main(void)
{
    int m[4];
    int i;
    int gap;
    int head;
    int cw;
    int ch;
    int col;
    int row;
    int t;

    W = ui_scr_w();
    H = ui_scr_h();

    ui_win_open_ex("文字竖对齐测试", W, H, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_keep_on(1);

    modes[0] = VML_VANCHOR_BASE;    names[0] = "BASE   (基线落准心)";
    modes[1] = VML_VANCHOR_MIDDLE;  names[1] = "MIDDLE (盒中心)";
    modes[2] = VML_VANCHOR_TOP;     names[2] = "TOP    (盒顶)";
    modes[3] = VML_VANCHOR_BOTTOM;  names[3] = "BOTTOM (盒底)";

    gap = 12;
    head = 76;
    cw = (W - gap * 3) / 2;
    ch = (H - head - gap * 3) / 2;

    ui_msg_clear();

    for (i = 0; i < 4; i++) {
        col = i % 2;
        row = i / 2;
        cell(gap + col * (cw + gap), head + gap + row * (ch + gap), cw, ch, modes[i], names[i]);
    }

    /* 顶部说明（用 BASE 画 —— 说明文字自己不需要居中）*/
    ui_set_valign(VML_VANCHOR_BASE);
    ui_set_font(22, VML_FONT_BOLD, TXT, VML_ANCHOR_CENTER);
    ui_text_cur(W / 2, 34, "竖对齐四档：同一个字、同一个格心");
    ui_set_font(18, 0, LBL, VML_ANCHOR_CENTER);
    ui_text_cur(W / 2, 62, "黄十字 = 准心（就是传给 ui_text 的那个 y）");

    ui_present();

    /* 只有触摸才重画：静态画面，省电。退出走窗口的返回箭头（ui_win_closed）。*/
    while (ui_win_closed() == 0) {
        t = ui_wait(m, 500);
        if (t == VML_MSG_TOUCHDOWN) {
            ui_present();       /* 重画一次，便于确认画面稳定 */
        }
    }
    return 0;
}
