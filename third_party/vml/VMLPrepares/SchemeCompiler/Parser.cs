using CompilerBase;

namespace SchemeCompiler;

public class Parser : ParserBase<Token, TokenType>
{
    protected override TokenType GetTokenType(Token token) => token.Type;

    public Parser(List<Token> tokens) : base(tokens) { }

    public SExpr Parse() {
        var exprs = new List<SExpr>();
        while (!IsAtEnd) exprs.Add(ParseExpr());
        return exprs.Count == 1 ? exprs[0] : new SList(exprs);
    }

    SExpr ParseExpr() {
        if (Check(TokenType.LPAREN)) { Advance(); var items = new List<SExpr>(); while (!Check(TokenType.RPAREN) && !IsAtEnd) items.Add(ParseExpr()); if (Check(TokenType.RPAREN)) Advance(); return new SList(items); }
        if (Check(TokenType.QUOTE)) { Advance(); var quoted = ParseExpr(); return new SList([new SSym("quote"), quoted]); }
        if (Check(TokenType.NUMBER)) { var v = Advance().Value; if (v.Contains('.')) return new SDouble(double.Parse(v)); return new SInt(int.Parse(v)); }
        if (Check(TokenType.STRING)) { return new SStr(Advance().Value); }
        if (Check(TokenType.TRUE)) { Advance(); return new SBool(true); }
        if (Check(TokenType.FALSE)) { Advance(); return new SBool(false); }
        return new SSym(Advance().Value);
    }
}
