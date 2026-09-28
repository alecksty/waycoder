/* gyro.c —— 「拧螺丝」：**转手机来拧**（陀螺仪的活样例）
 * gyro.c -- "Screwdriver": twist the phone to drive a screw (a live gyroscope sample).
 *
 * 玩法：把手机**像拧方向盘那样转**，屏幕中央的螺栓就跟着转。
 * How to play: rotate the phone like turning a steering wheel; the bolt in the center turns with it.
 * 在 20 秒内拧满 5 圈就赢；松手不转，螺纹会慢慢**回退**。
 * Turn 5 full laps within 20 seconds to win; if you stop turning, the thread slowly backs off.
 * 点屏幕任意处 = 重开。
 * Tap anywhere on the screen = restart.
 *
 * ## 为什么这个玩法适合陀螺仪（写"用传感器玩"的游戏先看这段）
 * ## Why this gameplay suits the gyroscope (read this first when writing sensor games)
 *
 * 三种传感器能拿到的量完全不同，**选错了玩法就会难受**：
 * The three sensors report completely different quantities; picking the wrong one makes the game feel bad:
 *
 *   · **陀螺仪 = 角速度**（转得多快）。它天生适合**"转圈"这个动作本身** ——
 *   · Gyroscope = angular velocity (how fast you turn). It naturally fits the act of spinning itself --
 *     玩家要的是"我拧了多少"，而这个量**累加就好**，不需要知道"我现在朝向哪"。
 *     the player wants "how much did I turn", and that quantity just accumulates; no heading is needed.
 *     这正是本游戏的做法。
 *     That is exactly what this game does.
 *
 *   · ⚠ **别用陀螺仪去算"绝对朝向"**。角速度积分出来的角度会**漂**：
 *   · Warning: do not use the gyroscope to compute an absolute heading. The integrated angle drifts:
 *     陀螺仪静止时读数并不精确为零（有偏置），积分几分钟就偏出可用范围。
 *     at rest the reading is not exactly zero (there is a bias), so a few minutes of integration leaves the usable range.
 *     本游戏只累计**短时间（20 秒）内的转动量**，漂移还没来得及咬人；
 *     This game only accumulates the rotation over a short time (20 seconds), before drift can bite;
 *     而且**回退机制**顺手把慢漂也压住了（偏置不会一直往一边跑）。
 *     and the back-off mechanism also keeps slow drift in check (the bias does not run one way forever).
 *     真要长时间稳定的朝向，用 `VML_SENS_ROTATION`（融合姿态），它不漂。
 *     For a long-term stable heading use `VML_SENS_ROTATION` (fused attitude); it does not drift.
 *
 *   · **加速度计 = 倾斜**（往哪边歪）。做"滚球 / 迷宫"那类用 `VML_SENS_ACCEL`，
 *   · Accelerometer = tilt (which way you lean). For "rolling ball / maze" games use `VML_SENS_ACCEL`,
 *     见 `tilt.c`。**那是另一个游戏**，别混着用 —— 拿加速度计做转圈会得到
 *     see `tilt.c`. That is a different game; do not mix them -- spinning with the accelerometer gives
 *     "转到某个角度就读不出来"的怪现象（越过 90° 时倾斜量的符号会翻回来）。
 *     the odd symptom of "it stops reading past a certain angle" (the tilt sign flips back beyond 90 degrees).
 *
 * ## 三个让手感成立的细节
 * ## Three details that make the feel work
 *
 *   ① **死区**：`|角速度| < DEAD` 当 0。陀螺仪静止时有偏置（几百 mdps 量级），
 *   1. Dead zone: treat `|angular velocity| < DEAD` as 0. A resting gyroscope has a bias (hundreds of mdps),
 *      不去掉的话螺栓会**自己慢慢转**，玩家以为游戏坏了。
 *      and without removing it the bolt slowly turns on its own and the player thinks the game is broken.
 *      死区同时也是"手抖不算" —— 一举两得。
 *      The dead zone also ignores hand shake -- two birds with one stone.
 *
 *   ② **回退**：不转的时候进度往回走。没有它，玩家可以慢慢蹭；
 *   2. Back-off: progress slides back while you are not turning. Without it the player could creep forward;
 *      而且"拧螺丝"这件事本身就该有回弹感。
 *      and screwing a screw should have a spring-back feel anyway.
 *
 *   ③ **用真实 dt 而不是"每拍固定 30ms"**：卡顿时才不会少转。
 *   3. Use the real dt instead of a fixed 30 ms per tick, so a stutter does not lose rotation.
 *
 * ## ⚠ 本游戏**刻意不区分转动方向**（这是设计，不是没做完）
 * ## Warning: this game deliberately ignores the rotation direction (by design, not unfinished)
 *
 * 进度按 `|角速度|` 累加 —— **转就涨，往哪边转都算**。两个理由：
 * Progress accumulates `|angular velocity|` -- turning always adds, either direction counts. Two reasons:
 *
 *   · **绕开一个桌面验不了的平台约定**。陀螺仪 z 轴的**正方向**是平台/硬件约定
 *   · It sidesteps a platform convention that cannot be verified on the desktop. The positive direction of
 *     （右手定则 + 设备坐标系），桌面上注入的只是数字，**"我往这边拧、读数是多少"
 *     the gyroscope z axis is a platform/hardware convention (right-hand rule + device frame); on the desktop
 *     只能在真机上确认**。而这里的错法是"拧了、但进度在**掉**" ——
 *     only numbers are injected, so "which way do I turn and what reading comes back" needs a real device.
 *     玩家不会觉得"我拧反了"，只会觉得**游戏坏了**。
 *     And the failure mode here is "you turned, but the progress is dropping" -- the player will not think
 *     （这一仓在 `tilt.c` 的符号上就是这么补的：先物理推导、桌面验"给定读数走哪边"、
 *     "I turned the wrong way", only that the game is broken. (That is how the sign was settled in `tilt.c`:
 *     真机补"轴的正方向"。那次推导是对的，但**那个环节绕不过去** ——
 *     physics first, a desktop check of "given this reading, which way does it move", then the device for the
 *     能不依赖它就不依赖。）
 *     positive axis direction. That derivation was correct, but the step is unavoidable -- so avoid depending on it.)
 *
 *   · **玩法本身更顺**。"拧螺丝"就是**一直往一个方向拧**，玩家不该先猜哪边对。
 *   · The gameplay is simply smoother. Screwing means turning one way the whole time; the player should not
 *     实测手把一直朝一个方向转、进度条一直涨，比"拧反了往回退"更好上手。
 *     have to guess which way first. Holding one direction and watching the bar fill is easier to pick up.
 *
 * 代价是失去"拧反了会松"的真实感 —— 但那点真实感换不来一个"可能让玩家以为
 * The cost is losing the realism of "turning the wrong way loosens it" -- but that realism is not worth
 * 游戏坏了"的风险。**要做成双向也行**，前提是先在真机上把符号确认下来。
 * the risk of the player thinking the game is broken. Two-way is fine too, once the sign is confirmed on a device.
 *
 * ## ⚠ 本平台没有 SIN/COS，也没有"旋转画布"
 * ## Warning: this platform has no SIN/COS and no canvas rotation
 *
 * 螺栓那条手把要随角度转，而 C 前端的 `sin`/`cos` 不可靠（见 `basiclib.c` 那条
 * The bolt handle must rotate with the angle, but the C frontend's `sin`/`cos` are unreliable (see the recorded
 * 有记录的缺陷）。这里用**定点整数旋转矩阵逐档递推**：从 0° 出发，每档转 10°，
 * defect in `basiclib.c`). This uses a fixed-point integer rotation matrix stepped one 10-degree step at a time
 * 递推到当前档位。`cos(10°)=985/1024`、`sin(10°)=178/1024` 是预先算好的常数，
 * from 0 degrees up to the current step. `cos(10 deg)=985/1024` and `sin(10 deg)=178/1024` are precomputed
 * 整个循环只有整数乘法与移位 —— 不碰浮点、不查表。
 * constants, so the whole loop is integer multiplies and shifts only -- no floating point, no lookup tables.
 *
 * ## 桌面上怎么验（`vmlcli` 没有陀螺仪）
 * ## How to test on the desktop (`vmlcli` has no gyroscope)
 *
 * 靠输入脚本注入**瞬时**角速度（单位：度/秒）：
 * Inject instantaneous angular velocity (in degrees per second) from an input script:
 *
 *     gyro 0 0 400        # 绕 z 轴 400 度/秒 ⇒ 每 30ms 转 12 度
 *     gyro 0 0 400        # 400 deg/s about z => 12 degrees every 30 ms
 *     gyro 0 0 0          # 停（不注就一直保持上一次的值）
 *     gyro 0 0 0          # stop (the last value sticks if you stop injecting)
 *
 * 连续注几拍就模拟出"一直在转"，注 0 就是"松手"。
 * Injecting a few ticks in a row simulates continuous turning; injecting 0 is "letting go".
 */

