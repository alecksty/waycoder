namespace CCompiler
{
    /// <summary>
    /// AST 基类。
    ///
    /// ⚠ 位置字段（<see cref="Line"/>/<see cref="Column"/>）是**后补的** —— 此前这个基类是**完全空的**，
    /// 全仓 41 个节点类一个行号都没有。后果是三件事都做不到：
    ///   · 报错给不出行列号（未声明变量只报 `<input>: error:`，指不到哪一行）；
    ///   · 未使用符号的警告指不到声明处；
    ///   · 编辑器里的气泡没有锚点。
    ///
    /// 填法收在**语句入口** `Parser.ParseStatement()` 一处（改名前是它的主体）——
    /// 在那里记下起始 token 的行列、解析完再盖到节点上，一处覆盖全部 36 个 return。
    /// **0 = 没填**（表达式节点一般不填，只有语句/声明级别的才需要）。
    /// </summary>
    public abstract class ASTNode
    {
        /// <summary>
        /// **预处理之后**的源码行号（1-based）；**0 = 未知**。
        ///
        /// ⚠ 这个才与 `SourceLines`（= `processedSource.Split('\n')`）**同一套索引** ——
        ///   产物里 `; N: &lt;原文&gt;` 注释取的就是 `SourceLines[N-1]`，拿别的行号来索引
        ///   会引到**毫不相干的一行**（实测：引出来的是 stdio.h 里的注释文字）。
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// **原文件**里的行号（1-based）；**0 = 未知**（没有 lineMap 时退回 <see cref="Line"/>）。
        ///
        /// 报给用户/编辑器的必须是这个 —— `#include` 一展开就把后面所有行整体推后
        /// （实测 `Examples/c/gomoku.c`：`int x0;` 原文件第 196 行、预处理后第 334 行）。
        /// 与 <see cref="Line"/> **分工不同、不能互相替代**：一个索引 SourceLines，一个给人看。
        /// </summary>
        public int OriginalLine { get; set; }

        /// <summary>源码列号（1-based）；**0 = 未知**。</summary>
        public int Column { get; set; }
    }

    /// <summary>
    /// 程序
    /// </summary>
    public class Program : ASTNode
    {
        public List<Function> Functions { get; set; }
        public List<VariableDecl> Variables { get; set; }

        public Dictionary<string, string> TypeDefs { get; set; }        

        public Dictionary<string, Dictionary<string, int>> EnumConstants { get; set; } // 枚举名 -> (常量名 -> 值)

        public Dictionary<string, StructDecl> Structs { get; set; } // struct名 -> StructDecl
        public Dictionary<string, UnionDecl> Unions { get; set; }  // union名 -> UnionDecl

        public Program()
        {
            Functions = new List<Function>();
            Variables = new List<VariableDecl>();
            TypeDefs = new Dictionary<string, string>();
            EnumConstants = new Dictionary<string, Dictionary<string, int>>();
            Structs = new Dictionary<string, StructDecl>();
            Unions = new Dictionary<string, UnionDecl>();
        }
    }

    /// <summary>
    /// 函数
    /// </summary>
    public class Function : ASTNode
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public List<Parameter> Params { get; set; }
        public Block Body { get; set; }
        public bool IsMain { get; set; }
        public bool IsDeclaration { get; set; }
        public bool IsVariadic { get; set; }
        public bool IsInterrupt { get; set; }

        /// <summary>
        /// `static` 函数 —— **内部链接**，外部访问不到。
        ///
        /// 这正是「未使用」该不该报警的**判据**（用户原话：「未使用的只报外部无法访问的」）：
        /// 非 static 的函数有外部链接、随时可能被别的翻译单元调用，
        /// 「本文件没调它」根本说明不了什么；只有 `static` 的才真的是"只可能在本文件里用"。
        ///
        /// ⚠ 解析器此前把存储类说明符读进一个局部变量 `storageClass` 就丢了，
        ///   `Function` 上根本没有这个字段 —— 所以这条判据以前**没法表达**。
        /// </summary>
        public bool IsStatic { get; set; }
        public CallingConvention Convention { get; set; } = CallingConvention.Cdecl;

        public Function(string name, string returnType, List<Parameter> parameters, Block body, bool isMain = false, bool isDeclaration = false, bool isVariadic = false, bool isInterrupt = false, CallingConvention convention = CallingConvention.Cdecl)
        {
            Name = name;
            ReturnType = returnType;
            Params = parameters;
            Body = body;
            IsMain = isMain;
            IsDeclaration = isDeclaration;
            IsVariadic = isVariadic;
            IsInterrupt = isInterrupt;
            Convention = convention;
        }

        /// <summary>
        /// 获取函数签名（支持重载）
        /// </summary>
        public string GetSignature()
        {
            var paramTypes = Params == null ? new List<string>() : Params.Select(p => p.Type).ToList();
            return $"{Name}({string.Join(",", paramTypes)})";
        }

        /// <summary>
        /// 简化的函数签名（用于匹配）
        /// </summary>
        public string GetSimpleSignature()
        {
            if (Params == null || Params.Count == 0)
                return $"{Name}()";
            var paramTypes = Params.Select(p => NormalizeType(p.Type)).ToList();
            return $"{Name}({string.Join(",", paramTypes)})";
        }

        private string NormalizeType(string type)
        {
            if (string.IsNullOrEmpty(type)) return "void";
            type = type.Trim();
            // 简化类型映射
            if (type.Contains("int")) return "int";
            if (type.Contains("char")) return "char";
            if (type.Contains("float")) return "float";
            if (type.Contains("double")) return "double";
            if (type.Contains("void")) return "void";
            if (type.Contains("*")) return "ptr";
            return type;
        }
    }

    /// <summary>
    /// 函数参数
    /// </summary>
    public class Parameter : ASTNode
    {
        public string Name { get; set; }
        public string Type { get; set; }

        public Parameter(string name, string type)
        {
            Name = name;
            Type = type;
        }
    }

    /// <summary>
    /// 变量声明
    /// </summary>
    public class VariableDecl : ASTNode
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public ASTNode Initializer { get; set; }
        public bool IsArray { get; set; }
        public int? ArraySize { get; set; }
        public List<int?> Dimensions { get; set; }
        public bool IsStatic { get; set; }
        /// <summary>VLA dimensions (runtime expressions) — null if not VLA</summary>
        public List<ASTNode>? VlaDimensions { get; set; }
        /// <summary>True if any dimension is runtime (VLA)</summary>
        public bool IsVLA => VlaDimensions != null && VlaDimensions.Count > 0;

        public VariableDecl(string name, string type, ASTNode initializer = null, bool isArray = false, int? arraySize = null)
        {
            Name = name;
            Type = type;
            Initializer = initializer;
            IsArray = isArray;
            ArraySize = arraySize;
            Dimensions = new List<int?>();
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
    /// if 语句
    /// </summary>
    public class IfStatement : ASTNode
    {
        public ASTNode Condition { get; set; }
        public Block ThenBranch { get; set; }
        public Block ElseBranch { get; set; }

        public IfStatement(ASTNode condition, Block thenBranch, Block elseBranch = null)
        {
            Condition = condition;
            ThenBranch = thenBranch;
            ElseBranch = elseBranch;
        }
    }

    /// <summary>
    /// while 语句
    /// </summary>
    public class WhileStatement : ASTNode
    {
        public ASTNode Condition { get; set; }
        public Block Body { get; set; }

        public WhileStatement(ASTNode condition, Block body)
        {
            Condition = condition;
            Body = body;
        }
    }

    /// <summary>
    /// do-while 语句
    /// </summary>
    public class DoWhileStatement : ASTNode
    {
        public ASTNode Condition { get; set; }
        public Block Body { get; set; }

        public DoWhileStatement(ASTNode condition, Block body)
        {
            Condition = condition;
            Body = body;
        }
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
        public string Label { get; set; }
        public ASTNode Statement { get; set; }

        public LabeledStatement(string label, ASTNode statement)
        {
            Label = label;
            Statement = statement;
        }
    }

    /// <summary>
    /// for 语句
    /// </summary>
    public class ForStatement : ASTNode
    {
        public ASTNode Init { get; set; }
        public ASTNode Condition { get; set; }
        public ASTNode Update { get; set; }
        public Block Body { get; set; }

        public ForStatement(ASTNode init, ASTNode condition, ASTNode update, Block body)
        {
            Init = init;
            Condition = condition;
            Update = update;
            Body = body;
        }
    }

    /// <summary>
    /// return 语句
    /// </summary>
    public class ReturnStatement : ASTNode
    {
        public ASTNode Value { get; set; }

        public ReturnStatement(ASTNode value = null)
        {
            Value = value;
        }
    }

    /// <summary>
    /// break 语句
    /// </summary>
    public class BreakStatement : ASTNode {}

    /// <summary>
    /// continue 语句
    /// </summary>
    public class ContinueStatement : ASTNode {}

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
    /// try-catch 语句 (OS 模式)
    /// </summary>
    public class TryStatement : ASTNode
    {
        public Block Body { get; set; }
        public List<CatchClause> Catches { get; set; }

        public TryStatement(Block body, List<CatchClause> catches)
        {
            Body = body;
            Catches = catches;
        }
    }

    /// <summary>
    /// catch 子句
    /// </summary>
    public class CatchClause : ASTNode
    {
        public string? ExceptionType { get; set; }
        public string? VariableName { get; set; }
        public Block Body { get; set; }

        public CatchClause(string? exceptionType, string? variableName, Block body)
        {
            ExceptionType = exceptionType;
            VariableName = variableName;
            Body = body;
        }
    }

    /// <summary>
    /// throw 语句 (OS 模式)
    /// </summary>
    public class ThrowStatement : ASTNode
    {
        public ASTNode? Expression { get; set; }

        public ThrowStatement(ASTNode? expression = null)
        {
            Expression = expression;
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
    /// 逗号表达式 `（左, 右）`：**两边都求值**（左边求完丢弃），整个表达式的值取**右边**。
    ///
    /// ⚠ 这一条踩过，而且症状极隐蔽 —— 因为**"值"是对的**（本来就该取右边），
    /// 丢的只有**左边的副作用**。早先的解析器写的是 `expr = right;`，
    /// 把左边整个丢掉，于是左边的赋值/函数调用**根本不生成代码**。
    ///
    /// 实测：`curses.h` 的 `getmaxyx(win,y,x)` 展开是
    /// `((y) = (win)->rows, (x) = (win)->cols)` —— **标准头里这种宏遍地都是**
    /// （`getbegyx`/`getparyx` 同款）。丢弃左边之后 `y` 拿不到值，
    /// 调用方读到的**还是自己栈上的哨兵值**（实测 `-9`），
    /// 而 `x` 恰好是对的 ⇒ "同一句话里一个对、一个错"，是这类缺陷的指纹。
    ///
    /// 判据：`scripts/vml-c-probe/cases/20-curses-api.c` 的 `S`/`T`/`U` 三条。
    /// </summary>
    public class CommaExpr : ASTNode
    {
        public ASTNode Left { get; set; }
        public ASTNode Right { get; set; }

        public CommaExpr(ASTNode left, ASTNode right)
        {
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
        public bool IsPostfix { get; set; }

        public UnaryOp(string op, ASTNode operand, bool isPostfix = false)
        {
            Op = op;
            Operand = operand;
            IsPostfix = isPostfix;
        }
    }

    /// <summary>
    /// 赋值
    /// </summary>
    public class Assignment : ASTNode
    {
        public ASTNode Target { get; set; }
        public ASTNode Value { get; set; }
        public string? Op { get; set; }

        public Assignment(ASTNode target, ASTNode value, string? op = null)
        {
            Target = target;
            Value = value;
            Op = op;
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
        public object Value { get; set; }
        public string Suffix { get; set; } // 常量后缀，如 "L", "U", "UL", "F" 等

        public NumberLiteral(object value, string suffix = "")
        {
            Value = value;
            Suffix = suffix;
        }
    }

    /// <summary>
    /// 字符串字面量
    /// </summary>
    /// <summary>字符串宽度 — 8=char, 16=wchar_t, 32=char32_t</summary>
    public enum StringWidth { Char = 8, Wide = 16, Unicode = 32 }

    public class StringLiteral : ASTNode
    {
        public string Value { get; set; }
        public StringWidth Width { get; set; }

        public StringLiteral(string value, StringWidth width = StringWidth.Char)
        {
            Value = value;
            Width = width;
        }
    }

    /// <summary>
    /// 字符字面量
    /// </summary>
    public class CharLiteral : ASTNode
    {
        public char Value { get; set; }

        public CharLiteral(char value)
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
    /// 函数调用
    /// </summary>
    public class FunctionCall : ASTNode
    {
        public string Name { get; set; }
        public List<ASTNode> Args { get; set; }
        public ASTNode Callee { get; set; }
        public CallingConvention Convention { get; set; } = CallingConvention.Cdecl;

        public bool IsIndirectCall => Callee != null && !(Callee is Identifier);

        public FunctionCall() { }
        public FunctionCall(string name, List<ASTNode> args)
        {
            Name = name; Args = args;
        }
    }

    /// <summary>
    /// 数组访问
    /// </summary>
    public class ArrayAccess : ASTNode
    {
        public ASTNode Array { get; set; }
        public ASTNode Index { get; set; }
        public List<ASTNode> Indices { get; set; }

        public ArrayAccess(ASTNode array, ASTNode index)
        {
            Array = array;
            Index = index;
            Indices = new List<ASTNode> { index };
        }

        public ArrayAccess(ASTNode array, List<ASTNode> indices)
        {
            Array = array;
            Indices = indices;
            Index = indices.Count > 0 ? indices[0] : null;
        }
    }

    /// <summary>
    /// switch 语句
    /// </summary>
    public class SwitchStatement : ASTNode
    {
        public ASTNode Expression { get; set; }
        public List<CaseStatement> Cases { get; set; }
        public DefaultStatement Default { get; set; }

        public SwitchStatement(ASTNode expression, List<CaseStatement> cases, DefaultStatement defaultStmt = null)
        {
            Expression = expression;
            Cases = cases;
            Default = defaultStmt;
        }
    }

    /// <summary>
    /// case 语句
    /// </summary>
    public class CaseStatement : ASTNode
    {
        public ASTNode Value { get; set; }
        public Block Body { get; set; }

        public CaseStatement(ASTNode value, Block body)
        {
            Value = value;
            Body = body;
        }
    }

    /// <summary>
    /// default 语句
    /// </summary>
    public class DefaultStatement : ASTNode
    {
        public Block Body { get; set; }

        public DefaultStatement(Block body)
        {
            Body = body;
        }
    }

    /// <summary>
    /// 汇编指令语句
    /// </summary>
    public class AsmStatement : ASTNode
    {
        public string Code { get; set; }

        public AsmStatement(string code)
        {
            Code = code;
        }
    }

    /// <summary>
    /// 数组初始化表达式
    /// </summary>
    public class ArrayInitializer : ASTNode
    {
        public List<ASTNode> Elements { get; set; }

        public ArrayInitializer(List<ASTNode> elements)
        {
            Elements = elements;
        }
    }

    /// <summary>
    /// 条件运算符（?:）
    /// </summary>
    public class ConditionalOp : ASTNode
    {
        public ASTNode Condition { get; set; }
        public ASTNode TrueExpr { get; set; }
        public ASTNode FalseExpr { get; set; }

        public ConditionalOp(ASTNode condition, ASTNode trueExpr, ASTNode falseExpr)
        {
            Condition = condition;
            TrueExpr = trueExpr;
            FalseExpr = falseExpr;
        }
    }

    /// <summary>
    /// 类型转换表达式
    /// </summary>
    public class CastExpr : ASTNode
    {
        public string Type { get; set; }
        public ASTNode Expression { get; set; }

        public CastExpr(string type, ASTNode expression)
        {
            Type = type;
            Expression = expression;
        }
    }

    /// <summary>
    /// 类型定义 (typedef)
    /// </summary>
    public class TypeDef : ASTNode
    {
        public string Alias { get; set; }
        public string OriginalType { get; set; }

        public TypeDef(string alias, string originalType)
        {
            Alias = alias;
            OriginalType = originalType;
        }
    }

    /// <summary>
    /// 结构体成员
    /// </summary>
    public class StructMember
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsArray { get; set; }
        public int? ArraySize { get; set; }
        public List<int?> Dimensions { get; set; }
        public int Offset { get; set; } // 字节偏移
        public int PointerLevel { get; set; } // 指针级别 (0=非指针)
        public bool IsBitfield { get; set; } // 是否为位域 (C bitfield)
        public int BitWidth { get; set; }   // 位宽 (1-32)
        public int BitOffset { get; set; }  // 在存储单元中的位偏移 (0-31)

        public StructMember(string name, string type, bool isArray = false, int? arraySize = null)
        {
            Name = name;
            Type = type;
            IsArray = isArray;
            ArraySize = arraySize;
            Dimensions = new List<int?>();
            Offset = 0;
            PointerLevel = 0;
        }
    }

    /// <summary>
    /// 结构体定义
    /// </summary>
    public class StructDecl
    {
        public string Name { get; set; }
        public List<StructMember> Members { get; set; }
        public int Size { get; set; } // 总大小(字节)

        public StructDecl(string name)
        {
            Name = name;
            Members = new List<StructMember>();
            Size = 0;
        }
    }

    /// <summary>
    /// 联合体定义
    /// </summary>
    public class UnionDecl
    {
        public string Name { get; set; }
        public List<StructMember> Members { get; set; }
        public int Size { get; set; } // 总大小(字节)

        public UnionDecl(string name)
        {
            Name = name;
            Members = new List<StructMember>();
            Size = 0;
        }
    }

    /// <summary>
    /// sizeof 运算符节点
    /// </summary>
    public class SizeOfNode : ASTNode
    {
        public string TypeName { get; set; }   // sizeof(type) — 类型名字符串
        public ASTNode Expression { get; set; } // sizeof(expr) — 表达式

        public SizeOfNode(string typeName = null, ASTNode expr = null)
        {
            TypeName = typeName;
            Expression = expr;
        }
    }

    /// <summary>
    /// 成员访问 (struct.member 或 ptr->member)
    /// </summary>
    public class MemberAccess : ASTNode
    {
        public ASTNode Object { get; set; } // 结构体变量或指针
        public string MemberName { get; set; }
        public bool IsPointer { get; set; } // true = ->, false = .

        public MemberAccess(ASTNode obj, string memberName, bool isPointer = false)
        {
            Object = obj;
            MemberName = memberName;
            IsPointer = isPointer;
        }
    }
}