namespace CoreCoderSharp.UI;

/// <summary>
/// 单元格尺寸定义 —— 固定像素或按权重分配。
/// 语法："10" = 固定 10 行/列，"20*" = 权重 20 的弹性行/列。
/// 类似 CSS Grid 的 px/fr 概念。
/// </summary>
public readonly struct GridSize
{
    /// <summary>固定像素值（>0）或权重（Star=true 时）</summary>
    public int Value { get; init; }

    /// <summary>true = 弹性分配（按权重）；false = 固定像素</summary>
    public bool IsStar { get; init; }

    /// <summary>解析单个定义："10"、"20*"、"*"（等价 1*）</summary>
    public static GridSize Parse(string raw)
    {
        var s = raw.Trim();
        if (s.EndsWith('*'))
        {
            var num = s.Length > 1 ? s[..^1] : "1";
            return new GridSize { Value = int.TryParse(num, out var v) ? Math.Max(1, v) : 1, IsStar = true };
        }
        return new GridSize { Value = int.TryParse(s, out var p) ? Math.Max(1, p) : 1, IsStar = false };
    }

    /// <summary>批量解析 "10,20*,30*,10" → GridSize[]</summary>
    public static GridSize[] ParseList(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return [];
        return csv.Split(',').Select(Parse).ToArray();
    }

    public override string ToString() => IsStar ? $"{Value}*" : $"{Value}";
}

/// <summary>
/// 网格布局容器 —— 二维行列布局，支持固定/弹性尺寸混合。
///
/// 用法：
/// <code>
/// var grid = new TuiGrid { Width = 80, Height = 24 };
/// grid.RowDefinitions = "10,20*,30*,10";   // 1行固定10, 2行30%弹性, 3行20%弹性, 4行固定10
/// grid.ColumnDefinitions = "100*,100*";    // 两列各占 50%
/// grid.Add(label, row: 0, col: 0);
/// grid.Add(button, row: 1, col: 1);
/// </code>
/// </summary>
public class TuiGrid : TuiView
{
    /// <summary>网格单元定义</summary>
    private readonly List<GridCell> _cells = [];

    /// <summary>
    /// 行定义字符串，如 "10,20*,30*,10"。
    /// 纯数字 = 固定像素高；数字+* = 按权重分配剩余高度。
    /// </summary>
    public string RowDefinitions
    {
        get => string.Join(",", _rowDefs.Select(d => d.ToString()));
        set => _rowDefs = GridSize.ParseList(value);
    }

    /// <summary>
    /// 列定义字符串，如 "100*,100*"。
    /// 纯数字 = 固定像素宽；数字+* = 按权重分配剩余宽度。
    /// </summary>
    public string ColumnDefinitions
    {
        get => string.Join(",", _colDefs.Select(d => d.ToString()));
        set => _colDefs = GridSize.ParseList(value);
    }

    private GridSize[] _rowDefs = [];
    private GridSize[] _colDefs = [];

    /// <summary>行列间距</summary>
    public int ColGap { get; set; } = 1;
    public int RowGap { get; set; }

    private int _rows, _cols;

    /// <summary>添加控件到网格指定位置</summary>
    public void Add(TuiControl child, int row, int col, int rowSpan = 1, int colSpan = 1)
    {
        child.Parent = this;
        Children.Add(child);
        _cells.Add(new GridCell { Child = child, Row = row, Col = col, RowSpan = rowSpan, ColSpan = colSpan });
        _rows = Math.Max(_rows, row + rowSpan);
        _cols = Math.Max(_cols, col + colSpan);
    }

    /// <summary>获取列数</summary>
    public int Columns => _cols;

    /// <summary>获取行数</summary>
    public int Rows => _rows;

    /// <summary>
    /// 按索引设置单行定义（便捷方法，用于动态构建）
    /// </summary>
    public void SetRowDef(int index, string def)
    {
        EnsureDefCount(ref _rowDefs, index + 1);
        _rowDefs[index] = GridSize.Parse(def);
    }

    /// <summary>
    /// 按索引设置单列定义
    /// </summary>
    public void SetColDef(int index, string def)
    {
        EnsureDefCount(ref _colDefs, index + 1);
        _colDefs[index] = GridSize.Parse(def);
    }

    private static void EnsureDefCount(ref GridSize[] arr, int minLen)
    {
        if (arr.Length >= minLen) return;
        var old = arr;
        arr = new GridSize[minLen];
        Array.Copy(old, arr, old.Length);
        for (int i = old.Length; i < minLen; i++)
            arr[i] = new GridSize { Value = 1, IsStar = true }; // 默认弹性
    }

    /// <summary>获取行定义（自动补齐）</summary>
    private GridSize GetRowDef(int row)
        => row < _rowDefs.Length ? _rowDefs[row] : new GridSize { Value = 1, IsStar = true };

    /// <summary>获取列定义（自动补齐）</summary>
    private GridSize GetColDef(int col)
        => col < _colDefs.Length ? _colDefs[col] : new GridSize { Value = 1, IsStar = true };

