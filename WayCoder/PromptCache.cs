using System.Security.Cryptography;
using System.Text;

namespace WayCoder;

/// <summary>
/// Prompt 缓存追踪 —— 本地 SHA256 检测系统提示词和工具定义是否变更。
///
/// 每次 LLM 请求前记录系统提示词 + 工具定义的哈希值。
/// 连续相同哈希 = 缓存命中，用于 /stats 面板展示节省量。
///
/// 不修改实际发送给 LLM 的内容（LLM 需要完整提示词），
/// 仅在监控层面追踪缓存命中，供费用估算和统计使用。
/// </summary>
public static class PromptCache
{
    /// <summary>是否启用（由 Config.PromptCaching 控制）</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>总请求次数</summary>
    public static int TotalRequests { get; private set; }

    /// <summary>缓存命中次数</summary>
    public static int CacheHits { get; private set; }

    /// <summary>估算节省的 prompt tokens</summary>
    public static long SavedTokens { get; private set; }

    /// <summary>缓存命中率 (0-100)</summary>
    public static double HitRate => TotalRequests == 0 ? 0 : (double)CacheHits / TotalRequests * 100;

    /// <summary>累计节省费用估算 (USD)</summary>
    public static double SavedCostUsd { get; private set; }

    private static string? _lastSystemHash;
    private static string? _lastToolsHash;
    private static int _lastSystemTokens;
    private static int _lastToolsTokens;

    /// <summary>
    /// 记录一次请求，返回是否缓存命中。
    /// 调用时机：LLM 请求构建完成、发送之前。
    /// </summary>
    /// <param name="systemPrompt">系统提示词全文</param>
    /// <param name="toolsJson">工具定义 JSON（序列化后的字符串）</param>
    /// <param name="systemTokens">系统提示词的估算 token 数</param>
    /// <param name="toolsTokens">工具定义的估算 token 数</param>
    /// <returns>true = 本次命中了缓存（节省了 systemTokens + toolsTokens）</returns>
    public static bool RecordRequest(string systemPrompt, string toolsJson,
        int systemTokens, int toolsTokens)
    {
        if (!Enabled) return false;

        TotalRequests++;

        var sysHash = ComputeSha256(systemPrompt);
        var toolsHash = ComputeSha256(toolsJson);

        // 如果系统提示词匹配且工具定义匹配 → 缓存命中
        if (sysHash == _lastSystemHash && toolsHash == _lastToolsHash)
        {
            CacheHits++;
            var saved = _lastSystemTokens + _lastToolsTokens;
            SavedTokens += saved;

            // 估算节省费用（按 $0.27/1M tokens 缓存价 ≈ 50% 折扣）
            // 实际各家定价不同，粗略估算
            SavedCostUsd += saved * 0.27 / 1_000_000 * 0.5;

            return true;
        }

        // 缓存未命中：更新记录
        _lastSystemHash = sysHash;
        _lastToolsHash = toolsHash;
        _lastSystemTokens = systemTokens;
        _lastToolsTokens = toolsTokens;

        return false;
    }

    /// <summary>
    /// 重置缓存状态（切换模型或 /reset 时调用）。
    /// </summary>
    public static void Reset()
    {
        _lastSystemHash = null;
        _lastToolsHash = null;
        _lastSystemTokens = 0;
        _lastToolsTokens = 0;
    }

    /// <summary>
    /// 完全清空统计数据。
    /// </summary>
    public static void ClearStats()
    {
        TotalRequests = 0;
        CacheHits = 0;
        SavedTokens = 0;
        SavedCostUsd = 0;
        Reset();
    }

    /// <summary>用于 /stats 展示的摘要</summary>
    public static string Summary()
    {
        if (!Enabled) return "Prompt 缓存：关闭";

        var sb = new StringBuilder();
        sb.AppendLine($"Prompt 缓存命中率：{HitRate:F0}%（{CacheHits}/{TotalRequests}）");
        sb.AppendLine($"节省 Token：{FormatTokens(SavedTokens)}");
        if (SavedCostUsd > 0)
            sb.AppendLine($"估算节省费用：${SavedCostUsd:F4}");
        return sb.ToString();
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    private static string FormatTokens(long tokens)
    {
        if (tokens >= 1_000_000)
            return $"{tokens / 1_000_000.0:F1}M";
        if (tokens >= 1_000)
            return $"{tokens / 1_000.0:F1}K";
        return tokens.ToString();
    }
}
