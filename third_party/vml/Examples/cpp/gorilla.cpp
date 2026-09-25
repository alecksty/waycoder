// 大猩猩扔香蕉 —— 用 **C++ 的类**写
//
// 这个例子的定位与同目录的 `snake.cpp` 不同：那一个是"能用就行"的写法（状态全塞进
// 一个文件级数组、辅助函数一律无参）。**这个例子是拿来压 C++ 前端的类支持的** ——
// 用户的原话是「尽量使用类对象，刚好测试类的功能」，所以这里刻意把每一样东西
// 都做成对象：天空、楼房、猴子、香蕉、风、状态栏、以及总控。
//
// 用到并因此被验证的类特性：
//   · 构造 / 析构以外的一切：字段、成员方法、`this->`、隐式字段名
//   · **多参构造函数**（`Ape(0)` / `Building(px, pw, ph, gy)`）
//   · **继承**（`Ape`/`Building`/`Banana`/`Sky` 都派生自 `Entity`）+ **虚函数派发**
//   · **基类指针数组**（`Entity* actors[8]`）---- 画图时多态派发到各自的 `Draw()`
//   · 对象数组、对象里嵌对象、对象互相调用、复合赋值与自增
//
// ## 前端的四条限制（本文件的写法是为绕开它们，不是风格选择）
//
//   ① **类里不能有数组字段**（`int data[4];` 直接解析错误「期望 SEMICOLON」）
//      ⇒ 香蕉的尾迹用**文件级数组**（`trailX/trailY`）。
//   ② **方法不能返回指针类型**（`Chunk* InnerRef()` 解析错误）⇒ 一律返回 int / void。
//   ③ **`cout << 对象的字段` 打不出东西**（打出来是空的）⇒ 要印就先存进局部量。
//   ④ **临时对象赋值**（`a0 = Ape(1, 2);`）不生效 ⇒ 用 `Setup(...)` 这样的成员方法改状态。
//
// ## 操作
//
//   ← → 调角度，↑ ↓ 调力度，回车 / 空格 / A 发射；按住会连发。
//   ESC 退出；窗口被关也退出。
//
// ## 跑法
//
//   手机：`vml run examples/cpp/gorilla.cpp`
//   桌面：`vmlcli Examples/cpp/gorilla.cpp --frames /tmp/fr`

#include <waycoder_ui.h>

// ════════════════════════════════════════════════════════════════════
// 定点数学
//
// 全程**整数**，不碰 float/double：这个 VM 的浮点是另一套寄存器（F/D），
// 而弹道只需要 1/256 像素的精度，整数定点足够而且没有取整漂移。
//
//   位置与速度的单位都是 **1/256 像素**（下称"定点单位"）。
//   画的时候 `bx / FP` 就是像素 —— 除法在**绘制时**做一次，物理里一次不做。
// ════════════════════════════════════════════════════════════════════

#define FP       256      // 定点标度：256 = 1 像素
#define GRAV     142      // 每拍重力增量（定点单位）
#define V_UNIT   43       // 力度 1 点 = 43 定点单位/拍（力度 100 → 4300）
#define TICK_MS  33       // 物理节拍（≈30fps）
#define SIN_SCALE 1000    // 正弦表的值域：1000 = 1.0

#define MAX_TRAIL 96      // 尾迹点数上限

// 回合状态
#define ST_AIM   0
#define ST_FLY   1
#define ST_BOOM  2        // 爆炸定格

// ════════════════════════════════════════════════════════════════════
// 整数三角函数（Bhaskara I 近似）
//
//   sin(a°) ≈ 4a(180−a) / (40500 − a(180−a))     —— 0..180 度内误差 < 0.0016
//
// 用近似而不是查表：表要能手写 91 项（写错一项就是"某个角度永远打不中"，
// 而且看不出来），这公式三行就完了，精度对弹道绰绰有余。
// 全程整数 ⇒ 定点标度 1000。
// ════════════════════════════════════════════════════════════════════

int isin(int deg)
{
    int u;
    if (deg < 0) { deg = -deg; }
    deg = deg % 360;
    if (deg > 180) { deg = deg - 180; }      // sin(x) = sin(180-x)，先折到 0..180
    u = deg * (180 - deg);                   // 0 .. 8100
    return 4 * u * SIN_SCALE / (40500 - u);
}

int icos(int deg)
{
    return isin(90 - deg);                   // cos(x) = sin(90-x)
}

int iabs(int v)
{
    if (v < 0) { return -v; }
    return v;
}

