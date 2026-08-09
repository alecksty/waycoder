using CoreCoderSharp.Terminal;
using CoreCoderSharp.UI;

namespace CoreCoderSharp;

/// <summary>
/// 权限确认系统 —— 危险操作执行前弹窗确认。
///
/// 三种模式：
///   Ask    — 每次都确认（默认）
///   Auto   — 首次确认后会话内自动允许
///   Yolo   — 不确认直接执行（上帝模式）
/// </summary>
public static class PermissionManager
{
    public enum Mode { Ask, Auto, Yolo }

    public static Mode CurrentMode { get; set; } = Mode.Ask;

    /// <summary>权限确认框显示时触发（用于状态栏槽位标记"等待权限"）</summary>
    public static event Action<string>? PermissionPromptStarted;
    /// <summary>权限确认框关闭后触发（恢复"工作"状态）</summary>
    public static event Action<string>? PermissionPromptResolved;

    /// <summary>本轮已自动允许的工具调用 ID 集合（Auto 模式用）</summary>
    private static readonly HashSet<string> _autoAllowed = [];

    /// <summary>需要确认的工具名列表</summary>
    private static readonly HashSet<string> DangerousTools =
        ["bash", "write_file", "edit_file", "agent", "kill", "rm"];

    /// <summary>
    /// 检查是否需要确认。返回 true 表示允许执行。
    /// autoKey 用于 Auto 模式去重（如工具名+参数组合）。
    /// </summary>
    public static async Task<bool> CheckAsync(string toolName, Dictionary<string, object?> args)
    {
        // 非危险工具直接放行
        if (!DangerousTools.Contains(toolName))
            return true;

        // 沙箱 full-auto 模式：bash 工具已在沙箱中保护，直接放行
        if (toolName == "bash" && SandboxManager.IsSandboxed)
            return true;

        // Yolo 模式：直接放行
        if (CurrentMode == Mode.Yolo)
            return true;

        // Auto 模式：首次确认后记住
        var autoKey = BuildAutoKey(toolName, args);
        if (CurrentMode == Mode.Auto && _autoAllowed.Contains(autoKey))
            return true;

        // 收集操作详情
        var details = FormatArgs(toolName, args);
        var content = $"工具: {TuiHelper.Esc(toolName)}\n{TuiHelper.Esc(details)}";

        int result;
        PermissionPromptStarted?.Invoke(toolName);
        var activeScreen = TuiManager.Instance.ActiveScreen as ChatScreen;
        if (activeScreen != null)
        {
            result = activeScreen.ShowInlinePermission(toolName, details,
                ["是 (y)", "总是允许 (a)", "否 (n)"]);
        }
        else
        {
            TuiBox.Warn("确认操作", content);
            var choice = TuiList.Select("是否执行？",
                ["是 (y)", "总是允许 (a)", "否 (n)"]);
            result = choice switch { "是 (y)" => 0, "总是允许 (a)" => 1, _ => 2 };
        }

        switch (result)
        {
            case 1:
                _autoAllowed.Add(autoKey);
                CurrentMode = Mode.Auto;
                break;
            case 0:
                break;
            default:
                if (activeScreen != null)
                    activeScreen.AddSystemMsg("已拒绝");
                else
                {
                    Console.WriteLine(AnsiText.Warn("已拒绝"));
                    Console.WriteLine();
                }
                break;
        }
        PermissionPromptResolved?.Invoke(toolName);
        return result switch { 0 => true, 1 => true, _ => false };
    }

    /// <summary>
    /// 重置自动允许列表（切换模式时调用）。
    /// </summary>
    public static void Reset()
    {
        _autoAllowed.Clear();
    }

    /// <summary>
    /// 设置模式。
    /// </summary>
    public static void SetMode(string modeName)
    {
        Reset();
        CurrentMode = modeName.ToLowerInvariant() switch
        {
            "yolo" or "god" => Mode.Yolo,
            "auto" or "smart" => Mode.Auto,
            _ => Mode.Ask,
        };

        int color = CurrentMode switch
        {
            Mode.Yolo => TuiColors.Red,
            Mode.Auto => TuiColors.Green,
            _ => TuiColors.Yellow,
        };
        var label = CurrentMode switch
        {
            Mode.Yolo => "YOLO (上帝模式)",
            Mode.Auto => "Auto (智能确认)",
            _ => "Ask (每次确认)",
        };

        Console.WriteLine($"权限模式: {AnsiText.Fg(label, color)}");
    }

