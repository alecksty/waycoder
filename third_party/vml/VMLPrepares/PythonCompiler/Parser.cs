using System.Collections.Generic;
using VMLPlugins;
using CompilerBase;

namespace PythonCompiler
{
    /// <summary>
    /// Python 语法分析器
    /// </summary>
    public class Parser : ParserBase<Token, TokenType>
    {
        private bool _isMCU;

        protected override TokenType GetTokenType(Token token) => token.Type;

        /// <summary>
        /// 表达式**收尾符** —— 永远不会是表达式的开头。`GapAnchor()` 拿它判
        /// 「缺口在撞上的 token 之前吗」（见基类那条规则的三个判据）。
        ///
        /// `NEWLINE` / `EOF` 也算收尾符：Python 的语句以换行收尾，`x = 1 +` 正是等换行后
        /// 遇到 `NEWLINE` 才发作 —— 不把它算进来，这条判据在最常见的形态上就不生效。
        /// （它同时是**语句分隔**，但两个谓词判的是不同的 token：这个判 `Cur`，
        ///  下面那个判"上一个"，不冲突。）
        /// </summary>
        protected override bool IsExpressionCloser(Token token) => token.Type
            is TokenType.RPAREN or TokenType.RBRACKET or TokenType.RBRACE
            or TokenType.COMMA or TokenType.COLON or TokenType.SEMICOLON
            or TokenType.NEWLINE or TokenType.EOF;

        /// <summary>语句分隔 —— `GapAnchor()` 的第二个判据：上一个若是它，缺口在本行行首。</summary>
        protected override bool IsStatementSeparator(Token token) => token.Type
            is TokenType.NEWLINE or TokenType.SEMICOLON;

        public Parser(List<Token> tokens, bool isMCU = true) : base(tokens)
        {
            _isMCU = isMCU;
        }

        private void SkipNewlines()
        {
            while (GetTokenType(Cur) == TokenType.NEWLINE || GetTokenType(Cur) == TokenType.SEMICOLON)
            {
                Advance();
            }
        }

        public ProgramNode Parse()
        {
            var body = new List<ASTNode>();
            SkipNewlines();
            
            while (GetTokenType(Cur) != TokenType.EOF)
            {
                SkipNewlines();
                if (GetTokenType(Cur) == TokenType.EOF) break;
                
                body.Add(ParseStatement());
                SkipNewlines();
            }
            
            return new ProgramNode(body, 1, 1);
        }

        private ASTNode ParseStatement()
        {
            SkipNewlines();
            var token = Cur;
            
            return token.Type switch
            {
                TokenType.AT      => ParseDecorated(),
                TokenType.DEF     => ParseFunctionDef(),
                TokenType.CLASS   => ParseClassDef(),
                TokenType.IF      => ParseIf(),
                TokenType.WHILE   => ParseWhile(),
                TokenType.FOR     => ParseFor(),
                TokenType.RETURN  => ParseReturn(),
                TokenType.BREAK   => ParseBreak(),
                TokenType.CONTINUE => ParseContinue(),
                TokenType.PASS    => ParsePass(),
                TokenType.ASSERT  => ParseAssert(),
                TokenType.RAISE   => ParseRaise(),
                TokenType.DEL     => ParseDel(),
                TokenType.GLOBAL  => ParseGlobal(),
                TokenType.NONLOCAL => ParseNonlocal(),
                TokenType.IMPORT   => ParseImport(),
                TokenType.FROM     => ParseImport(),
                TokenType.WITH    => ParseWith(),
                TokenType.TRY     => ParseTry(),
                TokenType.MATCH   => ParseMatch(),
                _                  => ParseSimpleStatement()
            };
        }

        private ASTNode ParseDecorated()
        {
            var decorators = new List<ASTNode>();
            // Collect all consecutive @decorator lines
            while (GetTokenType(Cur) == TokenType.AT)
            {
                Advance(); // consume @
                var decoExpr = ParseExpression(); // decorator name or call: @deco or @deco(args)
                decorators.Add(decoExpr);
                SkipNewlines();
            }

            // Parse the actual function or class definition
            ASTNode result;
            if (GetTokenType(Cur) == TokenType.DEF)
                result = ParseFunctionDef(decorators);
            else if (GetTokenType(Cur) == TokenType.CLASS)
                result = ParseClassDef(decorators);
            else
                throw Error($"期望 'def' 或 'class' 在装饰器后，实际得到 {GetTokenType(Cur)}");

            return result;
        }

