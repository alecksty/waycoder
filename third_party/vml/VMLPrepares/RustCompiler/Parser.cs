using System;
using System.Collections.Generic;
using CompilerBase;

namespace RustCompiler
{
    /// <summary>
    /// Rust语法分析器
    /// </summary>
    public partial class Parser : ParserBase<Token, TokenType>
    {
        private int _variableCounter = 0;
        private int _functionCounter = 0;

        protected override TokenType GetTokenType(Token token) => token.Type;

        public Parser(List<Token> tokens) : base(tokens) { }
        
        /// <summary>
        /// 语法分析主方法
        /// </summary>
        public ProgramNode Parse()
        {
            var program = new ProgramNode();
            
            while (!IsAtEnd)
            {
                var statement = ParseStatement();
                if (statement != null)
                {
                    program.Statements.Add(statement);
                }
            }
            
            return program;
        }
        
        /// <summary>
        /// 解析语句
        /// </summary>
        private ASTNode ParseStatement()
        {
            if (Match(TokenType.FN))
            {
                // 消耗函数名
                string functionName = "main"; // 默认值
                if (Check(TokenType.IDENTIFIER))
                {
                    functionName = Expect(TokenType.IDENTIFIER, "期望函数名").Value;
                }
                return ParseFunction(functionName);
            }
            else if (Match(TokenType.STATIC))
            {
                return ParseStaticDeclaration();
            }
            else if (Match(TokenType.CONST))
            {
                return ParseConstantDeclaration();
            }
            else if (Match(TokenType.LET))
            {
                return ParseVariableDeclaration();
            }
            else if (Match(TokenType.IF))
            {
                return ParseIfStatement();
            }
            else if (Match(TokenType.WHILE))
            {
                return ParseWhileStatement();
            }
            else if (Match(TokenType.FOR))
            {
                return ParseForStatement();
            }
            else if (Match(TokenType.RETURN))
            {
                return ParseReturnStatement();
            }
            else if (Check(TokenType.IDENTIFIER) && IsAssignment())
            {
                return ParseAssignment();
            }
            else if (Match(TokenType.LOOP))
            {
                return ParseLoopStatement();
            }
            else if (Match(TokenType.BREAK))
            {
                Expect(TokenType.SEMICOLON, "期望 ';' 在 break 后");
                return new BreakStatementNode();
            }
            else if (Match(TokenType.CONTINUE))
            {
                Expect(TokenType.SEMICOLON, "期望 ';' 在 continue 后");
                return new ContinueStatementNode();
            }
            else if (Match(TokenType.MATCH))
            {
                return ParseMatchStatement();
            }
            else if (Match(TokenType.ENUM))
            {
                var node = ParseEnumDecl();
                Match(TokenType.SEMICOLON);
                return node;
            }
            else if (Match(TokenType.STRUCT))
            {
                return ParseStructDecl();
            }
            else if (Match(TokenType.TRAIT))
            {
                var node = ParseTraitDecl();
                Match(TokenType.SEMICOLON);
                return node;
            }
            else if (Match(TokenType.IMPL))
            {
                var node = ParseImplBlock();
                Match(TokenType.SEMICOLON);
                return node;
            }
            else if (Check(TokenType.HASH) && Peek(1)?.Type == TokenType.LBRACKET)
            {
                SkipAttribute();
                return ParseStatement();
            }
            else if (Match(TokenType.MOD))
            {
                SkipModule();
                return null;
            }
            else if (Match(TokenType.USE))
            {
                SkipUse();
                return null;
            }
            else if (Check(TokenType.IDENTIFIER) && Cur.Value == "unsafe")
            {
                Advance();
                Expect(TokenType.LBRACE, "期望 '{' 在 unsafe 块后");
                var block = new BlockNode();
                while (!Check(TokenType.RBRACE) && !IsAtEnd)
                    block.Statements.Add(ParseStatement());
                Expect(TokenType.RBRACE, "期望 '}' 在 unsafe 块后");
                return block;
            }
            else if (Check(TokenType.IDENTIFIER) && IsCompoundAssignment(Peek(1)?.Type))
            {
                return ParseCompoundAssignment();
            }
            else if (Check(TokenType.IDENTIFIER) && Peek(1)?.Type == TokenType.LPAREN)
            {
                return ParseExpressionStatement();
            }
            else if (Check(TokenType.IDENTIFIER) && Cur.Value == "println")
            {
                return ParsePrintlnStatement();
            }
            else if (Match(TokenType.SEMICOLON))
            {
                // 空语句 (独立的 ';')
                return new BlockNode(); // 返回空块表示无操作
            }
            else
            {
                return ParseExpressionStatement();
            }
        }
        
