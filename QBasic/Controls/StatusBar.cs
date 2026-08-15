// =============================================================
// StatusBar.cs —— 底部状态栏
//
// 在屏幕底部显示状态文本（如 行:列、Insert 模式、文件名）。
// 支持左右分区显示。
// =============================================================
using QBasic.Tui;

namespace QBasic.Controls;

/// <summary>底部状态栏。</summary>
public class StatusBar
{
    public string Left { get; set; } = "";
    public string Right { get; set; } = "";
    public Color Fg { get; set; } = Color.BrightWhite;
    public Color Bg { get; set; } = Color.Blue;

    public void Draw(Screen screen, int row)
    {
        for (int c = 0; c < screen.Cols; c++) screen.Put(row - 1, c, ' ', Fg, Bg);
        screen.PutText(row - 1, 1, Cjk.Fit(Left, screen.Cols - 1), Fg, Bg);
        int rightW = Cjk.Width(Right);
        if (rightW > 0)
            screen.PutText(row - 1, Math.Max(1, screen.Cols - rightW), Right, Fg, Bg);
    }
}