        private ASTNode ParseFunctionDef(List<ASTNode>? decorators = null)
        {
            var defToken = Expect(TokenType.DEF);
            var name = Expect(TokenType.IDENTIFIER).Value;
            Expect(TokenType.LPAREN);

            var args = new List<string>();
            if (GetTokenType(Cur) != TokenType.RPAREN)
            {
                args.Add(Expect(TokenType.IDENTIFIER).Value);
                while (Match(TokenType.COMMA))
                {
                    args.Add(Expect(TokenType.IDENTIFIER).Value);
                }
            }
            Expect(TokenType.RPAREN);
            Expect(TokenType.COLON);

            var body = ParseSuite();
            return new FunctionDefNode(name, args, body, defToken.Line, defToken.Column, decorators);
        }

        private ASTNode ParseClassDef(List<ASTNode>? decorators = null)
        {
            var classToken = Expect(TokenType.CLASS);
            var name = Expect(TokenType.IDENTIFIER).Value;
            string? parentName = null;

            if (Match(TokenType.LPAREN))
            {
                parentName = Expect(TokenType.IDENTIFIER).Value;
                Expect(TokenType.RPAREN);
            }

            Expect(TokenType.COLON);

            var body = ParseSuite();
            return new ClassDefNode(name, body, parentName, classToken.Line, classToken.Column, decorators);
        }

        private ASTNode ParseIf()
        {
            var ifToken = Expect(TokenType.IF);
            var test = ParseExpression();
            Expect(TokenType.COLON);
            var body = ParseSuite();
            
            var orelse = new List<ASTNode>();
            SkipNewlines();
            if (GetTokenType(Cur) == TokenType.ELIF)
            {
                Advance(); // 消费 ELIF
                var elifTest = ParseExpression();
                Expect(TokenType.COLON);
                var elifBody = ParseSuite();
                // 将 elif 转为嵌套的 if-else
                var elifNode = ParseElifElse(elifTest, elifBody);
                orelse.Add(elifNode);
            }
            else if (GetTokenType(Cur) == TokenType.ELSE)
            {
                Advance();
                Expect(TokenType.COLON);
                orelse = ParseSuite();
            }

            return new IfNode(test, body, orelse, ifToken.Line, ifToken.Column);
        }

        /// <summary>
        /// 解析 elif 链：已消费 ELIF，已有 test 和 body，继续解析后续 elif/else
        /// </summary>
        private IfNode ParseElifElse(ASTNode test, List<ASTNode> body)
        {
            var orelse = new List<ASTNode>();
            SkipNewlines();
            if (GetTokenType(Cur) == TokenType.ELIF)
            {
                Advance(); // 消费 ELIF
                var elifTest = ParseExpression();
                Expect(TokenType.COLON);
                var elifBody = ParseSuite();
                orelse.Add(ParseElifElse(elifTest, elifBody));
            }
            else if (GetTokenType(Cur) == TokenType.ELSE)
            {
                Advance();
                Expect(TokenType.COLON);
                orelse = ParseSuite();
            }
            return new IfNode(test, body, orelse, 0, 0);
        }

        private ASTNode ParseWhile()
        {
            var whileToken = Expect(TokenType.WHILE);
            var test = ParseExpression();
            Expect(TokenType.COLON);
            var body = ParseSuite();
            List<ASTNode>? orelse = null;
            SkipNewlines();
            if (GetTokenType(Cur) == TokenType.ELSE)
            {
                Advance();
                Expect(TokenType.COLON);
                orelse = ParseSuite();
            }
            return new WhileNode(test, body, orelse, whileToken.Line, whileToken.Column);
        }

        private ASTNode ParseFor()
        {
            var forToken = Expect(TokenType.FOR);
            var target = Expect(TokenType.IDENTIFIER).Value;
            Expect(TokenType.IN);
            var iter = ParseExpression();
            Expect(TokenType.COLON);
            var body = ParseSuite();
            List<ASTNode>? orelse = null;
            SkipNewlines();
            if (GetTokenType(Cur) == TokenType.ELSE)
            {
                Advance();
                Expect(TokenType.COLON);
                orelse = ParseSuite();
            }
            return new ForNode(target, iter, body, orelse, forToken.Line, forToken.Column);
        }

        private ASTNode ParseReturn()
        {
            var returnToken = Expect(TokenType.RETURN);
            ASTNode value = null;
            if (GetTokenType(Cur) != TokenType.NEWLINE && GetTokenType(Cur) != TokenType.SEMICOLON && GetTokenType(Cur) != TokenType.EOF && GetTokenType(Cur) != TokenType.DEDENT)
            {
                value = ParseExpression();
            }
            return new ReturnNode(value, returnToken.Line, returnToken.Column);
        }

