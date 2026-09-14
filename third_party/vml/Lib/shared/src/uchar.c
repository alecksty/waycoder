#param lib("uscanf")
#param lib("ustring")

/* uchar.c — VML 32-bit Unicode character library implementation */

/* Types (inline, to avoid header circular dependency) */
typedef unsigned short wchar_t;
typedef unsigned int char32_t;

/* Forward declarations for internal use */
int c32tombs(char *dest, char32_t uc);
int mbtoc32(char32_t *dest, const char *src);

/* ── Length ─────────────────────────────────── */
size_t ucslen(const char32_t *s)
{
    size_t len = 0;
    while (s[len] != 0)
        len++;
    return len;
}

/* ── Compare ────────────────────────────────── */
int ucscmp(const char32_t *a, const char32_t *b)
{
    while (*a && *a == *b) { a++; b++; }
    if (*a == *b) return 0;
    return *a < *b ? -1 : 1;
}

int ucsncmp(const char32_t *a, const char32_t *b, size_t n)
{
    if (n == 0) return 0;
    while (--n && *a && *a == *b) { a++; b++; }
    if (*a == *b) return 0;
    return *a < *b ? -1 : 1;
}

/* ── Copy ───────────────────────────────────── */
char32_t *ucscpy(char32_t *dest, const char32_t *src)
{
    char32_t *p = dest;
    while ((*p++ = *src++) != 0) ;
    return dest;
}

char32_t *ucsncpy(char32_t *dest, const char32_t *src, size_t n)
{
    size_t i;
    for (i = 0; i < n && src[i] != 0; i++)
        dest[i] = src[i];
    for (; i < n; i++)
        dest[i] = 0;
    return dest;
}

/* ── Concatenate ────────────────────────────── */
char32_t *ucscat(char32_t *dest, const char32_t *src)
{
    char32_t *p = dest;
    while (*p) p++;
    while ((*p++ = *src++) != 0) ;
    return dest;
}

char32_t *ucsncat(char32_t *dest, const char32_t *src, size_t n)
{
    char32_t *p = dest;
    size_t i = 0;
    while (*p) p++;
    while (i < n && src[i] != 0) { *p++ = src[i++]; }
    *p = 0;
    return dest;
}

/* ── Search ─────────────────────────────────── */
char32_t *ucschr(const char32_t *s, char32_t c)
{
    while (*s != 0)
    {
        if (*s == c) return (char32_t *)s;
        s++;
    }
    return c == 0 ? (char32_t *)s : 0;
}

char32_t *ucsrchr(const char32_t *s, char32_t c)
{
    const char32_t *last = 0;
    while (*s != 0)
    {
        if (*s == c) last = s;
        s++;
    }
    return c == 0 ? (char32_t *)s : (char32_t *)last;
}

char32_t *ucsstr(const char32_t *haystack, const char32_t *needle)
{
    if (*needle == 0) return (char32_t *)haystack;
    while (*haystack)
    {
        const char32_t *h = haystack;
        const char32_t *n = needle;
        while (*h && *n && *h == *n) { h++; n++; }
        if (*n == 0) return (char32_t *)haystack;
        haystack++;
    }
    return 0;
}

size_t ucsspn(const char32_t *s, const char32_t *accept)
{
    size_t count = 0;
    while (*s)
    {
        const char32_t *a = accept;
        int found = 0;
        while (*a) { if (*s == *a) { found = 1; break; } a++; }
        if (!found) break;
        count++;
        s++;
    }
    return count;
}

size_t ucscspn(const char32_t *s, const char32_t *reject)
{
    size_t count = 0;
    while (*s)
    {
        const char32_t *r = reject;
        while (*r) { if (*s == *r) return count; r++; }
        count++;
        s++;
    }
    return count;
}

/* ── Memory ─────────────────────────────────── */
char32_t *umemcpy(char32_t *dest, const char32_t *src, size_t n)
{
    char32_t *d = dest;
    while (n--) *d++ = *src++;
    return dest;
}

char32_t *umemset(char32_t *dest, char32_t c, size_t n)
{
    char32_t *d = dest;
    while (n--) *d++ = c;
    return dest;
}

