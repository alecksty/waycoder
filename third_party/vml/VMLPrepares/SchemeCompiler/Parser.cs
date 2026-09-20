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

    // 收尾符 / 语句分隔：给 `GapAnchor()` 用（**只有各门自己知道自己的记号长什么样**）。
    // Scheme 的"语句"就是一个个顶层形式，没有分隔符 —— 所以后者恒 false。
    protected override bool IsExpressionCloser(Token token) =>
        token.Type is TokenType.RPAREN or TokenType.EOF;

    SExpr ParseExprCore() {
        if (Check(TokenType.LPAREN))
        {
            var open = Advance();
            var items = new List<SExpr>();
            while (!Check(TokenType.RPAREN) && !IsAtEnd) items.Add(ParseExpr());
            if (Check(TokenType.RPAREN)) Advance();
            // ⚠ 从前这里**不报错**：EOF 到了就带着没闭合的列表原样返回 ⇒
            //   `(define (f x) (+ x 1)` 这种"写了一半就先存一下"的文件**编译成功**、
            //   退出码 0，而生成器对着一棵缺胳膊少腿的树照样出指令。
            //   位置锚在**开括号**上（缺口就是它没被关上），不是锚在 EOF 上。
            else GccErrorAt("括号未闭合（缺少 ')'）", open, ErrorCode.Parser_SyntaxError);
            return new SList(items);
        }
        if (Check(TokenType.QUOTE)) {
            var q = Advance();
            // `'` 后面必须跟一个表达式；文件正好在此结束（或跟了 `)`）就是缺口。
            if (IsAtEnd || Check(TokenType.RPAREN))
                GccErrorAt("' 后面缺少表达式", q, ErrorCode.Parser_SyntaxError);
            var quoted = ParseExpr();
            return new SList([new SSym("quote"), quoted]);
        }
        if (Check(TokenType.NUMBER)) { var v = Advance().Value; if (v.Contains('.')) return new SDouble(double.Parse(v)); return new SInt(int.Parse(v)); }
        if (Check(TokenType.STRING)) { return new SStr(Advance().Value); }
        if (Check(TokenType.TRUE)) { Advance(); return new SBool(true); }
        if (Check(TokenType.FALSE)) { Advance(); return new SBool(false); }
        // 能从这儿落下来的只剩 SYMBOL 与**畸形记号**（多余的 `)`、EOF）。
        //
        // ⚠ 原来这里是无条件的 `new SSym(Advance().Value)` —— 于是 `(display "a"))`
        //   里多出来的那个 `)` 被当成名叫 `")"` 的符号**静默收下**（实测退出码 0）。
        //   符号名恰好等于 `)` / `<eof>` 是绝无可能合法的，所以按类型拦。
        if (Check(TokenType.SYMBOL)) return new SSym(Advance().Value);
        if (Check(TokenType.RPAREN))
            GccErrorAt("多余的 ')'", CurrentToken, ErrorCode.Parser_SyntaxError);
        else
            GccErrorAt("表达式缺失（输入在此结束）", CurrentToken, ErrorCode.Parser_SyntaxError);
        // 收下现状、让 `Parse()` 的循环去判断能不能继续 —— 报错与"还编不编得下去"
        // 是两件事（本仓的"错误两分"）：这里只是把缺口报出来。
        Advance();
        return new SSym("<error>");
    }
}
