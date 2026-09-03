using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.TUI;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

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
public class TuiTreeView : TuiListControl
{
    /// <summary>根节点列表</summary>
    public List<TuiTreeNode> RootNodes { get; set; } = [];

    /// <summary>
    /// 当前选中节点 —— 由基类 <see cref="TuiListControl.SelectedIndex"/> 派生（真源是可见扁平列表索引）。
    /// get：索引越界/空表 → null（= 无选中，与旧 SelectedNode==null 语义等价）；
    /// set：null → 清空选中；节点 → 重建扁平列表定位其索引（-1 = 折叠不可见，视为暂空选中）。
    /// </summary>
    public TuiTreeNode? SelectedNode
    {
        get => SelectedIndex >= 0 && SelectedIndex < _flatList.Count ? _flatList[SelectedIndex] : null;
        set
        {
            if (value == null)
            {
                SelectedIndex = -1;
                return;
            }

            // 定位节点在当前可见扁平列表中的索引（数据此刻即真源，重建保证新鲜）
            BuildFlatList();
            SelectedIndex = _flatList.IndexOf(value);
        }
    }

    /// <summary>缩进宽度（每级）</summary>
    public int IndentWidth { get; set; } = 2;

    /// <summary>自定义单元格模板（.tui 片段，{text}/{icon}/{depth} 占位符），空则用默认图标+文本。</summary>
    public string CellMarkup { get; set; } = "";

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

    // SelectedIndex / ScrollOffset 继承自 TuiListControl（选中索引真源 + 滚动偏移）；
    // SelectedNode 由 SelectedIndex 派生（见上）。初始无选中 = -1。

    public TuiTreeView()
    {
        Width = 40;
        Height = 10;
        Focused = true;
        SelectedIndex = -1; // 无选中（与 SelectedNode==null 语义一致）
        SelFg = TuiTheme.Current.ListSelFg;
        SelBg = TuiTheme.Current.TreeViewSelBg;
        LineColor = TuiTheme.Current.ControlDisabledFg;
    }

    // ── 基类骨架 ──

    /// <summary>可见扁平节点总数（OnRender/OnKey/OnMouse 前均已 BuildFlatList）。</summary>
    protected override int ItemCount => _flatList.Count;

    /// <summary>选中移动到新行：MarkDirty + 触发节点级 OnSelectionChanged。</summary>
    protected override void OnSelectionMoved(int index)
    {
        MarkDirty();
        if (index >= 0 && index < _flatList.Count)
            OnSelectionChanged?.Invoke(_flatList[index]);
    }

    // ── 渲染 ──

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        // 构建展开节点列表
        BuildFlatList();

        // 空树也要往下走：底色面板照铺（提前 return 会留下一块没画的洞）

        // 确保选中节点可见（无选中 / 选中节点折叠不可见时不动）
        if (SelectedIndex >= 0)
            EnsureSelectedVisible();

        int visH = Height;

        // 先把整块控件区铺成底色 —— 节点数不满一屏时，剩下的空行也得是黑底，
        // 否则底部会露出对话框的灰底，看着像树只画了半截
        int panelBg = !IsEnabled ? (DisabledBg > 0 ? DisabledBg : TuiTheme.Current.TreeViewBg)
                                 : (Bg > 0 ? Bg : TuiTheme.Current.TreeViewBg);
        if (panelBg != 0)
        {
            var rbPanel = new RenderBuffer();
            for (int i = 0; i < visH; i++)
            {
                int r = absY + i;
                if (r < ClipTop || r >= ClipBottom) continue;
                rbPanel.Write(r, absX, new string(' ', Width), bg: panelBg);
            }
            sb.Append(rbPanel.ToString());
        }

