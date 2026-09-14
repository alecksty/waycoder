namespace JavaCompiler
{
    /// <summary>
    /// Java词法单元类型
    /// </summary>
    public enum TokenType
    {
        // 标识符和字面量
        Identifier,
        IntegerLiteral,
        FloatLiteral,
        StringLiteral,
        CharLiteral,
        BooleanLiteral,
        
        // 关键字
        Public,
        Private,
        Protected,
        Static,
        Final,
        Class,
        Interface,
        Enum,
        Extends,
        Implements,
        Import,
        Package,
        New,
        This,
        Super,
        Void,
        Return,
        If,
        Else,
        For,
        While,
        Do,
        Switch,
        Case,
        Default,
        Break,
        Continue,
        Try,
        Catch,
        Finally,
        Throw,
        Throws,
        Synchronized,
        Native,
        Transient,
        Volatile,
        Strictfp,
        Abstract,
        Assert,
        Instanceof,
        
        // 类型
        Int,
        Long,
        Short,
        Byte,
        Char,
        Float,
        Double,
        Boolean,
        String,
        
        // 字面量关键字
        True,
        False,
        Null,
        
        // 运算符
        Plus,          // +
        Minus,         // -
        Multiply,      // *
        Divide,        // /
        Modulo,        // %
        Increment,     // ++
        Decrement,     // --
        Assign,        // =
        PlusAssign,    // +=
        MinusAssign,   // -=
        MultiplyAssign, // *=
        DivideAssign,  // /=
        ModuloAssign,  // %=
        AndAssign,     // &=
        OrAssign,      // |=
        XorAssign,     // ^=
        LeftShiftAssign, // <<=
        RightShiftAssign, // >>=
        UnsignedRightShiftAssign, // >>>=
        
        // 比较运算符
        Equal,         // ==
        NotEqual,      // !=
        LessThan,      // <
        GreaterThan,   // >
        LessThanOrEqual, // <=
        GreaterThanOrEqual, // >=
        
        // 逻辑运算符
        LogicalAnd,    // &&
        LogicalOr,     // ||
        LogicalNot,    // !
        
        // 位运算符
        BitwiseAnd,    // &
        BitwiseOr,     // |
        BitwiseXor,    // ^
        BitwiseNot,    // ~
        LeftShift,     // <<
        RightShift,    // >>
        UnsignedRightShift, // >>>
        
        // 位运算符别名（向后兼容）
        And = BitwiseAnd,
        Or = BitwiseOr,
        Xor = BitwiseXor,
        
        // 分隔符（显式值避免与之前的枚举冲突）
        Semicolon = 93,     // ;
        Comma = 94,         // ,
        Dot = 95,           // .
        Colon = 96,         // :
        QuestionMark = 97,  // ?
        DoubleColon = 98,   // ::
        
        // 括号
        LeftParen = 99,     // (
        RightParen = 100,   // )
        LeftBracket = 101,  // [
        RightBracket = 102, // ]
        LeftBrace = 103,    // {
        RightBrace = 104,   // }
        
        // 注释
        SingleLineComment = 105,
        MultiLineComment = 106,
        
        // 其他
        AtSymbol = 107,     // @
        Ellipsis = 108,     // ...
        Arrow = 109,        // ->
        
        // 特殊
        EndOfFile = 110,
        Error = 111
    }
}