/* test_font.c —— 竖对齐四档的**可视判据**（v0.96.399）
 * test_font.c — the **visual criterion** for the four vertical-align modes (v0.96.399)
 *
 * 为什么要有这个程序：竖对齐（`ui_set_valign`，状态式，四档）是新加的能力，而它的偏差
 * Why this program exists: vertical alignment (`ui_set_valign`, stateful, four modes) is a new capability, and its deviation
 * **只在真机上看得见** —— 桌面的自绘（光栅 TrueType / SVG）与手机的**平台字体**度量不同，
 * is **only visible on a real device** — the desktop self-drawn path (rasterized TrueType / SVG) and the phone's **platform font** metrics differ,
 * 构建全绿、自测全绿都证明不了"字在方框里到底居中没有"。所以把四种对齐**并排画出来**，
 * a green build and green self-tests prove nothing about whether the text is really centered in its box. So the four modes are **drawn side by side**,
 * 每格中心画一个黄色十字当**准心**，用眼睛（或截屏量像素）判。
 * with a yellow cross at each cell center as the **reference mark**, judged by eye (or by measuring pixels in a screenshot).
 *
 * 判据（四格用的是**同一个字、同一个字号、同一个 y** = 格心 ⇒ 差别只来自档位）：
 * Criterion (the four cells use **the same character, the same font size, the same y** = the cell center => the only difference is the mode):
 *
 *   BASE   —— `y` 就是**基线**：字形坐在准心上（字的底边贴着十字的横线）
 *   BASE   — `y` is the **baseline**: the glyph sits on the reference mark (its bottom edge touches the cross's horizontal line)
 *   MIDDLE —— 盒的竖直中心落在准心（"在方框/按钮里居中"用这一档）
 *   MIDDLE — the box's vertical center lands on the reference mark (use this mode to center inside a box or button)
 *   TOP    —— 盒顶落在准心（字形整体落在准心**下方**）
 *   TOP    — the box top lands on the reference mark (the glyph as a whole falls **below** the mark)
 *   BOTTOM —— 盒底落在准心（字形整体落在准心**上方**）
 *   BOTTOM — the box bottom lands on the reference mark (the glyph as a whole falls **above** the mark)
 *
 * ⚠ 盒高 = 字号（单行）⇒ BASE 与 MIDDLE 之间**理论上**差 0.3×字号、TOP/BOTTOM 各差一格。
 * ⚠ Box height = font size (single line) => BASE and MIDDLE should differ by 0.3x the font size, TOP/BOTTOM by one cell each.
 *   真机上量到的偏差就是"平台字体真实上升"与共享层那个近似值之差 —— 那条曾在算盘按键上
 *   The deviation measured on a real device is the gap between the platform font's true ascent and the shared layer's approximation — that one once showed up on the abacus buttons as
 *   表现为"文字靠下、不居中"（0.245×字号，肉眼一眼可见）。
 *   "text sitting low, not centered" (0.245x the font size, obvious at a glance).
 */
#include <waycoder_ui.h>

#define BG      0xFF10141C
#define CELL    0xFF1E2735
#define EDGE    0xFF3A4763
#define MARK    0xFFFFC83D      /* 准心十字：黄 */
                                /* Reference cross: yellow */
#define TXT     0xFFF2F6FA
#define LBL     0xFF8FA3BF

static int W;
static int H;
static int Lang;                /* 界面语言：开局查一次（ui_get_language 是 syscall，别每帧调） */
                                /* UI language: queried once at start (ui_get_language is a syscall, do not call it every frame) */

/* 四格的档位与名字（顺序 = 画出来的顺序）*/
/* The four cells' modes and names (order = the order they are drawn in) */
static int  modes[4];
static char *names[4];

/* 一格：底、边框、准心十字、一个字。`valign` 决定这个字怎么落在 (cx, cy) 上。*/
/* One cell: background, border, reference cross, one character. `valign` decides how that character lands on (cx, cy). */
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
    /* Reference cross: the horizontal line is a bit longer, so you can tell "the glyph presses on the line" from "the glyph sits on the line" */
    ui_line(cx - arm * 2, cy, cx + arm * 2, cy, MARK, 2);
    ui_line(cx, cy - arm, cx, cy + arm, MARK, 2);
    /* 准心中央一个小点：字号大时横线会被字形盖住，留个小标记便于定位 */
    /* A small dot at the center of the reference: with a large font the lines get hidden by the glyph, so a small mark is kept for locating it */
    ui_circle(cx, cy, 3, MARK, 1, 0);

    /* 档位名（左上角，一律 BASE 画，免得标签自己也在动）*/
    /* Mode name (top-left, always drawn with BASE so the label itself does not move) */
    ui_set_valign(VML_VANCHOR_BASE);
    ui_set_font(20, VML_FONT_BOLD, LBL, VML_ANCHOR_LEFT);
    ui_text_cur(x + 10, y + 28, name);

    /* 被测的那个字：同一字号、同一坐标，只有竖对齐不同 */
    /* The character under test: same font size, same coordinates, only the vertical align mode differs */
    ui_set_font(h * 30 / 100, VML_FONT_BOLD, TXT, VML_ANCHOR_CENTER);
    ui_set_valign(valign);
    ui_text_cur(cx, cy, Lang == 0 ? "中" : "M");
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
    Lang = ui_get_language();

    ui_win_open_ex(Lang == 0 ? "文字竖对齐测试" : "Vertical align test", W, H, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_keep_on(1);

    modes[0] = VML_VANCHOR_BASE;    names[0] = Lang == 0 ? "BASE   (基线落准心)" : "BASE   (baseline on mark)";
    modes[1] = VML_VANCHOR_MIDDLE;  names[1] = Lang == 0 ? "MIDDLE (盒中心)" : "MIDDLE (box center)";
    modes[2] = VML_VANCHOR_TOP;     names[2] = Lang == 0 ? "TOP    (盒顶)" : "TOP    (box top)";
    modes[3] = VML_VANCHOR_BOTTOM;  names[3] = Lang == 0 ? "BOTTOM (盒底)" : "BOTTOM (box bottom)";

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
    /* Top caption (drawn with BASE — the caption itself does not need to be centered) */
    ui_set_valign(VML_VANCHOR_BASE);
    ui_set_font(22, VML_FONT_BOLD, TXT, VML_ANCHOR_CENTER);
    ui_text_cur(W / 2, 34, Lang == 0 ? "竖对齐四档：同一个字、同一个格心" : "Four vertical-align modes: same glyph, same cell center");
    ui_set_font(18, 0, LBL, VML_ANCHOR_CENTER);
    ui_text_cur(W / 2, 62, Lang == 0 ? "黄十字 = 准心（就是传给 ui_text 的那个 y）" : "Yellow cross = the mark (the y you pass to ui_text)");

    ui_present();

    /* 只有触摸才重画：静态画面，省电。退出走窗口的返回箭头（ui_win_closed）。*/
    /* Redraw only on touch: the picture is static, so this saves power. Exit via the window's back arrow (ui_win_closed). */
    while (ui_win_closed() == 0) {
        t = ui_wait(m, 500);
        if (t == VML_MSG_TOUCHDOWN) {
            ui_present();       /* 重画一次，便于确认画面稳定 */
                                /* Redraw once, to confirm the picture is stable */
        }
    }
    return 0;
}
