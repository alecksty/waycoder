using System.Collections.Generic;
using CompilerBase;

namespace DartCompiler;

public class Parser : ParserBase<Token, TokenType>
{
    /// <summary>多变量声明中额外变量的暂存列表</summary>
    private List<ASTNode> _extraDeclarations = new();
    private bool _pendingExternal = false;

    protected override TokenType GetTokenType(Token token) => token.Type;

    protected override Token Expect(TokenType t, string msg) => base.Expect(t, $"Dart parse error: {msg} (got {Cur.Type})");

    public Parser(List<Token> tokens) : base(tokens) { }

    public ProgramNode Parse()
    {
        var prog = new ProgramNode();
        while (!IsAtEnd)
        {
            if (Check(TokenType.EOF)) break;
            prog.Statements.Add(ParseTopLevel());
        }
        return prog;
    }

    private ASTNode ParseTopLevel()
    {
        if (Match(TokenType.Enum))
        {
            string name = Expect(TokenType.Identifier, "expected enum name").Value;
            Expect(TokenType.LBrace, "expected '{'");
            while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
            {
                if (Check(TokenType.Identifier)) Advance();
                Match(TokenType.Comma);
            }
            Expect(TokenType.RBrace, "expected '}'");
            return new LiteralNode(0, Cur.Line, Cur.Column);
        }
        if (Check(TokenType.Mixin))
            return ParseMixinDecl();
        if (Check(TokenType.Extension))
            return ParseExtensionDecl();
        if (Match(TokenType.External)) { _pendingExternal = true; }
        if (Check(TokenType.Class))
            return ParseClassDecl();
        if (IsType(Cur) && Peek(1).Type == TokenType.Identifier && Peek(2).Type == TokenType.LParen)
            return ParseMethodOrFunction();
        return ParseStatement();
    }

    // ---- Statement parsing ----

    private ASTNode ParseStatement()
    {
        if (Check(TokenType.If)) return ParseIf();
        if (Check(TokenType.While)) return ParseWhile();
        if (Check(TokenType.For)) return ParseFor();
        if (Check(TokenType.DoKw)) return ParseDoWhile();
        if (Check(TokenType.TryKw)) return ParseTryCatch();
        if (Check(TokenType.ThrowKw)) return ParseThrow();
        if (Check(TokenType.Return)) return ParseReturn();
        if (Check(TokenType.LBrace)) return ParseBlock();
        if (Match(TokenType.Break))
        {
            int l = Cur.Line, c = Cur.Column;
            Expect(TokenType.Semicolon, "expected ';' after break");
            return new BreakNode(l, c);
        }
        if (Match(TokenType.Continue))
        {
            int l = Cur.Line, c = Cur.Column;
            Expect(TokenType.Semicolon, "expected ';' after continue");
            return new ContinueNode(l, c);
        }
        if (Check(TokenType.Semicolon)) { Advance(); return new ExprStmtNode(new LiteralNode(null, Cur.Line, Cur.Column), Cur.Line, Cur.Column); }

        // 变量声明: int x 或 int? x
        if (IsType(Cur) && Peek(1).Type == TokenType.Identifier)
            return ParseVarDecl();

        // 泛型集合声明: List<int> a = … / List<double> b;（Dart 里数组就是这么声明的）
        // ⚠ 此前 `IsType` 只认内建类型关键字，`List` 是普通 Identifier ⇒ 整条声明被
        //   ParseExprStmt 的容错循环当作"无法识别的语句"整个跳过，**变量根本没被声明**，
        //   后面所有对它的引用退化成 dataSection 里的全局 0。
        if (Check(TokenType.Identifier) && Peek(1).Type == TokenType.Lt && IsGenericVarDecl())
            return ParseVarDecl();

        return ParseExprStmt();
    }

