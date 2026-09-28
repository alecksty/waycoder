/* old_std_hanoi.c —— 汉诺塔（命令行的，递归 + printf 格式化）
 * old_std_hanoi.c -- Towers of Hanoi (command line, recursion + printf formatting)
 *
 * 类别：std
 * Category: std
 * 兼容面：递归函数、`%*s` 动态宽度、字符数组当字符串用、全局计数
 * Compatibility: recursive functions, `%*s` dynamic width, character arrays used as strings, and a global counter
 * 出处：自写，仿 80 年代算法教材的经典例题（塔的图形也是当年那种 ASCII 风格）。
 * Origin: self-written, imitating the classic example from 1980s algorithm textbooks (the tower graphic is in that era's ASCII style too).
 */
#include <stdio.h>
#include <waycoder_ui.h>     /* 只为 ui_get_language() —— 老程序不该猜系统语言 */
                            /* only for ui_get_language() -- an old program must not guess the system language */

static int moves = 0;

/* 把 n 个盘从 from 经 via 移到 to，并打印每一步 */
/* Move n disks from `from` via `via` to `to`, printing every step */
static void hanoi(int n, char from, char via, char to)
{
    if (n <= 0) return;

    hanoi(n - 1, from, to, via);

    moves++;
    printf("%3d: %c -> %c\n", moves, from, to);

    hanoi(n - 1, via, from, to);
}

int main(void)
{
    int n = 4;
    int lang;   /* 界面语言：开局查一次 */
                /* UI language: queried once at start */

    lang = ui_get_language();

    printf(lang == 0 ? "汉诺塔（%d 个盘）\n" : "Towers of Hanoi (%d disks)\n", n);
    hanoi(n, 'A', 'B', 'C');
    printf(lang == 0 ? "共 %d 步（2^%d - 1 = %d）\n" : "%d moves in total (2^%d - 1 = %d)\n",
           moves, n, (1 << n) - 1);

    /* 顺带钉一条老程序常用的格式化：动态宽度右对齐 */
    /* Also pin down one formatting trick old programs used a lot: dynamic-width right alignment */
    printf(lang == 0 ? "右对齐演示：\n" : "Right-aligned demo:\n");
    for (int i = 1; i <= 5; i++)
        printf("  %*d|\n", 5, i * i);

    return 0;
}
