using System;
using System.Collections.Generic;
using CompilerBase;

namespace LadderCompiler
{
    /// <summary>
    /// 梯形图语法分析器
    /// 支持IEC 61131-3梯形图语法
    /// </summary>
    public class Parser : ParserBase<Token, TokenType>
    {
        private readonly Dictionary<string, string> _typeAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        protected override TokenType GetTokenType(Token token) => token.Type;

        public Parser(List<Token> tokens) : base(tokens) { }

        protected override Token Expect(TokenType type, string message)
        {
            if (Check(type)) return Advance();
            string msg = message ?? VMLPlugins.Strings.ExpectedToken(type.ToString(), GetTokenType(Cur).ToString());
            throw Error(VMLPlugins.Strings.SyntaxErrorAt(Cur.Line, Cur.Column, msg));
        }

        private void SkipWhitespaceAndComments()
        {
            while (_pos < _tokens.Count && 
                   (GetTokenType(Cur) == TokenType.Whitespace || 
                    GetTokenType(Cur) == TokenType.NewLine || 
                    GetTokenType(Cur) == TokenType.Comment))
            {
                Advance();
            }
        }

        private void SkipInlineWhitespaceAndComments()
        {
            while (_pos < _tokens.Count &&
                   (GetTokenType(Cur) == TokenType.Whitespace || GetTokenType(Cur) == TokenType.Comment))
            {
                Advance();
            }
        }
        
        /// <summary>
        /// 解析程序
        /// </summary>
        public ProgramNode Parse()
        {
            SkipWhitespaceAndComments();

            // 先解析 FUNCTION 定义 (可在 PROGRAM 前)
            var functions = new List<FunctionDefinitionNode>();
            while (GetTokenType(Cur) == TokenType.KeywordFunction)
            {
                functions.Add(ParseFunctionDefinition());
                SkipWhitespaceAndComments();
            }

            while (GetTokenType(Cur) == TokenType.KeywordType)
            {
                ParseTypeDeclarations();
                SkipWhitespaceAndComments();
            }

            // 解析程序声明 (PROGRAM 关键字可选)
            ProgramNode programNode;
            if (GetTokenType(Cur) == TokenType.KeywordProgram)
            {
                Advance(); // consume PROGRAM
                SkipWhitespaceAndComments();
                programNode = new ProgramNode
                {
                    Line = Cur.Line,
                    Column = Cur.Column
                };
                // 程序名称 (可选)
                if (GetTokenType(Cur) == TokenType.Identifier)
                {
                    programNode.Name = Advance().Value;
                }
                else
                {
                    programNode.Name = "main";
                }
            }
            else
            {
                programNode = new ProgramNode
                {
                    Name = "main",
                    Line = Cur.Line,
                    Column = Cur.Column
                };
            }
            
            SkipWhitespaceAndComments();

            while (GetTokenType(Cur) == TokenType.KeywordType)
            {
                ParseTypeDeclarations();
                SkipWhitespaceAndComments();
            }
            
            // 解析变量声明部分
            while (GetTokenType(Cur) == TokenType.KeywordVar || GetTokenType(Cur) == TokenType.KeywordVarInput ||
                   GetTokenType(Cur) == TokenType.KeywordVarOutput || GetTokenType(Cur) == TokenType.KeywordVarInOut ||
                   GetTokenType(Cur) == TokenType.KeywordVarGlobal || GetTokenType(Cur) == TokenType.KeywordVarTemp)
            {
                var varKeyword = GetTokenType(Cur);
                Advance();
                
                ParseVariableDeclarations(programNode, varKeyword);
                
                Expect(TokenType.KeywordEndVar, "期望 END_VAR");
                SkipWhitespaceAndComments();
            }
            
            // 解析 BEGIN (裸语句模式允许省略 BEGIN — 例如 "PRINT_FLOAT 3.14;")
            if (GetTokenType(Cur) == TokenType.KeywordBegin)
            {
                Advance(); // consume BEGIN
            }
            else if (!IsPrintStatement() && GetTokenType(Cur) != TokenType.EOF &&
                     GetTokenType(Cur) != TokenType.KeywordEndProgram)
            {
                Expect(TokenType.KeywordBegin, "期望 BEGIN 关键字");
            }
            SkipWhitespaceAndComments();
            
            // 解析梯形图梯级和 ST 语句
            while (!Match(TokenType.KeywordEndProgram) && GetTokenType(Cur) != TokenType.EOF)
            {
                // 跳过空行和注释
                SkipWhitespaceAndComments();
                if (GetTokenType(Cur) == TokenType.KeywordEndProgram || GetTokenType(Cur) == TokenType.EOF)
                    break;

                // ST 语句: IF/WHILE/FOR 及简单赋值 (name := expr;)
                if (GetTokenType(Cur) == TokenType.KeywordVar || GetTokenType(Cur) == TokenType.KeywordVarInput ||
                    GetTokenType(Cur) == TokenType.KeywordVarOutput || GetTokenType(Cur) == TokenType.KeywordVarInOut ||
                    GetTokenType(Cur) == TokenType.KeywordVarGlobal || GetTokenType(Cur) == TokenType.KeywordVarTemp)
                {
                    // VAR 声明可以在 BEGIN 块内出现，解析后跳过
                    var varKeyword = GetTokenType(Cur);
                    Advance();
                    ParseVariableDeclarations(programNode, varKeyword);
                    Expect(TokenType.KeywordEndVar, "期望 END_VAR");
                }
                else if (GetTokenType(Cur) == TokenType.KeywordIf)
                {
                    programNode.StStatements.Add(ParseStIf());
                }
                else if (GetTokenType(Cur) == TokenType.KeywordWhile)
                {
                    programNode.StStatements.Add(ParseStWhile());
                }
                else if (GetTokenType(Cur) == TokenType.KeywordFor)
                {
                    programNode.StStatements.Add(ParseStFor());
                }
                else if (IsPrintStatement())
                {
                    // 裸打印语句: PRINT_INT / PRINT_FLOAT / PRINT_STR / PRINT_CHAR <expr>;
                    programNode.StStatements.Add(ParsePrintStatement());
                }
                else if (GetTokenType(Cur) == TokenType.Identifier)
                {
                    // 赋值语句 → 走ST路径 (ParseStStatement生成AssignmentNode)
                    programNode.StStatements.Add(ParseStStatement());
                }
                else
                {
                    var rung = ParseLadderRung();
                    if (rung != null && (rung.Elements.Count > 0 || rung.Output != null))
                    {
                        programNode.Rungs.Add(rung);
                    }
                }
                SkipWhitespaceAndComments();
            }
            
            programNode.Functions = functions;
            return programNode;
        }

