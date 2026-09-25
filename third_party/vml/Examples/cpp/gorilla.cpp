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
// ## 前端的四条限制 —— **已经全部修好了**，但这个文件**暂时仍按老写法**
//
// 这四条原先是真的写不出来（下面括号里是当年的报错），现在都能写了：
//
//   ① **类里不能有数组字段** ⇒ 现在能了（`int data[4];` 正常，见 `KNOWN_DEFECTS.md` 的 F25）。
//      本文件的尾迹仍在**文件级数组**（`trailX/trailY`），因为整条链上还有一小截没通：
//      **成员数组在类内部用隐式 `this` 做下标读**（`trailX[i]` 而不是 `b.trailX[i]`）
//      算出来的地址不对（`d30.cpp`：求和 143 应为 99，且输出会断）。等那一截修好再收回来。
//   ② **方法不能返回指针类型** ⇒ 现在能了（F26）。本文件没有需要返回指针的地方，维持原样。
//   ③ **`cout << 对象的字段` 打不出东西** ⇒ 现在能了（F22/F30）。本文件里凡是要印的
//      仍习惯性先存进局部量 —— 那是无害的写法，不是缺陷绕过。
//   ④ **临时对象赋值**（`a0 = Ape(1, 2);`）不生效 ⇒ 现在能了（F31，且带默认参数补齐 F28）。
//      本文件仍用 `Setup(...)` / `Launch(...)` 这类成员方法改状态：对"改一个已有对象的
//      若干字段"来说，成员方法本来就更直白。
//
// ⚠ **别照着这几条去写新程序** —— 新代码该用什么用什么；这份注释的作用是
//   让人知道"这段老写法不是风格选择、也不是不能改，而是**改之前先跑一遍**"。
//   完整台账（哪些修了、哪些还开着、怎么复现）见
//   `VMLPrepares/CppCompiler/KNOWN_DEFECTS.md`。
//
// ## 操作
//
//   拖「角度」条调仰角、拖「力度」条调力度、点左边的「发 射」按钮出香蕉 ——
//   手指点到条的哪一格就是哪个值，**不需要键盘**（与 `basic/gorilla_pro.bas` 同一套）。
//   开窗声明了 `VML_WIN_NO_GAMEPAD`（不要手柄区 ⇒ 画布吃满整屏）与 `VML_WIN_PORTRAIT`。
//   键盘**不是必需品**，接物理键盘时仍可用：←→ 角度、↑↓ 力度、回车/空格/A 发射、ESC 退出。
//   窗口被关、或结束画面上点一下，也会退出。
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

// 先拿满这么多分的人赢
#define WIN_SCORE 5

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
// 香蕉尾迹。见文件头 ①：**类里现在能放数组了**，但"成员数组在类内部用隐式 `this`
// 做下标读"那一截还没通（`d30.cpp`），所以尾迹暂时仍在文件级。
// ════════════════════════════════════════════════════════════════════

static int trailX[MAX_TRAIL];
static int trailY[MAX_TRAIL];

// 按序号访问具名字段的指针表（见 `Game` 里那段注释：那里的写法是历史原因，不是限制）
static Building* BL[4];
static Ape* AP[2];
static Tree TREES[6];        // 地上的树（数量随机，见 Game::Layout）
static Flyer* FLY[3];        // 鸟 / 飞碟 / 飞机 —— 三个派生类共用基类指针

// ════════════════════════════════════════════════════════════════════
// 弹坑 —— **可破坏地形**
//
// 香蕉撞上楼或落地就在那儿啃掉一块：把那一小块**涂回当地的天空色**。
// 因为是"涂"而不是"改楼房的数据"，所以楼房本身完全不用动 —— 楼照旧是一次画好的矩形，
// 坑只是画在它**之后**、**地面之前**的一层覆盖。
//
// ⚠ 顺序不能变（`Game::Draw` 里那三层的次序是有讲究的）：
//   · 必须在**所有楼之后** —— 坑可能横跨两栋的交界，画在前面会被后一栋盖掉；
//   · 必须在**地面之前** —— 否则会把地面也啃掉一块（地上的坑本来就该被地面盖住）。
//
// ⚠ "涂回天空色"必须问一句**哪个 y 的天空色**：天空是**渐变**的，
//   写死一个颜色会在楼中间留下一块色不对的圆 —— 而且只在某些时段看得出来。
//════════════════════════════════════════════════════════════════════

#define MAX_HOLE 24

static int holeX[MAX_HOLE];
static int holeY[MAX_HOLE];
static int holeR[MAX_HOLE];
static int holeN;

void clearHoles()
{
    holeN = 0;
}

// 记一个坑。满了就丢掉新的 —— **不覆盖旧的**：覆盖会让地形在打到第 25 发时
// 突然"愈合"，而那一下正好发生在玩家已经建立空间感之后。宁可不画新的。
void addHole(int hx, int hy, int hr)
{
    if (holeN >= MAX_HOLE) { return; }
    holeX[holeN] = hx;
    holeY[holeN] = hy;
    holeR[holeN] = hr;
    holeN = holeN + 1;
}

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
// 颜色插值 —— 全文件配色的**唯一**入口
//
//   两个颜色按百分比插值，结果打包成 0xAARRGGBB。
//   分量各自算完再打包 —— **只打包、不解包**：`0xFF` 打头的颜色在 int 里是负数，
//   反解一个"颜色变量"要先处理符号，而"分量算完再打包"根本不需要反解。
//
// ⚠ 参数名一律**两个字母以上**：本仓实测「7 个参数 + 单字母参数名」会让**别的类**
//   解析失败（解析期一个错都不报），见 `KNOWN_DEFECTS.md` 的 OPEN #17。
// ════════════════════════════════════════════════════════════════════

