/*
 * sysinfo.c — VML 系统信息共享库
 * 所有语言通过此库获取 VML 运行时信息（配置/随机数/日期时间/退出）
 * 编译: make 或在项目根目录执行对应编译器命令
 */

/* ---- 配置信息 ---- */

/* 获取系统配置 (SYSCALL 60: GetConfig)
 * type: 7=MemorySize, 8=DisplayWidth, 9=DisplayHeight,
 *       12=StackSize, 13=HeapBase
 * 返回: 配置值（在 R0 中）
 */
__stdcall int getconfig(int type) {
    asm("SYSCALL 60");
    return 0;
}

/* ---- 随机数 ---- */

/* 获取随机数 (SYSCALL 50: Random)
 * 返回: 随机整数值
 */
__stdcall int random(void) {
    asm("SYSCALL 50");
    return 0;
}

/* 设置随机种子 (SYSCALL 51: Seed)
 * seed: 种子值
 */
__stdcall void srand(int seed) {
    asm("SYSCALL 51");
}

/* 获取 Unix 时间戳 (SYSCALL 54: GetDateTime)
 * 返回: Unix 时间戳（秒）
 */
__stdcall int datetime(void) {
    int ts;
    asm("SYSCALL 54");
    return ts;
}

/* ---- 日期时间 ---- */

/* 获取日期字符串 (SYSCALL 55: GetDateString)
 * 返回: 日期字符串地址
 */
const char* get_date(void) {
    asm("SYSCALL 55");
    return 0;
}

/* 获取时间字符串 (SYSCALL 56: GetTimeString)
 * 返回: 时间字符串地址
 */
const char* get_time(void) {
    asm("SYSCALL 56");
    return 0;
}

/* ---- 退出 ---- */

/* 退出程序 (SYSCALL 3: Exit)
 * code: 退出码
 */
__stdcall void exit(int code) {
    asm("SYSCALL 3");
}
