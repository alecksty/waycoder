namespace CCompiler
{
    /// <summary>
    /// 词法类型
    /// </summary>
    public enum TokenType
    {
        // 关键字
        INT,
        VOID,
        CHAR,
        FLOAT,
        DOUBLE,
        SHORT,
        LONG,
        SIGNED,
        UNSIGNED,
        IF,
        ELSE,
        WHILE,
        FOR,
        RETURN,
        BREAK,
        CONTINUE,
        ENUM,
        STRUCT,
        UNION,
        SWITCH,
        CASE,
        DEFAULT,
        ASM,
        AUTO,
        CONST,
        DO,
        GOTO,
        REGISTER,
        SIZEOF,
        STATIC,
        TYPEDEF,
        VOLATILE,
        EXTERN,
        
        // 扩展关键字
        INTERRUPT,
        CHIPASM,
        STDCALL,
        FASTCALL,
        CDECL,
        TRY,
        CATCH,
        THROW,

        // C99 关键字
        INLINE,
        RESTRICT,
        BOOL,
        COMPLEX,
        IMAGINARY,
        
        // C99 固定宽度整数类型
        INT8,
        INT16,
        INT32,
        INT64,
        UINT8,
        UINT16,
        UINT32,
        UINT64,
        INTPTR_T,
        UINTPTR_T,
        SIZE_T,
        SSIZE_T,
        PTRDIFF_T,
        WCHAR_T,
        CHAR32_T,
        
        // 预处理器指令
        INCLUDE,
        DEFINE,
        IF_PRE,
        ELSE_PRE,
        ENDIF,
        IFDEF,
        IFNDEF,
        
        // 标识符和字面量
        IDENTIFIER,
        NUMBER,
        STRING,          // "string" (8-bit)
        WSTRING,         // L"string" (16-bit wchar_t)
        USTRING,         // U"string" (32-bit char32_t)
        CHAR_LITERAL,    // 'x'
        WCHAR_LITERAL,   // L'x'
        UCHAR_LITERAL,   // U'x'
        
        // 运算符
        PLUS,
        MINUS,
        STAR,
        SLASH,
        PERCENT,
        AMPERSAND,
        PIPE,
        CARET,
        TILDE,
        LSHIFT,
        RSHIFT,
        INCREMENT,
        DECREMENT,
        
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
        EQ,
        NE,
        LT,
        LE,
        GT,
        GE,
        
        AND,
        OR,
        NOT,
        
        // 其他运算符
        DOT,
        ELLIPSIS,
        ARROW,
        QUESTION,
        COLON,
        
        // 分隔符
        LPAREN,
        RPAREN,
        LBRACE,
        RBRACE,
        LBRACKET,
        RBRACKET,
        
        COMMA,
        SEMICOLON,
        
        // 特殊
        EOF,
        ERROR
    }
}