char32_t *umemmove(char32_t *dest, const char32_t *src, size_t n)
{
    char32_t *d = dest;
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

int umemcmp(const char32_t *a, const char32_t *b, size_t n)
{
    while (n--)
    {
        if (*a != *b)
        {
            if (*a < *b) return -1;
            return 1;
        }
        a++; b++;
    }
    return 0;
}

/* ── Conversion: char32_t ↔ UTF-8 ───────────── */
int c32tombs(char *dest, char32_t uc)
{
    if (uc < 0x80)
    {
        dest[0] = (char)uc;
        return 1;
    }
    else if (uc < 0x800)
    {
        dest[0] = (char)(0xC0 | (uc >> 6));
        dest[1] = (char)(0x80 | (uc & 0x3F));
        return 2;
    }
    else if (uc < 0x10000)
    {
        dest[0] = (char)(0xE0 | (uc >> 12));
        dest[1] = (char)(0x80 | ((uc >> 6) & 0x3F));
        dest[2] = (char)(0x80 | (uc & 0x3F));
        return 3;
    }
    else
    {
        /* Surrogate pairs: 4-byte UTF-8 for U+10000 ~ U+10FFFF */
        dest[0] = (char)(0xF0 | (uc >> 18));
        dest[1] = (char)(0x80 | ((uc >> 12) & 0x3F));
        dest[2] = (char)(0x80 | ((uc >> 6) & 0x3F));
        dest[3] = (char)(0x80 | (uc & 0x3F));
        return 4;
    }
}

int mbtoc32(char32_t *dest, const char *src)
{
    unsigned char b0 = (unsigned char)src[0];
    if (b0 < 0x80)
    {
        *dest = (char32_t)b0;
        return 1;
    }
    else if ((b0 & 0xE0) == 0xC0)
    {
        *dest = (char32_t)(((b0 & 0x1F) << 6) | ((unsigned char)src[1] & 0x3F));
        return 2;
    }
    else if ((b0 & 0xF0) == 0xE0)
    {
        *dest = (char32_t)(((b0 & 0x0F) << 12) | (((unsigned char)src[1] & 0x3F) << 6) | ((unsigned char)src[2] & 0x3F));
        return 3;
    }
    else if ((b0 & 0xF8) == 0xF0)
    {
        *dest = (char32_t)(((b0 & 0x07) << 18) | (((unsigned char)src[1] & 0x3F) << 12)
                         | (((unsigned char)src[2] & 0x3F) << 6) | ((unsigned char)src[3] & 0x3F));
        return 4;
    }
    return -1;
}

size_t ucs_to_utf8(const char32_t *src, char *dest, size_t max)
{
    size_t written = 0;
    while (*src)
    {
        char buf[5];
        int n = c32tombs(buf, *src);
        if (n < 0 || written + n >= max) break;
        for (int i = 0; i < n; i++)
            dest[written++] = buf[i];
        src++;
    }
    if (written < max) dest[written] = '\0';
    return written;
}

size_t utf8_to_ucs(const char *src, char32_t *dest, size_t max)
{
    size_t count = 0;
    while (*src && count < max - 1)
    {
        int n = mbtoc32(&dest[count], src);
        if (n < 0) break;
        src += n;
        count++;
    }
    if (count < max) dest[count] = 0;
    return count;
}

/* ── Conversion: char32_t ↔ wchar_t ─────────── */
size_t ucs_to_wcs(const char32_t *src, wchar_t *dest, size_t max)
{
    size_t count = 0;
    while (*src && count < max - 1)
    {
        if (*src <= 0xFFFF)
        {
            /* BMP character: direct copy */
            dest[count++] = (wchar_t)*src;
        }
        else
        {
            /* Supplementary plane: convert to surrogate pair */
            if (count + 1 >= max - 1) break;
            char32_t cp = *src - 0x10000;
            dest[count++] = (wchar_t)(0xD800 | (cp >> 10));
            dest[count++] = (wchar_t)(0xDC00 | (cp & 0x3FF));
        }
        src++;
    }
    if (count < max) dest[count] = 0;
    return count;
}

size_t wcs_to_ucs(const wchar_t *src, char32_t *dest, size_t max)
{
    size_t count = 0;
    while (*src && count < max - 1)
    {
        wchar_t wc = *src;
        if (wc >= 0xD800 && wc <= 0xDBFF)
        {
            /* High surrogate: combine with next low surrogate */
            src++;
            wchar_t lo = *src;
            if (lo >= 0xDC00 && lo <= 0xDFFF)
            {
                dest[count++] = 0x10000 + ((wc - 0xD800) << 10) + (lo - 0xDC00);
                src++;
                continue;
            }
            /* Invalid surrogate sequence: treat high surrogate as-is */
            dest[count++] = (char32_t)wc;
        }
        else if (wc >= 0xDC00 && wc <= 0xDFFF)
        {
            /* Lone low surrogate: treat as-is */
            dest[count++] = (char32_t)wc;
            src++;
        }
        else
        {
            dest[count++] = (char32_t)wc;
            src++;
        }
    }
    if (count < max) dest[count] = 0;
    return count;
}
