// 转换族 —— Lib/shared/src/{convert,convert64,util}.c
//
// 这一族是本会话两处 bug 的所在地，最该铺开：
//   · `_printf_itoa`（printf 内部的 static 助手）被链接器后缀启发式误重定向到
//     这里的 `itoa` —— 两者**参数顺序完全相反**（`(value,dst)` vs `(tmp,uv,base,upper)`）
//   · 十六进制常量里那句 `(ulong)decimal` 强转，也是在这一族的用法上暴露的
//
// 契约（**先读源码再定期望**，别按别的语言的习惯想当然）：
//   itoa(value, dst)      → 写十进制串，**返回长度**
//   itoa_hex(value, dst)  → **大写**十六进制，返回长度
//   atoi(s)               → 跳前导空格/制表符，认 +/-，遇非数字停
//   atoi_hex(s)           → 认可选 `0x`/`0X` 前缀，大小写都收

int fails = 0;

int chk(char *what, int got, int want) {
    if (got != want) {
        print_str("  FAIL ");
        print_str(what);
        print_str(" got=");
        print_int(got);
        print_str(" want=");
        print_int(want);
        newline();
        fails = fails + 1;
    }
    return 0;
}

int chks(char *what, char *buf, char *want) {
    int i = 0;
    while (want[i] != 0) {
        if (buf[i] != want[i]) {
            print_str("  FAIL ");
            print_str(what);
            print_str(" at ");
            print_int(i);
            print_str(" got=");
            print_int(buf[i]);
            print_str(" want=");
            print_int(want[i]);
            newline();
            fails = fails + 1;
            return 0;
        }
        i = i + 1;
    }
    if (buf[i] != 0) {
        print_str("  FAIL ");
        print_str(what);
        print_str(" 尾部多出字节");
        newline();
        fails = fails + 1;
    }
    return 0;
}

int main() {
    char buf[64];
    int n;

    // ---- itoa：返回长度 + 逐字节 ----
    n = itoa(0, buf);
    chk("itoa(0).len", n, 1);
    chks("itoa(0)", buf, "0");

    n = itoa(42, buf);
    chk("itoa(42).len", n, 2);
    chks("itoa(42)", buf, "42");

    n = itoa(0 - 5, buf);
    chk("itoa(-5).len", n, 2);
    chks("itoa(-5)", buf, "-5");

    n = itoa(1000000, buf);
    chk("itoa(1000000).len", n, 7);
    chks("itoa(1000000)", buf, "1000000");

    // ---- atoi ----
    chk("atoi(\"42\")", atoi("42"), 42);
    chk("atoi(\"-5\")", atoi("-5"), 0 - 5);
    chk("atoi(\"+7\")", atoi("+7"), 7);
    chk("atoi(前导空格)", atoi("   42"), 42);
    chk("atoi(尾部垃圾)", atoi("42abc"), 42);
    chk("atoi(空串)", atoi(""), 0);

    // ---- 往返：itoa 写出去，atoi 读回来 ----
    itoa(123456, buf);
    chk("往返 123456", atoi(buf), 123456);
    itoa(0 - 999, buf);
    chk("往返 -999", atoi(buf), 0 - 999);

    // ---- itoa_hex：大写、返回长度 ----
    n = itoa_hex(255, buf);
    chk("itoa_hex(255).len", n, 2);
    chks("itoa_hex(255)", buf, "FF");
    itoa_hex(0, buf);
    chks("itoa_hex(0)", buf, "0");
    itoa_hex(4660, buf);
    chks("itoa_hex(4660)", buf, "1234");

    // ---- atoi_hex：认 0x 前缀、大小写都收 ----
    chk("atoi_hex(\"FF\")", atoi_hex("FF"), 255);
    chk("atoi_hex(\"ff\")", atoi_hex("ff"), 255);
    chk("atoi_hex(\"0xFF\")", atoi_hex("0xFF"), 255);
    chk("atoi_hex(\"0X10\")", atoi_hex("0X10"), 16);
    chk("atoi_hex(\"0\")", atoi_hex("0"), 0);
    chk("atoi_hex(空串)", atoi_hex(""), 0);

    // ---- 位宽转换（真值表，每个都是确定值）----
    chk("byte_to_word(200)", byte_to_word(200), 200);
    chk("hword_to_word(0x1FFFF)", hword_to_word(0x1FFFF), 0xFFFF);
    chk("word_to_byte(0x1234)", word_to_byte(0x1234), 0x34);
    chk("word_to_hword(0x12345)", word_to_hword(0x12345), 0x2345);

    // ---- int_pow / int_sqrt ----
    chk("int_pow(2,10)", int_pow(2, 10), 1024);
    chk("int_pow(5,0)", int_pow(5, 0), 1);
    chk("int_sqrt(0)", int_sqrt(0), 0);
    chk("int_sqrt(1)", int_sqrt(1), 1);
    chk("int_sqrt(144)", int_sqrt(144), 12);
    chk("int_sqrt(145)", int_sqrt(145), 12);   // 向下取整
    chk("int_sqrt(143)", int_sqrt(143), 11);

    // ---- sum(int* arr, int n) ----
    int arr[4];
    arr[0] = 1;
    arr[1] = 2;
    arr[2] = 3;
    arr[3] = 4;
    chk("sum(n=4)", sum(arr, 4), 10);
    chk("sum(n=0)", sum(arr, 0), 0);

    // ---- 64 位：用**往返**判据，不依赖返回约定 ----
    long lv = 1234567890L;
    ltoa(lv, buf);
    chks("ltoa(1234567890)", buf, "1234567890");
    chk("atol(\"1234567890\")", (int)(atol(buf) == lv), 1);

    if (fails == 0) { print_str("PASS convert\n"); return 0; }
    print_str("FAIL convert count="); print_int(fails); newline();
    return 1;
}
