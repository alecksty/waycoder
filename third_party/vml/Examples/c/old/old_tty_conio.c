/* old_tty_conio.c —— Turbo C / Borland conio 风格（彩色的 tty）
 *
 * 类别：tty
 * 兼容面：**conio.h 那一套**（`clrscr` / `gotoxy` / `textcolor` / `textbackground`
 *         / `textattr` / `cprintf` / `cputs` / `wherex` / `putch`）——
 *         DOS 时代最普及的屏幕控制接口，也是 `conio.h` 用量排第一的原因
 * 出处：自写，仿 Turbo C 教材里的彩色菜单屏。
 */
#include <conio.h>
#include <stdio.h>

int main(void)
{
    int i;

    clrscr();

    /* 标题：黑底亮青 */
    textcolor(11);          /* LIGHTCYAN */
    textbackground(0);      /* BLACK */
    gotoxy(10, 2);
    cprintf("Turbo C 风格演示");

    /* 调色板：15 色一行一个 */
    gotoxy(6, 4);
    cputs("16 色前景：");
    for (i = 0; i < 16; i++) {
        textcolor(i);
        cprintf("%2d ", i);
    }

    /* 反白条（textattr = 背景<<4 | 前景） */
    gotoxy(6, 7);
    textattr((4 << 4) | 15);   /* 红底白字 */
    cprintf("  当前选中项（红底白字）  ");
    textattr(7);               /* 恢复默认 */

    /* 画一条横线（老程序里的分隔线就这么画） */
    gotoxy(6, 9);
    for (i = 0; i < 40; i++) putch('-');

    /* wherex/wherey 读回光标位置 —— 老程序用它算对齐 */
    gotoxy(6, 11);
    cprintf("光标在 (%d,%d)", wherex(), wherey());

    /* 单字符输出 + 换行 */
    gotoxy(6, 13);
    cputs("单字符：");
    putch('O');
    putch('K');

    gotoxy(1, 16);
    return 0;
}
