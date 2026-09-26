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
#define CLOCK_MS 50       // 游戏时钟的推进周期 —— **不是** 1000，见 `gMsOfDay` 的说明
#define SIN_SCALE 1000    // 正弦表的值域：1000 = 1.0

#define MAX_TRAIL 96      // 尾迹点数上限

// 先拿满这么多分的人赢
#define WIN_SCORE 5

// 回合状态
#define ST_AIM   0
#define ST_FLY   1
#define ST_BOOM  2        // 爆炸（短促的一下，见 `BOOM_TICKS`）

/// 爆炸持续几拍（一拍 = `TICK_MS` = 33ms ⇒ 12 拍 ≈ 0.4 秒）。
///
/// ⚠ **它同时决定火球能长多大**（`r = 6 + boomT * 3`）—— 缩时间就是缩大小，
///   这两件事本来就是同一个数。别在绘制那边另写一个上限：那会出现
///   "时间到了球还在长"或者"球长满了还停着"这类对不上的中间态。
/// ⚠ 原来 24（≈0.8 秒、最大直径 176px），玩家报「圆圈有点大、时间有点长」；
///   减半到 12 ⇒ 0.4 秒、最大直径约 104px。外圈多出来的那 10px 与碎屑的 14px
///   也一并减半（`+5` / `+7`）—— 半径减半了、这两个偏移不减，外圈占的比例就会变形。
#define BOOM_TICKS  12

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

