#include <conio.h>

static int cur_row = 1;
static int cur_col = 1;
static int fg_color = 7;
static int bg_color = 0;

void clrscr(void)
{
    int i;
    for (i = 0; i < 80 * 25 * 2; i += 2)
    {
        asm("MOVE R0, #0xB8000");
        asm("ADD R0, %0");
        asm("MOVEB [R0], #32");
        asm("MOVEB [R0+1], #7");
    }
    cur_row = 1;
    cur_col = 1;
    asm("MOVE R0, #1");
    asm("STORE R0, [0x6FF4]");
    asm("STORE R0, [0x6FF8]");
}

void gotoxy(int x, int y)
{
    cur_row = y;
    cur_col = x;
    if (cur_row < 1) cur_row = 1;
    if (cur_col < 1) cur_col = 1;
    if (cur_row > 25) cur_row = 25;
    if (cur_col > 80) cur_col = 80;
    asm("MOVE R0, %0");
    asm("STORE R0, [0x6FF4]");
    asm("MOVE R0, %0");
    asm("STORE R0, [0x6FF8]");
}

int wherex(void)
{
    return cur_col;
}

int wherey(void)
{
    return cur_row;
}

void textcolor(int color)
{
    fg_color = color & 0x0F;
    asm("MOVE R0, %0");
    asm("STOREB R0, [0x6FFC]");
}

void textbackground(int color)
{
    bg_color = color & 0x0F;
    asm("MOVE R0, %0");
    asm("STOREB R0, [0x6FFD]");
}

void cputs(const char *str)
{
    int offset;
    char ch;
    while (1)
    {
        asm("LOAD R0, %0");
        ch = *str;
        if (ch == 0) break;
        if (ch == '\n')
        {
            cur_row++;
            cur_col = 1;
            str++;
            continue;
        }
        offset = ((cur_row - 1) * 80 + (cur_col - 1)) * 2;
        asm("MOVE R0, #0xB8000");
        asm("ADD R0, %0");
        asm("MOVEB [R0], %0");
        asm("LOADB R1, [0x6FFC]");
        asm("LOADB R2, [0x6FFD]");
        asm("SHL R2, #4");
        asm("OR R1, R2");
        asm("MOVEB [R0+1], R1");
        cur_col++;
        str++;
        if (cur_col > 80)
        {
            cur_col = 1;
            cur_row++;
        }
        if (cur_row > 25) cur_row = 25;
    }
    asm("MOVE R0, %0");
    asm("STORE R0, [0x6FF4]");
    asm("MOVE R0, %0");
    asm("STORE R0, [0x6FF8]");
}

int getch(void)
{
    int ch;
    asm("SYSCALL 5");
    asm("MOVE %0, R0");
    return ch;
}

int getche(void)
{
    int ch;
    asm("SYSCALL 5");
    asm("MOVE %0, R0");
    asm("MOVE R0, %0");
    asm("SYSCALL 4");
    return ch;
}

int kbhit(void)
{
    int status;
    asm("LOADB R0, [0x64]");
    asm("MOVE %0, R0");
    return status;
}

void clreol(void)
{
    int i;
    for (i = cur_col - 1; i < 80; i++)
    {
        int offset = ((cur_row - 1) * 80 + i) * 2;
        asm("MOVE R0, #0xB8000");
        asm("ADD R0, %0");
        asm("MOVEB [R0], #32");
        asm("MOVEB [R0+1], #7");
    }
}

void delline(void) { clreol(); }
void insline(void) { clreol(); }

void delay(int ms)
{
    int i, j;
    for (i = 0; i < ms; i++)
        for (j = 0; j < 1000; j++)
            asm("NOP");
}

void sound(int freq)
{
    asm("MOVE R0, #7");
    asm("SYSCALL 4");
}

void nosound(void) {}
void _setcursortype(int type) {}

int putch(int ch)
{
    asm("MOVE R0, %0");
    asm("SYSCALL 4");
    return ch;
}

void highvideo(void) { fg_color = 15; asm("STOREB R0, [0x6FFC]"); }
void lowvideo(void) { fg_color = 7; asm("STOREB R0, [0x6FFC]"); }
void normvideo(void) { fg_color = 7; bg_color = 0; asm("MOVE R0, #7"); asm("STOREB R0, [0x6FFC]"); }
