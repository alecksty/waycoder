using System.Collections.Generic;

namespace FortranCompiler;

public abstract class ASTNode(int line, int column)
{
    public int Line { get; } = line;
    public int Column { get; } = column;
}

/// <summary>No-operation placeholder for skipped/ignored statements.</summary>
public class NopNode(int line, int col) : ASTNode(line, col) { }

/// <summary>
/// Top-level program node. Contains the program name and all body statements
/// (including contained subroutines/functions).
/// </summary>
public class ProgramNode : ASTNode
{
    public string Name { get; set; }
    public List<ASTNode> Statements { get; } = new();
    /// <summary>变量名 → 数组维度大小 (从声明如 a(10) 中提取)</summary>
    public Dictionary<string, int> ArraySizes { get; } = new();
    public ProgramNode(string name = "main") : base(0, 0) { Name = name; }
}

// ---- Declarations ----

/// <summary>
/// Variable declaration: integer :: x, y(10), z
/// </summary>
public class VarDeclNode(string typeName, List<string> names, int line, int col) : ASTNode(line, col)
{
    public string TypeName { get; } = typeName;
    public List<string> Names { get; } = names;
}

// ---- Routines ----

/// <summary>
/// Subroutine definition: subroutine name(params) ... end subroutine
/// </summary>
public class SubroutineNode(string name, List<string> parameters, List<ASTNode> body, int line, int col) : ASTNode(line, col)
{
    public string Name { get; } = name;
    public List<string> Parameters { get; } = parameters;
    public List<ASTNode> Body { get; } = body;
}

/// <summary>
/// Function definition: function name(params) result(res) ... end function
/// </summary>
public class FunctionNode(string name, string returnType, List<string> parameters, List<ASTNode> body, int line, int col) : ASTNode(line, col)
{
    /// <summary>**被调用的**名字 —— 定义体标签 = <c>func_{Name}</c>，调用点也是这个名字。</summary>
    public string Name { get; } = name;
    /// <summary>
    /// 装返回值的那个变量：有 <c>result(r)</c> 子句时是 <c>r</c>，否则与函数名同名
    /// （老式写法 <c>f = expr</c>）。**它和 <see cref="Name"/> 是两回事** ——
    /// 早先解析器把 Name 直接换成 result 变量名，于是定义体编成 `func_r:` 而调用点找
    /// `func_fib`：命中了库里的同名函数就**静默调错**（fact2.f90 的 `factorial` 实际
    /// 调的是 lib_math_factorial），没命中就运行期 KeyNotFound。
    /// </summary>
    public string ResultVar { get; set; } = name;
    public string ReturnType { get; } = returnType;
    public List<string> Parameters { get; } = parameters;
    public List<ASTNode> Body { get; } = body;
}

// ---- Statements ----

/// <summary>
/// Call statement: call subroutine(args)
/// </summary>
public class CallNode(string name, List<ASTNode> arguments, int line, int col) : ASTNode(line, col)
{
    public string Name { get; } = name;
    public List<ASTNode> Arguments { get; } = arguments;
}

/// <summary>
/// Return statement: return
/// </summary>
public class ReturnNode(int line, int col) : ASTNode(line, col) { }

/// <summary>
/// Exit statement: exit (loop break)
/// </summary>
public class ExitNode(int line, int col) : ASTNode(line, col) { }

/// <summary>
/// Cycle statement: cycle (loop continue)
/// </summary>
public class CycleNode(int line, int col) : ASTNode(line, col) { }

/// <summary>
/// Stop statement: stop [code]
/// </summary>
public class StopNode(ASTNode? code, int line, int col) : ASTNode(line, col)
{
    public ASTNode? Code { get; } = code;
}

/// <summary>
/// If statement: if (cond) then ... [else ...] end if
/// </summary>
public class IfNode(ASTNode condition, List<ASTNode> thenBody, List<ASTNode>? elseBody, int line, int col) : ASTNode(line, col)
{
    public ASTNode Condition { get; } = condition;
    public List<ASTNode> ThenBody { get; } = thenBody;
    public List<ASTNode>? ElseBody { get; } = elseBody;
}

/// <summary>
/// Do-while loop: do while (cond) ... end do
/// </summary>
public class DoWhileNode(ASTNode condition, List<ASTNode> body, int line, int col) : ASTNode(line, col)
{
    public ASTNode Condition { get; } = condition;
    public List<ASTNode> Body { get; } = body;
}

