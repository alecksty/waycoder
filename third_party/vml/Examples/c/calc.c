/* calc.c —— 《算盘》计算器（C 版），跑在手机端 VML 上
 * calc.c -- the "Abacus" calculator (C version), running on VML on mobile.
 *
 * 编译运行（手机 App 的 vml 工具）：
 * Build and run (with the mobile app's `vml` tool):
 *     vml run examples/c/calc.c
 *
 * ## 操作：**只有手指**，不要手柄
 * ## Controls: **fingers only**, no gamepad.
 *
 * 点按键就行。开窗时声明 `VML_WIN_NO_GAMEPAD`（整块手柄区不显示）+ `VML_WIN_PORTRAIT`
 * Just tap the keys. The window declares `VML_WIN_NO_GAMEPAD` (the whole gamepad area is hidden) + `VML_WIN_PORTRAIT`
 * 锁竖屏 —— 屏幕上的东西每一块都要有用。
 * to lock portrait -- everything on screen must earn its place.
 *
 * ## 界面怎么"好看"
 * ## How the interface looks good
 *
 * - **背景**：径向渐变（中心亮、四周沉），一块**玻璃面板**放显示屏
 * - **Background**: a radial gradient (bright in the middle, dark at the edges), with a **glass panel** behind the display
 * - **按键**：圆角矩形 + 渐变（数字键浅、功能键橙、等号蓝），**按下去有高亮反馈**
 * - **Keys**: rounded rects + gradients (digit keys light, function keys orange, equals blue), with **highlight feedback while pressed**
 * - **显示屏**：右对齐、算式在上（小字暗色）、结果在下（大字亮色）
 * - **Display**: right aligned, the expression on top (small and dim) and the result below (large and bright)
 * - **按下反馈**：手指按住期间那格换成高亮色并微微内缩 —— 没有动画库，全靠"换色 + 改尺寸"
 * - **Press feedback**: while a finger is held down that cell switches to a highlight color and shrinks a little -- no animation library, just "swap color + tweak size"
 *
 * ## 数字怎么算的（**没有浮点格式化**）
 * ## How the numbers are computed (**no floating point formatting**)
 *
 * 用**千分之一定点**：内部一律用 `值 × 100` 的整数存，显示时再插小数点。
 * Uses **1/1000 fixed point**: internally every value is stored as a `value x 100` integer, and the decimal point is inserted when displaying.
 * 这样加减乘除全是整数运算，不用 `sprintf`、不用 `%f`，结果精确到分。
 * That way add/subtract/multiply/divide are pure integer math, with no `sprintf` and no `%f`, and results are exact to the cent.
 * ⚠ 除法是**先放大再除**（`a * 100 / b`），顺序反了会把小数位截没。
 * ⚠ Division is **scale up first, then divide** (`a * 100 / b`); reversing the order truncates the decimals away.
 *
 * ## 三条 C 前端硬约束（与 plane.c 相同）
 * ## Three hard constraints of the C frontend (same as plane.c)
 *
 * 1. ⚠ `${}` 只认局部变量 ⇒ syscall 一律走包装函数
 * 1. ⚠ `${}` only accepts local variables => syscalls always go through wrapper functions
 * 2. ⚠ 复合字面量 `(int[]){…}` 不支持也不报错 ⇒ 顶点用**具名全局数组**
 * 2. ⚠ Compound literals `(int[]){...}` are unsupported and raise no error => vertices must use **named global arrays**
 * 3. ⚠ `#define` 不支持反斜杠续行
 * 3. ⚠ `#define` does not support backslash line continuation
 */

#include <waycoder_ui.h>

/* ── 配色 ───────────────────────────────────────────────── */
/* ── Colors ─────────────────────────────────────────────── */

#define BG_A       0xFF1B2233
#define BG_B       0xFF0B0F18
#define PANEL      0x33FFFFFF
#define PANEL_EDGE 0x55FFFFFF
#define DISP_EXPR  0x99C7D6E8
#define DISP_NUM   0xFFFFFFFF
#define DISP_ERR   0xFFFF7B6B

