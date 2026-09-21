namespace CompilerBase;

/// <summary>
/// 编译器错误码枚举, 按阶段分组.
/// 1000=词法, 1100=语法, 1200=预处理, 1300=代码生成, 1400=通用.
/// </summary>
public enum ErrorCode
{
    // ---- 通用 (0-9) ----
    Unknown = 0,

    // ---- 词法错误 (1000-1099) ----
    Lexer_UnknownCharacter = 1000,
    Lexer_UnterminatedString = 1001,
    Lexer_UnterminatedComment = 1002,
    Lexer_UnterminatedCharLiteral = 1003,
    Lexer_EmptyCharLiteral = 1004,
    Lexer_InvalidHexEscape = 1005,
    Lexer_InvalidOctalDigit = 1006,
    Lexer_InvalidNumberSuffix = 1007,
    Lexer_InvalidSuffixCombination = 1008,
    Lexer_TooManyTokens = 1009,
    Lexer_InvalidPreprocessorDirective = 1010,
    Lexer_CharLiteralTooLong = 1011,
    Lexer_UnterminatedAsm = 1012,
    Lexer_InvalidFloatSuffix = 1013,
    Lexer_InvalidSciSuffix = 1014,

    // ---- 语法错误 (1100-1199) ----
    Parser_SyntaxError = 1100,
    Parser_UnexpectedToken = 1101,
    Parser_ExpectedToken = 1102,
    Parser_ExpectedExpression = 1103,
    Parser_ExpectedType = 1104,
    Parser_TypeConflict = 1105,
    Parser_InvalidDeclaration = 1106,
    Parser_TooManyIterations = 1107,
    Parser_ExpectedIdentifier = 1108,
    Parser_BreakOutsideLoop = 1109,
    Parser_ContinueOutsideLoop = 1110,

    // ---- 预处理错误 (1200-1299) ----
    Preprocessor_UnclosedIf = 1200,
    Preprocessor_IncludeNotFound = 1201,
    Preprocessor_InvalidInclude = 1202,
    Preprocessor_MacroMissingParen = 1203,
    Preprocessor_ElseWithoutIf = 1204,
    Preprocessor_ElifWithoutIf = 1205,
    Preprocessor_EndifWithoutIf = 1206,
    Preprocessor_ErrorDirective = 1207,
    Preprocessor_ParamSyntaxError = 1208,
    Preprocessor_UnknownParamFunction = 1209,
    Preprocessor_MaxIncludeDepth = 1210,

    // ---- 代码生成错误 (1300-1399) ----
    CodeGen_UndefinedVariable = 1300,
    CodeGen_UndefinedArray = 1301,
    CodeGen_UndefinedFunction = 1302,
    CodeGen_TypeMismatch = 1303,
    CodeGen_CannotTakeAddress = 1304,
    CodeGen_UnsupportedExpression = 1305,
    CodeGen_InvalidOperand = 1306,
    CodeGen_NullLabel = 1307,
    CodeGen_NullKey = 1308,
    CodeGen_ConstantNeedsInit = 1311,
    CodeGen_UndefinedConst = 1312,
    CodeGen_BreakOutsideLoop = 1314,
    CodeGen_ContinueOutsideLoop = 1315,
    /// <summary>定义了但从未被调用的函数（**警告**，不是错误 —— 见 `WarnUndefined` 一族的说明）。</summary>
    CodeGen_UnusedFunction = 1316,
    /// <summary>定义了但从未被引用的局部变量（**警告**）。</summary>
    CodeGen_UnusedVariable = 1317,
    /// <summary>
    /// **实参个数不足**（1318）。
    ///
    /// 目前只有那几个「前端直接生成指令」的内置函数（`getenv`/`setenv`/`PEEK`/`POKE`…）
    /// 会报它 —— 它们要在编译期取 `Args[0]`/`Args[1]`，少一个就
    /// `ArgumentOutOfRangeException` 抛到用户脸上（一行 .NET 异常文本，
    /// **没有文件名、没有行号、没有诊断码**）。
    ///
    /// ⚠ 与普通函数**不一致**，这是已知的：C 前端对 `strlen()` / 用户函数
    /// **一概不做参数个数检查**（实测少传照样编过）。要统一得先有"声明表"，
    /// 那是另一件事；这里只保证**内置函数这条路不再是崩溃**。
    /// </summary>
    CodeGen_ArgCountMismatch = 1318,

    // ---- 通用编译错误 (1400-1499) ----
    Compilation_LibraryNotFound = 1400,
    Compilation_InternalError = 1401,
    Compilation_Timeout = 1402,
    /// <summary>「没进展 ⇒ 死循环」的进展守卫判定的（见 `ProgressGuard`）—— 编译器内部缺陷，不是用户输入的问题。</summary>
    Compilation_NotConverging = 1404,
    Compilation_BinaryFile = 1403,
}