        /// <summary>
        /// 解析类型（包括基本类型和自定义类型）
        /// </summary>
        private string ParseType()
        {
            // 引用/借用类型: &str, &mut T
            if (Match(TokenType.AMPERSAND))
            {
                if (Match(TokenType.MUT)) return "&mut " + ParseType();
                return "&" + ParseType();
            }
            // 数组类型: [T; N] 或 [T]
            if (Match(TokenType.LBRACKET))
            {
                string elementType = ParseType();
                if (Match(TokenType.SEMICOLON))
                {
                    // 跳过长度（编译期常量，这里简化处理）
                    while (!Check(TokenType.RBRACKET) && !IsAtEnd) Advance();
                }
                Expect(TokenType.RBRACKET, "期望 ']'");
                return "[" + elementType + "]";
            }
            // 基本类型关键字
            if (Match(TokenType.I8) || Match(TokenType.I16) || Match(TokenType.I32) || 
                Match(TokenType.I64) || Match(TokenType.I128) || Match(TokenType.ISIZE) ||
                Match(TokenType.U8) || Match(TokenType.U16) || Match(TokenType.U32) || 
                Match(TokenType.U64) || Match(TokenType.U128) || Match(TokenType.USIZE) ||
                Match(TokenType.F32) || Match(TokenType.F64) || Match(TokenType.BOOL) ||
                Match(TokenType.CHAR) || Match(TokenType.STR))
            {
                return Previous().Value;
            }
            // 自定义类型（标识符），可能带泛型参数: Option<i32>, Result<T, E>
            else if (Check(TokenType.IDENTIFIER))
            {
                string name = Expect(TokenType.IDENTIFIER, "期望类型名").Value;
                if (Match(TokenType.LT))
                {
                    SkipGenericArgs();
                }
                return name;
            }
            else
            {
                // 如果没有类型注解，返回默认类型"i32"
                return "i32";
            }
        }

        /// <summary>
        /// 解析函数定义
        /// </summary>
        private FunctionNode ParseFunction(string functionName = null)
        {
            var function = new FunctionNode();
            
            // 跳过fn关键字（已经在ParseStatement中消费了）
            // 这里不需要再调用Advance()
            
            // 函数名
            if (!string.IsNullOrEmpty(functionName))
            {
                function.Name = functionName;
            }
            else if (Check(TokenType.IDENTIFIER))
            {
                function.Name = Expect(TokenType.IDENTIFIER, "期望函数名").Value;
            }
            else
            {
                // 如果没有函数名，使用默认名称
                function.Name = $"func_{_functionCounter++}";
            }
            
            // 跳过泛型参数: fn f<T>(x: T)
            if (Match(TokenType.LT)) { SkipGenericArgs(); }

            // 参数列表
            Expect(TokenType.LPAREN, "期望 '('");
            
            while (!Check(TokenType.RPAREN) && !IsAtEnd)
            {
                var param = new ParameterNode();
                
                if (Check(TokenType.IDENTIFIER))
                {
                    param.Name = Expect(TokenType.IDENTIFIER, "期望参数名").Value;
                }
                
                Expect(TokenType.COLON, "期望 ':' 在参数名后");
                param.Type = ParseType();
                
                function.Parameters.Add(param);
                
                if (!Match(TokenType.COMMA))
                {
                    break;
                }
            }
            
            Expect(TokenType.RPAREN, "期望 ')'");

            // 返回类型
            if (Match(TokenType.ARROW))
            {
                function.ReturnType = ParseType();
            }

            // where 子句: fn foo<T>(x: T) where T: Clone + Debug
            if (Match(TokenType.WHERE))
            {
                do {
                    ParseType(); // Type name
                    Expect(TokenType.COLON, "期望 ':' 在 where 子句中");
                    do {
                        ParseType(); // Trait bound
                    } while (Match(TokenType.PLUS));
                } while (Match(TokenType.COMMA));
            }

            // 函数体
            function.Body = ParseBlock();

            return function;
        }
        
