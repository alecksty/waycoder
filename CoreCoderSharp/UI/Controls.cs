using System.Text;

namespace CoreCoderSharp.UI;

// ================================================================
// 基于 BoxBuffer 的 UI 控件体系
// 所有矩形控件继承 BoxBuffer，自动获得 CJK 对齐、边框、颜色能力
// ================================================================

/// <summary>
/// Label — 静态文本标签，可选对齐方式
/// </summary>
public class Label : BoxBuffer
{
    public string Text { get; set; } = "";
    public TextAlign Align { get; set; } = TextAlign.Left;

    public new void Render(StringBuilder sb)
    {
        base.Render(sb);
        if (string.IsNullOrEmpty(Text)) return;
        var lines = Text.Split('\n');
        for (int i = 0; i < lines.Length && i < ContentHeight; i++)
        {
            var line = lines[i];
            var lineVW = VW(line);
            int pad = Align switch
            {
                TextAlign.Center => Math.Max(0, (ContentWidth - lineVW) / 2),
                TextAlign.Right => Math.Max(0, ContentWidth - lineVW),
                _ => 0,
            };
            WriteAt(sb, i, pad, line);
        }
    }
}

public enum TextAlign { Left, Center, Right }

/// <summary>
/// Button — 可点击按钮，用回车/空格触发
/// </summary>
public class Button : BoxBuffer
{
    public string Text { get; set; } = "OK";
    public string ActiveFg { get; set; } = "30";
    public string ActiveBg { get; set; } = "46"; // 高亮背景
    public bool Focused { get; set; }

    public new void Render(StringBuilder sb)
    {
        base.Render(sb);
        var label = $" {Text} ";
        var labelVW = VW(label);
        var pad = Math.Max(0, (ContentWidth - labelVW) / 2);
        var fullText = new string(' ', pad) + label + new string(' ', ContentWidth - pad - labelVW);

        if (Focused)
            WriteLineHighlight(sb, 0, ActiveFg, ActiveBg, fullText);
        else
            WriteLine(sb, 0, 0, fullText);
    }

    /// <summary>回车/空格 = 点击</summary>
    public bool HandleKey(ConsoleKeyInfo key) =>
        key.Key is ConsoleKey.Enter or ConsoleKey.Spacebar;
}

/// <summary>
/// DialogBox — 模态对话框 = 标题 + 内容行 + 按钮栏
/// </summary>
public class DialogBox : BoxBuffer
{
    public string? Title { get; set; }
    public string Message { get; set; } = "";
    public List<string> Buttons { get; set; } = ["确定"];
    public int FocusedBtn { get; set; }
    public string FocusFg { get; set; } = "30";
    public string FocusBg { get; set; } = "46";

    /// <summary>计算适合内容的尺寸</summary>
    public void AutoSize()
    {
        var lines = Message.Split('\n');
        int maxW = Title != null ? VW(Title) + 4 : 0;
        foreach (var l in lines) { var w = VW(l); if (w > maxW) maxW = w; }
        // 按钮栏宽度
        int btnW = Buttons.Sum(b => VW(b) + 4) + 2;
        if (btnW > maxW) maxW = btnW;
        Width = Math.Max(20, maxW + 4);
        Height = lines.Length + (Title != null ? 1 : 0) + 3; // 内容 + 标题 + 空行 + 按钮行 + 底边
    }

