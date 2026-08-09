using CoreCoderSharp.Terminal;
using CoreCoderSharp.UI.Controls;

namespace CoreCoderSharp.UI;

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

    /// <summary>信息提示（TUI: 弹出对话框, Console: 旧 TuiBox）</summary>
    public static void Info(string title, string message)
    {
        if (IsTuiMode)
            ShowNotification(TuiDialog.Info(title, message));
        else
            TuiBox.Info(title, message);
    }

    /// <summary>成功提示</summary>
    public static void Success(string title, string message)
    {
        if (IsTuiMode)
            ShowNotification(TuiDialog.Success(title, message));
        else
            TuiBox.Success(title, message);
    }

    /// <summary>警告提示</summary>
    public static void Warn(string title, string message)
    {
        if (IsTuiMode)
            ShowNotification(TuiDialog.Warn(title, message));
        else
            TuiBox.Warn(title, message);
    }

    /// <summary>错误提示</summary>
    public static void Error(string title, string message)
    {
        if (IsTuiMode)
            ShowNotification(TuiDialog.Error(title, message));
        else
            TuiBox.Error(title, message);
    }

    /// <summary>TUI 模式下显示通知对话框（非阻塞，fire-and-forget）</summary>
    private static void ShowNotification(TuiWindow win)
    {
        try
        {
            var screen = TuiManager.Instance?.ActiveScreen;
            if (screen != null)
            {
                screen.ShowWindow(win);
                // 非阻塞 — 用户关闭对话框后自动消失
            }
        }
        catch { /* 静默回退 */ }
    }

    // ── 文本输入 ──

    /// <summary>普通文本输入（TUI: 弹出输入框, Console: 行读取）</summary>
    public static string Ask(string prompt, string? defaultValue = null)
    {
        if (IsTuiMode)
            return ShowInputDialog(prompt, defaultValue ?? "") ?? defaultValue ?? "";
        return TuiPrompt.Ask(prompt, defaultValue);
    }

    /// <summary>TUI 模式下弹出输入对话框并等待结果</summary>
    private static string? ShowInputDialog(string prompt, string defaultValue)
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
            RenderWait(screen, evt);
        }
        catch { evt.Set(); }
        return result;
    }

    // ── 选择列表 ──

    /// <summary>单选列表（TUI: 弹出 Select 对话框, Console: 数字选择）</summary>
    public static string? Select(string title, List<string> choices)
    {
        if (choices.Count == 0) return null;
        if (IsTuiMode)
            return ShowSelectDialog(title, choices);
        return TuiList.Select(title, choices);
    }

    /// <summary>TUI 模式下弹出选择对话框</summary>
    private static string? ShowSelectDialog(string title, List<string> choices)
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
            RenderWait(screen, evt);
        }
        catch { evt.Set(); }
        return result;
    }

    // ── 事件循环 ──

    /// <summary>渲染等待对话框关闭（带键盘事件循环）</summary>
    private static void RenderWait(TuiScreen? screen, ManualResetEventSlim evt)
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
            // 超时保护：30s
            if (Environment.TickCount64 - start > 30_000) break;
        }
        manager?.Render();
    }
}