/// **亚度**的 sin：`mdeg` 是**千分之一度**（毫度）。
///
/// ⚠ 为什么需要它：`isin` 的粒度是 **1 度**，而日月是"跟时间连续走"的 ——
///   弧顶附近 1 度 ≈ 4px 的纵向位移，于是太阳会**每隔几分钟往上弹一下**
///   （玩家报的"太阳能不能平滑移动"就是这个）。
///   粒度的锅补不了，只能把角度本身做细。
///
/// 做法是**在相邻两个整数度之间线性插值**（不是重推 Bhaskara）：
/// 一个 1 度区间内 sin 的二阶误差约 `(π/180)²/8 ≈ 0.00015`（相对值），
/// 比 Bhaskara 自身的误差还小 —— 而它**不会溢出**：Bhaskara 把输入放大 1000 倍后
/// 分子 `4·u·1000` 会冲到 3×10⁹ 撞破 32 位，插值版最大只有几千。
int isin_f(int mdeg)
{
    int d0;
    int frac;
    int s0;
    int s1;

    if (mdeg < 0) { mdeg = -mdeg; }
    d0 = mdeg / 1000;
    frac = mdeg - d0 * 1000;
    s0 = isin(d0);
    s1 = isin(d0 + 1);
    return s0 + (s1 - s0) * frac / 1000;
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

// ── 云 / 路灯 / 树的**图块**（开窗时录、每帧贴一次）──────────────────────
//
// ⚠ 它们的颜色**随昼夜连续变**，而图块里的颜色是**录制那一刻定死的** ⇒ 按天光
//   **分档录**（`VML_DAY_STEPS` 档），贴的时候按当前天光选最近的一档。
//   24 真实分钟走完一天 ⇒ 每档约 1.5 分钟，档位之间的颜色差看不出来。
// ⚠ 声明必须放在**最前面**：这个前端不做前向引用，`lampsDraw` 在 442 行就用到了。
#define VML_DAY_STEPS 16
static int cloudBid[VML_DAY_STEPS];
static int lampBid[VML_DAY_STEPS];
static int treeBid[3];           // 树的绿色**本来就是三档固定色** ⇒ 不需要分昼夜

#define MAX_HOLE 24

static int holeX[MAX_HOLE];
static int holeY[MAX_HOLE];
static int holeR[MAX_HOLE];
static int holeN;

// 画蒙版用的**临时**洞表：钳制 + 合并之后的圆（见 `Game::Draw` 里那段说明）。
static int mhx[MAX_HOLE];
static int mhy[MAX_HOLE];
static int mhr[MAX_HOLE];

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

/// 举起那只胳膊的长度（肩 → 手，像素）。
/// **画手臂与算香蕉出手点共用这一个数** —— 见 `Ape::ArmRootX` 上面的说明。
///
/// ⚠ 这个数试过三档：26 太短（手掌落在头的边缘，读成"手捂在脸上"）、
///   32 太长（手臂比躯干还长一大截，玩家报"太长，只要一半"）、**16** 正好 ——
///   手臂加手掌 ≈ 23px，与躯干高（23）相当。肩点因此也往外挪了一格（13→14），
///   否则手掌离脸只剩 5px 又要贴上去。
#define ARM_LEN     16
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
/// 当天已过的**毫秒**（游戏时间）—— **平滑动画的唯一真源**。
///
/// ⚠ 为什么不能只靠 `gHour/gMinute`：它们的分辨率是**一分钟**，而 1 真实秒 = 1 游戏分钟
///   ⇒ 任何"按分钟算位置"的东西都是**每秒跳一次**：
///   · 云的速度是 2~4 px/分钟 ⇒ 每秒瞬移 2~4px（玩家报的"云层不平滑"就是这个）；
///   · 日月的角度分辨率是 1 度 ⇒ 弧顶附近 1 度 ≈ 4px 的纵向位移，太阳会"每几分钟弹一下"。
///   **位置要连续，时间本身就得是连续的。**
///
/// ⚠ `gHour/gMinute` 降级为**显示值**（HUD 上的时钟、流星的分钟判据、暖色窗口），
///   由 `clockAdvance` 从 `gMsOfDay` 推出来。**别在别处直接累加分钟** ——
///   两个真源一旦不同步，就是"钟面上的时间和天色对不上"这类最难查的分叉。
///
/// ⚠ 初值在 `main` 里由 `gHour/gMinute` 算出来（**不在这里写死** —— 写死就是
///   同一个"开局 7:30"写两遍，改一处忘一处）。
static int gMsOfDay;
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

// 游戏时钟走 `ms` 毫秒（调用方按定时器周期给；见 `CLOCK_MS`）
void clockAdvance(int ms)
{
    gMsOfDay = gMsOfDay + ms;
    if (gMsOfDay >= 86400000) { gMsOfDay = gMsOfDay - 86400000; }
    gMinute = (gMsOfDay / 60000) % 60;
    gHour = (gMsOfDay / 3600000) % 24;
}

// 算这一刻的天光 / 暖色 / 日月位置 —— **每帧调一次**（游戏时间可能刚跳过一格）
void clockCompute(int scrW, int groundY, int hudH)
{
    int gmins;
    int tpvM;            // 千分之一 tpv（0..10000）—— **位置与角度都按它算**，见下
    int mm;
    int mmMs;
    int tp2M;
    int win;
    int arcX0;
    int arcW;
    int arcBot;
    int arcH;

    gmins = gHour * 60 + gMinute;

    // ── 太阳：6:00 升、12:00 顶、18:00 落 ──────────────────────────────
    //
    // ⚠ 位置一律由 `gMsOfDay`（毫秒）推，**不用 `gmins`**：
    //   `gmins` 的粒度是一分钟 ⇒ 太阳每秒才动一次、每次移动接近 1px，
    //   在屏幕上就是"一顿一顿地走"。毫秒版每帧都在动，而 `tpvM` 的分辨率
    //   （4.3 秒 / 步）换算到屏幕上是 0.04px/步 —— 远在肉眼之下。
    //   `gmins` 只留给"升没升起来"这种**开关量**判据（它是分钟语义的）。
    gSunUp = 0;
    tpvM = 0;
    if (gmins >= 360)
    {
        if (gmins <= 1080)
        {
            gSunUp = 1;
            // 6:00 = 21600000ms；12 小时 = 43200000ms ⇒ 每 4320ms 一格，共 10000 格
            tpvM = (gMsOfDay - 21600000) / 4320;      // 0..10000
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
        // ⚠ 用 `isin_f`（毫度）：亮度每秒跳一格会让朝霞的过渡出现台阶
        //   （按整数度算的话，1 度 ≈ 一整分钟的天光变化，天亮的边沿是锯齿状的）。
        gDayL = isin_f(tpvM * 18) * 14 / 100;         // tpvM×18 = 毫度（0..180000）
        if (gDayL > 100) { gDayL = 100; }
    }

    // 日月走**同一条弧**（月亮把时钟拨 12 小时）
    arcX0 = 40;
    arcW = scrW - 80;
    arcBot = groundY - 46;
    arcH = arcBot - hudH - 46;
    if (arcH < 40) { arcH = 40; }
    gSunX = arcX0 + arcW * tpvM / 10000;
    gSunY = arcBot - arcH * isin_f(tpvM * 18) / 1000;

    // 月亮：同样走毫秒（`mm` 那套是分钟语义的，这里只借它判"在不在天上"）
    mmMs = gMsOfDay + 43200000;
    if (mmMs >= 86400000) { mmMs = mmMs - 86400000; }
    mm = mmMs / 60000;
    gMoonUp = 0;
    tp2M = 0;
    if (mm >= 360)
    {
        if (mm <= 1080)
        {
            gMoonUp = 1;
            tp2M = (mmMs - 21600000) / 4320;
        }
    }
    gMoonX = arcX0 + arcW * tp2M / 10000;
    gMoonY = arcBot - arcH * isin_f(tp2M * 18) / 1000;

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

/// 一盏灯的**标准尺寸**（录图块用；灯是固定尺寸，贴的时候不用缩放）。
/// 内容范围：x ∈ [lx-1, lx+18]、y ∈ [ly-46, ly] ⇒ 19×46。
#define LAMP_STD_W 19
#define LAMP_STD_H 46

/// 一盏路灯的局部形状（`lx` = 灯柱中轴、`ly` = 地面）。
/// 抽出来是为了录图块 —— 杆和灯头是**两套独立的昼夜插值**，所以要按天光分档录。
void lampShapeAt(int lx, int ly, int dayL)
{
    int poleC;
    int headC;
    // 杆：白天是深灰（有体积感），夜里更深（背光）
    poleC = mixcol(28, 30, 36, 74, 78, 88, dayL);
    // 灯头：白天是玻璃（暗），夜里是暖黄 —— 夜里越黑越亮
    headC = mixcol(255, 214, 130, 96, 102, 116, dayL);
    ui_rect(lx - 1, ly - 46, 3, 46, poleC, 1, 0, 0);      // 杆
    ui_rect(lx - 1, ly - 46, 14, 3, poleC, 1, 0, 0);      // 横臂
    ui_rect(lx + 8, ly - 44, 10, 5, headC, 1, 0, 0);      // 灯头（挂在横臂末端）
}

void lampsDraw(int scrW, int groundY, int scrH)
{
    int i;
    int lx;
    int ly;
    int slot;

    slot = gDayL * VML_DAY_STEPS / 101;
    if (slot < 0) { slot = 0; }
    if (slot >= VML_DAY_STEPS) { slot = VML_DAY_STEPS - 1; }

    i = 0;
    while (i < N_LAMP)
    {
        lx = scrW * (i * 2 + 1) / (N_LAMP * 2);     // 均分：1/6、3/6、5/6 处
        ly = groundY;
        if (lampBid[slot] > 0)
        {
            // 图块中心相对"灯柱底"偏 (8.5, -23)（见录制处的局部坐标）——
            // ⚠ 灯是固定尺寸，**不用缩放**，所以这个偏移是常数，直接加到坐标上即可
            ui_draw_block(lampBid[slot], lx + 8, ly - 23, 1000, 1000, 0);
        }
        else
        {
            lampShapeAt(lx, ly, gDayL);
        }
        i = i + 1;
    }
}

// ════════════════════════════════════════════════════════════════════
// 流星 —— 夜里偶尔划过一颗
//
// 拆成两件事看：
//   · **节拍**（第几分钟会有）仍由**游戏分钟**推出来 —— 所以"为什么现在没流星"
//     一句话能答上来（现在是第 4 分钟，不在周期前 3 分钟里）；
//   · **这一颗长什么样**（起点在哪、尾多长、甚至这一颗来不来）由**随机数**定。
// 玩家要的"流星的位置随机"就是后半件：位置固定 ⇒ 每次都在同一个地方划过，
// 看两回就腻了。
//
// ⚠⚠ 随机数**每颗只掷一次**，判据是"周期序号变了没有"（`cid`）。
//   本函数**每帧都调** —— 每帧掷一次的话，流星会变成满屏乱跳的一条线
//   （位置每帧不同，读出来是"闪烁的斜线"而不是"划过"）。
//   这与云"按分钟算位置"是同一条理由：**跟时间走的东西不要每帧重新决定**。
//
// ⚠ 它和云一样，是**按时间算位置**而不是每帧累加 —— 累加会随帧率变快慢。
// ════════════════════════════════════════════════════════════════════

static int metCycle = -1;        // 当前这颗的周期序号（-1 = 还没掷过）
static int metSkip;              // 1 = 这一颗不出现
static int metOffX;              // 起点横向偏移
static int metOffY;              // 起点纵向偏移
static int metTail;              // 尾长（屏宽的百分之几）

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
    int cid;

    // 每 7 游戏分钟来一颗；只有夜里（天光低）才看得见
    if (gDayL > 35) { return; }
    cycle = (gHour * 60 + gMinute) % 7;
    if (cycle >= 3) { return; }                  // 只用周期里的前 3 分钟
    phase = cycle * 100 / 3;                     // 0..100

    // ── 这一颗的形状：**每颗只掷一次**（见文件头那条说明）──────────────
    cid = (gHour * 60 + gMinute) / 7;
    if (cid != metCycle)
    {
        metCycle = cid;
        metSkip = 0;
        if (ui_rand(4) == 0) { metSkip = 1; }              // 1/4 的夜里干脆不来
        metOffX = ui_rand(scrW / 3) - scrW / 6;            // 起点左右晃 ±1/6 屏宽
        metOffY = ui_rand(groundY / 4) - groundY / 8;      // 起点上下晃 ±1/8 地平线高
        metTail = 5 + ui_rand(7);                          // 尾长 5..11
    }
    if (metSkip != 0) { return; }

    mx = scrW * 70 / 100 - scrW * phase / 100 + metOffX;   // 从右上往左下
    // ⚠ `my` 的纵向跨度（55%）改了，下面尾迹的 `tailY` 必须**跟着改同一个系数** ——
    //   尾迹靠"取位移的一个固定比例"来与轨迹同向，两处系数一旦不同源，
    //   尾巴就又不按流线方向跑了（这正是玩家上一轮挑出来的毛病）。
    my = groundY * 25 / 100 + groundY * phase * 55 / 10000 + metOffY;
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
    tailX = scrW * metTail / 100;
    tailY = groundY * metTail * 55 / 10000;
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

// ── 云 / 路灯 / 树的**图块**（开窗时录、每帧贴一次）──────────────────────
//
// ⚠ 它们的颜色**随昼夜连续变**，而图块里的颜色是**录制那一刻定死的** ⇒ 按天光
//   **分档录**（`VML_DAY_STEPS` 档），贴的时候按当前天光选最近的一档。
//   24 真实分钟走完一天 ⇒ 每档约 1.5 分钟，档位之间的颜色差看不出来。
#define VML_DAY_STEPS 16

/// 楼的**窗户**按昼夜分几档录（开窗时录、每帧贴一次）。
///
/// ⚠⚠ **不能跟着 `VML_DAY_STEPS`（16）走** —— 块表有 `MaxBlocks = 128` 的上限，
///   而 6 栋楼 × 16 档 = 96 张，加上精灵 / 云 / 路灯 / 树的块一共 136 张，**超了**。
///   超限的后果**不是"少录几张"**：`ui_create_block` 返回 0 **且不进入录制态**，
///   此时若照常画窗户，那些矩形会**直接落到画布上** —— 实测现象就是
///   **一片窗户飘在天上**（录制用的是局部坐标 (0,0)，也就是画布原点）。
///   8 档 × 6 栋 = 48 张，连原有的 40 张一共 88，留足余量。
///   档位差（天光 12.5）下的台阶看不出来 —— 窗户本来只有三种色。
///
/// ⚠ **为什么是"整栋一张 w×h 的大块"而不是"一窗一块"**（v0.96.477 试过又改回来）：
///   一窗一块在结构上更稳（块的尺寸固定 11×14、位置每扇自负，不会整批偏），
///   但**性能差 21%**：真机实测整栋一块每帧只发 6 次贴图调用，一窗一块要发 84 次
///   （5~6 栋 × 几十扇）。而"程序→宿主"的往返正是当初录块要省掉的那笔开销 ——
///   一窗一块等于把它还回去 14 倍（23.8fps → 18.7fps，`gfxinfo` 里
///   `High input latency` 4682、中位帧时间 36ms，用户感受到的就是**触摸明显变迟钝**）。
///
///   "整批偏"那个风险改用**贴前校验**挡住（见 `DrawBody`）：块尺寸与楼体对不上就
///   **不贴**、退回逐扇画 —— 于是"拿错尺寸的块硬贴"从结构上不可能发生，
///   不必靠"一窗一块"来换。
///
/// ⚠ 块是**每局重录**的（楼基色每局都变，`SetTint` 在 `Layout` 里调）⇒
///   重录前**必须先 `ui_free_block` 释放上一局的**，否则每局漏 48 个句柄、三局就撑满 128。
#define WIN_DAY_STEPS 8

/* 一扇窗的图块尺寸（含边框）—— **固定值**，与楼体宽高无关。
   这正是"一窗一块"相对"整栋一张大块"的好处：位置由每扇窗自己算，
   不会因为块尺寸与楼体有一点点对不上而**整栋一起偏**。 */
#define WIN_W 11
#define WIN_H 14

static int cloudBid[VML_DAY_STEPS];
static int lampBid[VML_DAY_STEPS];
static int treeBid[3];           // 树的绿色**本来就是三档固定色** ⇒ 不需要分昼夜
static int cloudSpeed[N_CLOUD];  // 第 i 朵的飘动速度（**像素 / 秒**，见 `cloudX`）
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
        // 宽度 40..75（玩家报「云层有点偏大」）——
        // ⚠ 原来是 60..129，最宽那朵占屏宽的三分之一，天上全是云、楼都不显了。
        //   云是**背景的装饰**，它一大就抢戏；这里按"比一栋楼窄一点"来定
        //   （楼宽 ≈ 可用宽 / 栋数 ≈ 300/6 ≈ 50）⇒ 取 40~75 正好压在这个量级上。
        cloudW[i] = 40 + (i * 23) % 36;
        cloudSpeed[i] = 2 + (i % 3);
        i = i + 1;
    }
    cloudsReady = 1;
}

/// 第 i 朵云现在的左边 x（按**毫秒**算，出画就从另一头绕回来）。
///
/// ⚠ 时间用 `gMsOfDay` 而不是 `gHour*60+gMinute`（玩家报的"云层不平滑"就是这个）：
///   按分钟算 ⇒ 位置**每秒跳一次**，而 `cloudSpeed` 是 2~4px/分钟 ⇒
///   云每秒瞬移 2~4 像素，看着就是一顿一顿的。
///   换成毫秒之后同一段位置函数变成**每帧都在动**，每帧只动零点几像素 —— 连续了。
/// ⚠ `cloudSpeed` 的语义因此从"像素/游戏分钟"变成**"像素/秒"**（数值不变：
///   2~4 px/s 正是原来的观感速度，改的只是"跳一次"变成"连续走"）。
///   **别在这里再做 `*60` 之类的换算** —— 那会让云快 60 倍。
int cloudX(int idx, int scrW)
{
    int span;
    int pos;
    span = scrW + 160;                       // 多留 160，让云整个出画再回来
    pos = cloudSpeed[idx] * gMsOfDay / 1000 + idx * 97;
    pos = pos % span;
    return pos - 120;
}

// 一朵云 = 三团椭圆叠出来（便宜，形状够用）
/// 云的**标准尺寸**（录图块用）—— 贴的时候按实际宽度等比缩放。
#define CLOUD_STD_W 128
/// 云图块的高：云心**上方** 2×ch（最大的鼓包顶到那儿）、**下方** ch/2 ⇒ `2.5×ch`。
/// ⚠ 云心因此在图块里偏下（从顶算 `2×ch`），贴的时候要减掉那 31.5 个标准像素的偏移。
#define CLOUD_STD_H (CLOUD_STD_W * 5 / 6)

/// 一朵云（局部形状：`cx,cy` 是云心、`cw` 是宽度、`ch = cw/3`）。
///
/// ⚠ 抽成函数是**为了录图块**：形状写一遍，录的时候调它、画的时候贴。
///   `dayL` 是**绘制时**的天光 —— 录图块时传档位值，正常绘制时传 `gDayL`。
///   天空色（`gMr/gMg/gMb`）由调用方先 `clockPartsAt` 设好。
void cloudShapeAt(int cx, int cy, int cw, int dayL)
{
    int ch;
    int col;
    int shade;
    ch = cw / 3;
    // 云色 = 当地天空色 → 白，按天光插值 ⇒ 天全黑时云正好融进天空。
    // ⚠ 系数要**够狠**：天光 52（早上 7:30）时若只插到 44%，云就是 `0xFF8094B3`
    //   这种灰蓝，比天空亮一点点、**肉眼看不出来**（实测过）。
    //   乘 1.6、封顶 100 ⇒ 天光 63 以上就是纯白的云，日出前后才淡出。
    col = mixcol(gMr, gMg, gMb, 255, 255, 255, dayL * 160 / 100);
    if (dayL * 160 / 100 > 100) { col = mixcol(gMr, gMg, gMb, 255, 255, 255, 100); }
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
}

void cloudsDraw(int scrW, int groundY)
{
    int i;
    int cx;
    int cy;
    int cw;
    int slot;

    if (cloudsReady == 0) { return; }

    // 按当前天光选一档图块（24 分钟走完一天 ⇒ 每档 ~1.5 分钟，档间差看不出来）
    slot = gDayL * VML_DAY_STEPS / 101;
    if (slot < 0) { slot = 0; }
    if (slot >= VML_DAY_STEPS) { slot = VML_DAY_STEPS - 1; }

    i = 0;
    while (i < N_CLOUD)
    {
        cx = cloudX(i, scrW);
        cy = cloudY[i];
        cw = cloudW[i];
        if (cloudBid[slot] > 0)
        {
            // ⚠ **等比缩放**：云的高宽比本来就是定的（ch = cw/3），非等比会把鼓包压扁。
            // ⚠ **要减一个偏移**：图块的内容是"往上鼓"的（云心下方只有 ch/2、上方有
            //   2×ch），所以图块的**几何中心不等于云心** —— 差 31.5 个标准像素（见录制处）。
            //   不减这一步，云会整体往下沉一截。
            ui_draw_block(cloudBid[slot], cx, cy - (32 * cw / CLOUD_STD_W),
                          cw * 1000 / CLOUD_STD_W, cw * 1000 / CLOUD_STD_W, 0);
        }
        else
        {
            // 图块没建起来（有真窗口时不该发生）—— 退回直接画
            clockPartsAt(cy, groundY);
            cloudShapeAt(cx, cy, cw, gDayL);
        }
        i = i + 1;
    }
}



// ════════════════════════════════════════════════════════════════════
// 音效：一个小音序器
// ════════════════════════════════════════════════════════════════════
//
// 为什么要它，而不是在事件点上直接 `ui_tone_on/off`：
//
//   ① **`ui_tone_on` 没有时长参数** —— 响多久全看自己什么时候 `note_off`。
//      在事件点 on、忘了 off，声部就只涨不落（上限 32，满了以后新音**全哑**，
//      而且**一声不响地哑**）。表里记着"还差几拍关"，就不会漏。
//   ② 好听的音效往往是**几个音先后**（"叮—咚"、上行三音），而事件点只有一拍 ——
//      需要"过几拍再响下一个"。
//   ③ 同一个**通道**上后一个音会掐掉前一个（这正是 `ui_beep` 的老语义）。
//      所以"同时响"必须落在**不同通道**上 —— 得有一处统一分配，否则两处音效
//      撞到同一个通道就是"少响了一个，还看不出为什么"。
//
// 三条合起来 = 一张小表 + 每拍推进一次。
//
// ⚠ **通道分配是这张表的契约**（分区互不重叠；同分区内的音效可以互相覆盖，
//   那是有意的：新事件就该盖过旧事件）：
//     0–2   地面爆炸（三音轰鸣）
//     3–5   命中得分的和弦
//     6–7   发射
//     8–9   空中爆炸
//     10–12 警报（飞碟低吼）/ 激光
//     13–15 胜负
//
// ⚠ **两个"可能同时发生"的音效绝不能共用通道**。实测踩过：探针里让发射音与
//   空中爆炸音同拍响（它们本该相隔一个飞行过程，那次是人为凑到一起的），
//   后者把前者的槽顶掉 —— 表现是**发射音一声没有**，而日志上只是"少了两行"，
//   很容易当成"没做"。分区表就是按"谁与谁可能同拍"划的：发射与空中爆炸分开了，
//   低吼与激光分开用各自的（它们先后发生，共用没问题，但仍分开更省心）。
//   `ui_beep` 走宿主那条**专用声道**，与本表**互不干扰** —— 所以哪怕将来
//   还有地方在用老音效，它也不会吃掉和弦。
#define SFX_SLOTS 16

// 波形（与 `VML_WAVE_*` 同值，这里再写一遍是为了让下面的音色表读起来不用跳出去查）
#define SFX_SINE     0
#define SFX_SQUARE   1
#define SFX_SAW      2
#define SFX_TRIANGLE 3

static int sfxCh[SFX_SLOTS];
static int sfxNote[SFX_SLOTS];
static int sfxDel[SFX_SLOTS];      // 还差几拍开响（0 = 落到这一拍就响）
static int sfxDur[SFX_SLOTS];      // 响几拍
static int sfxVel[SFX_SLOTS];
static int sfxWave[SFX_SLOTS];
static int sfxOn[SFX_SLOTS];       // 1 = 已经 note_on、还等着 note_off

/// 清表（**不发声**）。⚠ 只清表不关音 = 已经在响的那些**从此没人管**，
/// 所以要"静音"请走 `SfxPanic()`。
void SfxReset()
{
    int i;
    i = 0;
    while (i < SFX_SLOTS)
    {
        sfxCh[i] = -1;
        sfxNote[i] = -1;
        sfxDel[i] = 0;
        sfxDur[i] = 0;
        sfxVel[i] = 0;
        sfxWave[i] = -1;
        sfxOn[i] = 0;
        i = i + 1;
    }
}

/// 立刻静音。退出、重开一局时用。
/// ⚠ **顺序不能反**：先清表就丢掉了"哪些通道在响"，那些声部会一直响到程序结束。
void SfxPanic()
{
    int i;
    i = 0;
    while (i < SFX_SLOTS)
    {
        if (sfxOn[i] != 0) { ui_tone_off(sfxCh[i], sfxNote[i]); }
        i = i + 1;
    }
    ui_tone_panic();          // 兜底：表外的（老音效那条声道）也一并停
    SfxReset();
}

/// 往表里塞一个音（`del` 拍之后开始响、响 `dur` 拍）。
///
/// ⚠ 会**先接管同通道的旧槽**，而且在接管之前**先把那个音关掉**：
///   不然旧槽连同"它还在响"这件事一起被丢掉，那个声部就再也没人去关它了。
void SfxAdd(int ch, int note, int del, int dur, int vel, int wave)
{
    int i;
    int slot;
    slot = -1;
    i = 0;
    while (i < SFX_SLOTS)
    {
        if (sfxCh[i] == ch)
        {
            if (sfxOn[i] != 0) { ui_tone_off(sfxCh[i], sfxNote[i]); }
            slot = i;
        }
        i = i + 1;
    }
    if (slot < 0)
    {
        i = 0;
        while (i < SFX_SLOTS)
        {
            if (sfxCh[i] < 0) { slot = i; }
            i = i + 1;
        }
    }
    if (slot < 0) { return; }        // 表满：宁可少一个音，也不要越界
    sfxCh[slot] = ch;
    sfxNote[slot] = note;
    sfxDel[slot] = del;
    sfxDur[slot] = dur;
    sfxVel[slot] = vel;
    sfxWave[slot] = wave;
    sfxOn[slot] = 0;
}

/// 一拍推进。**由主循环按真实流逝时间调**（不是物理节拍 —— 见那里的说明：
/// 游戏结束后物理定时器会被杀掉，而胜负音还得接着放完）。
void SfxTick()
{
    int i;
    i = 0;
    while (i < SFX_SLOTS)
    {
        if (sfxCh[i] >= 0)
        {
            if (sfxOn[i] == 0)
            {
                if (sfxDel[i] > 0) { sfxDel[i] = sfxDel[i] - 1; }
                else
                {
                    ui_tone_wave(sfxCh[i], sfxWave[i]);
                    ui_tone_on(sfxCh[i], sfxNote[i], sfxVel[i]);
                    sfxOn[i] = 1;
                }
            }
            else
            {
                sfxDur[i] = sfxDur[i] - 1;
                if (sfxDur[i] <= 0)
                {
                    ui_tone_off(sfxCh[i], sfxNote[i]);
                    sfxCh[i] = -1;
                    sfxNote[i] = -1;
                    sfxWave[i] = -1;
                    sfxOn[i] = 0;
                }
            }
        }
        i = i + 1;
    }
}

// ── 音色表 ──────────────────────────────────────────────────────────
//
// 音符号是**真 MIDI 语义**（中央 C = 60、A4 = 69 = 440Hz）。
//
// ⚠ **低音别写太低**：手机的外放小喇叭在 200Hz 以下衰减很快，写 36（C2=65Hz）
//   出来是"噗"的一声闷响，玩家听着像**没响**而不是"低沉"。所以轰鸣的**基音
//   落在 48（C3=130Hz）上下**，低八度只当"配重"垫一层（三角波、谐波少）。
//   要判断"够不够响"只能上真机听 —— 桌面（耳机/音箱）听得到不代表手机听得到。

/// 发射：短促的一记「嗖」（两个音快速下行 = 有方向感）。
void SfxFire()
{
    SfxAdd(6, 77, 0, 2, 70, SFX_TRIANGLE);
    SfxAdd(7, 72, 1, 2, 55, SFX_TRIANGLE);
}

/// 命中得分：**大三和弦上行**（do–mi–sol）。
/// 得分是这一局里重复最多的正反馈，就该是最好听的那个 —— 一次只响一个音
/// 的话，打十次听十遍"哔"，赢也听不出高兴。
void SfxHit()
{
    SfxAdd(3, 72, 0, 4, 95, SFX_SQUARE);
    SfxAdd(4, 76, 1, 4, 85, SFX_SQUARE);
    SfxAdd(5, 79, 2, 6, 85, SFX_SQUARE);
}

/// 空中爆炸（打到飞行物）：高音一「叮」+ 低音垫底，**不用轰鸣** ——
/// 那是"打爆了一个小东西"，与撞楼的份量不一样，听着就该不一样。
void SfxAirBoom()
{
    SfxAdd(8, 84, 0, 2, 85, SFX_SQUARE);
    SfxAdd(9, 55, 0, 3, 70, SFX_SAW);
}

/// 撞楼 / 落地：「轰」。
/// 靠**三个不谐和的低音叠在一起**做出粗粝感（48 与 54 是三全音，最"脏"的音程），
/// 再垫一个低八度当配重。锯齿波谐波丰富，比方波更像爆破。
void SfxGroundBoom()
{
    SfxAdd(0, 48, 0, 6, 100, SFX_SAW);
    SfxAdd(1, 54, 0, 5, 75, SFX_SAW);
    SfxAdd(2, 36, 0, 7, 85, SFX_TRIANGLE);
}

/// 飞碟被惹毛：**下行警报**（三个音，锯齿 = 有攻击性）。
/// 事件点上响一个音是"哔"，下行三音才是"我盯上你了"。
void SfxRage()
{
    SfxAdd(10, 72, 0, 3, 90, SFX_SAW);
    SfxAdd(11, 67, 3, 3, 90, SFX_SAW);
    SfxAdd(12, 60, 6, 8, 95, SFX_SAW);
}

/// 被飞碟清场（这一局**与分数无关地**结束）：低沉的长音慢慢往下沉。
void SfxLaser()
{
    SfxAdd(10, 55, 0, 6, 100, SFX_SAW);
    SfxAdd(11, 48, 5, 8, 95, SFX_SAW);
    SfxAdd(12, 36, 10, 14, 90, SFX_TRIANGLE);
}

/// 获胜：**上行大三和弦 + 高八度收尾**，明亮。
void SfxWin()
{
    SfxAdd(13, 72, 0, 4, 95, SFX_SQUARE);
    SfxAdd(14, 79, 2, 5, 90, SFX_SQUARE);
    SfxAdd(15, 84, 5, 12, 90, SFX_SQUARE);
}

/// 输：**小二度下行**（G–F#，最不谐和的音程之一）再拖一个低音。
/// ⚠ 输赢的音必须**不看屏幕也分得出** —— 合成音是单通道的那个年代只能用音高
///   表达情绪，这规矩在复音时代同样成立：两者都用"上行三音"的话，玩家只知道
///   "响了个东西"，还得抬头看横幅才知道自己是输是赢。
void SfxLose()
{
    SfxAdd(13, 67, 0, 4, 90, SFX_SAW);
    SfxAdd(14, 66, 4, 10, 90, SFX_SAW);
    SfxAdd(15, 42, 4, 10, 85, SFX_TRIANGLE);
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
    // 窗户图块：**整栋楼一张**（尺寸 = 楼体的 w×h），按昼夜分档。
    //
    // `winBW`/`winBH` 记的是**录这张块时的楼体尺寸** —— 贴之前拿它和当前楼体比，
    // 对不上就**不贴、退回逐扇画**（见 `DrawBody`）。这是"整栋一起偏出去"那道防线：
    // 不靠"一窗一块"来换结构安全，而是让"拿错尺寸的块硬贴"这件事根本无法发生。
    int winBid[WIN_DAY_STEPS];
    int winBW[WIN_DAY_STEPS];
    int winBH[WIN_DAY_STEPS];

    Building()
    {
        int s;
        w = 0;
        h = 0;
        gy = 0;
        tint = 0;
        baseR = 224; baseG = 96; baseB = 78;
        s = 0;
        while (s < WIN_DAY_STEPS)
        {
            winBid[s] = 0; winBW[s] = 0; winBH[s] = 0;
            s = s + 1;
        }
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
        DrawRoof();
        DrawBody();
    }

    /// <summary>
    /// 屋顶细节（水箱 / 天线）。
    ///
    /// ⚠ **必须在挖洞蒙版之外画**：它们的包围盒伸到楼体矩形**上方**
    /// （水箱到 `y-15`、天线到 `y-22`），而那个蒙版就是"楼体矩形 − 洞" ——
    /// 放进去会被整块裁掉。先画它、后画楼体没有差别：两者本来就不重叠
    /// （细节全在 `y` 以上，洞全在 `y` 以下 —— 判据见 `HoleOnBldg`）。
    /// </summary>
    void DrawRoof()
    {
        int frmC = mixcol(baseR, baseG, baseB, 0, 0, 0, 45);

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

    /// <summary>
    /// 楼体（体 / 受光面 / 压暗边 / 檐口 / 窗 / 门）。
    ///
    /// ⚠ **这一部分整个落在楼体矩形 `(x, y, w, h)` 内**，所以能直接套
    /// "楼体矩形 − 洞"的蒙版 —— 被炸开的地方直接透出背景层（云、日月都是活的）。
    /// 加新的装饰时要问一句"它还在这个矩形里吗"，不在就得挪到 `DrawRoof` 去。
    /// </summary>
    /// <summary>
    /// 画本栋的**全部窗户**。`(ox, oy)` 是**楼体左上角**在目标坐标系里的位置：
    /// 正常绘制传 `(x, y)`；录图块传 `(0, 0)`（块的局部坐标）。
    ///
    /// ⚠ 判据必须只用**相对楼顶**的量（`limit`）与**成员 `x`**（那个 `x / 8` 是
    ///   "哪几扇窗亮着"的图案种子）—— 两者都与 `ox/oy` 无关。
    ///   一旦写成绝对坐标，录进块的那一份就带着**录制时**的位置，贴到别处整片窗户会错位。
    /// </summary>
    /// 窗框色 —— **与门框同源**（门也用它），所以只在这里算一份。
    int FrameColor() { return mixcol(baseR, baseG, baseB, 0, 0, 0, 45); }

    /// 夜里透出来的灯光色（窗里、门缝里都是它）—— 同样只算一份。
    int LitColor() { return mixcol(255, 198, 104, 226, 236, 244, gDayL); }

    /// 画本栋的**全部窗户**（逐扇）。
    ///
    /// ⚠ **录制时走这条**（`MakeWinBlocks` 里 `DrawWindows(0, 0)`）—— 绝不能让录制去贴图块，
    ///   那是拿块录块。而**播放时通常不走这条**：`DrawBody` 直接贴整栋那一张块，
    ///   只有"块没录成"或"块尺寸对不上楼体"时才退到这里。
    void DrawWindows(int ox, int oy)
    {
        int litN;
        int limit;
        int cols;
        int rows;
        int c;
        int r;
        int wx;
        int wy;
        int lit;

        litN = 4 - gDayL * 2 / 100;

        // 门框顶**相对楼顶**的位置（门在 `gy-20 .. gy`）—— 与坐标系无关
        limit = (gy - 20) - y;

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
                wx = ox + 6 + c * 18;
                wy = oy + 10 + r * 24;
                // ⚠ **一楼只开门、不开窗** —— 原来窗是整列均匀铺到底的，最后一行正好
                //   压在门上（门在 `gy-20 .. gy`），玩家看到的就是"窗户和门重合"。
                //   压在门那一段的窗**一格都不画**。
                if (wy + 14 - oy <= limit)
                {
                    // ⚠ 图案种子用**成员 `x`**（楼的绝对位置）而不是 `ox` ——
                    //   这样录块与直接画得到的是同一片"哪几扇亮着"。
                    lit = ((c + r + x / 8) % 5) < litN;
                    DrawOneWindowAt(wx - 1, wy - 1, lit);
                }
                r = r + 1;
            }
            c = c + 1;
        }
    }

    /// <summary>
    /// 把本栋的窗户按**昼夜分档**录成图块（开窗之后、每次 `Layout()` 之后各调一次）。
    ///
    /// ⚠ 为什么值得录：窗户是一帧图元的大头（5~6 栋 × 几十扇 × 2 个矩形 ≈ 400 个，
    ///   占全帧七成）。真机实测**程序侧线程吃满一个核（89% CPU）**，而那一半的开销
    ///   正是"逐条发出绘图调用" —— 录成块之后一栋楼每帧只发**一次** `ui_draw_block`。
    ///   （宿主贴块时会把这些指令重放出来，所以**屏幕上的图元数不变**：
    ///     省掉的是"程序 → 宿主"的往返，不是"宿主 → 屏幕"的绘制。）
    ///
    /// ⚠ 用**局部坐标**录（`DrawWindows(0, 0)`）⇒ 块与楼的**位置**无关；
    ///   但块尺寸是 `w×h`，与楼的**宽高**有关 ⇒ 每局楼宽高都变，**每次 Layout 都要重录**。
    /// ⚠ 录制期间要临时改 `gDayL`（颜色都由它插值），**录完必须恢复** ——
    ///   与云的 `MakeBlocks` 同一条理由（留着档位值会让这一帧的天色不对）。
    /// </summary>
    void MakeWinBlocks()
    {
        int s;
        int save;
        int h0;

        // ── 先释放上一局录的 ────────────────────────────────────────────────
        // ⚠ 楼基色每局都变（`SetTint` 在 `Layout` 里调），而**块的内容与尺寸都不可变**
        //   ⇒ 只能重录。不释放就是每局漏 6 栋 × 4 档 × 2 = 48 个句柄，
        //   **三局就撑满 128**；撑满之后 `ui_create_block` 一律返回 0，画面悄悄退回逐帧画
        //   （慢，且看不出"块没了"）。`ui_free_block` 就是为这种"内容会变"的用法准备的。
        s = 0;
        while (s < WIN_DAY_STEPS)
        {
            if (winBid[s] > 0) { ui_free_block(winBid[s]); winBid[s] = 0; }
            winBW[s] = 0;
            winBH[s] = 0;
            s = s + 1;
        }

        s = 0;
        while (s < WIN_DAY_STEPS)
        {
            save = gDayL;
            gDayL = s * 101 / WIN_DAY_STEPS;

            // ⚠⚠ **建不出来就把句柄留 0、退回逐扇画**。块表满时 `ui_create_block` 返回 0
            //   并且**不进入录制态** —— 此时若照常调 `DrawWindows`，那些窗户会
            //   **直接落到画布上**（局部坐标 (0,0) 就是画布原点），实测现象是
            //   "一片窗户飘在天上"。这条是踩过的坑，别当成多余的防御删掉。
            h0 = ui_create_block(w, h, 0);
            if (h0 == 0)
            {
                winBid[s] = 0;
            }
            else
            {
                DrawWindows(0, 0);
                winBid[s] = ui_end_block();
                // 记下**录这张块时的楼体尺寸** —— `DrawBody` 贴之前拿它跟当前楼体比，
                // 对不上就不贴（那正是"整栋窗户一起偏出去"的入口）。
                winBW[s] = w;
                winBH[s] = h;
            }

            gDayL = save;
            s = s + 1;
        }
    }

    /// 当前天光对应的窗户档位 —— 录制与贴图**共用这一处**换算式
    /// （两处各写一遍就是"同一规则两处实现"，迟早漂）。
    int WinSlot()
    {
        int s;
        s = gDayL * WIN_DAY_STEPS / 101;
        if (s < 0) { s = 0; }
        if (s >= WIN_DAY_STEPS) { s = WIN_DAY_STEPS - 1; }
        return s;
    }

    /// 画**一扇窗**：`(bx, by)` 是**窗框左上角**（世界坐标；录图块时传 (0,0)）。
    ///
    /// ⚠ 录制与"没录成时的逐扇画"**共用这一份** —— 两处各画一遍，
    ///   录出来的块与直接画的必然长得不一样（那种差异只有肉眼能发现）。
    void DrawOneWindowAt(int bx, int by, int lit)
    {
        ui_rect(bx, by, WIN_W, WIN_H, FrameColor(), 1, 0, 0);
        ui_rect(bx + 1, by + 1, WIN_W - 2, WIN_H - 2,
                lit != 0 ? LitColor() : mixcol(14, 14, 24, 52, 74, 96, gDayL), 1, 0, 0);
    }

    void DrawBody()
    {
        int bodyC;
        int hiC;
        int edgeC;
        int slot;

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

        // ── 窗：贴**整栋一张块**（内容由 `DrawWindows(0,0)` 录进 `MakeWinBlocks`）──
        //
        // ⚠⚠ **贴之前必须校验块尺寸 == 当前楼体尺寸**。块是"w×h 的整栋窗户"，
        //   贴的时候靠"块中心 = 楼体中心"对齐 —— 尺寸一旦不一致，**整栋楼的窗户会
        //   一起偏出楼体**（用户报的「窗户铺到房子外面」）。所以对不上就**不贴**、
        //   退回逐扇画：宁可慢，不能让一栋楼的窗户整体挪到别处去。
        //   这道校验取代了"一窗一块"那种结构（后者每扇窗各贴一次、每帧 84 次调用，
        //   真机实测慢 21%：23.8fps → 18.7fps）。
        //
        // ⚠ 没录成（还没有场景 / 本局还没录 / 块表满）时同样退回逐扇画：不能没窗户。
        slot = WinSlot();
        if (winBid[slot] > 0 && winBW[slot] == w && winBH[slot] == h)
        {
            ui_draw_block(winBid[slot], x + w / 2, y + h / 2, 1000, 1000, 0);
        }
        else
        {
            DrawWindows(x, y);
        }

        // 门（贴楼底）。⚠ 被弹坑盖住是对的 —— 坑就是"打没了"。
        // 门框 + 门板 + 底下透出来的一条亮光（屋里有人，门就"活"了）
        ui_rect(x + w / 2 - 9, gy - 20, 18, 20, FrameColor(), 1, 0, 3);
        ui_rect(x + w / 2 - 7, gy - 18, 14, 18, mixcol(baseR, baseG, baseB, 0, 0, 0, 60), 1, 0, 2);
        // 门缝里透出来的一条灯光 —— 比整扇门发亮自然，也不会在白天显得奇怪
        ui_rect(x + w / 2 - 7, gy - 7, 14, 2, LitColor(), 1, 0, 0);
    }
};

