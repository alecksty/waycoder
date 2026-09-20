using CompilerBase;

namespace SchemeCompiler;

public class Parser : ParserBase<Token, TokenType>
{
    protected override TokenType GetTokenType(Token token) => token.Type;

    public Parser(List<Token> tokens) : base(tokens) { }

    public SExpr Parse() {
        var exprs = new List<SExpr>();
        while (!IsAtEnd) exprs.Add(ParseExpr());
        if (exprs.Count == 1) return exprs[0];
        // 多个顶层形式：合成一个列表当整棵树的根。**它的位置要显式给** ——
        // 它不是 `ParseExpr` 产出的（这里绕过那个入口），留 0 的话生成器
        // 那句 `Line > 0` 判据对整棵树失效。取第一个形式的位置即可。
        var root = new SList(exprs);
        if (exprs.Count > 0) { root.Line = exprs[0].Line; root.Column = exprs[0].Column; }
        return root;
    }

    /// <summary>
    /// 表达式的**唯一构造点** —— 顺手把起始位置盖上（`SExpr.Line`/`Column`）。
    ///
    /// 做成一个包一层的入口（与 C/C#/Rust 的 `ParseStatement` 同一套路）：递归进来的
    /// 子表达式也会各自被盖一次，于是**越往里越精确**。
    /// 加这个之前 `SExpr` 一个位置字段都没有 ⇒ 生成器想报行号也无从报起，
    /// 链接期报「未定义的函数」时只能给一句光杆消息（用户看不到位置）。
    /// </summary>
    SExpr ParseExpr() {
        var start = Cur;
        var node = ParseExprCore();
        if (node != null && node.Line == 0) { node.Line = start.Line; node.Column = start.Column; }
        return node;
    }

    SExpr ParseExprCore() {
        if (Check(TokenType.LPAREN)) { Advance(); var items = new List<SExpr>(); while (!Check(TokenType.RPAREN) && !IsAtEnd) items.Add(ParseExpr()); if (Check(TokenType.RPAREN)) Advance(); return new SList(items); }
        if (Check(TokenType.QUOTE)) { Advance(); var quoted = ParseExpr(); return new SList([new SSym("quote"), quoted]); }
        if (Check(TokenType.NUMBER)) { var v = Advance().Value; if (v.Contains('.')) return new SDouble(double.Parse(v)); return new SInt(int.Parse(v)); }
        if (Check(TokenType.STRING)) { return new SStr(Advance().Value); }
        if (Check(TokenType.TRUE)) { Advance(); return new SBool(true); }
        if (Check(TokenType.FALSE)) { Advance(); return new SBool(false); }
        return new SSym(Advance().Value);
    }
}
