using WayCoder.Terminal;
using WayCoder.UI;
using WayCoder.UI.TuiScreens;

namespace WayCoder;

/// <summary>
/// 权限确认系统 —— 危险操作执行前弹窗确认。
///
/// 四种模式：
///   Ask       — 每次都确认（默认）
///   Auto      — 首次确认后会话内自动允许
///   SmartAuto — 智能分级：Safe 放行 / Cautious 记一次 / Dangerous 每次确认，连续 3 次阻止退回 Ask
///   Yolo      — 不确认直接执行（上帝模式）
/// </summary>
public static class PermissionManager
{
    public enum Mode { Ask, Auto, SmartAuto, Yolo }

    public static Mode CurrentMode { get; set; } = Mode.Ask;

    /// <summary>权限确认框显示时触发（用于状态栏槽位标记"等待权限"）</summary>
    public static event Action<string>? PermissionPromptStarted;
    /// <summary>权限确认框关闭后触发（恢复"工作"状态）</summary>
    public static event Action<string>? PermissionPromptResolved;

    /// <summary>智能模式退回手动时触发（用于 UI 提示）</summary>
    public static event Action<string>? ModeFallbackTriggered;

    /// <summary>本轮已自动允许的工具调用 ID 集合（Auto / SmartAuto 模式用）。线程安全：并行子智能体 + 多槽位并发访问。</summary>
    private static readonly ThreadSafeStringSet AutoAllowed = new();

    /// <summary>串行化确认弹框：并行子智能体并发请求 shell 权限时逐个排队，避免抢键盘/渲染竞态。</summary>
    private static readonly SemaphoreSlim ConfirmLock = new(1, 1);

    /// <summary>需要确认的工具名列表（传统模式用）</summary>
    private static readonly HashSet<string> DangerousTools =
        ["bash", "write_file", "edit_file", "notebook_edit", "multiedit", "agent", "kill", "rm", "download",
         "test", "cp", "mv", "find_replace"];

    /// <summary>测试钩子：判断工具名是否在需确认名单中（验证权限绕过修复，如 test/cp/mv/find_replace）。</summary>
    internal static bool IsDangerousTool(string toolName) => DangerousTools.Contains(toolName);

    static PermissionManager()
    {
        // 订阅智能分类器的退回事件
        AutoModeClassifier.FallbackToManualTriggered += () =>
        {
            CurrentMode = Mode.Ask;
            AutoAllowed.Clear();
            var msg = $"⚠ 连续 {AutoModeClassifier.BlockThreshold} 次拒绝危险操作，已自动退回「Ask（每次确认）」模式";
            ModeFallbackTriggered?.Invoke(msg);
        };
    }

    /// <summary>
    /// 检查是否需要确认。返回 true 表示允许执行。
    /// autoKey 用于 Auto 模式去重（如工具名+参数组合）。
    /// </summary>
    public static async Task<bool> CheckAsync(string toolName, Dictionary<string, object?> args)
    {
        // 沙箱 full-auto 模式：bash 工具已在沙箱中保护，直接放行
        if (toolName == "bash" && SandboxManager.IsSandboxed)
            return true;

        // Yolo 模式：直接放行
        if (CurrentMode == Mode.Yolo)
            return true;

        // Bash 安全只读命令：自动放行（对标 crush safeCommands 白名单）
        if (toolName == "bash" && args.TryGetValue("command", out var cmdObj) &&
            cmdObj is string cmdStr && BashGuard.IsSafeReadOnly(cmdStr))
            return true;

        // ── SmartAuto 模式：三级智能分类 ──
        if (CurrentMode == Mode.SmartAuto)
        {
            var risk = AutoModeClassifier.Classify(toolName);

            // Safe → 自动放行
            if (risk == AutoModeClassifier.RiskLevel.Safe)
                return true;

            // Cautious → 首次确认后记住（同 Auto 模式逻辑）
            if (risk == AutoModeClassifier.RiskLevel.Cautious)
            {
                var autoKey = BuildAutoKey(toolName, args);
                if (AutoAllowed.Contains(autoKey))
                    return true;

                var allowed = await ShowConfirmDialog(toolName, args, isDangerous: false);
                if (allowed)
                    AutoAllowed.Add(autoKey);
                return allowed;
            }

            // Dangerous → 每次确认，追踪连续阻止
            if (risk == AutoModeClassifier.RiskLevel.Dangerous)
            {
                var allowed = await ShowConfirmDialog(toolName, args, isDangerous: true);
                if (allowed)
                    AutoModeClassifier.RecordDangerousAllow();
                else
                    AutoModeClassifier.RecordDangerousBlock();
                return allowed;
            }
        }

        // ── 传统模式（Ask / Auto）──

        // 非危险工具直接放行
        if (!DangerousTools.Contains(toolName))
            return true;

        // Auto 模式：首次确认后记住
        var legacyAutoKey = BuildAutoKey(toolName, args);
        if (CurrentMode == Mode.Auto && AutoAllowed.Contains(legacyAutoKey))
            return true;

        var legacyAllowed = await ShowConfirmDialog(toolName, args, isDangerous: true);
        if (legacyAllowed && CurrentMode == Mode.Auto)
            AutoAllowed.Add(legacyAutoKey);
        return legacyAllowed;
    }

