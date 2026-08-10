using System.Diagnostics;

namespace WayCoder;

/// <summary>
/// Hooks 生命周期系统 —— PreToolUse / PostToolUse 事件，Shell 脚本处理器。
/// 灵感来自 Claude Code 的 hooks 系统。
///
/// 使用方式：在 .waycoder/hooks/ (兼容 .corecoder/hooks/) 下放置可执行脚本，文件名为事件类型。
///   .waycoder/hooks/pre_tool_use.sh   — 工具调用前执行
///   .waycoder/hooks/post_tool_use.sh  — 工具调用后执行
///
/// 退出码语义（遵循 Claude Code 约定）：
///   0 = 成功 / 放行
///   2 = 阻止操作（仅 PreToolUse）
///   其他 = 警告但继续
///
/// 环境变量：
///   CORECODER_TOOL       — 工具名称
///   CORECODER_TOOL_ARGS  — JSON 格式的参数字符串
///   CORECODER_TOOL_RESULT — 工具结果（仅 PostToolUse）
///   CORECODER_EVENT      — 事件类型: pre_tool_use / post_tool_use
/// </summary>
public static class HooksManager
{
    private static string? _hooksDir;

    /// <summary>是否启用 hooks</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>
    /// 初始化 hooks 目录。自动向上查找 .waycoder/hooks/ (兼容 .corecoder/hooks/)。
    /// </summary>
    public static void Init()
    {
        var cwd = Environment.CurrentDirectory;
        var dir = cwd;
        while (dir != null)
        {
            foreach (var dirName in Global.ConfigDirSearchOrder)
            {
                var candidate = Path.Combine(dir, dirName, "hooks");
                if (Directory.Exists(candidate))
                {
                    _hooksDir = candidate;
                    return;
                }
            }
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
    }

    /// <summary>
    /// PreToolUse — 工具调用前执行。返回 null 表示放行，返回字符串表示阻止（含原因）。
    /// </summary>
    public static async Task<string?> RunPreToolUseAsync(string toolName, Dictionary<string, object?> arguments)
    {
        if (!Enabled || _hooksDir == null) return null;

        var script = FindHookScript("pre_tool_use");
        if (script == null) return null;

        var argsJson = JsonHelper.SerializeArgs(arguments);
        var result = await RunHookScriptAsync(script, toolName, argsJson, null, "pre_tool_use");

        if (result.ExitCode == 2)
            return string.IsNullOrEmpty(result.Output)
                ? $"操作被 pre_tool_use hook 阻止: {toolName}"
                : result.Output;

        return null; // 0 或其他退出码 = 放行
    }

    /// <summary>
    /// PostToolUse — 工具调用后执行。返回修改后的结果（或 null 表示不修改）。
    /// </summary>
    public static async Task<string?> RunPostToolUseAsync(string toolName, Dictionary<string, object?> arguments, string toolResult)
    {
        if (!Enabled || _hooksDir == null) return null;

        var script = FindHookScript("post_tool_use");
        if (script == null) return null;

        var argsJson = JsonHelper.SerializeArgs(arguments);
        var result = await RunHookScriptAsync(script, toolName, argsJson, toolResult, "post_tool_use");

        // PostToolUse: stdout 可用于修改工具结果
        if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
            return result.Output;

        return null;
    }

    // ========================================================================
    // 内部实现
    // ========================================================================

    private static string? FindHookScript(string eventName)
    {
        if (_hooksDir == null) return null;

        // 按优先级查找: .sh > .ps1 > .bat > .cmd
        var extensions = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new[] { ".ps1", ".bat", ".cmd", ".sh" }
            : new[] { ".sh", ".bash", ".py" };

        foreach (var ext in extensions)
        {
            var candidate = Path.Combine(_hooksDir, $"{eventName}{ext}");
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private static async Task<(int ExitCode, string Output)> RunHookScriptAsync(
        string scriptPath, string toolName, string argsJson, string? toolResult, string eventType)
    {
        try
        {
            var (fileName, arguments) = GetRunner(scriptPath);

            var envVars = new Dictionary<string, string>
            {
                ["WAYCODER_TOOL"] = toolName,
                ["WAYCODER_TOOL_ARGS"] = argsJson,
                ["WAYCODER_EVENT"] = eventType,
                // 旧名兼容
                ["CORECODER_TOOL"] = toolName,
                ["CORECODER_TOOL_ARGS"] = argsJson,
                ["CORECODER_EVENT"] = eventType,
            };
            if (toolResult != null)
            {
                envVars["WAYCODER_TOOL_RESULT"] = toolResult;
                envVars["CORECODER_TOOL_RESULT"] = toolResult;
            }

            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                }
            };

            foreach (var (key, value) in envVars)
                proc.StartInfo.Environment[key] = value;

            proc.Start();

            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();

            if (!proc.WaitForExit(10_000))
            {
                try { proc.Kill(); } catch { }
                return (-1, "Hook 超时（10 秒）");
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            var output = string.IsNullOrEmpty(stderr) ? stdout.Trim() : stderr.Trim();

            DebugLog.Log("hooks", $"[{eventType}] {toolName} → exit={proc.ExitCode} output={output[..Math.Min(output.Length, 200)]}");

            return (proc.ExitCode, output);
        }
        catch (Exception ex)
        {
            DebugLog.Log("hooks", $"Hook 异常: {ex.Message}");
            return (0, ""); // Hook 失败不影响主流程
        }
    }

    /// <summary>获取脚本的运行器（shell 和参数）</summary>
    private static (string FileName, string Arguments) GetRunner(string scriptPath)
    {
        var ext = Path.GetExtension(scriptPath).ToLowerInvariant();

        if (ext == ".ps1")
            return ("powershell", $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"");

        if (ext is ".bat" or ".cmd")
            return ("cmd", $"/c \"{scriptPath}\"");

        if (ext == ".py")
            return ("python3", $"\"{scriptPath}\"");

        // .sh / .bash
        return ("bash", $"\"{scriptPath}\"");
    }
}
