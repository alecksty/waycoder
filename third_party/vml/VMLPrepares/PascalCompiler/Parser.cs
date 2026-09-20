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

        protected override Token Expect(TokenType tokenType, string message)
        {
            if (Check(tokenType)) return Advance();
            string msg = message ?? VMLPlugins.Strings.ExpectedToken(tokenType.ToString(), GetTokenType(Cur).ToString());
            throw Error(VMLPlugins.Strings.SyntaxErrorAt(Cur.Line, Cur.Column, msg));
        }

        protected override ParseException Error(string message)
            => new ParseException(VMLPlugins.Strings.SyntaxErrorAt(Cur.Line, Cur.Column, message), Cur!);

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
            Expect(TokenType.PROGRAM, "期望 'program'");
            program.Name = Expect(TokenType.IDENTIFIER, "期望程序名").Value.ToString();
            // 跳过可选的程序参数 (input, output) — ISO Pascal 标准语法
            if (GetTokenType(Cur) == TokenType.LPAREN)
            {
                while (GetTokenType(Cur) != TokenType.RPAREN && GetTokenType(Cur) != TokenType.EOF)
                    Advance();
                Expect(TokenType.RPAREN, "期望 ')'");
            }
            Expect(TokenType.SEMICOLON, "期望 ';'");

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
                    Error("期望标签名");
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
                // 多维数组: array[a..b, c..d, ...] → 展平为 1D (v1.66.33)
                while (Match(TokenType.COMMA))
                {
                    ExpressionNode lb2 = ParseExpression();
                    Expect(TokenType.RANGE, "期望 '..'");
                    ExpressionNode ub2 = ParseExpression();
                    // 展平: size1 = (upper - lower + 1), size2 = (ub2 - lb2 + 1), total = size1 * size2
                    var one1 = new LiteralNode { Value = 1, Type = TokenType.INTEGER_LITERAL, Line = token.Line, Column = token.Column };
                    var one2 = new LiteralNode { Value = 1, Type = TokenType.INTEGER_LITERAL, Line = token.Line, Column = token.Column };
                    var size1 = new BinaryOpNode { Left = upperBound, Operator = TokenType.MINUS, Right = lowerBound, Line = token.Line, Column = token.Column };
                    size1 = new BinaryOpNode { Left = size1, Operator = TokenType.PLUS, Right = one1, Line = token.Line, Column = token.Column };
                    var size2 = new BinaryOpNode { Left = ub2, Operator = TokenType.MINUS, Right = lb2, Line = token.Line, Column = token.Column };
                    size2 = new BinaryOpNode { Left = size2, Operator = TokenType.PLUS, Right = one2, Line = token.Line, Column = token.Column };
                    upperBound = new BinaryOpNode { Left = size1, Operator = TokenType.STAR, Right = size2, Line = token.Line, Column = token.Column };
                    lowerBound = new LiteralNode { Value = 1, Type = TokenType.INTEGER_LITERAL, Line = token.Line, Column = token.Column };
                }
                Expect(TokenType.RBRACKET, "期望 ']'");
                Expect(TokenType.OF, "期望 'of'");
                TypeNode elementType = ParseType();

                return new ArrayTypeNode
                {
                    LowerBound = lowerBound,
                    UpperBound = upperBound,
                    ElementType = elementType,
                    Line = token.Line,
                    Column = token.Column
                };
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
                Error("期望子界类型上界");
                return null;
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
                        Error("期望字段声明或 'end'");
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
            else if (Match(TokenType.FILE))
            {
                FileTypeNode fileType = new FileTypeNode
                {
                    Line = token.Line,
                    Column = token.Column
                };
                
                // 检查是否是 FILE OF <type> 或 TEXT
                if (Match(TokenType.OF))
                {
                    // FILE OF <type>
                    fileType.ElementType = ParseType();
                }
                else
                {
                    // 简单 FILE 类型 (无类型文件) 或 TEXT
                    // 这里我们假设是简单文件类型
                    // TEXT 类型会在下面单独处理
                }
                
                return fileType;
            }
            else if (Match(TokenType.TEXT))
            {
                // TEXT 是预定义的文件类型
                return new FileTypeNode
                {
                    ElementType = null, // TEXT 文件没有元素类型
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
                Error("期望类型声明");
                return null;
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

            while (GetTokenType(Cur) != TokenType.END && GetTokenType(Cur) != TokenType.UNTIL)
            {
                block.Statements.Add(ParseStatement());
                if (GetTokenType(Cur) == TokenType.SEMICOLON)
                {
                    Advance();
                }
            }

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
                    Error("期望标签名");
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
                Error("期望语句");
                return null;
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
            while (Match(TokenType.LBRACKET))
            {
                variable.Indices.Add(ParseExpression());
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
                    variable.Indices.Add(ParseExpression());
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
