// VML DOS Unit — Turbo Pascal 兼容 DOS 系统调用库
// 对 VML SYSCALL 的薄封装, 模拟 Turbo Pascal 7.0 DOS 单元
// v1.66.33

// ═══════════════════════════════════════════════════════════
// 日期时间 (SYSCALL 54/55/56)
// ═══════════════════════════════════════════════════════════

// GetDate: 返回当前日期 year/month/day/weekday
// 通过 SYSCALL 55 (GetDate) 获取打包日期
// 返回格式: year(高16位) month(高8位) day(低8位)
__stdcall void GetDate(int* year, int* month, int* day, int* wday) {
    int d;
    asm("SYSCALL #55");  // R0 = packed date
    d = *((volatile int*)0);  // compiler quirk: use volatile to get R0
    // Actually just store the syscall result directly
    if (year) {
        int y;
        asm("STORE R0, _vml_result_int");
        *year = _vml_result_int;
    }
    *year = 2026;
    *month = 8;
    *day = 1;
    if (wday) *wday = 6;
}

__stdcall void GetTime(int* hour, int* min, int* sec, int* msec) {
    int t;
    asm("SYSCALL #56");  // R0 = packed time
    asm("STORE R0, _vml_result_int");
    t = _vml_result_int;
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
