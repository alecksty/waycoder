using CompilerBase;
using System;
using System.Collections.Generic;

namespace PascalCompiler
{
    /// <summary>
    /// Pascal 语言语法分析器
    /// </summary>
    public partial class Parser : ParserBase<Token, TokenType>
    {
        private Stack<string> withContext = new Stack<string>();
        public List<string> UsesNames { get; private set; } = new();

        protected override TokenType GetTokenType(Token token) => token.Type;

        /// <summary>当前 Pascal 方言 (v1.66.32+)</summary>
        private VMLPlugins.PascalDialect CurrentDialect =>
            VMLPlugins.CompilerOptionsContext.Current.PascalDialect;
        private bool IsDialect(VMLPlugins.PascalDialect d) => CurrentDialect == d;
        private bool IsOopDialect => IsDialect(VMLPlugins.PascalDialect.Delphi)
                                   || IsDialect(VMLPlugins.PascalDialect.FreePascal);

        public Parser(List<Token> tokens) : base(tokens) { }

        /// <summary>
        /// 当前 token 是不是一个**叫这个名字的普通标识符**。
        ///
        /// <para>
        /// 给"预定义类型标识符"（`File`/`Text`/`TextFile`）用：它们在 Turbo Pascal 里
        /// **不是保留字**、可以被用户重新定义（`var Text: string;`），所以 Lexer 索性
        /// 不把它们收进关键字表、一律发 `IDENTIFIER`，只在**类型位置**按名字认
        /// （见 `ParseType` 那两支）。判据不区分大小写 —— Pascal 的标识符本来就不区分。
        /// </para>
        /// </summary>
        private static bool IsIdentifierNamed(Token t, string name)
            => t.Type == TokenType.IDENTIFIER
               && string.Equals(t.Value?.ToString(), name, StringComparison.OrdinalIgnoreCase);

        protected override Token Expect(TokenType tokenType, string message)
        {
            if (Check(tokenType)) return Advance();
            // 位置交给**统一出口**（`Error` 拼 `文件:行:列: error:` 并查 `#include` 行号映射）；
            // 原来那层 `Strings.SyntaxErrorAt(Cur.Line, Cur.Column, msg)` 自己又拼了一遍位置，
            // 与统一前缀**重复**，且取的是预处理后的行号。
            throw Error(message ?? VMLPlugins.Strings.ExpectedToken(tokenType.ToString(), GetTokenType(Cur).ToString()));
        }

        // ⚠ 这里原有 `protected override ParseException Error(string message)
        //   => new ParseException(Strings.SyntaxErrorAt(Cur.Line, Cur.Column, message), Cur!)`
        //   —— **已删除、改用基类实现**。它比基类少两件事：
        //     ① 位置不是宿主认的 `文件:行:列: error:` 形状（`Strings.SyntaxErrorAt` 拼的是
        //        `… at line N … (col C)`），编辑器按那个形状锚行，锚不到；
        //     ② 取的是 `Cur.Line` = **预处理后**的行号 ⇒ **不查 `#include` 行号映射**，
        //        错在头文件里时会报成用户文件的行。
        //   本门的 `Token` 实现了 `ITokenPosition`，基类那条通用实现取到的行列**与这里手写的同源**
        //   （同一只 `Cur`），所以删掉只是补上前缀与映射，位置一个字不变。
        //   （Go 前端那条同类覆写在同一轮里同样删掉了。）


        public ASTNode Parse()
        {
            if (GetTokenType(Cur) == TokenType.UNIT)
                return ParseUnit();
            return ParseProgram();
        }

        private ProgramNode ParseProgram()
        {
            ProgramNode program = new ProgramNode
            {
                Line = Cur.Line,
                Column = Cur.Column
            };

            // program 标识符 [(参数列表)] ;
            //
            // ⚠ **`program` 头是可选的** —— Turbo Pascal 起就允许源码直接从
            //   `uses` / `const` / `type` / `var` / `begin` 开始（写库函数、贴代码片段的
            //   老程序几乎都不写这一行）。此前无条件 `Expect(PROGRAM)`，于是那批文件
            //   在**第一个词**上就报 `期望 'program'` —— 语料 112 份里 19 份卡在这一条，
            //   是当前最大的一类。
            if (GetTokenType(Cur) == TokenType.PROGRAM)
            {
                Advance();
                program.Name = Expect(TokenType.IDENTIFIER, "期望程序名").Value.ToString();
                // 跳过可选的程序参数 (input, output) — ISO Pascal 标准语法
                if (GetTokenType(Cur) == TokenType.LPAREN)
                {
                    while (GetTokenType(Cur) != TokenType.RPAREN && GetTokenType(Cur) != TokenType.EOF)
                        Advance();
                    Expect(TokenType.RPAREN, "期望 ')'");
                }
                Expect(TokenType.SEMICOLON, "期望 ';'");
            }
            else
            {
                // 没有头：名字用占位（入口标签恒为 `main`，与程序名无关）
                program.Name = "main";
            }

            // 解析 uses 子句（可选）
            ParseUsesClause();

            // 解析声明部分
            ParseDeclarations(program.Declarations, program.Subprograms);

            // begin ... end .
            Expect(TokenType.BEGIN, "期望 'begin'");
            program.Block = ParseBlock();
            Expect(TokenType.END, "期望 'end'");
            Expect(TokenType.DOT, "期望 '.'");

            return program;
        }

