using CompilerBase;
using System.Collections.Generic;

namespace GoCompiler
{
    public partial class Parser : ParserBase<Token, TokenType>
    {
        private ASTNode ParseExpression()
        {
            return ParseExpressionWithoutAssignment();
        }

        private ASTNode ParseExpressionWithoutAssignment()
        {
            return ParseOr();
        }

        private ASTNode ParseOr()
        {
            var left = ParseAnd();

            while (GetTokenType(Cur) == TokenType.OR)
            {
                Advance();
                var right = ParseAnd();
                left = new BinaryOp("||", left, right);
            }

            return left;
        }

        private ASTNode ParseAnd()
        {
            var left = ParseEquality();

            while (GetTokenType(Cur) == TokenType.AND)
            {
                Advance();
                var right = ParseEquality();
                left = new BinaryOp("&&", left, right);
            }

            return left;
        }

        private ASTNode ParseEquality()
        {
            var left = ParseRelational();

            while (GetTokenType(Cur) == TokenType.EQ || GetTokenType(Cur) == TokenType.NE)
            {
                var op = Advance().Type == TokenType.EQ ? "==" : "!=";
                var right = ParseRelational();
                left = new BinaryOp(op, left, right);
            }

            return left;
        }

        private ASTNode ParseRelational()
        {
            var left = ParseShift();

            while (GetTokenType(Cur) == TokenType.LT || GetTokenType(Cur) == TokenType.LE || GetTokenType(Cur) == TokenType.GT || GetTokenType(Cur) == TokenType.GE)
            {
                var op = Advance().Type switch
                {
                    TokenType.LT => "<",
                    TokenType.LE => "<=",
                    TokenType.GT => ">",
                    TokenType.GE => ">=",
                    _ => throw Error("Unknown operator")
                };
                var right = ParseShift();
                left = new BinaryOp(op, left, right);
            }

            return left;
        }

        private ASTNode ParseShift()
        {
            var left = ParseAddSub();

            while (GetTokenType(Cur) == TokenType.LSHIFT || GetTokenType(Cur) == TokenType.RSHIFT)
            {
                var op = Advance().Type == TokenType.LSHIFT ? "<<" : ">>";
                var right = ParseAddSub();
                left = new BinaryOp(op, left, right);
            }

            return left;
        }

        private ASTNode ParseAddSub()
        {
            var left = ParseMulDiv();

            while (GetTokenType(Cur) == TokenType.PLUS || GetTokenType(Cur) == TokenType.MINUS)
            {
                string op = Advance().Type == TokenType.PLUS ? "+" : "-";
                var right = ParseMulDiv();
                left = new BinaryOp(op, left, right);
            }

            return left;
        }

        private ASTNode ParseMulDiv()
        {
            var left = ParseUnary();

            while (GetTokenType(Cur) == TokenType.STAR || GetTokenType(Cur) == TokenType.SLASH || GetTokenType(Cur) == TokenType.PERCENT || GetTokenType(Cur) == TokenType.AMPERSAND || GetTokenType(Cur) == TokenType.PIPE || GetTokenType(Cur) == TokenType.CARET || GetTokenType(Cur) == TokenType.AND_NOT)
            {
                string op;
                if (GetTokenType(Cur) == TokenType.AND_NOT)
                {
                    op = "&^";
                }
                else
                {
                    op = Advance().Type switch
                    {
                        TokenType.STAR => "*",
                        TokenType.SLASH => "/",
                        TokenType.PERCENT => "%",
                        TokenType.AMPERSAND => "&",
                        TokenType.PIPE => "|",
                        TokenType.CARET => "^",
                        _ => throw Error("Unknown operator")
                    };
                }
                var right = ParseUnary();
                left = new BinaryOp(op, left, right);
            }

            return left;
        }

        private ASTNode ParseUnary()
        {
            if (GetTokenType(Cur) == TokenType.PLUS)
            {
                Advance();
                return ParseUnary();
            }
            if (GetTokenType(Cur) == TokenType.MINUS)
            {
                Advance();
                return new UnaryOp("-", ParseUnary());
            }
            if (GetTokenType(Cur) == TokenType.NOT)
            {
                Advance();
                return new UnaryOp("!", ParseUnary());
            }
            if (GetTokenType(Cur) == TokenType.XOR)
            {
                Advance();
                return new UnaryOp("^", ParseUnary());
            }
            if (GetTokenType(Cur) == TokenType.STAR)
            {
                Advance();
                return new UnaryOp("*", ParseUnary());
            }
            if (GetTokenType(Cur) == TokenType.AMPERSAND)
            {
                Advance();
                return new UnaryOp("&", ParseUnary());
            }
            // Channel receive: <-ch
            if (Match(TokenType.ARROW))
            {
                return new ReceiveExpr(ParseUnary());
            }

            return ParsePrimary();
        }

