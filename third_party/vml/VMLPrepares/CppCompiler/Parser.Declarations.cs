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
                // `[]`（不给维度）时**从初始化器推断元素个数**（C 的语义）。
                // ⚠ 此前一律给 1 ⇒ `int a[] = {1,2,3};` 只分到 `int[2]`（头 + 1 个元素），
                //   后两个元素**静默丢掉**；`char s[] = "abc"` 同理（见 InferArrayElements）。
                int totalElems = arraySize is IntLiteral il
                    ? il.Value
                    : (isArray ? InferArrayElements(init) : 0);
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
            // ⚠ 这里是**逗号表达式**唯一真正常出现的地方（`a(), b(), c();`），
            //   所以用 `ParseCommaExpression` 而不是 `ParseExpression` —— 后者
            //   把逗号留给"实参分隔符"语义，用它在这里会直接报「期望 SEMICOLON，
            //   实际得到 COMMA」。实测两个经典 BGI 游戏都这么写（Turbo C 时代
            //   把几条语句挤一行的习惯）。
            var expr = ParseCommaExpression();
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
            else if (!Check(TokenType.SEMICOLON)) { init = new ExprStmt { Expression = ParseCommaExpression() }; Expect(TokenType.SEMICOLON); }
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
            if (!Check(TokenType.RPAREN)) incr = ParseCommaExpression();   /* `for(;;i++,j--)` 合法 */
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
            if (!Check(TokenType.SEMICOLON)) val = ParseCommaExpression();
            Expect(TokenType.SEMICOLON);
            return new ReturnStmt { Value = val };
        }

        // Expression parsing (precedence climbing)

        /// <summary>
        /// **赋值的父级**：`ParseExpression()` 保持原语义（**逗号是分隔符**），
        /// 本函数才是"逗号表达式"那一层。
        ///
        /// ⚠ 两者必须分开，不能像 C 前端那样直接让 `ParseExpression` 吃掉逗号 ——
        ///   本文件的 `ParseExpression()` **被当参数解析用**
        ///   （`call.Arguments.Add(ParseExpression())`，见 `ParsePostfix`），
        ///   逗号在那里是**实参分隔符**，吃掉它会把 `f(a, b)` 解析成"一个逗号表达式参数"。
        ///   所以只在"这里不可能有分隔符语义"的地方改用本函数：
        ///   表达式语句、`for` 的初始化/步进、`return` 的值。
        /// </summary>
        private Expr ParseCommaExpression()
        {
            var expr = ParseAssignment();
            while (Match(TokenType.COMMA))
            {
                var right = ParseAssignment();
                expr = new CommaExpr { Left = expr, Right = right };
            }
            return expr;
        }

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

        /// <summary>
        /// 是不是「按进制的整数字面量」—— `0x1F` / `0b1010` / `010`。
        ///
        /// 判据只看**字面量的形状**，不猜类型：C++ 里 `0x` / `0b` / 前导 `0` 开头的
        /// **必定是整数**，`0.5` 这类因为含 `.` 天然落不进来（`All(char.IsDigit)` 拦住）。
        /// </summary>
        private static bool IsRadixLiteral(string s)
        {
            if (s.Length < 2 || s[0] != '0') return false;
            char c = s[1];
            if (c == 'x' || c == 'X' || c == 'b' || c == 'B') return true;
            // 八进制：前导 0 + 全是八进制数字（`08` 不是合法八进制，落回十进制即可）
            for (int i = 1; i < s.Length; i++)
                if (s[i] < '0' || s[i] > '7') return false;
            return true;
        }

        /// <summary>
        /// 解析按进制的整数字面量。
        ///
        /// **一律回 <see cref="IntLiteral"/>（unchecked 32 位）**，不回 LongLiteral：
        /// VML 是 32 位 VM，而超出 int 范围的十六进制字面量在真实代码里几乎都是**位掩码**
        /// （`0xFFFFFFFF` 要的就是全 1 那个位型，= -1），按位截断正是 C 的语义。
        /// 后缀（`u`/`U`/`l`/`L`）由词法器一并带进来，这里剥掉。
        /// </summary>
        private static int ParseRadixLiteral(string s)
        {
            if (s.Length < 2 || s[0] != '0') return 0;
            char c = s[1];
            int radix, start;
            if (c == 'x' || c == 'X') { radix = 16; start = 2; }
            else if (c == 'b' || c == 'B') { radix = 2; start = 2; }
            else { radix = 8; start = 1; }

            // 逐位累加（`uint` 自然回绕 ⇒ 只留低 32 位），**不用 Convert.***：
            // `Convert.ToUInt32` 碰到超过 8 位的十六进制（`0xFFFFFFFFFFFFFFFF`）会抛
            // OverflowException，而那种写法的本意就是「取低位当掩码」，抛异常反而更错。
            uint acc = 0;
            for (int i = start; i < s.Length; i++)
            {
                char ch = s[i];
                int d = ch >= '0' && ch <= '9' ? ch - '0'
                      : ch >= 'a' && ch <= 'f' ? ch - 'a' + 10
                      : ch >= 'A' && ch <= 'F' ? ch - 'A' + 10
                      : -1;
                if (d < 0 || d >= radix) break;   // 后缀 u/U/l/L 从这里退出
                acc = acc * (uint)radix + (uint)d;
            }
            return unchecked((int)acc);
        }

        private Expr ParsePrimaryCore()
        {
            if (Match(TokenType.NUMBER))
            {
                string val = Previous().Value;
                // ⚠ 按进制的字面量必须在浮点判定**之前**分流，两条 C++ 前端的老缺陷都在这条缝里：
                //   ① 浮点判据里的 `val.EndsWith("F")` 会把 **`0xFFFFFF`** 当成浮点后缀
                //      （`val.Contains('e')` 同理会误伤 `0xE5`）⇒ 整个字面量变成 FloatLiteral；
                //   ② 侥幸没中这两条的（如 `0x000000`）继续往下走 `int.TryParse("0x000000")` ——
                //      那个重载**不认 `0x` 前缀**，一律失败 ⇒ 落到本块末尾的 `new IntLiteral { Value = 0 }`，
                //      **静默变成 0**。
                //   两条合起来 = C++ 的十六进制字面量从来没有一个是解析对的（C 前端一直是好的）。
                //   症状见 `Lib/c/graphics.h` 的 BGI 调色板：`0xAA5500` 全变 0 ⇒ 画面全黑。
                if (IsRadixLiteral(val)) return new IntLiteral { Value = ParseRadixLiteral(val) };
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
                // ⚠ 括号里**逗号不是分隔符**（函数实参是 `ParsePostfix` 那条路单独解析的），
                //   所以用逗号表达式那一层。漏了它 `x = (a, b);` 报
                //   「期望 RPAREN，实际得到 COMMA」——与语句级那个缺口是同一件事的两半。
                var expr = ParseCommaExpression();
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
                // `true` / `false` 是 C++ 的**关键字**（bool 字面量），不是标识符。
                // ⚠ 词法器不认识它们 ⇒ 落到这里被当成变量名 ⇒ 报「未声明的变量 'true'」。
                //   实测老程序撞得很直接：DX Ball（BGI 打砖块）第 6 行就 `bool … = true;`。
                //   判据放在**解析器**而不是词法器：`#define true 1` 这类老写法会先被
                //   预处理器替换掉，走到这里的一定是真的字面量；而如果反过来在词法层
                //   把它定成关键字，`#define true 1` 之后的 `1` 仍然是数字，不受影响，
                //   但用户拿 `true` 当变量名的（C 里合法）会被无声改语义。
                if (name == "true") return new BoolLiteral { Value = true };
                if (name == "false") return new BoolLiteral { Value = false };
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
                // ⚠ **这条分支是死的**：括号在 `ParsePrimaryCore` 更靠上的
                //   "Parenthesized expression or C-style cast" 那一支就已经被吃掉了。
                //   留着它是因为不确定还有没有别的调用路径会走到这儿；改括号语义
                //   请改**上面那一支**（实测：改了这里 `(a, b)` 照旧报错）。
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
                bool isLongLong = Match(TokenType.LONG);      // `long long`
                // ⚠ `long int` / `long double` 这**第二个关键字必须吃掉，但不能拼进类型串**。
                //   上面那段注释担心的事（"拼成 long int 会让 8 字节悄悄变 4 字节"）
                //   **方向要反过来看**：本平台的宽度表里 `long` 是 **4 字节**，
                //   而兜底那条是 `baseType.Contains("int") → 8 字节` ——
                //   所以拼成 "long int" 是**悄悄变宽**，比不解析更糟（不解析至少报错）。
                //   正解：C++ 里 `long int` 与 `long` **是同一个类型** ⇒ 吃掉 `int`、
                //   类型串仍是 `long` ⇒ 走宽度表里那条 4 字节 ✓。
                //   实测来自老 BGI 游戏（`long int score=0;`）。
                if (!isLongLong && Match(TokenType.INT)) { /* long int ≡ long */ }
                // `long double`：宽度表里没有这一档，按 `double`（8 字节）算 ——
                // 比落进兜底的 4 字节对。
                if (!isLongLong && Match(TokenType.DOUBLE)) type = "double";
                else type += isLongLong ? "long long" : "long";
                type = type.TrimEnd();
                while (Match(TokenType.STAR)) type += "*";
                return type;
            }
            if (Match(TokenType.SHORT))
            {
                // `short int` ≡ `short`，同理（宽度表里两串都列着，统一成短的）
                Match(TokenType.INT);
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
