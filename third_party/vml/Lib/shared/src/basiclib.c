#param lib("math")
#param lib("fixed")

// VML BASIC Library — string, date/time, and utility functions
// Compiled as VML shared library, callable from BASIC via DECLARE FUNCTION
// Uses __stdcall convention: callee cleans stack, return in R0

// ============ Internal helpers ============

// Static buffers for string return values (QBASIC convention)
static char _buf1[256];
static char _buf2[256];
static char _buf3[256];   // 字符串拼接（`a$ + b$`）的结果缓冲，见 basic_concat

static void _hex_str(int val, char* buf) {
    const char* h = "0123456789ABCDEF";
    char tmp[9];
    int i = 0;
    if (val == 0) { buf[0] = '0'; buf[1] = 0; return; }
    while (val > 0 && i < 8) { tmp[i++] = h[val & 0xF]; val >>= 4; }
    for (int j = 0; j < i; j++) buf[j] = tmp[i - 1 - j];
    buf[i] = 0;
}

// ============ String Functions ============

/// INSTR([start,] haystack$, needle$) — 1-based position, 0=not found
__stdcall int basic_instr(int start, const char* haystack, const char* needle) {
    int hlen, nlen, i, j;
    if (!haystack || !needle) return 0;
    hlen = 0; while (haystack[hlen]) hlen++;
    nlen = 0; while (needle[nlen]) nlen++;
    if (nlen == 0) return 0;
    if (start < 1) start = 1;
    if (start > hlen) return 0;
    for (i = start - 1; i <= hlen - nlen; i++) {
        for (j = 0; j < nlen && haystack[i + j] == needle[j]; j++);
        if (j == nlen) return i + 1;
    }
    return 0;
}

/// STRING$(n, char-code) — repeat character N times
__stdcall char* basic_stringN(int n, int ch) {
    int i;
    if (n < 0) n = 0;
    if (n > 255) n = 255;
    i = 0;
    while (i < n) {
        _buf1[i] = (char)ch;
        i = i + 1;
    }
    _buf1[n] = 0;
    return _buf1;
}

/// STRING$(n, string$) — repeat first char of string N times
__stdcall char* basic_stringS(int n, const char* s) {
    int ch, i;
    ch = ' ';
    if (s && *s) ch = (int)(unsigned char)*s;
    if (n < 0) n = 0;
    if (n > 255) n = 255;
    i = 0;
    while (i < n) {
        _buf1[i] = (char)ch;
        i = i + 1;
    }
    _buf1[n] = 0;
    return _buf1;
}

/// HEX$(number) — integer to uppercase hex string
__stdcall char* basic_hex(int val) {
    _hex_str(val, _buf1);
    return _buf1;
}

/// OCT$(number) — integer to octal string
__stdcall char* basic_oct(int val) {
    char* p;
    int i;
    p = _buf1 + 10;
    *p = 0;
    if (val == 0) { _buf1[0] = '0'; _buf1[1] = 0; return _buf1; }
    while (val > 0 && p > _buf1) { *--p = (char)('0' + (val & 7)); val >>= 3; }
    for (i = 0; p[i]; i++) _buf1[i] = p[i];
    _buf1[i] = 0;
    return _buf1;
}

/// MID$(string$, start, length) — substring using _buf1
__stdcall char* basic_mid3(const char* s, int start, int length) {
    int i;
    if (!s) { _buf1[0] = 0; return _buf1; }
    if (start < 1) start = 1;
    if (length < 0) length = 255;
    if (length > 255) length = 255;
    i = 0;
    start = start - 1;
    while (s[start] && i < length) {
        _buf1[i] = s[start];
        i = i + 1;
        start = start + 1;
    }
    _buf1[i] = 0;
    return _buf1;
}

// ============ Date/Time Functions ============

/// DATE$ — current date "YYYY-MM-DD" (SYSCALL 55)
__stdcall const char* basic_date_str(void) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #55");
}

/// TIME$ — current time "HH:MM:SS" (SYSCALL 56)
__stdcall const char* basic_time_str(void) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #56");
}

