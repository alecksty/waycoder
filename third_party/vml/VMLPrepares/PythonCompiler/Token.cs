namespace PythonCompiler
{
    /// <summary>
    /// Python 词法单元类型
    /// </summary>
    public enum TokenType
    {
        // 关键字
        AND, AS, ASSERT, ASYNC, AWAIT,
        BREAK, CLASS, CONTINUE,
        DEF, DEL, ELIF, ELSE, EXCEPT,
        FALSE, FINALLY, FOR, FROM,
        GLOBAL,
        IF, IMPORT, IN, IS,
        LAMBDA,
        MATCH, CASE,
        NONE, NONLOCAL, NOT,
        OR,
        PASS,
        RAISE, RETURN,
        TRUE, TRY,
        WHILE, WITH,
        YIELD,

        // 字面量扩展
        HEX_INTEGER, OCT_INTEGER, BIN_INTEGER,

        // 字符串前缀
        RAW_STRING, FSTRING, BYTES,

        // 标识符和字面量
        IDENTIFIER, INTEGER, FLOAT, STRING,

        // 缩进
        INDENT, DEDENT, NEWLINE,

        // 运算符
        PLUS, MINUS, MUL, DIV, MOD, POWER, FLOOR_DIV,
        ASSIGN, PLUS_ASSIGN, MINUS_ASSIGN, MUL_ASSIGN, DIV_ASSIGN, MOD_ASSIGN, FLOOR_DIV_ASSIGN,
        EQ, NE, LT, LE, GT, GE,
        AND_ASSIGN, OR_ASSIGN, XOR_ASSIGN,
        LSHIFT, RSHIFT,
        BITAND, BITOR, BITXOR, BITNOT,

        // 分隔符
        LPAREN, RPAREN, LBRACKET, RBRACKET, LBRACE, RBRACE,
        COLON, SEMICOLON, COMMA, DOT, ARROW,

        // 其他
        AT,  // @ 装饰器
        EOF
    }

    /// <summary>
    /// Python 词法单元
    /// </summary>
    public class Token
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

        public override string ToString()
        {
            return $"{Type}({Value}) at {Line}:{Column}";
        }
    }
}
