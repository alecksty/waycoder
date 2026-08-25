using System.Text;
using WayCoder.Infra;

namespace WayCoder;

/// <summary>
/// 代码块嵌入缓存 —— 为 <see cref="CodeKnowledge"/> 的符号块缓存语义向量。
/// 与 <see cref="EmbeddingStore"/>（记忆条目 .vec）互补：这里是按代码块键（Title+内容哈希）存储，
/// 项目级 .waycoder/code-embeddings.json（JNode 原子写）。
///
/// 块键 = Title（rel › sym）+ "|" + SHA256(content)[..8]：内容变了键变天然失效；没变键稳定复用。
/// Prune 清理已不存在的块键（防孤儿膨胀）。
/// </summary>
public static class CodeEmbeddingCache
{
    static string StorePath => Global.WriteConfigPath(Directory.GetCurrentDirectory(), "code-embeddings.json");

    static Dictionary<string, float[]>? _cache;
    static bool _dirty;

    static Dictionary<string, float[]> Cache
    {
        get
        {
            if (_cache != null) return _cache;
            _cache = new Dictionary<string, float[]>(StringComparer.Ordinal);
            if (File.Exists(StorePath))
            {
                try
                {
                    var root = Json.Parse(File.ReadAllText(StorePath));
                    foreach (var n in root?["embeddings"]?.Items ?? [])
                    {
                        var key = n["key"]?.AsString() ?? "";
                        if (key.Length == 0) continue;
                        var vec = (n["vec"]?.Items ?? [])
                            .Select(v => (float)(v?.AsNumber() ?? 0.0)).ToArray();
                        if (vec.Length > 0) _cache[key] = vec;
                    }
                }
                catch { }
            }
            return _cache;
        }
    }

    static void Save()
    {
        if (!_dirty) return;
        try
        {
            var dir = Path.GetDirectoryName(StorePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var arr = JNode.Array();
            foreach (var kv in Cache)
            {
                var vec = JNode.Array();
                foreach (var f in kv.Value) vec.Add(JNode.Num(f));
                arr.Add(JNode.Object().Set("key", kv.Key).Set("vec", vec));
            }
            var tmp = StorePath + ".tmp";
            File.WriteAllText(tmp, JNode.Object().Set("embeddings", arr).ToJson());
            File.Move(tmp, StorePath, overwrite: true);
            _dirty = false;
        }
        catch { }
    }

    /// <summary>计算代码块的稳定键（Title + 内容哈希前缀）。内容变 → 键变 → 缓存失效。</summary>
    public static string ChunkKey(SemanticMemory.MemoryDocument doc)
    {
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(doc.Content)))[..8];
        return $"{doc.Title}|{hash}";
    }

    /// <summary>取块向量（未命中返回 null）。</summary>
    public static float[]? GetVector(string key)
        => Cache.TryGetValue(key, out var v) ? v : null;

    /// <summary>保存块向量（原子写）。</summary>
    public static void SaveVector(string key, float[] vector)
    {
        if (vector.Length == 0) return;
        Cache[key] = vector;
        _dirty = true;
        Save();
    }

    /// <summary>清理不在当前有效键集内的孤儿键（文件删除/内容变更后的旧嵌入）。返回清理数。</summary>
    public static int Prune(HashSet<string> validKeys)
    {
        int removed = 0;
        foreach (var key in Cache.Keys.ToList())
        {
            if (!validKeys.Contains(key)) { Cache.Remove(key); removed++; }
        }
        if (removed > 0) { _dirty = true; Save(); }
        return removed;
    }

    /// <summary>当前缓存向量数。</summary>
    public static int Count => Cache.Count;

    /// <summary>清空缓存（测试用）。</summary>
    public static void Reset()
    {
        try { if (File.Exists(StorePath)) File.Delete(StorePath); } catch { }
        _cache = null;
        _dirty = false;
    }
}
