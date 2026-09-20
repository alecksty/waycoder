namespace SchemeCompiler;
/// <summary>
/// S 表达式基类。
///
/// 位置（Line/Column）由 `Parser.ParseExpr` 这个**唯一入口**
/// 盖上（与 Python 端的 `ASTNode.Accept` 同一思路）：
/// 每个 S 表达式都是从那里产出的，
/// 写在那一处就自然覆盖全部构造点。
/// </summary>
public class SExpr {
    /// <summary>源码行号（1 基）；**0 = 未知**。</summary>
    public int Line { get; set; }
    /// <summary>源码列号（1 基）；**0 = 未知**。</summary>
    public int Column { get; set; }
}
public class SInt(int v) : SExpr { public int Value => v; }
public class SDouble(double v) : SExpr { public double Value => v; }
public class SSym(string n) : SExpr { public string Name => n; }
public class SStr(string v) : SExpr { public string Value => v; }
public class SBool(bool v) : SExpr { public bool Value => v; }
public class SList(List<SExpr> items) : SExpr { public List<SExpr> Items => items; }
