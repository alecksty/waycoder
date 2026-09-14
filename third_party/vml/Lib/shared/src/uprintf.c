/* uprintf.c — VML 32-bit Unicode printf
 * Converts char32_t format string to UTF-8, then calls printf
 */
#param lib("uchar")
#param lib("printf")
#param lib("uscanf")
#param lib("wprintf")

extern void printf(const char *fmt, ...);
extern size_t ucs_to_utf8(const unsigned int *src, char *dest, size_t max);

__stdcall void uprintf(const unsigned int *ufmt) {
    char buf[1024];
    ucs_to_utf8(ufmt, buf, sizeof(buf) - 1);
    printf(buf);
}
