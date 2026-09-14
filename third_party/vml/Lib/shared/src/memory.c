// VML Shared Memory Library

__stdcall void* memcpy(void* dst, const void* src, int n) {
    char* d = (char*)dst;
    const char* s = (const char*)src;
    for (int i = 0; i < n; i++) d[i] = s[i];
    return dst;
}

__stdcall void* memset(void* ptr, int val, int n) {
    char* p = (char*)ptr;
    for (int i = 0; i < n; i++) p[i] = (char)val;
    return ptr;
}

__stdcall void* memmove(void* dst, const void* src, int n) {
    char* d = (char*)dst;
    const char* s = (const char*)src;
    if (d < s) {
        for (int i = 0; i < n; i++) d[i] = s[i];
    } else {
        for (int i = n - 1; i >= 0; i--) d[i] = s[i];
    }
    return dst;
}

__stdcall int memcmp(const void* a, const void* b, int n) {
    const char* pa = (const char*)a;
    const char* pb = (const char*)b;
    for (int i = 0; i < n; i++) {
        if (pa[i] != pb[i]) return pa[i] - pb[i];
    }
    return 0;
}
