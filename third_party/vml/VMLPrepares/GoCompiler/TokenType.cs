namespace GoCompiler
{
    /// <summary>
    /// Go语言词法类型
    /// </summary>
    public enum TokenType
    {
        // 关键字
        PACKAGE,
        IMPORT,
        FUNC,
        VAR,
        CONST,
        TYPE,
        STRUCT,
        INTERFACE,
        MAP,
        CHAN,
        GO,
        DEFER,
        SELECT,
        IF,
        ELSE,
        FOR,
        RANGE,
        SWITCH,
        CASE,
        DEFAULT,
        BREAK,
        CONTINUE,
        RETURN,
        FALLTHROUGH,
        GOTO,

        // 类型关键字
        INT,
        INT8,
        INT16,
        INT32,
        INT64,
        UINT,
        UINT8,
        UINT16,
        UINT32,
        UINT64,
        UINTPTR,
        FLOAT32,
        FLOAT64,
        COMPLEX64,
        COMPLEX128,
        BOOL,
        STRING,
        BYTE,
        RUNE,
        ERROR,

        // 标识符和字面量
        IDENTIFIER,
        NUMBER,
        STRING_LITERAL,
        RAW_STRING,
        INTERPRETED_STRING,
        CHAR,
        ILLEGAL,
        EOF,

        // 运算符
        PLUS,
        MINUS,
        STAR,
        SLASH,
        PERCENT,
        AMPERSAND,
        PIPE,
        CARET,
        LSHIFT,
        RSHIFT,
        AND_NOT,

        ASSIGN,
        ADD_ASSIGN,
        SUB_ASSIGN,
        MUL_ASSIGN,
        DIV_ASSIGN,
        MOD_ASSIGN,
        AND_ASSIGN,
        OR_ASSIGN,
        XOR_ASSIGN,
        LSHIFT_ASSIGN,
        RSHIFT_ASSIGN,
        AND_NOT_ASSIGN,
        COLON_ASSIGN,

        EQ,
        NE,
        LT,
        LE,
        GT,
        GE,

        AND,
        OR,
        NOT,
        XOR,
        INCREMENT,
        DECREMENT,
        ARROW,
        RECEIVE,

        // 分隔符
        LPAREN,
        RPAREN,
        LBRACE,
        RBRACE,
        LBRACKET,
        RBRACKET,

        COMMA,
        PERIOD,
        SEMICOLON,
        COLON,
        ELLIPSIS,

        // 特殊
        COMMENT,
        NEWLINE
    }
}
