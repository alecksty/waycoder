using CompilerBase;
using System.Collections.Generic;

namespace BasicCompiler
{
    public partial class Parser : ParserBase<Token, TokenType>
    {
        private SubDeclaration ParseSubDeclaration()
        {
            Token token = Advance(); // 跳过 SUB
            if (Peek().Type != TokenType.IDENTIFIER)
            {
                return null;
            }
            string name = Peek().Value;
            Advance();

            SubDeclaration sub = new SubDeclaration(token.Line, token.Column, name);

            // 解析参数列表
            if (Peek().Type == TokenType.LPAREN)
            {
                Advance(); // 跳过 (
                while (Peek().Type != TokenType.RPAREN && !AtEnd())
                {
                    bool isByRef = false;  // 默认 BYVAL (MCU 模式下字面量参数无法传引用)
                    if (Peek().Type == TokenType.BYREF)
                    {
                        isByRef = true;
                        Advance();
                    }
                    else if (Peek().Type == TokenType.BYVAL)
                    {
                        isByRef = false;
                        Advance();
                    }

                    // ⚠ 形参名**接受关键字 token**（取它的文本当名字），不能要求 IDENTIFIER。
                    //
                    // 两件事一起逼出来的：
                    //   · `return null` 在调用方（Parser.Core 的 `case TokenType.FUNCTION`）只表示
                    //     「这条语句没解析出来」⇒ **整条声明被静默丢掉**，而词法位置还停在形参列表
                    //     **中间** ⇒ 后面的 token 全被当成顶层语句继续解析。拿 `on` 当形参名时，
                    //     编出来的东西运行期报 **`内存不足，无法分配!`**，屏幕上没有一个字提到真正的错处。
                    //   · 但改成一律报错**会打掉本来能用的写法**：`Examples/basic/sysinfo.bas` 里
                    //     `NATIVE FUNCTION ui_call_json_s(fn AS STRING, …)` 的 `fn` 就是 TokenType.FN，
                    //     而它**声明里根本用不到形参**（NATIVE 无函数体）⇒ 以前丢掉声明也照样跑。
                    // 所以这里的判据是「**这个 token 的文本能不能当名字**」，与它是不是关键字无关。
                    if (Peek().Type == TokenType.EOF || Peek().Type == TokenType.RPAREN ||
                        Peek().Type == TokenType.COMMA || string.IsNullOrEmpty(Peek().Value))
                    {
                        throw Error($"形参名缺失（第 {Peek().Line} 行）");
                    }
                    string paramName = Peek().Value;
                    Advance();

                    // Skip array parens: sammy() or Record()
                    if (Peek().Type == TokenType.LPAREN)
                    {
                        Advance(); // skip (
                        int pdepth = 1;
                        while (!AtEnd() && pdepth > 0)
                        {
                            if (Peek().Type == TokenType.LPAREN) pdepth++;
                            else if (Peek().Type == TokenType.RPAREN) pdepth--;
                            if (pdepth > 0) Advance();
                        }
                        if (Peek().Type == TokenType.RPAREN)
                            Advance(); // skip )
                    }

                    bool isString = false;
                    if (Peek().Type == TokenType.AS)
                    {
                        Advance(); // skip AS
                        if (Peek().Type == TokenType.IDENTIFIER)
                        {
                            var typeName = Peek().Value.ToUpper();
                            if (typeName == "STRING")
                                isString = true;
                            Advance(); // skip type name
                        }
                        else
                        {
                            // AS followed by something unknown, skip it
                            Advance();
                        }
                    }
                    else if (Peek().Type == TokenType.EQUALS)
                    {
                        // Legacy: parameter = value syntax (not standard QBasic but kept for compatibility)
                        Advance();
                        if (Peek().Type == TokenType.IDENTIFIER && Peek().Value.ToUpper() == "STRING")
                        {
                            isString = true;
                            Advance();
                        }
                    }
                    else if (paramName.EndsWith("$"))
                    {
                        isString = true;
                        paramName = paramName.Substring(0, paramName.Length - 1);
                    }

                    sub.Parameters.Add(new ParameterNode(token.Line, token.Column, paramName, isByRef, isString));

                    if (Peek().Type == TokenType.COMMA)
                    {
                        Advance();
                    }
                }
                if (Peek().Type == TokenType.RPAREN)
                {
                    Advance();
                }
            }

            // ── NATIVE 声明**没有体**，别去解析体 ────────────────────────────────
            //
            // 体循环是 `while (!AtEnd())` 直到配对的那个 END {SUB,FUNCTION}。声明写成
            // `NATIVE FUNCTION f() AS INTEGER` 而**没写 END FUNCTION** 时，它一路吃到
            // EOF —— 于是**该声明后面整个程序都被当成了它的体**。
            //
            // 为什么后果是「零输出」而不是报错：NATIVE 的体**本来就不生成代码**
            // （`CodeGenerator.Sub.cs` 的 GenerateSub/FunctionDeclaration 开头就 `if (IsNative) return;`），
            // 被吞进去的语句连一个 `line_N` 标签都拿不到。主程序一条语句都不剩 ⇒
            // 编出来的是一份**完全合法、什么也不做**的程序，退出码 0、屏幕上没有一个字。
            // 实测 `Examples/basic/sysinfo.bas`：`statements=1 FunctionDeclaration=1`。
            //
            // 判据是「是不是 NATIVE」而不是「有没有 END」：NATIVE 就是**外部符号声明**，
            // 它没有「体」这个概念，写不写 END 都不该吃后面的语句。
            // 紧跟的 END 仍旧吃掉 —— `Examples/basic/whack.bas` 等既有例子写的是
            // `NATIVE SUB f(...)` + `END SUB` 的**空体**写法（那是绕这个 bug 的土办法），
            // 留着它两种写法都能编。

            if (_pendingNative)
            {
                if (Peek().Type == TokenType.END && current + 1 < tokens.Count && tokens[current + 1].Type == TokenType.SUB)
                {
                    Advance(); // skip END
                    Advance(); // skip SUB
                }
                return sub;
            }

            // 解析子程序体直到 END SUB
            while (!AtEnd())
            {
                // Check for END SUB (two-token sequence)
                if (Peek().Type == TokenType.END && current + 1 < tokens.Count && tokens[current + 1].Type == TokenType.SUB)
                {
                    Advance(); // skip END
                    Advance(); // skip SUB
                    break;
                }
                Statement bodyStmt = ParseStatement();
                if (bodyStmt != null)
                {
                    sub.Body.Add(bodyStmt);
                }
            }

            // 跳过 END SUB
            if (Peek().Type == TokenType.END)
            {
                Advance();
                if (Peek().Type == TokenType.SUB)
                {
                    Advance();
                }
            }

            return sub;
        }

