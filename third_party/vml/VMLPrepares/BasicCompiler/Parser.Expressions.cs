using CompilerBase;
using System.Collections.Generic;

namespace BasicCompiler
{
    public partial class Parser : ParserBase<Token, TokenType>
    {
        private Expression ParseExpression()
        {
            return ParseLogicalOr();
        }

        private Expression ParseLogicalOr()
        {
            Expression expr = ParseLogicalAnd();

            while (Peek().Type == TokenType.OR)
            {
                string op = Peek().Value;
                Advance();
                Expression right = RequiredOperand(ParseLogicalAnd());
                BinaryExpression binary = new BinaryExpression(expr.Line, expr.Column);
                binary.Left = expr;
                binary.Operator = op;
                binary.Right = right;
                expr = binary;
            }

            return expr;
        }

        private Expression ParseLogicalAnd()
        {
            Expression expr = ParseLogicalNot();

            while (Peek().Type == TokenType.AND)
            {
                string op = Peek().Value;
                Advance();
                Expression right = RequiredOperand(ParseLogicalNot());
                BinaryExpression binary = new BinaryExpression(expr.Line, expr.Column);
                binary.Left = expr;
                binary.Operator = op;
                binary.Right = right;
                expr = binary;
            }

            return expr;
        }

        private Expression ParseLogicalNot()
        {
            if (Peek().Type == TokenType.NOT)
            {
                Advance();
                Expression operand = RequiredOperand(ParseLogicalNot());
                UnaryExpression unary = new UnaryExpression(operand.Line, operand.Column);
                unary.Operator = "NOT";
                unary.Expression = operand;
                return unary;
            }

            return ParseComparison();
        }

        private Expression ParseComparison()
        {
            Expression expr = ParseTerm();

            while (Peek().Type == TokenType.EQUALS || 
                   Peek().Type == TokenType.NOT_EQUAL ||
                   Peek().Type == TokenType.LESS ||
                   Peek().Type == TokenType.LESS_EQUAL ||
                   Peek().Type == TokenType.GREATER ||
                   Peek().Type == TokenType.GREATER_EQUAL)
            {
                string op = Peek().Value;
                Advance();
                Expression right = RequiredOperand(ParseTerm());
                BinaryExpression binary = new BinaryExpression(expr.Line, expr.Column);
                binary.Left = expr;
                binary.Operator = op;
                binary.Right = right;
                expr = binary;
            }

            return expr;
        }

        private Expression ParseTerm()
        {
            Expression expr = ParseFactor();
            if (expr == null) return null;

            while (Peek().Type == TokenType.PLUS || Peek().Type == TokenType.MINUS)
            {
                string op = Peek().Value;
                Advance();
                Expression right = RequiredOperand(ParseFactor());
                BinaryExpression binary = new BinaryExpression(expr.Line, expr.Column);
                binary.Left = expr;
                binary.Operator = op;
                binary.Right = right;
                expr = binary;
            }

            return expr;
        }

        /// <summary>
        /// 解析**必需**的操作数 —— 解析不出来就报带位置的语法错误，并返回占位 0。
        ///
        /// ⚠ 为什么不在 `ParsePrimary` 的 `default: return null` 处直接报：
        ///   `ParsePrimary` 返回 null 是**试探语义**（"这里是不是一个表达式"），
        ///   语句分派靠它决定要不要换一种语句种类（`Parser.Statements.cs` 里
        ///   `if (!IsExpressionStart(Peek())) break;` 与若干 `if (expr != null)` 都是这个用法）。
        ///   一律报出来会把合法语句判成语法错误 —— cs 前端本轮实测踩过
        ///   （3 条用例全变成 `1:1 …遇到 Class 'class'`）。
        ///   所以「试探」允许 null，「必需」在**操作数位置**报错 —— 本方法。
        ///
        /// ⚠ 修之前的行为：`PRINT 1 +`（行尾缺操作数）编出「右子节点为 null 的
        ///   `BinaryExpression`」⇒ 一句错都不报、程序照编（DiagProbe【语法错误】档 bas NOERR）。
        ///   报出来 + 占位 0 之后，这一句能继续解析下去，同一文件里后面的错也能一起报。
        /// </summary>
        private Expression RequiredOperand(Expression? parsed)
        {
            if (parsed != null) return parsed;
            var t = Peek();
            // ⚠ 位置**锚在上一个 token**，不是 `t`：BASIC 的语句以换行收尾，
            //   `PRINT 1 +` 的下一个 token 是**下一行**的 `EOF`（或下一句的行号），
            //   按当前位置收集就报到下一行去了（实测 5:1，而错在 4 行）。
            //   文案仍旧说"遇到了什么"——**位置取缺口在哪、文案取看到了什么**，
            //   与 `ParserBase.ErrorAt` 那条判据同一套。
            GccErrorAt($"这里缺少一个表达式，却遇到 {t.Type} '{t.Value}'",
                       Previous(), ErrorCode.Parser_ExpectedExpression);
            return new NumberLiteral(t.Line, t.Column, 0);
        }

