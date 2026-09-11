using System.Diagnostics;

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
    /// 构造「捕获输出」型进程的启动参数 —— **进程启动样板的唯一实现**。
    ///
    /// 三件事一次做齐：重定向三路输出、不共享主控台 stdin（启动后置 EOF，防子进程抢 TUI 的
    /// ReadKey）、Windows 上按 OEM 代码页解码（cmd 系包装器的中文输出）。
    ///
    /// 此前 <c>KillTool</c> 与 <c>PsTool</c> 各写一份**逐字相同**的实现，连「本工具此前漏了
    /// ProcEncoding.Apply」这个修复都各做了一遍（todos.json、mcp_servers.json 之外的又一处
    /// 「同一规则两处实现、只修其中一处」）。新增进程启动点请一律用它，别再抄第四份。
    /// </summary>
    public static ProcessStartInfo BuildPsi(string fileName, string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true, // 不共享主控台 stdin（启动后置 EOF，防 TUI ReadKey 竞态）
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        ProcEncoding.Apply(psi);
        return psi;
    }

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
        // psi 设置了 RedirectStandardInput 时，立即置 EOF：子进程不共享主控台 stdin，
        // 防与 TUI 主循环的 Console.KeyAvailable/ReadKey 抢控制台输入（ReadKey 永久阻塞 = 界面卡死）。
        // 未重定向 stdin 的调用方此处访问会抛异常，被捕获忽略。
        CloseStdin(proc);
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        using var timeoutCts = timeoutMs > 0 ? new CancellationTokenSource(timeoutMs) : null;
        using var linked = timeoutCts != null
            ? CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token)
            : CancellationTokenSource.CreateLinkedTokenSource(ct);
        try { await proc.WaitForExitAsync(linked.Token); }
        catch (OperationCanceledException)
        {
            KillTree(proc);
            return null; // 超时/取消
        }
        var stdout = await AwaitReadWithTimeoutAsync(stdoutTask, TimeSpan.FromSeconds(5)) ?? "";
        var stderr = await AwaitReadWithTimeoutAsync(stderrTask, TimeSpan.FromSeconds(5)) ?? "";
        return (proc.ExitCode, stdout, stderr);
    }

    /// <summary>关闭子进程 stdin 置 EOF（不共享主控台 stdin，防与 TUI 抢控制台输入）。异常忽略。</summary>
    public static void CloseStdin(System.Diagnostics.Process proc) { try { proc.StandardInput.Close(); } catch { } }

    /// <summary>终止整进程树（含子进程），异常忽略。</summary>
    public static void KillTree(System.Diagnostics.Process proc) { try { proc.Kill(entireProcessTree: true); } catch { } }
}
