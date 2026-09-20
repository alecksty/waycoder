using CompilerBase;

namespace LuaCompiler
{
    /// <summary>
    /// Lua 语法分析器（简化版本）
    /// </summary>
    public class Parser : ParserBase<Token, TokenType>
    {
        private int anonymousFunctionCounter;

        protected override TokenType GetTokenType(Token token) => token.Type;

        /// <summary>
        /// 表达式**收尾符** —— 永远不会是表达式的开头（`GapAnchor()` 的第一个判据）。
        ///
        /// ⚠ Lua **没有 `NEWLINE` 这个 token**（语句靠空白分隔，换行不是词法单元），
        /// 所以 `x = 1 +` 结尾撞上的是 **`EOF`** —— `EOF` 必须算进来，
        /// 否则这条判据在最常见的形态上不生效。
        /// </summary>
        protected override bool IsExpressionCloser(Token token) => token.Type
            is TokenType.RPAREN or TokenType.RBRACE or TokenType.RBRACKET
            or TokenType.COMMA or TokenType.SEMICOLON or TokenType.COLON
            or TokenType.EOF;

        /// <summary>语句分隔 —— Lua 只有 `;`（换行不是 token）。</summary>
        protected override bool IsStatementSeparator(Token token) => token.Type
            is TokenType.SEMICOLON;

        public Parser(List<Token> tokens) : base(tokens)
        {
            anonymousFunctionCounter = 0;
        }
        
        public ProgramNode Parse()
        {
            var statements = new List<ASTNode>();

            while (GetTokenType(Cur) != TokenType.EOF)
            {
                statements.Add(ParseStatement());
            }

            return new ProgramNode(statements, 1, 1);
        }
        
        private ASTNode ParseStatement()
        {
            // Lua 可选分号 — 空语句用 nil 常量表示
            if (Match(TokenType.SEMICOLON))
                return new ConstantNode(null, "nil", Cur.Line, Cur.Column);

            // 检查函数定义
            if (GetTokenType(Cur) == TokenType.FUNCTION)
            {
                return ParseFunctionDefinition();
            }
            
            // 检查控制流语句
            if (GetTokenType(Cur) == TokenType.IF)
            {
                return ParseIfStatement();
            }
            
            if (GetTokenType(Cur) == TokenType.WHILE)
            {
                return ParseWhileStatement();
            }
            
            if (GetTokenType(Cur) == TokenType.REPEAT)
            {
                return ParseRepeatStatement();
            }
            
            if (GetTokenType(Cur) == TokenType.FOR)
            {
                return ParseForStatement();
            }
            
            if (GetTokenType(Cur) == TokenType.RETURN)
            {
                return ParseReturnStatement();
            }
            
            if (GetTokenType(Cur) == TokenType.BREAK)
            {
                var token = Advance();
                return new BreakStatementNode(token.Line, token.Column);
            }
            
            // goto 语句
            if (GetTokenType(Cur) == TokenType.GOTO)
            {
                var token = Advance();
                var labelName = Expect(TokenType.IDENTIFIER, "期望标签名").Value;
                return new GotoStatementNode(labelName, token.Line, token.Column);
            }
            
            // 标签声明 ::label::
            if (GetTokenType(Cur) == TokenType.DOUBLE_COLON)
            {
                Advance();
                var labelName = Expect(TokenType.IDENTIFIER, "期望标签名").Value;
                Expect(TokenType.DOUBLE_COLON, "期望 '::'");
                return new LabelStatementNode(labelName, Cur.Line, Cur.Column);
            }
            
            if (Match(TokenType.LOCAL))
            {
                if (GetTokenType(Cur) == TokenType.FUNCTION)
                {
                    return ParseFunctionDefinition(true);
                }
                return ParseLocalDeclaration();
            }
            
            // 尝试解析赋值 (包括多变量: a, b = 1, 2)
            var variable = ParseExpression();
            if (GetTokenType(Cur) == TokenType.COMMA || GetTokenType(Cur) == TokenType.ASSIGN)
            {
                return ParseAssignment(variable);
            }
            
            // 如果不是赋值，回退并作为表达式语句处理
            return variable;
        }
        
