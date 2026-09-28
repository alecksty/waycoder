// 大猩猩扔香蕉 —— 用 **C++ 的类**写
// Gorilla throwing bananas — written with **C++ classes**
//
// 这个例子的定位与同目录的 `snake.cpp` 不同：那一个是"能用就行"的写法（状态全塞进
// This example has a different role from `snake.cpp` in the same directory: that one is
// 一个文件级数组、辅助函数一律无参）。**这个例子是拿来压 C++ 前端的类支持的** ——
// "good enough to work" code (all state crammed into one file-level array, helper functions all
// 用户的原话是「尽量使用类对象，刚好测试类的功能」，所以这里刻意把每一样东西
// taking no arguments). **This example is here to stress the C++ front end's class support** —
// 都做成对象：天空、楼房、猴子、香蕉、风、状态栏、以及总控。
// the user's words were "use class objects as much as possible, it happens to test classes", so everything here is deliberately
//
// 用到并因此被验证的类特性：
// Class features used here and therefore verified:
//   · 构造 / 析构以外的一切：字段、成员方法、`this->`、隐式字段名
//   · everything except constructors/destructors: fields, member methods, `this->`, implicit field names
//   · **多参构造函数**（`Ape(0)` / `Building(px, pw, ph, gy)`）
//   · **multi-parameter constructors** (`Ape(0)` / `Building(px, pw, ph, gy)`)
//   · **继承**（`Ape`/`Building`/`Banana`/`Sky` 都派生自 `Entity`）+ **虚函数派发**
//   · **inheritance** (`Ape`/`Building`/`Banana`/`Sky` all derive from `Entity`) + **virtual dispatch**
//   · **基类指针数组**（`Entity* actors[8]`）---- 画图时多态派发到各自的 `Draw()`
//   · **an array of base-class pointers** (`Entity* actors[8]`) ---- drawing dispatches polymorphically to each `Draw()`
//   · 对象数组、对象里嵌对象、对象互相调用、复合赋值与自增
//   · object arrays, objects nested inside objects, objects calling each other, compound assignment and increment
//
// ## 前端的四条限制 —— **已经全部修好了**，但这个文件**暂时仍按老写法**
// ## The front end's four limitations — **all fixed now**, but this file **still uses the old style for the moment**
//
// 这四条原先是真的写不出来（下面括号里是当年的报错），现在都能写了：
// These four really were impossible to write back then (the parentheses hold the error of the day); now all of them work:
//
//   ① **类里不能有数组字段** ⇒ 现在能了（`int data[4];` 正常，见 `KNOWN_DEFECTS.md` 的 F25）。
//   ① **a class could not have array fields** ⇒ now it can (`int data[4];` works, see F25 in `KNOWN_DEFECTS.md`).
//      本文件的尾迹仍在**文件级数组**（`trailX/trailY`），因为整条链上还有一小截没通：
//      This file's trail is still a **file-level array** (`trailX/trailY`), because one small link in the chain is still broken:
//      **成员数组在类内部用隐式 `this` 做下标读**（`trailX[i]` 而不是 `b.trailX[i]`）
//      **reading a member array by index inside the class through an implicit `this`** (`trailX[i]` instead of `b.trailX[i]`)
//      算出来的地址不对（`d30.cpp`：求和 143 应为 99，且输出会断）。等那一截修好再收回来。
//      computes the wrong address (`d30.cpp`: the sum should be 99 but comes out 143, and the output gets cut off). Fold it back once that link is fixed.
//   ② **方法不能返回指针类型** ⇒ 现在能了（F26）。本文件没有需要返回指针的地方，维持原样。
//   ② **a method could not return a pointer type** ⇒ now it can (F26). Nothing in this file needs to return a pointer, so it stays as it was.
//   ③ **`cout << 对象的字段` 打不出东西** ⇒ 现在能了（F22/F30）。本文件里凡是要印的
//   ③ **`cout << a field of an object` printed nothing** ⇒ now it works (F22/F30). Everything printed in this file
//      仍习惯性先存进局部量 —— 那是无害的写法，不是缺陷绕过。
//      is still habitually stored into a local first — that is a harmless style, not a workaround for a defect.
//   ④ **临时对象赋值**（`a0 = Ape(1, 2);`）不生效 ⇒ 现在能了（F31，且带默认参数补齐 F28）。
//   ④ **assignment from a temporary object** (`a0 = Ape(1, 2);`) had no effect ⇒ now it works (F31, with default-argument completion F28).
//      本文件仍用 `Setup(...)` / `Launch(...)` 这类成员方法改状态：对"改一个已有对象的
//      This file still uses member methods like `Setup(...)` / `Launch(...)` to change state: for "change several
//      若干字段"来说，成员方法本来就更直白。
//      fields of an existing object", a member method is simply more direct anyway.
//
// ⚠ **别照着这几条去写新程序** —— 新代码该用什么用什么；这份注释的作用是
// ⚠ **Do not write new programs by following these points** — new code should use whatever it needs; the purpose of this comment is
//   让人知道"这段老写法不是风格选择、也不是不能改，而是**改之前先跑一遍**"。
//   to make it known that "this old style is neither a style choice nor something that cannot be changed, but **run it once before you change it**".
//   完整台账（哪些修了、哪些还开着、怎么复现）见
//   The full ledger (what is fixed, what is still open, how to reproduce each) is in
//   `VMLPrepares/CppCompiler/KNOWN_DEFECTS.md`。
//   `VMLPrepares/CppCompiler/KNOWN_DEFECTS.md`.
//
// ## 操作
// ## Controls
//
//   拖「角度」条调仰角、拖「力度」条调力度、点左边的「发 射」按钮出香蕉 ——
//   Drag the "Angle" bar to set the elevation, drag the "Power" bar to set the power, tap the "FIRE" button on the left to launch a banana —
//   手指点到条的哪一格就是哪个值，**不需要键盘**（与 `basic/gorilla_pro.bas` 同一套）。
//   whichever cell of the bar your finger lands on is the value you get, **no keyboard needed** (the same scheme as `basic/gorilla_pro.bas`).
//   开窗声明了 `VML_WIN_NO_GAMEPAD`（不要手柄区 ⇒ 画布吃满整屏）与 `VML_WIN_PORTRAIT`。
//   The window declares `VML_WIN_NO_GAMEPAD` (no gamepad area ⇒ the canvas fills the whole screen) and `VML_WIN_PORTRAIT`.
//   键盘**不是必需品**，接物理键盘时仍可用：←→ 角度、↑↓ 力度、回车/空格/A 发射、ESC 退出。
//   The keyboard is **not required**, but still works with a physical one: ←→ angle, ↑↓ power, Enter/Space/A fire, ESC quit.
//   窗口被关、或结束画面上点一下，也会退出。
//   Closing the window, or tapping once on the end screen, also quits.
//
// ## 跑法
// ## How to run
//
//   手机：`vml run examples/cpp/gorilla.cpp`
//   Phone: `vml run examples/cpp/gorilla.cpp`
//   桌面：`vmlcli Examples/cpp/gorilla.cpp --frames /tmp/fr`
//   Desktop: `vmlcli Examples/cpp/gorilla.cpp --frames /tmp/fr`

#include <waycoder_ui.h>

// ════════════════════════════════════════════════════════════════════
// 定点数学
// Fixed-point math
//
// 全程**整数**，不碰 float/double：这个 VM 的浮点是另一套寄存器（F/D），
// **Integers** throughout, never touching float/double: this VM's floating point lives in a separate register bank (F/D),
// 而弹道只需要 1/256 像素的精度，整数定点足够而且没有取整漂移。
// while the trajectory only needs 1/256-pixel precision; integer fixed point is enough and has no rounding drift.
//
//   位置与速度的单位都是 **1/256 像素**（下称"定点单位"）。
//   Positions and velocities are both in **1/256 pixel** units (called "fixed-point units" below).
//   画的时候 `bx / FP` 就是像素 —— 除法在**绘制时**做一次，物理里一次不做。
//   When drawing, `bx / FP` gives pixels — the division happens **once at draw time**, and never inside the physics.
// ════════════════════════════════════════════════════════════════════

#define FP       256      // 定点标度：256 = 1 像素
// Fixed-point scale: 256 = 1 pixel
#define GRAV     142      // 每拍重力增量（定点单位）
// Gravity increment per tick (fixed-point units)
#define V_UNIT   43       // 力度 1 点 = 43 定点单位/拍（力度 100 → 4300）
// Power: 1 point = 43 fixed-point units per tick (power 100 → 4300)
#define TICK_MS  33       // 物理节拍（≈30fps）
// Physics tick (≈30fps)
#define CLOCK_MS 50       // 游戏时钟的推进周期 —— **不是** 1000，见 `gMsOfDay` 的说明
// How often the game clock advances — **not** 1000, see the explanation of `gMsOfDay`
#define SIN_SCALE 1000    // 正弦表的值域：1000 = 1.0
// Range of the sine table: 1000 = 1.0

#define MAX_TRAIL 96      // 尾迹点数上限
// Maximum number of trail points

// 先拿满这么多分的人赢
// Whoever reaches this score first wins
#define WIN_SCORE 5

// 回合状态
// Turn state
#define ST_AIM   0
#define ST_FLY   1
#define ST_BOOM  2        // 爆炸（短促的一下，见 `BOOM_TICKS`）
// Explosion (a short one, see `BOOM_TICKS`)

/// 爆炸持续几拍（一拍 = `TICK_MS` = 33ms ⇒ 12 拍 ≈ 0.4 秒）。
/// How many ticks the explosion lasts (one tick = `TICK_MS` = 33ms ⇒ 12 ticks ≈ 0.4 seconds).
///
/// ⚠ **它同时决定火球能长多大**（`r = 6 + boomT * 3`）—— 缩时间就是缩大小，
/// ⚠ **It also decides how big the fireball can grow** (`r = 6 + boomT * 3`) — shrinking the time shrinks the size;
///   这两件事本来就是同一个数。别在绘制那边另写一个上限：那会出现
///   the two really are the same number. Do not write a second cap on the drawing side: that would produce
///   "时间到了球还在长"或者"球长满了还停着"这类对不上的中间态。
///   mismatched intermediate states like "the time is up but the ball is still growing" or "the ball is full yet still sitting there".
/// ⚠ 原来 24（≈0.8 秒、最大直径 176px），玩家报「圆圈有点大、时间有点长」；
/// ⚠ It used to be 24 (≈0.8 seconds, max diameter 176px) and the player reported "the circle is a bit big and takes a bit long";
///   减半到 12 ⇒ 0.4 秒、最大直径约 104px。外圈多出来的那 10px 与碎屑的 14px
///   halved to 12 ⇒ 0.4 seconds, max diameter about 104px. The outer ring's extra 10px and the debris' 14px
///   也一并减半（`+5` / `+7`）—— 半径减半了、这两个偏移不减，外圈占的比例就会变形。
///   are halved along with it (`+5` / `+7`) — halve the radius but leave these two offsets alone and the outer ring's share distorts.
#define BOOM_TICKS  12

// ════════════════════════════════════════════════════════════════════
// 整数三角函数（Bhaskara I 近似）
// Integer trigonometry (Bhaskara I approximation)
//
//   sin(a°) ≈ 4a(180−a) / (40500 − a(180−a))     —— 0..180 度内误差 < 0.0016
//   sin(a°) ≈ 4a(180−a) / (40500 − a(180−a))     — error < 0.0016 over 0..180 degrees
//
// 用近似而不是查表：表要能手写 91 项（写错一项就是"某个角度永远打不中"，
// An approximation instead of a lookup table: a table means hand-writing 91 entries (one typo means "that angle never hits,
// 而且看不出来），这公式三行就完了，精度对弹道绰绰有余。
// and you cannot see it"), while this formula is three lines and its precision is more than enough for the trajectory.
// 全程整数 ⇒ 定点标度 1000。
// Integers throughout ⇒ fixed-point scale 1000.
// ════════════════════════════════════════════════════════════════════

int isin(int deg)
{
    int u;
    if (deg < 0) { deg = -deg; }
    deg = deg % 360;
    if (deg > 180) { deg = deg - 180; }      // sin(x) = sin(180-x)，先折到 0..180
    // sin(x) = sin(180-x); fold into 0..180 first
    u = deg * (180 - deg);                   // 0 .. 8100
    return 4 * u * SIN_SCALE / (40500 - u);
}

int icos(int deg)
{
    return isin(90 - deg);                   // cos(x) = sin(90-x)
}

/// **亚度**的 sin：`mdeg` 是**千分之一度**（毫度）。
/// sin at **sub-degree** resolution: `mdeg` is **thousandths of a degree** (milli-degrees).
///
/// ⚠ 为什么需要它：`isin` 的粒度是 **1 度**，而日月是"跟时间连续走"的 ——
/// ⚠ Why it is needed: `isin`'s granularity is **1 degree**, while the sun and moon "move continuously with time" —
///   弧顶附近 1 度 ≈ 4px 的纵向位移，于是太阳会**每隔几分钟往上弹一下**
///   near the top of the arc 1 degree ≈ 4px of vertical travel, so the sun **jumps up every few minutes**
///   （玩家报的"太阳能不能平滑移动"就是这个）。
///   (this is exactly the "can the sun move smoothly" the player reported).
///   粒度的锅补不了，只能把角度本身做细。
///   The granularity itself cannot be patched; only the angle itself can be made finer.
///
/// 做法是**在相邻两个整数度之间线性插值**（不是重推 Bhaskara）：
/// The approach is **linear interpolation between two adjacent integer degrees** (not a re-derived Bhaskara):
/// 一个 1 度区间内 sin 的二阶误差约 `(π/180)²/8 ≈ 0.00015`（相对值），
/// over a 1-degree interval sin's second-order error is about `(π/180)²/8 ≈ 0.00015` (relative),
/// 比 Bhaskara 自身的误差还小 —— 而它**不会溢出**：Bhaskara 把输入放大 1000 倍后
/// smaller than Bhaskara's own error — and it **does not overflow**: Bhaskara, after scaling the input by 1000,
/// 分子 `4·u·1000` 会冲到 3×10⁹ 撞破 32 位，插值版最大只有几千。
/// pushes the numerator `4·u·1000` to 3×10⁹ and breaks 32 bits, while the interpolated version peaks at a few thousand.
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
// Integer → string (no std::string; `sprintf` differs across front ends)
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
// Banana trail. See point ① in the file header: **a class can hold arrays now**, but "reading a member array
// 做下标读"那一截还没通（`d30.cpp`），所以尾迹暂时仍在文件级。
// by index inside the class with an implicit `this`" is still not working (`d30.cpp`), so the trail stays at file level for now.
// ════════════════════════════════════════════════════════════════════

static int trailX[MAX_TRAIL];
static int trailY[MAX_TRAIL];

// 按序号访问具名字段的指针表（见 `Game` 里那段注释：那里的写法是历史原因，不是限制）
// Pointer tables for accessing named fields by index (see the comment in `Game`: that style is historical, not a limitation)
static Building* BL[8];
// 本局的**实际栋数**（4~8，每局随机，见 `Game::Layout`）。
// This round's **actual building count** (4..8, random each round, see `Game::Layout`).
// 放文件级是因为 `HoleRadiusAt` 这个自由函数也要遍历楼 —— 它是按序号表 `BL` 走的。
// It lives at file level because the free function `HoleRadiusAt` also walks the buildings — it goes through the index table `BL`.
static int gBldgN;
static Ape* AP[2];
static Tree TREES[8];        // 地上的树（数量/高矮/位置都随机，见 Game::Layout）
// Trees on the ground (count/height/position all random, see `Game::Layout`)
static Flyer* FLY[3];        // 鸟 / 飞碟 / 飞机 —— 三个派生类共用基类指针
// Bird / UFO / plane — three derived classes sharing the base-class pointer

// ════════════════════════════════════════════════════════════════════
// 弹坑 —— **可破坏地形**
// Craters — **destructible terrain**
//
// 香蕉撞上楼或落地就在那儿啃掉一块：把那一小块**涂回当地的天空色**。
// When a banana hits a building or the ground it bites a piece out right there: that patch is **painted back with the local sky colour**.
// 因为是"涂"而不是"改楼房的数据"，所以楼房本身完全不用动 —— 楼照旧是一次画好的矩形，
// Because it "paints" rather than "changes the building's data", the building itself never has to move — it is still one rectangle drawn once,
// 坑只是画在它**之后**、**地面之前**的一层覆盖。
// and the crater is just an overlay drawn **after** it and **before** the ground.
//
// ⚠ 顺序不能变（`Game::Draw` 里那三层的次序是有讲究的）：
// ⚠ The order must not change (the order of those three layers in `Game::Draw` is deliberate):
//   · 必须在**所有楼之后** —— 坑可能横跨两栋的交界，画在前面会被后一栋盖掉；
//   · it must come **after all buildings** — a crater can straddle the boundary of two, and drawn earlier it would be covered by the next one;
//   · 必须在**地面之前** —— 否则会把地面也啃掉一块（地上的坑本来就该被地面盖住）。
//   · it must come **before the ground** — otherwise it would bite a piece out of the ground too (a ground crater is supposed to be covered by the ground).
//
// ⚠ "涂回天空色"必须问一句**哪个 y 的天空色**：天空是**渐变**的，
// ⚠ "Paint back the sky colour" must first ask **which y's sky colour**: the sky is a **gradient**,
//   写死一个颜色会在楼中间留下一块色不对的圆 —— 而且只在某些时段看得出来。
//   and hard-coding one colour leaves a circle of the wrong colour in the middle of a building — visible only during certain hours.
//════════════════════════════════════════════════════════════════════

// ── 云 / 路灯 / 树的**图块**（开窗时录、每帧贴一次）──────────────────────
// **Blocks** for clouds / street lamps / trees (recorded when the window opens, stamped once per frame)
//
// ⚠ 它们的颜色**随昼夜连续变**，而图块里的颜色是**录制那一刻定死的** ⇒ 按天光
// ⚠ Their colours **change continuously with the day/night cycle**, while the colours inside a block are **fixed at the moment of recording** ⇒ record
//   **分档录**（`VML_DAY_STEPS` 档），贴的时候按当前天光选最近的一档。
//   **one block per step** (`VML_DAY_STEPS` steps), and when stamping pick the nearest step to the current daylight.
//   24 真实分钟走完一天 ⇒ 每档约 1.5 分钟，档位之间的颜色差看不出来。
//   24 real minutes make a full day ⇒ about 1.5 minutes per step, and the colour difference between steps is invisible.
// ⚠ 声明必须放在**最前面**：这个前端不做前向引用，`lampsDraw` 在 442 行就用到了。
// ⚠ The declaration must come **first**: this front end does no forward references, and `lampsDraw` uses it on line 442.
#define VML_DAY_STEPS 16
static int cloudBid[VML_DAY_STEPS];
static int lampBid[VML_DAY_STEPS];
static int treeBid[3];           // 树的绿色**本来就是三档固定色** ⇒ 不需要分昼夜
// The trees' green **is three fixed colours by construction** ⇒ no day/night stepping needed

#define MAX_HOLE 24

static int holeX[MAX_HOLE];
static int holeY[MAX_HOLE];
static int holeR[MAX_HOLE];
static int holeN;

// 画蒙版用的**临时**洞表：钳制 + 合并之后的圆（见 `Game::Draw` 里那段说明）。
// The **temporary** hole table used for the mask: the circles after clamping + merging (see the explanation in `Game::Draw`).
static int mhx[MAX_HOLE];
static int mhy[MAX_HOLE];
static int mhr[MAX_HOLE];

void clearHoles()
{
    holeN = 0;
}

// 记一个坑。满了就丢掉新的 —— **不覆盖旧的**：覆盖会让地形在打到第 25 发时
// Record one crater. When full, drop the new one — **do not overwrite the old ones**: overwriting would make the terrain
// 突然"愈合"，而那一下正好发生在玩家已经建立空间感之后。宁可不画新的。
// suddenly "heal" on the 25th shot, and that would happen right after the player has built up a sense of space. Better not to draw the new one.
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
// Colours (0xAARRGGBB; the VM's int is signed, so anything above 0x7FFFFFFF is written as a negative decimal)
// ════════════════════════════════════════════════════════════════════

#define C_SKY_DAY1  0xFF3A78C8
#define C_SKY_DAY2  0xFFA8D4F0
#define C_SKY_NIT1  0xFF0A1230
#define C_SKY_NIT2  0xFF243056
#define C_STAR      0xFFFFFFFF
#define C_SUN       0xFFFFE060
#define C_MOON      0xFFE8E8F0
// 楼体色 —— ⚠ 这四档原本是**暗棕/土黄/石板/藕紫**（0xFF6E5A46 一路下来），
// Building colours — ⚠ these four used to be **dark brown / ochre / slate / muddy purple** (starting from 0xFF6E5A46 all the way down),
//   白天拉满也只有 RGB(110,90,70)，配上夜景那就是一片糊。玩家报「画面不太好看」
//   reaching only RGB(110,90,70) even at full daylight, and against the night scene that was a mush. A big part of the player's "the picture is not pretty"
//   很大一半在这里：**天空是鲜的、楼是灰的**，整个画面就"脏"。
//   complaint is here: **the sky is vivid, the buildings are grey**, and the whole frame looks "dirty".
//   换成饱和度高的四色（与 BASIC 版那排彩色楼同一个路子）。改这里记得同步 `SetTint`。
//   Switched to four highly saturated colours (the same approach as the row of colourful buildings in the BASIC version). Remember to keep `SetTint` in sync when changing this.
#define C_BLDG_A    0xFFE0604E
#define C_BLDG_B    0xFF3FB59A
#define C_BLDG_C    0xFFE8B840
#define C_BLDG_D    0xFF9A6AE0
#define C_WIN_LIT   0xFFFFD070
#define C_WIN_DARK  0xFF2A2A38
#define C_GROUND    0xFF3C5A34
#define C_DIRT      0xFF2A1E14   // 地上的弹坑挖出来的土色（**不是天空色**，见 holesDraw）
// The dirt colour dug out by a ground crater (**not the sky colour**, see `holesDraw`)
#define C_APE0      0xFFE08A3C
#define C_APE1      0xFFC758D6
#define C_APE_DARK  0xFF3A2A1A

/// 举起那只胳膊的长度（肩 → 手，像素）。
/// Length of the raised arm (shoulder → hand, pixels).
/// **画手臂与算香蕉出手点共用这一个数** —— 见 `Ape::ArmRootX` 上面的说明。
/// **Drawing the arm and computing the banana's launch point share this one number** — see the explanation above `Ape::ArmRootX`.
///
/// ⚠ 这个数试过三档：26 太短（手掌落在头的边缘，读成"手捂在脸上"）、
/// ⚠ Three values were tried: 26 is too short (the palm lands on the edge of the head and reads as "hand over the face"),
///   32 太长（手臂比躯干还长一大截，玩家报"太长，只要一半"）、**16** 正好 ——
///   32 is too long (the arm is much longer than the torso; the player said "too long, half of that is enough"), **16** is just right —
///   手臂加手掌 ≈ 23px，与躯干高（23）相当。肩点因此也往外挪了一格（13→14），
///   arm plus palm ≈ 23px, comparable to the torso height (23). The shoulder point therefore moved out one step too (13→14),
///   否则手掌离脸只剩 5px 又要贴上去。
///   otherwise the palm would be only 5px from the face and would stick to it again.
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
// Colour interpolation — the **only** entry point for colour in this whole file
//
//   两个颜色按百分比插值，结果打包成 0xAARRGGBB。
//   Two colours interpolated by percentage; the result is packed as 0xAARRGGBB.
//   分量各自算完再打包 —— **只打包、不解包**：`0xFF` 打头的颜色在 int 里是负数，
//   Each component is computed first and then packed — **pack only, never unpack**: a colour starting with 0xFF is negative in an int,
//   反解一个"颜色变量"要先处理符号，而"分量算完再打包"根本不需要反解。
//   so unpacking a "colour variable" would need sign handling first, while "compute components then pack" needs no unpacking at all.
//
// ⚠ 参数名一律**两个字母以上**：本仓实测「7 个参数 + 单字母参数名」会让**别的类**
// ⚠ Parameter names are always **two letters or more**: this repo measured that "7 parameters + single-letter parameter names" makes **other classes**
//   解析失败（解析期一个错都不报），见 `KNOWN_DEFECTS.md` 的 OPEN #17。
//   fail to parse (with no error at all during parsing), see OPEN #17 in `KNOWN_DEFECTS.md`.
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
// Colour components. **Outline colours are always derived from the body colour**, never hard-coded — see the explanation in `Ape::Draw`.
//
// ⚠⚠ **必须用位运算，不能用 `/` 和 `%`**：本平台的颜色值都带 alpha（`0xFFxxxxxx`），
// ⚠⚠ **Bit operations are mandatory; `/` and `%` must not be used**: this platform's colours all carry alpha (`0xFFxxxxxx`),
//   存进 `int` 是**负数** —— 实测 `0xFFFFFFFF` 就是 `-1`，而 C 的整数除法/取模是
//   stored in an int they are **negative** — 0xFFFFFFFF measured as -1 — and C's integer division/modulo
//   **向零截断**的 ⇒ `(-1 / 65536) % 256` 得 **0**、`-1 % 256` 得 **-1**，
//   **truncates toward zero** ⇒ `(-1 / 65536) % 256` gives **0** and `-1 % 256` gives **-1**,
//   分量全错。症状不是报错，是**画出一个谁也没指定的颜色**（云的底面被算成红褐色横线）。
//   so every component is wrong. The symptom is not an error but **a colour nobody specified** (a cloud's underside computed into a reddish-brown line).
//   位运算对正负都成立（算术/逻辑右移都行，因为后面 `& 0xFF` 会把高位切掉）。
//   Bit operations hold for both signs (arithmetic or logical shift both work, since the later `& 0xFF` cuts off the high bits).
int colR(int c) { return (c >> 16) & 255; }
int colG(int c) { return (c >> 8) & 255; }
int colB(int c) { return c & 255; }

// ════════════════════════════════════════════════════════════════════
// 落点该炸多大的坑：撞在楼身上大一些（打进墙里），砸在地上小一些。
// How big a crater an impact should make: bigger on a building body (it goes into the wall), smaller on the ground.
//
// ⚠ 判据与 `Game::Step` 里判 `hit` 用的是**同一个** `Covers` —— 两处各判一次的话，
// ⚠ The criterion is the **same** `Covers` used for `hit` in `Game::Step` — judging it separately in two places
//   "显示成撞楼、坑却按地面算"这种不一致迟早会出现。
//   would sooner or later produce inconsistencies like "it looks like it hit a building but the crater is computed as ground".
//
// ⚠ 这个函数曾经**只要存在就让画面全毁**（OPEN #19），当时只好改用固定半径。
// ⚠ This function once **wrecked the whole picture just by existing** (OPEN #19), and at the time we had to fall back to a fixed radius.
//   根因是编译器的一个缺陷（全局对象只分配 1 个 word，字段一写就踩到相邻全局量，
//   The root cause was a compiler defect (a global object was allocated only 1 word, so writing a field clobbered the adjacent global,
//   见 KNOWN_DEFECTS 的 F38）—— 修掉之后它就正常了。
//   see F38 in KNOWN_DEFECTS) — after that was fixed it behaved normally.
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
// Game clock — **a set of globals + free functions** (1 real second = 1 game minute)
//
// ## 为什么不是类
// ## Why it is not a class
//
// 一开始它是 `class Clock` + 一个全局对象 `static Clock gclk;`，结果**天空恒黑**：
// At first it was `class Clock` + a global object `static Clock gclk;`, and the result was **a permanently black sky**:
// `Compute` 里写 `zenR/G/B` 之后，外面读到的还是 0。查了一圈 —— 全局对象上调用方法
// after `Compute` wrote `zenR/G/B`, reads from outside still saw 0. After a round of investigation — calling methods on a global object
// 正常（`d44`）、方法内读自己的字段正常（`d45`/`d47`）、字段数多寡无关、
// works (`d44`), reading its own fields inside a method works (`d45`/`d47`), the number of fields is irrelevant,
// 参数重名无关、方法返回值当实参正常（`d48`）—— **根因没找到**（见 OPEN #18）。
// duplicate parameter names are irrelevant, and a method's return value used as an argument works (`d48`) — **the root cause was never found** (see OPEN #18).
//
// 用户拍板绕开：**没有类就没有 `this` 和字段偏移这整条链**。代价是这里的"状态"
// The user decided to route around it: **no class means no `this` and no field-offset chain at all**. The cost is that the "state" here is
// 是大写开头的全局量而不是对象字段，读起来糙一点；好处是它**确实能工作**。
// capitalised globals rather than object fields, which reads a bit rough; the benefit is that it **actually works**.
// ⚠ 等 OPEN #18 定位了，这一段可以收回成类 —— 那时别把上面的结论当"类不能用"。
// ⚠ Once OPEN #18 is pinned down this can go back to being a class — but do not read the above as "classes do not work".
//
// ## 时间怎么走
// ## How time advances
//
// 走**独立的一秒定时器**，不是"每画一帧加一点"：主循环在玩家拖动滑条时会一次
// It runs on a **separate one-second timer**, not "add a bit for every frame drawn": while the player drags a slider the main loop
// 抽干几十条 TOUCHMOVE，拿帧数当时钟会"滑得越勤、时钟跑得越快"。
// drains dozens of TOUCHMOVE messages at once, and using frame count as the clock would make "the more you slide, the faster the clock runs".
// ════════════════════════════════════════════════════════════════════

