using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace WayCoder;

/// <summary>
/// 跨会话记忆自动检索 —— 启动时扫描 memory 文件，根据当前任务关键词
/// 匹配相关记忆并注入系统提示词，让 Agent 越用越聪明。
/// </summary>
public static class MemoryRetrieval
{
    private static readonly ConcurrentDictionary<string, MemoryItem> _index = new();

    /// <summary>记忆缓存项</summary>
    public record MemoryItem(string Name, string Description, string Content, string Type, DateTime UpdatedAt);

    /// <summary>是否已加载索引</summary>
    public static bool IsLoaded => _index.Count > 0;

    /// <summary>已索引的记忆数量</summary>
    public static int Count => _index.Count;

    /// <summary>
    /// 加载所有记忆文件到内存索引。
    /// 扫描 .waycoder/memory/*.md 和 MEMORY.md 索引文件。
    /// 支持 claude 兼容路径和 waycoder 自有路径。
    /// </summary>
    public static void Load(string? rootDir = null)
    {
        var dirs = GetMemoryDirs(rootDir);
        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir)) continue;

            // 扫描 .md 文件
            foreach (var file in Directory.GetFiles(dir, "*.md"))
            {
                try
                {
                    var item = ParseMemoryFile(file);
                    if (item != null)
                        _index[item.Name] = item;
                }
                catch { /* 忽略损坏的文件 */ }
            }
        }
    }

    /// <summary>
    /// 根据用户任务文本匹配相关记忆。
    /// 匹配算法：提取任务中的关键词，在记忆的 name/description 中查找命中。
    /// </summary>
    /// <param name="taskText">用户任务描述（取前 500 字符）</param>
    /// <param name="maxResults">最多返回条数（默认 5）</param>
    /// <returns>相关记忆列表，按匹配度排序</returns>
    public static List<MemoryItem> GetRelevant(string taskText, int maxResults = 5)
    {
        if (_index.Count == 0) return [];

        var keywords = ExtractKeywords(taskText);
        if (keywords.Count == 0) return [];

        var scored = new List<(MemoryItem item, int score)>();
        foreach (var item in _index.Values)
        {
            var score = 0;
            var searchText = $"{item.Name} {item.Description}".ToLowerInvariant();
            foreach (var kw in keywords)
            {
                if (item.Name.Equals(kw, StringComparison.OrdinalIgnoreCase))
                    score += 3;
                else if (searchText.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    score += 1;
            }
            if (score > 0)
                scored.Add((item, score));
        }

        return scored
            .OrderByDescending(s => s.score)
            .Take(maxResults)
            .Select(s => s.item)
            .ToList();
    }

    /// <summary>
    /// 格式化为系统提示词注入文本。
    /// </summary>
    public static string FormatForPrompt(IEnumerable<MemoryItem> items)
    {
        var list = items.ToList();
        if (list.Count == 0) return "";

        var lines = new List<string>
        {
            "",
            "## 相关记忆（跨会话）",
            "以下是过往会话中记录的与当前任务相关的上下文：",
            "",
        };
        foreach (var item in list)
        {
            var brief = item.Description;
            if (brief.Length > 200) brief = ContextManager.TruncateByRunes(brief, 200) + "...";
            lines.Add($"- **{item.Name}** ({item.Type}): {brief}");
        }
        lines.Add("");
        return string.Join("\n", lines);
    }

    // ── 内部 ──

    private static List<string> GetMemoryDirs(string? rootDir)
    {
        var baseDir = rootDir ?? Environment.CurrentDirectory;
        return
        [
            // WayCoder 自有路径
            Path.Combine(baseDir, ".waycoder", "memory"),
            // Claude Code 兼容路径
            Path.Combine(baseDir, ".claude", "memory"),
            // 用户目录
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".waycoder", "memory"),
        ];
    }

    private static MemoryItem? ParseMemoryFile(string path)
    {
        var content = File.ReadAllText(path);
        var name = Path.GetFileNameWithoutExtension(path);
        var description = "";
        var type = "reference";
        var body = content;

        // 解析 YAML frontmatter
        var fmMatch = Regex.Match(content,
            @"^---\s*\n(.*?)\n---\s*\n(.*)", RegexOptions.Singleline);
        if (fmMatch.Success)
        {
            var fm = fmMatch.Groups[1].Value;
            body = fmMatch.Groups[2].Value.Trim();

            // 提取字段
            name = ExtractYamlField(fm, "name") ?? name;
            description = ExtractYamlField(fm, "description") ?? "";
            type = ExtractYamlField(fm, "type") ?? type;
        }

        return new MemoryItem(
            Name: name,
            Description: description,
            Content: body.Length > 500 ? ContextManager.TruncateByRunes(body, 500) + "..." : body,
            Type: type,
            UpdatedAt: File.GetLastWriteTimeUtc(path)
        );
    }

    private static string? ExtractYamlField(string yaml, string key)
    {
        var match = Regex.Match(yaml, $@"^{key}\s*:\s*(.+)$", RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    /// <summary>从任务文本中提取关键词（名词/动词/英文标识符）</summary>
    private static HashSet<string> ExtractKeywords(string text)
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 英文标识符（camelCase/PascalCase/snake_case）
        foreach (Match m in Regex.Matches(text, @"\b[a-zA-Z][a-zA-Z0-9_]{2,}\b"))
            keywords.Add(m.Value.ToLowerInvariant());

        // CJK 双字词（简单切分）
        var cjkOnly = Regex.Replace(text, @"[^一-鿿]", " ");
        foreach (var part in cjkOnly.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            for (int i = 0; i <= part.Length - 2; i++)
                keywords.Add(part.Substring(i, 2));
        }

        // 限制数量
        if (keywords.Count > 20)
            return keywords.Take(20).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return keywords;
    }
}
