namespace CoreCoderSharp.UI;

// ================================================================
// 控件基类
// ================================================================

/// <summary>控件基类 — 所有控件的相对位置相对于所属窗口原点</summary>
public abstract class UIControl
{
    /// <summary>相对窗口原点的 X 偏移</summary>
    public int X { get; set; }
    /// <summary>相对窗口原点的 Y 偏移</summary>
    public int Y { get; set; }
    /// <summary>控件宽度（字符单元格）</summary>
    public int Width { get; set; } = 10;
    /// <summary>控件高度</summary>
    public int Height { get; set; } = 1;
    /// <summary>是否获得焦点</summary>
    public bool Focused { get; set; }
    /// <summary>是否可见</summary>
    public bool Visible { get; set; } = true;

    /// <summary>渲染控件到 StringBuilder。absX/absY 为窗口原点绝对坐标。</summary>
    public abstract void Render(System.Text.StringBuilder sb, int absX, int absY);

    /// <summary>处理按键，返回 true 表示已处理（停止路由）</summary>
    public virtual bool HandleKey(ConsoleKeyInfo key) => false;
}

/// <summary>静态文本标签</summary>
public class UILabel : UIControl
{
    public string Text { get; set; } = "";
    public int FgColor { get; set; } = 0;

    public override void Render(System.Text.StringBuilder sb, int absX, int absY)
    {
        if (!Visible || string.IsNullOrEmpty(Text)) return;
        var sx = absX + X + 1;
        var sy = absY + Y + 1;
        sb.Append($"\x1b[{sy};{sx}H");
        if (FgColor != 0) sb.Append($"\x1b[{FgColor}m");
        sb.Append(ClipText(Text, Width));
        if (FgColor != 0) sb.Append("\x1b[0m");
    }

    private static string ClipText(string t, int w) =>
        TuiHelper.DisplayWidth(t) > w ? TuiHelper.TruncateByWidth(t, w) : t;
}

/// <summary>可点击按钮</summary>
public class UIButton : UIControl
{
    public string Text { get; set; } = "OK";
    public Action? OnClick { get; set; }

    public override void Render(System.Text.StringBuilder sb, int absX, int absY)
    {
        if (!Visible) return;
        var sx = absX + X + 1;
        var sy = absY + Y + 1;
        var label = $" {Text} ";
        if (TuiHelper.DisplayWidth(label) > Width)
            label = TuiHelper.TruncateByWidth(Text, Width - 2);
        var pad = Math.Max(0, Width - TuiHelper.DisplayWidth(label));

        sb.Append($"\x1b[{sy};{sx}H");
        if (Focused)
            sb.Append($"\x1b[30;46m{label}{new string(' ', pad)}\x1b[0m");
        else
            sb.Append($"\x1b[37;44m{label}{new string(' ', pad)}\x1b[0m");
    }

    public override bool HandleKey(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Spacebar)
        {
            OnClick?.Invoke();
            return true;
        }
        return false;
    }
}

/// <summary>文本输入框</summary>
public class UIInput : UIControl
{
    public string Text { get; set; } = "";
    public int CursorPos { get; set; }
    public Action<string>? OnSubmit { get; set; }
    public bool Password { get; set; }

    public UIInput() { Height = 1; CursorPos = 0; }

    public override void Render(System.Text.StringBuilder sb, int absX, int absY)
    {
        if (!Visible) return;
        var sx = absX + X + 1;
        var sy = absY + Y + 1;
        var displayText = Password ? new string('•', Text.Length) : Text;

        // 滚动确保光标可见
        var visW = Width;
        if (CursorPos >= visW) displayText = displayText[(CursorPos - visW + 1)..];
        if (displayText.Length > visW) displayText = displayText[..visW];

        sb.Append($"\x1b[{sy};{sx}H");
        if (Focused)
            sb.Append($"\x1b[37;44m"); // 白字蓝底 — 聚焦态
        else
            sb.Append($"\x1b[2m");      // 灰色 — 非聚焦态

        sb.Append(displayText);
        sb.Append(new string(' ', Math.Max(0, visW - TuiHelper.DisplayWidth(displayText))));
        sb.Append("\x1b[0m");

        // 光标
        if (Focused)
        {
            var cx = sx + Math.Min(TuiHelper.DisplayWidth(displayText), visW - 1);
            sb.Append($"\x1b[{sy};{cx}H\x1b[?25h");
        }
    }

