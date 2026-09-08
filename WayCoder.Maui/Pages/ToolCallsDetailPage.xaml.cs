using System.Collections.Specialized;
using System.ComponentModel;
using WayCoder.Maui.Markup;
using WayCoder.Maui.Models;

namespace WayCoder.Maui.Pages;

/// <summary>
/// 工具调用详情页（只读）——「工具调用:N 次」行的目标子页。
/// 聊天流默认只显示一行计数，点入口经 <see cref="Target"/> 传工具组消息，
/// 此页逐个工具分区展示：名称 + 参数摘要 + 输出详情（语法高亮富文本）。
/// 流式中的工具：Detail 变更会节流重绘本页（打开时快照 → 实时跟随）；
/// 渲染设总字符预算，防多工具各近上限时主线程一次性大构建导致 ANR。
/// </summary>
public partial class ToolCallsDetailPage : ContentPage
{
    /// <summary>待展示的工具组消息（ChatPage 点击入口时赋值；页 OnAppearing 读取渲染）。</summary>
    public static ChatMessage? Target { get; set; }

    private ChatMessage? _msg;
    private bool _pendingRerender;   // 节流：Detail 流式变更期间最多每 ~500ms 重绘一次
    private readonly object _lock = new();
    private bool _isDark;
    private Color? _main;
    private Color? _muted;
    private readonly List<ToolCardView> _cards = new(); // index 对齐 _msg.ToolCalls

    /// <summary>全页渲染总字符预算（按码点/rune 计，与 TruncateByRunes 单位一致，防多工具巨输出主线程 ANR）。</summary>
    private const int DetailBudget = 150_000;

    /// <summary>单个工具卡的渲染状态（供流式增量更新只重绘该工具，不整页重建）。</summary>
    private sealed class ToolCardView
    {
        public required VerticalStackLayout Card;
        public Label? DetailLabel;
        public int RenderedRunes;    // 该工具当前实际渲染的 rune（计入共享总量，预算=总量-其它工具已渲染）
        public string? LastRendered; // 上次渲染的（截断后）正文；文本未变则跳过重绘（finding #I）
    }

    private static int CountRunes(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        int n = 0;
        foreach (var _ in s.EnumerateRunes()) n++;
        return n;
    }

