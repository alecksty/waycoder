/* old_gfx_lines.c —— 直线/矩形/网格（图形界面）
 *
 * 类别：graphic
 * 兼容面：`ui_win_open` + `ui_line`（线宽）/ `ui_rect`（描边与填充）/ `ui_clear`
 *         / `ui_present` —— 对应 BGI 的 `line`/`rectangle`/`bar`
 * 出处：自写，仿 BGI（Turbo C `graphics.h`）教程里那种"先画坐标网格"的起手式。
 *
 * ⚠ 老图形程序当年是往显存直写（`0xA0000`）或调 BGI 的 —— 那两套本平台**有意不支持**
 *   （见 docs/老程序兼容性.md 的 C 档）。本程序用的是**本平台的绘图接口**，
 *   演示"同样的图形用 ui_* 怎么写"。
 */
#include <waycoder_ui.h>

#define W 320
#define H 240

int main(void)
{
    int i;
    int msg;

    ui_win_open("老式绘图：线/矩形", W, H);

    ui_clear(0x101820);

    /* 坐标网格：每 20 像素一条 */
    for (i = 0; i <= W; i += 20)
        ui_line(i, 0, i, H, 0x203040, 1);
    for (i = 0; i <= H; i += 20)
        ui_line(0, i, W, i, 0x203040, 1);

    /* 斜线：三种线宽 */
    ui_line(0, 0, W, H, 0xE06C75, 1);
    ui_line(0, H, W, 0, 0x98C379, 2);
    ui_line(0, H / 2, W, H / 2, 0x61AFEF, 4);

    /* 矩形：描边 + 填充 + 圆角 */
    ui_rect(20, 20, 80, 50, 0xE5C07B, 0, 2, 0);      /* 空心 */
    ui_rect(120, 20, 80, 50, 0xC678DD, 1, 1, 0);     /* 实心 */
    ui_rect(220, 20, 80, 50, 0x56B6C2, 0, 2, 12);    /* 圆角 */

    /* 坐标原点十字 */
    ui_line(W / 2 - 8, H / 2, W / 2 + 8, H / 2, 0xFFFFFF, 1);
    ui_line(W / 2, H / 2 - 8, W / 2, H / 2 + 8, 0xFFFFFF, 1);

    ui_present();
    /* ⚠ 画完不能立刻关窗：`--frame` 取的是 **`ui_present` 拍下的快照**，
     *   紧接着 `ui_win_close()` 会让"这一帧"还没被取走就随窗口一起没了
     *   （实测宿主报「没有可导出的帧（场景是空的）」）。
     *   老图形程序本来就是这个骨架：画完 → 等按键/关窗 → 退出。 */
    while (!ui_win_closed())
        ui_wait(&msg, 0);          /* timeout 0 = **无限等**（事件驱动，省电）*/
    ui_win_close();
    return 0;
}
