/* calc.c —— 《算盘》计算器（C 版），跑在手机端 VML 上
 *
 * 编译运行（手机 App 的 vml 工具）：
 *     vml run examples/c/calc.c
 *
 * ## 操作：**只有手指**，不要手柄
 *
 * 点按键就行。开窗时声明 `VML_WIN_NO_GAMEPAD`（整块手柄区不显示）+ `VML_WIN_PORTRAIT`
 * 锁竖屏 —— 屏幕上的东西每一块都要有用。
 *
 * ## 界面怎么"好看"
 *
 * - **背景**：径向渐变（中心亮、四周沉），一块**玻璃面板**放显示屏
 * - **按键**：圆角矩形 + 渐变（数字键浅、功能键橙、等号蓝），**按下去有高亮反馈**
 * - **显示屏**：右对齐、算式在上（小字暗色）、结果在下（大字亮色）
 * - **按下反馈**：手指按住期间那格换成高亮色并微微内缩 —— 没有动画库，全靠"换色 + 改尺寸"
 *
 * ## 数字怎么算的（**没有浮点格式化**）
 *
 * 用**千分之一定点**：内部一律用 `值 × 100` 的整数存，显示时再插小数点。
 * 这样加减乘除全是整数运算，不用 `sprintf`、不用 `%f`，结果精确到分。
 * ⚠ 除法是**先放大再除**（`a * 100 / b`），顺序反了会把小数位截没。
 *
 * ## 三条 C 前端硬约束（与 plane.c 相同）
 *
 * 1. ⚠ `${}` 只认局部变量 ⇒ syscall 一律走包装函数
 * 2. ⚠ 复合字面量 `(int[]){…}` 不支持也不报错 ⇒ 顶点用**具名全局数组**
 * 3. ⚠ `#define` 不支持反斜杠续行
 */

#include <waycoder_ui.h>

/* ── 配色 ───────────────────────────────────────────────── */

#define BG_A       0xFF1B2233
#define BG_B       0xFF0B0F18
#define PANEL      0x33FFFFFF
#define PANEL_EDGE 0x55FFFFFF
#define DISP_EXPR  0x99C7D6E8
#define DISP_NUM   0xFFFFFFFF
#define DISP_ERR   0xFFFF7B6B

/* 四档按键色，按**功能分区**选不同色相 —— 色相拉开才分得清，
   同一色系只差明度的话（比如两个蓝灰）在手机上几乎看不出区别：
     数字键  深蓝灰（安静，占大多数）
     运算符  暗金  （"这是要算的"）
     功能键  砖红  （C / ± / % —— 会改变当前输入，给一点警示感）
     等号    亮蓝  （唯一的"执行"键，最跳） */
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

int W, H;
int btop, bh, bw, bgap;          /* 按键区起点 / 键高 / 键宽 / 间隙（两个方向同一个）*/
int bcols, brows;

int acc;                         /* 累加器（×100 定点） */
int cur;                         /* 正在输入的数（×100） */
int curDigits;                   /* 已输入位数 */
int hasDot;                      /* 小数点后已几位 */
int pendingOp;                   /* 待执行的运算：0 无 / 1 + / 2 - / 3 × / 4 ÷ */
int entering;                    /* 是否正在输入新数 */
int err;                         /* 出错（除以 0） */

char expr[64];                   /* 上方那行算式 */
int  exprLen;

int pressIdx;                    /* 当前按住的键（-1 无） */
int flashT;                      /* 按下高亮还能亮几拍 */

/* 键盘：4 列 × 5 行。
 *
 * ⚠ **键面用整数码 + 一个 label_of()，不要 `char* keys[20] = {"C", …}`** ——
 *    全局的**指针数组**在这条前端上会读出垃圾（实测：运行到取键面就
 *    `内存错误 地址=FFFFFFF1`）。整数数组是稳的（前端刚修过全局 char/int 数组的元素类型）。
 *
 * 码：0-9 = 数字，10 +，11 -，12 *，13 /，14 =，15 C，16 +/-，17 %，18 .，19 空位
 */
