/* old_gfx_lines.c —— 直线/矩形/网格（图形界面）
 * old_gfx_lines.c -- lines / rectangles / grid (graphical window)
 *
 * 类别：graphic
 * Category: graphic
 * 兼容面：`ui_win_open` + `ui_line`（线宽）/ `ui_rect`（描边与填充）/ `ui_clear`
 * Compatibility: `ui_win_open` + `ui_line` (line width) / `ui_rect` (stroke and fill) / `ui_clear`
 *         / `ui_present` —— 对应 BGI 的 `line`/`rectangle`/`bar`
 *         / `ui_present` -- the counterpart of BGI's `line` / `rectangle` / `bar`
 * 出处：自写，仿 BGI（Turbo C `graphics.h`）教程里那种"先画坐标网格"的起手式。
 * Origin: self-written, imitating the "draw the coordinate grid first" opening move of BGI (Turbo C `graphics.h`) tutorials.
 *
 * ⚠ 老图形程序当年是往显存直写（`0xA0000`）或调 BGI 的 —— 那两套本平台**有意不支持**
 * ⚠ Old graphics programs used to write straight into video memory (`0xA0000`) or call BGI -- this platform **deliberately supports neither**
 *   （见 docs/老程序兼容性.md 的 C 档）。本程序用的是**本平台的绘图接口**，
 *   (see the C tier of the old-program compatibility doc). This program uses **this platform's own drawing interface**,
 *   演示"同样的图形用 ui_* 怎么写"。
 *   demonstrating "how to write the same shapes with ui_*".
 */
#include <waycoder_ui.h>

#define W 320
#define H 240

int main(void)
{
    int i;
    int msg;
    int lang;   /* 界面语言：开局查一次 */
                /* UI language: queried once at start */

    lang = ui_get_language();
    ui_win_open(lang == 0 ? "老式绘图：线/矩形" : "Old-style gfx: lines / rectangles", W, H);

    ui_clear(0x101820);

    /* 坐标网格：每 20 像素一条 */
    /* Coordinate grid: one line every 20 pixels */
    for (i = 0; i <= W; i += 20)
        ui_line(i, 0, i, H, 0x203040, 1);
    for (i = 0; i <= H; i += 20)
        ui_line(0, i, W, i, 0x203040, 1);

    /* 斜线：三种线宽 */
    /* Diagonal lines: three line widths */
    ui_line(0, 0, W, H, 0xE06C75, 1);
    ui_line(0, H, W, 0, 0x98C379, 2);
    ui_line(0, H / 2, W, H / 2, 0x61AFEF, 4);

    /* 矩形：描边 + 填充 + 圆角 */
    /* Rectangles: stroked + filled + rounded */
    ui_rect(20, 20, 80, 50, 0xE5C07B, 0, 2, 0);      /* 空心 */
                                                     /* Hollow */
    ui_rect(120, 20, 80, 50, 0xC678DD, 1, 1, 0);     /* 实心 */
                                                     /* Filled */
    ui_rect(220, 20, 80, 50, 0x56B6C2, 0, 2, 12);    /* 圆角 */
                                                     /* Rounded corners */

    /* 坐标原点十字 */
    /* Cross marking the coordinate origin */
    ui_line(W / 2 - 8, H / 2, W / 2 + 8, H / 2, 0xFFFFFF, 1);
    ui_line(W / 2, H / 2 - 8, W / 2, H / 2 + 8, 0xFFFFFF, 1);

    ui_present();
    /* ⚠ 画完不能立刻关窗：`--frame` 取的是 **`ui_present` 拍下的快照**，
     * ⚠ Do not close the window right after drawing: `--frame` takes **the snapshot captured by `ui_present`**,
     *   紧接着 `ui_win_close()` 会让"这一帧"还没被取走就随窗口一起没了
     *   closing with `ui_win_close()` right after would make "this frame" vanish with the window before it is taken
     *   （实测宿主报「没有可导出的帧（场景是空的）」）。
     *   (measured: the host reported "no frame available to export (the scene is empty)").
     *   老图形程序本来就是这个骨架：画完 → 等按键/关窗 → 退出。
     *   The old graphics skeleton is exactly this: draw -> wait for a key / window close -> exit.
     */
    while (!ui_win_closed())
        ui_wait(&msg, 0);          /* timeout 0 = **无限等**（事件驱动，省电）*/
                                   /* timeout 0 = **wait forever** (event driven, saves power) */
    ui_win_close();
    return 0;
}
