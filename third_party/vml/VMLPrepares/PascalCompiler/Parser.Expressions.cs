using CompilerBase;
using System;
using System.Collections.Generic;

namespace PascalCompiler
{
    public partial class Parser : ParserBase<Token, TokenType>
    {

        private ExpressionNode ParseExpression()
        {
            ExpressionNode left = ParseSimpleExpression();
            
            while (Match(TokenType.EQUALS, TokenType.NOT_EQUALS, TokenType.LESS_THAN, 
                        TokenType.LESS_EQUAL, TokenType.GREATER_THAN, TokenType.GREATER_EQUAL,
                        TokenType.IN))
            {
                TokenType op = Previous().Type; // Match已经消耗了运算符
                ExpressionNode right = ParseSimpleExpression();
                left = new BinaryOpNode
                {
                    Operator = op,
                    Left = left,
                    Right = right,
                    Line = left.Line,
                    Column = left.Column
                };
            }
            
            return left;
        }

        private ExpressionNode ParseSimpleExpression()
        {
            ExpressionNode left = ParseTerm();

            while (Match(TokenType.PLUS, TokenType.MINUS, TokenType.OR, TokenType.XOR, TokenType.SHL, TokenType.SHR))
            {
                TokenType op = Previous().Type; // Match已经消耗了运算符
                ExpressionNode right = ParseTerm();
                left = new BinaryOpNode
                {
                    Operator = op,
                    Left = left,
                    Right = right,
                    Line = left.Line,
                    Column = left.Column
                };
            }

            return left;
        }

        private ExpressionNode ParseTerm()
        {
            ExpressionNode left = ParseFactor();

            while (Match(TokenType.STAR, TokenType.SLASH, TokenType.DIV, TokenType.MOD, TokenType.AND))
            {
                TokenType op = Previous().Type; // Match已经消耗了运算符
                ExpressionNode right = ParseFactor();
                left = new BinaryOpNode
                {
                    Operator = op,
                    Left = left,
                    Right = right,
                    Line = left.Line,
                    Column = left.Column
                };
            }

            return left;
        }

        /// <summary>
        /// **原子表达式的唯一入口** —— 顺手盖上它自己的行列（`ASTNode.Line`/`Column`）。
        ///
        /// 与语句入口同一套路，但**粒度细一层**：语句级的列只能给到「这一句从哪开始」，
        /// 而用户报错时要看到的是**出错的那个标识符**从哪开始。原子（标识符/字面量/调用/括号）
        /// 是位置信息真正有意义的地方，而全部语句的表达式都是从这里递归产出的
        /// ⇒ 在这一处包一层就覆盖了整棵树。
        /// </summary>
        private ExpressionNode ParseFactor()
        {
            var __start = Cur;
            int __line = __start.Line, __col = __start.Column;
            var __node = ParseFactorCore();
            if (__node != null && __node.Line == 0) { __node.Line = __line; __node.Column = __col; }
            return __node;
        }

