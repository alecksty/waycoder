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
static Building* BL[8];
// 本局的**实际栋数**（4~8，每局随机，见 `Game::Layout`）。
// 放文件级是因为 `HoleRadiusAt` 这个自由函数也要遍历楼 —— 它是按序号表 `BL` 走的。
static int gBldgN;
static Ape* AP[2];
static Tree TREES[8];        // 地上的树（数量/高矮/位置都随机，见 Game::Layout）
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
// 楼体色 —— ⚠ 这四档原本是**暗棕/土黄/石板/藕紫**（0xFF6E5A46 一路下来），
//   白天拉满也只有 RGB(110,90,70)，配上夜景那就是一片糊。玩家报「画面不太好看」
//   很大一半在这里：**天空是鲜的、楼是灰的**，整个画面就"脏"。
//   换成饱和度高的四色（与 BASIC 版那排彩色楼同一个路子）。改这里记得同步 `SetTint`。
#define C_BLDG_A    0xFFE0604E
#define C_BLDG_B    0xFF3FB59A
#define C_BLDG_C    0xFFE8B840
#define C_BLDG_D    0xFF9A6AE0
#define C_WIN_LIT   0xFFFFD070
#define C_WIN_DARK  0xFF2A2A38
#define C_GROUND    0xFF3C5A34
#define C_DIRT      0xFF2A1E14   // 地上的弹坑挖出来的土色（**不是天空色**，见 holesDraw）
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

// 颜色分量。**描边色一律由本体色算出来**，不写死 —— 见 Ape::Draw 的说明。
//
// ⚠⚠ **必须用位运算，不能用 `/` 和 `%`**：本平台的颜色值都带 alpha（`0xFFxxxxxx`），
//   存进 `int` 是**负数** —— 实测 `0xFFFFFFFF` 就是 `-1`，而 C 的整数除法/取模是
//   **向零截断**的 ⇒ `(-1 / 65536) % 256` 得 **0**、`-1 % 256` 得 **-1**，
//   分量全错。症状不是报错，是**画出一个谁也没指定的颜色**（云的底面被算成红褐色横线）。
//   位运算对正负都成立（算术/逻辑右移都行，因为后面 `& 0xFF` 会把高位切掉）。
int colR(int c) { return (c >> 16) & 255; }
int colG(int c) { return (c >> 8) & 255; }
int colB(int c) { return c & 255; }

// ════════════════════════════════════════════════════════════════════
// 落点该炸多大的坑：撞在楼身上大一些（打进墙里），砸在地上小一些。
//
// ⚠ 判据与 `Game::Step` 里判 `hit` 用的是**同一个** `Covers` —— 两处各判一次的话，
//   "显示成撞楼、坑却按地面算"这种不一致迟早会出现。
//
// ⚠ 这个函数曾经**只要存在就让画面全毁**（OPEN #19），当时只好改用固定半径。
//   根因是编译器的一个缺陷（全局对象只分配 1 个 word，字段一写就踩到相邻全局量，
//   见 KNOWN_DEFECTS 的 F38）—— 修掉之后它就正常了。
int HoleRadiusAt(int px, int py)
{
    int i;
    i = 0;
    while (i < gBldgN)
    {
        if ((*BL[i]).Covers(px))
        {
            if (py >= (*BL[i]).RoofY()) { return 16; }
        }
        i = i + 1;
    }
    return 10;
}

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

static int gHour = 7;        // 开局清晨 —— 一进来就是白天，玩家不用干等 —— 一进来就是白天，玩家不用干等
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
// 街灯 —— 地面上几盏，**夜里才亮**
//
// 位置按屏幕宽度均分（不用随机数：路灯本来就该等距，随机反而像故障）。
// 白天灯是暗的（只是几根杆），夜里灯头变暖黄 —— 与窗户同一套天光口径。
// ════════════════════════════════════════════════════════════════════

#define N_LAMP 3

void lampsDraw(int scrW, int groundY, int scrH)
{
    int i;
    int lx;
    int ly;
    int poleC;
    int headC;

    // 杆：白天是深灰（有体积感），夜里更深（背光）
    poleC = mixcol(28, 30, 36, 74, 78, 88, gDayL);
    // 灯头：白天是玻璃（暗），夜里是暖黄 —— 夜里越黑越亮
    headC = mixcol(255, 214, 130, 96, 102, 116, gDayL);

    i = 0;
    while (i < N_LAMP)
    {
        lx = scrW * (i * 2 + 1) / (N_LAMP * 2);     // 均分：1/6、3/6、5/6 处
        ly = groundY;
        // 杆
        ui_rect(lx - 1, ly - 46, 3, 46, poleC, 1, 0, 0);
        // 横臂
        ui_rect(lx - 1, ly - 46, 14, 3, poleC, 1, 0, 0);
        // 灯头（挂在横臂末端）
        ui_rect(lx + 8, ly - 44, 10, 5, headC, 1, 0, 0);
        i = i + 1;
    }
}