        private ASTNode ParseBreak()
        {
            var token = Advance();
            return new BreakNode(token.Line, token.Column);
        }

        private ASTNode ParseContinue()
        {
            var token = Advance();
            return new ContinueNode(token.Line, token.Column);
        }

        private ASTNode ParsePass()
        {
            var token = Advance();
            return new PassNode(token.Line, token.Column);
        }

        private ASTNode ParseAssert()
        {
            var token = Expect(TokenType.ASSERT);
            var test = ParseExpression();
            ASTNode msg = null;
            if (GetTokenType(Cur) == TokenType.COMMA)
            {
                Advance();
                msg = ParseExpression();
            }
            return new AssertNode(test, msg, token.Line, token.Column);
        }

        private ASTNode ParseRaise()
        {
            var token = Expect(TokenType.RAISE);
            ASTNode exc = null;
            if (GetTokenType(Cur) != TokenType.NEWLINE && GetTokenType(Cur) != TokenType.SEMICOLON && GetTokenType(Cur) != TokenType.EOF && GetTokenType(Cur) != TokenType.DEDENT)
                exc = ParseExpression();
            return new RaiseNode(exc, token.Line, token.Column);
        }

        private ASTNode ParseDel()
        {
            var token = Expect(TokenType.DEL);
            var name = Expect(TokenType.IDENTIFIER).Value;
            return new DelNode(name, token.Line, token.Column);
        }

        private ASTNode ParseGlobal()
        {
            var token = Expect(TokenType.GLOBAL);
            var names = new List<string> { Expect(TokenType.IDENTIFIER).Value };
            while (Match(TokenType.COMMA))
                names.Add(Expect(TokenType.IDENTIFIER).Value);
            return new GlobalNode(names, token.Line, token.Column);
        }

        private ASTNode ParseNonlocal()
        {
            var token = Expect(TokenType.NONLOCAL);
            var names = new List<string> { Expect(TokenType.IDENTIFIER).Value };
            while (Match(TokenType.COMMA))
                names.Add(Expect(TokenType.IDENTIFIER).Value);
            return new NonlocalNode(names, token.Line, token.Column);
        }

        private ASTNode ParseImport()
        {
            var token = Advance(); // consume IMPORT or FROM

            if (token.Value == "from")
            {
                string moduleName = Expect(TokenType.IDENTIFIER).Value;
                Expect(TokenType.IMPORT);
                var items = new List<(string, string)>();
                items.Add((Expect(TokenType.IDENTIFIER).Value, null));
                while (Match(TokenType.COMMA))
                {
                    var name = Expect(TokenType.IDENTIFIER).Value;
                    string alias = null;
                    if (Match(TokenType.AS))
                        alias = Expect(TokenType.IDENTIFIER).Value;
                    items.Add((name, alias));
                }
                return new ImportNode(items, token.Line, token.Column, moduleName);
            }

            var items2 = new List<(string, string)>();
            items2.Add((Expect(TokenType.IDENTIFIER).Value, null));
            while (Match(TokenType.COMMA))
            {
                var name = Expect(TokenType.IDENTIFIER).Value;
                string alias = null;
                if (Match(TokenType.AS))
                    alias = Expect(TokenType.IDENTIFIER).Value;
                items2.Add((name, alias));
            }
            return new ImportNode(items2, token.Line, token.Column);
        }

        private ASTNode ParseWith()
        {
            var token = Expect(TokenType.WITH);
            var withItems = new List<(ASTNode, string)>();
            // 简化：只支持 "with EXPR as NAME:"
            withItems.Add((ParseExpression(), null));
            if (Match(TokenType.AS))
                withItems[^1] = (withItems[^1].Item1, Expect(TokenType.IDENTIFIER).Value);
            Expect(TokenType.COLON);
            var body = ParseSuite();
            return new WithNode(withItems, body, token.Line, token.Column);
        }

        private ASTNode ParseTry()
        {
            var token = Expect(TokenType.TRY);
            Expect(TokenType.COLON);
            var body = ParseSuite();
            var handlers = new List<(List<string>, ASTNode, List<ASTNode>)>();
            while (GetTokenType(Cur) == TokenType.EXCEPT)
            {
                Advance();
                List<string> types = null;
                ASTNode name = null;
                if (GetTokenType(Cur) == TokenType.IDENTIFIER)
                    types = new List<string> { Expect(TokenType.IDENTIFIER).Value };
                if (Match(TokenType.AS))
                    name = new NameNode(Expect(TokenType.IDENTIFIER).Value, Cur.Line, Cur.Column);
                Expect(TokenType.COLON);
                var handlerBody = ParseSuite();
                handlers.Add((types, name, handlerBody));
            }
            var orelse = new List<ASTNode>();
            if (GetTokenType(Cur) == TokenType.ELSE)
            {
                Advance();
                Expect(TokenType.COLON);
                orelse = ParseSuite();
            }
            var finalbody = new List<ASTNode>();
            if (GetTokenType(Cur) == TokenType.FINALLY)
            {
                Advance();
                Expect(TokenType.COLON);
                finalbody = ParseSuite();
            }
            return new TryNode(body, handlers, orelse, finalbody, token.Line, token.Column);
        }