        private ASTNode ParsePrimary()
        {
            ASTNode expr;

            switch (GetTokenType(Cur))
            {
                case TokenType.IDENTIFIER:
                    var name = Advance().Value as string;
                    // Go 预声明常量 true/false
                    if (name == "true")
                        return new BoolLiteral(true);
                    if (name == "false")
                        return new BoolLiteral(false);
                    // 复合字面量: TypeName{key: val, ...}
                    if (GetTokenType(Cur) == TokenType.LBRACE)
                    {
                        Advance(); // {
                        var lit = new CompositeLiteral();
                        lit.Type = new GoType(name);
                        if (GetTokenType(Cur) != TokenType.RBRACE)
                        {
                            lit.Elements.Add(ParseElement());
                            while (Match(TokenType.COMMA))
                            {
                                if (GetTokenType(Cur) == TokenType.RBRACE)
                                    break;
                                lit.Elements.Add(ParseElement());
                            }
                        }
                        Expect(TokenType.RBRACE);
                        expr = lit;
                    }
                    else
                    {
                        expr = new Identifier(name);
                    }
                    break;

                case TokenType.NUMBER:
                    var numValue = Advance().Value as string;
                    expr = new NumberLiteral(numValue, numValue.Contains(".") || numValue.Contains("e") || numValue.Contains("E"));
                    break;

                case TokenType.RAW_STRING:
                case TokenType.INTERPRETED_STRING:
                    var strValue = Advance().Value as string;
                    expr = new StringLiteral(strValue, Peek(-1).Type == TokenType.RAW_STRING);
                    break;

                case TokenType.FUNC:
                    expr = ParseFuncLit();
                    break;

                case TokenType.LBRACE:
                    // 复合字面量
                    return ParseCompositeLiteral();

                case TokenType.LBRACKET:
                    // 数组或切片
                    return ParseIndexOrSlice();

                case TokenType.MAP:
                    return ParseCompositeLiteral();

                case TokenType.STRUCT:
                    return ParseCompositeLiteral();

                // 类型关键字在表达式中：作为类型转换或类型名引用
                case TokenType.INT: case TokenType.INT8: case TokenType.INT16: case TokenType.INT32: case TokenType.INT64:
                case TokenType.UINT: case TokenType.UINT8: case TokenType.UINT16: case TokenType.UINT32: case TokenType.UINT64:
                case TokenType.FLOAT32: case TokenType.FLOAT64:
                case TokenType.COMPLEX64: case TokenType.COMPLEX128:
                case TokenType.BOOL: case TokenType.STRING: case TokenType.BYTE: case TokenType.RUNE:
                    return ParseTypeConversionOrIdent();

                case TokenType.LPAREN:
                    Advance(); // 消费 (
                    expr = ParseExpression();
                    Expect(TokenType.RPAREN);
                    break;

                case TokenType.CHAR:
                    var chValue = Advance().Value as string;
                    expr = new CharLiteral(chValue ?? "");
                    break;

                default:
                    Error($"意外的 token: {GetTokenType(Cur)}");
                    return null;
            }

            return ParseExpressionSuffix(expr);
        }

        /// <summary>
        /// 解析类型关键字表达式: int(x) 类型转换 或 int 类型名引用
        /// </summary>
        private ASTNode ParseTypeConversionOrIdent()
        {
            var typeName = Advance().Value as string;

            if (GetTokenType(Cur) == TokenType.LPAREN)
            {
                // 类型转换: int(x), float64(v)
                Advance(); // 消费 (
                var arg = ParseExpression();
                Expect(TokenType.RPAREN);
                return new TypeConversion(new GoType(typeName), arg);
            }
            else if (GetTokenType(Cur) == TokenType.LBRACE)
            {
                // 复合字面量: int{...} (不常见但语法合法)
                return ParseCompositeLiteralWithType(typeName);
            }

            // 单纯类型名引用（如用作标识符）
            var ident = new Identifier(typeName);
            return ParseExpressionSuffix(ident);
        }

