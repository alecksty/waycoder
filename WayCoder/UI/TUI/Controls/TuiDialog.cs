using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Edit;
using WayCoder.UI.Shared;
using WayCoder.UI.TUI;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 对话框工具集 —— 常用对话框的便捷工厂方法。
/// 布局全部来自声明式标记资源（UI/TUI/Raw/dialogs/*.tui），
/// 本类只做「加载模板 → 注入数据/标题/边框色 → 订阅事件」的 code-behind。
/// 调用方需要将返回的窗口添加到 Screen 并处理关闭逻辑。
/// </summary>
public static class TuiDialog
{
    /// <summary>对话框返回结果</summary>
    public enum EDialogResult
    {
        Ok,
        Yes,
        No,
        Cancel,
        Closed
    }

    /// <summary>对话框默认宽度比例</summary>
    private const double DefaultXScale = 0.25;

    private const double WideXScale = 0.75; // 宽对话框：最多占屏幕 3/4
    private const double NarrowXScale = 0.4;

    /// <summary>对话框最小宽度</summary>
    private const int MinDialogW = 24 / 2;

    private const int MinDialogH = 3;

    /// <summary>根据 XScale 计算内容可用宽度（减去边框和内边距）</summary>
    private static int ContentW(double xScale = DefaultXScale, int innerPad = 4)
    {
        //  按照比例计算窗口宽度，确保最小宽度为 MinDialogW
        var winW = Math.Max(MinDialogW, (int)(Tty.Cols * xScale));

        return Math.Max(10, winW - 2 - innerPad); // 2=边框, innerPad=内边距
    }

    /// <summary>
    /// 计算内容宽并同步设置窗口为按内容计算的固定宽度。
    /// 修复输入/选择类对话框只设控件宽、不设 win.Width 导致窗口停留在模板默认 30 列、内容被裁剪的问题。
    /// 关系：cw = winW - 2(边框) - innerPad，故 winW = cw + 2 + innerPad。
    /// </summary>
    /// <param name="applyWidth">
    /// 把算出来的内容宽刷到各控件上。传了它就一并注册到 <see cref="TuiWindow.OnResizeContent"/>，
    /// 终端缩放时重算一遍 —— 不注册的话 XScale=0 会让窗口对 resize 彻底无反应，
    /// 内容和外框一起卡在构建时那个屏宽算出来的尺寸上。
    /// </param>
    /// <param name="afterResize">
    /// 在 resize 处理器里「刷完宽度」之后再跑的动作。输入类对话框的提示文本在窄屏会折成更多行，
    /// 只重算宽度不重算高度 → 内容比内容区高、底部按钮被挤出窗口 =「改屏幕尺寸按钮不见了」。
    /// 传 <c>() =&gt; TuiMarkup.FitWindowToContent(win, fitWidth: false)</c> 让高度跟着内容走。
    /// </param>
    private static int ApplyContentWidth(TuiWindow win, double xScale, int innerPad,
        Action<int>? applyWidth = null, Action? afterResize = null)
    {
        int Apply()
        {
            var cw = ContentW(xScale, innerPad);
            win.Width = cw + 2 + innerPad;
            applyWidth?.Invoke(cw);
            return cw;
        }

        win.XScale = 0; // 宽度由 ContentW 统一决定，不走比例缩放；resize 时靠上面的回调重算
        var cw0 = Apply();
        if (applyWidth != null)
            win.OnResizeContent = () => { Apply(); afterResize?.Invoke(); };
        return cw0;
    }

    /// <summary>
    /// 将消息文本折行为 TuiLabel 列表。自动处理 \n 换行和超宽折行，
    /// 最多 MaxMessageLines() 行，超出行尾显示 "…"。
    /// </summary>
    private static List<TuiLabel> BuildMessageLabels(string message, int labelWidth, int? fg = null)
    {
        //  拆解决消息文本，确保不超过 labelWidth 宽度，最多 MaxMessageLines() 行，超出行尾显示 "…"
        var lines = AnsiHelper.WrapText(message, labelWidth, MaxMessageLines());
        return lines.Select(line => new TuiLabel(line)
        {
            Width = labelWidth,
            Fg = fg ?? TuiTheme.Current.DialogFg, // 对话框正文黑字（灰底可读；此前 fg=0 回退到 ControlFg 白字，白底白字不可见）
            TextAlign = EHAlign.Center
        }).ToList();
    }

    /// <summary>
    /// 消息框固定消息行数。消息框改为固定高度：窗口总高固定 7 行
    /// （上下边框 2 行 + 内容区 5 行），内容区 = 消息(最多 4 行) + 按钮(1 行)。
    /// 标题嵌在顶部边框上（不独占行），故内容区正好 5 行。
    /// </summary>
    private static int MaxMessageLines() => 5;

    /// <summary>
    /// 给按钮启用渐变背景。文字色走 <see cref="TuiTheme.ButtonGradientFg"/>（亮底黑字），
    /// 不是 ButtonFg —— 后者是给黑底扁平按钮的白字，压在橙黄渐变上看不清。
    /// </summary>
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

