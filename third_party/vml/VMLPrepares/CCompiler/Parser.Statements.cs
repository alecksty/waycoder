using CompilerBase;
#nullable disable // auto-generated

using System.Collections.Generic;

namespace CCompiler
{
    public partial class Parser : ParserBase<Token, TokenType>
    {
        private ASTNode ParseStatement()
        {
            // 处理enum、struct和union定义（局部作用域）
            if (Current().Type == TokenType.ENUM)
            {
                if (ParseEnum())
                {
                    return new ExpressionStatement(null); // 返回空语句
                }
                // 否则继续解析变量声明
            }
            else if (Current().Type == TokenType.STRUCT)
            {
                var result = ParseStructWithDecl();
                if (result != null)
                {
                    return result;
                }
                return new ExpressionStatement(null);
            }
            else if (Current().Type == TokenType.UNION)
            {
                var result = ParseUnionWithDecl();
                if (result != null)
                {
                    return result;
                }
                return new ExpressionStatement(null);
            }

            // __chipasm__("arch", "code") — 内联架构专属汇编
            // __chipasm__("code") — 简化形式(默认架构vml)
            if (Current().Type == TokenType.CHIPASM)
            {
                Advance(); // consume __chipasm__
                Expect(TokenType.LPAREN);
                string arch = "vml";
                string code;
                string firstArg = Expect(TokenType.STRING).Value?.ToString() ?? "";
                if (Peek().Type == TokenType.COMMA)
                {
                    // 双参数形式: __chipasm__("arch", "code")
                    Advance(); // consume comma
                    arch = firstArg;
                    code = Expect(TokenType.STRING).Value?.ToString() ?? "";
                }
                else
                {
                    // 单参数形式: __chipasm__("code")
                    code = firstArg;
                }
                Expect(TokenType.RPAREN);
                Expect(TokenType.SEMICOLON);
                // Generate as comment (codegen will handle)
                return new ExpressionStatement(null);
            }

            // 检查是否为函数调用（标识符后跟左括号）
            if (Current().Type == TokenType.IDENTIFIER && Peek(1).Type == TokenType.LPAREN)
            {
                ASTNode functionCallExpr = ParseExpression();
                Expect(TokenType.SEMICOLON);
                return new ExpressionStatement(functionCallExpr);
            }

            // 检查是否为赋值语句或数组访问赋值（标识符后跟等号或左括号）
            if (Current().Type == TokenType.IDENTIFIER && (Peek(1).Type == TokenType.ASSIGN || Peek(1).Type == TokenType.LBRACKET))
            {
                ASTNode assignmentExpr = ParseExpression();
                Expect(TokenType.SEMICOLON);
                return new ExpressionStatement(assignmentExpr);
            }

            // 检查是否为结构体成员赋值（如 p.x = 10; 或 p->x = 10;）
            if (Current().Type == TokenType.IDENTIFIER &&
                (Peek(1).Type == TokenType.DOT || Peek(1).Type == TokenType.ARROW))
            {
                ASTNode assignmentExpr = ParseExpression();
                Expect(TokenType.SEMICOLON);
                return new ExpressionStatement(assignmentExpr);
            }

            // 检查是否为复合赋值语句（如 i += 1; i &= 0x0F; 等）
            if (Current().Type == TokenType.IDENTIFIER &&
                (Peek(1).Type == TokenType.ADD_ASSIGN    || Peek(1).Type == TokenType.SUB_ASSIGN   ||
                 Peek(1).Type == TokenType.MUL_ASSIGN    || Peek(1).Type == TokenType.DIV_ASSIGN   ||
                 Peek(1).Type == TokenType.MOD_ASSIGN    || Peek(1).Type == TokenType.AND_ASSIGN   ||
                 Peek(1).Type == TokenType.OR_ASSIGN     || Peek(1).Type == TokenType.XOR_ASSIGN   ||
                 Peek(1).Type == TokenType.LSHIFT_ASSIGN || Peek(1).Type == TokenType.RSHIFT_ASSIGN))
            {
                ASTNode assignmentExpr = ParseExpression();
                Expect(TokenType.SEMICOLON);
                return new ExpressionStatement(assignmentExpr);
            }

            // 变量声明
            // 首先检查是否有存储类说明符或 interrupt
            Token storageClassToken = null;
            if (Current().Type == TokenType.INTERRUPT)
            {
                Advance();
            }
            if (Current().Type == TokenType.AUTO   || Current().Type == TokenType.REGISTER ||
                Current().Type == TokenType.STATIC || Current().Type == TokenType.EXTERN   ||
                Current().Type == TokenType.TYPEDEF)
            {
                storageClassToken = Advance();
            }

            // 解析类型说明符
            string typeName = ParseTypeSpecifiers();

            // 支持 struct/union 局部变量声明
            if (Current().Type == TokenType.STRUCT || Current().Type == TokenType.UNION)
            {
                typeName = (string.IsNullOrEmpty(typeName) ? "" : typeName + " ") + Advance().Value.ToString();
                if (Current().Type == TokenType.IDENTIFIER)
                    typeName += " " + Advance().Value.ToString();
            }
            // ParseTypeSpecifiers 可能已消费 struct 关键字 → 单独处理匿名体
            if (Current().Type == TokenType.LBRACE && typeName != null &&
                (typeName.StartsWith("struct") || typeName.Contains(" struct") ||
                 typeName.StartsWith("union") || typeName.Contains(" union")))
            {
                Advance(); // {
                int depth = 1;
                while (depth > 0 && Current().Type != TokenType.EOF)
                {
                    if (Current().Type == TokenType.LBRACE) depth++;
                    else if (Current().Type == TokenType.RBRACE) { depth--; if (depth == 0) break; }
                    Advance();
                }
                Expect(TokenType.RBRACE); // }
            }

            if (typeName != null ||
                (Current().Type == TokenType.IDENTIFIER && (Peek(1).Type != TokenType.LPAREN && Peek(1).Type != TokenType.INCREMENT && Peek(1).Type != TokenType.DECREMENT && Peek(1).Type != TokenType.COLON))) // 支持自定义类型
            {
                // 如果ParseTypeSpecifiers返回null，说明是自定义类型
                if (typeName == null && Current().Type == TokenType.IDENTIFIER)
                {
                    typeName = Advance().Value.ToString();
                }
                // 处理类型限定符后的自定义类型名: const wchar_t -> "const wchar_t" (含内置类型)
                else if (typeName != null && Current().Type == TokenType.IDENTIFIER &&
                    (program.TypeDefs.ContainsKey(Current().Value.ToString()) ||
                     Current().Value.ToString() == "wchar_t" || Current().Value.ToString() == "char32_t"))
                {
                    typeName += " " + Advance().Value.ToString();
                }

                // 检查是否为指针类型 (保存基底类型, 逗号声明时每变量独立)
                string baseType = typeName;
                // 处理指针前的 const/volatile: sqlite3_mutex_methods const *
                while (Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE)
                    baseType += " " + Advance().Value.ToString();
                int ptrStars = 0;
                while (Match(TokenType.STAR))
                {
                    ptrStars++;
                    while (Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE)
                        baseType += " " + Advance().Value.ToString();
                }
                typeName = baseType + new string('*', ptrStars);

                // 处理函数指针/数组指针: int (*name)(params) 或 int (*name)[N]
                if (Current().Type == TokenType.LPAREN && Peek(1).Type == TokenType.STAR)
                {
                    Advance(); // 跳过 (
                    Match(TokenType.STAR); // 跳过 *
                    typeName += "*";
                    if (Current().Type == TokenType.IDENTIFIER)
                    {
                        string name = Advance().Value.ToString();
                        // 函数指针数组: int(*fa[2])(...)
                        bool isFpArray = false;
                        int? fpArraySize = null;
                        while (Match(TokenType.LBRACKET)) {
                            isFpArray = true;
                            if (Current().Type == TokenType.NUMBER && Peek(1).Type == TokenType.RBRACKET)
                            {
                                fpArraySize = Convert.ToInt32(Current().Value);
                                Advance(); // consume number
                            }
                            else
                            {
                                while (Current().Type != TokenType.RBRACKET && Current().Type != TokenType.EOF) Advance();
                            }
                            Expect(TokenType.RBRACKET);
                            typeName += "[]";
                        }
                        Expect(TokenType.RPAREN); // 结束 (*name)
                        // 指针到数组: int (*name)[N]
                        if (Current().Type == TokenType.LBRACKET)
                        {
                            List<int?> dimensions = new List<int?>();
                            while (Match(TokenType.LBRACKET))
                            {
                                int? size = null;
                                if (Current().Type == TokenType.NUMBER) { size = Convert.ToInt32(Current().Value); Advance(); }
                                Expect(TokenType.RBRACKET);
                                dimensions.Add(size);
                            }
                            // Parse comma-separated declarators: int (*a)[N], (*b)[M]
                            var ptrVars = new List<VariableDecl>();
                            ptrVars.Add(new VariableDecl(name, typeName));
                            while (Match(TokenType.COMMA))
                            {
                                // Parse (*name)[N] or name for subsequent declarators
                                if (Current().Type == TokenType.LPAREN && Peek(1).Type == TokenType.STAR)
                                {
                                    Advance(); Match(TokenType.STAR);
                                    string n = Expect(TokenType.IDENTIFIER).Value.ToString();
                                    Expect(TokenType.RPAREN);
                                    // Skip dimensions for this declarator
                                    while (Match(TokenType.LBRACKET))
                                    {
                                        if (Current().Type == TokenType.NUMBER) Advance();
                                        Expect(TokenType.RBRACKET);
                                    }
                                    ptrVars.Add(new VariableDecl(n, typeName));
                                }
                                else if (Current().Type == TokenType.IDENTIFIER)
                                {
                                    string n = Advance().Value.ToString();
                                    ptrVars.Add(new VariableDecl(n, typeName));
                                }
                                else break;
                            }
                            if (Match(TokenType.ASSIGN))
                                ParseAssignment();
                            Expect(TokenType.SEMICOLON);
                            if (ptrVars.Count == 1) return ptrVars[0];
                            var ptrBlock = new Block();
                            foreach (var v in ptrVars) ptrBlock.Statements.Add(v);
                            return ptrBlock;
                        }
                        // 函数指针: int (*name)(params)
                        if (Match(TokenType.LPAREN))
                        {
                            int depth = 1;
                            while (depth > 0 && Current().Type != TokenType.EOF)
                            {
                                if (Current().Type == TokenType.LPAREN) depth++;
                                else if (Current().Type == TokenType.RPAREN) { depth--; if (depth > 0) Advance(); }
                                else Advance();
                            }
                            if (Current().Type == TokenType.RPAREN) Advance(); // 跳过匹配的 )
                        }
                        // 处理多个声明
                        var fpVars = new List<VariableDecl>();
                        var firstVar = new VariableDecl(name, typeName);
                        if (isFpArray)
                        {
                            firstVar.IsArray = true;
                            firstVar.ArraySize = fpArraySize;
                        }
                        fpVars.Add(firstVar);
                        while (Match(TokenType.COMMA))
                        {
                            // Parse next declarator: could be (*name)(params) or just name
                            if (Current().Type == TokenType.LPAREN && Peek(1).Type == TokenType.STAR)
                            {
                                Advance(); Match(TokenType.STAR);
                                string n = Expect(TokenType.IDENTIFIER).Value.ToString();
                                Expect(TokenType.RPAREN);
                                // Skip function params
                                if (Match(TokenType.LPAREN))
                                {
                                    int d2 = 1;
                                    while (d2 > 0 && Current().Type != TokenType.EOF)
                                    {
                                        if (Current().Type == TokenType.LPAREN) d2++;
                                        else if (Current().Type == TokenType.RPAREN) { d2--; if (d2 > 0) Advance(); }
                                        else Advance();
                                    }
                                    if (Current().Type == TokenType.RPAREN) Advance();
                                }
                                fpVars.Add(new VariableDecl(n, typeName));
                            }
                            else if (Current().Type == TokenType.IDENTIFIER)
                            {
                                string n = Advance().Value.ToString();
                                fpVars.Add(new VariableDecl(n, typeName));
                            }
                            else break;
                        }
                        if (Match(TokenType.ASSIGN))
                        {
                            if (Current().Type == TokenType.LBRACE)
                                firstVar.Initializer = ParseInitializerList();
                            else
                                firstVar.Initializer = ParseAssignment();
                        }
                        Expect(TokenType.SEMICOLON);
                        if (fpVars.Count == 1) return fpVars[0];
                        var fpBlock = new Block();
                        foreach (var v in fpVars) fpBlock.Statements.Add(v);
                        return fpBlock;
                    }
                }

                // typedef struct/union { ... } Alias; (匿名struct/union typedef)
                if (storageClassToken?.Type == TokenType.TYPEDEF &&
                    (typeName == "struct" || typeName == "union") &&
                    Current().Type == TokenType.LBRACE)
                {
                    bool isUnion = typeName == "union";
                    Expect(TokenType.LBRACE);
                    int offset = 0;
                    var members = new List<StructMember>();
                    while (Current().Type != TokenType.RBRACE)
                    {
                        string memberType = ParseTypeSpecifiers();
                        if (string.IsNullOrEmpty(memberType) && Current().Type == TokenType.IDENTIFIER)
                            memberType = Advance().Value.ToString();
                        if (memberType == "struct" || memberType == "union")
                        {
                            if (Current().Type == TokenType.IDENTIFIER)
                                memberType += " " + Advance().Value.ToString();
                        }
                        while (Current().Type == TokenType.LONG || Current().Type == TokenType.SIGNED || Current().Type == TokenType.UNSIGNED || Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE || Current().Type == TokenType.CHAR || Current().Type == TokenType.INT || Current().Type == TokenType.FLOAT || Current().Type == TokenType.DOUBLE || Current().Type == TokenType.SHORT || Current().Type == TokenType.STRUCT || Current().Type == TokenType.UNION || Current().Type == TokenType.INT8 || Current().Type == TokenType.INT16 || Current().Type == TokenType.INT32 || Current().Type == TokenType.INT64 || Current().Type == TokenType.UINT8 || Current().Type == TokenType.UINT16 || Current().Type == TokenType.UINT32 || Current().Type == TokenType.UINT64 || Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.SIZE_T || Current().Type == TokenType.SSIZE_T || Current().Type == TokenType.PTRDIFF_T || Current().Type == TokenType.BOOL)
                            memberType += " " + Advance().Value.ToString();
                        int ptrLevel = 0;
                        while (Match(TokenType.STAR)) ptrLevel++;
                        string memberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                        bool isArr = false; int? arrSize = null;
                        while (Match(TokenType.LBRACKET))
                        {
                            isArr = true;
                            if (Current().Type == TokenType.NUMBER && Peek(1).Type == TokenType.RBRACKET)
                            {
                                arrSize = Convert.ToInt32(Current().Value);
                                Advance();
                            }
                            else
                            {
                                // 跳过复杂数组大小表达式: [20-1], [(11+1)]
                                int depth = 1;
                                while (depth > 0 && Current().Type != TokenType.EOF)
                                {
                                    if (Current().Type == TokenType.LBRACKET) depth++;
                                    else if (Current().Type == TokenType.RBRACKET) { depth--; if (depth == 0) break; }
                                    Advance();
                                }
                            }
                            Expect(TokenType.RBRACKET);
                        }
                        Expect(TokenType.SEMICOLON);
                        var m = new StructMember(memberName, memberType, isArr, arrSize);
                        m.Offset = offset; m.PointerLevel = ptrLevel;
                        members.Add(m);
                        offset += ptrLevel > 0 ? 4 : GetMemberSize(memberType, isArr, arrSize);
                    }
                    Expect(TokenType.RBRACE);
                    string aliasName = Expect(TokenType.IDENTIFIER).Value.ToString();
                    string fullType = isUnion ? "union" : "struct";
                    if (isUnion) { var ud = new UnionDecl(""); ud.Members.AddRange(members); ud.Size = offset; program.Unions[aliasName] = ud; }
                    else { var sd = new StructDecl(""); sd.Members.AddRange(members); sd.Size = offset; program.Structs[aliasName] = sd; }
                    program.TypeDefs[aliasName] = fullType;
                    Expect(TokenType.SEMICOLON);
                    return new ExpressionStatement(null);
                }

                List<VariableDecl> variables = new List<VariableDecl>();

                while (true)
                {
                    string name = Expect(TokenType.IDENTIFIER).Value.ToString();

                    // 检查是否为数组（支持多维数组 + VLA + 常量表达式）
                    List<int?> dimensions = new List<int?>();
                    List<ASTNode>? vlaDims = null;
                    while (Match(TokenType.LBRACKET))
                    {
                        int? size = null;
                        // 空括号直接跳过
                        if (Current().Type == TokenType.RBRACKET)
                        {
                            // 空维度: type name[] = {...} 或函数参数 type name[]
                        }
                        else
                        {
                            // 检查下下个 token 是否为 ] — 如果是，说明是简单数字
                            // 否则是常量表达式（如 MAX+8）或 VLA（如 arr[n]）
                            if (Current().Type == TokenType.NUMBER && Peek(1).Type == TokenType.RBRACKET)
                            {
                                // 字面量超出 int 范围要报错：此前 Convert.ToInt32 抛的是裸 .NET
                                // OverflowException，被顶层容错恢复吞成一行 stderr 日志后**继续编**
                                // （实测 `int a[4294967295]` → 「编译完成: 0 条指令」，退出码 0）。
                                try { size = Convert.ToInt32(Current().Value); }
                                catch (System.OverflowException)
                                {
                                    throw Error(ErrorCode.Parser_UnexpectedToken,
                                        $"数组维度字面量超出 int 范围：{Current().Value}");
                                }
                                Advance();
                            }
                            else
                            {
                                // 常量表达式或 VLA: 运行时维度 (如 int arr[n] 或 int arr[MAX+8])
                                var dimExpr = ParseExpression();
                                // 能折成常量的**负数维度**要在这里就报错：`int a[-1]` 此前落进
                                // VLA 分支，运行时算出负的分配量 ⇒ 生成 `sub R13, R1` 而 R1 为负，
                                // 栈指针**反向移动**，编译不报错（v0.96.187 / patches/0018）。
                                if (TryConstInt(dimExpr, out var constDim) && constDim < 0)
                                    throw Error(ErrorCode.Parser_UnexpectedToken,
                                        $"数组维度不能为负数：{DescribeConstExpr(dimExpr)} = {constDim}");
                                vlaDims ??= new List<ASTNode>();
                                vlaDims.Add(dimExpr);
                            }
                        }
                        Expect(TokenType.RBRACKET);
                        dimensions.Add(size);
                    }

                    if (dimensions.Count > 0)
                    {
                        // 计算总大小
                        int? totalSize = 1;
                        bool isVLA = vlaDims != null && vlaDims.Count > 0;
                        if (!isVLA)
                        {
                            foreach (var dim in dimensions)
                            {
                                // MulArraySize（checked）：多维总元素数回绕就是静默拿到错的尺寸
                                if (dim.HasValue) totalSize = MulArraySize(totalSize.Value, dim.Value);
                                else { totalSize = null; break; }
                            }
                        }
                        else totalSize = null;

                        VariableDecl var = new VariableDecl(name, typeName, null, true, totalSize);
                        var.Dimensions = dimensions;
                        if (isVLA) var.VlaDimensions = vlaDims;
                        if (storageClassToken?.Type == TokenType.STATIC) var.IsStatic = true;

                        if (Match(TokenType.ASSIGN))
                        {
                            // 数组初始化
                            if (Match(TokenType.LBRACE))
                            {
                                List<ASTNode> elements = new List<ASTNode>();
                                if (!Match(TokenType.RBRACE))
                                {
                                    while (true)
                                    {
                                        elements.Add(ParseAssignment());
                                        if (!Match(TokenType.COMMA))
                                        {
                                            break;
                                        }
                                        // C99: 容忍尾部逗号 (如 {1,2,3,})
                                        if (Current().Type == TokenType.RBRACE)
                                            break;
                                    }
                                    Expect(TokenType.RBRACE);
                                }
                                var.Initializer = new ArrayInitializer(elements);
                            }
                            else
                            {
                                var.Initializer = ParseAssignment();
                            }
                        }
                        variables.Add(var);
                    }
                    else
                    {
                        VariableDecl var = new VariableDecl(name, typeName);
                        if (Match(TokenType.ASSIGN))
                        {
                            var.Initializer = ParseAssignment();
                        }
                        if (storageClassToken?.Type == TokenType.STATIC) var.IsStatic = true;
                        variables.Add(var);
                    }

                    // 检查是否有更多变量 (逗号声明: 每变量重新解析 * 指针层)
                    if (Match(TokenType.COMMA))
                    {
                        // 重建当前变量的指针类型 (解析每个变量自己的 *)
                        int myStars = 0;
                        while (Match(TokenType.STAR))
                            myStars++;
                        typeName = baseType + new string('*', myStars);
                    }
                    else
                    {
                        break;
                    }
                }

                Expect(TokenType.SEMICOLON);

                // 如果只有一个变量，直接返回
                if (variables.Count == 1)
                {
                    return variables[0];
                }

                // 否则，返回一个包含所有变量声明的 Block 节点
                Block block = new Block();
                foreach (var var in variables)
                {
                    block.Statements.Add(var);
                }
                return block;
            }

            // if 语句
            if (Match(TokenType.IF))
            {
                return ParseIf();
            }

            // while 语句
            if (Match(TokenType.WHILE))
            {
                return ParseWhile();
            }

            // for 语句
            if (Match(TokenType.FOR))
            {
                return ParseFor();
            }

            // do-while 语句
            if (Match(TokenType.DO))
            {
                return ParseDoWhile();
            }

            // switch 语句
            if (Match(TokenType.SWITCH))
            {
                return ParseSwitch();
            }

            // return 语句
            if (Match(TokenType.RETURN))
            {
                ASTNode value = null;
                if (Current().Type != TokenType.SEMICOLON)
                {
                    value = ParseExpression();
                }
                Expect(TokenType.SEMICOLON);
                return new ReturnStatement(value);
            }

            // break 语句
            if (Match(TokenType.BREAK))
            {
                Expect(TokenType.SEMICOLON);
                return new BreakStatement();
            }

            // continue 语句
            if (Match(TokenType.CONTINUE))
            {
                Expect(TokenType.SEMICOLON);
                return new ContinueStatement();
            }

            // goto 语句
            if (Match(TokenType.GOTO))
            {
                string label = Expect(TokenType.IDENTIFIER).Value.ToString();
                Expect(TokenType.SEMICOLON);
                return new GotoStatement(label);
            }

            // try 语句 (OS 模式)
            if (Match(TokenType.TRY))
            {
                return ParseTryStatement();
            }

            // throw 语句 (OS 模式)
            if (Match(TokenType.THROW))
            {
                return ParseThrowStatement();
            }

            // 标签语句
            if (Current().Type == TokenType.IDENTIFIER && Peek(1).Type == TokenType.COLON)
            {
                string label = Advance().Value.ToString();
                Expect(TokenType.COLON);
                ASTNode statement = ParseStatement();
                return new LabeledStatement(label, statement);
            }

            // 代码块
            if (Current().Type == TokenType.LBRACE)
            {
                return ParseBlock();
            }

            // 汇编指令（关键字形式：asm(...)）
            if (Current().Type == TokenType.ASM)
            {
                Advance();
                Expect(TokenType.LPAREN);
                Token stringToken = Current();
                Expect(TokenType.STRING);
                string asmCode = stringToken.Value.ToString();
                Expect(TokenType.RPAREN);
                Expect(TokenType.SEMICOLON);
                return new AsmStatement(asmCode);
            }

            // 汇编指令（标识符形式：asm 是保留字但可能被识别为 IDENTIFIER）
            if (Current().Type == TokenType.IDENTIFIER && 
                Current().Value?.ToString() == "asm" && 
                Peek(1).Type == TokenType.LPAREN)
            {
                Advance(); // 跳过 asm
                Expect(TokenType.LPAREN);
                Token stringToken = Current();
                Expect(TokenType.STRING);
                string asmCode = stringToken.Value.ToString();
                Expect(TokenType.RPAREN);
                Expect(TokenType.SEMICOLON);
                return new AsmStatement(asmCode);
            }

            // 空语句
            if (Current().Type == TokenType.SEMICOLON)
            {
                Advance();
                return new ExpressionStatement(null);
            }

            // 块结束符 (用于容错)
            if (Current().Type == TokenType.RBRACE)
            {
                return new ExpressionStatement(null);
            }

            // 表达式语句 (容错模式)
            try {
                ASTNode expr = ParseExpression();
                Expect(TokenType.SEMICOLON);
                return new ExpressionStatement(expr);
            } catch (System.Exception) {
                // 跳过无法解析的语句直到遇到 ; (不跳过 }, 让外层处理结构性错误)
                while (Current().Type != TokenType.SEMICOLON && Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                    Advance();
                if (Current().Type == TokenType.SEMICOLON) Advance();
                else throw; // 遇到}或其他结构性token→向上传播
                return new ExpressionStatement(null);
            }
        }

        private SwitchStatement ParseSwitch()
        {
            Expect(TokenType.LPAREN);
            ASTNode expression = ParseExpression();
            Expect(TokenType.RPAREN);
            Expect(TokenType.LBRACE);

            List<CaseStatement> cases       = new List<CaseStatement>();
            DefaultStatement    defaultStmt = null;

            while (Current().Type != TokenType.RBRACE)
            {
                if (Match(TokenType.CASE))
                {
                    ASTNode caseValue = ParseExpression();
                    Expect(TokenType.COLON);
                    Block caseBody = new Block();
                    while (Current().Type != TokenType.CASE && Current().Type != TokenType.DEFAULT && Current().Type != TokenType.RBRACE)
                    {
                        try { caseBody.Statements.Add(ParseStatement()); }
                        catch (System.Exception) {
                            while (Current().Type != TokenType.SEMICOLON && Current().Type != TokenType.RBRACE && Current().Type != TokenType.CASE && Current().Type != TokenType.DEFAULT && Current().Type != TokenType.EOF)
                                Advance();
                            if (Current().Type == TokenType.SEMICOLON) Advance();
                            else throw; // propagate structural errors
                        }
                    }
                    cases.Add(new CaseStatement(caseValue, caseBody));
                }
                else if (Match(TokenType.DEFAULT))
                {
                    Expect(TokenType.COLON);
                    Block defaultBody = new Block();
                    while (Current().Type != TokenType.CASE && Current().Type != TokenType.RBRACE)
                    {
                        try { defaultBody.Statements.Add(ParseStatement()); }
                        catch (System.Exception) {
                            while (Current().Type != TokenType.SEMICOLON && Current().Type != TokenType.RBRACE && Current().Type != TokenType.CASE && Current().Type != TokenType.DEFAULT && Current().Type != TokenType.EOF)
                                Advance();
                            if (Current().Type == TokenType.SEMICOLON) Advance();
                        }
                    }
                    defaultStmt = new DefaultStatement(defaultBody);
                }
                else
                {
                    Console.Error.WriteLine($"[SKIP] switch内跳过无法识别的token: {Current().Type} at line {Current().OriginalLine}");
                    // 容错: 跳过直到同级 CASE/DEFAULT/RBRACE (跟踪大括号深度)
                    int switchDepth = 0;
                    while (Current().Type != TokenType.EOF)
                    {
                        if (Current().Type == TokenType.LBRACE) switchDepth++;
                        else if (Current().Type == TokenType.RBRACE)
                        {
                            if (switchDepth == 0) break; // 同级 } → 可能是switch结束
                            switchDepth--;
                        }
                        else if (switchDepth == 0 && (Current().Type == TokenType.CASE || Current().Type == TokenType.DEFAULT))
                            break; // 同级 case/default
                        Advance();
                    }
                }
            }

            Expect(TokenType.RBRACE, $"switch体未关闭, 行{Current().OriginalLine}");
            return new SwitchStatement(expression, cases, defaultStmt);
        }

        private Block ParseStatementOrBlock()
        {
            // 检查是否为块
            if (Current().Type == TokenType.LBRACE)
            {
                return ParseBlock();
            }
            else
            {
                // 单个语句，包装成块
                Block block = new Block();
                block.Statements.Add(ParseStatement());
                return block;
            }
        }

        private IfStatement ParseIf()
        {
            Expect(TokenType.LPAREN);
            ASTNode condition = ParseExpression();
            Expect(TokenType.RPAREN);

            Block thenBranch = ParseStatementOrBlock();
            Block elseBranch = null;

            if (Match(TokenType.ELSE))
            {
                elseBranch = ParseStatementOrBlock();
            }

            return new IfStatement(condition, thenBranch, elseBranch);
        }

        private WhileStatement ParseWhile()
        {
            Expect(TokenType.LPAREN);
            ASTNode condition = ParseExpression();
            Expect(TokenType.RPAREN);

            Block body = ParseStatementOrBlock();
            return new WhileStatement(condition, body);
        }

        private DoWhileStatement ParseDoWhile()
        {
            Block body = ParseStatementOrBlock();
            Expect(TokenType.WHILE);
            Expect(TokenType.LPAREN);
            ASTNode condition = ParseExpression();
            Expect(TokenType.RPAREN);
            Expect(TokenType.SEMICOLON);
            return new DoWhileStatement(condition, body);
        }

        private ForStatement ParseFor()
        {
            Expect(TokenType.LPAREN);

            ASTNode init = null;
            if (Current().Type != TokenType.SEMICOLON)
            {
                // Check for C99 for-loop declaration: for(int i=0; ...; ...)
                string declType = ParseTypeSpecifiers();
                if (declType != null)
                {
                    while (Match(TokenType.STAR)) declType += "*";

                    string varName = Expect(TokenType.IDENTIFIER).Value.ToString();
                    ASTNode initializer = null;
                    List<int?> dims = new List<int?>();
                    while (Match(TokenType.LBRACKET))
                    {
                        if (Current().Type == TokenType.NUMBER && Peek(1).Type == TokenType.RBRACKET)
                        { dims.Add(Convert.ToInt32(Current().Value)); Advance(); }
                        else { dims.Add(null); int d2=1; while(d2>0&&Current().Type!=TokenType.EOF){if(Current().Type==TokenType.LBRACKET)d2++;else if(Current().Type==TokenType.RBRACKET){d2--;if(d2==0)break;}Advance();} }
                        Expect(TokenType.RBRACKET);
                    }
                    if (Match(TokenType.ASSIGN))
                        initializer = ParseAssignment();
                    VariableDecl varDecl = dims.Count > 0
                        ? new VariableDecl(varName, declType, null, true, dims[0])
                        : new VariableDecl(varName, declType);
                    if (initializer != null) varDecl.Initializer = initializer;
                    init = varDecl;
                }
                else
                {
                    init = ParseExpression();
                }
            }
            Expect(TokenType.SEMICOLON);

            ASTNode condition = null;
            if (Current().Type != TokenType.SEMICOLON)
            {
                condition = ParseExpression();
            }
            Expect(TokenType.SEMICOLON);

            ASTNode update = null;
            if (Current().Type != TokenType.RPAREN)
            {
                update = ParseExpression();
            }

            Expect(TokenType.RPAREN);
            Block body = ParseStatementOrBlock();

            return new ForStatement(init, condition, update, body);
        }

        private ASTNode ParseTryStatement()
        {
            Block body = ParseBlock();

            var catches = new List<CatchClause>();
            while (Match(TokenType.CATCH))
            {
                string exceptionType = null;
                string variableName = null;
                if (Match(TokenType.LPAREN))
                {
                    // Parse optional type and variable: catch (int err) or catch (...)
                    if (Current().Type == TokenType.ELLIPSIS)
                    {
                        Advance(); // ...
                    }
                    else if (IsTypeKeyword(Current().Type) || Current().Type == TokenType.IDENTIFIER)
                    {
                        string typeOrVar = Advance().Value.ToString();
                        // Combine unsigned/signed with following type
                        if ((typeOrVar == "unsigned" || typeOrVar == "signed") &&
                            IsTypeKeyword(Current().Type))
                        {
                            typeOrVar = typeOrVar + " " + Advance().Value.ToString();
                        }
                        if (IsTypeName(typeOrVar))
                        {
                            exceptionType = typeOrVar;
                            if (Current().Type == TokenType.IDENTIFIER)
                            {
                                variableName = Advance().Value.ToString();
                            }
                        }
                        else
                        {
                            variableName = typeOrVar;
                        }
                    }
                    Expect(TokenType.RPAREN);
                }
                Block catchBody = ParseBlock();
                catches.Add(new CatchClause(exceptionType, variableName, catchBody));
            }

            return new TryStatement(body, catches);
        }

        private static bool IsTypeKeyword(TokenType type)
        {
            return type is TokenType.INT or TokenType.CHAR or TokenType.SHORT or TokenType.LONG
                or TokenType.FLOAT or TokenType.DOUBLE or TokenType.VOID
                or TokenType.UNSIGNED or TokenType.SIGNED
                or TokenType.INT8 or TokenType.INT16 or TokenType.INT32 or TokenType.INT64
                or TokenType.UINT8 or TokenType.UINT16 or TokenType.UINT32 or TokenType.UINT64
                or TokenType.INTPTR_T or TokenType.UINTPTR_T;
        }

        private bool IsTypeName(string name)
        {
            return name is "int" or "char" or "short" or "long" or "float" or "double"
                or "void" or "unsigned" or "signed" or "int8_t" or "int16_t" or "int32_t"
                or "int64_t" or "uint8_t" or "uint16_t" or "uint32_t" or "uint64_t"
                or "size_t" or "ssize_t" or "intptr_t" or "uintptr_t" or "ptrdiff_t"
                or "_Bool" or "bool";
        }

        private ASTNode ParseThrowStatement()
        {
            ASTNode value = null;
            if (Current().Type != TokenType.SEMICOLON)
            {
                value = ParseExpression();
            }
            Expect(TokenType.SEMICOLON);
            return new ThrowStatement(value);
        }

    }
}  // namespace
