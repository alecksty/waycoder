using System.Collections.Generic;
using CompilerBase;

namespace DCompiler;

public class Parser : ParserBase<Token, TokenType>
{
    /// <summary>多变量声明中额外变量的暂存列表, ParseBlock 中注入</summary>
    private List<ASTNode> _extraDeclarations = new();

    protected override TokenType GetTokenType(Token token) => token.Type;

    protected override Token Expect(TokenType t, string msg) =>
        base.Expect(t, $"D parse error: {msg} (got {Cur.Type})");

    public Parser(List<Token> tokens) : base(tokens) { }

    public ProgramNode Parse()
    {
        var prog = new ProgramNode();
        while (!IsAtEnd)
            ParseTopLevel(prog);
        return prog;
    }

    private void ParseTopLevel(ProgramNode prog)
    {
        SkipComments();
        if (Check(TokenType.EOF)) return;
        prog.Statements.Add(ParseDeclaration());
    }

    // ---- Declarations ----

    private ASTNode ParseDeclaration()
    {
        SkipComments();
        if (Match(TokenType.Module)) return ParseModuleDecl();
        if (Match(TokenType.Import)) return ParseImportDecl();
        // D 构造函数: this(params) { body }
        if (Match(TokenType.This) && Check(TokenType.LParen))
        {
            return ParseFuncDef("this", "void");
        }

        if (IsTypeKeyword() || Check(TokenType.Identifier))
        {
            // look ahead to distinguish variable decl from function decl
            string type = ParseType();
            // D 静态数组类型: int[3] name
            while (Match(TokenType.LBracket))
            {
                if (!Check(TokenType.RBracket))
                    ParseExpression(); // 维度表达式
                Expect(TokenType.RBracket, "expected ']'");
                type += "[]";
            }
            string name = Expect(TokenType.Identifier, "expected name").Value;
            if (Check(TokenType.LParen))
                return ParseFuncDef(name, type);

            // 支持多变量声明: int x, y, z;
            int l = Cur.Line, c = Cur.Column;
            ASTNode? init = null;
            if (Match(TokenType.Assign))
                init = ParseExpression();

            var result = new VarDeclNode(name, type, init, l, c);

            // 收集后续变量名: int x, y=1, z = 0;
            while (Match(TokenType.Comma))
            {
                string extraName = Expect(TokenType.Identifier, "expected variable name").Value;
                ASTNode? extraInit = null;
                if (Match(TokenType.Assign))
                    extraInit = ParseExpression();
                // 将额外变量加入 _extraDeclarations 以便在 ParseBlock 中注入
                _extraDeclarations.Add(new VarDeclNode(extraName, type, extraInit, l, c));
            }

            Expect(TokenType.Semicolon, "expected ';'");
            return result;
        }

        if (Match(TokenType.Class)) return ParseClassDecl();
        if (Match(TokenType.Struct)) return ParseStructDecl();
        if (Match(TokenType.Interface)) return ParseInterfaceDecl();
        if (Match(TokenType.Enum)) return ParseEnumDecl();

        // standalone function (no return type specified, default to void)
        if (Check(TokenType.Identifier) && Peek(1).Type == TokenType.LParen)
        {
            string name = Advance().Value;
            return ParseFuncDef(name, "void");
        }

        return ParseStatement();
    }

    private bool IsTypeKeyword()
    {
        return Check(TokenType.Void) || Check(TokenType.Int) ||
               Check(TokenType.Float) || Check(TokenType.Double) ||
               Check(TokenType.Long) ||
               Check(TokenType.Bool) || Check(TokenType.String) ||
               Check(TokenType.Char) || Check(TokenType.Auto) ||
               Check(TokenType.Immutable) || Check(TokenType.Const);
    }

    private string ParseType()
    {
        // Skip type qualifiers: immutable int → int, const float → float
        while (Check(TokenType.Immutable) || Check(TokenType.Const))
            Advance();
        if (IsTypeKeyword())
        {
            string t = Advance().Value;
            // auto → default to int
            if (t == "auto") return "int";
            return t;
        }
        if (Check(TokenType.Identifier)) return Advance().Value;
        return "int";
    }

    private ASTNode ParseModuleDecl()
    {
        int l = Cur.Line, c = Cur.Column;
        string name = Expect(TokenType.Identifier, "expected module name").Value;
        Expect(TokenType.Semicolon, "expected ';'");
        return new LiteralNode(null, l, c); // placeholder
    }