// 整数 → 字符串（不引 std::string；`sprintf` 在各前端形态不一）
static char g_num[12];

char* numstr(int v)
{
    int n = 0;
    int m = 0;
    char tmp[12];
    if (v < 0) { g_num[0] = '-'; v = -v; n = 1; }
    if (v == 0) { g_num[n] = '0'; g_num[n + 1] = 0; return g_num; }
    while (v > 0) { tmp[m] = (char)('0' + v % 10); v = v / 10; m = m + 1; }
    while (m > 0) { m = m - 1; g_num[n] = tmp[m]; n = n + 1; }
    g_num[n] = 0;
    return g_num;
}

// ════════════════════════════════════════════════════════════════════
// 香蕉尾迹（见文件头 ①：类里放不了数组）
// ════════════════════════════════════════════════════════════════════

static int trailX[MAX_TRAIL];
static int trailY[MAX_TRAIL];

// 按序号访问具名字段的指针表（见 `Game` 里那段注释：类里放不了数组字段）
static Building* BL[4];
static Ape* AP[2];

// ════════════════════════════════════════════════════════════════════
// 颜色（0xAARRGGBB；VM 的 int 是有符号的，超过 0x7FFFFFFF 的写成十进制负数）
// ════════════════════════════════════════════════════════════════════

#define C_SKY_DAY1  0xFF3A78C8
#define C_SKY_DAY2  0xFFA8D4F0
#define C_SKY_NIT1  0xFF0A1230
#define C_SKY_NIT2  0xFF243056
#define C_STAR      0xFFFFFFFF
#define C_SUN       0xFFFFE060
#define C_MOON      0xFFE8E8F0
#define C_BLDG_A    0xFF6E5A46
#define C_BLDG_B    0xFF8A7258
#define C_BLDG_C    0xFF5A6E7A
#define C_BLDG_D    0xFF7A5A6E
#define C_WIN_LIT   0xFFFFD070
#define C_WIN_DARK  0xFF2A2A38
#define C_GROUND    0xFF3C5A34
#define C_APE0      0xFFE08A3C
#define C_APE1      0xFFC758D6
#define C_APE_DARK  0xFF3A2A1A
#define C_BANANA    0xFFFFE070
#define C_TRAIL     0xFFFFF0B0
#define C_HUD_BG    0xCC101020
#define C_TEXT      0xFFE8E8F0
#define C_TEXT_DIM  0xFF9AA0B0
#define C_WIND      0xFF8AD4FF
#define C_BOOM      0xFFFF7020

// ════════════════════════════════════════════════════════════════════
// Entity —— 所有能在屏幕上画自己的东西的基类
//
// 只放两个字段（屏幕坐标）+ 一个**虚**的 Draw()。放基类指针数组里逐个调 Draw()
// 时，走的就是虚表派发（对象第 0 个字是 vptr，槽号在整条继承链上稳定）。
// ════════════════════════════════════════════════════════════════════

class Entity
{
public:
    int x;
    int y;

    Entity()
    {
        x = 0;
        y = 0;
    }

    virtual void Draw()
    {
    }
};

// ════════════════════════════════════════════════════════════════════
// Sky —— 天空（昼/夜渐变 + 太阳或月亮 + 夜里的星星）
// ════════════════════════════════════════════════════════════════════

class Sky : public Entity
{
public:
    int sw;
    int sh;
    int gy;              // 地平线 y
    int night;
    int ticks;           // 用来自转星星/眨眼的计数

    Sky()
    {
        sw = 0;
        sh = 0;
        gy = 0;
        night = 0;
        ticks = 0;
    }

    void Setup(int w, int h, int groundY, int isNight)
    {
        sw = w;
        sh = h;
        gy = groundY;
        night = isNight;
        ticks = 0;
    }

    void Advance()
    {
        ticks = ticks + 1;
    }

