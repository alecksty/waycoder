namespace RubyCompiler;

public enum TokenType
{
    // Keywords
    Def, End, Class, Module, If, Elsif, Else, Unless, While, Until, For, In, Do,
    Return, Yield, Self, Nil, True, False, And, Or, Not, Begin, Rescue, Ensure, RaiseKw, Case, When,
    Include, Extend, Native,

    // Literals
    Identifier, Integer, Float, String, Symbol,

    // Operators
    Plus, Minus, Mul, Div, Mod, Pow,
    Eq, Neq, Lt, Gt, Le, Ge, Cmp, Assign,
    PlusAssign, MinusAssign, MulAssign, DivAssign,

    // Delimiters
    LParen, RParen, LBrace, RBrace, LBracket, RBracket,
    Comma, Semicolon, Newline, Colon, Dot, Range, RangeEx,
    FatArrow, ThinArrow,
    Question, // ? 三元运算符

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