#include <waycoder_ui.h>

#define TARGET_MDEG  1800000   /* 目标：5 圈（5 × 360 × 1000 千分之一度） */
                               /* Goal: 5 full turns (5 x 360 x 1000 millidegrees). */
#define GAME_MS      20000     /* 限时 20 秒 */
                               /* Time limit: 20 seconds. */
#define DEAD_MDPS    1200      /* 死区：|角速度| 小于它当 0（挡静止偏置与手抖）*/
                               /* Dead zone: |angular velocity| below this counts as 0 (blocks rest bias and hand shake). */
#define BACK_MDPS    9000      /* 松手后的回退速率（千分之一度/秒 ⇒ 9 度/秒）*/
                               /* Back-off rate after letting go (millidegrees/sec => 9 deg/sec). */

/* 状态放文件级全局（C 前端的惯例，见 §3 约束 2） */
/* State lives in file-level globals (C frontend convention, see section 3 constraint 2). */
static int sw;
static int sh;
static int cx;
static int cy;
static int cr;            /* 圆盘半径 */
                          /* Disc radius. */

static int angle;         /* **累计角度**（千分之一度）—— 本游戏唯一的进度 */
                          /* Accumulated angle (millidegrees) -- the only progress value in this game. */
static int omega;         /* 最新角速度（千分之一度/秒） */
                          /* Latest angular velocity (millidegrees/sec). */
