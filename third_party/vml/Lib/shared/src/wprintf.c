/* wprintf.c — VML wide-character printf
 * Converts wchar_t format string to UTF-8, then calls printf
 * Supports: %d %x %s (wchar_t*) %c (wchar_t) %f
 */
#param lib("wchar")
#param lib("printf")

typedef unsigned short wchar_t;

extern void printf(const char *fmt, ...);
extern size_t wcstombs(char *dest, const wchar_t *src, size_t max);

/// wprintf: wide format string → UTF-8 → printf
__stdcall void wprintf(const wchar_t *wfmt) {
    char buf[1024];
    wcstombs(buf, wfmt, sizeof(buf) - 1);
    printf(buf);
}