static int gHour = 7;        // 开局清晨 —— 一进来就是白天，玩家不用干等 —— 一进来就是白天，玩家不用干等
// Early morning at the start — you are in daylight as soon as you enter, the player does not have to wait
static int gMinute = 30;
/// 当天已过的**毫秒**（游戏时间）—— **平滑动画的唯一真源**。
/// **Milliseconds** elapsed today (game time) — **the single source of truth for smooth animation**.
///
/// ⚠ 为什么不能只靠 `gHour/gMinute`：它们的分辨率是**一分钟**，而 1 真实秒 = 1 游戏分钟
/// ⚠ Why `gHour/gMinute` alone are not enough: their resolution is **one minute**, and 1 real second = 1 game minute
///   ⇒ 任何"按分钟算位置"的东西都是**每秒跳一次**：
///   ⇒ anything that "computes position by the minute" **jumps once a second**:
///   · 云的速度是 2~4 px/分钟 ⇒ 每秒瞬移 2~4px（玩家报的"云层不平滑"就是这个）；
///   · clouds move at 2..4 px/minute ⇒ they teleport 2..4px every second (this is exactly the "the clouds are not smooth" the player reported);
///   · 日月的角度分辨率是 1 度 ⇒ 弧顶附近 1 度 ≈ 4px 的纵向位移，太阳会"每几分钟弹一下"。
///   · the sun/moon's angular resolution is 1 degree ⇒ near the top of the arc 1 degree ≈ 4px of vertical travel, so the sun "jumps every few minutes".
///   **位置要连续，时间本身就得是连续的。**
///   **For positions to be continuous, time itself has to be continuous.**
///
/// ⚠ `gHour/gMinute` 降级为**显示值**（HUD 上的时钟、流星的分钟判据、暖色窗口），
/// ⚠ `gHour/gMinute` are demoted to **display values** (the clock on the HUD, the meteor's minute criterion, the warm-colour window),
///   由 `clockAdvance` 从 `gMsOfDay` 推出来。**别在别处直接累加分钟** ——
///   derived by `clockAdvance` from `gMsOfDay`. **Do not accumulate minutes anywhere else** —
///   两个真源一旦不同步，就是"钟面上的时间和天色对不上"这类最难查的分叉。
///   once two sources of truth fall out of sync you get exactly the hardest kind of divergence to trace: "the clock face and the sky colour disagree".
///
/// ⚠ 初值在 `main` 里由 `gHour/gMinute` 算出来（**不在这里写死** —— 写死就是
/// ⚠ The initial value is computed in `main` from `gHour/gMinute` (**not hard-coded here** — hard-coding would mean
///   同一个"开局 7:30"写两遍，改一处忘一处）。
///   writing the same "starts at 7:30" twice, and changing one while forgetting the other).
static int gMsOfDay;
static int gLang;            // 界面语言：开局查一次（ui_get_language 是 syscall，别每帧调）
// UI language: looked up once at start (`ui_get_language` is a syscall, do not call it every frame)
static int gDayL;            // 天光 0..100（0 = 全黑、100 = 正午满亮）
// Daylight 0..100 (0 = fully dark, 100 = full midday brightness)
static int gWarm;            // 日出/日落的暖色系数 0..100（地平线偏橙）
// Sunrise/sunset warm factor 0..100 (the horizon leans orange)
static int gSunUp;
static int gMoonUp;
static int gSunX;
static int gSunY;
static int gMoonX;
static int gMoonY;
static int gZenR; static int gZenG; static int gZenB;    // 当前天顶色
// Current zenith colour
static int gHorR; static int gHorG; static int gHorB;    // 当前地平线色
// Current horizon colour
static int gMr; static int gMg; static int gMb;          // `clockPartsAt` 的输出
// Output of `clockPartsAt`

// 游戏时钟走 `ms` 毫秒（调用方按定时器周期给；见 `CLOCK_MS`）
// The game clock advances by `ms` milliseconds (the caller passes the timer period; see `CLOCK_MS`)
void clockAdvance(int ms)
{
    gMsOfDay = gMsOfDay + ms;
    if (gMsOfDay >= 86400000) { gMsOfDay = gMsOfDay - 86400000; }
    gMinute = (gMsOfDay / 60000) % 60;
    gHour = (gMsOfDay / 3600000) % 24;
}

// 算这一刻的天光 / 暖色 / 日月位置 —— **每帧调一次**（游戏时间可能刚跳过一格）
// Compute this moment's daylight / warm colour / sun and moon positions — **called once per frame** (the game time may have just stepped)
void clockCompute(int scrW, int groundY, int hudH)
{
    int gmins;
    int tpvM;            // 千分之一 tpv（0..10000）—— **位置与角度都按它算**，见下
    // Thousandths of tpv (0..10000) — **both position and angle are computed from it**, see below
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
    // ── Sun: rises at 6:00, peaks at 12:00, sets at 18:00 ────────────────
    //
    // ⚠ 位置一律由 `gMsOfDay`（毫秒）推，**不用 `gmins`**：
    // ⚠ Positions are always derived from `gMsOfDay` (milliseconds), **never from `gmins`**:
    //   `gmins` 的粒度是一分钟 ⇒ 太阳每秒才动一次、每次移动接近 1px，
    //   `gmins` has one-minute granularity ⇒ the sun would move only once a second, by close to 1px each time,
    //   在屏幕上就是"一顿一顿地走"。毫秒版每帧都在动，而 `tpvM` 的分辨率
    //   which on screen reads as "moving in jerks". The millisecond version moves every frame, and `tpvM`'s resolution
    //   （4.3 秒 / 步）换算到屏幕上是 0.04px/步 —— 远在肉眼之下。
    //   (4.3 seconds per step) works out to 0.04px per step on screen — far below what the eye can see.
    //   `gmins` 只留给"升没升起来"这种**开关量**判据（它是分钟语义的）。
    //   `gmins` is kept only for **boolean** criteria like "is it up yet" (those are minute-semantics).
    gSunUp = 0;
    tpvM = 0;
    if (gmins >= 360)
    {
        if (gmins <= 1080)
        {
            gSunUp = 1;
            // 6:00 = 21600000ms；12 小时 = 43200000ms ⇒ 每 4320ms 一格，共 10000 格
            // 6:00 = 21600000ms; 12 hours = 43200000ms ⇒ one step per 4320ms, 10000 steps in total
            tpvM = (gMsOfDay - 21600000) / 4320;      // 0..10000
        }
    }
    gDayL = 0;
    if (gSunUp != 0)
    {
        // 高度角 → 0..100，再**提亮 1.4 倍**（封顶 100）。
        // Elevation angle → 0..100, then **brightened 1.4×** (capped at 100).
        // 不提的话"看着像白天"只有 9:00–15:00 那一小段，而玩家 24 分钟才看完一天，
        // Without it, "looks like daytime" would be only the short stretch 9:00–15:00, and a player sees a whole day in 24 minutes,
        // 白天的观感被压到中间那几帧很不划算。1.4 倍之后约 7:00–17:00 都满亮。
        // so compressing the daytime feel into a few frames in the middle is a bad trade. With 1.4× about 7:00–17:00 is fully bright.
        // ⚠ `isin` 的值域是 ×1000（**不是 BASIC 那个 SIN 的 ×10000**），
        // ⚠ `isin`'s range is ×1000 (**not the ×10000 of BASIC's `SIN`**),
        //   所以这里是 `* 14 / 100` = ×140 —— 照抄 BASIC 的 `/ 1000` 会差一个数量级。
        //   so here it is `* 14 / 100` = ×140 — copying BASIC's `/ 1000` would be off by an order of magnitude.
        // ⚠ 用 `isin_f`（毫度）：亮度每秒跳一格会让朝霞的过渡出现台阶
        // ⚠ Use `isin_f` (milli-degrees): a brightness step every second would make the dawn transition stair-step
        //   （按整数度算的话，1 度 ≈ 一整分钟的天光变化，天亮的边沿是锯齿状的）。
        //   (with integer degrees, 1 degree ≈ a whole minute of daylight change, so the edge of daybreak is saw-toothed).
        gDayL = isin_f(tpvM * 18) * 14 / 100;         // tpvM×18 = 毫度（0..180000）
        // tpvM×18 = milli-degrees (0..180000)
        if (gDayL > 100) { gDayL = 100; }
    }

    // 日月走**同一条弧**（月亮把时钟拨 12 小时）
    // The sun and moon follow **the same arc** (the moon's clock is shifted by 12 hours)
    arcX0 = 40;
    arcW = scrW - 80;
    arcBot = groundY - 46;
    arcH = arcBot - hudH - 46;
    if (arcH < 40) { arcH = 40; }
    gSunX = arcX0 + arcW * tpvM / 10000;
    gSunY = arcBot - arcH * isin_f(tpvM * 18) / 1000;

    // 月亮：同样走毫秒（`mm` 那套是分钟语义的，这里只借它判"在不在天上"）
    // Moon: also driven by milliseconds (the `mm` set is minute-semantics; it is borrowed here only to tell "is it in the sky")
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
    // Zenith: night(7,10,24) → day(46,111,208)
    gZenR = 7 + (46 - 7) * gDayL / 100;
    gZenG = 10 + (111 - 10) * gDayL / 100;
    gZenB = 24 + (208 - 24) * gDayL / 100;
    // 地平线：夜(18,19,42) → 昼(168,216,245)
    // Horizon: night(18,19,42) → day(168,216,245)
    gHorR = 18 + (168 - 18) * gDayL / 100;
    gHorG = 19 + (216 - 19) * gDayL / 100;
    gHorB = 42 + (245 - 42) * gDayL / 100;

    // 日出 / 日落的暖色：以 6:00 与 18:00 为中心各一个 ±60 分钟的三角窗。
    // Sunrise / sunset warm colour: a ±60-minute triangular window centred on 6:00 and on 18:00.
    // ⚠ **不能用 gDayL 算暖色**：它在日落那一刻直接归 0，而"0 ⇒ 最暖"会让
    // ⚠ **The warm colour must not be computed from `gDayL`**: it drops straight to 0 at the moment of sunset, and "0 ⇒ warmest" would make
    //   地平线在 18:00 整从橙**跳**回蓝（BASIC 版实测过，肉眼可见的一跳）。
    //   the horizon **jump** from orange back to blue exactly at 18:00 (measured on the BASIC version, a visible jump).
    //   按**时钟**给窗、两窗重叠取大，黄昏的余晖才是连续收尾的。
    //   Windowing by the **clock** and taking the larger of the two overlapping windows is what makes the dusk afterglow fade out continuously.
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
// The sky colour **components** at some y — for `mixcol` (it takes components, not a packed colour).
// 结果落在 gMr/gMg/gMb（与 BASIC 版 `mix2`/`skyAt` 的约定一致：输出走固定全局，
// The result lands in `gMr/gMg/gMb` (the same convention as `mix2`/`skyAt` in the BASIC version: output goes through fixed globals,
// 因为 C++ 前端不能让函数一次返回三个值）。
// because the C++ front end cannot have a function return three values at once).
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
// Pack the result of the previous `clockPartsAt`
int clockPackParts() { return 0xFF000000 + gMr * 65536 + gMg * 256 + gMb; }

// ════════════════════════════════════════════════════════════════════
// 街灯 —— 地面上几盏，**夜里才亮**
// Street lamps — a few on the ground, **lit only at night**
//
// 位置按屏幕宽度均分（不用随机数：路灯本来就该等距，随机反而像故障）。
// Positions are divided evenly by screen width (no random numbers: street lamps should be evenly spaced, and randomness would look like a fault).
// 白天灯是暗的（只是几根杆），夜里灯头变暖黄 —— 与窗户同一套天光口径。
// By day the lamps are dark (just poles), at night the heads turn warm yellow — the same daylight convention as the windows.
// ════════════════════════════════════════════════════════════════════

#define N_LAMP 3

/// 一盏灯的**标准尺寸**（录图块用；灯是固定尺寸，贴的时候不用缩放）。
/// A lamp's **standard size** (for block recording; lamps are a fixed size, so no scaling when stamping).
/// 内容范围：x ∈ [lx-1, lx+18]、y ∈ [ly-46, ly] ⇒ 19×46。
/// Content extent: x ∈ [lx-1, lx+18], y ∈ [ly-46, ly] ⇒ 19×46.
#define LAMP_STD_W 19
#define LAMP_STD_H 46

/// 一盏路灯的局部形状（`lx` = 灯柱中轴、`ly` = 地面）。
/// A street lamp's local shape (`lx` = the pole's centre axis, `ly` = the ground).
/// 抽出来是为了录图块 —— 杆和灯头是**两套独立的昼夜插值**，所以要按天光分档录。
/// Extracted so it can be recorded as a block — the pole and the head are **two independent day/night interpolations**, so they must be recorded per daylight step.
void lampShapeAt(int lx, int ly, int dayL)
{
    int poleC;
    int headC;
    // 杆：白天是深灰（有体积感），夜里更深（背光）
    // Pole: dark grey by day (gives it volume), darker at night (backlit)
    poleC = mixcol(28, 30, 36, 74, 78, 88, dayL);
    // 灯头：白天是玻璃（暗），夜里是暖黄 —— 夜里越黑越亮
    // Head: glass by day (dark), warm yellow at night — the darker the night the brighter
    headC = mixcol(255, 214, 130, 96, 102, 116, dayL);
    ui_rect(lx - 1, ly - 46, 3, 46, poleC, 1, 0, 0);      // 杆
    // Pole
    ui_rect(lx - 1, ly - 46, 14, 3, poleC, 1, 0, 0);      // 横臂
    // Cross arm
    ui_rect(lx + 8, ly - 44, 10, 5, headC, 1, 0, 0);      // 灯头（挂在横臂末端）
    // Lamp head (hung at the end of the cross arm)
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
        // Evenly divided: at 1/6, 3/6, 5/6
        ly = groundY;
        if (lampBid[slot] > 0)
        {
            // 图块中心相对"灯柱底"偏 (8.5, -23)（见录制处的局部坐标）——
            // The block centre is offset (8.5, -23) from the "pole base" (see the local coordinates at the recording site) —
            // ⚠ 灯是固定尺寸，**不用缩放**，所以这个偏移是常数，直接加到坐标上即可
            // ⚠ lamps are a fixed size and **are not scaled**, so this offset is a constant and can simply be added to the coordinates
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
// Meteors — one occasionally streaks across at night
//
// 拆成两件事看：
// Break it into two separate things:
//   · **节拍**（第几分钟会有）仍由**游戏分钟**推出来 —— 所以"为什么现在没流星"
//   · the **cadence** (which minutes have one) is still derived from **game minutes** — so "why is there no meteor now"
//     一句话能答上来（现在是第 4 分钟，不在周期前 3 分钟里）；
//     is answerable in one line (it is minute 4 now, not in the first 3 minutes of the cycle);
//   · **这一颗长什么样**（起点在哪、尾多长、甚至这一颗来不来）由**随机数**定。
//   · **what this particular one looks like** (where it starts, how long its tail is, even whether it comes at all) is decided by **random numbers**.
// 玩家要的"流星的位置随机"就是后半件：位置固定 ⇒ 每次都在同一个地方划过，
// The player's "meteor positions should be random" is that second half: a fixed position ⇒ it streaks across the same place every time
// 看两回就腻了。
// and gets boring after two views.
//
// ⚠⚠ 随机数**每颗只掷一次**，判据是"周期序号变了没有"（`cid`）。
// ⚠⚠ The random numbers are **rolled once per meteor**, with the criterion "has the cycle index changed" (`cid`).
//   本函数**每帧都调** —— 每帧掷一次的话，流星会变成满屏乱跳的一条线
//   This function is **called every frame** — rolling every frame would turn the meteor into a line jumping all over the screen
//   （位置每帧不同，读出来是"闪烁的斜线"而不是"划过"）。
//   (a different position each frame, which reads as "a flickering diagonal line" rather than "streaking past").
//   这与云"按分钟算位置"是同一条理由：**跟时间走的东西不要每帧重新决定**。
//   This is the same reasoning as clouds "computing position by the minute": **do not re-decide every frame something that follows time**.
//
// ⚠ 它和云一样，是**按时间算位置**而不是每帧累加 —— 累加会随帧率变快慢。
// ⚠ Like the clouds it **computes position from time** rather than accumulating per frame — accumulation would speed up and slow down with the frame rate.
// ════════════════════════════════════════════════════════════════════

static int metCycle = -1;        // 当前这颗的周期序号（-1 = 还没掷过）
// Cycle index of the current one (-1 = not rolled yet)
static int metSkip;              // 1 = 这一颗不出现
// 1 = this one does not appear
static int metOffX;              // 起点横向偏移
// Starting horizontal offset
static int metOffY;              // 起点纵向偏移
// Starting vertical offset
static int metTail;              // 尾长（屏宽的百分之几）
// Tail length (as a percentage of screen width)

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
    // One every 7 game minutes; visible only at night (low daylight)
    if (gDayL > 35) { return; }
    cycle = (gHour * 60 + gMinute) % 7;
    if (cycle >= 3) { return; }                  // 只用周期里的前 3 分钟
    // Use only the first 3 minutes of the cycle
    phase = cycle * 100 / 3;                     // 0..100

    // ── 这一颗的形状：**每颗只掷一次**（见文件头那条说明）──────────────
    // ── The shape of this one: **rolled once per meteor** (see the note in the file header) ──
    cid = (gHour * 60 + gMinute) / 7;
    if (cid != metCycle)
    {
        metCycle = cid;
        metSkip = 0;
        if (ui_rand(4) == 0) { metSkip = 1; }              // 1/4 的夜里干脆不来
        // 1/4 of nights it simply does not come
        metOffX = ui_rand(scrW / 3) - scrW / 6;            // 起点左右晃 ±1/6 屏宽
        // The start sways ±1/6 of the screen width sideways
        metOffY = ui_rand(groundY / 4) - groundY / 8;      // 起点上下晃 ±1/8 地平线高
        // The start sways ±1/8 of the horizon height up and down
        metTail = 5 + ui_rand(7);                          // 尾长 5..11
        // Tail length 5..11
    }
    if (metSkip != 0) { return; }

    mx = scrW * 70 / 100 - scrW * phase / 100 + metOffX;   // 从右上往左下
    // From the upper right toward the lower left
    // ⚠ `my` 的纵向跨度（55%）改了，下面尾迹的 `tailY` 必须**跟着改同一个系数** ——
    // ⚠ If `my`'s vertical span (55%) changes, the trail's `tailY` below must **change by the same factor** —
    //   尾迹靠"取位移的一个固定比例"来与轨迹同向，两处系数一旦不同源，
    //   the trail stays aligned with the trajectory by "taking a fixed fraction of the displacement", and once the two factors come from different sources
    //   尾巴就又不按流线方向跑了（这正是玩家上一轮挑出来的毛病）。
    //   the tail stops following the streamline direction again (exactly the flaw the player picked out last round).
    my = groundY * 25 / 100 + groundY * phase * 55 / 10000 + metOffY;
    alpha = 100 - phase;                         // 越飞越淡
    // Fades as it flies
    if (alpha < 0) { alpha = 0; }

    // ── 尾迹**沿运动方向**（玩家报：「流星太大，而且不是按流线方向跑的」）──
    // ── The trail **runs along the direction of motion** (the player reported: "the meteor is too big, and the tail does not run along the streamline") ──
    //
    // ⚠ 原来尾巴是"往右下 26px、再往上一点"的一段**固定轴向**，
    // ⚠ The tail used to be a **fixed axis** segment "26px down-right, then a bit back up",
    //   而流星本身是**从右上往左下**飞的 —— 两者方向不一致，看着就是"拖错了方向"；
    //   while the meteor itself flies **from the upper right to the lower left** — the two disagreed, which looks like "the tail is trailing the wrong way";
    //   而且尾巴是 6 个半径 5/4/3/2/1/0 的**圆点**，头一颗直径 10px，太大。
    //   and the tail was 6 **dots** of radius 5/4/3/2/1/0, the first 10px across, too big.
    //
    // 现在：尾迹方向**就是运动方向的反向** —— 位移在一个周期里是
    // Now: the trail direction **is the reverse of the direction of motion** — the displacement over one cycle is
    //   `(-scrW, +groundY)`，那么尾巴取它的一个固定比例（这里 8%）就与轨迹严格同向，
    //   `(-scrW, +groundY)`, so taking a fixed fraction of it (8% here) for the tail stays strictly collinear with the trajectory,
    //   不用算归一化（**不能调 isqrt**：它定义在 Building 之后，这里在它之前）。
    //   with no normalisation needed (**`isqrt` must not be called**: it is defined after `Building`, and this is before that).
    tailX = scrW * metTail / 100;
    tailY = groundY * metTail * 55 / 10000;
    tx = mx + tailX;
    ty = my - tailY;

    clockPartsAt(my, groundY);
    // 外圈一道淡光晕 → 细芯 → 头上一点亮：一条线读出"划过"
    // A faint halo outside → a thin core → a bright point at the head: one line that reads as "streaking past"
    ui_line(tx, ty, mx, my, mixcol(gMr, gMg, gMb, 255, 250, 220, alpha * 40 / 100), 3);
    ui_line(tx, ty, mx, my, mixcol(gMr, gMg, gMb, 255, 252, 235, alpha * 85 / 100), 1);
    ui_circle(mx, my, 2, mixcol(gMr, gMg, gMb, 255, 255, 245, alpha), 1, 0);
}


// ════════════════════════════════════════════════════════════════════
// 云 —— 白天天上那几团，**随游戏时间慢慢飘**，出画从另一头绕回来
// Clouds — those clumps in the daytime sky, **drifting slowly with game time**, wrapping back in from the other side
//
// 可见度**跟着天光走**（而不是"天黑就不画"的开关）：天光为 0 时云的颜色正好等于
// Visibility **follows the daylight** (rather than a "do not draw once it is dark" switch): at daylight 0 the cloud colour equals exactly
// 当地天空色 ⇒ 自然消失。少一个能写错的开关，也少一次"某一刻画面跳一下"。
// the local sky colour ⇒ it disappears naturally. One less switch to get wrong, and one less "the picture jumps at some moment".
//
// ⚠ 位置是**按游戏分钟算的**（不是每帧累加）：累加的话帧率一变云就飘得快慢不一，
// ⚠ Positions are **computed from game minutes** (not accumulated per frame): with accumulation, a change in frame rate would make clouds drift at varying speeds,
//   而"飘"这件事本来就该由时钟定，与画多快无关 —— 与时钟走独立定时器是同一条理由。
//   and drifting is something the clock should decide, independent of how fast we draw — the same reasoning as the clock running on a separate timer.
// ════════════════════════════════════════════════════════════════════

#define N_CLOUD 5

static int cloudY[N_CLOUD];      // 第 i 朵的基准高度（按屏幕比例，开局定一次）
// Base height of cloud i (as a screen ratio, fixed once at start)
static int cloudW[N_CLOUD];      // 第 i 朵的宽度
// Width of cloud i

// ── 云 / 路灯 / 树的**图块**（开窗时录、每帧贴一次）──────────────────────
// ── **Blocks** for clouds / street lamps / trees (recorded when the window opens, stamped once per frame) ──
//
// ⚠ 它们的颜色**随昼夜连续变**，而图块里的颜色是**录制那一刻定死的** ⇒ 按天光
// ⚠ Their colours **change continuously with the day/night cycle**, while the colours inside a block are **fixed at the moment of recording** ⇒ record
//   **分档录**（`VML_DAY_STEPS` 档），贴的时候按当前天光选最近的一档。
//   **one block per step** (`VML_DAY_STEPS` steps), and when stamping pick the nearest step to the current daylight.
//   24 真实分钟走完一天 ⇒ 每档约 1.5 分钟，档位之间的颜色差看不出来。
//   24 real minutes make a full day ⇒ about 1.5 minutes per step, and the colour difference between steps is invisible.
#define VML_DAY_STEPS 16

/// 楼的**窗户**按昼夜分几档录（开窗时录、每帧贴一次）。
/// How many steps the building's **windows** are recorded in over the day/night cycle (recorded when the window opens, stamped once per frame).
///
/// ⚠⚠ **不能跟着 `VML_DAY_STEPS`（16）走** —— 块表有 `MaxBlocks = 128` 的上限，
/// ⚠⚠ **It must not follow `VML_DAY_STEPS` (16)** — the block table has a cap of `MaxBlocks = 128`,
///   而 6 栋楼 × 16 档 = 96 张，加上精灵 / 云 / 路灯 / 树的块一共 136 张，**超了**。
///   and 6 buildings × 16 steps = 96 blocks, plus the sprite / cloud / lamp / tree blocks, comes to 136, which **exceeds it**.
///   超限的后果**不是"少录几张"**：`ui_create_block` 返回 0 **且不进入录制态**，
///   The consequence of overflowing is **not "a few blocks went unrecorded"**: `ui_create_block` returns 0 **and does not enter recording mode**,
///   此时若照常画窗户，那些矩形会**直接落到画布上** —— 实测现象就是
///   and if the windows are then drawn as usual, those rectangles **land straight on the canvas** — the measured symptom being
///   **一片窗户飘在天上**（录制用的是局部坐标 (0,0)，也就是画布原点）。
///   **a sheet of windows floating in the sky** (recording uses local coordinates (0,0), which is the canvas origin).
///   8 档 × 6 栋 = 48 张，连原有的 40 张一共 88，留足余量。
///   8 steps × 6 buildings = 48 blocks, 88 together with the existing 40, leaving plenty of headroom.
///   档位差（天光 12.5）下的台阶看不出来 —— 窗户本来只有三种色。
///   The step difference (12.5 of daylight) shows no banding — the windows only ever had three colours anyway.
///
/// ⚠ **为什么是"整栋一张 w×h 的大块"而不是"一窗一块"**（v0.96.477 试过又改回来）：
/// ⚠ **Why "one big w×h block for the whole building" and not "one block per window"** (tried in v0.96.477 and reverted):
///   一窗一块在结构上更稳（块的尺寸固定 11×14、位置每扇自负，不会整批偏），
///   one block per window is structurally more robust (the block size is fixed at 11×14, each window is responsible for its own position, so the whole batch cannot shift),
///   但**性能差 21%**：真机实测整栋一块每帧只发 6 次贴图调用，一窗一块要发 84 次
///   but it is **21% slower**: measured on device, one block per building issues only 6 stamp calls per frame, while one per window issues 84
///   （5~6 栋 × 几十扇）。而"程序→宿主"的往返正是当初录块要省掉的那笔开销 ——
///   (5..6 buildings × dozens of windows). And the "program → host" round trip is exactly the cost that block recording was meant to save —
///   一窗一块等于把它还回去 14 倍（23.8fps → 18.7fps，`gfxinfo` 里
///   one block per window hands 14× of it back (23.8fps → 18.7fps; in `gfxinfo`,
///   `High input latency` 4682、中位帧时间 36ms，用户感受到的就是**触摸明显变迟钝**）。
///   `High input latency` 4682 and a 36ms median frame time, and what the user feels is **noticeably sluggish touch**).
///
///   "整批偏"那个风险改用**贴前校验**挡住（见 `DrawBody`）：块尺寸与楼体对不上就
///   The "whole batch shifts" risk is blocked instead by a **pre-stamp check** (see `DrawBody`): if the block size does not match the building
///   **不贴**、退回逐扇画 —— 于是"拿错尺寸的块硬贴"从结构上不可能发生，
///   it is **not stamped**, and drawing falls back to per-window — so "force-stamping a block of the wrong size" becomes structurally impossible,
///   不必靠"一窗一块"来换。
///   with no need to trade it away for "one block per window".
///
/// ⚠ 块是**每局重录**的（楼基色每局都变，`SetTint` 在 `Layout` 里调）⇒
/// ⚠ The blocks are **re-recorded every round** (the building base colour changes each round; `SetTint` is called inside `Layout`) ⇒
///   重录前**必须先 `ui_free_block` 释放上一局的**，否则每局漏 48 个句柄、三局就撑满 128。
///   before re-recording you **must `ui_free_block` the previous round's**, otherwise each round leaks 48 handles and three rounds fill up 128.
#define WIN_DAY_STEPS 8

/* 一扇窗的图块尺寸（含边框）—— **固定值**，与楼体宽高无关。 */
// Block size of one window (border included) — a **fixed value**, independent of the building's width and height.
/*   这正是"一窗一块"相对"整栋一张大块"的好处：位置由每扇窗自己算， */
// This is exactly the advantage of "one block per window" over "one big block for the whole building": each window
/*   不会因为块尺寸与楼体有一点点对不上而**整栋一起偏**。 */
// computes its own position, so a small mismatch between the block size and the building cannot make **the whole building shift together**.
// ⚠ 这两个是**最小值**，实际窗户尺寸随楼体缩放（见 `StepX/StepY`）。
// ⚠ These two are **minimums**; the actual window size scales with the building (see `StepX/StepY`).
//   固定像素尺寸的老写法有个致命后果：**楼一大，窗户就按面积炸开** ——
//   The old fixed-pixel-size approach had one fatal consequence: **the bigger the building, the more the windows explode in area** —
//   实测 1757×1904 的画布上，四栋楼合起来 **2158 个图元/帧**（≈1080 扇窗 × 每扇 2 个
//   measured on a 1757×1904 canvas, four buildings together were **2158 figures/frame** (≈1080 windows × 2
//   `ui_rect`：外框 + 玻璃），macOS 上直接掉到 **1.4 fps**，玩家看到的就是"只有一帧"。
//   `ui_rect` each: outer frame + glass), dropping straight to **1.4 fps** on macOS, which the player sees as "only one frame".
//   而"录制图块"省不掉这个数 —— 宿主贴块时会把指令**重放**出来，
//   And "recording a block" does not save that number — the host **replays** the instructions when stamping a block,
//   图元数照旧（那条注释在 `MakeWinBlocks` 上面，别误会成"录了块就便宜了"）。
//   so the figure count is unchanged (that comment sits above `MakeWinBlocks`; do not misread it as "recording a block makes it cheap").
#define WIN_W 11
#define WIN_H 14

static int cloudBid[VML_DAY_STEPS];
static int lampBid[VML_DAY_STEPS];
static int treeBid[3];           // 树的绿色**本来就是三档固定色** ⇒ 不需要分昼夜
// The trees' green **is three fixed colours by construction** ⇒ no day/night stepping needed
static int cloudSpeed[N_CLOUD];  // 第 i 朵的飘动速度（**像素 / 秒**，见 `cloudX`）
// Drift speed of cloud i (**pixels / second**, see `cloudX`)
static int cloudsReady;

void cloudsInit(int scrW, int groundY)
{
    int i;
    i = 0;
    while (i < N_CLOUD)
    {
        // 位置/大小由下标推出来（**不用随机数**：每局都该长得差不多，
        // Position/size derived from the index (**no random numbers**: every round should look roughly the same,
        // 而且随机的话"这朵云什么时候飘回来"就不可预期了）
        // and with randomness "when will this cloud drift back" becomes unpredictable)
        cloudY[i] = 40 + (i * 53) % (groundY / 2);
        // 宽度 40..75（玩家报「云层有点偏大」）——
        // Width 40..75 (the player reported "the clouds are a bit too big") —
        // ⚠ 原来是 60..129，最宽那朵占屏宽的三分之一，天上全是云、楼都不显了。
        // ⚠ it used to be 60..129, and the widest one took a third of the screen width, filling the sky with clouds so the buildings no longer showed.
        //   云是**背景的装饰**，它一大就抢戏；这里按"比一栋楼窄一点"来定
        //   Clouds are **background decoration**; too big and they steal the scene. Here the size is set as "a bit narrower than a building"
        //   （楼宽 ≈ 可用宽 / 栋数 ≈ 300/6 ≈ 50）⇒ 取 40~75 正好压在这个量级上。
        //   (building width ≈ usable width / building count ≈ 300/6 ≈ 50) ⇒ 40..75 lands right on that order of magnitude.
        cloudW[i] = 40 + (i * 23) % 36;
        cloudSpeed[i] = 2 + (i % 3);
        i = i + 1;
    }
    cloudsReady = 1;
}

/// 第 i 朵云现在的左边 x（按**毫秒**算，出画就从另一头绕回来）。
/// The current left x of cloud i (computed from **milliseconds**; when it leaves the screen it wraps in from the other side).
///
/// ⚠ 时间用 `gMsOfDay` 而不是 `gHour*60+gMinute`（玩家报的"云层不平滑"就是这个）：
/// ⚠ Time uses `gMsOfDay`, not `gHour*60+gMinute` (this is exactly the "the clouds are not smooth" the player reported):
///   按分钟算 ⇒ 位置**每秒跳一次**，而 `cloudSpeed` 是 2~4px/分钟 ⇒
///   computing by the minute ⇒ the position **jumps once a second**, and `cloudSpeed` is 2..4px/minute ⇒
///   云每秒瞬移 2~4 像素，看着就是一顿一顿的。
///   clouds teleport 2..4 pixels a second, which looks jerky.
///   换成毫秒之后同一段位置函数变成**每帧都在动**，每帧只动零点几像素 —— 连续了。
///   With milliseconds the same position function **moves every frame**, by a fraction of a pixel per frame — continuous.
/// ⚠ `cloudSpeed` 的语义因此从"像素/游戏分钟"变成**"像素/秒"**（数值不变：
/// ⚠ `cloudSpeed`'s semantics therefore change from "pixels per game minute" to **"pixels per second"** (the values are unchanged:
///   2~4 px/s 正是原来的观感速度，改的只是"跳一次"变成"连续走"）。
///   2..4 px/s is exactly the original perceived speed; only "one jump" became "a continuous glide").
///   **别在这里再做 `*60` 之类的换算** —— 那会让云快 60 倍。
///   **Do not add a `*60` or similar conversion here** — that would make the clouds 60× faster.
int cloudX(int idx, int scrW)
{
    int span;
    int pos;
    span = scrW + 160;                       // 多留 160，让云整个出画再回来
    // Keep 160 extra, so the cloud leaves the screen entirely before coming back
    pos = cloudSpeed[idx] * gMsOfDay / 1000 + idx * 97;
    pos = pos % span;
    return pos - 120;
}

// 一朵云 = 三团椭圆叠出来（便宜，形状够用）
// One cloud = three overlapping ellipses (cheap, and the shape is good enough)
/// 云的**标准尺寸**（录图块用）—— 贴的时候按实际宽度等比缩放。
/// A cloud's **standard size** (for block recording) — stamped scaled proportionally to the actual width.
#define CLOUD_STD_W 128
/// 云图块的高：云心**上方** 2×ch（最大的鼓包顶到那儿）、**下方** ch/2 ⇒ `2.5×ch`。
/// The cloud block's height: **above** the cloud centre 2×ch (the biggest bump reaches there), **below** it ch/2 ⇒ `2.5×ch`.
/// ⚠ 云心因此在图块里偏下（从顶算 `2×ch`），贴的时候要减掉那 31.5 个标准像素的偏移。
/// ⚠ The cloud centre is therefore low inside the block (2×ch from the top), and stamping must subtract that 31.5 standard-pixel offset.
#define CLOUD_STD_H (CLOUD_STD_W * 5 / 6)

/// 一朵云（局部形状：`cx,cy` 是云心、`cw` 是宽度、`ch = cw/3`）。
/// One cloud (local shape: `cx,cy` is the cloud centre, `cw` the width, `ch = cw/3`).
///
/// ⚠ 抽成函数是**为了录图块**：形状写一遍，录的时候调它、画的时候贴。
/// ⚠ It is extracted as a function **so it can be recorded as a block**: the shape is written once, called while recording and stamped while drawing.
///   `dayL` 是**绘制时**的天光 —— 录图块时传档位值，正常绘制时传 `gDayL`。
///   `dayL` is the daylight **at draw time** — pass the step value while recording, pass `gDayL` for normal drawing.
///   天空色（`gMr/gMg/gMb`）由调用方先 `clockPartsAt` 设好。
///   The sky colour (`gMr/gMg/gMb`) is set up by the caller with `clockPartsAt` first.
void cloudShapeAt(int cx, int cy, int cw, int dayL)
{
    int ch;
    int col;
    int shade;
    ch = cw / 3;
    // 云色 = 当地天空色 → 白，按天光插值 ⇒ 天全黑时云正好融进天空。
    // Cloud colour = local sky colour → white, interpolated by daylight ⇒ when the sky is fully dark the cloud blends right into it.
    // ⚠ 系数要**够狠**：天光 52（早上 7:30）时若只插到 44%，云就是 `0xFF8094B3`
    // ⚠ The factor has to be **aggressive**: at daylight 52 (7:30 in the morning), interpolating only to 44% gives a cloud of `0xFF8094B3`,
    //   这种灰蓝，比天空亮一点点、**肉眼看不出来**（实测过）。
    //   a greyish blue only slightly brighter than the sky and **invisible to the eye** (measured).
    //   乘 1.6、封顶 100 ⇒ 天光 63 以上就是纯白的云，日出前后才淡出。
    //   Multiplying by 1.6 and capping at 100 ⇒ above daylight 63 the cloud is pure white, fading out only around sunrise.
    col = mixcol(gMr, gMg, gMb, 255, 255, 255, dayL * 160 / 100);
    if (dayL * 160 / 100 > 100) { col = mixcol(gMr, gMg, gMb, 255, 255, 255, 100); }
    shade = mixcol(colR(col), colG(col), colB(col), gMr, gMg, gMb, 30);

    // ⚠ 原来是"三颗球叠一起"，玩家原话是「云就是圆球？」
    // ⚠ It used to be "three balls stacked together"; the player's words were "clouds are just round balls?"
    //   云之所以一眼是云，靠的是**底边接近一条水平线**、只有上半是鼓的。
    //   What makes a cloud read as a cloud at a glance is **a bottom edge close to a horizontal line**, with only the upper half bulging.
    //   所以改成：一条扁的圆角底座（两端自然收成半圆）+ 顶上加三个鼓包。
    //   So it is now: a flat rounded base (the ends closing naturally into half-circles) + three bumps added on top.
    //   只画圆的话，无论怎么排都会读成"一堆球" —— 差别全在这条底边。
    //   With circles only, no arrangement escapes reading as "a pile of balls" — the difference is entirely that bottom edge.
    ui_rect(cx, cy - ch / 2, cw, ch, col, 1, 0, ch / 2);
    ui_circle(cx + cw / 5,     cy - ch / 2, ch * 3 / 4, col, 1, 0);
    ui_circle(cx + cw / 2,     cy - ch,     ch,         col, 1, 0);
    ui_circle(cx + cw * 4 / 5, cy - ch / 2, ch * 2 / 3, col, 1, 0);
    // 底面一道暗色 —— 有它才有体积感，否则还是"贴纸"
    // A dark band along the underside — without it there is no volume, and it stays a "sticker"
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
    // Pick the block step for the current daylight (24 minutes make a full day ⇒ ~1.5 minutes per step, the difference between steps is invisible)
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
            // ⚠ **Proportional scaling**: the cloud's aspect ratio is fixed by construction (`ch = cw/3`), and non-uniform scaling would flatten the bumps.
            // ⚠ **要减一个偏移**：图块的内容是"往上鼓"的（云心下方只有 ch/2、上方有
            // ⚠ **An offset must be subtracted**: the block's content bulges **upward** (below the cloud centre only ch/2, above it
            //   2×ch），所以图块的**几何中心不等于云心** —— 差 31.5 个标准像素（见录制处）。
            //   2×ch), so the block's **geometric centre is not the cloud centre** — the difference is 31.5 standard pixels (see the recording site).
            //   不减这一步，云会整体往下沉一截。
            //   Skip this subtraction and the whole cloud sinks down by that much.
            ui_draw_block(cloudBid[slot], cx, cy - (32 * cw / CLOUD_STD_W),
                          cw * 1000 / CLOUD_STD_W, cw * 1000 / CLOUD_STD_W, 0);
        }
        else
        {
            // 图块没建起来（有真窗口时不该发生）—— 退回直接画
            // The block was not created (should not happen with a real window) — fall back to drawing directly
            clockPartsAt(cy, groundY);
            cloudShapeAt(cx, cy, cw, gDayL);
        }
        i = i + 1;
    }
}



