/* C标准库 — 用C语言本身实现
 * 编译器: CCompiler → VML
 * 所有函数使用C语言形式调用约定:
 *   参数从右向左压栈，调用者清理栈
 *   返回值在 R0
 */

/* ---------- I/O 函数 ---------- */

int putchar(int c) {
    /* SYSCALL #4: OutputChar */
    asm("MOVE R0, c");
    asm("SYSCALL #4");
    return c;
}

int puts(const char* s) {
    /* SYSCALL #1: OutputString */
    asm("MOVE R0, s");
    asm("SYSCALL #1");
    asm("MOVE R0, #10");
    asm("SYSCALL #4");  /* 换行 */
    return 1;
}

int getchar(void) {
    /* SYSCALL #5: InputChar */
    int c;
    asm("SYSCALL #5");
    asm("MOVE c, R0");
    return c;
}

/* ---------- 内存操作 ---------- */

void* memset(void* ptr, int value, int num) {
    char* p = (char*)ptr;
    while (num > 0) {
        *p = (char)value;
        p++;
        num--;
    }
    return ptr;
}

void* memcpy(void* dest, const void* src, int num) {
    char* d = (char*)dest;
    const char* s = (const char*)src;
    while (num > 0) {
        *d = *s;
        d++;
        s++;
        num--;
    }
    return dest;
}

int memcmp(const void* ptr1, const void* ptr2, int num) {
    const char* p1 = (const char*)ptr1;
    const char* p2 = (const char*)ptr2;
    while (num > 0) {
        if (*p1 != *p2) return *p1 - *p2;
        p1++; p2++; num--;
    }
    return 0;
}

/* ---------- 字符串操作 ---------- */

int strlen(const char* s) {
    int len = 0;
    while (s[len] != 0) len++;
    return len;
}

char* strcpy(char* dest, const char* src) {
    char* d = dest;
    while (*src != 0) { *d = *src; d++; src++; }
    *d = 0;
    return dest;
}

char* strcat(char* dest, const char* src) {
    char* d = dest + strlen(dest);
    while (*src != 0) { *d = *src; d++; src++; }
    *d = 0;
    return dest;
}

int strcmp(const char* s1, const char* s2) {
    while (*s1 != 0 && *s2 != 0 && *s1 == *s2) { s1++; s2++; }
    return *s1 - *s2;
}

/* ---------- 数学函数 ---------- */

int abs(int x) {
    return x < 0 ? -x : x;
}

int min(int a, int b) {
    return a < b ? a : b;
}

int max(int a, int b) {
    return a > b ? a : b;
}

/* ---------- 工具函数 ---------- */

void exit(int code) {
    asm("SYSCALL #3");
}

int atoi(const char* s) {
    int result = 0;
    int sign = 1;
    while (*s == ' ') s++;
    if (*s == '-') { sign = -1; s++; }
    else if (*s == '+') s++;
    while (*s >= '0' && *s <= '9') {
        result = result * 10 + (*s - '0');
        s++;
    }
    return result * sign;
}