static int hasGyro;
static int playing;
static int leftMs;
static int lastTick;
static int win;
static int finalAngle;    /* 结束那一刻的角度 —— 分享要用，而 angle 会被 reset 清掉 */
                          /* Angle at the moment the round ended -- needed for sharing, while angle is cleared by reset(). */

/* 「分享成绩」按钮的几何：**在 main 里算一次，画与命中共用**（本仓的规矩 ——
 * The geometry of the "share score" button: computed once in main and shared by drawing and hit testing (repo rule --
 * 各算一遍就会出现"看着在按钮上、点下去没反应"）。
 * computing it twice gives "it looks like it is on the button but tapping does nothing").
 */
static int shX;
static int shY;
static int shW;
static int shH;
static char shareBuf[96];  /* 拼好的分享文本（见 buildShare）*/
                           /* The assembled share text (see buildShare). */

/* ── 整数工具（本仓惯例：不依赖 sprintf 家族的格式化路径）──────────── */
/* ── Integer helpers (repo convention: do not rely on the sprintf family of formatting paths) ── */
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

/* 把 s 接到 d 后面（**逐字符手写**，不依赖本平台的字符串库 ——
 * Append s after d (written character by character by hand; do not rely on this platform's string library --
 * 那个库在这条链上出过问题，而这里只需要十行）。返回新的末尾指针。
 * that library has misbehaved on this chain, and only ten lines are needed here). Returns the new end pointer. */
static char* appStr(char* d, char* s)
{
    while (*s != 0)
    {
        *d = *s;
        d = d + 1;
        s = s + 1;
    }
    *d = 0;
    return d;
}

static int iabs(int v)
{
    if (v < 0) return 0 - v;
    return v;
}

/* 角度（千分之一度）→ "x.y"（圈，一位小数）。
 * Angle (millidegrees) -> "x.y" (laps, one decimal place).
 * ⚠ 字符串拼接在本平台是坏的（会得空串），所以**原地往 out 里写**。
 * Warning: string concatenation is broken on this platform (it yields an empty string), so write into out in place. */