        private FunctionDeclaration ParseFunctionDeclaration()
        {
            Token token = Advance(); // 跳过 FUNCTION
            if (Peek().Type != TokenType.IDENTIFIER)
            {
                return null;
            }
            string name = Peek().Value;
            bool isStringFunction = name.EndsWith("$");
            if (isStringFunction)
            {
                name = name.Substring(0, name.Length - 1);
            }
            Advance();

            FunctionDeclaration func = new FunctionDeclaration(token.Line, token.Column, name);
            func.IsStringFunction = isStringFunction;

            // 解析参数列表
            if (Peek().Type == TokenType.LPAREN)
            {
                Advance(); // 跳过 (
                while (Peek().Type != TokenType.RPAREN && !AtEnd())
                {
                    bool isByRef = false;  // 默认 BYVAL (MCU 模式下字面量参数无法传引用)
                    if (Peek().Type == TokenType.BYREF)
                    {
                        isByRef = true;
                        Advance();
                    }
                    else if (Peek().Type == TokenType.BYVAL)
                    {
                        isByRef = false;
                        Advance();
                    }

                    // ⚠ 形参名**接受关键字 token**（取它的文本当名字），不能要求 IDENTIFIER。
                    //
                    // 两件事一起逼出来的：
                    //   · `return null` 在调用方（Parser.Core 的 `case TokenType.FUNCTION`）只表示
                    //     「这条语句没解析出来」⇒ **整条声明被静默丢掉**，而词法位置还停在形参列表
                    //     **中间** ⇒ 后面的 token 全被当成顶层语句继续解析。拿 `on` 当形参名时，
                    //     编出来的东西运行期报 **`内存不足，无法分配!`**，屏幕上没有一个字提到真正的错处。
                    //   · 但改成一律报错**会打掉本来能用的写法**：`Examples/basic/sysinfo.bas` 里
                    //     `NATIVE FUNCTION ui_call_json_s(fn AS STRING, …)` 的 `fn` 就是 TokenType.FN，
                    //     而它**声明里根本用不到形参**（NATIVE 无函数体）⇒ 以前丢掉声明也照样跑。
                    // 所以这里的判据是「**这个 token 的文本能不能当名字**」，与它是不是关键字无关。
                    if (Peek().Type == TokenType.EOF || Peek().Type == TokenType.RPAREN ||
                        Peek().Type == TokenType.COMMA || string.IsNullOrEmpty(Peek().Value))
                    {
                        throw Error($"形参名缺失（第 {Peek().Line} 行）");
                    }
                    string paramName = Peek().Value;
                    Advance();

                    // Skip array parens: sammy() or Record()
                    if (Peek().Type == TokenType.LPAREN)
                    {
                        Advance(); // skip (
                        int pdepth = 1;
                        while (!AtEnd() && pdepth > 0)
                        {
                            if (Peek().Type == TokenType.LPAREN) pdepth++;
                            else if (Peek().Type == TokenType.RPAREN) pdepth--;
                            if (pdepth > 0) Advance();
                        }
                        if (Peek().Type == TokenType.RPAREN)
                            Advance(); // skip )
                    }

                    bool isString = false;
                    if (Peek().Type == TokenType.AS)
                    {
                        Advance(); // skip AS
                        if (Peek().Type == TokenType.IDENTIFIER)
                        {
                            var typeName = Peek().Value.ToUpper();
                            if (typeName == "STRING")
                                isString = true;
                            Advance(); // skip type name
                        }
                        else
                        {
                            Advance(); // skip unknown
                        }
                    }
                    else if (Peek().Type == TokenType.EQUALS)
                    {
                        Advance();
                        if (Peek().Type == TokenType.IDENTIFIER && Peek().Value.ToUpper() == "STRING")
                        {
                            isString = true;
                            Advance();
                        }
                    }
                    else if (paramName.EndsWith("$"))
                    {
                        isString = true;
                        paramName = paramName.Substring(0, paramName.Length - 1);
                    }

                    func.Parameters.Add(new ParameterNode(token.Line, token.Column, paramName, isByRef, isString));

                    if (Peek().Type == TokenType.COMMA)
                    {
                        Advance();
                    }
                }
                if (Peek().Type == TokenType.RPAREN)
                {
                    Advance();
                }
            }

            // 返回类型 `AS <类型>` —— **必须在这里吃掉**。
            //
            // ⚠ 此前这里直接进下面的「函数体」循环，`AS` 后面的类型名就留在 token 流里，
            //   被当成了函数体的第一个语句：
            //     · 改之前：那个 IDENTIFIER 落进 `ParseLetStatement()`，没有 `=` 就
            //       `return null` ⇒ **静默丢掉**。表面上「没事」（NATIVE 声明的体本来
            //       也不生成代码），实际是漏了一个 token；
            //     · 改之后（裸调用不再静默丢）：它被当成 `CALL func_integer` ⇒
            //       `NATIVE FUNCTION f() AS INTEGER` 后面**跟任何语句都编译不过**。
            //   两种表现都不是「对」，区别只是漏得响不响。正解是在这里就把它解析掉。
            //   实测最小复现：`NATIVE FUNCTION f() AS INTEGER` + 换行 + `NATIVE SUB g()`。
            //
            // 顺带把 `AS STRING` 接上 `IsStringFunction` —— 此前只有名字带 `$` 后缀
            // 才能标记字符串返回值，写 `AS STRING` 的会被 `CodeGenerator.Sub.cs:121`
            // 当成 INTEGER 处理（那里按这个字段二选一决定返回类型）。
            if (Peek().Type == TokenType.AS)
            {
                Advance(); // skip AS
                bool isStringRet = Peek().Type == TokenType.VB_STRING
                    || string.Equals(Peek().Value as string, "STRING", StringComparison.OrdinalIgnoreCase);
                if (isStringRet) func.IsStringFunction = true;
                // `INTEGER`/`STRING` 这些类型名大多是 IDENTIFIER（词法表里只有首字母大写的
                // `Integer`/`String` 才映射到 VB_* 专用 token），两条都要吃。
                if (isStringRet || Peek().Type == TokenType.IDENTIFIER
                    || Peek().Type == TokenType.VB_INTEGER
                    || Peek().Type == TokenType.VB_OBJECT
                    || Peek().Type == TokenType.VB_VARIANT)
                {
                    Advance(); // skip 类型名
                }
            }

            // ── NATIVE 声明**没有体**，别去解析体 ────────────────────────────────
            //
            // 体循环是 `while (!AtEnd())` 直到配对的那个 END {SUB,FUNCTION}。声明写成
            // `NATIVE FUNCTION f() AS INTEGER` 而**没写 END FUNCTION** 时，它一路吃到
            // EOF —— 于是**该声明后面整个程序都被当成了它的体**。
            //
            // 为什么后果是「零输出」而不是报错：NATIVE 的体**本来就不生成代码**
            // （`CodeGenerator.Sub.cs` 的 GenerateSub/FunctionDeclaration 开头就 `if (IsNative) return;`），
            // 被吞进去的语句连一个 `line_N` 标签都拿不到。主程序一条语句都不剩 ⇒
            // 编出来的是一份**完全合法、什么也不做**的程序，退出码 0、屏幕上没有一个字。
            // 实测 `Examples/basic/sysinfo.bas`：`statements=1 FunctionDeclaration=1`。
            //
            // 判据是「是不是 NATIVE」而不是「有没有 END」：NATIVE 就是**外部符号声明**，
            // 它没有「体」这个概念，写不写 END 都不该吃后面的语句。
            // 紧跟的 END 仍旧吃掉 —— `Examples/basic/whack.bas` 等既有例子写的是
            // `NATIVE SUB f(...)` + `END SUB` 的**空体**写法（那是绕这个 bug 的土办法），
            // 留着它两种写法都能编。

            if (_pendingNative)
            {
                if (Peek().Type == TokenType.END && current + 1 < tokens.Count && tokens[current + 1].Type == TokenType.FUNCTION)
                {
                    Advance(); // skip END
                    Advance(); // skip FUNCTION
                }
                return func;
            }

            // 解析函数体直到 END FUNCTION
            while (!AtEnd())
            {
                if (Peek().Type == TokenType.END && current + 1 < tokens.Count && tokens[current + 1].Type == TokenType.FUNCTION)
                {
                    Advance(); // skip END
                    Advance(); // skip FUNCTION
                    break;
                }
                Statement bodyStmt = ParseStatement();
                if (bodyStmt != null)
                {
                    func.Body.Add(bodyStmt);
                }
            }

            // 跳过 END FUNCTION
            if (Peek().Type == TokenType.END)
            {
                Advance();
                if (Peek().Type == TokenType.FUNCTION)
                {
                    Advance();
                }
            }

            return func;
        }

