using System.Globalization;
using System.Text;

namespace WayCoder.Infra;

/// <summary>
/// 手搓 JSON 库（AOT 安全：零反射，不依赖 System.Text.Json）。
/// 提供 DOM（JNode）+ 解析器（Json.Parse）+ 序列化器（Json.Serialize）。
/// 类名用 JNode/JKind/Json 前缀，避免与 global using System.Text.Json.Nodes 的
/// JsonNode/JsonObject/JsonArray/JsonValue 冲突。
/// </summary>

/// <summary>JSON 值类型。</summary>
public enum JKind
{
    Object,
    Array,
    String,
    Number,
    Bool,
    Null,
}

/// <summary>JSON 解析错误（携带位置信息）。</summary>
public sealed class JsonParseException : Exception
{
    public int Position { get; }

    public JsonParseException(string message, int position)
        : base($"{message}（位置 {position}）")
    {
        Position = position;
    }
}

/// <summary>
/// JSON DOM 节点。单个可变节点类，用 <see cref="Kind"/> 判别类型；
/// Object/Array 持有子节点，String/Number/Bool/Null 持有标量值。
/// </summary>
public sealed class JNode
{
    public JKind Kind { get; }

    // Object 存储：字典查值 + 有序列表保序
    private Dictionary<string, JNode>? _obj;
    private List<(string Key, JNode Value)>? _order;
    // Array 存储
    private List<JNode>? _arr;
    // 标量
    private string? _str;      // String 值
    internal double _num;      // Number 数值（Json 序列化器读取）
    internal string? _numRaw;  // Number 原始文本（序列化保真）
    private bool _bool;

    private JNode(JKind kind) => Kind = kind;

    // ---------- 工厂 ----------
    public static JNode Object() => new(JKind.Object) { _obj = new(), _order = new() };
    public static JNode Array() => new(JKind.Array) { _arr = new() };
    public static JNode Str(string s) => new(JKind.String) { _str = s ?? "" };
    public static JNode Num(double d) => new(JKind.Number) { _num = d };
    public static JNode Bool(bool b) => new(JKind.Bool) { _bool = b };
    public static JNode Null() => new(JKind.Null);

    internal static JNode NumRaw(double d, string raw) => new(JKind.Number) { _num = d, _numRaw = raw };

    /// <summary>
    /// 按运行时类型分派构造 JNode（替代 JsonValue.Create，AOT 安全）。
    /// null→Null、string→Str、bool→Bool、整数/浮点→Num、JNode→恒等、其余→Str(ToString)。
    /// </summary>
    public static JNode From(object? value) => value switch
    {
        null => Null(),
        JNode n => n,
        string s => Str(s),
        bool b => Bool(b),
        int i => Num(i),
        long l => Num(l),
        float f => Num(f),
        double d => Num(d),
        decimal m => Num((double)m),
        _ => Str(value.ToString() ?? ""),
    };

    // ---------- 大小 ----------
    public int Count => Kind switch
    {
        JKind.Object => _obj!.Count,
        JKind.Array => _arr!.Count,
        _ => 0,
    };

    // ---------- Object 操作 ----------
    public JNode? this[string key]
    {
        get => Kind == JKind.Object && _obj!.TryGetValue(key, out var v) ? v : null;
        set => Set(key, value ?? Null());
    }

    public JNode Set(string key, JNode value)
    {
        if (Kind != JKind.Object) return this;
        if (_obj!.ContainsKey(key))
        {
            _obj[key] = value;
            for (int i = 0; i < _order!.Count; i++)
                if (_order[i].Key == key) { _order[i] = (key, value); break; }
        }
        else
        {
            _obj[key] = value;
            _order!.Add((key, value));
        }
        return this;
    }

    public JNode Set(string key, string? value) => Set(key, value == null ? Null() : Str(value));
    public JNode Set(string key, double value) => Set(key, Num(value));
    public JNode Set(string key, int value) => Set(key, Num(value));
    public JNode Set(string key, bool value) => Set(key, Bool(value));

    public bool Has(string key) => Kind == JKind.Object && _obj!.ContainsKey(key);
    public IEnumerable<string> Keys => Kind == JKind.Object ? _order!.Select(p => p.Key) : [];
    public IEnumerable<(string Key, JNode Value)> Entries => Kind == JKind.Object ? _order! : [];

    // ---------- Array 操作 ----------
    public JNode? this[int index]
        => Kind == JKind.Array && index >= 0 && index < _arr!.Count ? _arr[index] : null;