/* 四档按键色，按**功能分区**选不同色相 —— 色相拉开才分得清，
   Four key color tiers, chosen by **functional zone** -- hues must be pulled apart to be told apart;
   同一色系只差明度的话（比如两个蓝灰）在手机上几乎看不出区别：
   if it were one hue differing only in brightness (say two blue-grays) they would be almost indistinguishable on a phone:
     数字键  深蓝灰（安静，占大多数）
     digit keys  dark blue-gray (quiet, the majority)
     运算符  暗金  （"这是要算的"）
     operators  dark gold ("this one computes")
     功能键  砖红  （C / ± / % —— 会改变当前输入，给一点警示感）
     function keys  brick red (C / ± / % -- they change the current input, so give a touch of warning)
     等号    亮蓝  （唯一的"执行"键，最跳）
     equals  bright blue (the only "execute" key, the most eye-catching)
 */
#define KEY_NUM    0xFF2A3346
#define KEY_NUM_HI 0xFF3C4A63
#define KEY_OP     0xFF7A5A28
#define KEY_OP_HI  0xFF9E7838
#define KEY_FN     0xFF9E4630
#define KEY_FN_HI  0xFFC06648
#define KEY_EQ     0xFF2E7DD1
#define KEY_EQ_HI  0xFF4C9BEF
#define KEY_TXT    0xFFF2F6FA
#define KEY_EDGE   0x22FFFFFF

#define MAXDIG 12

/* ── 状态 ───────────────────────────────────────────────── */
/* ── State ──────────────────────────────────────────────── */

int W, H;
int btop, bh, bw, bgap;          /* 按键区起点 / 键高 / 键宽 / 间隙（两个方向同一个）*/
// Key area top / key height / key width / gap (one and the same in both directions)
int bcols, brows;

int acc;                         /* 累加器（×100 定点） */
// Accumulator (x100 fixed point)
int cur;                         /* 正在输入的数（×100） */
// The number currently being typed (x100)
int curDigits;                   /* 已输入位数 */
// How many digits have been typed
int hasDot;                      /* 小数点后已几位 */
// How many digits follow the decimal point
int pendingOp;                   /* 待执行的运算：0 无 / 1 + / 2 - / 3 × / 4 ÷ */
// The pending operation: 0 none / 1 + / 2 - / 3 x / 4 /
int entering;                    /* 是否正在输入新数 */
// Whether a new number is being typed
int err;                         /* 出错（除以 0） */
// Error state (division by 0)

char expr[64];                   /* 上方那行算式 */
// The expression line shown on top
int  exprLen;

int pressIdx;                    /* 当前按住的键（-1 无） */
// The key currently held down (-1 = none)
int flashT;                      /* 按下高亮还能亮几拍 */
// How many more ticks the press highlight stays lit

int g_lang;                      /* 界面语言：开局查一次（ui_get_language 是 syscall，别每帧调） */
// UI language: queried once at startup (ui_get_language is a syscall, do not call it every frame)

/* 键盘：4 列 × 5 行。
 * Keyboard: 4 columns x 5 rows.
 *
 * ⚠ **键面用整数码 + 一个 label_of()，不要 `char* keys[20] = {"C", …}`** ——
 * ⚠ **Key faces use integer codes + a single label_of(), not `char* keys[20] = {"C", ...}`** --
 *    全局的**指针数组**在这条前端上会读出垃圾（实测：运行到取键面就
 *    a global **array of pointers** reads back garbage on this frontend (measured: reaching the key-face lookup it dies with
 *    `内存错误 地址=FFFFFFF1`）。整数数组是稳的（前端刚修过全局 char/int 数组的元素类型）。
 *    `memory error addr=FFFFFFF1`). Integer arrays are stable (the frontend just fixed the element type of global char/int arrays).
 *
 * 码：0-9 = 数字，10 +，11 -，12 *，13 /，14 =，15 C，16 +/-，17 %，18 .，19 空位
 * Codes: 0-9 = digits, 10 +, 11 -, 12 *, 13 /, 14 =, 15 C, 16 +/-, 17 %, 18 ., 19 empty slot
 */
