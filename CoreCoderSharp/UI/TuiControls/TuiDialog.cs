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
            ShowTitleSeparator = false,
            Modal = true, HasMask = true, BorderColor = TuiTheme.Current.DialogInfoBorder,
            Border = WindowBorder.Rounded,
            WinBg = TuiTheme.Current.WindowBg,
        };
        BuildContent(win, message, ("确定", _ => win.OnClosed?.Invoke()));
        return win;
    }

    /// <summary>成功提示框（绿色边框）</summary>
    public static TuiWindow Success(string title, string message)
    {
        var win = Info(title, message);
        win.BorderColor = TuiTheme.Current.DialogSuccessBorder;
        return win;
    }

    /// <summary>警告提示框（黄色边框）</summary>
    public static TuiWindow Warn(string title, string message)
    {
        var win = Info(title, message);
        win.BorderColor = TuiTheme.Current.DialogWarnBorder;
        return win;
    }

    /// <summary>错误提示框（红色边框）</summary>
    public static TuiWindow Error(string title, string message)
    {
        var win = Info(title, message);
        win.BorderColor = TuiTheme.Current.DialogErrorBorder;
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
            ShowTitleSeparator = false,
            Modal = true, HasMask = true, BorderColor = TuiTheme.Current.DialogConfirmBorder,
            Border = WindowBorder.Rounded,
            WinBg = TuiTheme.Current.WindowBg,
        };

        var vbox = new TuiVBox { Width = 40 };
        vbox.Add(new TuiLabel(message) { Width = 38 });
        vbox.Add(new TuiLabel("") { Height = 1 });

        var hbox = new TuiHBox { Spacing = 2, Width = 38, ContentHAlign = HAlign.Center };
        var yesBtn = new TuiButton("是 (Y)") { Width = 12, Focused = true };
        var noBtn = new TuiButton("否 (N)") { Width = 12 };
        yesBtn.OnClick = _ => { win.Result = true; onResult(true); win.OnClosed?.Invoke(); };
        noBtn.OnClick = _ => { win.Result = false; onResult(false); win.OnClosed?.Invoke(); };
        hbox.Add(yesBtn); hbox.Add(noBtn);
        vbox.Add(hbox);

        vbox.Layout();
        win.Width = vbox.Width + 4;
        win.Height = vbox.Height + 4;
        win.RootView = vbox;
        win.Center();

        // 快捷键：Y=是 N=否
        win.RegisterShortcut(ConsoleKey.Y, () => { win.Result = true; onResult(true); win.OnClosed?.Invoke(); });
        win.RegisterShortcut(ConsoleKey.N, () => { win.Result = false; onResult(false); win.OnClosed?.Invoke(); });
        return win;
    }

    /// <summary>Yes/No/Cancel 三选确认框</summary>
    public static TuiWindow Confirm3(string title, string message,
        Action<DialogResult> onResult)
    {
        var win = new TuiWindow
        {
            Title = title,
            ShowTitleSeparator = false,
            Modal = true, HasMask = true, BorderColor = TuiTheme.Current.DialogConfirmBorder,
            Border = WindowBorder.Rounded,
            WinBg = TuiTheme.Current.WindowBg,
        };

        var vbox = new TuiVBox { Width = 48 };
        vbox.Add(new TuiLabel(message) { Width = 46 });
        vbox.Add(new TuiLabel("") { Height = 1 });

        var hbox = new TuiHBox { Spacing = 2, Width = 46, ContentHAlign = HAlign.Center };
        var yesBtn = new TuiButton("是 (Y)") { Width = 12, Focused = true };
        var noBtn = new TuiButton("否 (N)") { Width = 12 };
        var cancelBtn = new TuiButton("取消 (Esc)") { Width = 14 };
        yesBtn.OnClick = _ => { win.Result = DialogResult.Yes; onResult(DialogResult.Yes); win.OnClosed?.Invoke(); };
        noBtn.OnClick = _ => { win.Result = DialogResult.No; onResult(DialogResult.No); win.OnClosed?.Invoke(); };
        cancelBtn.OnClick = _ => { win.Result = DialogResult.Cancel; onResult(DialogResult.Cancel); win.OnClosed?.Invoke(); };
        hbox.Add(yesBtn); hbox.Add(noBtn); hbox.Add(cancelBtn);
        vbox.Add(hbox);

        vbox.Layout();
        win.Width = vbox.Width + 4;
        win.Height = vbox.Height + 4;
        win.RootView = vbox;
        win.Center();

        // 快捷键：Y=是 N=否 Escape=取消（TuiScreen 已处理 Esc 关闭模态，此处注册确保一致性）
        win.RegisterShortcut(ConsoleKey.Y, () => { win.Result = DialogResult.Yes; onResult(DialogResult.Yes); win.OnClosed?.Invoke(); });
        win.RegisterShortcut(ConsoleKey.N, () => { win.Result = DialogResult.No; onResult(DialogResult.No); win.OnClosed?.Invoke(); });
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
            ShowTitleSeparator = false,
            Modal = true, HasMask = true, BorderColor = TuiTheme.Current.DialogInfoBorder,
            Border = WindowBorder.Rounded,
            WinBg = TuiTheme.Current.WindowBg,
        };

        var vbox = new TuiVBox { Width = 44 };
        vbox.Add(new TuiLabel(prompt) { Width = 42, Fg = TuiTheme.Current.ControlFg });

        var input = new TuiInput
        {
            Text = defaultValue, CursorPos = defaultValue.Length,
            Width = 42, Focused = true
        };
        vbox.Add(input);

        var hbox = new TuiHBox { Spacing = 2, Width = 42, ContentHAlign = HAlign.Center };
        var okBtn = new TuiButton("确定") { Width = 10 };
        var cancelBtn = new TuiButton("取消") { Width = 10 };
        okBtn.OnClick = _ => { win.Result = input.Text; onConfirm(input.Text); win.OnClosed?.Invoke(); };
        cancelBtn.OnClick = _ => { win.Result = null; onCancel?.Invoke(); win.OnClosed?.Invoke(); };
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
            ShowTitleSeparator = false,
            Modal = true, HasMask = true, BorderColor = TuiTheme.Current.DialogInfoBorder,
            Border = WindowBorder.Rounded,
        };

        var vbox = new TuiVBox { Width = listW };
        var list = new TuiList
        {
            Items = items, SelectedIndex = 0,
            Width = listW, Height = visItems, Focused = true
        };
        list.OnSelect = idx => { win.Result = idx; onSelect(idx); win.OnClosed?.Invoke(); };
        vbox.Add(list);

        var hbox = new TuiHBox { Spacing = 2, Width = listW, ContentHAlign = HAlign.Center };
        var cancelBtn = new TuiButton("取消 (Esc)") { Width = 14 };
        cancelBtn.OnClick = _ => { win.Result = -1; onCancel?.Invoke(); win.OnClosed?.Invoke(); };
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
            ShowTitleSeparator = false,
            Modal = true, HasMask = true, BorderColor = TuiTheme.Current.DialogInfoBorder,
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
        okBtn.OnClick = _ => { win.Result = list.CheckedIndices; onConfirm(list.CheckedIndices); win.OnClosed?.Invoke(); };
        cancelBtn.OnClick = _ => { win.Result = null; onCancel?.Invoke(); win.OnClosed?.Invoke(); };
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
    /// 权限确认对话框 —— 黄色警告背景 + 黑色文字 + 粗体标题 + 淡蓝按钮。
    /// 快捷键：Y=允许 N=拒绝 A=全部允许
    /// </summary>
    public static TuiWindow Permission(string title, string message,
        Action<DialogResult> onResult)
    {
        const int warnBg = TuiColors.BgYellow;    // 黄色警告背景
        const int blackFg = TuiColors.Black;      // 黑色文字
        const int btnBg = TuiColors.BgCyan;       // 淡蓝按钮底
        const int btnFocusBg = TuiColors.BgWhite; // 选中白底

        var win = new TuiWindow
        {
            Title = title,
            TitleBold = true,
            TitleFg = blackFg,
            TitleBg = warnBg,
            ShowTitleSeparator = false,
            Modal = true, HasMask = true,
            BorderColor = TuiColors.Yellow, // 黄色边框
            Border = WindowBorder.Single,
            WinBg = warnBg,   // 黄色背景
        };

        var lines = message.Replace("\r\n", "\n").Split('\n');
        var maxVw = lines.Max(l => TuiHelper.DisplayWidth(l));
        // 3 个按钮: 14+14+18 + spacing*2 = 50，最小宽度 62
        var w = Math.Max(62, maxVw + 10);

        var vbox = new TuiVBox { Width = w - 4 };
        foreach (var line in lines)
            vbox.Add(new TuiLabel(line) { Width = w - 6, Fg = blackFg });
        vbox.Add(new TuiLabel("") { Height = 1 });

        var hbox = new TuiHBox { Spacing = 2, Width = w - 4, ContentHAlign = HAlign.Center };

        // 按钮：淡蓝底黑字 / 选中白底黑字
        TuiButton MakeBtn(string text, int wd)
        {
            var b = new TuiButton(text) { Width = wd };
            b.Fg = blackFg; b.Bg = btnBg;
            b.FocusedFg = blackFg; b.FocusedBg = btnFocusBg;
            return b;
        }

        var yesBtn = MakeBtn("允许 (Y)", 14);
        var noBtn = MakeBtn("拒绝 (N)", 14);
        var allBtn = MakeBtn("全部允许 (A)", 18);
        yesBtn.Focused = true;
        yesBtn.OnClick = _ => { win.Result = DialogResult.Yes; onResult(DialogResult.Yes); win.OnClosed?.Invoke(); };
        noBtn.OnClick = _ => { win.Result = DialogResult.No; onResult(DialogResult.No); win.OnClosed?.Invoke(); };
        allBtn.OnClick = _ => { win.Result = DialogResult.Ok; onResult(DialogResult.Ok); win.OnClosed?.Invoke(); };
        hbox.Add(yesBtn); hbox.Add(noBtn); hbox.Add(allBtn);
        vbox.Add(hbox);

        vbox.Layout();
        win.Width = vbox.Width + 4;
        win.Height = vbox.Height + 4;
        win.RootView = vbox;
        win.Center();

        // 快捷键注册：按钮上已标注 (Y)/(N)/(A)，窗口级拦截确保无需 Tab 切换
        win.RegisterShortcut(ConsoleKey.Y, () => { win.Result = DialogResult.Yes; onResult(DialogResult.Yes); win.OnClosed?.Invoke(); });
        win.RegisterShortcut(ConsoleKey.N, () => { win.Result = DialogResult.No; onResult(DialogResult.No); win.OnClosed?.Invoke(); });
        win.RegisterShortcut(ConsoleKey.A, () => { win.Result = DialogResult.Ok; onResult(DialogResult.Ok); win.OnClosed?.Invoke(); });
        return win;
    }

    // ── 内部工具 ──

    private static void BuildContent(TuiWindow win, string message, (string label, Action<TuiButton> onClick) button)
    {
        var lines = message.Replace("\r\n", "\n").Split('\n');
        var maxVw = lines.Max(l => TuiHelper.DisplayWidth(l));
        var w = Math.Max(30, maxVw + 6);

        var vbox = new TuiVBox { Width = w - 4 };
        foreach (var line in lines)
            vbox.Add(new TuiLabel(line) { Width = w - 6 });
        vbox.Add(new TuiLabel("") { Height = 1 });

        var btn = new TuiButton(button.label)
        {
            Width = Math.Max(8, TuiHelper.DisplayWidth(button.label) + 4),
            Focused = true  // 默认按钮获得焦点，Enter/Space 触发
        };
        btn.OnClick = _ =>
        {
            win.Result = DialogResult.Ok;
            button.onClick(btn);
        };
        vbox.Add(btn);

        vbox.Layout();
        win.Width = vbox.Width + 4;
        win.Height = vbox.Height + 4;
        win.RootView = vbox;
        win.Center();

        // 单按钮对话框：Enter 快捷键 = 点击确定
        win.RegisterShortcut(ConsoleKey.Enter, () =>
        {
            win.Result = DialogResult.Ok;
            button.onClick(btn);
        });
    }
}
