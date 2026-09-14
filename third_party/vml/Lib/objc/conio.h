/* conio.h — Console I/O (ObjC) */

#ifndef _CONIO_H
#define _CONIO_H

#define VGA_BASE    0xB8000
#define VGA_WIDTH   80
#define VGA_HEIGHT  25

void vga_clear(void);
void vga_putchar(int c);
int kb_hit(void);
int getch(void);

#endif
