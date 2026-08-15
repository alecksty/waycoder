// =============================================================
// Ast.cs —— 抽象语法树节点
//
// 解析器产出表达式与语句节点。全部为类节点，AOT 友好（无反射）。
// =============================================================

namespace QBasic.Compiler;

/// <summary>表达式节点。</summary>
public class Expr
{
    public ExprKind Kind;
    public double Num;
    public string Str = "";
    public string VarName = "";
    public bool IsStrVar;
    public string Op = "";
    public Expr? Left, Right;
    public Expr? Index;
    /// <summary>数组下标列表（支持多维数组；单维时长度为 1）。</summary>
    public List<Expr>? Indexes;
    public string FuncName = "";
    public List<Expr>? Args;
    /// <summary>整数组引用（如 CALL foo(arr())），无下标。</summary>
    public bool WholeArray;

    public static Expr NumLit(double v) => new() { Kind = ExprKind.NumLit, Num = v };
    public static Expr StrLit(string v) => new() { Kind = ExprKind.StrLit, Str = v };
    public static Expr Var(string name, bool isStr) => new() { Kind = ExprKind.Var, VarName = name, IsStrVar = isStr };
    public static Expr ArrayRef(string name, Expr idx, bool isStr) => new() { Kind = ExprKind.ArrayRef, VarName = name, Index = idx, IsStrVar = isStr, Indexes = new List<Expr> { idx } };
    public static Expr Unary(string op, Expr operand) => new() { Kind = ExprKind.Unary, Op = op, Left = operand };
    public static Expr Binary(string op, Expr l, Expr r) => new() { Kind = ExprKind.Binary, Op = op, Left = l, Right = r };
    public static Expr Call(string name, List<Expr> args) => new() { Kind = ExprKind.FuncCall, FuncName = name, Args = args };

    /// <summary>去掉 QBasic 类型后缀（$ # ! % &），用于函数名归一化匹配（如声明 CalcDelay! 调用 CalcDelay）。</summary>
    public static string StripTypeSuffix(string name)
    {
        if (name.Length == 0) return name;
        char c = name[^1];
        return c is '$' or '#' or '!' or '%' or '&' ? name[..^1] : name;
    }

    /// <summary>递归把变量引用 from 改名为 to（DEF FN 参数隔离：避免与调用处同名局部变量冲突）。</summary>
    public static Expr RenameVar(Expr e, string from, string to)
    {
        if (e.Kind == ExprKind.Var && string.Equals(e.VarName, from, StringComparison.OrdinalIgnoreCase))
            return Var(to, e.IsStrVar);
        if (e.Kind == ExprKind.ArrayRef && string.Equals(e.VarName, from, StringComparison.OrdinalIgnoreCase))
        {
            var r = ArrayRef(to, e.Index!, e.IsStrVar);
            r.Indexes = e.Indexes;
            return r;
        }
        if (e.Left != null) e.Left = RenameVar(e.Left, from, to);
        if (e.Right != null) e.Right = RenameVar(e.Right, from, to);
        if (e.Index != null) e.Index = RenameVar(e.Index, from, to);
        if (e.Indexes != null) for (int i = 0; i < e.Indexes.Count; i++) e.Indexes[i] = RenameVar(e.Indexes[i]!, from, to);
        if (e.Args != null) for (int i = 0; i < e.Args.Count; i++) e.Args[i] = RenameVar(e.Args[i]!, from, to);
        return e;
    }
}

/// <summary>表达式种类。</summary>
public enum ExprKind
{
    NumLit, StrLit, Var, ArrayRef, Unary, Binary, FuncCall,
}

/// <summary>语句节点。</summary>
public class Stmt
{
    public StmtKind Kind;
    public int Line;
    public string VarName = "";
    public bool IsStrVar;
    public Expr? Index;
    /// <summary>数组赋值下标列表（多维时长度 > 1）。</summary>
    public List<Expr>? Indexes;
    public bool IsArray;
    public Expr? Value;
    public List<PrintItem>? PrintItems;
    public Expr? Cond;
    public List<Stmt> ThenStmts = new();
    public List<Stmt> ElseStmts = new();
    public List<Stmt>? SingleLineThen;
    public List<Stmt>? SingleLineElseStmts;
    public string ForVar = "";
    public Expr? From, To, Step;
    public List<Stmt> Body = new();
    public string Target = "";
    public double TargetNum;
    public bool TargetIsNum;
    public List<string> DimVars = new();
    public List<Expr>? DimSizes;
    /// <summary>每个 DIM 变量的维度尺寸列表（支持二维数组）。</summary>
    public List<List<Expr>> DimDims = new();
    /// <summary>每个 DIM 变量的每维下界（TO 语法；null 表示 0 基）。</summary>
    public List<List<Expr?>> DimLowers = new();
    /// <summary>每个 DIM 变量的 AS 类型名（用户类型或内置类型）。</summary>
    public List<string> DimType = new();
    public string LabelName = "";
    public double LabelNum;
    public bool LabelIsNum;
    /// <summary>标签处的 DATA 偏移（供 RESTORE label 使用）。</summary>
    public int LabelDataIdx = -1;