// ════════════════════════════════════════════════════════════════════
// 流星 —— 夜里偶尔划过一颗
//
// 起点/方向/速度都由**游戏分钟**推出来（不用随机数）：这样"第几分钟会有流星"
// 是可复现的，出问题时能原样重放；而随机数一旦引入，"这次为什么没出现"就说不清了。
//
// ⚠ 它和云一样，是**按时间算位置**而不是每帧累加 —— 累加会随帧率变快慢。
// ════════════════════════════════════════════════════════════════════

void meteorDraw(int scrW, int groundY)
{
    int cycle;
    int phase;
    int mx;
    int my;
    int tailX;
    int tailY;
    int tx;
    int ty;
    int alpha;

    // 每 7 游戏分钟来一颗；只有夜里（天光低）才看得见
    if (gDayL > 35) { return; }
    cycle = (gHour * 60 + gMinute) % 7;
    if (cycle >= 3) { return; }                  // 只用周期里的前 3 分钟
    phase = cycle * 100 / 3;                     // 0..100

    mx = scrW * 70 / 100 - scrW * phase / 100;   // 从右上往左下
    my = groundY * 30 / 100 + groundY * phase / 100;
    alpha = 100 - phase;                         // 越飞越淡
    if (alpha < 0) { alpha = 0; }

    // ── 尾迹**沿运动方向**（玩家报：「流星太大，而且不是按流线方向跑的」）──
    //
    // ⚠ 原来尾巴是"往右下 26px、再往上一点"的一段**固定轴向**，
    //   而流星本身是**从右上往左下**飞的 —— 两者方向不一致，看着就是"拖错了方向"；
    //   而且尾巴是 6 个半径 5/4/3/2/1/0 的**圆点**，头一颗直径 10px，太大。
    //
    // 现在：尾迹方向**就是运动方向的反向** —— 位移在一个周期里是
    //   `(-scrW, +groundY)`，那么尾巴取它的一个固定比例（这里 8%）就与轨迹严格同向，
    //   不用算归一化（**不能调 isqrt**：它定义在 Building 之后，这里在它之前）。
    tailX = scrW * 8 / 100;
    tailY = groundY * 8 / 100;
    tx = mx + tailX;
    ty = my - tailY;

    clockPartsAt(my, groundY);
    // 外圈一道淡光晕 → 细芯 → 头上一点亮：一条线读出"划过"
    ui_line(tx, ty, mx, my, mixcol(gMr, gMg, gMb, 255, 250, 220, alpha * 40 / 100), 3);
    ui_line(tx, ty, mx, my, mixcol(gMr, gMg, gMb, 255, 252, 235, alpha * 85 / 100), 1);
    ui_circle(mx, my, 2, mixcol(gMr, gMg, gMb, 255, 255, 245, alpha), 1, 0);
}


// ════════════════════════════════════════════════════════════════════
// 云 —— 白天天上那几团，**随游戏时间慢慢飘**，出画从另一头绕回来
//
// 可见度**跟着天光走**（而不是"天黑就不画"的开关）：天光为 0 时云的颜色正好等于
// 当地天空色 ⇒ 自然消失。少一个能写错的开关，也少一次"某一刻画面跳一下"。
//
// ⚠ 位置是**按游戏分钟算的**（不是每帧累加）：累加的话帧率一变云就飘得快慢不一，
//   而"飘"这件事本来就该由时钟定，与画多快无关 —— 与时钟走独立定时器是同一条理由。
// ════════════════════════════════════════════════════════════════════

#define N_CLOUD 5

static int cloudY[N_CLOUD];      // 第 i 朵的基准高度（按屏幕比例，开局定一次）
static int cloudW[N_CLOUD];      // 第 i 朵的宽度
static int cloudSpeed[N_CLOUD];  // 第 i 朵的飘动速度（像素 / 游戏分钟）
static int cloudsReady;

void cloudsInit(int scrW, int groundY)
{
    int i;
    i = 0;
    while (i < N_CLOUD)
    {
        // 位置/大小由下标推出来（**不用随机数**：每局都该长得差不多，
        // 而且随机的话"这朵云什么时候飘回来"就不可预期了）
        cloudY[i] = 40 + (i * 53) % (groundY / 2);
        cloudW[i] = 60 + (i * 37) % 70;
        cloudSpeed[i] = 2 + (i % 3);
        i = i + 1;
    }
    cloudsReady = 1;
}

// 第 i 朵云现在的左边 x（按游戏分钟算，出画就从另一头绕回来）
int cloudX(int idx, int scrW)
{
    int span;
    int pos;
    span = scrW + 160;                       // 多留 160，让云整个出画再回来
    pos = cloudSpeed[idx] * (gHour * 60 + gMinute) + idx * 97;
    pos = pos % span;
    return pos - 120;
}

