using System.Text;
using WayCoder.Terminal;

namespace WayCoder.UI.TuiControls;

// ================================================================
// 树节点
// ================================================================

/// <summary>树节点</summary>
public class TuiTreeNode
{
    /// <summary>节点文本</summary>
    public string Text { get; set; } = "";

    /// <summary>子节点列表</summary>
    public List<TuiTreeNode> Children { get; set; } = [];

    /// <summary>是否展开（显示子节点）</summary>
    public bool IsExpanded { get; set; }

    /// <summary>附带的用户数据</summary>
    public object? Tag { get; set; }

    /// <summary>图标（emoji 或符号，空=无图标）</summary>
    public string Icon { get; set; } = "";

    /// <summary>父节点</summary>
    public TuiTreeNode? Parent { get; set; }

    public TuiTreeNode() { }

    public TuiTreeNode(string text, string icon = "")
    {
        Text = text;
        Icon = icon;
    }

    /// <summary>添加子节点并设置 Parent 引用</summary>
    public TuiTreeNode Add(TuiTreeNode child)
    {
        child.Parent = this;
        Children.Add(child);
        return this;
    }

    /// <summary>批量添加子节点</summary>
    public TuiTreeNode AddRange(params TuiTreeNode[] children)
    {
        foreach (var c in children) { c.Parent = this; Children.Add(c); }
        return this;
    }

    /// <summary>展开此节点及所有祖先（使此节点可见）</summary>
    public void ExpandToRoot()
    {
        var p = Parent;
        while (p != null) { p.IsExpanded = true; p = p.Parent; }
    }

    /// <summary>是否是叶子节点（无子节点）</summary>
    public bool IsLeaf => Children.Count == 0;
}

// ================================================================
// 树形视图控件
// ================================================================

/// <summary>
/// 树形视图 —— 层级树控件。
/// 展开/折叠节点，键盘全导航，树线渲染。
///
/// 键盘：
///   ↑↓   — 上下移动选中节点
///   ←    — 折叠当前节点（或跳转到父节点）
///   →    — 展开当前节点（或跳转到第一个子节点）
///   Space — 切换展开/折叠
///   Enter — 激活（触发 OnNodeActivated）
///   Home  — 跳到第一个节点
///   End   — 跳到最后一个可见节点
/// </summary>
public class TuiTreeView : TuiControl
{
    /// <summary>根节点列表</summary>
    public List<TuiTreeNode> RootNodes { get; set; } = [];

    /// <summary>当前选中节点</summary>
    public TuiTreeNode? SelectedNode { get; set; }

    /// <summary>缩进宽度（每级）</summary>
    public int IndentWidth { get; set; } = 2;

    /// <summary>选中前景色</summary>
    public int SelFg { get; set; }

    /// <summary>选中背景色</summary>
    public int SelBg { get; set; }

    /// <summary>树线颜色</summary>
    public int LineColor { get; set; }

    /// <summary>节点激活（Enter）回调</summary>
    public Action<TuiTreeNode>? OnNodeActivated { get; set; }

    /// <summary>选中变化回调</summary>
    public Action<TuiTreeNode>? OnSelectionChanged { get; set; }

    // ── 内部 ──

    /// <summary>展开节点列表缓存（按可见顺序）</summary>
    private readonly List<TuiTreeNode> _flatList = [];

    /// <summary>每个节点在展开列表中的深度</summary>
    private readonly Dictionary<TuiTreeNode, int> _depthCache = new();

    /// <summary>滚动偏移（可见列表中的索引）</summary>
    private int _scrollOffset;

    public TuiTreeView()
    {
        Width = 40;
        Height = 10;
        Focused = true;
        SelFg = TuiTheme.Current.ListSelFg;
        SelBg = TuiTheme.Current.TreeViewSelBg;
        LineColor = TuiTheme.Current.ControlDisabledFg;
    }

    // ── 渲染 ──

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        // 构建展开节点列表
        BuildFlatList();

        if (_flatList.Count == 0) return;

