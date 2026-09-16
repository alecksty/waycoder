using System.Collections.Generic;

namespace CSharpCompiler
{
    /// <summary>
    /// AST 基类
    /// </summary>
    public abstract class ASTNode
    {}

    /// <summary>
    /// C#程序
    /// </summary>
    public class Program : ASTNode
    {
        public List<Statement> Statements { get; set; }
        public List<string> Namespaces { get; set; } = new();
        
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
        public string Type { get; set; }
        public string Name { get; set; }
        public Expression Initializer { get; set; }
        
        public VariableDeclStatement(string type, string name, Expression initializer = null)
        {
            Type = type;
            Name = name;
            Initializer = initializer;
        }
    }

    /// <summary>
    /// 方法声明语句
    /// </summary>
    public class EnumDeclStatement : Statement
    {
        public string Name { get; set; }
        public List<string> Members { get; set; }
        public EnumDeclStatement(string name, List<string> members)
        { Name = name; Members = members; }
    }

    public class MethodDeclStatement : Statement
    {
        public string ReturnType { get; set; }
        public string Name { get; set; }
        public List<Parameter> Parameters { get; set; }
        public Block Body { get; set; }
        public bool IsStatic { get; set; }
        public bool IsNative { get; set; }
        public string AliasName { get; set; }

        public MethodDeclStatement(string returnType, string name, List<Parameter> parameters = null, Block body = null)
        {
            ReturnType = returnType;
            Name = name;
            Parameters = parameters ?? new List<Parameter>();
            Body = body;
        }
    }

    /// <summary>
    /// 参数定义
    /// </summary>
    public class Parameter
    {
        public string Type { get; set; }
        public string Name { get; set; }
        
