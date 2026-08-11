using WayCoder.Terminal;

namespace WayCoder.UI.TuiControls;

/// <summary>
/// 快捷键速查面板 —— 全屏帮助，类似 CommandPalette 风格。
/// 按 Ctrl+H 或 F1 打开，Esc 关闭，↑↓ 滚动浏览。
/// </summary>
public static class TuiKeybindHelp
{
    private static readonly List<(string Category, List<(string Key, string Desc)> Bindings)> Groups =
    [
        ("🌐 全局", [
            ("F1 - F10", "切换工作区槽位"),
            ("Ctrl+C", "中断当前 Agent 操作"),
            ("Ctrl+Q", "退出 WayCoder"),
            ("Ctrl+L", "清屏（保留输入区）"),
            ("Ctrl+S", "保存当前会话"),
        ]),
        ("✏ 编辑", [
            ("Enter", "发送消息"),
            ("Ctrl+Enter", "输入区换行"),
            ("Shift+Enter", "插入空行"),
            ("Tab", "切换焦点（输入区 ↔ 列表）"),
            ("Ctrl+V", "粘贴（超长/多行时确认）"),
            ("Ctrl+Up / Down", "输入历史翻页"),
        ]),
        ("🔄 模式", [
            ("Shift+Tab", "切换工作模式 (Build→Plan→Review→Auto)"),
            ("Ctrl+M", "打开模型选择对话框"),
            ("Ctrl+G", "打开文件选择对话框"),
            ("Ctrl+P", "打开命令面板"),
        ]),
        ("🧭 导航", [
            ("↑↓", "聊天列表滚动"),
            ("PgUp / PgDn", "聊天列表翻页"),
            ("Home / End", "跳到列表顶部/底部"),
            ("←→", "输入区光标移动"),
        ]),
        ("🖱 鼠标", [
            ("左键点击", "选中/确认"),
            ("滚轮", "列表/面板滚动"),
            ("拖拽标题栏", "移动浮窗"),
            ("拖拽边缘", "缩放浮窗"),
        ]),
        ("🛠 工具", [
            ("Ctrl+D", "显示 Diff 预览"),
            ("Ctrl+R", "切换推理深度"),
            ("F5", "刷新/重绘界面"),
            ("Ctrl+H", "打开本帮助面板"),
        ]),
    ];

    private static int _scrollOffset;
    private static int _totalLines;
    private static int _visibleRows;

    /// <summary>
    /// 显示快捷键速查面板。返回 true 表示面板正常关闭。
    /// </summary>
    public static bool Show()
    {
        try
        {
            _scrollOffset = 0;
            var (tw, th) = (Tty.Cols, Tty.Rows);
            _visibleRows = th - 4;

            while (true)
            {
                Render(tw, th);

                if (!Console.KeyAvailable)
                {
                    Thread.Sleep(30);
                    continue;
                }

                var key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Q ||
                    (key.Modifiers == ConsoleModifiers.Control && key.Key == ConsoleKey.H))
                    break;
                if (key.Key == ConsoleKey.DownArrow)
                    _scrollOffset = Math.Min(_scrollOffset + 1, Math.Max(0, _totalLines - _visibleRows));
                if (key.Key == ConsoleKey.UpArrow)
                    _scrollOffset = Math.Max(0, _scrollOffset - 1);
                if (key.Key == ConsoleKey.PageDown)
                    _scrollOffset = Math.Min(_scrollOffset + _visibleRows / 2, Math.Max(0, _totalLines - _visibleRows));
                if (key.Key == ConsoleKey.PageUp)
                    _scrollOffset = Math.Max(0, _scrollOffset - _visibleRows / 2);
                if (key.Key == ConsoleKey.Home)
                    _scrollOffset = 0;
                if (key.Key == ConsoleKey.End)
                    _scrollOffset = Math.Max(0, _totalLines - _visibleRows);
            }
            return true;
        }
        catch { return false; }
    }

    private static void Render(int tw, int th)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(AnsiTty.CursorHide).Append(AnsiTty.Home);

        // 标题栏
        var title = "⌨ 快捷键速查  (Esc 关闭, ↑↓ 滚动)";
        var titleBg = AnsiTty.Bg(46);
        sb.Append(AnsiTty.Fg(30)).Append(titleBg)
          .Append($"  {title}")
          .Append(new string(' ', Math.Max(0, tw - VW(title) - 2)))
          .Append(AnsiTty.SgrReset).Append('\n');

        // 构建全部行
        var allLines = new List<(string text, bool isHeader)>();
        foreach (var (cat, bindings) in Groups)
        {
            allLines.Add((cat, true));
            foreach (var (key, desc) in bindings)
                allLines.Add(($"  {key.PadRight(20)} → {desc}", false));
        }
        _totalLines = allLines.Count;

        // 内容区
        int contentTop = 2;
        int contentH = th - 3;
        _visibleRows = contentH;

        for (int i = 0; i < contentH; i++)
        {
            int li = _scrollOffset + i;
            sb.Append(AnsiTty.CursorPos(contentTop + i, 1)).Append(AnsiTty.ClearToEnd);

            if (li >= allLines.Count) continue;
            var (text, isHeader) = allLines[li];

            if (isHeader)
            {
                sb.Append(AnsiTty.FgBg(30, 47)).Append(text)
                  .Append(new string(' ', Math.Max(0, tw - VW(text) - 1)))
                  .Append(AnsiTty.SgrReset);
            }
            else
            {
                // 高亮键名列
                var arrowIdx = text.IndexOf(" → ");
                if (arrowIdx > 0)
                {
                    var keyPart = text[..arrowIdx].Trim();
                    var descPart = text[(arrowIdx + 3)..];
                    sb.Append(AnsiTty.Fg(36)).Append(keyPart)
                      .Append(AnsiTty.SgrDim).Append(" → ")
                      .Append(AnsiTty.Fg(37)).Append(descPart)
                      .Append(AnsiTty.SgrReset);
                }
                else
                {
                    sb.Append(text);
                }
            }
        }

        // 滚动指示
        if (_totalLines > _visibleRows)
        {
            var pct = _totalLines > 0 ? (double)_scrollOffset / (_totalLines - _visibleRows) * 100 : 0;
            sb.Append(AnsiTty.CursorPos(th, 1))
              .Append(AnsiTty.SgrDim).Append($"  {pct:F0}% | {_scrollOffset + 1}-{Math.Min(_scrollOffset + _visibleRows, _totalLines)}/{_totalLines}")
              .Append(AnsiTty.SgrReset);
        }

        Console.Out.Write(sb.ToString());
    }

    private static int VW(string s) => TuiHelper.DisplayWidth(s);
}