        private CallStatement ParseCallStatement()
        {
            Token token = Advance(); // 跳过 CALL
            if (Peek().Type != TokenType.IDENTIFIER)
            {
                return null;
            }
            string name = Peek().Value;
            Advance();

            CallStatement call = new CallStatement(token.Line, token.Column, name);

            // 解析参数列表
            if (Peek().Type == TokenType.LPAREN)
            {
                Advance(); // 跳过 (
                while (Peek().Type != TokenType.RPAREN && !AtEnd())
                {
                    if (!IsExpressionStart(Peek())) break;
                    Expression arg = ParseExpression();
                    if (arg != null) call.Arguments.Add(arg);
                    if (Peek().Type == TokenType.COMMA)
                    {
                        Advance();
                    }
                    else
                    {
                        break;
                    }
                }
                if (Peek().Type == TokenType.RPAREN)
                {
                    Advance();
                }
            }

            return call;
        }

        private Statement ParseDeclareStatement()
        {
            Advance();

            if (Peek().Type == TokenType.SUB || Peek().Type == TokenType.FUNCTION)
                Advance();

            if (Peek().Type == TokenType.IDENTIFIER)
                Advance();

            if (Peek().Type == TokenType.LPAREN)
            {
                Advance();
                int parenDepth = 1;
                while (!AtEnd() && parenDepth > 0)
                {
                    if (Peek().Type == TokenType.LPAREN) parenDepth++;
                    else if (Peek().Type == TokenType.RPAREN) parenDepth--;
                    if (parenDepth > 0) Advance();
                }
                if (Peek().Type == TokenType.RPAREN)
                    Advance();
            }

            if (Peek().Type == TokenType.AS)
            {
                Advance();
                if (Peek().Type == TokenType.IDENTIFIER)
                    Advance();
            }

            return null;
        }

