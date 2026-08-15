using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace WayCoder;

/// <summary>
/// Hook 事件类型（对标 Claude Code HOOK_EVENTS）。
/// </summary>
public enum HookEvent
{
    /// <summary>工具调用前（可阻止）</summary>
    PreToolUse,
    /// <summary>工具调用成功后</summary>
    PostToolUse,
    /// <summary>工具调用失败后</summary>
    PostToolUseFailure,
    /// <summary>会话启动时</summary>
    SessionStart,
    /// <summary>会话结束时</summary>
    SessionEnd,
    /// <summary>Agent 完成一轮后</summary>
    Stop,
    /// <summary>上下文压缩前</summary>
    PreCompact,
    /// <summary>通知事件（权限提示等）</summary>
    Notification,
}

/// <summary>
/// Hook 脚本 JSON 输出协议（可选）。
/// 脚本 stdout 可返回纯文本（兼容旧格式）或 JSON（结构化控制）。
/// </summary>
public class HookOutput
{
    /// <summary>是否继续后续流程（false=阻止）</summary>
    public bool Continue { get; set; } = true;
    /// <summary>决策：approve/block（仅 PreToolUse 生效）</summary>
    public string? Decision { get; set; }
    /// <summary>决策原因（显示给用户）</summary>
    public string? Reason { get; set; }
    /// <summary>系统消息（显示给用户，不阻止）</summary>
    public string? SystemMessage { get; set; }
    /// <summary>注入到模型上下文的额外信息</summary>
    public string? AdditionalContext { get; set; }
}

/// <summary>
/// Hook 匹配器配置（从 hooks.json 加载）。
/// </summary>
public class HookMatcherConfig
{
    /// <summary>匹配模式：空/"*"=全部，管道分隔 "bash|git"，正则 "^Write"</summary>
    public string? Matcher { get; set; }
    /// <summary>适用于哪些事件</summary>
    public string[]? Events { get; set; }
    /// <summary>Hook 列表</summary>
    public HookCommandConfig[]? Hooks { get; set; }
}

/// <summary>
/// Hook 命令配置（command 类型）。
/// </summary>
public class HookCommandConfig
{
    /// <summary>类型：目前支持 "command"</summary>
    public string Type { get; set; } = "command";
    /// <summary>脚本路径（相对于 hooks 目录）</summary>
    public string Command { get; set; } = "";
    /// <summary>超时秒数（0=默认）</summary>
    public int Timeout { get; set; }
}

/// <summary>
/// Session Hook 上下文。
/// </summary>
public class HookContext
{
    public HookEvent EventType { get; set; }
    public string? ToolName { get; set; }
    public Dictionary<string, object?>? Arguments { get; set; }
    public string? ToolResult { get; set; }
    public string? MatchValue { get; set; }
}

/// <summary>
/// Session Hook 委托。返回 null=不影响流程，返回非 null=阻止/修改。
/// </summary>
public delegate Task<HookOutput?> SessionHookDelegate(HookContext ctx);

/// <summary>
/// Hooks 生命周期系统 —— 对标 Claude Code 的 hooks 架构。
///
/// 支持：
/// - 8 种事件类型（PreToolUse / PostToolUse / PostToolUseFailure / SessionStart / SessionEnd / Stop / PreCompact / Notification）
/// - hooks.json matcher 系统（通配符 + 正则匹配）
/// - JSON 输出协议（可选结构化控制，兼容旧纯文本格式）
/// - Session hooks（内存级动态注册，供 Skill/插件使用）
///
/// 目录结构：
///   .waycoder/hooks/
///   ├── pre_tool_use.sh        ← 文件名匹配（兼容）
///   ├── hooks.json             ← matcher 配置（可选）
///
/// 退出码语义：
///   0 = 成功 / 放行
///   2 = 阻止操作（仅 PreToolUse）
///   其他 = 警告但继续
/// </summary>
public static class HooksManager
{
    private static string? _hooksDir;
    private static List<HookMatcherConfig> _matchers = [];
    private static readonly ConcurrentDictionary<string, SessionHookDelegate> _sessionHooks = new();

    /// <summary>是否启用 hooks</summary>
    public static bool Enabled { get; set; } = true;

