using CompilerBase;
using System.Collections.Generic;

namespace BasicCompiler
{
    public partial class Parser : ParserBase<Token, TokenType>
    {
        protected override TokenType GetTokenType(Token token) => token.Type;
        private List<Token> tokens;
        private int current;
        private Dictionary<string, int> _enumValues = new(); // ENUM 成员→值映射 (v1.66.32+)

        /// <summary>当前 BASIC 方言 (v1.66.32+)</summary>
        private VMLPlugins.BasicDialect CurrentDialect =>
            VMLPlugins.CompilerOptionsContext.Current.BasicDialect;

        /// <summary>检查当前方言</summary>
        private bool IsDialect(VMLPlugins.BasicDialect d) => CurrentDialect == d;
        private bool IsAnyDialect(params VMLPlugins.BasicDialect[] ds) => ds.Contains(CurrentDialect);
        private HashSet<string> declaredArrays;
        private bool _pendingStdCall;
        private bool _pendingNative;
        private HashSet<string> declaredFunctions;
        private HashSet<string> declaredSubs;
        private HashSet<string> declaredFnFunctions;
        private Dictionary<string, string> declaredArrayTypes = new Dictionary<string, string>();

        public Parser(List<Token> tokens) : base(tokens)
        {
            this.tokens = tokens;
            current = 0;
            declaredArrays = new HashSet<string>();
            declaredFunctions = new HashSet<string>();
            declaredSubs = new HashSet<string>();
            declaredFnFunctions = new HashSet<string>();
        }

