#param lib("sysinfo")

// VML Shared SysInfo64 Library — 64-bit System Info

// datetime64 — 返回64位Unix时间戳 (避免year 2038问题)
__stdcall long ldatetime64(void) {
    // 使用32位datetime加上epoch偏移
    int now32 = datetime();
    return (long)(unsigned int)now32;
}