    public override bool HandleKey(ConsoleKeyInfo key)
    {
        if (!Focused) return false;

        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:  if (CursorPos > 0) CursorPos--; return true;
            case ConsoleKey.RightArrow: if (CursorPos < Text.Length) CursorPos++; return true;
            case ConsoleKey.Home: CursorPos = 0; return true;
            case ConsoleKey.End: CursorPos = Text.Length; return true;
            case ConsoleKey.Backspace:
                if (CursorPos > 0) { Text = Text[..(CursorPos - 1)] + Text[CursorPos..]; CursorPos--; }
                return true;
            case ConsoleKey.Delete:
                if (CursorPos < Text.Length) Text = Text[..CursorPos] + Text[(CursorPos + 1)..];
                return true;
            case ConsoleKey.Enter:
                OnSubmit?.Invoke(Text);
                return true;
            default:
                if (key.KeyChar >= ' ')
                {
                    Text = Text[..CursorPos] + key.KeyChar + Text[CursorPos..];
                    CursorPos++;
                    return true;
                }
                return false;
        }
    }
}

/// <summary>
/// 窗口管理器 —— 管理浮层窗口的 Z-order、渲染裁剪、键盘路由。
///
/// 窗口类型：
/// - Dialog：居中模态对话框，带遮罩
/// - Menu：弹出菜单，吸附在指定位置
/// - Toast：临时提示框，自动消失
///
/// 裁剪：超出窗口区域的内容自动截断，ANSI 颜色码在边界正确关闭。
/// </summary>
public class WindowManager
{
    public static WindowManager Instance { get; } = new();

    private readonly List<ManagedWindow> _windows = [];
    private int _nextZ;

    /// <summary>当前焦点窗口（只有一个）</summary>
    public ManagedWindow? FocusedWindow { get; private set; }

    // ================================================================
    // 创建窗口
    // ================================================================

    /// <summary>居中对话框</summary>
    public ManagedWindow ShowDialog(string title, string content,
        int? width = null, int? height = null)
    {
        var lines = content.Replace("\r\n", "\n").Split('\n');
        var maxLineVw = lines.Max(l => TuiHelper.DisplayWidth(l));
        var w = Math.Max(20, Math.Min(Console.WindowWidth - 8,
            width ?? Math.Max(maxLineVw + 4, TuiHelper.DisplayWidth(title) + 4)));
        // 折行长行
        var wrapped = new List<string>();
        foreach (var line in lines)
        {
            if (TuiHelper.DisplayWidth(line) > w - 4)
                wrapped.AddRange(WrapLine(line, w - 4));
            else
                wrapped.Add(line);
        }
        var h = Math.Min(Console.WindowHeight - 6,
            height ?? Math.Max(3, wrapped.Count + 4));
        var x = (Console.WindowWidth - w) / 2;
        var y = (Console.WindowHeight - h) / 2;

        var win = new ManagedWindow
        {
            X = x, Y = y, Width = w, Height = h,
            Title = title,
            ContentLines = wrapped,
            ZOrder = _nextZ++,
            Modal = true,
            HasMask = true,
            BorderColor = 36,  // Cyan
        };

        ThemeConfig.Instance.ApplyTo(win);
        _windows.Add(win);
        FocusedWindow = win;
        return win;
    }

    /// <summary>分隔线标记</summary>
    public const string MenuSeparator = "───";

