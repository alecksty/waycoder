// =============================================================
// Token.cs —— 词法记号定义
//
// 词法分析器输出的一颗颗 Token，携带类型、文本/字面量值、行号。
// 数字与字符串字面量直接解析为 double / string。
// =============================================================

namespace QBasic.Compiler;

/// <summary>记号类型。</summary>
public enum TokenType
{
    Number,        // 数字字面量
    Str,           // 字符串字面量
    Ident,         // 标识符 / 关键字
    Op,            // 运算符
    Newline,       // 行尾
    LineNum,       // 行号标签
    Eof,
}

/// <summary>单个词法记号。</summary>
public struct Token
{
    public TokenType Type;
    public string Text;      // 原始文本
    public double Num;       // Number 类型时
    public string Str;       // Str 类型时
    public int Line;         // 行号（1-based）

    public override string ToString() =>
        Type == TokenType.Number ? $"Number({Num})" :
        Type == TokenType.Str ? $"Str(\"{Str}\")" : $"{Type}({Text})";
}
