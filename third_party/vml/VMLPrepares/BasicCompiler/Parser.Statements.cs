using CompilerBase;
using System.Collections.Generic;

namespace BasicCompiler
{
    public partial class Parser : ParserBase<Token, TokenType>
    {
        private PrintStatement ParsePrintStatement()
        {
            Token token = Advance(); // 跳过 PRINT
            PrintStatement stmt = new PrintStatement(token.Line, token.Column);

            while (!AtEnd() && Peek().Type != TokenType.EOF && Peek().Type != TokenType.COLON)
            {
                // 遇到下一行的行号则终止（行号与 PRINT 不在同一行）
                if (Peek().Type == TokenType.NUMBER && Peek().Line > token.Line)
                    break;
                // Stop if next token can't start an expression
                if (!IsExpressionStart(Peek()))
                    break;
                Expression expr = ParseExpression();
                if (expr != null)
                    stmt.Expressions.Add(expr);
                if (Peek().Type == TokenType.COMMA || Peek().Type == TokenType.SEMICOLON)
                {
                    Advance(); // 跳过逗号或分号
                }
                else
                {
                    break;
                }
            }

            return stmt;
        }

        private InputStatement ParseInputStatement()
        {
            Token token = Advance(); // 跳过 INPUT
            InputStatement stmt = new InputStatement(token.Line, token.Column);

            while (!AtEnd() && Peek().Type != TokenType.EOF && Peek().Type != TokenType.COLON)
            {
                if (Peek().Type == TokenType.IDENTIFIER)
                {
                    stmt.Variables.Add(new Identifier(Peek().Line, Peek().Column, Peek().Value));
                    Advance(); // 跳过变量名
                }
                if (Peek().Type == TokenType.COMMA)
                {
                    Advance(); // 跳过逗号
                }
                else
                {
                    break;
                }
            }

            return stmt;
        }

        private LetStatement ParseLetStatement()
        {
            Token token = Peek();
            if (token.Type == TokenType.LET)
            {
                Advance(); // 跳过 LET
                token = Peek();
            }

            if (token.Type != TokenType.IDENTIFIER)
            {
                return null;
            }

            LetStatement stmt = new LetStatement(token.Line, token.Column);
            
            // 解析变量或数组访问
            Expression variableExpr = ParsePrimary();
            stmt.Variable = variableExpr;
            
            if (Peek().Type != TokenType.EQUALS)
            {
                return null;
            }
            Advance(); // 跳过 =

            stmt.Expression = ParseExpression();
            return stmt;
        }

        /// <summary>
        /// **块式** THEN / ELSE 体：把语句一直收到 `ELSEIF` / `ELSE` / `END IF` 为止。
        ///
        /// ⚠ 此前根本没有这条路径 —— `THEN` 之后无论换不换行都只 `ParseStatement()` 收**一条**，
        ///   于是块 IF 里**只有第一条语句是条件执行的**，其余全被拍平成无序的兄弟语句
        ///   （紧跟 `END IF` 的收尾）**无条件执行**。实测：
        ///     `IF a = 1 THEN / x = 5 / y = 6 / END IF`，a = 0 ⇒ 输出 `0 6`（应为 `0 0`）。
        ///   连带 `ELSEIF` 也废了：它只有**紧邻**体语句时才被上面的链状 while 看见，
        ///   多语句体的 ELSEIF 落在外面 → `ELSEIF` 被 `default:` 逐 token 跳过、
        ///   后面的条件表达式被当成普通语句 ⇒ **整条分支变成死代码**。
        ///   这个洞一直被"沉默地跳过 token"盖着，直到 v0.96.282 裸调用不再静默丢才露出来
        ///   （`ELSEIF ty > …` 里的 `ty` 被当成 `CALL func_ty`）。
        ///
        /// 判据用 **token 行号**：THEN 之后换行 = 块式；同一行 = 单行 IF（`IF x THEN y = 1`）。
        /// 这是本前端唯一能区分两者的信息 —— 词法里没有换行 token。
        /// </summary>
        private Statement ParseBlockBody()
        {
            var seq = new SequenceStatement(Peek().Line, Peek().Column);
            while (!AtEnd())
            {
                if (Peek().Type == TokenType.ELSEIF || Peek().Type == TokenType.ELSE) break;
                if (Peek().Type == TokenType.END && current + 1 < tokens.Count
                    && tokens[current + 1].Type == TokenType.IF) break;
                if (Peek().Type == TokenType.COLON || Peek().Type == TokenType.NUMBER)
                {
                    Advance();
                    continue;
                }
                int guard = current;
                var s = ParseStatement();
                if (s != null) seq.Statements.Add(s);
                // 兜底：某个分支没推进游标就手工推进一格，否则整个编译卡死在这儿
                if (current == guard) Advance();
            }
            if (seq.Statements.Count == 0) return null!;
            return seq.Statements.Count == 1 ? seq.Statements[0] : seq;
        }

