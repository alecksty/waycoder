#nullable enable
using System.Collections.Generic;

namespace JavaCompiler
{
    /// <summary>
    /// AST 基类
    /// </summary>
    public abstract class ASTNode {
        /// <summary>
        /// **预处理之后**的源码行号（1-based）；**0 = 未知**。
        /// 产物里 `; N: &lt;原文&gt;` 注释按它索引 `SourceLines`（= 预处理后的文本）。
        /// </summary>
        public int Line { get; set; }
        /// <summary>**原文件**行号（1-based）；**0 = 未知**（无预处理时不设，退回 Line）。</summary>
        public int OriginalLine { get; set; }
        /// <summary>源码列号（1-based）；**0 = 未知**。</summary>
        public int Column { get; set; }
    }

    /// <summary>
    /// Java程序
    /// </summary>
    public class Program : ASTNode
    {
        public List<ClassDecl> Classes { get; set; }
        public List<ImportDecl> Imports { get; set; }
        public List<EnumDeclStatement> EnumDecls { get; set; } = new();
        public PackageDecl? Package { get; set; }

        public Program()
        {
            Classes = new List<ClassDecl>();
            Imports = new List<ImportDecl>();
        }
    }

    /// <summary>
    /// 包声明
    /// </summary>
    public class PackageDecl : ASTNode
    {
        public string Name { get; set; }
        
        public PackageDecl(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// 导入声明
    /// </summary>
    public class ImportDecl : ASTNode
    {
        public string Name { get; set; }
        public bool IsStatic { get; set; }
        public bool IsWildcard { get; set; }
        
        public ImportDecl(string name, bool isStatic = false, bool isWildcard = false)
        {
            Name = name;
            IsStatic = isStatic;
            IsWildcard = isWildcard;
        }
    }

    /// <summary>
    /// 类声明
    /// </summary>
    public class ClassDecl : ASTNode
    {
        public string Name { get; set; }
        public string? SuperClass { get; set; }
        public List<string> Interfaces { get; set; }
        public List<FieldDecl> Fields { get; set; }
        public List<MethodDecl> Methods { get; set; }
        public List<ConstructorDecl> Constructors { get; set; }
        public List<ClassDecl> Classes { get; set; } // 嵌套类
        public List<EnumDeclStatement> EnumDecls { get; set; } = new();
        public Modifiers Modifiers { get; set; }
        
        public ClassDecl(string name)
        {
            Name = name;
            SuperClass = null;
            Interfaces = new List<string>();
            Fields = new List<FieldDecl>();
            Methods = new List<MethodDecl>();
            Constructors = new List<ConstructorDecl>();
            Classes = new List<ClassDecl>();
            Modifiers = new Modifiers();
        }
    }

    /// <summary>
    /// 修饰符
    /// </summary>
    public class Modifiers
    {
        public bool IsPublic { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsProtected { get; set; }
        public bool IsStatic { get; set; }
        public bool IsFinal { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsSynchronized { get; set; }
        public bool IsVolatile { get; set; }
        public bool IsTransient { get; set; }
        public bool IsNative { get; set; }
        public bool IsDefault { get; set; }
    }

    /// <summary>
    /// 字段声明
    /// </summary>
    public class FieldDecl : ASTNode
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Expression? Initializer { get; set; }
        public Modifiers Modifiers { get; set; } = new();
        
        public FieldDecl(string type, string name, Expression? initializer = null)
        {
            Type = type;
            Name = name;
            Initializer = initializer;
            Modifiers = new Modifiers();
        }
    }

    /// <summary>
    /// 方法声明
    /// </summary>
    public class MethodDecl : ASTNode
    {
        public string ReturnType { get; set; }
        public string Name { get; set; }
        public List<Parameter> Parameters { get; set; }
        public Block Body { get; set; }
        public Modifiers Modifiers { get; set; }
        public List<string> Throws { get; set; }
        
        public MethodDecl(string returnType, string name)
        {
            ReturnType = returnType;
            Name = name;
            Parameters = new List<Parameter>();
            Body = new Block();
            Modifiers = new Modifiers();
            Throws = new List<string>();
        }
    }

    /// <summary>
    /// 构造方法声明
    /// </summary>
    public class ConstructorDecl : ASTNode
    {
        public string Name { get; set; }
        public List<Parameter> Parameters { get; set; }
        public Block Body { get; set; }
        public Modifiers Modifiers { get; set; }
        public SuperCall? SuperCall { get; set; }
        
        public ConstructorDecl(string name)
        {
            Name = name;
            Parameters = new List<Parameter>();
            Body = new Block();
            Modifiers = new Modifiers();
        }
    }

    public class SuperCall
    {
        public List<Expression> Arguments { get; set; } = new();
    }

    public class ArrayInitializerExpression : Expression
    {
        public List<Expression> Elements { get; set; } = new();
    }

    /// <summary>
    /// 参数
    /// </summary>
    public class Parameter : ASTNode
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public bool IsVararg { get; set; }
        
        public Parameter(string type, string name)
        {
            Type = type;
            Name = name;
        }
    }

    /// <summary>
    /// 代码块
    /// </summary>
    public class Block : ASTNode
    {
        public List<Statement> Statements { get; set; }
        
        public Block()
        {
            Statements = new List<Statement>();
        }
    }

    /// <summary>
    /// 语句基类
    /// </summary>
    public abstract class Statement : ASTNode
    {}

    /// <summary>
    /// 表达式语句
    /// </summary>
    public class ExpressionStatement : Statement
    {
        public Expression Expression { get; set; }
        
        public ExpressionStatement(Expression expression)
        {
            Expression = expression;
        }
    }

    /// <summary>
    /// 变量声明语句
    /// </summary>
    public class VariableDeclStatement : Statement
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public Expression? Initializer { get; set; }
        
        public VariableDeclStatement(string type, string name, Expression? initializer = null)
        {
            Type = type;
            Name = name;
            Initializer = initializer;
        }
    }

