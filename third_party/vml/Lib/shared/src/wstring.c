/* wstring.c — VML 16-bit wide string library implementation */
#param lib("wchar")

typedef unsigned short wchar_t;

size_t wcslen(const wchar_t *s) {
    size_t len = 0;
    while (s[len] != 0) len++;
    return len;
}

int wcscmp(const wchar_t *a, const wchar_t *b) {
    while (*a && *a == *b) { a++; b++; }
    return (int)*a - (int)*b;
}

int wcsncmp(const wchar_t *a, const wchar_t *b, size_t n) {
    if (n == 0) return 0;
    while (--n && *a && *a == *b) { a++; b++; }
    return (int)*a - (int)*b;
}

wchar_t *wcscpy(wchar_t *dest, const wchar_t *src) {
    wchar_t *p = dest;
    while ((*p++ = *src++) != 0);
    return dest;
}

wchar_t *wcsncpy(wchar_t *dest, const wchar_t *src, size_t n) {
    size_t i;
    for (i = 0; i < n && src[i] != 0; i++) dest[i] = src[i];
    for (; i < n; i++) dest[i] = 0;
    return dest;
}

wchar_t *wcscat(wchar_t *dest, const wchar_t *src) {
    wchar_t *p = dest;
    while (*p) p++;
    while ((*p++ = *src++) != 0);
    return dest;
}

wchar_t *wcsncat(wchar_t *dest, const wchar_t *src, size_t n) {
    wchar_t *p = dest; size_t i = 0;
    while (*p) p++;
    while (i < n && src[i] != 0) { *p++ = src[i++]; }
    *p = 0;
    return dest;
}

wchar_t *wcschr(const wchar_t *s, wchar_t c) {
    while (*s) { if (*s == c) return (wchar_t *)s; s++; }
    return c == 0 ? (wchar_t *)s : 0;
}

wchar_t *wcsrchr(const wchar_t *s, wchar_t c) {
    const wchar_t *last = 0;
    while (*s) { if (*s == c) last = s; s++; }
    return c == 0 ? (wchar_t *)s : (wchar_t *)last;
}

wchar_t *wcsstr(const wchar_t *haystack, const wchar_t *needle) {
    if (*needle == 0) return (wchar_t *)haystack;
    while (*haystack) {
        const wchar_t *h = haystack, *n = needle;
        while (*h && *n && *h == *n) { h++; n++; }
        if (*n == 0) return (wchar_t *)haystack;
        haystack++;
    }
    return 0;
}

size_t wcsspn(const wchar_t *s, const wchar_t *accept) {
    size_t count = 0;
    while (*s) {
        const wchar_t *a = accept; int found = 0;
        while (*a) { if (*s == *a) { found = 1; break; } a++; }
        if (!found) break;
        count++; s++;
    }
    return count;
}

size_t wcscspn(const wchar_t *s, const wchar_t *reject) {
    size_t count = 0;
    while (*s) {
        const wchar_t *r = reject;
        while (*r) { if (*s == *r) return count; r++; }
        count++; s++;
    }
    return count;
}

wchar_t *wmemcpy(wchar_t *dest, const wchar_t *src, size_t n) {
    wchar_t *d = dest; while (n--) *d++ = *src++;
    return dest;
}

wchar_t *wmemset(wchar_t *dest, wchar_t c, size_t n) {
    wchar_t *d = dest; while (n--) *d++ = c;
    return dest;
}

wchar_t *wmemmove(wchar_t *dest, const wchar_t *src, size_t n) {
    wchar_t *d = dest;
    if (d < src) { while (n--) *d++ = *src++; }
    else { d += n; src += n; while (n--) *--d = *--src; }
    return dest;
}

int wmemcmp(const wchar_t *a, const wchar_t *b, size_t n) {
    while (n--) { if (*a != *b) return (int)*a - (int)*b; a++; b++; }
    return 0;
}
