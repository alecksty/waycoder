using CompilerBase;
using System.Text;

namespace CppCompiler
{
    public partial class Lexer : LexerBase
    {
        private readonly List<Token> _tokens = new();

        /// <summary>通过 #param lib(...) 收集的库名/路径列表</summary>
        public List<string> ParamLibraries = new();
        /// <summary>通过 #param path(...) 收集的额外 include 路径</summary>
        public List<string> ParamPaths = new();

        private static readonly Dictionary<string, TokenType> Keywords = new()
        {
            // C subset
            {"int", TokenType.INT}, {"void", TokenType.VOID},
            {"char", TokenType.CHAR}, {"float", TokenType.FLOAT},
            {"double", TokenType.DOUBLE}, {"short", TokenType.SHORT},
            {"long", TokenType.LONG}, {"signed", TokenType.SIGNED},
            {"unsigned", TokenType.UNSIGNED}, {"bool", TokenType.BOOL},
            {"if", TokenType.IF}, {"else", TokenType.ELSE},
            {"while", TokenType.WHILE}, {"for", TokenType.FOR},
            {"do", TokenType.DO}, {"switch", TokenType.SWITCH},
            {"case", TokenType.CASE}, {"default", TokenType.DEFAULT},
            {"return", TokenType.RETURN}, {"break", TokenType.BREAK},
            {"continue", TokenType.CONTINUE}, {"goto", TokenType.GOTO},
            {"struct", TokenType.STRUCT}, {"union", TokenType.UNION},
            {"enum", TokenType.ENUM}, {"const", TokenType.CONST},
            {"static", TokenType.STATIC}, {"extern", TokenType.EXTERN},
            {"typedef", TokenType.TYPEDEF}, {"sizeof", TokenType.SIZEOF},
            {"volatile", TokenType.VOLATILE}, {"auto", TokenType.AUTO},
            {"register", TokenType.REGISTER}, {"inline", TokenType.INLINE},
            {"asm", TokenType.ASM},
            // C99 fixed-width types
            // size_t/ssize_t/ptrdiff_t treated as regular identifiers (handled by type alias fallback)
            {"int8_t", TokenType.INT8}, {"int16_t", TokenType.INT16},
            {"int32_t", TokenType.INT32}, {"int64_t", TokenType.INT64},
            {"uint8_t", TokenType.UINT8}, {"uint16_t", TokenType.UINT16},
            {"uint32_t", TokenType.UINT32}, {"uint64_t", TokenType.UINT64},
            {"intptr_t", TokenType.INTPTR_T},             {"uintptr_t", TokenType.UINTPTR_T},
            {"restrict", TokenType.RESTRICT},
            {"wchar_t", TokenType.IDENTIFIER}, {"char32_t", TokenType.IDENTIFIER},
            // C++ keywords
            {"class", TokenType.CLASS}, {"namespace", TokenType.NAMESPACE},
            {"template", TokenType.TEMPLATE}, {"typename", TokenType.TYPENAME},
            {"public", TokenType.PUBLIC}, {"private", TokenType.PRIVATE},
            {"protected", TokenType.PROTECTED},
            {"new", TokenType.NEW}, {"delete", TokenType.DELETE},
            {"virtual", TokenType.VIRTUAL}, {"override", TokenType.OVERRIDE},
            {"operator", TokenType.OPERATOR}, {"explicit", TokenType.EXPLICIT},
            {"this", TokenType.THIS}, {"friend", TokenType.FRIEND},
            {"mutable", TokenType.MUTABLE}, {"constexpr", TokenType.CONSTEXPR},
            {"try", TokenType.TRY}, {"catch", TokenType.CATCH},
            {"throw", TokenType.THROW}, {"using", TokenType.USING},
            {"nullptr", TokenType.NULLPTR},
            {"static_assert", TokenType.STATIC_ASSERT}, {"noexcept", TokenType.NOEXCEPT},
            {"__stdcall", TokenType.STDCALL},
            {"__fastcall", TokenType.FASTCALL},
            {"__cdecl", TokenType.CDECL},
        };

        public Lexer(string source) : base(source) { }

