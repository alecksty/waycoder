using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CoreCoderSharp.Tools;

/// <summary>
/// 带安全检查的 Shell 命令执行。
/// </summary>
public class BashTool : ITool
{
    public string Name => "bash";
    public string Description => "执行 Shell 命令。返回 stdout、stderr 和退出码。用于运行测试、安装包、git 操作等。";

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
        var timeout = arguments.TryGetValue("timeout", out var t) && t is int ti ? ti : 120;

        return await Execute(command, timeout);
    }

    private async Task<string> Execute(string command, int timeout)
    {
        // 安全检查
        var warning = CheckDangerous(command);
        if (warning != null)
            return $"⚠ 已阻止：{warning}\n命令：{command}\n如有意执行，请修改命令使其更具体。";

        var cwd = CurrentCwd.Value ?? Directory.GetCurrentDirectory();

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

            // 异步读取输出，避免缓冲区满导致死锁
            var stdout = new System.Text.StringBuilder();
            var stderr = new System.Text.StringBuilder();
            proc.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
            proc.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            // 沙箱模式：后台监控内存
            var memCts = new CancellationTokenSource();
            Task<string?>? memTask = null;
            if (SandboxManager.IsSandboxed)
                memTask = SandboxManager.MonitorMemoryAsync(proc, memCts.Token);

            var exited = proc.WaitForExit(timeout * 1000);

            // 取消内存监控
            memCts.Cancel();

            // 检查内存超限
            if (memTask is { IsCompleted: true })
            {
                var memResult = await memTask;
                if (memResult != null)
                {
                    proc.Kill(entireProcessTree: true);
                    return memResult;
                }
            }

            if (!exited)
            {
                proc.Kill(entireProcessTree: true);
                return $"错误：在 {timeout} 秒后超时";
            }

            proc.WaitForExit(); // 确保异步读取完成

            var outStr = stdout.ToString();
            var errStr = stderr.ToString();

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
            if (outStr.Length > 15_000)
            {
                outStr = outStr[..6000]
                         + $"\n\n... 已截断（共 {outStr.Length} 字符）...\n\n"
                         + outStr[^3000..];
            }

            return string.IsNullOrWhiteSpace(outStr) ? "（无输出）" : outStr.Trim();
        }
        catch (Exception ex)
        {
            return $"运行命令时出错：{ex.Message}";
        }
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
