/* old_std_guess.c —— 猜数字（命令行的，纯 stdio + rand）
 * old_std_guess.c -- guess the number (command line, plain stdio + rand)
 *
 * 类别：std
 * Category: std
 * 兼容面：`srand`/`rand`、`scanf` 循环读、输入为 0 时主动退出（老程序的"输入 0 结束"约定）
 * Compatibility: `srand` / `rand`, a `scanf` loop, and quitting voluntarily when the input is 0 (the old "type 0 to finish" convention)
 * 出处：自写，仿 BASIC/早期 C 教材里最常见的"猜数字"。
 * Origin: self-written, imitating the most common "guess the number" exercise in BASIC and early C textbooks.
 */
#include <stdio.h>
#include <stdlib.h>

int main(void)
{
    int secret, guess, tries;

    /* ⚠ 固定种子：老程序常这么写（当年没法取时间），也让本用例可复现 ——
     * ⚠ A fixed seed: old programs often did this (there was no way to read the time back then), and it makes this case reproducible --
     *   随机数不钉死的话，"跑通了"这件事本身没法当判据。
     *   without pinning the random numbers, "it ran through" cannot itself serve as a criterion.
     */
    srand(12345);
    secret = rand() % 100 + 1;
    tries  = 0;

    printf("我想了一个 1..100 的数，你来猜（输入 0 放弃）\n");

    for (;;) {
        printf("你猜：");
        if (scanf("%d", &guess) != 1) break;   /* 读不到就结束 */
                                               /* Finish when nothing can be read */

        if (guess == 0) {
            printf("放弃啦？答案是 %d\n", secret);
            break;
        }

        tries++;
        if (guess < secret)      printf("小了\n");
        else if (guess > secret) printf("大了\n");
        else {
            printf("对了！用了 %d 次\n", tries);
            break;
        }
    }

    return 0;
}
