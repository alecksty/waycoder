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
            if (!IsExpressionStart(Peek())) break;
            Expression expr = ParseExpression();
            if (expr != null) s.Expressions.Add(expr);
            if (Peek().Type == TokenType.COMMA || Peek().Type == TokenType.SEMICOLON)
                Advance();
            else
                break;
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
            if (Peek().Type == TokenType.IDENTIFIER)
            {
                s.Variables.Add(new Identifier(Peek().Line, Peek().Column, Peek().Value));
                Advance();
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

        // 读取数组名
        if (Peek().Type != TokenType.IDENTIFIER)
            return null;
        s.ArrayName = Peek().Value.ToLower();
        Advance();

        // 解析 (newsize)
        if (Peek().Type != TokenType.LPAREN)
            return null;
        Advance(); // skip (

        s.NewSize = ParseExpression();

        if (Peek().Type != TokenType.RPAREN)
            return null;
        Advance(); // skip )

        return s;
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
        }
        // Optional action: PSET, PRESET, AND, OR, XOR (PSET is NOT an IDENTIFIER — it has its own token type)
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
