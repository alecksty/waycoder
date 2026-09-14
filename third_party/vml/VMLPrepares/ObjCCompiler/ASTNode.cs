using System.Collections.Generic;

namespace ObjCCompiler;

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

// C declarations
public class FuncDeclNode(string name, string returnType, List<(string type, string name)> parameters, List<ASTNode> body, int l, int c, bool isNative = false) : ASTNode(l, c)
{
    public string Name { get; } = name;
    public string ReturnType { get; } = returnType;
    public List<(string type, string name)> Parameters { get; } = parameters;
    public List<ASTNode> Body { get; } = body;
    public bool IsNative { get; } = isNative;
}

public class VarDeclNode(string type, string name, ASTNode? init, int l, int c, int arraySize = 0) : ASTNode(l, c)
{
    public string Type { get; } = type;
    public string Name { get; } = name;
    public ASTNode? Init { get; } = init;
    public int ArraySize { get; } = arraySize;
}

// ObjC declarations
public class ObjCInterfaceNode(string name, string superClass, List<ASTNode> members, int l, int c, string? categoryName = null) : ASTNode(l, c)
{
    public string Name { get; } = name;
    public string SuperClass { get; } = superClass;
    public List<ASTNode> Members { get; } = members;
    public string? CategoryName { get; } = categoryName;
}

public class ObjCImplNode(string name, List<ASTNode> methods, int l, int c, string? categoryName = null) : ASTNode(l, c)
{
    public string Name { get; } = name;
    public List<ASTNode> Methods { get; } = methods;
    public string? CategoryName { get; } = categoryName;
}

public class ObjCMethodNode(string name, string returnType, List<(string type, string name)> parameters, List<ASTNode> body, int l, int c, bool isNative = false) : ASTNode(l, c)
{
    public string Name { get; } = name;
    public string ReturnType { get; } = returnType;
    public List<(string type, string name)> Parameters { get; } = parameters;
    public List<ASTNode> Body { get; } = body;
    public bool IsNative { get; } = isNative;
}

// Statements
public class ReturnNode(ASTNode? value, int l, int c) : ASTNode(l, c) { public ASTNode? Value { get; } = value; }
// 内联汇编语句: asm("NOP");
public class AsmStatement(string code, int l, int c) : ASTNode(l, c) { public string Code { get; } = code; }
public class BreakNode(int l, int c) : ASTNode(l, c) { }
public class ContinueNode(int l, int c) : ASTNode(l, c) { }
public class GotoNode(string label, int l, int c) : ASTNode(l, c) { public string Label { get; } = label; }
public class LabelNode(string name, int l, int c) : ASTNode(l, c) { public string Name { get; } = name; }
public class IfNode(ASTNode condition, List<ASTNode> thenBody, List<ASTNode>? elseBody, int l, int c) : ASTNode(l, c)
{ public ASTNode Condition { get; } = condition; public List<ASTNode> ThenBody { get; } = thenBody; public List<ASTNode>? ElseBody { get; } = elseBody; }
public class WhileNode(ASTNode condition, List<ASTNode> body, int l, int c) : ASTNode(l, c)
{ public ASTNode Condition { get; } = condition; public List<ASTNode> Body { get; } = body; }
public class DoWhileNode(ASTNode condition, List<ASTNode> body, int l, int c) : ASTNode(l, c)
{ public ASTNode Condition { get; } = condition; public List<ASTNode> Body { get; } = body; }
public class ForNode(ASTNode? init, ASTNode? condition, ASTNode? update, List<ASTNode> body, int l, int c) : ASTNode(l, c)
{ public ASTNode? Init { get; } = init; public ASTNode? Condition { get; } = condition; public ASTNode? Update { get; } = update; public List<ASTNode> Body { get; } = body; }
public class SwitchNode(ASTNode expr, List<(ASTNode? caseVal, List<ASTNode> body)> cases, int l, int c) : ASTNode(l, c)
{ public ASTNode Expr { get; } = expr; public List<(ASTNode? caseVal, List<ASTNode> body)> Cases { get; } = cases; }

// Expressions
public class LiteralNode(object? value, int l, int c) : ASTNode(l, c) { public object? Value { get; } = value; }
public class VarNode(string name, int l, int c) : ASTNode(l, c) { public string Name { get; } = name; }
public class AssignNode(string name, ASTNode value, int l, int c) : ASTNode(l, c) { public string Name { get; } = name; public ASTNode Value { get; } = value; }
public class BinaryNode(ASTNode left, string op, ASTNode right, int l, int c) : ASTNode(l, c)
{ public ASTNode Left { get; } = left; public string Op { get; } = op; public ASTNode Right { get; } = right; }
public class UnaryNode(string op, ASTNode operand, int l, int c) : ASTNode(l, c) { public string Op { get; } = op; public ASTNode Operand { get; } = operand; }
public class CallNode(string name, List<ASTNode> arguments, int l, int c) : ASTNode(l, c)
{ public string Name { get; } = name; public List<ASTNode> Arguments { get; } = arguments; }