/// <summary>
/// 这个洞开在第 `k` 栋楼的**墙上**吗？
///
/// 判据三条，缺一不可：① 横向落在这栋楼的范围内；② 在楼顶**以下**（楼顶以上是屋面，
/// 不是墙）；③ 在地平线**以上**（地面以下的坑是"挖土"，走 `holesDraw` 那条路，
/// 不该从楼里透出天空）。
///
/// ⚠ 抽成函数是因为"墙上的洞"现在**有两个消费者**：挖洞的蒙版（建筑层）与
///   挖洞的蒙版（建筑层）与地上那批坑的分流。各写一遍就是"同一规则两处实现" ——
///   哪天判据一改，必有一处忘了跟。
///   （原先还有第三个消费者"洞口的断面"，那个已经删了 —— 见 `Game::Draw` 里那段说明。）
/// </summary>
static int HoleOnBldg(int hi, int k, int groundY)
{
    if (hi < 0 || hi >= holeN) { return 0; }
    if (holeY[hi] >= groundY) { return 0; }
    if (holeY[hi] < (*BL[k]).RoofY()) { return 0; }
    if (!(*BL[k]).Covers(holeX[hi])) { return 0; }
    return 1;
}

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

/// 把一个洞**钳制**进楼的范围（圆心在楼内、圆整个在 [楼顶, 地面) 里）。
///
/// ⚠ 这是为了让"楼 − 洞"能被**矢量后端折叠成一条路径**。折叠不成立时整个窗口会退回
///   光栅后端 —— 画面立刻变糊、帧率从几十掉到 6（真机实测 6.2fps）。
///   洞一旦探出楼外，"楼外那块洞"在 even-odd 下只穿过一次 ⇒ 被判成**内部**
///   ⇒ 画出一块不该有的肉，所以宿主的折叠判据只能**拒**。
///
/// ⚠ 楼比洞还窄/还矮时先把半径压到"装得下" —— 否则下面两组钳制条件会互相矛盾
///   （左边推右、右边推左），圆照样探出去。
static void ClampHoleToBuilding(int* px, int* py, int* pr,
                                int left, int right, int roofY, int groundY)
{
    int x = *px;
    int y = *py;
    int r = *pr;

    // ⚠ **优先缩半径、保住圆心**。
    //   圆心是"炸在哪"的忠实记录 —— 玩家看得出弹着点，**洞挪了地方比洞小一圈刺眼得多**
    //   （第一版直接钳圆心，玩家的原话是"剪切的洞位置有点怪"）。
    //
    // ⚠⚠ **只钳三个方向：左、右、楼顶。地面那个方向不用管** ——
    //   洞的下缘压到地面以下是**无害**的：地面是**后画**的（楼之后才画那条地面带），
    //   会把多出来的那块盖住。而把它算进半径上限的后果是**灾难性的**：
    //   洞本来就常落在楼的下半部、离地面不远，一算就只剩十几像素，
    //   再叠上"横向也贴边"就成了 3px 的小点 —— 玩家的原话是「**炸不动楼房了**」。
    int hLimit = x - (left + 1);
    if ((right - 1) - x < hLimit) { hLimit = (right - 1) - x; }
    int vLimit = y - (roofY + 1);
    int lim = hLimit < vLimit ? hLimit : vLimit;

    // 半径下限 8：再小就看不出是个洞了（"炸不动"就是这么来的）。
    // 装得下就**只缩半径、圆心一动不动**；实在贴边（连 8 都放不下）才挪圆心。
    if (lim >= 8)
    {
        if (r > lim) { r = lim; }
    }
    else
    {
        r = 8;
        if (x - r < left + 1) { x = left + 1 + r; }
        if (x + r > right - 1) { x = right - 1 - r; }
        if (y - r < roofY + 1) { y = roofY + 1 + r; }
    }
    (void)groundY;   // 地面那个方向**有意不钳**（见上面那段）

    *px = x;
    *py = y;
    *pr = r;
}

