#param lib("fixed")
#param lib("math")

// VML Cross-Language Shared Library
// 各语言通过 native/alias 机制调用，无需前缀

// ===== 数学函数 =====

__stdcall int add(int a, int b) { return a + b; }
__stdcall int sub(int a, int b) { return a - b; }
__stdcall int mul(int a, int b) { return a * b; }
__stdcall int div_(int a, int b) { return b != 0 ? a / b : 0; }
__stdcall int mod_(int a, int b) { return b != 0 ? a % b : 0; }
__stdcall int neg_(int x) { return -x; }

// ===== 数组操作 =====

__stdcall int arr_sum(int* arr) {
    if (arr == 0) return 0;
    int i, s = 0;
    for (i = 0; i < arr[0]; i++) s += arr[i + 1];
    return s;
}

__stdcall int arr_avg(int* arr) {
    int n = arr ? arr[0] : 0;
    return n > 0 ? arr_sum(arr) / n : 0;
}

__stdcall int arr_max(int* arr) {
    if (arr == 0 || arr[0] <= 0) return 0;
    int i, m = arr[1];
    for (i = 1; i < arr[0]; i++)
        if (arr[i + 1] > m) m = arr[i + 1];
    return m;
}

__stdcall int arr_min(int* arr) {
    if (arr == 0 || arr[0] <= 0) return 0;
    int i, m = arr[1];
    for (i = 1; i < arr[0]; i++)
        if (arr[i + 1] < m) m = arr[i + 1];
    return m;
}

// ===== 字符串操作 =====

__stdcall int str_len(const char* s) {
    int n = 0;
    while (s[n]) n++;
    return n;
}

__stdcall int str_cmp(const char* a, const char* b) {
    while (*a && *a == *b) { a++; b++; }
    return *a - *b;
}

// ===== 通用工具 =====

__stdcall int clamp(int x, int low, int high) {
    if (x < low) return low;
    if (x > high) return high;
    return x;
}

__stdcall int is_even(int x) { return (x & 1) == 0; }
__stdcall int is_odd(int x)  { return (x & 1) == 1; }
