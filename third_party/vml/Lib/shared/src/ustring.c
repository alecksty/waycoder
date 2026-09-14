/* ustring.c — VML 32-bit Unicode string library implementation */
#param lib("uchar")

typedef unsigned int char32_t;

size_t ucslen(const char32_t *s) {
    size_t len = 0;
    while (s[len] != 0) len++;
    return len;
}

int ucscmp(const char32_t *a, const char32_t *b) {
    while (*a && *a == *b) { a++; b++; }
    if (*a == *b) return 0;
    return *a < *b ? -1 : 1;
}

int ucsncmp(const char32_t *a, const char32_t *b, size_t n) {
    if (n == 0) return 0;
    while (--n && *a && *a == *b) { a++; b++; }
    if (*a == *b) return 0;
    return *a < *b ? -1 : 1;
}

char32_t *ucscpy(char32_t *dest, const char32_t *src) {
    char32_t *p = dest;
    while ((*p++ = *src++) != 0);
    return dest;
}

char32_t *ucsncpy(char32_t *dest, const char32_t *src, size_t n) {
    size_t i;
    for (i = 0; i < n && src[i] != 0; i++) dest[i] = src[i];
    for (; i < n; i++) dest[i] = 0;
    return dest;
}

char32_t *ucscat(char32_t *dest, const char32_t *src) {
    char32_t *p = dest;
    while (*p) p++;
    while ((*p++ = *src++) != 0);
    return dest;
}

char32_t *ucsncat(char32_t *dest, const char32_t *src, size_t n) {
    char32_t *p = dest; size_t i = 0;
    while (*p) p++;
    while (i < n && src[i] != 0) { *p++ = src[i++]; }
    *p = 0;
    return dest;
}

char32_t *ucschr(const char32_t *s, char32_t c) {
    while (*s) { if (*s == c) return (char32_t *)s; s++; }
    return c == 0 ? (char32_t *)s : 0;
}

char32_t *ucsrchr(const char32_t *s, char32_t c) {
    const char32_t *last = 0;
    while (*s) { if (*s == c) last = s; s++; }
    return c == 0 ? (char32_t *)s : (char32_t *)last;
}

char32_t *ucsstr(const char32_t *haystack, const char32_t *needle) {
    if (*needle == 0) return (char32_t *)haystack;
    while (*haystack) {
        const char32_t *h = haystack, *n = needle;
        while (*h && *n && *h == *n) { h++; n++; }
        if (*n == 0) return (char32_t *)haystack;
        haystack++;
    }
    return 0;
}

size_t ucsspn(const char32_t *s, const char32_t *accept) {
    size_t count = 0;
    while (*s) {
        const char32_t *a = accept; int found = 0;
        while (*a) { if (*s == *a) { found = 1; break; } a++; }
        if (!found) break;
        count++; s++;
    }
    return count;
}

size_t ucscspn(const char32_t *s, const char32_t *reject) {
    size_t count = 0;
    while (*s) {
        const char32_t *r = reject;
        while (*r) { if (*s == *r) return count; r++; }
        count++; s++;
    }
    return count;
}

char32_t *umemcpy(char32_t *dest, const char32_t *src, size_t n) {
    char32_t *d = dest; while (n--) *d++ = *src++;
    return dest;
}

char32_t *umemset(char32_t *dest, char32_t c, size_t n) {
    char32_t *d = dest; while (n--) *d++ = c;
    return dest;
}

char32_t *umemmove(char32_t *dest, const char32_t *src, size_t n) {
    char32_t *d = dest;
    if (d < src) { while (n--) *d++ = *src++; }
    else { d += n; src += n; while (n--) *--d = *--src; }
    return dest;
}

int umemcmp(const char32_t *a, const char32_t *b, size_t n) {
    while (n--) {
        if (*a != *b) { if (*a < *b) return -1; return 1; }
        a++; b++;
    }
    return 0;
}