int keyCode[20] = {
    15, 16, 17, 13,
     7,  8,  9, 12,
     4,  5,  6, 11,
     1,  2,  3, 10,
     0, 19, 18, 14
};
/* 0 数字 / 1 功能(C ± %) / 2 等号 / 3 运算符(+ - x /)
 * 0 digit / 1 function (C ± %) / 2 equals / 3 operator (+ - x /)
 *
 * ⚠ 用**函数**给，不用数组查表：`keyFn[i]` 这种「全局 int 数组的**元素比较**」在这条
 * ⚠ Provide it as a **function**, not an array lookup: `keyFn[i]`, i.e. comparing an **element of a global int array**, on this
 *    前端上读不出正确的值（数据段里明明是 1/3/2，程序却一律当 0，于是所有键都画成
 *    frontend does not read back the right value (the data segment clearly holds 1/3/2, yet the program treats it all as 0, so every key is drawn in
 *    数字键的颜色 —— 而同一张表用 `label_of(keyCode[i])` 就是好的，见文件头那条）。
 *    the digit-key color -- while the same table read through `label_of(keyCode[i])` is fine, see the note at the top of the file).
 *    改成 if 链之后与 `label_of` 完全同构，行为一致、可预期。
 *    Rewritten as an if chain it is exactly isomorphic to `label_of`, with consistent and predictable behavior.
 */
int fn_of(int i)
{
    if (i == 0 || i == 1 || i == 2) return 1;                 /* C  +/-  %  */
    if (i == 3 || i == 7 || i == 11 || i == 15) return 3;     /* /  x  -  +  */
    if (i == 19) return 2;                                    /* =          */
    return 0;
}

char* label_of(int c)
{
    if (c == 0) return "0";
    if (c == 1) return "1";
    if (c == 2) return "2";
    if (c == 3) return "3";
    if (c == 4) return "4";
    if (c == 5) return "5";
    if (c == 6) return "6";
    if (c == 7) return "7";
    if (c == 8) return "8";
    if (c == 9) return "9";
    if (c == 10) return "+";
    if (c == 11) return "-";
    if (c == 12) return "x";
    if (c == 13) return "/";
    if (c == 14) return "=";
    if (c == 15) return "C";
    if (c == 16) return "+/-";
    if (c == 17) return "%";
    if (c == 18) return ".";
    return "";
}

int g_pts[24];

/* ── 定点数与格式化 ─────────────────────────────────────── */
/* ── Fixed point and formatting ─────────────────────────── */

char g_fmt[24];

/* v 是 ×100 的定点数 → 写进全局 g_fmt："12.34" / "12" / "-0.05"
 * v is a x100 fixed point value -> written into the global g_fmt: "12.34" / "12" / "-0.05"
 *
 * ⚠ 直接写全局量、**不接收 out 指针**：这条前端的「指针形参写回」已知不可靠
 * ⚠ Writes a global directly and **takes no out pointer**: writing back through a pointer parameter is known to be unreliable on
 *    （见文件头第 3 条：`void f(int* x){ *x=…; }` 实测会生成坏地址）。写全局是
 *    this frontend (see item 3 at the top of the file: `void f(int* x){ *x=...; }` was measured to generate a bad address). Writing globals is
 *    **规避**这条风险 —— 与 `keyRect` 改成"全局量返回"同一个理由，全文件一种写法。
 *    how that risk is **side-stepped** -- the same reason `keyRect` was changed to "return through globals", one style throughout the file.
 */
void fmt(int v)
{
    int i = 0, j = 0, neg = 0, ip, fp;
    char t[16];

    if (v < 0) { neg = 1; v = -v; }
    ip = v / 100;
    fp = v % 100;

    if (ip == 0) t[j++] = '0';
    while (ip > 0 && j < 14) { t[j++] = (char)('0' + ip % 10); ip = ip / 10; }
    if (neg) g_fmt[i++] = '-';
    while (j > 0) g_fmt[i++] = t[--j];

    if (fp != 0) {
        g_fmt[i++] = '.';
        g_fmt[i++] = (char)('0' + (fp / 10) % 10);
        if (fp % 10 != 0) g_fmt[i++] = (char)('0' + fp % 10);
    }
    g_fmt[i] = 0;
}

int atoi_(char* b)
{
    int i = 0, v = 0;
    while (b[i] >= '0' && b[i] <= '9') { v = v * 10 + (b[i] - '0'); i = i + 1; }
    return v;
}

/* ── 布局 ───────────────────────────────────────────────── */
/* ── Layout ─────────────────────────────────────────────── */

int keyX(int c) { return 14 + c * (bw + bgap); }
int keyW(void)  { return bw; }
int keyY(int r) { return btop + r * (bh + bgap); }