        private IfStatement ParseIfStatement()
        {
            Token token = Advance(); // 跳过 IF
            IfStatement stmt = new IfStatement(token.Line, token.Column);

            stmt.Condition = ParseExpression();

            if (Peek().Type != TokenType.THEN)
            {
                return null;
            }
            Token thenTok = Advance(); // 跳过 THEN

            bool blockThen = !AtEnd() && Peek().Line > thenTok.Line;
            // 只要**任何一个**分支走了块式（THEN/ELSEIF/ELSE 之后换行），这条 IF 就必须有
            // `END IF` 收尾 —— 单行 IF（`IF x THEN y = 1`）不需要。判据在函数末尾。
            bool blockForm = blockThen;
            stmt.ThenBranch = blockThen ? ParseBlockBody() : ParseStatement();

            // Handle colon-separated multi-statement THEN branch: IF x THEN a=1: b=2
            if (stmt.ThenBranch != null && Peek().Type == TokenType.COLON)
            {
                Advance(); // skip colon
                var seq = new SequenceStatement(stmt.ThenBranch.Line, stmt.ThenBranch.Column);
                seq.Statements.Add(stmt.ThenBranch);
                while (Peek().Type != TokenType.ELSE && Peek().Type != TokenType.ELSEIF &&
                       Peek().Type != TokenType.END && !AtEnd())
                {
                    var nextStmt = ParseStatement();
                    if (nextStmt != null) seq.Statements.Add(nextStmt);
                    if (Peek().Type == TokenType.COLON) { Advance(); continue; }
                    else break;
                }
                stmt.ThenBranch = seq;
            }

            // 处理 ELSEIF 链 — 变换为 ELSE 内嵌 IF
            IfStatement currentStmt = stmt;
            while (Peek().Type == TokenType.ELSEIF)
            {
                Advance(); // skip ELSEIF
                IfStatement elseifStmt = new IfStatement(token.Line, token.Column);
                elseifStmt.Condition = ParseExpression();
                if (Peek().Type != TokenType.THEN) break;
                Token elseifThen = Advance(); // skip THEN
                if (Peek().Line > elseifThen.Line) blockForm = true;
                elseifStmt.ThenBranch = (Peek().Line > elseifThen.Line)
                    ? ParseBlockBody() : ParseStatement();
                // Handle colon-separated multi-statement ELSEIF branch
                if (elseifStmt.ThenBranch != null && Peek().Type == TokenType.COLON)
                {
                    Advance();
                    var seq = new SequenceStatement(elseifStmt.ThenBranch.Line, elseifStmt.ThenBranch.Column);
                    seq.Statements.Add(elseifStmt.ThenBranch);
                    while (Peek().Type != TokenType.ELSE && Peek().Type != TokenType.ELSEIF &&
                           Peek().Type != TokenType.END && !AtEnd())
                    {
                        var nextStmt = ParseStatement();
                        if (nextStmt != null) seq.Statements.Add(nextStmt);
                        if (Peek().Type == TokenType.COLON) { Advance(); continue; }
                        else break;
                    }
                    elseifStmt.ThenBranch = seq;
                }
                currentStmt.ElseBranch = elseifStmt;
                currentStmt = elseifStmt;
            }

            if (Peek().Type == TokenType.ELSE)
            {
                Token elseTok = Advance(); // 跳过 ELSE
                // 与 THEN 同一判据：ELSE 之后换行 = 块式（多语句），同一行 = 单行 IF 的 ELSE
                if (Peek().Line > elseTok.Line) blockForm = true;
                currentStmt.ElseBranch = (Peek().Line > elseTok.Line)
                    ? ParseBlockBody() : ParseStatement();
                // Handle colon-separated multi-statement ELSE branch
                if (currentStmt.ElseBranch != null && Peek().Type == TokenType.COLON)
                {
                    Advance();
                    var seq = new SequenceStatement(currentStmt.ElseBranch.Line, currentStmt.ElseBranch.Column);
                    seq.Statements.Add(currentStmt.ElseBranch);
                    while (Peek().Type != TokenType.END && !AtEnd())
                    {
                        var nextStmt = ParseStatement();
                        if (nextStmt != null) seq.Statements.Add(nextStmt);
                        if (Peek().Type == TokenType.COLON) { Advance(); continue; }
                        else break;
                    }
                    currentStmt.ElseBranch = seq;
                }
            }

            // Consume END IF if present
            if (Peek().Type == TokenType.END && current + 1 < tokens.Count && tokens[current + 1].Type == TokenType.IF)
            {
                Advance(); // skip END
                Advance(); // skip IF
            }
            // ⚠ 从这里往下（EOF 处）**从前是一声不吭**：块式的 `IF ... THEN` 不写 `END IF`
            //   就结束文件 ⇒ 编译成功、退出码 0 —— 一份写了一半的 BASIC 程序被编成残程序。
            //   位置锚在 `IF` 那个词上（缺口就是它没被关上）。
            else if (blockForm)
                GccErrorAt("IF 块未闭合（缺少 'END IF'）", token, ErrorCode.Parser_SyntaxError);

            return stmt;
        }

        private GotoStatement ParseGotoStatement()
        {
            Token token = Advance(); // 跳过 GOTO
            GotoStatement stmt = new GotoStatement(token.Line, token.Column);

            if (Peek().Type == TokenType.NUMBER)
            {
                stmt.LineNumber = int.Parse(Peek().Value);
                Advance();
            }
            else if (Peek().Type == TokenType.IDENTIFIER)
            {
                // Will be converted to a label reference during codegen
                stmt.Label = Peek().Value.ToLower();
                stmt.IsLabel = true;
                Advance();
            }
            else
            {
                return null;
            }

            return stmt;
        }