    /// <summary>弹出菜单（吸附在指定位置，自动调整不超出屏幕）。
    /// 支持分隔线：choices 中包含 MenuSeparator 的项渲染为分隔线，不可选。</summary>
    public ManagedWindow ShowMenu(int x, int y, string title, List<string> choices)
    {
        var maxVw = choices.Where(c => c != MenuSeparator).Max(c => TuiHelper.DisplayWidth(c));
        var w = Math.Max(12, Math.Min(Console.WindowWidth - 4, maxVw + 6));
        var itemCount = choices.Count;
        var maxVisH = Math.Min(itemCount, Console.WindowHeight - y - 4);
        var h = maxVisH + 3;

        if (x + w > Console.WindowWidth) x = Console.WindowWidth - w - 1;
        if (y + h > Console.WindowHeight) y = Console.WindowHeight - h - 1;
        if (x < 1) x = 1;
        if (y < 1) y = 1;

        var win = new ManagedWindow
        {
            X = x, Y = y, Width = w, Height = h,
            Title = title,
            MenuItems = choices,
            ZOrder = _nextZ++,
            Modal = true,
            HasMask = false,
            BorderColor = 33,
            SelectedIndex = choices.FindIndex(c => c != MenuSeparator), // 跳过首个分隔线
        };
        if (win.SelectedIndex < 0) win.SelectedIndex = 0;

        ThemeConfig.Instance.ApplyTo(win);
        _windows.Add(win);
        FocusedWindow = win;
        return win;
    }

    /// <summary>提示框（右下角，2 秒自动消失）</summary>
    public ManagedWindow ShowToast(string message, int durationMs = 2000)
    {
        var vw = TuiHelper.DisplayWidth(message);
        var w = Math.Min(Console.WindowWidth - 4, vw + 4);
        var x = Console.WindowWidth - w - 2;
        var y = Console.WindowHeight - 4;

        var win = new ManagedWindow
        {
            X = x, Y = y, Width = w, Height = 3,
            Title = "",
            ContentLines = [message],
            ZOrder = _nextZ++,
            Modal = false,
            HasMask = false,
            BorderColor = 32,  // Green
        };

        _windows.Add(win);

        // 自动关闭
        Task.Delay(durationMs).ContinueWith(_ =>
        {
            _windows.Remove(win);
            win.OnClosed?.Invoke();
        });

        return win;
    }

    /// <summary>提示框（同步版，立即渲染，等待 durationMs）</summary>
    public void ShowToastSync(string message, int durationMs = 2000)
    {
        var win = ShowToast(message, durationMs);
        // 强制刷新
        var sb = new System.Text.StringBuilder();
        ScreenManager.Instance.Render(); // 触发主布局
        RenderOverlay(sb);
        Console.Write(sb.ToString());
    }

    /// <summary>关闭窗口（不写回背景，用于连续截图）</summary>
    public void CloseNoRestore(ManagedWindow win)
    {
        _windows.Remove(win);
        if (FocusedWindow == win) FocusedWindow = _windows.LastOrDefault();
        win.OnClosed?.Invoke();
    }

    /// <summary>关闭窗口并立即还原背景。</summary>
    public void Close(ManagedWindow win)
    {
        _windows.Remove(win);
        if (FocusedWindow == win) FocusedWindow = _windows.LastOrDefault();

        // 立即还原背景（写入关闭窗口前的最后一帧干净画面）
        var cleanFrame = ScreenManager.Instance.LastCleanFrame;
        if (!string.IsNullOrEmpty(cleanFrame))
        {
            Console.Write(cleanFrame);
        }

        win.OnClosed?.Invoke();
    }

    /// <summary>获取关闭所有窗口后的干净帧</summary>
    public string GetCleanFrame() => ScreenManager.Instance.LastCleanFrame;

    /// <summary>路由按键到焦点窗口的焦点控件</summary>
    public bool HandleKey(ConsoleKeyInfo key)
    {
        var fw = FocusedWindow;
        if (fw == null) return false;

        // Tab/Shift+Tab 切换控件焦点
        if (key.Key == ConsoleKey.Tab && !key.Modifiers.HasFlag(ConsoleModifiers.Shift))
        {
            fw.FocusNext();
            return true;
        }
        if (key.Key == ConsoleKey.Tab && key.Modifiers.HasFlag(ConsoleModifiers.Shift))
        {
            fw.FocusPrev();
            return true;
        }

        // 路由到焦点控件
        if (fw.FocusedControl != null)
            return fw.FocusedControl.HandleKey(key);

        return false;
    }

