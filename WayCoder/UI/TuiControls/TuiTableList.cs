using System.Text;

namespace WayCoder.UI.TuiControls;

/// <summary>
/// 可选中多列表格列表 —— 列头 + 数据行 + 选中反白 + 键盘导航 + 内联滚动条。
/// 补齐「多列对齐且可交互」这一缺口，供 ModelPicker / FilePicker 等多列选择场景复用。
/// 纯数据模型 + TuiHelper，AOT 安全（无反射）。
/// </summary>
public class TuiTableList : TuiControl
{
    /// <summary>列定义（标题 + 固定列宽，CJK 按 2 列计）</summary>
    public sealed class Column
    {
        public string Title { get; }
        public int Width { get; }
        public Column(string title, int width) { Title = title; Width = width; }
    }

    private readonly List<Column> _columns = [];
    private readonly List<string[]> _rows = [];

    /// <summary>当前选中行索引</summary>
    public int SelectedIndex { get; set; }

    /// <summary>数据区滚动偏移（行数，0=顶部）</summary>
    public int ScrollOffset { get; set; }

    /// <summary>是否渲染列头（列头 + 分隔线占顶部 2 行）</summary>
    public bool ShowHeader { get; set; } = true;

    /// <summary>列头前景色（0=主题 MdHeadingFg）</summary>
    public int HeaderFg { get; set; }

    /// <summary>列头背景色（0=继承窗口底色）</summary>
    public int HeaderBg { get; set; }

    /// <summary>选中行激活（Enter/空格）回调</summary>
    public Action<int>? OnSelect { get; set; }

    /// <summary>选中行变化回调</summary>
    public Action<int>? OnSelectionChanged { get; set; }

    public TuiTableList()
    {
        Height = 8;
        Width = 40;
    }

    // ── 数据 ──

    public int ColumnCount => _columns.Count;

    public int RowCount => _rows.Count;

    /// <summary>数据区可见行数（扣除列头与分隔线）</summary>
    public int VisibleDataRows => Math.Max(0, Height - (ShowHeader ? 2 : 0));

    /// <summary>各列宽度之和（终端显示宽度）</summary>
    private int TotalColWidth
    {
        get
        {
            int w = 0;
            foreach (var c in _columns) w += c.Width;
            return w;
        }
    }

    /// <summary>数据区宽度（右侧预留 1 列滚动条）</summary>
    private int DataWidth => Math.Max(1, Width - 1);

    public void AddColumn(string title, int width) => _columns.Add(new Column(title, width));

    public void AddRow(params string[] cells) => _rows.Add(cells);

    public void ClearRows() => _rows.Clear();

    public string GetCell(int row, int col) =>
        row >= 0 && row < _rows.Count && col >= 0 && col < _rows[row].Length ? _rows[row][col] : "";

    // ── 单元格格式化 ──

    /// <summary>截断 + 按显示宽度右填充，使单元格恰好占 width 列。</summary>
    private static string FormatCell(string cell, int width)
        => TuiHelper.PadRightByWidth(TuiHelper.TruncateByWidth(cell, width), width);