int keyCode[20] = {
    15, 16, 17, 13,
     7,  8,  9, 12,
     4,  5,  6, 11,
     1,  2,  3, 10,
     0, 19, 18, 14
};
/* 0 数字 / 1 功能(C ± %) / 2 等号 / 3 运算符(+ - x /)
 *
 * ⚠ 用**函数**给，不用数组查表：`keyFn[i]` 这种「全局 int 数组的**元素比较**」在这条
 *    前端上读不出正确的值（数据段里明明是 1/3/2，程序却一律当 0，于是所有键都画成
 *    数字键的颜色 —— 而同一张表用 `label_of(keyCode[i])` 就是好的，见文件头那条）。
 *    改成 if 链之后与 `label_of` 完全同构，行为一致、可预期。 */
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

char g_fmt[24];

/* v 是 ×100 的定点数 → 写进全局 g_fmt："12.34" / "12" / "-0.05"
 *
 * ⚠ 直接写全局量、**不接收 out 指针**：这条前端的「指针形参写回」已知不可靠
 *    （见文件头第 3 条：`void f(int* x){ *x=…; }` 实测会生成坏地址）。写全局是
 *    **规避**这条风险 —— 与 `keyRect` 改成"全局量返回"同一个理由，全文件一种写法。 */
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

int keyX(int c) { return 14 + c * (bw + bgap); }
int keyW(void)  { return bw; }
int keyY(int r) { return btop + r * (bh + bgap); }

/* 按键在屏幕上的矩形。结果放**全局**量 ——
   ⚠ 不用「指针形参写回」（`void f(int* x) { *x = …; }`）：实测这条前端在那上面会崩
   （`内存错误 地址=FFFFFFF1`，而同样的逻辑改成全局量就正常）。 */
int rx, ry, rw, rh;

void keyRect(int i)
{
    int r = i / 4, c = i % 4;
    rx = keyX(c);
    ry = keyY(r);
    rw = keyW();
    rh = bh;
    if (i == 16) rw = keyW() * 2 + bgap;      /* 0 跨两格 */
}

/* ── 运算 ───────────────────────────────────────────────── */

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
    }
    return b;
}

