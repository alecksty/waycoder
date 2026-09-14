using System.Text;
using CompilerBase;

namespace ForthCompiler
{
    /// <summary>
    /// Forth语言词法分析器
    /// </summary>
    public class Lexer : LexerBase
    {
        public List<Token> Tokens { get; private set; }

        /// <summary>通过 \ param lib(...) 收集的库名列表</summary>
        public List<string> ParamLibraries = new();
        /// <summary>通过 \ param path(...) 收集的路径列表</summary>
        public List<string> ParamPaths = new();

        // Forth关键字映射
        private static readonly Dictionary<string, TokenType> KEYWORDS = new()
        {
            // 词定义
            { ":", TokenType.COLON },
            { ";", TokenType.SEMICOLON },
            
            // 栈操作
            { "DUP", TokenType.DUP },
            { "DROP", TokenType.DROP },
            { "SWAP", TokenType.SWAP },
            { "OVER", TokenType.OVER },
            { "ROT", TokenType.ROT },
            { "?DUP", TokenType.QDUP },
            { "2DUP", TokenType.DUP2 },
            { "2DROP", TokenType.DROP2 },
            { "2SWAP", TokenType.SWAP2 },
            { "2OVER", TokenType.OVER2 },
            { "2ROT", TokenType.ROT2 },
            { "PICK", TokenType.PICK },
            { "ROLL", TokenType.ROLL },
            { "DEPTH", TokenType.DEPTH },
            { "CLEAR", TokenType.CLEAR },
            
            // 算术运算
            { "+", TokenType.PLUS },
            { "-", TokenType.MINUS },
            { "*", TokenType.MULTIPLY },
            { "/", TokenType.DIVIDE },
            { "MOD", TokenType.MOD },
            { "/MOD", TokenType.DIVMOD },
            { "1+", TokenType.INCREMENT },
            { "1-", TokenType.DECREMENT },
            { "2*", TokenType.DOUBLE },
            { "2/", TokenType.HALF },
            
            // 比较运算
            { "=", TokenType.EQUAL },
            { "<>", TokenType.NOTEQUAL },
            { "<", TokenType.LESSTHAN },
            { ">", TokenType.GREATERTHAN },
            { "<=", TokenType.LESSEQUAL },
            { ">=", TokenType.GREATEREQUAL },
            { "0=", TokenType.ZEROEQUAL },
            { "0<>", TokenType.ZERONOTEQUAL },
            { "0<", TokenType.ZEROLESSTHAN },
            { "0>", TokenType.ZEROGREATERTHAN },
            
            // 逻辑运算
            { "AND", TokenType.AND },
            { "OR", TokenType.OR },
            { "XOR", TokenType.XOR },
            { "NOT", TokenType.NOT },
            { "INVERT", TokenType.INVERT },
            
            // 控制流
            { "IF", TokenType.IF },
            { "THEN", TokenType.THEN },
            { "ELSE", TokenType.ELSE },
            { "BEGIN", TokenType.BEGIN },
            { "UNTIL", TokenType.UNTIL },
            { "WHILE", TokenType.WHILE },
            { "REPEAT", TokenType.REPEAT },
            { "DO", TokenType.DO },
            { "LOOP", TokenType.LOOP },
            { "+LOOP", TokenType.PLUSLOOP },
            { "I", TokenType.I },
            { "J", TokenType.J },
            { "LEAVE", TokenType.LEAVE },
            { "RECURSE", TokenType.RECURSE },
            { "CASE", TokenType.CASE },
            { "OF", TokenType.OF },
            { "ENDOF", TokenType.ENDOF },
            { "ENDCASE", TokenType.ENDCASE },
            
            // 内存操作
            { "!", TokenType.STORE },
            { "@", TokenType.FETCH },
            { "C!", TokenType.CSTORE },
            { "C@", TokenType.CFETCH },
            { "ALLOT", TokenType.ALLOT },
            { "HERE", TokenType.HERE },
            { "ALLOC", TokenType.ALLOC },
            { "FREE", TokenType.FREE },

            // 浮点类型和操作
            { "FLOAT", TokenType.FLOAT },
            { "SFLOAT", TokenType.SFLOAT },
            { "DFLOAT", TokenType.DFLOAT },
            { "F+", TokenType.FPLUS },
            { "F-", TokenType.FMINUS },
            { "F*", TokenType.FMULTIPLY },
            { "F/", TokenType.FDIVIDE },
            { "F=", TokenType.FEQUAL },
            { "F<", TokenType.FLESSTHAN },
            { "F>", TokenType.FGREATERTHAN },
            { "F<=", TokenType.FLESSEQUAL },
            { "F>=", TokenType.FGREATEREQUAL },
            { "FLOAD", TokenType.FLOAD },
            { "FSTORE", TokenType.FSTORE },
            { "F.", TokenType.FDOT },

            // 文件操作
            { "FOPEN", TokenType.FOPEN },
            { "FCLOSE", TokenType.FCLOSE },
            { "FREAD", TokenType.FREAD },
            { "FWRITE", TokenType.FWRITE },
            { "FSEEK", TokenType.FSEEK },
            { "FTELL", TokenType.FTELL },

            // 异常处理
            { "CATCH", TokenType.CATCH },
            { "THROW", TokenType.THROW },
            { "END-CATCH", TokenType.ENDCATCH },
            
            // 输入输出
            { ".", TokenType.DOT },
            { ".\"", TokenType.DOTSTRING },
            { ".(", TokenType.DOTQUOTE },
            { "EMIT", TokenType.EMIT },
            { "KEY", TokenType.KEY },
            { "CR", TokenType.CR },
            { "SPACE", TokenType.SPACE },
            { "SPACES", TokenType.SPACES },
            { "TYPE", TokenType.TYPE },
            { "COUNT", TokenType.COUNT },
            { "TRAILING", TokenType.TRAILING },
            
            // 变量和常量
            { "VARIABLE", TokenType.VARIABLE },
            { "CONSTANT", TokenType.CONSTANT },
            { "CREATE", TokenType.CREATE },
            { "DOES>", TokenType.DOES },
            
            // ASM/asm 已移除 — asm() 仅限 C/ObjC/C++ 语言，Forth 通过 Lib/shared/vmlsys.c 调用系统功能
            
            // 其他
            { "EXECUTE", TokenType.EXECUTE },
            { "EXIT", TokenType.EXIT },
            { "ABORT", TokenType.ABORT },
            { "QUIT", TokenType.QUIT },
            { "BYE", TokenType.BYE },
            
            // 注释标记
            { "(", TokenType.PAREN_COMMENT },
            { "\\", TokenType.BACKSLASH_COMMENT }
        };

