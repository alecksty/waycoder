using CompilerBase;
using System.Collections.Generic;
using VMLPlugins;

namespace GoCompiler
{
    public partial class Parser : ParserBase<Token, TokenType>
    {
        public Program ParseProgram()
        {
            var program = new Program();

            SkipNewlines();

            while (GetTokenType(Cur) != TokenType.EOF)
            {
                SkipNewlines();
                if (GetTokenType(Cur) == TokenType.EOF)
                    break;

                try
                {
                    var decl = ParseDeclaration();
                    if (decl != null)
                    {
                        program.Declarations.Add(decl);
                    }
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine($"解析错误: {ex.Message}");
                    // 尝试恢复
                    while (GetTokenType(Cur) != TokenType.EOF && GetTokenType(Cur) != TokenType.PACKAGE && GetTokenType(Cur) != TokenType.FUNC && GetTokenType(Cur) != TokenType.VAR && GetTokenType(Cur) != TokenType.CONST && GetTokenType(Cur) != TokenType.TYPE && GetTokenType(Cur) != TokenType.IMPORT)
                    {
                        Advance();
                    }
                }
            }

            // 分离函数和变量
            foreach (var decl in program.Declarations)
            {
                if (decl is Function func)
                {
                    program.Functions.Add(func);
                    if (func.Name == "main")
                    {
                        func.IsMain = true;
                        program.PackageName = "main";
                    }
                }
                else if (decl is VariableDecl varDecl)
                {
                    program.Variables.Add(varDecl);
                }
                else if (decl is ConstantDecl constDecl)
                {
                    program.Constants.Add(constDecl);
                }
                else if (decl is TypeDecl typeDecl)
                {
                    program.Types.Add(typeDecl);
                }
                else if (decl is Import import)
                {
                    program.Imports.Add(import);
                }
            }

            return program;
        }

        /// <summary>
        /// 解析声明
        /// </summary>
        private ASTNode ParseDeclaration()
        {
            SkipNewlines();

            switch (GetTokenType(Cur))
            {
                case TokenType.PACKAGE:
                    return ParsePackageClause();
                case TokenType.IMPORT:
                    return ParseImportDecl();
                case TokenType.FUNC:
                    return ParseFunctionDecl();
                case TokenType.VAR:
                    return ParseVarDecl();
                case TokenType.CONST:
                    return ParseConstDecl();
                case TokenType.TYPE:
                    return ParseTypeDecl();
                default:
                    return ParseStatement();
            }
        }

        /// <summary>
        /// 解析 package 子句
        /// </summary>
        private ASTNode ParsePackageClause()
        {
            Expect(TokenType.PACKAGE);
            var name = Expect(TokenType.IDENTIFIER);
            SkipNewlines();
            return new Program { PackageName = (string)name.Value };
        }

        /// <summary>
        /// 解析导入声明
        /// </summary>
        private Import ParseImportDecl()
        {
            Expect(TokenType.IMPORT);

            string path = "";
            string alias = null;

            if (GetTokenType(Cur) == TokenType.LPAREN)
            {
                Advance();
                SkipNewlines();
                // 多重导入
                while (GetTokenType(Cur) != TokenType.RPAREN && GetTokenType(Cur) != TokenType.EOF)
                {
                    SkipNewlines();
                    if (GetTokenType(Cur) == TokenType.RPAREN)
                        break;

                    (path, alias) = ParseImportSpec();
                    SkipNewlines();
                }
                Expect(TokenType.RPAREN);
            }
            else
            {
                (path, alias) = ParseImportSpec();
            }

            SkipNewlines();
            return new Import(path, alias);
        }

        private (string path, string alias) ParseImportSpec()
        {
            string alias = null;
            string path;

            if (GetTokenType(Cur) == TokenType.IDENTIFIER)
            {
                alias = ((Token)Advance()).Value as string;
            }

            // 检查当前 token（不是 Peek()，Peek 默认看下一个）
            Token token;
            if (GetTokenType(Cur) == TokenType.INTERPRETED_STRING || GetTokenType(Cur) == TokenType.RAW_STRING)
            {
                token = Advance();
                path = (token.Value as string).Trim('"', '`');
            }
            else
            {
                token = Expect(TokenType.INTERPRETED_STRING);
                path = (token.Value as string).Trim('"', '`');
            }

            SkipNewlines();
            return (path, alias);
        }

