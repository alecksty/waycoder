// string 族 —— Lib/shared/src/string.c
//
// 判据（照 README 的约定）：
//   · 期望值**手算**，不从被测函数反推
//   · **同时查返回值和逐字节内容** —— 只查返回值会漏掉"长度对、内容写错"这种
//   · 不经过 stdio 做判据（C 的 puts 在字面量多的程序里会串行/重复，
//     会把 stdio 的毛病看成 codegen 的毛病）

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

// 把 buf 当字符串逐字节比对到 s 的结尾，多出/少出一个字符都算错
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
        print_str(" 尾部多出字节 got=");
        print_int(buf[i]);
        newline();
        fails = fails + 1;
    }
    return 0;
}

int main() {
    char buf[64];
    char *p;
    int n;

    // ---- strlen ----
    chk("strlen(\"\")", strlen(""), 0);
    chk("strlen(\"abc\")", strlen("abc"), 3);
    chk("strlen(\"hello, world\")", strlen("hello, world"), 12);

    // ---- strcmp ----
    chk("strcmp(=)", strcmp("abc", "abc"), 0);
    chk("strcmp(a<b)", strcmp("abc", "abd") < 0, 1);
    chk("strcmp(a>b)", strcmp("abd", "abc") > 0, 1);
    chk("strcmp(前缀短)", strcmp("ab", "abc") < 0, 1);

    // ---- strncmp ----
    chk("strncmp(前3同)", strncmp("abcdef", "abcxyz", 3), 0);
    chk("strncmp(n=0)", strncmp("abc", "xyz", 0), 0);
    chk("strncmp(第4位异)", strncmp("abcdef", "abcxyz", 4) < 0, 1);

    // ---- strcpy：返回值是 dst 本身 ----
    p = strcpy(buf, "hello");
    chks("strcpy 内容", buf, "hello");
    chk("strcpy 返回 dst", p == buf, 1);

    // ---- strcat ----
    strcpy(buf, "ab");
    strcat(buf, "cd");
    chks("strcat 内容", buf, "abcd");

    // ---- strncpy：**只拷 n 个，不补 NUL**（标准语义）----
    strcpy(buf, "XXXXXXXX");
    strncpy(buf, "abc", 2);
    chk("strncpy b0", buf[0], 97);   // 'a'
    chk("strncpy b1", buf[1], 98);   // 'b'
    chk("strncpy b2 未被写", buf[2], 88);  // 仍是 'X' —— 标准不补 NUL

    // ---- strchr / strstr ----
    // ⚠ 必须**先把字面量绑到变量**再相减：同一条语句里写两个 `"hello"` 字面量
    //   **不共享地址**（实测两处相距 4 字节）⇒ 指针相减得到的是垃圾。
    //   这是用例自己的 bug，不是库的。
    char *hay = "hello";
    p = strchr(hay, 108);            // 'l' 第一次出现 → 下标 2
    chk("strchr 命中下标", (int)(p - hay), 2);
    chk("strchr 未命中为 0", strchr(hay, 122) == 0, 1);   // 'z'

    char *h2 = "hello, world";
    p = strstr(h2, "world");
    chk("strstr 命中下标", (int)(p - h2), 7);
    chk("strstr 未命中为 0", strstr(hay, "xyz") == 0, 1);

    // ---- strrev：写到另一个缓冲区、源不动 ----
    n = strrev(buf, "abc");
    chks("strrev 内容", buf, "cba");
    chk("strrev 返回长度", n, 3);

    // ---- 大小写转换 ----
    str_toupper(buf, "aBc1");
    chks("str_toupper", buf, "ABC1");
    str_tolower(buf, "AbC1");
    chks("str_tolower", buf, "abc1");

    // ---- contains / indexof / startswith / endswith ----
    chk("str_contains 有", str_contains("hello, world", "o, w"), 1);
    chk("str_contains 无", str_contains("hello", "xyz"), 0);
    chk("str_indexof", str_indexof("hello", 108), 2);          // 'l'
    chk("str_indexof 未命中", str_indexof("hello", 122), -1);  // 'z'
    chk("str_startswith 是", str_startswith("hello", "hel"), 1);
    chk("str_startswith 否", str_startswith("hello", "ell"), 0);
    chk("str_endswith 是", str_endswith("hello", "llo"), 1);
    chk("str_endswith 否", str_endswith("hello", "hel"), 0);

    // ---- charat ----
    chk("str_charat", str_charat("hello", 1), 101);   // 'e'
    // ⚠ 越界返回 **0** —— 这是源码里写明的契约
    //   （`// returns 0 if index out of bounds`），不是 -1
    chk("str_charat 越界", str_charat("hello", 9), 0);

    // ---- str_repeat：把 src 重复 n 遍 ----
    str_repeat(buf, "ab", 3);
    chks("str_repeat", buf, "ababab");

    if (fails == 0) { print_str("PASS string\n"); return 0; }
    print_str("FAIL string count="); print_int(fails); newline();
    return 1;
}