        private ASTNode ParseLocalDeclaration()
        {
            int line = Cur.Line;
            int column = Cur.Column;
             
            var names = new List<string>();
            names.Add(Expect(TokenType.IDENTIFIER, "期望变量名").Value);
            while (Match(TokenType.COMMA))
            {
                names.Add(Expect(TokenType.IDENTIFIER, "期望变量名").Value);
            }
             
            var values = new List<ASTNode>();
            if (Match(TokenType.ASSIGN))
            {
                values.Add(ParseExpression());
                while (Match(TokenType.COMMA))
                {
                    values.Add(ParseExpression());
                }
            }
            else
            {
                foreach (var _ in names)
                {
                    values.Add(new ConstantNode(null, "nil", line, column));
                }
            }
            
            return new VariableDeclarationNode(names, values, true, line, column);
        }
        
        private ASTNode ParseAssignment(ASTNode variable)
        {
            int line = variable.Line;
            int column = variable.Column;
            
            var variables = new List<ASTNode> { variable };
            while (Match(TokenType.COMMA))
            {
                variables.Add(ParseExpression());
            }

            Match(TokenType.ASSIGN); // consume = (may already be consumed by caller)
            var values = new List<ASTNode> { ParseExpression() };
            while (Match(TokenType.COMMA))
            {
                values.Add(ParseExpression());
            }
            
            return new AssignmentNode(variables, values, line, column);
        }
        
        private ASTNode ParseExpression()
        {
            return ParseLogicalOr();
        }
        
        private ASTNode ParseLogicalOr()
        {
            var node = ParseLogicalAnd();
            
            while (GetTokenType(Cur) == TokenType.OR)
            {
                var op = Advance();
                var right = ParseLogicalAnd();
                node = new BinaryOperationNode(node, op.Type, right, op.Line, op.Column);
            }
            
            return node;
        }
        
        private ASTNode ParseLogicalAnd()
        {
            var node = ParseUnary();
            
            while (GetTokenType(Cur) == TokenType.AND)
            {
                var op = Advance();
                var right = ParseUnary();
                node = new BinaryOperationNode(node, op.Type, right, op.Line, op.Column);
            }
            
            return node;
        }
        
        private ASTNode ParseUnary()
        {
            if (GetTokenType(Cur) == TokenType.NOT)
            {
                var op = Advance();
                var right = ParseUnary();
                return new UnaryOperationNode(op.Type, right, op.Line, op.Column);
            }
            // 一元负号: -expr
            if (GetTokenType(Cur) == TokenType.MINUS)
            {
                var op = Advance();
                var right = ParseUnary();
                return new UnaryOperationNode(op.Type, right, op.Line, op.Column);
            }
            if (GetTokenType(Cur) == TokenType.LEN)
            {
                var op = Advance();
                var right = ParseUnary();
                return new UnaryOperationNode(op.Type, right, op.Line, op.Column);
            }
             
            return ParseRelational();
        }
        
        private ASTNode ParseRelational()
        {
            var node = ParseAdditive();
            
            while (GetTokenType(Cur) == TokenType.EQ || GetTokenType(Cur) == TokenType.NE ||
                   GetTokenType(Cur) == TokenType.LT || GetTokenType(Cur) == TokenType.LE ||
                   GetTokenType(Cur) == TokenType.GT || GetTokenType(Cur) == TokenType.GE)
            {
                var op = Advance();
                var right = ParseAdditive();
                node = new BinaryOperationNode(node, op.Type, right, op.Line, op.Column);
            }
            
            return node;
        }
        

        
        private ASTNode ParseAdditive()
        {
            var node = ParseMultiplicative();
            
            while (GetTokenType(Cur) == TokenType.PLUS || GetTokenType(Cur) == TokenType.MINUS ||
                   GetTokenType(Cur) == TokenType.CONCAT)
            {
                var op = Advance();
                var right = ParseMultiplicative();
                node = new BinaryOperationNode(node, op.Type, right, op.Line, op.Column);
            }
            
            return node;
        }
        
