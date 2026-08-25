namespace WayCoder;

/// <summary>
/// 向量嵌入存储 — 为结构化记忆提供语义向量搜索。
///
/// 功能：
/// 1. .vec 二进制向量文件 I/O（AOT 兼容，纯 BitConverter）
/// 2. 通过 LLM API 的 /v1/embeddings 端点生成向量
/// 3. 余弦相似度计算
/// 4. Hybrid 混合搜索（embedding × 0.7 + TF-IDF × 0.3）
/// 5. 懒加载向量生成（fire-and-forget，不阻塞搜索）
///
/// 设计约束：AOT 兼容（无反射）、零 NuGet 依赖、纯计算。
/// </summary>
public static class EmbeddingStore
{
    /// <summary>LLM 客户端引用（用于调用 /v1/embeddings）。null 时禁用向量生成。</summary>
    public static LLM? LlmClient { get; set; }

    /// <summary>向量嵌入功能开关</summary>
    public static bool Enabled { get; set; }

    /// <summary>嵌入模型名称（如 "text-embedding-3-small"）</summary>
    public static string EmbeddingModel { get; set; } = "text-embedding-3-small";

    /// <summary>正在生成中的记忆名称集合（防止重复生成）</summary>
    private static readonly HashSet<string> _generating = new(StringComparer.Ordinal);
    private static readonly object _genLock = new();

    // ================================================================
    // .vec 文件 I/O
    // ================================================================

    /// <summary>计算 .md 文件对应的 .vec 文件路径</summary>
    public static string VecPath(string mdPath)
    {
        var dir = Path.GetDirectoryName(mdPath) ?? "";
        var name = Path.GetFileNameWithoutExtension(mdPath);
        return Path.Combine(dir, $"{name}.vec");
    }

    /// <summary>检查记忆条目是否已有嵌入向量</summary>
    public static bool HasEmbedding(StructuredMemory.MemoryEntry entry)
        => File.Exists(VecPath(entry.FilePath));

    /// <summary>
    /// 保存浮点数组为二进制 .vec 文件。
    /// 格式: [int32 dims][float32 × dims] 全部小端序。
    /// 使用临时文件 + 原子重命名防止并发写冲突。
    /// </summary>
    public static void SaveEmbedding(string mdPath, float[] embedding)
    {
        var path = VecPath(mdPath);
        var tmpPath = path + ".tmp";

        try
        {
            using var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None);
            // 写入维度
            byte[] dimBytes = BitConverter.GetBytes(embedding.Length);
            fs.Write(dimBytes, 0, 4);
            // 写入浮点值
            var floatBytes = new byte[embedding.Length * 4];
            Buffer.BlockCopy(embedding, 0, floatBytes, 0, floatBytes.Length);
            fs.Write(floatBytes, 0, floatBytes.Length);
            fs.Flush();
        }
        catch
        {
            try { File.Delete(tmpPath); } catch { }
            return;
        }