    /// <summary>
    /// 显示当前权限状态。
    /// </summary>
    public static void ShowStatus()
    {
        var (label, desc, color) = CurrentMode switch
        {
            Mode.Yolo => ("YOLO", "不确认，直接执行", TuiColors.Red),
            Mode.Auto => ("Auto", "首次确认后自动允许", TuiColors.Green),
            _ => ("Ask", "每次都确认", TuiColors.Yellow),
        };

        var sandboxInfo = SandboxManager.IsSandboxed
            ? $"\n{AnsiText.Accent("沙箱:")} full-auto（bash 隔离 + 环境清理 + 内存监控）"
            : "";

        var content = $"当前模式: {AnsiText.Fg(label, color)} — {TuiHelper.Esc(desc)}{sandboxInfo}\n" +
            $"{AnsiText.Dim("需要确认:")} {string.Join(", ", DangerousTools)}\n" +
            $"{AnsiText.Dim("直接放行:")} read_file, glob, grep, ls, stat 等只读工具";

        TuiBox.Info("权限状态", content);
    }

    // ---- 内部 ----

    private static string BuildAutoKey(string toolName, Dictionary<string, object?> args)
    {
        var key = toolName;
        if (args.TryGetValue("command", out var cmd))
            key += ":" + (cmd?.ToString()?[..Math.Min(60, cmd.ToString()!.Length)] ?? "");
        else if (args.TryGetValue("file_path", out var fp))
            key += ":" + (fp?.ToString() ?? "");
        return key;
    }

    private static string FormatArgs(string toolName, Dictionary<string, object?> args)
    {
        switch (toolName)
        {
            case "bash":
                var cmd = args.GetValueOrDefault("command")?.ToString() ?? "";
                return $"命令: {TuiHelper.Esc(cmd.Length > 200 ? cmd[..200] + "..." : cmd)}";
            case "write_file":
            case "edit_file":
                var fp = args.GetValueOrDefault("file_path")?.ToString() ?? "";
                var result = $"文件: {TuiHelper.Esc(fp)}";
                if (toolName == "write_file")
                {
                    var content = args.GetValueOrDefault("content")?.ToString() ?? "";
                    var lines = content.Count(c => c == '\n') + 1;
                    var exists = File.Exists(fp);
                    var existsNote = exists ? $" (覆盖已有 {new FileInfo(fp).Length} 字节)" : " (新建)";
                    result += existsNote + $"\n内容: {lines} 行";
                    var preview = content.Length > 100 ? content[..100] + "..." : content;
                    result += $"\n预览: {TuiHelper.Esc(preview.Replace("\n", "\\n"))}";
                }
                if (toolName == "edit_file")
                {
                    var old = args.GetValueOrDefault("old_string")?.ToString() ?? "";
                    var n = args.GetValueOrDefault("new_string")?.ToString() ?? "";
                    result += $"\n-{TuiHelper.Esc(old.Length > 80 ? old[..80] + "..." : old)}";
                    result += $"\n+{TuiHelper.Esc(n.Length > 80 ? n[..80] + "..." : n)}";
                }
                return result;
            case "agent":
                var task = args.GetValueOrDefault("task")?.ToString() ?? "";
                return $"任务: {TuiHelper.Esc(task.Length > 120 ? task[..120] + "..." : task)}";
            case "kill":
                var pid = args.GetValueOrDefault("pid")?.ToString() ?? "?";
                return $"进程 ID: {TuiHelper.Esc(pid)}";
            case "rm":
                var path = args.GetValueOrDefault("path")?.ToString() ?? "?";
                return $"路径: {TuiHelper.Esc(path)}";
            default:
                return "";
        }
    }
}
