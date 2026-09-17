using System;
using System.Collections.Generic;
using System.Text;
using CompilerBase;

namespace LadderCompiler
{
    /// <summary>
    /// 梯形图词法分析器
    /// 支持IEC 61131-3梯形图语法
    /// </summary>
    public class Lexer : LexerBase
    {
        private readonly List<(string, int)> _lineMap;

        /// <summary>通过 (*$param lib(...)*) 收集的库名列表</summary>
        public List<string> ParamLibraries = new();
        /// <summary>通过 (*$param path(...)*) 收集的路径列表</summary>
        public List<string> ParamPaths = new();
        
        public Lexer(string source, List<(string, int)> lineMap = null) : base(source)
        {
            _lineMap = lineMap ?? new List<(string, int)>();
        }
        
        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();
            
            while (_pos < _source.Length)
            {
                char current = _source[_pos];
                
                // 跳过空白字符
                if (char.IsWhiteSpace(current))
                {
                    if (current == '\n')
                    {
                        tokens.Add(new Token(TokenType.NewLine, "\n", _line, _col));
                        _line++;
                        _col = 1;
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Whitespace, current.ToString(), _line, _col));
                        _col++;
                    }
                    _pos++;
                    continue;
                }
                
                // 注释处理
                if (current == '(' && Peek(1) == '*')
                {
                    tokens.Add(ReadComment());
                    continue;
                }
                
                // 字符串字面量
                if (current == '\'' || current == '"')
                {
                    tokens.Add(ReadString());
                    continue;
                }
                
                // 数字
                if (char.IsDigit(current))
                {
                    tokens.Add(ReadNumber());
                    continue;
                }
                
                // 标识符或关键字
                if (char.IsLetter(current) || IsChineseChar(current) || current == '_')
                {
                    tokens.Add(ReadIdentifier());
                    continue;
                }
                