    public override void Layout()
    {
        if (_cells.Count == 0) { Height = 1; Width = 1; return; }

        // ── 解析列宽 ──
        var colW = ResolveSizes(_cols, _colDefs, Width, ColGap);

        // ── 解析行高 ──
        var rowH = ResolveSizes(_rows, _rowDefs, Height, RowGap);

        // ── 计算列起始 X ──
        var colX = new int[_cols];
        int x = 0;
        for (int c = 0; c < _cols; c++)
        {
            colX[c] = x;
            x += colW[c] + ColGap;
        }

        // ── 计算行起始 Y ──
        var rowY = new int[_rows];
        int y = 0;
        for (int r = 0; r < _rows; r++)
        {
            rowY[r] = y;
            y += rowH[r] + RowGap;
        }

        // ── 放置子控件 ──
        foreach (var cell in _cells)
        {
            int cellW = 0;
            for (int c = cell.Col; c < cell.Col + cell.ColSpan && c < _cols; c++)
                cellW += colW[c] + (c > cell.Col ? ColGap : 0);

            int cellH = 0;
            for (int r = cell.Row; r < cell.Row + cell.RowSpan && r < _rows; r++)
                cellH += rowH[r] + (r > cell.Row ? RowGap : 0);

            cell.Child.X = colX[cell.Col] + AlignChildX(cell.Child, cellW);
            cell.Child.Y = rowY[cell.Row] + AlignChildY(cell.Child, cellH);
            cell.Child.Width = ChildHAlign == HAlign.Stretch ? cellW : Math.Min(cell.Child.Width, cellW);
            cell.Child.Height = ChildVAlign == VAlign.Stretch ? cellH : Math.Min(cell.Child.Height, cellH);
        }

        // 容器尺寸 = 所有行/列的总和（包含间距）
        Width = Math.Max(1, _cols > 0 ? colX[^1] + colW[^1] : 1);
        Height = Math.Max(1, _rows > 0 ? rowY[^1] + rowH[^1] : 1);
    }

    /// <summary>
    /// 根据定义列表解析实际像素尺寸。
    /// 先分配固定尺寸，剩余空间按星号权重分配给弹性行列。
    /// </summary>
    private static int[] ResolveSizes(int count, GridSize[] defs, int totalSpace, int gap)
    {
        var sizes = new int[count];

        // 第一遍：收集固定尺寸 + 弹性权重
        int fixedTotal = 0;
        int starTotal = 0;
        for (int i = 0; i < count; i++)
        {
            var def = i < defs.Length ? defs[i] : new GridSize { Value = 1, IsStar = true };
            if (def.IsStar)
            {
                starTotal += def.Value;
            }
            else
            {
                sizes[i] = def.Value;
                fixedTotal += def.Value;
            }
        }

        // 剩余空间 = 总空间 - 固定尺寸 - 间距
        int gapsTotal = gap * Math.Max(0, count - 1);
        int remaining = Math.Max(0, totalSpace - fixedTotal - gapsTotal);

        // 第二遍：按权重分配剩余空间给弹性行列
        if (starTotal > 0 && remaining > 0)
        {
            int allocated = 0;
            for (int i = 0; i < count; i++)
            {
                var def = i < defs.Length ? defs[i] : new GridSize { Value = 1, IsStar = true };
                if (!def.IsStar) continue;

                // 按权重比例分配，最后一行/列拿剩余全部
                int share = (i == LastStarIndex(count, defs))
                    ? remaining - allocated
                    : Math.Max(1, remaining * def.Value / starTotal);
                sizes[i] = share;
                allocated += share;
            }
        }
        else if (starTotal > 0)
        {
            // 无剩余空间：弹性行列最小 1px
            for (int i = 0; i < count; i++)
            {
                var def = i < defs.Length ? defs[i] : new GridSize { Value = 1, IsStar = true };
                if (def.IsStar) sizes[i] = 1;
            }
        }

        // 保证最小尺寸
        for (int i = 0; i < count; i++)
            if (sizes[i] < 1) sizes[i] = 1;

        return sizes;
    }

    /// <summary>找到最后一个弹性列的索引</summary>
    private static int LastStarIndex(int count, GridSize[] defs)
    {
        for (int i = count - 1; i >= 0; i--)
        {
            var def = i < defs.Length ? defs[i] : new GridSize { Value = 1, IsStar = true };
            if (def.IsStar) return i;
        }
        return -1;
    }

    private int AlignChildX(TuiControl child, int cellW) =>
        ChildHAlign switch
        {
            HAlign.Center => (cellW - child.Width) / 2,
            HAlign.Right => cellW - child.Width,
            _ => 0
        };

    private int AlignChildY(TuiControl child, int cellH) =>
        ChildVAlign switch
        {
            VAlign.Middle => (cellH - child.Height) / 2,
            VAlign.Bottom => cellH - child.Height,
            _ => 0
        };

    private class GridCell
    {
        public TuiControl Child { get; set; } = null!;
        public int Row, Col, RowSpan = 1, ColSpan = 1;
    }
}