    /// <summary>
    /// 返回语句
    /// </summary>
    public class ReturnStatement : Statement
    {
        public Expression? Value { get; set; }
        
        public ReturnStatement(Expression? value = null)
        {
            Value = value;
        }
    }

    /// <summary>
    /// If语句
    /// </summary>
    public class IfStatement : Statement
    {
        public Expression Condition { get; set; }
        public Statement ThenBranch { get; set; }
        public Statement? ElseBranch { get; set; }
        
        public IfStatement(Expression condition, Statement thenBranch, Statement? elseBranch = null)
        {
            Condition = condition;
            ThenBranch = thenBranch;
            ElseBranch = elseBranch;
        }
    }

    /// <summary>
    /// While语句
    /// </summary>
    public class WhileStatement : Statement
    {
        public Expression Condition { get; set; }
        public Statement Body { get; set; }
        
        public WhileStatement(Expression condition, Statement body)
        {
            Condition = condition;
            Body = body;
        }
    }

    /// <summary>
    /// For语句
    /// </summary>
    public class ForStatement : Statement
    {
        public Statement? Initializer { get; set; }
        public Expression? Condition { get; set; }
        public Expression? Increment { get; set; }
        public Statement Body { get; set; }
        
        public ForStatement(Statement? initializer, Expression? condition, Expression? increment, Statement body)
        {
            Initializer = initializer;
            Condition = condition;
            Increment = increment;
            Body = body;
        }
    }

    /// <summary>
    /// 块语句
    /// </summary>
    public class BlockStatement : Statement
    {
        public Block Block { get; set; }
        
        public BlockStatement(Block block)
        {
            Block = block;
        }
    }

    /// <summary>
    /// 表达式基类
    /// </summary>
    public abstract class Expression : ASTNode
    {}

    /// <summary>
    /// 字面量表达式
    /// </summary>
    public class LiteralExpression : Expression
    {
        public object? Value { get; set; }
        public string Type { get; set; }
        
        public LiteralExpression(object? value, string type = "int")
        {
            Value = value;
            Type = type;
        }
    }

    /// <summary>
    /// 变量表达式
    /// </summary>
    public class VariableExpression : Expression
    {
        public string Name { get; set; }
        
