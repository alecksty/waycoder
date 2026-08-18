using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using WayCoder.Tools;

namespace WayCoder.UI.Gui;

/// <summary>
/// 右侧数据面板构建辅助（对齐 Web 右栏五卡片：任务/Token费用/修改文件/MCP/LSP）。
/// 数据直接引用主项目静态类（TodoTool/EditFileTool/McpManager/LspTool），同进程无网络。
/// </summary>
internal static class Panels
{
    // ── 基础构件 ──

    /// <summary>卡片容器：标题（accent 粗体）+ 内容区 + 底部边框分隔。</summary>
    public static Border Card(string title)
    {
        var head = new TextBlock
        {
            Text = title,
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 6),
        };
        head[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("AccentBrush");
        var content = new StackPanel { Spacing = 4 };
        var box = new StackPanel { Spacing = 2 };
        box.Children.Add(head);
        box.Children.Add(content);
        var border = new Border
        {
            Padding = new Thickness(14, 11),
            Child = box,
            BorderThickness = new Thickness(0, 0, 0, 1),
        };
        border[!Border.BorderBrushProperty] = new DynamicResourceExtension("BorderBrush");
        return border;
    }

    /// <summary>取卡片的内容 StackPanel。</summary>
    public static StackPanel Content(Border card)
        => card.Child is StackPanel box && box.Children.Count > 1
            ? (StackPanel)box.Children[1]
            : new StackPanel();

    /// <summary>状态圆点（8×8）。</summary>
    public static Border StatusDot(string colorHex) => new()
    {
        Width = 8,
        Height = 8,
        CornerRadius = new CornerRadius(4),
        Background = new SolidColorBrush(Color.Parse(colorHex)),
        VerticalAlignment = VerticalAlignment.Center,
    };