        /// <summary>
        /// 解析函数声明
        /// </summary>
        private Function ParseFunctionDecl()
        {
            Expect(TokenType.FUNC);

            // 检查是否有接收者（方法声明）
            Function func;
            if (GetTokenType(Cur) == TokenType.LPAREN)
            {
                // 方法: func (receiver) methodName(...)
                Expect(TokenType.LPAREN);
                var recvName = Expect(TokenType.IDENTIFIER).Value as string;
                var recvType = ParseType();
                Expect(TokenType.RPAREN);
                string methodName = Expect(TokenType.IDENTIFIER).Value as string;
                func = new Function(methodName);
                // 接收者作为第一个参数
                func.Parameters = new List<Parameter> { new Parameter(new List<string> { recvName }, recvType) };
            }
            else
            {
                string name = Expect(TokenType.IDENTIFIER).Value as string;
                func = new Function(name);
            }

            // 解析参数（方法已有接收者作为第一个参数）
            Expect(TokenType.LPAREN);
            if (GetTokenType(Cur) != TokenType.RPAREN)
            {
                var extraParams = ParseParamList();
                if (func.Parameters != null)
                    func.Parameters.AddRange(extraParams);
                else
                    func.Parameters = extraParams;
            }
            Expect(TokenType.RPAREN);

            // 解析结果
            if (GetTokenType(Cur) == TokenType.LPAREN || IsTypeStart(GetTokenType(Cur)))
            {
                func.Results = ParseResultList();
            }
            else if (GetTokenType(Cur) == TokenType.LBRACE)
            {
                // 无返回值
                func.Results = new List<Parameter>();
            }
            else
            {
                // 没有明确返回值类型，可能是隐式
                func.Results = new List<Parameter>();
            }

            SkipNewlines();

            // 解析函数体
            if (GetTokenType(Cur) == TokenType.LBRACE)
            {
                func.Body = ParseBlock();
            }
            else
            {
                SkipNewlines();
                if (GetTokenType(Cur) == TokenType.LBRACE)
                {
                    func.Body = ParseBlock();
                }
            }

            return func;
        }

        /// <summary>
        /// 解析参数列表（LPAREN已经在调用前被消费，RPAREN由调用者处理）
        /// </summary>
        private List<Parameter> ParseParamList()
        {
            var parameters = new List<Parameter>();
            
            if (GetTokenType(Cur) != TokenType.RPAREN)
            {
                parameters.Add(ParseParam());
                while (Match(TokenType.COMMA))
                {
                    parameters.Add(ParseParam());
                }
            }
            
            // 注意：不消费RPAREN，由调用者处理
            return parameters;
        }

        /// <summary>
        /// 解析返回值列表
        /// </summary>
        private List<Parameter> ParseResultList()
        {
            var results = new List<Parameter>();
            
            if (GetTokenType(Cur) == TokenType.LPAREN)
            {
                // 多个返回值，带括号
                Expect(TokenType.LPAREN);
                
                if (GetTokenType(Cur) != TokenType.RPAREN)
                {
                    // 解析第一个返回值
                    if (GetTokenType(Cur) == TokenType.IDENTIFIER)
                    {
                        // 可能是命名返回值: (a int, b int)
                        results.Add(ParseParam());
                    }
                    else
                    {
                        // 匿名返回值: (int, error)
                        var type = ParseType();
                        results.Add(new Parameter(new List<string>(), type));
                    }
                    
                    // 解析更多返回值
                    while (Match(TokenType.COMMA))
                    {
                        if (GetTokenType(Cur) == TokenType.IDENTIFIER)
                        {
                            results.Add(ParseParam());
                        }
                        else
                        {
                            var type = ParseType();
                            results.Add(new Parameter(new List<string>(), type));
                        }
                    }
                }
                
                Expect(TokenType.RPAREN);
            }
            else
            {
                // 单个返回值，无括号
                var type = ParseType();
                results.Add(new Parameter(new List<string>(), type));
            }
            
            return results;
        }

        /// <summary>
        /// 解析单个参数
        /// </summary>
        private Parameter ParseParam()
        {
            var names = new List<string>();

            // 收集所有名字 (Go支持: a, b int)
            if (GetTokenType(Cur) == TokenType.IDENTIFIER)
            {
                names.Add(Expect(TokenType.IDENTIFIER).Value as string);
                while (GetTokenType(Cur) == TokenType.COMMA)
                {
                    Advance();
                    if (GetTokenType(Cur) == TokenType.IDENTIFIER)
                        names.Add(Expect(TokenType.IDENTIFIER).Value as string);
                    else
                        break;
                }
            }

            if (GetTokenType(Cur) == TokenType.ELLIPSIS)
            {
                Advance();
                var elemType = ParseType();
                var sliceType = new GoType("[]") { ElementType = elemType };
                return new Parameter(names, sliceType);
            }

            var type = ParseType();
            return new Parameter(names, type);
        }

