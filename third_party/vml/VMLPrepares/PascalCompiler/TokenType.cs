namespace PascalCompiler
{
    /// <summary>
    /// Pascal 语言词法单元类型
    /// </summary>
    public enum TokenType
    {
        // 关键字
        PROGRAM, UNIT, INTERFACE, IMPLEMENTATION, USES, VAR, CONST, TYPE, ARRAY, OF, RECORD, END,
        BEGIN, IF, THEN, ELSE, WHILE, DO, FOR, TO, DOWNTO, REPEAT, UNTIL,
        CASE, OF_CASE, OTHERWISE, BREAK, CONTINUE,
        WITH, GOTO, LABEL,
        FUNCTION, PROCEDURE, FORWARD,
        INTEGER, REAL, BOOLEAN, CHAR, STRING,
        TRUE, FALSE, NIL,
        AND, OR, NOT, DIV, MOD, XOR, SHL, SHR,
        IN, OUT, INOUT,
        SET, FILE, TEXT,

        // Delphi/FreePascal OOP (v1.66.32+)
        CLASS, OBJECT, CONSTRUCTOR, DESTRUCTOR, PROPERTY,
        INHERITED, VIRTUAL, OVERRIDE, ABSTRACT, DYNAMIC,
        TRY, EXCEPT, FINALLY, RAISE, AS, IS,

        // 标识符和字面量
        IDENTIFIER,
        INTEGER_LITERAL, REAL_LITERAL, STRING_LITERAL, CHAR_LITERAL,
        
        // 运算符
        PLUS, MINUS, STAR, SLASH, ASSIGN, EQUALS, NOT_EQUALS,
        LESS_THAN, LESS_EQUAL, GREATER_THAN, GREATER_EQUAL,
        DOT, COMMA, COLON, SEMICOLON, RANGE,
        LPAREN, RPAREN, LBRACKET, RBRACKET,
        CARET, AT, PIPE, BACKSLASH,
        
        // 特殊
        EOF
    }
}
