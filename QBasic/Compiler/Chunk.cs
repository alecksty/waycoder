// =============================================================
// Chunk.cs —— 字节码块（常量表 + 变量表 + 指令序列）
//
// Chunk 是编译产物：一串 OpCode（含操作数）+ 数字常量表 +
// 字符串常量表 + 变量表 + 与指令平行的行号表（用于运行时错误定位）。
// 跳转使用绝对指令地址，配合标签解析表在编译末尾回填。
// =============================================================

namespace QBasic.Compiler;

/// <summary>字节码操作码。操作数以紧随的短整型参数编码。</summary>
public enum OpCode : byte
{
    Nop = 0,
    ConstNum,      // [i16 constIdx] 推入数字常量
    ConstStr,      // [i16 constIdx] 推入字符串常量
    VarLoad,       // [i16 varIdx] 推入变量值
    VarStore,      // [i16 varIdx] 弹栈存到变量
    ArrLoad,       // [i16 varIdx] 弹下标，推入数组元素
    ArrStore,      // [i16 varIdx] 弹值、弹下标，写入数组
    Pop,           // 丢弃栈顶
    Add, Sub, Mul, Div, Mod, Neg,
    Concat,        // 字符串拼接
    Eq, Ne, Lt, Le, Gt, Ge,
    And, Or, Not,
    Call,          // [i16 funcIdx][i16 argc] 调用内置函数
    Jump,          // [i16 addr] 无条件跳转
    JumpIfFalse,   // [i16 addr] 弹条件，假则跳转
    JumpIfTrue,    // [i16 addr] 弹条件，真则跳转
    ForInit,       // [i16 varIdx][i16 limitIdx][i16 stepIdx] 初始化循环
    ForCheck,      // [i16 varIdx][i16 limitIdx][i16 stepIdx] 递增并检查
    Input,         // [i16 varIdx] 从输入读一个值
    Print,         // 无操作数，输出栈顶并换行
    PrintNoNl,     // 输出栈顶不换行
    PrintSemicolon,// 输出栈顶并接空格
    PrintComma,    // 输出栈顶并跳到下一制表位
    PrintNewline,  // 输出换行
    Gosub,         // [i16 addr] 调用子程序
    Return,        // 从子程序返回
    Halt,          // 程序结束
    Randomize,     // 无操作数，重置随机数种子
    Read,          // [i16 varIdx] 从 DATA 读取下一项存入变量
    Restore,       // 无操作数，重置 DATA 指针
    DimArray2,     // [i16 varIdx] 弹两个尺寸，分配二维数组
    Arr2Load,      // [i16 varIdx] 弹两个下标，推入二维数组元素
    Arr2Store,     // [i16 varIdx] 弹值、弹两个下标，写入二维数组
    // ---- 扩展：GORILLA.BAS ----
    Power,         // 幂运算
    Idiv,          // 整数除
    DimRange,      // [i16 varIdx] 弹 upper、lower，分配 lo TO hi 数组（含基址）
    PrintTab,      // [i16 col] 移到指定列（TAB）
    CallRoutine,   // [i16 addr] 调用 SUB/FUNCTION 例程（压返回地址）
    EndRoutine,    // 例程结束，返回调用方
    SetErrHandler, // [i16 addr] 设置 ON ERROR 目标
    ClearErrHandler, // 清除 ON ERROR
    Resume,        // [i16 mode] 0=RESUME 1=RESUME NEXT
    LineInput,     // [i16 varIdx] 读整行存入字符串变量
    // ---- 图形 / 交互 ----
    GfxScreen,     // 弹 mode，设置 SCREEN
    GfxCls,        // 清屏
    GfxLine,       // [i16 mode] 弹 color,y2,x2,y1,x1 画线/边框/填充
    GfxCircle,     // 弹 aspect,end,start,color,radius,y,x,hasAngles
    GfxPset,       // 弹 color,y,x
    GfxPaint,      // 弹 boundary,color,y,x 洪泛填充
    GfxLocate,     // 弹 col,row 设文本光标
    GfxColor,      // 弹 bg,fg 设前景背景
    GfxPalette,    // 弹 v,c 设调色板
    GfxGet,        // [i16 varIdx] 弹 y2,x2,y1,x1 存 sprite
    GfxPut,        // [i16 varIdx][i16 mode] 弹 y,x 画 sprite（0=PSET 1=XOR）
    GfxPlay,       // 弹音乐串，解析后忽略（可发出提示音）
    GfxSleep,      // 弹秒数，暂停
    GfxWidth,      // 弹列数，设 WIDTH
    Beep,          // 提示音
}

