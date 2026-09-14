using CompilerBase;
using System;
using System.Collections.Generic;
using VMLPlugins;

namespace CSharpCompiler
{
    public partial class Parser : ParserBase<Token, TokenType>
    {
        
        private Expression ParseExpression()
        {
            return ParseConditional();
        }

        private Expression ParseConditional()
        {
            var expr = ParseAssignment();

            // ?? 空合并运算符 (必须在 ? 之前检查)
            if (Match(TokenType.NullCoalescing))
            {
                var right = ParseConditional();
                // 简化：直接返回右值（null check 在运行时）
                return right;
            }

            if (Match(TokenType.Conditional))
            {
                var trueVal = ParseExpression();
                Expect(TokenType.Colon, "Expected ':' for conditional expression");
                var falseVal = ParseConditional();
                return new ConditionalExpression(expr, trueVal, falseVal);
            }

            return expr;
        }

        private Expression ParseLogicalOr()
        {
            var expr = ParseLogicalAnd();
            while (Match(TokenType.LogicalOr))
            {
                var op = Previous();
                var right = ParseLogicalAnd();
                expr = new BinaryExpression(expr, op.Type, right);
            }
            return expr;
        }

        private Expression ParseLogicalAnd()
        {
            var expr = ParseBitwiseOr();
            while (Match(TokenType.LogicalAnd))
            {
                var op = Previous();
                var right = ParseBitwiseOr();
                expr = new BinaryExpression(expr, op.Type, right);
            }
            return expr;
        }

        private Expression ParseBitwiseOr()
        {
            var expr = ParseBitwiseXor();
            while (Match(TokenType.BitwiseOr))
            {
                var op = Previous();
                var right = ParseBitwiseXor();
                expr = new BinaryExpression(expr, op.Type, right);
            }
            return expr;
        }

        private Expression ParseBitwiseXor()
        {
            var expr = ParseBitwiseAnd();
            while (Match(TokenType.BitwiseXor))
            {
                var op = Previous();
                var right = ParseBitwiseAnd();
                expr = new BinaryExpression(expr, op.Type, right);
            }
            return expr;
        }

        private Expression ParseBitwiseAnd()
        {
            var expr = ParseEquality();
            while (Match(TokenType.BitwiseAnd))
            {
                var op = Previous();
                var right = ParseEquality();
                expr = new BinaryExpression(expr, op.Type, right);
            }
            return expr;
        }
        
        private Expression ParseAssignment()
        {
            var expr = ParseLogicalOr();
            
            if (Match(TokenType.Assignment) || Match(TokenType.PlusEqual) || Match(TokenType.MinusEqual) ||
                Match(TokenType.MultiplyEqual) || Match(TokenType.DivideEqual) || Match(TokenType.ModuloEqual))
            {
                var op = Previous();
                var value = ParseAssignment();
                
                if (expr is VariableExpression || expr is IndexExpression)
                {
                    return new AssignmentExpression(expr, op.Type, value);
                }
            }
            
            return expr;
        }
        
        private Expression ParseEquality()
        {
            var expr = ParseComparison();
            
            while (Match(TokenType.Equal) || Match(TokenType.NotEqual))
            {
                var op = Previous();
                var right = ParseComparison();
                expr = new BinaryExpression(expr, op.Type, right);
            }
            
            return expr;
        }
        
        private Expression ParseComparison()
        {
            var expr = ParseShift();

            while (Match(TokenType.LessThan) || Match(TokenType.LessThanOrEqual) || 
                   Match(TokenType.GreaterThan) || Match(TokenType.GreaterThanOrEqual))
            {
                var op = Previous();
                var right = ParseShift();
                expr = new BinaryExpression(expr, op.Type, right);
            }

            return expr;
        }

        private Expression ParseShift()
        {
            var expr = ParseTerm();
            while (Match(TokenType.LeftShift) || Match(TokenType.RightShift))
            {
                var op = Previous();
                var right = ParseTerm();
                expr = new BinaryExpression(expr, op.Type, right);
            }
            return expr;
        }
        
