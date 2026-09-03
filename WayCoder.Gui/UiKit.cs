using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Threading;

namespace WayCoder.UI.Gui;

/// <summary>按钮视觉变体（颜色取自 GuiColors，随主题切换，代码零硬编码）。</summary>
public enum ButtonVariant
{
    /// <summary>主操作：Accent 底 + 白字（确定/保存）。</summary>
    Primary,
    /// <summary>强调：强调色底 + 白字（切换模型等醒目主按钮）。</summary>
    Accent,
    /// <summary>成功/放行：绿底 + 白字（全部允许/全部接受）。</summary>
    Success,
    /// <summary>危险：红底 + 白字（拒绝/删除）。</summary>
    Danger,
    /// <summary>次要：灰底 + 白字（取消/复位）。</summary>
    Secondary,
    /// <summary>中性：面板底 + 正文色（工具类按钮）。</summary>
    Default,
    /// <summary>幽灵：透明底 + 正文色（弱化按钮）。</summary>
    Ghost,
}

/// <summary>
/// Avalonia 界面共享控件/对话框工具：统一按钮风格（变体取色）+ 常见窗口对话框。
/// 消除各窗口重复的 <c>MakeButton/MakeBtn</c> 与 `new Window{...}` 脚手架；颜色一律经 GuiColors。
/// </summary>
public static class UiKit
{
    /// <summary>统一按钮（配色随主题变体）。</summary>
    public static Button MakeButton(string text, ButtonVariant variant, Action onClick)
    {
        var btn = new Button
        {
            Content = text,
            Padding = new Thickness(12, 5),
            FontSize = 12,
        };
        btn[!Button.ForegroundProperty] = new DynamicResourceExtension(
            variant is ButtonVariant.Default or ButtonVariant.Ghost ? "TextBrush" : "ButtonText");
        // Ghost = 透明底（直接用静态 Brushes.Transparent，非主题资源键）；其余走主题画刷。
        if (variant == ButtonVariant.Ghost)
        {
            btn.Background = Brushes.Transparent;
        }
        else
        {
            btn[!Button.BackgroundProperty] = new DynamicResourceExtension(variant switch
            {
                ButtonVariant.Primary => "ButtonPrimaryBg",
                ButtonVariant.Accent => "AccentBrush",
                ButtonVariant.Success => "StatusSuccess",
                ButtonVariant.Danger => "ButtonDangerBg",
                ButtonVariant.Secondary => "ButtonSecondaryBg",
                ButtonVariant.Default => "Panel2BgBrush",
                _ => "Panel2BgBrush",
            });
        }
        btn.Click += (_, _) => onClick();
        return btn;
    }

    /// <summary>小圆角按钮（会话操作 ✎/✕）。</summary>
    public static Button MakeSmallButton(string text, Action onClick)
    {
        var btn = new Button
        {
            Content = text,
            Width = 22,
            Height = 22,
            FontSize = 11,
            Padding = new Thickness(0),
        };
        btn.Click += (_, _) => onClick();
        return btn;
    }

