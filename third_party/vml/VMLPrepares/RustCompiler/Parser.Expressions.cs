using System;
using System.Collections.Generic;
using CompilerBase;

namespace RustCompiler
{
    public partial class Parser : ParserBase<Token, TokenType>
    {
        private ASTNode ParseExpression()
        {
            var result = ParseLogicalOr();
            return result;
        }
        
        /// <summary>
        /// 解析逻辑或表达式
        /// </summary>
        private ASTNode ParseLogicalOr()
        {
            var expr = ParseLogicalAnd();
            
            while (Match(TokenType.OR))
            {
                var binaryExpr = new BinaryOperationNode
                {
                    Operator = "||",
                    Left = expr,
                    Right = ParseLogicalAnd()
                };
                expr = binaryExpr;
            }
            
            return expr;
        }
        
        /// <summary>
        /// 解析逻辑与表达式
        /// </summary>
        private ASTNode ParseLogicalAnd()
        {
            var expr = ParseBitwiseOr();
            
            while (Match(TokenType.AND))
            {
                var binaryExpr = new BinaryOperationNode
                {
                    Operator = "&&",
                    Left = expr,
                    Right = ParseBitwiseOr()
                };
                expr = binaryExpr;
            }
            
            return expr;
        }

        private ASTNode ParseBitwiseOr()
        {
            var expr = ParseBitwiseXor();
            while (Match(TokenType.PIPE))
            {
                expr = new BinaryOperationNode { Operator = "|", Left = expr, Right = ParseBitwiseXor() };
            }
            return expr;
        }

        private ASTNode ParseBitwiseXor()
        {
            var expr = ParseBitwiseAnd();
            while (Match(TokenType.CARET))
            {
                expr = new BinaryOperationNode { Operator = "^", Left = expr, Right = ParseBitwiseAnd() };
            }
            return expr;
        }

        private ASTNode ParseBitwiseAnd()
        {
            var expr = ParseEquality();
            while (Match(TokenType.AMPERSAND))
            {
                expr = new BinaryOperationNode { Operator = "&", Left = expr, Right = ParseEquality() };
            }
            return expr;
        }
        
        /// <summary>
        /// 解析相等性表达式
        /// </summary>
        private ASTNode ParseEquality()
        {
            var expr = ParseComparison();
            
            while (Match(TokenType.EQEQ) || Match(TokenType.BANGEQ))
            {
                var binaryExpr = new BinaryOperationNode
                {
                    Operator = Previous().Value,
                    Left = expr,
                    Right = ParseComparison()
                };
                expr = binaryExpr;
            }
            
            return expr;
        }
        
        /// <summary>
        /// 解析比较表达式
        /// </summary>
        private ASTNode ParseComparison()
        {
            var expr = ParseShift();
            
            while (true)
            {
                string op = null;
                if (Match(TokenType.LT)) op = "<";
                else if (Check(TokenType.GT) && Peek(1)?.Type == TokenType.EQ)
                {
                    op = ">=";
                    Advance(); Advance();
                }
                else if (Match(TokenType.GT)) { op = ">"; }
                else if (Match(TokenType.LTEQ)) op = "<=";
                else if (Match(TokenType.GTEQ)) op = ">=";
                else break;

                var binaryExpr = new BinaryOperationNode
                {
                    Operator = op,
                    Left = expr,
                    Right = ParseShift()
                };
                expr = binaryExpr;
            }
            
            return expr;
        }

        private ASTNode ParseShift()
        {
            var expr = ParseTerm();
            while (Match(TokenType.SHL) || Match(TokenType.SHR))
            {
                expr = new BinaryOperationNode { Operator = Previous().Value, Left = expr, Right = ParseTerm() };
            }
            return expr;
        }
        
        /// <summary>
        /// 解析加减表达式
        /// </summary>
        private ASTNode ParseTerm()
        {
            var expr = ParseFactor();
            
            while (Match(TokenType.PLUS) || Match(TokenType.MINUS))
            {
                var binaryExpr = new BinaryOperationNode
                {
                    Operator = Previous().Value,
                    Left = expr,
                    Right = ParseFactor()
                };
                expr = binaryExpr;
            }
            
            return expr;
        }
        
        /// <summary>
        /// 解析乘除表达式
        /// </summary>
        private ASTNode ParseFactor()
        {
            var expr = ParseUnary();
            
            while (Match(TokenType.STAR) || Match(TokenType.SLASH) || Match(TokenType.PERCENT))
            {
                var binaryExpr = new BinaryOperationNode
                {
                    Operator = Previous().Value,
                    Left = expr,
                    Right = ParseUnary()
                };
                expr = binaryExpr;
            }
            
            return expr;
        }
        