        private GosubStatement ParseGosubStatement()
        {
            Token token = Advance(); // 跳过 GOSUB
            GosubStatement stmt = new GosubStatement(token.Line, token.Column);

            if (Peek().Type == TokenType.NUMBER)
            {
                stmt.LineNumber = int.Parse(Peek().Value);
                Advance();
            }
            else if (Peek().Type == TokenType.IDENTIFIER)
            {
                stmt.Label = Peek().Value.ToLower();
                stmt.IsLabel = true;
                Advance();
            }
            else
            {
                return null;
            }

            return stmt;
        }

        private ReturnStatement ParseReturnStatement()
        {
            Token token = Advance(); // 跳过 RETURN
            return new ReturnStatement(token.Line, token.Column);
        }

        private Statement ParseDimStatement()
        {
            Token token = Advance(); // 跳过 DIM

            DimStatement stmt = new DimStatement(token.Line, token.Column);

            // 处理 DIM SHARED
            if (Peek().Type == TokenType.SHARED)
            {
                Advance(); // skip SHARED
                stmt.IsShared = true;
            }

            // 解析变量名
            if (Peek().Type != TokenType.IDENTIFIER)
            {
                return null;
            }

            string varName = Peek().Value;
            Advance(); // 跳过变量名
            
            // 检查是否是字符串数组（以$结尾）
            bool isStringArray = false;
            if (varName.EndsWith("$"))
            {
                isStringArray = true;
                varName = varName.Substring(0, varName.Length - 1);
            }
            
            stmt.VariableName = varName;
            stmt.IsStringArray = isStringArray;
            
            // 检查是否为 DIM var AS TypeName
            if (Peek().Type == TokenType.AS)
            {
                return ParseDimAsStatement(token, varName);
            }
            
            // 如果不是左括号，则是简单变量声明 (DIM SHARED varname)
            if (Peek().Type != TokenType.LPAREN)
            {
                stmt.VariableName = varName;
                stmt.Size = 1;
                return stmt;
            }
            Advance(); // 跳过(
            
            // 解析数组维度 (支持TO语法: 1 TO 2, 支持表达式: MAXSNAKELENGTH-1)
            while (true)
            {
                // Parse lower bound (number, identifier, or expression)
                int lowerBound = 0;
                int upperBound;
                
                if (Peek().Type == TokenType.NUMBER)
                {
                    lowerBound = int.Parse(Peek().Value);
                    Advance();
                }
                else if (Peek().Type == TokenType.IDENTIFIER)
                {
                    // Identifier (could be a constant like MAXSNAKELENGTH)
                    // For now, record it as a dimension expression (store as 0 and note that it's non-numeric)
                    string identVal = Peek().Value;
                    Advance();
                    // Check if there's arithmetic after identifier
                    if (Peek().Type == TokenType.MINUS || Peek().Type == TokenType.PLUS ||
                        Peek().Type == TokenType.MULTIPLY || Peek().Type == TokenType.DIVIDE)
                    {
                        // Skip arithmetic operators and following number/identifier
                        Advance(); // skip operator
                        if (Peek().Type == TokenType.NUMBER)
                        {
                            lowerBound = int.Parse(Peek().Value);
                            Advance();
                        }
                        else if (Peek().Type == TokenType.IDENTIFIER)
                        {
                            Advance();
                        }
                    }
                    // 维度是个标识符 —— **查常量表**（v0.96.331 修）。
                    // ⚠ 从前这里写死 `lowerBound = 1` 当占位，注释说"实际大小在代码生成
                    //   阶段算"，而那个阶段**根本没人算** ⇒ `DIM terr(COLS)` 只分到 2 格，
                    //   一写就越界（LANDER 实测循环变量跑到 760、把数组和邻居变量一起写花）。
                    //   查不到（不是字面量 CONST / 顺序反了）就退回占位行为，不更糟。
                    if (_constValues.TryGetValue(identVal.ToLower(), out int constDim))
                        lowerBound = constDim;
                    else
                        lowerBound = 1;
                }
                else
                {
                    return null;
                }
                
                // Check for TO syntax: lower TO upper
                if (Peek().Type == TokenType.TO)
                {
                    Advance(); // skip TO
                    
                    if (Peek().Type == TokenType.NUMBER)
                    {
                        upperBound = int.Parse(Peek().Value);
                        Advance();
                    }
                    else if (Peek().Type == TokenType.IDENTIFIER)
                    {
                        Advance();
                        upperBound = lowerBound + 1; // placeholder
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    // No TO: the value is the upper bound, lower bound is 0
                    upperBound = lowerBound;
                    lowerBound = 0;
                }
                
                int dimSize = upperBound - lowerBound + 1;
                if (dimSize < 1) dimSize = 1;
                stmt.Dimensions.Add(dimSize);
                
                // 检查是逗号还是右括号
                if (Peek().Type == TokenType.COMMA)
                {
                    Advance(); // 跳过逗号，继续解析下一维度
                }
                else if (Peek().Type == TokenType.RPAREN)
                {
                    Advance(); // 跳过)
                    break;
                }
                else
                {
                    return null;
                }
            }
            
            // 兼容旧版单维度
            if (stmt.Dimensions.Count == 1)
            {
                stmt.Size = stmt.Dimensions[0];
            }
            else if (stmt.Dimensions.Count > 1)
            {
                stmt.Size = 1;
                foreach (int d in stmt.Dimensions)
                {
                    stmt.Size *= d;
                }
            }
            
            // 检查是否为 DIM arr(size) AS TypeName
            if (Peek().Type == TokenType.AS)
            {
                Advance(); // skip AS
                if (IsTypeNameToken(Peek()))
                {
                    stmt.TypeName = Peek().Value;
                    Advance();
                }
            }
            
            // Consume comma-separated DIM vars: DIM SHARED a(n), b(m), c AS Type
            while (Peek().Type == TokenType.COMMA)
            {
                Advance(); // skip comma
                if (Peek().Type == TokenType.IDENTIFIER)
                {
                    var extraName = Peek().Value;
                    Advance(); // skip name
                    // Skip array dimensions if present: ident(size) or ident(low TO high)
                    if (Peek().Type == TokenType.LPAREN)
                    {
                        Advance(); // skip (
                        int parenDepth = 1;
                        while (!AtEnd() && parenDepth > 0)
                        {
                            if (Peek().Type == TokenType.LPAREN) parenDepth++;
                            else if (Peek().Type == TokenType.RPAREN) parenDepth--;
                            if (parenDepth > 0) Advance();
                        }
                        if (Peek().Type == TokenType.RPAREN)
                            Advance(); // skip )
                    }
                    // Skip AS TypeName
                    if (Peek().Type == TokenType.AS)
                    {
                        Advance(); // skip AS
                        if (IsTypeNameToken(Peek()))
                            Advance(); // skip type name
                    }
                }
            }

            return stmt;
        }

        private ForStatement ParseForStatement()
        {
            Token token = Advance(); // 跳过 FOR
            ForStatement stmt = new ForStatement(token.Line, token.Column);

            if (Peek().Type != TokenType.IDENTIFIER)
            {
                return null;
            }

            stmt.Variable = new Identifier(Peek().Line, Peek().Column, Peek().Value);
            Advance(); // 跳过变量名

            if (Peek().Type != TokenType.EQUALS)
            {
                return null;
            }
            Advance(); // 跳过 =

            stmt.InitialValue = ParseExpression();

            if (Peek().Type != TokenType.TO)
            {
                return null;
            }
            Advance(); // 跳过 TO

            stmt.EndValue = ParseExpression();

            if (Peek().Type == TokenType.STEP)
            {
                Advance(); // 跳过 STEP
                stmt.StepValue = ParseExpression();
            }
            else
            {
                // 默认步长为 1
                stmt.StepValue = new NumberLiteral(token.Line, token.Column, 1);
            }

            // 解析循环体 — 仅 NEXT 终止
            while (!AtEnd() && Peek().Type != TokenType.NEXT)
            {
                while (Peek().Type == TokenType.NUMBER || Peek().Type == TokenType.COLON) Advance();
                if (Peek().Type == TokenType.NEXT) break;
                Statement bodyStmt = ParseStatement();
                if (bodyStmt != null)
                    stmt.Body.Add(bodyStmt);
            }

            // ⚠ `NEXT i` 里的循环变量**必须在这里吃掉**。
            //
            // 词法里没有换行 token，语句边界靠"读到什么关键字"来断。只 `Advance()` 掉 NEXT
            // 的话，紧跟的 `i` 会被外层语句循环当成**下一条语句**，而
            // `case TokenType.IDENTIFIER` 的兜底分支把「不是 `=` 的标识符」一律当裸调用
            // ⇒ 编出 `call func_i`，链接期报「未定义的函数 'func_i'」。
            //   **更糟的是它不报错也能"跑"**：链接器把 `func_i` 解析到同名的全局标签上，
            //   于是 `NEXT i` 变成一次**递归调用**，`FOR i = 0 TO 3` 无限循环
            //   （实测打出 1 2 3 0 1 2 3 …直到内存不足）。
            //
            // 因此这里的判据是「同一行、且是标识符」——避免把下一行的语句吃进来。
            if (Peek().Type == TokenType.NEXT)
            {
                int nextLine = Peek().Line;
                Advance(); // 跳过 NEXT
                while (!AtEnd() && Peek().Line == nextLine && Peek().Type == TokenType.IDENTIFIER)
                {
                    Advance(); // 跳过循环变量名
                    if (Peek().Type == TokenType.COMMA) { Advance(); continue; } // NEXT i, j
                    break;
                }
            }

            return stmt;
        }

        private WhileStatement ParseWhileStatement()
        {
            Token token = Advance(); // 跳过 WHILE
            WhileStatement stmt = new WhileStatement(token.Line, token.Column);

            stmt.Condition = ParseExpression();

            // 解析循环体 — 仅 WEND 终止
            while (!AtEnd() && Peek().Type != TokenType.WEND)
            {
                while (Peek().Type == TokenType.NUMBER || Peek().Type == TokenType.COLON) Advance();
                // 跳过数字/冒号后可能已经到达 WEND，必须在此处再检查，否则 ParseStatement 的 default 分支会消费它
                if (Peek().Type == TokenType.WEND) break;
                Statement bodyStmt = ParseStatement();
                if (bodyStmt != null)
                    stmt.Body.Add(bodyStmt);
            }

            if (Peek().Type == TokenType.WEND)
                Advance(); // 跳过 WEND

            return stmt;
        }

        private DoLoopStatement ParseDoLoopStatement()
        {
            Token doToken = Advance(); // 跳过 DO

            var stmt = new DoLoopStatement(doToken.Line, doToken.Column);

            // Handle DO WHILE / DO UNTIL (pre-test condition)
            if (Peek().Type == TokenType.WHILE)
            {
                Advance(); // 跳过 WHILE
                stmt.Condition = ParseExpression();
                stmt.IsUntil = false;
                stmt.HasCondition = true;
                stmt.IsPreTest = true;
            }
            else if (Peek().Type == TokenType.UNTIL)
            {
                Advance(); // 跳过 UNTIL
                stmt.Condition = ParseExpression();
                stmt.IsUntil = true;
                stmt.HasCondition = true;
                stmt.IsPreTest = true;
            }

            // 解析循环体 — 仅 LOOP 终止
            while (!AtEnd() && Peek().Type != TokenType.LOOP)
            {
                while (Peek().Type == TokenType.NUMBER || Peek().Type == TokenType.COLON) Advance();
                if (Peek().Type == TokenType.LOOP) break;
                Statement bodyStmt = ParseStatement();
                if (bodyStmt != null)
                    stmt.Body.Add(bodyStmt);
            }

            if (Peek().Type == TokenType.LOOP)
            {
                Advance(); // 跳过 LOOP

                // LOOP WHILE / LOOP UNTIL (post-test condition)
                if (Peek().Type == TokenType.UNTIL)
                {
                    Advance(); // 跳过 UNTIL
                    stmt.Condition = ParseExpression();
                    stmt.IsUntil = true;
                    stmt.HasCondition = true;
                }
                else if (Peek().Type == TokenType.WHILE)
                {
                    Advance(); // 跳过 WHILE
                    stmt.Condition = ParseExpression();
                    stmt.IsUntil = false;
                    stmt.HasCondition = true;
                }
            }

            return stmt;
        }

        private EndStatement ParseEndStatement()
        {
            Token token = Advance(); // 跳过 END
            return new EndStatement(token.Line, token.Column);
        }

        private Statement ParseConstStatement()
        {
            Token token = Advance(); // 跳过 CONST
            ConstStatement stmt = new ConstStatement(token.Line, token.Column);
            List<ConstStatement> extraConsts = new List<ConstStatement>();

            if (Peek().Type != TokenType.IDENTIFIER)
                return null;

            stmt.Name = Peek().Value;
            Advance(); // 跳过变量名

            if (Peek().Type != TokenType.EQUALS)
                return null;
            Advance(); // 跳过 =

            stmt.Value = ParseExpression();
            // 记进解析期常量表 —— `DIM a(N)` 的维度要用（见 `_constValues` 的说明）。
            // 用 `TryEvalConst` 而不是 `is NumberLiteral`：`CONST GCELLS = GW * GH`
            // 这种**引用别的常量**的写法同样要记进去（只收字面量的话它进不了表，
            // `DIM bd(GCELLS)` 就分不到格子）。
            if (TryEvalConst(stmt.Value, out int constVal))
            {
                _constValues[stmt.Name.ToLower()] = constVal;
                // ⚠ **就地换成字面量**（v0.96.331）：常量表只有解析期这一张，
                //   而**代码生成期**用的是它自己那份（从 ConstStatement 的 Value 建的）。
                //   `CONST GCELLS = GW * GH` 这种表达式进不了那份表 ⇒ 生成期读 `GCELLS`
                //   得 0（实测：`DIM bd(GCELLS)` 尺寸对了、而 `WHILE i < GCELLS` 一次都不跑）。
                //   换成字面量之后两边**同源**，不用再让代码生成也实现一遍折叠。
                stmt.Value = new NumberLiteral(stmt.Value.Line, stmt.Value.Column, constVal);
            }

            // Handle comma-separated CONST declarations: CONST a=1, b=2, c=3
            while (Peek().Type == TokenType.COMMA)
            {
                Advance(); // skip comma
                if (Peek().Type != TokenType.IDENTIFIER)
                    break;
                var extraConst = new ConstStatement(token.Line, token.Column);
                extraConst.Name = Peek().Value;
                Advance(); // skip name
                if (Peek().Type != TokenType.EQUALS)
                    break;
                Advance(); // skip =
                extraConst.Value = ParseExpression();
                if (TryEvalConst(extraConst.Value, out int extraVal))
                    _constValues[extraConst.Name.ToLower()] = extraVal;
                extraConsts.Add(extraConst);
            }
            
            // If there are extra consts, return a MultiStatement wrapping all of them
            // (The program doesn't support MultiStatement directly, so we return the first
            // and the parser's main loop will handle the rest since we've consumed the tokens)
            // Actually just return the first one — the extra consts will be parsed by the main loop
            // since ParseExpression consumed the value tokens.
            return stmt;
        }

        private Statement ParseExitStatement()
        {
            Token exitToken = Advance(); // 跳过 EXIT
            
            if (AtEnd())
            {
                throw Error($"语法错误: EXIT 后缺少 FOR/WHILE/SUB/FUNCTION，第{exitToken.Line}行");
            }
            
            Token next = Peek();
            if (next.Type == TokenType.FOR)
            {
                Advance(); // 跳过 FOR
                return new ExitLoopStatement(exitToken.Line, exitToken.Column);
            }
            else if (next.Type == TokenType.WHILE)
            {
                Advance(); // 跳过 WHILE
                return new ExitLoopStatement(exitToken.Line, exitToken.Column);
            }
            else if (next.Type == TokenType.DO)
            {
                Advance(); // 跳过 DO
                return new ExitLoopStatement(exitToken.Line, exitToken.Column);
            }
            else if (next.Type == TokenType.SUB || next.Type == TokenType.FUNCTION)
            {
                Advance(); // 跳过 SUB/FUNCTION
                return new ExitSubStatement(exitToken.Line, exitToken.Column);
            }
            else
            {
                throw Error($"语法错误: EXIT 后期望 FOR/WHILE/SUB/FUNCTION，但得到 '{next.Value}'，第{next.Line}行");
            }
        }

        /// <summary>
        /// 这个 token 的文本能不能当**类型名**用。
        ///
        /// ⚠ 不能只认 `IDENTIFIER` —— 词法表里有 `Integer` / `String` 这些
        ///   **VB 风格关键字**（`TokenType.VB_INTEGER` / `VB_STRING`），
        ///   而词法表是**大小写敏感**的：`AS INTEGER`（全大写）落到 IDENTIFIER、
        ///   `AS Integer`（首字母大写，标准写法）落到 VB_INTEGER。
        ///   于是 `DIM x AS Integer` 在 `ParseDimAsStatement` 里 `return null`
        ///   ⇒ **整条 DIM 被静默丢掉**（语句层对 null 是跳过），变量退化成隐式全局整数。
        ///   实测：`DIM s AS String` 之后 `s = "hello"`、`PRINT s` 打出 **1024**（一个栈地址）。
        /// </summary>
        private static bool IsTypeNameToken(Token t)
            => (t.Type == TokenType.IDENTIFIER || t.Type == TokenType.VB_INTEGER
                || t.Type == TokenType.VB_STRING || t.Type == TokenType.VB_OBJECT
                || t.Type == TokenType.VB_VARIANT)
               && !string.IsNullOrEmpty(t.Value);

        private DimAsStatement ParseDimAsStatement(Token dimToken, string varName)
        {
            // Already consumed DIM and variable name, now on AS
            Advance(); // skip AS

            if (!IsTypeNameToken(Peek()))
                return null;

            string typeName = Peek().Value;
            Advance();

            return new DimAsStatement(dimToken.Line, dimToken.Column)
            {
                VariableName = varName,
                TypeName = typeName
            };
        }

        private Statement ParseTypeDeclaration()
        {
            Token token = Advance(); // skip TYPE
            if (Peek().Type != TokenType.IDENTIFIER)
                return null;

            string typeName = Peek().Value;
            Advance();

            TypeDeclaration typeDecl = new TypeDeclaration(token.Line, token.Column)
            {
                Name = typeName
            };

            // Parse fields until END TYPE
            while (!AtEnd())
            {
                if (Peek().Type == TokenType.END)
                {
                    Advance(); // skip END
                    if (Peek().Type == TokenType.TYPE_KW)
                    {
                        Advance(); // skip TYPE
                        break;
                    }
                    // Put back if not END TYPE
                    break;
                }

                if (Peek().Type != TokenType.IDENTIFIER)
                {
                    Advance(); // skip unknown
                    continue;
                }

                string fieldName = Peek().Value;
                Advance();

                // Expect AS keyword
                if (Peek().Type != TokenType.AS)
                    continue;
                Advance(); // skip AS

                TypeField field = new TypeField(token.Line, token.Column)
                {
                    Name = fieldName
                };

                // Parse type: INTEGER, SINGLE, STRING [* n]
                if (Peek().Type == TokenType.IDENTIFIER)
                {
                    string ftype = Peek().Value.ToUpper();
                    field.FieldType = ftype;
                    Advance();

                    if (ftype == "STRING" && Peek().Type == TokenType.MULTIPLY)
                    {
                        Advance(); // skip *
                        if (Peek().Type == TokenType.NUMBER)
                        {
                            field.StringLength = int.Parse(Peek().Value);
                            Advance();
                        }
                    }
                }

                typeDecl.Fields.Add(field);
            }

            return typeDecl;
        }

        /// <summary>CLASS name ... END CLASS — FreeBasic OOP (v1.66.31+)</summary>
        private Statement ParseClassDeclaration()
        {
            Token token = Advance(); // skip CLASS
            if (Peek().Type != TokenType.IDENTIFIER)
                return new ClassDeclaration(token.Line, token.Column) { Name = "?" };
            string name = Peek().Value;
            Advance();
            var classDecl = new ClassDeclaration(token.Line, token.Column) { Name = name };

            while (!AtEnd())
            {
                // END CLASS / END CONSTRUCTOR / END METHOD
                if (Peek().Type == TokenType.END)
                {
                    Advance(); // skip END
                    if (Peek().Type == TokenType.CLASS_KW)
                    {
                        Advance(); // skip CLASS
                        break;
                    }
                    // END without CLASS at top level — break for error recovery
                    break;
                }
                // CONSTRUCTOR
                if (Peek().Type == TokenType.CONSTRUCTOR)
                {
                    Advance(); // skip CONSTRUCTOR
                    classDecl.ConstructorBody = new List<Statement>();
                    while (!AtEnd())
                    {
                        if (Peek().Type == TokenType.END)
                        {
                            Advance(); // skip END
                            if (Peek().Type == TokenType.CONSTRUCTOR)
                            {
                                Advance(); // skip CONSTRUCTOR
                                break;
                            }
                            // END CLASS without END CONSTRUCTOR — terminate
                            if (Peek().Type == TokenType.CLASS_KW)
                            {
                                Advance();
                                goto classEnd;
                            }
                            break;
                        }
                        var stmt = ParseStatement();
                        if (stmt != null) classDecl.ConstructorBody.Add(stmt);
                    }
                    continue;
                }
                // DESTRUCTOR
                if (Peek().Type == TokenType.DESTRUCTOR)
                {
                    Advance(); // skip DESTRUCTOR
                    classDecl.DestructorBody = new List<Statement>();
                    while (!AtEnd())
                    {
                        if (Peek().Type == TokenType.END)
                        {
                            Advance(); // skip END
                            if (Peek().Type == TokenType.DESTRUCTOR)
                            {
                                Advance(); // skip DESTRUCTOR
                                break;
                            }
                            if (Peek().Type == TokenType.CLASS_KW)
                            {
                                Advance();
                                goto classEnd;
                            }
                            break;
                        }
                        var stmt = ParseStatement();
                        if (stmt != null) classDecl.DestructorBody.Add(stmt);
                    }
                    continue;
                }
                // Field or Method
                if (Peek().Type == TokenType.IDENTIFIER)
                {
                    string fieldName = Peek().Value;
                    Advance();
                    if (Peek().Type == TokenType.LPAREN)
                    {
                        // Method: name(params)
                        Advance(); // skip (
                        var method = new MethodDeclaration(token.Line, token.Column) { Name = fieldName };
                        while (Peek().Type != TokenType.RPAREN && !AtEnd())
                        {
                            if (Peek().Type == TokenType.IDENTIFIER)
                                method.Parameters.Add(Peek().Value);
                            Advance();
                        }
                        if (Peek().Type == TokenType.RPAREN) Advance(); // skip )
                        // Parse method body until END METHOD or END CLASS
                        while (!AtEnd())
                        {
                            if (Peek().Type == TokenType.END)
                            {
                                Advance(); // skip END
                                if (Peek().Type == TokenType.METHOD_KW)
                                {
                                    Advance(); // skip METHOD
                                    break;
                                }
                                // END CLASS without END METHOD — terminate
                                if (Peek().Type == TokenType.CLASS_KW)
                                {
                                    Advance();
                                    goto classEnd;
                                }
                                break;
                            }
                            var stmt = ParseStatement();
                            if (stmt != null) method.Body.Add(stmt);
                        }
                        classDecl.Methods.Add(method);
                        continue;
                    }
                    if (Peek().Type == TokenType.AS)
                    {
                        Advance(); // skip AS
                        if (Peek().Type != TokenType.IDENTIFIER)
                            continue;
                        string ftype = Peek().Value;
                        Advance();
                        classDecl.Fields.Add(new TypeField(token.Line, token.Column) { Name = fieldName, FieldType = ftype });
                        continue;
                    }
                }
                Advance(); // skip unknown token
            }
        classEnd:
            return classDecl;
        }

        // ----- Turbo Basic 扩展解析方法 -----

        private LocalDeclaration ParseLocalDeclaration()
        {
            Token token = Advance(); // skip LOCAL
            LocalDeclaration stmt = new LocalDeclaration(token.Line, token.Column);
            // LOCAL var1, var2, var3 ...
            while (true)
            {
                if (Peek().Type != TokenType.IDENTIFIER) break;
                stmt.Variables.Add(new Identifier(Peek().Line, Peek().Column, Peek().Value));
                Advance();
                if (Peek().Type != TokenType.COMMA) break;
                Advance(); // skip comma
            }
            return stmt;
        }

        private StaticDeclaration ParseStaticDeclaration()
        {
            Token token = Advance(); // skip STATIC
            StaticDeclaration stmt = new StaticDeclaration(token.Line, token.Column);
            // STATIC var1, var2, var3 ...
            while (true)
            {
                if (Peek().Type != TokenType.IDENTIFIER) break;
                stmt.Variables.Add(new Identifier(Peek().Line, Peek().Column, Peek().Value));
                Advance();
                if (Peek().Type != TokenType.COMMA) break;
                Advance(); // skip comma
            }
            return stmt;
        }

        private SharedStatement ParseSharedStatement()
        {
            Token token = Advance(); // skip SHARED
            SharedStatement stmt = new SharedStatement(token.Line, token.Column);
            // SHARED var1, var2, var3 ...
            // SHARED var AS Type [, var2 AS Type ...]
            while (true)
            {
                if (Peek().Type != TokenType.IDENTIFIER) break;
                stmt.Variables.Add(new Identifier(Peek().Line, Peek().Column, Peek().Value));
                Advance();
                // 跳过 AS type
                if (Peek().Type == TokenType.AS)
                {
                    Advance(); // skip AS
                    if (Peek().Type == TokenType.IDENTIFIER)
                        Advance(); // skip type name
                }
                if (Peek().Type != TokenType.COMMA) break;
                Advance(); // skip comma
            }
            return stmt;
        }

        private CommonStatement ParseCommonStatement()
        {
            Token token = Advance(); // skip COMMON
            CommonStatement stmt = new CommonStatement(token.Line, token.Column);
            // COMMON var1, var2, var3 ...
            // COMMON /blockname/ var1, var2 (skip block name if present)
            if (Peek().Type == TokenType.DIVIDE)
            {
                Advance(); // skip opening /
                if (Peek().Type == TokenType.IDENTIFIER)
                    Advance(); // skip block name
                if (Peek().Type == TokenType.DIVIDE)
                    Advance(); // skip closing /
            }
            while (true)
            {
                if (Peek().Type != TokenType.IDENTIFIER) break;
                stmt.VariableNames.Add(Peek().Value);
                Advance();
                if (Peek().Type != TokenType.COMMA) break;
                Advance(); // skip comma
            }
            return stmt;
        }

        /// <summary>
        /// 解析 DEFSNG/DEFINT/DEFSTR letter[-letter][, letter[-letter]]...
        /// </summary>
        private DefTypeStatement ParseDefTypeStatement()
        {
            Token token = Advance(); // consume DEFSNG/DEFINT/DEFSTR
            DefTypeStatement stmt = new DefTypeStatement(token.Line, token.Column);
            stmt.TypeName = token.Value.ToUpper();

            // 解析字母范围，例如: A-Z, A-C, F, M-R
            while (true)
            {
                if (AtEnd()) break;
                // 检查是否是逗号（分隔多个范围）
                if (Peek().Type == TokenType.COMMA)
                {
                    Advance(); // skip comma
                    continue;
                }
                // 期望一个字母标识符
                if (Peek().Type != TokenType.IDENTIFIER) break;
                string letterStr = Peek().Value.ToUpper();
                if (letterStr.Length != 1 || letterStr[0] < 'A' || letterStr[0] > 'Z')
                    break; // 不是单字母，停止解析
                char startLetter = letterStr[0];
                Advance(); // consume letter

                char endLetter = startLetter; // 默认单字母
                // 检查是否有 '-' 后跟另一个字母
                if (!AtEnd() && Peek().Type == TokenType.MINUS)
                {
                    Advance(); // consume '-'
                    if (!AtEnd() && Peek().Type == TokenType.IDENTIFIER)
                    {
                        string endStr = Peek().Value.ToUpper();
                        if (endStr.Length == 1 && endStr[0] >= 'A' && endStr[0] <= 'Z')
                        {
                            endLetter = endStr[0];
                            Advance(); // consume end letter
                        }
                    }
                }

                stmt.Ranges.Add((startLetter, endLetter));

                // 如果没有逗号，结束
                if (AtEnd() || Peek().Type != TokenType.COMMA)
                    break;
            }

            return stmt;
        }

        private OptionBaseStatement ParseOptionBaseStatement()
        {
            Token token = Advance(); // skip OPTION
            OptionBaseStatement stmt = new OptionBaseStatement(token.Line, token.Column);
            // OPTION BASE n — expect IDENTIFIER "BASE" then number
            if (Peek().Type == TokenType.IDENTIFIER && Peek().Value.ToUpper() == "BASE")
            {
                Advance(); // skip BASE
                if (Peek().Type == TokenType.NUMBER)
                {
                    stmt.Base = int.Parse(Peek().Value);
                    Advance();
                }
            }
            // `OPTION EXPLICIT` —— 从此**变量必须先声明**。
            //
            // ⚠ 此前这里只认 `BASE`，别的 OPTION 一律**静默吞掉**（整个 `EXPLICIT` 当没看见）
            //   ⇒ 写了 `OPTION EXPLICIT` 的程序里引用未声明变量，照样被"隐式建个全局"兜住，
            //   指令形同虚设。现在落一个标志位交给代码生成：QBasic 的**默认**语义就是
            //   "未声明即隐式全局"（合法），只有写了这条指令才该报错。
            else if (Peek().Type == TokenType.IDENTIFIER && Peek().Value.ToUpper() == "EXPLICIT")
            {
                Advance(); // skip EXPLICIT
                OptionExplicit = true;
            }
            return stmt;
        }

        private Statement ParsePrintUsingStatement()
        {
            Token token = Advance(); // skip PRINT
            Advance(); // skip USING
            PrintUsingStatement stmt = new PrintUsingStatement(token.Line, token.Column);

            // Parse format string
            stmt.Format = ParseExpression();

            // Parse values separated by semicolons
            while (Peek().Type == TokenType.SEMICOLON)
            {
                Advance(); // skip ;
                if (AtEnd() || Peek().Type == TokenType.COLON || Peek().Type == TokenType.EOF)
                    break;
                if (IsExpressionStart(Peek()))
                {
                    Expression val = ParseExpression();
                    if (val != null) stmt.Values.Add(val);
                }
            }

            return stmt;
        }
    }
}