        public Lexer(string source) : base(source)
        {
            Tokens = new List<Token>();
        }



        private void AddToken(TokenType type, string value = null)
        {
            Tokens.Add(new Token(type, value, _line, _col));
        }

        /// <summary>
        /// 词法分析主方法
        /// </summary>
        public List<Token> Tokenize()
        {
            while (_pos < _source.Length)
            {
                char ch = Peek();
                
                // 跳过空白字符
                if (char.IsWhiteSpace(ch))
                {
                    if (ch == '\n')
                    {
                        AddToken(TokenType.NEWLINE);
                    }
                    Advance();
                    continue;
                }
                
                // 处理数字 — 但先检查是否有以数字开头的关键字（如 1+, 0=, 2*, 2DUP 等）
                if (char.IsDigit(ch) || (ch == '-' && char.IsDigit(Peek(1))) || ch == '$' || ch == '%')
                {
                    // 向前看是否构成关键字（如 "1+" 应作为 INCREMENT 而非 NUMBER + PLUS）
                    string lookahead = ScanIdentifierAhead();
                    if (lookahead != null && KEYWORDS.ContainsKey(lookahead.ToUpperInvariant()))
                    {
                        TokenizeIdentifier();
                        continue;
                    }
                    TokenizeNumber();
                    continue;
                }
                
                // 处理字符串字面量 ." ... "
                if (ch == '.' && Peek(1) == '"')
                {
                    TokenizeDotString();
                    continue;
                }
                
                // 处理字符串字面量 S" ... "
                if (ch == 'S' && Peek(1) == '"')
                {
                    TokenizeSString();
                    continue;
                }
                
                // 处理字符字面量
                if (ch == '\'')
                {
                    TokenizeChar();
                    continue;
                }
                
                // 处理注释
                if (ch == '(')
                {
                    TokenizeParenComment();
                    continue;
                }
                
                if (ch == '\\')
                {
                    TokenizeBackslashComment();
                    continue;
                }
                
                // 处理标识符和关键字
                if (IsIdentifierStart(ch))
                {
                    TokenizeIdentifier();
                    continue;
                }
                
                // 处理单个字符的Token（如 + - * / 等）
                TokenizeSingleChar();
            }
            
            AddToken(TokenType.EOF);
            return Tokens;
        }

