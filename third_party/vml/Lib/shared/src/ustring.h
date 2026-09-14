/* ustring.h — VML 32-bit Unicode string library
 * char32_t* strings terminated by 0x00000000 (quad NUL)
 * Pure string operations; character conversion in uchar.h
 */

#ifndef _USTRING_H
#define _USTRING_H

typedef unsigned int char32_t;

size_t    ucslen(const char32_t *s);
int       ucscmp(const char32_t *a, const char32_t *b);
int       ucsncmp(const char32_t *a, const char32_t *b, size_t n);
char32_t *ucscpy(char32_t *dest, const char32_t *src);
char32_t *ucsncpy(char32_t *dest, const char32_t *src, size_t n);
char32_t *ucscat(char32_t *dest, const char32_t *src);
char32_t *ucsncat(char32_t *dest, const char32_t *src, size_t n);
char32_t *ucschr(const char32_t *s, char32_t c);
char32_t *ucsrchr(const char32_t *s, char32_t c);
char32_t *ucsstr(const char32_t *haystack, const char32_t *needle);
size_t    ucsspn(const char32_t *s, const char32_t *accept);
size_t    ucscspn(const char32_t *s, const char32_t *reject);
char32_t *umemcpy(char32_t *dest, const char32_t *src, size_t n);
char32_t *umemset(char32_t *dest, char32_t c, size_t n);
char32_t *umemmove(char32_t *dest, const char32_t *src, size_t n);
int       umemcmp(const char32_t *a, const char32_t *b, size_t n);

#endif