    /// <summary>
    /// 前瞻判定「Identifier &lt; … &gt; Identifier (= | ;)」形态的泛型变量声明。
    /// 括号配对跳过泛型参数；中途撞上 <c>;</c>/<c>{</c>/<c>}</c>/<c>)</c>/EOF 即判定不是声明
    /// （这样 <c>a &lt; b</c> 这类比较表达式不会被误判）。
    /// </summary>
    private bool IsGenericVarDecl()
    {
        int i = _pos + 1;                     // 当前是 Identifier，下一个是 '<'
        if (i >= _tokens.Count || GetTokenType(_tokens[i]) != TokenType.Lt) return false;
        int depth = 0;
        while (i < _tokens.Count)
        {
            var tt = GetTokenType(_tokens[i]);
            if (tt == TokenType.Lt) depth++;
            else if (tt == TokenType.Gt)
            {
                depth--;
                if (depth == 0) break;
            }
            else if (tt is TokenType.Semicolon or TokenType.LBrace or TokenType.RBrace
                     or TokenType.RParen or TokenType.EOF)
                return false;
            i++;
        }
        if (i >= _tokens.Count || depth != 0) return false;
        int nameIdx = i + 1;                  // '>' 之后必须是变量名
        if (nameIdx >= _tokens.Count || GetTokenType(_tokens[nameIdx]) != TokenType.Identifier) return false;
        int afterIdx = nameIdx + 1;           // 再之后必须是 '=' 或 ';'
        if (afterIdx >= _tokens.Count) return false;
        var after = GetTokenType(_tokens[afterIdx]);
        return after == TokenType.Assign || after == TokenType.Semicolon;
    }

    private bool IsType(Token t)
    {
        return t.Type is TokenType.Int or TokenType.DoubleKw or TokenType.StringKw
            or TokenType.Bool or TokenType.Void or TokenType.Var or TokenType.Final;
    }

    private bool IsFunctionCallStart(Token afterIdent)
    {
        return afterIdent.Type == TokenType.LParen || afterIdent.Type == TokenType.Lt;
    }

    private ASTNode ParseVarDecl()
    {
        int l = Cur.Line, c = Cur.Column;
        string type = Advance().Value;

        // 可空类型: int? String?
        Match(TokenType.Question);

        if (Check(TokenType.Lt))
        {
            Advance();
            int depth = 1;
            while (depth > 0 && !Check(TokenType.EOF))
            {
                if (Check(TokenType.Lt)) depth++;
                else if (Check(TokenType.Gt)) depth--;
                Advance();
            }
            type = type + "_gen";
        }

        string name = Expect(TokenType.Identifier, "expected variable name").Value;
        ASTNode? init = null;
        if (Match(TokenType.Assign))
            init = ParseExpression();
        var result = new VarDeclNode(type, name, init, l, c);
        // 多变量声明: int a = 1, b = 2;
        while (Match(TokenType.Comma))
        {
            string extraName = Expect(TokenType.Identifier, "expected variable name").Value;
            ASTNode? extraInit = null;
            if (Match(TokenType.Assign))
                extraInit = ParseExpression();
            _extraDeclarations.Add(new VarDeclNode(type, extraName, extraInit, l, c));
        }
        // 容错: 跳过无法识别的token到;
        while (!Check(TokenType.Semicolon) && !Check(TokenType.EOF) && !Check(TokenType.RBrace))
            Advance();
        Expect(TokenType.Semicolon, "expected ';' after variable declaration");
        return result;
    }

