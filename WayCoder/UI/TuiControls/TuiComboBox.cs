using System.Text;
using WayCoder.Terminal;

namespace WayCoder.UI.TuiControls;

/// <summary>
/// 组合框 —— 点击展开下拉列表选择。
/// 收起时显示当前选中项 + ▼ 箭头，展开时弹出选项列表。
/// 键盘：Enter/↓ 展开，↑↓ 在列表内导航，Enter 确认，Esc 收起。
/// </summary>
public class TuiComboBox : TuiControl
{
    /// <summary>选项列表</summary>
    public List<string> Options { get; set; } = [];

    /// <summary>当前选中索引（-1 = 未选择）</summary>
    public int SelectedIndex { get; set; } = -1;

    /// <summary>下拉列表是否展开</summary>
    public bool IsExpanded { get; set; }

    /// <summary>未选择时的占位文本</summary>
    public string Placeholder { get; set; } = "请选择...";

    /// <summary>箭头颜色</summary>
    public int ArrowColor { get; set; } = 36;

    /// <summary>选择变化回调</summary>
    public Action<int>? OnSelectionChanged { get; set; }

    /// <summary>展开状态变化回调</summary>
    public Action<bool>? OnExpandedChanged { get; set; }

    // ── 搜索过滤 ──
    private string _searchText = "";
    private List<int> _filteredIndices = []; // 符合搜索的选项索引列表

    /// <summary>是否启用搜索过滤</summary>
    public bool EnableSearch { get; set; } = true;

    public TuiComboBox()
    {
        Width = 24;
        Height = 1;
        Focused = true;
    }

    public TuiComboBox(List<string> options, int defaultIdx = -1)
    {
        Options = options;
        SelectedIndex = defaultIdx >= 0 && defaultIdx < options.Count ? defaultIdx : -1;
        Width = Math.Max(24, options.Count > 0 ? options.Max(o => TuiHelper.DisplayWidth(o)) + 6 : 24);
        Height = 1; // 收起时只占一行
        Focused = true;
    }

    public int ExpandedHeight => IsExpanded ? 1 + Math.Min(Options.Count, 10) : 1;

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        bool isFocused = Focused && IsEnabled;

        // 禁用时用灰色，聚焦时用 FocusedBg
        int rowBg = !IsEnabled ? (DisabledBg > 0 ? DisabledBg : 0)
                  : isFocused ? (FocusedBg > 0 ? FocusedBg : TuiTheme.Current.WindowBg)
                  : (Bg > 0 ? Bg : 0);

        // ── 主行：选中项 + ▼ ──
        string display = SelectedIndex >= 0 && SelectedIndex < Options.Count
            ? Options[SelectedIndex]
            : Placeholder;
        int dfg = !IsEnabled ? (DisabledFg > 0 ? DisabledFg : TuiTheme.Current.ControlDisabledFg)
                : SelectedIndex >= 0 ? (FocusedFg > 0 && isFocused ? FocusedFg : TuiTheme.Current.ControlFg)
                : TuiTheme.Current.ControlDisabledFg;

        // 背景
        if (rowBg > 0)
        {
            var rbBg = new RenderBuffer();
            rbBg.Write(absY, absX, new string(' ', Width), bg: rowBg);
            sb.Append(rbBg.ToString());
        }

        // 文本 + 箭头（根据 TextAlign 对齐）
        int maxTextW = Width - 4; // 预留 " ▼" 位置
        var text = TuiHelper.DisplayWidth(display) > maxTextW
            ? TuiHelper.TruncateByWidth(display, maxTextW)
            : display;
        int textVw = TuiHelper.DisplayWidth(text);
        int textX = TextAlign switch
        {
            HAlign.Center => absX + 1 + Math.Max(0, (maxTextW - textVw) / 2),
            HAlign.Right  => absX + 1 + Math.Max(0, maxTextW - textVw),
            _ => absX + 1
        };
        WriteAt(sb, absY, textX, text, dfg, rowBg);

        int arrFg = !IsEnabled ? (DisabledFg > 0 ? DisabledFg : TuiTheme.Current.ControlDisabledFg) : ArrowColor;
        string arrow = IsExpanded ? "▲" : "▼";
        WriteAt(sb, absY, absX + Width - 3, $" {arrow}", arrFg, rowBg);

