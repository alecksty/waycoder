namespace CoreCoderSharp;

/// <summary>
/// 记忆系统 —— Agent 可读写的持久化项目知识库。
/// 存储在 .corecoder/memory.md，跨会话保留。
/// Agent 可通过 memory 工具读写。
/// </summary>
public static class MemoryStore
{
    private static string? _memoryPath;

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
    /// 搜索记忆中的关键词。
    /// </summary>
    public static string Search(string query)
    {
        if (!File.Exists(MemoryPath))
            return "（无记忆可搜索）";

        try
        {
            var content = File.ReadAllText(MemoryPath);
            var lines = content.Split('\n');
            var results = new List<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add($"  L{i + 1}: {lines[i].Trim()[..Math.Min(120, lines[i].Trim().Length)]}");
                    if (results.Count >= 20) break;
                }
            }
            return results.Count > 0
                ? $"搜索 \"{query}\" ({results.Count} 处):\n" + string.Join('\n', results)
                : $"未找到 \"{query}\"";
        }
        catch (Exception ex)
        {
            return $"搜索记忆失败: {ex.Message}";
        }
    }
}