    /// <summary>关闭所有模态窗口</summary>
    public void CloseAll()
    {
        foreach (var w in _windows.Where(w => w.Modal).ToList())
            _windows.Remove(w);
    }

    /// <summary>是否有模态窗口</summary>
    public bool HasModal => _windows.Any(w => w.Modal);

    // ================================================================
    // 渲染
    // ================================================================

    /// <summary>
    /// 渲染所有浮层窗口到 StringBuilder（覆盖在 ScreenManager Render 之上）。
    /// 调用时机：ScreenManager.Render() 输出完主布局之后。
    /// 窗口关闭后，下次 Render() 自动还原背景（全帧重绘）。
    /// </summary>
    public void RenderOverlay(System.Text.StringBuilder sb)
    {
        if (_windows.Count == 0) return;

        // 收集所有模态窗口的遮罩区域
        var masks = _windows.Where(w => w.HasMask).ToList();

        // 渲染遮罩
        foreach (var mask in masks)
        {
            for (int row = 0; row < mask.Height; row++)
            {
                int screenY = mask.Y + row;
                if (screenY < 0 || screenY >= Console.WindowHeight) continue;
                sb.Append($"\x1b[{screenY + 1};{mask.X}H");
                sb.Append($"\x1b[100m{new string(' ', mask.Width)}\x1b[0m");
            }
        }

        // 按 Z-order 渲染窗口
        foreach (var win in _windows.OrderBy(w => w.ZOrder))
        {
            RenderWindow(sb, win);
        }
    }

    internal void RenderWindow(System.Text.StringBuilder sb, ManagedWindow win)
    {
        var (tl, tr, bl, br, hh, vv) = GetBorder(win);
        int bc = win.BorderColor;
        var (clipL, clipT, clipR, clipB) = Clip(win);

        // 上边框
        int titleFg = win.TitleFg > 0 ? win.TitleFg : bc;
        int titleBg = win.TitleBg;
        WriteClipped(sb, win.Y, win.X, tl, win, fg: bc);
        if (!string.IsNullOrEmpty(win.Title))
        {
            var titleText = $" {win.Title} ";
            WriteClipped(sb, win.Y, win.X + 1, titleText, win, fg: titleFg, bg: titleBg > 0 ? titleBg : null);
            var rem = win.Width - 2 - TuiHelper.DisplayWidth(titleText);
            if (rem > 0) WriteClipped(sb, win.Y, win.X + 1 + TuiHelper.DisplayWidth(titleText),
                new string(hh[0], rem), win, fg: bc);
        }
        else
        {
            WriteClipped(sb, win.Y, win.X + 1, new string(hh[0], win.Width - 2), win, fg: bc);
        }
        WriteClipped(sb, win.Y, win.X + win.Width - 1, tr, win, fg: bc);

        // 窗口背景填充（WinBg > 0 时用空格填满整个内部）
        if (win.WinBg > 0)
        {
            for (int r = 0; r < win.Height - 2; r++)
            {
                FillClipped(sb, win.Y + 1 + r, win.X + 1, win.Width - 2, win);
                // 同时写入背景色（通过覆写带颜色的空格）
                WriteClipped(sb, win.Y + 1 + r, win.X + 1,
                    new string(' ', win.Width - 2), win, bg: win.WinBg);
            }
        }

        // 内容
        if (win.Controls.Count > 0)
        {
            foreach (var ctrl in win.Controls.OrderBy(c => c.Y).ThenBy(c => c.X))
            {
                ctrl.Focused = (ctrl == win.FocusedControl);
                ctrl.Render(sb, win.X, win.Y);
            }
        }
        else if (win.MenuItems != null)
        {
            RenderMenuItemsClipped(sb, win);
        }
        else
        {
            // 始终渲染所有内部行（即使无内容也画竖边）
            int innerRows = win.Height - 2;
            for (int i = 0; i < innerRows; i++)
            {
                int row = win.Y + 1 + i;
                // 左框
                WriteClipped(sb, row, win.X, vv, win, fg: bc);
                // 内容
                if (i < win.ContentLines.Count)
                {
                    var line = win.ContentLines[i];
                    var contentVw = TuiHelper.DisplayWidth(line);
                    var maxContentVw = win.Width - 3;
                    if (contentVw > maxContentVw)
                        line = ClipTextVw(line, maxContentVw);
                    WriteClipped(sb, row, win.X + 1, $" {line}", win,
                        fg: win.ContentFg > 0 ? win.ContentFg : null,
                        bg: win.WinBg > 0 ? win.WinBg : null);
                }
                // 右框：绝对位置
                WriteClipped(sb, row, win.X + win.Width - 1, vv, win, fg: bc);
            }
        }

        // 底边框（用横线字符连接两角）
        WriteClipped(sb, win.Y + win.Height - 1, win.X, bl, win, fg: bc);
        WriteClipped(sb, win.Y + win.Height - 1, win.X + 1,
            new string(hh[0], win.Width - 2), win, fg: bc);
        WriteClipped(sb, win.Y + win.Height - 1, win.X + win.Width - 1, br, win, fg: bc);
    }

