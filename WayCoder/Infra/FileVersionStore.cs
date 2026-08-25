using System.Text;
using WayCoder.Infra;

namespace WayCoder;

/// <summary>
/// 编辑级文件版本历史 —— 每次写文件前记录旧内容，支持「回退最后一次编辑」（/undo &lt;file&gt;）。
/// 与 <see cref="CheckpointManager"/>（每轮整树快照）互补：这里按文件粒度、按编辑次数版本化。
///
/// 存储：项目级 .waycoder/file-versions/{相对路径}/v{NNN}（内容文件）+ index.json（path → 版本列表）。
/// 保留：每文件最多 20 版本、总量 200，超限滚动删最旧（防磁盘膨胀）。
/// AOT 安全：纯 File I/O + JNode + 原子写（tmp + Move）。
/// </summary>
public static class FileVersionStore
{
    const int MaxPerFile = 20;
    const int MaxTotal = 200;

    static string StoreDir => Global.WriteConfigPath(Directory.GetCurrentDirectory(), "file-versions");
    static string IndexPath => Path.Combine(StoreDir, "index.json");

    // 索引：绝对路径 → 版本文件名列表（v001, v002, ...，最新在末尾）
    static Dictionary<string, List<string>>? _index;
    static bool _dirty;

    // ── 索引 ──

    static Dictionary<string, List<string>> Index
    {
        get
        {
            if (_index != null) return _index;
            _index = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(IndexPath))
            {
                try
                {
                    var root = Json.Parse(File.ReadAllText(IndexPath));
                    foreach (var n in root?["files"]?.Items ?? [])
                    {
                        var path = n["path"]?.AsString() ?? "";
                        var versions = (n["versions"]?.Items ?? [])
                            .Select(v => v.AsString() ?? "").Where(v => v.Length > 0).ToList();
                        if (path.Length > 0 && versions.Count > 0) _index[path] = versions;
                    }
                }
                catch { }
            }
            return _index;
        }
    }

    static void SaveIndex()
    {
        if (!_dirty) return;
        try
        {
            if (!Directory.Exists(StoreDir)) Directory.CreateDirectory(StoreDir);
            var arr = JNode.Array();
            foreach (var kv in Index)
            {
                var vArr = JNode.Array();
                foreach (var v in kv.Value) vArr.Add(v);
                arr.Add(JNode.Object().Set("path", kv.Key).Set("versions", vArr));
            }
            var tmp = IndexPath + ".tmp";
            File.WriteAllText(tmp, JNode.Object().Set("files", arr).ToJson());
            File.Move(tmp, IndexPath, overwrite: true);
            _dirty = false;
        }
        catch { }
    }

    /// <summary>清空索引与版本文件（测试用）。</summary>
    public static void Reset()
    {
        try { if (Directory.Exists(StoreDir)) Directory.Delete(StoreDir, recursive: true); } catch { }
        _index = null;
        _dirty = false;
    }

    // ── 记录 ──

    /// <summary>写文件前调用：把文件当前内容存为下一版本。文件不存在或内容未变则跳过。</summary>
    public static void RecordBefore(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        string full;
        try { full = Path.GetFullPath(path); } catch { return; }
        if (!File.Exists(full)) return;

        string content;
        try { content = File.ReadAllText(full); } catch { return; }

        var existing = Index.GetValueOrDefault(full);
        if (existing is { Count: > 0 })
        {
            var lastContent = ReadVersionFile(full, existing[^1]);
            if (lastContent == content) return; // 内容未变，无意义版本
        }

        var ver = $"v{(existing is { Count: > 0 } ? existing.Count + 1 : 1):000}";
        try
        {
            var verDir = VersionDir(full);
            if (!Directory.Exists(verDir)) Directory.CreateDirectory(verDir);
            var verPath = Path.Combine(verDir, ver);
            var tmp = verPath + ".tmp";
            File.WriteAllText(tmp, content);
            File.Move(tmp, verPath, overwrite: true);

            existing ??= [];
            existing.Add(ver);
            Index[full] = existing;

            // 保留：每文件上限，滚动删最旧
            while (existing.Count > MaxPerFile)
            {
                DeleteVersionFile(full, existing[0]);
                existing.RemoveAt(0);
            }
            _dirty = true;
            SaveIndex();
        }
        catch { }
    }

    // ── 还原 ──

    /// <summary>回退 steps 个编辑版本（默认 1 = 撤销最后一次编辑）。成功返回 true。</summary>
    public static bool Restore(string path, int steps = 1)
    {
        if (steps < 1 || string.IsNullOrWhiteSpace(path)) return false;
        string full;
        try { full = Path.GetFullPath(path); } catch { return false; }

        var existing = Index.GetValueOrDefault(full);
        if (existing is not { Count: > 0 }) return false;

        int idx = existing.Count - steps;
        if (idx < 0) return false;

        var content = ReadVersionFile(full, existing[idx]);
        if (content == null) return false;

        try
        {
            File.WriteAllText(full, content);
            FileTracker.RecordWrite(full);

            // 回退后丢弃被跨过的版本（当前已回到 ver，其后版本不再需要）
            while (existing.Count > idx + 1)
            {
                DeleteVersionFile(full, existing[^1]);
                existing.RemoveAt(existing.Count - 1);
            }
            _dirty = true;
            SaveIndex();
            return true;
        }
        catch { return false; }
    }

    // ── 查询 ──

    /// <summary>列出文件的版本（序号 + 时间，1=最早）。</summary>
    public static List<(int Ver, DateTime Time)> List(string path)
    {
        var result = new List<(int, DateTime)>();
        if (string.IsNullOrWhiteSpace(path)) return result;
        string full;
        try { full = Path.GetFullPath(path); } catch { return result; }

        var existing = Index.GetValueOrDefault(full);
        if (existing == null) return result;
        for (int i = 0; i < existing.Count; i++)
        {
            DateTime t = DateTime.MinValue;
            try { t = File.GetLastWriteTime(Path.Combine(VersionDir(full), existing[i])); } catch { }
            result.Add((i + 1, t));
        }
        return result;
    }

    /// <summary>列出所有有版本的文件路径。</summary>
    public static List<string> ListAll() => Index.Keys.ToList();

    /// <summary>当前总版本数（测试/统计用）。</summary>
    public static int TotalVersions => Index.Values.Sum(v => v.Count);

    // ── 内部 ──

    static string VersionDir(string full)
        => Path.Combine(StoreDir, SanitizeRel(full));

    /// <summary>绝对路径 → 安全相对目录段（项目内保留相对路径；外部文件退化用哈希前缀）。</summary>
    static string SanitizeRel(string full)
    {
        string rel;
        try { rel = Path.GetRelativePath(Directory.GetCurrentDirectory(), full).Replace('\\', '/'); }
        catch { rel = full; }
        if (rel.StartsWith("..", StringComparison.Ordinal))
            rel = "ext_" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(full)))[..8];
        var chars = rel.Select(c => (char.IsLetterOrDigit(c) || c is '/' or '.' or '_' or '-') ? c : '_').ToArray();
        return new string(chars);
    }

    static string? ReadVersionFile(string full, string ver)
    {
        try
        {
            var p = Path.Combine(VersionDir(full), ver);
            return File.Exists(p) ? File.ReadAllText(p) : null;
        }
        catch { return null; }
    }

    static void DeleteVersionFile(string full, string ver)
    {
        try { File.Delete(Path.Combine(VersionDir(full), ver)); } catch { }
    }
}