        private ASTNode ParseMatch()
        {
            var token = Expect(TokenType.MATCH);
            var subject = ParseExpression();
            Expect(TokenType.COLON);
            Expect(TokenType.NEWLINE);
            Expect(TokenType.INDENT);
            var cases = new List<CaseNode>();
            while (GetTokenType(Cur) == TokenType.CASE)
            {
                cases.Add(ParseCase());
                if (GetTokenType(Cur) == TokenType.NEWLINE) Advance();
            }
            Expect(TokenType.DEDENT);
            return new MatchNode(subject, cases, token.Line, token.Column);
        }

        private CaseNode ParseCase()
        {
            var token = Expect(TokenType.CASE);
            var pattern = ParsePattern();
            ASTNode guard = null;
            if (Match(TokenType.IF))
                guard = ParseExpression();
            Expect(TokenType.COLON);
            var body = ParseSuite();
            return new CaseNode(pattern, guard, body, token.Line, token.Column);
        }

        private ASTNode ParsePattern()
        {
            // 简化：支持常量、as 模式、or 模式
            return ParseOr();
        }

        private ASTNode ParseSimpleStatement()
        {
            var expr = ParseExpression();

            // 多元赋值: a, b = 1, 2
            if (expr is NameNode firstTarget && GetTokenType(Cur) == TokenType.COMMA)
            {
                var targets = new List<string> { firstTarget.Name };
                while (Match(TokenType.COMMA))
                {
                    targets.Add(Expect(TokenType.IDENTIFIER).Value);
                }
                Expect(TokenType.ASSIGN);
                var values = new List<ASTNode> { ParseExpression() };
                while (Match(TokenType.COMMA))
                    values.Add(ParseExpression());
                var value = values.Count == 1 ? values[0] : new TupleNode(values, firstTarget.Line, firstTarget.Column);
                return new MultiAssignNode(targets, value, firstTarget.Line, firstTarget.Column);
            }

            // 赋值语句 (支持链式赋值: a = b = c = value)
            if (GetTokenType(Cur) == TokenType.ASSIGN)
            {
                Advance();
                var value = ParseExpression();

                // 链式赋值: 收集所有目标
                var targets = new List<ASTNode>();
                if (expr is NameNode || expr is AttributeNode || expr is SubscriptNode)
                    targets.Add(expr);
                else
                    return new ExprStmtNode(expr, expr.Line, expr.Column);

                while (GetTokenType(Cur) == TokenType.ASSIGN)
                {
                    if (value is NameNode || value is AttributeNode || value is SubscriptNode)
                    {
                        targets.Add(value);
                        Advance();
                        value = ParseExpression();
                    }
                    else break;
                }

                // 从右向左生成嵌套赋值
                ASTNode result = MakeAssignNode(targets[targets.Count - 1], value);
                for (int i = targets.Count - 2; i >= 0; i--)
                    result = MakeAssignNode(targets[i], result);
                return result;
            }
            // 增强赋值
            else if (GetTokenType(Cur) is TokenType.PLUS_ASSIGN or TokenType.MINUS_ASSIGN or 
                     TokenType.MUL_ASSIGN or TokenType.DIV_ASSIGN or
                     TokenType.MOD_ASSIGN or TokenType.FLOOR_DIV_ASSIGN)
            {
                var op = Advance().Value;
                var value = ParseExpression();
                if (expr is NameNode nameNode)
                    return new AugAssignNode(nameNode.Name, op, value, nameNode.Line, nameNode.Column);
                if (expr is AttributeNode || expr is SubscriptNode)
                    return new AugAssignNode(expr, op, value, expr.Line, expr.Column);
            }
            
            return new ExprStmtNode(expr, expr.Line, expr.Column);
        }

        private ASTNode MakeAssignNode(ASTNode target, ASTNode value)
        {
            if (target is NameNode nameNode)
                return new AssignNode(nameNode.Name, value, nameNode.Line, nameNode.Column);
            if (target is AttributeNode || target is SubscriptNode)
                return new AssignNode(target, value, target.Line, target.Column);
            return new ExprStmtNode(value, value.Line, value.Column);
        }

