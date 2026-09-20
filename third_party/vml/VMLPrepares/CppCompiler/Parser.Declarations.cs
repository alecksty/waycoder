using CompilerBase;
using VMLPlugins;

namespace CppCompiler
{
    public partial class Parser : ParserBase<Token, TokenType>
    {

        private Stmt ParseVarDeclList(string type)
        {
            // Handle pointer types: consume * modifiers and update the type string
            while (Match(TokenType.STAR)) type += "*";
            var stmts = new List<Expr>();
            do
            {
                // 函数指针声明: int (*fp)(int, int) 或 int (*fa[N])(int)
                bool isFuncPtr = false;
                if (Match(TokenType.LPAREN) && Check(TokenType.STAR))
                {
                    isFuncPtr = true;
                    Match(TokenType.STAR); // consume *
                    type += "(*)";
                }
                bool isRef = Match(TokenType.AMPERSAND);
                string name = Expect(TokenType.IDENTIFIER).Value;
                if (isFuncPtr)
                {
                    // 函数指针数组: (*fa[N]) — 在 ) 前解析维度
                    while (Match(TokenType.LBRACKET))
                    {
                        if (!Check(TokenType.RBRACKET)) ParseExpression();
                        Expect(TokenType.RBRACKET);
                    }
                    Expect(TokenType.RPAREN); // close (*name[N])
                    // Skip parameter list: (params)
                    if (Match(TokenType.LPAREN))
                    {
                        int depth = 1;
                        while (depth > 0 && !Check(TokenType.EOF))
                        {
                            if (Match(TokenType.LPAREN)) depth++;
                            else if (Match(TokenType.RPAREN)) depth--;
                            else Advance();
                        }
                    }
                }
                var dimensions = new List<Expr?>();
                bool isArray = false;
                while (Match(TokenType.LBRACKET))
                {
                    isArray = true;
                    if (!Check(TokenType.RBRACKET)) dimensions.Add(ParseExpression());
                    else dimensions.Add(null);
                    Expect(TokenType.RBRACKET);
                }
                Expr? arraySize = null;
                var dimValues = new List<int>();
                if (isArray && dimensions.Count > 0 && dimensions[0] != null)
                {
                    arraySize = new IntLiteral { Value = ComputeTotalElements(dimensions) };
                    foreach (var d in dimensions)
                        dimValues.Add(d is IntLiteral il2 ? il2.Value : 1);
                }
                Expr? init = null;
                if (Match(TokenType.ASSIGN))
                {
                    if (Check(TokenType.LBRACE)) init = ParseInitializerList();
                    else init = ParseExpression();
                }
                else if (Match(TokenType.LPAREN))
                {
                    // 构造函数风格初始化: Foo f(42)
                    init = ParseExpression();
                    while (Match(TokenType.COMMA)) { }
                    Expect(TokenType.RPAREN);
                }
                int totalElems = arraySize is IntLiteral il ? il.Value : (isArray ? 1 : 0);
                stmts.Add(new AssignExpr { Target = new IdentExpr { Name = name }, Value = init ?? new IntLiteral { Value = 0 }, DeclType = type, ArraySize = totalElems, Dimensions = dimValues });
            } while (Match(TokenType.COMMA));
            Expect(TokenType.SEMICOLON);
            if (stmts.Count == 1) return new ExprStmt { Expression = stmts[0] };
            var block = new BlockStmt();
            foreach (var s in stmts) block.Statements.Add(new ExprStmt { Expression = s });
            return block;
        }

        private Stmt ParseExprStmt()
        {
            var expr = ParseExpression();
            Expect(TokenType.SEMICOLON);
            return new ExprStmt { Expression = expr };
        }

        private Stmt ParseIf()
        {
            Expect(TokenType.LPAREN);
            var cond = ParseExpression();
            Expect(TokenType.RPAREN);
            var thenBr = ParseStatement();
            Stmt? elseBr = null;
            if (Match(TokenType.ELSE)) elseBr = ParseStatement();
            return new IfStmt { Condition = cond, ThenBranch = thenBr, ElseBranch = elseBr };
        }

        private Stmt ParseWhile()
        {
            Expect(TokenType.LPAREN);
            var cond = ParseExpression();
            Expect(TokenType.RPAREN);
            return new WhileStmt { Condition = cond, Body = ParseStatement() };
        }