        /// <summary>
        /// 解析变量声明
        /// </summary>
        private VariableDeclarationNode ParseVariableDeclaration()
        {
            var declaration = new VariableDeclarationNode();
            
            // 注意：let关键字已经在ParseStatement中被Match(TokenType.LET)消费了
            // 所以这里不需要再调用Advance()
            
            // 检查是否可变
            if (Match(TokenType.MUT))
            {
                declaration.IsMutable = true;
            }
            
            // 变量名
            if (Check(TokenType.IDENTIFIER))
            {
                declaration.Name = Expect(TokenType.IDENTIFIER, "期望变量名").Value;
            }
            else
            {
                // 如果没有变量名，使用默认名称
                declaration.Name = $"var_{_variableCounter++}";
            }
            
            // 类型注解
            if (Match(TokenType.COLON))
            {
                declaration.Type = ParseType();
            }
            else
            {
                // 如果没有类型注解，设置默认类型
                declaration.Type = "i32";
            }
            
            // 初始化表达式
            if (Match(TokenType.EQ))
            {
                declaration.Initializer = ParseExpression();
            }
            
            Expect(TokenType.SEMICOLON, "期望 ';'");
            
            return declaration;
        }
        
        /// <summary>
        /// 解析常量声明
        /// </summary>
        private ConstantDeclarationNode ParseConstantDeclaration()
        {
            var declaration = new ConstantDeclarationNode();

            // 注意：const关键字已经在ParseStatement中被Match(TokenType.CONST)消费了
            // 所以这里不需要再调用Advance()

            // 常量名
            if (Check(TokenType.IDENTIFIER))
            {
                declaration.Name = Expect(TokenType.IDENTIFIER, "期望常量名").Value;
            }
            else
            {
                // 如果没有常量名，使用默认名称
                declaration.Name = $"const_{_variableCounter++}";
            }

            // 类型注解
            if (Match(TokenType.COLON))
            {
                declaration.Type = ParseType();
            }
            else
            {
                // 如果没有类型注解，设置默认类型
                declaration.Type = "i32";
            }

            // 初始化表达式（常量必须有初始值）
            Expect(TokenType.EQ, "期望 '=' 用于常量初始化");
            declaration.Initializer = ParseExpression();

            Expect(TokenType.SEMICOLON, "期望 ';'");

            return declaration;
        }

        /// <summary>解析 static 声明: static mut NAME: TYPE = VALUE;</summary>
        private ASTNode ParseStaticDeclaration()
        {
            // static mut NAME: TYPE = VALUE;
            bool isMut = Match(TokenType.MUT);
            string name = Expect(TokenType.IDENTIFIER, "期望静态变量名").Value;

            Expect(TokenType.COLON, "期望 ':'");
            string type = ParseType();

            Expect(TokenType.EQ, "期望 '=' 用于静态初始化");
            var initializer = ParseExpression();

            Expect(TokenType.SEMICOLON, "期望 ';'");

            // 映射为 VariableDeclarationNode（全局变量存储在数据段）
            return new VariableDeclarationNode
            {
                Name = name,
                Type = type,
                IsMutable = isMut,
                Initializer = initializer
            };
        }
        
        /// <summary>
        /// 解析赋值语句
        /// </summary>
        private AssignmentNode ParseAssignment()
        {
            var assignment = new AssignmentNode();

            // 变量名
            if (Check(TokenType.IDENTIFIER))
            {
                assignment.VariableName = Expect(TokenType.IDENTIFIER, "期望变量名").Value;
            }

            // 成员访问链: p.x = ... 或 p.x.y = ...
            if (Check(TokenType.DOT))
            {
                ASTNode target = new IdentifierNode { Name = assignment.VariableName };
                while (Check(TokenType.DOT))
                {
                    Advance(); // .
                    string member = Expect(TokenType.IDENTIFIER, "期望字段名").Value;
                    target = new MemberAccessNode { Target = target, Member = member };
                }
                assignment.Target = target;
            }

            // 等号
            Expect(TokenType.EQ, "期望 '='");

            // 值
            assignment.Value = ParseExpression();

            if (!Check(TokenType.RBRACE))
                Expect(TokenType.SEMICOLON, "期望 ';'");

            return assignment;
        }
        
