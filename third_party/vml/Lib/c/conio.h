#ifndef _CONIO_H
#define _CONIO_H

// 默认最小集已包含 io (putchar/getchar/kb_hit/vga_clear)

#include <stdarg.h>

#define VGA_BASE    0xB8000
#define VGA_WIDTH   80
#define VGA_HEIGHT  25
#define CURSOR_ROW_ADDR 0x6FF4
#define CURSOR_COL_ADDR 0x6FF8
#define TEXT_FG_ADDR    0x6FFC
#define TEXT_BG_ADDR    0x6FFD
#define KBD_DATA_ADDR   0x60
#define KBD_STAT_ADDR   0x64

#define _NOCURSOR       0
#define _SOLIDCURSOR    1
#define _NORMALCURSOR   2

#define BLACK           0
#define BLUE            1
#define GREEN           2
#define CYAN            3
#define RED             4
#define MAGENTA         5
#define BROWN           6
#define LIGHTGRAY       7
#define DARKGRAY        8
#define LIGHTBLUE       9
#define LIGHTGREEN      10
#define LIGHTCYAN       11
#define LIGHTRED        12
#define LIGHTMAGENTA    13
#define YELLOW          14
#define WHITE           15
#define BLINK           128

void clrscr(void);
void gotoxy(int x, int y);
int wherex(void);
int wherey(void);
void textcolor(int color);
void textbackground(int color);
int cprintf(const char *fmt, ...);
void cputs(const char *str);
int getch(void);
int getche(void);
int kbhit(void);
void clreol(void);
void delline(void);
void insline(void);
void delay(int ms);
void sound(int freq);
void nosound(void);
int putch(int ch);
void _setcursortype(int type);
void highvideo(void);
void lowvideo(void);
void normvideo(void);

#endif