/// 两个圆合并成**包含它们的最小圆**（同心的那种包含关系直接取大的）。
///
/// ⚠ 为什么必须合并：折叠"楼 − 洞"要求**洞与洞互不重叠** —— 重叠处在 even-odd 下
///   会被填实（NonZero 也一样，这是"一条路径 + 填充规则"的数学限制，绕不过去）。
///   而观感上合并反而更对：炸得太密，破洞本来就该连成一片。
static void MergeHoles(int ax, int ay, int ar, int bx, int by, int br,
                       int* ox, int* oy, int* orr)
{
    int dx = bx - ax;
    int dy = by - ay;
    int d = isqrt(dx * dx + dy * dy);
    if (d + br <= ar) { *ox = ax; *oy = ay; *orr = ar; return; }   // a 已经把 b 包住了
    if (d + ar <= br) { *ox = bx; *oy = by; *orr = br; return; }   // b 把 a 包住了
    int R = (d + ar + br) / 2;
    if (d == 0) { *ox = ax; *oy = ay; *orr = R; return; }
    int t = R - ar;                       // 新圆心沿 a→b 方向走这么远
    *ox = ax + dx * t / d;
    *oy = ay + dy * t / d;
    *orr = R;
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
    int dead;            // 1 = 被飞碟的激光打死（画成焦黑、不再举手臂）

    Ape()
    {
        x = 0;
        y = 0;
        angle = 45;
        power = 60;
        score = 0;
        flip = 0;
        body = C_APE0;
        dead = 0;
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

    // ── 手臂：肩点 → 手 ────────────────────────────────────────────────
    //
    // ⚠⚠ 这四个函数是**画手臂**与**算出手点**共用的唯一真源。
    //   原来两边各写各的：`Draw` 从躯干中心 `(x, y-24)` 画一条 `len=26` 的斜线，
    //   而 `HandX/HandY` 写死 `x±18, y-20` —— 于是
    //     · 手臂短到**手掌贴在脸上**（手掌到头的距离只有 1.4px）；
    //     · 香蕉从**胸口**飞出去，手上空空。
    //   玩家报的「猴子的手臂外观有点奇怪」就是这两条。
    //   现在改一处两边都对 —— 再出现"手和香蕉不在一起"，先看这里。
    //
    // 肩点取**躯干外缘**（躯干是 `x±14` 的圆角矩形），不是躯干中心：
    // 从中心出发的胳膊读出来是"从胸口斜插出来的一截"。
    int ArmRootX()
    {
        return x + 14 * (1 - 2 * flip);
    }

    int ArmRootY()
    {
        return y - 26;
    }

    /// 出手点（手臂末端）。**方向按 `angle` 算** —— 那是玩家调的角度，得看得见。
    int HandX()
    {
        return ArmRootX() + icos(angle) * ARM_LEN / SIN_SCALE * (1 - 2 * flip);
    }

    int HandY()
    {
        return ArmRootY() - isin(angle) * ARM_LEN / SIN_SCALE;
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

    /// 被打死的样子：**焦黑**的一团 + 三缕余烟。
    ///
    /// ⚠ 不做成"消失"：玩家得看见"我的猴子没了"才明白这一局是怎么结束的。
    ///   结束语在屏幕正中的横幅上（`Game::Draw`），可玩家的视线一直在自己那只猴子这边 ——
    ///   两处都要有交代，只写横幅的话会读成"画面卡住了"。
    ///
    /// ⚠ 轮廓与活着的猴子**同一套坐标**（腿 / 躯干 / 头 / 耳朵 / 眉骨）：轮廓一变
    ///   就认不出"这是刚才那只猴子"，而认不出来等于没交代。
    void DrawDead()
    {
        int ash;
        int soot;
        int i;
        int sx;

        ash = 0xFF3A3632;
        soot = 0xFF211E1B;

        ui_rect(x - 12, y - 9, 11, 9, ash, 1, 0, 3);
        ui_rect(x + 1, y - 9, 11, 9, ash, 1, 0, 3);
        ui_rect(x - 14, y - 30, 28, 23, ash, 1, 0, 9);
        ui_rect(x - 14, y - 30, 28, 23, soot, 0, 2, 9);
        ui_circle(x - 11, y - 39, 5, ash, 1, 0);
        ui_circle(x + 11, y - 39, 5, ash, 1, 0);
        ui_circle(x, y - 40, 12, ash, 1, 0);
        ui_circle(x, y - 40, 12, soot, 0, 2);
        // 眉骨照旧压着，眼睛画成两个 ✕ ——"死透了"得一眼看出来，不能靠猜
        ui_rect(x - 9, y - 47, 18, 4, soot, 1, 0, 2);
        ui_line(x - 7, y - 45, x - 3, y - 41, soot, 2);
        ui_line(x - 3, y - 45, x - 7, y - 41, soot, 2);
        ui_line(x + 3, y - 45, x + 7, y - 41, soot, 2);
        ui_line(x + 7, y - 45, x + 3, y - 41, soot, 2);

        // 三缕余烟。**不带时间参数** —— 这一局已经结束了，动不动的没人再看；
        // 而引入一个计时变量就多一处"重开时忘了归零"的隐患。
        sx = -10;
        i = 0;
        while (i < 3)
        {
            ui_circle(x + sx, y - 58 - i * 9, 4 - i, 0x55B0B0B0, 1, 0);
            sx = sx + 10;
            i = i + 1;
        }
    }

    virtual void Draw()
    {
        int ax;              // 肩点（手臂根）
        int ay;
        int hx;              // 手（= 香蕉的出手点）
        int hy;
        int nb;              // 镜像方向：+1 朝右、-1 朝左（后边那只胳膊用）
        int dark;
        int light;

        if (dead != 0) { DrawDead(); return; }

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
        //
        // ⚠ **必须跟着 `flip` 镜像**（`nb` 就是那个 ±1）。原来这条胳膊写死在左边，
        //   而举起来那只跟着 `flip` 走 ⇒ 右边的紫猴**两只胳膊都在左边** ——
        //   玩家报的"两只手都是左手"就是这个：一只镜像了、另一只没镜像。
        //   一条画两只胳膊的路径上只镜像一半，是本仓最典型的一类缺陷。
        // ⚠ 往外多伸 2px（末端 `x±18` 而不是 `x±16`）：躯干到 `x±14`，原来只有
        //   2px 的胳膊露在外面，看上去是"身侧挂着一个圆"而不是一条手臂。
        nb = 1 - 2 * flip;
        ui_line(x - 12 * nb, y - 26, x - 18 * nb, y - 10, dark, 9);
        ui_line(x - 12 * nb, y - 26, x - 18 * nb, y - 10, body, 6);
        ui_circle(x - 18 * nb, y - 10, 5, body, 1, 0);
        ui_circle(x - 18 * nb, y - 10, 5, dark, 0, 2);

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
        //
        // ⚠ 端点一律取自 `HandX/HandY`（与香蕉的出手点是同一个函数）——
        //   见那组函数上面的说明：分开算过一次，代价是"手在脸上、香蕉从胸口飞"。
        ax = ArmRootX();
        ay = ArmRootY();
        hx = HandX();
        hy = HandY();
        ui_line(ax, ay, hx, hy, dark, 10);      // 先粗的深色当描边
        ui_line(ax, ay, hx, hy, body, 6);       // 再细的本体色
        // 手掌要比手臂**明显**粗（半径 7 对线半宽 3）—— 只粗一点点的话，
        // 末端读出来是"一根棍子的圆头"，不是"手"。
        ui_circle(hx, hy, 7, body, 1, 0);
        ui_circle(hx, hy, 7, dark, 0, 2);
    }
};

// ════════════════════════════════════════════════════════════════════
// Banana —— 香蕉（定点弹道）
// ════════════════════════════════════════════════════════════════════

// ── 速度方向 → 姿态角（度）──────────────────────────────────────────────
//
// 用**菱形近似**算 atan2（那条 `45·ay/ax`），误差最大约 4.5° —— 香蕉只要"大致顺着
// 弹道"，而本平台上 `sqrt`/`sin`/`cos` 都不可用（见下面 `isqrt` 那段说明），
// 为几度的精度去引一条数学库依赖不值当。
//
// 角度口径与 `basic/gorilla_pro.bas` 的 `bananaAngle` 一致（**屏幕 y 向下**）：
// 向右上飞是**负角**、向右下飞是正角。两版用同一套，观感才对得上。
static int AngleOf(int vx, int vy)
{
    int ax = vx < 0 ? 0 - vx : vx;
    int ay = vy < 0 ? 0 - vy : vy;
    int t;

    if (ax == 0 && ay == 0) { return 0; }
    if (ax >= ay) { t = ax == 0 ? 0 : 45 * ay / ax; }
    else { t = 90 - (ay == 0 ? 0 : 45 * ax / ay); }

    if (vx >= 0)
    {
        if (vy < 0) { return 0 - t; }
        return t;
    }
    if (vy < 0) { return 180 + t; }
    return 180 - t;
}

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
    int bid;             // 月牙**图块**句柄（开窗后建一次，见 `MakeBlock`）
    int spin;            // 飞行中的自转累计角（度）—— 见 `Draw`

    Banana()
    {
        bx = 0;
        by = 0;
        vx = 0;
        vy = 0;
        live = 0;
        owner = 0;
        trailN = 0;
        bid = 0;
        spin = 0;
        x = 0;
        y = 0;
    }

    /// <summary>
    /// 把月牙录成**图块**（开窗之后调一次）。
    ///
    /// ⚠ 为什么不每帧直接画路径：`ui_path` 收的是**绝对场景坐标**，没有任何变换参数 ——
    /// 要让月牙顺着弹道转，就得把角度算进每一段贝塞尔的控制点里，那是一场噩梦。
    /// 图块正好补上这个缺口：形状随便画（曲线都行），贴的时候 `ui_draw_block` 带旋转。
    /// 路径字符串与 `basic/gorilla_pro.bas` 的 `makeBanana` **逐字相同**（两版同一只香蕉）。
    /// </summary>
    void MakeBlock()
    {
        bid = ui_create_block(30, 20, 0);
        // 月牙：两段三次贝塞尔，两个尖端在 (3,14) 与 (27,9)，凹面朝下
        ui_path("M 3 14 C 8 2, 22 -1, 27 9 C 20 4, 10 6, 3 14 Z", 0xFF8A7418, 1, 0xFFFFE066, "", 1, 0);
        // 两个蒂（深一点的圆点）—— 两端有蒂才像香蕉，不然像月牙
        ui_circle(3, 14, 2, 0xFF6E5A14, 1, 0);
        ui_circle(27, 9, 2, 0xFF6E5A14, 1, 0);
        bid = ui_end_block();
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
        spin = 0;              // 每一发重新开始翻跟头
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

        // 自转（翻跟头）。
        // ⚠ 光靠"顺着弹道"是不够的：一发平射全程只转 ±45°，22px 的小月牙看着几乎没动
        //   （`basic/gorilla_pro.bas` 那边的玩家原话是"转动幅度太小了"）。
        spin = spin + 18;
        if (spin >= 360) { spin = spin - 360; }

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
        if (bid > 0)
        {
            // 姿态 = **弹道方向**（新月顺着飞行方向）+ **自转**（翻跟头）。
            // 缩放 720‰：图块本身 30x20，原尺寸贴出来比大猩猩（约 28px 宽）还大一圈，
            // 看着像"扔出去一个球拍"；720 之后约 22px，比猿小、又还看得出是香蕉。
            ui_draw_block(bid, x, y, 720, 720, AngleOf(vx, vy) + spin);
        }
        else
        {
            // 图块没建起来（有真窗口时不该发生）—— 退回一个圆点。
            // **宁可画得糙，也不能让香蕉看不见**：看不见 = 这一局没法玩，
            // 而屏幕上没有任何东西提示你是哪儿坏了。
            ui_circle(x, y, 5, C_BANANA, 1, 0);
            ui_circle(x - 2, y - 2, 2, 0xFFFFFFFF, 1, 0);
        }
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

// ── 飞碟的"脾气"：速度与报复 ─────────────────────────────────────────
//
// 玩家定的：**飞碟要"速度很快飞来、悬停、很快飞走"**（原来 4px/拍 是飘过来的），
// 而且**打中它 = 招来报复**（见 `Ufo::BeginRage`）。
// 这三只定时器都按"拍"算（一拍 = `TICK_MS` = 33ms）。
#define UFO_V 10             // 巡航速度（px/拍）—— "很快"，是飞机(6)的 1.7 倍
#define UFO_HOLD 18          // 悬停拍数（原来 45，玩家要"悬停一下就走"）
#define RAGE_V 14            // 报复时扑向猴子的速度（比巡航还快）
#define RAGE_AIM 20          // 飞到头顶后瞄准的拍数（玩家看得见"它在瞄你"）
#define RAGE_SHOT 45         // 激光持续的拍数（约 1.5 秒 —— 够看清，又不拖沓）

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
    int bidUp;           // 翅膀朝上的那帧
    int bidDn;           // 翅膀朝下的那帧

    Bird()
    {
        kind = 1;
        wob = 0;
        t = 0;
        turns = 0;
        bidUp = 0;
        bidDn = 0;
    }

    /// <summary>
    /// 把鸟的**两种翅膀姿态**各录成图块（开窗之后调一次）。
    ///
    /// ⚠ 为什么是**两个**图块而不是"一个身体 + 一条会转的翅膀"：
    ///   翅膀是长在**身后**的（从 `cx-6` 甩到 `cx-14`），绕身体转会把根也甩出去。
    ///   两帧各录一份最直白，代价只是多一份 30×20 的录制。
    ///
    /// ⚠ 只录**朝右**的那一份 —— 朝左贴的时候用 `-1000` 镜像（见 `Draw`）。
    ///   这是本版才通的：`Stamp` 原先把 `sx <= 0` 一起拒了，负值（镜像）根本贴不出来。
    /// </summary>
    void MakeBlocks()
    {
        bidUp = ui_create_block(30, 20, 0);
        Shape(15, 10, 1);
        bidUp = ui_end_block();

        bidDn = ui_create_block(30, 20, 0);
        Shape(15, 10, -1);
        bidDn = ui_end_block();
    }

    /// <summary>一只**朝右**的鸟，画在局部坐标 (cx, cy) 处。两帧只差翅膀那条线。</summary>
    void Shape(int cx, int cy, int wingUp)
    {
        ui_circle(cx, cy, 6, 0xFF30343C, 1, 0);
        ui_circle(cx + 4, cy - 4, 4, 0xFF30343C, 1, 0);
        ui_rect(cx + 7, cy - 5, 4, 2, 0xFFFFC060, 1, 0, 0);
        if (wingUp > 0) { ui_line(cx - 6, cy, cx - 14, cy + 5, 0xFF50565E, 3); }
        else { ui_line(cx - 6, cy, cx - 14, cy - 5, 0xFF50565E, 3); }
    }

    void Spawn(int sw, int gy, int dir)
    {
        live = 1;
        turns = 0;
        t = 0;
        vx = 3 * dir;
        if (dir > 0) { x = -20; }
        else { x = sw + 20; }
        // 高度随机，**最低**那一档（鸟本来就贴着楼顶飞，偶尔从楼顶那一线掠过才好看）。
        // ⚠ 原来 `gy/4 + rand(gy/3)` 最下一档也会到 0.58·gy，比最高的楼顶（0.297·gy）还低一截。
        y = gy * 21 / 100 + ui_rand(gy * 9 / 100);
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
        int b;

        if (live == 0) { return; }

        // ⚠ 朝向必须**按当前速度算**（`vx` 会被 `t % 90` 那个掉头翻转），
        //   不能按出生时的 `dir`。原来整套图形是按"朝右"写死的（头在 x+4、喙在 x+7、
        //   尾巴在 x-14）⇒ **掉头之后就是倒着飞**（玩家报的正是这个）。
        d = 1;
        if (vx < 0) { d = -1; }

        b = bidUp;
        if (wob < 0) { b = bidDn; }

        if (b > 0)
        {
            // 朝左 = **水平镜像**（`sx` 取负）。同一个图块左右通吃，不必录两份形状。
            ui_draw_block(b, x, y, d * 1000, 1000, 0);
        }
        else
        {
            // 图块没建起来（有真窗口时不该发生）—— 退回一只"圆点鸟"。
            // **宁可画得糙，也不能让它看不见**：看不见 = 打不中它，而屏幕上没有
            // 任何东西提示你是哪儿坏了。
            ui_circle(x, y, 6, 0xFF30343C, 1, 0);
            ui_circle(x + 4 * d, y - 4, 4, 0xFF30343C, 1, 0);
        }
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
    int bid;             // 碟身图块（座舱罩 + 碟身；光晕与灯是动态的，不进块）

    // ── 报复模式 ─────────────────────────────────────────────────────
    //
    // 玩家定的玩法：**打中飞碟会招来报复** —— 它眼神不好，只找**离它最近的**那只猴子，
    // 飞到头顶放激光，直接打死，一局结束。
    //
    // ⚠ 所以"打中飞碟"**不再等于击落它**（鸟和飞机照旧击落）。这一发的代价从
    //   "白扔一个香蕉"变成了"可能输掉这一局" —— 这正是玩家要的张力：天上飞的
    //   不再是"可以随便打的靶子"，而是**要躲开的东西**。
    int rage;            // 1 = 报复模式
    int target;          // 目标猴子（0 / 1）
    int arrived;         // 已经飞到目标头顶
    int aimT;            // 到位之后瞄了几拍
    int shot;            // 激光已经开了几拍（0 = 还没开火）
    int fired;           // 已经结算过（防止"每拍都打死一次"）

    Ufo()
    {
        kind = 2;
        phase = 0;
        hold = 0;
        t = 0;
        lit = 0;
        bid = 0;
        rage = 0;
        target = 0;
        arrived = 0;
        aimT = 0;
        shot = 0;
        fired = 0;
    }

    /// 被香蕉打中 → **转入报复**（而不是被击落）。
    /// `who` 是目标猴子，由 `Game::NearestApeTo` 按"离飞碟最近"选出来。
    void BeginRage(int who)
    {
        rage = 1;
        target = who;
        arrived = 0;
        aimT = 0;
        shot = 0;
        fired = 0;
        live = 1;
        phase = 0;
        t = 0;
        SfxRage();           // 下行警报：玩家要立刻知道"我惹到它了"
        ui_vibrate(120, 0);
    }

    /// 报复模式的一拍：扑向目标头顶 → 悬停瞄准 → 开火。
    ///
    /// ⚠ 走的是**直线逼近**（每拍朝目标走 `RAGE_V`），不是"先横后竖"的分段 ——
    ///   分段会让它在猴子正上方拐个直角，看着像"按格子走"，与"扑过来"完全不是一回事。
    void RageStep(int sw, int gy)
    {
        int tx;
        int ty;
        int dx;
        int dy;
        int d;

        // 目标点 = 猴子**头顶上方** 78px（猴子头顶在 `y-52`，再留 26px 空档，
        // 免得碟身压着猴子的脑袋 —— 那样激光就没地方画了）
        tx = (*AP[target]).x;
        ty = (*AP[target]).y - 78;

        if (arrived == 0)
        {
            dx = tx - x;
            dy = ty - y;
            d = isqrt(dx * dx + dy * dy);
            if (d <= RAGE_V)
            {
                x = tx;
                y = ty;
                arrived = 1;
            }
            else
            {
                x = x + dx * RAGE_V / d;
                y = y + dy * RAGE_V / d;
            }
        }
        else if (aimT < RAGE_AIM)
        {
            aimT = aimT + 1;
            y = y + (ui_rand(3) - 1);        // 悬停时轻微浮动（"它在瞄准"）
        }
        else if (shot < RAGE_SHOT)
        {
            shot = shot + 1;
        }
    }

    /// <summary>
    /// 把碟身录成图块（开窗之后调一次）。
    ///
    /// ⚠ 碟身**不能整体旋转**（座舱罩在顶上，转起来就成了"歪帽子"）—— 它用图块是为了
    ///   形状只写一遍 + 贴出时省调用。**会转的是底下那圈灯**（见 `Draw` 里 `spin` 那段）：
    ///   一圈灯绕中心转，才像"盘子底下有东西在转"。
    ///
    /// 块范围按最外沿取：碟身 `x±22`、罩顶到 `y-16` ⇒ 48×34，中心在局部 (24,17)。
    /// </summary>
    void MakeBlock()
    {
        bid = ui_create_block(48, 34, 0);
        // 座舱罩：**先画一整个圆**，碟身随后盖掉它的下半 ⇒ 正好剩一个半圆罩
        //（本平台没有裁剪，这个"画完再盖"就是最省事的做法）
        ui_circle(24, 11, 10, 0xFF8ADCFF, 1, 0);
        ui_circle(24, 11, 10, 0xFF2E6E9E, 0, 2);
        ui_ellipse(20, 8, 3, 2, 0xFFFFFFFF, 1, 0);          // 罩子高光
        // 碟身：宽扁椭圆（宽:高 ≈ 3:1 才像碟）
        ui_ellipse(24, 18, 22, 7, 0xFFC8D0DC, 1, 0);
        ui_ellipse(24, 18, 22, 7, 0xFF5A6472, 0, 2);
        // 碟身上半的一道亮边，给它"金属盘子"的感觉
        ui_ellipse(24, 16, 17, 3, 0xFFE8EEF6, 1, 0);
        bid = ui_end_block();
    }

    void Spawn(int sw, int gy, int dir)
    {
        live = 1;
        phase = 0;
        t = 0;
        lit = 0;
        // 上一次若是在报复中被重开，这些状态得清掉（否则新飞碟一出来就直奔猴子）
        rage = 0;
        target = 0;
        arrived = 0;
        aimT = 0;
        shot = 0;
        fired = 0;
        vx = UFO_V * dir;
        if (dir > 0) { x = -30; }
        else { x = sw + 30; }
        // 高度随机，中间那一档（见 `Plane::Spawn` 的说明）。
        // ⚠ 原来 `gy/3 + rand(gy/4)` 最下一档到 0.58·gy，那已经**低于最高的楼顶**
        //   （0.297·gy），碟子会从楼中间穿过去。
        y = gy * 17 / 100 + ui_rand(gy * 8 / 100);
        hold = sw / 2 + ui_rand(sw / 3);
    }

    virtual int R() { return 16; }

    virtual void Step(int sw, int gy)
    {
        if (live == 0) { return; }
        t = t + 1;
        if (t % 4 == 0) { lit = 1 - lit; }

        // 报复模式**整条走法都不一样**（扑向猴子 → 悬停 → 开火），
        // 而且它不该再"飞出屏幕就消失" —— 那等于半路撤销了惩罚。
        if (rage != 0) { RageStep(sw, gy); return; }

        if (phase == 0)
        {
            x = x + vx;
            if (vx > 0) { if (x >= hold) { phase = 1; t = 0; } }
            else { if (x <= hold) { phase = 1; t = 0; } }
        }
        else if (phase == 1)
        {
            y = y + (ui_rand(3) - 1);
            if (t > UFO_HOLD) { phase = 2; }
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

        // ⚠ 原来是「大球 + 小球」，玩家原话是「飞碟也是个球」。
        //   飞碟之所以一眼是飞碟，靠的是**宽扁的碟身 + 顶上一个罩子**这两条轮廓线，
        //   光有圆是读不出来的。这两条轮廓现在录在 `MakeBlock` 里。
        //
        // 悬停的光晕：**先画**（随后被碟身压住一半）。
        // 它是半透明的、又跟着碟身走，留在块外更省事（进了块也只是多录一行）。
        ui_ellipse(x, y + 10, 17, 4, 0x40A0E0FF, 1, 0);

        if (bid > 0)
        {
            ui_draw_block(bid, x, y, 1000, 1000, 0);
        }
        else
        {
            // 图块没建起来（有真窗口时不该发生）—— 退回"大球 + 小球"。
            // ⚠ 那正是玩家挑过的形状，但**看不见比难看糟得多**。
            ui_circle(x, y - 6, 10, 0xFF8ADCFF, 1, 0);
            ui_ellipse(x, y + 1, 22, 7, 0xFFC8D0DC, 1, 0);
        }

        // ── 底下一圈灯（`lit` 隔一阵闪一下）──
        // **动态的，不进图块**：进了就得录两帧，而它只值五个圆点。
        if (lit != 0)
        {
            ui_circle(x - 15, y + 5, 2, 0xFFFF5050, 1, 0);
            ui_circle(x - 8,  y + 7, 2, 0xFFFFE050, 1, 0);
            ui_circle(x,      y + 8, 2, 0xFF50FF70, 1, 0);
            ui_circle(x + 8,  y + 7, 2, 0xFF50D0FF, 1, 0);
            ui_circle(x + 15, y + 5, 2, 0xFFFF5050, 1, 0);
        }

        // ── 报复：瞄准警示 + 激光 ──────────────────────────────────────
        //
        // 警示圈：**到位之后、开火之前**那段（`RAGE_AIM` 拍 ≈ 0.7 秒）在猴子头上
        // 闪一个红圈。玩家反应不过来，但"我知道我要死了"和"莫名其妙就死了"
        // 是两种完全不同的体验 —— 前者是惩罚，后者是 bug。
        if (rage != 0 && arrived != 0 && shot == 0)
        {
            if (aimT % 6 < 3)
            {
                ui_circle((*AP[target]).x, (*AP[target]).y - 30, 20, 0xFFFF4040, 0, 3);
            }
        }

        // 激光：三层同轴（宽而淡 → 中 → 细而白）。
        // ⚠ 与流星尾迹是**同一套画法**：单画一条线读出来是"一根棍子"，
        //   三层叠起来才有"能量烧穿"的观感。起画点取碟身下沿 `y+8`，
        //   免得被碟身盖掉一截（碟身是**先**画的，后画的线会盖住它 —— 所以起点
        //   定在碟身下沿而不是中心）。
        if (rage != 0 && shot > 0)
        {
            int cx;
            int cy;

            cx = (*AP[target]).x;
            cy = (*AP[target]).y - 18;      // 落在猴子**身上**（不是脚下）
            ui_line(x, y + 8, cx, cy, 0x50FF3030, 13);
            ui_line(x, y + 8, cx, cy, 0xCCFF5050, 6);
            ui_line(x, y + 8, cx, cy, 0xFFFFFFFF, 2);
            ui_circle(cx, cy, 9, 0x80FF6060, 1, 0);
            ui_circle(cx, cy, 4, 0xFFFFFFFF, 1, 0);
        }
    }
};

// ── 飞机：定期飞过、匀速直线；夜里机翼有闪灯 ──
class Plane : public Flyer
{
public:
    int t;
    int lit;
    int bid;             // 机身图块（含尾翼与座舱；灯是动态的，不进块）

    Plane()
    {
        kind = 3;
        t = 0;
        lit = 0;
        bid = 0;
    }

    /// <summary>
    /// 把机身录成图块（开窗之后调一次）。只录**朝右**那一份，朝左用 `-1000` 镜像。
    /// 块范围按最外沿取：机身 `x±16`、尾翼到 `y-10`、灯在 `y-11` ⇒ 40×24，中心在局部 (20,12)。
    /// </summary>
    void MakeBlock()
    {
        bid = ui_create_block(40, 24, 0);
        ui_rect(4, 9, 32, 6, 0xFFD8DEE6, 1, 0, 3);          // 机身
        ui_line(18, 12, 8, 2, 0xFFB8C0CC, 3);               // 尾翼（后）
        ui_line(22, 12, 28, 3, 0xFFB8C0CC, 3);              // 尾翼（前）
        ui_rect(26, 10, 8, 5, 0xFF6FA8DC, 1, 0, 2);         // 座舱
        bid = ui_end_block();
    }

    void Spawn(int sw, int gy, int dir)
    {
        live = 1;
        t = 0;
        lit = 0;
        vx = 6 * dir;
        if (dir > 0) { x = -40; }
        else { x = sw + 40; }
        // 高度**随机**（玩家："飞机的高度…都应该随机"）。原来写死 `gy / 5`，
        // 于是每一架都在同一条水平线上飞 —— 看两眼就发现是"轨道"不是"天空"。
        //
        // ── 三档飞行高的划分（见 `Flyer` 那段的说明）────────────────────
        // 天空带其实**只有 0.13·gy ~ 0.30·gy 这一段**：
        //   · 上限 0.13·gy —— 顶上是计分/风向栏（`hud` 是**最后**画的，飞进去就被盖住）；
        //   · 下限 0.30·gy —— 最高那栋楼的楼顶（楼高 = sh/5+sh/4，两边那两栋再加 sh/14
        //     ⇒ 楼顶最高到 0.22·sh = 0.297·gy），再低就从楼里穿过去了。
        // 于是三档**首尾相接**地铺满这一段：飞机最高、鸟最低、飞碟居中。
        // 三档各自 7~9% 的宽度，飞几趟就能看出高度不是固定的。
        y = gy * 13 / 100 + ui_rand(gy * 7 / 100);
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

        if (live == 0) { return; }

        // 与鸟同一个道理：机身按**当前速度**镜像（原来写死朝右）
        d = 1;
        if (vx < 0) { d = -1; }

        if (bid > 0)
        {
            ui_draw_block(bid, x, y, d * 1000, 1000, 0);
        }
        else
        {
            // 图块没建起来（有真窗口时不该发生）—— 退回简版（见 `Bird::Draw` 的同一条理由）
            ui_rect(x - 16, y - 3, 32, 6, 0xFFD8DEE6, 1, 0, 3);
            ui_rect(x + 6 * d - 8, y - 2, 8, 5, 0xFF6FA8DC, 1, 0, 2);
        }

        // 航行灯：**动态的**（`lit` 隔一阵闪一下），不进图块 —— 进了就得录两帧、
        // 而它只值一个圆点。位置按 `d` 镜像，与机身同一边。
        if (lit != 0) { ui_circle(x - 14 * d, y - 11, 2, 0xFFFF4040, 1, 0); }
    }
};

// ════════════════════════════════════════════════════════════════════
// Tree —— 地上的树（位置 / 数量 / 高矮都随机）
// ════════════════════════════════════════════════════════════════════

/// 蒙版**底矩形**比楼体向外扩多少（px）。
///
/// ⚠⚠ 这一圈是**让洞不被缩小**的关键。折叠"楼 − 洞"要求洞整个落在底矩形里
/// （even-odd 下"底外洞内"会被判成**内部**，那一块就会允许绘制）。
/// 而**楼体外的这一圈本来就没有任何绘制**（建筑层画的楼体/窗/门/檐口全在楼矩形内），
/// 所以把底矩形放大一圈、让洞挖进去，**不会有"肉"露出来**，洞却能保持原始大小。
///
/// 洞半径最大 16 ⇒ 20 足够。
#define MASK_PAD 20

/// 树的**标准尺寸**（录图块用）。⚠ 树的高矮宽窄每棵都不同，贴的时候按**高度等比缩放**
/// （宽度不按实际值 —— 那会让树冠变成椭圆；宽度上的差异在观感上本来就只是"冠大一点"）。
#define TREE_STD_W 40
#define TREE_STD_H 64

/// 一棵树的局部形状：`cx` = 树干中轴、`cy` = 树根、`cw`/`chh` = 冠宽/树高、`v` = 绿档 0..2。
///
/// 抽成函数是为了录图块（形状写一遍，录的时候调它、画的时候贴）。
/// ⚠ 绿档**本来就是三档固定色**（按位置取，与天光无关）⇒ 树只要录 3 个图块，不用分昼夜。
void treeShapeAt(int cx, int cy, int cw, int chh, int v)
{
    int r;
    int cyy;
    int dark;
    int mid;
    int lit;

    dark = 0xFF1E4A22;
    mid  = 0xFF2E7A38;
    lit  = 0xFF57B05E;
    if (v == 1) { dark = 0xFF22401E; mid = 0xFF4A7A2E; lit = 0xFF78C05A; }
    if (v == 2) { dark = 0xFF173F2A; mid = 0xFF2A6E4A; lit = 0xFF4FA87A; }

    // 树干
    ui_rect(cx - 2, cy - chh / 3, 5, chh / 3 + 2, dark, 1, 0, 1);
    ui_rect(cx - 1, cy - chh / 3, 3, chh / 3, 0xFF5A4326, 1, 0, 1);

    // 树冠：两侧小圆先画（当底部层次），主冠盖上去，最后左上一块受光
    cyy = cy - chh * 2 / 3;
    r = cw / 2 + 2;
    ui_circle(cx - r * 3 / 4, cyy + r / 3, r / 2, dark, 1, 0);
    ui_circle(cx + r * 3 / 4, cyy + r / 3, r / 2, dark, 1, 0);
    ui_circle(cx, cyy, r, mid, 1, 0);
    ui_circle(cx - r / 4, cyy - r / 3, r * 2 / 3, lit, 1, 0);
}

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
        int v;              // 本棵的绿色档（0..2）—— 按位置取，不用额外字段
        v = (x / 7) % 3;
        if (treeBid[v] > 0)
        {
            // ⚠ **按高度等比缩放**：宽度不按实际值 —— 那会让树冠变椭圆，
            //   而宽度上的差异观感上只是"冠大一点"。
            // ⚠ 图块的"高"含树根**下方 2px**（树冠画到 cy-h/3+2），所以中心要跟着偏。
            int sc;
            sc = h * 1000 / TREE_STD_H;
            ui_draw_block(treeBid[v], x, y - h / 2, sc, sc, 0);
            return;
        }
        treeShapeAt(x, y, w, h, v);
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
    int overKind;           // 0 = 打够分数结束 / 1 = 被飞碟激光清场
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
        overKind = 0;
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

    /// <summary>
    /// 把**会动的精灵**都录成图块（开窗之后调一次，见 `main`）。
    ///
    /// 三类精灵一起走这条路：形状只写一遍、贴出来带旋转/镜像/缩放，
    /// 而且每帧从"十几次绘图调用"压成"一次贴图块"（实测 300 个精灵 × 60 帧：
    /// 直接画 3531ms → 贴图块 2945ms）。
    /// </summary>
    void MakeBlocks()
    {
        ban.MakeBlock();
        bird.MakeBlocks();
        ufo.MakeBlock();
        plane.MakeBlock();

        // ── 楼的窗户：**每栋各录一套**（宽高都不同）────────────────────────
        //
        // ⚠ 必须在 `Layout()` **之后**、且**有场景**时调 —— 开窗之前 `ui_create_block`
        //   会返回 0（块是宿主那边的资源，没有场景就建不起来）。
        // ⚠ 每局楼宽高都变 ⇒ **每次 Layout 之后都要重录**（这里 + `Restart` 各一处）。
        {
            int i;
            i = 0;
            while (i < gBldgN) { (*BL[i]).MakeWinBlocks(); i = i + 1; }
        }

        // ── 云 / 路灯：按天光**分档录** ────────────────────────────────────
        //
        // ⚠ 图块里的颜色是**录制那一刻定死的**，而这两样的颜色随昼夜连续变
        //   ⇒ 每档录一份，贴的时候按当前天光选最近的一档。
        //   24 真实分钟走完一天 ⇒ 每档约 1.5 分钟，档与档之间的颜色差看不出来。
        //
        // ⚠ 录制期间要**临时改 `gDayL`**（云还要用"当地天空色" ⇒ 顺带调一次
        //   `clockPartsAt`）。用屏宽中点的高度当代表 —— 云分布在不同高度、天空色
        //   略有差异，但那个差异比档位差还小。
        // ⚠ **录完必须恢复 `gDayL`**：它决定天色，留着档位值会让第一帧的天色不对
        //   （下一帧 `Draw` 开头的 `clockCompute` 会重算，但那一帧已经画出去了）。
        int k;
        int d;
        int saveDay;
        saveDay = gDayL;
        k = 0;
        while (k < VML_DAY_STEPS)
        {
            d = k * 100 / (VML_DAY_STEPS - 1);
            gDayL = d;
            clockPartsAt(sw / 2, gy);

            cloudBid[k] = ui_create_block(CLOUD_STD_W, CLOUD_STD_H, 0);
            // ⚠ 云心在图块里**偏下**：从顶算 2×ch（最大的鼓包顶到那儿）—— 贴的时候减掉
            cloudShapeAt(CLOUD_STD_W / 2, CLOUD_STD_W * 2 / 3, CLOUD_STD_W, d);
            cloudBid[k] = ui_end_block();

            lampBid[k] = ui_create_block(LAMP_STD_W, LAMP_STD_H, 0);
            // 灯柱底放在图块底边 ⇒ 图块中心相对灯柱底偏 (8.5, -23)
            lampShapeAt(1, LAMP_STD_H, d);
            lampBid[k] = ui_end_block();
            k = k + 1;
        }

        // 树的绿档**本来就是固定色**（与天光无关）⇒ 只录 3 个
        k = 0;
        while (k < 3)
        {
            treeBid[k] = ui_create_block(TREE_STD_W, TREE_STD_H, 0);
            treeShapeAt(TREE_STD_W / 2, TREE_STD_H, TREE_STD_W, TREE_STD_H, k);
            treeBid[k] = ui_end_block();
            k = k + 1;
        }

        gDayL = saveDay;
    }

    /// 离横坐标 `px` **最近**的那只猴子（0 / 1）—— 飞碟报复时挑目标用。
    ///
    /// ⚠ 判据是「离**飞碟**最近」（玩家原话："他只找离他最近的猴子"），
    ///   不是"离被打中那栋楼最近"、也不是"离发射者最近"。
    /// ⚠ 平手取左边那只（`<=`）：**必须有个确定的裁决** —— 同一帧里若两次调用
    ///   给出不同答案，目标就会在半路改口，飞碟会在空中拐一个莫名其妙的弯。
    int NearestApeTo(int px)
    {
        if (iabs(px - (*AP[0]).x) <= iabs(px - (*AP[1]).x)) { return 0; }
        return 1;
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
        // ⚠ 音效挂在**这里**（动作本身），不是挂在某条输入路径上 ——
        //   原先它只写在触摸那支里，于是**键盘回车发射是一声不响的**
        //   （游戏是"全触摸"，键盘只是兜底，所以这个缺口一直没被发现）。
        //   挂在动作上则键盘 / 触摸 / 将来真加了自动发射，都自动有。
        SfxFire();
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

        // ── 飞碟的激光打完了吗？→ 猴子死、这一局结束 ──────────────────────
        //
        // ⚠ 结算放在 **Game** 这一层，而不是 `Ufo::RageStep` 里：飞碟只管
        //   "我怎么飞、什么时候开火"，"谁死了、这一局算不算完"是**游戏的规则** ——
        //   规则挂到飞行物身上，将来加第二艘飞碟就得在两处各写一遍。
        // ⚠ `fired` 是**必须的**：`shot` 到顶之后会一直停在 `RAGE_SHOT`，
        //   没有这个闸门就是"每拍打死一次、每拍结束一局"。
        if (ufo.rage != 0 && ufo.shot >= RAGE_SHOT && ufo.fired == 0)
        {
            ufo.fired = 1;
            (*AP[ufo.target]).dead = 1;
            overKind = 1;
            over = 1;
            boomX = (*AP[ufo.target]).x;
            boomY = (*AP[ufo.target]).y - 20;
            SfxLaser();
            ui_vibrate(400, 0);
            return 1;
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
                            // ⚠ **只有飞碟不一样**：打中它 = **招来报复**，不是把它打下来。
                            //   鸟和飞机照旧击落（代价仍然只是"白扔一个香蕉"）。
                            // ⚠ 这里按**下标**判（`FLY[1]` 是飞碟，见 `Bind`）——
                            //   要是哪天调整了 `Bind` 里的顺序，这一行也得跟着改。
                            //   三种飞行物的**走法本来就不一样**，用 `kind` 判更稳，
                            //   所以判据写在这里，别挪去别处再抄一份。
                            if (i == 1) { ufo.BeginRage(NearestApeTo(ufo.x)); }
                            else { FLY[i]->live = 0; }
                            hit = -3;
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
                SfxHit();
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
                SfxAirBoom();
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
                SfxGroundBoom();
                return 1;
            }
            if (r == 2)
            {
                hitBy = -1;
                state = ST_BOOM;
                boomT = BOOM_TICKS;       // 飞出去：不画爆炸，直接进入下一回合
                ban.Stop();
                return 1;
            }
        }
        else if (state == ST_BOOM)
        {
            boomT = boomT + 1;
            if (boomT > BOOM_TICKS)
            {
                // 打够分就结束 —— 赢家**一定是刚刚得分的那位**（分数是一个一个加的，
                // 不存在两人同时到顶），所以这里不必再比一次分数。⚠ 但**不能**因此
                // 就用 `SfxWin()` 一把梭：另一条结束路径（被飞碟清场）的胜负与分数无关，
                // 它在上面单独响了 `SfxLaser()`。
                if ((*AP[0]).score >= WIN_SCORE || (*AP[1]).score >= WIN_SCORE)
                {
                    over = 1;
                    SfxWin();
                }
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
            Fire(&(*AP[turn]));      // 发射音在 Fire() 里，别在这里再来一次
        }
    }

    void Draw()
    {
        int i;
        Entity* actors[16];
        int actorN;
        int onBldg;         // 这个弹坑是不是开在楼上（见 `HoleOnBldg`）
        int k;
        int j;
        int hx;             // 画洞蒙版时的**钳制后**圆心/半径（见那段注释）
        int hy;
        int hr;
        int hn;             // 本楼有几个洞（合并之后）
        int changed;        // 合并循环：这一趟有没有合并过
        int a;
        int b2;
        int ddx;
        int ddy;
        int dd;

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

        // ── 建筑层：**楼 − 自己身上的洞** ───────────────────────────────
        //
        // 三层模型（玩家定的）：**背景层 / 建筑层 / 精灵层**。
        // 建筑层"可以被炸穿" = 楼体**挖掉**洞 ⇒ 上一层画好的背景**直接从洞里透出来**
        // （云、日月都是活的），不需要在洞里补画任何东西。
        //
        // ⚠ 上一版是"每个洞开一个洞口蒙版、把背景**重画一遍**"。像素结果对，但：
        //   · 24 个洞 = **24 倍**背景图元（天空渐变 + 星 + 日月 + 云各来一遍）；
        //   · 而且洞一多就撞 `MaxFigures = 12000`（新图元被**静默丢弃**）。
        //   现在每个洞只花"一个蒙版形状"，楼体只画一遍。
        //
        // ⚠ `actors[1..gBldgN]` 是楼，后面依次是猴子、香蕉。这个循环原先是一整趟跑完的，
        //   于是弹坑只能画在**最后**，把香蕉也盖住了 —— 玩家说的
        //   「被炸穿的地方还是会挡住香蕉」就是这么来的（香蕉是飞在半空的，
        //   而坑是开在墙上的，墙在香蕉**后面**）。
        //
        // ⚠ 顺序：**屋顶细节 → 开蒙版 → 楼体**。屋顶细节（水箱 / 天线）伸到楼体矩形
        //   **上方**，套进"楼体矩形 − 洞"的蒙版里会被整块裁掉。
        i = 1;
        k = 0;
        while (i <= gBldgN)
        {
            (*BL[k]).DrawRoof();

            // 先数本栋有几个洞 —— **开不开蒙版由它决定**（见下面的 `if`）
            onBldg = 0;
            j = 0;
            while (j < holeN)
            {
                if (HoleOnBldg(j, k, gy) != 0) { onBldg = onBldg + 1; }
                j = j + 1;
            }

            // ⚠⚠ **没洞就不开蒙版** —— 这一条是真机实测出来的大头。
            //   蒙版在平台侧是 `clipPath`，而 Android 硬件加速下每次路径裁剪都要
            //   重建裁剪层；一帧 6 栋楼各开一次，单单这一项就吃掉 **30ms**（17fps → 26fps）。
            //   而**没有洞的楼，蒙版里除了底矩形什么都没有** —— 它起不到任何作用。
            //   （开局时全城都没洞，这一条把所有楼的裁剪都省掉了。）
            if (onBldg == 0)
            {
                (*BL[k]).DrawBody();
            }
            else
            {
            // 蒙版 = 楼体矩形 − 本栋楼上所有的洞（`SUBTRACT` 一次算完，不必逐洞嵌套）
            ui_mask_begin();
            // ⚠ 底矩形**比楼体大一圈**（`MASK_PAD`）—— 见那个常量的说明：
            //   楼外那一圈本来就没有绘制，所以洞挖进去不会露出"肉"；
            //   而底矩形只贴着楼体的话，洞一贴边就被迫缩小（玩家报的「就一个小洞」）。
            ui_rect((*BL[k]).Left() - MASK_PAD, (*BL[k]).RoofY() - MASK_PAD,
                    (*BL[k]).w + MASK_PAD * 2, (*BL[k]).h + MASK_PAD * 2, 0xFFFFFFFF, 1, 0, 0);
            ui_mask_end(1);

            // 没有洞就别开那一段（`SUBTRACT` 空集本来是合法的，但少一段就少一次逐点判定）
            if (onBldg > 0)
            {
                // ── 先把本楼的洞收进一个临时表，**钳制进楼、再合并重叠的** ──────────
                //
                // 这两步都是为了让"楼 − 洞"能被矢量后端**折叠成一条路径**。折叠不成立时
                // 整个窗口会退回光栅后端 —— 画面立刻变糊、帧率从几十掉到 6（实测 6.2fps）。
                //
                // ① **钳制**：折叠要求每个洞整个落在楼里。洞一旦探出楼外，
                //    "楼外那块洞"在 even-odd 下只穿过一次 ⇒ 被判成**内部** ⇒ 画出一块不该有的肉。
                // ② **合并**：折叠要求洞与洞互不重叠 —— 重叠处同样会被 even-odd 填实。
                //    这条在数学上绕不过去（NonZero 也一样），只能**不让它重叠**。
                //    ⚠ 观感上反而更对：炸得太密，破洞本来就该连成一片。
                hn = 0;
                j = 0;
                while (j < holeN)
                {
                    if (HoleOnBldg(j, k, gy) != 0)
                    {
                        hx = holeX[j]; hy = holeY[j]; hr = holeR[j];
                        // 边界用**放大后的底矩形**（不是楼体本身）—— 洞因此基本不会被缩，
                        // 只有极端情况（洞比 PAD 还大）才动它
                        ClampHoleToBuilding(&hx, &hy, &hr, (*BL[k]).Left() - MASK_PAD,
                                            (*BL[k]).Right() + MASK_PAD,
                                            (*BL[k]).RoofY() - MASK_PAD, gy + MASK_PAD);
                        mhx[hn] = hx; mhy[hn] = hy; mhr[hn] = hr;
                        hn = hn + 1;
                    }
                    j = j + 1;
                }
                // 反复合并，直到没有一对重叠（合并会造出更大的圆，可能又压到别人）
                changed = 1;
                while (changed != 0)
                {
                    changed = 0;
                    a = 0;
                    while (a < hn)
                    {
                        b2 = a + 1;
                        while (b2 < hn)
                        {
                            ddx = mhx[b2] - mhx[a];
                            ddy = mhy[b2] - mhy[a];
                            dd = isqrt(ddx * ddx + ddy * ddy);
                            if (dd < mhr[a] + mhr[b2])          // 重叠（**相切不算**，见宿主那条判据）
                            {
                                MergeHoles(mhx[a], mhy[a], mhr[a], mhx[b2], mhy[b2], mhr[b2],
                                           &hx, &hy, &hr);
                                // 合并出来的圆可能探出楼外 ⇒ 再钳一次
                                ClampHoleToBuilding(&hx, &hy, &hr, (*BL[k]).Left() - MASK_PAD,
                                                    (*BL[k]).Right() + MASK_PAD,
                                                    (*BL[k]).RoofY() - MASK_PAD, gy + MASK_PAD);
                                mhx[a] = hx; mhy[a] = hy; mhr[a] = hr;
                                // 把最后一个搬到 b2 的位置（顺序无所谓）
                                hn = hn - 1;
                                mhx[b2] = mhx[hn]; mhy[b2] = mhy[hn]; mhr[b2] = mhr[hn];
                                changed = 1;
                            }
                            else
                            {
                                b2 = b2 + 1;
                            }
                        }
                        a = a + 1;
                    }
                }

                ui_mask_begin();
                a = 0;
                while (a < hn)
                {
                    ui_circle(mhx[a], mhy[a], mhr[a], 0xFFFFFFFF, 1, 0);
                    a = a + 1;
                }
                ui_mask_end2(VML_MASK_SUBTRACT);
            }

            (*BL[k]).DrawBody();
            ui_mask_clear();

            }   // ← 结束"有洞才开蒙版"那个 else

            i = i + 1;
            k = k + 1;
        }

        // ── 洞口的断面：**不画了**（玩家："炸完了怎么还留下一个圆环？"）────────
        //
        // 这里原先描两圈：内壁一圈暗色（"墙厚"）+ 左上受光一线（"翻起来的边"）。
        // 那是**"洞里涂天空色"那个年代的补丁** —— 当时洞和楼的边界靠这一圈才分得开，
        // 不然读出来像一张贴上去的圆纸片。
        //
        // 现在洞是**真的挖掉了**（建筑层 = 楼 − 洞），洞沿**天然就是楼的边界**：
        // 一边是楼体色、一边是透出来的天空，对比本来就够。再套一圈就成了
        // "炸穿的洞"上额外挂的一圈装饰 —— 玩家一眼看出来不对。
        //
        // ⚠ 洞的边界**不需要**任何描边来"帮助识别"。真觉得糊（比如楼色与天空接近），
        //   那要调的是**楼的配色**，不是给洞加圈。

        holesDraw(gy, sh);          // 地上的坑（挖土）

        // ── 精灵层：猴子、香蕉（`actors[0]` 是天空、`actors[1..gBldgN]` 是楼，都已画过）
        //
        // ⚠ 起点必须是 `gBldgN + 1`。原先这里**没有赋起点**，沿用上面洞循环遗留下来的 `i`
        //   ⇒ 下标变成了**洞的数量**（拿一个不相干的计数当数组下标）：
        //   洞少时把天空和楼又画一遍（**盖掉云和洞口断面**），洞多时前面的猴子被整段跳过。
        //   这是本仓"变量复用出了边界"的又一个实例 —— 循环变量用完就该显式重置。
        i = gBldgN + 1;
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
        if (state == ST_BOOM && boomT <= BOOM_TICKS)
        {
            int r;
            int k;
            int dx;
            int dy;

            // 半径随时间线性长到 `6 + BOOM_TICKS*3`（= 42，直径 84）——
            // 时间与大小由 `BOOM_TICKS` 一处决定，见那个常量的说明。
            r = 6 + boomT * 3;
            ui_circle(boomX, boomY, r + 5, 0x30FF7020, 1, 0);         // 半透明冲击波
            ui_circle(boomX, boomY, r, C_BOOM, 1, 0);
            ui_circle(boomX, boomY, r * 2 / 3, C_BANANA, 1, 0);
            ui_circle(boomX, boomY, r / 3, 0xFFFFFFFF, 1, 0);

            k = 0;
            while (k < 8)
            {
                dx = icos(k * 45) * (r + 7) / 800;
                dy = -isin(k * 45) * (r + 7) / 800;
                ui_rect(boomX + dx, boomY + dy, 3, 3, C_BOOM, 1, 0, 0);
                k = k + 1;
            }
        }

        // 命中横幅（谁打中了谁）
        if (state == ST_BOOM && hitBy >= 0 && boomT <= BOOM_TICKS)
        {
            if (hitBy == 0) { ui_text(sw / 2, gy / 2, "橙猴命中！", C_APE0, 26, VML_ANCHOR_CENTER); }
            else { ui_text(sw / 2, gy / 2, "紫猴命中！", C_APE1, 26, VML_ANCHOR_CENTER); }
        }

        hud.Draw(&(*AP[0]), &(*AP[1]), turn, sw);
        wind.Draw(sw, 44);
        DrawAimPanel();

        if (over != 0)
        {
            // 横幅加高到 100：被飞碟清场时要放**两行**（怎么输的 + 下次别这么干）
            ui_rect(0, sh / 2 - 50, sw, 100, 0xE0101020, 1, 0, 0);
            if (overKind == 1)
            {
                // ⚠ 这一局的胜负**与分数无关**，所以绝不能落到下面"比分数"那几支去 ——
                //   否则一只猴子被激光打死，屏幕上却在报"紫猴获胜！"，玩家一头雾水。
                ui_text(sw / 2, sh / 2 - 18, "飞碟清场！", 0xFFFF6060, 26, VML_ANCHOR_CENTER);
                ui_text(sw / 2, sh / 2 + 10, "别打飞碟 —— 它会记住你", C_TEXT_DIM, 14, VML_ANCHOR_CENTER);
            }
            else if ((*AP[0]).score > (*AP[1]).score)
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
            ui_text(sw / 2, sh / 2 + 36, "点一下屏幕退出", C_TEXT_DIM, 13, VML_ANCHOR_CENTER);
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
    int needDraw;            // 这一轮要不要重绘（见主循环里那条说明）
    int msg[4];
    int score0;
    int score1;
    // 上一拍物理 / 游戏时钟的 `ui_tick()` 时刻 —— 用来"按真实流逝时间补拍"，
    // 让物理与时钟**与帧率解耦**（见主循环 `VML_MSG_TIMER` 那两支的长注释）。
    int lastPhysMs;
    int lastSfxMs;           // 音效音序器的上一拍时刻（见主循环里那段说明）
    int lastClockMs;

    // 把静态初值的 `gHour/gMinute`（开局 7:30）落进毫秒真源 —— **全程序只此一处换算**。
    // 两处各写一遍就是"改一处忘一处"，而症状是"钟面上 7:30、天却已经大亮"。
    gMsOfDay = (gHour * 60 + gMinute) * 60000;

    g.Layout();
    // **全触摸**：不要屏幕手柄区（`VML_WIN_NO_GAMEPAD`）—— 手柄区连折叠条一起吃画布
    // 高度，这个游戏只用手指，没必要为它留一条；顺带锁竖屏（版面按竖屏排）。
    ui_win_open_ex("大猩猩扔香蕉 (C++)", g.sw, g.sh, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_keep_on(1);
    // ⚠ **必须在开窗之后**：图块是宿主那边的资源，没有场景就建不起来
    //   （`ui_create_block` 会返回 0，之后整局都只能用"退化的简版形状"兜底）。
    // ⚠ 而且**只建这一次**：块表不随 `ui_clear` 清，每帧建一遍会在 128 帧后拿不到句柄。
    g.MakeBlocks();
    g.NewTurn();

    lastPhysMs = ui_tick();
    lastClockMs = lastPhysMs;
    lastSfxMs = lastPhysMs;
    SfxReset();              // 表清干净（静态区本来就是空的，这里只为"重开一局"这条将来留一个入口）

    tid = ui_timer_set(TICK_MS, 0);
    // 游戏时钟：**另一只定时器**，1 真实秒 = 1 游戏分钟（24 真实分钟走完一天）。
    // 为什么不拿物理节拍那只数帧：玩家拖动滑条时主循环会一次抽干几十条 TOUCHMOVE，
    // 帧率完全取决于输入有多密 —— 数帧的话"滑得越勤、时钟跑得越快"。
    //
    // ⚠ 周期是 `CLOCK_MS`（50ms）而**不是 1000ms**：时钟的**语义**没变（1 秒照走
    //   1 游戏分钟），变的只是**推进的粒度** —— 见 `gMsOfDay` 的说明。
    //   1000ms 一跳的话，云和日月的位置每秒才更新一次，看着就是一顿一顿的。
    cid = ui_timer_set(CLOCK_MS, 0);
    done = 0;
    needDraw = 1;            // 第一帧总得画

    while (done == 0 && ui_win_closed() == 0)
    {
        // ── 音效音序器：**按真实流逝时间**推进，与物理同一套理由 ──────────
        //
        // ⚠⚠ **不能挂在物理节拍（`g.Tick()`）里**，这是本版最容易踩的一处：
        //   一局结束的**那一刻**主循环就把物理定时器 `ui_timer_kill(tid)` 了
        //   （见下面"结束"那一支），而胜负音正要在这时候开始放 ——
        //   挂在物理上，`Tick()` 从此不再被调 ⇒ 那串音**只会响出第一个音**，
        //   后面几个永远等不到"下一拍"。症状是"赢了只听到一声"，很容易被当成
        //   "音效没做"而不是"驱动源选错了"。
        //   挂在这儿则一路活到程序退出（时钟定时器 `cid` 还在，消息循环照转）。
        //
        // ⚠ 上限 4 拍、且**推进后把 `lastSfxMs` 对齐到真实时间**（不是 `+want*TICK_MS`）：
        //   与物理那段同一个理由 —— 卡顿一下不该让音效"补跑"一串回来，
        //   但也不能把欠账一直记着（那会让音序永久偏快）。
        {
            int sNow;
            int sWant;
            sNow = ui_tick();
            sWant = (sNow - lastSfxMs) / TICK_MS;
            if (sWant > 4) { sWant = 4; }
            if (sWant > 0)
            {
                lastSfxMs = sNow;
                while (sWant > 0)
                {
                    SfxTick();
                    sWant = sWant - 1;
                }
            }
        }

        // 重绘由**变化**驱动，不由**节拍**驱动（本仓的一条老规矩）。
        // ⚠ 原来这里是无条件 `g.Draw()`：主循环每收到**一条**消息就画一帧，而
        //   时钟定时器 50ms 一条 ⇒ 凭空多出 20 帧/秒，内容一模一样。
        //   实测帧率因此从 30 涨到 50 —— 多出来的全是空转（手机上就是白耗电）。
        if (needDraw != 0)
        {
            g.Draw();
            needDraw = 0;
        }

        t = ui_wait_msg(0);

    handle_msg:
        // 默认**每一轮都重绘**，只有"纯时钟推进"那一支把它按下去。
        // ⚠ 写反了（默认不画、白名单里才画）就会在将来加消息类型时**静默漏掉重绘** ——
        //   "界面不刷新"是最难查的一类症状，而这里的代价只是多画几帧。
        needDraw = 1;

        if (t == VML_MSG_WINDOWCLOSE) { done = 1; }

        if (t == VML_MSG_TIMER)
        {
            // ⚠ **必须按定时器 id 分流**：现在有两只（物理节拍 33ms、游戏时钟 50ms），
            //   不分的话两只都会走对方的逻辑 —— 时钟按 33ms 飞奔、物理按 50ms 一跳。
            //   消息 A 就是定时器 id。
            if (ui_msg_a() == cid)
            {
                // ⚠ 时钟**只推进时间、不请求重绘**：云和日月的位置是按 `gMsOfDay`
                //   现算的，下一拍物理帧自然会用上最新的时间。
                //   在这里补一帧的话，画出来的和上一帧没有区别（时间才走了 50ms，
                //   云也才挪了 0.1px）—— 纯浪费。
                // ⚠ 游戏时钟**同样按真实经过时间补**（理由见下面物理那一支）：
                //   清空时钟类消息之后这一支会变稀，再固定推进 `CLOCK_MS` 的话，
                //   游戏里的一天会被拉长（时钟挂在帧率上了）。上限同样 8 拍。
                {
                    int dt;
                    dt = ui_tick() - lastClockMs;
                    if (dt < 1) { dt = CLOCK_MS; }
                    if (dt > CLOCK_MS * 8) { dt = CLOCK_MS * 8; }
                    clockAdvance(dt);
                    lastClockMs = ui_tick();
                }
                needDraw = 0;
            }
            else
            {
                // ⚠⚠ **按真实经过时间补拍**，而不是"收到一条跑一拍"（v0.96.478）。
                //
                //   定时器消息的语义是"**该推进了**"，它是**按时间**产生的（每 `TICK_MS` 一条）。
                //   而主循环一帧才消费一条 ⇒ 积压时物理每帧只跑一拍、**速度挂在帧率上**
                //   （实测积压到 2280 条时，物理每秒只走 20 拍而不是 30 拍 —— 游戏在偷偷变慢，
                //   手感发黏）。上面那句 `ui_msg_drop(VML_MSG_KIND_TIMER)` 把积压清掉是对的，
                //   但**光清不补就是另一种坏**：物理会彻底变成"一帧一拍"。
                //   ⇒ **清空 + 按 `ui_tick()` 的真实流逝时间补拍**，两者缺一不可。
                //
                //   ⚠ 上限 8 拍：真机上偶尔会有几百毫秒的卡顿（GC / 调度 / 切后台），
                //   不设上限的话恢复那一帧会一口气跑几十拍 —— 香蕉瞬移、直接穿过楼房。
                //   "宁可慢这一下，不要瞬移"。
                {
                    int now;
                    int want;
                    now = ui_tick();
                    want = (now - lastPhysMs) / TICK_MS;
                    if (want < 1) { want = 1; }
                    if (want > 8) { want = 8; }
                    while (want > 0)
                    {
                        g.Tick();
                        if (g.over != 0)
                        {
                            ui_timer_kill(tid);
                            tid = 0;
                            break;
                        }
                        want = want - 1;
                    }
                    lastPhysMs = ui_tick();
                }
            }

            // ⚠⚠ **读到时钟就立刻清空时钟类消息**（v0.96.478，用户提的做法）。
            //
            //   定时器消息是"**到点了**"的通知 —— **中间那些已经过期了**，一条都没用：
            //   物理该走几拍、时钟该走多久，上面都按 `ui_tick()` 的真实流逝补齐了。
            //   留着它们只有坏处：队列越积越长，触摸、键盘全排在后面（实测积压到 2280 条）。
            //
            //   ⚠ **必须"先补拍、再清"** —— 反过来就是把"物理变慢"换成"物理跳帧"。
            //   这一对（清空 + 按真实时间补）合起来才等价于"定时器每 TICK_MS 响一次"，
            //   而且**与帧率彻底解耦**：帧率掉到 10fps 物理照样按 30 拍/秒走。
            //   ⚠ 只清 `TIMER` 类：键盘 / 触摸是**离散语义**，丢一条就少一次事件
            //   （见 `ui_msg_drop` 的说明）。
            ui_msg_drop(VML_MSG_KIND_TIMER);
        }

        if (t == VML_MSG_KEYDOWN) { g.KeyDown(ui_msg_a()); }
        if (t == VML_MSG_KEYUP) { g.KeyUp(ui_msg_a()); }

        // 触摸 / 鼠标 —— **与 BASIC 版同一套**：拖条调值、点按钮发射。
        // 移动事件（TOUCHMOVE）也走同一条，所以按住条一路拖就一直跟手。
        if (t == VML_MSG_TOUCHDOWN || t == VML_MSG_MOUSEDOWN) { g.HandlePoint(1); }
        if (t == VML_MSG_TOUCHMOVE || t == VML_MSG_MOUSEMOVE)
        {
            // ⚠⚠ **丢掉已经排队、但已经过期的移动事件**（v0.96.478）。
            //
            //   移动是**"追最新位置"**的语义 —— 用户要的是手指**现在**在哪，中间那几百个
            //   位置一个都没用。而主循环一次只取一条 ⇒ 拖动时产生的比消费的快，队列只涨不落：
            //   实测连续拖滑条 6 轮，队列 285 → 596 → 1050 → 1481 → 2010 → **2280** 条，
            //   **完全不回落**，而同一时间 fps 全程 19~21。
            //   于是出现用户报的那种「**背景绘图不卡、但触摸要等一下才反应**」——
            //   他的触摸事件排在那两千多条移动事件后面。
            //
            //   ⚠ 用"丢一类"而**不是** `ui_msg_clear`：清空会把同时排着的键盘、定时器
            //   一起扔掉 —— 那两样是**离散语义**，少一条就是"按键丢了/物理卡了一拍"。
            //   ⚠ 丢掉的是**已经排队**的，当前这条（刚取出来的）照常处理，所以
            //   "跟手"这件事一点不受影响：手指最后停在哪，程序拿到的就是哪。
            ui_msg_drop(VML_MSG_KIND_TOUCH);
            g.HandlePoint(0);
        }

        if (g.over == 2) { done = 1; }      // 结束画面被点了一下

        // ⚠⚠ **把已经排队的消息一次抽干**（v0.96.478，治"越玩越卡"的**主力**）。
        //
        //   上面 `ui_wait_msg(0)` **一次只取一条**，而消息的**产生速度**是：
        //     · 定时器：物理 33ms + 时钟 50ms ⇒ **每秒 ~50 条**
        //     · 触摸拖动：手指一秒几百条 TOUCHMOVE
        //   而主循环**一帧才消费一条**（画一帧 ~50ms）⇒ **每秒只消费 ~20 条**
        //   ⇒ **每秒净积压 30 条以上**。真机实测（连续拖滑条 6 轮）：
        //   队列 285 → 596 → 1050 → 1481 → 2010 → **2280**，**完全不回落**。
        //
        //   后果有两层，**第二层才是要命的**：
        //     ① 触摸事件排在那几千条后面 ⇒ 「**触摸要等一下才反应**」；
        //     ② **定时器消息也被积压** ⇒ `g.Tick()` 每秒只跑 20 次而不是 30 次
        //        ⇒ **物理在偷偷变慢**（香蕉飞得比设计慢、手感发黏）。
        //   ⚠ 所以这里**不能**改用"丢定时器"来省事 —— 丢一条就少一拍物理，
        //     那是把"变慢"换成"跳帧"。抽干才是对的：让该跑的每一拍都跑到。
        //
        //   ⚠ 判据用 `ui_msg_count()` 而**不是**再调一次 `ui_wait_msg(0)` ——
        //   后者没消息时会**阻塞**，把主循环变成空转（手机上是白耗电）；
        //   也**不能**把 `ui_wait_msg(0)` 直接换成 `ui_poll_msg()`：那会让
        //   "没消息时让出 CPU"消失，主循环变成 100% 占用的忙等。
        //   ⚠ 用 `goto` 而不是把这一大段抽成函数：处理体要用 `t/done/needDraw` 这些
        //   主循环局部量，抽函数得把它们全改成全局 —— 那才是真正的隐患来源。
        if (done == 0 && ui_msg_count() > 0)
        {
            t = ui_poll_msg();
            goto handle_msg;
        }
    }

    if (tid != 0) { ui_timer_kill(tid); }
    if (cid != 0) { ui_timer_kill(cid); }

    // ⚠ **退出前必须静音**：胜负音还在响的时候玩家就点了退出，声部会一直响下去
    //   （进程没了才停）。桌面上表现为"窗口关了还有声音"，手机上更明显 ——
    //   切回桌面还在响。
    SfxPanic();

    // 比分落盘（下次开局问不出来，但先存着 —— 与 BASIC 版同一套键）
    score0 = (*AP[0]).score;
    score1 = (*AP[1]).score;
    ui_store_set("gorilla.hpp.score0", numstr(score0));
    ui_store_set("gorilla.hpp.score1", numstr(score1));

    ui_keep_on(0);
    return 0;
}
