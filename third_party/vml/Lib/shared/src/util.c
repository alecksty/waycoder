#param lib("math")

// VML Shared Utility Library

__stdcall void delay(int ms) {
    asm("SYSCALL #52");
}

__stdcall int sscanf(const char* s, const char* fmt, void* ptr) {
    return 0;
}

// ⚠ 这里原本还有一份 `atoi` —— **已删**。全库曾有**三份** `atoi`：
//   `convert.c`（跳空白 + 认 +/-）、`util.c`（**不跳空白、不认 +、只认 -**）、
//   `builtins.c`（同 convert）。模块映射把 `atoi` 指向 `convert`，但 `util` 也被链进来，
//   而重名标签**按链接顺序后者覆盖**⇒ 实测生效的是 **util 那份最弱的**：
//       atoi("42")=42 ✓   atoi("+7")=0 ✗   atoi("   42")=0 ✗
//   ⇒ 按「相同函数只留一份」删掉本份与 builtins 那份，唯一实现留在 `convert.c`。

// Integer power: base^exp (exp >= 0)
__stdcall int int_pow(int base, int exp) {
    int result = 1;
    while (exp > 0) {
        if (exp & 1) result *= base;
        base *= base;
        exp >>= 1;
    }
    return result;
}

// Integer square root (floor)
__stdcall int int_sqrt(int n) {
    if (n <= 0) return 0;
    int x = n;
    int y = (x + 1) / 2;
    while (y < x) {
        x = y;
        y = (x + n / x) / 2;
    }
    return x;
}

// Sum of integer array
__stdcall int sum(int* arr, int n) {
    int total = 0;
    for (int i = 0; i < n; i++)
        total += arr[i];
    return total;
}