static void lapsStr(int mdeg, char* out)
{
    int lap10;
    int whole;
    int v;
    int i;
    char tmp[16];
    int n;
    lap10 = mdeg * 10 / 360000;
    if (lap10 < 0) lap10 = 0;
    whole = lap10 / 10;
    v = whole;
    i = 0;
    if (v == 0) { tmp[i] = '0'; i = i + 1; }
    while (v > 0) { tmp[i] = (char)(48 + v % 10); v = v / 10; i = i + 1; }
    n = 0;
    while (i > 0) { i = i - 1; out[n] = tmp[i]; n = n + 1; }
    out[n] = '.'; n = n + 1;
    out[n] = (char)(48 + (lap10 % 10)); n = n + 1;
    out[n] = 0;
}

/* ── 旋转：把 (0,-arm) 绕中心转 `mdeg` 千分之一度，得到端点 ─────────────
 * ── Rotation: rotate (0,-arm) about the center by `mdeg` millidegrees to get the endpoints ──
 *
 * 用**定点整数旋转矩阵逐档递推**（本平台没有可靠的 sin/cos，见文件头）：
 * Uses a fixed-point integer rotation matrix, one 10-degree step at a time (no reliable sin/cos here, see the header):
 * 从 0° 出发每档转 10°，递推到当前档位。
 * start at 0 degrees, rotate 10 degrees per step, up to the current step.
 * 返回时把端点写进 out[0..3]（x1,y1,x2,y2）。
 * On return the endpoints are written into out[0..3] (x1,y1,x2,y2).
 *
 * ⚠ **先归一到 [0,36) 再迭代** —— 不然转了几圈要迭代几千次，每帧都做就是白白烧 CPU。
 * Warning: normalize into [0,36) before iterating -- otherwise a few laps means thousands of iterations per frame, burning CPU for nothing.
 *   （这一条踩过：第一版直接拿总档数去循环，转到第 5 圈时每帧迭代 180 次。）
 *   (Learned the hard way: the first version looped over the total step count and iterated 180 times per frame by lap 5.)
 */
static void boltEnds(int mdeg, int arm, int* out)
{
    /* cos(10°) = 0.98481 → 1008/1024；sin(10°) = 0.17365 → 178/1024 */
    int vx = 1024;      /* 从 (1, 0) 出发 */
                        /* Start from (1, 0). */
    int vy = 0;
    int steps;
    int k;
    int nx;

    steps = (mdeg / 10000) % 36;      /* 千分之一度 → 10 度档，再归一 */
                                      /* Millidegrees -> 10-degree steps, then normalized. */
    if (steps < 0) steps = steps + 36;

    k = 0;
    while (k < steps)
    {
        nx = (vx * 1008 - vy * 178) / 1024;
        vy = (vx * 178 + vy * 1008) / 1024;
        vx = nx;
        k = k + 1;
    }

    /* 手把在圆环上：(vx,vy) 是单位向量的 1024 倍 */
    /* The handle sits on the ring: (vx,vy) is the unit vector scaled by 1024. */
    out[0] = cx - arm * vx / 1024;
    out[1] = cy - arm * vy / 1024;
    out[2] = cx + arm * vx / 1024;
    out[3] = cy + arm * vy / 1024;
}

/* 拼分享文本。⚠ **手动逐字符拼**（见 appStr）——
 * Builds the share text. Warning: assembled character by character by hand (see appStr) --
 * 本平台的字符串库在这条链上出过问题，而这里只需要三次 append。
 * this platform's string library has misbehaved on this chain, and only three appends are needed here. */
static void buildShare(void)
{
    char* p;
    char laps[16];
    lapsStr(finalAngle, laps);
    p = shareBuf;
    p = appStr(p, "我在「拧螺丝」里拧了 ");
    p = appStr(p, laps);
    p = appStr(p, " 圈！");
}

/* ── 绘制 ───────────────────────────────────────────────────────────── */
/* ── Drawing ────────────────────────────────────────────────────────── */

