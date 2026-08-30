namespace WayCoder;

/// <summary>
/// 线程安全的字符串去重集合（快照枚举）。
///
/// 用于 <see cref="Tools.EditFileTool.ChangedFiles"/> 等「多个并行槽位并发写入、
/// 主线程 / 其他 Agent 并发读取」的共享集合场景，替代无锁的
/// <see cref="HashSet{T}"/>——后者的 Add 与枚举并发时会导致集合内部状态损坏
/// （遍历抛异常 / 元素丢失 / 无限循环）。
///
/// 语义与 HashSet&lt;string&gt; 的常用子集对齐（Add/Clear/Contains/Count），
/// 枚举返回快照，调用方在枚举期间不受并发写入影响。
/// </summary>
public sealed class ThreadSafeStringSet : IEnumerable<string>
{
    private readonly HashSet<string> _set = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    /// <summary>最大容量（0=不限）。超限时清空重建，防长期会话无限累积（如 ChangedFiles 全会话累计）。</summary>
    public int MaxCount { get; init; }

    /// <summary>当前元素个数。</summary>
    public int Count { get { lock (_lock) return _set.Count; } }

    /// <summary>添加元素（已存在则忽略并返回 false）。超 <see cref="MaxCount"/> 时清空重建（防无限累积）。线程安全。</summary>
    public bool Add(string item)
    {
        lock (_lock)
        {
            if (MaxCount > 0 && _set.Count >= MaxCount)
                _set.Clear(); // 超限重置：ChangedFiles 等只用于「本会话改了哪些」，清空影响可接受
            return _set.Add(item);
        }
    }

    /// <summary>是否包含指定元素。线程安全。</summary>
    public bool Contains(string item) { lock (_lock) return _set.Contains(item); }

    /// <summary>清空集合。线程安全。</summary>
    public void Clear() { lock (_lock) _set.Clear(); }

    /// <summary>返回当前元素的快照列表（枚举期间不受并发写入影响）。</summary>
    public List<string> ToList() { lock (_lock) return new List<string>(_set); }

    /// <inheritdoc/>
    public IEnumerator<string> GetEnumerator() => ToList().GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
