/* old_tty_conio.c —— Turbo C / Borland conio 风格（彩色的 tty）
 * old_tty_conio.c -- Turbo C / Borland conio style (color tty)
 *
 * 类别：tty
 * Category: tty
 * 兼容面：**conio.h 那一套**（`clrscr` / `gotoxy` / `textcolor` / `textbackground`
 *         / `textattr` / `cprintf` / `cputs` / `wherex` / `putch`）——
 *         DOS 时代最普及的屏幕控制接口，也是 `conio.h` 用量排第一的原因
 *         the most widespread screen-control interface of the DOS era, and the reason `conio.h` ranks first in usage
 * 出处：自写，仿 Turbo C 教材里的彩色菜单屏。
 * Origin: self-written, imitating the color menu screens found in Turbo C textbooks.
 */
#include <conio.h>
#include <stdio.h>
#include <waycoder_ui.h>     /* 只为 ui_get_language() —— 老程序不该猜系统语言 */
                            /* only for ui_get_language() -- an old program must not guess the system language */

int main(void)
{
    int i;
    int lang;   /* 界面语言：开局查一次 */
                /* UI language: queried once at start */

    lang = ui_get_language();

    clrscr();

    /* 标题：黑底亮青 */
    textcolor(11);          /* LIGHTCYAN */
    textbackground(0);      /* BLACK */
    gotoxy(10, 2);
    cprintf(lang == 0 ? "Turbo C 风格演示" : "Turbo C style demo");

    /* 调色板：15 色一行一个 */
    /* Palette: the 15 colors, printed one after another */
    gotoxy(6, 4);
    cputs(lang == 0 ? "16 色前景：" : "16 foreground colors:");
    for (i = 0; i < 16; i++) {
        textcolor(i);
        cprintf("%2d ", i);
    }

    /* 反白条（textattr = 背景<<4 | 前景） */
    /* Reverse-video bar (textattr = background<<4 | foreground) */
    gotoxy(6, 7);
    textattr((4 << 4) | 15);   /* 红底白字 */
                               /* White text on a red background */
    cprintf(lang == 0 ? "  当前选中项（红底白字）  " : "  current item (white on red)  ");
    textattr(7);               /* 恢复默认 */
                               /* Restore the default */

    /* 画一条横线（老程序里的分隔线就这么画） */
    /* Draw a horizontal rule (this is how separators were drawn in old programs) */
    gotoxy(6, 9);
    for (i = 0; i < 40; i++) putch('-');

    /* wherex/wherey 读回光标位置 —— 老程序用它算对齐 */
    /* wherex/wherey read the cursor position back -- old programs used it to work out alignment */
    gotoxy(6, 11);
    cprintf(lang == 0 ? "光标在 (%d,%d)" : "Cursor at (%d,%d)", wherex(), wherey());

    /* 单字符输出 + 换行 */
    /* Single-character output + newline */
    gotoxy(6, 13);
    cputs(lang == 0 ? "单字符：" : "Single chars: ");
    putch('O');
    putch('K');

    gotoxy(1, 16);
    return 0;
}