        private Expression ParseFactor()
        {
            Expression expr = ParsePrimary();
            if (expr == null) return null;

            while (Peek().Type == TokenType.MULTIPLY || Peek().Type == TokenType.DIVIDE
                   || Peek().Type == TokenType.INT_DIVIDE
                   || Peek().Type == TokenType.EXPONENT || Peek().Type == TokenType.MOD_KW)
            {
                string op = Peek().Value;
                Advance();
                Expression right = RequiredOperand(ParsePrimary());
                BinaryExpression binary = new BinaryExpression(expr.Line, expr.Column);
                binary.Left = expr;
                binary.Operator = op;
                binary.Right = right;
                expr = binary;
            }

            return expr;
        }

        private Expression ParsePrimary()
        {
            Token token = Peek();
            Expression expr;

            switch (token.Type)
            {
                case TokenType.NUMBER:
                    Advance();
                    if (token.Value.StartsWith("&H") || token.Value.StartsWith("&h"))
                    {
                        string hex = token.Value.Substring(2);
                        expr = new NumberLiteral(token.Line, token.Column, (double)Convert.ToInt32(hex, 16));
                    }
                    else
                    {
                        expr = new NumberLiteral(token.Line, token.Column, double.Parse(token.Value));
                    }
                    break;
                case TokenType.STRING:
                    Advance();
                    expr = new StringLiteral(token.Line, token.Column, token.Value);
                    break;
                case TokenType.IDENTIFIER:
                    Advance();
                    // Check if this is a function call, array access, or plain identifier
                    if (Peek().Type == TokenType.LPAREN)
                    {
                        // Could be array access or function call
                        if (declaredFunctions.Contains(token.Value))
                        {
                            expr = ParseFunctionCall(token);
                        }
                        else if (declaredArrays.Contains(token.Value))
                        {
                            expr = ParseArrayAccess(token);
                        }
                        else if (declaredSubs.Contains(token.Value))
                        {
                            // SUB call in expression context - treat as function call returning value
                            expr = ParseFunctionCall(token);
                        }
                        else
                        {
                            // Unknown - try function call first, fall back to array access
                            // In BASIC, NAME(args) without CALL is ambiguous; default to function call
                            expr = ParseFunctionCall(token);
                        }
                    }
                    // Check for field access: record.field or method call: obj.method(args)
                    else if (Peek().Type == TokenType.DOT)
                    {
                        Advance(); // skip .
                        if (Peek().Type == TokenType.IDENTIFIER)
                        {
                            string fieldName = Peek().Value;
                            Advance();
                            // Check if this is a method call: obj.method(args)
                            if (Peek().Type == TokenType.LPAREN)
                            {
                                var methodToken = new Token(TokenType.IDENTIFIER, $"{token.Value}_{fieldName}", token.Line, token.Column);
                                expr = ParseFunctionCall(methodToken);
                            }
                            else
                            {
                                expr = new FieldAccessExpression(token.Line, token.Column)
                                {
                                    RecordName = token.Value,
                                    FieldName = fieldName
                                };
                            }
                        }
                        else
                        {
                            expr = new Identifier(token.Line, token.Column, token.Value);
                        }
                    }
                    // **无参函数的「裸名调用」**（没写括号）——
                    //   QBasic 里 `X = CalcDelay` 与 `X = CalcDelay()` 等价（无参函数
                    //   的括号可有可无），而这里此前一律落到"普通标识符" ⇒ 取的是**同名变量**
                    //   （多半从没被赋过值 = 0），**一个错都不报**。
                    //
                    //   实测（GORILLA.BAS）：`MachSpeed = CalcDelay` 拿到 0 ⇒
                    //   `SUB Rest` 里 `t2# = MachSpeed * t# / SPEEDCONST` 恒为 0 ⇒
                    //   `LOOP UNTIL TIMER - s# > t2#` 退化成"等到下一秒"，
                    //   香蕉每走一步都卡将近 1 秒 —— 画面看上去就是"射出去之后不动了"。
                    //
                    //   ⚠ 判据用 `declaredFunctions`（`DECLARE FUNCTION` / 函数体都往里登记），
                    //   与"带括号那条路"同一个表；`X(...)` 的数组/函数歧义在这里不存在
                    //   （裸名没有括号，只可能是无参调用）。
                    //   ⚠ 紧跟着 `=` 的**不算**：那是"这条名字是赋值目标"（`ParseLetStatement`
                    //   也用 `ParsePrimary` 取左值），当调用就会把 `F = 7` 编成一次丢弃返回值的
                    //   调用 —— 实测直接把 `FUNCTION F () / F = 7 / END FUNCTION` 编成恒返回 0。
                    else if (Peek().Type != TokenType.EQUALS
                             && ResolveDeclaredFunction(token.Value) is { } bareFn)
                    {
                        //   ⚠ 名字要**按登记时的原名**去调：`DECLARE FUNCTION CalcDelay! ()`
                        //   登记的是带 `!` 的名字，而 GORILLA 的调用点写的是 `CalcDelay`
                        //   （不带后缀）。用裸名编出来的标签是 `func_calcaldelay`… 而真正的
                        //   函数标签带 `_sng`（`BasicSymbol` 把 `!` 转义）—— 直接用裸名会
                        //   链接报「未定义的函数」。
                        expr = new FunctionCallExpression(token.Line, token.Column, bareFn);
                    }
                    else
                    {
                        expr = new Identifier(token.Line, token.Column, token.Value);
                    }
                    break;
                case TokenType.FN_KW:
                    Advance(); // skip FN
                    if (Peek().Type == TokenType.IDENTIFIER)
                    {
                        string fnName = "FN" + Peek().Value;
                        Token funcToken = Peek();
                        Advance();
                        if (Peek().Type == TokenType.LPAREN)
                        {
                            expr = ParseFunctionCall(new Token(TokenType.IDENTIFIER, fnName, funcToken.Line, funcToken.Column));
                        }
                        else
                        {
                            expr = new Identifier(token.Line, token.Column, fnName);
                        }
                    }
                    else
                    {
                        expr = new Identifier(token.Line, token.Column, "FN");
                    }
                    break;
                case TokenType.INKEY:
                case TokenType.KB_GETCH:
                    Advance();
                    expr = new InkeyExpression(token.Line, token.Column);
                    break;
                case TokenType.KB_HIT:
                    Advance();
                    expr = new InkeyExpression(token.Line, token.Column);
                    break;
                case TokenType.TIMER_FUNC:
                    Advance();
                    expr = new TimerFunctionExpression(token.Line, token.Column);
                    break;
                case TokenType.DATE_FUNC:
                    Advance();
                    expr = new DateFunctionExpression(token.Line, token.Column);
                    break;
                case TokenType.TIME_FUNC:
                    Advance();
                    expr = new TimeFunctionExpression(token.Line, token.Column);
                    break;
                case TokenType.LPAREN:
                    Advance();
                    expr = ParseExpression();
                    if (Peek().Type == TokenType.RPAREN)
                        Advance();
                    break;
                case TokenType.DOT:
                    // 前导小数: .5 → 0.5
                    Advance();
                    if (Peek().Type == TokenType.NUMBER)
                    {
                        string numStr = "0." + Peek().Value;
                        Advance();
                        expr = new NumberLiteral(token.Line, token.Column, double.Parse(numStr));
                    }
                    else
                    {
                        return null;
                    }
                    break;
                case TokenType.PLUS:
                case TokenType.MINUS:
                    string op = token.Value;
                    Advance();
                    UnaryExpression unary = new UnaryExpression(token.Line, token.Column);
                    unary.Operator = op;
                    unary.Expression = RequiredOperand(ParsePrimary());
                    expr = unary;
                    break;
                default:
                    return null;
            }

            // After parsing a primary expression, check for postfix field access: expr.field
            while (expr != null && Peek().Type == TokenType.DOT)
            {
                Advance(); // skip .
                if (Peek().Type == TokenType.IDENTIFIER)
                {
                    string fieldName = Peek().Value;
                    Advance();
                    var fieldAccess = new FieldAccessExpression(expr.Line, expr.Column)
                    {
                        FieldName = fieldName,
                        RecordExpression = expr
                    };
                    // If expr is an Identifier, also set RecordName for backward compatibility
                    if (expr is Identifier ident)
                    {
                        fieldAccess.RecordName = ident.Name;
                    }
                    expr = fieldAccess;
                }
                else
                {
                    break;
                }
            }

            return expr;
        }

        private new Token Advance()
        {
            if (!AtEnd())
            {
                current++;
            }
            return Previous();
        }

        private Token Peek()
        {
            return tokens[current];
        }

        private new Token Previous()
        {
            return tokens[current - 1];
        }

        private bool AtEnd()
        {
            return current >= tokens.Count || Peek().Type == TokenType.EOF;
        }
    }
}
