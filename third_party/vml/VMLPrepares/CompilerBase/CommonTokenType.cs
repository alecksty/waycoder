namespace CompilerBase;

/// <summary>所有编译器共享的公共 Token 类型</summary>
public enum CommonTokenType
{
    // 字面量
    IntegerLiteral,
    FloatLiteral,
    StringLiteral,
    CharLiteral,
    TrueLiteral,
    FalseLiteral,
    NullLiteral,

    // 标识符
    Identifier,

    // 算术运算符
    Plus,           // +
    Minus,          // -
    Star,           // *
    Slash,          // /
    Percent,        // %

    // 比较运算符
    Equal,          // ==
    NotEqual,       // !=
    Less,           // <
    Greater,        // >
    LessEqual,      // <=
    GreaterEqual,   // >=

    // 逻辑运算符
    LogicalAnd,     // &&
    LogicalOr,      // ||
    LogicalNot,     // !

    // 位运算符
    BitwiseAnd,     // &
    BitwiseOr,      // |
    BitwiseXor,     // ^
    BitwiseNot,     // ~
    ShiftLeft,      // <<
    ShiftRight,     // >>

    // 赋值
    Assign,         // =
    PlusAssign,     // +=
    MinusAssign,    // -=
    StarAssign,     // *=
    SlashAssign,    // /=

    // 自增自减
    Increment,      // ++
    Decrement,      // --

    // 分隔符
    LParen,         // (
    RParen,         // )
    LBrace,         // {
    RBrace,         // }
    LBracket,       // [
    RBracket,       // ]
    Comma,          // ,
    Semicolon,      // ;
    Colon,          // :
    Dot,            // .
    Arrow,          // ->

    // 三元
    Question,       // ?
    Colon2,         // ::

    // 特殊
    EndOfFile,
    Unknown,
}
