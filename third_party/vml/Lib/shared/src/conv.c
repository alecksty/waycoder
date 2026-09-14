/*
 * conv.c — VML 全类型转换共享库 (v1.66.44)
 * 基本类型 ↔ 字符串 互转
 *
 * 支持类型: bool, char, byte, sbyte, short, ushort, int, uint,
 *           long, ulong, float, double ↔ string, wstring, ustring
 *
 * 编译: vmltool conv.c -o conv.vml --no-link
 * 依赖: convert.vml (itoa/atoi/ftoa), convert64.vml (ltoa/atol/dtoa)
 *       wchar.vml (宽字符), uchar.vml (Unicode字符)
 */

/* ---- 静态缓冲区 (每个线程一个，非线程安全) ---- */
static char _buf[128];
static wchar_t _wbuf[128];
static char32_t _ubuf[128];

/* ================================================================
 * 1. 整数 → 字符串 (int32)
 * ================================================================ */

/* itoa — builtins.c 中定义，conv.c 仅声明引用 */
extern int itoa(int value, char* dst);

/* 便捷包装: 返回静态缓冲区 */
__stdcall const char* int_to_str(int val) {
    itoa(val, _buf);
    return _buf;
}

__stdcall const wchar_t* int_to_wstr(int val) {
    itoa(val, _buf);
    for (int i = 0; _buf[i]; i++) _wbuf[i] = _buf[i];
    _wbuf[strlen(_buf)] = 0;
    return _wbuf;
}

__stdcall const char32_t* int_to_ustr(int val) {
    itoa(val, _buf);
    for (int i = 0; _buf[i]; i++) _ubuf[i] = _buf[i];
    _ubuf[strlen(_buf)] = 0;
    return _ubuf;
}

/* ================================================================
 * 2. 字符串 → 整数 (int32)
 * ================================================================ */

int atoi(const char* s);
__stdcall int str_to_int(const char* s) { return atoi(s); }
__stdcall int wstr_to_int(const wchar_t* s) {
    char tmp[64]; int i = 0; while (s[i] && i < 63) { tmp[i] = (char)s[i]; i++; } tmp[i] = 0;
    return atoi(tmp);
}
__stdcall int ustr_to_int(const char32_t* s) {
    char tmp[64]; int i = 0; while (s[i] && i < 63) { tmp[i] = (char)s[i]; i++; } tmp[i] = 0;
    return atoi(tmp);
}

/* ================================================================
 * 3. 整数 → 字符串 (int64 / long)
 * ================================================================ */

int ltoa(long value, char* dst);
__stdcall int long_to_strbuf(long val, char* dst) { return ltoa(val, dst); }

__stdcall const char* long_to_str(long val) {
    ltoa(val, _buf);
    return _buf;
}

__stdcall const wchar_t* long_to_wstr(long val) {
    ltoa(val, _buf);
    for (int i = 0; _buf[i]; i++) _wbuf[i] = _buf[i];
    _wbuf[strlen(_buf)] = 0;
    return _wbuf;
}

__stdcall const char32_t* long_to_ustr(long val) {
    ltoa(val, _buf);
    for (int i = 0; _buf[i]; i++) _ubuf[i] = _buf[i];
    _ubuf[strlen(_buf)] = 0;
    return _ubuf;
}

/* ================================================================
 * 4. 字符串 → 长整数 (int64)
 * ================================================================ */

long atol(const char* s);
__stdcall long str_to_long(const char* s) { return atol(s); }
__stdcall long wstr_to_long(const wchar_t* s) {
    char tmp[64]; int i = 0; while (s[i] && i < 63) { tmp[i] = (char)s[i]; i++; } tmp[i] = 0;
    return atol(tmp);
}
__stdcall long ustr_to_long(const char32_t* s) {
    char tmp[64]; int i = 0; while (s[i] && i < 63) { tmp[i] = (char)s[i]; i++; } tmp[i] = 0;
    return atol(tmp);
}

/* ================================================================
 * 5. 无符号整数 → 字符串 (uint32)
 * ================================================================ */

__stdcall const char* uint_to_str(unsigned int val) {
    if (val <= 2147483647) { itoa((int)val, _buf); return _buf; }
    // 超过有符号范围: 手动转换
    int pos = 0;
    unsigned int v = val;
    do { _buf[pos++] = '0' + (v % 10); v /= 10; } while (v > 0);
    _buf[pos] = 0;
    // 反转
    for (int i = 0; i < pos / 2; i++) { char t = _buf[i]; _buf[i] = _buf[pos-1-i]; _buf[pos-1-i] = t; }
    return _buf;
}

__stdcall const char* ulong_to_str(unsigned long val) {
    if (val <= 9223372036854775807UL) { ltoa((long)val, _buf); return _buf; }
    int pos = 0;
    unsigned long v = val;
    do { _buf[pos++] = '0' + (v % 10); v /= 10; } while (v > 0);
    _buf[pos] = 0;
    for (int i = 0; i < pos / 2; i++) { char t = _buf[i]; _buf[i] = _buf[pos-1-i]; _buf[pos-1-i] = t; }
    return _buf;
}

/* ================================================================
 * 6. 字符串 → 无符号整数
 * ================================================================ */

__stdcall unsigned int str_to_uint(const char* s) { return (unsigned int)atoi(s); }
__stdcall unsigned long str_to_ulong(const char* s) { return (unsigned long)atol(s); }

/* ================================================================
 * 7. 布尔 → 字符串
 * ================================================================ */