        /// <summary>
        /// Parse an implicit SUB call (without CALL keyword) — QBasic bare name syntax.
        /// Arguments are comma-separated without parentheses.
        /// </summary>
        private Statement ParseImplicitCallStatement()
        {
            string name = Peek().Value;
            Token token = Advance(); // consume SUB name
            CallStatement call = new CallStatement(token.Line, token.Column, name);

            // Parse comma-separated arguments until we hit a statement boundary
            while (!AtEnd() && Peek().Type != TokenType.COLON)
            {
                // Stop if next token is a statement-level keyword
                if (IsStatementBoundary(Peek()))
                    break;

                // Stop if next token is an identifier that starts a label (followed by ':')
                if (Peek().Type == TokenType.IDENTIFIER &&
                    current + 1 < tokens.Count && tokens[current + 1].Type == TokenType.COLON)
                    break;

                // Stop if next token is an identifier that's a known SUB (start of next implicit call)
                // 但跳过带有类型后缀的标识符 (如 Player1$), 它们不可能是SUB名
                string nextVal = Peek().Value;
                if (Peek().Type == TokenType.IDENTIFIER && declaredSubs.Contains(nextVal)
                    && !nextVal.EndsWith("$") && !nextVal.EndsWith("%") && !nextVal.EndsWith("!")
                    && !nextVal.EndsWith("#") && !nextVal.EndsWith("&"))
                    break;

                if (!IsExpressionStart(Peek())) break;
                Expression arg = ParseExpression();
                if (arg != null) call.Arguments.Add(arg);

                if (Peek().Type == TokenType.COMMA)
                    Advance(); // consume comma
                else
                    break;
            }

            return call;
        }