    // ═══════════════════════════════════════════════════════════════
    // 标记模板加载辅助（布局来自 UI/TUI/Raw/dialogs/*.tui）
    // ═══════════════════════════════════════════════════════════════

    /// <summary>加载对话框模板，注入标题/边框色，返回 (窗口, 标记结果)。</summary>
    private static (TuiWindow win, TuiMarkupResult res) LoadDialog(
        string template, string title, int borderColor)
    {
        var file = template.EndsWith(".tui") ? template : template + ".tui";
        var res = TuiMarkup.LoadResource($"dialogs/{file}",
            new Dictionary<string, string> { ["title"] = title });
        var win = res.Window ?? throw new InvalidOperationException($"{template} 根应为 Dialog");
        win.Title = title;
        if (borderColor > 0) win.BorderColor = borderColor;
        return (win, res);
    }

    /// <summary>
    /// 把消息折行标签填入模板的 msgBox 容器（自动尺寸），返回 (内容宽, 窗口宽, 窗口高)。
    /// </summary>
    private static (int cw, int winW, int winH) FillMsgBox(
        TuiMarkupResult res, string message, IReadOnlyList<string> btnLabels)
    {
        var msgBox = res.Find<TuiVBox>("msgBox")
                     ?? throw new InvalidOperationException("对话框模板缺少 id=\"msgBox\" 容器");
        var (cw, lines, winW, winH) = AutoSizeMessageDialog(message, btnLabels);
        msgBox.Width = cw;
        msgBox.Children.Clear(); // 清掉模板里的预览占位标签
        foreach (var line in lines)
            msgBox.Add(new TuiLabel(line)
            {
                Width = cw,
                TextAlign = EHAlign.Center,
                Fg = TuiTheme.Current.DialogFg,
            });
        return (cw, winW, winH);
    }

    /// <summary>
    /// 按消息内容定窗口尺寸，并注册 resize 回调让内容跟着终端一起重算。
    ///
    /// 消息是构建时按当时屏宽折行的。只算一次的话，终端缩放时只有外框跟着动，
    /// 里面的标签还按老宽度折行 —— 就是「对话框只刷新外框、不刷新控件」。
    /// <see cref="TuiWindow.OnResizeContent"/> 这个钩子早就留好了（注释写着「由 TuiDialog 工厂方法设置」），
    /// 但在此之前没有任何对话框注册过，只有自测在用。
    ///
    /// XScale=0 是必须的：留着比例缩放，OnResize 步骤 0 会先按屏宽比覆盖 Width，
    /// 步骤 1 的回调再按内容算一遍，两者打架。宽度的唯一来源就是内容。
    /// </summary>
    private static void FitAndBindResize(TuiWindow win, TuiMarkupResult res,
        string message, IReadOnlyList<string> btnLabels)
    {
        void Refit()
        {
            // FillMsgBox 自带 Children.Clear()，重复调用是幂等的
            var (_, winW, _) = FillMsgBox(res, message, btnLabels);
            win.Width = winW;
            TuiMarkup.FitWindowToContent(win, fitWidth: false);
        }

        win.XScale = 0;
        Refit();
        win.OnResizeContent = Refit;
    }

    /// <summary>
    /// 消息/确认对话框自适应尺寸：宽度取「消息最宽行」与「按钮行宽」的较大者（含内边距），
    /// 高度取折行数（含按钮/边框）。返回 (内容宽 cw, 折行后的消息行, 窗口宽 winW, 窗口高 winH)。
    /// </summary>
    private static (int cw, List<string> lines, int winW, int winH) AutoSizeMessageDialog(
        string message, IReadOnlyList<string> btnLabels)
    {
        const int hPad = 4; // 内容区左右内边距（各 2 列）
        const int btnSpacing = 2; // 按钮间水平间距

        var maxContentW = Math.Max(12, Tty.Cols - 8); // 屏宽 - 左右边距(各3) - 边框(2)
        var maxMsgLines = Math.Max(1, Tty.Rows - 8); // 高度上限：留标题/边框/按钮/上下边距

        // 消息最宽自然行（按显示宽度，CJK/emoji 各占 2/1 列）
        var naturalW = 0;
        foreach (var raw in message.Replace("\r\n", "\n").Split('\n'))
        {
            var w = AnsiHelper.DisplayWidth(raw);
            if (w > naturalW) naturalW = w;
        }

        // 按钮行自然宽：各按钮 label+4 内边距 + 间距（取 max(8,·) 与单按钮一致）
        int btnRowW = 0;
        for (int i = 0; i < btnLabels.Count; i++)
        {
            btnRowW += Math.Max(8, AnsiHelper.DisplayWidth(btnLabels[i]) + 4);
            if (i > 0) btnRowW += btnSpacing;
        }

        int cw = Math.Clamp(Math.Max(naturalW, btnRowW) + hPad, 10, maxContentW);
        var lines = AnsiHelper.WrapText(message, cw - hPad, maxMsgLines);

        return (cw, lines, cw + 2, lines.Count + 3); // 宽=内容+2边框；高=消息+按钮+2边框（无标题分隔线）
    }

