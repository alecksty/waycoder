using System.Text;
using WayCoder.Infra;

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

    static WinConsoleMode()
    {
        // 进程正常终结（含 Environment.Exit / Main 返回）时兜底还原。
        // 本类改的是**整个控制台输入缓冲区**的模式（`SetConsoleMode` 作用于 CONIN$ 即全控制台共享），
        // 即用户那扇 cmd/PowerShell 窗口的行缓冲与回显。若 TUI 因异常或直接退出没走到
        // TuiManager.Exit → Disable，父 shell 就被留在 raw、无回显状态。
        // 被强杀（taskkill / 崩溃）时本钩子不会跑，那种情况无解，靠下次启动的清残留逻辑缓解。
        AppDomain.CurrentDomain.ProcessExit += static (_, _) =>
        {
            try { Disable(); } catch { /* 退出路径，吞掉 */ }
        };
    }

    [LibraryImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true)]
    private static partial nint GetStdHandle(int nStdHandle);

    [LibraryImport("kernel32.dll", EntryPoint = "GetConsoleMode", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);

    [LibraryImport("kernel32.dll", EntryPoint = "SetConsoleMode", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetConsoleMode(nint hConsoleHandle, uint dwMode);

    [LibraryImport("kernel32.dll", EntryPoint = "GetConsoleCP", SetLastError = true)]
    private static partial uint GetConsoleCP();

    [LibraryImport("kernel32.dll", EntryPoint = "SetConsoleCP", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetConsoleCP(uint wCodePageID);

    [LibraryImport("kernel32.dll", EntryPoint = "IsDBCSLeadByteEx", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsDbcsLeadByteEx(uint codePage, byte testChar);

    private const uint CP_UTF8 = 65001;

    private static uint _origCp;            // 施加前的控制台输入代码页（0 = 未知/取不到）
    private static bool _inputIsUtf8;       // 输入代码页是否**实际**已是 UTF-8
    private static Encoding? _legacyInput;  // 非 UTF-8 时的回退解码（null = 不回退）

    /// <summary>
    /// 控制台输入字节当前**不是** UTF-8 时的回退解码编码（中文系统 = GBK）；已是 UTF-8 或取不到 → null。
    /// 供 <c>WindowsCharSource</c> 在「字节不是合法 UTF-8」时按它重解 —— 见 <see cref="EnsureInputCodePage"/>。
    /// </summary>
    public static Encoding? LegacyInputEncoding => _inputIsUtf8 ? null : _legacyInput;

    /// <summary>施加前的控制台输入代码页（0 = 未知）。回退解码要按它的字节规则切分 DBCS 字符。</summary>
    public static uint OriginalInputCodePage => _origCp;

    /// <summary>该代码页下此字节是否 DBCS 前导字节（需再吃 1 个尾字节）。非 Windows / 无代码页 → false。</summary>
    public static bool IsDbcsLead(uint codePage, byte b)
        => OperatingSystem.IsWindows() && codePage != 0 && IsDbcsLeadByteEx(codePage, b);

    /// <summary>
    /// 把**控制台输入代码页**切成 UTF-8，让 conhost 的 VT 输入字节按 UTF-8 交付。
    ///
    /// 为什么必须做：`WindowsCharSource` 是按 UTF-8 解码字节流的（状态化拼多字节字符），而 conhost
    /// 在 VT 输入模式下**用 `GetConsoleCP()` 编码非 ASCII 按键**——中文系统该值是 936(GBK)，于是
    /// 「打中文 → 输入缓冲里是 GBK 字节 → 按 UTF-8 解 → U+FFFD / 解成别的字符」= **TUI 里打字乱码**。
    /// 注意 `Program.Main` 的 `Console.OutputEncoding = UTF8` 只调 `SetConsoleOutputCP`，
    /// **输入侧完全没被改过**，所以「界面文字正常、自己打的字乱码」并存 —— 正是这个组合的症状。
    /// （v0.96.74 前 Windows 走 `Console.ReadKey`，拿的是输入记录里的 UTF-16 字符、不经过字节编码，
    /// 所以那时打中文是好的；改字节流之后才暴露。）
    ///
    /// 幂等且**每次进界面都重施**：裸 `!` 跑完 shell 命令 Exit→Enter 往返、或 `!chcp 936`，
    /// 都会把输入页改回去（同 <c>ReapplyConsoleMode</c> 的道理）。
    /// </summary>
    private static void EnsureInputCodePage()
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            var cp = GetConsoleCP();
            if (_origCp == 0) _origCp = cp;   // 只记第一次（那才是真正的「原始」值）
            if (cp != CP_UTF8) SetConsoleCP(CP_UTF8);
            _inputIsUtf8 = GetConsoleCP() == CP_UTF8; // 以实际生效为准（SetConsoleCP 可能失败）
            if (!_inputIsUtf8)
            {
                // 切不过去不致命（回退解码会兜住），但「打字乱码」的疑虑要能查 —— 记一行日志
                DebugLog.Log("console", $"输入代码页仍是 {GetConsoleCP()}（切 UTF-8 失败）→ 走原页回退解码");
            }
        }
        catch { _inputIsUtf8 = false; }
        // 切成功 → 输入就是 UTF-8，不需要回退；没切成 → 按原页（取不到就用系统 OEM 页）回退解码
        _legacyInput = _inputIsUtf8 ? null : (ProcEncoding.ForCodePage(_origCp) ?? ProcEncoding.OemEncoding);
    }

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
        if (_saved) { EnsureInputCodePage(); return true; } // 已启用：模式幂等，但输入页每次进界面重施

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
        // 输入代码页与输入模式是一对：字节流按 UTF-8 解，输入页就必须是 UTF-8（否则中文按键乱码）
        EnsureInputCodePage();
        return true;
    }

    /// <summary>恢复原始控制台输入模式与输入代码页（退出 TUI 时调用）。未启用则 no-op。</summary>
    public static void Disable()
    {
        if (!_saved) return;
        _saved = false;
        if (!OperatingSystem.IsWindows()) return;
        var handle = _handle != nint.Zero ? _handle : GetStdHandle(STD_INPUT_HANDLE);
        if (handle != nint.Zero && handle != (nint)(-1))
        {
            try { SetConsoleMode(handle, _origMode); } catch { /* 恢复失败不致命 */ }
        }
        // 代码页是控制台全局的（与模式一样作用于用户那扇窗口），同样要还原；
        // 留着 65001 会让父 shell 里的 cmd/批处理输出变样。
        if (_origCp != 0)
        {
            try { SetConsoleCP(_origCp); } catch { /* 恢复失败不致命 */ }
        }
        _inputIsUtf8 = false;
        _legacyInput = null;
    }
}