/* 按键在屏幕上的矩形。结果放**全局**量 ——
   The on-screen rectangle of a key. The result goes into **globals** --
   ⚠ 不用「指针形参写回」（`void f(int* x) { *x = …; }`）：实测这条前端在那上面会崩
   ⚠ No "write back through a pointer parameter" (`void f(int* x) { *x = ...; }`): measured, this frontend crashes on that
   （`内存错误 地址=FFFFFFF1`，而同样的逻辑改成全局量就正常）。
   (`memory error addr=FFFFFFF1`), while the same logic written with globals works fine.
 */
int rx, ry, rw, rh;

void keyRect(int i)
{
    int r = i / 4, c = i % 4;
    rx = keyX(c);
    ry = keyY(r);
    rw = keyW();
    rh = bh;
    if (i == 16) rw = keyW() * 2 + bgap;      /* 0 跨两格 */
    /* The 0 key spans two cells. */
}

/* ── 运算 ───────────────────────────────────────────────── */
/* ── Arithmetic ─────────────────────────────────────────── */

void push_expr_op(int op)
{
    if (exprLen < 58) {
        expr[exprLen++] = ' ';
        if (op == 1) expr[exprLen++] = '+';
        else if (op == 2) expr[exprLen++] = '-';
        else if (op == 3) expr[exprLen++] = '*';
        else expr[exprLen++] = '/';
        expr[exprLen++] = ' ';
        expr[exprLen] = 0;
    }
}

int apply(int a, int b, int op)
{
    if (op == 1) return a + b;
    if (op == 2) return a - b;
    if (op == 3) return (a / 100) * (b / 100) * 100 + ((a % 100) * (b / 100)) + ((b % 100) * (a / 100));
    if (op == 4) {
        if (b == 0) { err = 1; return 0; }
        return a * 100 / b;      /* ⚠ 先放大再除，顺序反了小数位就没了 */
        /* ⚠ Scale up first and only then divide; reverse the order and the decimals are gone. */
    }
    return b;
}

/* 按下一个键 */
/* Handle one key press. */
void on_key(int i)
{
    int c = keyCode[i];

    if (err && c != 15) return;                       /* 出错后只认 C */
    /* After an error, only C is accepted. */

    if (c >= 0 && c <= 9) {                           /* 数字 */
        /* A digit key. */
        if (curDigits < MAXDIG) {
            if (!entering) { cur = 0; curDigits = 0; hasDot = 0; entering = 1; }
            cur = cur * 10 + c * 100;
            curDigits = curDigits + 1;
            sfx_digit();
        }
        return;
    }

    if (c == 18) {                                    /* . */
        if (!entering) { cur = 0; curDigits = 0; entering = 1; hasDot = 0; }
        if (hasDot == 0) hasDot = 1;
        sfx_digit();
        return;
    }

    if (c == 16) { cur = -cur; sfx_sign(); return; }        /* +/- */
    if (c == 17) { cur = cur / 100; sfx_sign(); return; }   /* % */

    if (c == 15) {                                    /* C */
        acc = 0; cur = 0; curDigits = 0; hasDot = 0;
        pendingOp = 0; entering = 0; err = 0;
        exprLen = 0; expr[0] = 0;
        sfx_clear();
        return;
    }

    /* 四则运算与等号 */
    /* The four arithmetic operators and equals. */
    if (c >= 10 && c <= 14) {
        int op = c - 9;                               /* 1 + / 2 - / 3 x / 4 / / 5 = */

        if (op == 5) {
            if (pendingOp != 0) acc = apply(acc, cur, pendingOp);
            else acc = cur;
            cur = acc; entering = 0; curDigits = 0; hasDot = 0;
            pendingOp = 0;
            exprLen = 0; expr[0] = 0;
            if (err) { sfx_err(); } else { sfx_ok(); }
            return;
        }

        if (pendingOp != 0 && entering) acc = apply(acc, cur, pendingOp);
        else if (pendingOp == 0) acc = cur;

        cur = 0; curDigits = 0; hasDot = 0; entering = 0;
        pendingOp = op;
        exprLen = 0; expr[0] = 0;
        fmt(acc);
        {
            int t = 0;
            while (g_fmt[t] != 0 && exprLen < 58) expr[exprLen++] = g_fmt[t++];
            expr[exprLen] = 0;
        }
        push_expr_op(op);
        sfx_op();
    }
}