    // ═══════════════════════════════════════════════════════════════
    // 消息框（Info / Success / Warn / Error）
    // ═══════════════════════════════════════════════════════════════

    /// <summary>信息提示框（单"确定"按钮）</summary>
    public static TuiWindow Info(string title, string message)
        => BuildMessageBox(title, message, TuiTheme.Current.DialogInfoBorder,
            TuiTheme.Current.BtnCyanBlue, TuiTheme.Current.GradCyanBlue);

    /// <summary>成功提示框（绿色边框）</summary>
    public static TuiWindow Success(string title, string message)
        => BuildMessageBox(title, message, TuiTheme.Current.DialogSuccessBorder,
            TuiTheme.Current.BtnGreenCyan, TuiTheme.Current.GradGreenCyan);

    /// <summary>警告提示框（黄色边框）</summary>
    public static TuiWindow Warn(string title, string message)
        => BuildMessageBox(title, message, TuiTheme.Current.DialogWarnBorder,
            TuiTheme.Current.BtnOrangeYellow, TuiTheme.Current.DialogGradient);

    /// <summary>错误提示框（红色边框）</summary>
    public static TuiWindow Error(string title, string message)
        => BuildMessageBox(title, message, TuiTheme.Current.DialogErrorBorder,
            TuiTheme.Current.BtnRedOrange, TuiTheme.Current.GradRedOrange);

    /// <summary>单按钮消息框通用构建（模板 info.tui，宽高随消息内容自适应）。</summary>
    private static TuiWindow BuildMessageBox(string title, string message, int borderColor,
        (int start, int end) btnGrad, (int start, int end) winGrad)
    {
        var (win, res) = LoadDialog("info", title, borderColor);
        FitAndBindResize(win, res, message, ["确定"]);

        var btn = res.Find<TuiButton>("ok") ?? throw Invalid("info.tui", "ok");
        btn.Width = Math.Max(8, AnsiHelper.DisplayWidth("确定") + 4);
        btn.Focused = true;
        ApplyButtonGradient(btnGrad, btn);
        btn.OnClick = _ =>
        {
            win.Result = EDialogResult.Ok;
            win.OnClosed?.Invoke();
        };
        win.RegisterShortcut(ConsoleKey.Enter, () =>
        {
            win.Result = EDialogResult.Ok;
            win.OnClosed?.Invoke();
        });

        ApplyGradient(win, winGrad);
        return win;
    }

    // ═══════════════════════════════════════════════════════════════
    // 确认框
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Yes/No 确认框。onResult(true=Yes, false=No)</summary>
    public static TuiWindow Confirm(string title, string message, Action<bool> onResult)
    {
        var (win, res) = LoadDialog("confirm", title, TuiTheme.Current.DialogConfirmBorder);
        FitAndBindResize(win, res, message, ["是 (Y)", "否 (N)"]);

        var yesBtn = res.Find<TuiButton>("yes") ?? throw Invalid("confirm.tui", "yes");
        var noBtn = res.Find<TuiButton>("no") ?? throw Invalid("confirm.tui", "no");
        yesBtn.Focused = true;
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, yesBtn, noBtn);
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
        win.RegisterShortcut(ConsoleKey.Escape, () =>
        {
            win.Result = false;
            onResult(false);
            win.OnClosed?.Invoke();
        });