    public ToolCallsDetailPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var msg = Target;
        Target = null; // 一次性消费，防返回后再进残留旧内容
        if (msg == null || msg.ToolCalls.Count == 0)
        {
            _msg = null;
            Body.Add(new Label { Text = "（无工具调用记录）", FontSize = 13 });
            return;
        }
        _msg = msg;
        // 流式中工具仍可能追加：订阅集合变更，新工具加入 → 追加卡片 + 订阅（finding #F）
        _msg.ToolCalls.CollectionChanged += OnToolCallsChanged;
        foreach (var tc in msg.ToolCalls)
            tc.PropertyChanged += OnToolDetailChanged;
        Build(msg);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_msg != null)
        {
            _msg.ToolCalls.CollectionChanged -= OnToolCallsChanged;
            foreach (var tc in _msg.ToolCalls)
                tc.PropertyChanged -= OnToolDetailChanged;
        }
        _msg = null;
    }

    /// <summary>工具组会话期间仍追加工具 → 追加卡片并订阅其 PropertyChanged（否则新工具永不渲染）。</summary>
    private void OnToolCallsChanged(object? s, NotifyCollectionChangedEventArgs e)
    {
        if (_msg == null || e.Action != NotifyCollectionChangedAction.Add || e.NewItems == null) return;
        foreach (ToolCallItem tc in e.NewItems)
        {
            tc.PropertyChanged += OnToolDetailChanged;
            var index = _cards.Count; // BuildCard 已把视图加入 _cards（序号 i+1）
            Body.Add(BuildCard(tc, index + 1));
            var view = _cards[index];
            RenderOrOmit(view, tc, DetailBudget - TotalRenderedExcept(index));
        }
    }

    private void OnToolDetailChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ToolCallItem.Detail)) return;
        lock (_lock)
        {
            if (_pendingRerender) return;
            _pendingRerender = true;
        }
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), () =>
        {
            lock (_lock) _pendingRerender = false;
            if (_msg == null || sender is not ToolCallItem tc) return;
            var idx = _msg.ToolCalls.IndexOf(tc);
            if (idx >= 0) UpdateTool(idx); // 只重绘该工具，不整页重载（防主线程反复大构建 ANR）
        });
    }

    /// <summary>重建整页（幂等：先清 Body）。含总字符预算，超限省略并提示。</summary>
    private void Build(ChatMessage msg)
    {
        Body.Children.Clear();
        _cards.Clear();
        _isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        _main = _isDark ? (Color?)Application.Current?.Resources["MainTextDark"] : (Color?)Application.Current?.Resources["MainTextLight"];
        _muted = _isDark ? (Color?)Application.Current?.Resources["MutedTextDark"] : (Color?)Application.Current?.Resources["MutedTextLight"];

        int used = 0;
        for (int i = 0; i < msg.ToolCalls.Count; i++)
        {
            var tc = msg.ToolCalls[i];
            Body.Add(BuildCard(tc, i + 1)); // 每张卡自带尾部分隔线（后续追加时自动承接分隔）
            var view = _cards[i];
            RenderOrOmit(view, tc, DetailBudget - used);
            used += view.RenderedRunes;
        }
    }

    /// <summary>创建「序号 + 名称 + 摘要」卡（正文细节由 RenderOrOmit/UpdateTool 惰性渲染；自带宽高 1 的尾部分隔线）。</summary>
    private VerticalStackLayout BuildCard(ToolCallItem tc, int number)
    {
        var card = new VerticalStackLayout { Spacing = 4 };
        card.Children.Add(new Label
        {
            Text = $"{number}. 🔧 {tc.Name}",
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = _main as Color ?? Colors.DimGray,
        });
        if (!string.IsNullOrEmpty(tc.Summary))
        {
            card.Children.Add(new Label
            {
                Text = tc.Summary,
                FontSize = 12,
                LineBreakMode = LineBreakMode.WordWrap,
                TextColor = _muted as Color ?? Colors.Gray,
            });
        }
        card.Children.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray, Opacity = 0.4 }); // 尾部分隔线
        _cards.Add(new ToolCardView { Card = card }); // 占位（DetailLabel/RenderedRunes 稍后填充），与 _msg.ToolCalls 对齐
        return card;
    }

    /// <summary>在共享预算内渲染/省略某工具的正文（补齐空/超预算卡的具体内容）。</summary>
    private void RenderOrOmit(ToolCardView view, ToolCallItem tc, int allowance)
    {
        if (string.IsNullOrWhiteSpace(tc.Detail)) return; // 无正文：等流式填充（UpdateTool 懒建）
        if (allowance <= 0)
        {
            // 预算已耗尽：显示「已省略」占位（此前为空卡则补上，finding #G）
            if (view.DetailLabel == null)
            {
                var lbl = new Label { Text = "（输出过长，已省略详情）", FontSize = 11, TextColor = _muted as Color ?? Colors.Gray };
                view.Card.Children.Insert(view.Card.Children.Count - 1, lbl); // 插到尾部分隔线之前
                view.DetailLabel = lbl;
                view.LastRendered = null;
            }
            return;
        }
        string detail = tc.Detail;
        if (CountRunes(detail) > allowance)
            detail = WayCoder.ContextManager.TruncateByRunes(detail, allowance);
        RenderDetail(view, tc, detail);
    }

    /// <summary>把（截断后的）正文渲染到工具卡正文标签；文本未变则跳过，避免每 500ms 重渲染同串（finding #I）。</summary>
    private void RenderDetail(ToolCardView view, ToolCallItem tc, string detail)
    {
        if (view.LastRendered == detail) return; // 流式只追加，截断前缀稳定 → 跳过重渲染
        if (view.DetailLabel == null)
        {
            var lbl = new Label
            {
                FontSize = 12,
                LineHeight = 1.25,
                LineBreakMode = LineBreakMode.WordWrap,
            };
            view.Card.Children.Insert(view.Card.Children.Count - 1, lbl); // 插到尾部分隔线之前
            view.DetailLabel = lbl;
        }
        view.DetailLabel.FormattedText = ToolOutputFormatter.Render(detail, tc.FilePath, _isDark);
        view.RenderedRunes = CountRunes(detail);
        view.LastRendered = detail;
    }

    /// <summary>除某工具外，其余工具当前已实际渲染的 rune 总量（共享预算口径，保证总量不超 DetailBudget）。</summary>
    private int TotalRenderedExcept(int index)
    {
        int total = 0;
        for (int j = 0; j < _cards.Count; j++)
            if (j != index) total += _cards[j].RenderedRunes;
        return total;
    }

    /// <summary>增量重绘指定工具：流式 Detail 变更只重建该工具，不整页重载——避免主线程反复大构建 ANR（code-review finding #5）。</summary>
    private void UpdateTool(int index)
    {
        if (_msg == null || index < 0 || index >= _msg.ToolCalls.Count || index >= _cards.Count) return;
        var tc = _msg.ToolCalls[index];
        var view = _cards[index];
        if (string.IsNullOrWhiteSpace(tc.Detail)) return; // 无正文不动

        // 共享预算：其它工具已渲染的总 rune（随各自增长动态收缩本工具的允许额，总量保持 ≤ DetailBudget）
        RenderOrOmit(view, tc, DetailBudget - TotalRenderedExcept(index));
    }
}
