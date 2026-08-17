using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Shared;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// Toast 通知队列管理器 —— 管理多条 Toast 的排队显示。
/// 同一时间只显示 1 条，剩余排队；每条自动超时后显示下一条。
/// 最多排队 5 条，超出则丢弃最旧的。
/// </summary>
public static class TuiToastQueue
{
    public enum ToastType { Info, Success, Warn, Error }

    private static readonly Queue<ToastItem> _queue = new();
    private static readonly Lock _lock = new();
    private static CancellationTokenSource? _cts;
    private static Task? _worker;
    private static volatile ToastItem? _current; // 跨线程（worker 写 / UI 渲染线程读），volatile 保证可见性

    /// <summary>最大排队数量</summary>
    public static int MaxQueueSize { get; set; } = 5;

    /// <summary>默认显示时长（毫秒）</summary>
    public static int DefaultDurationMs { get; set; } = 2500;

    /// <summary>当前显示的 Toast（null=无）</summary>
    public static ToastItem? Current => _current;

    /// <summary>排队中的 Toast 数量</summary>
    public static int PendingCount
    {
        get { lock (_lock) return _queue.Count; }
    }

    /// <summary>
    /// 加入一条 Toast 到队列。
    /// </summary>
    /// <param name="message">消息文本</param>
    /// <param name="type">类型（影响图标和颜色）</param>
    /// <param name="durationMs">显示时长（0=使用默认值）</param>
    public static void Enqueue(string message, ToastType type = ToastType.Info, int durationMs = 0)
    {
        var item = new ToastItem
        {
            Message = message,
            Type = type,
            DurationMs = durationMs > 0 ? durationMs : DefaultDurationMs,
            CreatedAt = DateTime.UtcNow,
        };

        lock (_lock)
        {
            // 去重：同消息 2 秒内不重复添加
            if (_queue.Any(t => t.Message == message && (DateTime.UtcNow - t.CreatedAt).TotalSeconds < 2))
                return;

            _queue.Enqueue(item);
            while (_queue.Count > MaxQueueSize)
                _queue.Dequeue();
        }

        // 确保工作循环在跑
        StartWorker();
    }

    /// <summary>获取当前 Toast 的渲染字符串（供 ChatScreen 调用）。</summary>
    public static string? Render(int terminalWidth)
    {
        var current = _current;
        if (current == null) return null;

        var icon = current.Type switch
        {
            ToastType.Success => "✓",
            ToastType.Warn => "⚠",
            ToastType.Error => "✘",
            _ => "ℹ",
        };

        var color = current.Type switch
        {
            ToastType.Success => AnsiTty.Fg(32),
            ToastType.Warn => AnsiTty.Fg(33),
            ToastType.Error => AnsiTty.Fg(31),
            _ => AnsiTty.Fg(36),
        };

        var maxW = Math.Min(terminalWidth - 4, 60);
        var text = current.Message;
        if (AnsiHelper.DisplayWidth(text) > maxW)
            text = AnsiHelper.TruncateByWidth(text, maxW);

        var bg = AnsiTty.Bg(0);
        return $"{color}{bg} {icon} {text}{AnsiTty.SgrReset}";
    }

    // ── 内部 ──

    private static void StartWorker()
    {
        lock (_lock)
        {
            if (_worker != null && !_worker.IsCompleted) return;
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _worker = Task.Run(() => RunAsync(_cts.Token));
        }
    }

    private static async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            ToastItem? next = null;
            lock (_lock)
            {
                if (_queue.Count > 0)
                    next = _queue.Dequeue();
            }

            if (next == null)
            {
                _current = null;
                break; // 队列空，停止工作循环
            }

            _current = next;
            try { await Task.Delay(next.DurationMs, ct); }
            catch (OperationCanceledException) { break; }
        }
        _current = null;
    }

    /// <summary>清空所有待显示的 Toast。</summary>
    public static void Clear()
    {
        lock (_lock) _queue.Clear();
        _current = null;
    }

    public class ToastItem
    {
        public string Message { get; init; } = "";
        public ToastType Type { get; init; }
        public int DurationMs { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