        private Expression ParseTerm()
        {
            var expr = ParseFactor();
            
            while (Match(TokenType.Plus) || Match(TokenType.Minus))
            {
                var op = Previous();
                var right = ParseFactor();
                expr = new BinaryExpression(expr, op.Type, right);
            }
            
            return expr;
        }
        
        private Expression ParseFactor()
        {
            var expr = ParseUnary();
            
            while (Match(TokenType.Multiply) || Match(TokenType.Divide) || Match(TokenType.Modulo))
            {
                var op = Previous();
                var right = ParseUnary();
                expr = new BinaryExpression(expr, op.Type, right);
            }
            
            return expr;
        }
        
        private Expression ParseUnary()
        {
            if (Match(TokenType.Minus) || Match(TokenType.LogicalNot) || Match(TokenType.BitwiseNot))
            {
                var op = Previous();
                var right = ParseUnary();
                return new UnaryExpression(op.Type, right);
            }

            // Pointer dereference: *ptr
            if (Match(TokenType.Multiply))
            {
                var right = ParseUnary();
                return new DerefExpression(right);
            }

            // Address-of: &var
            if (Match(TokenType.BitwiseAnd))
            {
                var right = ParseUnary();
                return new AddrOfExpression(right);
            }

            if (Match(TokenType.Increment))
            {
                var right = ParseUnary();
                return new AssignmentExpression(right, TokenType.Increment, new LiteralExpression(1));
            }

            if (Match(TokenType.Decrement))
            {
                var right = ParseUnary();
                return new AssignmentExpression(right, TokenType.Decrement, new LiteralExpression(1));
            }

            return ParseCallOrMember();
        }
        
        /// <summary>
        /// 解析成员访问和方法调用链（在ParsePrimary之后）
        /// </summary>
        private Expression ParseCallOrMember()
        {
            var expr = ParsePrimary();
            if (expr == null) return null;
            
            while (true)
            {
                // 成员访问：obj.member
                if (Match(TokenType.Dot))
                {
                    Advance(); // consume identifier
                    var memberName = Previous().Value;
                    
                    // 检查是否是方法调用：obj.method()
                    if (Match(TokenType.LeftParen))
                    {
                        var call = new CallExpression(new MemberExpression(expr, memberName));
                        if (!Check(TokenType.RightParen))
                        {
                            do
                            {
                                var arg = ParseExpression();
                                if (arg != null)
                                    call.Arguments.Add(arg);
                            } while (Match(TokenType.Comma));
                        }
                        Expect(TokenType.RightParen, "Expected ')' after method arguments");
                        expr = call;
                    }
                    else
                    {
                        expr = new MemberExpression(expr, memberName);
                    }
                }
                // 直接函数调用：func()
                else if (Match(TokenType.LeftParen))
                {
                    var call = new CallExpression(expr);
                    if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                            var arg = ParseExpression();
                            if (arg != null)
                                call.Arguments.Add(arg);
                        } while (Match(TokenType.Comma));
                    }
                    Expect(TokenType.RightParen, "Expected ')' after function arguments");
                    expr = call;
                }
                // 数组索引：arr[index]
                else if (Match(TokenType.LeftBracket))
                {
                    var index = ParseExpression();
                    Expect(TokenType.RightBracket, "Expected ']' after array index");
                    expr = new IndexExpression(expr, index);
                }
                // 后置自增：expr++
                else if (Match(TokenType.Increment))
                {
                    expr = new AssignmentExpression(expr, TokenType.Increment, null);
                }
                // 后置自减：expr--
                else if (Match(TokenType.Decrement))
                {
                    expr = new AssignmentExpression(expr, TokenType.Decrement, null);
                }
                else
                {
                    break;
                }
            }
            
