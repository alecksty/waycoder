using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.TUI;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Tui;

/// <summary>
/// 统一 UX 辅助层 —— TUI 模式下使用 TuiDialog 模态窗口，
/// 非 TUI 模式（一次性 --prompt）回退到 Console I/O。
/// </summary>
public static class UxHelper
{
    /// <summary>当前是否在 TUI 全屏模式</summary>
    public static bool IsTuiMode
    {
        get
        {
            try { return TuiManager.Instance?.ActiveScreen != null; }
            catch { return false; }
        }
    }

    /// <summary>
    /// 眼下有能应答逐条确认的**交互界面**吗（权限确认 / 逐 hunk diff 预览 / 向用户提问，
    /// 都是「停下来等人按一个键」的用法）。含 TUI 全屏界面与交互式终端。
    ///
    /// 判据不能退化成只看 `Console.IsInputRedirected`：stdin 被重定向**也可能**是有界面的
    /// ——v0.96.88 起这种环境照常进 TUI（读键改从控制台设备取，见 <c>ConsoleDevice</c>）。
    /// 只判重定向会把这些确认悄悄降级成「自动通过」，用户在非 YOLO 模式下被跳过逐 hunk 确认
    /// 却毫无提示。Web 端由 SSE 交互桥单独处理（见 AskUserQuestionTool），不在此列。
    /// </summary>
    public static bool CanConfirmInline
        => IsTuiMode || (!Console.IsInputRedirected && !Console.IsOutputRedirected);

    /// <summary>把 TuiMarkup 指定 id 的按钮接线到 action（多个 Picker 重复的 Wire 样板）。</summary>
    public static void Wire(TuiMarkupResult res, string id, Action action)
    {
        var btn = res.Find<TuiButton>(id);
        if (btn != null) btn.OnClick = _ => action();
    }

    /// <summary>应用统一对话框渐变（与 TuiDialog 系一致），收敛各 Picker 手写的 4 行渐变样板。</summary>
    public static void ApplyGradient(TuiWindow win)
    {
        var g = TuiTheme.Current.DialogGradient;
        win.GradientBorder = true;
        win.GradientStart = g.start;
        win.GradientEnd = g.end;
    }

    /// <summary>
    /// Web 模式的异步交互桥。WebChatServer 注入实现后，AskUserQuestionTool / PermissionManager
    /// 的提问/确认不再阻塞在 Console，而是经 SSE 弹浏览器对话框等待响应。
    /// </summary>
    public interface IWebInteraction
    {
        /// <summary>文本输入。返回输入内容，null=取消。</summary>
        Task<string?> AskAsync(string prompt, string? defaultValue, int timeoutMs);

        /// <summary>单选。返回选中项 label，null=取消。</summary>
        Task<string?> SelectAsync(string title, List<string> choices, int timeoutMs);

        /// <summary>多选。返回选中项 label 列表，null=取消。</summary>
        Task<List<string>?> MultiSelectAsync(string title, List<string> choices, int timeoutMs);

        /// <summary>确认框。返回 0=是 1=总是允许 2=否（与 UxHelper.Confirm 对齐）。</summary>
        Task<int> ConfirmAsync(string title, string message, bool allowAll, int timeoutMs);

        /// <summary>Diff 预览：逐 hunk 确认。返回决策与接受的 hunk 索引；null=取消/超时（视为拒绝）。</summary>
        Task<DiffConfirmResult?> DiffConfirmAsync(string filePath, List<DiffPreview.Hunk> hunks, int timeoutMs);
    }

    /// <summary>Web 模式注入的交互桥（null=非 Web 模式，走原 TUI/Console 路径）。</summary>
    public static IWebInteraction? WebInteraction { get; set; }

    // ── 通知消息 ──

    /// <summary>非 TUI 模式的 GUI 通知回调（level/title/message，GUI 注入后显示 Toast/系统消息）。</summary>
    public static Action<string, string, string>? OnNotify;