        /// <summary>
        /// 检查是否是复合赋值运算符
        /// </summary>
        private bool IsAssignment()
        {
            // Lookahead: IDENTIFIER (. IDENTIFIER)* = ...
            int savedPos = _pos;
            try
            {
                Advance(); // skip IDENTIFIER
                while (Check(TokenType.DOT))
                {
                    Advance(); // skip .
                    if (!Check(TokenType.IDENTIFIER)) return false;
                    Advance(); // skip IDENTIFIER
                }
                return Check(TokenType.EQ);
            }
            finally
            {
                _pos = savedPos;
            }
        }

        private bool IsCompoundAssignment(TokenType? type)
        {
            return type == TokenType.PLUSEQ || type == TokenType.MINUSEQ || 
                   type == TokenType.STAREQ || type == TokenType.SLASHEQ || 
                   type == TokenType.PERCENTEQ;
        }
        
        /// <summary>
        /// 解析复合赋值语句 (+=, -=, *=, /=, %=)
        /// </summary>
        private CompoundAssignmentNode ParseCompoundAssignment()
        {
            var compound = new CompoundAssignmentNode();
            
            // 变量名
            if (Check(TokenType.IDENTIFIER))
            {
                compound.VariableName = Expect(TokenType.IDENTIFIER, "期望变量名").Value;
            }
            
            // 复合赋值运算符
            if (Match(TokenType.PLUSEQ))
                compound.Operator = "+=";
            else if (Match(TokenType.MINUSEQ))
                compound.Operator = "-=";
            else if (Match(TokenType.STAREQ))
                compound.Operator = "*=";
            else if (Match(TokenType.SLASHEQ))
                compound.Operator = "/=";
            else if (Match(TokenType.PERCENTEQ))
                compound.Operator = "%=";
            else
                throw new ParseException("期望复合赋值运算符");
            
            // 值
            compound.Value = ParseExpression();

            if (!Check(TokenType.RBRACE))
                Expect(TokenType.SEMICOLON, "期望 ';'");

            return compound;
        }
        
        /// <summary>
        /// 解析if语句
        /// </summary>
        private ASTNode ParseIfStatement()
        {
            // if let Pattern = Expr { ... }
            if (Match(TokenType.LET))
            {
                var pattern = ParseMatchPattern();
                Expect(TokenType.EQ, "期望 '='");
                var value = ParseExpression();
                var thenBlock = ParseBlock();
                ASTNode? elseBlock = null;
                if (Match(TokenType.ELSE))
                {
                    if (Match(TokenType.IF))
                        elseBlock = ParseIfStatement();
                    else
                        elseBlock = ParseBlock();
                }
                return new IfLetStatementNode { Pattern = pattern, Value = value, ThenBlock = thenBlock, ElseBlock = elseBlock };
            }

            var ifStatement = new IfStatementNode();

            // 条件
            ifStatement.Condition = ParseExpression();

            // then块
            ifStatement.ThenBlock = ParseBlock();

            // else块
            if (Match(TokenType.ELSE))
            {
                if (Match(TokenType.IF))
                {
                    ifStatement.ElseBlock = ParseIfStatement();
                }
                else
                {
                    ifStatement.ElseBlock = ParseBlock();
                }
            }

            return ifStatement;
        }
        
        /// <summary>
        /// 解析while语句
        /// </summary>
        private WhileStatementNode ParseWhileStatement()
        {
            var whileStatement = new WhileStatementNode();
            
            // 注意：while关键字已经在ParseStatement中被Match(TokenType.WHILE)消费了
            // 所以这里不需要再调用Advance()
            
            // 条件
            whileStatement.Condition = ParseExpression();
            
            // 循环体
            whileStatement.Body = ParseBlock();
            
            return whileStatement;
        }
        
        /// <summary>
        /// 解析for语句
        /// </summary>
        private ForStatementNode ParseForStatement()
        {
            var forStatement = new ForStatementNode();
            if (Check(TokenType.IDENTIFIER))
            {
                forStatement.VariableName = Expect(TokenType.IDENTIFIER, "期望变量名").Value;
            }
            Expect(TokenType.IDENTIFIER, "期望 'in'");

            // 先解析表达式
            forStatement.RangeStart = ParseExpression();

            // 检查是否是范围: 有 .. 或 ..= 则是范围，否则是集合迭代
            if (Match(TokenType.RANGE))
            {
                forStatement.Inclusive = false;
                forStatement.RangeEnd = ParseExpression();
            }
            else if (Match(TokenType.RANGE_INCLUSIVE))
            {
                forStatement.Inclusive = true;
                forStatement.RangeEnd = ParseExpression();
            }
            else
            {
                // 集合迭代: 将整个表达式作为集合，RangeEnd = null 表示集合模式
                forStatement.RangeEnd = null;
            }

            forStatement.Body = ParseBlock();
            return forStatement;
        }
        
