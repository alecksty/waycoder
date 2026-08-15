// =============================================================
// Parser.cs —— 语法分析器
//
// 把 Token 序列解析为语句列表。递归下降 + 优先级爬升。
// 支持原有语句，并扩展了 GORILLA.BAS 所需的全部语法：
//   - SUB/FUNCTION 例程（参数、AS ANY 数组参数、局部变量、STATIC）
//   - DECLARE / TYPE / CONST / DEFINT / DEF FN / REDIM / DIM SHARED
//   - 类型后缀 $ % & ! #；DIM (lo TO hi)；CASE lo TO hi 范围
//   - 图形语句 SCREEN/CLS/LINE/CIRCLE/PSET/PAINT/COLOR/PALETTE/LOCATE/GET/PUT
//   - 交互 PLAY/SLEEP/BEEP/WIDTH/INKEY$/LINE INPUT；ON ERROR GOTO/RESUME
//   - 运算符：+ - * / \ ^ MOD AND OR NOT = <> < > <= >= ；字段访问 arr(i).field
// =============================================================

namespace QBasic.Compiler;

/// <summary>解析错误。</summary>
public class ParseException : Exception
{
    public int Line;
    public ParseException(string msg, int line) : base(msg) { Line = line; }
}

/// <summary>语法分析器。</summary>
public sealed class Parser
{
    private readonly List<Token> _toks;
    private int _pos;

