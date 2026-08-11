namespace WayCoder;

/// <summary>
/// 代码片段管理器 —— 可复用的代码模板存储与检索。
///
/// 存储结构：.waycoder/snippets/*.md
/// 每个文件一个片段，YAML frontmatter 含 name/tags/language 元数据。
/// </summary>
public static class SnippetStore
{
    /// <summary>默认片段存储目录</summary>
    public static string DefaultDir => Path.Combine(
        Environment.CurrentDirectory, ".waycoder", "snippets");

    /// <summary>已缓存的片段列表</summary>
    private static readonly Dictionary<string, Snippet> _cache = new();
    private static bool _loaded;
    private static string? _loadedDir;

    /// <summary>代码片段</summary>
    public class Snippet
    {
        public string Name { get; init; } = "";
        public string Content { get; set; } = "";
        public string Language { get; init; } = "";
        public List<string> Tags { get; init; } = [];
        public DateTime CreatedAt { get; init; }
    }

    /// <summary>确保索引已加载。</summary>
    private static void EnsureLoaded(string? dir = null)
    {
        var d = dir ?? DefaultDir;
        if (_loaded && _loadedDir == d) return;
        Load(d);
    }

    /// <summary>从磁盘加载所有片段。</summary>
    public static void Load(string? dir = null)
    {
        var d = dir ?? DefaultDir;
        _cache.Clear();

        if (!Directory.Exists(d))
        {
            Directory.CreateDirectory(d);
            // 创建 README
            var readme = Path.Combine(d, "README.md");
            if (!File.Exists(readme))
            {
                File.WriteAllText(readme, """
                    ---
                    name: readme
                    description: 片段使用说明
                    ---
                    # 代码片段

                    在此目录下创建 `.md` 文件，每个文件一个片段。

                    ## 文件格式

                    ```markdown
                    ---
                    name: my-snippet
                    tags: [utility, string]
                    language: csharp
                    ---
                    这里放代码内容...
                    ```

                    ## 使用方式

                    在 Agent 对话中提及片段名即可自动检索。
                    """);
            }

            _loaded = true;
            _loadedDir = d;
            return;
        }

        foreach (var file in Directory.GetFiles(d, "*.md"))
        {
            try
            {
                var snippet = ParseSnippetFile(file);
                if (snippet != null)
                    _cache[snippet.Name] = snippet;
            }
            catch { /* 跳过损坏的文件 */ }
        }

        _loaded = true;
        _loadedDir = d;
    }

    /// <summary>
    /// 添加或更新一个代码片段。
    /// </summary>
    /// <param name="name">片段名（唯一标识）</param>
    /// <param name="content">代码内容</param>
    /// <param name="language">编程语言</param>
    /// <param name="tags">标签列表</param>
    public static void Add(string name, string content, string language = "", List<string>? tags = null)
    {
        EnsureLoaded();
        var snippet = new Snippet
        {
            Name = name,
            Content = content,
            Language = language,
            Tags = tags ?? [],
            CreatedAt = DateTime.UtcNow,
        };

        _cache[name] = snippet;

        // 持久化到文件
        var dir = _loadedDir ?? DefaultDir;
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{SanitizeFileName(name)}.md");
        var fileContent = BuildFileContent(snippet);
        File.WriteAllText(path, fileContent);
    }

    /// <summary>
    /// 搜索片段：按名称或标签模糊匹配。
    /// </summary>
    /// <param name="query">搜索关键词（空格分隔多词，OR 逻辑）</param>
    /// <returns>匹配的片段列表</returns>
    public static List<Snippet> Search(string query)
    {
        EnsureLoaded();
        if (string.IsNullOrWhiteSpace(query))
            return _cache.Values.OrderByDescending(s => s.CreatedAt).ToList();

        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.ToLowerInvariant()).ToList();

        return _cache.Values
            .Where(s =>
            {
                var haystack = $"{s.Name} {string.Join(" ", s.Tags)} {s.Language}".ToLowerInvariant();
                return terms.Any(t => haystack.Contains(t));
            })
            .OrderByDescending(s => s.CreatedAt)
            .ToList();
    }

    /// <summary>列出所有片段。</summary>
    public static List<Snippet> List()
    {
        EnsureLoaded();
        return _cache.Values.OrderBy(s => s.Name).ToList();
    }

    /// <summary>删除指定片段。</summary>
    public static bool Delete(string name)
    {
        EnsureLoaded();
        if (!_cache.Remove(name)) return false;

        var dir = _loadedDir ?? DefaultDir;
        var path = Path.Combine(dir, $"{SanitizeFileName(name)}.md");
        try { if (File.Exists(path)) File.Delete(path); } catch { }
        return true;
    }

    /// <summary>获取单个片段内容。</summary>
    public static string? Get(string name)
    {
        EnsureLoaded();
        return _cache.TryGetValue(name, out var s) ? s.Content : null;
    }

    // ── 内部 ──

    private static Snippet? ParseSnippetFile(string path)
    {
        var content = File.ReadAllText(path);
        var name = Path.GetFileNameWithoutExtension(path);
        var language = "";
        var tags = new List<string>();
        var body = content;

        var fmMatch = System.Text.RegularExpressions.Regex.Match(
            content, @"^---\s*\n(.*?)\n---\s*\n(.*)", System.Text.RegularExpressions.RegexOptions.Singleline);

        if (fmMatch.Success)
        {
            var fm = fmMatch.Groups[1].Value;
            body = fmMatch.Groups[2].Value.Trim();

            name = ExtractField(fm, "name") ?? name;
            language = ExtractField(fm, "language") ?? "";
            var tagsStr = ExtractField(fm, "tags");
            if (!string.IsNullOrEmpty(tagsStr))
            {
                tags = tagsStr.Trim('[', ']').Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim().Trim('"', '\'')).ToList();
            }
        }

        return new Snippet
        {
            Name = name,
            Content = body,
            Language = language,
            Tags = tags,
            CreatedAt = File.GetLastWriteTimeUtc(path),
        };
    }

    private static string BuildFileContent(Snippet s)
    {
        var fm = new System.Text.StringBuilder();
        fm.AppendLine("---");
        fm.AppendLine($"name: {s.Name}");
        if (!string.IsNullOrEmpty(s.Language))
            fm.AppendLine($"language: {s.Language}");
        if (s.Tags.Count > 0)
            fm.AppendLine($"tags: [{string.Join(", ", s.Tags)}]");
        fm.AppendLine("---");
        fm.AppendLine();
        fm.Append(s.Content);
        return fm.ToString();
    }

    private static string? ExtractField(string yaml, string key)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            yaml, $@"^{key}\s*:\s*(.+)$", System.Text.RegularExpressions.RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }
}