        private List<ASTNode> ParseSuite()
        {
            var body = new List<ASTNode>();
            
            SkipNewlines();
            if (GetTokenType(Cur) != TokenType.INDENT)
            {
                // 单行语句
                body.Add(ParseStatement());
                return body;
            }
            
            Expect(TokenType.INDENT);
            SkipNewlines();
            
            while (GetTokenType(Cur) != TokenType.DEDENT && GetTokenType(Cur) != TokenType.EOF)
            {
                body.Add(ParseStatement());
                SkipNewlines();
            }
            
            if (GetTokenType(Cur) == TokenType.DEDENT)
            {
                Advance();
            }
            
            return body;
        }

        private ASTNode ParseExpression()
        {
            return ParseNamedExpr();
        }

        // Walrus := (最低优先级表达式)
        private ASTNode ParseNamedExpr()
        {
            // Lambda 嵌入在表达式链顶部（比赋值更低）
            if (GetTokenType(Cur) == TokenType.LAMBDA)
            {
                var token = Advance();
                var args = new List<string>();
                while (GetTokenType(Cur) != TokenType.COLON && GetTokenType(Cur) != TokenType.EOF)
                {
                    if (GetTokenType(Cur) == TokenType.IDENTIFIER)
                        args.Add(Expect(TokenType.IDENTIFIER).Value);
                    if (GetTokenType(Cur) == TokenType.COMMA)
                        Advance();
                    else break;
                }
                Expect(TokenType.COLON);
                var body = ParseNamedExpr(); // 右结合
                return new LambdaNode(args, body, token.Line, token.Column);
            }
            return ParseTernary();
        }

        // 三元表达式 if-else
        private ASTNode ParseTernary()
        {
            var test = ParseOr();
            if (GetTokenType(Cur) == TokenType.IF)
            {
                Advance();
                var trueExpr = ParseOr();
                Expect(TokenType.ELSE);
                var falseExpr = ParseTernary();
                return new IfNode(test, new List<ASTNode> { new ExprStmtNode(trueExpr, test.Line, test.Column) },
                                  new List<ASTNode> { new ExprStmtNode(falseExpr, test.Line, test.Column) },
                                  test.Line, test.Column);
            }
            return test;
        }

        private ASTNode ParseOr()
        {
            var left = ParseAnd();
            while (GetTokenType(Cur) == TokenType.OR)
            {
                var op = Advance().Value;
                var right = ParseAnd();
                left = new BoolOpNode(op, new List<ASTNode> { left, right }, left.Line, left.Column);
            }
            return left;
        }

        private ASTNode ParseAnd()
        {
            var left = ParseNot();
            while (GetTokenType(Cur) == TokenType.AND)
            {
                var op = Advance().Value;
                var right = ParseNot();
                left = new BoolOpNode("and", new List<ASTNode> { left, right }, left.Line, left.Column);
            }
            return left;
        }

        private ASTNode ParseNot()
        {
            if (GetTokenType(Cur) == TokenType.NOT)
            {
                var token = Advance();
                var operand = ParseNot();
                return new UnaryOpNode("not", operand, token.Line, token.Column);
            }
            return ParseComparison();
        }

        private ASTNode ParseComparison()
        {
            var left = ParseBitwiseOr();
            var ops = new List<(string op, ASTNode right)>();
            
            while (GetTokenType(Cur) is TokenType.EQ or TokenType.NE or TokenType.LT or 
                   TokenType.LE or TokenType.GT or TokenType.GE or
                   TokenType.IN or TokenType.IS)
            {
                var op = Advance().Value;
                var right = ParseAddSub();
                ops.Add((op, right));
            }
            
            if (ops.Count == 0) return left;
            if (ops.Count == 1) return new CompareNode(left, ops[0].op, ops[0].Item2, left.Line, left.Column);
            
            // 链式比较：a < b <= c 转为 (a < b) and (b <= c)
            var parts = new List<ASTNode> { new CompareNode(left, ops[0].op, ops[0].Item2, left.Line, left.Column) };
            for (int i = 1; i < ops.Count; i++)
                parts.Add(new CompareNode(ops[i-1].Item2, ops[i].op, ops[i].Item2, left.Line, left.Column));
            return new BoolOpNode("and", parts, left.Line, left.Column);
        }

        private ASTNode ParseBitwiseOr()
        {
            var left = ParseBitwiseXor();
            while (GetTokenType(Cur) == TokenType.BITOR)
            {
                Advance();
                var right = ParseBitwiseXor();
                left = new BinOpNode(left, "|", right, left.Line, left.Column);
            }
            return left;
        }

