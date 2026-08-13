using WayCoder.Terminal;

namespace WayCoder.UI.TuiControls;

/// <summary>
/// 对话框工具集 —— 常用对话框的便捷工厂方法。
/// 所有对话框基于 TuiWindow + 控件树构建，使用 XScale/Flex 自动适配终端 resize。
/// 调用方需要将返回的窗口添加到 Screen 并处理关闭逻辑。
/// </summary>
public static class TuiDialog
{
    /// <summary>对话框返回结果</summary>
    public enum DialogResult
    {
        Ok,
        Yes,
        No,
        Cancel,
        Closed
    }

    /// <summary>对话框默认宽度比例</summary>
    private const double DefaultXScale = 0.5;
    private const double WideXScale = 0.6;
    private const double NarrowXScale = 0.4;

    /// <summary>对话框最小宽度</summary>
    private const int MinDialogW = 24;

    /// <summary>根据 XScale 计算内容可用宽度（减去边框和内边距）</summary>
    private static int ContentW(double xScale = DefaultXScale, int innerPad = 4)
    {
        int winW = Math.Max(MinDialogW, (int)(Tty.Cols * xScale));
        return Math.Max(10, winW - 2 - innerPad); // 2=边框, innerPad=内边距
    }

    /// <summary>
    /// 将消息文本折行为 TuiLabel 列表。自动处理 \n 换行和超宽折行，
    /// 最多 10 行，超出行尾显示 "…"。
    /// </summary>
    private static List<TuiLabel> BuildMessageLabels(string message, int labelWidth, int? fg = null)
    {
        var lines = TuiHelper.WrapText(message, labelWidth);
        return lines.Select(line => new TuiLabel(line)
        {
            Width = labelWidth,
            Fg = fg ?? 0,
            TextAlign = HAlign.Center
        }).ToList();
    }

