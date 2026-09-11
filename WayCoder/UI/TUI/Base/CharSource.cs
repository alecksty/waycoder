using WayCoder.UI.Shared.Terminal;

namespace WayCoder.UI.TUI.Base;

/// <summary>
/// 终端字符源抽象 —— InputManager 解析层透过它「判断是否有输入 + 读一个字符」，
/// 与底层读取通道解耦。这是统一字节源重构的关键：macOS/Linux（真实 PTY）与 Windows
/// （VT 输入）用不同实现，但解析逻辑（TryParseEscapeSequence/ParseSgrMouse 等）只依赖此抽象。
///
/// 设计要点：
/// - <see cref="TryReadChar"/>：非阻塞「有字符则读并返回 true」，返回的 char 是当前解析单元
///   （可打印字符或 ASCII 控制符如 ESC=0x1B、RS=0x1E）。Windows 字节流实现需自行把多字节
///   UTF-8 聚合成一个 char、并跳过终端注入的孤立字节（如 SGR 鼠标序列后的 \x1e 分隔）。
/// - <see cref="WaitChar"/>：阻塞等待最多 timeoutMs，超时返回 false（忙等实现，调用方保证
///   已在后台线程，不会阻塞主循环）。
/// - 调用方（InputManager）保证单读者：本抽象只在泵线程被调用。
///
/// 可移植性：Unix 实现依赖 Console.KeyAvailable/ReadKey（真实 PTY 字节流原生可读）；
/// Windows 实现依赖 Console.OpenStandardInput + WinConsoleMode 已开 VT 输入。
/// 实现须幂等、线程安全于后台泵线程上下文，绝不触碰控件树。
/// </summary>
public interface ICharSource
{
    /// <summary>非阻塞读一个字符。有输入返回 true 并写 out c；无输入返回 false。EOF 返回 false。</summary>
    bool TryReadChar(out char c);

    /// <summary>
    /// 非阻塞读一个完整按键（含 ConsoleKey——方向键/功能键等 KeyChar='\0' 的键）。
    /// 主泵线程用它读「主键」，确保带 ConsoleKey 语义的键（方向键/F1 等）不丢失。
    /// 返回 false 表示无输入。EOF 返回 false。
    /// </summary>
    bool TryReadKey(out ConsoleKeyInfo key);

    /// <summary>是否可能有输入待读（仅用于低成本轮询提示；可为近似判断）。</summary>
    bool HasInput { get; }
}

/// <summary>
/// Unix/macOS 字符源 —— 复用现有 Console.KeyAvailable + Tty.ReadKey（真实 PTY 上
/// SGR 鼠标字节以字符流入stdin，ReadKey 原生可读）。行为与旧 InputManager 完全一致，
/// 确保 macOS/Linux 零回归。
/// </summary>
public sealed class UnixCharSource : ICharSource
{
    public bool HasInput => Console.KeyAvailable;

    public bool TryReadChar(out char c)
    {
        c = '\0';
        if (!Console.KeyAvailable) return false;
        var key = Tty.ReadKey(); // Console.ReadKey(intercept:true)
        c = key.KeyChar;
        return true;
    }

    public bool TryReadKey(out ConsoleKeyInfo key)
    {
        key = default;
        if (!Console.KeyAvailable) return false;
        key = Tty.ReadKey(); // 完整 ConsoleKeyInfo（方向键/功能键 KeyChar='\0' 但 Key 有语义）
        return true;
    }
}

/// <summary>
/// 控制台设备 —— stdin 被重定向时改从这里读键，让「不带参数但 stdin 不接键盘」的场景
/// （被别的程序拉起、脚本调用、`waycoder &lt; file`、双击启动器）同样能启动全屏界面。
/// </summary>
public static class ConsoleDevice
{
    /// <summary>
    /// 能否启动全屏界面（纯逻辑，便于自测）。
    ///
    /// 判据不是「stdin 是否被重定向」—— stdin 被占不等于没有终端：被别的程序拉起时
    /// 进程往往仍挂着可用的控制台，只是键盘不走 stdin。真正的判据是
    /// **有没有画布（stdout 是终端）+ 拿不拿得到键盘（stdin 是终端，或 Windows 上能开 CONIN$）**。
    ///
    /// Unix 不算 CONIN$ 这条路：`Console.ReadKey` 的 raw mode 绑在 stdin 上，
    /// 单独打开 `/dev/tty` 没进 raw mode，按键要等回车才到（等于不能交互）。
    /// </summary>
    public static bool CanUseFullScreen(bool stdinRedirected, bool stdoutRedirected,
        bool hasConsoleDevice, bool isWindows)
        => !stdoutRedirected && (!stdinRedirected || (isWindows && hasConsoleDevice));