        /// <summary>
        /// 解析类型
        /// </summary>
        private GoType ParseType()
        {
            // 处理基本类型关键字
            switch (GetTokenType(Cur))
            {
                case TokenType.INT: Advance(); return new GoType("int");
                case TokenType.INT8: Advance(); return new GoType("int8");
                case TokenType.INT16: Advance(); return new GoType("int16");
                case TokenType.INT32: Advance(); return new GoType("int32");
                case TokenType.INT64: Advance(); return new GoType("int64");
                case TokenType.UINT: Advance(); return new GoType("uint");
                case TokenType.UINT8: Advance(); return new GoType("uint8");
                case TokenType.UINT16: Advance(); return new GoType("uint16");
                case TokenType.UINT32: Advance(); return new GoType("uint32");
                case TokenType.UINT64: Advance(); return new GoType("uint64");
                case TokenType.FLOAT32: Advance(); return new GoType("float32");
                case TokenType.FLOAT64: Advance(); return new GoType("float64");
                case TokenType.COMPLEX64: Advance(); return new GoType("complex64");
                case TokenType.COMPLEX128: Advance(); return new GoType("complex128");
                case TokenType.BOOL: Advance(); return new GoType("bool");
                case TokenType.STRING: Advance(); return new GoType("string");
                case TokenType.BYTE: Advance(); return new GoType("byte");
                case TokenType.RUNE: Advance(); return new GoType("rune");
                case TokenType.ERROR: Advance(); return new GoType("error");
            }

            if (GetTokenType(Cur) == TokenType.IDENTIFIER)
            {
                var name = Advance().Value as string;
                return new GoType(name);
            }

            switch (GetTokenType(Cur))
            {
                case TokenType.STAR:
                    Advance();
                    var baseType = ParseType();
                    return new GoType("*") { ElementType = baseType };

                case TokenType.LBRACKET:
                    Advance();
                    GoType elementType;
                    int? size = null;

                    if (GetTokenType(Cur) == TokenType.RBRACKET)
                    {
                        // []slice
                        Advance();
                        elementType = ParseType();
                        return GoType.Slice(elementType);
                    }
                    else if (GetTokenType(Cur) == TokenType.NUMBER)
                    {
                        // [n]array
                        size = int.Parse(Advance().Value as string);
                        Expect(TokenType.RBRACKET);
                        elementType = ParseType();
                        return GoType.Array(elementType, size.Value);
                    }
                    else
                    {
                        // []slice
                        elementType = ParseType();
                        Expect(TokenType.RBRACKET);
                        return GoType.Slice(elementType);
                    }

                case TokenType.MAP:
                    Advance();
                    Expect(TokenType.LBRACKET);
                    var keyType = ParseType();
                    Expect(TokenType.RBRACKET);
                    var valueType = ParseType();
                    return GoType.Map(keyType, valueType);

                case TokenType.CHAN:
                    if (_isMCU) { WarningEmitter.Emit("go", "MCU模式: chan类型被忽略，替换为int"); Advance(); ParseType(); return GoType.Int; }
                    Advance();
                    var chanElementType = ParseType();
                    return GoType.Chan(chanElementType);

                case TokenType.FUNC:
                    return ParseFuncType();

                case TokenType.STRUCT:
                    return ParseStructType();

                case TokenType.INTERFACE:
                    return ParseInterfaceType();

                default:
                    var typeName = Advance().Value as string;
                    return new GoType(typeName);
            }
        }

        /// <summary>
        /// 解析函数类型
        /// </summary>
        private GoType ParseFuncType()
        {
            Expect(TokenType.FUNC);
            Expect(TokenType.LPAREN);
            // 简化处理
            Expect(TokenType.RPAREN);
            return new GoType("func");
        }

        /// <summary>
        /// 解析结构体类型
        /// </summary>
        private GoType ParseStructType()
        {
            Expect(TokenType.STRUCT);
            Expect(TokenType.LBRACE);
            var fields = new List<Field>();

            SkipNewlines();
            while (GetTokenType(Cur) != TokenType.RBRACE && GetTokenType(Cur) != TokenType.EOF)
            {
                SkipNewlines();
                if (GetTokenType(Cur) == TokenType.RBRACE)
                    break;

                var field = new Field();
                field.Names = new List<string>();
                field.Names.Add(Expect(TokenType.IDENTIFIER).Value as string);
                while (GetTokenType(Cur) == TokenType.COMMA)
                {
                    Advance();
                    field.Names.Add(Expect(TokenType.IDENTIFIER).Value as string);
                }
                field.Type = ParseType();
                fields.Add(field);
                SkipNewlines();
            }

            Expect(TokenType.RBRACE);
            var structType = new GoType("struct");
            structType.Fields = fields;
            return structType;
        }

        /// <summary>
        /// 解析接口类型
        /// </summary>
        private GoType ParseInterfaceType()
        {
            Expect(TokenType.INTERFACE);
            Expect(TokenType.LBRACE);
            var methods = new List<GoType>();
            SkipNewlines();
            while (GetTokenType(Cur) != TokenType.RBRACE && GetTokenType(Cur) != TokenType.EOF)
            {
                if (GetTokenType(Cur) == TokenType.IDENTIFIER)
                {
                    Advance();
                    if (GetTokenType(Cur) == TokenType.LPAREN)
                    {
                        Expect(TokenType.LPAREN);
                        while (GetTokenType(Cur) != TokenType.RPAREN && GetTokenType(Cur) != TokenType.EOF)
                        {
                            ParseParam();
                            Match(TokenType.COMMA);
                        }
                        Expect(TokenType.RPAREN);
                        if (GetTokenType(Cur) == TokenType.LPAREN || IsTypeStart(GetTokenType(Cur)))
                            ParseResultList();
                        methods.Add(new GoType("method"));
                    }
                    else
                    {
                        methods.Add(new GoType("embedded"));
                    }
                }
                else if (IsTypeStart(GetTokenType(Cur)))
                {
                    methods.Add(ParseType());
                }
                else
                {
                    Advance();
                }
                SkipNewlines();
            }
            Expect(TokenType.RBRACE);
            return GoType.Interface(methods);
        }