        private FunctionDefinitionNode ParseFunctionDefinition()
        {
            Advance();
            SkipWhitespaceAndComments();
            string name = Expect(TokenType.Identifier, "期望函数名").Value;
            SkipWhitespaceAndComments();
            string returnType = "INT";
            if (Match(TokenType.Colon))
            {
                SkipWhitespaceAndComments();
                if (GetTokenType(Cur) == TokenType.TypeInt || GetTokenType(Cur) == TokenType.Identifier)
                    returnType = Advance().Value.ToUpper();
            }
            SkipWhitespaceAndComments();
            var funcNode = new FunctionDefinitionNode { Name = name, ReturnType = returnType, Line = Cur.Line, Column = Cur.Column };
            // Parse VAR_INPUT / VAR blocks (don't skip — parse parameters)
            while (GetTokenType(Cur) == TokenType.KeywordVarInput || GetTokenType(Cur) == TokenType.KeywordVar)
            {
                bool isInput = GetTokenType(Cur) == TokenType.KeywordVarInput;
                Advance();
                SkipWhitespaceAndComments();
                while (GetTokenType(Cur) != TokenType.KeywordEndVar && GetTokenType(Cur) != TokenType.EOF)
                {
                    // Parse: name : type;
                    if (GetTokenType(Cur) == TokenType.Identifier)
                    {
                        var varNode = new VariableDeclarationNode
                        {
                            Name = Advance().Value,
                            Line = Cur.Line, Column = Cur.Column
                        };
                        SkipWhitespaceAndComments();
                        if (Match(TokenType.Colon))
                        {
                            SkipWhitespaceAndComments();
                            varNode.Type = ParseType();
                        }
                        else
                            varNode.Type = "INT";
                        if (isInput)
                            funcNode.InputParams.Add(varNode);
                        else
                            funcNode.LocalVars.Add(varNode);
                    }
                    else
                        Advance();
                    SkipWhitespaceAndComments();
                    Match(TokenType.Semicolon);
                    SkipWhitespaceAndComments();
                }
                Expect(TokenType.KeywordEndVar, "期望 END_VAR");
                SkipWhitespaceAndComments();
            }
            // Parse body: name := expr; or IF/WHILE/FOR ST statements
            while (GetTokenType(Cur) != TokenType.KeywordEndFunction && GetTokenType(Cur) != TokenType.EOF)
            {
                if (GetTokenType(Cur) == TokenType.KeywordIf)
                {
                    funcNode.StStatements.Add(ParseStIf());
                }
                else if (GetTokenType(Cur) == TokenType.KeywordWhile)
                {
                    funcNode.StStatements.Add(ParseStWhile());
                }
                else if (GetTokenType(Cur) == TokenType.KeywordFor)
                {
                    funcNode.StStatements.Add(ParseStFor());
                }
                else if (GetTokenType(Cur) == TokenType.Identifier)
                {
                    var identName = Advance().Value;
                    SkipWhitespaceAndComments();
                    if (Match(TokenType.Assignment))
                    {
                        SkipWhitespaceAndComments();
                        var expr = ParseExpression();
                        var assignRung = new LadderRungNode { Line = Cur.Line, Column = Cur.Column };
                        assignRung.Output = new AssignmentNode { Variable = identName, Value = expr, Line = Cur.Line, Column = Cur.Column };
                        funcNode.Rungs.Add(assignRung);
                    }
                }
                else
                    Advance();
                SkipWhitespaceAndComments();
                Match(TokenType.Semicolon);
                SkipWhitespaceAndComments();
            }
            Expect(TokenType.KeywordEndFunction, "期望 END_FUNCTION");
            return funcNode;
        }

