using CompilerBase;
using System;
using System.Collections.Generic;
using VMLPlugins;

namespace CSharpCompiler
{
    /// <summary>
    /// C#语法分析器
    /// </summary>
    public partial class Parser : ParserBase<Token, TokenType>
    {
        protected override TokenType GetTokenType(Token token) => token.Type;
        private readonly List<Token> tokens;
        private int position;
        private readonly bool debugMode;
        private bool IsMCU => CompilerOptionsContext.Current.IsMCU;
        public List<string> UsingNamespaces { get; } = new();
        private List<string> _usingNamespaces = new();
        private string _currentClassName = null;
        private List<Statement> _extraDeclarations = new List<Statement>();
        private bool _pendingNative = false;

        public Parser(List<Token> tokens, bool debugMode = false) : base(tokens)
        {
            this.tokens = tokens;
            position = 0;
            this.debugMode = debugMode;
        }
        
        public Program Parse()
        {
            var program = new Program();
            program.Namespaces.AddRange(_usingNamespaces);
            
            while (!IsAtEnd())
            {
                Statement statement;
                try { statement = ParseStatement(); }
                catch
                {
                    // 跳过当前有问题的语句, 找下一个分号或右大括号
                    int depth = 0;
                    while (!IsAtEnd())
                    {
                        if (Check(TokenType.LeftBrace)) depth++;
                        if (Check(TokenType.RightBrace)) { if (depth <= 0) break; depth--; }
                        if (Check(TokenType.Semicolon) && depth <= 0) { Advance(); break; }
                        Advance();
                    }
                    statement = null;
                }
                if (statement != null)
                {
                    program.Statements.Add(statement);
                    // Inject any extra declarations from comma-separated multi-var decls
                    if (_extraDeclarations.Count > 0)
                    {
                        program.Statements.AddRange(_extraDeclarations);
                        _extraDeclarations.Clear();
                    }
                }
            }
            
            return program;
        }
        
