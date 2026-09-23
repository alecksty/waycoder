using CompilerBase;
using System.Linq;

namespace BasicCompiler;

public partial class Parser : ParserBase<Token, TokenType>
{
    Statement ParseScreenStatement()
    {
        var t = Advance();
        var s = new ScreenStatement(t.Line, t.Column);
        s.Mode = ParseExpression();
        return s;
    }

    Statement ParsePsetStatement()
    {
        var t = Advance();
        var s = new PsetStatement(t.Line, t.Column);
        if (Peek().Type == TokenType.LPAREN) Advance();
        s.X = ParseExpression();
        if (Peek().Type == TokenType.COMMA) Advance();
        s.Y = ParseExpression();
        if (Peek().Type == TokenType.RPAREN) Advance(); // consume )
        // Parse optional color argument
        if (Peek().Type == TokenType.COMMA)
        {
            Advance();
            s.Color = ParseExpression();
        }
        return s;
    }

    Statement ParseQbLineStatement()
    {
        var t = Advance();
        var s = new QbLineStatement(t.Line, t.Column);
        if (Peek().Type == TokenType.LPAREN) Advance();
        s.X1 = ParseExpression();
        if (Peek().Type == TokenType.COMMA) Advance();
        s.Y1 = ParseExpression();
        if (Peek().Type == TokenType.RPAREN) Advance();
        // Expect '-'
        if (Peek().Type == TokenType.MINUS) Advance();
        if (Peek().Type == TokenType.LPAREN) Advance();
        s.X2 = ParseExpression();
        if (Peek().Type == TokenType.COMMA) Advance();
        s.Y2 = ParseExpression();
        if (Peek().Type == TokenType.RPAREN) Advance();
        if (Peek().Type == TokenType.COMMA) Advance();
        s.Color = ParseExpression();
        // Skip optional comma before B/BF
        if (Peek().Type == TokenType.COMMA) Advance();
        // Check for optional "B" or "BF" (box/filled box)
        if (Peek().Type == TokenType.IDENTIFIER)
        {
            var opt = Peek().Value.ToUpper();
            if (opt == "BF")
            {
                Advance();
                s.Box = true;
                s.Fill = true;
            }
            else if (opt == "B")
            {
                Advance();
                s.Box = true;
            }
        }
        return s;
    }

    Statement ParseQbCircleStatement()
    {
        var t = Advance();
        var s = new QbCircleStatement(t.Line, t.Column);
        if (Peek().Type == TokenType.LPAREN) Advance();
        s.X = ParseExpression();
        if (Peek().Type == TokenType.COMMA) Advance();
        s.Y = ParseExpression();
        if (Peek().Type == TokenType.RPAREN) Advance();
        if (Peek().Type == TokenType.COMMA) Advance();
        s.Radius = ParseExpression();
        // Parse optional: , color [, start, end, aspect]
        if (Peek().Type == TokenType.COMMA) { Advance();
            if (Peek().Type != TokenType.COMMA && Peek().Type != TokenType.RPAREN &&
                Peek().Type != TokenType.EOF)
                s.Color = ParseExpression();
            // Optional start angle
            if (Peek().Type == TokenType.COMMA) { Advance();
                if (Peek().Type != TokenType.COMMA && Peek().Type != TokenType.RPAREN &&
                    Peek().Type != TokenType.EOF) {
                    s.Start = ParseExpression(); s.HasStart = true;
                }
                // Optional end angle
                if (Peek().Type == TokenType.COMMA) { Advance();
                    if (Peek().Type != TokenType.COMMA && Peek().Type != TokenType.RPAREN &&
                        Peek().Type != TokenType.EOF) {
                        s.End = ParseExpression(); s.HasEnd = true;
                    }
                    // Optional aspect
                    if (Peek().Type == TokenType.COMMA) { Advance();
                        if (Peek().Type != TokenType.RPAREN && Peek().Type != TokenType.EOF) {
                            s.Aspect = ParseExpression(); s.HasAspect = true;
                        }
                    }
                }
            }
        }
        return s;
    }

