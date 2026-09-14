using CompilerBase;

namespace KotlinCompiler;

public class Parser : ParserBase<Token, TokenType>
{
    protected override TokenType GetTokenType(Token token) => token.Type;

    bool _pendingExternal = false;

    public Parser(List<Token> tokens) : base(tokens) { }

    protected override Token Expect(TokenType t, string msg) =>
        Check(t) ? Advance() : throw Error($"{msg} at {Cur.Line}:{Cur.Column}, got {GetTokenType(Cur)} '{Cur.Value}'");

    public Program Parse() {
        var funcs = new List<ASTNode>();
        while (GetTokenType(Cur) != TokenType.EOF) {
            if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "external") { _pendingExternal = true; Advance(); continue; }
            if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "fun") funcs.Add(ParseFunction());
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
        string name = Expect(TokenType.IDENTIFIER, "Expected interface name").Value;
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
                tparams.Add(Expect(TokenType.IDENTIFIER, "Expected type parameter name").Value);
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
        string name = Expect(TokenType.IDENTIFIER, "Expected function name").Value;
        // Extension function: fun ReceiverType.methodName(...)
        string? receiverType = null;
        if (GetTokenType(Cur) == TokenType.DOT) {
            Advance(); // .
            receiverType = name; // First identifier is the receiver type
            name = Expect(TokenType.IDENTIFIER, "Expected function name after .").Value;
        }
        Expect(TokenType.LPAREN, "Expected '('");
        var pars = new List<string>();
        while (GetTokenType(Cur) != TokenType.RPAREN) {
            if (pars.Count > 0) Expect(TokenType.COMMA, "Expected ','");
            Expect(TokenType.IDENTIFIER, "Expected param name");
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

    ASTNode ParseStatement() {
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
        string name = Expect(TokenType.IDENTIFIER, "Expected variable name").Value;
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
        string varName = Expect(TokenType.IDENTIFIER, "Expected variable name").Value;
        Expect(TokenType.KEYWORD, "Expected 'in'"); // in
        var start = ParseExpr();
        string kind = "..";
        if (GetTokenType(Cur) == TokenType.DOTDOT) { Advance(); }
        else if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "until") { Advance(); kind = "until"; }
        else if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "downTo") { Advance(); kind = "downTo"; }
        else Expect(TokenType.DOTDOT, "Expected '..', 'until', or 'downTo'");
        var end = ParseExpr();
        ASTNode? step = null;
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "step") { Advance(); step = ParseExpr(); }
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
                string typeName = GetTokenType(Cur) == TokenType.KEYWORD ? Advance().Value : Expect(TokenType.IDENTIFIER, "Expected type name after 'is'").Value;
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
        string name = Expect(TokenType.IDENTIFIER, "Expected class name").Value;
        var tparams = ParseTypeParams(); // generic type params (ignored in MCU mode)
        var props = new List<(string, string)>();

        // Optional constructor params: class Pt(x:Int, y:Int) or class Pt(val x:Int, val y:Int)
        if (GetTokenType(Cur) == TokenType.LPAREN) {
            Advance(); // (
            while (GetTokenType(Cur) != TokenType.RPAREN) {
                if (props.Count > 0) Expect(TokenType.COMMA, "Expected ','");
                bool isVal = Cur.Value == "val" || Cur.Value == "var";
                if (isVal) Advance();
                string pname = Expect(TokenType.IDENTIFIER, "Expected property name").Value;
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
                string ifaceName = Expect(TokenType.IDENTIFIER, "Expected interface name after ':'").Value;
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
                    string pname = Expect(TokenType.IDENTIFIER, "Expected property name").Value;
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
        Expect(TokenType.KEYWORD, "Expected 'class' after 'data'"); // class
        string name = Expect(TokenType.IDENTIFIER, "Expected class name").Value;
        Expect(TokenType.LPAREN, "Expected '('");
        var props = new List<(string, string)>();
        while (GetTokenType(Cur) != TokenType.RPAREN) {
            if (props.Count > 0) Expect(TokenType.COMMA, "Expected ','");
            bool isVal = Cur.Value == "val" || Cur.Value == "var";
            if (isVal) Advance();
            string pname = Expect(TokenType.IDENTIFIER, "Expected property name").Value;
            if (GetTokenType(Cur) == TokenType.COLON) { Advance(); string ptype = Advance().Value; props.Add((pname, ptype)); }
            else { props.Add((pname, "Int")); }
        }
        Expect(TokenType.RPAREN, "Expected ')'");
        // Parse optional interface implementation: : InterfaceName
        var interfaces = new List<string>();
        if (GetTokenType(Cur) == TokenType.COLON) {
            Advance();
            while (true) {
                interfaces.Add(Expect(TokenType.IDENTIFIER, "Expected interface name").Value);
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
            string typeName = GetTokenType(Cur) == TokenType.KEYWORD ? Advance().Value : Expect(TokenType.IDENTIFIER, "Expected type name after 'is'").Value;
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
                string member = Expect(TokenType.IDENTIFIER, "Expected member name").Value;
                expr = new MemberAccess(expr, member);
                continue;
            }
            if (GetTokenType(Cur) == TokenType.SAFE_DOT) {
                Advance();
                string member = Expect(TokenType.IDENTIFIER, "Expected member name").Value;
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

    ASTNode ParsePrimary() {
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
                        string safeMember = Expect(TokenType.IDENTIFIER, "Expected member name").Value;
                        result = new SafeCallExpr(result, safeMember);
                        break; // SAFE_DOT terminates the chain
                    }
                    Advance(); // .
                    string member = Expect(TokenType.IDENTIFIER, "Expected member name").Value;
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
                if (GetTokenType(Cur) == TokenType.ASSIGN || GetTokenType(Cur) == TokenType.PLUS_ASSIGN || GetTokenType(Cur) == TokenType.MINUS_ASSIGN || GetTokenType(Cur) == TokenType.STAR_ASSIGN || GetTokenType(Cur) == TokenType.SLASH_ASSIGN || GetTokenType(Cur) == TokenType.PERCENT_ASSIGN) {
                    string op = Advance().Value;
                    return new AssignStmt(name, ParseExpr(), op, result);
                }
                return result;
            }
            if (GetTokenType(Cur) == TokenType.SAFE_DOT) {
                Advance(); // ?.
                string member = Expect(TokenType.IDENTIFIER, "Expected member name").Value;
                return new SafeCallExpr(new VarRef(name), member);
            }
            if (GetTokenType(Cur) == TokenType.ASSIGN || GetTokenType(Cur) == TokenType.PLUS_ASSIGN || GetTokenType(Cur) == TokenType.MINUS_ASSIGN || GetTokenType(Cur) == TokenType.STAR_ASSIGN || GetTokenType(Cur) == TokenType.SLASH_ASSIGN || GetTokenType(Cur) == TokenType.PERCENT_ASSIGN) {
                string op = Advance().Value;
                return new AssignStmt(name, ParseExpr(), op);
            }
            // Check for [ index ]
            if (GetTokenType(Cur) == TokenType.LBRACKET) {
                Advance(); // [
                var idx = ParseExpr();
                Expect(TokenType.RBRACKET, "Expected ']'");
                return new IndexExpr(new VarRef(name), idx);
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
                    string pname = Expect(TokenType.IDENTIFIER, "Expected parameter name").Value;
                    if (GetTokenType(Cur) == TokenType.COLON) { Advance(); Advance(); }
                    pars.Add(pname);
                }
                if (GetTokenType(Cur) == TokenType.ARROW) Advance(); // ->
                var body = ParseExpr();
                Expect(TokenType.RBRACE, "Expected '}' in lambda");
                return new LambdaExpr(pars, body);
            } else {
                // Implicit 'it' parameter lambda: { expr }
                var body = ParseExpr();
                Expect(TokenType.RBRACE, "Expected '}' in lambda");
                return new LambdaExpr(["it"], body);
            }
        }
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "when")
            return ParseWhen();
        if (GetTokenType(Cur) == TokenType.KEYWORD && Cur.Value == "if")
            return ParseIf();
        throw Error($"Unexpected token: {GetTokenType(Cur)} '{Cur.Value}' at {Cur.Line}:{Cur.Column}");
    }
}
