using System.Collections.Generic;
using VMLPlugins;
using CompilerBase;

namespace JavaCompiler
{
    /// <summary>
    /// Java语言语法分析器
    /// </summary>
    public class Parser : ParserBase<Token, TokenType>
    {
        private List<Statement> _extraDeclarations = new List<Statement>();
        private bool IsMCU => CompilerOptionsContext.Current.IsMCU;

        protected override TokenType GetTokenType(Token token) => token.Type;

        public Parser(List<Token> tokens) : base(tokens) { }

        /// <summary>
        /// 解析Java程序
        /// </summary>
        public Program Parse()
        {
            var program = new Program();

            // 解析包声明
            if (Match(TokenType.Package))
            {
                program.Package = ParsePackage();
            }

            // 解析导入声明
            while (Match(TokenType.Import))
            {
                program.Imports.Add(ParseImport());
            }

            // 解析类声明和枚举
            while (!IsAtEnd)
            {
                if (Match(TokenType.Class, TokenType.Interface))
                {
                    program.Classes.Add(ParseClass());
                }
                else if (Match(TokenType.Enum))
                {
                    program.EnumDecls.Add(ParseEnumDecl());
                }
                else
                {
                    Advance(); // 跳过未知标记
                }
            }

            return program;
        }

        private PackageDecl ParsePackage()
        {
            var name = ParseQualifiedName();
            Expect(TokenType.Semicolon, "期望分号");
            return new PackageDecl(name);
        }

        private ImportDecl ParseImport()
        {
            bool isStatic = Match(TokenType.Static);
            var name = ParseQualifiedName();
            bool isWildcard = Match(TokenType.Dot) && Match(TokenType.Multiply);
            Expect(TokenType.Semicolon, "期望分号");
            return new ImportDecl(name, isStatic, isWildcard);
        }

        private EnumDeclStatement ParseEnumDecl()
        {
            string name = Expect(TokenType.Identifier, "期望枚举名").Value;
            Expect(TokenType.LeftBrace, "期望 '{'");
            var enumDecl = new EnumDeclStatement { Name = name };
            while (!Check(TokenType.RightBrace) && !IsAtEnd)
            {
                if (Match(TokenType.Identifier))
                {
                    enumDecl.Members.Add(Previous().Value);
                    Match(TokenType.Comma);
                    // 跳过可选的 = value
                    if (Check(TokenType.Assign))
                    {
                        Advance();
                        while (!Check(TokenType.Comma) && !Check(TokenType.RightBrace) && !IsAtEnd)
                            Advance();
                    }
                }
                else break;
            }
            Expect(TokenType.RightBrace, "期望 '}'");
            return enumDecl;
        }

