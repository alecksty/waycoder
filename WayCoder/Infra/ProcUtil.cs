namespace WayCoder.Infra;

/// <summary>
/// 进程输出读取辅助 —— 修复「守护/后台子进程继承 stdout/stderr 管道」导致的永久挂起：
/// 主进程很快退出（WaitForExit 完成），但孙进程（如 `nohup node &`、`python -m http.server &`）
/// 仍持有管道写端 → ReadToEndAsync 永远等不到 EOF → 后续 `await stdoutTask` 无超时永久阻塞，
/// 且常不可取消（Agent 主循环被拖死）。
///
/// 统一给读取加超时：超时返回 null，调用方按「输出丢失但进程已结束」降级（不挂起）。
/// </summary>
public static class ProcUtil
{
    /// <summary>
    /// 带超时等待读取任务完成。返回 null = 超时（孙进程持有管道，输出不可达）。
    /// readTask 的异常（进程被杀等）也归为 null。
    /// </summary>
    public static async Task<string?> AwaitReadWithTimeoutAsync(Task<string> readTask, TimeSpan timeout)
    {
        var completed = await Task.WhenAny(readTask, Task.Delay(timeout));
        if (completed != readTask) return null;
        try { return await readTask; }
        catch { return null; }
    }

    /// <summary>
    /// 统一「启动进程 + 并发读 stdout/stderr + 等退出（带超时） + 超时杀进程树 + 读取兜底」。
    /// 返回 null = 超时/取消（调用方自行格式化超时文案）；否则 (退出码, stdout, stderr)。
    /// 消除各工具重复的进程运行样板 + 三种超时语义差异。
    /// </summary>
    public static async Task<(int ExitCode, string Stdout, string Stderr)?> RunAsync(
        System.Diagnostics.ProcessStartInfo psi, int timeoutMs, CancellationToken ct = default)
    {
        using var proc = new System.Diagnostics.Process { StartInfo = psi };
        proc.Start();
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        using var timeoutCts = timeoutMs > 0 ? new CancellationTokenSource(timeoutMs) : null;
        using var linked = timeoutCts != null
            ? CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token)
            : CancellationTokenSource.CreateLinkedTokenSource(ct);
        try { await proc.WaitForExitAsync(linked.Token); }
        catch (OperationCanceledException)
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            return null; // 超时/取消
        }
        var stdout = await AwaitReadWithTimeoutAsync(stdoutTask, TimeSpan.FromSeconds(5)) ?? "";
        var stderr = await AwaitReadWithTimeoutAsync(stderrTask, TimeSpan.FromSeconds(5)) ?? "";
        return (proc.ExitCode, stdout, stderr);
    }
}
