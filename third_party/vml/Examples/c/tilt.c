/* tilt.c —— 水平仪 + 小球（传感器 `ui_sensor` 的活样例）
 * tilt.c — a spirit level with a ball (a live sample of the `ui_sensor` sensor)
 *
 * 玩法：**没有玩法**，这是一块"把传感器接到画面上"的演示板。
 * Gameplay: **there is no gameplay**; this is a demo board that "wires the sensors onto the screen".
 *   · 中间一个圆盘，小球跟着手机倾斜滚到低的那一边；
 *   · A disc in the middle; the ball rolls to whichever side is lower as the phone tilts.
 *   · 下面三行是三个传感器的实时读数（加速度 / 角速度 / 融合姿态）；
 *   · Three lines below show live readings of three sensors (accel / gyro / fused rotation).
 *   · 点屏幕任意处 = **校准**（把当前姿态当水平）—— 躺着玩时"水平"是错的，靠它纠正。
 *   · Tap anywhere = **calibrate** (treat the current attitude as level) — when playing lying down "level" is wrong, and this corrects it.
 *
 * 想抄的地方（写倾斜控制只需要前两条）：
 * The parts worth copying (tilt control only needs the first two):
 *
 *   ① **倾斜控制用加速度计**（`VML_SENS_ACCEL`），不是陀螺仪 ——
 *   ① **Use the accelerometer for tilt control** (`VML_SENS_ACCEL`), not the gyroscope —
 *      静止时它读到的是重力方向，也就是"往哪边歪"，**不漂移**。
 *      at rest it reads the direction of gravity, i.e. which way you are leaning, and it **does not drift**.
 *      陀螺仪是角速度，要"角度"得自己积分，几十秒就漂出可用范围。
 *      A gyroscope gives angular rate; getting an angle means integrating, which drifts out of usable range within tens of seconds.
 *
 *   ② 单位是**毫克**（`1000` = 1g）：放平时 `z ≈ 1000`、`x ≈ y ≈ 0`；
 *   ② The unit is **milli-g** (`1000` = 1g): lying flat gives `z ≈ 1000`, `x ≈ y ≈ 0`;
 *      往右歪 30° 时 `x ≈ 500`。所以"倾斜量"直接就是 `x / 1000`，
 *      tilting 30 degrees to the right gives `x ≈ 500`. So the tilt amount is simply `x / 1000`,
 *      映射到画面就是 `x * 半径 / 1000`，不必自己算角度。
 *      mapped to the screen as `x * radius / 1000`, with no need to compute angles yourself.
 *
 *   ③ **三种传感器都可能"没有"**（尤其融合姿态，没磁力计的设备上不准/没有）
 *   ③ **Any of the three sensors may be absent** (especially fused rotation, which is inaccurate or missing without a magnetometer)
 *      ⇒ 先问 `ui_sensor_available`，再决定显示什么。
 *      ⇒ ask `ui_sensor_available` first, then decide what to display.
 *      **读不到 ≠ 读到了 0**：前者要如实说"没有"，后者才是"放平了"。
 *      **Cannot read is not the same as read 0**: the former must honestly say "absent"; only the latter means "flat".
 *
 * ⚠ 桌面（`vmlcli`）**没有这些硬件**，它靠输入脚本注入：
 * ⚠ The desktop (`vmlcli`) **has no such hardware**; it injects values from an input script:
 *      `--input` 里写 `accel 0 0 1`（单位是 g，脚本层换算成 SI）。
 *      write `accel 0 0 1` in `--input` (the unit is g; the script layer converts to SI).
 *   所以桌面上跑通证明的是**接口对**，不是**硬件对** —— 手感只能上真机。
 *   So passing on the desktop proves the **interface** is right, not the **hardware** — the feel can only be judged on a real device.
 */

#include <waycoder_ui.h>

/* 三种传感器各要 3 个数；**每个数组单独一行**（C 前端的老约束，见 calc.c 的注释）。 */
/* Each of the three sensors needs 3 values; **one array per line** (an old C front-end constraint, see the note in calc.c). */
static int sAcc[3];
static int sGyr[3];
static int sRot[3];
static int sBat[3];    /* 电量 / 充电中 / 省电模式 */
                       /* Battery level / charging / power-save mode. */
static int hasBat;
static int hasAcc;
static int hasGyr;
static int hasRot;
static int lang;        /* 界面语言：开局查一次（ui_get_language 是 syscall，别每帧调） */
                        /* UI language: queried once at start (ui_get_language is a syscall, do not call it every frame) */

