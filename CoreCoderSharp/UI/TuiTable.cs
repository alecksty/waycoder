namespace CoreCoderSharp.UI;

/// <summary>
/// 表格控件 —— 原生 ANSI 渲染，通过 AnsiText 封装层。
/// CJK 感知：使用 TuiHelper.DisplayWidth 自动对齐。
/// 渲染为纯字符串，输出到聊天区而非直接写终端。
/// </summary>
public class TuiTable
{
    private readonly string? _title;
    private readonly List<ColumnDef> _columns = [];
    private readonly List<RowDef> _rows = [];

    private record ColumnDef(string Header, int? Width);
    private record RowDef(List<CellDef> Cells, bool IsMarkup);
    private record CellDef(string Text, bool IsMarkup);

    public TuiTable(string? title = null)
    {
        _title = title;
    }

    /// <summary>添加一列</summary>
    public TuiTable AddColumn(string header, int? width = null)
    {
        _columns.Add(new ColumnDef(header, width));
        return this;
    }

    /// <summary>添加一个普通数据行（文本自动转义）</summary>
    public TuiTable AddRow(params string[] cells)
    {
        _rows.Add(new RowDef(
            cells.Select(c => new CellDef(c, IsMarkup: false)).ToList(),
            IsMarkup: false));
        return this;
    }

    /// <summary>添加一个带 ANSI 颜色码的行（调用方负责提供合法 ANSI 序列）</summary>
    public TuiTable AddMarkupRow(params string[] markupCells)
    {
        _rows.Add(new RowDef(
            markupCells.Select(c => new CellDef(c, IsMarkup: true)).ToList(),
            IsMarkup: true));
        return this;
    }

    /// <summary>渲染表格为 ANSI 字符串</summary>
    public string RenderToString()
    {
        if (_columns.Count == 0) return "";

        // 计算列宽
        var colWidths = CalcColumnWidths();

        var sb = new System.Text.StringBuilder();

        // 顶边框
        sb.Append("┌");
        for (int i = 0; i < _columns.Count; i++)
        {
            if (i > 0) sb.Append("┬");
            sb.Append(new string('─', colWidths[i] + 2));
        }
        sb.Append("┐");

        // 标题
        if (!string.IsNullOrEmpty(_title))
        {
            sb.Append($" {AnsiText.BoldFg(_title, TuiColors.Cyan)}");
        }
        sb.Append('\n');

        // 表头
        sb.Append("│");
        for (int i = 0; i < _columns.Count; i++)
        {
            var hdr = PadCenter(_columns[i].Header, colWidths[i]);
            sb.Append($" {AnsiText.Bold(hdr)} │");
        }
        sb.Append('\n');

        // 表头分隔线
        sb.Append("├");
        for (int i = 0; i < _columns.Count; i++)
        {
            if (i > 0) sb.Append("┼");
            sb.Append(new string('─', colWidths[i] + 2));
        }
        sb.Append("┤\n");

        // 数据行
        foreach (var row in _rows)
        {
            sb.Append("│");
            for (int i = 0; i < _columns.Count; i++)
            {
                var cellText = i < row.Cells.Count ? row.Cells[i].Text : "";
                var isMarkup = i < row.Cells.Count && row.Cells[i].IsMarkup;

                if (!isMarkup)
                    cellText = TuiHelper.Esc(cellText);

                var displayW = isMarkup
                    ? AnsiDisplayWidth(cellText)
                    : TuiHelper.DisplayWidth(cellText);

                var padR = colWidths[i] - displayW;
                if (padR < 0) padR = 0;

                sb.Append(' ');
                sb.Append(cellText);
                if (padR > 0) sb.Append(new string(' ', padR));
                sb.Append(" │");
            }
            sb.Append('\n');
        }

        // 底边框
        sb.Append("└");
        for (int i = 0; i < _columns.Count; i++)
        {
            if (i > 0) sb.Append("┴");
            sb.Append(new string('─', colWidths[i] + 2));
        }
        sb.Append("┘");

        return sb.ToString();
    }

    /// <summary>渲染表格。TUI 模式下每行单独注入聊天区（绕过 ANSI 批量渲染管线），非 TUI 模式直接写终端。</summary>
    public void Render()
    {
        var output = RenderToString();
        if (ScreenManager.Instance.IsActive)
        {
            // 每行作为独立消息注入，让 BuildChatScreenLines 逐行处理
            // 避免整个表格字符串进入 ANSI 检测路径后可能的渲染异常
            foreach (var line in output.Split('\n'))
            {
                if (line.Length > 0)
                    ScreenManager.Instance.AddSystemMsg(line);
            }
            // 主循环会在下一次迭代自动 Render()，这里不主动触发
        }
        else
        {
            Console.Write(output);
        }
    }

    // ---- 内部 ----

    private List<int> CalcColumnWidths()
    {
        var widths = new List<int>();
        for (int i = 0; i < _columns.Count; i++)
        {
            int maxW = TuiHelper.DisplayWidth(_columns[i].Header);

            foreach (var row in _rows)
            {
                if (i >= row.Cells.Count) continue;
                var cell = row.Cells[i];
                var w = cell.IsMarkup
                    ? AnsiDisplayWidth(cell.Text)
                    : TuiHelper.DisplayWidth(cell.Text);
                if (w > maxW) maxW = w;
            }

            if (_columns[i].Width is { } colW)
                maxW = colW;

            // 最小 3，最大 60
            maxW = Math.Max(3, Math.Min(60, maxW));
            widths.Add(maxW);
        }
        return widths;
    }

    /// <summary>居中对齐文本</summary>
    private static string PadCenter(string text, int width)
    {
        var tw = TuiHelper.DisplayWidth(text);
        if (tw >= width) return TuiHelper.TruncateByWidth(text, width);
        var left = (width - tw) / 2;
        var right = width - tw - left;
        return new string(' ', left) + text + new string(' ', right);
    }

    /// <summary>计算 ANSI 标记文本的显示宽度（忽略转义序列）</summary>
    private static int AnsiDisplayWidth(string text)
    {
        int w = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\x1b' && i + 1 < text.Length && text[i + 1] == '[')
            {
                while (i < text.Length && text[i] != 'm') i++;
                continue;
            }
            if (text[i] == '[' || text[i] == ']') continue; // Spectre 标记
            var rune = System.Text.Rune.GetRuneAt(text, i);
            w += TuiHelper.RuneWidth(rune);
            if (rune.Utf16SequenceLength > 1) i += rune.Utf16SequenceLength - 1;
        }
        return w;
    }
}
