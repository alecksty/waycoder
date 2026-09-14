using VMLAssembler;

namespace VMLPlugins;

/// <summary>
/// VML 统一寄存器分配管理器 — 所有编译器通过 CodeGeneratorBase.Regs 使用。
/// 管理整数(R1-R11)、浮点(F0-F15)、双精度(D0-D7)三组寄存器池。
/// 池耗尽时自动溢出最旧的寄存器到 BP 相对栈槽。
/// </summary>
public class RegisterManager
{
    // ======== 寄存器常量 (VML ISA 约定) ========
    public const int R0  = 0;  // 累加器/返回值
    public const int R12 = 12; // BP 帧指针
    public const int R13 = 13; // SP 栈指针
    public const int R15 = 15; // RA 返回地址

    // ======== 整数寄存器池 (R1-R11) ========
    private readonly List<int> _freeInt = new();
    private readonly Stack<int> _spillStack = new();
    private readonly Dictionary<int, int> _regSpillSlot = new();
    private readonly Dictionary<int, int> _spillSlotReg = new();
    private int _spillOffset;

    // ======== 浮点寄存器池 (F0-F15) ========
    private readonly List<int> _freeFloat = new();
    private readonly Stack<int> _floatStack = new();

    // ======== 双精度寄存器池 (D0-D7) ========
    private readonly List<int> _freeDouble = new();

    public RegisterManager()
    {
        Reset();
    }

    /// <summary>剩余可分配整数寄存器</summary>
    public int FreeIntCount => _freeInt.Count;
    /// <summary>当前溢出槽数量</summary>
    public int SpillSlotsUsed => _spillOffset / 4;

    // ======== 整数寄存器分配 ========

    /// <summary>从池中分配一个整数寄存器。池空时溢出最旧的。</summary>
    public int AllocInt(List<Instruction> instructions)
    {
        if (_freeInt.Count > 0)
        {
            int reg = _freeInt[0];
            _freeInt.RemoveAt(0);
            _spillStack.Push(reg);
            return reg;
        }
        return SpillOldestInt(instructions);
    }

    /// <summary>释放整数寄存器回池</summary>
    public void FreeInt(int reg, List<Instruction> instructions)
    {
        RestoreSpill(reg, instructions);
        if (!_freeInt.Contains(reg))
            _freeInt.Add(reg);
        RemoveFromStack(reg);
    }

    /// <summary>确保指定寄存器可用（必要时溢出其当前值）</summary>
    public void ReserveInt(int reg, List<Instruction> instructions)
    {
        if (_freeInt.Contains(reg))
        {
            _freeInt.Remove(reg);
            _spillStack.Push(reg);
            return;
        }
        if (_regSpillSlot.ContainsKey(reg))
        {
            int slot = _regSpillSlot[reg];
            _regSpillSlot.Remove(reg);
            _spillSlotReg.Remove(slot);
        }
        else
        {
            SpillReg(reg, instructions);
        }
        _spillStack.Push(reg);
    }

    // ======== 浮点寄存器分配 ========

    public int AllocFloat(List<Instruction> instructions)
    {
        if (_freeFloat.Count > 0)
        {
            int reg = _freeFloat[0];
            _freeFloat.RemoveAt(0);
            _floatStack.Push(reg);
            return reg;
        }
        // 浮点寄存器池空时抛出（未来可加溢出）
        throw new InvalidOperationException("浮点寄存器池已空 (F0-F15 全部在用)");
    }

    public void FreeFloat(int reg, List<Instruction> instructions)
    {
        if (!_freeFloat.Contains(reg))
            _freeFloat.Add(reg);
        var temp = new Stack<int>();
        while (_floatStack.Count > 0 && _floatStack.Peek() != reg)
            temp.Push(_floatStack.Pop());
        if (_floatStack.Count > 0) _floatStack.Pop();
        while (temp.Count > 0) _floatStack.Push(temp.Pop());
    }

    // ======== 双精度寄存器分配 ========

    public int AllocDouble(List<Instruction> instructions)
    {
        if (_freeDouble.Count > 0)
        {
            int reg = _freeDouble[0];
            _freeDouble.RemoveAt(0);
            return reg;
        }
        throw new InvalidOperationException("双精度寄存器池已空 (D0-D7 全部在用)");
    }