        /// <summary>
        /// 解析变量声明
        /// </summary>
        private void ParseVariableDeclarations(ProgramNode programNode, TokenType varKeyword)
        {
            SkipWhitespaceAndComments();
            
            while (GetTokenType(Cur) != TokenType.KeywordEndVar && GetTokenType(Cur) != TokenType.EOF)
            {
                var varNode = new VariableDeclarationNode
                {
                    Line = Cur.Line,
                    Column = Cur.Column
                };
                
                // 变量名
                var varNameToken = Expect(TokenType.Identifier, "期望变量名称");
                varNode.Name = varNameToken.Value;
                
                SkipWhitespaceAndComments();
                
                // 冒号
                Expect(TokenType.Colon, "期望冒号");
                SkipWhitespaceAndComments();
                
                // 变量类型
                varNode.Type = ParseType();
                
                SkipWhitespaceAndComments();
                
                // 可选的内存位置
                if (Match(TokenType.KeywordAt))
                {
                    SkipWhitespaceAndComments();
                    if (Match(TokenType.Percent))
                    {
                        // 构建内存地址: %I0.0
                        var memAddr = "%";
                        // 区域类型 (I/Q/M)
                        if (GetTokenType(Cur) == TokenType.Identifier)
                        {
                            memAddr += Cur.Value;
                            Advance();
                        }
                        // 字节地址 (数字)
                        if (GetTokenType(Cur) == TokenType.Number)
                        {
                            memAddr += Cur.Value;
                            Advance();
                        }
                        // 位地址 (.数字)
                        if (GetTokenType(Cur) == TokenType.Dot)
                        {
                            memAddr += ".";
                            Advance();
                            if (GetTokenType(Cur) == TokenType.Number)
                            {
                                memAddr += Cur.Value;
                                Advance();
                            }
                        }
                        varNode.MemoryLocation = memAddr;
                    }
                }
                
                SkipWhitespaceAndComments();
                
                // 可选的初始值
                if (Match(TokenType.Assignment))
                {
                    SkipWhitespaceAndComments();
                    if (GetTokenType(Cur) == TokenType.LeftBracket)
                    {
                        var initParts = new List<string>();
                        int depth = 0;
                        do
                        {
                            if (GetTokenType(Cur) == TokenType.LeftBracket) depth++;
                            if (GetTokenType(Cur) == TokenType.RightBracket) depth--;
                            initParts.Add(Cur.Value);
                            Advance();
                        } while (depth > 0 && GetTokenType(Cur) != TokenType.EOF);
                        varNode.InitialValue = string.Join("", initParts);
                    }
                    else
                    {
                        varNode.InitialValue = ExpressionToInitialString(ParseExpression());
                    }
                }
                
                SkipWhitespaceAndComments();
                
                // 可选的 RETAIN/NON_RETAIN
                if (Match(TokenType.KeywordRetain))
                {
                    varNode.IsRetain = true;
                }
                else if (Match(TokenType.KeywordNonRetain))
                {
                    varNode.IsRetain = false;
                }
                
                SkipWhitespaceAndComments();
                
                // 设置输入输出标志
                switch (varKeyword)
                {
                    case TokenType.KeywordVarInput:
                        varNode.IsInput = true;
                        break;
                    case TokenType.KeywordVarOutput:
                        varNode.IsOutput = true;
                        break;
                    case TokenType.KeywordVarInOut:
                        varNode.IsInput = true;
                        varNode.IsOutput = true;
                        break;
                }
                
                programNode.Variables.Add(varNode);
                
                SkipWhitespaceAndComments();
                
                // 分号
                Expect(TokenType.Semicolon, "期望分号");
                SkipWhitespaceAndComments();
            }
        }

        private void ParseTypeDeclarations()
        {
            Expect(TokenType.KeywordType, "期望 TYPE");
            SkipWhitespaceAndComments();

            while (GetTokenType(Cur) != TokenType.KeywordEndType && GetTokenType(Cur) != TokenType.EOF)
            {
                string typeName = Expect(TokenType.Identifier, "期望类型名称").Value;
                SkipWhitespaceAndComments();
                Expect(TokenType.Colon, "期望冒号");
                SkipWhitespaceAndComments();
                _typeAliases[typeName] = ParseType();
                SkipWhitespaceAndComments();
                Match(TokenType.Semicolon);
                SkipWhitespaceAndComments();
            }

            Expect(TokenType.KeywordEndType, "期望 END_TYPE");
        }
        
        /// <summary>
        /// 解析类型
        /// </summary>
        private string ParseType()
        {
            if (Match(TokenType.TypeBool, TokenType.TypeByte, TokenType.TypeWord, TokenType.TypeDWord, 
                        TokenType.TypeLWord, TokenType.TypeSInt, TokenType.TypeInt, TokenType.TypeDInt, 
                        TokenType.TypeLInt, TokenType.TypeUSInt, TokenType.TypeUInt, TokenType.TypeUDInt, 
                        TokenType.TypeULInt, TokenType.TypeReal, TokenType.TypeLReal, TokenType.TypeTime, 
                        TokenType.TypeDate, TokenType.TypeTimeOfDay, TokenType.TypeDateAndTime, 
                        TokenType.TypeString, TokenType.TypeWString))
            {
                return _tokens[_pos - 1].Value;
            }
            
            // 数组类型
            if (Match(TokenType.KeywordArray))
            {
                SkipWhitespaceAndComments();
                Expect(TokenType.LeftBracket, "期望 [");
                SkipWhitespaceAndComments();
                
                // 数组范围
                var rangeParts = new List<string>();
                while (GetTokenType(Cur) != TokenType.RightBracket && GetTokenType(Cur) != TokenType.EOF)
                {
                    rangeParts.Add(Cur.Value);
                    Advance();
                }
                string arrayRange = string.Join("", rangeParts);
                
                SkipWhitespaceAndComments();
                Expect(TokenType.RightBracket, "期望 ]");
                SkipWhitespaceAndComments();
                
                if (!Match(TokenType.KeywordOfType) && !Match(TokenType.KeywordOf))
                    Expect(TokenType.KeywordOfType, "期望 OF");
                SkipWhitespaceAndComments();
                
                string elementType = ParseType();
                return $"ARRAY[{arrayRange}] OF {elementType}";
            }
            
            // 结构体类型
            if (Match(TokenType.KeywordStruct))
            {
                var fields = new List<string>();
                SkipWhitespaceAndComments();
                while (GetTokenType(Cur) != TokenType.KeywordEndStruct && GetTokenType(Cur) != TokenType.EOF)
                {
                    string fieldName = Expect(TokenType.Identifier, "期望结构体成员名称").Value;
                    SkipWhitespaceAndComments();
                    Expect(TokenType.Colon, "期望冒号");
                    SkipWhitespaceAndComments();
                    fields.Add($"{fieldName}:{ParseType()}");
                    SkipWhitespaceAndComments();
                    Match(TokenType.Semicolon);
                    SkipWhitespaceAndComments();
                }
                Expect(TokenType.KeywordEndStruct, "期望 END_STRUCT");
                return $"STRUCT{{{string.Join(";", fields)}}}";
            }

            if (Match(TokenType.KeywordEnum))
            {
                var values = new List<string>();
                SkipWhitespaceAndComments();
                if (Match(TokenType.LeftParenthesis))
                {
                    SkipWhitespaceAndComments();
                    while (GetTokenType(Cur) != TokenType.RightParenthesis && GetTokenType(Cur) != TokenType.EOF)
                    {
                        values.Add(Expect(TokenType.Identifier, "期望枚举值").Value);
                        SkipWhitespaceAndComments();
                        if (!Match(TokenType.Comma)) break;
                        SkipWhitespaceAndComments();
                    }
                    Expect(TokenType.RightParenthesis, "期望 )");
                }
                else
                {
                    while (GetTokenType(Cur) != TokenType.KeywordEndEnum && GetTokenType(Cur) != TokenType.EOF)
                    {
                        values.Add(Expect(TokenType.Identifier, "期望枚举值").Value);
                        SkipWhitespaceAndComments();
                        Match(TokenType.Comma);
                        SkipWhitespaceAndComments();
                    }
                    Expect(TokenType.KeywordEndEnum, "期望 END_ENUM");
                }
                return $"ENUM{{{string.Join(",", values)}}}";
            }

            if (Match(TokenType.KeywordSubrange))
            {
                SkipWhitespaceAndComments();
                Expect(TokenType.LeftBracket, "期望 [");
                var rangeParts = new List<string>();
                while (GetTokenType(Cur) != TokenType.RightBracket && GetTokenType(Cur) != TokenType.EOF)
                {
                    rangeParts.Add(Cur.Value);
                    Advance();
                }
                Expect(TokenType.RightBracket, "期望 ]");
                SkipWhitespaceAndComments();
                string baseType = "INT";
                if (Match(TokenType.KeywordOfType) || Match(TokenType.KeywordOf))
                {
                    SkipWhitespaceAndComments();
                    baseType = ParseType();
                }
                return $"SUBRANGE[{string.Join("", rangeParts)}] OF {baseType}";
            }
            
            // 自定义类型
            string typeName;
            if (IsFunctionBlockToken(GetTokenType(Cur)))
            {
                // TON/TOF/TP/CTU/CTD/CTUD 也是有效的类型名
                typeName = Cur.Value;
                Advance();
            }
            else
            {
                var typeToken = Expect(TokenType.Identifier, "期望类型名称");
                typeName = typeToken.Value;
            }
            return _typeAliases.TryGetValue(typeName, out var aliasedType) ? aliasedType : typeName;
        }
        
