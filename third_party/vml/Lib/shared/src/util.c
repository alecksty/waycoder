#param lib("math")

// VML Shared Utility Library

__stdcall void delay(int ms) {
    asm("SYSCALL #52");
}

__stdcall int sscanf(const char* s, const char* fmt, void* ptr) {
    return 0;
}

__stdcall int atoi(const char* s) {
    int r = 0;
    int sign = 1;
    int i = 0;
    if (s[i] == '-') { sign = -1; i++; }
    while (s[i] >= '0' && s[i] <= '9') {
        r = r * 10 + (s[i] - '0');
        i++;
    }
    return r * sign;
}

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
