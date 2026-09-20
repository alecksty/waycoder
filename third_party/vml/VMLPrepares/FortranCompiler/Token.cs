namespace FortranCompiler;

public enum TokenType
{
    // Keywords
    Program, Subroutine, Function, End, Integer, Real, Double, Complex, Logical, Character,
    If, Then, Else, Do, While, Call, Return, Stop,
    True, False, Parameter,
    Print, Write, Read, Contains, Implicit, None, Result,
    Dimension, Allocatable, Allocate, Deallocate, Intent, In, Out, InOut, Module, Use, Only,
    DoWhileKeyword,  // synthetic: "do" followed by "while"
    Exit, Cycle,  // loop control

    // Literals
    Identifier, IntLiteral, RealLiteral, DoubleLiteral, StringLiteral, LogicalLiteral,

    // Operators
    Plus, Minus, Mul, Div, Pow,
    Eq, Neq, Lt, Gt, Le, Ge,
    Assign, ColonColon,  // = and ::
    LogicalAnd, LogicalOr, LogicalNot, LogicalEqv, LogicalNeqv,

    // Delimiters
    LParen, RParen, Comma, Colon, Dot, Semicolon, Newline,
    Asterisk,  // for print *, read *

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