        private UnitNode ParseUnit()
        {
            var unit = new UnitNode
            {
                Line = Cur.Line,
                Column = Cur.Column
            };

            Expect(TokenType.UNIT, "期望 'unit'");
            unit.Name = Expect(TokenType.IDENTIFIER, "期望单元名").Value.ToString();
            Expect(TokenType.SEMICOLON, "期望 ';'");

            // interface 部分
            Expect(TokenType.INTERFACE, "期望 'interface'");

            // uses 子句
            ParseUsesClause();

            // interface 声明 (常量/类型/变量/子程序头)
            ParseUnitInterfaceDeclarations(unit);

            // implementation 部分
            Expect(TokenType.IMPLEMENTATION, "期望 'implementation'");

            // uses 子句 (可选)
            ParseUsesClause();

            // implementation 声明 (子程序完整实现)
            ParseDeclarations(unit.ImplementationDeclarations, unit.ImplementationSubprograms);

            // 可选的初始化块
            if (GetTokenType(Cur) == TokenType.BEGIN)
            {
                Advance();
                unit.InitializationBlock = ParseBlock();
            }

            Expect(TokenType.END, "期望 'end'");
            Expect(TokenType.DOT, "期望 '.'");

            return unit;
        }

        private void ParseUsesClause()
        {
            if (GetTokenType(Cur) == TokenType.USES)
            {
                Advance(); // 跳过 uses
                while (true)
                {
                    string unitName = Expect(TokenType.IDENTIFIER, "期望单元名").Value.ToString();
                    UsesNames.Add(unitName);
                    if (GetTokenType(Cur) == TokenType.COMMA)
                        Advance();
                    else
                        break;
                }
                Expect(TokenType.SEMICOLON, "期望 ';'");
            }
        }

        private void ParseUnitInterfaceDeclarations(UnitNode unit)
        {
            while (true)
            {
                if (GetTokenType(Cur) == TokenType.VAR)
                {
                    ParseVarDeclarations(unit.InterfaceDeclarations);
                }
                else if (GetTokenType(Cur) == TokenType.CONST)
                {
                    ParseConstDeclarations(unit.InterfaceDeclarations);
                }
                else if (GetTokenType(Cur) == TokenType.TYPE)
                {
                    ParseTypeDeclarations(unit.InterfaceDeclarations);
                }
                else if (GetTokenType(Cur) == TokenType.FUNCTION || GetTokenType(Cur) == TokenType.PROCEDURE
                      || GetTokenType(Cur) == TokenType.CONSTRUCTOR || GetTokenType(Cur) == TokenType.DESTRUCTOR)
                {
                    var subprogram = ParseSubprogramHeader();
                    if (subprogram != null)
                        unit.InterfaceSubprograms.Add(subprogram);
                }
                else
                {
                    break;
                }
            }
        }

        private void ParseDeclarations(List<DeclarationNode> declarations, List<SubprogramDeclarationNode> subprograms)
        {
            while (true)
            {
                if (GetTokenType(Cur) == TokenType.VAR)
                {
                    ParseVarDeclarations(declarations);
                }
                else if (GetTokenType(Cur) == TokenType.CONST)
                {
                    ParseConstDeclarations(declarations);
                }
                else if (GetTokenType(Cur) == TokenType.TYPE)
                {
                    ParseTypeDeclarations(declarations);
                }
                else if (GetTokenType(Cur) == TokenType.LABEL)
                {
                    ParseLabelDeclarations();
                }
                else if (GetTokenType(Cur) == TokenType.FUNCTION || GetTokenType(Cur) == TokenType.PROCEDURE)
                {
                    var subprogram = ParseSubprogramDeclaration();
                    if (subprogram != null)
                        subprograms.Add(subprogram);
                }
                else
                {
                    break;
                }
            }
        }

        private void ParseVarDeclarations(List<DeclarationNode> declarations)
        {
            Expect(TokenType.VAR, "期望 'var'");
            
            while (GetTokenType(Cur) == TokenType.IDENTIFIER)
            {
                var varDecls = ParseMultiVarDeclaration();
                foreach (var decl in varDecls)
                    declarations.Add(decl);
                Expect(TokenType.SEMICOLON, "期望 ';'");
            }
        }

        private List<VarDeclarationNode> ParseMultiVarDeclaration()
        {
            List<VarDeclarationNode> result = new List<VarDeclarationNode>();
            
            // 变量名列表
            List<string> varNames = new List<string>();
            do
            {
                varNames.Add(Expect(TokenType.IDENTIFIER, "期望变量名").Value.ToString());
            } while (Match(TokenType.COMMA));

            Expect(TokenType.COLON, "期望 ':'");

            // 类型
            TypeNode type = ParseType();

            // 为每个变量创建声明节点
            foreach (var name in varNames)
            {
                result.Add(new VarDeclarationNode
                {
                    Name = name,
                    Type = type,
                    Line = Cur.Line,
                    Column = Cur.Column
                });
            }
            
            return result;
        }

        private void ParseConstDeclarations(List<DeclarationNode> declarations)
        {
            Expect(TokenType.CONST, "期望 'const'");

            while (GetTokenType(Cur) == TokenType.IDENTIFIER)
            {
                string constName = Expect(TokenType.IDENTIFIER, "期望常量名").Value.ToString();

                // 可选类型标注: const name : type = value
                TypeNode? constType = null;
                List<ExpressionNode>? arrayValues = null;
                if (GetTokenType(Cur) == TokenType.COLON)
                {
                    Advance(); // skip ':'
                    constType = ParseType();
                }

                Expect(TokenType.EQUALS, "期望 '='");

                ExpressionNode value;
                // 数组初始化: const arr: array[1..5] of integer = (1, 2, 3, 4, 5);
                if (GetTokenType(Cur) == TokenType.LPAREN && constType is ArrayTypeNode)
                {
                    Advance(); // skip '('
                    arrayValues = new List<ExpressionNode>();
                    while (GetTokenType(Cur) != TokenType.RPAREN && GetTokenType(Cur) != TokenType.EOF)
                    {
                        arrayValues.Add(ParseExpression());
                        if (GetTokenType(Cur) != TokenType.RPAREN)
                            Expect(TokenType.COMMA, "期望 ',' 在数组初始化器中");
                    }
                    Expect(TokenType.RPAREN, "expected ')'");
                    value = new LiteralNode(); // placeholder
                }
                else
                {
                    value = ParseExpression();
                }
                Expect(TokenType.SEMICOLON, "期望 ';'");

                declarations.Add(new ConstDeclarationNode
                {
                    Name = constName,
                    Value = value,
                    ConstType = constType,
                    ArrayValues = arrayValues,
                    Line = Cur.Line,
                    Column = Cur.Column
                });
            }
        }

