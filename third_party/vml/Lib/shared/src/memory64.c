// VML Shared Memory64 Library — 64-bit Memory Operations
// Uses long (64-bit) for size/count parameters instead of int (32-bit)

// lmemcpy — 64位内存复制
__stdcall void* lmemcpy(void* dst, const void* src, long n) {
    char* d = (char*)dst;
    const char* s = (const char*)src;
    long i;
    if (d < s || d >= s + n) {
        for (i = 0; i < n; i++) d[i] = s[i];
    } else {
        for (i = n - 1; i >= 0; i--) d[i] = s[i];
    }
    return dst;
}

// lmemset — 64位内存填充
__stdcall void* lmemset(void* ptr, int val, long n) {
    char* p = (char*)ptr;
    long i;
    for (i = 0; i < n; i++) p[i] = (char)val;
    return ptr;
}

// lmemmove — 64位内存移动 (同 lmemcpy, 处理重叠)
__stdcall void* lmemmove(void* dst, const void* src, long n) {
    return lmemcpy(dst, src, n);
}

// lmemcmp — 64位内存比较
__stdcall int lmemcmp(const void* a, const void* b, long n) {
    const unsigned char* pa = (const unsigned char*)a;
    const unsigned char* pb = (const unsigned char*)b;
    long i;
    for (i = 0; i < n; i++) {
        if (pa[i] != pb[i]) return (int)pa[i] - (int)pb[i];
    }
    return 0;
}
