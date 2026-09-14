using System.Text;
using CompilerBase;

namespace BasicCompiler
{
    /// <summary>
    /// BASIC 词法分析器
    /// </summary>
    public class Lexer : LexerBase
    {

        /// <summary>通过 '$param lib(...) 收集的库名列表</summary>
        public List<string> ParamLibraries = new();
        /// <summary>通过 '$param path(...) 收集的路径列表</summary>
        public List<string> ParamPaths = new();

        private static readonly Dictionary<string, TokenType> keywords = new Dictionary<string, TokenType>
        {
            { "REM", TokenType.REM },
            { "PRINT", TokenType.PRINT },
            { "INPUT", TokenType.INPUT },
            { "LET", TokenType.LET },
            { "IF", TokenType.IF },
            { "THEN", TokenType.THEN },
            { "ELSE", TokenType.ELSE },
            { "ELSEIF", TokenType.ELSEIF },
            { "END", TokenType.END },
            { "GOTO", TokenType.GOTO },
            { "GOSUB", TokenType.GOSUB },
            { "RETURN", TokenType.RETURN },
            { "FOR", TokenType.FOR },
            { "TO", TokenType.TO },
            { "STEP", TokenType.STEP },
            { "NEXT", TokenType.NEXT },
            { "WHILE", TokenType.WHILE },
            { "WEND", TokenType.WEND },
            { "DO", TokenType.DO },
            { "LOOP", TokenType.LOOP },
            { "UNTIL", TokenType.UNTIL },
            { "DIM", TokenType.DIM },
            { "AND", TokenType.AND },
            { "OR", TokenType.OR },
            { "NOT", TokenType.NOT },
            { "MOD", TokenType.MOD_KW },
            { "SUB", TokenType.SUB },
            { "FUNCTION", TokenType.FUNCTION },
            { "CALL", TokenType.CALL },
            { "BYVAL", TokenType.BYVAL },
            { "BYREF", TokenType.BYREF },
            { "EXIT", TokenType.EXIT },
            // SELECT CASE 关键字
            { "SELECT", TokenType.SELECT },
            { "CASE", TokenType.CASE },
            { "IS", TokenType.IS },
            // 硬件接口关键字
            { "KBHIT", TokenType.KB_HIT },
            { "KBGETCH", TokenType.KB_GETCH },
            { "MOUSEGETX", TokenType.MOUSE_GETX },
            { "MOUSEGETY", TokenType.MOUSE_GETY },
            { "MOUSELEFT", TokenType.MOUSE_LEFT },
            { "MOUSERIGHT", TokenType.MOUSE_RIGHT },
            // 文件操作关键字
            { "OPEN", TokenType.OPEN },
            { "CLOSE", TokenType.CLOSE },
            { "AS", TokenType.AS },
            { "FREEFILE", TokenType.FREEFILE },
            // Turbo Basic 扩展关键字
            { "LOCAL", TokenType.LOCAL },
            { "STATIC", TokenType.STATIC_KW },
            { "SHARED", TokenType.SHARED },
            { "COMMON", TokenType.COMMON },
            { "OPTION", TokenType.OPTION_KW },
            // QBASIC 标准关键字
            { "SCREEN", TokenType.SCREEN },
            { "CLS", TokenType.CLS_KW },
            { "PSET", TokenType.PSET },
            { "LINE", TokenType.QB_LINE },
            { "CIRCLE", TokenType.QB_CIRCLE },
            { "PAINT", TokenType.PAINT },
            { "LOCATE", TokenType.LOCATE },
            { "COLOR", TokenType.QB_COLOR },
            { "RANDOMIZE", TokenType.RANDOMIZE },
            { "WIDTH", TokenType.QB_WIDTH },
            { "INKEY$", TokenType.INKEY },
            { "BEEP", TokenType.BEEP },
            { "SLEEP", TokenType.SLEEP },
            { "SWAP", TokenType.SWAP },
            { "ERASE", TokenType.ERASE },
            { "SYSTEM", TokenType.SYSTEM },
            { "POKE", TokenType.POKE },
            { "CHIPASM", TokenType.CHIPASM },
            { "__stdcall", TokenType.STDCALL },
            // { "ASM", TokenType.ASM_KW },  — asm() 已移除，仅限 C/ObjC/C++ 语言
            { "DEFINT", TokenType.DEFINT },
            { "DEFSNG", TokenType.DEFSNG },
            { "DEFSTR", TokenType.DEFSTR },
            { "DEFDBL", TokenType.DEFDBL },
            { "DEFLNG", TokenType.DEFLNG },
            { "LPRINT", TokenType.LPRINT },
            { "VIEW", TokenType.VIEW },
            { "DECLARE", TokenType.DECLARE },
            { "NATIVE", TokenType.NATIVE },
            { "WINDOW", TokenType.WINDOW },
            { "TIMER", TokenType.TIMER_FUNC },
            { "DATE$", TokenType.DATE_FUNC },
            { "TIME$", TokenType.TIME_FUNC },
            // DATA/READ/RESTORE
            { "DATA", TokenType.DATA },
            { "READ", TokenType.READ_KW },
            { "RESTORE", TokenType.RESTORE },
            // CONST
            { "CONST", TokenType.CONST_KW },
            // ON ERROR GOTO / RESUME
            { "ON", TokenType.ON },
            { "ERROR", TokenType.ERROR_KW },
            { "RESUME", TokenType.RESUME },
            // DRAW
            { "DRAW", TokenType.DRAW_KW },
            // PRINT USING
            { "USING", TokenType.USING_KW },
            // TYPE
            { "TYPE", TokenType.TYPE_KW },
            // FreeBasic OOP keywords (v1.66.31+)
            { "CLASS", TokenType.CLASS_KW },
            { "CONSTRUCTOR", TokenType.CONSTRUCTOR },
            { "DESTRUCTOR", TokenType.DESTRUCTOR },
            { "PROPERTY", TokenType.PROPERTY_KW },
            { "METHOD", TokenType.METHOD_KW },
            { "PTR", TokenType.PTR_KW },
            { "CAST", TokenType.CAST_KW },
            { "EXTENDS", TokenType.EXTENDS_KW },
            { "OPERATOR", TokenType.OPERATOR_KW },
            { "ENUM", TokenType.ENUM_KW },
            // PureBasic keywords (v1.66.32+) — 映射到已有实现
            { "PROCEDURE", TokenType.SUB },          // PROCEDURE → SUB
            { "GLOBAL", TokenType.SHARED },          // GLOBAL → SHARED
            { "PROTECTED", TokenType.PROTECTED_KW }, // PROTECTED (待完整实现)
            { "INTERFACE", TokenType.INTERFACE_KW }, // INTERFACE (待完整实现)
            { "ENDINTERFACE", TokenType.ENDINTERFACE },
            { "NEW", TokenType.NEW_KW },
            // ChipBasic MCU keywords (v1.66.32+)
            { "PINMODE", TokenType.PINMODE_KW },
            { "DIGITALWRITE", TokenType.DIGITALWRITE },
            { "DIGITALREAD", TokenType.DIGITALREAD },
            // TrueBasic keywords (v1.66.32+)
            { "MAT", TokenType.MAT_KW },
            { "ZER", TokenType.ZER_KW },
            { "CON", TokenType.CON_KW },
            // GW-BASIC keywords (v1.66.32+)
            { "BLOAD", TokenType.BLOAD_KW },
            { "BSAVE", TokenType.BSAVE_KW },
            { "KEY", TokenType.KEY_KW },
            // PowerBASIC keywords (v1.66.32+)
            { "THREADED", TokenType.THREADED },
            { "FASTPROC", TokenType.FASTPROC },
            // VisualBasic keywords (v1.66.32+) — 映射到已有实现
            { "Private", TokenType.PRIVATE_KW },
            { "Public", TokenType.PUBLIC_KW },
            { "Friend", TokenType.FRIEND_KW },
            { "Optional", TokenType.OPTIONAL_KW },
            { "ParamArray", TokenType.PARAMARRAY },
            { "With", TokenType.WITH_KW },
            { "Integer", TokenType.VB_INTEGER },
            { "String", TokenType.VB_STRING },
            { "Object", TokenType.VB_OBJECT },
            { "Variant", TokenType.VB_VARIANT },
            // PLAY/SOUND
            { "PLAY", TokenType.PLAY_KW },
            { "SOUND", TokenType.SOUND_KW },
            // REDIM/PRESERVE
            { "REDIM", TokenType.REDIM },
            { "PRESERVE", TokenType.PRESERVE },
            // DEF FN
            { "DEF", TokenType.DEF_KW },
            { "FN", TokenType.FN_KW },
            // GET/PUT
            { "GET", TokenType.GET_KW },
            { "PUT", TokenType.PUT_KW },
            // PALETTE
            { "PALETTE", TokenType.PALETTE_KW }
        };