        ApplyGradient(win, TuiTheme.Current.DialogGradient);
        return win;
    }

    /// <summary>Yes/No/Cancel 三选确认框</summary>
    public static TuiWindow Confirm3(string title, string message, Action<EDialogResult> onResult)
    {
        var (win, res) = LoadDialog("confirm3", title, TuiTheme.Current.DialogConfirmBorder);
        FitAndBindResize(win, res, message, ["是 (Y)", "否 (N)", "取消 (Esc)"]);

        var yesBtn = res.Find<TuiButton>("yes") ?? throw Invalid("confirm3.tui", "yes");
        var noBtn = res.Find<TuiButton>("no") ?? throw Invalid("confirm3.tui", "no");
        var cancelBtn = res.Find<TuiButton>("cancel") ?? throw Invalid("confirm3.tui", "cancel");
        yesBtn.Focused = true;
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, yesBtn, noBtn, cancelBtn);
        yesBtn.OnClick = _ =>
        {
            win.Result = EDialogResult.Yes;
            onResult(EDialogResult.Yes);
            win.OnClosed?.Invoke();
        };
        noBtn.OnClick = _ =>
        {
            win.Result = EDialogResult.No;
            onResult(EDialogResult.No);
            win.OnClosed?.Invoke();
        };
        cancelBtn.OnClick = _ =>
        {
            win.Result = EDialogResult.Cancel;
            onResult(EDialogResult.Cancel);
            win.OnClosed?.Invoke();
        };

        win.RegisterShortcut(ConsoleKey.Y, () =>
        {
            win.Result = EDialogResult.Yes;
            onResult(EDialogResult.Yes);
            win.OnClosed?.Invoke();
        });
        win.RegisterShortcut(ConsoleKey.N, () =>
        {
            win.Result = EDialogResult.No;
            onResult(EDialogResult.No);
            win.OnClosed?.Invoke();
        });
        win.RegisterShortcut(ConsoleKey.Escape, () =>
        {
            win.Result = EDialogResult.Cancel;
            onResult(EDialogResult.Cancel);
            win.OnClosed?.Invoke();
        });

        ApplyGradient(win, TuiTheme.Current.DialogGradient);
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

        var displayTitle = string.IsNullOrEmpty(title) || title == "输入" ? "请输入" : title;
        var (win, res) = LoadDialog("input", displayTitle, TuiTheme.Current.DialogInfoBorder);
        win.MinHeight = 8;

        var input = res.Find<TuiTextArea>("input")
                    ?? throw Invalid("input.tui", "input");
        var cw = ApplyContentWidth(win, WideXScale, 2, w =>
        {
            FillPrompt(res, prompt, w); // 内部自带 Children.Clear()，重复调用幂等
            input.Width = w;
        }, () => AfterResizeRefitHeight(win)); // 窄屏重折行 → 高度跟着重算，别让按钮被挤出窗口
        input.Height = inputHeight;
        input.Fg = AnsiColors.White;
        input.Bg = AnsiColors.BgBlack;
        input.Focused = true;

        var hist = TuiInputHistory.Get(title);
        var initVal = !string.IsNullOrEmpty(defaultValue) ? defaultValue
            : hist.Count > 0 ? hist[0] : "";
        if (!string.IsNullOrEmpty(initVal)) input.Text = initVal;

        var okBtn = res.Find<TuiButton>("ok") ?? throw Invalid("input.tui", "ok");
        var cancelBtn = res.Find<TuiButton>("cancel") ?? throw Invalid("input.tui", "cancel");
        okBtn.Flex = 1;
        cancelBtn.Flex = 1;
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
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, okBtn, cancelBtn);
        win.RegisterShortcut(ConsoleKey.Escape, () =>
        {
            win.Result = null;
            onCancel?.Invoke();
            win.OnClosed?.Invoke();
        });

        // 高度按填好内容后的控件树实测（提示行 + 输入区 + 按钮行 + 各处 spacing）。
        // 此前根本没算高，直接吃模板默认 height —— 内容比内容区高一行，按钮就被切在框外
        TuiMarkup.FitWindowToContent(win, fitWidth: false);
        ApplyGradient(win, TuiTheme.Current.DialogGradient);
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
        var displayTitle = string.IsNullOrEmpty(title) || title == "输入" ? "请输入" : title;
        var (win, res) = LoadDialog("inputline", displayTitle, TuiTheme.Current.DialogInfoBorder);
        win.MinHeight = 6;

        var hist = TuiInputHistory.Get(title);
        var initVal = !string.IsNullOrEmpty(defaultValue) ? defaultValue
            : hist.Count > 0 ? hist[0] : "";

        var input = res.Find<TuiInput>("input") ?? throw Invalid("inputline.tui", "input");
        input.Text = initVal;
        input.CursorPos = initVal.Length;
        int cw = ApplyContentWidth(win, WideXScale, 2, w =>
        {
            FillPrompt(res, prompt, w);
            input.Width = w;
        }, () => AfterResizeRefitHeight(win)); // 窄屏重折行 → 高度跟着重算，别让按钮被挤出窗口
        input.Height = 1;
        input.Fg = AnsiColors.White;
        input.Bg = AnsiColors.BgBlack;
        input.Focused = true;

        var okBtn = res.Find<TuiButton>("ok") ?? throw Invalid("inputline.tui", "ok");
        var cancelBtn = res.Find<TuiButton>("cancel") ?? throw Invalid("inputline.tui", "cancel");
        okBtn.Flex = 1;
        cancelBtn.Flex = 1;

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
        cancelBtn.OnClick = _ =>
        {
            win.Result = null;
            onCancel?.Invoke();
            win.OnClosed?.Invoke();
        };
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, okBtn, cancelBtn);
        win.RegisterShortcut(ConsoleKey.Escape, () =>
        {
            win.Result = null;
            onCancel?.Invoke();
            win.OnClosed?.Invoke();
        });

        // 高度按填好内容后的控件树实测（提示行 + 输入区 + 按钮行 + 各处 spacing）。
        // 此前根本没算高，直接吃模板默认 height —— 内容比内容区高一行，按钮就被切在框外
        TuiMarkup.FitWindowToContent(win, fitWidth: false);
        ApplyGradient(win, TuiTheme.Current.DialogGradient);
        return win;
    }

    /// <summary>把提示文本折行填入模板 msgBox 容器（输入类对话框共用）。</summary>
    private static void FillPrompt(TuiMarkupResult res, string prompt, int cw)
    {
        var msgBox = res.Find<TuiVBox>("msgBox") ?? throw Invalid("输入对话框", "msgBox");
        msgBox.Width = cw;
        msgBox.Children.Clear(); // 清掉模板里的预览占位标签
        foreach (var line in AnsiHelper.WrapText(prompt, cw, 5))
            msgBox.Add(new TuiLabel(line) { Width = cw, Fg = AnsiColors.Black });
    }

    /// <summary>
    /// resize 回调的公共收尾：按填充后的控件树重算窗口高度。
    /// 输入对话框的提示在窄屏折成更多行，只重算宽度会让内容比窗口高、按钮被挤出窗口。
    /// 注意只用 fitHeight —— 宽度归 <see cref="ApplyContentWidth"/> 管，两者各司其职不打架。
    /// </summary>
    private static void AfterResizeRefitHeight(TuiWindow win)
        => TuiMarkup.FitWindowToContent(win, fitWidth: false);

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
        var (win, res) = LoadDialog("findreplace", "查找/替换", TuiTheme.Current.DialogInfoBorder);
        win.MinHeight = 11;

        var findInput = res.Find<TuiInput>("find") ?? throw Invalid("findreplace.tui", "find");
        var replInput = res.Find<TuiInput>("repl") ?? throw Invalid("findreplace.tui", "repl");
        var caseCb = res.Find<TuiCheckbox>("case") ?? throw Invalid("findreplace.tui", "case");
        var regexCb = res.Find<TuiCheckbox>("regex") ?? throw Invalid("findreplace.tui", "regex");
        var wordCb = res.Find<TuiCheckbox>("word") ?? throw Invalid("findreplace.tui", "word");
        var findBtn = res.Find<TuiButton>("findNext") ?? throw Invalid("findreplace.tui", "findNext");
        var replBtn = res.Find<TuiButton>("replace") ?? throw Invalid("findreplace.tui", "replace");
        var allBtn = res.Find<TuiButton>("replaceAll") ?? throw Invalid("findreplace.tui", "replaceAll");
        var cancelBtn = res.Find<TuiButton>("close") ?? throw Invalid("findreplace.tui", "close");

        findInput.Text = initialFind;
        findInput.CursorPos = initialFind.Length;
        findInput.Focused = true;
        replInput.Text = initialReplace;
        replInput.CursorPos = initialReplace.Length;
        caseCb.Checked = initialOpts.CaseSensitive;
        regexCb.Checked = initialOpts.UseRegex;
        wordCb.Checked = initialOpts.WholeWord;

        FindOptions CurrentOpts() => new(caseCb.Checked, regexCb.Checked, wordCb.Checked);

        findBtn.OnClick = _ =>
        {
            onFindNext(findInput.Text, CurrentOpts());
            win.OnClosed?.Invoke();
        };
        replBtn.OnClick = _ =>
        {
            onReplace(findInput.Text, replInput.Text, CurrentOpts());
            win.OnClosed?.Invoke();
        };
        allBtn.OnClick = _ =>
        {
            onReplaceAll(findInput.Text, replInput.Text, CurrentOpts());
            win.OnClosed?.Invoke();
        };
        cancelBtn.OnClick = _ => win.OnClosed?.Invoke();
        ApplyButtonGradient(TuiTheme.Current.BtnCyanBlue, findBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, replBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnGreenCyan, allBtn);
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, cancelBtn);

        findInput.OnSubmit = _ =>
        {
            onFindNext(findInput.Text, CurrentOpts());
            win.OnClosed?.Invoke();
        };
        replInput.OnSubmit = _ =>
        {
            onReplace(findInput.Text, replInput.Text, CurrentOpts());
            win.OnClosed?.Invoke();
        };
        win.RegisterShortcut(ConsoleKey.F3, () =>
        {
            onFindNext(findInput.Text, CurrentOpts());
            win.OnClosed?.Invoke();
        });
        win.RegisterShortcut(ConsoleKey.Escape, () => win.OnClosed?.Invoke());

        ApplyGradient(win, TuiTheme.Current.DialogGradient);
        return win;
    }

    // ═══════════════════════════════════════════════════════════════
    // 密码输入对话框
    // ═══════════════════════════════════════════════════════════════

    /// <summary>密码/密钥输入对话框 —— 字符显示为 • 掩码</summary>
    public static TuiWindow Secret(string title, string prompt, string defaultValue,
        Action<string> onConfirm, Action? onCancel = null)
    {
        var displayTitle = string.IsNullOrEmpty(title) || title == "输入密钥" ? "请输入" : title;
        var (win, res) = LoadDialog("secret", displayTitle, TuiTheme.Current.DialogInfoBorder);
        win.MinHeight = 6;

        var input = res.Find<TuiInput>("input") ?? throw Invalid("secret.tui", "input");
        input.Text = defaultValue;
        input.CursorPos = defaultValue.Length;
        int cw = ApplyContentWidth(win, NarrowXScale, 4, w =>
        {
            FillPrompt(res, prompt, w);
            input.Width = w;
        }, () => AfterResizeRefitHeight(win)); // 窄屏重折行 → 高度跟着重算，别让按钮被挤出窗口
        input.Password = true;
        input.Focused = true;

        var okBtn = res.Find<TuiButton>("ok") ?? throw Invalid("secret.tui", "ok");
        var cancelBtn = res.Find<TuiButton>("cancel") ?? throw Invalid("secret.tui", "cancel");
        okBtn.Flex = 1;
        cancelBtn.Flex = 1;

        void SubmitInput()
        {
            win.Result = input.Text;
            onConfirm(input.Text);
            win.OnClosed?.Invoke();
        }

        okBtn.OnClick = _ => SubmitInput();
        input.OnSubmit = _ => SubmitInput(); // 单行输入框回车 = 确定
        cancelBtn.OnClick = _ =>
        {
            win.Result = null;
            onCancel?.Invoke();
            win.OnClosed?.Invoke();
        };
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, okBtn, cancelBtn);
        win.RegisterShortcut(ConsoleKey.Escape, () =>
        {
            win.Result = null;
            onCancel?.Invoke();
            win.OnClosed?.Invoke();
        });

        // 高度按填好内容后的控件树实测（提示行 + 输入区 + 按钮行 + 各处 spacing）。
        // 此前根本没算高，直接吃模板默认 height —— 内容比内容区高一行，按钮就被切在框外
        TuiMarkup.FitWindowToContent(win, fitWidth: false);
        ApplyGradient(win, TuiTheme.Current.DialogGradient);
        return win;
    }

    // ═══════════════════════════════════════════════════════════════
    // 列表选择对话框
    // ═══════════════════════════════════════════════════════════════

    /// <summary>单选列表对话框</summary>
    public static TuiWindow Select(string title, List<string> items,
        Action<int> onSelect, Action? onCancel = null)
    {
        var (win, res) = LoadDialog("select", title, TuiTheme.Current.DialogInfoBorder);

        // 可见项：不超过全部项，也不超过屏幕可用高度（标题+列表+按钮+边框 ≈ 列表 + 4）
        var visItems = Math.Min(items.Count, Math.Max(3, Tty.Rows - 4));

        var list = res.Find<TuiList>("list") ?? throw Invalid("select.tui", "list");
        list.Items = items;
        list.SelectedIndex = 0;
        ApplyContentWidth(win, NarrowXScale, 2, w => list.Width = w);
        list.Height = visItems;
        list.Focused = true;
        // 模板里 list 默认 height=5，改大后必须按内容重算窗口高度，否则列表底部溢出下边框
        TuiMarkup.FitWindowToContent(win, fitWidth: false);
        win.MaxHeight = Math.Max(0, Tty.Rows - 1);
        list.OnSelect = idx =>
        {
            win.Result = idx;
            onSelect(idx);
            win.OnClosed?.Invoke();
        };

        var cancelBtn = res.Find<TuiButton>("cancel") ?? throw Invalid("select.tui", "cancel");
        cancelBtn.Flex = 1;
        cancelBtn.OnClick = _ =>
        {
            win.Result = -1;
            onCancel?.Invoke();
            win.OnClosed?.Invoke();
        };
        ApplyButtonGradient(TuiTheme.Current.BtnCyanBlue, cancelBtn);
        win.RegisterShortcut(ConsoleKey.Escape, () =>
        {
            win.Result = -1;
            onCancel?.Invoke();
            win.OnClosed?.Invoke();
        });

        ApplyGradient(win, TuiTheme.Current.GradCyanBlue);
        return win;
    }

    /// <summary>多选列表对话框</summary>
    public static TuiWindow MultiSelect(string title, List<string> items,
        Action<HashSet<int>> onConfirm, Action? onCancel = null,
        HashSet<int>? preChecked = null)
    {
        var (win, res) = LoadDialog("multiselect", title, TuiTheme.Current.DialogInfoBorder);

        // 可见项：不超过全部项，也不超过屏幕可用高度（标题+列表+hint+按钮+边框 ≈ 列表 + 6）
        var visItems = Math.Min(items.Count, Math.Max(3, Tty.Rows - 6));

        var list = res.Find<TuiList>("list") ?? throw Invalid("multiselect.tui", "list");
        list.Items = items;
        list.SelectedIndex = 0;
        list.MultiSelect = true;
        if (preChecked != null) list.CheckedIndices = new HashSet<int>(preChecked);
        ApplyContentWidth(win, NarrowXScale, 2, w => list.Width = w);
        list.Height = visItems;
        list.Focused = true;
        // 模板里 list 默认 height=5，改大后必须按内容重算窗口高度，否则列表底部溢出下边框
        TuiMarkup.FitWindowToContent(win, fitWidth: false);
        win.MaxHeight = Math.Max(0, Tty.Rows - 1);

        var okBtn = res.Find<TuiButton>("ok") ?? throw Invalid("multiselect.tui", "ok");
        var cancelBtn = res.Find<TuiButton>("cancel") ?? throw Invalid("multiselect.tui", "cancel");
        okBtn.Flex = 1;
        cancelBtn.Flex = 1;

        void Confirm()
        {
            win.Result = list.CheckedIndices;
            onConfirm(list.CheckedIndices);
            win.OnClosed?.Invoke();
        }

        okBtn.OnClick = _ => Confirm();
        cancelBtn.OnClick = _ =>
        {
            win.Result = null;
            onCancel?.Invoke();
            win.OnClosed?.Invoke();
        };
        // 多选列表：空格勾选，Enter = 确认（等同点击“确定”按钮）
        list.OnSelect = _ => Confirm();
        ApplyButtonGradient(TuiTheme.Current.BtnCyanBlue, okBtn, cancelBtn);
        win.RegisterShortcut(ConsoleKey.Escape, () =>
        {
            win.Result = null;
            onCancel?.Invoke();
            win.OnClosed?.Invoke();
        });

        ApplyGradient(win, TuiTheme.Current.GradCyanBlue);
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
        var (win, res) = LoadDialog("ask", title, TuiTheme.Current.DialogInfoBorder);

        // ── 消息正文（1~5 行，超出末尾加省略号）──
        var msgBox = res.Find<TuiVBox>("msgBox") ?? throw Invalid("ask.tui", "msgBox");

        // ── 选项列表（单选 ▶ 选中，多选 ☑/☐ 勾选，复用现有 TuiList）──
        int listH = Math.Max(1, Math.Min(options.Count, AskMaxListItems));
        var list = res.Find<TuiList>("list") ?? throw Invalid("ask.tui", "list");
        list.Items = options;
        list.SelectedIndex = 0;
        list.MultiSelect = multiSelect;

        // 宽度 + 折行都放进回调，终端缩放时整套重来一遍
        ApplyContentWidth(win, WideXScale, 4, w =>
        {
            msgBox.Width = w;
            msgBox.Children.Clear(); // 也清掉模板里的预览占位标签
            foreach (var line in AnsiHelper.WrapText(message, w, AskMaxMessageLines))
                msgBox.Add(new TuiLabel(line) { Width = w, TextAlign = EHAlign.Center, Fg = TuiTheme.Current.DialogFg });
            list.Width = w;
            // 精确高度：边框(2) + 消息 + spacer(1) + 列表 + spacer(1) + 底部按钮(1)。
            // 也得在回调里 —— 窄屏下消息折行变多，高度不跟着长就把列表挤出去了
            win.Height = msgBox.Children.Count + listH + 5;
        });
        list.Height = listH;
        list.Focused = true;

        // ── 底部操作按钮 ──
        var okBtn = res.Find<TuiButton>("ok") ?? throw Invalid("ask.tui", "ok");
        var cancelBtn = res.Find<TuiButton>("cancel") ?? throw Invalid("ask.tui", "cancel");
        okBtn.Flex = 1;
        cancelBtn.Flex = 1;
        okBtn.Visible = multiSelect; // 单选隐藏"确定"，选中即确认
        ApplyButtonGradient(TuiTheme.Current.BtnCyanBlue, okBtn, cancelBtn);
        cancelBtn.OnClick = _ =>
        {
            win.Result = null;
            onCancel?.Invoke();
            win.OnClosed?.Invoke();
        };
        if (multiSelect)
        {
            void Confirm()
            {
                win.Result = list.CheckedIndices;
                onMultiConfirm(list.CheckedIndices);
                win.OnClosed?.Invoke();
            }
            okBtn.OnClick = _ => Confirm();
            // 多选：空格勾选，Enter = 确认（等同点击"确定"）
            list.OnSelect = _ => Confirm();
        }
        else
        {
            // 单选：Enter/空格 激活当前选中项
            list.OnSelect = idx =>
            {
                win.Result = idx;
                onSelect(idx);
                win.OnClosed?.Invoke();
            };
        }

        win.RegisterShortcut(ConsoleKey.Escape, () =>
        {
            win.Result = null;
            onCancel?.Invoke();
            win.OnClosed?.Invoke();
        });

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
    public static TuiWindow Permission(string title, string message, Action<EDialogResult> onResult)
    {
        // 灰底黑字与其他对话框一致（主题 WindowBg/DialogFg）；黄边框保留为「权限确认」的语义信号
        var (win, res) = LoadDialog("permission", title, AnsiColors.Yellow);

        var yesBtn = res.Find<TuiButton>("allow") ?? throw Invalid("permission.tui", "allow");
        var noBtn = res.Find<TuiButton>("deny") ?? throw Invalid("permission.tui", "deny");
        var allBtn = res.Find<TuiButton>("always") ?? throw Invalid("permission.tui", "always");
        foreach (var b in new[] { yesBtn, noBtn, allBtn })
            b.Flex = 1; // 配色由 ApplyButtonGradient 设（橙黄渐变底 + 黑字）
        yesBtn.Focused = true;
        yesBtn.OnClick = _ => { win.Result = EDialogResult.Yes; onResult(EDialogResult.Yes); win.OnClosed?.Invoke(); };
        noBtn.OnClick = _ => { win.Result = EDialogResult.No; onResult(EDialogResult.No); win.OnClosed?.Invoke(); };
        allBtn.OnClick = _ => { win.Result = EDialogResult.Ok; onResult(EDialogResult.Ok); win.OnClosed?.Invoke(); };
        ApplyButtonGradient(TuiTheme.Current.BtnOrangeYellow, yesBtn, noBtn, allBtn);

        // 尺寸走和 Info/Confirm 同一条自适应路径。此前是 ContentW(WideXScale, 4) —— 不看内容，
        // 一律占屏宽 3/4，于是一行短消息也撑出一个大宽框。
        // FillMsgBox 顺带 Children.Clear() 掉模板里的 "…" 占位标签：别的对话框都清了，就这儿漏了，
        // 于是第一行永远多出一个左对齐的省略号。
        // 放在按钮接线之后：FitWindowToContent 要量控件树，按钮宽度得先定下来。
        FitAndBindResize(win, res, message, ["允许 (Y)", "拒绝 (N)", "全允 (A)"]);
        ApplyGradient(win, TuiTheme.Current.DialogGradient);

        win.RegisterShortcut(ConsoleKey.Y, () =>
        {
            win.Result = EDialogResult.Yes;
            onResult(EDialogResult.Yes);
            win.OnClosed?.Invoke();
        });
        win.RegisterShortcut(ConsoleKey.N, () =>
        {
            win.Result = EDialogResult.No;
            onResult(EDialogResult.No);
            win.OnClosed?.Invoke();
        });
        win.RegisterShortcut(ConsoleKey.A, () =>
        {
            win.Result = EDialogResult.Ok;
            onResult(EDialogResult.Ok);
            win.OnClosed?.Invoke();
        });
        win.RegisterShortcut(ConsoleKey.Escape, () =>
        {
            win.Result = EDialogResult.No;
            onResult(EDialogResult.No);
            win.OnClosed?.Invoke();
        });

        return win;
    }

    // ═══════════════════════════════════════════════════════════════
    // 仅绘制（调试/抓屏）
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 仅绘制对话框到 ANSI 字符串（不进入输入循环、不响应按键/消息），供调试/抓屏核对布局。
    /// x/y 传负值 = 按窗口默认对齐（居中）；传非负值 = 把窗口画到指定终端坐标 (x, y)。
    /// 返回完整 ANSI 帧（含光标隐藏/回首页/清屏），可直接 Console.Write 抓屏。
    /// </summary>
    public static string Show(TuiWindow win, int x = -1, int y = -1)
    {
        int termW = Tty.Cols;
        int termH = Tty.Rows;

        var screen = new RenderOnlyScreen();
        screen.SetSize(termW, termH);
        screen.RootView = new TuiVBox { Width = termW, Height = termH };
        screen.RootView.OnCreate();

        // 指定位置 → 禁用自动对齐（Stretch = 不自动定位），改由下方手动落位
        if (x >= 0 || y >= 0)
        {
            win.WindowHAlign = EHAlign.Stretch;
            win.WindowVAlign = EVAlign.Stretch;
        }

        // 复用真实 ShowWindow 的链路：OnCreate 初始化控件树，OnResize 按 XScale 算宽、
        // 按对齐算位并把 RootView 布局到内容区。此处无管理器/无输入循环，仅渲染一次。
        screen.AddWindow(win);

        // 手动落位：AddWindow 已触发 OnResize，此处再精确覆盖 X/Y（不受对齐/钳制影响）
        if (x >= 0) win.X = x;
        if (y >= 0) win.Y = y;

        var sb = new StringBuilder();
        sb.Append(AnsiTty.CursorHide).Append(AnsiTty.Home).Append(AnsiTty.ClearScreen);
        screen.Render(sb);
        return sb.ToString();
    }

    /// <summary>仅绘制用的最小屏幕：暴露 SetSize 以便在无管理器/无输入循环下渲染单个窗口。</summary>
    public sealed class RenderOnlyScreen : TuiScreen
    {
        public void SetSize(int w, int h)
        {
            TW = w;
            TH = h;
        }
    }

    /// <summary>模板缺失指定 id 控件的异常。</summary>
    private static InvalidOperationException Invalid(string template, string id)
        => new($"{template} 缺少 id=\"{id}\" 的控件");
}
