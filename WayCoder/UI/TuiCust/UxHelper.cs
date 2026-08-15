using WayCoder.Terminal;
using WayCoder.UI.TuiControls;
using WayCoder.UI.TuiScreens;

namespace WayCoder.UI;

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

    // ── 通知消息 ──

    public static void Info(string title, string message)
    {
        if (IsTuiMode)
            ShowNotification(TuiDialog.Info(title, message));
        else
            Console.WriteLine($"{AnsiTty.Accent($"[ℹ {title}]")} {message}");
    }

    public static void Success(string title, string message)
    {
        if (IsTuiMode)
            ShowNotification(TuiDialog.Success(title, message));
        else
            Console.WriteLine($"{AnsiTty.Success($"[✓ {title}]")} {message}");
    }

    public static void Warn(string title, string message)
    {
        if (IsTuiMode)
            ShowNotification(TuiDialog.Warn(title, message));
        else
            Console.WriteLine($"{AnsiTty.Warn($"[⚠ {title}]")} {message}");
    }

    public static void Error(string title, string message)
    {
        if (IsTuiMode)
            ShowNotification(TuiDialog.Error(title, message));
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
    public static List<string>? MultiSelect(string title, List<string> choices, int timeoutMs = 30_000)
    {
        if (choices.Count == 0) return new List<string>();

        if (IsTuiMode)
            return ShowMultiSelectDialog(title, choices, timeoutMs);

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

    private static List<string>? ShowMultiSelectDialog(string title, List<string> choices, int timeoutMs)
    {
        List<string>? result = null;
        using var evt = new ManualResetEventSlim(false);
        try
        {
            var win = TuiDialog.MultiSelect(title, choices,
                onConfirm: indices =>
                {
                    result = new List<string>();
                    for (int i = 0; i < choices.Count; i++)
                        if (indices.Contains(i)) result.Add(choices[i]);
                    evt.Set();
                },
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
                        TuiDialog.DialogResult.Yes => 0,
                        TuiDialog.DialogResult.Ok => 1,
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
    public static void RenderWait(TuiScreen? screen, ManualResetEventSlim evt, int timeoutMs = 30_000, TuiWindow? win = null)
    {
        if (screen == null) { evt.Wait(TimeSpan.FromSeconds(30)); return; }
        var manager = TuiManager.Instance;
        var start = Environment.TickCount64;
        while (!evt.IsSet)
        {
            manager?.Render();
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                screen.OnKey(key);
            }
            else
            {
                Thread.Sleep(30);
            }
            if (timeoutMs > 0 && Environment.TickCount64 - start > timeoutMs) break;
        }
        // 超时兜底：关闭仍残留的模态窗口，避免窗口停在屏幕上
        if (!evt.IsSet && win != null)
            screen.CloseWindow(win);
        manager?.Render();
    }
}
