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
        // 工具仍在流式输出时，Detail 变化 → 节流重绘（首次渲染同步快照；之后跟随到结束）
        foreach (var tc in msg.ToolCalls)
            tc.PropertyChanged += OnToolDetailChanged;
        Build(msg);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_msg != null)
            foreach (var tc in _msg.ToolCalls)
                tc.PropertyChanged -= OnToolDetailChanged;
        _msg = null;
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
            if (_msg != null) Build(_msg); // 仍在当前页 → 跟随更新
        });
    }

    /// <summary>重建整页（幂等：先清 Body）。含总字符预算，超限省略并提示。</summary>
    private void Build(ChatMessage msg)
    {
        Body.Children.Clear();
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var main = isDark ? (Color?)Application.Current?.Resources["MainTextDark"] : Application.Current?.Resources["MainTextLight"];
        var muted = isDark ? Application.Current?.Resources["MutedTextDark"] : Application.Current?.Resources["MutedTextLight"];
        var primary = Application.Current?.Resources["Primary"];

        const int detailBudget = 150_000; // 全页渲染总字符预算，防多工具巨输出主线程 ANR
        int used = 0;
        for (int i = 0; i < msg.ToolCalls.Count; i++)
        {
            var tc = msg.ToolCalls[i];
            var card = new VerticalStackLayout { Spacing = 4 };

            card.Children.Add(new Label
            {
                Text = $"{(i + 1)}. 🔧 {tc.Name}",
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = main as Color ?? Colors.DimGray,
            });

            if (!string.IsNullOrEmpty(tc.Summary))
            {
                card.Children.Add(new Label
                {
                    Text = tc.Summary,
                    FontSize = 12,
                    LineBreakMode = LineBreakMode.WordWrap,
                    TextColor = muted as Color ?? Colors.Gray,
                });
            }

            if (!string.IsNullOrWhiteSpace(tc.Detail))
            {
                if (used >= detailBudget)
                {
                    card.Children.Add(new Label { Text = "（输出过长，已省略详情）", FontSize = 11, TextColor = muted as Color ?? Colors.Gray });
                }
                else
                {
                    // 预算内只渲染剩余可容纳部分（按码点截断，防切半代理对）
                    string detail = tc.Detail;
                    if (used + detail.Length > detailBudget)
                        detail = WayCoder.ContextManager.TruncateByRunes(detail, detailBudget - used);
                    used += detail.Length;
                    var detailLabel = new Label
                    {
                        FontSize = 12,
                        LineHeight = 1.25,
                        LineBreakMode = LineBreakMode.WordWrap,
                    };
                    detailLabel.FormattedText = ToolOutputFormatter.Render(detail, tc.FilePath, isDark);
                    card.Children.Add(detailLabel);
                }
            }

            Body.Add(card);
            if (i < msg.ToolCalls.Count - 1)
                Body.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray, Opacity = 0.4 });
        }
    }
}
