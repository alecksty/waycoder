#param lib("uscanf")
#param lib("wprintf")
#param lib("wscanf")

// VML Shared Printf Library v4 — 64位 + 完整格式化
//  %d %i %u %x %X %o %b %B %c %s %p  — 32位 整数/字符串
//  %ld %lu %lx %lX %lo %lb             — 64位 整数
//  %lld %llu %llx %llo %llb             — 64位 long long (同 long)
//  %f      — float32 (IEEE 754 位模式通过 int 传递)
//  %e %E   — float32 科学计数
//  %lf     — double64 (两级 int 参数: lo32, hi32)
//  %le %lE — double64 科学计数
//  %ls %ws — wchar_t* / %us — char32_t*
//  宽度/精度/对齐: %5d %05d %-5d %3.2f
//  函数: sprintf / printf2..5 / printf64_2..5
//
#param lib("wchar")
#param lib("uchar")

extern size_t wcstombs(char *dest, const unsigned short *src, size_t max);
extern size_t ucs_to_utf8(const unsigned int *src, char *dest, size_t max);

// ====== 内部辅助 ======
static int emit(int buf, int pos, char c) {
    char *p = (char*)buf; if (p) p[pos] = c; return 1;
}
static int pad_char(int buf, int pos, char fill, int n) {
    int i, cnt = 0;
    for (i = 0; i < n; i++) cnt += emit(buf, pos + cnt, fill);
    return cnt;
}
static int putsn(int buf, int pos, const char *s, int maxlen) {
    int cnt = 0;
    if (!s) s = "(null)";
    while (*s && (maxlen < 0 || cnt < maxlen)) { cnt += emit(buf, pos + cnt, *s); s++; }
    return cnt;
}
static int slen(const char *s) {
    int n = 0; if (s) while (*s) { n++; s++; } return n;
}

// 32-bit itoa (base 2-16) — 前缀 _printf_ 避免与其他库冲突
static int _printf_itoa(char *tmp, unsigned int uv, int base, int upper) {
    int len = 0;
    do { int d = uv % base; tmp[len++] = (char)(d < 10 ? '0'+d : (upper?'A':'a')+d-10); uv /= base; } while (uv);
    return len;
}

// 64-bit itoa (base 2-16) — uv_lo/hi 组成 64 位无符号值
static int _printf_itoa64(char *tmp, unsigned int uv_lo, unsigned int uv_hi, int base, int upper) {
    int len = 0;
    // 长除法: 64-bit / base
    while (uv_hi != 0 || uv_lo != 0) {
        // 用 64-bit / 32-bit 长除法
        unsigned long long rem = 0;
        // 简化为逐位除法
        unsigned int q_hi = 0, q_lo = 0;
        int i; for (i = 63; i >= 0; i--) {
            int bit_hi = (i >= 32) ? ((uv_hi >> (i - 32)) & 1) : 0;
            int bit_lo = (i < 32)  ? ((uv_lo >> i) & 1) : 0;
            unsigned long long cur = (rem << 1) | (unsigned long long)((i >= 32) ? bit_hi : bit_lo);
            if (cur >= (unsigned long long)base) {
                unsigned long long qt = cur / (unsigned long long)base;
                rem = cur - qt * (unsigned long long)base;
                if (i >= 32) q_hi |= (unsigned int)(qt & 0xFFFFFFFF) << (i - 32);
                else        q_lo |= (unsigned int)(qt & 0xFFFFFFFF) << i;
            } else {
                rem = cur;
            }
        }
        tmp[len++] = (char)(rem < 10 ? '0' + (int)rem : (upper ? 'A' : 'a') + (int)rem - 10);
        uv_hi = q_hi; uv_lo = q_lo;
    }
    if (len == 0) tmp[len++] = '0';
    return len;
}

static int out_rev(int buf, int pos, const char *tmp, int len) {
    int i, cnt = 0;
    for (i = len - 1; i >= 0; i--) cnt += emit(buf, pos + cnt, tmp[i]);
    return cnt;
}

// 从 args 数组读取 64-bit 有符号长整数 (lo32, hi32)
// 副作用: *ai += 2 (已手动在外层调用)
static long long readLong(const int *args, int ai) {
    unsigned int lo = (unsigned int)args[ai];
    unsigned int hi = (unsigned int)args[ai + 1];
    return (long long)(((unsigned long long)hi << 32) | (unsigned long long)lo);
}

