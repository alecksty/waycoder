namespace DCompiler;

public enum TokenType
{
    // Keywords
    Module, Import, Void, Int, Float, Double, Long, Bool, String, Char,
    Class, Struct, Interface, Enum,
    If, Else, For, While, Do, Return,
    True, False, Null, New, This,
    Foreach, Break, Continue,
    Try, Catch, Finally, Throw,
    Switch, Case, Default,
    Auto, Immutable, Const, Scope, Union, With,
    Cast,

    // Literals
    Identifier, Integer, FloatLiteral, StringLiteral, CharLiteral,

    // Operators
    Plus, Minus, Mul, Div, Mod, Pow,
    Eq, Neq, Lt, Gt, Le, Ge, Assign,
    PlusAssign, MinusAssign, MulAssign, DivAssign,
    And, Or, Not,
    Increment, Decrement,
    Amp, Pipe, Caret, Tilde,
    LShift, RShift,
    Question, Colon,

    // Delimiters
    LParen, RParen, LBrace, RBrace, LBracket, RBracket,
    Comma, Semicolon, Dot,

    // Comments
    LineComment, BlockComment,

    EOF
}

public class Token(TokenType type, string value, int line, int column)
{
    public TokenType Type { get; } = type;
    public string Value { get; } = value;
    public int Line { get; } = line;
    public int Column { get; } = column;
    public override string ToString() => $"{Type}({Value}) at {Line}:{Column}";
}
