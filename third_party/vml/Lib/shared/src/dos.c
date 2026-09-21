// VML DOS Unit — Turbo Pascal 兼容 DOS 系统调用库
// 对 VML SYSCALL 的薄封装, 模拟 Turbo Pascal 7.0 DOS 单元
// v1.66.33

// ═══════════════════════════════════════════════════════════
// 日期时间 (SYSCALL 54/55/56)
// ═══════════════════════════════════════════════════════════

// GetDate: 返回当前日期 year/month/day/weekday
// 通过 SYSCALL 55 (GetDate) 获取打包日期
// 返回格式: year(高16位) month(高8位) day(低8位)
// ⚠ 这两处原来写的是 `asm("STORE R0, _vml_result_int")` —— **`STORE` 不是有效指令，
//    而且 `_vml_result_int` 在本文件里从未声明** ⇒ 整个模块**编译不过**
//    （GenLib 每次 `-b` 都在这里报 2 个错，是那条流水线上唯一的红点）。
//    取返回值要用 **asm 表达式**（见 vmlui.c 头部），不要自己造搬运指令。
//
// ⚠ 打包格式（`>>16` / `>>8` / `&0xFF`）是**照 GetTime 原来的写法推的、未实测**；
//    `wday` 原文件给的是硬编码常量，这里改成 0 —— 都是占位，别当契约用。
__stdcall void GetDate(int* year, int* month, int* day, int* wday) {
    int d = asm("SYSCALL #55");  // R0 = packed date
    if (year)  *year  = (d >> 16) & 0xFFFF;
    if (month) *month = (d >> 8) & 0xFF;
    if (day)   *day   = d & 0xFF;
    if (wday)  *wday  = 0;
}

__stdcall void GetTime(int* hour, int* min, int* sec, int* msec) {
    int t = asm("SYSCALL #56");  // R0 = packed time
    if (hour) *hour = (t >> 16) & 0xFF;
    if (min)  *min  = (t >> 8) & 0xFF;
    if (sec)  *sec  = t & 0xFF;
    if (msec) *msec = 0;
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