/* ── 音效：**用 ui_beep 单音**（v0.96.509 统一换回来） ──────────
 * ── Sound: **single-tone ui_beep** (switched back in v0.96.509) ─
 *
 * ⚠⚠ 这些音一度走共享库的音序器（`ui_sfx_add`），**真机上破音**，换回来了。
 * ⚠⚠ These tones once went through the shared library's sequencer (`ui_sfx_add`) and **clipped on the real device**, so they were switched back.
 *   破音的是**这里配的音**：等号成功/出错是 2–3 个声部叠着响，多声部一叠加就顶到
 *   What clipped was **the tones configured right here**: equals on success/failure sounded 2-3 voices stacked, and stacking voices pushed it into
 *   削波。`ui_beep` 是**单通道**的（后一个音掐掉前一个）⇒ 一个事件永远只有一个音
 *   clipping. `ui_beep` is **single channel** (each new tone cuts off the previous one) => one event can only ever have one tone
 *   在响，**结构上不可能削波**。代价是没有音色。
 *   sounding, so clipping is **structurally impossible**. The price is losing timbre.
 *
 * 计算器的音效是**触感**，不是气氛 —— 所以一律**极短**（按键那三个只有 33ms）。
 * A calculator's sound effects are **haptics**, not atmosphere -- so they are all **extremely short** (the three key tones last only 33ms).
 * 按一次键响 200ms 的"叮"，按十下就是噪音。
 * A 200ms "ding" on every key press turns into noise after ten presses.
 * ⚠ 频率沿用原来那版的音区（数字最高、出错最低最长），时长封顶 320ms。
 * ⚠ Frequencies keep the original version's register (digits highest, error lowest and longest), with durations capped at 320ms.
 */

void sfx_digit(void) { ui_beep(1319, 33); }
void sfx_sign(void)  { ui_beep(784, 33); }
void sfx_op(void)    { ui_beep(659, 33); }

/* 清零：一声干净的中音 —— 一听就知道"清掉了"。 */
/* Clear: one clean mid tone -- you can hear at once that it was cleared. */
void sfx_clear(void) { ui_beep(523, 132); }

/* 等号：对是**高而短**、错是**低而长** —— 两个方向，不会听错。 */
/* Equals: correct is **high and short**, wrong is **low and long** -- two opposite shapes, impossible to mishear. */
void sfx_ok(void) { ui_beep(1047, 165); }
void sfx_err(void) { ui_beep(262, 320); }

/* ── 渲染 ───────────────────────────────────────────────── */
/* ── Rendering ──────────────────────────────────────────── */