        /// <summary>
        /// 解析已知类型的复合字面量: TypeName{field: val, ...}
        /// </summary>
        private ASTNode ParseCompositeLiteralWithType(string typeName)
        {
            Advance(); // 跳过 {
            var lit = new CompositeLiteral();
            lit.Type = new GoType(typeName);
            if (GetTokenType(Cur) != TokenType.RBRACE)
            {
                lit.Elements.Add(ParseElement());
                while (Match(TokenType.COMMA))
                {
                    if (GetTokenType(Cur) == TokenType.RBRACE)
                        break;
                    lit.Elements.Add(ParseElement());
                }
            }
            Expect(TokenType.RBRACE);
            return lit;
        }

        /// <summary>
        /// 解析表达式后缀
        /// </summary>
        private ASTNode ParseExpressionSuffix(ASTNode expr)
        {
            while (true)
            {
                switch (GetTokenType(Cur))
                {
                    case TokenType.LPAREN:
                        // 函数调用
                        Advance();
                        var call = new FunctionCall(expr);
                        if (GetTokenType(Cur) != TokenType.RPAREN)
                        {
                            call.Arguments.Add(ParseExpression());
                            while (Match(TokenType.COMMA))
                            {
                                call.Arguments.Add(ParseExpression());
                            }
                        }
                        Expect(TokenType.RPAREN);
                        expr = call;
                        break;

                    case TokenType.LBRACKET:
                        // 索引访问
                        Advance();
                        if (GetTokenType(Cur) == TokenType.COLON)
                        {
                            // 切片
                            var slice = new SliceExpr(expr);
                            if (Match(TokenType.COLON))
                            {
                                slice.High = ParseExpression();
                            }
                            Expect(TokenType.RBRACKET);
                            expr = slice;
                        }
                        else
                        {
                            var index = ParseExpression();
                            Expect(TokenType.RBRACKET);
                            expr = new IndexExpr(expr, index);
                        }
                        break;

                    case TokenType.PERIOD:
                        // 选择器
                        Advance();
                        var memberName = Expect(TokenType.IDENTIFIER).Value as string;
                        expr = new SelectorExpr(expr, memberName);
                        break;

                    case TokenType.LBRACE:
                        // Point{X: 10} — 复合字面量（类型已解析为 expr）
                        // 排除纯 Identifier（避免 switch x{ 被误解析为复合字面量）
                        // pure Identifier 的复合字面量在 ParsePrimary 中处理
                        if (expr is SelectorExpr || expr is IndexExpr)
                        {
                            // 将当前表达式作为类型名构建复合字面量
                            string typeName = expr.ToString();
                            Advance(); // 跳过 {
                            var lit = new CompositeLiteral();
                            // Type由调用者上下文确定，这里用解析器已知信息
                            if (GetTokenType(Cur) != TokenType.RBRACE)
                            {
                                lit.Elements.Add(ParseElement());
                                while (Match(TokenType.COMMA))
                                {
                                    if (GetTokenType(Cur) == TokenType.RBRACE)
                                        break;
                                    lit.Elements.Add(ParseElement());
                                }
                            }
                            Expect(TokenType.RBRACE);
                            expr = lit;
                        }
                        else
                        {
                            goto default;
                        }
                        break;

                    case TokenType.ASSIGN:
                        // 赋值语句 - 返回一个临时包装器，让调用者处理
                        return new AssignmentWrapper(expr);

                    default:
                        return expr;
                }
            }
        }

        /// <summary>
        /// 解析索引或切片
        /// </summary>
        private ASTNode ParseIndexOrSlice()
        {
            Expect(TokenType.LBRACKET);

            if (GetTokenType(Cur) == TokenType.RBRACKET)
            {
                // 切片 [:] 或切片类型 []T{...}
                Advance();
                if (IsTypeStart(GetTokenType(Cur)) && Peek(1).Type == TokenType.LBRACE)
                {
                    // []T{...} — 切片类型复合字面量
                    var typeName = "[]";
                    if (GetTokenType(Cur) == TokenType.IDENTIFIER)
                        typeName += Advance().Value;
                    else
                        typeName += Advance().Value; // type keyword value
                    return ParseCompositeLiteralWithType(typeName);
                }
                else if (IsTypeStart(GetTokenType(Cur)) && Peek(1).Type != TokenType.LBRACE)
                {
                    // []T — 切片类型（用于类型声明等，简化处理）
                    var typeName = "[]" + (Cur.Value ?? "");
                    Advance();
                    return new Identifier(typeName);
                }
                // 切片表达式 [:]
                Expect(TokenType.COLON);
                ASTNode high = null;
                if (GetTokenType(Cur) != TokenType.RBRACKET && GetTokenType(Cur) != TokenType.COMMA)
                {
                    high = ParseExpression();
                }
                var slice = new SliceExpr(new Identifier(""));
                slice.Low = new NumberLiteral("0");
                slice.High = high;
                return slice;
            }

            var index = ParseExpression();

            if (Match(TokenType.COLON))
            {
                // 切片
                var slice = new SliceExpr(index);
                if (GetTokenType(Cur) != TokenType.RBRACKET && GetTokenType(Cur) != TokenType.COMMA)
                {
                    slice.High = ParseExpression();
                }
                Expect(TokenType.RBRACKET);
                return slice;
            }

            Expect(TokenType.RBRACKET);
            return new IndexExpr(new Identifier(""), index);
        }

