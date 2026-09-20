namespace RCompiler;

public enum TokenType
{
    // Keywords
    Function, If, Else, For, While, Repeat, Return,
    True, False, Null, Na, Inf, In, Break, Next,

    // Literals
    Identifier, Integer, Float, String,

    // Operators
    Plus, Minus, Mul, Div, Pow, Mod,
    Eq, Neq, Lt, Gt, Le, Ge,
    Assign, SuperAssign,   // <- and <<-
    And, Or, Not,          // & | !
    Tilde,                 // ~ formula
    PercentOp,             // %in% and other %...% infix operators

    // Delimiters
    LParen, RParen, LBrace, RBrace, LBracket, RBracket,
    Comma, Semicolon, Colon, Dollar, Dot,

    Comment, EOF
}

public class Token(TokenType type, string value, int line, int column) : CompilerBase.ITokenPosition
{
    public TokenType Type { get; } = type;
    public string Value { get; } = value;
    public int Line { get; } = line;
    public int Column { get; } = column;
    public override string ToString() => $"{Type}({Value}) at {Line}:{Column}";
}
