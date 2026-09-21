// `conio.h` 的**格式化输出**那半边：`cprintf` / `cputs` / `putch`。
//
// ## 为什么单独一条（13/14/15 都没压到）
//
// 那三条压的是定位（`gotoxy`/`wherex`）、配色（`textcolor`/`textattr`）、
// 行操作（`delline`/`insline`/`clreol`）—— **`cprintf` 一个字符都没验过**。
// 而 DOS 老程序里 `cprintf("%d", x)` 是**最常出现的那一句**（`printf` 在 Turbo C 里
// 走的是 stdio，要 `#include <stdio.h>` + 初始化 FILE 流；`conio` 的 `cprintf`
// 是"不过流、直接写屏"，所以文本界面程序几乎清一色用它）。
//
// ## 期望值怎么独立推导（不是"跑出来是什么就写什么"）
//
// 出口链路读的是 `Lib/shared/src/conio.c`：
//
//   · `gotoxy(x,y)`   ⇒ `ESC[{y};{x}H`（**1 起**，与 DOS 同）
//   · `normvideo()`   ⇒ 状态置 `conFg=7, conBg=0`，**不发字节**
//   · `putch(c)`      ⇒ 先 `con_sgr((bg<<4)|fg)` 再发字符。`con_sgr` = 前景 SGR + 背景 SGR；
//                        DOS 7 ⇒ ANSI 37 ⇒ `ESC[37m`；DOS 0 ⇒ ANSI 40 ⇒ `ESC[40m`。
//                        ⇒ **每个可见字符前面都挂一对 `ESC[37mESC[40m`**
//   · `cprintf(fmt,…)`⇒ 自己不再造一份格式化：`format_arg_count` 取参数个数 →
//                        `vsnprintf` 格式化 → `cputs`（逐字符 `putch`）。
//                        ⇒ 判据落在"`%d`/`%s`/`%%`/`%c` 展开对不对"上。
//   · `cputs(s)`      ⇒ 逐字符 `putch`，**不解析 `%`**（这是它与 `cprintf` 的分界）。
//
// 所以 `%d|%s|%%|%c` 配 `(42,"ab",'Z')` 必须展开成 **9 个字符** `42|ab|%|Z`
// ⇒ `wherex()` = 起点列 + 9。**这个 9 是数出来的，不是从实得反推的。**
//
// ⚠ 判据 `printf` 要**另起一行**：`conio` 的输出与判据会粘在同一行，
//   而 run.sh 的匹配是行首锚定的 `^KEY=`（13/15 踩过）。用 `printf("\nK=…")`
//   而不是 `putch('\n')` —— 后者会推光标、把被测量的状态改掉。
//
// ⚠ 还有一条**排版上的次序**（第一版就是这么写错的）：每个小节是
//   `…printf("\nB=…");  normvideo(); gotoxy(…);  printf("\nC=…"); …`
//   —— `gotoxy` 的 `CUP` 发在**上一条判据文本之后、下一个 `\n` 之前**，
//   于是它**挂在上一行尾巴上**（`B=10,1\e[1;10H`），而不是落在 `C` 那一行的开头。
//   这不是缺陷，是"谁先发字节"的直接推论；期望值里要**照实带上**这几个 CUP。
#include <stdio.h>
#include <conio.h>

int main()
{
    /* ① `cprintf` 的格式化 + 光标推进。
          gotoxy(1,1) 起点列 0 ⇒ 写完 9 个字符后 conX=9 ⇒ wherex()=10。 */
    normvideo();
    gotoxy(1, 1);
    printf("\nA=");
    cprintf("%d|%s|%%|%c", 42, "ab", 'Z');
    printf("\nB=%d,%d", wherex(), wherey());

    /* ② `cputs` **不解析 `%`** —— 这正是它与 `cprintf` 的分界。
          起点列 9（gotoxy(10,…)）⇒ 写完 3 个字符 ⇒ wherex()=13。 */
    normvideo();
    gotoxy(10, 1);
    printf("\nC=");
    cputs("a%b");
    printf("\nD=%d,%d", wherex(), wherey());

    /* ③ `cprintf` 的宽度/左对齐（`printf` 家族那份实现的能力，走同一条路） */
    normvideo();
    gotoxy(1, 2);
    printf("\nE=");
    cprintf("[%5d][%-5d]", 42, 7);
    printf("\nF=%d,%d", wherex(), wherey());

    /* ④ `putch` 用**当前**颜色发字节：DOS 红(RED=4) ⇒ ANSI 1 ⇒ `ESC[31m`。
          ⚠ 这里卡的是 DOS↔ANSI 的**调色板换位**（DOS 1=蓝、ANSI 1=红）——
          直接拿 DOS 号当 ANSI 号会静默串色（conio.c 里那段注释就是为它写的）。 */
    normvideo();
    textcolor(RED);
    gotoxy(1, 3);
    printf("\nG=");
    putch('#');
    printf("\nH=%d,%d", wherex(), wherey());

    /* ⑤ `\n` 在 conio 里**自带回车**（DOS 语义，不是 POSIX）：列归 1、行 +1。
          起点 (5,4)：写完 'x' 到列 6，再 `putch(10)` ⇒ 列 1、行 5。 */
    normvideo();
    gotoxy(5, 4);
    printf("\nI=");
    putch('x');
    putch(10);
    printf("\nJ=%d,%d", wherex(), wherey());

    printf("\n");
    return 0;
}
// EXPECT: A=\e[37m\e[40m4\e[37m\e[40m2\e[37m\e[40m|\e[37m\e[40ma\e[37m\e[40mb\e[37m\e[40m|\e[37m\e[40m%\e[37m\e[40m|\e[37m\e[40mZ|B=10,1\e[1;10H|C=\e[37m\e[40ma\e[37m\e[40m%\e[37m\e[40mb|D=13,1\e[2;1H|E=\e[37m\e[40m[\e[37m\e[40m \e[37m\e[40m \e[37m\e[40m \e[37m\e[40m4\e[37m\e[40m2\e[37m\e[40m]\e[37m\e[40m[\e[37m\e[40m7\e[37m\e[40m \e[37m\e[40m \e[37m\e[40m \e[37m\e[40m \e[37m\e[40m]|F=15,2\e[3;1H|G=\e[31m\e[40m#|H=2,3\e[4;5H|I=\e[37m\e[40mx\e[5;1H|J=1,5