    virtual void Draw()
    {
        int i;
        int sx;
        int sy;
        int r;

        if (night != 0)
        {
            ui_gradient("sky", 0, C_SKY_NIT1, C_SKY_NIT2, 0, 0, 0, 1000);
        }
        else
        {
            ui_gradient("sky", 0, C_SKY_DAY1, C_SKY_DAY2, 0, 0, 0, 1000);
        }
        ui_rect_grad(0, 0, sw, gy, "sky", 0);

        if (night != 0)
        {
            // 星星：位置由下标推出来（**不用随机数**，否则每帧都在闪）
            i = 0;
            while (i < 26)
            {
                sx = (i * 137 + 41) % sw;
                sy = (i * 89 + 23) % (gy - 40) + 10;
                r = 1 + (i % 3);
                if ((ticks / 8 + i) % 5 != 0)
                {
                    ui_rect(sx, sy, r, r, C_STAR, 1, 0, 0);
                }
                i = i + 1;
            }
            ui_circle(sw - 54, 54, 22, C_MOON, 1, 0);
            ui_circle(sw - 62, 48, 20, C_SKY_NIT1, 1, 0);
        }
        else
        {
            ui_circle(sw - 54, 54, 24, C_SUN, 1, 0);
            ui_circle(sw - 54, 54, 34, 0x40FFE060, 1, 0);
        }
    }
};

// ════════════════════════════════════════════════════════════════════
// Building —— 楼房
// ════════════════════════════════════════════════════════════════════

class Building : public Entity
{
public:
    int w;
    int h;
    int gy;              // 地平线（底面）
    int tint;            // 配色档（0..3）

    Building()
    {
        w = 0;
        h = 0;
        gy = 0;
        tint = 0;
    }

    void SetTint(int t) { tint = t; }

    void Setup(int px, int pw, int ph, int groundY)
    {
        w = pw;
        h = ph;
        gy = groundY;
        x = px;
        y = gy - ph;
    }

    int Left() { return x; }
    int Right() { return x + w; }
    int RoofY() { return y; }

    // 这个 x 在不在这栋楼的横向范围内？
    int Covers(int px)
    {
        if (px < x) { return 0; }
        if (px > x + w) { return 0; }
        return 1;
    }

    virtual void Draw()
    {
        int col;
        int cols;
        int rows;
        int c;
        int r;
        int wx;
        int wy;

        // 按**楼号**染色（不是按 x 的奇偶）：一整条街四栋各不同，看着才像城市
        col = C_BLDG_A;
        if (tint == 1) { col = C_BLDG_B; }
        if (tint == 2) { col = C_BLDG_C; }
        if (tint == 3) { col = C_BLDG_D; }
        ui_rect(x, y, w, h, col, 1, 0, 0);
        ui_rect(x, y, w, 3, C_APE_DARK, 1, 0, 0);

        // 窗格
        cols = w / 18;
        if (cols < 1) { cols = 1; }
        rows = h / 24;
        if (rows < 1) { rows = 1; }
        c = 0;
        while (c < cols)
        {
            r = 0;
            while (r < rows)
            {
                wx = x + 6 + c * 18;
                wy = y + 10 + r * 24;
                if (((c + r + x) % 3) == 0)
                {
                    ui_rect(wx, wy, 9, 12, C_WIN_LIT, 1, 0, 0);
                }
                else
                {
                    ui_rect(wx, wy, 9, 12, C_WIN_DARK, 1, 0, 0);
                }
                r = r + 1;
            }
            c = c + 1;
        }
    }
};

// ════════════════════════════════════════════════════════════════════
// Ape —— 猴子（角度 / 力度 / 得分 / 朝哪边）
// ════════════════════════════════════════════════════════════════════

class Ape : public Entity
{
public:
    int angle;           // 0..90 度，相对"朝对手那一边"的水平线
    int power;           // 10..100
    int score;
    int flip;            // 0 = 朝右（左边那只），1 = 朝左（右边那只）
    int body;            // 颜色

    Ape()
    {
        x = 0;
        y = 0;
        angle = 45;
        power = 60;
        score = 0;
        flip = 0;
        body = C_APE0;
    }

    void SetSide(int isFlip)
    {
        flip = isFlip;
        body = C_APE0;
        if (isFlip != 0) { body = C_APE1; }
    }

    void StandOn(Building* b)
    {
        x = b->x + b->w / 2;
        y = b->RoofY();
    }

    void Aim(int d)
    {
        angle = angle + d;
        if (angle < 1) { angle = 1; }
        if (angle > 89) { angle = 89; }
    }

    void Boost(int d)
    {
        power = power + d;
        if (power < 10) { power = 10; }
        if (power > 100) { power = 100; }
    }

    // 出手点（手臂末端）
    int HandX()
    {
        if (flip != 0) { return x - 18; }
        return x + 18;
    }

    int HandY()
    {
        return y - 20;
    }