        /// <summary>
        /// 解析梯形图梯级
        /// </summary>
        private LadderRungNode ParseLadderRung()
        {
            var rungNode = new LadderRungNode
            {
                Line = Cur.Line,
                Column = Cur.Column
            };

            // 跳过空行和注释
            SkipWhitespaceAndComments();

            // 如果已经是END_PROGRAM或EOF，返回空梯级
            if (GetTokenType(Cur) == TokenType.KeywordEndProgram || GetTokenType(Cur) == TokenType.EOF)
            {
                return rungNode;
            }

            // 解析梯级元素
            while (GetTokenType(Cur) != TokenType.NewLine && GetTokenType(Cur) != TokenType.EOF &&
                   GetTokenType(Cur) != TokenType.KeywordEndProgram)
            {
                // 跳过注释
                if (GetTokenType(Cur) == TokenType.Comment)
                {
                    Advance();
                    continue;
                }

                var element = ParseLadderElement();
                if (element != null)
                {
                    rungNode.Elements.Add(element);
                }
                else
                {
                    // 无法识别的元素，跳过
                    Advance();
                }
                
                SkipWhitespaceAndComments();
            }

            // 解析输出
            if (GetTokenType(Cur) != TokenType.NewLine && GetTokenType(Cur) != TokenType.EOF &&
                GetTokenType(Cur) != TokenType.KeywordEndProgram)
            {
                rungNode.Output = ParseLadderElement();
            }

            // 跳过换行符
            if (Match(TokenType.NewLine))
            {
                SkipWhitespaceAndComments();
            }

            return rungNode;
        }
        