    public new void Render(StringBuilder sb)
    {
        base.Render(sb);
        int row = 0;

        // 标题
        if (Title != null)
        {
            WriteLine(sb, row, 0, $" \x1b[1m{Title}\x1b[0m");
            row++;
        }

        // 消息
        var lines = Message.Split('\n');
        foreach (var line in lines)
        {
            var pad = Math.Max(0, (ContentWidth - VW(line)) / 2);
            WriteAt(sb, row, pad, line);
            row++;
        }

        // 空行
        row++;

        // 按钮栏 (居中排列)
        if (Buttons.Count > 0 && row < ContentHeight)
        {
            var totalBW = Buttons.Sum(b => VW(b) + 4); // [ text ]
            var gap = Buttons.Count > 1 ? (ContentWidth - totalBW) / (Buttons.Count - 1) : 0;
            var startX = Buttons.Count == 1 ? (ContentWidth - totalBW) / 2 : 2;
            int x = Math.Max(0, startX);

            for (int i = 0; i < Buttons.Count; i++)
            {
                var btnLabel = $"[ {Buttons[i]} ]";
                if (i == FocusedBtn)
                    WriteAt(sb, row, x, $"\x1b[{FocusFg}m\x1b[{FocusBg}m{btnLabel}\x1b[0m");
                else
                    WriteAt(sb, row, x, btnLabel);
                x += VW(btnLabel) + (i < Buttons.Count - 1 ? Math.Max(1, gap) : 0);
            }
        }
    }

    /// <summary>←→ 切换按钮焦点，Enter 确认</summary>
    public bool HandleKey(ConsoleKeyInfo key, out int clickedIdx)
    {
        clickedIdx = -1;
        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:
                FocusedBtn = (FocusedBtn - 1 + Buttons.Count) % Buttons.Count;
                return false;
            case ConsoleKey.RightArrow:
                FocusedBtn = (FocusedBtn + 1) % Buttons.Count;
                return false;
            case ConsoleKey.Enter or ConsoleKey.Spacebar:
                clickedIdx = FocusedBtn;
                return true;
            case ConsoleKey.Escape:
                clickedIdx = -1;
                return true;
        }
        return false;
    }

    // ================================================================
    // 静态便捷方法
    // ================================================================

    /// <summary>显示消息对话框（确定按钮）</summary>
    public static void ShowMessage(string title, string message,
        string borderColor = "36")
    {
        var sm = ScreenManager.Instance;
        var wasActive = sm.IsActive;
        if (!wasActive) sm.Enter();

        try
        {
            var dialog = new DialogBox
            {
                Title = title, Message = message, Buttons = ["确定"],
                FgColor = borderColor, Border = BorderStyle.Single,
            };
            dialog.AutoSize();
            dialog.X = Math.Max(1, (Console.WindowWidth - dialog.Width) / 2);
            dialog.Y = Math.Max(1, (Console.WindowHeight - dialog.Height) / 2);

            while (true)
            {
                Console.CursorVisible = false;
                var sb = new StringBuilder();
                // 遮罩
                for (int i = 0; i < Console.WindowHeight; i++)
                    sb.Append($"\x1b[{i + 1};1H\x1b[100m{new string(' ', Console.WindowWidth)}\x1b[0m");
                dialog.Render(sb);
                Console.Write(sb.ToString());

                var key = Console.ReadKey(intercept: true);
                if (dialog.HandleKey(key, out _)) break;
            }
        }
        finally
        {
            Console.CursorVisible = true;
            if (!wasActive) sm.Exit(); else sm.Render();
        }
    }

    /// <summary>显示确认对话框，返回按钮索引（0=确定, 1=取消, -1=Esc）</summary>
    public static int ShowConfirm(string title, string message,
        string okText = "确定", string cancelText = "取消")
    {
        var sm = ScreenManager.Instance;
        var wasActive = sm.IsActive;
        if (!wasActive) sm.Enter();

        try
        {
            var dialog = new DialogBox
            {
                Title = title, Message = message,
                Buttons = [okText, cancelText], FocusedBtn = 0,
                FgColor = "33", Border = BorderStyle.Single,
            };
            dialog.AutoSize();
            dialog.X = Math.Max(1, (Console.WindowWidth - dialog.Width) / 2);
            dialog.Y = Math.Max(1, (Console.WindowHeight - dialog.Height) / 2);

            while (true)
            {
                Console.CursorVisible = false;
                var sb = new StringBuilder();
                for (int i = 0; i < Console.WindowHeight; i++)
                    sb.Append($"\x1b[{i + 1};1H\x1b[100m{new string(' ', Console.WindowWidth)}\x1b[0m");
                dialog.Render(sb);
                Console.Write(sb.ToString());

                var key = Console.ReadKey(intercept: true);
                if (dialog.HandleKey(key, out var clicked))
                    return clicked;
            }
        }
        finally
        {
            Console.CursorVisible = true;
            if (!wasActive) sm.Exit(); else sm.Render();
        }
    }
}

