using System.Diagnostics;

namespace CoreCoderSharp.Tools;

/// <summary>
/// Git 操作工具 —— 安全封装常用 git 命令。
/// 输出自动截断，禁止 force push / hard reset 等危险操作。
/// </summary>
public class GitTool : ITool
{
    public string Name => "git";
    public string Description => "执行 Git 操作：status、log、diff、add、commit、branch、blame。自动检测仓库根目录。禁止 force push / hard reset。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["command"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Git 子命令及参数，如 'status'、'log --oneline -10'、'diff HEAD~1'、'add src/'、'commit -m \"msg\"'",
            },
        },
        ["required"] = new JsonArray("command"),
    };

    private static readonly HashSet<string> BlockedPatterns =
        ["push --force", "push -f", "reset --hard", "clean -f", "clean -fd",
         "checkout -- .", "stash drop", "branch -D"];

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var command = arguments.GetValueOrDefault("command")?.ToString() ?? "";
        return Task.FromResult(Execute(command));
    }

    private static string Execute(string command)
    {
        // 安全检查
        foreach (var blocked in BlockedPatterns)
        {
            if (command.Contains(blocked, StringComparison.OrdinalIgnoreCase))
                return $"⚠ 已阻止：'{blocked}' 是危险操作，请手动执行或使用更安全的替代方式。";
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var proc = Process.Start(psi)!;

            // 异步读取输出，避免缓冲区满导致死锁
            var stdout = new System.Text.StringBuilder();
            var stderr = new System.Text.StringBuilder();
            proc.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
            proc.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            var exited = proc.WaitForExit(15_000);
            if (!exited)
            {
                proc.Kill(entireProcessTree: true);
                return "Git 错误：在 15 秒后超时";
            }
            proc.WaitForExit(); // 确保异步读取完成

            var outStr = stdout.ToString();
            var errStr = stderr.ToString();

            var result = outStr;
            if (!string.IsNullOrEmpty(errStr))
                result += $"\n[stderr]\n{errStr}";
            if (proc.ExitCode != 0)
                result += $"\n[退出码：{proc.ExitCode}]";

            // 截断长输出
            if (result.Length > 8000)
                result = result[..6000] + $"\n... (已截断，共 {result.Length} 字符) ...\n" + result[^1000..];

            return string.IsNullOrWhiteSpace(result) ? "（无输出）" : result.Trim();
        }
        catch (Exception ex)
        {
            return $"Git 错误：{ex.Message}";
        }
    }
}