    Statement ParseQbPaintStatement()
    {
        var t = Advance();
        var s = new QbPaintStatement(t.Line, t.Column);
        if (Peek().Type == TokenType.LPAREN) Advance();
        s.X = ParseExpression();
        if (Peek().Type == TokenType.COMMA) Advance();
        s.Y = ParseExpression();
        if (Peek().Type == TokenType.RPAREN) Advance();
        if (Peek().Type == TokenType.COMMA) Advance();
        s.Color = ParseExpression();
        if (Peek().Type == TokenType.COMMA)
        {
            Advance();
            s.Border = ParseExpression();
            s.HasBorder = true;
        }
        return s;
    }

    Statement ParseLocateStatement()
    {
        var t = Advance();
        var s = new LocateStatement(t.Line, t.Column);
        s.Row = ParseExpression();
        if (Peek().Type == TokenType.COMMA) Advance();
        s.Col = ParseExpression();
        return s;
    }

    Statement ParseQbColorStatement()
    {
        var t = Advance();
        var s = new QbColorStatement(t.Line, t.Column);
        s.Foreground = ParseExpression();
        if (Peek().Type == TokenType.COMMA)
        {
            Advance();
            s.Background = ParseExpression();
            s.HasBackground = true;
        }
        return s;
    }

    Statement ParseRandomizeStatement()
    {
        var t = Advance();
        var s = new RandomizeStatement(t.Line, t.Column);
        if (Peek().Type == TokenType.IDENTIFIER && Peek().Value.ToUpper() == "TIMER")
        {
            Advance(); // consume TIMER
            s.HasSeed = false; // will use timer
        }
        else if (Peek().Type != TokenType.EOF && Peek().Type != TokenType.COLON &&
                 Peek().Type != TokenType.REM)
        {
            s.Seed = ParseExpression();
            s.HasSeed = true;
        }
        return s;
    }

    Statement ParseQbWidthStatement()
    {
        var t = Advance();
        var s = new QbWidthStatement(t.Line, t.Column);
        s.Cols = ParseExpression();
        if (Peek().Type == TokenType.COMMA) Advance();
        s.Rows = ParseExpression();
        return s;
    }

    Statement ParseSleepStatement()
    {
        var t = Advance();
        var s = new SleepStatement(t.Line, t.Column);
        if (Peek().Type == TokenType.NUMBER || Peek().Type == TokenType.IDENTIFIER ||
            Peek().Type == TokenType.LPAREN || Peek().Type == TokenType.PLUS || Peek().Type == TokenType.MINUS)
        {
            s.Seconds = ParseExpression();
            s.HasSeconds = true;
        }
        return s;
    }

    Statement ParseSwapStatement()
    {
        var t = Advance();
        var s = new SwapStatement(t.Line, t.Column);
        s.Var1 = ParsePrimary();
        if (Peek().Type == TokenType.COMMA) Advance();
        s.Var2 = ParsePrimary();
        return s;
    }

    Statement ParseEraseStatement()
    {
        var t = Advance();
        var s = new EraseStatement(t.Line, t.Column);
        if (Peek().Type == TokenType.IDENTIFIER)
        {
            s.ArrayName = Peek().Value.ToLower();
            Advance();
        }
        return s;
    }

    Statement ParseLPrintStatement()
    {
        var t = Advance();
        var s = new LPrintStatement(t.Line, t.Column);
        s.Expressions = new List<Expression>();
        while (Peek().Type != TokenType.EOF && Peek().Type != TokenType.COLON)
        {
            // 行尾结束（同 `ParsePrintStatement` / `LineEnded`）
            if (LineEnded(t.Line)) break;
            if (!IsExpressionStart(Peek())) break;
            Expression expr = ParseExpression();
            bool hasExpr = expr != null;
            if (hasExpr) s.Expressions.Add(expr);
            // 分隔符记账 —— 与 `ParsePrintStatement` 同一口径（见 `PrintStatement.Separators`）
            if (Peek().Type == TokenType.COMMA || Peek().Type == TokenType.SEMICOLON)
            {
                if (hasExpr) s.Separators.Add(Peek().Type == TokenType.COMMA ? ',' : ';');
                Advance();
            }
            else
            {
                if (hasExpr) s.Separators.Add('\0');
                break;
            }
        }
        return s;
    }