        private Stmt ParseFor()
        {
            Expect(TokenType.LPAREN);
            // Range-based for: for (int x : vec) - detect by looking ahead for ':'
            int savePos = _pos;
            if (IsVarDecl())
            {
                int afterType = _pos;
                ParseType();
                if (Check(TokenType.IDENTIFIER))
                {
                    int afterVar = _pos;
                    _pos++;
                    bool hasColon = Check(TokenType.COLON);
                    _pos = afterVar;
                    if (hasColon)
                    {
                        _pos = afterType;
                        string vtype = ParseType();
                        string vname = Expect(TokenType.IDENTIFIER).Value;
                        Expect(TokenType.COLON);
                        var collection = ParseExpression();
                        Expect(TokenType.RPAREN);
                        var body = ParseStatement();
                        // Desugar range-for to standard for: for (int __i = 0; __i < coll.size(); __i++) { auto x = coll[__i]; body }
                        string iterVar = $"__range_i{_pos}";
                        var rfInit = new AssignExpr { DeclType = "int", Target = new IdentExpr { Name = iterVar }, Op = "=", Value = new IntLiteral { Value = 0 } };
                        var rfCond = new BinaryExpr { Left = new IdentExpr { Name = iterVar }, Op = "<", Right = new CallExpr { Callee = new MemberExpr { Object = collection, Member = "size" } } };
                        var rfIncr = new UnaryExpr { Op = "++", Operand = new IdentExpr { Name = iterVar } };
                        var rfLoopVar = new AssignExpr { DeclType = vtype, Target = new IdentExpr { Name = vname }, Op = "=", Value = new BinaryExpr { Left = collection, Op = "[]", Right = new IdentExpr { Name = iterVar } } };
                        var rfBody = new BlockStmt();
                        rfBody.Statements.Add(new ExprStmt { Expression = rfLoopVar });
                        if (body is BlockStmt bs) { foreach (var s in bs.Statements) rfBody.Statements.Add(s); }
                        else rfBody.Statements.Add(body);
                        return new ForStmt { Initializer = new ExprStmt { Expression = rfInit }, Condition = rfCond, Increment = rfIncr, Body = rfBody };
                    }
                }
            }
            _pos = savePos;
            Stmt? init = null;
            bool hasVarDecl = false;
            if (IsVarDecl()) { init = ParseVarDeclStmt(); hasVarDecl = true; }
            else if (!Check(TokenType.SEMICOLON)) { init = new ExprStmt { Expression = ParseExpression() }; Expect(TokenType.SEMICOLON); }
            else { Advance(); }
            Expr? cond = null;
            if (!hasVarDecl)
            {
                if (Check(TokenType.SEMICOLON)) cond = new IntLiteral { Value = 1 };
                else cond = ParseExpression();
                Expect(TokenType.SEMICOLON);
            }
            else
            {
                if (!Check(TokenType.SEMICOLON)) cond = ParseExpression();
                Expect(TokenType.SEMICOLON);
            }
            Expr? incr = null;
            if (!Check(TokenType.RPAREN)) incr = ParseExpression();
            Expect(TokenType.RPAREN);
            return new ForStmt { Initializer = init, Condition = cond, Increment = incr, Body = ParseStatement() };
        }

        private Stmt ParseDoWhile()
        {
            var body = ParseStatement();
            Expect(TokenType.WHILE);
            Expect(TokenType.LPAREN);
            var cond = ParseExpression();
            Expect(TokenType.RPAREN);
            Expect(TokenType.SEMICOLON);
            return new WhileStmt { Condition = cond, Body = body };
        }

        private Stmt ParseSwitch()
        {
            Expect(TokenType.LPAREN);
            var val = ParseExpression();
            Expect(TokenType.RPAREN);
            Expect(TokenType.LBRACE, "Switch");
            var sw = new SwitchStmt { Value = val };
            while (!Check(TokenType.RBRACE) && !IsAtEnd)
            {
                if (Match(TokenType.CASE))
                {
                    var cv = ParseExpression();
                    Expect(TokenType.COLON);
                    var sc = new SwitchCase { Value = cv };
                    while (!Check(TokenType.RBRACE) && !Check(TokenType.CASE) && !Check(TokenType.DEFAULT))
                    {
                        var s = ParseStatement();
                        if (s != null) sc.Body.Add(s);
                        if (s is BreakStmt) break;
                    }
                    sw.Cases.Add(sc);
                }
                else if (Match(TokenType.DEFAULT))
                {
                    Expect(TokenType.COLON);
                    var sc = new SwitchCase { Value = null };
                    while (!Check(TokenType.RBRACE) && !Check(TokenType.CASE) && !Check(TokenType.DEFAULT))
                    {
                        var s = ParseStatement();
                        if (s != null) sc.Body.Add(s);
                        if (s is BreakStmt) break;
                    }
                    sw.Cases.Add(sc);
                }
                else break;
            }
            Expect(TokenType.RBRACE);
            return sw;
        }