        private Statement ParseStatement()
        {
            if (IsAtEnd()) return null;
            
            // 代码块
            if (Check(TokenType.LeftBrace))
            {
                return ParseBlock();
            }
            
            // 检查Console.WriteLine / Console.Write
            int savePos = position;
            if (Match(TokenType.Identifier, "Console"))
            {
                if (Peek().Type == TokenType.Dot && Peek(1).Type == TokenType.Identifier &&
                    (Peek(1).Value == "WriteLine" || Peek(1).Value == "Write"))
                {
                    return ParseConsoleWriteLineStatement();
                }
                position = savePos;
            }
            
            // if语句
            if (Match(TokenType.If))
            {
                return ParseIfStatement();
            }
            
            // while语句
            if (Match(TokenType.While))
            {
                return ParseWhileStatement();
            }
            
            // foreach语句
            if (Match(TokenType.Foreach))
            {
                return ParseForEachStatement();
            }

            // for语句
            if (Match(TokenType.For))
            {
                return ParseForStatement();
            }
            
            // switch语句
            if (Match(TokenType.Switch))
            {
                return ParseSwitchStatement();
            }

            // do-while语句
            if (Match(TokenType.Do))
            {
                return ParseDoWhileStatement();
            }

            // try语句
            if (Match(TokenType.Try))
            {
                return ParseTryStatement();
            }

            // throw语句
            if (Match(TokenType.Throw))
            {
                return ParseThrowStatement();
            }

            // MCU 模式: lock 语句（线程同步，MCU 不支持）
            if (Match(TokenType.Lock) && IsMCU)
            {
                WarningEmitter.Emit("csharp", Strings.McuSkipped("csharp", "lock语句", "不支持多线程"), line: Previous().Line);
                Expect(TokenType.LeftParen, "Expected '(' after lock");
                SkipExpression();  // 跳过锁定对象表达式
                Expect(TokenType.RightParen, "Expected ')' after lock expression");
                SkipBlock();       // 跳过锁定的代码块
                return null;
            }

            // MCU 模式: yield 语句（迭代器，MCU 不支持）
            if (Match(TokenType.Yield) && IsMCU)
            {
                WarningEmitter.Emit("csharp", Strings.McuSkipped("csharp", "yield语句", "不支持迭代器"), line: Previous().Line);
                // 消耗 yield return / yield break
                Advance();
                Expect(TokenType.Semicolon, "Expected ';' after yield");
                return null;
            }

            // MCU 模式: fixed 语句（固定指针，MCU 平坦内存模型不需要）
            if (Match(TokenType.Fixed) && IsMCU)
            {
                WarningEmitter.Emit("csharp", Strings.McuSkipped("csharp", "fixed语句", "平坦内存模型不需要"), line: Previous().Line);
                Expect(TokenType.LeftParen, "Expected '(' after fixed");
                SkipExpression();
                Expect(TokenType.RightParen, "Expected ')' after fixed expression");
                SkipBlock();
                return null;
            }

            // break/continue语句
            if (Match(TokenType.Break))
            {
                Expect(TokenType.Semicolon, "Expected ';' after break");
                return new BreakStatement();
            }
            if (Match(TokenType.Continue))
            {
                Expect(TokenType.Semicolon, "Expected ';' after continue");
                return new ContinueStatement();
            }
            
            // MCU 模式: 跳过 async 修饰符（OS依赖特性）
            if (Check(TokenType.Async))
            {
                if (IsMCU)
                {
                    WarningEmitter.Emit("csharp", "MCU模式: async关键字被忽略（不支持的OS特性）", line: Peek().Line);
                }
                // OS mode: allow async through (emit runtime stubs later)
                return ParseStatement();
            }

            // unsafe 代码块: unsafe { int* p = &x; *p = 42; }
            if (Match(TokenType.Unsafe))
            {
                if (Check(TokenType.LeftBrace))
                {
                    var body = ParseBlock();
                    return new UnsafeBlock(body);
                }
                // unsafe 修饰符在语句之前: unsafe int* p = &x;
                return ParseStatement();
            }

            // 跳过 public/private/protected/static/readonly/extern/native 等修饰符
            if (Check(TokenType.Public) || Check(TokenType.Private) || Check(TokenType.Protected) ||
                Check(TokenType.Static) || Check(TokenType.Readonly) || Check(TokenType.Extern) ||
                Check(TokenType.Native) || Check(TokenType.Abstract) || Check(TokenType.Virtual) ||
                Check(TokenType.Override) || Check(TokenType.Sealed) || Check(TokenType.Delegate))
            {
                if (Peek().Type == TokenType.Native || Peek().Type == TokenType.Extern)
                {
                    _pendingNative = true;
                }
                Advance();
                // delegate 声明: delegate int Dlg(int x); → 跳过整个声明
                if (Previous().Type == TokenType.Delegate)
                {
                    // 跳过返回类型和名称
                    while (!Check(TokenType.LeftParen) && !Check(TokenType.Semicolon) && !IsAtEnd())
                        Advance();
                    if (Check(TokenType.LeftParen))
                    {
                        int depth = 1; Advance();
                        while (depth > 0 && !IsAtEnd())
                        {
                            if (Check(TokenType.LeftParen)) depth++;
                            else if (Check(TokenType.RightParen)) depth--;
                            Advance();
                        }
                    }
                    Expect(TokenType.Semicolon, "Expected ';' after delegate");
                    return null;
                }
                // 递归解析后面的声明
                return ParseStatement();
            }

            // enum 声明
            if (Match(TokenType.Enum))
            {
                return ParseEnumDeclaration();
            }

            // 方法声明或变量声明（以类型关键字开头）
            bool isTypeKw = Check(TokenType.Var) || Check(TokenType.Int) || Check(TokenType.String) || Check(TokenType.Bool) ||
                Check(TokenType.Float) || Check(TokenType.Double) || Check(TokenType.Long) || Check(TokenType.Char) ||
                Check(TokenType.Object) || Check(TokenType.Decimal) || Check(TokenType.Byte) || Check(TokenType.SByte) ||
                Check(TokenType.Short) || Check(TokenType.UShort) || Check(TokenType.UInt) || Check(TokenType.ULong) ||
                Check(TokenType.Void);

            if (isTypeKw)
            {
                // Peek: type NAME { → property declaration
                if (Peek(2)?.Type == TokenType.LeftBrace)
                {
                    _pendingNative = false; // native 不适用于属性
                    string propType = "";
                    if (Match(TokenType.Void)) propType = "void";
                    else if (Match(TokenType.Int)) propType = "int";
                    else if (Match(TokenType.String)) propType = "string";
                    else if (Match(TokenType.Bool)) propType = "bool";
                    else if (Match(TokenType.Float)) propType = "float";
                    else if (Match(TokenType.Double)) propType = "double";
                    else if (Match(TokenType.Char)) propType = "char";
                    else if (Match(TokenType.Long)) propType = "long";
                    else if (Match(TokenType.Object)) propType = "object";
                    else { Advance(); Advance(); return null; }
                    if (!Check(TokenType.Identifier)) { Advance(); Advance(); return null; }
                    string propName = tokens[position++].Value;
                    return ParsePropertyDeclaration(propType, propName);
                }

                // Peek: type NAME ( → method declaration
                // Peek: type NAME ... → variable declaration
                if (Peek(2)?.Type == TokenType.LeftParen)
                {
                    string retType = "";
                    if (Match(TokenType.Void)) retType = "void";
                    else if (Match(TokenType.Int)) retType = "int";
                    else if (Match(TokenType.String)) retType = "string";
                    else if (Match(TokenType.Bool)) retType = "bool";
                    else if (Match(TokenType.Float)) retType = "float";
                    else if (Match(TokenType.Double)) retType = "double";
                    else if (Match(TokenType.Char)) retType = "char";
                    else if (Match(TokenType.Long)) retType = "long";
                    else if (Match(TokenType.Object)) retType = "object";
                    else { Advance(); Advance(); return null; }
                    return ParseMethodDeclaration(retType);
                }
                // 变量声明需要消费类型关键字
                _pendingNative = false; // native 不适用于变量声明
                if (Match(TokenType.Var) || Match(TokenType.Int) || Match(TokenType.String) || Match(TokenType.Bool) ||
                    Match(TokenType.Float) || Match(TokenType.Double) || Match(TokenType.Long) || Match(TokenType.Char) ||
                    Match(TokenType.Object) || Match(TokenType.Decimal) || Match(TokenType.Byte) || Match(TokenType.SByte) ||
                    Match(TokenType.Short) || Match(TokenType.UShort) || Match(TokenType.UInt) || Match(TokenType.ULong) ||
                    Match(TokenType.Void))
                {
                    return ParseVariableDeclaration();
                }
            }
            
            // 标识符类型变量声明: List<int> list, String name, System.Collections.Generic.List<int> list, 等
            if (Check(TokenType.Identifier))
            {
                // 跳过Console.xxx → 由前面的Console.WriteLine处理
                if (Current?.Value == "Console" && Peek(1)?.Type == TokenType.Dot)
                {
                    // 由前面的Console.WriteLine逻辑处理
                }
                else
                {
                    // 检查是否可能是变量声明
                    int lookahead = 1;
                    bool hasDot = false;
                    while (Peek(lookahead)?.Type == TokenType.Dot || Peek(lookahead)?.Type == TokenType.Identifier)
                    {
                        if (Peek(lookahead)?.Type == TokenType.Dot) hasDot = true;
                        lookahead++;
                    }
                    // 只有 Identifier Identifier = 或 Identifier Identifier , 才认为是变量声明
                    // 单独的 Identifier = 是赋值表达式
                    // 带点号的(obj.prop = x)不匹配，避免误判成员访问为变量声明
                    if (Peek(lookahead)?.Type == TokenType.LessThan ||
                        (!hasDot && Peek(lookahead)?.Type == TokenType.Assignment && lookahead >= 2) ||
                        (!hasDot && Peek(lookahead)?.Type == TokenType.Comma && lookahead >= 2) ||
                        (Peek(lookahead)?.Type == TokenType.Identifier && lookahead > 0))
                    {
                        return ParseVariableDeclaration();
                    }
                }
            }
            
            // 返回语句
            if (Match(TokenType.Return))
            {
                return ParseReturnStatement();
            }

            // 构造函数声明: ClassName(...) { ... }
            if (_currentClassName != null && Check(TokenType.Identifier) && Peek().Value == _currentClassName && Peek(1)?.Type == TokenType.LeftParen)
            {
                return ParseConstructorDeclaration();
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

            // using 指令
            if (Match(TokenType.Using))
            {
                var nsParts = new List<string>();
                while (!Check(TokenType.Semicolon) && !IsAtEnd())
                {
                    if (Peek().Type == TokenType.Identifier)
                        nsParts.Add(Peek().Value);
                    Advance();
                }
                Expect(TokenType.Semicolon, "Expected ';' after using");
                if (nsParts.Count > 0)
                    _usingNamespaces.Add(string.Join(".", nsParts));
                return null;
            }

            // namespace 声明
            if (Match(TokenType.Namespace))
            {
                Advance(); // 跳过命名空间名
                Expect(TokenType.LeftBrace, "Expected '{'");
                while (!Check(TokenType.RightBrace) && !IsAtEnd())
                {
                    var nsStmt = ParseStatement();
                    if (nsStmt != null && nsStmt is ClassDeclaration)
                        return nsStmt;
                }
                Expect(TokenType.RightBrace, "Expected '}'");
                return null;
            }

            // 类声明
            if (Match(TokenType.Class))
            {
                return ParseClassDeclaration();
            }
            
            // 跳过未知的token
            Advance();
            return null;
        }
        
        private Statement ParseClassDeclaration()
        {
            string name = Peek()?.Value ?? "Unknown";
            Advance(); // class name
            // 跳过泛型类型参数: class Box<T, U>
            if (Match(TokenType.LessThan))
            {
                int genDepth = 1;
                while (genDepth > 0 && !IsAtEnd())
                {
                    if (Check(TokenType.LessThan)) genDepth++;
                    else if (Check(TokenType.GreaterThan)) genDepth--;
                    Advance();
                }
            }
            var cls = new ClassDeclaration(name);
            string prevClassName = _currentClassName;
            _currentClassName = name;
            Expect(TokenType.LeftBrace, "Expected '{'");
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var member = ParseStatement();
                if (member != null)
                    cls.Members.Add(member);
            }
            Expect(TokenType.RightBrace, "Expected '}'");
            _currentClassName = prevClassName;
            return cls;
        }

