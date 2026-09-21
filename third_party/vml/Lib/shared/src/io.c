#param lib("builtins")
#param lib("wchar")
#param lib("uchar")
#param lib("ctype")
#param lib("scanf")
#param lib("uscanf")
#param lib("wscanf")
#param lib("wstring")

/* WString/UTF-8 conversion helpers — uses wchar.c functions */
extern size_t wcslen(const wchar_t *s);
extern size_t wcstombs(char *dest, const wchar_t *src, size_t max);
extern size_t mbstowcs(wchar_t *dest, const char *src, size_t max);

/* 标准流。`stdio.h` 里是 `extern int stdin/stdout/stderr;`（**只有声明**），
   **定义放在这里** —— 只声明不定义的话链接期缺符号，而老程序到处在用 `stderr`
   （tty-clock 第一轮编译就报"未声明的变量 'stderr'"）。
   VML 里 `FILE` 就是个 fd（见 stdio.h），所以取值就是 POSIX 那三个。 */
int stdin = 0;
int stdout = 1;
int stderr = 2;

__stdcall void putchar(char c) {
    asm("SYSCALL #4");
}

__stdcall int getchar(void) {
    /* ⚠ 把 asm 当**表达式**用（规则见 vmlui.c 头部）："先 asm(...) 再
       return c" 会把返回值丢掉 —— 实测 c 恒为垃圾，且不报错。本文件漏改。 */
    return asm("SYSCALL #5");
}

__stdcall void print_str(const char* str) {
    asm("SYSCALL #1");
}

__stdcall void print_int(int val) {
    asm("SYSCALL #6");
}

__stdcall void print_hex(int val) {
    asm("SYSCALL #10");
}

__stdcall int input_str(void) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #2");
}

__stdcall int input_int(void) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #7");
}

__stdcall void puts(const char* str) {
    asm("SYSCALL #1");
    asm("LOAD R0 #10");
    asm("SYSCALL #4");
}

__stdcall void print_string(const char* str) {
    asm("SYSCALL #1");
}

__stdcall int kb_hit(void) {
    int r;
    r = 0;
    return r;
}

__stdcall void clear_screen(void) {
    asm("LOAD R0 #12");
    asm("SYSCALL #4");
}

__stdcall void println_str(const char* str) {
    print_str(str);
    putchar(10);
}

__stdcall void println_int(int val) {
    print_int(val);
    putchar(10);
}

__stdcall void println_hex(int val) {
    print_hex(val);
    putchar(10);
}

__stdcall void print_str_no_nl(const char* str) {
    print_str(str);
}

/* ── Wide String I/O (wchar_t ↔ UTF-8 auto-conversion) ── */

/// 输出 wide 字符串: wchar_t* → UTF-8 → SYSCALL #1
/// MCU 兼容: 始终输出 UTF-8 字节流 (串口终端标准)
__stdcall void print_wstr(const wchar_t* wstr) {
    char buf[1024];
    wcstombs(buf, wstr, sizeof(buf) - 1);
    print_str(buf);
}

/// 输出 wide 字符串 + 换行
__stdcall void println_wstr(const wchar_t* wstr) {
    print_wstr(wstr);
    putchar(10);
}

/// 将 wide 字符串转换为 UTF-8 存入缓冲区，返回字节数
__stdcall int wstr_to_utf8(const wchar_t* wstr, char* out, int max_bytes) {
    return (int)wcstombs(out, wstr, (size_t)max_bytes);
}

/// 将 UTF-8 字符串转换为 wide 字符串，返回字符数
__stdcall int utf8_to_wstr(const char* str, wchar_t* out, int max_chars) {
    return (int)mbstowcs(out, str, (size_t)max_chars);
}

/* ── 32-bit Unicode String I/O (char32_t ↔ UTF-8) ── */

extern size_t ucs_to_utf8(const char32_t *src, char *dest, size_t max);
extern size_t utf8_to_ucs(const char *src, char32_t *dest, size_t max);

__stdcall void print_ustr(const char32_t* ustr) {
    char buf[1024];
    ucs_to_utf8(ustr, buf, sizeof(buf) - 1);
    print_str(buf);
}

__stdcall int ustr_to_utf8(const char32_t* ustr, char* out, int max_bytes) {
    return (int)ucs_to_utf8(ustr, out, (size_t)max_bytes);
}

__stdcall int utf8_to_ustr(const char* str, char32_t* out, int max_chars) {
    return (int)utf8_to_ucs(str, out, (size_t)max_chars);
}
