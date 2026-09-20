using System;
using System.Text;

namespace CompilerBase;

/// <summary>词法分析器辅助基类 — 共享字符扫描、位置跟踪、数字/字符串/标识符读取、错误报告</summary>
public class LexerBase
{
    protected readonly string _source;
    protected int _pos;
    protected int _line = 1;
    protected int _col = 1;

    protected LexerBase(string source)
    {
        _source = source;
        RefreshLineMap();
    }

    protected LexerBase(string source, string? fileName) : this(source) { FileName = fileName; }

    /// <summary>当前行号</summary>
    public int Line => _line;
    /// <summary>当前列号</summary>
    public int Column => _col;
    /// <summary>源文件名（用于诊断消息）</summary>
    public string? FileName { get; set; }
    /// <summary>GCC 风格诊断收集器（设置后启用新格式）</summary>
    public DiagnosticBag? Diagnostics { get; set; }

    /// <summary>
    /// 预处理行号映射（`Preprocessor.LineMap` 原样传进来）：第 N 项 = 预处理输出**第 N 行**
    /// 对应的 <c>(原文件, 原行)</c>。**null = 没有预处理/不用映射**。
    ///
    /// <para>
    /// 为什么必须传：`#include` 是把头文件内容**拼进同一个流**的，于是 `_line` 是
    /// **拼接后**的行号。用户文件里第 112 行的注释、可能对应拼接流的第 241 行 ——
    /// 报错照抄拼接行号就等于**指到另一行**上（实测：`#include &lt;stdio.h&gt;` 有 103 行，
    /// 它后面的代码整体后移，于是报错指到了用户文件里一个毫不相干的 `/// &lt;summary&gt;`）。
    /// </para>
    /// </summary>
    public List<(string, int)>? SourceLineMap
    {
        // ⚠ `get` 返回的是**已经解析好的那一份**（显式给的优先，否则是"这份源码对应的那张表"），
        //   不能让调用方拿到 null 再退回拼接行号 —— `MapOriginal` 就在下面转调它。
        get => _resolvedLineMap;
        set { _explicitLineMap = value; RefreshLineMap(); }
    }

    /// <summary>显式设进来的表（C/C++ 走这条）；null = 用"这份源码对应的那张表"。</summary>
    private List<(string, int)>? _explicitLineMap;
    private List<(string, int)>? _resolvedLineMap;

    /// <summary>
    /// 认领/刷新映射表 —— **把"当前生效的映射"登记给解析器与代码生成器**
    /// （它们手里只有 token / AST，拿不到源码字符串，只能读这一份）。
    ///
    /// 词法器是**唯一**知道"这份源码是不是预处理产物"的地方（它就拿在手上），
    /// 所以这个认领动作放在这里、且**每次构造都做**：认不到匹配的表就写 null，
    /// 上一次编译的陈表因此进不来（陈表会把行号映射到**另一份文件**上 —— 那正是要修的 bug）。
    /// </summary>
    private void RefreshLineMap()
    {
        _resolvedLineMap = _explicitLineMap ?? CompilerHelper.LineMapForSource(_source);
        CompilerHelper.SetActiveLineMap(_resolvedLineMap);
    }

    /// <summary>
    /// 「预处理拼接后的行号」→「(原文件, 原行)」——转调
    /// <see cref="CompilerHelper.MapOriginalLine"/>（规则本体在那一处，
    /// 解析器那边也是转调它，别再各写一份）。
    /// </summary>
    public (string? File, int Line) MapOriginal(int processedLine)
        => CompilerHelper.MapOriginalLine(SourceLineMap, processedLine);

    public List<string> Errors { get; } = new();

    protected virtual void ReportError(int line, int col, string message)
    {
        Errors.Add($"行 {line}:{col} — {message}");
    }

    /// <summary>提取指定行的源码文本</summary>
    protected string? GetSourceLine(int line)
    {
        if (_source == null) return null;
        var idx = 0;
        for (int i = 1; i < line && idx < _source.Length; i++)
        {
            var nl = _source.IndexOf('\n', idx);
            if (nl < 0) return null;
            idx = nl + 1;
        }
        var end = _source.IndexOf('\n', idx);
        if (end < 0) end = _source.Length;
        return _source[idx..end].TrimEnd('\r');
    }

    /// <summary>GCC 风格错误报告（如果 Diagnostics 设置则收集，否则抛出）</summary>
    protected void GccError(string message, ErrorCode code = ErrorCode.Unknown)
    {
        // 位置走 `MapOriginal`：报给用户的是**原文件的行**，不是 `#include` 拼接后的行。
        // 源码行文本仍按拼接流取 —— 它就是要报的那一行的正文（映射是双射）。
        var (originFile, originLine) = MapOriginal(_line);
        var sourceLine = GetSourceLine(_line);
        var file = originFile ?? FileName ?? "<input>";
        if (Diagnostics != null)
        {
            Diagnostics.AddError(file, originLine, _col, code, message, sourceLine: sourceLine);
        }
        else
        {
            throw new ParseException(code, $"{file}:{originLine}:{_col}: error: {message}");
        }
    }