/* 按下一个键 */
void on_key(int i)
{
    int c = keyCode[i];

    if (err && c != 15) return;                       /* 出错后只认 C */

    if (c >= 0 && c <= 9) {                           /* 数字 */
        if (curDigits < MAXDIG) {
            if (!entering) { cur = 0; curDigits = 0; hasDot = 0; entering = 1; }
            cur = cur * 10 + c * 100;
            curDigits = curDigits + 1;
            ui_beep(1200, 12);
        }
        return;
    }

    if (c == 18) {                                    /* . */
        if (!entering) { cur = 0; curDigits = 0; entering = 1; hasDot = 0; }
        if (hasDot == 0) hasDot = 1;
        ui_beep(1200, 12);
        return;
    }

    if (c == 16) { cur = -cur; ui_beep(900, 14); return; }        /* +/- */
    if (c == 17) { cur = cur / 100; ui_beep(900, 14); return; }   /* % */

    if (c == 15) {                                    /* C */
        acc = 0; cur = 0; curDigits = 0; hasDot = 0;
        pendingOp = 0; entering = 0; err = 0;
        exprLen = 0; expr[0] = 0;
        ui_beep(420, 40);
        return;
    }

    /* 四则运算与等号 */
    if (c >= 10 && c <= 14) {
        int op = c - 9;                               /* 1 + / 2 - / 3 x / 4 / / 5 = */

        if (op == 5) {
            if (pendingOp != 0) acc = apply(acc, cur, pendingOp);
            else acc = cur;
            cur = acc; entering = 0; curDigits = 0; hasDot = 0;
            pendingOp = 0;
            exprLen = 0; expr[0] = 0;
            if (err) { ui_beep(300, 120); } else { ui_beep(1400, 40); }
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
        ui_beep(900, 18);
    }
}

/* ── 渲染 ───────────────────────────────────────────────── */

void draw_key(int i)
{
    int x, y, w, h, col, tcol, dy, f;
    int held = (pressIdx == i);

    /* 空位键（码 19）：整个不画。
       ⚠ 它本来就只是列表里的占位符 —— `0` 键（i=16）跨两格、右半边正好压在它上面。
       但绘制顺序是 i=0..19，i=17 在 i=16 **之后**画 ⇒ 照常画的话它会盖住跨格键的右半，
       屏幕上就多出一个空按钮（用户报的"下方有多的空按钮"就是这个）。
       所以必须**在画之前**就返回，而不是画完矩形再跳过文字。 */
    if (keyCode[i] == 19) return;

    /* ⚠ keyRect 的结果经**全局量** rx/ry/rw/rh 返回（原因见其定义处），这里立刻拷进
       局部 —— 免得函数体后半段再有什么东西动了那四个全局量。

       这里原先写的是 `keyRect(i, &x, &y, &w, &h)`：那是「指针形参写回」版本的调用点，
       而 keyRect 后来改成了全局量返回、**调用点忘了跟着改**。C 前端不检查实参个数
       （多余的实参求值后静默丢弃），于是 x/y/w/h 一路保持未初始化 —— 每个键都按垃圾
       坐标/尺寸去画，屏幕上按键区就是一片空白，而编译期一声不响。 */
    keyRect(i);
    x = rx; y = ry; w = rw; h = rh;

    /* ⚠ 别在赋值右边写嵌套三元（同上面那条：这条前端会生成坏地址）*/
    f = fn_of(i);                     /* ⚠ 用函数、不用 keyFn[i] 查表（原因见 fn_of 处） */

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
    }

    /* 底部一道暗边，做出"立体键"的感觉 */
    ui_rect(x, y + 3, w, h, 0x55000000, 1, 0, 14);
    ui_rect(x, y + dy, w, h, col, 1, 0, 14);
    ui_rect(x, y + dy, w, h, KEY_EDGE, 0, 2, 14);      /* 描边 */

    tcol = 0xFFFFFFFF;
    if (f == 0) tcol = KEY_TXT;

    if (keyCode[i] == 13) {                            /* 除号：自己画，省得依赖字体有没有那个字形 */
        ui_line(x + w / 2 - 9, y + h / 2 + dy, x + w / 2 + 9, y + h / 2 + dy, tcol, 3);
        ui_circle(x + w / 2 - 9, y + h / 2 - 7 + dy, 2, tcol, 1, 0);
        ui_circle(x + w / 2 + 9, y + h / 2 - 7 + dy, 2, tcol, 1, 0);
        ui_circle(x + w / 2, y + h / 2 - 12 + dy, 2, tcol, 1, 0);
        ui_circle(x + w / 2, y + h / 2 + 10 + dy, 2, tcol, 1, 0);
        return;
    }

    ui_set_font(h * 34 / 100, VML_FONT_BOLD, tcol, VML_ANCHOR_CENTER);
    ui_text_cur(x + w / 2, y + h / 2 - h * 16 / 100 + dy, label_of(keyCode[i]));
}

