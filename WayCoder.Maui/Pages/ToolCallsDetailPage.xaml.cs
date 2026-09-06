using WayCoder.Maui.Markup;
using WayCoder.Maui.Models;

namespace WayCoder.Maui.Pages;

/// <summary>
/// 工具调用详情页（只读）——「工具调用:N 次」行的目标子页。
/// 聊天流默认只显示一行计数，点入口经 <see cref="Target"/> 传工具组消息，
/// 此页逐个工具分区展示：名称 + 参数摘要 + 输出详情（语法高亮富文本）。
/// </summary>
public partial class ToolCallsDetailPage : ContentPage
{
    /// <summary>待展示的工具组消息（ChatPage 点击入口时赋值；页 OnAppearing 读取渲染）。</summary>
    public static ChatMessage? Target { get; set; }

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
            Body.Add(new Label { Text = "（无工具调用记录）", FontSize = 13 });
            return;
        }

        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var main = isDark ? (Color?)Application.Current?.Resources["MainTextDark"] : Application.Current?.Resources["MainTextLight"];
        var muted = isDark ? Application.Current?.Resources["MutedTextDark"] : Application.Current?.Resources["MutedTextLight"];
        var primary = Application.Current?.Resources["Primary"];

        for (int i = 0; i < msg.ToolCalls.Count; i++)
        {
            var tc = msg.ToolCalls[i];
            var card = new VerticalStackLayout { Spacing = 4 };

            // 标题：序号 + 工具名
            card.Children.Add(new Label
            {
                Text = $"{(i + 1)}. 🔧 {tc.Name}",
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = main as Color ?? Colors.DimGray,
            });

            // 参数摘要（灰小字，可折叠换行）
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

            // 输出详情（语法高亮富文本；无输出不占位）
            if (!string.IsNullOrWhiteSpace(tc.Detail))
            {
                var detailLabel = new Label
                {
                    FontSize = 12,
                    LineHeight = 1.25,
                    LineBreakMode = LineBreakMode.WordWrap,
                };
                detailLabel.FormattedText = ToolOutputFormatter.Render(tc.Detail, tc.FilePath, isDark);
                card.Children.Add(detailLabel);
            }

            Body.Add(card);

            // 工具间分隔线（末个不加）
            if (i < msg.ToolCalls.Count - 1)
                Body.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray, Opacity = 0.4 });
        }
    }
}