    public static void Info(string title, string message)
    {
        if (IsTuiMode)
            ShowNotification(TuiDialog.Info(title, message));
        else if (OnNotify != null) OnNotify("info", title, message);
        else
            Console.WriteLine($"{AnsiTty.Accent($"[ℹ {title}]")} {message}");
    }

    public static void Success(string title, string message)
    {
        if (IsTuiMode)
            ShowNotification(TuiDialog.Success(title, message));
        else if (OnNotify != null) OnNotify("success", title, message);
        else
            Console.WriteLine($"{AnsiTty.Success($"[✓ {title}]")} {message}");
    }

    public static void Warn(string title, string message)
    {
        if (IsTuiMode)
            ShowNotification(TuiDialog.Warn(title, message));
        else if (OnNotify != null) OnNotify("warn", title, message);
        else
            Console.WriteLine($"{AnsiTty.Warn($"[⚠ {title}]")} {message}");
    }

    public static void Error(string title, string message)
    {
        if (IsTuiMode)
            ShowNotification(TuiDialog.Error(title, message));
        else if (OnNotify != null) OnNotify("error", title, message);
        else
            Console.WriteLine($"{AnsiTty.Error($"[✘ {title}]")} {message}");
    }

    private static void ShowNotification(TuiWindow win)
    {
        try
        {
            var screen = TuiManager.Instance?.ActiveScreen;
            screen?.AddRootWindow(win); // 通知框恒为根窗口：不被模态对话框的关闭递归带走
        }
        catch { /* 静默回退 */ }
    }

    // ── 文本输入 ──

    public static string Ask(string prompt, string? defaultValue = null, int timeoutMs = 30_000)
    {
        if (IsTuiMode)
            return ShowInputDialog(prompt, defaultValue ?? "", timeoutMs) ?? defaultValue ?? "";

        var defSuffix = defaultValue != null ? $" [{AnsiTty.DimText(defaultValue)}]" : "";
        Console.Write($"{AnsiTty.BoldText(prompt)}{defSuffix} ");
        var result = Console.ReadLine() ?? "";
        return string.IsNullOrEmpty(result) ? (defaultValue ?? "") : result;
    }

    private static string? ShowInputDialog(string prompt, string defaultValue, int timeoutMs)
        => RunModalDialog<string>((_, done) =>
            TuiDialog.Input("输入", prompt, defaultValue, val => done(val)), timeoutMs);

    // ── 密码输入 ──

    /// <summary>密码/密钥输入 —— TUI 下打开掩码对话框，非 TUI 回退到 Console 掩码读取</summary>
    public static string Secret(string prompt, string? defaultValue = null)
    {
        if (IsTuiMode)
            return ShowSecretDialog(prompt, defaultValue ?? "") ?? defaultValue ?? "";

        var defSuffix = defaultValue != null ? $" [{AnsiTty.DimText("***")}]" : "";
        Console.Write($"{AnsiTty.BoldText(prompt)}{defSuffix} ");
        var result = ReadPassword();
        return string.IsNullOrEmpty(result) ? (defaultValue ?? "") : result;
    }