    private ASTNode ParseImportDecl()
    {
        int l = Cur.Line, c = Cur.Column;
        string name = Expect(TokenType.Identifier, "expected module name").Value;
        // support import foo.bar;
        while (Match(TokenType.Dot))
            name += "." + Expect(TokenType.Identifier, "expected identifier").Value;
        Expect(TokenType.Semicolon, "expected ';'");
        return new LiteralNode(null, l, c); // placeholder
    }

    private ASTNode ParseFuncDef(string name, string returnType)
    {
        int l = Cur.Line, c = Cur.Column;
        Expect(TokenType.LParen, "expected '('");
        var parms = new List<(string name, string type)>();
        if (!Check(TokenType.RParen))
        {
            do
            {
                string pt = ParseType();
                string pn = Expect(TokenType.Identifier, "expected param name").Value;
                parms.Add((pn, pt));
            } while (Match(TokenType.Comma));
        }
        Expect(TokenType.RParen, "expected ')'");
        var body = ParseBlock();
        return new FuncDefNode(name, returnType, parms, body, l, c);
    }

    private ASTNode ParseVarDecl(string name, string type)
    {
        int l = Cur.Line, c = Cur.Column;
        ASTNode? init = null;
        if (Match(TokenType.Assign))
            init = ParseExpression();
        Expect(TokenType.Semicolon, "expected ';'");
        return new VarDeclNode(name, type, init, l, c);
    }

    private ASTNode ParseClassDecl()
    {
        int l = Cur.Line, c = Cur.Column;
        string name = Expect(TokenType.Identifier, "expected class name").Value;
        // D 模板声明: class Box(T) { ... } — 跳过模板参数列表
        if (Match(TokenType.LParen))
        {
            int depth = 1;
            while (depth > 0 && !Check(TokenType.EOF))
            {
                if (Check(TokenType.LParen)) { depth++; Advance(); }
                else if (Check(TokenType.RParen)) { depth--; Advance(); }
                else Advance();
            }
        }
        Expect(TokenType.LBrace, "expected '{'");
        var members = new List<ASTNode>();
        while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
        {
            SkipComments();
            if (Check(TokenType.RBrace)) break;
            members.Add(ParseDeclaration());
        }
        Expect(TokenType.RBrace, "expected '}'");
        Match(TokenType.Semicolon);
        return new ClassDeclNode(name, members, l, c);
    }

    private ASTNode ParseStructDecl()
    {
        int l = Cur.Line, c = Cur.Column;
        string name = Expect(TokenType.Identifier, "expected struct name").Value;
        Expect(TokenType.LBrace, "expected '{'");
        var members = new List<ASTNode>();
        while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
        {
            SkipComments();
            if (Check(TokenType.RBrace)) break;
            members.Add(ParseDeclaration());
        }
        Expect(TokenType.RBrace, "expected '}'");
        Match(TokenType.Semicolon);
        return new ClassDeclNode(name, members, l, c);
    }

    private ASTNode ParseInterfaceDecl()
    {
        int l = Cur.Line, c = Cur.Column;
        string name = Expect(TokenType.Identifier, "expected interface name").Value;
        Expect(TokenType.LBrace, "expected '{'");
        while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
        {
            SkipComments();
            if (Check(TokenType.RBrace)) break;
            ParseDeclaration();
        }
        Expect(TokenType.RBrace, "expected '}'");
        Expect(TokenType.Semicolon, "expected ';'");
        return new LiteralNode(null, l, c); // placeholder
    }

    private ASTNode ParseEnumDecl()
    {
        int l = Cur.Line, c = Cur.Column;
        string name = Expect(TokenType.Identifier, "expected enum name").Value;
        Expect(TokenType.LBrace, "expected '{'");
        while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
        {
            SkipComments();
            if (Check(TokenType.RBrace)) break;
            var _ = Expect(TokenType.Identifier, "expected enum member").Value;
            if (Match(TokenType.Assign)) ParseExpression();
            Match(TokenType.Comma);
        }
        Expect(TokenType.RBrace, "expected '}'");
        Match(TokenType.Semicolon); // D 分号在 enum 后可选
        return new LiteralNode(null, l, c); // placeholder
    }

    // ---- Statements ----