        private ASTNode ParseBitwiseXor()
        {
            var left = ParseBitwiseAnd();
            while (GetTokenType(Cur) == TokenType.BITXOR)
            {
                Advance();
                var right = ParseBitwiseAnd();
                left = new BinOpNode(left, "^", right, left.Line, left.Column);
            }
            return left;
        }

        private ASTNode ParseBitwiseAnd()
        {
            var left = ParseShift();
            while (GetTokenType(Cur) == TokenType.BITAND)
            {
                Advance();
                var right = ParseShift();
                left = new BinOpNode(left, "&", right, left.Line, left.Column);
            }
            return left;
        }

        private ASTNode ParseShift()
        {
            var left = ParseAddSub();
            while (GetTokenType(Cur) is TokenType.LSHIFT or TokenType.RSHIFT)
            {
                var op = Advance().Value;
                var right = ParseAddSub();
                left = new BinOpNode(left, op, right, left.Line, left.Column);
            }
            return left;
        }

        private ASTNode ParseAddSub()
        {
            var left = ParseMulDiv();
            while (GetTokenType(Cur) is TokenType.PLUS or TokenType.MINUS)
            {
                var op = Advance().Value;
                var right = ParseMulDiv();
                left = new BinOpNode(left, op, right, left.Line, left.Column);
            }
            return left;
        }

        private ASTNode ParseMulDiv()
        {
            var left = ParseUnary();
            while (GetTokenType(Cur) is TokenType.MUL or TokenType.DIV or TokenType.MOD or TokenType.FLOOR_DIV)
            {
                var op = Advance().Value;
                var right = ParseUnary();
                left = new BinOpNode(left, op, right, left.Line, left.Column);
            }
            return left;
        }

        private ASTNode ParseUnary()
        {
            if (GetTokenType(Cur) is TokenType.MINUS or TokenType.PLUS or TokenType.BITNOT)
            {
                var op = Advance().Value;
                var operand = ParseUnary();
                return new UnaryOpNode(op, operand, Cur.Line, Cur.Column);
            }
            return ParsePower();
        }

        private ASTNode ParseAwait()
        {
            if (GetTokenType(Cur) == TokenType.AWAIT)
            {
                var token = Advance();
                var value = ParsePower(); // await expr 优先级高于 **
                return new AwaitNode(value, token.Line, token.Column);
            }
            return ParseAtom();
        }

        private ASTNode ParsePower()
        {
            var left = ParseAwait();
            if (GetTokenType(Cur) == TokenType.POWER)
            {
                var op = Advance().Value;
                var right = ParseUnary(); // ** 右结合，右操作数用 unary 优先级
                return new BinOpNode(left, op, right, left.Line, left.Column);
            }
            return left;
        }

