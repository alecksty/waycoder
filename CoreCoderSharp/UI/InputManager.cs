namespace CoreCoderSharp.UI;
using CoreCoderSharp.Terminal;

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

    /// <summary>窗口大小变化时触发（在 ReadInput 返回前调用）</summary>
    public event Action? OnResize;

    /// <summary>初始化终端输入模式</summary>
    public void Init()
    {
        // 不拦截 Ctrl+C——让 OS 信号触发 CancelKeyPress 实现随时退出
        Console.TreatControlCAsInput = false;
        Console.CursorVisible = false;
        (_lastWidth, _lastHeight) = (TTY.Cols, TTY.Rows);

        try { TTY.EnableMouse(); _mouseEnabled = true; }
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
            var (w, h) = (TTY.Cols, TTY.Rows);
            if (w != _lastWidth || h != _lastHeight)
            {
                (_lastWidth, _lastHeight) = (w, h);
                OnResize?.Invoke();
                return new InputEvent { Type = InputType.Resize, Width = w, Height = h };
            }

            // 键盘输入
            if (Console.KeyAvailable)
            {
                var key = TTY.ReadKey();

                // 鼠标转义序列解析（SGR extended mouse: \x1b[<...）
                if (_mouseEnabled && key.KeyChar == '\x1b')
                {
                    var mouseEvent = TryParseMouse();
                    if (mouseEvent != null) return mouseEvent;
                }

                // Ctrl+C 拦截为 Esc（防止退出）
                if (key.Key == ConsoleKey.C && key.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    return new InputEvent { Type = InputType.Key, KeyInfo = key };
                }

                return new InputEvent { Type = InputType.Key, KeyInfo = key };
            }

            Thread.Sleep(10); // 10ms 轮询间隔
        }
        while (Environment.TickCount64 < deadline);

        return new InputEvent { Type = InputType.Timeout };
    }

    /// <summary>尝试解析 SGR 扩展鼠标协议 \x1b[&lt;C;X;Y M/m</summary>
    private InputEvent? TryParseMouse()
    {
        // 等待 '[' 到达（最多 20ms）
        if (!WaitForChar(20)) return null;
        var bracket = TTY.ReadKey();
        if (bracket.KeyChar != '[') return null;

        // 逐字符读取序列，每字符最多等待 10ms
        var buf = new System.Text.StringBuilder();
        for (int i = 0; i < 30; i++)
        {
            if (!WaitForChar(10)) break;
            var ch = TTY.ReadKey();
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
            MouseX = x - 1,  // 1-based → 0-based
            MouseY = y - 1,
            MouseLeft = !isRelease && (code == 0 || code == 32),
            MouseRight = !isRelease && (code == 2 || code == 34),
            MouseScrollUp = code == 64,
            MouseScrollDown = code == 65,
            MouseButton = code,
            MouseRelease = isRelease,
        };
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

        if (_mouseEnabled) { try { TTY.DisableMouse(); } catch { } }
        Console.CursorVisible = true;
    }
}

/// <summary>输入事件类型</summary>
public enum InputType
{
    Key,        // 键盘按键
    Mouse,      // 鼠标（点击/移动/滚轮）
    Resize,     // 窗口大小变化
    Timeout,    // 超时（无输入）
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
    public bool MouseRelease { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