    // 把初速写进香蕉（定点单位/拍）。`dir` 由 flip 决定。
    void Shoot(Banana* b, int wind)
    {
        int v;
        int sv;
        int cv;
        int dir;

        v = power * V_UNIT;
        sv = isin(angle);
        cv = icos(angle);
        dir = 1;
        if (flip != 0) { dir = -1; }

        b->Launch(HandX(), HandY(), v * cv / SIN_SCALE * dir, -v * sv / SIN_SCALE, wind);
    }

    // 命中判定：以身体中心为准
    int Hits(int bx, int by)
    {
        if (iabs(bx - x) < 16 && iabs(by - (y - 12)) < 22) { return 1; }
        return 0;
    }

    virtual void Draw()
    {
        int hx;
        int hy;
        int len;

        // 身体
        ui_rect(x - 11, y - 26, 22, 26, body, 1, 0, 0);
        // 头
        ui_circle(x, y - 34, 10, body, 1, 0);
        // 眼睛（朝对手那边）
        if (flip != 0)
        {
            ui_rect(x - 7, y - 37, 3, 3, C_APE_DARK, 1, 0, 0);
        }
        else
        {
            ui_rect(x + 4, y - 37, 3, 3, C_APE_DARK, 1, 0, 0);
        }
        // 腿
        ui_rect(x - 9, y - 6, 7, 6, C_APE_DARK, 1, 0, 0);
        ui_rect(x + 2, y - 6, 7, 6, C_APE_DARK, 1, 0, 0);
        // 举起来那只胳膊（按角度画）—— 玩家看得见自己调的角度
        len = 26;
        hx = x + icos(angle) * len / SIN_SCALE * (1 - 2 * flip);
        hy = y - 20 - isin(angle) * len / SIN_SCALE;
        ui_line(x, y - 20, hx, hy, body, 5);
        ui_circle(hx, hy, 4, body, 1, 0);
    }
};

// ════════════════════════════════════════════════════════════════════
// Banana —— 香蕉（定点弹道）
// ════════════════════════════════════════════════════════════════════

class Banana : public Entity
{
public:
    int bx;              // 定点位置
    int by;
    int vx;              // 定点速度（每拍）
    int vy;
    int live;
    int owner;           // 谁扔的（0/1）
    int trailN;

    Banana()
    {
        bx = 0;
        by = 0;
        vx = 0;
        vy = 0;
        live = 0;
        owner = 0;
        trailN = 0;
        x = 0;
        y = 0;
    }

    void Launch(int sx, int sy, int svx, int svy, int wind)
    {
        bx = sx * FP;
        by = sy * FP;
        vx = svx + wind * FP / 4;
        vy = svy;
        live = 1;
        trailN = 0;
    }

    // 一拍。返回 0 = 还在飞，1 = 掉到地上/楼里，2 = 飞出屏幕
    int Step(int wind, int gy, int sw)
    {
        int i;

        if (live == 0) { return 0; }

        bx = bx + vx;
        by = by + vy;
        vy = vy + GRAV;
        vx = vx + wind;                     // 风是"加速度"（每拍加一点水平速度）

        x = bx / FP;
        y = by / FP;

        // 记尾迹（满了就整体左移一格）
        if (trailN < MAX_TRAIL)
        {
            trailX[trailN] = x;
            trailY[trailN] = y;
            trailN = trailN + 1;
        }
        else
        {
            i = 0;
            while (i < MAX_TRAIL - 1)
            {
                trailX[i] = trailX[i + 1];
                trailY[i] = trailY[i + 1];
                i = i + 1;
            }
            trailX[MAX_TRAIL - 1] = x;
            trailY[MAX_TRAIL - 1] = y;
        }

        if (x < -80 || x > sw + 80) { return 2; }
        if (y > gy) { live = 0; return 1; }   // 落到地平线以下（含矮楼之间）
        return 0;
    }

    void Stop()
    {
        live = 0;
    }

    virtual void Draw()
    {
        int i;

        if (live == 0) { return; }

        // 尾迹：越早的点越暗（用不同的固定色，避免每帧算颜色）
        i = 0;
        while (i < trailN)
        {
            if (i % 3 == 0)
            {
                ui_rect(trailX[i], trailY[i], 2, 2, C_TRAIL, 1, 0, 0);
            }
            i = i + 1;
        }
        ui_circle(x, y, 5, C_BANANA, 1, 0);
        ui_circle(x - 2, y - 2, 2, 0xFFFFFFFF, 1, 0);
    }
};

// ════════════════════════════════════════════════════════════════════
// Wind —— 风
// ════════════════════════════════════════════════════════════════════