    public void FreeDouble(int reg, List<Instruction> instructions)
    {
        if (!_freeDouble.Contains(reg))
            _freeDouble.Add(reg);
    }

    // ======== 函数调用前后保存/恢复 ========

    /// <summary>
    /// 函数调用前保存所有活跃寄存器。返回 (save, restore) 指令对。
    /// 调用者在 CALL 前插入 save，CALL 后插入 restore。
    /// </summary>
    public (List<Instruction> save, List<Instruction> restore) SaveForCall()
    {
        var save = new List<Instruction>();
        var restore = new List<Instruction>();

        var activeInts = new List<int>();
        foreach (int r in _spillStack)
            if (!activeInts.Contains(r)) activeInts.Add(r);

        foreach (int reg in activeInts)
        {
            int slot = NextSpillSlot();
            _regSpillSlot[reg] = slot;
            _spillSlotReg[slot] = reg;
            save.Add(new Instruction(OpCode.MOVE,
                new List<Operand> { Mem(R12, -slot), Op(reg) }));
            restore.Add(new Instruction(OpCode.MOVE,
                new List<Operand> { Op(reg), Mem(R12, -slot) }));
        }

        var activeFloats = new List<int>();
        foreach (int r in _floatStack)
            if (!activeFloats.Contains(r)) activeFloats.Add(r);

        foreach (int reg in activeFloats)
        {
            int slot = NextSpillSlot();
            save.Add(new Instruction(OpCode.MOVEF,
                new List<Operand> { Mem(R12, -slot), Op(reg) }));
            restore.Add(new Instruction(OpCode.MOVEF,
                new List<Operand> { Op(reg), Mem(R12, -slot) }));
        }

        return (save, restore);
    }

    // ======== 生命周期 ========

    /// <summary>重置管理器状态（函数/模块边界调用）</summary>
    public void Reset()
    {
        _freeInt.Clear();
        _freeInt.AddRange(new[] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 });
        _spillStack.Clear();
        _regSpillSlot.Clear();
        _spillSlotReg.Clear();
        _spillOffset = 0;

        _freeFloat.Clear();
        _freeFloat.AddRange(new[] { 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 });
        _floatStack.Clear();

        _freeDouble.Clear();
        _freeDouble.AddRange(new[] { 7, 6, 5, 4, 3, 2, 1, 0 });
    }

    /// <summary>获取当前活跃整数寄存器数量</summary>
    public int ActiveIntCount => _spillStack.Count;

    // ======== 内部 ========

    private int SpillOldestInt(List<Instruction> instructions)
    {
        if (_spillStack.Count == 0)
            throw new InvalidOperationException("整数寄存器池已空且无活跃寄存器可溢出");

        var all = _spillStack.ToArray();
        int oldest = all[^1];
        SpillReg(oldest, instructions);
        RemoveFromStack(oldest);
        return oldest;
    }

    private void SpillReg(int reg, List<Instruction> instructions)
    {
        int slot = NextSpillSlot();
        _regSpillSlot[reg] = slot;
        _spillSlotReg[slot] = reg;
        instructions.Add(new Instruction(OpCode.MOVE,
            new List<Operand> { Mem(R12, -slot), Op(reg) }));
    }

    private void RestoreSpill(int reg, List<Instruction> instructions)
    {
        if (_regSpillSlot.TryGetValue(reg, out int slot))
        {
            _regSpillSlot.Remove(reg);
            _spillSlotReg.Remove(slot);
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> { Op(reg), Mem(R12, -slot) }));
        }
    }

    private int NextSpillSlot()
    {
        _spillOffset += 4;
        return _spillOffset;
    }

    private void RemoveFromStack(int reg)
    {
        var temp = new Stack<int>();
        while (_spillStack.Count > 0 && _spillStack.Peek() != reg)
            temp.Push(_spillStack.Pop());
        if (_spillStack.Count > 0) _spillStack.Pop();
        while (temp.Count > 0) _spillStack.Push(temp.Pop());
    }

    // ======== Operand 快捷构造 ========

    private static Operand Op(int reg) =>
        new(OperandType.REGISTER, reg);

    private static Operand Mem(int baseReg, int offset) =>
        new(OperandType.MEMORY, $"{BaseRegName(baseReg)}{offset:+0;-#}");

    private static string BaseRegName(int reg) => reg switch
    {
        R12 => "R12",
        R13 => "R13",
        _ => $"R{reg}"
    };
}
