using System.Text;

namespace WayCoder.Infra;

/// <summary>
/// 手搓 XML 库（AOT 安全：零反射，不依赖 System.Xml）。
/// 提供 DOM（XNode）+ 解析器（Xml.Parse）+ 序列化器（Xml.Serialize）。
/// 支持：XML 声明、注释、CDATA、属性（单/双引号）、预定义实体与数字字符引用、
/// 自闭合标签、嵌套元素。类名用 XNode/XKind/Xml 避免与 System.Xml.Linq 冲突。
/// </summary>

/// <summary>XML 节点类型。</summary>
public enum XKind
{
    Element,
    Text,
}

/// <summary>XML 解析错误（携带位置信息）。</summary>
public sealed class XmlParseException : Exception
{
    public int Position { get; }

    public XmlParseException(string message, int position)
        : base($"{message}（位置 {position}）")
    {
        Position = position;
    }
}

/// <summary>XML DOM 节点。</summary>
public sealed class XNode
{
    public XKind Kind { get; }

    /// <summary>元素名（仅 Element）。</summary>
    public string Name { get; }

    /// <summary>属性表（仅 Element，保序）。</summary>
    private readonly List<(string Key, string Value)> _attrs = new();
    private readonly Dictionary<string, int> _attrIndex = new();

    /// <summary>子节点（仅 Element）。</summary>
    private readonly List<XNode> _children = new();

    /// <summary>文本内容（仅 Text）。</summary>
    public string? Text { get; private set; }

    private XNode(XKind kind, string name)
    {
        Kind = kind;
        Name = name;
    }

    // ---------- 工厂 ----------
    public static XNode Element(string name) => new(XKind.Element, name);
    public static XNode TextNode(string text) => new(XKind.Text, "") { Text = text };

    // ---------- 属性 ----------
    public XNode Attr(string key, string value)
    {
        if (Kind != XKind.Element) return this;
        if (_attrIndex.TryGetValue(key, out var idx)) _attrs[idx] = (key, value);
        else { _attrIndex[key] = _attrs.Count; _attrs.Add((key, value)); }
        return this;
    }

    public string? GetAttr(string key)
        => Kind == XKind.Element && _attrIndex.TryGetValue(key, out var idx) ? _attrs[idx].Value : null;

    public bool HasAttr(string key) => Kind == XKind.Element && _attrIndex.ContainsKey(key);

    public IEnumerable<(string Key, string Value)> Attributes => Kind == XKind.Element ? _attrs : [];

    // ---------- 子节点 ----------
    public XNode Add(XNode child)
    {
        if (Kind == XKind.Element) _children.Add(child);
        return this;
    }

    public XNode AddText(string text)
    {
        if (Kind == XKind.Element && !string.IsNullOrEmpty(text)) _children.Add(TextNode(text));
        return this;
    }

    public IEnumerable<XNode> Children => Kind == XKind.Element ? _children : [];

    // ---------- 查询 ----------
    public XNode? Find(string name)
        => Kind == XKind.Element ? _children.FirstOrDefault(c => c.Kind == XKind.Element && c.Name == name) : null;

    public IEnumerable<XNode> FindAll(string name)
        => Kind == XKind.Element ? _children.Where(c => c.Kind == XKind.Element && c.Name == name) : [];

    /// <summary>递归拼接所有文本内容（含子元素）。</summary>
    public string InnerText()
    {
        if (Kind == XKind.Text) return Text ?? "";
        var sb = new StringBuilder();
        foreach (var c in _children) sb.Append(c.InnerText());
        return sb.ToString();
    }

    // ---------- 序列化 ----------
    public string ToXml(bool indent = false) => Xml.Serialize(this, indent);
    public override string ToString() => ToXml();
}

/// <summary>手搓 XML 解析器 + 序列化器。</summary>
public static class Xml
{
    // ============ 解析 ============