class Wind
{
public:
    int v;               // 定点单位/拍^2（正 = 向右吹）

    Wind()
    {
        v = 0;
    }

    void Randomize()
    {
        v = ui_rand(61) - 30;
        v = v * 3;                       // -90 .. +90
    }

    void Draw(int sw, int y)
    {
        int cx;
        int i;
        int n;

        cx = sw / 2;
        ui_text(cx, y, "风", C_TEXT_DIM, 12, VML_ANCHOR_CENTER);
        ui_rect(cx - 34, y + 18, 68, 4, 0x40FFFFFF, 1, 0, 0);
        if (v != 0)
        {
            i = 0;
            n = v / 12;
            if (n < 0) { n = -n; }
            if (n < 1) { n = 1; }
            while (i < n)
            {
                if (v > 0) { ui_rect(cx + 2 + i * 6, y + 16, 4, 8, C_WIND, 1, 0, 0); }
                else { ui_rect(cx - 6 - i * 6, y + 16, 4, 8, C_WIND, 1, 0, 0); }
                i = i + 1;
            }
        }
        if (v > 0) { ui_text(cx + 40, y, "→", C_WIND, 12, VML_ANCHOR_LEFT); }
        if (v < 0) { ui_text(cx - 40, y, "←", C_WIND, 12, VML_ANCHOR_RIGHT); }
    }
};

// ════════════════════════════════════════════════════════════════════
// Hud —— 顶部状态栏
// ════════════════════════════════════════════════════════════════════

class Hud
{
public:
    Hud()
    {
    }

    void Draw(Ape* a0, Ape* a1, int turn, int sw, int night)
    {
        ui_rect(0, 0, sw, 30, C_HUD_BG, 1, 0, 0);

        ui_text(8, 20, "橙", C_APE0, 13, VML_ANCHOR_LEFT);
        ui_text(30, 20, numstr(a0->score), C_TEXT, 14, VML_ANCHOR_LEFT);

        ui_text(sw - 8, 20, "紫", C_APE1, 13, VML_ANCHOR_RIGHT);
        ui_text(sw - 30, 20, numstr(a1->score), C_TEXT, 14, VML_ANCHOR_RIGHT);

        if (turn == 0) { ui_text(sw / 2, 20, "轮到 橙", C_APE0, 14, VML_ANCHOR_CENTER); }
        else { ui_text(sw / 2, 20, "轮到 紫", C_APE1, 14, VML_ANCHOR_CENTER); }

        if (night != 0)
        {
            ui_text(sw / 2 + 58, 20, "夜", C_TEXT_DIM, 12, VML_ANCHOR_CENTER);
        }
    }
};

// ════════════════════════════════════════════════════════════════════
// Game —— 总控
// ════════════════════════════════════════════════════════════════════

class Game
{
public:
    // ⚠ 这里**不能写成 `Building bldgs[4];`** —— 类里放不了数组字段
    //   （`int data[4];` 直接解析错误「期望 SEMICOLON，实际得到 LBRACKET」）。
    //   所以四条楼、两只猴子各自**具名**，另外用文件级指针表按序号访问
    //   （`BL[i]` / `AP[i]`，见本文件顶部的 `g_*` 那一组）。
    Sky sky;
    Building b0;
    Building b1;
    Building b2;
    Building b3;
    Ape a0;
    Ape a1;
    Banana ban;
    Wind wind;
    Hud hud;

    int sw;
    int sh;
    int gy;
    int turn;
    int state;
    int boomT;
    int boomX;
    int boomY;
    int over;
    int night;
    int hitBy;              // 这一发打中了谁（-1 = 没打中人）
    int heldL;
    int heldR;
    int heldU;
    int heldD;
    int repeatT;

    Game()
    {
        sw = 0;
        sh = 0;
        gy = 0;
        turn = 0;
        state = ST_AIM;
        boomT = 0;
        boomX = 0;
        boomY = 0;
        over = 0;
        night = 0;
        hitBy = -1;
        heldL = 0;
        heldR = 0;
        heldU = 0;
        heldD = 0;
        repeatT = 0;
    }

    // 把具名字段与按序号的访问对上（构造函数跑完、对象地址稳定之后调一次）
    void Bind()
    {
        BL[0] = &b0;
        BL[1] = &b1;
        BL[2] = &b2;
        BL[3] = &b3;
        AP[0] = &a0;
        AP[1] = &a1;
        a0.SetSide(0);
        a1.SetSide(1);
    }