    /// <summary>一行水平控件。</summary>
    public static StackPanel Row(params Control[] children)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        foreach (var c in children) row.Children.Add(c);
        return row;
    }

    /// <summary>正文文本（可选淡色/小号/换行）。</summary>
    public static TextBlock Text(string text, bool dim = false, double size = 12.5, bool wrap = false)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = size,
            TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        tb[!TextBlock.ForegroundProperty] = new DynamicResourceExtension(dim ? "DimTextBrush" : "TextBrush");
        return tb;
    }

    /// <summary>指定颜色文本（+n 绿 / -n 红 等）。</summary>
    public static TextBlock ColoredText(string text, string colorHex, double size = 11.5)
    {
        var tb = Text(text, size: size);
        tb.Foreground = new SolidColorBrush(Color.Parse(colorHex));
        return tb;
    }

    // ── 数据格式化（对齐 Web formatContext/formatPrice）──

    public static string FormatCtx(long ctx)
    {
        if (ctx <= 0) return "-";
        if (ctx >= 1_000_000) return Math.Round(ctx / 100_000.0) / 10 + "M";
        return Math.Round(ctx / 1000.0) + "K";
    }

    public static string FormatPrice(double price)
    {
        if (price <= 0) return "Free";
        if (price < 0.01) return "<$0.01";
        return "$" + price.ToString("0.00");
    }

    // ── 各卡片构建 ──

    /// <summary>📋 任务（TodoTool.Items，全局共享）。</summary>
    public static Border TodosCard()
    {
        var card = Card("📋 任务");
        var content = Content(card);
        try
        {
            var items = TodoTool.Items;
            if (items == null || items.Count == 0)
            {
                content.Children.Add(Text("无任务", dim: true));
                return card;
            }
            foreach (var t in items)
                content.Children.Add(Row(StatusDot(StatusColor(t.Status)), Text(t.Title, wrap: true)));
        }
        catch { content.Children.Add(Text("读取失败", dim: true)); }
        return card;
    }

    private static string StatusColor(string status) => status switch
    {
        "in_progress" => "#4f8cff",
        "completed" => "#3fb950",
        "cancelled" => "#e5534b",
        "blocked" => "#e8b34b",
        _ => "#8b93a7", // pending
    };

    /// <summary>💰 Token / 费用（当前活跃槽位 LLM 实例级）。</summary>
    public static Border TokensCard(Agent? active)
    {
        var card = Card("💰 Token / 费用");
        var content = Content(card);
        var llm = active?.LlmClient;
        if (llm == null)
        {
            content.Children.Add(Text("无数据", dim: true));
            return card;
        }
        void RowKV(string k, string v) => content.Children.Add(Row(Text(k, dim: true, size: 11.5), Text(v)));
        RowKV("本轮", $"{llm.TaskPromptTokens:N0} / {llm.TaskCompletionTokens:N0}");
        RowKV("累计", $"{llm.TotalPromptTokens:N0} / {llm.TotalCompletionTokens:N0}");
        RowKV("本轮费用", llm.TaskCost.HasValue ? "$" + llm.TaskCost.Value.ToString("F4") : "-");
        RowKV("累计估计", llm.EstimatedCost.HasValue ? "$" + llm.EstimatedCost.Value.ToString("F4") : "-");
        if (llm.LastTokensPerSec > 0)
            RowKV("速率", $"{llm.LastTokensPerSec:N0} tok/s");
        return card;
    }

    /// <summary>🔧 修改文件（EditFileTool.ChangedFiles + Stats，全局共享）。</summary>
    public static Border FilesCard()
    {
        var card = Card("🔧 修改文件");
        var content = Content(card);
        try
        {
            var files = EditFileTool.ChangedFiles.ToList();
            if (files.Count == 0)
            {
                content.Children.Add(Text("无", dim: true));
                return card;
            }
            foreach (var f in files)
            {
                EditFileTool.ChangedFileStats.TryGetValue(f, out var st);
                var row = Row(Text(Path.GetFileName(f), wrap: true));
                if (st.Added > 0)
                    row.Children.Add(ColoredText($"+{st.Added}", "#3fb950"));
                if (st.Deleted > 0)
                    row.Children.Add(ColoredText($"-{st.Deleted}", "#e5534b"));
                content.Children.Add(row);
            }
        }
        catch { content.Children.Add(Text("读取失败", dim: true)); }
        return card;
    }

    /// <summary>🔌 MCP 服务器（McpManager.Servers，全局共享）。</summary>
    public static Border McpCard()
    {
        var card = Card("🔌 MCP 服务器");
        var content = Content(card);
        try
        {
            var servers = McpManager.Servers;
            if (servers == null || servers.Count == 0)
            {
                content.Children.Add(Text("未配置", dim: true));
                return card;
            }
            foreach (var s in servers)
            {
                var dot = s.Status switch
                {
                    McpServerStatus.Connected => StatusDot("#3fb950"),
                    McpServerStatus.Connecting => StatusDot("#e8b34b"),
                    _ => StatusDot("#e5534b"),
                };
                var line = Row(dot, Text(s.Name, wrap: true));
                if (s.ToolCount > 0)
                    line.Children.Add(Text($"· {s.ToolCount} 工具", dim: true, size: 11));
                content.Children.Add(line);
                if (!string.IsNullOrEmpty(s.Error))
                    content.Children.Add(Text(s.Error, dim: true, size: 11, wrap: true));
            }
        }
        catch { content.Children.Add(Text("读取失败", dim: true)); }
        return card;
    }

    /// <summary>🧠 LSP 会话（LspTool.ActiveSessions，全局共享）。</summary>
    public static Border LspCard()
    {
        var card = Card("🧠 LSP 会话");
        var content = Content(card);
        try
        {
            var sessions = LspTool.ActiveSessions;
            if (sessions == null || sessions.Count == 0)
            {
                content.Children.Add(Text("无活动会话", dim: true));
                return card;
            }
            foreach (var s in sessions)
            {
                var dot = s.HasExited ? StatusDot("#e5534b")
                    : s.Initialized ? StatusDot("#3fb950")
                    : StatusDot("#e8b34b");
                var line = Row(dot, Text(s.Command, wrap: true));
                content.Children.Add(line);
                if (!string.IsNullOrEmpty(s.Root))
                    content.Children.Add(Text(s.Root, dim: true, size: 11, wrap: true));
            }
        }
        catch { content.Children.Add(Text("读取失败", dim: true)); }
        return card;
    }
}
