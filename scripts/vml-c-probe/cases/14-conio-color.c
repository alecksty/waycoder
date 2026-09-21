// conio 的**颜色映射**判据：DOS 的 16 色号 → ANSI SGR。
//
// ## 为什么单独一条
//
// `conio` 唯一的产物是 ANSI 字节流，而 **DOS 的调色板顺序与 ANSI 的不一样**：
//
//     DOS : 0黑 1蓝 2绿 3青 4红 5紫 6棕 7浅灰   (8-15 同序的亮色档)
//     ANSI: 40黑 41红 42绿 43黄 44蓝 45紫 46青 47白
//
// 拿 DOS 号直接当 ANSI 号用 ⇒ **整屏颜色串位，而且不报错**。实现里就真错过一次：
// `textbackground(CYAN)` 打出 `[43m`（黄底）—— 一张 DOS 界面的每个颜色都挪了位。
//
// ## 期望值是从哪儿来的（**不是抄实现那张表**）
//
// 按 CGA 16 色序 与 ANSI SGR 标准色序**各自独立**写出来再对应：
//
//     DOS 1 蓝 → ANSI 34 蓝      DOS 3 青 → ANSI 36 青     DOS 6 棕 → ANSI 33 黄
//     DOS 4 红 → ANSI 31 红      DOS 8 深灰 → ANSI 90 亮黑  DOS 14 黄 → ANSI 93 亮黄
//
// 8-15 走 ANSI 的**亮色档** 90-97 / 100-107（真终端就是这么表达的）。
// 判据是 `run.sh` 转义过的 ANSI 字面量（`ESC[46m` → `\e[46m`），逐字节比对。
//
// ⚠ 每格都是**两条** SGR（`con_sgr` 无条件先前景后背景）—— 所以 `A` 组一格同时判到
//   fg 与 bg 两个映射。这不是判据绑实现细节：下面 `B` 组单独走 `textcolor`/
//   `textbackground`，`C` 组单独走三个亮度开关，三条路各判各的。
#include <stdio.h>
#include <conio.h>

int main()
{
    int i;

    clrscr();

    /* ⚠ `gotoxy` 一律放在判据 `printf` **之后**：它发的 `ESC[行;列H` 会跟在这一行的
       `KEY=` 后面（行首照样干净）；放到前面就落到**上一行行尾**去了 —— 上一行的
       `.*` 一路吃到行尾，那个 cup 就把上一条判据值污染了（实测踩过）。 */
    /* A：16 色走一遍。`textattr(i*17)` 里 i*17 == i|(i<<4) ⇒ 前景与背景同为第 i 号色，
       一次 putch 把 fg 与 bg 两条 SGR 都发出来。 */
    printf("\nA=");
    gotoxy(1, 2);
    for (i = 0; i < 16; i++) {
        textattr(i * 17);
        putch('#');
    }

    /* B：`textcolor` / `textbackground` 各自独立生效（别是只有 `textattr` 那条路通） */
    printf("\nB=");
    gotoxy(1, 3);
    textcolor(RED);          /* DOS 4  → ANSI 31 */
    textbackground(GREEN);   /* DOS 2  → ANSI 42 */
    putch('#');

    /* C：三个亮度开关都**只动前景的加亮位**（背景不该被它们碰到） */
    printf("\nC=");
    gotoxy(1, 4);
    textbackground(BLUE);    /* DOS 1  → ANSI 44，整组不变 */
    textcolor(BLUE);         /* DOS 1  → 34 */
    putch('#');
    highvideo();             /* 前景 +8 ⇒ DOS 9 亮蓝 → 94 */
    putch('#');
    lowvideo();              /* 前景 -8 ⇒ 回 DOS 1   → 34 */
    putch('#');
    normvideo();             /* 回"黑底浅灰"：fg 7 → 37 / bg 0 → 40 */
    putch('#');

    return 0;
}
// EXPECT: A=\e[2;1H\e[30m\e[40m#\e[34m\e[44m#\e[32m\e[42m#\e[36m\e[46m#\e[31m\e[41m#\e[35m\e[45m#\e[33m\e[43m#\e[37m\e[47m#\e[90m\e[100m#\e[94m\e[104m#\e[92m\e[102m#\e[96m\e[106m#\e[91m\e[101m#\e[95m\e[105m#\e[93m\e[103m#\e[97m\e[107m#|B=\e[3;1H\e[31m\e[42m#|C=\e[4;1H\e[34m\e[44m#\e[94m\e[44m#\e[34m\e[44m#\e[37m\e[40m#
