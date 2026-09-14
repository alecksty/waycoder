/* stdio.h — Standard I/O (D) */
#ifndef _STDIO_H
#define _STDIO_H
#param lib("file")
#param lib("stdio_funcs")
#include <stddef.h>
typedef int FILE;
int printf(const char *fmt, ...);
int sprintf(char *buf, const char *fmt, ...);
int putchar(int c);
#endif
