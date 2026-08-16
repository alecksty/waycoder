namespace WayCoder.UI.Tui;

/// <summary>
/// 输入历史管理器 —— 按字段名记录最近输入。
/// 内存环形缓冲 + 简单文本文件持久化（无 JSON 依赖，AOT 安全）。
/// </summary>
public static class TuiInputHistory
{
    private const int MaxPerField = 50;
    private const int GlobalMax = 500;

    private static readonly Dictionary<string, List<string>> _store = new(StringComparer.OrdinalIgnoreCase);
    private static string? _persistPath;

    static TuiInputHistory()
    {
        LoadFromDisk();
    }

    /// <summary>获取指定字段的历史（最新在前）</summary>
    public static List<string> Get(string fieldName)
        => _store.TryGetValue(fieldName, out var list) ? [.. list] : [];

    /// <summary>添加一条历史（去重 + 裁剪）</summary>
    public static void Add(string fieldName, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (!_store.TryGetValue(fieldName, out var list))
            _store[fieldName] = list = [];

        list.Remove(value);
        list.Insert(0, value);
        while (list.Count > MaxPerField) list.RemoveAt(list.Count - 1);

        // 全局裁剪
        int total = _store.Sum(kv => kv.Value.Count);
        while (total > GlobalMax)
        {
            foreach (var kv in _store)
            {
                if (kv.Value.Count > 0) { kv.Value.RemoveAt(kv.Value.Count - 1); total--; if (total <= GlobalMax) break; }
            }
        }
        SaveToDisk();
    }

    /// <summary>清除指定字段</summary>
    public static void Clear(string fieldName) { _store.Remove(fieldName); SaveToDisk(); }

    /// <summary>清除全部</summary>
    public static void ClearAll() { _store.Clear(); SaveToDisk(); }

    /// <summary>设置持久化路径并加载</summary>
    public static void SetPersistPath(string path) { _persistPath = path; LoadFromDisk(); }

    // ── 简单文本持久化（AOT 安全） ──

    private static void SaveToDisk()
    {
        if (string.IsNullOrEmpty(_persistPath)) return;
        try
        {
            var dir = Path.GetDirectoryName(_persistPath);
            if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var lines = new List<string>();
            foreach (var kv in _store)
                foreach (var v in kv.Value)
                    lines.Add(Escape(kv.Key) + '|' + Escape(v));
            File.WriteAllLines(_persistPath, lines);
        }
        catch { }
    }

    private static void LoadFromDisk()
    {
        if (string.IsNullOrEmpty(_persistPath) || !File.Exists(_persistPath)) return;
        try
        {
            foreach (var line in File.ReadAllLines(_persistPath))
            {
                var idx = line.IndexOf('|');
                if (idx < 0) continue;
                var field = Unescape(line[..idx]);
                var value = Unescape(line[(idx + 1)..]);
                if (string.IsNullOrEmpty(field) || string.IsNullOrEmpty(value)) continue;
                if (!_store.TryGetValue(field, out var list))
                    _store[field] = list = [];
                if (list.Count < MaxPerField) list.Add(value);
            }
        }
        catch { }
    }

    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("|", "\\p").Replace("\n", "\\n").Replace("\r", "\\r");
    private static string Unescape(string s) => s.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\p", "|").Replace("\\\\", "\\");
}