/// <summary>
/// Counted do loop: do var = start, end [, step] ... end do
/// </summary>
public class ForNode(string var, ASTNode start, ASTNode end, ASTNode? step, List<ASTNode> body, int line, int col) : ASTNode(line, col)
{
    public string Var { get; } = var;
    public ASTNode Start { get; } = start;
    public ASTNode End { get; } = end;
    public ASTNode? Step { get; } = step;
    public List<ASTNode> Body { get; } = body;
}

/// <summary>
/// Print statement: print *, expr1, expr2, ...
/// </summary>
public class PrintNode(List<ASTNode> expressions, int line, int col, bool isRead = false) : ASTNode(line, col)
{
    public List<ASTNode> Expressions { get; } = expressions;
    public bool IsRead { get; } = isRead;
}

// ---- Expressions ----

/// <summary>
/// Literal value: integer, real, string, logical
/// </summary>
public class LiteralNode(object? value, string typeName, int line, int col) : ASTNode(line, col)
{
    public object? Value { get; } = value;
    public string TypeName { get; } = typeName;  // "integer", "real", "logical", "character"
}

/// <summary>
/// Variable reference
/// </summary>
public class VarNode(string name, int line, int col) : ASTNode(line, col)
{
    public string Name { get; } = name;
}

/// <summary>
/// Assignment: var = expr  or  array(indices) = expr
/// </summary>
public class AssignNode(string name, ASTNode value, int line, int col) : ASTNode(line, col)
{
    public string Name { get; } = name;
    public ASTNode Value { get; } = value;
    public List<ASTNode>? ArrayIndices { get; set; } // for array element: a(i) = expr
    public bool IsArraySection { get; set; } // array slice: a(:) = expr
}

/// <summary>
/// Array element reference in an expression: a(i)（标量元素，1-based）
/// </summary>
public class ArrayElemNode(string name, ASTNode index, int line, int col) : ASTNode(line, col)
{
    public string Name { get; } = name;
    public ASTNode Index { get; } = index;
}

/// Array section: A(lower:upper) or A(:) for whole array
public class ArraySectionNode(string name, ASTNode? lower, ASTNode? upper, int line, int col) : ASTNode(line, col)
{
    public string Name { get; } = name;
    public ASTNode? Lower { get; } = lower; // null = default lower bound (1)
    public ASTNode? Upper { get; } = upper; // null = default upper bound (size)
}

/// Whole-array binary operation: A(:) = B(:) op C(:) — element-wise with implicit loop
public class ArrayAssignNode(string target, ASTNode? range, string op, ASTNode leftArr, ASTNode rightArr, int line, int col) : ASTNode(line, col)
{
    public string Target { get; } = target;
    public ASTNode? Range { get; } = range; // null = whole array, or ArraySectionNode
    public string Op { get; } = op;
    public ASTNode LeftArr { get; } = leftArr;
    public ASTNode RightArr { get; } = rightArr;
}

/// <summary>
/// Binary expression: left op right
/// </summary>
public class BinaryNode(ASTNode left, string op, ASTNode right, int line, int col) : ASTNode(line, col)
{
    public ASTNode Left { get; } = left;
    public string Op { get; } = op;
    public ASTNode Right { get; } = right;
}

/// <summary>
/// Unary expression: op operand (e.g., -x, .not. x)
/// </summary>
public class UnaryNode(string op, ASTNode operand, int line, int col) : ASTNode(line, col)
{
    public string Op { get; } = op;
    public ASTNode Operand { get; } = operand;
}

/// <summary>
/// Function call expression: func(args)
/// </summary>
public class FuncCallNode(string name, List<ASTNode> arguments, int line, int col) : ASTNode(line, col)
{
    public string Name { get; } = name;
    public List<ASTNode> Arguments { get; } = arguments;
}

/// <summary>MODULE name ... END MODULE — contains functions/subroutines</summary>
public class ModuleNode(string name, List<ASTNode> body, int line, int col) : ASTNode(line, col)
{
    public string Name { get; } = name;
    public List<ASTNode> Body { get; } = body;
}

/// <summary>USE module_name — import module symbols</summary>
public class UseNode(string moduleName, int line, int col) : ASTNode(line, col)
{
    public string ModuleName { get; } = moduleName;
}

/// <summary>ALLOCATE(array(size)) — dynamic array allocation</summary>
public class AllocateNode(string name, ASTNode size, int line, int col) : ASTNode(line, col)
{
    public string Name { get; } = name;
    public ASTNode Size { get; } = size;
}

/// <summary>DEALLOCATE(array) — dynamic array deallocation</summary>
public class DeallocateNode(string name, int line, int col) : ASTNode(line, col)
{
    public string Name { get; } = name;
}