void draw_key(int i)
{
    int x, y, w, h, col, tcol, dy, f;
    int held = (pressIdx == i);

    /* 空位键（码 19）：整个不画。
       Empty slot keys (code 19) are not drawn at all.
       ⚠ 它本来就只是列表里的占位符 —— `0` 键（i=16）跨两格、右半边正好压在它上面。
       ⚠ It is only a placeholder in the list anyway -- the `0` key (i=16) spans two cells and its right half sits right on top of it.
       但绘制顺序是 i=0..19，i=17 在 i=16 **之后**画 ⇒ 照常画的话它会盖住跨格键的右半，
       But the draw order is i=0..19 and i=17 is drawn **after** i=16 => drawn as usual it would cover the right half of the two-cell key,
       屏幕上就多出一个空按钮（用户报的"下方有多的空按钮"就是这个）。
       and one extra empty button shows up on screen (this is exactly the "extra empty button below" the user reported).
       所以必须**在画之前**就返回，而不是画完矩形再跳过文字。
       So we must return **before drawing**, not draw the rectangle and then skip the text.
    */
    if (keyCode[i] == 19) return;

    /* ⚠ keyRect 的结果经**全局量** rx/ry/rw/rh 返回（原因见其定义处），这里立刻拷进
       ⚠ keyRect returns its result through the **globals** rx/ry/rw/rh (see its definition for why), so copy it into locals right here
       局部 —— 免得函数体后半段再有什么东西动了那四个全局量。
       -- in case something later in the function body moves those four globals.

       这里原先写的是 `keyRect(i, &x, &y, &w, &h)`：那是「指针形参写回」版本的调用点，
       This used to be written as `keyRect(i, &x, &y, &w, &h)`, the call site of the "write back through pointer params" version,
       而 keyRect 后来改成了全局量返回、**调用点忘了跟着改**。C 前端不检查实参个数
       but keyRect was later changed to return through globals and **the call site was forgotten**. The C frontend does not check argument counts
       （多余的实参求值后静默丢弃），于是 x/y/w/h 一路保持未初始化 —— 每个键都按垃圾
       (extra arguments are evaluated and silently dropped), so x/y/w/h stayed uninitialized all along -- every key was drawn at garbage
       坐标/尺寸去画，屏幕上按键区就是一片空白，而编译期一声不响。
       coordinates/sizes, the key area on screen was simply blank, and the compiler said nothing.
    */
    keyRect(i);
    x = rx; y = ry; w = rw; h = rh;

    /* ⚠ 别在赋值右边写嵌套三元（同上面那条：这条前端会生成坏地址）*/
    /* ⚠ Do not put nested ternaries on the right of an assignment (same reason as above: this frontend generates a bad address). */
    f = fn_of(i);                     /* ⚠ 用函数、不用 keyFn[i] 查表（原因见 fn_of 处） */
    /* ⚠ Use the function, not a keyFn[i] table lookup (see fn_of for why). */

    col = KEY_NUM; dy = 0;
    if (f == 1) col = KEY_FN;
    if (f == 2) col = KEY_EQ;
    if (f == 3) col = KEY_OP;
    if (held) {
        col = KEY_NUM_HI;
        if (f == 1) col = KEY_FN_HI;
        if (f == 2) col = KEY_EQ_HI;
        if (f == 3) col = KEY_OP_HI;
        dy = 2;                       /* 按下去：整格下沉 2px（比缩放省事，效果一样清楚） */
        /* Pressed: the whole cell sinks 2px (simpler than scaling, and just as clear). */
    }

    /* 底部一道暗边，做出"立体键"的感觉 */
    /* A dark edge along the bottom, for a "3D key" feel. */
    ui_rect(x, y + 3, w, h, 0x55000000, 1, 0, 14);
    ui_rect(x, y + dy, w, h, col, 1, 0, 14);
    ui_rect(x, y + dy, w, h, KEY_EDGE, 0, 2, 14);      /* 描边 */
    /* Outline stroke. */

    tcol = 0xFFFFFFFF;
    if (f == 0) tcol = KEY_TXT;

    if (keyCode[i] == 13) {                            /* 除号：自己画，省得依赖字体有没有那个字形 */
        /* Division sign: drawn by hand, so we do not depend on the font having that glyph. */
        ui_line(x + w / 2 - 9, y + h / 2 + dy, x + w / 2 + 9, y + h / 2 + dy, tcol, 3);
        ui_circle(x + w / 2 - 9, y + h / 2 - 7 + dy, 2, tcol, 1, 0);
        ui_circle(x + w / 2 + 9, y + h / 2 - 7 + dy, 2, tcol, 1, 0);
        ui_circle(x + w / 2, y + h / 2 - 12 + dy, 2, tcol, 1, 0);
        ui_circle(x + w / 2, y + h / 2 + 10 + dy, 2, tcol, 1, 0);
        return;
    }

    /* 按键标签：**用 MIDDLE 档居中**，不再靠"再减 16% 高度"手工凑。
       Key label: **centered with the MIDDLE anchor mode**, no more hand-tuning by "subtract 16% of the height".
       从前 `y` 被当基线（手机端的实际语义），要把字摆到按键正中就只能手动往上抬一截 ——
       `y` used to be treated as the baseline (the actual semantics on mobile), so putting the text in the middle of a key meant lifting it by hand --
       而那个数字是**量出来的**、换个字号或按键高度就不准了。现在竖对齐有档位（#586），
       and that number was **measured**, so a different font size or key height made it wrong. Now vertical alignment has anchor modes (#586),
       让宿主按字体度量去算，程序只给"按键中心"。
       letting the host compute from font metrics while the program only gives the "key center".
    */
    ui_set_font(h * 34 / 100, VML_FONT_BOLD, tcol, VML_ANCHOR_CENTER);
    ui_set_valign(VML_VANCHOR_MIDDLE);
    ui_text_cur(x + w / 2, y + h / 2 + dy, label_of(keyCode[i]));
}