        public Parameter(string type, string name)
        {
            Type = type;
            Name = name;
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
    public class ForEachStatement : Statement
    {
        public string VariableType { get; set; }
        public string VariableName { get; set; }
        public Expression Collection { get; set; }
        public Statement Body { get; set; }
        public ForEachStatement(string varType, string varName, Expression collection, Statement body)
        { VariableType = varType; VariableName = varName; Collection = collection; Body = body; }
    }

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
    /// Console.WriteLine/Write语句
    /// </summary>
    public class ConsoleWriteLineStatement : Statement
    {
        public List<Expression> Arguments { get; }
        public bool HasNewLine { get; set; } = true;

        public ConsoleWriteLineStatement(List<Expression> arguments = null)
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
        
        public LiteralExpression(object value)
        {
            Value = value;
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
    /// 方法调用表达式
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
    /// 条件表达式 (cond ? trueVal : falseVal)
    /// </summary>
    public class ConditionalExpression : Expression
    {
        public Expression Condition { get; set; }
        public Expression TrueValue { get; set; }
        public Expression FalseValue { get; set; }
        public ConditionalExpression(Expression cond, Expression trueVal, Expression falseVal)
        { Condition = cond; TrueValue = trueVal; FalseValue = falseVal; }
    }

    /// <summary>
    /// 索引表达式（数组/字符串索引）
    /// </summary>
    public class IndexExpression : Expression
    {
        public Expression Object { get; set; }
        public Expression Index { get; set; }

        public IndexExpression(Expression obj, Expression index)
        { Object = obj; Index = index; }
    }

    /// <summary>
    /// 成员访问表达式
    /// </summary>
    public class MemberExpression : Expression
    {
        public Expression Object { get; set; }
        public string Member { get; set; }
        
        public MemberExpression(Expression obj, string member)
        {
            Object = obj;
            Member = member;
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
    /// 类型转换表达式: (int)expr, (float)expr
    /// </summary>
    public class CastExpression : Expression
    {
        public string TargetType { get; set; }
        public Expression Operand { get; set; }

        public CastExpression(string targetType, Expression operand)
        {
            TargetType = targetType;
            Operand = operand;
        }
    }

    /// <summary>
    /// break语句
    /// </summary>
    public class PropertyDeclaration : Statement
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public bool HasGet { get; set; }
        public bool HasSet { get; set; }
        public PropertyDeclaration(string type, string name, bool hasGet, bool hasSet)
        { Type = type; Name = name; HasGet = hasGet; HasSet = hasSet; }
    }

    public class ConstructorDeclaration : Statement
    {
        public string Name { get; set; }
        public List<Parameter> Parameters { get; set; }
        public Block Body { get; set; }
        public ConstructorDeclaration(string name, List<Parameter> parameters, Block body)
        {
            Name = name; Parameters = parameters; Body = body;
        }
    }

    public class ClassDeclaration : Statement
    {
        public string Name { get; set; }
        public List<Statement> Members { get; set; } = new();
        public ClassDeclaration(string name) { Name = name; }
    }

    public class BreakStatement : Statement
    {
        public BreakStatement() {}
    }

    /// <summary>
    /// continue语句
    /// </summary>
    public class ContinueStatement : Statement
    {
        public ContinueStatement() {}
    }

    public class SwitchStatement : Statement
    {
        public Expression Value { get; set; }
        public List<SwitchCase> Cases { get; set; } = new();
        public SwitchStatement(Expression value) { Value = value; }
    }

    public class SwitchCase
    {
        public Expression? Value { get; set; } // null = default
        public List<Statement> Body { get; set; } = new();
        public bool HasBreak { get; set; }
    }

    public class DoWhileStatement : Statement
    {
        public Statement Body { get; set; }
        public Expression Condition { get; set; }
        public DoWhileStatement(Statement body, Expression condition) { Body = body; Condition = condition; }
    }

    public class TryStatement : Statement
    {
        public Statement Body { get; set; }
        public List<CatchClause> Catches { get; set; } = new();
        public TryStatement(Statement body) { Body = body; }
    }

    public class CatchClause
    {
        public string? ExceptionType { get; set; }
        public string? VariableName { get; set; }
        public Statement Body { get; set; }
    }

    public class ThrowStatement : Statement
    {
        public Expression? Value { get; set; }
        public ThrowStatement(Expression? value = null) { Value = value; }
    }

    /// <summary>
    /// 数组字面量表达式
    /// </summary>
    public class ArrayLiteralExpression : Expression
    {
        public List<Expression> Elements { get; set; }

        /// <summary>
        /// `new T[N]` 的**长度表达式**（`new T[]{…}` 时为 null，长度取 <see cref="Elements"/>）。
        ///
        /// 为什么不在这里就地折成元素个数：长度的合法写法包含**常量标识符**
        /// （`static int[] sx = new int[MAXLEN];`），而常量表在**代码生成器**那边
        /// （`RegisterStaticField` 把 `const` 字段折进了 `dataSection`）。
        /// 解析阶段折不了就只能报错 —— 而这一轮实测过：报错会让整个类成员的解析降级，
        /// 连**前面已经登记好的 `const` 初值一起丢掉**（`var_CW` 从 20 变成 0 ⇒ 除零崩溃）。
        /// 故这里只**留着表达式**，交给 `GenerateArrayLiteral` 折。
        /// </summary>
        public Expression SizeExpr { get; set; }

        public ArrayLiteralExpression(List<Expression> elements = null)
        {
            Elements = elements ?? new List<Expression>();
        }
    }

    public class NewExpression : Expression
    {
        public string TypeName { get; set; }
        public List<Expression> Arguments { get; set; } = new();

        public NewExpression(string typeName)
        {
            TypeName = typeName;
        }
    }

    /// <summary>
    /// 指针解引用表达式: *ptr
    /// </summary>
    public class DerefExpression : Expression
    {
        public Expression Target { get; set; }
        public DerefExpression(Expression target) { Target = target; }
    }

    /// <summary>
    /// 取地址表达式: &var
    /// </summary>
    public class AddrOfExpression : Expression
    {
        public Expression Target { get; set; }
        public AddrOfExpression(Expression target) { Target = target; }
    }

    /// <summary>
    /// Unsafe 代码块
    /// </summary>
    public class UnsafeBlock : Statement
    {
        public Block Body { get; set; }
        public UnsafeBlock(Block body) { Body = body; }
    }
}