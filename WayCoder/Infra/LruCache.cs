using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace WayCoder;

/// <summary>
/// 线程安全、支持 TTL 过期与容量淘汰的泛型 LRU（最近最少使用）缓存。
///
/// 内部用 <see cref="Dictionary{K,V}"/> + <see cref="LinkedList{Node}"/> 实现，
/// Get / Put / Remove 均为 O(1) 时间复杂度的平均操作；读多写少场景用
/// <see cref="ReaderWriterLockSlim"/> 提升并发读吞吐。
///
/// 特性：
///  - Capacity 容量超限时，淘汰最久未使用的条目（LRU 策略）
///  - 每条目可设置独立 TTL，Get 时惰性检查并清理过期项
///  - 条目被淘汰（容量淘汰 / 过期 / 显式 Remove / Clear）时触发 <see cref="OnEvicted"/>
///  - 完全基于 BCL，无反射，NativeAOT 兼容
/// </summary>
/// <typeparam name="K">键类型</typeparam>
/// <typeparam name="V">值类型</typeparam>
public sealed class LruCache<K, V> where K : notnull
{
    /// <summary>缓存节点：键、值、TTL 截止时间（UTC）。</summary>
    private sealed class Node
    {
        public K Key;
        public V Value;
        public DateTimeOffset? ExpiresAt;

        public Node(K key, V value, DateTimeOffset? expiresAt)
        {
            Key = key;
            Value = value;
            ExpiresAt = expiresAt;
        }
    }

    private readonly Dictionary<K, LinkedListNode<Node>> _map;
    private readonly LinkedList<Node> _list = new();
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    private readonly int _capacity;
    private readonly TimeSpan? _defaultTtl;

    private long _hits;
    private long _misses;
    private long _evictions;

    /// <summary>条目被淘汰时触发。参数为被淘汰的键值。</summary>
    public event Action<K, V>? OnEvicted;