    void Layout()
    {
        int i;
        int gap;

        Bind();          // 先把按序号的指针表填上（下面按 BL[i]/AP[i] 访问）

        int bw;
        int base;
        int h;
        int x;

        sw = ui_scr_w();
        sh = ui_scr_h();
        if (sw <= 0) { sw = 411; }
        if (sh <= 0) { sh = 726; }

        gy = sh * 74 / 100;
        night = (ui_rand(2) == 1);

        sky.Setup(sw, sh, gy, night);

        gap = sw / 40;
        bw = (sw - gap * 5) / 4;
        base = gy - sh / 5;
        x = gap;

        i = 0;
        while (i < 4)
        {
            h = sh / 5 + ui_rand(sh / 4);
            if (i == 0 || i == 3) { h = h + sh / 14; }   // 两边的楼高一点，好站
            (*BL[i]).Setup(x, bw, h, gy);
            (*BL[i]).SetTint(i);
            x = x + bw + gap;
            i = i + 1;
        }

        (*AP[0]).StandOn(&(*BL[0]));
        (*AP[1]).StandOn(&(*BL[3]));
    }

    void NewTurn()
    {
        wind.Randomize();
        state = ST_AIM;
        repeatT = 0;
    }

    void Fire(Ape* who)
    {
        who->Shoot(&ban, wind.v);
        state = ST_FLY;
    }

    // 一拍物理；返回 1 = 这一拍结算了（换人或结束）
    int Tick()
    {
        int r;
        int i;
        int hit;

        sky.Advance();

        if (state == ST_FLY)
        {
            r = ban.Step(wind.v, gy, sw);

            // 打到猴子？
            i = 0;
            hit = -1;
            while (i < 2)
            {
                if (i != ban.owner)
                {
                    if (ban.live != 0 && (*AP[i]).Hits(ban.x, ban.y))
                    {
                        hit = i;
                    }
                }
                i = i + 1;
            }
            // 打到楼？
            if (hit < 0 && r == 0)
            {
                i = 0;
                while (i < 4)
                {
                    if ((*BL[i]).Covers(ban.x) && ban.y >= (*BL[i]).RoofY())
                    {
                        hit = -2;
                    }
                    i = i + 1;
                }
            }
            else if (hit < 0 && r == 1)
            {
                hit = -2;                 // 落地
            }

            if (hit >= 0)
            {
                (*AP[ban.owner]).score = (*AP[ban.owner]).score + 1;
                hitBy = ban.owner;
                boomX = ban.x;
                boomY = ban.y;
                boomT = 0;
                state = ST_BOOM;
                ban.Stop();
                ui_beep(880, 60);
                ui_vibrate(40, 0);
                return 1;
            }
            if (hit == -2)
            {
                hitBy = -1;
                boomX = ban.x;
                boomY = ban.y;
                boomT = 0;
                state = ST_BOOM;
                ban.Stop();
                ui_beep(180, 40);
                return 1;
            }
            if (r == 2)
            {
                hitBy = -1;
                state = ST_BOOM;
                boomT = 24;               // 飞出去：不画爆炸，直接进入下一回合
                ban.Stop();
                return 1;
            }
        }
        else if (state == ST_BOOM)
        {
            boomT = boomT + 1;
            if (boomT > 24)
            {
                if ((*AP[0]).score >= 3 || (*AP[1]).score >= 3) { over = 1; }
                turn = 1 - turn;
                NewTurn();
                return 1;
            }
        }
        else if (state == ST_AIM)
        {
            // 按住方向键连发
            if (heldL != 0 || heldR != 0 || heldU != 0 || heldD != 0)
            {
                repeatT = repeatT + 1;
                if (repeatT >= 3)
                {
                    repeatT = 0;
                    if (heldL != 0) { (*AP[turn]).Aim(1); }
                    if (heldR != 0) { (*AP[turn]).Aim(-1); }
                    if (heldU != 0) { (*AP[turn]).Boost(1); }
                    if (heldD != 0) { (*AP[turn]).Boost(-1); }
                }
            }
        }
        return 0;
    }