static void drawBolt(void)
{
    int ends[4];      /* **数组单独一行**（C 前端的老约束）*/
                      /* Arrays go on a line of their own (old C frontend constraint). */
    int arm;
    int a;
    int b;

    ui_circle(cx, cy, cr, 0xFF1E2430, 1, 0);
    ui_circle(cx, cy, cr, 0xFF3C4658, 0, 3);
    ui_circle(cx, cy, cr * 55 / 100, 0xFF2A3346, 0, 2);

    /* 随角度旋转的横杆 + 环上的手把 */
    /* The cross bar that rotates with the angle, plus the handle on the ring. */
    arm = cr * 66 / 100;
    boltEnds(angle, arm, ends);
    ui_line(ends[0], ends[1], ends[2], ends[3], 0xFFFFD166, 5);
    ui_circle(ends[2], ends[3], 13, 0xFFFF8A65, 1, 0);
    ui_circle(ends[2], ends[3], 13, 0xFFFFFFFF, 0, 2);

    /* 中心轴 */
    /* Center axle. */
    ui_circle(cx, cy, 16, 0xFF3C4658, 1, 0);
    ui_circle(cx, cy, 16, 0xFF6B7789, 0, 2);

    /* 进度条（横的）—— 不用弧：本平台没有画弧的接口，
     * Progress bar (horizontal) -- not an arc: this platform has no arc API,
     * 而横条同样直观，还省掉一堆三角函数。
     * and a bar is just as intuitive while saving a pile of trigonometry. */
    a = 30;
    b = sw - 30;
    ui_rect(a, cy + cr + 96, b - a, 14, 0xFF232A38, 1, 0, 7);
    {
        int filled;
        filled = (b - a) * angle / TARGET_MDEG;
        if (filled > b - a) filled = b - a;
        if (filled > 0) ui_rect(a, cy + cr + 96, filled, 14, 0xFF4ADE80, 1, 0, 7);
    }
}

static void draw(void)
{
    char buf[24];

    ui_clear(0xFF0E1218);

    /* 顶部：剩余秒数 */
    /* Top: seconds remaining. */
    if (playing == 1)
    {
        ui_text(sw / 2, 26, numStr(leftMs / 1000), 0xFFFFFFFF, 26, VML_ANCHOR_CENTER);
        ui_text(sw / 2 + 44, 34, "秒", 0xFF8A94A8, 15, VML_ANCHOR_CENTER);
    }

    drawBolt();

    /* 进度：拧了几圈 */
    /* Progress: how many laps have been turned. */
    lapsStr(angle, buf);
    ui_text(cx, cy + cr + 30, buf, 0xFF4ADE80, 36, VML_ANCHOR_CENTER);
    ui_text(cx, cy + cr + 62, "圈 / 目标 5.0", 0xFF8A94A8, 14, VML_ANCHOR_CENTER);

    /* 底部提示 */
    /* Bottom hints. */
    if (hasGyro == 0)
    {
        ui_text(sw / 2, sh - 74, "这台设备没有陀螺仪", 0xFFFF8A80, 16, VML_ANCHOR_CENTER);
        ui_text(sw / 2, sh - 48, "手机端才有（桌面可以用 gyro 注入模拟）", 0xFF8A94A8, 13, VML_ANCHOR_CENTER);
    }
    else if (playing == 1)
    {
        ui_text(sw / 2, sh - 74, "像拧方向盘那样转手机", 0xFFFFD166, 16, VML_ANCHOR_CENTER);
        /* 当前转速 —— 给玩家一个"我转得够不够快"的反馈。
         * Current spin rate -- gives the player feedback on whether they are turning fast enough.
         * ⚠ 报**绝对值**：进度不区分方向，转速却显示成负数会让玩家以为拧反了。
         * Warning: report the absolute value; progress ignores direction, so a negative rate would look like a wrong-way turn. */
        ui_text(sw / 2, sh - 48, numStr(iabs(omega) / 1000), 0xFF8A94A8, 14, VML_ANCHOR_CENTER);
        ui_text(sw / 2 + 56, sh - 48, "度/秒", 0xFF8A94A8, 14, VML_ANCHOR_CENTER);
    }
    else
    {
        if (win == 1) ui_text(sw / 2, sh - 150, "拧到底了！", 0xFF4ADE80, 18, VML_ANCHOR_CENTER);
        else ui_text(sw / 2, sh - 150, "时间到 —— 还差一点", 0xFFFF8A80, 18, VML_ANCHOR_CENTER);
        /* 「分享成绩」按钮（**几何在 main 里算好，画与命中共用**）。
         * The "share score" button (geometry worked out in main, shared by drawing and hit testing).
         * ⚠ 桌面没有分享面板 —— `ui_share_text` 会返回 0，游戏**照常继续**
         * Warning: the desktop has no share panel -- `ui_share_text` returns 0 and the game just carries on
         *   （0 是"这一端没有这个能力"，不是错误）。
         *   (0 means "this end has no such capability", not an error). */
        ui_rect(shX, shY, shW, shH, 0xFF2A6E3A, 1, 0, 10);
        ui_rect(shX, shY, shW, shH, 0xFF4ADE80, 0, 2, 10);
        ui_text(shX + shW / 2, shY + 14, "分享成绩", 0xFFFFFFFF, 16, VML_ANCHOR_CENTER);
        ui_text(sw / 2, sh - 46, "点别处再来一次", 0xFF8A94A8, 14, VML_ANCHOR_CENTER);
    }

    ui_present();
}

