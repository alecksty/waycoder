namespace LuaCompiler
{
    /// <summary>
    /// Lua 词法单元类型
    /// </summary>
    public enum TokenType
    {
        // 关键字
        AND, BREAK, DO, ELSE, ELSEIF, END,
        FALSE, FOR, FUNCTION, GOTO, IF, IN,
        LOCAL, NIL, NOT, OR, REPEAT, RETURN,
        THEN, TRUE, UNTIL, WHILE,

        // 标识符和字面量
        IDENTIFIER, NUMBER, STRING,

        // 运算符
        PLUS, MINUS, MUL, DIV, FLOOR_DIV, MOD, POW,
        CONCAT, LEN, EQ, NE, LT, LE, GT, GE,
        ASSIGN,

        // 分隔符
        LPAREN, RPAREN, LBRACE, RBRACE, LBRACKET, RBRACKET,
        COMMA, SEMICOLON, COLON, DOT, DOTS, DOUBLE_COLON,

        // 注释
        COMMENT,

        // 文件结束
        EOF
    }

    /// <summary>
    /// Lua 词法单元
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

        public override string ToString()
        {
            return $"{Type}({Value}) at {Line}:{Column}";
        }
    }
}