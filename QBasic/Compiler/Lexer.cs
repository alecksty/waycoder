// =============================================================
// Lexer.cs —— 词法分析器
//
// 把 BASIC 源码拆分为 Token 序列。支持：
//   - 数字（含小数与指数）、字符串字面量（"..."）
//   - 标识符 / 关键字（大小写不敏感）
//   - 运算符：+ - * / \ = <> < > <= >= MOD AND OR NOT ( ) , ; $
//   - 注释：REM 开头或 ' 单引号
//   - 行首行号标签（数字后跟空格/语句）
// =============================================================
using System.Globalization;

namespace QBasic.Compiler;

/// <summary>BASIC 关键字集合（统一大写比较）。</summary>
public static class Keywords
{
    public static readonly HashSet<string> Set = new(StringComparer.OrdinalIgnoreCase)
    {
        "LET", "PRINT", "INPUT", "IF", "THEN", "ELSE", "ENDIF", "END", "FOR", "TO", "STEP", "NEXT",
        "WHILE", "WEND", "GOTO", "GOSUB", "RETURN", "REM", "DIM", "AND", "OR", "NOT", "MOD",
        "INT", "SQR", "ABS", "LEN", "CHR$", "VAL", "STR$", "MID$", "LEFT$", "RIGHT$",
        "SELECT", "CASE", "IS", "ENDSELECT", "DO", "LOOP", "UNTIL",
        "RANDOMIZE", "DATA", "READ", "RESTORE",
        "INSTR", "UCASE$", "LCASE$", "STRING$", "SPACE$", "LTRIM$", "RTRIM$", "RND",
        "SIN", "COS", "ATN", "TAN", "CINT", "CDBL", "CSNG", "POINT", "TIMER", "INKEY$",
        "SCREEN", "CLS", "LINE", "CIRCLE", "PSET", "PAINT", "COLOR", "PALETTE", "LOCATE",
        "GET", "PUT", "PLAY", "SLEEP", "BEEP", "WIDTH", "CALL", "DECLARE", "SUB", "FUNCTION",
        "TYPE", "CONST", "REDIM", "SHARED", "DEFINT", "DEFSNG", "DEFDBL", "DEFSTR", "DEF",
        "FN", "ON", "ERROR", "RESUME", "AS", "ANY", "STATIC", "PEEK", "POKE", "SEG",
    };
}

/// <summary>词法分析器。</summary>
public sealed class Lexer
{
    private readonly string _src;
    private int _pos;
    private int _line = 1;

    public Lexer(string source)
    {
        _src = source;
    }

    /// <summary>产生全部记号。</summary>
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

    private char Cur => _pos < _src.Length ? _src[_pos] : '\0';
    private char Peek(int n = 1) => _pos + n < _src.Length ? _src[_pos + n] : '\0';

    private Token Make(TokenType type, string text) => new() { Type = type, Text = text, Line = _line };

