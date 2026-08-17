using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WayCoder;

/// <summary>
/// 持久 shell 会话 —— 一个长生命周期的 shell 进程，支持多条命令在同一会话中
/// 顺序执行，保持 cwd / 环境变量 / shell 状态（alias、export 等跨命令生效）。
/// 用唯一 marker 界定每条命令的输出边界并回读退出码；命令超时或进程崩溃时
/// 整会话终止，下次执行自动重建（对标 deepseek-harness 的 persistent shell）。
/// </summary>
public sealed class PersistentShell : IDisposable
{
    private readonly string _id;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private Process? _proc;
    private long _lastUsedTicks;

    public string Id => _id;
    public long LastUsedTicks => _lastUsedTicks;
    public bool IsAlive => _proc != null && !_proc.HasExited;

    public PersistentShell(string id)
    {
        _id = id;
        _lastUsedTicks = Environment.TickCount64;
    }

    /// <summary>
    /// 包装一条命令：命令后追加 marker 输出命令，用于界定输出边界并回读退出码。
    /// 命令 stderr 经 2>&1 合并到 stdout（marker 由 echo 写 stdout，故总在 stdout 末尾）。
    /// 纯逻辑，跨平台，便于自测。
    /// </summary>
    internal static string BuildCommand(string command, string marker)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return $"{command} 2>&1\r\necho {marker}:%ERRORLEVEL%\r\n";
        return $"{command} 2>&1\necho \"{marker}:$?\"\n";
    }

    /// <summary>在会话中执行一条命令，返回输出（含退出码标注，格式与 bash 工具一致）。</summary>
    public async Task<string> RunAsync(string command, int timeoutSec)
    {
        await _lock.WaitAsync();
        try
        {
            _lastUsedTicks = Environment.TickCount64;
            EnsureAlive();

            var marker = $"__WC_END_{Guid.NewGuid():N}__";
            var shellCmd = BuildCommand(command, marker);

            await _proc!.StandardInput.WriteAsync(shellCmd);
            await _proc.StandardInput.FlushAsync();

            var (output, exitCode, timedOut) = await ReadUntilMarkerAsync(marker, timeoutSec);
            if (timedOut)
            {
                KillSession();
                return $"{output}\n[错误：命令在 {timeoutSec} 秒后超时，会话已终止]";
            }

            var result = output;
            if (exitCode != 0)
                result += $"\n[退出码：{exitCode}]";

            return string.IsNullOrWhiteSpace(result) ? "（无输出）" : result.Trim();
        }
        catch (Exception ex)
        {
            // 进程异常（写失败/读失败），终止会话下次重建
            KillSession();
            return $"运行命令时出错：{ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>启动 shell 进程（若尚未启动或已退出）。</summary>
    private void EnsureAlive()
    {
        if (_proc != null && !_proc.HasExited) return;

        if (_proc != null)
        {
            try { if (!_proc.HasExited) _proc.Kill(entireProcessTree: true); } catch { }
            try { _proc.Dispose(); } catch { }
            _proc = null;
        }

        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var psi = new ProcessStartInfo
        {
            FileName = isWindows ? "cmd.exe" : "/bin/bash",
            // bash：不加载 profile/rc（干净、无交互 prompt）；cmd：/Q 关闭回显
            Arguments = isWindows ? "/Q" : "--noprofile --norc",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        _proc = Process.Start(psi)!;
        DrainStderr();
    }

    /// <summary>后台排空 stderr（命令 stderr 已 2>&1 合并，此处仅防管道缓冲区满）。</summary>
    private void DrainStderr()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var reader = _proc!.StandardError;
                var buf = new char[1024];
                while (await reader.ReadAsync(buf.AsMemory()) > 0) { /* 丢弃 */ }
            }
            catch { /* 进程退出时读失败，忽略 */ }
        });
    }

    /// <summary>字符级读取 stdout 直到出现 marker 行，返回（输出, 退出码, 是否超时）。</summary>
    private async Task<(string Output, int ExitCode, bool TimedOut)> ReadUntilMarkerAsync(string marker, int timeoutSec)
    {
        var sb = new StringBuilder();
        var buf = new char[1024];
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec));
        try
        {
            while (true)
            {
                int n;
                try { n = await _proc!.StandardOutput.ReadAsync(buf.AsMemory(), cts.Token); }
                catch (OperationCanceledException) { return (sb.ToString().TrimEnd('\r', '\n'), 0, true); }

                if (n == 0) break; // EOF：进程退出
                sb.Append(buf, 0, n);

                var text = sb.ToString();
                var idx = text.IndexOf(marker, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    var after = text[(idx + marker.Length)..];   // ":<code>\n..."
                    var codeStr = after.TrimStart(':').Trim();
                    var lineEnd = codeStr.IndexOf('\n');
                    if (lineEnd >= 0) codeStr = codeStr[..lineEnd];
                    var exitCode = int.TryParse(codeStr.Trim(), out var c) ? c : 0;
                    return (text[..idx].TrimEnd('\r', '\n'), exitCode, false);
                }
            }
            return (sb.ToString().TrimEnd('\r', '\n'), 0, false);
        }
        catch (OperationCanceledException)
        {
            return (sb.ToString().TrimEnd('\r', '\n'), 0, true);
        }
    }

    /// <summary>终止会话进程（下次 RunAsync 自动重建）。</summary>
    private void KillSession()
    {
        try { if (_proc != null && !_proc.HasExited) _proc.Kill(entireProcessTree: true); } catch { }
        try { _proc?.Dispose(); } catch { }
        _proc = null;
    }

    public void Dispose()
    {
        _lock.Wait();
        try { KillSession(); }
        finally { _lock.Release(); }
    }
}

