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
    /* asm 必须是表达式（见 vmlui.c 头部）—— 原来写 `asm(...); return 0;`，
       配置值整个丢掉、恒返回 0。 */
    return asm("SYSCALL 60");
}

/* ---- 随机数 ---- */

/* 获取随机数 (SYSCALL 50: Random)
 * 返回: 随机整数值
 */
__stdcall int random(void) {
    /* asm 必须是表达式（见 vmlui.c 头部）—— 原来恒返回 0。
       ⚠ `random` 在 `builtins.c` 里**也有一份**定义；实测链接器选了**这一份**
       （`call lib_sysinfo_random`），所以只修 builtins 那份没用。 */
    return asm("SYSCALL 50");
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
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL 54");
}

/* ---- 日期时间 ---- */

/* 获取日期字符串 (SYSCALL 55: GetDateString)
 * 返回: 日期字符串地址
 */
const char* get_date(void) {
    /* asm 必须是表达式（见 vmlui.c 头部）—— 原来恒返回 NULL */
    return asm("SYSCALL 55");
}

/* 获取时间字符串 (SYSCALL 56: GetTimeString)
 * 返回: 时间字符串地址
 */
const char* get_time(void) {
    /* asm 必须是表达式（见 vmlui.c 头部）—— 原来恒返回 NULL */
    return asm("SYSCALL 56");
}

/* ---- 退出 ---- */

/* 退出程序 (SYSCALL 3: Exit)
 * code: 退出码
 */
__stdcall void exit(int code) {
    asm("SYSCALL 3");
}