    // ---- SELECT CASE ----
    public Expr? SelectExpr;
    public List<CaseClause>? Cases;

    // ---- DO ... LOOP ----
    public Expr? DoCond;
    public bool DoUntil;
    public bool DoCondAfter; // true 表示条件在 LOOP 之后（后置）

    // ---- READ 多变量 ----
    public List<string>? ReadVars;
    public List<bool>? ReadIsStr;
    /// <summary>READ 到数组元素时的下标（无则 null）。</summary>
    public List<Expr?>? ReadIndexes;

    // ---- 图形语句 ----
    public int GfxMode;              // SCREEN 模式 / LINE B|BF 标志 / PUT 模式
    public bool Fill;                // LINE BF
    public Expr? X1, Y1, X2, Y2;     // LINE / GET 坐标
    public Expr? ColorExpr;          // LINE/CIRCLE/PSET/PAINT 颜色
    public Expr? Radius;             // CIRCLE 半径
    public Expr? StartAngle, EndAngle, Aspect; // CIRCLE 弧参数
    public Expr? Row, Col;           // LOCATE 行列 / COLOR fg
    public Expr? Fg, Bg;             // COLOR fg,bg
    public string SpriteVar = "";    // GET/PUT 数组名
    public Expr? PutX, PutY;         // PUT 坐标
    public bool SpriteXor, SpritePset; // PUT XOR / PSET
    public string PlayStr = "";      // PLAY 音乐串
    public Expr? SleepSec;           // SLEEP
    public string CallName = "";     // SUB 调用名
    public List<Expr>? CallArgs;     // SUB 调用实参
    public List<bool>? CallArgIsArray; // 实参是否为整数组（BCoor()）
    public string ErrLabel = "";     // ON ERROR 目标标签
    public bool ErrZero;             // ON ERROR GOTO 0
    public int ResumeMode;           // 0=RESUME 1=RESUME NEXT
    public int TextWidth;            // WIDTH 列数
}

/// <summary>SUB/FUNCTION 例程定义。</summary>
public class Routine
{
    public string Name = "";
    public bool IsFunction;
    /// <summary>返回变量名（函数名，含类型后缀）。</summary>
    public string ReturnVar = "";
    public List<Param> Params = new();
    public List<Stmt> Body = new();
}

/// <summary>例程参数。</summary>
public class Param
{
    public string Name = "";      // 参数名（含类型后缀）
    public bool IsArray;          // 数组参数（BCoor()）
    public string Type = "";      // AS 类型（XYPoint / ANY / SINGLE / INTEGER...）
}

/// <summary>DEF FN 单行函数。</summary>
public class DefFn
{
    public string Name = "";
    public string Param = "";
    public Expr Body = null!;
}

/// <summary>TYPE 用户自定义类型。</summary>
public class UserType
{
    public string Name = "";
    public List<string> Fields = new();
}

/// <summary>语句种类。</summary>
public enum StmtKind
{
    Let, Print, Input, If, For, While, Goto, Gosub, Return, Rem, End, Dim, Label,
    SelectCase, DoLoop, Randomize, Read, Restore,
    // ---- 图形 / 交互 ----
    Screen, Cls, Line, Circle, Pset, Paint, Color, Palette, Locate,
    GetSprite, PutSprite, Play, Sleep, SubCall, OnError, Resume, Width,
    LineInput, Beep,
}

/// <summary>SELECT CASE 的一个分支。</summary>
public class CaseClause
{
    public bool IsElse;
    /// <summary>CASE 1, 2 ... 的匹配值列表。</summary>
    public List<Expr> Values = new();
    /// <summary>CASE IS op value 的比较条件列表。</summary>
    public List<(string Op, Expr Value)> Conds = new();
    /// <summary>CASE lo TO hi 范围列表。</summary>
    public List<(Expr Lo, Expr Hi)> Ranges = new();
    public List<Stmt> Body = new();
}

/// <summary>DATA 语句中的一个数据项（数字或字符串）。</summary>
public class DataItem
{
    public bool IsStr;
    public double Num;
    public string Str = "";
}

/// <summary>PRINT 的一个输出片段。</summary>
public class PrintItem
{
    public Expr? Expr;
    public bool IsNewline;
    public bool IsEmpty;
    /// <summary>分隔符：0 无、';' 分号、',' 逗号。</summary>
    public char Separator;
    /// <summary>TAB(n) 制表项。</summary>
    public Expr? TabCol;
}