        private void TokenizeNumber()
        {
            int start = _pos;
            int startLine = _line;
            int startColumn = _col;
            
            StringBuilder sb = new StringBuilder();
            bool isHex = false;
            bool isBinary = false;
            bool isFloat = false;
            bool hasSign = false;
            
            // 检查基数前缀
            char first = Peek();
            if (first == '0' && (Peek(1) == 'x' || Peek(1) == 'X'))
            {
                isHex = true;
                sb.Append("0x");
                Advance();
                Advance();
            }
            else if (first == '0' && (Peek(1) == 'b' || Peek(1) == 'B'))
            {
                isBinary = true;
                sb.Append("0b");
                Advance();
                Advance();
            }
            else if (first == '$')
            {
                isHex = true;
                sb.Append("0x");
                Advance(); // 跳过$
            }
            else if (first == '%')
            {
                isBinary = true;
                sb.Append("0b");
                Advance(); // 跳过%
            }
            else if (first == '-')
            {
                hasSign = true;
                sb.Append('-');
                Advance(); // 跳过-
            }
            
            while (_pos < _source.Length)
            {
                char ch = Peek();
                
                if (isHex)
                {
                    if (char.IsDigit(ch) || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F'))
                    {
                        sb.Append(ch);
                        Advance();
                    }
                    else
                    {
                        break;
                    }
                }
                else if (isBinary)
                {
                    if (ch == '0' || ch == '1')
                    {
                        sb.Append(ch);
                        Advance();
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    if (char.IsDigit(ch))
                    {
                        sb.Append(ch);
                        Advance();
                    }
                    else if (ch == '.' && !isFloat)
                    {
                        isFloat = true;
                        sb.Append(ch);
                        Advance();
                    }
                    else if ((ch == 'e' || ch == 'E') && !isHex && !isBinary)
                    {
                        isFloat = true;
                        sb.Append(ch);
                        Advance();
                        if (Peek() == '+' || Peek() == '-')
                        {
                            sb.Append(Advance());
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            
            string value = sb.ToString();
            if (string.IsNullOrEmpty(value) || (hasSign && value == "-"))
            {
                // 如果只有符号没有数字，可能是减法运算符
                _pos = start;
                _line = startLine;
                _col = startColumn;
                TokenizeSingleChar();
                return;
            }
            
            AddToken(TokenType.NUMBER, value);
        }

        private void TokenizeDotString()
        {
            Advance(); // 跳过.
            Advance(); // 跳过"
            
            int startLine = _line;
            int startColumn = _col;
            StringBuilder sb = new StringBuilder();
            
            while (_pos < _source.Length)
            {
                char ch = Advance();
                
                if (ch == '"')
                {
                    break;
                }
                else if (ch == '\\' && Peek() == '"')
                {
                    // 转义引号
                    sb.Append('"');
                    Advance();
                }
                else if (ch == '\\' && Peek() == '\\')
                {
                    // 转义反斜杠
                    sb.Append('\\');
                    Advance();
                }
                else if (ch == '\\' && Peek() == 'n')
                {
                    // 换行符
                    sb.Append('\n');
                    Advance();
                }
                else if (ch == '\\' && Peek() == 't')
                {
                    // 制表符
                    sb.Append('\t');
                    Advance();
                }
                else if (ch == '\n')
                {
                    // 字符串中的换行，Forth允许多行字符串
                    sb.Append('\n');
                }
                else
                {
                    sb.Append(ch);
                }
            }
            
            AddToken(TokenType.DOTSTRING, sb.ToString());
        }

        private void TokenizeChar()
        {
            Advance(); // 跳过'
            
            char value;
            if (Peek() == '\\')
            {
                Advance(); // 跳过\
                char escaped = Advance();
                value = escaped switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    '0' => '\0',
                    '\'' => '\'',
                    '\\' => '\\',
                    _ => escaped
                };
            }
            else
            {
                value = Advance();
            }
            
            // 跳过结束的'
            if (Peek() == '\'')
            {
                Advance();
            }
            
            AddToken(TokenType.CHARACTER, value.ToString());
        }

        private void TokenizeSString()
        {
            Advance(); // 跳过S
            Advance(); // 跳过"
            
            int startLine = _line;
            int startColumn = _col;
            StringBuilder sb = new StringBuilder();
            
            while (_pos < _source.Length)
            {
                char ch = Advance();
                
                if (ch == '"')
                {
                    break;
                }
                else if (ch == '\\' && Peek() == '"')
                {
                    // 转义引号
                    sb.Append('"');
                    Advance();
                }
                else if (ch == '\\' && Peek() == '\\')
                {
                    // 转义反斜杠
                    sb.Append('\\');
                    Advance();
                }
                else if (ch == '\\' && Peek() == 'n')
                {
                    // 换行符
                    sb.Append('\n');
                    Advance();
                }
                else if (ch == '\\' && Peek() == 't')
                {
                    // 制表符
                    sb.Append('\t');
                    Advance();
                }
                else if (ch == '\n')
                {
                    // 字符串中的换行，Forth允许多行字符串
                    sb.Append('\n');
                }
                else
                {
                    sb.Append(ch);
                }
            }
            
            AddToken(TokenType.STRING, sb.ToString());
        }

        private void TokenizeParenComment()
        {
            Advance(); // 跳过(
            
            int startLine = _line;
            int startColumn = _col;
            StringBuilder sb = new StringBuilder();
            int depth = 1; // 跟踪嵌套的括号
            
            while (_pos < _source.Length && depth > 0)
            {
                char ch = Advance();
                
                if (ch == '(')
                {
                    depth++;
                    sb.Append(ch);
                }
                else if (ch == ')')
                {
                    depth--;
                    if (depth > 0)
                    {
                        sb.Append(ch);
                    }
                }
                else if (ch == '\n')
                {
                    sb.Append('\n');
                }
                else
                {
                    sb.Append(ch);
                }
            }
            
            AddToken(TokenType.COMMENT, sb.ToString());
        }

        private void TokenizeBackslashComment()
        {
            Advance(); // 跳过\

            int startLine = _line;
            int startColumn = _col;
            StringBuilder sb = new StringBuilder();
            int contentStart = _pos;

            while (_pos < _source.Length)
            {
                char ch = Peek();
                if (ch == '\n')
                {
                    break;
                }
                sb.Append(ch);
                Advance();
            }

            string content = sb.ToString().Trim();
            if (content.StartsWith("param "))
                ParseParamDirective(content);
            else
                AddToken(TokenType.COMMENT, sb.ToString());
        }

        private void ParseParamDirective(string content)
        {
            string rest = content.Substring("param".Length).Trim();
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

        /// <summary>
        /// 向前扫描标识符但不推进位置（用于判断数字开头是否构成关键字）
        /// </summary>
        private string? ScanIdentifierAhead()
        {
            int saved = _pos;
            int savedLine = _line;
            int savedColumn = _col;
            var sb = new StringBuilder();
            while (_pos < _source.Length)
            {
                char ch = Peek();
                if (IsIdentifierChar(ch))
                {
                    sb.Append(ch);
                    Advance();
                }
                else break;
            }
            string result = sb.ToString();
            _pos = saved;
            _line = savedLine;
            _col = savedColumn;
            return string.IsNullOrEmpty(result) ? null : result;
        }

        private void TokenizeIdentifier()
        {
            int start = _pos;
            int startLine = _line;
            int startColumn = _col;
            
            StringBuilder sb = new StringBuilder();
            
            while (_pos < _source.Length)
            {
                char ch = Peek();
                if (IsIdentifierChar(ch))
                {
                    sb.Append(ch);
                    Advance();
                }
                else
                {
                    break;
                }
            }
            
            string identifier = sb.ToString();
            
            // 检查是否是关键字（大小写不敏感）
            if (KEYWORDS.TryGetValue(identifier.ToUpperInvariant(), out TokenType type))
            {
                AddToken(type, identifier);
            }
            else
            {
                AddToken(TokenType.IDENTIFIER, identifier);
            }
        }

        private void TokenizeSingleChar()
        {
            char ch = Advance();
            string chStr = ch.ToString();
            
            // 检查单个字符的关键字
            if (KEYWORDS.TryGetValue(chStr, out TokenType type))
            {
                AddToken(type, chStr);
            }
            else
            {
                // 未知字符，作为标识符处理（可能是用户定义的单个字符词）
                AddToken(TokenType.IDENTIFIER, chStr);
            }
        }



        protected override bool IsIdentifierStart(char ch)
        {
            return char.IsLetter(ch) || IsChineseChar(ch) || ch == '_'  || ch == '?' || ch == '!' || ch == '@' || ch == '#' || ch == '$' || ch == '%' || ch == '&' || ch == '*' || ch == '+' || ch == '-' || ch == '/' || ch == '<' || ch == '>' || ch == '=' || ch == '~' || ch == '^' || ch == '|';
        }

        protected override bool IsIdentifierChar(char ch)
        {
            return IsIdentifierStart(ch) || IsChineseChar(ch) || char.IsDigit(ch) || ch == '.' || ch == ':' || ch == ';' || ch == ',' || ch == '[' || ch == ']' || ch == '{' || ch == '}';
        }
    }
}
