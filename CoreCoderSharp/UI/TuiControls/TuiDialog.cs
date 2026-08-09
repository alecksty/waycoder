namespace CoreCoderSharp.UI.Controls;

/// <summary>
/// 对话框工具集 —— 常用对话框的便捷工厂方法。
/// 所有对话框基于 TuiWindow + 控件树构建。
/// 调用方需要将返回的窗口添加到 Screen 并处理关闭逻辑。
/// </summary>
public static class TuiDialog
{
    /// <summary>对话框返回结果</summary>
    public enum DialogResult { Ok, Yes, No, Cancel, Closed }

    // ── 消息框 ──

    /// <summary>信息提示框（单"确定"按钮）</summary>
    public static TuiWindow Info(string title, string message)
    {
        var win = new TuiWindow
        {
            Title = title,
            Modal = true, HasMask = true, BorderColor = 36,
            Border = WindowBorder.Rounded,
            WinBg = 0,  // 不填充，控件自带背景，遮罩提供层次
        };
        BuildContent(win, message, ("确定", () => win.OnClosed?.Invoke()));
        return win;
    }

    /// <summary>成功提示框（绿色边框）</summary>
    public static TuiWindow Success(string title, string message)
    {
        var win = Info(title, message);
        win.BorderColor = 32;
        return win;
    }

    /// <summary>警告提示框（黄色边框）</summary>
    public static TuiWindow Warn(string title, string message)
    {
        var win = Info(title, message);
        win.BorderColor = 33;
        return win;
    }

    /// <summary>错误提示框（红色边框）</summary>
    public static TuiWindow Error(string title, string message)
    {
        var win = Info(title, message);
        win.BorderColor = 31;
        return win;
    }

    // ── 确认框 ──

    /// <summary>Yes/No 确认框。onResult(true=Yes, false=No)</summary>
    public static TuiWindow Confirm(string title, string message,
        Action<bool> onResult)
    {
        var win = new TuiWindow
        {
            Title = title,
            Modal = true, HasMask = true, BorderColor = 33,
            Border = WindowBorder.Rounded,
            WinBg = 0,
        };

        var vbox = new TuiVBox { Width = 40 };
        vbox.Add(new TuiLabel(message) { Width = 38 });
        vbox.Add(new TuiLabel("") { Height = 1 });

        var hbox = new TuiHBox { Spacing = 2, Width = 38, ContentHAlign = HAlign.Center };
        var yesBtn = new TuiButton("是 (Y)") { Width = 12, Focused = true };
        var noBtn = new TuiButton("否 (N)") { Width = 12 };
        yesBtn.OnClick = () => { onResult(true); win.OnClosed?.Invoke(); };
        noBtn.OnClick = () => { onResult(false); win.OnClosed?.Invoke(); };
        hbox.Add(yesBtn); hbox.Add(noBtn);
        vbox.Add(hbox);

        vbox.Layout();
        win.Width = vbox.Width + 4;
        win.Height = vbox.Height + 4;
        win.RootView = vbox;
        win.Center();
        return win;
    }

    /// <summary>Yes/No/Cancel 三选确认框</summary>
    public static TuiWindow Confirm3(string title, string message,
        Action<DialogResult> onResult)
    {
        var win = new TuiWindow
        {
            Title = title,
            Modal = true, HasMask = true, BorderColor = 33,
            Border = WindowBorder.Rounded,
            WinBg = 0,
        };

        var vbox = new TuiVBox { Width = 48 };
        vbox.Add(new TuiLabel(message) { Width = 46 });
        vbox.Add(new TuiLabel("") { Height = 1 });

        var hbox = new TuiHBox { Spacing = 2, Width = 46, ContentHAlign = HAlign.Center };
        var yesBtn = new TuiButton("是 (Y)") { Width = 12, Focused = true };
        var noBtn = new TuiButton("否 (N)") { Width = 12 };
        var cancelBtn = new TuiButton("取消 (Esc)") { Width = 14 };
        yesBtn.OnClick = () => { onResult(DialogResult.Yes); win.OnClosed?.Invoke(); };
        noBtn.OnClick = () => { onResult(DialogResult.No); win.OnClosed?.Invoke(); };
        cancelBtn.OnClick = () => { onResult(DialogResult.Cancel); win.OnClosed?.Invoke(); };
        hbox.Add(yesBtn); hbox.Add(noBtn); hbox.Add(cancelBtn);
        vbox.Add(hbox);

        vbox.Layout();
        win.Width = vbox.Width + 4;
        win.Height = vbox.Height + 4;
        win.RootView = vbox;
        win.Center();
        return win;
    }