    internal void RenderMenuItemsClipped(System.Text.StringBuilder sb, ManagedWindow win)
    {
        var items = win.MenuItems!;
        int totalItems = items.Count;
        int visRows = win.Height - 2;
        if (visRows <= 0) return;
        var vv = GetBorder(win).v;

        int scroll = win.ScrollOffset;
        if (win.SelectedIndex < scroll) scroll = win.SelectedIndex;
        else if (win.SelectedIndex >= scroll + visRows) scroll = win.SelectedIndex - visRows + 1;
        scroll = Math.Clamp(scroll, 0, Math.Max(0, totalItems - visRows));
        win.ScrollOffset = scroll;
        bool canUp = scroll > 0, canDown = scroll + visRows < totalItems;

        for (int i = 0; i < visRows; i++)
        {
            int ci = scroll + i;
            int row = win.Y + 1 + i;
            int col = win.X + 1;  // 内容区起点

            // 左框（每行必画）
            WriteClipped(sb, row, win.X, vv, win, fg: win.BorderColor);

            // 滚动指示器 / 内容
            if (i == 0 && canUp)
            {
                WriteClipped(sb, row, col, $" ▲ {scroll} more ", win, fg: 2);
            }
            else if (i == visRows - 1 && canDown)
            {
                var remaining = totalItems - scroll - visRows;
                WriteClipped(sb, row, col, $" ▼ {remaining} more ", win, fg: 2);
            }
            else if (ci < totalItems)
            {
                var text = items[ci];
                if (text == MenuSeparator)
                {
                    WriteClipped(sb, row, col, new string('─', win.Width - 3), win, fg: 2);
                }
                else if (ci == win.SelectedIndex)
                {
                    // 满行高亮条
                    var label = $" {text} ";
                    var pad = win.Width - 2 - TuiHelper.DisplayWidth(label);
                    var fullText = pad > 0 ? label + new string(' ', pad) : label;
                    WriteClipped(sb, row, col, fullText, win, fg: win.SelFg, bg: win.SelBg);
                }
                else
                {
                    WriteClipped(sb, row, col, $" {text} ", win,
                        fg: win.ItemFg > 0 ? win.ItemFg : null);
                }
            }

            // 右框（每行绝对位置——覆盖溢出）
            WriteClipped(sb, row, win.X + win.Width - 1, vv, win, fg: win.BorderColor);
        }

        // 滚动条
        if (totalItems > visRows)
        {
            var barH = Math.Max(1, visRows * visRows / totalItems);
            var barPos = visRows * scroll / Math.Max(1, totalItems - visRows);
            barPos = Math.Clamp(barPos, 0, visRows - barH);
            for (int i = 0; i < visRows; i++)
            {
                int row = win.Y + 1 + i;
                var ch = (i >= barPos && i < barPos + barH) ? "█" : "│";
                WriteClipped(sb, row, win.X + win.Width, ch, win, fg: 2);
            }
        }
    }

