// vsnprintf —— 格式化引擎。**所有 22 门语言的 printf/sprintf 都走它**。
//
// 这个用例的存在理由：`_printf_itoa` 被链接器后缀启发式误重定向到 `itoa`
// （参数顺序相反）时，`%d` 返回垃圾长度、缓冲区没被填，**而整个仓库没有一个用例会红**。
// 下面每一格都对着**手算的期望值**比，不看输出好不好看。

int fails = 0;

int fill_hash(char *b, int len) {
    int i;
    for (i = 0; i < len - 1; i++) b[i] = 35;   // '#'
    b[len - 1] = 0;
    return 0;
}

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

int main() {
    char b[64];
    char *bp;
    int args[4];
    int n;

    args[0] = 42;
    args[1] = 7;
    args[2] = 0 - 5;
    args[3] = 255;
    bp = b;

    // ---- %d：返回长度 + 逐字节内容（两者必须同时对；只查长度会漏掉"写了个 0"）----
    fill_hash(bp, 64);
    n = vsnprintf(bp, "N=%d", args, 1);
    chk("d.len", n, 4);
    chk("d.b0", b[0], 78);    // 'N'
    chk("d.b1", b[1], 61);    // '='
    chk("d.b2", b[2], 52);    // '4'
    chk("d.b3", b[3], 50);    // '2'

    // ---- 负数 ----
    args[0] = 0 - 5;
    fill_hash(bp, 64);
    n = vsnprintf(bp, "%d", args, 1);
    chk("neg.len", n, 2);
    chk("neg.b0", b[0], 45);  // '-'
    chk("neg.b1", b[1], 53);  // '5'

    // ---- %%：**不消耗实参**。nargs=0 也必须能输出一个 '%' ----
    fill_hash(bp, 64);
    n = vsnprintf(bp, "P=%%", args, 0);
    chk("pct.len", n, 3);
    chk("pct.b2", b[2], 37);  // '%'

    // ---- 纯字面量（对照组：这条路一直是好的）----
    fill_hash(bp, 64);
    n = vsnprintf(bp, "lit", args, 0);
    chk("lit.len", n, 3);
    chk("lit.b0", b[0], 108); // 'l'

    // ---- %s（实参是字符串地址）----
    args[0] = (int)"AB";
    fill_hash(bp, 64);
    n = vsnprintf(bp, "[%s]", args, 1);
    chk("s.len", n, 4);
    chk("s.b0", b[0], 91);   // '['
    chk("s.b1", b[1], 65);   // 'A'
    chk("s.b2", b[2], 66);   // 'B'
    chk("s.b3", b[3], 93);   // ']'

    // ---- %c ----
    args[0] = 7;
    fill_hash(bp, 64);
    n = vsnprintf(bp, "%c", args, 1);
    chk("c.len", n, 1);
    chk("c.b0", b[0], 7);

    // ---- %x ----
    args[0] = 255;
    fill_hash(bp, 64);
    n = vsnprintf(bp, "%x", args, 1);
    chk("x.len", n, 2);
    chk("x.b0", b[0], 102);  // 'f'
    chk("x.b1", b[1], 102);  // 'f'

    // ---- 宽度 / 左对齐 / 补零 ----
    args[0] = 42;
    fill_hash(bp, 64);
    n = vsnprintf(bp, "%5d", args, 1);          // args[0] = 42
    chk("w.len", n, 5);
    chk("w.b0", b[0], 32);   // ' '
    chk("w.b4", b[4], 50);   // '2'

    fill_hash(bp, 64);
    n = vsnprintf(bp, "%-5d", args, 1);
    chk("left.len", n, 5);
    chk("left.b0", b[0], 52); // '4'
    chk("left.b4", b[4], 32); // ' '

    fill_hash(bp, 64);
    n = vsnprintf(bp, "%05d", args, 1);
    chk("zero.len", n, 5);
    chk("zero.b0", b[0], 48); // '0'
    chk("zero.b4", b[4], 50); // '2'

    if (fails == 0) { print_str("PASS vsnprintf\n"); return 0; }
    print_str("FAIL vsnprintf count="); print_int(fails); newline();
    return 1;
}