        private ASTNode ParseMultiplicative()
        {
            var node = ParsePrimary();
            
            while (GetTokenType(Cur) == TokenType.MUL || GetTokenType(Cur) == TokenType.DIV ||
                   GetTokenType(Cur) == TokenType.FLOOR_DIV ||
                   GetTokenType(Cur) == TokenType.MOD || GetTokenType(Cur) == TokenType.POW)
            {
                var op = Advance();
                var right = ParsePrimary();
                node = new BinaryOperationNode(node, op.Type, right, op.Line, op.Column);
            }
            
            return node;
        }
        
        private ASTNode ParseFunctionDefinition(bool isLocal = false)
        {
            int line = Cur.Line;
            int column = Cur.Column;
            
            Expect(TokenType.FUNCTION, "期望 'function'");
            
            string functionName = ParseFunctionName(out bool hasImplicitSelf);
            
            // 参数列表
            Expect(TokenType.LPAREN, "期望 '('");
            var parameters = new List<string>();
            
            if (hasImplicitSelf)
            {
                parameters.Add("self");
            }

            if (GetTokenType(Cur) != TokenType.RPAREN)
            {
                parameters.Add(Expect(TokenType.IDENTIFIER, "期望参数名").Value);
                
                while (Match(TokenType.COMMA))
                {
                    parameters.Add(Expect(TokenType.IDENTIFIER, "期望参数名").Value);
                }
            }
            
            Expect(TokenType.RPAREN, "期望 ')'");
            
            // 函数体
            var body = new List<ASTNode>();
            while (GetTokenType(Cur) != TokenType.END && GetTokenType(Cur) != TokenType.EOF)
            {
                body.Add(ParseStatement());
            }
            
            Expect(TokenType.END, "期望 'end'");
            
            return new FunctionDefinitionNode(functionName, parameters, body, isLocal, line, column);
        }

        private string ParseFunctionName(out bool hasImplicitSelf)
        {
            hasImplicitSelf = false;
            string functionName = Expect(TokenType.IDENTIFIER, "期望函数名").Value;

            while (Match(TokenType.DOT))
            {
                functionName += "." + Expect(TokenType.IDENTIFIER, "期望字段名").Value;
            }

            if (Match(TokenType.COLON))
            {
                hasImplicitSelf = true;
                functionName += "." + Expect(TokenType.IDENTIFIER, "期望方法名").Value;
            }

            return functionName;
        }
        
        private ASTNode ParseIfStatement()
        {
            int line = Cur.Line;
            int column = Cur.Column;
            
            Expect(TokenType.IF, "期望 'if'");
            var condition = ParseExpression();
            Expect(TokenType.THEN, "期望 'then'");
            
            var thenBody = new List<ASTNode>();
            while (GetTokenType(Cur) != TokenType.ELSEIF && GetTokenType(Cur) != TokenType.ELSE && GetTokenType(Cur) != TokenType.END)
            {
                thenBody.Add(ParseStatement());
            }
            
            var elseifClauses = new List<(ASTNode, List<ASTNode>)>();
            while (Match(TokenType.ELSEIF))
            {
                var elseifCondition = ParseExpression();
                Expect(TokenType.THEN, "期望 'then'");
                
                var elseifBody = new List<ASTNode>();
                while (GetTokenType(Cur) != TokenType.ELSEIF && GetTokenType(Cur) != TokenType.ELSE && GetTokenType(Cur) != TokenType.END)
                {
                    elseifBody.Add(ParseStatement());
                }
                
                elseifClauses.Add((elseifCondition, elseifBody));
            }
            
            var elseBody = new List<ASTNode>();
            if (Match(TokenType.ELSE))
            {
                while (GetTokenType(Cur) != TokenType.END)
                {
                    elseBody.Add(ParseStatement());
                }
            }
            
            Expect(TokenType.END, "期望 'end'");
            
            // 构建条件列表：主条件 + elseif条件
            var conditions = new List<(ASTNode, List<ASTNode>)>();
            conditions.Add((condition, thenBody));
            conditions.AddRange(elseifClauses);
            
            return new IfStatementNode(conditions, elseBody, line, column);
        }
        
