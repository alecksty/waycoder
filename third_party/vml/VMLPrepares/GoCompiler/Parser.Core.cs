using CompilerBase;
using System.Collections.Generic;
using VMLPlugins;

namespace GoCompiler
{
    public partial class Parser : ParserBase<Token, TokenType>
    {
        private bool _isMCU;

        /// <summary>
        /// &gt;0 表示正在解析控制语句的头部（if/for/switch 的条件、for 的 post）。
        /// Go 规范禁止在这里出现复合字面量 —— 否则 `for i &lt; A[1] {` 的那个 `{`
        /// 会被当成 `A[1]` 的复合字面量，把整个循环体当元素列表吃掉，
        /// 报错却是"Expected RBRACE, but got IF"（v0.96.195）。
        /// </summary>
        private int _noCompositeLiteral;

        protected override TokenType GetTokenType(Token token) => token.Type;

        /// <summary>按"不允许复合字面量"的规则解析控制语句头部里的表达式</summary>
        private ASTNode ParseConditionExpr()
        {
            _noCompositeLiteral++;
            try { return ParseExpression(); }
            finally { _noCompositeLiteral--; }
        }

        public Parser(List<Token> tokens, bool isMCU = true) : base(tokens)
        {
            _isMCU = isMCU;
        }

        // ⚠ 这里原先有一条 `protected override ParseException Error(string message)`
        //   自己拼 `语法错误 在第{Cur.Line}行{Cur.Column}列：…`。**已删除、改用基类实现**，
        //   理由是它比基类少两件事：
        //     ① 没有 `文件:行:列: error:` 前缀 —— 宿主（CLI/MAUI/LSP）按这个形状锚位置
        //        （`VmlDiagnostics` 的 4 条正则 + 编辑器气泡），Go 的语法错误在编辑器里
        //        锚不到行；
        //     ② 没走 `ResolveDiagnosticPosition` ⇒ **不查预处理行号映射** ——
        //        错在 `#include` 进来的头文件里时，报的是拼接后的行号。
        //   Go 的 `Token` 实现了 `ITokenPosition`（`Token.cs:6`），基类那条通用实现
        //   取到的行列**与这里手写的完全同源**（同一只 `Cur`），所以删掉只是补上前缀与映射，
        //   位置一个字不变。实测 DiagProbe 三档无回归。

        private void SkipNewlines()
        {
            while (GetTokenType(Cur) == TokenType.NEWLINE || GetTokenType(Cur) == TokenType.SEMICOLON || GetTokenType(Cur) == TokenType.COMMENT)
            {
                Advance();
            }
        }

        private static bool IsCompoundAssign(TokenType type) => type switch
        {
            TokenType.ADD_ASSIGN or TokenType.SUB_ASSIGN or TokenType.MUL_ASSIGN or TokenType.DIV_ASSIGN
            or TokenType.MOD_ASSIGN or TokenType.AND_ASSIGN or TokenType.OR_ASSIGN or TokenType.XOR_ASSIGN
            or TokenType.LSHIFT_ASSIGN or TokenType.RSHIFT_ASSIGN or TokenType.AND_NOT_ASSIGN => true,
            _ => false
        };

        private bool IsTypeStart(TokenType type)
        {
            return type == TokenType.IDENTIFIER || type == TokenType.STAR || type == TokenType.LBRACKET ||
                   type == TokenType.MAP || type == TokenType.CHAN || type == TokenType.FUNC ||
                   type == TokenType.STRUCT || type == TokenType.INTERFACE || type == TokenType.INT ||
                   type == TokenType.INT8 || type == TokenType.INT16 || type == TokenType.INT32 ||
                   type == TokenType.INT64 || type == TokenType.UINT || type == TokenType.UINT8 ||
                   type == TokenType.UINT16 || type == TokenType.UINT32 || type == TokenType.UINT64 ||
                   type == TokenType.FLOAT32 || type == TokenType.FLOAT64 || type == TokenType.COMPLEX64 ||
                   type == TokenType.COMPLEX128 || type == TokenType.BOOL || type == TokenType.STRING ||
                   type == TokenType.BYTE || type == TokenType.RUNE || type == TokenType.ERROR;
        }

    }
}
