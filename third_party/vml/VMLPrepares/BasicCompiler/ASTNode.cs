namespace BasicCompiler
{
    /// <summary>
    /// 抽象语法树节点基类
    /// </summary>
    public abstract class ASTNode
    {
        public int Line { get; set; }
        public int Column { get; set; }

        protected ASTNode(int line, int column)
        {
            Line = line;
            Column = column;
        }
    }

    /// <summary>
    /// 程序节点
    /// </summary>
    public class BasicProgram : ASTNode
    {
        public List<Statement> Statements { get; set; }
        public Dictionary<string, string> ArrayTypes { get; set; }
        /// <summary>ENUM 成员名 → 值映射 (v1.66.32+)</summary>
        public Dictionary<string, int> EnumValues { get; set; }

        public BasicProgram(int line, int column)
            : base(line, column)
        {
            Statements = new List<Statement>();
            ArrayTypes = new Dictionary<string, string>();
            EnumValues = new Dictionary<string, int>();
        }
    }

    /// <summary>
    /// 语句节点
    /// </summary>
    public abstract class Statement : ASTNode
    {
        /// <summary>BASIC 行号 (老式行号 BASIC, 如 "10 PRINT" 的 10)；无行号时为 0</summary>
        public int BasicLineNumber { get; set; }

        protected Statement(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// 打印语句
    /// </summary>
    public class PrintStatement : Statement
    {
        public List<Expression> Expressions { get; set; }

        public PrintStatement(int line, int column)
            : base(line, column)
        {
            Expressions = new List<Expression>();
        }
    }

    /// <summary>
    /// 输入语句
    /// </summary>
    public class InputStatement : Statement
    {
        public List<Identifier> Variables { get; set; }

        public InputStatement(int line, int column)
            : base(line, column)
        {
            Variables = new List<Identifier>();
        }
    }

    /// <summary>
    /// 赋值语句
    /// </summary>
    public class LetStatement : Statement
    {
        public Expression Variable { get; set; }
        public Expression Expression { get; set; }

        public LetStatement(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// IF 语句
    /// </summary>
    public class IfStatement : Statement
    {
        public Expression Condition { get; set; }
        public Statement ThenBranch { get; set; }
        public Statement ElseBranch { get; set; }

        public IfStatement(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>Sequence of statements (for colon-separated multi-statement lines)</summary>
    public class SequenceStatement : Statement
    {
        public List<Statement> Statements { get; set; }

        public SequenceStatement(int line, int column) : base(line, column)
        {
            Statements = new List<Statement>();
        }
    }

    /// <summary>
    /// GOTO 语句
    /// </summary>
    public class GotoStatement : Statement
    {
        public int LineNumber { get; set; }
        public string Label { get; set; }
        public bool IsLabel { get; set; }

        public GotoStatement(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// GOSUB 语句
    /// </summary>
    public class GosubStatement : Statement
    {
        public int LineNumber { get; set; }
        public string Label { get; set; }
        public bool IsLabel { get; set; }

        public GosubStatement(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// RETURN 语句
    /// </summary>
    public class ReturnStatement : Statement
    {
        public ReturnStatement(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// FOR 语句
    /// </summary>
    public class ForStatement : Statement
    {
        public Identifier Variable { get; set; }
        public Expression InitialValue { get; set; }
        public Expression EndValue { get; set; }
        public Expression StepValue { get; set; }
        public List<Statement> Body { get; set; }

        public ForStatement(int line, int column)
            : base(line, column)
        {
            Body = new List<Statement>();
        }
    }

    /// <summary>
    /// WHILE 语句
    /// </summary>
    public class WhileStatement : Statement
    {
        public Expression Condition { get; set; }
        public List<Statement> Body { get; set; }

        public WhileStatement(int line, int column)
            : base(line, column)
        {
            Body = new List<Statement>();
        }
    }

    /// <summary>
    /// END 语句
    /// </summary>
    public class EndStatement : Statement
    {
        public EndStatement(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// 硬件接口语句
    /// </summary>
    public abstract class HardwareStatement : Statement
    {
        protected HardwareStatement(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// 检查键盘语句
    /// </summary>
    public class KbHitStatement : HardwareStatement
    {
        public KbHitStatement(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// 获取按键语句
    /// </summary>
    public class KbGetChStatement : HardwareStatement
    {
        public KbGetChStatement(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// 获取鼠标X坐标语句
    /// </summary>
    public class MouseGetXStatement : HardwareStatement
    {
        public MouseGetXStatement(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// 获取鼠标Y坐标语句
    /// </summary>
    public class MouseGetYStatement : HardwareStatement
    {
        public MouseGetYStatement(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// 鼠标左键语句
    /// </summary>
    public class MouseLeftStatement : HardwareStatement
    {
        public MouseLeftStatement(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// 鼠标右键语句
    /// </summary>
    public class MouseRightStatement : HardwareStatement
    {
        public MouseRightStatement(int line, int column)
            : base(line, column)
        { }
    }

    // ----- Turbo Basic 扩展语句 -----

    /// <summary>
    /// LOCAL 局部变量声明 (Turbo Basic)
    /// </summary>
    public class LocalDeclaration : Statement
    {
        public List<Identifier> Variables { get; set; }

        public LocalDeclaration(int line, int column)
            : base(line, column)
        {
            Variables = new List<Identifier>();
        }
    }

    /// <summary>
    /// STATIC 静态变量声明 (Turbo Basic)
    /// </summary>
    public class StaticDeclaration : Statement
    {
        public List<Identifier> Variables { get; set; }

        public StaticDeclaration(int line, int column)
            : base(line, column)
        {
            Variables = new List<Identifier>();
        }
    }

    /// <summary>
    /// SHARED 共享变量声明 (Turbo Basic)
    /// </summary>
    public class SharedStatement : Statement
    {
        public List<Identifier> Variables { get; set; }

        public SharedStatement(int line, int column)
            : base(line, column)
        {
            Variables = new List<Identifier>();
        }
    }

    /// <summary>
    /// COMMON 全局变量声明 (Turbo Basic)
    /// </summary>
    public class CommonStatement : Statement
    {
        public List<string> VariableNames { get; set; }

        public CommonStatement(int line, int column)
            : base(line, column)
        {
            VariableNames = new List<string>();
        }
    }

    /// <summary>
    /// OPTION BASE n 语句 (Turbo Basic)
    /// </summary>
    public class OptionBaseStatement : Statement
    {
        public int Base { get; set; }

        public OptionBaseStatement(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// 表达式节点
    /// </summary>
    public abstract class Expression : ASTNode
    {
        protected Expression(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// 标识符节点
    /// </summary>
    public class Identifier : Expression
    {
        public string Name { get; set; }

        public Identifier(int line, int column, string name)
            : base(line, column)
        {
            Name = name;
        }
    }

    /// <summary>
    /// 数字常量节点
    /// </summary>
    public class NumberLiteral : Expression
    {
        public double Value { get; set; }

        public NumberLiteral(int line, int column, double value)
            : base(line, column)
        {
            Value = value;
        }
    }

    /// <summary>
    /// 字符串常量节点
    /// </summary>
    public class StringLiteral : Expression
    {
        public string Value { get; set; }

        public StringLiteral(int line, int column, string value)
            : base(line, column)
        {
            Value = value;
        }
    }

    /// <summary>
    /// 二元表达式节点
    /// </summary>
    public class BinaryExpression : Expression
    {
        public Expression Left { get; set; }
        public string Operator { get; set; }
        public Expression Right { get; set; }

        public BinaryExpression(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// 一元表达式节点
    /// </summary>
    public class UnaryExpression : Expression
    {
        public string Operator { get; set; }
        public Expression Expression { get; set; }

        public UnaryExpression(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// DIM 语句节点
    /// </summary>
    public class DimStatement : Statement
    {
        public string VariableName { get; set; }
        public int Size { get; set; }
        public List<int> Dimensions { get; set; }
        public bool IsStringArray { get; set; }
        public string TypeName { get; set; }  // For DIM arr(size) AS TypeName
        public bool IsShared { get; set; }

        public DimStatement(int line, int column)
            : base(line, column)
        {
            Dimensions = new List<int>();
        }
    }

    /// <summary>
    /// 数组访问表达式
    /// </summary>
    public class ArrayAccessExpression : Expression
    {
        public string ArrayName { get; set; }
        public Expression Index { get; set; }
        public List<Expression> Indices { get; set; }

        public ArrayAccessExpression(int line, int column)
            : base(line, column)
        {
            Indices = new List<Expression>();
        }
    }

    /// <summary>
    /// 参数声明
    /// </summary>
    public class ParameterNode : ASTNode
    {
        public string Name { get; set; }
        public bool IsByRef { get; set; }
        public bool IsString { get; set; }

        public ParameterNode(int line, int column, string name, bool isByRef = false, bool isString = false)
            : base(line, column)
        {
            Name = name;
            IsByRef = isByRef;
            IsString = isString;
        }
    }

    /// <summary>
    /// SUB 声明
    /// </summary>
    public class SubDeclaration : Statement
    {
        public string Name { get; set; }
        public List<ParameterNode> Parameters { get; set; }
        public List<Statement> Body { get; set; }
        public bool IsStdCall { get; set; }
        public bool IsNative { get; set; }

        public SubDeclaration(int line, int column, string name)
            : base(line, column)
        {
            Name = name;
            Parameters = new List<ParameterNode>();
            Body = new List<Statement>();
        }
    }

    /// <summary>
    /// FUNCTION 声明
    /// </summary>
    public class FunctionDeclaration : Statement
    {
        public string Name { get; set; }
        public List<ParameterNode> Parameters { get; set; }
        public List<Statement> Body { get; set; }
        public bool IsStringFunction { get; set; }
        public bool IsStdCall { get; set; }
        public bool IsNative { get; set; }

        public FunctionDeclaration(int line, int column, string name)
            : base(line, column)
        {
            Name = name;
            Parameters = new List<ParameterNode>();
            Body = new List<Statement>();
        }
    }

    /// <summary>
    /// 函数调用表达式
    /// </summary>
    public class FunctionCallExpression : Expression
    {
        public string FunctionName { get; set; }
        public List<Expression> Arguments { get; set; }

        public FunctionCallExpression(int line, int column, string name)
            : base(line, column)
        {
            FunctionName = name;
            Arguments = new List<Expression>();
        }
    }

    /// <summary>
    /// CALL 语句 (调用 SUB)
    /// </summary>
    public class CallStatement : Statement
    {
        public string SubName { get; set; }
        public List<Expression> Arguments { get; set; }

        public CallStatement(int line, int column, string name)
            : base(line, column)
        {
            SubName = name;
            Arguments = new List<Expression>();
        }
    }

    /// <summary>
    /// EXIT SUB/FUNCTION 语句
    /// </summary>
    public class ExitSubStatement : Statement
    {
        public ExitSubStatement(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// EXIT FOR/WHILE 语句 (循环内跳出)
    /// </summary>
    public class ExitLoopStatement : Statement
    {
        public ExitLoopStatement(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// DO/LOOP 循环语句
    /// DO
    ///   ...body...
    /// LOOP UNTIL condition   (或 LOOP WHILE condition, 或简单 LOOP)
    /// </summary>
    public class DoLoopStatement : Statement
    {
        public List<Statement> Body { get; set; } = new List<Statement>();
        public Expression Condition { get; set; }  // null 表示简单 LOOP
        public bool IsUntil { get; set; }          // true=UNTIL, false=WHILE
        public bool HasCondition { get; set; }     // 是否有条件
        public bool IsPreTest { get; set; }        // true=DO WHILE/UNTIL, false=LOOP WHILE/UNTIL

        public DoLoopStatement(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// 文件打开模式
    /// </summary>
    public enum FileOpenMode
    {
        Input,    // 读取
        Output,   // 写入(覆盖)
        Append    // 追加
    }

    /// <summary>
    /// OPEN语句: OPEN "filename" FOR mode AS #n
    /// </summary>
    public class OpenStatement : Statement
    {
        public Expression FileName { get; set; }
        public FileOpenMode Mode { get; set; }
        public Expression FileNumber { get; set; }

        public OpenStatement(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// CLOSE语句: CLOSE #n 或 CLOSE
    /// </summary>
    public class CloseStatement : Statement
    {
        public Expression FileNumber { get; set; } // null表示关闭所有

        public CloseStatement(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// PRINT #语句: PRINT #n, expr1, expr2
    /// </summary>
    public class PrintFileStatement : Statement
    {
        public Expression FileNumber { get; set; }
        public List<Expression> Expressions { get; set; } = new List<Expression>();

        public PrintFileStatement(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// INPUT #语句: INPUT #n, var1, var2
    /// </summary>
    public class InputFileStatement : Statement
    {
        public Expression FileNumber { get; set; }
        public List<Identifier> Variables { get; set; } = new List<Identifier>();

        public InputFileStatement(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// EOF函数: EOF(n)
    /// </summary>
    public class EofExpression : Expression
    {
        public Expression FileNumber { get; set; }

        public EofExpression(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// FREEFILE函数: FREEFILE
    /// </summary>
    public class FreeFileExpression : Expression
    {
        public FreeFileExpression(int line, int column)
            : base(line, column)
        { }
    }

    /// <summary>
    /// SELECT CASE 语句
    /// </summary>
    public class SelectCaseStatement : Statement
    {
        public Expression TestExpression { get; set; }
        public List<CaseBlock> CaseBlocks { get; set; }
        public List<Statement> ElseBlock { get; set; }

        public SelectCaseStatement(int line, int column, Expression testExpr)
            : base(line, column)
        {
            TestExpression = testExpr;
            CaseBlocks = new List<CaseBlock>();
            ElseBlock = new List<Statement>();
        }
    }

    /// <summary>
    /// CASE 块
    /// </summary>
    public class CaseBlock
    {
        public List<CaseCondition> Conditions { get; set; }
        public List<Statement> Body { get; set; }

        public CaseBlock()
        {
            Conditions = new List<CaseCondition>();
            Body = new List<Statement>();
        }
    }

    /// <summary>
    /// CASE 条件
    /// </summary>
    public class CaseCondition
    {
        public enum ConditionType
        {
            Value,      // CASE 5
            Range,      // CASE 1 TO 10
            Comparison, // CASE IS > 5
            Else        // CASE ELSE
        }

        public ConditionType Type { get; set; }
        public Expression Value { get; set; }          // 用于Value类型
        public Expression FromValue { get; set; }      // 用于Range类型
        public Expression ToValue { get; set; }        // 用于Range类型
        public TokenType ComparisonOp { get; set; }    // 用于Comparison类型
        public Expression CompareValue { get; set; }   // 用于Comparison类型

        public CaseCondition()
        {
            Type = ConditionType.Value;
        }
    }

    // ==================== QBASIC 标准关键字 AST 节点 ====================

    /// <summary>SCREEN 模式设置</summary>
    public class ScreenStatement : Statement
    {
        public Expression Mode { get; set; }
        public ScreenStatement(int line, int column) : base(line, column) { }
    }

    /// <summary>PSET (x, y), color - 画点</summary>
    public class PsetStatement : Statement
    {
        public Expression X { get; set; }
        public Expression Y { get; set; }
        public Expression Color { get; set; }
        public PsetStatement(int line, int column) : base(line, column) { }
    }

    /// <summary>LINE (x1,y1)-(x2,y2), color [, B/BF] - 画线/矩形/填充矩形</summary>
    public class QbLineStatement : Statement
    {
        public Expression X1 { get; set; }
        public Expression Y1 { get; set; }
        public Expression X2 { get; set; }
        public Expression Y2 { get; set; }
        public Expression Color { get; set; }
        public bool Box { get; set; }
        public bool Fill { get; set; }
        public QbLineStatement(int line, int column) : base(line, column) { }
    }

    /// <summary>CIRCLE (x, y), radius, color, start, end, aspect - 画圆/椭圆/弧</summary>
    public class QbCircleStatement : Statement
    {
        public Expression X { get; set; }
        public Expression Y { get; set; }
        public Expression Radius { get; set; }
        public Expression Color { get; set; }
        public Expression Start { get; set; }    // start angle (radians), optional
        public Expression End { get; set; }      // end angle (radians), optional
        public Expression Aspect { get; set; }   // aspect ratio (y/x), optional, default 1.0
        public bool HasStart { get; set; }
        public bool HasEnd { get; set; }
        public bool HasAspect { get; set; }
        public QbCircleStatement(int line, int column) : base(line, column) { }
    }

    /// <summary>PAINT (x, y), color [, border] - 填充</summary>
    public class QbPaintStatement : Statement
    {
        public Expression X { get; set; }
        public Expression Y { get; set; }
        public Expression Color { get; set; }
        public Expression Border { get; set; }
        public bool HasBorder { get; set; }
        public QbPaintStatement(int line, int column) : base(line, column) { }
    }

    /// <summary>LOCATE row, col - 定位光标</summary>
    public class LocateStatement : Statement
    {
        public Expression Row { get; set; }
        public Expression Col { get; set; }
        public LocateStatement(int line, int column) : base(line, column) { }
    }

    /// <summary>COLOR fg [, bg] - 设置颜色</summary>
    public class QbColorStatement : Statement
    {
        public Expression Foreground { get; set; }
        public Expression Background { get; set; }
        public bool HasBackground { get; set; }
        public QbColorStatement(int line, int column) : base(line, column) { }
    }

    /// <summary>CLS - clear screen</summary>
    public class ClsStatement : Statement
    {
        public ClsStatement(int line, int column) : base(line, column) { }
    }

    /// <summary>INKEY$ - 键盘输入（作为函数表达式）</summary>
    public class InkeyExpression : Expression
    {
        public InkeyExpression(int line, int column) : base(line, column) { }
    }

    /// <summary>RANDOMIZE [seed] - 随机数种子</summary>
    public class RandomizeStatement : Statement
    {
        public Expression Seed { get; set; }
        public bool HasSeed { get; set; }
        public RandomizeStatement(int line, int column) : base(line, column) { }
    }

    /// <summary>WIDTH cols, rows - 设置屏幕尺寸</summary>
    public class QbWidthStatement : Statement
    {
        public Expression Cols { get; set; }
        public Expression Rows { get; set; }
        public QbWidthStatement(int line, int column) : base(line, column) { }
    }

    /// <summary>BEEP - 扬声器</summary>
    public class BeepStatement : Statement
    {
        public BeepStatement(int line, int column) : base(line, column) { }
    }

    /// <summary>SLEEP [seconds] - 暂停</summary>
    public class SleepStatement : Statement
    {
        public Expression Seconds { get; set; }
        public bool HasSeconds { get; set; }
        public SleepStatement(int line, int column) : base(line, column) { }
    }

    /// <summary>SWAP var1, var2 - 变量交换</summary>
    public class SwapStatement : Statement
    {
        public Expression Var1 { get; set; }
        public Expression Var2 { get; set; }
        public SwapStatement(int line, int column) : base(line, column) { }
    }

    /// <summary>ERASE array - 清除数组</summary>
    public class EraseStatement : Statement
    {
        public string ArrayName { get; set; }
        public EraseStatement(int line, int column) : base(line, column) { }
    }

    /// <summary>TIMER 函数 - 返回秒数（作为表达式）</summary>
    public class TimerFunctionExpression : Expression
    {
        public TimerFunctionExpression(int line, int column) : base(line, column) { }
    }

    /// <summary>DATE$ 函数 - 返回日期字符串（作为表达式）</summary>
    public class DateFunctionExpression : Expression
    {
        public DateFunctionExpression(int line, int column) : base(line, column) { }
    }

    /// <summary>TIME$ 函数 - 返回时间字符串（作为表达式）</summary>
    public class TimeFunctionExpression : Expression
    {
        public TimeFunctionExpression(int line, int column) : base(line, column) { }
    }

    /// <summary>SYSTEM [exitcode] - 退出程序</summary>
    public class SystemStatement : Statement
    {
        public Expression ExitCode { get; set; }
        public SystemStatement(int line, int column) : base(line, column) { }
    }

    /// <summary>DEFINT/DEFSNG/DEFSTR - 类型声明</summary>
    public class DefTypeStatement : Statement
    {
        public string TypeName { get; set; }
        /// <summary>字母范围列表: (startLetter, endLetter), endLetter='\0' 表示单字母</summary>
        public List<(char Start, char End)> Ranges { get; set; } = new List<(char, char)>();
        public DefTypeStatement(int line, int column) : base(line, column) { }
    }

    /// <summary>LPRINT - 打印机输出</summary>
    public class LPrintStatement : PrintStatement
    {
        public LPrintStatement(int line, int column) : base(line, column) { }
    }

    // ==================== DATA/READ/RESTORE ====================

    /// <summary>DATA val1, val2, ... - 存储数据常量</summary>
    public class DataStatement : Statement
    {
        public List<Expression> Values { get; set; }
        public DataStatement(int line, int column) : base(line, column)
        {
            Values = new List<Expression>();
        }
    }

    /// <summary>READ var1, var2, ... - 从数据区读取到变量</summary>
    public class ReadStatement : Statement
    {
        /// <summary>
        /// READ 的目标列表。元素是 <see cref="Identifier"/>（简单变量 / 整个数组）
        /// 或 <see cref="ArrayAccessExpression"/>（数组元素，`READ a(i)`）。
        ///
        /// <para>
        /// ⚠ 从前这里是 `List&lt;Identifier&gt;`，于是 `READ a(i)` 只吃下 `a`、
        /// 把 `(i)` 留在 token 流上 —— `i` 落到「名字后面不是 `=`」的兜底分支、
        /// 编成 `CALL func_i`，链接期报「未定义的函数 'func_i'」（数值全写进 a(0)）。
        /// GORILLA.BAS 的香蕉位图加载（`FOR i = 0 TO 8 / READ LBan&amp;(i) / NEXT i`）
        /// 就是这个形状，8 个循环正好 8 次报错。
        /// </para>
        /// </summary>
        public List<Expression> Variables { get; set; }
        public ReadStatement(int line, int column) : base(line, column)
        {
            Variables = new List<Expression>();
        }
    }

    /// <summary>RESTORE - 重置数据指针</summary>
    public class RestoreStatement : Statement
    {
        public RestoreStatement(int line, int column) : base(line, column) { }
    }

    // ==================== CONST ====================

    /// <summary>CONST name = value - 常量声明</summary>
    public class ConstStatement : Statement
    {
        public string Name { get; set; }
        public Expression Value { get; set; }
        public ConstStatement(int line, int column) : base(line, column) { }
    }

    // ==================== ON ERROR GOTO / RESUME ====================

    /// <summary>ON ERROR GOTO label / ON ERROR GOTO 0 / ON ERROR RESUME NEXT</summary>
    public class OnErrorStatement : Statement
    {
        public string ErrorHandlerLabel { get; set; }
        public int ErrorHandlerLine { get; set; }
        public bool DisableHandler { get; set; }
        public bool ResumeNext { get; set; }
        public OnErrorStatement(int line, int column) : base(line, column) { }
    }

    /// <summary>RESUME / RESUME NEXT / RESUME 0</summary>
    public class ResumeStatement : Statement
    {
        public bool ResumeNext { get; set; }
        public ResumeStatement(int line, int column) : base(line, column) { }
    }

    // ==================== DRAW 语句 ====================

    /// <summary>DRAW "U10 R10 D10 L10" - 图形绘制宏</summary>
    public class DrawStatement : Statement
    {
        public Expression DrawString { get; set; }
        public DrawStatement(int line, int column) : base(line, column) { }
    }

    // ==================== PRINT USING 语句 ====================

    /// <summary>PRINT USING "##.##"; value1; value2</summary>
    public class PrintUsingStatement : Statement
    {
        public Expression Format { get; set; }
        public List<Expression> Values { get; set; }

        public PrintUsingStatement(int line, int column) : base(line, column)
        {
            Values = new List<Expression>();
        }
    }

    // ==================== TYPE/END TYPE ====================

    /// <summary>TYPE 声明中的字段</summary>
    public class TypeField : ASTNode
    {
        public string Name { get; set; }
        public string FieldType { get; set; }  // "INTEGER", "SINGLE", "STRING"
        public int StringLength { get; set; }  // for STRING * n
        public int Offset { get; set; }        // byte offset within struct

        public TypeField(int line, int column) : base(line, column) { }
    }

    /// <summary>TYPE name ... END TYPE - 类型声明</summary>
    public class TypeDeclaration : Statement
    {
        public string Name { get; set; }
        public List<TypeField> Fields { get; set; }
        public int TotalSize { get; set; }  // total bytes

        public TypeDeclaration(int line, int column) : base(line, column)
        {
            Fields = new List<TypeField>();
        }
    }

    /// <summary>CLASS 方法声明 (FreeBasic OOP v1.66.31+)</summary>
    public class MethodDeclaration : Statement
    {
        public string Name { get; set; }
        public List<string> Parameters { get; set; }
        public List<Statement> Body { get; set; }
        public bool IsNative { get; set; }

        public MethodDeclaration(int line, int column) : base(line, column)
        {
            Parameters = new List<string>();
            Body = new List<Statement>();
        }
    }

    /// <summary>CLASS name ... END CLASS — 类声明 (FreeBasic OOP v1.66.31+)</summary>
    public class ClassDeclaration : Statement
    {
        public string Name { get; set; }
        public List<TypeField> Fields { get; set; }
        public List<MethodDeclaration> Methods { get; set; }
        public List<Statement>? ConstructorBody { get; set; }
        public List<Statement>? DestructorBody { get; set; }

        public ClassDeclaration(int line, int column) : base(line, column)
        {
            Fields = new List<TypeField>();
            Methods = new List<MethodDeclaration>();
        }
    }

    /// <summary>DIM var AS TypeName - 自定义类型变量声明</summary>
    public class DimAsStatement : Statement
    {
        public string VariableName { get; set; }
        public string TypeName { get; set; }

        public DimAsStatement(int line, int column) : base(line, column) { }
    }

    /// <summary>record.field - 字段访问表达式</summary>
    public class FieldAccessExpression : Expression
    {
        public string RecordName { get; set; }
        public string FieldName { get; set; }
        /// <summary>非标识符的记录表达式（如数组访问 arr(1).field），为null时使用RecordName</summary>
        public Expression RecordExpression { get; set; }

        public FieldAccessExpression(int line, int column) : base(line, column) { }
    }

    // ==================== PLAY/SOUND ====================

    /// <summary>PLAY command string - 播放音乐</summary>
    public class PlayStatement : Statement
    {
        public Expression CommandString { get; set; }
        public PlayStatement(int line, int column) : base(line, column) { }
    }

    /// <summary>SOUND frequency, duration - 发声</summary>
    public class SoundStatement : Statement
    {
        public Expression Frequency { get; set; }
        public Expression Duration { get; set; }
        public SoundStatement(int line, int column) : base(line, column) { }
    }

    // ==================== REDIM/PRESERVE ====================

    /// <summary>REDIM array(newsize) [/ REDIM PRESERVE array(newsize)]</summary>
    public class RedimStatement : Statement
    {
        public string ArrayName { get; set; }
        public Expression NewSize { get; set; }
        public bool Preserve { get; set; }
        public RedimStatement(int line, int column) : base(line, column) { }
    }

    // ==================== DEF FN ====================

    /// <summary>DEF FNname(param) = expression (单行)</summary>
    public class DefFnStatement : Statement
    {
        public string FunctionName { get; set; }  // 不含 FN 前缀
        public List<ParameterNode> Parameters { get; set; }
        public Expression BodyExpression { get; set; }
        public string FullFnName { get; set; }  // 含 FN 前缀的完整名

        public DefFnStatement(int line, int column) : base(line, column)
        {
            Parameters = new List<ParameterNode>();
        }
    }

    /// <summary>FNname(args) 调用表达式</summary>
    public class FnCallExpression : Expression
    {
        public string FunctionName { get; set; }  // 不含 FN 前缀
        public List<Expression> Arguments { get; set; }

        public FnCallExpression(int line, int column, string name) : base(line, column)
        {
            FunctionName = name;
            Arguments = new List<Expression>();
        }
    }

    // ==================== GET/PUT (Graphics Array) ====================

    /// <summary>GET (x1,y1)-(x2,y2), array - 保存图形区域到数组</summary>
    public class GetStatement : Statement
    {
        public Expression X1 { get; set; }
        public Expression Y1 { get; set; }
        public Expression X2 { get; set; }
        public Expression Y2 { get; set; }
        public string ArrayName { get; set; }

        public GetStatement(int line, int column) : base(line, column) { }
    }

    /// <summary>PUT (x,y), array, action - 从数组恢复图形区域</summary>
    public class PutStatement : Statement
    {
        public Expression X { get; set; }
        public Expression Y { get; set; }
        public string ArrayName { get; set; }
        public string Action { get; set; } // PSET, PRESET, AND, OR, XOR

        public PutStatement(int line, int column) : base(line, column) { }
    }

    // ==================== PALETTE ====================

    /// <summary>PALETTE color_index, red, green, blue (0-63 each)</summary>
    public class PaletteStatement : Statement
    {
        public Expression ColorIndex { get; set; }
        public Expression Red { get; set; }
        public Expression Green { get; set; }
        public Expression Blue { get; set; }

        public PaletteStatement(int line, int column) : base(line, column) { }
    }

    // ==================== POKE / CHIPASM ====================

    /// <summary>POKE address, value — 写内存</summary>
    public class PokeStatement : Statement
    {
        public Expression Address { get; set; }
        public Expression Value { get; set; }
        public PokeStatement(Expression addr, Expression val, int line = 0, int col = 0) : base(line, col)
        {
            Address = addr; Value = val;
        }
    }

    /// <summary>CHIPASM("arch", "code") — 内联架构汇编</summary>
    public class ChipAsmStatement : Statement
    {
        public string Arch { get; set; }
        public string Code { get; set; }
        public ChipAsmStatement(string arch, string code, int line = 0, int col = 0) : base(line, col)
        {
            Arch = arch; Code = code;
        }
    }

    // AsmStatement 已移除 — asm() 仅限 C/ObjC/C++ 语言
    // BASIC 通过 Lib/shared/vmlsys.c 调用系统功能

    /// <summary>行标签: LabelName:</summary>
    public class LabelStatement : Statement
    {
        public string Name { get; set; }
        public Statement Body { get; set; }
        public LabelStatement(int line, int column, string name, Statement body) : base(line, column)
        {
            Name = name;
            Body = body;
        }
    }

    /// <summary>堆分配语句: NEW type (v1.66.32+)</summary>
    public class AllocStatement : Statement
    {
        public AllocStatement(int line, int column) : base(line, column) { }
    }

    /// <summary>ChipBasic GPIO 语句: PINMODE/DIGITALWRITE/DIGITALREAD (v1.66.32+)</summary>
    public class GpioStatement : Statement
    {
        public string Function { get; set; }
        public List<object> Arguments { get; set; }
        public GpioStatement(int line, int column) : base(line, column) { Arguments = new List<object>(); }
    }
}