        /// <summary>
        /// 解析函数字面量 — 返回 FuncLiteral AST
        /// </summary>
        private FuncLiteral ParseFuncLit()
        {
            Expect(TokenType.FUNC);
            var lit = new FuncLiteral();

            Expect(TokenType.LPAREN);
            while (GetTokenType(Cur) != TokenType.RPAREN && GetTokenType(Cur) != TokenType.EOF)
            {
                var names = new List<string>();
                GoType paramType = null;

                if (GetTokenType(Cur) == TokenType.IDENTIFIER)
                {
                    var name = Advance().Value as string;
                    if (GetTokenType(Cur) == TokenType.ELLIPSIS)
                    {
                        Advance();
                        var typeName = Expect(TokenType.IDENTIFIER).Value as string;
                        paramType = new GoType(typeName);
                    }
                    else
                    {
                        names.Add(name);
                        if (GetTokenType(Cur) == TokenType.IDENTIFIER)
                        {
                            var typeName = Advance().Value as string;
                            paramType = new GoType(typeName);
                        }
                        else
                        {
                            paramType = new GoType("int");
                        }
                    }
                }
                else if (GetTokenType(Cur) == TokenType.ELLIPSIS)
                {
                    Advance();
                    var typeName = Expect(TokenType.IDENTIFIER).Value as string;
                    paramType = new GoType(typeName);
                }

                var param = new Parameter(names.Count > 0 ? names : new List<string>(), paramType);
                lit.Parameters.Add(param);
                if (!Match(TokenType.COMMA)) break;
            }
            Expect(TokenType.RPAREN);

            if (GetTokenType(Cur) == TokenType.IDENTIFIER && Cur.Value is string rtn)
            {
                if (rtn != "func" && rtn != "map" && rtn != "struct" && rtn != "interface" && rtn != "chan")
                {
                    Advance();
                    lit.Results.Add(new Parameter(new List<string> { rtn }, new GoType(rtn)));
                }
            }

            if (GetTokenType(Cur) == TokenType.LBRACE)
            {
                lit.Body = ParseBlock();
            }

            return lit;
        }

        /// <summary>
        /// 解析复合字面量
        /// </summary>
        private CompositeLiteral ParseCompositeLiteral()
        {
            var lit = new CompositeLiteral();

            // 类型
            if (GetTokenType(Cur) == TokenType.LBRACE)
            {
                // 切片/数组/映射/结构体复合字面量
                Advance();
            }
            else if (GetTokenType(Cur) == TokenType.IDENTIFIER || GetTokenType(Cur) == TokenType.STRUCT || GetTokenType(Cur) == TokenType.MAP)
            {
                lit.Type = ParseType();
            }

            Expect(TokenType.LBRACE);

            if (GetTokenType(Cur) != TokenType.RBRACE)
            {
                lit.Elements.Add(ParseElement());
                while (Match(TokenType.COMMA))
                {
                    if (GetTokenType(Cur) == TokenType.RBRACE)
                        break;
                    lit.Elements.Add(ParseElement());
                }
            }

            Expect(TokenType.RBRACE);
            return lit;
        }

        /// <summary>
        /// 解析复合字面量元素
        /// </summary>
        private ASTNode ParseElement()
        {
            SkipNewlines();

            // 键值对 (支持 string/number 等非标识符key)
            if (Peek(1).Type == TokenType.COLON && GetTokenType(Cur) != TokenType.RBRACE && GetTokenType(Cur) != TokenType.COMMA)
            {
                var keyToken = Advance();
                Expect(TokenType.COLON);
                ASTNode key;
                if (keyToken.Type == TokenType.IDENTIFIER)
                    key = new Identifier(keyToken.Value as string);
                else
                    key = new StringLiteral(keyToken.Value as string);
                var value = ParseExpression();
                return new KeyValueExpr(key, value);
            }

            // 普通值
            return ParseExpression();
        }
    }
}
