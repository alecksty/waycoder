namespace LadderCompiler
{
    /// <summary>
    /// 梯形图词法单元类型
    /// </summary>
    public enum TokenType
    {
        // 特殊标记
        EOF,
        NewLine,
        Whitespace,
        Comment,
        
        // 标识符和常量
        Identifier,
        Number,
        String,
        HexNumber,
        BinaryNumber,
        
        // 梯形图元素
        ContactNormallyOpen,      // --| |--
        ContactNormallyClosed,    // --|/|--
        Coil,                     // --( )--
        CoilSet,                  // --(S)--
        CoilReset,                // --(R)--
        CoilPositiveTransition,   // --(P)--
        CoilNegativeTransition,   // --(N)--
        
        // 函数块
        FunctionBlock,            // --[FB]--
        TimerBlock,               // --[TON]--, --[TOF]--, --[TP]--
        CounterBlock,             // --[CTU]--, --[CTD]--, --[CTUD]--
        CompareBlock,             // --[CMP]--
        MathBlock,                // --[ADD]--, --[SUB]--, --[MUL]--, --[DIV]--
        
        // 运算符
        Assignment,               // :=
        Comparison,               // =, <>, <, >, <=, >=
        Arithmetic,               // +, -, *, /, MOD
        Logical,                  // AND, OR, XOR, NOT
        
        // 分隔符
        LeftParenthesis,          // (
        RightParenthesis,         // )
        LeftBracket,              // [
        RightBracket,             // ]
        Comma,                    // ,
        Colon,                    // :
        Semicolon,                // ;
        Dot,                      // .
        At,                       // @
        Percent,                  // %
        
        // 关键字
        KeywordProgram,
        KeywordFunction,
        KeywordFunctionBlock,
        KeywordVar,
        KeywordVarInput,
        KeywordVarOutput,
        KeywordVarInOut,
        KeywordVarGlobal,
        KeywordVarExternal,
        KeywordVarAccess,
        KeywordVarTemp,
        KeywordEndVar,
        KeywordBegin,
        KeywordEndProgram,
        KeywordEndFunction,
        KeywordEndFunctionBlock,
        KeywordIf,
        KeywordThen,
        KeywordElsif,
        KeywordElse,
        KeywordEndIf,
        KeywordCase,
        KeywordOf,
        KeywordEndCase,
        KeywordFor,
        KeywordTo,
        KeywordBy,
        KeywordDo,
        KeywordEndFor,
        KeywordWhile,
        KeywordDoWhile,
        KeywordEndWhile,
        KeywordRepeat,
        KeywordUntil,
        KeywordEndRepeat,
        KeywordReturn,
        KeywordWith,
        KeywordAt,
        KeywordRetain,
        KeywordNonRetain,
        KeywordConstant,
        KeywordType,
        KeywordEndType,
        KeywordStruct,
        KeywordEndStruct,
        KeywordEnum,
        KeywordEndEnum,
        KeywordSubrange,
        KeywordArray,
        KeywordOfType,
        
        // 数据类型
        TypeBool,
        TypeByte,
        TypeWord,
        TypeDWord,
        TypeLWord,
        TypeSInt,
        TypeInt,
        TypeDInt,
        TypeLInt,
        TypeUSInt,
        TypeUInt,
        TypeUDInt,
        TypeULInt,
        TypeReal,
        TypeLReal,
        TypeTime,
        TypeDate,
        TypeTimeOfDay,
        TypeDateAndTime,
        TypeString,
        TypeWString,
        
        // 特殊值
        ValueTrue,
        ValueFalse,
        ValueNull,
        
        // 预定义函数
        FunctionTON,
        FunctionTOF,
        FunctionTP,
        FunctionCTU,
        FunctionCTD,
        FunctionCTUD,
        FunctionADD,
        FunctionSUB,
        FunctionMUL,
        FunctionDIV,
        FunctionMOD,
        FunctionMOVE,
        FunctionLIMIT,
        FunctionSEL,
        FunctionMAX,
        FunctionMIN,
        FunctionABS,
        FunctionSQRT,
        FunctionLN,
        FunctionLOG,
        FunctionEXP,
        FunctionSIN,
        FunctionCOS,
        FunctionTAN,
        FunctionASIN,
        FunctionACOS,
        FunctionATAN,

        // IL指令
        LD,
        LDI,
        ST,
        OUT,
        SET,
        RST,
        ANDN,
        ORN,

        // 错误
        Error
    }
}