        public BasicProgram Parse()
        {
            // First pass: collect SUB/FUNCTION/DIM/DEF FN declarations
            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Type == TokenType.DIM && i + 1 < tokens.Count && tokens[i + 1].Type == TokenType.IDENTIFIER)
                {
                    string arrName = tokens[i + 1].Value;
                    declaredArrays.Add(arrName);
                    // Scan forward for AS TypeName (handles DIM arr(size) AS TypeName and DIM arr(TO) AS TypeName)
                    int parenDepth = 0;
                    for (int j = i + 2; j < tokens.Count && j < i + 100; j++)
                    {
                        if (tokens[j].Type == TokenType.LPAREN) parenDepth++;
                        else if (tokens[j].Type == TokenType.RPAREN) parenDepth--;
                        else if (parenDepth == 0 && tokens[j].Type == TokenType.AS && j + 1 < tokens.Count && tokens[j + 1].Type == TokenType.IDENTIFIER)
                        {
                            declaredArrayTypes[arrName] = tokens[j + 1].Value;
                            break;
                        }
                        if (parenDepth == 0 && (tokens[j].Type == TokenType.COLON || tokens[j].Type == TokenType.EOF))
                            break;
                    }
                }
                else if (tokens[i].Type == TokenType.DIM && i + 1 < tokens.Count && tokens[i + 1].Type == TokenType.SHARED)
                {
                    // DIM SHARED var, var2(n), var3 AS TYPE — collect following identifiers
                    for (int j = i + 2; j < tokens.Count; j++)
                    {
                        if (tokens[j].Type == TokenType.IDENTIFIER)
                        {
                            string arrName = tokens[j].Value;
                            declaredArrays.Add(arrName);
                            // Scan forward for AS TypeName (handles DIM SHARED arr(size) AS TypeName)
                            int parenDepth = 0;
                            for (int k = j + 1; k < tokens.Count && k < j + 100; k++)
                            {
                                if (tokens[k].Type == TokenType.LPAREN) parenDepth++;
                                else if (tokens[k].Type == TokenType.RPAREN) parenDepth--;
                                else if (parenDepth == 0 && tokens[k].Type == TokenType.AS && k + 1 < tokens.Count && tokens[k + 1].Type == TokenType.IDENTIFIER)
                                {
                                    declaredArrayTypes[arrName] = tokens[k + 1].Value;
                                    break;
                                }
                                if (parenDepth == 0 && (tokens[k].Type == TokenType.COLON || tokens[k].Type == TokenType.EOF))
                                    break;
                            }
                        }
                        if (tokens[j].Type == TokenType.COLON)
                            break;
                    }
                }
                else if (tokens[i].Type == TokenType.SUB && i + 1 < tokens.Count && tokens[i + 1].Type == TokenType.IDENTIFIER)
                {
                    declaredSubs.Add(tokens[i + 1].Value);
                }
                else if (tokens[i].Type == TokenType.FUNCTION && i + 1 < tokens.Count && tokens[i + 1].Type == TokenType.IDENTIFIER)
                {
                    declaredFunctions.Add(tokens[i + 1].Value);
                }
                else if (tokens[i].Type == TokenType.DECLARE && i + 1 < tokens.Count)
                {
                    if (tokens[i + 1].Type == TokenType.SUB && i + 2 < tokens.Count && tokens[i + 2].Type == TokenType.IDENTIFIER)
                    {
                        declaredSubs.Add(tokens[i + 2].Value);
                    }
                    else if (tokens[i + 1].Type == TokenType.FUNCTION && i + 2 < tokens.Count && tokens[i + 2].Type == TokenType.IDENTIFIER)
                    {
                        declaredFunctions.Add(tokens[i + 2].Value);
                    }
                }
                else if (tokens[i].Type == TokenType.ENUM_KW && i + 1 < tokens.Count)
                {
                    // ENUM: 收集成员为隐式常量 (v1.66.32+)
                    int j = i + 1; if (tokens[j].Type == TokenType.IDENTIFIER) j++;
                    int val = 0;
                    while (j < tokens.Count && tokens[j].Type != TokenType.END)
                    {
                        if (tokens[j].Type == TokenType.IDENTIFIER)
                        {
                            string n = tokens[j].Value; j++;
                            if (j < tokens.Count && tokens[j].Type == TokenType.EQUALS)
                                { j++; if (j < tokens.Count && tokens[j].Type == TokenType.NUMBER) { val = int.Parse(tokens[j].Value); j++; } }
                            _enumValues[n] = val++;
                        }
                        else j++;
                    }
                    while (j < tokens.Count && tokens[j].Type != TokenType.ENUM_KW) j++;
                    i = j;
                }
                else if (tokens[i].Type == TokenType.DEF_KW && i + 1 < tokens.Count)
                {
                    if (tokens[i + 1].Type == TokenType.IDENTIFIER)
                    {
                        string ident = tokens[i + 1].Value;
                        if (ident.StartsWith("FN") || ident.StartsWith("fn"))
                        {
                            declaredFunctions.Add(ident);
                            declaredFnFunctions.Add(ident);
                        }
                    }
                    else if (tokens[i + 1].Type == TokenType.FN_KW && i + 2 < tokens.Count && tokens[i + 2].Type == TokenType.IDENTIFIER)
                    {
                        string fnName = "FN" + tokens[i + 2].Value;
                        declaredFunctions.Add(fnName);
                        declaredFnFunctions.Add(fnName);
                    }
                }
            }

            BasicProgram program = new BasicProgram(1, 1);

            while (!AtEnd())
            {
                // 跳过 : 分隔符（BASIC 语句分隔符）
                if (Peek().Type == TokenType.COLON)
                {
                    Advance();
                    continue;
                }
                var stmt = ParseStatement();
                if (stmt != null)
                    program.Statements.Add(stmt);
            }

            // Propagate array type info discovered in first pass
            foreach (var kv in declaredArrayTypes)
                program.ArrayTypes[kv.Key] = kv.Value;
            foreach (var kv in _enumValues)
                program.EnumValues[kv.Key] = kv.Value;