        /// <summary>
        /// 解析变量声明
        /// </summary>
        private VariableDecl ParseVarDecl()
        {
            Expect(TokenType.VAR);
            var decl = new VariableDecl();

            // 解析变量名
            if (GetTokenType(Cur) == TokenType.LPAREN)
            {
                Advance();
                while (GetTokenType(Cur) != TokenType.RPAREN && GetTokenType(Cur) != TokenType.EOF)
                {
                    SkipNewlines();
                    if (GetTokenType(Cur) == TokenType.RPAREN)
                        break;

                    var name = Expect(TokenType.IDENTIFIER).Value as string;
                    decl.Names.Add(name);
                    SkipNewlines();
                }
                Expect(TokenType.RPAREN);
            }
            else
            {
                decl.Names.Add(Expect(TokenType.IDENTIFIER).Value as string);
            }

            // 解析类型
            if (IsTypeStart(GetTokenType(Cur)))
            {
                decl.Type = ParseType();
            }

            // 解析初始值
            if (Match(TokenType.ASSIGN))
            {
                if (GetTokenType(Cur) == TokenType.LBRACE)
                {
                    decl.Values.Add(ParseCompositeLiteral());
                }
                else
                {
                    decl.Values.Add(ParseExpression());
                    while (Match(TokenType.COMMA))
                    {
                        decl.Values.Add(ParseExpression());
                    }
                }
            }

            SkipNewlines();
            return decl;
        }

        /// <summary>
        /// 解析短变量声明 i := expr
        /// </summary>
        private ShortVarDecl ParseShortVarDecl(string firstName)
        {
            var decl = new ShortVarDecl();
            decl.Names.Add(firstName);
            Expect(TokenType.COLON_ASSIGN);
            decl.Values.Add(ParseExpression());
            while (Match(TokenType.COMMA))
            {
                decl.Names.Add(Expect(TokenType.IDENTIFIER).Value as string);
                Expect(TokenType.COLON_ASSIGN);
                decl.Values.Add(ParseExpression());
            }
            SkipNewlines();
            return decl;
        }

        /// <summary>
        /// 解析 for 循环的 post 语句: i = i + 1, i += 1 等
        /// </summary>
        private ASTNode ParseForPost()
        {
            // 解析标识符 + 赋值
            if (GetTokenType(Cur) == TokenType.IDENTIFIER)
            {
                var name = Expect(TokenType.IDENTIFIER).Value as string;
                if (GetTokenType(Cur) == TokenType.ASSIGN)
                {
                    Advance(); // =
                    var value = ParseExpression();
                    return new Assignment(
                        new List<ASTNode> { new Identifier(name) },
                        new List<ASTNode> { value }
                    );
                }
                if (GetTokenType(Cur) == TokenType.ADD_ASSIGN)
                {
                    Advance();
                    var value = ParseExpression();
                    return new Assignment(
                        new List<ASTNode> { new Identifier(name) },
                        new List<ASTNode> { value }, "+");
                }
                if (GetTokenType(Cur) == TokenType.SUB_ASSIGN)
                {
                    Advance();
                    var value = ParseExpression();
                    return new Assignment(
                        new List<ASTNode> { new Identifier(name) },
                        new List<ASTNode> { value }, "-");
                }
                // i++ / i--
                if (GetTokenType(Cur) == TokenType.INCREMENT)
                {
                    Advance();
                    return new Assignment(
                        new List<ASTNode> { new Identifier(name) },
                        new List<ASTNode> { new NumberLiteral("1") }, "+");
                }
                if (GetTokenType(Cur) == TokenType.DECREMENT)
                {
                    Advance();
                    return new Assignment(
                        new List<ASTNode> { new Identifier(name) },
                        new List<ASTNode> { new NumberLiteral("1") }, "-");
                }
                // 如果不是赋值，作为表达式回退
                return ParseExpressionSuffix(new Identifier(name));
            }

            return ParseExpression();
        }

        /// <summary>
        /// 解析常量声明
        /// </summary>
        private ConstantDecl ParseConstDecl()
        {
            Expect(TokenType.CONST);
            var decl = new ConstantDecl();

            // 解析常量名
            if (GetTokenType(Cur) == TokenType.LPAREN)
            {
                Advance();
                while (GetTokenType(Cur) != TokenType.RPAREN && GetTokenType(Cur) != TokenType.EOF)
                {
                    SkipNewlines();
                    if (GetTokenType(Cur) == TokenType.RPAREN)
                        break;

                    decl.Names.Add(Expect(TokenType.IDENTIFIER).Value as string);
                    SkipNewlines();
                }
                Expect(TokenType.RPAREN);
            }
            else
            {
                decl.Names.Add(Expect(TokenType.IDENTIFIER).Value as string);
            }

            // 解析类型
            if (GetTokenType(Cur) == TokenType.IDENTIFIER)
            {
                decl.Type = ParseType();
            }

            // 解析初始值
            if (Match(TokenType.ASSIGN))
            {
                decl.Values.Add(ParseExpression());
                while (Match(TokenType.COMMA))
                {
                    decl.Values.Add(ParseExpression());
                }
            }

            SkipNewlines();
            return decl;
        }

        /// <summary>
        /// 解析类型声明
        /// </summary>
        private TypeDecl ParseTypeDecl()
        {
            Expect(TokenType.TYPE);
            var name = Expect(TokenType.IDENTIFIER).Value as string;
            SkipNewlines();
            // Go 语法: type Name Type (无 =)
            var type = ParseType();
            SkipNewlines();
            return new TypeDecl(name, type);
        }