        public VariableExpression(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// 二元运算表达式
    /// </summary>
    public class BinaryExpression : Expression
    {
        public Expression Left { get; set; }
        public TokenType Operator { get; set; }
        public Expression Right { get; set; }
        
        public BinaryExpression(Expression left, TokenType op, Expression right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }
    }

    /// <summary>
    /// 一元运算表达式
    /// </summary>
    public class UnaryExpression : Expression
    {
        public TokenType Operator { get; set; }
        public Expression Operand { get; set; }
        public bool IsPostfix { get; set; }
        
        public UnaryExpression(TokenType op, Expression operand, bool isPostfix = false)
        {
            Operator = op;
            Operand = operand;
            IsPostfix = isPostfix;
        }
    }

    /// <summary>
    /// 方法调用表达式
    /// </summary>
    public class MethodCallExpression : Expression
    {
        public Expression Target { get; set; }
        public string MethodName { get; set; }
        public List<Expression> Arguments { get; set; }
        
        public MethodCallExpression(Expression target, string methodName)
        {
            Target = target;
            MethodName = methodName;
            Arguments = new List<Expression>();
        }
    }

    /// <summary>
    /// 字段访问表达式
    /// </summary>
    public class FieldAccessExpression : Expression
    {
        public Expression Target { get; set; }
        public string FieldName { get; set; }
        
        public FieldAccessExpression(Expression target, string fieldName)
        {
            Target = target;
            FieldName = fieldName;
        }
    }

    /// <summary>
    /// 赋值表达式
    /// </summary>
    public class AssignmentExpression : Expression
    {
        public Expression Left { get; set; }
        public TokenType Operator { get; set; }
        public Expression Right { get; set; }
        
        public AssignmentExpression(Expression left, TokenType op, Expression right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }
    }

    /// <summary>
    /// 新建对象表达式
    /// </summary>
    public class NewExpression : Expression
    {
        public string Type { get; set; }
        public List<Expression> Arguments { get; set; }
        
        public NewExpression(string type)
        {
            Type = type;
            Arguments = new List<Expression>();
        }
    }

    /// <summary>
    /// 数组访问表达式
    /// </summary>
    public class ArrayAccessExpression : Expression
    {
        public Expression Array { get; set; }
        public Expression Index { get; set; }
        
        public ArrayAccessExpression(Expression array, Expression index)
        {
            Array = array;
            Index = index;
        }
    }

    /// <summary>
    /// 类型转换表达式
    /// </summary>
    public class CastExpression : Expression
    {
        public string TargetType { get; set; }
        public Expression Expression { get; set; }
        
        public CastExpression(string targetType, Expression expression)
        {
            TargetType = targetType;
            Expression = expression;
        }
    }

    /// <summary>
    /// 实例检查表达式
    /// </summary>
    public class InstanceofExpression : Expression
    {
        public Expression Expression { get; set; }
        public string Type { get; set; }
        
        public InstanceofExpression(Expression expression, string type)
        {
            Expression = expression;
            Type = type;
        }
    }

    /// <summary>
    /// 括号表达式
    /// </summary>
    public class ParenthesizedExpression : Expression
    {
        public Expression Expression { get; set; }
        
        public ParenthesizedExpression(Expression expression)
        {
            Expression = expression;
        }
    }

    public class ConditionalExpression : Expression
    {
        public Expression Condition { get; set; }
        public Expression TrueValue { get; set; }
        public Expression FalseValue { get; set; }
        public ConditionalExpression(Expression cond, Expression tv, Expression fv)
        { Condition = cond; TrueValue = tv; FalseValue = fv; }
    }

    public class DoWhileStatement : Statement
    {
        public Statement Body { get; set; }
        public Expression Condition { get; set; }
        public DoWhileStatement(Statement body, Expression cond) { Body = body; Condition = cond; }
    }

    public class SwitchStatement : Statement
    {
        public Expression Value { get; set; }
        public List<SwitchCase> Cases { get; set; } = new();
        public SwitchStatement(Expression value) { Value = value; }
    }

    public class SwitchCase
    {
        public Expression? Value { get; set; }
        public List<Statement> Body { get; set; } = new();
        public bool HasBreak { get; set; }
    }

    public class BreakStatement : Statement
    {
        public string? Label { get; set; }
        public BreakStatement(string? label = null) { Label = label; }
    }
    public class ContinueStatement : Statement
    {
        public string? Label { get; set; }
        public ContinueStatement(string? label = null) { Label = label; }
    }
    public class LabeledStatement : Statement
    {
        public string Label { get; set; }
        public Statement Statement { get; set; }
        public LabeledStatement(string label, Statement statement)
        {
            Label = label;
            Statement = statement;
        }
    }

    public class AssertStatement : Statement
    {
        public Expression Condition { get; set; }
        public Expression? Message { get; set; }
        public AssertStatement(Expression condition, Expression? message = null)
        {
            Condition = condition;
            Message = message;
        }
    }

    public class ThrowStatement : Statement
    {
        public Expression? Value { get; set; }
        public ThrowStatement(Expression? value = null) { Value = value; }
    }

    public class TryStatement : Statement
    {
        public Statement Body { get; set; }
        public List<CatchClause> Catches { get; set; } = new();
        public Statement? FinallyBody { get; set; }
        public TryStatement(Statement body) { Body = body; }
    }

    public class CatchClause
    {
        public string? ExceptionType { get; set; }
        public string? VariableName { get; set; }
        public Statement? Body { get; set; }
    }

    public class ForEachStatement : Statement
    {
        public string VariableType { get; set; }
        public string VariableName { get; set; }
        public Expression Collection { get; set; }
        public Statement Body { get; set; }
        public ForEachStatement(string varType, string varName, Expression coll, Statement body)
        { VariableType = varType; VariableName = varName; Collection = coll; Body = body; }
    }

    public class EnumDeclStatement : Statement
    {
        public string? Name { get; set; }
        public List<string> Members { get; set; } = new();
        public Modifiers Modifiers { get; set; } = new();
    }
}