// VML Shared Convert Library
// 数字↔字符串转换 — MCU兼容


// itoa: 整数→字符串, 返回字符串长度
__stdcall int itoa(int value, char* dst) {
    int neg = 0;
    char* start = dst;
    if (value < 0) { neg = 1; value = -value; }
    char buf[12];
    int pos = 0;
    if (value == 0) buf[pos++] = '0';
    while (value > 0) { buf[pos++] = '0' + (value % 10); value /= 10; }
    if (neg) buf[pos++] = '-';
    while (pos > 0) *dst++ = buf[--pos];
    *dst = 0;
    return (int)(dst - start);
}

// atoi: 字符串→整数 (跳过前导空白, 支持正负号)
__stdcall int atoi(const char* s) {
    int result = 0;
    int sign = 1;
    while (*s == ' ' || *s == '\t') s++;
    if (*s == '-') { sign = -1; s++; }
    else if (*s == '+') s++;
    while (*s >= '0' && *s <= '9') {
        result = result * 10 + (*s - '0');
        s++;
    }
    return sign * result;
}

// itoa_hex: 整数→十六进制字符串 (大写), 返回长度
__stdcall int itoa_hex(int value, char* dst) {
    char hex[] = "0123456789ABCDEF";
    char* start = dst;
    char buf[12];
    int pos = 0;
    unsigned int u = (unsigned int)value;
    if (u == 0) buf[pos++] = '0';
    while (u > 0) { buf[pos++] = hex[u & 0xF]; u >>= 4; }
    while (pos > 0) *dst++ = buf[--pos];
    *dst = 0;
    return (int)(dst - start);
}

// atoi_hex: 十六进制字符串→整数
__stdcall int atoi_hex(const char* s) {
    int result = 0;
    while (*s == ' ' || *s == '\t') s++;
    if (s[0] == '0' && (s[1] == 'x' || s[1] == 'X')) s += 2;
    for (;;) {
        char c = *s;
        if (c >= '0' && c <= '9') result = result * 16 + (c - '0');
        else if (c >= 'A' && c <= 'F') result = result * 16 + (c - 'A' + 10);
        else if (c >= 'a' && c <= 'f') result = result * 16 + (c - 'a' + 10);
        else break;
        s++;
    }
    return result;
}

// ftoa: 定点数→字符串 (value/scale), 返回长度
__stdcall int ftoa(int value, int scale, char* dst) {
    char* start = dst;
    int neg = 0;
    if (value < 0) { neg = 1; value = -value; }
    int int_part = value / scale;
    int frac_part = value % scale;
    if (neg) *dst++ = '-';
    if (int_part == 0) *dst++ = '0';
    else {
        char buf[12]; int p = 0;
        while (int_part > 0) { buf[p++] = '0' + (int_part % 10); int_part /= 10; }
        while (p > 0) *dst++ = buf[--p];
    }
    if (scale > 1 && frac_part > 0) {
        *dst++ = '.';
        int i, max_digits = 6;
        for (i = 0; i < max_digits && frac_part > 0; i++) {
            frac_part *= 10;
            *dst++ = '0' + (frac_part / scale);
            frac_part %= scale;
        }
    }
    *dst = 0;
    return (int)(dst - start);
}

// ===== 类型转换 (byte/hword/word 互转, MCU) =====
__stdcall int byte_to_hword(unsigned char b) { return (int)b; }
__stdcall int byte_to_word(unsigned char b) { return (int)b; }
__stdcall unsigned char hword_to_byte(int h) { return (unsigned char)(h & 0xFF); }
__stdcall int hword_to_word(int h) { return h & 0xFFFF; }
__stdcall unsigned char word_to_byte(int w) { return (unsigned char)(w & 0xFF); }
__stdcall int word_to_hword(int w) { return w & 0xFFFF; }
