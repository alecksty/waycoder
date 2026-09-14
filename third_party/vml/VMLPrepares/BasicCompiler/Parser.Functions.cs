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

                    if (Peek().Type != TokenType.IDENTIFIER)
                    {
                        return null;
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

                    if (Peek().Type != TokenType.IDENTIFIER)
                    {
                        return null;
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
