namespace CSharpCompiler
{
    /// <summary>
    /// C#词法单元类型
    /// </summary>
    public enum TokenType
    {
        // 标识符和字面量
        Identifier,
        IntegerLiteral,
        FloatLiteral,
        StringLiteral,
        BooleanLiteral,
        CharacterLiteral,
        NullLiteral,
        
        // 关键字
        Using,
        Namespace,
        Class,
        Struct,
        Interface,
        Enum,
        Delegate,
        Event,
        Method,
        Property,
        Field,
        Constructor,
        Destructor,
        Get,
        Set,
        Add,
        Remove,
        Operator,
        Implicit,
        Explicit,
        Params,
        Ref,
        Out,
        In,
        This,
        Base,
        New,
        Override,
        Abstract,
        Virtual,
        Sealed,
        Static,
        Readonly,
        Volatile,
        Extern,
        Native,
        Alias,
        Public,
        Private,
        Protected,
        Async,
        Await,
        Partial,
        Const,
        Fixed,
        Unsafe,
        Stackalloc,
        Checked,
        Unchecked,
        Default,
        If,
        Else,
        Switch,
        Case,
        DefaultCase,
        For,
        Foreach,
        While,
        Do,
        Break,
        Continue,
        Goto,
        Return,
        Throw,
        Try,
        Catch,
        Finally,
        Lock,
        UsingStatement,
        Yield,
        Var,
        Dynamic,
        Object,
        String,
        Bool,
        Byte,
        SByte,
        Short,
        UShort,
        Int,
        UInt,
        Long,
        ULong,
        Float,
        Double,
        Decimal,
        Char,
        Void,
        True,
        False,
        Null,
        Is,
        As,
        Typeof,
        Sizeof,

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
        Assignment,         // =
        NullCoalescing,     // ??
        NullConditional,    // ?.
        Lambda,             // =>
        Increment,          // ++
        Decrement,          // --
        Conditional,        // ?
        Colon,              // :
        
        // 分隔符
        LeftParen,          // (
        RightParen,         // )
        LeftBrace,          // {
        RightBrace,         // }
        LeftBracket,        // [
        RightBracket,       // ]
        Comma,              // ,
        Semicolon,          // ;
        Dot,                // .
        Question,           // ?
        
        // 注释
        SingleLineComment,
        MultiLineComment,
        
        // 其他
        EndOfFile,
        Error,
        OperatorToken
    }
    
    /// <summary>
    /// 令牌
    /// </summary>
    public class Token : CompilerBase.ITokenPosition
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