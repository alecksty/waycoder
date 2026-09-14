namespace SchemeCompiler;
public class SExpr { }
public class SInt(int v) : SExpr { public int Value => v; }
public class SDouble(double v) : SExpr { public double Value => v; }
public class SSym(string n) : SExpr { public string Name => n; }
public class SStr(string v) : SExpr { public string Value => v; }
public class SBool(bool v) : SExpr { public bool Value => v; }
public class SList(List<SExpr> items) : SExpr { public List<SExpr> Items => items; }