        // ── 下拉列表 ──
        if (IsExpanded && Options.Count > 0)
        {
            // 确定要显示的选项（有过滤时显示过滤结果）
            var displayIndices = _filteredIndices.Count > 0
                ? _filteredIndices
                : Enumerable.Range(0, Options.Count).ToList();

            int listH = Math.Min(displayIndices.Count, 10);
            for (int i = 0; i < listH; i++)
            {
                int row = absY + 1 + i;
                if (row < ClipTop || row >= ClipBottom) continue;

                int optIdx = displayIndices[i];
                bool sel = optIdx == SelectedIndex;
                int lFg = !IsEnabled ? (DisabledFg > 0 ? DisabledFg : TuiTheme.Current.ControlDisabledFg)
                        : sel ? TuiTheme.Current.ListSelFg : TuiTheme.Current.ListFg;
                int lBg = !IsEnabled ? (DisabledBg > 0 ? DisabledBg : 0)
                        : sel ? TuiTheme.Current.ListSelBg : TuiTheme.Current.WindowBg;

                var optText = TuiHelper.DisplayWidth(Options[optIdx]) > Width - 3
                    ? TuiHelper.TruncateByWidth(Options[optIdx], Width - 3)
                    : Options[optIdx];
                var pad = Width - 3 - TuiHelper.DisplayWidth(optText);

                var rb = new RenderBuffer();
                rb.Write(row, absX + 1, $" {optText}{new string(' ', Math.Max(0, pad))}", fg: lFg, bg: lBg);
                rb.Write(row, absX + Width - 1, " ", bg: lBg);
                sb.Append(rb.ToString());
            }

            // 搜索文本提示（下拉底部）
            if (!string.IsNullOrEmpty(_searchText))
            {
                int hintRow = absY + 1 + listH;
                if (hintRow < ClipBottom)
                {
                    string hint = $" 搜索: {_searchText} ({displayIndices.Count} 项)";
                    if (TuiHelper.DisplayWidth(hint) > Width - 2)
                        hint = TuiHelper.TruncateByWidth(hint, Width - 2);
                    WriteAt(sb, hintRow, absX + 1, hint, TuiTheme.Current.ChatSystemFg, TuiTheme.Current.WindowBg);
                }
            }
        }
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (!IsEnabled || Options.Count == 0) return false;

        if (IsExpanded)
        {
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    NavigateFiltered(-1);
                    return true;
                case ConsoleKey.DownArrow:
                    NavigateFiltered(1);
                    return true;
                case ConsoleKey.Home:
                    if (_filteredIndices.Count > 0) { SelectedIndex = _filteredIndices[0]; OnSelectionChanged?.Invoke(SelectedIndex); }
                    else { SelectedIndex = 0; OnSelectionChanged?.Invoke(SelectedIndex); }
                    return true;
                case ConsoleKey.End:
                    if (_filteredIndices.Count > 0) { SelectedIndex = _filteredIndices[^1]; OnSelectionChanged?.Invoke(SelectedIndex); }
                    else { SelectedIndex = Options.Count - 1; OnSelectionChanged?.Invoke(SelectedIndex); }
                    return true;
                case ConsoleKey.Enter:
                    CloseDropdown();
                    return true;
                case ConsoleKey.Escape:
                    _searchText = "";
                    _filteredIndices = [];
                    CloseDropdown();
                    return true;
                case ConsoleKey.Backspace:
                    if (_searchText.Length > 0)
                    {
                        _searchText = _searchText[..^1];
                        RebuildFilter();
                    }
                    return true;
                default:
                    // 可打印字符 → 增量搜索
                    if (EnableSearch && key.KeyChar >= ' ')
                    {
                        _searchText += key.KeyChar;
                        RebuildFilter();
                        return true;
                    }
                    return false;
            }
        }
        else
        {
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                case ConsoleKey.Spacebar:
                    IsExpanded = true;
                    _searchText = "";
                    _filteredIndices = [];
                    if (SelectedIndex < 0) SelectedIndex = 0;
                    OnExpandedChanged?.Invoke(true);
                    return true;
                case ConsoleKey.UpArrow:
                    SelectedIndex = Math.Max(0, SelectedIndex - 1);
                    OnSelectionChanged?.Invoke(SelectedIndex);
                    return true;
                case ConsoleKey.DownArrow:
                    SelectedIndex = Math.Min(Options.Count - 1, SelectedIndex + 1);
                    OnSelectionChanged?.Invoke(SelectedIndex);
                    return true;
            }
        }
        return false;
    }

    public override void OnResize(int newParentW, int newParentH) { }

    /// <summary>在过滤列表中导航</summary>
    private void NavigateFiltered(int delta)
    {
        var list = _filteredIndices.Count > 0 ? _filteredIndices : Enumerable.Range(0, Options.Count).ToList();
        if (list.Count == 0) return;
        int curIdx = list.IndexOf(SelectedIndex);
        if (curIdx < 0) curIdx = delta > 0 ? -1 : list.Count;
        int newIdx = (curIdx + delta + list.Count) % list.Count;
        SelectedIndex = list[newIdx];
        OnSelectionChanged?.Invoke(SelectedIndex);
    }

    /// <summary>根据当前搜索文本重建过滤列表</summary>
    private void RebuildFilter()
    {
        if (string.IsNullOrEmpty(_searchText))
        {
            _filteredIndices = [];
            return;
        }
        _filteredIndices = Options
            .Select((o, i) => (text: o, index: i))
            .Where(x => x.text.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.index)
            .ToList();
        // 选中第一个匹配项
        if (_filteredIndices.Count > 0 && !_filteredIndices.Contains(SelectedIndex))
        {
            SelectedIndex = _filteredIndices[0];
            OnSelectionChanged?.Invoke(SelectedIndex);
        }
    }

    /// <summary>关闭下拉列表并清理搜索状态</summary>
    private void CloseDropdown()
    {
        IsExpanded = false;
        _searchText = "";
        _filteredIndices = [];
        OnExpandedChanged?.Invoke(false);
    }

    /// <summary>设置为指定索引</summary>
    public void Select(int index)
    {
        if (index >= 0 && index < Options.Count)
        {
            SelectedIndex = index;
            OnSelectionChanged?.Invoke(index);
        }
    }
}