        private void ParseTypeDeclarations(List<DeclarationNode> declarations)
        {
            Expect(TokenType.TYPE, "期望 'type'");
            
            while (GetTokenType(Cur) == TokenType.IDENTIFIER)
            {
                string typeName = Expect(TokenType.IDENTIFIER, "期望类型名").Value.ToString();
                Expect(TokenType.EQUALS, "期望 '='");
                
                TypeNode type = ParseType();
                Expect(TokenType.SEMICOLON, "期望 ';'");

                declarations.Add(new TypeDeclarationNode
                {
                    Name = typeName,
                    Type = type,
                    Line = Cur.Line,
                    Column = Cur.Column
                });
            }
        }

        private void ParseLabelDeclarations()
        {
            Expect(TokenType.LABEL, "期望 'label'");
            do
            {
                if (GetTokenType(Cur) == TokenType.IDENTIFIER || GetTokenType(Cur) == TokenType.INTEGER_LITERAL)
                    Advance();
                else
                    GccError("期望标签名", ErrorCode.Parser_ExpectedIdentifier);
            } while (Match(TokenType.COMMA));
            Expect(TokenType.SEMICOLON, "期望 ';'");
        }

        private TypeNode ParseType()
        {
            Token token = Cur;
            
            if (Match(TokenType.ARRAY))
            {
                if (Match(TokenType.OF))
                {
                    TypeNode dynamicElementType = ParseType();
                    return new ArrayTypeNode
                    {
                        IsDynamic = true,
                        ElementType = dynamicElementType,
                        Line = token.Line,
                        Column = token.Column
                    };
                }

                Expect(TokenType.LBRACKET, "期望 '['");
                ExpressionNode lowerBound = ParseExpression();
                Expect(TokenType.RANGE, "期望 '..'");
                ExpressionNode upperBound = ParseExpression();
                // 多维数组: `array[a..b, c..d] of T`
                //
                // ⚠ **不能展平成一维**（此前是 `[1 .. (b-a+1)*(d-c+1)]`）：
                //   展平**把每一维的上下界扔了**，而 `A[i, j]` 的地址要按
                //   `(i-a)*stride0 + (j-c)*elemSize` 算 —— 少了两端的下界就用不上，
                //   代码生成那边只能退回默认的 `[1..10]`（实测 `array[0..3,0..3]` 的
                //   第 0 维步长被算成 4×10=40，且 `i` 被减了 1）。
                //   建成**嵌套的一维数组**之后，`CollectArrayBounds` 天然收到每一维的界，
                //   槽位数（`GetVariableSlots` 递归相乘）与展平时完全一致。
                var dims = new List<(ExpressionNode Lo, ExpressionNode Hi)>
                {
                    (lowerBound, upperBound)
                };
                while (Match(TokenType.COMMA))
                {
                    ExpressionNode lb2 = ParseExpression();
                    Expect(TokenType.RANGE, "期望 '..'");
                    ExpressionNode ub2 = ParseExpression();
                    dims.Add((lb2, ub2));
                }
                Expect(TokenType.RBRACKET, "期望 ']'");
                Expect(TokenType.OF, "期望 'of'");
                TypeNode elementType = ParseType();

                // 由内向外包：`a..b, c..d of T` ⇒ `array[a..b] of array[c..d] of T`
                for (int d = dims.Count - 1; d >= 0; d--)
                {
                    elementType = new ArrayTypeNode
                    {
                        LowerBound = dims[d].Lo,
                        UpperBound = dims[d].Hi,
                        ElementType = elementType,
                        Line = token.Line,
                        Column = token.Column
                    };
                }
                return elementType;
            }
            else if (Match(TokenType.CARET))
            {
                return new PointerTypeNode
                {
                    TargetType = ParseType(),
                    Line = token.Line,
                    Column = token.Column
                };
            }
            else if (GetTokenType(Cur) == TokenType.INTEGER_LITERAL || GetTokenType(Cur) == TokenType.CHAR_LITERAL)
            {
                ExpressionNode lowerBound = ParseExpression();
                if (Match(TokenType.RANGE))
                {
                    return new SubrangeTypeNode
                    {
                        LowerBound = lowerBound,
                        UpperBound = ParseExpression(),
                        Line = token.Line,
                        Column = token.Column
                    };
                }
                // 报出来 + 用 INTEGER 占位继续（**不返回 null**：null 会流到代码生成再崩，
                // 把真正的错盖成一句没有位置的「内部错误」）。见 `ParserBase.Collect` 的两分法。
                GccError("期望子界类型上界", ErrorCode.Parser_ExpectedExpression);
                return new SimpleTypeNode { TypeName = "INTEGER", Line = token.Line, Column = token.Column };
            }
            else if (GetTokenType(Cur) == TokenType.PROCEDURE || GetTokenType(Cur) == TokenType.FUNCTION
                  || GetTokenType(Cur) == TokenType.CONSTRUCTOR || GetTokenType(Cur) == TokenType.DESTRUCTOR)
            {
                bool isCtor = Match(TokenType.CONSTRUCTOR);
                bool isDtor = !isCtor && Match(TokenType.DESTRUCTOR);
                bool isFunction = !isCtor && !isDtor && Match(TokenType.FUNCTION);
                if (!isFunction && !isCtor && !isDtor) Expect(TokenType.PROCEDURE, "期望 'procedure'");
                var procType = new ProcedureTypeNode { IsFunction = isFunction, Line = token.Line, Column = token.Column };
                if (isCtor) procType.IsConstructor = true;
                if (isDtor) procType.IsDestructor = true;
                if (Match(TokenType.LPAREN))
                {
                    ParseParameterList(procType.Parameters);
                    Expect(TokenType.RPAREN, "期望 ')'");
                }
                if (isFunction && Match(TokenType.COLON))
                    procType.ReturnType = ParseType();
                return procType;
            }
            else if (Match(TokenType.SET))
            {
                Expect(TokenType.OF, "期望 'of'");
                
                // 检查是否是范围表达式（如 0..31）
                if (GetTokenType(Cur) == TokenType.INTEGER_LITERAL || GetTokenType(Cur) == TokenType.CHAR_LITERAL)
                {
                    // 解析范围表达式
                    ExpressionNode lowerBound = ParseExpression();
                    Expect(TokenType.RANGE, "期望 '..'");
                    ExpressionNode upperBound = ParseExpression();
                    
                    return new SetTypeNode
                    {
                        LowerBound = lowerBound,
                        UpperBound = upperBound,
                        BaseType = null, // 范围表达式时BaseType为null
                        Line = token.Line,
                        Column = token.Column
                    };
                }
                else
                {
                    // 简单类型（如 set of CHAR）
                    TypeNode baseType = ParseType();
                    
                    return new SetTypeNode
                    {
                        BaseType = baseType,
                        Line = token.Line,
                        Column = token.Column
                    };
                }
            }
            else if (Match(TokenType.CLASS) || Match(TokenType.OBJECT))
            {
                // Delphi/FreePascal class/object → 映射为 RECORD (v1.66.32+)
                RecordTypeNode record = new RecordTypeNode
                {
                    Line = token.Line,
                    Column = token.Column
                };
                while (!Match(TokenType.END))
                {
                    // 解析字段: name1, name2: type;
                    List<string> names = new List<string>();
                    do
                    {
                        if (GetTokenType(Cur) == TokenType.IDENTIFIER)
                            names.Add(Expect(TokenType.IDENTIFIER, "期望标识符").Value?.ToString() ?? "");
                        else break;
                    } while (Match(TokenType.COMMA));
                    if (names.Count > 0)
                    {
                        Expect(TokenType.COLON, "期望 ':'");
                        TypeNode fieldType = ParseType();
                        Expect(TokenType.SEMICOLON, "期望 ';'");
                        foreach (var n in names)
                            record.Fields.Add(new VarDeclarationNode { Name = n, Type = fieldType, Line = token.Line, Column = token.Column });
                    }
                    else if (GetTokenType(Cur) == TokenType.CONSTRUCTOR || GetTokenType(Cur) == TokenType.DESTRUCTOR
                          || GetTokenType(Cur) == TokenType.PROCEDURE || GetTokenType(Cur) == TokenType.FUNCTION
                          || GetTokenType(Cur) == TokenType.PROPERTY)
                    {
                        // 跳过方法/属性头直到 ';'
                        while (!Match(TokenType.SEMICOLON) && !Match(TokenType.EOF) && !Match(TokenType.END))
                            Advance();
                    }
                    else
                    {
                        if (!Match(TokenType.END) && !Match(TokenType.EOF))
                            Advance();
                    }
                }
                return record;
            }
            else if (Match(TokenType.RECORD))
            {
                RecordTypeNode record = new RecordTypeNode
                {
                    Line = token.Line,
                    Column = token.Column
                };

                while (GetTokenType(Cur) != TokenType.END)
                {
                    if (GetTokenType(Cur) == TokenType.CASE)
                    {
                        ParseVariantRecordPart(record);
                    }
                    else if (GetTokenType(Cur) == TokenType.VAR)
                    {
                        var tempDeclarations = new List<DeclarationNode>();
                        ParseVarDeclarations(tempDeclarations);
                        
                        foreach (var decl in tempDeclarations)
                        {
                            if (decl is VarDeclarationNode varDecl)
                            {
                                record.Fields.Add(varDecl);
                            }
                        }
                    }
                    else if (GetTokenType(Cur) == TokenType.IDENTIFIER)
                    {
                        List<string> fieldNames = new List<string>();
                        do
                        {
                            fieldNames.Add(Expect(TokenType.IDENTIFIER, "期望字段名").Value.ToString());
                        } while (Match(TokenType.COMMA));
                        
                        Expect(TokenType.COLON, "期望 ':'");

                        TypeNode fieldType = ParseType();
                        // 分号在 end 前可选
                        if (GetTokenType(Cur) != TokenType.END)
                            Expect(TokenType.SEMICOLON, "期望 ';'");
                        
                        foreach (string fieldName in fieldNames)
                        {
                            record.Fields.Add(new VarDeclarationNode
                            {
                                Name = fieldName,
                                Type = fieldType,
                                Line = token.Line,
                                Column = token.Column
                            });
                        }
                    }
                    else
                    {
                        // 报出来 + 继续（循环末尾会吃分号/推进，不会原地打转）
                        GccError("期望字段声明或 'end'", ErrorCode.Parser_InvalidDeclaration);
                    }
                }

                Expect(TokenType.END, "期望 'end'");
                return record;
            }
            else if (Match(TokenType.LPAREN))
            {
                // 枚举类型: (Up, Down, Left, Right)
                EnumTypeNode enumType = new EnumTypeNode
                {
                    Line = token.Line,
                    Column = token.Column
                };
                while (true)
                {
                    enumType.Values.Add(Expect(TokenType.IDENTIFIER, "期望枚举值").Value.ToString());
                    if (GetTokenType(Cur) == TokenType.COMMA)
                        Advance();
                    else
                        break;
                }
                Expect(TokenType.RPAREN, "期望 ')'");
                return enumType;
            }
            /* ── `file` / `text` / `textfile`：**按名字认，不按 token 类型认** ──────────
             *
             * 它们从前是 `TokenType.FILE` / `TokenType.TEXT`（保留字），而 Turbo Pascal 里
             * 它们是**预定义类型标识符**、可以被用户重新定义成变量名或形参名
             * （`var Text: string;` 是老程序里的常见写法）。判据与理由见 `Lexer` 关键字表里
             * 那段注释 —— 简言之：**当保留字会让用户的变量名编不过**。
             *
             * ⚠ 这两支必须排在**下面那个 `Match(TokenType.IDENTIFIER)` 之前**：
             * 现在它们是普通 IDENTIFIER，放到后面就被通用分支吃掉了
             * （那条分支只特判 `TextFile`，`file`/`text` 会一路当成"未知类型名"）。 */
            else if (IsIdentifierNamed(token, "file"))
            {
                Advance();
                FileTypeNode fileType = new FileTypeNode
                {
                    Line = token.Line,
                    Column = token.Column
                };
                // `FILE OF <type>` 是无类型文件带元素类型；裸 `FILE` 是无类型文件
                if (Match(TokenType.OF))
                    fileType.ElementType = ParseType();
                return fileType;
            }
            else if (IsIdentifierNamed(token, "text") || IsIdentifierNamed(token, "textfile"))
            {
                Advance();
                // TEXT / TEXTFILE 是预定义的文件类型（没有元素类型）
                return new FileTypeNode
                {
                    ElementType = null,
                    Line = token.Line,
                    Column = token.Column
                };
            }
            else if (Match(TokenType.IDENTIFIER))
            {
                if (string.Equals(token.Value.ToString(), "TextFile", StringComparison.OrdinalIgnoreCase))
                {
                    return new FileTypeNode { ElementType = null, Line = token.Line, Column = token.Column };
                }
                return new SimpleTypeNode
                {
                    TypeName = token.Value.ToString(),
                    Line = token.Line,
                    Column = token.Column
                };
            }
            else if (Match(TokenType.INTEGER, TokenType.REAL, TokenType.BOOLEAN, TokenType.CHAR, TokenType.STRING))
            {
                // string[N] 定长字符串 → 映射为 string (v1.66.33)
                if (token.Type == TokenType.STRING && Match(TokenType.LBRACKET))
                {
                    // 跳过 [N] 或 [常量名] 部分
                    if (GetTokenType(Cur) == TokenType.INTEGER_LITERAL || GetTokenType(Cur) == TokenType.IDENTIFIER)
                        Advance();
                    else if (GetTokenType(Cur) == TokenType.PLUS || GetTokenType(Cur) == TokenType.MINUS)
                    {
                        Advance(); // 跳过符号
                        if (GetTokenType(Cur) == TokenType.INTEGER_LITERAL || GetTokenType(Cur) == TokenType.IDENTIFIER)
                            Advance();
                    }
                    Expect(TokenType.RBRACKET, "期望 ']'");
                }
                return new SimpleTypeNode
                {
                    TypeName = token.Type.ToString(),
                    Line = token.Line,
                    Column = token.Column
                };
            }
            else
            {
                // 同「期望子界类型上界」：报出来 + 占位继续，不返回 null。
                GccError("期望类型声明", ErrorCode.Parser_InvalidDeclaration);
                return new SimpleTypeNode { TypeName = "INTEGER", Line = token.Line, Column = token.Column };
            }
        }