    private ASTNode ParseMixinDecl()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // mixin
        string name = Expect(TokenType.Identifier, "expected mixin name").Value;
        Expect(TokenType.LBrace, "expected '{'");
        var members = new List<ASTNode>();
        while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
        {
            if (IsType(Cur) && Peek().Type == TokenType.Identifier && Peek(2).Type == TokenType.LParen)
                members.Add(ParseMethodOrFunction());
            else if (IsType(Cur) && Peek().Type == TokenType.Identifier)
                { Advance(); Advance(); } // skip var declarations in mixin
            else Advance();
        }
        Expect(TokenType.RBrace, "expected '}'");
        // mixin → 当作 class 编译 (标签前缀不同但在代码生成中统一处理)
        var node = new ClassDeclNode(name, members, l, c);
        node.IsMixin = true;
        return node;
    }

    /// MCU-compatible extension method: desugar to static functions
    /// extension Name on Type { R method(T2 p) => body; }
    /// → R Name_method(Type _this, T2 p) { body; }
    private ASTNode ParseExtensionDecl()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // 'extension'
        string extName = Expect(TokenType.Identifier, "expected extension name").Value;
        string onKw = Match(TokenType.OnKw) ? "on" : "";
        if (onKw == "") { Expect(TokenType.OnKw, "expected 'on'"); }
        string targetType = IsType(Cur) ? Advance().Value : Expect(TokenType.Identifier, "expected type").Value;
        Expect(TokenType.LBrace, "expected '{'");
        var methods = new List<ASTNode>();
        while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
        {
            int ml = Cur.Line, mc = Cur.Column;
            string retType = Advance().Value; // return type
            Match(TokenType.Question);
            string mName = Expect(TokenType.Identifier, "expected method name").Value;
            Expect(TokenType.LParen, "expected '('");
            var parms = ParseParameters();
            while (!Check(TokenType.RParen) && !Check(TokenType.EOF)) Advance();
            Expect(TokenType.RParen, "expected ')'");
            List<ASTNode> body;
            if (Check(TokenType.Assign) && Peek(1).Type == TokenType.Gt)
            {
                Advance(); Advance(); // skip '=' and '>'
                var e = ParseExpression();
                Match(TokenType.Semicolon);
                body = new List<ASTNode> { new ReturnNode(e, ml, mc) };
            }
            else { body = ParseBlockBody(); }
            // Prepend target type as first _this parameter
            parms.Insert(0, new ParamNode(targetType, "_this", ml, mc));
            string fullName = $"{extName}_{mName}";
            methods.Add(new MethodDeclNode(fullName, retType, parms, body, ml, mc));
        }
        Expect(TokenType.RBrace, "expected '}'");
        return new BlockNode(methods, l, c);
    }

    private ASTNode ParseClassDecl()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance();
        string name = Expect(TokenType.Identifier, "expected class name").Value;
        // 泛型类型参数: class Box<T> { ... } — 跳过 <...>
        if (Check(TokenType.Lt))
        {
            int depth = 1;
            Advance();
            while (depth > 0 && !Check(TokenType.EOF))
            {
                if (Check(TokenType.Lt)) depth++;
                else if (Check(TokenType.Gt)) depth--;
                Advance();
            }
        }
        // Parse extends/with clauses
        var mixinNames = new List<string>();
        while (!Check(TokenType.LBrace) && !Check(TokenType.EOF))
        {
            if (Match(TokenType.WithKw))
            {
                // Parse comma-separated mixin names: with A, B, C
                do {
                    string mixinName = Expect(TokenType.Identifier, "expected mixin name after 'with'").Value;
                    mixinNames.Add(mixinName);
                } while (Match(TokenType.Comma));
            }
            else if (Check(TokenType.Identifier))
                Advance(); // extends class name
            else if (Check(TokenType.Comma))
                Advance();
            else break;
        }
        Expect(TokenType.LBrace, "expected '{'");
        var members = new List<ASTNode>();
        while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
        {
            // 构造函数: ClassName(params) { body } 或 ClassName(params);
            if (Cur.Type == TokenType.Identifier && Cur.Value == name && Peek().Type == TokenType.LParen)
            {
                int cl = Cur.Line, cc = Cur.Column;
                Advance(); // 类名
                Expect(TokenType.LParen, "expected '('");
                var ctorParms = ParseParameters();
                while (!Check(TokenType.RParen) && !Check(TokenType.EOF)) Advance();
                Expect(TokenType.RParen, "expected ')'");
                List<ASTNode> ctorBody;
                if (Check(TokenType.LBrace))
                    ctorBody = ParseBlockBody();
                else
                {
                    ctorBody = new List<ASTNode>();
                    Match(TokenType.Semicolon); // 无体构造函数声明
                }
                members.Add(new MethodDeclNode(name, "void", ctorParms, ctorBody, cl, cc));
            }
            else if (IsType(Cur))
            {
                if (Peek(1).Type == TokenType.Identifier && Peek(2).Type == TokenType.LParen)
                    members.Add(ParseMethodOrFunction());
                else if (Peek(1).Type == TokenType.Identifier)
                    members.Add(ParseVarDecl());
                else
                    throw Error($"Unexpected token in class body: {Cur.Type} at {Cur.Line}:{Cur.Column}");
            }
            else if (Cur.Type == TokenType.Identifier && (Cur.Value == "static" || Cur.Value == "native" || Cur.Value == "external"))
            {
                if (Cur.Value == "native" || Cur.Value == "external") _pendingExternal = true;
                Advance(); // 跳过修饰符
            }
            else if (Cur.Type == TokenType.External) { _pendingExternal = true; Advance(); }
            else if (Cur.Type == TokenType.Identifier)
            {
                // 泛型类型字段: T val; / T? val; / List<int> vals;（数组字段就是这种写法）
                if (Peek(1).Type == TokenType.Identifier || Peek(1).Type == TokenType.Question
                    || (Peek(1).Type == TokenType.Lt && IsGenericVarDecl()))
                    members.Add(ParseVarDecl());
                else
                    throw Error($"Unexpected token in class body: {Cur.Type} at {Cur.Line}:{Cur.Column}");
            }
            else
                throw Error($"Unexpected token in class body: {Cur.Type} at {Cur.Line}:{Cur.Column}");
        }
        Expect(TokenType.RBrace, "expected '}'");
        return new ClassDeclNode(name, members, l, c, mixinNames);
    }

    private ASTNode ParseMethodOrFunction()
    {
        bool isExternal = _pendingExternal;
        _pendingExternal = false;
        int l = Cur.Line, c = Cur.Column;
        string returnType = Advance().Value;
        Match(TokenType.Question); // 可空返回类型
        string name = Expect(TokenType.Identifier, "expected method name").Value;
        Expect(TokenType.LParen, "expected '('");
        var parms = ParseParameters();
        while (!Check(TokenType.RParen) && !Check(TokenType.EOF)) Advance();
        Expect(TokenType.RParen, "expected ')'");
        List<ASTNode> body;
        if (isExternal)
        {
            body = new List<ASTNode>(); // external 函数无函数体
            Match(TokenType.Semicolon);
        }
        else
        {
            body = ParseBlockBody();
        }
        var result = new MethodDeclNode(name, returnType, parms, body, l, c);
        result.IsExternal = isExternal;
        return result;
    }

    private List<ParamNode> ParseParameters()
    {
        var parms = new List<ParamNode>();
        if (!Check(TokenType.RParen))
        {
            do
            {
                int pl = Cur.Line, pc = Cur.Column;
                string ptype = Advance().Value;
                // Dart 构造函数参数: this.field
                if (ptype == "this" && Check(TokenType.Dot))
                {
                    Advance(); // .
                    string pname = Expect(TokenType.Identifier, "expected parameter name").Value;
                    parms.Add(new ParamNode("this", pname, pl, pc));
                }
                else
                {
                    // 跳过泛型参数: List<int>, Map<String, int>
                    if (Check(TokenType.Lt))
                    {
                        int d = 1; Advance();
                        while (d > 0 && !Check(TokenType.EOF))
                        {
                            if (Check(TokenType.Lt)) d++;
                            else if (Check(TokenType.Gt)) d--;
                            Advance();
                        }
                    }
                    // 跳过可空类型?
                    Match(TokenType.Question);
                    string pname = Expect(TokenType.Identifier, "expected parameter name").Value;
                    parms.Add(new ParamNode(ptype, pname, pl, pc));
                }
            } while (Match(TokenType.Comma));
        }
        return parms;
    }

    private ASTNode ParseIf()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance();
        Expect(TokenType.LParen, "expected '(' after if");
        var cond = ParseExpression();
        while (!Check(TokenType.RParen) && !Check(TokenType.EOF)) Advance();
        Expect(TokenType.RParen, "expected ')' after condition");
        var thenBody = ParseStatementAsBlock();
        List<ASTNode>? elseBody = null;
        if (Match(TokenType.Else))
            elseBody = ParseStatementAsBlock();
        return new IfNode(cond, thenBody, elseBody, l, c);
    }

    private ASTNode ParseWhile()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance();
        Expect(TokenType.LParen, "expected '(' after while");
        var cond = ParseExpression();
        while (!Check(TokenType.RParen) && !Check(TokenType.EOF)) Advance();
        Expect(TokenType.RParen, "expected ')' after condition");
        var body = ParseStatementAsBlock();
        return new WhileNode(cond, body, l, c);
    }

    private ASTNode ParseFor()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance();
        Expect(TokenType.LParen, "expected '(' after for");

        ASTNode? init = null;
        if (!Check(TokenType.Semicolon))
        {
            if (IsType(Cur) && Peek().Type == TokenType.Identifier)
                init = ParseVarDeclNoSemicolon();
            else
                init = ParseExpression();
        }
        // 检测 for-in: 如果下一个token是 Identifier "in", 则跳过in+collection
        if (!IsAtEnd && Check(TokenType.Identifier) && Cur.Value == "in")
        {
            Advance(); // in
            if (!IsAtEnd) ParseExpression(); // collection
            while (!IsAtEnd && !Check(TokenType.RParen)) Advance();
            if (Check(TokenType.RParen)) Advance();
            var forInBody = ParseStatementAsBlock();
            return new ForNode(init, null, null, forInBody, l, c);
        }
        while (!Check(TokenType.Semicolon) && !Check(TokenType.EOF)) Advance();
        Expect(TokenType.Semicolon, "expected ';' in for");

        ASTNode? condition = null;
        if (!Check(TokenType.Semicolon))
            condition = ParseExpression();
        Expect(TokenType.Semicolon, "expected ';' in for");

        ASTNode? increment = null;
        if (!Check(TokenType.RParen))
            increment = ParseExpression();
        Expect(TokenType.RParen, "expected ')' after for clauses");

        var body = ParseStatementAsBlock();
        return new ForNode(init, condition, increment, body, l, c);
    }

    private ASTNode ParseDoWhile()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // do
        var body = ParseStatementAsBlock();
        Expect(TokenType.While, "expected 'while' after do body");
        Expect(TokenType.LParen, "expected '(' after while");
        var cond = ParseExpression();
        while (!Check(TokenType.RParen) && !Check(TokenType.EOF)) Advance();
        Expect(TokenType.RParen, "expected ')' after condition");
        Expect(TokenType.Semicolon, "expected ';' after do-while");
        return new DoWhileNode(cond, body, l, c);
    }

    private ASTNode ParseTryCatch()
    {
        int l = Cur.Line, c = Cur.Column;
        var body = ParseBlockBody();
        var catches = new List<CatchClause>();
        while (Check(TokenType.CatchKw))
        {
            int cl = Cur.Line, cc = Cur.Column;
            string? varName = null; string? excType = null;
            if (Check(TokenType.LParen))
            {
                if (Cur.Type == TokenType.Identifier)
                {
                    varName = Cur.Value; excType = Cur.Value;
                    Advance();
                }
                while (!Check(TokenType.RParen) && !IsAtEnd) Advance();
            }
            var catchBody = ParseBlockBody();
            catches.Add(new CatchClause(varName, excType, catchBody, cl, cc));
        }
        List<ASTNode>? finallyBlock = null;
        if (Check(TokenType.FinallyKw))
        {
            finallyBlock = ParseBlockBody();
        }
        return new TryStmt(body, catches, finallyBlock, l, c);
    }

    private ASTNode ParseThrow()
    {
        int l = Cur.Line, c = Cur.Column;
        ASTNode? expr = null;
        if (Cur.Type != TokenType.Semicolon) expr = ParseExpression();
        Expect(TokenType.Semicolon, "expected ';' after throw");
        return new ThrowStmt(expr, l, c);
    }

    private ASTNode ParseVarDeclNoSemicolon()
    {
        int l = Cur.Line, c = Cur.Column;
        string type = Advance().Value;
        string name = Expect(TokenType.Identifier, "expected variable name").Value;
        ASTNode? init = null;
        if (Match(TokenType.Assign))
            init = ParseExpression();
        return new VarDeclNode(type, name, init, l, c);
    }

    private ASTNode ParseReturn()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance();
        ASTNode? val = null;
        if (!Check(TokenType.Semicolon))
            val = ParseExpression();
        Expect(TokenType.Semicolon, "expected ';' after return");
        return new ReturnNode(val, l, c);
    }

    private ASTNode ParseBlock()
    {
        int l = Cur.Line, c = Cur.Column;
        var stmts = ParseBlockBody();
        return new BlockNode(stmts, l, c);
    }

    private List<ASTNode> ParseBlockBody()
    {
        Expect(TokenType.LBrace, "expected '{'");
        var stmts = new List<ASTNode>();
        while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
        {
            var stmt = ParseStatement();
            if (stmt != null)
            {
                stmts.Add(stmt);
                if (_extraDeclarations.Count > 0)
                {
                    stmts.AddRange(_extraDeclarations);
                    _extraDeclarations.Clear();
                }
            }
        }
        Expect(TokenType.RBrace, "expected '}'");
        return stmts;
    }

    private List<ASTNode> ParseStatementAsBlock()
    {
        if (Check(TokenType.LBrace))
            return ParseBlockBody();
        var stmt = ParseStatement();
        return new List<ASTNode> { stmt };
    }

    private ASTNode ParseExprStmt()
    {
        int l = Cur.Line, c = Cur.Column;
        var expr = ParseExpression();
        // 容错: 跳过无法识别的中缀(is/as等)直到;或}
        // 安全检查: 确保不会越界
        while (!IsAtEnd && !Check(TokenType.Semicolon) && !Check(TokenType.RBrace) && !Check(TokenType.EOF))
            Advance();
        if (!IsAtEnd && Check(TokenType.Semicolon)) Advance();
        return new ExprStmtNode(expr, l, c);
    }

    // ---- Expression parsing (precedence climbing) ----

    private ASTNode ParseExpression() => ParseAssignment();

    private ASTNode ParseAssignment()
    {
        var left = ParseTernary();

        if (Check(TokenType.Assign))
        {
            int l = Cur.Line, c = Cur.Column;
            Advance();
            var right = ParseAssignment();
            if (left is VarNode v)
                return new AssignNode(v.Name, right, l, c);
            // 下标左值: a[i] = v —— 此前整段落到下面的"容错"分支被丢掉（目标与下标都没了）。
            if (left is IndexNode ix)
                return new IndexAssignNode(ix.Target, ix.Index, right, l, c);
            // 容错: 其它复杂左值 — 求值但返回右值
            return right;
        }
        if (Check(TokenType.PlusAssign) || Check(TokenType.MinusAssign) || Check(TokenType.MulAssign) || Check(TokenType.DivAssign) || Check(TokenType.ModAssign))
        {
            string op = Advance().Value;
            var right = ParseAssignment();
            if (left is VarNode v)
                return new OpAssignNode(v.Name, op, right, left.Line, left.Column);
            if (left is IndexNode ix2)
                return new IndexOpAssignNode(ix2.Target, ix2.Index, op, right, left.Line, left.Column);
            // 容错: 复杂左值复合赋值 — 返回右值
            return right;
        }
        return left;
    }

    private ASTNode ParseTernary()
    {
        var left = ParseLogicOr();
        // ?? null-coalescing: a ?? b
        if (Check(TokenType.Question) && Peek(1).Type == TokenType.Question)
        {
            Advance(); Advance(); // ??
            var right = ParseTernary();
            return new BinaryNode(left, "??", right, left.Line, left.Column);
        }
        if (Match(TokenType.Question))
        {
            int l = left.Line, cl = left.Column;
            // 容错: ?可能是可空类型后缀或错误, 非三元运算符
            if (Check(TokenType.Semicolon) || Check(TokenType.RParen) || Check(TokenType.Comma) || Check(TokenType.EOF))
                return left;
            var thenExpr = ParseExpression();
            // 容错: 如果找不到:, 回退(可能不是三元)
            if (!Check(TokenType.Colon))
            {
                while (!Check(TokenType.Colon) && !Check(TokenType.Semicolon) && !Check(TokenType.EOF))
                    Advance();
                if (!Check(TokenType.Colon)) return left;
            }
            Expect(TokenType.Colon, "expected ':' in ternary");
            var elseExpr = ParseTernary();
            return new BinaryNode(
                new BinaryNode(left, "?", thenExpr, l, cl),
                ":", elseExpr, l, cl);
        }
        return left;
    }

    private ASTNode ParseLogicOr()
    {
        var left = ParseLogicAnd();
        while (Match(TokenType.OrOr))
        {
            string op = "||";
            var right = ParseLogicAnd();
            left = new BinaryNode(left, op, right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseLogicAnd()
    {
        var left = ParseBitwiseXor();
        while (Match(TokenType.AndAnd))
        {
            string op = "&&";
            var right = ParseBitwiseXor();
            left = new BinaryNode(left, op, right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseBitwiseXor()
    {
        var left = ParseEquality();
        while (Match(TokenType.BitNot)) // BitNot reused for ^ (XOR)
        {
            string op = "^";
            var right = ParseEquality();
            left = new BinaryNode(left, op, right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseEquality()
    {
        var left = ParseComparison();
        while (Check(TokenType.Eq) || Check(TokenType.Neq))
        {
            string op = Advance().Value;
            var right = ParseComparison();
            left = new BinaryNode(left, op, right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseComparison()
    {
        var left = ParseAdditive();
        while (Check(TokenType.Lt) || Check(TokenType.Gt) || Check(TokenType.Le) || Check(TokenType.Ge))
        {
            string op = Advance().Value;
            var right = ParseAdditive();
            left = new BinaryNode(left, op, right, left.Line, left.Column);
        }
        // 容错: is / as / is! 类型检查 — 跳过类型名,保留左值
        if (Check(TokenType.IsKw) || Check(TokenType.AsKw))
        {
            Advance(); // is/as
            Match(TokenType.Not); // is!
            // 跳过类型名 (可能带泛型)
            if (Check(TokenType.Identifier) || Check(TokenType.Int) || Check(TokenType.DoubleKw) ||
                Check(TokenType.StringKw) || Check(TokenType.Bool) || Check(TokenType.Void))
                Advance();
            if (Check(TokenType.Lt))
            { int d=1; Advance(); while (d>0&&!Check(TokenType.EOF)){if(Check(TokenType.Lt))d++;else if(Check(TokenType.Gt))d--;Advance();} }
            Match(TokenType.Question); // 可空类型?
            return left;
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
        while (Check(TokenType.Mul) || Check(TokenType.Div) || Check(TokenType.Mod) || Check(TokenType.IntDiv))
        {
            string op = Advance().Value;
            var right = ParseUnary();
            left = new BinaryNode(left, op, right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseUnary()
    {
        if (Match(TokenType.Minus))
            return new UnaryNode("-", ParseUnary(), Cur.Line, Cur.Column);
        if (Match(TokenType.Not))
            return new UnaryNode("!", ParseUnary(), Cur.Line, Cur.Column);
        if (Match(TokenType.BitNot))
            return new UnaryNode("~", ParseUnary(), Cur.Line, Cur.Column);
        if (Match(TokenType.Increment))
        {
            int l = Cur.Line, c = Cur.Column;
            return new PrefixPostfixNode("++", ParseUnary(), true, l, c);
        }
        if (Match(TokenType.Decrement))
        {
            int l = Cur.Line, c = Cur.Column;
            return new PrefixPostfixNode("--", ParseUnary(), true, l, c);
        }
        return ParsePostfix();
    }

    private ASTNode ParsePostfix()
    {
        var expr = ParsePrimary();
        while (true)
        {
            // 成员访问: obj.field / obj.method()
            // 必须在 LParen 之前检查, 以便 obj.method() 将接收者作为第一个参数
            if (Check(TokenType.Dot))
            {
                Advance();
                if (Check(TokenType.Identifier))
                {
                    string memberName = Advance().Value;
                    ASTNode receiver = expr; // 保存接收者

                    // 如果紧接着是 '(' → 方法调用, 接收者作为第一个参数
                    if (Check(TokenType.LParen))
                    {
                        Advance();
                        var args = new List<ASTNode>();
                        if (!Check(TokenType.RParen))
                        {
                            do args.Add(ParseExpression());
                            while (Match(TokenType.Comma));
                        }
                        while (!Check(TokenType.RParen) && !Check(TokenType.EOF))
                            Advance();
                        Expect(TokenType.RParen, "expected ')'");
                        // 接收者作为第一个参数传入
                        args.Insert(0, receiver);
                        expr = new CallNode($"_dot_{memberName}", args, receiver.Line, receiver.Column);
                        continue;
                    }
                    else
                    {
                        // 属性访问
                        expr = new VarNode($"_dot_{memberName}", expr.Line, expr.Column);
                        continue;
                    }
                }
                else if (Check(TokenType.Dot)) { Advance(); } // cascade ..
                else { } // skip lone dot
                continue;
            }

            if (Check(TokenType.LParen))
            {
                Advance();
                var args = new List<ASTNode>();
                if (!Check(TokenType.RParen))
                {
                    do args.Add(ParseExpression());
                    while (Match(TokenType.Comma));
                }
                // 容错: 跳过未知token到)
                while (!Check(TokenType.RParen) && !Check(TokenType.EOF))
                    Advance();
                Expect(TokenType.RParen, "expected ')'");
                if (expr is VarNode v)
                    expr = new CallNode(v.Name, args, v.Line, v.Column);
                else
                    expr = new CallNode("_anon", args, expr.Line, expr.Column);
                continue;
            }

            // 下标: a[i] / a[i][j] / f()[i]
            // ⚠ 此前**没有这个分支** ⇒ `a[i]` 只解析成裸 `a`（下标整段被吞掉，后续 token
            //   由 ParseExprStmt 的容错循环跳过），于是数组读写全部退化成"对数组变量本身操作"。
            if (Check(TokenType.LBracket))
            {
                Advance(); // [
                var index = ParseExpression();
                while (!Check(TokenType.RBracket) && !Check(TokenType.EOF)) Advance();
                Expect(TokenType.RBracket, "expected ']'");
                expr = new IndexNode(expr, index, expr.Line, expr.Column);
                continue;
            }

            // 空值安全成员访问: obj?.field
            if (Check(TokenType.Question) && Peek(1).Type == TokenType.Dot)
            {
                Advance(); Advance(); // ?.
                string field = Expect(TokenType.Identifier, "expected field name").Value;
                expr = new VarNode($"_qdot_{field}", expr.Line, expr.Column);
                continue;
            }

            if (Check(TokenType.Increment) || Check(TokenType.Decrement))
            {
                string op = Advance().Value;
                int l = expr.Line, cl = expr.Column;
                expr = new PrefixPostfixNode(op, expr, false, l, cl);
                continue;
            }

            break;
        }
        return expr;
    }

    private ASTNode ParsePrimary()
    {
        int l = Cur.Line, c = Cur.Column;

        if (Check(TokenType.Integer))
        {
            var t = Advance();
            return new LiteralNode(int.Parse(t.Value), l, c);
        }
        if (Check(TokenType.Float))
        {
            var t = Advance();
            // Dart 只有 double (64-bit), 没有 float 类型
            return new LiteralNode(double.Parse(t.Value, System.Globalization.CultureInfo.InvariantCulture), l, c);
        }
        if (Check(TokenType.StringLit))
        {
            var t = Advance();
            return new LiteralNode(t.Value, l, c);
        }
        if (Match(TokenType.True)) return new LiteralNode(1, l, c);
        if (Match(TokenType.False)) return new LiteralNode(0, l, c);
        if (Match(TokenType.Null)) return new LiteralNode(null, l, c);

        if (Check(TokenType.Identifier))
        {
            var name = Advance().Value;
            // 泛型实例化: Box<int>() / Box<int>.static() — 跳过 <...> (启发式: 匹配的 > 后紧跟 ( 或 . )
            if (Check(TokenType.Lt) && IsGenericInstantiation())
                SkipGenericArguments();
            return new VarNode(name, l, c);
        }

        if (Match(TokenType.LParen))
        {
            var expr = ParseExpression();
            int pd = 1;
            while (pd > 0 && !Check(TokenType.EOF))
            {
                if (Check(TokenType.LParen)) pd++;
                else if (Check(TokenType.RParen)) pd--;
                if (pd > 0) Advance();
            }
            Expect(TokenType.RParen, "expected ')'");
            return expr;
        }

        // 容错: 类型关键词/泛型/字面量容器/其他token
        if (IsType(Cur) || Cur.Type == TokenType.Void || Cur.Type == TokenType.Identifier ||
            Cur.Type == TokenType.Lt || Cur.Type == TokenType.Gt ||
            Cur.Type == TokenType.RBrace || Cur.Type == TokenType.RBracket ||
            Cur.Type == TokenType.Semicolon || Cur.Type == TokenType.Colon)
        {
            Advance(); return new LiteralNode(0, l, c);
        }
        // 数组字面量: [1,2,3]
        // ⚠ 此前是把整个 `[...]` 括号配对跳过后 `return LiteralNode(0)` —— 元素**一个都没解析**，
        //   于是 `List<int> a = [1,2,3,4]` 里的数组永远是常量 0（`a[i]` 再怎么修也读不到东西）。
        if (Match(TokenType.LBracket))
        {
            var elements = new List<ASTNode>();
            if (!Check(TokenType.RBracket))
            {
                do elements.Add(ParseExpression());
                while (Match(TokenType.Comma) && !Check(TokenType.RBracket) && !Check(TokenType.EOF));
            }
            while (!Check(TokenType.RBracket) && !Check(TokenType.EOF)) Advance();
            Expect(TokenType.RBracket, "expected ']'");
            return new ArrayLiteralNode(elements, l, c);
        }
        // map/set字面量: {a:1, b:2}
        if (Match(TokenType.LBrace))
        {
            int bd = 1;
            while (bd > 0 && !Check(TokenType.EOF))
            {
                if (Check(TokenType.LBrace)) bd++;
                else if (Check(TokenType.RBrace)) bd--;
                Advance();
            }
            return new LiteralNode(0, l, c);
        }
        throw Error($"Unexpected token: {Cur.Type}({Cur.Value}) at {Cur.Line}:{Cur.Column}");
    }

    /// <summary>检测当前 &lt; 是否为泛型参数列表 (而非比较运算符)。
    /// 启发式: 匹配的 &gt; 之后紧跟 '(' 或 '.' 时视为泛型实例化。</summary>
    private bool IsGenericInstantiation()
    {
        int depth = 0;
        for (int i = 0; _pos + i < _tokens.Count; i++)
        {
            var tt = GetTokenType(_tokens[_pos + i]);
            if (tt == TokenType.Lt) depth++;
            else if (tt == TokenType.Gt)
            {
                depth--;
                if (depth == 0)
                {
                    int next = _pos + i + 1;
                    if (next >= _tokens.Count) return false;
                    var nt = GetTokenType(_tokens[next]);
                    return nt == TokenType.LParen || nt == TokenType.Dot;
                }
            }
            else if (tt == TokenType.Semicolon || tt == TokenType.EOF) return false;
        }
        return false;
    }

    /// <summary>跳过泛型参数列表 &lt;...&gt; (当前 Cur 必须为 &lt;)。</summary>
    private void SkipGenericArguments()
    {
        int depth = 0;
        do
        {
            if (Check(TokenType.Lt)) depth++;
            else if (Check(TokenType.Gt)) depth--;
            Advance();
        } while (depth > 0 && !Check(TokenType.EOF));
    }
}