    private static string ReadPassword()
    {
        var pass = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); break; }
            if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                pass.Length--;
            else if (key.KeyChar >= ' ' && key.Key != ConsoleKey.Escape)
                pass.Append(key.KeyChar);
        }
        return pass.ToString();
    }

    private static string? ShowSecretDialog(string prompt, string defaultValue)
        // 超时 30s 是这里原有的硬编码（其余对话框走各自传入的 timeoutMs）——显式保留
        => RunModalDialog<string>((_, done) =>
            TuiDialog.Secret("输入密钥", prompt, defaultValue, val => done(val),
                onCancel: () => done(null)), 30_000);

    // ── 选择列表 ──

    public static string? Select(string title, List<string> choices, int timeoutMs = 30_000)
    {
        if (choices.Count == 0) return null;

        if (IsTuiMode)
            return ShowSelectDialog(title, choices, timeoutMs);

        Console.WriteLine(AnsiTty.BoldText(title));
        for (int i = 0; i < choices.Count; i++)
            Console.WriteLine($"  [{i + 1}] {choices[i]}");
        Console.Write($"选择 (1-{choices.Count}, q=取消): ");

        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.KeyChar == 'q' || key.KeyChar == 'Q' || key.Key == ConsoleKey.Escape)
            {
                Console.WriteLine("取消");
                return null;
            }
            if (int.TryParse(key.KeyChar.ToString(), out var idx) && idx >= 1 && idx <= choices.Count)
            {
                Console.WriteLine(choices[idx - 1]);
                return choices[idx - 1];
            }
        }
    }

    private static string? ShowSelectDialog(string title, List<string> choices, int timeoutMs)
        => RunModalDialog<string>((_, done) =>
            TuiDialog.Select(title, choices,
                onSelect: idx => done(idx >= 0 && idx < choices.Count ? choices[idx] : null),
                onCancel: () => done(null)), timeoutMs);

    // ── 多选 ──

    /// <summary>
    /// 多选列表 —— TUI 下弹出多选对话框，非 TUI 回退到逐项 y/n 确认。
    /// 返回选中的项（原样）；null = 用户取消，空列表 = 确认但未选。
    /// </summary>
    public static List<string>? MultiSelect(string title, List<string> choices, int timeoutMs = 30_000, bool preCheckAll = false)
    {
        if (choices.Count == 0) return new List<string>();

        if (IsTuiMode)
            return ShowMultiSelectDialog(title, choices, timeoutMs, preCheckAll);

        var selected = new List<string>();
        Console.WriteLine($"{AnsiTty.BoldText(title)} (多选，逐项输入 y/n)");
        foreach (var c in choices)
        {
            Console.Write($"  [{c}] (y/n): ");
            var key = Console.ReadKey(intercept: false);
            Console.WriteLine();
            if (key.KeyChar == 'y' || key.KeyChar == 'Y')
                selected.Add(c);
        }
        return selected;
    }

    private static List<string>? ShowMultiSelectDialog(string title, List<string> choices, int timeoutMs, bool preCheckAll = false)
        => RunModalDialog<List<string>>((_, done) =>
        {
            var pre = preCheckAll ? Enumerable.Range(0, choices.Count).ToHashSet() : null;
            return TuiDialog.MultiSelect(title, choices,
                onConfirm: indices =>
                {
                    var picked = new List<string>();
                    for (int i = 0; i < choices.Count; i++)
                        if (indices.Contains(i)) picked.Add(choices[i]);
                    done(picked);
                },
                onCancel: () => done(null),
                preChecked: pre);
        }, timeoutMs);

    // ── 提问（LLM ask_user_question）──

    /// <summary>
    /// 提问对话框（标题 + 消息 + 选项按钮）。TUI 下弹出 Ask 对话框，非 TUI 回退到编号菜单。
    /// 单选返回选中索引，多选返回选中索引集合；null = 取消。
    /// </summary>
    public static List<int>? Ask(string title, string message, List<string> options, bool multiSelect, int timeoutMs = 30_000)
    {
        if (options.Count == 0) return multiSelect ? new List<int>() : null;

        if (IsTuiMode)
            return ShowAskDialog(title, message, options, multiSelect, timeoutMs);

        // 非 TUI：打印标题 + 消息 + 编号选项
        Console.WriteLine(AnsiTty.BoldText(title));
        if (!string.IsNullOrWhiteSpace(message))
            Console.WriteLine(message);

        if (multiSelect)
        {
            var sel = new List<int>();
            Console.WriteLine("(多选，输入编号用逗号分隔，如 1,3)");
            for (int i = 0; i < options.Count; i++)
                Console.WriteLine($"  [{i + 1}] {options[i]}");
            Console.Write($"选择 (1-{options.Count}, q=取消): ");
            var line = Console.ReadLine() ?? "";
            if (line.Trim().ToLowerInvariant() is "q" or "quit") return null;
            foreach (var part in line.Split([',', '，', ' '], StringSplitOptions.RemoveEmptyEntries))
                if (int.TryParse(part, out var idx) && idx >= 1 && idx <= options.Count)
                    sel.Add(idx - 1);
            return sel;
        }
        else
        {
            for (int i = 0; i < options.Count; i++)
                Console.WriteLine($"  [{i + 1}] {options[i]}");
            Console.Write($"选择 (1-{options.Count}, q=取消): ");
            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.KeyChar == 'q' || key.KeyChar == 'Q' || key.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine("取消");
                    return null;
                }
                if (int.TryParse(key.KeyChar.ToString(), out var idx) && idx >= 1 && idx <= options.Count)
                {
                    Console.WriteLine(options[idx - 1]);
                    return [idx - 1];
                }
            }
        }
    }

    private static List<int>? ShowAskDialog(string title, string message, List<string> options, bool multiSelect, int timeoutMs)
        => RunModalDialog<List<int>>((_, done) =>
            TuiDialog.Ask(title, message, options, multiSelect,
                onSelect: idx => done([idx]),
                onMultiConfirm: picked => done(picked.ToList()),
                onCancel: () => done(null)), timeoutMs);

    // ── 确认（权限） ──

    /// <summary>
    /// 确认对话框 —— TUI 下弹出权限确认框（黄底 Y/N/A），非 TUI 回退到编号菜单。
    /// 返回 0=允许、1=全部允许、2=拒绝。allowAll=false 时不给「全部允许」选项（危险操作）。
    /// </summary>
    public static int Confirm(string title, string message, bool allowAll = false, int timeoutMs = 0)
    {
        if (IsTuiMode)
            return ShowConfirmDialog(title, message, allowAll, timeoutMs);

        Warn(title, message);
        List<string> choices = allowAll
            ? new List<string> { "是 (y)", "总是允许 (a)", "否 (n)" }
            : new List<string> { "是 (y)", "否 (n)" };
        var choice = Select("是否执行？", choices);
        return choice switch
        {
            "是 (y)" => 0,
            "总是允许 (a)" => 1,
            _ => 2
        };
    }

    private static int ShowConfirmDialog(string title, string message, bool allowAll, int timeoutMs)
    {
        // ⚠ TResult 必须取 int? 而**不是** int：RunModalDialog 是无约束泛型，对值类型
        // `TResult?` 不退化为 Nullable<T> —— 用 int 的话「回调未触发」（超时 / 构建抛异常）
        // 会退化成 default(int) = 0 = **允许**，权限弹窗在异常路径上就从「拒绝」翻成「允许」了。
        // 取 int? 才能表达空态，末尾的 ?? 2 保住原来那句「默认拒绝」。
        var r = RunModalDialog<int?>((_, done) =>
        {
            if (allowAll)
                return TuiDialog.Permission(title, message, res => done(res switch
                {
                    TuiDialog.EDialogResult.Yes => 0,
                    TuiDialog.EDialogResult.Ok => 1,
                    _ => 2,
                }));
            return TuiDialog.Confirm(title, message, ok => done(ok ? 0 : 2));
        }, timeoutMs);
        return r ?? 2; // 未回调 → 拒绝（危险操作绝不因超时/异常被放行）
    }

    // ── 模态对话框样板收敛 ──

    /// <summary>
    /// 运行一个模态对话框的通用样板：构建窗口 → ShowWindow → RenderWait 阻塞等待结果。
    /// 各 Picker（ModelPicker/FilePicker/CommandPalette…）此前重复约 8 份同样的
    /// 「result + ManualResetEventSlim + try/catch + RenderWait」样板，收敛到此单点。
    /// </summary>
    /// <param name="build">构建对话框窗口。参数为当前活跃屏 + 完成回调（回调触发即置位事件、返回结果）。</param>
    /// <param name="timeoutMs">超时毫秒数（0=无限等，用户主动对话框默认）。透传 RenderWait。</param>
    /// <param name="readKeys">是否由本循环接管渲染+读键，见 <see cref="RenderWait"/>。null 自动判定。</param>
    public static TResult? RunModalDialog<TResult>(
        Func<TuiScreen?, Action<TResult?>, TuiWindow> build,
        int timeoutMs = 0, bool? readKeys = null)
    {
        TResult? result = default;
        using var evt = new ManualResetEventSlim(false);
        try
        {
            var screen = TuiManager.Instance?.ActiveScreen;
            var win = build(screen, r => { result = r; evt.Set(); });
            screen?.ShowWindow(win);
            RenderWait(screen, evt, timeoutMs, win, readKeys: readKeys);
        }
        catch { evt.Set(); }
        return result;
    }

    /// <summary>
    /// 统一「模态对话框完成」样板：回调结果置位事件 + 关闭窗口。
    /// 各 Picker（ModelPicker/SessionPicker/CommandPalette/ReasoningPicker/FilePicker）的
    /// Finish 此前重复同一段「onDone(r); win.OnClosed?.Invoke();」，收敛到此单点。
    /// 关窗动作本身委托给 <see cref="TuiWindow.Close(object?, Action?)"/> —— 全仓「关模态窗」
    /// 只有那一个出口，这里只是它的类型化适配（picker 的结果走 <c>evt.Set()</c> 回调交付，
    /// 不从 <c>win.Result</c> 读，写进去只是顺带，无副作用）。
    /// </summary>
    public static void FinishModal<TResult>(TuiWindow win, Action<TResult?> onDone, TResult? result)
        => win.Close(result, () => onDone(result));

    /// <summary>屏幕版模态对话框样板：UI 线程直执 ShowWindow；后台线程经 screen.PostToUI 投递，
    /// RenderWait 保持原 readKeys 语义（后台线程只等待）。收敛 ChatScreen.Dialogs/Input 手写样板。
    /// 约束 struct 使 TResult? = Nullable&lt;T&gt;（int/bool 等值类型可放心用 ?? 兜底），
    /// 无约束泛型的 TResult? 对值类型不退化为 Nullable，无法表达「对话框未回调」的空态。</summary>
    public static TResult? RunModalDialogOnScreen<TResult>(
        TuiScreen screen, Func<Action<TResult?>, TuiWindow> build,
        int timeoutMs = 0, bool? readKeys = null) where TResult : struct
    {
        TResult? result = default;
        using var evt = new ManualResetEventSlim(false);
        try
        {
            var win = build(r => { result = r; evt.Set(); });
            if (screen.IsUiThread) screen.ShowWindow(win);
            else screen.PostToUI(() => screen.ShowWindow(win));
            RenderWait(screen, evt, timeoutMs, win, readKeys: readKeys);
        }
        catch { evt.Set(); }
        return result;
    }

    // ── 事件循环 ──

    /// <summary>
    /// 渲染等待循环 —— 阻塞当前线程，轮询渲染 + 处理输入直到 evt 被设置或超时。
    /// 由 ShowInputDialog/ShowSelectDialog 等内部调用，也可由工具（如 AskUserQuestion）外部调用。
    /// </summary>
    /// <param name="timeoutMs">超时毫秒数（默认 30s，AskUserQuestion 等需更长超时）</param>
    /// <param name="readKeys">
    /// 谁接管「渲染 + 读键 + 路由」—— 单所有者原则，绝不双线程并发：
    ///   null（默认）→ 按调用线程自动判定：UI 线程 = true（主循环被本调用阻塞，本循环全权接管）；
    ///                            后台线程 = false（常驻主循环/外层渲染循环负责，本循环只等待事件）。
    ///   true  → 本循环渲染+读键（UI 线程命令/对话框场景，如 /model、ModelPicker）。
    ///   false → 只等待事件，外层循环负责渲染与按键路由（Agent 执行期）。
    /// 此前用 <c>readKeys:!Program.InAgentRenderLoop</c> 判定，槽位任务路径（REPL 主循环常驻）漏判 →
    /// 后台 Agent 线程自己也渲染+读键，与主循环并发写终端/抢 Console 输入/改窗口栈
    /// （Windows 列表竞态、焦点捕获丢失、输入被抢），正是「任务执行中卡死 + 任务后输入框失灵」的根源。
    /// </param>
    public static void RenderWait(TuiScreen? screen, ManualResetEventSlim evt, int timeoutMs = 30_000, TuiWindow? win = null, bool? readKeys = null)
    {
        if (screen == null) { evt.Wait(TimeSpan.FromSeconds(30)); return; }
        var manager = TuiManager.Instance;
        var inputMgr = TuiManager.Instance.Input; // 共享 InputManager：统一 bracketed paste/CSI 解析
        // 单一 UI 循环所有者：后台线程调用一律只等待（渲染+读键交给常驻主循环/外层循环），
        // 只有 UI 线程调用才由本循环全权接管（此时主循环正被本调用阻塞，不接管没人渲染）。
        bool ownLoop = readKeys ?? screen.IsUiThread;
        var start = Environment.TickCount64;
        while (!evt.IsSet)
        {
            // 窗口已被关闭（ESC / OnClosed 关窗但未置位 evt）→ 不再等待，立即返回。
            // 防 RenderWait(timeoutMs=0) 依赖 evt 永久卡死调用方：`.tui` 对话框（ProviderPicker 等）若
            // 未像 ModelPicker 那样 RegisterShortcut(Esc) 置位 evt，ESC 关窗后主循环会被永远堵在 RenderWait
            // → 「消息进列表但命令没执行」。
            if (win != null && win.Screen == null) break;

            // 单帧 try/catch：渲染/读键一帧异常不逃逸（否则 evt 永不置位 → 对话框永久卡死），
            // 下一帧照常重绘，窗口栈不残留。
            try
            {
                if (ownLoop)
                {
                    // 本循环接管渲染+读键 = 主循环（UiLoopTick 所属循环）被本调用阻塞。
                    // 必须同步刷新 UiLoopTick + 阶段标记，否则看门狗会把「对话框正常打开等待用户操作」
                    // 误判成「主循环冻结」（日志里反复的 ~3s UI.Freeze 假阳性）。本循环每帧都在跑，
                    // UiLoopTick 随之前进 → 看门狗不再误报。
                    TuiManager.SetActivity("RenderWait.Dialog");
                    screen.PumpUIQueue(); // 对话框期间也消费后台投递的 UI 操作（PostToUI 已提炼到基类）
                    manager?.Render();
                    var ev = inputMgr.ReadInput(30);
                    if (ev.Type == InputType.Mouse && TuiManager.MouseEnabled)
                        manager?.HandleMouse(ev); // 对话框按钮点击
                    else if (ev.Type == InputType.Key) screen.OnKey(ev.KeyInfo);
                    else if (ev.Type == InputType.Paste && screen is ChatScreen cs && !string.IsNullOrEmpty(ev.PasteText))
                        cs.HandleBracketedPaste(ev.PasteText); // 粘贴到对话框焦点输入控件
                    else if (ev.Type == InputType.Resize) manager?.OnResize();
                }
                else
                {
                    // 后台线程：只等待事件，渲染 + 键路由由常驻主循环 / RunAgentWithRenderLoop / RunWithUiLoop 负责
                    Thread.Sleep(30);
                }
            }
            catch { /* 单帧异常吞掉，下帧重绘；不关窗不置位，避免卡死 */ }
            if (timeoutMs > 0 && Environment.TickCount64 - start > timeoutMs) break;
        }
        // 超时兜底：关闭仍残留的模态窗口，避免窗口停在屏幕上。
        // 后台线程绝不直接改窗口栈（Windows 列表无锁），投递到 UI 线程关闭。
        if (!evt.IsSet && win != null)
        {
            if (ownLoop) screen.CloseWindow(win);
            else screen.PostToUI(() => screen.CloseWindow(win));
        }
        if (ownLoop) manager?.Render(); // 后台线程：渲染由外层负责
    }
}

/// <summary>Web diff 预览确认结果（决策 + 接受的 hunk 索引集合）。</summary>
public sealed class DiffConfirmResult
{
    public DiffPreview.Decision Decision = DiffPreview.Decision.RejectAll;
    public HashSet<int>? AcceptedHunks;
}