// ════════════════════════════════════════════════════════════════════
// 音效：音色表
// Sound effects: the timbre table
// ════════════════════════════════════════════════════════════════════
//
// **用 `ui_beep` 单音**（v0.96.509 统一换回来）。
// **Single tones via `ui_beep`** (switched back uniformly in v0.96.509).
//
// ⚠⚠ 这些音一度走共享库的音序器（`ui_sfx_add` / `ui_sfx_tick`），**真机上破音**，
// ⚠⚠ These sounds once went through the shared library's sequencer (`ui_sfx_add` / `ui_sfx_tick`) and **clipped on a real device**,
//   全部换回来了。破音的是**这里配的音** —— 那一版同时踩了两条：
//   so they were all switched back. What clipped was **the sound configured here** — that version hit two things at once:
//     · **多个声部同时响**：命中是和弦（3 声部）、撞楼是三个不谐和低音一起轰，
//     · **several voices sounding at the same time**: a hit was a chord (3 voices), and a building strike was three dissonant low notes blasting together;
//       多声部混音一叠加，音量就顶到削波；
//       mix several voices together and the level reaches clipping;
//     · **长音拖尾**：胜负两组末音都拖了 10–12 拍（一拍 33ms ≈ 400ms）。
//     · **long trailing notes**: the last notes of both the win and lose sets lasted 10–12 ticks (one tick 33ms ≈ 400ms).
//   `ui_beep` 是**单通道**的（后一个音掐掉前一个）⇒ 一个事件永远只有一个音在响，
//   `ui_beep` is **single-channel** (a new tone cuts the previous one) ⇒ an event never has more than one tone sounding,
//   **结构上不可能削波、也不会长音叠加**。代价是没有和弦、没有音色 —— 对
//   so **clipping is structurally impossible and long notes cannot stack**. The cost is no chords and no timbre — good enough for
//   "打中/爆炸/胜负"这类**一次性提示音**够用。
//   **one-shot cues** like "hit / explosion / win-or-lose".
//
// ── 频率怎么定的（机械规则，别随手改，改了几处要一起改）────────────────
// ── How the frequencies were chosen (mechanical rules; do not change them casually, and change all the related places together) ──
//   · 取整块的**首音**（MIDI → Hz）；低音不低于 **C3(131Hz)** —— 手机外放在
//   · take the **first note** of the block (MIDI → Hz); lows no lower than **C3(131Hz)** — a phone speaker
//     200Hz 以下衰减很快，36（C2=65Hz）出来是"噗"一声闷响，玩家听着像**没响**。
//     rolls off fast below 200Hz, so note 36 (C2=65Hz) comes out as a muffled "puff" that the player hears as **nothing sounding**.
//   · **胜负取两端的极值**：赢（含"更值得听见"的升级类）取整块**最高音**、
//   · **win/lose take the extremes of the range**: a win (including the "more worth hearing" upgrade ones) takes the block's **highest note**,
//     输取**最低音**。这是「不看屏幕也分得出输赢」唯一还剩的杠杆 ——
//     a loss takes the **lowest note**. This is the only lever left for "you can tell win from lose without looking at the screen" —
//     五子棋/象棋两版也是这么配的（赢 1320 / 输 240）。
//     the gomoku and chess versions are configured the same way (win 1320 / lose 240).
//   · 时长 = 整块总时长（拍 × 33ms），封顶 320ms。
//   · duration = the block's total duration (ticks × 33ms), capped at 320ms.
//   · 同文件内两个事件换算后**撞车**（同频率）时，把语义较低沉的那个挪到它的
//   · when two events in the same file **collide** after conversion (same frequency), move the semantically lower one to its
//     最低音 —— 下面 `SfxRage` 就是这么来的。
//     lowest note — that is where `SfxRage` below comes from.
//
// ── 音色表 ──────────────────────────────────────────────────────────
// ── Timbre table ──
//
// 音符号是**真 MIDI 语义**（中央 C = 60、A4 = 69 = 440Hz）—— 下面的 Hz 就是
// The note numbers are **real MIDI semantics** (middle C = 60, A4 = 69 = 440Hz) — the Hz below are converted
// 从它换算来的，注释里的 note 号标着出处。
// from them, and the note numbers in the comments mark where each came from.

/// 发射：短促的一记「嗖」（原来两个音快速下行；单音只留起手那一下）。
/// Firing: one short "whoosh" (it used to be two notes descending quickly; with a single tone only the initial hit is kept).
void SfxFire()
{
    ui_beep(698, 99);       // note 77
}

/// 命中得分：一声清亮的「叮」。得分是这一局里重复最多的正反馈 ——
/// Scoring a hit: one bright "ding". Scoring is the most repeated positive feedback in a round —
/// 音区落在中高（523Hz），既不与发射（698）混，也不像胜负那样郑重。
/// its pitch sits mid-high (523Hz), so it neither blends with firing (698) nor sounds as solemn as win/lose.
void SfxHit()
{
    ui_beep(523, 264);      // note 72
}

/// 空中爆炸（打到飞行物）：高音一「叮」，**不用轰鸣** ——
/// Mid-air explosion (hitting a flying object): one high "ding", **not a boom** —
/// 那是"打爆了一个小东西"，与撞楼的份量不一样，听着就该不一样。
/// that is "popped something small", a different weight from hitting a building, and it should sound different.
void SfxAirBoom()
{
    ui_beep(1047, 99);      // note 84
}

/// 撞楼 / 落地：「轰」—— **最低的一档**（原来靠 48/54 三全音叠出粗粝感，
/// Hitting a building / landing: a "boom" — **the lowest setting** (it used to stack 48/54 tritones for a gritty feel,
/// 单音只剩音高可用，就压到 C3）。
/// and with a single tone only pitch is left, so it goes down to C3).
void SfxGroundBoom()
{
    ui_beep(131, 231);      // note 48
}

/// 飞碟被惹毛：**我盯上你了**。
/// The UFO gets angry: **I have my eye on you**.
/// ⚠ 原来那版的**首音是 72**，与 `SfxHit` 换算出来是同一个数（523）⇒ 按上面
/// ⚠ The old version's **first note was 72**, which converts to the same number as `SfxHit` (523) ⇒ per the
///   那条"撞车就取最低音"的规则改用落点 60。
///   "on collision take the lowest note" rule above, it now uses the landing note 60.
void SfxRage()
{
    ui_beep(262, 320);      // note 60（原下行三音的落点）
    // note 60 (the landing note of the old descending three)
}

/// 被飞碟清场（这一局**与分数无关地**结束）：低沉的长音。
/// Wiped out by the UFO (this round ends **independently of the score**): one deep, long tone.
void SfxLaser()
{
    ui_beep(196, 320);      // note 55
}

/// 获胜：**最亮最高的那一档**。
/// Winning: **the brightest, highest setting**.
/// ⚠ 取整块**最高音**（原来琶音顶到 84）而不是首音 —— 见上面"胜负取两端"。
/// ⚠ It takes the block's **highest note** (the old arpeggio peaked at 84) rather than the first note — see "win/lose take the extremes" above.
void SfxWin()
{
    ui_beep(1047, 320);     // note 84（原上行和弦的顶点）
    // note 84 (the peak of the old rising chord)
}

/// 输：**最低最长的**那一档。
/// Losing: **the lowest, longest** setting.
/// ⚠ 输赢的音必须**不看屏幕也分得出** —— 原来的办法是"上行和弦 vs 小二度下行"，
/// ⚠ The win and lose tones must be **distinguishable without looking at the screen** — the old way was "rising chord vs minor-second descent",
///   单音没有方向，改用**音高**：赢 1047 / 输 131，差三个八度，不会听错。
///   and a single tone has no direction, so **pitch** is used instead: win 1047 / lose 131, three octaves apart, impossible to mishear.
void SfxLose()
{
    ui_beep(131, 320);      // note 48（原下行落点）
    // note 48 (the old descending landing note)
}

// ════════════════════════════════════════════════════════════════════
// Entity —— 所有能在屏幕上画自己的东西的基类
// Entity — the base class for everything that can draw itself on screen
//
// 只放两个字段（屏幕坐标）+ 一个**虚**的 Draw()。放基类指针数组里逐个调 Draw()
// Only two fields (screen coordinates) + one **virtual** `Draw()`. Calling `Draw()` on each entry of an array of base-class pointers
// 时，走的就是虚表派发（对象第 0 个字是 vptr，槽号在整条继承链上稳定）。
// goes through vtable dispatch (word 0 of the object is the vptr, and slot numbers are stable along the whole inheritance chain).
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
// Sky — the sky (day/night gradient + sun or moon + stars at night)
// ════════════════════════════════════════════════════════════════════

class Sky : public Entity
{
public:
    int sw;
    int sh;
    int gy;              // 地平线 y
    // Horizon y
    int ticks;           // 用来自转星星/眨眼的计数
    // Counter used to spin the stars / blink

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
        // Visibility of the stars (follows the daylight)

        // 天空：**整块渐变**，两端颜色每一帧都随时钟算（与 BASIC 版 `skyPal` 同一口径）。
        // Sky: a **single full-screen gradient**, both end colours computed from the clock every frame (the same convention as `skyPal` in the BASIC version).
        ui_gradient("sky", 0, clockZenith(), clockHorizon(), 0, 0, 0, 1000);
        ui_rect_grad(0, 0, sw, gy, "sky", 0);

        // ── 星星 ──────────────────────────────────────────────────────
        // ── Stars ──
        // 位置由下标推出来（**不用随机数**，否则每帧都在闪）。
        // Positions derived from the index (**no random numbers**, otherwise they would flicker every frame).
        // 可见度**跟着天光淡出**：天光满时正好等于当地天空色 ⇒ 自然消失。
        // Visibility **fades out with the daylight**: at full daylight it equals exactly the local sky colour ⇒ it disappears naturally.
        // 这样就不需要"天黑该不该画星星"那个开关 —— 开关会在某一刻跳一下，
        // That removes the need for a "should stars be drawn once it is dark" switch — a switch jumps at some instant,
        // 而连续变化的场景里，任何"跳"都看得出来。
        // and in a continuously changing scene any "jump" is visible.
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
                    // Each star is a touch dimmer than the next (fixed by the index), otherwise a field of identical white dots looks like noise
                    ui_rect(sx, sy, rad, rad,
                            mixcol(gMr, gMg, gMb, 255, 255, 255, fade - (i % 5) * 6),
                            1, 0, 0);
                }
                i = i + 1;
            }
        }

        // ── 太阳 / 月亮 ────────────────────────────────────────────────
        // ── Sun / moon ──
        // 位置由时钟算（6:00 升、12:00 顶、18:00 落；月亮差 12 小时走同一条弧）。
        // Positions come from the clock (rises at 6:00, peaks at 12:00, sets at 18:00; the moon walks the same arc 12 hours offset).
        // 都带一圈"朝**当地天空色**淡出"的光晕 —— 天空是渐变的，光晕外缘写死一个
        // Both carry a halo that "fades out toward the **local sky colour**" — the sky is a gradient, and hard-coding
        // 颜色就会在渐变天上留一个色斑，而且只在某些时段看得出来。
        // a colour for the halo's outer edge would leave a blotch on the gradient sky, visible only during certain hours.
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
            // Craters (three slightly darker small circles) — without them the moon is just a white pancake
            ui_circle(gMoonX - 5, gMoonY - 4, 3, 0xFFDCD6BE, 1, 0);
            ui_circle(gMoonX + 4, gMoonY + 2, 2, 0xFFDCD6BE, 1, 0);
            ui_circle(gMoonX + 1, gMoonY - 7, 2, 0xFFE4DEC6, 1, 0);
        }
    }
};

// ════════════════════════════════════════════════════════════════════
// Building —— 楼房
// Building
// ════════════════════════════════════════════════════════════════════