        private Stmt ParseReturn()
        {
            Expr? val = null;
            if (!Check(TokenType.SEMICOLON)) val = ParseExpression();
            Expect(TokenType.SEMICOLON);
            return new ReturnStmt { Value = val };
        }

        // Expression parsing (precedence climbing)
        public Expr ParseExpression() => ParseAssignment();

        private Expr ParseAssignment()
        {
            var expr = ParseConditional();
            if (Match(TokenType.ASSIGN, TokenType.ADD_ASSIGN, TokenType.SUB_ASSIGN,
                     TokenType.MUL_ASSIGN, TokenType.DIV_ASSIGN, TokenType.MOD_ASSIGN,
                     TokenType.AND_ASSIGN, TokenType.OR_ASSIGN, TokenType.XOR_ASSIGN,
                     TokenType.LSHIFT_ASSIGN, TokenType.RSHIFT_ASSIGN))
            {
                string op = Previous().Value;
                var val = ParseAssignment();
                return new AssignExpr { Target = expr, Op = op, Value = val };
            }
            return expr;
        }

        private Expr ParseConditional()
        {
            var expr = ParseLogicalOr();
            if (Match(TokenType.QUESTION))
            {
                var tv = ParseExpression();
                Expect(TokenType.COLON);
                var fv = ParseConditional();
                return new ConditionalExpr { Condition = expr, TrueExpr = tv, FalseExpr = fv };
            }
            return expr;
        }

        private Expr ParseLogicalOr()
        {
            var expr = ParseLogicalAnd();
            while (Match(TokenType.OR))
            {
                var right = ParseLogicalAnd();
                expr = new BinaryExpr { Left = expr, Op = "||", Right = right };
            }
            return expr;
        }

        private Expr ParseLogicalAnd()
        {
            var expr = ParseBitwiseOr();
            while (Match(TokenType.AND))
            {
                var right = ParseBitwiseOr();
                expr = new BinaryExpr { Left = expr, Op = "&&", Right = right };
            }
            return expr;
        }

        private Expr ParseBitwiseOr()
        {
            var expr = ParseBitwiseXor();
            while (Match(TokenType.PIPE))
            {
                var right = ParseBitwiseXor();
                expr = new BinaryExpr { Left = expr, Op = "|", Right = right };
            }
            return expr;
        }

        private Expr ParseBitwiseXor()
        {
            var expr = ParseBitwiseAnd();
            while (Match(TokenType.CARET))
            {
                var right = ParseBitwiseAnd();
                expr = new BinaryExpr { Left = expr, Op = "^", Right = right };
            }
            return expr;
        }

        private Expr ParseBitwiseAnd()
        {
            var expr = ParseEquality();
            while (Match(TokenType.AMPERSAND))
            {
                var right = ParseEquality();
                expr = new BinaryExpr { Left = expr, Op = "&", Right = right };
            }
            return expr;
        }

        private Expr ParseEquality()
        {
            var expr = ParseComparison();
            while (Match(TokenType.EQ, TokenType.NE))
            {
                string op = Previous().Value;
                var right = ParseComparison();
                expr = new BinaryExpr { Left = expr, Op = op, Right = right };
            }
            return expr;
        }

        private Expr ParseComparison()
        {
            var expr = ParseShift();
            while (Match(TokenType.LT, TokenType.LE, TokenType.GT, TokenType.GE))
            {
                string op = Previous().Value;
                var right = ParseShift();
                expr = new BinaryExpr { Left = expr, Op = op, Right = right };
            }
            return expr;
        }

        private Expr ParseShift()
        {
            var expr = ParseTerm();
            while (Match(TokenType.LSHIFT, TokenType.RSHIFT))
            {
                string op = Previous().Value;
                var right = ParseTerm();
                expr = new BinaryExpr { Left = expr, Op = op, Right = right };
            }
            return expr;
        }

