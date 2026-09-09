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
        // 复用字符聚合；把码点映射为 ConsoleKey（方向键/功能键等转义序列由 PumpKeys 走
        // TryParseEscapeSequence 单独解析，这里只处理单字符键）。
        if (c == '\0') return false;
        key = new ConsoleKeyInfo(c, CharToConsoleKey(c), false, false, false);
        return true;
    }

    private static ConsoleKey CharToConsoleKey(char c) => MapToConsoleKey(c);

    /// <summary>char → ConsoleKey 唯一映射（InputManager.ToConsoleKey 与 WindowsCharSource 共用，
    /// 避免三处重复实现、修一处全端生效——code-review finding）。方向/功能键走转义序列单独解析，
    /// 这里只覆盖纯字符/编辑键。</summary>
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
            '\b' => ConsoleKey.Backspace,
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