    /// <summary>菜单键盘导航，返回选择的索引（-1=取消，-2=继续）</summary>
    public int HandleMenuKey(ManagedWindow win, ConsoleKeyInfo key)
    {
        if (win.MenuItems == null) return -1;

        return key.Key switch
        {
            ConsoleKey.UpArrow => Nav(-1),
            ConsoleKey.DownArrow => Nav(1),
            ConsoleKey.Home => Nav(-win.SelectedIndex),
            ConsoleKey.End => Nav(win.MenuItems.Count - 1 - win.SelectedIndex),
            ConsoleKey.PageUp => Nav(-5),
            ConsoleKey.PageDown => Nav(5),
            ConsoleKey.Enter => win.MenuItems![win.SelectedIndex] == MenuSeparator ? -2 : win.SelectedIndex,
            ConsoleKey.Escape => -1,
            _ => -2,  // 未处理
        };

        int Nav(int delta)
        {
            var items = win.MenuItems!;
            int newIdx = win.SelectedIndex;
            int attempts = 0;
            do {
                newIdx = Math.Clamp(newIdx + delta, 0, items.Count - 1);
                attempts++;
            } while (attempts < items.Count && items[newIdx] == MenuSeparator);
            win.SelectedIndex = newIdx;
            return -2;
        }
    }

    // ================================================================
    // 裁剪渲染（核心难点：超出窗口区域的内容必须被裁剪）
    // ================================================================

    /// <summary>裁剪矩形——包含边框的完整窗口区域</summary>
    private static (int L, int T, int R, int B) Clip(ManagedWindow w) =>
        (w.X, w.Y, w.X + w.Width, w.Y + w.Height);

    /// <summary>带裁剪的文本写入——超出窗口边界的部分自动截断/跳过</summary>
    internal static void WriteClipped(System.Text.StringBuilder sb,
        int row, int col, string text, ManagedWindow win,
        int? fg = null, int? bg = null)
    {
        var (clipL, clipT, clipR, clipB) = Clip(win);

        // 行超出裁剪区
        if (row < clipT || row >= clipB) return;
        // 列完全超出右边界
        if (col >= clipR) return;

        // 计算文本视觉宽度（text 不含转义符，直接算）
        var textVw = TuiHelper.DisplayWidth(text);
        var avail = clipR - col;
        if (avail <= 0) return;
        if (textVw > avail)
            text = ClipTextVw(text, avail);

        // 颜色（0=无色，等同于不设）
        bool hasFg = fg.HasValue && fg.Value > 0;
        bool hasBg = bg.HasValue && bg.Value > 0;
        if (hasFg || hasBg)
        {
            sb.Append($"\x1b[{row + 1};{col + 1}H");
            if (hasFg && hasBg) sb.Append($"\x1b[{fg};{bg}m");
            else if (hasFg) sb.Append($"\x1b[{fg}m");
            else sb.Append($"\x1b[{bg}m");
            sb.Append(text);
            sb.Append("\x1b[0m");
        }
        else
        {
            sb.Append($"\x1b[{row + 1};{col + 1}H{text}");
        }
    }

    /// <summary>按视觉宽度截断文本（保留 ANSI 码）</summary>
    private static string ClipTextVw(string text, int maxVw)
    {
        var clean = StripAnsi(text);
        if (TuiHelper.DisplayWidth(clean) <= maxVw) return text;

        var sb = new System.Text.StringBuilder();
        int vw = 0;
        bool inAnsi = false;
        for (int i = 0; i < text.Length && vw < maxVw; i++)
        {
            if (text[i] == '\x1b' && i + 1 < text.Length && text[i + 1] == '[')
            {
                int j = i;
                while (j < text.Length && text[j] != 'm') j++;
                sb.Append(text[i..(j + 1)]);
                i = j;
                continue;
            }
            if (!inAnsi)
            {
                var rune = System.Text.Rune.GetRuneAt(text, i);
                var w = TuiHelper.RuneWidth(rune);
                if (vw + w > maxVw) break;
                vw += w;
            }
            sb.Append(text[i]);
        }
        sb.Append("\x1b[0m");
        return sb.ToString();
    }

