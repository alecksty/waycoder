/* mask_bool.c —— 蒙版**布尔运算**与**路径当蒙版**的体检：逐点取像素，不靠眼睛。
 * mask_bool.c -- a check-up for mask **boolean operations** and **using a path as a mask**: sample pixels point by point instead of using your eyes.
 *
 * ## 为什么必须"取像素"
 * ## Why "sampling pixels" is required
 *
 * 交集、异或这类错误是"**少一块 / 多一块**"：整张图看着都对，只有某处该空的没空。
 * Errors such as intersection and XOR are "**one piece missing or one piece extra**": the whole picture looks right, except that somewhere that should be empty is not.
 * 盯着屏幕看是看不出来的（本仓在这上面栽过：v0.96.427 的验收判据是"出帧非空白"，
 * You cannot spot it by staring at the screen (this repo was burned by that before: the v0.96.427 acceptance criterion was "the frame is not blank",
 * 而**纯背景色的帧不算空白**，于是整类缺陷都被放过去了）。
 * and **a frame of pure background color does not count as blank**, so a whole class of defects slipped through).
 * 这里每条判据都是 `ui_get_pixel(x,y) == 期望色`，对错由程序说，不由眼睛说。
 * Here every criterion is `ui_get_pixel(x,y) == expected color`; the program says right or wrong, not your eyes.
 *
 * ## 覆盖的七条
 * ## The seven groups covered
 *
 * | # | 测什么 | 蒙版 |
 * | # | what it tests | mask |
 * |---|---|---|
 * | 1~3   | `SUBTRACT` 挖洞（甜甜圈） | 大圆 − 小圆 |
 * | 1~3   | `SUBTRACT` punches a hole (donut) | big circle minus small circle |
 * | 4~6   | `INTERSECT` 取交 | 圆 ∩ 矩形 |
 * | 4~6   | `INTERSECT` intersects | circle and rectangle |
 * | 7~9   | `UNION` 取并 | 两个分离的圆 |
 * | 7~9   | `UNION` unites | two separate circles |
 * | 10~12 | `XOR` 取异或 | 两个重叠的圆（重叠处应**空**）|
 * | 10~12 | `XOR` exclusive-or | two overlapping circles (the overlap should be **empty**) |
 * | 13~15 | `ui_path` 当蒙版（曲线展平） | 三角形路径 |
 * | 13~15 | `ui_path` used as a mask (curve flattening) | a triangle path |
 * | 16~18 | **洞后面正好有"日/月"**（三层里的背景层）| 楼 − 洞，洞开在太阳上 |
 * | 16~18 | **the sun or moon sits right behind the hole** (the background layer of the three) | building minus hole, with the hole over the sun |
 * | 19~22 | **蒙版当碰撞体**（`ui_mask_test`）| 同上，判"这一点在不在楼体里" |
 * | 19~22 | **a mask as a collision body** (`ui_mask_test`) | the same, testing whether a point is inside the building body |
 *
 * 采样点全都落在区域中心附近，**离边界 5px 以上** —— 判据不该被抗锯齿的边缘像素左右。
 * The sample points all land near the centers of the regions, **more than 5px from any edge** -- a criterion should not be swayed by antialiased edge pixels.
 *
 * 最后一组是**用户点名的场景**：三层模型里"建筑挖洞、背景透出来"最容易露馅的时候，
 * The last group is the **scenario the user called out**: in the three-layer model, the moment when "the building has a hole and the background shows through" is most likely to give itself away
 * 就是洞后面正好压着太阳或月亮 —— 那时若还在洞里补画"当时的天空色"，
 * is when the sun or the moon sits right behind the hole -- if you then still paint the sky color of that moment inside the hole,
 * 一眼就能看出**圆圆的太阳被啃掉一块**。判据因此是"洞心必须是太阳色"。
 * you can see at once that **a bite has been taken out of the round sun**. The criterion is therefore "the hole center must be the sun color".
 *
 * ## 用法
 * ## Usage
 *
 * 手机上：`vml run examples/c/mask_bool.c`（C 前端 + 汇编 + 链接要一分多钟，别当成卡死）。
 * On a phone: `vml run examples/c/mask_bool.c` (the C front end plus assembly and linking takes over a minute; do not mistake that for a hang).
 * 桌面上：`scripts/vmlcli Examples/c/mask_bool.c -r .`（秒级，改完前端先用它）。
 * On the desktop: `scripts/vmlcli Examples/c/mask_bool.c -r .` (seconds; use this first after changing the front end).
 * 屏幕正中会写 `ALL PASS` 或 `FAIL`；失败时再按下面的 `报告` 弹窗看是哪几条。
 * The middle of the screen shows `ALL PASS` or `FAIL`; on failure, check the report dialog below to see which items failed.
 */
