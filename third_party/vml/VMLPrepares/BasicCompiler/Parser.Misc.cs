using CompilerBase;
using System.Collections.Generic;

namespace BasicCompiler
{
    public partial class Parser : ParserBase<Token, TokenType>
    {
        private Expression ParseArrayAccess(Token arrayNameToken)
        {
            // 已经读取了数组名，现在应该是 LPAREN
            if (Peek().Type != TokenType.LPAREN)
            {
                // 如果不是 LPAREN，则返回普通标识符
                return new Identifier(arrayNameToken.Line, arrayNameToken.Column, arrayNameToken.Value);
            }
            Advance(); // 跳过 '('
            
            // 解析索引表达式 (支持逗号分隔的多维)
            ArrayAccessExpression arrayAccess = new ArrayAccessExpression(arrayNameToken.Line, arrayNameToken.Column);
            arrayAccess.ArrayName = arrayNameToken.Value;
            
            while (true)
            {
                Expression indexExpr = ParseExpression();
                arrayAccess.Indices.Add(indexExpr);
                
                if (Peek().Type == TokenType.COMMA)
                {
                    Advance(); // 跳过逗号，继续解析下一索引
                }
                else if (Peek().Type == TokenType.RPAREN)
                {
                    Advance(); // 跳过 ')'
                    break;
                }
                else
                {
                    // 解析失败，返回普通标识符
                    return new Identifier(arrayNameToken.Line, arrayNameToken.Column, arrayNameToken.Value);
                }
            }
            
            // 兼容旧版单索引
            if (arrayAccess.Indices.Count == 1)
            {
                arrayAccess.Index = arrayAccess.Indices[0];
            }
            
            return arrayAccess;
        }

        private Expression ParseFunctionCall(Token functionNameToken)
        {
            // 已经读取了函数名，现在应该是 LPAREN
            if (Peek().Type != TokenType.LPAREN)
            {
                return new Identifier(functionNameToken.Line, functionNameToken.Column, functionNameToken.Value);
            }
            Advance(); // 跳过 '('
            
            FunctionCallExpression call = new FunctionCallExpression(
                functionNameToken.Line, functionNameToken.Column, functionNameToken.Value);
            
            // 解析参数列表
            while (Peek().Type != TokenType.RPAREN && !AtEnd())
            {
                call.Arguments.Add(ParseExpression());
                if (Peek().Type == TokenType.COMMA)
                {
                    Advance();
                }
                else
                {
                    break;
                }
            }
            
            if (Peek().Type == TokenType.RPAREN)
            {
                Advance();
            }
            
            return call;
        }

        private Statement ParseOpenStatement()
        {
            Token token = Advance(); // 跳过 OPEN
            OpenStatement stmt = new OpenStatement(token.Line, token.Column);

            // 解析文件名
            if (Peek().Type == TokenType.STRING)
            {
                stmt.FileName = new StringLiteral(Peek().Line, Peek().Column, Peek().Value);
                Advance();
            }
            else
            {
                stmt.FileName = ParseExpression();
            }

            // 解析 FOR mode (可选)
            stmt.Mode = FileOpenMode.Input; // 默认
            if (Peek().Type == TokenType.FOR)
            {
                Advance(); // 跳过 FOR
                if (Peek().Type == TokenType.IDENTIFIER)
                {
                    string modeStr = Peek().Value.ToUpper();
                    Advance();
                    if (modeStr == "INPUT")
                        stmt.Mode = FileOpenMode.Input;
                    else if (modeStr == "OUTPUT")
                        stmt.Mode = FileOpenMode.Output;
                    else if (modeStr == "APPEND")
                        stmt.Mode = FileOpenMode.Append;
                }
            }

            // 解析 AS #n
            if (Peek().Type == TokenType.AS)
            {
                Advance(); // 跳过 AS
                if (Peek().Type == TokenType.HASH)
                {
                    Advance(); // 跳过 #
                }
                stmt.FileNumber = ParseExpression();
            }

            return stmt;
        }

        private Statement ParseCloseStatement()
        {
            Token token = Advance(); // 跳过 CLOSE
            CloseStatement stmt = new CloseStatement(token.Line, token.Column);

            // 可选的文件号
            if (Peek().Type == TokenType.HASH)
            {
                Advance(); // 跳过 #
                stmt.FileNumber = ParseExpression();
            }
            else if (Peek().Type == TokenType.IDENTIFIER || Peek().Type == TokenType.NUMBER)
            {
                stmt.FileNumber = ParseExpression();
            }

            return stmt;
        }

