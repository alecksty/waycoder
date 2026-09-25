/* mask_bool.c —— 蒙版**布尔运算**与**路径当蒙版**的体检：逐点取像素，不靠眼睛。
 *
 * ## 为什么必须"取像素"
 *
 * 交集、异或这类错误是"**少一块 / 多一块**"：整张图看着都对，只有某处该空的没空。
 * 盯着屏幕看是看不出来的（本仓在这上面栽过：v0.96.427 的验收判据是"出帧非空白"，
 * 而**纯背景色的帧不算空白**，于是整类缺陷都被放过去了）。
 * 这里每条判据都是 `ui_get_pixel(x,y) == 期望色`，对错由程序说，不由眼睛说。
 *
 * ## 覆盖的七条
 *
 * | # | 测什么 | 蒙版 |
 * |---|---|---|
 * | 1~3   | `SUBTRACT` 挖洞（甜甜圈） | 大圆 − 小圆 |
 * | 4~6   | `INTERSECT` 取交 | 圆 ∩ 矩形 |
 * | 7~9   | `UNION` 取并 | 两个分离的圆 |
 * | 10~12 | `XOR` 取异或 | 两个重叠的圆（重叠处应**空**）|
 * | 13~15 | `ui_path` 当蒙版（曲线展平） | 三角形路径 |
 * | 16~18 | **洞后面正好有"日/月"**（三层里的背景层）| 楼 − 洞，洞开在太阳上 |
 * | 19~22 | **蒙版当碰撞体**（`ui_mask_test`）| 同上，判"这一点在不在楼体里" |
 *
 * 采样点全都落在区域中心附近，**离边界 5px 以上** —— 判据不该被抗锯齿的边缘像素左右。
 *
 * 最后一组是**用户点名的场景**：三层模型里"建筑挖洞、背景透出来"最容易露馅的时候，
 * 就是洞后面正好压着太阳或月亮 —— 那时若还在洞里补画"当时的天空色"，
 * 一眼就能看出**圆圆的太阳被啃掉一块**。判据因此是"洞心必须是太阳色"。
 *
 * ## 用法
 *
 * 手机上：`vml run examples/c/mask_bool.c`（C 前端 + 汇编 + 链接要一分多钟，别当成卡死）。
 * 桌面上：`scripts/vmlcli Examples/c/mask_bool.c -r .`（秒级，改完前端先用它）。
 * 屏幕正中会写 `ALL PASS` 或 `FAIL`；失败时再按下面的 `报告` 弹窗看是哪几条。
 */
#include <waycoder_ui.h>

#define C  0xFF3C6EB4      /* 采样色：r/g/b 三通道互不相同，串色一眼可辨 */
#define BG 0xFFFFFFFF      /* 背景（ui_clear 用的色）*/

int passed;
int failed;
char report[80];

/* 极简整数转字符串。
 *
 * ⚠ **不调库**（不 `strcpy`/`strcat`/`itoa`）：`Lib/` 里两套栈清理约定并存，
 *   调用那些函数会让调用方的栈指针多释放一次，之后凡是靠 `pop` 取临时值的地方都读错
 *   （见 `docs/VML宿主接口.md` §9 与 CHANGELOG v0.96.175）——
 *   本程序是**判据**，判据本身不能建立在会漂的东西上。
 *   所以这里只用局部数组 + 指针寻址，一个库函数都不碰。 */
int put_num(char* p, int at, int v) {
    char tmp[12];
    int n;
    int i;
    if (v < 0) { p[at] = '-'; at = at + 1; v = 0 - v; }
    n = 0;
    if (v == 0) { tmp[0] = '0'; n = 1; }
    while (v > 0) { tmp[n] = '0' + (v % 10); n = n + 1; v = v / 10; }
    i = n - 1;
    while (i >= 0) { p[at] = tmp[i]; at = at + 1; i = i - 1; }
    return at;
}

/* 判一条：不中就把它编号追加进 `report`（形如 `F:3,7,11`）。 */
void expect(int idx, int got, int want) {
    int at;
    if (got == want) { passed = passed + 1; return; }
    failed = failed + 1;
    at = 0;
    if (failed == 1) { report[0] = 'F'; report[1] = ':'; at = 2; }
    else { report[at] = ','; at = at + 1; }
    at = put_num(report, at, idx);
    report[at] = 0;
}

