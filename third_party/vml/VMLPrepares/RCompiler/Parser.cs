using System.Collections.Generic;
using CompilerBase;

namespace RCompiler;

public class Parser : ParserBase<Token, TokenType>
{
    protected override TokenType GetTokenType(Token token) => token.Type;

    /// <summary>
    /// 表达式**收尾符** —— 永远不会是表达式的开头（`GapAnchor()` 的第一个判据）。
    ///
    /// ⚠ R **没有 `Newline` 这个 token**（靠空白分隔语句，换行不是词法单元），
    /// 所以 `x = 1 +` 结尾撞上的是 **`EOF`** —— `EOF` 必须算进来，
    /// 否则这条判据在最常见的形态上不生效。
    /// </summary>
    protected override bool IsExpressionCloser(Token token) => token.Type
        is TokenType.RParen or TokenType.RBrace or TokenType.RBracket
        or TokenType.Comma or TokenType.Semicolon or TokenType.Colon
        or TokenType.EOF;

    /// <summary>语句分隔 —— R 只有 `;`（换行不是 token）。</summary>
    protected override bool IsStatementSeparator(Token token) => token.Type
        is TokenType.Semicolon;

    protected override Token Expect(TokenType t, string msg) => base.Expect(t, $"R 解析错误: {msg}（得到 {Cur.Type}）");

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
        while (Check(TokenType.Semicolon)) Advance();
        if (Check(TokenType.EOF)) return;
        prog.Statements.Add(ParseStatement());
        if (Check(TokenType.Semicolon)) Advance();
    }

    private ASTNode ParseStatement()
    {
        while (Check(TokenType.Semicolon)) Advance();

        if (Check(TokenType.Function)) return ParseFuncDef();
        if (Check(TokenType.If)) return ParseIf();
        if (Check(TokenType.While)) return ParseWhile();
        if (Check(TokenType.For)) return ParseFor();
        if (Check(TokenType.Repeat)) return ParseRepeat();
        if (Check(TokenType.Return)) return ParseReturn();
        if (Check(TokenType.Break)) { int l = Cur.Line, c = Cur.Column; Advance(); return new BreakNode(l, c); }
        if (Check(TokenType.Next)) { int l = Cur.Line, c = Cur.Column; Advance(); return new NextNode(l, c); }

        return ParseExpressionStatement();
    }

    private ASTNode ParseExpressionStatement()
    {
        var expr = ParseExpression();
        return expr;
    }

    private ASTNode ParseFuncDef()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // function
        Expect(TokenType.LParen, "expected '('");
        var parms = new List<string>();
        if (!Check(TokenType.RParen))
        {
            do
            {
                parms.Add(Expect(TokenType.Identifier, "期望参数名").Value);
            }
            while (Match(TokenType.Comma));
        }
        Expect(TokenType.RParen, "expected ')'");
        var body = ParseBlock();
        return new FuncDefNode("", parms, body, l, c);
    }

    private List<ASTNode> ParseBlock()
    {
        var body = new List<ASTNode>();
        Expect(TokenType.LBrace, "expected '{'");
        while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
        {
            while (Check(TokenType.Semicolon)) Advance();
            if (Check(TokenType.RBrace) || Check(TokenType.EOF)) break;
            body.Add(ParseStatement());
            if (Check(TokenType.Semicolon)) Advance();
        }
        Expect(TokenType.RBrace, "expected '}'");
        return body;
    }

    private List<ASTNode> ParseBody()
    {
        if (Check(TokenType.LBrace))
            return ParseBlock();
        return new List<ASTNode> { ParseStatement() };
    }

    private ASTNode ParseReturn()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance();
        ASTNode? val = null;
        if (Match(TokenType.LParen))
        {
            if (!Check(TokenType.RParen))
                val = ParseExpression();
            Expect(TokenType.RParen, "expected ')'");
        }
        else
        {
            val = ParseExpression();
        }
        return new ReturnNode(val, l, c);
    }

    private ASTNode ParseIf()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // if
        Expect(TokenType.LParen, "expected '('");
        var cond = ParseExpression();
        Expect(TokenType.RParen, "expected ')'");
        var thenBody = ParseBody();
        List<ASTNode>? elseBody = null;
        if (Match(TokenType.Else))
        {
            if (Check(TokenType.If))
                elseBody = new List<ASTNode> { ParseIf() };
            else
                elseBody = ParseBody();
        }
        return new IfNode(cond, thenBody, elseBody, l, c);
    }

    private ASTNode ParseWhile()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // while
        Expect(TokenType.LParen, "expected '('");
        var cond = ParseExpression();
        Expect(TokenType.RParen, "expected ')'");
        var body = ParseBlock();
        return new WhileNode(cond, body, l, c);
    }

    private ASTNode ParseFor()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // for
        Expect(TokenType.LParen, "expected '('");
        string variable = Expect(TokenType.Identifier, "期望变量名").Value;
        Expect(TokenType.In, "expected 'in'");
        var sequence = ParseExpression();
        Expect(TokenType.RParen, "expected ')'");
        var body = ParseBlock();
        return new ForNode(variable, sequence, body, l, c);
    }

    private ASTNode ParseRepeat()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // repeat
        var body = ParseBlock();
        return new RepeatNode(body, l, c);
    }

    // ---- Expression Parsing ----

    private ASTNode ParseExpression() => ParseAssignment();

    /// <summary>
    /// Assignment operator &lt;- and = (lowest precedence except formula)
    /// </summary>
    private ASTNode ParseAssignment()
    {
        var left = ParseFormula();

        if (Check(TokenType.Assign)) // = or <-
        {
            string op = Cur.Value;
            int l = Cur.Line, c = Cur.Column;
            bool isSuper = op == "<<-";
            Advance();
            var value = ParseAssignment();
            if (left is FuncDefNode funcDef && string.IsNullOrEmpty(funcDef.Name))
            {
                // we don't know the name yet — handle specially
            }
            if (left is IndexNode idx)
                return new IndexAssignNode(idx.Target, idx.Index, value, l, c);
            if (left is VarNode v)
                return new AssignNode(v.Name, value, isSuper, l, c);
            return new AssignNode("_", value, isSuper, l, c);
        }
        if (Check(TokenType.SuperAssign))
        {
            int l = Cur.Line, c = Cur.Column;
            Advance();
            var value = ParseAssignment();
            if (left is IndexNode idx)
                return new IndexAssignNode(idx.Target, idx.Index, value, l, c);
            if (left is VarNode v)
                return new AssignNode(v.Name, value, true, l, c);
            return new AssignNode("_", value, true, l, c);
        }

        return left;
    }

    /// <summary>
    /// Formula operator ~ (lowest precedence in R)
    /// </summary>
    private ASTNode ParseFormula()
    {
        var left = ParseOr();
        while (Match(TokenType.Tilde))
        {
            var right = ParseOr();
            left = new BinaryNode(left, "~", right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseOr()
    {
        var left = ParseAnd();
        while (Check(TokenType.Or))
        {
            string op = Advance().Value;
            var right = ParseAnd();
            left = new BinaryNode(left, op, right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseAnd()
    {
        var left = ParseComparison();
        while (Check(TokenType.And))
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
        while (Check(TokenType.Eq) || Check(TokenType.Neq) || Check(TokenType.Lt) ||
               Check(TokenType.Gt) || Check(TokenType.Le) || Check(TokenType.Ge))
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
        while (Check(TokenType.Mul) || Check(TokenType.Div) || Check(TokenType.Mod) || Check(TokenType.Pow) || Check(TokenType.PercentOp))
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
        if (Match(TokenType.Plus))
            return ParseUnary();
        return ParsePostfix();
    }

    private ASTNode ParsePostfix()
    {
        var expr = ParsePrimary();

        while (true)
        {
            if (Check(TokenType.LParen))
            {
                Advance();
                var args = new List<ASTNode>();
                if (!Check(TokenType.RParen))
                {
                    do { args.Add(ParseExpression()); }
                    while (Match(TokenType.Comma));
                }
                Expect(TokenType.RParen, "expected ')'");
                string name = expr is VarNode v ? v.Name : "_";
                expr = new CallNode(name, args, expr.Line, expr.Column);
            }
            else if (Check(TokenType.LBracket))
            {
                Advance();
                var indices = new List<ASTNode>();
                // R 支持多维索引: m[1, 2] 或 m[1, ]
                if (!Check(TokenType.RBracket))
                {
                    do { indices.Add(ParseExpression()); }
                    while (Match(TokenType.Comma));
                }
                Expect(TokenType.RBracket, "expected ']'");
                // 使用第一个索引（多维索引的简化处理）
                expr = new IndexNode(expr, indices[0], expr.Line, expr.Column);
            }
            else if (Check(TokenType.Dollar))
            {
                Advance();
                string field = Expect(TokenType.Identifier, "期望字段名").Value;
                expr = new BinaryNode(expr, "$", new VarNode(field, field.Length, 0), expr.Line, expr.Column);
            }
            else if (Check(TokenType.Colon))
            {
                Advance();
                var right = ParsePrimary();
                expr = new CallNode(":", new List<ASTNode> { expr, right }, expr.Line, expr.Column);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private ASTNode ParsePrimary()
    {
        int l = Cur.Line, c = Cur.Column;

        if (Check(TokenType.Function))
            return ParseFuncDef();

        if (Check(TokenType.Integer))
        {
            var t = Advance();
            string val = t.Value;
            // check for L suffix (R integer literal)
            if (val.EndsWith("L")) val = val.Substring(0, val.Length - 1);
            return new LiteralNode(int.Parse(val), l, c);
        }
        if (Check(TokenType.Float))
        {
            var t = Advance();
            return new LiteralNode(float.Parse(t.Value, System.Globalization.CultureInfo.InvariantCulture), l, c);
        }
        if (Check(TokenType.String))
        {
            var t = Advance();
            return new LiteralNode(t.Value, l, c);
        }
        if (Match(TokenType.True)) return new LiteralNode(1, l, c);
        if (Match(TokenType.False)) return new LiteralNode(0, l, c);
        if (Match(TokenType.Null)) return new LiteralNode(null, l, c);
        if (Match(TokenType.Na)) return new LiteralNode(int.MinValue, l, c);   // NA sentinel
        if (Match(TokenType.Inf)) return new LiteralNode(float.PositiveInfinity, l, c);

        if (Check(TokenType.Identifier))
        {
            string name = Advance().Value;

            // c() is a special built-in for creating vectors
            if (name == "c" && Check(TokenType.LParen))
            {
                Advance();
                var elems = new List<ASTNode>();
                if (!Check(TokenType.RParen))
                {
                    do { elems.Add(ParseExpression()); }
                    while (Match(TokenType.Comma));
                }
                Expect(TokenType.RParen, "expected ')'");
                return new SeqNode(elems, l, c);
            }

            // list() for creating lists
            if (name == "list" && Check(TokenType.LParen))
            {
                Advance();
                var listElems = new List<(string? name, ASTNode value)>();
                if (!Check(TokenType.RParen))
                {
                    do
                    {
                        // name = value or just value
                        if (Check(TokenType.Identifier) && Peek(1).Type == TokenType.Assign)
                        {
                            string tag = Advance().Value;
                            Advance(); // =
                            listElems.Add((tag, ParseExpression()));
                        }
                        else
                        {
                            listElems.Add((null, ParseExpression()));
                        }
                    }
                    while (Match(TokenType.Comma));
                }
                Expect(TokenType.RParen, "expected ')'");
                return new ListNode(listElems, l, c);
            }

            return new VarNode(name, l, c);
        }

        if (Match(TokenType.LParen))
        {
            var expr = ParseExpression();
            Expect(TokenType.RParen, "expected ')'");
            return expr;
        }

        // 位置用 `GapAnchor()`：`x = 1 +` 结尾撞上 **`EOF`**（R 没有换行 token，EOF 标在下一行）
        // ⇒ 按当前位置报就落到第 5 行，而错在第 4 行。锚定规则见基类 `GapAnchor`。
        // 顺带去掉消息里手写的「（位置 L:C）」—— 与统一前缀重复，且那份取的是
        // 预处理后的行列（不查 `#include` 映射），可能与前缀不一致。
        throw ErrorAt($"意外的 token: {Cur.Type}({Cur.Value})", GapAnchor());
    }
}
