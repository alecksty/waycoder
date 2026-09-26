/* touch_probe.c —— 验证**桌面注入的多点触控能被 `ui_touch(slot)` 轮询读到**。
 *
 * ## 它治的是什么
 *
 * `scripts/vmlcli` 的输入脚本原先把 `touchdown/touchmove/touchup` 投成
 * `rt.PostInput(...)` —— 那只**投队列消息**，不碰 `_touchX/_touchY/_touchDown` 那三张槽位表
 * （那是 `PostTouch` 的活）。于是桌面上任何 `ui_touch(slot)` 轮询**永远读到"没按"**：
 * 多点触控程序（钢琴）在桌面**一格都驱不动**，只能上真机验 —— 而"只在真机上能验"
 * 是这个仓库最贵的一种返工。
 *
 * ## 判据（走 beep 频率编码，与 host_probe.c 同一套）
 *
 * 脚本投哪几个槽位，日志里就该出现哪几个频率：
 *
 *     10<slot>00 + x/10
 *
 * 例：`touchn_down 1 60 400` ⇒ `hz=10106`（slot 1、x=60）。
 * 判据是脚本的 `[vml-audio] tone hz=…` 序列与期望逐字节相同。
 *
 * ## ⚠ 两个 C 前端坑（本条是 calc.c 踩出来的，照抄它的纪律）
 *
 * 1. `int t[3];` **必须单独一行**声明 —— 标量与数组写在同一行会让局部数组分不到槽位，
 *    连读取指令都不生成，触摸坐标恒为 0（表现是"按键完全没反应"）。
 * 2. 一行一个变量，别写 `int x = t[0], y = t[1];`。
 */
#include <waycoder_ui.h>

int main(void)
{
    int sw;
    int sh;
    int m[4];
    int t[3];
    int i;
    int slot;
    int msgType;

    sw = ui_scr_w();
    sh = ui_scr_h();
    if (sw <= 0) sw = 360;
    if (sh <= 0) sh = 620;
    ui_win_open_ex("触摸探针", sw, sh, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);

    while (ui_win_closed() == 0) {
        /* 16ms 一拍：既当帧节拍，也当轮询周期。
         * ⚠ **不要用 ui_timer** —— 定时器消息会和触摸消息争同一个队列，
         *   而 `ui_wait` 的 timeout 天然就是"至少每 16ms 醒一次"。 */
        msgType = ui_wait(m, 16);
        if (msgType == VML_MSG_WINDOWCLOSE) break;

        for (slot = 0; slot < 6; slot++) {
            if (ui_touch(slot, t) == 0) continue;
            if (t[2] == 0) continue;
            /* 按着就报：频率编出 slot 与 x（都落在 ClampTone 的 20–20000 内）。
             * 每拍都报 ⇒ 日志里是连续的一串，正好证明"槽位状态**持续**存在"，
             * 而不只是投消息那一瞬间有。 */
            ui_beep(10000 + slot * 100 + t[0] / 10, 5);
        }
    }

    ui_win_close();
    return 0;
}