        /// <summary>
        /// 解析代码块
        /// </summary>
        private Block ParseBlock()
        {
            Expect(TokenType.LBRACE);
            var block = new Block();
            SkipNewlines();

            int _stmtGuard = 0;
            while (GetTokenType(Cur) != TokenType.RBRACE && GetTokenType(Cur) != TokenType.EOF)
            {
                if (++_stmtGuard > 100000)
                {
                    Console.Error.WriteLine("解析错误: ParseBlock 语句过多，可能存在死循环");
                    break;
                }
                SkipNewlines();
                if (GetTokenType(Cur) == TokenType.RBRACE)
                    break;

                try
                {
                    var stmt = ParseStatement();
                    if (stmt != null)
                    {
                        block.Statements.Add(stmt);
                    }
                    // 防御: 如果语句解析未消费任何 token，强制跳过当前 token
                    else if (stmt == null)
                    {
                        Advance();
                    }
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine($"解析错误: {ex.Message}");
                    break;
                }
            }

            Expect(TokenType.RBRACE);
            return block;
        }

        /// <summary>
        /// 解析语句
        /// </summary>
        private ASTNode ParseStatement()
        {
            SkipNewlines();

            switch (GetTokenType(Cur))
            {
                case TokenType.VAR:
                    return ParseVarDecl();

                case TokenType.CONST:
                    return ParseConstDecl();

                case TokenType.TYPE:
                    return ParseTypeDecl();

                case TokenType.IF:
                    return ParseIfStatement();

                case TokenType.FOR:
                    return ParseForStatement();

                case TokenType.SWITCH:
                    return ParseSwitchStatement();

                case TokenType.SELECT:
                    if (_isMCU) { WarningEmitter.Emit("go", "MCU模式: select被忽略（不支持channel）"); }
                    return ParseSelectStatement();

                case TokenType.RETURN:
                    return ParseReturnStatement();

                case TokenType.BREAK:
                    Advance();
                    return new BreakStatement();

                case TokenType.CONTINUE:
                    Advance();
                    return new ContinueStatement();

                case TokenType.GOTO:
                    Advance();
                    return new GotoStatement(Expect(TokenType.IDENTIFIER).Value as string);

                case TokenType.FALLTHROUGH:
                    Advance();
                    return new FallthroughStatement();

                case TokenType.DEFER:
                    return ParseDeferStatement();

                case TokenType.GO:
                    if (_isMCU) { WarningEmitter.Emit("go", "MCU模式: go(goroutine)被忽略（不支持并发）"); Advance(); return null; }
                    return ParseGoStatement();

                case TokenType.LBRACE:
                    return ParseBlock();

                case TokenType.SEMICOLON:
                case TokenType.NEWLINE:
                    Advance();
                    return new EmptyStatement();

                case TokenType.IDENTIFIER:
                    // 可能是标签、赋值、短变量声明或表达式语句
                    return ParseIdentifierStatement();

                case TokenType.INT:
                case TokenType.INT8:
                case TokenType.INT16:
                case TokenType.INT32:
                case TokenType.INT64:
                case TokenType.UINT:
                case TokenType.UINT8:
                case TokenType.UINT16:
                case TokenType.UINT32:
                case TokenType.UINT64:
                case TokenType.FLOAT32:
                case TokenType.FLOAT64:
                case TokenType.BOOL:
                case TokenType.STRING:
                case TokenType.BYTE:
                case TokenType.RUNE:
                    // 类型转换
                    return ParseTypeConversionStatement();

                default:
                    // 表达式语句
                    int _posBefore = _pos;
                    var expr = ParseExpression();
                    // 防御: 如果 ParseExpression 返回 null 且未消费任何 token, 跳过当前 token
                    if (expr == null && _pos == _posBefore)
                    {
                        Advance(); // 跳过无法识别的 token
                        return new EmptyStatement();
                    }
                    if (expr is AssignmentWrapper wrapper)
                    {
                        Expect(TokenType.ASSIGN);
                        var right = ParseExpression();
                        return new Assignment(new List<ASTNode> { wrapper.Left }, new List<ASTNode> { right });
                    }
                    // 处理 left = right 形式（left 可以是 SelectorExpr 等）
                    if (GetTokenType(Cur) == TokenType.ASSIGN)
                    {
                        Expect(TokenType.ASSIGN);
                        var right = ParseExpression();
                        return new Assignment(new List<ASTNode> { expr }, new List<ASTNode> { right });
                    }
                    // 复合赋值: left += right 等形式
                    if (IsCompoundAssign(GetTokenType(Cur)))
                    {
                        string op = (string)Advance().Value!;
                        var right = ParseExpression();
                        return new Assignment(new List<ASTNode> { expr }, new List<ASTNode> { right }, op.TrimEnd('='));
                    }
                    if (GetTokenType(Cur) == TokenType.ARROW)
                    {
                        Advance();
                        var value = ParseExpression();
                        return new SendStatement(expr, value);
                    }
                    return new ExpressionStatement(expr);
            }
        }