/// <summary>
/// ListBox — 可选择列表（无标题的 ScrollMenu）
/// </summary>
public class ListBox : BoxBuffer
{
    public List<string> Items { get; set; } = [];
    public int SelectedIndex { get; set; }
    public int ScrollOffset { get; private set; }
    public string HighlightFg { get; set; } = "30";
    public string HighlightBg { get; set; } = "46";

    public void EnsureVisible()
    {
        if (SelectedIndex < ScrollOffset) ScrollOffset = SelectedIndex;
        if (SelectedIndex >= ScrollOffset + ContentHeight) ScrollOffset = SelectedIndex - ContentHeight + 1;
        ScrollOffset = Math.Clamp(ScrollOffset, 0, Math.Max(0, Items.Count - ContentHeight));
    }

    public bool HandleKey(ConsoleKeyInfo key, out bool cancelled)
    {
        cancelled = false;
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                if (SelectedIndex > 0) SelectedIndex--; EnsureVisible(); return false;
            case ConsoleKey.DownArrow:
                if (SelectedIndex < Items.Count - 1) SelectedIndex++; EnsureVisible(); return false;
            case ConsoleKey.Enter: return true;
            case ConsoleKey.Escape: cancelled = true; return true;
        }
        return false;
    }

    public new void Render(StringBuilder sb)
    {
        base.Render(sb);
        int visible = Math.Min(Items.Count, ContentHeight);
        for (int i = 0; i < visible; i++)
        {
            int idx = ScrollOffset + i;
            var text = " " + Items[idx];
            if (idx == SelectedIndex)
                WriteLineHighlight(sb, i, HighlightFg, HighlightBg, text);
            else
                WriteLine(sb, i, 0, text);
        }
    }
}

/// <summary>
/// EditBox — 单行文本输入框
/// </summary>
public class EditBox : BoxBuffer
{
    public string Text { get; set; } = "";
    public int CursorPos { get; set; }
    public bool IsPassword { get; set; }
    public bool Focused { get; set; } = true;

    public new void Render(StringBuilder sb)
    {
        base.Render(sb);
        var display = IsPassword && Text.Length > 0
            ? new string('•', Text.Length) : Text;
        // 截断到内容宽度
        var cw = ContentWidth - 1;
        if (VW(display) > cw)
            display = TruncateByVW(display, cw - 1) + "…";

        WriteLine(sb, 0, 0, " " + display);

        // 光标（如果聚焦）
        if (Focused)
        {
            var absRow = ContentTop;
            var absCol = ContentLeft + 1 + Math.Min(CursorPos, cw);
            sb.Append($"\x1b[{absRow};{absCol}H\x1b[?25h");
        }
    }

    public void HandleKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.LeftArrow: if (CursorPos > 0) CursorPos--; break;
            case ConsoleKey.RightArrow: if (CursorPos < Text.Length) CursorPos++; break;
            case ConsoleKey.Home: CursorPos = 0; break;
            case ConsoleKey.End: CursorPos = Text.Length; break;
            case ConsoleKey.Backspace:
                if (CursorPos > 0) { Text = Text[..(CursorPos - 1)] + Text[CursorPos..]; CursorPos--; }
                break;
            case ConsoleKey.Delete:
                if (CursorPos < Text.Length) Text = Text[..CursorPos] + Text[(CursorPos + 1)..];
                break;
            default:
                if (key.KeyChar >= ' ')
                { Text = Text[..CursorPos] + key.KeyChar + Text[CursorPos..]; CursorPos++; }
                break;
        }
    }
}
