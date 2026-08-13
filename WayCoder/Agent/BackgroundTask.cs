using System.Diagnostics;
using System.Collections.Concurrent;

namespace WayCoder;

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
    /// 启动后台任务，返回任务 ID。任务在后台异步执行，不阻塞调用方。
    /// </summary>
    public static int Start(string command, int timeoutSec = -1)
    {
        var effectiveTimeout = timeoutSec > 0 ? timeoutSec : Config.Instance.BackgroundTaskTimeoutSec;
        var id = Interlocked.Increment(ref _nextId);
        var task = new BgTask(id, command, DateTime.Now);
        _tasks[id] = task;

        // 后台异步执行，不阻塞；异常由 RunTaskAsync 内部捕获
        _ = RunTaskAsync(task, effectiveTimeout);

        return id;
    }

    /// <summary>
    /// 接纳一个已在前台运行（ReadToEnd 模式）的进程为后台任务。
    /// 用于前台命令超时自动迁移（对标 Crush）。进程所有权转移给后台管理器，调用方不再 dispose。
    /// </summary>
    public static int Adopt(Process proc, string command, Task<string> stdoutTask, Task<string> stderrTask)
    {
        var id = Interlocked.Increment(ref _nextId);
        var task = new BgTask(id, command, DateTime.Now) { Process = proc };
        _tasks[id] = task;

        var outStr = "";
        var errStr = "";
        _ = RunAdoptedTaskAsync(task,
            async () => { outStr = await stdoutTask; errStr = await stderrTask; },
            () => outStr + "\n" + errStr);

        return id;
    }

    /// <summary>
    /// 接纳一个已在前台流式运行（逐行读取）的进程为后台任务。
    /// readTasks 为已在执行的逐行读取任务，outputGetter 返回共享输出缓冲内容。
    /// </summary>
    public static int AdoptStreaming(Process proc, string command, Task readStdout, Task readStderr, Func<string> outputGetter)
    {
        var id = Interlocked.Increment(ref _nextId);
        var task = new BgTask(id, command, DateTime.Now) { Process = proc };
        _tasks[id] = task;
        _ = RunAdoptedTaskAsync(task, () => Task.WhenAll(readStdout, readStderr), outputGetter);
        return id;
    }

    private static async Task RunTaskAsync(BgTask task, int timeoutSec)
    {
        Process? proc = null;
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

            proc = Process.Start(psi)!;
            task.Process = proc;

            // 异步读取输出（防止管道缓冲区满死锁）
            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();
            var outStr = "";
            var errStr = "";

            await WaitAndCollectAsync(task, timeoutSec,
                async () => { outStr = await stdoutTask; errStr = await stderrTask; },
                () => outStr + "\n" + errStr);
        }
        catch (Exception ex)
        {
            task.Status = "error";
            task.Output = $"启动失败: {ex.Message}";
            DebugLog.Log("bgtask", $"后台任务 #{task.Id} 异常: {ex.Message}");
            ErrorLog.Error("BackgroundTask", $"后台任务 #{task.Id} 异常: {task.Command}", ex);
        }
        finally
        {
            task.CompletedAt = DateTime.Now;
            task.Process = null;
            proc?.Dispose();
        }
    }

    private static async Task RunAdoptedTaskAsync(BgTask task, Func<Task> ioCompletion, Func<string> outputGetter)
    {
        try
        {
            await WaitAndCollectAsync(task, Config.Instance.BackgroundTaskTimeoutSec, ioCompletion, outputGetter);
        }
        catch (Exception ex)
        {
            task.Status = "error";
            task.Output = $"迁移后异常: {ex.Message}";
            DebugLog.Log("bgtask", $"后台任务 #{task.Id} 迁移后异常: {ex.Message}");
            ErrorLog.Error("BackgroundTask", $"后台任务 #{task.Id} 迁移后异常: {task.Command}", ex);
        }
        finally
        {
            task.CompletedAt = DateTime.Now;
            var proc = task.Process;
            task.Process = null;
            proc?.Dispose(); // 释放迁移过来的进程句柄
        }
    }

    /// <summary>
    /// 等待进程退出（带超时），收集中间 IO，写入任务状态与输出。
    /// </summary>
    private static async Task WaitAndCollectAsync(BgTask task, int timeoutSec, Func<Task> ioCompletion, Func<string> outputGetter)
    {
        var proc = task.Process!;

        // 异步等待退出（带超时）
        var exitTask = proc.WaitForExitAsync();
        var delayTask = Task.Delay(timeoutSec * 1000);
        var completed = await Task.WhenAny(exitTask, delayTask);
        var exited = completed == exitTask && exitTask.IsCompletedSuccessfully;

        if (!exited)
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            task.Status = "timeout";
            await ioCompletion();
            task.Output = outputGetter().Trim() + "\n[超时]";
            ErrorLog.Warning("BackgroundTask", $"后台任务超时 (id={task.Id}, timeout={timeoutSec}s): {task.Command}");
        }
        else
        {
            // 进程已退出，等待异步 IO 完成
            await ioCompletion();
            task.Status = proc.ExitCode == 0 ? "completed" : "failed";
            task.ExitCode = proc.ExitCode;
            task.Output = outputGetter().Trim();
            if (task.ExitCode != 0)
                task.Output += $"\n[退出码: {task.ExitCode}]";
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
    /// 终止指定后台任务。
    /// </summary>
    public static string Kill(int id)
    {
        if (!_tasks.TryGetValue(id, out var task))
            return $"未找到任务 #{id}";

        if (task.Status != "running")
            return $"任务 #{id} 已结束（状态: {task.Status}）";

        try
        {
            task.Process?.Kill(entireProcessTree: true);
            task.Status = "killed";
            task.Output += "\n[已被用户终止]";
            task.CompletedAt = DateTime.Now;
            return $"已终止任务 #{id}: {task.Command}";
        }
        catch (Exception ex)
        {
            return $"终止任务 #{id} 失败: {ex.Message}";
        }
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
                _tasks.TryRemove(id, out _);
            }
        }
    }
}
