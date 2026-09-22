/*
 * VML Console I/O — C 源码版本
 * 替代各语言目录下手写的 console.vml (如 Lib/kotlin/console.vml)
 * 编译为 console.vml，由 builtins.c 的 #param lib("console") 链接
 *
 * 提供所有语言通用的控制台输入输出包装函数
 */

// ===== 输出函数 =====

__stdcall void putchar(char ch)
{
    asm("SYSCALL #4");
}

__stdcall void puthex(int val)
{
    asm("SYSCALL #10");
}

__stdcall void putfloat(float f)
{
    asm("SYSCALL 8");
}

__stdcall void print_str(const char* str)
{
    asm("SYSCALL #1");
}

__stdcall void print_str_no_nl(const char* str)
{
    asm("CALL print_str_no_nl_impl");
}

// 内部实现: 没有C标准puts，使用SYSCALL
static void print_str_no_nl_impl(const char* str)
{
    asm("SYSCALL #1");
}

__stdcall void print_int(int val)
{
    asm("SYSCALL #6");
}

__stdcall void print_hex(int val)
{
    asm("SYSCALL #10");
}

__stdcall void print_float(float f)
{
    asm("SYSCALL 8");
}

__stdcall void print_bool(int b)
{
    if (b) {
        print_str("true");
    } else {
        print_str("false");
    }
}

__stdcall void println_str(const char* str)
{
    asm("SYSCALL #1");
    asm("MOVE R0 #10");
    asm("SYSCALL #4");
}

__stdcall void println_int(int val)
{
    asm("SYSCALL #6");
    asm("MOVE R0 #10");
    asm("SYSCALL #4");
}

__stdcall void println_hex(int val)
{
    asm("SYSCALL #10");
    asm("MOVE R0 #10");
    asm("SYSCALL #4");
}

__stdcall void newline(void)
{
    asm("MOVE R0 #10");
    asm("SYSCALL #4");
}

__stdcall void clear_screen(void)
{
    // ANSI/VT100 escape sequence
    print_str("\033[2J\033[H");
}

// ===== 输入函数 =====

__stdcall char getchar(void)
{
    /* asm 必须是表达式（见 vmlui.c 头部）：写成语句 + return 局部变量
       会把返回值丢掉（局部变量是未初始化的垃圾），且不报错。 */
    return asm("SYSCALL #5");
}

__stdcall char* input_str(void)
{
    /* asm 必须是表达式（见 vmlui.c 头部）：写成语句 + return 局部变量
       会把返回值丢掉（局部变量是未初始化的垃圾），且不报错。 */
    return asm("SYSCALL #2");
}

__stdcall int input_int(void)
{
    /* asm 必须是表达式（见 vmlui.c 头部）：写成语句 + return 局部变量
       会把返回值丢掉（局部变量是未初始化的垃圾），且不报错。 */
    return asm("SYSCALL #7");
}

__stdcall float input_float(void)
{
    /* asm 必须是表达式（见 vmlui.c 头部）：写成语句 + return 局部变量
       会把返回值丢掉（局部变量是未初始化的垃圾），且不报错。 */
    return asm("SYSCALL #9");
}

/* ⚠ 这个函数**原来叫 `gets`** —— 名字是错的，改名 `read_string`（2026-09-23）。
 *
 * `SYSCALL #2` 是"读一行到**内部缓冲区**并返回那个指针"，与 C 标准的
 * `gets(char *s)`（把行读进**调用方给的缓冲区**）是两件不同的事。
 * 而 `Lib/c/stdio.h:52` 声明的正是标准那个 `char *gets(char *s);`
 * ⇒ **同名、签名不同**：老程序写 `while (gets(buf))`（老 C 最典型的读循环）时，
 * `buf` 被当参数压进去、**被本函数彻底忽略**（它没有形参），
 * 于是 `buf` 永远填不上、程序读到自己的旧内容 —— **不报错、结果错**。
 * 这正是「同名函数只能有一份定义」那条规矩被违反的后果。
 * 改名后标准 `gets` 由下面的实现顶替（已核对：全库**没有任何调用点**调零参版本）。 */
__stdcall char* read_string(void)
{
    /* asm 必须是表达式（见 vmlui.c 头部）：写成语句 + return 局部变量
       会把返回值丢掉（局部变量是未初始化的垃圾），且不报错。 */
    return asm("SYSCALL #2");
}

/* C 标准的 `gets`：读一行到**调用方给的缓冲区** `s`，去掉行尾的 `\n`，返回 `s`。
 *
 * ## 为什么要自己读字符，不用现成的 `read_line`
 *
 * `read_line`（`readline.c`）走 `SYSCALL #5`，而 **`#5` 在输入源耗尽时返回 `0x0A`**
 * —— 那是给 `conio.getch()` 的单键读定的语义 ⇒ 它**区分不出"空行"与"EOF"**，
 * 两者都长得像"读到一个换行"。而 `gets` 必须在 EOF 返回 `NULL`（老程序的
 * `while (gets(buf))` 就是靠这个 `NULL` 收尾的）。
 * 所以逐字符用 **`SYSCALL #14`**（耗尽给 -1）自己读 —— 与 `io.c` 的 `getchar` 同一条链。
 *
 * ⚠ 与标准 `gets` 一致：**不做长度上限检查**（老程序依赖这个"不管多长都读得下"）。
 * 这也是 `gets` 后来被 C11 删掉的原因，但兼容老程序就是不能改这条语义。
 * ⚠ `CR` 直接丢掉（DOS 文本文件的行尾是 `\r\n`，老代码把它当 `\n` 用）。 */
__stdcall char* gets(char* s)
{
    char* p = s;
    for (;;) {
        int c;
        asm("MOVE R0 #1");        /* 阻塞读（字面指令，见 io.c 的 getchar 说明） */
        c = asm("SYSCALL #14");   /* 带 EOF 语义：输入耗尽给 -1 */
        if (c == -1) {
            /* 一个字符都没读到 ⇒ 真 EOF，返回 NULL（老程序靠它收尾） */
            if (p == s) return 0;
            break;                /* 读到了半行 ⇒ 当作行尾 */
        }
        if (c == '\n') break;
        if (c == '\r') continue;
        *p = (char)c;
        p = p + 1;
    }
    *p = 0;
    return s;
}

__stdcall int kb_hit(void)
{
    // 简化: 轮询键盘状态
    // 在真实实现中会检查键盘数据寄存器
    int status;
    // 返回-1表示没有按键
    return -1;
}

// ===== 多参数 printf 包装器 =====

// printf 可变参数通过 push 传给 printf2/printf3 系列
__stdcall void printf2(const char* fmt, int a1, int a2)
{
    asm("MOVE R0 fmt");
    asm("MOVE R1 a1");
    asm("MOVE R2 a2");
    asm("CALL printf2");
    asm("ADD R13 #12");
}

__stdcall void printf3(const char* fmt, int a1, int a2, int a3)
{
    asm("MOVE R0 fmt");
    asm("MOVE R1 a1");
    asm("MOVE R2 a2");
    asm("MOVE R3 a3");
    asm("CALL printf3");
    asm("ADD R13 #16");
}
