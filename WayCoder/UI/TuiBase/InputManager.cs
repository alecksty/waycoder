using System.Diagnostics;

namespace WayCoder.UI;

using WayCoder.Terminal;

/// <summary>
/// 终端输入管理器 —— 拦截键盘、管理鼠标、即时响应窗口 resize。
///
/// 功能：
/// - 拦截所有按键（none 泄露到终端）
/// - 轮询键盘（非阻塞）+ resize 检测
/// - 启用鼠标事件（支持点击/滚轮）
/// - 窗口大小变化时立即触发 OnResize 回调
/// - 管理终端模式（TreatControlCAsInput、隐藏光标）
/// </summary>
public class InputManager : IDisposable
{
    private int _lastWidth, _lastHeight;
    private bool _mouseEnabled;
    private bool _disposed;

    /// <summary>ESC 序列解析时暂读的非鼠标字符（保证 Alt+字母 等组合键不丢失）</summary>
    private readonly Queue<ConsoleKeyInfo> _pendingKeys = new();

    /// <summary>窗口大小变化时触发（在 ReadInput 返回前调用）</summary>
    public event Action? OnResize;

    /// <summary>初始化终端输入模式</summary>
    public void Init()
    {
        // 不拦截 Ctrl+C——让 OS 信号触发 CancelKeyPress 实现随时退出
        Console.TreatControlCAsInput = false;
        Console.CursorVisible = false;
        (_lastWidth, _lastHeight) = (Tty.Cols, Tty.Rows);

        // 无论是否启用鼠标，先发送禁用序列：清除上一个程序（如崩溃退出）残留在
        // 终端里的鼠标追踪模式，否则 SGR 鼠标事件会被逐字符当作普通按键敲进输入框。
        try
        {
            Tty.DisableMouse();
        }
        catch
        {
            Debug.Print("Couldn't disable mouse");
        }

        // TODO: 鼠标暂不开启，后续通过 WAYCODER_MOUSE=1 启用
        try { Tty.EnableMouse(); _mouseEnabled = true; }
        catch { _mouseEnabled = false; }
    }

    /// <summary>
    /// 读取下一个输入事件。非阻塞：timeoutMs 后返回 Timeout 事件。
    /// 每个轮询周期都会检查窗口大小变化。
    /// </summary>
    public InputEvent ReadInput(int timeoutMs = 50)
    {
        if (_disposed) return new InputEvent { Type = InputType.Timeout };

        var deadline = Environment.TickCount64 + timeoutMs;

        // 至少执行一轮检查，防止 Render() 耗时导致 deadline 过期后跳过所有输入检测
        do
        {
            // 检查窗口大小变化（立即返回）
            var (w, h) = (Tty.Cols, Tty.Rows);
            if (w != _lastWidth || h != _lastHeight)
            {
                (_lastWidth, _lastHeight) = (w, h);
                OnResize?.Invoke();
                return new InputEvent { Type = InputType.Resize, Width = w, Height = h };
            }

            // 先返回 ESC 序列解析时暂存的字符（如 Alt+x 的 'x'），保证按键顺序
            if (_pendingKeys.Count > 0)
            {
                return new InputEvent { Type = InputType.Key, KeyInfo = _pendingKeys.Dequeue() };
            }

            // 键盘输入
            if (Console.KeyAvailable)
            {
                var key = Tty.ReadKey();

                // 转义序列解析（SGR 鼠标 \x1b[<...、其他 CSI \x1b[...）
                // 无论鼠标是否启用都必须尝试：终端可能残留鼠标上报模式，
                // 若不吞掉，\x1b 被当 ESC、后面的 [<35;95;28M 被逐字符敲进输入框。
                if (key.KeyChar == AnsiTty.AnsiCharPrefix)
                {
                    var ev = TryParseEscapeSequence();
                    if (ev != null) return ev;
                    return new InputEvent { Type = InputType.Key, KeyInfo = key };
                }

                // Ctrl+C 拦截为 Esc（防止退出）
                if (key.Key == ConsoleKey.C && key.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    return new InputEvent { Type = InputType.Key, KeyInfo = key };
                }

                return new InputEvent { Type = InputType.Key, KeyInfo = key };
            }

            Thread.Sleep(10); // 10ms 轮询间隔
        } while (Environment.TickCount64 < deadline);

        return new InputEvent { Type = InputType.Timeout };
    }

