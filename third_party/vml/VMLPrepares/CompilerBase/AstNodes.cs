using System.Collections.Generic;

namespace CompilerBase
{
    /// <summary>
    /// 所有语言编译器 AST 节点的抽象基类。
    /// 包含 22 个编译器中高度重复的通用节点类型。
    /// 各语言编译器可继承或 using-alias 这些节点。
    /// </summary>
    public abstract class AstNode(int line, int column)
    {
        public int Line { get; } = line;
        public int Column { get; } = column;
    }

    /// <summary>程序根节点</summary>
    public class ProgramNode : AstNode
    {
        public List<AstNode> Statements { get; } = new();
        public ProgramNode() : base(0, 0) { }
    }

    /// <summary>字面量节点 (int/float/string/null)</summary>
    public class LiteralNode(object? value, int line, int col) : AstNode(line, col)
    {
        public object? Value { get; } = value;
    }

    /// <summary>变量引用节点</summary>
    public class VarNode(string name, int line, int col) : AstNode(line, col)
    {
        public string Name { get; } = name;
    }

    /// <summary>二元运算节点</summary>
    public class BinaryNode(AstNode left, string op, AstNode right, int line, int col) : AstNode(line, col)
    {
        public AstNode Left { get; } = left;
        public string Op { get; } = op;
        public AstNode Right { get; } = right;
    }

    /// <summary>一元运算节点</summary>
    public class UnaryNode(string op, AstNode operand, int line, int col) : AstNode(line, col)
    {
        public string Op { get; } = op;
        public AstNode Operand { get; } = operand;
    }

    /// <summary>函数调用节点</summary>
    public class CallNode(string name, List<AstNode> arguments, int line, int col) : AstNode(line, col)
    {
        public string Name { get; } = name;
        public List<AstNode> Arguments { get; } = arguments;
    }

    /// <summary>return 语句节点</summary>
    public class ReturnNode(AstNode? value, int line, int col) : AstNode(line, col)
    {
        public AstNode? Value { get; } = value;
    }

    /// <summary>if 语句节点</summary>
    public class IfNode(AstNode condition, AstNode thenBody, AstNode? elseBody, int line, int col) : AstNode(line, col)
    {
        public AstNode Condition { get; } = condition;
        public AstNode ThenBody { get; } = thenBody;
        public AstNode? ElseBody { get; } = elseBody;
    }

    /// <summary>while 循环节点</summary>
    public class WhileNode(AstNode condition, AstNode body, int line, int col) : AstNode(line, col)
    {
        public AstNode Condition { get; } = condition;
        public AstNode Body { get; } = body;
    }

    /// <summary>赋值节点</summary>
    public class AssignNode(string name, AstNode value, int line, int col) : AstNode(line, col)
    {
        public string Name { get; } = name;
        public AstNode Value { get; } = value;
    }

    /// <summary>break 语句</summary>
    public class BreakNode(int line, int col) : AstNode(line, col);

    /// <summary>continue 语句</summary>
    public class ContinueNode(int line, int col) : AstNode(line, col);

    /// <summary>表达式语句包装</summary>
    public class ExprStmtNode(AstNode expression, int line, int col) : AstNode(line, col)
    {
        public AstNode Expression { get; } = expression;
    }

    /// <summary>代码块</summary>
    public class BlockNode(List<AstNode> statements, int line, int col) : AstNode(line, col)
    {
        public List<AstNode> Statements { get; } = statements;
    }
}
