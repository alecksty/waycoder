using System.Collections.Generic;

namespace DartCompiler;

public abstract class ASTNode(int line, int column)
{
    public int Line { get; } = line;
    public int Column { get; } = column;
}

// Program root
public class ProgramNode : ASTNode
{
    public List<ASTNode> Statements { get; } = new();
    public ProgramNode() : base(0, 0) { }
}

// Type representation
public class TypeNode(string name, int line, int col) : ASTNode(line, col)
{
    public string Name { get; } = name;
}

// Parameter: (type, name)
public class ParamNode(string type, string name, int line, int col) : ASTNode(line, col)
{
    public string Type { get; } = type;
    public string Name { get; } = name;
}

// Statements
public class VarDeclNode(string type, string name, ASTNode? init, int line, int col) : ASTNode(line, col)
{
    public string Type { get; } = type;       // "int", "double", "String", "bool", "var", "final", or class name
    public string Name { get; } = name;
    public ASTNode? Init { get; } = init;
}

public class ClassDeclNode(string name, List<ASTNode> members, int line, int col, List<string>? mixinNames = null) : ASTNode(line, col)
{
    public string Name { get; } = name;
    public List<ASTNode> Members { get; } = members;
    public List<string> MixinNames { get; } = mixinNames ?? new List<string>();
    public bool IsMixin { get; set; } = false;
}

public class MethodDeclNode(string name, string returnType, List<ParamNode> parameters, List<ASTNode> body, int line, int col) : ASTNode(line, col)
{
    public string Name { get; } = name;
    public string ReturnType { get; } = returnType;
    public List<ParamNode> Parameters { get; } = parameters;
    public List<ASTNode> Body { get; } = body;
    public bool IsExternal { get; set; }
}

public class ReturnNode(ASTNode? value, int line, int col) : ASTNode(line, col)
{
    public ASTNode? Value { get; } = value;
}

public class IfNode(ASTNode condition, List<ASTNode> thenBody, List<ASTNode>? elseBody, int line, int col) : ASTNode(line, col)
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

public class ForNode(ASTNode? init, ASTNode? condition, ASTNode? increment, List<ASTNode> body, int line, int col) : ASTNode(line, col)
{
    public ASTNode? Init { get; } = init;
    public ASTNode? Condition { get; } = condition;
    public ASTNode? Increment { get; } = increment;
    public List<ASTNode> Body { get; } = body;
}

public class BlockNode(List<ASTNode> statements, int line, int col) : ASTNode(line, col)
{
    public List<ASTNode> Statements { get; } = statements;
}

public class ExprStmtNode(ASTNode expr, int line, int col) : ASTNode(line, col)
{
    public ASTNode Expr { get; } = expr;
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

public class OpAssignNode(string name, string op, ASTNode value, int line, int col) : ASTNode(line, col)
{
    public string Name { get; } = name;
    public string Op { get; } = op;
    public ASTNode Value { get; } = value;
}

// ── 数组（下标）──────────────────────────────────────────────────────────────
// 此前 Dart 前端**完全没有下标这个概念**：`a[i]` 只解析成裸 `a`，`a[i] = v` 的左值被
// ParseAssignment 丢掉（注释自称"容错"）。下面三个节点把下标补上（与 Go/Kotlin/Rust 同形）。

/// <summary>数组字面量 <c>[e0, e1, …]</c>。布局 <c>[count, e0, e1, …]</c>（4 字节/元素）。</summary>
public class ArrayLiteralNode(List<ASTNode> elements, int line, int col) : ASTNode(line, col)
{
    public List<ASTNode> Elements { get; } = elements;
}

/// <summary>下标读取 <c>a[i]</c>。</summary>
public class IndexNode(ASTNode target, ASTNode index, int line, int col) : ASTNode(line, col)
{
    public ASTNode Target { get; } = target;
    public ASTNode Index { get; } = index;
}

/// <summary>下标赋值 <c>a[i] = v</c>。注意左值**不是**变量名，必须整段留着。</summary>
public class IndexAssignNode(ASTNode target, ASTNode index, ASTNode value, int line, int col) : ASTNode(line, col)
{
    public ASTNode Target { get; } = target;
    public ASTNode Index { get; } = index;
    public ASTNode Value { get; } = value;
}

/// <summary>下标复合赋值 <c>a[i] += v</c>。</summary>
public class IndexOpAssignNode(ASTNode target, ASTNode index, string op, ASTNode value, int line, int col) : ASTNode(line, col)
{
    public ASTNode Target { get; } = target;
    public ASTNode Index { get; } = index;
    public string Op { get; } = op;
    public ASTNode Value { get; } = value;
}

public class BinaryNode(ASTNode left, string op, ASTNode right, int line, int col) : ASTNode(line, col)
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

public class BreakNode(int line, int col) : ASTNode(line, col) { }

public class ContinueNode(int line, int col) : ASTNode(line, col) { }

public class DoWhileNode(ASTNode condition, List<ASTNode> body, int line, int col) : ASTNode(line, col)
{
    public ASTNode Condition { get; } = condition;
    public List<ASTNode> Body { get; } = body;
}

public class PrefixPostfixNode(string op, ASTNode operand, bool isPrefix, int line, int col) : ASTNode(line, col)
{
    public string Op { get; } = op;
    public ASTNode Operand { get; } = operand;
    public bool IsPrefix { get; } = isPrefix;
}

public class TryStmt(List<ASTNode> body, List<CatchClause> catches, List<ASTNode>? finallyBlock, int line, int col) : ASTNode(line, col)
{
    public List<ASTNode> Body { get; } = body;
    public List<CatchClause> Catches { get; } = catches;
    public List<ASTNode>? FinallyBlock { get; } = finallyBlock;
}

public class CatchClause(string? varName, string? excType, List<ASTNode> body, int line, int col) : ASTNode(line, col)
{
    public string? VarName { get; } = varName;
    public string? ExcType { get; } = excType;
    public List<ASTNode> Body { get; } = body;
}

public class ThrowStmt(ASTNode? expression, int line, int col) : ASTNode(line, col)
{
    public ASTNode? Expression { get; } = expression;
}
