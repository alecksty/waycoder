/* old_std_guess.c —— 猜数字（命令行的，纯 stdio + rand）
 *
 * 类别：std
 * 兼容面：`srand`/`rand`、`scanf` 循环读、输入为 0 时主动退出（老程序的"输入 0 结束"约定）
 * 出处：自写，仿 BASIC/早期 C 教材里最常见的"猜数字"。
 */
#include <stdio.h>
#include <stdlib.h>

int main(void)
{
    int secret, guess, tries;

    /* ⚠ 固定种子：老程序常这么写（当年没法取时间），也让本用例可复现 ——
     *   随机数不钉死的话，"跑通了"这件事本身没法当判据。 */
    srand(12345);
    secret = rand() % 100 + 1;
    tries  = 0;

    printf("我想了一个 1..100 的数，你来猜（输入 0 放弃）\n");

    for (;;) {
        printf("你猜：");
        if (scanf("%d", &guess) != 1) break;   /* 读不到就结束 */

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