__stdcall const char* bool_to_str(int b) {
    if (b) { _buf[0]='t'; _buf[1]='r'; _buf[2]='u'; _buf[3]='e'; _buf[4]=0; }
    else   { _buf[0]='f'; _buf[1]='a'; _buf[2]='l'; _buf[3]='s'; _buf[4]='e'; _buf[5]=0; }
    return _buf;
}

__stdcall int str_to_bool(const char* s) {
    if (!s || !s[0]) return 0;
    if (s[0]=='t' || s[0]=='T' || s[0]=='1') return 1;
    return 0;
}

/* ================================================================
 * 8. 字符 → 字符串
 * ================================================================ */

__stdcall const char* char_to_str(char c) { _buf[0]=c; _buf[1]=0; return _buf; }
__stdcall char str_to_char(const char* s) { return s && s[0] ? s[0] : 0; }

/* ================================================================
 * 9. byte/sbyte → 字符串 (8-bit)
 * ================================================================ */

__stdcall const char* byte_to_str(unsigned char b) { return int_to_str((int)b); }
__stdcall unsigned char str_to_byte(const char* s) { return (unsigned char)atoi(s); }

__stdcall const char* sbyte_to_str(signed char b) { return int_to_str((int)b); }
__stdcall signed char str_to_sbyte(const char* s) { return (signed char)atoi(s); }

/* ================================================================
 * 10. short/ushort → 字符串 (16-bit)
 * ================================================================ */

__stdcall const char* short_to_str(short s) { return int_to_str((int)s); }
__stdcall short str_to_short(const char* s) { return (short)atoi(s); }

__stdcall const char* ushort_to_str(unsigned short s) { return int_to_str((int)s); }
__stdcall unsigned short str_to_ushort(const char* s) { return (unsigned short)atoi(s); }

/* ================================================================
 * 11. 浮点 (float32) → 字符串
 * 使用 ftoa 定点数, scale=1000000 (6位小数)
 * ================================================================ */

int ftoa(int value, int scale, char* dst);
int strlen(const char* s);
void memcpy(char* dst, const char* src, int len);

__stdcall const char* float_to_str(float f) {
    if (f < 0) {
        _buf[0] = '-'; f = -f;
        int val = (int)(f * 1000000.0f + 0.5f);
        ftoa(val, 1000000, _buf + 1);
    } else {
        int val = (int)(f * 1000000.0f + 0.5f);
        ftoa(val, 1000000, _buf);
    }
    return _buf;
}

/* 简易 atof: 解析 "3.14" → float */
__stdcall float str_to_float(const char* s) {
    if (!s || !s[0]) return 0.0f;
    int sign = 1;
    if (s[0] == '-') { sign = -1; s++; }
    float result = 0.0f;
    while (*s >= '0' && *s <= '9') { result = result * 10.0f + (float)(*s - '0'); s++; }
    if (*s == '.') {
        s++;
        float frac = 0.1f;
        while (*s >= '0' && *s <= '9') { result += frac * (float)(*s - '0'); frac *= 0.1f; s++; }
    }
    return sign * result;
}

/* ================================================================
 * 12. 双精度 (float64) → 字符串
 * ================================================================ */

int dtoa(double value, int precision, char* dst);

__stdcall const char* double_to_str(double d) {
    dtoa(d, 9, _buf);
    return _buf;
}

/* 简易 atod: 解析 "3.14159" → double */
__stdcall double str_to_double(const char* s) {
    if (!s || !s[0]) return 0.0;
    int sign = 1;
    if (s[0] == '-') { sign = -1; s++; }
    double result = 0.0;
    while (*s >= '0' && *s <= '9') { result = result * 10.0 + (double)(*s - '0'); s++; }
    if (*s == '.') {
        s++;
        double frac = 0.1;
        while (*s >= '0' && *s <= '9') { result += frac * (double)(*s - '0'); frac *= 0.1; s++; }
    }
    return sign * result;
}

/* ================================================================
 * 13. Wide String / Unicode String 包装器 (委托给普通字符串函数)
 * ================================================================ */

/* wstring 转换 — 先转为 char 再处理 */
__stdcall const wchar_t* float_to_wstr(float f) {
    float_to_str(f);
    int len = strlen(_buf);
    for (int i = 0; i <= len; i++) _wbuf[i] = _buf[i];
    return _wbuf;
}

__stdcall const wchar_t* double_to_wstr(double d) {
    double_to_str(d);
    int len = strlen(_buf);
    for (int i = 0; i <= len; i++) _wbuf[i] = _buf[i];
    return _wbuf;
}

__stdcall const wchar_t* bool_to_wstr(int b) {
    bool_to_str(b);
    int len = strlen(_buf);
    for (int i = 0; i <= len; i++) _wbuf[i] = _buf[i];
    return _wbuf;
}

/* ustring 转换 */
__stdcall const char32_t* float_to_ustr(float f) {
    float_to_str(f);
    int len = strlen(_buf);
    for (int i = 0; i <= len; i++) _ubuf[i] = _buf[i];
    return _ubuf;
}

__stdcall const char32_t* double_to_ustr(double d) {
    double_to_str(d);
    int len = strlen(_buf);
    for (int i = 0; i <= len; i++) _ubuf[i] = _buf[i];
    return _ubuf;
}

__stdcall const char32_t* bool_to_ustr(int b) {
    bool_to_str(b);
    int len = strlen(_buf);
    for (int i = 0; i <= len; i++) _ubuf[i] = _buf[i];
    return _ubuf;
}

/* ================================================================
 * 14. 格式化字符串转换 (Printf-style)
 * conv_printf(fmt, ...) → 输出到静态缓冲区
 * 简化版: 只支持 %d %ld %f %lf %s 格式
 * ================================================================ */

/* 外部依赖声明 */
void printf(const char* fmt, ...);