        /// <summary>
        /// 解析标识符开始的语句
        /// </summary>
        private ASTNode ParseIdentifierStatement()
        {
            var name = Expect(TokenType.IDENTIFIER).Value as string;

            // i++ / i-- 自增自减语句
            if (GetTokenType(Cur) == TokenType.INCREMENT)
            {
                Advance();
                SkipNewlines();
                return new IncDecStatement(new Identifier(name), true);
            }
            if (GetTokenType(Cur) == TokenType.DECREMENT)
            {
                Advance();
                SkipNewlines();
                return new IncDecStatement(new Identifier(name), false);
            }

            // 多变量声明: a, b, c := ... 或 a, b, c = ...
            if (GetTokenType(Cur) == TokenType.COMMA)
            {
                var names = new List<string> { name };
                while (Match(TokenType.COMMA))
                {
                    names.Add(Expect(TokenType.IDENTIFIER).Value as string);
                }

                if (GetTokenType(Cur) == TokenType.COLON_ASSIGN)
                {
                    Advance(); // :=
                    var decl = new ShortVarDecl();
                    decl.Names = names;
                    decl.Values.Add(ParseExpression());
                    while (Match(TokenType.COMMA))
                    {
                        decl.Values.Add(ParseExpression());
                    }
                    SkipNewlines();
                    return decl;
                }
                else if (GetTokenType(Cur) == TokenType.ASSIGN)
                {
                    Advance(); // =
                    var values = new List<ASTNode>();
                    values.Add(ParseExpression());
                    while (Match(TokenType.COMMA))
                    {
                        values.Add(ParseExpression());
                    }
                    var lefts = names.ConvertAll(n => (ASTNode)new Identifier(n));
                    SkipNewlines();
                    return new Assignment(lefts, values);
                }
                // fallthrough: expression statement (comma operator not in Go, treat first as expr)
                var fallback = new Identifier(name);
                return new ExpressionStatement(ParseExpressionSuffix(fallback));
            }

            // 标签后跟语句
            if (GetTokenType(Cur) == TokenType.COLON && Peek(1).Type != TokenType.ASSIGN)
            {
                Advance(); // :
                SkipNewlines();
                return new LabeledStatement(name, ParseStatement());
            }

            // 短变量声明
            if (GetTokenType(Cur) == TokenType.COLON_ASSIGN)
            {
                return ParseShortVarDecl(name);
            }

            // 函数调用、赋值或字段赋值
            var primary = new Identifier(name);
            var expr = ParseExpressionSuffix(primary);
            // Channel send: ch <- value
            if (Match(TokenType.ARROW))
            {
                var value = ParseExpression();
                SkipNewlines();
                return new SendStatement(expr, value);
            }
            // 检查是否为 a.X = value 形式的赋值
            if (GetTokenType(Cur) == TokenType.ASSIGN)
            {
                Expect(TokenType.ASSIGN);
                var right = ParseExpression();
                return new Assignment(new List<ASTNode> { expr }, new List<ASTNode> { right });
            }
            return new ExpressionStatement(expr);
        }

        /// <summary>
        /// 解析 if 语句
        /// </summary>
        private IfStatement ParseIfStatement()
        {
            var ifStmt = new IfStatement();
            Expect(TokenType.IF);

            // 初始化语句
            if (GetTokenType(Cur) == TokenType.VAR)
            {
                ifStmt.Init = ParseVarDecl();
            }
            else if (GetTokenType(Cur) == TokenType.IDENTIFIER && Peek(1).Type == TokenType.COLON_ASSIGN)
            {
                var name = Expect(TokenType.IDENTIFIER).Value as string;
                ifStmt.Init = ParseShortVarDecl(name);
            }
            else if (GetTokenType(Cur) != TokenType.LBRACE)
            {
                // 标识符+LBRACE (如 if x{}): 只取标识符, 避免误解析为复合字面量
                if (GetTokenType(Cur) == TokenType.IDENTIFIER && Peek(1).Type == TokenType.LBRACE)
                {
                    ifStmt.Condition = new Identifier(Advance().Value as string);
                }
                else
                {
                    ifStmt.Condition = ParseConditionExpr();
                }
            }

            // 条件
            if (ifStmt.Condition == null && GetTokenType(Cur) != TokenType.LBRACE)
            {
                // 标识符+LBRACE (如 if x{}): 只取标识符
                if (GetTokenType(Cur) == TokenType.IDENTIFIER && Peek(1).Type == TokenType.LBRACE)
                {
                    ifStmt.Condition = new Identifier(Advance().Value as string);
                }
                else
                {
                    ifStmt.Condition = ParseConditionExpr();
                }
            }

            // then 分支
            SkipNewlines();
            ifStmt.ThenBranch = ParseBlock();
            SkipNewlines();

            // else if / else
            while (true)
            {
                SkipNewlines();
                if (GetTokenType(Cur) != TokenType.ELSE) break;
                Advance();
                SkipNewlines();

                if (GetTokenType(Cur) == TokenType.IF)
                {
                    Advance(); // consume IF
                    var elseIfCond = ParseConditionExpr();
                    SkipNewlines();
                    var elseIfBody = ParseBlock();
                    ifStmt.ElseIfBranches.Add((elseIfCond, elseIfBody));
                }
                else
                {
                    ifStmt.ElseBranch = ParseBlock();
                    break;
                }
            }

            return ifStmt;
        }