        // 原子重命名
        try
        {
            File.Move(tmpPath, path, overwrite: true);
        }
        catch
        {
            try { File.Delete(tmpPath); } catch { }
        }
    }

    /// <summary>
    /// 从 .vec 文件加载浮点数组。文件不存在或损坏时返回 null。
    /// </summary>
    public static float[]? LoadEmbedding(string mdPath)
    {
        var path = VecPath(mdPath);
        if (!File.Exists(path)) return null;

        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length < 4) return null;

            var dims = BitConverter.ToInt32(bytes, 0);
            if (dims <= 0 || dims > 4096) return null;
            if (bytes.Length < 4 + dims * 4) return null;

            var result = new float[dims];
            Buffer.BlockCopy(bytes, 4, result, 0, dims * 4);
            return result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>删除记忆条目对应的 .vec 文件</summary>
    public static void DeleteEmbedding(string mdPath)
    {
        var path = VecPath(mdPath);
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    // ================================================================
    // 余弦相似度
    // ================================================================

    /// <summary>
    /// 两个向量的余弦相似度。
    /// 任一为 null 或维度不匹配时返回 0。
    /// </summary>
    public static double CosineSimilarity(float[]? a, float[]? b)
    {
        if (a == null || b == null || a.Length != b.Length || a.Length == 0)
            return 0;

        double dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += (double)a[i] * b[i];
            normA += (double)a[i] * a[i];
            normB += (double)b[i] * b[i];
        }

        if (normA == 0 || normB == 0) return 0;
        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    // ================================================================
    // 嵌入生成
    // ================================================================

    /// <summary>
    /// 通过 LLM API 的 /v1/embeddings 端点生成向量。
    /// 返回 float[] 或 null（失败时）。
    /// </summary>
    public static async Task<float[]?> GenerateEmbeddingAsync(
        string text, CancellationToken ct = default)
    {
        if (LlmClient == null) return null;

        try
        {
            return await LlmClient.GetEmbeddingAsync(text, EmbeddingModel, ct);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 为记忆条目生成并保存向量（fire-and-forget 安全）。
    /// 搜索文本 = Description + " " + Content（截断到 8000 字符以防 API 限制）。
    /// </summary>
    public static async Task GenerateAndSaveAsync(
        StructuredMemory.MemoryEntry entry, CancellationToken ct = default)
    {
        if (LlmClient == null || !Enabled) return;

        // 防重复生成
        lock (_genLock)
        {
            if (!_generating.Add(entry.Name)) return;
        }

        try
        {
            var text = $"{entry.Description} {entry.Content}";
            if (text.Length > 8000) text = ContextManager.TruncateByRunes(text, 8000);

            var vec = await GenerateEmbeddingAsync(text, ct);
            if (vec != null)
                SaveEmbedding(entry.FilePath, vec);
        }
        catch
        {
            // 静默失败
        }
        finally
        {
            lock (_genLock) { _generating.Remove(entry.Name); }
        }
    }

    // ================================================================
    // 混合搜索
    // ================================================================

    /// <summary>
    /// 混合搜索：embedding 余弦相似度 + TF-IDF 语义评分。
    /// 有 .vec 文件的条目：finalScore = 0.7 × embedScore + 0.3 × tfidfScore
    /// 无 .vec 文件的条目：纯 TF-IDF 评分
    /// 返回按分数降序排列的 topN 结果。
    /// </summary>
    public static async Task<List<(StructuredMemory.MemoryEntry Entry, double Score)>> SearchHybrid(
        List<StructuredMemory.MemoryEntry> entries, string query, int topN = 20,
        CancellationToken ct = default)
    {
        if (entries.Count == 0) return [];

        // 1. 计算 TF-IDF 分数
        var tfidfResults = SemanticMemory.SearchEntries(entries, query, topN: entries.Count);
        var tfidfScores = new Dictionary<string, double>(StringComparer.Ordinal);
        double maxTfIdf = 0.001;
        foreach (var (entry, score) in tfidfResults)
        {
            tfidfScores[entry.Name] = score;
            if (score > maxTfIdf) maxTfIdf = score;
        }

        // 2. 尝试获取查询向量
        float[]? queryVec = null;
        if (Enabled && LlmClient != null)
        {
            try
            {
                queryVec = await GenerateEmbeddingAsync(query, ct);
            }
            catch
            {
                // 向量生成失败，回退到纯 TF-IDF
            }
        }

        // 3. 为每个条目计算综合分数并收集需要懒加载的条目
        var scored = new List<(StructuredMemory.MemoryEntry Entry, double Score)>();
        var toGenerate = new List<StructuredMemory.MemoryEntry>();

        foreach (var entry in entries)
        {
            double finalScore;

            // TF-IDF 分量（始终可用）
            double tfidfScore = tfidfScores.GetValueOrDefault(entry.Name, 0) / maxTfIdf;

            // Embedding 分量（有向量 + 查询向量时使用）
            double embedScore = 0;
            bool hasEmbedding = false;

            if (queryVec != null)
            {
                var entryVec = LoadEmbedding(entry.FilePath);
                if (entryVec != null)
                {
                    embedScore = CosineSimilarity(queryVec, entryVec);
                    // 余弦相似度范围 [-1, 1]，归一化到 [0, 1]
                    embedScore = (embedScore + 1) / 2;
                    hasEmbedding = true;
                }
            }

            if (hasEmbedding)
                finalScore = 0.7 * embedScore + 0.3 * tfidfScore;
            else
                finalScore = tfidfScore;

            if (finalScore > 0)
                scored.Add((entry, Math.Round(finalScore, 4)));

            // 记录需要懒加载生成的条目（TF-IDF > 0 但无向量）
            if (Enabled && LlmClient != null && !hasEmbedding && tfidfScore > 0)
                toGenerate.Add(entry);
        }

        // 4. 排序
        scored.Sort((a, b) => b.Score.CompareTo(a.Score));
        var result = scored.Take(topN).ToList();

        // 5. 懒加载向量生成（fire-and-forget，最多 3 个并发）
        if (toGenerate.Count > 0)
        {
            var batch = toGenerate.Take(3).ToList();
            foreach (var entry in batch)
            {
                _ = Task.Run(async () =>
                {
                    try { await GenerateAndSaveAsync(entry, ct); }
                    catch { /* 静默失败 */ }
                }, ct);
            }
        }

        return result;
    }

    // ================================================================
    // 代码块混合检索（CodeKnowledge MemoryDocument 块）
    // ================================================================

    /// <summary>
    /// 代码块混合检索：有缓存向量的块用 0.7×余弦 + 0.3×TF-IDF，无向量块纯 TF-IDF。
    /// 返回与 <see cref="SemanticMemory.SearchRelevant"/> 相同的 (MemoryDocument, double) 元组（调用方零改动）。
    /// embedder 可注入供测试；缺省用 LlmClient。向量失败/未启用 → 纯 TF-IDF（优雅回退）。
    /// </summary>
    public static async Task<List<(SemanticMemory.MemoryDocument Doc, double Score)>> SearchRelevantHybrid(
        List<SemanticMemory.MemoryDocument> docs, string query, int topN = 5,
        Func<string, Task<float[]?>>? embedder = null, CancellationToken ct = default)
    {
        if (docs.Count == 0 || string.IsNullOrWhiteSpace(query)) return [];

        // 1. TF-IDF 分（归一化）
        var tfidfResults = SemanticMemory.SearchRelevant(docs, query, topN: docs.Count);
        var tfidfScores = new Dictionary<int, double>();
        double maxTfIdf = 0.001;
        foreach (var (doc, score) in tfidfResults)
        {
            tfidfScores[doc.Index] = score;
            if (score > maxTfIdf) maxTfIdf = score;
        }

        // 2. 查询向量
        float[]? queryVec = null;
        if (Enabled && (embedder != null || LlmClient != null))
        {
            try
            {
                queryVec = embedder != null ? await embedder(query) : await GenerateEmbeddingAsync(query, ct);
            }
            catch { /* 向量失败 → 纯 TF-IDF 回退 */ }
        }

        // 3. 逐块综合分 + 收集待生成
        var scored = new List<(SemanticMemory.MemoryDocument Doc, double Score)>();
        var toGenerate = new List<(SemanticMemory.MemoryDocument Doc, string Key)>();
        var validKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var doc in docs)
        {
            var key = CodeEmbeddingCache.ChunkKey(doc);
            validKeys.Add(key);

            double tfidfScore = tfidfScores.GetValueOrDefault(doc.Index, 0) / maxTfIdf;
            double embedScore = 0;
            bool hasVec = false;

            if (queryVec != null)
            {
                var vec = CodeEmbeddingCache.GetVector(key);
                if (vec != null)
                {
                    embedScore = (CosineSimilarity(queryVec, vec) + 1) / 2; // [-1,1] → [0,1]
                    hasVec = true;
                }
            }

            var final = hasVec ? 0.7 * embedScore + 0.3 * tfidfScore : tfidfScore;
            if (final > 0)
                scored.Add((doc, Math.Round(final, 4)));

            if (Enabled && (embedder != null || LlmClient != null) && !hasVec && tfidfScore > 0)
                toGenerate.Add((doc, key));
        }

        // 4. 排序 + topN
        scored.Sort((a, b) => b.Score.CompareTo(a.Score));
        var result = scored.Take(topN).ToList();

        // 5. 清理孤儿键（内容变更/文件删除后的旧嵌入）
        try { CodeEmbeddingCache.Prune(validKeys); } catch { }

        // 6. 后台生成缺失向量（有界并发 3，防首次查询卡顿；下轮即命中缓存）
        if (toGenerate.Count > 0)
        {
            foreach (var (doc, key) in toGenerate.Take(3))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var vec = embedder != null
                            ? await embedder(doc.Title + "\n" + doc.Content)
                            : await GenerateEmbeddingAsync(doc.Title + "\n" + doc.Content, ct);
                        if (vec != null) CodeEmbeddingCache.SaveVector(key, vec);
                    }
                    catch { /* 静默失败 */ }
                }, ct);
            }
        }

        return result;
    }
}