void draw_display(void)
{
    int px = 14, py = 16, pw = W - 28, ph = btop - 34;

    /* 玻璃面板 */
    ui_gradient("glass", 0, PANEL, 0x11FFFFFF, 0, 0, 0, 1000);
    ui_rect_grad(px, py, pw, ph, "glass", 18);
    ui_rect(px, py, pw, ph, PANEL_EDGE, 0, 2, 18);

    /* 算式行（小、暗、右对齐） */
    if (exprLen > 0) {
        ui_set_font(ph * 15 / 100, 0, DISP_EXPR, VML_ANCHOR_RIGHT);
        ui_text_cur(px + pw - 18, py + ph * 14 / 100, expr);
    }

    /* 结果行（大、亮、右对齐）。
       ⚠ 这里原本写的是「参数位置的三元表达式」—— 这条前端在那上面会生成坏掉的地址
       （实测 `MOVE @1, R0 地址=FFFFFFF1`）。改成先算进局部变量再传，稳妥得多。 */
    {
        int show = cur;
        int col = DISP_NUM;
        char* txt = g_fmt;

        if (err) { col = DISP_ERR; txt = "错误"; show = 0; }
        else if (!entering && pendingOp != 0) show = acc;

        fmt(show);
        ui_set_font(ph * 34 / 100, VML_FONT_BOLD, col, VML_ANCHOR_RIGHT);
        ui_text_cur(px + pw - 18, py + ph * 52 / 100, txt);
    }

    /* ⚠ 临时诊断：按住某键时在面板左下角画个洋红方块 ——
       用来区分「TOUCHDOWN 根本没收到」与「收到了但高亮没生效」。验完删。 */
    if (pressIdx >= 0 || flashT > 0) ui_rect(px + 8, py + ph - 34, 18, 18, 0xFFFF00FF, 1, 0, 3);
    {
    }
}

void draw(void)
{
    int i;

    /* ⚠ 先清场：绘制都往宿主的**同一张图元表**里追加，只有 ui_clear 会清空它。
       这里每点一次按键就整屏重画一遍，不清的话玩久了同样会把宿主撑爆（见 plane.c 的说明）。*/
    ui_clear(BG_B);

    /* 径向渐变底：中心亮、四周沉 */
    ui_gradient("bg", 1, BG_A, BG_B, 500, 380, 780, 0);
    ui_rect_grad(0, 0, W, H, "bg", 0);

    draw_display();
    for (i = 0; i < 20; i++) draw_key(i);
}

/* ── 入口 ───────────────────────────────────────────────── */

int main(void)
{
    int i, t;
    /* ⚠ `m` 必须**单独一行**声明，别挤进 `int i, m[4], t;`。
       这条前端对「标量与数组写在同一条声明里」处理不了：局部数组分不到自己的槽位，
       连 `m[1]`/`m[2]` 的**读取指令都不会生成**（汇编里一处都没有），于是触摸坐标
       恒为 0、命中判定全部落空 —— 表现是**按键完全没反应**（连按下高亮都没有），
       而界面绘制一切正常，很难往"输入"上想。
       plane.c 那边 `int m[4];` 本来就单独声明，所以同样的代码在那边是好的。 */
    int m[4];

    W = ui_scr_w();
    H = ui_scr_h();

    ui_win_open_ex("算盘", W, H, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_keep_on(1);

    /* 布局：上面 26% 给显示屏，下面按键区 */
    bgap = 8;
    btop = H * 26 / 100;
    bh = (H - btop - 20 - bgap * 4) / 5;       /* 5 行 + 4 条竖缝 */
    bw = (W - 28 - bgap * 3) / 4;              /* 4 列 + 3 条横缝（左右各留 14）*/
    bcols = 4; brows = 5;

    acc = 0; cur = 0; curDigits = 0; hasDot = 0;
    pendingOp = 0; entering = 0; err = 0; exprLen = 0; expr[0] = 0;
    pressIdx = -1; flashT = 0;

    ui_msg_clear();

    /* 静态界面：只在需要重画时出图（省电），按下/抬起各重画一次 */
    draw();
    ui_present();

    while (ui_win_closed() == 0) {
        t = ui_wait(m, 400);

        if (t == VML_MSG_TOUCHDOWN) {
            /* ⚠ 一行一个变量：别写 `int x = m[1], y = m[2];` —— 同一条声明里塞多个
               变量在这条前端上不可靠（见 main 里 `m` 那段说明），拆开才拿得到正确的值。 */
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
            if (flashT > 0) {
                flashT = flashT - 1;
                if (flashT == 0 && pressIdx >= 0) { pressIdx = -1; draw(); ui_present(); }
            }
        }
    }

    ui_keep_on(0);
    return 0;
}