/// TIMER — seconds elapsed (SYSCALL 53 returns ms)
__stdcall int basic_timer(void) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #53");
}

// ============ Input ============

/// INPUT$(n) — read N chars from stdin, blocking (SYSCALL 5 loop)
__stdcall char* basic_inputN(int n) {
    if (n < 0) n = 0;
    if (n > 255) n = 255;
    for (int i = 0; i < n; i++) {
        int ch;
        asm("LOAD R0 #1");  // blocking mode
        asm("SYSCALL #5");
        ch = 0;  // SYSCALL result stored to stack by compiler fix
        _buf1[i] = (char)ch;
    }
    _buf1[n] = 0;
    return _buf1;
}

// ============ EOF ============

/// EOF(filenum) — check end-of-file (SYSCALL 114 FileControl)
__stdcall int basic_eof(int filenum) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #114");
}

// ============ String Manipulation (inline replacements) ============

/// UCASE$(string$) — convert to uppercase
__stdcall char* basic_ucase(const char* s) {
    int i = 0;
    if (!s) { _buf1[0] = 0; return _buf1; }
    while (s[i] && i < 255) {
        char c = s[i];
        if (c >= 'a' && c <= 'z') c -= 32;
        _buf1[i] = c;
        i++;
    }
    _buf1[i] = 0;
    return _buf1;
}

/// LCASE$(string$) — convert to lowercase
__stdcall char* basic_lcase(const char* s) {
    int i = 0;
    if (!s) { _buf1[0] = 0; return _buf1; }
    while (s[i] && i < 255) {
        char c = s[i];
        if (c >= 'A' && c <= 'Z') c += 32;
        _buf1[i] = c;
        i++;
    }
    _buf1[i] = 0;
    return _buf1;
}

/// LEN(string$) — return length
__stdcall int basic_len(const char* s) {
    int n = 0;
    if (!s) return 0;
    while (s[n]) n++;
    return n;
}

/// ASC(string$) — ASCII code of first character
__stdcall int basic_asc(const char* s) {
    if (!s || !*s) return 0;
    return (int)(unsigned char)*s;
}

/// CHR$(n) — character from ASCII code (returns 1-char string)
__stdcall char* basic_chr(int n) {
    _buf1[0] = (char)(n & 0xFF);
    _buf1[1] = 0;
    return _buf1;
}

/// SPACE$(n) — n spaces (inline to avoid nested CALL stack cleanup issues)
__stdcall char* basic_space(int n) {
    int i;
    if (n < 0) n = 0;
    if (n > 255) n = 255;
    i = 0;
    while (i < n) {
        _buf1[i] = ' ';
        i = i + 1;
    }
    _buf1[n] = 0;
    return _buf1;
}

/// STR$(number) — integer to decimal string (no division, pure subtraction)
__stdcall char* basic_str_int(int val) {
    int d, n, i, j, digit;
    char t;
    if (val == 0) { _buf1[0] = '0'; _buf1[1] = 0; return _buf1; }
    if (val < 0) { _buf1[0] = '-'; val = -val; i = 1; } else i = 0;
    // Find largest power of 10 <= val using multiplication
    d = 1;
    while (d * 10 <= val) d = d * 10;
    // Extract digits by repeated subtraction
    j = i;
    n = val;
    while (d >= 1) {
        digit = 0;
        while (n >= d) { n = n - d; digit = digit + 1; }
        _buf1[j] = (char)('0' + digit);
        j = j + 1;
        // Divide d by 10 via repeated subtraction
        { int count = 0; int tmp = d; while (tmp >= 10) { tmp = tmp - 10; count = count + 1; } d = count; }
    }
    _buf1[j] = 0;
    return _buf1;
}

/// VAL(string$) — string to integer
__stdcall int basic_val(const char* s) {
    int result = 0, sign = 1, i = 0;
    if (!s) return 0;
    // Skip leading spaces
    while (s[i] == ' ') i++;
    // Optional sign
    if (s[i] == '-') { sign = -1; i++; }
    else if (s[i] == '+') i++;
    // Parse digits
    while (s[i] >= '0' && s[i] <= '9') {
        result = result * 10 + (s[i] - '0');
        i++;
    }
    return result * sign;
}

