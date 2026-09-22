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

/* fflush —— 本平台的输出**无缓冲**（`putchar` 直接 `SYSCALL #4` 落笔），
 * 所以这里不需要真的"刷"，是个**语义正确**的空实现。
 *
 * ⚠ 但**必须存在**：老程序的进度条几乎都写
 *   `printf("...%d%%", p); fflush(stdout);` —— 少了这个符号就直接编不过
 *   （实测 `Examples/c/old/old_tty_progress.c` 报「未定义的函数 'fflush'」）。
 *
 * 返回 0 = 成功，与 C 标准一致；`fflush(NULL)`（刷全部流）传 0 也照收。
 * 参数写 `int *` 是为了与 `stdio.h` 的 `int fflush(FILE *stream)`（`FILE` = int）
 * 对得上 —— 本文件没有 include stdio.h，用不了 `FILE` 这个名字。 */
int fflush(int *stream) {
    (void)stream;
    return 0;
}

__stdcall int getchar(void) {
    /* ⚠ 把 asm 当**表达式**用（规则见 vmlui.c 头部）："先 asm(...) 再
       return c" 会把返回值丢掉 —— 实测 c 恒为垃圾，且不报错。本文件漏改。 */
    /* ⚠⚠ **R0 必须显式给 1 —— 而且必须写成 `MOVE R0 #1` 这条指令**：
       `SYSCALL #14`（与 `#5` 同）用 R0 区分两种语义：`R0=1` 阻塞读、
       其他值（含 `R0=0`）非阻塞探一下（见 `VMLRuntime.Syscall.cs` 的
       `ExecuteSyscall5_InputChar` 的 `bool blocking = registers[0] == 1;`）。

       本文件这里原来写的是 `asm("SYSCALL #5, ${1}")`，注释还写着"R0 必须显式给 1"
       —— **那是错的，而且不报错**：asm 模板里的 `${...}` 是**变量替换**
       （`${ch}` → 该变量所在的寄存器，见 `CodeGenerator.Statements.cs`），
       `${1}` 不是变量名，于是**什么都不生成**。实测生成出来的函数体里
       `syscall` 前面**一行都没有**（`git show HEAD:.../io.vml` 的 getchar 可查），
       ⇒ `getchar` 一直是**非阻塞**的：输入一空就 `if (!blocking) { R0 = 0; return; }`，
       程序读到的"字符"是 0，而下面那条 `InputExhausted` 分支**根本走不到**。

       这条正是「EOF 收不到」的真根因 —— 不是宿主不给 EOF，是**它从没被问到**。
       （`conio.c` 的 `kbhit` 里 `asm("SYSCALL #5, ${0}")` 与那句"R0=0 ⇒ 非阻塞"
       是同一个错法：它靠 R0 残留值"碰巧"非阻塞，而不是靠那行 asm。） */
    asm("MOVE R0 #1");              /* 阻塞读（字面指令，见 `builtins.c` 的同类写法） */
    /* ⚠ **走 `#14` 而不是 `#5`** —— 两者只差"输入源耗尽时给什么"：
       `#5` 给空行（0x0A，那是给 `conio.getch()` 的单键读用的），`#14` 给 **EOF(-1)**。
       stdio 的 `getchar` 必须是后者，否则老程序**最标准的那句**
           while ((c = getchar()) != EOF) { ... }
       **永远不结束**（实测：一直转到宿主超时被掐，输出 `VM execution cancelled`）。
       这一条是「老程序一行不改」的关键 —— 用 EOF 收尾的读法比哨兵字符常见得多。
       `fgetc(stdin)` / `getc(stdin)` 那几个也落在本函数上，同样受益。 */
    return asm("SYSCALL #14");
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