#include <waycoder_ui.h>

#define C  0xFF3C6EB4      /* 采样色：r/g/b 三通道互不相同，串色一眼可辨 */
                           /* Sample color: the r/g/b channels all differ, so a mix-up is obvious at a glance */
#define BG 0xFFFFFFFF      /* 背景（ui_clear 用的色）*/
                           /* Background (the color used by ui_clear) */

int passed;
int failed;
char report[80];

/* 极简整数转字符串。
 * A minimal integer-to-string converter.
 *
 * ⚠ **不调库**（不 `strcpy`/`strcat`/`itoa`）：`Lib/` 里两套栈清理约定并存，
 * ⚠ **No library calls** (no `strcpy`/`strcat`/`itoa`): two stack-cleanup conventions coexist in `Lib/`,
 *   调用那些函数会让调用方的栈指针多释放一次，之后凡是靠 `pop` 取临时值的地方都读错
 *   calling those functions makes the caller's stack pointer release one time too many, and afterwards every place that takes a temporary value with `pop` reads the wrong thing
 *   （见 `docs/VML宿主接口.md` §9 与 CHANGELOG v0.96.175）——
 *   (see the VML host-interface document, section 9, and CHANGELOG v0.96.175) --
 *   本程序是**判据**，判据本身不能建立在会漂的东西上。
 *   this program is a **criterion**, and a criterion must not be built on something that drifts.
 *   所以这里只用局部数组 + 指针寻址，一个库函数都不碰。
 *   So here we use only local arrays and pointer arithmetic, touching no library function at all.
 */
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
/* Check one item: on failure, append its number to `report` (in the form `F:3,7,11`). */
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
    /* == Region 1: a donut = big circle SUBTRACT small circle (x 10..110, y 5..105) == */
    ui_mask_begin();
    ui_circle(60, 55, 40, 0xFFFFFFFF, 1, 0);
    ui_mask_end(1);
    ui_mask_begin();
    ui_circle(60, 55, 20, 0xFFFFFFFF, 1, 0);
    ui_mask_end2(VML_MASK_SUBTRACT);
    ui_rect(10, 5, 100, 100, C, 1, 0, 0);
    ui_mask_clear();

    /* ══ 区域 2：INTERSECT —— 圆 ∩ 矩形（x 150..225, y 20..90）══
     * == Region 2: INTERSECT -- circle ∩ rectangle (x 150..225, y 20..90) ==
     * 圆在 (190,55) r=35，裁剪矩形是圆的**右半**那块方形 ⇒ 交集 = 圆的右半。
     * The circle is at (190,55) with r=35 and the clip rectangle is the **right half** square of the circle => the intersection is the right half of the circle.
     */
    ui_mask_begin();
    ui_circle(190, 55, 35, 0xFFFFFFFF, 1, 0);
    ui_mask_end(1);
    ui_mask_begin();
    ui_rect(190, 20, 35, 70, 0xFFFFFFFF, 1, 0, 0);
    ui_mask_end2(VML_MASK_INTERSECT);
    ui_rect(150, 20, 75, 70, C, 1, 0, 0);
    ui_mask_clear();

    /* ══ 区域 3：UNION —— 两个**分离**的圆（x 30..150, y 150..210）══ */
    /* == Region 3: UNION -- two **separate** circles (x 30..150, y 150..210) == */
    ui_mask_begin();
    ui_circle(60, 180, 25, 0xFFFFFFFF, 1, 0);
    ui_mask_end(1);
    ui_mask_begin();
    ui_circle(120, 180, 25, 0xFFFFFFFF, 1, 0);
    ui_mask_end2(VML_MASK_UNION);
    ui_rect(30, 150, 120, 60, C, 1, 0, 0);
    ui_mask_clear();

    /* ══ 区域 4：XOR —— 两个**重叠**的圆（x 170..270, y 150..210）══
     * == Region 4: XOR -- two **overlapping** circles (x 170..270, y 150..210) ==
     * 重叠处两圆都命中 ⇒ 互相抵消 ⇒ **应该是空的**（这条最容易错：写错成并集就看不出）。
     * In the overlap both circles match => they cancel out => it **should be empty** (this is the easiest one to get wrong: mistaking it for a union is invisible).
     */
    ui_mask_begin();
    ui_circle(200, 180, 28, 0xFFFFFFFF, 1, 0);
    ui_mask_end(1);
    ui_mask_begin();
    ui_circle(240, 180, 28, 0xFFFFFFFF, 1, 0);
    ui_mask_end2(VML_MASK_XOR);
    ui_rect(170, 150, 100, 60, C, 1, 0, 0);
    ui_mask_clear();

    /* ══ 区域 5：`ui_path` 当蒙版（x 20..110, y 240..310）══
     * == Region 5: `ui_path` used as a mask (x 20..110, y 240..310) ==
     * 三角形 (30,300) (100,300) (65,250)。走的是**曲线展平器**那条路 ⇒
     * A triangle (30,300) (100,300) (65,250). It goes through the **curve flattener** path =>
     * 顺带证明"路径当蒙版"与"路径当图元"用的是同一个 `DrawPath.Flatten`。
     * which incidentally proves that "a path as a mask" and "a path as a figure" use the same `DrawPath.Flatten`.
     */
    ui_mask_begin();
    ui_path("M30 300 L100 300 L65 250 Z", 0xFFFFFFFF, 1, 0xFFFFFFFF, "", 1, 0);
    ui_mask_end(1);
    ui_rect(20, 240, 90, 70, C, 1, 0, 0);
    ui_mask_clear();

    /* ══ 区域 6：**洞后面压着太阳**（x 20..110, y 320..400）══
     * == Region 6: **the sun sits behind the hole** (x 20..110, y 320..400) ==
     * 三层模型最容易露馅的一刻：背景层的太阳被建筑层的洞"咬掉一块"，
     * The moment the three-layer model is most likely to give itself away: whether the sun of the background layer has "a bite taken out of it" by the hole in the building layer,
     * 还是**完整地从洞里露出来**。
     * or is still **fully visible through the hole**.
     * ⚠ 上一版（在洞里补画"当时的天空色"）在这里会露馅：补的是一个纯色圆，
     * ⚠ The previous version (painting the sky color of that moment inside the hole) gives itself away here: what it painted was a solid circle,
     *   正好把太阳盖成一个缺角的形状。
     * which covers the sun into a shape with a corner missing.
     */
    ui_circle(60, 360, 30, 0xFFFFE060, 1, 0);              /* 太阳（背景层）*/
                                                           /* The sun (background layer) */
    ui_mask_begin();
    ui_rect(20, 320, 90, 80, 0xFFFFFFFF, 1, 0, 0);         /* 楼体轮廓 */
                                                           /* The building outline */
    ui_mask_end(1);
    ui_mask_begin();
    ui_circle(60, 360, 16, 0xFFFFFFFF, 1, 0);              /* 洞：正开在太阳上 */
                                                           /* The hole: cut right over the sun */
    ui_mask_end2(VML_MASK_SUBTRACT);
    ui_rect(20, 320, 90, 80, 0xFF6FA8DC, 1, 0, 0);         /* 楼（蓝）*/
                                                           /* The building (blue) */

    /* 蒙版还生效时问一句"这些点在不在可见区域里" —— **蒙版当碰撞体**。
     * While the mask is still active, ask "are these points inside the visible area" -- **a mask as a collision body**.
     * 建筑层"炸一块缺一块"之后，程序不必再自己维护一份洞的坐标表：
     * After the building layer has "chunks blown out of it", the program does not need to keep its own table of hole coordinates:
     * 洞口是"可见的"（子弹能穿过去），楼体是"不可见的"（打到墙了）。
     * the hole is "visible" (a bullet can pass through) and the building body is "not visible" (a hit on the wall).
     * ⚠ 必须在 ui_mask_clear() **之前**问 —— 清掉之后处处可见，三个都是 1。
     * ⚠ This must be asked **before** ui_mask_clear() -- after clearing, everything is visible and all three are 1.
     */
    hitHole = ui_mask_test(60, 360);      /* 洞心：被挖掉 ⇒ 0 */
                                          /* The hole center: punched out => 0 */
    hitWall = ui_mask_test(60, 328);      /* 楼体：可见 ⇒ 1 */
                                          /* The building body: visible => 1 */
    hitSky  = ui_mask_test(300, 380);     /* 楼外：不可见 ⇒ 0 */
                                          /* Outside the building: not visible => 0 */

    ui_mask_clear();

    /* 清掉之后处处可见 —— 这条同时钉住"没有蒙版时恒为 1"的口径 */
    /* After clearing, everything is visible -- this also pins down the convention that "with no mask it is always 1" */
    hitClear = ui_mask_test(300, 380);

    /* ══ 逐点判定（全部画完之后统一采样）══ */
    /* == Point-by-point checks (all sampled at once after everything is drawn) == */
    expect(1, ui_get_pixel(90, 55), C & 0xFFFFFF);      /* 环上 */
                                                      /* On the ring */
    expect(2, ui_get_pixel(60, 55), BG & 0xFFFFFF);     /* 洞心：挖掉了 */
                                                      /* The hole center: punched out */
    expect(3, ui_get_pixel(15, 10), BG & 0xFFFFFF);     /* 圆外（填充矩形内）*/
                                                      /* Outside the circle (inside the fill rectangle) */

    expect(4, ui_get_pixel(200, 55), C & 0xFFFFFF);     /* 交集中：圆内且矩形内 */
                                                      /* In the intersection: inside the circle and inside the rectangle */
    expect(5, ui_get_pixel(170, 55), BG & 0xFFFFFF);    /* 圆内、矩形外 ⇒ 被裁掉 */
                                                      /* Inside the circle but outside the rectangle => clipped away */
    expect(6, ui_get_pixel(220, 25), BG & 0xFFFFFF);    /* 矩形内、圆外 ⇒ 被裁掉 */
                                                      /* Inside the rectangle but outside the circle => clipped away */

    expect(7, ui_get_pixel(60, 180), C & 0xFFFFFF);     /* 并：左圆心 */
                                                      /* Union: the left center */
    expect(8, ui_get_pixel(120, 180), C & 0xFFFFFF);    /* 并：右圆心 */
                                                      /* Union: the right center */
    expect(9, ui_get_pixel(90, 180), BG & 0xFFFFFF);    /* 并：两圆之间（都不在）*/
                                                      /* Union: between the two circles (inside neither) */

    expect(10, ui_get_pixel(180, 180), C & 0xFFFFFF);   /* 异或：只在左圆 */
                                                      /* XOR: only in the left circle */
    expect(11, ui_get_pixel(220, 180), BG & 0xFFFFFF);  /* 异或：两圆重叠处 ⇒ 空 */
                                                      /* XOR: the overlap of the two circles => empty */
    expect(12, ui_get_pixel(260, 180), C & 0xFFFFFF);   /* 异或：只在右圆 */
                                                      /* XOR: only in the right circle */

    expect(13, ui_get_pixel(65, 290), C & 0xFFFFFF);    /* 三角形内 */
                                                      /* Inside the triangle */
    expect(14, ui_get_pixel(35, 250), BG & 0xFFFFFF);   /* 三角形外（左上角）*/
                                                      /* Outside the triangle (top left corner) */
    expect(15, ui_get_pixel(85, 250), BG & 0xFFFFFF);   /* 三角形外（右上角）*/
                                                      /* Outside the triangle (top right corner) */

    /* 洞后面压着太阳：洞心必须是**太阳色**（0xFFE060），不能被楼的蓝盖住 */
    /* The sun sits behind the hole: the hole center must be the **sun color** (0xFFE060) and must not be covered by the blue of the building */
    expect(16, ui_get_pixel(60, 360), 0xFFE060);        /* 洞心 = 太阳 */
                                                      /* The hole center = the sun */
    expect(17, ui_get_pixel(52, 366), 0xFFE060);        /* 洞内偏一点，仍是太阳 */
                                                      /* Slightly off center inside the hole, still the sun */
    expect(18, ui_get_pixel(60, 328), 0x6FA8DC);        /* 对照：洞**外**是楼体蓝 */
                                                      /* Control: **outside** the hole is the building blue */

    /* 蒙版当碰撞体（`ui_mask_test`）—— 判据与上面三条画面判据**同源**：
     * A mask as a collision body (`ui_mask_test`) -- these criteria are **of the same origin** as the three picture criteria above:
     * 洞里"看得见太阳"⇒ 那一点就该报"在蒙版里"？**不** —— 这里报的是
     * "The sun is visible" inside the hole => so that point should report "inside the mask"? **No** -- what is reported here is
     * "会不会画出来"。洞是挖掉的（不画楼）但**太阳透出来了**……
     * "whether it gets drawn". The hole is punched out (no building drawn) but **the sun shows through**...
     * ⚠ 所以口径是**几何**的：蒙版 = "楼减洞" 这块**楼体**的形状。
     * ⚠ So the convention is **geometric**: the mask is the shape of the **building body** = "building minus hole".
     *   洞心不在楼体里 ⇒ 0；楼体上 ⇒ 1。程序据此判"打到墙了没有"。
     *   The hole center is not inside the building body => 0; on the building body => 1. The program uses this to decide "did I hit a wall".
     */
    expect(19, hitHole, 0);
    expect(20, hitWall, 1);
    expect(21, hitSky, 0);
    expect(22, hitClear, 1);                            /* 清掉蒙版 ⇒ 处处可见 */
                                                      /* After clearing the mask => everything is visible */

    if (failed == 0) ui_text(160, 160, "ALL PASS", 0xFF20A020, 20, VML_ANCHOR_CENTER);
    else ui_text(160, 160, "FAIL", 0xFFD02020, 20, VML_ANCHOR_CENTER);
    ui_present();

    if (failed != 0) ui_dlg_msg("mask bool", report, 0);

    while (ui_win_closed() == 0) { ui_wait(msg, 0); }
    ui_win_close();
    return failed;
}