        /// <summary>
        /// 带行号映射的构造 —— <paramref name="lineMap"/> 就是 <c>Preprocessor.LineMap</c>
        /// （第 N 项 = 预处理输出第 N 行的 <c>(原文件, 原行)</c>）。
        ///
        /// <para>
        /// 不传的话 token 只有**拼接后**的行号，报错就会指到用户文件里另一行上
        /// （实测：`#include &lt;stdio.h&gt;` 有 103 行，它后面所有代码整体后移，
        /// 报错于是落在用户文件第 112 行那句无害的 `/// &lt;summary&gt;` 上）。
        /// </para>
        /// </summary>
        public Lexer(string source, List<(string, int)>? lineMap, string? fileName = null) : base(source, fileName)
        {
            SourceLineMap = lineMap;
        }

        public List<Token> Tokenize()
        {
            while (_pos < _source.Length)
            {
                char c = Peek();
                if (char.IsWhiteSpace(c)) { SkipWhitespace(); continue; }
                if (c == '/' && Peek(1) == '/') { SkipLineComment(); continue; }
                if (c == '/' && Peek(1) == '*') { SkipBlockComment(); continue; }
                if (c == '#' && IsAtLineStart()) { ProcessPreprocessor(); continue; }

                // L/U/u prefix handling for strings and chars (BEFORE identifier check!)
                if ((c == 'L' || c == 'U' || c == 'u') && _pos + 1 < _source.Length && _source[_pos + 1] == '"')
                {
                    _pos++; _col++; // skip prefix
                    TokenType t = c == 'U' ? TokenType.USTRING : TokenType.WSTRING;
                    AddToken(ReadString(t));
                    continue;
                }
                if ((c == 'L' || c == 'U' || c == 'u') && _pos + 1 < _source.Length && _source[_pos + 1] == '\'')
                {
                    _pos++; _col++; // skip prefix
                    TokenType t = c == 'U' ? TokenType.UCHAR_LITERAL : TokenType.WCHAR_LITERAL;
                    AddToken(ReadChar(t));
                    continue;
                }
                if (c == '\'') { AddToken(ReadChar()); continue; }
                if (c == '"') { AddToken(ReadString()); continue; }
                if (char.IsLetter(c) || c == '_') { AddToken(ReadIdentifier()); continue; }
                if (char.IsDigit(c)) { var numTok = ReadNumber(); _tokens.Add(new Token(TokenType.NUMBER, numTok, _line, _col - numTok.Length)); continue; }

                if (!ReadOperator()) { _pos++; _col++; }
            }
            _tokens.Add(new Token(TokenType.EOF, "", _line, _col));

            // 全部 token 建好后**统一**补「原文件 / 原行」——收在一处比在 8 个 `new Token` 点上
            // 各映射一次强：漏掉任何一个构造点都是一个"偶发指错行"，而那种 bug 只有真机上看得见。
            if (SourceLineMap != null)
                foreach (var t in _tokens)
                {
                    var (file, originLine) = MapOriginal(t.Line);
                    t.OriginalFile = file;
                    t.OriginalLine = file != null ? originLine : 0;
                }

            return _tokens;
        }


        private new void SkipWhitespace()
        {
            while (_pos < _source.Length && char.IsWhiteSpace(Peek()))
            {
                if (Peek() == '\n') { _line++; _col = 1; }
                else _col++;
                _pos++;
            }
        }



        private void ProcessPreprocessor()
        {
            _pos++; _col++; // skip '#'
            // skip whitespace after #
            while (_pos < _source.Length && (Peek() == ' ' || Peek() == '\t')) { _pos++; _col++; }

            // read directive name
            int dirStart = _pos;
            while (_pos < _source.Length && char.IsLetter(Peek())) { _pos++; _col++; }
            string directive = _source.Substring(dirStart, _pos - dirStart).ToLowerInvariant();

            if (directive == "param")
            {
                // skip whitespace before arg
                while (_pos < _source.Length && (Peek() == ' ' || Peek() == '\t')) { _pos++; _col++; }

                // read rest of line
                int argStart = _pos;
                while (_pos < _source.Length && Peek() != '\n' && Peek() != '\r') { _pos++; _col++; }
                string rest = _source.Substring(argStart, _pos - argStart).Trim();

                // skip trailing newline
                if (Peek() == '\r') { _pos++; _col++; }
                if (Peek() == '\n') { _pos++; _col++; _line++; }

                ParseParamDirective(rest);
            }
            else
            {
                // skip rest of line for other directives
                while (_pos < _source.Length && Peek() != '\n') { _pos++; _col++; }
            }
        }