    /// <summary>
    /// 统一词法错误报告：抛出 ParseException(携带行/列/错误码)。
    ///
    /// <para>
    /// **没有行号映射时输出与从前逐字相同**（`<see cref="SourceLineMap"/>` 为 null ⇒
    /// `MapOriginal` 原样退回 `_line`）—— 所以改动对不传映射的那些语言是零影响；
    /// 而有映射时行号才换成原文件的行。
    /// </para>
    /// </summary>
    protected void Error(string message) => Error(ErrorCode.Unknown, message);

    /// <summary>
    /// 同上，但允许指定错误码。
    /// </summary>
    protected void Error(ErrorCode code, string message)
    {
        var (originFile, originLine) = MapOriginal(_line);
        // 映射给出了**另一个文件**（错误其实在头文件里）⇒ 只有 GCC 形态写得下文件名；
        // 没有映射/仍是本文件 ⇒ 保持从前的文案，一个字符都不动。
        if (originFile != null)
            throw new ParseException(code, $"{originFile}:{originLine}:{_col}: error: {message}");
        throw new ParseException(code, $"{FileName ?? "词法错误 在第"}{originLine}行{_col}列：{message}");
    }

    // ---- 位置 ----
    protected char Peek(int offset = 0) =>
        _pos + offset < _source.Length ? _source[_pos + offset] : '\0';
    protected bool Match(char expected)
    {
        if (Peek() == expected) { Advance(); return true; }
        return false;
    }
    protected static bool IsChineseChar(char ch) => LexerHelper.IsChineseChar(ch);
    protected char Advance()
    {
        if (_pos >= _source.Length) return '\0';
        char c = _source[_pos++];
        if (c == '\n') { _line++; _col = 1; } else _col++;
        return c;
    }
    protected bool AtEnd => _pos >= _source.Length;

    // ---- 跳过 ----
    protected void SkipWhitespace()
    {
        while (!AtEnd && char.IsWhiteSpace(Peek()))
        {
            if (Peek() == '\n') { _line++; _col = 1; } else _col++;
            _pos++;
        }
    }

    protected void SkipLineComment()
    {
        while (!AtEnd && Peek() != '\n') { _pos++; _col++; }
    }

    protected void SkipBlockComment()
    {
        while (_pos + 1 < _source.Length && !(Peek() == '*' && Peek(1) == '/'))
        {
            if (Peek() == '\n') { _line++; _col = 1; } else _col++;
            _pos++;
        }
        if (_pos + 1 < _source.Length) { _pos += 2; _col += 2; }
        else GccError("未终止的块注释，缺少 '*/' 闭合", ErrorCode.Lexer_UnterminatedComment);
    }

    // ---- 读取 ----
    protected string ReadWhile(Func<char, bool> predicate)
    {
        int start = _pos;
        while (!AtEnd && predicate(Peek())) { Advance(); }
        return _source.Substring(start, _pos - start);
    }

    /// <summary>标识符起始字符判定。子类覆写以添加语言特有字符（C#: @, Ruby: ?!, R: . 等）</summary>
    protected virtual bool IsIdentifierStart(char c) =>
        char.IsLetter(c) || c == '_' || c == '$' || LexerHelper.IsChineseChar(c);

    /// <summary>标识符后续字符判定。子类覆写以添加语言特有字符（BASIC: %$!# 类型后缀等）</summary>
    protected virtual bool IsIdentifierChar(char c) =>
        char.IsLetterOrDigit(c) || c == '_' || c == '$';

    protected string ReadIdentifier()
    {
        return ReadWhile(IsIdentifierChar);
    }

    /// <summary>
    /// 统一关键词查找。替代各 Lexer 中重复的 keywords.TryGetValue(v, out kw) ? kw : IDENTIFIER 模式。
    /// 用法: return LookupKeyword(word, KEYWORDS, TokenType.IDENTIFIER);
    /// </summary>
    protected static TTokenType LookupKeyword<TTokenType>(
        string word, Dictionary<string, TTokenType> keywords, TTokenType defaultType)
    {
        return keywords.TryGetValue(word, out var kw) ? kw : defaultType;
    }

