namespace CoreCoderSharp;

/// <summary>
/// 语义记忆引擎 — 纯 C# TF-IDF 实现，零外部依赖。
///
/// 功能：
/// 1. 记忆文档解析（按 --- 分段）
/// 2. CJK bigram + 英文分词
/// 3. TF-IDF 相关性排序
/// 4. Top-N 相关记忆检索
///
/// 设计约束：AOT 兼容（无反射）、零依赖、纯计算。
/// </summary>
public static class SemanticMemory
{
    /// <summary>记忆文档</summary>
    public class MemoryDocument
    {
        public DateTime Timestamp { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        /// <summary>原始索引（在文件中的段落顺序）</summary>
        public int Index { get; set; }
    }

    /// <summary>
    /// 从 raw markdown 解析记忆文档列表。
    /// 格式: \n---\n## 2024-01-01 12:00\n\n正文内容\n
    /// </summary>
    public static List<MemoryDocument> ParseDocuments(string rawMarkdown)
    {
        var docs = new List<MemoryDocument>();
        if (string.IsNullOrWhiteSpace(rawMarkdown)) return docs;

        // 按 --- 分割（独立成行的分割线），先规范化换行符
        var normalized = rawMarkdown.Replace("\r\n", "\n");
        var parts = normalized.Split("\n---\n", StringSplitOptions.RemoveEmptyEntries);
        int idx = 0;

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            // 尝试提取 ## 时间戳标题
            var title = "";
            var content = trimmed;
            DateTime timestamp = DateTime.MinValue;

            var lines = trimmed.Split('\n');
            if (lines.Length > 0 && lines[0].StartsWith("## "))
            {
                title = lines[0]["## ".Length..].Trim();
                // 尝试解析时间戳
                DateTime.TryParse(title, out timestamp);
                // 正文从第2行开始
                content = lines.Length > 1
                    ? string.Join('\n', lines.Skip(1)).Trim()
                    : "";
            }
            else if (lines.Length > 0 && lines[0].StartsWith("# "))
            {
                // 顶级标题作为文档标题
                title = lines[0]["# ".Length..].Trim();
                content = lines.Length > 1
                    ? string.Join('\n', lines.Skip(1)).Trim()
                    : "";
            }

            if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(title))
                continue;

            docs.Add(new MemoryDocument
            {
                Timestamp = timestamp,
                Title = title,
                Content = content,
                Index = idx++
            });
        }

