namespace WayCoder;

using System.Text;

/// <summary>
/// 项目知识库 RAG —— 摄入项目文档（README / docs/*.md / AGENT.md / CLAUDE.md 等），
/// 按标题分块后复用 <see cref="SemanticMemory"/> 的 TF-IDF 检索，把与当前任务最相关的片段
/// 注入系统提示词，让智能体「带着项目上下文」工作。默认轻量（纯 TF-IDF，零网络/零向量依赖）。
/// </summary>
public static class ProjectKnowledge
{
    /// <summary>要摄入的根目录文档名。</summary>
    private static readonly string[] RootDocs =
        ["README.md", "README", "AGENT.md", "CLAUDE.md", "AGENTS.md", "ARCHITECTURE.md"];

    private static List<SemanticMemory.MemoryDocument> _docs = [];
    private static string _cacheFingerprint = "";

    /// <summary>已摄入的文档块数。</summary>
    public static int ChunkCount => _docs.Count;

    /// <summary>
    /// 摄入项目文档（幂等：文档 mtime 指纹未变则复用缓存，避免每轮重读磁盘）。
    /// 同时合并 <see cref="CodeKnowledge"/> 摄入的源码符号块，让检索覆盖「文档 + 代码」。
    /// </summary>
    public static int Ingest(string? cwd = null)
    {
        cwd ??= Directory.GetCurrentDirectory();
        var codeDocs = CodeKnowledge.Ingest(cwd); // 自带缓存：指纹未变复用已提取符号
        var fp = BuildFingerprint(cwd) + "|code:" + CodeKnowledge.Fingerprint;
        if (fp == _cacheFingerprint && _docs.Count > 0) return _docs.Count;

        var docs = new List<SemanticMemory.MemoryDocument>();
        foreach (var name in RootDocs)
        {
            var path = Path.Combine(cwd, name);
            if (File.Exists(path)) AddFile(docs, path, name);
        }

        var docsDir = Path.Combine(cwd, "docs");
        if (Directory.Exists(docsDir))
        {
            foreach (var f in Directory.EnumerateFiles(docsDir, "*.md", SearchOption.AllDirectories).Take(60))
                AddFile(docs, f, "docs/" + Path.GetRelativePath(docsDir, f));
        }

        // 合并代码符号块（代码块数可配：0 关闭，默认按 CodeKnowledge 上限）
        docs.AddRange(codeDocs);

        _docs = docs;
        _cacheFingerprint = fp;
        return _docs.Count;
    }

    /// <summary>检索与任务最相关的知识片段，返回拼接文本（空=无相关知识）。</summary>
    public static string Query(string query, int topN = 4, int maxChars = 1600)
    {
        if (_docs.Count == 0 || string.IsNullOrWhiteSpace(query)) return "";
        var relevant = SemanticMemory.SearchRelevant(_docs, query, topN);
        if (relevant.Count == 0) return "";

        var sb = new StringBuilder();
        foreach (var (doc, _) in relevant)
        {
            var snippet = doc.Content;
            if (snippet.Length > 300)
                snippet = ContextManager.TruncateByRunes(snippet, 300) + "…";
            sb.Append("- ").Append(doc.Title).Append("：").AppendLine(snippet);
        }
        var result = sb.ToString();
        return result.Length > maxChars ? ContextManager.TruncateByRunes(result, maxChars) : result;
    }

    private static void AddFile(List<SemanticMemory.MemoryDocument> docs, string path, string label)
    {
        try { docs.AddRange(ChunkMarkdown(File.ReadAllText(path), label)); }
        catch { /* 不可读文件跳过 */ }
    }

    /// <summary>按标题（#/##/###）分块，超大块按 rune 上限二次切分（中文/emoji 安全）。</summary>
    private static List<SemanticMemory.MemoryDocument> ChunkMarkdown(string text, string label)
    {
        var result = new List<SemanticMemory.MemoryDocument>();
        var normalized = text.Replace("\r\n", "\n");
        var curTitle = label;
        var cur = new StringBuilder();
        int idx = 0;

        void Flush()
        {
            var content = cur.ToString().Trim();
            cur.Clear();
            if (content.Length == 0) return;
            if (content.Length > 1200)
            {
                foreach (var p in SplitByRunes(content, 1200))
                    result.Add(new SemanticMemory.MemoryDocument { Title = curTitle, Content = p, Index = idx++ });
            }
            else
            {
                result.Add(new SemanticMemory.MemoryDocument { Title = curTitle, Content = content, Index = idx++ });
            }
        }

        foreach (var line in normalized.Split('\n'))
        {
            var t = line.Trim();
            if (t.StartsWith("### ") || t.StartsWith("## ") || t.StartsWith("# "))
            {
                Flush();
                curTitle = $"{label} › {t.TrimStart('#').Trim()}";
                continue;
            }
            cur.AppendLine(line);
        }
        Flush();
        return result;
    }

    private static List<string> SplitByRunes(string text, int maxRunes)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        int n = 0;
        foreach (var r in text.EnumerateRunes())
        {
            sb.Append(r.ToString());
            if (++n >= maxRunes) { result.Add(sb.ToString()); sb.Clear(); n = 0; }
        }
        if (sb.Length > 0) result.Add(sb.ToString());
        return result;
    }

    private static string BuildFingerprint(string cwd)
    {
        var sb = new StringBuilder();
        sb.Append(cwd).Append('|');
        foreach (var name in RootDocs)
        {
            var path = Path.Combine(cwd, name);
            if (File.Exists(path))
                sb.Append(name).Append(':').Append(File.GetLastWriteTimeUtc(path).Ticks).Append(';');
        }
        var docsDir = Path.Combine(cwd, "docs");
        if (Directory.Exists(docsDir))
        {
            foreach (var f in Directory.EnumerateFiles(docsDir, "*.md", SearchOption.AllDirectories).Take(60))
                sb.Append(f).Append(':').Append(File.GetLastWriteTimeUtc(f).Ticks).Append(';');
        }
        return sb.ToString();
    }
}
