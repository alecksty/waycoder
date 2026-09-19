using System.Collections.Generic;

namespace JavaScriptCompiler
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
    /// 函数参数定义（支持默认值）
    /// </summary>
    public class ParameterDef
    {
        public string Name { get; set; }
        public Expression DefaultValue { get; set; }  // null if no default

        public ParameterDef(string name, Expression defaultValue = null)
        {
            Name = name;
            DefaultValue = defaultValue;
        }
    }

    /// <summary>
    /// JavaScript程序
    /// </summary>
    public class Program : ASTNode
    {
        public List<Statement> Statements { get; set; }
        
        public Program()
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
        public string Keyword { get; set; } // var, let, const
        public string Name { get; set; }
        public Expression Initializer { get; set; }
        
        public VariableDeclStatement(string keyword, string name, Expression initializer = null)
        {
            Keyword = keyword;
            Name = name;
            Initializer = initializer;
        }
    }

    /// <summary>
    /// 函数声明语句
    /// </summary>
    public class FunctionDeclStatement : Statement
    {
        public string Name { get; set; }
        public List<ParameterDef> Parameters { get; set; }
        public Block Body { get; set; }
        public bool IsNative { get; set; }

        public FunctionDeclStatement(string name)
        {
            Name = name;
            Parameters = new List<ParameterDef>();
            Body = new Block();
        }
    }

    /// <summary>
    /// 返回语句
    /// </summary>
    public class ReturnStatement : Statement
    {
        public Expression Value { get; set; }
        
        public ReturnStatement(Expression value = null)
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
        public Statement ElseBranch { get; set; }
        
        public IfStatement(Expression condition, Statement thenBranch, Statement elseBranch = null)
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
        public Statement Initializer { get; set; }
        public Expression Condition { get; set; }
        public Expression Increment { get; set; }
        public Statement Body { get; set; }
        
        public ForStatement(Statement initializer, Expression condition, Expression increment, Statement body)
        {
            Initializer = initializer;
            Condition = condition;
            Increment = increment;
            Body = body;
        }
    }

    /// <summary>
    /// 代码块
    /// </summary>
    public class Block : Statement
    {
        public List<Statement> Statements { get; set; }
        
        public Block()
        {
            Statements = new List<Statement>();
        }
    }

    /// <summary>
    /// Break语句
    /// </summary>
    public class BreakStatement : Statement
    {
    }

    /// <summary>
    /// Continue语句
    /// </summary>
    public class ContinueStatement : Statement
    {
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
        public object Value { get; set; }
        public string Type { get; set; }
        
        public LiteralExpression(object value, string type = null)
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
    /// 二元表达式
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
    /// 一元表达式
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
    /// 函数调用表达式
    /// </summary>
    public class CallExpression : Expression
    {
        public Expression Callee { get; set; }
        public List<Expression> Arguments { get; set; }
        
        public CallExpression(Expression callee)
        {
            Callee = callee;
            Arguments = new List<Expression>();
        }
    }

    /// <summary>
    /// 成员访问表达式
    /// </summary>
    public class MemberExpression : Expression
    {
        public Expression Object { get; set; }
        public string Property { get; set; }
        public bool Computed { get; set; }
        
        public MemberExpression(Expression obj, string property, bool computed = false)
        {
            Object = obj;
            Property = property;
            Computed = computed;
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

    /// <summary>
    /// 字符串模板表达式（支持模板字符串）
    /// </summary>
    public class TemplateExpression : Expression
    {
        public List<Expression> Parts { get; set; }

        public TemplateExpression()
        {
            Parts = new List<Expression>();
        }
    }

    /// <summary>
    /// 数组字面量表达式
    /// </summary>
    public class ArrayLiteralExpression : Expression
    {
        public List<Expression> Elements { get; set; }

        public ArrayLiteralExpression()
        {
            Elements = new List<Expression>();
        }
    }

    public class ObjectLiteralExpression : Expression
    {
        public Dictionary<string, Expression> Properties { get; set; }
        public List<(Expression keyExpr, Expression valueExpr)> ComputedProperties { get; set; }
        public ObjectLiteralExpression() { Properties = new Dictionary<string, Expression>(); ComputedProperties = new(); }
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

    public class DoWhileStatement : Statement
    {
        public Statement Body { get; set; }
        public Expression Condition { get; set; }
        public DoWhileStatement(Statement body, Expression cond) { Body = body; Condition = cond; }
    }

    public class ConditionalExpression : Expression
    {
        public Expression Condition { get; set; }
        public Expression TrueValue { get; set; }
        public Expression FalseValue { get; set; }
        public ConditionalExpression(Expression cond, Expression tv, Expression fv)
        { Condition = cond; TrueValue = tv; FalseValue = fv; }
    }

    public class NewExpression : Expression
    {
        public string Type { get; set; }
        public List<Expression> Arguments { get; set; } = new();
        public NewExpression(string type) { Type = type; }
    }

    public class InstanceofExpression : Expression
    {
        public Expression Left { get; set; }
        public string TypeName { get; set; }
        public InstanceofExpression(Expression left, string typeName)
        { Left = left; TypeName = typeName; }
    }

    public class InExpression : Expression
    {
        public Expression Left { get; set; }
        public Expression Right { get; set; }
        public InExpression(Expression left, Expression right)
        { Left = left; Right = right; }
    }

    public class SuperExpression : Expression
    {
        public List<Expression> Arguments { get; set; } = new();
        public SuperExpression() { }
    }

    public class IndexExpression : Expression
    {
        public Expression Object { get; set; }
        public Expression Index { get; set; }
        public IndexExpression(Expression obj, Expression index)
        { Object = obj; Index = index; }
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
        public TryStatement(Statement body) { Body = body; }
    }

    public class CatchClause
    {
        public string? VariableName { get; set; }
        public Statement Body { get; set; }
    }

    public class FunctionExpression : Expression
    {
        public string? Name { get; set; }
        public List<ParameterDef> Parameters { get; set; } = new();
        public Block Body { get; set; } = new();
    }

    public class ArrowFunctionExpression : Expression
    {
        public List<ParameterDef> Parameters { get; set; } = new();
        public Statement Body { get; set; }
    }

    public class ClassDeclStatement : Statement
    {
        public string Name { get; set; }
        public string? ParentClass { get; set; }
        public FunctionDeclStatement? Constructor { get; set; }
        public List<FunctionDeclStatement> Methods { get; set; } = new();
    }

    public class MethodDefinition
    {
        public string Name { get; set; }
        public FunctionDeclStatement Function { get; set; }
    }

    public class ArrayDestructureStatement : Statement
    {
        public List<string> Names { get; set; } = new();
        public Expression Initializer { get; set; }
    }

    public class ObjectDestructureStatement : Statement
    {
        public List<string> Names { get; set; } = new();
        public Expression Initializer { get; set; }
    }
}