    private Token Next()
    {
        SkipWhitespaceAndComments();
        if (_pos >= _src.Length) return Make(TokenType.Eof, "");

        char c = Cur;

        // 行号：行首数字后接非数字即视为行号标签
        if (char.IsDigit(c) && IsAtLineStart())
        {
            int start = _pos;
            while (_pos < _src.Length && char.IsDigit(_src[_pos])) _pos++;
            string text = _src[start.._pos];
            return new Token { Type = TokenType.LineNum, Text = text, Num = double.Parse(text, CultureInfo.InvariantCulture), Line = _line };
        }

        // 数字（含类型后缀 # ! % &）
        if (char.IsDigit(c) || (c == '.' && char.IsDigit(Peek())))
        {
            var num = LexNumber();
            if (Cur == '#' || Cur == '!' || Cur == '%' || Cur == '&') _pos++;
            return num;
        }

        // 字符串
        if (c == '"')
        {
            _pos++;
            var sb = new System.Text.StringBuilder();
            while (_pos < _src.Length && _src[_pos] != '"')
            {
                if (_src[_pos] == '\n') _line++;
                sb.Append(_src[_pos]);
                _pos++;
            }
            if (_pos < _src.Length) _pos++; // 结束引号
            return new Token { Type = TokenType.Str, Text = sb.ToString(), Str = sb.ToString(), Line = _line };
        }

        // 标识符 / 关键字
        if (char.IsLetter(c) || c == '_')
        {
            int start = _pos;
            while (_pos < _src.Length && (char.IsLetterOrDigit(_src[_pos]) || _src[_pos] == '_' || _src[_pos] == '$' || _src[_pos] == '#' || _src[_pos] == '!' || _src[_pos] == '%' || _src[_pos] == '&'))
                _pos++;
            string text = _src[start.._pos];
            return new Token { Type = TokenType.Ident, Text = text, Line = _line };
        }

        // 运算符
        if (c == '=') { _pos++; return Make(TokenType.Op, "="); }
        if (c == '+') { _pos++; return Make(TokenType.Op, "+"); }
        if (c == '-') { _pos++; return Make(TokenType.Op, "-"); }
        if (c == '*') { _pos++; return Make(TokenType.Op, "*"); }
        if (c == '/') { _pos++; return Make(TokenType.Op, "/"); }
        if (c == '\\') { _pos++; return Make(TokenType.Op, "\\"); } // 整数除
        if (c == '^') { _pos++; return Make(TokenType.Op, "^"); }   // 幂
        if (c == '<')
        {
            _pos++;
            if (Cur == '>') { _pos++; return Make(TokenType.Op, "<>"); }
            if (Cur == '=') { _pos++; return Make(TokenType.Op, "<="); }
            return Make(TokenType.Op, "<");
        }
        if (c == '>')
        {
            _pos++;
            if (Cur == '=') { _pos++; return Make(TokenType.Op, ">="); }
            return Make(TokenType.Op, ">");
        }
        if (c == '(') { _pos++; return Make(TokenType.Op, "("); }
        if (c == ')') { _pos++; return Make(TokenType.Op, ")"); }
        if (c == ',') { _pos++; return Make(TokenType.Op, ","); }
        if (c == ';') { _pos++; return Make(TokenType.Op, ";"); }
        if (c == '$') { _pos++; return Make(TokenType.Op, "$"); }
        if (c == ':') { _pos++; return Make(TokenType.Op, ":"); }

        // 换行
        if (c == '\n') { _pos++; _line++; return Make(TokenType.Newline, "\n"); }
        if (c == '\r') { _pos++; return Make(TokenType.Newline, "\r"); }

        // 未知字符跳过
        _pos++;
        return Make(TokenType.Op, c.ToString());
    }

    private bool IsAtLineStart()
    {
        // 判断当前位置是否为语句起始（前面只有空白或行首）
        int p = _pos;
        while (p > 0 && (_src[p - 1] == ' ' || _src[p - 1] == '\t')) p--;
        return p == 0 || _src[p - 1] == '\n';
    }

    private Token LexNumber()
    {
        int start = _pos;
        while (_pos < _src.Length && char.IsDigit(_src[_pos])) _pos++;
        if (_pos < _src.Length && _src[_pos] == '.')
        {
            _pos++;
            while (_pos < _src.Length && char.IsDigit(_src[_pos])) _pos++;
        }
        // 指数
        if (_pos < _src.Length && (_src[_pos] == 'e' || _src[_pos] == 'E'))
        {
            int save = _pos;
            _pos++;
            if (_pos < _src.Length && (_src[_pos] == '+' || _src[_pos] == '-')) _pos++;
            if (_pos < _src.Length && char.IsDigit(_src[_pos]))
            {
                while (_pos < _src.Length && char.IsDigit(_src[_pos])) _pos++;
            }
            else _pos = save;
        }
        string text = _src[start.._pos];
        double val = double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;
        return new Token { Type = TokenType.Number, Text = text, Num = val, Line = _line };
    }

    private void SkipWhitespaceAndComments()
    {
        while (_pos < _src.Length)
        {
            char c = _src[_pos];
            if (c == ' ' || c == '\t') { _pos++; continue; }
            if (c == '\n' || c == '\r') return;
            if (c == '\'')
            {
                while (_pos < _src.Length && _src[_pos] != '\n') _pos++;
                return;
            }
            // REM 注释
            if (c == 'r' || c == 'R')
            {
                if (IsRemAtPos())
                {
                    while (_pos < _src.Length && _src[_pos] != '\n') _pos++;
                    return;
                }
            }
            return;
        }
    }

    private bool IsRemAtPos()
    {
        // 检查当前位置起是否为 REM 关键字（后面跟空格或行尾）
        string lower = _src.Substring(_pos, Math.Min(3, _src.Length - _pos));
        if (!lower.Equals("rem", StringComparison.OrdinalIgnoreCase)) return false;
        if (_pos + 3 < _src.Length)
        {
            char after = _src[_pos + 3];
            return after == ' ' || after == '\t' || after == '\n' || after == '\r';
        }
        return true;
    }
}