        /// <summary>
        /// 解析一元表达式
        /// </summary>
        private ASTNode ParseUnary()
        {
            if (Match(TokenType.MINUS) || Match(TokenType.BANG) || Match(TokenType.STAR))
            {
                var unaryExpr = new UnaryOperationNode
                {
                    Operator = Previous().Value,
                    Operand = ParseUnary()
                };
                return unaryExpr;
            }

            // & 和 &mut 引用运算符
            if (Match(TokenType.AMPERSAND))
            {
                string refOp = "&";
                if (Match(TokenType.MUT))
                    refOp = "&mut";
                var unaryExpr = new UnaryOperationNode
                {
                    Operator = refOp,
                    Operand = ParseUnary()
                };
                return unaryExpr;
            }
            
            var expr = ParsePrimary();
            // 后置操作符: 索引访问 expr[0] 和成员访问 expr.member / expr.method()
            while (true)
            {
                if (Match(TokenType.LBRACKET))
                {
                    var index = ParseExpression();
                    Expect(TokenType.RBRACKET, "期望 ']'");
                    expr = new IndexAccessNode { Target = expr, Index = index };
                }
                else if (Match(TokenType.DOT))
                {
                    string member;
                    if (Check(TokenType.INTEGER))
                    {
                        member = Advance().Value; // tuple index: .0, .1
                    }
                    else
                    {
                        member = Expect(TokenType.IDENTIFIER, "期望成员名").Value;
                    }
                    var ma = new MemberAccessNode { Target = expr, Member = member };
                    if (Match(TokenType.LPAREN))
                    {
                        while (!Check(TokenType.RPAREN) && !IsAtEnd)
                        {
                            ma.Arguments.Add(ParseExpression());
                            if (!Match(TokenType.COMMA))
                                break;
                        }
                        Expect(TokenType.RPAREN, "期望 ')'");
                    }
                    expr = ma;
                }
                else if (Match(TokenType.AS))
                {
                    string targetType = ParseType();
                    expr = new BinaryOperationNode
                    {
                        Operator = "as",
                        Left = expr,
                        Right = new LiteralNode { Value = targetType, Type = "type" }
                    };
                }
                else
                {
                    break;
                }
            }
            return expr;
        }
        
