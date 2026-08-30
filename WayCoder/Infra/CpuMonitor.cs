using System.Diagnostics;

namespace WayCoder;

/// <summary>
/// CPU 占用检测器（动态栏显示 + 超阈值 dump）。
/// 用「缓存的当前进程实例 + TotalProcessorTime 差值 / 墙钟差值」算占用百分比，
/// 纯 BCL、零反射零 NuGet（AOT 兼容），跨平台（Process.TotalProcessorTime 内部处理系统差异）。
///
/// 采样由 TuiAnimTicker 心跳线程每 ~5s 调一次 <see cref="Sample"/>，结果写入
/// <see cref="LastPercent"/>；内部有 2s 最小采样间隔保护，避免过频。
/// </summary>
public static class CpuMonitor
{
    private static Process? _proc;          // 缓存进程实例（勿每次 new）
    private static TimeSpan _lastCpu;       // 上次 CPU 累计时间
    private static long _lastWall;          // 上次墙钟（Stopwatch.GetTimestamp）
    private static double _lastPercent;     // 最近一次采样 CPU 占用%（0-100，单核百分比）
    private static readonly object _lock = new();

    /// <summary>最近一次采样 CPU 占用%（0-100）。volatile 不支持 double，用锁读。</summary>
    public static double LastPercent { get { lock (_lock) return _lastPercent; } }

    /// <summary>
    /// 采样一次 CPU 占用%。间隔太短（&lt;2s）时返回上次值（避免过频刷新）。
    /// 首次调用只记录基线返回 0。
    /// </summary>
    public static double Sample()
    {
        lock (_lock)
        {
            var proc = _proc ??= Process.GetCurrentProcess();
            var now = Stopwatch.GetTimestamp();
            if (_lastWall == 0)
            {
                proc.Refresh();
                _lastCpu = proc.TotalProcessorTime;
                _lastWall = now;
                return 0;
            }

            var wallMs = (now - _lastWall) * 1000.0 / Stopwatch.Frequency;
            if (wallMs < 2000) return _lastPercent; // 采样间隔过短，用上次值

            proc.Refresh();
            var cpuMs = (proc.TotalProcessorTime - _lastCpu).TotalMilliseconds;
            _lastCpu = proc.TotalProcessorTime;
            _lastWall = now;
            _lastPercent = wallMs > 0 ? Math.Clamp(cpuMs / wallMs * 100.0, 0, 100) : 0;
            return _lastPercent;
        }
    }
}