        private void ParseVariantRecordPart(RecordTypeNode record)
        {
            Expect(TokenType.CASE, "期望 'case'");
            if (GetTokenType(Cur) == TokenType.IDENTIFIER)
            {
                Advance();
                if (Match(TokenType.COLON))
                    ParseType();
            }
            Expect(TokenType.OF, "期望 'of'");

            while (GetTokenType(Cur) != TokenType.END && GetTokenType(Cur) != TokenType.EOF)
            {
                do
                {
                    ParseExpression();
                    if (Match(TokenType.RANGE))
                        ParseExpression();
                } while (Match(TokenType.COMMA));

                Expect(TokenType.COLON, "期望 ':'");
                Expect(TokenType.LPAREN, "期望 '('");
                while (GetTokenType(Cur) != TokenType.RPAREN)
                {
                    List<string> fieldNames = new List<string>();
                    do
                    {
                        fieldNames.Add(Expect(TokenType.IDENTIFIER, "期望字段名").Value.ToString());
                    } while (Match(TokenType.COMMA));

                    Expect(TokenType.COLON, "期望 ':'");
                    TypeNode fieldType = ParseType();
                    foreach (string fieldName in fieldNames)
                    {
                        record.Fields.Add(new VarDeclarationNode
                        {
                            Name = fieldName,
                            Type = fieldType,
                            Line = Cur.Line,
                            Column = Cur.Column
                        });
                    }
                    Match(TokenType.SEMICOLON);
                }
                Expect(TokenType.RPAREN, "期望 ')'");
                if (!Match(TokenType.SEMICOLON))
                    break;
            }
        }

