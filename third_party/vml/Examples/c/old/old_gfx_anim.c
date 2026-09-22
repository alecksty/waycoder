/* old_gfx_anim.c —— 简单动画（图形界面）
 *
 * 类别：graphic
 * 兼容面：`ui_win_open` 的**事件循环** + `ui_tick`（计时）+ `ui_poll`（取输入）
 *         + 每帧 `ui_clear`/重画/`ui_present` —— 老图形程序的主循环骨架
 * 出处：自写，仿当年"反弹球"的演示程序。
 *
 * ⚠ 用 `ui_poll` 而不是 `ui_wait(msg, 0)`：**`ui_wait` 的 timeout 0 是"无限等"**
 *   而不是"不阻塞"（宿主侧 `Take(0)` → `Wait(Timeout.Infinite)`），
 *   拿它当轮询会让程序停在第一帧。要跑连续动画就用 `ui_poll` + 自己节流。
 *   （见 docs/VML宿主接口.md 与 CHANGELOG 里那条实测。）
 */
#include <waycoder_ui.h>

#define W 320
#define H 240
#define BALL 10
#define FRAMES 240          /* 跑够这么多帧就收尾 —— 示例不该永远转下去 */

int main(void)
{
    int x = 40, y = 30;
    int dx = 3, dy = 2;
    int f, msg;

    ui_win_open("老式绘图：反弹球", W, H);

    for (f = 0; f < FRAMES; f++) {
        /* 取输入（只轮询、不阻塞 —— 有键就退出）*/
        if (ui_poll(&msg) > 0) break;

        /* 物理：撞墙反弹 */
        x += dx;
        y += dy;
        if (x < BALL || x > W - BALL) { dx = -dx; x += dx; }
        if (y < BALL || y > H - BALL) { dy = -dy; y += dy; }

        /* 画这一帧 */
        ui_clear(0x101820);
        ui_circle(x, y, BALL, 0xE06C75, 1, 1);
        ui_text(8, 8, "反弹球（有键则退出）", 0xABB2BF, 14, 0);
        ui_present();
    }

    ui_win_close();
    return 0;
}
