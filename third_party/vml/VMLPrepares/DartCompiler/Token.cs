namespace DartCompiler;

public enum TokenType
{
    // Keywords
    Class, Void, Int, DoubleKw, StringKw, Bool, Var, Final,
    If, Else, For, While, Return, True, False, Null,
    DoKw, Break, Continue, Mixin, WithKw, Enum, Extension, OnKw,
    IsKw, AsKw, External,
    TryKw, CatchKw, ThrowKw, FinallyKw,

    // Literals
    Identifier, Integer, Float, StringLit,

    // Operators
    Plus, Minus, Mul, Div, Mod,
    PlusAssign, MinusAssign, MulAssign, DivAssign, ModAssign,
    Eq, Neq, Lt, Gt, Le, Ge, Assign,
    AndAnd, OrOr, Not,
    BitNot,       // ~
    IntDiv,       // ~/
    Increment, Decrement,
    Question, Colon,

    // Delimiters
    LParen, RParen, LBrace, RBrace, LBracket, RBracket,
    Comma, Semicolon, Dot,

    Comment, EOF
}

public class Token(TokenType type, string value, int line, int column)
{
    public TokenType Type { get; } = type;
    public string Value { get; } = value;
    public int Line { get; } = line;
    public int Column { get; } = column;
    public override string ToString() => $"{Type}({Value}) at {Line}:{Column}";
}