    /// <summary>填充一行空格（裁剪安全）</summary>
    private static void FillClipped(System.Text.StringBuilder sb,
        int row, int col, int count, ManagedWindow win)
    {
        var (clipL, clipT, clipR, clipB) = Clip(win);
        if (row < clipT || row >= clipB) return;
        if (col >= clipR) return;
        var actual = Math.Min(count, clipR - col);
        if (actual > 0)
            sb.Append($"\x1b[{row + 1};{col + 1}H{new string(' ', actual)}");
    }

    // ================================================================
    // 工具方法
    // ================================================================

    /// <summary>按视觉宽度折行</summary>
    private static List<string> WrapLine(string text, int maxVw)
    {
        var result = new List<string>();
        int start = 0;
        while (start < text.Length)
        {
            var slice = text.AsSpan(start);
            int vw = 0, chars = 0;
            foreach (var rune in slice.EnumerateRunes())
            {
                var w = TuiHelper.RuneWidth(rune);
                if (vw + w > maxVw) break;
                vw += w; chars += rune.Utf16SequenceLength;
            }
            if (chars == 0) chars = 1;
            result.Add(text[start..(start + chars)]);
            start += chars;
        }
        return result;
    }

    /// <summary>截断超出宽度的文本（保留 ANSI 颜色码）</summary>
    private static string ClipLine(string text, int maxVw)
    {
        var clean = StripAnsi(text);
        if (TuiHelper.DisplayWidth(clean) <= maxVw) return text;

        // 逐字符截断（跳过 ANSI 序列）
        var sb = new System.Text.StringBuilder();
        int vw = 0;
        bool inAnsi = false;
        for (int i = 0; i < text.Length && vw < maxVw; i++)
        {
            if (text[i] == '\x1b' && i + 1 < text.Length && text[i + 1] == '[')
            {
                int j = i;
                while (j < text.Length && text[j] != 'm') j++;
                sb.Append(text[i..(j + 1)]);
                i = j;
                continue;
            }
            if (!inAnsi)
            {
                var rune = System.Text.Rune.GetRuneAt(text, i);
                var w = TuiHelper.RuneWidth(rune);
                if (vw + w > maxVw) break;
                vw += w;
            }
            sb.Append(text[i]);
        }
        if (vw < maxVw) sb.Append(new string(' ', maxVw - vw));
        sb.Append("\x1b[0m"); // 确保 ANSI 关闭
        return sb.ToString();
    }

    private static string StripAnsi(string text)
    {
        if (!text.Contains('\x1b')) return text;
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\x1b' && i + 1 < text.Length && text[i + 1] == '[')
            {
                int j = i + 2;
                while (j < text.Length && text[j] != 'm') j++;
                i = j;
                continue;
            }
            sb.Append(text[i]);
        }
        return sb.ToString();
    }

    private static (string tl, string tr, string bl, string br, string h, string v) BoxChars(string? style = null)
        => (style ?? "single") switch
        {
            "double" => ("╔", "╗", "╚", "╝", "═", "║"),
            "rounded" => ("╭", "╮", "╰", "╯", "─", "│"),
            "thick" => ("┏", "┓", "┗", "┛", "━", "┃"),
            "solid" => ("█", "█", "█", "█", "▀", "▌"),
            "semisolid" => ("▄", "▄", "▀", "▀", "▀", "▐"),
            "dotted" => ("┌", "┐", "└", "┘", "┈", "┆"),
            "dashed" => ("┌", "┐", "└", "┘", "┅", "┇"),
            "ascii" => ("+", "+", "+", "+", "-", "|"),
            "slash" => ("╱", "╲", "╲", "╱", "╱", "╲"),      // 斜线角
            "triangle" => ("◣", "◤", "◥", "◢", "═", "║"),    // 三角角
            _ => ("┌", "┐", "└", "┘", "─", "│"),
        };