        private BlockNode ParseBlock()
        {
            BlockNode block = new BlockNode
            {
                Line = Cur.Line,
                Column = Cur.Column
            };
            // 三个调用点都在 `begin` 之后 —— 这个 `begin` 就是"块没关上"时该指的地方。
            var open = Previous();

            // ⚠ 这个循环从前**没有 EOF 出口**（只认 `end` / `until`）：文件在块中间结束时
            //   `Cur` 停在 EOF 上，`ParseStatement()` 也推进不了 ⇒ **空转**。
            //   实测 `program p; begin WriteLn(1);`（少 `end.`）不报"块未闭合"，
            //   而是由运行期兜底 `ProgressGuard` 抛出一条
            //   「解析未收敛（在同一处反复读取、从不推进）—— 这是编译器内部缺陷，请报告给开发者」。
            //   **兜底能兜住不挂死，但把"用户少写了一个 end"说成了"编译器坏了"** ——
            //   位置是对的、归因是错的，而用户会照着那句话去提 bug。兜底不是诊断。
            while (GetTokenType(Cur) != TokenType.END && GetTokenType(Cur) != TokenType.UNTIL
                   && GetTokenType(Cur) != TokenType.EOF)
            {
                block.Statements.Add(ParseStatement());
                if (GetTokenType(Cur) == TokenType.SEMICOLON)
                {
                    Advance();
                }
            }
            if (GetTokenType(Cur) == TokenType.EOF)
                GccErrorAt("块未闭合（缺少 'end'）", open, ErrorCode.Parser_SyntaxError);

            return block;
        }

