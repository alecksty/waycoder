using System.Collections.Generic;

namespace DCompiler;

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

// Declarations
public class VarDeclNode(string name, string typeName, ASTNode? initializer, int line, int col)
    : ASTNode(line, col)
{
    public string Name { get; } = name;
    public string TypeName { get; } = typeName;
    public ASTNode? Initializer { get; } = initializer;
}

public class FuncDefNode(string name, string returnType, List<(string name, string type)> parameters, List<ASTNode> body, int line, int col)
    : ASTNode(line, col)
{
    public string Name { get; } = name;
    public string ReturnType { get; } = returnType;
    public List<(string name, string type)> Parameters { get; } = parameters;
    public List<ASTNode> Body { get; } = body;
}

public class ClassDeclNode(string name, List<ASTNode> members, int line, int col)
    : ASTNode(line, col)
{
    public string Name { get; } = name;
    public List<ASTNode> Members { get; } = members;
}

// Statements
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

public class WhileNode(ASTNode condition, List<ASTNode> body, int line, int col)
    : ASTNode(line, col)
{
    public ASTNode Condition { get; } = condition;
    public List<ASTNode> Body { get; } = body;
}

public class ForNode(ASTNode? init, ASTNode? condition, ASTNode? increment, List<ASTNode> body, int line, int col)
    : ASTNode(line, col)
{
    public ASTNode? Init { get; } = init;
    public ASTNode? Condition { get; } = condition;
    public ASTNode? Increment { get; } = increment;
    public List<ASTNode> Body { get; } = body;
}

// Expressions
public class LiteralNode(object? value, int line, int col) : ASTNode(line, col)
{
    public object? Value { get; } = value;
}

public class VarNode(string name, int line, int col) : ASTNode(line, col)
{
    public string Name { get; } = name;
}

public class AssignNode(string name, ASTNode value, int line, int col) : ASTNode(line, col)
{
    public string Name { get; } = name;
    public ASTNode Value { get; } = value;
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

public class CallNode(string function, List<ASTNode> arguments, int line, int col)
    : ASTNode(line, col)
{
    public string Function { get; } = function;
    public List<ASTNode> Arguments { get; } = arguments;
}

public class BreakNode(int line, int col) : ASTNode(line, col) { }

public class ContinueNode(int line, int col) : ASTNode(line, col) { }

public class DoWhileNode(ASTNode condition, List<ASTNode> body, int line, int col)
    : ASTNode(line, col)
{
    public ASTNode Condition { get; } = condition;
    public List<ASTNode> Body { get; } = body;
}

public class IndexNode(string name, ASTNode index, int line, int col) : ASTNode(line, col)
{
    public string Name { get; } = name;
    public ASTNode Index { get; } = index;
}

public class ForeachNode(string varName, string? keyName, ASTNode collection, List<ASTNode> body, int line, int col)
    : ASTNode(line, col)
{
    public string VarName { get; } = varName;
    public string? KeyName { get; } = keyName;
    public ASTNode Collection { get; } = collection;
    public List<ASTNode> Body { get; } = body;
}

public class PrefixPostfixNode(string op, ASTNode operand, bool isPrefix, int line, int col)
    : ASTNode(line, col)
{
    public string Op { get; } = op;
    public ASTNode Operand { get; } = operand;
    public bool IsPrefix { get; } = isPrefix;
}

public class SwitchNode(ASTNode expr, List<CaseNode> cases, int line, int col)
    : ASTNode(line, col)
{
    public ASTNode Expression { get; } = expr;
    public List<CaseNode> Cases { get; } = cases;
}

public class CaseNode(ASTNode? value, List<ASTNode> body, int line, int col)
    : ASTNode(line, col)
{
    public ASTNode? Value { get; } = value; // null = default
    public List<ASTNode> Body { get; } = body;
}

public class CastExpr(string targetType, ASTNode expression, int line, int col)
    : ASTNode(line, col)
{
    public string TargetType { get; } = targetType;
    public ASTNode Expression { get; } = expression;
}
