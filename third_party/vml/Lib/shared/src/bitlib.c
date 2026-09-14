#param lib("builtins")

// VML Shared Bit Manipulation Library
// 位操作扩展 — MCU兼容

// set: 设置第 n 位为 1, 返回新值
__stdcall int set(int value, int n) {
    if (n < 0 || n > 31) return value;
    return value | (1 << n);
}

// clear: 清除第 n 位为 0, 返回新值
__stdcall int clear(int value, int n) {
    if (n < 0 || n > 31) return value;
    return value & ~(1 << n);
}

// toggle: 翻转第 n 位, 返回新值
__stdcall int toggle(int value, int n) {
    if (n < 0 || n > 31) return value;
    return value ^ (1 << n);
}

// test: 测试第 n 位是否为 1, 返回 1/0
__stdcall int test(int value, int n) {
    if (n < 0 || n > 31) return 0;
    return (value >> n) & 1;
}

// count: 统计二进制中 1 的个数 (popcount)
__stdcall int count(int value) {
    unsigned int u = (unsigned int)value;
    u = u - ((u >> 1) & 0x55555555);
    u = (u & 0x33333333) + ((u >> 2) & 0x33333333);
    u = (u + (u >> 4)) & 0x0F0F0F0F;
    u = u + (u >> 8);
    u = u + (u >> 16);
    return u & 0x3F;
}

// reverse: 反转所有32位 (bit 0↔31, 1↔30, ...)
__stdcall int reverse(int value) {
    unsigned int u = (unsigned int)value;
    u = ((u & 0xAAAAAAAA) >> 1) | ((u & 0x55555555) << 1);
    u = ((u & 0xCCCCCCCC) >> 2) | ((u & 0x33333333) << 2);
    u = ((u & 0xF0F0F0F0) >> 4) | ((u & 0x0F0F0F0F) << 4);
    u = ((u & 0xFF00FF00) >> 8) | ((u & 0x00FF00FF) << 8);
    return (int)((u >> 16) | (u << 16));
}

// rotate_left: 循环左移 n 位
__stdcall int rotate_left(int value, int n) {
    unsigned int u = (unsigned int)value;
    n = n & 31;
    return (int)((u << n) | (u >> (32 - n)));
}

// rotate_right: 循环右移 n 位
__stdcall int rotate_right(int value, int n) {
    unsigned int u = (unsigned int)value;
    n = n & 31;
    return (int)((u >> n) | (u << (32 - n)));
}

// lowest_set: 返回最低位1的位置 (0-31), 无1返回 -1
__stdcall int lowest_set(int value) {
    if (value == 0) return -1;
    int pos = 0;
    while ((value & 1) == 0) { value >>= 1; pos++; }
    return pos;
}

// highest_set: 返回最高位1的位置 (0-31), 无1返回 -1
__stdcall int highest_set(int value) {
    if (value == 0) return -1;
    int pos = 31;
    unsigned int u = (unsigned int)value;
    while ((u & 0x80000000) == 0) { u <<= 1; pos--; }
    return pos;
}

// mask: 生成 n 位全1掩码 (n=0→0, n=32→0xFFFFFFFF)
__stdcall int mask(int n) {
    if (n <= 0) return 0;
    if (n >= 32) return -1;
    return (1 << n) - 1;
}

// extract: 提取 value 的 [start, start+len-1] 位域
__stdcall int extract(int value, int start, int len) {
    if (start < 0 || len <= 0 || start + len > 32) return 0;
    return ((unsigned int)value >> start) & ((1u << len) - 1);
}

// insert: 将 field 插入 value 的 [start, start+len-1] 位域
__stdcall int insert(int value, int field, int start, int len) {
    if (start < 0 || len <= 0 || start + len > 32) return value;
    unsigned int mask = ((1u << len) - 1) << start;
    return (value & ~mask) | ((field & ((1u << len) - 1)) << start);
}

// is_power_of_two: 判断是否为2的幂 (1, 2, 4, 8, ...)
__stdcall int is_power_of_two(int x) {
    return (x > 0) && ((x & (x - 1)) == 0) ? 1 : 0;
}

// next_power_of_two: 向上取整到2的幂
__stdcall int next_power_of_two(int x) {
    if (x <= 0) return 1;
    unsigned int u = (unsigned int)x - 1;
    u |= u >> 1; u |= u >> 2; u |= u >> 4;
    u |= u >> 8; u |= u >> 16;
    return (int)(u + 1);
}