/// LEFT$(string$, n) — left N characters
__stdcall char* basic_left(const char* s, int n) {
    int i;
    i = 0;
    if (n < 0) n = 0;
    if (n > 255) n = 255;
    if (s == 0) { _buf1[0] = 0; return _buf1; }
    for (i = 0; i < n; i++) {
        if (s[i] == 0) break;
        _buf1[i] = s[i];
    }
    _buf1[i] = 0;
    return _buf1;
}

/// RIGHT$(string$, n) — right N characters
__stdcall char* basic_right(const char* s, int n) {
    int len, start, i;
    if (n < 0) n = 0;
    if (n > 255) n = 255;
    if (s == 0) { _buf1[0] = 0; return _buf1; }
    len = 0; for (len = 0; s[len]; len++);
    start = len - n;
    if (start < 0) start = 0;
    for (i = 0; s[start]; i++) { _buf1[i] = s[start]; start = start + 1; }
    _buf1[i] = 0;
    return _buf1;
}

/// LTRIM$(string$) — remove leading spaces
__stdcall char* basic_ltrim(const char* s) {
    int i;
    i = 0;
    if (s == 0) { _buf1[0] = 0; return _buf1; }
    for (i = 0; s[i] == ' '; i++);
    { int j = 0; for (j = 0; s[i]; j++) { _buf1[j] = s[i]; i = i + 1; } _buf1[j] = 0; }
    return _buf1;
}

/// RTRIM$(string$) — remove trailing spaces
__stdcall char* basic_rtrim(const char* s) {
    int len, i;
    if (s == 0) { _buf1[0] = 0; return _buf1; }
    len = 0; for (len = 0; s[len]; len++);
    for (; len > 0 && s[len - 1] == ' '; len--);
    for (i = 0; i < len; i++) _buf1[i] = s[i];
    _buf1[len] = 0;
    return _buf1;
}

/// 字符串拼接 `a$ + b$` —— 返回一个**静态缓冲区**里的结果（QBasic 约定）。
///
/// 为什么必须放在库里：`+` 在编译器里只按整数加法生成（把两个**指针**相加），
/// 结果是野指针，`PRINT` 打出来是空行（实测 `b$ = "x" + "y"` 得空串、且不报错）。
/// 前端认到「两边都是字符串」时会直接 `CALL basic_concat`（见 `GenerateStringConcat`）。
///
/// ⚠ 用 `_buf3` 这块**独立**缓冲，不复用 `_buf1`/`_buf2`：那两个是 `LEFT$/RIGHT$/MID$`
///   等函数的返回值暂存处，拼接结果若与参数共用会让 `a$ + LEFT$(a$, 1)` 这类写法自己踩自己。
/// ⚠ **拼接结果必须按"拼接点"分区**（v0.96.330 修）—— 从前只有一块 `_buf3`，
/// 于是两条拼接先后求值就互相覆盖：`a$ = "AAA" + STR$(1)` 紧跟
/// `b$ = "BBB" + STR$(2)` 之后，**a 和 b 都等于 "BBB2"**（实测，应 AAA1 / BBB2）。
/// 字符串在 BASIC 里是**值**，两个同时活着的拼接结果必须各有各的地方。
///
/// 分区的办法：前端给**每个拼接点**一个固定槽位（同一处代码每次都写同一槽），
/// 于是"不同表达式点算出来的串"天然落在不同缓冲区上。
/// 槽位数 `CONCAT_SLOTS` 是**跨语言契约**，前端 `GenerateStringConcat` 里写着同一个数；
/// 两边对不上也不会崩 —— `basic_concat_slot` 会把越界槽位夹回 0（退化成从前的行为）。
#define CONCAT_SLOTS 16
static char _concats[CONCAT_SLOTS * 256];

__stdcall char* basic_concat_slot(const char* a, const char* b, int slot) {
    char* out;
    int i, j;
    if (slot < 0) slot = 0;
    if (slot >= CONCAT_SLOTS) slot = 0;
    slot = slot * 256;
    out = _concats;
    out = out + slot;
    if (a == 0) { out[0] = 0; return out; }
    if (b == 0) b = a;
    for (i = 0; a[i] && i < 255; i++) out[i] = a[i];
    for (j = 0; b[j] && i < 255; j++) { out[i] = b[j]; i = i + 1; }
    out[i] = 0;
    return out;
}

