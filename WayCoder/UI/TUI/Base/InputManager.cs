using System.Diagnostics;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui;

namespace WayCoder.UI.TUI.Base;

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

    /// <summary>注入一个按键到输入队列（脚本测试用：阻塞式选择器 RenderWait 的 ReadInput 优先消费队列）。</summary>
    public void InjectKey(ConsoleKeyInfo key) => _pendingKeys.Enqueue(key);

    /// <summary>窗口大小变化时触发（在 ReadInput 返回前调用）</summary>
    public event Action? OnResize;

    /// <summary>初始化终端输入模式</summary>
    public void Init()
    {
        // 不拦截 Ctrl+C——让 OS 信号触发 CancelKeyPress 实现随时退出
        // 非交互环境（管道/重定向/后台/Keypad 脚本回放）：Console 模式设置会抛 IOException
        // （如 "console input has been redirected"），跳过——Keypad 用注入键 + 空输出，不需要真实终端模式
        if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            Console.TreatControlCAsInput = false;
            Console.CursorVisible = false;
        }
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

        if (TuiManager.MouseEnabled)
        {
            try
            {
                // macOS 自带终端不支持 ?1003h（移动追踪）/ ?1015h（UTF-8 鼠标），启用后显示/输入异常
                if (Tty.IsAppleTerminal) Tty.EnableMouseBasic();
                else Tty.EnableMouse();
                _mouseEnabled = true;
            }
            catch
            {
                _mouseEnabled = false;
            }
        }

        // 启用 bracketed paste：终端自动包裹粘贴内容为 \x1b[200~...\x1b[201~
        try
        {
            Tty.EnableBracketedPaste();
        }
        catch
        {
            /* 非关键功能 */
        }

        // 启用 Kitty 键盘协议：现代终端（iTerm2/Kitty/WezTerm）支持修饰键完整报告；
        // macOS 自带终端不支持 Kitty，启用后可能产生异常（不做协议协商就发 >1u）。
        if (!Tty.IsAppleTerminal)
        {
            try
            {
                Tty.EnableKittyKeyboard();
            }
            catch
            {
                /* 非关键功能 */
            }
        }
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
            // 非交互环境（管道/重定向/后台）：Console.KeyAvailable 会抛 InvalidOperationException
            // （"Cannot see if a key has been pressed when ... console input has been redirected"），
            // 空转返回超时，避免 REPL 在 echo "x" | waycoder / CI 场景崩溃。
            if (Console.IsInputRedirected)
                return new InputEvent { Type = InputType.Timeout };

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
    /// - Bracketed paste AnsiTty.AnsiCharPrefix[200~ ... AnsiTty.AnsiCharPrefix[201~ → 返回 Paste 事件
    /// - Kitty 键盘协议 AnsiTty.AnsiCharPrefix[keycode;mod u → 返回 Key 事件
    /// - xterm 功能键 AnsiTty.AnsiCharPrefix[num;mod P/~ → 返回 Key 事件
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

        // 非 SGR 鼠标的 CSI 序列：统一解析（bracketed paste / Kitty / xterm 功能键）
        if (lt.KeyChar != '<')
        {
            return TryParseCsiFunctionKey(lt.KeyChar);
        }

        // SGR 鼠标：\x1b[<Cb;Cx;CyM（按下）/ \x1b[<Cb;Cx;Cym（释放）
        // 逐字节读入（交互部分），收集完整序列后交给纯函数解析（可测）
        var buf = new System.Text.StringBuilder();
        for (int i = 0; i < 30; i++)
        {
            if (!WaitForChar(10)) break;
            var ch = Tty.ReadKey();
            buf.Append(ch.KeyChar);
            if (ch.KeyChar == 'M' || ch.KeyChar == 'm') break;
        }

        return ParseSgrMouse(buf.ToString());
    }

    /// <summary>
    /// 把 SGR 鼠标序列（'&lt;' 之后的内容，如 "0;10;5M" / "64;3;9m"）解析成鼠标事件。
    /// 纯函数：便于离屏单测（喂真实终端字节串断言 InputEvent 字段）。
    /// </summary>
    internal static InputEvent? ParseSgrMouse(string seq)
    {
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
            // SGR 鼠标：32/33/34/35 是 motion（32=无按键纯移动），不是点击——
            // 误判会让鼠标悬停在标题栏就触发拖拽
            // 注意：注释说的 32/33/34/35 与实际代码判定的 35/36/39 不完全一致，
            // 以代码实现为准（锁定现状，避免改动影响真实终端行为）
            MouseLeft = !isRelease && code == 0,
            MouseRight = !isRelease && code == 2,
            MouseScrollUp = code == 64,
            MouseScrollDown = code == 65,
            MouseMotion = code == 35 || code == 36 || code == 39, // SGR motion (no button / with button / release motion)
            MouseButton = code,
            MouseRelease = isRelease,
        };
    }

    /// <summary>
    /// 尝试解析 CSI 功能键序列（如 \x1b[1;2P = Shift+F1）。
    /// 同时处理 bracketed paste（\x1b[200~）和 Kitty 键盘协议（CSI ... u）。
    /// firstChar 是 '[' 之后的第一个字符（已被 ReadKey 读取）。
    /// 返回解析后的 InputEvent，若无法识别则返回 null。
    /// </summary>
    private InputEvent? TryParseCsiFunctionKey(char firstChar)
    {
        // 读取 CSI 参数串：从 firstChar 开始，直到终止字节（0x40-0x7E）
        var paramStr = new System.Text.StringBuilder();
        paramStr.Append(firstChar);

        char terminator;
        if (firstChar >= 0x40 && firstChar <= 0x7E)
        {
            terminator = firstChar;
        }
        else
        {
            terminator = '\0';
            for (int i = 0; i < 20; i++)
            {
                if (!WaitForChar(10)) break;
                var ch = Tty.ReadKey();
                paramStr.Append(ch.KeyChar);
                if (ch.KeyChar >= 0x40 && ch.KeyChar <= 0x7E)
                {
                    terminator = ch.KeyChar;
                    break;
                }
            }

            if (terminator == '\0') return null; // 无终止符，损坏的序列
        }

        var paramBody = paramStr.ToString();

        // Bracketed paste：\x1b[200~（开始）/ \x1b[201~（结束）
        if (terminator == '~')
        {
            if (paramBody == "200~") return ReadPasteContent();
            if (paramBody == "201~") return null; // 孤立的粘贴结束标记，忽略
        }

        // Kitty 键盘协议：CSI keycode[:alternate];modifiers u
        if (terminator == 'u')
        {
            return ParseKittyKeySequence(paramBody.TrimEnd('u'));
        }

        // xterm 功能键格式：num;mod P 或 num;mod ~
        return ParseCsiFuncKey(paramBody, terminator);
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
        bool alt = mod == 3 || mod == 4 || mod == 7 || mod == 8;
        bool ctrl = mod == 5 || mod == 6 || mod == 7 || mod == 8;

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

    /// <summary>
    /// 读取 bracketed paste 内容，直到遇到 \x1b[201~ 结束标记。
    /// 调用时 \x1b[200~ 已被完整消费。
    /// </summary>
    private InputEvent ReadPasteContent()
    {
        var sb = new System.Text.StringBuilder();
        const string endMarker = "\x1b[201~";
        // 结束标记缺失时的空闲超时兜底：终端发来 \x1b[200~ 却因崩溃/截断漏发 \x1b[201~
        // 时，原实现会永久卡死；超时后把已读内容按普通粘贴文本返回。
        const int pasteIdleTimeoutMs = 2000;
        long lastActivity = Environment.TickCount64;

        while (true)
        {
            if (!Console.KeyAvailable)
            {
                if (Environment.TickCount64 - lastActivity > pasteIdleTimeoutMs)
                    break;
                Thread.Sleep(1);
                continue;
            }

            var ch = Tty.ReadKey();
            sb.Append(ch.KeyChar);
            lastActivity = Environment.TickCount64;

            // 检查缓冲区末尾是否匹配结束标记
            if (sb.Length >= endMarker.Length)
            {
                var match = true;
                for (int i = 0; i < endMarker.Length; i++)
                {
                    if (sb[sb.Length - endMarker.Length + i] != endMarker[i])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    var text = sb.ToString(0, sb.Length - endMarker.Length);
                    return new InputEvent { Type = InputType.Paste, PasteText = text };
                }
            }
        }

        // 超时兜底：结束标记缺失，把已读内容按普通粘贴文本返回
        return new InputEvent { Type = InputType.Paste, PasteText = sb.ToString() };
    }

    /// <summary>
    /// 解析 Kitty 键盘协议 CSI 序列参数（不含终止符 'u'）。
    /// 格式：keycode[:alternate];modifiers
    /// Kitty 修饰键编码：1=Shift, 2=Alt, 4=Ctrl, 8=Super(Meta/Cmd)
    /// 功能键在 Unicode Private Use Area：57344-57355 = F1-F12
    /// </summary>
    private static InputEvent? ParseKittyKeySequence(string body)
    {
        if (string.IsNullOrEmpty(body)) return null;

        // 按 ';' 分割：前半部分=keycode[:alternate]，后半部分=modifiers
        var semicolonIdx = body.LastIndexOf(';');
        string keyPart;
        int modifiers = 0;

        if (semicolonIdx >= 0)
        {
            keyPart = body.Substring(0, semicolonIdx);
            if (!int.TryParse(body.Substring(semicolonIdx + 1), out modifiers))
                modifiers = 0;
        }
        else
        {
            keyPart = body;
        }

        // 提取 keycode（可选 ':' 后的 alternate key 忽略）
        var colonIdx = keyPart.IndexOf(':');
        var keycodeStr = colonIdx >= 0 ? keyPart.Substring(0, colonIdx) : keyPart;
        if (!int.TryParse(keycodeStr, out var keycode)) return null;

        // Kitty 修饰键位掩码 → bool
        bool shift = (modifiers & 1) != 0;
        bool alt = (modifiers & 2) != 0;
        bool ctrl = (modifiers & 4) != 0;
        // Super (8) 忽略，因为 .NET ConsoleKeyInfo 无 Super 修饰键

        // 映射 keycode → ConsoleKey + keyChar
        ConsoleKey consoleKey;
        char keyChar = '\0';

        if (keycode >= 57344 && keycode <= 57355)
        {
            // 功能键 F1-F12：57344 = F1
            consoleKey = (ConsoleKey)((int)ConsoleKey.F1 + (keycode - 57344));
        }
        else if (keycode >= 57356 && keycode <= 57399)
        {
            // 特殊命名键（方向键、Home/End/PgUp/PgDn/Insert/Delete 等）
            consoleKey = keycode switch
            {
                57356 => ConsoleKey.UpArrow,
                57357 => ConsoleKey.DownArrow,
                57358 => ConsoleKey.LeftArrow,
                57359 => ConsoleKey.RightArrow,
                57360 => ConsoleKey.Home,
                57361 => ConsoleKey.End,
                57362 => ConsoleKey.PageUp,
                57363 => ConsoleKey.PageDown,
                57364 => ConsoleKey.Insert,
                57365 => ConsoleKey.Delete,
                57366 => ConsoleKey.Backspace,
                57367 => ConsoleKey.Tab,
                57368 => ConsoleKey.Enter,
                57369 => ConsoleKey.Escape,
                _ => ConsoleKey.NoName,
            };
        }
        else
        {
            // 普通按键：keycode = Unicode 码点
            switch (keycode)
            {
                case 13: // Enter（回车键也作为标准键处理）
                    consoleKey = ConsoleKey.Enter;
                    keyChar = '\r';
                    break;
                case 27: // Escape
                    consoleKey = ConsoleKey.Escape;
                    keyChar = '\x1b';
                    break;
                case 9: // Tab
                    consoleKey = ConsoleKey.Tab;
                    keyChar = '\t';
                    break;
                case 127: // Backspace
                    consoleKey = ConsoleKey.Backspace;
                    keyChar = '\b';
                    break;
                case 32: // Space
                    consoleKey = ConsoleKey.Spacebar;
                    keyChar = ' ';
                    break;
                default:
                    if (keycode >= 33 && keycode <= 126)
                    {
                        keyChar = (char)keycode;
                        // 映射 ASCII 可打印字符 → ConsoleKey
                        if (keyChar >= 'a' && keyChar <= 'z')
                            consoleKey = (ConsoleKey)((int)ConsoleKey.A + (keyChar - 'a'));
                        else if (keyChar >= 'A' && keyChar <= 'Z')
                            consoleKey = (ConsoleKey)((int)ConsoleKey.A + (keyChar - 'A'));
                        else if (keyChar >= '0' && keyChar <= '9')
                            consoleKey = (ConsoleKey)((int)ConsoleKey.D0 + (keyChar - '0'));
                        else
                            consoleKey = keyChar switch
                            {
                                '`' => ConsoleKey.Oem3, '-' => ConsoleKey.OemMinus,
                                '=' => ConsoleKey.OemPlus, '[' => ConsoleKey.Oem4,
                                ']' => ConsoleKey.Oem6, '\\' => ConsoleKey.Oem5,
                                ';' => ConsoleKey.Oem1, '\'' => ConsoleKey.Oem7,
                                ',' => ConsoleKey.OemComma, '.' => ConsoleKey.OemPeriod,
                                '/' => ConsoleKey.Oem2,
                                _ => ConsoleKey.NoName,
                            };
                    }
                    else
                    {
                        return null; // 无法识别的键码
                    }

                    break;
            }
        }

        var keyInfo = new ConsoleKeyInfo(keyChar, consoleKey, shift, alt, ctrl);
        return new InputEvent { Type = InputType.Key, KeyInfo = keyInfo };
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

            try
            {
                Tty.DisableBracketedPaste();
            }
            catch
            {
            }

            try
            {
                Tty.DisableKittyKeyboard();
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
    Paste, // 粘贴事件（bracketed paste 检测）
}

/// <summary>输入事件数据</summary>
public class InputEvent
{
    /// <summary>
    /// 是否「切换工作模式」键。三个入口，因为 Shift+Tab 在两个平台上长得不一样：
    ///   Unix    终端发 ESC[Z → <see cref="InputType.ShiftTab"/>
    ///   Windows Console.ReadKey 给 ConsoleKey.Tab + Shift 修饰键，永远没有 ESC[Z
    ///   Ctrl+K  两平台通用别名
    /// 抽成纯函数是因为这个条件错过一次（只认第一种，Windows 上按 Shift+Tab 变成插 4 空格），
    /// REPL 主循环没法自测，判定逻辑放这儿能锁住 —— 尤其两条反向：
    /// 裸 Tab 必须放行给路径补全，裸 k 必须当普通字符打进输入框。
    /// </summary>
    public static bool IsModeSwitchKey(InputEvent ev)
    {
        if (ev.Type == InputType.ShiftTab) return true;
        if (ev.Type != InputType.Key) return false;
        var k = ev.KeyInfo;
        if (k.Key == ConsoleKey.Tab && k.Modifiers.HasFlag(ConsoleModifiers.Shift)) return true;
        if (k.Key == ConsoleKey.K && k.Modifiers.HasFlag(ConsoleModifiers.Control)) return true;
        return false;
    }

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

    /// <summary>粘贴文本内容（仅 InputType.Paste 时有效）</summary>
    public string? PasteText { get; set; }
}