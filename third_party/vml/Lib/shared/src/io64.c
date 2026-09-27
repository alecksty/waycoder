#param lib("convert64")
#param lib("io")
#param lib("scanf")

// 前向声明 —— **类型必须写全**：被调函数在别的模块里，没有声明时前端按默认
// `int` 猜形参 ⇒ `long`/`double` 被压成 4 字节，而被调方按 8 字节读，
// 实参整体错位（实测 print_long 打出空、64 位值丢高半字）。
__stdcall int ltoa(long value, char* dst);
__stdcall int ltoa_hex(long value, char* dst);
__stdcall int dtoa(double value, int precision, char* dst);


// VML Shared IO64 Library — 64-bit I/O Functions

__stdcall void print_long(long val) {
    char buf[24];
    ltoa(val, buf);
    print_str(buf);
}

__stdcall void print_hex_long(long val) {
    char buf[20];
    ltoa_hex(val, buf);
    print_str(buf);
}

__stdcall void println_long(long val) {
    print_long(val);
    putchar('\n');
}

__stdcall void println_hex_long(long val) {
    print_hex_long(val);
    putchar('\n');
}

__stdcall long input_long(void) {
    char buf[32];
    int i = 0;
    while (i < 30) {
        int c = getchar();
        if (c == '\n' || c == '\r' || c == 0) break;
        buf[i++] = (char)c;
    }
    buf[i] = 0;
    return atol(buf);
}

__stdcall void print_double(double val, int precision) {
    char buf[64];
    dtoa(val, precision, buf);
    print_str(buf);
}

__stdcall void println_double(double val, int precision) {
    print_double(val, precision);
    putchar('\n');
}