        private ASTNode ParseWhileStatement()
        {
            int line = Cur.Line;
            int column = Cur.Column;
            
            Expect(TokenType.WHILE, "期望 'while'");
            var condition = ParseExpression();
            Expect(TokenType.DO, "期望 'do'");
            
            var body = new List<ASTNode>();
            while (GetTokenType(Cur) != TokenType.END)
            {
                body.Add(ParseStatement());
            }
            
            Expect(TokenType.END, "期望 'end'");
            
            return new WhileStatementNode(condition, body, line, column);
        }
        
        private ASTNode ParseRepeatStatement()
        {
            int line = Cur.Line;
            int column = Cur.Column;
            
            Expect(TokenType.REPEAT, "期望 'repeat'");
            
            var body = new List<ASTNode>();
            while (GetTokenType(Cur) != TokenType.UNTIL)
            {
                body.Add(ParseStatement());
            }
            
            Expect(TokenType.UNTIL, "期望 'until'");
            var condition = ParseExpression();
            
            return new RepeatStatementNode(condition, body, line, column);
        }
        
        private ASTNode ParseForStatement()
        {
            int line = Cur.Line;
            int column = Cur.Column;
            
            Expect(TokenType.FOR, "期望 'for'");
            string firstVar = Expect(TokenType.IDENTIFIER, "期望变量名").Value;

            // 检测 for-in 还是 numeric for
            if (GetTokenType(Cur) == TokenType.IN || (GetTokenType(Cur) == TokenType.COMMA && _pos + 2 < _tokens.Count && _tokens[_pos + 1].Type == TokenType.IDENTIFIER && _tokens[_pos + 2].Type == TokenType.IN))
            {
                var variables = new List<string> { firstVar };
                while (Match(TokenType.COMMA))
                {
                    variables.Add(Expect(TokenType.IDENTIFIER, "期望变量名").Value);
                }
                Expect(TokenType.IN, "期望 'in'");
                var iteratorExpr = ParseExpression();
                Expect(TokenType.DO, "期望 'do'");
                var forInBody = new List<ASTNode>();
                while (GetTokenType(Cur) != TokenType.END)
                    forInBody.Add(ParseStatement());
                Expect(TokenType.END, "期望 'end'");
                return new ForInStatementNode(variables, iteratorExpr, forInBody, line, column);
            }

            Expect(TokenType.ASSIGN, "期望 '='");
            var startExpr = ParseExpression();
            Expect(TokenType.COMMA, "期望 ','");
            var endExpr = ParseExpression();
            
            ASTNode stepExpr = null;
            if (Match(TokenType.COMMA))
            {
                stepExpr = ParseExpression();
            }
            
            Expect(TokenType.DO, "期望 'do'");
            
            var body = new List<ASTNode>();
            while (GetTokenType(Cur) != TokenType.END)
            {
                body.Add(ParseStatement());
            }
            
            Expect(TokenType.END, "期望 'end'");
            
            return new ForStatementNode(firstVar, startExpr, endExpr, stepExpr, body, line, column);
        }
        
        private ASTNode ParseReturnStatement()
        {
            int line = Cur.Line;
            int column = Cur.Column;
            
            Expect(TokenType.RETURN, "期望 'return'");
            
            var values = new List<ASTNode>();
            if (GetTokenType(Cur) != TokenType.SEMICOLON && GetTokenType(Cur) != TokenType.EOF)
            {
                values.Add(ParseExpression());
                while (Match(TokenType.COMMA))
                {
                    values.Add(ParseExpression());
                }
            }
            
            return new ReturnStatementNode(values, line, column);
        }
        