// 一朵云 = 三团椭圆叠出来（便宜，形状够用）
void cloudsDraw(int scrW, int groundY)
{
    int i;
    int cx;
    int cy;
    int cw;
    int ch;
    int col;
    int shade;

    if (cloudsReady == 0) { return; }

    i = 0;
    while (i < N_CLOUD)
    {
        cx = cloudX(i, scrW);
        cy = cloudY[i];
        cw = cloudW[i];
        ch = cw / 3;
        clockPartsAt(cy, groundY);
        // 云色 = 当地天空色 → 白，按天光插值 ⇒ 天全黑时云正好融进天空。
        // ⚠ 系数要**够狠**：天光 52（早上 7:30）时若只插到 44%，云就是 `0xFF8094B3`
        //   这种灰蓝，比天空亮一点点、**肉眼看不出来**（实测过）。
        //   乘 1.6、封顶 100 ⇒ 天光 63 以上就是纯白的云，日出前后才淡出。
        col = mixcol(gMr, gMg, gMb, 255, 255, 255, gDayL * 160 / 100);
        if (gDayL * 160 / 100 > 100) { col = mixcol(gMr, gMg, gMb, 255, 255, 255, 100); }
        shade = mixcol(colR(col), colG(col), colB(col), gMr, gMg, gMb, 30);

        // ⚠ 原来是"三颗球叠一起"，玩家原话是「云就是圆球？」
        //   云之所以一眼是云，靠的是**底边接近一条水平线**、只有上半是鼓的。
        //   所以改成：一条扁的圆角底座（两端自然收成半圆）+ 顶上加三个鼓包。
        //   只画圆的话，无论怎么排都会读成"一堆球" —— 差别全在这条底边。
        ui_rect(cx, cy - ch / 2, cw, ch, col, 1, 0, ch / 2);
        ui_circle(cx + cw / 5,     cy - ch / 2, ch * 3 / 4, col, 1, 0);
        ui_circle(cx + cw / 2,     cy - ch,     ch,         col, 1, 0);
        ui_circle(cx + cw * 4 / 5, cy - ch / 2, ch * 2 / 3, col, 1, 0);
        // 底面一道暗色 —— 有它才有体积感，否则还是"贴纸"
        ui_rect(cx + ch / 2, cy + ch / 2 - 3, cw - ch, 3, shade, 1, 0, 1);
        i = i + 1;
    }
}



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
        baseR = 224; baseG = 96; baseB = 78;
    }

    /// <summary>
    /// 定色相。**存分量而不是只存档号** —— 后面每一处配色（楼体、受光面、檐口、窗框）
    /// 都要以它为基准按天光插值，每次再从档号反解一遍颜色分量是"同一件事两处实现"。
    /// 四个色与 `C_BLDG_A..D` 一一对应（BASIC 版是 6 档，这里沿用本文件原有的 4 档）。
    /// </summary>
    void SetTint(int t)
    {
        tint = t;
        baseR = 224; baseG = 96;  baseB = 78;     // 0 珊瑚红
        if (t == 1) { baseR = 63;  baseG = 181; baseB = 154; }   // 1 青绿
        if (t == 2) { baseR = 232; baseG = 184; baseB = 64;  }   // 2 明黄
        if (t == 3) { baseR = 154; baseG = 106; baseB = 224; }   // 3 紫
        if (t == 4) { baseR = 74;  baseG = 144; baseB = 217; }   // 4 天蓝
        if (t == 5) { baseR = 230; baseG = 140; baseB = 60;  }   // 5 橙
        if (t == 6) { baseR = 224; baseG = 106; baseB = 152; }   // 6 粉
        if (t == 7) { baseR = 122; baseG = 190; baseB = 84;  }   // 7 草绿
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

        // 楼体：夜色 = **同一个色相压到 26% 再偏一点冷蓝**，按天光在两者之间插值。
        // ⚠ 原来压到 16% 且不加蓝 ⇒ 夜里几乎全黑，四栋楼糊成一片分不出彼此；
        //   月光本身是冷的，给夜景补 18 点蓝分量，暗是暗、但"看得见是几栋楼"。
        bodyC = mixcol(baseR * 26 / 100, baseG * 26 / 100, baseB * 26 / 100 + 18,
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
                // ⚠ **一楼只开门、不开窗** —— 原来窗是整列均匀铺到底的，最后一行正好
                //   压在门上（门在 `gy-20 .. gy`），玩家看到的就是"窗户和门重合"。
                //   压在门那一段的窗**一格都不画**（判据用门框的顶 `gy-20`）。
                if (wy + 14 <= gy - 20)
                {
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
                }
                r = r + 1;
            }
            c = c + 1;
        }

        // 门（贴楼底）。⚠ 被弹坑盖住是对的 —— 坑就是"打没了"。
        // 门框 + 门板 + 底下透出来的一条亮光（屋里有人，门就"活"了）
        ui_rect(x + w / 2 - 9, gy - 20, 18, 20, frmC, 1, 0, 3);
        ui_rect(x + w / 2 - 7, gy - 18, 14, 18, mixcol(baseR, baseG, baseB, 0, 0, 0, 60), 1, 0, 2);
        // 门缝里透出来的一条灯光 —— 比整扇门发亮自然，也不会在白天显得奇怪
        ui_rect(x + w / 2 - 7, gy - 7, 14, 2, litC, 1, 0, 0);

        // ── 屋顶细节：水箱 / 天线 / 平的 ────────────────────────────────
        // 按**楼号**分（`tint` 就是它在 `Layout` 里的序号），三种轮着来。
        //
        // ⚠ **0 号与 3 号不给** —— 那是两只猴子站的地方，加个水箱/天线正好跟猿抢位置
        //   （BASIC 版 `drawBuilding` 里同样是 `IF bi > 0 THEN IF bi < nb1`）。
        //   所以实际只有中间那两栋有屋顶细节，看着正好错落。
        //
        // 用同色系的深浅色，不要新颜色 —— 屋顶是"同一栋楼的顶"，不是另一种建筑。
        if (tint == 1)
        {
            // 水箱：一个小方块 + 两条腿
            ui_rect(x + 7, y - 15, 15, 11,
                    mixcol(baseR, baseG, baseB, 255, 255, 255, 10), 1, 0, 2);
            ui_line(x + 10, y - 4, x + 10, y, frmC, 2);
            ui_line(x + 19, y - 4, x + 19, y, frmC, 2);
        }
        if (tint == 2)
        {
            // 天线：一根细杆 + 两道横档。
            // ⚠ **顶上不要亮点** —— 用户一眼就把它当成"屋子上的路灯"（路灯在街上，见 `lampsDraw`）。
            ui_line(x + w - 15, y - 22, x + w - 15, y, frmC, 2);
            ui_line(x + w - 19, y - 18, x + w - 11, y - 18, frmC, 2);
            ui_line(x + w - 17, y - 12, x + w - 13, y - 12, frmC, 2);
        }
    }
};

