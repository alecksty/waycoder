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

#define KEY_NUM    0xFF2A3346
#define KEY_NUM_HI 0xFF3C4A63
#define KEY_FN     0xFFE08A2B
#define KEY_FN_HI  0xFFFFA94D
#define KEY_EQ     0xFF2E7DD1
#define KEY_EQ_HI  0xFF4C9BEF
#define KEY_TXT    0xFFF2F6FA
#define KEY_EDGE   0x22FFFFFF

#define MAXDIG 12

/* ── 状态 ───────────────────────────────────────────────── */

int W, H;
int btop, bh, bgap;              /* 按键区起点 / 键高 / 间隙 */
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
int keyFn[20] = {                 /* 1 = 功能键（橙）2 = 等号（蓝） */
    1,1,1,1,
    0,0,0,1,
    0,0,0,1,
    0,0,0,1,
    0,2,0,2
};

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

/* v 是 ×100 的定点数 → "12.34" / "12" / "-0.05" */
void fmt(char* out, int v)
{
    int i = 0, j = 0, neg = 0, ip, fp;
    char t[16];

    if (v < 0) { neg = 1; v = -v; }
    ip = v / 100;
    fp = v % 100;

    if (ip == 0) t[j++] = '0';
    while (ip > 0 && j < 14) { t[j++] = (char)('0' + ip % 10); ip = ip / 10; }
    if (neg) out[i++] = '-';
    while (j > 0) out[i++] = t[--j];

    if (fp != 0) {
        out[i++] = '.';
        out[i++] = (char)('0' + (fp / 10) % 10);
        if (fp % 10 != 0) out[i++] = (char)('0' + fp % 10);
    }
    out[i] = 0;
}

int atoi_(char* b)
{
    int i = 0, v = 0;
    while (b[i] >= '0' && b[i] <= '9') { v = v * 10 + (b[i] - '0'); i = i + 1; }
    return v;
}

/* ── 布局 ───────────────────────────────────────────────── */

int keyX(int c) { return 14 + c * ((W - 28) / 4); }
int keyW(void)  { return (W - 28) / 4 - bgap; }
int keyY(int r) { return btop + r * (bh + bgap); }

/* 按键在屏幕上的矩形。结果放**全局**量 ——
   ⚠ 不用「指针形参写回」（`void f(int* x) { *x = …; }`）：实测这条前端在那上面会崩
   （`内存错误 地址=FFFFFFF1`，而同样的逻辑改成全局量就正常）。 */
int rx, ry, rw, rh;

void keyRect(int i)
{
    int r = i / 4, c = i % 4;
    rx = keyX(c) + c * bgap;
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
        fmt(g_fmt, acc);
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
    int x, y, w, h, col, tcol, dy;
    int held = (pressIdx == i);

    keyRect(i, &x, &y, &w, &h);

    /* ⚠ 别在赋值右边写嵌套三元（同上面那条：这条前端会生成坏地址）*/
    col = KEY_NUM; dy = 0;
    if (keyFn[i] == 1) col = KEY_FN;
    if (keyFn[i] == 2) col = KEY_EQ;
    if (held) {
        col = KEY_NUM_HI;
        if (keyFn[i] == 1) col = KEY_FN_HI;
        if (keyFn[i] == 2) col = KEY_EQ_HI;
        dy = 2;                       /* 按下去：整格下沉 2px（比缩放省事，效果一样清楚） */
    }

    /* 底部一道暗边，做出"立体键"的感觉 */
    ui_rect(x, y + 3, w, h, 0x55000000, 1, 0, 14);
    ui_rect(x, y + dy, w, h, col, 1, 0, 14);
    ui_rect(x, y + dy, w, h, KEY_EDGE, 0, 2, 14);      /* 描边 */

    tcol = 0xFFFFFFFF;
    if (keyFn[i] == 0) tcol = KEY_TXT;
    if (keyCode[i] == 19) return;                      /* 空位 */

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

        fmt(g_fmt, show);
        ui_set_font(ph * 34 / 100, VML_FONT_BOLD, col, VML_ANCHOR_RIGHT);
        ui_text_cur(px + pw - 18, py + ph * 52 / 100, txt);
    }
}

void draw(void)
{
    int i;

    /* 径向渐变底：中心亮、四周沉 */
    ui_gradient("bg", 1, BG_A, BG_B, 500, 380, 780, 0);
    ui_rect_grad(0, 0, W, H, "bg", 0);

    draw_display();
    for (i = 0; i < 20; i++) draw_key(i);
}

/* ── 入口 ───────────────────────────────────────────────── */

int main(void)
{
    int i, m[4], t;

    W = ui_scr_w();
    H = ui_scr_h();

    ui_win_open_ex("算盘", W, H, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_keep_on(1);

    /* 布局：上面 26% 给显示屏，下面按键区 */
    bgap = 8;
    btop = H * 26 / 100;
    bh = (H - btop - 20 - bgap * 4) / 5;
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
            int x = m[1], y = m[2];
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
