using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 标签页控件 —— 水平标签栏，可切换内容面板。
///
/// 用法：
///   var tabs = new TuiTabs();
///   tabs.AddTab("聊天", chatPanel);
///   tabs.AddTab("文件", filePanel);
///   tabs.SelectTab(0);
///
///   当前激活的内容面板通过 ActiveContent 获取。
/// </summary>
public class TuiTabs : TuiControl
{
    /// <summary>标签定义列表</summary>
    private readonly List<string> _tabLabels = [];

    private readonly List<TuiControl> _tabContents = [];

    /// <summary>当前选中标签索引</summary>
    public int SelectedIndex { get; set; }

    /// <summary>标签数</summary>
    public int Count => _tabLabels.Count;

    /// <summary>标签栏高度（固定 1 行）</summary>
    public int TabBarHeight => 1;

    /// <summary>标签栏背景色</summary>
    public int TabBarBg { get; set; }

    /// <summary>标签栏前景色</summary>
    public int TabBarFg { get; set; }

    /// <summary>选中标签背景色</summary>
    public int ActiveTabBg { get; set; }

    /// <summary>选中标签前景色</summary>
    public int ActiveTabFg { get; set; }

    /// <summary>非选中标签前景色</summary>
    public int InactiveTabFg { get; set; }

    /// <summary>标签最小宽度</summary>
    public int MinTabWidth { get; set; } = 6;

    /// <summary>选择变化回调</summary>
    public Action<int>? OnSelectionChanged { get; set; }

    public TuiTabs()
    {
        Height = 1;
        Width = 40;
        TabBarBg = TuiTheme.Current.TabsBarBg;
        TabBarFg = TuiTheme.Current.TabsBarFg;
        ActiveTabFg = TuiTheme.Current.TabsActiveFg;
        ActiveTabBg = TuiTheme.Current.TabsActiveBg;
        InactiveTabFg = TuiTheme.Current.TabsInactiveFg;
    }

    /// <summary>当前激活的内容面板</summary>
    public TuiControl? ActiveContent =>
        SelectedIndex >= 0 && SelectedIndex < _tabContents.Count
            ? _tabContents[SelectedIndex]
            : null;

    /// <summary>添加标签页</summary>
    public void AddTab(string label, TuiControl content)
    {
        _tabLabels.Add(label);
        _tabContents.Add(content);
        content.Parent = this;
        content.Visible = _tabContents.Count == 1; // 首选项卡默认可见
        if (_tabContents.Count == 1) SelectedIndex = 0;
    }

    /// <summary>移除标签页</summary>
    public void RemoveTab(int index)
    {
        if (index < 0 || index >= _tabLabels.Count) return;
        _tabLabels.RemoveAt(index);
        _tabContents.RemoveAt(index);
        if (SelectedIndex >= _tabLabels.Count)
            SelectedIndex = Math.Max(0, _tabLabels.Count - 1);
    }

    /// <summary>选择标签页</summary>
    public void SelectTab(int index)
    {
        if (index < 0 || index >= _tabLabels.Count) return;
        // 隐藏旧内容
        if (SelectedIndex >= 0 && SelectedIndex < _tabContents.Count)
            _tabContents[SelectedIndex].Visible = false;
        // 显示新内容
        SelectedIndex = index;
        _tabContents[SelectedIndex].Visible = true;
        OnSelectionChanged?.Invoke(index);
    }

    /// <summary>选择下一个标签</summary>
    public void SelectNext()
    {
        if (_tabLabels.Count == 0) return;
        SelectTab((SelectedIndex + 1) % _tabLabels.Count);
    }

    /// <summary>选择上一个标签</summary>
    public void SelectPrev()
    {
        if (_tabLabels.Count == 0) return;
        SelectTab((SelectedIndex - 1 + _tabLabels.Count) % _tabLabels.Count);
    }

    // ── 渲染 ──

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        if (_tabLabels.Count == 0) return;

        int totalW = Width;
        int tabW = Math.Max(MinTabWidth, totalW / _tabLabels.Count);

        for (int i = 0; i < _tabLabels.Count; i++)
        {
            int x = absX + i * tabW;
            var label = _tabLabels[i];
            bool active = i == SelectedIndex;

            // 裁剪标签文本并居中
            int maxTextW = tabW - 2;
            if (AnsiHelper.DisplayWidth(label) > maxTextW)
                label = AnsiHelper.TruncateByWidth(label, maxTextW);
            int labelVw = AnsiHelper.DisplayWidth(label);
            int leftPad = Math.Max(0, (tabW - labelVw) / 2);
            int rightPad = Math.Max(0, tabW - leftPad - labelVw);
            var display = new string(' ', leftPad) + label + new string(' ', rightPad);

            int fg = !IsEnabled ? (DisabledFg > 0 ? DisabledFg : TuiTheme.Current.ControlDisabledFg)
                : active ? ActiveTabFg : InactiveTabFg;
            int bg = !IsEnabled ? (DisabledBg > 0 ? DisabledBg : TabBarBg)
                : active ? (ActiveTabBg > 0 ? ActiveTabBg : (Bg > 0 ? Bg : 0))
                : TabBarBg;

            WriteAt(sb, absY, x, display, fg, bg);
        }

        // 填充剩余位置
        int usedW = _tabLabels.Count * tabW;
        if (usedW < totalW)
            WriteAt(sb, absY, absX + usedW, new string(' ', totalW - usedW), bg: TabBarBg);
    }

    // ── 输入 ──

    public override bool OnMouse(InputEvent ev)
    {
        // 每个 tab 占 [absX+i*tabW, +tabW)（见 OnRender）。点击标签行 = 切换 + 聚焦。
        if (!MouseInBounds(ev, out int relX, out int relY)) return false;
        if (!ev.MouseLeft || relY != 0 || _tabLabels.Count == 0) return false;

        int tabW = Math.Max(MinTabWidth, Width / _tabLabels.Count);
        int idx = Math.Clamp(relX / tabW, 0, _tabLabels.Count - 1);

        Focused = true;
        SelectTab(idx);
        MarkDirty();
        return true;
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (!IsEnabled || !Focused) return false;

        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:
                SelectPrev();
                return true;
            case ConsoleKey.RightArrow:
                SelectNext();
                return true;
            default:
                // 数字键 1-9 快速切换
                if (key.KeyChar >= '1' && key.KeyChar <= '9')
                {
                    int idx = key.KeyChar - '1';
                    if (idx < _tabLabels.Count)
                        SelectTab(idx);
                    return true;
                }

                return false;
        }
    }
}