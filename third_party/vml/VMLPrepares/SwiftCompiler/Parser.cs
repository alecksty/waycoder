using System;
using System.Collections.Generic;
using VMLPlugins;
using CompilerBase;

namespace SwiftCompiler
{
    /// <summary>
    /// Swift语法分析器
    /// </summary>
    public class Parser : ParserBase<Token, TokenType>
    {
        private bool hasError;
        private string errorMessage;
        private bool IsMCU => CompilerOptionsContext.Current.IsMCU;

        protected override TokenType GetTokenType(Token token) => token.Type;

        public bool HasError => hasError;
        public string ErrorMessage => errorMessage;

        public Parser(List<Token> tokens) : base(tokens)
        {
            hasError = false;
            errorMessage = "";
        }
        
        public Program Parse()
        {
            try
            {
                var program = new Program();
                
                while (!IsAtEnd)
                {
                    var statement = ParseStatement();
                    if (statement != null)
                        program.Statements.Add(statement);
                    Match(TokenType.Semicolon);
                }
                return program;
            }
            catch (ParseException ex)
            {
                hasError = true;
                errorMessage = ex.Message;
                return new Program();
            }
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
            if (Match(TokenType.Let, TokenType.Var))
            {
                return ParseVariableDecl();
            }
            
            bool isNative = false;
            while (Match(TokenType.Native) || Match(TokenType.External) || Match(TokenType.Static))
            {
                if (Previous().Type == TokenType.Native || Previous().Type == TokenType.External)
                    isNative = true;
            }

            if (Match(TokenType.Func))
            {
                var funcDecl = ParseFunctionDecl();
                funcDecl.IsNative = isNative;
                return funcDecl;
            }
            
            if (Match(TokenType.If))
            {
                return ParseIfStatement();
            }
            
            if (Match(TokenType.While))
            {
                return ParseWhileStatement();
            }
            
            if (Match(TokenType.For))
            {
                return ParseForStatement();
            }
            
            if (Match(TokenType.Return))
            {
                return ParseReturnStatement();
            }
            
            if (Match(TokenType.Print))
            {
                return ParsePrintStatement();
            }

            if (Match(TokenType.Switch))
            {
                return ParseSwitchStatement();
            }

            if (Match(TokenType.Repeat))
            {
                return ParseRepeatWhileStatement();
            }

            if (Match(TokenType.Throw))
            {
                return ParseThrowStatement();
            }

            if (Match(TokenType.Do))
            {
                return ParseDoStatement();
            }

            if (Match(TokenType.Struct))
            {
                return ParseStructDecl();
            }

            if (Match(TokenType.Enum))
            {
                return ParseEnumDecl();
            }

            if (Match(TokenType.Protocol))
            {
                return ParseProtocolDecl();
            }

            if (Match(TokenType.Extension))
            {
                return ParseExtensionDecl();
            }

            // MCU模式: 跳过 async/await/actor 关键字
            if (Check(TokenType.Async) || Check(TokenType.Await) || Check(TokenType.Actor))
            {
                if (IsMCU)
                {
                    WarningEmitter.Emit("swift", $"MCU模式: 异步/并发关键字被忽略（不支持异步/并发）");
                }
                // OS mode: allow async/await through (emit runtime stubs later)
                if (Check(TokenType.Async) || Check(TokenType.Await) || Check(TokenType.Actor))
                { Advance(); }
                return ParseStatement(); // continue parsing after keyword
            }

            if (Match(TokenType.Guard))
            {
                var condition = ParseExpression();
                if (Match(TokenType.Identifier) && Previous().Value == "else")
                {
                    var elseBody = ParseBlock();
                    return new GuardStatement { Condition = condition, Body = elseBody };
                }
                // guard without else = just evaluate condition
                return new GuardStatement { Condition = condition, Body = new Block() };
            }

            if (Match(TokenType.Defer))
            {
                var body = ParseBlock();
                return new DeferStatement { Body = body };
            }

            if (Match(TokenType.Break))
            {
                Match(TokenType.Semicolon);
                return new BreakStatement();
            }

            if (Match(TokenType.Continue))
            {
                Match(TokenType.Semicolon);
                return new ContinueStatement();
            }
            
            // 表达式语句
            var expr = ParseExpression();
            if (expr != null)
            {
                if (Match(TokenType.Semicolon))
                {
                    // 有分号
                }
                return new ExpressionStatement(expr);
            }
            
            // 空语句
            if (Match(TokenType.Semicolon))
            {
                return null;
            }
            
            // 错误恢复
            Advance();
            return null;
        }
        
