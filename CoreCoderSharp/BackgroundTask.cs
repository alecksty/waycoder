using System.Diagnostics;
using System.Collections.Concurrent;

namespace CoreCoderSharp;

/// <summary>
/// 后台任务管理器 —— 支持异步执行长时间命令。
/// bash 工具添加 mode=background 参数时，任务在后台运行。
/// /jobs 查看状态，/job-output <id> 获取结果。
/// </summary>
public static class BackgroundTaskManager
{
    private static readonly ConcurrentDictionary<int, BgTask> _tasks = new();
    private static int _nextId = 1;

    public record BgTask(int Id, string Command, DateTime StartedAt)
    {
        public Process? Process { get; set; }
        public string Status { get; set; } = "running";
        public string Output { get; set; } = "";
        public DateTime? CompletedAt { get; set; }
        public int? ExitCode { get; set; }
    }

    /// <summary>
    /// 启动后台任务，返回任务 ID。
    /// </summary>
    public static async Task<int> StartAsync(string command, int timeoutSec = 600)
    {
        var id = Interlocked.Increment(ref _nextId);
        var task = new BgTask(id, command, DateTime.Now);
        _tasks[id] = task;

        _ = Task.Run(() => RunTask(task, timeoutSec));

        return await Task.FromResult(id);
    }

    private static void RunTask(BgTask task, int timeoutSec)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash",
                Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? $"/c \"{task.Command}\""
                    : $"-c \"{task.Command.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Directory.GetCurrentDirectory(),
            };

            task.Process = Process.Start(psi)!;

            var output = new System.Text.StringBuilder();
            task.Process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) output.AppendLine(e.Data);
            };
            task.Process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) output.AppendLine("[stderr] " + e.Data);
            };
            task.Process.BeginOutputReadLine();
            task.Process.BeginErrorReadLine();

            var exited = task.Process.WaitForExit(timeoutSec * 1000);
            if (!exited)
            {
                task.Process.Kill(true);
                task.Status = "timeout";
                task.Output = output.ToString() + "\n[超时]";
            }
            else
            {
                task.Process.WaitForExit(1000); // flush async output
                task.Status = task.Process.ExitCode == 0 ? "completed" : "failed";
                task.ExitCode = task.Process.ExitCode;
                task.Output = output.ToString();
                if (task.ExitCode != 0)
                    task.Output += $"\n[退出码: {task.ExitCode}]";
            }
        }
        catch (Exception ex)
        {
            task.Status = "error";
            task.Output = $"启动失败: {ex.Message}";
        }
        finally
        {
            task.CompletedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// 获取任务状态和输出（截断）。
    /// </summary>
    public static string GetOutput(int id)
    {
        if (!_tasks.TryGetValue(id, out var task))
            return $"未找到任务 #{id}";

        var output = task.Output;
        if (output.Length > 5000)
            output = output[..4000] + $"\n... (已截断，共 {output.Length} 字符)";

        return output.Trim();
    }

    /// <summary>
    /// 列出所有任务。
    /// </summary>
    public static string ListTasks()
    {
        if (_tasks.IsEmpty) return "（无后台任务）";

        var lines = new List<string>();
        foreach (var (_, task) in _tasks.OrderByDescending(t => t.Key))
        {
            var statusIcon = task.Status switch
            {
                "running" => "🔄",
                "completed" => "✅",
                "failed" => "❌",
                "timeout" => "⏰",
                "error" => "💥",
                _ => "❓",
            };
            var duration = (task.CompletedAt ?? DateTime.Now) - task.StartedAt;
            var cmd = task.Command.Length > 60 ? task.Command[..57] + "..." : task.Command;
            lines.Add($"  #{task.Id} {statusIcon} [{task.Status}] {cmd} ({duration.TotalSeconds:F0}s)");
        }
        return string.Join("\n", lines);
    }

    /// <summary>
    /// 清理已完成的任务。
    /// </summary>
    public static void Cleanup()
    {
        foreach (var (id, task) in _tasks)
        {
            if (task.Status != "running")
            {
                task.Process?.Dispose();
                _tasks.TryRemove(id, out _);
            }
        }
    }
}
