using Spectre.Console;

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

        // Yolo 模式：直接放行
        if (CurrentMode == Mode.Yolo)
            return true;

        // Auto 模式：首次确认后记住
        var autoKey = BuildAutoKey(toolName, args);
        if (CurrentMode == Mode.Auto && _autoAllowed.Contains(autoKey))
            return true;

        // 展示操作内容并询问
        Console.WriteLine();
        Spectre.Console.AnsiConsole.MarkupLine($"[bold yellow]⚠ 确认操作[/]");
        Spectre.Console.AnsiConsole.MarkupLine($"  工具: [cyan]{toolName}[/]");
        PrintArgs(toolName, args);
        Console.WriteLine();

        var choice = Spectre.Console.AnsiConsole.Prompt(
            new Spectre.Console.SelectionPrompt<string>()
                .Title("[bold]是否执行？[/]")
                .AddChoices(["是 (y)", "总是允许 (a)", "否 (n)"]));

        switch (choice)
        {
            case "总是允许 (a)":
                _autoAllowed.Add(autoKey);
                CurrentMode = Mode.Auto;
                Console.WriteLine();
                return true;
            case "是 (y)":
                Console.WriteLine();
                return true;
            default:
                Spectre.Console.AnsiConsole.MarkupLine("[orange3]已拒绝[/]");
                Console.WriteLine();
                return false;
        }
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

        var label = CurrentMode switch
        {
            Mode.Yolo => "[red]YOLO (上帝模式)[/]",
            Mode.Auto => "[green]Auto (智能确认)[/]",
            _ => "[yellow]Ask (每次确认)[/]",
        };

        Spectre.Console.AnsiConsole.MarkupLine($"权限模式: {label}");
    }

    /// <summary>
    /// 显示当前权限状态。
    /// </summary>
    public static void ShowStatus()
    {
        var label = CurrentMode switch
        {
            Mode.Yolo => "[red]YOLO[/] — 不确认，直接执行",
            Mode.Auto => "[green]Auto[/] — 首次确认后自动允许",
            _ => "[yellow]Ask[/] — 每次都确认",
        };
        Spectre.Console.AnsiConsole.MarkupLine($"[bold]权限模式:[/] {label}");
        Spectre.Console.AnsiConsole.MarkupLine($"[dim]需要确认的工具:[/] {string.Join(", ", DangerousTools)}");
        Spectre.Console.AnsiConsole.MarkupLine($"[dim]安全工具 (直接执行):[/] read_file, glob, grep");
    }

    private static string BuildAutoKey(string toolName, Dictionary<string, object?> args)
    {
        // 用工具名 + 第一个关键参数构建去重 key
        var key = toolName;
        if (args.TryGetValue("command", out var cmd))
            key += ":" + (cmd?.ToString()?[..Math.Min(60, cmd.ToString()!.Length)] ?? "");
        else if (args.TryGetValue("file_path", out var fp))
            key += ":" + (fp?.ToString() ?? "");
        return key;
    }

    private static void PrintArgs(string toolName, Dictionary<string, object?> args)
    {
        switch (toolName)
        {
            case "bash":
                var cmd = args.GetValueOrDefault("command")?.ToString() ?? "";
                Spectre.Console.AnsiConsole.MarkupLine($"  命令: [dim]{Markup.Escape(cmd)}[/]");
                break;
            case "write_file":
            case "edit_file":
                var fp = args.GetValueOrDefault("file_path")?.ToString() ?? "";
                Spectre.Console.AnsiConsole.MarkupLine($"  文件: [dim]{Markup.Escape(fp)}[/]");
                if (toolName == "edit_file")
                {
                    var old = args.GetValueOrDefault("old_string")?.ToString() ?? "";
                    var n = args.GetValueOrDefault("new_string")?.ToString() ?? "";
                    Spectre.Console.AnsiConsole.MarkupLine($"  旧: [dim]{Markup.Escape(old.Length > 80 ? old[..80] + "..." : old)}[/]");
                    Spectre.Console.AnsiConsole.MarkupLine($"  新: [dim]{Markup.Escape(n.Length > 80 ? n[..80] + "..." : n)}[/]");
                }
                break;
            case "agent":
                var task = args.GetValueOrDefault("task")?.ToString() ?? "";
                Spectre.Console.AnsiConsole.MarkupLine($"  任务: [dim]{Markup.Escape(task.Length > 120 ? task[..120] + "..." : task)}[/]");
                break;
        }
    }
}