int mixcol(int nr, int ng, int nb, int dr, int dg, int db, int pct)
{
    int cr;
    int cg;
    int cb;
    cr = nr + (dr - nr) * pct / 100;
    cg = ng + (dg - ng) * pct / 100;
    cb = nb + (db - nb) * pct / 100;
    return 0xFF000000 + cr * 65536 + cg * 256 + cb;
}

// ════════════════════════════════════════════════════════════════════
// 游戏时钟 —— **一组全局量 + 自由函数**（1 真实秒 = 1 游戏分钟）
//
// ## 为什么不是类
//
// 一开始它是 `class Clock` + 一个全局对象 `static Clock gclk;`，结果**天空恒黑**：
// `Compute` 里写 `zenR/G/B` 之后，外面读到的还是 0。查了一圈 —— 全局对象上调用方法
// 正常（`d44`）、方法内读自己的字段正常（`d45`/`d47`）、字段数多寡无关、
// 参数重名无关、方法返回值当实参正常（`d48`）—— **根因没找到**（见 OPEN #18）。
//
// 用户拍板绕开：**没有类就没有 `this` 和字段偏移这整条链**。代价是这里的"状态"
// 是大写开头的全局量而不是对象字段，读起来糙一点；好处是它**确实能工作**。
// ⚠ 等 OPEN #18 定位了，这一段可以收回成类 —— 那时别把上面的结论当"类不能用"。
//
// ## 时间怎么走
//
// 走**独立的一秒定时器**，不是"每画一帧加一点"：主循环在玩家拖动滑条时会一次
// 抽干几十条 TOUCHMOVE，拿帧数当时钟会"滑得越勤、时钟跑得越快"。
// ════════════════════════════════════════════════════════════════════

static int gHour = 7;        // 开局清晨 —— 一进来就是白天，玩家不用干等
static int gMinute = 30;
static int gDayL;            // 天光 0..100（0 = 全黑、100 = 正午满亮）
static int gWarm;            // 日出/日落的暖色系数 0..100（地平线偏橙）
static int gSunUp;
static int gMoonUp;
static int gSunX;
static int gSunY;
static int gMoonX;
static int gMoonY;
static int gZenR; static int gZenG; static int gZenB;    // 当前天顶色
static int gHorR; static int gHorG; static int gHorB;    // 当前地平线色
static int gMr; static int gMg; static int gMb;          // `clockPartsAt` 的输出

// 游戏时钟走一格
void clockTick()
{
    gMinute = gMinute + 1;
    if (gMinute >= 60) { gMinute = 0; gHour = gHour + 1; }
    if (gHour >= 24) { gHour = 0; }
}

// 算这一刻的天光 / 暖色 / 日月位置 —— **每帧调一次**（游戏时间可能刚跳过一格）
void clockCompute(int scrW, int groundY, int hudH)
{
    int gmins;
    int tpv;
    int mm;
    int tp2;
    int win;
    int arcX0;
    int arcW;
    int arcBot;
    int arcH;

    gmins = gHour * 60 + gMinute;

    // 太阳：6:00 升、12:00 顶、18:00 落
    gSunUp = 0;
    tpv = 0;
    if (gmins >= 360)
    {
        if (gmins <= 1080)
        {
            gSunUp = 1;
            tpv = (gmins - 360) * 1000 / 720;         // 0..1000
        }
    }
    gDayL = 0;
    if (gSunUp != 0)
    {
        // 高度角 → 0..100，再**提亮 1.4 倍**（封顶 100）。
        // 不提的话"看着像白天"只有 9:00–15:00 那一小段，而玩家 24 分钟才看完一天，
        // 白天的观感被压到中间那几帧很不划算。1.4 倍之后约 7:00–17:00 都满亮。
        // ⚠ `isin` 的值域是 ×1000（**不是 BASIC 那个 SIN 的 ×10000**），
        //   所以这里是 `* 14 / 100` = ×140 —— 照抄 BASIC 的 `/ 1000` 会差一个数量级。
        gDayL = isin(tpv * 180 / 1000) * 14 / 100;
        if (gDayL > 100) { gDayL = 100; }
    }

    // 日月走**同一条弧**（月亮把时钟拨 12 小时）
    arcX0 = 40;
    arcW = scrW - 80;
    arcBot = groundY - 46;
    arcH = arcBot - hudH - 46;
    if (arcH < 40) { arcH = 40; }
    gSunX = arcX0 + arcW * tpv / 1000;
    gSunY = arcBot - arcH * isin(tpv * 180 / 1000) / 1000;

    mm = gmins + 720;
    if (mm >= 1440) { mm = mm - 1440; }
    gMoonUp = 0;
    tp2 = 0;
    if (mm >= 360)
    {
        if (mm <= 1080)
        {
            gMoonUp = 1;
            tp2 = (mm - 360) * 1000 / 720;
        }
    }
    gMoonX = arcX0 + arcW * tp2 / 1000;
    gMoonY = arcBot - arcH * isin(tp2 * 180 / 1000) / 1000;

    // 天顶：夜(7,10,24) → 昼(46,111,208)
    gZenR = 7 + (46 - 7) * gDayL / 100;
    gZenG = 10 + (111 - 10) * gDayL / 100;
    gZenB = 24 + (208 - 24) * gDayL / 100;
    // 地平线：夜(18,19,42) → 昼(168,216,245)
    gHorR = 18 + (168 - 18) * gDayL / 100;
    gHorG = 19 + (216 - 19) * gDayL / 100;
    gHorB = 42 + (245 - 42) * gDayL / 100;

    // 日出 / 日落的暖色：以 6:00 与 18:00 为中心各一个 ±60 分钟的三角窗。
    // ⚠ **不能用 gDayL 算暖色**：它在日落那一刻直接归 0，而"0 ⇒ 最暖"会让
    //   地平线在 18:00 整从橙**跳**回蓝（BASIC 版实测过，肉眼可见的一跳）。
    //   按**时钟**给窗、两窗重叠取大，黄昏的余晖才是连续收尾的。
    gWarm = 0;
    if (gmins >= 300)
    {
        if (gmins <= 420)
        {
            gWarm = 100 - iabs(gmins - 360) * 100 / 60;
        }
    }
    if (gmins >= 1020)
    {
        if (gmins <= 1140)
        {
            win = 100 - iabs(gmins - 1080) * 100 / 60;
            if (win > gWarm) { gWarm = win; }
        }
    }
    gHorR = gHorR + (236 - gHorR) * gWarm / 100;
    gHorG = gHorG + (126 - gHorG) * gWarm / 100;
    gHorB = gHorB + (64 - gHorB) * gWarm / 100;
}