        /// <summary>
        /// 解析表构造器 {key = value, ...} 或 {value, ...}
        /// </summary>
        private ASTNode ParseTableConstructor()
        {
            int line = Cur.Line;
            int column = Cur.Column;
            
            Expect(TokenType.LBRACE, "期望 '{'");
            
            var fields = new List<(ASTNode, ASTNode)>();
            int index = 1; // Lua 表索引从 1 开始
            
            while (GetTokenType(Cur) != TokenType.RBRACE && GetTokenType(Cur) != TokenType.EOF)
            {
                ASTNode key;
                ASTNode value;
                
                // 检查是否有 [key] = value 格式
                if (GetTokenType(Cur) == TokenType.LBRACKET)
                {
                    Advance(); // 跳过 '['
                    key = ParseExpression();
                    Expect(TokenType.RBRACKET, "期望 ']'");
                    Expect(TokenType.ASSIGN, "期望 '='");
                    value = ParseExpression();
                }
                // 检查是否有 key = value 格式（标识符）
                else if (GetTokenType(Cur) == TokenType.IDENTIFIER && _pos + 1 < _tokens.Count && _tokens[_pos + 1].Type == TokenType.ASSIGN)
                {
                    var keyToken = Advance();
                    Advance(); // 跳过 '='
                    key = new ConstantNode(keyToken.Value, "string", keyToken.Line, keyToken.Column);
                    value = ParseExpression();
                }
                // 否则是连续值 [index] = value 格式
                else
                {
                    value = ParseExpression();
                    // 自动分配索引（Lua 表从 1 开始）
                    key = new ConstantNode((double)index, "number", line, column);
                    index++;
                }
                
                fields.Add((key, value));
                
                // 跳过逗号或分号
                if (GetTokenType(Cur) == TokenType.COMMA || GetTokenType(Cur) == TokenType.SEMICOLON)
                {
                    Advance();
                }
            }
            
            Expect(TokenType.RBRACE, "期望 '}'");
            
            return new TableConstructorNode(fields, line, column);
        }
        
        /// <summary>
        /// 解析后缀表达式（表访问、方法调用）
        /// </summary>
        private ASTNode ParsePostfix(ASTNode node)
        {
            while (true)
            {
                if (GetTokenType(Cur) == TokenType.LBRACKET)
                {
                    // 表访问 t[index]
                    Advance(); // 跳过 '['
                    var index = ParseExpression();
                    Expect(TokenType.RBRACKET, "期望 ']'");
                    node = new TableAccessNode(node, index, node.Line, node.Column);
                }
                else if (GetTokenType(Cur) == TokenType.DOT)
                {
                    // 表访问 t.key
                    Advance(); // 跳过 '.'
                    var keyToken = Expect(TokenType.IDENTIFIER, "期望字段名");
                    // 转换为字符串键
                    var keyNode = new ConstantNode(keyToken.Value, "string", keyToken.Line, keyToken.Column);
                    node = new TableAccessNode(node, keyNode, node.Line, node.Column);
                }
                else if (GetTokenType(Cur) == TokenType.LPAREN)
                {
                    node = ParseFunctionCall(node, node.Line, node.Column);
                }
                else if (GetTokenType(Cur) == TokenType.COLON)
                {
                    var selfExpr = node;
                    Advance();
                    var keyToken = Expect(TokenType.IDENTIFIER, "期望方法名");
                    var keyNode = new ConstantNode(keyToken.Value, "string", keyToken.Line, keyToken.Column);
                    var methodExpr = new TableAccessNode(selfExpr, keyNode, selfExpr.Line, selfExpr.Column);
                    var call = ParseFunctionCall(methodExpr, keyToken.Line, keyToken.Column);
                    call.Arguments.Insert(0, selfExpr);
                    node = call;
                }
                else
                {
                    break;
                }
            }
            return node;
        }
        