    /// <summary>按真实环境判断能否启动全屏界面。</summary>
    public static bool CanUseFullScreen()
    {
        // 逐项短路，勿写成「四个实参一次求值」：C# 会先求值全部实参，而 hasConsoleDevice 那一项
        // 要真的开一个 CONIN$ 句柄 —— 无条件求值等于每次 Windows 启动都开一个控制台句柄再丢掉
        // （stdout 重定向时根本用不上），而真正读键那次 OpenInput() 还会另开一个。见 HasConsoleDevice。
        if (Console.IsOutputRedirected) return false;     // 没有画布
        if (!Console.IsInputRedirected) return true;      // stdin 接键盘，不用碰控制台设备
        if (!OperatingSystem.IsWindows()) return false;   // Unix：/dev/tty 没进 raw mode，按键要等回车
        return HasConsoleDevice();
    }

    /// <summary>能否打开控制台输入设备（探测用：句柄当即释放，不作读键通道——真正的通道在 OpenInput）。
    /// 探测与读键各开一次是有意的：读键那条流的生命周期归 InputManager。</summary>
    private static bool HasConsoleDevice()
    {
        var probe = TryOpen();
        if (probe == null) return false;
        probe.Dispose();
        return true;
    }

    /// <summary>
    /// 本次会话是否有**真正可读的键盘通道** —— InputManager 据此决定启不启动后台读键泵线程。
    ///
    /// 不能用 <c>Console.IsInputRedirected</c> 代替：本类存在的意义恰恰是「stdin 被重定向仍可能拿得到
    /// 键盘」。那种环境现在也会进 TUI（<see cref="CanUseFullScreen()"/>），若泵线程按重定向早退，
    /// 结果是 **TUI 起来了但按键全无反应**，而且挂在泵线程上的心跳（spinner 动画 / 冻结看门狗 /
    /// CPU 采样）一起停摆，只有 Ctrl+C 能逃出去。反之 <paramref name="fromDevice"/> 为 false
    /// 且 stdin 被重定向 = 手上只有一个空管道，读它毫无意义，那才是真的该早退。
    /// </summary>
    public static bool HasKeyboard(bool stdinRedirected, bool fromDevice)
        => !stdinRedirected || fromDevice;