    void KeyDown(int k)
    {
        Ape* cur;

        if (k == VML_KEY_ESCAPE) { over = 1; return; }
        if (over != 0) { return; }
        if (state != ST_AIM) { return; }

        cur = &(*AP[turn]);
        if (k == VML_KEY_LEFT) { cur->Aim(1); heldL = 1; repeatT = 0; }
        if (k == VML_KEY_RIGHT) { cur->Aim(-1); heldR = 1; repeatT = 0; }
        if (k == VML_KEY_UP) { cur->Boost(1); heldU = 1; repeatT = 0; }
        if (k == VML_KEY_DOWN) { cur->Boost(-1); heldD = 1; repeatT = 0; }
        if (k == VML_KEY_ENTER || k == VML_KEY_SPACE || k == VML_KEY_PAD_A) { Fire(cur); }
    }

    void KeyUp(int k)
    {
        if (k == VML_KEY_LEFT) { heldL = 0; }
        if (k == VML_KEY_RIGHT) { heldR = 0; }
        if (k == VML_KEY_UP) { heldU = 0; }
        if (k == VML_KEY_DOWN) { heldD = 0; }
    }

    // ── 瞄准时的**弹道预览** ────────────────────────────────────────────
    // 用与 `Banana::Step` **完全同一套**公式试算一遍（只是不改真香蕉的状态），
    // 每 5 拍点一个点。手感差别很大：原来是"凭感觉蒙"，现在是"看得见落点".
    //
    // ⚠ 公式必须与 `Step` 一致 —— 两处各写一份就一定会漂（预览的弧线和实际飞行的
    //   弧线不一样，比没有预览更糟）。这里连常量都引用同一批宏。
    void DrawAimPreview()
    {
        Ape* cur;
        int px;
        int py;
        int vx;
        int vy;
        int sv;
        int cv;
        int dir;
        int v;
        int n;

        if (state != ST_AIM) { return; }

        cur = AP[turn];
        v = (*cur).power * V_UNIT;
        sv = isin((*cur).angle);
        cv = icos((*cur).angle);
        dir = 1;
        if ((*cur).flip != 0) { dir = -1; }

        px = (*cur).HandX() * FP;
        py = (*cur).HandY() * FP;
        vx = v * cv / SIN_SCALE * dir + wind.v * FP / 4;
        vy = -v * sv / SIN_SCALE;

        n = 0;
        while (n < 150)
        {
            px = px + vx;
            py = py + vy;
            vy = vy + GRAV;
            vx = vx + wind.v;

            if (n % 5 == 0)
            {
                if (py / FP < gy)
                {
                    ui_rect(px / FP - 1, py / FP - 1, 5, 5, 0xCCFFE070, 1, 0, 0);
                }
            }
            if (py / FP > gy) { n = 150; }
            n = n + 1;
        }
    }

    void DrawAimPanel()
    {
        Ape* cur;
        int y;
        int px;

        cur = &(*AP[turn]);
        y = sh - 74;
        ui_rect(0, y, sw, 74, C_HUD_BG, 1, 0, 0);

        ui_text(12, y + 26, "角度", C_TEXT_DIM, 12, VML_ANCHOR_LEFT);
        ui_text(56, y + 26, numstr(cur->angle), C_TEXT, 15, VML_ANCHOR_LEFT);
        ui_text(12, y + 54, "力度", C_TEXT_DIM, 12, VML_ANCHOR_LEFT);
        ui_text(56, y + 54, numstr(cur->power), C_TEXT, 15, VML_ANCHOR_LEFT);

        // 力度条
        px = 96;
        ui_rect(px, y + 48, sw - px - 96, 8, 0x40FFFFFF, 1, 0, 0);
        ui_rect(px, y + 48, (sw - px - 96) * cur->power / 100, 8, C_BANANA, 1, 0, 0);

        // 角度条
        ui_rect(px, y + 20, sw - px - 96, 8, 0x40FFFFFF, 1, 0, 0);
        ui_rect(px, y + 20, (sw - px - 96) * cur->angle / 90, 8, C_WIND, 1, 0, 0);

        ui_text(sw - 12, y + 26, "←→ 角度", C_TEXT_DIM, 11, VML_ANCHOR_RIGHT);
        ui_text(sw - 12, y + 54, "↑↓ 力度", C_TEXT_DIM, 11, VML_ANCHOR_RIGHT);
        ui_text(sw / 2, y + 8, "回车 / 空格 / A 发射", C_BANANA, 12, VML_ANCHOR_CENTER);
    }

