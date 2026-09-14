#param lib("convert64")
#param lib("io")
#param lib("scanf")

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