        private Statement ParseSelectCaseStatement()
        {
            Token selectToken = Advance(); // 跳过 SELECT
            Expect(TokenType.CASE, "期望 CASE 关键字"); // 跳过 CASE

            // 解析测试表达式
            Expression testExpr = ParseExpression();
            SelectCaseStatement stmt = new SelectCaseStatement(selectToken.Line, selectToken.Column, testExpr);

            // 跳过冒号（行内多语句分隔符）
            while (Peek().Type == TokenType.COLON) Advance();

            // 解析CASE块
            while (!AtEnd() && Peek().Type != TokenType.EOF)
            {
                // 跳过冒号分隔符
                while (Peek().Type == TokenType.COLON) Advance();

                if (Peek().Type == TokenType.CASE)
                {
                    Advance(); // 跳过 CASE
                    CaseBlock caseBlock = ParseCaseBlock();
                    stmt.CaseBlocks.Add(caseBlock);
                }
                else if (Peek().Type == TokenType.ELSE)
                {
                    Advance(); // 跳过 ELSE
                    stmt.ElseBlock = ParseStatementBlock();
                    // 跳过冒号
                    while (Peek().Type == TokenType.COLON) Advance();
                    break;
                }
                else if (Peek().Type == TokenType.END)
                {
                    // 只当 END 后跟 SELECT 时才表示 END SELECT
                    if (current + 1 < tokens.Count && tokens[current + 1].Type == TokenType.SELECT)
                        break;
                    // 其他 END (如 END IF / END SUB) 属于嵌套块，跳过并继续
                    Advance();
                }
                else
                {
                    // 跳过未知标记（如行内多余的内容）
                    if (!AtEnd()) Advance();
                }
            }

            // 跳过冒号
            while (Peek().Type == TokenType.COLON) Advance();

            // 期望 END SELECT
            if (Peek().Type == TokenType.END)
            {
                Advance(); // 跳过 END
                while (Peek().Type == TokenType.COLON) Advance();
                if (Peek().Type == TokenType.SELECT)
                {
                    Advance(); // 跳过 SELECT
                }
            }

            return stmt;
        }

        private CaseBlock ParseCaseBlock()
        {
            CaseBlock caseBlock = new CaseBlock();

            // 解析CASE条件
            while (!AtEnd() && Peek().Type != TokenType.COLON && Peek().Type != TokenType.EOF)
            {
                CaseCondition condition = ParseCaseCondition();
                caseBlock.Conditions.Add(condition);

                if (Peek().Type == TokenType.COMMA)
                {
                    Advance(); // 跳过逗号，继续解析下一个条件
                }
                else if (Peek().Type == TokenType.AND)
                {
                    // QBASIC 兼容: CASE IS >= 18 AND IS < 60
                    Advance(); // 跳过 AND，继续解析下一个条件
                }
                else
                {
                    break;
                }
            }

            // 解析CASE块体
            caseBlock.Body = ParseStatementBlock();
            return caseBlock;
        }

        private CaseCondition ParseCaseCondition()
        {
            CaseCondition condition = new CaseCondition();

            if (Peek().Type == TokenType.ELSE)
            {
                // CASE ELSE
                condition.Type = CaseCondition.ConditionType.Else;
                Advance(); // 跳过 ELSE
                return condition;
            }

            if (Peek().Type == TokenType.IS)
            {
                // CASE IS > 5
                condition.Type = CaseCondition.ConditionType.Comparison;
                Advance(); // 跳过 IS

                // 解析比较运算符
                Token opToken = Peek();
                if (IsComparisonOperator(opToken.Type))
                {
                    condition.ComparisonOp = opToken.Type;
                    Advance(); // 跳过比较运算符
                }
                else
                {
                    // 默认使用等于
                    condition.ComparisonOp = TokenType.EQUALS;
                }

                // 解析比较值（使用 ParseTerm 避免 AND 被当作逻辑运算符消费）
                condition.CompareValue = ParseTerm();
                return condition;
            }

            // 解析第一个值
            Expression firstExpr = ParseExpression();

            if (Peek().Type == TokenType.TO)
            {
                // CASE 1 TO 10
                condition.Type = CaseCondition.ConditionType.Range;
                condition.FromValue = firstExpr;
                Advance(); // 跳过 TO
                condition.ToValue = ParseExpression();
            }
            else
            {
                // CASE 5
                condition.Type = CaseCondition.ConditionType.Value;
                condition.Value = firstExpr;
            }

            return condition;
        }