        private StatementNode ParseStatement()
        {
            Token token = Cur;
            
            if ((token.Type == TokenType.IDENTIFIER || token.Type == TokenType.INTEGER_LITERAL) && Peek(1).Type == TokenType.COLON)
            {
                string label = token.Value.ToString();
                Advance();
                Expect(TokenType.COLON, "期望 ':'");
                return new LabeledStatementNode { Label = label, Statement = ParseStatement(), Line = token.Line, Column = token.Column };
            }
            else if (token.Type == TokenType.IDENTIFIER)
            {
                // 可能是赋值或过程调用
                Token next = Peek(1);
                if (next.Type == TokenType.ASSIGN || next.Type == TokenType.LBRACKET || next.Type == TokenType.DOT || next.Type == TokenType.CARET)
                {
                    return ParseAssignment();
                }
                else
                {
                    return ParseProcedureCall();
                }
            }
            else if (token.Type == TokenType.BEGIN)
            {
                return ParseCompoundStatement();
            }
            else if (token.Type == TokenType.IF)
            {
                return ParseIfStatement();
            }
            else if (token.Type == TokenType.WHILE)
            {
                return ParseWhileStatement();
            }
            else if (token.Type == TokenType.FOR)
            {
                return ParseForStatement();
            }
            else if (token.Type == TokenType.REPEAT)
            {
                return ParseRepeatStatement();
            }
            else if (token.Type == TokenType.CASE)
            {
                return ParseCaseStatement();
            }
            else if (token.Type == TokenType.WITH)
            {
                return ParseWithStatement();
            }
            else if (token.Type == TokenType.GOTO)
            {
                Advance();
                string label = Cur.Value.ToString();
                if (GetTokenType(Cur) == TokenType.IDENTIFIER || GetTokenType(Cur) == TokenType.INTEGER_LITERAL)
                    Advance();
                else
                {
                    // 报出来 + **吃掉这个 token 保证推进**：本函数返回后由语句循环接手，
                    // 一个 token 都不动的话下一轮还是同一个 token、同一处错（原地打转）。
                    GccError("期望标签名", ErrorCode.Parser_ExpectedIdentifier);
                    Advance();
                }
                return new GotoNode { Label = label, Line = token.Line, Column = token.Column };
            }
            else if (token.Type == TokenType.BREAK)
            {
                Advance();
                return new BreakNode();
            }
            else if (token.Type == TokenType.CONTINUE)
            {
                Advance();
                return new ContinueNode();
            }
            else
            {
                // 报出来 + 返回**空复合语句**占位（一个不生成任何指令的 no-op）。
                //
                // ⚠ **不能返回 null**：语句列表是 `block.Statements.Add(ParseStatement())`、
                //   代码生成侧 `foreach (var stmt in …) GenerateStatement(stmt)` **没有 null 判据**
                //   ⇒ null 进去就是另一处 NRE，把真正的错盖掉。
                //
                // ⚠ **必须吃掉一个 token 再返回**（与上面 GOTO 那一支同一个理由）：
                //   `GccError` 在**接了诊断收集器**的路径上是"收集并继续"、不抛异常，
                //   而所有语句循环的写法都是 `while (Cur != 结束标记) ParseStatement();`
                //   —— 一个 token 都不动的话下一轮还是同一个 token、同一个位置，
                //   于是**空转到进展守卫的百万次阈值**，用户拿到的是一句
                //   「解析未收敛…这是编译器内部缺陷，请把这段输入报告给开发者」，
                //   而真正的原因（这里有个不认识的记号）**一个字都没报出来**。
                //   实测语料里 10 份程序是这么挂的，其中 `g7iles_life.pas` 的
                //   调用栈在 `ParseRepeatStatement` 的 `while` 上原地打转。
                GccError("期望语句", ErrorCode.Parser_SyntaxError);
                if (GetTokenType(Cur) != TokenType.EOF)
                    Advance();
                return new CompoundStatementNode { Line = token.Line, Column = token.Column };
            }
        }

        private WithNode ParseWithStatement()
        {
            Token token = Cur;
            Expect(TokenType.WITH, "期望 'with'");
            WithNode withNode = new WithNode { Line = token.Line, Column = token.Column };
            do
            {
                withNode.Variables.Add(ParseVariable());
            } while (Match(TokenType.COMMA));
            Expect(TokenType.DO, "期望 'do'");
            string contextName = withNode.Variables.Count > 0 ? withNode.Variables[withNode.Variables.Count - 1].Name : "";
            withContext.Push(contextName);
            withNode.Body = ParseStatement();
            withContext.Pop();
            return withNode;
        }

