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
                var right = RequiredOperand(ParseConditional());
                // 简化：直接返回右值（null check 在运行时）
                return right;
            }

            if (Match(TokenType.Conditional))
            {
                var trueVal = ParseExpression();
                Expect(TokenType.Colon, "期望 ':' 用于条件表达式");
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
                var right = RequiredOperand(ParseLogicalAnd());
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
                var right = RequiredOperand(ParseBitwiseOr());
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
                var right = RequiredOperand(ParseBitwiseXor());
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
                var right = RequiredOperand(ParseBitwiseAnd());
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
                var right = RequiredOperand(ParseEquality());
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
                var right = RequiredOperand(ParseComparison());
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
                var right = RequiredOperand(ParseShift());
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
                var right = RequiredOperand(ParseTerm());
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
                var right = RequiredOperand(ParseFactor());
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
                var right = RequiredOperand(ParseUnary());
                expr = new BinaryExpression(expr, op.Type, right);
            }
            
            return expr;
        }
        
        private Expression ParseUnary()
        {
            if (Match(TokenType.Minus) || Match(TokenType.LogicalNot) || Match(TokenType.BitwiseNot))
            {
                var op = Previous();
                var right = RequiredOperand(ParseUnary());
                return new UnaryExpression(op.Type, right);
            }

            // Pointer dereference: *ptr
            if (Match(TokenType.Multiply))
            {
                var right = RequiredOperand(ParseUnary());
                return new DerefExpression(right);
            }

            // Address-of: &var
            if (Match(TokenType.BitwiseAnd))
            {
                var right = RequiredOperand(ParseUnary());
                return new AddrOfExpression(right);
            }

            if (Match(TokenType.Increment))
            {
                var right = RequiredOperand(ParseUnary());
                return new AssignmentExpression(right, TokenType.Increment, new LiteralExpression(1));
            }

            if (Match(TokenType.Decrement))
            {
                var right = RequiredOperand(ParseUnary());
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
                        Expect(TokenType.RightParen, "期望 ')' 在方法实参后");
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
                    Expect(TokenType.RightParen, "期望 ')' 在函数实参后");
                    expr = call;
                }
                // 数组索引：arr[index]
                else if (Match(TokenType.LeftBracket))
                {
                    var index = ParseExpression();
                    Expect(TokenType.RightBracket, "期望 ']' 在数组下标后");
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
        
        /// <summary>
        /// **原子表达式的唯一入口** —— 顺手盖上它自己的行列（`ASTNode.Line`/`Column`）。
        ///
        /// 与语句入口 `ParseStatement` 同一套路，但**粒度细一层**：语句级的列只能给到
        /// 「这一句从哪开始」（`int` / `return` 那一列），用户报错时要看到的却是
        /// **出错的那个标识符**从哪开始。原子（标识符/字面量/调用/括号）是位置信息
        /// 真正有意义的地方，而全部语句的表达式都是从这里递归产出的。
        ///
        /// ⚠ 用本类的 `Peek()` 而不是基类 `Cur` —— 这个解析器自己维护游标
        /// （`private int position`），基类的 `_pos` 从不移动（见 `ParseStatement` 那段）。
        /// </summary>
        private Expression ParsePrimary()
        {
            var __start = Peek();
            int __line = __start.Line, __col = __start.Column;
            var __node = ParsePrimaryCore();
            if (__node != null && __node.Line == 0) { __node.Line = __line; __node.Column = __col; }
            return __node;
        }

        /// <summary>
        /// 解析**必需**的操作数 —— 解析不出来就报带位置的语法错误。
        ///
        /// ⚠ 为什么不能把这条判据塞进 `ParsePrimary`（"认不出表达式开头就抛"）：
        ///   那个 `null` **是承重的**。`ParseStatementCore` 的分派顺序是
        ///   「表达式语句兜底」**在前**、「`using` / `namespace` / `class` 声明」在后
        ///   （见 `Parser.cs` 那几段），靠的正是"认不出表达式 ⇒ 返回 null ⇒
        ///   落到后面的语句种类"这个**试探语义**。一律抛出去，`class P { … }`
        ///   在第一句就被判成语法错误 —— 实测就是这么炸的（3 条 cs 用例全变成
        ///   `1:1 需要一个表达式，但遇到 Class 'class'`）。
        ///
        ///   所以「试探」与「必需」必须分开：
        ///   · **试探** = 语句开头的 `ParseExpression()`，允许 null（原样保留）；
        ///   · **必需** = 二元运算符**右边**那个操作数，缺了就是语法错误（本方法）。
        ///
        /// ⚠ 修之前的行为：`int b = a + ;` 编出「右操作数为 null 的
        ///   `BinaryExpression`」，解析期一句错都不报，**到代码生成才 NRE** ——
        ///   被 `CompilerHelper` 包成「内部错误: Object reference not set…」，
        ///   一句位置都没有（DiagProbe【语法错误】档 cs 一栏实测）。
        ///
        /// 位置取 `Peek()`（= 那个不该出现的 token），诊断的当前 token 由
        /// `CurrentToken` 覆写指向同一个 `Peek()`，两者同源、不会错位。
        /// </summary>
        private Expression RequiredOperand(Expression? parsed)
        {
            if (parsed != null) return parsed;
            // **报错，但把这一句接着解析下去** —— 这正是"错误分两类"里的前一类：
            // 少一个操作数不影响解析器继续认出后面的东西（`a + ; b = ] c` 里的后两处
            // 也应当一起报出来），所以不该当场把整份文件停掉。
            // 发个占位 0 顶上去，AST 才是完好的 —— 否则 null 会一路流到代码生成，
            // 变成一句没有位置的「内部错误」（修之前就是这个症状）。
            //
            // ⚠ `GccError` 是**收集**、`Error` 是**抛出**。这里必须用收集的那条：
            //   `ParserBase.Collect` 的注释里写了这个两分法。没有收集器时
            //   `GccError` 自己会抛，不会凭空吞掉。
            GccError($"这里缺少一个表达式，却遇到 {Peek().Type} '{Peek().Value}'",
                     ErrorCode.Parser_ExpectedExpression);
            return new LiteralExpression(0);
        }

        private Expression ParsePrimaryCore()
        {
            if (Match(TokenType.False)) return new LiteralExpression(false);
            if (Match(TokenType.True)) return new LiteralExpression(true);
            if (Match(TokenType.Null)) return new LiteralExpression(null);
            if (Match(TokenType.This)) return new VariableExpression("this");

            // typeof(int) → MCU不支持反射, 返回0
            if (Match(TokenType.Typeof))
            {
                Expect(TokenType.LeftParen, "期望 '(' 在 typeof 后");
                // 跳过类型名(可能包含泛型)
                SkipExpression();
                Expect(TokenType.RightParen, "期望 ')' 在 typeof 后");
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
                    Expect(TokenType.RightParen, "期望 ')' 在强制转换的类型后");
                    var operand = ParseUnary();
                    return new CastExpression(typeName, operand);
                }

                var expr = ParseExpression();
                if (debugMode)
                {
                    Console.WriteLine($"DEBUG ParsePrimary: after ParseExpression, current={Current?.Type}:{Current?.Value}");
                }
                Expect(TokenType.RightParen, "期望 ')' 在表达式后");
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
                    Expression sizeExpr = null;
                    if (!Check(TokenType.RightBracket))
                        sizeExpr = ParseExpression();
                    Expect(TokenType.RightBracket, "期望 ']' 在数组类型后");
                    if (Match(TokenType.LeftBrace))
                    {
                        var arr = ParseArrayLiteral();
                        return arr;
                    }
                    // ⚠ v0.96.190 修：原来这里是 `return new ArrayLiteralExpression(new List<Expression>())`
                    //   —— **长度表达式被整个丢掉**。`GenerateArrayLiteral` 是按元素个数预留空间的
                    //   （数据段里 `[count, e0, e1, …]` 一整块），于是 `new int[40]` 只留了
                    //   count 一个字、**一个元素的空间都没有** ⇒ 读写全落到块外的相邻数据上。
                    //   实测症状：`a[2] = 7; if (a[2] == 7)` **为假**，读回来恒是 0；
                    //   `ui_rect(..., a[2]*10, ...)` 画出来是一条 1 像素的细线。
                    //   现在把**长度表达式留给代码生成器**折（常量标识符只有那边认识 ——
                    //   `RegisterStaticField` 已经把 `const` 字段折进了 `dataSection`）。
                    return new ArrayLiteralExpression(new List<Expression>()) { SizeExpr = sizeExpr };
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
                Expect(TokenType.RightBrace, "期望 '}' 在数组字面量后");
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
