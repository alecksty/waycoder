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
            long l => (int)l,
            double d => (int)d,
            string s => int.TryParse(s, out var p) ? p : fallback,
            _ => fallback,
        };
    }
}
