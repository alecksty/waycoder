using WayCoder.UI.Shared.Terminal;

using WayCoder.UI.Tui.Edit;
using WayCoder.UI.Shared;

namespace WayCoder.UI.Tui.Controls;

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
    private const double WideXScale = 0.75;   // 宽对话框：最多占屏幕 3/4
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
    /// 最多 MaxMessageLines() 行，超出行尾显示 "…"。
    /// </summary>
    private static List<TuiLabel> BuildMessageLabels(string message, int labelWidth, int? fg = null)
    {
        var lines = TuiHelper.WrapText(message, labelWidth, MaxMessageLines());
        return lines.Select(line => new TuiLabel(line)
        {
            Width = labelWidth,
            Fg = fg ?? 0,
            TextAlign = HAlign.Center
        }).ToList();
    }

    /// <summary>
    /// 消息框固定消息行数。消息框改为固定高度：窗口总高固定 7 行
    /// （上下边框 2 行 + 内容区 5 行），内容区 = 消息(最多 4 行) + 按钮(1 行)。
    /// 标题嵌在顶部边框上（不独占行），故内容区正好 5 行。
    /// </summary>
    private static int MaxMessageLines() => 4;

    /// <summary>
    /// 固定窗口高度为 7（边框 2 + 内容区 5）。不再随消息行数动态伸缩——
    /// 动态高度导致每次内容变化都要重算布局并整窗重绘，易产生残影。
    /// </summary>
    private static void FitHeight(TuiWindow win, int msgLines)
    {
        win.Height = 7;
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
            Border = WindowBorder.Rounded, // 与主界面圆角细线统一（原 Solid 实心块很突兀）
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
        win.TitleBold = true; // 标题独占一行（粗体）
        int cw = ContentW(DefaultXScale, 4);
        var msgLabels = BuildMessageLabels(message, cw);
        FitHeight(win, msgLabels.Count);

        var vbox = new TuiVBox { Width = cw, ChildHAlign = HAlign.Center };
        foreach (var lbl in msgLabels) vbox.Add(lbl);

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
        win.TitleBold = true; // 标题独占一行（粗体）

        int cw = ContentW(WideXScale, 4);
        var msgLabels = BuildMessageLabels(message, cw);
        FitHeight(win, msgLabels.Count);

        var vbox = new TuiVBox { Width = cw, ChildHAlign = HAlign.Center };
        foreach (var lbl in msgLabels) vbox.Add(lbl);

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
        win.RegisterShortcut(ConsoleKey.Escape, () => { win.Result = false; onResult(false); win.OnClosed?.Invoke(); });
        return win;
    }

    /// <summary>Yes/No/Cancel 三选确认框</summary>
    public static TuiWindow Confirm3(string title, string message, Action<DialogResult> onResult)
    {
        var win = NewDialog(title, TuiTheme.Current.DialogConfirmBorder, WideXScale);
        win.TitleBold = true; // 标题独占一行（粗体）

        int cw = ContentW(WideXScale, 4);
        var msgLabels = BuildMessageLabels(message, cw);
        FitHeight(win, msgLabels.Count);

        var vbox = new TuiVBox { Width = cw, ChildHAlign = HAlign.Center };
        foreach (var lbl in msgLabels) vbox.Add(lbl);

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
        win.RegisterShortcut(ConsoleKey.Escape, () => { win.Result = DialogResult.Cancel; onResult(DialogResult.Cancel); win.OnClosed?.Invoke(); });
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
            vbox.Add(new TuiLabel(line) { Width = cw, Fg = TuiTheme.Current.ControlFg });

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
        win.RegisterShortcut(ConsoleKey.Escape, () => { win.Result = null; onCancel?.Invoke(); win.OnClosed?.Invoke(); });
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
            vbox.Add(new TuiLabel(line) { Width = cw, Fg = TuiTheme.Current.ControlFg });

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
        void SubmitInput()
        {
            var text = input.Text;
            win.Result = text;
            if (!string.IsNullOrWhiteSpace(text)) TuiInputHistory.Add(title, text);
            onConfirm(text);
            win.OnClosed?.Invoke();
        }
        okBtn.OnClick = _ => SubmitInput();
        input.OnSubmit = _ => SubmitInput(); // 单行输入框回车 = 确定
        cancelBtn.OnClick = _ => { win.Result = null; onCancel?.Invoke(); win.OnClosed?.Invoke(); };
        hbox.Add(okBtn); hbox.Add(cancelBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, okBtn, cancelBtn);
        vbox.Add(hbox);

        win.RootView = vbox;
        ApplyGradient(win, TuiTheme.Current.GradOrangeYellow);
        win.RegisterShortcut(ConsoleKey.Escape, () => { win.Result = null; onCancel?.Invoke(); win.OnClosed?.Invoke(); });
        return win;
    }

    // ═══════════════════════════════════════════════════════════════
    // 查找/替换对话框
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 查找/替换对话框 —— 两个单行输入（查找/替换）+ 三个选项复选（区分大小写/正则/整词）+ 四个操作按钮。
    /// onFindNext(find, opts) 查找下一处；onReplace(find, repl, opts) 替换当前处；
    /// onReplaceAll(find, repl, opts) 全部替换；Esc/取消 关闭。
    /// </summary>
    public static TuiWindow FindReplace(string initialFind, string initialReplace, FindOptions initialOpts,
        Action<string, FindOptions> onFindNext, Action<string, string, FindOptions> onReplace,
        Action<string, string, FindOptions> onReplaceAll)
    {
        var win = NewDialog("查找/替换", TuiTheme.Current.DialogInfoBorder, WideXScale);
        win.MinHeight = 11;

        int cw = ContentW(WideXScale, 4);

        var vbox = new TuiVBox { Width = cw, ChildHAlign = HAlign.Left };

        // 查找行
        var findRow = new TuiHBox { Width = cw, Spacing = 1 };
        findRow.Add(new TuiLabel("查找:") { Width = 6, Fg = TuiTheme.Current.ControlFg });
        var findInput = new TuiInput
        {
            Text = initialFind,
            CursorPos = initialFind.Length,
            Flex = 1, Height = 1,
            Fg = TuiColors.White, Bg = TuiColors.BgBlack,
            Focused = true,
        };
        findRow.Add(findInput);
        vbox.Add(findRow);

        // 替换行
        var replRow = new TuiHBox { Width = cw, Spacing = 1 };
        replRow.Add(new TuiLabel("替换:") { Width = 6, Fg = TuiTheme.Current.ControlFg });
        var replInput = new TuiInput
        {
            Text = initialReplace,
            CursorPos = initialReplace.Length,
            Flex = 1, Height = 1,
            Fg = TuiColors.White, Bg = TuiColors.BgBlack,
        };
        replRow.Add(replInput);
        vbox.Add(replRow);

        // 选项行：区分大小写 / 正则 / 整词（Space/Enter 切换）
        var caseCb = new TuiCheckbox("区分大小写", initialOpts.CaseSensitive) { Fg = TuiTheme.Current.ControlFg };
        var regexCb = new TuiCheckbox("正则", initialOpts.UseRegex) { Fg = TuiTheme.Current.ControlFg };
        var wordCb = new TuiCheckbox("整词", initialOpts.WholeWord) { Fg = TuiTheme.Current.ControlFg };
        var optRow = new TuiHBox { Width = cw, Spacing = 3, ContentHAlign = HAlign.Left };
        optRow.Add(caseCb); optRow.Add(regexCb); optRow.Add(wordCb);
        vbox.Add(optRow);

        vbox.Add(new TuiLabel("") { Height = 1 }); // spacer

        FindOptions CurrentOpts() => new(caseCb.Checked, regexCb.Checked, wordCb.Checked);

        // 按钮行 1：查找下一个 / 替换
        var row1 = new TuiHBox { Width = cw, Spacing = 2, ContentHAlign = HAlign.Center };
        var findBtn = new TuiButton("查找下一个") { Flex = 1 };
        var replBtn = new TuiButton("替换") { Flex = 1 };
        findBtn.OnClick = _ => { onFindNext(findInput.Text, CurrentOpts()); win.OnClosed?.Invoke(); };
        replBtn.OnClick = _ => { onReplace(findInput.Text, replInput.Text, CurrentOpts()); win.OnClosed?.Invoke(); };
        row1.Add(findBtn); row1.Add(replBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnCyanBlue, findBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, replBtn);
        vbox.Add(row1);

        // 按钮行 2：全部替换 / 取消
        var row2 = new TuiHBox { Width = cw, Spacing = 2, ContentHAlign = HAlign.Center };
        var allBtn = new TuiButton("全部替换") { Flex = 1 };
        var cancelBtn = new TuiButton("取消 (Esc)") { Flex = 1 };
        allBtn.OnClick = _ => { onReplaceAll(findInput.Text, replInput.Text, CurrentOpts()); win.OnClosed?.Invoke(); };
        cancelBtn.OnClick = _ => win.OnClosed?.Invoke();
        row2.Add(allBtn); row2.Add(cancelBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnGreenCyan, allBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, cancelBtn);
        vbox.Add(row2);

        win.RootView = vbox;
        ApplyGradient(win, TuiTheme.Current.GradOrangeYellow);

        // 输入框回车 = 查找下一处 / 替换；F3 = 查找下一处；Esc = 关闭
        findInput.OnSubmit = _ => { onFindNext(findInput.Text, CurrentOpts()); win.OnClosed?.Invoke(); };
        replInput.OnSubmit = _ => { onReplace(findInput.Text, replInput.Text, CurrentOpts()); win.OnClosed?.Invoke(); };
        win.RegisterShortcut(ConsoleKey.F3, () => { onFindNext(findInput.Text, CurrentOpts()); win.OnClosed?.Invoke(); });
        win.RegisterShortcut(ConsoleKey.Escape, () => win.OnClosed?.Invoke());
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
            vbox.Add(new TuiLabel(line) { Width = cw, Fg = TuiTheme.Current.ControlFg });

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
        void SubmitInput() { win.Result = input.Text; onConfirm(input.Text); win.OnClosed?.Invoke(); }
        okBtn.OnClick = _ => SubmitInput();
        input.OnSubmit = _ => SubmitInput(); // 单行输入框回车 = 确定
        cancelBtn.OnClick = _ => { win.Result = null; onCancel?.Invoke(); win.OnClosed?.Invoke(); };
        hbox.Add(okBtn); hbox.Add(cancelBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, okBtn, cancelBtn);
        vbox.Add(hbox);

        win.RootView = vbox;
        ApplyGradient(win, TuiTheme.Current.GradOrangeYellow);
        win.RegisterShortcut(ConsoleKey.Escape, () => { win.Result = null; onCancel?.Invoke(); win.OnClosed?.Invoke(); });
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
        win.RegisterShortcut(ConsoleKey.Escape, () => { win.Result = -1; onCancel?.Invoke(); win.OnClosed?.Invoke(); });
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
        void Confirm()
        {
            win.Result = list.CheckedIndices;
            onConfirm(list.CheckedIndices);
            win.OnClosed?.Invoke();
        }
        okBtn.OnClick = _ => Confirm();
        cancelBtn.OnClick = _ => { win.Result = null; onCancel?.Invoke(); win.OnClosed?.Invoke(); };
        // 多选列表：空格勾选，Enter = 确认（等同点击“确定”按钮）
        list.OnSelect = _ => Confirm();
        hbox.Add(okBtn); hbox.Add(cancelBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnCyanBlue, okBtn, cancelBtn);
        vbox.Add(hbox);

        win.RootView = vbox;
        ApplyGradient(win, TuiTheme.Current.GradCyanBlue);
        win.RegisterShortcut(ConsoleKey.Escape, () => { win.Result = null; onCancel?.Invoke(); win.OnClosed?.Invoke(); });
        return win;
    }

    // ═══════════════════════════════════════════════════════════════
    // LLM 提问对话框（标题 + 消息 + 单选/多选按钮）
    // ═══════════════════════════════════════════════════════════════

    /// <summary>消息正文最多行数（超出末尾加省略号）</summary>
    private const int AskMaxMessageLines = 5;

    /// <summary>选项列表最多可见行数（超出滚动）</summary>
    private const int AskMaxListItems = 9;

    /// <summary>
    /// 提问对话框 —— 专门给 LLM（ask_user_question 工具）使用。
    /// 布局：标题独占一行（粗体，如诗名）→ 消息正文（1~5 行，超出省略号，如诗内容）
    /// → 选项列表（单选 ▶ 选中，多选 ☑/☐ 勾选，复用现有 TuiList）→ 底部操作按钮。
    /// 高度按内容精确计算，不留过度空白。
    /// </summary>
    /// <param name="title">标题（header/诗名，独占一行粗体）</param>
    /// <param name="message">消息正文（question/诗内容，最多 5 行，超出省略号）</param>
    /// <param name="options">选项标签列表（列表项文本）</param>
    /// <param name="multiSelect">true=多选（☑勾选+确定），false=单选（▶选中即确认）</param>
    /// <param name="onSelect">单选回调（选中索引）</param>
    /// <param name="onMultiConfirm">多选回调（选中索引集合）</param>
    /// <param name="onCancel">取消回调</param>
    public static TuiWindow Ask(string title, string message, List<string> options,
        bool multiSelect, Action<int> onSelect, Action<HashSet<int>> onMultiConfirm,
        Action? onCancel = null)
    {
        var win = NewDialog(title, TuiTheme.Current.DialogInfoBorder, WideXScale);
        win.TitleBold = true; // 标题独占一行（诗名醒目）

        int cw = ContentW(WideXScale, 4);

        // ── 消息正文（1~5 行，超出末尾加省略号）──
        var msgLines = TuiHelper.WrapText(message, cw, AskMaxMessageLines);

        var vbox = new TuiVBox { Width = cw, ChildHAlign = HAlign.Center };
        foreach (var line in msgLines)
            vbox.Add(new TuiLabel(line) { Width = cw, TextAlign = HAlign.Center });

        vbox.Add(new TuiLabel("") { Height = 1 }); // spacer：消息与选项分隔

        // ── 选项列表（单选 ▶ 选中，多选 ☑/☐ 勾选，复用现有 TuiList）──
        int listH = Math.Max(1, Math.Min(options.Count, AskMaxListItems));
        var list = new TuiList
        {
            Items = options,
            SelectedIndex = 0,
            MultiSelect = multiSelect,
            Width = cw,
            Height = listH,
            Focused = true,
        };
        vbox.Add(list);

        vbox.Add(new TuiLabel("") { Height = 1 }); // spacer：选项与底部按钮分隔

        // ── 底部操作按钮 ──
        var bottom = new TuiHBox { Width = cw, Spacing = 2, ContentHAlign = HAlign.Center };
        if (multiSelect)
        {
            var okBtn = new TuiButton("确定") { Flex = 1 };
            var cancelBtn = new TuiButton("取消") { Flex = 1 };
            void Confirm()
            {
                win.Result = list.CheckedIndices;
                onMultiConfirm(list.CheckedIndices);
                win.OnClosed?.Invoke();
            }
            okBtn.OnClick = _ => Confirm();
            cancelBtn.OnClick = _ => { win.Result = null; onCancel?.Invoke(); win.OnClosed?.Invoke(); };
            // 多选：空格勾选，Enter = 确认（等同点击"确定"）
            list.OnSelect = _ => Confirm();
            bottom.Add(okBtn); bottom.Add(cancelBtn);
            ApplyButtonGradient(TuiTheme.Current.BtnCyanBlue, okBtn, cancelBtn);
        }
        else
        {
            var cancelBtn = new TuiButton("取消 (Esc)") { Flex = 1 };
            cancelBtn.OnClick = _ => { win.Result = null; onCancel?.Invoke(); win.OnClosed?.Invoke(); };
            bottom.Add(cancelBtn);
            ApplyButtonGradient(TuiTheme.Current.BtnCyanBlue, cancelBtn);
            // 单选：Enter/空格 激活当前选中项
            list.OnSelect = idx => { win.Result = idx; onSelect(idx); win.OnClosed?.Invoke(); };
        }
        vbox.Add(bottom);

        win.RootView = vbox;
        ApplyGradient(win, TuiTheme.Current.GradCyanBlue);
        win.RegisterShortcut(ConsoleKey.Escape, () => { win.Result = null; onCancel?.Invoke(); win.OnClosed?.Invoke(); });

        // 精确高度：边框(2) + 标题行(1) + 消息 + spacer(1) + 列表 + spacer(1) + 底部按钮(1)
        win.Height = msgLines.Count + listH + 6;

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
        const int blackFg = TuiColors.Black; // 黄底警告框保持黑字
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
            Border = WindowBorder.Rounded,
            WinBg = warnBg,
            XScale = WideXScale,
            WindowHAlign = HAlign.Center,
            WindowVAlign = VAlign.Middle,
            MinWidth = MinDialogW,
            Height = 7,
        };

        int cw = ContentW(WideXScale, 4);
        var msgLabels = BuildMessageLabels(message, cw, blackFg);

        var vbox = new TuiVBox { Width = cw, ChildHAlign = HAlign.Center };
        foreach (var lbl in msgLabels)
        {
            lbl.Fg = blackFg;
            vbox.Add(lbl);
        }

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
        win.RegisterShortcut(ConsoleKey.Escape, () => { win.Result = DialogResult.No; onResult(DialogResult.No); win.OnClosed?.Invoke(); });
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