                // 特殊符号
                switch (current)
                {
                    case '-':
                        if (Peek(1) == '-')
                        {
                            tokens.Add(ReadLadderElement());
                            continue;
                        }
                        tokens.Add(new Token(TokenType.Arithmetic, "-", _line, _col));
                        _pos++;
                        _col++;
                        break;
                        
                    case ':':
                        if (Peek(1) == '=')
                        {
                            tokens.Add(new Token(TokenType.Assignment, ":=", _line, _col));
                            _pos += 2;
                            _col += 2;
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.Colon, ":", _line, _col));
                            _pos++;
                            _col++;
                        }
                        break;
                        
                    case ';':
                        tokens.Add(new Token(TokenType.Semicolon, ";", _line, _col));
                        _pos++;
                        _col++;
                        break;
                        
                    case ',':
                        tokens.Add(new Token(TokenType.Comma, ",", _line, _col));
                        _pos++;
                        _col++;
                        break;
                        
                    case '(':
                        tokens.Add(new Token(TokenType.LeftParenthesis, "(", _line, _col));
                        _pos++;
                        _col++;
                        break;
                        
                    case ')':
                        tokens.Add(new Token(TokenType.RightParenthesis, ")", _line, _col));
                        _pos++;
                        _col++;
                        break;
                        
                    case '[':
                        tokens.Add(new Token(TokenType.LeftBracket, "[", _line, _col));
                        _pos++;
                        _col++;
                        break;
                        
                    case ']':
                        tokens.Add(new Token(TokenType.RightBracket, "]", _line, _col));
                        _pos++;
                        _col++;
                        break;
                        
                    case '.':
                        tokens.Add(new Token(TokenType.Dot, ".", _line, _col));
                        _pos++;
                        _col++;
                        break;
                        
                    case '@':
                        tokens.Add(new Token(TokenType.At, "@", _line, _col));
                        _pos++;
                        _col++;
                        break;
                        
                    case '%':
                        tokens.Add(new Token(TokenType.Percent, "%", _line, _col));
                        _pos++;
                        _col++;
                        break;
                        
                    case '=':
                        tokens.Add(new Token(TokenType.Comparison, "=", _line, _col));
                        _pos++;
                        _col++;
                        break;
                        
                    case '<':
                        if (Peek(1) == '>')
                        {
                            tokens.Add(new Token(TokenType.Comparison, "<>", _line, _col));
                            _pos += 2;
                            _col += 2;
                        }
                        else if (Peek(1) == '=')
                        {
                            tokens.Add(new Token(TokenType.Comparison, "<=", _line, _col));
                            _pos += 2;
                            _col += 2;
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.Comparison, "<", _line, _col));
                            _pos++;
                            _col++;
                        }
                        break;
                        
                    case '>':
                        if (Peek(1) == '=')
                        {
                            tokens.Add(new Token(TokenType.Comparison, ">=", _line, _col));
                            _pos += 2;
                            _col += 2;
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.Comparison, ">", _line, _col));
                            _pos++;
                            _col++;
                        }
                        break;
                        
                    case '+':
                        tokens.Add(new Token(TokenType.Arithmetic, "+", _line, _col));
                        _pos++;
                        _col++;
                        break;
                        
                    case '*':
                        tokens.Add(new Token(TokenType.Arithmetic, "*", _line, _col));
                        _pos++;
                        _col++;
                        break;
                        
                    case '/':
                        tokens.Add(new Token(TokenType.Arithmetic, "/", _line, _col));
                        _pos++;
                        _col++;
                        break;
                        
                    default:
                        // 未知字符
                        tokens.Add(new Token(TokenType.Error, current.ToString(), _line, _col));
                        _pos++;
                        _col++;
                        break;
                }
            }
            
            tokens.Add(new Token(TokenType.EOF, "", _line, _col));
            return tokens;
        }
        
        private Token ReadComment()
        {
            int startLine = _line;
            int startColumn = _col;
            StringBuilder value = new StringBuilder();

            // 读取 "(*"
            value.Append(_source[_pos]);
            value.Append(_source[_pos + 1]);
            _pos += 2;
            _col += 2;

            int contentStart = _pos;

            // ⚠ **支持嵌套 `(* … (* … *) … *)`**（CODESYS / TwinCAT 的 ST 也是这样）：
            //   原来只找**第一个** `*)` 就收尾，于是注释正文里出现一对 `(* … *)` 时
            //   注释会**提前结束**，后面的散文全变成 token ⇒ 报一个与真实原因无关的
            //   「期望 BEGIN 关键字」。这是一个纯词法层面的健壮性修复，不针对任何具体输入。
            int depth = 1;
            while (_pos < _source.Length)
            {
                if (_source[_pos] == '(' && Peek(1) == '*')
                {
                    depth++;
                    value.Append("(*");
                    _pos += 2;
                    _col += 2;
                    continue;
                }
                if (_source[_pos] == '*' && Peek(1) == ')')
                {
                    depth--;
                    value.Append("*)");
                    _pos += 2;
                    _col += 2;
                    if (depth == 0)
                    {
                        string content = _source.Substring(contentStart, _pos - 2 - contentStart).Trim();
                        if (content.StartsWith("$param"))
                            ParseParamDirective(content);
                        break;
                    }
                    continue;
                }

                if (_source[_pos] == '\n')
                {
                    _line++;
                    _col = 1;
                }
                else
                {
                    _col++;
                }

                value.Append(_source[_pos]);
                _pos++;
            }

            return new Token(TokenType.Comment, value.ToString(), startLine, startColumn);
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
        
        private Token ReadString()
        {
            int startLine = _line;
            int startColumn = _col;
            char quote = _source[_pos];
            StringBuilder value = new StringBuilder();
            
            value.Append(quote);
            _pos++;
            _col++;
            
            while (_pos < _source.Length && _source[_pos] != quote)
            {
                if (_source[_pos] == '\\' && _pos + 1 < _source.Length)
                {
                    // 转义字符
                    value.Append(_source[_pos]);
                    value.Append(_source[_pos + 1]);
                    _pos += 2;
                    _col += 2;
                }
                else
                {
                    if (_source[_pos] == '\n')
                    {
                        _line++;
                        _col = 1;
                    }
                    else
                    {
                        _col++;
                    }
                    
                    value.Append(_source[_pos]);
                    _pos++;
                }
            }
            
            if (_pos < _source.Length)
            {
                value.Append(quote);
                _pos++;
                _col++;
            }
            
            return new Token(TokenType.String, value.ToString(), startLine, startColumn);
        }
        
        private new Token ReadNumber()
        {
            int startLine = _line;
            int startColumn = _col;
            StringBuilder value = new StringBuilder();
            
            // 检查是否为十六进制或二进制
            if (_source[_pos] == '0' && _pos + 1 < _source.Length)
            {
                char next = _source[_pos + 1];
                if (next == 'x' || next == 'X')
                {
                    // 十六进制
                    value.Append("0x");
                    _pos += 2;
                    _col += 2;
                    
                    while (_pos < _source.Length && LexerHelper.IsHexDigit(_source[_pos]))
                    {
                        value.Append(_source[_pos]);
                        _pos++;
                        _col++;
                    }
                    
                    return new Token(TokenType.HexNumber, value.ToString(), startLine, startColumn);
                }
                else if (next == 'b' || next == 'B')
                {
                    // 二进制
                    value.Append("0b");
                    _pos += 2;
                    _col += 2;
                    
                    while (_pos < _source.Length && (_source[_pos] == '0' || _source[_pos] == '1'))
                    {
                        value.Append(_source[_pos]);
                        _pos++;
                        _col++;
                    }
                    
                    return new Token(TokenType.BinaryNumber, value.ToString(), startLine, startColumn);
                }
            }
            
            // 十进制数字
            while (_pos < _source.Length && char.IsDigit(_source[_pos]))
            {
                value.Append(_source[_pos]);
                _pos++;
                _col++;
            }
            
            // 检查小数部分
            if (_pos < _source.Length && _source[_pos] == '.')
            {
                value.Append('.');
                _pos++;
                _col++;
                
                while (_pos < _source.Length && char.IsDigit(_source[_pos]))
                {
                    value.Append(_source[_pos]);
                    _pos++;
                    _col++;
                }
            }
            
            return new Token(TokenType.Number, value.ToString(), startLine, startColumn);
        }
        


        private new Token ReadIdentifier()
        {
            int startLine = _line;
            int startColumn = _col;
            StringBuilder value = new StringBuilder();
            
            while (_pos < _source.Length && (char.IsLetterOrDigit(_source[_pos]) || IsChineseChar(_source[_pos]) || _source[_pos] == '_'))
            {
                value.Append(_source[_pos]);
                _pos++;
                _col++;
            }
            
            string identifier = value.ToString();
            TokenType type = GetKeywordTokenType(identifier);
            
            return new Token(type, identifier, startLine, startColumn);
        }
        
        private Token ReadLadderElement()
        {
            int startLine = _line;
            int startColumn = _col;
            StringBuilder value = new StringBuilder();
            
            // 读取 "--"
            value.Append("--");
            _pos += 2;
            _col += 2;
            
            // 读取元素类型
            while (_pos < _source.Length && _source[_pos] != '-')
            {
                value.Append(_source[_pos]);
                _pos++;
                _col++;
            }
            
            // 读取结束的 "--"
            if (_pos + 1 < _source.Length && _source[_pos] == '-' && _source[_pos + 1] == '-')
            {
                value.Append("--");
                _pos += 2;
                _col += 2;
            }
            
            string element = value.ToString();
            TokenType type = GetLadderElementTokenType(element);
            
            return new Token(type, element, startLine, startColumn);
        }
        
        private TokenType GetKeywordTokenType(string keyword)
        {
            return keyword.ToUpper() switch
            {
                "PROGRAM" => TokenType.KeywordProgram,
                "FUNCTION" => TokenType.KeywordFunction,
                "FUNCTION_BLOCK" => TokenType.KeywordFunctionBlock,
                "VAR" => TokenType.KeywordVar,
                "VAR_INPUT" => TokenType.KeywordVarInput,
                "VAR_OUTPUT" => TokenType.KeywordVarOutput,
                "VAR_IN_OUT" => TokenType.KeywordVarInOut,
                "VAR_GLOBAL" => TokenType.KeywordVarGlobal,
                "VAR_EXTERNAL" => TokenType.KeywordVarExternal,
                "VAR_ACCESS" => TokenType.KeywordVarAccess,
                "VAR_TEMP" => TokenType.KeywordVarTemp,
                "END_VAR" => TokenType.KeywordEndVar,
                "BEGIN" => TokenType.KeywordBegin,
                "END_PROGRAM" => TokenType.KeywordEndProgram,
                "END_FUNCTION" => TokenType.KeywordEndFunction,
                "END_FUNCTION_BLOCK" => TokenType.KeywordEndFunctionBlock,
                "IF" => TokenType.KeywordIf,
                "THEN" => TokenType.KeywordThen,
                "ELSIF" => TokenType.KeywordElsif,
                "ELSE" => TokenType.KeywordElse,
                "END_IF" => TokenType.KeywordEndIf,
                "CASE" => TokenType.KeywordCase,
                "OF" => TokenType.KeywordOf,
                "END_CASE" => TokenType.KeywordEndCase,
                "FOR" => TokenType.KeywordFor,
                "TO" => TokenType.KeywordTo,
                "BY" => TokenType.KeywordBy,
                "DO" => TokenType.KeywordDo,
                "END_FOR" => TokenType.KeywordEndFor,
                "WHILE" => TokenType.KeywordWhile,
                "DO_WHILE" => TokenType.KeywordDoWhile,
                "END_WHILE" => TokenType.KeywordEndWhile,
                "REPEAT" => TokenType.KeywordRepeat,
                "UNTIL" => TokenType.KeywordUntil,
                "END_REPEAT" => TokenType.KeywordEndRepeat,
                "RETURN" => TokenType.KeywordReturn,
                "WITH" => TokenType.KeywordWith,
                "AT" => TokenType.KeywordAt,
                "RETAIN" => TokenType.KeywordRetain,
                "NON_RETAIN" => TokenType.KeywordNonRetain,
                "CONSTANT" => TokenType.KeywordConstant,
                "TYPE" => TokenType.KeywordType,
                "END_TYPE" => TokenType.KeywordEndType,
                "STRUCT" => TokenType.KeywordStruct,
                "END_STRUCT" => TokenType.KeywordEndStruct,
                "ENUM" => TokenType.KeywordEnum,
                "END_ENUM" => TokenType.KeywordEndEnum,
                "SUBRANGE" => TokenType.KeywordSubrange,
                "ARRAY" => TokenType.KeywordArray,
                "OF_TYPE" => TokenType.KeywordOfType,
                "BOOL" => TokenType.TypeBool,
                "BYTE" => TokenType.TypeByte,
                "WORD" => TokenType.TypeWord,
                "DWORD" => TokenType.TypeDWord,
                "LWORD" => TokenType.TypeLWord,
                "SINT" => TokenType.TypeSInt,
                "INT" => TokenType.TypeInt,
                "DINT" => TokenType.TypeDInt,
                "LINT" => TokenType.TypeLInt,
                "USINT" => TokenType.TypeUSInt,
                "UINT" => TokenType.TypeUInt,
                "UDINT" => TokenType.TypeUDInt,
                "ULINT" => TokenType.TypeULInt,
                "REAL" => TokenType.TypeReal,
                "LREAL" => TokenType.TypeLReal,
                "TIME" => TokenType.TypeTime,
                "DATE" => TokenType.TypeDate,
                "TIME_OF_DAY" => TokenType.TypeTimeOfDay,
                "DATE_AND_TIME" => TokenType.TypeDateAndTime,
                "STRING" => TokenType.TypeString,
                "WSTRING" => TokenType.TypeWString,
                "TRUE" => TokenType.ValueTrue,
                "FALSE" => TokenType.ValueFalse,
                "NULL" => TokenType.ValueNull,
                "AND" => TokenType.Logical,
                "OR" => TokenType.Logical,
                "XOR" => TokenType.Logical,
                "NOT" => TokenType.Logical,
                "MOD" => TokenType.Arithmetic,
                "LIMIT" => TokenType.FunctionLIMIT,
                "SEL" => TokenType.FunctionSEL,
                "MUX" => TokenType.Identifier,
                "MAX" => TokenType.FunctionMAX,
                "MIN" => TokenType.FunctionMIN,
                "ABS" => TokenType.FunctionABS,
                "SQRT" => TokenType.FunctionSQRT,
                "LN" => TokenType.FunctionLN,
                "LOG" => TokenType.FunctionLOG,
                "EXP" => TokenType.FunctionEXP,
                "SIN" => TokenType.FunctionSIN,
                "COS" => TokenType.FunctionCOS,
                "TAN" => TokenType.FunctionTAN,
                "ASIN" => TokenType.FunctionASIN,
                "ACOS" => TokenType.FunctionACOS,
                "ATAN" => TokenType.FunctionATAN,
                // IL 格式指令关键字
                "TON" => TokenType.FunctionTON,
                "TOF" => TokenType.FunctionTOF,
                "TP" => TokenType.FunctionTP,
                "CTU" => TokenType.FunctionCTU,
                "CTD" => TokenType.FunctionCTD,
                "CTUD" => TokenType.FunctionCTUD,
                "LD" => TokenType.LD,
                "LDI" => TokenType.LDI,
                "ST" => TokenType.ST,
                "OUT" => TokenType.OUT,
                "SET" => TokenType.SET,
                "RST" => TokenType.RST,
                "ANDN" => TokenType.ANDN,
                "ORN" => TokenType.ORN,
                _ => TokenType.Identifier
            };
        }
        
        private TokenType GetLadderElementTokenType(string element)
        {
            return element.ToUpper() switch
            {
                "--| |--" => TokenType.ContactNormallyOpen,
                "--|/|--" => TokenType.ContactNormallyClosed,
                "--( )--" => TokenType.Coil,
                "--(S)--" => TokenType.CoilSet,
                "--(R)--" => TokenType.CoilReset,
                "--(P)--" => TokenType.CoilPositiveTransition,
                "--(N)--" => TokenType.CoilNegativeTransition,
                "--[TON]--" => TokenType.FunctionTON,
                "--[TOF]--" => TokenType.FunctionTOF,
                "--[TP]--" => TokenType.FunctionTP,
                "--[CTU]--" => TokenType.FunctionCTU,
                "--[CTD]--" => TokenType.FunctionCTD,
                "--[CTUD]--" => TokenType.FunctionCTUD,
                "--[ADD]--" => TokenType.FunctionADD,
                "--[SUB]--" => TokenType.FunctionSUB,
                "--[MUL]--" => TokenType.FunctionMUL,
                "--[DIV]--" => TokenType.FunctionDIV,
                "--[MOD]--" => TokenType.FunctionMOD,
                "--[MOVE]--" => TokenType.FunctionMOVE,
                "--[LIMIT]--" => TokenType.FunctionLIMIT,
                "--[SEL]--" => TokenType.FunctionSEL,
                "--[MUX]--" => TokenType.FunctionBlock,
                "--[MAX]--" => TokenType.FunctionMAX,
                "--[MIN]--" => TokenType.FunctionMIN,
                "--[ABS]--" => TokenType.FunctionABS,
                "--[SQRT]--" => TokenType.FunctionSQRT,
                "--[LN]--" => TokenType.FunctionLN,
                "--[LOG]--" => TokenType.FunctionLOG,
                "--[EXP]--" => TokenType.FunctionEXP,
                "--[SIN]--" => TokenType.FunctionSIN,
                "--[COS]--" => TokenType.FunctionCOS,
                "--[TAN]--" => TokenType.FunctionTAN,
                "--[ASIN]--" => TokenType.FunctionASIN,
                "--[ACOS]--" => TokenType.FunctionACOS,
                "--[ATAN]--" => TokenType.FunctionATAN,
                _ => TokenType.FunctionBlock
            };
        }
        
        
        // Uses LexerHelper.IsHexDigit from CompilerBase
    }
}