        /// <summary>
        /// 解析return语句
        /// </summary>
        private ReturnStatementNode ParseReturnStatement()
        {
            var returnStatement = new ReturnStatementNode();
            
            // 注意：return关键字已经在ParseStatement中被Match(TokenType.RETURN)消费了
            // 所以这里不需要再调用Advance()
            
            // 返回值
            if (!Check(TokenType.SEMICOLON))
            {
                returnStatement.Value = ParseExpression();
            }
            
            Expect(TokenType.SEMICOLON, "期望 ';'");
            
            return returnStatement;
        }
        
        /// <summary>
        /// 解析表达式语句
        /// </summary>
        private ExpressionStatementNode ParseExpressionStatement()
        {
            var exprStatement = new ExpressionStatementNode();
            exprStatement.Expression = ParseExpression();
            
            // 在Rust中，函数体中的最后一个表达式是隐式返回值，不需要分号
            // 只有在不是块的最后一条语句时才需要分号
            if (!Check(TokenType.RBRACE))
            {
                Expect(TokenType.SEMICOLON, "期望 ';'");
            }
            
            return exprStatement;
        }
        
        /// <summary>
        /// 解析println!宏语句
        /// </summary>
        private PrintlnStatementNode ParsePrintlnStatement()
        {
            var printlnStatement = new PrintlnStatementNode();
            
            // 跳过println标识符
            Advance();
            
            // 检查宏调用符号（在词法分析器中是BANG）
            if (Match(TokenType.BANG))
            {
                Expect(TokenType.LPAREN, "期望 '('");
                
                // 解析参数
                while (!Check(TokenType.RPAREN) && !IsAtEnd)
                {
                    printlnStatement.Arguments.Add(ParseExpression());
                    
                    if (!Match(TokenType.COMMA))
                    {
                        break;
                    }
                }
                
                Expect(TokenType.RPAREN, "期望 ')'");
                Match(TokenType.SEMICOLON);
            }
            
            return printlnStatement;
        }
        
        private LoopStatementNode ParseLoopStatement()
        {
            var body = ParseBlock();
            return new LoopStatementNode { Body = body };
        }

        private MatchStatementNode ParseMatchStatement()
        {
            var value = ParseExpression();
            Expect(TokenType.LBRACE, "期望 '{' 在 match 后");
            var match = new MatchStatementNode { Value = value };
            while (!Check(TokenType.RBRACE) && !IsAtEnd)
            {
                ASTNode pattern = ParseMatchPattern();
                // OR patterns: pat1 | pat2 | pat3 => body
                List<ASTNode> altPatterns = new() { pattern };
                while (Match(TokenType.PIPE))
                    altPatterns.Add(ParseMatchPattern());
                Expect(TokenType.FAT_ARROW, "期望 '=>' 在 match arm 后");
                ASTNode body;
                if (Check(TokenType.LBRACE))
                    body = ParseBlock();
                else
                    body = ParseExpression();
                foreach (var pat in altPatterns)
                    match.Arms.Add(new MatchArm { Pattern = pat, Body = body });
                Match(TokenType.COMMA);
            }
            Expect(TokenType.RBRACE, "期望 '}' 在 match 后");
            return match;
        }