int clockZenith() { return 0xFF000000 + gZenR * 65536 + gZenG * 256 + gZenB; }

int clockHorizon() { return 0xFF000000 + gHorR * 65536 + gHorG * 256 + gHorB; }

// 某个 y 处的天空色**分量** —— 给 `mixcol` 用（它吃分量、不吃打包色）。
// 结果落在 gMr/gMg/gMb（与 BASIC 版 `mix2`/`skyAt` 的约定一致：输出走固定全局，
// 因为 C++ 前端不能让函数一次返回三个值）。
void clockPartsAt(int posY, int groundY)
{
    int frac;
    if (groundY <= 0) { groundY = 1; }
    frac = posY * 100 / groundY;
    if (frac < 0) { frac = 0; }
    if (frac > 100) { frac = 100; }
    gMr = gZenR + (gHorR - gZenR) * frac / 100;
    gMg = gZenG + (gHorG - gZenG) * frac / 100;
    gMb = gZenB + (gHorB - gZenB) * frac / 100;
}

// 把上一次 `clockPartsAt` 的结果打包
int clockPackParts() { return 0xFF000000 + gMr * 65536 + gMg * 256 + gMb; }


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
    int ticks;           // 用来自转星星/眨眼的计数

    Sky()
    {
        sw = 0;
        sh = 0;
        gy = 0;
        ticks = 0;
    }

    void Setup(int w, int h, int groundY)
    {
        sw = w;
        sh = h;
        gy = groundY;
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
        int rad;
        int fade;        // 星星的可见度（跟着天光走）

        // 天空：**整块渐变**，两端颜色每一帧都随时钟算（与 BASIC 版 `skyPal` 同一口径）。
        ui_gradient("sky", 0, clockZenith(), clockHorizon(), 0, 0, 0, 1000);
        ui_rect_grad(0, 0, sw, gy, "sky", 0);

        // ── 星星 ──────────────────────────────────────────────────────
        // 位置由下标推出来（**不用随机数**，否则每帧都在闪）。
        // 可见度**跟着天光淡出**：天光满时正好等于当地天空色 ⇒ 自然消失。
        // 这样就不需要"天黑该不该画星星"那个开关 —— 开关会在某一刻跳一下，
        // 而连续变化的场景里，任何"跳"都看得出来。
        fade = 0;
        if (gDayL < 45) { fade = (45 - gDayL) * 220 / 45; }
        if (fade > 100) { fade = 100; }
        if (fade > 0)
        {
            i = 0;
            while (i < 26)
            {
                sx = (i * 137 + 41) % sw;
                sy = (i * 89 + 23) % (gy - 40) + 10;
                rad = 1 + (i % 3);
                if ((ticks / 8 + i) % 5 != 0)
                {
                    clockPartsAt(sy, gy);
                    // 每颗星再差一点亮度（由下标定），不然一片一模一样的白点像噪点
                    ui_rect(sx, sy, rad, rad,
                            mixcol(gMr, gMg, gMb, 255, 255, 255, fade - (i % 5) * 6),
                            1, 0, 0);
                }
                i = i + 1;
            }
        }

        // ── 太阳 / 月亮 ────────────────────────────────────────────────
        // 位置由时钟算（6:00 升、12:00 顶、18:00 落；月亮差 12 小时走同一条弧）。
        // 都带一圈"朝**当地天空色**淡出"的光晕 —— 天空是渐变的，光晕外缘写死一个
        // 颜色就会在渐变天上留一个色斑，而且只在某些时段看得出来。
        if (gSunUp != 0)
        {
            clockPartsAt(gSunY, gy);
            ui_gradient("sunglow", 1, 0xFFFFE9A8, clockPackParts(), 500, 500, 500);
            ui_circle_grad(gSunX, gSunY, 34, "sunglow");
            ui_circle(gSunX, gSunY, 11, 0xFFFFF0B4, 1, 0);
            ui_circle(gSunX - 3, gSunY - 3, 4, 0xFFFFFCE6, 1, 0);
        }
        if (gMoonUp != 0)
        {
            clockPartsAt(gMoonY, gy);
            ui_gradient("moonglow", 1, 0xFFEDE9D6, clockPackParts(), 500, 500, 500);
            ui_circle_grad(gMoonX, gMoonY, 26, "moonglow");
            ui_circle(gMoonX, gMoonY, 14, 0xFFF4F0DE, 1, 0);
            // 环形山（三个暗一点的小圆）—— 没有它们月亮就是一个白饼
            ui_circle(gMoonX - 5, gMoonY - 4, 3, 0xFFDCD6BE, 1, 0);
            ui_circle(gMoonX + 4, gMoonY + 2, 2, 0xFFDCD6BE, 1, 0);
            ui_circle(gMoonX + 1, gMoonY - 7, 2, 0xFFE4DEC6, 1, 0);
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
    int baseR;           // 本栋的**基色分量** —— 配色随时钟变化时要从分量算起
    int baseG;
    int baseB;

    Building()
    {
        w = 0;
        h = 0;
        gy = 0;
        tint = 0;
        baseR = 110; baseG = 90; baseB = 70;
    }

    /// <summary>
    /// 定色相。**存分量而不是只存档号** —— 后面每一处配色（楼体、受光面、檐口、窗框）
    /// 都要以它为基准按天光插值，每次再从档号反解一遍颜色分量是"同一件事两处实现"。
    /// 四个色与 `C_BLDG_A..D` 一一对应（BASIC 版是 6 档，这里沿用本文件原有的 4 档）。
    /// </summary>
    void SetTint(int t)
    {
        tint = t;
        baseR = 110; baseG = 90; baseB = 70;        // A 0xFF6E5A46
        if (t == 1) { baseR = 138; baseG = 114; baseB = 88; }    // B 0xFF8A7258
        if (t == 2) { baseR = 90; baseG = 110; baseB = 122; }    // C 0xFF5A6E7A
        if (t == 3) { baseR = 122; baseG = 90; baseB = 110; }    // D 0xFF7A5A6E
    }

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

    /// <summary>
    /// 一栋楼。**全部颜色都跟着天光走** —— 这是"昼夜连续变化"里最显眼的一块：
    /// 楼体是"同一个色相压到 16%"，窗是"白天冷玻璃 → 夜里暖灯"，两者都按 `gDayL` 插值。
    ///
    /// ⚠ 不另存一张夜色调色板 —— 那样"哪栋是哪栋"得靠人保持两张表同步（本仓的平行表坑）。
    /// 口径与 `basic/gorilla_pro.bas` 的 `drawBuilding` 一致。
    /// </summary>
    virtual void Draw()
    {
        int bodyC;
        int hiC;
        int edgeC;
        int litC;
        int offC;
        int frmC;
        int cols;
        int rows;
        int c;
        int r;
        int wx;
        int wy;
        int litN;

        // 楼体：夜色 = **同一个色相压到 16%**，按天光在两者之间插值
        bodyC = mixcol(baseR * 16 / 100, baseG * 16 / 100, baseB * 16 / 100,
                       baseR, baseG, baseB, gDayL);
        ui_rect(x, y, w, h, bodyC, 1, 0, 0);

        // 受光面：左侧 3px 亮一点（白天更明显）；右侧 2px 压暗 —— 楼才有体积感
        hiC = mixcol(baseR, baseG, baseB, 255, 255, 255, 6 + gDayL * 12 / 100);
        edgeC = mixcol(baseR, baseG, baseB, 0, 0, 0, 14 + gDayL * 10 / 100);
        ui_rect(x, y, 3, h, hiC, 1, 0, 0);
        ui_rect(x + w - 2, y, 2, h, edgeC, 1, 0, 0);

        // 檐口：比楼体亮一档（白天像被阳光打亮，夜里像被月光勾了一道边）
        ui_rect(x, y, w, 4, mixcol(baseR, baseG, baseB, 255, 255, 255, 18 + gDayL * 22 / 100), 1, 0, 0);

        // 窗：白天是玻璃（冷色反光），夜里点灯（暖黄）；**夜里亮的窗更多**。
        litC = mixcol(255, 198, 104, 226, 236, 244, gDayL);
        offC = mixcol(14, 14, 24, 52, 74, 96, gDayL);
        frmC = mixcol(baseR, baseG, baseB, 0, 0, 0, 45);
        litN = 4 - gDayL * 2 / 100;

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
                // 窗框 + 玻璃（**有框才像窗**，没框就是一排色块）
                ui_rect(wx - 1, wy - 1, 11, 14, frmC, 1, 0, 0);
                if (((c + r + x / 8) % 5) < litN)
                {
                    ui_rect(wx, wy, 9, 12, litC, 1, 0, 0);
                }
                else
                {
                    ui_rect(wx, wy, 9, 12, offC, 1, 0, 0);
                }
                r = r + 1;
            }
            c = c + 1;
        }

        // 门（贴楼底）。⚠ 被弹坑盖住是对的 —— 坑就是"打没了"。
        ui_rect(x + w / 2 - 6, gy - 16, 12, 16, frmC, 1, 0, 3);
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

    // 触摸拖条直接落值（钳位与 `Aim`/`Boost` 同源，别在面板那边再写一遍）
    void SetAngle(int a)
    {
        angle = a;
        if (angle < 1) { angle = 1; }
        if (angle > 89) { angle = 89; }
    }

    void SetPower(int pw)
    {
        power = pw;
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
        // ⚠ **不要**在这里再加一次风的冲量：`wind` 是**加速度**（定点单位/拍²），
        //   已经由 `Step` 每拍累加。原先写 `svx + wind * FP / 4` —— 风本来就是定点量、
        //   不用再过 `FP`，那一下把它放大 **64 倍**，直接盖过初速 ⇒ 香蕉**反着飞**。
        vx = svx;
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
// 天上飞的：鸟 / 飞碟 / 飞机
//
// 这一族是**继承 + 虚函数**的正经用法（不是为了炫技）：
//   · 三者的**走法**完全不同（鸟乱抖乱掉头、飞碟悬停、飞机匀速直线）；
//   · 但游戏只关心"你在哪、你能不能被打到"这两件事。
// 基类 `Flyer` 定 `Step()` / `R()` / `Draw()` 三个虚接口，派生类各写各的。
// ════════════════════════════════════════════════════════════════════

class Flyer : public Entity
{
public:
    int vx;
    int live;            // 1 = 在场
    int kind;            // 1 鸟 / 2 飞碟 / 3 飞机

    Flyer()
    {
        x = 0;
        y = 0;
        vx = 0;
        live = 0;
        kind = 0;
    }

    virtual int R() { return 10; }                  // 命中半径（画多大就判多大）
    virtual void Step(int sw, int gy) { }           // 一拍的运动
    virtual void Draw() { }
};

// ── 鸟：进哪边随机、高度隔一阵抖一下、偶尔掉头（只掉有限次）──
class Bird : public Flyer
{
public:
    int wob;
    int t;
    int turns;

    Bird()
    {
        kind = 1;
        wob = 0;
        t = 0;
        turns = 0;
    }

    void Spawn(int sw, int gy, int dir)
    {
        live = 1;
        turns = 0;
        t = 0;
        vx = 3 * dir;
        if (dir > 0) { x = -20; }
        else { x = sw + 20; }
        y = gy / 4 + ui_rand(gy / 3);
        wob = ui_rand(3) - 1;
    }

    virtual int R() { return 10; }

    virtual void Step(int sw, int gy)
    {
        if (live == 0) { return; }
        t = t + 1;
        if (t % 12 == 0) { wob = ui_rand(3) - 1; }
        if (t % 90 == 0) { if (turns < 2) { vx = -vx; turns = turns + 1; } }
        x = x + vx;
        y = y + wob;
        if (y < 30) { y = 30; }
        if (y > gy - 60) { y = gy - 60; }
        if (x < -40 || x > sw + 40) { live = 0; }
    }

    virtual void Draw()
    {
        if (live == 0) { return; }
        ui_circle(x, y, 6, 0xFF30343C, 1, 0);
        ui_circle(x + 4, y - 4, 4, 0xFF30343C, 1, 0);
        ui_rect(x + 7, y - 5, 4, 2, 0xFFFFC060, 1, 0, 0);
        if (wob >= 0) { ui_line(x - 6, y, x - 14, y + 5, 0xFF50565E, 3); }
        else { ui_line(x - 6, y, x - 14, y - 5, 0xFF50565E, 3); }
    }
};

// ── 飞碟：飞来 → 悬停一会儿 → 飞走；悬停时灯闪 ──
class Ufo : public Flyer
{
public:
    int phase;
    int hold;
    int t;
    int lit;

    Ufo()
    {
        kind = 2;
        phase = 0;
        hold = 0;
        t = 0;
        lit = 0;
    }

    void Spawn(int sw, int gy, int dir)
    {
        live = 1;
        phase = 0;
        t = 0;
        lit = 0;
        vx = 4 * dir;
        if (dir > 0) { x = -30; }
        else { x = sw + 30; }
        y = gy / 3 + ui_rand(gy / 4);
        hold = sw / 2 + ui_rand(sw / 3);
    }

    virtual int R() { return 16; }

    virtual void Step(int sw, int gy)
    {
        if (live == 0) { return; }
        t = t + 1;
        if (t % 4 == 0) { lit = 1 - lit; }
        if (phase == 0)
        {
            x = x + vx;
            if (vx > 0) { if (x >= hold) { phase = 1; t = 0; } }
            else { if (x <= hold) { phase = 1; t = 0; } }
        }
        else if (phase == 1)
        {
            y = y + (ui_rand(3) - 1);
            if (t > 45) { phase = 2; }
        }
        else
        {
            x = x + vx;
            if (x < -40 || x > sw + 40) { live = 0; }
        }
    }

    virtual void Draw()
    {
        if (live == 0) { return; }
        ui_circle(x, y, 13, 0xFFB8C0CC, 1, 0);
        ui_circle(x, y - 5, 7, 0xFF97A0AC, 1, 0);
        if (lit != 0)
        {
            ui_circle(x - 9, y + 5, 2, 0xFFFF5050, 1, 0);
            ui_circle(x, y + 7, 2, 0xFFFFE050, 1, 0);
            ui_circle(x + 9, y + 5, 2, 0xFF50FF70, 1, 0);
        }
    }
};

// ── 飞机：定期飞过、匀速直线；夜里机翼有闪灯 ──
class Plane : public Flyer
{
public:
    int t;
    int lit;

    Plane()
    {
        kind = 3;
        t = 0;
        lit = 0;
    }

    void Spawn(int sw, int gy, int dir)
    {
        live = 1;
        t = 0;
        lit = 0;
        vx = 6 * dir;
        if (dir > 0) { x = -40; }
        else { x = sw + 40; }
        y = gy / 5;
    }

    virtual int R() { return 20; }

    virtual void Step(int sw, int gy)
    {
        if (live == 0) { return; }
        t = t + 1;
        if (t % 20 < 10) { lit = 1; } else { lit = 0; }
        x = x + vx;
        if (x < -60 || x > sw + 60) { live = 0; }
    }

    virtual void Draw()
    {
        if (live == 0) { return; }
        ui_rect(x - 16, y - 3, 32, 6, 0xFFD8DEE6, 1, 0, 3);
        ui_line(x - 2, y, x - 12, y - 10, 0xFFB8C0CC, 3);
        ui_line(x + 2, y, x + 8, y - 9, 0xFFB8C0CC, 3);
        ui_rect(x + 6, y - 2, 8, 5, 0xFF6FA8DC, 1, 0, 2);
        if (lit != 0) { ui_circle(x - 14, y - 11, 2, 0xFFFF4040, 1, 0); }
    }
};

// ════════════════════════════════════════════════════════════════════
// Tree —— 地上的树（位置 / 数量 / 高矮都随机）
// ════════════════════════════════════════════════════════════════════

class Tree
{
public:
    int x;
    int y;               // 树根
    int h;               // 树高
    int w;

    Tree()
    {
        x = 0;
        y = 0;
        h = 0;
        w = 0;
    }

    void Setup(int px, int py, int ph, int pw)
    {
        x = px;
        y = py;
        h = ph;
        w = pw;
    }

    void Draw()
    {
        int i;
        int ty;
        int tw;

        ui_rect(x - 2, y - h / 4, 4, h / 4, 0xFF4A3520, 1, 0, 0);   // 树干
        // 树冠：三层越来越小的圆角矩形叠出来（便宜，形状够用）
        i = 0;
        while (i < 3)
        {
            ty = y - h / 4 - (h / 3) * i;
            tw = w * (3 - i) / 3;
            ui_rect(x - tw / 2, ty - h / 3, tw, h / 3, 0xFF2E6B34, 1, 0, 4);
            i = i + 1;
        }
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
        // ±30 定点单位/拍²（≈重力的 21%）—— 一局里的横向偏移够明显，又不至于没法瞄
        v = ui_rand(61) - 30;
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

    void Draw(Ape* a0, Ape* a1, int turn, int sw)
    {
        ui_rect(0, 0, sw, 30, C_HUD_BG, 1, 0, 0);

        ui_text(8, 20, "橙", C_APE0, 13, VML_ANCHOR_LEFT);
        ui_text(30, 20, numstr(a0->score), C_TEXT, 14, VML_ANCHOR_LEFT);

        ui_text(sw - 8, 20, "紫", C_APE1, 13, VML_ANCHOR_RIGHT);
        ui_text(sw - 30, 20, numstr(a1->score), C_TEXT, 14, VML_ANCHOR_RIGHT);

        if (turn == 0) { ui_text(sw / 2, 20, "轮到 橙", C_APE0, 14, VML_ANCHOR_CENTER); }
        else { ui_text(sw / 2, 20, "轮到 紫", C_APE1, 14, VML_ANCHOR_CENTER); }

        // 昼夜标记：白天不写、天黑了才写"夜" —— 比写"昼"省一格，也更像在报状态
        if (gDayL < 40)
        {
            ui_text(sw / 2 + 58, 20, "夜", C_TEXT_DIM, 12, VML_ANCHOR_CENTER);
        }

        // 游戏时间 hh:mm。
        // ⚠ 一律**左对齐**、坐标各自往左让位 —— 用 `VML_ANCHOR_RIGHT` 让它们右对齐
        //   的话几段会叠在同一处（实测「7」和「30」压成了「7月3」）。
        // ⚠ 分钟补零靠判断而不是 `sprintf("%02d")` —— 本平台的 `%` 转换是坏的
        //   （见 KNOWN_DEFECTS 的 9/10）。
        ui_text(sw - 96, 20, numstr(gHour), C_TEXT_DIM, 11, VML_ANCHOR_LEFT);
        ui_text(sw - 89, 20, ":", C_TEXT_DIM, 11, VML_ANCHOR_LEFT);
        if (gMinute < 10)
        {
            ui_text(sw - 84, 20, "0", C_TEXT_DIM, 11, VML_ANCHOR_LEFT);
            ui_text(sw - 77, 20, numstr(gMinute), C_TEXT_DIM, 11, VML_ANCHOR_LEFT);
        }
        else
        {
            ui_text(sw - 84, 20, numstr(gMinute), C_TEXT_DIM, 11, VML_ANCHOR_LEFT);
        }
    }
};

// ════════════════════════════════════════════════════════════════════
// Game —— 总控
// ════════════════════════════════════════════════════════════════════

class Game
{
public:
    // ⚠ 这里写的是"四个具名 `Building` + 文件级指针表 `BL[i]`"。
    //   **当年是因为类里放不了数组字段**（`int data[4];` 报「期望 SEMICOLON」），
    //   现在那个限制已经修好（F25），但**还没收回来**：成员数组的读写整体缺一小截
    //   （见 `d30.cpp` —— 类内部用隐式 `this` 做下标读会算错地址），
    //   而这个文件当前是**能跑的**，不宜为写法好看去动它的内存布局。
    //   等那一截修好，这里就该是 `Building bldgs[4]; Ape apes[2];`。
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
    Bird bird;
    Ufo ufo;
    Plane plane;
    int spawnT;          // 天上添东西的节拍计数

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
    int treeN;

    // 操作面板的几何（绘制与命中**共用**，见 PanelGeometry 的注释）
    int panH;
    int panY;
    int barX;
    int barW;
    int barH;
    int barAy;
    int barPy;
    int fireX;
    int fireY;
    int fireW;
    int fireH;

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
        treeN = 0;
        spawnT = 0;
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
        FLY[0] = &bird;
        FLY[1] = &ufo;
        FLY[2] = &plane;
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
        // 地形是本局的战果，换一局就推倒重来 —— 不清的话上一局打出来的洞会跟着下一局
        // （`Layout` 就是"换一局"的入口：新城市 + 新地形）
        clearHoles();
        // 天空不再"掷一个昼夜"，它跟着游戏时钟连续变化
        sky.Setup(sw, sh, gy);

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

        // 地上的树：**位置 / 数量 / 高矮都随机**（每局不一样）
        treeN = 2 + ui_rand(4);
        i = 0;
        while (i < treeN)
        {
            int tx;
            int th;
            tx = 10 + ui_rand(sw - 20);
            th = 26 + ui_rand(30);
            TREES[i].Setup(tx, gy + 2, th, 14 + ui_rand(12));
            i = i + 1;
        }
        spawnT = 0;
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
        spawnT = spawnT + 1;

        // ── 天上添东西：节拍到了掷一次，天上空着才放 ──
        if (spawnT % 40 == 0)
        {
            int dice;
            int dir;
            dice = ui_rand(20);
            dir = 1;
            if (ui_rand(2) == 0) { dir = -1; }
            if (dice == 0 && FLY[2]->live == 0) { plane.Spawn(sw, gy, dir); }
            else if (dice == 1 && FLY[1]->live == 0) { ufo.Spawn(sw, gy, dir); }
            else if (dice < 4 && FLY[0]->live == 0) { bird.Spawn(sw, gy, dir); }
        }

        // 夜里偶尔来一颗流星

        // 多态：三个派生类各走各的，这里一行覆盖
        i = 0;
        while (i < 3)
        {
            FLY[i]->Step(sw, gy);
            i = i + 1;
        }

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
            // 打到**天上飞的**？→ 空中爆炸、不得分、直接换人（"浪费一个香蕉"，与 BASIC 版一致）
            if (ban.live != 0)
            {
                i = 0;
                while (i < 3)
                {
                    if (hit < 0 && FLY[i]->live != 0)
                    {
                        if (iabs(ban.x - FLY[i]->x) < FLY[i]->R() && iabs(ban.y - FLY[i]->y) < FLY[i]->R())
                        {
                            hit = -3;
                            FLY[i]->live = 0;
                        }
                    }
                    i = i + 1;
                }
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
            if (hit == -3)
            {
                // 空中爆炸：**不得分**，这一发白扔
                hitBy = -1;
                boomX = ban.x;
                boomY = ban.y;
                boomT = 0;
                state = ST_BOOM;
                ban.Stop();
                ui_beep(1200, 50);
                ui_vibrate(30, 0);
                return 1;
            }
            if (hit == -2)
            {
                hitBy = -1;
                boomX = ban.x;
                boomY = ban.y;
                // ⚠ **这一支同时覆盖"撞楼"和"落地"**（见上面那两处 `hit = -2`）——
                //   两种都该留下痕迹，而且半径分两档：撞楼炸得大一点（那是"打进墙里"），
                //   落地小一点。判据是"落点上方有没有楼"，与判定 `hit` 用的是同一个 `Covers`。
                addHole(ban.x, ban.y, 14);      // ⚠ 临时：固定半径，先不调 HoleRadiusAt
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
                if ((*AP[0]).score >= WIN_SCORE || (*AP[1]).score >= WIN_SCORE) { over = 1; }
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
        int k;
        int hitX;
        int hitY;

        if (state != ST_AIM) { return; }
        hitX = 0;
        hitY = 0;

        cur = AP[turn];
        v = (*cur).power * V_UNIT;
        sv = isin((*cur).angle);
        cv = icos((*cur).angle);
        dir = 1;
        if ((*cur).flip != 0) { dir = -1; }

        px = (*cur).HandX() * FP;
        py = (*cur).HandY() * FP;
        vx = v * cv / SIN_SCALE * dir;      // 与 Banana::Launch 一致：风不在起飞时给冲量
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
            if (py / FP > gy) { n = 150; }        // 掉到地平线以下：停
            if (hitX == 0)
            {
                // 撞楼就停在这一格（落点标记画在**它撞上的那栋楼**的屋顶上）
                k = 0;
                while (k < 4)
                {
                    if ((*BL[k]).Covers(px / FP) && py / FP >= (*BL[k]).RoofY())
                    {
                        hitX = px / FP;
                        hitY = py / FP;
                    }
                    k = k + 1;
                }
            }
            n = n + 1;
        }

        // 落点准星：一眼看出"这一发会打哪儿"
        if (hitX != 0)
        {
            ui_rect(hitX - 9, hitY - 1, 19, 3, 0xCCFF5060, 1, 0, 0);
            ui_rect(hitX - 1, hitY - 9, 3, 19, 0xCCFF5060, 1, 0, 0);
            ui_circle(hitX, hitY, 10, 0x80FF5060, 1, 0);
        }
    }

    // ── 操作面板：**直接上手拖**（与 BASIC 版同一套操控）────────────────────
    //
    // 两根值是**可拖的横条**、发射是一个**大按钮** —— 手指点/拖到哪就设到哪，
    // 不需要键盘。键盘（←→↑↓ + 回车）仍保留，接物理键盘时照旧能用。
    //
    // ⚠ 条要**够厚**：`barH = 30` 是照手指定的（8px 的细条在手机上根本按不准）。
    //   命中判定与绘制**共用同一组几何**（`PanelGeometry` 算一次，两边都读），
    //   两边各算一遍就是"看着在条上、点了没反应"。
    // ⚠ 三行**不许重叠**：按钮 6..40、角度条 46..76、力度条 82..112、面板高 122。
    //   第一版把发射键摆在左上、角度条摆在 y+34 —— 键的下沿压在角度条上。
    void PanelGeometry()
    {
        panH = 122;
        panY = sh - panH;
        fireX = 12;
        fireW = 78;
        fireY = panY + 6;
        fireH = 34;
        barX = 96;
        barW = sw - barX - 14;
        if (barW < 40) { barW = 40; }
        barH = 30;
        barAy = panY + 46;
        barPy = panY + 82;
    }

    void DrawBar(int y, int val, int vmax, int col, char* name)
    {
        int w;
        w = barW * val / vmax;
        if (w > barW) { w = barW; }
        ui_rect(barX, y, barW, barH, 0x33FFFFFF, 1, 0, 6);
        if (w > 0)
        {
            ui_rect(barX, y, w, barH, col, 1, 0, 6);
        }
        ui_text(14, y + barH - 9, name, C_TEXT_DIM, 12, VML_ANCHOR_LEFT);
        ui_text(barX + barW - 6, y + barH - 9, numstr(val), C_TEXT, 15, VML_ANCHOR_RIGHT);
    }

    void DrawAimPanel()
    {
        Ape* cur;
        int bc;

        cur = &(*AP[turn]);
        PanelGeometry();

        ui_rect(0, panY, sw, panH, C_HUD_BG, 1, 0, 0);

        bc = C_BANANA;
        if (state != ST_AIM) { bc = 0x66FFE070; }
        ui_rect(fireX, fireY, fireW, fireH, bc, 1, 0, 8);
        if (state == ST_AIM) { ui_text(fireX + fireW / 2, fireY + 23, "发 射", 0xFF201810, 15, VML_ANCHOR_CENTER); }
        else { ui_text(fireX + fireW / 2, fireY + 23, "飞行中", 0xFF201810, 12, VML_ANCHOR_CENTER); }

        DrawBar(barAy, cur->angle, 90, C_WIND, "角度");
        DrawBar(barPy, cur->power, 100, C_BANANA, "力度");

        ui_text(sw - 12, panY + 22, "拖动调值 · 点发射", C_TEXT_DIM, 11, VML_ANCHOR_RIGHT);
    }

    // ── 触摸 / 鼠标：按下的那一点落在哪根条上就改哪个值 ────────────────────
    // `isDown = 1` 只对**按下那一刻**认（拖动路过不算），免得调条时误射 —— 与 BASIC 版一致。
    void HandlePoint(int isDown)
    {
        int px;
        int py;

        if (over != 0)
        {
            if (isDown != 0) { over = 2; }        // 结束画面：点一下 = 请求退出
            return;
        }
        if (state != ST_AIM) { return; }

        px = ui_msg_a();
        py = ui_msg_b();
        PanelGeometry();

        if (py >= barAy && py < barAy + barH)
        {
            if (px >= barX) { (*AP[turn]).SetAngle((px - barX) * 90 / barW); }
        }
        if (py >= barPy && py < barPy + barH)
        {
            if (px >= barX) { (*AP[turn]).SetPower((px - barX) * 100 / barW); }
        }
        if (isDown != 0 && py >= fireY && py < fireY + fireH && px >= fireX && px <= fireX + fireW)
        {
            Fire(&(*AP[turn]));
            ui_beep(660, 40);
        }
    }

    void Draw()
    {
        int i;
        Entity* actors[8];

        // ⚠ **每帧的第一件事**：把这一刻的天光 / 暖色 / 日月位置算出来。
        //   后面每一处配色（天、楼、窗、地、云、弹坑）都读它算出来的那几个全局量。
        clockCompute(sw, gy, 30);

        // 多态绘制：全部当 Entity 指针调 Draw()，实现由各自决定
        actors[0] = &sky;
        actors[1] = &(*BL[0]);
        actors[2] = &b1;
        actors[3] = &b2;
        actors[4] = &(*BL[3]);
        actors[5] = &(*AP[0]);
        actors[6] = &(*AP[1]);
        actors[7] = &ban;

        // 树画在楼房**之前** —— 楼房后画会把树挡住，正好得到"树长在楼缝里"的观感
        i = 0;
        while (i < treeN)
        {
            TREES[i].Draw();
            i = i + 1;
        }

        i = 0;
        while (i < 8)
        {
            actors[i]->Draw();
            i = i + 1;
        }
        bird.Draw();
        ufo.Draw();
        plane.Draw();
        // met.Draw();

        // ── 弹坑：把炸掉的那块**涂回当地的天空色** ─────────────────────────
        // ⚠ 位置很讲究：**在所有楼之后**（坑可能横跨两栋的交界，画在前会被后一栋盖掉）、
        //   **在地面之前**（否则会把地面也啃掉一块）。
        // ⚠ "涂回天空色"要问**这个 y 的天空色**（`clockPartsAt`）—— 天空是渐变的，
        //   写死一个颜色会在楼中间留一块色不对的圆，而且只在某些时段看得出来。
        i = 0;
        while (i < holeN)
        {
            clockPartsAt(holeY[i], gy);
            ui_circle(holeX[i], holeY[i], holeR[i], clockPackParts(), 1, 0);
            i = i + 1;
        }

        // 地面：白天是灰亮的街面，夜里压暗（与楼房同一套天光口径）
        ui_rect(0, gy, sw, sh - gy, mixcol(12, 12, 22, 74, 78, 86, gDayL), 1, 0, 0);

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

        hud.Draw(&(*AP[0]), &(*AP[1]), turn, sw);
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
    int cid;
    int t;
    int k;
    int done;
    int msg[4];
    int score0;
    int score1;

    g.Layout();
    // **全触摸**：不要屏幕手柄区（`VML_WIN_NO_GAMEPAD`）—— 手柄区连折叠条一起吃画布
    // 高度，这个游戏只用手指，没必要为它留一条；顺带锁竖屏（版面按竖屏排）。
    ui_win_open_ex("大猩猩扔香蕉 (C++)", g.sw, g.sh, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_keep_on(1);
    g.NewTurn();

    tid = ui_timer_set(TICK_MS, 0);
    // 游戏时钟：**另一只定时器**，1 真实秒 = 1 游戏分钟（24 真实分钟走完一天）。
    // 为什么不拿物理节拍那只数帧：玩家拖动滑条时主循环会一次抽干几十条 TOUCHMOVE，
    // 帧率完全取决于输入有多密 —— 数帧的话"滑得越勤、时钟跑得越快"。
    cid = ui_timer_set(1000, 0);
    done = 0;

    while (done == 0 && ui_win_closed() == 0)
    {
        g.Draw();

        t = ui_wait_msg(0);
        if (t == VML_MSG_WINDOWCLOSE) { done = 1; }

        if (t == VML_MSG_TIMER)
        {
            // ⚠ **必须按定时器 id 分流**：现在有两只（物理节拍 33ms、游戏时钟 1000ms），
            //   不分的话两只都会走对方的逻辑 —— 时钟按 33ms 飞奔、物理按 1 秒一跳。
            //   消息 A 就是定时器 id。
            if (ui_msg_a() == cid)
            {
                clockTick();
            }
            else
            {
                g.Tick();
                if (g.over != 0)
                {
                    ui_timer_kill(tid);
                    tid = 0;
                }
            }
        }
        if (t == VML_MSG_KEYDOWN) { g.KeyDown(ui_msg_a()); }
        if (t == VML_MSG_KEYUP) { g.KeyUp(ui_msg_a()); }

        // 触摸 / 鼠标 —— **与 BASIC 版同一套**：拖条调值、点按钮发射。
        // 移动事件（TOUCHMOVE）也走同一条，所以按住条一路拖就一直跟手。
        if (t == VML_MSG_TOUCHDOWN || t == VML_MSG_MOUSEDOWN) { g.HandlePoint(1); }
        if (t == VML_MSG_TOUCHMOVE || t == VML_MSG_MOUSEMOVE) { g.HandlePoint(0); }

        if (g.over == 2) { done = 1; }      // 结束画面被点了一下
    }

    if (tid != 0) { ui_timer_kill(tid); }
    if (cid != 0) { ui_timer_kill(cid); }

    // 比分落盘（下次开局问不出来，但先存着 —— 与 BASIC 版同一套键）
    score0 = (*AP[0]).score;
    score1 = (*AP[1]).score;
    ui_store_set("gorilla.hpp.score0", numstr(score0));
    ui_store_set("gorilla.hpp.score1", numstr(score1));

    ui_keep_on(0);
    return 0;
}
