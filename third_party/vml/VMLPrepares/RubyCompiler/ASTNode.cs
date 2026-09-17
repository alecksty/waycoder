using System.Collections.Generic;

namespace RubyCompiler;

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
public class DefNode(string name, List<string> parameters, List<ASTNode> body, int line, int col, bool isNative = false) : ASTNode(line, col)
{
    public string Name { get; } = name;
    public List<string> Parameters { get; } = parameters;
    public List<ASTNode> Body { get; } = body;
    public bool IsNative { get; } = isNative;
}

public class ReturnNode(ASTNode? value, int line, int col) : ASTNode(line, col) { public ASTNode? Value { get; } = value; }
public class IfNode(ASTNode condition, List<ASTNode> thenBody, List<ASTNode>? elseBody, int line, int col) : ASTNode(line, col)
{ public ASTNode Condition { get; } = condition; public List<ASTNode> ThenBody { get; } = thenBody; public List<ASTNode>? ElseBody { get; } = elseBody; }
public class WhileNode(ASTNode condition, List<ASTNode> body, int line, int col) : ASTNode(line, col)
{ public ASTNode Condition { get; } = condition; public List<ASTNode> Body { get; } = body; }
public class ForNode(string var, ASTNode from, ASTNode to, List<ASTNode> body, int line, int col) : ASTNode(line, col)
{ public string Var { get; } = var; public ASTNode From { get; } = from; public ASTNode To { get; } = to; public List<ASTNode> Body { get; } = body; }

// Expressions
public class TernaryNode(ASTNode condition, ASTNode trueExpr, ASTNode falseExpr, int line, int col) : ASTNode(line, col)
{ public ASTNode Condition { get; } = condition; public ASTNode TrueExpr { get; } = trueExpr; public ASTNode FalseExpr { get; } = falseExpr; }
public class LiteralNode(object? value, int line, int col) : ASTNode(line, col) { public object? Value { get; } = value; }
public class VarNode(string name, int line, int col) : ASTNode(line, col) { public string Name { get; } = name; }
public class AssignNode(string name, ASTNode value, int line, int col) : ASTNode(line, col) { public string Name { get; } = name; public ASTNode Value { get; } = value; }
public class OpAssignNode(string name, string op, ASTNode value, int line, int col) : ASTNode(line, col) { public string Name { get; } = name; public string Op { get; } = op; public ASTNode Value { get; } = value; }
public class BinaryNode(ASTNode left, string op, ASTNode right, int line, int col) : ASTNode(line, col)
{ public ASTNode Left { get; } = left; public string Op { get; } = op; public ASTNode Right { get; } = right; }
public class UnaryNode(string op, ASTNode operand, int line, int col) : ASTNode(line, col) { public string Op { get; } = op; public ASTNode Operand { get; } = operand; }
public class CallNode(ASTNode? receiver, string method, List<ASTNode> arguments, int line, int col) : ASTNode(line, col)
{ public ASTNode? Receiver { get; } = receiver; public string Method { get; } = method; public List<ASTNode> Arguments { get; } = arguments; }
public class ArrayNode(List<ASTNode> elements, int line, int col) : ASTNode(line, col) { public List<ASTNode> Elements { get; } = elements; }

// ── 数组下标 ──────────────────────────────────────────────────────────────────
// 此前 Ruby 前端**没有下标表达式**：`a[i]` 只解析成裸 `a`（下标 token 被语句层跳过），
// `a[i] = v` 的左值被 ParseAssignment 丢成 `_`。下面三个节点把下标补上。
public class IndexNode(ASTNode target, ASTNode index, int line, int col) : ASTNode(line, col)
{ public ASTNode Target { get; } = target; public ASTNode Index { get; } = index; }
public class IndexAssignNode(ASTNode target, ASTNode index, ASTNode value, int line, int col) : ASTNode(line, col)
{ public ASTNode Target { get; } = target; public ASTNode Index { get; } = index; public ASTNode Value { get; } = value; }
public class IndexOpAssignNode(ASTNode target, ASTNode index, string op, ASTNode value, int line, int col) : ASTNode(line, col)
{ public ASTNode Target { get; } = target; public ASTNode Index { get; } = index; public string Op { get; } = op; public ASTNode Value { get; } = value; }
public class StringInterpolateNode(List<ASTNode> parts, int line, int col) : ASTNode(line, col) { public List<ASTNode> Parts { get; } = parts; }
public class WhenClauseNode(List<ASTNode> values, List<ASTNode> body, int line, int col) : ASTNode(line, col)
{ public List<ASTNode> Values { get; } = values; public List<ASTNode> Body { get; } = body; }
public class CaseNode(ASTNode condition, List<WhenClauseNode> whenClauses, List<ASTNode>? elseBody, int line, int col) : ASTNode(line, col)
{ public ASTNode Condition { get; } = condition; public List<WhenClauseNode> WhenClauses { get; } = whenClauses; public List<ASTNode>? ElseBody { get; } = elseBody; }
public class ModuleNode(string name, List<ASTNode> body, int line, int col) : ASTNode(line, col)
{ public string Name { get; } = name; public List<ASTNode> Body { get; } = body; }
public class IncludeNode(string moduleName, int line, int col) : ASTNode(line, col)
{ public string ModuleName { get; } = moduleName; }
public class BeginRescueNode(List<ASTNode> body, List<RescueClauseNode> rescueClauses, List<ASTNode>? ensureBody, List<ASTNode>? elseBody, int line, int col) : ASTNode(line, col)
{
    public List<ASTNode> Body { get; } = body;
    public List<RescueClauseNode> RescueClauses { get; } = rescueClauses;
    public List<ASTNode>? EnsureBody { get; } = ensureBody;
    public List<ASTNode>? ElseBody { get; } = elseBody;
}
public class RescueClauseNode(string? exceptionClass, string? variable, List<ASTNode> body, int line, int col) : ASTNode(line, col)
{
    public string? ExceptionClass { get; } = exceptionClass;
    public string? Variable { get; } = variable;
    public List<ASTNode> Body { get; } = body;
}
public class RaiseNode(ASTNode? expression, int line, int col) : ASTNode(line, col)
{ public ASTNode? Expression { get; } = expression; }
