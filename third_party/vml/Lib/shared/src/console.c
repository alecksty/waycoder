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
    char c;
    asm("SYSCALL #5");
    return c;
}

__stdcall char* input_str(void)
{
    char* buf;
    asm("SYSCALL #2");
    return buf;
}

__stdcall int input_int(void)
{
    int n;
    asm("SYSCALL #7");
    return n;
}

__stdcall float input_float(void)
{
    float f;
    asm("SYSCALL #9");
    return f;
}

__stdcall char* gets(void)
{
    char* buf;
    asm("SYSCALL #2");
    return buf;
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
