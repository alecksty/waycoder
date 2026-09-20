using System.Collections.Generic;
using System.Linq;
using CompilerBase;

namespace FortranCompiler;

public class Parser : ParserBase<Token, TokenType>
{
    protected override TokenType GetTokenType(Token token) => token.Type;

    private bool CheckAhead(int n, TokenType t) => _pos + n < _tokens.Count && EqualityComparer<TokenType>.Default.Equals(GetTokenType(_tokens[_pos + n]), t);

    protected override Token Expect(TokenType t, string msg) =>
        base.Expect(t, $"Fortran 解析错误: {msg}（得到 {Cur.Type}）");

    private ProgramNode _program = null!;

    /// <summary>
    /// 本文件里出现过 `implicit none` —— 从此**变量必须先声明**。
    ///
    /// 它决定未声明变量是「编译错误」还是「只警告」：Fortran 的**默认**语义是隐式类型
    /// （i-n 为 integer、其余 real），没写 `implicit none` 时引用未声明变量是**合法**的。
    /// 由 <see cref="FortranCompiler"/> 在解析完交给 <c>CodeGenerator</c>。
    /// </summary>
    public bool ImplicitNone { get; private set; }

    /// <summary>
    /// 本文件里出现过的函数/子程序名（小写）。只用来消歧：`x(i)` 到底是数组元素
    /// 还是函数调用 —— 名字声明成数组**且**没被声明成例程，才按数组元素编。
    /// 少了后半条，一个「数组名恰好与某个 contained 函数同名」的文件会被静默编错。
    /// </summary>
    private readonly HashSet<string> _declaredRoutines = new();

    /// <summary>`name(i)` 是否应按数组元素处理（见 <see cref="_declaredRoutines"/>）。</summary>
    private bool IsDeclaredArray(string name)
    {
        string key = name.ToLowerInvariant();
        return _program.ArraySizes.ContainsKey(key) && !_declaredRoutines.Contains(key);
    }

    public Parser(List<Token> tokens) : base(tokens) { }

    public ProgramNode Parse()
    {
        SkipNewlines();
        // 写出来的 `program 名` **必须**有 `end program` 收尾 —— 循环跑完没见到就是漏了。
        // ⚠ 没有 `program` 语句的文件是**隐式主程序**（Fortran 本来就这么规定），
        //   那种情况没有"块"要关，不报 —— 判据挂在"写没写 program"上。
        bool hasProgramStmt = Match(TokenType.Program);
        var programTok = hasProgramStmt ? Previous() : Cur;
        bool closed = false;
        if (hasProgramStmt)
        {
            string name = Expect(TokenType.Identifier, "期望程序名").Value;
            _program = new ProgramNode(name);
            SkipNewlines();
        }
        else
        {
            _program = new ProgramNode("main");
        }
        var prog = _program;

        // Parse top-level: declarations, contains, subroutines/functions, and statements
        bool inContains = false;
        while (_pos < _tokens.Count - 1)
        {
            SkipNewlines();
            if (Check(TokenType.EOF)) break;
            if (Check(TokenType.End))
            {
                Advance(); // end
                if (Match(TokenType.Program))
                {
                    if (!inContains) Match(TokenType.Identifier); // optional program name
                    closed = true;
                    break;
                }
                // end subroutine/function inside contains
                throw Error($"意外的 'end'（位置 {Cur.Line}:{Cur.Column}，没有与之匹配的块）");
            }
            if (Match(TokenType.Contains))
            {
                inContains = true;
                SkipNewlines();
                continue;
            }
            if (Match(TokenType.Module))
            {
                prog.Statements.Add(ParseModule());
                continue;
            }
            if (Match(TokenType.Use))
            {
                string modName = Expect(TokenType.Identifier, "期望模块名在 use 后").Value.ToString();
                // 跳过 use 的可选项: , only: name1, name2, ...
                // 终止条件: 换行/分号(语句分隔符)/EOF。
                // 注意: Check(EOF) 在 EOF 哨兵处因 IsAtEnd 恒为 false, 须用 !IsAtEnd 终止 (否则分号分隔源码会死循环)
                while (!Check(TokenType.Newline) && !Check(TokenType.Semicolon) && !IsAtEnd) Advance();
                prog.Statements.Add(new UseNode(modName, Cur.Line, Cur.Column));
                continue;
            }
            if (Match(TokenType.Subroutine))
            {
                prog.Statements.Add(ParseSubroutine());
                continue;
            }
            // "integer function" / "real function" — return type before function keyword
            if (IsTypeKeyword(Cur.Type) && _pos + 1 < _tokens.Count && _tokens[_pos + 1].Type == TokenType.Function)
            {
                prog.Statements.Add(ParseFunction());
                continue;
            }
            if (Match(TokenType.Function))
            {
                prog.Statements.Add(ParseFunction());
                continue;
            }

            prog.Statements.Add(ParseStatement());
        }
        // ⚠ 循环**只在 `end program` 或 EOF 处退出**，而 EOF 这一支从前**一声不吭**：
        //   `program p` + 几行语句、不写 `end program p` ⇒ 编译成功、退出码 0。
        //   位置锚在 `program` 那个词上（缺口就是它没被关上）。
        if (hasProgramStmt && !closed)
            GccErrorAt("程序块未闭合（缺少 'end program'）", programTok, ErrorCode.Parser_SyntaxError);
        return prog;
    }

