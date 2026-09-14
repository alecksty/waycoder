namespace KotlinCompiler;
public abstract class ASTNode { }
public class Program(List<ASTNode> functions) : ASTNode { public List<ASTNode> Functions => functions; }
public class FunctionDecl(string name, List<string> pars, ASTNode body, List<string>? typeParams = null, bool isExternal = false) : ASTNode {
    public string Name => name; public List<string> Parameters => pars; public ASTNode Body => body;
    public List<string> TypeParams => typeParams ?? [];
    public bool IsExternal => isExternal;
}
public class Block(List<ASTNode> stmts) : ASTNode { public List<ASTNode> Statements => stmts; }
public class CallExpr(string name, List<ASTNode> args) : ASTNode { public string Name => name; public List<ASTNode> Args => args; }
public class IntLiteral(long value) : ASTNode { public long Value => value; }
public class FloatLiteral(float value) : ASTNode { public float Value => value; }
public class DoubleLiteral(double value) : ASTNode { public double Value => value; }
public class StringLiteral(string value) : ASTNode { public string Value => value; }
public class BoolLiteral(bool value) : ASTNode { public bool Value => value; }
public class VarDecl(string name, ASTNode? init, bool isVal, string? type = null) : ASTNode {
    public string Name => name; public ASTNode? Init => init; public bool IsVal => isVal; public string? Type => type;
}
public class VarRef(string name) : ASTNode { public string Name => name; }
public class AssignStmt(string name, ASTNode value, string op = "=", ASTNode? target = null) : ASTNode { public string Name => name; public ASTNode Value => value; public string Op => op; public ASTNode? Target => target; }
public class IfStmt(ASTNode cond, ASTNode then, ASTNode? @else) : ASTNode {
    public ASTNode Condition => cond; public ASTNode Then => then; public ASTNode? Else => @else;
}
public class WhileStmt(ASTNode cond, ASTNode body) : ASTNode { public ASTNode Condition => cond; public ASTNode Body => body; }
public class DoWhileStmt(ASTNode cond, ASTNode body) : ASTNode { public ASTNode Condition => cond; public ASTNode Body => body; }
public class ForStmt(string varName, ASTNode start, ASTNode end, ASTNode body, string kind = "..", ASTNode? step = null) : ASTNode {
    public string VarName => varName; public ASTNode Start => start; public ASTNode End => end; public ASTNode Body => body;
    public string Kind { get; set; } = kind; public ASTNode? Step => step;
}
public class WhenStmt(ASTNode value, List<WhenBranch> branches, ASTNode? elseBranch) : ASTNode {
    public ASTNode Value => value; public List<WhenBranch> Branches => branches; public ASTNode? Else => elseBranch;
}
public class WhenBranch(ASTNode condition, ASTNode body) : ASTNode {
    public ASTNode Condition => condition; public ASTNode Body => body;
}
public class ReturnStmt(ASTNode? value) : ASTNode { public ASTNode? Value => value; }
public class BreakStmt : ASTNode { }
public class ContinueStmt : ASTNode { }
public class BinaryOp(string op, ASTNode left, ASTNode right) : ASTNode {
    public string Op => op; public ASTNode Left => left; public ASTNode Right => right;
}
public class ClassDecl(string name, List<(string, string)> props, List<FunctionDecl> methods, bool isData = false, List<string>? interfaces = null) : ASTNode {
    public string Name => name; public List<(string propName, string propType)> Props => props;
    public List<FunctionDecl> Methods => methods; public bool IsData { get; set; } = isData;
    public List<string> Interfaces => interfaces ?? [];
    public bool IsSealed { get; set; } = false;
}
public class ExtensionDecl(string receiverType, FunctionDecl func) : ASTNode {
    public string ReceiverType => receiverType; public FunctionDecl Func => func;
}
public class NewExpr(string className, List<ASTNode> args) : ASTNode {
    public string ClassName => className; public List<ASTNode> Args => args;
}
public class MemberAccess(ASTNode obj, string member) : ASTNode {
    public ASTNode Object => obj; public string Member => member;
}
public class LambdaExpr(List<string> parameters, ASTNode body) : ASTNode {
    public List<string> Parameters => parameters; public ASTNode Body => body;
}
public class IndexExpr(ASTNode target, ASTNode index) : ASTNode {
    public ASTNode Target => target; public ASTNode Index => index;
}
public class UnaryOp(string op, ASTNode operand) : ASTNode { public string Op => op; public ASTNode Operand => operand; }
public class SafeCallExpr(ASTNode obj, string member) : ASTNode { public ASTNode Object => obj; public string Member => member; }
public class ElvisExpr(ASTNode left, ASTNode right) : ASTNode { public ASTNode Left => left; public ASTNode Right => right; }
public class InterfaceDecl(string name, List<FunctionDecl> methods) : ASTNode {
    public string Name => name; public List<FunctionDecl> Methods => methods;
}
public class NotNullAssert(ASTNode expr) : ASTNode { public ASTNode Expr => expr; }
public class TryStmt(ASTNode body, List<CatchClause> catches, ASTNode? finallyBlock) : ASTNode {
    public ASTNode Body => body; public List<CatchClause> Catches => catches; public ASTNode? FinallyBlock => finallyBlock;
}
public class CatchClause(string? varName, string? excType, ASTNode body) : ASTNode {
    public string? VarName => varName; public string? ExcType => excType; public ASTNode Body => body;
}
public class ThrowStmt(ASTNode? expr) : ASTNode { public ASTNode? Expression => expr; }