        /// <summary>
        /// 解析梯形图元素
        /// </summary>
        private ASTNode ParseLadderElement()
        {
            SkipWhitespaceAndComments();
            
            // 触点
            if (GetTokenType(Cur) == TokenType.ContactNormallyOpen || GetTokenType(Cur) == TokenType.ContactNormallyClosed)
            {
                var contactToken = Advance();
                var contactNode = new ContactNode
                {
                    Line = contactToken.Line,
                    Column = contactToken.Column,
                    NormallyOpen = contactToken.Type == TokenType.ContactNormallyOpen
                };
                SkipWhitespaceAndComments();
                
                // 触点变量
                var contactVarToken = Expect(TokenType.Identifier, "期望触点变量名称");
                contactNode.Variable = contactVarToken.Value;
                
                return contactNode;
            }

            // IL格式指令 - 触点 (LD, LDI, AND, ANDN, OR, ORN)
            if (GetTokenType(Cur) == TokenType.LD || GetTokenType(Cur) == TokenType.LDI ||
                GetTokenType(Cur) == TokenType.ANDN || GetTokenType(Cur) == TokenType.ORN ||
                (GetTokenType(Cur) == TokenType.Logical &&
                 (Cur.Value.ToUpper() == "AND" || Cur.Value.ToUpper() == "OR")))
            {
                var ilToken = Advance();
                bool isNormallyOpen = ilToken.Type == TokenType.LD ||
                    (ilToken.Type == TokenType.Logical && ilToken.Value.ToUpper() == "AND") ||
                    (ilToken.Type == TokenType.Logical && ilToken.Value.ToUpper() == "OR");

                SkipWhitespaceAndComments();

                var contactNode = new ContactNode
                {
                    Line = ilToken.Line,
                    Column = ilToken.Column,
                    NormallyOpen = isNormallyOpen
                };

                if (GetTokenType(Cur) == TokenType.Identifier ||
                    GetTokenType(Cur) == TokenType.ValueTrue ||
                    GetTokenType(Cur) == TokenType.ValueFalse)
                {
                    contactNode.Variable = Advance().Value;
                }

                return contactNode;
            }
            if (GetTokenType(Cur) == TokenType.Coil || GetTokenType(Cur) == TokenType.CoilSet ||
                GetTokenType(Cur) == TokenType.CoilReset || GetTokenType(Cur) == TokenType.CoilPositiveTransition ||
                GetTokenType(Cur) == TokenType.CoilNegativeTransition)
            {
                var coilToken = Advance();
                var coilNode = new CoilNode
                {
                    Line = coilToken.Line,
                    Column = coilToken.Column
                };
                
                // 设置线圈类型
                coilNode.Type = coilToken.Type switch
                {
                    TokenType.Coil => CoilType.Normal,
                    TokenType.CoilSet => CoilType.Set,
                    TokenType.CoilReset => CoilType.Reset,
                    TokenType.CoilPositiveTransition => CoilType.Positive,
                    TokenType.CoilNegativeTransition => CoilType.Negative,
                    _ => CoilType.Normal
                };
                
                SkipWhitespaceAndComments();
                
                // 线圈变量
                var coilVarToken = Expect(TokenType.Identifier, "期望线圈变量名称");
                coilNode.Variable = coilVarToken.Value;
                
                // 可选的赋值表达式
                if (Match(TokenType.Assignment))
                {
                    SkipWhitespaceAndComments();
                    coilNode.Value = ParseExpression();
                }
                
                return coilNode;
            }

            // IL格式指令 - 线圈 (OUT, ST, SET, RST)
            if (GetTokenType(Cur) == TokenType.OUT || GetTokenType(Cur) == TokenType.ST ||
                GetTokenType(Cur) == TokenType.SET || GetTokenType(Cur) == TokenType.RST)
            {
                var ilToken = Advance();
                var coilNode = new CoilNode
                {
                    Line = ilToken.Line,
                    Column = ilToken.Column,
                    Type = ilToken.Type switch
                    {
                        TokenType.SET => CoilType.Set,
                        TokenType.RST => CoilType.Reset,
                        _ => CoilType.Normal
                    }
                };

                SkipWhitespaceAndComments();

                if (GetTokenType(Cur) == TokenType.Identifier ||
                    GetTokenType(Cur) == TokenType.ValueTrue ||
                    GetTokenType(Cur) == TokenType.ValueFalse)
                {
                    coilNode.Variable = Advance().Value;
                }

                return coilNode;
            }

            // 函数块
            if (IsFunctionBlockToken(GetTokenType(Cur)))
            {
                var fbToken = Advance();
                var fbNode = new FunctionBlockNode
                {
                    Line = fbToken.Line,
                    Column = fbToken.Column,
                    Name = fbToken.Value.Replace("--", "").Replace("[", "").Replace("]", "")
                };
                SkipWhitespaceAndComments();
                
                // 解析实例名称 (可选)
                // 格式: --[TON]-- InstanceName(IN := ..., PT := ...)
                // 或: --[TON]-- InstanceName
                if (GetTokenType(Cur) == TokenType.Identifier)
                {
                    string instanceName = Cur.Value;
                    Advance();
                    // 使用实例名称作为函数块的完整名称
                    fbNode.Name = instanceName;
                    SkipWhitespaceAndComments();
                }
                
                // 解析参数
                if (Match(TokenType.LeftParenthesis))
                {
                    SkipWhitespaceAndComments();
                    bool firstParam = true;

                    while (!Match(TokenType.RightParenthesis) && GetTokenType(Cur) != TokenType.EOF)
                    {
                        // 参数名
                        var paramNameToken = Expect(TokenType.Identifier, "期望参数名称");
                        string paramName = paramNameToken.Value;

                        SkipWhitespaceAndComments();

                        // 如果第一个参数后面没有 := 则视为实例名 (如 TON(Timer1,PT:=1000))
                        if (firstParam && (GetTokenType(Cur) == TokenType.Comma || GetTokenType(Cur) == TokenType.RightParenthesis))
                        {
                            fbNode.Name = paramName;
                            firstParam = false;
                            if (GetTokenType(Cur) == TokenType.Comma)
                            {
                                Advance();
                                SkipWhitespaceAndComments();
                            }
                            continue;
                        }
                        firstParam = false;

                        Expect(TokenType.Assignment, "期望 :=");
                        SkipWhitespaceAndComments();

                        // 参数值
                        var paramValue = ParseExpression();
                        fbNode.Parameters[paramName] = paramValue;

                        SkipWhitespaceAndComments();

                        // 逗号分隔
                        if (!Match(TokenType.Comma))
                            break;

                        SkipWhitespaceAndComments();
                    }
                    Match(TokenType.RightParenthesis);
                }
                else if (Match(TokenType.Comma))
                {
                    // IL格式：逗号分隔参数 TON Timer1, PT := T#5s, IN := X1
                    SkipWhitespaceAndComments();

                    while (GetTokenType(Cur) != TokenType.NewLine && GetTokenType(Cur) != TokenType.EOF &&
                           GetTokenType(Cur) != TokenType.KeywordEndProgram)
                    {
                        // 参数名
                        var paramNameToken = Expect(TokenType.Identifier, "期望参数名称");
                        string paramName = paramNameToken.Value;

                        SkipWhitespaceAndComments();
                        Expect(TokenType.Assignment, "期望 :=");
                        SkipWhitespaceAndComments();

                        // 参数值
                        var paramValue = ParseExpression();
                        fbNode.Parameters[paramName] = paramValue;

                        SkipWhitespaceAndComments();

                        // 逗号分隔
                        if (!Match(TokenType.Comma))
                            break;

                        SkipWhitespaceAndComments();
                    }
                }
                
                return fbNode;
            }
            
            // 赋值语句
            if (GetTokenType(Cur) == TokenType.Identifier && IsAssignmentStart())
            {
                var assignNode = new AssignmentNode
                {
                    Line = Cur.Line,
                    Column = Cur.Column,
                    Variable = ParseAssignableTarget()
                };
                Expect(TokenType.Assignment, "期望 :=");
                
                SkipWhitespaceAndComments();
                assignNode.Value = ParseExpression();
                
                return assignNode;
            }
            
            // asm() 内联汇编
            if (GetTokenType(Cur) == TokenType.Identifier &&
                Cur.Value.Equals("asm", StringComparison.OrdinalIgnoreCase))
            {
                var callNode = new CallNode
                {
                    FunctionName = Advance().Value,
                    Line = _tokens[_pos - 1].Line,
                    Column = _tokens[_pos - 1].Column
                };
                if (GetTokenType(Cur) == TokenType.LeftParenthesis)
                {
                    Advance();
                    SkipWhitespaceAndComments();
                    if (GetTokenType(Cur) != TokenType.RightParenthesis)
                    {
                        callNode.Arguments.Add(ParseExpression());
                        SkipWhitespaceAndComments();
                    }
                    Match(TokenType.RightParenthesis);
                }
                return callNode;
            }

            return null;
        }

