/* stdio.h - Standard input/output
 * ISO C Standard 7.9
 */

#ifndef _STDIO_H
#define _STDIO_H

#param lib("file")
#param lib("stdio_funcs")
// printf/putchar/getchar/sprintf 已由默认最小集提供

#include <stddef.h>
#include <stdarg.h>

/* File type - 使用 int 代替 FILE* 以避免编译器限制 */
typedef int FILE;     /* FILE is file descriptor number */

/* Standard streams (VML: handled via syscalls, FILE type is int handle) */
extern int stdin;
extern int stdout;
extern int stderr;

/* File operations */
FILE *fopen(const char *filename, const char *mode);
int fclose(FILE *stream);
int fflush(FILE *stream);

/* Character input/output */
int fgetc(FILE *stream);
int getc(FILE *stream);
int getchar(void);
int ungetc(int c, FILE *stream);

int fputc(int c, FILE *stream);
int putc(int c, FILE *stream);
int putchar(int c);

/* String input/output */
char *fgets(char *s, int size, FILE *stream);
int fputs(const char *s, FILE *stream);
char *gets(char *s);
int puts(const char *s);

/* Formatted input/output */
int printf(const char *format, ...);
int fprintf(FILE *stream, const char *format, ...);
int sprintf(char *str, const char *format, ...);
int snprintf(char *str, size_t size, const char *format, ...);

int scanf(const char *format, ...);
int fscanf(FILE *stream, const char *format, ...);
int sscanf(const char *str, const char *format, ...);

/* Variable argument versions */
int vprintf(const char *format, va_list ap);
int vfprintf(FILE *stream, const char *format, va_list ap);
int vsprintf(char *str, const char *format, va_list ap);
int vsnprintf(char *str, size_t size, const char *format, va_list ap);

/* File positioning */
int fseek(FILE *stream, long offset, int whence);
long ftell(FILE *stream);
void rewind(FILE *stream);

/* Error handling */
void clearerr(FILE *stream);
int feof(FILE *stream);
int ferror(FILE *stream);
void perror(const char *s);

/* Temporary files */
FILE *tmpfile(void);
char *tmpnam(char *s);

/* File removal/renaming */
int remove(const char *filename);
int rename(const char *oldname, const char *newname);

/* Buffer control */
void setbuf(FILE *stream, char *buf);
int setvbuf(FILE *stream, char *buf, int mode, size_t size);

/* EOF and BUFSIZ constants */
#define EOF (-1)
#define BUFSIZ 1024

/* File opening modes */
#define _IOFBF 0  /* Fully buffered */
#define _IOLBF 1  /* Line buffered */
#define _IONBF 2  /* No buffering */

/* fseek whence values */
#define SEEK_SET 0
#define SEEK_CUR 1
#define SEEK_END 2

#endif /* _STDIO_H */