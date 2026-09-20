using System.Collections.Generic;
using CompilerBase;

namespace JavaScriptCompiler
{
    /// <summary>
    /// JavaScript语法分析器
    /// </summary>
    public class Parser : ParserBase<Token, TokenType>
    {
        protected override TokenType GetTokenType(Token token) => token.Type;

        /// <summary>
        /// 表达式**收尾符** —— 永远不会是表达式的开头（`GapAnchor()` 的第一个判据）。
        /// `EndOfFile` 必须算进来：`var x = 1 +` 结尾会撞上它。
        /// </summary>
        protected override bool IsExpressionCloser(Token token) => token.Type
            is TokenType.RightParen or TokenType.RightBrace or TokenType.RightBracket
            or TokenType.Comma or TokenType.Semicolon or TokenType.Colon
            or TokenType.EndOfFile;

        /// <summary>语句分隔 —— JS 的 `;`（换行不是本前端的词法单元）。</summary>
        protected override bool IsStatementSeparator(Token token) => token.Type
            is TokenType.Semicolon;

        public Parser(List<Token> tokens) : base(tokens) { }

        /// <summary>
        /// 解析JavaScript程序
        /// </summary>
        public Program Parse()
        {
            var program = new Program();

            while (!IsAtEnd)
            {
                program.Statements.Add(ParseStatement());
            }

            return program;
        }

        /// <summary>
        /// 语句入口 —— **顺手给每条语句盖上起始行列**（`ASTNode.Line`/`Column`）。
        ///
        /// 与 C/Rust 同一套路：这个方法是**单一入口**，包一层就覆盖了全部 return
        ///（含递归进来的嵌套语句）。用 `Line == 0` 才盖 ——
        /// 子解析器自己填过的更精确位置不会被外层冲掉。
        ///
        /// ⚠ 加这个之前 `ASTNode` 是个**空基类**，一个位置字段都没有 ⇒
        ///   代码生成侧那句 `if (node.Line > 0) …` 永远不生效，报错只能给个名字。
        /// </summary>
        private Statement ParseStatement()
        {
            int __line = Cur.Line, __col = Cur.Column;
            var __node = ParseStatementCore();
            if (__node != null && __node.Line == 0) { __node.Line = __line; __node.Column = __col; }
            return __node;
        }

        private Statement ParseStatementCore()
        {
            if (Check(TokenType.Keyword))
            {
                string keyword = Cur.Value;

                switch (keyword)
                {
                    case "var":
                    case "let":
                    case "const":
                        Advance();
                        return ParseVariableDecl(keyword);
                    case "native":
                        Advance();
                        Expect(TokenType.Keyword, "expected 'function' after 'native'");
                        if (Previous().Value == "function")
                        {
                            var nativeFunc = ParseFunctionDecl();
                            nativeFunc.IsNative = true;
                            return nativeFunc;
                        }
                        throw Error($"'native' 后出现意外的 token: {Previous().Value}");

                    case "function":
                        Advance();
                        return ParseFunctionDecl();
                    case "class":
                        Advance();
                        return ParseClassDecl();
                    case "return":
                        Advance();
                        return ParseReturnStatement();
                    case "if":
                        Advance();
                        return ParseIfStatement();
                    case "while":
                        Advance();
                        return ParseWhileStatement();
                    case "for":
                        Advance();
                        return ParseForStatement();
                    case "do":
                        Advance();
                        return ParseDoWhileStatement();
                    case "switch":
                        Advance();
                        return ParseSwitchStatement();
                    case "throw":
                        Advance();
                        return ParseThrowStatement();
                    case "try":
                        Advance();
                        return ParseTryStatement();
                    case "break":
                    case "continue":
                        Advance();
                        return ParseBreakOrContinue(keyword);
                }
            }

            // 处理块语句
            if (Match(TokenType.LeftBrace))
            {
                return ParseBlock();
            }

            // 处理空语句（额外的分号）
            if (Match(TokenType.Semicolon))
            {
                return new ExpressionStatement(null);
            }

            // 默认：表达式语句
            return ParseExpressionStatement();
        }