static int sw;
static int sh;
static int cx;          /* 圆盘中心 */
                        /* Center of the disc. */
static int cy;
static int cr;          /* 圆盘半径 */
                        /* Radius of the disc. */
static int bx;          /* 小球位置（定点整数，见下面"为什么不用浮点"） */
                        /* Ball position (fixed-point integers, see "why not floating point" below). */
static int by;
static int tickN;

/* 读数行画在哪 */
/* Where the readout lines are drawn. */
static int lineY;

/* ── 整数转字符串（本仓惯例：不依赖 sprintf 家族的格式化路径）────────── */
/* -- Integer to string (a house convention: do not rely on the sprintf formatting path) -- */
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
    /* The digits were written back to front above; reverse them here. */
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
 * Building one line "x=… y=… z=…" — string concatenation is broken on this platform (it yields an empty string),
 * 所以一段一段画，不拼。见 drawReadout()。
 * so we draw it piece by piece instead of concatenating. See drawReadout().
 */

static int clampInt(int v, int lo, int hi)
{
    if (v < lo) return lo;
    if (v > hi) return hi;
    return v;
}

/* ── 绘制 ───────────────────────────────────────────────────────────── */
/* -- Drawing -- */

static void drawGauge(void)
{
    int i;
    int dotX;
    int dotY;
    int inner;

    /* 盘面 */
    /* The dial face. */
    ui_circle(cx, cy, cr, 0xFF1E2430, 1, 0);
    ui_circle(cx, cy, cr, 0xFF3C4658, 0, 2);
    /* 同心圈：给"偏离多少"一个参照 */
    /* Concentric rings: a reference for "how far off" you are. */
    inner = cr / 3;
    ui_circle(cx, cy, inner, 0xFF2A3346, 0, 1);
    ui_circle(cx, cy, cr * 2 / 3, 0xFF2A3346, 0, 1);
    /* 十字准线 */
    /* Crosshairs. */
    ui_line(cx - cr, cy, cx + cr, cy, 0xFF2A3346, 1);
    ui_line(cx, cy - cr, cx, cy + cr, 0xFF2A3346, 1);
    /* 中心的目标圈：小球停在这里 = 水平 */
    /* The target circle at the center: the ball resting here means level. */
    ui_circle(cx, cy, 14, 0xFF4ADE80, 0, 2);

    /* 小球。⚠ **不夹在圆盘里** —— 滚出盘外正是"歪得厉害"该有的样子，
     * The ball. ⚠ **Not clamped inside the disc** — rolling off the disc is exactly what "leaning a lot" should look like;
     *   夹住反而让人以为"歪到某个角度就不动了"。只夹在窗口内（免得画到看不见的地方）。
     *   clamping it would make people think "it stops moving past some angle". Only clamp inside the window (so it is never drawn off-screen).
     */
    dotX = clampInt(bx, 14, sw - 14);
    dotY = clampInt(by, 14, sh - 14);
    ui_circle(dotX, dotY, 12, 0x60FFD166, 1, 0);       /* 半透明外圈 */
                                                       /* Translucent outer ring. */
    ui_circle(dotX, dotY, 7, 0xFFFFD166, 1, 0);
    (void)i;
}

