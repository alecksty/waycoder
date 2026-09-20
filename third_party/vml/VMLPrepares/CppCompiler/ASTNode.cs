namespace CppCompiler
{
    public abstract class ASTNode {
        /// <summary>
        /// **预处理之后**的源码行号（1-based）；**0 = 未知**。
        /// 产物里 `; N: &lt;原文&gt;` 注释按它索引 `SourceLines`（= 预处理后的文本）。
        /// </summary>
        public int Line { get; set; }
        /// <summary>**原文件**行号（1-based）；**0 = 未知**（无预处理时不设，退回 Line）。</summary>
        public int OriginalLine { get; set; }
        /// <summary>
        /// **原文件**路径；<c>null</c> = 未知（没有行号映射时）。
        /// 与 <see cref="OriginalLine"/> 配对 —— 报错时要能说出「这是**哪个文件**的第几行」，
        /// 只说行号会让头文件里的错被当成用户自己文件的错（见 `Parser.Where`）。
        /// </summary>
        public string? OriginalFile { get; set; }
        /// <summary>源码列号（1-based）；**0 = 未知**。</summary>
        public int Column { get; set; }
    }

    public enum CallingConvention
    {
        Cdecl,
        Stdcall,
        Fastcall,
    }

    // Program root
    public class Program : ASTNode
    {
        public List<ASTNode> Declarations { get; } = new();
        public string? NamespaceName { get; set; }
    }

    // Declarations
    public class InitEntry
    {
        public string MemberName { get; set; } = "";
        public Expr? Value { get; set; }
    }

    public class FunctionDecl : ASTNode
    {
        public string Name { get; set; } = "";
        public string ReturnType { get; set; } = "int";
        public List<Parameter> Parameters { get; } = new();
        public BlockStmt? Body { get; set; }
        public bool IsMember { get; set; }
        public string? ClassName { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsOverride { get; set; }
        public bool IsConst { get; set; }
        public List<InitEntry> InitList { get; } = new();  // : member1(val1), member2(val2)
        public CallingConvention Convention { get; set; } = CallingConvention.Cdecl;
    }

    public class Parameter
    {
        public string Type { get; set; } = "int";
        public string Name { get; set; } = "";
        public bool IsReference { get; set; }
    }

    public class MultiVarDecl : ASTNode
    {
        public List<VariableDecl> Variables { get; set; } = new();
    }

    public class VariableDecl : ASTNode
    {
        public string Type { get; set; } = "int";
        public string Name { get; set; } = "";
        public Expr? Initializer { get; set; }
        public Expr? ArraySize { get; set; }
        public bool IsReference { get; set; }
        public bool IsArray { get; set; }
        public List<Expr?> Dimensions { get; set; } = new();
    }

    public class ClassDecl : ASTNode
    {
        public string Name { get; set; } = "";
        public List<ClassMember> Members { get; } = new();
        public string? BaseClass { get; set; }
        public AccessSpec CurrentAccess { get; set; } = AccessSpec.Private;
    }

    public class TemplateClassDecl : ASTNode
    {
        public string Name { get; set; } = "";
        public List<string> TypeParams { get; } = new();
        public List<ClassMember> Members { get; } = new();
        public string? BaseClass { get; set; }
    }

    public class TemplateFunctionDecl : ASTNode
    {
        public string Name { get; set; } = "";
        public List<string> TypeParams { get; } = new();
        public string ReturnType { get; set; } = "int";
        public List<Parameter> Parameters { get; } = new();
        public BlockStmt? Body { get; set; }
        public CallingConvention Convention { get; set; } = CallingConvention.Cdecl;
    }

    public enum AccessSpec { Private, Protected, Public }

    public class ClassMember
    {
        public AccessSpec Access { get; set; }
        public string Type { get; set; } = "int";
        public string Name { get; set; } = "";
        public Expr? Initializer { get; set; }
        public bool IsMethod { get; set; }
        public FunctionDecl? Method { get; set; }
        public bool IsConstructor { get; set; }
        public bool IsDestructor { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsStatic { get; set; }
    }

    public class NamespaceDecl : ASTNode
    {
        public string Name { get; set; } = "";
        public List<ASTNode> Members { get; } = new();
    }

    public class UsingDecl : ASTNode
    {
        public string NamespaceName { get; set; } = "";
    }

    public class ExternBlock : ASTNode
    {
        public string Linkage { get; set; } = "C";
        public List<ASTNode> Members { get; } = new();
    }

    public class EnumDecl : ASTNode
    {
        public string Name { get; set; } = "";
        public List<EnumMember> Members { get; } = new();
    }

    public class EnumMember
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    public class UnionDecl : ASTNode
    {
        public string Name { get; set; } = "";
        public List<ClassMember> Members { get; } = new();
    }

    // Statements
    public abstract class Stmt : ASTNode { }

    public class BlockStmt : Stmt
    {
        public List<Stmt> Statements { get; } = new();
    }

    public class ExprStmt : Stmt
    {
        public Expr? Expression { get; set; }
    }

    public class IfStmt : Stmt
    {
        public Expr Condition { get; set; } = null!;
        public Stmt ThenBranch { get; set; } = null!;
        public Stmt? ElseBranch { get; set; }
    }

    public class WhileStmt : Stmt
    {
        public Expr Condition { get; set; } = null!;
        public Stmt Body { get; set; } = null!;
    }

    public class ForStmt : Stmt
    {
        public Stmt? Initializer { get; set; }
        public Expr? Condition { get; set; }
        public Expr? Increment { get; set; }
        public Stmt Body { get; set; } = null!;
    }

    public class ReturnStmt : Stmt
    {
        public Expr? Value { get; set; }
    }

    public class BreakStmt : Stmt { }
    public class ContinueStmt : Stmt { }
    public class LabelStmt : Stmt { public string Name { get; set; } = ""; }
    public class GotoStmt : Stmt { public string Target { get; set; } = ""; }

    public class SwitchStmt : Stmt
    {
        public Expr Value { get; set; } = null!;
        public List<SwitchCase> Cases { get; } = new();
    }

    public class TryStmt : Stmt
    {
        public BlockStmt Body { get; set; } = new();
        public List<CatchClause> Catches { get; } = new();
    }

    public class CatchClause
    {
        public string? ExceptionType { get; set; }
        public string? VariableName { get; set; }
        public BlockStmt Body { get; set; } = new();
    }

    public class ThrowStmt : Stmt
    {
        public Expr? Expression { get; set; }
    }

    public class AsmStmt : Stmt
    {
        public string Code { get; set; } = "";
    }

    public class SwitchCase
    {
        public Expr? Value { get; set; }
        public List<Stmt> Body { get; } = new();
    }

    // Expressions
    public abstract class Expr : ASTNode { }

    public class IntLiteral : Expr { public int Value { get; set; } }
    public class LongLiteral : Expr { public long Value { get; set; } }
    public class FloatLiteral : Expr { public double Value { get; set; } public bool IsFloatSuffix { get; set; } } // f/F 后缀=true(float), 无后缀=false(double)
    public class StringLiteral : Expr { public string Value { get; set; } = ""; public int Width { get; set; } = 8; } /* 8=char, 16=wchar_t, 32=char32_t */
    public class BoolLiteral : Expr { public bool Value { get; set; } }
    public class CharLiteral : Expr { public char Value { get; set; } }
    public class NullPtrLiteral : Expr { }

    public class IdentExpr : Expr { public string Name { get; set; } = ""; }

    public class BinaryExpr : Expr
    {
        public Expr Left { get; set; } = null!;
        public string Op { get; set; } = "";
        public Expr Right { get; set; } = null!;
    }

    public class UnaryExpr : Expr
    {
        public string Op { get; set; } = "";
        public Expr Operand { get; set; } = null!;
    }

    public class CallExpr : Expr
    {
        public Expr Callee { get; set; } = null!;
        public List<Expr> Arguments { get; } = new();
    }

    public class MemberExpr : Expr
    {
        public Expr Object { get; set; } = null!;
        public string Member { get; set; } = "";
        public bool Arrow { get; set; }
    }

    public class AssignExpr : Expr
    {
        public Expr Target { get; set; } = null!;
        public string Op { get; set; } = "=";
        public Expr Value { get; set; } = null!;
        public string? DeclType { get; set; } // 变量声明时的类型 (struct Pt / int / ...)
        public int ArraySize { get; set; } // 数组元素个数 (0 = 非数组)
        public List<int> Dimensions { get; set; } = new(); // 多维数组原始维度 [2,3] for arr[2][3]
    }

    public class NewExpr : Expr
    {
        public string Type { get; set; } = "int";
        public Expr? Size { get; set; }
        public List<Expr> Init { get; set; } = new();
    }

    public class DeleteExpr : Expr
    {
        public Expr Target { get; set; } = null!;
        public bool IsArray { get; set; }
    }

    public class ThisExpr : Expr { }

    public class LambdaExpr : Expr
    {
        public List<string> Parameters { get; } = new();
        public BlockStmt Body { get; set; } = new();
        public bool HasCapture { get; set; }
    }

    public class SizeofExpr : Expr
    {
        public Expr? Expression { get; set; }
        public string? TypeName { get; set; }
    }

    public class ConditionalExpr : Expr
    {
        public Expr Condition { get; set; } = null!;
        public Expr TrueExpr { get; set; } = null!;
        public Expr FalseExpr { get; set; } = null!;
    }

    public class CastExpr : Expr
    {
        public string TargetType { get; set; } = "int";
        public Expr Expression { get; set; } = null!;
    }

    public class TypeIdExpr : Expr
    {
        public Expr Expression { get; set; } = null!;
    }

    public class InitializerListExpr : Expr
    {
        public List<Expr> Elements { get; } = new();
    }
}