    /// <summary>
    /// 显示确认对话框。返回 true 表示用户允许。
    /// </summary>
    private static async Task<bool> ShowConfirmDialog(string toolName, Dictionary<string, object?> args, bool isDangerous)
    {
        // 串行化：并行子智能体并发请求确认时逐个弹框，避免抢键盘/渲染竞态
        await ConfirmLock.WaitAsync();
        try
        {
            var details = FormatArgs(toolName, args);
            var content = $"工具: {TuiHelper.Esc(toolName)}\n{TuiHelper.Esc(details)}";

            int result;
            PermissionPromptStarted?.Invoke(toolName);
            var activeScreen = TuiManager.Instance.ActiveScreen as ChatScreen;
            if (activeScreen != null)
            {
                // 提取简短摘要（第一行）和完整详情
                var lines = details.Split('\n');
                var summary = lines.Length > 0 ? lines[0] : details;
                var fullDetail = string.Join("\n", lines);
                result = activeScreen.ShowInlinePermission(toolName, summary, fullDetail, isDangerous);
            }
            else
            {
                UxHelper.Warn("确认操作", content);
                List<string> choices = isDangerous
                    ? new List<string> { "是 (y)", "否 (n)" }
                    : new List<string> { "是 (y)", "总是允许 (a)", "否 (n)" };
                var choice = UxHelper.Select("是否执行？", choices);
                result = choice switch
                {
                    "是 (y)" => 0,
                    "总是允许 (a)" => 1,
                    _ => 2
                };
            }

            switch (result)
            {
                case 1: // "总是允许" — 仅 Cautious 工具会走到这里
                    break;
                case 0: // "是"
                    break;
                default: // "否"
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
        finally
        {
            ConfirmLock.Release();
        }
    }

    /// <summary>
    /// 重置自动允许列表（切换模式时调用）。
    /// </summary>
    public static void Reset()
    {
        AutoAllowed.Clear();
        AutoModeClassifier.Reset();
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
            "smartauto" or "smart-auto" or "smart" => Mode.SmartAuto,
            "auto" => Mode.Auto,
            _ => Mode.Ask,
        };

        int color = CurrentMode switch
        {
            Mode.Yolo => TuiColors.Red,
            Mode.SmartAuto => TuiColors.Cyan,
            Mode.Auto => TuiColors.Green,
            _ => TuiColors.Yellow,
        };
        var label = CurrentMode switch
        {
            Mode.Yolo => "YOLO (上帝模式)",
            Mode.SmartAuto => "SmartAuto (智能分级)",
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
            Mode.SmartAuto => ("SmartAuto", "智能分级：Safe 放行 / Cautious 记一次 / Dangerous 每次确认", TuiColors.Cyan),
            Mode.Auto => ("Auto", "首次确认后自动允许", TuiColors.Green),
            _ => ("Ask", "每次都确认", TuiColors.Yellow),
        };

        var sandboxInfo = SandboxManager.IsSandboxed
            ? $"\n{AnsiText.Accent("沙箱:")} full-auto（bash 隔离 + 环境清理 + 内存监控）"
            : "";

        var classifierInfo = CurrentMode == Mode.SmartAuto
            ? $"\n{AnsiText.Dim("分级:")} {AutoModeClassifier.GetStats()}"
            : "";

        var content = $"当前模式: {AnsiText.Fg(label, color)} — {TuiHelper.Esc(desc)}{sandboxInfo}{classifierInfo}\n" +
            $"{AnsiText.Dim("需要确认:")} {string.Join(", ", DangerousTools)}\n" +
            $"{AnsiText.Dim("直接放行:")} read_file, glob, grep, ls, stat 等只读工具";

        UxHelper.Info("权限状态", content);
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
