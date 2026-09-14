using System.Collections.Generic;

namespace SwiftCompiler
{
    /// <summary>
    /// AST 基类
    /// </summary>
    public abstract class ASTNode
    {}

    /// <summary>
    /// Swift程序
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
        public string Keyword { get; set; } // let, var
        public string Name { get; set; }
        public string TypeAnnotation { get; set; } // 可选类型注解
        public Expression Initializer { get; set; }
        
        public VariableDeclStatement(string keyword, string name, string typeAnnotation = null, Expression initializer = null)
        {
            Keyword = keyword;
            Name = name;
            TypeAnnotation = typeAnnotation;
            Initializer = initializer;
        }
    }

    /// <summary>
    /// 函数声明语句
    /// </summary>
    public class FunctionDeclStatement : Statement
    {
        public string Name { get; set; }
        public List<Parameter> Parameters { get; set; }
        public string ReturnType { get; set; }
        public Block Body { get; set; }
        public bool IsNative { get; set; }

        public FunctionDeclStatement(string name, List<Parameter> parameters = null, string returnType = null, Block body = null, bool isNative = false)
        {
            Name = name;
            Parameters = parameters ?? new List<Parameter>();
            ReturnType = returnType;
            Body = body;
            IsNative = isNative;
        }
    }

    /// <summary>
    /// 参数定义
    /// </summary>
    public class Parameter
    {
        public string ExternalName { get; set; } // 外部参数名
        public string InternalName { get; set; } // 内部参数名
        public string Type { get; set; }
        
        public Parameter(string externalName, string internalName, string type)
        {
            ExternalName = externalName;
            InternalName = internalName;
            Type = type;
        }
        
        public Parameter(string name, string type) : this(name, name, type)
        {
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
    /// 打印语句
    /// </summary>
    public class PrintStatement : Statement
    {
        public List<Expression> Arguments { get; set; }
        
        public PrintStatement(List<Expression> arguments = null)
        {
            Arguments = arguments ?? new List<Expression>();
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
        
        public UnaryExpression(TokenType op, Expression operand)
        {
            Operator = op;
            Operand = operand;
        }
    }

    /// <summary>
    /// 赋值表达式
    /// </summary>
    public class AssignmentExpression : Expression
    {
        public Expression Target { get; set; }
        public TokenType Operator { get; set; }
        public Expression Value { get; set; }
        
        public AssignmentExpression(Expression target, TokenType op, Expression value)
        {
            Target = target;
            Operator = op;
            Value = value;
        }
    }

    /// <summary>
    /// 函数调用表达式
    /// </summary>
    public class CallExpression : Expression
    {
        public Expression Callee { get; set; }
        public List<Expression> Arguments { get; set; }
        
        public CallExpression(Expression callee, List<Expression> arguments = null)
        {
            Callee = callee;
            Arguments = arguments ?? new List<Expression>();
        }
    }

    /// <summary>
    /// 成员访问表达式
    /// </summary>
    public class MemberExpression : Expression
    {
        public Expression Object { get; set; }
        public string Property { get; set; }
        
        public MemberExpression(Expression obj, string property)
        {
            Object = obj;
            Property = property;
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
    /// 字符串插值表达式
    /// </summary>
    public class StringInterpolationExpression : Expression
    {
        public List<Expression> Parts { get; set; }
        
        public StringInterpolationExpression(List<Expression> parts = null)
        {
            Parts = parts ?? new List<Expression>();
        }
    }
    
    /// <summary>
    /// 数组字面量表达式
    /// </summary>
    public class ArrayLiteralExpression : Expression
    {
        public List<Expression> Elements { get; set; }
        
        public ArrayLiteralExpression(List<Expression> elements = null)
        {
            Elements = elements ?? new List<Expression>();
        }
    }
    
    /// <summary>
    /// 可选类型表达式
    /// </summary>
    public class OptionalExpression : Expression
    {
        public Expression Expression { get; set; }
        public TokenType Operator { get; set; }
        public OptionalExpression(Expression expression, TokenType op) { Expression = expression; Operator = op; }
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

    public class BreakStatement : Statement {}
    public class ContinueStatement : Statement {}

    public class ThrowStatement : Statement
    {
        public Expression? Value { get; set; }
        public ThrowStatement(Expression? value = null) { Value = value; }
    }

    public class DoStatement : Statement
    {
        public Statement Body { get; set; }
        public List<CatchClause> Catches { get; set; } = new();
        public DoStatement(Statement body) { Body = body; }
    }

    public class CatchClause
    {
        public string? Pattern { get; set; }
        public Statement Body { get; set; }
    }

    public class StructDeclStatement : Statement
    {
        public string Name { get; set; }
        public List<StructField> Fields { get; set; } = new();
        public List<string> Protocols { get; set; } = new();
    }

    public class StructField
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public class EnumDeclStatement : Statement
    {
        public string Name { get; set; }
        public List<string> Members { get; set; } = new();
    }

    public class ProtocolDeclStatement : Statement
    {
        public string Name { get; set; }
        public List<ProtocolMethod> Methods { get; set; } = new();
    }

    public class ProtocolMethod
    {
        public string Name { get; set; }
        public List<string> Parameters { get; set; } = new();
        public string ReturnType { get; set; } = "void";
    }

    public class ExtensionDeclStatement : Statement
    {
        public string TypeName { get; set; }
        public List<FunctionDeclStatement> Methods { get; set; } = new();
    }

    public class GuardStatement : Statement
    {
        public Expression Condition { get; set; }
        public Statement Body { get; set; }
    }

    public class DeferStatement : Statement
    {
        public Statement Body { get; set; }
    }

    public class IndexAccessExpression : Expression
    {
        public Expression Target { get; set; }
        public Expression Index { get; set; }
        public IndexAccessExpression(Expression target, Expression index)
        { Target = target; Index = index; }
    }

    public class ClosureExpression : Expression
    {
        public List<string> Parameters { get; set; } = new();
        public string? ReturnType { get; set; }
        public Statement Body { get; set; }
    }

    public class ForEachStatement : Statement
    {
        public string VariableName { get; set; }
        public Expression Collection { get; set; }
        public Statement Body { get; set; }
        public ForEachStatement(string varName, Expression collection, Statement body)
        { VariableName = varName; Collection = collection; Body = body; }
    }

    public class DictionaryLiteralExpression : Expression
    {
        public List<KeyValuePair<Expression, Expression>> Entries { get; set; } = new();
    }

    public class IfLetStatement : Statement
    {
        public string VariableName { get; set; }
        public Expression OptionalExpr { get; set; }
        public Statement ThenBranch { get; set; }
        public Statement? ElseBranch { get; set; }
    }

    public class GuardLetStatement : Statement
    {
        public string VariableName { get; set; }
        public Expression OptionalExpr { get; set; }
        public Statement ElseBranch { get; set; }
    }
}