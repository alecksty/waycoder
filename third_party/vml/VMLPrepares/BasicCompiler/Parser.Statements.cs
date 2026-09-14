using CompilerBase;
using System.Collections.Generic;

namespace BasicCompiler
{
    public partial class Parser : ParserBase<Token, TokenType>
    {
        private PrintStatement ParsePrintStatement()
        {
            Token token = Advance(); // 跳过 PRINT
            PrintStatement stmt = new PrintStatement(token.Line, token.Column);

            while (!AtEnd() && Peek().Type != TokenType.EOF && Peek().Type != TokenType.COLON)
            {
                // 遇到下一行的行号则终止（行号与 PRINT 不在同一行）
                if (Peek().Type == TokenType.NUMBER && Peek().Line > token.Line)
                    break;
                // Stop if next token can't start an expression
                if (!IsExpressionStart(Peek()))
                    break;
                Expression expr = ParseExpression();
                if (expr != null)
                    stmt.Expressions.Add(expr);
                if (Peek().Type == TokenType.COMMA || Peek().Type == TokenType.SEMICOLON)
                {
                    Advance(); // 跳过逗号或分号
                }
                else
                {
                    break;
                }
            }

            return stmt;
        }

        private InputStatement ParseInputStatement()
        {
            Token token = Advance(); // 跳过 INPUT
            InputStatement stmt = new InputStatement(token.Line, token.Column);

            while (!AtEnd() && Peek().Type != TokenType.EOF && Peek().Type != TokenType.COLON)
            {
                if (Peek().Type == TokenType.IDENTIFIER)
                {
                    stmt.Variables.Add(new Identifier(Peek().Line, Peek().Column, Peek().Value));
                    Advance(); // 跳过变量名
                }
                if (Peek().Type == TokenType.COMMA)
                {
                    Advance(); // 跳过逗号
                }
                else
                {
                    break;
                }
            }

            return stmt;
        }

        private LetStatement ParseLetStatement()
        {
            Token token = Peek();
            if (token.Type == TokenType.LET)
            {
                Advance(); // 跳过 LET
                token = Peek();
            }

            if (token.Type != TokenType.IDENTIFIER)
            {
                return null;
            }

            LetStatement stmt = new LetStatement(token.Line, token.Column);
            
            // 解析变量或数组访问
            Expression variableExpr = ParsePrimary();
            stmt.Variable = variableExpr;
            
            if (Peek().Type != TokenType.EQUALS)
            {
                return null;
            }
            Advance(); // 跳过 =

            stmt.Expression = ParseExpression();
            return stmt;
        }