    public JNode Add(JNode value)
    {
        if (Kind == JKind.Array) _arr!.Add(value);
        return this;
    }

    public JNode Add(string? value) => Add(value == null ? Null() : Str(value));
    public JNode Add(double value) => Add(Num(value));
    public JNode Add(int value) => Add(Num(value));
    public JNode Add(bool value) => Add(Bool(value));

    public IEnumerable<JNode> Items => Kind == JKind.Array ? _arr! : [];
    public JNode? At(int index)
        => Kind == JKind.Array && index >= 0 && index < _arr!.Count ? _arr[index] : null;

    // ---------- 标量取值 ----------
    public string? AsString() => Kind == JKind.String ? _str : null;
    public double AsNumber() => Kind == JKind.Number ? _num : 0;
    public bool AsBool() => Kind == JKind.Bool && _bool;
    public bool IsNull => Kind == JKind.Null;

    // 便捷取值（按 key）
    public string? GetString(string key) => this[key]?.AsString();
    public double GetNumber(string key) => this[key]?.AsNumber() ?? 0;
    public bool GetBool(string key) => this[key]?.AsBool() ?? false;
    public JNode? Get(string key) => this[key];

    /// <summary>深拷贝（AOT 安全：序列化 + 反序列化）。</summary>
    public JNode? Clone() => Json.Parse(ToJson());

    // ---------- 序列化 ----------
    public string ToJson(bool indent = false) => Json.Serialize(this, indent);
    public override string ToString() => ToJson();
}

/// <summary>手搓 JSON 解析器 + 序列化器。</summary>
public static class Json
{
    // ============ 解析 ============