    /// <summary>把一行单元格拼成定宽字符串（各格已对齐）。</summary>
    private string FormatRow(string[] cells)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < _columns.Count; i++)
            sb.Append(FormatCell(i < cells.Length ? cells[i] : "", _columns[i].Width));
        return sb.ToString();
    }

    /// <summary>列头标题行纯文本（各标题已按列宽对齐，无分隔线）。</summary>
    private string FormatHeaderTitles()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < _columns.Count; i++)
            sb.Append(FormatCell(_columns[i].Title, _columns[i].Width));
        return sb.ToString();
    }

    /// <summary>列头纯文本（标题行 + 分隔线），无 ANSI，供渲染与自测复用。</summary>
    public string RenderHeader()
    {
        var sb = new StringBuilder();
        sb.Append(FormatHeaderTitles());
        sb.Append('\n');
        sb.Append(new string('─', TotalColWidth));
        return sb.ToString();
    }

    // ── 滚动 ──

    /// <summary>调整 ScrollOffset，保证选中行落在可见数据区。</summary>
    public void EnsureSelectedVisible()
    {
        int vis = VisibleDataRows;
        int total = _rows.Count;
        if (vis <= 0 || total == 0) return;

        int idx = Math.Clamp(SelectedIndex, 0, total - 1);
        if (idx < ScrollOffset) ScrollOffset = idx;
        if (idx >= ScrollOffset + vis) ScrollOffset = idx - vis + 1;
        ScrollOffset = Math.Clamp(ScrollOffset, 0, Math.Max(0, total - vis));
    }

    // ── 选择导航 ──

    public void SelectNext()
    {
        if (SelectedIndex < _rows.Count - 1)
        {
            SelectedIndex++;
            OnSelectionChanged?.Invoke(SelectedIndex);
        }
    }

    public void SelectPrev()
    {
        if (SelectedIndex > 0)
        {
            SelectedIndex--;
            OnSelectionChanged?.Invoke(SelectedIndex);
        }
    }

    /// <summary>激活选中行（触发 OnSelect）</summary>
    public void ActivateSelected() => OnSelect?.Invoke(SelectedIndex);

    // ── 渲染 ──

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        if (_columns.Count == 0) return;
        EnsureSelectedVisible();

        int total = _rows.Count;
        int dataWidth = DataWidth;
        int dataStart = absY;

        // 列头 + 分隔线
        if (ShowHeader && Height >= 3)
        {
            WriteTableRow(sb, absX, dataStart, FormatHeaderTitles(),
                HeaderFg > 0 ? HeaderFg : TuiTheme.Current.MdHeadingFg, HeaderBg, dataWidth);
            WriteTableRow(sb, absX, dataStart + 1, new string('─', TotalColWidth),
                TuiTheme.Current.SeparatorFg, 0, dataWidth);
            dataStart += 2;
        }

        // 数据行
        int vis = Math.Min(VisibleDataRows, Height - (dataStart - absY));
        for (int i = 0; i < vis; i++)
        {
            int idx = ScrollOffset + i;
            if (idx >= total) break;
            bool selected = idx == SelectedIndex;
            WriteTableRow(sb, absX, dataStart + i, FormatRow(_rows[idx]),
                selected ? TuiTheme.Current.ListSelFg : (Fg > 0 ? Fg : TuiTheme.Current.ListFg),
                selected ? TuiTheme.Current.ListSelBg : 0, dataWidth);
        }

        // 内联滚动条
        if (total > vis && vis > 0)
        {
            int barH = Math.Max(1, vis * vis / total);
            int maxOff = Math.Max(1, total - vis);
            int barPos = Math.Clamp(vis * ScrollOffset / maxOff, 0, vis - barH);
            for (int i = 0; i < vis; i++)
            {
                var ch = (i >= barPos && i < barPos + barH) ? "█" : "│";
                var fg = (i >= barPos && i < barPos + barH)
                    ? TuiTheme.Current.SeekBarThumbFg
                    : TuiTheme.Current.SeparatorFg;
                var rb = new Terminal.RenderBuffer();
                rb.Write(dataStart + i, absX + Width - 1, ch, fg: fg);
                sb.Append(rb.ToString());
            }
        }
    }

    /// <summary>写入一行表格文本（截断到 dataWidth，背景反白覆盖整行）</summary>
    private void WriteTableRow(StringBuilder sb, int absX, int row, string line, int fg, int bg, int dataWidth)
    {
        if (TuiHelper.DisplayWidth(line) > dataWidth)
            line = TuiHelper.TruncateByWidth(line, dataWidth);
        WriteAt(sb, row, absX, line + new string(' ', Math.Max(0, dataWidth - TuiHelper.DisplayWidth(line))), fg, bg);
    }

    // ── 输入 ──

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (!IsEnabled) return false;
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                SelectPrev();
                return true;
            case ConsoleKey.DownArrow:
                SelectNext();
                return true;
            case ConsoleKey.Home:
                SelectedIndex = 0;
                EnsureSelectedVisible();
                return true;
            case ConsoleKey.End:
                SelectedIndex = _rows.Count - 1;
                EnsureSelectedVisible();
                return true;
            case ConsoleKey.PageUp:
                SelectedIndex = Math.Max(0, SelectedIndex - Math.Max(1, VisibleDataRows));
                EnsureSelectedVisible();
                return true;
            case ConsoleKey.PageDown:
                SelectedIndex = Math.Min(_rows.Count - 1, SelectedIndex + Math.Max(1, VisibleDataRows));
                EnsureSelectedVisible();
                return true;
            case ConsoleKey.Enter:
            case ConsoleKey.Spacebar:
                ActivateSelected();
                return true;
        }
        return false;
    }
}
