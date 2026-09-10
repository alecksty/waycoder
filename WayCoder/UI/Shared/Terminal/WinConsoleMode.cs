namespace WayCoder.UI.Shared.Terminal;

/// <summary>
/// Windows 控制台输入模式管理 —— 启用 VT 输入（ENABLE_VIRTUAL_TERMINAL_INPUT）并关闭 quick-edit 模式。
///
/// 背景：WayCoder 的鼠标（以及未来可能用到的其它 VT 转义输入）依赖终端把 VT 字节流
/// （如 SGR 鼠标 \x1b[&lt;b;x;yM/m）以原始字节送进控制台输入缓冲。Windows 控制台宿主默认
/// 把输入按「记录流」处理（KEY_EVENT/MOUSE_EVENT 等输入记录），不把 VT 序列作为字节交付——
/// 只有输入模式设置 ENABLE_VIRTUAL_TERMINAL_INPUT(0x0200) 后，终端才会把 VT 字节流送进输入缓冲，
/// 此时用 <c>Console.OpenStandardInput()</c> 读原始字节才能收到鼠标序列。
/// 另需关闭 ENABLE_QUICK_EDIT_MODE(0x0040)：conhost 下 quick-edit 打开时鼠标点击做选区而非发事件，
/// 会吞掉鼠标点击。
///
/// 序列（?1006h SGR）与解析（ParseSgrMouse）在别处已就绪，本类只负责把 Windows 输入模式
/// 切到能接收 VT 字节流的状态。非 Windows / stdin 被重定向（管道/CI）时 Enable 为 no-op（返回 false）。
///
/// P/Invoke 采用 [LibraryImport("kernel32.dll")] 源生成风格（见 Infra/ProcEncoding.cs 的 GetOEMCP），
/// 与 TerminalRawMode 的 libc P/Invoke 一致；工程已 AllowUnsafeBlocks=true（WayCoder.csproj）。
/// </summary>
public static partial class WinConsoleMode
{
    private const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200;
    private const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
    // 关行缓冲/回显（0x0002/0x0004）：WindowsCharSource 走 OpenStandardInput 原生字节流，
    // 若行缓冲开着按键只在 Enter 成批到达、回显开着 conhost 把字符叠加回 TUI 自绘上（重复/鬼影，
    // 即「鼠标乱码」症状之一）。保留 PROCESSED_INPUT(0x0001) 让 Ctrl+C 仍由控制台处理。
    private const uint ENABLE_LINE_INPUT = 0x0002;
    private const uint ENABLE_ECHO_INPUT = 0x0004;

    private const int STD_INPUT_HANDLE = -10;

    private static bool _saved;
    private static uint _origMode;
    private static nint _handle; // Enable 实际作用的句柄（CONIN$ 场景下与 std stdin 不同）

    [LibraryImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true)]
    private static partial nint GetStdHandle(int nStdHandle);

    [LibraryImport("kernel32.dll", EntryPoint = "GetConsoleMode", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);

    [LibraryImport("kernel32.dll", EntryPoint = "SetConsoleMode", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetConsoleMode(nint hConsoleHandle, uint dwMode);

    /// <summary>
    /// 启用 VT 输入并关闭 quick-edit。幂等：已启用/已保存原 mode 时直接返回 true。
    /// 非 Windows、stdin 被重定向、或控制台句柄不可得时静默失败（返回 false）——
    /// 此时鼠标保持不可用（不强退），符合「鼠标是增强能力、缺失不致命」的定位。
    /// </summary>
    public static bool Enable() => Enable(nint.Zero);

    /// <summary>
    /// 同上，但作用在**指定句柄**上（该句柄须是控制台输入句柄，如 `CONIN$` 的
    /// SafeFileHandle）—— stdin 被重定向而改从控制台设备读键时用这个重载，
    /// 因为此时 GetStdHandle(STD_INPUT) 拿到的是管道、拿不到控制台模式。
    /// </summary>
    public static bool Enable(nint explicitHandle)
    {
        if (!OperatingSystem.IsWindows()) return false;
        if (explicitHandle == nint.Zero && Console.IsInputRedirected) return false;
        if (_saved) return true; // 已启用，幂等

        var handle = explicitHandle != nint.Zero ? explicitHandle : GetStdHandle(STD_INPUT_HANDLE);
        if (handle == nint.Zero || handle == (nint)(-1)) return false;
        if (!GetConsoleMode(handle, out var mode)) return false;

        // 打开 VT 输入 + 关 quick-edit（否则 conhost 鼠标点击做选区不发事件）+ 关行缓冲/回显
        // （原生字节流需 raw；否则按键回显叠加 TUI 自绘、行缓冲等 Enter 成批到——code-review finding）。
        var newMode = (mode | ENABLE_VIRTUAL_TERMINAL_INPUT) & ~(ENABLE_QUICK_EDIT_MODE | ENABLE_LINE_INPUT | ENABLE_ECHO_INPUT);
        if (!SetConsoleMode(handle, newMode)) return false;

        _origMode = mode;
        _handle = handle; // Disable 要作用在同一个句柄上（可能是 CONIN$ 而非 std stdin）
        _saved = true;
        return true;
    }

    /// <summary>恢复原始控制台输入模式（退出 TUI 时调用）。未启用则 no-op。</summary>
    public static void Disable()
    {
        if (!_saved) return;
        _saved = false;
        if (!OperatingSystem.IsWindows()) return;
        var handle = _handle != nint.Zero ? _handle : GetStdHandle(STD_INPUT_HANDLE);
        if (handle == nint.Zero || handle == (nint)(-1)) return;
        try { SetConsoleMode(handle, _origMode); } catch { /* 恢复失败不致命 */ }
    }
}