        private ASTNode ParseAtom()
        {
            var token = Cur;
            
            switch (token.Type)
            {
                case TokenType.INTEGER:
                    Advance();
                    string intStr = token.Value.Replace("_", "");
                    return new ConstantNode(int.Parse(intStr), "int", token.Line, token.Column);
                    
                case TokenType.HEX_INTEGER:
                    Advance();
                    string hexStr = token.Value[2..].Replace("_", "");
                    return new ConstantNode(Convert.ToInt32(hexStr, 16), "int", token.Line, token.Column);
                    
                case TokenType.OCT_INTEGER:
                    Advance();
                    string octStr = token.Value[2..].Replace("_", "");
                    return new ConstantNode(Convert.ToInt32(octStr, 8), "int", token.Line, token.Column);
                    
                case TokenType.BIN_INTEGER:
                    Advance();
                    string binStr = token.Value[2..].Replace("_", "");
                    return new ConstantNode(Convert.ToInt32(binStr, 2), "int", token.Line, token.Column);
                    
                case TokenType.FLOAT:
                    Advance();
                    string floatStr = token.Value.Replace("_", "");
                    return new ConstantNode(float.Parse(floatStr), "float", token.Line, token.Column);
                    
                case TokenType.STRING:
                    Advance();
                    return new ConstantNode(token.Value, "str", token.Line, token.Column);
                    
                case TokenType.FSTRING:
                    Advance();
                    // 解析f-string，提取字符串片段和表达式
                    return ParseFString(token.Value, token.Line, token.Column);
                    
                case TokenType.TRUE:
                    Advance();
                    return new ConstantNode(true, "bool", token.Line, token.Column);
                    
                case TokenType.FALSE:
                    Advance();
                    return new ConstantNode(false, "bool", token.Line, token.Column);
                    
                case TokenType.NONE:
                    Advance();
                    return new ConstantNode(null, "None", token.Line, token.Column);
                    
                case TokenType.IDENTIFIER:
                    Advance();
                    var name = token.Value;
                    
                    // 函数调用
                    if (GetTokenType(Cur) == TokenType.LPAREN)
                    {
                        var callNode = ParseCall(name, token.Line, token.Column);
                        // 处理调用后的属性访问: func().attr
                        return ParsePostAtom(callNode);
                    }
                    
                    ASTNode nameNode = new NameNode(name, token.Line, token.Column);
                    return ParsePostAtom(nameNode);
                    
                case TokenType.LPAREN:
                    Advance();
                    // 括号表达式：tuple() / generator / yield
                    if (GetTokenType(Cur) == TokenType.YIELD)
                    {
                        Advance();
                        ASTNode val = null;
                        if (GetTokenType(Cur) != TokenType.RPAREN)
                            val = ParseExpression();
                        Expect(TokenType.RPAREN);
                        return new YieldNode(val, token.Line, token.Column);
                    }
                    if (GetTokenType(Cur) == TokenType.RPAREN)
                    {
                        Advance();
                        return new TupleNode(new List<ASTNode>(), token.Line, token.Column);
                    }
                    var first = ParseExpression();
                    if (GetTokenType(Cur) == TokenType.COMMA)
                    {
                        // 元组
                        var elements = new List<ASTNode> { first };
                        while (Match(TokenType.COMMA))
                            elements.Add(ParseExpression());
                        Expect(TokenType.RPAREN);
                        return new TupleNode(elements, token.Line, token.Column);
                    }
                    Expect(TokenType.RPAREN);
                    return first;
                    
                case TokenType.LBRACKET:
                    return ParseList();
                    
                case TokenType.LBRACE:
                    return ParseDictOrSet();
                    
                // yield 是前缀表达式（MCU模式跳过）
                case TokenType.YIELD:
                    if (_isMCU) { WarningEmitter.Emit("python", "MCU模式: yield被忽略（不支持生成器）"); Advance(); return null; }
                    Advance();
                    ASTNode yval = null;
                    if (GetTokenType(Cur) != TokenType.NEWLINE && GetTokenType(Cur) != TokenType.COLON)
                        yval = ParseExpression();
                    return new YieldNode(yval, token.Line, token.Column);
                    
                // await 是前缀表达式（MCU模式跳过）
                case TokenType.AWAIT:
                    if (_isMCU) { WarningEmitter.Emit("python", "MCU模式: await被忽略（不支持异步）"); Advance(); return null; }
                    Advance();
                    var aval = ParseAtom();
                    return new AwaitNode(aval, token.Line, token.Column);
                    
                default:
                    // 位置用 `GapAnchor()`，不是裸 `Cur`：`x = 1 +` 的下一个 token 是
                    // **下一行**的 `NEWLINE` —— 按当前位置报就落到第 5 行（修复前实测），
                    // 而错在第 4 行。锚定规则（含"什么时候**不该**锚"）见基类 `GapAnchor`。
                    throw ErrorAt($"意外的 token: {token.Type}（此处不该出现它）", GapAnchor());
            }
        }

        private ASTNode ParseDictOrSet()
        {
            var token = Expect(TokenType.LBRACE);
            if (GetTokenType(Cur) == TokenType.RBRACE)
            {
                Advance();
                return new DictNode(new List<(ASTNode, ASTNode)>(), token.Line, token.Column);
            }
            
            var elements = new List<ASTNode>();
            var dictItems = new List<(ASTNode, ASTNode)>();
            bool isDict = false;
            
            while (GetTokenType(Cur) != TokenType.RBRACE)
            {
                var key = ParseExpression();
                if (GetTokenType(Cur) == TokenType.COLON)
                {
                    Advance();
                    var val = ParseExpression();
                    dictItems.Add((key, val));
                    isDict = true;
                }
                else
                {
                    if (isDict) throw Error("字典字面量中不能混入非键值对元素");
                    elements.Add(key);
                }
                
                if (GetTokenType(Cur) != TokenType.RBRACE)
                    Expect(TokenType.COMMA);
            }
            
            Expect(TokenType.RBRACE);
            return isDict
                ? new DictNode(dictItems, token.Line, token.Column) as ASTNode
                : new SetNode(elements, token.Line, token.Column);
        }