// ⚠ `holesDraw` 与 `isqrt` **必须放在 `Building` 之后**：这个前端不做前向引用，
//   而这里要用 `Building` 的 `Covers/Left/Right/RoofY`、还要用颜色宏 `C_DIRT`，
//   那两样都定义在后面。（第一版插在 `addHole` 后面，直接编不过。）
// 整数平方根（牛顿迭代）—— 凿洞时要按行算半宽 `sqrt(r^2 - dy^2)`。
// 平台没有 sqrt 可用，就这几行，自己带一个。
int isqrt(int v)
{
    int x;
    int y;
    if (v <= 0) { return 0; }
    x = v;
    y = (x + 1) / 2;
    while (y < x)
    {
        x = y;
        y = (x + v / x) / 2;
    }
    return x;
}

// ── 弹坑：**在墙上真正凿一个洞**（而不是盖一个天空色的圆）──────────────
//
// ⚠ 原先就是在坑的位置盖一个**天空色的实心圆**，玩家在手机上点出了两个毛病：
//   ① 那一趟画在**香蕉之后** ⇒ 香蕉飞过坑口被盖住，**看着像撞上一块看不见的墙**；
//   ② 一整块**平色**盖上去 ⇒ 天空是渐变的，坑里那块色对不上；
//      而且它会把**后面的云 / 日月一起盖掉** —— 那些东西本来是"透过洞该看见"的。
//
// 现在的做法（平台**没有** clip/mask 接口，所以这一层只能程序自己裁剪）：
//   · **逐行凿**：每行一条 1 像素高的横条，半宽按 `sqrt(r² - dy²)` 算 ⇒ 出来是圆的；
//   · **每行单独取色**（`clockPartsAt(y)`）⇒ 天空渐变在洞里自然接得上，没有补丁感；
//   · **横向夹到所在那栋楼的范围内** —— 这就是"裁剪"，洞不会糊到隔壁楼上去；
//   · **纵向夹在楼顶与地面之间** —— 洞不会翻到楼顶外面去。
//   · 地上的坑**涂土色而不是天空色**（原来地上也是个蓝洞，那显然不对）。
//
// ⚠ 调用时机也是修的一部分：必须夹在**楼之后、香蕉之前**（见 `Game::Draw`）。
void holesDraw(int groundY, int scrH)
{
    int i;
    int j;
    int dy;
    int y;
    int hw;
    int x0;
    int x1;
    int r;
    int left;
    int right;
    int roof;
    int onBldg;
    int col;

    i = 0;
    while (i < holeN)
    {
        r = holeR[i];

        // 这个坑开在**哪一栋楼**上？（一栋都没覆盖 ⇒ 是打在地上的坑）
        onBldg = 0;
        left = 0;
        right = 0;
        roof = groundY;
        j = 0;
        while (j < gBldgN)
        {
            if ((*BL[j]).Covers(holeX[i]))
            {
                onBldg = 1;
                left = (*BL[j]).Left();
                right = (*BL[j]).Right();
                roof = (*BL[j]).RoofY();
            }
            j = j + 1;
        }

        dy = -r;
        while (dy <= r)
        {
            y = holeY[i] + dy;
            if (y >= 0 && y < scrH)
            {
                hw = isqrt(r * r - dy * dy);
                x0 = holeX[i] - hw;
                x1 = holeX[i] + hw;

                // ⚠ **楼上的洞不在这里画** —— 见 `Game::Draw` 那段：那边用**蒙版**
                //   把背景**重画一遍**，洞后面才是"活"的（云、日月会从洞里露出来）。
                //   这里只负责**地上的坑**（挖土）。
                if (y >= groundY)
                {
                    // 地上的坑：挖土
                    if (x1 > x0) { ui_rect(x0, y, x1 - x0, 1, C_DIRT, 1, 0, 0); }
                }
            }
            dy = dy + 1;
        }
        i = i + 1;
    }
}

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
        int dark;
        int light;

        // ⚠ **描边色与浅色都由本体色算出来，不写死**。
        //   原来写死 `C_APE_DARK`（深棕），橙猴子看着还行，**紫猴子配深棕是发闷的** ——
        //   玩家当时的原话是「猴子没有轮廓」。由本体色推出来的描边在任何配色下都成立。
        dark = mixcol(colR(body), colG(body), colB(body), 12, 8, 6, 62);
        light = mixcol(colR(body), colG(body), colB(body), 255, 245, 235, 45);

        // ── 腿脚（先画，躯干盖住上沿）────────────────────────────
        ui_rect(x - 12, y - 9, 11, 9, body, 1, 0, 3);
        ui_rect(x + 1, y - 9, 11, 9, body, 1, 0, 3);
        ui_rect(x - 12, y - 9, 11, 9, dark, 0, 2, 3);
        ui_rect(x + 1, y - 9, 11, 9, dark, 0, 2, 3);

        // ── 后边那只胳膊（垂着）──────────────────────────────────
        ui_line(x - 11, y - 25, x - 16, y - 8, dark, 9);
        ui_line(x - 11, y - 25, x - 16, y - 8, body, 6);
        ui_circle(x - 16, y - 8, 5, body, 1, 0);
        ui_circle(x - 16, y - 8, 5, dark, 0, 2);

        // ── 躯干：宽肩（大猩猩的体型就是"肩膀比头宽"）────────────
        ui_rect(x - 14, y - 30, 28, 23, body, 1, 0, 9);
        ui_rect(x - 14, y - 30, 28, 23, dark, 0, 2, 9);
        // 浅色胸腹 —— 有它才不会读成"一块方砖"
        ui_ellipse(x, y - 20, 8, 9, light, 1, 0);

        // ── 头：耳朵 → 头 → 眉骨 → 吻部 → 眼 ─────────────────────
        ui_circle(x - 11, y - 39, 5, body, 1, 0);
        ui_circle(x + 11, y - 39, 5, body, 1, 0);
        ui_circle(x - 11, y - 39, 5, dark, 0, 2);
        ui_circle(x + 11, y - 39, 5, dark, 0, 2);
        ui_circle(x, y - 40, 12, body, 1, 0);
        ui_circle(x, y - 40, 12, dark, 0, 2);
        // 眉骨：一条压低的横条 —— **这一笔是"看出是猩猩"的关键**，
        //   光有一个圆头，放大到手机屏幕上就是个球。
        ui_rect(x - 9, y - 47, 18, 4, dark, 1, 0, 2);
        // 吻部
        ui_ellipse(x, y - 33, 7, 5, light, 1, 0);
        ui_ellipse(x, y - 33, 7, 5, dark, 0, 2);
        // 眼睛（朝对手那边）
        if (flip != 0)
        {
            ui_circle(x - 5, y - 42, 2, C_TEXT, 1, 0);
        }
        else
        {
            ui_circle(x + 5, y - 42, 2, C_TEXT, 1, 0);
        }

        // ── 举起来那只胳膊（按角度画）—— 玩家看得见自己调的角度 ──
        len = 26;
        hx = x + icos(angle) * len / SIN_SCALE * (1 - 2 * flip);
        hy = y - 24 - isin(angle) * len / SIN_SCALE;
        ui_line(x, y - 24, hx, hy, dark, 10);   // 先粗的深色当描边
        ui_line(x, y - 24, hx, hy, body, 6);    // 再细的本体色
        ui_circle(hx, hy, 5, body, 1, 0);
        ui_circle(hx, hy, 5, dark, 0, 2);
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
        int d;
        int bx;

        if (live == 0) { return; }

        // ⚠ 朝向必须**按当前速度算**（`vx` 会被 `t % 90` 那个掉头翻转），
        //   不能按出生时的 `dir`。原来整套图形是按"朝右"写死的（头在 x+4、喙在 x+7、
        //   尾巴在 x-14）⇒ **掉头之后就是倒着飞**（玩家报的正是这个）。
        d = 1;
        if (vx < 0) { d = -1; }

        ui_circle(x, y, 6, 0xFF30343C, 1, 0);
        ui_circle(x + 4 * d, y - 4, 4, 0xFF30343C, 1, 0);
        bx = x + 7;
        if (d < 0) { bx = x - 11; }
        ui_rect(bx, y - 5, 4, 2, 0xFFFFC060, 1, 0, 0);
        if (wob >= 0) { ui_line(x - 6 * d, y, x - 14 * d, y + 5, 0xFF50565E, 3); }
        else { ui_line(x - 6 * d, y, x - 14 * d, y - 5, 0xFF50565E, 3); }
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
        int hull;
        int hullD;
        int glass;
        int glassD;

        if (live == 0) { return; }

        hull = 0xFFC8D0DC;
        hullD = 0xFF5A6472;
        glass = 0xFF8ADCFF;
        glassD = 0xFF2E6E9E;

        // ⚠ 原来是「大球 + 小球」，玩家原话是「飞碟也是个球」。
        //   飞碟之所以一眼是飞碟，靠的是**宽扁的碟身 + 顶上一个罩子**这两条轮廓线，
        //   光有圆是读不出来的。
        // 悬停的光晕（先画，后面被碟身压住一半）
        ui_ellipse(x, y + 10, 17, 4, 0x40A0E0FF, 1, 0);

        // ── 座舱罩：**先画一整个圆**，碟身随后盖掉它的下半 ⇒ 正好剩一个半圆罩 ──
        //    （本平台没有裁剪，这个"画完再盖"就是最省事的做法）
        ui_circle(x, y - 6, 10, glass, 1, 0);
        ui_circle(x, y - 6, 10, glassD, 0, 2);
        ui_ellipse(x - 4, y - 9, 3, 2, 0xFFFFFFFF, 1, 0);   // 罩子高光

        // ── 碟身：宽扁椭圆（宽:高 ≈ 3:1 才像碟）──
        ui_ellipse(x, y + 1, 22, 7, hull, 1, 0);
        ui_ellipse(x, y + 1, 22, 7, hullD, 0, 2);
        // 碟身上半的一道亮边，给它"金属盘子"的感觉
        ui_ellipse(x, y - 1, 17, 3, 0xFFE8EEF6, 1, 0);

        // ── 底下一圈灯 ──
        if (lit != 0)
        {
            ui_circle(x - 15, y + 5, 2, 0xFFFF5050, 1, 0);
            ui_circle(x - 8,  y + 7, 2, 0xFFFFE050, 1, 0);
            ui_circle(x,      y + 8, 2, 0xFF50FF70, 1, 0);
            ui_circle(x + 8,  y + 7, 2, 0xFF50D0FF, 1, 0);
            ui_circle(x + 15, y + 5, 2, 0xFFFF5050, 1, 0);
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
        int d;
        int cx;

        if (live == 0) { return; }

        // 与鸟同一个道理：座舱与尾翼都按**当前速度**镜像（原来写死朝右）
        d = 1;
        if (vx < 0) { d = -1; }

        ui_rect(x - 16, y - 3, 32, 6, 0xFFD8DEE6, 1, 0, 3);
        ui_line(x - 2 * d, y, x - 12 * d, y - 10, 0xFFB8C0CC, 3);
        ui_line(x + 2 * d, y, x + 8 * d, y - 9, 0xFFB8C0CC, 3);
        cx = x + 6;
        if (d < 0) { cx = x - 14; }
        ui_rect(cx, y - 2, 8, 5, 0xFF6FA8DC, 1, 0, 2);
        if (lit != 0) { ui_circle(x - 14 * d, y - 11, 2, 0xFFFF4040, 1, 0); }
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
        int cy;
        int r;
        int v;              // 本棵的绿色档（0..2）—— 按位置取，不用额外字段
        int dark;
        int mid;
        int lit;

        // ⚠ 原来树冠是**三层方角矩形**叠出来的 —— 放小了一看就是"绿色方块"，
        //   与云那时候同一个毛病。树冠就用圆：一大两小拼一顶，再补受光面。
        // 三档绿，按 x 取（同一棵每帧稳定，每棵树之间不一样）
        v = (x / 7) % 3;
        dark = 0xFF1E4A22;
        mid  = 0xFF2E7A38;
        lit  = 0xFF57B05E;
        if (v == 1) { dark = 0xFF22401E; mid = 0xFF4A7A2E; lit = 0xFF78C05A; }
        if (v == 2) { dark = 0xFF173F2A; mid = 0xFF2A6E4A; lit = 0xFF4FA87A; }

        // 树干
        ui_rect(x - 2, y - h / 3, 5, h / 3 + 2, dark, 1, 0, 1);
        ui_rect(x - 1, y - h / 3, 3, h / 3, 0xFF5A4326, 1, 0, 1);

        // 树冠：两侧小圆先画（当底部层次），主冠盖上去，最后左上一块受光
        cy = y - h * 2 / 3;
        r = w / 2 + 2;
        ui_circle(x - r * 3 / 4, cy + r / 3, r / 2, dark, 1, 0);
        ui_circle(x + r * 3 / 4, cy + r / 3, r / 2, dark, 1, 0);
        ui_circle(x, cy, r, mid, 1, 0);
        ui_circle(x - r / 4, cy - r / 3, r * 2 / 3, lit, 1, 0);
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
    Building b4;
    Building b5;
    Building b6;
    Building b7;
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
        BL[4] = &b4;
        BL[5] = &b5;
        BL[6] = &b6;
        BL[7] = &b7;
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
        int avail;          // 扣掉间隔之后、留给楼体的总宽
        int wsum;           // 权重之和
        int wW[8];          // 每栋的**宽度权重**（见下面"权重法"的说明）
        int tintOff;        // 配色的起始档 —— 每局换个顺序，连玩两局不会"又是那排色"

        sw = ui_scr_w();
        sh = ui_scr_h();
        if (sw <= 0) { sw = 411; }
        if (sh <= 0) { sh = 726; }

        gy = sh * 74 / 100;
        // 地形是本局的战果，换一局就推倒重来 —— 不清的话上一局打出来的洞会跟着下一局
        // （`Layout` 就是"换一局"的入口：新城市 + 新地形）
        clearHoles();
        cloudsInit(sw, gy);
        // 天空不再"掷一个昼夜"，它跟着游戏时钟连续变化
        sky.Setup(sw, sh, gy);

        // ── 楼：**4~8 栋、宽度不一**（玩家要求："房子数量应该是 4 到 8 个不等，
        //    宽度不固定，随机的"）────────────────────────────────────────
        gap = sw / 40;
        gBldgN = 4 + ui_rand(5);            // 4..8
        avail = sw - gap * (gBldgN + 1);
        if (avail < 60) { avail = 60; }

        // 宽度用**权重法**：先随机每栋的权重，再按权重去分总宽。
        // ⚠ 别直接给每栋随机一个宽度 —— 那些数加起来**不等于**可用宽度，
        //   右边不是空一截就是溢出屏幕（"随机的"要的是宽窄不一，不是铺不满）。
        wsum = 0;
        i = 0;
        while (i < gBldgN)
        {
            wW[i] = 5 + ui_rand(11);        // 5..15
            wsum = wsum + wW[i];
            i = i + 1;
        }

        tintOff = ui_rand(8);
        x = gap;
        i = 0;
        while (i < gBldgN)
        {
            bw = avail * wW[i] / wsum;
            if (bw < 22) { bw = 22; }       // 太窄站不下猴子
            if (i == gBldgN - 1)
            {
                // **最后一栋吃掉余量** ⇒ 右边严丝合缝；只要剩得下，就不留缝
                int rest;
                rest = sw - gap - x;
                if (rest >= 22) { bw = rest; }
            }
            h = sh / 5 + ui_rand(sh / 4);
            // 两边的楼高一点，好站（猴子站在最左和最右那两栋上）
            if (i == 0 || i == gBldgN - 1) { h = h + sh / 14; }
            (*BL[i]).Setup(x, bw, h, gy);
            (*BL[i]).SetTint((i + tintOff) % 8);
            x = x + bw + gap;
            i = i + 1;
        }

        (*AP[0]).StandOn(&(*BL[0]));
        (*AP[1]).StandOn(&(*BL[gBldgN - 1]));

        // 地上的树：**位置 / 数量 / 高矮都随机**（每局都不一样）
        treeN = 3 + ui_rand(6);             // 3..8
        i = 0;
        while (i < treeN)
        {
            int tx;
            int th;
            int tw;
            tx = 12 + ui_rand(sw - 24);
            th = 24 + ui_rand(42);          // 24..65，高矮差一倍多才看得出"随机"
            tw = 12 + ui_rand(16);          // 12..27
            TREES[i].Setup(tx, gy + 4, th, tw);
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
                while (i < gBldgN)
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
                addHole(ban.x, ban.y, HoleRadiusAt(ban.x, ban.y));
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
                while (k < gBldgN)
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
        Entity* actors[16];
        int actorN;
        int onBldg;         // 这个弹坑是不是开在楼上（见下面那段蒙版重画）
        int k;

        // ⚠ **每帧的第一件事**：把这一刻的天光 / 暖色 / 日月位置算出来。
        //   后面每一处配色（天、楼、窗、地、云、弹坑）都读它算出来的那几个全局量。
        clockCompute(sw, gy, 30);

        // ⚠⚠ **每帧必须清一次场景图元** —— 这一句看着多余（下面 `Sky` 会用渐变铺满整屏，
        //   视觉上早就盖住了上一帧），但**图元数组不是"视觉上的覆盖"，是只增不减的列表**：
        //   不清的话每帧的几百个图元一直堆，跑几十帧就撞上宿主的 `MaxFigures = 12000`，
        //   之后**新加的绘制被静默丢弃** —— 症状是"画面里靠后画的东西（猴子、后两栋楼）
        //   逐个消失，而且越跑越少"，而**坐标、颜色全都是对的**（trace 里能看到它们被正常发出）。
        //   实测：这份程序跑到第 50 帧左右开始缺，正是 12000 ÷ 每帧图元数。
        //   宿主那边给的提示是「程序很可能漏了 ui_clear()」——就是这么回事。
        // 用**天顶色**清（与 BASIC 版同一口径）：天空随后会用渐变整块盖上，
        //   清成什么色其实看不见，但取天色能保证万一渐变没铺满时露出的也是天空色。
        ui_clear(clockZenith());

        // 多态绘制：全部当 Entity 指针调 Draw()，实现由各自决定
        // ⚠ 画表**按本局实际栋数拼**，不能写死下标 ——
        //   原来写的是 actors[1..4] = 四栋楼，栋数一变（4~8）就会漏画或越界。
        actorN = 0;
        actors[actorN] = &sky;     actorN = actorN + 1;
        i = 0;
        while (i < gBldgN) { actors[actorN] = &(*BL[i]); actorN = actorN + 1; i = i + 1; }
        actors[actorN] = &(*AP[0]); actorN = actorN + 1;
        actors[actorN] = &(*AP[1]); actorN = actorN + 1;
        actors[actorN] = &ban;      actorN = actorN + 1;


        // ⚠ **`actors[0]` 是 `Sky`，它会把整片天空铺一遍** —— 所以云必须画在它**之后**，
        //   否则刚画好的云立刻被天空盖掉（实测：云的位置颜色都对，屏幕上却什么都没有）。
        //   这与 BASIC 版 `drawScene` 的次序一致：天空 → 星星 → 日月 → 云 → 飞行物 → 楼。
        //   代价是这里得把 `actors` 的循环拆成两段 —— 云不属于任何 `Entity` 对象。
        actors[0]->Draw();
        cloudsDraw(sw, gy);
        meteorDraw(sw, gy);

        // ── 顺序：**楼 → 凿洞 → 其余** ─────────────────────────────────
        // ⚠ `actors[1..gBldgN]` 是楼，后面依次是猴子、香蕉。这个循环原先是一整趟跑完的，
        //   于是弹坑只能画在**最后**，把香蕉也盖住了 —— 玩家说的
        //   「被炸穿的地方还是会挡住香蕉」就是这么来的（香蕉是飞在半空的，
        //   而坑是开在墙上的，墙在香蕉**后面**）。
        i = 1;
        while (i <= gBldgN)
        {
            actors[i]->Draw();
            i = i + 1;
        }

        // ── 楼上的弹坑：**用蒙版把背景重画一遍**（洞后面才"活"起来）──────────
        //
        // ⚠ 原先是在洞里涂"当时的天空色"。天空渐变虽然接得上，但
        //   **洞后面的云 / 日月不会露出来** ⇒ 一眼就看出"洞是贴上去的"
        //   （玩家报的「破洞还会挡住背景」就是这么来的）。
        //   正解就是蒙版：开一个**洞口形状**的蒙版 → **重画一遍背景** → 关蒙版。
        //   `ui_mask_end(1)` = "只在里面画"。
        //
        // ⚠ 代价要盯着：**每个洞重画一遍背景**（天空渐变 + 星 + 日月 + 云），
        //   24 个洞就是 24 倍。所以只对**楼上的洞**做（地上的坑挖土就够），
        //   而且背景只重画天空与云 —— 楼是并排的，洞后面不会有别的楼。
        i = 0;
        while (i < holeN)
        {
            onBldg = 0;
            k = 0;
            while (k < gBldgN)
            {
                if ((*BL[k]).Covers(holeX[i]))
                {
                    if (holeY[i] >= (*BL[k]).RoofY()) { onBldg = 1; }
                }
                k = k + 1;
            }
            if (onBldg != 0 && holeY[i] < gy)
            {
                ui_mask_begin();
                ui_circle(holeX[i], holeY[i], holeR[i], 0xFFFFFFFF, 1, 0);
                ui_mask_end(1);
                sky.Draw();
                cloudsDraw(sw, gy);
                ui_mask_clear();
                // 洞口断面：内壁压一圈暗色 ⇒ 有"墙厚"，不然还是像贴纸。
                // ⚠ **要细、要淡** —— 先前是 width 3、60% 黑，在 r=16 的洞上几乎把洞填满，
                //   读出来是"一团黑"而不是"透过去看见了天空"。现在是 2px、35% 黑。
                ui_circle(holeX[i], holeY[i], holeR[i] + 1, 0x59000000, 0, 2);
                // 外缘受光一线（左上那半圈），破口才有"翻起来的边"的观感
                ui_circle(holeX[i] - 1, holeY[i] - 1, holeR[i], 0x40FFFFFF, 0, 1);
            }
            i = i + 1;
        }

        holesDraw(gy, sh);          // 地上的坑（挖土）

        while (i < actorN)
        {
            actors[i]->Draw();
            i = i + 1;
        }

        bird.Draw();
        ufo.Draw();
        plane.Draw();
        // met.Draw();

        // 地面：白天是灰亮的街面，夜里压暗（与楼房同一套天光口径）
        ui_rect(0, gy, sw, sh - gy, mixcol(12, 12, 22, 74, 78, 86, gDayL), 1, 0, 0);
        // 街灯：**地面之后**画（灯杆是立在地上的，画在地面前会被地面啃掉一截）
        lampsDraw(sw, gy, sh);

        // ── 树：**画在所有楼之后** ────────────────────────────────────
        // ⚠ 原来画在楼**之前**，注释写的是"楼房后画会把树挡住，正好得到树长在楼缝里的观感"。
        //   但楼缝只有 `gap = sw/40`（十几像素），树基本全被挡掉 —— 玩家看不到地上有树，
        //   于是又提了一次「地上还有随机的树，数量大小，位置随机」。
        //   街上种的树本来就该**站在楼前面**（也遮住楼脚，画面更有层次），与 BASIC 版一致。
        i = 0;
        while (i < treeN)
        {
            TREES[i].Draw();
            i = i + 1;
        }

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
