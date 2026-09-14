/* uchar.h — VML 32-bit Unicode character library
 * char32_t = unsigned int (32-bit, UTF-32LE)
 * Strings terminated by 0x00000000 (quad NUL)
 * Supports full Unicode range (U+0000 ~ U+10FFFF) including emoji
 */
#param lib("uchar")
#param lib("wchar")

#ifndef _UCHAR_H
#define _UCHAR_H

/* Type: 32-bit Unicode code point (full range) */
typedef unsigned int char32_t;

/* Length: returns number of characters (excluding terminator) */
size_t ucslen(const char32_t *s);

/* Compare */
int     ucscmp(const char32_t *a, const char32_t *b);
int     ucsncmp(const char32_t *a, const char32_t *b, size_t n);

/* Copy */
char32_t *ucscpy(char32_t *dest, const char32_t *src);
char32_t *ucsncpy(char32_t *dest, const char32_t *src, size_t n);

/* Concatenate */
char32_t *ucscat(char32_t *dest, const char32_t *src);
char32_t *ucsncat(char32_t *dest, const char32_t *src, size_t n);

/* Search */
char32_t *ucschr(const char32_t *s, char32_t c);
char32_t *ucsrchr(const char32_t *s, char32_t c);
char32_t *ucsstr(const char32_t *haystack, const char32_t *needle);
size_t    ucsspn(const char32_t *s, const char32_t *accept);
size_t    ucscspn(const char32_t *s, const char32_t *reject);

/* Memory (n = number of char32_t elements) */
char32_t *umemcpy(char32_t *dest, const char32_t *src, size_t n);
char32_t *umemset(char32_t *dest, char32_t c, size_t n);
char32_t *umemmove(char32_t *dest, const char32_t *src, size_t n);
int       umemcmp(const char32_t *a, const char32_t *b, size_t n);

/* Conversion: char32_t ↔ UTF-8 */
int    c32tombs(char *dest, char32_t uc);
int    mbtoc32(char32_t *dest, const char *src);
size_t ucs_to_utf8(const char32_t *src, char *dest, size_t max);
size_t utf8_to_ucs(const char *src, char32_t *dest, size_t max);

/* Conversion: char32_t ↔ wchar_t */
size_t ucs_to_wcs(const char32_t *src, wchar_t *dest, size_t max);
size_t wcs_to_ucs(const wchar_t *src, char32_t *dest, size_t max);

#endif /* _UCHAR_H */