    /// <summary>当前缓存条目数。</summary>
    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try { return _map.Count; }
            finally { _lock.ExitReadLock(); }
        }
    }

    /// <summary>缓存最大容量。条目数超过该值时会淘汰最久未使用的项。</summary>
    public int Capacity => _capacity;

    /// <summary>命中次数（Get 返回有效值）。</summary>
    public long Hits
    {
        get { _lock.EnterReadLock(); try { return _hits; } finally { _lock.ExitReadLock(); } }
    }

    /// <summary>未命中次数（Get 未找到或已过期）。</summary>
    public long Misses
    {
        get { _lock.EnterReadLock(); try { return _misses; } finally { _lock.ExitReadLock(); } }
    }

    /// <summary>累计被淘汰次数（容量淘汰 + 过期清理 + 显式 Remove + Clear）。</summary>
    public long Evictions
    {
        get { _lock.EnterReadLock(); try { return _evictions; } finally { _lock.ExitReadLock(); } }
    }

    /// <summary>当前缓存中的全部键（快照，不保证顺序）。</summary>
    public IReadOnlyList<K> Keys
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                var keys = new K[_map.Count];
                var i = 0;
                foreach (var node in _list)
                    keys[i++] = node.Key;
                return keys;
            }
            finally { _lock.ExitReadLock(); }
        }
    }

    /// <summary>
    /// 创建 LRU 缓存。
    /// </summary>
    /// <param name="capacity">容量上限，必须为正数。超过时淘汰最久未使用的条目。</param>
    /// <param name="defaultTtl">默认过期时间；为 null 时条目默认永不过期，除非 Put 时单独指定。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> 小于等于 0 时抛出。</exception>
    public LruCache(int capacity, TimeSpan? defaultTtl = null)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity 必须为正数。");

        _capacity = capacity;
        _defaultTtl = defaultTtl;
        _map = new Dictionary<K, LinkedListNode<Node>>(capacity);
    }

    /// <summary>
    /// 读取键对应的值。若键不存在、已过期或已被淘汰，返回默认值并计入未命中。
    /// 命中时会将条目提升为最近使用，并返回其值。
    /// </summary>
    /// <param name="key">要查询的键。</param>
    /// <returns>键对应的值；未命中时返回默认值 <c>default(V)</c>。</returns>
    public V? Get(K key)
    {
        var now = DateTimeOffset.UtcNow;

        _lock.EnterUpgradeableReadLock();
        try
        {
            if (!_map.TryGetValue(key, out var link))
            {
                _lock.EnterWriteLock();
                try { _misses++; }
                finally { _lock.ExitWriteLock(); }
                return default;
            }

            var node = link.Value;
            if (node.ExpiresAt is { } exp && exp <= now)
            {
                _lock.EnterWriteLock();
                try
                {
                    _misses++;
                    RemoveNode(link, evict: true);
                }
                finally { _lock.ExitWriteLock(); }
                return default;
            }

            // 命中：移到链表尾部表示最近使用
            _lock.EnterWriteLock();
            try
            {
                _hits++;
                _list.Remove(link);
                _list.AddLast(link);
            }
            finally { _lock.ExitWriteLock(); }

            return node.Value;
        }
        finally { _lock.ExitUpgradeableReadLock(); }
    }

    /// <summary>
    /// 写入（或覆盖）键值对。若键已存在则覆盖并提升为最近使用；若不存在则新增。
    /// 新增后若超出容量，则淘汰最久未使用的条目。
    /// </summary>
    /// <param name="key">键。</param>
    /// <param name="value">值。</param>
    /// <param name="ttl">此条目的过期时间；为 null 时使用构造时设置的默认 TTL（无默认则永不过期）。</param>
    public void Put(K key, V value, TimeSpan? ttl = null)
    {
        var expiresAt = ttl is { } t && t > TimeSpan.Zero
            ? DateTimeOffset.UtcNow + t
            : _defaultTtl is { } dt && dt > TimeSpan.Zero
                ? DateTimeOffset.UtcNow + dt
                : (DateTimeOffset?)null;

        _lock.EnterWriteLock();
        try
        {
            if (_map.TryGetValue(key, out var existing))
            {
                var node = existing.Value;
                node.Value = value;
                node.ExpiresAt = expiresAt;
                _list.Remove(existing);
                _list.AddLast(existing);
                return;
            }

            var newNode = new Node(key, value, expiresAt);
            var link = _list.AddLast(newNode);
            _map[key] = link;

            // 超容：淘汰最久未使用（链表头部），保留最近写入
            while (_map.Count > _capacity)
            {
                var first = _list.First!;
                RemoveNode(first, evict: true);
            }
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>
    /// 移除指定键的条目。
    /// </summary>
    /// <param name="key">要移除的键。</param>
    /// <returns>若键存在并被移除返回 true，否则返回 false。</returns>
    public bool Remove(K key)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_map.TryGetValue(key, out var link))
            {
                RemoveNode(link, evict: true);
                return true;
            }
            return false;
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>清空缓存中的所有条目。</summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            if (_map.Count == 0) return;

            // 先复制事件参数，避免在锁内触发用户回调造成死锁/重入风险
            var victims = new List<(K, V)>(_map.Count);
            foreach (var link in _list)
                victims.Add((link.Key, link.Value));

            _map.Clear();
            _list.Clear();
            _evictions += victims.Count;

            if (OnEvicted is { } handler)
                foreach (var (k, v) in victims)
                    handler(k, v);
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>判断缓存中是否存在指定键且未过期。</summary>
    /// <param name="key">要检查的键。</param>
    /// <returns>存在且未过期返回 true，否则返回 false。</returns>
    public bool ContainsKey(K key)
    {
        var now = DateTimeOffset.UtcNow;

        _lock.EnterUpgradeableReadLock();
        try
        {
            if (!_map.TryGetValue(key, out var link))
                return false;

            var node = link.Value;
            if (node.ExpiresAt is { } exp && exp <= now)
            {
                _lock.EnterWriteLock();
                try { RemoveNode(link, evict: true); }
                finally { _lock.ExitWriteLock(); }
                return false;
            }
            return true;
        }
        finally { _lock.ExitUpgradeableReadLock(); }
    }

    /// <summary>
    /// 尝试读取键对应的值。
    /// </summary>
    /// <param name="key">键。</param>
    /// <param name="value">命中时输出值；未命中时输出默认值。</param>
    /// <returns>命中且未过期返回 true，否则返回 false。</returns>
    public bool TryGet(K key, [MaybeNullWhen(false)] out V value)
    {
        var now = DateTimeOffset.UtcNow;

        _lock.EnterUpgradeableReadLock();
        try
        {
            if (!_map.TryGetValue(key, out var link))
            {
                _lock.EnterWriteLock();
                try { _misses++; }
                finally { _lock.ExitWriteLock(); }
                value = default;
                return false;
            }

            var node = link.Value;
            if (node.ExpiresAt is { } exp && exp <= now)
            {
                _lock.EnterWriteLock();
                try
                {
                    _misses++;
                    RemoveNode(link, evict: true);
                }
                finally { _lock.ExitWriteLock(); }
                value = default;
                return false;
            }

            _lock.EnterWriteLock();
            try
            {
                _hits++;
                _list.Remove(link);
                _list.AddLast(link);
            }
            finally { _lock.ExitWriteLock(); }

            value = node.Value;
            return true;
        }
        finally { _lock.ExitUpgradeableReadLock(); }
    }

    /// <summary>从内部结构中移除指定节点并触发淘汰事件。调用方须持有写锁。</summary>
    private void RemoveNode(LinkedListNode<Node> link, bool evict)
    {
        _map.Remove(link.Value.Key);
        _list.Remove(link);

        if (evict)
        {
            _evictions++;
            if (OnEvicted is { } handler)
                handler(link.Value.Key, link.Value.Value);
        }
    }
}