    /// <summary>
    /// 解析 AnsiTty.AnsiCharPrefix 开头的转义序列（调用时 AnsiTty.AnsiCharPrefix 已被读取）：
    /// - SGR 鼠标 AnsiTty.AnsiCharPrefix[&lt;C;X;Y M/m → 返回 Mouse 事件
    /// - 修饰键功能键 CSI 序列（如 AnsiTty.AnsiCharPrefix[1;2P = Shift+F1）→ 返回对应 Key 事件
    /// - 其他 CSI 序列（如 AnsiTty.AnsiCharPrefix[1;5A 带修饰键方向键）→ 吞掉整个序列，返回 null
    /// - Alt+字符（AnsiTty.AnsiCharPrefix x）→ 字符退回 _pendingKeys，返回 null
    /// </summary>
    private InputEvent? TryParseEscapeSequence()
    {
        // 等待 '['（最多 20ms）；超时 = 用户单独按了 ESC
        if (!WaitForChar(20)) return null;
        var bracket = Tty.ReadKey();
        if (bracket.KeyChar != AnsiTty.AnsiCharEscape)
        {
            // Alt+字符 组合：AnsiTty.AnsiCharPrefix x —— 退回字符，AnsiTty.AnsiCharPrefix 单独作为 ESC 键返回
            _pendingKeys.Enqueue(bracket);
            return null;
        }

        // \x1b[ 后无内容（极少见）→ 退回 '['，让 \x1b 单独作为 ESC 键
        if (!WaitForChar(10))
        {
            _pendingKeys.Enqueue(bracket);
            return null;
        }

        var lt = Tty.ReadKey();

        // Shift+Tab：\x1b[Z
        if (lt.KeyChar == 'Z')
            return new InputEvent { Type = InputType.ShiftTab };

        // 非 SGR 鼠标的 CSI 序列
        if (lt.KeyChar != '<')
        {
            // 尝试解析为带修饰符的功能键
            // 格式：CSI num;mod term （xterm 风格，如 \x1b[1;2P = Shift+F1）
            var funcKeyEvent = TryParseCsiFunctionKey(lt.KeyChar);
            if (funcKeyEvent != null) return funcKeyEvent;

            ConsumeCsi(lt.KeyChar);
            return null;
        }

        // SGR 鼠标：\x1b[<Cb;Cx;CyM（按下）/ \x1b[<Cb;Cx;Cym（释放）
        var buf = new System.Text.StringBuilder();
        for (int i = 0; i < 30; i++)
        {
            if (!WaitForChar(10)) break;
            var ch = Tty.ReadKey();
            buf.Append(ch.KeyChar);
            if (ch.KeyChar == 'M' || ch.KeyChar == 'm') break;
        }

        var seq = buf.ToString();
        if (seq.Length < 2) return null;

        // 去掉终止符再解析 C;X;Y
        var body = seq.TrimEnd('M', 'm');
        var parts = body.Split(';');
        if (parts.Length < 3) return null;

        if (!int.TryParse(parts[0], out var code)) return null;
        if (!int.TryParse(parts[1], out var x)) return null;
        if (!int.TryParse(parts[2], out var y)) return null;

        var isRelease = seq.EndsWith('m');

        return new InputEvent
        {
            Type = InputType.Mouse,
            MouseX = x - 1, // 1-based → 0-based
            MouseY = y - 1,
            MouseLeft = !isRelease && (code == 0 || code == 32),
            MouseRight = !isRelease && (code == 2 || code == 34),
            MouseScrollUp = code == 64,
            MouseScrollDown = code == 65,
            MouseMotion = code == 35 || code == 36 || code == 39, // SGR motion (no button / with button / release motion)
            MouseButton = code,
            MouseRelease = isRelease,
        };
    }

    /// <summary>
    /// 尝试解析 CSI 功能键序列（如 \x1b[1;2P = Shift+F1）。
    /// firstChar 是 '[' 之后的第一个字符（已被 ReadKey 读取）。
    /// 返回解析后的 InputEvent，若无法识别则返回 null。
    /// </summary>
    private InputEvent? TryParseCsiFunctionKey(char firstChar)
    {
        // 读取 CSI 参数串：从 firstChar 开始，直到终止字节（0x40-0x7E）
        var paramStr = new System.Text.StringBuilder();
        paramStr.Append(firstChar);

        // 如果 firstChar 本身就是终止字节（如 \x1b[P = F1 老旧格式 \x1bOP）
        if (firstChar >= 0x40 && firstChar <= 0x7E)
        {
            return ParseCsiFuncKey(paramStr.ToString(), firstChar);
        }

        // 继续读取参数
        for (int i = 0; i < 20; i++)
        {
            if (!WaitForChar(10)) break;
            var ch = Tty.ReadKey();
            paramStr.Append(ch.KeyChar);
            if (ch.KeyChar >= 0x40 && ch.KeyChar <= 0x7E)
            {
                // 终止字节到达，解析
                return ParseCsiFuncKey(paramStr.ToString(), ch.KeyChar);
            }
        }

        return null;
    }