        private ExpressionNode ParseFactorCore()
        {
            Token token = Cur;

            // Pascal set 字面量: [1, 3, 5]
            if (Match(TokenType.LBRACKET))
            {
                var setExpr = new SetExpressionNode { Line = token.Line, Column = token.Column };
                if (GetTokenType(Cur) != TokenType.RBRACKET)
                {
                    do { setExpr.Elements.Add(ParseExpression()); }
                    while (Match(TokenType.COMMA));
                }
                Expect(TokenType.RBRACKET, "期望 ']'");
                return setExpr;
            }

            if (Match(TokenType.IDENTIFIER))
            {
                // 检查是否为函数调用
                if (GetTokenType(Cur) == TokenType.LPAREN)
                {
                    string name = token.Value.ToString();
                    FunctionCallNode call = new FunctionCallNode
                    {
                        Name = name,
                        Line = token.Line,
                        Column = token.Column
                    };

                    Expect(TokenType.LPAREN, "期望 '('");
                    if (GetTokenType(Cur) != TokenType.RPAREN)
                    {
                        do
                        {
                            call.Arguments.Add(ParseExpression());
                        } while (Match(TokenType.COMMA));
                    }
                    Expect(TokenType.RPAREN, "期望 ')'");

                    return call;
                }
                else
                {
                    // 回退一个token，让ParseVariable重新解析标识符
                    _pos--;
                    return ParseVariable();
                }
            }
            else if (Match(TokenType.NIL))
            {
                return new LiteralNode
                {
                    Value = 0,
                    Type = TokenType.NIL,
                    Line = token.Line,
                    Column = token.Column
                };
            }
            else if (Match(TokenType.AT))
            {
                return new AddressOfNode
                {
                    Variable = ParseVariable(),
                    Line = token.Line,
                    Column = token.Column
                };
            }
            else if (Match(TokenType.CARET))
            {
                return new DereferenceNode
                {
                    Pointer = ParseFactor(),
                    Line = token.Line,
                    Column = token.Column
                };
            }
            else if (Match(TokenType.INTEGER_LITERAL, TokenType.REAL_LITERAL))
            {
                return new LiteralNode
                {
                    Value = token.Value,
                    Type = token.Type,
                    Line = token.Line,
                    Column = token.Column
                };
            }
            else if (Match(TokenType.STRING_LITERAL, TokenType.CHAR_LITERAL))
            {
                return new LiteralNode
                {
                    Value = token.Value,
                    Type = token.Type,
                    Line = token.Line,
                    Column = token.Column
                };
            }
            else if (Match(TokenType.TRUE, TokenType.FALSE))
            {
                return new LiteralNode
                {
                    Value = token.Type == TokenType.TRUE,
                    Type = TokenType.BOOLEAN,
                    Line = token.Line,
                    Column = token.Column
                };
            }
            else if (Match(TokenType.LPAREN))
            {
                ExpressionNode expr = ParseExpression();
                Expect(TokenType.RPAREN, "期望 ')'");
                return expr;
            }
            else if (Match(TokenType.NOT, TokenType.PLUS, TokenType.MINUS))
            {
                TokenType op = token.Type;
                ExpressionNode operand = ParseFactor();
                return new UnaryOpNode
                {
                    Operator = op,
                    Operand = operand,
                    Line = token.Line,
                    Column = token.Column
                };
            }
            else
            {
                // **报错，但把这一句接着解析下去** —— 少一个表达式不影响解析器继续
                // 认出后面的东西（同一句里还有别的错时也能一起报出来），所以不当场停。
                //
                // ⚠ 这里原先是 `Error("期望表达式"); return null;` —— **`Error` 是
                //   「构造并返回异常」、不会自己抛**（Pascal 的覆写就是 `=> new ParseException(…)`）。
                //   少了 `throw` 就等于**把错构造出来直接丢掉**，然后返回 null；
                //   null 一路流到代码生成，崩成一句没有位置的
                //   「内部错误: Object reference not set…」。实测 `WriteLn(1 + )` 就是这样
                //   （DiagProbe【语法错误】档 pas 一栏）。
                //   改走 `GccError`（**收集**）之后，位置与文案都进诊断，编译整体照样失败。
                GccError("期望表达式", ErrorCode.Parser_ExpectedExpression);
                // 占位 0 顶上去，AST 保持完好 —— 这是"能继续"的那一类错误该有的恢复。
                return new LiteralNode
                {
                    Value = 0,
                    Type = TokenType.INTEGER_LITERAL,
                    Line = token.Line,
                    Column = token.Column
                };
            }
        }

        private SubprogramDeclarationNode? ParseSubprogramHeader()
        {
            SubprogramDeclarationNode? subprogram = null;

            if (GetTokenType(Cur) == TokenType.PROCEDURE)
            {
                Advance();
                var proc = new ProcedureDeclarationNode();
                proc.Name = Cur.Value.ToString();
                Expect(TokenType.IDENTIFIER, "期望过程名称");
                subprogram = proc;
            }
            else if (GetTokenType(Cur) == TokenType.FUNCTION)
            {
                Advance();
                var func = new FunctionDeclarationNode();
                func.Name = Cur.Value.ToString();
                Expect(TokenType.IDENTIFIER, "期望函数名称");
                subprogram = func;
            }
            else if (GetTokenType(Cur) == TokenType.CONSTRUCTOR)
            {
                Advance();
                var ctor = new ProcedureDeclarationNode();
                ctor.Name = Cur.Value.ToString();
                Expect(TokenType.IDENTIFIER, "期望构造名");
                ctor.IsConstructor = true;
                subprogram = ctor;
            }
            else if (GetTokenType(Cur) == TokenType.DESTRUCTOR)
            {
                Advance();
                var dtor = new ProcedureDeclarationNode();
                dtor.Name = Cur.Value.ToString();
                Expect(TokenType.IDENTIFIER, "期望析构名");
                dtor.IsDestructor = true;
                subprogram = dtor;
            }

            if (subprogram == null) return null;

            if (Match(TokenType.LPAREN))
            {
                ParseParameterList(subprogram.Parameters);
                Expect(TokenType.RPAREN, "期望 ')'");
            }

            if (subprogram is FunctionDeclarationNode funcNode && Match(TokenType.COLON))
            {
                funcNode.ReturnType = ParseType();
            }

            Expect(TokenType.SEMICOLON, "期望 ';'");
            subprogram.IsForward = true;
            return subprogram;
        }