        private void ParseParamDirective(string rest)
        {
            if (string.IsNullOrEmpty(rest)) return;

            int parenOpen = rest.IndexOf('(');
            int parenClose = rest.LastIndexOf(')');
            if (parenOpen < 0 || parenClose < 0 || parenClose <= parenOpen) return;

            string func = rest.Substring(0, parenOpen).Trim().ToLowerInvariant();
            string arg = rest.Substring(parenOpen + 1, parenClose - parenOpen - 1).Trim();

            // strip optional outer quotes
            if (arg.Length >= 2 &&
                ((arg.StartsWith('"') && arg.EndsWith('"')) ||
                 (arg.StartsWith('\'') && arg.EndsWith('\''))))
            {
                arg = arg.Substring(1, arg.Length - 2);
            }

            switch (func)
            {
                case "lib":
                    if (!arg.EndsWith(".vml", StringComparison.OrdinalIgnoreCase) &&
                        !arg.Contains('/') && !arg.Contains('\\'))
                    {
                        arg += ".vml";
                    }
                    if (!ParamLibraries.Contains(arg))
                        ParamLibraries.Add(arg);
                    break;

                case "path":
                    if (!ParamPaths.Contains(arg))
                        ParamPaths.Add(arg);
                    break;
            }
        }

        private void AddToken(Token t)
        {
            _tokens.Add(t);
        }

        /// <summary>
        /// 检查当前位置是否在行首（只允许前面有空白字符）
        /// </summary>
        private bool IsAtLineStart()
        {
            if (_pos == 0) return true;
            // 向前查找，直到遇到换行符或非空白字符
            for (int i = _pos - 1; i >= 0; i--)
            {
                char c = _source[i];
                if (c == '\n') return true;
                if (c != ' ' && c != '\t') return false;
            }
            return true; // 文件开头
        }

        private new string ReadIdentifier()
        {
            int start = _pos;
            while (_pos < _source.Length && (char.IsLetterOrDigit(Peek()) || Peek() == '_')) { _pos++; _col++; }
            string val = _source.Substring(start, _pos - start);
            return val;
        }

        private new string ReadNumber()
        {
            int start = _pos;
            bool isHex = false;
            if (Peek() == '0' && (Peek(1) == 'x' || Peek(1) == 'X')) { _pos += 2; _col += 2; isHex = true; }
            while (_pos < _source.Length && (isHex ? LexerHelper.IsHexDigit(Peek()) : char.IsDigit(Peek()))) { _pos++; _col++; }
            if (!isHex && Peek() == '.') { _pos++; _col++; while (_pos < _source.Length && char.IsDigit(Peek())) { _pos++; _col++; } }
            // 读取整数/浮点后缀: f/F/l/L/u/U/ll/LL/ul/uL/Ul/UL/ull/ULL 等
            char c = Peek();
            while (c == 'f' || c == 'F' || c == 'l' || c == 'L' || c == 'u' || c == 'U')
            {
                _pos++; _col++;
                c = Peek();
            }
            return _source.Substring(start, _pos - start);
        }

        private Token ReadChar(TokenType type = TokenType.CHAR_LITERAL)
        {
            _pos++; _col++; // skip '
            int start = _pos;
            if (Peek() == '\\') { _pos++; _col++; }
            _pos++; _col++;
            string value = _source.Substring(start, _pos - start);
            _pos++; _col++; // skip '
            return new Token(type, value, _line, _col);
        }

        private Token ReadString(TokenType type = TokenType.STRING)
        {
            _pos++; _col++; // skip "
            var sb = new System.Text.StringBuilder();
            while (_pos < _source.Length && Peek() != '"')
            {
                if (Peek() == '\\')
                {
                    _pos++; _col++;
                    char next = Peek();
                    switch (next)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case '0': sb.Append('\0'); break;
                        case '\\': sb.Append('\\'); break;
                        case '"': sb.Append('"'); break;
                        default: sb.Append(next); break;
                    }
                    _pos++; _col++;
                }
                else
                {
                    sb.Append(Peek());
                    _pos++; _col++;
                }
            }
            if (_pos < _source.Length) { _pos++; _col++; } // skip "
            return new Token(type, sb.ToString(), _line, _col);
        }