    private List<ASTNode> ParseBlock()
    {
        Expect(TokenType.LBrace, "expected '{'");
        var body = new List<ASTNode>();
        while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
        {
            SkipComments();
            if (Check(TokenType.RBrace)) break;
            var stmt = ParseStatement();
            if (stmt != null)
            {
                body.Add(stmt);
                if (_extraDeclarations.Count > 0)
                {
                    body.AddRange(_extraDeclarations);
                    _extraDeclarations.Clear();
                }
            }
        }
        Expect(TokenType.RBrace, "expected '}'");
        return body;
    }

    /// <summary>解析代码块 { ... } 或单条语句 (用于 if/while/for 等)</summary>
    private List<ASTNode> ParseBlockOrSingle()
    {
        if (Check(TokenType.LBrace))
            return ParseBlock();
        return new List<ASTNode> { ParseStatement() };
    }

    private ASTNode ParseStatement()
    {
        SkipComments();
        // D label: NAME:
        if (Check(TokenType.Identifier) && Peek(1).Type == TokenType.Colon)
        {
            Advance(); Advance(); // skip name and :
            return ParseStatement();
        }
        // D throw expr;
        if (Match(TokenType.Throw))
        {
            if (!Check(TokenType.Semicolon)) ParseExpression();
            Expect(TokenType.Semicolon, "expected ';'");
            return new LiteralNode(null, Cur.Line, Cur.Column);
        }
        // D goto NAME;
        if (Check(TokenType.Identifier) && Cur.Value == "goto")
        {
            Advance(); // goto
            Expect(TokenType.Identifier, "expected label name");
            Expect(TokenType.Semicolon, "expected ';'");
            return new LiteralNode(null, Cur.Line, Cur.Column);
        }
        // D 块语句: { ... }
        if (Check(TokenType.LBrace))
        {
            Advance(); // {
            while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
            {
                SkipComments();
                if (Check(TokenType.RBrace)) break;
                var _ = ParseStatement();
            }
            Expect(TokenType.RBrace, "expected '}'");
            return new LiteralNode(null, Cur.Line, Cur.Column);
        }
        // D try/catch/finally — MCU模式仅执行try体
        if (Match(TokenType.Try))
        {
            ParseBlockOrSingle();
            while (Match(TokenType.Catch))
            {
                if (Match(TokenType.LParen)) { while (!Check(TokenType.RParen)&&!Check(TokenType.EOF)) Advance(); Advance(); }
                ParseBlockOrSingle();
            }
            if (Match(TokenType.Finally)) ParseBlockOrSingle();
            return new LiteralNode(null, Cur.Line, Cur.Column);
        }
        if (Check(TokenType.Return)) return ParseReturn();
        if (Check(TokenType.If)) return ParseIf();
        if (Check(TokenType.While)) return ParseWhile();
        if (Check(TokenType.For)) return ParseFor();
        if (Check(TokenType.Do)) return ParseDoWhile();
        // D switch语句
        if (Match(TokenType.Switch))
            return ParseSwitch();
        // D with语句: with (expr) { ... } — evaluate expr, parse body (simplified)
        if (Match(TokenType.With))
        {
            Expect(TokenType.LParen, "expected '(' after with");
            ParseExpression(); // evaluate with expression (discard for now)
            Expect(TokenType.RParen, "expected ')' after with");
            ParseBlockOrSingle(); // parse body
            return new LiteralNode(null, Cur.Line, Cur.Column);
        }
        // D scope语句: scope(exit/success/failure) { ... } — parse body (deferred exec not yet)
        if (Match(TokenType.Scope))
        {
            Expect(TokenType.LParen, "expected '(' after scope");
            if (Match(TokenType.Identifier)) { /* exit/success/failure */ }
            Expect(TokenType.RParen, "expected ')' after scope");
            ParseBlockOrSingle(); // parse body
            return new LiteralNode(null, Cur.Line, Cur.Column);
        }
        if (Check(TokenType.Foreach)) return ParseForeach();
        // D import 可在函数体内
        if (Match(TokenType.Import))
        {
            while (!Check(TokenType.Semicolon) && !Check(TokenType.EOF)) Advance();
            Expect(TokenType.Semicolon, "expected ';'");
            return new LiteralNode(null, Cur.Line, Cur.Column);
        }
        if (Match(TokenType.Break))
        {
            int l = Cur.Line, c = Cur.Column;
            Expect(TokenType.Semicolon, "expected ';'");
            return new BreakNode(l, c);
        }
        if (Match(TokenType.Continue))
        {
            int l = Cur.Line, c = Cur.Column;
            Expect(TokenType.Semicolon, "expected ';'");
            return new ContinueNode(l, c);
        }

        // variable declaration inside blocks
        if (IsTypeKeyword() || (Check(TokenType.Identifier) && Peek(1).Type == TokenType.Identifier))
        {
            string type = ParseType();
            // D 静态数组类型: int[3] name
            while (Match(TokenType.LBracket))
            {
                if (!Check(TokenType.RBracket))
                    ParseExpression();
                Expect(TokenType.RBracket, "expected ']'");
                type += "[]";
            }
            string name = Expect(TokenType.Identifier, "expected name").Value;
            ASTNode? init = null;
            if (Match(TokenType.Assign))
                init = ParseExpression();
            // 多变量声明: int a = 1, b = 2;
            int declLine = Cur.Line, declCol = Cur.Column;
            var result = new VarDeclNode(name, type, init, declLine, declCol);
            while (Match(TokenType.Comma))
            {
                string extraName = Expect(TokenType.Identifier, "expected variable name").Value;
                ASTNode? extraInit = null;
                if (Match(TokenType.Assign))
                    extraInit = ParseExpression();
                _extraDeclarations.Add(new VarDeclNode(extraName, type, extraInit, declLine, declCol));
            }
            Expect(TokenType.Semicolon, "expected ';'");
            return result;
        }

        return ParseExpressionStatement();
    }