void draw_display(void)
{
    int px = 14, py = 16, pw = W - 28, ph = btop - 34;

    /* 玻璃面板 */
    /* The glass panel. */
    ui_gradient("glass", 0, PANEL, 0x11FFFFFF, 0, 0, 0, 1000);
    ui_rect_grad(px, py, pw, ph, "glass", 18);
    ui_rect(px, py, pw, ph, PANEL_EDGE, 0, 2, 18);

    /* ⚠ 显示区这两行的坐标是照**基线档（老行为）**量出来的 ⇒ 显式声明 BASE，
       ⚠ The coordinates of these two display lines were measured against the **baseline mode (the old behavior)** => declare BASE explicitly,
       不靠"上一个 draw 设过什么"残留（按键那边设的是 MIDDLE）。
       instead of relying on leftovers from "whatever the last draw set" (the key side sets MIDDLE).
    */
    ui_set_valign(VML_VANCHOR_BASE);

    /* 算式行（小、暗、右对齐） */
    /* The expression line (small, dim, right aligned). */
    if (exprLen > 0) {
        ui_set_font(ph * 15 / 100, 0, DISP_EXPR, VML_ANCHOR_RIGHT);
        ui_text_cur(px + pw - 18, py + ph * 14 / 100, expr);
    }

    /* 结果行（大、亮、右对齐）。
       The result line (large, bright, right aligned).
       ⚠ 这里原本写的是「参数位置的三元表达式」—— 这条前端在那上面会生成坏掉的地址
       ⚠ This used to be a "ternary expression in an argument position" -- this frontend generates a broken address for that
       （实测 `MOVE @1, R0 地址=FFFFFFF1`）。改成先算进局部变量再传，稳妥得多。
       (measured: `MOVE @1, R0 addr=FFFFFFF1`). Computing into a local variable first and then passing it is far safer.
    */
    {
        int show = cur;
        int col = DISP_NUM;
        char* txt = g_fmt;

        if (err) {
            col = DISP_ERR;
            if (g_lang == 0) txt = "错误";
            else             txt = "Error";
            show = 0;
        }
        else if (!entering && pendingOp != 0) show = acc;

        fmt(show);
        ui_set_font(ph * 34 / 100, VML_FONT_BOLD, col, VML_ANCHOR_RIGHT);
        ui_text_cur(px + pw - 18, py + ph * 52 / 100, txt);
    }

    /* ⚠ 临时诊断：按住某键时在面板左下角画个洋红方块 ——
       ⚠ Temporary diagnostic: while a key is held, draw a magenta square at the lower left of the panel --
       用来区分「TOUCHDOWN 根本没收到」与「收到了但高亮没生效」。验完删。
       to tell "TOUCHDOWN never arrived" apart from "it arrived but the highlight did not take effect". Delete once verified.
    */
    if (pressIdx >= 0 || flashT > 0) ui_rect(px + 8, py + ph - 34, 18, 18, 0xFFFF00FF, 1, 0, 3);
    {
    }
}

void draw(void)
{
    int i;

    /* ⚠ 先清场：绘制都往宿主的**同一张图元表**里追加，只有 ui_clear 会清空它。
       ⚠ Clear first: every draw appends to the host's **single figure table**, and only ui_clear empties it.
       这里每点一次按键就整屏重画一遍，不清的话玩久了同样会把宿主撑爆（见 plane.c 的说明）。
       Every key press redraws the whole screen here, so without clearing we would blow up the host after a while (see the note in plane.c).
    */
    ui_clear(BG_B);

    /* 径向渐变底：中心亮、四周沉 */
    /* Radial gradient background: bright in the middle, dark at the edges. */
    ui_gradient("bg", 1, BG_A, BG_B, 500, 380, 780, 0);
    ui_rect_grad(0, 0, W, H, "bg", 0);

    draw_display();
    for (i = 0; i < 20; i++) draw_key(i);
}

/* ── 入口 ───────────────────────────────────────────────── */
/* ── Entry point ────────────────────────────────────────── */

