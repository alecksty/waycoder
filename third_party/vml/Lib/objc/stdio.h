/* stdio.h — Standard I/O (ObjC) */

#ifndef _STDIO_H
#define _STDIO_H

#param lib("file")
#param lib("stdio_funcs")

#include <stddef.h>

typedef int FILE;
extern int stdin;
extern int stdout;
extern int stderr;

int printf(const char *fmt, ...);
int sprintf(char *buf, const char *fmt, ...);
int putchar(int c);
int puts(const char *s);

#endif
