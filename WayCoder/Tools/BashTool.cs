using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WayCoder.Tools;

/// <summary>
/// 带安全检查的 Shell 命令执行。
/// </summary>
public class BashTool : ITool
{
    public string Name => "bash";
    public string Description => "执行 Shell 命令。返回 stdout、stderr 和退出码。\n⚠ 禁止执行：网络下载工具(curl/wget/ssh)、包管理器安装(apt/pip/npm install 等)、权限提升(sudo/su)、系统修改。\n✅ 安全免确认：ls/cat/grep/find/git log/dotnet --version 等只读操作自动放行。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["command"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "要运行的 Shell 命令",
            },
            ["timeout"] = new JsonObject
            {
                ["type"] = "integer",
                ["description"] = "超时时间，单位秒（默认 120）",
            },
            ["run_in_background"] = new JsonObject
            {
                ["type"] = "boolean",
                ["description"] = "设为 true 则后台运行，立即返回 shell_id。之后用 job_output 读取输出，用 job_kill 终止。默认 60 秒后自动转入后台。",
            },
            ["auto_background_after"] = new JsonObject
            {
                ["type"] = "integer",
                ["description"] = "前台等待 N 秒后自动转入后台（默认 60 秒）。仅 run_in_background=true 时生效。",
            },
        },
        ["required"] = new JsonArray("command"),
    };

    /// <summary>
    /// 跨命令跟踪 cwd。AsyncLocal 确保每个异步上下文
    /// 跟踪自己的工作目录，并行调用不会产生竞态。
    /// </summary>
    internal static readonly AsyncLocal<string> CurrentCwd = new();

    // 可能破坏文件系统或泄露密钥的危险模式
    private static readonly (Regex Pattern, string Reason)[] DangerousPatterns =
    [
        (new(@"\brm\s+(-\w*)?-r\w*\s+(/|~|\$HOME)"), "对家目录/根目录的递归删除"),
        (new(@"\brm\b(?=(?:.*\s)?-\w*[rR])(?=(?:.*\s)?-\w*f)"), "强制递归删除"),
        (new(@"\brm\b.*--recursive\b.*--force\b|\brm\b.*--force\b.*--recursive\b"), "强制递归删除"),
        (new(@"\bmkfs\b"), "格式化文件系统"),
        (new(@"\bdd\s+.*of=/dev/"), "原始磁盘写入"),
        (new(@">\s*/dev/sd[a-z]"), "覆盖块设备"),
        (new(@"\bchmod\s+(-R\s+)?777\s+/"), "对根目录 chmod 777"),
        (new(@":\(\)\s*\{.*:\|:.*\}"), "fork 炸弹"),
        (new(@"\bcurl\b.*\|\s*(sudo\s+)?(ba)?sh\b"), "curl 管道到 shell"),
        (new(@"\bwget\b.*\|\s*(sudo\s+)?(ba)?sh\b"), "wget 管道到 shell"),
    ];

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var command = arguments.GetValueOrDefault("command")?.ToString() ?? "";
        var configTimeout = Config.Instance.ToolTimeoutSec;
        var timeout = arguments.TryGetValue("timeout", out var t) && t is int ti ? ti : configTimeout;

        // 后台运行模式
        var runInBackground = arguments.TryGetValue("run_in_background", out var bg) && bg is true;
        if (runInBackground)
        {
            var autoBgAfter = arguments.TryGetValue("auto_background_after", out var aba) && aba is int abaVal
                ? abaVal : 60;
            var bgId = BackgroundTaskManager.Start(command, Math.Max(timeout, autoBgAfter + 30));
            return $"✅ 后台任务已启动\n" +
                   $"Shell ID: {bgId}\n" +
                   $"命令: {command}\n" +
                   $"使用 job_output 工具读取输出（参数 shell_id={bgId}）\n" +
                   $"使用 job_kill 工具终止任务（参数 shell_id={bgId}）";
        }

        return await Execute(command, timeout);
    }

    /// <summary>
    /// 流式执行命令，每读到一行就调用 onLine 回调。
    /// 返回完整输出（与 ExecuteAsync 格式一致）。
    /// </summary>
    public async Task<string> ExecuteStreamingAsync(
        Dictionary<string, object?> arguments,
        Func<string, Task>? onLine)
    {
        var command = arguments.GetValueOrDefault("command")?.ToString() ?? "";
        var configTimeout = Config.Instance.ToolTimeoutSec;
        var timeout = arguments.TryGetValue("timeout", out var t) && t is int ti ? ti : configTimeout;

        return await Execute(command, timeout, onLine);
    }

    private async Task<string> Execute(string command, int timeout, Func<string, Task>? onLine = null)
    {
        // BashGuard 命令黑名单检查（对标 crush 三层防护）
        var (blocked, reason) = BashGuard.CheckBanned(command);
        if (blocked)
            return $"{reason}\n命令：{command}";

        // 已有危险模式检查
        var warning = CheckDangerous(command);
        if (warning != null)
            return $"⚠ 已阻止：{warning}\n命令：{command}\n如有意执行，请修改命令使其更具体。";

        // Worktree 隔离：检测 worktree 路径，自动切换 cwd
        var worktreePath = WorktreeIsolation.CurrentWorktree;
        var cwd = worktreePath ?? CurrentCwd.Value ?? Directory.GetCurrentDirectory();

        // 沙箱检查（full-auto 模式）
        if (SandboxManager.IsSandboxed)
        {
            var violation = SandboxManager.CheckSandboxViolation(command, cwd);
            if (violation != null)
                return $"⛔ 沙箱阻止：{violation}\n命令：{command}";
        }

        try
        {
            ProcessStartInfo psi;

            if (SandboxManager.IsSandboxed)
            {
                psi = SandboxManager.CreateSandboxedProcess(command, cwd);
            }
            else
            {
                psi = new ProcessStartInfo
                {
                    FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash",
                    Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? $"/c \"{command}\""
                        : $"-c \"{command.Replace("\"", "\\\"")}\"",
                    WorkingDirectory = cwd,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
            }

            using var proc = Process.Start(psi)!;

            // 流式模式：逐行读取 stdout 并回调（沙箱模式不支持流式）
            if (onLine != null && !SandboxManager.IsSandboxed)
                return await ExecuteStreaming(proc, command, cwd, timeout, onLine);

            // 非流式模式：保持原有 ReadToEndAsync 逻辑
            // 立即启动异步读取（防止管道缓冲区满导致死锁）
            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();

            // 沙箱模式：后台监控内存 + CPU 时间（使用实际处理器时间，非墙上时钟）
            var sandboxCts = new CancellationTokenSource();
            Task<string?>? memTask = null;
            Task<string?>? cpuTask = null;
            if (SandboxManager.IsSandboxed)
            {
                memTask = SandboxManager.MonitorMemoryAsync(proc, sandboxCts.Token);
                cpuTask = SandboxManager.MonitorCpuAsync(proc, sandboxCts.Token);
            }

            // 异步等待进程退出（不阻塞线程）
            var exitTask = proc.WaitForExitAsync();
            var delayTask = Task.Delay(timeout * 1000);
            var completed = await Task.WhenAny(exitTask, delayTask);
            var exited = completed == exitTask && exitTask.IsCompletedSuccessfully;

            // 取消沙箱监控
            sandboxCts.Cancel();

            // 检查沙箱资源超限（内存 / CPU）
            var sandboxViolation = await CheckSandboxMonitorsAsync(memTask, cpuTask);
            if (sandboxViolation != null)
            {
                if (!exited) proc.Kill(entireProcessTree: true);
                return sandboxViolation;
            }

            if (!exited)
            {
                proc.Kill(entireProcessTree: true);
                return $"错误：在 {timeout} 秒后超时";
            }

            // 等待异步读取完成
            var outStr = await stdoutTask;
            var errStr = await stderrTask;

            // 跟踪 cd 命令
            if (proc.ExitCode == 0)
            {
                UpdateCwd(command, cwd);
            }

            if (!string.IsNullOrEmpty(errStr))
                outStr += $"\n[stderr]\n{errStr}";
            if (proc.ExitCode != 0)
                outStr += $"\n[退出码：{proc.ExitCode}]";

            // 保留头尾以保留最有用的信息
            var maxChars = Config.Instance.BashOutputMaxChars;
            if (maxChars > 0 && outStr.Length > maxChars)
            {
                var headLen = maxChars * 40 / 100;
                var tailLen = maxChars * 40 / 100;
                outStr = outStr[..headLen]
                         + $"\n\n... 已截断（共 {outStr.Length} 字符）...\n\n"
                         + outStr[^tailLen..];
            }

            // 文件追踪：检查已读取文件是否被此外部命令修改
            var changeWarning = FileTracker.GetChangeWarning();
            if (changeWarning != null)
                outStr += "\n\n" + changeWarning;

            return string.IsNullOrWhiteSpace(outStr) ? "（无输出）" : outStr.Trim();
        }
        catch (Exception ex)
        {
            ErrorLog.ToolError("bash", $"命令执行异常: {command}", ex,
                new Dictionary<string, object?> { ["command"] = command, ["cwd"] = cwd });
            return $"运行命令时出错：{ex.GetType().Name}: {ex.Message}";
        }
    }

    /// <summary>流式执行：逐行读取 stdout 和 stderr 并回调，最后返回完整输出</summary>
    private async Task<string> ExecuteStreaming(
        Process proc, string command, string cwd, int timeout, Func<string, Task> onLine)
    {
        var outBuilder = new System.Text.StringBuilder();

        // 同时逐行读取 stdout 和 stderr
        async Task ReadStream(StreamReader reader, string prefix)
        {
            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;
                var output = prefix == "" ? line : $"{prefix}{line}";
                lock (outBuilder)
                {
                    outBuilder.AppendLine(output);
                }
                try { await onLine(output); } catch { /* 回调异常不影响执行 */ }
            }
        }

        var stdoutTask = ReadStream(proc.StandardOutput, "");
        var stderrTask = ReadStream(proc.StandardError, "[stderr] ");

        // 等待进程退出（同时 stdout/stderr 继续流式读取）
        var exitStream = proc.WaitForExitAsync();
        var delayStream = Task.Delay(timeout * 1000);
        var completedStream = await Task.WhenAny(exitStream, delayStream);
        var exitedStream = completedStream == exitStream && exitStream.IsCompletedSuccessfully;

        if (!exitedStream)
        {
            proc.Kill(entireProcessTree: true);
            return $"错误：在 {timeout} 秒后超时";
        }

        // 给流读取一个收尾窗口
        await Task.WhenAll(stdoutTask, stderrTask);

        var outStream = outBuilder.ToString();

        if (proc.ExitCode == 0) UpdateCwd(command, cwd);

        if (proc.ExitCode != 0)
            outStream += $"\n[退出码：{proc.ExitCode}]";

        var maxStreamChars = Config.Instance.BashOutputMaxChars;
        if (maxStreamChars > 0 && outStream.Length > maxStreamChars)
        {
            var headLen = maxStreamChars * 40 / 100;
            var tailLen = maxStreamChars * 40 / 100;
            outStream = outStream[..headLen]
                         + $"\n\n... 已截断（共 {outStream.Length} 字符）...\n\n"
                         + outStream[^tailLen..];
        }

        // 文件追踪：检查已读取文件是否被此外部命令修改
        var streamChangeWarning = FileTracker.GetChangeWarning();
        if (streamChangeWarning != null)
            outStream += "\n\n" + streamChangeWarning;

        return string.IsNullOrWhiteSpace(outStream) ? "（无输出）" : outStream.Trim();
    }

    /// <summary>
    /// 检查沙箱监控任务（内存 + CPU），返回违规消息，null 表示通过。
    /// </summary>
    private static async Task<string?> CheckSandboxMonitorsAsync(Task<string?>? memTask, Task<string?>? cpuTask)
    {
        if (memTask is { IsCompleted: true })
        {
            var memResult = await memTask;
            if (memResult != null) return memResult;
        }
        if (cpuTask is { IsCompleted: true })
        {
            var cpuResult = await cpuTask;
            if (cpuResult != null) return cpuResult;
        }
        return null;
    }

    /// <summary>
    /// 如果命令看起来具有破坏性，返回警告字符串；否则返回 null。
    /// </summary>
    internal static string? CheckDangerous(string cmd)
    {
        foreach (var (pattern, reason) in DangerousPatterns)
        {
            if (pattern.IsMatch(cmd)) return reason;
        }
        return null;
    }

    /// <summary>
    /// 跟踪 cd 命令导致的目录变更，按异步上下文隔离。
    /// </summary>
    internal static void UpdateCwd(string command, string currentCwd)
    {
        var running = currentCwd;
        var changed = false;

        foreach (var part in command.Split("&&"))
        {
            var trimmed = part.Trim();
            if (!trimmed.StartsWith("cd ")) continue;

            var target = trimmed[3..].Trim().Trim('\'', '"');
            if (string.IsNullOrEmpty(target)) continue;

            var newDir = Path.GetFullPath(Path.Combine(running,
                target.StartsWith('~') ? target.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)) : target));
            if (Directory.Exists(newDir))
            {
                running = newDir;
                changed = true;
            }
        }

        if (changed)
        {
            CurrentCwd.Value = running;
        }
    }
}
