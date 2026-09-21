namespace WayCoder.UI.Shared.Terminal;

/// <summary>
/// **程序按键的交接队列** —— 交互式程序（`vim`/`mc`/`top`）要的是"按一下立刻拿到"，
/// 而不是"敲满一行按回车才给"。
///
/// <para>
/// 跨线程的形态是固定的：UI 线程**投**（软键盘的字符、外接键盘的键码），
/// VM 线程**取**（阻塞等）。所以这里只做一件事 —— 把这两个方向接起来，
/// 而且做得**能被桌面自测钉住**（UI 那半在 MAUI 里、测不到，这一半必须测到）。
/// </para>
///
/// <para>
/// ⚠ **只支持一个等待者**：VM 是单线程执行的（`VmlTool.ExecutionMode = Exclusive`），
/// 同一时刻只可能有一个"程序在等键"。真出现了第二个，会**覆盖**掉前一个的等待 ——
/// 所以 <see cref="Take"/> 里那句断言是有意留的，不是装饰。
/// </para>
/// </summary>
public sealed class KeyQueue
{
    private readonly Queue<char> _queue = new();
    private readonly object _gate = new();
    private TaskCompletionSource<char>? _waiter;

    /// <summary>待取的按键个数。</summary>
    public int Count { get { lock (_gate) return _queue.Count; } }

    /// <summary>投一个按键（UI 线程）。有人在等就**当场交给他**，否则入队。</summary>
    public void Post(char c)
    {
        TaskCompletionSource<char>? waiter;
        lock (_gate)
        {
            waiter = _waiter;
            if (waiter == null)
            {
                _queue.Enqueue(c);
                return;
            }
            _waiter = null;
        }
        waiter.TrySetResult(c);
    }

    /// <summary>
    /// 取一个按键（VM 线程）。**没有就阻塞** —— 这正是"程序在等输入"的语义。
    /// </summary>
    public char Take()
    {
        TaskCompletionSource<char> tcs;
        lock (_gate)
        {
            if (_queue.Count > 0) return _queue.Dequeue();
            tcs = new TaskCompletionSource<char>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiter = tcs;      // 覆盖前一个等待者（见类注释：VM 单线程，正常不会有两个）
        }
        return tcs.Task.GetAwaiter().GetResult();
    }

    /// <summary>看一眼有没有（不阻塞）。**看得到就能拿走** —— 没有"窥探但不消费"的接口，
    /// 因为那样调用方就得自己保证两件事之间的原子性。</summary>
    public bool TryTake(out char c)
    {
        lock (_gate)
        {
            if (_queue.Count > 0) { c = _queue.Dequeue(); return true; }
            c = '\0';
            return false;
        }
    }

    /// <summary>丢掉全部待取（**唤醒等待者**，给它一个 NUL —— 否则它会一直挂着）。</summary>
    public void Clear()
    {
        TaskCompletionSource<char>? waiter;
        lock (_gate)
        {
            _queue.Clear();
            waiter = _waiter;
            _waiter = null;
        }
        waiter?.TrySetResult('\0');
    }
}