        /// <summary>
        /// Check if a token could start an expression (used to prevent ParseExpression
        /// from being called on statement-level keywords).
        /// </summary>
        private bool IsExpressionStart(Token token)
        {
            switch (token.Type)
            {
                case TokenType.NUMBER:
                case TokenType.STRING:
                case TokenType.IDENTIFIER:
                case TokenType.LPAREN:
                case TokenType.DOT:
                case TokenType.PLUS:
                case TokenType.MINUS:
                case TokenType.NOT:
                case TokenType.FN_KW:
                case TokenType.INKEY:
                case TokenType.KB_GETCH:
                case TokenType.KB_HIT:
                case TokenType.TIMER_FUNC:
                case TokenType.DATE_FUNC:
                case TokenType.TIME_FUNC:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Check if a token indicates a statement boundary (ends the current implicit call arguments).
        /// </summary>
        private bool IsStatementBoundary(Token token)
        {
            // Keyword tokens that start new statements — not expressions
            switch (token.Type)
            {
                case TokenType.PRINT:
                case TokenType.INPUT:
                case TokenType.LET:
                case TokenType.IF:
                case TokenType.GOTO:
                case TokenType.GOSUB:
                case TokenType.RETURN:
                case TokenType.FOR:
                case TokenType.DIM:
                case TokenType.WHILE:
                case TokenType.DO:
                case TokenType.END:
                case TokenType.SUB:
                case TokenType.FUNCTION:
                case TokenType.CALL:
                case TokenType.EXIT:
                case TokenType.SELECT:
                case TokenType.DECLARE:
                case TokenType.CONST_KW:
                case TokenType.TYPE_KW:
                case TokenType.OPEN:
                case TokenType.CLOSE:
                case TokenType.ON:
                case TokenType.DATA:
                case TokenType.READ_KW:
                case TokenType.RESTORE:
                case TokenType.RESUME:
                case TokenType.REDIM:
                case TokenType.VIEW:
                case TokenType.WINDOW:
                case TokenType.DEF_KW:
                case TokenType.DEFINT:
                case TokenType.DEFSNG:
                case TokenType.DEFSTR:
                case TokenType.DEFDBL:
                case TokenType.DEFLNG:
                case TokenType.SHARED:
                case TokenType.COMMON:
                case TokenType.OPTION_KW:
                case TokenType.LPRINT:
                case TokenType.SCREEN:
                case TokenType.SYSTEM:
                case TokenType.PSET:
                case TokenType.QB_LINE:
                case TokenType.QB_CIRCLE:
                case TokenType.PAINT:
                case TokenType.LOCATE:
                case TokenType.QB_COLOR:
                case TokenType.DRAW_KW:
                case TokenType.QB_WIDTH:
                case TokenType.PALETTE_KW:
                case TokenType.PLAY_KW:
                case TokenType.SOUND_KW:
                case TokenType.RANDOMIZE:
                case TokenType.KB_GETCH:
                case TokenType.SLEEP:
                case TokenType.SWAP:
                case TokenType.ERASE:
                case TokenType.POKE:
                    return true;
                default:
                    return false;
            }
        }
    }
}
