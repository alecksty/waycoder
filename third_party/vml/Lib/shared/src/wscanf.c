/* wscanf.c — VML wide-character scanf
 * Converts wchar_t format to UTF-8, calls scanf, converts result back
 */
#param lib("wchar")
#param lib("scanf")

typedef unsigned short wchar_t;

extern void scanf(const char *fmt);
extern size_t wcstombs(char *dest, const wchar_t *src, size_t max);

__stdcall void wscanf(const wchar_t *wfmt) {
    char buf[1024];
    wcstombs(buf, wfmt, sizeof(buf) - 1);
    scanf(buf);
}
