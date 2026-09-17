#param lib("shared")
#param lib("io")
#param lib("memory")
#param lib("sysinfo")
#param lib("vmlsys")
#param lib("syscall")
#param lib("console")
#param lib("convert")
#param lib("string")
#param lib("math")
#param lib("printf")
#param lib("ctype")
#param lib("bitops")
#param lib("util")
#param lib("float")
#param lib("file")
#param lib("os")

// VML Shared Built-in Library
// v1.66.51+: 薄包装层 — 函数实现已搬迁到 io.c/syscall.c/vmlsys.c/console.c
// builtins.c 包含核心内置函数实现

// ===== 内存访问 =====

__stdcall int peek(int addr) { int v; asm("MOVE @R0 R0"); return v; }
__stdcall void poke(int val, int addr) { asm("MOVE @R1 R0"); }
__stdcall int peekb(int addr) { int v; asm("MOVEB @R0 R0"); return v; }
__stdcall void pokeb(int val, int addr) { asm("MOVEB @R1 R0"); }

// ===== 控制台 I/O =====

__stdcall void putchar(char ch) { asm("SYSCALL #4"); }
__stdcall void print_str(const char* str) { asm("SYSCALL #1"); }
__stdcall void print_int(int val) { asm("SYSCALL #6"); }
__stdcall void print_hex(int val) { asm("SYSCALL #10"); }
__stdcall void println_str(const char* str) {
    asm("SYSCALL #1");
    asm("MOVE R0 #10");
    asm("SYSCALL #4");
}
__stdcall void println_int(int val) {
    asm("SYSCALL #6");
    asm("MOVE R0 #10");
    asm("SYSCALL #4");
}

__stdcall void newline(void) { asm("MOVE R0 #10"); asm("SYSCALL #4"); }
__stdcall void print_float(float f) { asm("SYSCALL 8"); }

__stdcall void print_bool(int b) {
    if (b) { print_str("true"); }
    else { print_str("false"); }
}

// ===== 数学 (已迁移到 math.c, 此处保留别名) =====

__stdcall int abs(int x) { return x < 0 ? -x : x; }
__stdcall int min(int a, int b) { return a < b ? a : b; }
__stdcall int max(int a, int b) { return a > b ? a : b; }

// ===== 系统调用 (委托 vmlsys.c) =====

__stdcall int random(void) { int r; asm("SYSCALL #50"); return r; }
__stdcall void sleep(int ms) { asm("SYSCALL #52"); }
__stdcall int get_tick(void) { int t; asm("SYSCALL #53"); return t; }

// ===== 内存管理 =====

void* alloc(int size) { void* p; asm("SYSCALL #40"); return p; }
__stdcall void free(void* ptr) { asm("SYSCALL #41"); }

// ===== 调试 =====

__stdcall void debug_print(const char* msg) { asm("SYSCALL #70"); }
__stdcall void debug_print_int(int val) { asm("SYSCALL #71"); }
__stdcall void assert(int cond, const char* msg) { asm("SYSCALL #72"); }

// ===== MCU 数学/工具 =====

__stdcall int clamp(int x, int low, int high) {
    if (x < low) return low; if (x > high) return high; return x;
}
__stdcall int sign(int x) { if (x < 0) return -1; if (x > 0) return 1; return 0; }
__stdcall int is_power_of_two(int x) { return (x > 0) && ((x & (x - 1)) == 0) ? 1 : 0; }
__stdcall int next_power_of_two(int x) {
    if (x <= 0) return 1; x--; x |= x >> 1; x |= x >> 2; x |= x >> 4; x |= x >> 8; x |= x >> 16; return x + 1;
}

// ===== 位操作 =====

__stdcall int bit_set(int val, int n) { return val | (1 << n); }
__stdcall int bit_clear(int val, int n) { return val & ~(1 << n); }
__stdcall int bit_toggle(int val, int n) { return val ^ (1 << n); }
__stdcall int bit_test(int val, int n) { return (val >> n) & 1; }
__stdcall int rol(int val, int bits) { bits &= 31; return (val << bits) | (val >> (32 - bits)); }
__stdcall int ror(int val, int bits) { bits &= 31; return (val >> bits) | (val << (32 - bits)); }
__stdcall int popcount(int val) { int n=0; while(val){ n++; val&=val-1; } return n; }
__stdcall int clz(int val) { int n=32; while(val){ val>>=1; n--; } return n; }

// ===== 数论 =====

__stdcall int gcd(int a, int b) { int t; while (b != 0) { t = b; b = a % b; a = t; } return a; }
__stdcall int lcm(int a, int b) { int g = gcd(a, b); if (g == 0) return 0; return (a / g) * b; }
__stdcall int factorial(int n) { int r = 1; if (n < 0) return 0; while (n > 1) { r *= n; n--; } return r; }
__stdcall int is_prime(int n) { int i; if (n < 2) return 0; for (i = 2; i * i <= n; i++) if (n % i == 0) return 0; return 1; }

// ===== 字符串 (精简版, 完整版在 string.c) =====

__stdcall char* strcpy(char* dst, const char* src) {
    int i = 0; if (!dst || !src) return dst;
    while (src[i]) { dst[i] = src[i]; i++; } dst[i] = 0; return dst;
}
__stdcall char* strcat(char* dst, const char* src) {
    int i = 0, j = 0; if (!dst || !src) return dst;
    while (dst[i]) i++; while (src[j]) { dst[i++] = src[j++]; } dst[i] = 0; return dst;
}
__stdcall int strlen(const char* s) { int n = 0; if (!s) return 0; while (s[n]) n++; return n; }

// ===== 转换 =====

// ⚠ `atoi` 的原生实现在 `convert.c`（模块映射 `["atoi"] = "convert"`）。
//   这里与 `util.c` 原先各有一份重名实现，而重名标签**按链接顺序后者覆盖**
//   ⇒ 实测生效的是 `util.c` 那份最弱的（不跳空白、不认 `+`）。
//   已按「相同函数只留一份」删掉两处冗余，唯一实现留在 `convert.c`。
// (itoa 同样由 convert.c 提供 2 参数版本: int itoa(int value, char* dst))

// ===== 内存操作 =====

__stdcall void* memcpy(void* dst, const void* src, int n) {
    char* d = (char*)dst; const char* s = (const char*)src; int i;
    for (i = 0; i < n; i++) d[i] = s[i]; return dst;
}
__stdcall void* memset(void* ptr, int val, int n) {
    char* p = (char*)ptr; int i; for (i = 0; i < n; i++) p[i] = (char)val; return ptr;
}
__stdcall int memcmp(const void* a, const void* b, int n) {
    const char* pa = (const char*)a; const char* pb = (const char*)b; int i;
    for (i = 0; i < n; i++) { if (pa[i] != pb[i]) return (int)(pa[i]) - (int)(pb[i]); } return 0;
}

// ===== 延时/随机工具 =====

__stdcall void delay_us(int us) { int i; for (i = 0; i < us * 10; i++) { } }
__stdcall int rand_range(int min, int max) {
    int r; if (max <= min) return min; asm("SYSCALL #50"); r = r % (max - min + 1); return min + r;
}
