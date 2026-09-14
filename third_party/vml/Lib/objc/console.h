/* console.h — Console functions (ObjC) */

#ifndef _CONSOLE_H
#define _CONSOLE_H

int console_clear(void);
int console_gotoxy(int x, int y);
int console_textcolor(int fg, int bg);
void console_cursor(int visible);

#endif
