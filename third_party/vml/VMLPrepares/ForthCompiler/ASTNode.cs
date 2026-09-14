namespace ForthCompiler
{
    /// <summary>
    /// Forth语言AST节点基类
    /// </summary>
    public abstract class ASTNode
    {
        public int Line { get; set; }
        public int Column { get; set; }
    }

    /// <summary>
    /// Forth程序节点
    /// </summary>
    public class Program : ASTNode
    {
        public List<ASTNode> Definitions { get; } = new List<ASTNode>();
        public List<ASTNode> Words { get; } = new List<ASTNode>();
        public List<ASTNode> Statements { get; } = new List<ASTNode>();
    }

    /// <summary>
    /// 词定义节点
    /// </summary>
    public class WordDefinition : ASTNode
    {
        public string Name { get; set; }
        public List<ASTNode> Body { get; } = new List<ASTNode>();
        public List<string> StackEffect { get; } = new List<string>(); // ( input -- output )
    }

    /// <summary>
    /// 变量定义节点
    /// </summary>
    public class VariableDefinition : ASTNode
    {
        public string Name { get; set; }
        public ASTNode InitialValue { get; set; }
    }

    /// <summary>
    /// CREATE定义节点（字典项/缓冲区）
    /// </summary>
    public class CreateDefinition : ASTNode
    {
        public string Name { get; set; }
        public int AllotSize { get; set; }
    }

    /// <summary>
    /// 常量定义节点
    /// </summary>
    public class ConstantDefinition : ASTNode
    {
        public string Name { get; set; }
        public ASTNode Value { get; set; }
    }

    /// <summary>
    /// 数字字面量节点
    /// </summary>
    public class NumberLiteral : ASTNode
    {
        public string Value { get; set; }
        public bool IsFloat { get; set; }
        public bool IsHex { get; set; }
        public bool IsBinary { get; set; }
    }

    /// <summary>
    /// 字符串字面量节点
    /// </summary>
    public class StringLiteral : ASTNode
    {
        public string Value { get; set; }
    }

    /// <summary>
    /// 字符字面量节点
    /// </summary>
    public class CharLiteral : ASTNode
    {
        public char Value { get; set; }
    }

    /// <summary>
    /// 词调用节点
    /// </summary>
    public class WordCall : ASTNode
    {
        public string Name { get; set; }
        public bool IsRecursive { get; set; } = false; // RECURSE keyword
    }

    /// <summary>
    /// 栈操作节点
    /// </summary>
    public class StackOperation : ASTNode
    {
        public TokenType Operation { get; set; } // DUP, DROP, SWAP, OVER, ROT等
    }

    /// <summary>
    /// 算术运算节点
    /// </summary>
    public class ArithmeticOperation : ASTNode
    {
        public TokenType Operation { get; set; } // +, -, *, /, MOD等
    }

    /// <summary>
    /// 比较运算节点
    /// </summary>
    public class ComparisonOperation : ASTNode
    {
        public TokenType Operation { get; set; } // =, <>, <, >等
    }

    /// <summary>
    /// 逻辑运算节点
    /// </summary>
    public class LogicalOperation : ASTNode
    {
        public TokenType Operation { get; set; } // AND, OR, XOR, NOT等
    }

    /// <summary>
    /// 内存操作节点
    /// </summary>
    public class MemoryOperation : ASTNode
    {
        public TokenType Operation { get; set; } // !, @, C!, C@等
    }

    /// <summary>
    /// 输入输出节点
    /// </summary>
    // AsmStatement 已移除 — asm() 仅限 C/ObjC/C++ 语言，Forth 通过 Lib/shared/vmlsys.c 调用

public class IOOperation : ASTNode
    {
        public TokenType Operation { get; set; } // ., .", EMIT, KEY等
        public ASTNode Argument { get; set; } // 对于."需要字符串参数
    }

    /// <summary>
    /// 条件语句节点
    /// </summary>
    public class IfStatement : ASTNode
    {
        public List<ASTNode> ThenBranch { get; } = new List<ASTNode>();
        public List<ASTNode> ElseBranch { get; } = new List<ASTNode>();
    }

    /// <summary>
    /// 循环语句节点
    /// </summary>
    public class LoopStatement : ASTNode
    {
        public TokenType LoopType { get; set; } // BEGIN...UNTIL, BEGIN...WHILE...REPEAT, DO...LOOP
        public List<ASTNode> Condition { get; } = new List<ASTNode>();
        public List<ASTNode> Body { get; } = new List<ASTNode>();
        public ASTNode StartValue { get; set; } // DO循环的起始值
        public ASTNode EndValue { get; set; }   // DO循环的结束值
    }

    /// <summary>
    /// 注释节点
    /// </summary>
    public class Comment : ASTNode
    {
        public string Text { get; set; }
        public bool IsParenComment { get; set; } // ( ... ) 注释
        public bool IsBackslashComment { get; set; } // \ 行注释
    }

    /// <summary>
    /// 栈索引节点（PICK, ROLL等）
    /// </summary>
    public class StackIndexOperation : ASTNode
    {
        public TokenType Operation { get; set; } // PICK, ROLL
        public ASTNode Index { get; set; }
    }

    /// <summary>
    /// 变量访问节点
    /// </summary>
    public class VariableAccess : ASTNode
    {
        public string Name { get; set; }
        public bool IsFetch { get; set; } = true; // @ 获取值，否则为存储
        public ASTNode Value { get; set; } // 存储时的值
    }

    /// <summary>
    /// 常量访问节点
    /// </summary>
    public class ConstantAccess : ASTNode
    {
        public string Name { get; set; }
    }

    /// <summary>
    /// 退出节点（EXIT）
    /// </summary>
    public class ExitStatement : ASTNode
    {
    }

    /// <summary>
    /// 中止节点（ABORT）
    /// </summary>
    public class AbortStatement : ASTNode
    {
        public ASTNode Message { get; set; }
    }

    /// <summary>
    /// 异常处理节点（CATCH/THROW/END-CATCH）
    /// </summary>
    public class ExceptionOperation : ASTNode
    {
        public TokenType Operation { get; set; }
    }

    /// <summary>
    /// CASE/OF/ENDOF/ENDCASE 多分支节点
    /// </summary>
    public class CaseStatement : ASTNode
    {
        public List<CaseBranch> Branches { get; set; } = new();
        public List<ASTNode> DefaultBody { get; set; } = new();
    }

    public class CaseBranch
    {
        public List<ASTNode> ValueExpr { get; set; } = new();
        public List<ASTNode> Body { get; set; } = new();
    }
}