    /// <summary>解析过程中收集的 DATA 数据项（按出现顺序）。</summary>
    public List<DataItem> Data { get; } = new();
    /// <summary>收集到的 SUB/FUNCTION 例程。</summary>
    public List<Routine> Routines { get; } = new();
    /// <summary>常量表（名称 → 数值）。</summary>
    public Dictionary<string, double> Consts { get; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>用户自定义类型表。</summary>
    public Dictionary<string, UserType> Types { get; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>DEF FN 单行函数表。</summary>
    public Dictionary<string, DefFn> DefFns { get; } = new(StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<string> _subNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _funcNames = new(StringComparer.OrdinalIgnoreCase);

    public Parser(List<Token> tokens) { _toks = tokens; }

    private Token Cur => _toks[_pos];
    private Token Peek(int n = 1) => _pos + n < _toks.Count ? _toks[_pos + n] : _toks[^1];

    private Token Advance() { var t = _toks[_pos]; if (_pos < _toks.Count - 1) _pos++; return t; }

    private bool IsEof => Cur.Type == TokenType.Eof;
    private bool IsOp(string op) => Cur.Type == TokenType.Op && Cur.Text == op;

    private bool MatchOp(string op)
    {
        if (IsOp(op)) { Advance(); return true; }
        return false;
    }

    private bool MatchKeyword(string kw)
    {
        if (Cur.Type == TokenType.Ident && Cur.Text.Equals(kw, StringComparison.OrdinalIgnoreCase)) { Advance(); return true; }
        return false;
    }

    private bool IsKeyword(string kw) =>
        Cur.Type == TokenType.Ident && Cur.Text.Equals(kw, StringComparison.OrdinalIgnoreCase);

    /// <summary>判断 name 是否为用户自定义 FUNCTION（忽略类型后缀 $#!%&，如声明 CalcDelay! 调用 CalcDelay）。</summary>
    private bool IsUserFunc(string name)
    {
        string n = Expr.StripTypeSuffix(name);
        foreach (var f in _funcNames)
            if (string.Equals(Expr.StripTypeSuffix(f), n, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>预扫描：收集 SUB/FUNCTION 名称（无论声明顺序）。</summary>
    private void Prescan()
    {
        for (int i = 0; i < _toks.Count - 1; i++)
        {
            var t = _toks[i];
            if (t.Type != TokenType.Ident) continue;
            string up = t.Text.ToUpperInvariant();
            if (up == "SUB" && _toks[i + 1].Type == TokenType.Ident) _subNames.Add(_toks[i + 1].Text);
            else if (up == "FUNCTION" && _toks[i + 1].Type == TokenType.Ident) _funcNames.Add(_toks[i + 1].Text);
            else if (up == "DECLARE" && _toks[i + 1].Type == TokenType.Ident &&
                     _toks[i + 2].Type == TokenType.Ident)
            {
                string k2 = _toks[i + 1].Text.ToUpperInvariant();
                if (k2 == "SUB") _subNames.Add(_toks[i + 2].Text);
                else if (k2 == "FUNCTION") _funcNames.Add(_toks[i + 2].Text);
            }
        }
    }

    /// <summary>解析整个程序：主语句 + 例程。</summary>
    public List<Stmt> ParseProgram()
    {
        Prescan();
        var stmts = new List<Stmt>();
        while (!IsEof)
        {
            SkipNewlines();
            if (IsEof) break;
            if (IsKeyword("SUB")) { ParseRoutine(false); continue; }
            if (IsKeyword("FUNCTION")) { ParseRoutine(true); continue; }
            stmts.Add(ParseStatement());
            SkipNewlines();
            if (IsOp(":")) { Advance(); continue; }
        }
        return stmts;
    }

    private void SkipNewlines()
    {
        while (Cur.Type == TokenType.Newline) Advance();
    }

    private void ParseRoutine(bool isFunction)
    {
        int line = Cur.Line;
        Advance(); // SUB / FUNCTION
        string name = ExpectIdent();
        var r = new Routine { Name = name, IsFunction = isFunction, ReturnVar = name };
        if (MatchOp("("))
        {
            while (!IsEof && !IsOp(")"))
            {
                var p = new Param { Name = ExpectIdent() };
                if (MatchOp("(")) { p.IsArray = true; ExpectOp(")"); }
                if (MatchKeyword("AS")) p.Type = ExpectIdent();
                r.Params.Add(p);
                if (!MatchOp(",")) break;
            }
            ExpectOp(")");
        }
        MatchKeyword("STATIC");
        SkipNewlines();
        while (!IsEof)
        {
            if (IsKeyword("END") && Peek().Type == TokenType.Ident &&
                (Peek().Text.Equals("SUB", StringComparison.OrdinalIgnoreCase) || Peek().Text.Equals("FUNCTION", StringComparison.OrdinalIgnoreCase)))
                break;
            r.Body.Add(ParseStatement());
            SkipNewlines();
            if (IsOp(":")) { Advance(); continue; }
        }
        if (IsKeyword("END")) { Advance(); Advance(); }
        Routines.Add(r);
    }

    private Stmt ParseStatement()
    {
        int line = Cur.Line;
        if (Cur.Type == TokenType.LineNum)
        {
            string text = Cur.Text;
            double num = Cur.Num;
            Advance();
            return new Stmt { Kind = StmtKind.Label, Line = line, LabelName = text, LabelNum = num, LabelIsNum = true, LabelDataIdx = Data.Count };
        }
        if (Cur.Type == TokenType.Ident && Peek().Type == TokenType.Op && Peek().Text == ":")
        {
            string name = Advance().Text;
            Advance(); // ':'
            return new Stmt { Kind = StmtKind.Label, Line = line, LabelName = name, LabelIsNum = false, LabelDataIdx = Data.Count };
        }

        if (IsKeyword("REM"))
        {
            while (Cur.Type != TokenType.Newline && !IsEof) Advance();
            return new Stmt { Kind = StmtKind.Rem, Line = line };
        }
        if (IsKeyword("PRINT")) return ParsePrint(line);
        if (IsKeyword("INPUT")) return ParseInput(line);
        if (IsKeyword("LINE"))
        {
            if (Peek().Type == TokenType.Ident && Peek().Text.Equals("INPUT", StringComparison.OrdinalIgnoreCase))
                return ParseLineInput(line);
            return ParseLine(line);
        }
        if (IsKeyword("SCREEN")) return ParseScreen(line);
        if (IsKeyword("CLS")) { Advance(); if (Cur.Type == TokenType.Number) Advance(); return new Stmt { Kind = StmtKind.Cls, Line = line }; }
        if (IsKeyword("CIRCLE")) return ParseCircle(line);
        if (IsKeyword("PSET")) return ParsePset(line);
        if (IsKeyword("PAINT")) return ParsePaint(line);
        if (IsKeyword("COLOR")) return ParseColor(line);
        if (IsKeyword("PALETTE")) return ParsePalette(line);
        if (IsKeyword("LOCATE")) return ParseLocate(line);
        if (IsKeyword("GET")) return ParseGet(line);
        if (IsKeyword("PUT")) return ParsePut(line);
        if (IsKeyword("PLAY")) return ParsePlay(line);
        if (IsKeyword("SLEEP")) { Advance(); return new Stmt { Kind = StmtKind.Sleep, Line = line, SleepSec = ParseExpr() }; }
        if (IsKeyword("BEEP")) { Advance(); return new Stmt { Kind = StmtKind.Beep, Line = line }; }
        if (IsKeyword("WIDTH")) return ParseWidth(line);
        if (IsKeyword("CALL")) return ParseCall(line);
        if (IsKeyword("ON")) return ParseOnError(line);
        if (IsKeyword("RESUME")) { Advance(); int m = 0; if (MatchKeyword("NEXT")) m = 1; return new Stmt { Kind = StmtKind.Resume, Line = line, ResumeMode = m }; }
        if (IsKeyword("DECLARE")) { while (Cur.Type != TokenType.Newline && !IsEof) Advance(); return new Stmt { Kind = StmtKind.Rem, Line = line }; }
        if (IsKeyword("TYPE")) return ParseType(line);
        if (IsKeyword("CONST")) return ParseConst(line);
        if (IsKeyword("DEF")) return ParseDef(line);
        if (IsKeyword("DEFINT") || IsKeyword("DEFSNG") || IsKeyword("DEFDBL") || IsKeyword("DEFSTR"))
        { while (Cur.Type != TokenType.Newline && !IsEof) Advance(); return new Stmt { Kind = StmtKind.Rem, Line = line }; }
        if (IsKeyword("REDIM") || IsKeyword("DIM")) return ParseDim(line);
        if (IsKeyword("LET")) { Advance(); return ParseLet(line); }
        if (IsKeyword("IF")) return ParseIf(line);
        if (IsKeyword("FOR")) return ParseFor(line);
        if (IsKeyword("NEXT")) { Advance(); return new Stmt { Kind = StmtKind.Rem, Line = line }; }
        if (IsKeyword("WHILE")) return ParseWhile(line);
        if (IsKeyword("WEND")) { Advance(); return new Stmt { Kind = StmtKind.Rem, Line = line }; }
        if (IsKeyword("GOTO")) { Advance(); return ParseGoto(line, false); }
        if (IsKeyword("GOSUB")) { Advance(); return ParseGoto(line, true); }
        if (IsKeyword("RETURN")) { Advance(); return new Stmt { Kind = StmtKind.Return, Line = line }; }
        if (IsKeyword("END"))
        {
            if (Peek().Type == TokenType.Ident && Peek().Text.Equals("IF", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); Advance();
                return new Stmt { Kind = StmtKind.Rem, Line = line };
            }
            if (Peek().Type == TokenType.Ident && Peek().Text.Equals("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); Advance();
                return new Stmt { Kind = StmtKind.Rem, Line = line };
            }
            Advance();
            return new Stmt { Kind = StmtKind.End, Line = line };
        }
        if (IsKeyword("SELECT")) return ParseSelect(line);
        if (IsKeyword("DO")) return ParseDo(line);
        if (IsKeyword("RANDOMIZE"))
        {
            Advance();
            if (MatchOp("("))
            {
                int d = 1; // 已消费左括号，跟踪未闭合深度
                while (!IsEof)
                {
                    if (IsOp("(")) { d++; Advance(); }
                    else if (IsOp(")")) { d--; Advance(); if (d == 0) break; }
                    else Advance();
                }
            }
            return new Stmt { Kind = StmtKind.Randomize, Line = line };
        }
        if (IsKeyword("DATA")) return ParseData(line);
        if (IsKeyword("READ")) return ParseRead(line);
        if (IsKeyword("RESTORE"))
        {
            Advance();
            var rs = new Stmt { Kind = StmtKind.Restore, Line = line };
            if (Cur.Type == TokenType.Ident) { rs.Target = Cur.Text; Advance(); }
            return rs;
        }        if (IsKeyword("POKE") || IsKeyword("SEG")) { while (Cur.Type != TokenType.Newline && !IsEof) Advance(); return new Stmt { Kind = StmtKind.Rem, Line = line }; }
        if (IsKeyword("VIEW")) { while (Cur.Type != TokenType.Newline && !IsEof) Advance(); return new Stmt { Kind = StmtKind.Rem, Line = line }; }
        // 默认：SUB 调用（裸名）或赋值语句
        if (Cur.Type == TokenType.Ident && _subNames.Contains(Cur.Text)) return ParseSubCall(line);
        return ParseLet(line);
    }

    // ============ 图形 / 交互语句 ============

    private Stmt ParseScreen(int line)
    {
        Advance(); // SCREEN
        var mode = ParseExpr();
        return new Stmt { Kind = StmtKind.Screen, Line = line, Fg = mode };
    }

    private (Expr, Expr) ParseCoord()
    {
        ExpectOp("(");
        var x = ParseExpr();
        ExpectOp(",");
        var y = ParseExpr();
        ExpectOp(")");
        return (x, y);
    }

    private Stmt ParseLine(int line)
    {
        Advance(); // LINE
        var (x1, y1) = ParseCoord();
        ExpectOp("-");
        var (x2, y2) = ParseCoord();
        Expr? color = null;
        if (MatchOp(",")) color = ParseExpr();
        int mode = 0; // 0=none,1=B边框,2=BF填充
        if (Cur.Type == TokenType.Ident)
        {
            string m = Cur.Text.ToUpperInvariant();
            if (m == "BF") { mode = 2; Advance(); }
            else if (m == "B") { mode = 1; Advance(); }
        }
        else if (MatchOp(",") && Cur.Type == TokenType.Ident)
        {
            string m = Cur.Text.ToUpperInvariant();
            if (m == "BF") { mode = 2; Advance(); }
            else if (m == "B") { mode = 1; Advance(); }
        }
        return new Stmt { Kind = StmtKind.Line, Line = line, X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, ColorExpr = color, GfxMode = mode };
    }

    private Stmt ParseCircle(int line)
    {
        Advance(); // CIRCLE
        var (x, y) = ParseCoord();
        ExpectOp(",");
        var r = ParseExpr();
        Expr? color = null, start = null, end = null, aspect = null;
        if (MatchOp(",")) { if (!IsSep()) color = ParseExpr(); }
        if (MatchOp(",")) { if (!IsSep()) start = ParseExpr(); }
        if (MatchOp(",")) { if (!IsSep()) end = ParseExpr(); }
        if (MatchOp(",")) { if (!IsSep()) aspect = ParseExpr(); }
        return new Stmt { Kind = StmtKind.Circle, Line = line, X1 = x, Y1 = y, Radius = r, ColorExpr = color, StartAngle = start, EndAngle = end, Aspect = aspect };
    }

    private bool IsSep() => IsOp(",") || IsOp(")") || Cur.Type == TokenType.Newline || IsEof || IsOp(":");

    private Stmt ParsePset(int line)
    {
        Advance(); // PSET
        var (x, y) = ParseCoord();
        Expr? color = null;
        if (MatchOp(",")) color = ParseExpr();
        return new Stmt { Kind = StmtKind.Pset, Line = line, X1 = x, Y1 = y, ColorExpr = color };
    }

    private Stmt ParsePaint(int line)
    {
        Advance(); // PAINT
        var (x, y) = ParseCoord();
        Expr? color = null, boundary = null;
        if (MatchOp(",")) { if (!IsSep()) color = ParseExpr(); }
        if (MatchOp(",")) { if (!IsSep()) boundary = ParseExpr(); }
        return new Stmt { Kind = StmtKind.Paint, Line = line, X1 = x, Y1 = y, ColorExpr = color, X2 = boundary };
    }

    private Stmt ParseColor(int line)
    {
        Advance(); // COLOR
        var fg = ParseExpr();
        Expr? bg = null;
        if (MatchOp(",")) { if (!IsSep()) bg = ParseExpr(); }
        return new Stmt { Kind = StmtKind.Color, Line = line, Fg = fg, Bg = bg };
    }

    private Stmt ParsePalette(int line)
    {
        Advance(); // PALETTE
        var c = ParseExpr();
        Expr? v = null;
        if (MatchOp(",")) { if (!IsSep()) v = ParseExpr(); }
        return new Stmt { Kind = StmtKind.Palette, Line = line, Fg = c, Bg = v };
    }

    private Stmt ParseLocate(int line)
    {
        Advance(); // LOCATE
        var row = ParseExpr();
        Expr? col = null;
        if (MatchOp(",")) { if (!IsSep()) col = ParseExpr(); }
        return new Stmt { Kind = StmtKind.Locate, Line = line, Row = row, Col = col };
    }

    private Stmt ParseGet(int line)
    {
        Advance(); // GET
        var (x1, y1) = ParseCoord();
        ExpectOp("-");
        var (x2, y2) = ParseCoord();
        ExpectOp(",");
        var arr = ExpectIdent();
        return new Stmt { Kind = StmtKind.GetSprite, Line = line, X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, SpriteVar = arr };
    }

    private Stmt ParsePut(int line)
    {
        Advance(); // PUT
        var (x, y) = ParseCoord();
        ExpectOp(",");
        var arr = ExpectIdent();
        bool xor = false, pset = false;
        if (MatchOp(",") && Cur.Type == TokenType.Ident)
        {
            if (Cur.Text.Equals("XOR", StringComparison.OrdinalIgnoreCase)) xor = true;
            else if (Cur.Text.Equals("PSET", StringComparison.OrdinalIgnoreCase)) pset = true;
            Advance();
        }
        return new Stmt { Kind = StmtKind.PutSprite, Line = line, X1 = x, Y1 = y, SpriteVar = arr, SpriteXor = xor, SpritePset = pset };
    }

    private Stmt ParsePlay(int line)
    {
        Advance(); // PLAY
        string s = "";
        if (Cur.Type == TokenType.Str) { s = Cur.Str; Advance(); }
        else ParseExpr();
        return new Stmt { Kind = StmtKind.Play, Line = line, PlayStr = s };
    }

    private Stmt ParseWidth(int line)
    {
        Advance(); // WIDTH
        var w = ParseExpr();
        if (MatchOp(",")) { if (!IsSep()) ParseExpr(); }
        return new Stmt { Kind = StmtKind.Width, Line = line, Fg = w };
    }

    private Stmt ParseCall(int line)
    {
        Advance(); // CALL
        string name = ExpectIdent();
        ExpectOp("(");
        var args = new List<Expr>();
        var isArr = new List<bool>();
        while (!IsEof && !IsOp(")"))
        {
            if (Cur.Type == TokenType.Ident && Peek().Type == TokenType.Op && Peek().Text == "(" &&
                Peek(2).Type == TokenType.Op && Peek(2).Text == ")")
            {
                string an = Advance().Text; Advance(); Advance();
                args.Add(new Expr { Kind = ExprKind.ArrayRef, VarName = an, Indexes = new List<Expr>(), WholeArray = true });
                isArr.Add(true);
            }
            else { args.Add(ParseExpr()); isArr.Add(false); }
            if (!MatchOp(",")) break;
        }
        ExpectOp(")");
        return new Stmt { Kind = StmtKind.SubCall, Line = line, CallName = name, CallArgs = args, CallArgIsArray = isArr };
    }

    private Stmt ParseSubCall(int line)
    {
        string name = Advance().Text;
        var args = new List<Expr>();
        var isArr = new List<bool>();
        while (!IsEof && Cur.Type != TokenType.Newline && !IsOp(":"))
        {
            if (MatchOp(",")) continue;
            if (Cur.Type == TokenType.Ident && Peek().Type == TokenType.Op && Peek().Text == "(" &&
                Peek(2).Type == TokenType.Op && Peek(2).Text == ")")
            {
                string an = Advance().Text; Advance(); Advance();
                args.Add(new Expr { Kind = ExprKind.ArrayRef, VarName = an, Indexes = new List<Expr>(), WholeArray = true });
                isArr.Add(true);
                if (!IsOp(",")) break;
                continue;
            }
            args.Add(ParseExpr());
            isArr.Add(false);
            if (!IsOp(",")) break;
        }
        return new Stmt { Kind = StmtKind.SubCall, Line = line, CallName = name, CallArgs = args, CallArgIsArray = isArr };
    }

    private Stmt ParseOnError(int line)
    {
        Advance(); // ON
        ExpectKeyword("ERROR");
        ExpectKeyword("GOTO");
        bool zero = false; string label = "";
        if (Cur.Type == TokenType.Number && Cur.Num == 0) { zero = true; Advance(); }
        else label = ExpectIdent();
        return new Stmt { Kind = StmtKind.OnError, Line = line, ErrLabel = label, ErrZero = zero };
    }

    private Stmt ParseLineInput(int line)
    {
        Advance(); // LINE
        ExpectKeyword("INPUT");
        Expr? prompt = null;
        if (Cur.Type == TokenType.Str) { prompt = Expr.StrLit(Cur.Str); Advance(); if (MatchOp(";")) { } }
        string var = ExpectIdent();
        return new Stmt { Kind = StmtKind.LineInput, Line = line, VarName = var, IsStrVar = var.EndsWith('$'), Value = prompt };
    }

    // ============ 声明语句 ============

    private Stmt ParseType(int line)
    {
        Advance(); // TYPE
        string name = ExpectIdent();
        var ut = new UserType { Name = name };
        SkipNewlines();
        while (!IsEof)
        {
            if (IsKeyword("END") && Peek().Type == TokenType.Ident && Peek().Text.Equals("TYPE", StringComparison.OrdinalIgnoreCase)) break;
            if (Cur.Type == TokenType.Newline) { Advance(); continue; }
            string field = ExpectIdent();
            if (MatchKeyword("AS")) ExpectIdent();
            ut.Fields.Add(field);
            SkipNewlines();
        }
        if (IsKeyword("END")) { Advance(); Advance(); }
        Types[name] = ut;
        return new Stmt { Kind = StmtKind.Rem, Line = line };
    }

    private Stmt ParseConst(int line)
    {
        Advance(); // CONST
        do
        {
            string name = ExpectIdent();
            ExpectOp("=");
            var e = ParseExpr();
            Consts[name] = EvalConst(e);
        } while (MatchOp(","));
        return new Stmt { Kind = StmtKind.Rem, Line = line };
    }

    private double EvalConst(Expr e)
    {
        switch (e.Kind)
        {
            case ExprKind.NumLit: return e.Num;
            case ExprKind.Var:
                return Consts.TryGetValue(e.VarName, out double v) ? v : 0;
            case ExprKind.Unary:
                if (e.Op == "-") return -EvalConst(e.Left!);
                if (e.Op == "NOT") return EvalConst(e.Left!) == 0 ? -1 : 0;
                return 0;
            case ExprKind.Binary:
                double l = EvalConst(e.Left!), r = EvalConst(e.Right!);
                return e.Op switch
                {
                    "+" => l + r, "-" => l - r, "*" => l * r,
                    "/" => r == 0 ? 0 : l / r,
                    "MOD" => r == 0 ? 0 : l % r,
                    "AND" => (l != 0 && r != 0) ? -1 : 0,
                    "OR" => (l != 0 || r != 0) ? -1 : 0,
                    "=" => l == r ? -1 : 0,
                    "<>" => l != r ? -1 : 0,
                    _ => 0,
                };
            default: return 0;
        }
    }

    private Stmt ParseDef(int line)
    {
        Advance(); // DEF
        // DEF FNname (param) = body —— 函数名以 FN 开头，可为连写（FnRan）或独立（FN Ran）
        if (Cur.Type == TokenType.Ident && Cur.Text.StartsWith("FN", StringComparison.OrdinalIgnoreCase))
        {
            string name;
            if (Cur.Text.Equals("FN", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); // 独立 FN 关键字
                name = ExpectIdent();
            }
            else
            {
                name = ExpectIdent(); // 连写形式：FnRan
            }
            string param = "";
            if (MatchOp("(")) { param = ExpectIdent(); ExpectOp(")"); }
            ExpectOp("=");
            var body = ParseExpr();
            if (param != "")
            {
                // 参数隔离：把形参改名为唯一名，避免与调用处同名局部变量冲突
                // （如 DEF FnRan(x) 内联到 MakeCityScape 时，x 不能覆盖调用处的 x）
                string unique = "~fn_" + name + "_arg";
                body = Expr.RenameVar(body, param, unique);
                param = unique;
            }
            DefFns[name] = new DefFn { Name = name, Param = param, Body = body };
            return new Stmt { Kind = StmtKind.Rem, Line = line };
        }
        // DEFINT/DEFSNG/DEFDBL/DEFSTR/DEF SEG —— 本实现数值一律 double，忽略
        while (Cur.Type != TokenType.Newline && !IsEof) Advance();
        return new Stmt { Kind = StmtKind.Rem, Line = line };
    }

    private Stmt ParseDim(int line)
    {
        Advance(); // DIM / REDIM
        MatchKeyword("SHARED");
        var stmt = new Stmt { Kind = StmtKind.Dim, Line = line, DimSizes = new() };
        do
        {
            string name = ExpectIdent();
            var sizes = new List<Expr>();
            var lowers = new List<Expr?>();
            if (MatchOp("("))
            {
                do
                {
                    var lo = ParseExpr();
                    if (MatchKeyword("TO"))
                    {
                        lowers.Add(lo);
                        sizes.Add(ParseExpr());
                    }
                    else { lowers.Add(null); sizes.Add(lo); }
                } while (MatchOp(","));
                ExpectOp(")");
            }
            string type = "";
            if (MatchKeyword("AS")) type = ExpectIdent();
            stmt.DimVars.Add(name);
            stmt.DimDims.Add(sizes);
            stmt.DimLowers.Add(lowers);
            stmt.DimType.Add(type);
            stmt.DimSizes!.Add(sizes.Count > 0 ? sizes[0] : Expr.NumLit(0));
        } while (MatchOp(","));
        return stmt;
    }

    private Stmt ParseInput(int line)
    {
        Advance(); // INPUT
        Expr? prompt = null;
        if (Cur.Type == TokenType.Str) { prompt = Expr.StrLit(Cur.Str); Advance(); if (MatchOp(";")) { } }
        var stmt = new Stmt { Kind = StmtKind.Input, Line = line, Value = prompt };
        stmt.VarName = ExpectIdent();
        stmt.IsStrVar = stmt.VarName.EndsWith('$');
        return stmt;
    }

    private Stmt ParseLet(int line)
    {
        if (Cur.Type != TokenType.Ident) throw new ParseException("缺少赋值目标", line);
        string name = Advance().Text;
        bool isStr = name.EndsWith('$');
        bool isArray = false;
        Expr? index = null;
        List<Expr>? indexes = null;
        if (MatchOp("("))
        {
            isArray = true;
            indexes = new List<Expr>();
            do { indexes.Add(ParseExpr()); } while (MatchOp(","));
            ExpectOp(")");
            index = indexes[0];
        }
        if (MatchOp(".")) { name = name + ".." + ExpectIdent(); }
        ExpectOp("=");
        var value = ParseExpr();
        return new Stmt
        {
            Kind = StmtKind.Let,
            Line = line,
            VarName = name,
            IsStrVar = isStr,
            Index = index,
            Indexes = indexes,
            IsArray = isArray,
            Value = value,
        };
    }

    private Stmt ParsePrint(int line)
    {
        Advance(); // PRINT
        var items = new List<PrintItem>();
        if (Cur.Type == TokenType.Newline || Cur.Type == TokenType.Eof)
        {
            items.Add(new PrintItem { IsNewline = true });
            return new Stmt { Kind = StmtKind.Print, Line = line, PrintItems = items };
        }
        while (true)
        {
            char leadSep = '\0';
            if (MatchSeparator(ref leadSep))
            {
                if (items.Count > 0)
                {
                    var last = items[^1];
                    if (!last.IsNewline)
                        last.Separator = leadSep;
                }
                continue;
            }
            if (Cur.Type == TokenType.Newline || Cur.Type == TokenType.Eof) break;
            if (IsKeyword("REM")) { while (Cur.Type != TokenType.Newline && !IsEof) Advance(); break; }
            if (IsKeyword("ELSE") || IsKeyword("THEN") || IsKeyword("END")) break;
            // TAB(n) 制表项
            if (Cur.Type == TokenType.Ident && Cur.Text.Equals("TAB", StringComparison.OrdinalIgnoreCase) && Peek().Text == "(")
            {
                Advance(); Advance();
                var tc = ParseExpr();
                ExpectOp(")");
                items.Add(new PrintItem { TabCol = tc });
                char tsep = '\0';
                if (MatchSeparator(ref tsep)) items[^1].Separator = tsep;
                else if (Cur.Type == TokenType.Newline || Cur.Type == TokenType.Eof || IsOp(":")) break;
                continue;
            }
            var e = ParseExpr();
            items.Add(new PrintItem { Expr = e });
            char sep = '\0';
            if (MatchSeparator(ref sep))
            {
                items[^1].Separator = sep;
            }
            else if (Cur.Type == TokenType.Newline || Cur.Type == TokenType.Eof)
            {
                break;
            }
            else if (IsOp(":")) break;
        }
        return new Stmt { Kind = StmtKind.Print, Line = line, PrintItems = items };
    }

    private bool MatchSeparator(ref char sep)
    {
        if (Cur.Type == TokenType.Op && (Cur.Text == ";" || Cur.Text == ","))
        {
            sep = Cur.Text[0];
            Advance();
            return true;
        }
        return false;
    }

    // ============ SELECT / 流程控制 ============

    private Stmt ParseSelect(int line)
    {
        Advance(); // SELECT
        ExpectKeyword("CASE");
        var stmt = new Stmt { Kind = StmtKind.SelectCase, Line = line };
        stmt.SelectExpr = ParseExpr();
        stmt.Cases = new List<CaseClause>();
        SkipNewlines();
        while (!IsEof && !(IsKeyword("END") && Peek().Text.Equals("SELECT", StringComparison.OrdinalIgnoreCase)))
        {
            if (IsOp(":")) { Advance(); SkipNewlines(); continue; }
            if (Cur.Type == TokenType.Newline) { Advance(); continue; }
            if (IsKeyword("CASE"))
            {
                Advance();
                var clause = new CaseClause();
                if (IsKeyword("ELSE")) { Advance(); clause.IsElse = true; }
                else
                {
                    while (true)
                    {
                        if (IsKeyword("IS"))
                        {
                            Advance();
                            string op = ParseRelOp();
                            var v = ParseExpr();
                            clause.Conds.Add((op, v));
                        }
                        else
                        {
                            var e1 = ParseExpr();
                            if (MatchKeyword("TO"))
                            {
                                var e2 = ParseExpr();
                                clause.Ranges.Add((e1, e2));
                            }
                            else clause.Values.Add(e1);
                        }
                        if (!MatchOp(",")) break;
                    }
                }
                SkipNewlines();
                while (!IsEof && !IsKeyword("CASE")
                       && !(IsKeyword("END") && Peek().Text.Equals("SELECT", StringComparison.OrdinalIgnoreCase)))
                {
                    if (IsOp(":")) { Advance(); continue; }
                    clause.Body.Add(ParseStatement());
                    SkipNewlines();
                }
                stmt.Cases.Add(clause);
            }
            else throw new ParseException($"SELECT 中期望 CASE，实际得到 '{Cur.Text}'", Cur.Line);
        }
        if (IsKeyword("END") && Peek().Text.Equals("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            Advance(); Advance();
        }
        return stmt;
    }

    private string ParseRelOp()
    {
        if (Cur.Type == TokenType.Op && (Cur.Text == "=" || Cur.Text == "<>" || Cur.Text == "<" || Cur.Text == ">" || Cur.Text == "<=" || Cur.Text == ">="))
            return Advance().Text;
        throw new ParseException("CASE IS 需要比较运算符", Cur.Line);
    }

    private Stmt ParseIf(int line)
    {
        Advance(); // IF
        var cond = ParseExpr();
        ExpectKeyword("THEN");
        if (Cur.Type == TokenType.Newline || IsKeyword("ENDIF"))
        {
            var stmt = new Stmt { Kind = StmtKind.If, Line = line, Cond = cond };
            SkipNewlines();
            while (!IsEof && !IsKeyword("ELSE") && !IsKeyword("ELSEIF") && !(IsKeyword("END") && Peek().Text.Equals("IF", StringComparison.OrdinalIgnoreCase)))
            {
                if (IsOp(":")) { Advance(); continue; }
                stmt.ThenStmts.Add(ParseStatement());
                SkipNewlines();
            }
            // ELSEIF / ELSE / END IF 分支
            while (!IsEof && !(IsKeyword("END") && Peek().Text.Equals("IF", StringComparison.OrdinalIgnoreCase)))
            {
                if (IsOp(":")) { Advance(); continue; }
                if (IsKeyword("ELSEIF"))
                {
                    // 嵌套 IF：ELSEIF cond THEN ... 直到下一个 ELSEIF/ELSE/END IF
                    Advance(); // ELSEIF
                    var econd = ParseExpr();
                    ExpectKeyword("THEN");
                    var eif = new Stmt { Kind = StmtKind.If, Line = line, Cond = econd };
                    SkipNewlines();
                    while (!IsEof && !IsKeyword("ELSEIF") && !IsKeyword("ELSE")
                           && !(IsKeyword("END") && Peek().Text.Equals("IF", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (IsOp(":")) { Advance(); continue; }
                        eif.ThenStmts.Add(ParseStatement());
                        SkipNewlines();
                    }
                    stmt.ElseStmts.Add(eif);
                    continue;
                }
                if (IsKeyword("ELSE"))
                {
                    Advance();
                    SkipNewlines();
                    while (!IsEof && !IsKeyword("ELSEIF") && !(IsKeyword("END") && Peek().Text.Equals("IF", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (IsOp(":")) { Advance(); continue; }
                        stmt.ElseStmts.Add(ParseStatement());
                        SkipNewlines();
                    }
                    continue;
                }
                if (Cur.Type == TokenType.Newline) { Advance(); continue; }
                break;
            }
            if (IsKeyword("END") && Peek().Text.Equals("IF", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); Advance();
            }
            return stmt;
        }
        else
        {
            var then = new List<Stmt>();
            while (!IsEof && !IsKeyword("ELSE") && Cur.Type != TokenType.Newline)
            {
                if (IsOp(":")) { Advance(); continue; }
                then.Add(ParseStatement());
            }
            var elseStmts = new List<Stmt>();
            if (IsKeyword("ELSE"))
            {
                Advance();
                while (!IsEof && Cur.Type != TokenType.Newline)
                {
                    if (IsOp(":")) { Advance(); continue; }
                    elseStmts.Add(ParseStatement());
                }
            }
            return new Stmt
            {
                Kind = StmtKind.If,
                Line = line,
                Cond = cond,
                SingleLineThen = then,
                SingleLineElseStmts = elseStmts,
            };
        }
    }

    private Stmt ParseFor(int line)
    {
        Advance(); // FOR
        string var = ExpectIdent();
        ExpectOp("=");
        var from = ParseExpr();
        ExpectKeyword("TO");
        var to = ParseExpr();
        Expr? step = null;
        if (MatchKeyword("STEP")) step = ParseExpr();
        if (step == null) step = Expr.NumLit(1);
        var stmt = new Stmt { Kind = StmtKind.For, Line = line, ForVar = var, From = from, To = to, Step = step };
        SkipNewlines();
        while (!IsEof && !IsKeyword("NEXT"))
        {
            if (IsOp(":")) { Advance(); continue; }
            stmt.Body.Add(ParseStatement());
            SkipNewlines();
        }
        if (IsKeyword("NEXT"))
        {
            Advance();
            if (Cur.Type == TokenType.Ident) Advance(); // NEXT i 的循环变量
        }
        return stmt;
    }

    private Stmt ParseWhile(int line)
    {
        Advance(); // WHILE
        var cond = ParseExpr();
        var stmt = new Stmt { Kind = StmtKind.While, Line = line, Cond = cond };
        SkipNewlines();
        while (!IsEof && !IsKeyword("WEND"))
        {
            if (IsOp(":")) { Advance(); continue; }
            stmt.Body.Add(ParseStatement());
            SkipNewlines();
        }
        if (IsKeyword("WEND")) Advance();
        return stmt;
    }

    private Stmt ParseDo(int line)
    {
        Advance(); // DO
        var stmt = new Stmt { Kind = StmtKind.DoLoop, Line = line };
        if (MatchKeyword("WHILE")) { stmt.DoCond = ParseExpr(); stmt.DoUntil = false; stmt.DoCondAfter = false; }
        else if (MatchKeyword("UNTIL")) { stmt.DoCond = ParseExpr(); stmt.DoUntil = true; stmt.DoCondAfter = false; }
        SkipNewlines();
        while (!IsEof && !IsKeyword("LOOP"))
        {
            if (IsOp(":")) { Advance(); continue; }
            stmt.Body.Add(ParseStatement());
            SkipNewlines();
        }
        if (IsKeyword("LOOP"))
        {
            Advance();
            if (MatchKeyword("WHILE")) { stmt.DoCond = ParseExpr(); stmt.DoUntil = false; stmt.DoCondAfter = true; }
            else if (MatchKeyword("UNTIL")) { stmt.DoCond = ParseExpr(); stmt.DoUntil = true; stmt.DoCondAfter = true; }
        }
        return stmt;
    }

    private Stmt ParseGoto(int line, bool gosub)
    {
        if (Cur.Type == TokenType.LineNum || Cur.Type == TokenType.Number)
        {
            double num = Cur.Num;
            string text = Cur.Text;
            Advance();
            return new Stmt { Kind = gosub ? StmtKind.Gosub : StmtKind.Goto, Line = line, Target = text, TargetNum = num, TargetIsNum = true };
        }
        string name = ExpectIdent();
        return new Stmt { Kind = gosub ? StmtKind.Gosub : StmtKind.Goto, Line = line, Target = name, TargetIsNum = false };
    }

    private Stmt ParseData(int line)
    {
        Advance(); // DATA
        while (true)
        {
            if (Cur.Type == TokenType.Number)
            {
                Data.Add(new DataItem { IsStr = false, Num = Cur.Num });
                Advance();
            }
            else if (Cur.Type == TokenType.Str)
            {
                Data.Add(new DataItem { IsStr = true, Str = Cur.Str });
                Advance();
            }
            else if (Cur.Type == TokenType.Op && Cur.Text == "-")
            {
                Advance();
                if (Cur.Type != TokenType.Number) throw new ParseException("DATA 负数格式错误", line);
                Data.Add(new DataItem { IsStr = false, Num = -Cur.Num });
                Advance();
            }
            else break;
            if (!MatchOp(",")) break;
        }
        return new Stmt { Kind = StmtKind.Rem, Line = line };
    }

    private Stmt ParseRead(int line)
    {
        Advance(); // READ
        var stmt = new Stmt { Kind = StmtKind.Read, Line = line };
        stmt.ReadVars = new List<string>();
        stmt.ReadIsStr = new List<bool>();
        stmt.ReadIndexes = new List<Expr?>();
        do
        {
            string name = ExpectIdent();
            stmt.ReadVars.Add(name);
            stmt.ReadIsStr!.Add(name.EndsWith('$'));
            if (MatchOp("("))
            {
                stmt.ReadIndexes.Add(ParseExpr());
                ExpectOp(")");
            }
            else stmt.ReadIndexes.Add(null);
        } while (MatchOp(","));
        return stmt;
    }

    // ============ 辅助 ============

    private void ExpectOp(string op)
    {
        if (!MatchOp(op)) throw new ParseException($"期望 '{op}'，实际得到 '{Cur.Text}'", Cur.Line);
    }

    private void ExpectKeyword(string kw)
    {
        if (!MatchKeyword(kw)) throw new ParseException($"期望 {kw}", Cur.Line);
    }

    private string ExpectIdent()
    {
        if (Cur.Type != TokenType.Ident) throw new ParseException($"期望标识符，实际得到 '{Cur.Text}'", Cur.Line);
        return Advance().Text;
    }

    // ---------- 表达式（优先级爬升） ----------

    private Expr ParseExpr() => ParseOr();

    private Expr ParseOr()
    {
        var e = ParseAnd();
        while (IsKeyword("OR")) { Advance(); var r = ParseAnd(); e = Expr.Binary("OR", e, r); }
        return e;
    }

    private Expr ParseAnd()
    {
        var e = ParseNot();
        while (IsKeyword("AND")) { Advance(); var r = ParseNot(); e = Expr.Binary("AND", e, r); }
        return e;
    }

    private Expr ParseNot()
    {
        if (IsKeyword("NOT")) { Advance(); return Expr.Unary("NOT", ParseNot()); }
        return ParseComparison();
    }

    private Expr ParseComparison()
    {
        var e = ParseAddSub();
        while (Cur.Type == TokenType.Op && (Cur.Text == "=" || Cur.Text == "<>" || Cur.Text == "<" || Cur.Text == ">" || Cur.Text == "<=" || Cur.Text == ">="))
        {
            string op = Advance().Text;
            var r = ParseAddSub();
            e = Expr.Binary(op, e, r);
        }
        return e;
    }

    private Expr ParseAddSub()
    {
        var e = ParseMulDiv();
        while (Cur.Type == TokenType.Op && (Cur.Text == "+" || Cur.Text == "-"))
        {
            string op = Advance().Text;
            var r = ParseMulDiv();
            e = Expr.Binary(op, e, r);
        }
        return e;
    }

    private Expr ParseMulDiv()
    {
        var e = ParseUnary();
        while (true)
        {
            if (Cur.Type == TokenType.Op && (Cur.Text == "*" || Cur.Text == "/" || Cur.Text == "\\"))
            {
                string op = Advance().Text;
                var r = ParseUnary();
                e = Expr.Binary(op, e, r);
            }
            else if (IsKeyword("MOD"))
            {
                Advance();
                var r = ParseUnary();
                e = Expr.Binary("MOD", e, r);
            }
            else break;
        }
        return e;
    }

    private Expr ParseUnary()
    {
        if (Cur.Type == TokenType.Op && Cur.Text == "-") { Advance(); return Expr.Unary("-", ParseUnary()); }
        if (Cur.Type == TokenType.Op && Cur.Text == "+") { Advance(); return ParseUnary(); }
        return ParsePower();
    }

    private Expr ParsePower()
    {
        var e = ParsePrimary();
        while (Cur.Type == TokenType.Op && Cur.Text == "^")
        {
            Advance();
            var r = ParsePower(); // 右结合
            e = Expr.Binary("^", e, r);
        }
        return e;
    }

    private Expr ParsePrimary()
    {
        var t = Cur;
        if (t.Type == TokenType.Number) { Advance(); return Expr.NumLit(t.Num); }
        if (t.Type == TokenType.Str) { Advance(); return Expr.StrLit(t.Str); }
        if (t.Type == TokenType.Op && t.Text == "(")
        {
            Advance();
            var e = ParseExpr();
            ExpectOp(")");
            return e;
        }
        if (t.Type == TokenType.Ident)
        {
            string name = t.Text;
            bool isStr = name.EndsWith('$');
            if (Consts.TryGetValue(name, out double cv) && !(Peek().Type == TokenType.Op && (Peek().Text == "(" || Peek().Text == "=" || Peek().Text == ".")))
            { Advance(); return Expr.NumLit(cv); }
            if (Keywords.Set.Contains(name.ToUpperInvariant()) && Peek().Type == TokenType.Op && Peek().Text == "(")
            {
                Advance(); // name
                Advance(); // (
                var args = new List<Expr>();
                if (!(Cur.Type == TokenType.Op && Cur.Text == ")"))
                {
                    do { args.Add(ParseExpr()); } while (MatchOp(","));
                }
                ExpectOp(")");
                return Expr.Call(name.ToUpperInvariant(), args);
            }
            // 零参函数裸用（不带括号）：INKEY$ / TIMER —— QBasic 中常直接写 `INKEY$`、`TIMER`
            if (Keywords.Set.Contains(name.ToUpperInvariant()) &&
                (name.Equals("INKEY$", StringComparison.OrdinalIgnoreCase) || name.Equals("TIMER", StringComparison.OrdinalIgnoreCase)))
            {
                Advance();
                return Expr.Call(name.ToUpperInvariant(), new List<Expr>());
            }
            Advance();
            // 例程函数调用
            if (Cur.Type == TokenType.Op && Cur.Text == "(")
            {
                if (IsUserFunc(name))
                {
                    Advance(); // (
                    var args = new List<Expr>();
                    if (!(Cur.Type == TokenType.Op && Cur.Text == ")"))
                    {
                        do { args.Add(ParseExpr()); } while (MatchOp(","));
                    }
                    ExpectOp(")");
                    return Expr.Call(name, args);
                }
                if (DefFns.ContainsKey(name))
                {
                    Advance(); // (
                    var args = new List<Expr>();
                    if (!(Cur.Type == TokenType.Op && Cur.Text == ")"))
                    {
                        do { args.Add(ParseExpr()); } while (MatchOp(","));
                    }
                    ExpectOp(")");
                    return Expr.Call(name, args);
                }
                Advance();
                var idxs = new List<Expr>();
                do { idxs.Add(ParseExpr()); } while (MatchOp(","));
                ExpectOp(")");
                string field = "";
                if (MatchOp(".")) field = ExpectIdent();
                if (field != "") name = name + ".." + field;
                var e = Expr.ArrayRef(name, idxs[0], isStr);
                e.Indexes = idxs;
                return e;
            }
            // 零参用户函数裸调用（不带括号）：MachSpeed = CalcDelay
            if (IsUserFunc(name))
                return Expr.Call(name, new List<Expr>());
            string f2 = "";
            if (MatchOp(".")) f2 = ExpectIdent();
            if (f2 != "") name = name + ".." + f2;
            return Expr.Var(name, isStr);
        }
        throw new ParseException($"意外的记号 '{t.Text}'", t.Line);
    }
}