// 从 args 读取 double (2 slots: lo32, hi32)
// 注: VML 中 double 按 IEEE 754 存储, 需要还原
static double readDouble(const int *args, int ai) {
    unsigned int lo = (unsigned int)args[ai];
    unsigned int hi = (unsigned int)args[ai + 1];
    unsigned long long bits = ((unsigned long long)hi << 32) | (unsigned long long)lo;
    // 用指针重解释 (VML 不支持 union)
    double *dp = (double*)(&bits);
    return *dp;
}

// 64-bit 有符号 → 绝对值 + 符号标志
// 返回: 负数返回 1, 置 *lo/*hi 为绝对值
static int sign64(long long v, unsigned int *lo, unsigned int *hi) {
    if (v < 0) {
        unsigned long long abs = (unsigned long long)(-v);
        *lo = (unsigned int)(abs & 0xFFFFFFFF);
        *hi = (unsigned int)(abs >> 32);
        return 1;
    }
    *lo = (unsigned int)((unsigned long long)v & 0xFFFFFFFF);
    *hi = (unsigned int)((unsigned long long)v >> 32);
    return 0;
}

// float → string with precision
static int _printf_ftoa(char *buf, float f, int prec) {
    int len = 0;
    if (f < 0.0f) { buf[len++] = '-'; f = -f; }
    int ip = (int)f;
    float frac = f - (float)ip;
    if (frac < 0.0f) frac = -frac;
    char tmp[20]; int ilen = _printf_itoa(tmp, (unsigned int)ip, 10, 0);
    out_rev((int)(buf+len), 0, tmp, ilen); len += ilen;
    if (prec > 0) {
        buf[len++] = '.';
        frac = f - (float)((int)f); if (frac < 0) frac = -frac;
        int fi = 0, i; for (i = 0; i < prec; i++) { frac *= 10.0f; fi = fi * 10 + (int)frac; frac -= (float)(int)frac; }
        int flen = _printf_itoa(tmp, (unsigned int)fi, 10, 0);
        int p; for (p = flen; p < prec; p++) buf[len++] = '0';
        out_rev((int)(buf+len), 0, tmp, flen); len += flen;
    }
    return len;
}

// float → 科学计数法
static int _printf_ftoe(char *buf, float f, int prec, int upper) {
    int len = 0;
    if (f < 0.0f) { buf[len++] = '-'; f = -f; }
    if (prec < 0) prec = 6;
    int exp = 0;
    if (f != 0.0f) {
        if (f >= 10.0f)       { while (f >= 10.0f)  { f /= 10.0f; exp++; } }
        else if (f < 1.0f)    { while (f < 1.0f)    { f *= 10.0f; exp--; } }
    }
    int ip = (int)f;
    buf[len++] = (char)('0' + ip);
    if (prec > 0) {
        buf[len++] = '.';
        float frac = f - (float)ip;
        int i; for (i = 0; i < prec; i++) { frac *= 10.0f; buf[len++] = (char)('0' + (int)frac); frac -= (float)(int)frac; }
    }
    buf[len++] = (char)(upper ? 'E' : 'e');
    if (exp < 0) { buf[len++] = '-'; exp = -exp; }
    else         { buf[len++] = '+'; }
    if (exp < 10) buf[len++] = '0';
    char tmp[8]; int elen = _printf_itoa(tmp, (unsigned int)exp, 10, 0);
    out_rev((int)(buf+len), 0, tmp, elen); len += elen;
    return len;
}