    // ==================== DATA/READ/RESTORE ====================

    Statement ParseDataStatement()
    {
        var t = Advance();
        var s = new DataStatement(t.Line, t.Column);
        while (!AtEnd() && Peek().Type != TokenType.EOF && Peek().Type != TokenType.COLON && Peek().Type != TokenType.DATA)
        {
            // 行尾结束（同 `LineEnded`）：`DATA 1,` 的尾逗号不能吃掉下一行
            if (LineEnded(t.Line))
                break;
            s.Values.Add(ParseExpression());
            if (Peek().Type == TokenType.COMMA)
                Advance();
            else
                break;
        }
        return s;
    }

    Statement ParseReadStatement()
    {
        var t = Advance();
        var s = new ReadStatement(t.Line, t.Column);
        while (!AtEnd() && Peek().Type != TokenType.EOF && Peek().Type != TokenType.COLON)
        {
            // 行尾结束（同 `LineEnded`）：`READ a,` 的尾逗号不能吃掉下一行
            if (LineEnded(t.Line))
                break;
            if (Peek().Type == TokenType.IDENTIFIER)
            {
                var id = new Identifier(Peek().Line, Peek().Column, Peek().Value);
                Advance();
                // ⚠ `READ a(i)` —— 数组元素。**下标必须一起吃掉**。
                //
                //   从前这里只 Add 一个 Identifier 就完事，`(i)` 留在 token 流上：
                //   `(` 被语句层跳过、`i` 落到「名字后面不是 `=`」的兜底分支编成 `CALL func_i`，
                //   链接期报「未定义的函数 'func_i'」——**报错的名字与真实缺陷（数组元素读）
                //   毫无关联**，而且链接器还会把 `func_i` 兜到同名全局变量上，
                //   于是它**不报错也能"跑"**、只在运行期崩。
                //   实测最小复现：`FOR i = 0 TO 2 / READ arr(i) / NEXT i` ⇒ func_i；
                //   GORILLA.BAS 的 `READ LBan&(i)`（8 个循环）就是这一条。
                if (Peek().Type == TokenType.LPAREN)
                {
                    Advance(); // skip (
                    var acc = new ArrayAccessExpression(id.Line, id.Column) { ArrayName = id.Name };
                    while (!AtEnd() && Peek().Type != TokenType.RPAREN && Peek().Type != TokenType.COLON)
                    {
                        if (Peek().Type == TokenType.COMMA) { Advance(); continue; }
                        var idx = ParseExpression();
                        if (idx == null) break;
                        acc.Indices.Add(idx);
                    }
                    if (Peek().Type == TokenType.RPAREN) Advance(); // skip )
                    if (acc.Indices.Count > 0) acc.Index = acc.Indices[0];
                    s.Variables.Add(acc);
                }
                else
                {
                    s.Variables.Add(id);
                }
            }
            if (Peek().Type == TokenType.COMMA)
                Advance();
            else
                break;
        }
        return s;
    }

    Statement ParseRestoreStatement()
    {
        var t = Advance(); // skip RESTORE
        // RESTORE [label] — optional label to reset data pointer to
        if (!AtEnd() && Peek().Type == TokenType.IDENTIFIER)
            Advance(); // skip label name
        return new RestoreStatement(t.Line, t.Column);
    }

    // ==================== ON ERROR GOTO / RESUME ====================

