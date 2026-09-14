// VML Shared Bit Operations Library
// 编译: vmltool compile bitops.c -o ../bitops.vml --lang c

// bit_and(a, b) -> a & b
__stdcall int bit_and(int a, int b) {
    return a & b;
}

// bit_or(a, b) -> a | b
__stdcall int bit_or(int a, int b) {
    return a | b;
}

// bit_xor(a, b) -> a ^ b
__stdcall int bit_xor(int a, int b) {
    return a ^ b;
}

// bit_not(a) -> ~a
__stdcall int bit_not(int a) {
    return ~a;
}

// bit_shl(a, bits) -> a << bits
__stdcall int bit_shl(int a, int bits) {
    return a << bits;
}

// bit_shr(a, bits) -> a >> bits (算术右移)
__stdcall int bit_shr(int a, int bits) {
    return a >> bits;
}

// rol(val, bits) -> 循环左移
__stdcall int rol(int val, int bits) {
    return (val << bits) | ((val >> (32 - bits)));
}

// ror(val, bits) -> 循环右移
__stdcall int ror(int val, int bits) {
    return ((val >> bits)) | (val << (32 - bits));
}

// bit_set(val, bit) -> 设置某位
__stdcall int bit_set(int val, int bit) {
    return val | (1 << bit);
}

// bit_clear(val, bit) -> 清除某位
__stdcall int bit_clear(int val, int bit) {
    return val & ~(1 << bit);
}

// bit_toggle(val, bit) -> 翻转某位
__stdcall int bit_toggle(int val, int bit) {
    return val ^ (1 << bit);
}

// bit_test(val, bit) -> 测试某位
__stdcall int bit_test(int val, int bit) {
    return (val >> bit) & 1;
}