class Building : public Entity
{
public:
    int w;
    int h;
    int gy;              // 地平线（底面）
    // Horizon (the base)
    int tint;            // 配色档（0..3）
    // Colour step (0..3)
    int baseR;           // 本栋的**基色分量** —— 配色随时钟变化时要从分量算起
    // This building's **base-colour components** — colours that change with the clock must start from the components
    int baseG;
    int baseB;
    // 窗户图块：**整栋楼一张**（尺寸 = 楼体的 w×h），按昼夜分档。
    // Window block: **one for the whole building** (size = the building's w×h), stepped by the day/night cycle.
    //
    // `winBW`/`winBH` 记的是**录这张块时的楼体尺寸** —— 贴之前拿它和当前楼体比，
    // `winBW`/`winBH` record **the building size at the time this block was recorded** — compare them against the current building before stamping,
    // 对不上就**不贴、退回逐扇画**（见 `DrawBody`）。这是"整栋一起偏出去"那道防线：
    // and on a mismatch **do not stamp; fall back to per-window drawing** (see `DrawBody`). This is the guard against "the whole building shifting off together":
    // 不靠"一窗一块"来换结构安全，而是让"拿错尺寸的块硬贴"这件事根本无法发生。
    // rather than trading structural safety for "one block per window", it makes "force-stamping a block of the wrong size" simply impossible.
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
    /// Set the hue. **Store the components, not just the index** — every later colour decision (body, lit face, cornice, window frame)
    /// 都要以它为基准按天光插值，每次再从档号反解一遍颜色分量是"同一件事两处实现"。
    /// interpolates from it by daylight, and unpacking the components from the index again each time would be "the same rule implemented twice".
    /// 四个色与 `C_BLDG_A..D` 一一对应（BASIC 版是 6 档，这里沿用本文件原有的 4 档）。
    /// The four colours correspond one-to-one with `C_BLDG_A..D` (the BASIC version has 6 steps; this file keeps its original 4).
    /// </summary>
    void SetTint(int t)
    {
        tint = t;
        baseR = 224; baseG = 96;  baseB = 78;     // 0 珊瑚红
        // 0 coral red
        if (t == 1) { baseR = 63;  baseG = 181; baseB = 154; }   // 1 青绿
        // 1 teal
        if (t == 2) { baseR = 232; baseG = 184; baseB = 64;  }   // 2 明黄
        // 2 bright yellow
        if (t == 3) { baseR = 154; baseG = 106; baseB = 224; }   // 3 紫
        // 3 purple
        if (t == 4) { baseR = 74;  baseG = 144; baseB = 217; }   // 4 天蓝
        // 4 sky blue
        if (t == 5) { baseR = 230; baseG = 140; baseB = 60;  }   // 5 橙
        // 5 orange
        if (t == 6) { baseR = 224; baseG = 106; baseB = 152; }   // 6 粉
        // 6 pink
        if (t == 7) { baseR = 122; baseG = 190; baseB = 84;  }   // 7 草绿
        // 7 grass green
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
    // Is this x inside this building's horizontal extent?
    int Covers(int px)
    {
        if (px < x) { return 0; }
        if (px > x + w) { return 0; }
        return 1;
    }

    /// <summary>
    /// 一栋楼。**全部颜色都跟着天光走** —— 这是"昼夜连续变化"里最显眼的一块：
    /// One building. **All colours follow the daylight** — this is the most visible part of the "continuous day/night change":
    /// 楼体是"同一个色相压到 16%"，窗是"白天冷玻璃 → 夜里暖灯"，两者都按 `gDayL` 插值。
    /// the body is "the same hue pressed down to 16%", the windows are "cold glass by day → warm lamp at night", and both interpolate by `gDayL`.
    ///
    /// ⚠ 不另存一张夜色调色板 —— 那样"哪栋是哪栋"得靠人保持两张表同步（本仓的平行表坑）。
    /// ⚠ No separate night palette is stored — that would make "which building is which" depend on a human keeping two tables in sync (this repo's parallel-table trap).
    /// 口径与 `basic/gorilla_pro.bas` 的 `drawBuilding` 一致。
    /// The convention matches `drawBuilding` in `basic/gorilla_pro.bas`.
    /// </summary>
    virtual void Draw()
    {
        DrawRoof();
        DrawBody();
    }

    /// <summary>
    /// 屋顶细节（水箱 / 天线）。
    /// Roof details (water tank / antenna).
    ///
    /// ⚠ **必须在挖洞蒙版之外画**：它们的包围盒伸到楼体矩形**上方**
    /// ⚠ **They must be drawn outside the hole mask**: their bounding boxes extend **above** the building rectangle
    /// （水箱到 `y-15`、天线到 `y-22`），而那个蒙版就是"楼体矩形 − 洞" ——
    /// (the tank to `y-15`, the antenna to `y-22`), and that mask is exactly "building rectangle − holes" —
    /// 放进去会被整块裁掉。先画它、后画楼体没有差别：两者本来就不重叠
    /// inside it they would be clipped away whole. Drawing them first and the body after makes no difference: the two do not overlap anyway
    /// （细节全在 `y` 以上，洞全在 `y` 以下 —— 判据见 `HoleOnBldg`）。
    /// (the details are all above `y`, the holes all below it — the criterion is in `HoleOnBldg`).
    /// </summary>
    void DrawRoof()
    {
        int frmC = mixcol(baseR, baseG, baseB, 0, 0, 0, 45);

        // ── 屋顶细节：水箱 / 天线 / 平的 ────────────────────────────────
        // ── Roof details: water tank / antenna / flat ──
        // 按**楼号**分（`tint` 就是它在 `Layout` 里的序号），三种轮着来。
        // Chosen by **building number** (`tint` is its index in `Layout`), cycling through the three.
        //
        // ⚠ **0 号与 3 号不给** —— 那是两只猴子站的地方，加个水箱/天线正好跟猿抢位置
        // ⚠ **Numbers 0 and 3 get none** — those are where the two apes stand, and a tank/antenna there would fight the ape for the spot
        //   （BASIC 版 `drawBuilding` 里同样是 `IF bi > 0 THEN IF bi < nb1`）。
        //   (the BASIC version's `drawBuilding` likewise has `IF bi > 0 THEN IF bi < nb1`).
        //   所以实际只有中间那两栋有屋顶细节，看着正好错落。
        //   So in practice only the two middle buildings have roof details, which gives a nicely staggered look.
        //
        // 用同色系的深浅色，不要新颜色 —— 屋顶是"同一栋楼的顶"，不是另一种建筑。
        // Use shades of the same colour family, not new colours — a roof is "the top of the same building", not another kind of structure.
        if (tint == 1)
        {
            // 水箱：一个小方块 + 两条腿
            // Water tank: one small box + two legs
            ui_rect(x + 7, y - 15, 15, 11,
                    mixcol(baseR, baseG, baseB, 255, 255, 255, 10), 1, 0, 2);
            ui_line(x + 10, y - 4, x + 10, y, frmC, 2);
            ui_line(x + 19, y - 4, x + 19, y, frmC, 2);
        }
        if (tint == 2)
        {
            // 天线：一根细杆 + 两道横档。
            // Antenna: one thin pole + two crossbars.
            // ⚠ **顶上不要亮点** —— 用户一眼就把它当成"屋子上的路灯"（路灯在街上，见 `lampsDraw`）。
            // ⚠ **No bright tip on top** — the user reads it at once as "a street lamp on the house" (street lamps are on the street, see `lampsDraw`).
            ui_line(x + w - 15, y - 22, x + w - 15, y, frmC, 2);
            ui_line(x + w - 19, y - 18, x + w - 11, y - 18, frmC, 2);
            ui_line(x + w - 17, y - 12, x + w - 13, y - 12, frmC, 2);
        }
    }

    /// <summary>
    /// 楼体（体 / 受光面 / 压暗边 / 檐口 / 窗 / 门）。
    /// The building body (mass / lit face / darkened edge / cornice / windows / door).
    ///
    /// ⚠ **这一部分整个落在楼体矩形 `(x, y, w, h)` 内**，所以能直接套
    /// ⚠ **This whole part falls inside the building rectangle `(x, y, w, h)`**, so it can go straight under
    /// "楼体矩形 − 洞"的蒙版 —— 被炸开的地方直接透出背景层（云、日月都是活的）。
    /// the "building rectangle − holes" mask — blown-open places show the background layer right through (clouds, sun and moon are live).
    /// 加新的装饰时要问一句"它还在这个矩形里吗"，不在就得挪到 `DrawRoof` 去。
    /// When adding new decoration, ask "is it still inside this rectangle"; if not, it has to move to `DrawRoof`.
    /// </summary>
    /// <summary>
    /// 画本栋的**全部窗户**。`(ox, oy)` 是**楼体左上角**在目标坐标系里的位置：
    /// Draw **all windows** of this building. `(ox, oy)` is the **building's top-left corner** in the target coordinate system:
    /// 正常绘制传 `(x, y)`；录图块传 `(0, 0)`（块的局部坐标）。
    /// pass `(x, y)` for normal drawing, `(0, 0)` for block recording (the block's local coordinates).
    ///
    /// ⚠ 判据必须只用**相对楼顶**的量（`limit`）与**成员 `x`**（那个 `x / 8` 是
    /// ⚠ The criteria must use only a quantity **relative to the roof** (`limit`) and the **member `x`** (that `x / 8` is
    ///   "哪几扇窗亮着"的图案种子）—— 两者都与 `ox/oy` 无关。
    ///   the pattern seed for "which windows are lit") — neither has anything to do with `ox/oy`.
    ///   一旦写成绝对坐标，录进块的那一份就带着**录制时**的位置，贴到别处整片窗户会错位。
    ///   Write absolute coordinates and the recorded copy carries the position **at record time**, so stamping it elsewhere shifts the whole sheet of windows.
    /// </summary>
    /// 窗框色 —— **与门框同源**（门也用它），所以只在这里算一份。
    /// Window frame colour — **shared with the door frame** (the door uses it too), so it is computed once here.
    /// 窗格步进（像素）—— **按楼体缩放**，窗户数量封顶（每行 ≤10、每列 ≤14）。
    /// Window step (pixels) — **scales with the building**, with the window count capped (≤10 per row, ≤14 per column).
    /// 楼体小的时候退回原来的 18×24（与老画面一致），楼体大时才拉开。
    /// For a small building it falls back to the original 18×24 (matching the old picture) and only spreads out for a large one.
    int StepX() { int s; s = w / 10; return s < 18 ? 18 : s; }
    int StepY() { int s; s = h / 14; return s < 24 ? 24 : s; }

    /// 一扇窗的尺寸 —— 与步进同比例（原来是 18→11、24→14，这里保持同样的留白）。
    /// The size of one window — the same proportion as the step (it used to be 18→11, 24→14; the same margin is kept here).
    int WinW() { int s; s = StepX() - 7; return s < 4 ? 4 : s; }
    int WinH() { int s; s = StepY() - 10; return s < 4 ? 4 : s; }

    int FrameColor() { return mixcol(baseR, baseG, baseB, 0, 0, 0, 45); }

    /// 夜里透出来的灯光色（窗里、门缝里都是它）—— 同样只算一份。
    /// The lamp colour that shows through at night (used both in the windows and in the door gap) — likewise computed only once.
    int LitColor() { return mixcol(255, 198, 104, 226, 236, 244, gDayL); }

    /// 画本栋的**全部窗户**（逐扇）。
    /// Draw **all windows** of this building (one at a time).
    ///
    /// ⚠ **录制时走这条**（`MakeWinBlocks` 里 `DrawWindows(0, 0)`）—— 绝不能让录制去贴图块，
    /// ⚠ **This is the path recording takes** (`DrawWindows(0, 0)` inside `MakeWinBlocks`) — recording must never stamp a block,
    ///   那是拿块录块。而**播放时通常不走这条**：`DrawBody` 直接贴整栋那一张块，
    ///   which would be recording a block out of blocks. **Playback usually does not take this path**: `DrawBody` stamps the single whole-building block directly,
    ///   只有"块没录成"或"块尺寸对不上楼体"时才退到这里。
    ///   and it only falls back here when "the block was not recorded" or "the block size does not match the building".
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
        // The top of the door frame **relative to the roof** (the door spans `gy-20 .. gy`) — independent of the coordinate system
        limit = (gy - 20) - y;

        cols = w / StepX();
        if (cols < 1) { cols = 1; }
        rows = h / StepY();
        if (rows < 1) { rows = 1; }
        c = 0;
        while (c < cols)
        {
            r = 0;
            while (r < rows)
            {
                wx = ox + 6 + c * StepX();
                wy = oy + 10 + r * StepY();
                // ⚠ **一楼只开门、不开窗** —— 原来窗是整列均匀铺到底的，最后一行正好
                // ⚠ **The ground floor gets a door but no windows** — the windows used to be laid out evenly down the whole column, and the last row landed
                //   压在门上（门在 `gy-20 .. gy`），玩家看到的就是"窗户和门重合"。
                //   right on the door (the door spans `gy-20 .. gy`), so the player saw "the windows and the door overlapping".
                //   压在门那一段的窗**一格都不画**。
                //   Windows falling in the door's range are **not drawn at all**.
                if (wy + WinH() - oy <= limit)
                {
                    // ⚠ 图案种子用**成员 `x`**（楼的绝对位置）而不是 `ox` ——
                    // ⚠ The pattern seed uses the **member `x`** (the building's absolute position), not `ox` —
                    //   这样录块与直接画得到的是同一片"哪几扇亮着"。
                    //   that way recording a block and drawing directly produce the same "which windows are lit" pattern.
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
    /// Record this building's windows into blocks **stepped by the day/night cycle** (called once after the window opens and once after each `Layout()`).
    ///
    /// ⚠ 为什么值得录：窗户是一帧图元的大头（5~6 栋 × 几十扇 × 2 个矩形 ≈ 400 个，
    /// ⚠ Why it is worth recording: windows are the bulk of a frame's figures (5..6 buildings × dozens of windows × 2 rectangles ≈ 400,
    ///   占全帧七成）。真机实测**程序侧线程吃满一个核（89% CPU）**，而那一半的开销
    ///   about 70% of the frame). Measured on device, **the program-side thread saturates a core (89% CPU)**, and half of that cost
    ///   正是"逐条发出绘图调用" —— 录成块之后一栋楼每帧只发**一次** `ui_draw_block`。
    ///   is exactly "issuing drawing calls one by one" — with blocks recorded, a building issues **one** `ui_draw_block` per frame.
    ///   （宿主贴块时会把这些指令重放出来，所以**屏幕上的图元数不变**：
    ///   (The host replays those instructions when stamping, so **the number of figures on screen is unchanged**:
    ///     省掉的是"程序 → 宿主"的往返，不是"宿主 → 屏幕"的绘制。）
    ///     what is saved is the "program → host" round trip, not the "host → screen" drawing.)
    ///
    /// ⚠ 用**局部坐标**录（`DrawWindows(0, 0)`）⇒ 块与楼的**位置**无关；
    /// ⚠ Record with **local coordinates** (`DrawWindows(0, 0)`) ⇒ the block is independent of the building's **position**;
    ///   但块尺寸是 `w×h`，与楼的**宽高**有关 ⇒ 每局楼宽高都变，**每次 Layout 都要重录**。
    ///   but the block size is `w×h`, which depends on the building's **width and height** ⇒ those change every round, so **every Layout must re-record**.
    /// ⚠ 录制期间要临时改 `gDayL`（颜色都由它插值），**录完必须恢复** ——
    /// ⚠ Recording temporarily changes `gDayL` (all colours interpolate from it), and **it must be restored afterwards** —
    ///   与云的 `MakeBlocks` 同一条理由（留着档位值会让这一帧的天色不对）。
    ///   the same reasoning as the clouds' `MakeBlocks` (leaving the step value in place makes this frame's sky colour wrong).
    /// </summary>
    void MakeWinBlocks()
    {
        int s;
        int save;
        int h0;

        // ── 先释放上一局录的 ────────────────────────────────────────────────
        // ── First free the ones recorded last round ──
        // ⚠ 楼基色每局都变（`SetTint` 在 `Layout` 里调），而**块的内容与尺寸都不可变**
        // ⚠ The building base colour changes every round (`SetTint` is called in `Layout`), while **a block's content and size are immutable**
        //   ⇒ 只能重录。不释放就是每局漏 6 栋 × 4 档 × 2 = 48 个句柄，
        //   ⇒ re-recording is the only option. Not freeing leaks 6 buildings × 4 steps × 2 = 48 handles per round,
        //   **三局就撑满 128**；撑满之后 `ui_create_block` 一律返回 0，画面悄悄退回逐帧画
        //   and **three rounds fill up 128**; after that `ui_create_block` always returns 0 and the picture quietly falls back to per-frame drawing
        //   （慢，且看不出"块没了"）。`ui_free_block` 就是为这种"内容会变"的用法准备的。
        //   (slow, and with no visible sign that "the blocks are gone"). `ui_free_block` exists exactly for this kind of "the content will change" usage.
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
            // ⚠⚠ **If it cannot be created, leave the handle 0 and fall back to per-window drawing**. When the block table is full `ui_create_block` returns 0
            //   并且**不进入录制态** —— 此时若照常调 `DrawWindows`，那些窗户会
            //   and **does not enter recording mode** — and if `DrawWindows` is then called as usual, those windows will
            //   **直接落到画布上**（局部坐标 (0,0) 就是画布原点），实测现象是
            //   **land straight on the canvas** (local coordinates (0,0) are the canvas origin), the measured symptom being
            //   "一片窗户飘在天上"。这条是踩过的坑，别当成多余的防御删掉。
            //   "a sheet of windows floating in the sky". This is a hole we have already fallen into; do not delete it as redundant defence.
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
                // Record **the building size at the time this block was recorded** — `DrawBody` compares it against the current building before stamping,
                // 对不上就不贴（那正是"整栋窗户一起偏出去"的入口）。
                // and does not stamp when they differ (that is exactly the doorway to "the whole building's windows shifting off together").
                winBW[s] = w;
                winBH[s] = h;
            }

            gDayL = save;
            s = s + 1;
        }
    }

    /// 当前天光对应的窗户档位 —— 录制与贴图**共用这一处**换算式
    /// The window step for the current daylight — recording and stamping **share this one** conversion
    /// （两处各写一遍就是"同一规则两处实现"，迟早漂）。
    /// (writing it in two places would be "the same rule implemented twice", which drifts sooner or later).
    int WinSlot()
    {
        int s;
        s = gDayL * WIN_DAY_STEPS / 101;
        if (s < 0) { s = 0; }
        if (s >= WIN_DAY_STEPS) { s = WIN_DAY_STEPS - 1; }
        return s;
    }

    /// 画**一扇窗**：`(bx, by)` 是**窗框左上角**（世界坐标；录图块时传 (0,0)）。
    /// Draw **one window**: `(bx, by)` is the **frame's top-left corner** (world coordinates; pass (0,0) when recording a block).
    ///
    /// ⚠ 录制与"没录成时的逐扇画"**共用这一份** —— 两处各画一遍，
    /// ⚠ Recording and "the per-window fallback when recording failed" **share this one copy** — draw it in two places
    ///   录出来的块与直接画的必然长得不一样（那种差异只有肉眼能发现）。
    ///   and the recorded block and the direct drawing are bound to look different (a difference only the eye can find).
    void DrawOneWindowAt(int bx, int by, int lit)
    {
        int ww;
        int wh;
        ww = WinW();
        wh = WinH();
        ui_rect(bx, by, ww, wh, FrameColor(), 1, 0, 0);
        ui_rect(bx + 1, by + 1, ww - 2, wh - 2,
                lit != 0 ? LitColor() : mixcol(14, 14, 24, 52, 74, 96, gDayL), 1, 0, 0);
    }

    void DrawBody()
    {
        int bodyC;
        int hiC;
        int edgeC;
        int slot;

        // 楼体：夜色 = **同一个色相压到 26% 再偏一点冷蓝**，按天光在两者之间插值。
        // Body: night colour = **the same hue pressed to 26% and nudged a little cold blue**, interpolated by daylight between the two.
        // ⚠ 原来压到 16% 且不加蓝 ⇒ 夜里几乎全黑，四栋楼糊成一片分不出彼此；
        // ⚠ It used to be pressed to 16% with no blue ⇒ almost fully black at night, the four buildings blurring into one indistinguishable mass;
        //   月光本身是冷的，给夜景补 18 点蓝分量，暗是暗、但"看得见是几栋楼"。
        //   moonlight is cold anyway, so the night scene gets 18 points of blue: still dark, but "you can see how many buildings there are".
        bodyC = mixcol(baseR * 26 / 100, baseG * 26 / 100, baseB * 26 / 100 + 18,
                       baseR, baseG, baseB, gDayL);
        ui_rect(x, y, w, h, bodyC, 1, 0, 0);

        // 受光面：左侧 3px 亮一点（白天更明显）；右侧 2px 压暗 —— 楼才有体积感
        // Lit face: the left 3px a touch brighter (more obvious by day); the right 2px darkened — that is what gives the building volume
        hiC = mixcol(baseR, baseG, baseB, 255, 255, 255, 6 + gDayL * 12 / 100);
        edgeC = mixcol(baseR, baseG, baseB, 0, 0, 0, 14 + gDayL * 10 / 100);
        ui_rect(x, y, 3, h, hiC, 1, 0, 0);
        ui_rect(x + w - 2, y, 2, h, edgeC, 1, 0, 0);

        // 檐口：比楼体亮一档（白天像被阳光打亮，夜里像被月光勾了一道边）
        // Cornice: one step brighter than the body (by day it looks sunlit, at night like a moonlit edge)
        ui_rect(x, y, w, 4, mixcol(baseR, baseG, baseB, 255, 255, 255, 18 + gDayL * 22 / 100), 1, 0, 0);

        // ── 窗：贴**整栋一张块**（内容由 `DrawWindows(0,0)` 录进 `MakeWinBlocks`）──
        // ── Windows: stamp **one block for the whole building** (its content is recorded by `DrawWindows(0,0)` in `MakeWinBlocks`) ──
        //
        // ⚠⚠ **贴之前必须校验块尺寸 == 当前楼体尺寸**。块是"w×h 的整栋窗户"，
        // ⚠⚠ **Before stamping, the block size must be verified == the current building size**. The block is "the whole building's windows at w×h",
        //   贴的时候靠"块中心 = 楼体中心"对齐 —— 尺寸一旦不一致，**整栋楼的窗户会
        //   and stamping aligns by "block centre = building centre" — once the sizes disagree, **the whole building's windows shift
        //   一起偏出楼体**（用户报的「窗户铺到房子外面」）。所以对不上就**不贴**、
        //   off the building together** (the user's "the windows are spread outside the house"). So on a mismatch, **do not stamp**
        //   退回逐扇画：宁可慢，不能让一栋楼的窗户整体挪到别处去。
        //   and fall back to per-window drawing: better slow than let a building's windows move somewhere else wholesale.
        //   这道校验取代了"一窗一块"那种结构（后者每扇窗各贴一次、每帧 84 次调用，
        //   This check replaces the "one block per window" structure (which stamps each window separately, 84 calls per frame,
        //   真机实测慢 21%：23.8fps → 18.7fps）。
        //   measured 21% slower on device: 23.8fps → 18.7fps).
        //
        // ⚠ 没录成（还没有场景 / 本局还没录 / 块表满）时同样退回逐扇画：不能没窗户。
        // ⚠ When recording failed (no scene yet / not recorded this round / the block table is full) it likewise falls back to per-window drawing: there must be windows.
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
        // Door (at the base of the building). ⚠ Being covered by a crater is correct — the crater means "blown away".
        // 门框 + 门板 + 底下透出来的一条亮光（屋里有人，门就"活"了）
        // Frame + panel + a strip of light showing underneath (someone is home, so the door is "alive")
        ui_rect(x + w / 2 - 9, gy - 20, 18, 20, FrameColor(), 1, 0, 3);
        ui_rect(x + w / 2 - 7, gy - 18, 14, 18, mixcol(baseR, baseG, baseB, 0, 0, 0, 60), 1, 0, 2);
        // 门缝里透出来的一条灯光 —— 比整扇门发亮自然，也不会在白天显得奇怪
        // A strip of lamplight through the door gap — more natural than lighting the whole door, and it does not look odd by day
        ui_rect(x + w / 2 - 7, gy - 7, 14, 2, LitColor(), 1, 0, 0);
    }
};

/// <summary>
/// 这个洞开在第 `k` 栋楼的**墙上**吗？
/// Is this hole in the **wall** of building `k`?
///
/// 判据三条，缺一不可：① 横向落在这栋楼的范围内；② 在楼顶**以下**（楼顶以上是屋面，
/// Three criteria, all required: ① it falls within this building horizontally; ② it is **below** the roof (above the roof is roof surface,
/// 不是墙）；③ 在地平线**以上**（地面以下的坑是"挖土"，走 `holesDraw` 那条路，
/// not wall); ③ it is **above** the horizon (a crater below ground level is "digging", the `holesDraw` path,
/// 不该从楼里透出天空）。
/// and should not show sky through a building).
///
/// ⚠ 抽成函数是因为"墙上的洞"现在**有两个消费者**：挖洞的蒙版（建筑层）与
/// ⚠ It is a function because "a hole in a wall" now has **two consumers**: the hole mask (the building layer) and
///   挖洞的蒙版（建筑层）与地上那批坑的分流。各写一遍就是"同一规则两处实现" ——
///   the hole mask (the building layer) and the split of the ground craters. Writing it twice would be "the same rule implemented twice" —
///   哪天判据一改，必有一处忘了跟。
///   change the criterion one day and one of them is bound to be forgotten.
///   （原先还有第三个消费者"洞口的断面"，那个已经删了 —— 见 `Game::Draw` 里那段说明。）
///   (There used to be a third consumer, the "crater rim cross-section", which has been deleted — see the explanation in `Game::Draw`.)
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
// ⚠ `holesDraw` and `isqrt` **must come after `Building`**: this front end does no forward references,
//   而这里要用 `Building` 的 `Covers/Left/Right/RoofY`、还要用颜色宏 `C_DIRT`，
//   and this code needs `Building`'s `Covers/Left/Right/RoofY` plus the colour macro `C_DIRT`,
//   那两样都定义在后面。（第一版插在 `addHole` 后面，直接编不过。）
//   both of which are defined later. (The first version sat after `addHole` and simply would not compile.)
// 整数平方根（牛顿迭代）—— 凿洞时要按行算半宽 `sqrt(r^2 - dy^2)`。
// Integer square root (Newton iteration) — carving a hole needs the per-row half width `sqrt(r^2 - dy^2)`.
// 平台没有 sqrt 可用，就这几行，自己带一个。
// The platform has no `sqrt` available, it is only a few lines, so we carry our own.
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
/// **Clamp** a hole into the building's extent (centre inside the building, the whole circle within [roof, ground)).
///
/// ⚠ 这是为了让"楼 − 洞"能被**矢量后端折叠成一条路径**。折叠不成立时整个窗口会退回
/// ⚠ This is so that "building − holes" can be **folded into a single path by the vector back end**. When the fold does not hold, the whole window falls back
///   光栅后端 —— 画面立刻变糊、帧率从几十掉到 6（真机实测 6.2fps）。
///   to the raster back end — the picture immediately goes blurry and the frame rate drops from tens to 6 (measured 6.2fps on device).
///   洞一旦探出楼外，"楼外那块洞"在 even-odd 下只穿过一次 ⇒ 被判成**内部**
///   Once a hole pokes outside the building, "the part of the hole outside it" is crossed only once under even-odd ⇒ it is judged **inside**
///   ⇒ 画出一块不该有的肉，所以宿主的折叠判据只能**拒**。
///   ⇒ producing a lump of fill that should not be there, so the host's fold criterion can only **reject** it.
///
/// ⚠ 楼比洞还窄/还矮时先把半径压到"装得下" —— 否则下面两组钳制条件会互相矛盾
/// ⚠ When the building is narrower/shorter than the hole, first squeeze the radius until it "fits" — otherwise the two clamp conditions below contradict each other
///   （左边推右、右边推左），圆照样探出去。
///   (the left pushes right, the right pushes left) and the circle pokes out anyway.
static void ClampHoleToBuilding(int* px, int* py, int* pr,
                                int left, int right, int roofY, int groundY)
{
    int x = *px;
    int y = *py;
    int r = *pr;

    // ⚠ **优先缩半径、保住圆心**。
    // ⚠ **Shrink the radius first, keep the centre**.
    //   圆心是"炸在哪"的忠实记录 —— 玩家看得出弹着点，**洞挪了地方比洞小一圈刺眼得多**
    //   The centre is a faithful record of "where it exploded" — the player can see the impact point, and **a hole that has moved is far more jarring than a hole that is a bit smaller**
    //   （第一版直接钳圆心，玩家的原话是"剪切的洞位置有点怪"）。
    //   (the first version clamped the centre directly; the player's words were "the clipped hole's position is a bit odd").
    //
    // ⚠⚠ **只钳三个方向：左、右、楼顶。地面那个方向不用管** ——
    // ⚠⚠ **Clamp only three directions: left, right, roof. The ground direction does not need it** —
    //   洞的下缘压到地面以下是**无害**的：地面是**后画**的（楼之后才画那条地面带），
    //   a hole's lower edge reaching below the ground is **harmless**: the ground is drawn **later** (that ground strip is drawn after the buildings)
    //   会把多出来的那块盖住。而把它算进半径上限的后果是**灾难性的**：
    //   and covers the excess. Counting it into the radius cap, however, is **disastrous**:
    //   洞本来就常落在楼的下半部、离地面不远，一算就只剩十几像素，
    //   holes usually land in the lower half of a building, not far from the ground, so the calculation leaves only a dozen pixels,
    //   再叠上"横向也贴边"就成了 3px 的小点 —— 玩家的原话是「**炸不动楼房了**」。
    //   and combined with "also against the horizontal edge" that becomes a 3px dot — the player's words were "**it cannot blow up the buildings any more**".
    int hLimit = x - (left + 1);
    if ((right - 1) - x < hLimit) { hLimit = (right - 1) - x; }
    int vLimit = y - (roofY + 1);
    int lim = hLimit < vLimit ? hLimit : vLimit;

    // 半径下限 8：再小就看不出是个洞了（"炸不动"就是这么来的）。
    // Minimum radius 8: any smaller and it stops reading as a hole (that is where "it cannot blow anything up" came from).
    // 装得下就**只缩半径、圆心一动不动**；实在贴边（连 8 都放不下）才挪圆心。
    // If it fits, **shrink only the radius and leave the centre untouched**; only when it really is against the edge (even 8 does not fit) does the centre move.
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
    // The ground direction is **deliberately not clamped** (see the passage above)

    *px = x;
    *py = y;
    *pr = r;
}

/// 两个圆合并成**包含它们的最小圆**（同心的那种包含关系直接取大的）。
/// Merge two circles into **the smallest circle containing them** (for the concentric containment case, just take the bigger one).
///
/// ⚠ 为什么必须合并：折叠"楼 − 洞"要求**洞与洞互不重叠** —— 重叠处在 even-odd 下
/// ⚠ Why merging is required: folding "building − holes" requires **holes not to overlap each other** — under even-odd an overlap
///   会被填实（NonZero 也一样，这是"一条路径 + 填充规则"的数学限制，绕不过去）。
///   gets filled in (NonZero behaves the same; it is a mathematical limit of "one path + a fill rule" and there is no way around it).
///   而观感上合并反而更对：炸得太密，破洞本来就该连成一片。
///   And visually the merge is actually more correct: when the shelling is dense, the holes should join into one.
static void MergeHoles(int ax, int ay, int ar, int bx, int by, int br,
                       int* ox, int* oy, int* orr)
{
    int dx = bx - ax;
    int dy = by - ay;
    int d = isqrt(dx * dx + dy * dy);
    if (d + br <= ar) { *ox = ax; *oy = ay; *orr = ar; return; }   // a 已经把 b 包住了
    // a already contains b
    if (d + ar <= br) { *ox = bx; *oy = by; *orr = br; return; }   // b 把 a 包住了
    // b contains a
    int R = (d + ar + br) / 2;
    if (d == 0) { *ox = ax; *oy = ay; *orr = R; return; }
    int t = R - ar;                       // 新圆心沿 a→b 方向走这么远
    // The new centre moves this far along a→b
    *ox = ax + dx * t / d;
    *oy = ay + dy * t / d;
    *orr = R;
}

// ── 弹坑：**在墙上真正凿一个洞**（而不是盖一个天空色的圆）──────────────
// ── Craters: **actually carve a hole in the wall** (rather than laying down a sky-coloured circle) ──
//
// ⚠ 原先就是在坑的位置盖一个**天空色的实心圆**，玩家在手机上点出了两个毛病：
// ⚠ Originally a **solid sky-coloured circle** was laid over the crater's position, and on the phone the player found two problems with it:
//   ① 那一趟画在**香蕉之后** ⇒ 香蕉飞过坑口被盖住，**看着像撞上一块看不见的墙**；
//   ① that pass was drawn **after the banana** ⇒ a banana flying past the crater mouth got covered, **looking like it hit an invisible wall**;
//   ② 一整块**平色**盖上去 ⇒ 天空是渐变的，坑里那块色对不上；
//   ② one flat **solid colour** laid over it ⇒ the sky is a gradient, so the colour inside the crater did not match;
//      而且它会把**后面的云 / 日月一起盖掉** —— 那些东西本来是"透过洞该看见"的。
//      and it also **covered the clouds / sun / moon behind it** — things that were supposed to be visible through the hole.
//
// 现在的做法（平台**没有** clip/mask 接口，所以这一层只能程序自己裁剪）：
// The current approach (the platform has **no** clip/mask interface, so this layer has to clip for itself):
//   · **逐行凿**：每行一条 1 像素高的横条，半宽按 `sqrt(r² - dy²)` 算 ⇒ 出来是圆的；
//   · **carve row by row**: one 1-pixel-high strip per row, half width from `sqrt(r² - dy²)` ⇒ the result is round;
//   · **每行单独取色**（`clockPartsAt(y)`）⇒ 天空渐变在洞里自然接得上，没有补丁感；
//   · **take the colour per row** (`clockPartsAt(y)`) ⇒ the sky gradient continues naturally inside the hole, with no patchwork feel;
//   · **横向夹到所在那栋楼的范围内** —— 这就是"裁剪"，洞不会糊到隔壁楼上去；
//   · **clamp horizontally to the building it belongs to** — that is the "clipping", so the hole does not smear onto the neighbouring building;
//   · **纵向夹在楼顶与地面之间** —— 洞不会翻到楼顶外面去。
//   · **clamp vertically between the roof and the ground** — the hole cannot spill over the roof.
//   · 地上的坑**涂土色而不是天空色**（原来地上也是个蓝洞，那显然不对）。
//   · ground craters are **painted with the dirt colour, not the sky colour** (they used to be blue holes in the ground, which is clearly wrong).
//
// ⚠ 调用时机也是修的一部分：必须夹在**楼之后、香蕉之前**（见 `Game::Draw`）。
// ⚠ The call timing is part of the fix too: it must sit **after the buildings and before the banana** (see `Game::Draw`).
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
        // Which **building** is this crater on? (covered by none ⇒ it is a crater in the ground)
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
                // ⚠ **Holes in buildings are not drawn here** — see that part of `Game::Draw`: there a **mask**
                //   把背景**重画一遍**，洞后面才是"活"的（云、日月会从洞里露出来）。
                //   **redraws the background**, which is what makes what is behind the hole "live" (clouds, sun and moon show through it).
                //   这里只负责**地上的坑**（挖土）。
                //   This function handles only **ground craters** (digging).
                if (y >= groundY)
                {
                    // 地上的坑：挖土
                    // Ground crater: dig
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
// Ape — the monkey (angle / power / score / which way it faces)
// ════════════════════════════════════════════════════════════════════

class Ape : public Entity
{
public:
    int angle;           // 0..90 度，相对"朝对手那一边"的水平线
    // 0..90 degrees, relative to the horizontal line "toward the opponent"
    int power;           // 10..100
    int score;
    int flip;            // 0 = 朝右（左边那只），1 = 朝左（右边那只）
    // 0 = faces right (the left one), 1 = faces left (the right one)
    int body;            // 颜色
    // Colour
    int dead;            // 1 = 被飞碟的激光打死（画成焦黑、不再举手臂）
    // 1 = killed by the UFO's laser (drawn charred, the arm no longer raised)

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
    // Dragging the bar sets the value directly (the clamping shares its source with `Aim`/`Boost`; do not write it a second time on the panel side)
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
    // ── Arm: shoulder point → hand ──
    //
    // ⚠⚠ 这四个函数是**画手臂**与**算出手点**共用的唯一真源。
    // ⚠⚠ These four functions are the single source of truth **shared by drawing the arm and computing the launch point**.
    //   原来两边各写各的：`Draw` 从躯干中心 `(x, y-24)` 画一条 `len=26` 的斜线，
    //   They used to be written separately: `Draw` drew a line of `len=26` from the torso centre `(x, y-24)`,
    //   而 `HandX/HandY` 写死 `x±18, y-20` —— 于是
    //   while `HandX/HandY` hard-coded `x±18, y-20` — with the result that
    //     · 手臂短到**手掌贴在脸上**（手掌到头的距离只有 1.4px）；
    //     · the arm was so short that **the palm sat on the face** (only 1.4px from the palm to the head);
    //     · 香蕉从**胸口**飞出去，手上空空。
    //     · the banana flew out of the **chest**, leaving the hand empty.
    //   玩家报的「猴子的手臂外观有点奇怪」就是这两条。
    //   The player's "the monkey's arm looks a bit odd" was exactly these two.
    //   现在改一处两边都对 —— 再出现"手和香蕉不在一起"，先看这里。
    //   Now one change fixes both — if "the hand and the banana are not together" ever comes back, look here first.
    //
    // 肩点取**躯干外缘**（躯干是 `x±14` 的圆角矩形），不是躯干中心：
    // The shoulder point is taken at the **torso's outer edge** (the torso is a rounded rectangle at `x±14`), not the torso centre:
    // 从中心出发的胳膊读出来是"从胸口斜插出来的一截"。
    // an arm starting from the centre reads as "a stub sticking out of the chest at an angle".
    int ArmRootX()
    {
        return x + 14 * (1 - 2 * flip);
    }

    int ArmRootY()
    {
        return y - 26;
    }

    /// 出手点（手臂末端）。**方向按 `angle` 算** —— 那是玩家调的角度，得看得见。
    /// The launch point (the arm's end). **The direction comes from `angle`** — that is the angle the player set, and it has to be visible.
    int HandX()
    {
        return ArmRootX() + icos(angle) * ARM_LEN / SIN_SCALE * (1 - 2 * flip);
    }

    int HandY()
    {
        return ArmRootY() - isin(angle) * ARM_LEN / SIN_SCALE;
    }

    // 把初速写进香蕉（定点单位/拍）。`dir` 由 flip 决定。
    // Write the initial velocity into the banana (fixed-point units per tick). `dir` comes from `flip`.
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
    // Hit test: based on the body centre
    int Hits(int bx, int by)
    {
        if (iabs(bx - x) < 16 && iabs(by - (y - 12)) < 22) { return 1; }
        return 0;
    }

    /// 被打死的样子：**焦黑**的一团 + 三缕余烟。
    /// How it looks when shot dead: one **charred** lump + three wisps of leftover smoke.
    ///
    /// ⚠ 不做成"消失"：玩家得看见"我的猴子没了"才明白这一局是怎么结束的。
    /// ⚠ It is not made to "disappear": the player has to see "my monkey is gone" to understand how the round ended.
    ///   结束语在屏幕正中的横幅上（`Game::Draw`），可玩家的视线一直在自己那只猴子这边 ——
    ///   The closing words are on the banner in the middle of the screen (`Game::Draw`), but the player's gaze stays on their own monkey —
    ///   两处都要有交代，只写横幅的话会读成"画面卡住了"。
    ///   both places have to say something; a banner alone reads as "the picture froze".
    ///
    /// ⚠ 轮廓与活着的猴子**同一套坐标**（腿 / 躯干 / 头 / 耳朵 / 眉骨）：轮廓一变
    /// ⚠ The outline uses the **same coordinates as a live monkey** (legs / torso / head / ears / brow ridge): change the outline
    ///   就认不出"这是刚才那只猴子"，而认不出来等于没交代。
    ///   and nobody recognises "that is the monkey from a moment ago", and not recognising it is as good as saying nothing.
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
        // The brow ridge stays where it was, but the eyes become two ✕ marks — "stone dead" has to be obvious at a glance, not something you guess at
        ui_rect(x - 9, y - 47, 18, 4, soot, 1, 0, 2);
        ui_line(x - 7, y - 45, x - 3, y - 41, soot, 2);
        ui_line(x - 3, y - 45, x - 7, y - 41, soot, 2);
        ui_line(x + 3, y - 45, x + 7, y - 41, soot, 2);
        ui_line(x + 7, y - 45, x + 3, y - 41, soot, 2);

        // 三缕余烟。**不带时间参数** —— 这一局已经结束了，动不动的没人再看；
        // Three wisps of leftover smoke. **No time parameter** — the round is already over and nobody is watching whether it moves;
        // 而引入一个计时变量就多一处"重开时忘了归零"的隐患。
        // and adding a timer variable creates one more place where "somebody forgets to reset it on restart".
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
        // Shoulder point (the root of the arm)
        int ay;
        int hx;              // 手（= 香蕉的出手点）
        // Hand (= the banana's launch point)
        int hy;
        int nb;              // 镜像方向：+1 朝右、-1 朝左（后边那只胳膊用）
        // Mirror direction: +1 right, -1 left (used by the arm behind)
        int dark;
        int light;

        if (dead != 0) { DrawDead(); return; }

        // ⚠ **描边色与浅色都由本体色算出来，不写死**。
        // ⚠ **The outline colour and the light colour are both derived from the body colour, never hard-coded**.
        //   原来写死 `C_APE_DARK`（深棕），橙猴子看着还行，**紫猴子配深棕是发闷的** ——
        //   They used to be hard-coded to `C_APE_DARK` (dark brown); that looked acceptable on the orange ape, but **dark brown with the purple ape is muddy** —
        //   玩家当时的原话是「猴子没有轮廓」。由本体色推出来的描边在任何配色下都成立。
        //   the player's words at the time were "the monkey has no outline". An outline derived from the body colour works for any colour scheme.
        dark = mixcol(colR(body), colG(body), colB(body), 12, 8, 6, 62);
        light = mixcol(colR(body), colG(body), colB(body), 255, 245, 235, 45);

        // ── 腿脚（先画，躯干盖住上沿）────────────────────────────
        // ── Legs and feet (drawn first; the torso covers their top edge) ──
        ui_rect(x - 12, y - 9, 11, 9, body, 1, 0, 3);
        ui_rect(x + 1, y - 9, 11, 9, body, 1, 0, 3);
        ui_rect(x - 12, y - 9, 11, 9, dark, 0, 2, 3);
        ui_rect(x + 1, y - 9, 11, 9, dark, 0, 2, 3);

        // ── 后边那只胳膊（垂着）──────────────────────────────────
        // ── The arm behind (hanging down) ──
        //
        // ⚠ **必须跟着 `flip` 镜像**（`nb` 就是那个 ±1）。原来这条胳膊写死在左边，
        // ⚠ **It must mirror with `flip`** (`nb` is that ±1). This arm used to be hard-coded on the left,
        //   而举起来那只跟着 `flip` 走 ⇒ 右边的紫猴**两只胳膊都在左边** ——
        //   while the raised one followed `flip` ⇒ the purple ape on the right had **both arms on the left** —
        //   玩家报的"两只手都是左手"就是这个：一只镜像了、另一只没镜像。
        //   that is exactly the player's "both hands are left hands": one was mirrored and the other was not.
        //   一条画两只胳膊的路径上只镜像一半，是本仓最典型的一类缺陷。
        //   Mirroring only half of a path that draws two arms is the most typical class of defect in this repo.
        // ⚠ 往外多伸 2px（末端 `x±18` 而不是 `x±16`）：躯干到 `x±14`，原来只有
        // ⚠ It reaches 2px further out (ending at `x±18` instead of `x±16`): the torso runs to `x±14`, so previously only
        //   2px 的胳膊露在外面，看上去是"身侧挂着一个圆"而不是一条手臂。
        //   2px of arm showed, looking like "a circle hanging at the side" rather than an arm.
        nb = 1 - 2 * flip;
        ui_line(x - 12 * nb, y - 26, x - 18 * nb, y - 10, dark, 9);
        ui_line(x - 12 * nb, y - 26, x - 18 * nb, y - 10, body, 6);
        ui_circle(x - 18 * nb, y - 10, 5, body, 1, 0);
        ui_circle(x - 18 * nb, y - 10, 5, dark, 0, 2);

        // ── 躯干：宽肩（大猩猩的体型就是"肩膀比头宽"）────────────
        // ── Torso: broad shoulders (a gorilla's build is "shoulders wider than the head") ──
        ui_rect(x - 14, y - 30, 28, 23, body, 1, 0, 9);
        ui_rect(x - 14, y - 30, 28, 23, dark, 0, 2, 9);
        // 浅色胸腹 —— 有它才不会读成"一块方砖"
        // A lighter chest and belly — without it the torso reads as "a square brick"
        ui_ellipse(x, y - 20, 8, 9, light, 1, 0);

        // ── 头：耳朵 → 头 → 眉骨 → 吻部 → 眼 ─────────────────────
        // ── Head: ears → head → brow ridge → muzzle → eyes ──
        ui_circle(x - 11, y - 39, 5, body, 1, 0);
        ui_circle(x + 11, y - 39, 5, body, 1, 0);
        ui_circle(x - 11, y - 39, 5, dark, 0, 2);
        ui_circle(x + 11, y - 39, 5, dark, 0, 2);
        ui_circle(x, y - 40, 12, body, 1, 0);
        ui_circle(x, y - 40, 12, dark, 0, 2);
        // 眉骨：一条压低的横条 —— **这一笔是"看出是猩猩"的关键**，
        // Brow ridge: a low, heavy horizontal bar — **this one stroke is the key to reading it as an ape**,
        //   光有一个圆头，放大到手机屏幕上就是个球。
        //   since a round head alone, blown up on a phone screen, is just a ball.
        ui_rect(x - 9, y - 47, 18, 4, dark, 1, 0, 2);
        // 吻部
        // Muzzle
        ui_ellipse(x, y - 33, 7, 5, light, 1, 0);
        ui_ellipse(x, y - 33, 7, 5, dark, 0, 2);
        // 眼睛（朝对手那边）
        // Eyes (facing the opponent)
        if (flip != 0)
        {
            ui_circle(x - 5, y - 42, 2, C_TEXT, 1, 0);
        }
        else
        {
            ui_circle(x + 5, y - 42, 2, C_TEXT, 1, 0);
        }

        // ── 举起来那只胳膊（按角度画）—— 玩家看得见自己调的角度 ──
        // ── The raised arm (drawn from the angle) — the player can see the angle they set ──
        //
        // ⚠ 端点一律取自 `HandX/HandY`（与香蕉的出手点是同一个函数）——
        // ⚠ The endpoints always come from `HandX/HandY` (the same functions as the banana's launch point) —
        //   见那组函数上面的说明：分开算过一次，代价是"手在脸上、香蕉从胸口飞"。
        //   see the explanation above that group: they were computed separately once, at the cost of "hand on the face, banana flying from the chest".
        ax = ArmRootX();
        ay = ArmRootY();
        hx = HandX();
        hy = HandY();
        ui_line(ax, ay, hx, hy, dark, 10);      // 先粗的深色当描边
        // The thick dark line as an outline first
        ui_line(ax, ay, hx, hy, body, 6);       // 再细的本体色
        // Then the thinner body colour
        // 手掌要比手臂**明显**粗（半径 7 对线半宽 3）—— 只粗一点点的话，
        // The palm has to be **clearly** thicker than the arm (radius 7 against a half line-width of 3) — only slightly thicker
        // 末端读出来是"一根棍子的圆头"，不是"手"。
        // and the end reads as "the round tip of a stick" rather than a "hand".
        ui_circle(hx, hy, 7, body, 1, 0);
        ui_circle(hx, hy, 7, dark, 0, 2);
    }
};

// ════════════════════════════════════════════════════════════════════
// Banana —— 香蕉（定点弹道）
// Banana — the banana (fixed-point trajectory)
// ════════════════════════════════════════════════════════════════════

// ── 速度方向 → 姿态角（度）──────────────────────────────────────────────
// ── Velocity direction → attitude angle (degrees) ──
//
// 用**菱形近似**算 atan2（那条 `45·ay/ax`），误差最大约 4.5° —— 香蕉只要"大致顺着
// atan2 is approximated **by the diamond method** (that `45·ay/ax`), with a maximum error of about 4.5° — the banana only needs
// 弹道"，而本平台上 `sqrt`/`sin`/`cos` 都不可用（见下面 `isqrt` 那段说明），
// to "roughly follow the trajectory", and this platform has no `sqrt`/`sin`/`cos` (see the note at `isqrt` below),
// 为几度的精度去引一条数学库依赖不值当。
// so pulling in a maths library dependency for a few degrees of accuracy is not worth it.
//
// 角度口径与 `basic/gorilla_pro.bas` 的 `bananaAngle` 一致（**屏幕 y 向下**）：
// The angle convention matches `bananaAngle` in `basic/gorilla_pro.bas` (**screen y points down**):
// 向右上飞是**负角**、向右下飞是正角。两版用同一套，观感才对得上。
// flying up-right is a **negative angle**, flying down-right is positive. Both versions use the same scheme, so they feel alike.
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
    // Fixed-point position
    int by;
    int vx;              // 定点速度（每拍）
    // Fixed-point velocity (per tick)
    int vy;
    int live;
    int owner;           // 谁扔的（0/1）
    // Who threw it (0/1)
    int trailN;
    int bid;             // 月牙**图块**句柄（开窗后建一次，见 `MakeBlock`）
    // Crescent **block** handle (created once at window open, see `MakeBlock`)
    int spin;            // 飞行中的自转累计角（度）—— 见 `Draw`
    // Accumulated spin while flying (degrees) — see `Draw`

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
    /// Record the crescent as a **block** (called once after the window opens).
    ///
    /// ⚠ 为什么不每帧直接画路径：`ui_path` 收的是**绝对场景坐标**，没有任何变换参数 ——
    /// ⚠ Why not just draw the path every frame: `ui_path` takes **absolute scene coordinates** and has no transform parameters at all —
    /// 要让月牙顺着弹道转，就得把角度算进每一段贝塞尔的控制点里，那是一场噩梦。
    /// to make the crescent rotate with the trajectory, the angle would have to be folded into every Bezier control point, which is a nightmare.
    /// 图块正好补上这个缺口：形状随便画（曲线都行），贴的时候 `ui_draw_block` 带旋转。
    /// A block fills exactly that gap: draw any shape you like (curves included), and stamp it with rotation via `ui_draw_block`.
    /// 路径字符串与 `basic/gorilla_pro.bas` 的 `makeBanana` **逐字相同**（两版同一只香蕉）。
    /// The path string is **character-for-character identical** to `makeBanana` in `basic/gorilla_pro.bas` (the same banana in both versions).
    /// </summary>
    void MakeBlock()
    {
        bid = ui_create_block(30, 20, 0);
        // 月牙：两段三次贝塞尔，两个尖端在 (3,14) 与 (27,9)，凹面朝下
        // Crescent: two cubic Beziers, tips at (3,14) and (27,9), concave side facing down
        ui_path("M 3 14 C 8 2, 22 -1, 27 9 C 20 4, 10 6, 3 14 Z", 0xFF8A7418, 1, 0xFFFFE066, "", 1, 0);
        // 两个蒂（深一点的圆点）—— 两端有蒂才像香蕉，不然像月牙
        // Two stems (slightly darker dots) — with stems at both ends it looks like a banana, otherwise like a crescent moon
        ui_circle(3, 14, 2, 0xFF6E5A14, 1, 0);
        ui_circle(27, 9, 2, 0xFF6E5A14, 1, 0);
        bid = ui_end_block();
    }

    void Launch(int sx, int sy, int svx, int svy, int wind)
    {
        bx = sx * FP;
        by = sy * FP;
        // ⚠ **不要**在这里再加一次风的冲量：`wind` 是**加速度**（定点单位/拍²），
        // ⚠ Do **not** add the wind impulse again here: `wind` is an **acceleration** (fixed-point units per tick²)
        //   已经由 `Step` 每拍累加。原先写 `svx + wind * FP / 4` —— 风本来就是定点量、
        //   already accumulated every tick by `Step`. It used to be written `svx + wind * FP / 4` — the wind is already a fixed-point quantity
        //   不用再过 `FP`，那一下把它放大 **64 倍**，直接盖过初速 ⇒ 香蕉**反着飞**。
        //   and does not need `FP` applied, so that multiplied it by **64** and swamped the initial velocity ⇒ the banana **flew backwards**.
        vx = svx;
        vy = svy;
        live = 1;
        trailN = 0;
        spin = 0;              // 每一发重新开始翻跟头
        // Every shot starts tumbling afresh
    }

    // 一拍。返回 0 = 还在飞，1 = 掉到地上/楼里，2 = 飞出屏幕
    // One tick. Returns 0 = still flying, 1 = hit the ground/building, 2 = flew off screen
    int Step(int wind, int gy, int sw)
    {
        int i;

        if (live == 0) { return 0; }

        bx = bx + vx;
        by = by + vy;
        vy = vy + GRAV;
        vx = vx + wind;                     // 风是"加速度"（每拍加一点水平速度）
        // Wind is an acceleration (adds a bit of horizontal speed every tick)

        x = bx / FP;
        y = by / FP;

        // 自转（翻跟头）。
        // Spin (tumbling).
        // ⚠ 光靠"顺着弹道"是不够的：一发平射全程只转 ±45°，22px 的小月牙看着几乎没动
        // ⚠ Following the trajectory alone is not enough: a flat shot turns only ±45° over its whole flight, and a 22px crescent barely appears to move
        //   （`basic/gorilla_pro.bas` 那边的玩家原话是"转动幅度太小了"）。
        //   (the player's words on the `basic/gorilla_pro.bas` side were "the rotation is too small").
        spin = spin + 18;
        if (spin >= 360) { spin = spin - 360; }

        // 记尾迹（满了就整体左移一格）
        // Record the trail (when full, shift the whole thing one slot to the left)
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
        // Below the horizon (including between short buildings)
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
        // Trail: earlier points are darker (using distinct fixed colours, to avoid computing colours every frame)
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
            // Attitude = **trajectory direction** (the crescent follows the flight direction) + **spin** (tumbling).
            // 缩放 720‰：图块本身 30x20，原尺寸贴出来比大猩猩（约 28px 宽）还大一圈，
            // Scale 720‰: the block itself is 30x20, and stamped at full size it comes out a size larger than the gorilla (about 28px wide),
            // 看着像"扔出去一个球拍"；720 之后约 22px，比猿小、又还看得出是香蕉。
            // looking like "throwing a racket"; at 720 it is about 22px, smaller than the ape yet still recognisably a banana.
            ui_draw_block(bid, x, y, 720, 720, AngleOf(vx, vy) + spin);
        }
        else
        {
            // 图块没建起来（有真窗口时不该发生）—— 退回一个圆点。
            // The block was not created (should not happen with a real window) — fall back to a single dot.
            // **宁可画得糙，也不能让香蕉看不见**：看不见 = 这一局没法玩，
            // **Better drawn crudely than invisible**: invisible = the round cannot be played,
            // 而屏幕上没有任何东西提示你是哪儿坏了。
            // and nothing on screen hints at what broke.
            ui_circle(x, y, 5, C_BANANA, 1, 0);
            ui_circle(x - 2, y - 2, 2, 0xFFFFFFFF, 1, 0);
        }
    }
};

