using System.Text;

namespace WayCoder;

/// <summary>
/// AOT 兼容的 JSON 辅助方法。统一委托给 JsonLib 的 <see cref="Json.SerializeValue"/> / <see cref="Json.Quote"/>，
/// 消除重复实现，并统一 NaN/Inf→null、控制字符转义等语义。
/// </summary>
internal static class JsonHelper
{
    /// <summary>
    /// 将参数字典手动序列化为 JSON 字符串（无反射，AOT 安全）。
    /// </summary>
    public static string SerializeArgs(Dictionary<string, object?> args)
    {
        var sb = new StringBuilder("{");
        var first = true;
        foreach (var (key, value) in args)
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append(Json.Quote(key));
            sb.Append(':');
            sb.Append(Json.SerializeValue(value));
        }
        sb.Append('}');
        return sb.ToString();
    }

    /// <summary>
    /// 序列化任意对象（无反射，AOT 安全）。委托 <see cref="Json.SerializeValue"/>。
    /// </summary>
    public static string SerializeValue(object? value) => Json.SerializeValue(value);
}