        /// <summary>
        /// 解析函数调用
        /// </summary>
        private FunctionCallNode ParseFunctionCall(ASTNode function, int line, int column)
        {
            Expect(TokenType.LPAREN, "期望 '('");
            
            List<ASTNode> arguments = new List<ASTNode>();
            
            // 检查是否有参数
            if (GetTokenType(Cur) != TokenType.RPAREN)
            {
                // 解析第一个参数
                arguments.Add(ParseExpression());
                
                // 解析更多参数（逗号分隔）
                while (GetTokenType(Cur) == TokenType.COMMA)
                {
                    Advance(); // 跳过逗号
                    arguments.Add(ParseExpression());
                }
            }
            
            Expect(TokenType.RPAREN, "期望 ')'");
            
            return new FunctionCallNode(function, arguments, line, column);
        }

        private ASTNode ParseFunctionExpression()
        {
            int line = Cur.Line;
            int column = Cur.Column;
            Expect(TokenType.FUNCTION, "期望 'function'");
            Expect(TokenType.LPAREN, "期望 '('");

            var parameters = new List<string>();
            if (GetTokenType(Cur) != TokenType.RPAREN)
            {
                parameters.Add(Expect(TokenType.IDENTIFIER, "期望参数名").Value);
                while (Match(TokenType.COMMA))
                {
                    parameters.Add(Expect(TokenType.IDENTIFIER, "期望参数名").Value);
                }
            }

            Expect(TokenType.RPAREN, "期望 ')'");

            var body = new List<ASTNode>();
            while (GetTokenType(Cur) != TokenType.END && GetTokenType(Cur) != TokenType.EOF)
            {
                body.Add(ParseStatement());
            }
            Expect(TokenType.END, "期望 'end'");

            string name = $"anon_{anonymousFunctionCounter++}";
            return new FunctionExpressionNode(name, parameters, body, line, column);
        }
        
        private ASTNode ParsePrimary()
        {
            var token = Cur;
            
            switch (token.Type)
            {
                case TokenType.NUMBER:
                    Advance();
                    if (token.Value.StartsWith("0x") || token.Value.StartsWith("0X"))
                    {
                        int hexVal = Convert.ToInt32(token.Value.Substring(2), 16);
                        return new ConstantNode((double)hexVal, "number", token.Line, token.Column);
                    }
                    return new ConstantNode(double.Parse(token.Value), "number", token.Line, token.Column);
                    
                case TokenType.STRING:
                    Advance();
                    return new ConstantNode(token.Value, "string", token.Line, token.Column);
                    
                case TokenType.TRUE:
                    Advance();
                    return new ConstantNode(true, "boolean", token.Line, token.Column);
                    
                case TokenType.FALSE:
                    Advance();
                    return new ConstantNode(false, "boolean", token.Line, token.Column);
                    
                case TokenType.NIL:
                    Advance();
                    return new ConstantNode(null, "nil", token.Line, token.Column);
                    
                case TokenType.IDENTIFIER:
                    Advance();
                    var identifier = new IdentifierNode(token.Value, token.Line, token.Column);
                    return ParsePostfix(identifier);

                case TokenType.FUNCTION:
                    return ParsePostfix(ParseFunctionExpression());
                    
                case TokenType.LPAREN:
                    Advance();
                    var expr = ParseExpression();
                    Expect(TokenType.RPAREN, "期望 ')'");
                    return ParsePostfix(expr);
                    
                case TokenType.LEN:
                    Advance();
                    var lenOperand = ParsePrimary();
                    return new UnaryOperationNode(TokenType.LEN, lenOperand, token.Line, token.Column);

                case TokenType.LBRACE:
                    var table = ParseTableConstructor();
                    return ParsePostfix(table);
                    
                default:
                    // 位置用 `GapAnchor()`，不是裸 `Cur`：`x = 1 +` 结尾撞上的是 **`EOF`**
                    // （Lua 没有换行 token，EOF 被标在下一行）⇒ 按当前位置报就落到第 5 行，
                    // 而错在第 4 行。锚定规则见基类 `GapAnchor`。
                    throw ErrorAt($"意外的token: {token.Type}（此处不该出现它）", GapAnchor());
            }
        }
    }
}
