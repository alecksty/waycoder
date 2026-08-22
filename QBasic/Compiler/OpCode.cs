// QBasic/Compiler/OpCode.cs
// 字节码操作码枚举：定义栈式虚拟机支持的全部指令。
namespace QBasic.Compiler;

public enum OpCode : byte
{
    // 常量与变量
    Const,          // 压入常量（操作数：常量表索引）
    Load,           // 压入变量值（操作数：变量表索引）
    Store,          // 弹出值存入变量（操作数：变量表索引）
    LoadArray,      // 压入数组元素（操作数：变量表索引，栈顶为下标）
    StoreArray,     // 弹出值+下标存入数组元素（操作数：变量表索引）
    Dim,            // 声明数组（操作数：变量表索引，栈顶为长度）

    // 算术
    Add, Sub, Mul, Div, Mod,
    Neg,

    // 比较
    Eq, Ne, Lt, Le, Gt, Ge,

    // 逻辑
    And, Or, Not,

    // 字符串
    Concat,

    // 内置函数调用（操作数：函数表索引）
    Call,

    // 输出
    Print,          // 弹出并打印（不带换行）
    PrintLine,      // 弹出并打印（带换行）
    PrintNewLine,   // 仅换行
    Input,          // 输入到变量（操作数：变量表索引）

    // 控制流
    Jump,           // 无条件跳转（操作数：目标指令偏移）
    JumpIfFalse,    // 弹出，若为假则跳转
    Gosub,          // 调用子程序（操作数：目标偏移）
    Return,         // 从子程序返回
    End,            // 程序结束
    Halt,           // 停止执行（供测试）
}