        private bool IsFunctionBlockToken(TokenType type)
        {
            return type == TokenType.FunctionBlock ||
                   (type >= TokenType.FunctionTON && type <= TokenType.FunctionATAN);
        }

        private bool IsAssignmentStart()
        {
            int offset = 1;
            while (true)
            {
                while (Peek(offset).Type == TokenType.Whitespace || Peek(offset).Type == TokenType.Comment)
                    offset++;
                var token = Peek(offset);
                if (token.Type == TokenType.Assignment) return true;
                if (token.Type == TokenType.Dot)
                {
                    offset++;
                    while (Peek(offset).Type == TokenType.Whitespace || Peek(offset).Type == TokenType.Comment)
                        offset++;
                    if (Peek(offset).Type != TokenType.Identifier) return false;
                    offset++;
                    continue;
                }
                if (token.Type == TokenType.LeftBracket)
                {
                    int depth = 1;
                    offset++;
                    while (depth > 0 && Peek(offset).Type != TokenType.EOF)
                    {
                        if (Peek(offset).Type == TokenType.LeftBracket) depth++;
                        else if (Peek(offset).Type == TokenType.RightBracket) depth--;
                        offset++;
                    }
                    continue;
                }
                return false;
            }
        }

        private string ParseAssignableTarget()
        {
            var parts = new List<string> { Expect(TokenType.Identifier, "期望变量名称").Value };
            while (true)
            {
                SkipWhitespaceAndComments();
                if (Match(TokenType.Dot))
                {
                    SkipWhitespaceAndComments();
                    parts.Add(".");
                    parts.Add(Expect(TokenType.Identifier, "期望成员名称").Value);
                }
                else if (Match(TokenType.LeftBracket))
                {
                    parts.Add("[");
                    parts.Add(ExpressionToInitialString(ParseExpression()));
                    Expect(TokenType.RightBracket, "期望 ]");
                    parts.Add("]");
                }
                else
                {
                    break;
                }
            }
            return string.Join("", parts);
        }
        
        /// <summary>
        /// 解析表达式
        /// </summary>
        private ExpressionNode ParseExpression()
        {
            return ParseLogicalOr();
        }
        
        private ExpressionNode ParseLogicalOr()
        {
            var left = ParseLogicalAnd();
            SkipInlineWhitespaceAndComments();
            
            while (GetTokenType(Cur) == TokenType.Logical && Cur.Value.ToUpper() == "OR")
            {
                var op = Cur.Value;
                Advance();
                
                var right = ParseLogicalAnd();
                left = new BinaryExpressionNode
                {
                    Left = left,
                    Operator = op,
                    Right = right,
                    Line = left.Line,
                    Column = left.Column
                };
                SkipInlineWhitespaceAndComments();
            }
            
            return left;
        }
        
        private ExpressionNode ParseLogicalAnd()
        {
            var left = ParseComparison();
            SkipInlineWhitespaceAndComments();
            
            while (GetTokenType(Cur) == TokenType.Logical && Cur.Value.ToUpper() == "AND")
            {
                var op = Cur.Value;
                Advance();
                
                var right = ParseComparison();
                left = new BinaryExpressionNode
                {
                    Left = left,
                    Operator = op,
                    Right = right,
                    Line = left.Line,
                    Column = left.Column
                };
                SkipInlineWhitespaceAndComments();
            }
            
            return left;
        }
        
        private ExpressionNode ParseComparison()
        {
            var left = ParseAdditive();
            SkipInlineWhitespaceAndComments();
            
            while (GetTokenType(Cur) == TokenType.Comparison)
            {
                var op = Cur.Value;
                Advance();
                
                var right = ParseAdditive();
                left = new BinaryExpressionNode
                {
                    Left = left,
                    Operator = op,
                    Right = right,
                    Line = left.Line,
                    Column = left.Column
                };
                SkipInlineWhitespaceAndComments();
            }
            
            return left;
        }
        
        private ExpressionNode ParseAdditive()
        {
            var left = ParseMultiplicative();
            SkipInlineWhitespaceAndComments();
            
            while (GetTokenType(Cur) == TokenType.Arithmetic && 
                   (Cur.Value == "+" || Cur.Value == "-"))
            {
                var op = Cur.Value;
                Advance();
                
                var right = ParseMultiplicative();
                left = new BinaryExpressionNode
                {
                    Left = left,
                    Operator = op,
                    Right = right,
                    Line = left.Line,
                    Column = left.Column
                };
                SkipInlineWhitespaceAndComments();
            }
            
            return left;
        }
        
        private ExpressionNode ParseMultiplicative()
        {
            var left = ParseUnary();
            SkipInlineWhitespaceAndComments();
            
            while (GetTokenType(Cur) == TokenType.Arithmetic && 
                   (Cur.Value == "*" || Cur.Value == "/" || Cur.Value.ToUpper() == "MOD"))
            {
                var op = Cur.Value;
                Advance();
                
                var right = ParseUnary();
                left = new BinaryExpressionNode
                {
                    Left = left,
                    Operator = op,
                    Right = right,
                    Line = left.Line,
                    Column = left.Column
                };
                SkipInlineWhitespaceAndComments();
            }
            
            return left;
        }
        
