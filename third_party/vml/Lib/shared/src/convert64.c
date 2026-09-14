// VML Shared Convert64 Library — 64-bit Numeric Conversion

// ltoa(value, dst) — 64位有符号整数→字符串
__stdcall int ltoa(long value, char* dst) {
    int i = 0;
    int j;
    int is_neg = 0;
    if (value < 0) { is_neg = 1; value = -value; }
    if (value == 0) { dst[0] = '0'; dst[1] = 0; return 1; }
    while (value > 0) {
        dst[i++] = '0' + (char)(value % 10);
        value /= 10;
    }
    if (is_neg) dst[i++] = '-';
    dst[i] = 0;
    // reverse
    for (j = 0; j < i / 2; j++) {
        char t = dst[j]; dst[j] = dst[i - 1 - j]; dst[i - 1 - j] = t;
    }
    return i;
}

// atol(s) — 字符串→64位长整数
__stdcall long atol(const char* s) {
    long result = 0;
    int sign = 1;
    int i = 0;
    while (s[i] == ' ' || s[i] == '\t') i++;
    if (s[i] == '-') { sign = -1; i++; }
    else if (s[i] == '+') i++;
    while (s[i] >= '0' && s[i] <= '9') {
        result = result * 10 + (long)(s[i] - '0');
        i++;
    }
    return sign * result;
}

// ltoa_hex(value, dst) — 64位整数→十六进制字符串
__stdcall int ltoa_hex(long value, char* dst) {
    int i = 0;
    int j;
    int started = 0;
    unsigned long uval = (unsigned long)value;
    int shift;
    for (shift = 60; shift >= 0; shift -= 4) {
        int nibble = (int)((uval >> shift) & 0xF);
        if (nibble > 0 || started || shift == 0) {
            dst[i++] = (char)(nibble < 10 ? '0' + nibble : 'A' + nibble - 10);
            started = 1;
        }
    }
    dst[i] = 0;
    return i;
}

// atol_hex(s) — 十六进制字符串→64位长整数
__stdcall long atol_hex(const char* s) {
    long result = 0;
    int i = 0;
    while (s[i] == ' ' || s[i] == '\t') i++;
    if (s[i] == '0' && (s[i+1] == 'x' || s[i+1] == 'X')) i += 2;
    while (1) {
        char c = s[i];
        if (c >= '0' && c <= '9') result = result * 16 + (long)(c - '0');
        else if (c >= 'a' && c <= 'f') result = result * 16 + (long)(c - 'a' + 10);
        else if (c >= 'A' && c <= 'F') result = result * 16 + (long)(c - 'A' + 10);
        else break;
        i++;
    }
    return result;
}

// dtoa(value, precision, dst) — 双精度浮点→字符串 (precision=小数位数)
__stdcall int dtoa(double value, int precision, char* dst) {
    int i = 0;
    int j;
    if (value < 0) { dst[i++] = '-'; value = -value; }
    long int_part = (long)value;
    double frac = value - (double)int_part;
    // integer part
    int istart = i;
    if (int_part == 0) { dst[i++] = '0'; }
    else {
        int tmp_i = i;
        while (int_part > 0) { dst[i++] = '0' + (char)(int_part % 10); int_part /= 10; }
        // reverse
        int end = i - 1;
        while (tmp_i < end) { char t = dst[tmp_i]; dst[tmp_i] = dst[end]; dst[end] = t; tmp_i++; end--; }
    }
    // fraction part
    if (precision > 0) {
        dst[i++] = '.';
        int p;
        for (p = 0; p < precision && p < 15; p++) {
            frac *= 10.0;
            int digit = (int)frac;
            dst[i++] = '0' + (char)digit;
            frac -= (double)digit;
        }
    }
    dst[i] = 0;
    return i;
}