    /// <summary>打开控制台输入设备（Windows `CONIN$` / Unix `/dev/tty`）；没有控制台则返回 null。</summary>
    public static Stream? TryOpen()
    {
        try
        {
            return new FileStream(OperatingSystem.IsWindows() ? "CONIN$" : "/dev/tty",
                FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        catch { return null; }
    }

    /// <summary>选择读键用的事件流：stdin 接键盘就用 stdin；被重定向则尝试控制台设备。
    /// 第二个返回值 = 是否来自控制台设备（决定 WinConsoleMode 要作用在哪个句柄上）。</summary>
    public static (Stream Stream, bool FromDevice) OpenInput()
    {
        if (!Console.IsInputRedirected || !OperatingSystem.IsWindows())
            return (Console.OpenStandardInput(), false);
        var dev = TryOpen();
        return dev != null ? (dev, true) : (Console.OpenStandardInput(), false);
    }
}

/// <summary>
/// Windows 字符源 —— 读 stdin 原始字节流（Console.OpenStandardInput），聚合成 char ，
/// 使 VT 输入的 SGR 鼠标字节流可被解析。前置条件：WinConsoleMode.Enable 已开 ENABLE_VIRTUAL_TERMINAL_INPUT
/// （否则终端不把 VT 序列作为字节交付）。非 Windows 或 stdin 被重定向时回退到 <see cref="UnixCharSource"/>路径。
///
/// 细节：
/// - 用后台读取线程把 stdin 字节轮询进队列（阻塞读放后台，避免主循环挂死）；
///   主泵线程 TryReadChar 从队列出队 1 字节。
/// - UTF-8 聚合：0x80-0xBF 为续字节，落在 UTF-8 前导字节之后须合并成完整字符；
///   纯 ASCII（含 ESC \x1b、RS \x1e 等控制符）单字节即可。
/// - 鼠标序列后的孤立 \x1e(RS)：终端把 SGR 序列 m/M 后额外发一个 RS 记录分隔符，
///   本实现把它跳过（不当作按键；也不让后续解析误认）。这是 Windows Terminal 与 macOS 的差异点。
/// </summary>
public sealed class WindowsCharSource : ICharSource, IDisposable
{
    private readonly System.Collections.Concurrent.ConcurrentQueue<byte> _bytes = new();
    private readonly Thread _reader;
    private volatile bool _done;
    private readonly System.IO.Stream _stdin;

    public WindowsCharSource() : this(Console.OpenStandardInput()) { }

    /// <summary>测试/自定义流注入构造（生产走无参 = Console 标准输入）。</summary>
    public WindowsCharSource(Stream stdin)
    {
        _stdin = stdin;
        _reader = new Thread(ReadLoop) { IsBackground = true, Name = "waycoder-win-char-source" };
        _reader.Start();
    }

    private void ReadLoop()
    {
        try
        {
            // 批量读：一次读入最多 64 字节再逐字节入队。鼠标序列（如 \x1b[<0;10;5M\x1e）
            // 由终端一次性发出，批量读让连续字节几乎同时到达 —— 消除逐字节读的间隙，
            // 避免 TryParseEscapeSequence 在等待下一个字节时因 10ms 超时把小间隙当序列中断
            // （中断 → 部分字节被当普通键 → 鼠标点击乱码）。这是 Windows 与 macOS 的时序差异点。
            var buf = new byte[64];
            while (!_done)
            {
                int n;
                try { n = _stdin.Read(buf, 0, buf.Length); }
                catch { break; }
                if (n <= 0) break; // EOF
                for (int i = 0; i < n; i++)
                    _bytes.Enqueue(buf[i]);
            }
        }
        catch { /* 读线程终止路径 */ }
    }

    public bool HasInput => !_bytes.IsEmpty;

    // 状态化 UTF-8：跨 64 字节读边界时续字节可能尚未到达，暂存 _pending 等下次继续拼（原实现直接
    // 丢弃起始字节/把半字符解成 U+FFFD）；代理对（emoji 等 >U+FFFF）解出两个 char，先返回高位、
    // 低位存 _pendingSurrogate 下次返回（原实现只取 s[0] 丢低位，code-review finding）。
    private readonly List<byte> _pending = new();
    private char _pendingSurrogate;

    public bool TryReadChar(out char c)
    {
        c = '\0';
        if (_pendingSurrogate != '\0') { c = _pendingSurrogate; _pendingSurrogate = '\0'; return true; }

        if (_pending.Count == 0)
        {
            if (!_bytes.TryDequeue(out var first)) return false;
            // 跳过终端协议层分隔符 RS(0x1E)：Windows Terminal 在 SGR 鼠标上报（m/M）后额外发一个
            // RS 记录分隔符。它不代表任何按键，ReadKey 路径（macOS）没有它，这里是 Windows 与 macOS
            // 的关键差异。丢弃以免被当成孤立字符当按键入队。
            if (first == 0x1E) return TryReadChar(out c); // 递归跳过多余 RS，读下一个实际字符
            _pending.Add(first);
        }

        byte lead = _pending[0];
        if (lead < 0x80) { _pending.Clear(); c = (char)lead; return true; } // ASCII

        // 从首字节估算续字节数；不足则留在 _pending 等下次（不丢、不产生 U+FFFD）。
        int expected = lead switch
        {
            >= 0xF0 => 3, >= 0xE0 => 2, >= 0xC0 => 1, _ => 0,
        };
        while (_pending.Count < expected + 1)
        {
            if (!_bytes.TryDequeue(out var b)) return false; // 续字节未齐：等待下轮
            _pending.Add(b);
        }
        var seq = _pending.ToArray();
        _pending.Clear();
        try
        {
            var s = System.Text.Encoding.UTF8.GetString(seq, 0, seq.Length);
            if (s.Length == 0) return false;
            if (s.Length == 2) // 代理对：高位先返回，低位缓存
            {
                c = s[0];
                _pendingSurrogate = s[1];
                return true;
            }
            c = s[0];
            return true;
        }
        catch { /* 非法序列丢弃 */ }
        return false;
    }

    public bool TryReadKey(out ConsoleKeyInfo key)
    {
        key = default;
        if (!TryReadChar(out var c)) return false;
        // 复用字符聚合；把码点映射为 ConsoleKeyInfo（方向键/功能键等转义序列由 PumpKeys 走
        // TryParseEscapeSequence 单独解析，这里只处理单字符键）。
        if (c == '\0') return false;
        key = ToConsoleKeyInfo(c);
        return true;
    }

    /// <summary>
    /// char（码点）→ ConsoleKeyInfo 的**唯一实现**（<see cref="TryReadKey"/> 与
    /// <c>InputManager.ToConsoleKeyInfo</c> 共用——此前两处各写一份，0x7F 与控制字符在两处
    /// 一起漏掉，修一处也修不全）。
    ///
    /// 字节流层拿不到修饰键信息，这里按 VT 约定还原：
    /// - **0x7F(DEL) → Backspace**：Windows VT 输入下 Backspace 发 DEL 而非 BS(0x08)。漏映射则
    ///   `Key=NoName`，而编辑控件都按 `Key==Backspace` 分支判（TuiEditBase/TuiChatInput），
    ///   表现为**退格无法擦除输入**（v0.96.74 改字节流后引入）。
    /// - **0x01..0x1A → Ctrl+字母**：终端未协商 Kitty 协议（conhost/旧终端忽略 CSI &gt;1u）时
    ///   Ctrl 组合以控制字节到达。不还原则 Ctrl+P/E/M/B/S 等**全部快捷键在 Windows 上静默失效**
    ///   （`Program.Repl` 判的是 `Modifiers.HasFlag(Control)`）。
    ///
    /// 歧义码位保持既有语义不动：0x08(BS)/0x09(Tab)/0x0A(LF)/0x0D(CR)/0x1B(ESC)——它们在 Unix 上
    /// 与 Ctrl+H/I/J/M/[ 同码，代码库既有约定见 <c>TuiKeybindHelp</c>，不在字节流层重新分配。
    /// </summary>
    public static ConsoleKeyInfo ToConsoleKeyInfo(char c)
    {
        if (c == '\0') return new ConsoleKeyInfo('\0', ConsoleKey.NoName, false, false, false);

        // DEL → Backspace（KeyChar 归一为 '\b'，与 Kitty 解析路径 keyChar='\b' 保持一致）
        if (c == '\x7f') return new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false);

        // 控制字节 → Ctrl+字母（跳过上面那批有独立语义的歧义码位）
        if (c is >= '\x01' and <= '\x1a'
            && c is not ('\b' or '\t' or '\n' or '\r' or '\x1b'))
            return new ConsoleKeyInfo((char)('a' + (c - 1)),
                (ConsoleKey)((int)ConsoleKey.A + (c - 1)), false, false, true);

        return new ConsoleKeyInfo(c, MapToConsoleKey(c), false, false, false);
    }

    /// <summary>char → ConsoleKey 唯一映射（InputManager.ToConsoleKey 与 WindowsCharSource 共用，
    /// 避免三处重复实现、修一处全端生效——code-review finding）。方向/功能键走转义序列单独解析，
    /// 这里只覆盖纯字符/编辑键。修饰键信息见 <see cref="ToConsoleKeyInfo"/>。</summary>
    public static ConsoleKey MapToConsoleKey(char c)
    {
        if (c >= 'a' && c <= 'z') return (ConsoleKey)((int)ConsoleKey.A + (c - 'a'));
        if (c >= 'A' && c <= 'Z') return (ConsoleKey)((int)ConsoleKey.A + (c - 'A'));
        if (c >= '0' && c <= '9') return (ConsoleKey)((int)ConsoleKey.D0 + (c - '0'));
        return c switch
        {
            ' ' => ConsoleKey.Spacebar,
            '\r' or '\n' => ConsoleKey.Enter,
            '\t' => ConsoleKey.Tab,
            '\b' or '\x7f' => ConsoleKey.Backspace,
            '\x1b' => ConsoleKey.Escape,
            '\0' => ConsoleKey.NoName,
            _ => ConsoleKey.NoName,
        };
    }

    public void Dispose()
    {
        _done = true;
        try { _stdin.Dispose(); } catch { }
    }
}
