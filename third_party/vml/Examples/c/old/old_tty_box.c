/* old_tty_box.c —— 用框线字符拼界面（彩色的 tty）
 *
 * 类别：tty
 * 兼容面：**扩展 ASCII / CP437 框线字符**（`┌─┐│└┘` 这类，老 DOS 界面的骨架）、
 *         ANSI 前景/背景色、光标定位
 * 出处：自写，仿 Turbo C / QBasic 时代那种单线框菜单。
 */
#include <stdio.h>

#define ESC "\033"

static void at(int y, int x) { printf(ESC "[%d;%dH", y, x); }

/* 画一个单线框（宽 w、高 h，左上角在 y,x）—— 老程序里这是最常手写的一段 */
static void box(int y, int x, int w, int h, int color)
{
    int i;

    printf(ESC "[%dm", color);
    at(y, x);         printf("┌");
    at(y, x + w - 1); printf("┐");
    at(y + h - 1, x); printf("└");
    at(y + h - 1, x + w - 1); printf("┘");

    for (i = 1; i < w - 1; i++) {
        at(y, x + i);         printf("─");
        at(y + h - 1, x + i); printf("─");
    }
    for (i = 1; i < h - 1; i++) {
        at(y + i, x);         printf("│");
        at(y + i, x + w - 1); printf("│");
    }
    printf(ESC "[0m");
}

int main(void)
{
    printf(ESC "[2J");

    box(2, 4, 34, 9, 36);          /* 青边框 */
    box(3, 6, 30, 7, 33);          /* 黄内框 */

    printf(ESC "[1;33m");
    at(4, 12); printf("主 菜 单");
    printf(ESC "[0m");

    printf(ESC "[37m");
    at(6, 9);  printf("1. 新建文件");
    at(7, 9);  printf("2. 打开文件");
    printf(ESC "[7m");             /* 反白 = 当前选中项 */
    at(8, 9);  printf("3. 退出      ");
    printf(ESC "[0m");

    printf(ESC "[36m");
    at(13, 4); printf("F1=帮助  ESC=返回  ↑↓=选择");
    printf(ESC "[0m");

    at(15, 1);
    return 0;
}