        return docs;
    }

    /// <summary>
    /// 分词：CJK 字符用 bigram（二元组滑动窗口），英文/数字用空格分词。
    /// </summary>
    public static List<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        if (string.IsNullOrEmpty(text)) return tokens;

        var span = text.AsSpan();
        int i = 0;

        while (i < span.Length)
        {
            char c = span[i];

            // 跳过空格和标点
            if (char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsSeparator(c))
            {
                i++;
                continue;
            }

            // CJK 字符 — 收集连续的 CJK 字符做 bigram
            if (IsCJK(c))
            {
                var cjkStart = i;
                while (i < span.Length && IsCJK(span[i])) i++;

                var cjkSpan = span[cjkStart..i];
                // Bigram 滑动窗口
                for (int j = 0; j < cjkSpan.Length - 1; j++)
                {
                    tokens.Add(new string(cjkSpan.Slice(j, 2)));
                }
                // 单个 CJK 字符也作为 token
                if (cjkSpan.Length == 1)
                {
                    tokens.Add(new string(cjkSpan));
                }
                continue;
            }

            // 英文字母/数字 — 收集连续的单词字符
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
            {
                var wordStart = i;
                while (i < span.Length && (char.IsLetterOrDigit(span[i]) || span[i] == '_' || span[i] == '-'))
                    i++;

                var word = span[wordStart..i].ToString().ToLowerInvariant();
                // 过滤太短的词和常见的停用词
                if (word.Length >= 2 && !IsStopWord(word))
                    tokens.Add(word);
                continue;
            }

            i++;
        }

        return tokens;
    }

    /// <summary>
    /// 计算查询与每个文档的 TF-IDF 相关性分数。
    /// 返回按分数降序排列的 (文档, 分数) 列表。
    /// </summary>
    public static List<(MemoryDocument Doc, double Score)> SearchRelevant(
        List<MemoryDocument> docs, string query, int topN = 5)
    {
        if (docs.Count == 0 || string.IsNullOrWhiteSpace(query))
            return [];

        var queryTokens = Tokenize(query);
        if (queryTokens.Count == 0) return [];

        int N = docs.Count;

        // 对每个文档分词
        var docTokens = new List<List<string>>();
        foreach (var doc in docs)
        {
            var tokens = Tokenize(doc.Content);
            // 也加入标题的关键词
            if (!string.IsNullOrWhiteSpace(doc.Title))
                tokens.AddRange(Tokenize(doc.Title));
            docTokens.Add(tokens);
        }

        // 计算 IDF：log(N / 包含该词的文档数)
        var idfCache = new Dictionary<string, double>();
        foreach (var qt in queryTokens.Distinct())
        {
            int docFreq = 0;
            foreach (var dt in docTokens)
            {
                if (dt.Contains(qt)) docFreq++;
            }
            idfCache[qt] = docFreq > 0 ? Math.Log((double)(N + 1) / (docFreq + 1)) + 0.5 : 0;
        }

        // 计算每个文档的 TF-IDF 总分
        var scored = new List<(MemoryDocument Doc, double Score)>();
        for (int i = 0; i < docs.Count; i++)
        {
            double totalScore = 0;
            var dt = docTokens[i];
            int totalTerms = dt.Count;
            if (totalTerms == 0) continue;

            foreach (var qt in queryTokens.Distinct())
            {
                int termFreq = dt.Count(t => t == qt);
                if (termFreq == 0) continue;

                double tf = (double)termFreq / totalTerms;
                double idf = idfCache.GetValueOrDefault(qt, 0);
                totalScore += tf * idf;
            }

            // 根据结果是否包含查询原文加分（精确匹配奖励）
            if (docs[i].Content.Contains(query, StringComparison.OrdinalIgnoreCase))
                totalScore += 0.2;

            // 新近记忆微幅加分
            if (docs[i].Timestamp != DateTime.MinValue)
            {
                var age = DateTime.Now - docs[i].Timestamp;
                if (age.TotalDays < 7) totalScore += 0.1;
                if (age.TotalDays < 1) totalScore += 0.1;
            }

            if (totalScore > 0)
                scored.Add((docs[i], Math.Round(totalScore, 4)));
        }

        scored.Sort((a, b) => b.Score.CompareTo(a.Score));
        return scored.Take(topN).ToList();
    }

    /// <summary>
    /// 获取与查询最相关的记忆摘要文本（用于注入系统提示词）。
    /// </summary>
    public static string GetRelevantContext(string rawMemory, string query,
        int topN = 5, int maxChars = 2000)
    {
        var docs = ParseDocuments(rawMemory);
        var relevant = SearchRelevant(docs, query, topN);

        if (relevant.Count == 0)
            return "";

        var sb = new System.Text.StringBuilder();
        foreach (var (doc, score) in relevant)
        {
            var snippet = doc.Content;
            if (snippet.Length > 300)
                snippet = snippet[..300] + "...";

            var timeStr = doc.Timestamp != DateTime.MinValue
                ? doc.Timestamp.ToString("MM-dd HH:mm")
                : "";

            sb.AppendLine($"### {timeStr} (相关度: {score:F2})");
            sb.AppendLine(snippet);
            sb.AppendLine();

            if (sb.Length >= maxChars) break;
        }

        return sb.ToString().TrimEnd();
    }

    // ================================================================
    // 内部工具方法
    // ================================================================

    /// <summary>判断字符是否为 CJK（中日韩统一表意文字）</summary>
    private static bool IsCJK(char c)
    {
        return (c >= 0x4E00 && c <= 0x9FFF)   // CJK Unified Ideographs
            || (c >= 0x3400 && c <= 0x4DBF)    // CJK Extension A
            || (c >= 0x2E80 && c <= 0x2EFF)    // CJK Radicals
            || (c >= 0x3000 && c <= 0x303F)    // CJK Symbols/Punctuation
            || (c >= 0xFF00 && c <= 0xFFEF)    // Halfwidth/Fullwidth
            || (c >= 0xF900 && c <= 0xFAFF)    // CJK Compatibility
            || (c >= 0xFE30 && c <= 0xFE4F)    // CJK Compatibility Forms
            || (c >= 0xAC00 && c <= 0xD7AF)    // Hangul Syllables
            || (c >= 0x3040 && c <= 0x309F)    // Hiragana
            || (c >= 0x30A0 && c <= 0x30FF);   // Katakana
    }

    /// <summary>常见英文停用词表</summary>
    private static readonly HashSet<string> _stopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "is", "am", "are", "was", "were", "be", "been",
        "do", "does", "did", "have", "has", "had",
        "will", "would", "shall", "should", "can", "could", "may", "might",
        "the", "of", "and", "or", "not", "to", "in", "on",
        "at", "by", "for", "with", "about", "from",
    };

    private static bool IsStopWord(string word) => _stopWords.Contains(word);
}
