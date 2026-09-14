/* uscanf.c — VML 32-bit Unicode scanf
 * Converts char32_t format to UTF-8, calls scanf
 */
#param lib("uchar")
#param lib("scanf")

extern void scanf(const char *fmt);
extern size_t ucs_to_utf8(const unsigned int *src, char *dest, size_t max);

__stdcall void uscanf(const unsigned int *ufmt) {
    char buf[1024];
    ucs_to_utf8(ufmt, buf, sizeof(buf) - 1);
    scanf(buf);
}