    /// <summary>解析 JSON 文本为 DOM。空/全空白返回 null，非法抛 <see cref="JsonParseException"/>。</summary>
    public static JNode? Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var p = new Parser(text);
        p.SkipWs();
        var node = p.ParseValue();
        p.SkipWs();
        if (!p.AtEnd) throw new JsonParseException("根值后存在多余内容", p.Pos);
        return node;
    }

    public static bool TryParse(string text, out JNode? node)
    {
        node = null;
        try { node = Parse(text); return node != null; }
        catch { return false; }
    }

    // ============ 序列化 ============

    public static string Serialize(JNode node, bool indent = false, int depth = 0)
    {
        var sb = new StringBuilder();
        WriteNode(sb, node, indent, depth);
        return sb.ToString();
    }

    /// <summary>
    /// 序列化任意对象（无反射，AOT 安全），对齐 JsonHelper.SerializeValue。
    /// 支持 null/string/bool/整数/浮点/JNode/IDictionary/IEnumerable。
    /// </summary>
    public static string SerializeValue(object? value)
    {
        switch (value)
        {
            case null: return "null";
            case JNode n: return Serialize(n);
            case string s: return Quote(s);
            case bool b: return b ? "true" : "false";
            case int i: return i.ToString(CultureInfo.InvariantCulture);
            case long l: return l.ToString(CultureInfo.InvariantCulture);
            case double d: return NumToText(d);
            case float f: return NumToText(f);
            case System.Collections.IDictionary dict:
            {
                var sb = new StringBuilder("{");
                var first = true;
                foreach (System.Collections.DictionaryEntry e in dict)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append(Quote(e.Key?.ToString() ?? ""));
                    sb.Append(':');
                    sb.Append(SerializeValue(e.Value));
                }
                sb.Append('}');
                return sb.ToString();
            }
            case System.Collections.IEnumerable seq:
            {
                var sb = new StringBuilder("[");
                var first = true;
                foreach (var item in seq)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append(SerializeValue(item));
                }
                sb.Append(']');
                return sb.ToString();
            }
            default: return Quote(value.ToString() ?? "");
        }
    }

    // ---------- 内部：序列化 ----------

    private static void WriteNode(StringBuilder sb, JNode node, bool indent, int depth)
    {
        switch (node.Kind)
        {
            case JKind.Object:
            {
                if (node.Count == 0) { sb.Append("{}"); return; }
                sb.Append('{');
                var first = true;
                foreach (var (key, val) in node.Entries)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    if (indent) NewLine(sb, depth + 1);
                    sb.Append(Quote(key));
                    sb.Append(indent ? ": " : ":");
                    WriteNode(sb, val, indent, depth + 1);
                }
                if (indent) NewLine(sb, depth);
                sb.Append('}');
                break;
            }
            case JKind.Array:
            {
                if (node.Count == 0) { sb.Append("[]"); return; }
                sb.Append('[');
                var first = true;
                foreach (var item in node.Items)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    if (indent) NewLine(sb, depth + 1);
                    WriteNode(sb, item, indent, depth + 1);
                }
                if (indent) NewLine(sb, depth);
                sb.Append(']');
                break;
            }
            case JKind.String:
                sb.Append(Quote(node.AsString() ?? ""));
                break;
            case JKind.Number:
                sb.Append(node._numRaw ?? NumToText(node._num));
                break;
            case JKind.Bool:
                sb.Append(node.AsBool() ? "true" : "false");
                break;
            case JKind.Null:
                sb.Append("null");
                break;
        }
    }

    private static void NewLine(StringBuilder sb, int depth)
    {
        sb.Append('\n');
        for (int i = 0; i < depth; i++) sb.Append("  ");
    }

    private static string NumToText(double d)
    {
        if (double.IsNaN(d) || double.IsInfinity(d)) return "null";
        return d.ToString("R", CultureInfo.InvariantCulture);
    }

    /// <summary>字符串转义为 JSON 字符串字面量（含双引号）。中文等非 ASCII 保留原样。</summary>
    internal static string Quote(string s)
    {
        var sb = new StringBuilder(s.Length + 2);
        sb.Append('"');
        foreach (var c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                default:
                    if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }

    // ============ 内部：递归下降解析器 ============

    private sealed class Parser
    {
        private readonly string _s;
        private int _i;

        public Parser(string s) => _s = s;
        public int Pos => _i;
        public bool AtEnd => _i >= _s.Length;

        public void SkipWs()
        {
            while (_i < _s.Length)
            {
                char c = _s[_i];
                if (c == ' ' || c == '\t' || c == '\n' || c == '\r') _i++;
                else break;
            }
        }

        private int _depth;
        private const int MaxDepth = 512;

        public JNode ParseValue()
        {
            // 递归下降无深度限制 → 深层嵌套（如 5 万层 [[[..）StackOverflowException 崩溃进程
            if (++_depth > MaxDepth)
                throw new JsonParseException("JSON 嵌套过深（>512 层）", _i);
            try
            {
                SkipWs();
                if (AtEnd) throw new JsonParseException("意外的文件结尾", _i);
                char c = _s[_i];
                return c switch
                {
                    '{' => ParseObject(),
                    '[' => ParseArray(),
                    '"' => JNode.Str(ParseString()),
                    't' => ParseLiteral("true", () => JNode.Bool(true)),
                    'f' => ParseLiteral("false", () => JNode.Bool(false)),
                    'n' => ParseLiteral("null", () => JNode.Null()),
                    _ => ParseNumber(),
                };
            }
            finally { _depth--; }
        }

        private JNode ParseObject()
        {
            _i++; // {
            var obj = JNode.Object();
            SkipWs();
            if (!AtEnd && _s[_i] == '}') { _i++; return obj; }
            while (true)
            {
                SkipWs();
                if (AtEnd) throw new JsonParseException("对象未闭合", _i);
                if (_s[_i] != '"') throw new JsonParseException("期望属性名（字符串）", _i);
                var key = ParseString();
                SkipWs();
                Expect(':');
                SkipWs();
                var val = ParseValue();
                obj.Set(key, val);
                SkipWs();
                if (AtEnd) throw new JsonParseException("对象未闭合", _i);
                char c = _s[_i];
                if (c == ',') { _i++; continue; }
                if (c == '}') { _i++; return obj; }
                throw new JsonParseException("期望 ',' 或 '}'", _i);
            }
        }

        private JNode ParseArray()
        {
            _i++; // [
            var arr = JNode.Array();
            SkipWs();
            if (!AtEnd && _s[_i] == ']') { _i++; return arr; }
            while (true)
            {
                SkipWs();
                arr.Add(ParseValue());
                SkipWs();
                if (AtEnd) throw new JsonParseException("数组未闭合", _i);
                char c = _s[_i];
                if (c == ',') { _i++; continue; }
                if (c == ']') { _i++; return arr; }
                throw new JsonParseException("期望 ',' 或 ']'", _i);
            }
        }

        private string ParseString()
        {
            _i++; // 开引号
            var sb = new StringBuilder();
            while (true)
            {
                if (AtEnd) throw new JsonParseException("字符串未闭合", _i);
                char c = _s[_i];
                if (c == '"') { _i++; return sb.ToString(); }
                if (c == '\\')
                {
                    _i++;
                    if (AtEnd) throw new JsonParseException("非法的转义结尾", _i);
                    char e = _s[_i];
                    switch (e)
                    {
                        case '"': sb.Append('"'); _i++; break;
                        case '\\': sb.Append('\\'); _i++; break;
                        case '/': sb.Append('/'); _i++; break;
                        case 'b': sb.Append('\b'); _i++; break;
                        case 'f': sb.Append('\f'); _i++; break;
                        case 'n': sb.Append('\n'); _i++; break;
                        case 'r': sb.Append('\r'); _i++; break;
                        case 't': sb.Append('\t'); _i++; break;
                        case 'u': _i++; sb.Append(ParseUnicode()); break;
                        default: throw new JsonParseException($"非法的转义字符 '\\{e}'", _i);
                    }
                }
                else
                {
                    sb.Append(c);
                    _i++;
                }
            }
        }

        private string ParseUnicode()
        {
            int code = ReadHex4();
            // 孤立低代理（0xDC00-0xDFFF）或越界码点：ConvertFromUtf32 会抛
            // ArgumentOutOfRangeException（调用方只 catch JsonParseException → 崩溃），这里抛解析异常
            if ((code >= 0xDC00 && code <= 0xDFFF) || code > 0x10FFFF)
                throw new JsonParseException("非法 Unicode 码点（孤立低代理/越界）", _i);
            // 代理对：高代理后必须跟低代理
            if (code >= 0xD800 && code <= 0xDBFF)
            {
                if (_i + 1 < _s.Length && _s[_i] == '\\' && _s[_i + 1] == 'u')
                {
                    _i += 2;
                    int low = ReadHex4();
                    if (low >= 0xDC00 && low <= 0xDFFF)
                        code = 0x10000 + ((code - 0xD800) << 10) + (low - 0xDC00);
                    else
                        throw new JsonParseException("非法代理对", _i);
                }
                else
                {
                    throw new JsonParseException("高代理后缺少低代理", _i);
                }
            }
            return char.ConvertFromUtf32(code);
        }

        private int ReadHex4()
        {
            if (_i + 4 > _s.Length) throw new JsonParseException("\\u 转义不完整", _i);
            int v = 0;
            for (int k = 0; k < 4; k++)
            {
                char c = _s[_i++];
                int d = HexVal(c);
                if (d < 0) throw new JsonParseException("\\u 转义含非法十六进制字符", _i - 1);
                v = (v << 4) | d;
            }
            return v;
        }

        private static int HexVal(char c) => c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'a' and <= 'f' => c - 'a' + 10,
            >= 'A' and <= 'F' => c - 'A' + 10,
            _ => -1,
        };

        private JNode ParseLiteral(string lit, Func<JNode> make)
        {
            if (_i + lit.Length > _s.Length || !_s.AsSpan(_i, lit.Length).SequenceEqual(lit))
                throw new JsonParseException($"非法字面量（期望 {lit}）", _i);
            _i += lit.Length;
            return make();
        }

        private JNode ParseNumber()
        {
            int start = _i;
            if (!AtEnd && _s[_i] == '-') _i++;

            if (AtEnd) throw new JsonParseException("非法的数字", start);
            if (_s[_i] == '0') _i++;
            else if (_s[_i] >= '1' && _s[_i] <= '9')
                while (!AtEnd && _s[_i] >= '0' && _s[_i] <= '9') _i++;
            else throw new JsonParseException("非法的数字", _i);

            if (!AtEnd && _s[_i] == '.')
            {
                _i++;
                if (AtEnd || _s[_i] < '0' || _s[_i] > '9') throw new JsonParseException("小数点后缺数字", _i);
                while (!AtEnd && _s[_i] >= '0' && _s[_i] <= '9') _i++;
            }

            if (!AtEnd && (_s[_i] == 'e' || _s[_i] == 'E'))
            {
                _i++;
                if (!AtEnd && (_s[_i] == '+' || _s[_i] == '-')) _i++;
                if (AtEnd || _s[_i] < '0' || _s[_i] > '9') throw new JsonParseException("指数后缺数字", _i);
                while (!AtEnd && _s[_i] >= '0' && _s[_i] <= '9') _i++;
            }

            var raw = _s[start.._i];
            double num = double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;
            if (double.IsInfinity(num) || double.IsNaN(num)) num = 0;
            return JNode.NumRaw(num, raw);
        }

        private void Expect(char c)
        {
            if (AtEnd || _s[_i] != c) throw new JsonParseException($"期望 '{c}'", _i);
            _i++;
        }
    }
}
