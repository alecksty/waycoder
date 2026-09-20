using CompilerBase;

namespace ForthCompiler
{
    /// <summary>
    /// Forth语言语法分析器
    /// </summary>
    public class Parser : ParserBase<Token, TokenType>
    {
        private HashSet<string> definedVariables = new HashSet<string>();
        private HashSet<string> definedConstants = new HashSet<string>();
        private HashSet<string> definedWords = new HashSet<string>();
        private string? _currentWordName = null; // 跟踪当前正在解析的词名（用于RECURSE）

        protected override TokenType GetTokenType(Token token) => token.Type;

        public Parser(List<Token> tokens) : base(tokens) { }

        protected override Token Expect(TokenType type, string message)
        {
            if (Check(type)) return Advance();
            throw Error(message);
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


        private void SkipNewlines()
        {
            while (GetTokenType(Cur) == TokenType.NEWLINE)
            {
                Advance();
            }
        }

        /// <summary>
        /// 解析程序
        /// </summary>
        public Program ParseProgram()
        {
            var program = new Program();

            SkipNewlines();

            while (GetTokenType(Cur) != TokenType.EOF)
            {
                SkipNewlines();
                if (GetTokenType(Cur) == TokenType.EOF)
                    break;

                try
                {
                    var node = ParseStatement();
                    if (node != null)
                    {
                        program.Statements.Add(node);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"解析错误: {ex.Message}");
                    break;
                }
            }

            return program;
        }

        private ASTNode ParseStatement()
        {
            var token = Cur;
            
            // 检查是否是 <value> CONSTANT <name> 模式
            if ((token.Type == TokenType.NUMBER || token.Type == TokenType.CHARACTER || token.Type == TokenType.STRING || token.Type == TokenType.DOTSTRING) && Peek(1).Type == TokenType.CONSTANT)
            {
                return ParseForthConstantDefinition();
            }
            
            switch (token.Type)
            {
                case TokenType.COLON:
                    return ParseWordDefinition();
                    
                case TokenType.VARIABLE:
                    return ParseVariableDefinition();

                case TokenType.CREATE:
                    return ParseCreateDefinition();
                    
                case TokenType.CONSTANT:
                    return ParseConstantDefinition();
                    
                // ASM_KW 已移除 — asm() 仅限 C/ObjC/C++ 语言
                // Forth 通过 Lib/shared/vmlsys.c 调用系统功能
                case TokenType.DOTSTRING:
                    return ParseDotString();

                case TokenType.STRING:
                    return ParseStringLiteral();
                    
                case TokenType.NUMBER:
                    return ParseNumber();
                    
                case TokenType.CHARACTER:
                    return ParseCharacter();
                    
                case TokenType.IDENTIFIER:
                    var identifierToken = Cur;
                    if (definedVariables.Contains(identifierToken.Value))
                    {
                        return ParseVariableAccess();
                    }
                    else if (definedConstants.Contains(identifierToken.Value))
                    {
                        return ParseConstantAccess();
                    }
                    else
                    {
                        return ParseWordCall();
                    }
                    
                case TokenType.CASE:
                    return ParseCaseStatement();

                case TokenType.IF:
                    return ParseIfStatement();

                case TokenType.BEGIN:
                    return ParseLoopStatement();
                    
                case TokenType.DO:
                    return ParseDoLoop();

                case TokenType.RECURSE:
                    Advance();
                    if (_currentWordName == null)
                        throw Error("RECURSE 只能在字定义内部使用。");
                    return new WordCall
                    {
                        Name = _currentWordName,
                        IsRecursive = true,
                        Line = token.Line,
                        Column = token.Column
                    };

                case TokenType.LEAVE:
                    Advance();
                    return new LoopStatement { LoopType = TokenType.LEAVE, Line = token.Line, Column = token.Column };

                case TokenType.EXIT:
                    Advance();
                    return new ExitStatement { Line = token.Line, Column = token.Column };

                case TokenType.CATCH:
                case TokenType.THROW:
                case TokenType.ENDCATCH:
                    Advance();
                    return new ExceptionOperation { Operation = token.Type, Line = token.Line, Column = token.Column };
                    
                default:
                    // 处理各种操作符
                    return ParseOperation();
            }
        }

        private WordDefinition ParseWordDefinition()
        {
            var wordDef = new WordDefinition
            {
                Line = Cur.Line,
                Column = Cur.Column
            };

            Expect(TokenType.COLON);
            
            // 解析词名
            var nameToken = Cur;
            if (nameToken.Type != TokenType.IDENTIFIER)
            {
                throw Error(VMLPlugins.Strings.ExpectedIdentifier("词名(word name)"));
            }
            wordDef.Name = nameToken.Value;
            definedWords.Add(nameToken.Value);
            _currentWordName = nameToken.Value;
            Advance();
            
            // 可选：解析栈效应注释 ( n -- n )
            if (GetTokenType(Cur) == TokenType.PAREN_COMMENT)
            {
                // 简单跳过栈效应注释
                Advance();
            }
            
            // 解析词体
            SkipNewlines();
            while (GetTokenType(Cur) != TokenType.SEMICOLON && GetTokenType(Cur) != TokenType.EOF)
            {
                var node = ParseStatement();
                if (node != null)
                {
                    wordDef.Body.Add(node);
                }
                SkipNewlines();
            }
            
            Expect(TokenType.SEMICOLON);

            _currentWordName = null;
            return wordDef;
        }

        private VariableDefinition ParseVariableDefinition()
        {
            var varDef = new VariableDefinition
            {
                Line = Cur.Line,
                Column = Cur.Column
            };

            Expect(TokenType.VARIABLE);
            
            var nameToken = Cur;
            if (nameToken.Type != TokenType.IDENTIFIER)
            {
                throw Error(VMLPlugins.Strings.ExpectedIdentifier("变量名"));
            }
            varDef.Name = nameToken.Value;
            definedVariables.Add(nameToken.Value);
            Advance();

            return varDef;
        }

        private CreateDefinition ParseCreateDefinition()
        {
            var createDef = new CreateDefinition
            {
                Line = Cur.Line,
                Column = Cur.Column,
                AllotSize = 4
            };

            Expect(TokenType.CREATE);

            var nameToken = Cur;
            if (nameToken.Type != TokenType.IDENTIFIER)
            {
                throw Error(VMLPlugins.Strings.ExpectedIdentifier("CREATE名称"));
            }

            createDef.Name = nameToken.Value;
            definedVariables.Add(nameToken.Value);
            Advance();

            if (GetTokenType(Cur) == TokenType.NUMBER && Peek(1).Type == TokenType.ALLOT)
            {
                var sizeToken = Advance();
                if (int.TryParse(sizeToken.Value, out int size) && size > 0)
                {
                    createDef.AllotSize = size;
                }
                Advance(); // ALLOT
            }

            return createDef;
        }

        private ConstantDefinition ParseConstantDefinition()
        {
            var constDef = new ConstantDefinition
            {
                Line = Cur.Line,
                Column = Cur.Column
            };

            Expect(TokenType.CONSTANT);
            
            var nameToken = Cur;
            if (nameToken.Type != TokenType.IDENTIFIER)
            {
                throw Error(VMLPlugins.Strings.ExpectedIdentifier("常量名"));
            }
            constDef.Name = nameToken.Value;
            Advance();
            
            // 常量值
            if (GetTokenType(Cur) == TokenType.NUMBER)
            {
                constDef.Value = ParseNumber();
            }
            else if (GetTokenType(Cur) == TokenType.CHARACTER)
            {
                constDef.Value = ParseCharacter();
            }
            else
            {
                throw Error(VMLPlugins.Strings.ExpectedToken("数字或字符", GetTokenType(Cur).ToString()));
            }
            
            definedConstants.Add(constDef.Name);
            return constDef;
        }

        private ConstantDefinition ParseForthConstantDefinition()
        {
            var constDef = new ConstantDefinition
            {
                Line = Cur.Line,
                Column = Cur.Column
            };

            // 解析常量值
            if (GetTokenType(Cur) == TokenType.NUMBER)
            {
                constDef.Value = ParseNumber();
            }
            else if (GetTokenType(Cur) == TokenType.CHARACTER)
            {
                constDef.Value = ParseCharacter();
            }
            else if (GetTokenType(Cur) == TokenType.DOTSTRING)
            {
                constDef.Value = ParseDotString();
            }
            else if (GetTokenType(Cur) == TokenType.STRING)
            {
                constDef.Value = ParseStringLiteral();
            }
            else
            {
                throw Error(VMLPlugins.Strings.ExpectedToken("数字、字符或字符串", GetTokenType(Cur).ToString()));
            }
            
            // 期望 CONSTANT 关键字
            Expect(TokenType.CONSTANT);
            
            // 解析常量名
            var nameToken = Cur;
            if (nameToken.Type != TokenType.IDENTIFIER)
            {
                throw Error(VMLPlugins.Strings.ExpectedIdentifier("常量名"));
            }
            constDef.Name = nameToken.Value;
            definedConstants.Add(constDef.Name);
            Advance();

            return constDef;
        }

        private Comment ParseComment()
        {
            var comment = new Comment
            {
                Line = Cur.Line,
                Column = Cur.Column
            };

            var token = Advance();
            comment.Text = token.Value;
            
            // 判断注释类型
            if (token.Type == TokenType.PAREN_COMMENT)
            {
                comment.IsParenComment = true;
            }
            else if (token.Type == TokenType.BACKSLASH_COMMENT)
            {
                comment.IsBackslashComment = true;
            }
            
            return comment;
        }

        private IOOperation ParseDotString()
        {
            var ioOp = new IOOperation
            {
                Line = Cur.Line,
                Column = Cur.Column,
                Operation = TokenType.DOTSTRING
            };

            var token = Advance();
            ioOp.Argument = new StringLiteral
            {
                Value = token.Value,
                Line = token.Line,
                Column = token.Column
            };
            
            return ioOp;
        }

        private StringLiteral ParseStringLiteral()
        {
            var token = Advance();
            return new StringLiteral
            {
                Value = token.Value,
                Line = token.Line,
                Column = token.Column
            };
        }

        private NumberLiteral ParseNumber()
        {
            var token = Advance();
            var numLit = new NumberLiteral
            {
                Value = token.Value,
                Line = token.Line,
                Column = token.Column
            };
            
            // 判断数字类型
            if (token.Value.Contains("."))
            {
                numLit.IsFloat = true;
            }
            else if (token.Value.StartsWith("$") || token.Value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || (token.Value.Length > 0 && 
                     (token.Value[0] == '-' && token.Value.Length > 2 && token.Value.Substring(1).StartsWith("0x", StringComparison.OrdinalIgnoreCase))))
            {
                numLit.IsHex = true;
            }
            else if (token.Value.StartsWith("%") || token.Value.StartsWith("0b", StringComparison.OrdinalIgnoreCase) || (token.Value.Length > 0 && 
                     (token.Value[0] == '-' && token.Value.Length > 2 && token.Value.Substring(1).StartsWith("0b", StringComparison.OrdinalIgnoreCase))))
            {
                numLit.IsBinary = true;
            }
            
            return numLit;
        }

        private CharLiteral ParseCharacter()
        {
            var token = Advance();
            var charLit = new CharLiteral
            {
                Line = token.Line,
                Column = token.Column
            };
            
            if (!string.IsNullOrEmpty(token.Value) && token.Value.Length > 0)
            {
                charLit.Value = token.Value[0];
            }
            
            return charLit;
        }

        private WordCall ParseWordCall()
        {
            var token = Advance();
            bool isRecursive = _currentWordName != null &&
                               string.Equals(token.Value, _currentWordName, StringComparison.OrdinalIgnoreCase);
            return new WordCall
            {
                Name = token.Value,
                IsRecursive = isRecursive,
                Line = token.Line,
                Column = token.Column
            };
        }
        
        private ConstantAccess ParseConstantAccess()
        {
            return new ConstantAccess { Name = Advance().Value, Line = Cur.Line, Column = Cur.Column };
        }

        private VariableAccess ParseVariableAccess()
        {
            var token = Advance();
            return new VariableAccess
            {
                Name = token.Value,
                Line = token.Line,
                Column = token.Column,
                IsFetch = true
            };
        }

        private IfStatement ParseIfStatement()
        {
            var ifStmt = new IfStatement
            {
                Line = Cur.Line,
                Column = Cur.Column
            };

            Expect(TokenType.IF);
            
            // 解析THEN分支
            SkipNewlines();
            while (GetTokenType(Cur) != TokenType.ELSE && GetTokenType(Cur) != TokenType.THEN && GetTokenType(Cur) != TokenType.EOF)
            {
                var node = ParseStatement();
                if (node != null)
                {
                    ifStmt.ThenBranch.Add(node);
                }
                SkipNewlines();
            }
            
            // 解析ELSE分支（可选）
            if (Match(TokenType.ELSE))
            {
                SkipNewlines();
                while (GetTokenType(Cur) != TokenType.THEN && GetTokenType(Cur) != TokenType.EOF)
                {
                    var node = ParseStatement();
                    if (node != null)
                    {
                        ifStmt.ElseBranch.Add(node);
                    }
                    SkipNewlines();
                }
            }
            
            Expect(TokenType.THEN);

            return ifStmt;
        }

        private CaseStatement ParseCaseStatement()
        {
            var caseStmt = new CaseStatement
            {
                Line = Cur.Line,
                Column = Cur.Column
            };

            Expect(TokenType.CASE);
            SkipNewlines();

            var pendingValueExpr = new List<ASTNode>();

            while (GetTokenType(Cur) != TokenType.ENDCASE && GetTokenType(Cur) != TokenType.EOF)
            {
                if (Match(TokenType.OF))
                {
                    // pending statements are the value expression for this branch
                    var branch = new CaseBranch();
                    branch.ValueExpr.AddRange(pendingValueExpr);
                    pendingValueExpr.Clear();

                    SkipNewlines();
                    while (GetTokenType(Cur) != TokenType.ENDOF
                        && GetTokenType(Cur) != TokenType.ENDCASE && GetTokenType(Cur) != TokenType.EOF)
                    {
                        var stmt = ParseStatement();
                        if (stmt != null)
                            branch.Body.Add(stmt);
                        SkipNewlines();
                    }
                    caseStmt.Branches.Add(branch);

                    if (Match(TokenType.ENDOF))
                        SkipNewlines();
                }
                else
                {
                    var stmt = ParseStatement();
                    if (stmt != null)
                    {
                        if (caseStmt.Branches.Count > 0)
                        {
                            // After at least one complete OF...ENDOF, new statements are default body
                            caseStmt.DefaultBody.Add(stmt);
                        }
                        else
                        {
                            // Before first OF, statements are the value expression
                            pendingValueExpr.Add(stmt);
                        }
                    }
                    SkipNewlines();
                }
            }

            // Any remaining pending value expr without OF → default body
            if (pendingValueExpr.Count > 0)
            {
                caseStmt.DefaultBody.AddRange(pendingValueExpr);
            }

            Expect(TokenType.ENDCASE);
            return caseStmt;
        }

        private LoopStatement ParseLoopStatement()
        {
            var loopStmt = new LoopStatement
            {
                Line = Cur.Line,
                Column = Cur.Column
            };

            Expect(TokenType.BEGIN);
            
            // 解析循环体
            SkipNewlines();
            while (GetTokenType(Cur) != TokenType.UNTIL && GetTokenType(Cur) != TokenType.WHILE && 
                   GetTokenType(Cur) != TokenType.EOF && !(GetTokenType(Cur) == TokenType.IDENTIFIER && Cur.Value?.ToUpper() == "AGAIN"))
            {
                var node = ParseStatement();
                if (node != null)
                {
                    loopStmt.Body.Add(node);
                }
                SkipNewlines();
            }
            
            // 判断循环类型
            if (Match(TokenType.UNTIL))
            {
                loopStmt.LoopType = TokenType.UNTIL;
                // 解析UNTIL条件（遇到;时停止，因为它是词定义的结束符）
                while (GetTokenType(Cur) != TokenType.EOF && GetTokenType(Cur) != TokenType.NEWLINE && GetTokenType(Cur) != TokenType.SEMICOLON)
                {
                    var node = ParseStatement();
                    if (node != null)
                    {
                        loopStmt.Condition.Add(node);
                    }
                }
            }
            else if (Match(TokenType.WHILE))
            {
                loopStmt.LoopType = TokenType.WHILE;
                // 解析WHILE条件
                while (GetTokenType(Cur) != TokenType.REPEAT && GetTokenType(Cur) != TokenType.EOF)
                {
                    var node = ParseStatement();
                    if (node != null)
                    {
                        loopStmt.Condition.Add(node);
                    }
                    SkipNewlines();
                }
                
                Expect(TokenType.REPEAT);
            }
            else if (GetTokenType(Cur) == TokenType.IDENTIFIER && Cur.Value?.ToUpper() == "AGAIN")
            {
                Advance();
                loopStmt.LoopType = TokenType.AGAIN;
            }
            
            return loopStmt;
        }

        private LoopStatement ParseDoLoop()
        {
            var loopStmt = new LoopStatement
            {
                Line = Cur.Line,
                Column = Cur.Column,
                LoopType = TokenType.DO
            };

            Expect(TokenType.DO);
            
            // 解析循环体
            SkipNewlines();
            while (GetTokenType(Cur) != TokenType.LOOP && GetTokenType(Cur) != TokenType.PLUSLOOP && GetTokenType(Cur) != TokenType.EOF)
            {
                var node = ParseStatement();
                if (node != null)
                {
                    loopStmt.Body.Add(node);
                }
                SkipNewlines();
            }
            
            if (GetTokenType(Cur) == TokenType.PLUSLOOP)
            {
                loopStmt.LoopType = TokenType.PLUSLOOP;
                Advance();
            }
            else
            {
                Expect(TokenType.LOOP);
            }

            return loopStmt;
        }

        private ASTNode ParseOperation()
        {
            var token = Cur;
            
            // 栈操作
            if (token.Type == TokenType.DUP || token.Type == TokenType.DROP || token.Type == TokenType.SWAP ||
                token.Type == TokenType.OVER || token.Type == TokenType.ROT || token.Type == TokenType.QDUP ||
                token.Type == TokenType.DUP2 || token.Type == TokenType.DROP2 || token.Type == TokenType.SWAP2 ||
                token.Type == TokenType.OVER2 || token.Type == TokenType.ROT2 || token.Type == TokenType.DEPTH ||
                token.Type == TokenType.CLEAR)
            {
                Advance();
                return new StackOperation
                {
                    Operation = token.Type,
                    Line = token.Line,
                    Column = token.Column
                };
            }
            
            // 算术运算
            if (token.Type == TokenType.PLUS || token.Type == TokenType.MINUS || token.Type == TokenType.MULTIPLY ||
                token.Type == TokenType.DIVIDE || token.Type == TokenType.MOD || token.Type == TokenType.DIVMOD ||
                token.Type == TokenType.INCREMENT || token.Type == TokenType.DECREMENT || token.Type == TokenType.DOUBLE ||
                token.Type == TokenType.HALF)
            {
                Advance();
                return new ArithmeticOperation
                {
                    Operation = token.Type,
                    Line = token.Line,
                    Column = token.Column
                };
            }
            
            // 比较运算
            if (token.Type == TokenType.EQUAL || token.Type == TokenType.NOTEQUAL || token.Type == TokenType.LESSTHAN ||
                token.Type == TokenType.GREATERTHAN || token.Type == TokenType.LESSEQUAL || token.Type == TokenType.GREATEREQUAL ||
                token.Type == TokenType.ZEROEQUAL || token.Type == TokenType.ZERONOTEQUAL || token.Type == TokenType.ZEROLESSTHAN ||
                token.Type == TokenType.ZEROGREATERTHAN)
            {
                Advance();
                return new ComparisonOperation
                {
                    Operation = token.Type,
                    Line = token.Line,
                    Column = token.Column
                };
            }
            
            // 逻辑运算
            if (token.Type == TokenType.AND || token.Type == TokenType.OR || token.Type == TokenType.XOR ||
                token.Type == TokenType.NOT || token.Type == TokenType.INVERT)
            {
                Advance();
                return new LogicalOperation
                {
                    Operation = token.Type,
                    Line = token.Line,
                    Column = token.Column
                };
            }
            
            // 内存操作
            if (token.Type == TokenType.STORE || token.Type == TokenType.FETCH || token.Type == TokenType.CSTORE ||
                token.Type == TokenType.CFETCH || token.Type == TokenType.ALLOT || token.Type == TokenType.HERE ||
                token.Type == TokenType.ALLOC || token.Type == TokenType.FREE || token.Type == TokenType.FLOAD ||
                token.Type == TokenType.FSTORE)
            {
                Advance();
                return new MemoryOperation
                {
                    Operation = token.Type,
                    Line = token.Line,
                    Column = token.Column
                };
            }
            
            // 输入输出（除了."已单独处理）
            if (token.Type == TokenType.DOT || token.Type == TokenType.DOTQUOTE || token.Type == TokenType.EMIT ||
                token.Type == TokenType.KEY || token.Type == TokenType.CR || token.Type == TokenType.SPACE ||
                token.Type == TokenType.SPACES || token.Type == TokenType.TYPE || token.Type == TokenType.COUNT ||
                token.Type == TokenType.TRAILING || token.Type == TokenType.FDOT)
            {
                Advance();
                return new IOOperation
                {
                    Operation = token.Type,
                    Line = token.Line,
                    Column = token.Column
                };
            }

            // 浮点、文件、类型和时间日期等扩展词
            if (token.Type == TokenType.FLOAT || token.Type == TokenType.SFLOAT || token.Type == TokenType.DFLOAT ||
                token.Type == TokenType.FPLUS || token.Type == TokenType.FMINUS || token.Type == TokenType.FMULTIPLY ||
                token.Type == TokenType.FDIVIDE || token.Type == TokenType.FEQUAL || token.Type == TokenType.FLESSTHAN ||
                token.Type == TokenType.FGREATERTHAN || token.Type == TokenType.FLESSEQUAL || token.Type == TokenType.FGREATEREQUAL ||
                token.Type == TokenType.FOPEN || token.Type == TokenType.FCLOSE || token.Type == TokenType.FREAD ||
                token.Type == TokenType.FWRITE || token.Type == TokenType.FSEEK || token.Type == TokenType.FTELL)
            {
                Advance();
                return new WordCall { Name = token.Value ?? token.Type.ToString(), Line = token.Line, Column = token.Column };
            }
            
            // I and J: DO/LOOP loop index words
            if (token.Type == TokenType.I || token.Type == TokenType.J)
            {
                Advance();
                return new WordCall { Name = token.Value ?? token.Type.ToString(), Line = token.Line, Column = token.Column };
            }

            // PICK和ROLL需要参数
            if (token.Type == TokenType.PICK || token.Type == TokenType.ROLL)
            {
                Advance();
                var stackIndexOp = new StackIndexOperation
                {
                    Operation = token.Type,
                    Line = token.Line,
                    Column = token.Column
                };
                
                // 解析索引
                if (GetTokenType(Cur) == TokenType.NUMBER)
                {
                    stackIndexOp.Index = ParseNumber();
                }
                
                return stackIndexOp;
            }
            
            // 其他Token
            Advance();
            return null;
        }
    }
}
