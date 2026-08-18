using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
        return WaitWithTimeout(tcs.Task, timeoutMs, 2); // 超时 = 拒绝（防 Agent 无限挂起）
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
        return WaitWithTimeout(tcs.Task, timeoutMs, null); // 超时 = 取消
    }

    // ── 单选 / 多选（MVP：简单列表对话框）──

    public async Task<string?> SelectAsync(string title, List<string> choices, int timeoutMs)
    {
        var picked = await WaitWithTimeout(PickAsync(title, choices, multi: false), timeoutMs, null);
        return picked?.FirstOrDefault();
    }

    public Task<List<string>?> MultiSelectAsync(string title, List<string> choices, int timeoutMs)
        => WaitWithTimeout(PickAsync(title, choices, multi: true), timeoutMs, null);

    // ── Diff 预览（逐 hunk 确认：勾选接受任意子集，对齐 TUI/Web）──

    public Task<DiffConfirmResult?> DiffConfirmAsync(string filePath, List<DiffPreview.Hunk> hunks, int timeoutMs)
    {
        var tcs = new TaskCompletionSource<DiffConfirmResult?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(() =>
        {
            var win = new Window
            {
                Title = $"Diff 预览 — {filePath}",
                Width = 820,
                Height = 620,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = new SolidColorBrush(Color.Parse("#171a23")),
            };
            var root = new DockPanel { Margin = new Avalonia.Thickness(16) };

            // 每个 hunk 的勾选状态（默认不勾 = 拒绝）
            var accepted = new HashSet<int>();

            // ── 底部按钮行 ──
            void Finish()
            {
                if (accepted.Count == hunks.Count) tcs.TrySetResult(new DiffConfirmResult { Decision = DiffPreview.Decision.AcceptAll });
                else if (accepted.Count == 0) tcs.TrySetResult(new DiffConfirmResult { Decision = DiffPreview.Decision.RejectAll });
                else tcs.TrySetResult(new DiffConfirmResult { Decision = DiffPreview.Decision.Partial, AcceptedHunks = new HashSet<int>(accepted) });
                win.Close();
            }
            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Avalonia.Thickness(0, 0, 0, 10) };
            btnRow.Children.Add(MakeButton("✔ 全部接受", "#1a7f37", () => { accepted.Clear(); for (int i = 0; i < hunks.Count; i++) accepted.Add(i); Finish(); }));
            btnRow.Children.Add(MakeButton("✘ 全部拒绝", "#d73a49", () => { accepted.Clear(); Finish(); }));
            btnRow.Children.Add(MakeButton("取消", "#5b6472", () => { win.Close(); tcs.TrySetResult(null); }));
            btnRow.Children.Add(MakeButton("💾 应用所选", "#2f6bff", Finish));
            DockPanel.SetDock(btnRow, Dock.Bottom);
            root.Children.Add(btnRow);

            // 顶部提示
            var hint = new TextBlock
            {
                Text = "勾选要接受的变更（不勾 = 拒绝该 hunk），底部「应用所选」确认",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.Parse("#8b93a7")),
                Margin = new Avalonia.Thickness(0, 0, 0, 8),
            };
            DockPanel.SetDock(hint, Dock.Top);
            root.Children.Add(hint);

            // ── 逐 hunk 列表 ──
            var listHost = new StackPanel { Spacing = 8 };
            var scroller = new ScrollViewer { Content = listHost, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };

            for (int i = 0; i < hunks.Count; i++)
            {
                var h = hunks[i];
                var idx = i;

                var checkbox = new CheckBox
                {
                    Content = "接受此变更",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.Parse("#e6e8ee")),
                    IsChecked = false,
                };
                checkbox.IsCheckedChanged += (_, _) =>
                {
                    if (checkbox.IsChecked == true) accepted.Add(idx);
                    else accepted.Remove(idx);
                };

                // hunk 内容（+ 绿 / - 红 / 空格普通）
                var content = new StackPanel { Spacing = 1, Margin = new Avalonia.Thickness(12, 0, 0, 0) };
                var head = new TextBlock
                {
                    Text = h.Header,
                    FontFamily = new FontFamily("Menlo,Consolas,monospace"),
                    FontSize = 12,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#58a6ff")),
                };
                content.Children.Add(head);
                foreach (var l in h.Lines)
                {
                    var color = l.Kind == '+' ? "#3fb950" : l.Kind == '-' ? "#e5534b" : "#c9d1d9";
                    content.Children.Add(new TextBlock
                    {
                        Text = l.Kind + l.Text,
                        FontFamily = new FontFamily("Menlo,Consolas,monospace"),
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.Parse(color)),
                    });
                }

                var box = new StackPanel { Spacing = 4 };
                box.Children.Add(checkbox);
                box.Children.Add(content);
                listHost.Children.Add(new Border
                {
                    Child = box,
                    Background = new SolidColorBrush(Color.Parse("#1d2230")),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Avalonia.Thickness(12, 8),
                });
            }

            root.Children.Add(scroller);
            win.Content = root;
            win.Closed += (_, _) => tcs.TrySetResult(null); // 关闭窗口 = 取消
            win.ShowDialog(_owner);
        });
        return WaitWithTimeout(tcs.Task, timeoutMs, null); // 超时 = 取消
    }

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
                var picked = list.SelectedItems?.Cast<string>().ToList() ?? [];
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

    /// <summary>带超时的等待：超时返回 fallback（防 Agent 因对话框无人点击而无限挂起）。</summary>
    private static async Task<T> WaitWithTimeout<T>(Task<T> task, int timeoutMs, T fallback)
    {
        if (timeoutMs <= 0) return await task.ConfigureAwait(false);
        var done = await Task.WhenAny(task, Task.Delay(timeoutMs)).ConfigureAwait(false);
        return done == task ? await task.ConfigureAwait(false) : fallback;
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
