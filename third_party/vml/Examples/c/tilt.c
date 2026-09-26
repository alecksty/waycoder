/* tilt.c —— 水平仪 + 小球（传感器 `ui_sensor` 的活样例）
 *
 * 玩法：**没有玩法**，这是一块"把传感器接到画面上"的演示板。
 *   · 中间一个圆盘，小球跟着手机倾斜滚到低的那一边；
 *   · 下面三行是三个传感器的实时读数（加速度 / 角速度 / 融合姿态）；
 *   · 点屏幕任意处 = **校准**（把当前姿态当水平）—— 躺着玩时"水平"是错的，靠它纠正。
 *
 * 想抄的地方（写倾斜控制只需要前两条）：
 *
 *   ① **倾斜控制用加速度计**（`VML_SENS_ACCEL`），不是陀螺仪 ——
 *      静止时它读到的是重力方向，也就是"往哪边歪"，**不漂移**。
 *      陀螺仪是角速度，要"角度"得自己积分，几十秒就漂出可用范围。
 *
 *   ② 单位是**毫克**（`1000` = 1g）：放平时 `z ≈ 1000`、`x ≈ y ≈ 0`；
 *      往右歪 30° 时 `x ≈ 500`。所以"倾斜量"直接就是 `x / 1000`，
 *      映射到画面就是 `x * 半径 / 1000`，不必自己算角度。
 *
 *   ③ **三种传感器都可能"没有"**（尤其融合姿态，没磁力计的设备上不准/没有）
 *      ⇒ 先问 `ui_sensor_available`，再决定显示什么。
 *      **读不到 ≠ 读到了 0**：前者要如实说"没有"，后者才是"放平了"。
 *
 * ⚠ 桌面（`vmlcli`）**没有这些硬件**，它靠输入脚本注入：
 *      `--input` 里写 `accel 0 0 1`（单位是 g，脚本层换算成 SI）。
 *   所以桌面上跑通证明的是**接口对**，不是**硬件对** —— 手感只能上真机。
 */

#include <waycoder_ui.h>

/* 三种传感器各要 3 个数；**每个数组单独一行**（C 前端的老约束，见 calc.c 的注释）。 */
static int sAcc[3];
static int sGyr[3];
static int sRot[3];
static int hasAcc;
static int hasGyr;
static int hasRot;

static int sw;
static int sh;
static int cx;          /* 圆盘中心 */
static int cy;
static int cr;          /* 圆盘半径 */
static int bx;          /* 小球位置（定点整数，见下面"为什么不用浮点"） */
static int by;
static int tickN;

/* 读数行画在哪 */
static int lineY;

/* ── 整数转字符串（本仓惯例：不依赖 sprintf 家族的格式化路径）────────── */
static char g_buf[24];

static char* numStr(int v)
{
    int i;
    int neg;
    int n;
    neg = 0;
    if (v < 0) { neg = 1; v = 0 - v; }
    i = 0;
    if (v == 0) { g_buf[i] = '0'; i = i + 1; }
    while (v > 0)
    {
        g_buf[i] = (char)(48 + v % 10);
        v = v / 10;
        i = i + 1;
    }
    if (neg != 0) { g_buf[i] = '-'; i = i + 1; }
    g_buf[i] = 0;
    /* 上面是倒着写的，这里翻过来 */
    n = 0;
    while (n < i / 2)
    {
        char t;
        t = g_buf[n];
        g_buf[n] = g_buf[i - 1 - n];
        g_buf[i - 1 - n] = t;
        n = n + 1;
    }
    return g_buf;
}

/* 拼一行 "x=… y=… z=…" —— 字符串拼接在本平台是坏的（会得空串），
 * 所以一段一段画，不拼。见 drawReadout()。 */

static int clampInt(int v, int lo, int hi)
{
    if (v < lo) return lo;
    if (v > hi) return hi;
    return v;
}

/* ── 绘制 ───────────────────────────────────────────────────────────── */