// ════════════════════════════════════════════════════════════════════
// 天上飞的：鸟 / 飞碟 / 飞机
// Things that fly: bird / UFO / plane
//
// 这一族是**继承 + 虚函数**的正经用法（不是为了炫技）：
// This family is a proper use of **inheritance + virtual functions** (not showing off):
//   · 三者的**走法**完全不同（鸟乱抖乱掉头、飞碟悬停、飞机匀速直线）；
//   · their **movement** is completely different (the bird wobbles and turns around, the UFO hovers, the plane flies a straight line at constant speed);
//   · 但游戏只关心"你在哪、你能不能被打到"这两件事。
//   · but the game cares about only two things: where you are, and whether you can be hit.
// 基类 `Flyer` 定 `Step()` / `R()` / `Draw()` 三个虚接口，派生类各写各的。
// The base class `Flyer` defines three virtual interfaces — `Step()` / `R()` / `Draw()` — and each derived class writes its own.
// ════════════════════════════════════════════════════════════════════

// ── 飞碟的"脾气"：速度与报复 ─────────────────────────────────────────
// ── The UFO's "temperament": speed and revenge ──
//
// 玩家定的：**飞碟要"速度很快飞来、悬停、很快飞走"**（原来 4px/拍 是飘过来的），
// Set by the player: **the UFO should "come in fast, hover, and leave fast"** (it used to drift in at 4px/tick),
// 而且**打中它 = 招来报复**（见 `Ufo::BeginRage`）。
// and **hitting it = provoking revenge** (see `Ufo::BeginRage`).
// 这三只定时器都按"拍"算（一拍 = `TICK_MS` = 33ms）。
// These three timers are all counted in ticks (one tick = `TICK_MS` = 33ms).
#define UFO_V 10             // 巡航速度（px/拍）—— "很快"，是飞机(6)的 1.7 倍
// Cruise speed (px/tick) — "very fast", 1.7× the plane's (6)
#define UFO_HOLD 18          // 悬停拍数（原来 45，玩家要"悬停一下就走"）
// Hover ticks (used to be 45; the player wanted "hover a moment then go")
#define RAGE_V 14            // 报复时扑向猴子的速度（比巡航还快）
// Speed of the swoop at the monkey during revenge (faster than cruising)
#define RAGE_AIM 20          // 飞到头顶后瞄准的拍数（玩家看得见"它在瞄你"）
// Ticks spent aiming after arriving overhead (the player can see "it is aiming at you")
#define RAGE_SHOT 45         // 激光持续的拍数（约 1.5 秒 —— 够看清，又不拖沓）
// Ticks the laser lasts (about 1.5 seconds — long enough to see, short enough not to drag)

class Flyer : public Entity
{
public:
    int vx;
    int live;            // 1 = 在场
    // 1 = present
    int kind;            // 1 鸟 / 2 飞碟 / 3 飞机
    // 1 bird / 2 UFO / 3 plane

    Flyer()
    {
        x = 0;
        y = 0;
        vx = 0;
        live = 0;
        kind = 0;
    }

    virtual int R() { return 10; }                  // 命中半径（画多大就判多大）
    // Hit radius (judged exactly as big as it is drawn)
    virtual void Step(int sw, int gy) { }           // 一拍的运动
    // Movement for one tick
    virtual void Draw() { }
};

// ── 鸟：进哪边随机、高度隔一阵抖一下、偶尔掉头（只掉有限次）──
// ── Bird: enters from a random side, its height jitters now and then, and it occasionally turns around (only a limited number of times) ──
class Bird : public Flyer
{
public:
    int wob;
    int t;
    int turns;
    int bidUp;           // 翅膀朝上的那帧
    // The pose with the wings up
    int bidDn;           // 翅膀朝下的那帧
    // The pose with the wings down

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
    /// Record the bird's **two wing poses** as blocks (called once after the window opens).
    ///
    /// ⚠ 为什么是**两个**图块而不是"一个身体 + 一条会转的翅膀"：
    /// ⚠ Why **two** blocks rather than "one body + one rotating wing":
    ///   翅膀是长在**身后**的（从 `cx-6` 甩到 `cx-14`），绕身体转会把根也甩出去。
    ///   the wing is attached **behind** the body (sweeping from `cx-6` to `cx-14`), and rotating it about the body would swing the root out as well.
    ///   两帧各录一份最直白，代价只是多一份 30×20 的录制。
    ///   Recording one block per pose is the most straightforward, at the cost of one extra 30×20 recording.
    ///
    /// ⚠ 只录**朝右**的那一份 —— 朝左贴的时候用 `-1000` 镜像（见 `Draw`）。
    /// ⚠ Only the **right-facing** one is recorded — facing left is stamped mirrored with `-1000` (see `Draw`).
    ///   这是本版才通的：`Stamp` 原先把 `sx <= 0` 一起拒了，负值（镜像）根本贴不出来。
    ///   This only started working in this version: `Stamp` used to reject `sx <= 0` along with everything else, so a negative (mirrored) value could not be stamped at all.
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
    /// <summary>A **right-facing** bird, drawn at local coordinates (cx, cy). The two poses differ only in the wing line.</summary>
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
        // Random height, the **lowest** band (a bird flies just over the rooftops anyway, and skimming the roofline now and then looks good).
        // ⚠ 原来 `gy/4 + rand(gy/3)` 最下一档也会到 0.58·gy，比最高的楼顶（0.297·gy）还低一截。
        // ⚠ It used to be `gy/4 + rand(gy/3)`, whose lowest band also reached 0.58·gy — well below even the tallest roof (0.297·gy).
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
        // ⚠ The facing must be **computed from the current velocity** (`vx` gets flipped by that `t % 90` turn-around),
        //   不能按出生时的 `dir`。原来整套图形是按"朝右"写死的（头在 x+4、喙在 x+7、
        //   not from the `dir` at spawn time. The whole shape used to be hard-coded "facing right" (head at `x+4`, beak at `x+7`,
        //   尾巴在 x-14）⇒ **掉头之后就是倒着飞**（玩家报的正是这个）。
        //   tail at `x-14`) ⇒ **after turning around it flew backwards** (which is exactly what the player reported).
        d = 1;
        if (vx < 0) { d = -1; }

        b = bidUp;
        if (wob < 0) { b = bidDn; }

        if (b > 0)
        {
            // 朝左 = **水平镜像**（`sx` 取负）。同一个图块左右通吃，不必录两份形状。
            // Facing left = **horizontal mirror** (a negative `sx`). One block serves both directions, so there is no need to record two shapes.
            ui_draw_block(b, x, y, d * 1000, 1000, 0);
        }
        else
        {
            // 图块没建起来（有真窗口时不该发生）—— 退回一只"圆点鸟"。
            // The block was not created (should not happen with a real window) — fall back to a "dot bird".
            // **宁可画得糙，也不能让它看不见**：看不见 = 打不中它，而屏幕上没有
            // **Better drawn crudely than invisible**: invisible = you cannot hit it, and nothing on screen
            // 任何东西提示你是哪儿坏了。
            // hints at what broke.
            ui_circle(x, y, 6, 0xFF30343C, 1, 0);
            ui_circle(x + 4 * d, y - 4, 4, 0xFF30343C, 1, 0);
        }
    }
};

// ── 飞碟：飞来 → 悬停一会儿 → 飞走；悬停时灯闪 ──
// ── UFO: flies in → hovers a while → flies away; its lights blink while hovering ──
class Ufo : public Flyer
{
public:
    int phase;
    int hold;
    int t;
    int lit;
    int bid;             // 碟身图块（座舱罩 + 碟身；光晕与灯是动态的，不进块）
    // Saucer body block (dome + body; the glow and the lights are dynamic and are not in the block)

