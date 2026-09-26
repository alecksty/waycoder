/* gyro.c —— 「拧螺丝」：**转手机来拧**（陀螺仪的活样例）
 *
 * 玩法：把手机**像拧方向盘那样转**，屏幕中央的螺栓就跟着转。
 * 在 20 秒内拧满 5 圈就赢；松手不转，螺纹会慢慢**回退**。
 * 点屏幕任意处 = 重开。
 *
 * ## 为什么这个玩法适合陀螺仪（写"用传感器玩"的游戏先看这段）
 *
 * 三种传感器能拿到的量完全不同，**选错了玩法就会难受**：
 *
 *   · **陀螺仪 = 角速度**（转得多快）。它天生适合**"转圈"这个动作本身** ——
 *     玩家要的是"我拧了多少"，而这个量**累加就好**，不需要知道"我现在朝向哪"。
 *     这正是本游戏的做法。
 *
 *   · ⚠ **别用陀螺仪去算"绝对朝向"**。角速度积分出来的角度会**漂**：
 *     陀螺仪静止时读数并不精确为零（有偏置），积分几分钟就偏出可用范围。
 *     本游戏只累计**短时间（20 秒）内的转动量**，漂移还没来得及咬人；
 *     而且**回退机制**顺手把慢漂也压住了（偏置不会一直往一边跑）。
 *     真要长时间稳定的朝向，用 `VML_SENS_ROTATION`（融合姿态），它不漂。
 *
 *   · **加速度计 = 倾斜**（往哪边歪）。做"滚球 / 迷宫"那类用 `VML_SENS_ACCEL`，
 *     见 `tilt.c`。**那是另一个游戏**，别混着用 —— 拿加速度计做转圈会得到
 *     "转到某个角度就读不出来"的怪现象（越过 90° 时倾斜量的符号会翻回来）。
 *
 * ## 三个让手感成立的细节
 *
 *   ① **死区**：`|角速度| < DEAD` 当 0。陀螺仪静止时有偏置（几百 mdps 量级），
 *      不去掉的话螺栓会**自己慢慢转**，玩家以为游戏坏了。
 *      死区同时也是"手抖不算" —— 一举两得。
 *
 *   ② **回退**：不转的时候进度往回走。没有它，玩家可以慢慢蹭；
 *      而且"拧螺丝"这件事本身就该有回弹感。
 *
 *   ③ **用真实 dt 而不是"每拍固定 30ms"**：卡顿时才不会少转。
 *
 * ## ⚠ 本游戏**刻意不区分转动方向**（这是设计，不是没做完）
 *
 * 进度按 `|角速度|` 累加 —— **转就涨，往哪边转都算**。两个理由：
 *
 *   · **绕开一个桌面验不了的平台约定**。陀螺仪 z 轴的**正方向**是平台/硬件约定
 *     （右手定则 + 设备坐标系），桌面上注入的只是数字，**"我往这边拧、读数是多少"
 *     只能在真机上确认**。而这里的错法是"拧了、但进度在**掉**" ——
 *     玩家不会觉得"我拧反了"，只会觉得**游戏坏了**。
 *     （这一仓在 `tilt.c` 的符号上就是这么补的：先物理推导、桌面验"给定读数走哪边"、
 *     真机补"轴的正方向"。那次推导是对的，但**那个环节绕不过去** ——
 *     能不依赖它就不依赖。）
 *
 *   · **玩法本身更顺**。"拧螺丝"就是**一直往一个方向拧**，玩家不该先猜哪边对。
 *     实测手把一直朝一个方向转、进度条一直涨，比"拧反了往回退"更好上手。
 *
 * 代价是失去"拧反了会松"的真实感 —— 但那点真实感换不来一个"可能让玩家以为
 * 游戏坏了"的风险。**要做成双向也行**，前提是先在真机上把符号确认下来。
 *
 * ## ⚠ 本平台没有 SIN/COS，也没有"旋转画布"
 *
 * 螺栓那条手把要随角度转，而 C 前端的 `sin`/`cos` 不可靠（见 `basiclib.c` 那条
 * 有记录的缺陷）。这里用**定点整数旋转矩阵逐档递推**：从 0° 出发，每档转 10°，
 * 递推到当前档位。`cos(10°)=985/1024`、`sin(10°)=178/1024` 是预先算好的常数，
 * 整个循环只有整数乘法与移位 —— 不碰浮点、不查表。
 *
 * ## 桌面上怎么验（`vmlcli` 没有陀螺仪）
 *
 * 靠输入脚本注入**瞬时**角速度（单位：度/秒）：
 *
 *     gyro 0 0 400        # 绕 z 轴 400 度/秒 ⇒ 每 30ms 转 12 度
 *     gyro 0 0 0          # 停（不注就一直保持上一次的值）
 *
 * 连续注几拍就模拟出"一直在转"，注 0 就是"松手"。
 */