        private Statement ParseConstructorDeclaration()
        {
            string name = Advance().Value; // 类名
            Expect(TokenType.LeftParen, "Expected '('");
            var parameters = new List<Parameter>();
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    string paramType = tokens[position++].Value;
                    string paramName = tokens[position++].Value;
                    parameters.Add(new Parameter(paramType, paramName));
                } while (Match(TokenType.Comma));
            }
            Expect(TokenType.RightParen, "Expected ')'");
            var body = ParseBlock();
            return new ConstructorDeclaration(name, parameters, body);
        }

        private Statement ParsePropertyDeclaration(string propType, string propName)
        {
            Expect(TokenType.LeftBrace, "Expected '{'");
            bool hasGet = false, hasSet = false;
            if (Match(TokenType.Get)) { Expect(TokenType.Semicolon, ";"); hasGet = true; }
            if (Match(TokenType.Set)) { Expect(TokenType.Semicolon, ";"); hasSet = true; }
            Expect(TokenType.RightBrace, "Expected '}'");
            return new PropertyDeclaration(propType, propName, hasGet, hasSet);
        }

        private Statement ParseMethodDeclaration(string returnType)
        {
            bool isNative = _pendingNative;
            _pendingNative = false;

            if (!Check(TokenType.Identifier)) { Advance(); return null; }
            string name = tokens[position++].Value;
            Expect(TokenType.LeftParen, "Expected '('");
            var parameters = new List<Parameter>();
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    string paramType = ParseTypeName();
                    if (Check(TokenType.Identifier))
                    {
                        string paramName = tokens[position++].Value;
                        parameters.Add(new Parameter(paramType, paramName));
                    }
                } while (Match(TokenType.Comma));
            }
            Expect(TokenType.RightParen, "Expected ')'");

            // native 方法的 alias 子句
            string aliasName = null;
            if (isNative && Match(TokenType.Alias))
            {
                Expect(TokenType.StringLiteral, "Expected string literal after 'alias'");
                aliasName = Previous().Value;
            }

            Block body = null;
            if (isNative)
            {
                Expect(TokenType.Semicolon, "Expected ';' after native method declaration");
            }
            else if (Match(TokenType.Lambda))
            {
                // 表达式体方法: int Foo() => expr;
                var expr = ParseExpression();
                body = new Block();
                body.Statements.Add(new ReturnStatement(expr));
                Expect(TokenType.Semicolon, "Expected ';' after expression-bodied method");
            }
            else
            {
                body = ParseBlock();
            }
            return new MethodDeclStatement(returnType, name, parameters, body)
            {
                IsNative = isNative,
                AliasName = aliasName
            };
        }

        private string ParseTypeName()
        {
            string baseType;
            if (Match(TokenType.Int)) baseType = "int";
            else if (Match(TokenType.String)) baseType = "string";
            else if (Match(TokenType.Bool)) baseType = "bool";
            else if (Match(TokenType.Float)) baseType = "float";
            else if (Match(TokenType.Double)) baseType = "double";
            else if (Match(TokenType.Char)) baseType = "char";
            else if (Match(TokenType.Long)) baseType = "long";
            else if (Match(TokenType.Object)) baseType = "object";
            else if (Match(TokenType.Void)) baseType = "void";
            else if (Match(TokenType.Byte)) baseType = "byte";
            else if (Match(TokenType.SByte)) baseType = "sbyte";
            else if (Match(TokenType.Short)) baseType = "short";
            else if (Match(TokenType.UShort)) baseType = "ushort";
            else if (Match(TokenType.UInt)) baseType = "uint";
            else if (Match(TokenType.ULong)) baseType = "ulong";
            else if (Match(TokenType.Decimal)) baseType = "decimal";
            else if (Check(TokenType.Identifier))
                baseType = tokens[position++].Value;
            else
            {
                Expect(TokenType.Identifier, "Expected type name");
                return "int";
            }

            // 泛型: List<int>, Dictionary<string, int>
            if (Match(TokenType.LessThan))
            {
                baseType += "<";
                int depth = 1;
                while (depth > 0 && position < tokens.Count)
                {
                    if (tokens[position].Type == TokenType.GreaterThan) { depth--; if (depth > 0) baseType += ">"; }
                    else if (tokens[position].Type == TokenType.LessThan) { depth++; baseType += "<"; }
                    else baseType += tokens[position].Value;
                    position++;
                }
                if (depth == 0) baseType += ">";
            }

            // 可空类型: int?, long?
            if (Match(TokenType.Conditional))
            {
                baseType += "?";
            }

            // 数组类型: string[], int[,]
            while (Match(TokenType.LeftBracket))
            {
                baseType += "[";
                while (!Check(TokenType.RightBracket) && !IsAtEnd()) baseType += tokens[position++].Value;
                if (Match(TokenType.RightBracket)) baseType += "]";
            }

            return baseType;
        }

        private Block ParseBlock()
        {
            Expect(TokenType.LeftBrace, "Expected '{'");
            var block = new Block();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var stmt = ParseStatement();
                if (stmt != null)
                {
                    block.Statements.Add(stmt);
                    // Inject extra declarations from comma-separated multi-var decls
                    if (_extraDeclarations.Count > 0)
                    {
                        block.Statements.AddRange(_extraDeclarations);
                        _extraDeclarations.Clear();
                    }
                }
            }
            Expect(TokenType.RightBrace, "Expected '}'");
            return block;
        }

        private Statement ParseConsoleWriteLineStatement()
        {
            // Console 已被外层 Match 消费，当前 token 是 .
            
            // 跳过.
            if (Match(TokenType.Dot))
            {
                // 跳过WriteLine或Write
                bool hasNewLine = true;
                if (Match(TokenType.Identifier, "WriteLine"))
                {
                    hasNewLine = true;
                }
                else if (Match(TokenType.Identifier, "Write"))
                {
                    hasNewLine = false;
                }
                else
                {
                    return null;
                }
                
                // 解析参数
                var arguments = new List<Expression>();
                
                if (Match(TokenType.LeftParen))
                {
                    if (!Match(TokenType.RightParen))
                    {
                        do
                        {
                            var arg = ParseExpression();
                            if (arg != null)
                            {
                                arguments.Add(arg);
                            }
                        } while (Match(TokenType.Comma));
                        
                        Expect(TokenType.RightParen, "Expected ')' after arguments");
                    }
                }
                
                Expect(TokenType.Semicolon, "Expected ';' after Console statement");
                
                return new ConsoleWriteLineStatement(arguments) { HasNewLine = hasNewLine };
            }
            
            return null;
        }
        
        private Statement ParseIfStatement()
        {
            Expect(TokenType.LeftParen, "Expected '(' after 'if'");
            var condition = ParseExpression();
            Expect(TokenType.RightParen, "Expected ')' after if condition");
            
            var thenBranch = ParseStatement();
            
            Statement elseBranch = null;
            if (Match(TokenType.Else))
            {
                elseBranch = ParseStatement();
            }
            
            return new IfStatement(condition, thenBranch, elseBranch);
        }
        
        private Statement ParseWhileStatement()
        {
            Expect(TokenType.LeftParen, "Expected '(' after 'while'");
            var condition = ParseExpression();
            Expect(TokenType.RightParen, "Expected ')' after while condition");
            
            var body = ParseStatement();
            
            return new WhileStatement(condition, body);
        }
        
        private Statement ParseForStatement()
        {
            Expect(TokenType.LeftParen, "Expected '(' after 'for'");

            
            Statement initializer = null;
            if (!Match(TokenType.Semicolon))
            {
                // 检查是否是变量声明
                if (Match(TokenType.Var) || Match(TokenType.Int) || Match(TokenType.String) || Match(TokenType.Bool) ||
                    Match(TokenType.Float) || Match(TokenType.Double) || Match(TokenType.Long) || Match(TokenType.Char) ||
                    Match(TokenType.Object) || Match(TokenType.Decimal) || Match(TokenType.Byte) || Match(TokenType.SByte) ||
                    Match(TokenType.Short) || Match(TokenType.UShort) || Match(TokenType.UInt) || Match(TokenType.ULong))
                {
                    // 在for循环中，变量声明不应消耗分号，分号由for循环逻辑处理
                    var typeToken = Previous();
                    string type = typeToken.Value;
                    
                    // 处理数组类型：int[], string[]等
                    if (Match(TokenType.LeftBracket))
                    {
                        Expect(TokenType.RightBracket, "Expected ']' after array type");
                        type += "[]";
                    }
                    
                    Expect(TokenType.Identifier, "Expected variable name");
                    var nameToken = Previous();
                    string name = nameToken.Value;
                    
                    Expression initializerExpr = null;
                    
                    if (Match(TokenType.Assignment))
                    {
                        if (Match(TokenType.LeftBrace))
                        {
                            var elements = new List<Expression>();
                            if (!Match(TokenType.RightBrace))
                            {
                                do
                                {
                                    var element = ParseExpression();
                                    if (element != null) elements.Add(element);
                                } while (Match(TokenType.Comma));
                                Expect(TokenType.RightBrace, "Expected '}' after array literal");
                            }
                            initializerExpr = new ArrayLiteralExpression(elements);
                        }
                        else
                        {
                            initializerExpr = ParseExpression();
                        }
                    }
                    
                    initializer = new VariableDeclStatement(type, name, initializerExpr);
                }
                else
                {
                    var expr = ParseExpression();
                    if (expr != null)
                    {
                        initializer = new ExpressionStatement(expr);
                    }
                }
            }
            
            Expect(TokenType.Semicolon, "Expected ';' after for initializer");
            
            Expression condition = null;
            if (!Check(TokenType.Semicolon))
            {
                condition = ParseExpression();
            }
            
            Expect(TokenType.Semicolon, "Expected ';' after for condition");
            
            Expression increment = null;
            if (!Check(TokenType.RightParen))
            {
                increment = ParseExpression();
            }
            Expect(TokenType.RightParen, "Expected ')' after for increment");
            
            var body = ParseStatement();
            
            return new ForStatement(initializer, condition, increment, body);
        }
        
        private Statement ParseForEachStatement()
        {
            Expect(TokenType.LeftParen, "Expected '(' after 'foreach'");
            string varType = null;
            string varName = null;

            if (Match(TokenType.Var))
            {
                Expect(TokenType.Identifier, "Expected variable name after 'var'");
                varName = Previous().Value;
                varType = "var";
            }
            else if (Match(TokenType.Int) || Match(TokenType.String) || Match(TokenType.Bool) ||
                     Match(TokenType.Float) || Match(TokenType.Double) || Match(TokenType.Long) || Match(TokenType.Char) ||
                     Match(TokenType.Object) || Match(TokenType.Decimal) || Match(TokenType.Byte) || Match(TokenType.SByte) ||
                     Match(TokenType.Short) || Match(TokenType.UShort) || Match(TokenType.UInt) || Match(TokenType.ULong))
            {
                varType = Previous().Value;
                if (Match(TokenType.LeftBracket))
                {
                    Expect(TokenType.RightBracket, "Expected ']' after array type");
                    varType += "[]";
                }
                Expect(TokenType.Identifier, "Expected variable name");
                varName = Previous().Value;
            }
            else
            {
                Expect(TokenType.Identifier, "Expected variable type");
                varType = Previous().Value;
                Expect(TokenType.Identifier, "Expected variable name");
                varName = Previous().Value;
            }

            Expect(TokenType.In, "Expected 'in' in foreach statement");
            var collection = ParseExpression();
            Expect(TokenType.RightParen, "Expected ')' after foreach collection");
            var body = ParseStatement();
            return new ForEachStatement(varType, varName, collection, body);
        }

        private Statement ParseSwitchStatement()
        {
            Expect(TokenType.LeftParen, "Expected '(' after 'switch'");
            var value = ParseExpression();
            Expect(TokenType.RightParen, "Expected ')' after switch value");
            Expect(TokenType.LeftBrace, "Expected '{' to start switch block");

            var sw = new SwitchStatement(value);
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                if (Match(TokenType.Case))
                {
                    var caseVal = ParseExpression();
                    Expect(TokenType.Colon, "Expected ':' after case value");
                    var sc = new SwitchCase { Value = caseVal };
                    while (!Check(TokenType.RightBrace) && !Check(TokenType.Case) && !Check(TokenType.Default) && !IsAtEnd())
                    {
                        var stmt = ParseStatement();
                        if (stmt is BreakStatement) { sc.HasBreak = true; sc.Body.Add(stmt); break; }
                        sc.Body.Add(stmt);
                    }
                    sw.Cases.Add(sc);
                }
                else if (Match(TokenType.Default))
                {
                    Expect(TokenType.Colon, "Expected ':' after default");
                    var sc = new SwitchCase { Value = null };
                    while (!Check(TokenType.RightBrace) && !Check(TokenType.Case) && !Check(TokenType.Default) && !IsAtEnd())
                    {
                        var stmt = ParseStatement();
                        if (stmt is BreakStatement) { sc.HasBreak = true; sc.Body.Add(stmt); break; }
                        sc.Body.Add(stmt);
                    }
                    sw.Cases.Add(sc);
                }
                else break;
            }
            Expect(TokenType.RightBrace, "Expected '}' to end switch block");
            return sw;
        }

        private Statement ParseDoWhileStatement()
        {
            var body = ParseStatement();
            Expect(TokenType.While, "Expected 'while' after do body");
            Expect(TokenType.LeftParen, "Expected '(' after 'while'");
            var condition = ParseExpression();
            Expect(TokenType.RightParen, "Expected ')' after while condition");
            Match(TokenType.Semicolon);
            return new DoWhileStatement(body, condition);
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
                    Expect(TokenType.RightParen, "Expected ')' after catch parameters");
                }
                cc.Body = ParseStatement();
                ts.Catches.Add(cc);
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
            Expect(TokenType.Semicolon, "Expected ';' after throw");
            return new ThrowStatement(value);
        }

        private Statement ParseEnumDeclaration()
        {
            Expect(TokenType.Identifier, "Expected enum name");
            string name = Previous().Value;
            Expect(TokenType.LeftBrace, "Expected '{' after enum name");

            var members = new List<string>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                if (Check(TokenType.Identifier))
                {
                    members.Add(Advance().Value);
                    // 跳过可选的 = value
                    if (Check(TokenType.Assignment))
                    {
                        Advance(); // skip =
                        while (!Check(TokenType.Comma) && !Check(TokenType.RightBrace) && !IsAtEnd())
                            Advance();
                    }
                    Match(TokenType.Comma);
                }
                else break;
            }
            Expect(TokenType.RightBrace, "Expected '}' after enum members");
            Match(TokenType.Semicolon);

            return new EnumDeclStatement(name, members);
        }

        private Statement ParseVariableDeclaration()
        {
            // 类型尚未被消耗(从标识符路径来), 需要解析类型
            string type = "";
            Token prev = Previous();
            bool typeAlreadyConsumed = prev.Type == TokenType.Int || prev.Type == TokenType.String || 
                prev.Type == TokenType.Bool || prev.Type == TokenType.Float || prev.Type == TokenType.Double ||
                prev.Type == TokenType.Long || prev.Type == TokenType.Char || prev.Type == TokenType.Object ||
                prev.Type == TokenType.Void || prev.Type == TokenType.Var || prev.Type == TokenType.Byte ||
                prev.Type == TokenType.SByte || prev.Type == TokenType.Short || prev.Type == TokenType.UShort ||
                prev.Type == TokenType.UInt || prev.Type == TokenType.ULong || prev.Type == TokenType.Decimal;
            
            if (!typeAlreadyConsumed && Check(TokenType.Identifier))
            {
                // 限定名: System.Collections.Generic.List
                while (Check(TokenType.Identifier))
                {
                    type += tokens[position++].Value;
                    if (Match(TokenType.Dot))
                        type += ".";
                    else
                        break;
                }
                // 泛型: List<int>
                if (Match(TokenType.LessThan))
                {
                    type += "<";
                    int depth = 1;
                    while (depth > 0 && position < tokens.Count)
                    {
                        if (tokens[position].Type == TokenType.GreaterThan) { depth--; if (depth > 0) type += ">"; }
                        else if (tokens[position].Type == TokenType.LessThan) { depth++; type += "<"; }
                        else if (tokens[position].Type == TokenType.Dot) type += ".";
                        else type += tokens[position].Value;
                        position++;
                    }
                    if (depth == 0) type += ">";
                }
                // 可空: int?
                if (Match(TokenType.Conditional)) type += "?";
            }
            else
            {
                // 类型已被消耗(从关键字路径来, Previous()是类型token)
                var typeToken = Previous();
                type = typeToken.Value;
                // 可空: int?
                if (Match(TokenType.Conditional)) type += "?";
                // 数组
                if (Match(TokenType.LeftBracket))
                {
                    Expect(TokenType.RightBracket, "Expected ']' after array type");
                    type += "[]";
                }
            }
            
            // 处理数组类型：int[], string[]等
            if (Match(TokenType.LeftBracket))
            {
                Expect(TokenType.RightBracket, "Expected ']' after array type");
                type += "[]";
            }
            
            // 调试：打印当前token
            if (debugMode)
            {
                Console.WriteLine($"DEBUG ParseVariableDeclaration: type={type}, current={Current?.Type}:{Current?.Value}");
            }
            
            Expect(TokenType.Identifier, "Expected variable name");
            var nameToken = Previous();
            string name = nameToken.Value;
            
            Expression initializer = null;
            
            if (Match(TokenType.Assignment))
            {
                // 检查是否是数组字面量
                if (Match(TokenType.LeftBrace))
                {
                    // 解析数组字面量
                    var elements = new List<Expression>();
                    
                    if (!Match(TokenType.RightBrace))
                    {
                        do
                        {
                            var element = ParseExpression();
                            if (element != null)
                            {
                                elements.Add(element);
                            }
                        } while (Match(TokenType.Comma));
                        
                        Expect(TokenType.RightBrace, "Expected '}' after array literal");
                    }
                    
                    initializer = new ArrayLiteralExpression(elements);
                }
                else
                {
                    initializer = ParseExpression();
                    // Lambda表达式: Dlg d = x => x * 2;
                    if (Match(TokenType.Lambda))
                    {
                        initializer = ParseExpression();
                    }
                }
            }

            // Handle comma-separated multi-variable declarations: int a=1, b=2, r;
            if (Match(TokenType.Comma))
            {
                var stmts = new List<Statement>();
                stmts.Add(new VariableDeclStatement(type, name, initializer));
                do
                {
                    Expect(TokenType.Identifier, "Expected variable name after comma");
                    var nextName = Previous().Value;
                    Expression nextInit = null;
                    if (Match(TokenType.Assignment))
                        nextInit = ParseExpression();
                    stmts.Add(new VariableDeclStatement(type, nextName, nextInit));
                } while (Match(TokenType.Comma));
                Expect(TokenType.Semicolon, "Expected ';' after variable declaration");
                if (stmts.Count > 1)
                    _extraDeclarations.AddRange(stmts.Skip(1));
                return stmts[0];
            }
            
            Expect(TokenType.Semicolon, "Expected ';' after variable declaration");
            
            return new VariableDeclStatement(type, name, initializer);
        }
        
        private Statement ParseReturnStatement()
        {
            Expression value = null;
            
            if (!Check(TokenType.Semicolon))
            {
                value = ParseExpression();
            }
            
            Expect(TokenType.Semicolon, "Expected ';' after return statement");
            
            return new ReturnStatement(value);
        }
        
        private new bool Match(params TokenType[] types)
        {
            foreach (var type in types)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }
            
            return false;
        }
        
        private bool Match(TokenType type, string value)
        {
            if (Check(type) && Peek().Value == value)
            {
                Advance();
                return true;
            }
            
            return false;
        }
        
        private new bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            return Peek().Type == type;
        }
        
        private new Token Advance()
        {
            if (!IsAtEnd()) position++;
            return Previous();
        }
        
        private new bool IsAtEnd()
        {
            return Peek().Type == TokenType.EndOfFile;
        }
        
        private new Token Peek(int offset = 0)
        {
            int index = position + offset;
            if (index >= tokens.Count) return tokens[^1];
            return tokens[index];
        }
        
        private Token Current => Peek();
        
        private new Token Previous()
        {
            return tokens[position - 1];
        }
        
        private new void Expect(TokenType type, string message)
        {
            if (!Match(type))
            {
                // 简化错误处理
                throw Error(message);
            }
        }

        /// <summary>跳过表达式直到分隔符（用于MCU模式跳过OS特性）</summary>
        private void SkipExpression()
        {
            int depth = 0;
            while (!IsAtEnd())
            {
                var t = Peek().Type;
                if (t == TokenType.Semicolon && depth == 0) break;
                if (t == TokenType.RightParen && depth == 0) break;
                if (t == TokenType.Comma && depth == 0) break;
                if (t == TokenType.LeftParen) depth++;
                if (t == TokenType.RightParen) depth--;
                Advance();
            }
        }

        /// <summary>跳过代码块 { ... }（用于MCU模式跳过OS特性）</summary>
        private void SkipBlock()
        {
            if (Match(TokenType.LeftBrace))
            {
                int depth = 1;
                while (depth > 0 && !IsAtEnd())
                {
                    var t = Peek().Type;
                    if (t == TokenType.LeftBrace) depth++;
                    if (t == TokenType.RightBrace) depth--;
                    Advance();
                }
            }
        }
    }
}