/* ── 一拍 ───────────────────────────────────────────────────────────── */
/* ── One tick ───────────────────────────────────────────────────────── */

static void step(int dtMs)
{
    int g[3];
    int w;

    if (playing != 1) return;

    /* 读陀螺仪。**只要 z 轴** —— "像拧方向盘那样转"就是绕屏幕法线转。 */
    /* Read the gyroscope. Only the z axis -- "turning like a steering wheel" means spinning about the screen normal. */
    if (ui_sensor(VML_SENS_GYRO, g) == 1) w = g[2];
    else w = 0;
    omega = w;

    /* **死区**：挡住静止偏置与手抖。⚠ 没有它螺栓会自己慢慢转。 */
    /* Dead zone: blocks the rest bias and hand shake. Warning: without it the bolt slowly turns on its own. */
    if (iabs(w) < DEAD_MDPS) w = 0;

    if (w != 0)
    {
        /* 角速度(千分之一度/秒) × 时长(ms) / 1000 = 转过的千分之一度。
         * Angular velocity (millidegrees/sec) x duration (ms) / 1000 = millidegrees turned.
         * ⚠ 用 `iabs(w)` —— **刻意不区分方向**，理由见文件头那段。
         * Warning: uses `iabs(w)` -- the direction is deliberately ignored, see the file header for why. */
        angle = angle + iabs(w) * dtMs / 1000;
    }
    else if (angle > 0)
    {
        /* 回退：松手后螺纹往回走 */
        /* Back-off: the thread slides back after you let go. */
        angle = angle - BACK_MDPS * dtMs / 1000;
        if (angle < 0) angle = 0;
    }

    leftMs = leftMs - dtMs;

    /* ⚠ 结束那一刻**先把角度存下来** —— 后面 `reset()` 会把它清掉，
     * Warning: save the angle at the moment the round ends -- `reset()` clears it later,
     *   而分享文案要用的是"这一局拧到哪"。
     *   and the share text needs "how far this round got". */
    if (angle >= TARGET_MDEG) { playing = 0; win = 1; finalAngle = angle; buildShare(); return; }
    if (leftMs <= 0) { leftMs = 0; playing = 0; win = 0; finalAngle = angle; buildShare(); }
}

static void reset(void)
{
    angle = 0;
    omega = 0;
    playing = 1;
    leftMs = GAME_MS;
    win = 0;
    lastTick = ui_tick();
}