        private Expr ParseTerm()
        {
            var expr = ParseFactor();
            while (Match(TokenType.PLUS, TokenType.MINUS))
            {
                string op = Previous().Value;
                var right = ParseFactor();
                expr = new BinaryExpr { Left = expr, Op = op, Right = right };
            }
            return expr;
        }

        private Expr ParseFactor()
        {
            var expr = ParseUnary();
            while (Match(TokenType.STAR, TokenType.SLASH, TokenType.PERCENT))
            {
                string op = Previous().Value;
                var right = ParseUnary();
                expr = new BinaryExpr { Left = expr, Op = op, Right = right };
            }
            return expr;
        }

        private Expr ParseUnary()
        {
            if (Match(TokenType.PLUS, TokenType.MINUS, TokenType.NOT, TokenType.TILDE,
                     TokenType.INCREMENT, TokenType.DECREMENT, TokenType.STAR, TokenType.AMPERSAND))
            {
                string op = Previous().Value;
                var operand = ParseUnary();
                return new UnaryExpr { Op = op, Operand = operand };
            }
            if (Match(TokenType.SIZEOF)) return ParseSizeof();
            if (Match(TokenType.NEW)) return ParseNew();
            if (Match(TokenType.DELETE)) return ParseDelete();
            return ParsePostfix();
        }