        /// <summary>
        /// 解析 for 语句
        /// </summary>
        private ForStatement ParseForStatement()
        {
            var forStmt = new ForStatement();
            Expect(TokenType.FOR);
            SkipNewlines();

            if (GetTokenType(Cur) == TokenType.LBRACE)
            {
                // 无限循环
                forStmt.Body = ParseBlock();
                return forStmt;
            }

            // for idx, val := range expr — 带变量的range循环
            // 先做lookahead检查，确认是range后才消费token
            bool isRangeLoop = false;
            string rangeKeyVar = null, rangeValVar = null;
            
            if (GetTokenType(Cur) == TokenType.IDENTIFIER && Peek(1).Type == TokenType.COMMA &&
                Peek(2).Type == TokenType.IDENTIFIER && Peek(3).Type == TokenType.COLON_ASSIGN && Peek(4).Type == TokenType.RANGE)
            {
                // idx, val := range ...
                rangeKeyVar = Cur.Value as string;
                rangeValVar = Peek(2).Value as string;
                isRangeLoop = true;
                Advance(); Advance(); Advance(); Advance(); Advance(); // skip idx, val, :=, range
            }
            else if (GetTokenType(Cur) == TokenType.IDENTIFIER && Peek(1).Type == TokenType.COLON_ASSIGN && Peek(2).Type == TokenType.RANGE)
            {
                // val := range ...
                rangeValVar = Cur.Value as string;
                isRangeLoop = true;
                Advance(); Advance(); Advance(); // skip val, :=, range
            }
            
            if (isRangeLoop)
            {
                forStmt.IsRangeLoop = true;
                forStmt.KeyVar = rangeKeyVar;
                forStmt.ValueVar = rangeValVar;
                forStmt.RangeExpr = ParseExpression();
                SkipNewlines();
                forStmt.Body = ParseBlock();
                return forStmt;
            }

            if (GetTokenType(Cur) == TokenType.RANGE)
            {
                // range 循环 (无变量)
                Advance();
                forStmt.IsRangeLoop = true;
                forStmt.RangeExpr = ParseExpression();
                SkipNewlines();
                forStmt.Body = ParseBlock();
                return forStmt;
            }

            // 初始化
            if (GetTokenType(Cur) == TokenType.VAR)
            {
                forStmt.Init = ParseVarDecl();
            }
            else if (GetTokenType(Cur) == TokenType.IDENTIFIER && Peek(1).Type == TokenType.COLON_ASSIGN)
            {
                var name = Expect(TokenType.IDENTIFIER).Value as string;
                forStmt.Init = ParseShortVarDecl(name);
            }
            else if (GetTokenType(Cur) != TokenType.LBRACE && GetTokenType(Cur) != TokenType.SEMICOLON)
            {
                // 检查是否是为while风格条件(后跟比较/算术运算符): for w < 5 { 
                // 这种情况下表达式是条件不是初始化
                bool isCondition = false;
                if (GetTokenType(Cur) == TokenType.IDENTIFIER && (Peek(1).Type == TokenType.LT || Peek(1).Type == TokenType.GT || 
                    Peek(1).Type == TokenType.LE || Peek(1).Type == TokenType.GE ||
                    Peek(1).Type == TokenType.EQ || Peek(1).Type == TokenType.NE ||
                    Peek(1).Type == TokenType.ASSIGN))
                {
                    // while-style: for w < 5 { ... }
                    isCondition = true;
                }
                if (!isCondition)
                {
                    // 标识符+LBRACE (如 for x{}): 跳过 init, 由后续 condition 处理
                    if (GetTokenType(Cur) == TokenType.IDENTIFIER && Peek(1).Type == TokenType.LBRACE)
                    {
                        // 不消费任何 token
                    }
                    else
                    {
                        forStmt.Init = ParseConditionExpr();
                    }
                }
            }

            // 条件/更新（SkipNewlines已跳过;分隔符）
            // 在 for init; cond; post 或 for cond 中
            // 如果当前token不是LBRACE，解析表达式
            if (GetTokenType(Cur) == TokenType.SEMICOLON)
            {
                // for init;;post — 条件为空，跳过;
                Advance();
                SkipNewlines();
                if (GetTokenType(Cur) != TokenType.LBRACE)
                    forStmt.Post = ParseForPost();
            }
            else if (GetTokenType(Cur) != TokenType.LBRACE && GetTokenType(Cur) != TokenType.RBRACE)
            {
                // 解析条件 (标识符+LBRACE 只取标识符，避免误解析为复合字面量)
                if (GetTokenType(Cur) == TokenType.IDENTIFIER && Peek(1).Type == TokenType.LBRACE)
                {
                    forStmt.Condition = new Identifier(Advance().Value as string);
                }
                else
                {
                    forStmt.Condition = ParseConditionExpr();
                }
                // 检查是否还有;post
                if (GetTokenType(Cur) == TokenType.SEMICOLON)
                {
                    Advance();
                    SkipNewlines();
                    if (GetTokenType(Cur) != TokenType.LBRACE)
                        forStmt.Post = ParseForPost();
                }
            }

            SkipNewlines();
            forStmt.Body = ParseBlock();
            return forStmt;
        }

        /// <summary>
        /// 解析 switch 语句
        /// </summary>
        private SwitchStatement ParseSwitchStatement()
        {
            var switchStmt = new SwitchStatement();
            Expect(TokenType.SWITCH);
            SkipNewlines();

            if (GetTokenType(Cur) == TokenType.VAR)
            {
                switchStmt.Init = ParseVarDecl();
            }
            else if (GetTokenType(Cur) != TokenType.LBRACE)
            {
                // 标识符+LBRACE (如 switch x{...}): 只取标识符, 避免误解析为复合字面量
                if (GetTokenType(Cur) == TokenType.IDENTIFIER && Peek(1).Type == TokenType.LBRACE)
                {
                    switchStmt.Tag = new Identifier(Advance().Value as string);
                }
                else
                {
                    switchStmt.Tag = ParseConditionExpr();
                }
            }

            SkipNewlines();
            Expect(TokenType.LBRACE);
            SkipNewlines();

            while (GetTokenType(Cur) != TokenType.RBRACE && GetTokenType(Cur) != TokenType.EOF)
            {
                SkipNewlines();
                if (GetTokenType(Cur) == TokenType.RBRACE)
                    break;

                var clause = ParseCaseClause();
                switchStmt.Cases.Add(clause);
            }

            Expect(TokenType.RBRACE);
            return switchStmt;
        }

