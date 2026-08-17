using System.Diagnostics;

namespace WayCoder;

/// <summary>
/// 统一 Git 进程执行器 —— 消除项目中 8 处重复的 Process.Start("git") 模式。
/// 所有 git 调用都应通过此类，确保一致的超时、错误处理和输出收集。
/// </summary>
public static class GitRunner
{
    static int DefaultTimeoutMs => Config.Instance.GitTimeoutSec * 1000;

    static ProcessStartInfo BuildStartInfo(string args, string? cwd)
    {
        var psi = new ProcessStartInfo("git", args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        if (cwd != null) psi.WorkingDirectory = cwd;
        return psi;
    }

    /// <summary>同步执行（内部异步，供调用链已处于 Task.Run 的上下文使用）</summary>
    public static (int ExitCode, string Stdout, string Stderr) Run(string args, string? cwd = null)
    {
        try
        {
            using var proc = Process.Start(BuildStartInfo(args, cwd))!;
            // 并发读取 stdout/stderr，避免经典死锁：stderr 缓冲写满时进程阻塞，
            // 而同步先读 stdout 永远等不到 EOF。
            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();
            if (!proc.WaitForExit(DefaultTimeoutMs))
            {
                try { proc.Kill(entireProcessTree: true); } catch { /* 已退出 */ }
                return (-1, "", $"git 命令超时（>{DefaultTimeoutMs / 1000}s）: {args}");
            }
            return (proc.ExitCode, stdoutTask.GetAwaiter().GetResult(), stderrTask.GetAwaiter().GetResult());
        }
        catch (Exception ex)
        {
            return (-1, "", ex.Message);
        }
    }

    /// <summary>异步执行，返回完整的 (退出码, stdout, stderr)</summary>
    public static async Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(string args, string? cwd = null)
        => await RunAsync(args, cwd, CancellationToken.None);

    /// <summary>异步执行（可取消）：中断时杀掉 git 子进程并抛 OperationCanceledException。</summary>
    public static async Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(
        string args, string? cwd, CancellationToken cancellationToken)
    {
        try
        {
            using var proc = Process.Start(BuildStartInfo(args, cwd))!;
            // 并发读取 stdout/stderr，避免同步先读 stdout 的死锁。
            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();
            using var timeoutCts = new CancellationTokenSource(DefaultTimeoutMs);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            try
            {
                await proc.WaitForExitAsync(linked.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // 配置超时（非外部取消）：杀掉进程并返回超时错误
                try { proc.Kill(entireProcessTree: true); } catch { /* 已退出 */ }
                return (-1, "", $"git 命令超时（>{DefaultTimeoutMs / 1000}s）: {args}");
            }
            catch (OperationCanceledException)
            {
                try { proc.Kill(entireProcessTree: true); } catch { /* 已退出 */ }
                throw;
            }
            return (proc.ExitCode, await stdoutTask, await stderrTask);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return (-1, "", ex.Message);
        }
    }

    /// <summary>同步执行，只返回 stdout（忽略 stderr），失败返回 ""</summary>
    public static string Output(string args, string? cwd = null)
    {
        var (_, stdout, _) = Run(args, cwd);
        return stdout;
    }

    /// <summary>同步执行，成功后返回 stdout，失败抛出异常</summary>
    public static string RunOrThrow(string args, string? cwd = null)
    {
        var (exitCode, stdout, stderr) = Run(args, cwd);
        if (exitCode != 0)
            throw new Exception($"git {args} 失败 (exit {exitCode}): {stderr.Trim()}");
        return stdout;
    }
}