    private ASTNode ParseExpressionStatement()
    {
        var expr = ParseExpression();
        Expect(TokenType.Semicolon, "expected ';'");
        return expr;
    }

    private ASTNode ParseReturn()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance();
        ASTNode? val = Check(TokenType.Semicolon) ? null : ParseExpression();
        Expect(TokenType.Semicolon, "expected ';'");
        return new ReturnNode(val, l, c);
    }

    private ASTNode ParseIf()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance();
        Expect(TokenType.LParen, "expected '('");
        var cond = ParseExpression();
        Expect(TokenType.RParen, "expected ')'");
        var thenBody = ParseBlockOrSingle();
        List<ASTNode>? elseBody = null;
        if (Match(TokenType.Else))
        {
            if (Check(TokenType.If))
                elseBody = new List<ASTNode> { ParseIf() };
            else
                elseBody = ParseBlockOrSingle();
        }
        return new IfNode(cond, thenBody, elseBody, l, c);
    }

    private ASTNode ParseWhile()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance();
        Expect(TokenType.LParen, "expected '('");
        var cond = ParseExpression();
        Expect(TokenType.RParen, "expected ')'");
        var body = ParseBlockOrSingle();
        return new WhileNode(cond, body, l, c);
    }

    private ASTNode ParseFor()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance();
        Expect(TokenType.LParen, "expected '('");

        ASTNode? init = null;
        bool initIsVarDecl = false;
        if (!Check(TokenType.Semicolon))
        {
            if (IsTypeKeyword() || (Check(TokenType.Identifier) && Peek(1).Type == TokenType.Identifier))
            {
                string type = ParseType();
                string name = Expect(TokenType.Identifier, "expected name").Value;
                init = ParseVarDecl(name, type);
                initIsVarDecl = true;
            }
            else
            {
                init = ParseExpression();
            }
            if (!initIsVarDecl)
                Expect(TokenType.Semicolon, "expected ';'");
        }
        else
        {
            Expect(TokenType.Semicolon, "expected ';'");
        }

        ASTNode? condition = null;
        if (!Check(TokenType.Semicolon))
            condition = ParseExpression();
        Expect(TokenType.Semicolon, "expected ';'");

        ASTNode? increment = null;
        if (!Check(TokenType.RParen))
            increment = ParseExpression();
        Expect(TokenType.RParen, "expected ')'");

        var body = ParseBlockOrSingle();
        return new ForNode(init, condition, increment, body, l, c);
    }

    private ASTNode ParseDoWhile()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance();
        var body = ParseBlockOrSingle();
        Expect(TokenType.While, "expected 'while'");
        Expect(TokenType.LParen, "expected '('");
        var cond = ParseExpression();
        Expect(TokenType.RParen, "expected ')'");
        Expect(TokenType.Semicolon, "expected ';'");
        return new DoWhileNode(cond, body, l, c);
    }

    private ASTNode ParseForeach()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance();
        Expect(TokenType.LParen, "expected '('");

        string? keyName = null;
        string varName;
        // foreach (element; collection) or foreach (key, element; collection)
        var first = Expect(TokenType.Identifier, "expected identifier").Value;
        if (Match(TokenType.Comma))
        {
            keyName = first;
            varName = Expect(TokenType.Identifier, "expected element variable").Value;
        }
        else
        {
            varName = first;
        }

        Expect(TokenType.Semicolon, "expected ';'");
        var collection = ParseExpression();
        Expect(TokenType.RParen, "expected ')'");
        var body = ParseBlockOrSingle();
        return new ForeachNode(varName, keyName, collection, body, l, c);
    }

    // ---- Expressions ----

    private ASTNode ParseExpression() => ParseAssignment();

    private ASTNode ParseAssignment()
    {
        var left = ParseTernary();

        if (Check(TokenType.Assign) || Check(TokenType.PlusAssign) ||
            Check(TokenType.MinusAssign) || Check(TokenType.MulAssign) ||
            Check(TokenType.DivAssign))
        {
            string op = Advance().Value;
            int l = left.Line, cl = left.Column;
            var right = ParseAssignment();
            if (left is VarNode v)
            {
                if (op == "=") return new AssignNode(v.Name, right, l, cl);
                var binOp = op switch
                {
                    "+=" => "+", "-=" => "-", "*=" => "*", "/=" => "/",
                    _ => "+"
                };
                return new AssignNode(v.Name, new BinaryNode(v, binOp, right, l, cl), l, cl);
            }
            if (left is IndexNode idx)
            {
                // a[i] = value / a[i] += value
                // ⚠ 原来 `a[i] = v` 被压成 `AssignNode(a, v)` —— **给数组变量本身赋值**
                //   （下标整个丢掉）；复合赋值连分支都没有，落空后整条语句被静默丢弃。
                if (op == "=") return new IndexAssignNode(idx.Name, idx.Index, right, l, cl);
                return new IndexOpAssignNode(idx.Name, idx.Index, op, right, l, cl);
            }
        }
        return left;
    }

    private ASTNode ParseTernary()
    {
        var left = ParseLogicalOr();
        if (Match(TokenType.Question))
        {
            int l = left.Line, cl = left.Column;
            var thenExpr = ParseExpression();
            Expect(TokenType.Colon, "expected ':' in ternary");
            var elseExpr = ParseTernary();
            // Encode as nested BinaryNode: (cond ? then) : else
            return new BinaryNode(
                new BinaryNode(left, "?", thenExpr, l, cl),
                ":", elseExpr, l, cl);
        }
        return left;
    }

    private ASTNode ParseLogicalOr()
    {
        var left = ParseLogicalAnd();
        while (Match(TokenType.Or))
        {
            var right = ParseLogicalAnd();
            left = new BinaryNode(left, "||", right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseLogicalAnd()
    {
        var left = ParseBitwiseOr();
        while (Match(TokenType.And))
        {
            var right = ParseBitwiseOr();
            left = new BinaryNode(left, "&&", right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseBitwiseOr()
    {
        var left = ParseBitwiseXor();
        while (Match(TokenType.Pipe))
        {
            var right = ParseBitwiseXor();
            left = new BinaryNode(left, "|", right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseBitwiseXor()
    {
        var left = ParseBitwiseAnd();
        while (Match(TokenType.Caret))
        {
            var right = ParseBitwiseAnd();
            left = new BinaryNode(left, "^", right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseBitwiseAnd()
    {
        var left = ParseShift();
        while (Match(TokenType.Amp))
        {
            var right = ParseShift();
            left = new BinaryNode(left, "&", right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseShift()
    {
        var left = ParseComparison();
        while (Check(TokenType.LShift) || Check(TokenType.RShift))
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
        while (Check(TokenType.Eq) || Check(TokenType.Neq) ||
               Check(TokenType.Lt) || Check(TokenType.Gt) ||
               Check(TokenType.Le) || Check(TokenType.Ge))
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
        while (Check(TokenType.Mul) || Check(TokenType.Div) ||
               Check(TokenType.Mod) || Check(TokenType.Pow))
        {
            string op = Advance().Value;
            var right = ParseUnary();
            left = new BinaryNode(left, op, right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseUnary()
    {
        // cast(type) expression
        if (Check(TokenType.Cast))
        {
            int l = Cur.Line, c = Cur.Column;
            Advance(); // consume 'cast'
            Expect(TokenType.LParen, "expected '(' after cast");
            string targetType = ParseType();
            Expect(TokenType.RParen, "expected ')' after cast type");
            var expr = ParseUnary();
            return new CastExpr(targetType, expr, l, c);
        }

        if (Match(TokenType.Minus)) return new UnaryNode("-", ParseUnary(), Cur.Line, Cur.Column);
        if (Match(TokenType.Not)) return new UnaryNode("!", ParseUnary(), Cur.Line, Cur.Column);
        if (Match(TokenType.Tilde)) return new UnaryNode("~", ParseUnary(), Cur.Line, Cur.Column);
        if (Match(TokenType.Amp)) return new UnaryNode("&", ParseUnary(), Cur.Line, Cur.Column);
        if (Match(TokenType.Mul)) return new UnaryNode("*", ParseUnary(), Cur.Line, Cur.Column);
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
            // function call
            if (Check(TokenType.LParen))
            {
                int l = expr.Line, cl = expr.Column;
                if (expr is VarNode vn)
                    expr = ParseCall(vn.Name, l, cl);
                else
                    ParseCall("_", l, cl);
                continue;
            }

            // member access: obj.field
            if (Check(TokenType.Dot))
            {
                Advance(); // consume '.'
                string field = Expect(TokenType.Identifier, "expected field name").Value;
                string baseName = expr is VarNode v ? v.Name : "_";
                expr = new VarNode($"{baseName}.{field}", expr.Line, expr.Column);
                continue;
            }

            // array indexing: a[i]
            if (Check(TokenType.LBracket))
            {
                int l = expr.Line, cl = expr.Column;
                Advance(); // consume '['
                var index = ParseExpression();
                Expect(TokenType.RBracket, "expected ']'");
                if (expr is VarNode vn2)
                    expr = new IndexNode(vn2.Name, index, l, cl);
                else
                    expr = new IndexNode("_", index, l, cl);
                continue;
            }

            // postfix ++ / --
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

    private ASTNode ParseCall(string name, int l, int c)
    {
        Expect(TokenType.LParen, "expected '('");
        var args = new List<ASTNode>();
        if (!Check(TokenType.RParen))
        {
            do args.Add(ParseExpression());
            while (Match(TokenType.Comma));
        }
        // 跳过无法识别的参数内容 (如 D is 表达式: check(42 is int))
        while (!Check(TokenType.RParen) && !Check(TokenType.EOF))
            Advance();
        Expect(TokenType.RParen, "expected ')'");
        return new CallNode(name, args, l, c);
    }

    private ASTNode ParsePrimary()
    {
        int l = Cur.Line, c = Cur.Column;

        // literals
        if (Check(TokenType.Integer))
        {
            var t = Advance();
            string v = t.Value;
            if (v.StartsWith("0x") || v.StartsWith("0X"))
            {
                if (v.Length > 10) // 0x + 8 hex digits > Int32 range
                    return new LiteralNode(System.Convert.ToInt64(v, 16), l, c);
                return new LiteralNode(System.Convert.ToInt32(v, 16), l, c);
            }
            // D 长整型后缀 L/l → 64 位
            if (v.EndsWith("L") || v.EndsWith("l"))
                return new LiteralNode(long.Parse(v.Substring(0, v.Length - 1)), l, c);
            if (!int.TryParse(v, out int iv)) return new LiteralNode(long.Parse(v), l, c);
            return new LiteralNode(iv, l, c);
        }
        if (Check(TokenType.FloatLiteral))
        {
            var t = Advance();
            string v = t.Value;
            // D 浮点字面量默认 double; f/F 后缀 → float
            if (v.EndsWith("f") || v.EndsWith("F"))
                return new LiteralNode(float.Parse(v.Substring(0, v.Length - 1),
                    System.Globalization.CultureInfo.InvariantCulture), l, c);
            return new LiteralNode(double.Parse(v,
                System.Globalization.CultureInfo.InvariantCulture), l, c);
        }
        if (Check(TokenType.StringLiteral))
        {
            var t = Advance();
            return new LiteralNode(t.Value, l, c);
        }
        if (Check(TokenType.CharLiteral))
        {
            var t = Advance();
            return new LiteralNode(t.Value.Length > 0 ? (int)t.Value[0] : 0, l, c);
        }
        if (Match(TokenType.True)) return new LiteralNode(true, l, c);
        if (Match(TokenType.False)) return new LiteralNode(false, l, c);
        if (Match(TokenType.Null)) return new LiteralNode(null, l, c);

        // D 数组字面量: [1, 2, 3]
        if (Match(TokenType.LBracket))
        {
            var elements = new List<ASTNode>();
            if (!Check(TokenType.RBracket))
            {
                do { elements.Add(ParseExpression()); }
                while (Match(TokenType.Comma));
            }
            Expect(TokenType.RBracket, "expected ']'");
            // ⚠ 原来 here 是 `new LiteralNode(elements, l, c)` —— 把元素列表当成"字面量的值"，
            //   而 EmitLoadConstant 会对它做 Convert.ToInt32 ⇒ InvalidCastException
            //   （实测：`Unable to cast List<ASTNode> to IConvertible`）。改成真正的数组字面量节点。
            return new ArrayLiteralNode(elements, l, c);
        }

        // this
        if (Match(TokenType.This)) return new VarNode("this", l, c);

        // new expression: new ClassName(args)
        if (Match(TokenType.New))
        {
            string className = Expect(TokenType.Identifier, "expected class name").Value;
            // D 模板实例化: new Box!int() — 跳过 !type 模板参数
            if (Match(TokenType.Not))
            {
                while (!Check(TokenType.LParen) && !Check(TokenType.RParen) &&
                       !Check(TokenType.Semicolon) && !Check(TokenType.EOF))
                    Advance();
            }
            var args = new List<ASTNode>();
            if (Match(TokenType.LParen))
            {
                if (!Check(TokenType.RParen))
                {
                    do args.Add(ParseExpression());
                    while (Match(TokenType.Comma));
                }
                Expect(TokenType.RParen, "expected ')'");
            }
            return new CallNode(className, args, l, c);
        }

        // identifier (variable or function call)
        if (Check(TokenType.Identifier))
        {
            var name = Advance().Value;
            if (Check(TokenType.LParen))
                return ParseCall(name, l, c);
            return new VarNode(name, l, c);
        }

        // parenthesized expression
        if (Match(TokenType.LParen))
        {
            var expr = ParseExpression();
            Expect(TokenType.RParen, "expected ')'");
            return expr;
        }

        throw Error(
            $"Unexpected token: {Cur.Type}({Cur.Value}) at {Cur.Line}:{Cur.Column}");
    }

    private void SkipComments()
    {
        while (Check(TokenType.LineComment) || Check(TokenType.BlockComment))
            Advance();
    }

    private ASTNode ParseSwitch()
    {
        int l = Cur.Line, c = Cur.Column;
        Expect(TokenType.LParen, "expected '(' after switch");
        var expr = ParseExpression();
        Expect(TokenType.RParen, "expected ')'");
        Expect(TokenType.LBrace, "expected '{'");
        var cases = new List<CaseNode>();
        while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
        {
            if (Match(TokenType.Case))
            {
                int cl = Cur.Line, cc = Cur.Column;
                var caseVal = ParseExpression();
                Expect(TokenType.Colon, "expected ':' after case");
                var body = new List<ASTNode>();
                while (!Check(TokenType.Case) && !Check(TokenType.Default) && !Check(TokenType.RBrace) && !Check(TokenType.EOF))
                    body.Add(ParseStatement());
                cases.Add(new CaseNode(caseVal, body, cl, cc));
            }
            else if (Match(TokenType.Default))
            {
                int dl = Cur.Line, dc = Cur.Column;
                Expect(TokenType.Colon, "expected ':' after default");
                var body = new List<ASTNode>();
                while (!Check(TokenType.Case) && !Check(TokenType.Default) && !Check(TokenType.RBrace) && !Check(TokenType.EOF))
                    body.Add(ParseStatement());
                cases.Add(new CaseNode(null, body, dl, dc));
            }
            else Advance();
        }
        Expect(TokenType.RBrace, "expected '}'");
        return new SwitchNode(expr, cases, l, c);
    }
}
