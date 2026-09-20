using System;

namespace RustCompiler
{
    /// <summary>
    /// Token类型枚举
    /// </summary>
    public enum TokenType
    {
        // 标识符和字面量
        IDENTIFIER,     // 标识符
        INTEGER,        // 整数字面量
        FLOAT,          // 浮点数字面量
        STRING,         // 字符串字面量
        CHARACTER,      // 字符字面量
        
        // 关键字
        FN,             // fn
        LET,            // let
        MUT,            // mut
        CONST,          // const
        STATIC,         // static
        IF,             // if
        ELSE,           // else
        WHILE,          // while
        FOR,            // for
        LOOP,           // loop
        MATCH,          // match
        RETURN,         // return
        BREAK,          // break
        CONTINUE,       // continue
        TRUE,           // true
        FALSE,          // false
        AS,             // as
        USE,            // use
        MOD,            // mod
        STRUCT,         // struct
        ENUM,           // enum
        IMPL,           // impl
        TRAIT,          // trait
        WHERE,          // where
        TYPE,           // type
        PUB,            // pub
        CRATE,          // crate
        SUPER,          // super
        SELF,           // self
        SELF_TYPE,      // Self
        
        // 类型关键字
        I8, I16, I32, I64, I128, ISIZE,
        U8, U16, U32, U64, U128, USIZE,
        F32, F64,
        BOOL, CHAR, STR,
        
        // 运算符
        PLUS,           // +
        MINUS,          // -
        STAR,           // *
        SLASH,          // /
        PERCENT,        // %
        CARET,          // ^
        AMPERSAND,      // &
        PIPE,           // |
        TILDE,          // ~
        BANG,           // !
        AND,            // &&
        OR,             // ||
        SHL,            // <<
        SHR,            // >>
        
        // 赋值运算符
        EQ,             // =
        PLUSEQ,         // +=
        MINUSEQ,        // -=
        STAREQ,         // *=
        SLASHEQ,        // /=
        PERCENTEQ,      // %=
        CARETEQ,        // ^=
        AMPERSANDEQ,    // &=
        PIPEEQ,         // |=
        SHLEQ,          // <<=
        SHREQ,          // >>=
        
        // 比较运算符
        EQEQ,           // ==
        BANGEQ,         // !=
        LT,             // <
        GT,             // >
        LTEQ,           // <=
        GTEQ,           // >=
        
        // 分隔符
        LPAREN,         // (
        RPAREN,         // )
        LBRACE,         // {
        RBRACE,         // }
        LBRACKET,       // [
        RBRACKET,       // ]
        COMMA,          // ,
        DOT,            // .
        COLON,          // :
        SEMICOLON,      // ;
        ARROW,          // ->
        FAT_ARROW,      // =>
        RANGE,          // ..
        RANGE_INCLUSIVE,// ..=
        AT,             // @
        HASH,           // #
        DOLLAR,         // $
        UNDERSCORE,     // _
        QUESTION,       // ?
        
        // 宏相关
        MACRO_BANG,     // !
        
        // 特殊
        EOF,            // 文件结束
        ERROR           // 错误token
    }
    
    /// <summary>
    /// Token结构
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
            return $"Token({Type}, '{Value}', line:{Line}, col:{Column})";
        }
    }
}