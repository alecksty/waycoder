#param lib("builtins")
#param lib("wchar")
#param lib("uchar")

/* printx.c — VML shared library: formatted output for all data types
 * Provides print and println for: int32, string, wstring, ustring, float32
 * int64 and float64 via softint64/softdouble libraries
 *
 * Compile: vmltool printx.c -o printx.vml -l wchar -l uchar -l softfloat -l softint64 -l softdouble --no-link
 */
#param lib("wchar")
#param lib("uchar")

/* ── External dependencies ──────────────────── */
extern size_t wcstombs(char *dest, const unsigned short *src, size_t max);
extern size_t ucs_to_utf8(const unsigned int *src, char *dest, size_t max);

/* ── Forward declarations ───────────────────── */
void print_int(int val);
void print_str(const char *str);
void putchar(char c);

/* ==============================================
 * int32 — SYSCALL #6
 * ============================================ */
__stdcall void printx_int32(int val) {
    asm("SYSCALL #6");
}
__stdcall void printlnx_int32(int val) {
    asm("SYSCALL #6");
    asm("LOAD R0 #10");
    asm("SYSCALL #4");
}

/* ==============================================
 * string (8-bit char*) — SYSCALL #1
 * ============================================ */
__stdcall void printx_string(const char *str) {
    asm("SYSCALL #1");
}
__stdcall void printlnx_string(const char *str) {
    asm("SYSCALL #1");
    asm("LOAD R0 #10");
    asm("SYSCALL #4");
}

/* ==============================================
 * wstring (16-bit wchar_t*) — wcstombs → SYSCALL #1
 * ============================================ */
__stdcall void printx_wstring(const unsigned short *wstr) {
    char buf[1024];
    wcstombs(buf, wstr, 1023);
    print_str(buf);
}
__stdcall void printlnx_wstring(const unsigned short *wstr) {
    char buf[1024];
    wcstombs(buf, wstr, 1023);
    print_str(buf);
    putchar(10);
}

/* ==============================================
 * ustring (32-bit char32_t*) — ucs_to_utf8 → SYSCALL #1
 * ============================================ */
__stdcall void printx_ustring(const unsigned int *ustr) {
    char buf[1024];
    ucs_to_utf8(ustr, buf, 1023);
    print_str(buf);
}
__stdcall void printlnx_ustring(const unsigned int *ustr) {
    char buf[1024];
    ucs_to_utf8(ustr, buf, 1023);
    print_str(buf);
    putchar(10);
}

/* ==============================================
 * float32 — SYSCALL #8 (OutputFloat)
 * ============================================ */
__stdcall void printx_float32(float val) {
    asm("SYSCALL #8");
}
__stdcall void printlnx_float32(float val) {
    asm("SYSCALL #8");
    asm("LOAD R0 #10");
    asm("SYSCALL #4");
}

/* ==============================================
 * float64 — 暂用 SYSCALL #8 (截断为 float32)
 * 完整支持需 softdouble 库的 print_double
 * ============================================ */
__stdcall void printx_float64(double val) {
    float f = (float)val;
    asm("SYSCALL #8");
}
__stdcall void printlnx_float64(double val) {
    float f = (float)val;
    asm("SYSCALL #8");
    asm("LOAD R0 #10");
    asm("SYSCALL #4");
}

/* ==============================================
 * int64 — 高32位+低32位分别输出十六进制
 * ============================================ */
__stdcall void printx_int64_hex(int hi, int lo) {
    /* Output "0x" + hi_hex + lo_hex via SYSCALL #10 (print hex) */
    /* For simplicity: output high word then low word */
    print_str("0x");
    print_int(hi);  /* fallback: print as decimal */
    putchar('_');
    print_int(lo);
}

/* ==============================================
 * Convenience: print newline only
 * ============================================ */
__stdcall void printx_nl(void) {
    putchar(10);
}
