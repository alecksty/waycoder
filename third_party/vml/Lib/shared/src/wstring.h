/* wstring.h — VML 16-bit wide string library
 * wchar_t* strings terminated by 0x0000 (double NUL)
 * Pure string operations; character conversion in wchar.h
 */

#ifndef _WSTRING_H
#define _WSTRING_H

typedef unsigned short wchar_t;

/* Length: returns number of characters (excluding terminator) */
size_t wcslen(const wchar_t *s);

/* Compare */
int     wcscmp(const wchar_t *a, const wchar_t *b);
int     wcsncmp(const wchar_t *a, const wchar_t *b, size_t n);

/* Copy */
wchar_t *wcscpy(wchar_t *dest, const wchar_t *src);
wchar_t *wcsncpy(wchar_t *dest, const wchar_t *src, size_t n);

/* Concatenate */
wchar_t *wcscat(wchar_t *dest, const wchar_t *src);
wchar_t *wcsncat(wchar_t *dest, const wchar_t *src, size_t n);

/* Search */
wchar_t *wcschr(const wchar_t *s, wchar_t c);
wchar_t *wcsrchr(const wchar_t *s, wchar_t c);
wchar_t *wcsstr(const wchar_t *haystack, const wchar_t *needle);
size_t   wcsspn(const wchar_t *s, const wchar_t *accept);
size_t   wcscspn(const wchar_t *s, const wchar_t *reject);

/* Memory (n = number of wchar_t elements) */
wchar_t *wmemcpy(wchar_t *dest, const wchar_t *src, size_t n);
wchar_t *wmemset(wchar_t *dest, wchar_t c, size_t n);
wchar_t *wmemmove(wchar_t *dest, const wchar_t *src, size_t n);
int      wmemcmp(const wchar_t *a, const wchar_t *b, size_t n);

#endif /* _WSTRING_H */