        private ClassDecl ParseClass()
        {
            var classToken = Previous();
            var name = Expect(TokenType.Identifier, "期望类名").Value;

            // 跳过泛型类型参数: class Box<T> 或 class Box<T extends Foo>
            if (Match(TokenType.LessThan))
            {
                int genDepth = 1;
                while (genDepth > 0 && !IsAtEnd)
                {
                    if (Check(TokenType.LessThan)) genDepth++;
                    else if (Check(TokenType.GreaterThan)) genDepth--;
                    Advance();
                }
            }

            var classDecl = new ClassDecl(name);

            // 解析继承
            if (Match(TokenType.Extends))
            {
                classDecl.SuperClass = ParseQualifiedName();
            }

            // 解析接口实现
            if (Match(TokenType.Implements))
            {
                do
                {
                    classDecl.Interfaces.Add(ParseQualifiedName());
                } while (Match(TokenType.Comma));
            }

            Expect(TokenType.LeftBrace, "期望 '{'");

            // 解析类成员
            while (!Check(TokenType.RightBrace) && !IsAtEnd)
            {
                // 跳过注解 @Override, @Deprecated 等
                while (Match(TokenType.AtSymbol))
                {
                    ParseQualifiedName();
                    if (Match(TokenType.LeftParen))
                    {
                        int depth = 1;
                        while (depth > 0 && !IsAtEnd)
                        {
                            if (Check(TokenType.LeftParen)) depth++;
                            else if (Check(TokenType.RightParen)) depth--;
                            Advance();
                        }
                    }
                }
                var modifiers = ParseModifiers();

                if (Match(TokenType.Enum))
                {
                    // 枚举
                    var enumDecl = ParseEnumDecl();
                    enumDecl.Modifiers = modifiers;
                    classDecl.EnumDecls.Add(enumDecl);
                }
                else if (Match(TokenType.Class, TokenType.Interface))
                {
                    // 嵌套类/接口
                    var nestedClass = ParseClass();
                    nestedClass.Modifiers = modifiers;
                    classDecl.Classes.Add(nestedClass);
                }
                else if (IsTypeToken())
                {
                    // 检查是否为构造方法: 类名(参数) — 紧跟左括号, 无返回类型
                    if (GetTokenType(Cur) == TokenType.Identifier && _pos + 1 < _tokens.Count
                        && _tokens[_pos + 1].Type == TokenType.LeftParen
                        && Cur.Value == classDecl.Name)
                    {
                        // 构造方法: ClassName(params) { ... }
                        Advance(); // 消费类名
                        Match(TokenType.LeftParen);
                        var parameters = ParseParameters();
                        var constructor = new ConstructorDecl(classDecl.Name);
                        constructor.Parameters = parameters;
                        constructor.Modifiers = modifiers;
                        if (Match(TokenType.Throws)) { }
                        if (Match(TokenType.LeftBrace))
                        {
                            constructor.Body = ParseBlock();
                            // 检查第一个语句是否是 super(...)
                            if (constructor.Body.Statements.Count > 0 &&
                                constructor.Body.Statements[0] is ExpressionStatement es &&
                                es.Expression is MethodCallExpression mc &&
                                mc.MethodName == "super")
                            {
                                constructor.SuperCall = new SuperCall { Arguments = mc.Arguments };
                                constructor.Body.Statements.RemoveAt(0);
                            }
                        }
                        classDecl.Constructors.Add(constructor);
                    }
                    else
                    {
                        // 跳过方法级泛型类型参数: <T> 或 <T extends Foo>
                        if (Match(TokenType.LessThan))
                        {
                            int mGenDepth = 1;
                            while (mGenDepth > 0 && !IsAtEnd)
                            {
                                if (Check(TokenType.LessThan)) mGenDepth++;
                                else if (Check(TokenType.GreaterThan)) mGenDepth--;
                                Advance();
                            }
                        }
                        var type = ParseType();
                        var nameToken = Expect(TokenType.Identifier, "期望成员名");

                        if (Match(TokenType.LeftParen))
                        {
                            // 方法
                            var parameters = ParseParameters();
                            var method = new MethodDecl(type, nameToken.Value);
                            method.Parameters = parameters;
                            method.Modifiers = modifiers;
                            if (Match(TokenType.Throws)) { }
                            if (Match(TokenType.LeftBrace))
                            {
                                method.Body = ParseBlock();
                            }
                            classDecl.Methods.Add(method);
                        }
                        else
                        {
                            // 字段 (支持多变量: int x, y, z;)
                            var field = new FieldDecl(type, nameToken.Value);
                            field.Modifiers = modifiers;
                            if (Match(TokenType.Assign))
                            {
                                field.Initializer = ParseExpression();
                            }
                            classDecl.Fields.Add(field);
                            // 跳过逗号分隔的后续字段名
                            while (Match(TokenType.Comma))
                            {
                                string nextName = Expect(TokenType.Identifier, "期望字段名").Value;
                                var nextField = new FieldDecl(type, nextName);
                                nextField.Modifiers = modifiers;
                                if (Match(TokenType.Assign))
                                    nextField.Initializer = ParseExpression();
                                classDecl.Fields.Add(nextField);
                            }
                            Expect(TokenType.Semicolon, "期望分号");
                        }
                    }
                }
                else
                {
                    Advance(); // 跳过未知标记
                }
            }

            Expect(TokenType.RightBrace, "期望 '}'");

            return classDecl;
        }

        private Modifiers ParseModifiers()
        {
            var modifiers = new Modifiers();

            while (true)
            {
                if (Match(TokenType.Public)) modifiers.IsPublic = true;
                else if (Match(TokenType.Private)) modifiers.IsPrivate = true;
                else if (Match(TokenType.Protected)) modifiers.IsProtected = true;
                else if (Match(TokenType.Static)) modifiers.IsStatic = true;
                else if (Match(TokenType.Final)) modifiers.IsFinal = true;
                else if (Match(TokenType.Abstract)) modifiers.IsAbstract = true;
                else if (Match(TokenType.Synchronized)) {
                    if (IsMCU) WarningEmitter.Emit("java", "MCU模式: synchronized被忽略（不支持多线程）");
                    modifiers.IsSynchronized = true;
                }
                else if (Match(TokenType.Volatile)) {
                    if (IsMCU) WarningEmitter.Emit("java", "MCU模式: volatile被忽略（不支持多线程）");
                    modifiers.IsVolatile = true;
                }
                else if (Match(TokenType.Transient)) modifiers.IsTransient = true;
                else if (Match(TokenType.Native)) modifiers.IsNative = true;
                else if (Match(TokenType.Default)) modifiers.IsDefault = true;
                else break;
            }

            return modifiers;
        }