int main(void) {
    int msg[8];
    int hitHole;
    int hitWall;
    int hitSky;
    int hitClear;

    passed = 0;
    failed = 0;
    report[0] = 0;

    ui_win_open("mask bool", 320, 400);
    ui_clear(BG);

    /* ══ 区域 1：甜甜圈 = 大圆 SUBTRACT 小圆（x 10..110, y 5..105）══ */
    ui_mask_begin();
    ui_circle(60, 55, 40, 0xFFFFFFFF, 1, 0);
    ui_mask_end(1);
    ui_mask_begin();
    ui_circle(60, 55, 20, 0xFFFFFFFF, 1, 0);
    ui_mask_end2(VML_MASK_SUBTRACT);
    ui_rect(10, 5, 100, 100, C, 1, 0, 0);
    ui_mask_clear();

    /* ══ 区域 2：INTERSECT —— 圆 ∩ 矩形（x 150..225, y 20..90）══
     * 圆在 (190,55) r=35，裁剪矩形是圆的**右半**那块方形 ⇒ 交集 = 圆的右半。 */
    ui_mask_begin();
    ui_circle(190, 55, 35, 0xFFFFFFFF, 1, 0);
    ui_mask_end(1);
    ui_mask_begin();
    ui_rect(190, 20, 35, 70, 0xFFFFFFFF, 1, 0, 0);
    ui_mask_end2(VML_MASK_INTERSECT);
    ui_rect(150, 20, 75, 70, C, 1, 0, 0);
    ui_mask_clear();

    /* ══ 区域 3：UNION —— 两个**分离**的圆（x 30..150, y 150..210）══ */
    ui_mask_begin();
    ui_circle(60, 180, 25, 0xFFFFFFFF, 1, 0);
    ui_mask_end(1);
    ui_mask_begin();
    ui_circle(120, 180, 25, 0xFFFFFFFF, 1, 0);
    ui_mask_end2(VML_MASK_UNION);
    ui_rect(30, 150, 120, 60, C, 1, 0, 0);
    ui_mask_clear();

    /* ══ 区域 4：XOR —— 两个**重叠**的圆（x 170..270, y 150..210）══
     * 重叠处两圆都命中 ⇒ 互相抵消 ⇒ **应该是空的**（这条最容易错：写错成并集就看不出）。 */
    ui_mask_begin();
    ui_circle(200, 180, 28, 0xFFFFFFFF, 1, 0);
    ui_mask_end(1);
    ui_mask_begin();
    ui_circle(240, 180, 28, 0xFFFFFFFF, 1, 0);
    ui_mask_end2(VML_MASK_XOR);
    ui_rect(170, 150, 100, 60, C, 1, 0, 0);
    ui_mask_clear();

    /* ══ 区域 5：`ui_path` 当蒙版（x 20..110, y 240..310）══
     * 三角形 (30,300) (100,300) (65,250)。走的是**曲线展平器**那条路 ⇒
     * 顺带证明"路径当蒙版"与"路径当图元"用的是同一个 `DrawPath.Flatten`。 */
    ui_mask_begin();
    ui_path("M30 300 L100 300 L65 250 Z", 0xFFFFFFFF, 1, 0xFFFFFFFF, "", 1, 0);
    ui_mask_end(1);
    ui_rect(20, 240, 90, 70, C, 1, 0, 0);
    ui_mask_clear();

    /* ══ 区域 6：**洞后面压着太阳**（x 20..110, y 320..400）══
     * 三层模型最容易露馅的一刻：背景层的太阳被建筑层的洞"咬掉一块"，
     * 还是**完整地从洞里露出来**。
     * ⚠ 上一版（在洞里补画"当时的天空色"）在这里会露馅：补的是一个纯色圆，
     *   正好把太阳盖成一个缺角的形状。 */
    ui_circle(60, 360, 30, 0xFFFFE060, 1, 0);              /* 太阳（背景层）*/
    ui_mask_begin();
    ui_rect(20, 320, 90, 80, 0xFFFFFFFF, 1, 0, 0);         /* 楼体轮廓 */
    ui_mask_end(1);
    ui_mask_begin();
    ui_circle(60, 360, 16, 0xFFFFFFFF, 1, 0);              /* 洞：正开在太阳上 */
    ui_mask_end2(VML_MASK_SUBTRACT);
    ui_rect(20, 320, 90, 80, 0xFF6FA8DC, 1, 0, 0);         /* 楼（蓝）*/

    /* 蒙版还生效时问一句"这些点在不在可见区域里" —— **蒙版当碰撞体**。
     * 建筑层"炸一块缺一块"之后，程序不必再自己维护一份洞的坐标表：
     * 洞口是"可见的"（子弹能穿过去），楼体是"不可见的"（打到墙了）。
     * ⚠ 必须在 ui_mask_clear() **之前**问 —— 清掉之后处处可见，三个都是 1。 */
    hitHole = ui_mask_test(60, 360);      /* 洞心：被挖掉 ⇒ 0 */
    hitWall = ui_mask_test(60, 328);      /* 楼体：可见 ⇒ 1 */
    hitSky  = ui_mask_test(300, 380);     /* 楼外：不可见 ⇒ 0 */

    ui_mask_clear();

    /* 清掉之后处处可见 —— 这条同时钉住"没有蒙版时恒为 1"的口径 */
    hitClear = ui_mask_test(300, 380);

    /* ══ 逐点判定（全部画完之后统一采样）══ */
    expect(1, ui_get_pixel(90, 55), C & 0xFFFFFF);      /* 环上 */
    expect(2, ui_get_pixel(60, 55), BG & 0xFFFFFF);     /* 洞心：挖掉了 */
    expect(3, ui_get_pixel(15, 10), BG & 0xFFFFFF);     /* 圆外（填充矩形内）*/

    expect(4, ui_get_pixel(200, 55), C & 0xFFFFFF);     /* 交集中：圆内且矩形内 */
    expect(5, ui_get_pixel(170, 55), BG & 0xFFFFFF);    /* 圆内、矩形外 ⇒ 被裁掉 */
    expect(6, ui_get_pixel(220, 25), BG & 0xFFFFFF);    /* 矩形内、圆外 ⇒ 被裁掉 */

    expect(7, ui_get_pixel(60, 180), C & 0xFFFFFF);     /* 并：左圆心 */
    expect(8, ui_get_pixel(120, 180), C & 0xFFFFFF);    /* 并：右圆心 */
    expect(9, ui_get_pixel(90, 180), BG & 0xFFFFFF);    /* 并：两圆之间（都不在）*/

    expect(10, ui_get_pixel(180, 180), C & 0xFFFFFF);   /* 异或：只在左圆 */
    expect(11, ui_get_pixel(220, 180), BG & 0xFFFFFF);  /* 异或：两圆重叠处 ⇒ 空 */
    expect(12, ui_get_pixel(260, 180), C & 0xFFFFFF);   /* 异或：只在右圆 */

    expect(13, ui_get_pixel(65, 290), C & 0xFFFFFF);    /* 三角形内 */
    expect(14, ui_get_pixel(35, 250), BG & 0xFFFFFF);   /* 三角形外（左上角）*/
    expect(15, ui_get_pixel(85, 250), BG & 0xFFFFFF);   /* 三角形外（右上角）*/

    /* 洞后面压着太阳：洞心必须是**太阳色**（0xFFE060），不能被楼的蓝盖住 */
    expect(16, ui_get_pixel(60, 360), 0xFFE060);        /* 洞心 = 太阳 */
    expect(17, ui_get_pixel(52, 366), 0xFFE060);        /* 洞内偏一点，仍是太阳 */
    expect(18, ui_get_pixel(60, 328), 0x6FA8DC);        /* 对照：洞**外**是楼体蓝 */

    /* 蒙版当碰撞体（`ui_mask_test`）—— 判据与上面三条画面判据**同源**：
     * 洞里"看得见太阳"⇒ 那一点就该报"在蒙版里"？**不** —— 这里报的是
     * "会不会画出来"。洞是挖掉的（不画楼）但**太阳透出来了**……
     * ⚠ 所以口径是**几何**的：蒙版 = "楼减洞" 这块**楼体**的形状。
     *   洞心不在楼体里 ⇒ 0；楼体上 ⇒ 1。程序据此判"打到墙了没有"。 */
    expect(19, hitHole, 0);
    expect(20, hitWall, 1);
    expect(21, hitSky, 0);
    expect(22, hitClear, 1);                            /* 清掉蒙版 ⇒ 处处可见 */

    if (failed == 0) ui_text(160, 160, "ALL PASS", 0xFF20A020, 20, VML_ANCHOR_CENTER);
    else ui_text(160, 160, "FAIL", 0xFFD02020, 20, VML_ANCHOR_CENTER);
    ui_present();

    if (failed != 0) ui_dlg_msg("mask bool", report, 0);

    while (ui_win_closed() == 0) { ui_wait(msg, 0); }
    ui_win_close();
    return failed;
}
