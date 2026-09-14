namespace SwiftCompiler
{
    /// <summary>
    /// Swift词法单元类型
    /// </summary>
    public enum TokenType
    {
        // 标识符和字面量
        Identifier,
        IntegerLiteral,
        FloatLiteral,
        StringLiteral,
        BooleanLiteral,
        
        // 关键字
        Import,
        Class,
        Struct,
        Enum,
        Protocol,
        Extension,
        Func,
        Var,
        Let,
        If,
        Else,
        Switch,
        Case,
        Default,
        For,
        While,
        Repeat,
        Do,
        In,
        Return,
        Break,
        Continue,
        Fallthrough,
        Defer,
        Guard,
        Throw,
        Throws,
        Rethrows,
        Try,
        Catch,
        Async,
        Await,
        Actor,
        Some,
        Any,
        Self,
        Super,
        True,
        False,
        Nil,
        Typealias,
        Associatedtype,
        Where,
        Convenience,
        Dynamic,
        Final,
        Infix,
        Lazy,
        Mutating,
        Nonmutating,
        Optional,
        Override,
        Postfix,
        Prefix,
        Required,
        Static,
        Unowned,
        Weak,
        Private,
        Fileprivate,
        Internal,
        Public,
        Open,
        Native,
        External,
        Print,
        
        // 类型关键字
        Int,
        Double,
        Float,
        Bool,
        String,
        Character,
        Array,
        Dictionary,
        Set,
        OptionalType, // ?
        
        // 运算符
        Plus,               // +
        Minus,              // -
        Multiply,           // *
        Divide,             // /
        Modulo,             // %
        PlusEqual,          // +=
        MinusEqual,         // -=
        MultiplyEqual,      // *=
        DivideEqual,        // /=
        ModuloEqual,        // %=
        Equal,              // ==
        NotEqual,           // !=
        LessThan,           // <
        GreaterThan,        // >
        LessThanOrEqual,    // <=
        GreaterThanOrEqual, // >=
        LogicalAnd,         // &&
        LogicalOr,          // ||
        LogicalNot,         // !
        BitwiseAnd,         // &
        BitwiseOr,          // |
        BitwiseXor,         // ^
        BitwiseNot,         // ~
        LeftShift,          // <<
        RightShift,         // >>
        Range,              // ..
        HalfOpenRange,      // ..<
        Assignment,         // =
        NilCoalescing,      // ??
        OptionalChaining,   // ?.
        ForceUnwrap,        // !
        Arrow,              // ->
        At,                 // @
        Hash,               // #
        Backtick,           // `
        
        // 分隔符
        LeftParen,          // (
        RightParen,         // )
        LeftBrace,          // {
        RightBrace,         // }
        LeftBracket,        // [
        RightBracket,       // ]
        Comma,              // ,
        Semicolon,          // ;
        Colon,              // :
        Dot,                // .
        Question,           // ?
        
        // 注释
        SingleLineComment,
        MultiLineComment,
        
        // 其他
        StringInterpolationStart, // \(
        StringInterpolationEnd,   // )
        Operator,
        EndOfFile,
        Error
    }
    
    /// <summary>
    /// 令牌
    /// </summary>
    public class Token
    {
        public TokenType Type { get; set; }
        public string Value { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        
        public Token(TokenType type, string value, int line, int column)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
        }
        
        public override string ToString()
        {
            return $"{Type}: '{Value}' at {Line}:{Column}";
        }
    }
}