    /// <summary>给按钮启用渐变背景</summary>
    private static void ApplyButtonGradient((int start, int end) grad, params TuiButton[] buttons)
    {
        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            btn.GradientBg = true;
            btn.GradientBgStart = grad.start;
            btn.GradientBgEnd = grad.end;
        }
    }

    /// <summary>给窗口启用渐变边框</summary>
    private static void ApplyGradient(TuiWindow win, (int start, int end) grad)
    {
        win.GradientBorder = true;
        win.GradientStart = grad.start;
        win.GradientEnd = grad.end;
    }

    /// <summary>创建标准对话框窗口（居中、模态、带遮罩）</summary>
    private static TuiWindow NewDialog(string title, int borderColor, double xScale = DefaultXScale)
    {
        return new TuiWindow
        {
            Title = title,
            ShowTitleSeparator = false,
            Modal = true, HasMask = true,
            BorderColor = borderColor,
            Border = WindowBorder.Solid,
            WinBg = TuiTheme.Current.WindowBg,
            XScale = xScale,
            WindowHAlign = HAlign.Center,
            WindowVAlign = VAlign.Middle,
            MinWidth = MinDialogW,
            MinHeight = 5,
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // 消息框（Info / Success / Warn / Error）
    // ═══════════════════════════════════════════════════════════════

    /// <summary>信息提示框（单"确定"按钮）</summary>
    public static TuiWindow Info(string title, string message)
    {
        var win = NewDialog(title, TuiTheme.Current.DialogInfoBorder);
        BuildSingleButton(win, message, "确定", TuiTheme.Current.BtnCyanBlue, TuiTheme.Current.GradCyanBlue);
        return win;
    }

    /// <summary>成功提示框（绿色边框）</summary>
    public static TuiWindow Success(string title, string message)
    {
        var win = NewDialog(title, TuiTheme.Current.DialogSuccessBorder);
        BuildSingleButton(win, message, "确定", TuiTheme.Current.BtnGreenCyan, TuiTheme.Current.GradGreenCyan);
        return win;
    }

    /// <summary>警告提示框（黄色边框）</summary>
    public static TuiWindow Warn(string title, string message)
    {
        var win = NewDialog(title, TuiTheme.Current.DialogWarnBorder);
        BuildSingleButton(win, message, "确定", TuiTheme.Current.BtnOrangeYellow, TuiTheme.Current.GradOrangeYellow);
        return win;
    }

    /// <summary>错误提示框（红色边框）</summary>
    public static TuiWindow Error(string title, string message)
    {
        var win = NewDialog(title, TuiTheme.Current.DialogErrorBorder);
        BuildSingleButton(win, message, "确定", TuiTheme.Current.BtnRedOrange, TuiTheme.Current.GradRedOrange);
        return win;
    }

    /// <summary>单按钮消息框通用构建</summary>
    private static void BuildSingleButton(TuiWindow win, string message, string btnLabel,
        (int start, int end) btnGrad, (int start, int end) winGrad)
    {
        int cw = ContentW(DefaultXScale, 4);
        var msgLabels = BuildMessageLabels(message, cw);

        var vbox = new TuiVBox { Width = cw, ChildHAlign = HAlign.Center };
        foreach (var lbl in msgLabels) vbox.Add(lbl);
        vbox.Add(new TuiLabel("") { Height = 1 }); // spacer

        var btn = new TuiButton(btnLabel)
        {
            Width = Math.Max(8, TuiHelper.DisplayWidth(btnLabel) + 4),
            Focused = true
        };
        ApplyButtonGradient(btnGrad, btn);
        btn.OnClick = _ =>
        {
            win.Result = DialogResult.Ok;
            win.OnClosed?.Invoke();
        };
        vbox.Add(btn);

        win.RootView = vbox;
        ApplyGradient(win, winGrad);

        win.RegisterShortcut(ConsoleKey.Enter, () =>
        {
            win.Result = DialogResult.Ok;
            win.OnClosed?.Invoke();
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // 确认框
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Yes/No 确认框。onResult(true=Yes, false=No)</summary>
    public static TuiWindow Confirm(string title, string message, Action<bool> onResult)
    {
        var win = NewDialog(title, TuiTheme.Current.DialogConfirmBorder, WideXScale);

        int cw = ContentW(WideXScale, 4);
        var msgLabels = BuildMessageLabels(message, cw);

        var vbox = new TuiVBox { Width = cw, ChildHAlign = HAlign.Center };
        foreach (var lbl in msgLabels) vbox.Add(lbl);
        vbox.Add(new TuiLabel("") { Height = 1 }); // spacer

        // 按钮用 Flex 均分
        var hbox = new TuiHBox { Width = cw, Spacing = 2, ContentHAlign = HAlign.Center };
        var yesBtn = new TuiButton("是 (Y)") { Flex = 1, Focused = true };
        var noBtn = new TuiButton("否 (N)") { Flex = 1 };
        yesBtn.OnClick = _ => { win.Result = true; onResult(true); win.OnClosed?.Invoke(); };
        noBtn.OnClick = _ => { win.Result = false; onResult(false); win.OnClosed?.Invoke(); };
        hbox.Add(yesBtn); hbox.Add(noBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, yesBtn, noBtn);
        vbox.Add(hbox);

        win.RootView = vbox;
        ApplyGradient(win, TuiTheme.Current.GradOrangeYellow);

        win.RegisterShortcut(ConsoleKey.Y, () => { win.Result = true; onResult(true); win.OnClosed?.Invoke(); });
        win.RegisterShortcut(ConsoleKey.N, () => { win.Result = false; onResult(false); win.OnClosed?.Invoke(); });
        return win;
    }

    /// <summary>Yes/No/Cancel 三选确认框</summary>
    public static TuiWindow Confirm3(string title, string message, Action<DialogResult> onResult)
    {
        var win = NewDialog(title, TuiTheme.Current.DialogConfirmBorder, WideXScale);

        int cw = ContentW(WideXScale, 4);
        var msgLabels = BuildMessageLabels(message, cw);

        var vbox = new TuiVBox { Width = cw, ChildHAlign = HAlign.Center };
        foreach (var lbl in msgLabels) vbox.Add(lbl);
        vbox.Add(new TuiLabel("") { Height = 1 }); // spacer

        var hbox = new TuiHBox { Width = cw, Spacing = 2, ContentHAlign = HAlign.Center };
        var yesBtn = new TuiButton("是 (Y)") { Flex = 1, Focused = true };
        var noBtn = new TuiButton("否 (N)") { Flex = 1 };
        var cancelBtn = new TuiButton("取消 (Esc)") { Flex = 1 };
        yesBtn.OnClick = _ => { win.Result = DialogResult.Yes; onResult(DialogResult.Yes); win.OnClosed?.Invoke(); };
        noBtn.OnClick = _ => { win.Result = DialogResult.No; onResult(DialogResult.No); win.OnClosed?.Invoke(); };
        cancelBtn.OnClick = _ => { win.Result = DialogResult.Cancel; onResult(DialogResult.Cancel); win.OnClosed?.Invoke(); };
        hbox.Add(yesBtn); hbox.Add(noBtn); hbox.Add(cancelBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, yesBtn, noBtn, cancelBtn);
        vbox.Add(hbox);

        win.RootView = vbox;
        ApplyGradient(win, TuiTheme.Current.GradOrangeYellow);

        win.RegisterShortcut(ConsoleKey.Y, () => { win.Result = DialogResult.Yes; onResult(DialogResult.Yes); win.OnClosed?.Invoke(); });
        win.RegisterShortcut(ConsoleKey.N, () => { win.Result = DialogResult.No; onResult(DialogResult.No); win.OnClosed?.Invoke(); });
        return win;
    }

    // ═══════════════════════════════════════════════════════════════
    // 输入对话框
    // ═══════════════════════════════════════════════════════════════

    /// <summary>文本输入对话框（多行 TuiTextArea + 历史）</summary>
    public static TuiWindow Input(string title, string prompt, string defaultValue,
        Action<string> onConfirm, Action? onCancel = null)
    {
        const int inputHeight = 5;
        const int maxPromptLines = 5;

        var displayTitle = string.IsNullOrEmpty(title) || title == "输入" ? "请输入" : title;
        var win = NewDialog(displayTitle, TuiTheme.Current.DialogInfoBorder, WideXScale);
        win.MinHeight = 8;

        int cw = ContentW(WideXScale, 2);

        var promptLines = TuiHelper.WrapText(prompt, cw, maxPromptLines);

        var vbox = new TuiVBox { Width = cw, ChildHAlign = HAlign.Center };
        foreach (var line in promptLines)
            vbox.Add(new TuiLabel(line) { Width = cw, Fg = TuiColors.Black });

        var input = new TuiTextArea
        {
            Width = cw, Height = inputHeight,
            Fg = TuiColors.White, Bg = TuiColors.BgBlack,
            Focused = true,
        };
        var hist = TuiInputHistory.Get(title);
        var initVal = !string.IsNullOrEmpty(defaultValue) ? defaultValue
            : hist.Count > 0 ? hist[0] : "";
        if (!string.IsNullOrEmpty(initVal)) input.Text = initVal;
        vbox.Add(input);

        vbox.Add(new TuiLabel("") { Height = 1 }); // spacer

        var hbox = new TuiHBox { Width = cw, Spacing = 2, ContentHAlign = HAlign.Center };
        var okBtn = new TuiButton("确定") { Flex = 1 };
        var cancelBtn = new TuiButton("取消") { Flex = 1 };
        okBtn.OnClick = _ =>
        {
            var text = input.Text;
            win.Result = text;
            if (!string.IsNullOrWhiteSpace(text)) TuiInputHistory.Add(title, text);
            onConfirm(text);
            win.OnClosed?.Invoke();
        };
        cancelBtn.OnClick = _ => { win.Result = null; onCancel?.Invoke(); win.OnClosed?.Invoke(); };
        hbox.Add(okBtn); hbox.Add(cancelBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, okBtn, cancelBtn);
        vbox.Add(hbox);

        win.RootView = vbox;
        ApplyGradient(win, TuiTheme.Current.GradOrangeYellow);
        return win;
    }

    // ═══════════════════════════════════════════════════════════════
    // 单行输入对话框
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 单行文本输入对话框（TuiInput + 历史）。
    /// 复制 Ctrl+Insert、粘贴 Ctrl+V / Shift+Insert（Ctrl+C 全局保留为退出）。
    /// </summary>
    public static TuiWindow InputLine(string title, string prompt, string defaultValue,
        Action<string> onConfirm, Action? onCancel = null)
    {
        const int maxPromptLines = 5;

        var displayTitle = string.IsNullOrEmpty(title) || title == "输入" ? "请输入" : title;
        var win = NewDialog(displayTitle, TuiTheme.Current.DialogInfoBorder, WideXScale);
        win.MinHeight = 6;

        int cw = ContentW(WideXScale, 2);

        var promptLines = TuiHelper.WrapText(prompt, cw, maxPromptLines);

        var vbox = new TuiVBox { Width = cw, ChildHAlign = HAlign.Center };
        foreach (var line in promptLines)
            vbox.Add(new TuiLabel(line) { Width = cw, Fg = TuiColors.Black });

        var hist = TuiInputHistory.Get(title);
        var initVal = !string.IsNullOrEmpty(defaultValue) ? defaultValue
            : hist.Count > 0 ? hist[0] : "";

        var input = new TuiInput
        {
            Text = initVal,
            CursorPos = initVal.Length,
            Width = cw, Height = 1,
            Fg = TuiColors.White, Bg = TuiColors.BgBlack,
            Focused = true,
        };
        vbox.Add(input);

        vbox.Add(new TuiLabel("") { Height = 1 }); // spacer

        var hbox = new TuiHBox { Width = cw, Spacing = 2, ContentHAlign = HAlign.Center };
        var okBtn = new TuiButton("确定") { Flex = 1 };
        var cancelBtn = new TuiButton("取消") { Flex = 1 };
        okBtn.OnClick = _ =>
        {
            var text = input.Text;
            win.Result = text;
            if (!string.IsNullOrWhiteSpace(text)) TuiInputHistory.Add(title, text);
            onConfirm(text);
            win.OnClosed?.Invoke();
        };
        cancelBtn.OnClick = _ => { win.Result = null; onCancel?.Invoke(); win.OnClosed?.Invoke(); };
        hbox.Add(okBtn); hbox.Add(cancelBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, okBtn, cancelBtn);
        vbox.Add(hbox);

        win.RootView = vbox;
        ApplyGradient(win, TuiTheme.Current.GradOrangeYellow);
        return win;
    }

    // ═══════════════════════════════════════════════════════════════
    // 密码输入对话框
    // ═══════════════════════════════════════════════════════════════

    /// <summary>密码/密钥输入对话框 —— 字符显示为 • 掩码</summary>
    public static TuiWindow Secret(string title, string prompt, string defaultValue,
        Action<string> onConfirm, Action? onCancel = null)
    {
        const int maxPromptLines = 5;

        var displayTitle = string.IsNullOrEmpty(title) || title == "输入密钥" ? "请输入" : title;
        var win = NewDialog(displayTitle, TuiTheme.Current.DialogInfoBorder, NarrowXScale);
        win.MinHeight = 6;

        int cw = ContentW(NarrowXScale, 4);

        var promptLines = TuiHelper.WrapText(prompt, cw, maxPromptLines);

        var vbox = new TuiVBox { Width = cw, ChildHAlign = HAlign.Center };
        foreach (var line in promptLines)
            vbox.Add(new TuiLabel(line) { Width = cw, Fg = TuiColors.Black });

        var input = new TuiInput
        {
            Text = defaultValue,
            CursorPos = defaultValue.Length,
            Width = cw, Focused = true, Password = true
        };
        vbox.Add(input);

        vbox.Add(new TuiLabel("") { Height = 1 }); // spacer

        var hbox = new TuiHBox { Width = cw, Spacing = 2, ContentHAlign = HAlign.Center };
        var okBtn = new TuiButton("确定") { Flex = 1 };
        var cancelBtn = new TuiButton("取消") { Flex = 1 };
        okBtn.OnClick = _ => { win.Result = input.Text; onConfirm(input.Text); win.OnClosed?.Invoke(); };
        cancelBtn.OnClick = _ => { win.Result = null; onCancel?.Invoke(); win.OnClosed?.Invoke(); };
        hbox.Add(okBtn); hbox.Add(cancelBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, okBtn, cancelBtn);
        vbox.Add(hbox);

        win.RootView = vbox;
        ApplyGradient(win, TuiTheme.Current.GradOrangeYellow);
        return win;
    }

    // ═══════════════════════════════════════════════════════════════
    // 列表选择对话框
    // ═══════════════════════════════════════════════════════════════

    /// <summary>单选列表对话框</summary>
    public static TuiWindow Select(string title, List<string> items,
        Action<int> onSelect, Action? onCancel = null)
    {
        var win = NewDialog(title, TuiTheme.Current.DialogInfoBorder, NarrowXScale);

        int cw = ContentW(NarrowXScale, 2);
        var visItems = Math.Min(items.Count, 12);

        var vbox = new TuiVBox { Width = cw };
        var list = new TuiList
        {
            Items = items,
            SelectedIndex = 0,
            Width = cw, Height = visItems, Focused = true
        };
        list.OnSelect = idx => { win.Result = idx; onSelect(idx); win.OnClosed?.Invoke(); };
        vbox.Add(list);

        var hbox = new TuiHBox { Width = cw, Spacing = 2, ContentHAlign = HAlign.Center };
        var cancelBtn = new TuiButton("取消 (Esc)") { Flex = 1 };
        cancelBtn.OnClick = _ => { win.Result = -1; onCancel?.Invoke(); win.OnClosed?.Invoke(); };
        ApplyButtonGradient(TuiTheme.Current.BtnCyanBlue, cancelBtn);
        hbox.Add(cancelBtn);
        vbox.Add(hbox);

        win.RootView = vbox;
        ApplyGradient(win, TuiTheme.Current.GradCyanBlue);
        return win;
    }

    /// <summary>多选列表对话框</summary>
    public static TuiWindow MultiSelect(string title, List<string> items,
        Action<HashSet<int>> onConfirm, Action? onCancel = null)
    {
        var win = NewDialog(title, TuiTheme.Current.DialogInfoBorder, NarrowXScale);

        int cw = ContentW(NarrowXScale, 2);
        var visItems = Math.Min(items.Count, 12);

        var vbox = new TuiVBox { Width = cw };
        var list = new TuiList
        {
            Items = items, SelectedIndex = 0, MultiSelect = true,
            Width = cw, Height = visItems, Focused = true
        };
        vbox.Add(list);

        var hbox = new TuiHBox { Width = cw, Spacing = 2, ContentHAlign = HAlign.Center };
        var okBtn = new TuiButton("确定") { Flex = 1 };
        var cancelBtn = new TuiButton("取消") { Flex = 1 };
        okBtn.OnClick = _ => { win.Result = list.CheckedIndices; onConfirm(list.CheckedIndices); win.OnClosed?.Invoke(); };
        cancelBtn.OnClick = _ => { win.Result = null; onCancel?.Invoke(); win.OnClosed?.Invoke(); };
        hbox.Add(okBtn); hbox.Add(cancelBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnCyanBlue, okBtn, cancelBtn);
        vbox.Add(hbox);

        win.RootView = vbox;
        ApplyGradient(win, TuiTheme.Current.GradCyanBlue);
        return win;
    }

    // ═══════════════════════════════════════════════════════════════
    // 权限确认对话框
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 权限确认对话框 —— 黄色警告背景 + 黑色文字 + 粗体标题 + 淡蓝按钮。
    /// 快捷键：Y=允许 N=拒绝 A=全部允许
    /// </summary>
    public static TuiWindow Permission(string title, string message, Action<DialogResult> onResult)
    {
        const int warnBg = TuiColors.BgYellow;
        const int blackFg = TuiColors.Black;
        const int btnBg = TuiColors.BgCyan;
        const int btnFocusBg = TuiColors.BgWhite;

        var win = new TuiWindow
        {
            Title = title,
            TitleBold = true,
            TitleFg = blackFg,
            TitleBg = warnBg,
            ShowTitleSeparator = false,
            Modal = true, HasMask = true,
            BorderColor = TuiColors.Yellow,
            Border = WindowBorder.Solid,
            WinBg = warnBg,
            XScale = WideXScale,
            WindowHAlign = HAlign.Center,
            WindowVAlign = VAlign.Middle,
            MinWidth = MinDialogW,
            MinHeight = 5,
        };

        int cw = ContentW(WideXScale, 4);
        var msgLabels = BuildMessageLabels(message, cw, blackFg);

        var vbox = new TuiVBox { Width = cw, ChildHAlign = HAlign.Center };
        foreach (var lbl in msgLabels)
        {
            lbl.Fg = blackFg;
            vbox.Add(lbl);
        }
        vbox.Add(new TuiLabel("") { Height = 1 }); // spacer

        var hbox = new TuiHBox { Width = cw, Spacing = 2, ContentHAlign = HAlign.Center };
        var yesBtn = MakePermBtn("允许 (Y)", 1, DialogResult.Yes);
        var noBtn = MakePermBtn("拒绝 (N)", 1, DialogResult.No);
        var allBtn = MakePermBtn("全允 (A)", 1, DialogResult.Ok);
        yesBtn.Focused = true;
        hbox.Add(yesBtn); hbox.Add(noBtn); hbox.Add(allBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, yesBtn, noBtn, allBtn);
        vbox.Add(hbox);

        win.RootView = vbox;
        ApplyGradient(win, TuiTheme.Current.GradOrangeYellow);

        win.RegisterShortcut(ConsoleKey.Y, () => { win.Result = DialogResult.Yes; onResult(DialogResult.Yes); win.OnClosed?.Invoke(); });
        win.RegisterShortcut(ConsoleKey.N, () => { win.Result = DialogResult.No; onResult(DialogResult.No); win.OnClosed?.Invoke(); });
        win.RegisterShortcut(ConsoleKey.A, () => { win.Result = DialogResult.Ok; onResult(DialogResult.Ok); win.OnClosed?.Invoke(); });
        return win;

        TuiButton MakePermBtn(string text, int flex, DialogResult result)
        {
            var b = new TuiButton(text)
            {
                Flex = flex,
                Fg = blackFg, Bg = btnBg,
                FocusedFg = blackFg, FocusedBg = btnFocusBg
            };
            b.OnClick = _ => { win.Result = result; onResult(result); win.OnClosed?.Invoke(); };
            return b;
        }
    }
}