#include <waycoder_ui.h>

#define TARGET_MDEG  1800000   /* 目标：5 圈（5 × 360 × 1000 千分之一度） */
#define GAME_MS      20000     /* 限时 20 秒 */
#define DEAD_MDPS    1200      /* 死区：|角速度| 小于它当 0（挡静止偏置与手抖）*/
#define BACK_MDPS    9000      /* 松手后的回退速率（千分之一度/秒 ⇒ 9 度/秒）*/

/* 状态放文件级全局（C 前端的惯例，见 §3 约束 2） */
static int sw;
static int sh;
static int cx;
static int cy;
static int cr;            /* 圆盘半径 */

static int angle;         /* **累计角度**（千分之一度）—— 本游戏唯一的进度 */
static int omega;         /* 最新角速度（千分之一度/秒） */
static int hasGyro;
static int playing;
static int leftMs;
static int lastTick;
static int win;

/* ── 整数工具（本仓惯例：不依赖 sprintf 家族的格式化路径）──────────── */
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

static int iabs(int v)
{
    if (v < 0) return 0 - v;
    return v;
}

/* 角度（千分之一度）→ "x.y"（圈，一位小数）。
 * ⚠ 字符串拼接在本平台是坏的（会得空串），所以**原地往 out 里写**。 */
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
 *
 * 用**定点整数旋转矩阵逐档递推**（本平台没有可靠的 sin/cos，见文件头）：
 * 从 0° 出发每档转 10°，递推到当前档位。
 * 返回时把端点写进 out[0..3]（x1,y1,x2,y2）。
 *
 * ⚠ **先归一到 [0,36) 再迭代** —— 不然转了几圈要迭代几千次，每帧都做就是白白烧 CPU。
 *   （这一条踩过：第一版直接拿总档数去循环，转到第 5 圈时每帧迭代 180 次。）
 */
static void boltEnds(int mdeg, int arm, int* out)
{
    /* cos(10°) = 0.98481 → 1008/1024；sin(10°) = 0.17365 → 178/1024 */
    int vx = 1024;      /* 从 (1, 0) 出发 */
    int vy = 0;
    int steps;
    int k;
    int nx;

    steps = (mdeg / 10000) % 36;      /* 千分之一度 → 10 度档，再归一 */
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
    out[0] = cx - arm * vx / 1024;
    out[1] = cy - arm * vy / 1024;
    out[2] = cx + arm * vx / 1024;
    out[3] = cy + arm * vy / 1024;
}

/* ── 绘制 ───────────────────────────────────────────────────────────── */

