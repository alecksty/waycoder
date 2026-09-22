/* old_std_hanoi.c —— 汉诺塔（命令行的，递归 + printf 格式化）
 *
 * 类别：std
 * 兼容面：递归函数、`%*s` 动态宽度、字符数组当字符串用、全局计数
 * 出处：自写，仿 80 年代算法教材的经典例题（塔的图形也是当年那种 ASCII 风格）。
 */
#include <stdio.h>

static int moves = 0;

/* 把 n 个盘从 from 经 via 移到 to，并打印每一步 */
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

    printf("汉诺塔（%d 个盘）\n", n);
    hanoi(n, 'A', 'B', 'C');
    printf("共 %d 步（2^%d - 1 = %d）\n", moves, n, (1 << n) - 1);

    /* 顺带钉一条老程序常用的格式化：动态宽度右对齐 */
    printf("右对齐演示：\n");
    for (int i = 1; i <= 5; i++)
        printf("  %*d|\n", 5, i * i);

    return 0;
}