        private string ParseType()
        {
            string type;
            
            // 处理基本类型和void
            if (Match(TokenType.Int, TokenType.Long, TokenType.Short, TokenType.Byte,
                     TokenType.Char, TokenType.Float, TokenType.Double, TokenType.Boolean,
                     TokenType.Void))
            {
                type = Previous().Value;
            }
            else
            {
                type = Expect(TokenType.Identifier, "期望类型").Value;
            }

            // 处理泛型
            if (Match(TokenType.LessThan))
            {
                // 简化处理：跳过泛型参数
                while (!Match(TokenType.GreaterThan) && !IsAtEnd)
                {
                    Advance();
                }
            }

            // 处理数组
            while (Match(TokenType.LeftBracket) && Match(TokenType.RightBracket))
            {
                type += "[]";
            }

            return type;
        }

        private List<Parameter> ParseParameters()
        {
            var parameters = new List<Parameter>();

            if (!Check(TokenType.RightParen))
            {
                do
                {
                    var type = ParseType();
                    // 可变参数: Type... name
                    bool isVararg = Match(TokenType.Ellipsis);
                    var name = Expect(TokenType.Identifier, "期望参数名").Value;
                    parameters.Add(new Parameter(type, name) { IsVararg = isVararg });
                } while (Match(TokenType.Comma));
            }

            Expect(TokenType.RightParen, "期望 ')'");
            return parameters;
        }

        private Block ParseBlock()
        {
            var block = new Block();

            while (!Check(TokenType.RightBrace) && !IsAtEnd)
            {
                var stmt = ParseStatement();
                if (stmt != null)
                {
                    block.Statements.Add(stmt);
                    // Inject any extra declarations from comma-separated multi-var decls
                    if (_extraDeclarations.Count > 0)
                    {
                        block.Statements.AddRange(_extraDeclarations);
                        _extraDeclarations.Clear();
                    }
                }
            }

            Expect(TokenType.RightBrace, "期望 '}'");
            return block;
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
            if (Match(TokenType.If)) return ParseIfStatement();
            if (Match(TokenType.While)) return ParseWhileStatement();
            if (Match(TokenType.Do)) return ParseDoWhileStatement();
            if (Match(TokenType.For)) return ParseForStatement();
            if (Match(TokenType.Switch)) return ParseSwitchStatement();
            if (Match(TokenType.Try)) return ParseTryStatement();
            if (Match(TokenType.Throw)) return ParseThrowStatement();
            if (Match(TokenType.Break)) {
                string? breakLabel = Check(TokenType.Identifier) ? Advance().Value : null;
                Expect(TokenType.Semicolon, "期望 ';'");
                return new BreakStatement(breakLabel);
            }
            if (Match(TokenType.Continue)) {
                string? continueLabel = Check(TokenType.Identifier) ? Advance().Value : null;
                Expect(TokenType.Semicolon, "期望 ';'");
                return new ContinueStatement(continueLabel);
            }
            if (Match(TokenType.Assert)) return ParseAssertStatement();
            if (Match(TokenType.Return)) return ParseReturnStatement();
            // Handle 'final' modifier before type: final int x = 1;
            if (Match(TokenType.Final))
            {
                // consume actual type after final
                if (Match(TokenType.Int, TokenType.Long, TokenType.Short, TokenType.Byte,
                         TokenType.Char, TokenType.Float, TokenType.Double, TokenType.Boolean))
                {
                    return ParseVariableDeclStatement();
                }
                if (Check(TokenType.Identifier))
                {
                    Advance(); // consume type identifier (e.g. String, List)
                    return ParseVariableDeclStatement();
                }
                // fallback: just final without type is invalid, but try expression
                return ParseExpressionStatement();
            }
            if (Match(TokenType.Int, TokenType.Long, TokenType.Short, TokenType.Byte,
                     TokenType.Char, TokenType.Float, TokenType.Double, TokenType.Boolean))
            {
                return ParseVariableDeclStatement();
            }
            if (Check(TokenType.Identifier))
            {
                var lookahead = _pos + 1;
                // Labeled statement: identifier : statement
                if (lookahead < _tokens.Count && _tokens[lookahead].Type == TokenType.Colon)
                {
                    string label = Advance().Value; // consume label identifier
                    Advance(); // consume colon
                    return new LabeledStatement(label, ParseStatement());
                }
                if (lookahead < _tokens.Count && _tokens[lookahead].Type == TokenType.Identifier)
                {
                    Advance(); // consume the type identifier (e.g. "String")
                    return ParseVariableDeclStatement();
                }
                else if (lookahead < _tokens.Count && _tokens[lookahead].Type == TokenType.LessThan && IsGenericVarDecl())
                {
                    Advance(); // 消费泛型类型标识符 (e.g. "Box")
                    return ParseVariableDeclStatement();
                }
                else
                {
                    return ParseExpressionStatement();
                }
            }

            if (Match(TokenType.Semicolon))
            {
                return null; // 空语句
            }

            if (Match(TokenType.LeftBrace))
            {
                return new BlockStatement(ParseBlock());
            }

            return ParseExpressionStatement();
        }

