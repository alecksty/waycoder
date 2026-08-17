using System.Globalization;
using System.Text;

namespace WayCoder;

/// <summary>
/// 一条结构化日志条目。不可变，包含时间戳、级别、消息、类别、
/// 标签、异常和附加属性，支持手动序列化为 JSON（AOT 安全，无反射）。
/// </summary>
public sealed class LogEntry
{
    /// <summary>日志产生的时间（本地时区）。</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>日志级别。</summary>
    public LogLevel Level { get; }

    /// <summary>日志消息正文。</summary>
    public string Message { get; }

    /// <summary>日志类别（如模块名、类名），可空。</summary>
    public string? Category { get; }

    /// <summary>关联的标签集合（只读）。</summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>关联的异常，可空。</summary>
    public Exception? Exception { get; }

    /// <summary>附加的键值属性（只读，值须为原始类型）。</summary>
    public IReadOnlyDictionary<string, object?> Properties { get; }

    /// <summary>
    /// 构造一条日志条目。tags 与 properties 会被复制为只读集合。
    /// </summary>
    public LogEntry(
        LogLevel level,
        string message,
        string? category = null,
        Exception? exception = null,
        IEnumerable<string>? tags = null,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        Timestamp = DateTimeOffset.Now;
        Level = level;
        Message = message ?? string.Empty;
        Category = category;
        Exception = exception;
        Tags = tags is null ? Array.Empty<string>()
            : new List<string>(tags).AsReadOnly();
        Properties = properties is null
            ? new Dictionary<string, object?>(StringComparer.Ordinal)
            : new Dictionary<string, object?>(properties, StringComparer.Ordinal);
    }

    /// <summary>
    /// 将本条日志序列化为单行 JSON 对象。手动拼接，不使用反射，
    /// 因此兼容 NativeAOT 裁剪。
    /// </summary>
    public string ToJson()
    {
        var sb = new StringBuilder(256);
        sb.Append('{');
        sb.Append("\"timestamp\":\"").Append(EscapeJson(
            Timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))).Append('"');
        sb.Append(",\"level\":\"").Append(Level.ToString()).Append('"');
        sb.Append(",\"levelValue\":").Append(((int)Level).ToString(CultureInfo.InvariantCulture));
        sb.Append(",\"message\":\"").Append(EscapeJson(Message)).Append('"');
        sb.Append(",\"category\":").Append(Category is null ? "null" : "\"" + EscapeJson(Category) + "\"");

        sb.Append(",\"tags\":[");
        for (var i = 0; i < Tags.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append('"').Append(EscapeJson(Tags[i])).Append('"');
        }
        sb.Append(']');

        if (Exception is not null)
        {
            sb.Append(",\"exception\":{");
            sb.Append("\"type\":\"").Append(EscapeJson(Exception.GetType().FullName)).Append('"');
            sb.Append(",\"message\":\"").Append(EscapeJson(Exception.Message)).Append('"');
            var stack = Exception.StackTrace;
            sb.Append(",\"stackTrace\":")
              .Append(stack is null ? "null" : "\"" + EscapeJson(stack) + "\"");
            sb.Append('}');
        }

        sb.Append(",\"properties\":{");
        var first = true;
        foreach (var kv in Properties)
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append('"').Append(EscapeJson(kv.Key)).Append("\":");
            sb.Append(EncodeValue(kv.Value));
        }
        sb.Append('}');

        sb.Append('}');
        return sb.ToString();
    }

    private static string EncodeValue(object? value)
    {
        switch (value)
        {
            case null:
                return "null";
            case bool b:
                return b ? "true" : "false";
            case string s:
                return "\"" + EscapeJson(s) + "\"";
            case int i:
                return i.ToString(CultureInfo.InvariantCulture);
            case long l:
                return l.ToString(CultureInfo.InvariantCulture);
            case double d:
                return d.ToString("R", CultureInfo.InvariantCulture);
            case float f:
                return f.ToString("R", CultureInfo.InvariantCulture);
            case decimal m:
                return m.ToString(CultureInfo.InvariantCulture);
            case DateTime dt:
                return "\"" + EscapeJson(dt.ToString("O", CultureInfo.InvariantCulture)) + "\"";
            case DateTimeOffset dto:
                return "\"" + EscapeJson(dto.ToString("O", CultureInfo.InvariantCulture)) + "\"";
            case Enum e:
                return "\"" + EscapeJson(e.ToString()) + "\"";
            default:
                return "\"" + EscapeJson(value.ToString()) + "\"";
        }
    }

    /// <summary>
    /// 将字符串转义为可安全放入 JSON 双引号内的形式。null 返回 "null"。
    /// </summary>
    public static string EscapeJson(string? value)
    {
        if (value is null) return "null";
        var sb = new StringBuilder(value.Length + 8);
        foreach (var c in value)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20)
                        sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    /// <summary>人类可读的单行表示：`[INF 12:00:00.000] 消息`。</summary>
    public override string ToString()
        => $"[{Level.Label()} {Timestamp:HH:mm:ss.fff}] {Message}";
}
