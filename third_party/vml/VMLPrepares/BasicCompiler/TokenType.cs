namespace BasicCompiler
{
    /// <summary>
    /// BASIC 语言的令牌类型
    /// </summary>
    public enum TokenType
    {
        // 关键字
        REM,    // 注释
        PRINT,  // 输出
        INPUT,  // 输入
        LET,    // 赋值
        IF,     // 条件
        THEN,   // 条件分支
        ELSE,   // 否则分支
        ELSEIF, // ELSE IF
        END,    // 结束
        GOTO,   // 跳转
        GOSUB,  // 调用子程序
        RETURN, // 返回
        FOR,    // 循环开始
        STEP,   // 循环步长
        NEXT,   // 循环结束
        WHILE,  // 当循环
        WEND,   // 当循环结束
        DO,     // DO循环开始
        LOOP,   // DO循环结束
        UNTIL,  // 直到条件
        DIM,    // 数组声明
        
        // 标识符
        IDENTIFIER,  // 变量名
        
        // 常量
        NUMBER,      // 数字
        STRING,      // 字符串
        
        // 运算符
        PLUS,        // +
        MINUS,       // -
        MULTIPLY,    // *
        DIVIDE,      // /
        /// <summary>
        /// `\` —— BASIC 的**整除**运算符（与 `/` 同优先级，结果取整）。
        ///
        /// ⚠ 原来**根本没有这个 token**：词法把它当"未知字符"发 ERROR，表达式解析
        /// 走到那儿就停了 —— 于是 `c = 100 \ 20` 静默变成 `c = 100`（`\ 20` 整段被丢），
        /// 而按 `\` 排版的代码（比如用格宽算坐标）会踩运行时"整数除零"。
        /// </summary>
        INT_DIVIDE,
        EXPONENT,    // ^
        EQUALS,      // =
        LESS,        // <
        LESS_EQUAL,  // <=
        GREATER,     // >
        GREATER_EQUAL, // >=
        NOT_EQUAL,   // <>
        
        // 逻辑运算符
        AND,         // AND
        OR,          // OR
        NOT,         // NOT
        
        // 子程序
        SUB,         // SUB
        FUNCTION,    // FUNCTION
        CALL,        // CALL
        BYVAL,       // BYVAL
        BYREF,       // BYREF
        EXIT,        // EXIT (EXIT FOR, EXIT WHILE)
        
        // 标点符号
        COMMA,       // ,
        SEMICOLON,   // ;
        COLON,       // :
        DOT,         // .
        QUOTE,       // "
        LPAREN,      // (
        RPAREN,      // )
        
        // 硬件接口关键字
        KB_HIT,      // 检查键盘
        KB_GETCH,    // 获取按键
        MOUSE_GETX,  // 获取鼠标X坐标
        MOUSE_GETY,  // 获取鼠标Y坐标
        MOUSE_LEFT,  // 鼠标左键
        MOUSE_RIGHT, // 鼠标右键
        
        // 控制流关键字
        SELECT,      // SELECT CASE
        CASE,        // CASE
        IS,          // CASE IS
        TO,          // CASE 1 TO 10
        
        // 特殊
        EOF,         // 文件结束
        ERROR,       // 错误
        
        // 文件操作关键字
        OPEN,        // OPEN
        CLOSE,       // CLOSE
        AS,          // AS
        FREEFILE,    // FREEFILE
        HASH,        // #
        
        // QBASIC 标准关键字
        SCREEN,      // SCREEN 模式
        PSET,        // PSET (x, y), color
        QB_LINE,     // LINE (x1,y1)-(x2,y2), color
        QB_CIRCLE,   // CIRCLE (x, y), radius, color
        PAINT,       // PAINT (x, y), color, border
        LOCATE,      // LOCATE row, col
        QB_COLOR,    // COLOR foreground, background
        INKEY,       // INKEY$ 键盘输入（字符串函数）
        RANDOMIZE,   // RANDOMIZE 随机数种子
        QB_WIDTH,    // WIDTH cols, rows
        BEEP,        // BEEP 扬声器
        SLEEP,       // SLEEP 暂停
        SWAP,        // SWAP 变量交换
        ERASE,       // ERASE 清除数组
        TIMER_FUNC,  // TIMER 函数（返回秒数）
        DATE_FUNC,   // DATE$ 日期函数
        TIME_FUNC,   // TIME$ 时间函数
        SYSTEM,      // SYSTEM 退出程序
        POKE,        // POKE 写入内存
        CHIPASM,     // CHIPASM 内联架构汇编
        STDCALL,     // __stdcall 调用约定
        DEFINT,      // DEFINT 整型声明
        DEFSNG,      // DEFSNG 单精度声明
        DEFSTR,      // DEFSTR 字符串声明
        DEFDBL,      // DEFDBL 双精度声明
        DEFLNG,      // DEFLNG 长整数声明
        LPRINT,      // LPRINT 打印机输出
        VIEW,        // VIEW 视口
        WINDOW,      // WINDOW 坐标系统
        MOD_KW,      // MOD 取模运算符
        
        // DATA/READ/RESTORE
        DATA,        // DATA 数据语句
        READ_KW,     // READ 读取数据 (避免与 Read 方法冲突)
        RESTORE,     // RESTORE 重置数据指针
        
        // CONST
        CONST_KW,    // CONST 常量声明
        
        // ON ERROR GOTO / RESUME
        ON,          // ON 错误处理
        ERROR_KW,    // ERROR 关键字
        RESUME,      // RESUME 恢复执行

        // QBASIC DRAW 宏
        DRAW_KW,     // DRAW

        // QBASIC PRINT USING
        USING_KW,    // USING

        // QBASIC TYPE/END TYPE
        TYPE_KW,      // TYPE

        // FreeBasic OOP (v1.66.31+)
        CLASS_KW,     // CLASS
        CONSTRUCTOR,  // CONSTRUCTOR
        DESTRUCTOR,   // DESTRUCTOR
        PROPERTY_KW,  // PROPERTY
        METHOD_KW,    // METHOD
        END_CLASS,    // END CLASS
        PTR_KW,       // PTR (指针)
        CAST_KW,      // CAST (类型转换)
        EXTENDS_KW,   // EXTENDS (继承)
        OPERATOR_KW,  // OPERATOR (运算符重载)
        ENUM_KW,      // ENUM (枚举)

        // PureBasic (v1.66.32+)
        PROCEDURE_KW,  // PROCEDURE
        GLOBAL_KW,     // GLOBAL
        PROTECTED_KW,  // PROTECTED
        INTERFACE_KW,  // INTERFACE
        ENDINTERFACE,  // ENDINTERFACE
        NEW_KW,        // NEW

        // ChipBasic MCU (v1.66.32+)
        PINMODE_KW,     // PINMODE
        DIGITALWRITE,   // DIGITALWRITE
        DIGITALREAD,    // DIGITALREAD

        // TrueBasic (v1.66.32+)
        MAT_KW,        // MAT (矩阵)
        ZER_KW,        // ZER
        CON_KW,        // CON

        // GW-BASIC (v1.66.32+)
        DEFSEG,        // DEF SEG
        BLOAD_KW,      // BLOAD
        BSAVE_KW,      // BSAVE
        KEY_KW,        // KEY

        // PowerBASIC (v1.66.32+)
        THREADED,      // THREADED
        REGISTER_KW,   // REGISTER
        FASTPROC,      // FASTPROC

        // 老程序兼容 —「接受并空转」的语句（本平台没有对应语义，见 `Parser.Core.cs` 那几条 case）
        PCOPY,         // PCOPY a, b          视频页复制（本平台不分页）
        SHELL,         // SHELL "cmd"         起子进程（本平台不提供）
        WRITE_KW,      // WRITE #n, a, b      带引号的输出（与 PRINT #n 同形）

        // VisualBasic (v1.66.32+)
        PRIVATE_KW,    // Private
        PUBLIC_KW,     // Public
        FRIEND_KW,     // Friend
        OPTIONAL_KW,   // Optional
        PARAMARRAY,    // ParamArray
        WITH_KW,       // With
        ENDWITH,       // End With
        REDIM_PRESERVE,// ReDim Preserve
        VB_INTEGER,    // Integer (VB type)
        VB_STRING,     // String (VB type)
        VB_OBJECT,     // Object (VB type)
        VB_VARIANT,    // Variant (VB type)

        // PLAY/SOUND
        PLAY_KW,     // PLAY 音乐播放
        SOUND_KW,    // SOUND 声音

        // REDIM/PRESERVE
        REDIM,       // REDIM 重新分配数组
        PRESERVE,    // PRESERVE 保留数组内容

        // Turbo Basic 扩展关键字
        LOCAL,       // LOCAL 局部变量声明
        STATIC_KW,   // STATIC 静态变量声明
        SHARED,      // SHARED 共享变量声明
        COMMON,      // COMMON 全局变量声明
        OPTION_KW,   // OPTION 选项（OPTION BASE n）

        // DEF FN
        DEF_KW,      // DEF (独立使用, 如 DEF FN)
        FN_KW,       // FN (DEF FN 中的函数名前缀)

        // DECLARE SUB/FUNCTION
        DECLARE,     // DECLARE
        NATIVE,      // NATIVE

        // GET/PUT graphics array
        GET_KW,      // GET
        PUT_KW,      // PUT

        // PALETTE
        PALETTE_KW,   // PALETTE index, red, green, blue
        // ASM_KW 已移除 — asm() 仅限 C/ObjC/C++ 语言使用，BASIC 通过 Lib/shared/vmlsys.c 调用系统功能
        // CHIPASM 保留 — 转译必需
        // CLS clear screen
        CLS_KW         // CLS
    }
}
