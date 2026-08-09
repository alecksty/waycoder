using System.Text;

namespace CoreCoderSharp.UI.Controls;

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
    public int TabBarBg { get; set; } = 44;

    /// <summary>标签栏前景色</summary>
    public int TabBarFg { get; set; } = 37;

    /// <summary>选中标签背景色</summary>
    public int ActiveTabBg { get; set; }

    /// <summary>选中标签前景色</summary>
    public int ActiveTabFg { get; set; } = 30;

    /// <summary>非选中标签前景色</summary>
    public int InactiveTabFg { get; set; } = 90;

    /// <summary>标签最小宽度</summary>
    public int MinTabWidth { get; set; } = 6;

    /// <summary>选择变化回调</summary>
    public Action<int>? OnSelectionChanged { get; set; }

    public TuiTabs()
    {
        Height = 1;
        Width = 40;
    }

    /// <summary>当前激活的内容面板</summary>
    public TuiControl? ActiveContent =>
        SelectedIndex >= 0 && SelectedIndex < _tabContents.Count
            ? _tabContents[SelectedIndex] : null;

    /// <summary>添加标签页</summary>
    public void AddTab(string label, TuiControl content)
    {
        _tabLabels.Add(label);
        _tabContents.Add(content);
        content.Parent = Parent;
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

            // 裁剪标签文本
            int maxTextW = tabW - 1;
            if (TuiHelper.DisplayWidth(label) > maxTextW)
                label = TuiHelper.TruncateByWidth(label, maxTextW);
            var pad = Math.Max(0, tabW - TuiHelper.DisplayWidth(label));
            var display = label + new string(' ', pad);

            int fg = active ? ActiveTabFg : InactiveTabFg;
            int bg = active ? ActiveTabBg : TabBarBg;
            if (active && ActiveTabBg == 0) bg = Bg > 0 ? Bg : 0;

            WriteAt(sb, absY, x, display, fg, bg);
        }

        // 填充剩余位置
        int usedW = _tabLabels.Count * tabW;
        if (usedW < totalW)
            WriteAt(sb, absY, absX + usedW, new string(' ', totalW - usedW), bg: TabBarBg);
    }

    // ── 输入 ──

    public override bool HandleKey(ConsoleKeyInfo key)
    {
        if (!Focused) return false;

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