static void drawGauge(void)
{
    int i;
    int dotX;
    int dotY;
    int inner;

    /* 盘面 */
    ui_circle(cx, cy, cr, 0xFF1E2430, 1, 0);
    ui_circle(cx, cy, cr, 0xFF3C4658, 0, 2);
    /* 同心圈：给"偏离多少"一个参照 */
    inner = cr / 3;
    ui_circle(cx, cy, inner, 0xFF2A3346, 0, 1);
    ui_circle(cx, cy, cr * 2 / 3, 0xFF2A3346, 0, 1);
    /* 十字准线 */
    ui_line(cx - cr, cy, cx + cr, cy, 0xFF2A3346, 1);
    ui_line(cx, cy - cr, cx, cy + cr, 0xFF2A3346, 1);
    /* 中心的目标圈：小球停在这里 = 水平 */
    ui_circle(cx, cy, 14, 0xFF4ADE80, 0, 2);

    /* 小球。⚠ **不夹在圆盘里** —— 滚出盘外正是"歪得厉害"该有的样子，
     *   夹住反而让人以为"歪到某个角度就不动了"。只夹在窗口内（免得画到看不见的地方）。 */
    dotX = clampInt(bx, 14, sw - 14);
    dotY = clampInt(by, 14, sh - 14);
    ui_circle(dotX, dotY, 12, 0x60FFD166, 1, 0);       /* 半透明外圈 */
    ui_circle(dotX, dotY, 7, 0xFFFFD166, 1, 0);
    (void)i;
}

/* 一行读数：标签 + x/y/z。三个数分三段画，不拼字符串。 */
static void drawReadout(int y, char* label, int* v, int present)
{
    ui_rect(16, y - 2, sw - 32, 26, 0xFF161B24, 1, 0, 6);
    ui_text(24, y, label, present ? 0xFFB8C4D8 : 0xFF606878, 14, VML_ANCHOR_LEFT);
    if (present == 0)
    {
        ui_text(sw - 24, y, "这台设备没有", 0xFF606878, 13, VML_ANCHOR_RIGHT);
        return;
    }
    ui_text(sw / 2 - 60, y, numStr(v[0]), 0xFFFFFFFF, 14, VML_ANCHOR_LEFT);
    ui_text(sw / 2 + 6, y, numStr(v[1]), 0xFFFFFFFF, 14, VML_ANCHOR_LEFT);
    ui_text(sw / 2 + 72, y, numStr(v[2]), 0xFFFFFFFF, 14, VML_ANCHOR_LEFT);
}

static void draw(void)
{
    ui_clear(0xFF0E1218);
    drawGauge();

    drawReadout(lineY, "加速度 mg", sAcc, hasAcc);
    drawReadout(lineY + 32, "角速度 mdps", sGyr, hasGyr);
    drawReadout(lineY + 64, "姿态 mdeg", sRot, hasRot);

    if (hasAcc == 1)
    {
        ui_text(sw / 2, sh - 34, "点屏幕任意处 = 校准水平", 0xFF8A94A8, 13, VML_ANCHOR_CENTER);
    }
    else
    {
        ui_text(sw / 2, sh - 34, "没有加速度计 —— 手机端才有", 0xFFFF8A80, 13, VML_ANCHOR_CENTER);
    }
    ui_present();
}

/* ── 一拍：读传感器 + 小球跟随 ──────────────────────────────────────── */