        /// <summary>
        /// 解析 case 子句
        /// </summary>
        private CaseClause ParseCaseClause()
        {
            var clause = new CaseClause();

            if (GetTokenType(Cur) == TokenType.CASE)
            {
                Advance();
                clause.Cases.Add(ParseExpression());
                while (Match(TokenType.COMMA))
                {
                    clause.Cases.Add(ParseExpression());
                }
            }
            else if (GetTokenType(Cur) == TokenType.DEFAULT)
            {
                Advance();
                clause.IsDefault = true;
            }
            else
            {
                Error("期望 case 或 default");
            }

            Expect(TokenType.COLON);
            SkipNewlines();

            clause.Body = new Block();
            while (GetTokenType(Cur) != TokenType.CASE && GetTokenType(Cur) != TokenType.DEFAULT && GetTokenType(Cur) != TokenType.RBRACE && GetTokenType(Cur) != TokenType.EOF)
            {
                SkipNewlines();
                if (GetTokenType(Cur) == TokenType.CASE || GetTokenType(Cur) == TokenType.DEFAULT || GetTokenType(Cur) == TokenType.RBRACE)
                    break;

                var stmt = ParseStatement();
                if (stmt != null)
                {
                    clause.Body.Statements.Add(stmt);
                }
            }

            return clause;
        }

        /// <summary>
        /// 解析 return 语句
        /// </summary>
        private ReturnStatement ParseReturnStatement()
        {
            Expect(TokenType.RETURN);
            var ret = new ReturnStatement();

            if (GetTokenType(Cur) != TokenType.SEMICOLON && GetTokenType(Cur) != TokenType.NEWLINE && GetTokenType(Cur) != TokenType.RBRACE && GetTokenType(Cur) != TokenType.EOF)
            {
                ret.Results.Add(ParseExpression());
                while (Match(TokenType.COMMA))
                {
                    ret.Results.Add(ParseExpression());
                }
            }

            SkipNewlines();
            return ret;
        }

        /// <summary>
        /// 解析 defer 语句
        /// </summary>
        private DeferStatement ParseDeferStatement()
        {
            Expect(TokenType.DEFER);
            var call = ParseExpression();
            SkipNewlines();
            return new DeferStatement(call);
        }

        /// <summary>
        /// 解析 go 语句
        /// </summary>
        private GoStatement ParseGoStatement()
        {
            Expect(TokenType.GO);
            var call = ParseExpression();
            SkipNewlines();
            return new GoStatement(call);
        }

        /// <summary>
        /// 解析 select 语句（MCU 不兼容：需 goroutine/channel，直接跳过）
        /// </summary>
        private SelectStatement ParseSelectStatement()
        {
            Expect(TokenType.SELECT);
            var selectStmt = new SelectStatement();
            if (Match(TokenType.LBRACE))
            {
                while (!Check(TokenType.RBRACE) && !Check(TokenType.EOF))
                {
                    SkipNewlines();
                    if (Check(TokenType.RBRACE)) break;
                    var clause = new SelectClause();
                    if (Check(TokenType.DEFAULT))
                    {
                        Advance(); // default
                        clause.IsDefault = true;
                    }
                    else if (Check(TokenType.CASE))
                    {
                        Advance(); // case
                        // Parse communication clause: send or receive
                        var commExpr = ParseExpression();
                        clause.Communication = commExpr;
                        if (Match(TokenType.COLON_ASSIGN))
                        {
                            // case x := <-ch: — short var decl in case
                            var decl = new ShortVarDecl();
                            if (commExpr is Identifier id)
                                decl.Names = new List<string> { id.Name };
                            else
                                decl.Names = new List<string>();
                            decl.Values.Add(ParseExpression());
                            clause.Body.Statements.Add(decl);
                        }
                        else if (GetTokenType(Cur) == TokenType.ASSIGN)
                        {
                            // case x = <-ch:
                            Advance();
                            var rhs = ParseExpression();
                            clause.Body.Statements.Add(new Assignment(
                                new List<ASTNode> { commExpr },
                                new List<ASTNode> { rhs }));
                        }
                    }
                    Expect(TokenType.COLON);
                    SkipNewlines();
                    // Parse case body statements
                    while (!Check(TokenType.CASE) && !Check(TokenType.DEFAULT) && !Check(TokenType.RBRACE) && !Check(TokenType.EOF))
                    {
                        var stmt = ParseStatement();
                        if (stmt != null)
                            clause.Body.Statements.Add(stmt);
                        SkipNewlines();
                    }
                    selectStmt.Clauses.Add(clause);
                }
                Expect(TokenType.RBRACE);
            }
            SkipNewlines();
            return selectStmt;
        }

        /// <summary>
        /// 解析类型转换语句
        /// </summary>
        private ExpressionStatement ParseTypeConversionStatement()
        {
            var typeName = Advance().Value as string;
            Expect(TokenType.LPAREN);
            var arg = ParseExpression();
            Expect(TokenType.RPAREN);
            var conv = new TypeConversion(new GoType(typeName), arg);
            SkipNewlines();
            return new ExpressionStatement(conv);
        }
    }
}