    Statement ParseOnErrorStatement()
    {
        var t = Advance(); // skip ON
        var s = new OnErrorStatement(t.Line, t.Column);

        // Expect ERROR
        if (Peek().Type != TokenType.ERROR_KW)
            return null;
        Advance(); // skip ERROR

        // Expect GOTO or RESUME
        if (Peek().Type == TokenType.GOTO)
        {
            Advance(); // skip GOTO
            if (Peek().Type == TokenType.NUMBER)
            {
                var lineVal = int.Parse(Peek().Value);
                if (lineVal == 0)
                {
                    s.DisableHandler = true;
                }
                else
                {
                    s.ErrorHandlerLine = lineVal;
                }
                Advance();
            }
            else if (Peek().Type == TokenType.IDENTIFIER)
            {
                s.ErrorHandlerLabel = Peek().Value;
                Advance();
            }
        }
        else if (Peek().Type == TokenType.RESUME)
        {
            Advance(); // skip RESUME
            if (Peek().Type == TokenType.NEXT)
            {
                Advance(); // skip NEXT
                s.ResumeNext = true;
            }
        }

        return s;
    }

    Statement ParseResumeStatement()
    {
        var t = Advance(); // skip RESUME
        var s = new ResumeStatement(t.Line, t.Column);
        if (Peek().Type == TokenType.NEXT)
        {
            Advance();
            s.ResumeNext = true;
        }
        return s;
    }

    // ==================== DRAW 语句 ====================

    Statement ParseDrawStatement()
    {
        var t = Advance(); // skip DRAW
        var s = new DrawStatement(t.Line, t.Column);
        s.DrawString = ParseExpression();
        return s;
    }

    // ==================== PLAY/SOUND ====================

    Statement ParsePlayStatement()
    {
        var t = Advance(); // skip PLAY
        var s = new PlayStatement(t.Line, t.Column);
        s.CommandString = ParseExpression();
        return s;
    }

    Statement ParseSoundStatement()
    {
        var t = Advance(); // skip SOUND
        var s = new SoundStatement(t.Line, t.Column);
        s.Frequency = ParseExpression();
        if (Peek().Type == TokenType.COMMA) Advance();
        s.Duration = ParseExpression();
        return s;
    }

    // ==================== REDIM/PRESERVE ====================

    Statement ParseRedimStatement()
    {
        var t = Advance(); // skip REDIM
        var s = new RedimStatement(t.Line, t.Column);

        // 检查是否有 PRESERVE
        if (Peek().Type == TokenType.PRESERVE)
        {
            Advance();
            s.Preserve = true;
        }

        // 读取**一个或多个** `数组名(大小)`，逗号分隔。
        //
        // ⚠ 这里原来只读**第一个**就 return，于是 `REDIM a(8), b(8)` 里
        //   `, b(8)` 被留在行上、由语句分派器当成新语句 —— 而行首是一个
        //   未声明标识符时会走「裸调用」那条路，编出 `CALL func_b`。后果不是
        //   "少 redim 一个数组"，而是**后面的 FOR/NEXT 整段被误解析**（实测 GORILLA.BAS 报
        //   `未定义的函数 'func_i'（引用 8 次）`，真因就在这一行）。
        //   老 QBasic 里多数组 REDIM 很常见，故按 QBasic 语义收下：AST 的 `RedimStatement`
        //   仍然一个数组一个，多个包成 `SequenceStatement` —— 语句层两条路
        //   （主程序 / SUB 体）都已经有 SequenceStatement 的生成分支，不用改 AST。
        var seq = new SequenceStatement(t.Line, t.Column);
        while (true)
        {
            if (Peek().Type != TokenType.IDENTIFIER)
                return seq.Statements.Count > 0 ? seq : null;
            var one = new RedimStatement(t.Line, t.Column) { Preserve = s.Preserve };
            one.ArrayName = Peek().Value.ToLower();
            Advance();

            // 解析 (newsize)
            if (Peek().Type != TokenType.LPAREN)
                return null;
            Advance(); // skip (
            one.NewSize = ParseExpression();
            if (Peek().Type != TokenType.RPAREN)
                return null;
            Advance(); // skip )
            seq.Statements.Add(one);

            if (Peek().Type != TokenType.COMMA) break;
            Advance(); // skip ,
        }

        return seq.Statements.Count == 1 ? seq.Statements[0] : seq;
    }

    // ==================== LINE INPUT ====================

