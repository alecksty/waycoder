// VML DOS Unit — Turbo Pascal 兼容 DOS 系统调用库
// 对 VML SYSCALL 的薄封装, 模拟 Turbo Pascal 7.0 DOS 单元
// v1.66.33

// ═══════════════════════════════════════════════════════════
// 日期时间 (SYSCALL 54/55/56)
// ═══════════════════════════════════════════════════════════

// GetDate: 返回当前日期 year/month/day/weekday
// 通过 SYSCALL 55 (GetDate) 获取打包日期
// 返回格式: year(高16位) month(高8位) day(低8位)
// ⚠ 这段的历史，两条都记下来免得再踩：
//   ① 原来写的是 `asm("STORE R0, _vml_result_int")` —— **`STORE` 不是有效指令，
//      而且 `_vml_result_int` 在本文件里从未声明** ⇒ 整个模块**编译不过**
//      （GenLib 每次 `-b` 都在这儿报 2 个错，是那条流水线上唯一的红点）。
//      取返回值要用 **asm 表达式**（规则见 vmlui.c 头部），不要自己造搬运指令。
//   ② 改成 asm 表达式后我又按 `>>16 / >>8 / &0xFF` 的**位段**解析了一版 ——
//      **那仍然是错的**：实测 `#55` 返回的是 `"2026-09-21"` 的**字符串地址**、
//      `#56` 是 `"23:00:44"` 的地址（不是打包整数）。现在按**字符串**解析。
extern int atoi(const char* s);

/* Turbo Pascal 的 `GetDate(var y, m, d, dow: word)` 风格 */
__stdcall void GetDate(int* year, int* month, int* day, int* wday) {
    char* s = (char*)asm("SYSCALL #55");   /* "YYYY-MM-DD" */
    if (year)  *year  = atoi(s);           /* 从串首取年 */
    if (month) *month = atoi(s + 5);       /* 跳过 "YYYY-" */
    if (day)   *day   = atoi(s + 8);       /* 跳过 "YYYY-MM-" */
    if (wday)  *wday  = 0;                 /* 星期本平台不给，恒 0（占位） */
}

/* Turbo Pascal 的 `GetTime(var h, m, s, hund: word)` 风格（无毫秒，DOS 只有百分秒） */
__stdcall void GetTime(int* hour, int* min, int* sec, int* msec) {
    char* s = (char*)asm("SYSCALL #56");   /* "HH:MM:SS" */
    if (hour) *hour = atoi(s);
    if (min)  *min  = atoi(s + 3);
    if (sec)  *sec  = atoi(s + 6);
    if (msec) *msec = 0;                   /* 本平台不提供百分秒 */
}

/* ═══════════════════════════════════════════════════════════
 * Turbo C 风格（`dos.h` 那一套，见 Lib/c/dos.h）
 * ═══════════════════════════════════════════════════════════ */

struct vml_date { int da_year; char da_day; char da_mon; };
struct vml_time { unsigned char ti_min, ti_hour, ti_hund, ti_sec; };

extern void delay(int ms);                 /* util.c — SYSCALL #52 */

__stdcall void sleep(unsigned seconds) {
    delay((int)seconds * 1000);
}

/* PC 喇叭。⚠ 语义与 DOS 有差：DOS 的 `sound` 是"开始响、一直响到 nosound"，
   本平台的音效原语是"响固定时长"（宿主上限 5000ms）⇒ 这里取最长的 5 秒近似。
   老程序那种 `sound(440); delay(100); nosound();` 的写法听感与 DOS 完全一致。 */
__stdcall void sound(unsigned freq) {
    asm("SYSCALL #57, ${freq}, ${5000}");
}

__stdcall void nosound(void) {
    asm("SYSCALL #542");                   /* AudioStop —— 立刻掐掉正在响的那一段 */
}

__stdcall void getdate(struct vml_date* d) {
    char* s = (char*)asm("SYSCALL #55");   /* "YYYY-MM-DD" */
    if (!d) return;
    d->da_year = atoi(s);
    d->da_mon  = (char)atoi(s + 5);
    d->da_day  = (char)atoi(s + 8);
}

__stdcall void gettime(struct vml_time* t) {
    char* s = (char*)asm("SYSCALL #56");   /* "HH:MM:SS" */
    if (!t) return;
    t->ti_hour = (unsigned char)atoi(s);
    t->ti_min  = (unsigned char)atoi(s + 3);
    t->ti_sec  = (unsigned char)atoi(s + 6);
    t->ti_hund = 0;
}

// 简单日期时间 (返回打包值)
__stdcall int DosVersion(void) {
    return 0x0700;  // 模拟 DOS 7.0
}

// ═══════════════════════════════════════════════════════════
// 磁盘操作
// ═══════════════════════════════════════════════════════════

__stdcall int DiskFree(int drive) {
    // 返回可用磁盘空间 (KB)
    // VML 不直接管理磁盘, 返回大值表示"足够"
    return 65535;
}

__stdcall int DiskSize(int drive) {
    return 65535;
}

// ═══════════════════════════════════════════════════════════
// 文件搜索 (FindFirst/FindNext/FindClose)
// ═══════════════════════════════════════════════════════════

// 简化的 SearchRec 结构: name(80) + size(4) + attr(1) + time(4) + date(4)
// VML 简化实现: name 字段有效, 其余为默认值
__stdcall int FindFirst(const char* path, int attr, void* sr) {
    // VML 环境下简化: 返回 0 (未找到) 表示没有匹配文件
    // 实际使用时需要文件系统支持
    return 0;  // 简化: 未实现, 返回 0 = no match
}

__stdcall int FindNext(void* sr) {
    return 0;
}

__stdcall void FindClose(void* sr) {
    // nothing
}

// ═══════════════════════════════════════════════════════════
// 环境变量
// ═══════════════════════════════════════════════════════════

__stdcall int EnvCount(void) {
    return 0;  // VML MCU 模式无环境变量
}

__stdcall char* EnvStr(int index) {
    return (char*)0;  // null
}

__stdcall char* GetEnv(const char* name) {
    return (char*)0;
}

// ═══════════════════════════════════════════════════════════
// 执行程序
// ═══════════════════════════════════════════════════════════

__stdcall int Exec(const char* path, const char* args) {
    return -1;  // VML MCU 模式不支持 Exec
}

// ═══════════════════════════════════════════════════════════
// 中断向量 (SwapVectors/GetIntVec/SetIntVec)
// ═══════════════════════════════════════════════════════════

__stdcall void SwapVectors(void) { /* VML 无 DOS 中断向量 */ }

__stdcall void GetIntVec(int intno, void* vector) {
    // 简化: 返回 null
    if (vector) *(void**)vector = 0;
}

__stdcall void SetIntVec(int intno, void* vector) {
    // 简化: 无操作
}

// ═══════════════════════════════════════════════════════════
// 杂项
// ═══════════════════════════════════════════════════════════

__stdcall int DosExitCode(void) {
    return 0;
}