        private ExpressionNode ParseUnary()
        {
            if (GetTokenType(Cur) == TokenType.Logical && Cur.Value.ToUpper() == "NOT")
            {
                var op = Cur.Value;
                Advance();
                
                var operand = ParseUnary();
                return new UnaryExpressionNode
                {
                    Operator = op,
                    Operand = operand,
                    Line = operand.Line,
                    Column = operand.Column
                };
            }
            
            if (GetTokenType(Cur) == TokenType.Arithmetic && Cur.Value == "-")
            {
                var op = Cur.Value;
                Advance();
                
                var operand = ParseUnary();
                return new UnaryExpressionNode
                {
                    Operator = op,
                    Operand = operand,
                    Line = operand.Line,
                    Column = operand.Column
                };
            }
            
            return ParsePrimary();
        }
        
        private ExpressionNode ParsePrimary()
        {
            SkipWhitespaceAndComments();
            
            // 字面量
            if (Match(TokenType.Number, TokenType.HexNumber, TokenType.BinaryNumber))
            {
                var tok = _tokens[_pos - 1];
                object value = tok.Value;
                // 十进制数字: 含小数点 → float/double, 纯整数 → int (十六进制/二进制保持字符串)
                if (tok.Type == TokenType.Number)
                {
                    string s = (string)tok.Value;
                    if (s.Contains('.'))
                    {
                        if (float.TryParse(s, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out float f))
                            value = f;
                        else if (double.TryParse(s, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out double d))
                            value = d;
                    }
                    else if (int.TryParse(s, out int i))
                        value = i;
                }
                return new LiteralNode
                {
                    Value = value,
                    Line = tok.Line,
                    Column = tok.Column
                };
            }
            
            if (Match(TokenType.ValueTrue, TokenType.ValueFalse))
            {
                return new LiteralNode
                {
                    Value = _tokens[_pos - 1].Value.ToUpper() == "TRUE",
                    Line = _tokens[_pos - 1].Line,
                    Column = _tokens[_pos - 1].Column
                };
            }
            
            if (Match(TokenType.String))
            {
                return new LiteralNode
                {
                    Value = _tokens[_pos - 1].Value,
                    Line = _tokens[_pos - 1].Line,
                    Column = _tokens[_pos - 1].Column
                };
            }
            
            // 标识符/预定义函数
            if (GetTokenType(Cur) == TokenType.Identifier || IsFunctionBlockToken(GetTokenType(Cur)))
            {
                var idToken = Advance();
                var identifier = idToken.Value;
                if (idToken.Type != TokenType.Identifier)
                    identifier = identifier.Replace("--", "").Replace("[", "").Replace("]", "");
                ExpressionNode expr;
                
                // 检查是否为函数调用
                if (GetTokenType(Cur) == TokenType.LeftParenthesis)
                {
                    Advance(); // (
                    
                    var callNode = new CallNode
                    {
                        FunctionName = identifier,
                        Line = Cur.Line,
                        Column = Cur.Column
                    };
                    
                    SkipWhitespaceAndComments();
                    
                    // 解析参数
                    while (!Match(TokenType.RightParenthesis) && GetTokenType(Cur) != TokenType.EOF)
                    {
                        callNode.Arguments.Add(ParseExpression());
                        
                        SkipWhitespaceAndComments();
                        
                        if (!Match(TokenType.Comma))
                            break;
                        
                        SkipWhitespaceAndComments();
                    }
                    Match(TokenType.RightParenthesis);
                    
                    expr = callNode;
                }
                else
                {
                    expr = new IdentifierNode
                    {
                        Name = identifier,
                        Line = idToken.Line,
                        Column = idToken.Column
                    };
                }

                while (true)
                {
                    if (Match(TokenType.LeftBracket))
                    {
                        var index = ParseExpression();
                        Expect(TokenType.RightBracket, "期望 ]");
                        expr = new ArrayAccessNode { Target = expr, Index = index, Line = expr.Line, Column = expr.Column };
                    }
                    else if (Match(TokenType.Dot))
                    {
                        var member = Expect(TokenType.Identifier, "期望成员名称").Value;
                        expr = new MemberAccessNode { Target = expr, Member = member, Line = expr.Line, Column = expr.Column };
                    }
                    else
                    {
                        break;
                    }
                }

                return expr;
            }
            
            // 括号表达式
            if (Match(TokenType.LeftParenthesis))
            {
                var expr = ParseExpression();
                Expect(TokenType.RightParenthesis, "期望 )");
                return expr;
            }
            
            throw Error(VMLPlugins.Strings.SyntaxErrorAt(Cur.Line, Cur.Column, VMLPlugins.Strings.ExpectedToken("表达式", GetTokenType(Cur).ToString())));
        }

        private string ExpressionToInitialString(ExpressionNode expr)
        {
            switch (expr)
            {
                case LiteralNode lit:
                    return lit.Value is bool b ? (b ? "TRUE" : "FALSE") : lit.Value?.ToString() ?? "0";
                case IdentifierNode id:
                    return id.Name;
                case ArrayAccessNode arr:
                    return $"{ExpressionToInitialString(arr.Target)}[{ExpressionToInitialString(arr.Index)}]";
                case MemberAccessNode mem:
                    return $"{ExpressionToInitialString(mem.Target)}.{mem.Member}";
                case UnaryExpressionNode unary:
                    return unary.Operator + ExpressionToInitialString(unary.Operand);
                case BinaryExpressionNode binary:
                    return $"{ExpressionToInitialString(binary.Left)}{binary.Operator}{ExpressionToInitialString(binary.Right)}";
                case CallNode call:
                    return $"{call.FunctionName}({string.Join(",", call.Arguments.ConvertAll(ExpressionToInitialString))})";
                default:
                    return "0";
            }
        }

        // ====== ST (Structured Text) 语句解析 ======