        /// <summary>
        /// 解析基本表达式
        /// </summary>
        private ASTNode ParsePrimary()
        {
            if (Match(TokenType.INTEGER))
            {
                long intVal = ParseLongValue(Previous().Value);
                // 溢出 int32 时升级为 long (i64)，避免 int.Parse 抛异常 (conv long_to_str 等)
                if (intVal >= int.MinValue && intVal <= int.MaxValue)
                    return new LiteralNode { Value = (int)intVal, Type = "int" };
                return new LiteralNode { Value = intVal, Type = "long" };
            }
            else if (Match(TokenType.FLOAT))
            {
                return new LiteralNode
                {
                    Value = float.Parse(Previous().Value),
                    Type = "float"
                };
            }
            else if (Match(TokenType.STRING))
            {
                return new LiteralNode
                {
                    Value = Previous().Value,
                    Type = "string"
                };
            }
            else if (Match(TokenType.CHARACTER))
            {
                return new LiteralNode
                {
                    Value = Previous().Value[0],
                    Type = "char"
                };
            }
            else if (Match(TokenType.TRUE))
            {
                return new LiteralNode
                {
                    Value = true,
                    Type = "bool"
                };
            }
            else if (Match(TokenType.FALSE))
            {
                return new LiteralNode
                {
                    Value = false,
                    Type = "bool"
                };
            }
            else if (Match(TokenType.MATCH))
            {
                return ParseMatchStatement();
            }
            else if (Match(TokenType.IF))
            {
                return ParseIfStatement();
            }
            else if (Match(TokenType.IDENTIFIER) || Match(TokenType.SELF) || Match(TokenType.SELF_TYPE))
            {
                string idName = Previous().Value;
                // 宏调用: name!(...)
                if (Check(TokenType.BANG))
                {
                    Advance(); // 跳过 !
                    if (Match(TokenType.LPAREN))
                    {
                        var callExpr = new CallExpressionNode { FunctionName = idName };
                        while (!Check(TokenType.RPAREN) && !IsAtEnd)
                        {
                            callExpr.Arguments.Add(ParseExpression());
                            if (!Match(TokenType.COMMA)) break;
                        }
                        Expect(TokenType.RPAREN, "期望 ')'");
                        return callExpr;
                    }
                    else if (Match(TokenType.LBRACKET))
                    {
                        var arr = new ArrayLiteralNode();
                        while (!Check(TokenType.RBRACKET) && !IsAtEnd)
                        {
                            arr.Elements.Add(ParseExpression());
                            if (!Match(TokenType.COMMA)) break;
                        }
                        Expect(TokenType.RBRACKET, "期望 ']'");
                        // vec![] returns the array literal directly
                        return arr;
                    }
                    else if (Match(TokenType.LBRACE))
                    {
                        var block = new BlockNode();
                        while (!Check(TokenType.RBRACE) && !IsAtEnd)
                        {
                            block.Statements.Add(ParseStatement());
                        }
                        Expect(TokenType.RBRACE, "期望 '}'");
                        return block;
                    }
                    return new IdentifierNode { Name = idName };
                }
                // 路径表达式: Module::function(...) 或 Vec::new()
                if (Check(TokenType.COLON) && Peek(1)?.Type == TokenType.COLON)
                {
                    Advance(); // 跳过第一个 :
                    Advance(); // 跳过第二个 :
                    string memberName = Expect(TokenType.IDENTIFIER, "期望成员名").Value;
                    string qualifiedName = $"{idName}_{memberName}";
                    if (Check(TokenType.LPAREN))
                    {
                        return ParseCallExpression(qualifiedName);
                    }
                    return new IdentifierNode { Name = qualifiedName };
                }
                // 检查是否是函数调用
                if (Check(TokenType.LPAREN))
                {
                    return ParseCallExpression(idName);
                }
                
                // 结构体字面量: Point { x: 10, y: 20 } 或 Point { x, y }
                if (Check(TokenType.LBRACE) && 
                    _pos + 1 < _tokens.Count &&
                    _tokens[_pos + 1].Type == TokenType.IDENTIFIER)
                {
                    bool isShorthand = _pos + 2 < _tokens.Count &&
                        (_tokens[_pos + 2].Type == TokenType.COMMA || 
                         _tokens[_pos + 2].Type == TokenType.RBRACE);
                    bool isLonghand = _pos + 2 < _tokens.Count &&
                        _tokens[_pos + 2].Type == TokenType.COLON &&
                        !(_pos + 3 < _tokens.Count && _tokens[_pos + 3].Type == TokenType.COLON);
                    
                    if (isShorthand || isLonghand)
                    {
                        var structLit = new StructLiteralNode { StructName = idName };
                        Advance(); // consume {
                        while (!Check(TokenType.RBRACE) && !IsAtEnd)
                        {
                            string fieldName = Expect(TokenType.IDENTIFIER, "期望字段名").Value;
                            if (Match(TokenType.COLON))
                            {
                                var fieldValue = ParseExpression();
                                structLit.Fields.Add((fieldName, fieldValue));
                            }
                            else
                            {
                                // Shorthand: just field name (value = name)
                                structLit.Fields.Add((fieldName, new IdentifierNode { Name = fieldName }));
                            }
                            if (!Match(TokenType.COMMA)) break;
                        }
                        Expect(TokenType.RBRACE, "期望 '}'");
                        return structLit;
                    }
                }
                
                return new IdentifierNode
                {
                    Name = idName
                };
            }
            else if (Match(TokenType.PIPE))
            {
                var closure = new ClosureExprNode();
                while (!Check(TokenType.PIPE) && !IsAtEnd)
                {
                    string paramName = Expect(TokenType.IDENTIFIER, "期望参数名").Value;
                    closure.Parameters.Add(paramName);
                    if (Check(TokenType.COLON)) Advance(); while (!Check(TokenType.COMMA) && !Check(TokenType.PIPE) && !IsAtEnd) Advance();
                    Match(TokenType.COMMA);
                }
                Expect(TokenType.PIPE, "期望 '|' 在闭包参数后");
                // 跳过可选的返回类型标注: -> Type
                if (Match(TokenType.ARROW))
                {
                    while (!Check(TokenType.LBRACE) && !IsAtEnd) Advance();
                }
                if (Check(TokenType.LBRACE))
                    closure.Body = ParseBlock();
                else
                    closure.Body = ParseExpression();
                return closure;
            }
            else if (Match(TokenType.LPAREN))
            {
                var first = ParseExpression();
                if (Match(TokenType.COMMA))
                {
                    var tuple = new TupleExprNode();
                    tuple.Elements.Add(first);
                    do { tuple.Elements.Add(ParseExpression()); } while (Match(TokenType.COMMA));
                    Expect(TokenType.RPAREN, "期望 ')'");
                    return tuple;
                }
                Expect(TokenType.RPAREN, "期望 ')'");
                return first;
            }
            else if (Match(TokenType.LBRACKET))
            {
                var arr = new ArrayLiteralNode();
                if (!Check(TokenType.RBRACKET))
                {
                    do
                    {
                        arr.Elements.Add(ParseExpression());
                    } while (Match(TokenType.COMMA));
                }
                Expect(TokenType.RBRACKET, "期望 ']'");
                return arr;
            }
            
            throw new ParseException($"意外的token: {Peek()}");
        }
        
        /// <summary>
        /// 解析函数调用表达式
        /// </summary>
        private CallExpressionNode ParseCallExpression(string functionName)
        {
            var callExpr = new CallExpressionNode
            {
                FunctionName = functionName
            };
            
            Expect(TokenType.LPAREN, "期望 '('");
            
            while (!Check(TokenType.RPAREN) && !IsAtEnd)
            {
                callExpr.Arguments.Add(ParseExpression());
                
                if (!Match(TokenType.COMMA))
                {
                    break;
                }
            }
            
            Expect(TokenType.RPAREN, "期望 ')'");
            
            return callExpr;
        }
        
        private long ParseLongValue(string val)
        {
            if (val.StartsWith("0x") || val.StartsWith("0X"))
                return Convert.ToInt64(val.Substring(2), 16);
            return long.Parse(val);
        }

        // 辅助方法
    }
}