        private SubprogramDeclarationNode? ParseSubprogramDeclaration()
        {
            SubprogramDeclarationNode? subprogram = null;

            if (GetTokenType(Cur) == TokenType.PROCEDURE)
            {
                Advance();
                var proc = new ProcedureDeclarationNode();
                proc.Name = Cur.Value.ToString();
                Expect(TokenType.IDENTIFIER, "期望过程名称");
                subprogram = proc;
            }
            else if (GetTokenType(Cur) == TokenType.FUNCTION)
            {
                Advance(); // 跳过 function
                var func = new FunctionDeclarationNode();
                func.Name = Cur.Value.ToString();
                Expect(TokenType.IDENTIFIER, "期望函数名称");
                subprogram = func;
            }
            else if (GetTokenType(Cur) == TokenType.CONSTRUCTOR)
            {
                Advance();
                var ctor = new ProcedureDeclarationNode();
                ctor.Name = Cur.Value.ToString();
                Expect(TokenType.IDENTIFIER, "期望构造名");
                ctor.IsConstructor = true;
                subprogram = ctor;
            }
            else if (GetTokenType(Cur) == TokenType.DESTRUCTOR)
            {
                Advance();
                var dtor = new ProcedureDeclarationNode();
                dtor.Name = Cur.Value.ToString();
                Expect(TokenType.IDENTIFIER, "期望析构名");
                dtor.IsDestructor = true;
                subprogram = dtor;
            }

            if (subprogram == null) return null;

            // 解析参数列表
            if (Match(TokenType.LPAREN))
            {
                ParseParameterList(subprogram.Parameters);
                Expect(TokenType.RPAREN, "期望 ')'");
            }
            
            // 解析函数返回类型(在参数列表之后)
            if (subprogram is FunctionDeclarationNode funcNode && Match(TokenType.COLON))
            {
                funcNode.ReturnType = ParseType();
            }
            
            // 检查forward声明
            if (GetTokenType(Cur) == TokenType.SEMICOLON)
            {
                // 先看一下下一个token是不是forward
                // 不消耗分号，只是检查
                // 实际上我们需要检查的是 forward 关键字
                // 简化处理：如果不是forward，就期望分号并继续
            }
            
            Expect(TokenType.SEMICOLON, "期望 ';'");
            
            if (GetTokenType(Cur) == TokenType.FORWARD)
            {
                Advance();
                subprogram.IsForward = true;
                Expect(TokenType.SEMICOLON, "期望 ';'");
                return subprogram;
            }
            
            // 解析局部声明: var, const, type, label
            while (GetTokenType(Cur) == TokenType.VAR || GetTokenType(Cur) == TokenType.CONST ||
                   GetTokenType(Cur) == TokenType.TYPE || GetTokenType(Cur) == TokenType.LABEL)
            {
                if (GetTokenType(Cur) == TokenType.VAR)
                {
                    Advance();
                    while (GetTokenType(Cur) == TokenType.IDENTIFIER)
                    {
                        var varDecls = ParseMultiVarDeclaration();
                        foreach (var decl in varDecls)
                            subprogram.LocalVariables.Add(decl);
                        Expect(TokenType.SEMICOLON, "期望 ';'");
                    }
                }
                else if (GetTokenType(Cur) == TokenType.LABEL)
                {
                    Advance(); // label
                    while (GetTokenType(Cur) == TokenType.IDENTIFIER || GetTokenType(Cur) == TokenType.INTEGER_LITERAL)
                    {
                        Advance(); // label name
                        if (Match(TokenType.COMMA)) continue;
                        break;
                    }
                    Expect(TokenType.SEMICOLON, "期望 ';'");
                }
                else
                {
                    // const / type: 跳过直到 ;
                    Advance();
                    while (GetTokenType(Cur) != TokenType.SEMICOLON && GetTokenType(Cur) != TokenType.EOF)
                        Advance();
                    Expect(TokenType.SEMICOLON, "期望 ';'");
                }
            }

            // 解析嵌套子程序声明 (Turbo Pascal 支持过程/函数嵌套)
            while (GetTokenType(Cur) == TokenType.PROCEDURE || GetTokenType(Cur) == TokenType.FUNCTION)
            {
                var nested = ParseSubprogramDeclaration();
                if (nested != null)
                    subprogram.NestedSubprograms.Add(nested);
            }

            // 解析过程/函数体
            Expect(TokenType.BEGIN, "期望 'begin'");
            subprogram.Body = ParseBlock();
            Expect(TokenType.END, "期望 'end'");
            Expect(TokenType.SEMICOLON, "期望 ';'");
            
            return subprogram;
        }

        private void ParseParameterList(List<ParameterNode> parameters)
        {
            while (GetTokenType(Cur) == TokenType.IDENTIFIER || GetTokenType(Cur) == TokenType.VAR)
            {
                bool isVar = false;
                if (GetTokenType(Cur) == TokenType.VAR)
                {
                    isVar = true;
                    Advance();
                }
                
                // 解析参数名列表
                var names = new List<string>();
                names.Add(Cur.Value.ToString());
                Expect(TokenType.IDENTIFIER, "期望参数名");
                
                while (Match(TokenType.COMMA))
                {
                    names.Add(Cur.Value.ToString());
                    Expect(TokenType.IDENTIFIER, "期望参数名");
                }
                
                Expect(TokenType.COLON, "期望 ':'");
                var type = ParseType();
                
                foreach (var name in names)
                {
                    parameters.Add(new ParameterNode
                    {
                        Name = name,
                        Type = type,
                        IsVarParameter = isVar
                    });
                }
                
                if (Match(TokenType.SEMICOLON))
                    continue;
                break;
            }
        }
    }
}