/* 一行读数：标签 + x/y/z。三个数分三段画，不拼字符串。 */
/* One readout line: a label plus x/y/z. The three numbers are drawn as three pieces, with no string concatenation. */
static void drawReadout(int y, char* label, int* v, int present)
{
    ui_rect(16, y - 2, sw - 32, 26, 0xFF161B24, 1, 0, 6);
    ui_text(24, y, label, present ? 0xFFB8C4D8 : 0xFF606878, 14, VML_ANCHOR_LEFT);
    if (present == 0)
    {
        ui_text(sw - 24, y, lang == 0 ? "这台设备没有" : "not present", 0xFF606878, 13, VML_ANCHOR_RIGHT);
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

    drawReadout(lineY, lang == 0 ? "加速度 mg" : "Accel mg", sAcc, hasAcc);
    drawReadout(lineY + 32, lang == 0 ? "角速度 mdps" : "Gyro mdps", sGyr, hasGyr);
    drawReadout(lineY + 64, lang == 0 ? "姿态 mdeg" : "Attitude mdeg", sRot, hasRot);

    /* 电量那一行（`ui_battery`，见 waycoder_ui.h 的 POWER 段）。
     * The battery line (`ui_battery`, see the POWER section of waycoder_ui.h).
     * ⚠ 电量变得很慢，**不必每帧查** —— 这里是 30fps 的演示，每 30 帧查一次就够，
     * ⚠ Battery level changes very slowly, so **there is no need to poll every frame** — this is a 30fps demo and once every 30 frames is plenty,
     *   顺便把"低频的东西别每帧问"这条示范出来。
     *   which also demonstrates the rule "do not ask about low-frequency things every frame".
     */
    if (tickN % 30 == 0)
    {
        hasBat = ui_battery(sBat);
        /* ⚠ 省电模式**不在** `ui_battery` 里 —— 它是另一个操作码。
         * ⚠ Power-save mode is **not** part of `ui_battery` — it is a separate opcode.
         *   忘了这一句的话 `sBat[2]` 永远是 0，而代码看着完全正常
         *   Forget this line and `sBat[2]` stays 0 forever while the code looks perfectly normal
         *   （「省电模式」那一行就是不显示，也不报错）。
         *   (the "power-save mode" line simply never appears, and no error is reported).
         */
        sBat[2] = ui_power_saver();
    }
    if (hasBat == 1)
    {
        char* p;
        ui_text(16, lineY + 100, lang == 0 ? "电量" : "Batt", 0xFFB8C4D8, 14, VML_ANCHOR_LEFT);
        p = numStr(sBat[0]);
        ui_text(64, lineY + 100, p, 0xFFFFFFFF, 14, VML_ANCHOR_LEFT);
        ui_text(120, lineY + 100, "%", 0xFFB8C4D8, 14, VML_ANCHOR_LEFT);
        if (sBat[1] != 0) ui_text(150, lineY + 100, lang == 0 ? "充电中" : "charging", 0xFF4ADE80, 13, VML_ANCHOR_LEFT);
        /* 省电模式与"电量低"**不是一回事** —— 分开显示，别混成一句。 */
        /* Power-save mode and "battery low" are **not the same thing** — show them separately, do not merge them into one line. */
        if (sBat[2] != 0) ui_text(210, lineY + 100, lang == 0 ? "省电模式" : "power save", 0xFFFFD166, 13, VML_ANCHOR_LEFT);
        /* ⚠ 电量低**且没在充电**才提醒（插着电的时候电量低是正常的）。 */
        /* ⚠ Warn only when the battery is low **and not charging** (a low level while plugged in is normal). */
        if (sBat[0] < 20 && sBat[1] == 0)
        {
            ui_text(sw / 2, lineY + 128, lang == 0 ? "电量偏低 —— 记得存档" : "Battery low - save your game", 0xFFFF8A80, 13, VML_ANCHOR_CENTER);
        }
    }
    else
    {
        ui_text(16, lineY + 100, lang == 0 ? "这台设备没有电池" : "No battery on this device", 0xFF606878, 13, VML_ANCHOR_LEFT);
    }

    if (hasAcc == 1)
    {
        ui_text(sw / 2, sh - 34, lang == 0 ? "点屏幕任意处 = 校准水平" : "Tap anywhere = calibrate level", 0xFF8A94A8, 13, VML_ANCHOR_CENTER);
    }
    else
    {
        ui_text(sw / 2, sh - 34, lang == 0 ? "没有加速度计 —— 手机端才有" : "No accelerometer - phone only", 0xFFFF8A80, 13, VML_ANCHOR_CENTER);
    }
    ui_present();
}

/* ── 一拍：读传感器 + 小球跟随 ──────────────────────────────────────── */
/* -- One tick: read the sensors and move the ball -- */

static void step(void)
{
    tickN = tickN + 1;

    hasAcc = ui_sensor(VML_SENS_ACCEL, sAcc);
    hasGyr = ui_sensor(VML_SENS_GYRO, sGyr);
    hasRot = ui_sensor(VML_SENS_ROTATION, sRot);

    if (hasAcc == 1)
    {
        /* 目标位置：倾斜量直接映射到半径上（`x / 1000` = 倾斜角的正弦）。
         * Target position: the tilt amount maps straight onto the radius (`x / 1000` = sine of the tilt angle).
         *
         * ⚠⚠ **两个轴都要取负**，这是最容易写反的一处：
         * ⚠⚠ **Both axes must be negated** — this is the easiest place to get it backwards:
         *
         *   加速度计静止时读到的是**世界"上"方向在设备坐标里的表示** ——
         *   At rest the accelerometer reads **the world "up" direction expressed in device coordinates** —
         *   也就是"**哪条轴朝上**"。把手机右边抬起来，`x` 会变大（读数为正）。
         *   that is, "**which axis points up**". Lift the right edge of the phone and `x` grows (the reading is positive).
         *
         *   而小球是往**低**处滚的 ⇒ 球要朝读数的**反**方向走。
         *   The ball however rolls toward the **low** side, so it must move in the **opposite** direction of the reading.
         *   所以 `cx - x` / `cy - y`，不是 `cx + x`。
         *   Hence `cx - x` / `cy - y`, not `cx + x`.
         *
         *   （写反的症状是"歪这边、球往那边跑"，而它看起来完全像**传感器的轴装反了**，
         *   (The symptom is "tilt this way, the ball runs the other way", which looks exactly like **the sensor axes are mounted backwards**,
         *   排查时容易一头扎进平台代码里 —— 其实错在这两行。）
         *   so you dive into the platform code — when the bug is actually in these two lines.)
         *
         *   ✅ **已真机确认（2026-09-26）**：把手机右边抬高，球往左滚 —— 与上面这条推导一致。
         *   ✅ **Confirmed on a real device (2026-09-26)**: lifting the right edge makes the ball roll left, matching the derivation above.
         *   桌面注入的只是数字，验的是"给定读数、球走哪边"；轴的正方向那一半是这么补上的。
         *   The desktop only injects numbers, which verifies "given a reading, which way the ball goes"; the axis sign half was filled in this way.
         */
        int tx;
        int ty;
        tx = cx - sAcc[0] * cr / 1000;
        ty = cy - sAcc[1] * cr / 1000;
        /* 一阶低通跟随（比"直接赋值"稳，也比"积分"简单 —— 积分会漂） */
        /* First-order low-pass follow (smoother than assigning directly, simpler than integrating — integration drifts). */
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

    lang = ui_get_language();

    /* 先问可用绘图区，再开窗 —— 窗口宽高就是画布的坐标空间 */
    /* Ask for the usable drawing area first, then open the window — the window size becomes the canvas coordinate space. */
    sw = ui_scr_w();
    sh = ui_scr_h();
    if (sw <= 0) sw = 380;
    if (sh <= 0) sh = 660;

    /* 竖屏（盘面是圆的，转屏重排没意义）+ 不要手柄区（这个程序只用触摸）。 */
    /* Portrait (the dial is round, so re-laying out on rotation is pointless) plus no gamepad area (this program uses touch only). */
    ui_win_open_ex(lang == 0 ? "水平仪" : "Level", sw, sh, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_keep_on(1);

    cx = sw / 2;
    cy = sh / 3;
    cr = sw / 2 - 30;
    if (cr > 150) cr = 150;
    bx = cx;
    by = cy;

    lineY = cy + cr + 30;

    /* 采样率：这一屏是 30fps 刷的，60Hz 采样够；不设的话平台默认也行 ——
     * Sample rate: this screen refreshes at 30fps, so 60Hz sampling is plenty; the platform default would do as well —
     * 但设一下能省电（传感器一开就在耗电，间隔越大越省）。
     * but setting it saves power (a running sensor draws current, and the longer the interval the less it costs).
     */
    ui_sensor_rate(VML_SENS_ACCEL, 30);
    ui_sensor_rate(VML_SENS_GYRO, 30);
    ui_sensor_rate(VML_SENS_ROTATION, 30);

    /* 第一帧之前先抓一次 —— 否则第一帧画的是"没有传感器"（盘面会闪一下）。 */
    /* Sample once before the first frame — otherwise the first frame shows "no sensor" (the dial would flicker). */
    hasAcc = ui_sensor(VML_SENS_ACCEL, sAcc);
    hasGyr = ui_sensor(VML_SENS_GYRO, sGyr);
    hasRot = ui_sensor(VML_SENS_ROTATION, sRot);
    hasBat = ui_battery(sBat);
    sBat[2] = ui_power_saver();

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
        /* A tap means calibrate (treat the current attitude as the zero point). Both touch and mouse are accepted — the desktop injects touch. */
        if (t == VML_MSG_TOUCHDOWN || t == VML_MSG_MOUSEDOWN)
        {
            if (hasAcc == 1) ui_sensor_calibrate(VML_SENS_ACCEL);
            if (hasRot == 1) ui_sensor_calibrate(VML_SENS_ROTATION);
            /* 校准之后**立刻**重读一次：不重读的话校准要把球"慢慢地"拉回中心，
             * Re-read **immediately** after calibrating: without the re-read, calibration only pulls the ball slowly back to the center,
             * 看着像没生效（这是"用户按了没反应"的典型）。
             * which looks like nothing happened (the classic "the user pressed and got no response" case).
             */
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
