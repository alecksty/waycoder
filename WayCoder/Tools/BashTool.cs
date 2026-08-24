using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WayCoder.Tools;

/// <summary>
/// 带安全检查的 Shell 命令执行。
/// </summary>
public class BashTool : ITool, ICancellableTool
{
    public string Name => "bash";
    public ToolExecutionMode ExecutionMode => ToolExecutionMode.Exclusive;
    public string Description => "执行 Shell 命令。返回 stdout、stderr 和退出码。\n⚠ 禁止执行：网络下载工具(curl/wget/ssh)、包管理器安装(apt/pip/npm install 等)、权限提升(sudo/su)、系统修改。\n✅ 安全免确认：ls/cat/grep/find/git log/dotnet --version 等只读操作自动放行。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("command", JNode.Object()
                .Set("type", "string")
                .Set("description", "要运行的 Shell 命令"))
            .Set("timeout", JNode.Object()
                .Set("type", "integer")
                .Set("description", "超时时间，单位秒（默认 120）。超时后命令自动转入后台继续执行，返回 shell_id，可用 job_output 轮询。"))
            .Set("run_in_background", JNode.Object()
                .Set("type", "boolean")
                .Set("description", "设为 true 则立即后台运行，返回 shell_id。之后用 job_output 读取输出，用 job_kill 终止。"))
            .Set("auto_background_after", JNode.Object()
                .Set("type", "integer")
                .Set("description", "前台等待 N 秒后自动转入后台（默认 60 秒）。仅 run_in_background=true 时生效。"))
            .Set("session_id", JNode.Object()
                .Set("type", "string")
                .Set("description", "持久 shell 会话 ID。提供则复用同一 shell 进程，多命令共享 cwd/环境变量/shell 状态（如 export、alias）。省略则每次新建进程。")))
        .Set("required", JNode.Array().Add("command"));

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

    // 绝对红线：即使 YOLO 模式也拦截（不可逆系统级破坏，防止误操作毁机）
    private static readonly (Regex Pattern, string Reason)[] RedLinePatterns =
    [
        (new(@"\brm\s+(-\w*)?-r\w*\s+(/|~|\$HOME)"), "对家目录/根目录的递归删除"),
        (new(@":\(\)\s*\{.*:\|:.*\}"), "fork 炸弹"),
        (new(@"\bdd\s+.*of=/dev/"), "原始磁盘写入"),
        (new(@">\s*/dev/sd[a-z]"), "覆盖块设备"),
        (new(@"\bmkfs\b"), "格式化文件系统"),
        (new(@"\bchmod\s+(-R\s+)?777\s+/"), "对根目录 chmod 777"),
    ];

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
        => await ExecuteAsync(arguments, CancellationToken.None);

    /// <summary>可取消执行（ICancellableTool）：中断时杀掉子进程并抛 OperationCanceledException 向上传播。</summary>
    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var command = arguments.GetValueOrDefault("command")?.ToString() ?? "";
        var configTimeout = Config.Instance.ToolTimeoutSec;
        var timeout = ToolArgs.GetInt(arguments, "timeout", configTimeout);

        // 后台运行模式（后台任务有意超出本轮生命周期，不受本轮中断令牌约束）
        var runInBackground = arguments.TryGetValue("run_in_background", out var bg) && bg is true;
        if (runInBackground)
        {
            var autoBgAfter = ToolArgs.GetInt(arguments, "auto_background_after", 60);
            var bgId = BackgroundTaskManager.Start(command, Math.Max(timeout, autoBgAfter + 30));
            return $"✅ 后台任务已启动\n" +
                   $"Shell ID: {bgId}\n" +
                   $"命令: {command}\n" +
                   $"使用 job_output 工具读取输出（参数 shell_id={bgId}）\n" +
                   $"使用 job_kill 工具终止任务（参数 shell_id={bgId}）";
        }

        var sessionId = arguments.GetValueOrDefault("session_id")?.ToString() ?? "";
        return await Execute(command, timeout, sessionId: sessionId, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 流式执行命令，每读到一行就调用 onLine 回调。
    /// 返回完整输出（与 ExecuteAsync 格式一致）。
    /// </summary>
    public async Task<string> ExecuteStreamingAsync(
        Dictionary<string, object?> arguments,
        Func<string, Task>? onLine)
        => await ExecuteStreamingAsync(arguments, onLine, CancellationToken.None);

    /// <summary>可取消流式执行（中断时杀掉子进程）。</summary>
    public async Task<string> ExecuteStreamingAsync(
        Dictionary<string, object?> arguments,
        Func<string, Task>? onLine,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var command = arguments.GetValueOrDefault("command")?.ToString() ?? "";
        var configTimeout = Config.Instance.ToolTimeoutSec;
        var timeout = ToolArgs.GetInt(arguments, "timeout", configTimeout);

        return await Execute(command, timeout, onLine, cancellationToken: cancellationToken);
    }

    private async Task<string> Execute(string command, int timeout, Func<string, Task>? onLine = null, string? sessionId = null, CancellationToken cancellationToken = default)
    {
        // YOLO 模式（畅通/上帝模式）：跳过 BashGuard 黑名单与普通危险检查，全部放行；
        // 仅保留绝对红线（rm -rf /、fork 炸弹、dd 写磁盘、mkfs 等不可逆系统破坏）
        var yolo = PermissionManager.CurrentMode == PermissionManager.Mode.Yolo;

        // BashGuard 命令黑名单检查（对标 crush 三层防护；yolo 跳过）
        if (!yolo)
        {
            var (blocked, reason) = BashGuard.CheckBanned(command);
            if (blocked)
                return $"{reason}\n命令：{command}";
        }

        // 已有危险模式检查（yolo 仅查绝对红线）
        var warning = CheckDangerous(command, yolo);
        if (warning != null)
            return $"⚠ 已阻止：{warning}\n命令：{command}\n如有意执行，请修改命令使其更具体。";

        // Worktree 隔离：检测 worktree 路径，自动切换 cwd
        var worktreePath = WorktreeIsolation.CurrentWorktree;
        var cwd = worktreePath ?? CurrentCwd.Value ?? Directory.GetCurrentDirectory();

        // 沙箱检查（full-auto 模式；yolo 不启用沙箱，见 SandboxManager.SetLevel）
        if (SandboxManager.IsSandboxed)
        {
            var violation = SandboxManager.CheckSandboxViolation(command, cwd);
            if (violation != null)
                return $"⛔ 沙箱阻止：{violation}\n命令：{command}";
        }

        // 持久 shell 会话（session_id 提供时复用同一 shell 进程，保持 cwd/env；沙箱模式不支持）
        if (!string.IsNullOrWhiteSpace(sessionId) && !SandboxManager.IsSandboxed)
        {
            var result = await PersistentShellManager.RunAsync(sessionId, command, timeout);
            UpdateCwd(command, cwd);
            return result;
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
                    // 必须重定向 stdin：否则子进程继承主控台 stdin，与 TUI 主循环的 Console.KeyAvailable/ReadKey
                    // 抢控制台输入 —— 子进程读到按键时主循环的 ReadKey 会永久阻塞（YOLO 模式任务执行中卡死的根源）。
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
            }

            // 移除凭据形状的环境变量，防止密钥经子进程 env / 输出泄漏
            EnvScrubber.Scrub(psi);

            var proc = Process.Start(psi)!;
            // 立即关闭 stdin 管道：子进程读到 EOF（而非挂起/共享控制台）。非交互命令不受影响。
            try { proc.StandardInput.Close(); } catch { /* 进程已退出 */ }

            // 中断令牌 → 立即杀掉子进程（Web 停止按钮 / Ctrl+C 真正终止 bash）
            using var cancelReg = cancellationToken.Register(() => KillQuietly(proc));

            // 流式模式：逐行读取 stdout 并回调（沙箱模式不支持流式）。
            // ExecuteStreaming 内部负责 proc 的 dispose / 迁移。
            if (onLine != null && !SandboxManager.IsSandboxed)
                return await ExecuteStreaming(proc, command, cwd, timeout, onLine, cancellationToken);

            var migrated = false;
            try
            {
                // 非流式模式：保持原有 ReadToEndAsync 逻辑
                // 立即启动异步读取（防止管道缓冲区满导致死锁）
                var stdoutTask = proc.StandardOutput.ReadToEndAsync();
                var stderrTask = proc.StandardError.ReadToEndAsync();

                // 沙箱模式：后台监控内存 + CPU 时间（使用实际处理器时间，非墙上时钟）
                using var sandboxCts = new CancellationTokenSource();
                Task<string?>? memTask = null;
                Task<string?>? cpuTask = null;
                if (SandboxManager.IsSandboxed)
                {
                    memTask = SandboxManager.MonitorMemoryAsync(proc, sandboxCts.Token);
                    cpuTask = SandboxManager.MonitorCpuAsync(proc, sandboxCts.Token);
                }

                // 异步等待进程退出（不阻塞线程）
                var exitTask = proc.WaitForExitAsync();
                var delayTask = Task.Delay(TimeSpan.FromMilliseconds(Math.Clamp((long)timeout * 1000, 0, int.MaxValue)));
                var completed = await Task.WhenAny(exitTask, delayTask);
                var exited = completed == exitTask && exitTask.IsCompletedSuccessfully;

                // 取消沙箱监控
                sandboxCts.Cancel();

                // 中断优先于超时迁移 / 沙箱违规：令牌已取消则抛异常向上传播（子进程已被回调杀掉）
                cancellationToken.ThrowIfCancellationRequested();

                // 检查沙箱资源超限（内存 / CPU）
                var sandboxViolation = await CheckSandboxMonitorsAsync(memTask, cpuTask);
                if (sandboxViolation != null)
                {
                    if (!exited) proc.Kill(entireProcessTree: true);
                    return sandboxViolation;
                }

                if (!exited)
                {
                    // 沙箱模式：直接终止以强制资源限制（迁移会绕过内存/CPU 上限）
                    if (SandboxManager.IsSandboxed)
                    {
                        proc.Kill(entireProcessTree: true);
                        return $"错误：在 {timeout} 秒后超时";
                    }

                    // 前台超时自动迁移到后台（对标 Crush），进程所有权转移，不再 dispose
                    var bgId = BackgroundTaskManager.Adopt(proc, command, stdoutTask, stderrTask);
                    migrated = true;
                    return $"⏰ 命令已运行超过 {timeout} 秒，自动转入后台继续执行\n" +
                           $"Shell ID: {bgId}\n" +
                           $"命令: {command}\n" +
                           $"使用 job_output 工具读取输出（参数 shell_id={bgId}）\n" +
                           $"使用 job_kill 工具终止任务（参数 shell_id={bgId}）";
                }

                // 等待异步读取完成（带超时：守护子进程继承管道会让 ReadToEndAsync 永不 EOF）
                var outStr = await WayCoder.Infra.ProcUtil.AwaitReadWithTimeoutAsync(stdoutTask, TimeSpan.FromSeconds(5)) ?? "";
                var errStr = await WayCoder.Infra.ProcUtil.AwaitReadWithTimeoutAsync(stderrTask, TimeSpan.FromSeconds(5)) ?? "";

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
                    outStr = ContextManager.TruncateKeepHeadTail(outStr, headLen, tailLen, $"\n\n... 已截断（共 {outStr.Length} 字符）...\n\n");
                }

                // 文件追踪：检查已读取文件是否被此外部命令修改
                var changeWarning = FileTracker.GetChangeWarning();
                if (changeWarning != null)
                    outStr += "\n\n" + changeWarning;

                return string.IsNullOrWhiteSpace(outStr) ? "（无输出）" : outStr.Trim();
            }
            finally
            {
                if (!migrated)
                    proc.Dispose();
            }
        }
        catch (OperationCanceledException)
        {
            throw; // 中断信号，向上传播，不吞掉
        }
        catch (Exception ex)
        {
            ErrorLog.ToolError("bash", $"命令执行异常: {command}", ex,
                new Dictionary<string, object?> { ["command"] = command, ["cwd"] = cwd });
            return $"运行命令时出错：{ex.GetType().Name}: {ex.Message}";
        }
    }

    /// <summary>安全杀掉进程（进程可能已退出，忽略异常）。</summary>
    private static void KillQuietly(Process proc)
    {
        try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); }
        catch { /* 进程已退出或无法访问 */ }
    }

    /// <summary>流式执行：逐行读取 stdout 和 stderr 并回调，最后返回完整输出</summary>
    private async Task<string> ExecuteStreaming(
        Process proc, string command, string cwd, int timeout, Func<string, Task> onLine, CancellationToken cancellationToken = default)
    {
        var outBuilder = new System.Text.StringBuilder();

        // 同时逐行读取 stdout 和 stderr
        async Task ReadStream(StreamReader reader, string prefix)
        {
            while (true)
            {
                string? line;
                try { line = await reader.ReadLineAsync(); }
                catch { break; } // 流被关闭/进程被杀死，结束读取
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

        var migrated = false;
        try
        {
            // 等待进程退出（同时 stdout/stderr 继续流式读取）
            var exitStream = proc.WaitForExitAsync();
            var delayStream = Task.Delay(TimeSpan.FromMilliseconds(Math.Clamp((long)timeout * 1000, 0, int.MaxValue)));
            var completedStream = await Task.WhenAny(exitStream, delayStream);
            var exitedStream = completedStream == exitStream && exitStream.IsCompletedSuccessfully;

            // 中断优先于超时迁移：令牌已取消则抛异常向上传播（子进程已被回调杀掉）
            cancellationToken.ThrowIfCancellationRequested();

            if (!exitedStream)
            {
                // 前台超时自动迁移到后台（对标 Crush），进程所有权转移，不再 dispose
                var bgId = BackgroundTaskManager.AdoptStreaming(proc, command, stdoutTask, stderrTask, () => outBuilder.ToString());
                migrated = true;
                return $"⏰ 命令已运行超过 {timeout} 秒，自动转入后台继续执行\n" +
                       $"Shell ID: {bgId}\n" +
                       $"命令: {command}\n" +
                       $"使用 job_output 工具读取输出（参数 shell_id={bgId}）\n" +
                       $"使用 job_kill 工具终止任务（参数 shell_id={bgId}）";
            }

            // 给流读取一个收尾窗口（守护子进程继承管道会让读永不 EOF，加超时防挂起）
            await Task.WhenAny(Task.WhenAll(stdoutTask, stderrTask), Task.Delay(5000));

            var outStream = outBuilder.ToString();

            if (proc.ExitCode == 0) UpdateCwd(command, cwd);

            if (proc.ExitCode != 0)
                outStream += $"\n[退出码：{proc.ExitCode}]";

            var maxStreamChars = Config.Instance.BashOutputMaxChars;
            if (maxStreamChars > 0 && outStream.Length > maxStreamChars)
            {
                var headLen = maxStreamChars * 40 / 100;
                var tailLen = maxStreamChars * 40 / 100;
                outStream = ContextManager.TruncateKeepHeadTail(outStream, headLen, tailLen, $"\n\n... 已截断（共 {outStream.Length} 字符）...\n\n");
            }

            // 文件追踪：检查已读取文件是否被此外部命令修改
            var streamChangeWarning = FileTracker.GetChangeWarning();
            if (streamChangeWarning != null)
                outStream += "\n\n" + streamChangeWarning;

            return string.IsNullOrWhiteSpace(outStream) ? "（无输出）" : outStream.Trim();
        }
        finally
        {
            if (!migrated)
                proc.Dispose();
        }
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
    /// yoloOnly=true（YOLO 模式）时仅检查绝对红线（不可逆系统破坏）。
    /// </summary>
    internal static string? CheckDangerous(string cmd, bool yoloOnly = false)
    {
        var patterns = yoloOnly ? RedLinePatterns : DangerousPatterns;
        foreach (var (pattern, reason) in patterns)
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
                target.StartsWith('~') ? ExpandHome(target) : target));

            static string ExpandHome(string p)
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (p == "~") return home;
                if (p.StartsWith("~/") || p.StartsWith("~\\")) return Path.Combine(home, p[2..]);
                return p; // ~user 等形式保持原样
            }
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