/// 老入口（两个参数）—— 落到槽 0。其它语言/老产物照旧可用。
__stdcall char* basic_concat(const char* a, const char* b) {
    return basic_concat_slot(a, b, 0);
}

/// ABS(n) — absolute value
__stdcall int basic_abs(int n) {
    return n < 0 ? -n : n;
}

/// SGN(n) — sign: -1, 0, or 1
__stdcall int basic_sgn(int n) {
    if (n < 0) return -1;
    if (n > 0) return 1;
    return 0;
}

// ============ Math Functions ============

/// Helper: sin lookup using linear interpolation (no array, VML C compiler safe)
__stdcall int _sin_lookup(int deg) {
    int v;
    // Normalize
    if (deg < 0) deg = 0;
    if (deg >= 360) deg = deg - (deg / 360) * 360;
    if (deg < 0) deg = deg + 360;
    // Quadrant 0-90: sin(deg) = sin(deg)
    //
    // ⚠ 分段的**基点**原来一律抄成了该段**上界**的值（`if (deg <= 30) return 5000 + ...`
    //   —— 而 5000 是 30° 的 sin，应该是 15° 的 2588）⇒ 每段整体抬高一段：
    //   30° 得 7505（应 5000）、60° 得 10250（> 10000，比 sin 的最大值还大）。
    //   斜率也因此全线偏移。现在每一段用**该段下界的真值**当基点、上界当真值收敛点，
    //   端点逐点精确（sin 15/30/45/60/75/90 = 2588/5000/7071/8660/9659/10000）。
    if (deg <= 90) {
        if (deg <= 15) return 2588 * deg / 15;                            //  0°:0     15°:2588
        if (deg <= 30) return 2588 + 2412 * (deg - 15) / 15;              // 15°:2588  30°:5000
        if (deg <= 45) return 5000 + 2071 * (deg - 30) / 15;              // 30°:5000  45°:7071
        if (deg <= 60) return 7071 + 1589 * (deg - 45) / 15;              // 45°:7071  60°:8660
        if (deg <= 75) return 8660 + 999 * (deg - 60) / 15;               // 60°:8660  75°:9659
        return 9659 + 341 * (deg - 75) / 15;                              // 75°:9659  90°:10000
    }
    // Quadrant 90-180: sin(deg) = sin(180-deg)
    if (deg <= 180) {
        v = _sin_lookup(180 - deg);
        return v;
    }
    // Quadrant 180-270: sin(deg) = -sin(deg-180)
    if (deg <= 270) {
        v = _sin_lookup(deg - 180);
        return -v;
    }
    // Quadrant 270-360: sin(deg) = -sin(360-deg)
    v = _sin_lookup(360 - deg);
    return -v;
}

/// SIN(x) — **x 是角度**，返回 sin(x) * 10000（0.5 → 5000）
///
/// ⚠ 参数单位：QBasic 的 SIN 收弧度，但这一套库**整体是角度制** ——
///   同族函数全按角度调用（`SIN(30)` 要 0.5、`SIN(90)` 要 1.0），
///   而最初这里写的是「x 是弧度 * 10000 ⇒ deg = x / 174」，
///   于是 `SIN(30)` 落进 `_sin_lookup(0)`，**恒等于 0**（不管表修没修都一样）。
///   与 `_sin_lookup` 的度数参数直接对齐，去掉那层换算。
__stdcall int basic_sin(int x) {
    while (x < 0) x = x + 360;
    while (x >= 360) x = x - 360;
    return _sin_lookup(x);
}

/// COS(x) — x 是角度，cos(x) = sin(x + 90°)
__stdcall int basic_cos(int x) {
    return basic_sin(x + 90);
}