    // ── 输入对话框 ──

    /// <summary>文本输入对话框</summary>
    public static TuiWindow Input(string title, string prompt, string defaultValue,
        Action<string> onConfirm, Action? onCancel = null)
    {
        var win = new TuiWindow
        {
            Title = title,
            Modal = true, HasMask = true, BorderColor = 36,
            Border = WindowBorder.Rounded,
            WinBg = 0,
        };

        var vbox = new TuiVBox { Width = 44 };
        vbox.Add(new TuiLabel(prompt) { Width = 42, Fg = 37 });

        var input = new TuiInput
        {
            Text = defaultValue, CursorPos = defaultValue.Length,
            Width = 42, Focused = true
        };
        vbox.Add(input);

        var hbox = new TuiHBox { Spacing = 2, Width = 42, ContentHAlign = HAlign.Center };
        var okBtn = new TuiButton("确定") { Width = 10 };
        var cancelBtn = new TuiButton("取消") { Width = 10 };
        okBtn.OnClick = () => { onConfirm(input.Text); win.OnClosed?.Invoke(); };
        cancelBtn.OnClick = () => { onCancel?.Invoke(); win.OnClosed?.Invoke(); };
        hbox.Add(okBtn); hbox.Add(cancelBtn);
        vbox.Add(hbox);

        vbox.Layout();
        win.Width = vbox.Width + 4;
        win.Height = vbox.Height + 4;
        win.RootView = vbox;
        win.Center();
        return win;
    }

    // ── 列表选择对话框 ──

    /// <summary>单选列表对话框</summary>
    public static TuiWindow Select(string title, List<string> items,
        Action<int> onSelect, Action? onCancel = null)
    {
        var visItems = Math.Min(items.Count, 12);
        var maxVw = items.Count > 0 ? items.Max(i => TuiHelper.DisplayWidth(i)) : 10;
        var listW = Math.Max(20, maxVw + 6);

        var win = new TuiWindow
        {
            Title = title,
            Modal = true, HasMask = true, BorderColor = 36,
            Border = WindowBorder.Rounded,
        };

        var vbox = new TuiVBox { Width = listW };
        var list = new TuiList
        {
            Items = items, SelectedIndex = 0,
            Width = listW, Height = visItems, Focused = true
        };
        list.OnSelect = idx => { onSelect(idx); win.OnClosed?.Invoke(); };
        vbox.Add(list);

        var hbox = new TuiHBox { Spacing = 2, Width = listW, ContentHAlign = HAlign.Center };
        var cancelBtn = new TuiButton("取消 (Esc)") { Width = 14 };
        cancelBtn.OnClick = () => { onCancel?.Invoke(); win.OnClosed?.Invoke(); };
        hbox.Add(cancelBtn);
        vbox.Add(hbox);

        vbox.Layout();
        win.Width = vbox.Width + 4;
        win.Height = vbox.Height + 4;
        win.RootView = vbox;
        win.Center();
        return win;
    }