/// <summary>编译后的字节码块。</summary>
public sealed class Chunk
{
    /// <summary>指令编码：每字节一个 OpCode，操作数以小端 i16 紧随。</summary>
    public List<byte> Code = new();
    /// <summary>与 Code 指令一一对应的行号。</summary>
    public List<int> Lines = new();
    /// <summary>数字常量表。</summary>
    public List<double> ConstNums = new();
    /// <summary>字符串常量表。</summary>
    public List<string> ConstStrs = new();
    /// <summary>变量名表（含隐藏循环变量）。</summary>
    public List<string> VarNames = new();
    /// <summary>哪些变量是数组。</summary>
    public HashSet<int> ArrayVars = new();
    /// <summary>函数名表。</summary>
    public List<string> FuncNames = new();
    /// <summary>DATA 数据表（解析器收集，供 READ/RESTORE 使用）。</summary>
    public List<DataItem> Data = new();

    public int Ip { get; set; }   // 下一指令偏移（用于回填）

    public int AddConstNum(double d)
    {
        ConstNums.Add(d);
        return ConstNums.Count - 1;
    }

    public int AddConstStr(string s)
    {
        ConstStrs.Add(s);
        return ConstStrs.Count - 1;
    }

    /// <summary>按名获取变量索引，不存在则创建。</summary>
    public int ResolveVar(string name)
    {
        for (int i = 0; i < VarNames.Count; i++)
            if (string.Equals(VarNames[i], name, StringComparison.OrdinalIgnoreCase)) return i;
        VarNames.Add(name);
        return VarNames.Count - 1;
    }

    public int ResolveFunc(string name)
    {
        for (int i = 0; i < FuncNames.Count; i++)
            if (string.Equals(FuncNames[i], name, StringComparison.Ordinal)) return i;
        FuncNames.Add(name);
        return FuncNames.Count - 1;
    }

    /// <summary>发射一条无操作数字令。</summary>
    public void Emit(OpCode op, int line)
    {
        Code.Add((byte)op);
        Lines.Add(line);
        Ip++;
    }

    /// <summary>发射带一个 i16 操作数的指令。</summary>
    public void Emit(OpCode op, int operand, int line)
    {
        Code.Add((byte)op);
        Lines.Add(line);
        Code.Add((byte)(operand & 0xFF));
        Code.Add((byte)((operand >> 8) & 0xFF));
        Lines.Add(line); Lines.Add(line);
        Ip += 3;
    }

    /// <summary>发射 Call 指令（两个 i16 操作数：函数索引 + 实参数）。</summary>
    public void EmitCall(int funcIdx, int argc, int line)
    {
        Code.Add((byte)OpCode.Call);
        Lines.Add(line);
        Code.Add((byte)(funcIdx & 0xFF));
        Code.Add((byte)((funcIdx >> 8) & 0xFF));
        Code.Add((byte)(argc & 0xFF));
        Code.Add((byte)((argc >> 8) & 0xFF));
        Lines.Add(line); Lines.Add(line); Lines.Add(line); Lines.Add(line);
        Ip += 5;
    }

    /// <summary>发射带三个 i16 操作数的指令（如 ForCheck）。</summary>
    public void Emit3(OpCode op, int op1, int op2, int op3, int line)
    {
        Code.Add((byte)op);
        Lines.Add(line);
        Code.Add((byte)(op1 & 0xFF)); Code.Add((byte)((op1 >> 8) & 0xFF));
        Code.Add((byte)(op2 & 0xFF)); Code.Add((byte)((op2 >> 8) & 0xFF));
        Code.Add((byte)(op3 & 0xFF)); Code.Add((byte)((op3 >> 8) & 0xFF));
        for (int i = 0; i < 6; i++) Lines.Add(line);
        Ip += 7;
    }

    /// <summary>回填 offset 处指令的操作数（k 表示第几个操作数，0 起）。</summary>
    public void PatchOperand3(int instrOffset, int k, int value)
    {
        int baseOff = instrOffset + 1 + k * 2;
        Code[baseOff] = (byte)(value & 0xFF);
        Code[baseOff + 1] = (byte)((value >> 8) & 0xFF);
    }

    /// <summary>读取指令偏移处的 OpCode。</summary>
    public OpCode ReadOp(int offset) => (OpCode)Code[offset];

    /// <summary>读取 offset 处的 i16 操作数。</summary>
    public int ReadOperand(int offset) => Code[offset] | (Code[offset + 1] << 8);

    /// <summary>回填 offset 处指令的操作数。</summary>
    public void PatchOperand(int operandOffset, int value)
    {
        Code[operandOffset] = (byte)(value & 0xFF);
        Code[operandOffset + 1] = (byte)((value >> 8) & 0xFF);
    }

    /// <summary>当前指令地址。</summary>
    public int Address => Ip;
}