        private IfStatement ParseIfStatement()
        {
            Token token = Advance(); // 跳过 IF
            IfStatement stmt = new IfStatement(token.Line, token.Column);

            stmt.Condition = ParseExpression();

            if (Peek().Type != TokenType.THEN)
            {
                return null;
            }
            Advance(); // 跳过 THEN

            stmt.ThenBranch = ParseStatement();

            // Handle colon-separated multi-statement THEN branch: IF x THEN a=1: b=2
            if (stmt.ThenBranch != null && Peek().Type == TokenType.COLON)
            {
                Advance(); // skip colon
                var seq = new SequenceStatement(stmt.ThenBranch.Line, stmt.ThenBranch.Column);
                seq.Statements.Add(stmt.ThenBranch);
                while (Peek().Type != TokenType.ELSE && Peek().Type != TokenType.ELSEIF &&
                       Peek().Type != TokenType.END && !AtEnd())
                {
                    var nextStmt = ParseStatement();
                    if (nextStmt != null) seq.Statements.Add(nextStmt);
                    if (Peek().Type == TokenType.COLON) { Advance(); continue; }
                    else break;
                }
                stmt.ThenBranch = seq;
            }

            // 处理 ELSEIF 链 — 变换为 ELSE 内嵌 IF
            IfStatement currentStmt = stmt;
            while (Peek().Type == TokenType.ELSEIF)
            {
                Advance(); // skip ELSEIF
                IfStatement elseifStmt = new IfStatement(token.Line, token.Column);
                elseifStmt.Condition = ParseExpression();
                if (Peek().Type != TokenType.THEN) break;
                Advance(); // skip THEN
                elseifStmt.ThenBranch = ParseStatement();
                // Handle colon-separated multi-statement ELSEIF branch
                if (elseifStmt.ThenBranch != null && Peek().Type == TokenType.COLON)
                {
                    Advance();
                    var seq = new SequenceStatement(elseifStmt.ThenBranch.Line, elseifStmt.ThenBranch.Column);
                    seq.Statements.Add(elseifStmt.ThenBranch);
                    while (Peek().Type != TokenType.ELSE && Peek().Type != TokenType.ELSEIF &&
                           Peek().Type != TokenType.END && !AtEnd())
                    {
                        var nextStmt = ParseStatement();
                        if (nextStmt != null) seq.Statements.Add(nextStmt);
                        if (Peek().Type == TokenType.COLON) { Advance(); continue; }
                        else break;
                    }
                    elseifStmt.ThenBranch = seq;
                }
                currentStmt.ElseBranch = elseifStmt;
                currentStmt = elseifStmt;
            }

            if (Peek().Type == TokenType.ELSE)
            {
                Advance(); // 跳过 ELSE
                currentStmt.ElseBranch = ParseStatement();
                // Handle colon-separated multi-statement ELSE branch
                if (currentStmt.ElseBranch != null && Peek().Type == TokenType.COLON)
                {
                    Advance();
                    var seq = new SequenceStatement(currentStmt.ElseBranch.Line, currentStmt.ElseBranch.Column);
                    seq.Statements.Add(currentStmt.ElseBranch);
                    while (Peek().Type != TokenType.END && !AtEnd())
                    {
                        var nextStmt = ParseStatement();
                        if (nextStmt != null) seq.Statements.Add(nextStmt);
                        if (Peek().Type == TokenType.COLON) { Advance(); continue; }
                        else break;
                    }
                    currentStmt.ElseBranch = seq;
                }
            }

            // Consume END IF if present
            if (Peek().Type == TokenType.END && current + 1 < tokens.Count && tokens[current + 1].Type == TokenType.IF)
            {
                Advance(); // skip END
                Advance(); // skip IF
            }

            return stmt;
        }

        private GotoStatement ParseGotoStatement()
        {
            Token token = Advance(); // 跳过 GOTO
            GotoStatement stmt = new GotoStatement(token.Line, token.Column);

            if (Peek().Type == TokenType.NUMBER)
            {
                stmt.LineNumber = int.Parse(Peek().Value);
                Advance();
            }
            else if (Peek().Type == TokenType.IDENTIFIER)
            {
                // Will be converted to a label reference during codegen
                stmt.Label = Peek().Value.ToLower();
                stmt.IsLabel = true;
                Advance();
            }
            else
            {
                return null;
            }

            return stmt;
        }

        private GosubStatement ParseGosubStatement()
        {
            Token token = Advance(); // 跳过 GOSUB
            GosubStatement stmt = new GosubStatement(token.Line, token.Column);

            if (Peek().Type == TokenType.NUMBER)
            {
                stmt.LineNumber = int.Parse(Peek().Value);
                Advance();
            }
            else if (Peek().Type == TokenType.IDENTIFIER)
            {
                stmt.Label = Peek().Value.ToLower();
                stmt.IsLabel = true;
                Advance();
            }
            else
            {
                return null;
            }

            return stmt;
        }

        private ReturnStatement ParseReturnStatement()
        {
            Token token = Advance(); // 跳过 RETURN
            return new ReturnStatement(token.Line, token.Column);
        }