        /// <summary>
        /// 处理后置操作符: .attr, [index]
        /// </summary>
        private ASTNode ParsePostAtom(ASTNode expr)
        {
            while (true)
            {
                // 属性访问: expr.attr
                if (GetTokenType(Cur) == TokenType.DOT)
                {
                    Advance(); // 跳过 .
                    string attr = Expect(TokenType.IDENTIFIER).Value;
                    expr = new AttributeNode(expr, attr, expr.Line, expr.Column);
                    // 属性后可能是函数调用: expr.attr()
                    if (GetTokenType(Cur) == TokenType.LPAREN)
                    {
                        if (expr is AttributeNode callExpr)
                        {
                            Expect(TokenType.LPAREN);
                            var args = new List<ASTNode>();
                            if (GetTokenType(Cur) != TokenType.RPAREN)
                            {
                                args.Add(ParseExpression());
                                while (Match(TokenType.COMMA))
                                    args.Add(ParseExpression());
                            }
                            Expect(TokenType.RPAREN);
                            expr = new MethodCallNode(callExpr.Value, callExpr.Attr, args, callExpr.Line, callExpr.Column);
                        }
                    }
                    continue;
                }
                // 下标访问: expr[index]
                if (GetTokenType(Cur) == TokenType.LBRACKET)
                {
                    Advance();
                    var index = ParseExpression();
                    Expect(TokenType.RBRACKET);
                    expr = new SubscriptNode(expr, index, expr.Line, expr.Column);
                    continue;
                }
                break;
            }
            return expr;
        }

        private ASTNode ParseCall(string name, int line, int column)
        {
            Expect(TokenType.LPAREN);
            var args = new List<ASTNode>();
            
            if (GetTokenType(Cur) != TokenType.RPAREN)
            {
                args.Add(ParseExpression());
                while (Match(TokenType.COMMA))
                {
                    args.Add(ParseExpression());
                }
            }
            
            Expect(TokenType.RPAREN);
            return new CallNode(name, args, line, column);
        }

        /// <summary>
        /// 解析f-string
        /// </summary>
        private ASTNode ParseFString(string fstringValue, int line, int column)
        {
            var parts = new List<object>();
            int i = 0;
            
            while (i < fstringValue.Length)
            {
                if (fstringValue[i] == '{' && i + 1 < fstringValue.Length && fstringValue[i + 1] == '{')
                {
                    // 转义的 {{
                    parts.Add("{"); // 添加单个 {
                    i += 2;
                }
                else if (fstringValue[i] == '}' && i + 1 < fstringValue.Length && fstringValue[i + 1] == '}')
                {
                    // 转义的 }}
                    parts.Add("}"); // 添加单个 }
                    i += 2;
                }
                else if (fstringValue[i] == '{')
                {
                    // 表达式开始
                    int start = i + 1;
                    int depth = 1;
                    i++;
                    
                    while (i < fstringValue.Length && depth > 0)
                    {
                        if (fstringValue[i] == '{')
                            depth++;
                        else if (fstringValue[i] == '}')
                            depth--;
                        i++;
                    }
                    
                    if (depth > 0)
                        throw Error($"未终止的 f-string 表达式");
                    
                    // 提取表达式（不包括最后的}）
                    string exprStr = fstringValue.Substring(start, i - start - 1);
                    
                    // 简化：将表达式作为字符串处理（实际应该解析表达式）
                    // 为了简化，我们暂时将其作为字符串常量
                    parts.Add(new ConstantNode(exprStr, "str", line, column));
                }
                else
                {
                    // 普通字符串片段
                    int start = i;
                    while (i < fstringValue.Length && fstringValue[i] != '{' && fstringValue[i] != '}')
                    {
                        i++;
                    }
                    string strPart = fstringValue.Substring(start, i - start);
                    if (!string.IsNullOrEmpty(strPart))
                        parts.Add(strPart);
                }
            }
            
            return new FStringNode(parts, line, column);
        }

        private ASTNode ParseList()
        {
            var token = Expect(TokenType.LBRACKET);
            var elements = new List<ASTNode>();

            if (GetTokenType(Cur) != TokenType.RBRACKET)
            {
                var firstExpr = ParseExpression();
                // List comprehension: [expr for var in iter if cond]
                if (Match(TokenType.FOR))
                {
                    string loopVar = Expect(TokenType.IDENTIFIER).Value;
                    Expect(TokenType.IN);
                    var iterExpr = ParseExpression();
                    ASTNode? ifCond = null;
                    if (Match(TokenType.IF))
                        ifCond = ParseExpression();
                    Expect(TokenType.RBRACKET);
                    return new ListCompNode(firstExpr, loopVar, iterExpr, ifCond, token.Line, token.Column);
                }
                elements.Add(firstExpr);
                while (Match(TokenType.COMMA))
                {
                    elements.Add(ParseExpression());
                }
            }

            Expect(TokenType.RBRACKET);
            return new ListNode(elements, token.Line, token.Column);
        }
    }
}