    void Draw()
    {
        int i;
        Entity* actors[8];

        // 多态绘制：全部当 Entity 指针调 Draw()，实现由各自决定
        actors[0] = &sky;
        actors[1] = &(*BL[0]);
        actors[2] = &b1;
        actors[3] = &b2;
        actors[4] = &(*BL[3]);
        actors[5] = &(*AP[0]);
        actors[6] = &(*AP[1]);
        actors[7] = &ban;

        i = 0;
        while (i < 8)
        {
            actors[i]->Draw();
            i = i + 1;
        }

        // 地面
        ui_rect(0, gy, sw, sh - gy, C_GROUND, 1, 0, 0);

        DrawAimPreview();

        // 爆炸：冲击波 + 火球 + 碎屑
        if (state == ST_BOOM && boomT <= 24)
        {
            int r;
            int k;
            int dx;
            int dy;

            r = 6 + boomT * 3;
            ui_circle(boomX, boomY, r + 10, 0x30FF7020, 1, 0);        // 半透明冲击波
            ui_circle(boomX, boomY, r, C_BOOM, 1, 0);
            ui_circle(boomX, boomY, r * 2 / 3, C_BANANA, 1, 0);
            ui_circle(boomX, boomY, r / 3, 0xFFFFFFFF, 1, 0);

            k = 0;
            while (k < 8)
            {
                dx = icos(k * 45) * (r + 14) / 800;
                dy = -isin(k * 45) * (r + 14) / 800;
                ui_rect(boomX + dx, boomY + dy, 3, 3, C_BOOM, 1, 0, 0);
                k = k + 1;
            }
        }

        // 命中横幅（谁打中了谁）
        if (state == ST_BOOM && hitBy >= 0 && boomT <= 24)
        {
            if (hitBy == 0) { ui_text(sw / 2, gy / 2, "橙猴命中！", C_APE0, 26, VML_ANCHOR_CENTER); }
            else { ui_text(sw / 2, gy / 2, "紫猴命中！", C_APE1, 26, VML_ANCHOR_CENTER); }
        }

        hud.Draw(&(*AP[0]), &(*AP[1]), turn, sw, night);
        wind.Draw(sw, 44);
        DrawAimPanel();

        if (over != 0)
        {
            ui_rect(0, sh / 2 - 40, sw, 80, 0xE0101020, 1, 0, 0);
            if ((*AP[0]).score > (*AP[1]).score)
            {
                ui_text(sw / 2, sh / 2 - 2, "橙猴获胜！", C_APE0, 24, VML_ANCHOR_CENTER);
            }
            else if ((*AP[1]).score > (*AP[0]).score)
            {
                ui_text(sw / 2, sh / 2 - 2, "紫猴获胜！", C_APE1, 24, VML_ANCHOR_CENTER);
            }
            else
            {
                ui_text(sw / 2, sh / 2 - 2, "平局", C_TEXT, 24, VML_ANCHOR_CENTER);
            }
            ui_text(sw / 2, sh / 2 + 26, "点一下屏幕退出", C_TEXT_DIM, 13, VML_ANCHOR_CENTER);
        }

        ui_present();
    }
};

// ════════════════════════════════════════════════════════════════════
// 主循环
// ════════════════════════════════════════════════════════════════════

int main()
{
    Game g;
    int tid;
    int t;
    int k;
    int done;
    int msg[4];
    int score0;
    int score1;

    g.Layout();
    ui_win_open("大猩猩扔香蕉 (C++)", g.sw, g.sh);
    ui_keep_on(1);
    g.NewTurn();

    tid = ui_timer_set(TICK_MS, 0);
    done = 0;

    while (done == 0 && ui_win_closed() == 0)
    {
        g.Draw();

        t = ui_wait_msg(0);
        if (t == VML_MSG_WINDOWCLOSE) { done = 1; }

        if (t == VML_MSG_TIMER)
        {
            g.Tick();
            if (g.over != 0)
            {
                ui_timer_kill(tid);
                tid = 0;
            }
        }
        if (t == VML_MSG_KEYDOWN) { g.KeyDown(ui_msg_a()); }
        if (t == VML_MSG_KEYUP) { g.KeyUp(ui_msg_a()); }
        if (t == VML_MSG_TOUCHDOWN || t == VML_MSG_MOUSEUP)
        {
            if (g.over != 0) { done = 1; }
        }
    }

    if (tid != 0) { ui_timer_kill(tid); }

    // 比分落盘（下次开局问不出来，但先存着 —— 与 BASIC 版同一套键）
    score0 = (*AP[0]).score;
    score1 = (*AP[1]).score;
    ui_store_set("gorilla.hpp.score0", numstr(score0));
    ui_store_set("gorilla.hpp.score1", numstr(score1));

    ui_keep_on(0);
    return 0;
}
