#param lib("math64")

// VML Shared BitOps64 Library — 64-bit Bit Manipulation
// Uses long instead of int for 64-bit width operations

// ===== 基础位运算 (bitops 64位版) =====

__stdcall long lbit_and64(long a, long b) { return a & b; }
__stdcall long lbit_or64(long a, long b)  { return a | b; }
__stdcall long lbit_xor64(long a, long b) { return a ^ b; }
__stdcall long lbit_not64(long a)         { return ~a; }

__stdcall long lbit_shl64(long a, long bits) {
    if (bits <= 0) return a;
    if (bits >= 64) return 0;
    return a << bits;
}

__stdcall long lbit_shr64(long a, long bits) {
    if (bits <= 0) return a;
    if (bits >= 64) return 0;
    unsigned long ua = (unsigned long)a;
    return (long)(ua >> bits);
}

// lrol64 — 64位循环左移
__stdcall long lrol64(long val, long bits) {
    unsigned long uval = (unsigned long)val;
    bits = bits & 63;
    return (long)((uval << bits) | (uval >> (64 - bits)));
}

// lror64 — 64位循环右移
__stdcall long lror64(long val, long bits) {
    unsigned long uval = (unsigned long)val;
    bits = bits & 63;
    return (long)((uval >> bits) | (uval << (64 - bits)));
}

// lbit_set64 — 置位
__stdcall long lbit_set64(long val, long bit) {
    if (bit < 0 || bit >= 64) return val;
    return val | (1L << bit);
}

// lbit_clear64 — 清零
__stdcall long lbit_clear64(long val, long bit) {
    if (bit < 0 || bit >= 64) return val;
    return val & ~(1L << bit);
}

// lbit_toggle64 — 翻转
__stdcall long lbit_toggle64(long val, long bit) {
    if (bit < 0 || bit >= 64) return val;
    return val ^ (1L << bit);
}

// lbit_test64 — 测试位
__stdcall long lbit_test64(long val, long bit) {
    if (bit < 0 || bit >= 64) return 0;
    return (val >> bit) & 1;
}

// ===== 高级位操作 (bitlib 64位版) =====

// lcount64 — 统计1位个数
__stdcall long lcount64(long value) {
    unsigned long v = (unsigned long)value;
    v = v - ((v >> 1) & 0x5555555555555555UL);
    v = (v & 0x3333333333333333UL) + ((v >> 2) & 0x3333333333333333UL);
    v = (v + (v >> 4)) & 0x0F0F0F0F0F0F0F0FUL;
    v = (v * 0x0101010101010101UL) >> 56;
    return (long)v;
}

// lreverse64 — 64位位反转
__stdcall long lreverse64(long value) {
    unsigned long v = (unsigned long)value;
    v = ((v & 0xAAAAAAAAAAAAAAAAUL) >> 1) | ((v & 0x5555555555555555UL) << 1);
    v = ((v & 0xCCCCCCCCCCCCCCCCUL) >> 2) | ((v & 0x3333333333333333UL) << 2);
    v = ((v & 0xF0F0F0F0F0F0F0F0UL) >> 4) | ((v & 0x0F0F0F0F0F0F0F0FUL) << 4);
    v = ((v & 0xFF00FF00FF00FF00UL) >> 8) | ((v & 0x00FF00FF00FF00FFUL) << 8);
    v = ((v & 0xFFFF0000FFFF0000UL) >> 16) | ((v & 0x0000FFFF0000FFFFUL) << 16);
    v = (v >> 32) | (v << 32);
    return (long)v;
}

// lrotate_left64 — 循环左移 (兼容 bitlib 命名)
__stdcall long lrotate_left64(long value, long n) {
    return lrol64(value, n);
}

// lrotate_right64 — 循环右移
__stdcall long lrotate_right64(long value, long n) {
    return lror64(value, n);
}

// llowest_set64 — 最低置位位置 (0-63)
__stdcall long llowest_set64(long value) {
    if (value == 0) return -1;
    return lctz64(value);
}

// lhighest_set64 — 最高置位位置 (0-63)
__stdcall long lhighest_set64(long value) {
    if (value == 0) return -1;
    unsigned long v = (unsigned long)value;
    long pos = 63;
    unsigned long mask = 1UL << 63;
    while (mask && !(v & mask)) { pos--; mask >>= 1; }
    return pos;
}

// lmask64 — 生成 n 位掩码 (n <= 64)
__stdcall long lmask64(long n) {
    if (n <= 0) return 0;
    if (n >= 64) return -1;
    return (1L << n) - 1;
}

// lextract64 — 提取位域
__stdcall long lextract64(long value, long start, long len) {
    if (start < 0 || len <= 0 || start + len > 64) return 0;
    return (value >> start) & lmask64(len);
}

// linsert64 — 插入位域
__stdcall long linsert64(long value, long field, long start, long len) {
    if (start < 0 || len <= 0 || start + len > 64) return value;
    long mask = lmask64(len) << start;
    return (value & ~mask) | ((field & lmask64(len)) << start);
}

// lis_power_of_two64 — 2的幂检测
__stdcall long lis_power_of_two64(long x) {
    return x > 0 && (x & (x - 1)) == 0;
}

// lnext_power_of_two64 — 下一个2的幂
__stdcall long lnext_power_of_two64(long x) {
    if (x <= 1) return 1;
    unsigned long v = (unsigned long)(x - 1);
    v |= v >> 1; v |= v >> 2; v |= v >> 4;
    v |= v >> 8; v |= v >> 16; v |= v >> 32;
    return (long)(v + 1);
}