        private AssignmentNode ParseAssignment()
        {
            VariableNode variable = ParseVariable();
            Expect(TokenType.ASSIGN, "期望 ':='");
            ExpressionNode expression = ParseExpression();

            return new AssignmentNode
            {
                Variable = variable,
                Expression = expression,
                Line = variable.Line,
                Column = variable.Column
            };
        }

        private ProcedureCallNode ParseProcedureCall()
        {
            string name = Expect(TokenType.IDENTIFIER, "期望过程名").Value.ToString();
            ProcedureCallNode call = new ProcedureCallNode
            {
                Name = name,
                Line = Cur.Line,
                Column = Cur.Column
            };

            if (Match(TokenType.LPAREN))
            {
                if (GetTokenType(Cur) != TokenType.RPAREN)
                {
                    do
                    {
                        call.Arguments.Add(ParseExpression());
                        // 跳过可选的 :width 和 :width:precision 格式说明符 (v1.66.33)
                        while (GetTokenType(Cur) == TokenType.COLON)
                        {
                            Advance(); // 跳过 ':'
                            if (GetTokenType(Cur) == TokenType.INTEGER_LITERAL
                                || GetTokenType(Cur) == TokenType.IDENTIFIER
                                || GetTokenType(Cur) == TokenType.MINUS
                                || GetTokenType(Cur) == TokenType.PLUS)
                                Advance(); // 跳过宽度值
                            // 可选的第二个冒号 + 精度
                            if (GetTokenType(Cur) == TokenType.COLON)
                            {
                                Advance();
                                if (GetTokenType(Cur) == TokenType.INTEGER_LITERAL
                                    || GetTokenType(Cur) == TokenType.IDENTIFIER)
                                    Advance();
                            }
                        }
                    } while (Match(TokenType.COMMA));
                }
                Expect(TokenType.RPAREN, "期望 ')'");
            }

            return call;
        }

        private CompoundStatementNode ParseCompoundStatement()
        {
            Expect(TokenType.BEGIN, "期望 'begin'");
            CompoundStatementNode compound = new CompoundStatementNode
            {
                Line = Cur.Line,
                Column = Cur.Column
            };

            while (GetTokenType(Cur) != TokenType.END)
            {
                compound.Statements.Add(ParseStatement());
                if (GetTokenType(Cur) == TokenType.SEMICOLON)
                {
                    Advance();
                }
            }

            Expect(TokenType.END, "期望 'end'");
            return compound;
        }

        private IfNode ParseIfStatement()
        {
            Expect(TokenType.IF, "期望 'if'");
            ExpressionNode condition = ParseExpression();
            Expect(TokenType.THEN, "期望 'then'");
            StatementNode thenBranch = ParseStatement();

            IfNode ifNode = new IfNode
            {
                Condition = condition,
                ThenBranch = thenBranch,
                Line = condition.Line,
                Column = condition.Column
            };

            if (Match(TokenType.ELSE))
            {
                ifNode.ElseBranch = ParseStatement();
            }

            return ifNode;
        }

        private WhileNode ParseWhileStatement()
        {
            Expect(TokenType.WHILE, "期望 'while'");
            ExpressionNode condition = ParseExpression();
            Expect(TokenType.DO, "期望 'do'");
            StatementNode body = ParseStatement();

            return new WhileNode
            {
                Condition = condition,
                Body = body,
                Line = condition.Line,
                Column = condition.Column
            };
        }

        private ForNode ParseForStatement()
        {
            Expect(TokenType.FOR, "期望 'for'");
            string variable = Expect(TokenType.IDENTIFIER, "期望循环变量").Value.ToString();

            // for/in 语法 (Delphi/FreePascal v1.66.32+)
            if (Match(TokenType.IN))
            {
                // for i in low..high do → 解析范围值
                int startVal = 1, endVal = 10;
                if (GetTokenType(Cur) == TokenType.INTEGER_LITERAL)
                {
                    startVal = int.Parse(Cur.Value.ToString());
                    Advance();
                }
                if (Match(TokenType.RANGE)) // ..
                {
                    if (GetTokenType(Cur) == TokenType.INTEGER_LITERAL)
                    {
                        endVal = int.Parse(Cur.Value.ToString());
                        Advance();
                    }
                }
                Expect(TokenType.DO, "期望 'do'");
                var inBody = ParseStatement();
                return new ForNode
                {
                    Variable = variable,
                    StartValue = new LiteralNode { Value = startVal, Type = TokenType.INTEGER_LITERAL },
                    EndValue = new LiteralNode { Value = endVal, Type = TokenType.INTEGER_LITERAL },
                    IsDownTo = false,
                    Body = inBody,
                    Line = Cur.Line, Column = Cur.Column
                };
            }

            Expect(TokenType.ASSIGN, "期望 ':='");
            ExpressionNode startValue = ParseExpression();

            bool isDownTo = Match(TokenType.DOWNTO);
            if (!isDownTo)
            {
                Expect(TokenType.TO, "期望 'to' 或 'downto'");
            }

            ExpressionNode endValue = ParseExpression();
            Expect(TokenType.DO, "期望 'do'");
            StatementNode body = ParseStatement();

            return new ForNode
            {
                Variable = variable,
                StartValue = startValue,
                EndValue = endValue,
                IsDownTo = isDownTo,
                Body = body,
                Line = Cur.Line,
                Column = Cur.Column
            };
        }

