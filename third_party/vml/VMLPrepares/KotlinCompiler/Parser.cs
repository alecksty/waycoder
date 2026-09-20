using CompilerBase;

namespace KotlinCompiler;

public class Parser : ParserBase<Token, TokenType>
{
    protected override TokenType GetTokenType(Token token) => token.Type;

    bool _pendingExternal = false;

    public Parser(List<Token> tokens) : base(tokens) { }

    protected override Token Expect(TokenType t, string msg) =>
        Check(t) ? Advance() : throw Error($"{msg}（位置 {Cur.Line}:{Cur.Column}），实际得到 {GetTokenType(Cur)} '{Cur.Value}'");

    public Program Parse() {
        var funcs = new List<ASTNode>();
        while (GetTokenType(Cur) != TokenType.EOF) {
            if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "external") { _pendingExternal = true; Advance(); continue; }
            // 顶层 `val` / `var` —— 此前**没有任何一条分支认它**，于是兜底的
            // `else Advance()` 把它**一个 token 一个 token 地静默吃掉**：
            // 声明没了、`Program.Functions` 里也没有它，函数里引用它时
            // `_varOffsets` 查不到 ⇒ 那条 `VarRef` **一个字都不生成**（R0 留着上一步的残值）。
            // 表现就是台账里那条「顶层 `arrayOf` 读回是 0」—— 其实**顶层标量也一样是 0**，
            // 而且根因不在数组、在这个兜底分支。
            if (GetTokenType(Cur) == TokenType.KEYWORD && (Cur.Value == "val" || Cur.Value == "var"))
                funcs.Add(ParseVarDecl(Cur.Value == "val"));
            else if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "fun") funcs.Add(ParseFunction());
            else if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "data") funcs.Add(ParseDataClass());
            else if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "class") funcs.Add(ParseClassDecl());
            else if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "interface") funcs.Add(ParseInterface());
            else if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "sealed") { Advance(); var cd = ParseClassDecl(); if (cd is ClassDecl c) c.IsSealed = true; funcs.Add(cd); }
            else if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "object") { Advance(); if (GetTokenType(Cur) == TokenType.IDENTIFIER) Advance(); if (GetTokenType(Cur) == TokenType.LBRACE) { Advance(); while (GetTokenType(Cur) != TokenType.RBRACE && GetTokenType(Cur) != TokenType.EOF) { if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "external") { _pendingExternal = true; Advance(); continue; } if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "fun") funcs.Add(ParseFunction()); else Advance(); } Advance(); /* skip } */ } }
            else Advance();
        }
        return new Program(funcs);
    }

    ASTNode ParseInterface() {
        Advance(); // interface
        string name = Expect(TokenType.IDENTIFIER, "期望接口名").Value;
        Expect(TokenType.LBRACE, "Expected '{'");
        var methods = new List<FunctionDecl>();
        while (GetTokenType(Cur) != TokenType.RBRACE && GetTokenType(Cur) != TokenType.EOF) {
            if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "fun") {
                var fn = ParseFunction();
                if (fn is FunctionDecl fd) methods.Add(fd);
            } else { Advance(); }
        }
        Expect(TokenType.RBRACE, "Expected '}'");
        return new InterfaceDecl(name, methods);
    }

    List<string> ParseTypeParams() {
        var tparams = new List<string>();
        if (GetTokenType(Cur) == TokenType.LT) {
            Advance(); // <
            while (GetTokenType(Cur) != TokenType.GT && GetTokenType(Cur) != TokenType.EOF) {
                if (tparams.Count > 0 && GetTokenType(Cur) == TokenType.COMMA) { Advance(); continue; }
                tparams.Add(Expect(TokenType.IDENTIFIER, "期望类型参数名").Value);
            }
            Expect(TokenType.GT, "Expected '>'");
        }
        return tparams;
    }

    ASTNode ParseFunction() {
        bool isExternal = _pendingExternal;
        _pendingExternal = false;
        Expect(TokenType.KEYWORD, "Expected 'fun'"); // fun
        var tparams = ParseTypeParams(); // generic type params (ignored in MCU mode)
        string name = Expect(TokenType.IDENTIFIER, "期望函数名").Value;
        // Extension function: fun ReceiverType.methodName(...)
        string? receiverType = null;
        if (GetTokenType(Cur) == TokenType.DOT) {
            Advance(); // .
            receiverType = name; // First identifier is the receiver type
            name = Expect(TokenType.IDENTIFIER, "期望函数名在 . 后").Value;
        }
        Expect(TokenType.LPAREN, "Expected '('");
        var pars = new List<string>();
        while (GetTokenType(Cur) != TokenType.RPAREN) {
            if (pars.Count > 0) Expect(TokenType.COMMA, "Expected ','");
            Expect(TokenType.IDENTIFIER, "期望参数名");
            if (GetTokenType(Cur) == TokenType.COLON) { Advance(); Advance(); }
            pars.Add(_tokens[_pos - 3].Value);
        }
        Expect(TokenType.RPAREN, "Expected ')'");
        if (GetTokenType(Cur) == TokenType.COLON) { Advance(); Advance(); } // : ReturnType
        ASTNode body;
        if (isExternal) {
            // external 函数无函数体
            body = new Block([]);
        } else if (GetTokenType(Cur) == TokenType.ASSIGN) {
            Advance();
            var expr = ParseExpr();
            body = new Block([new ReturnStmt(expr)]);
        } else {
            body = ParseBlock();
        }
        var func = new FunctionDecl(name, pars, body, tparams, isExternal);
        if (receiverType != null)
            return new ExtensionDecl(receiverType, func);
        return func;
    }

    ASTNode ParseBlock() {
        Expect(TokenType.LBRACE, "Expected '{'");
        var stmts = new List<ASTNode>();
        while (GetTokenType(Cur) != TokenType.RBRACE && GetTokenType(Cur) != TokenType.EOF)
            stmts.Add(ParseStatement());
        Expect(TokenType.RBRACE, "Expected '}'");
        return new Block(stmts);
    }

    /// <summary>
    /// 语句入口 —— **顺手给每条语句盖上起始行列**（`ASTNode.Line`/`Column`）。
    /// 与 C/Rust 同一套路（单一入口 + 包一层）。见 `CCompiler/Parser.Statements.cs` 的说明。
    /// </summary>
    ASTNode ParseStatement()
    {
        int __line = Cur.Line, __col = Cur.Column;
        var __node = ParseStatementCore();
        if (__node != null && __node.Line == 0) { __node.Line = __line; __node.Column = __col; }
        return __node;
    }

    ASTNode ParseStatementCore() {
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "val")
            return ParseVarDecl(true);
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "var")
            return ParseVarDecl(false);
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "if")
            return ParseIf();
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "while")
            return ParseWhile();
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "do")
            return ParseDoWhile();
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "for")
            return ParseFor();
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "when")
            return ParseWhen();
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "return")
            return ParseReturn();
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "try")
            return ParseTryCatch();
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "throw") {
            Advance(); var expr = ParseExpr();
            if (GetTokenType(Cur) == TokenType.SEMICOLON) Advance();
            return new ThrowStmt(expr);
        }
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "break")
            { Advance(); if (GetTokenType(Cur) == TokenType.SEMICOLON) Advance(); return new BreakStmt(); }
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "continue")
            { Advance(); if (GetTokenType(Cur) == TokenType.SEMICOLON) Advance(); return new ContinueStmt(); }
        if (GetTokenType(Cur) == TokenType.KEYWORD && (Cur.Value == "println" || Cur.Value == "print")) {
            bool isPrintln = Cur.Value == "println";
            Advance();
            List<ASTNode> args;
            if (GetTokenType(Cur) == TokenType.LPAREN) {
                Advance();
                args = new List<ASTNode>();
                if (GetTokenType(Cur) != TokenType.RPAREN) args.Add(ParseExpr());
                Expect(TokenType.RPAREN, "Expected ')'");
            } else {
                args = [ParseExpr()];
            }
            if (GetTokenType(Cur) == TokenType.SEMICOLON) Advance();
            return new CallExpr(isPrintln ? "println" : "print", args);
        }
        // 字面量关键词: true, false, null
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "true") { Advance(); return new BoolLiteral(true); }
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "false") { Advance(); return new BoolLiteral(false); }
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "null") { Advance(); return new IntLiteral(0); }
        if (GetTokenType(Cur) == TokenType.LBRACE) return ParseBlock();
        if (GetTokenType(Cur) == TokenType.SEMICOLON) { Advance(); return new Block([]); }
        return ParseExprStmt();
    }

    ASTNode ParseVarDecl(bool isVal) {
        Advance(); // val/var
        string name = Expect(TokenType.IDENTIFIER, "期望变量名").Value;
        string type = "Int";
        if (GetTokenType(Cur) == TokenType.COLON) { Advance(); type = Advance().Value; } // : Type
        ASTNode? init = null;
        if (Match(TokenType.ASSIGN)) init = ParseExpr();
        if (GetTokenType(Cur) == TokenType.SEMICOLON) Advance();
        return new VarDecl(name, init, isVal, type);
    }

    ASTNode ParseIf() {
        Advance(); // if
        Expect(TokenType.LPAREN, "Expected '('");
        var cond = ParseExpr();
        Expect(TokenType.RPAREN, "Expected ')'");
        var then = ParseStatement();
        ASTNode? @else = null;
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "else") {
            Advance();
            @else = ParseStatement();
        }
        return new IfStmt(cond, then, @else);
    }

    ASTNode ParseBlockOrSingle() {
        if (GetTokenType(Cur) == TokenType.LBRACE) return ParseBlock();
        return new Block([ParseStatement()]);
    }
    ASTNode ParseTryCatch() {
        Advance(); // try
        var body = ParseBlockOrSingle();
        var catches = new List<CatchClause>();
        while (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "catch") {
            Advance();
            string? varName = null; string? excType = null;
            if (GetTokenType(Cur) == TokenType.LPAREN) {
                Advance();
                var excStr = new System.Text.StringBuilder();
                while (GetTokenType(Cur) != TokenType.RPAREN && GetTokenType(Cur) != TokenType.EOF) {
                    if (GetTokenType(Cur) == TokenType.IDENTIFIER) {
                        varName = Cur.Value; excType = varName;
                    }
                    Advance();
                }
                if (GetTokenType(Cur) == TokenType.RPAREN) Advance();
            }
            var catchBody = ParseBlockOrSingle();
            catches.Add(new CatchClause(varName, excType, catchBody));
        }
        ASTNode? finallyBlock = null;
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "finally") { Advance(); finallyBlock = ParseBlockOrSingle(); }
        return new TryStmt(body, catches, finallyBlock);
    }
    ASTNode ParseDoWhile() {
        Advance(); // do
        var body = ParseBlockOrSingle();
        Expect(TokenType.KEYWORD, "Expected 'while'"); // Expect already consumes 'while'
        Expect(TokenType.LPAREN, "Expected '('");
        var cond = ParseExpr();
        Expect(TokenType.RPAREN, "Expected ')'");
        return new DoWhileStmt(cond, body);
    }
    ASTNode ParseWhile() {
        Advance(); // while
        Expect(TokenType.LPAREN, "Expected '('");
        var cond = ParseExpr();
        Expect(TokenType.RPAREN, "Expected ')'");
        return new WhileStmt(cond, ParseBlockOrSingle());
    }

    ASTNode ParseFor() {
        Advance(); // for
        Expect(TokenType.LPAREN, "Expected '('");
        string varName = Expect(TokenType.IDENTIFIER, "期望变量名").Value;
        Expect(TokenType.KEYWORD, "Expected 'in'"); // in
        var start = ParseExpr();
        string kind = "..";
        if (GetTokenType(Cur) == TokenType.DOTDOT) { Advance(); }
        else if (Cur.Value == "until") { Advance(); kind = "until"; }
        else if (Cur.Value == "downTo") { Advance(); kind = "downTo"; }
        else Expect(TokenType.DOTDOT, "期望 '..'、'until' 或 'downTo'");
        var end = ParseExpr();
        ASTNode? step = null;
        if (Cur.Value == "step") { Advance(); step = ParseExpr(); }
        Expect(TokenType.RPAREN, "Expected ')'");
        return new ForStmt(varName, start, end, ParseBlockOrSingle(), kind, step);
    }

    ASTNode ParseWhen() {
        Advance(); // when
        Expect(TokenType.LPAREN, "Expected '('");
        var value = ParseExpr();
        Expect(TokenType.RPAREN, "Expected ')'");
        Expect(TokenType.LBRACE, "Expected '{'");
        var branches = new List<WhenBranch>();
        ASTNode? elseBranch = null;
        while (GetTokenType(Cur) != TokenType.RBRACE && GetTokenType(Cur) != TokenType.EOF) {
            if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "else") {
                Advance(); // else
                Expect(TokenType.ARROW, "Expected '->'");
                elseBranch = ParseStatement();
            } else if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "in") {
                Advance(); // in
                var rangeStart = ParseExpr();
                if (GetTokenType(Cur) == TokenType.DOTDOT) { Advance(); var rangeEnd = ParseExpr(); Expect(TokenType.ARROW, "Expected '->'"); branches.Add(new WhenBranch(new BinaryOp("in", new VarRef("__when_val__"), new BinaryOp("..", rangeStart, rangeEnd)), ParseStatement())); }
                else { Expect(TokenType.ARROW, "Expected '->'"); var body = ParseStatement(); branches.Add(new WhenBranch(new BinaryOp("in", new VarRef("__when_val__"), rangeStart), body)); }
            } else if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "is") {
                Advance(); // is
                string typeName = GetTokenType(Cur) == TokenType.KEYWORD ? Advance().Value : Expect(TokenType.IDENTIFIER, "期望类型名在 'is' 后").Value;
                Expect(TokenType.ARROW, "Expected '->'");
                branches.Add(new WhenBranch(new BinaryOp("is", new VarRef("__when_val__"), new VarRef(typeName)), ParseStatement()));
            } else {
                var cond = ParseExpr();
                Expect(TokenType.ARROW, "Expected '->'");
                var body = ParseStatement();
                branches.Add(new WhenBranch(cond, body));
            }
        }
        Expect(TokenType.RBRACE, "Expected '}'");
        return new WhenStmt(value, branches, elseBranch);
    }

    ASTNode ParseClassDecl() {
        Advance(); // class
        string name = Expect(TokenType.IDENTIFIER, "期望类名").Value;
        var tparams = ParseTypeParams(); // generic type params (ignored in MCU mode)
        var props = new List<(string, string)>();

        // Optional constructor params: class Pt(x:Int, y:Int) or class Pt(val x:Int, val y:Int)
        if (GetTokenType(Cur) == TokenType.LPAREN) {
            Advance(); // (
            while (GetTokenType(Cur) != TokenType.RPAREN) {
                if (props.Count > 0) Expect(TokenType.COMMA, "Expected ','");
                bool isVal = Cur.Value == "val" || Cur.Value == "var";
                if (isVal) Advance();
                string pname = Expect(TokenType.IDENTIFIER, "期望属性名").Value;
                if (GetTokenType(Cur) == TokenType.COLON) { Advance(); string ptype = Advance().Value; props.Add((pname, ptype)); }
                else { props.Add((pname, "Int")); }
            }
            Expect(TokenType.RPAREN, "Expected ')'");
        }

        // Parse optional interface implementation: : InterfaceName, InterfaceName2
        var interfaces = new List<string>();
        if (GetTokenType(Cur) == TokenType.COLON) {
            Advance(); // :
            while (true) {
                string ifaceName = Expect(TokenType.IDENTIFIER, "期望接口名在 ':' 后").Value;
                interfaces.Add(ifaceName);
                if (GetTokenType(Cur) == TokenType.COMMA) { Advance(); }
                else break;
            }
        }
        // Parse optional body with methods and properties
        var methods = new List<FunctionDecl>();
        if (GetTokenType(Cur) == TokenType.LBRACE) {
            Advance(); // {
            while (GetTokenType(Cur) != TokenType.RBRACE && GetTokenType(Cur) != TokenType.EOF) {
                if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "fun") {
                    var fn = ParseFunction();
                    if (fn is FunctionDecl fd) methods.Add(fd);
                } else if (Cur.Value == "var" || Cur.Value == "val") {
                    // Parse body property: var/val name:Type = init
                    Advance(); // var or val
                    string pname = Expect(TokenType.IDENTIFIER, "期望属性名").Value;
                    string ptype = "Int";
                    if (GetTokenType(Cur) == TokenType.COLON) { Advance(); ptype = Advance().Value; }
                    if (GetTokenType(Cur) == TokenType.EQ) {
                        Advance(); // =
                        if (GetTokenType(Cur) == TokenType.NUMBER) Advance();
                        else ParseExpr(); // parse and discard non-numeric init
                    }
                    props.Add((pname, ptype));
                } else { Advance(); }
            }
            Expect(TokenType.RBRACE, "Expected '}'");
        }
        return new ClassDecl(name, props, methods, false, interfaces);
    }

    ASTNode ParseDataClass() {
        Advance(); // data
        Expect(TokenType.KEYWORD, "期望 'class' 在 'data' 后"); // class
        string name = Expect(TokenType.IDENTIFIER, "期望类名").Value;
        Expect(TokenType.LPAREN, "Expected '('");
        var props = new List<(string, string)>();
        while (GetTokenType(Cur) != TokenType.RPAREN) {
            if (props.Count > 0) Expect(TokenType.COMMA, "Expected ','");
            bool isVal = Cur.Value == "val" || Cur.Value == "var";
            if (isVal) Advance();
            string pname = Expect(TokenType.IDENTIFIER, "期望属性名").Value;
            if (GetTokenType(Cur) == TokenType.COLON) { Advance(); string ptype = Advance().Value; props.Add((pname, ptype)); }
            else { props.Add((pname, "Int")); }
        }
        Expect(TokenType.RPAREN, "Expected ')'");
        // Parse optional interface implementation: : InterfaceName
        var interfaces = new List<string>();
        if (GetTokenType(Cur) == TokenType.COLON) {
            Advance();
            while (true) {
                interfaces.Add(Expect(TokenType.IDENTIFIER, "期望接口名").Value);
                if (GetTokenType(Cur) == TokenType.COMMA) { Advance(); }
                else break;
            }
        }
        var methods = new List<FunctionDecl>();
        if (GetTokenType(Cur) == TokenType.LBRACE) {
            Advance();
            while (GetTokenType(Cur) != TokenType.RBRACE && GetTokenType(Cur) != TokenType.EOF) {
                if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "fun") {
                    var fn = ParseFunction();
                    if (fn is FunctionDecl fd) methods.Add(fd);
                } else { Advance(); }
            }
            Expect(TokenType.RBRACE, "Expected '}'");
        }
        return new ClassDecl(name, props, methods, true, interfaces);
    }

    ASTNode ParseReturn() {
        Advance(); // return
        if (GetTokenType(Cur) == TokenType.RBRACE || GetTokenType(Cur) == TokenType.EOF) return new ReturnStmt(null);
        var r = new ReturnStmt(ParseExpr());
        if (GetTokenType(Cur) == TokenType.SEMICOLON) Advance();
        return r;
    }

    /// <summary>赋值运算符集合（`=` / `+=` / `-=` / `*=` / `/=` / `%=`）的唯一判据。</summary>
    static bool IsAssignOp(TokenType t) =>
        t is TokenType.ASSIGN or TokenType.PLUS_ASSIGN or TokenType.MINUS_ASSIGN
          or TokenType.STAR_ASSIGN or TokenType.SLASH_ASSIGN or TokenType.PERCENT_ASSIGN;

    ASTNode ParseExprStmt() {
        var expr = ParseExpr();
        if (GetTokenType(Cur) == TokenType.SEMICOLON) Advance();
        if (GetTokenType(Cur) == TokenType.NEWLINE) Advance();
        return expr;
    }

    ASTNode ParseExpr() => ParseOr();

    ASTNode ParseOr() {
        var left = ParseAnd();
        while (GetTokenType(Cur) == TokenType.OR) { Advance(); left = new BinaryOp("||", left, ParseAnd()); }
        if (GetTokenType(Cur) == TokenType.ELVIS) { Advance(); left = new ElvisExpr(left, ParseOr()); }
        return left;
    }

    ASTNode ParseAnd() {
        var left = ParseComparison();
        while (GetTokenType(Cur) == TokenType.AND) { Advance(); left = new BinaryOp("&&", left, ParseComparison()); }
        return left;
    }

    ASTNode ParseComparison() {
        var left = ParseAddSub();
        if (GetTokenType(Cur) is TokenType.EQ or TokenType.NE or TokenType.LT or TokenType.GT or TokenType.LE or TokenType.GE) {
            var op = Advance().Value;
            return new BinaryOp(op, left, ParseAddSub());
        }
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "is") {
            Advance(); // is
            string typeName = GetTokenType(Cur) == TokenType.KEYWORD ? Advance().Value : Expect(TokenType.IDENTIFIER, "期望类型名在 'is' 后").Value;
            return new BinaryOp("is", left, new VarRef(typeName));
        }
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "in") {
            Advance(); // in
            var rangeStart = ParseExpr();
            if (GetTokenType(Cur) == TokenType.DOTDOT) {
                Advance(); // ..
                var rangeEnd = ParseExpr();
                return new BinaryOp("in", left, new BinaryOp("..", rangeStart, rangeEnd));
            }
            return new BinaryOp("in", left, rangeStart);
        }
        return left;
    }

    ASTNode ParseAddSub() {
        var left = ParseMulDiv();
        while (GetTokenType(Cur) is TokenType.PLUS or TokenType.MINUS) {
            var op = Advance().Value;
            left = new BinaryOp(op, left, ParseMulDiv());
        }
        return left;
    }

    ASTNode ParseMulDiv() {
        var left = ParseUnary();
        while (GetTokenType(Cur) is TokenType.STAR or TokenType.SLASH or TokenType.PERCENT) {
            var op = Advance().Value;
            left = new BinaryOp(op, left, ParseUnary());
        }
        return left;
    }

    ASTNode ParseUnary() {
        if (GetTokenType(Cur) == TokenType.MINUS || GetTokenType(Cur) == TokenType.NOT) {
            var op = Advance().Value;
            return new UnaryOp(op, ParseUnary());
        }
        if (GetTokenType(Cur) == TokenType.INCREMENT) {
            Advance(); return new UnaryOp("++", ParseUnary());
        }
        if (GetTokenType(Cur) == TokenType.DECREMENT) {
            Advance(); return new UnaryOp("--", ParseUnary());
        }
        var expr = ParsePrimary();
        // 通用后缀循环: .member, (args), [index], ++, --
        while (true) {
            if (GetTokenType(Cur) == TokenType.INCREMENT) { Advance(); expr = new UnaryOp("++post", expr); continue; }
            if (GetTokenType(Cur) == TokenType.DECREMENT) { Advance(); expr = new UnaryOp("--post", expr); continue; }
            if (GetTokenType(Cur) == TokenType.DOT) {
                Advance();
                string member = Expect(TokenType.IDENTIFIER, "期望成员名").Value;
                expr = new MemberAccess(expr, member);
                continue;
            }
            if (GetTokenType(Cur) == TokenType.SAFE_DOT) {
                Advance();
                string member = Expect(TokenType.IDENTIFIER, "期望成员名").Value;
                expr = new SafeCallExpr(expr, member);
                continue;
            }
            if (GetTokenType(Cur) == TokenType.LPAREN) {
                Advance();
                var args = new List<ASTNode>();
                while (GetTokenType(Cur) != TokenType.RPAREN) {
                    if (args.Count > 0 && GetTokenType(Cur) != TokenType.COMMA) {
                        while (GetTokenType(Cur) != TokenType.COMMA && GetTokenType(Cur) != TokenType.RPAREN && GetTokenType(Cur) != TokenType.EOF)
                            Advance();
                    }
                    if (args.Count > 0) {
                        if (GetTokenType(Cur) == TokenType.COMMA) Advance();
                        else break;
                    }
                    args.Add(ParseExpr());
                }
                Expect(TokenType.RPAREN, "Expected ')'");
                expr = new CallExpr("__lambda", args);
                continue;
            }
            if (GetTokenType(Cur) == TokenType.LBRACKET) {
                Advance();
                var idx = ParseExpr();
                Expect(TokenType.RBRACKET, "Expected ']'");
                expr = new IndexExpr(expr, idx);
                continue;
            }
            // !! 非空断言: expr!!
            if (GetTokenType(Cur) == TokenType.NOT && GetTokenType(Peek(1)) == TokenType.NOT) {
                Advance(); Advance(); // consume both !!
                expr = new NotNullAssert(expr);
                continue;
            }
            break;
        }
        return expr;
    }

    /// <summary>
    /// **原子表达式的唯一入口** —— 顺手盖上它自己的行列（`ASTNode.Line`/`Column`\uff09。
    ///
    /// 语句级的列只能给到「这一句从哪开始」，而用户报错时要看到的是**出错的那个标识符**
    /// 从哪开始。原子是位置信息真正有意义的地方，而全部表达式都是从这里递归产出的。
    /// </summary>
    ASTNode ParsePrimary() {
        var __start = Cur;
        int __line = __start.Line, __col = __start.Column;
        var __node = ParsePrimaryCore();
        if (__node != null && __node.Line == 0) { __node.Line = __line; __node.Column = __col; }
        return __node;
    }

    ASTNode ParsePrimaryCore() {
        if (GetTokenType(Cur) == TokenType.LPAREN) { Advance(); var e = ParseExpr(); int pdepth = 1; while (pdepth > 0 && GetTokenType(Cur) != TokenType.EOF) { if (GetTokenType(Cur) == TokenType.LPAREN) pdepth++; else if (GetTokenType(Cur) == TokenType.RPAREN) pdepth--; if (pdepth > 0) Advance(); } Expect(TokenType.RPAREN, "Expected ')'"); return e; }
        if (GetTokenType(Cur) == TokenType.KEYWORD) {
            string kw = Cur.Value;
            if (kw == "true") { Advance(); return new BoolLiteral(true); }
            if (kw == "false") { Advance(); return new BoolLiteral(false); }
            if (kw == "null") { Advance(); return new IntLiteral(0); }
        }
        if (GetTokenType(Cur) == TokenType.NUMBER) { var v = Advance().Value; char last = v[^1]; bool hasL = last == 'L' || last == 'l'; bool hasF = last == 'f' || last == 'F'; bool hasD = last == 'd' || last == 'D'; if (hasL || hasF || hasD) v = v[..^1]; if (v.Contains('.')) { if (hasF) return new FloatLiteral((float)double.Parse(v)); return new DoubleLiteral(double.Parse(v)); } if (hasL || !int.TryParse(v, out _)) return new IntLiteral(long.Parse(v)); return new IntLiteral(int.Parse(v)); }
        if (GetTokenType(Cur) == TokenType.STRING) {
            var s = new StringLiteral(Advance().Value);
            // Check for [ index ]
            if (GetTokenType(Cur) == TokenType.LBRACKET) {
                Advance(); // [
                var idx = ParseExpr();
                Expect(TokenType.RBRACKET, "Expected ']'");
                return new IndexExpr(s, idx);
            }
            return s;
        }
        if (GetTokenType(Cur) == TokenType.IDENTIFIER) {
            string name = Advance().Value;
            if (GetTokenType(Cur) == TokenType.LPAREN) {
                Advance();
                var args = new List<ASTNode>();
                while (GetTokenType(Cur) != TokenType.RPAREN) {
                    if (args.Count > 0 && GetTokenType(Cur) != TokenType.COMMA) {
                        // 容错跳过中缀函数(to等): 跳过直到逗号或右括号
                        while (GetTokenType(Cur) != TokenType.COMMA && GetTokenType(Cur) != TokenType.RPAREN && GetTokenType(Cur) != TokenType.EOF)
                            Advance();
                    }
                    if (args.Count > 0) {
                        if (GetTokenType(Cur) == TokenType.COMMA) Advance();
                        else break; // 容错退出
                    }
                    args.Add(ParseExpr());
                }
                Expect(TokenType.RPAREN, "Expected ')'");
                // Check if this is a class constructor call (class name used as function)
                if (char.IsUpper(name[0]) && !name.All(char.IsUpper)) {
                    return new NewExpr(name, args);
                }
                return new CallExpr(name, args);
            }
            if (GetTokenType(Cur) == TokenType.DOT) {
                ASTNode result = new VarRef(name);
                while (GetTokenType(Cur) == TokenType.DOT || GetTokenType(Cur) == TokenType.SAFE_DOT) {
                    if (GetTokenType(Cur) == TokenType.SAFE_DOT) {
                        Advance(); // ?.
                        string safeMember = Expect(TokenType.IDENTIFIER, "期望成员名").Value;
                        result = new SafeCallExpr(result, safeMember);
                        break; // SAFE_DOT terminates the chain
                    }
                    Advance(); // .
                    string member = Expect(TokenType.IDENTIFIER, "期望成员名").Value;
                    if (GetTokenType(Cur) == TokenType.LPAREN) {
                        Advance();
                        var args = new List<ASTNode>();
                        while (GetTokenType(Cur) != TokenType.RPAREN) {
                            if (args.Count > 0 && GetTokenType(Cur) != TokenType.COMMA) {
                                while (GetTokenType(Cur) != TokenType.COMMA && GetTokenType(Cur) != TokenType.RPAREN && GetTokenType(Cur) != TokenType.EOF)
                                    Advance();
                            }
                            if (args.Count > 0) {
                                if (GetTokenType(Cur) == TokenType.COMMA) Advance();
                                else break;
                            }
                            args.Add(ParseExpr());
                        }
                        Expect(TokenType.RPAREN, "Expected ')'");
                        args.Insert(0, result);
                        string classPrefix = char.IsUpper(member[0]) ? "" : "";
                        result = new CallExpr(member, args);
                    } else {
                        result = new MemberAccess(result, member);
                    }
                }
                // Check for assignment after member access: p.x = 5
                if (IsAssignOp(GetTokenType(Cur))) {
                    string op = Advance().Value;
                    return new AssignStmt(name, ParseExpr(), op, result);
                }
                return result;
            }
            if (GetTokenType(Cur) == TokenType.SAFE_DOT) {
                Advance(); // ?.
                string member = Expect(TokenType.IDENTIFIER, "期望成员名").Value;
                return new SafeCallExpr(new VarRef(name), member);
            }
            if (IsAssignOp(GetTokenType(Cur))) {
                string op = Advance().Value;
                return new AssignStmt(name, ParseExpr(), op);
            }
            // Check for [ index ]
            if (GetTokenType(Cur) == TokenType.LBRACKET) {
                Advance(); // [
                var idx = ParseExpr();
                Expect(TokenType.RBRACKET, "Expected ']'");
                ASTNode indexTarget = new IndexExpr(new VarRef(name), idx);
                // ⚠ 下标赋值：`a[i] = v` / `a[i] += v`。原来这里直接 `return indexTarget`，
                //   后面那个 `=` 没人接 ⇒ ParseExprStmt 完事后撞上 ASSIGN，
                //   报 "Unexpected token: ASSIGN '='"（整份文件解析失败）。
                //   与上面成员访问 `p.x = v` 的收尾（:594）是同一件事，条件也照抄。
                if (IsAssignOp(GetTokenType(Cur))) {
                    string aop = Advance().Value;
                    return new AssignStmt(name, ParseExpr(), aop, indexTarget);
                }
                return indexTarget;
            }
            return new VarRef(name);
        }
        if (GetTokenType(Cur) == TokenType.KEYWORD && (Cur.Value == "true" || Cur.Value == "false"))
            return new BoolLiteral(Advance().Value == "true");
        if (GetTokenType(Cur) == TokenType.LBRACE && GetTokenType(Peek(1)) != TokenType.RBRACE) {
            // Lambda: { params -> body } or just { body } with implicit 'it'
            Advance(); // {
            // Check if this looks like a lambda with params: id (->) or id : type (->)
            bool looksLikeLambda = false;
            int savedPos = _pos;
            try {
                if (GetTokenType(Cur) == TokenType.IDENTIFIER && GetTokenType(Peek(1)) == TokenType.ARROW) looksLikeLambda = true;
                if (GetTokenType(Cur) == TokenType.IDENTIFIER && GetTokenType(Peek(1)) == TokenType.COLON) looksLikeLambda = true;
            } catch { }
            if (looksLikeLambda) {
                var pars = new List<string>();
                while (GetTokenType(Cur) != TokenType.ARROW && GetTokenType(Cur) != TokenType.RBRACE && GetTokenType(Cur) != TokenType.EOF) {
                    if (pars.Count > 0 && GetTokenType(Cur) == TokenType.COMMA) { Advance(); continue; }
                    string pname = Expect(TokenType.IDENTIFIER, "期望参数名").Value;
                    if (GetTokenType(Cur) == TokenType.COLON) { Advance(); Advance(); }
                    pars.Add(pname);
                }
                if (GetTokenType(Cur) == TokenType.ARROW) Advance(); // ->
                var body = ParseExpr();
                Expect(TokenType.RBRACE, "期望 '}' 在 lambda 中");
                return new LambdaExpr(pars, body);
            } else {
                // Implicit 'it' parameter lambda: { expr }
                var body = ParseExpr();
                Expect(TokenType.RBRACE, "期望 '}' 在 lambda 中");
                return new LambdaExpr(["it"], body);
            }
        }
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "when")
            return ParseWhen();
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "if")
            return ParseIf();
        throw Error($"意外的 token: {GetTokenType(Cur)} '{Cur.Value}'（位置 {Cur.Line}:{Cur.Column}）");
    }
}
