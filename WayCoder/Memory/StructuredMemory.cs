namespace WayCoder;

/// <summary>
/// 结构化记忆系统 —— 对标 Claude Code 的 frontmatter 记忆设计。
///
/// 每个记忆一个 .md 文件，存储在 .waycoder/memory/ 下。
/// MEMORY.md 作为索引（一行一个文件指针）。
///
/// Frontmatter 格式：
/// ---
/// name: &lt;kebab-case-slug&gt;
/// description: &lt;一行摘要&gt;
/// metadata:
///   type: user | feedback | project | reference
/// ---
/// 正文内容，支持 [[wiki-link]] 交叉引用
///
/// v0.19.0: 新增，替代旧的单文件 memory.md。
/// </summary>
public static class StructuredMemory
{
    /// <summary>
    /// 当前活跃的槽位索引（0-9），由 Program.cs 在切换槽位时设置。
    /// 用 AsyncLocal 实现：每个槽位后台任务在启动时捕获自己的槽位值，
    /// 主线程切槽位不再污染正在运行的其他槽位任务（否则 A 槽后台任务会把记忆写进 B 槽目录）。
    /// 主线程上下文的赋值（SwitchAgentSlot）语义不变——仅影响主线程及其 async 链。
    /// </summary>
    private static readonly System.Threading.AsyncLocal<int> _currentSlot = new();
    public static int CurrentSlotIndex
    {
        get => _currentSlot.Value;
        set => _currentSlot.Value = value;
    }