        private VariableDeclStatement ParseVariableDecl()
        {
            var keyword = Previous();
            var name = Expect(TokenType.Identifier, "期望变量名").Value;

            string typeAnnotation = null;
            if (Match(TokenType.Colon))
            {
                // 接受标识符和类型关键字 (Int, Float, Double, Bool, String, Character)
                if (GetTokenType(Cur) == TokenType.Identifier || GetTokenType(Cur) == TokenType.Int ||
                    GetTokenType(Cur) == TokenType.Float || GetTokenType(Cur) == TokenType.Double ||
                    GetTokenType(Cur) == TokenType.Bool || GetTokenType(Cur) == TokenType.String ||
                    GetTokenType(Cur) == TokenType.Character)
                    typeAnnotation = Advance().Value;
                else
                    throw new ParseException("期望类型注解");
            }
            
            Expression initializer = null;
            if (Match(TokenType.Assignment))
            {
                initializer = ParseExpression();
            }
            
            // 可选分号
            Match(TokenType.Semicolon);
            
            return new VariableDeclStatement(keyword.Value, name, typeAnnotation, initializer);
        }
        
        private FunctionDeclStatement ParseFunctionDecl()
        {
            // Func token already consumed by Match() in ParseStatement()
            var name = Expect(TokenType.Identifier, "期望函数名").Value;

            // 跳过泛型参数 <T, U>
            if (Match(TokenType.LessThan))
            {
                int depth = 1;
                while (depth > 0 && !IsAtEnd)
                {
                    if (Match(TokenType.LessThan)) depth++;
                    else if (Match(TokenType.GreaterThan)) depth--;
                    else Advance();
                }
            }
            
            Expect(TokenType.LeftParen, "期望 '('");
            
            var parameters = new List<Parameter>();
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    var externalName = Cur.Value;
                    Expect(TokenType.Identifier, "期望参数名");
                    
                    string internalName = externalName;
                    // _ internalName: Type 模式
                    if (externalName == "_")
                    {
                        internalName = Expect(TokenType.Identifier, "期望内部参数名").Value;
                        Expect(TokenType.Colon, "期望 ':'");
                    }
                    else if (Match(TokenType.Colon))
                    {
                        // 检查是否是 external internal: 模式（冒号后还有标识符+冒号）
                        if (_pos + 1 < _tokens.Count && _tokens[_pos].Type == TokenType.Identifier && _tokens[_pos + 1].Type == TokenType.Colon)
                        {
                            internalName = Expect(TokenType.Identifier, "期望内部参数名").Value;
                            Expect(TokenType.Colon, "期望 ':'");
                        }
                        // 否则是 name: Type 模式，internalName = externalName
                    }
                    else
                    {
                        Expect(TokenType.Colon, "期望 ':'");
                    }
                    
                    string type;
                    if (GetTokenType(Cur) == TokenType.Identifier || GetTokenType(Cur) == TokenType.Int || GetTokenType(Cur) == TokenType.Double ||
                        GetTokenType(Cur) == TokenType.Float || GetTokenType(Cur) == TokenType.Bool || GetTokenType(Cur) == TokenType.String ||
                        GetTokenType(Cur) == TokenType.Character)
                    {
                        type = Advance().Value;
                    }
                    else
                    {
                        throw new ParseException("期望参数类型");
                    }
                    
                    parameters.Add(new Parameter(externalName, internalName, type));
                } while (Match(TokenType.Comma));
            }
            
            Expect(TokenType.RightParen, "期望 ')'");
            