        private Expr ParsePostfix()
        {
            var expr = ParsePrimary();
            while (true)
            {
                if (Match(TokenType.LPAREN))
                {
                    var call = new CallExpr { Callee = expr };
                    if (!Check(TokenType.RPAREN))
                    {
                        do { call.Arguments.Add(ParseExpression()); } while (Match(TokenType.COMMA));
                    }
                    Expect(TokenType.RPAREN);
                    expr = call;
                }
                else if (Match(TokenType.LBRACKET))
                {
                    var idx = ParseExpression();
                    Expect(TokenType.RBRACKET);
                    expr = new BinaryExpr { Left = expr, Op = "[]", Right = idx };
                }
                else if (Match(TokenType.DOT))
                {
                    string member;
                    if (Match(TokenType.OPERATOR))
                        member = "operator" + ParseOperatorSymbol();
                    else
                        member = Expect(TokenType.IDENTIFIER).Value;
                    expr = new MemberExpr { Object = expr, Member = member };
                }
                else if (Match(TokenType.ARROW))
                {
                    string member;
                    if (Match(TokenType.OPERATOR))
                        member = "operator" + ParseOperatorSymbol();
                    else
                        member = Expect(TokenType.IDENTIFIER).Value;
                    expr = new MemberExpr { Object = expr, Member = member, Arrow = true };
                }
                else if (Match(TokenType.INCREMENT, TokenType.DECREMENT))
                {
                    expr = new UnaryExpr { Op = Previous().Value + "post", Operand = expr };
                }
                else break;
            }
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
        private Expr ParsePrimary()
        {
            var __start = Cur;
            int __line = __start.Line, __origLine = __start.OriginalLine, __col = __start.Column;
            var __node = ParsePrimaryCore();
            if (__node != null && __node.Line == 0) { __node.Line = __line; __node.OriginalLine = __origLine; __node.Column = __col; }
            return __node;
        }

        private Expr ParsePrimaryCore()
        {
            if (Match(TokenType.NUMBER))
            {
                string val = Previous().Value;
                if (val.Contains('.') || val.Contains('e') || val.Contains('E') || val.EndsWith("f") || val.EndsWith("F"))
                {
                    bool hasFSuffix = val.EndsWith("f") || val.EndsWith("F");
                    return new FloatLiteral { Value = double.TryParse(val.TrimEnd('f', 'F', 'l', 'L'), out var dv) ? dv : 0.0, IsFloatSuffix = hasFSuffix };
                }
                // 处理整数后缀 (L/l, LL/ll, UL/ul, ULL/ull, U/u 等)
                string numStr = val.TrimEnd('L', 'l', 'U', 'u');
                bool hasLongSuffix = val.EndsWith("ll", StringComparison.OrdinalIgnoreCase)
                    || (val.EndsWith("l", StringComparison.OrdinalIgnoreCase) && !val.EndsWith("ul", StringComparison.OrdinalIgnoreCase));
                if (hasLongSuffix || !int.TryParse(numStr, out _))
                {
                    // 64-bit long literal
                    if (long.TryParse(numStr, out var lv))
                        return new LongLiteral { Value = lv };
                }
                if (long.TryParse(numStr, out var lv2))
                    return new IntLiteral { Value = (int)lv2 };
                return new IntLiteral { Value = 0 };
            }
            if (Match(TokenType.STRING)) return new StringLiteral { Value = Previous().Value };
            if (Match(TokenType.WSTRING)) return new StringLiteral { Value = Previous().Value, Width = 16 };
            if (Match(TokenType.USTRING)) return new StringLiteral { Value = Previous().Value, Width = 32 };
            if (Match(TokenType.WCHAR_LITERAL)) { string sv = Previous().Value; return new CharLiteral { Value = sv.Length >= 1 ? sv[0] : '\0' }; }
            if (Match(TokenType.UCHAR_LITERAL)) { string sv = Previous().Value; return new CharLiteral { Value = sv.Length >= 1 ? sv[0] : '\0' }; }
            if (Match(TokenType.CHAR_LITERAL))
            {
                string sv = Previous().Value;
                return new CharLiteral { Value = sv.Length >= 1 ? sv[0] : '\0' };
            }
            if (Match(TokenType.THIS)) return new ThisExpr();
            if (Match(TokenType.NULLPTR)) return new IntLiteral { Value = 0 };
            // C++ casts: static_cast<Type>(expr)
            if (Check(TokenType.IDENTIFIER) && (Cur.Value == "static_cast" || Cur.Value == "dynamic_cast" || Cur.Value == "reinterpret_cast" || Cur.Value == "const_cast"))
            {
                Advance();
                string castName = Cur.Value;
                string targetType = ParseTemplateArgs(); // parse <Type>
                Expect(TokenType.LPAREN);
                var expr = ParseExpression();
                Expect(TokenType.RPAREN);
                return new CastExpr { TargetType = targetType, Expression = expr };
            }
            // typeid(expr)
            if (Check(TokenType.IDENTIFIER) && Cur.Value == "typeid")
            {
                Advance();
                Expect(TokenType.LPAREN);
                var expr = ParseExpression();
                Expect(TokenType.RPAREN);
                return new TypeIdExpr { Expression = expr };
            }
            // Parenthesized expression or C-style cast: (expr) or (Type)expr
            if (Match(TokenType.LPAREN))
            {
                // Try C-style cast: save position, check if content is a type
                int savePos = _pos;
                bool isType = IsTypeToken();
                if (isType)
                {
                    // Looks like a type — parse as C-style cast
                    string castType = ParseType();
                    if (Check(TokenType.RPAREN))
                    {
                        Advance(); // consume )
                        var castExpr = ParseUnary();
                        return new CastExpr { TargetType = castType, Expression = castExpr };
                    }
                }
                // Not a cast — parse as parenthesized expression
                _pos = savePos;
                var expr = ParseExpression();
                Expect(TokenType.RPAREN);
                return expr;
            }
            // Lambda: [capture](params) { body }
            if (GetTokenType(Cur) == TokenType.LBRACKET)
            {
                Advance();
                bool isLambda = Check(TokenType.AMPERSAND) || Check(TokenType.ASSIGN) || Check(TokenType.IDENTIFIER) || Check(TokenType.THIS);
                if (!isLambda && Check(TokenType.RBRACKET))
                {
                    Advance();
                    isLambda = Check(TokenType.LPAREN) || Check(TokenType.LBRACE);
                    _pos--; // back to RBRACKET
                }
                if (isLambda)
                {
                    bool hasCapture = Check(TokenType.AMPERSAND) || Check(TokenType.ASSIGN)
                        || Check(TokenType.IDENTIFIER) || Check(TokenType.THIS);
                    SkipTo(TokenType.RBRACKET);
                    Expect(TokenType.RBRACKET);
                    var lambda = new LambdaExpr { HasCapture = hasCapture };
                    if (Match(TokenType.LPAREN))
                    {
                        if (!Check(TokenType.RPAREN))
                        {
                            do
                            {
                                string pt = ParseType();
                                string pn = Expect(TokenType.IDENTIFIER).Value;
                                lambda.Parameters.Add(pn);
                            } while (Match(TokenType.COMMA));
                        }
                        Expect(TokenType.RPAREN);
                    }
                    Expect(TokenType.LBRACE, "Lambda");
                    lambda.Body = ParseBlock();
                    return lambda;
                }
                _pos--; // put back LBRACKET, let ParsePostfix handle as subscript
            }
            if (Match(TokenType.IDENTIFIER))
            {
                string name = Previous().Value;
                // 处理限定名: Math::square → Math_square
                while (Match(TokenType.SCOPE_RESOLVE))
                {
                    name += "_";
                    if (Match(TokenType.IDENTIFIER)) name += Previous().Value;
                    else if (Check(TokenType.LT)) name += ParseTemplateArgs();
                    else break;
                }
                return new IdentExpr { Name = name };
            }
            if (Match(TokenType.LPAREN))
            {
                var expr = ParseExpression();
                Expect(TokenType.RPAREN);
                return expr;
            }
            if (Check(TokenType.LBRACE)) return ParseInitializerList();
            // ⚠ **兜底不能"静默当成 0"** —— 这一行就是本仓反复记的那个形态的最坏版本。
            //
            // 这里对**任何认不出的 token** 直接造一个 `IntLiteral(0)` 顶上，一句话都不报。
            // 于是 `int c = a + ;` 被编成 `a + 0`：**编译成功、程序照跑、结果是错的**。
            // 比崩掉更糟 —— 崩了至少用户知道出事了；这个连提示都没有。
            // 实测（DiagProbe【语法错误】档 cpp 一栏）修之前是 **NOERR**。
            //
            // 与 C 前端同一处置：**报出来**（`GccError` 收集、不抛）再返回占位 0。
            // 收集而非抛出是刻意的：解析器还能往下走，同一份文件里后面几处错也能一起报出来。
            GccError($"表达式缺失或多余（遇到 '{Cur.Value ?? Cur.Type.ToString()}'）",
                ErrorCode.Parser_SyntaxError);
            return new IntLiteral { Value = 0 };
        }

        private Expr ParseInitializerList()
        {
            Expect(TokenType.LBRACE, "InitList");
            var list = new InitializerListExpr();
            if (!Check(TokenType.RBRACE))
            {
                do
                {
                    if (Check(TokenType.LBRACE)) list.Elements.Add(ParseInitializerList());
                    else list.Elements.Add(ParseExpression());
                } while (Match(TokenType.COMMA));
            }
            Expect(TokenType.RBRACE);
            return list;
        }

        private Expr ParseSizeof()
        {
            if (Match(TokenType.LPAREN))
            {
                // sizeof(type) or sizeof(expr)
                if (IsTypeToken()) { string tn = ParseType(); Expect(TokenType.RPAREN); return new SizeofExpr { TypeName = tn }; }
                var e = ParseExpression();
                Expect(TokenType.RPAREN);
                return new SizeofExpr { Expression = e };
            }
            if (Check(TokenType.IDENTIFIER)) { return new SizeofExpr { TypeName = Cur.Value }; }
            return new IntLiteral { Value = 4 };
        }

        private Expr ParseNew()
        {
            string type = ParseType();
            Expr? size = null;
            if (Match(TokenType.LBRACKET)) { size = ParseExpression(); Expect(TokenType.RBRACKET); }
            // new int(42) — constructor-style initializer
            if (Match(TokenType.LPAREN))
            {
                var init = ParseExpression();
                Expect(TokenType.RPAREN);
                return new NewExpr { Type = type, Size = size, Init = new List<Expr> { init } };
            }
            return new NewExpr { Type = type, Size = size };
        }

        private Expr ParseDelete()
        {
            bool isArray = Match(TokenType.LBRACKET);
            if (isArray) Expect(TokenType.RBRACKET);
            var target = ParseUnary();
            return new DeleteExpr { Target = target, IsArray = isArray };
        }

        // Type parsing
        private string ParseType()
        {
            string type = "";
            if (Match(TokenType.CONST, TokenType.STATIC, TokenType.EXTERN, TokenType.REGISTER, TokenType.AUTO, TokenType.VOLATILE, TokenType.RESTRICT))
                return ParseType(); // skip qualifiers
            if (Match(TokenType.UNSIGNED)) type = "unsigned ";
            if (Match(TokenType.SIGNED)) type = "signed ";
            // ⚠ `long` / `short` **必须也吃掉后面的 `*`** 再返回。
            //   这两支原本是提前 `return` 的（为的是不让下面的 `Match(INT)` 去啃
            //   `long int` 里的那个 `int`），可末行那个 `while (Match(STAR))` 于是
            //   永远走不到 ⇒ `long* v` 解析成类型 "long"、紧跟着的 `*` 留给调用方，
            //   调用方在参数位 `Expect(IDENTIFIER)` 撞上 STAR 当场报
            //   「期望 IDENTIFIER，实际得到 STAR」。
            //   实测只有 `long` / `short` 中招（`int*`/`float*`/`double*`/`char*` 都
            //   落到末行、本来就是对的），而 `Lib/c/waycoder_ui.h` 里的
            //   `long callwithlong4(long* v);` 正好踩在 `long` 上 ⇒
            //   `Examples/cpp/sysinfo.cpp` 编不过。
            //   这里**只补指针后缀、不改成 fall-through**：换成 fall-through 会让
            //   `long int` 变成类型串 "long int"，而 C++ 后端的判宽是
            //   `baseType.Contains("int")` ⇒ 4 字节悄悄变 8 字节（见
            //   CodeGenerator.Expressions.cs:77-78）。那是另一件事，别混进来。
            //   （拼指针前先 `TrimEnd` —— 否则会拼出 `"long *"`，与末行那条路拼出的
            //     `"int*"` 形态不一致，下游按字符串比类型就会漏。）
            if (Match(TokenType.LONG))
            {
                type += "long ";
                if (Match(TokenType.LONG)) type += "long ";
                type = type.TrimEnd();
                while (Match(TokenType.STAR)) type += "*";
                return type;
            }
            if (Match(TokenType.SHORT))
            {
                type += "short ";
                type = type.TrimEnd();
                while (Match(TokenType.STAR)) type += "*";
                return type;
            }
            if (Match(TokenType.INT)) type += "int";
            else if (Match(TokenType.CHAR)) type += "char";
            else if (Match(TokenType.FLOAT)) type += "float";
            else if (Match(TokenType.DOUBLE)) type += "double";
            else             if (Match(TokenType.VOID)) type += "void";
            else if (Match(TokenType.BOOL)) type += "bool";
            else if (Match(TokenType.ENUM)) { type += "enum"; if (Check(TokenType.IDENTIFIER)) { type += " " + Advance().Value; } }
            else if (Match(TokenType.STRUCT)) { if (Check(TokenType.IDENTIFIER)) { type += Advance().Value; } else type += "struct"; }
            else if (Match(TokenType.UNION)) { if (Check(TokenType.IDENTIFIER)) { type += Advance().Value; } else type += "union"; }
            else if (Match(TokenType.AUTO)) type += "auto";
            else if (Match(TokenType.NULLPTR)) type += "int";
            else if (Match(TokenType.SIZE_T)) type += "size_t";
            else if (Match(TokenType.SSIZE_T)) type += "ssize_t";
            else if (Match(TokenType.PTRDIFF_T)) type += "ptrdiff_t";
            else if (Match(TokenType.INT8)) type += "int8_t";
            else if (Match(TokenType.INT16)) type += "int16_t";
            else if (Match(TokenType.INT32)) type += "int32_t";
            else if (Match(TokenType.INT64)) type += "int64_t";
            else if (Match(TokenType.UINT8)) type += "uint8_t";
            else if (Match(TokenType.UINT16)) type += "uint16_t";
            else if (Match(TokenType.UINT32)) type += "uint32_t";
            else if (Match(TokenType.UINT64)) type += "uint64_t";
            else if (Match(TokenType.INTPTR_T)) type += "intptr_t";
            else if (Match(TokenType.UINTPTR_T)) type += "uintptr_t";
            else if (!string.IsNullOrEmpty(type.Trim()))
            {
                // type already has qualifiers (unsigned/signed/long/short), don't consume IDENTIFIER as type
            }
            else if (Match(TokenType.IDENTIFIER))
            {
                type += Previous().Value;
                // 结构体别名换成**标签**（`typedef struct tm tm_t;` ⇒ `tm_t` 当归于 `tm`）。
                // 下游按标签查结构定义（`CleanType(type)` → `_classes`），不换就查不到 ——
                // 症状是 `tm_t *p; p->tm_sec` 解析不出成员（登记了别名才真的"能用"）。
                // ⚠ 只换**结构体别名**：`typedef unsigned int time_t;` 这类基本类型别名不动，
                //   免得改动既有程序里的类型串（那是另一件事，收益不明、风险却不小）。
                if (_structAliases.TryGetValue(type, out var structTag)) type = structTag;
                // Handle qualified names: std::string → std_string
                while (Match(TokenType.SCOPE_RESOLVE))
                {
                    type += "_";
                    if (Match(TokenType.IDENTIFIER)) type += Previous().Value;
                    else if (Check(TokenType.LT)) type += "_" + ParseTemplateArgs();
                }
                // Handle template type: vector<int> → vector_int
                if (Check(TokenType.LT))
                {
                    type += "_" + ParseTemplateArgs();
                }
            }
            else return "int";
            // "unsigned" / "signed" 单独使用时默认加 int
            if (type == "unsigned " || type == "signed ") type += "int";
            while (Match(TokenType.STAR)) type += "*";
            return type.Trim();
        }

        private string ParseTemplateArgs()
        {
            Expect(TokenType.LT);
            string result = "";
            int depth = 1;
            while (depth > 0 && !IsAtEnd)
            {
                if (Match(TokenType.LT)) { result += "_"; depth++; }
                else if (Match(TokenType.GT)) { depth--; if (depth > 0) result += "_"; }
                else if (Match(TokenType.COMMA)) { result += "_"; }
                else if (Check(TokenType.RSHIFT))
                {
                    Advance();
                    if (depth >= 2) depth -= 2;
                    else depth--; // treat >> as single > when closing current level
                }
                else if (Match(TokenType.IDENTIFIER))
                {
                    string name = Previous().Value;
                    // Handle qualified names: std::vector
                    while (Match(TokenType.SCOPE_RESOLVE))
                    {
                        name += "_";
                        if (Match(TokenType.IDENTIFIER)) name += Previous().Value;
                    }
                    // Nested template: vector<vector<int>>
                    if (Check(TokenType.LT))
                        result += name + "_" + ParseTemplateArgs();
                    else
                        result += name;
                }
                else if (Match(TokenType.INT)) result += "int";
                else if (Match(TokenType.CHAR)) result += "char";
                else if (Match(TokenType.FLOAT)) result += "float";
                else if (Match(TokenType.DOUBLE)) result += "double";
                else if (Match(TokenType.BOOL)) result += "bool";
                else if (Match(TokenType.STRING)) result += "string";
                else if (Match(TokenType.NUMBER)) result += Previous().Value;
                else Advance();
            }
            return result;
        }

        private bool IsTypeToken()
        {
            return Check(TokenType.INT, TokenType.CHAR, TokenType.FLOAT, TokenType.DOUBLE,
                TokenType.SHORT, TokenType.LONG, TokenType.SIGNED, TokenType.UNSIGNED,
                TokenType.VOID, TokenType.BOOL, TokenType.CONST, TokenType.STATIC,
                TokenType.EXTERN, TokenType.REGISTER, TokenType.AUTO, TokenType.VOLATILE,
                TokenType.ENUM, TokenType.STRUCT, TokenType.UNION, TokenType.TYPEDEF,
                TokenType.SIZE_T, TokenType.SSIZE_T, TokenType.PTRDIFF_T,
                TokenType.INT8, TokenType.INT16, TokenType.INT32, TokenType.INT64,
                TokenType.UINT8, TokenType.UINT16, TokenType.UINT32, TokenType.UINT64,
                TokenType.INTPTR_T, TokenType.UINTPTR_T);
        }

        private void SkipTemplate()
        {
            ParseTemplateArgs(); // just consume and discard
        }

        private ExternBlock ParseExternBlock()
        {
            string linkage = "C";
            if (Match(TokenType.STRING)) linkage = Previous().Value.Trim('"');
            var block = new ExternBlock { Linkage = linkage };
            if (Match(TokenType.LBRACE))
            {
                while (!Check(TokenType.RBRACE) && !IsAtEnd)
                {
                    var decl = ParseDeclaration();
                    if (decl != null)
                        block.Members.Add(decl);
                    else if (!Match(TokenType.SEMICOLON))
                        Advance();
                }
                Expect(TokenType.RBRACE, "期望 '}' 在 extern 块后");
            }
            else
            {
                // extern "C" without braces — parse single declaration
                var decl = ParseDeclaration();
                if (decl != null)
                    block.Members.Add(decl);
            }
            return block;
        }

    }
}
