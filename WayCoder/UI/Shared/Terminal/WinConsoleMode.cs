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

    private const int STD_INPUT_HANDLE = -10;

    private static bool _saved;
    private static uint _origMode;

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
    public static bool Enable()
    {
        if (!OperatingSystem.IsWindows() || Console.IsInputRedirected) return false;
        if (_saved) return true; // 已启用，幂等

        var handle = GetStdHandle(STD_INPUT_HANDLE);
        if (handle == nint.Zero || handle == (nint)(-1)) return false;
        if (!GetConsoleMode(handle, out var mode)) return false;

        // 打开 VT 输入 + 关 quick-edit（否则 conhost 鼠标点击做选区不发事件）。
        var newMode = (mode | ENABLE_VIRTUAL_TERMINAL_INPUT) & ~ENABLE_QUICK_EDIT_MODE;
        if (!SetConsoleMode(handle, newMode)) return false;

        _origMode = mode;
        _saved = true;
        return true;
    }

    /// <summary>恢复原始控制台输入模式（退出 TUI 时调用）。未启用则 no-op。</summary>
    public static void Disable()
    {
        if (!_saved) return;
        _saved = false;
        if (!OperatingSystem.IsWindows() || Console.IsInputRedirected) return;
        var handle = GetStdHandle(STD_INPUT_HANDLE);
        if (handle == nint.Zero || handle == (nint)(-1)) return;
        try { SetConsoleMode(handle, _origMode); } catch { /* 恢复失败不致命 */ }
    }
}