        private ASTNode ParseMatchPattern()
        {
            if (Match(TokenType.UNDERSCORE))
                return new IdentifierNode { Name = "_" };
            if (Check(TokenType.IDENTIFIER))
            {
                string name = Expect(TokenType.IDENTIFIER, "期望变体名").Value;
                // Qualified path: EnumName::VariantName(...)
                if (Check(TokenType.COLON) && Peek(1)?.Type == TokenType.COLON)
                {
                    Advance(); Advance(); // skip ::
                    string variantName = Expect(TokenType.IDENTIFIER, "期望变体名").Value;
                    string fullName = $"{name}_{variantName}";
                    if (Match(TokenType.LPAREN))
                    {
                        string bindName = Expect(TokenType.IDENTIFIER, "期望绑定变量名").Value;
                        Expect(TokenType.RPAREN, "期望 ')'");
                        return new EnumPatternNode { VariantName = fullName, BindName = bindName };
                    }
                    // Unit variant without parens
                    return new EnumPatternNode { VariantName = fullName };
                }
                // Simple variant: VariantName(bind)
                if (Match(TokenType.LPAREN))
                {
                    string bindName = Expect(TokenType.IDENTIFIER, "期望绑定变量名").Value;
                    Expect(TokenType.RPAREN, "期望 ')'");
                    return new EnumPatternNode { VariantName = name, BindName = bindName };
                }
                // Unit variant or literal identifier
                return new IdentifierNode { Name = name };
            }
            if (Check(TokenType.LPAREN))
            {
                Advance(); // consume (
                var tuple = new TupleExprNode();
                while (!Check(TokenType.RPAREN) && !IsAtEnd)
                {
                    tuple.Elements.Add(ParseMatchPattern());
                    if (!Match(TokenType.COMMA)) break;
                }
                Expect(TokenType.RPAREN, "期望 ')'");
                return tuple;
            }
            return ParsePrimary();
        }

        private StructDeclNode ParseStructDecl()
        {
            string name = Expect(TokenType.IDENTIFIER, "期望结构体名").Value;
            var sd = new StructDeclNode { Name = name };
            if (Match(TokenType.LBRACE))
            {
                while (!Check(TokenType.RBRACE) && !IsAtEnd)
                {
                    string fieldName = Expect(TokenType.IDENTIFIER, "期望字段名").Value;
                    Expect(TokenType.COLON, "期望 ':'");
                    string fieldType = ParseType();
                    sd.Fields.Add(new StructField { Name = fieldName, Type = fieldType });
                    Match(TokenType.COMMA);
                }
                Expect(TokenType.RBRACE, "期望 '}' 在 struct 后");
            }
            else
            {
                Match(TokenType.SEMICOLON);
            }
            return sd;
        }

        private EnumDeclNode ParseEnumDecl()
        {
            string name = Expect(TokenType.IDENTIFIER, "期望枚举名").Value;
            var ed = new EnumDeclNode { Name = name };
            // Skip generic args: enum Option<T> { ... }
            if (Match(TokenType.LT)) { SkipGenericArgs(); }
            Expect(TokenType.LBRACE, "期望 '{' 在 enum 后");
            while (!Check(TokenType.RBRACE) && !IsAtEnd)
            {
                string variant = Expect(TokenType.IDENTIFIER, "期望枚举变体名").Value;
                string? payloadType = null;
                if (Match(TokenType.LPAREN))
                {
                    payloadType = ParseType();
                    Expect(TokenType.RPAREN, "期望 ')' 在枚举变体后");
                }
                ed.Variants.Add(new EnumVariant { Name = variant, PayloadType = payloadType });
                Match(TokenType.COMMA);
            }
            Expect(TokenType.RBRACE, "期望 '}' 在 enum 后");
            Match(TokenType.SEMICOLON);
            return ed;
        }

        private TraitDeclNode ParseTraitDecl()
        {
            string name = Expect(TokenType.IDENTIFIER, "期望 trait 名").Value;
            var td = new TraitDeclNode { Name = name };
            Expect(TokenType.LBRACE, "期望 '{' 在 trait 后");
            while (!Check(TokenType.RBRACE) && !IsAtEnd)
            {
                if (Match(TokenType.FN))
                {
                    string methodName = Expect(TokenType.IDENTIFIER, "期望方法名").Value;
                    Expect(TokenType.LPAREN, "期望 '('");
                    var tm = new TraitMethod { Name = methodName };
                    if (!Check(TokenType.RPAREN))
                    {
                        do
                        {
                            string pn = Expect(TokenType.IDENTIFIER, "期望参数名").Value;
                            Expect(TokenType.COLON, "期望 ':'");
                            string pt = ParseType();
                            tm.Parameters.Add(new ParameterNode { Name = pn, Type = pt });
                        } while (Match(TokenType.COMMA));
                    }
                    Expect(TokenType.RPAREN, "期望 ')'");
                    if (Match(TokenType.ARROW)) tm.ReturnType = ParseType();
                    Expect(TokenType.SEMICOLON, "期望 ';'");
                    td.Methods.Add(tm);
                }
                else break;
            }
            Expect(TokenType.RBRACE, "期望 '}' 在 trait 后");
            return td;
        }

