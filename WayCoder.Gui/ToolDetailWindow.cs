using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace WayCoder.UI.Gui;

/// <summary>
/// 折叠详情窗 —— 「点开才显示详细」的 GUI 落点（对齐 MAUI 的 ToolCallsDetailPage / ReasoningDetailPage）。
///
/// 聊天流里工具调用只剩一行「🔧 工具调用:N 次」、思考只剩一行「💭 已思考 N 秒」，
/// **明细（可能几十万字符）只在这里渲染一次** —— 这既是用户要的观感，也是「界面不卡死」的关键：
/// 以前每个输出 chunk 都新建一条气泡并滚动到底，一次大输出就是成百上千条气泡。
///
/// 输出按项预算均分（对齐 MAUI 的 ShareFor），避免「先到先得」把后序关键输出饿死。
/// </summary>
public sealed class ToolDetailWindow : Window
{
    private const int TotalBudget = 120_000; // 详情总字符预算
    private const int ItemMin = 2_000;
    private const int ItemMax = 30_000;

    private static readonly FontFamily Mono = new("Cascadia Mono,Consolas,Menlo,monospace");

    private readonly ChatMessage _msg;
    private readonly bool _thinking;
    private readonly StackPanel _body = new() { Spacing = 8 };
    private readonly TextBox _search = new() { PlaceholderText = "搜索…", IsVisible = false };

    private ToolDetailWindow(ChatMessage msg, bool thinking)
    {
        _msg = msg;
        _thinking = thinking;
        Title = thinking ? "💭 思考详情" : "🔧 工具调用详情";
        Width = 920;
        Height = 640;
        MinWidth = 420;
        MinHeight = 320;
        Background = GuiColors.WindowBg;

        Content = BuildLayout();
        KeyDown += (_, e) => { if (e.Key == Key.Escape) Close(); };
        RenderBody();
    }

    /// <summary>打开工具调用详情（owner 为空也能开：非模态，用户可以继续聊天）</summary>
    public static void ShowToolCalls(Window? owner, ChatMessage msg) => ShowImpl(owner, new ToolDetailWindow(msg, thinking: false));

    /// <summary>打开思考全文</summary>
    public static void ShowThinking(Window? owner, ChatMessage msg) => ShowImpl(owner, new ToolDetailWindow(msg, thinking: true));

    private static void ShowImpl(Window? owner, ToolDetailWindow w)
    {
        if (owner != null) w.Show(owner); else w.Show();
    }

    private Control BuildLayout()
    {
        var root = new DockPanel { Margin = new Thickness(14) };

        var head = new StackPanel { Spacing = 8, [DockPanel.DockProperty] = Dock.Top };
        head.Children.Add(new TextBlock
        {
            Text = _thinking
                ? $"{_msg.ThinkingLine} · {_msg.ReasoningBody.Length} 字符"
                : $"{_msg.GroupLine}（输出共 {TotalChars()} 字符）",
            Foreground = GuiColors.BrushOf("TextBrush"),
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
        });
        _search.IsVisible = true;
        _search.TextChanged += (_, _) => RenderBody();
        _search.Background = GuiColors.Panel2Bg;
        _search.Foreground = GuiColors.BrushOf("TextBrush");
        _search.BorderBrush = GuiColors.Border;
        head.Children.Add(_search);
        var hint = new TextBlock
        {
            Text = "Esc 关闭 · 详情只在打开时渲染（聊天流里只留一行）",
            Foreground = GuiColors.BrushOf("DimTextBrush"),
            FontSize = 11,
        };
        head.Children.Add(hint);
        root.Children.Add(head);

        var scroll = new ScrollViewer
        {
            Content = _body,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        root.Children.Add(scroll);
        return root;
    }

    private int TotalChars()
    {
        var n = 0;
        foreach (var it in _msg.ToolCalls) n += it.Detail.Length;
        return n;
    }

    private void RenderBody()
    {
        _body.Children.Clear();
        var q = (_search.Text ?? "").Trim().ToLowerInvariant();

        if (_thinking)
        {
            var text = _msg.ReasoningBody.ToString();
            if (q.Length > 0)
            {
                var hit = text.Split('\n').Where(l => l.ToLowerInvariant().Contains(q)).ToList();
                text = hit.Count > 0 ? string.Join("\n", hit) : "无匹配行";
            }
            _body.Children.Add(MonoText(string.IsNullOrEmpty(text) ? "（暂无内容）" : text));
            return;
        }

        var items = _msg.ToolCalls;
        var perItem = Math.Max(ItemMin, Math.Min(ItemMax, TotalBudget / Math.Max(1, items.Count)));
        var shown = 0;
        for (var i = 0; i < items.Count; i++)
        {
            var it = items[i];
            var detail = it.Detail.ToString();
            if (q.Length > 0 && !(it.Name + " " + it.Args + " " + detail).ToLowerInvariant().Contains(q)) continue;

            shown++;
            var card = new StackPanel { Spacing = 4, Margin = new Thickness(0, 0, 0, 4) };
            card.Children.Add(new TextBlock
            {
                Text = $"{i + 1}. {it.Name}({it.Args})",
                Foreground = GuiColors.BrushOf("TextBrush"),
                FontSize = 13,
                FontWeight = FontWeight.SemiBold,
                TextWrapping = TextWrapping.Wrap,
            });
            card.Children.Add(new TextBlock
            {
                Text = detail.Length == 0 ? "（暂无输出）"
                    : detail.Length <= perItem ? detail
                    : detail[..perItem] + $"\n…（本条输出过长，仅显示前 {perItem} 字符）…",
                FontFamily = Mono,
                FontSize = 12,
                Foreground = GuiColors.BrushOf("DimTextBrush"),
                TextWrapping = TextWrapping.Wrap,
            });
            _body.Children.Add(card);
        }
        if (shown == 0)
            _body.Children.Add(new TextBlock
            {
                Text = q.Length > 0 ? "无匹配调用" : "（无调用）",
                Foreground = GuiColors.BrushOf("DimTextBrush"),
                FontSize = 12,
            });
    }

    private static TextBlock MonoText(string text) => new()
    {
        Text = text,
        FontFamily = Mono,
        FontSize = 12.5,
        Foreground = GuiColors.BrushOf("DimTextBrush"),
        TextWrapping = TextWrapping.Wrap,
    };
}