    Statement ParseLineInputStatement()
    {
        Advance(); // skip LINE
        Advance(); // skip INPUT
        // LINE INPUT ["prompt";] variable$
        // LINE INPUT #filenum, variable$
        if (Peek().Type == TokenType.STRING)
        {
            Advance(); // skip prompt string
            if (Peek().Type == TokenType.SEMICOLON) Advance();
        }
        else if (Peek().Type == TokenType.HASH)
        {
            // FILE INPUT: LINE INPUT #1, var$
            Advance(); // skip #
            ParseExpression(); // file number
            if (Peek().Type == TokenType.COMMA) Advance();
        }
        // Parse variable
        if (Peek().Type == TokenType.IDENTIFIER) Advance();
        return null; // no-op (INPUT is complex; game uses default values)
    }

    // ==================== VIEW PRINT ====================

    Statement ParseViewPrintStatement()
    {
        Advance(); // skip VIEW
        // VIEW PRINT [topRow TO bottomRow]
        if (Peek().Type == TokenType.PRINT)
        {
            Advance(); // skip PRINT
            // Next could be topRow
            if (Peek().Type == TokenType.NUMBER || Peek().Type == TokenType.IDENTIFIER)
            {
                ParseExpression();
                if (Peek().Type == TokenType.TO) Advance();
                if (Peek().Type == TokenType.NUMBER || Peek().Type == TokenType.IDENTIFIER)
                    ParseExpression();
            }
        }
        return null; // no-op
    }

    // ==================== POINT ====================

    Expression ParsePointExpression()
    {
        Advance(); // skip POINT
        // POINT(x, y) — return 0 (no pixel read support)
        if (Peek().Type == TokenType.LPAREN) Advance();
        ParseExpression(); // x
        if (Peek().Type == TokenType.COMMA) Advance();
        ParseExpression(); // y
        if (Peek().Type == TokenType.RPAREN) Advance();
        return new NumberLiteral(0, 0, 0); // always 0
    }

    // ==================== DEF FN ====================

    // ==================== GET/PUT ====================

    Statement ParseGetStatement()
    {
        var t = Advance();
        var s = new GetStatement(t.Line, t.Column);
        // GET (x1,y1)-(x2,y2), array
        if (Peek().Type == TokenType.LPAREN) Advance();
        s.X1 = ParseExpression();
        if (Peek().Type == TokenType.COMMA) Advance();
        s.Y1 = ParseExpression();
        if (Peek().Type == TokenType.RPAREN) Advance();
        if (Peek().Type == TokenType.MINUS) Advance();
        if (Peek().Type == TokenType.LPAREN) Advance();
        s.X2 = ParseExpression();
        if (Peek().Type == TokenType.COMMA) Advance();
        s.Y2 = ParseExpression();
        if (Peek().Type == TokenType.RPAREN) Advance();
        if (Peek().Type == TokenType.COMMA) Advance();
        if (Peek().Type == TokenType.IDENTIFIER)
        {
            s.ArrayName = Peek().Value.ToLower();
            Advance();
            // `, buf()` —— QBasic 的**整数组**写法就是带一对空括号。
            // 不吃掉的话 `(` 留在 token 流里被语句层当垃圾跳过，
            // 后面那半个语句就变成一次莫名的 `func_xxx` 调用（链接期报错的还是别的名字）。
            if (Peek().Type == TokenType.LPAREN)
            {
                Advance();
                if (Peek().Type == TokenType.RPAREN) Advance();
            }
        }
        return s;
    }