    /// <summary>共享记忆目录（所有槽位可见）</summary>
    public static string SharedMemoryDir
    {
        get
        {
            var dir = Global.WriteConfigPath(Directory.GetCurrentDirectory(), "memory");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>当前槽位的独立记忆目录（仅本槽位可见）</summary>
    public static string SlotMemoryDir
    {
        get
        {
            var dir = Global.WriteConfigPath(Directory.GetCurrentDirectory(), $"memory{Path.DirectorySeparatorChar}slot_{CurrentSlotIndex}");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>记忆目录路径（当前槽位独立目录，向后兼容）</summary>
    public static string MemoryDir => SlotMemoryDir;

    /// <summary>索引文件路径（基于槽位目录）</summary>
    public static string IndexPath => Path.Combine(SlotMemoryDir, "..", "MEMORY.md");

    /// <summary>旧格式记忆文件路径（兼容迁移用）</summary>
    public static string OldMemoryPath => Path.Combine(Directory.GetCurrentDirectory(), Global.LegacyConfigDirName, "memory.md");

    /// <summary>记忆条目</summary>
    public class MemoryEntry
    {
        public string Name { get; set; } = "";           // kebab-case slug
        public string Description { get; set; } = "";     // 一行摘要
        public string Type { get; set; } = "reference";   // user | feedback | project | reference
        public string Content { get; set; } = "";         // 正文（不含 frontmatter）
        public Dictionary<string, string> Metadata { get; set; } = new();
        public string FilePath { get; set; } = "";        // 磁盘路径
        public DateTime CreatedAt { get; set; } = DateTime.MinValue;
        public DateTime UpdatedAt { get; set; } = DateTime.MinValue;

        /// <summary>是否为团队共享记忆（git 同步）</summary>
        public bool IsShared { get; set; }

        /// <summary>提取交叉引用（[[name]] 格式）</summary>
        public List<string> GetLinks()
        {
            var links = new List<string>();
            var span = Content.AsSpan();
            for (int i = 0; i < span.Length - 3; i++)
            {
                if (span[i] == '[' && span[i + 1] == '[')
                {
                    var end = span[(i + 2)..].IndexOf("]]");
                    if (end >= 0 && end < 100)
                    {
                        var link = span.Slice(i + 2, end).ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(link) && !link.Contains(' '))
                            links.Add(link);
                    }
                }
            }
            return links.Distinct().ToList();
        }
    }

    // ---- 读写操作 ----

    /// <summary>列出所有记忆条目（共享 + 当前槽位独立）</summary>
    public static List<MemoryEntry> ListAll()
    {
        var entries = new List<MemoryEntry>();
        var seen = new HashSet<string>();

        // 1. 共享记忆（.waycoder/memory/）
        LoadFromDir(SharedMemoryDir, entries, seen);

        // 2. 槽位独立记忆（.waycoder/memory/slot_N/）
        LoadFromDir(SlotMemoryDir, entries, seen);

        entries.Sort((a, b) => b.UpdatedAt.CompareTo(a.UpdatedAt));
        return entries;
    }

    private static void LoadFromDir(string dir, List<MemoryEntry> entries, HashSet<string> seen)
    {
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.GetFiles(dir, "*.md"))
        {
            // 跳过索引文件本身（MEMORY.md）
            if (Path.GetFileName(file).Equals("MEMORY.md", StringComparison.OrdinalIgnoreCase))
                continue;
            var entry = ReadFile(file);
            if (entry != null && seen.Add(entry.Name))
                entries.Add(entry);
        }
    }

    /// <summary>按名称查找一条记忆（槽位独立目录优先，共享目录回退，与 ListAll 双目录行为一致）</summary>
    public static MemoryEntry? Get(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        // 槽位独立目录优先（个人记忆可覆盖同名共享记忆）
        var path = NameToPathIn(SlotMemoryDir, name);
        if (File.Exists(path)) return ReadFile(path);

        // 共享目录回退（共享记忆直接存于 SharedMemoryDir 根，非 slot_N 子目录）
        var shared = NameToPathIn(SharedMemoryDir, name);
        return File.Exists(shared) ? ReadFile(shared) : null;
    }

    /// <summary>创建一条新记忆</summary>
    public static MemoryEntry Create(string name, string description, string type, string content)
    {
        var now = DateTime.Now;
        var entry = new MemoryEntry
        {
            Name = SanitizeName(name),
            Description = description,
            Type = NormalizeType(type),
            Content = content.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        var path = NameToPath(entry.Name);
        entry.FilePath = path;
        WriteFile(entry);
        RebuildIndex();
        return entry;
    }

    /// <summary>更新一条记忆</summary>
    public static MemoryEntry? Update(string name, string? description = null, string? type = null, string? content = null)
    {
        var existing = Get(name);
        if (existing == null) return null;

        if (description != null) existing.Description = description;
        if (type != null) existing.Type = NormalizeType(type);
        if (content != null) existing.Content = content.Trim();
        existing.UpdatedAt = DateTime.Now;

        WriteFile(existing);
        RebuildIndex();
        return existing;
    }

    /// <summary>删除一条记忆</summary>
    public static bool Delete(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        // 与 Get 一致的双目录查找：槽位目录优先，共享目录回退，确保共享记忆也能删除
        var path = NameToPathIn(SlotMemoryDir, name);
        if (!File.Exists(path))
        {
            var shared = NameToPathIn(SharedMemoryDir, name);
            if (!File.Exists(shared)) return false;
            path = shared;
        }
        File.Delete(path);
        RebuildIndex();
        return true;
    }

    /// <summary>设置记忆的共享状态（团队共享）</summary>
    public static void SetShared(string name, bool shared)
    {
        var entry = Get(name);
        if (entry == null) return;
        entry.IsShared = shared;
        WriteFile(entry);
        RebuildIndex();
    }

    /// <summary>列出所有共享记忆</summary>
    public static List<MemoryEntry> ListShared()
    {
        return ListAll().Where(e => e.IsShared).ToList();
    }

    /// <summary>
    /// 搜索记忆（TF-IDF 语义搜索 + 子串匹配兜底）。
    /// 优先使用 CJK bigram + TF-IDF 评分，无结果时回退到子串匹配。
    /// </summary>
    public static List<MemoryEntry> Search(string query)
    {
        var all = ListAll();
        if (string.IsNullOrWhiteSpace(query)) return all;
        if (all.Count == 0) return [];

        // 优先 TF-IDF 语义搜索
        var scored = SemanticMemory.SearchEntries(all, query, topN: 50);
        if (scored.Count > 0)
            return scored.Select(x => x.Entry).ToList();

        // 兜底：原始子串匹配（TF-IDF 无结果时，如纯符号查询）
        var q = query.ToLowerInvariant();
        return all.Where(e =>
            e.Name.ToLowerInvariant().Contains(q) ||
            e.Description.ToLowerInvariant().Contains(q) ||
            e.Content.ToLowerInvariant().Contains(q)
        ).ToList();
    }

    /// <summary>
    /// 获取与查询相关的记忆上下文（用于系统提示词注入）。
    /// 使用 TF-IDF 语义评分（CJK bigram + 英文分词 + 时间新鲜度加权）。
    /// </summary>
    public static string GetRelevantContext(string query, int topN = 5, int maxChars = 2000)
    {
        var all = ListAll();
        if (all.Count == 0) return "";

        // TF-IDF 语义评分
        var scored = SemanticMemory.SearchEntries(all, query, topN);
        if (scored.Count == 0) return "";

        var sb = new System.Text.StringBuilder();
        var totalChars = 0;
        foreach (var (entry, score) in scored)
        {
            var snippet = $"- **{entry.Description}** (相关度: {score:F2})";
            if (totalChars + snippet.Length > maxChars) break;
            sb.AppendLine(snippet);
            totalChars += snippet.Length;

            // 展开交叉引用
            var links = entry.GetLinks();
            if (links.Count > 0)
            {
                sb.Append("  链接: ");
                foreach (var link in links.Take(5))
                {
                    var linked = Get(link);
                    if (linked != null)
                    {
                        sb.Append($"[[{link}]]: {linked.Description}; ");
                    }
                    else
                    {
                        sb.Append($"[[{link}]]; ");
                    }
                }
                sb.AppendLine();
            }

            // 加入正文摘要
            var contentPreview = entry.Content.Length > 200
                ? ContextManager.TruncateByRunes(entry.Content, 200) + "..."
                : entry.Content;
            if (totalChars + contentPreview.Length + 10 <= maxChars)
            {
                sb.AppendLine($"  {contentPreview}");
                totalChars += contentPreview.Length + 10;
            }
        }

        return sb.ToString();
    }

    /// <summary>记忆总数（共享 + 当前槽位独立，排除 MEMORY.md 索引文件）</summary>
    public static int Count
    {
        get
        {
            var seen = new HashSet<string>();
            int c = 0;
            if (Directory.Exists(SharedMemoryDir))
            {
                foreach (var f in Directory.GetFiles(SharedMemoryDir, "*.md"))
                {
                    if (Path.GetFileName(f).Equals("MEMORY.md", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (seen.Add(Path.GetFileNameWithoutExtension(f))) c++;
                }
            }
            if (Directory.Exists(SlotMemoryDir))
            {
                foreach (var f in Directory.GetFiles(SlotMemoryDir, "*.md"))
                {
                    if (Path.GetFileName(f).Equals("MEMORY.md", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (seen.Add(Path.GetFileNameWithoutExtension(f))) c++;
                }
            }
            return c;
        }
    }

    // ---- 迁移 ----

    /// <summary>
    /// 从旧格式 memory.md 迁移到结构化格式。
    /// 迁移后旧文件保留为备份（.memory.md.bak）。
    /// </summary>
    public static int MigrateFromOldFormat()
    {
        if (!File.Exists(OldMemoryPath)) return 0;
        if (Count > 0) return 0; // 已有结构化记忆，不迁移

        var content = File.ReadAllText(OldMemoryPath);
        if (string.IsNullOrWhiteSpace(content)) return 0;

        var docs = SemanticMemory.ParseDocuments(content);
        if (docs.Count == 0) return 0;

        var count = 0;
        foreach (var doc in docs)
        {
            var name = SanitizeName(doc.Title.Length > 0 ? doc.Title : $"memory-{doc.Index}");
            if (string.IsNullOrWhiteSpace(name) || name.Length < 2) continue;

            // 推断类型
            var type = "reference";
            var descLower = doc.Content.ToLowerInvariant();
            if (descLower.Contains("偏好") || descLower.Contains("喜欢") || descLower.Contains("习惯"))
                type = "user";
            else if (descLower.Contains("修复") || descLower.Contains("改进") || descLower.Contains("建议"))
                type = "feedback";
            else if (descLower.Contains("架构") || descLower.Contains("项目") || descLower.Contains("规则"))
                type = "project";

            Create(name, doc.Title, type, doc.Content);
            count++;
        }

        // 备份旧文件
        try { File.Move(OldMemoryPath, OldMemoryPath + ".bak"); } catch { }

        return count;
    }

    // ---- 内部方法 ----

    /// <summary>文件名 → 完整路径（槽位独立目录）</summary>
    private static string NameToPath(string name) => NameToPathIn(MemoryDir, name);

    /// <summary>文件名 → 指定目录下的完整路径</summary>
    private static string NameToPathIn(string dir, string name)
    {
        var safe = SanitizeName(name);
        if (string.IsNullOrWhiteSpace(safe)) safe = "untitled";
        return Path.Combine(dir, $"{safe}.md");
    }

    /// <summary>清理名称为 kebab-case</summary>
    private static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        // 替换空格和特殊字符为连字符
        var result = new System.Text.StringBuilder();
        foreach (var ch in name.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_')
                result.Append(ch);
            else if (ch == ' ' || ch == '/' || ch == '\\' || ch == '.' || ch == ',')
                result.Append('-');
        }
        var s = result.ToString().Trim('-');
        // 压缩连续的连字符
        while (s.Contains("--")) s = s.Replace("--", "-");
        return s.Length > 64 ? ContextManager.TruncateByRunes(s, 64).TrimEnd('-') : s;
    }

    /// <summary>规范化类型</summary>
    private static string NormalizeType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "user" or "偏好" or "用户" => "user",
            "feedback" or "反馈" or "改进" => "feedback",
            "project" or "项目" or "规则" => "project",
            _ => "reference",
        };
    }

    /// <summary>从磁盘文件读取记忆条目</summary>
    private static MemoryEntry? ReadFile(string path)
    {
        if (!File.Exists(path)) return null;

        try
        {
            var text = File.ReadAllText(path);
            var (fm, body) = ParseFrontmatter(text);
            var fi = new FileInfo(path);

            return new MemoryEntry
            {
                Name = fm.GetValueOrDefault("name") ?? Path.GetFileNameWithoutExtension(path),
                Description = fm.GetValueOrDefault("description") ?? "",
                Type = NormalizeType(fm.GetValueOrDefault("type") ?? "reference"),
                Content = body.Trim(),
                Metadata = fm.Where(kv => kv.Key != "name" && kv.Key != "description" && kv.Key != "type" && kv.Key != "shared")
                    .ToDictionary(kv => kv.Key, kv => kv.Value),
                FilePath = path,
                CreatedAt = fi.CreationTime,
                UpdatedAt = fi.LastWriteTime,
                IsShared = fm.TryGetValue("shared", out var sv) && sv == "true",
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>写入记忆条目到磁盘</summary>
    private static void WriteFile(MemoryEntry entry)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"name: {entry.Name}");
        sb.AppendLine($"description: {entry.Description.ReplaceLineEndings(" ")}");
        sb.AppendLine($"type: {entry.Type}");
        sb.AppendLine($"shared: {entry.IsShared.ToString().ToLowerInvariant()}");
        sb.AppendLine($"created: {entry.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"updated: {entry.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
        foreach (var kv in entry.Metadata)
            sb.AppendLine($"{kv.Key}: {kv.Value}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(entry.Content);

        var path = entry.FilePath;
        if (string.IsNullOrWhiteSpace(path))
            path = NameToPath(entry.Name);

        File.WriteAllText(path, sb.ToString());
        entry.FilePath = path;
    }

    /// <summary>解析 YAML-like frontmatter</summary>
    private static (Dictionary<string, string> Frontmatter, string Body) ParseFrontmatter(string text)
    {
        var fm = new Dictionary<string, string>();
        var body = text;

        if (!text.TrimStart().StartsWith("---")) return (fm, body);

        var lines = text.Split('\n');
        var delimCount = 0;
        var fmLines = new List<string>();
        var bodyStart = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim() == "---")
            {
                delimCount++;
                if (delimCount == 2) { bodyStart = i + 1; break; }
                continue;
            }
            if (delimCount == 1)
                fmLines.Add(lines[i]);
        }

        foreach (var line in fmLines)
        {
            var colon = line.IndexOf(':');
            if (colon > 0)
            {
                var key = line[..colon].Trim();
                var value = line[(colon + 1)..].Trim();
                fm[key] = value;
            }
        }

        if (bodyStart > 0 && bodyStart < lines.Length)
            body = string.Join('\n', lines.Skip(bodyStart)).Trim();

        return (fm, body);
    }

    /// <summary>重建 MEMORY.md 索引</summary>
    public static void RebuildIndex()
    {
        var entries = ListAll();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# 项目记忆索引");
        sb.AppendLine();
        sb.AppendLine($"共 {entries.Count} 条记忆");
        sb.AppendLine();

        var indexDir = Path.GetDirectoryName(IndexPath) ?? ".";
        foreach (var entry in entries)
        {
            // 相对 MEMORY.md 的链接：共享记忆在 memory/ 根、槽位记忆在 slot_N/ 子目录，硬编码 memory/{name}.md 会导致断链
            var rel = string.IsNullOrEmpty(entry.FilePath)
                ? $"{entry.Name}.md"
                : Path.GetRelativePath(indexDir, entry.FilePath);
            sb.AppendLine($"- [{entry.Description}]({rel}) — `{entry.Type}`");
        }

        try
        {
            File.WriteAllText(IndexPath, sb.ToString());
        }
        catch { }
    }
}
