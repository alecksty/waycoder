using WayCoder.Terminal;
using WayCoder.UI.TuiControls;

namespace WayCoder.UI.TuiControls;

/// <summary>
/// 对话框工具集 —— 常用对话框的便捷工厂方法。
/// 所有对话框基于 TuiWindow + 控件树构建。
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

    /// <summary>对话框最大宽度比例（分子/分母 = 终端宽度的 3/4）</summary>
    private const int MaxWidthNum = 3;

    private const int MaxWidthDen = 4;

    /// <summary>对话框最小消息区宽度</summary>
    private const int MinMsgWidth = 24;

    /// <summary>计算消息区最大宽度（终端宽度的 3/4 减去边框和边距）</summary>
    private static int CalcMaxMsgWidth() => Math.Max(MinMsgWidth, Tty.Cols * MaxWidthNum / MaxWidthDen - 12);

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

    /// <summary>统一按钮宽度为最宽者的宽度</summary>
    private static void NormalizeButtons(params TuiButton[] buttons)
    {
        if (buttons.Length == 0) return;
        int maxW = buttons.Max(b => b.Width);
        foreach (var b in buttons) b.Width = maxW;
    }

    /// <summary>给按钮启用渐变背景。焦点/非焦点的视觉差异由 ControlRenderer 渲染时动态处理。</summary>
    /// <param name="grad">渐变颜色对</param>
    /// <param name="buttons">要启用渐变背景的按钮列表</param>
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

    // ── 消息框 ──

    /// <summary>信息提示框（单"确定"按钮）</summary>
    public static TuiWindow Info(string title, string message)
    {
        var win = new TuiWindow
        {
            Title = title,
            ShowTitleSeparator = false,
            Modal = true, HasMask = true, BorderColor = TuiTheme.Current.DialogInfoBorder,
            Border = WindowBorder.Solid,
            WinBg = TuiTheme.Current.WindowBg,
        };
        BuildContent(win, message, ("确定", _ => win.OnClosed?.Invoke()), TuiTheme.Current.BtnCyanBlue);
        ApplyGradient(win, TuiTheme.Current.GradCyanBlue);
        return win;
    }

    /// <summary>成功提示框（绿色边框）</summary>
    public static TuiWindow Success(string title, string message)
    {
        var win = Info(title, message);
        win.BorderColor = TuiTheme.Current.DialogSuccessBorder;
        ApplyGradient(win, TuiTheme.Current.GradGreenCyan);
        var btn = (TuiButton)((TuiVBox)win.RootView).Children.Last();
        ApplyButtonGradient(TuiTheme.Current.BtnGreenCyan, btn);
        return win;
    }

    /// <summary>警告提示框（黄色边框）</summary>
    public static TuiWindow Warn(string title, string message)
    {
        var win = Info(title, message);
        win.BorderColor = TuiTheme.Current.DialogWarnBorder;
        ApplyGradient(win, TuiTheme.Current.GradOrangeYellow);
        var btn = (TuiButton)((TuiVBox)win.RootView).Children.Last();
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, btn);
        return win;
    }

    /// <summary>错误提示框（红色边框）</summary>
    public static TuiWindow Error(string title, string message)
    {
        var win = Info(title, message);
        win.BorderColor = TuiTheme.Current.DialogErrorBorder;
        ApplyGradient(win, TuiTheme.Current.GradRedOrange);
        var btn = (TuiButton)((TuiVBox)win.RootView).Children.Last();
        ApplyButtonGradient(TuiTheme.Current.BtnRedOrange, btn);
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
            Border = WindowBorder.Solid,
            WinBg = TuiTheme.Current.WindowBg,
        };

        int maxMsgW = CalcMaxMsgWidth();
        var msgLabels = BuildMessageLabels(message, maxMsgW);
        int maxVw = msgLabels.Count > 0 ? msgLabels.Max(l => TuiHelper.DisplayWidth(l.Text)) : 10;
        int w = Math.Clamp(Math.Max(30, maxVw + 6), 30, Tty.Cols * MaxWidthNum / MaxWidthDen);
        int labelW = w - 6;

        var vbox = new TuiVBox { Width = w - 4 };
        foreach (var lbl in msgLabels)
        {
            lbl.Width = labelW;
            vbox.Add(lbl);
        }

        vbox.Add(new TuiLabel("") { Height = 1 });

        var hbox = new TuiHBox { Spacing = 2, Width = w - 6, ContentHAlign = HAlign.Center };
        var yesBtn = new TuiButton("是 (Y)") { Width = 12, Focused = true };
        var noBtn = new TuiButton("否 (N)") { Width = 12 };
        yesBtn.OnClick = _ =>
        {
            win.Result = true;
            onResult(true);
            win.OnClosed?.Invoke();
        };
        noBtn.OnClick = _ =>
        {
            win.Result = false;
            onResult(false);
            win.OnClosed?.Invoke();
        };
        hbox.Add(yesBtn);
        hbox.Add(noBtn);
        NormalizeButtons(yesBtn, noBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, yesBtn, noBtn);
        vbox.Add(hbox);

        vbox.Layout();
        win.Width = vbox.Width + 4;
        win.Height = vbox.Height + 3;
        win.RootView = vbox;
        win.Center();
        ApplyGradient(win, TuiTheme.Current.GradOrangeYellow);

        win.RegisterShortcut(ConsoleKey.Y, () =>
        {
            win.Result = true;
            onResult(true);
            win.OnClosed?.Invoke();
        });
        win.RegisterShortcut(ConsoleKey.N, () =>
        {
            win.Result = false;
            onResult(false);
            win.OnClosed?.Invoke();
        });
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
            Border = WindowBorder.Solid,
            WinBg = TuiTheme.Current.WindowBg,
        };

        int maxMsgW = CalcMaxMsgWidth();
        var msgLabels = BuildMessageLabels(message, maxMsgW);
        int maxVw = msgLabels.Count > 0 ? msgLabels.Max(l => TuiHelper.DisplayWidth(l.Text)) : 10;
        int w = Math.Clamp(Math.Max(30, maxVw + 6), 30, Tty.Cols * MaxWidthNum / MaxWidthDen);
        int labelW = w - 6;

        var vbox = new TuiVBox { Width = w - 4 };
        foreach (var lbl in msgLabels)
        {
            lbl.Width = labelW;
            vbox.Add(lbl);
        }

        vbox.Add(new TuiLabel("") { Height = 1 });

        var hbox = new TuiHBox { Spacing = 2, Width = w - 6, ContentHAlign = HAlign.Center };
        var yesBtn = new TuiButton("是 (Y)") { Width = 12, Focused = true };
        var noBtn = new TuiButton("否 (N)") { Width = 12 };
        var cancelBtn = new TuiButton("取消 (Esc)") { Width = 14 };
        yesBtn.OnClick = _ =>
        {
            win.Result = DialogResult.Yes;
            onResult(DialogResult.Yes);
            win.OnClosed?.Invoke();
        };
        noBtn.OnClick = _ =>
        {
            win.Result = DialogResult.No;
            onResult(DialogResult.No);
            win.OnClosed?.Invoke();
        };
        cancelBtn.OnClick = _ =>
        {
            win.Result = DialogResult.Cancel;
            onResult(DialogResult.Cancel);
            win.OnClosed?.Invoke();
        };
        hbox.Add(yesBtn);
        hbox.Add(noBtn);
        hbox.Add(cancelBtn);
        NormalizeButtons(yesBtn, noBtn, cancelBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, yesBtn, noBtn, cancelBtn);
        vbox.Add(hbox);

        vbox.Layout();
        win.Width = vbox.Width + 4;
        win.Height = vbox.Height + 3;
        win.RootView = vbox;
        win.Center();
        ApplyGradient(win, TuiTheme.Current.GradOrangeYellow);

        win.RegisterShortcut(ConsoleKey.Y, () =>
        {
            win.Result = DialogResult.Yes;
            onResult(DialogResult.Yes);
            win.OnClosed?.Invoke();
        });
        win.RegisterShortcut(ConsoleKey.N, () =>
        {
            win.Result = DialogResult.No;
            onResult(DialogResult.No);
            win.OnClosed?.Invoke();
        });
        return win;
    }

    // ── 输入对话框 ──

    /// <summary>文本输入对话框（多行 TuiTextArea + 历史）</summary>
    public static TuiWindow Input(string title, string prompt, string defaultValue,
        Action<string> onConfirm, Action? onCancel = null)
    {
        const int inputHeight = 5;
        const int maxPromptLines = 5;

        // 标题默认"请输入"
        var displayTitle = string.IsNullOrEmpty(title) || title == "输入" ? "请输入" : title;

        var win = new TuiWindow
        {
            Title = displayTitle,
            ShowTitleSeparator = false,
            Modal = true, 
            HasMask = true, 
            BorderColor = TuiTheme.Current.DialogInfoBorder,
            Border = WindowBorder.Solid,
            WinBg = TuiTheme.Current.WindowBg,
        };

        // 最大宽度 2/3 屏宽
        int maxW = Math.Max(32, Tty.Cols * 3 / 4);
        int maxContentW = maxW - 2; // 2 边框 + 2 边距（各 1 字符）

        // 提示文本折行（最多 5 行，黑字）
        var promptLines = TuiHelper.WrapText(prompt, maxContentW, maxPromptLines);
        int maxLineVw = promptLines.Count > 0 ? promptLines.Max(l => TuiHelper.DisplayWidth(l)) : 10;

        // 自适应宽度
        int w = Math.Clamp(Math.Max(maxLineVw + 4, 40), 30, maxW);
        int cw = w - 2; // 内容区宽度（左右各 1 字符边距）

        // ChildHAlign=Center: TuiWindow.OnCreate 会将 vbox.Width 覆写为 ContentWidth=win.Width-2=cw+2，
        // 子控件比 vbox 窄 2 字符，居中后自然产生左右各 1 字符边距
        var vbox = new TuiVBox { Width = cw, ChildHAlign = HAlign.Center };
        foreach (var line in promptLines)
        {
            vbox.Add(new TuiLabel(line) {
                Margin = new EdgeInsets(1),
                Width = cw, 
                Fg = TuiColors.Black 
            });
        }

        // 多行 TuiTextArea
        var input = new TuiTextArea
        {
            Margin = new EdgeInsets(0, 1, 0, 1),
            Width = cw, Height = inputHeight,
            Fg = TuiColors.White,
            Bg = TuiColors.BgBlack,
            Focused = true,
            //Placeholder = "输入... (Ctrl+Enter 换行)",
        };
        var hist = TuiInputHistory.Get(title);
        var initVal = !string.IsNullOrEmpty(defaultValue) ? defaultValue
            : hist.Count > 0 ? hist[0] : "";
        
        if (!string.IsNullOrEmpty(initVal))
            input.Text = initVal;

        vbox.Add(input);

        // 按钮与输入区空一行
        vbox.Add(new TuiLabel("") { Height = 1 });

        var hbox = new TuiHBox { Spacing = 2, Width = cw, ContentHAlign = HAlign.Center };
        var okBtn = new TuiButton("确定") { Width = 10 };
        var cancelBtn = new TuiButton("取消") { Width = 10 };
        okBtn.OnClick = _ =>
        {
            var text = input.Text;
            win.Result = text;
            if (!string.IsNullOrWhiteSpace(text)) TuiInputHistory.Add(title, text);
            onConfirm(text);
            win.OnClosed?.Invoke();
        };
        cancelBtn.OnClick = _ =>
        {
            win.Result = null;
            onCancel?.Invoke();
            win.OnClosed?.Invoke();
        };
        hbox.Add(okBtn);
        hbox.Add(cancelBtn);
        NormalizeButtons(okBtn, cancelBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, okBtn, cancelBtn);
        vbox.Add(hbox);

        vbox.Layout();
        win.Width = vbox.Width + 4;
        win.Height = vbox.Height + 3;
        win.RootView = vbox;
        win.Center();
        ApplyGradient(win, TuiTheme.Current.GradOrangeYellow);
        return win;
    }

    // ── 密码输入对话框 ──

    /// <summary>密码/密钥输入对话框 —— 字符显示为 • 掩码</summary>
    public static TuiWindow Secret(string title, string prompt, string defaultValue,
        Action<string> onConfirm, Action? onCancel = null)
    {
        const int maxPromptLines = 5;

        // 标题默认"请输入"
        var displayTitle = string.IsNullOrEmpty(title) || title == "输入密钥" ? "请输入" : title;

        var win = new TuiWindow
        {
            Title = displayTitle,
            ShowTitleSeparator = false,
            Modal = true, HasMask = true, BorderColor = TuiTheme.Current.DialogInfoBorder,
            Border = WindowBorder.Solid,
            WinBg = TuiTheme.Current.WindowBg,
        };

        // 最大宽度 2/3 屏宽
        int maxW = Math.Max(30, Tty.Cols * 2 / 3);
        int maxContentW = maxW - 4;

        // 提示文本折行（最多 5 行，黑字）
        var promptLines = TuiHelper.WrapText(prompt, maxContentW, maxPromptLines);
        int maxLineVw = promptLines.Count > 0 ? promptLines.Max(l => TuiHelper.DisplayWidth(l)) : 10;
        int inputMinW = Math.Max(20, defaultValue.Length + 4);

        // 自适应宽度
        int w = Math.Clamp(Math.Max(Math.Max(maxLineVw, inputMinW) + 4, 30), 30, maxW);
        int cw = w - 4; // 内容区宽度（左右各 1 字符边距）

        // ChildHAlign=Center: TuiWindow.OnCreate 会将 vbox.Width 覆写为 ContentWidth=win.Width-2=cw+2，
        // 子控件比 vbox 窄 2 字符，居中后自然产生左右各 1 字符边距
        var vbox = new TuiVBox { Width = cw, ChildHAlign = HAlign.Center };
        foreach (var line in promptLines)
        {
            vbox.Add(new TuiLabel(line) { Width = cw, Fg = TuiColors.Black });
        }

        var input = new TuiInput
        {
            Text = defaultValue, CursorPos = defaultValue.Length,
            Width = cw, Focused = true, Password = true
        };
        vbox.Add(input);

        // 按钮与输入区空一行
        vbox.Add(new TuiLabel("") { Height = 1 });

        var hbox = new TuiHBox { Spacing = 2, Width = cw, ContentHAlign = HAlign.Center };
        var okBtn = new TuiButton("确定") { Width = 10 };
        var cancelBtn = new TuiButton("取消") { Width = 10 };
        okBtn.OnClick = _ =>
        {
            win.Result = input.Text;
            onConfirm(input.Text);
            win.OnClosed?.Invoke();
        };
        cancelBtn.OnClick = _ =>
        {
            win.Result = null;
            onCancel?.Invoke();
            win.OnClosed?.Invoke();
        };
        hbox.Add(okBtn);
        hbox.Add(cancelBtn);
        NormalizeButtons(okBtn, cancelBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, okBtn, cancelBtn);
        vbox.Add(hbox);

        vbox.Layout();
        win.Width = vbox.Width + 4;
        win.Height = vbox.Height + 3;
        win.RootView = vbox;
        win.Center();
        ApplyGradient(win, TuiTheme.Current.GradOrangeYellow);
        return win;
    }

    // ── 列表选择对话框 ──

    /// <summary>单选列表对话框</summary>
    public static TuiWindow Select(string title, List<string> items,
        Action<int> onSelect, Action? onCancel = null)
    {
        var visItems = Math.Min(items.Count, 12);
        var maxVw = items.Count > 0 ? items.Max(i => TuiHelper.DisplayWidth(i)) : 10;
        var listW = Math.Clamp(Math.Max(20, maxVw + 6), 20, Tty.Cols * MaxWidthNum / MaxWidthDen);

        var win = new TuiWindow
        {
            Title = title,
            ShowTitleSeparator = false,
            Modal = true, HasMask = true, BorderColor = TuiTheme.Current.DialogInfoBorder,
            Border = WindowBorder.Solid,
        };

        var vbox = new TuiVBox { Width = listW };
        var list = new TuiList
        {
            Items = items, SelectedIndex = 0,
            Width = listW, Height = visItems, Focused = true
        };
        list.OnSelect = idx =>
        {
            win.Result = idx;
            onSelect(idx);
            win.OnClosed?.Invoke();
        };
        vbox.Add(list);

        var hbox = new TuiHBox { Spacing = 2, Width = listW - 2, ContentHAlign = HAlign.Center };
        var cancelBtn = new TuiButton("取消 (Esc)") { Width = 14 };
        cancelBtn.OnClick = _ =>
        {
            win.Result = -1;
            onCancel?.Invoke();
            win.OnClosed?.Invoke();
        };
        ApplyButtonGradient(TuiTheme.Current.BtnCyanBlue, cancelBtn);
        hbox.Add(cancelBtn);
        vbox.Add(hbox);

        vbox.Layout();
        win.Width = vbox.Width + 4;
        win.Height = vbox.Height + 3;
        win.RootView = vbox;
        win.Center();
        ApplyGradient(win, TuiTheme.Current.GradCyanBlue);
        return win;
    }

    /// <summary>多选列表对话框</summary>
    public static TuiWindow MultiSelect(string title, List<string> items,
        Action<HashSet<int>> onConfirm, Action? onCancel = null)
    {
        var visItems = Math.Min(items.Count, 12);
        var maxVw = items.Count > 0 ? items.Max(i => TuiHelper.DisplayWidth(i)) : 10;
        var listW = Math.Clamp(Math.Max(24, maxVw + 6), 24, Tty.Cols * MaxWidthNum / MaxWidthDen);

        var win = new TuiWindow
        {
            Title = title,
            ShowTitleSeparator = false,
            Modal = true, HasMask = true, BorderColor = TuiTheme.Current.DialogInfoBorder,
            Border = WindowBorder.Solid,
        };

        var vbox = new TuiVBox { Width = listW };
        var list = new TuiList
        {
            Items = items, SelectedIndex = 0, MultiSelect = true,
            Width = listW, Height = visItems, Focused = true
        };
        vbox.Add(list);

        var hbox = new TuiHBox { Spacing = 2, Width = listW - 2, ContentHAlign = HAlign.Center };
        var okBtn = new TuiButton("确定") { Width = 10 };
        var cancelBtn = new TuiButton("取消") { Width = 10 };
        okBtn.OnClick = _ =>
        {
            win.Result = list.CheckedIndices;
            onConfirm(list.CheckedIndices);
            win.OnClosed?.Invoke();
        };
        cancelBtn.OnClick = _ =>
        {
            win.Result = null;
            onCancel?.Invoke();
            win.OnClosed?.Invoke();
        };
        hbox.Add(okBtn);
        hbox.Add(cancelBtn);
        NormalizeButtons(okBtn, cancelBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnCyanBlue, okBtn, cancelBtn);
        vbox.Add(hbox);

        vbox.Layout();
        win.Width = vbox.Width + 4;
        win.Height = vbox.Height + 3;
        win.RootView = vbox;
        win.Center();
        ApplyGradient(win, TuiTheme.Current.GradCyanBlue);
        return win;
    }

    // ── 权限确认对话框 ──

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
            Modal = true,
            HasMask = true,
            BorderColor = TuiColors.Yellow,
            Border = WindowBorder.Solid,
            WinBg = warnBg,
        };

        //  消息标签宽度
        var maxMsgW = CalcMaxMsgWidth();
        //  生成标签
        var msgLabels = BuildMessageLabels(message, maxMsgW, blackFg);
        //  最大标签宽度
        var maxVw = msgLabels.Count > 0 ? msgLabels.Max(l => TuiHelper.DisplayWidth(l.Text)) : 10;

        // 3 个按钮: 14+14+18 + spacing*2 = 50
        var w = Math.Clamp(Math.Max(62, maxVw + 10), 30, Tty.Cols * MaxWidthNum / MaxWidthDen);
        var labelW = w - 6;

        var vbox = new TuiVBox { Width = w - 4 };
        foreach (var lbl in msgLabels)
        {
            lbl.Width = labelW;
            lbl.Fg = blackFg;
            vbox.Add(lbl);
        }

        vbox.Add(new TuiLabel("") { Height = 1 });

        var hbox = new TuiHBox { Spacing = 2, Width = w - 4, ContentHAlign = HAlign.Center };

        var yesBtn = MakeBtn("允许 (Y)", 14);
        var noBtn = MakeBtn("拒绝 (N)", 14);
        var allBtn = MakeBtn("全允 (A)", 14);
        yesBtn.Focused = true;
        yesBtn.OnClick = _ =>
        {
            win.Result = DialogResult.Yes;
            onResult(DialogResult.Yes);
            win.OnClosed?.Invoke();
        };
        noBtn.OnClick = _ =>
        {
            win.Result = DialogResult.No;
            onResult(DialogResult.No);
            win.OnClosed?.Invoke();
        };
        allBtn.OnClick = _ =>
        {
            win.Result = DialogResult.Ok;
            onResult(DialogResult.Ok);
            win.OnClosed?.Invoke();
        };
        hbox.Add(yesBtn);
        hbox.Add(noBtn);
        hbox.Add(allBtn);
        //  一按钮宽度
        NormalizeButtons(yesBtn, noBtn, allBtn);
        //  按钮渐变
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, yesBtn, noBtn, allBtn);
        vbox.Add(hbox);

        vbox.Layout();
        win.Width = vbox.Width + 4;
        win.Height = vbox.Height + 3;
        win.RootView = vbox;
        win.Center();
        ApplyGradient(win, TuiTheme.Current.GradOrangeYellow);

        win.RegisterShortcut(ConsoleKey.Y, () =>
        {
            win.Result = DialogResult.Yes;
            onResult(DialogResult.Yes);
            win.OnClosed?.Invoke();
        });
        win.RegisterShortcut(ConsoleKey.N, () =>
        {
            win.Result = DialogResult.No;
            onResult(DialogResult.No);
            win.OnClosed?.Invoke();
        });
        win.RegisterShortcut(ConsoleKey.A, () =>
        {
            win.Result = DialogResult.Ok;
            onResult(DialogResult.Ok);
            win.OnClosed?.Invoke();
        });
        
        return win;

        TuiButton MakeBtn(string text, int wd)
        {
            var b = new TuiButton(text)
            {
                Width = wd,
                Fg = blackFg,
                Bg = btnBg,
                FocusedFg = blackFg,
                FocusedBg = btnFocusBg
            };
            return b;
        }
    }

    // ── 内部工具 ──

    /// <summary>单按钮消息框通用构建（Info/Success/Warn/Error 共用）</summary>
    private static void BuildContent(TuiWindow win, string message,
        (string label, Action<TuiButton> onClick) button,
        (int start, int end)? grad = null)
    {
        int maxMsgW = CalcMaxMsgWidth();
        var msgLabels = BuildMessageLabels(message, maxMsgW);
        int maxVw = msgLabels.Count > 0 ? msgLabels.Max(l => TuiHelper.DisplayWidth(l.Text)) : 10;
        int w = Math.Clamp(Math.Max(30, maxVw + 6), 30, Tty.Cols * MaxWidthNum / MaxWidthDen);
        int labelW = w - 6;

        var vbox = new TuiVBox { Width = w - 4 };
        foreach (var lbl in msgLabels)
        {
            lbl.Width = labelW;
            vbox.Add(lbl);
        }

        vbox.Add(new TuiLabel("") { Height = 1 });

        var btn = new TuiButton(button.label)
        {
            Width = Math.Max(8, TuiHelper.DisplayWidth(button.label) + 4),
            Focused = true
        };
        if (grad.HasValue) ApplyButtonGradient(grad.Value, btn);
        btn.OnClick = _ =>
        {
            win.Result = DialogResult.Ok;
            button.onClick(btn);
        };
        vbox.Add(btn);

        vbox.Layout();
        win.Width = vbox.Width + 4;
        win.Height = vbox.Height + 3;
        win.RootView = vbox;
        win.Center();

        win.RegisterShortcut(ConsoleKey.Enter, () =>
        {
            win.Result = DialogResult.Ok;
            button.onClick(btn);
        });
    }
}