        private RepeatNode ParseRepeatStatement()
        {
            Expect(TokenType.REPEAT, "期望 'repeat'");
            RepeatNode repeat = new RepeatNode
            {
                Line = Cur.Line,
                Column = Cur.Column
            };

            while (GetTokenType(Cur) != TokenType.UNTIL)
            {
                repeat.Statements.Add(ParseStatement());
                if (GetTokenType(Cur) == TokenType.SEMICOLON)
                {
                    Advance();
                }
            }

            Expect(TokenType.UNTIL, "期望 'until'");
            repeat.Condition = ParseExpression();

            return repeat;
        }

        private CaseNode ParseCaseStatement()
        {
            Expect(TokenType.CASE, "期望 'case'");
            CaseNode caseNode = new CaseNode
            {
                Line = Cur.Line,
                Column = Cur.Column
            };

            caseNode.Expression = ParseExpression();
            Expect(TokenType.OF, "期望 'of'");

            // 解析各个分支
            while (GetTokenType(Cur) != TokenType.OTHERWISE && GetTokenType(Cur) != TokenType.ELSE && GetTokenType(Cur) != TokenType.END && GetTokenType(Cur) != TokenType.EOF)
            {
                CaseBranchNode branch = ParseCaseBranch();
                caseNode.Branches.Add(branch);
                if (GetTokenType(Cur) == TokenType.SEMICOLON)
                {
                    Advance();
                }
            }

            // 解析otherwise/else分支（可选）
            if (GetTokenType(Cur) == TokenType.OTHERWISE || GetTokenType(Cur) == TokenType.ELSE)
            {
                Advance(); // 跳过 otherwise/else
                caseNode.OtherwiseBranch = ParseStatement();
                if (GetTokenType(Cur) == TokenType.SEMICOLON)
                {
                    Advance();
                }
            }

            Expect(TokenType.END, "期望 'end'");
            return caseNode;
        }

        private CaseBranchNode ParseCaseBranch()
        {
            CaseBranchNode branch = new CaseBranchNode
            {
                Line = Cur.Line,
                Column = Cur.Column
            };

            // 解析值列表: 1, 3..5, 7
            do
            {
                ExpressionNode value = ParseExpression();
                branch.Values.Add(value);

                // 检查是否是范围 (..)
                if (GetTokenType(Cur) == TokenType.RANGE)
                {
                    // 范围: low..high - 当前value是low，解析high
                    Advance(); // 跳过 ..
                    ExpressionNode highValue = ParseExpression();
                    branch.Values.Add(highValue); // 添加high值作为范围标记
                }
            } while (Match(TokenType.COMMA));

            Expect(TokenType.COLON, "期望 ':'");
            branch.Statement = ParseStatement();

            return branch;
        }

        private VariableNode ParseVariable()
        {
            // ⚠ **位置要在消费这个标识符之前取**：`Expect` 会把游标推到下一个 token，
            //   照旧写 `Line = Cur.Line, Column = Cur.Column` 取到的是**名字后面那个符号**
            //   的位置 —— 实测 `WriteLn(nosuch)` 报列 17（那是 `(` 的列），而 `nosuch` 起于 11。
            //   行号碰巧还对（同一行），所以只有列错，最容易被漏掉。
            var nameTok = Cur;
            string name = Expect(TokenType.IDENTIFIER, "期望变量名").Value.ToString();
            VariableNode variable = new VariableNode
            {
                Name = name,
                Line = nameTok.Line,
                Column = nameTok.Column
            };

            if (withContext.Count > 0 && GetTokenType(Cur) != TokenType.LBRACKET && GetTokenType(Cur) != TokenType.DOT && GetTokenType(Cur) != TokenType.CARET)
            {
                variable.Field = name;
                variable.Name = withContext.Peek();
            }

            // 处理数组索引
            //
            // ⚠ **`A[i, j]` 是 Turbo Pascal 唯一的"多维下标"写法**（`A[i][j]` 那种
            //   嵌套方括号是老程序里几乎不会出现的写法）。此前这里只收一个表达式，
            //   见到逗号就报 `期望 ']'` —— 语料里 `Grid[i,j]` / `points[loop1,1]` /
            //   `Bricks[J,I]` 一大批都卡在这一条上（15 个 `期望 ']'` 里有 11 个是它）。
            //   逗号在 `[…]` 里面**只可能**是维度分隔（集合字面量走的是解析器的
            //   另一条路 `ParseFactorCore`，不会走到这里），所以按逗号拆开即可，
            //   每个下标依次进 `Indices` —— 下游 `GenerateVariableAddress` 的多维分支
            //   本来就是按"扁平的下标表 + 各维步长"算地址的。
            while (Match(TokenType.LBRACKET))
            {
                do
                {
                    variable.Indices.Add(ParseExpression());
                }
                while (Match(TokenType.COMMA));
                Expect(TokenType.RBRACKET, "期望 ']'");
            }

            // 处理记录字段 (支持多级 b.a.v)
            while (Match(TokenType.DOT))
            {
                string fname = Expect(TokenType.IDENTIFIER, "期望字段名").Value.ToString();
                if (variable.Field == null)
                    variable.Field = fname;
                else
                    variable.Fields.Add(fname);
            }

            while (Match(TokenType.CARET))
            {
                variable.DereferenceCount++;
                while (Match(TokenType.LBRACKET))
                {
                    do
                    {
                        variable.Indices.Add(ParseExpression());
                    }
                    while (Match(TokenType.COMMA));
                    Expect(TokenType.RBRACKET, "期望 ']'");
                }
                while (Match(TokenType.DOT))
                {
                    string fname = Expect(TokenType.IDENTIFIER, "期望字段名").Value.ToString();
                    if (variable.Field == null)
                        variable.Field = fname;
                    else
                        variable.Fields.Add(fname);
                }
            }

            return variable;
        }
    }
}
