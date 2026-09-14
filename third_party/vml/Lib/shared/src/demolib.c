// demolib — 独立测试库 (验证 GenLib 全流水线)
// 跨 22 语言调用的 8 个简单函数

__stdcall int demolib_add(int a, int b) { return a + b; }
__stdcall int demolib_sub(int a, int b) { return a - b; }
__stdcall int demolib_mul(int a, int b) { return a * b; }
__stdcall int demolib_div(int a, int b) { if (b == 0) return 0; return a / b; }

__stdcall int demolib_fact(int n) {
    if (n <= 1) return 1;
    return n * demolib_fact(n - 1);
}

__stdcall int demolib_fib(int n) {
    if (n <= 0) return 0;
    if (n == 1) return 1;
    return demolib_fib(n - 1) + demolib_fib(n - 2);
}

__stdcall int demolib_square(int x) { return x * x; }
__stdcall int demolib_negate(int x) { return -x; }