    /// <summary>根据窗口边框设置返回字符组（含自定义）</summary>
    private static (string tl, string tr, string bl, string br, string h, string v) GetBorder(ManagedWindow w)
    {
        if (w.BorderStyle == "custom" && !string.IsNullOrEmpty(w.CustomBorder) && w.CustomBorder.Length >= 6)
        {
            var r = System.Text.Rune.GetRuneAt(w.CustomBorder, 0).ToString();
            var chars = new List<string>();
            int pos = 0;
            while (pos < w.CustomBorder.Length && chars.Count < 6)
            {
                var rune = System.Text.Rune.GetRuneAt(w.CustomBorder, pos);
                chars.Add(rune.ToString());
                pos += rune.Utf16SequenceLength;
            }
            if (chars.Count >= 6)
                return (chars[0], chars[1], chars[2], chars[3], chars[4], chars[5]);
        }
        return BoxChars(w.BorderStyle);
    }
}

/// <summary>窗口对象——可承载多个控件的浮层容器</summary>
public class ManagedWindow
{
    public int X, Y, Width, Height;
    public string Title { get; set; } = "";
    public int ZOrder { get; set; }
    public bool Modal { get; set; }
    public bool HasMask { get; set; }
    public int BorderColor { get; set; } = 36;
    public string BorderStyle { get; set; } = "single";
    public string? CustomBorder { get; set; }

    // 配色（0=默认终端色/透明）
    public int WinBg { get; set; } = 0;       // 窗口背景色（填充整个内部）
    public int TitleFg { get; set; } = 0;     // 标题前景色
    public int TitleBg { get; set; } = 0;     // 标题背景色
    public int ContentFg { get; set; } = 0;   // 正文前景色
    public int ItemFg { get; set; } = 0;      // 非选中项前景色
    public int SelFg { get; set; } = 30;      // 选中项前景（默认黑字）
    public int SelBg { get; set; } = 46;      // 选中项背景（默认青底）

    // 内容（二选一：纯文本 或 控件列表）
    public List<string> ContentLines { get; set; } = [];
    public List<string>? MenuItems { get; set; }

    // 控件容器
    public List<UIControl> Controls { get; set; } = [];

    // 菜单状态
    public int SelectedIndex { get; set; }
    public int ScrollOffset { get; set; }

    // 焦点控件
    public UIControl? FocusedControl { get; set; }

    // 关闭回调
    public Action? OnClosed { get; set; }

    /// <summary>聚焦第一个可聚焦控件</summary>
    public void FocusFirst()
    {
        var focusable = Controls.Where(c => c is UIInput or UIButton).ToList();
        foreach (var c in Controls) c.Focused = false;
        FocusedControl = focusable.FirstOrDefault();
        if (FocusedControl != null) FocusedControl.Focused = true;
    }

    /// <summary>Tab 切换到下一个控件</summary>
    public void FocusNext()
    {
        var focusable = Controls.Where(c => c is UIInput or UIButton).ToList();
        if (focusable.Count == 0) return;
        var old = FocusedControl;
        foreach (var c in Controls) c.Focused = false;
        var idx = old != null ? focusable.IndexOf(old) : -1;
        idx = (idx + 1) % focusable.Count;
        FocusedControl = focusable[idx];
        FocusedControl.Focused = true;
    }

    /// <summary>Shift+Tab 切换到上一个控件</summary>
    public void FocusPrev()
    {
        var focusable = Controls.Where(c => c is UIInput or UIButton).ToList();
        if (focusable.Count == 0) return;
        var old = FocusedControl;
        foreach (var c in Controls) c.Focused = false;
        var idx = old != null ? focusable.IndexOf(old) : 0;
        idx = (idx - 1 + focusable.Count) % focusable.Count;
        FocusedControl = focusable[idx];
        FocusedControl.Focused = true;
    }

    /// <summary>清除所有控件的焦点</summary>
    public void ClearFocus()
    {
        foreach (var c in Controls) c.Focused = false;
        FocusedControl = null;
    }
}
