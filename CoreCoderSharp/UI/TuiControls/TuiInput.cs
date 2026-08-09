using System.Text;
namespace CoreCoderSharp.UI.Controls;

/// <summary>单行文本输入框 —— 支持光标移动、插入、删除。</summary>
public class TuiInput : TuiControl
{
    public string Text { get; set; } = "";
    public int CursorPos { get; set; }
    public Action<string>? OnSubmit { get; set; }
    public bool Password { get; set; }

    public TuiInput() { Height = 1; Width = 20; }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        var originalText = Password ? new string('•', Text.Length) : Text;
        var visW = Width;

        // ── CJK 宽度感知的滚动逻辑 ──
        // 计算从 0 到 CursorPos 的视觉宽度
        int cursorVisualEnd = TuiHelper.DisplayWidth(originalText[..Math.Min(CursorPos, originalText.Length)]);

        // 确定滚动起始字符索引，使光标在可见区域内
        int scrollStart = 0;
        if (cursorVisualEnd >= visW)
        {
            // 光标超出右边界 → 向右滚动到光标可见
            int needSkip = cursorVisualEnd - visW + 1;
            int skipped = 0;
            for (int i = 0; i < originalText.Length; i++)
            {
                int rw = TuiHelper.RuneWidth(originalText.EnumerateRunes().ElementAt(i));
                if (skipped + rw > needSkip) break;
                skipped += rw;
                scrollStart = i + 1;
            }
        }

        // 截取可见文本，并按视觉宽度裁剪到 visW
        var visiblePart = originalText[scrollStart..];
        if (TuiHelper.DisplayWidth(visiblePart) > visW)
            visiblePart = TuiHelper.TruncateByWidth(visiblePart, visW);

        int vw = TuiHelper.DisplayWidth(visiblePart);
        var pad = Math.Max(0, visW - vw);

        int fg = Focused ? 37 : (Fg > 0 ? Fg : 0);
        int bg = Focused ? 44 : (Bg > 0 ? Bg : 0);

        // 用 WriteAt 写入（走裁剪通道）
        WriteAt(sb, absY, absX, visiblePart + new string(' ', pad), fg, bg);

        // ── 光标：记录位置，由 Screen 在最后统一输出 ──
        if (IsCursorOwner)
        {
            int cursorInVisible = TuiHelper.DisplayWidth(
                originalText[scrollStart..Math.Min(CursorPos, originalText.Length)]);
            _cursorRow = absY;
            _cursorCol = absX + Math.Min(cursorInVisible, visW - 1);
            _showCursor = true;
        }
        else
        {
            _showCursor = false;
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
            case ConsoleKey.End:  CursorPos = Text.Length; return true;
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