    /// <summary>解析 XML 文本为 DOM（返回根元素）。空/全空白返回 null，非法抛 <see cref="XmlParseException"/>。</summary>
    public static XNode? Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var p = new Parser(text);
        p.SkipMisc();
        if (p.AtEnd) return null;
        var root = p.ParseElement();
        p.SkipMisc();
        if (!p.AtEnd) throw new XmlParseException("存在多个根元素", p.Pos);
        return root;
    }

    public static bool TryParse(string text, out XNode? root)
    {
        root = null;
        try { root = Parse(text); return root != null; }
        catch { return false; }
    }

    // ============ 序列化 ============

    public static string Serialize(XNode node, bool indent = false, int depth = 0)
    {
        var sb = new StringBuilder();
        WriteNode(sb, node, indent, depth);
        return sb.ToString();
    }

    private static void WriteNode(StringBuilder sb, XNode node, bool indent, int depth)
    {
        if (node.Kind == XKind.Text)
        {
            sb.Append(EscapeText(node.Text ?? ""));
            return;
        }

        if (indent) Indent(sb, depth);
        sb.Append('<').Append(node.Name);
        foreach (var (k, v) in node.Attributes)
            sb.Append(' ').Append(k).Append("=\"").Append(EscapeAttr(v)).Append('"');

        var children = node.Children.ToList();
        if (children.Count == 0)
        {
            sb.Append("/>");
            if (indent) sb.Append('\n');
            return;
        }

        sb.Append('>');
        // 若只有单个文本子节点，紧凑输出
        if (children.Count == 1 && children[0].Kind == XKind.Text)
        {
            sb.Append(EscapeText(children[0].Text ?? ""));
        }
        else
        {
            if (indent) sb.Append('\n');
            foreach (var c in children)
                WriteNode(sb, c, indent, depth + 1);
            if (indent) Indent(sb, depth);
        }
        sb.Append("</").Append(node.Name).Append('>');
        if (indent) sb.Append('\n');
    }

    private static void Indent(StringBuilder sb, int depth)
    {
        for (int i = 0; i < depth; i++) sb.Append("  ");
    }

    private static string EscapeText(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            switch (c)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    private static string EscapeAttr(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            switch (c)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '"': sb.Append("&quot;"); break;
                case '\'': sb.Append("&apos;"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    // ============ 内部：解析器 ============

    private sealed class Parser
    {
        private readonly string _s;
        private int _i;

        public Parser(string s) => _s = s;
        public int Pos => _i;
        public bool AtEnd => _i >= _s.Length;

        // 跳过空白 + 声明/注释/DOCTYPE/处理指令
        public void SkipMisc()
        {
            while (true)
            {
                SkipWs();
                if (AtEnd) return;
                if (_s[_i] == '<' && _i + 1 < _s.Length && _s[_i + 1] == '!')
                {
                    SkipCommentOrCdataOrDoctype();
                    continue;
                }
                if (_s[_i] == '<' && _i + 1 < _s.Length && _s[_i + 1] == '?')
                {
                    SkipTo("?>");
                    continue;
                }
                return;
            }
        }

        private void SkipWs()
        {
            while (!AtEnd && (_s[_i] == ' ' || _s[_i] == '\t' || _s[_i] == '\n' || _s[_i] == '\r')) _i++;
        }

        private void SkipCommentOrCdataOrDoctype()
        {
            // 已定位到 '<!'
            if (_i + 4 <= _s.Length && _s.AsSpan(_i, 4).SequenceEqual("<!--"))
            {
                SkipTo("-->");
                return;
            }
            if (_i + 9 <= _s.Length && _s.AsSpan(_i, 9).SequenceEqual("<![CDATA["))
            {
                SkipTo("]]>");
                return;
            }
            // <!DOCTYPE ...> 或 <!ENTITY ...>：跳过到 '>'（处理内部子集 [ ... ]）
            _i += 2;
            int depth = 0;
            while (!AtEnd)
            {
                char c = _s[_i];
                if (c == '[') depth++;
                else if (c == ']') depth--;
                else if (c == '>' && depth <= 0) { _i++; return; }
                _i++;
            }
            throw new XmlParseException("声明未闭合", _i);
        }

        private void SkipTo(string terminator)
        {
            int idx = _s.IndexOf(terminator, _i, StringComparison.Ordinal);
            if (idx < 0) throw new XmlParseException($"期望 '{terminator}'", _i);
            _i = idx + terminator.Length;
        }

        private int _depth;
        private const int MaxDepth = 512;

        public XNode ParseElement()
        {
            // 递归下降无深度限制 → 深层嵌套 XML（5 万层 <a><a>..）StackOverflowException 崩溃进程
            if (++_depth > MaxDepth)
                throw new XmlParseException("XML 嵌套过深（>512 层）", _i);
            try
            {
                if (AtEnd || _s[_i] != '<') throw new XmlParseException("期望 '<'", _i);
                _i++; // <
                var name = ReadName();
                var el = XNode.Element(name);

            // 属性
            while (true)
            {
                SkipWs();
                if (AtEnd) throw new XmlParseException("元素未闭合", _i);
                char c = _s[_i];
                if (c == '>') { _i++; break; }
                if (c == '/')
                {
                    _i++;
                    Expect('>');
                    return el; // 自闭合
                }
                var attrName = ReadName();
                SkipWs();
                Expect('=');
                SkipWs();
                var val = ReadAttrValue();
                el.Attr(attrName, val);
            }

            // 内容
            while (true)
            {
                if (AtEnd) throw new XmlParseException("元素未闭合", _i);
                if (_s[_i] == '<')
                {
                    if (_i + 1 < _s.Length && _s[_i + 1] == '/')
                    {
                        // 结束标签
                        _i += 2;
                        var closeName = ReadName();
                        if (closeName != name) throw new XmlParseException($"结束标签不匹配（期望 </{name}>，得到 </{closeName}>）", _i);
                        SkipWs();
                        Expect('>');
                        return el;
                    }
                    if (_i + 1 < _s.Length && _s[_i + 1] == '!')
                    {
                        // 注释或 CDATA
                        if (_i + 9 <= _s.Length && _s.AsSpan(_i, 9).SequenceEqual("<![CDATA["))
                        {
                            _i += 9;
                            int end = _s.IndexOf("]]>", _i, StringComparison.Ordinal);
                            if (end < 0) throw new XmlParseException("CDATA 未闭合", _i);
                            el.AddText(_s[_i..end]);
                            _i = end + 3;
                        }
                        else
                        {
                            SkipCommentOrCdataOrDoctype();
                        }
                        continue;
                    }
                    if (_i + 1 < _s.Length && _s[_i + 1] == '?')
                    {
                        SkipTo("?>");
                        continue;
                    }
                    // 子元素
                    el.Add(ParseElement());
                    continue;
                }
                // 文本
                el.AddText(ReadText());
            }
            }
            finally { _depth--; }
        }

        private string ReadText()
        {
            var sb = new StringBuilder();
            while (!AtEnd && _s[_i] != '<')
            {
                if (_s[_i] == '&')
                {
                    sb.Append(ReadEntity());
                }
                else
                {
                    sb.Append(_s[_i]);
                    _i++;
                }
            }
            return sb.ToString();
        }

        private string ReadEntity()
        {
            int amp = _i;
            int semi = _s.IndexOf(';', _i);
            if (semi < 0) throw new XmlParseException("实体未闭合", amp);
            var body = _s[(_i + 1)..semi];
            _i = semi + 1;
            return body switch
            {
                "lt" => "<",
                "gt" => ">",
                "amp" => "&",
                "quot" => "\"",
                "apos" => "'",
                _ => DecodeCharRef(body, amp),
            };
        }

        private static string DecodeCharRef(string body, int pos)
        {
            if (body.Length > 2 && body[0] == '#')
            {
                int code;
                if (body[1] == 'x' || body[1] == 'X')
                {
                    if (!int.TryParse(body.AsSpan(2), System.Globalization.NumberStyles.HexNumber,
                            System.Globalization.CultureInfo.InvariantCulture, out code))
                        throw new XmlParseException($"非法字符引用 '&{body};'", pos);
                }
                else
                {
                    if (!int.TryParse(body.AsSpan(1), out code))
                        throw new XmlParseException($"非法字符引用 '&{body};'", pos);
                }
                // 孤立低代理/越界码点：ConvertFromUtf32 抛 ArgumentOutOfRangeException（调用方只 catch XmlParseException）
                if ((code >= 0xD800 && code <= 0xDFFF) || code > 0x10FFFF)
                    throw new XmlParseException($"非法 Unicode 码点 '&{body};'（孤立低代理/越界）", pos);
                return char.ConvertFromUtf32(code);
            }
            throw new XmlParseException($"未知实体 '&{body};'", pos);
        }

        private string ReadAttrValue()
        {
            if (AtEnd) throw new XmlParseException("期望属性值", _i);
            char q = _s[_i];
            if (q != '"' && q != '\'') throw new XmlParseException("属性值须用引号", _i);
            _i++;
            var sb = new StringBuilder();
            while (!AtEnd && _s[_i] != q)
            {
                if (_s[_i] == '&') sb.Append(ReadEntity());
                else { sb.Append(_s[_i]); _i++; }
            }
            if (AtEnd) throw new XmlParseException("属性值未闭合", _i);
            _i++; // 闭引号
            return sb.ToString();
        }

        private string ReadName()
        {
            if (AtEnd) throw new XmlParseException("期望名字", _i);
            char c = _s[_i];
            if (!(char.IsLetter(c) || c == '_' || c == ':'))
                throw new XmlParseException("非法的名字起始字符", _i);
            int start = _i;
            _i++;
            while (!AtEnd)
            {
                c = _s[_i];
                if (char.IsLetterOrDigit(c) || c == '_' || c == ':' || c == '-' || c == '.') _i++;
                else break;
            }
            return _s[start.._i];
        }

        private void Expect(char c)
        {
            if (AtEnd || _s[_i] != c) throw new XmlParseException($"期望 '{c}'", _i);
            _i++;
        }
    }
}