        private bool ReadOperator()
        {
            char c = Peek();
            char n = Peek(1);

            // C++ specific: ::
            if (c == ':' && n == ':') { AddOp(TokenType.SCOPE_RESOLVE, "::"); return true; }
            // 3-char operators first (before 2-char to prevent <<= → << + =)
            if (_pos + 2 < _source.Length)
            {
                string three = c.ToString() + n.ToString() + Peek(2).ToString();
                if (three == "<<=") { AddOp(TokenType.LSHIFT_ASSIGN, "<<="); return true; }
                if (three == ">>=") { AddOp(TokenType.RSHIFT_ASSIGN, ">>="); return true; }
                // `...` 可变参数 —— C 标准头里的 `int printf(const char *, ...)` 就是这个形态。
                // 不认它的话每个 `.` 各自成 DOT，参数列表解析到第一个点就炸
                //（报 "Expected IDENTIFIER but got DOT"）。C 前端早有此分支，这里补齐。
                if (three == "...") { AddOp(TokenType.ELLIPSIS, "..."); return true; }
            }
            // 2-char operators
            string two = c.ToString() + n.ToString();
            var op2 = new Dictionary<string, TokenType>
            {
                { "==", TokenType.EQ }, { "!=", TokenType.NE },
                { "<=", TokenType.LE }, { ">=", TokenType.GE },
                { "&&", TokenType.AND }, { "||", TokenType.OR },
                { "++", TokenType.INCREMENT }, { "--", TokenType.DECREMENT },
                { "->", TokenType.ARROW },
                { "<<", TokenType.LSHIFT }, { ">>", TokenType.RSHIFT },
                { "+=", TokenType.ADD_ASSIGN }, { "-=", TokenType.SUB_ASSIGN },
                { "*=", TokenType.MUL_ASSIGN }, { "/=", TokenType.DIV_ASSIGN },
                { "%=", TokenType.MOD_ASSIGN },
                { "&=", TokenType.AND_ASSIGN }, { "|=", TokenType.OR_ASSIGN },
                { "^=", TokenType.XOR_ASSIGN },
                { "//", TokenType.ERROR }, { "/*", TokenType.ERROR },
            };
            if (two.Length == 2 && op2.TryGetValue(two, out TokenType t2) && t2 != TokenType.ERROR)
            { AddOp(t2, two); return true; }

            var op1 = new Dictionary<char, TokenType>
            {
                {'+', TokenType.PLUS}, {'-', TokenType.MINUS},
                {'*', TokenType.STAR}, {'/', TokenType.SLASH},
                {'%', TokenType.PERCENT}, {'&', TokenType.AMPERSAND},
                {'|', TokenType.PIPE}, {'^', TokenType.CARET},
                {'~', TokenType.TILDE},
                {'=', TokenType.ASSIGN},
                {'<', TokenType.LT}, {'>', TokenType.GT},
                {'!', TokenType.NOT},
                {'.', TokenType.DOT}, {',', TokenType.COMMA},
                {';', TokenType.SEMICOLON},
                {'(', TokenType.LPAREN}, {')', TokenType.RPAREN},
                {'{', TokenType.LBRACE}, {'}', TokenType.RBRACE},
                {'[', TokenType.LBRACKET}, {']', TokenType.RBRACKET},
                {'?', TokenType.QUESTION}, {':', TokenType.COLON},
                {'#', TokenType.HASH},
            };
            if (op1.TryGetValue(c, out TokenType t1))
            { AddOp(t1, c.ToString()); return true; }
            return false;
        }

        private void AddOp(TokenType type, string val)
        {
            _tokens.Add(new Token(type, val, _line, _col));
            _pos += val.Length;
            _col += val.Length;
        }

        private TokenType GetKeywordType(string word)
        {
            return Keywords.TryGetValue(word, out TokenType t) ? t : TokenType.IDENTIFIER;
        }

        private Token ReadIdentifierAsToken(string word)
        {
            return new Token(GetKeywordType(word), word, _line, _col - word.Length);
        }
    }

    // Hide the static method in the form used by AddToken above
    public partial class Lexer : LexerBase
    {
        private void AddToken(string word)
        {
            if (Keywords.TryGetValue(word, out TokenType t))
                _tokens.Add(new Token(t, word, _line, _col - word.Length));
            else
                _tokens.Add(new Token(TokenType.IDENTIFIER, word, _line, _col - word.Length));
        }
    }
}
