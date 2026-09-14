using System.Collections.Generic;

namespace RCompiler;

public abstract class ASTNode(int line, int column)
{
    public int Line { get; } = line;
    public int Column { get; } = column;
}

public class ProgramNode : ASTNode
{
    public List<ASTNode> Statements { get; } = new();
    public ProgramNode() : base(0, 0) { }
}

// Statements
public class FuncDefNode(string name, List<string> parameters, List<ASTNode> body, int line, int col)
    : ASTNode(line, col)
{
    public string Name { get; } = name;
    public List<string> Parameters { get; } = parameters;
    public List<ASTNode> Body { get; } = body;
}

public class ReturnNode(ASTNode? value, int line, int col) : ASTNode(line, col)
{
    public ASTNode? Value { get; } = value;
}

public class IfNode(ASTNode condition, List<ASTNode> thenBody, List<ASTNode>? elseBody, int line, int col)
    : ASTNode(line, col)
{
    public ASTNode Condition { get; } = condition;
    public List<ASTNode> ThenBody { get; } = thenBody;
    public List<ASTNode>? ElseBody { get; } = elseBody;
}

public class WhileNode(ASTNode condition, List<ASTNode> body, int line, int col) : ASTNode(line, col)
{
    public ASTNode Condition { get; } = condition;
    public List<ASTNode> Body { get; } = body;
}

public class ForNode(string variable, ASTNode sequence, List<ASTNode> body, int line, int col)
    : ASTNode(line, col)
{
    public string Variable { get; } = variable;
    public ASTNode Sequence { get; } = sequence;
    public List<ASTNode> Body { get; } = body;
}

public class RepeatNode(List<ASTNode> body, int line, int col) : ASTNode(line, col)
{
    public List<ASTNode> Body { get; } = body;
}

public class BreakNode(int line, int col) : ASTNode(line, col) { }
public class NextNode(int line, int col) : ASTNode(line, col) { }

// Expressions
public class LiteralNode(object? value, int line, int col) : ASTNode(line, col)
{
    public object? Value { get; } = value;
}

public class VarNode(string name, int line, int col) : ASTNode(line, col)
{
    public string Name { get; } = name;
}

public class AssignNode(string name, ASTNode value, bool isSuper, int line, int col) : ASTNode(line, col)
{
    public string Name { get; } = name;
    public ASTNode Value { get; } = value;
    public bool IsSuper { get; } = isSuper;
}

public class BinaryNode(ASTNode left, string op, ASTNode right, int line, int col)
    : ASTNode(line, col)
{
    public ASTNode Left { get; } = left;
    public string Op { get; } = op;
    public ASTNode Right { get; } = right;
}

public class UnaryNode(string op, ASTNode operand, int line, int col) : ASTNode(line, col)
{
    public string Op { get; } = op;
    public ASTNode Operand { get; } = operand;
}

public class CallNode(string name, List<ASTNode> arguments, int line, int col) : ASTNode(line, col)
{
    public string Name { get; } = name;
    public List<ASTNode> Arguments { get; } = arguments;
}

public class SeqNode(List<ASTNode> elements, int line, int col) : ASTNode(line, col)
{
    public List<ASTNode> Elements { get; } = elements;
}

public class IndexNode(ASTNode target, ASTNode index, int line, int col) : ASTNode(line, col)
{
    public ASTNode Target { get; } = target;
    public ASTNode Index { get; } = index;
}

/// <summary>索引赋值: target[index] <- value</summary>
public class IndexAssignNode(ASTNode target, ASTNode index, ASTNode value, int line, int col) : ASTNode(line, col)
{
    public ASTNode Target { get; } = target;
    public ASTNode Index { get; } = index;
    public ASTNode Value { get; } = value;
}

public class ListNode(List<(string? name, ASTNode value)> elements, int line, int col) : ASTNode(line, col)
{
    public List<(string? name, ASTNode value)> Elements { get; } = elements;
}