// ObjC @property declaration
public class ObjCPropertyNode(string type, string name, int l, int c) : ASTNode(l, c)
{ public string Type { get; } = type; public string Name { get; } = name;
  public List<string> Attributes { get; } = new(); } // nonatomic, strong, weak, etc.

// ObjC @synthesize declaration
public class ObjCSynthesizeNode(string propertyName, string ivarName, int l, int c) : ASTNode(l, c)
{ public string PropertyName { get; } = propertyName; public string IvarName { get; } = ivarName; }

// ObjC @dynamic declaration
public class ObjCDynamicNode(string propertyName, int l, int c) : ASTNode(l, c)
{ public string PropertyName { get; } = propertyName; }

// ObjC message expression: [receiver method:arg1 param2:arg2 ...]
public class MsgSendNode(ASTNode receiver, string method, List<ASTNode> arguments, int l, int c) : ASTNode(l, c)
{ public ASTNode Receiver { get; } = receiver; public string Method { get; } = method; public List<ASTNode> Arguments { get; } = arguments; }

// ObjC @try/@catch/@finally
public class ObjCTryNode(List<ASTNode> tryBody, List<(string? excType, string? excName, List<ASTNode> body)> catches, List<ASTNode>? finallyBody, int l, int c) : ASTNode(l, c)
{ public List<ASTNode> TryBody { get; } = tryBody; public List<(string? excType, string? excName, List<ASTNode> body)> Catches { get; } = catches; public List<ASTNode>? FinallyBody { get; } = finallyBody; }

// ObjC @throw
public class ObjCThrowNode(ASTNode? expr, int l, int c) : ASTNode(l, c)
{ public ASTNode? Expr { get; } = expr; }

// ObjC boxed expression: @(expr) → equivalent to (id)expr
public class ObjCBoxedNode(ASTNode expr, int l, int c) : ASTNode(l, c)
{ public ASTNode Expr { get; } = expr; }

// ObjC fast enumeration: for (id x in collection) { ... }
public class ObjCForEachNode(string varType, string varName, ASTNode collection, List<ASTNode> body, int l, int c) : ASTNode(l, c)
{ public string VarType { get; } = varType; public string VarName { get; } = varName; public ASTNode Collection { get; } = collection; public List<ASTNode> Body { get; } = body; }

// ObjC @synchronized(expr) { body }
public class ObjCSynchronizedNode(List<ASTNode> body, int l, int c) : ASTNode(l, c)
{ public List<ASTNode> Body { get; } = body; }

// ObjC @autoreleasepool { body }
public class ObjCAutoreleasepoolNode(List<ASTNode> body, int l, int c) : ASTNode(l, c)
{ public List<ASTNode> Body { get; } = body; }

// ObjC protocol type check: [obj conformsToProtocol:@protocol(MyProto)]
// Forward declaration: @protocol MyProto; @end
public class ObjCProtocolNode(string name, List<ASTNode> members, int l, int c) : ASTNode(l, c)
{ public string Name { get; } = name; public List<ASTNode> Members { get; } = members; }

// BlockNode — used for multi-var declarations and compound statements
public class BlockNode(List<ASTNode> statements, int l, int c) : ASTNode(l, c)
{ public List<ASTNode> Statements { get; } = statements; }

// C-style cast: (int)expr
public class CastNode(string targetType, ASTNode expr, int l, int c) : ASTNode(l, c)
{ public string TargetType { get; } = targetType; public ASTNode Expr { get; } = expr; }

// ObjC block literal: ^int(int x) { return x*2; } or ^{ ... }
public class ObjCBlockNode(string returnType, List<(string type, string name)> parameters, List<ASTNode> body, int l, int c) : ASTNode(l, c)
{
    public string ReturnType { get; } = returnType;
    public List<(string type, string name)> Parameters { get; } = parameters;
    public List<ASTNode> Body { get; } = body;
}

// Struct member access: expr.field or expr->field
public class MemberAccessNode(ASTNode obj, string memberName, bool isPointer, int l, int c) : ASTNode(l, c)
{
    public ASTNode Object { get; } = obj;
    public string MemberName { get; } = memberName;
    public bool IsPointer { get; } = isPointer;
}

// Struct field definition for layout tracking
public class StructFieldDef(string name, string type, int offset, int size)
{
    public string Name { get; } = name;
    public string Type { get; } = type;
    public int Offset { get; } = offset;
    public int Size { get; } = size;
}

// Struct definition with field layout
public class StructDef(string name, List<StructFieldDef> fields)
{
    public string Name { get; } = name;
    public List<StructFieldDef> Fields { get; } = fields;
}
