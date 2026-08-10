using System.Diagnostics;

namespace WayCoder;

/// <summary>
/// 统一 Git 进程执行器 —— 消除项目中 8 处重复的 Process.Start("git") 模式。
/// 所有 git 调用都应通过此类，确保一致的超时、错误处理和输出收集。
/// </summary>
public static class GitRunner
{
    const int DefaultTimeoutMs = 15_000;

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
            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExitAsync().GetAwaiter().GetResult();
            return (proc.ExitCode, stdout, stderr);
        }
        catch (Exception ex)
        {
            return (-1, "", ex.Message);
        }
    }

    /// <summary>异步执行，返回完整的 (退出码, stdout, stderr)</summary>
    public static async Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(string args, string? cwd = null)
    {
        try
        {
            using var proc = Process.Start(BuildStartInfo(args, cwd))!;
            var stdout = await proc.StandardOutput.ReadToEndAsync();
            var stderr = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();
            return (proc.ExitCode, stdout, stderr);
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