            string returnType = null;
            if (Match(TokenType.Arrow))
            {
                // Accept Int, Double, etc. as type identifiers (not just TokenType.Identifier)
                if (GetTokenType(Cur) == TokenType.Int || GetTokenType(Cur) == TokenType.Double || 
                    GetTokenType(Cur) == TokenType.Float || GetTokenType(Cur) == TokenType.Bool || 
                    GetTokenType(Cur) == TokenType.String || GetTokenType(Cur) == TokenType.Character)
                    returnType = Advance().Value;
                else
                    returnType = Expect(TokenType.Identifier, "期望返回类型").Value;
            }
            
            var body = ParseBlock();
            
            return new FunctionDeclStatement(name, parameters, returnType, body);
        }
        
        private Statement ParseIfStatement()
        {
            // if let x = optionalExpr { body }
            if (Match(TokenType.Let))
            {
                string vn = Expect(TokenType.Identifier, "期望变量名").Value;
                Expect(TokenType.Assignment, "期望 '='");
                var oe = ParseExpression();
                Statement tb = Check(TokenType.LeftBrace) ? ParseBlock() : ParseStatement();
                Statement eb = null;
                if (Match(TokenType.Else)) eb = Check(TokenType.LeftBrace) ? ParseBlock() : ParseStatement();
                return new IfLetStatement { VariableName = vn, OptionalExpr = oe, ThenBranch = tb, ElseBranch = eb };
            }

            var cond = ParseExpression();
            Statement tb2 = Check(TokenType.LeftBrace) ? ParseBlock() : ParseStatement();
            Statement eb2 = null;
            if (Match(TokenType.Else)) eb2 = Check(TokenType.LeftBrace) ? ParseBlock() : ParseStatement();
            return new IfStatement(cond, tb2, eb2);
        }
        
        private WhileStatement ParseWhileStatement()
        {
            var condition = ParseExpression();
            Statement body;
            if (Check(TokenType.LeftBrace))
                body = ParseBlock();
            else
                body = ParseStatement();
            return new WhileStatement(condition, body);
        }
        
        private Statement ParseForStatement()
        {
            // For token already consumed by Match() in ParseStatement()
            int nextPos = _pos + 1;

            // Swift for-in: for x in collection { body }
            if (Check(TokenType.Identifier) && nextPos < _tokens.Count &&
                _tokens[nextPos].Type == TokenType.In)
            {
                string varName = Expect(TokenType.Identifier, "期望变量名").Value;
                Expect(TokenType.In, "期望 'in'");
                var collection = ParseExpression();
                var body = Check(TokenType.LeftBrace) ? ParseBlock() : ParseStatement();
                return new ForEachStatement(varName, collection, body);
            }

            Statement initializer = null;
            if (!Match(TokenType.Semicolon))
            {
                initializer = ParseVariableDecl();
                if (!Check(TokenType.Semicolon))
                    initializer = new ExpressionStatement(ParseExpression());
            }
            Expect(TokenType.Semicolon, "期望 ';'");
            Expression condition = null;
            if (!Check(TokenType.Semicolon)) condition = ParseExpression();
            Expect(TokenType.Semicolon, "期望 ';'");
            Expression increment = null;
            if (!Check(TokenType.RightParen)) increment = ParseExpression();
            var forBody = ParseStatement();
            return new ForStatement(initializer, condition, increment, forBody);
        }
        
        private ReturnStatement ParseReturnStatement()
        {
            // return token already consumed by ParseStatement()
            
            Expression value = null;
            if (!Check(TokenType.Semicolon) && !Check(TokenType.RightBrace))
            {
                value = ParseExpression();
            }
            
            Match(TokenType.Semicolon);
            
            return new ReturnStatement(value);
        }
        
        private PrintStatement ParsePrintStatement()
        {
            Expect(TokenType.Print, "期望 'print'");
            Expect(TokenType.LeftParen, "期望 '('");
            
            var arguments = new List<Expression>();
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    arguments.Add(ParseExpression());
                } while (Match(TokenType.Comma));
            }
            
            Expect(TokenType.RightParen, "期望 ')'");
            Match(TokenType.Semicolon);
            
            return new PrintStatement(arguments);
        }
        
        private SwitchStatement ParseSwitchStatement()
        {
            Expect(TokenType.LeftParen, "期望 '('");
            var value = ParseExpression();
            Expect(TokenType.RightParen, "期望 ')'");
            Expect(TokenType.LeftBrace, "期望 '{'");
            var sw = new SwitchStatement(value);
            while (!Check(TokenType.RightBrace) && !IsAtEnd)
            {
                if (Match(TokenType.Case))
                {
                    var caseVal = ParseExpression();
                    Expect(TokenType.Colon, "期望 ':'");
                    var sc = new SwitchCase { Value = caseVal };
                    while (!Check(TokenType.RightBrace) && !Check(TokenType.Case) && !Check(TokenType.Default) && !IsAtEnd)
                    {
                        var stmt = ParseStatement();
                        if (stmt == null) break;
                        sc.Body.Add(stmt);
                        if (stmt is BreakStatement) break;
                    }
                    sw.Cases.Add(sc);
                }
                else if (Match(TokenType.Default))
                {
                    Expect(TokenType.Colon, "期望 ':'");
                    var sc = new SwitchCase { Value = null };
                    while (!Check(TokenType.RightBrace) && !Check(TokenType.Case) && !Check(TokenType.Default) && !IsAtEnd)
                    {
                        var stmt = ParseStatement();
                        if (stmt == null) break;
                        sc.Body.Add(stmt);
                        if (stmt is BreakStatement) break;
                    }
                    sw.Cases.Add(sc);
                }
                else break;
            }
            Expect(TokenType.RightBrace, "期望 '}'");
            return sw;
        }

        private Statement ParseRepeatWhileStatement()
        {
            var body = ParseBlock();
            Expect(TokenType.While, "期望 'while'");
            var condition = ParseExpression();
            return new DoWhileStatement(body, condition);
        }

        private Statement ParseThrowStatement()
        {
            Expression? value = null;
            if (!Check(TokenType.Semicolon) && !Check(TokenType.RightBrace))
            {
                value = ParseExpression();
            }
            Match(TokenType.Semicolon);
            return new ThrowStatement(value);
        }

        private Statement ParseDoStatement()
        {
            var body = ParseBlock();
            var ds = new DoStatement(body);
            while (Match(TokenType.Catch))
            {
                var cc = new CatchClause();
                if (Check(TokenType.Identifier))
                {
                    cc.Pattern = Advance().Value;
                }
                cc.Body = ParseBlock();
                ds.Catches.Add(cc);
            }
            return ds;
        }

        private Statement ParseStructDecl()
        {
            string name = Expect(TokenType.Identifier, "期望结构体名").Value;
            var sd = new StructDeclStatement { Name = name };
            // Parse protocol conformance: struct Name: Protocol1, Protocol2 { ... }
            if (Match(TokenType.Colon))
            {
                do {
                    string protoName = Expect(TokenType.Identifier, "期望协议名").Value;
                    sd.Protocols.Add(protoName);
                } while (Match(TokenType.Comma));
            }
            Expect(TokenType.LeftBrace, "期望 '{'");
            while (!Check(TokenType.RightBrace) && !IsAtEnd)
            {
                string fieldName = Expect(TokenType.Identifier, "期望字段名").Value;
                Expect(TokenType.Colon, "期望 ':'");
                string fieldType = Expect(TokenType.Identifier, "期望类型").Value;
                sd.Fields.Add(new StructField { Name = fieldName, Type = fieldType });
                Match(TokenType.Comma);
            }
            Expect(TokenType.RightBrace, "期望 '}'");
            return sd;
        }

        private Statement ParseEnumDecl()
        {
            string name = Expect(TokenType.Identifier, "期望枚举名").Value;
            Expect(TokenType.LeftBrace, "期望 '{'");
            var ed = new EnumDeclStatement { Name = name };
            while (!Check(TokenType.RightBrace) && !IsAtEnd)
            {
                if (Match(TokenType.Identifier))
                {
                    ed.Members.Add(Previous().Value);
                    Match(TokenType.Comma);
                }
                else break;
            }
            Expect(TokenType.RightBrace, "期望 '}'");
            return ed;
        }

        private Statement ParseProtocolDecl()
        {
            string name = Expect(TokenType.Identifier, "期望协议名").Value;
            var pd = new ProtocolDeclStatement { Name = name };
            Expect(TokenType.LeftBrace, "期望 '{'");
            while (!Check(TokenType.RightBrace) && !IsAtEnd)
            {
                if (Match(TokenType.Func))
                {
                    string methodName = Expect(TokenType.Identifier, "期望方法名").Value;
                    Expect(TokenType.LeftParen, "期望 '('");
                    var pm = new ProtocolMethod { Name = methodName };
                    if (!Check(TokenType.RightParen))
                    {
                        do { pm.Parameters.Add(Expect(TokenType.Identifier, "期望参数名").Value); } while (Match(TokenType.Comma));
                    }
                    Expect(TokenType.RightParen, "期望 ')'");
                    if (Match(TokenType.Arrow)) pm.ReturnType = Expect(TokenType.Identifier, "期望类型").Value;
                    pd.Methods.Add(pm);
                }
                else break;
            }
            Expect(TokenType.RightBrace, "期望 '}'");
            return pd;
        }

        private Statement ParseExtensionDecl()
        {
            string typeName = Expect(TokenType.Identifier, "期望类型名").Value;
            var ed = new ExtensionDeclStatement { TypeName = typeName };
            Expect(TokenType.LeftBrace, "期望 '{'");
            while (!Check(TokenType.RightBrace) && !IsAtEnd)
            {
                if (Match(TokenType.Func))
                {
                    string methodName = Expect(TokenType.Identifier, "期望方法名").Value;
                    Expect(TokenType.LeftParen, "期望 '('");
                    var fd = new FunctionDeclStatement(methodName);
                    if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                            string pname = Expect(TokenType.Identifier, "期望参数名").Value;
                            fd.Parameters.Add(new Parameter(pname, pname, "Any"));
                        } while (Match(TokenType.Comma));
                    }
                    Expect(TokenType.RightParen, "期望 ')'");
                    if (Match(TokenType.Arrow)) fd.ReturnType = Expect(TokenType.Identifier, "期望类型").Value;
                    Match(TokenType.LeftBrace);
                    while (!Check(TokenType.RightBrace) && !IsAtEnd) { var s = ParseStatement(); if (s != null) fd.Body.Statements.Add(s); }
                    Expect(TokenType.RightBrace, "期望 '}'");
                    ed.Methods.Add(fd);
                }
                else break;
            }
            Expect(TokenType.RightBrace, "期望 '}'");
            return ed;
        }

        private Block ParseBlock()
        {
            Expect(TokenType.LeftBrace, "期望 '{'");
            
            var block = new Block();
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd)
            {
                var statement = ParseStatement();
                if (statement != null)
                {
                    block.Statements.Add(statement);
                }
                Match(TokenType.Semicolon);
            }
            
            Expect(TokenType.RightBrace, "期望 '}'");
            
            return block;
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
                Expect(TokenType.Colon, "期望 ':'");
                var falseVal = ParseConditional();
                return new ConditionalExpression(expr, trueVal, falseVal);
            }
            return expr;
        }
        
        private Expression ParseAssignment()
        {
            var expr = ParseLogicalOr();
            
            if (Match(TokenType.Assignment, TokenType.PlusEqual, TokenType.MinusEqual, 
                     TokenType.MultiplyEqual, TokenType.DivideEqual, TokenType.ModuloEqual))
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
            
            while (Match(TokenType.Equal, TokenType.NotEqual))
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
            
            while (Match(TokenType.LessThan, TokenType.GreaterThan, 
                        TokenType.LessThanOrEqual, TokenType.GreaterThanOrEqual))
            {
                var op = Previous().Type;
                var right = ParseShift();
                expr = new BinaryExpression(expr, op, right);
            }
            
            return expr;
        }

        private Expression ParseRange()
        {
            var expr = ParseAddition();
            
            while (Match(TokenType.Range, TokenType.HalfOpenRange))
            {
                var op = Previous().Type;
                var right = ParseAddition();
                expr = new BinaryExpression(expr, op, right);
            }
            
            return expr;
        }

        private Expression ParseShift()
        {
            var expr = ParseRange();
            
            while (Match(TokenType.LeftShift, TokenType.RightShift))
            {
                var op = Previous().Type;
                var right = ParseRange();
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
            var expr = ParseUnary();
            
            while (Match(TokenType.Multiply, TokenType.Divide, TokenType.Modulo))
            {
                var op = Previous().Type;
                var right = ParseUnary();
                expr = new BinaryExpression(expr, op, right);
            }
            
            return expr;
        }
        
        private Expression ParseUnary()
        {
            if (Match(TokenType.Plus, TokenType.Minus, TokenType.LogicalNot, TokenType.BitwiseNot))
            {
                var op = Previous().Type;
                var right = ParseUnary();
                return new UnaryExpression(op, right);
            }
            
            // 可选类型操作符：? 和 !
            if (Match(TokenType.Question, TokenType.ForceUnwrap))
            {
                var op = Previous().Type;
                var expr = ParseUnary();
                return new OptionalExpression(expr, op);
            }
            
            var result = ParsePrimary();
            // 后置操作: 索引访问 arr[i], 成员访问 obj.member
            while (true)
            {
                if (Match(TokenType.LeftBracket))
                {
                    var index = ParseExpression();
                    Expect(TokenType.RightBracket, "期望 ']'");
                    result = new IndexAccessExpression(result, index);
                }
                else if (Match(TokenType.Dot))
                {
                    string member = Expect(TokenType.Identifier, "期望成员名").Value;
                    if (Match(TokenType.LeftParen))
                    {
                        var call = new CallExpression(new MemberExpression(result, member));
                        if (!Check(TokenType.RightParen))
                        {
                            do { call.Arguments.Add(ParseExpression()); } while (Match(TokenType.Comma));
                        }
                        Expect(TokenType.RightParen, "期望 ')'");
                        result = call;
                    }
                    else
                    {
                        result = new MemberExpression(result, member);
                    }
                }
                else break;
            }
            return result;
        }
        
        /// <summary>
        /// **原子表达式的唯一入口** —— 顺手盖上它自己的行列（`ASTNode.Line`/`Column`）。
        ///
        /// 与语句入口同一套路，但**粒度细一层**：语句级的列只能给到「这一句从哪开始」，
        /// 而用户报错时要看到的是**出错的那个标识符**从哪开始。原子（标识符/字面量/调用/括号）
        /// 是位置信息真正有意义的地方，而全部语句的表达式都是从这里递归产出的
        /// ⇒ 在这一处包一层就覆盖了整棵树。
        /// </summary>
        private Expression ParsePrimary()
        {
            var __start = Cur;
            int __line = __start.Line, __col = __start.Column;
            var __node = ParsePrimaryCore();
            if (__node != null && __node.Line == 0) { __node.Line = __line; __node.Column = __col; }
            return __node;
        }

        private Expression ParsePrimaryCore()
        {
            if (Match(TokenType.IntegerLiteral))
            {
                long longVal = long.Parse(Previous().Value);
                // v1.66.66: 保留超出32位范围的整数为 long，避免截断高32位
                if (longVal >= int.MinValue && longVal <= int.MaxValue)
                {
                    return new LiteralExpression((int)longVal, "Int");
                }
                else
                {
                    return new LiteralExpression(longVal, "Int64");
                }
            }
            
            if (Match(TokenType.FloatLiteral))
            {
                var value = double.Parse(Previous().Value);
                return new LiteralExpression(value, "Double");
            }
            
            if (Match(TokenType.StringLiteral))
            {
                var value = Previous().Value;
                return new LiteralExpression(value, "String");
            }
            
            // 字符串插值
            if (Match(TokenType.StringInterpolationStart))
            {
                return ParseStringInterpolation();
            }
            
            if (Match(TokenType.True))
            {
                return new LiteralExpression(true, "Bool");
            }
            
            if (Match(TokenType.False))
            {
                return new LiteralExpression(false, "Bool");
            }
            
            if (Match(TokenType.Nil))
            {
                return new LiteralExpression(null, "Optional");
            }

            // 闭包表达式: { (params) -> RetType in body } or { in body } or { body }
            if (Match(TokenType.LeftBrace))
            {
                var closure = new ClosureExpression();
                if (Match(TokenType.LeftParen))
                {
                    if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                            string pname = Expect(TokenType.Identifier, "期望参数名").Value;
                            if (Match(TokenType.Colon)) { var _ = Expect(TokenType.Identifier, "期望参数类型"); }
                            closure.Parameters.Add(pname);
                        } while (Match(TokenType.Comma));
                    }
                    Expect(TokenType.RightParen, "期望 ')'");
                    if (Match(TokenType.Arrow)) closure.ReturnType = Expect(TokenType.Identifier, "期望返回类型").Value;
                    Expect(TokenType.In, "期望 'in'");
                }
                else if (Check(TokenType.Identifier))
                {
                    int lookahead = _pos + 1;
                    if (lookahead < _tokens.Count && _tokens[lookahead].Type == TokenType.In)
                    {
                        closure.Parameters.Add(Advance().Value);
                        Advance(); // skip in
                    }
                }
                // 解析闭包体
                var body = new Block();
                while (!Check(TokenType.RightBrace) && !IsAtEnd)
                {
                    var stmt = ParseStatement();
                    if (stmt != null) body.Statements.Add(stmt);
                }
                Expect(TokenType.RightBrace, "期望 '}'");
                closure.Body = body;
                return closure;
            }
            
            if (Match(TokenType.Identifier) || Match(TokenType.Int) || Match(TokenType.Float) ||
                Match(TokenType.Double) || Match(TokenType.Bool) || Match(TokenType.String) ||
                Match(TokenType.Character))
            {
                var name = Previous().Value;
                
                // 函数调用
                if (Match(TokenType.LeftParen))
                {
                    var call = new CallExpression(new VariableExpression(name));
                    if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                            call.Arguments.Add(ParseExpression());
                        } while (Match(TokenType.Comma));
                    }
                    Expect(TokenType.RightParen, "期望 ')'");
                    return call;
                }
                
                // 变量访问
                return new VariableExpression(name);
            }
            
            if (Match(TokenType.LeftParen))
            {
                var expr = ParseExpression();
                Expect(TokenType.RightParen, "期望 ')'");
                return new ParenthesizedExpression(expr);
            }
            
            // 数组字面量
            if (Match(TokenType.LeftBracket))
            {
                return ParseArrayLiteral();
            }
            
            // 错误恢复
            Advance();
            throw new ParseException($"期望表达式，但找到 {GetTokenType(Cur)}");
        }
        
        private Expression ParseStringInterpolation()
        {
            // 字符串插值表达式：\(expression)
            var parts = new List<Expression>();
            var expr = ParseExpression();
            parts.Add(expr);
            Expect(TokenType.RightParen, "期望 ')'");
            return new StringInterpolationExpression(parts);
        }
        
        private Expression ParseArrayLiteral()
        {
            // 检查是否是字典字面量: [key: value, ...]
            if (!Check(TokenType.RightBracket))
            {
                int savePos = _pos;
                var firstKey = ParseExpression();
                if (Match(TokenType.Colon))
                {
                    // 是字典字面量
                    var dict = new DictionaryLiteralExpression();
                    dict.Entries.Add(new KeyValuePair<Expression, Expression>(firstKey, ParseExpression()));
                    while (Match(TokenType.Comma))
                    {
                        var k = ParseExpression();
                        Expect(TokenType.Colon, "期望 ':'");
                        dict.Entries.Add(new KeyValuePair<Expression, Expression>(k, ParseExpression()));
                    }
                    Expect(TokenType.RightBracket, "期望 ']'");
                    return dict;
                }
                _pos = savePos;
            }

            // 数组字面量：[element1, element2, ...]
            var elements = new List<Expression>();
            if (!Check(TokenType.RightBracket))
            {
                do { elements.Add(ParseExpression()); } while (Match(TokenType.Comma));
            }
            Expect(TokenType.RightBracket, "期望 ']'");
            return new ArrayLiteralExpression(elements);
        }
        
    }
}
