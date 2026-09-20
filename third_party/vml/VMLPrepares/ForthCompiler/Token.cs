namespace ForthCompiler
{
    /// <summary>
    /// Forth语言Token类型
    /// </summary>
    public enum TokenType
    {
        // 特殊Token
        EOF,
        NEWLINE,
        COMMENT,
        
        // 字面量
        NUMBER,
        STRING,
        CHARACTER,
        
        // 标识符和词
        IDENTIFIER,
        COLON,          // : 开始词定义
        SEMICOLON,      // ; 结束词定义
        
        // 栈操作词
        DUP,
        DROP,
        SWAP,
        OVER,
        ROT,
        QDUP,           // ?DUP
        
        // 算术运算
        PLUS,           // +
        MINUS,          // -
        MULTIPLY,       // *
        DIVIDE,         // /
        MOD,
        DIVMOD,         // /MOD
        INCREMENT,      // 1+
        DECREMENT,      // 1-
        DOUBLE,         // 2*
        HALF,           // 2/
        
        // 比较运算
        EQUAL,          // =
        NOTEQUAL,       // <>
        LESSTHAN,       // <
        GREATERTHAN,    // >
        LESSEQUAL,      // <=
        GREATEREQUAL,   // >=
        ZEROEQUAL,      // 0=
        ZERONOTEQUAL,   // 0<>
        ZEROLESSTHAN,   // 0<
        ZEROGREATERTHAN,// 0>
        
        // 逻辑运算
        AND,
        OR,
        XOR,
        NOT,
        INVERT,
        
        // 控制流
        IF,
        THEN,
        ELSE,
        BEGIN,
        UNTIL,
        WHILE,
        REPEAT,
        AGAIN,
        DO,
        LOOP,
        PLUSLOOP,       // +LOOP
        I,
        J,
        LEAVE,
        RECURSE,        // RECURSE — recursive call to current word
        CASE,
        OF,
        ENDOF,
        ENDCASE,
        
        // 内存操作
        STORE,          // !
        FETCH,          // @
        CSTORE,         // C!
        CFETCH,         // C@
        ALLOT,
        HERE,
        ALLOC,
        FREE,

        // 浮点类型和操作
        FLOAT,
        SFLOAT,
        DFLOAT,
        FPLUS,          // F+
        FMINUS,         // F-
        FMULTIPLY,      // F*
        FDIVIDE,        // F/
        FEQUAL,         // F=
        FLESSTHAN,      // F<
        FGREATERTHAN,   // F>
        FLESSEQUAL,     // F<=
        FGREATEREQUAL,  // F>=
        FLOAD,          // FLOAD
        FSTORE,         // FSTORE
        FDOT,           // F.

        // 文件操作
        FOPEN,
        FCLOSE,
        FREAD,
        FWRITE,
        FSEEK,
        FTELL,

        // 异常处理
        CATCH,
        THROW,
        ENDCATCH,
        
        // 输入输出
        DOT,            // .
        DOTSTRING,      // ."
        DOTQUOTE,       // .(
        EMIT,
        KEY,
        CR,
        SPACE,
        SPACES,
        
        // 其他常用词
        DUP2,           // 2DUP
        DROP2,          // 2DROP
        SWAP2,          // 2SWAP
        OVER2,          // 2OVER
        ROT2,           // 2ROT
        PICK,
        ROLL,
        DEPTH,
        CLEAR,
        
        // 变量和常量
        VARIABLE,
        CONSTANT,
        CREATE,
        DOES,
        
        // 字符串操作
        TYPE,
        COUNT,
        TRAILING,
        
        // 注释
        PAREN_COMMENT,  // ( ... )
        BACKSLASH_COMMENT, // \
        
        // ASM_KW 已移除 — asm() 仅限 C/ObjC/C++ 语言，Forth 通过 Lib/shared/vmlsys.c 调用系统功能
        // 其他
        EXECUTE,
        EXIT,
        ABORT,
        QUIT,
        BYE
    }

    /// <summary>
    /// Forth语言Token
    /// </summary>
    public class Token : CompilerBase.ITokenPosition
    {
        public TokenType Type { get; }
        public string Value { get; }
        public int Line { get; }
        public int Column { get; }

        public Token(TokenType type, string value, int line, int column)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
        }

        public Token(TokenType type, int line, int column) : this(type, null, line, column) { }

        public override string ToString()
        {
            return Value != null ? $"{Type}('{Value}') at {Line}:{Column}" : $"{Type} at {Line}:{Column}";
        }
    }
}