    /// <summary>多选列表对话框</summary>
    public static TuiWindow MultiSelect(string title, List<string> items,
        Action<HashSet<int>> onConfirm, Action? onCancel = null)
    {
        var visItems = Math.Min(items.Count, 12);
        var maxVw = items.Count > 0 ? items.Max(i => TuiHelper.DisplayWidth(i)) : 10;
        var listW = Math.Max(24, maxVw + 6);

        var win = new TuiWindow
        {
            Title = title,
            Modal = true, HasMask = true, BorderColor = 36,
            Border = WindowBorder.Rounded,
        };

        var vbox = new TuiVBox { Width = listW };
        var list = new TuiList
        {
            Items = items, SelectedIndex = 0, MultiSelect = true,
            Width = listW, Height = visItems, Focused = true
        };
        vbox.Add(list);

        var hbox = new TuiHBox { Spacing = 2, Width = listW, ContentHAlign = HAlign.Center };
        var okBtn = new TuiButton("确定") { Width = 10 };
        var cancelBtn = new TuiButton("取消") { Width = 10 };
        okBtn.OnClick = () => { onConfirm(list.CheckedIndices); win.OnClosed?.Invoke(); };
        cancelBtn.OnClick = () => { onCancel?.Invoke(); win.OnClosed?.Invoke(); };
        hbox.Add(okBtn); hbox.Add(cancelBtn);
        vbox.Add(hbox);

        vbox.Layout();
        win.Width = vbox.Width + 4;
        win.Height = vbox.Height + 4;
        win.RootView = vbox;
        win.Center();
        return win;
    }

    // ── 权限确认对话框 ──

    /// <summary>
    /// 权限确认对话框 —— 带颜色编码的 Yes / No / Yes-to-all 三选项
    /// </summary>
    public static TuiWindow Permission(string title, string message,
        Action<DialogResult> onResult)
    {
        var win = new TuiWindow
        {
            Title = title,
            Modal = true, HasMask = true, BorderColor = 35, // Magenta
            Border = WindowBorder.Single,
            WinBg = 0,
        };

        var lines = message.Replace("\r\n", "\n").Split('\n');
        var maxVw = lines.Max(l => TuiHelper.DisplayWidth(l));
        // 3 个按钮: 14+14+18 + spacing*2 = 50，最小宽度 62
        var w = Math.Max(62, maxVw + 10);

        var vbox = new TuiVBox { Width = w - 4 };
        foreach (var line in lines)
            vbox.Add(new TuiLabel(line) { Width = w - 6, Fg = 37 });
        vbox.Add(new TuiLabel("") { Height = 1 });

        var hbox = new TuiHBox { Spacing = 2, Width = w - 4, ContentHAlign = HAlign.Center };
        var yesBtn = new TuiButton("允许 (Y)") { Width = 14, Fg = 32, Focused = true };
        var noBtn = new TuiButton("拒绝 (N)") { Width = 14, Fg = 31 };
        var allBtn = new TuiButton("全部允许 (A)") { Width = 18, Fg = 33 };
        yesBtn.OnClick = () => { onResult(DialogResult.Yes); win.OnClosed?.Invoke(); };
        noBtn.OnClick = () => { onResult(DialogResult.No); win.OnClosed?.Invoke(); };
        allBtn.OnClick = () => { onResult(DialogResult.Ok); win.OnClosed?.Invoke(); };
        hbox.Add(yesBtn); hbox.Add(noBtn); hbox.Add(allBtn);
        vbox.Add(hbox);

        vbox.Layout();
        win.Width = vbox.Width + 4;
        win.Height = vbox.Height + 4;
        win.RootView = vbox;
        win.Center();
        return win;
    }

    // ── 内部工具 ──

    private static void BuildContent(TuiWindow win, string message, (string label, Action onClick) button)
    {
        var lines = message.Replace("\r\n", "\n").Split('\n');
        var maxVw = lines.Max(l => TuiHelper.DisplayWidth(l));
        var w = Math.Max(30, maxVw + 6);

        var vbox = new TuiVBox { Width = w - 4 };
        foreach (var line in lines)
            vbox.Add(new TuiLabel(line) { Width = w - 6 });
        vbox.Add(new TuiLabel("") { Height = 1 });

        var btn = new TuiButton(button.label, button.onClick)
        {
            Width = Math.Max(8, TuiHelper.DisplayWidth(button.label) + 4),
            Focused = true  // 默认按钮获得焦点，Enter/Space 触发
        };
        vbox.Add(btn);

        vbox.Layout();
        win.Width = vbox.Width + 4;
        win.Height = vbox.Height + 4;
        win.RootView = vbox;
        win.Center();
    }
}