static void drawBolt(void)
{
    int ends[4];      /* **数组单独一行**（C 前端的老约束）*/
    int arm;
    int a;
    int b;

    ui_circle(cx, cy, cr, 0xFF1E2430, 1, 0);
    ui_circle(cx, cy, cr, 0xFF3C4658, 0, 3);
    ui_circle(cx, cy, cr * 55 / 100, 0xFF2A3346, 0, 2);

    /* 随角度旋转的横杆 + 环上的手把 */
    arm = cr * 66 / 100;
    boltEnds(angle, arm, ends);
    ui_line(ends[0], ends[1], ends[2], ends[3], 0xFFFFD166, 5);
    ui_circle(ends[2], ends[3], 13, 0xFFFF8A65, 1, 0);
    ui_circle(ends[2], ends[3], 13, 0xFFFFFFFF, 0, 2);

    /* 中心轴 */
    ui_circle(cx, cy, 16, 0xFF3C4658, 1, 0);
    ui_circle(cx, cy, 16, 0xFF6B7789, 0, 2);

    /* 进度条（横的）—— 不用弧：本平台没有画弧的接口，
     * 而横条同样直观，还省掉一堆三角函数。 */
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
    if (playing == 1)
    {
        ui_text(sw / 2, 26, numStr(leftMs / 1000), 0xFFFFFFFF, 26, VML_ANCHOR_CENTER);
        ui_text(sw / 2 + 44, 34, "秒", 0xFF8A94A8, 15, VML_ANCHOR_CENTER);
    }

    drawBolt();

    /* 进度：拧了几圈 */
    lapsStr(angle, buf);
    ui_text(cx, cy + cr + 30, buf, 0xFF4ADE80, 36, VML_ANCHOR_CENTER);
    ui_text(cx, cy + cr + 62, "圈 / 目标 5.0", 0xFF8A94A8, 14, VML_ANCHOR_CENTER);

    /* 底部提示 */
    if (hasGyro == 0)
    {
        ui_text(sw / 2, sh - 74, "这台设备没有陀螺仪", 0xFFFF8A80, 16, VML_ANCHOR_CENTER);
        ui_text(sw / 2, sh - 48, "手机端才有（桌面可以用 gyro 注入模拟）", 0xFF8A94A8, 13, VML_ANCHOR_CENTER);
    }
    else if (playing == 1)
    {
        ui_text(sw / 2, sh - 74, "像拧方向盘那样转手机", 0xFFFFD166, 16, VML_ANCHOR_CENTER);
        /* 当前转速 —— 给玩家一个"我转得够不够快"的反馈。
         * ⚠ 报**绝对值**：进度不区分方向，转速却显示成负数会让玩家以为拧反了。 */
        ui_text(sw / 2, sh - 48, numStr(iabs(omega) / 1000), 0xFF8A94A8, 14, VML_ANCHOR_CENTER);
        ui_text(sw / 2 + 56, sh - 48, "度/秒", 0xFF8A94A8, 14, VML_ANCHOR_CENTER);
    }
    else
    {
        if (win == 1) ui_text(sw / 2, sh - 74, "拧到底了！", 0xFF4ADE80, 18, VML_ANCHOR_CENTER);
        else ui_text(sw / 2, sh - 74, "时间到 —— 还差一点", 0xFFFF8A80, 18, VML_ANCHOR_CENTER);
        ui_text(sw / 2, sh - 46, "点屏幕再来一次", 0xFF8A94A8, 14, VML_ANCHOR_CENTER);
    }

    ui_present();
}

/* ── 一拍 ───────────────────────────────────────────────────────────── */

static void step(int dtMs)
{
    int g[3];
    int w;

    if (playing != 1) return;

    /* 读陀螺仪。**只要 z 轴** —— "像拧方向盘那样转"就是绕屏幕法线转。 */
    if (ui_sensor(VML_SENS_GYRO, g) == 1) w = g[2];
    else w = 0;
    omega = w;

    /* **死区**：挡住静止偏置与手抖。⚠ 没有它螺栓会自己慢慢转。 */
    if (iabs(w) < DEAD_MDPS) w = 0;

    if (w != 0)
    {
        /* 角速度(千分之一度/秒) × 时长(ms) / 1000 = 转过的千分之一度。
         * ⚠ 用 `iabs(w)` —— **刻意不区分方向**，理由见文件头那段。 */
        angle = angle + iabs(w) * dtMs / 1000;
    }
    else if (angle > 0)
    {
        /* 回退：松手后螺纹往回走 */
        angle = angle - BACK_MDPS * dtMs / 1000;
        if (angle < 0) angle = 0;
    }

    leftMs = leftMs - dtMs;

    if (angle >= TARGET_MDEG) { playing = 0; win = 1; return; }
    if (leftMs <= 0) { leftMs = 0; playing = 0; win = 0; }
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
    ui_win_open_ex("拧螺丝", sw, sh, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_keep_on(1);

    cx = sw / 2;
    cy = sh / 2 - 50;
    cr = sw / 2 - 40;
    if (cr > 140) cr = 140;

    hasGyro = ui_sensor_available(VML_SENS_GYRO);
    /* 转圈是快动作，采密一点（20ms）。间隔越大越省电，但手感越钝。 */
    ui_sensor_rate(VML_SENS_GYRO, 20);

    reset();
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
                lastTick = now;
                wasPlaying = playing;
                step(dt);
                if (wasPlaying == 1 && playing == 0)
                {
                    /* 刚结束：**先把终局画面画出来再弹框** —— 弹框会盖住画面，
                     * 不先画一帧的话玩家看不到自己拧到哪儿。 */
                    draw();
                    if (win == 1) ui_dlg_msg("拧螺丝", "拧到底了！", VML_DLG_INFO);
                    else ui_dlg_msg("拧螺丝", "时间到，还差一点。", VML_DLG_INFO);
                    reset();
                }
            }
            draw();
            continue;
        }

        /* 点一下 = 重开（玩到一半也能重来） */
        if (t == VML_MSG_TOUCHDOWN || t == VML_MSG_MOUSEDOWN)
        {
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