        private ImplBlockNode ParseImplBlock()
        {
            string first = Expect(TokenType.IDENTIFIER, "期望类型名").Value;
            string? traitName = null;
            string structName = first;
            if (Match(TokenType.FOR))
            {
                traitName = first;
                structName = Expect(TokenType.IDENTIFIER, "期望结构体名").Value;
            }
            var impl = new ImplBlockNode { StructName = structName, TraitName = traitName };
            Expect(TokenType.LBRACE, "期望 '{' 在 impl 后");
            while (!Check(TokenType.RBRACE) && !IsAtEnd)
            {
                if (Match(TokenType.FN))
                {
                    string fnName = Expect(TokenType.IDENTIFIER, "期望方法名").Value;
                    var func = ParseMethod(fnName);
                    impl.Methods.Add(func);
                }
                else break;
            }
            Expect(TokenType.RBRACE, "期望 '}' 在 impl 后");
            return impl;
        }

        private FunctionNode ParseMethod(string name)
        {
            Expect(TokenType.LPAREN, "期望 '('");
            var func = new FunctionNode { Name = name };
            if (!Check(TokenType.RPAREN))
            {
                do
                {
                    string paramName;
                    if (Match(TokenType.SELF))
                        paramName = "self";
                    else if (Check(TokenType.AMPERSAND) && Peek(1)?.Type == TokenType.SELF)
                    {
                        Advance(); Advance(); // skip &self
                        paramName = "self";
                    }
                    else
                        paramName = Expect(TokenType.IDENTIFIER, "期望参数名").Value;
                    Expect(TokenType.COLON, "期望 ':'");
                    string paramType = ParseType();
                    func.Parameters.Add(new ParameterNode { Name = paramName, Type = paramType });
                } while (Match(TokenType.COMMA));
            }
            Expect(TokenType.RPAREN, "期望 ')'");
            if (Match(TokenType.ARROW))
            {
                func.ReturnType = ParseType();
            }
            func.Body = ParseBlock();
            return func;
        }

        /// <summary>
        /// 解析代码块
        /// </summary>
        private BlockNode ParseBlock()
        {
            var block = new BlockNode();
            
            Expect(TokenType.LBRACE, "期望 '{'");
            
            while (!Check(TokenType.RBRACE) && !IsAtEnd)
            {
                var statement = ParseStatement();
                if (statement != null)
                {
                    block.Statements.Add(statement);
                }
            }
            
            Expect(TokenType.RBRACE, "期望 '}'");
            
            return block;
        }
        
        /// <summary>
        /// 解析表达式
        /// </summary>
        private void SkipGenericArgs()
        {
            int depth = 1;
            while (depth > 0 && !IsAtEnd)
            {
                if (Check(TokenType.GTEQ) || Check(TokenType.SHR))
                {
                    depth = 0; // >= or >> closes the generic at this level
                    break;
                }
                else if (Match(TokenType.LT)) depth++;
                else if (Match(TokenType.GT)) depth--;
                else Advance();
            }
        }

        private void SkipAttribute()
        {
            Advance(); // skip #
            if (Match(TokenType.LBRACKET))
            {
                int depth = 1;
                while (depth > 0 && !IsAtEnd)
                {
                    if (Match(TokenType.LBRACKET)) depth++;
                    else if (Match(TokenType.RBRACKET)) depth--;
                    else Advance();
                }
            }
        }

        private void SkipModule()
        {
            Expect(TokenType.IDENTIFIER, "期望模块名");
            if (Match(TokenType.LBRACE))
            {
                int depth = 1;
                while (depth > 0 && !IsAtEnd)
                {
                    if (Match(TokenType.LBRACE)) depth++;
                    else if (Match(TokenType.RBRACE)) depth--;
                    else Advance();
                }
            }
            else
                Match(TokenType.SEMICOLON);
        }

        private void SkipUse()
        {
            while (!Check(TokenType.SEMICOLON) && !IsAtEnd) Advance();
            Match(TokenType.SEMICOLON);
        }
    }
}