        public Lexer(string source) : base(source) { }

        public List<Token> Tokenize()
        {
            List<Token> tokens = new List<Token>();

            while (_pos < _source.Length)
            {
                char current = Peek();

                if (char.IsWhiteSpace(current))
                {
                    SkipWhitespace();
                }
                else if (char.IsLetter(current) || IsChineseChar(current))
                {
                    Token tok = ReadIdentifier();
                    if (tok.Type == TokenType.REM)
                    {
                        // REM is a comment - skip rest of _line
                        while (_pos < _source.Length && _source[_pos] != '\n')
                        {
                            _pos++;
                            _col++;
                        }
                        continue;
                    }
                    tokens.Add(tok);
                }
                else if (current == '&' && (_pos + 1 < _source.Length && (_source[_pos + 1] == 'H' || _source[_pos + 1] == 'h')))
                {
                    tokens.Add(ReadHexNumber());
                }
                else if (char.IsDigit(current))
                {
                    tokens.Add(ReadNumber());
                }
                else if (current == '"')
                {
                    tokens.Add(ReadString());
                }
                else if (current == '=')
                {
                    tokens.Add(new Token(TokenType.EQUALS, "=", _line, _col));
                    Advance();
                }
                else if (current == '+')
                {
                    tokens.Add(new Token(TokenType.PLUS, "+", _line, _col));
                    Advance();
                }
                else if (current == '-')
                {
                    tokens.Add(new Token(TokenType.MINUS, "-", _line, _col));
                    Advance();
                }
                else if (current == '*')
                {
                    tokens.Add(new Token(TokenType.MULTIPLY, "*", _line, _col));
                    Advance();
                }
                else if (current == '/')
                {
                    tokens.Add(new Token(TokenType.DIVIDE, "/", _line, _col));
                    Advance();
                }
                else if (current == '<')
                {
                    Advance();
                    if (Peek() == '>')
                    {
                        Advance();
                        tokens.Add(new Token(TokenType.NOT_EQUAL, "<>", _line, _col - 1));
                    }
                    else if (Peek() == '=')
                    {
                        Advance();
                        tokens.Add(new Token(TokenType.LESS_EQUAL, "<=", _line, _col - 1));
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.LESS, "<", _line, _col - 1));
                    }
                }
                else if (current == '>')
                {
                    Advance();
                    if (Peek() == '=')
                    {
                        Advance();
                        tokens.Add(new Token(TokenType.GREATER_EQUAL, ">=", _line, _col - 1));
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.GREATER, ">", _line, _col - 1));
                    }
                }
                else if (current == ',')
                {
                    tokens.Add(new Token(TokenType.COMMA, ",", _line, _col));
                    Advance();
                }
                else if (current == ';')
                {
                    tokens.Add(new Token(TokenType.SEMICOLON, ";", _line, _col));
                    Advance();
                }
                else if (current == ':')
                {
                    tokens.Add(new Token(TokenType.COLON, ":", _line, _col));
                    Advance();
                }
                else if (current == '(')
                {
                    tokens.Add(new Token(TokenType.LPAREN, "(", _line, _col));
                    Advance();
                }
                else if (current == ')')
                {
                    tokens.Add(new Token(TokenType.RPAREN, ")", _line, _col));
                    Advance();
                }
                else if (current == '#')
                {
                    tokens.Add(new Token(TokenType.HASH, "#", _line, _col));
                    Advance();
                }
                else if (current == '^')
                {
                    tokens.Add(new Token(TokenType.EXPONENT, "^", _line, _col));
                    Advance();
                }
                else if (current == '.')
                {
                    tokens.Add(new Token(TokenType.DOT, ".", _line, _col));
                    Advance();
                }
                else if (current == '\'')
                {
                    // Single-quote comment (QBASIC style)
                    Advance();
                    int commentStart = _pos;

                    if (_pos < _source.Length && _source[_pos] == '$')
                    {
                        while (_pos < _source.Length && _source[_pos] != '\n')
                        {
                            _pos++;
                            _col++;
                        }
                        string comment = _source.Substring(commentStart, _pos - commentStart).Trim();
                        if (comment.StartsWith("$param"))
                            ParseParamDirective(comment);
                    }
                    else
                    {
                        while (_pos < _source.Length && _source[_pos] != '\n')
                        {
                            _pos++;
                            _col++;
                        }
                    }
                    continue;
                }
                else
                {
                    // 未知字符
                    tokens.Add(new Token(TokenType.ERROR, current.ToString(), _line, _col));
                    Advance();
                }
            }

            tokens.Add(new Token(TokenType.EOF, "EOF", _line, _col));
            return tokens;
        }


        private new Token ReadIdentifier()
        {
            StringBuilder sb = new StringBuilder();
            int startColumn = _col;

            while (_pos < _source.Length && (char.IsLetterOrDigit(Peek()) || Peek() == '_' || IsChineseChar(Peek())))
            {
                sb.Append(Advance());
            }

            // 处理 QBasic 变量类型后缀: $=string %=integer !=single #=double &=long
            if (_pos < _source.Length && (Peek() == '$' || Peek() == '%' || Peek() == '!' || Peek() == '#' || Peek() == '&'))
            {
                sb.Append(Advance());
            }

            string value = sb.ToString();
            // 关键字仍然需要大写比较
            string upperValue = value.ToUpper();
            if (keywords.ContainsKey(upperValue))
            {
                return new Token(keywords[upperValue], value, _line, startColumn);
            }
            return new Token(TokenType.IDENTIFIER, value, _line, startColumn);
        }

        private new Token ReadNumber()
        {
            StringBuilder sb = new StringBuilder();
            int startColumn = _col;

            while (_pos < _source.Length && char.IsDigit(Peek()))
            {
                sb.Append(Advance());
            }

            if (_pos < _source.Length && Peek() == '.')
            {
                sb.Append(Advance());
                while (_pos < _source.Length && char.IsDigit(Peek()))
                {
                    sb.Append(Advance());
                }
            }

            return new Token(TokenType.NUMBER, sb.ToString(), _line, startColumn);
        }

        private Token ReadHexNumber()
        {
            StringBuilder sb = new StringBuilder();
            int startColumn = _col;
            sb.Append(Advance()); // &
            sb.Append(Advance()); // H
            while (_pos < _source.Length && "0123456789ABCDEFabcdef".Contains(Peek()))
            {
                sb.Append(Advance());
            }
            return new Token(TokenType.NUMBER, sb.ToString(), _line, startColumn);
        }

        private Token ReadString()
        {
            StringBuilder sb = new StringBuilder();
            int startColumn = _col;
            Advance(); // 跳过开始的引号

            while (_pos < _source.Length && Peek() != '"')
            {
                sb.Append(Advance());
            }

            if (_pos < _source.Length)
            {
                Advance(); // 跳过结束的引号
            }

            return new Token(TokenType.STRING, sb.ToString(), _line, startColumn);
        }


        private void ParseParamDirective(string content)
        {
            string rest = content.Substring("$param".Length).Trim();
            int parenOpen = rest.IndexOf('(');
            int parenClose = rest.LastIndexOf(')');
            if (parenOpen < 0 || parenClose < 0 || parenClose <= parenOpen) return;

            string func = rest.Substring(0, parenOpen).Trim().ToLowerInvariant();
            string arg = rest.Substring(parenOpen + 1, parenClose - parenOpen - 1).Trim();

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
                        arg += ".vml";
                    if (!ParamLibraries.Contains(arg))
                        ParamLibraries.Add(arg);
                    break;
                case "path":
                    if (!ParamPaths.Contains(arg))
                        ParamPaths.Add(arg);
                    break;
            }
        }


    }
}