        // 确保选中节点可见
        if (SelectedNode != null)
        {
            int selIdx = _flatList.IndexOf(SelectedNode);
            if (selIdx >= 0)
            {
                if (selIdx < _scrollOffset)
                    _scrollOffset = selIdx;
                else if (selIdx >= _scrollOffset + Height)
                    _scrollOffset = selIdx - Height + 1;
                _scrollOffset = Math.Clamp(_scrollOffset, 0, Math.Max(0, _flatList.Count - Height));
            }
        }

        int visH = Height;
        for (int i = 0; i < visH; i++)
        {
            int flatIdx = _scrollOffset + i;
            if (flatIdx >= _flatList.Count) break;

            int row = absY + i;
            if (row < ClipTop || row >= ClipBottom) continue;

            var node = _flatList[flatIdx];
            bool sel = node == SelectedNode;
            int depth = _depthCache.GetValueOrDefault(node, 0);

            int fg = !IsEnabled ? (DisabledFg > 0 ? DisabledFg : TuiTheme.Current.ControlDisabledFg)
                   : sel ? SelFg : (Fg > 0 ? Fg : TuiTheme.Current.TreeViewFg);
            int bg = !IsEnabled ? (DisabledBg > 0 ? DisabledBg : 0)
                   : sel ? SelBg : (Bg > 0 ? Bg : 0);

            // 行背景
            if (sel)
            {
                var rbBg = new RenderBuffer();
                rbBg.Write(row, absX, new string(' ', Width), bg: SelBg);
                sb.Append(rbBg.ToString());
            }

            // 构建缩进 + 树线
            var lineBuilder = new StringBuilder();

            // 缩进级别：跳过父级（父级视觉连接由 ├─/└─ 表达）
            var ancestors = GetAncestors(node);
            for (int d = 0; d < depth - 1; d++)
            {
                if (d < ancestors.Count)
                {
                    var ancestor = ancestors[d];
                    bool isLastSibling = IsLastChild(ancestor);
                    lineBuilder.Append(isLastSibling ? "  " : "│ ");
                }
            }

            // 连接线
            if (depth > 0 && node.Parent != null)
            {
                bool last = IsLastChild(node);
                lineBuilder.Append(last ? "└─" : "├─");
            }

            // 展开指示符（三角形）
            if (!node.IsLeaf)
            {
                lineBuilder.Append(node.IsExpanded ? "▼ " : "▶ ");
            }

            // 图标 + 文本（图标已含在 Text 中时不重复添加）
            var nodeText = string.IsNullOrEmpty(node.Icon) || node.Text.StartsWith(node.Icon)
                ? node.Text
                : $"{node.Icon} {node.Text}";

            lineBuilder.Append(nodeText);

            WriteAt(sb, row, absX, lineBuilder.ToString(), fg, bg);
        }
    }

    /// <summary>递归构建展开节点列表</summary>
    private void BuildFlatList()
    {
        _flatList.Clear();
        _depthCache.Clear();
        foreach (var root in RootNodes)
            CollectVisible(root, 0);
    }

    private void CollectVisible(TuiTreeNode node, int depth)
    {
        _flatList.Add(node);
        _depthCache[node] = depth;
        if (node.IsExpanded)
        {
            foreach (var child in node.Children)
                CollectVisible(child, depth + 1);
        }
    }

    /// <summary>获取从根到节点的祖先列表</summary>
    private List<TuiTreeNode> GetAncestors(TuiTreeNode node)
    {
        var list = new List<TuiTreeNode>();
        var p = node.Parent;
        while (p != null)
        {
            list.Insert(0, p);
            p = p.Parent;
        }
        return list;
    }

    /// <summary>节点在其父节点中是否是最后一个子节点</summary>
    private static bool IsLastChild(TuiTreeNode node)
    {
        var p = node.Parent;
        return p != null && p.Children.Count > 0 && p.Children[^1] == node;
    }

    // ── 输入 ──

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (!IsEnabled) return false;
        BuildFlatList();

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                MoveUp();
                return true;
            case ConsoleKey.DownArrow:
                MoveDown();
                return true;
            case ConsoleKey.Home:
                if (_flatList.Count > 0) SelectNode(_flatList[0]);
                return true;
            case ConsoleKey.End:
                if (_flatList.Count > 0) SelectNode(_flatList[^1]);
                return true;
            case ConsoleKey.PageUp:
                PageMove(-Math.Max(1, Height));
                return true;
            case ConsoleKey.PageDown:
                PageMove(Math.Max(1, Height));
                return true;
            case ConsoleKey.RightArrow:
                if (SelectedNode != null)
                {
                    if (!SelectedNode.IsLeaf && !SelectedNode.IsExpanded)
                        ExpandNode(SelectedNode);
                    else if (!SelectedNode.IsLeaf)
                        MoveDown(); // 已展开 → 跳到第一个子节点
                }
                return true;
            case ConsoleKey.LeftArrow:
                if (SelectedNode != null)
                {
                    if (!SelectedNode.IsLeaf && SelectedNode.IsExpanded)
                        CollapseNode(SelectedNode);
                    else if (SelectedNode.Parent != null)
                        SelectNode(SelectedNode.Parent);
                }
                return true;
            case ConsoleKey.Spacebar:
                if (SelectedNode != null && !SelectedNode.IsLeaf)
                    ToggleExpand(SelectedNode);
                return true;
            case ConsoleKey.Enter:
                if (SelectedNode != null)
                    OnNodeActivated?.Invoke(SelectedNode);
                return true;
        }
        return false;
    }

    // ── 导航方法 ──

    public void MoveUp()
    {
        BuildFlatList();
        if (_flatList.Count == 0) return;
        int idx = SelectedNode != null ? _flatList.IndexOf(SelectedNode) : -1;
        int newIdx = Math.Max(0, idx - 1);
        if (newIdx < _flatList.Count)
            SelectNode(_flatList[newIdx]);
    }

    public void MoveDown()
    {
        BuildFlatList();
        if (_flatList.Count == 0) return;
        int idx = SelectedNode != null ? _flatList.IndexOf(SelectedNode) : -1;
        int newIdx = Math.Min(_flatList.Count - 1, idx + 1);
        if (newIdx >= 0)
            SelectNode(_flatList[newIdx]);
    }

    /// <summary>按页（delta 为行数，正向下翻/负向上翻）移动选中节点</summary>
    private void PageMove(int delta)
    {
        BuildFlatList();
        if (_flatList.Count == 0) return;
        int idx = SelectedNode != null ? _flatList.IndexOf(SelectedNode) : 0;
        int newIdx = Math.Clamp(idx + delta, 0, _flatList.Count - 1);
        SelectNode(_flatList[newIdx]);
    }

    public void ExpandNode(TuiTreeNode node)
    {
        if (!node.IsLeaf)
        {
            node.IsExpanded = true;
            SelectedNode = node;
            BuildFlatList(); // 子节点现在可见
        }
    }

    public void CollapseNode(TuiTreeNode node)
    {
        node.IsExpanded = false;
        SelectedNode = node;
        BuildFlatList();
    }

    public void ToggleExpand(TuiTreeNode node)
    {
        if (node.IsExpanded) CollapseNode(node);
        else ExpandNode(node);
    }

    public void SelectNode(TuiTreeNode node)
    {
        SelectedNode = node;
        OnSelectionChanged?.Invoke(node);
    }

    // ── 数据操作 ──

    /// <summary>添加根节点</summary>
    public TuiTreeNode AddRoot(string text, string icon = "")
    {
        var node = new TuiTreeNode(text, icon);
        RootNodes.Add(node);
        if (SelectedNode == null) SelectedNode = node;
        return node;
    }

    /// <summary>清空所有节点</summary>
    public void Clear()
    {
        RootNodes.Clear();
        SelectedNode = null;
        _flatList.Clear();
        _depthCache.Clear();
    }

    /// <summary>节点数（含所有层级）</summary>
    public int TotalNodeCount
    {
        get
        {
            int Count(TuiTreeNode n)
            {
                int c = 1;
                foreach (var child in n.Children) c += Count(child);
                return c;
            }
            return RootNodes.Sum(Count);
        }
    }

    public override void OnResize(int newParentW, int newParentH) { }
}
