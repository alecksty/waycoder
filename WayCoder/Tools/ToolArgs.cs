namespace WayCoder.Tools;

/// <summary>
/// 工具参数取数助手。
/// JSON 整数经 <c>LLM.ParseJsonNumber</c> 统一解析为 <c>long</c>，小数解析为 <c>double</c>；
/// 工具若只判 <c>is int</c> 会因 <c>long</c> 不匹配而静默丢参、回退默认值（如 timeout/max/depth 全部失效）。
/// 此处统一兼容 int/long/double/string 四种来源。
/// </summary>
internal static class ToolArgs
{
    /// <summary>取整数参数：兼容 int/long/double/string，无法解析或缺失时返回 fallback。</summary>
    public static int GetInt(Dictionary<string, object?> args, string key, int fallback = 0)
    {
        if (!args.TryGetValue(key, out var v) || v == null) return fallback;
        return v switch
        {
            int i => i,
            // 超 int 范围的 long 截断会静默变负/变小（如 limit=3000000000 → 负值），钳制而非截断
            long l => (int)Math.Clamp(l, int.MinValue, int.MaxValue),
            // NaN/Infinity 转 int 未定义（常为 int.MinValue），非有限值回退；小数截断保留原语义
            double d => double.IsFinite(d) ? (int)Math.Clamp(d, (double)int.MinValue, (double)int.MaxValue) : fallback,
            string s => int.TryParse(s, out var p) ? p : fallback,
            _ => fallback,
        };
    }
}
