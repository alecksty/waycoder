using System.Collections.Generic;

namespace GoCompiler
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
    /// 程序节点
    /// </summary>
    public class Program : ASTNode
    {
        public string PackageName { get; set; }
        public List<Import> Imports { get; set; }
        public List<ASTNode> Declarations { get; set; }
        public List<Function> Functions { get; set; }
        public List<VariableDecl> Variables { get; set; }
        public List<ConstantDecl> Constants { get; set; }
        public List<TypeDecl> Types { get; set; }

        public Program()
        {
            PackageName = "main";
            Imports = new List<Import>();
            Declarations = new List<ASTNode>();
            Functions = new List<Function>();
            Variables = new List<VariableDecl>();
            Constants = new List<ConstantDecl>();
            Types = new List<TypeDecl>();
        }
    }

    /// <summary>
    /// 导入语句
    /// </summary>
    public class Import : ASTNode
    {
        public string Path { get; set; }
        public string Alias { get; set; }

        public Import(string path, string alias = null)
        {
            Path = path;
            Alias = alias;
        }
    }

    /// <summary>
    /// 函数声明
    /// </summary>
    public class Function : ASTNode
    {
        public string Name { get; set; }
        public List<Parameter> Parameters { get; set; }
        public List<Parameter> Results { get; set; }
        public Block Body { get; set; }
        public bool IsMain { get; set; }
        public bool IsVariadic { get; set; }

        public Function(string name)
        {
            Name = name;
            Parameters = new List<Parameter>();
            Results = new List<Parameter>();
            IsMain = false;
            IsVariadic = false;
        }
    }

    /// <summary>
    /// 参数
    /// </summary>
    public class Parameter : ASTNode
    {
        public List<string> Names { get; set; }
        public GoType Type { get; set; }

        public Parameter()
        {
            Names = new List<string>();
        }

        public Parameter(List<string> names, GoType type)
        {
            Names = names;
            Type = type;
        }
    }

    /// <summary>
    /// Go语言类型
    /// </summary>
    public class GoType : ASTNode
    {
        public string Name { get; set; }
        public GoType ElementType { get; set; }
        public List<GoType> KeyTypes { get; set; }
        public List<GoType> ValueTypes { get; set; }
        public List<Field> Fields { get; set; }
        public List<GoType> MethodTypes { get; set; }

        public GoType(string name)
        {
            Name = name;
        }

        public static GoType Int => new GoType("int");
        public static GoType Int32 => new GoType("int32");
        public static GoType Int64 => new GoType("int64");
        public static GoType Uint => new GoType("uint");
        public static GoType Uint32 => new GoType("uint32");
        public static GoType Float32 => new GoType("float32");
        public static GoType Float64 => new GoType("float64");
        public static GoType Bool => new GoType("bool");
        public static GoType String => new GoType("string");
        public static GoType Byte => new GoType("byte");
        public static GoType Rune => new GoType("rune");

        public static GoType Array(GoType elementType, int size)
        {
            return new GoType($"[{size}]") { ElementType = elementType };
        }

        public static GoType Slice(GoType elementType)
        {
            return new GoType("[]") { ElementType = elementType };
        }

        public static GoType Map(GoType keyType, GoType valueType)
        {
            var t = new GoType("map");
            t.KeyTypes = new List<GoType> { keyType };
            t.ValueTypes = new List<GoType> { valueType };
            return t;
        }

        public static GoType Chan(GoType elementType)
        {
            return new GoType("chan") { ElementType = elementType };
        }

        public static GoType Interface(List<GoType> methodTypes = null)
        {
            return new GoType("interface") { MethodTypes = methodTypes ?? new List<GoType>() };
        }
    }

    /// <summary>
    /// 结构体字段
    /// </summary>
    public class Field : ASTNode
    {
        public List<string> Names { get; set; }
        public GoType Type { get; set; }

        public Field()
        {
            Names = new List<string>();
        }
    }

    /// <summary>
    /// 代码块
    /// </summary>
    public class Block : ASTNode
    {
        public List<ASTNode> Statements { get; set; }

        public Block()
        {
            Statements = new List<ASTNode>();
        }
    }

    /// <summary>
    /// 变量声明
    /// </summary>
    public class VariableDecl : ASTNode
    {
        public List<string> Names { get; set; }
        public GoType Type { get; set; }
        public List<ASTNode> Values { get; set; }

        public VariableDecl()
        {
            Names = new List<string>();
            Values = new List<ASTNode>();
        }
    }

    /// <summary>
    /// 常量声明
    /// </summary>
    public class ConstantDecl : ASTNode
    {
        public List<string> Names { get; set; }
        public GoType Type { get; set; }
        public List<ASTNode> Values { get; set; }

        public ConstantDecl()
        {
            Names = new List<string>();
            Values = new List<ASTNode>();
        }
    }

    /// <summary>
    /// 类型声明
    /// </summary>
    public class TypeDecl : ASTNode
    {
        public string Name { get; set; }
        public GoType Type { get; set; }

        public TypeDecl(string name, GoType type)
        {
            Name = name;
            Type = type;
        }
    }

    /// <summary>
    /// if 语句
    /// </summary>
    public class IfStatement : ASTNode
    {
        public ASTNode Init { get; set; }
        public ASTNode Condition { get; set; }
        public Block ThenBranch { get; set; }
        public List<(ASTNode Condition, Block Body)> ElseIfBranches { get; set; }
        public Block ElseBranch { get; set; }

        public IfStatement()
        {
            ElseIfBranches = new List<(ASTNode, Block)>();
        }
    }

    /// <summary>
    /// for 语句
    /// </summary>
    public class ForStatement : ASTNode
    {
        public ASTNode Init { get; set; }
        public ASTNode Condition { get; set; }
        public ASTNode Post { get; set; }
        public Block Body { get; set; }

        public bool IsRangeLoop { get; set; }
        public ASTNode RangeExpr { get; set; }
        public string KeyVar { get; set; }
        public string ValueVar { get; set; }
    }

    /// <summary>
    /// switch 语句
    /// </summary>
    public class SwitchStatement : ASTNode
    {
        public ASTNode Init { get; set; }
        public ASTNode Tag { get; set; }
        public List<CaseClause> Cases { get; set; }

        public SwitchStatement()
        {
            Cases = new List<CaseClause>();
        }
    }

    /// <summary>
    /// case 子句
    /// </summary>
    public class CaseClause : ASTNode
    {
        public List<ASTNode> Cases { get; set; }
        public Block Body { get; set; }
        public bool IsDefault { get; set; }

        public CaseClause()
        {
            Cases = new List<ASTNode>();
        }
    }

    /// <summary>
    /// return 语句
    /// </summary>
    public class ReturnStatement : ASTNode
    {
        public List<ASTNode> Results { get; set; }

        public ReturnStatement()
        {
            Results = new List<ASTNode>();
        }
    }

    /// <summary>
    /// break 语句
    /// </summary>
    public class BreakStatement : ASTNode
    {
        public string Label { get; set; }
    }

    /// <summary>
    /// continue 语句
    /// </summary>
    public class ContinueStatement : ASTNode
    {
        public string Label { get; set; }
    }

    /// <summary>
    /// goto 语句
    /// </summary>
    public class GotoStatement : ASTNode
    {
        public string Label { get; set; }

        public GotoStatement(string label)
        {
            Label = label;
        }
    }

    /// <summary>
    /// 标签语句
    /// </summary>
    public class LabeledStatement : ASTNode
    {
        public string Name { get; set; }
        public ASTNode Statement { get; set; }

        public LabeledStatement(string name, ASTNode statement)
        {
            Name = name;
            Statement = statement;
        }
    }

    /// <summary>
    /// 表达式语句
    /// </summary>
    public class ExpressionStatement : ASTNode
    {
        public ASTNode Expression { get; set; }

        public ExpressionStatement(ASTNode expression)
        {
            Expression = expression;
        }
    }

    /// <summary>
    /// 短变量声明 (:=)
    /// </summary>
    public class ShortVarDecl : ASTNode
    {
        public List<string> Names { get; set; }
        public List<ASTNode> Values { get; set; }

        public ShortVarDecl()
        {
            Names = new List<string>();
            Values = new List<ASTNode>();
        }
    }

    /// <summary>
    /// 赋值语句
    /// </summary>
    public class Assignment : ASTNode
    {
        public List<ASTNode> Left { get; set; }
        public List<ASTNode> Right { get; set; }
        public string Op { get; set; }

        public Assignment()
        {
            Left = new List<ASTNode>();
            Right = new List<ASTNode>();
        }

        public Assignment(List<ASTNode> left, List<ASTNode> right, string op = "=")
        {
            Left = left;
            Right = right;
            Op = op;
        }
    }

    /// <summary>
    /// 函数调用
    /// </summary>
    public class FunctionCall : ASTNode
    {
        public ASTNode Function { get; set; }
        public List<ASTNode> Arguments { get; set; }

        public FunctionCall(ASTNode function)
        {
            Function = function;
            Arguments = new List<ASTNode>();
        }
    }

    /// <summary>
    /// 方法调用
    /// </summary>
    public class MethodCall : ASTNode
    {
        public ASTNode Receiver { get; set; }
        public string MethodName { get; set; }
        public List<ASTNode> Arguments { get; set; }

        public MethodCall(ASTNode receiver, string methodName)
        {
            Receiver = receiver;
            MethodName = methodName;
            Arguments = new List<ASTNode>();
        }
    }

    /// <summary>
    /// 二元运算
    /// </summary>
    public class BinaryOp : ASTNode
    {
        public string Op { get; set; }
        public ASTNode Left { get; set; }
        public ASTNode Right { get; set; }

        public BinaryOp(string op, ASTNode left, ASTNode right)
        {
            Op = op;
            Left = left;
            Right = right;
        }
    }

    /// <summary>
    /// 一元运算
    /// </summary>
    public class UnaryOp : ASTNode
    {
        public string Op { get; set; }
        public ASTNode Operand { get; set; }

        public UnaryOp(string op, ASTNode operand)
        {
            Op = op;
            Operand = operand;
        }
    }

    /// <summary>
    /// 标识符
    /// </summary>
    public class Identifier : ASTNode
    {
        public string Name { get; set; }

        public Identifier(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// 数字字面量
    /// </summary>
    public class NumberLiteral : ASTNode
    {
        public string Value { get; set; }
        public bool IsFloat { get; set; }

        public NumberLiteral(string value, bool isFloat = false)
        {
            Value = value;
            IsFloat = isFloat;
        }
    }

    /// <summary>
    /// 字符串字面量
    /// </summary>
    public class StringLiteral : ASTNode
    {
        public string Value { get; set; }
        public bool IsRaw { get; set; }

        public StringLiteral(string value, bool isRaw = false)
        {
            Value = value;
            IsRaw = isRaw;
        }
    }

    /// <summary>
    /// 字符字面量
    /// </summary>
    public class CharLiteral : ASTNode
    {
        public string Value { get; set; }

        public CharLiteral(string value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// 布尔字面量
    /// </summary>
    public class BoolLiteral : ASTNode
    {
        public bool Value { get; set; }

        public BoolLiteral(bool value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// nil 字面量
    /// </summary>
    public class NilLiteral : ASTNode
    {
    }

    /// <summary>
    /// 数组字面量
    /// </summary>
    public class ArrayLiteral : ASTNode
    {
        public List<ASTNode> Elements { get; set; }

        public ArrayLiteral()
        {
            Elements = new List<ASTNode>();
        }
    }

    /// <summary>
    /// 切片字面量
    /// </summary>
    public class SliceLiteral : ASTNode
    {
        public List<ASTNode> Elements { get; set; }

        public SliceLiteral()
        {
            Elements = new List<ASTNode>();
        }
    }

    /// <summary>
    /// 复合字面量
    /// </summary>
    public class CompositeLiteral : ASTNode
    {
        public GoType Type { get; set; }
        public List<ASTNode> Elements { get; set; }

        public CompositeLiteral()
        {
            Elements = new List<ASTNode>();
        }
    }

    /// <summary>
    /// 键值对
    /// </summary>
    public class KeyValueExpr : ASTNode
    {
        public ASTNode Key { get; set; }
        public ASTNode Value { get; set; }

        public KeyValueExpr(ASTNode key, ASTNode value)
        {
            Key = key;
            Value = value;
        }
    }

    /// <summary>
    /// 数组/切片访问
    /// </summary>
    public class IndexExpr : ASTNode
    {
        public ASTNode Array { get; set; }
        public ASTNode Index { get; set; }

        public IndexExpr(ASTNode array, ASTNode index)
        {
            Array = array;
            Index = index;
        }
    }

    /// <summary>
    /// 切片表达式
    /// </summary>
    public class SliceExpr : ASTNode
    {
        public ASTNode Array { get; set; }
        public ASTNode Low { get; set; }
        public ASTNode High { get; set; }
        public ASTNode Max { get; set; }

        public SliceExpr(ASTNode array)
        {
            Array = array;
        }
    }

    /// <summary>
    /// 选择器表达式 (e.g., obj.field)
    /// </summary>
    public class SelectorExpr : ASTNode
    {
        public ASTNode Expr { get; set; }
        public string Sel { get; set; }

        public SelectorExpr(ASTNode expr, string sel)
        {
            Expr = expr;
            Sel = sel;
        }
    }

    /// <summary>
    /// 类型断言 (e.g., i.(T))
    /// </summary>
    public class TypeAssertion : ASTNode
    {
        public ASTNode X { get; set; }
        public GoType Type { get; set; }

        public TypeAssertion(ASTNode x, GoType type = null)
        {
            X = x;
            Type = type;
        }
    }

    /// <summary>
    /// 类型转换
    /// </summary>
    public class TypeConversion : ASTNode
    {
        public GoType Type { get; set; }
        public ASTNode Arg { get; set; }

        public TypeConversion(GoType type, ASTNode arg)
        {
            Type = type;
            Arg = arg;
        }
    }

    /// <summary>
    /// 类型表达式（make/delete/type assertion 等内置函数参数）
    /// </summary>
    public class TypeExpr : ASTNode
    {
        public GoType Type { get; set; }

        public TypeExpr(GoType type)
        {
            Type = type;
        }
    }

    /// <summary>
    /// 函数字面量 func(...) { ... }
    /// </summary>
    public class FuncLiteral : ASTNode
    {
        public List<Parameter> Parameters { get; set; }
        public List<Parameter> Results { get; set; }
        public Block Body { get; set; }

        public FuncLiteral()
        {
            Parameters = new List<Parameter>();
            Results = new List<Parameter>();
        }
    }

    /// <summary>
    /// go 语句
    /// </summary>
    public class GoStatement : ASTNode
    {
        public ASTNode Call { get; set; }

        public GoStatement(ASTNode call)
        {
            Call = call;
        }
    }

    /// <summary>
    /// defer 语句
    /// </summary>
    public class DeferStatement : ASTNode
    {
        public ASTNode Call { get; set; }

        public DeferStatement(ASTNode call)
        {
            Call = call;
        }
    }

    /// <summary>
    /// send 语句 (ch <- val)
    /// </summary>
    public class SendStatement : ASTNode
    {
        public ASTNode Chan { get; set; }
        public ASTNode Value { get; set; }

        public SendStatement(ASTNode chan, ASTNode value)
        {
            Chan = chan;
            Value = value;
        }
    }

    /// <summary>
    /// 接收表达式 (&lt;-ch)
    /// </summary>
    public class ReceiveExpr : ASTNode
    {
        public ASTNode Chan { get; set; }

        public ReceiveExpr(ASTNode chan)
        {
            Chan = chan;
        }
    }

    /// <summary>
    /// select 语句
    /// </summary>
    public class SelectStatement : ASTNode
    {
        public List<SelectClause> Clauses { get; set; }

        public SelectStatement()
        {
            Clauses = new List<SelectClause>();
        }
    }

    /// <summary>
    /// select case/default 子句
    /// </summary>
    public class SelectClause : ASTNode
    {
        public ASTNode Communication { get; set; }
        public Block Body { get; set; }
        public bool IsDefault { get; set; }

        public SelectClause()
        {
            Body = new Block();
        }
    }

    /// <summary>
    /// 空接口
    /// </summary>
    public class EmptyStatement : ASTNode
    {
    }

    /// <summary>
    /// 递减语句
    /// </summary>
    public class IncDecStatement : ASTNode
    {
        public ASTNode Expression { get; set; }
        public bool IsIncrement { get; set; }

        public IncDecStatement(ASTNode expr, bool isIncrement)
        {
            Expression = expr;
            IsIncrement = isIncrement;
        }
    }

    /// <summary>
    /// fallthrough 语句
    /// </summary>
    public class FallthroughStatement : ASTNode
    {
    }

    /// <summary>
    /// 赋值语句包装器（解析临时使用）
    /// </summary>
    public class AssignmentWrapper : ASTNode
    {
        public ASTNode Left { get; set; }

        public AssignmentWrapper(ASTNode left)
        {
            Left = left;
        }
    }
}