        /// <summary>
        /// 解析 ST IF-THEN-ELSE 语句
        /// </summary>
        private StIfNode ParseStIf()
        {
            var node = new StIfNode { Line = Cur.Line, Column = Cur.Column };
            Expect(TokenType.KeywordIf, "期望 IF");
            SkipWhitespaceAndComments();
            node.Condition = ParseExpression();
            SkipWhitespaceAndComments();
            Expect(TokenType.KeywordThen, "期望 THEN");
            SkipWhitespaceAndComments();
            // 解析 THEN 体直到 ELSE/ELSIF/END_IF
            while (GetTokenType(Cur) != TokenType.KeywordElse &&
                   GetTokenType(Cur) != TokenType.KeywordElsif &&
                   GetTokenType(Cur) != TokenType.KeywordEndIf &&
                   GetTokenType(Cur) != TokenType.EOF)
            {
                node.ThenBody.Add(ParseStStatement());
                SkipWhitespaceAndComments();
            }
            // 解析可选的 ELSE 分支
            if (Match(TokenType.KeywordElse))
            {
                SkipWhitespaceAndComments();
                while (GetTokenType(Cur) != TokenType.KeywordEndIf && GetTokenType(Cur) != TokenType.EOF)
                {
                    node.ElseBody.Add(ParseStStatement());
                    SkipWhitespaceAndComments();
                }
            }
            Expect(TokenType.KeywordEndIf, "期望 END_IF");
            Match(TokenType.Semicolon);
            return node;
        }

        /// <summary>
        /// 解析 ST FOR 循环语句
        /// </summary>
        private StForNode ParseStFor()
        {
            var node = new StForNode { Line = Cur.Line, Column = Cur.Column };
            Expect(TokenType.KeywordFor, "期望 FOR");
            SkipWhitespaceAndComments();
            node.VarName = Expect(TokenType.Identifier, "期望循环变量名").Value;
            SkipWhitespaceAndComments();
            Expect(TokenType.Assignment, "期望 :=");
            SkipWhitespaceAndComments();
            node.Start = ParseExpression();
            SkipWhitespaceAndComments();
            Expect(TokenType.KeywordTo, "期望 TO");
            SkipWhitespaceAndComments();
            node.End = ParseExpression();
            SkipWhitespaceAndComments();
            // 可选的 BY 子句
            if (Match(TokenType.KeywordBy))
            {
                SkipWhitespaceAndComments();
                ParseExpression(); // 暂存—MCU模式下固定步长1
            }
            SkipWhitespaceAndComments();
            Expect(TokenType.KeywordDo, "期望 DO");
            SkipWhitespaceAndComments();
            while (GetTokenType(Cur) != TokenType.KeywordEndFor && GetTokenType(Cur) != TokenType.EOF)
            {
                node.Body.Add(ParseStStatement());
                SkipWhitespaceAndComments();
            }
            Expect(TokenType.KeywordEndFor, "期望 END_FOR");
            Match(TokenType.Semicolon);
            return node;
        }

        /// <summary>
        /// 解析 ST WHILE 循环语句
        /// </summary>
        private StWhileNode ParseStWhile()
        {
            var node = new StWhileNode { Line = Cur.Line, Column = Cur.Column };
            Expect(TokenType.KeywordWhile, "期望 WHILE");
            SkipWhitespaceAndComments();
            node.Condition = ParseExpression();
            SkipWhitespaceAndComments();
            Expect(TokenType.KeywordDo, "期望 DO");
            SkipWhitespaceAndComments();
            while (GetTokenType(Cur) != TokenType.KeywordEndWhile && GetTokenType(Cur) != TokenType.EOF)
            {
                node.Body.Add(ParseStStatement());
                SkipWhitespaceAndComments();
            }
            Expect(TokenType.KeywordEndWhile, "期望 END_WHILE");
            Match(TokenType.Semicolon);
            return node;
        }

        /// <summary>
        /// 判断当前 token 是否为裸打印语句关键字 (PRINT_INT/PRINT_FLOAT/PRINT_STR/PRINT_CHAR)
        /// </summary>
        private bool IsPrintStatement()
        {
            if (GetTokenType(Cur) != TokenType.Identifier) return false;
            return Cur.Value.ToUpperInvariant() is "PRINT_INT" or "PRINT_FLOAT" or "PRINT_STR" or "PRINT_CHAR";
        }

        /// <summary>
        /// 解析裸打印语句: PRINT_FLOAT 3.14; / PRINT_INT 42; / PRINT_STR "hi";
        /// 生成 CallNode, 由 CodeGenerator 内联为 SYSCALL 输出
        /// </summary>
        private ASTNode ParsePrintStatement()
        {
            string name = Advance().Value; // PRINT_INT / PRINT_FLOAT / ...
            SkipWhitespaceAndComments();
            var call = new CallNode { FunctionName = name, Line = Cur.Line, Column = Cur.Column };

            while (GetTokenType(Cur) != TokenType.Semicolon && GetTokenType(Cur) != TokenType.EOF &&
                   GetTokenType(Cur) != TokenType.NewLine)
            {
                call.Arguments.Add(ParseExpression());
                SkipWhitespaceAndComments();
                if (!Match(TokenType.Comma)) break;
                SkipWhitespaceAndComments();
            }
            Match(TokenType.Semicolon);
            return call;
        }

        /// <summary>
        /// 解析单个 ST 语句 (赋值 or 嵌套 IF/WHILE/FOR)
        /// </summary>
        private ASTNode ParseStStatement()
        {
            SkipWhitespaceAndComments();
            if (GetTokenType(Cur) == TokenType.KeywordIf)
                return ParseStIf();
            if (GetTokenType(Cur) == TokenType.KeywordWhile)
                return ParseStWhile();
            if (GetTokenType(Cur) == TokenType.KeywordFor)
                return ParseStFor();
            if (GetTokenType(Cur) == TokenType.Identifier)
            {
                var name = Advance().Value;
                SkipWhitespaceAndComments();
                if (Match(TokenType.Assignment))
                {
                    SkipWhitespaceAndComments();
                    var expr = ParseExpression();
                    Match(TokenType.Semicolon);
                    return new AssignmentNode { Variable = name, Value = expr, Line = Cur.Line, Column = Cur.Column };
                }
            }
            // 跳过无法识别的语句
            while (GetTokenType(Cur) != TokenType.Semicolon && GetTokenType(Cur) != TokenType.EOF)
                Advance();
            Match(TokenType.Semicolon);
            return new LiteralNode { Value = 0, Line = Cur.Line, Column = Cur.Column };
        }
    }
}