            return expr;
        }
        
        private Expression ParsePrimary()
        {
            if (Match(TokenType.False)) return new LiteralExpression(false);
            if (Match(TokenType.True)) return new LiteralExpression(true);
            if (Match(TokenType.Null)) return new LiteralExpression(null);
            if (Match(TokenType.This)) return new VariableExpression("this");

            // typeof(int) → MCU不支持反射, 返回0
            if (Match(TokenType.Typeof))
            {
                Expect(TokenType.LeftParen, "Expected '(' after typeof");
                // 跳过类型名(可能包含泛型)
                SkipExpression();
                Expect(TokenType.RightParen, "Expected ')' after typeof");
                return new LiteralExpression(0);
            }
            
            if (Match(TokenType.IntegerLiteral))
            {
                string val = Previous().Value;
                // L 后缀 → long 字面量
                if (val.EndsWith("L") || val.EndsWith("l"))
                {
                    if (long.TryParse(val.TrimEnd('L', 'l'), out long lv))
                        return new LiteralExpression(lv);
                }
                else if (int.TryParse(val, out int intValue))
                {
                    return new LiteralExpression(intValue);
                }
                else if (long.TryParse(val, out long longValue))
                {
                    // 超出 int 范围的整数 → long
                    return new LiteralExpression(longValue);
                }
            }

            if (Match(TokenType.FloatLiteral))
            {
                string val = Previous().Value;
                // f 后缀 → float；无后缀或 d 后缀 → double (C# 默认浮点字面量是 double)
                if (val.EndsWith("f") || val.EndsWith("F"))
                {
                    if (float.TryParse(val.TrimEnd('f', 'F'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float floatValue))
                        return new LiteralExpression(floatValue);
                }
                else
                {
                    string num = val.TrimEnd('d', 'D');
                    if (double.TryParse(num, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double doubleValue))
                        return new LiteralExpression(doubleValue);
                }
            }
            
            if (Match(TokenType.StringLiteral))
            {
                return new LiteralExpression(Previous().Value);
            }
            
            if (Match(TokenType.CharacterLiteral))
            {
                return new LiteralExpression(Previous().Value);
            }
            
            if (Match(TokenType.Identifier))
            {
                return new VariableExpression(Previous().Value);
            }
            
            if (Match(TokenType.LeftParen))
            {
                // 检测类型转换表达式: (int)expr, (float)expr, etc.
                if (IsTypeKeyword(Current))
                {
                    string typeName = GetTypeKeywordName(Current);
                    Advance(); // 消费类型关键字
                    Expect(TokenType.RightParen, "Expected ')' after type in cast");
                    var operand = ParseUnary();
                    return new CastExpression(typeName, operand);
                }

                var expr = ParseExpression();
                if (debugMode)
                {
                    Console.WriteLine($"DEBUG ParsePrimary: after ParseExpression, current={Current?.Type}:{Current?.Value}");
                }
                Expect(TokenType.RightParen, "Expected ')' after expression");
                return new ParenthesizedExpression(expr);
            }
            
            // new 表达式: new int[]{1,2,3} / new int[5] / new List<int>() / new A.B.C()
            if (Match(TokenType.New))
            {
                // 解析类型名(支持泛型、限定名、关键字类型)
                string newType = "";
                // 关键字类型: int, string, bool, float, double, char, long, byte, etc.
                if (Match(TokenType.Int)) newType = "int";
                else if (Match(TokenType.String)) newType = "string";
                else if (Match(TokenType.Bool)) newType = "bool";
                else if (Match(TokenType.Float)) newType = "float";
                else if (Match(TokenType.Double)) newType = "double";
                else if (Match(TokenType.Char)) newType = "char";
                else if (Match(TokenType.Long)) newType = "long";
                else if (Match(TokenType.Byte)) newType = "byte";
                else if (Match(TokenType.SByte)) newType = "sbyte";
                else if (Match(TokenType.Short)) newType = "short";
                else if (Match(TokenType.UShort)) newType = "ushort";
                else if (Match(TokenType.UInt)) newType = "uint";
                else if (Match(TokenType.ULong)) newType = "ulong";
                else if (Match(TokenType.Decimal)) newType = "decimal";
                else if (Match(TokenType.Object)) newType = "object";
                // 标识符类型: 限定名和泛型
                else if (Check(TokenType.Identifier))
                {
                    while (Check(TokenType.Identifier))
                    {
                        newType += tokens[position++].Value;
                        if (Match(TokenType.Dot)) newType += ".";
                        else break;
                    }
                    // 泛型: new List<int>()
                    if (Match(TokenType.LessThan))
                    {
                        newType += "<";
                        int depth = 1;
                        while (depth > 0 && position < tokens.Count)
                        {
                            if (tokens[position].Type == TokenType.GreaterThan) { depth--; if (depth > 0) newType += ">"; }
                            else if (tokens[position].Type == TokenType.LessThan) { depth++; newType += "<"; }
                            else if (tokens[position].Type == TokenType.Dot) newType += ".";
                            else newType += tokens[position].Value;
                            position++;
                        }
                        if (depth == 0) newType += ">";
                    }
                }

                // new Type[N] — 数组类型
                if (Match(TokenType.LeftBracket))
                {
                    if (!Check(TokenType.RightBracket))
                        ParseExpression();
                    Expect(TokenType.RightBracket, "Expected ']' after array type");
                    if (Match(TokenType.LeftBrace))
                    {
                        var arr = ParseArrayLiteral();
                        return arr;
                    }
                    return new ArrayLiteralExpression(new List<Expression>());
                }
                
                // new Type() — 构造函数调用
                if (Match(TokenType.LeftParen))
                {
                    // 跳过参数(简化处理)
                    while (!Check(TokenType.RightParen) && !IsAtEnd())
                        ParseExpression();
                    Expect(TokenType.RightParen, "Expected ')'");
                }
                
                return new NewExpression(newType);
            }

            // 数组字面量 (C#使用{})
            if (Match(TokenType.LeftBrace))
            {
                var arrayLiteral = ParseArrayLiteral();
                if (arrayLiteral != null)
                {
                    return arrayLiteral;
                }
                // 如果不是有效的数组字面量，继续解析
            }
            
            return null;
        }
        
        private Expression ParseArrayLiteral()
        {
            // C#数组字面量：{element1, element2, ...}
            var elements = new List<Expression>();
            
            if (!Check(TokenType.RightBrace))
            {
                do
                {
                    var expr = ParseExpression();
                    if (expr != null)
                    {
                        elements.Add(expr);
                    }
                    else
                    {
                        // 如果解析表达式失败，跳出循环
                        break;
                    }
                } while (Match(TokenType.Comma));
            }
            
            if (Check(TokenType.RightBrace))
            {
                Expect(TokenType.RightBrace, "Expected '}' after array literal");
                return new ArrayLiteralExpression(elements);
            }
            else
            {
                // 如果不是有效的数组字面量，返回null
                return null;
            }
        }

        private bool IsTypeKeyword(Token token)
        {
            if (token == null) return false;
            return token.Type is TokenType.Int or TokenType.Float or TokenType.Double
                or TokenType.Long or TokenType.Char or TokenType.Bool or TokenType.String
                or TokenType.Byte or TokenType.SByte or TokenType.Short or TokenType.UShort
                or TokenType.UInt or TokenType.ULong or TokenType.Decimal or TokenType.Object;
        }

        private string GetTypeKeywordName(Token token)
        {
            return token.Type switch
            {
                TokenType.Int => "int",
                TokenType.Float => "float",
                TokenType.Double => "double",
                TokenType.Long => "long",
                TokenType.Char => "char",
                TokenType.Bool => "bool",
                TokenType.String => "string",
                TokenType.Byte => "byte",
                TokenType.SByte => "sbyte",
                TokenType.Short => "short",
                TokenType.UShort => "ushort",
                TokenType.UInt => "uint",
                TokenType.ULong => "ulong",
                TokenType.Decimal => "decimal",
                TokenType.Object => "object",
                _ => token.Value
            };
        }
    }
}