            return program;
        }

        /// <summary>
        /// 跳过当前行剩余的所有 token（到换行或 EOF）
        /// </summary>
        private void SkipToNextLine()
        {
            if (AtEnd()) return;
            int currentLine = Peek().Line;
            while (!AtEnd() && Peek().Line == currentLine)
                Advance();
        }

        private Statement ParseStatement()
        {
            Token token = Peek();

            switch (token.Type)
            {
                case TokenType.PRINT:
                    if (current + 1 < tokens.Count && tokens[current + 1].Type == TokenType.USING_KW)
                        return ParsePrintUsingStatement();
                    return ParsePrintStatement();
                case TokenType.INPUT:
                    return ParseInputStatement();
                case TokenType.LET:
                    return ParseLetStatement();
                case TokenType.IF:
                    return ParseIfStatement();
                case TokenType.GOTO:
                    return ParseGotoStatement();
                case TokenType.GOSUB:
                    return ParseGosubStatement();
                case TokenType.RETURN:
                    return ParseReturnStatement();
                case TokenType.FOR:
                    return ParseForStatement();
                case TokenType.DIM:
                    return ParseDimStatement();
                case TokenType.WHILE:
                    return ParseWhileStatement();
                case TokenType.DO:
                    return ParseDoLoopStatement();
                case TokenType.END:
                    // 处理 END IF/SELECT/SUB — 同时消耗 END 和后续关键字
                    if (current + 1 < tokens.Count)
                    {
                        if (tokens[current + 1].Type == TokenType.IF ||
                            tokens[current + 1].Type == TokenType.SELECT ||
                            tokens[current + 1].Type == TokenType.SUB ||
                            tokens[current + 1].Type == TokenType.FUNCTION ||
                            tokens[current + 1].Type == TokenType.TYPE_KW ||
                            tokens[current + 1].Type == TokenType.CLASS_KW)
                        {
                            Advance(); // skip END
                            Advance(); // skip IF/SELECT/SUB/FUNCTION/TYPE
                            return null;
                        }
                    }
                    return ParseEndStatement();
                case TokenType.STDCALL:
                    _pendingStdCall = true;
                    Advance();
                    return ParseStatement(); // 递归解析下一个语句
                case TokenType.NATIVE:
                    _pendingNative = true;
                    Advance();
                    return ParseStatement(); // 递归解析下一个语句
                case TokenType.SUB:
                    var sub = ParseSubDeclaration();
                    if (sub != null) { sub.IsStdCall = _pendingStdCall; sub.IsNative = _pendingNative; }
                    _pendingStdCall = false;
                    _pendingNative = false;
                    return sub;
                case TokenType.FUNCTION:
                    var func = ParseFunctionDeclaration();
                    if (func != null) { func.IsStdCall = _pendingStdCall; func.IsNative = _pendingNative; }
                    _pendingStdCall = false;
                    _pendingNative = false;
                    return func;
                case TokenType.CALL:
                    return ParseCallStatement();
                case TokenType.EXIT:
                    return ParseExitStatement();
                case TokenType.SELECT:
                    return ParseSelectCaseStatement();
                // DECLARE SUB/FUNCTION — 前向声明
                case TokenType.DECLARE:
                    return ParseDeclareStatement();
                // CONST 常量
                case TokenType.CONST_KW:
                    return ParseConstStatement();
                // TYPE...END TYPE 结构
                case TokenType.TYPE_KW:
                    return ParseTypeDeclaration();
                // CLASS...END CLASS OOP (FreeBasic v1.66.31+)
                case TokenType.CLASS_KW:
                    return ParseClassDeclaration();
                // 文件操作
                case TokenType.OPEN:
                    return ParseOpenStatement();
                case TokenType.CLOSE:
                    return ParseCloseStatement();
                // ON ERROR GOTO
                case TokenType.ON:
                    return ParseOnErrorStatement();
                // DATA 数据语句 — 编译期跳过，运行时由 READ 读取
                case TokenType.DATA:
                    SkipToNextLine();
                    return null;
                // READ/RESTORE/RESUME
                case TokenType.READ_KW:
                    return ParseReadStatement();
                case TokenType.RESTORE:
                    return ParseRestoreStatement();
                case TokenType.RESUME:
                    return ParseResumeStatement();
                // REDIM
                case TokenType.REDIM:
                    return ParseRedimStatement();
                // VIEW [PRINT]
                case TokenType.VIEW:
                    return ParseViewPrintStatement();
                // WINDOW 坐标系统 — 编译器跳过
                case TokenType.WINDOW:
                    SkipToNextLine();
                    return null;
                // DEF: DEF FN / DEF SEG
                case TokenType.DEF_KW:
                    if (current + 1 < tokens.Count)
                    {
                        var nextType = tokens[current + 1].Type;
                        var nextVal = tokens[current + 1].Value.ToUpper();
                        if (nextType == TokenType.FN_KW || (nextType == TokenType.IDENTIFIER && nextVal.StartsWith("FN")))
                            return ParseDefFnStatement();
                        if (nextVal == "SEG")
                            return ParseDefSegStatement();
                    }
                    SkipToNextLine();
                    return null;
                case TokenType.DEFINT:
                case TokenType.DEFSNG:
                case TokenType.DEFSTR:
                case TokenType.DEFDBL:
                case TokenType.DEFLNG:
                    return ParseDefTypeStatement();
                case TokenType.SHARED:
                    return ParseSharedStatement();
                case TokenType.COMMON:
                    return ParseCommonStatement();
                case TokenType.OPTION_KW:
                    return ParseOptionBaseStatement();
                // LPRINT — 打印机输出（编译器跳过）
                case TokenType.LPRINT:
                    SkipToNextLine();
                    return null;
                case TokenType.NUMBER:
                    // 记录 BASIC 行号 (老式行号 BASIC)，供 GOTO/GOSUB 目标标签生成
                    int basicLineNumber = int.Parse(token.Value);
                    Advance();
                    var numberedStmt = ParseStatement(); // 递归解析行号后的语句
                    if (numberedStmt != null)
                        numberedStmt.BasicLineNumber = basicLineNumber;
                    return numberedStmt;
                case TokenType.SCREEN:
                    return ParseScreenStatement();
                case TokenType.CLS_KW:
                    {
                        Token clsToken = Advance(); // consume CLS
                        return new ClsStatement(clsToken.Line, clsToken.Column);
                    }
                case TokenType.SYSTEM:
                    return ParseSystemStatement();
                case TokenType.IDENTIFIER:
                    // Check if it's a line label (identifier followed by ':')
                    if (current + 1 < tokens.Count && tokens[current + 1].Type == TokenType.COLON)
                    {
                        string labelName = token.Value.ToLower();
                        Advance(); // consume identifier
                        Advance(); // consume ':'
                        Statement body = null;
                        if (current < tokens.Count && Peek().Type != TokenType.EOF)
                            body = ParseStatement();
                        return new LabelStatement(token.Line, token.Column, labelName, body);
                    }
                    // Check for implicit SUB call (without CALL keyword) — QBasic allows bare sub name calls
                    if (declaredSubs.Contains(token.Value))
                    {
                        return ParseImplicitCallStatement();
                    }
                    return ParseLetStatement();
                // QBASIC 图形/硬件关键字
                case TokenType.PSET:        return ParsePsetStatement();
                case TokenType.QB_LINE:     return ParseQbLineStatement();
                case TokenType.QB_CIRCLE:   return ParseQbCircleStatement();
                case TokenType.PAINT:       return ParseQbPaintStatement();
                case TokenType.LOCATE:      return ParseLocateStatement();
                case TokenType.QB_COLOR:    return ParseQbColorStatement();
                case TokenType.DRAW_KW:     return ParseDrawStatement();
                case TokenType.QB_WIDTH:    return ParseQbWidthStatement();
                case TokenType.PALETTE_KW:  return ParsePaletteStatement();
                case TokenType.GET_KW:      return ParseGetStatement();
                case TokenType.PUT_KW:      return ParsePutStatement();
                case TokenType.PLAY_KW:     return ParsePlayStatement();
                case TokenType.SOUND_KW:    return ParseSoundStatement();
                case TokenType.RANDOMIZE:   return ParseRandomizeStatement();
                case TokenType.KB_GETCH:    return ParseKbGetChStatement();
                case TokenType.SLEEP:       return ParseSleepStatement();
                case TokenType.SWAP:        return ParseSwapStatement();
                case TokenType.ERASE:       return ParseEraseStatement();
                case TokenType.CHIPASM:     return ParseChipAsmStatement();
                case TokenType.POKE:        return ParsePokeStatement();
                // FreeBasic OOP 扩展 (v1.66.32+)
                case TokenType.ENUM_KW:
                    return ParseEnumStatement();
                case TokenType.PTR_KW:
                case TokenType.CAST_KW:
                case TokenType.EXTENDS_KW:
                case TokenType.OPERATOR_KW:
                // PureBasic 扩展
                case TokenType.PROCEDURE_KW:
                case TokenType.GLOBAL_KW:
                case TokenType.PROTECTED_KW:
                case TokenType.INTERFACE_KW:
                case TokenType.ENDINTERFACE:
                // NEW type → 堆分配 (v1.66.32+)
                case TokenType.NEW_KW:
                    Advance(); // 跳过 NEW
                    if (Peek().Type == TokenType.IDENTIFIER) Advance(); // 跳过类型名
                    return new AllocStatement(token.Line, token.Column);
                // ChipBasic MCU GPIO (v1.66.32+) — 编译为 CALL __gpio_xxx
                case TokenType.PINMODE_KW:
                    return ParseGpioStatement("pinmode");
                case TokenType.DIGITALWRITE:
                    return ParseGpioStatement("digitalwrite");
                case TokenType.DIGITALREAD:
                    return ParseGpioStatement("digitalread");
                // TrueBasic 扩展
                case TokenType.MAT_KW:
                    return ParseMatStatement();
                case TokenType.ZER_KW:
                    Advance(); // 跳过 ZER
                    return new LetStatement(token.Line, token.Column) { Variable = new Identifier(token.Line, token.Column, "__zer"), Expression = new NumberLiteral(token.Line, token.Column, 0) };
                case TokenType.CON_KW:
                    Advance(); // 跳过 CON
                    return null; // CON 作为常量声明，在 CollectVariablesAndLabels 中处理
                // GW-BASIC BLOAD/BSAVE → 跨平台文件I/O库调用 (v1.66.32+)
                case TokenType.BLOAD_KW:
                    return ParseGpioStatement("bload");
                case TokenType.BSAVE_KW:
                    return ParseGpioStatement("bsave");
                case TokenType.KEY_KW:
                case TokenType.DEFSEG:
                // PowerBASIC 扩展
                case TokenType.THREADED:
                case TokenType.REGISTER_KW:
                case TokenType.FASTPROC:
                // VisualBasic 扩展 — Private/Public/Friend 作为修饰符跳过
                case TokenType.PRIVATE_KW:
                case TokenType.PUBLIC_KW:
                case TokenType.FRIEND_KW:
                case TokenType.OPTIONAL_KW:
                case TokenType.PARAMARRAY:
                case TokenType.WITH_KW:
                case TokenType.ENDWITH:
                    Advance(); // 已识别但尚未完全实现
                    return null;
                default:
                    Advance();
                    return null;
            }
        }

        // ===== KB/MOUSE 硬件接口解析 =====

        private Statement ParseKbHitStatement()
        {
            Token token = Advance();
            KbHitStatement stmt = new KbHitStatement(token.Line, token.Column);
            return stmt;
        }

        private Statement ParseKbGetChStatement()
        {
            Token token = Advance();
            KbGetChStatement stmt = new KbGetChStatement(token.Line, token.Column);
            return stmt;
        }

        private Statement ParseMouseGetXStatement()
        {
            Token token = Advance();
            MouseGetXStatement stmt = new MouseGetXStatement(token.Line, token.Column);
            return stmt;
        }

        private Statement ParseMouseGetYStatement()
        {
            Token token = Advance();
            MouseGetYStatement stmt = new MouseGetYStatement(token.Line, token.Column);
            return stmt;
        }

        private Statement ParseMouseLeftStatement()
        {
            Token token = Advance();
            MouseLeftStatement stmt = new MouseLeftStatement(token.Line, token.Column);
            return stmt;
        }

        private Statement ParseMouseRightStatement()
        {
            Token token = Advance();
            MouseRightStatement stmt = new MouseRightStatement(token.Line, token.Column);
            return stmt;
        }
    }
}