    /// <summary>
    /// 解析 CSI 参数串为功能键事件。
    /// 支持格式：
    ///   num;mod term  →  xterm 修饰键格式（如 1;2P = Shift+F1）
    ///   num term      →  无修饰键格式（如 1P = F1, 15~ = F5）
    /// term: P = F1-F4, ~ = F5+ 或 Home/End/Insert/Delete/PgUp/PgDn
    /// </summary>
    private static InputEvent? ParseCsiFuncKey(string paramBody, char terminator)
    {
        // 去掉终止符，解析数字参数
        var body = paramBody.TrimEnd(terminator);
        if (body.Length == 0) return null;

        var parts = body.Split(';');
        if (!int.TryParse(parts[0], out var num)) return null;

        int mod = parts.Length >= 2 && int.TryParse(parts[1], out var m) ? m : 0;

        // 映射功能键编号：xterm 有两种编码
        // 编码1：F1-F4=1-4(P) / F5+=5-12(~)
        // 编码2：F1-F12=11-24(~)
        int funcNum;
        if (terminator == 'P' && num >= 1 && num <= 4)
        {
            funcNum = num; // F1=1, F2=2, F3=3, F4=4
        }
        else if (terminator == '~')
        {
            if (num >= 1 && num <= 12)
            {
                // 部分终端 F1=1~, F2=2~, ..., F12=12~
                // 也有 F1=11~, F2=12~, ..., F12=24~
                // 但 num=1~6 可能是 Home/Insert/Delete/End/PgUp/PgDn
                // 仅当有修饰键时才解析为功能键
                if (mod != 0 && num >= 1 && num <= 12)
                    funcNum = num;
                else if (num >= 11 && num <= 24)
                    funcNum = num - 10; // F1=11, F2=12, ...
                else if (num >= 5 && num <= 12)
                    funcNum = num; // F5=5, F6=6, ...
                else
                    return null;
            }
            else
                return null;
        }
        else
        {
            return null; // 不认识的终止符（如 A/B/C/D 方向键，已在 .NET 层处理）
        }

        if (funcNum < 1 || funcNum > 12) return null;

        // xterm modifier encoding:
        // 2=Shift, 3=Alt, 4=Shift+Alt, 5=Ctrl, 6=Ctrl+Shift, 7=Ctrl+Alt, 8=Ctrl+Shift+Alt
        bool shift = mod == 2 || mod == 4 || mod == 6 || mod == 8;
        bool alt   = mod == 3 || mod == 4 || mod == 7 || mod == 8;
        bool ctrl  = mod == 5 || mod == 6 || mod == 7 || mod == 8;

        var consoleKey = funcNum switch
        {
            1 => ConsoleKey.F1, 2 => ConsoleKey.F2, 3 => ConsoleKey.F3,
            4 => ConsoleKey.F4, 5 => ConsoleKey.F5, 6 => ConsoleKey.F6,
            7 => ConsoleKey.F7, 8 => ConsoleKey.F8, 9 => ConsoleKey.F9,
            10 => ConsoleKey.F10, 11 => ConsoleKey.F11, 12 => ConsoleKey.F12,
            _ => ConsoleKey.F1
        };

        var keyInfo = new ConsoleKeyInfo('\0', consoleKey, shift, alt, ctrl);
        return new InputEvent { Type = InputType.Key, KeyInfo = keyInfo };
    }

    /// <summary>吞掉 CSI 序列剩余部分直到终止字节（0x40-0x7E），防止泄漏为文本。
    /// firstChar 是已经读取的第一个参数字符。</summary>
    private void ConsumeCsi(char firstChar)
    {
        // 如果第一个字符就是终止字节，无需继续读取
        if (firstChar >= 0x40 && firstChar <= 0x7E) return;

        for (int i = 0; i < 30; i++)
        {
            if (!WaitForChar(10)) break;
            var ch = Tty.ReadKey();
            if (ch.KeyChar >= 0x40 && ch.KeyChar <= 0x7E) break; // CSI 终止字节
        }
    }

    /// <summary>等待键盘输入到达（忙等），最多 timeoutMs 毫秒</summary>
    private static bool WaitForChar(int timeoutMs)
    {
        int waited = 0;
        while (!Console.KeyAvailable)
        {
            if (waited >= timeoutMs) return false;
            Thread.Sleep(1);
            waited++;
        }

        return true;
    }

    /// <summary>恢复终端设置</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_mouseEnabled)
        {
            try
            {
                Tty.DisableMouse();
            }
            catch
            {
            }
        }

        Console.CursorVisible = true;
    }
}

/// <summary>输入事件类型</summary>
public enum InputType
{
    Key, // 键盘按键
    Mouse, // 鼠标（点击/移动/滚轮）
    Resize, // 窗口大小变化
    Timeout, // 超时（无输入）
    ShiftTab, // Shift+Tab（模式切换）
}

/// <summary>输入事件数据</summary>
public class InputEvent
{
    public InputType Type { get; set; }
    public ConsoleKeyInfo KeyInfo { get; set; }
    public int MouseX { get; set; }
    public int MouseY { get; set; }
    public bool MouseLeft { get; set; }
    public bool MouseRight { get; set; }
    public bool MouseScrollUp { get; set; }
    public bool MouseScrollDown { get; set; }
    public int MouseButton { get; set; }
    public bool MouseMotion { get; set; }
    public bool MouseRelease { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}