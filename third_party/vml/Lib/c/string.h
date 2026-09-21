/* string.h - String handling
 * ISO C Standard 7.11
 */

#ifndef _STRING_H
#define _STRING_H

// 默认最小集已包含 string+memory (strlen/strcpy/memset/...)

#include <stddef.h>

/* Copying functions */
void *memcpy(void *dest, const void *src, size_t n);
void *memmove(void *dest, const void *src, size_t n);
char *strcpy(char *dest, const char *src);
char *strncpy(char *dest, const char *src, size_t n);

/* Concatenation functions */
char *strcat(char *dest, const char *src);
char *strncat(char *dest, const char *src, size_t n);

/* Comparison functions */
int memcmp(const void *s1, const void *s2, size_t n);
int strcmp(const char *s1, const char *s2);
int strncmp(const char *s1, const char *s2, size_t n);

/* 忽略大小写的比较。C 标准把它们放在 `<strings.h>` 里，但那个头本仓没有，
   而老程序**常常直接就用**（实测 cmatrix 引用了 16 次 `strcasecmp`）——
   所以放在这里让它"顺手就有"，比逼每个程序自己 include 一个不存在的头强。
   实现见 `Lib/shared/src/string.c`，只处理 ASCII（本平台只有 C locale）。 */
int strcasecmp(const char *s1, const char *s2);
int strncasecmp(const char *s1, const char *s2, size_t n);
int strcoll(const char *s1, const char *s2);
size_t strxfrm(char *dest, const char *src, size_t n);

/* Search functions */
void *memchr(const void *s, int c, size_t n);
char *strchr(const char *s, int c);
size_t strcspn(const char *s, const char *reject);
char *strpbrk(const char *s, const char *accept);
char *strrchr(const char *s, int c);
size_t strspn(const char *s, const char *accept);
char *strstr(const char *haystack, const char *needle);
char *strtok(char *str, const char *delim);

/* Other functions */
void *memset(void *s, int c, size_t n);
size_t strlen(const char *s);
char *strerror(int errnum);

#endif /* _STRING_H */