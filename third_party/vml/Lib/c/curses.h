/* VML curses.h stub — minimal ncurses compatibility */
#ifndef _CURSES_H
#define _CURSES_H
#include <stdio.h>

/* chtype */
typedef unsigned int chtype;

/* Mouse */
typedef struct { short y, x; unsigned long bstate; } MEVENT;
#define BUTTON1_CLICKED        0x1
#define BUTTON1_RELEASED       0x2
#define BUTTON2_CLICKED        0x4
#define BUTTON2_RELEASED       0x8
#define BUTTON3_CLICKED        0x10
#define BUTTON3_RELEASED       0x20
#define ALL_MOUSE_EVENTS       0x3F
#define BUTTON_CTRL            0x100
#define BUTTON_SHIFT           0x200
#define BUTTON_ALT             0x400

/* Key codes */
#define KEY_DOWN    0402
#define KEY_UP      0403
#define KEY_LEFT    0404
#define KEY_RIGHT   0405
#define KEY_ENTER   0x0A
#define KEY_BACKSPACE 0x08
#define KEY_F(n)    (0410 + (n))
#define KEY_MOUSE   0631
#define KEY_NPAGE   0522
#define KEY_PPAGE   0523

/* Colors */
#define COLOR_BLACK   0
#define COLOR_RED     1
#define COLOR_GREEN   2
#define COLOR_YELLOW  3
#define COLOR_BLUE    4
#define COLOR_MAGENTA 5
#define COLOR_CYAN    6
#define COLOR_WHITE   7
#define A_REVERSE     0x10
#define A_BOLD        0x20
#define A_DIM         0x40
#define A_STANDOUT    0x80
#define A_UNDERLINE   0x100
#define A_NORMAL      0x00

/* Functions & Return Values */
#define ERR (-1)
#define OK  (0)

/* ACS (Alternative Character Set) — mapped to ASCII approximations */
#define ACS_VLINE    '|'
#define ACS_HLINE    '-'
#define ACS_ULCORNER '+'
#define ACS_URCORNER '+'
#define ACS_LLCORNER '+'
#define ACS_LRCORNER '+'
#define ACS_LTEE     '+'
#define ACS_RTEE     '+'
#define ACS_TTEE     '+'
#define ACS_BTEE     '+'
#define ACS_PLUS     '+'
#define ACS_S1       '-'
#define ACS_S9       '-'
#define ACS_DIAMOND  '+'
#define ACS_CKBOARD  '#'
#define ACS_DEGREE   '\''
#define ACS_PLMINUS  '#'
#define ACS_BULLET   'o'
#define ACS_LARROW   '<'
#define ACS_RARROW   '>'
#define ACS_UARROW   '^'
#define ACS_DARROW   'v'
#define ACS_BOARD    '#'
#define ACS_LANTERN  '#'
#define ACS_BLOCK    '#'

#define COLOR_PAIR(n) ((n) << 8)

int COLS;
int LINES;
void initscr(void);
void endwin(void);
void noecho(void);
void cbreak(void);
void refresh(void);
void erase(void);
void flushinp(void);
int getch(void);
int ungetch(int ch);
int mvaddch(int y, int x, chtype ch);
int mvaddstr(int y, int x, const char* str);
int mvprintw(int y, int x, const char* fmt, ...);
int printw(const char* fmt, ...);
int addstr(const char* str);
int attron(chtype attr);
int attroff(chtype attr);
int init_pair(int pair, int fg, int bg);
int mousemask(unsigned long mask, void* old);
int getmouse(MEVENT* event);
int nc_getmouse(MEVENT* event);

void* stdscr;

#endif