    // ── 报复模式 ─────────────────────────────────────────────────────
    // ── Revenge mode ──
    //
    // 玩家定的玩法：**打中飞碟会招来报复** —— 它眼神不好，只找**离它最近的**那只猴子，
    // The gameplay set by the player: **hitting the UFO brings revenge** — its eyesight is poor, so it seeks out only the **nearest** monkey,
    // 飞到头顶放激光，直接打死，一局结束。
    // flies over its head, fires a laser, kills it outright, and the round is over.
    //
    // ⚠ 所以"打中飞碟"**不再等于击落它**（鸟和飞机照旧击落）。这一发的代价从
    // ⚠ So "hitting the UFO" **no longer means shooting it down** (birds and planes are still shot down). The cost of that shot changes from
    //   "白扔一个香蕉"变成了"可能输掉这一局" —— 这正是玩家要的张力：天上飞的
    //   "wasting a banana" into "possibly losing the round" — exactly the tension the player wanted: what flies in the sky is
    //   不再是"可以随便打的靶子"，而是**要躲开的东西**。
    //   no longer "a target you can shoot at freely" but **something to be avoided**.
    int rage;            // 1 = 报复模式
    // 1 = revenge mode
    int target;          // 目标猴子（0 / 1）
    // Target monkey (0 / 1)
    int arrived;         // 已经飞到目标头顶
    // Has already flown above the target
    int aimT;            // 到位之后瞄了几拍
    // How many ticks it has been aiming since arriving
    int shot;            // 激光已经开了几拍（0 = 还没开火）
    // How many ticks the laser has been firing (0 = not yet)
    int fired;           // 已经结算过（防止"每拍都打死一次"）
    // Already resolved (so it does not "kill once per tick")

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
    /// Hit by a banana → **switches into revenge** (rather than being shot down).
    /// `who` 是目标猴子，由 `Game::NearestApeTo` 按"离飞碟最近"选出来。
    /// `who` is the target monkey, chosen by `Game::NearestApeTo` on the "nearest to the UFO" criterion.
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
        // Descending alarm: the player must know at once that "I provoked it"
        ui_vibrate(120, 0);
    }

    /// 报复模式的一拍：扑向目标头顶 → 悬停瞄准 → 开火。
    /// One tick of revenge mode: swoop to above the target → hover and aim → open fire.
    ///
    /// ⚠ 走的是**直线逼近**（每拍朝目标走 `RAGE_V`），不是"先横后竖"的分段 ——
    /// ⚠ It approaches **in a straight line** (moving `RAGE_V` toward the target each tick), not as a "horizontal then vertical" two-leg path —
    ///   分段会让它在猴子正上方拐个直角，看着像"按格子走"，与"扑过来"完全不是一回事。
    ///   a two-leg path would make it turn a right angle directly above the monkey, looking like "moving on a grid", which is nothing like "swooping in".
    void RageStep(int sw, int gy)
    {
        int tx;
        int ty;
        int dx;
        int dy;
        int d;

        // 目标点 = 猴子**头顶上方** 78px（猴子头顶在 `y-52`，再留 26px 空档，
        // Target point = 78px **above the monkey's head** (the top of the head is at `y-52`, leaving a 26px gap,
        // 免得碟身压着猴子的脑袋 —— 那样激光就没地方画了）
        // so the saucer does not sit on the monkey's head — that would leave nowhere to draw the laser)
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
            // Slight float while hovering ("it is aiming")
        }
        else if (shot < RAGE_SHOT)
        {
            shot = shot + 1;
        }
    }

    /// <summary>
    /// 把碟身录成图块（开窗之后调一次）。
    /// Record the saucer body as a block (called once after the window opens).
    ///
    /// ⚠ 碟身**不能整体旋转**（座舱罩在顶上，转起来就成了"歪帽子"）—— 它用图块是为了
    /// ⚠ The saucer **cannot be rotated as a whole** (the dome sits on top, and rotating it would give a "crooked hat") — it uses a block in order to
    ///   形状只写一遍 + 贴出时省调用。**会转的是底下那圈灯**（见 `Draw` 里 `spin` 那段）：
    ///   write the shape once and save calls when stamping. **What does rotate is the ring of lights underneath** (see the `spin` part in `Draw`):
    ///   一圈灯绕中心转，才像"盘子底下有东西在转"。
    ///   a ring of lights turning about the centre is what makes it look like "something is spinning under the saucer".
    ///
    /// 块范围按最外沿取：碟身 `x±22`、罩顶到 `y-16` ⇒ 48×34，中心在局部 (24,17)。
    /// The block extent is taken at the outermost edges: body `x±22`, dome top to `y-16` ⇒ 48×34, centre at local (24,17).
    /// </summary>
    void MakeBlock()
    {
        bid = ui_create_block(48, 34, 0);
        // 座舱罩：**先画一整个圆**，碟身随后盖掉它的下半 ⇒ 正好剩一个半圆罩
        // Dome: **draw a whole circle first**, and the saucer body then covers its lower half ⇒ leaving exactly a half-dome
        //（本平台没有裁剪，这个"画完再盖"就是最省事的做法）
        //(this platform has no clipping, so "draw then cover" is the least trouble)
        ui_circle(24, 11, 10, 0xFF8ADCFF, 1, 0);
        ui_circle(24, 11, 10, 0xFF2E6E9E, 0, 2);
        ui_ellipse(20, 8, 3, 2, 0xFFFFFFFF, 1, 0);          // 罩子高光
        // Dome highlight
        // 碟身：宽扁椭圆（宽:高 ≈ 3:1 才像碟）
        // Body: a wide flat ellipse (width:height ≈ 3:1 is what makes it look like a saucer)
        ui_ellipse(24, 18, 22, 7, 0xFFC8D0DC, 1, 0);
        ui_ellipse(24, 18, 22, 7, 0xFF5A6472, 0, 2);
        // 碟身上半的一道亮边，给它"金属盘子"的感觉
        // A bright edge along the upper half of the body, to give it the feel of a "metal plate"
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
        // If the last one was restarted while raging, these states have to be cleared (otherwise a new UFO comes out heading straight for the monkey)
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
        // Random height, the middle band (see the explanation at `Plane::Spawn`).
        // ⚠ 原来 `gy/3 + rand(gy/4)` 最下一档到 0.58·gy，那已经**低于最高的楼顶**
        // ⚠ It used to be `gy/3 + rand(gy/4)`, whose lowest band reached 0.58·gy, which is already **below the tallest roof**
        //   （0.297·gy），碟子会从楼中间穿过去。
        //   (0.297·gy), so the saucer would pass straight through the middle of the buildings.
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
        // Revenge mode **has a completely different movement path** (swoop at the monkey → hover → fire),
        // 而且它不该再"飞出屏幕就消失" —— 那等于半路撤销了惩罚。
        // and it should no longer "disappear when it leaves the screen" — that would be revoking the punishment halfway.
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
        // ⚠ It used to be "a big ball + a small ball"; the player's words were "the UFO is a ball too".
        //   飞碟之所以一眼是飞碟，靠的是**宽扁的碟身 + 顶上一个罩子**这两条轮廓线，
        //   What makes a UFO read as a UFO at a glance is the two outline strokes **a wide flat saucer body + a dome on top**;
        //   光有圆是读不出来的。这两条轮廓现在录在 `MakeBlock` 里。
        //   circles alone do not read. Both outlines are now recorded in `MakeBlock`.
        //
        // 悬停的光晕：**先画**（随后被碟身压住一半）。
        // The hover glow: **drawn first** (the saucer body then covers half of it).
        // 它是半透明的、又跟着碟身走，留在块外更省事（进了块也只是多录一行）。
        // It is translucent and travels with the saucer, so leaving it outside the block is less trouble (putting it inside would only record one more line).
        ui_ellipse(x, y + 10, 17, 4, 0x40A0E0FF, 1, 0);

        if (bid > 0)
        {
            ui_draw_block(bid, x, y, 1000, 1000, 0);
        }
        else
        {
            // 图块没建起来（有真窗口时不该发生）—— 退回"大球 + 小球"。
            // The block was not created (should not happen with a real window) — fall back to "big ball + small ball".
            // ⚠ 那正是玩家挑过的形状，但**看不见比难看糟得多**。
            // ⚠ That is exactly the shape the player complained about, but **invisible is far worse than ugly**.
            ui_circle(x, y - 6, 10, 0xFF8ADCFF, 1, 0);
            ui_ellipse(x, y + 1, 22, 7, 0xFFC8D0DC, 1, 0);
        }

        // ── 底下一圈灯（`lit` 隔一阵闪一下）──
        // ── The ring of lights underneath (`lit` blinks every so often) ──
        // **动态的，不进图块**：进了就得录两帧，而它只值五个圆点。
        // **Dynamic, so not in the block**: putting it in would mean recording two frames, and it is worth only five dots.
        if (lit != 0)
        {
            ui_circle(x - 15, y + 5, 2, 0xFFFF5050, 1, 0);
            ui_circle(x - 8,  y + 7, 2, 0xFFFFE050, 1, 0);
            ui_circle(x,      y + 8, 2, 0xFF50FF70, 1, 0);
            ui_circle(x + 8,  y + 7, 2, 0xFF50D0FF, 1, 0);
            ui_circle(x + 15, y + 5, 2, 0xFFFF5050, 1, 0);
        }

        // ── 报复：瞄准警示 + 激光 ──────────────────────────────────────
        // ── Revenge: aim warning + laser ──
        //
        // 警示圈：**到位之后、开火之前**那段（`RAGE_AIM` 拍 ≈ 0.7 秒）在猴子头上
        // Warning ring: during the stretch **after arriving and before firing** (`RAGE_AIM` ticks ≈ 0.7 seconds) a red ring blinks
        // 闪一个红圈。玩家反应不过来，但"我知道我要死了"和"莫名其妙就死了"
        // over the monkey's head. The player cannot react in time, but "I know I am about to die" and "I died for no reason"
        // 是两种完全不同的体验 —— 前者是惩罚，后者是 bug。
        // are two completely different experiences — the first is a punishment, the second is a bug.
        if (rage != 0 && arrived != 0 && shot == 0)
        {
            if (aimT % 6 < 3)
            {
                ui_circle((*AP[target]).x, (*AP[target]).y - 30, 20, 0xFFFF4040, 0, 3);
            }
        }

        // 激光：三层同轴（宽而淡 → 中 → 细而白）。
        // Laser: three coaxial layers (wide and faint → medium → thin and white).
        // ⚠ 与流星尾迹是**同一套画法**：单画一条线读出来是"一根棍子"，
        // ⚠ It uses **the same drawing scheme** as the meteor trail: a single line reads as "a stick",
        //   三层叠起来才有"能量烧穿"的观感。起画点取碟身下沿 `y+8`，
        //   and only three stacked layers give the "burning through" feel. The start point is at the saucer's lower edge `y+8`,
        //   免得被碟身盖掉一截（碟身是**先**画的，后画的线会盖住它 —— 所以起点
        //   so it is not partly covered by the body (the body is drawn **first**, and a line drawn later covers it — hence the start point
        //   定在碟身下沿而不是中心）。
        //   is at the body's lower edge rather than its centre).
        if (rage != 0 && shot > 0)
        {
            int cx;
            int cy;

            cx = (*AP[target]).x;
            cy = (*AP[target]).y - 18;      // 落在猴子**身上**（不是脚下）
            // Lands **on** the monkey (not at its feet)
            ui_line(x, y + 8, cx, cy, 0x50FF3030, 13);
            ui_line(x, y + 8, cx, cy, 0xCCFF5050, 6);
            ui_line(x, y + 8, cx, cy, 0xFFFFFFFF, 2);
            ui_circle(cx, cy, 9, 0x80FF6060, 1, 0);
            ui_circle(cx, cy, 4, 0xFFFFFFFF, 1, 0);
        }
    }
};

// ── 飞机：定期飞过、匀速直线；夜里机翼有闪灯 ──
// ── Plane: flies past periodically in a straight line at constant speed; its wing light blinks at night ──
class Plane : public Flyer
{
public:
    int t;
    int lit;
    int bid;             // 机身图块（含尾翼与座舱；灯是动态的，不进块）
    // Fuselage block (tail and cockpit included; the light is dynamic and is not in the block)

    Plane()
    {
        kind = 3;
        t = 0;
        lit = 0;
        bid = 0;
    }

    /// <summary>
    /// 把机身录成图块（开窗之后调一次）。只录**朝右**那一份，朝左用 `-1000` 镜像。
    /// Record the fuselage as a block (called once after the window opens). Only the **right-facing** one is recorded; left uses a `-1000` mirror.
    /// 块范围按最外沿取：机身 `x±16`、尾翼到 `y-10`、灯在 `y-11` ⇒ 40×24，中心在局部 (20,12)。
    /// The block extent is taken at the outermost edges: fuselage `x±16`, tail to `y-10`, light at `y-11` ⇒ 40×24, centre at local (20,12).
    /// </summary>
    void MakeBlock()
    {
        bid = ui_create_block(40, 24, 0);
        ui_rect(4, 9, 32, 6, 0xFFD8DEE6, 1, 0, 3);          // 机身
        // Fuselage
        ui_line(18, 12, 8, 2, 0xFFB8C0CC, 3);               // 尾翼（后）
        // Tail fin (rear)
        ui_line(22, 12, 28, 3, 0xFFB8C0CC, 3);              // 尾翼（前）
        // Tail fin (front)
        ui_rect(26, 10, 8, 5, 0xFF6FA8DC, 1, 0, 2);         // 座舱
        // Cockpit
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
        // Height is **random** (the player: "the plane's height ... should all be random"). It used to be hard-coded to `gy / 5`,
        // 于是每一架都在同一条水平线上飞 —— 看两眼就发现是"轨道"不是"天空"。
        // so every plane flew along the same horizontal line — two glances are enough to see it is a "rail", not a "sky".
        //
        // ── 三档飞行高的划分（见 `Flyer` 那段的说明）────────────────────
        // ── The three flight-height bands (see the explanation at `Flyer`) ──
        // 天空带其实**只有 0.13·gy ~ 0.30·gy 这一段**：
        // The sky band is really **only the stretch 0.13·gy .. 0.30·gy**:
        //   · 上限 0.13·gy —— 顶上是计分/风向栏（`hud` 是**最后**画的，飞进去就被盖住）；
        //   · upper bound 0.13·gy — above it is the score/wind bar (`hud` is drawn **last**, so anything flying into it gets covered);
        //   · 下限 0.30·gy —— 最高那栋楼的楼顶（楼高 = sh/5+sh/4，两边那两栋再加 sh/14
        //   · lower bound 0.30·gy — the roof of the tallest building (height = `sh/5+sh/4`, plus `sh/14` for the two outer ones
        //     ⇒ 楼顶最高到 0.22·sh = 0.297·gy），再低就从楼里穿过去了。
        //     ⇒ the highest roof reaches 0.22·sh = 0.297·gy); any lower and it passes through the buildings.
        // 于是三档**首尾相接**地铺满这一段：飞机最高、鸟最低、飞碟居中。
        // So the three bands **join end to end** to fill that stretch: the plane highest, the bird lowest, the UFO in the middle.
        // 三档各自 7~9% 的宽度，飞几趟就能看出高度不是固定的。
        // Each band is 7..9% wide, so after a few passes you can tell the height is not fixed.
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
        // Same reasoning as the bird: the fuselage mirrors by **current velocity** (it used to be hard-coded right-facing)
        d = 1;
        if (vx < 0) { d = -1; }

        if (bid > 0)
        {
            ui_draw_block(bid, x, y, d * 1000, 1000, 0);
        }
        else
        {
            // 图块没建起来（有真窗口时不该发生）—— 退回简版（见 `Bird::Draw` 的同一条理由）
            // The block was not created (should not happen with a real window) — fall back to the simple version (the same reasoning as `Bird::Draw`)
            ui_rect(x - 16, y - 3, 32, 6, 0xFFD8DEE6, 1, 0, 3);
            ui_rect(x + 6 * d - 8, y - 2, 8, 5, 0xFF6FA8DC, 1, 0, 2);
        }

        // 航行灯：**动态的**（`lit` 隔一阵闪一下），不进图块 —— 进了就得录两帧、
        // Navigation light: **dynamic** (`lit` blinks every so often), so not in the block — putting it in would mean recording two frames
        // 而它只值一个圆点。位置按 `d` 镜像，与机身同一边。
        // for a single dot. Its position mirrors by `d`, on the same side as the fuselage.
        if (lit != 0) { ui_circle(x - 14 * d, y - 11, 2, 0xFFFF4040, 1, 0); }
    }
};

// ════════════════════════════════════════════════════════════════════
// Tree —— 地上的树（位置 / 数量 / 高矮都随机）
// Tree — trees on the ground (position / count / height all random)
// ════════════════════════════════════════════════════════════════════

/// 蒙版**底矩形**比楼体向外扩多少（px）。
/// How far (px) the mask's **base rectangle** extends beyond the building.
///
/// ⚠⚠ 这一圈是**让洞不被缩小**的关键。折叠"楼 − 洞"要求洞整个落在底矩形里
/// ⚠⚠ This margin is the key to **keeping the holes from being shrunk**. Folding "building − holes" requires each hole to lie entirely inside the base rectangle
/// （even-odd 下"底外洞内"会被判成**内部**，那一块就会允许绘制）。
/// (under even-odd, "outside the base but inside the hole" is judged **inside**, and that region is then allowed to be drawn).
/// 而**楼体外的这一圈本来就没有任何绘制**（建筑层画的楼体/窗/门/檐口全在楼矩形内），
/// And **this ring outside the building has nothing drawn in it anyway** (the body/windows/door/cornice all lie inside the building rectangle),
/// 所以把底矩形放大一圈、让洞挖进去，**不会有"肉"露出来**，洞却能保持原始大小。
/// so enlarging the base rectangle and letting the hole dig into it **exposes no unwanted fill**, while the hole keeps its original size.
///
/// 洞半径最大 16 ⇒ 20 足够。
/// The largest hole radius is 16 ⇒ 20 is enough.
#define MASK_PAD 20

/// 树的**标准尺寸**（录图块用）。⚠ 树的高矮宽窄每棵都不同，贴的时候按**高度等比缩放**
/// A tree's **standard size** (for block recording). ⚠ Every tree differs in height and width, and stamping scales **proportionally by height**
/// （宽度不按实际值 —— 那会让树冠变成椭圆；宽度上的差异在观感上本来就只是"冠大一点"）。
/// (the width is not taken from the actual value — that would make the crown an ellipse; and visually the width difference is only "a slightly bigger crown").
#define TREE_STD_W 40
#define TREE_STD_H 64