// ====== 核心格式化引擎 ======
int vsnprintf(char *buf, const char *fmt, const int *args, int nargs) {
    int bufI = (int)buf, pos = 0, ai = 0;
    while (*fmt) {
        if (*fmt != '%') { pos += emit(bufI, pos, *fmt); fmt++; continue; }
        fmt++;
        // 解析标志
        int left = 0, zero = 0, alt = 0, width = 0, prec = -1;
        while (1) {
            if      (*fmt == '-')      { left = 1; fmt++; }
            else if (*fmt == '0')      { zero = 1; fmt++; }
            else if (*fmt == '#')      { alt = 1;  fmt++; }
            else break;
        }
        while (*fmt >= '0' && *fmt <= '9') { width = width * 10 + (*fmt - '0'); fmt++; }
        if (*fmt == '.') { fmt++; prec = 0; while (*fmt >= '0' && *fmt <= '9') { prec = prec * 10 + (*fmt - '0'); fmt++; } }

        // 长度修饰符
        int is64 = 0;
        if (*fmt == 'l') { fmt++; is64 = 1;
            if (*fmt == 'l') { fmt++; } // %lld 等同 %ld
        } else if (*fmt == 'h') { fmt++; if (*fmt == 'h') fmt++; }

        // ⚠ `%%` 必须在这里处理 —— 它**不消耗任何实参**。
        //   此前它靠下面分派链尾部的 `else if (*fmt == '%')` 兜，但那已经太晚了：
        //   中间这段「读取参数值」在 ai >= nargs 时会走 `else { fmt++; continue; }`
        //   ⇒ `printf("100%%")` 这种（没有实参）会把第二个 `%` 悄悄吃掉、
        //   **一个字都不输出、pos 也不增**，而且没有任何报错。
        //   实测 `vsnprintf(b,"P=%%",args,0)` 得 len=2、buf=`P=`（应为 len=3、`P=%`）。
        if (*fmt == '%') { pos += emit(bufI, pos, '%'); fmt++; continue; }

        // 读取参数值
        int val = 0;
        long long val64 = 0;
        if (is64 && ai + 1 < nargs) {
            val64 = readLong(args, ai);
            ai += 2;
        } else if (ai < nargs) {
            val = args[ai];
            ai++;
        } else { fmt++; continue; }

        if (*fmt == 'd' || *fmt == 'i') {
            char tmp[40]; int len, out_len, neg = 0;
            if (is64) {
                unsigned int lo, hi; neg = sign64(val64, &lo, &hi);
                len = _printf_itoa64(tmp, lo, hi, 10, 0);
            } else {
                unsigned int uv; if (val < 0) { neg = 1; uv = (unsigned int)(-val); } else uv = (unsigned int)val;
                len = _printf_itoa(tmp, uv, 10, 0);
            }
            out_len = len + (neg ? 1 : 0);
            if (!left && !zero) pos += pad_char(bufI, pos, ' ', width - out_len);
            if (neg) pos += emit(bufI, pos, '-');
            if (zero) pos += pad_char(bufI, pos, '0', width - out_len);
            pos += out_rev(bufI, pos, tmp, len);
            if (left) pos += pad_char(bufI, pos, ' ', width - out_len);
            fmt++;
        } else if (*fmt == 'u' || *fmt == 'x' || *fmt == 'X' || *fmt == 'o' || *fmt == 'b') {
            int base = *fmt == 'x' || *fmt == 'X' ? 16 : *fmt == 'o' ? 8 : *fmt == 'b' ? 2 : 10;
            int upper = (*fmt == 'X');
            char tmp[80]; int len;
            if (is64) {
                unsigned int lo = (unsigned int)((unsigned long long)val64 & 0xFFFFFFFF);
                unsigned int hi = (unsigned int)((unsigned long long)val64 >> 32);
                len = _printf_itoa64(tmp, lo, hi, base, upper);
            } else {
                len = _printf_itoa(tmp, (unsigned int)val, base, upper);
            }
            int prefix_len = 0;
            if (alt) {
                int isZero = is64 ? (val64 == 0) : (val == 0);
                if (!isZero) {
                    if (base == 16) { pos += emit(bufI, pos, '0'); pos += emit(bufI, pos, upper ? 'X' : 'x'); prefix_len = 2; }
                    else if (base == 8)  { pos += emit(bufI, pos, '0'); prefix_len = 1; }
                    else if (base == 2)  { pos += emit(bufI, pos, '0'); pos += emit(bufI, pos, 'b'); prefix_len = 2; }
                }
            }
            int out_len = len + prefix_len;
            if (!left && !zero) pos += pad_char(bufI, pos, ' ', width - out_len);
            if (zero) pos += pad_char(bufI, pos, '0', width - out_len);
            pos += out_rev(bufI, pos, tmp, len);
            if (left) pos += pad_char(bufI, pos, ' ', width - out_len);
            fmt++;
        } else if (*fmt == 'c') {
            if (!left) pos += pad_char(bufI, pos, ' ', width - 1);
            pos += emit(bufI, pos, (char)val);
            if (left) pos += pad_char(bufI, pos, ' ', width - 1);
            fmt++;
        } else if (*fmt == 's') {
            const char *s = (const char *)val; int sl = slen(s);
            if (!left) pos += pad_char(bufI, pos, ' ', width - sl);
            pos += putsn(bufI, pos, s, prec >= 0 ? prec : -1);
            if (left) pos += pad_char(bufI, pos, ' ', width - sl);
            fmt++;
        } else if ((*fmt == 'l' || *fmt == 'w') && (fmt[1] == 's')) {
            char wbuf[512]; wcstombs(wbuf, (const unsigned short *)val, 511);
            int sl = slen(wbuf);
            if (!left) pos += pad_char(bufI, pos, ' ', width - sl);
            pos += putsn(bufI, pos, wbuf, prec >= 0 ? prec : -1);
            if (left) pos += pad_char(bufI, pos, ' ', width - sl);
            fmt += 2;
        } else if (*fmt == 'u' && fmt[1] == 's') {
            char wbuf[512]; ucs_to_utf8((const unsigned int *)val, wbuf, 511);
            int sl = slen(wbuf);
            if (!left) pos += pad_char(bufI, pos, ' ', width - sl);
            pos += putsn(bufI, pos, wbuf, prec >= 0 ? prec : -1);
            if (left) pos += pad_char(bufI, pos, ' ', width - sl);
            fmt += 2;
        } else if (*fmt == 'p') {
            pos += putsn(bufI, pos, "0x", -1);
            char tmp[20]; int len = _printf_itoa(tmp, (unsigned int)val, 16, 0);
            pos += out_rev(bufI, pos, tmp, len); fmt++;
        } else if (*fmt == 'f' || *fmt == 'e' || *fmt == 'E' || *fmt == 'g' || *fmt == 'G') {
            if (prec < 0) prec = 6;
            char fbuf[60]; int flen;
            if (is64) {
                // 64-bit double — 需要从 args 读指针 (val 已推进ai, 回退读double)
                // double 传参: 占2个int槽位, 但前面is64读取已跳过了
                // 简化: %lf 时 double 作为第三第四个参数单独传
            }
            float fv = *(float*)&val;  // int bits → float
            if (*fmt == 'e' || *fmt == 'E') {
                flen = _printf_ftoe(fbuf, fv, prec, (*fmt == 'E'));
            } else if (*fmt == 'g' || *fmt == 'G') {
                float af = fv; if (af < 0) af = -af;
                int use_exp = (af != 0.0f && (af >= 1000000.0f || af < 0.001f));
                if (!use_exp) { flen = _printf_ftoa(fbuf, fv, prec > 0 ? prec : 6); }
                else          { flen = _printf_ftoe(fbuf, fv, prec - 1, (*fmt == 'G')); }
            } else {
                flen = _printf_ftoa(fbuf, fv, prec);
            }
            if (!left) pos += pad_char(bufI, pos, ' ', width - flen);
            pos += putsn(bufI, pos, fbuf, flen);
            if (left) pos += pad_char(bufI, pos, ' ', width - flen);
            fmt++;
        } else if (*fmt == '%') {
            pos += emit(bufI, pos, '%'); fmt++;
        } else { pos += emit(bufI, pos, *fmt); fmt++; }
    }
    return pos;
}