        private Statement ParseVariableDecl(string keyword)
        {
            // Array destructuring: var [a, b] = expr
            if (Match(TokenType.LeftBracket))
            {
                var names = new List<string>();
                if (!Check(TokenType.RightBracket))
                {
                    do
                    {
                        names.Add(Expect(TokenType.Identifier, "期望变量名").Value);
                    } while (Match(TokenType.Comma));
                }
                Expect(TokenType.RightBracket, "期望 ']'");
                Expression initializer = null;
                if (Match(TokenType.Assign))
                    initializer = ParseExpression();
                ExpectSemicolon();
                return new ArrayDestructureStatement { Names = names, Initializer = initializer };
            }

            // Object destructuring: var {x, y} = expr
            if (Match(TokenType.LeftBrace))
            {
                var names = new List<string>();
                if (!Check(TokenType.RightBrace))
                {
                    do
                    {
                        names.Add(Expect(TokenType.Identifier, "期望属性名").Value);
                    } while (Match(TokenType.Comma));
                }
                Expect(TokenType.RightBrace, "期望 '}'");
                Expression initializer = null;
                if (Match(TokenType.Assign))
                    initializer = ParseExpression();
                ExpectSemicolon();
                return new ObjectDestructureStatement { Names = names, Initializer = initializer };
            }

            var name = Expect(TokenType.Identifier, "期望变量名").Value;
            Expression init = null;

            if (Match(TokenType.Assign))
            {
                init = ParseExpression();
            }

            // 支持逗号分隔的多变量声明: var a=1,b=2,c=3;
            var decls = new List<VariableDeclStatement>();
            decls.Add(new VariableDeclStatement(keyword, name, init));
            while (Match(TokenType.Comma))
            {
                var nextName = Expect(TokenType.Identifier, "期望变量名").Value;
                Expression nextInit = null;
                if (Match(TokenType.Assign))
                {
                    nextInit = ParseExpression();
                }
                decls.Add(new VariableDeclStatement(keyword, nextName, nextInit));
            }

            ExpectSemicolon();
            if (decls.Count == 1)
                return decls[0];
            var block = new Block();
            block.Statements.AddRange(decls);
            return block;
        }

        private void ExpectSemicolon(string message = "期望 ';'")
        {
            if (Match(TokenType.Semicolon)) return;
            if (_pos > 0 && _tokens.Count > _pos)
            {
                int prevLine = _tokens[_pos - 1].Line;
                int currLine = _tokens[_pos].Line;
                if (currLine > prevLine) return;
            }
            if (Check(TokenType.RightBrace) || IsAtEnd) return;
            Expect(TokenType.Semicolon, message);
        }

        private FunctionDeclStatement ParseFunctionDecl()
        {
            var name = Expect(TokenType.Identifier, "期望函数名").Value;
            Expect(TokenType.LeftParen, "期望 '('");

            var function = new FunctionDeclStatement(name);

            // 解析参数
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    var paramName = Expect(TokenType.Identifier, "期望参数名").Value;
                    Expression defaultValue = null;
                    if (Match(TokenType.Assign))
                    {
                        defaultValue = ParseExpression();
                    }
                    function.Parameters.Add(new ParameterDef(paramName, defaultValue));
                } while (Match(TokenType.Comma));
            }

            Expect(TokenType.RightParen, "期望 ')'");
            Expect(TokenType.LeftBrace, "期望 '{'");

            // 解析函数体
            while (!Check(TokenType.RightBrace) && !IsAtEnd)
            {
                function.Body.Statements.Add(ParseStatement());
            }

