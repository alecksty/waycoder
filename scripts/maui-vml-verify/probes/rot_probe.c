/* rot_probe.c —— 转屏：`ScrW/ScrH/ScrOrient` 查询 + `WINDOWORIENT`/`WINDOWRESIZE` 消息。
 *
 * ## 为什么结果要**攒到最后再打**（先 `ui_win_close()` 再 printf）
 *
 * 程序开着绘图窗口时命令行页被盖住、输出区不在 `uiautomator` 的可见树里 ——
 * 边跑边打的话驱动在这一段读不到任何东西，而"读不到"极容易被读成"没发生"。
 *
 * ## 为什么要留**整条轨迹**而不是"最后那个数"
 *
 * 只打最后一个读数的话，"转回竖屏后报的是横屏的尺寸"与"转回竖屏后报的是竖屏的尺寸"
 * 长得一模一样 —— 前者是陈旧视口（宿主把上一轮量到的值当成了真值），后者才是对的。
 * 所以这里把**每一条消息**与**每一次屏幕读数变化**都记下来，回来照 ①②③ 对。
 *
 * 声明 `VML_WIN_ROTATABLE` —— 只有这一档宿主才会在转屏时**换坐标系**并投
 * `WINDOWORIENT` + `WINDOWRESIZE`（老接口是 Legacy：跟随旋转但不动坐标系）。
 */
#include <waycoder_ui.h>

int msg[4];
int mt[16];      /* 消息类型轨迹 */
int ma[16];      /* A 槽 */
int mb[16];      /* B 槽 */
int sw[16];      /* 屏幕读数变化轨迹：宽 */
int sh[16];      /* 高 */
int so[16];      /* 方向 */
int st[16];      /* 距开始多少 ms */

int main(void)
{
    int o0, w0, h0, o1, w1, h1, r, t0, tk, nm, ns, i,
        openOk, lastw, lasth, lasto;

    o0 = ui_orientation(); w0 = ui_scr_w(); h0 = ui_scr_h();
    openOk = ui_win_open_ex("转屏探针", 300, 400, VML_WIN_ROTATABLE, VML_WIN_NEED_GAMEPAD);

    nm = 0; ns = 0; lastw = -1; lasth = -1; lasto = -1;
    t0 = ui_tick();
    while (ui_tick() - t0 < 16000) {
        r = ui_poll(msg);
        if (r != 0 && nm < 16) {
            mt[nm] = r; ma[nm] = msg[1]; mb[nm] = msg[2]; nm = nm + 1;
        }
        tk = ui_tick() - t0;
        w1 = ui_scr_w(); h1 = ui_scr_h(); o1 = ui_orientation();
        if ((w1 != lastw || h1 != lasth || o1 != lasto) && ns < 16) {
            sw[ns] = w1; sh[ns] = h1; so[ns] = o1; st[ns] = tk; ns = ns + 1;
            lastw = w1; lasth = h1; lasto = o1;
        }
    }

    ui_win_close();

    printf("R0 open=%d orient=%d w=%d h=%d\n", openOk, o0, w0, h0);
    printf("R1 msgs=%d screens=%d\n", nm, ns);
    i = 0;
    while (i < nm) { printf("M t=%d type=%d a=%d b=%d\n", i, mt[i], ma[i], mb[i]); i = i + 1; }
    i = 0;
    while (i < ns) { printf("S t=%d w=%d h=%d orient=%d\n", st[i], sw[i], sh[i], so[i]); i = i + 1; }
    printf("R2 end orient=%d w=%d h=%d\n", ui_orientation(), ui_scr_w(), ui_scr_h());
    printf("ROT-DONE\n");
    return 0;
}
