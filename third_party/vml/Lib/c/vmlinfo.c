/*
 * VML info 共享库 — 系统信息 / 随机数 / 日期时间
 * 所有语言通过链接此库获取 VML 运行时信息
 */

/* 全局存储 — asm 结果通过此变量中转返回 */
int _vml_result_int = 0;
const char* _vml_result_str = 0;

/* ---- 声明外部汇编函数 ---- */

/* 输出字符串 (R0 = 字符串地址) */
void vml_print_str(const char* s);
void vml_print_str(const char* s) {
    asm("SYSCALL 1");
}

/* 输出整数 (R0 = 整数值) */
void vml_print_int(int n);
void vml_print_int(int n) {
    asm("SYSCALL 6");
}

/* 输出十六进制 (R0 = 整数值) */
void vml_print_hex(int n);
void vml_print_hex(int n) {
    asm("SYSCALL 10");
}

/* 输出字符 (R0 = 字符) */
void vml_putchar(char c);
void vml_putchar(char c) {
    asm("SYSCALL 4");
}

/* 输出新行 */
void vml_newline(void);
void vml_newline(void) {
    asm("LOAD R0 #10\nSYSCALL 4");
}

/* 输出浮点 (R0 = float) — SYSCALL 8 */
void vml_print_float(float f);
void vml_print_float(float f) {
    asm("SYSCALL 8");
}

/* 输出双精度 (R0/R1 = double) — 截断为 float32 (v1.66.41) */
void vml_print_double(double d);
void vml_print_double(double d) {
    float f = (float)d;
    asm("SYSCALL 8");
}

/* 输出长整数 (R0/R1 = int64) — 暂截断为 int32 (v1.66.41) */
void vml_print_long(long long n);
void vml_print_long(long long n) {
    int val = (int)n;
    asm("SYSCALL 6");
}

/* 输出布尔 (R0 = 0/1) — prints "true"/"false" */
void vml_print_bool(int b);
void vml_print_bool(int b) {
    if (b) { vml_print_str("true"); }
    else   { vml_print_str("false"); }
}

/* ---- 配置信息 ---- */

/* GetConfig(type) -> 返回值 */
int vml_getconfig(int type);
int vml_getconfig(int type) {
    asm("SYSCALL 60");
    asm("STORE R0, _vml_result_int");
    return _vml_result_int;
}

/* ---- 随机数 ---- */

/* Random() -> 随机整数 */
int vml_random(void);
int vml_random(void) {
    asm("SYSCALL 50");
    asm("STORE R0, _vml_result_int");
    return _vml_result_int;
}

/* ---- 日期时间 ---- */

/* GetDateString() -> 日期字符串地址 */
const char* vml_get_date(void);
const char* vml_get_date(void) {
    asm("SYSCALL 55");
    asm("STORE R0, _vml_result_str");
    return _vml_result_str;
}

/* GetTimeString() -> 时间字符串地址 */
const char* vml_get_time(void);
const char* vml_get_time(void) {
    asm("SYSCALL 56");
    asm("STORE R0, _vml_result_str");
    return _vml_result_str;
}

/* ---- 退出 ---- */

/* Exit(code) */
void vml_exit(int code);
void vml_exit(int code) {
    asm("SYSCALL 3");
}
