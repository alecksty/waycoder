using Microsoft.Maui.Controls.Shapes;
using WayCoder.Maui.Services;

namespace WayCoder.Maui.Pages;

/// <summary>
/// 会话历史页（独立页，替代原左侧抽屉浮层）——聊天页保持全宽，无覆盖/让位布局干扰。
/// 点选会话或「＋ 新会话」后经 <see cref="ChatPage.PendingOpenSessionId"/> 通知聊天页，
/// 返回聊天页时消费并切换/新建（聊天页 OnAppearing 处理）。
/// </summary>
public sealed class SessionHistoryPage : ContentPage
{
    private readonly VerticalStackLayout _list = new() { Spacing = 4, Padding = new Thickness(8, 4, 8, 16) };

    public SessionHistoryPage()
    {
        Title = "会话历史";
        BackgroundColor = Res(isDark ? "CardBgDark" : "CardBgLight");

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) },
            Padding = new Thickness(16, 12, 8, 6),
        };
        header.Add(new Label
        {
            Text = "🗂 会话历史",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
            TextColor = Res(isDark ? "MainTextDark" : "MainTextLight"),
        });
        var btn = new Button
        {
            Text = "＋ 新会话",
            FontSize = 13,
            Padding = new Thickness(10, 6),
            BackgroundColor = Colors.Transparent,
            TextColor = Res(isDark ? "MutedTextDark" : "MutedTextLight"),
        };
        btn.Clicked += async (_, _) =>
        {
            ChatPage.PendingOpenSessionId = MauiSessions.NewId();
            await Shell.Current.GoToAsync(".."); // 回聊天页 → OnAppearing 消费新建
        };
        header.Add(btn, 1);

        var scroll = new ScrollView { Content = _list };
        Content = new Grid
        {
            RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) },
            Children = { header, scroll },
        };
        Grid.SetRow(scroll, 1);
    }

    private static bool isDark => MauiUi.IsDark;
    private static Color Res(string key) => MauiUi.Res(key);

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Populate();
    }

    private void Populate()
    {
        _list.Children.Clear();
        var main = Res(isDark ? "MainTextDark" : "MainTextLight");
        var muted = Res(isDark ? "MutedTextDark" : "MutedTextLight");
        var primary = Res("Primary");
        var inputBg = Res(isDark ? "InputBgDark" : "InputBgLight");

        var sessions = MauiSessions.List(50);
        if (sessions.Count == 0)
        {
            _list.Add(new Label
            {
                Text = "暂无历史会话",
                FontSize = 12,
                TextColor = muted,
                Margin = new Thickness(12, 20),
            });
            return;
        }

        var current = MauiSessions.CurrentSessionId();
        foreach (var s in sessions)
        {
            var isCur = s.Id == current;
            var card = new Border
            {
                Padding = new Thickness(12, 9),
                StrokeThickness = 0,
                BackgroundColor = isCur ? new Color(primary.Red, primary.Green, primary.Blue, 0.14f) : inputBg,
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
            };
            card.Content = new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    new Label
                    {
                        Text = string.IsNullOrWhiteSpace(s.Preview) ? "（空会话）" : s.Preview,
                        FontSize = 14,
                        FontAttributes = isCur ? FontAttributes.Bold : FontAttributes.None,
                        TextColor = isCur ? primary : main,
                        LineBreakMode = LineBreakMode.TailTruncation,
                        MaxLines = 1,
                    },
                    new Label
                    {
                        Text = $"{MauiSessions.RelativeTime(s.SavedAt)} · {s.MessageCount} 条消息",
                        FontSize = 11,
                        TextColor = muted,
                    },
                },
            };
            var id = s.Id;
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (_, _) =>
            {
                ChatPage.PendingOpenSessionId = id;
                await Shell.Current.GoToAsync(".."); // 回聊天页 → OnAppearing 切换
            };
            card.GestureRecognizers.Add(tap);
            _list.Add(card);
        }
    }
}
