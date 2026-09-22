/* old_std_wc.c —— 仿 Unix 的 wc（命令行的，`getchar` 状态机 + **EOF 收尾**）
 *
 * 类别：std
 * 兼容面：**逐字符读 `getchar()`**、`while ((c = getchar()) != EOF)` 收尾、
 *         `isspace` 状态机、`%d` 输出
 * 出处：自写，仿 Unix 第 7 版那种一次只做一件小事的工具。
 *
 * ## 这个文件改过一次，原因值得记下来
 *
 * 第一版用的是哨兵字符 `.` 收尾，注释里写着「`EOF` 在本平台等不到」——
 * 那是**当时的事实**，绕开它是不得已。根因后来查明并修掉了：不是宿主不给 EOF，
 * 而是 `io.c` 的 `getchar` 从来没把 R0 置 1（`asm("SYSCALL #5, ${1}")` 里的
 * `${1}` 不是变量、什么都不生成），于是它一直走**非阻塞**分支，
 * 输入一空就返回 0 —— "读到的字符是 0"看起来就像"没有 EOF 这个信号"。
 *
 * 现在它走 `SYSCALL #14`（与 #5 同，只差"耗尽时给 EOF(-1) 而不是空行"），
 * 所以**这一版用的是老程序原本那句 `!= EOF`** —— 这正是本文件存在的意义：
 * 它是「EOF 收尾能不能用」这条的判据。哨兵收尾的写法在别处另有示例。
 *
 * ## 判据怎么读
 *
 * 喂 `--stdin 'hello world\nfoo bar\n'` 应得「字符 23，词 4，行 2」左右——
 * 具体数字不必与真实 `wc` 对齐（本平台的行尾换行由输入的 `\n` 决定），
 * **要看的是"它自己收尾了、不是被超时掐掉的"**（被掐的话宿主会打
 * `VM execution cancelled`，而且退出码非 0）。
 */
#include <stdio.h>
#include <ctype.h>

int main(void)
{
    int c;
    int chars = 0, words = 0, lines = 0;
    int in_word = 0;

    printf("输入文本，Ctrl+D / Ctrl+Z 结束（脚本化运行时 = 输入用完）\n");

    while ((c = getchar()) != EOF) {
        chars++;
        if (c == '\n') lines++;

        if (isspace(c)) {
            in_word = 0;
        } else if (!in_word) {
            in_word = 1;
            words++;
        }
    }

    printf("字符 %d，词 %d，行 %d\n", chars, words, lines);
    return 0;
}