/// <summary>
/// 持久 shell 会话管理器 —— 按 session_id 缓存 PersistentShell，
/// 空闲超时自动回收、进程崩溃自动重建。所有会话串行化以避免并发写同一 stdin。
/// </summary>
public static class PersistentShellManager
{
    private static readonly Dictionary<string, PersistentShell> _shells = new();
    private static readonly SemaphoreSlim _lock = new(1, 1);
    // 单位毫秒，与 Environment.TickCount64 一致。此前 TimeSpan.FromMinutes(5).Ticks 是 100 纳秒（3e9），
    // 与毫秒差值比较，5 分钟回收实际约 34.7 天，空闲 shell 进程长期不释放。
    private static readonly long IdleTimeoutMs = 5L * 60 * 1000;

    /// <summary>在指定会话中执行命令（会话不存在则创建）。</summary>
    public static async Task<string> RunAsync(string sessionId, string command, int timeoutSec)
    {
        PersistentShell shell;
        await _lock.WaitAsync();
        try
        {
            CleanupStale();
            if (!_shells.TryGetValue(sessionId, out shell!))
            {
                shell = new PersistentShell(sessionId);
                _shells[sessionId] = shell;
            }
        }
        finally
        {
            _lock.Release();
        }

        return await shell.RunAsync(command, timeoutSec);
    }

    /// <summary>回收已退出或空闲超时的会话。</summary>
    private static void CleanupStale()
    {
        var now = Environment.TickCount64;
        var stale = _shells
            .Where(kv => !kv.Value.IsAlive || (now - kv.Value.LastUsedTicks) > IdleTimeoutMs)
            .Select(kv => kv.Key)
            .ToList();
        foreach (var key in stale)
        {
            var s = _shells[key];
            _shells.Remove(key);
            s.Dispose();
        }
    }

    /// <summary>关闭所有会话（进程退出/测试清理时调用）。</summary>
    public static void ShutdownAll()
    {
        _lock.Wait();
        try
        {
            foreach (var s in _shells.Values) s.Dispose();
            _shells.Clear();
        }
        finally
        {
            _lock.Release();
        }
    }
}