int main(void)
{
    int msg[4];
    int t;
    int now;

    sw = ui_scr_w();
    sh = ui_scr_h();
    if (sw <= 0) sw = 380;
    if (sh <= 0) sh = 660;

    /* 竖屏 + 不要手柄区：全程靠转手机，一个键都不用。 */
    /* Portrait plus no gamepad area: everything is done by turning the phone, no keys at all. */
    ui_win_open_ex("拧螺丝", sw, sh, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_keep_on(1);

    cx = sw / 2;
    cy = sh / 2 - 50;
    cr = sw / 2 - 40;
    if (cr > 140) cr = 140;

    /* 「分享成绩」按钮的几何 —— **只在这里算一次**，画与命中都用它。 */
    /* Geometry of the "share score" button -- computed only once here, used by both drawing and hit testing. */
    shW = 200;
    if (shW > sw - 60) shW = sw - 60;
    shH = 44;
    shX = (sw - shW) / 2;
    shY = sh - 116;

    hasGyro = ui_sensor_available(VML_SENS_GYRO);
    /* 转圈是快动作，采密一点（20ms）。间隔越大越省电，但手感越钝。 */
    /* Spinning is a fast motion, so sample densely (20 ms). A longer interval saves power but feels duller. */
    ui_sensor_rate(VML_SENS_GYRO, 20);

    reset();
    buildShare();
    ui_timer_set(30, 0);
    draw();

    while (ui_win_closed() == 0)
    {
        t = ui_wait(msg, 0);
        if (t == 0) continue;
        if (t == VML_MSG_WINDOWCLOSE) break;

        if (t == VML_MSG_TIMER)
        {
            now = ui_tick();
            {
                int dt;
                int wasPlaying;
                dt = now - lastTick;
                if (dt < 1) dt = 30;
                if (dt > 200) dt = 200;    /* 卡顿一下别一口气补太多 */
                                           /* One stutter should not be compensated all at once. */
                lastTick = now;
                wasPlaying = playing;
                step(dt);
                /* ⚠ **结束后不弹框**（v0.96.498 改的）。
                 * Warning: no dialog box once the round ends (changed in v0.96.498).
                 *
                 * 原来这里弹一个 `ui_dlg_msg` 再 `reset()` —— 而弹框是**阻塞**的，
                 * Previously this popped up a `ui_dlg_msg` and then called `reset()` -- but the dialog blocks,
                 * 玩家看完点掉就直接重开了，**挂在同一屏上的「分享成绩」按钮
                 * so the player dismissed it and the game restarted at once, leaving the "share score" button
                 * 根本没有机会被点到**（画了，但永远点不着）。
                 * on the same screen with no chance of ever being tapped (drawn, but unreachable).
                 *
                 * 现在把结果留在画面上（见 draw() 的结束面板），玩家想分享就点按钮、
                 * Now the result stays on the screen (see the end panel in draw()): tap the button to share,
                 * 想重来就点别处 —— **两个动作都是他自己选的**，也没有模态打断。
                 * tap elsewhere to play again -- both actions are the player's own choice, with no modal interruption.
                 * （这也是"按钮画出来了"与"按钮能点到"是两件事的一个实例：
                 * (This is also an instance of "the button is drawn" and "the button is tappable" being two different things:
                 *   本仓记过好几回"看着在键上、点下去没反应"。）
                 *   this repo has recorded "it looks like it is on the key but tapping does nothing" several times.) */
                (void)wasPlaying;
            }
            draw();
            continue;
        }

        /* 触摸：**先判「分享」按钮，再判"点别处重开"** ——
         * Touch: test the "share" button first, then "tap elsewhere to restart" --
         * 用 `shX/shY/shW/shH` 那组数（与画按钮时**同一组**，本仓的规矩）。
         * using the `shX/shY/shW/shH` values (the very same set used when drawing the button, a repo rule). */
        if (t == VML_MSG_TOUCHDOWN || t == VML_MSG_MOUSEDOWN)
        {
            int px;
            int py;
            px = msg[1];
            py = msg[2];
            if (playing == 0 && px >= shX && px < shX + shW && py >= shY && py < shY + shH)
            {
                /* ⚠ 返回值**不看**：0 只表示"这一端没有分享面板"（桌面就是），
                 * Warning: the return value is ignored -- 0 only means "this end has no share panel" (the desktop does not),
                 *   不是错误 —— 游戏照常继续。
                 *   it is not an error, and the game carries on as usual. */
                ui_share_text(shareBuf, "拧螺丝");
                continue;
            }
            reset();
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
