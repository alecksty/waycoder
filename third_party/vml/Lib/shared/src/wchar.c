#param lib("wscanf")
#param lib("wstring")

/* wchar.c — VML 16-bit wide character library implementation */

/* Type: 16-bit Unicode character (inline, to avoid header parsing issues) */
typedef unsigned short wchar_t;

/* Forward declarations for internal use */
int wctomb(char *dest, wchar_t wc);
int mbtowc(wchar_t *dest, const char *src);

/* ── Length ─────────────────────────────────── */
size_t wcslen(const wchar_t *s)
{
    size_t len = 0;
    while (s[len] != 0)
        len++;
    return len;
}

/* ── Compare ────────────────────────────────── */
int wcscmp(const wchar_t *a, const wchar_t *b)
{
    while (*a && *a == *b) { a++; b++; }
    return (int)*a - (int)*b;
}

int wcsncmp(const wchar_t *a, const wchar_t *b, size_t n)
{
    if (n == 0) return 0;
    while (--n && *a && *a == *b) { a++; b++; }
    return (int)*a - (int)*b;
}

/* ── Copy ───────────────────────────────────── */
wchar_t *wcscpy(wchar_t *dest, const wchar_t *src)
{
    wchar_t *p = dest;
    while ((*p++ = *src++) != 0) ;
    return dest;
}

wchar_t *wcsncpy(wchar_t *dest, const wchar_t *src, size_t n)
{
    size_t i;
    for (i = 0; i < n && src[i] != 0; i++)
        dest[i] = src[i];
    for (; i < n; i++)
        dest[i] = 0;
    return dest;
}

/* ── Concatenate ────────────────────────────── */
wchar_t *wcscat(wchar_t *dest, const wchar_t *src)
{
    wchar_t *p = dest;
    while (*p) p++;
    while ((*p++ = *src++) != 0) ;
    return dest;
}

wchar_t *wcsncat(wchar_t *dest, const wchar_t *src, size_t n)
{
    wchar_t *p = dest;
    size_t i = 0;
    while (*p) p++;
    while (i < n && src[i] != 0) { *p++ = src[i++]; }
    *p = 0;
    return dest;
}

/* ── Search ─────────────────────────────────── */
wchar_t *wcschr(const wchar_t *s, wchar_t c)
{
    while (*s != 0)
    {
        if (*s == c) return (wchar_t *)s;
        s++;
    }
    return c == 0 ? (wchar_t *)s : 0;
}

wchar_t *wcsrchr(const wchar_t *s, wchar_t c)
{
    const wchar_t *last = 0;
    while (*s != 0)
    {
        if (*s == c) last = s;
        s++;
    }
    return c == 0 ? (wchar_t *)s : (wchar_t *)last;
}

wchar_t *wcsstr(const wchar_t *haystack, const wchar_t *needle)
{
    if (*needle == 0) return (wchar_t *)haystack;
    while (*haystack)
    {
        const wchar_t *h = haystack;
        const wchar_t *n = needle;
        while (*h && *n && *h == *n) { h++; n++; }
        if (*n == 0) return (wchar_t *)haystack;
        haystack++;
    }
    return 0;
}

size_t wcsspn(const wchar_t *s, const wchar_t *accept)
{
    size_t count = 0;
    while (*s)
    {
        const wchar_t *a = accept;
        int found = 0;
        while (*a) { if (*s == *a) { found = 1; break; } a++; }
        if (!found) break;
        count++;
        s++;
    }
    return count;
}

size_t wcscspn(const wchar_t *s, const wchar_t *reject)
{
    size_t count = 0;
    while (*s)
    {
        const wchar_t *r = reject;
        while (*r) { if (*s == *r) return count; r++; }
        count++;
        s++;
    }
    return count;
}

/* ── Memory ─────────────────────────────────── */
wchar_t *wmemcpy(wchar_t *dest, const wchar_t *src, size_t n)
{
    wchar_t *d = dest;
    while (n--) *d++ = *src++;
    return dest;
}

wchar_t *wmemset(wchar_t *dest, wchar_t c, size_t n)
{
    wchar_t *d = dest;
    while (n--) *d++ = c;
    return dest;
}

wchar_t *wmemmove(wchar_t *dest, const wchar_t *src, size_t n)
{
    wchar_t *d = dest;
    if (d < src)
    {
        while (n--) *d++ = *src++;
    }
    else
    {
        d += n;
        src += n;
        while (n--) *--d = *--src;
    }
    return dest;
}

int wmemcmp(const wchar_t *a, const wchar_t *b, size_t n)
{
    while (n--)
    {
        if (*a != *b) return (int)*a - (int)*b;
        a++; b++;
    }
    return 0;
}

/* ── Conversion ─────────────────────────────── */
int wctomb(char *dest, wchar_t wc)
{
    if (wc < 0x80)
    {
        dest[0] = (char)wc;
        return 1;
    }
    else if (wc < 0x800)
    {
        dest[0] = (char)(0xC0 | (wc >> 6));
        dest[1] = (char)(0x80 | (wc & 0x3F));
        return 2;
    }
    else
    {
        dest[0] = (char)(0xE0 | (wc >> 12));
        dest[1] = (char)(0x80 | ((wc >> 6) & 0x3F));
        dest[2] = (char)(0x80 | (wc & 0x3F));
        return 3;
    }
}

int mbtowc(wchar_t *dest, const char *src)
{
    unsigned char b0 = (unsigned char)src[0];
    if (b0 < 0x80)
    {
        *dest = (wchar_t)b0;
        return 1;
    }
    else if ((b0 & 0xE0) == 0xC0)
    {
        *dest = (wchar_t)(((b0 & 0x1F) << 6) | ((unsigned char)src[1] & 0x3F));
        return 2;
    }
    else if ((b0 & 0xF0) == 0xE0)
    {
        *dest = (wchar_t)(((b0 & 0x0F) << 12) | (((unsigned char)src[1] & 0x3F) << 6) | ((unsigned char)src[2] & 0x3F));
        return 3;
    }
    return -1;
}

size_t wcstombs(char *dest, const wchar_t *src, size_t max)
{
    size_t written = 0;
    while (*src)
    {
        char buf[4];
        int n = wctomb(buf, *src);
        if (n < 0 || written + n >= max) break;
        for (int i = 0; i < n; i++)
            dest[written++] = buf[i];
        src++;
    }
    if (written < max) dest[written] = '\0';
    return written;
}

size_t mbstowcs(wchar_t *dest, const char *src, size_t max)
{
    size_t count = 0;
    while (*src && count < max - 1)
    {
        int n = mbtowc(&dest[count], src);
        if (n < 0) break;
        src += n;
        count++;
    }
    if (count < max) dest[count] = 0;
    return count;
}
