namespace JavaScriptCompiler
{
    /// <summary>
    /// JavaScript 令牌类型
    /// </summary>
    public enum TokenType
    {
        // 标识符和关键字
        Identifier,
        Keyword,

        // 字面量
        NumberLiteral,
        StringLiteral,
        True,
        False,
        Null,
        Undefined,

        // 运算符
        Assign,           // =
        Plus,             // +
        Minus,            // -
        Multiply,         // *
        Divide,           // /
        Modulo,           // %
        PlusAssign,       // +=
        MinusAssign,      // -=
        MultiplyAssign,   // *=
        DivideAssign,     // /=
        ModuloAssign,     // %=
        Equal,            // ==
        NotEqual,         // !=
        StrictEqual,      // ===
        StrictNotEqual,   // !==
        LessThan,         // <
        LessThanOrEqual,  // <=
        GreaterThan,      // >
        GreaterThanOrEqual, // >=
        LogicalAnd,       // &&
        LogicalOr,        // ||
        LogicalNot,       // !
        BitwiseAnd,       // &
        BitwiseOr,        // |
        BitwiseXor,       // ^
        BitwiseNot,       // ~
        LeftShift,        // <<
        RightShift,       // >>
        UnsignedRightShift, // >>>
        Exponent,          // **
        Increment,        // ++
        Decrement,        // --
        Typeof,           // typeof

        // 分隔符
        LeftParen,        // (
        RightParen,       // )
        LeftBrace,        // {
        RightBrace,       // }
        LeftBracket,      // [
        RightBracket,     // ]
        Comma,            // ,
        Semicolon,        // ;
        Colon,            // :
        Dot,              // .
        Question,         // ?
        Arrow,            // =>

        // 模板字符串
        Backtick,            // `
        TemplateInterp,      // ${

        // 其他
        EndOfFile,
        Unknown
    }
}