/// TAN(x) — tan = sin/cos, avoid division if cos is small
__stdcall int basic_tan(int x) {
    int s, c;
    s = basic_sin(x);
    c = basic_cos(x);
    if (c < 500 && c > -500) {
        if (s >= 0) return 32767;
        return -32767;
    }
    return (s * 10000) / c;
}

/// SQR(x) — square root via Newton's method (integer)
__stdcall int basic_sqr(int x) {
    int r, i;
    if (x <= 0) return 0;
    r = x;
    for (i = 0; i < 12; i++) {
        int q;
        if (r == 0) return 0;
        q = x / r;
        r = (r + q) / 2;
    }
    return r;
}

/// RND — random integer (SYSCALL #50)
__stdcall int basic_rnd(void) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #50");
}

/// EXP(x) — e^x scaled by 10000, Taylor series (avoid div in loop)
__stdcall int basic_exp(int x) {
    int result, term, n;
    if (x > 100000) return 32767;
    if (x < -100000) return 0;
    result = 10000;
    term = 10000;
    for (n = 1; n <= 8; n++) {
        term = term / n;
        term = term * x;
        term = term / 10000;
        result = result + term;
        if (term < 5 && term > -5) break;
    }
    return result;
}

/// LOG(x) — ln(x*10000) scaled by 10000, using Newton on exp
__stdcall int basic_log(int x) {
    int result, i;
    if (x <= 0) return -32767;
    result = 0;
    for (i = 0; i < 5; i++) {
        int y, diff;
        y = basic_exp(result);
        if (y == 0) break;
        diff = x * 10000;
        diff = diff / y;
        result = result + diff - 10000;
    }
    return result;
}

/// ATN(x) — arctangent, returns radians*10000
__stdcall int basic_atn(int x) {
    int sign, t, s;
    if (x == 0) return 0;
    sign = 1;
    if (x < 0) { sign = -1; x = -x; }
    // Use identity: atan(x) = atan(t) where t = x/(1+sqrt(1+x^2))
    // Simpler: series approximation for small x, identity for large x
    if (x > 20000) {
        // atan(x) = PI/2 - atan(1/x) for |x| > 1
        t = 100000000 / x; // 10000*10000/x
        s = basic_atn(t);
        return sign * (15708 - s); // PI/2*10000 - atan(1/x)
    }
    // Taylor: atan(x) ≈ x - x^3/3 + x^5/5 - x^7/7 + x^9/9
    s = x;
    t = x;
    t = -(t / 10000) * (x / 1000) * (x / 1000) / 10; // -x^3/3 ≈ -x*x^2/30000
    s = s + t / 3;
    t = -(t / 10000) * (x / 1000) * (x / 1000) / 10;
    s = s + t / 5;
    t = -(t / 10000) * (x / 1000) * (x / 1000) / 10;
    s = s + t / 7;
    return sign * s;
}

/// INT(x) — identity for integers
__stdcall int basic_int(int x) {
    return x;
}

/// POINT(x, y) — read pixel color from VGA framebuffer, return palette index 0-15 or -1
__stdcall int basic_point(int x, int y) {
    int width, height, bpp;
    int i, offset;
    char pixel_B, pixel_G, pixel_R;
    char* p;
    char* fb;

    // Screen dimensions from MMIO
    p = (char*)0x6FE0;
    width = *((int*)p);
    p = (char*)0x6FE4;
    height = *((int*)p);
    p = (char*)0x6FF3;
    bpp = (int)*p;

    // Bounds check
    if (x < 0 || x >= width || y < 0 || y >= height)
        return -1;

    // Read pixel from framebuffer at 0xA0000 + (y*width + x)*bpp
    offset = (y * width + x) * bpp;
    fb = (char*)(0xA0000 + offset);
    pixel_B = *fb;
    fb = fb + 1;
    pixel_G = *fb;
    fb = fb + 1;
    pixel_R = *fb;

    // Scan 16-entry palette at 0x9F000
    p = (char*)0x9F000;
    for (i = 0; i < 16; i++) {
        offset = i * bpp;
        if (p[offset] == pixel_B && p[offset + 1] == pixel_G && p[offset + 2] == pixel_R)
            return i;
    }
    return -1;
}

// EOF — see declaration above (implemented in basiclib.vml)