/// 一棵树的局部形状：`cx` = 树干中轴、`cy` = 树根、`cw`/`chh` = 冠宽/树高、`v` = 绿档 0..2。
/// A tree's local shape: `cx` = trunk axis, `cy` = root, `cw`/`chh` = crown width / tree height, `v` = green step 0..2.
///
/// 抽成函数是为了录图块（形状写一遍，录的时候调它、画的时候贴）。
/// It is a function for the sake of block recording (the shape is written once, called while recording and stamped while drawing).
/// ⚠ 绿档**本来就是三档固定色**（按位置取，与天光无关）⇒ 树只要录 3 个图块，不用分昼夜。
/// ⚠ The green steps **are three fixed colours by construction** (taken from position, independent of daylight) ⇒ trees need only 3 blocks, with no day/night stepping.
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
    // Trunk
    ui_rect(cx - 2, cy - chh / 3, 5, chh / 3 + 2, dark, 1, 0, 1);
    ui_rect(cx - 1, cy - chh / 3, 3, chh / 3, 0xFF5A4326, 1, 0, 1);

    // 树冠：两侧小圆先画（当底部层次），主冠盖上去，最后左上一块受光
    // Crown: the two side circles first (as the lower layer), the main crown over them, and finally a lit patch at the upper left
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
    // Root of the tree
    int h;               // 树高
    // Tree height
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
        // This tree's green step (0..2) — taken from position, no extra field needed
        v = (x / 7) % 3;
        if (treeBid[v] > 0)
        {
            // ⚠ **按高度等比缩放**：宽度不按实际值 —— 那会让树冠变椭圆，
            // ⚠ **Scale proportionally by height**: the width is not taken from the actual value — that would make the crown an ellipse,
            //   而宽度上的差异观感上只是"冠大一点"。
            //   and the width difference visually amounts to only "a slightly bigger crown".
            // ⚠ 图块的"高"含树根**下方 2px**（树冠画到 cy-h/3+2），所以中心要跟着偏。
            // ⚠ The block's "height" includes **2px below the root** (the trunk is drawn to `cy-h/3+2`), so the centre has to be offset accordingly.
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
// Wind
// ════════════════════════════════════════════════════════════════════

class Wind
{
public:
    int v;               // 定点单位/拍^2（正 = 向右吹）
    // Fixed-point units per tick² (positive = blowing to the right)

    Wind()
    {
        v = 0;
    }

    void Randomize()
    {
        // ±30 定点单位/拍²（≈重力的 21%）—— 一局里的横向偏移够明显，又不至于没法瞄
        // ±30 fixed-point units per tick² (≈21% of gravity) — enough horizontal drift in a round to be obvious, yet not so much that aiming becomes impossible
        v = ui_rand(61) - 30;
    }

    void Draw(int sw, int y)
    {
        int cx;
        int i;
        int n;

        cx = sw / 2;
        ui_text(cx, y, gLang == 0 ? "风" : "Wind", C_TEXT_DIM, 12, VML_ANCHOR_CENTER);
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
// Hud — the status bar along the top
// ════════════════════════════════════════════════════════════════════

class Hud
{
public:
    Hud()
    {
    }

    void Draw(Ape* a0, Ape* a1, int turn, int sw)
    {
        int sx;      // 分数数字的起点/右界（英文名字宽，得往中间让）
        // Start / right bound of the score digits (English names are wide, so it has to give way toward the middle)
        int dy;      // 昼夜标记的横坐标（英文那行"Turn: X"更宽，标记也得让）
        // x coordinate of the day/night marker (the English "Turn: X" line is wider, so the marker has to give way too)

        ui_rect(0, 0, sw, 30, C_HUD_BG, 1, 0, 0);

        /* ⚠ "Orange"/"Purple"（≈39px）比"橙"/"紫"（13px）宽得多 ⇒ 分数数字必须让开， */
        // ⚠ "Orange"/"Purple" (≈39px) is much wider than the two-character Chinese labels (13px) ⇒ the score digits have to move out of the way,
        /*    否则两边压在一起（中文那版 30 / sw-30 就是照 13px 量出来的）。 */
        //    otherwise the two sides collide (the Chinese version's 30 / sw-30 was measured against 13px).
        sx = gLang == 0 ? 30 : 53;
        ui_text(8, 20, gLang == 0 ? "橙" : "Orange", C_APE0, 13, VML_ANCHOR_LEFT);
        ui_text(sx, 20, numstr(a0->score), C_TEXT, 14, VML_ANCHOR_LEFT);

        ui_text(sw - 8, 20, gLang == 0 ? "紫" : "Purple", C_APE1, 13, VML_ANCHOR_RIGHT);
        ui_text(sw - sx, 20, numstr(a1->score), C_TEXT, 14, VML_ANCHOR_RIGHT);

        if (turn == 0) { ui_text(sw / 2, 20, gLang == 0 ? "轮到 橙" : "Turn: Orange", C_APE0, 14, VML_ANCHOR_CENTER); }
        else { ui_text(sw / 2, 20, gLang == 0 ? "轮到 紫" : "Turn: Purple", C_APE1, 14, VML_ANCHOR_CENTER); }

        // 昼夜标记：白天不写、天黑了才写"夜/Night" —— 比写"昼"省一格，也更像在报状态
        // Day/night marker: nothing by day, and "Night" only once it is dark — one cell cheaper than writing "day", and it reads more like reporting a state
        // ⚠ 英文那行（"Turn: Orange" ≈84px）比中文（"轮到 橙" ≈35px）宽出一倍，
        // ⚠ The English line ("Turn: Orange" ≈84px) is twice as wide as the Chinese one (≈35px),
        //   标记再多让 8px 才不会顶上（中文那版 +58 是照中文宽度定的）。
        //   so the marker has to give another 8px to avoid colliding (the Chinese version's +58 was set to the Chinese width).
        if (gDayL < 40)
        {
            dy = gLang == 0 ? 58 : 66;
            ui_text(sw / 2 + dy, 20, gLang == 0 ? "夜" : "Night", C_TEXT_DIM, 12, VML_ANCHOR_CENTER);
        }

        // 游戏时间 hh:mm。
        // Game time hh:mm.
        // ⚠ 一律**左对齐**、坐标各自往左让位 —— 用 `VML_ANCHOR_RIGHT` 让它们右对齐
        // ⚠ Always **left-aligned**, with each coordinate shifted left as needed — using `VML_ANCHOR_RIGHT` to right-align them
        //   的话几段会叠在同一处（实测「7」和「30」压成了「7月3」）。
        //   would stack the pieces on top of each other (measured: the "7" and the "30" were printed on top of one another, reading as a single garbled glyph).
        // ⚠ 分钟补零靠判断而不是 `sprintf("%02d")` —— 本平台的 `%` 转换是坏的
        // ⚠ Zero-padding the minutes uses a check rather than `sprintf("%02d")` — this platform's `%` conversion is broken
        //   （见 KNOWN_DEFECTS 的 9/10）。
        //   (see items 9/10 in KNOWN_DEFECTS).
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
// Game — the master controller
// ════════════════════════════════════════════════════════════════════

class Game
{
public:
    // ⚠ 这里写的是"四个具名 `Building` + 文件级指针表 `BL[i]`"。
    // ⚠ What is written here is "four named `Building` objects + the file-level pointer table `BL[i]`".
    //   **当年是因为类里放不了数组字段**（`int data[4];` 报「期望 SEMICOLON」），
    //   That was because a class could not hold an array field back then (`int data[4];` reported "expected SEMICOLON"),
    //   现在那个限制已经修好（F25），但**还没收回来**：成员数组的读写整体缺一小截
    //   and that limit is fixed now (F25), but it has **not been folded back yet**: reading and writing member arrays is still missing a small piece
    //   （见 `d30.cpp` —— 类内部用隐式 `this` 做下标读会算错地址），
    //   (see `d30.cpp` — reading an index inside a class with an implicit `this` computes the wrong address),
    //   而这个文件当前是**能跑的**，不宜为写法好看去动它的内存布局。
    //   and this file **currently runs**, so its memory layout should not be disturbed just to make the style look nicer.
    //   等那一截修好，这里就该是 `Building bldgs[4]; Ape apes[2];`。
    //   Once that piece is fixed, this should be `Building bldgs[4]; Ape apes[2];`.
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
    // Cadence counter for adding things to the sky

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
    // 0 = ended by reaching the score / 1 = wiped out by the UFO's laser
    int night;
    int hitBy;              // 这一发打中了谁（-1 = 没打中人）
    // Who this shot hit (-1 = hit nobody)
    int heldL;
    int heldR;
    int heldU;
    int heldD;
    int repeatT;
    int treeN;

    // 操作面板的几何（绘制与命中**共用**，见 PanelGeometry 的注释）
    // Geometry of the control panel (**shared** by drawing and hit testing, see the comment on `PanelGeometry`)
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
    // Match the named fields up with index-based access (called once after the constructors have run and the object addresses are stable)
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
        // Fill in the index-based pointer tables first (BL[i]/AP[i] are used below)

        int bw;
        int base;
        int h;
        int x;
        int avail;          // 扣掉间隔之后、留给楼体的总宽
        // Total width left for the buildings after the gaps are taken out
        int wsum;           // 权重之和
        // Sum of the weights
        int wW[8];          // 每栋的**宽度权重**（见下面"权重法"的说明）
        // **Width weight** of each building (see the "weight method" explanation below)
        int tintOff;        // 配色的起始档 —— 每局换个顺序，连玩两局不会"又是那排色"
        // Starting colour step — the order changes every round, so two rounds in a row are never "that row of colours again"

        sw = ui_scr_w();
        sh = ui_scr_h();
        if (sw <= 0) { sw = 411; }
        if (sh <= 0) { sh = 726; }

        gy = sh * 74 / 100;
        // 地形是本局的战果，换一局就推倒重来 —— 不清的话上一局打出来的洞会跟着下一局
        // The terrain is this round's battle damage, and a new round starts from scratch — without clearing, the holes from the last round follow into the next one
        // （`Layout` 就是"换一局"的入口：新城市 + 新地形）
        // (`Layout` is the entry point for "a new round": new city + new terrain)
        clearHoles();
        cloudsInit(sw, gy);
        // 天空不再"掷一个昼夜"，它跟着游戏时钟连续变化
        // The sky no longer "rolls a day or night"; it changes continuously with the game clock
        sky.Setup(sw, sh, gy);

        // ── 楼：**4~8 栋、宽度不一**（玩家要求："房子数量应该是 4 到 8 个不等，
        // ── Buildings: **4..8 of them, with differing widths** (the player's request: "the number of houses should vary from 4 to 8,
        //    宽度不固定，随机的"）────────────────────────────────────────
        //    the widths should not be fixed, they should be random") ──
        gap = sw / 40;
        gBldgN = 4 + ui_rand(5);            // 4..8
        avail = sw - gap * (gBldgN + 1);
        if (avail < 60) { avail = 60; }

        // 宽度用**权重法**：先随机每栋的权重，再按权重去分总宽。
        // Widths use a **weight method**: roll a weight for each building first, then divide the total width by weight.
        // ⚠ 别直接给每栋随机一个宽度 —— 那些数加起来**不等于**可用宽度，
        // ⚠ Do not just roll a width for each building — those numbers do **not** add up to the usable width,
        //   右边不是空一截就是溢出屏幕（"随机的"要的是宽窄不一，不是铺不满）。
        //   so the right side is either short by a stretch or overflows the screen ("random" means varying widths, not an unfilled row).
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
            // Too narrow to stand a monkey on
            if (i == gBldgN - 1)
            {
                // **最后一栋吃掉余量** ⇒ 右边严丝合缝；只要剩得下，就不留缝
                // **The last building eats the remainder** ⇒ the right side fits exactly; as long as what is left is enough, no gap is left
                int rest;
                rest = sw - gap - x;
                if (rest >= 22) { bw = rest; }
            }
            h = sh / 5 + ui_rand(sh / 4);
            // 两边的楼高一点，好站（猴子站在最左和最右那两栋上）
            // The buildings at both ends are taller to make them easier to stand on (the apes stand on the leftmost and rightmost ones)
            if (i == 0 || i == gBldgN - 1) { h = h + sh / 14; }
            (*BL[i]).Setup(x, bw, h, gy);
            (*BL[i]).SetTint((i + tintOff) % 8);
            x = x + bw + gap;
            i = i + 1;
        }

        (*AP[0]).StandOn(&(*BL[0]));
        (*AP[1]).StandOn(&(*BL[gBldgN - 1]));

        // 地上的树：**位置 / 数量 / 高矮都随机**（每局都不一样）
        // Trees on the ground: **position / count / height all random** (different every round)
        treeN = 3 + ui_rand(6);             // 3..8
        i = 0;
        while (i < treeN)
        {
            int tx;
            int th;
            int tw;
            tx = 12 + ui_rand(sw - 24);
            th = 24 + ui_rand(42);          // 24..65，高矮差一倍多才看得出"随机"
            // 24..65; only a difference of more than double makes the "randomness" visible
            tw = 12 + ui_rand(16);          // 12..27
            TREES[i].Setup(tx, gy + 4, th, tw);
            i = i + 1;
        }
        spawnT = 0;
    }

    /// <summary>
    /// 把**会动的精灵**都录成图块（开窗之后调一次，见 `main`）。
    /// Record all the **moving sprites** as blocks (called once after the window opens, see `main`).
    ///
    /// 三类精灵一起走这条路：形状只写一遍、贴出来带旋转/镜像/缩放，
    /// All three kinds of sprite go this way: the shape is written once, and stamping carries rotation/mirroring/scaling,
    /// 而且每帧从"十几次绘图调用"压成"一次贴图块"（实测 300 个精灵 × 60 帧：
    /// and each frame compresses from "a dozen-plus drawing calls" to "one block stamp" (measured with 300 sprites × 60 frames:
    /// 直接画 3531ms → 贴图块 2945ms）。
    /// drawing directly 3531ms → stamping blocks 2945ms).
    /// </summary>
    void MakeBlocks()
    {
        ban.MakeBlock();
        bird.MakeBlocks();
        ufo.MakeBlock();
        plane.MakeBlock();

        // ── 楼的窗户：**每栋各录一套**（宽高都不同）────────────────────────
        // ── Building windows: **one set recorded per building** (their widths and heights all differ) ──
        //
        // ⚠ 必须在 `Layout()` **之后**、且**有场景**时调 —— 开窗之前 `ui_create_block`
        // ⚠ It must be called **after** `Layout()` and **with a scene present** — before the window opens, `ui_create_block`
        //   会返回 0（块是宿主那边的资源，没有场景就建不起来）。
        //   returns 0 (blocks are a resource on the host side, and without a scene they cannot be created).
        // ⚠ 每局楼宽高都变 ⇒ **每次 Layout 之后都要重录**（这里 + `Restart` 各一处）。
        // ⚠ The building widths and heights change every round ⇒ **re-record after every Layout** (here, plus one place in `Restart`).
        {
            int i;
            i = 0;
            while (i < gBldgN) { (*BL[i]).MakeWinBlocks(); i = i + 1; }
        }

        // ── 云 / 路灯：按天光**分档录** ────────────────────────────────────
        // ── Clouds / street lamps: **recorded per daylight step** ──
        //
        // ⚠ 图块里的颜色是**录制那一刻定死的**，而这两样的颜色随昼夜连续变
        // ⚠ The colours inside a block are **fixed at the moment of recording**, while these two change continuously with the day/night cycle
        //   ⇒ 每档录一份，贴的时候按当前天光选最近的一档。
        //   ⇒ record one per step and pick the nearest step to the current daylight when stamping.
        //   24 真实分钟走完一天 ⇒ 每档约 1.5 分钟，档与档之间的颜色差看不出来。
        //   24 real minutes make a full day ⇒ about 1.5 minutes per step, and the colour difference between steps is invisible.
        //
        // ⚠ 录制期间要**临时改 `gDayL`**（云还要用"当地天空色" ⇒ 顺带调一次
        // ⚠ Recording has to **change `gDayL` temporarily** (the clouds also need the "local sky colour" ⇒ so `clockPartsAt`
        //   `clockPartsAt`）。用屏宽中点的高度当代表 —— 云分布在不同高度、天空色
        //   is called along the way). The height at the middle of the screen width is used as the representative — the clouds sit at different heights
        //   略有差异，但那个差异比档位差还小。
        //   with slightly different sky colours, but that difference is smaller than the step difference.
        // ⚠ **录完必须恢复 `gDayL`**：它决定天色，留着档位值会让第一帧的天色不对
        // ⚠ **`gDayL` must be restored after recording**: it decides the sky colour, and leaving the step value in place makes the first frame's sky colour wrong
        //   （下一帧 `Draw` 开头的 `clockCompute` 会重算，但那一帧已经画出去了）。
        //   (the `clockCompute` at the start of the next frame's `Draw` would recompute it, but that frame has already been drawn).
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
            // ⚠ The cloud centre is **low** inside the block: 2×ch from the top (the biggest bump reaches there) — subtract it when stamping
            cloudShapeAt(CLOUD_STD_W / 2, CLOUD_STD_W * 2 / 3, CLOUD_STD_W, d);
            cloudBid[k] = ui_end_block();

            lampBid[k] = ui_create_block(LAMP_STD_W, LAMP_STD_H, 0);
            // 灯柱底放在图块底边 ⇒ 图块中心相对灯柱底偏 (8.5, -23)
            // The pole base sits on the block's bottom edge ⇒ the block centre is offset (8.5, -23) from the pole base
            lampShapeAt(1, LAMP_STD_H, d);
            lampBid[k] = ui_end_block();
            k = k + 1;
        }

        // 树的绿档**本来就是固定色**（与天光无关）⇒ 只录 3 个
        // The trees' green steps **are fixed colours by construction** (independent of daylight) ⇒ record only 3
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
    /// The monkey (0 / 1) **nearest** to the x coordinate `px` — used by the UFO to pick a target when taking revenge.
    ///
    /// ⚠ 判据是「离**飞碟**最近」（玩家原话："他只找离他最近的猴子"），
    /// ⚠ The criterion is "nearest to the **UFO**" (the player's words: "it only goes for the monkey nearest to it"),
    ///   不是"离被打中那栋楼最近"、也不是"离发射者最近"。
    ///   not "nearest to the building that was hit" and not "nearest to the thrower".
    /// ⚠ 平手取左边那只（`<=`）：**必须有个确定的裁决** —— 同一帧里若两次调用
    /// ⚠ A tie goes to the left one (`<=`): **there has to be a definite verdict** — if two calls in the same frame
    ///   给出不同答案，目标就会在半路改口，飞碟会在空中拐一个莫名其妙的弯。
    ///   gave different answers, the target would change its mind mid-flight and the UFO would turn an inexplicable corner in mid-air.
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
        // ⚠ The sound effect hangs off **here** (the action itself), not off some input path —
        //   原先它只写在触摸那支里，于是**键盘回车发射是一声不响的**
        //   it used to be written only in the touch branch, so **firing with the keyboard's Enter was silent**
        //   （游戏是"全触摸"，键盘只是兜底，所以这个缺口一直没被发现）。
        //   (the game is "all touch" and the keyboard is only a fallback, so the gap was never noticed).
        //   挂在动作上则键盘 / 触摸 / 将来真加了自动发射，都自动有。
        //   Hanging it off the action means keyboard / touch / any future auto-fire all get it automatically.
        SfxFire();
    }

    // 一拍物理；返回 1 = 这一拍结算了（换人或结束）
    // One tick of physics; returns 1 = this tick produced a resolution (turn changed or game over)
    int Tick()
    {
        int r;
        int i;
        int hit;

        sky.Advance();
        spawnT = spawnT + 1;

        // ── 天上添东西：节拍到了掷一次，天上空着才放 ──
        // ── Adding something to the sky: roll once the cadence comes round, and only spawn if the sky is free ──
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
        // A meteor comes by occasionally at night

        // 多态：三个派生类各走各的，这里一行覆盖
        // Polymorphism: the three derived classes each move their own way, covered by one line here
        i = 0;
        while (i < 3)
        {
            FLY[i]->Step(sw, gy);
            i = i + 1;
        }

        // ── 飞碟的激光打完了吗？→ 猴子死、这一局结束 ──────────────────────
        // ── Has the UFO's laser finished firing? → the monkey dies and the round ends ──
        //
        // ⚠ 结算放在 **Game** 这一层，而不是 `Ufo::RageStep` 里：飞碟只管
        // ⚠ The resolution lives at the **Game** level, not inside `Ufo::RageStep`: the UFO takes care only of
        //   "我怎么飞、什么时候开火"，"谁死了、这一局算不算完"是**游戏的规则** ——
        //   "how do I fly, when do I fire", while "who died, does this round count as over" is a **rule of the game** —
        //   规则挂到飞行物身上，将来加第二艘飞碟就得在两处各写一遍。
        //   hang the rule on the flying object and a second UFO later would mean writing it in two places.
        // ⚠ `fired` 是**必须的**：`shot` 到顶之后会一直停在 `RAGE_SHOT`，
        // ⚠ `fired` is **essential**: once `shot` reaches the cap it stays at `RAGE_SHOT`,
        //   没有这个闸门就是"每拍打死一次、每拍结束一局"。
        //   and without this gate it would be "kill once per tick, end the round once per tick".
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
            // Hit a monkey?
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
            // Hit something **flying**? → mid-air explosion, no score, immediate turn change ("wasting a banana", the same as the BASIC version)
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
                            // ⚠ **Only the UFO is different**: hitting it = **provoking revenge**, not shooting it down.
                            //   鸟和飞机照旧击落（代价仍然只是"白扔一个香蕉"）。
                            //   Birds and planes are still shot down (the cost remains just "wasting a banana").
                            // ⚠ 这里按**下标**判（`FLY[1]` 是飞碟，见 `Bind`）——
                            // ⚠ This judges by **index** (`FLY[1]` is the UFO, see `Bind`) —
                            //   要是哪天调整了 `Bind` 里的顺序，这一行也得跟着改。
                            //   if the order in `Bind` is ever rearranged, this line has to change with it.
                            //   三种飞行物的**走法本来就不一样**，用 `kind` 判更稳，
                            //   The three flyers' **movements are inherently different**, so judging by `kind` would be more robust,
                            //   所以判据写在这里，别挪去别处再抄一份。
                            //   which is why the criterion is written here — do not move it elsewhere and copy it.
                            if (i == 1) { ufo.BeginRage(NearestApeTo(ufo.x)); }
                            else { FLY[i]->live = 0; }
                            hit = -3;
                        }
                    }
                    i = i + 1;
                }
            }

            // 打到楼？
            // Hit a building?
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
                // Landed
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
                // Mid-air explosion: **no score**, that shot is wasted
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
                // ⚠ **This branch covers both "hitting a building" and "landing"** (see the two `hit = -2` places above) —
                //   两种都该留下痕迹，而且半径分两档：撞楼炸得大一点（那是"打进墙里"），
                //   both should leave a mark, with two radius bands: a building hit blasts bigger (that is "going into the wall")
                //   落地小一点。判据是"落点上方有没有楼"，与判定 `hit` 用的是同一个 `Covers`。
                //   and a ground hit smaller. The criterion is "is there a building above the impact point", using the same `Covers` as the `hit` test.
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
                // Flew off screen: no explosion, straight into the next turn
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
                // Reaching the score ends it — the winner is **always the one who just scored** (scores go up one at a time,
                // 不存在两人同时到顶），所以这里不必再比一次分数。⚠ 但**不能**因此
                // so both cannot reach the top at once), which is why there is no need to compare scores again here. ⚠ But that **must not** be used
                // 就用 `SfxWin()` 一把梭：另一条结束路径（被飞碟清场）的胜负与分数无关，
                // to just fire `SfxWin()` across the board: the other ending path (wiped out by the UFO) has nothing to do with the score,
                // 它在上面单独响了 `SfxLaser()`。
                // and it sounds `SfxLaser()` separately above.
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
            // Holding a direction key auto-repeats
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
    // ── The **trajectory preview** while aiming ──
    // 用与 `Banana::Step` **完全同一套**公式试算一遍（只是不改真香蕉的状态），
    // Dry-run the **exact same** formulas as `Banana::Step` (only without changing the real banana's state),
    // 每 5 拍点一个点。手感差别很大：原来是"凭感觉蒙"，现在是"看得见落点".
    // dotting one point every 5 ticks. The feel is very different: it used to be "guessing by feel", now "you can see the landing point".
    //
    // ⚠ 公式必须与 `Step` 一致 —— 两处各写一份就一定会漂（预览的弧线和实际飞行的
    // ⚠ The formulas must match `Step` — two separate copies are bound to drift (the preview's arc and the actual flight's
    //   弧线不一样，比没有预览更糟）。这里连常量都引用同一批宏。
    //   arc differ, which is worse than having no preview at all). Here even the constants reference the same macros.
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
        // Same as Banana::Launch: the wind gives no impulse at launch
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
            // Below the horizon: stop
            if (hitX == 0)
            {
                // 撞楼就停在这一格（落点标记画在**它撞上的那栋楼**的屋顶上）
                // A building hit stops it at this cell (the landing marker is drawn on the roof of **the building it hit**)
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
        // Landing crosshair: see at a glance "where this shot will land"
        if (hitX != 0)
        {
            ui_rect(hitX - 9, hitY - 1, 19, 3, 0xCCFF5060, 1, 0, 0);
            ui_rect(hitX - 1, hitY - 9, 3, 19, 0xCCFF5060, 1, 0, 0);
            ui_circle(hitX, hitY, 10, 0x80FF5060, 1, 0);
        }
    }

    // ── 操作面板：**直接上手拖**（与 BASIC 版同一套操控）────────────────────
    // ── The control panel: **drag it directly with your finger** (the same controls as the BASIC version) ──
    //
    // 两根值是**可拖的横条**、发射是一个**大按钮** —— 手指点/拖到哪就设到哪，
    // The two values are **draggable bars** and firing is a **big button** — wherever the finger taps/drags is what gets set,
    // 不需要键盘。键盘（←→↑↓ + 回车）仍保留，接物理键盘时照旧能用。
    // with no keyboard needed. The keyboard (←→↑↓ + Enter) is still kept and works as before with a physical one.
    //
    // ⚠ 条要**够厚**：`barH = 30` 是照手指定的（8px 的细条在手机上根本按不准）。
    // ⚠ The bars have to be **thick enough**: `barH = 30` is set for fingers (an 8px thin bar cannot be hit reliably on a phone).
    //   命中判定与绘制**共用同一组几何**（`PanelGeometry` 算一次，两边都读），
    //   Hit testing and drawing **share one set of geometry** (`PanelGeometry` computes it once, both read it),
    //   两边各算一遍就是"看着在条上、点了没反应"。
    //   and computing it twice is "it looks like it is on the bar but tapping does nothing".
    // ⚠ 三行**不许重叠**：按钮 6..40、角度条 46..76、力度条 82..112、面板高 122。
    // ⚠ The three rows **must not overlap**: button 6..40, angle bar 46..76, power bar 82..112, panel height 122.
    //   第一版把发射键摆在左上、角度条摆在 y+34 —— 键的下沿压在角度条上。
    //   The first version put the fire button top-left and the angle bar at `y+34` — the button's lower edge lay on the angle bar.
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

    /* ⚠ 第 5 个形参是 `int which`（0 = 角度 / 1 = 力度），**不是** `char* name`。 */
    // ⚠ The 5th parameter is `int which` (0 = angle / 1 = power), **not** `char* name`.
    //
    /* ⚠ 原先它是 `char* name`，而这条链上 `char*` **形参**是坏的：实参传进去、 */
    // ⚠ It used to be `char* name`, and on this chain a `char*` **parameter** is broken: the argument goes in,
    /* ⚠ 函数里读出来是**空串**（最小复现：`void f(int y, char* s) { ui_text(10,y,s,…); }` */
    // ⚠ but reading it inside the function gives back an **empty string** (minimal repro: `void f(int y, char* s) { ui_text(10,y,s,…); }`
    /* ⚠ 调 `f(100,"ABC")` ⇒ 宿主收到 `""`）。症状极隐蔽 —— 两根条照画、只是**标签不见了**， */
    // ⚠ called as `f(100,"ABC")` ⇒ the host receives `""`). The symptom is very subtle — both bars are drawn normally,
    /* ⚠ 而且它在改动之前就是这样（拿 HEAD 版本跑 trace 一样是 `str=""`）。 */
    // ⚠ only **the label is missing** — and it behaved that way before the change too (running trace on the HEAD version gives `str=""` all the same).
    /* ⚠ 把字符串**写在 ui_text 调用点上**（字面量直传）是好的，所以这里改成传一个选择子、 */
    // ⚠ Putting the string **at the ui_text call site** (passing a literal straight through) does work, so this was changed to pass a selector
    /* ⚠ 在函数内部点出字面量。判据见 `--trace-draw` 里那两行的 str。 */
    // ⚠ and spell the literal out inside the function. The criterion is the str of those two lines in `--trace-draw`.
    void DrawBar(int y, int val, int vmax, int col, int which)
    {
        int w;
        w = barW * val / vmax;
        if (w > barW) { w = barW; }
        ui_rect(barX, y, barW, barH, 0x33FFFFFF, 1, 0, 6);
        if (w > 0)
        {
            ui_rect(barX, y, w, barH, col, 1, 0, 6);
        }
        if (which == 0) { ui_text(14, y + barH - 9, gLang == 0 ? "角度" : "Angle", C_TEXT_DIM, 12, VML_ANCHOR_LEFT); }
        else { ui_text(14, y + barH - 9, gLang == 0 ? "力度" : "Power", C_TEXT_DIM, 12, VML_ANCHOR_LEFT); }
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
        if (state == ST_AIM) { ui_text(fireX + fireW / 2, fireY + 23, gLang == 0 ? "发 射" : "FIRE", 0xFF201810, 15, VML_ANCHOR_CENTER); }
        else { ui_text(fireX + fireW / 2, fireY + 23, gLang == 0 ? "飞行中" : "In flight", 0xFF201810, 12, VML_ANCHOR_CENTER); }

        DrawBar(barAy, cur->angle, 90, C_WIND, 0);      /* 0 = 角度 / Angle */
        // 0 selects the angle bar (the `which` argument of DrawBar)
        DrawBar(barPy, cur->power, 100, C_BANANA, 1);   /* 1 = 力度 / Power */
        // 1 selects the power bar (the `which` argument of DrawBar)

        ui_text(sw - 12, panY + 22, gLang == 0 ? "拖动调值 · 点发射" : "Drag to set, tap to fire", C_TEXT_DIM, 11, VML_ANCHOR_RIGHT);
    }

    // ── 触摸 / 鼠标：按下的那一点落在哪根条上就改哪个值 ────────────────────
    // ── Touch / mouse: whichever bar the point lands on is the value that changes ──
    // `isDown = 1` 只对**按下那一刻**认（拖动路过不算），免得调条时误射 —— 与 BASIC 版一致。
    // `isDown = 1` is honoured only for **the moment of pressing** (passing over while dragging does not count), to avoid firing by accident while adjusting a bar — the same as the BASIC version.
    void HandlePoint(int isDown)
    {
        int px;
        int py;

        if (over != 0)
        {
            if (isDown != 0) { over = 2; }        // 结束画面：点一下 = 请求退出
            // End screen: a tap = request to quit
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
            // The fire sound is inside Fire(), do not add another one here
        }
    }

    void Draw()
    {
        int i;
        Entity* actors[16];
        int actorN;
        int onBldg;         // 这个弹坑是不是开在楼上（见 `HoleOnBldg`）
        // Is this crater on a building (see `HoleOnBldg`)
        int k;
        int j;
        int hx;             // 画洞蒙版时的**钳制后**圆心/半径（见那段注释）
        // The **clamped** centre/radius used when drawing the hole mask (see that passage)
        int hy;
        int hr;
        int hn;             // 本楼有几个洞（合并之后）
        // How many holes this building has (after merging)
        int changed;        // 合并循环：这一趟有没有合并过
        // Merge loop: did this pass merge anything
        int a;
        int b2;
        int ddx;
        int ddy;
        int dd;

        // ⚠ **每帧的第一件事**：把这一刻的天光 / 暖色 / 日月位置算出来。
        // ⚠ **The first thing every frame**: compute this moment's daylight / warm colour / sun and moon positions.
        //   后面每一处配色（天、楼、窗、地、云、弹坑）都读它算出来的那几个全局量。
        //   Every later colour decision (sky, buildings, windows, ground, clouds, craters) reads the globals it computes.
        clockCompute(sw, gy, 30);

        // ⚠⚠ **每帧必须清一次场景图元** —— 这一句看着多余（下面 `Sky` 会用渐变铺满整屏，
        // ⚠⚠ **The scene's figures must be cleared once per frame** — this line looks redundant (below, `Sky` fills the whole screen with a gradient,
        //   视觉上早就盖住了上一帧），但**图元数组不是"视觉上的覆盖"，是只增不减的列表**：
        //   visually covering the previous frame long ago), but **the figure array is not "visual coverage", it is a list that only grows**:
        //   不清的话每帧的几百个图元一直堆，跑几十帧就撞上宿主的 `MaxFigures = 12000`，
        //   without clearing, the few hundred figures per frame keep piling up, and after a few dozen frames you hit the host's `MaxFigures = 12000`,
        //   之后**新加的绘制被静默丢弃** —— 症状是"画面里靠后画的东西（猴子、后两栋楼）
        //   after which **new drawings are silently dropped** — the symptom being "things drawn later in the frame (the apes, the last two buildings)
        //   逐个消失，而且越跑越少"，而**坐标、颜色全都是对的**（trace 里能看到它们被正常发出）。
        //   disappear one by one, and fewer and fewer are drawn", while **the coordinates and colours are all correct** (the trace shows them being issued normally).
        //   实测：这份程序跑到第 50 帧左右开始缺，正是 12000 ÷ 每帧图元数。
        //   Measured: this program starts dropping things around frame 50, which is exactly 12000 ÷ figures per frame.
        //   宿主那边给的提示是「程序很可能漏了 ui_clear()」——就是这么回事。
        //   The hint the host gives is "the program has probably forgotten ui_clear()" — and that is exactly what it is.
        // 用**天顶色**清（与 BASIC 版同一口径）：天空随后会用渐变整块盖上，
        // Clear with the **zenith colour** (the same convention as the BASIC version): the sky then covers it with a gradient,
        //   清成什么色其实看不见，但取天色能保证万一渐变没铺满时露出的也是天空色。
        //   so the colour rarely shows, but using a sky colour guarantees that anything showing when the gradient does not cover everything is still sky coloured.
        ui_clear(clockZenith());

        // 多态绘制：全部当 Entity 指针调 Draw()，实现由各自决定
        // Polymorphic drawing: everything is called as an `Entity` pointer via `Draw()`, with the implementation decided by each object
        // ⚠ 画表**按本局实际栋数拼**，不能写死下标 ——
        // ⚠ The draw list is **assembled from this round's actual building count** and must not hard-code indices —
        //   原来写的是 actors[1..4] = 四栋楼，栋数一变（4~8）就会漏画或越界。
        //   it used to be `actors[1..4]` = four buildings, and once the count changes (4..8) that misses some or goes out of bounds.
        actorN = 0;
        actors[actorN] = &sky;     actorN = actorN + 1;
        i = 0;
        while (i < gBldgN) { actors[actorN] = &(*BL[i]); actorN = actorN + 1; i = i + 1; }
        actors[actorN] = &(*AP[0]); actorN = actorN + 1;
        actors[actorN] = &(*AP[1]); actorN = actorN + 1;
        actors[actorN] = &ban;      actorN = actorN + 1;


        // ⚠ **`actors[0]` 是 `Sky`，它会把整片天空铺一遍** —— 所以云必须画在它**之后**，
        // ⚠ **`actors[0]` is `Sky`, and it lays down the whole sky** — so the clouds must be drawn **after** it,
        //   否则刚画好的云立刻被天空盖掉（实测：云的位置颜色都对，屏幕上却什么都没有）。
        //   otherwise the freshly drawn clouds are immediately covered by the sky (measured: the clouds' positions and colours were all right, yet nothing appeared on screen).
        //   这与 BASIC 版 `drawScene` 的次序一致：天空 → 星星 → 日月 → 云 → 飞行物 → 楼。
        //   This matches the order in the BASIC version's `drawScene`: sky → stars → sun/moon → clouds → flyers → buildings.
        //   代价是这里得把 `actors` 的循环拆成两段 —— 云不属于任何 `Entity` 对象。
        //   The cost is that the `actors` loop has to be split into two here — the clouds are not an `Entity` object.
        actors[0]->Draw();
        cloudsDraw(sw, gy);
        meteorDraw(sw, gy);

        // ── 建筑层：**楼 − 自己身上的洞** ───────────────────────────────
        // ── The building layer: **building − the holes on it** ──
        //
        // 三层模型（玩家定的）：**背景层 / 建筑层 / 精灵层**。
        // The three-layer model (set by the player): **background layer / building layer / sprite layer**.
        // 建筑层"可以被炸穿" = 楼体**挖掉**洞 ⇒ 上一层画好的背景**直接从洞里透出来**
        // The building layer can be "blown through" = holes are **carved out** of the body ⇒ the background already drawn on the layer above **shows straight through the hole**
        // （云、日月都是活的），不需要在洞里补画任何东西。
        // (the clouds, sun and moon are live), with nothing to redraw inside the hole.
        //
        // ⚠ 上一版是"每个洞开一个洞口蒙版、把背景**重画一遍**"。像素结果对，但：
        // ⚠ The previous version "opened a crater mask per hole and **redrew the background**". The pixels were right, but:
        //   · 24 个洞 = **24 倍**背景图元（天空渐变 + 星 + 日月 + 云各来一遍）；
        //   · 24 holes = **24×** the background figures (sky gradient + stars + sun/moon + clouds, each repeated);
        //   · 而且洞一多就撞 `MaxFigures = 12000`（新图元被**静默丢弃**）。
        //   · and with many holes you hit `MaxFigures = 12000` (new figures get **silently dropped**).
        //   现在每个洞只花"一个蒙版形状"，楼体只画一遍。
        //   Now each hole costs only "one mask shape" and the body is drawn once.
        //
        // ⚠ `actors[1..gBldgN]` 是楼，后面依次是猴子、香蕉。这个循环原先是一整趟跑完的，
        // ⚠ `actors[1..gBldgN]` are the buildings, followed by the apes and the banana. This loop used to run through in one pass,
        //   于是弹坑只能画在**最后**，把香蕉也盖住了 —— 玩家说的
        //   so the craters could only be drawn **last**, covering the banana too — that is where the player's
        //   「被炸穿的地方还是会挡住香蕉」就是这么来的（香蕉是飞在半空的，
        //   "the blown-through places still block the banana" came from (the banana flies in mid-air,
        //   而坑是开在墙上的，墙在香蕉**后面**）。
        //   while the holes are in the walls, and the walls are **behind** the banana).
        //
        // ⚠ 顺序：**屋顶细节 → 开蒙版 → 楼体**。屋顶细节（水箱 / 天线）伸到楼体矩形
        // ⚠ Order: **roof details → open the mask → the body**. The roof details (tank / antenna) reach **above**
        //   **上方**，套进"楼体矩形 − 洞"的蒙版里会被整块裁掉。
        //   the building rectangle, so putting them under the "building rectangle − holes" mask would clip them away entirely.
        i = 1;
        k = 0;
        while (i <= gBldgN)
        {
            (*BL[k]).DrawRoof();

            // 先数本栋有几个洞 —— **开不开蒙版由它决定**（见下面的 `if`）
            // First count how many holes this building has — **that decides whether the mask is opened** (see the `if` below)
            onBldg = 0;
            j = 0;
            while (j < holeN)
            {
                if (HoleOnBldg(j, k, gy) != 0) { onBldg = onBldg + 1; }
                j = j + 1;
            }

            // ⚠⚠ **没洞就不开蒙版** —— 这一条是真机实测出来的大头。
            // ⚠⚠ **No holes means no mask** — this one was the big win measured on device.
            //   蒙版在平台侧是 `clipPath`，而 Android 硬件加速下每次路径裁剪都要
            //   On the platform side the mask is a `clipPath`, and under Android hardware acceleration every path clip has to
            //   重建裁剪层；一帧 6 栋楼各开一次，单单这一项就吃掉 **30ms**（17fps → 26fps）。
            //   rebuild the clip layer; opening one per building six times a frame ate **30ms** on its own (17fps → 26fps).
            //   而**没有洞的楼，蒙版里除了底矩形什么都没有** —— 它起不到任何作用。
            //   And **for a building with no holes the mask contains nothing but the base rectangle** — it does nothing at all.
            //   （开局时全城都没洞，这一条把所有楼的裁剪都省掉了。）
            //   (At the start of a round the whole city has no holes, so this skips the clipping for every building.)
            if (onBldg == 0)
            {
                (*BL[k]).DrawBody();
            }
            else
            {
            // 蒙版 = 楼体矩形 − 本栋楼上所有的洞（`SUBTRACT` 一次算完，不必逐洞嵌套）
            // Mask = building rectangle − all the holes on this building (`SUBTRACT` does it in one go, no per-hole nesting)
            ui_mask_begin();
            // ⚠ 底矩形**比楼体大一圈**（`MASK_PAD`）—— 见那个常量的说明：
            // ⚠ The base rectangle is **one ring larger than the body** (`MASK_PAD`) — see that constant's explanation:
            //   楼外那一圈本来就没有绘制，所以洞挖进去不会露出"肉"；
            //   that ring outside the building has nothing drawn in it, so a hole digging into it exposes no unwanted fill;
            //   而底矩形只贴着楼体的话，洞一贴边就被迫缩小（玩家报的「就一个小洞」）。
            //   whereas if the base rectangle hugged the body, any hole touching the edge would be forced to shrink (the player's "it is just a small hole").
            ui_rect((*BL[k]).Left() - MASK_PAD, (*BL[k]).RoofY() - MASK_PAD,
                    (*BL[k]).w + MASK_PAD * 2, (*BL[k]).h + MASK_PAD * 2, 0xFFFFFFFF, 1, 0, 0);
            ui_mask_end(1);

            // 没有洞就别开那一段（`SUBTRACT` 空集本来是合法的，但少一段就少一次逐点判定）
            // With no holes, skip that section (`SUBTRACT` with an empty set is legal, but one less section is one less per-point test)
            if (onBldg > 0)
            {
                // ── 先把本楼的洞收进一个临时表，**钳制进楼、再合并重叠的** ──────────
                // ── First gather this building's holes into a temporary table, **clamp them into the building, then merge the overlapping ones** ──
                //
                // 这两步都是为了让"楼 − 洞"能被矢量后端**折叠成一条路径**。折叠不成立时
                // Both steps are so that "building − holes" can be **folded into a single path by the vector back end**. When the fold does not hold,
                // 整个窗口会退回光栅后端 —— 画面立刻变糊、帧率从几十掉到 6（实测 6.2fps）。
                // the whole window falls back to the raster back end — the picture immediately goes blurry and the frame rate drops from tens to 6 (measured 6.2fps).
                //
                // ① **钳制**：折叠要求每个洞整个落在楼里。洞一旦探出楼外，
                // ① **Clamping**: the fold requires every hole to lie entirely inside the building. Once a hole pokes outside,
                //    "楼外那块洞"在 even-odd 下只穿过一次 ⇒ 被判成**内部** ⇒ 画出一块不该有的肉。
                //    "the part of the hole outside" is crossed only once under even-odd ⇒ it is judged **inside** ⇒ producing a lump of fill that should not be there.
                // ② **合并**：折叠要求洞与洞互不重叠 —— 重叠处同样会被 even-odd 填实。
                // ② **Merging**: the fold requires holes not to overlap — an overlap likewise gets filled in by even-odd.
                //    这条在数学上绕不过去（NonZero 也一样），只能**不让它重叠**。
                //    There is no mathematical way around this (NonZero behaves the same), so the only option is **not to let them overlap**.
                //    ⚠ 观感上反而更对：炸得太密，破洞本来就该连成一片。
                //    ⚠ Visually it is actually more correct: when the shelling is dense, the holes should join into one.
                hn = 0;
                j = 0;
                while (j < holeN)
                {
                    if (HoleOnBldg(j, k, gy) != 0)
                    {
                        hx = holeX[j]; hy = holeY[j]; hr = holeR[j];
                        // 边界用**放大后的底矩形**（不是楼体本身）—— 洞因此基本不会被缩，
                        // The bounds use the **enlarged base rectangle** (not the body itself) — so holes are essentially never shrunk,
                        // 只有极端情况（洞比 PAD 还大）才动它
                        // and only in the extreme case (a hole bigger than PAD) are they touched
                        ClampHoleToBuilding(&hx, &hy, &hr, (*BL[k]).Left() - MASK_PAD,
                                            (*BL[k]).Right() + MASK_PAD,
                                            (*BL[k]).RoofY() - MASK_PAD, gy + MASK_PAD);
                        mhx[hn] = hx; mhy[hn] = hy; mhr[hn] = hr;
                        hn = hn + 1;
                    }
                    j = j + 1;
                }
                // 反复合并，直到没有一对重叠（合并会造出更大的圆，可能又压到别人）
                // Merge repeatedly until no pair overlaps (merging produces a larger circle that may then touch another)
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
                            // Overlapping (**tangency does not count**, see the host's criterion)
                            {
                                MergeHoles(mhx[a], mhy[a], mhr[a], mhx[b2], mhy[b2], mhr[b2],
                                           &hx, &hy, &hr);
                                // 合并出来的圆可能探出楼外 ⇒ 再钳一次
                                // The merged circle may poke outside the building ⇒ clamp it once more
                                ClampHoleToBuilding(&hx, &hy, &hr, (*BL[k]).Left() - MASK_PAD,
                                                    (*BL[k]).Right() + MASK_PAD,
                                                    (*BL[k]).RoofY() - MASK_PAD, gy + MASK_PAD);
                                mhx[a] = hx; mhy[a] = hy; mhr[a] = hr;
                                // 把最后一个搬到 b2 的位置（顺序无所谓）
                                // Move the last one into b2's slot (the order does not matter)
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
            // ← ends the else branch "open the mask only when there are holes"

            i = i + 1;
            k = k + 1;
        }

        // ── 洞口的断面：**不画了**（玩家："炸完了怎么还留下一个圆环？"）────────
        // ── The crater rim cross-section: **no longer drawn** (the player: "why is there still a ring left after it blows up?") ──
        //
        // 这里原先描两圈：内壁一圈暗色（"墙厚"）+ 左上受光一线（"翻起来的边"）。
        // This used to draw two rings: a dark ring for the inner wall ("wall thickness") + a lit line at the upper left ("the edge turned up").
        // 那是**"洞里涂天空色"那个年代的补丁** —— 当时洞和楼的边界靠这一圈才分得开，
        // That was a **patch from the era of "paint the hole with the sky colour"** — back then the boundary between hole and building needed that ring to be visible,
        // 不然读出来像一张贴上去的圆纸片。
        // otherwise it read as a round paper disc stuck on top.
        //
        // 现在洞是**真的挖掉了**（建筑层 = 楼 − 洞），洞沿**天然就是楼的边界**：
        // Now the hole is **really carved out** (building layer = building − holes), so the hole's edge **is naturally the building's boundary**:
        // 一边是楼体色、一边是透出来的天空，对比本来就够。再套一圈就成了
        // one side is the body colour and the other the sky showing through, which is contrast enough already. Adding another ring turns it into
        // "炸穿的洞"上额外挂的一圈装饰 —— 玩家一眼看出来不对。
        // an extra ring of decoration hung on a "blown-through hole" — and the player spots it at once.
        //
        // ⚠ 洞的边界**不需要**任何描边来"帮助识别"。真觉得糊（比如楼色与天空接近），
        // ⚠ The hole's boundary **does not need** any outline to "help identify it". If it really looks unclear (say the building colour is close to the sky),
        //   那要调的是**楼的配色**，不是给洞加圈。
        //   what needs adjusting is **the buildings' colour scheme**, not a ring on the hole.

        holesDraw(gy, sh);          // 地上的坑（挖土）
        // Ground craters (digging)

        // ── 精灵层：猴子、香蕉（`actors[0]` 是天空、`actors[1..gBldgN]` 是楼，都已画过）
        // ── Sprite layer: apes, banana (`actors[0]` is the sky and `actors[1..gBldgN]` are the buildings, all already drawn) ──
        //
        // ⚠ 起点必须是 `gBldgN + 1`。原先这里**没有赋起点**，沿用上面洞循环遗留下来的 `i`
        // ⚠ The start index must be `gBldgN + 1`. Originally there was **no assignment of the start**, so it reused the `i` left over from the hole loop above
        //   ⇒ 下标变成了**洞的数量**（拿一个不相干的计数当数组下标）：
        //   ⇒ the index became **the number of holes** (using an unrelated counter as an array index):
        //   洞少时把天空和楼又画一遍（**盖掉云和洞口断面**），洞多时前面的猴子被整段跳过。
        //   with few holes it drew the sky and the buildings again (**covering the clouds and the crater rim**), and with many holes the earlier apes were skipped entirely.
        //   这是本仓"变量复用出了边界"的又一个实例 —— 循环变量用完就该显式重置。
        //   This is another instance of this repo's "variable reuse going out of bounds" — a loop variable should be reset explicitly once it is done.
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
        // Ground: a bright grey street by day, darkened at night (the same daylight convention as the buildings)
        ui_rect(0, gy, sw, sh - gy, mixcol(12, 12, 22, 74, 78, 86, gDayL), 1, 0, 0);
        // 街灯：**地面之后**画（灯杆是立在地上的，画在地面前会被地面啃掉一截）
        // Street lamps: drawn **after the ground** (the poles stand on the ground, and drawn before it the ground would eat a bite out of them)
        lampsDraw(sw, gy, sh);

        // ── 树：**画在所有楼之后** ────────────────────────────────────
        // ── Trees: **drawn after all the buildings** ──
        // ⚠ 原来画在楼**之前**，注释写的是"楼房后画会把树挡住，正好得到树长在楼缝里的观感"。
        // ⚠ They used to be drawn **before** the buildings, with a comment saying "drawing the buildings later will cover the trees, which gives exactly the look of trees growing in the gaps".
        //   但楼缝只有 `gap = sw/40`（十几像素），树基本全被挡掉 —— 玩家看不到地上有树，
        //   But the gaps are only `gap = sw/40` (a dozen-odd pixels), so the trees were almost entirely covered — the player could not see any trees on the ground,
        //   于是又提了一次「地上还有随机的树，数量大小，位置随机」。
        //   and so asked again for "there should also be random trees on the ground, random count, size and position".
        //   街上种的树本来就该**站在楼前面**（也遮住楼脚，画面更有层次），与 BASIC 版一致。
        //   Trees planted along a street should **stand in front of the buildings** (they also hide the base of the building and give the picture more depth), the same as the BASIC version.
        i = 0;
        while (i < treeN)
        {
            TREES[i].Draw();
            i = i + 1;
        }

        DrawAimPreview();

        // 爆炸：冲击波 + 火球 + 碎屑
        // Explosion: shockwave + fireball + debris
        if (state == ST_BOOM && boomT <= BOOM_TICKS)
        {
            int r;
            int k;
            int dx;
            int dy;

            // 半径随时间线性长到 `6 + BOOM_TICKS*3`（= 42，直径 84）——
            // The radius grows linearly to `6 + BOOM_TICKS*3` (= 42, diameter 84) —
            // 时间与大小由 `BOOM_TICKS` 一处决定，见那个常量的说明。
            // both time and size are decided by `BOOM_TICKS` in one place, see that constant's explanation.
            r = 6 + boomT * 3;
            ui_circle(boomX, boomY, r + 5, 0x30FF7020, 1, 0);         // 半透明冲击波
            // Translucent shockwave
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
        // Hit banner (who hit whom)
        if (state == ST_BOOM && hitBy >= 0 && boomT <= BOOM_TICKS)
        {
            if (hitBy == 0) { ui_text(sw / 2, gy / 2, gLang == 0 ? "橙猴命中！" : "Orange hit!", C_APE0, 26, VML_ANCHOR_CENTER); }
            else { ui_text(sw / 2, gy / 2, gLang == 0 ? "紫猴命中！" : "Purple hit!", C_APE1, 26, VML_ANCHOR_CENTER); }
        }

        hud.Draw(&(*AP[0]), &(*AP[1]), turn, sw);
        wind.Draw(sw, 44);
        DrawAimPanel();

        if (over != 0)
        {
            // 横幅加高到 100：被飞碟清场时要放**两行**（怎么输的 + 下次别这么干）
            // The banner is raised to 100: being wiped out by the UFO needs **two lines** (how you lost + do not do that next time)
            ui_rect(0, sh / 2 - 50, sw, 100, 0xE0101020, 1, 0, 0);
            if (overKind == 1)
            {
                // ⚠ 这一局的胜负**与分数无关**，所以绝不能落到下面"比分数"那几支去 ——
                // ⚠ The outcome of this round has **nothing to do with the score**, so it must never fall through to the "compare scores" branches below —
                //   否则一只猴子被激光打死，屏幕上却在报"紫猴获胜！"，玩家一头雾水。
                //   otherwise a monkey is killed by the laser and the screen announces "Purple wins!", leaving the player baffled.
                ui_text(sw / 2, sh / 2 - 18, gLang == 0 ? "飞碟清场！" : "UFO wipeout!", 0xFFFF6060, 26, VML_ANCHOR_CENTER);
                ui_text(sw / 2, sh / 2 + 10, gLang == 0 ? "别打飞碟 —— 它会记住你" : "Don't hit the UFO - it remembers", C_TEXT_DIM, 14, VML_ANCHOR_CENTER);
            }
            else if ((*AP[0]).score > (*AP[1]).score)
            {
                ui_text(sw / 2, sh / 2 - 2, gLang == 0 ? "橙猴获胜！" : "Orange wins!", C_APE0, 24, VML_ANCHOR_CENTER);
            }
            else if ((*AP[1]).score > (*AP[0]).score)
            {
                ui_text(sw / 2, sh / 2 - 2, gLang == 0 ? "紫猴获胜！" : "Purple wins!", C_APE1, 24, VML_ANCHOR_CENTER);
            }
            else
            {
                ui_text(sw / 2, sh / 2 - 2, gLang == 0 ? "平局" : "Draw", C_TEXT, 24, VML_ANCHOR_CENTER);
            }
            ui_text(sw / 2, sh / 2 + 36, gLang == 0 ? "点一下屏幕退出" : "Tap to quit", C_TEXT_DIM, 13, VML_ANCHOR_CENTER);
        }

        ui_present();
    }
};

// ════════════════════════════════════════════════════════════════════
// 主循环
// Main loop
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
    // Whether this round needs a redraw (see the note in the main loop)
    int msg[4];
    int score0;
    int score1;
    // 上一拍物理 / 游戏时钟的 `ui_tick()` 时刻 —— 用来"按真实流逝时间补拍"，
    // The `ui_tick()` moment of the last physics tick / game-clock tick — used to "catch up on elapsed real time",
    // 让物理与时钟**与帧率解耦**（见主循环 `VML_MSG_TIMER` 那两支的长注释）。
    // keeping the physics and the clock **decoupled from the frame rate** (see the long comments on the two `VML_MSG_TIMER` branches in the main loop).
    int lastPhysMs;
    int lastClockMs;

    // 把静态初值的 `gHour/gMinute`（开局 7:30）落进毫秒真源 —— **全程序只此一处换算**。
    // Push the statically initialised `gHour/gMinute` (starts at 7:30) into the millisecond source of truth — **the only conversion of its kind in the whole program**.
    // 两处各写一遍就是"改一处忘一处"，而症状是"钟面上 7:30、天却已经大亮"。
    // Writing it in two places means "change one and forget the other", with the symptom "the clock face says 7:30 but the sky is already bright".
    gMsOfDay = (gHour * 60 + gMinute) * 60000;

    g.Layout();
    // **全触摸**：不要屏幕手柄区（`VML_WIN_NO_GAMEPAD`）—— 手柄区连折叠条一起吃画布
    // **All touch**: no on-screen gamepad area (`VML_WIN_NO_GAMEPAD`) — the gamepad area together with its collapse bar eats canvas
    // 高度，这个游戏只用手指，没必要为它留一条；顺带锁竖屏（版面按竖屏排）。
    // height, and this game uses fingers only, so there is no reason to keep a strip for it; portrait is locked at the same time (the layout is arranged for portrait).
    gLang = ui_get_language();
    ui_win_open_ex(gLang == 0 ? "大猩猩扔香蕉 (C++)" : "Gorilla", g.sw, g.sh, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_keep_on(1);
    // ⚠ **必须在开窗之后**：图块是宿主那边的资源，没有场景就建不起来
    // ⚠ **It must come after the window opens**: blocks are a resource on the host side and cannot be created without a scene
    //   （`ui_create_block` 会返回 0，之后整局都只能用"退化的简版形状"兜底）。
    //   (`ui_create_block` returns 0, and the whole round then has to fall back to "degraded simplified shapes").
    // ⚠ 而且**只建这一次**：块表不随 `ui_clear` 清，每帧建一遍会在 128 帧后拿不到句柄。
    // ⚠ And **build them only this once**: the block table is not cleared by `ui_clear`, so building them every frame would fail to get handles after 128 frames.
    g.MakeBlocks();
    g.NewTurn();

    lastPhysMs = ui_tick();
    lastClockMs = lastPhysMs;

    tid = ui_timer_set(TICK_MS, 0);
    // 游戏时钟：**另一只定时器**，1 真实秒 = 1 游戏分钟（24 真实分钟走完一天）。
    // Game clock: **a second timer**, 1 real second = 1 game minute (24 real minutes make a full day).
    // 为什么不拿物理节拍那只数帧：玩家拖动滑条时主循环会一次抽干几十条 TOUCHMOVE，
    // Why not count frames on the physics timer: while the player drags a slider the main loop drains dozens of TOUCHMOVE messages at once,
    // 帧率完全取决于输入有多密 —— 数帧的话"滑得越勤、时钟跑得越快"。
    // so the frame rate depends entirely on how dense the input is — counting frames would make "the more you slide, the faster the clock runs".
    //
    // ⚠ 周期是 `CLOCK_MS`（50ms）而**不是 1000ms**：时钟的**语义**没变（1 秒照走
    // ⚠ The period is `CLOCK_MS` (50ms) and **not 1000ms**: the clock's **semantics** are unchanged (1 second still advances
    //   1 游戏分钟），变的只是**推进的粒度** —— 见 `gMsOfDay` 的说明。
    //   1 game minute); only the **granularity of the advance** changed — see the explanation of `gMsOfDay`.
    //   1000ms 一跳的话，云和日月的位置每秒才更新一次，看着就是一顿一顿的。
    //   With a 1000ms hop the clouds and the sun/moon would update only once a second, which looks jerky.
    cid = ui_timer_set(CLOCK_MS, 0);
    done = 0;
    needDraw = 1;            // 第一帧总得画
    // The first frame always has to be drawn

    while (done == 0 && ui_win_closed() == 0)
    {
        // 重绘由**变化**驱动，不由**节拍**驱动（本仓的一条老规矩）。
        // Redraws are driven by **change**, not by a **tick** (an old rule in this repo).
        // ⚠ 原来这里是无条件 `g.Draw()`：主循环每收到**一条**消息就画一帧，而
        // ⚠ This used to be an unconditional `g.Draw()`: the main loop drew a frame for **every** message it received, and
        //   时钟定时器 50ms 一条 ⇒ 凭空多出 20 帧/秒，内容一模一样。
        //   the clock timer sends one every 50ms ⇒ 20 extra frames per second out of nowhere, with identical content.
        //   实测帧率因此从 30 涨到 50 —— 多出来的全是空转（手机上就是白耗电）。
        //   The measured frame rate therefore rose from 30 to 50 — all of it spinning for nothing (pure battery drain on a phone).
        if (needDraw != 0)
        {
            g.Draw();
            needDraw = 0;
        }

        t = ui_wait_msg(0);

    handle_msg:
        // 默认**每一轮都重绘**，只有"纯时钟推进"那一支把它按下去。
        // By default **every round redraws**; only the "clock advance only" branch lowers it.
        // ⚠ 写反了（默认不画、白名单里才画）就会在将来加消息类型时**静默漏掉重绘** ——
        // ⚠ Write it the wrong way round (default no draw, draw only on a whitelist) and a future message type will **silently miss its redraw** —
        //   "界面不刷新"是最难查的一类症状，而这里的代价只是多画几帧。
        //   "the interface does not refresh" is one of the hardest symptoms to trace, while the cost here is only a few extra frames.
        needDraw = 1;

        if (t == VML_MSG_WINDOWCLOSE) { done = 1; }

        if (t == VML_MSG_TIMER)
        {
            // ⚠ **必须按定时器 id 分流**：现在有两只（物理节拍 33ms、游戏时钟 50ms），
            // ⚠ **It must branch by timer id**: there are two now (physics tick 33ms, game clock 50ms),
            //   不分的话两只都会走对方的逻辑 —— 时钟按 33ms 飞奔、物理按 50ms 一跳。
            //   and without branching each would run the other's logic — the clock racing at 33ms and the physics hopping at 50ms.
            //   消息 A 就是定时器 id。
            //   Message field A is the timer id.
            if (ui_msg_a() == cid)
            {
                // ⚠ 时钟**只推进时间、不请求重绘**：云和日月的位置是按 `gMsOfDay`
                // ⚠ The clock **only advances time and does not request a redraw**: the clouds' and sun/moon's positions are computed
                //   现算的，下一拍物理帧自然会用上最新的时间。
                //   on demand from `gMsOfDay`, so the next physics frame naturally uses the latest time.
                //   在这里补一帧的话，画出来的和上一帧没有区别（时间才走了 50ms，
                //   Drawing a frame here would produce something identical to the last one (only 50ms of time has passed
                //   云也才挪了 0.1px）—— 纯浪费。
                //   and the clouds have moved 0.1px) — pure waste.
                // ⚠ 游戏时钟**同样按真实经过时间补**（理由见下面物理那一支）：
                // ⚠ The game clock **likewise catches up by elapsed real time** (the reasoning is in the physics branch below):
                //   清空时钟类消息之后这一支会变稀，再固定推进 `CLOCK_MS` 的话，
                //   with clock-class messages dropped this branch becomes sparse, and advancing a fixed `CLOCK_MS` would
                //   游戏里的一天会被拉长（时钟挂在帧率上了）。上限同样 8 拍。
                //   stretch a game day out (the clock would hang off the frame rate). The cap is 8 ticks as well.
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
                // ⚠⚠ **Catch up by elapsed real time**, not "run one tick per message received" (v0.96.478).
                //
                //   定时器消息的语义是"**该推进了**"，它是**按时间**产生的（每 `TICK_MS` 一条）。
                //   A timer message means "**it is time to advance**", and it is produced **by time** (one every `TICK_MS`).
                //   而主循环一帧才消费一条 ⇒ 积压时物理每帧只跑一拍、**速度挂在帧率上**
                //   But the main loop consumes only one per frame ⇒ when messages back up, the physics runs one tick per frame and **the speed hangs off the frame rate**
                //   （实测积压到 2280 条时，物理每秒只走 20 拍而不是 30 拍 —— 游戏在偷偷变慢，
                //   (measured with a backlog of 2280 messages, the physics ran only 20 ticks per second instead of 30 — the game was quietly slowing down
                //   手感发黏）。上面那句 `ui_msg_drop(VML_MSG_KIND_TIMER)` 把积压清掉是对的，
                //   and felt sticky). The `ui_msg_drop(VML_MSG_KIND_TIMER)` above is right to clear the backlog,
                //   但**光清不补就是另一种坏**：物理会彻底变成"一帧一拍"。
                //   but **clearing without catching up is a different kind of broken**: the physics would become strictly "one tick per frame".
                //   ⇒ **清空 + 按 `ui_tick()` 的真实流逝时间补拍**，两者缺一不可。
                //   ⇒ **drop + catch up by `ui_tick()`'s elapsed real time**; neither is any use without the other.
                //
                //   ⚠ 上限 8 拍：真机上偶尔会有几百毫秒的卡顿（GC / 调度 / 切后台），
                //   ⚠ The cap is 8 ticks: on a real device there are occasional several-hundred-millisecond stalls (GC / scheduling / app switch),
                //   不设上限的话恢复那一帧会一口气跑几十拍 —— 香蕉瞬移、直接穿过楼房。
                //   and without a cap the recovering frame would run dozens of ticks at once — the banana teleporting straight through buildings.
                //   "宁可慢这一下，不要瞬移"。
                //   "Better to be slow for that one moment than to teleport".
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
            // ⚠⚠ **Drop clock-class messages as soon as a clock message is read** (v0.96.478, the user's suggestion).
            //
            //   定时器消息是"**到点了**"的通知 —— **中间那些已经过期了**，一条都没用：
            //   A timer message is a notification that "**the time has come**" — **the ones in between are already stale** and none of them is of any use:
            //   物理该走几拍、时钟该走多久，上面都按 `ui_tick()` 的真实流逝补齐了。
            //   how many physics ticks to run and how long the clock should advance have both been caught up from `ui_tick()`'s elapsed real time above.
            //   留着它们只有坏处：队列越积越长，触摸、键盘全排在后面（实测积压到 2280 条）。
            //   Keeping them can only hurt: the queue grows longer and longer and touch and keyboard all wait behind it (measured backlog of 2280 messages).
            //
            //   ⚠ **必须"先补拍、再清"** —— 反过来就是把"物理变慢"换成"物理跳帧"。
            //   ⚠ **The catch-up must come first and the clear second** — the other way round trades "the physics slows down" for "the physics skips frames".
            //   这一对（清空 + 按真实时间补）合起来才等价于"定时器每 TICK_MS 响一次"，
            //   Only together (drop + catch up by real time) do these equal "the timer fires every `TICK_MS`",
            //   而且**与帧率彻底解耦**：帧率掉到 10fps 物理照样按 30 拍/秒走。
            //   and they **decouple it completely from the frame rate**: drop to 10fps and the physics still runs 30 ticks per second.
            //   ⚠ 只清 `TIMER` 类：键盘 / 触摸是**离散语义**，丢一条就少一次事件
            //   ⚠ Drop only the `TIMER` class: keyboard / touch are **discrete semantics**, and dropping one loses one event
            //   （见 `ui_msg_drop` 的说明）。
            //   (see the explanation of `ui_msg_drop`).
            ui_msg_drop(VML_MSG_KIND_TIMER);
        }

        if (t == VML_MSG_KEYDOWN) { g.KeyDown(ui_msg_a()); }
        if (t == VML_MSG_KEYUP) { g.KeyUp(ui_msg_a()); }

        // 触摸 / 鼠标 —— **与 BASIC 版同一套**：拖条调值、点按钮发射。
        // Touch / mouse — **the same scheme as the BASIC version**: drag the bars to set values, tap the button to fire.
        // 移动事件（TOUCHMOVE）也走同一条，所以按住条一路拖就一直跟手。
        // Move events (TOUCHMOVE) go the same way too, so pressing a bar and dragging keeps tracking your finger.
        if (t == VML_MSG_TOUCHDOWN || t == VML_MSG_MOUSEDOWN) { g.HandlePoint(1); }
        if (t == VML_MSG_TOUCHMOVE || t == VML_MSG_MOUSEMOVE)
        {
            // ⚠⚠ **丢掉已经排队、但已经过期的移动事件**（v0.96.478）。
            // ⚠⚠ **Drop the move events that are already queued but stale** (v0.96.478).
            //
            //   移动是**"追最新位置"**的语义 —— 用户要的是手指**现在**在哪，中间那几百个
            //   Movement means **"follow the latest position"** — the user wants to know where the finger is **now**, and those hundreds of
            //   位置一个都没用。而主循环一次只取一条 ⇒ 拖动时产生的比消费的快，队列只涨不落：
            //   intermediate positions are worth nothing. But the main loop takes only one at a time ⇒ dragging produces faster than it is consumed, and the queue only grows:
            //   实测连续拖滑条 6 轮，队列 285 → 596 → 1050 → 1481 → 2010 → **2280** 条，
            //   measured over 6 consecutive slider drags, the queue went 285 → 596 → 1050 → 1481 → 2010 → **2280** messages,
            //   **完全不回落**，而同一时间 fps 全程 19~21。
            //   **never falling back**, while the fps stayed at 19..21 throughout.
            //   于是出现用户报的那种「**背景绘图不卡、但触摸要等一下才反应**」——
            //   That produces the thing the user reported: "**the background drawing is not stuttering, but touch takes a moment to respond**" —
            //   他的触摸事件排在那两千多条移动事件后面。
            //   their touch events were queued behind those two thousand-plus move events.
            //
            //   ⚠ 用"丢一类"而**不是** `ui_msg_clear`：清空会把同时排着的键盘、定时器
            //   ⚠ Use "drop one class", **not** `ui_msg_clear`: clearing would also throw away the keyboard and timer messages
            //   一起扔掉 —— 那两样是**离散语义**，少一条就是"按键丢了/物理卡了一拍"。
            //   queued at the same time — those two are **discrete semantics**, and losing one means "a keypress was lost / the physics missed a tick".
            //   ⚠ 丢掉的是**已经排队**的，当前这条（刚取出来的）照常处理，所以
            //   ⚠ What is dropped is what is **already queued**; the current one (just taken out) is processed as usual, so
            //   "跟手"这件事一点不受影响：手指最后停在哪，程序拿到的就是哪。
            //   "tracking the finger" is not affected at all: wherever the finger finally stops is what the program gets.
            ui_msg_drop(VML_MSG_KIND_TOUCH);
            g.HandlePoint(0);
        }

        if (g.over == 2) { done = 1; }      // 结束画面被点了一下
        // The end screen was tapped once

        // ⚠⚠ **把已经排队的消息一次抽干**（v0.96.478，治"越玩越卡"的**主力**）。
        // ⚠⚠ **Drain the queued messages in one go** (v0.96.478, the **main** cure for "the longer you play the laggier it gets").
        //
        //   上面 `ui_wait_msg(0)` **一次只取一条**，而消息的**产生速度**是：
        //   `ui_wait_msg(0)` above takes **only one at a time**, while messages are **produced** at:
        //     · 定时器：物理 33ms + 时钟 50ms ⇒ **每秒 ~50 条**
        //     · timers: physics 33ms + clock 50ms ⇒ **~50 per second**
        //     · 触摸拖动：手指一秒几百条 TOUCHMOVE
        //     · touch dragging: several hundred TOUCHMOVE messages per second from one finger
        //   而主循环**一帧才消费一条**（画一帧 ~50ms）⇒ **每秒只消费 ~20 条**
        //   And the main loop **consumes one per frame** (~50ms to draw a frame) ⇒ **only ~20 per second**
        //   ⇒ **每秒净积压 30 条以上**。真机实测（连续拖滑条 6 轮）：
        //   ⇒ **a net backlog of over 30 per second**. Measured on device (6 consecutive slider drags):
        //   队列 285 → 596 → 1050 → 1481 → 2010 → **2280**，**完全不回落**。
        //   queue 285 → 596 → 1050 → 1481 → 2010 → **2280**, **never falling back**.
        //
        //   后果有两层，**第二层才是要命的**：
        //   There are two consequences, and **the second is the deadly one**:
        //     ① 触摸事件排在那几千条后面 ⇒ 「**触摸要等一下才反应**」；
        //     ① touch events queue behind those thousands ⇒ "**touch takes a moment to respond**";
        //     ② **定时器消息也被积压** ⇒ `g.Tick()` 每秒只跑 20 次而不是 30 次
        //     ② **timer messages back up too** ⇒ `g.Tick()` runs only 20 times a second instead of 30
        //        ⇒ **物理在偷偷变慢**（香蕉飞得比设计慢、手感发黏）。
        //        ⇒ **the physics is quietly slowing down** (the banana flies slower than designed and it feels sticky).
        //   ⚠ 所以这里**不能**改用"丢定时器"来省事 —— 丢一条就少一拍物理，
        //   ⚠ So "drop the timers" is **not** an option here to save effort — dropping one loses one physics tick,
        //     那是把"变慢"换成"跳帧"。抽干才是对的：让该跑的每一拍都跑到。
        //     which trades "slower" for "skipping frames". Draining is the right answer: every tick that should run does run.
        //
        //   ⚠ 判据用 `ui_msg_count()` 而**不是**再调一次 `ui_wait_msg(0)` ——
        //   ⚠ The test uses `ui_msg_count()` and **not** another `ui_wait_msg(0)` —
        //   后者没消息时会**阻塞**，把主循环变成空转（手机上是白耗电）；
        //   the latter **blocks** when there are no messages, turning the main loop into spinning (pure battery drain on a phone);
        //   也**不能**把 `ui_wait_msg(0)` 直接换成 `ui_poll_msg()`：那会让
        //   nor may `ui_wait_msg(0)` simply be replaced with `ui_poll_msg()`: that would remove
        //   "没消息时让出 CPU"消失，主循环变成 100% 占用的忙等。
        //   the "yield the CPU when there is nothing to do" behaviour and turn the main loop into a 100%-busy wait.
        //   ⚠ 用 `goto` 而不是把这一大段抽成函数：处理体要用 `t/done/needDraw` 这些
        //   ⚠ A `goto` is used rather than extracting this block into a function: the handler needs main-loop locals like `t/done/needDraw`,
        //   主循环局部量，抽函数得把它们全改成全局 —— 那才是真正的隐患来源。
        //   and extracting it would mean turning them all into globals — which is where the real risk would come from.
        if (done == 0 && ui_msg_count() > 0)
        {
            t = ui_poll_msg();
            goto handle_msg;
        }
    }

    if (tid != 0) { ui_timer_kill(tid); }
    if (cid != 0) { ui_timer_kill(cid); }

    // ⚠ **退出前必须静音**：胜负音还在响的时候玩家就点了退出，那一声会一直响下去
    // ⚠ **The sound must be silenced before exiting**: if the player quits while a win/lose tone is still sounding, that tone keeps sounding
    //   （进程没了才停）。桌面上表现为"窗口关了还有声音"，手机上更明显 ——
    //   (it only stops when the process dies). On the desktop that shows up as "the window is closed but the sound continues", and on a phone it is even more obvious —
    //   切回桌面还在响。`ui_sfx_panic` 是"全停"（它内部还会 `ui_tone_panic`，
    //   it is still sounding after switching back to the home screen. `ui_sfx_panic` is the "stop everything" call (internally it also calls `ui_tone_panic`,
    //   把老式 `ui_beep` 那条声道一起掐掉），所以音效改走 beep 之后依然该调它。
    //   cutting the old `ui_beep` channel as well), so it should still be called now that the sound effects go through beep.
    ui_sfx_panic();

    // 比分落盘（下次开局问不出来，但先存着 —— 与 BASIC 版同一套键）
    // Persist the scores (they cannot be read back at the next start, but store them anyway — the same key scheme as the BASIC version)
    score0 = (*AP[0]).score;
    score1 = (*AP[1]).score;
    ui_store_set("gorilla.hpp.score0", numstr(score0));
    ui_store_set("gorilla.hpp.score1", numstr(score1));

    ui_keep_on(0);
    return 0;
}