            Expect(TokenType.RightBrace, "期望 '}'");
            Match(TokenType.Semicolon); // 可选的分号
            return function;
        }

        private ClassDeclStatement ParseClassDecl()
        {
            var name = Expect(TokenType.Identifier, "期望类名").Value;
            var classDecl = new ClassDeclStatement { Name = name };

            if (Match(TokenType.Keyword) && Previous().Value == "extends")
            {
                classDecl.ParentClass = Expect(TokenType.Identifier, "期望父类名").Value;
            }

            Expect(TokenType.LeftBrace, "期望 '{'");

            while (!Check(TokenType.RightBrace) && !IsAtEnd)
            {
                bool isNativeMethod = false;
                if (Match(TokenType.Keyword) && Previous().Value == "static")
                {
                    // static methods ignored in MCU mode
                }
                if (Match(TokenType.Keyword) && Previous().Value == "native")
                {
                    isNativeMethod = true;
                }

                string methodName = Expect(TokenType.Identifier, "期望方法名").Value;
                if (methodName == "constructor")
                {
                    // Parse constructor: already consumed name, parse (params){body}
                    Expect(TokenType.LeftParen, "期望 '('");
                    var ctor = new FunctionDeclStatement("constructor");
                    if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                            var paramName = Expect(TokenType.Identifier, "期望参数名").Value;
                            Expression defaultValue = null;
                            if (Match(TokenType.Assign)) { defaultValue = ParseExpression(); }
                            ctor.Parameters.Add(new ParameterDef(paramName, defaultValue));
                        } while (Match(TokenType.Comma));
                    }
                    Expect(TokenType.RightParen, "期望 ')'");
                    Expect(TokenType.LeftBrace, "期望 '{'");
                    while (!Check(TokenType.RightBrace) && !IsAtEnd)
                        ctor.Body.Statements.Add(ParseStatement());
                    Expect(TokenType.RightBrace, "期望 '}'");
                    classDecl.Constructor = ctor;
                }
                else
                {
                    // Parse method: already consumed name, parse (params){body}
                    Expect(TokenType.LeftParen, "期望 '('");
                    var method = new FunctionDeclStatement(methodName);
                    method.IsNative = isNativeMethod;
                    if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                            var paramName = Expect(TokenType.Identifier, "期望参数名").Value;
                            Expression defaultValue = null;
                            if (Match(TokenType.Assign)) { defaultValue = ParseExpression(); }
                            method.Parameters.Add(new ParameterDef(paramName, defaultValue));
                        } while (Match(TokenType.Comma));
                    }
                    Expect(TokenType.RightParen, "期望 ')'");
                    Expect(TokenType.LeftBrace, "期望 '{'");
                    while (!Check(TokenType.RightBrace) && !IsAtEnd)
                        method.Body.Statements.Add(ParseStatement());
                    Expect(TokenType.RightBrace, "期望 '}'");
                    classDecl.Methods.Add(method);
                }
            }

            Expect(TokenType.RightBrace, "期望 '}'");
            Match(TokenType.Semicolon);
            return classDecl;
        }

        private ReturnStatement ParseReturnStatement()
        {
            Expression value = null;

            if (!Check(TokenType.Semicolon))
            {
                value = ParseExpression();
            }

            ExpectSemicolon();
            return new ReturnStatement(value);
        }

        private IfStatement ParseIfStatement()
        {
            Expect(TokenType.LeftParen, "期望 '('");
            var condition = ParseExpression();
            Expect(TokenType.RightParen, "期望 ')'");

            var thenBranch = ParseStatement();
            Statement elseBranch = null;

            if (Check(TokenType.Keyword) && _tokens[_pos].Value == "else")
            {
                Advance();
                elseBranch = ParseStatement();
            }

            return new IfStatement(condition, thenBranch, elseBranch);
        }

        private WhileStatement ParseWhileStatement()
        {
            Expect(TokenType.LeftParen, "期望 '('");
            var condition = ParseExpression();
            Expect(TokenType.RightParen, "期望 ')'");

            var body = ParseStatement();
            return new WhileStatement(condition, body);
        }

        private ForStatement ParseForStatement()
        {
            Expect(TokenType.LeftParen, "期望 '('");

            Statement initializer = null;
            if (!Check(TokenType.Semicolon))
            {
                initializer = ParseVariableDeclOrExpression();
            }
            ExpectSemicolon();

            Expression condition = null;
            if (!Check(TokenType.Semicolon))
            {
                condition = ParseExpression();
            }
            ExpectSemicolon();

            Expression increment = null;
            if (!Check(TokenType.RightParen))
            {
                increment = ParseExpression();
            }
            Expect(TokenType.RightParen, "期望 ')'");

            var body = ParseStatement();
            return new ForStatement(initializer, condition, increment, body);
        }

        private Statement ParseVariableDeclOrExpression()
        {
            if (Match(TokenType.Keyword) && (Previous().Value == "var" || Previous().Value == "let" || Previous().Value == "const"))
            {
                return ParseForInitializerDecl(Previous().Value);
            }
            else
            {
                // 只解析表达式，不消费分号（ParseForStatement会处理）
                var expr = ParseExpression();
                return new ExpressionStatement(expr);
            }
        }

        private VariableDeclStatement ParseForInitializerDecl(string keyword)
        {
            var name = Expect(TokenType.Identifier, "期望变量名").Value;
            Expression initializer = null;

            if (Match(TokenType.Assign))
            {
                initializer = ParseExpression();
            }

            return new VariableDeclStatement(keyword, name, initializer);
        }

        private Statement ParseDoWhileStatement()
        {
            var body = ParseStatement();
            Expect(TokenType.Keyword, "期望 'while'");
            // 位置走统一出口（`ErrorAt` 把位置拼成 `文件:行:列: error:` 并查 `#include` 映射）；
            // 两参 `new ParseException(msg, token)` **不带位置**（`Line = 0`）⇒ 报出去锚不到行。
            if (Previous().Value != "while") throw ErrorAt("期望 'while'", Previous());
            Expect(TokenType.LeftParen, "期望 '('");
            var condition = ParseExpression();
            Expect(TokenType.RightParen, "期望 ')'");
            Match(TokenType.Semicolon);
            return new DoWhileStatement(body, condition);
        }

        private Statement ParseSwitchStatement()
        {
            Expect(TokenType.LeftParen, "期望 '('");
            var value = ParseExpression();
            Expect(TokenType.RightParen, "期望 ')'");
            Expect(TokenType.LeftBrace, "期望 '{'");
            var sw = new SwitchStatement(value);
            while (!Check(TokenType.RightBrace) && !IsAtEnd)
            {
                if (Check(TokenType.Keyword))
                {
                    string kw = Cur.Value;
                    if (kw == "case")
                    {
                        Advance(); // consume 'case'
                        var caseVal = ParseExpression();
                        Expect(TokenType.Colon, "期望 ':'");
                        var sc = new SwitchCase { Value = caseVal };
                        while (!Check(TokenType.RightBrace) && !(Check(TokenType.Keyword) && (Cur.Value == "case" || Cur.Value == "default")) && !IsAtEnd)
                        {
                            var stmt = ParseStatement();
                            sc.Body.Add(stmt);
                            if (stmt is BreakStatement) break;
                        }
                        sw.Cases.Add(sc);
                    }
                    else if (kw == "default")
                    {
                        Advance(); // consume 'default'
                        Expect(TokenType.Colon, "期望 ':'");
                        var sc = new SwitchCase { Value = null };
                        while (!Check(TokenType.RightBrace) && !(Check(TokenType.Keyword) && (Cur.Value == "case" || Cur.Value == "default")) && !IsAtEnd)
                        {
                            var stmt = ParseStatement();
                            sc.Body.Add(stmt);
                            if (stmt is BreakStatement) break;
                        }
                        sw.Cases.Add(sc);
                    }
                    else break;
                }
                else break;
            }
            Expect(TokenType.RightBrace, "期望 '}'");
            return sw;
        }

        private Statement ParseThrowStatement()
        {
            var value = ParseExpression();
            ExpectSemicolon();
            return new ThrowStatement(value);
        }

        private Statement ParseTryStatement()
        {
            var body = ParseStatement();
            var ts = new TryStatement(body);
            if (Match(TokenType.Keyword) && Previous().Value == "catch")
            {
                var cc = new CatchClause();
                if (Match(TokenType.LeftParen))
                {
                    cc.VariableName = Expect(TokenType.Identifier, "期望异常变量名").Value;
                    Expect(TokenType.RightParen, "期望 ')'");
                }
                cc.Body = ParseStatement();
                ts.Catches.Add(cc);
            }
            return ts;
        }

        private ExpressionStatement ParseExpressionStatement()
        {
            var expression = ParseExpression();
            ExpectSemicolon();
            return new ExpressionStatement(expression);
        }

        private Block ParseBlock()
        {
            var block = new Block();
            while (!Check(TokenType.RightBrace) && !IsAtEnd)
            {
                block.Statements.Add(ParseStatement());
            }
            Expect(TokenType.RightBrace, "期望 '}'");
            return block;
        }

        private Statement ParseBreakOrContinue(string keyword)
        {
            ExpectSemicolon();
            if (keyword == "break")
                return new BreakStatement();
            else
                return new ContinueStatement();
        }

        private Expression ParseExpression()
        {
            return ParseConditional();
        }

        private Expression ParseConditional()
        {
            var expr = ParseAssignment();
            if (Match(TokenType.Question))
            {
                var trueVal = ParseExpression();
                Expect(TokenType.Colon, "期望 ':' 用于条件表达式");
                var falseVal = ParseConditional();
                return new ConditionalExpression(expr, trueVal, falseVal);
            }
            return expr;
        }

        private Expression ParseAssignment()
        {
            var expr = ParseLogicalOr();

            if (Match(TokenType.Assign, TokenType.PlusAssign, TokenType.MinusAssign,
                     TokenType.MultiplyAssign, TokenType.DivideAssign, TokenType.ModuloAssign))
            {
                var op = Previous().Type;
                var right = ParseAssignment();
                return new AssignmentExpression(expr, op, right);
            }

            return expr;
        }

        private Expression ParseLogicalOr()
        {
            var expr = ParseLogicalAnd();

            while (Match(TokenType.LogicalOr))
            {
                var op = Previous().Type;
                var right = ParseLogicalAnd();
                expr = new BinaryExpression(expr, op, right);
            }

            return expr;
        }

        private Expression ParseLogicalAnd()
        {
            var expr = ParseBitwiseOr();

            while (Match(TokenType.LogicalAnd))
            {
                var op = Previous().Type;
                var right = ParseBitwiseOr();
                expr = new BinaryExpression(expr, op, right);
            }

            return expr;
        }

        private Expression ParseBitwiseOr()
        {
            var expr = ParseBitwiseXor();
            while (Match(TokenType.BitwiseOr))
            {
                var op = Previous().Type;
                var right = ParseBitwiseXor();
                expr = new BinaryExpression(expr, op, right);
            }
            return expr;
        }

        private Expression ParseBitwiseXor()
        {
            var expr = ParseBitwiseAnd();
            while (Match(TokenType.BitwiseXor))
            {
                var op = Previous().Type;
                var right = ParseBitwiseAnd();
                expr = new BinaryExpression(expr, op, right);
            }
            return expr;
        }

        private Expression ParseBitwiseAnd()
        {
            var expr = ParseEquality();
            while (Match(TokenType.BitwiseAnd))
            {
                var op = Previous().Type;
                var right = ParseEquality();
                expr = new BinaryExpression(expr, op, right);
            }
            return expr;
        }

        private Expression ParseEquality()
        {
            var expr = ParseComparison();

            while (Match(TokenType.Equal, TokenType.NotEqual, TokenType.StrictEqual, TokenType.StrictNotEqual))
            {
                var op = Previous().Type;
                var right = ParseComparison();
                expr = new BinaryExpression(expr, op, right);
            }

            return expr;
        }

        private Expression ParseComparison()
        {
            var expr = ParseShift();

            while (Match(TokenType.LessThan, TokenType.LessThanOrEqual,
                        TokenType.GreaterThan, TokenType.GreaterThanOrEqual))
            {
                var op = Previous().Type;
                var right = ParseShift();
                expr = new BinaryExpression(expr, op, right);
            }

            // 'in' operator: "a" in obj
            if (Check(TokenType.Keyword) && Cur.Value == "in")
            {
                Advance(); // consume 'in'
                var right = ParseShift();
                expr = new InExpression(expr, right);
            }

            // instanceof
            if (Check(TokenType.Keyword) && Cur.Value == "instanceof")
            {
                Advance(); // consume 'instanceof'
                string typeName = Expect(TokenType.Identifier, "期望类型名在 'instanceof' 后").Value;
                expr = new InstanceofExpression(expr, typeName);
            }

            return expr;
        }

        private Expression ParseShift()
        {
            var expr = ParseAddition();
            while (Match(TokenType.LeftShift, TokenType.RightShift, TokenType.UnsignedRightShift))
            {
                var op = Previous().Type;
                var right = ParseAddition();
                expr = new BinaryExpression(expr, op, right);
            }
            return expr;
        }

        private Expression ParseAddition()
        {
            var expr = ParseMultiplication();

            while (Match(TokenType.Plus, TokenType.Minus))
            {
                var op = Previous().Type;
                var right = ParseMultiplication();
                expr = new BinaryExpression(expr, op, right);
            }

            return expr;
        }

        private Expression ParseMultiplication()
        {
            var expr = ParseExponent();

            while (Match(TokenType.Multiply, TokenType.Divide, TokenType.Modulo))
            {
                var op = Previous().Type;
                var right = ParseExponent();
                expr = new BinaryExpression(expr, op, right);
            }

            return expr;
        }

        /// <summary>** 幂运算 — 最高优先级，右结合</summary>
        private Expression ParseExponent()
        {
            var expr = ParseUnary();

            if (Match(TokenType.Exponent))
            {
                var right = ParseExponent(); // 右结合
                expr = new BinaryExpression(expr, TokenType.Exponent, right);
            }

            return expr;
        }

        private Expression ParseUnary()
        {
            if (Match(TokenType.Plus, TokenType.Minus, TokenType.LogicalNot, TokenType.BitwiseNot,
                     TokenType.Increment, TokenType.Decrement))
            {
                var op = Previous().Type;
                var operand = ParseUnary();
                return new UnaryExpression(op, operand);
            }
            if (Check(TokenType.Keyword) && Cur.Value == "typeof")
            {
                Advance();
                var operand = ParseUnary();
                return new UnaryExpression(TokenType.Typeof, operand);
            }
            
            var expr = ParsePrimary();

            // Postfix ++/--
            if (Match(TokenType.Increment))
                return new UnaryExpression(TokenType.Increment, expr, isPostfix: true);
            if (Match(TokenType.Decrement))
                return new UnaryExpression(TokenType.Decrement, expr, isPostfix: true);

            return expr;
        }

        private Expression ParsePrimary()
        {
            //System.Diagnostics.Debug.WriteLine($"JS ParsePrimary: {Cur} (line {Cur.Line}:{Cur.Column})");
            if (Match(TokenType.NumberLiteral))
            {
                var value = Previous().Value;
                if (value.StartsWith("0x") || value.StartsWith("0X"))
                {
                    int hexVal = Convert.ToInt32(value.Substring(2), 16);
                    return new LiteralExpression(hexVal, "number");
                }
                if (int.TryParse(value, out int intValue))
                {
                    return new LiteralExpression(intValue, "number");
                }
                else if (double.TryParse(value, out double doubleValue))
                {
                    return new LiteralExpression(doubleValue, "number");
                }
            }

            if (Match(TokenType.StringLiteral))
            {
                var value = Previous().Value;
                return new LiteralExpression(value, "string");
            }

            if (Match(TokenType.True))
            {
                return new LiteralExpression(true, "boolean");
            }

            if (Match(TokenType.False))
            {
                return new LiteralExpression(false, "boolean");
            }

            if (Match(TokenType.Null))
            {
                return new LiteralExpression(null, "object");
            }

            if (Match(TokenType.Undefined))
            {
                return new LiteralExpression(null, "undefined");
            }

            if (Check(TokenType.Keyword) && Cur.Value == "function")
            {
                Advance(); // consume 'function'
                string? name = null;
                if (Check(TokenType.Identifier))
                {
                    int lookahead = _pos + 1;
                    if (lookahead < _tokens.Count && _tokens[lookahead].Type == TokenType.LeftParen)
                    {
                        // anonymous function - named not supported in expression
                    }
                    else
                    {
                        name = Advance().Value;
                    }
                }
                if (name == null && Check(TokenType.Identifier))
                    name = Advance().Value;
                var fe = new FunctionExpression { Name = name };
                Expect(TokenType.LeftParen, "期望 '('");
                if (!Check(TokenType.RightParen))
                {
                    do {
                        var feParamName = Expect(TokenType.Identifier, "期望参数名").Value;
                        Expression feDefaultValue = null;
                        if (Match(TokenType.Assign))
                        {
                            feDefaultValue = ParseExpression();
                        }
                        fe.Parameters.Add(new ParameterDef(feParamName, feDefaultValue));
                    } while (Match(TokenType.Comma));
                }
                Expect(TokenType.RightParen, "期望 ')'");
                Expect(TokenType.LeftBrace, "期望 '{'");
                while (!Check(TokenType.RightBrace) && !IsAtEnd)
                {
                    fe.Body.Statements.Add(ParseStatement());
                }
                Expect(TokenType.RightBrace, "期望 '}'");
                return fe;
            }

            if (Check(TokenType.Keyword) && Cur.Value == "new")
            {
                Advance(); // consume 'new'
                string typeName = Expect(TokenType.Identifier, "期望类型名").Value;
                var newExpr = new NewExpression(typeName);
                if (Match(TokenType.LeftParen))
                {
                    if (!Check(TokenType.RightParen))
                    {
                        do { newExpr.Arguments.Add(ParseExpression()); } while (Match(TokenType.Comma));
                    }
                    Expect(TokenType.RightParen, "期望 ')'");
                }
                return newExpr;
            }

            if (Check(TokenType.Keyword) && Cur.Value == "super")
            {
                Advance(); // consume 'super'
                if (Match(TokenType.LeftParen))
                {
                    var superExpr = new SuperExpression();
                    if (!Check(TokenType.RightParen))
                    {
                        do { superExpr.Arguments.Add(ParseExpression()); } while (Match(TokenType.Comma));
                    }
                    Expect(TokenType.RightParen, "期望 ')'");
                    return superExpr;
                }
                // super.method(args) or super.property
                if (Match(TokenType.Dot))
                {
                    string member = Expect(TokenType.Identifier, "期望成员名在 super. 后").Value;
                    if (Match(TokenType.LeftParen))
                    {
                        // super.method(args) → CallExpression on super member
                        var superMember = new MemberExpression(new VariableExpression("super"), member);
                        var callExpr = new CallExpression(superMember);
                        if (!Check(TokenType.RightParen))
                        {
                            do { callExpr.Arguments.Add(ParseExpression()); } while (Match(TokenType.Comma));
                        }
                        Expect(TokenType.RightParen, "期望 ')'");
                        return callExpr;
                    }
                    // super.property
                    return new MemberExpression(new VariableExpression("super"), member);
                }
                throw Error("super 仅支持 super()/super.method()/super.property");
            }

            Expression primaryExpr = null;

            if (Match(TokenType.Keyword) && Previous().Value == "this")
            {
                primaryExpr = new VariableExpression("this");
            }
            else if (Match(TokenType.Identifier))
            {
                var name = Previous().Value;
                // 处理后置 ++/--
                if (Match(TokenType.Increment))
                    return new UnaryExpression(TokenType.Increment, new VariableExpression(name), isPostfix: true);
                if (Match(TokenType.Decrement))
                    return new UnaryExpression(TokenType.Decrement, new VariableExpression(name), isPostfix: true);

                primaryExpr = new VariableExpression(name);
            }

            if (primaryExpr != null)
            {
                // 处理成员访问和方法调用链
                while (true)
                {
                    // 成员访问：console.log
                    if (Match(TokenType.Dot))
                    {
                        var memberName = Expect(TokenType.Identifier, "期望成员名").Value;

                        // 检查是否是方法调用：console.log()
                        if (Match(TokenType.LeftParen))
                        {
                            var methodCall = new CallExpression(new MemberExpression(primaryExpr, memberName));
                            if (!Check(TokenType.RightParen))
                            {
                                do
                                {
                                    methodCall.Arguments.Add(ParseExpression());
                                } while (Match(TokenType.Comma));
                            }
                            Expect(TokenType.RightParen, "期望 ')'");
                            primaryExpr = methodCall;
                        }
                        else
                        {
                            // 属性访问：console.log（没有括号）
                            primaryExpr = new MemberExpression(primaryExpr, memberName);
                        }
                    }
                    // 直接函数调用：log()
                    else if (Match(TokenType.LeftParen))
                    {
                        var call = new CallExpression(primaryExpr);
                        if (!Check(TokenType.RightParen))
                        {
                            do
                            {
                                call.Arguments.Add(ParseExpression());
                            } while (Match(TokenType.Comma));
                        }
                        Expect(TokenType.RightParen, "期望 ')'");
                        primaryExpr = call;
                    }
                    // 计算属性访问：obj[expr]
                    else if (Match(TokenType.LeftBracket))
                    {
                        var indexExpr = ParseExpression();
                        Expect(TokenType.RightBracket, "期望 ']'");
                        primaryExpr = new IndexExpression(primaryExpr, indexExpr);
                    }
                    else
                    {
                        break;
                    }
                }

                return primaryExpr;
            }

            if (Match(TokenType.LeftParen))
            {
                // Try parsing as arrow function parameters: (x, y) => ...
                int savePos = _pos;
                var params_list = new List<ParameterDef>();
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        if (Check(TokenType.Identifier))
                        {
                            var arrowParamName = Advance().Value;
                            Expression arrowDefault = null;
                            if (Match(TokenType.Assign))
                            {
                                arrowDefault = ParseExpression();
                            }
                            params_list.Add(new ParameterDef(arrowParamName, arrowDefault));
                        }
                        else
                        {
                            _pos = savePos;
                            goto parse_grouped;
                        }
                    } while (Match(TokenType.Comma));
                }
                if (Check(TokenType.RightParen))
                {
                    Advance(); // skip )
                    if (Match(TokenType.Arrow))
                    {
                        // Arrow function: (x, y) => expr or (x, y) => { body }
                        var arrow = new ArrowFunctionExpression { Parameters = params_list };
                        if (Match(TokenType.LeftBrace))
                        {
                            arrow.Body = ParseBlock();
                        }
                        else
                        {
                            arrow.Body = new ExpressionStatement(ParseExpression());
                        }
                        return arrow;
                    }
                }
                _pos = savePos;

            parse_grouped:
                var expr = ParseExpression();
                Expect(TokenType.RightParen, "期望 ')'");
                return new ParenthesizedExpression(expr);
            }

            // 数组字面量
            if (Match(TokenType.LeftBracket))
            {
                var array = new ArrayLiteralExpression();
                if (!Check(TokenType.RightBracket))
                {
                    do
                    {
                        array.Elements.Add(ParseExpression());
                    } while (Match(TokenType.Comma));
                }
                Expect(TokenType.RightBracket, "期望 ']'");
                return array;
            }

            // 对象字面量
            if (Match(TokenType.LeftBrace))
            {
                var obj = new ObjectLiteralExpression();
                if (!Check(TokenType.RightBrace))
                {
                    do
                    {
                        // 计算属性: { [expr]: value }
                        if (Match(TokenType.LeftBracket))
                        {
                            var keyExpr = ParseExpression();
                            Expect(TokenType.RightBracket, "期望 ']'");
                            Expect(TokenType.Colon, "期望 ':'");
                            var value = ParseExpression();
                            obj.ComputedProperties.Add((keyExpr, value));
                        }
                        else
                        {
                            var key = Expect(TokenType.Identifier, "期望属性名").Value;
                            Expect(TokenType.Colon, "期望 ':'");
                            var value = ParseExpression();
                            obj.Properties[key] = value;
                        }
                    } while (Match(TokenType.Comma));
                }
                Expect(TokenType.RightBrace, "期望 '}'");
                return obj;
            }

            // 模板字符串 `...`
            if (Match(TokenType.Backtick))
            {
                var template = new TemplateExpression();
                while (!Check(TokenType.Backtick) && !IsAtEnd)
                {
                    if (Check(TokenType.StringLiteral))
                    {
                        var strToken = Advance();
                        template.Parts.Add(new LiteralExpression(strToken.Value, "string"));
                    }
                    else if (Check(TokenType.TemplateInterp))
                    {
                        Advance(); // consume ${
                        template.Parts.Add(ParseExpression());
                        Expect(TokenType.RightBrace, "期望 '}'");
                    }
                    else
                    {
                        // `{Cur}` 会用 `Token.ToString()`，而它自带 `at 行:列` ⇒ 位置混进正文、且不是宿主认的形状。
                // 换成 `{Cur.Type} '{Cur.Value}'`，位置交给统一前缀一处给。
                throw ErrorAt($"模板字符串中意外的标记: {Cur.Type} '{Cur.Value}'", GapAnchor());
                    }
                }
                Expect(TokenType.Backtick, "期望 '`'");
                return template;
            }

            throw ErrorAt($"意外的标记: {Cur.Type} '{Cur.Value}'", GapAnchor());
        }

        // 辅助方法
    }
}