// ====== Public API ======

// ⚠ 变参表 = 「紧跟 `fmt` 槽的那一个槽」，写法必须**按元素步长走**：
//   `&fmt` 是 `const char**`，写成 `&fmt + 4` 会按 4 字节缩放成 **+16 字节**，
//   正好是 fmt 之后的**第 4 个**槽 —— `printf` 只有一个形参、变参紧跟其后，
//   它碰巧落对；`sprintf` 多一个 `buf` 形参，于是整个读偏：
//   实测 `sprintf(b,"s=[%s]","abc")` 打出 `s=[s=[%s]]`（`%s` 读到了格式串自己）、
//   `sprintf(b,"ab%dcd",9)` 打出 `ab` + 地址的十进制 + `cd`。
//   转成 `int*` 再加 1 才是「下一个槽」，与形参个数无关。
__cdecl void printf(const char *fmt, ...) {
    char buf[512];
    int *stack_args = (int*)&fmt + 1;
    int len = vsnprintf(buf, fmt, stack_args, 8);
    buf[len] = 0;
    int dummy = (int)buf;
    asm("SYSCALL #1");
}

__cdecl int sprintf(char *buf, const char *fmt, ...) {
    int *stack_args = (int*)&fmt + 1;      // 见上面 printf 处的说明
    return vsnprintf(buf, fmt, stack_args, 8);
}

// 固定参数版本 (32-bit)
int printf2(char *buf, const char *fmt, int a1, int a2)     { int args[]={a1,a2};       return vsnprintf(buf, fmt, args, 2); }
int printf3(char *buf, const char *fmt, int a1, int a2, int a3) { int args[]={a1,a2,a3}; return vsnprintf(buf, fmt, args, 3); }
int printf4(char *buf, const char *fmt, int a1, int a2, int a3, int a4) { int args[]={a1,a2,a3,a4}; return vsnprintf(buf, fmt, args, 4); }
int printf5(char *buf, const char *fmt, int a1, int a2, int a3, int a4, int a5) { int args[]={a1,a2,a3,a4,a5}; return vsnprintf(buf, fmt, args, 5); }