    // ---- Module ----

    private ModuleNode ParseModule()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // module
        string name = Expect(TokenType.Identifier, "期望模块名").Value;
        var body = new List<ASTNode>();

        while (!Check(TokenType.EOF))
        {
            SkipNewlines();
            if (Match(TokenType.End))
            {
                Advance(); // end
                Match(TokenType.Module);
                Match(TokenType.Identifier); // optional name
                break;
            }
            if (Match(TokenType.Contains))
            {
                SkipNewlines();
                continue;
            }
            if (Match(TokenType.Use))
            {
                string modName = Expect(TokenType.Identifier, "期望模块名").Value;
                while (!Check(TokenType.Newline) && !Check(TokenType.Semicolon) && !IsAtEnd) Advance();
                body.Add(new UseNode(modName, Cur.Line, Cur.Column));
                continue;
            }
            if (Match(TokenType.Subroutine))
                body.Add(ParseSubroutine());
            else if (Match(TokenType.Function))
                body.Add(ParseFunction());
            else
                Advance(); // skip declarations
        }
        return new ModuleNode(name, body, l, c);
    }

    // ---- Routines ----

    private SubroutineNode ParseSubroutine()
    {
        int l = Cur.Line, c = Cur.Column;
        string name = Expect(TokenType.Identifier, "期望子程序名").Value;
        _declaredRoutines.Add(name.ToLowerInvariant());
        var parms = ParseParameterList();
        SkipNewlines();
        var body = new List<ASTNode>();
        while (!Check(TokenType.End) && !Check(TokenType.EOF))
        {
            SkipNewlines();
            if (Check(TokenType.End) || Check(TokenType.EOF) || Check(TokenType.Contains)) break;
            body.Add(ParseStatement());
        }
        Expect(TokenType.End, "期望 'end' 用于结束 subroutine");
        if (Match(TokenType.Subroutine)) Match(TokenType.Identifier); // optional name
        return new SubroutineNode(name, parms, body, l, c);
    }

    private FunctionNode ParseFunction()
    {
        int l = Cur.Line, c = Cur.Column;
        // Consume optional return type keyword before "function" (e.g., "integer function sq2(x)")
        string returnType = "integer"; // default
        if (IsTypeKeyword(Cur.Type))
        {
            returnType = Advance().Value.ToLowerInvariant();
            // Consume "function" keyword
            if (Cur.Type == TokenType.Function)
                Advance();
        }
        string name = Expect(TokenType.Identifier, "期望函数名").Value;
        // 记的是**被调用的那个名字** —— `name` 全程不再被改写成 result 变量名
        _declaredRoutines.Add(name.ToLowerInvariant());
        var parms = ParseParameterList();

        // result(resultVar) 子句 —— "result" 被词法器当普通标识符
        string resultVar = name; // 没有 result 子句时返回值就存在函数名里（`f = expr`）
        if (Check(TokenType.Identifier) && Cur.Value.Equals("result", System.StringComparison.OrdinalIgnoreCase))
        {
            Advance(); // consume "result"
            Expect(TokenType.LParen, "期望 ( 在 result 后");
            resultVar = Expect(TokenType.Identifier, "期望 result 名").Value;
            Expect(TokenType.RParen, "expected )");
        }

        SkipNewlines();

        // Check for return type declaration before body
        // "integer :: function_name" or "real :: function_name"
        returnType = ParseReturnTypeFromDecl(returnType, resultVar);

        var body = new List<ASTNode>();
        while (!Check(TokenType.End) && !Check(TokenType.EOF))
        {
            SkipNewlines();
            if (Check(TokenType.End) || Check(TokenType.EOF) || Check(TokenType.Contains)) break;
            body.Add(ParseStatement());
        }
        Expect(TokenType.End, "期望 'end' 用于结束 function");
        if (Match(TokenType.Function)) Match(TokenType.Identifier); // optional name
        return new FunctionNode(name, returnType, parms, body, l, c) { ResultVar = resultVar };
    }

    /// <summary>
    /// If the next tokens are "type :: name", consume them and return the type.
    /// This handles the function return type declaration.
    /// </summary>
    private string ParseReturnTypeFromDecl(string defaultType, string funcName)
    {
        if (IsTypeKeyword(Cur.Type))
        {
            int saved = _pos;
            string typeName = Advance().Value.ToLowerInvariant();
            bool hadColonColon = Match(TokenType.ColonColon);
            if (hadColonColon)
            {
                // Only consume if it's followed by the function name
                if (Peek().Value.Equals(funcName, System.StringComparison.OrdinalIgnoreCase))
                    return MapTypeName(typeName);
                // Otherwise backtrack — it's a variable declaration
                _pos = saved;
            }
            else if (Peek().Value.Equals(funcName, System.StringComparison.OrdinalIgnoreCase))
            {
                // "integer function_name" without ::
                Advance(); // consume function name
                return MapTypeName(typeName);
            }
            else
            {
                // Not for this function — backtrack
                _pos = saved;
            }
        }
        return defaultType;
    }

    private string MapTypeName(string name) => name.ToLowerInvariant() switch
    {
        "integer" => "integer",
        "real" => "real",
        "double" => "real",
        "complex" => "complex",
        "logical" => "logical",
        "character" => "character",
        "doubleprecision" => "real",
        _ => "integer",
    };

    private List<string> ParseParameterList()
    {
        var parms = new List<string>();
        if (Match(TokenType.LParen))
        {
            if (!Check(TokenType.RParen))
            {
                do
                {
                    parms.Add(Expect(TokenType.Identifier, "期望参数名").Value);
                } while (Match(TokenType.Comma));
            }
            Expect(TokenType.RParen, "expected )");
        }
        return parms;
    }

    private bool IsArraySectionExpr(ASTNode node) =>
        node is ArraySectionNode || (node is VarNode);

    // ---- Statements ----

    private ASTNode ParseStatement()
    {
        SkipNewlines();
        if (Check(TokenType.EOF)) return new ReturnNode(Cur.Line, Cur.Column);

        if (Match(TokenType.Implicit))
        {
            // `implicit none` —— 从此**所有变量必须先声明**。
            //
            // ⚠ 此前这里只是把它解析掉就丢（`NopNode`），**语义完全没生效**：
            //   写了 `implicit none` 的程序里引用未声明变量，代码生成照样静默按 0。
            //   现在落一个标志位，由 `CodeGenerator` 决定"报错"还是"只警告"——
            //   Fortran 的**默认**语义是隐式类型（i-n 为 integer、其余 real），
            //   所以没有 `implicit none` 时未声明是**合法**的，不能一刀切报错
            //   （用户原话：「根据语言特性来定」）。
            int l = Cur.Line, c = Cur.Column;
            Expect(TokenType.None, "期望 'none' 在 implicit 后");
            ImplicitNone = true;
            return new NopNode(l, c);
        }
        if (Match(TokenType.If)) return ParseIf();
        if (Match(TokenType.Do)) return ParseDo();
        if (Match(TokenType.Call)) return ParseCall();
        if (Match(TokenType.Return)) return new ReturnNode(_tokens[_pos - 1].Line, _tokens[_pos - 1].Column);
        if (Match(TokenType.Stop))
        {
            ASTNode? code = null;
            if (!Check(TokenType.Newline) && !Check(TokenType.EOF) && !Check(TokenType.Semicolon))
                code = ParseExpression();
            return new StopNode(code, _tokens[_pos - 1].Line, _tokens[_pos - 1].Column);
        }
        if (Match(TokenType.Print) || Match(TokenType.Write))
        {
            return ParsePrint();
        }
        if (Match(TokenType.Read))
        {
            return ParseRead();
        }
        if (Match(TokenType.Exit))
        {
            return new ExitNode(_tokens[_pos - 1].Line, _tokens[_pos - 1].Column);
        }
        if (Match(TokenType.Cycle))
        {
            return new CycleNode(_tokens[_pos - 1].Line, _tokens[_pos - 1].Column);
        }
        if (Match(TokenType.Allocate))
        {
            int al = _tokens[_pos - 1].Line, ac = _tokens[_pos - 1].Column;
            Expect(TokenType.LParen, "期望 '(' 在 allocate 后");
            string arrName = Expect(TokenType.Identifier, "期望数组名").Value;
            Expect(TokenType.LParen, "期望 '(' 用于尺寸");
            var sizeExpr = ParseExpression();
            Expect(TokenType.RParen, "期望 ')' 在尺寸后");
            Expect(TokenType.RParen, "期望 ')' 在 allocate 后");
            return new AllocateNode(arrName, sizeExpr, al, ac);
        }
        if (Match(TokenType.Deallocate))
        {
            int dl = _tokens[_pos - 1].Line, dc = _tokens[_pos - 1].Column;
            Expect(TokenType.LParen, "期望 '(' 在 deallocate 后");
            string arrName = Expect(TokenType.Identifier, "期望数组名").Value;
            Expect(TokenType.RParen, "期望 ')' 在 deallocate 后");
            return new DeallocateNode(arrName, dl, dc);
        }

        // Variable declaration: type [::] name [, name]*
        if (IsTypeKeyword(Cur.Type))
        {
            return ParseVarDecl();
        }

        // Assignment or expression statement
        return ParseExpressionStatement();
    }

    private bool IsTypeKeyword(TokenType t) => t is TokenType.Integer or TokenType.Real or TokenType.Double or TokenType.Complex or TokenType.Logical or TokenType.Character;

    private ASTNode ParseVarDecl()
    {
        int l = Cur.Line, c = Cur.Column;
        string typeName = Advance().Value.ToLowerInvariant();
        // Fortran 复合类型: double precision
        if (typeName == "double" && Cur.Type == TokenType.Identifier && Cur.Value.ToLowerInvariant() == "precision")
        {
            typeName = "double precision";
            Advance(); // consume 'precision'
        }
        // Fortran kind-selector: integer*4, integer*8, real*4, real*8
        if (Match(TokenType.Asterisk))
        {
            string kindStr = Expect(TokenType.IntLiteral, "期望 kind 数字在 * 后").Value;
            if (int.TryParse(kindStr, out int kind) && kind == 8)
            {
                typeName = typeName switch
                {
                    "integer" => "long",
                    "real" => "double precision",
                    _ => typeName
                };
            }
        }
        // 跳过类型后的可选属性: integer, intent(in) :: name
        while (Match(TokenType.Comma))
        {
            // 跳过属性关键字或括号内参数: intent(in), parameter, dimension(n)
            while (!Check(TokenType.Comma) && !Check(TokenType.ColonColon) && !Check(TokenType.EOF))
            {
                if (Match(TokenType.LParen))
                {
                    int depth = 1;
                    while (depth > 0 && !Check(TokenType.EOF))
                    {
                        if (Check(TokenType.LParen)) depth++;
                        else if (Check(TokenType.RParen)) depth--;
                        Advance();
                    }
                }
                else
                {
                    Advance(); // 属性名或关键字 (intent, parameter, dimension, etc.)
                }
            }
        }
        // Skip :: if present
        Match(TokenType.ColonColon);

        var names = new List<string>();
        // Parse comma-separated names (possibly with dimension specs like a(10))
        do
        {
            string name = Expect(TokenType.Identifier, "期望变量名").Value;
            // Parse dimension spec: a(10)
            if (Match(TokenType.LParen))
            {
                // Extract array size from dimension expression
                if (Check(TokenType.IntLiteral))
                {
                    // 键统一小写 —— 代码生成侧（EmitArrayBaseAddress / GenerateArrayAssign）
                    // 一律按小写查，大写声明（`integer :: A(4)`）否则查不到、数组退化成标量。
                    if (int.TryParse(Cur.Value, out int arrSize) && arrSize > 0)
                        _program.ArraySizes[name.ToLowerInvariant()] = arrSize;
                }
                // Consume the rest of the dimension expression
                int depth = 1;
                while (depth > 0 && !Check(TokenType.EOF))
                {
                    if (Check(TokenType.LParen)) depth++;
                    else if (Check(TokenType.RParen)) depth--;
                    Advance();
                }
                names.Add(name);
            }
            else
            {
                names.Add(name);
            }
        } while (Match(TokenType.Comma));

        return new VarDeclNode(typeName, names, l, c);
    }

    private ASTNode ParseIf()
    {
        int l = _tokens[_pos - 1].Line, c = _tokens[_pos - 1].Column;
        Expect(TokenType.LParen, "期望 ( 在 if 后");
        var cond = ParseExpression();
        Expect(TokenType.RParen, "expected )");

        // 逻辑 IF: if (expr) statement (没有 then, 单条语句)
        if (!Check(TokenType.Then))
        {
            var singleStmt = ParseStatement();
            return new IfNode(cond, new List<ASTNode> { singleStmt }, null, l, c);
        }

        Advance(); // consume 'then'

        var thenBody = new List<ASTNode>();
        // Parse until else, else if, or end if
        while (!Check(TokenType.EOF))
        {
            SkipNewlines();
            if (Check(TokenType.Else) || Check(TokenType.End)) break;
            // "else if" is two tokens
            if (Check(TokenType.Else) && CheckAhead(1, TokenType.If)) break;
            thenBody.Add(ParseStatement());
        }

        // 支持链式 else-if 和最终 else
        var remaining = ParseElseIfChain(l, c);
        List<ASTNode>? elseBody = remaining;

        Expect(TokenType.End, "期望 'end' 用于结束 if");
        Expect(TokenType.If, "expected 'if' after 'end'");
        return new IfNode(cond, thenBody, elseBody, l, c);
    }

    /// 递归解析 else-if 链和最终 else，返回嵌套 IfNode 的 else 体列表（单个元素=嵌套 if）
    private List<ASTNode>? ParseElseIfChain(int l, int c)
    {
        if (!Match(TokenType.Else))
            return null;

        // else if (cond) then ... — 递归处理链式
        if (Match(TokenType.If))
        {
            Expect(TokenType.LParen, "expected (");
            var elifCond = ParseExpression();
            Expect(TokenType.RParen, "expected )");
            Expect(TokenType.Then, "expected 'then'");
            var elifBody = new List<ASTNode>();
            while (!Check(TokenType.EOF))
            {
                SkipNewlines();
                if (Check(TokenType.Else) || Check(TokenType.End)) break;
                elifBody.Add(ParseStatement());
            }
            // 递归解析后续的 else-if / else 链
            var remainingElse = ParseElseIfChain(l, c);
            return new List<ASTNode>
            {
                new IfNode(elifCond, elifBody, remainingElse, l, c)
            };
        }
        else
        {
            // 纯 else 块 — 链的末端
            var body = new List<ASTNode>();
            while (!Check(TokenType.EOF) && !Check(TokenType.End))
            {
                SkipNewlines();
                if (Check(TokenType.End)) break;
                body.Add(ParseStatement());
            }
            return body;
        }
    }

    private ASTNode ParseDo()
    {
        int l = _tokens[_pos - 1].Line, c = _tokens[_pos - 1].Column;

        // do while (cond)
        if (Match(TokenType.While))
        {
            Expect(TokenType.LParen, "expected (");
            var cond = ParseExpression();
            Expect(TokenType.RParen, "expected )");
            SkipNewlines();
            var body = ParseBlockUntil("end do");
            return new DoWhileNode(cond, body, l, c);
        }

        // do var = start, end [, step]
        string varName = Expect(TokenType.Identifier, "期望循环变量").Value;
        Expect(TokenType.Assign, "expected =");
        var start = ParseExpression();
        Expect(TokenType.Comma, "expected ,");
        var end = ParseExpression();
        ASTNode? step = null;
        if (Match(TokenType.Comma))
        {
            step = ParseExpression();
        }
        SkipNewlines();
        var doBody = ParseBlockUntil("end do");
        return new ForNode(varName, start, end, step, doBody, l, c);
    }

    private ASTNode ParseCall()
    {
        int l = _tokens[_pos - 1].Line, c = _tokens[_pos - 1].Column;
        string name = Expect(TokenType.Identifier, "期望子程序名").Value;
        var args = new List<ASTNode>();
        if (Match(TokenType.LParen))
        {
            if (!Check(TokenType.RParen))
            {
                do
                {
                    args.Add(ParseExpression());
                } while (Match(TokenType.Comma));
            }
            Expect(TokenType.RParen, "expected )");
        }
        return new CallNode(name, args, l, c);
    }

    private ASTNode ParsePrint()
    {
        int l = _tokens[_pos - 1].Line, c = _tokens[_pos - 1].Column;
        // Handle write(*,*) or print * format specifier
        if (Match(TokenType.LParen))
        {
            // write(*,*) - consume format specifier
            Expect(TokenType.Asterisk, "期望 * 在格式说明符中");
            Match(TokenType.Comma);
            Expect(TokenType.Asterisk, "期望 * 在格式说明符中");
            Expect(TokenType.RParen, "expected )");
        }
        else
        {
            Expect(TokenType.Asterisk, "期望 * 在 print 后");
        }
        var exprs = new List<ASTNode>();
        while (!Check(TokenType.Newline) && !Check(TokenType.EOF) && !Check(TokenType.End) && !Check(TokenType.Semicolon))
        {
            Match(TokenType.Comma);
            if (Check(TokenType.Newline) || Check(TokenType.EOF) || Check(TokenType.End) || Check(TokenType.Semicolon)) break;
            exprs.Add(ParseExpression());
        }
        return new PrintNode(exprs, l, c);
    }

    private ASTNode ParseRead()
    {
        int l = _tokens[_pos - 1].Line, c = _tokens[_pos - 1].Column;
        // Handle read(*,*) format specifier
        if (Match(TokenType.LParen))
        {
            Expect(TokenType.Asterisk, "期望 * 在格式说明符中");
            Match(TokenType.Comma);
            Expect(TokenType.Asterisk, "期望 * 在格式说明符中");
            Expect(TokenType.RParen, "expected )");
        }
        var vars = new List<ASTNode>();
        while (!Check(TokenType.Newline) && !Check(TokenType.EOF) && !Check(TokenType.End) && !Check(TokenType.Semicolon))
        {
            Match(TokenType.Comma);
            if (Check(TokenType.Newline) || Check(TokenType.EOF) || Check(TokenType.End) || Check(TokenType.Semicolon)) break;
            // read target must be a variable
            string varName = Expect(TokenType.Identifier, "期望变量名").Value;
            vars.Add(new VarNode(varName, l, c));
        }
        return new PrintNode(vars, l, c, isRead: true); // reuse PrintNode for read with IsRead flag
    }

    private ASTNode ParseExpressionStatement()
    {
        if (Check(TokenType.Identifier) || Check(TokenType.LParen) || Check(TokenType.Plus)
            || Check(TokenType.Minus) || Check(TokenType.Asterisk) || Check(TokenType.Dot)
            || Check(TokenType.IntLiteral) || Check(TokenType.RealLiteral) || Check(TokenType.DoubleLiteral)
            || Check(TokenType.StringLiteral) || Check(TokenType.True) || Check(TokenType.False))
        {
            var expr = ParseExpression();
            return expr;
        }
        throw Error($"语句中意外的 token: {Cur.Type}({Cur.Value})（位置 {Cur.Line}:{Cur.Column}）");
    }

    private List<ASTNode> ParseBlockUntil(string endMarker)
    {
        var body = new List<ASTNode>();
        while (!Check(TokenType.EOF))
        {
            SkipNewlines();
            if (Check(TokenType.End))
            {
                int saved = _pos;
                Advance();
                if (Match(TokenType.Do))
                {
                    break;
                }
                // Reset - end without 'do' is not our end marker
                _pos = saved;
            }
            if (Check(TokenType.EOF) || Check(TokenType.Contains)) break;
            body.Add(ParseStatement());
        }
        return body;
    }

    // ---- Expressions (standard precedence) ----

    private ASTNode ParseExpression() => ParseLogicalOr();

    private ASTNode ParseLogicalOr()
    {
        var left = ParseLogicalEquiv();
        while (Match(TokenType.LogicalOr))
        {
            int l = left.Line, c = left.Column;
            var right = ParseLogicalEquiv();
            left = new BinaryNode(left, ".or.", right, l, c);
        }
        return left;
    }

    private ASTNode ParseLogicalEquiv()
    {
        var left = ParseLogicalAnd();
        while (Check(TokenType.LogicalEqv) || Check(TokenType.LogicalNeqv))
        {
            string op = Advance().Value; // .eqv. or .neqv.
            var right = ParseLogicalAnd();
            left = new BinaryNode(left, op, right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseLogicalAnd()
    {
        var left = ParseComparison();
        while (Match(TokenType.LogicalAnd))
        {
            int l = left.Line, c = left.Column;
            var right = ParseComparison();
            left = new BinaryNode(left, ".and.", right, l, c);
        }
        return left;
    }

    private ASTNode ParseComparison()
    {
        var left = ParseAdditive();
        while (Check(TokenType.Eq) || Check(TokenType.Neq) || Check(TokenType.Lt)
            || Check(TokenType.Gt) || Check(TokenType.Le) || Check(TokenType.Ge))
        {
            string op = Advance().Value;
            var right = ParseAdditive();
            left = new BinaryNode(left, op, right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseAdditive()
    {
        var left = ParseMultiplicative();
        while (Check(TokenType.Plus) || Check(TokenType.Minus))
        {
            string op = Advance().Value;
            var right = ParseMultiplicative();
            left = new BinaryNode(left, op, right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseMultiplicative()
    {
        var left = ParseUnary();
        while (Check(TokenType.Asterisk) || Check(TokenType.Div) || Check(TokenType.Pow))
        {
            string op = Advance().Value;
            var right = ParseUnary();
            left = new BinaryNode(left, op, right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseUnary()
    {
        int l = Cur.Line, c = Cur.Column;
        if (Match(TokenType.Minus))
            return new UnaryNode("-", ParseUnary(), l, c);
        if (Match(TokenType.Plus))
            return new UnaryNode("+", ParseUnary(), l, c);
        if (Match(TokenType.LogicalNot))
            return new UnaryNode(".not.", ParseUnary(), l, c);
        return ParsePrimary();
    }

    private ASTNode ParsePrimary()
    {
        int l = Cur.Line, c = Cur.Column;

        if (Check(TokenType.IntLiteral))
        {
            var t = Advance();
            int.TryParse(t.Value, out int iv);
            return new LiteralNode(iv, "integer", l, c);
        }
        if (Check(TokenType.RealLiteral))
        {
            var t = Advance();
            double.TryParse(t.Value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double fv);
            return new LiteralNode((float)fv, "real", l, c);
        }
        if (Check(TokenType.DoubleLiteral))
        {
            var t = Advance();
            double.TryParse(t.Value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double dv);
            return new LiteralNode(dv, "double", l, c);
        }
        if (Check(TokenType.StringLiteral))
        {
            var t = Advance();
            return new LiteralNode(t.Value, "character", l, c);
        }
        if (Match(TokenType.True))
            return new LiteralNode(1, "logical", l, c);
        if (Match(TokenType.False))
            return new LiteralNode(0, "logical", l, c);

        if (Check(TokenType.Identifier))
        {
            var name = Advance().Value;
            // Check for function call: name(args)
            if (Match(TokenType.LParen))
            {
                var args = new List<ASTNode>();
                bool isArraySection = false;
                if (!Check(TokenType.RParen))
                {
                    do
                    {
                        // Array section: a(:) or a(1:n) — detect colon operator
                        ASTNode arg;
                        if (Check(TokenType.Colon))
                        {
                            // Whole array: a(:) — lower=null, upper=null
                            isArraySection = true;
                            Advance();
                            ASTNode? upper = null;
                            if (!Check(TokenType.RParen) && !Check(TokenType.Comma))
                                upper = ParseExpression();
                            args.Add(new ArraySectionNode(name, null, upper, l, c));
                        }
                        else
                        {
                            arg = ParseExpression();
                            if (Check(TokenType.Colon))
                            {
                                isArraySection = true;
                                Advance(); // skip ':'
                                ASTNode? upper = null;
                                if (!Check(TokenType.RParen) && !Check(TokenType.Comma))
                                    upper = ParseExpression();
                                args.Add(new ArraySectionNode(name, arg, upper, l, c));
                            }
                            else
                            {
                                args.Add(arg);
                            }
                        }
                    } while (Match(TokenType.Comma));
                }
                Expect(TokenType.RParen, "expected )");
                // 数组元素赋值: a(1) = 10
                if (Check(TokenType.Assign))
                {
                    Advance(); // =
                    var value = ParseExpression();
                    if (isArraySection)
                    {
                        // Array slice assignment: a(:) = expr  or  a(:) = b(:) + c(:)
                        if (value is BinaryNode bn && IsArraySectionExpr(bn.Left) && IsArraySectionExpr(bn.Right))
                            return new ArrayAssignNode(name, args.Count > 0 ? args[0] : null, bn.Op, bn.Left, bn.Right, l, c);
                        return new AssignNode(name, value, l, c) { ArrayIndices = args, IsArraySection = true };
                    }
                    return new AssignNode(name, value, l, c) { ArrayIndices = args };
                }
                if (isArraySection)
                    return new ArraySectionNode(name, args.Count > 0 ? args[0] : null, null, l, c);
                // 数组标量元素读 `a(i)`：前端原先一律当函数调用，编出 `CALL func_a`
                // —— 链接期「未找到标签: func_a」、运行期 KeyNotFound（Examples/fortran/sorting
                // 的 `arr(j)` 就是这么坏的）。Fortran 没有 C 那样的「调用才加括号」线索，
                // 判据只能是「这个名字声明成了数组」。
                if (args.Count == 1 && IsDeclaredArray(name))
                    return new ArrayElemNode(name, args[0], l, c);
                return new FuncCallNode(name, args, l, c);
            }
            // Assignment detection: name = expr
            if (Check(TokenType.Assign))
            {
                Advance(); // =
                var value = ParseExpression();
                return new AssignNode(name, value, l, c);
            }
            return new VarNode(name, l, c);
        }

        if (Match(TokenType.LParen))
        {
            var expr = ParseExpression();
            Expect(TokenType.RParen, "expected )");
            return expr;
        }

        throw Error($"意外的 token: {Cur.Type}({Cur.Value})（位置 {Cur.Line}:{Cur.Column}）");
    }

    // ---- Helpers ----

    private void SkipNewlines()
    {
        while (Check(TokenType.Newline) || Check(TokenType.Semicolon)) Advance();
    }
}
