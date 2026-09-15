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
                Expression right = ParseLogicalAnd();
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
                Expression right = ParseLogicalNot();
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
                Expression operand = ParseLogicalNot();
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
                Expression right = ParseTerm();
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
                Expression right = ParseFactor();
                BinaryExpression binary = new BinaryExpression(expr.Line, expr.Column);
                binary.Left = expr;
                binary.Operator = op;
                binary.Right = right;
                expr = binary;
            }

            return expr;
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
                Expression right = ParsePrimary();
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
                    unary.Expression = ParsePrimary();
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