    // ═══════════════════════════════════════════════════════════════
    // 初始化
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 初始化 hooks 目录。自动向上查找 .waycoder/hooks/ (兼容 .corecoder/hooks/)。
    /// 加载 hooks.json（若存在）。
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
                    LoadMatchers();
                    return;
                }
            }
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
    }

    /// <summary>加载 hooks.json matcher 配置</summary>
    private static void LoadMatchers()
    {
        if (_hooksDir == null) return;
        var jsonPath = Path.Combine(_hooksDir, "hooks.json");
        if (!File.Exists(jsonPath)) return;

        try
        {
            var json = File.ReadAllText(jsonPath);
            // AOT 安全：Json.Parse 手写解析，避免 JsonSerializer.Deserialize 反射
            var list = new List<HookMatcherConfig>();
            if (Json.Parse(json) is { Kind: JKind.Object } root && root["matchers"] is { Kind: JKind.Array } matchersArr)
            {
                foreach (var item in matchersArr.Items)
                {
                    if (item.Kind != JKind.Object) continue;

                    var mc = new HookMatcherConfig
                    {
                        Matcher = item["matcher"]?.AsString(),
                    };

                    if (item["events"] is { Kind: JKind.Array } eventsArr)
                        mc.Events = eventsArr.Items
                            .Select(e => e.AsString())
                            .Where(s => s != null)
                            .Select(s => s!)
                            .ToArray();

                    if (item["hooks"] is { Kind: JKind.Array } hooksArr)
                    {
                        var hooks = new List<HookCommandConfig>();
                        foreach (var h in hooksArr.Items)
                        {
                            if (h.Kind != JKind.Object) continue;
                            hooks.Add(new HookCommandConfig
                            {
                                Type = h["type"]?.AsString() ?? "command",
                                Command = h["command"]?.AsString() ?? "",
                                Timeout = h["timeout"] is { Kind: JKind.Number } tv ? (int)tv.AsNumber() : 0,
                            });
                        }
                        mc.Hooks = hooks.ToArray();
                    }

                    list.Add(mc);
                }
            }
            _matchers = list;
        }
        catch (Exception ex)
        {
            DebugLog.Log("hooks", $"Failed to load hooks.json: {ex.Message}");
            _matchers = [];
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 公共 API —— 事件执行
    // ═══════════════════════════════════════════════════════════════

    /// <summary>PreToolUse — 工具调用前执行。返回 null=放行，非 null=阻止（含原因）。</summary>
    public static async Task<string?> RunPreToolUseAsync(string toolName, Dictionary<string, object?> arguments)
    {
        var ctx = new HookContext { EventType = HookEvent.PreToolUse, ToolName = toolName, Arguments = arguments, MatchValue = toolName };
        var results = await RunEventAsync(HookEvent.PreToolUse, toolName, arguments, null);
        foreach (var result in results)
        {
            if (result.Decision == "block" || !result.Continue)
                return result.Reason ?? $"操作被 PreToolUse hook 阻止: {toolName}";
        }
        return null;
    }

    /// <summary>PostToolUse — 工具调用后执行。返回修改后的结果（或 null=不修改）。</summary>
    public static async Task<string?> RunPostToolUseAsync(string toolName, Dictionary<string, object?> arguments, string toolResult)
    {
        var results = await RunEventAsync(HookEvent.PostToolUse, toolName, arguments, toolResult);
        foreach (var result in results)
        {
            if (!string.IsNullOrWhiteSpace(result.AdditionalContext))
                return result.AdditionalContext;
        }
        return null;
    }

    /// <summary>PostToolUseFailure — 工具调用失败后执行。</summary>
    public static async Task RunPostToolUseFailureAsync(string toolName, Dictionary<string, object?> arguments, string errorResult)
    {
        await RunEventAsync(HookEvent.PostToolUseFailure, toolName, arguments, errorResult);
    }

    /// <summary>SessionStart — 会话启动时触发（fire-and-forget）。</summary>
    public static void RunSessionStart(string source)
    {
        if (!Enabled) return;
        _ = Task.Run(async () =>
        {
            try { await RunEventAsync(HookEvent.SessionStart, source, null, null); }
            catch { /* fire-and-forget */ }
        });
    }

    /// <summary>SessionEnd — 会话结束时触发（fire-and-forget）。</summary>
    public static void RunSessionEnd(string reason)
    {
        if (!Enabled) return;
        _ = Task.Run(async () =>
        {
            try { await RunEventAsync(HookEvent.SessionEnd, reason, null, null); }
            catch { /* fire-and-forget */ }
        });
    }

    /// <summary>Stop — Agent 完成一轮后。返回 null=无特殊处理，非 null=追加到上下文。</summary>
    public static async Task<string?> RunStopAsync()
    {
        var results = await RunEventAsync(HookEvent.Stop, "", null, null);
        foreach (var result in results)
        {
            if (!string.IsNullOrWhiteSpace(result.AdditionalContext))
                return result.AdditionalContext;
        }
        return null;
    }

    /// <summary>PreCompact — 上下文压缩前。返回 null=无特殊处理，非 null=追加到上下文。</summary>
    public static async Task<string?> RunPreCompactAsync(string trigger)
    {
        var results = await RunEventAsync(HookEvent.PreCompact, trigger, null, null);
        foreach (var result in results)
        {
            if (!string.IsNullOrWhiteSpace(result.AdditionalContext))
                return result.AdditionalContext;
        }
        return null;
    }

    /// <summary>Notification — 通知事件（fire-and-forget）。</summary>
    public static void RunNotification(string notificationType, string? message = null)
    {
        if (!Enabled) return;
        _ = Task.Run(async () =>
        {
            try { await RunEventAsync(HookEvent.Notification, notificationType, null, message); }
            catch { /* fire-and-forget */ }
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // Session Hooks API
    // ═══════════════════════════════════════════════════════════════

    /// <summary>注册 session hook（会话存活期内有效，返回 ID 用于注销）。</summary>
    public static string RegisterSessionHook(HookEvent eventType, SessionHookDelegate hook)
    {
        var id = $"{eventType}_{Guid.NewGuid():N}";
        _sessionHooks[id] = hook;
        return id;
    }

    /// <summary>注销 session hook。</summary>
    public static void UnregisterSessionHook(string hookId)
    {
        _sessionHooks.TryRemove(hookId, out _);
    }

    /// <summary>清空所有 session hooks（会话退出时调用）。</summary>
    public static void ClearSessionHooks()
    {
        _sessionHooks.Clear();
    }

    // ═══════════════════════════════════════════════════════════════
    // 核心执行引擎
    // ═══════════════════════════════════════════════════════════════

    /// <summary>执行指定事件的所有匹配 hook（文件名 + hooks.json + session hooks），返回解析后的输出列表。</summary>
    private static async Task<List<HookOutput>> RunEventAsync(
        HookEvent eventType, string matchValue,
        Dictionary<string, object?>? arguments, string? toolResult)
    {
        var outputs = new List<HookOutput>();
        if (!Enabled) return outputs;

        // 1. Session hooks（最高优先级）
        var eventName = eventType.ToString();
        foreach (var kv in _sessionHooks)
        {
            if (!kv.Key.StartsWith(eventName)) continue;
            try
            {
                var ctx = new HookContext
                {
                    EventType = eventType,
                    ToolName = matchValue,
                    Arguments = arguments,
                    ToolResult = toolResult,
                    MatchValue = matchValue,
                };
                var output = await kv.Value(ctx);
                if (output != null) outputs.Add(output);
            }
            catch (Exception ex)
            {
                DebugLog.Log("hooks", $"Session hook error [{eventName}]: {ex.Message}");
            }
        }

        // 2. 文件名匹配的脚本（兼容旧格式）
        if (_hooksDir != null)
        {
            var script = FindHookScript(SnakeCase(eventName));
            if (script != null)
            {
                var eventNameSnake = SnakeCase(eventName);
                var argsJson = arguments != null ? JsonHelper.SerializeArgs(arguments) : "{}";
                var (exitCode, stdout) = await RunHookScriptAsync(script, matchValue, argsJson, toolResult, eventNameSnake);
                var output = ParseHookOutput(stdout, exitCode);
                if (output != null) outputs.Add(output);
            }
        }

        // 3. hooks.json matcher 匹配的脚本
        if (_hooksDir != null && _matchers.Count > 0)
        {
            foreach (var matcher in _matchers)
            {
                // 检查事件是否匹配
                if (matcher.Events != null && matcher.Events.Length > 0 &&
                    !matcher.Events.Contains(eventName, StringComparer.OrdinalIgnoreCase))
                    continue;

                // 检查 matcher 模式是否匹配
                if (!MatchesPattern(matchValue, matcher.Matcher))
                    continue;

                // 执行匹配的 hooks
                if (matcher.Hooks != null)
                {
                    foreach (var hook in matcher.Hooks)
                    {
                        if (hook.Type != "command") continue;
                        var scriptPath = Path.Combine(_hooksDir, hook.Command);
                        if (!File.Exists(scriptPath)) continue;

                        var argsJson = arguments != null ? JsonHelper.SerializeArgs(arguments) : "{}";
                        var timeout = hook.Timeout > 0 ? hook.Timeout : Config.Instance.HookTimeoutSec;
                        var (exitCode, stdout) = await RunHookScriptAsync(scriptPath, matchValue, argsJson, toolResult, SnakeCase(eventName), timeout);
                        var output = ParseHookOutput(stdout, exitCode);
                        if (output != null) outputs.Add(output);
                    }
                }
            }
        }

        return outputs;
    }

    /// <summary>解析 hook 脚本输出：先尝试 JSON，失败则作为纯文本（兼容旧 hook）。</summary>
    internal static HookOutput? ParseHookOutput(string stdout, int exitCode)
    {
        if (string.IsNullOrWhiteSpace(stdout) && exitCode == 0) return null;

        // 尝试 JSON 解析（AOT 安全：JsonNode.Parse 手写解析，避免 JsonSerializer.Deserialize
        // 反射在 NativeAOT 下抛 JsonSerializerIsReflectionDisabled）
        var trimmed = stdout.Trim();
        if (trimmed.StartsWith('{'))
        {
            try
            {
                if (Json.Parse(trimmed) is { Kind: JKind.Object } obj)
                {
                    return new HookOutput
                    {
                        Continue = GetJsonBool(obj, "continue", "Continue") ?? true,
                        Decision = GetJsonString(obj, "decision", "Decision"),
                        Reason = GetJsonString(obj, "reason", "Reason"),
                        SystemMessage = GetJsonString(obj, "systemMessage", "SystemMessage"),
                        AdditionalContext = GetJsonString(obj, "additionalContext", "AdditionalContext"),
                    };
                }
            }
            catch
            {
                // JSON 解析失败，回退到纯文本处理
            }
        }

        // 纯文本回退（兼容旧 hook）
        if (exitCode == 2)
        {
            return new HookOutput
            {
                Continue = false,
                Decision = "block",
                Reason = trimmed,
            };
        }

        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            return new HookOutput
            {
                AdditionalContext = trimmed,
            };
        }

        return null;
    }

    /// <summary>从 JsonObject 提取字符串字段（多个候选键名，先命中的返回）。</summary>
    private static string? GetJsonString(JNode obj, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (obj[key] is { Kind: JKind.String } v)
                return v.AsString();
        }
        return null;
    }

    /// <summary>从 JsonObject 提取布尔字段（多个候选键名）。</summary>
    private static bool? GetJsonBool(JNode obj, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (obj[key] is { Kind: JKind.Bool } v)
                return v.AsBool();
        }
        return null;
    }

    // ═══════════════════════════════════════════════════════════════
    // Matcher 模式匹配
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 检查 matchValue 是否匹配给定的 matcher 模式。
    /// 空/null/"*" = 全匹配；管道分隔 = 任一匹配；其他 = 正则匹配。
    /// </summary>
    internal static bool MatchesPattern(string matchValue, string? matcher)
    {
        if (string.IsNullOrEmpty(matcher) || matcher == "*")
            return true;

        // 管道分隔：bash|git|rm
        if (matcher.Contains('|') && !matcher.StartsWith('^') && !matcher.EndsWith('$'))
        {
            var parts = matcher.Split('|');
            foreach (var part in parts)
            {
                if (string.Equals(part.Trim(), matchValue, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        // 正则匹配
        try
        {
            return Regex.IsMatch(matchValue, matcher, RegexOptions.IgnoreCase);
        }
        catch
        {
            // 正则无效，回退到精确匹配
            return string.Equals(matcher, matchValue, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 工具方法
    // ═══════════════════════════════════════════════════════════════

    /// <summary>将 PascalCase 转为 snake_case（如 PreToolUse → pre_tool_use）</summary>
    internal static string SnakeCase(string pascal)
    {
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < pascal.Length; i++)
        {
            if (i > 0 && char.IsUpper(pascal[i]))
                result.Append('_');
            result.Append(char.ToLowerInvariant(pascal[i]));
        }
        return result.ToString();
    }

    private static string? FindHookScript(string eventName)
    {
        if (_hooksDir == null) return null;

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
        string scriptPath, string toolName, string argsJson, string? toolResult, string eventType, int timeoutSec = 0)
    {
        try
        {
            var (fileName, arguments) = GetRunner(scriptPath);

            var envVars = new Dictionary<string, string>
            {
                ["WAYCODER_TOOL"] = toolName,
                ["WAYCODER_TOOL_ARGS"] = argsJson,
                ["WAYCODER_EVENT"] = eventType,
            };
            if (toolResult != null)
            {
                envVars["WAYCODER_TOOL_RESULT"] = toolResult;
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

            var actualTimeout = timeoutSec > 0 ? timeoutSec * 1000 : Config.Instance.HookTimeoutSec * 1000;
            var exitTask = proc.WaitForExitAsync();
            var delayTask = Task.Delay(actualTimeout);
            var completed = await Task.WhenAny(exitTask, delayTask);
            if (completed != exitTask || !exitTask.IsCompletedSuccessfully)
            {
                try { proc.Kill(); } catch { }
                return (-1, $"Hook 超时（{actualTimeout / 1000} 秒）");
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
            return (0, "");
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