    /// <summary>
    /// 读取数字字面量（十进制、十六进制 0x，以及可通过虚方法启用的八进制/二进制/指数/浮点后缀）。
    /// 子类可覆盖 TryXxx 方法以支持各语言特有数字格式。
    /// </summary>
    protected virtual string ReadNumber()
    {
        int start = _pos;
        if (TryReadHexPrefix())
        {
            ReadWhile(c => LexerHelper.IsHexDigit(c));
        }
        else if (TryReadOctalPrefix())
        {
            ReadWhile(c => LexerHelper.IsOctalDigit(c));
        }
        else if (TryReadBinaryPrefix())
        {
            ReadWhile(c => LexerHelper.IsBinaryDigit(c));
        }
        else
        {
            // 小数部分 + 可选数字分隔符
            ReadWhile(c => char.IsDigit(c) || c == '.');
            TryReadExponent();
            TryReadFloatSuffix();
        }
        return _source.Substring(start, _pos - start);
    }

    /// <summary>读取 0x/0X 十六进制前缀，默认支持。子类可覆盖以支持 &H 等前缀。</summary>
    protected virtual bool TryReadHexPrefix()
    {
        if (Peek() == '0' && (Peek(1) == 'x' || Peek(1) == 'X'))
        {
            Advance(); Advance();
            return true;
        }
        return false;
    }

    /// <summary>读取 0o/0O 八进制前缀，默认不支持。子类覆盖以启用。</summary>
    protected virtual bool TryReadOctalPrefix() => false;

    /// <summary>读取 0b/0B 二进制前缀，默认不支持。子类覆盖以启用。</summary>
    protected virtual bool TryReadBinaryPrefix() => false;

    /// <summary>读取指数部分 (e/E + 可选符号 + 数字)，默认支持。</summary>
    protected virtual void TryReadExponent()
    {
        if (Peek() == 'e' || Peek() == 'E')
        {
            Advance();
            if (Peek() == '+' || Peek() == '-') Advance();
            ReadWhile(char.IsDigit);
        }
    }

    /// <summary>读取浮点后缀 (f/d/l 等)，默认不支持。子类覆盖以启用。</summary>
    protected virtual void TryReadFloatSuffix() { }

    /// <summary>读取字符串字面量。未终止时通过 GCC 诊断管道报告错误。</summary>
    protected string ReadStringLiteral(char quote)
    {
        Advance(); // skip opening quote
        var sb = new StringBuilder();
        while (!AtEnd && Peek() != quote)
        {
            if (Peek() == '\\') { sb.Append(ReadEscape()); }
            else { sb.Append(Advance()); }
        }
        if (!AtEnd) Advance(); // skip closing quote
        else GccError($"未终止的字符串字面量，缺少闭合引号 '{quote}'", ErrorCode.Lexer_UnterminatedString);
        return sb.ToString();
    }

    /// <summary>
    /// 读取转义字符（C99 完整转义集: \n \t \r \0 \a \b \f \v \\ \' \" \? \xNN \NNN）
    /// 子类可重写此方法自定义转义处理
    /// </summary>
    protected virtual char ReadEscape()
    {
        Advance(); // skip backslash
        char esc = Advance();
        switch (esc)
        {
            case 'n': return '\n';
            case 't': return '\t';
            case 'r': return '\r';
            case '0': return '\0';
            case 'a': return '\a';
            case 'b': return '\b';
            case 'f': return '\f';
            case 'v': return '\v';
            case '\\': return '\\';
            case '\'': return '\'';
            case '"': return '"';
            case '?': return '?';
            case 'x':
            {
                // 十六进制转义: \xNN
                int hexStart = _pos;
                while (!AtEnd && char.IsDigit(Peek()) || (char.ToUpper(Peek()) >= 'A' && char.ToUpper(Peek()) <= 'F'))
                    Advance();
                int hexLen = _pos - hexStart;
                if (hexLen == 0) return 'x';
                return (char)Convert.ToInt32(_source.Substring(hexStart, hexLen), 16);
            }
            default:
                if (esc >= '0' && esc <= '7')
                {
                    // 八进制转义: \NNN (最多3位)
                    int octStart = _pos;
                    int octCount = 1;
                    while (octCount < 3 && !AtEnd && Peek() >= '0' && Peek() <= '7')
                    {
                        Advance();
                        octCount++;
                    }
                    int totalOctLen = 1 + (_pos - octStart);
                    char[] octChars = new char[totalOctLen];
                    octChars[0] = esc;
                    if (totalOctLen > 1)
                        _source.CopyTo(octStart, octChars, 1, totalOctLen - 1);
                    return (char)Convert.ToInt32(new string(octChars), 8);
                }
                return esc; // 未知转义序列，保留原字符
        }
    }
}

/// <summary>通用 Token 表示（用于不需要语言特定 TokenType 的场景）</summary>
public class Token
{
    public CommonTokenType Type { get; set; }
    public string Value { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }

    public Token(CommonTokenType type, string value, int line = 0, int col = 0)
    {
        Type = type; Value = value; Line = line; Column = col;
    }

    public override string ToString() => $"Token({Type}, '{Value}', L{Line}:{Column})";
}
