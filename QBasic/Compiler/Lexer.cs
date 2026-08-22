// QBasic/Compiler/Lexer.cs
// 词法分析器：把 BASIC 源码拆分为 token 序列，支持行号、REM 注释、字符串字面量。
namespace QBasic.Compiler;

public enum TokenType
{
    Number,     // 数字常量
    String,     // 字符串字面量
    Identifier, // 标识符 / 关键字
    Operator,   // 运算符
    NewLine,    // 行结束
    Eof,        // 文件结束
}

public readonly struct Token
{
    public TokenType Type { get; init; }
    public string Text { get; init; }
    public double Number { get; init; }
    public int Line { get; init; }

    public override string ToString() => $"{Type}:{Text}";
}

public sealed class Lexer
{
    private readonly string _src;
    private int _pos;
    private int _line = 1;

    public Lexer(string source)
    {
        _src = source;
    }

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();
        while (true)
        {
            var t = Next();
            tokens.Add(t);
            if (t.Type == TokenType.Eof) break;
        }
        return tokens;
    }

    private Token Next()
    {
        SkipWhitespace();

        if (_pos >= _src.Length)
            return new Token { Type = TokenType.Eof, Line = _line };

        char c = _src[_pos];

        // 行号：行首的数字后跟空格视为行号（跳过）
        // 注释
        if (c == '\'' || (c == 'R' && PeekWord("REM")))
        {
            SkipLine();
            return new Token { Type = TokenType.NewLine, Line = _line };
        }

        // 换行
        if (c == '\n')
        {
            _pos++;
            _line++;
            return new Token { Type = TokenType.NewLine, Line = _line - 1 };
        }
        if (c == '\r')
        {
            _pos++;
            if (_pos < _src.Length && _src[_pos] == '\n') _pos++;
            _line++;
            return new Token { Type = TokenType.NewLine, Line = _line - 1 };
        }

        // 字符串
        if (c == '"')
        {
            return ReadString();
        }

        // 数字
        if (char.IsDigit(c) || (c == '.' && _pos + 1 < _src.Length && char.IsDigit(_src[_pos + 1])))
        {
            return ReadNumber();
        }

        // 标识符 / 关键字
        if (char.IsLetter(c) || c == '_')
        {
            return ReadIdentifier();
        }

        // 运算符
        return ReadOperator();
    }

    private bool PeekWord(string word)
    {
        if (_pos + word.Length > _src.Length) return false;
        for (int i = 0; i < word.Length; i++)
        {
            if (char.ToUpperInvariant(_src[_pos + i]) != word[i]) return false;
        }
        // 后面必须是分隔符
        int after = _pos + word.Length;
        if (after < _src.Length && (char.IsLetterOrDigit(_src[after]) || _src[after] == '_')) return false;
        return true;
    }

    private void SkipWhitespace()
    {
        while (_pos < _src.Length && (_src[_pos] == ' ' || _src[_pos] == '\t'))
            _pos++;
    }

    private void SkipLine()
    {
        while (_pos < _src.Length && _src[_pos] != '\n' && _src[_pos] != '\r')
            _pos++;
    }

    private Token ReadString()
    {
        int startLine = _line;
        _pos++; // 跳过开引号
        var sb = new System.Text.StringBuilder();
        while (_pos < _src.Length && _src[_pos] != '"')
        {
            if (_src[_pos] == '\n') _line++;
            sb.Append(_src[_pos]);
            _pos++;
        }
        if (_pos < _src.Length) _pos++; // 跳过闭引号
        return new Token { Type = TokenType.String, Text = sb.ToString(), Line = startLine };
    }

    private Token ReadNumber()
    {
        int startLine = _line;
        int start = _pos;
        bool isDouble = false;
        while (_pos < _src.Length && (char.IsDigit(_src[_pos]) || _src[_pos] == '.'))
        {
            if (_src[_pos] == '.') isDouble = true;
            _pos++;
        }
        // 科学计数法
        if (_pos < _src.Length && (_src[_pos] == 'e' || _src[_pos] == 'E'))
        {
            int save = _pos;
            _pos++;
            if (_pos < _src.Length && (_src[_pos] == '+' || _src[_pos] == '-')) _pos++;
            if (_pos < _src.Length && char.IsDigit(_src[_pos]))
            {
                while (_pos < _src.Length && char.IsDigit(_src[_pos])) _pos++;
                isDouble = true;
            }
            else
            {
                _pos = save;
            }
        }
        string text = _src.Substring(start, _pos - start);
        double val = double.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;
        return new Token { Type = TokenType.Number, Text = text, Number = val, Line = startLine };
    }

    private Token ReadIdentifier()
    {
        int startLine = _line;
        int start = _pos;
        while (_pos < _src.Length && (char.IsLetterOrDigit(_src[_pos]) || _src[_pos] == '_' || _src[_pos] == '$'))
            _pos++;
        string text = _src.Substring(start, _pos - start);
        return new Token { Type = TokenType.Identifier, Text = text, Line = startLine };
    }

    private Token ReadOperator()
    {
        int startLine = _line;
        char c = _src[_pos];
        _pos++;
        string two = c.ToString();
        if (_pos < _src.Length)
        {
            char n = _src[_pos];
            if ((c == '<' && (n == '=' || n == '>')) || (c == '>' && n == '='))
            {
                two += n;
                _pos++;
            }
        }
        return new Token { Type = TokenType.Operator, Text = two, Line = startLine };
    }
}
