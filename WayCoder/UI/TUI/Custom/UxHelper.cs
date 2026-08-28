using WayCoder.UI.Shared.Terminal;
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
            screen?.ShowWindow(win);
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
    {
        string? result = null;
        using var evt = new ManualResetEventSlim(false);
        try
        {
            var win = TuiDialog.Input("输入", prompt, defaultValue, val =>
            {
                result = val;
                evt.Set();
            });
            var screen = TuiManager.Instance?.ActiveScreen;
            screen?.ShowWindow(win);
            RenderWait(screen, evt, timeoutMs, win);
        }
        catch { evt.Set(); }
        return result;
    }

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
    {
        string? result = null;
        using var evt = new ManualResetEventSlim(false);
        try
        {
            var win = TuiDialog.Secret("输入密钥", prompt, defaultValue, val =>
            {
                result = val;
                evt.Set();
            },
            onCancel: () =>
            {
                result = null;
                evt.Set();
            });
            var screen = TuiManager.Instance?.ActiveScreen;
            screen?.ShowWindow(win);
            RenderWait(screen, evt, 30_000, win);
        }
        catch { evt.Set(); }
        return result;
    }

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
    {
        string? result = null;
        using var evt = new ManualResetEventSlim(false);
        try
        {
            var win = TuiDialog.Select(title, choices,
                onSelect: idx => { result = idx >= 0 && idx < choices.Count ? choices[idx] : null; evt.Set(); },
                onCancel: () => { result = null; evt.Set(); });
            var screen = TuiManager.Instance?.ActiveScreen;
            screen?.ShowWindow(win);
            RenderWait(screen, evt, timeoutMs, win);
        }
        catch { evt.Set(); }
        return result;
    }

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
    {
        List<string>? result = null;
        using var evt = new ManualResetEventSlim(false);
        try
        {
            var pre = preCheckAll ? Enumerable.Range(0, choices.Count).ToHashSet() : null;
            var win = TuiDialog.MultiSelect(title, choices,
                onConfirm: indices =>
                {
                    result = new List<string>();
                    for (int i = 0; i < choices.Count; i++)
                        if (indices.Contains(i)) result.Add(choices[i]);
                    evt.Set();
                },
                onCancel: () => { result = null; evt.Set(); },
                preChecked: pre);
            var screen = TuiManager.Instance?.ActiveScreen;
            screen?.ShowWindow(win);
            RenderWait(screen, evt, timeoutMs, win);
        }
        catch { evt.Set(); }
        return result;
    }

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
    {
        List<int>? result = null;
        using var evt = new ManualResetEventSlim(false);
        try
        {
            var win = TuiDialog.Ask(title, message, options, multiSelect,
                onSelect: idx => { result = [idx]; evt.Set(); },
                onMultiConfirm: picked => { result = picked.ToList(); evt.Set(); },
                onCancel: () => { result = null; evt.Set(); });
            var screen = TuiManager.Instance?.ActiveScreen;
            screen?.ShowWindow(win);
            RenderWait(screen, evt, timeoutMs, win);
        }
        catch { evt.Set(); }
        return result;
    }

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
        int result = 2; // 默认拒绝
        using var evt = new ManualResetEventSlim(false);
        try
        {
            var screen = TuiManager.Instance?.ActiveScreen;
            TuiWindow win = allowAll
                ? TuiDialog.Permission(title, message, r =>
                {
                    result = r switch
                    {
                        TuiDialog.EDialogResult.Yes => 0,
                        TuiDialog.EDialogResult.Ok => 1,
                        _ => 2
                    };
                    evt.Set();
                })
                : TuiDialog.Confirm(title, message, r =>
                {
                    result = r ? 0 : 2;
                    evt.Set();
                });
            screen?.ShowWindow(win);
            RenderWait(screen, evt, timeoutMs, win);
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
            // 单帧 try/catch：渲染/读键一帧异常不逃逸（否则 evt 永不置位 → 对话框永久卡死），
            // 下一帧照常重绘，窗口栈不残留。
            try
            {
                if (ownLoop)
                {
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
