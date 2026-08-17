using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using WayCoder.UI.Tui;

namespace WayCoder.UI.Gui;

/// <summary>
/// GUI 交互桥 —— 实现 UxHelper.IWebInteraction，用 Avalonia 模态对话框承接
/// Agent 的权限确认（bash/write/edit）、文本提问、单选/多选、diff 逐 hunk 确认。
/// 注入到 UxHelper.WebInteraction 后，Agent 不再回退 Console I/O（GUI 无控制台）。
/// </summary>
public sealed class GuiInteraction : UxHelper.IWebInteraction
{
    private readonly Window _owner;

    public GuiInteraction(Window owner) => _owner = owner;

    // ── 确认框（0=是 1=总是允许 2=否）──

    public Task<int> ConfirmAsync(string title, string message, bool allowAll, int timeoutMs)
    {
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(() =>
        {
            var win = new Window
            {
                Title = title,
                Width = 540,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Background = new SolidColorBrush(Color.Parse("#171a23")),
            };

            var panel = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 16 };
            panel.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.Parse("#e6e8ee")),
                FontSize = 13,
            });

            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right };
            btnRow.Children.Add(MakeButton("允许", "#2f6bff", () => { win.Close(); tcs.TrySetResult(0); }));
            if (allowAll)
                btnRow.Children.Add(MakeButton("全部允许", "#1a7f37", () => { win.Close(); tcs.TrySetResult(1); }));
            btnRow.Children.Add(MakeButton("拒绝", "#d73a49", () => { win.Close(); tcs.TrySetResult(2); }));
            panel.Children.Add(btnRow);

            win.Content = panel;
            win.Closed += (_, _) => tcs.TrySetResult(2); // 关闭窗口 = 拒绝
            win.ShowDialog(_owner);
        });
        return tcs.Task;
    }

    // ── 文本提问 ──

    public Task<string?> AskAsync(string prompt, string? defaultValue, int timeoutMs)
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(() =>
        {
            var win = new Window
            {
                Title = "输入",
                Width = 480,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Background = new SolidColorBrush(Color.Parse("#171a23")),
            };
            var panel = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 12 };
            panel.Children.Add(new TextBlock { Text = prompt, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.Parse("#e6e8ee")) });
            var input = new TextBox { Text = defaultValue ?? "", AcceptsReturn = true, MinHeight = 60 };
            panel.Children.Add(input);
            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right };
            btnRow.Children.Add(MakeButton("确定", "#2f6bff", () => { win.Close(); tcs.TrySetResult(input.Text); }));
            btnRow.Children.Add(MakeButton("取消", "#5b6472", () => { win.Close(); tcs.TrySetResult(null); }));
            panel.Children.Add(btnRow);
            win.Content = panel;
            win.Closed += (_, _) => tcs.TrySetResult(null);
            win.ShowDialog(_owner);
        });
        return tcs.Task;
    }

    // ── 单选 / 多选（MVP：简单列表对话框）──

    public Task<string?> SelectAsync(string title, List<string> choices, int timeoutMs)
        => PickAsync(title, choices, multi: false).ContinueWith(t => (string?)t.Result?.FirstOrDefault());

    public Task<List<string>?> MultiSelectAsync(string title, List<string> choices, int timeoutMs)
        => PickAsync(title, choices, multi: true);

    // ── Diff 逐 hunk 确认（MVP：整文件一次性确认，null 已由调用方按拒绝处理）──

    public Task<DiffConfirmResult?> DiffConfirmAsync(string filePath, List<DiffPreview.Hunk> hunks, int timeoutMs)
        => Task.FromResult<DiffConfirmResult?>(null);

    // ── 内部：单选/多选对话框 ──

    private Task<List<string>?> PickAsync(string title, List<string> choices, bool multi)
    {
        var tcs = new TaskCompletionSource<List<string>?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(() =>
        {
            var win = new Window
            {
                Title = title,
                Width = 480,
                Height = 420,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = new SolidColorBrush(Color.Parse("#171a23")),
            };
            var panel = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 12 };
            var list = new ListBox { ItemsSource = choices, SelectionMode = multi ? SelectionMode.Multiple : SelectionMode.Single };
            panel.Children.Add(list);
            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right };
            btnRow.Children.Add(MakeButton("确定", "#2f6bff", () =>
            {
                var picked = list.SelectedItems.Cast<string>().ToList();
                win.Close();
                tcs.TrySetResult(picked.Count > 0 ? picked : null);
            }));
            btnRow.Children.Add(MakeButton("取消", "#5b6472", () => { win.Close(); tcs.TrySetResult(null); }));
            panel.Children.Add(btnRow);
            win.Content = panel;
            win.Closed += (_, _) => tcs.TrySetResult(null);
            win.ShowDialog(_owner);
        });
        return tcs.Task;
    }

    private static Button MakeButton(string text, string colorHex, Action onClick)
    {
        var btn = new Button
        {
            Content = text,
            Padding = new Avalonia.Thickness(14, 6),
            Background = new SolidColorBrush(Color.Parse(colorHex)),
            Foreground = new SolidColorBrush(Colors.White),
        };
        btn.Click += (_, _) => onClick();
        return btn;
    }
}