    Statement ParsePutStatement()
    {
        var t = Advance();
        var s = new PutStatement(t.Line, t.Column);
        // PUT (x,y), array [, action]
        if (Peek().Type == TokenType.LPAREN) Advance();
        s.X = ParseExpression();
        if (Peek().Type == TokenType.COMMA) Advance();
        s.Y = ParseExpression();
        if (Peek().Type == TokenType.RPAREN) Advance();
        if (Peek().Type == TokenType.COMMA) Advance();
        if (Peek().Type == TokenType.IDENTIFIER)
        {
            s.ArrayName = Peek().Value.ToLower();
            Advance();
            // `, buf()` —— 整数组写法（同 GET 那处，理由见那里）
            if (Peek().Type == TokenType.LPAREN)
            {
                Advance();
                if (Peek().Type == TokenType.RPAREN) Advance();
            }
        }
        // Optional action: PSET, PRESET, AND, OR, XOR
        //（PSET/AND/OR 有自己的 token 类型；`XOR` 词法表里没有 ⇒ 是 IDENTIFIER。
        //  三种都要认，漏一个的后果是它被**当成下一条语句** —— 实测 `PUT …, buf(), XOR`
        //  报「未定义的函数 'func_xor'」，而那条 PUT 其实已经解析完了、只是动作没吃掉。）
        if (Peek().Type == TokenType.COMMA)
        {
            Advance();
            if (Peek().Type == TokenType.IDENTIFIER || Peek().Type == TokenType.PSET
                || Peek().Type == TokenType.AND || Peek().Type == TokenType.OR)
            {
                s.Action = Peek().Value.ToUpper();
                Advance();
            }
        }
        else
        {
            s.Action = "PSET";
        }
        return s;
    }

    // ==================== PALETTE ====================

    Statement ParsePaletteStatement()
    {
        var t = Advance();
        var s = new PaletteStatement(t.Line, t.Column);
        // PALETTE index, red, green, blue (all except index are optional)
        s.ColorIndex = ParseExpression();
        if (Peek().Type == TokenType.COMMA) { Advance();
            s.Red = ParseExpression();
            if (Peek().Type == TokenType.COMMA) { Advance();
                s.Green = ParseExpression();
                if (Peek().Type == TokenType.COMMA) { Advance();
                    s.Blue = ParseExpression();
                }
            }
        }
        return s;
    }

    Statement ParseDefFnStatement()
    {
        var t = Advance(); // skip DEF
        var s = new DefFnStatement(t.Line, t.Column);

        // 支持两种语法:
        // 1. DEF FNname(x) = expr (IDENTIFIER "FNname")
        // 2. DEF FN name(x) = expr (FN_KW + IDENTIFIER "name")
        string fullName;
        string funcName;

        if (Peek().Type == TokenType.IDENTIFIER)
        {
            string ident = Peek().Value;
            Advance();
            // 标识符必须以 "FN" 开头 (如 FNname, Fnname)
            if (!ident.StartsWith("FN", System.StringComparison.OrdinalIgnoreCase))
                return null;
            fullName = ident;
            funcName = ident.Substring(2);
        }
        else if (Peek().Type == TokenType.FN_KW)
        {
            Advance(); // skip FN
            fullName = "FN" + Peek().Value;
            funcName = Peek().Value;
            if (Peek().Type != TokenType.IDENTIFIER)
                return null;
            Advance(); // skip name
        }
        else
        {
            return null;
        }

        s.FullFnName = fullName;
        s.FunctionName = funcName;

        // 解析 (params)
        if (Peek().Type != TokenType.LPAREN)
            return null;
        Advance(); // skip (

        // 解析参数列表 (单个或多个)
        if (Peek().Type != TokenType.RPAREN)
        {
            if (Peek().Type == TokenType.IDENTIFIER)
            {
                s.Parameters.Add(new ParameterNode(Peek().Line, Peek().Column, Peek().Value));
                Advance();
            }
        }
        if (Peek().Type != TokenType.RPAREN)
            return null;
        Advance(); // skip )

        // 期望 =
        if (Peek().Type != TokenType.EQUALS)
            return null;
        Advance(); // skip =

        // 解析表达式
        s.BodyExpression = ParseExpression();

        return s;
    }

    /// <summary>
    /// 解析 DEF SEG [= address]
    /// QBasic 内存段设置语句 — 本编译器跳过（不生成代码）
    /// </summary>
    Statement ParseDefSegStatement()
    {
        Advance();
        Advance();
        if (Peek().Type == TokenType.EQUALS)
        {
            Advance();
            ParseExpression();
        }
        return null;
    }
}