        private Statement ParseDimStatement()
        {
            Token token = Advance(); // 跳过 DIM

            DimStatement stmt = new DimStatement(token.Line, token.Column);

            // 处理 DIM SHARED
            if (Peek().Type == TokenType.SHARED)
            {
                Advance(); // skip SHARED
                stmt.IsShared = true;
            }

            // 解析变量名
            if (Peek().Type != TokenType.IDENTIFIER)
            {
                return null;
            }

            string varName = Peek().Value;
            Advance(); // 跳过变量名
            
            // 检查是否是字符串数组（以$结尾）
            bool isStringArray = false;
            if (varName.EndsWith("$"))
            {
                isStringArray = true;
                varName = varName.Substring(0, varName.Length - 1);
            }
            
            stmt.VariableName = varName;
            stmt.IsStringArray = isStringArray;
            
            // 检查是否为 DIM var AS TypeName
            if (Peek().Type == TokenType.AS)
            {
                return ParseDimAsStatement(token, varName);
            }
            
            // 如果不是左括号，则是简单变量声明 (DIM SHARED varname)
            if (Peek().Type != TokenType.LPAREN)
            {
                stmt.VariableName = varName;
                stmt.Size = 1;
                return stmt;
            }
            Advance(); // 跳过(
            
            // 解析数组维度 (支持TO语法: 1 TO 2, 支持表达式: MAXSNAKELENGTH-1)
            while (true)
            {
                // Parse lower bound (number, identifier, or expression)
                int lowerBound = 0;
                int upperBound;
                
                if (Peek().Type == TokenType.NUMBER)
                {
                    lowerBound = int.Parse(Peek().Value);
                    Advance();
                }
                else if (Peek().Type == TokenType.IDENTIFIER)
                {
                    // Identifier (could be a constant like MAXSNAKELENGTH)
                    // For now, record it as a dimension expression (store as 0 and note that it's non-numeric)
                    string identVal = Peek().Value;
                    Advance();
                    // Check if there's arithmetic after identifier
                    if (Peek().Type == TokenType.MINUS || Peek().Type == TokenType.PLUS ||
                        Peek().Type == TokenType.MULTIPLY || Peek().Type == TokenType.DIVIDE)
                    {
                        // Skip arithmetic operators and following number/identifier
                        Advance(); // skip operator
                        if (Peek().Type == TokenType.NUMBER)
                        {
                            lowerBound = int.Parse(Peek().Value);
                            Advance();
                        }
                        else if (Peek().Type == TokenType.IDENTIFIER)
                        {
                            Advance();
                        }
                    }
                    // Use 1 as placeholder size for identifier-based dimensions
                    lowerBound = 1;
                    // We'll compute actual size from the identifier value during codegen
                }
                else
                {
                    return null;
                }
                
                // Check for TO syntax: lower TO upper
                if (Peek().Type == TokenType.TO)
                {
                    Advance(); // skip TO
                    
                    if (Peek().Type == TokenType.NUMBER)
                    {
                        upperBound = int.Parse(Peek().Value);
                        Advance();
                    }
                    else if (Peek().Type == TokenType.IDENTIFIER)
                    {
                        Advance();
                        upperBound = lowerBound + 1; // placeholder
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    // No TO: the value is the upper bound, lower bound is 0
                    upperBound = lowerBound;
                    lowerBound = 0;
                }
                
                int dimSize = upperBound - lowerBound + 1;
                if (dimSize < 1) dimSize = 1;
                stmt.Dimensions.Add(dimSize);
                
                // 检查是逗号还是右括号
                if (Peek().Type == TokenType.COMMA)
                {
                    Advance(); // 跳过逗号，继续解析下一维度
                }
                else if (Peek().Type == TokenType.RPAREN)
                {
                    Advance(); // 跳过)
                    break;
                }
                else
                {
                    return null;
                }
            }
            
            // 兼容旧版单维度
            if (stmt.Dimensions.Count == 1)
            {
                stmt.Size = stmt.Dimensions[0];
            }
            else if (stmt.Dimensions.Count > 1)
            {
                stmt.Size = 1;
                foreach (int d in stmt.Dimensions)
                {
                    stmt.Size *= d;
                }
            }
            
            // 检查是否为 DIM arr(size) AS TypeName
            if (Peek().Type == TokenType.AS)
            {
                Advance(); // skip AS
                if (Peek().Type == TokenType.IDENTIFIER)
                {
                    stmt.TypeName = Peek().Value;
                    Advance();
                }
            }
            
            // Consume comma-separated DIM vars: DIM SHARED a(n), b(m), c AS Type
            while (Peek().Type == TokenType.COMMA)
            {
                Advance(); // skip comma
                if (Peek().Type == TokenType.IDENTIFIER)
                {
                    var extraName = Peek().Value;
                    Advance(); // skip name
                    // Skip array dimensions if present: ident(size) or ident(low TO high)
                    if (Peek().Type == TokenType.LPAREN)
                    {
                        Advance(); // skip (
                        int parenDepth = 1;
                        while (!AtEnd() && parenDepth > 0)
                        {
                            if (Peek().Type == TokenType.LPAREN) parenDepth++;
                            else if (Peek().Type == TokenType.RPAREN) parenDepth--;
                            if (parenDepth > 0) Advance();
                        }
                        if (Peek().Type == TokenType.RPAREN)
                            Advance(); // skip )
                    }
                    // Skip AS TypeName
                    if (Peek().Type == TokenType.AS)
                    {
                        Advance(); // skip AS
                        if (Peek().Type == TokenType.IDENTIFIER)
                            Advance(); // skip type name
                    }
                }
            }

            return stmt;
        }

        private ForStatement ParseForStatement()
        {
            Token token = Advance(); // 跳过 FOR
            ForStatement stmt = new ForStatement(token.Line, token.Column);

            if (Peek().Type != TokenType.IDENTIFIER)
            {
                return null;
            }

            stmt.Variable = new Identifier(Peek().Line, Peek().Column, Peek().Value);
            Advance(); // 跳过变量名

            if (Peek().Type != TokenType.EQUALS)
            {
                return null;
            }
            Advance(); // 跳过 =

            stmt.InitialValue = ParseExpression();

            if (Peek().Type != TokenType.TO)
            {
                return null;
            }
            Advance(); // 跳过 TO

            stmt.EndValue = ParseExpression();

            if (Peek().Type == TokenType.STEP)
            {
                Advance(); // 跳过 STEP
                stmt.StepValue = ParseExpression();
            }
            else
            {
                // 默认步长为 1
                stmt.StepValue = new NumberLiteral(token.Line, token.Column, 1);
            }

            // 解析循环体 — 仅 NEXT 终止
            while (!AtEnd() && Peek().Type != TokenType.NEXT)
            {
                while (Peek().Type == TokenType.NUMBER || Peek().Type == TokenType.COLON) Advance();
                if (Peek().Type == TokenType.NEXT) break;
                Statement bodyStmt = ParseStatement();
                if (bodyStmt != null)
                    stmt.Body.Add(bodyStmt);
            }

            if (Peek().Type == TokenType.NEXT)
                Advance(); // 跳过 NEXT

            return stmt;
        }

        private WhileStatement ParseWhileStatement()
        {
            Token token = Advance(); // 跳过 WHILE
            WhileStatement stmt = new WhileStatement(token.Line, token.Column);

            stmt.Condition = ParseExpression();

            // 解析循环体 — 仅 WEND 终止
            while (!AtEnd() && Peek().Type != TokenType.WEND)
            {
                while (Peek().Type == TokenType.NUMBER || Peek().Type == TokenType.COLON) Advance();
                // 跳过数字/冒号后可能已经到达 WEND，必须在此处再检查，否则 ParseStatement 的 default 分支会消费它
                if (Peek().Type == TokenType.WEND) break;
                Statement bodyStmt = ParseStatement();
                if (bodyStmt != null)
                    stmt.Body.Add(bodyStmt);
            }

            if (Peek().Type == TokenType.WEND)
                Advance(); // 跳过 WEND

            return stmt;
        }

        private DoLoopStatement ParseDoLoopStatement()
        {
            Token doToken = Advance(); // 跳过 DO

            var stmt = new DoLoopStatement(doToken.Line, doToken.Column);

            // Handle DO WHILE / DO UNTIL (pre-test condition)
            if (Peek().Type == TokenType.WHILE)
            {
                Advance(); // 跳过 WHILE
                stmt.Condition = ParseExpression();
                stmt.IsUntil = false;
                stmt.HasCondition = true;
                stmt.IsPreTest = true;
            }
            else if (Peek().Type == TokenType.UNTIL)
            {
                Advance(); // 跳过 UNTIL
                stmt.Condition = ParseExpression();
                stmt.IsUntil = true;
                stmt.HasCondition = true;
                stmt.IsPreTest = true;
            }

            // 解析循环体 — 仅 LOOP 终止
            while (!AtEnd() && Peek().Type != TokenType.LOOP)
            {
                while (Peek().Type == TokenType.NUMBER || Peek().Type == TokenType.COLON) Advance();
                if (Peek().Type == TokenType.LOOP) break;
                Statement bodyStmt = ParseStatement();
                if (bodyStmt != null)
                    stmt.Body.Add(bodyStmt);
            }

            if (Peek().Type == TokenType.LOOP)
            {
                Advance(); // 跳过 LOOP

                // LOOP WHILE / LOOP UNTIL (post-test condition)
                if (Peek().Type == TokenType.UNTIL)
                {
                    Advance(); // 跳过 UNTIL
                    stmt.Condition = ParseExpression();
                    stmt.IsUntil = true;
                    stmt.HasCondition = true;
                }
                else if (Peek().Type == TokenType.WHILE)
                {
                    Advance(); // 跳过 WHILE
                    stmt.Condition = ParseExpression();
                    stmt.IsUntil = false;
                    stmt.HasCondition = true;
                }
            }

            return stmt;
        }

        private EndStatement ParseEndStatement()
        {
            Token token = Advance(); // 跳过 END
            return new EndStatement(token.Line, token.Column);
        }

        private Statement ParseConstStatement()
        {
            Token token = Advance(); // 跳过 CONST
            ConstStatement stmt = new ConstStatement(token.Line, token.Column);
            List<ConstStatement> extraConsts = new List<ConstStatement>();

            if (Peek().Type != TokenType.IDENTIFIER)
                return null;

            stmt.Name = Peek().Value;
            Advance(); // 跳过变量名

            if (Peek().Type != TokenType.EQUALS)
                return null;
            Advance(); // 跳过 =

            stmt.Value = ParseExpression();
            
            // Handle comma-separated CONST declarations: CONST a=1, b=2, c=3
            while (Peek().Type == TokenType.COMMA)
            {
                Advance(); // skip comma
                if (Peek().Type != TokenType.IDENTIFIER)
                    break;
                var extraConst = new ConstStatement(token.Line, token.Column);
                extraConst.Name = Peek().Value;
                Advance(); // skip name
                if (Peek().Type != TokenType.EQUALS)
                    break;
                Advance(); // skip =
                extraConst.Value = ParseExpression();
                extraConsts.Add(extraConst);
            }
            
            // If there are extra consts, return a MultiStatement wrapping all of them
            // (The program doesn't support MultiStatement directly, so we return the first
            // and the parser's main loop will handle the rest since we've consumed the tokens)
            // Actually just return the first one — the extra consts will be parsed by the main loop
            // since ParseExpression consumed the value tokens.
            return stmt;
        }

        private Statement ParseExitStatement()
        {
            Token exitToken = Advance(); // 跳过 EXIT
            
            if (AtEnd())
            {
                throw Error($"语法错误: EXIT 后缺少 FOR/WHILE/SUB/FUNCTION，第{exitToken.Line}行");
            }
            
            Token next = Peek();
            if (next.Type == TokenType.FOR)
            {
                Advance(); // 跳过 FOR
                return new ExitLoopStatement(exitToken.Line, exitToken.Column);
            }
            else if (next.Type == TokenType.WHILE)
            {
                Advance(); // 跳过 WHILE
                return new ExitLoopStatement(exitToken.Line, exitToken.Column);
            }
            else if (next.Type == TokenType.DO)
            {
                Advance(); // 跳过 DO
                return new ExitLoopStatement(exitToken.Line, exitToken.Column);
            }
            else if (next.Type == TokenType.SUB || next.Type == TokenType.FUNCTION)
            {
                Advance(); // 跳过 SUB/FUNCTION
                return new ExitSubStatement(exitToken.Line, exitToken.Column);
            }
            else
            {
                throw Error($"语法错误: EXIT 后期望 FOR/WHILE/SUB/FUNCTION，但得到 '{next.Value}'，第{next.Line}行");
            }
        }

        private DimAsStatement ParseDimAsStatement(Token dimToken, string varName)
        {
            // Already consumed DIM and variable name, now on AS
            Advance(); // skip AS

            if (Peek().Type != TokenType.IDENTIFIER)
                return null;

            string typeName = Peek().Value;
            Advance();

            return new DimAsStatement(dimToken.Line, dimToken.Column)
            {
                VariableName = varName,
                TypeName = typeName
            };
        }

        private Statement ParseTypeDeclaration()
        {
            Token token = Advance(); // skip TYPE
            if (Peek().Type != TokenType.IDENTIFIER)
                return null;

            string typeName = Peek().Value;
            Advance();

            TypeDeclaration typeDecl = new TypeDeclaration(token.Line, token.Column)
            {
                Name = typeName
            };

            // Parse fields until END TYPE
            while (!AtEnd())
            {
                if (Peek().Type == TokenType.END)
                {
                    Advance(); // skip END
                    if (Peek().Type == TokenType.TYPE_KW)
                    {
                        Advance(); // skip TYPE
                        break;
                    }
                    // Put back if not END TYPE
                    break;
                }

                if (Peek().Type != TokenType.IDENTIFIER)
                {
                    Advance(); // skip unknown
                    continue;
                }

                string fieldName = Peek().Value;
                Advance();

                // Expect AS keyword
                if (Peek().Type != TokenType.AS)
                    continue;
                Advance(); // skip AS

                TypeField field = new TypeField(token.Line, token.Column)
                {
                    Name = fieldName
                };

                // Parse type: INTEGER, SINGLE, STRING [* n]
                if (Peek().Type == TokenType.IDENTIFIER)
                {
                    string ftype = Peek().Value.ToUpper();
                    field.FieldType = ftype;
                    Advance();

                    if (ftype == "STRING" && Peek().Type == TokenType.MULTIPLY)
                    {
                        Advance(); // skip *
                        if (Peek().Type == TokenType.NUMBER)
                        {
                            field.StringLength = int.Parse(Peek().Value);
                            Advance();
                        }
                    }
                }

                typeDecl.Fields.Add(field);
            }

            return typeDecl;
        }

        /// <summary>CLASS name ... END CLASS — FreeBasic OOP (v1.66.31+)</summary>
        private Statement ParseClassDeclaration()
        {
            Token token = Advance(); // skip CLASS
            if (Peek().Type != TokenType.IDENTIFIER)
                return new ClassDeclaration(token.Line, token.Column) { Name = "?" };
            string name = Peek().Value;
            Advance();
            var classDecl = new ClassDeclaration(token.Line, token.Column) { Name = name };

            while (!AtEnd())
            {
                // END CLASS / END CONSTRUCTOR / END METHOD
                if (Peek().Type == TokenType.END)
                {
                    Advance(); // skip END
                    if (Peek().Type == TokenType.CLASS_KW)
                    {
                        Advance(); // skip CLASS
                        break;
                    }
                    // END without CLASS at top level — break for error recovery
                    break;
                }
                // CONSTRUCTOR
                if (Peek().Type == TokenType.CONSTRUCTOR)
                {
                    Advance(); // skip CONSTRUCTOR
                    classDecl.ConstructorBody = new List<Statement>();
                    while (!AtEnd())
                    {
                        if (Peek().Type == TokenType.END)
                        {
                            Advance(); // skip END
                            if (Peek().Type == TokenType.CONSTRUCTOR)
                            {
                                Advance(); // skip CONSTRUCTOR
                                break;
                            }
                            // END CLASS without END CONSTRUCTOR — terminate
                            if (Peek().Type == TokenType.CLASS_KW)
                            {
                                Advance();
                                goto classEnd;
                            }
                            break;
                        }
                        var stmt = ParseStatement();
                        if (stmt != null) classDecl.ConstructorBody.Add(stmt);
                    }
                    continue;
                }
                // DESTRUCTOR
                if (Peek().Type == TokenType.DESTRUCTOR)
                {
                    Advance(); // skip DESTRUCTOR
                    classDecl.DestructorBody = new List<Statement>();
                    while (!AtEnd())
                    {
                        if (Peek().Type == TokenType.END)
                        {
                            Advance(); // skip END
                            if (Peek().Type == TokenType.DESTRUCTOR)
                            {
                                Advance(); // skip DESTRUCTOR
                                break;
                            }
                            if (Peek().Type == TokenType.CLASS_KW)
                            {
                                Advance();
                                goto classEnd;
                            }
                            break;
                        }
                        var stmt = ParseStatement();
                        if (stmt != null) classDecl.DestructorBody.Add(stmt);
                    }
                    continue;
                }
                // Field or Method
                if (Peek().Type == TokenType.IDENTIFIER)
                {
                    string fieldName = Peek().Value;
                    Advance();
                    if (Peek().Type == TokenType.LPAREN)
                    {
                        // Method: name(params)
                        Advance(); // skip (
                        var method = new MethodDeclaration(token.Line, token.Column) { Name = fieldName };
                        while (Peek().Type != TokenType.RPAREN && !AtEnd())
                        {
                            if (Peek().Type == TokenType.IDENTIFIER)
                                method.Parameters.Add(Peek().Value);
                            Advance();
                        }
                        if (Peek().Type == TokenType.RPAREN) Advance(); // skip )
                        // Parse method body until END METHOD or END CLASS
                        while (!AtEnd())
                        {
                            if (Peek().Type == TokenType.END)
                            {
                                Advance(); // skip END
                                if (Peek().Type == TokenType.METHOD_KW)
                                {
                                    Advance(); // skip METHOD
                                    break;
                                }
                                // END CLASS without END METHOD — terminate
                                if (Peek().Type == TokenType.CLASS_KW)
                                {
                                    Advance();
                                    goto classEnd;
                                }
                                break;
                            }
                            var stmt = ParseStatement();
                            if (stmt != null) method.Body.Add(stmt);
                        }
                        classDecl.Methods.Add(method);
                        continue;
                    }
                    if (Peek().Type == TokenType.AS)
                    {
                        Advance(); // skip AS
                        if (Peek().Type != TokenType.IDENTIFIER)
                            continue;
                        string ftype = Peek().Value;
                        Advance();
                        classDecl.Fields.Add(new TypeField(token.Line, token.Column) { Name = fieldName, FieldType = ftype });
                        continue;
                    }
                }
                Advance(); // skip unknown token
            }
        classEnd:
            return classDecl;
        }

        // ----- Turbo Basic 扩展解析方法 -----

        private LocalDeclaration ParseLocalDeclaration()
        {
            Token token = Advance(); // skip LOCAL
            LocalDeclaration stmt = new LocalDeclaration(token.Line, token.Column);
            // LOCAL var1, var2, var3 ...
            while (true)
            {
                if (Peek().Type != TokenType.IDENTIFIER) break;
                stmt.Variables.Add(new Identifier(Peek().Line, Peek().Column, Peek().Value));
                Advance();
                if (Peek().Type != TokenType.COMMA) break;
                Advance(); // skip comma
            }
            return stmt;
        }

        private StaticDeclaration ParseStaticDeclaration()
        {
            Token token = Advance(); // skip STATIC
            StaticDeclaration stmt = new StaticDeclaration(token.Line, token.Column);
            // STATIC var1, var2, var3 ...
            while (true)
            {
                if (Peek().Type != TokenType.IDENTIFIER) break;
                stmt.Variables.Add(new Identifier(Peek().Line, Peek().Column, Peek().Value));
                Advance();
                if (Peek().Type != TokenType.COMMA) break;
                Advance(); // skip comma
            }
            return stmt;
        }

        private SharedStatement ParseSharedStatement()
        {
            Token token = Advance(); // skip SHARED
            SharedStatement stmt = new SharedStatement(token.Line, token.Column);
            // SHARED var1, var2, var3 ...
            // SHARED var AS Type [, var2 AS Type ...]
            while (true)
            {
                if (Peek().Type != TokenType.IDENTIFIER) break;
                stmt.Variables.Add(new Identifier(Peek().Line, Peek().Column, Peek().Value));
                Advance();
                // 跳过 AS type
                if (Peek().Type == TokenType.AS)
                {
                    Advance(); // skip AS
                    if (Peek().Type == TokenType.IDENTIFIER)
                        Advance(); // skip type name
                }
                if (Peek().Type != TokenType.COMMA) break;
                Advance(); // skip comma
            }
            return stmt;
        }

        private CommonStatement ParseCommonStatement()
        {
            Token token = Advance(); // skip COMMON
            CommonStatement stmt = new CommonStatement(token.Line, token.Column);
            // COMMON var1, var2, var3 ...
            // COMMON /blockname/ var1, var2 (skip block name if present)
            if (Peek().Type == TokenType.DIVIDE)
            {
                Advance(); // skip opening /
                if (Peek().Type == TokenType.IDENTIFIER)
                    Advance(); // skip block name
                if (Peek().Type == TokenType.DIVIDE)
                    Advance(); // skip closing /
            }
            while (true)
            {
                if (Peek().Type != TokenType.IDENTIFIER) break;
                stmt.VariableNames.Add(Peek().Value);
                Advance();
                if (Peek().Type != TokenType.COMMA) break;
                Advance(); // skip comma
            }
            return stmt;
        }

        /// <summary>
        /// 解析 DEFSNG/DEFINT/DEFSTR letter[-letter][, letter[-letter]]...
        /// </summary>
        private DefTypeStatement ParseDefTypeStatement()
        {
            Token token = Advance(); // consume DEFSNG/DEFINT/DEFSTR
            DefTypeStatement stmt = new DefTypeStatement(token.Line, token.Column);
            stmt.TypeName = token.Value.ToUpper();

            // 解析字母范围，例如: A-Z, A-C, F, M-R
            while (true)
            {
                if (AtEnd()) break;
                // 检查是否是逗号（分隔多个范围）
                if (Peek().Type == TokenType.COMMA)
                {
                    Advance(); // skip comma
                    continue;
                }
                // 期望一个字母标识符
                if (Peek().Type != TokenType.IDENTIFIER) break;
                string letterStr = Peek().Value.ToUpper();
                if (letterStr.Length != 1 || letterStr[0] < 'A' || letterStr[0] > 'Z')
                    break; // 不是单字母，停止解析
                char startLetter = letterStr[0];
                Advance(); // consume letter

                char endLetter = startLetter; // 默认单字母
                // 检查是否有 '-' 后跟另一个字母
                if (!AtEnd() && Peek().Type == TokenType.MINUS)
                {
                    Advance(); // consume '-'
                    if (!AtEnd() && Peek().Type == TokenType.IDENTIFIER)
                    {
                        string endStr = Peek().Value.ToUpper();
                        if (endStr.Length == 1 && endStr[0] >= 'A' && endStr[0] <= 'Z')
                        {
                            endLetter = endStr[0];
                            Advance(); // consume end letter
                        }
                    }
                }

                stmt.Ranges.Add((startLetter, endLetter));

                // 如果没有逗号，结束
                if (AtEnd() || Peek().Type != TokenType.COMMA)
                    break;
            }

            return stmt;
        }

        private OptionBaseStatement ParseOptionBaseStatement()
        {
            Token token = Advance(); // skip OPTION
            OptionBaseStatement stmt = new OptionBaseStatement(token.Line, token.Column);
            // OPTION BASE n — expect IDENTIFIER "BASE" then number
            if (Peek().Type == TokenType.IDENTIFIER && Peek().Value.ToUpper() == "BASE")
            {
                Advance(); // skip BASE
                if (Peek().Type == TokenType.NUMBER)
                {
                    stmt.Base = int.Parse(Peek().Value);
                    Advance();
                }
            }
            return stmt;
        }

        private Statement ParsePrintUsingStatement()
        {
            Token token = Advance(); // skip PRINT
            Advance(); // skip USING
            PrintUsingStatement stmt = new PrintUsingStatement(token.Line, token.Column);

            // Parse format string
            stmt.Format = ParseExpression();

            // Parse values separated by semicolons
            while (Peek().Type == TokenType.SEMICOLON)
            {
                Advance(); // skip ;
                if (AtEnd() || Peek().Type == TokenType.COLON || Peek().Type == TokenType.EOF)
                    break;
                if (IsExpressionStart(Peek()))
                {
                    Expression val = ParseExpression();
                    if (val != null) stmt.Values.Add(val);
                }
            }

            return stmt;
        }
    }
}