static void step(void)
{
    tickN = tickN + 1;

    hasAcc = ui_sensor(VML_SENS_ACCEL, sAcc);
    hasGyr = ui_sensor(VML_SENS_GYRO, sGyr);
    hasRot = ui_sensor(VML_SENS_ROTATION, sRot);

    if (hasAcc == 1)
    {
        /* 目标位置：倾斜量直接映射到半径上（`x / 1000` = 倾斜角的正弦）。
         *
         * ⚠⚠ **两个轴都要取负**，这是最容易写反的一处：
         *
         *   加速度计静止时读到的是**世界"上"方向在设备坐标里的表示** ——
         *   也就是"**哪条轴朝上**"。把手机右边抬起来，`x` 会变大（读数为正）。
         *
         *   而小球是往**低**处滚的 ⇒ 球要朝读数的**反**方向走。
         *   所以 `cx - x` / `cy - y`，不是 `cx + x`。
         *
         *   （写反的症状是"歪这边、球往那边跑"，而它看起来完全像**传感器的轴装反了**，
         *   排查时容易一头扎进平台代码里 —— 其实错在这两行。）
         *
         *   ✅ **已真机确认（2026-09-26）**：把手机右边抬高，球往左滚 —— 与上面这条推导一致。
         *   桌面注入的只是数字，验的是"给定读数、球走哪边"；轴的正方向那一半是这么补上的。*/
        int tx;
        int ty;
        tx = cx - sAcc[0] * cr / 1000;
        ty = cy - sAcc[1] * cr / 1000;
        /* 一阶低通跟随（比"直接赋值"稳，也比"积分"简单 —— 积分会漂） */
        bx = bx + (tx - bx) / 3;
        by = by + (ty - by) / 3;
    }
    else
    {
        bx = cx;
        by = cy;
    }
}

int main(void)
{
    int msg[4];
    int t;

    /* 先问可用绘图区，再开窗 —— 窗口宽高就是画布的坐标空间 */
    sw = ui_scr_w();
    sh = ui_scr_h();
    if (sw <= 0) sw = 380;
    if (sh <= 0) sh = 660;

    /* 竖屏（盘面是圆的，转屏重排没意义）+ 不要手柄区（这个程序只用触摸）。 */
    ui_win_open_ex("水平仪", sw, sh, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_keep_on(1);

    cx = sw / 2;
    cy = sh / 3;
    cr = sw / 2 - 30;
    if (cr > 150) cr = 150;
    bx = cx;
    by = cy;

    lineY = cy + cr + 30;

    /* 采样率：这一屏是 30fps 刷的，60Hz 采样够；不设的话平台默认也行 ——
     * 但设一下能省电（传感器一开就在耗电，间隔越大越省）。 */
    ui_sensor_rate(VML_SENS_ACCEL, 30);
    ui_sensor_rate(VML_SENS_GYRO, 30);
    ui_sensor_rate(VML_SENS_ROTATION, 30);

    /* 第一帧之前先抓一次 —— 否则第一帧画的是"没有传感器"（盘面会闪一下）。 */
    hasAcc = ui_sensor(VML_SENS_ACCEL, sAcc);
    hasGyr = ui_sensor(VML_SENS_GYRO, sGyr);
    hasRot = ui_sensor(VML_SENS_ROTATION, sRot);

    ui_timer_set(33, 0);
    draw();

    while (ui_win_closed() == 0)
    {
        t = ui_wait(msg, 0);
        if (t == 0) continue;
        if (t == VML_MSG_WINDOWCLOSE) break;

        if (t == VML_MSG_TIMER)
        {
            step();
            draw();
            continue;
        }

        /* 点一下 = 校准（把当前姿态当零点）。触摸与鼠标都收 —— 桌面上注入的是触摸。 */
        if (t == VML_MSG_TOUCHDOWN || t == VML_MSG_MOUSEDOWN)
        {
            if (hasAcc == 1) ui_sensor_calibrate(VML_SENS_ACCEL);
            if (hasRot == 1) ui_sensor_calibrate(VML_SENS_ROTATION);
            /* 校准之后**立刻**重读一次：不重读的话校准要把球"慢慢地"拉回中心，
             * 看着像没生效（这是"用户按了没反应"的典型）。 */
            hasAcc = ui_sensor(VML_SENS_ACCEL, sAcc);
            hasRot = ui_sensor(VML_SENS_ROTATION, sRot);
            bx = cx;
            by = cy;
            draw();
            continue;
        }

        if (t == VML_MSG_KEYDOWN)
        {
            if (msg[1] == VML_KEY_ESCAPE) break;
        }
    }

    ui_keep_on(0);
    ui_win_close();
    return 0;
}
