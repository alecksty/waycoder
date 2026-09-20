using System.Collections.Generic;
using System.Linq;
using System;
using CompilerBase;

namespace RubyCompiler;

public class Parser : ParserBase<Token, TokenType>
{
    protected override TokenType GetTokenType(Token token) => token.Type;

    /// <summary>
    /// 表达式**收尾符** —— 永远不会是表达式的开头（`GapAnchor()` 的第一个判据）。
    /// `Newline` / `EOF` 必须算进来：Ruby 的语句以换行收尾，`x = 1 +` 正是等换行后
    /// 遇到 `Newline` 才发作。
    /// </summary>
    protected override bool IsExpressionCloser(Token token) => token.Type
        is TokenType.RParen or TokenType.RBrace or TokenType.RBracket
        or TokenType.Comma or TokenType.Semicolon or TokenType.Colon
        or TokenType.Newline or TokenType.EOF;

    /// <summary>语句分隔 —— `GapAnchor()` 的第二个判据：上一个若是它，缺口在本行行首。</summary>
    protected override bool IsStatementSeparator(Token token) => token.Type
        is TokenType.Newline or TokenType.Semicolon;

    protected override Token Expect(TokenType t, string msg) => base.Expect(t, $"Ruby 解析错误: {msg}（得到 {Cur.Type}）");

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
        while (Check(TokenType.Newline)) Advance();
        if (Check(TokenType.EOF)) return;
        prog.Statements.Add(ParseStatement());
    }

    private ASTNode ParseStatement()
    {
        while (Check(TokenType.Newline)) Advance();
        if (Check(TokenType.Def)) return ParseDef();
        if (Check(TokenType.Module)) return ParseModule();
        if (Check(TokenType.Class)) return ParseClass();
        if (Check(TokenType.Include)) { Advance(); return new IncludeNode(Expect(TokenType.Identifier, "期望模块名").Value, Cur.Line, Cur.Column); }
        if (Check(TokenType.Return)) return ParseReturn();
        if (Check(TokenType.If) || Check(TokenType.Unless)) return ParseIf();
        if (Check(TokenType.While) || Check(TokenType.Until)) return ParseWhile();
        if (Check(TokenType.For)) return ParseFor();
        if (Check(TokenType.Begin)) return ParseBeginRescue();
        if (Check(TokenType.RaiseKw)) return ParseRaise();
        if (Check(TokenType.Case)) return ParseCase();
        return ParseExpressionStatement();
    }

    private ASTNode ParseExpressionStatement()
    {
        var expr = ParseExpression();
        SkipNewlines();
        return expr;
    }

    private ASTNode ParseDef()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // def
        bool isNative = Match(TokenType.Native);
        string name = Expect(TokenType.Identifier, "期望方法名").Value;
        var parms = new List<string>();
        if (Match(TokenType.LParen))
        {
            if (!Check(TokenType.RParen))
            {
                do parms.Add(Expect(TokenType.Identifier, "期望参数名").Value);
                while (Match(TokenType.Comma));
            }
            Expect(TokenType.RParen, "expected )");
        }
        var body = ParseBlock();
        return new DefNode(name, parms, body, l, c, isNative);
    }

    private ASTNode ParseModule()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // module
        string name = Expect(TokenType.Identifier, "期望模块名").Value;
        var body = new List<ASTNode>(ParseBlock());
        return new ModuleNode(name, body, l, c);
    }

    private ASTNode ParseClass()
    {
        Advance(); // class
        string name = Expect(TokenType.Identifier, "期望类名").Value;
        var body = ParseBlock();
        return new DefNode("self." + name, new List<string>(), body, 0, 0);
    }

    private List<ASTNode> ParseBlock(params TokenType[] extraStop)
    {
        var body = new List<ASTNode>();
        while (!Check(TokenType.End) && !Check(TokenType.EOF))
        {
            if (extraStop.Length > 0 && extraStop.Any(t => Check(t))) break;
            while (Check(TokenType.Newline)) Advance();
            if (Check(TokenType.End) || Check(TokenType.EOF)) break;
            if (extraStop.Length > 0 && extraStop.Any(t => Check(t))) break;
            body.Add(ParseStatement());
        }
        Expect(TokenType.End, "expected 'end'");
        return body;
    }

    private List<ASTNode> ParseBlockUntil(Func<bool> stopCondition)
    {
        var body = new List<ASTNode>();
        while (!Check(TokenType.End) && !Check(TokenType.EOF) && !stopCondition())
        {
            while (Check(TokenType.Newline)) Advance();
            if (Check(TokenType.End) || Check(TokenType.EOF) || stopCondition()) break;
            body.Add(ParseStatement());
        }
        return body;
    }

    private ASTNode ParseReturn()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance();
        ASTNode? val = Check(TokenType.Newline) ? null : ParseExpression();
        return new ReturnNode(val, l, c);
    }

    private ASTNode ParseIf()
    {
        int l = Cur.Line, c = Cur.Column;
        bool isUnless = Match(TokenType.Unless);
        if (!isUnless) Advance(); // consume 'if'
        var cond = ParseExpression();
        // true branch stops at else/elsif/end
        var body = ParseBlockUntil(() => Check(TokenType.Else) || Check(TokenType.Elsif));
        List<ASTNode>? elseBody = null;
        while (Check(TokenType.Elsif))
        {
            Advance();
            var elifCond = ParseExpression();
            var elifBody = ParseBlockUntil(() => Check(TokenType.Else) || Check(TokenType.Elsif));
            body.Add(new IfNode(elifCond, elifBody, null, l, c));
        }
        if (Match(TokenType.Else)) elseBody = ParseBlock();
        else Expect(TokenType.End, "expected 'end'");
        if (isUnless) return new IfNode(new UnaryNode("!", cond, l, c), body, elseBody, l, c);
        return new IfNode(cond, body, elseBody, l, c);
    }

    private ASTNode ParseWhile()
    {
        int l = Cur.Line, c = Cur.Column;
        bool isUntil = Match(TokenType.Until);
        if (!isUntil) Advance(); // consume 'while'
        var cond = ParseExpression();
        var body = ParseBlock();
        return isUntil ? new WhileNode(new UnaryNode("!", cond, l, c), body, l, c) : new WhileNode(cond, body, l, c);
    }

    private ASTNode ParseFor()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // for
        string v = Expect(TokenType.Identifier, "期望变量名").Value;
        Expect(TokenType.In, "expected 'in'");
        var from = ParseExpression();
        Expect(TokenType.Range, "expected '..'");
        var to = ParseExpression();
        var body = ParseBlock();
        return new ForNode(v, from, to, body, l, c);
    }

    private ASTNode ParseBeginRescue()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // consume 'begin'
        var body = new List<ASTNode>();
        // Parse body until rescue/ensure/else/end
        while (Cur.Type != TokenType.Rescue && Cur.Type != TokenType.Ensure && Cur.Type != TokenType.Else && Cur.Type != TokenType.End)
        {
            body.Add(ParseStatement());
        }
        var rescues = new List<RescueClauseNode>();
        while (Cur.Type == TokenType.Rescue)
        {
            Advance(); // consume 'rescue'
            int rl = Cur.Line, rc = Cur.Column;
            string? excClass = null; string? varName = null;
            // 异常类 + 变量: rescue Foo => e ; 否则紧跟的 identifier 属于 rescue 体 (如 x=0)
            if (Cur.Type == TokenType.Identifier && Peek(1).Type == TokenType.FatArrow)
            {
                excClass = Cur.Value; Advance(); // 异常类名
                Advance(); // =>
                varName = Expect(TokenType.Identifier, "期望变量名在 => 后").Value;
            }
            var rescueBody = new List<ASTNode>();
            while (Cur.Type != TokenType.Rescue && Cur.Type != TokenType.Ensure && Cur.Type != TokenType.Else && Cur.Type != TokenType.End)
                rescueBody.Add(ParseStatement());
            rescues.Add(new RescueClauseNode(excClass, varName, rescueBody, rl, rc));
        }
        List<ASTNode>? elseBody = null;
        if (Cur.Type == TokenType.Else)
        {
            Advance(); // consume 'else'
            elseBody = new List<ASTNode>();
            while (Cur.Type != TokenType.Ensure && Cur.Type != TokenType.End)
                elseBody.Add(ParseStatement());
        }
        List<ASTNode>? ensureBody = null;
        if (Cur.Type == TokenType.Ensure)
        {
            Advance(); // consume 'ensure'
            ensureBody = new List<ASTNode>();
            while (Cur.Type != TokenType.End)
                ensureBody.Add(ParseStatement());
        }
        Expect(TokenType.End, "expected 'end'");
        return new BeginRescueNode(body, rescues, ensureBody, elseBody, l, c);
    }

    private ASTNode ParseRaise()
    {
        int l = Cur.Line, c = Cur.Column;
        ASTNode? expr = null;
        if (Peek().Type != TokenType.Newline && Peek().Type != TokenType.EOF)
            expr = ParseExpression();
        SkipNewlines();
        return new RaiseNode(expr, l, c);
    }

    private ASTNode ParseCase()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // case
        var condition = ParseExpression();
        var whenClauses = new List<WhenClauseNode>();
        List<ASTNode>? elseBody = null;

        while (Check(TokenType.When))
        {
            Advance();
            var values = new List<ASTNode>();
            do values.Add(ParseExpression());
            while (Match(TokenType.Comma));
            var body = ParseBlockUntil(() => Check(TokenType.When) || Check(TokenType.Else));
            whenClauses.Add(new WhenClauseNode(values, body, l, c));
        }

        if (Match(TokenType.Else))
            elseBody = ParseBlock();
        else
            Expect(TokenType.End, "expected 'end'");

        return new CaseNode(condition, whenClauses, elseBody, l, c);
    }

    private ASTNode ParseExpression() => ParseAssignment();

    private ASTNode ParseAssignment()
    {
        var left = ParseLogicalOr();

        // 三元运算符: condition ? true_value : false_value
        if (Check(TokenType.Question))
        {
            int l = Cur.Line, c = Cur.Column;
            Advance(); // consume ?
            var trueVal = ParseExpression();
            Expect(TokenType.Colon, "期望 ':' 在三元表达式中");
            var falseVal = ParseExpression();
            return new TernaryNode(left, trueVal, falseVal, l, c);
        }

        if (Check(TokenType.Assign))
        {
            int l = Cur.Line, c = Cur.Column;
            Advance();
            var right = ParseAssignment();
            if (left is VarNode v) return new AssignNode(v.Name, right, l, c);
            // 下标左值: a[i] = v —— 原来整段丢成下划线变量 `_`（目标与下标都没了，写得"成功"但其实什么都没写）
            if (left is IndexNode ix) return new IndexAssignNode(ix.Target, ix.Index, right, l, c);
            return new AssignNode("_", right, l, c);
        }
        if (Check(TokenType.PlusAssign) || Check(TokenType.MinusAssign) || Check(TokenType.MulAssign) || Check(TokenType.DivAssign))
        {
            string op = Advance().Value;
            var right = ParseAssignment();
            if (left is VarNode v) return new OpAssignNode(v.Name, op, right, left.Line, left.Column);
            // 下标复合赋值: 原来落空（既没生成节点也没报错，整条语句的值被丢掉）
            if (left is IndexNode ix2) return new IndexOpAssignNode(ix2.Target, ix2.Index, op, right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseLogicalOr()
    {
        var left = ParseLogicalAnd();
        while (Check(TokenType.Or))
        {
            string op = Advance().Value;
            var right = ParseLogicalAnd();
            left = new BinaryNode(left, op, right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseLogicalAnd()
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
        while (Check(TokenType.Eq) || Check(TokenType.Neq) || Check(TokenType.Lt) || Check(TokenType.Gt) || Check(TokenType.Le) || Check(TokenType.Ge) || Check(TokenType.Cmp))
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
        while (Check(TokenType.Mul) || Check(TokenType.Div) || Check(TokenType.Mod) || Check(TokenType.Pow))
        {
            string op = Advance().Value;
            var right = ParseUnary();
            left = new BinaryNode(left, op, right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseUnary()
    {
        if (Match(TokenType.Minus)) return new UnaryNode("-", ParseUnary(), Cur.Line, Cur.Column);
        if (Match(TokenType.Not)) return new UnaryNode("!", ParseUnary(), Cur.Line, Cur.Column);
        return ParsePrimary();
    }

    /// <summary>
    /// 主表达式 + **后缀下标链**。此前只有主表达式，`a[i]` 里的 `[i]` 根本没人吃
    /// （表现为 `plus1(a[i])` 报「expected ) (got LBracket)」）。
    /// </summary>
    private ASTNode ParsePrimary()
    {
        var expr = ParsePrimaryCore();
        while (Check(TokenType.LBracket))
        {
            int l = expr.Line, c = expr.Column;
            Advance(); // [
            var index = ParseExpression();
            Expect(TokenType.RBracket, "expected ]");
            expr = new IndexNode(expr, index, l, c);
        }
        return expr;
    }

    private ASTNode ParsePrimaryCore()
    {
        int l = Cur.Line, c = Cur.Column;

        if (Check(TokenType.Integer)) { var t = Advance(); return new LiteralNode(int.Parse(t.Value), l, c); }
        if (Check(TokenType.Float)) { var t = Advance(); return new LiteralNode(float.Parse(t.Value, System.Globalization.CultureInfo.InvariantCulture), l, c); }
        if (Check(TokenType.String)) { var t = Advance(); return new LiteralNode(t.Value, l, c); }
        if (Match(TokenType.Nil)) return new LiteralNode(null, l, c);
        if (Match(TokenType.True)) return new LiteralNode(1, l, c);
        if (Match(TokenType.False)) return new LiteralNode(0, l, c);
        if (Match(TokenType.Self)) return new VarNode("self", l, c);
        if (Check(TokenType.Symbol)) { var t = Advance(); return new LiteralNode(t.Value, l, c); }

        if (Check(TokenType.LBracket)) return ParseArray();

        if (Check(TokenType.Identifier))
        {
            var name = Advance().Value;
            if (Match(TokenType.LParen)) return ParseCall(null, name);
            if (Check(TokenType.Dot))
            {
                Advance();
                string method = Expect(TokenType.Identifier, "期望方法名").Value;
                if (Match(TokenType.LParen)) return ParseCall(new VarNode(name, l, c), method);
                return new CallNode(new VarNode(name, l, c), method, new List<ASTNode>(), l, c);
            }
            // 无括号方法调用 (Ruby 语法: "puts int_to_str(42)" ≡ "puts(int_to_str(42))")
            // 仅当同一行紧跟能开始表达式的 token 时视为调用；否则按变量引用处理
            if ((name == "print" || name == "puts") && CanStartExpression(Cur.Type))
            {
                var args = new List<ASTNode>();
                do args.Add(ParseExpression());
                while (Match(TokenType.Comma));
                return new CallNode(null, name, args, l, c);
            }
            return new VarNode(name, l, c);
        }

        if (Match(TokenType.LParen))
        {
            var expr = ParseExpression();
            Expect(TokenType.RParen, "expected )");
            return expr;
        }

        // 位置用 `GapAnchor()`：`x = 1 +` 的下一个 token 是**下一行**的 `Newline`
        // ⇒ 按当前位置报就落到第 5 行，而错在第 4 行。锚定规则见基类 `GapAnchor`。
        // 顺带去掉消息里手写的「（位置 L:C）」—— 与统一前缀 `文件:行:列: error:`
        // **重复**，且手写那份取的是预处理后的行列（不查 `#include` 映射），
        // 两份可能不一致。位置由前缀一处给。
        throw ErrorAt($"意外的 token: {Cur.Type}({Cur.Value})", GapAnchor());
    }

    private ASTNode ParseCall(ASTNode? receiver, string method)
    {
        int l = receiver?.Line ?? Cur.Line, c = receiver?.Column ?? Cur.Column;
        var args = new List<ASTNode>();
        if (!Check(TokenType.RParen))
        {
            do args.Add(ParseExpression());
            while (Match(TokenType.Comma));
        }
        Expect(TokenType.RParen, "expected )");
        return new CallNode(receiver, method, args, l, c);
    }

    /// <summary>判断 token 是否能开始一个表达式（用于无括号方法调用参数检测）</summary>
    private static bool CanStartExpression(TokenType t) => t switch
    {
        TokenType.Identifier or TokenType.Integer or TokenType.Float or TokenType.String
            or TokenType.Symbol or TokenType.Nil or TokenType.True or TokenType.False
            or TokenType.Self or TokenType.LParen or TokenType.LBracket
            or TokenType.Not or TokenType.Minus or TokenType.Plus => true,
        _ => false
    };

    private ASTNode ParseArray()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // [
        var elems = new List<ASTNode>();
        if (!Check(TokenType.RBracket))
        {
            do elems.Add(ParseExpression());
            while (Match(TokenType.Comma));
        }
        Expect(TokenType.RBracket, "expected ]");
        return new ArrayNode(elems, l, c);
    }

    private void SkipNewlines() { while (Check(TokenType.Newline)) Advance(); }
}
