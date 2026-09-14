using CompilerBase;
using System.Collections.Generic;
using VMLPlugins;

namespace GoCompiler
{
    public partial class Parser : ParserBase<Token, TokenType>
    {
        private bool _isMCU;

        protected override TokenType GetTokenType(Token token) => token.Type;

        public Parser(List<Token> tokens, bool isMCU = true) : base(tokens)
        {
            _isMCU = isMCU;
        }

        protected override ParseException Error(string message)
        {
            return new ParseException($"语法错误 在第{Cur.Line}行{Cur.Column}列：{message}", Cur!);
        }

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