int main(void)
{
    int i, t;
    /* ⚠ `m` 必须**单独一行**声明，别挤进 `int i, m[4], t;`。
       ⚠ `m` must be declared on a **line of its own**, not squeezed into `int i, m[4], t;`.
       这条前端对「标量与数组写在同一条声明里」处理不了：局部数组分不到自己的槽位，
       This frontend cannot handle "a scalar and an array in one declaration": the local array gets no slot of its own,
       连 `m[1]`/`m[2]` 的**读取指令都不会生成**（汇编里一处都没有），于是触摸坐标
       not even the **read instructions** for `m[1]`/`m[2]` are generated (not one of them in the assembly), so the touch coordinates
       恒为 0、命中判定全部落空 —— 表现是**按键完全没反应**（连按下高亮都没有），
       stay 0 and every hit test misses -- the symptom is that **keys do not react at all** (not even a press highlight),
       而界面绘制一切正常，很难往"输入"上想。
       while all the drawing looks perfectly fine, so it is hard to even suspect the input path.
       plane.c 那边 `int m[4];` 本来就单独声明，所以同样的代码在那边是好的。
       In plane.c `int m[4];` was already declared on its own, so the same code works fine over there.
    */
    int m[4];

    W = ui_scr_w();
    H = ui_scr_h();

    g_lang = ui_get_language();

    /* 标题先落进变量再传：沿用本文件 draw_display() 里记下的那条写法约定
       Put the title into a variable first and then pass it: following the convention recorded in draw_display() of this file
       （那里注释着"参数位置的三元表达式这条前端会生成坏掉的地址"）。
       (the comment there says "a ternary in an argument position generates a broken address on this frontend").
       别处（plane.c / pacman.c 一类）直接写在实参位置是好的，但这个文件既然记过这个坑，
       Elsewhere (plane.c / pacman.c and the like) writing it right in the argument position works, but since this file has recorded the pitfall,
       就照着它自己的约定写，别在这里做风格实验。
       follow its own convention and do not run style experiments here.
    */
    {
        char* title;
        if (g_lang == 0) title = "算盘";
        else             title = "Calculator";
        ui_win_open_ex(title, W, H, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    }
    ui_keep_on(1);

    /* 布局：上面 26% 给显示屏，下面按键区 */
    /* Layout: the top 26% goes to the display, the rest below is the key area. */
    bgap = 8;
    btop = H * 26 / 100;
    bh = (H - btop - 20 - bgap * 4) / 5;       /* 5 行 + 4 条竖缝 */
    /* 5 rows + 4 vertical gaps */
    bw = (W - 28 - bgap * 3) / 4;              /* 4 列 + 3 条横缝（左右各留 14）*/
    /* 4 columns + 3 horizontal gaps (14 left over on each side) */
    bcols = 4; brows = 5;

    acc = 0; cur = 0; curDigits = 0; hasDot = 0;
    pendingOp = 0; entering = 0; err = 0; exprLen = 0; expr[0] = 0;
    pressIdx = -1; flashT = 0;

    ui_msg_clear();

    /* 静态界面：只在需要重画时出图（省电），按下/抬起各重画一次 */
    /* Static interface: only present when a redraw is needed (to save power); press/release each redraw once. */
    draw();
    ui_present();

    while (ui_win_closed() == 0) {
        t = ui_wait(m, 400);

        if (t == VML_MSG_TOUCHDOWN) {
            /* ⚠ 一行一个变量：别写 `int x = m[1], y = m[2];` —— 同一条声明里塞多个
               ⚠ One variable per line: do not write `int x = m[1], y = m[2];` -- several
               变量在这条前端上不可靠（见 main 里 `m` 那段说明），拆开才拿得到正确的值。
               variables in one declaration are unreliable on this frontend (see the `m` note in main); only split apart do you get correct values.
            */
            int x;
            int y;
            x = m[1];
            y = m[2];
            pressIdx = -1;
            for (i = 0; i < 20; i++) {
                keyRect(i);
                if (x >= rx && x < rx + rw && y >= ry && y < ry + rh) { pressIdx = i; break; }
            }
            if (pressIdx >= 0) { flashT = 6; draw(); ui_present(); }
        } else if (t == VML_MSG_TOUCHUP) {
            if (pressIdx >= 0) {
                on_key(pressIdx);
                pressIdx = -1;
                draw();
                ui_present();
            }
        } else if (t == VML_MSG_WINDOWCLOSE) {
            break;
        } else if (t == VML_MSG_TIMER || t == VML_MSG_NONE) {
            /* 兜底重画：触摸事件偶尔丢一条时，界面不至于停在"按下"的样子 */
            /* Fallback redraw: when a touch event is occasionally lost, the UI does not stay stuck in the "pressed" look. */
            if (flashT > 0) {
                flashT = flashT - 1;
                if (flashT == 0 && pressIdx >= 0) { pressIdx = -1; draw(); ui_present(); }
            }
        }
    }

    ui_keep_on(0);
    ui_sfx_panic();
    return 0;
}
