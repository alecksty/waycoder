namespace CoreCoderSharp;

/// <summary>
/// 记忆系统（已废弃）—— 旧单文件格式。
/// 请使用 StructuredMemory（v0.19.1+ 多文件 frontmatter 格式）。
/// 仅保留用于自测中的迁移兼容验证。
/// </summary>
[Obsolete("请使用 StructuredMemory（v0.19.1+ 多文件 frontmatter 格式）")]
public static class MemoryStore
{
    private static string? _memoryPath;

    /// <summary>
    /// 仅供自测：重置缓存的记忆路径，使下次访问重新解析 cwd。
    /// </summary>
    public static void ResetCache() => _memoryPath = null;

    /// <summary>
    /// 获取记忆文件路径（自动创建目录）。
    /// </summary>
    public static string MemoryPath
    {
        get
        {
            if (_memoryPath != null) return _memoryPath;
            _memoryPath = Path.Combine(Directory.GetCurrentDirectory(), ".corecoder", "memory.md");
            var dir = Path.GetDirectoryName(_memoryPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return _memoryPath;
        }
    }

    /// <summary>记忆条目数量</summary>
    public static int MemoryCount
    {
        get
        {
            if (!File.Exists(MemoryPath)) return 0;
            var content = File.ReadAllText(MemoryPath);
            var docs = SemanticMemory.ParseDocuments(content);
            return docs.Count;
        }
    }

    /// <summary>
    /// 读取全部记忆内容。
    /// </summary>
    public static string Read()
    {
        if (!File.Exists(MemoryPath))
            return "（暂无记忆。Agent 可通过 memory write 工具记录关键信息。）";

        try
        {
            return File.ReadAllText(MemoryPath);
        }
        catch (Exception ex)
        {
            return $"读取记忆失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 追加一段记忆（自动加时间戳）。
    /// </summary>
    public static string Append(string content)
    {
        try
        {
            var entry = $"\n---\n## {DateTime.Now:yyyy-MM-dd HH:mm}\n\n{content}\n";
            File.AppendAllText(MemoryPath, entry);
            return "✅ 已记录到项目记忆";
        }
        catch (Exception ex)
        {
            return $"写入记忆失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 语义搜索：TF-IDF 相关性排序，返回 Top-20。
    /// 回退：如果 TF-IDF 无结果，回退到关键词子串匹配。
    /// </summary>
    public static string Search(string query)
    {
        if (!File.Exists(MemoryPath))
            return "（无记忆可搜索）";

        try
        {
            var content = File.ReadAllText(MemoryPath);
            var docs = SemanticMemory.ParseDocuments(content);

            if (docs.Count == 0)
                return "（无记忆条目可搜索）";

            // TF-IDF 语义搜索
            var relevant = SemanticMemory.SearchRelevant(docs, query, topN: 20);

            if (relevant.Count > 0)
            {
                var lines = new List<string>();
                foreach (var (doc, score) in relevant)
                {
                    var snippet = doc.Content.Length > 120
                        ? doc.Content[..120] + "..."
                        : doc.Content;
                    var timeStr = doc.Timestamp != DateTime.MinValue
                        ? doc.Timestamp.ToString("MM-dd HH:mm")
                        : "";
                    lines.Add($"  [{score:F2}] {timeStr}  {snippet}");
                }
                return $"搜索 \"{query}\" ({relevant.Count} 条相关记忆):\n" + string.Join('\n', lines);
            }

            // 回退：关键词子串匹配
            var lines2 = content.Split('\n');
            var results = new List<string>();
            for (int i = 0; i < lines2.Length; i++)
            {
                if (lines2[i].Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add($"  L{i + 1}: {lines2[i].Trim()[..Math.Min(120, lines2[i].Trim().Length)]}");
                    if (results.Count >= 20) break;
                }
            }
            return results.Count > 0
                ? $"搜索 \"{query}\" (关键词匹配 {results.Count} 处):\n" + string.Join('\n', results)
                : $"未找到 \"{query}\"";
        }
        catch (Exception ex)
        {
            return $"搜索记忆失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 获取与查询最相关的记忆上下文（用于系统提示词增量注入）。
    /// </summary>
    public static string GetRelevantContext(string query, int topN = 5, int maxChars = 2000)
    {
        if (!File.Exists(MemoryPath)) return "";
        try
        {
            var content = File.ReadAllText(MemoryPath);
            return SemanticMemory.GetRelevantContext(content, query, topN, maxChars);
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// 清空所有记忆。
    /// </summary>
    public static void Clear()
    {
        try
        {
            if (File.Exists(MemoryPath))
                File.WriteAllText(MemoryPath, "");
        }
        catch { }
    }
}