    /// <summary>对话框底部按钮行（右对齐、等距）。</summary>
    public static StackPanel ButtonRow(params Button[] buttons)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        foreach (var b in buttons) row.Children.Add(b);
        return row;
    }

    // ═══════════════════════════ 通用对话框 ═══════════════════════════

    /// <summary>确认框：确定 / 取消，返回是否确定。</summary>
    public static Task<bool> ShowConfirm(Window owner, string title, string message, string ok = "确定", string cancel = "取消")
    {
        var tcs = new TaskCompletionSource<bool>();
        Dispatcher.UIThread.Post(() =>
        {
            var win = NewDialog(owner, title, 430);
            var panel = new StackPanel { Margin = new Thickness(16), Spacing = 12 };
            panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });
            panel.Children.Add(ButtonRow(
                MakeButton(ok, ButtonVariant.Primary, () => { tcs.TrySetResult(true); win.Close(); }),
                MakeButton(cancel, ButtonVariant.Secondary, () => { tcs.TrySetResult(false); win.Close(); })));
            win.Content = panel;
            win.Closed += (_, _) => tcs.TrySetResult(false);
            win.ShowDialog(owner);
        });
        return tcs.Task;
    }

    /// <summary>
    /// 文本输入框：确定返回输入，取消返回 null。
    /// <paramref name="defaultValue"/> 预填实际值（编辑现有 key/名称/地址用）；<paramref name="placeholder"/> 非空时作为灰色占位提示
    /// （新建文件路径等场景，startText 留空、placeholder 提示格式，避免占位符被当成预填值提交）。
    /// </summary>
    public static Task<string?> ShowPrompt(Window owner, string title, string label, string defaultValue, string placeholder = "")
    {
        var tcs = new TaskCompletionSource<string?>();
        Dispatcher.UIThread.Post(() =>
        {
            var win = NewDialog(owner, title, 460);
            var panel = new StackPanel { Margin = new Thickness(16), Spacing = 12 };
            panel.Children.Add(new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap });
            var input = new TextBox { Text = defaultValue, MinWidth = 380, AcceptsReturn = true, MinHeight = 60 };
            if (!string.IsNullOrEmpty(placeholder)) input.PlaceholderText = placeholder;
            panel.Children.Add(input);
            panel.Children.Add(ButtonRow(
                MakeButton("确定", ButtonVariant.Primary, () => { tcs.TrySetResult(input.Text); win.Close(); }),
                MakeButton("取消", ButtonVariant.Secondary, () => { tcs.TrySetResult(null); win.Close(); })));
            win.Content = panel;
            win.Closed += (_, _) => tcs.TrySetResult(null);
            win.ShowDialog(owner);
        });
        return tcs.Task;
    }

    /// <summary>单选下拉框：返回选中项，取消返回 null。</summary>
    public static Task<string?> ShowSelect(Window owner, string title, string[] options)
    {
        var tcs = new TaskCompletionSource<string?>();
        Dispatcher.UIThread.Post(() =>
        {
            var win = NewDialog(owner, title, 420);
            var panel = new StackPanel { Margin = new Thickness(16), Spacing = 12 };
            var combo = new ComboBox { ItemsSource = options, SelectedIndex = 0 };
            panel.Children.Add(combo);
            panel.Children.Add(ButtonRow(
                MakeButton("确定", ButtonVariant.Primary, () => { tcs.TrySetResult(combo.SelectedItem as string ?? options.FirstOrDefault()); win.Close(); }),
                MakeButton("取消", ButtonVariant.Secondary, () => { tcs.TrySetResult(null); win.Close(); })));
            win.Content = panel;
            win.Closed += (_, _) => tcs.TrySetResult(null);
            win.ShowDialog(owner);
        });
        return tcs.Task;
    }

    /// <summary>多选勾选框：返回选中项列表，取消/未选返回 null。</summary>
    public static Task<List<string>?> ShowMultiCheck(Window owner, string title, string[] options, bool preCheckAll = false)
    {
        var tcs = new TaskCompletionSource<List<string>?>();
        Dispatcher.UIThread.Post(() =>
        {
            var win = NewDialog(owner, title, 440, 380);
            var panel = new StackPanel { Margin = new Thickness(16), Spacing = 10 };
            var list = new StackPanel { Spacing = 6 };
            var checks = options.Select(o => new CheckBox { Content = o, IsChecked = preCheckAll }).ToList();
            foreach (var c in checks) list.Children.Add(c);
            panel.Children.Add(list);
            panel.Children.Add(ButtonRow(
                MakeButton("确定", ButtonVariant.Primary, () =>
                {
                    var picked = checks.Where(c => c.IsChecked == true).Select(c => (string)c.Content!).ToList();
                    tcs.TrySetResult(picked.Count > 0 ? picked : null);
                    win.Close();
                }),
                MakeButton("取消", ButtonVariant.Secondary, () => { tcs.TrySetResult(null); win.Close(); })));
            win.Content = panel;
            win.Closed += (_, _) => tcs.TrySetResult(null);
            win.ShowDialog(owner);
        });
        return tcs.Task;
    }

    /// <summary>三选一消息框：0=主 1=次 -1=取消（Unsaved 等）。</summary>
    public static Task<int> ShowMessageBox(Window owner, string title, string message, string primary, string secondary, string cancel)
    {
        var tcs = new TaskCompletionSource<int>();
        Dispatcher.UIThread.Post(() =>
        {
            var win = NewDialog(owner, title, 380);
            var panel = new StackPanel { Margin = new Thickness(16), Spacing = 12 };
            panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });
            panel.Children.Add(ButtonRow(
                MakeButton(primary, ButtonVariant.Primary, () => { tcs.TrySetResult(0); win.Close(); }),
                MakeButton(secondary, ButtonVariant.Default, () => { tcs.TrySetResult(1); win.Close(); }),
                MakeButton(cancel, ButtonVariant.Secondary, () => { tcs.TrySetResult(-1); win.Close(); })));
            win.Content = panel;
            win.Closed += (_, _) => tcs.TrySetResult(-1);
            win.ShowDialog(owner);
        });
        return tcs.Task;
    }

    /// <summary>
    /// 标准居中对话框。height&gt;0 走固定高度（Manual）；height=0 走 SizeToContent.Height 按内容自适应
    /// （showConfirm/showPrompt/showSelect/showMessageBox 等纯内容对话框不传 height，必须自适应，否则会被压到近零高）。
    /// </summary>
    public static Window NewDialog(Window owner, string title, double width, double height = 0)
    {
        var win = new Window
        {
            Title = title,
            Width = width,
            CanResize = false,
            SizeToContent = height > 0 ? SizeToContent.Manual : SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        if (height > 0) win.Height = height;
        win[!Window.BackgroundProperty] = new DynamicResourceExtension("WindowBgBrush");
        return win;
    }
}