        for (int i = 0; i < visH; i++)
        {
            int flatIdx = ScrollOffset + i;
            if (flatIdx >= _flatList.Count) break;

            int row = absY + i;
            if (row < ClipTop || row >= ClipBottom) continue;

            var node = _flatList[flatIdx];
            bool sel = flatIdx == SelectedIndex;
            int depth = _depthCache.GetValueOrDefault(node, 0);

            int fg = !IsEnabled ? (DisabledFg > 0 ? DisabledFg : TuiTheme.Current.ControlDisabledFg)
                   : sel ? SelFg : (Fg > 0 ? Fg : TuiTheme.Current.TreeViewFg);
            int bg = !IsEnabled ? (DisabledBg > 0 ? DisabledBg : TuiTheme.Current.TreeViewBg)
                   : sel ? SelBg : (Bg > 0 ? Bg : TuiTheme.Current.TreeViewBg);

            // 行背景：整行铺满。以前只在选中行铺，未选中行是透明的，
            // 放进灰底对话框里就漏出灰底 —— 树/列表/表格要一律黑底
            if (bg != 0)
            {
                var rbBg = new RenderBuffer();
                rbBg.Write(row, absX, new string(' ', Width), bg: bg);
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

            // 图标 + 文本（图标已含在 Text 中时不重复添加）；有 CellMarkup 时用自定义单元格渲染
            if (!string.IsNullOrEmpty(CellMarkup))
            {
                WriteAt(sb, row, absX, lineBuilder.ToString(), fg, bg); // 先写缩进 + 树线 + 展开符
                int indentW = AnsiHelper.DisplayWidth(lineBuilder.ToString());
                try
                {
                    var cell = TuiMarkup.LoadCell(CellMarkup, new Dictionary<string, string>
                    {
                        ["text"] = node.Text,
                        ["icon"] = node.Icon,
                        ["depth"] = depth.ToString(),
                    });
                    cell.Width = Math.Max(1, Width - indentW);
                    cell.Height = 1;
                    cell.OnResize(cell.Width, 1);
                    cell.Render(sb, absX + indentW, row, ClipLeft, ClipTop, ClipRight, ClipBottom);
                }
                catch { WriteAt(sb, row, absX + indentW, node.Text, fg, bg); }
            }
            else
            {
                var nodeText = string.IsNullOrEmpty(node.Icon) || node.Text.StartsWith(node.Icon)
                    ? node.Text
                    : $"{node.Icon} {node.Text}";
                lineBuilder.Append(nodeText);
                WriteAt(sb, row, absX, lineBuilder.ToString(), fg, bg);
            }
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

    public override bool OnMouse(InputEvent ev)
    {
        if (!MouseInBounds(ev, out int relX, out int relY)) return false;

        // 滚轮：垂直滚动（扁平列表索引单位，每滚 3 行）
        if (ev.MouseScrollUp)
        {
            BuildFlatList();
            ScrollOffset = Math.Max(0, ScrollOffset - 3);
            MarkDirty();
            return true;
        }
        if (ev.MouseScrollDown)
        {
            BuildFlatList();
            ScrollOffset = Math.Min(Math.Max(0, _flatList.Count - Height), ScrollOffset + 3);
            MarkDirty();
            return true;
        }
        if (!ev.MouseLeft) return false;

        // 点击行 → 扁平列表索引（BuildFlatList 保证一行一节点，见 OnRender）
        BuildFlatList();
        int flatIdx = ScrollOffset + relY;
        if (flatIdx < 0 || flatIdx >= _flatList.Count) return false;

        var node = _flatList[flatIdx];
        Focused = true;

        // 展开指示符列（非叶节点的 ▼/▶ 占 2 列）→ 切换展开；否则选中
        int depth = _depthCache.GetValueOrDefault(node, 0);
        int markerStart = depth > 0 ? (depth - 1) * 2 + 2 : 0;
        if (!node.IsLeaf && relX >= markerStart && relX < markerStart + 2)
            ToggleExpand(node);
        else
            SelectNode(node);

        return true;
    }

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
                MoveHome();
                return true;
            case ConsoleKey.End:
                MoveEnd();
                return true;
            case ConsoleKey.PageUp:
                MovePage(-1);
                return true;
            case ConsoleKey.PageDown:
                MovePage(1);
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
    // 树每行均可选（无组头/分隔线），移动/页跳/首尾跳全部由 TuiListControl 基类骨架完成
    // （MoveTo 钳制 + IsSelectable 恒 true + OnSelectionMoved 触发回调）。此处只补扁平列表重建
    // 与 public 可见性。

    /// <summary>上移选中节点（public 包装：先重建扁平列表保证索引新鲜，再走基类导航）。</summary>
    public new bool MoveUp()
    {
        BuildFlatList();
        return base.MoveUp();
    }

    /// <summary>下移选中节点（public 包装：先重建扁平列表保证索引新鲜，再走基类导航）。</summary>
    public new bool MoveDown()
    {
        BuildFlatList();
        return base.MoveDown();
    }

    public void ExpandNode(TuiTreeNode node)
    {
        if (!node.IsLeaf)
        {
            node.IsExpanded = true;
            SelectedNode = node; // SelectedNode setter 重建扁平列表（子节点现在可见）+ 定位索引
            MarkDirty();
        }
    }

    public void CollapseNode(TuiTreeNode node)
    {
        node.IsExpanded = false;
        SelectedNode = node; // setter 重建扁平列表（子节点已隐藏）+ 定位索引
        MarkDirty();
    }

    public void ToggleExpand(TuiTreeNode node)
    {
        if (node.IsExpanded) CollapseNode(node);
        else ExpandNode(node);
    }

    /// <summary>
    /// 选中节点。必须标脏 —— 渲染是增量的，只重画标脏的叶子控件；
    /// 光改 SelectedNode 不标脏，画面停在旧帧上，用户看到的就是「按键没反应」。
    /// </summary>
    public void SelectNode(TuiTreeNode node)
    {
        SelectedNode = node;
        MarkDirty();
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
        MarkDirty();
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