        private List<Statement> ParseStatementBlock()
        {
            List<Statement> statements = new List<Statement>();

            if (Peek().Type == TokenType.COLON)
            {
                // 单行语句块: CASE 5: PRINT "Five"
                Advance(); // 跳过冒号
                if (!AtEnd() && Peek().Type != TokenType.EOF)
                {
                    Statement stmt = ParseStatement();
                    if (stmt != null)
                    {
                        statements.Add(stmt);
                    }
                }
            }
            else
            {
                // 多行语句块 — 停止于块分隔关键字
                while (!AtEnd() && Peek().Type != TokenType.CASE &&
                       Peek().Type != TokenType.ELSE && Peek().Type != TokenType.EOF &&
                       Peek().Type != TokenType.NEXT && Peek().Type != TokenType.WEND &&
                       Peek().Type != TokenType.LOOP && Peek().Type != TokenType.END)
                {
                    Statement stmt = ParseStatement();
                    if (stmt != null)
                    {
                        statements.Add(stmt);
                    }
                }
            }

            return statements;
        }

        private Statement ParsePokeStatement()
        {
            Advance(); // consume POKE
            var addrExpr = ParseExpression();
            Expect(TokenType.COMMA, "POKE 语法: POKE address, value");
            var valueExpr = ParseExpression();
            var t = Peek();
            return new PokeStatement(addrExpr, valueExpr, t.Line, t.Column);
        }

        private Statement ParseChipAsmStatement()
        {
            Advance(); // consume CHIPASM
            if (Peek().Type != TokenType.LPAREN) throw Error("期望 '(' 在 CHIPASM 后");
            Advance(); // consume (
            if (Peek().Type != TokenType.STRING) throw Error("期望字符串形式的 arch");
            string arch = Advance().Value;
            if (Peek().Type != TokenType.COMMA) throw Error("期望 ','");
            Advance(); // consume ,
            if (Peek().Type != TokenType.STRING) throw Error("期望字符串形式的 code");
            string code = Advance().Value;
            if (Peek().Type != TokenType.RPAREN) throw Error("期望 ')'");
            Advance(); // consume )
            var t2 = Peek();
            return new ChipAsmStatement(arch, code, t2.Line, t2.Column);
        }

        private bool IsComparisonOperator(TokenType type)
        {
            return type == TokenType.EQUALS || type == TokenType.NOT_EQUAL ||
                   type == TokenType.LESS || type == TokenType.LESS_EQUAL ||
                   type == TokenType.GREATER || type == TokenType.GREATER_EQUAL;
        }
        // ParseAsmStatement() 已移除 — asm() 仅限 C/ObjC/C++ 语言
        // BASIC 通过 Lib/shared/vmlsys.c 调用系统功能，ChipAsmStatement (chipasm) 保留

        private new void Expect(TokenType expected, string errorMessage)
        {
            if (Peek().Type != expected)
            {
                throw Error($"{errorMessage} (第{Peek().Line}行, 第{Peek().Column}列)");
            }
        }

        private Statement ParseSystemStatement()
        {
            Token token = Advance(); // skip SYSTEM
            var stmt = new SystemStatement(token.Line, token.Column);
            // Optional exit code: SYSTEM n
            if (Peek().Type != TokenType.EOF && Peek().Type != TokenType.COLON)
                stmt.ExitCode = ParseExpression();
            return stmt;
        }

        /// <summary>MAT 矩阵操作 — 跳过 (v1.66.32+)</summary>
        private Statement ParseMatStatement()
        {
            Advance(); // 跳过 MAT
            while (!AtEnd() && Peek().Type != TokenType.EOF && Peek().Type != TokenType.COLON)
                Advance();
            return null;
        }

        /// <summary>ENUM name ... END ENUM — 跳过整个块 (v1.66.32+)</summary>
        private Statement ParseEnumStatement()
        {
            Advance(); // 跳过 ENUM
            if (Peek().Type == TokenType.IDENTIFIER) Advance(); // 跳过枚举名
            // 跳到 END ENUM
            while (!AtEnd() && Peek().Type != TokenType.END)
                Advance();
            if (Peek().Type == TokenType.END)
            {
                Advance(); // 跳过 END
                if (Peek().Type == TokenType.ENUM_KW) Advance(); // 跳过 ENUM
            }
            return null;
        }

        /// <summary>解析 ChipBasic GPIO 语句: PINMODE/DIGITALWRITE/DIGITALREAD (v1.66.32+)</summary>
        private GpioStatement ParseGpioStatement(string func)
        {
            Token token = Advance(); // 跳过 PINMODE/DIGITALWRITE/DIGITALREAD
            var stmt = new GpioStatement(token.Line, token.Column) { Function = func };
            // 读取参数: pin, value (逗号分隔)
            while (Peek().Type != TokenType.EOF && Peek().Type != TokenType.COLON)
            {
                if (Peek().Type == TokenType.NUMBER || Peek().Type == TokenType.IDENTIFIER)
                {
                    stmt.Arguments.Add(Peek().Value);
                    Advance();
                }
                else if (Peek().Type == TokenType.COMMA)
                    Advance();
                else
                    break;
            }
            return stmt;
        }
    }
}