        private Statement ParseDoWhileStatement()
        {
            var body = ParseStatement();
            Expect(TokenType.While, "期望 'while'");
            Expect(TokenType.LeftParen, "期望 '('");
            var condition = ParseExpression();
            Expect(TokenType.RightParen, "期望 ')'");
            Match(TokenType.Semicolon);
            return new DoWhileStatement(body, condition);
        }

        private Statement ParseForEachStatement()
        {
            Expect(TokenType.LeftParen, "期望 '('");
            string varType = "";
            if (Match(TokenType.Int, TokenType.Long, TokenType.Short, TokenType.Byte,
                     TokenType.Char, TokenType.Float, TokenType.Double, TokenType.Boolean))
                varType = Previous().Value;
            else
                varType = Expect(TokenType.Identifier, "期望类型").Value;
            string varName = Expect(TokenType.Identifier, "期望变量名").Value;
            Expect(TokenType.Colon, "期望 ':'");
            var collection = ParseExpression();
            Expect(TokenType.RightParen, "期望 ')'");
            var body = ParseStatement();
            return new ForEachStatement(varType, varName, collection, body);
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
                if (Match(TokenType.Case))
                {
                    var caseVal = ParseExpression();
                    Expect(TokenType.Colon, "期望 ':'");
                    var sc = new SwitchCase { Value = caseVal };
                    while (!Check(TokenType.RightBrace) && !Check(TokenType.Case) && !Check(TokenType.Default) && !IsAtEnd)
                    {
                        var stmt = ParseStatement();
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

        private Statement ParseTryStatement()
        {
            var body = ParseStatement();
            var ts = new TryStatement(body);
            while (Match(TokenType.Catch))
            {
                var cc = new CatchClause();
                if (Match(TokenType.LeftParen))
                {
                    if (Match(TokenType.Identifier)) cc.ExceptionType = Previous().Value;
                    if (Match(TokenType.Identifier)) cc.VariableName = Previous().Value;
                    Expect(TokenType.RightParen, "期望 ')'");
                }
                cc.Body = ParseStatement();
                ts.Catches.Add(cc);
            }
            if (Match(TokenType.Finally))
            {
                ts.FinallyBody = ParseStatement();
            }
            return ts;
        }

        private Statement ParseThrowStatement()
        {
            Expression? value = null;
            if (!Check(TokenType.Semicolon))
            {
                value = ParseExpression();
            }
            Expect(TokenType.Semicolon, "期望 ';'");
            return new ThrowStatement(value);
        }

        private IfStatement ParseIfStatement()
        {
            Expect(TokenType.LeftParen, "期望 '('");
            var condition = ParseExpression();
            Expect(TokenType.RightParen, "期望 ')'");
            var thenBranch = ParseStatement();
            Statement elseBranch = null;
            if (Match(TokenType.Else))
            {
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

        private Statement ParseAssertStatement()
        {
            var condition = ParseExpression();
            Expression? message = null;
            if (Match(TokenType.Colon))
            {
                message = ParseExpression();
            }
            Expect(TokenType.Semicolon, "期望 ';'");
            return new AssertStatement(condition, message);
        }

        private Statement ParseForStatement()
        {
            Expect(TokenType.LeftParen, "期望 '('");

            // 检查是否是增强 for (Type var : iterable)
            int savePos = _pos;
            // 跳过类型名(可能多个token如 int[] 或限定名)
            if (IsTypeToken()) Advance(); else { _pos = savePos; }
            if (Check(TokenType.Identifier) && 
                _pos + 1 < _tokens.Count && _tokens[_pos + 1].Type == TokenType.Colon)
            {
                _pos = savePos;
                return ParseForEachBody();
            }
            _pos = savePos;

            Statement initializer = null;
            if (!Match(TokenType.Semicolon))
            {
                initializer = ParseForInitializer();
            }
            
            Expression condition = null;
            if (!Check(TokenType.Semicolon))
            {
                condition = ParseExpression();
            }
            Expect(TokenType.Semicolon, "期望 ';'");
            
            Expression increment = null;
            if (!Check(TokenType.RightParen))
            {
                increment = ParseExpression();
            }
            Expect(TokenType.RightParen, "期望 ')'");
            
            var body = ParseStatement();
            return new ForStatement(initializer, condition, increment, body);
        }

        private ForEachStatement ParseForEachBody()
        {
            string varType = "";
            if (IsTypeToken())
                varType = Advance().Value;
            else
                varType = Expect(TokenType.Identifier, "期望类型").Value;
            string varName = Expect(TokenType.Identifier, "期望变量名").Value;
            Expect(TokenType.Colon, "期望 ':'");
            var collection = ParseExpression();
            Expect(TokenType.RightParen, "期望 ')'");
            var body = ParseStatement();
            return new ForEachStatement(varType, varName, collection, body);
        }

        private Statement ParseForInitializer()
        {
            // Skip 'final' modifier in for-loop initializer
            Match(TokenType.Final);
            if (Match(TokenType.Int, TokenType.Long, TokenType.Short, TokenType.Byte,
                     TokenType.Char, TokenType.Float, TokenType.Double, TokenType.Boolean))
            {
                return ParseVariableDeclStatement();
            }
            else if (Check(TokenType.Identifier))
            {
                // 检查是变量声明还是表达式: Type varName = x vs expr = x
                int save = _pos;
                Advance(); // consume potential type identifier
                if (Check(TokenType.Identifier))
                {
                    // Type Name — 这是变量声明
                    return ParseVariableDeclStatement();
                }
                // 回退 — 这是表达式 (如 i = 0)
                _pos = save;
                return ParseExpressionStatement();
            }
            else
            {
                return ParseExpressionStatement();
            }
        }

        private ReturnStatement ParseReturnStatement()
        {
            Expression value = null;
            if (!Check(TokenType.Semicolon))
            {
                value = ParseExpression();
            }
            Expect(TokenType.Semicolon, "期望 ';'");
            return new ReturnStatement(value);
        }

        private VariableDeclStatement ParseVariableDeclStatement()
        {
            var typeToken = Previous();
            var type = typeToken.Value;

            // 泛型类型: Box<Integer> — 跳过 <...> 并保留完整类型名
            if (Check(TokenType.LessThan))
            {
                int gd = 0;
                var sb = new System.Text.StringBuilder(type);
                do
                {
                    var tok = Advance();
                    if (tok.Type == TokenType.LessThan) gd++;
                    else if (tok.Type == TokenType.GreaterThan) gd--;
                    sb.Append(tok.Value);
                } while (gd > 0 && !IsAtEnd);
                type = sb.ToString();
            }

            // 支持数组前缀语法: int[] name
            while (Match(TokenType.LeftBracket) && Match(TokenType.RightBracket))
            {
                type += "[]";
            }

            var name = Expect(TokenType.Identifier, "期望变量名").Value;
            Expression initializer = null;
            if (Match(TokenType.Assign))
            {
                initializer = ParseExpression();
            }
            // Handle comma-separated multi-variable: int a=1, b=2, r;
            if (Match(TokenType.Comma))
            {
                var stmts = new List<Statement>();
                stmts.Add(new VariableDeclStatement(type, name, initializer));
                do
                {
                    var nextName = Expect(TokenType.Identifier, "期望变量名").Value;
                    Expression nextInit = null;
                    if (Match(TokenType.Assign))
                        nextInit = ParseExpression();
                    stmts.Add(new VariableDeclStatement(type, nextName, nextInit));
                } while (Match(TokenType.Comma));
                Expect(TokenType.Semicolon, "期望 ';'");
                if (stmts.Count > 1)
                    _extraDeclarations.AddRange(stmts.Skip(1));
                return (VariableDeclStatement)stmts[0];
            }
            Expect(TokenType.Semicolon, "期望 ';'");
            return new VariableDeclStatement(type, name, initializer);
        }

        /// <summary>检测当前 Identifier 后的 &lt;...&gt; 是否为泛型变量声明 (Box&lt;Integer&gt; b)
        /// 而非比较表达式 (a &lt; b)。启发式: 匹配的 &gt; 之后紧跟变量名标识符。</summary>
        private bool IsGenericVarDecl()
        {
            int depth = 0;
            for (int i = 1; _pos + i < _tokens.Count; i++)
            {
                var t = _tokens[_pos + i].Type;
                if (t == TokenType.LessThan) depth++;
                else if (t == TokenType.GreaterThan)
                {
                    depth--;
                    if (depth == 0)
                    {
                        int next = _pos + i + 1;
                        return next < _tokens.Count && _tokens[next].Type == TokenType.Identifier;
                    }
                }
                else if (t == TokenType.Semicolon || t == TokenType.EndOfFile) return false;
            }
            return false;
        }

        private ExpressionStatement ParseExpressionStatement()
        {
            var expression = ParseExpression();
            Expect(TokenType.Semicolon, "期望 ';'");
            return new ExpressionStatement(expression);
        }

        private Expression ParseExpression()
        {
            return ParseConditional();
        }

        private Expression ParseConditional()
        {
            var expr = ParseAssignment();

            if (Match(TokenType.QuestionMark))
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

            if (Match(TokenType.Assign, TokenType.PlusAssign, TokenType.MinusAssign,
                     TokenType.MultiplyAssign, TokenType.DivideAssign, TokenType.ModuloAssign,
                     TokenType.AndAssign, TokenType.OrAssign, TokenType.XorAssign,
                     TokenType.LeftShiftAssign, TokenType.RightShiftAssign, TokenType.UnsignedRightShiftAssign))
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

            while (Match(TokenType.LessThan, TokenType.LessThanOrEqual,
                        TokenType.GreaterThan, TokenType.GreaterThanOrEqual,
                        TokenType.Instanceof))
            {
                var op = Previous().Type;
                var right = ParseShift();
                expr = new BinaryExpression(expr, op, right);
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
            if (Match(TokenType.Plus, TokenType.Minus, TokenType.LogicalNot, TokenType.BitwiseNot,
                     TokenType.Increment, TokenType.Decrement))
            {
                var op = Previous().Type;
                var operand = ParseUnary();
                return new UnaryExpression(op, operand);
            }

            // 类型转换: (int)expr, (float)expr, etc.
            if (Check(TokenType.LeftParen))
            {
                int savedPos = _pos;
                Advance(); // 消费 '('
                if (Check(TokenType.Int) || Check(TokenType.Long) || Check(TokenType.Short) ||
                    Check(TokenType.Byte) || Check(TokenType.Char) || Check(TokenType.Float) ||
                    Check(TokenType.Double) || Check(TokenType.Boolean))
                {
                    string typeName = ParseType();
                    if (Match(TokenType.RightParen))
                    {
                        var operand = ParseUnary();
                        return new CastExpression(typeName, operand);
                    }
                }
                // 回溯: 不是类型转换，交给 ParsePrimary 作为括号表达式
                _pos = savedPos;
            }

            var expr = ParsePrimary();

            // Postfix ++/--
            if (Match(TokenType.Increment))
                return new UnaryExpression(TokenType.Increment, expr, isPostfix: true);
            if (Match(TokenType.Decrement))
                return new UnaryExpression(TokenType.Decrement, expr, isPostfix: true);

            return expr;
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
                var tokenValue = Previous().Value;
                string type = "int";
                
                if (tokenValue.EndsWith("l") || tokenValue.EndsWith("L"))
                {
                    tokenValue = tokenValue.Substring(0, tokenValue.Length - 1);
                    type = "long";
                }
                
                try
                {
                    bool isHex = tokenValue.StartsWith("0x") || tokenValue.StartsWith("0X");
                    if (type == "long")
                    {
                        var value = isHex ? Convert.ToInt64(tokenValue.Substring(2), 16) : long.Parse(tokenValue);
                        return new LiteralExpression(value, type);
                    }
                    else
                    {
                        var value = isHex ? Convert.ToInt32(tokenValue.Substring(2), 16) : int.Parse(tokenValue);
                        return new LiteralExpression(value, type);
                    }
                }
                catch (FormatException)
                {
                    // 如果解析失败，返回默认值
                    return new LiteralExpression(0, "int");
                }
            }

            if (Match(TokenType.FloatLiteral))
            {
                var tokenValue = Previous().Value;
                // Java 默认: 无后缀的浮点字面量是 double (不是 float)
                string type = "double";

                // 处理Java浮点数后缀：f, F, d, D
                if (tokenValue.EndsWith("f") || tokenValue.EndsWith("F"))
                {
                    tokenValue = tokenValue.Substring(0, tokenValue.Length - 1);
                    type = "float";
                }
                else if (tokenValue.EndsWith("d") || tokenValue.EndsWith("D"))
                {
                    tokenValue = tokenValue.Substring(0, tokenValue.Length - 1);
                    type = "double";
                }

                // 尝试解析为浮点数
                try
                {
                    if (type == "float")
                    {
                        var value = float.Parse(tokenValue);
                        return new LiteralExpression(value, type);
                    }
                    else // double
                    {
                        var value = double.Parse(tokenValue);
                        return new LiteralExpression(value, type);
                    }
                }
                catch (FormatException)
                {
                    // 如果解析失败，返回默认值
                    return new LiteralExpression(0.0, "double");  // 默认 double 值也应匹配
                }
            }

            if (Match(TokenType.StringLiteral))
            {
                var value = Previous().Value;
                return new LiteralExpression(value, "String");
            }

            if (Match(TokenType.CharLiteral))
            {
                var value = Previous().Value[0];
                return new LiteralExpression(value, "char");
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
                return new LiteralExpression(null, "Object");
            }

            if (Match(TokenType.This))
            {
                // this(...) 构造方法调用链
                if (Match(TokenType.LeftParen))
                {
                    var call = new MethodCallExpression(null, "this");
                    if (!Check(TokenType.RightParen))
                    {
                        do { call.Arguments.Add(ParseExpression()); } while (Match(TokenType.Comma));
                    }
                    Expect(TokenType.RightParen, "期望 ')'");
                    return call;
                }
                return new VariableExpression("this");
            }

            if (Match(TokenType.Super))
            {
                if (Match(TokenType.LeftParen))
                {
                    var superCall = new MethodCallExpression(null, "super");
                    if (!Check(TokenType.RightParen))
                    {
                        do { superCall.Arguments.Add(ParseExpression()); } while (Match(TokenType.Comma));
                    }
                    Expect(TokenType.RightParen, "期望 ')'");
                    return superCall;
                }
                return new VariableExpression("super");
            }

            if (Match(TokenType.Identifier))
            {
                var name = Previous().Value;
                var expr = new VariableExpression(name) as Expression;

                // 处理成员访问和方法调用链
                while (true)
                {
                    // 成员访问：System.out
                    if (Match(TokenType.Dot))
                    {
                        var memberName = Expect(TokenType.Identifier, "期望成员名").Value;
                        
                        // 检查是否是方法调用：System.out.println()
                        if (Match(TokenType.LeftParen))
                        {
                            var methodCall = new MethodCallExpression(expr, memberName);
                            if (!Check(TokenType.RightParen))
                            {
                                do
                                {
                                    methodCall.Arguments.Add(ParseExpression());
                                } while (Match(TokenType.Comma));
                            }
                            Expect(TokenType.RightParen, "期望 ')'");
                            expr = methodCall;
                        }
                        else
                        {
                            // 字段访问：System.out
                            expr = new FieldAccessExpression(expr, memberName);
                        }
                    }
                    // 直接方法调用：println()
                    else if (Match(TokenType.LeftParen))
                    {
                        var methodCall = new MethodCallExpression(null, name);
                        if (!Check(TokenType.RightParen))
                        {
                            do
                            {
                                methodCall.Arguments.Add(ParseExpression());
                            } while (Match(TokenType.Comma));
                        }
                        Expect(TokenType.RightParen, "期望 ')'");
                        expr = methodCall;
                    }
                    // 数组索引：arr[i]
                    else if (Match(TokenType.LeftBracket))
                    {
                        var index = ParseExpression();
                        Expect(TokenType.RightBracket, "期望 ']'");
                        expr = new ArrayAccessExpression(expr, index);
                    }
                    // 后缀自增/自减：x++ 或 x--
                    else if (Match(TokenType.Increment))
                    {
                        expr = new UnaryExpression(TokenType.Increment, expr, isPostfix: true);
                    }
                    else if (Match(TokenType.Decrement))
                    {
                        expr = new UnaryExpression(TokenType.Decrement, expr, isPostfix: true);
                    }
                    else
                    {
                        break;
                    }
                }

                return expr;
            }

            if (Match(TokenType.New))
            {
                // 关键字类型: int, char, float, double, long, short, byte, boolean
                string typeName;
                if (Match(TokenType.Int)) typeName = "int";
                else if (Match(TokenType.Char)) typeName = "char";
                else if (Match(TokenType.Float)) typeName = "float";
                else if (Match(TokenType.Double)) typeName = "double";
                else if (Match(TokenType.Long)) typeName = "long";
                else if (Match(TokenType.Short)) typeName = "short";
                else if (Match(TokenType.Byte)) typeName = "byte";
                else if (Match(TokenType.Boolean)) typeName = "boolean";
                else typeName = Expect(TokenType.Identifier, "期望类型名").Value;

                // 泛型实例化: new Box<>(42) / new Box<Integer>(42) — 跳过 <...> (含钻石 < >)
                if (Check(TokenType.LessThan))
                {
                    int gd = 0;
                    do
                    {
                        var tok = Advance();
                        if (tok.Type == TokenType.LessThan) gd++;
                        else if (tok.Type == TokenType.GreaterThan) gd--;
                        typeName += tok.Value;
                    } while (gd > 0 && !IsAtEnd);
                }

                if (Match(TokenType.LeftBracket))
                {
                    // new Type[N] — 数组分配
                    var sizeExpr = ParseExpression();
                    Expect(TokenType.RightBracket, "期望 ']'");
                    typeName += "[]";
                    // 支持多维数组: new int[3][3]
                    while (Match(TokenType.LeftBracket))
                    {
                        ParseExpression();
                        Expect(TokenType.RightBracket, "期望 ']'");
                        typeName += "[]";
                    }
                    // 创建一个带 "[]" 标识的 NewExpression
                    var newExpr = new NewExpression($"{typeName}");
                    newExpr.Arguments.Add(sizeExpr);
                    return newExpr;
                }
                else if (Match(TokenType.LeftParen))
                {
                    var newExpr = new NewExpression(typeName);
                    if (!Check(TokenType.RightParen))
                    {
                        do { newExpr.Arguments.Add(ParseExpression()); } while (Match(TokenType.Comma));
                    }
                    Expect(TokenType.RightParen, "期望 ')'");
                    return newExpr;
                }
                else
                {
                    return new NewExpression(typeName);
                }
            }

            if (Match(TokenType.LeftParen))
            {
                var expr = ParseExpression();
                Expect(TokenType.RightParen, "期望 ')'");
                return expr;
            }

            if (Match(TokenType.LeftBrace))
            {
                var arr = new ArrayInitializerExpression();
                if (!Check(TokenType.RightBrace))
                {
                    do { arr.Elements.Add(ParseExpression()); } while (Match(TokenType.Comma));
                }
                Expect(TokenType.RightBrace, "期望 '}'");
                return arr;
            }

            // 错误恢复
            Advance();
            return new LiteralExpression(0, "int");
        }

        private bool IsTypeToken()
        {
            return Check(TokenType.Int) || Check(TokenType.Long) || Check(TokenType.Short) || 
                   Check(TokenType.Byte) || Check(TokenType.Char) || Check(TokenType.Float) || 
                   Check(TokenType.Double) || Check(TokenType.Boolean) || Check(TokenType.Void) ||
                   Check(TokenType.Identifier);
        }

        private string ParseQualifiedName()
        {
            var name = Expect(TokenType.Identifier, "期望标识符").Value;

            while (Check(TokenType.Dot))
            {
                // 提前终止: 遇到 .* 是通配符导入，留给 ParseImport 处理
                if (_tokens.Count > _pos + 1 && _tokens[_pos + 1].Type == TokenType.Multiply)
                    break;
                Advance(); // 消费点号
                name += "." + Expect(TokenType.Identifier, "期望标识符").Value;
            }

            return name;
        }

    }

}