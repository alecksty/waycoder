namespace VMLRuntime
{
    public enum SyscallNumber
    {
        // === 基本 I/O (1-10) ===
        OutputString = 1,
        InputString = 2,
        Exit = 3,
        OutputChar = 4,
        InputChar = 5,
        OutputInt = 6,
        InputInt = 7,
        OutputFloat = 8,
        InputFloat = 9,
        // 10: OutputHex — 已删除，无编译器使用

        // === 内存工具 (11-13) ===
        MemCopy = 11,    // R0=src, R1=dst, R2=len → R0=bytes copied
        MemFill = 12,    // R0=addr, R1=value, R2=len → R0=0
        MemCompare = 13, // R0=addr1, R1=addr2, R2=len → R0=0(equal)

        // === 内存管理 (40-41) ===
        Alloc = 40,
        Free = 41,

        // === 随机数/时间 (50-58, 61) ===
        Random = 50,
        Seed = 51,
        Sleep = 52,
        GetTick = 53,
        GetDateTime = 54,
        GetDateString = 55,
        GetTimeString = 56,
        SpeakerBeep = 57,    // R0=freq_hz, R1=duration_ms
        SetRTC = 58,         // R0=unix_timestamp → R0=0 success
        /* 本地时区偏移（秒，**东为正**；UTC 为 0）。
           ⚠ 为什么要有它：`#54` 给的是 **Unix 时间戳**，那是**与地区无关**的，
           而库里把它拆成 `struct tm` 的那段（`util.c` 的 `_ts_to_tm`）**是纯 UTC 换算**
           —— 于是 `localtime()` 与 `gmtime()` 一模一样，钟面小时差 8 小时
           （分/秒/日期都对，因为 UTC+8 是整小时 ⇒ 只错小时这一项，最容易看成"程序算错了"）。
           ⚠ **不动 `#54` 的语义**（它是时间戳，本来就该与地区无关），按本仓规矩
           「新能力一律走新号」另开一个号。 */
        GetUtcOffset = 61,

        /* ── 命令行参数 (62-63) ────────────────────────────────────────────
           宿主喂一份参数、程序按需取。**与特权级无关**（参数是 ABI/环境的事，不是内核能力），
           但**仍要登记进 `UserAllowed`** —— 白名单管的是"这一号允许不允许用"，
           漏登记会打 `Permission denied: syscall N requires kernel mode` 并把 R0 置成错误码。
           ⚠ 为什么不做成"给 `#59 GetInfo` 加一个 TypeId"：本仓铁律**新能力一律走新号** ——
             老程序那几只寄存器里是它自己上一句留下的值，宿主无从判断"这是不是真给了"。 */
        ArgCount = 62,  // 无参 → R0 = 参数个数（**含 argv[0]**，恒 ≥ 1）
        ArgGet = 63,    // R0=序号, R1=缓冲区, R2=容量 → R0 = 写入字节数（不含 NUL），失败 -1

        // === 系统信息 (59) ===
        GetInfo = 59,        // R0=TypeId → R0=string_addr (0=不支持)

        // === 配置查询 (60) ===
        GetConfig = 60,

        // === 调试 (70-72) ===
        DebugPrint = 70,
        DebugPrintInt = 71,
        Assert = 72,

        // === 设备 I/O (100-107) ===
        DeviceOpen = 100,
        DeviceClose = 101,
        DeviceRead = 102,
        DeviceWrite = 103,
        DeviceControl = 104,
        // 105: DeviceInfo — 已删除，无编译器使用
        EEPROM_Read = 106,   // R0=offset, R1=buf, R2=len → R0=bytes read
        EEPROM_Write = 107,  // R0=offset, R1=buf, R2=len → R0=bytes written

        // === 文件操作 (110-114) ===
        FileOpen = 110,
        FileClose = 111,
        FileRead = 112,
        FileWrite = 113,
        FileControl = 114,   // 原名 FileSeek，实际功能含 seek/truncate/size

        // === 图形/截屏 (200-203) ===
        DebugScreenshot = 200,
        PutImage = 201,      // R0=x, R1=y, R2=w, R3=h, R4=pixels → R0=0
        GetImage = 202,      // R0=x, R1=y, R2=w, R3=h, R4=buf → R0=0
        Viewport = 203,      // R0=x1, R1=y1, R2=x2, R3=y2 → clip region

        // === OS模式扩展 (300+) ===

        // 线程/并发 (300-303)
        ThreadCreate = 300,
        ThreadExit = 301,
        ThreadJoin = 302,
        ThreadYield = 303,
        // 304: ThreadSleep — 已删除，统一使用 Sleep(52)

        // 互斥锁 (310-316)
        MutexCreate = 310,
        MutexLock = 311,
        MutexUnlock = 312,
        CondCreate = 313,
        CondWait = 314,
        CondSignal = 315,
        CondBroadcast = 316,

        // 进程 (320-322)
        Exec = 320,
        // 321: ProcessExit — 已删除，统一使用 Exit(3)
        GetPID = 322,

        // 网络 (330-338)
        SocketCreate = 330,
        SocketBind = 331,
        SocketListen = 332,
        SocketAccept = 333,
        SocketConnect = 334,
        SocketSend = 335,
        SocketRecv = 336,
        SocketClose = 337,
        DnsResolve = 338,

        // 文件系统 OS (340-344)
        MkDir = 340,
        Remove = 341,
        Rename = 342,
        ReadDir = 343,
        Stat = 344,

        // 信号 (350)
        Signal = 350,

        // 环境变量 (360-362)
        GetEnv = 360,
        SetEnv = 361,
        GetArgs = 362,

        // 动态加载 (370-371)
        DLOpen = 370,
        DLSym = 371,

        // FFI 原生调用 (372-376)
        DLClose = 372,
        // NativeCall = 373,   // REMOVED: 统一使用 NativeCallEx
        GetPlatform = 374,
        // NativeCallF = 375,  // REMOVED: 统一使用 NativeCallEx
        NativeCallEx = 376,  // 统一类型数组 FFI：R0=funcId,R1=argsPtr,R2=typeDescPtr,R3=argCount,R4=retType

        // 反射 (380-381)
        TypeOf = 380,
        TypeName = 381,

        // === TTY 终端直接输出 (400-401) — 绕过 stdout 重定向 ===
        TTY_WriteChar = 400,    // R0=char — 直接写入终端
        TTY_WriteString = 401,  // R0=addr — 直接写入终端字符串
        TTY_PrintInt = 402,     // R0=int — 直接写入终端整数

        // === 宽字符/Unicode I/O (391-394) ===
        OutputWString = 391,  // R0=wchar_t*addr — 输出 16 位宽字符串
        InputWString = 392,   // R0=wchar_t*buf, R1=max_chars — 读入 16 位宽字符串
        OutputUString = 393,  // R0=char32_t*addr — 输出 32 位 Unicode 字符串
        InputUString = 394,   // R0=char32_t*buf, R1=max_chars — 读入 32 位 Unicode 字符串
    }

    public static class SyscallConstants
    {
        public static readonly HashSet<int> UserAllowed = new()
        {
            /* 14 = `#5` 的 EOF 版（输入源耗尽给 -1 而不是空行，stdio 的 `getchar` 用）。
               ⚠ **必须登记在这里**：用户态只放行本表的号，漏了会打出
               `Permission denied: syscall 14 requires kernel mode` 并把 R0 置成错误码
               —— 表现是 `getchar()` 恒返一个负数、老程序读到的"字符"全是垃圾。 */
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
            40, 41, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 70, 71, 72,
            100, 101, 102, 103, 104, 106, 107,
            110, 111, 112, 113, 114,
            200, 201, 202, 203,
            // OS模式系统调用(privilegeLevel=0时可用)
            300, 301, 302, 303,
            310, 311, 312, 313, 314, 315, 316,
            320, 322,
            330, 331, 332, 333, 334, 335, 336, 337, 338,
            340, 341, 342, 343, 344,
            350,
            360, 361, 362,
            370, 371, 372, 374, 376,
            380, 381,
            391, 392, 393, 394,
            400, 401, 402,
        };
    }
}
