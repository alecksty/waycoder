namespace ObjCCompiler;

public enum TokenType
{
    // C keywords
    Int, Void, Char, Float, Double, Short, Long, Signed, Unsigned,
    If, Else, While, For, Return, Break, Continue,
    Enum, Struct, Union, Switch, Case, Default,
    Const, Do, Goto, Static, Typedef, Extern, StructDecl, Native,

    // ObjC keywords
    Interface, Implementation, Protocol, End, Property, Synthesize, Dynamic,
    Selector, IdType, Class, Super, Self, NilObj, YesObj, NoObj,
    Try, Catch, Finally, Throw, Synchronized, Autoreleasepool,
    Strong, Weak, Copy, Assign_R, Readonly, Readwrite, Nonatomic, Atomic_,
    Block,

    // Literals
    Identifier, Number, String, CharLiteral, ObjCString,

    // Operators
    Plus, Minus, Star, Slash, Percent,
    PlusAssign, MinusAssign, MulAssign, DivAssign, ModAssign,
    Eq, Neq, Lt, Le, Gt, Ge,
    And, Or, Not, Amp, Pipe, Caret, Tilde,
    LShift, RShift, Increment, Decrement,
    Assign, Arrow, Dot, Ellipsis,

    // Delimiters
    LParen, RParen, LBrace, RBrace, LBracket, RBracket,
    Comma, Semicolon, Colon, Question,

    // Special
    AtSign, Hash, Newline, EOF, Error
}

public class Token(TokenType type, string value, int line, int column)
{
    public TokenType Type { get; } = type;
    public string Value { get; } = value;
    public int Line { get; } = line;
    public int Column { get; } = column;
    public override string ToString() => $"{Type}({Value}) at {Line}:{Column}";
}
