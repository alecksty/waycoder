using VMLAssembler;
using System.IO;
using VMLRuntime.Device;

namespace VMLRuntime
{
    public partial class VmRuntime
    {
        /// <summary>
        /// 触发浮点异常
        /// </summary>
        /// <param name="exceptionType">异常类型：invalid, zero, underflow, overflow, precision</param>
        private void TriggerFloatException(string exceptionType)
        {
            fes = true; // 设置异常状态

            // 根据异常类型设置相应的标志位
            switch (exceptionType)
            {
                case "invalid":
                    fif = true;
                    if (fie)
                        throw new VmlFloatException("浮点无效操作异常");
                    break;
                case "zero":
                    fdf = true;
                    if (fze)
                        throw new VmlFloatException("浮点除零异常");
                    break;
                case "underflow":
                    fuf = true;
                    if (fde)
                        throw new VmlFloatException("浮点下溢异常");
                    break;
                case "overflow":
                    fof = true;
                    if (foe)
                        throw new VmlFloatException("浮点溢出异常");
                    break;
                case "precision":
                    fpf = true;
                    if (fpe)
                        throw new VmlFloatException("浮点精度异常");
                    break;
            }
        }

        /// <summary>
        /// 清除浮点异常标志
        /// </summary>
        private void ClearFloatExceptions()
        {
            fzf = false;
            fsf = false;
            fcf = false;
            fof = false;
            fuf = false;
            fdf = false;
            fif = false;
            fes = false;
        }

        /// <summary>
        /// 设置浮点标志位
        /// </summary>
        /// <param name="value">浮点值</param>
        private void SetFloatFlags(float value)
        {
            fzf = (Math.Abs(value) < float.Epsilon);
            fsf = (value < 0);
            fcf = false; // 浮点进位标志通常用于比较
        }

        /// <summary>
        /// 设置双精度浮点标志位
        /// </summary>
        /// <param name="value">双精度浮点值</param>
        private void SetDoubleFlags(double value)
        {
            fzf = (Math.Abs(value) < double.Epsilon);
            fsf = (value < 0);
            fcf = false;
        }

        private float GetFloatValue(Operand operand)
        {
            if (operand.Type == OperandType.IMMEDIATE)
            {
                // 立即数：支持整数和浮点字面量
                if (operand.Value is float f)
                    return f;
                if (operand.Value is int i)
                    return (float)i;
                if (operand.Value is double d)
                    return (float)d;
                return Convert.ToSingle(operand.Value);
            }
            else if (operand.Type == OperandType.REGISTER)
            {
                int regNum = (int)operand.Value;
                if (regNum < 16)
                    return floatRegisters[regNum];
                else if (regNum >= 16 && regNum < 24) // D0-D7 (16-23)
                    return (float)doubleRegisters[regNum - 16];
                else
                    throw new VmlFloatException($"无效的浮点寄存器：F{regNum} 或 D{regNum-16}");
            }
            else if (operand.Type == OperandType.MEMORY)
            {
                int addr = GetMemoryAddress(operand);
                return GetFloatFromMemory(addr);
            }
            else if (operand.Type == OperandType.INDIRECT)
            {
                int addr = GetAddress(operand);
                return GetFloatFromMemory(addr);
            }
            else if (operand.Type == OperandType.LABEL)
            {
                // 浮点常量标签
                string label = operand.Value.ToString();
                if (floatConstants.ContainsKey(label))
                    return (float)floatConstants[label];
                if (labelAddresses.ContainsKey(label))
                    return GetFloatFromMemory(labelAddresses[label]);
                throw new VmlFloatException($"FLOAD: 未知浮点常量标签 '{label}'");
            }

            throw new VmlFloatException($"不支持的浮点操作数类型：{operand.Type}");
        }

        private double GetDoubleValue(Operand operand)
        {
            if (operand.Type == OperandType.IMMEDIATE)
            {
                // 立即数：支持整数和浮点字面量
                if (operand.Value is double d)
                    return d;
                if (operand.Value is float f)
                    return (double)f;
                if (operand.Value is int i)
                    return (double)i;
                return Convert.ToDouble(operand.Value);
            }
            else if (operand.Type == OperandType.REGISTER)
            {
                int regNum = (int)operand.Value;
                if (regNum >= 16 && regNum < 24) // D0-D7 (16-23) — 显式 Dx 寄存器
                    return doubleRegisters[regNum - 16];
                else if (regNum < 8) // R0-R7 → D0-D7 (C 编译器输出 R0 表示双精度寄存器)
                    return doubleRegisters[regNum];
                else if (regNum < 16) // R8-R15 → F8-F15 → float→double 自动提升
                    return (double)floatRegisters[regNum];
                else
                    throw new VmlFloatException($"无效的双精度寄存器：D{regNum-16} 或 F{regNum}");
            }
            else if (operand.Type == OperandType.MEMORY)
            {
                int addr = GetMemoryAddress(operand);
                return GetDoubleFromMemory(addr);
            }
            else if (operand.Type == OperandType.INDIRECT)
            {
                int addr = GetAddress(operand);
                return GetDoubleFromMemory(addr);
            }
            else if (operand.Type == OperandType.LABEL)
            {
                // 双精度常量标签
                string label = operand.Value.ToString();
                if (labelAddresses.ContainsKey(label))
                    return GetDoubleFromMemory(labelAddresses[label]);
                throw new VmlFloatException($"DLOAD: 未知双精度常量标签 '{label}'");
            }

            throw new VmlFloatException($"不支持的双精度操作数类型：{operand.Type}");
        }

        private void SetFloatValue(Operand operand, float value)
        {
            if (operand.Type == OperandType.REGISTER)
            {
                int regNum = (int)operand.Value;
                if (regNum < 16)
                {
                    floatRegisters[regNum] = value;
                    // 同步整数寄存器 (v1.66.66): PUSH/POP 等指令读取 registers[]，
                    // 未同步会导致 MOVEF R0 + PUSH R0 时压入垃圾值
                    registers[regNum] = BitConverter.SingleToInt32Bits(value);
                }
                else if (regNum >= 16 && regNum < 24) // D0-D7 (16-23)
                    doubleRegisters[regNum - 16] = (double)value;
                else
                    throw new VmlFloatException($"无效的浮点寄存器：F{regNum} 或 D{regNum-16}");
            }
            else if (operand.Type == OperandType.MEMORY)
            {
                int addr = GetMemoryAddress(operand);
                SetFloatToMemory(addr, value);
            }
            else if (operand.Type == OperandType.INDIRECT)
            {
                int addr = GetAddress(operand);
                SetFloatToMemory(addr, value);
            }
            else
            {
                throw new VmlFloatException($"不支持的浮点目标操作数类型：{operand.Type}");
            }
        }

        private void SetDoubleValue(Operand operand, double value)
        {
            if (operand.Type == OperandType.REGISTER)
            {
                int regNum = (int)operand.Value;
                if (regNum >= 16 && regNum < 24) // D0-D7 (16-23) — 显式 Dx 寄存器
                    doubleRegisters[regNum - 16] = value;
                else if (regNum < 8) // R0-R7 → D0-D7 (C 编译器输出 R0 表示双精度寄存器)
                {
                    doubleRegisters[regNum] = value;
                    // 同步整数寄存器低位 (v1.66.66): 64-bit→32-bit 截断，
                    // 使 PUSH R0 至少压入有意义的位模式而非垃圾
                    long bits = BitConverter.DoubleToInt64Bits(value);
                    registers[regNum] = (int)(bits & 0xFFFFFFFF);
                }
                else if (regNum < 16) // R8-R15 → F8-F15 → float 截断存储
                    floatRegisters[regNum] = (float)value;
                else
                    throw new VmlFloatException($"无效的双精度寄存器：D{regNum-16} 或 F{regNum}");
            }
            else if (operand.Type == OperandType.MEMORY)
            {
                int addr = GetMemoryAddress(operand);
                SetDoubleToMemory(addr, value);
            }
            else if (operand.Type == OperandType.INDIRECT)
            {
                int addr = GetAddress(operand);
                SetDoubleToMemory(addr, value);
            }
            else
            {
                throw new VmlFloatException($"不支持的双精度目标操作数类型：{operand.Type}");
            }
        }

        private void CheckFloatMemAccess(int address, int size)
        {
            if (address < 0 || address + size > memory.Length)
                throw new VmlMemoryException($"浮点内存访问越界：{address:X8}", (uint)address);
        }

        private float GetFloatFromMemory(int address)
        {
            CheckFloatMemAccess(address, 4);
            return BitConverter.ToSingle(memory, address);
        }

        private void SetFloatToMemory(int address, float value)
        {
            CheckFloatMemAccess(address, 4);
            BitConverter.TryWriteBytes(memory.AsSpan(address, 4), value);
        }

        private double GetDoubleFromMemory(int address)
        {
            CheckFloatMemAccess(address, 8);
            return BitConverter.ToDouble(memory, address);
        }

        private void SetDoubleToMemory(int address, double value)
        {
            CheckFloatMemAccess(address, 8);
            BitConverter.TryWriteBytes(memory.AsSpan(address, 8), value);
        }

        private int GetMemoryAddress(Operand operand)
        {
            if (operand.Type == OperandType.MEMORY)
            {
                var val = operand.Value;
                if (val is DecodedMemAddr dma)
                    return ResolveDecodedAddr(dma);
                if (val is int addr)
                    return addr;
                if (val is string s)
                {
                    // AT&T 语法: -4(R12) 或 (R12)
                    int paren = s.IndexOf('(');
                    if (paren >= 0 && s.EndsWith(")"))
                    {
                        string offsetPart = paren > 0 ? s.Substring(0, paren).Trim() : "0";
                        string regPart = s.Substring(paren + 1).TrimEnd(')').Trim();
                        if (regPart.Length >= 2 && regPart[0] == 'R' && char.IsDigit(regPart[1]))
                        {
                            int ri = 1;
                            while (ri < regPart.Length && char.IsDigit(regPart[ri])) ri++;
                            int regNum = int.Parse(regPart.Substring(1, ri - 1));
                            int offset = int.Parse(offsetPart);
                            return registers[regNum] + offset;
                        }
                    }
                    // 格式: "R<N>-<offset>" 或 "R<N>+<offset>" 或 "R<N>"
                    if (s.Length >= 2 && s[0] == 'R' && char.IsDigit(s[1]))
                    {
                        int i = 1;
                        while (i < s.Length && char.IsDigit(s[i])) i++;
                        if (i == s.Length && i > 1)
                        {
                            int regNum = int.Parse(s.Substring(1));
                            return registers[regNum];
                        }
                        if (i > 1 && i < s.Length && (s[i] == '-' || s[i] == '+'))
                        {
                            int regNum = int.Parse(s.Substring(1, i - 1));
                            int offset = int.Parse(s.Substring(i + 1));
                            return registers[regNum] + (s[i] == '-' ? -offset : offset);
                        }
                    }
                    // 裸地址：尝试解析为整数，或标签
                    if (int.TryParse(s, out int directAddr))
                        return directAddr;
                    if (labelAddresses != null && labelAddresses.ContainsKey(s))
                        return labelAddresses[s];
                }
            }
            throw new VmlException($"无法获取内存地址：{operand.Value}");
        }

        /// <summary>
        /// 获取 INDIRECT 操作数对应的内存地址
        /// </summary>
        private int GetIndirectAddress(Operand operand)
        {
            if (operand.Value is int regIdx && regIdx >= 0 && regIdx < 16)
                return registers[regIdx];
            if (operand.Value is string s)
            {
                // 格式: "R<N>+<offset>" 或 "R<N>-<offset>" 或 "R<N>"
                if (s.Length >= 2 && s[0] == 'R' && char.IsDigit(s[1]))
                {
                    int i = 1;
                    while (i < s.Length && char.IsDigit(s[i])) i++;
                    int regNum = int.Parse(s.Substring(1, i - 1));
                    int offset = 0;
                    if (i < s.Length && (s[i] == '+' || s[i] == '-'))
                    {
                        offset = int.Parse(s.Substring(i + 1));
                        if (s[i] == '-') offset = -offset;
                    }
                    return registers[regNum] + offset;
                }
            }
            return 0;
        }

        private void ExecuteFadd(List<Operand> operands)
        {
            // FADD dst, src1, src2
            if (operands.Count < 3) return;
            float src1 = GetFloatValue(operands[1]);
            float src2 = GetFloatValue(operands[2]);
            float result = src1 + src2;
            
            // 检查溢出
            if (float.IsInfinity(result) && !float.IsInfinity(src1) && !float.IsInfinity(src2))
                TriggerFloatException("overflow");
            
            // 检查下溢（结果太小）
            if (Math.Abs(result) < float.Epsilon && Math.Abs(src1) > float.Epsilon && Math.Abs(src2) > float.Epsilon)
                TriggerFloatException("underflow");
            
            SetFloatValue(operands[0], result);
            SetFloatFlags(result);
        }

        private void ExecuteFsub(List<Operand> operands)
        {
            // FSUB dst, src1, src2
            if (operands.Count < 3) return;
            float src1 = GetFloatValue(operands[1]);
            float src2 = GetFloatValue(operands[2]);
            float result = src1 - src2;
            
            // 检查溢出
            if (float.IsInfinity(result) && !float.IsInfinity(src1) && !float.IsInfinity(src2))
                TriggerFloatException("overflow");
            
            // 检查下溢
            if (Math.Abs(result) < float.Epsilon && Math.Abs(src1) > float.Epsilon && Math.Abs(src2) > float.Epsilon)
                TriggerFloatException("underflow");
            
            SetFloatValue(operands[0], result);
            SetFloatFlags(result);
        }

        private void ExecuteFmul(List<Operand> operands)
        {
            // FMUL dst, src1, src2
            if (operands.Count < 3) return;
            float src1 = GetFloatValue(operands[1]);
            float src2 = GetFloatValue(operands[2]);
            float result = src1 * src2;
            
            // 检查无效操作（NaN * 0 或 Infinity * 0）
            if (float.IsNaN(result) || (float.IsInfinity(src1) && src2 == 0) || (src1 == 0 && float.IsInfinity(src2)))
                TriggerFloatException("invalid");
            
            // 检查溢出
            if (float.IsInfinity(result) && !float.IsInfinity(src1) && !float.IsInfinity(src2))
                TriggerFloatException("overflow");
            
            // 检查下溢
            if (Math.Abs(result) < float.Epsilon && Math.Abs(src1) > float.Epsilon && Math.Abs(src2) > float.Epsilon)
                TriggerFloatException("underflow");
            
            SetFloatValue(operands[0], result);
            SetFloatFlags(result);
        }

        private float FSrc(List<Operand> ops, int i) => ops.Count > i ? GetFloatValue(ops[i]) : GetFloatValue(ops[0]);

        private void ExecuteFdiv(List<Operand> operands)
        {
            float src1 = FSrc(operands, 1);
            float src2 = FSrc(operands, 2);
            
            // 检查除零
            if (src2 == 0.0f)
            {
                TriggerFloatException("zero");
                if (!fze) // 如果除零异常未启用，返回Infinity或NaN
                {
                    SetFloatValue(operands[0], float.PositiveInfinity * Math.Sign(src1));
                    SetFloatFlags(float.PositiveInfinity);
                    return;
                }
            }
            
            float result = src1 / src2;
            
            // 检查无效操作（0/0 或 Infinity/Infinity）
            if (float.IsNaN(result))
                TriggerFloatException("invalid");
            
            // 检查溢出
            if (float.IsInfinity(result) && !float.IsInfinity(src1) && !float.IsInfinity(src2))
                TriggerFloatException("overflow");
            
            SetFloatValue(operands[0], result);
            SetFloatFlags(result);
        }

        private void ExecuteFneg(List<Operand> operands)
        {
            // FNEG dst
            float val = GetFloatValue(operands[0]);
            float result = -val;
            
            // 检查无效操作（NaN取负）
            if (float.IsNaN(val))
                TriggerFloatException("invalid");
            
            SetFloatValue(operands[0], result);
            SetFloatFlags(result);
        }

        private void ExecuteFcmp(List<Operand> operands)
        {
            // FCMP src1, src2 - 设置浮点比较结果
            float src1 = GetFloatValue(operands[0]);
            float src2 = GetFloatValue(operands[1]);

            // 检查无效操作（NaN比较）
            if (float.IsNaN(src1) || float.IsNaN(src2))
            {
                TriggerFloatException("invalid");
                floatCmpResult = 2; // 无序（unordered）
                fcf = true; // 设置浮点进位标志表示无序
            }
            else if (src1 < src2)
            {
                floatCmpResult = -1;
                fcf = false;
            }
            else if (src1 > src2)
            {
                floatCmpResult = 1;
                fcf = false;
            }
            else
            {
                floatCmpResult = 0;
                fcf = false;
            }

            // 设置浮点标志 (epsilon比较，与DCMP一致)
            fzf = (Math.Abs(src1 - src2) < float.Epsilon);
            fsf = (floatCmpResult < 0);

            // 同时设置整数标志（用于条件跳转）—— `JL/JLE/JG/JGE` 读的就是这三个
            //   （本 ISA **没有**浮点专用跳转）。`cf` 原来漏搬 ⇒ `sf != cf` 里的 `cf`
            //   还是上一条指令的残留，四个有序跳转全靠 `sf` 恰好对才碰巧成立。
            //   这里照搬 `fcf`（有序 = false、NaN = true，与上面几个分支同源）。
            zf = fzf;
            sf = fsf;
            cf = fcf;
        }

        private void ExecuteI2f(List<Operand> operands)
        {
            // I2F dst, src - 整数转浮点
            int intVal;
            if (operands[1].Type == OperandType.REGISTER)
            {
                int regNum = (int)operands[1].Value;
                intVal = registers[regNum];
            }
            else
            {
                intVal = Convert.ToInt32(operands[1].Value);
            }

            SetFloatValue(operands[0], (float)intVal);
        }

        private void ExecuteF2i(List<Operand> operands)
        {
            // F2I dst, src - 浮点转整数（截断）
            float floatVal = GetFloatValue(operands[1]);
            int   intVal   = (int)Math.Truncate(floatVal);

            if (operands[0].Type == OperandType.REGISTER)
            {
                int regNum = (int)operands[0].Value;
                registers[regNum] = intVal;
            }
            else
            {
                throw new VmlFloatException($"F2I不支持的目标类型：{operands[0].Type}");
            }
        }

        private void ExecuteF2d(List<Operand> operands)
        {
            // F2D dst, src - 单精度转双精度
            float  floatVal  = GetFloatValue(operands[1]);
            double doubleVal = (double)floatVal;

            SetDoubleValue(operands[0], doubleVal);
        }

        private void ExecuteD2f(List<Operand> operands)
        {
            // D2F dst, src - 双精度转单精度
            double doubleVal = GetDoubleValue(operands[1]);

            SetFloatValue(operands[0], (float)doubleVal);
        }

        private void ExecuteI2d(List<Operand> operands)
        {
            // I2D dst, src - 整数转双精度浮点
            int intVal;
            if (operands[1].Type == OperandType.REGISTER)
            {
                int regNum = (int)operands[1].Value;
                intVal = registers[regNum];
            }
            else
            {
                intVal = Convert.ToInt32(operands[1].Value);
            }

            SetDoubleValue(operands[0], (double)intVal);
        }

        private void ExecuteD2i(List<Operand> operands)
        {
            // D2I dst, src - 双精度浮点转整数（截断）
            double doubleVal = GetDoubleValue(operands[1]);
            int intVal = (int)Math.Truncate(doubleVal);

            if (operands[0].Type == OperandType.REGISTER)
            {
                int regNum = (int)operands[0].Value;
                registers[regNum] = intVal;
            }
            else
            {
                throw new VmlFloatException($"D2I不支持的目标类型：{operands[0].Type}");
            }
        }


        /// <summary>
        /// 执行MOVEF — 统一 FLOAD/FSTORE（浮点移动）
        /// </summary>
        private void ExecuteMoveF(List<Operand> operands)
        {
            var dest = operands[0];
            var src = operands[1];
            float val = GetFloatValue(src);
            SetFloatValue(dest, val);
        }

        /// <summary>
        /// 执行MOVEL — 64位长整数移动
        /// </summary>
        private void ExecuteMoveL(List<Operand> operands)
        {
            var dest = operands[0];
            var src = operands[1];
            long val = GetLongValue(src);
            SetLongValue(dest, val);
        }

        /// <summary>
        /// 执行MOVED — 统一 DLOAD/DSTORE（双精度移动）
        /// </summary>
        private void ExecuteMoveD(List<Operand> operands)
        {
            var dest = operands[0];
            var src = operands[1];
            // MOVED 使用 dest-first 格式（与 MOVE/MOVEF 一致）
            double val = GetDoubleValue(src);
            SetDoubleValue(dest, val);
        }

        private void ExecuteFpush(List<Operand> operands)
        {
            // FPUSH src - 浮点数压栈
            float val = GetFloatValue(operands[0]);
            floatStack.Push(val);
        }

        private void ExecuteFpop(List<Operand> operands)
        {
            // FPOP dst - 浮点数弹栈
            if (floatStack.Count == 0)
                throw new VmlFloatException("浮点栈下溢");

            float val = floatStack.Pop();
            SetFloatValue(operands[0], val);
        }

        // ====== 双精度 D 指令实现 ======

        private void ExecuteDadd(List<Operand> operands)
        {
            double src1 = GetDoubleValue(operands.Count > 2 ? operands[1] : operands[0]);
            double src2 = GetDoubleValue(operands.Count > 1 ? operands[^1] : operands[0]);
            double result = src1 + src2;
            if (double.IsInfinity(result) && !double.IsInfinity(src1) && !double.IsInfinity(src2))
                TriggerFloatException("overflow");
            if (Math.Abs(result) < double.Epsilon && Math.Abs(src1) > double.Epsilon && Math.Abs(src2) > double.Epsilon)
                TriggerFloatException("underflow");
            SetDoubleValue(operands[0], result);
            SetDoubleFlags(result);
        }

        private void ExecuteDsub(List<Operand> operands)
        {
            double src1 = GetDoubleValue(operands.Count > 2 ? operands[1] : operands[0]);
            double src2 = GetDoubleValue(operands.Count > 1 ? operands[^1] : operands[0]);
            double result = src1 - src2;
            if (double.IsInfinity(result) && !double.IsInfinity(src1) && !double.IsInfinity(src2))
                TriggerFloatException("overflow");
            if (Math.Abs(result) < double.Epsilon && Math.Abs(src1) > double.Epsilon && Math.Abs(src2) > double.Epsilon)
                TriggerFloatException("underflow");
            SetDoubleValue(operands[0], result);
            SetDoubleFlags(result);
        }

        private void ExecuteDmul(List<Operand> operands)
        {
            double src1 = GetDoubleValue(operands.Count > 2 ? operands[1] : operands[0]);
            double src2 = GetDoubleValue(operands.Count > 1 ? operands[^1] : operands[0]);
            double result = src1 * src2;
            if (double.IsNaN(result) || (double.IsInfinity(src1) && src2 == 0) || (src1 == 0 && double.IsInfinity(src2)))
                TriggerFloatException("invalid");
            if (double.IsInfinity(result) && !double.IsInfinity(src1) && !double.IsInfinity(src2))
                TriggerFloatException("overflow");
            if (Math.Abs(result) < double.Epsilon && Math.Abs(src1) > double.Epsilon && Math.Abs(src2) > double.Epsilon)
                TriggerFloatException("underflow");
            SetDoubleValue(operands[0], result);
            SetDoubleFlags(result);
        }

        private void ExecuteDdiv(List<Operand> operands)
        {
            double src1 = GetDoubleValue(operands.Count > 2 ? operands[1] : operands[0]);
            double src2 = GetDoubleValue(operands.Count > 1 ? operands[^1] : operands[0]);
            if (src2 == 0.0)
            {
                TriggerFloatException("zero");
                if (!fze)
                {
                    SetDoubleValue(operands[0], double.PositiveInfinity * Math.Sign(src1));
                    SetDoubleFlags(double.PositiveInfinity);
                    return;
                }
            }
            double result = src1 / src2;
            if (double.IsNaN(result))
                TriggerFloatException("invalid");
            if (double.IsInfinity(result) && !double.IsInfinity(src1) && !double.IsInfinity(src2))
                TriggerFloatException("overflow");
            SetDoubleValue(operands[0], result);
            SetDoubleFlags(result);
        }

        private void ExecuteDneg(List<Operand> operands)
        {
            double val = GetDoubleValue(operands[0]);
            double result = -val;
            SetDoubleValue(operands[0], result);
            SetDoubleFlags(result);
        }

        private void ExecuteDcmp(List<Operand> operands)
        {
            double src1 = GetDoubleValue(operands[0]);
            double src2 = GetDoubleValue(operands[1]);
            if (double.IsNaN(src1) || double.IsNaN(src2))
            {
                TriggerFloatException("invalid");
                fzf = false; fsf = false; fcf = true;
                zf = false; sf = false; cf = true;   // 无序：按"小于"（与 Fcmp 同）
                return;
            }
            fzf = (Math.Abs(src1 - src2) < double.Epsilon);
            fsf = (src1 < src2);
            fcf = (src1 > src2);

            // ⚠ **同时设置整数标志**（条件跳转读的是它们）——与 `ExecuteFcmp` 同一处置。
            //
            //   本 ISA **没有**浮点专用的跳转指令：`JL/JLE/JG/JGE/JE/JNE` 读的都是
            //   `zf/sf/cf`，所以比较完之后**必须把浮点标志搬过去**。
            //   `Fcmp` 搬了（见那里的"同时设置整数标志"）、`Dcmp` 原来**没搬** ⇒
            //   `dcmp` 之后的跳转读的是**上一条整数比较留下的** `zf/sf/cf`：
            //   结果时对时错、还取决于上一条指令是什么 —— 最难查的那种。
            //
            //   实测（`.scratch/gor/mre_dcmp.bas`）：同一段 `A# = 1.5 + 2.25` 之后
            //   `IF A# > 3.5`，顶层判对（GT）、SUB 里判错（LE），而**两边生成的 dcmp
            //   操作数顺序完全一样** —— 差别只在"前一条指令留下的整数标志"。
            //
            //   `cf` 取 **false**（有序比较没有"借位"）—— **不要**照搬本函数的 `fcf`：
            //   那个的含义是"第一个操作数更大"（见上面 `fcf = (src1 > src2)`），
            //   与 `Fcmp` 里 `fcf` = "无序" 不是一回事。取错这一位症状极有指向性：
            //   四个有序跳转一起反过来（3.75 > 3.5 被判成"小于等于"）。
            zf = fzf;
            sf = fsf;
            cf = false;
        }

        private void ExecuteDpush(List<Operand> operands)
        {
            double val = 0.0;
            try
            {
                val = GetDoubleValue(operands[0]);
            }
            catch (Exception)
            {
                Console.Error.WriteLine($"DPUSH fallback: type={operands[0].Type} valType={operands[0].Value?.GetType()}");
                object v = operands[0].Value;
                if (v is int iv) val = (double)iv;
                else if (v is DecodedMemAddr dm) val = (double)registers[dm.BaseReg];
                else val = 0.0;
            }
            doubleStack.Push(val);
        }

        private void ExecuteDpop(List<Operand> operands)
        {
            if (doubleStack.Count == 0)
                throw new VmlFloatException("双精度栈下溢");
            double val = doubleStack.Pop();
            SetDoubleValue(operands[0], val);
        }

        // ======== 64位长整数操作 ========

        /// <summary>
        /// 获取64位长整数值
        /// </summary>
        private long GetLongValue(Operand operand)
        {
            if (operand.Type == OperandType.IMMEDIATE)
            {
                if (operand.Value is long l) return l;
                if (operand.Value is int i) return (long)i;
                if (operand.Value is double d) return (long)d;
                if (operand.Value is float f) return (long)f;
                return Convert.ToInt64(operand.Value);
            }
            else if (operand.Type == OperandType.REGISTER)
            {
                int regNum = (int)operand.Value;
                if (regNum >= 24 && regNum < 32) // L0-L7 (24-31) — 显式 Lx 寄存器
                    return longRegisters[regNum - 24];
                if (regNum >= 16 && regNum < 24) // D0-D7 → 按64位整数读取(兼容旧编码)
                    return BitConverter.DoubleToInt64Bits(doubleRegisters[regNum - 16]);
                else if (regNum < 8) // R0-R7 → L0-L7 (C 编译器输出 R0 表示长整数寄存器)
                    return longRegisters[regNum];
                else if (regNum < 16) // R8-R15 → 32位零扩展
                    return (long)(uint)registers[regNum];
                throw new VmlFloatException($"无效的长整数寄存器：L{regNum}");
            }
            else if (operand.Type == OperandType.MEMORY)
            {
                int addr = GetMemoryAddress(operand);
                long low = (uint)GetMemory(addr);
                long high = (uint)GetMemory(addr + 4);
                return (high << 32) | low;
            }
            else if (operand.Type == OperandType.INDIRECT)
            {
                int addr = GetIndirectAddress(operand);
                long low = (uint)GetMemory(addr);
                long high = (uint)GetMemory(addr + 4);
                return (high << 32) | low;
            }
            else if (operand.Type == OperandType.LABEL)
            {
                // 与 GetFloatValue/GetDoubleValue 一致: 查标签地址, 读取内存中的值
                string label = operand.Value.ToString();
                if (labelAddresses.ContainsKey(label))
                {
                    int addr = labelAddresses[label];
                    long low = (uint)GetMemory(addr);
                    long high = (uint)GetMemory(addr + 4);
                    return (high << 32) | low;
                }
                throw new VmlFloatException($"获取长整数: 未知标签 '{label}'");
            }
            throw new VmlFloatException($"不支持的长整数操作数类型：{operand.Type}");
        }

        /// <summary>
        /// 设置64位长整数值
        /// </summary>
        private void SetLongValue(Operand operand, long value)
        {
            if (operand.Type == OperandType.REGISTER)
            {
                int regNum = (int)operand.Value;
                if (regNum >= 24 && regNum < 32) // L0-L7 (24-31) — 显式 Lx 寄存器
                {
                    longRegisters[regNum - 24] = value;
                }
                else if (regNum >= 16 && regNum < 24) // D0-D7 → 按64位整数存储(兼容旧编码)
                    doubleRegisters[regNum - 16] = BitConverter.Int64BitsToDouble(value);
                else if (regNum < 8) // R0-R7 → L0-L7 (C 编译器输出 R0 表示长整数寄存器)
                {
                    longRegisters[regNum] = value;
                    // 同步更新32位整数寄存器，使 MOVE/PUSH 等32位指令能读取低32位
                    registers[regNum] = (int)(value & 0xFFFFFFFF);
                }
                else if (regNum < 16) // R8-R15 → 截断为32位
                    registers[regNum] = (int)(value & 0xFFFFFFFF);
                else
                    throw new VmlFloatException($"无效的长整数寄存器：L{regNum}");
            }
            else if (operand.Type == OperandType.MEMORY)
            {
                int addr = GetMemoryAddress(operand);
                SetMemory(addr, (int)(value & 0xFFFFFFFF));
                SetMemory(addr + 4, (int)((value >> 32) & 0xFFFFFFFF));
            }
            else if (operand.Type == OperandType.INDIRECT)
            {
                int addr = GetIndirectAddress(operand);
                SetMemory(addr, (int)(value & 0xFFFFFFFF));
                SetMemory(addr + 4, (int)((value >> 32) & 0xFFFFFFFF));
            }
            else
            {
                throw new VmlFloatException($"不支持的长整数目标操作数类型：{operand.Type}");
            }
        }

        /// <summary>PUSHL — 64位长整数压栈</summary>
        private void ExecutePushL(List<Operand> operands)
        {
            long val = GetLongValue(operands[0]);
            longStack.Push(val);
        }

        /// <summary>POPL — 64位长整数弹栈</summary>
        private void ExecutePopL(List<Operand> operands)
        {
            if (longStack.Count == 0)
                throw new VmlFloatException("长整数栈下溢");
            long val = longStack.Pop();
            SetLongValue(operands[0], val);
        }

        /// <summary>ADDL dst, src1, src2 — 64位加法</summary>
        private void ExecuteAddL(List<Operand> operands)
        {
            long src1 = GetLongValue(operands.Count > 2 ? operands[1] : operands[0]);
            long src2 = GetLongValue(operands.Count > 1 ? operands[^1] : operands[0]);
            SetLongValue(operands[0], src1 + src2);
        }

        /// <summary>SUBL dst, src1, src2 — 64位减法</summary>
        private void ExecuteSubL(List<Operand> operands)
        {
            long src1 = GetLongValue(operands.Count > 2 ? operands[1] : operands[0]);
            long src2 = GetLongValue(operands.Count > 1 ? operands[^1] : operands[0]);
            SetLongValue(operands[0], src1 - src2);
        }

        /// <summary>MULL dst, src1, src2 — 64位乘法</summary>
        private void ExecuteMulL(List<Operand> operands)
        {
            long src1 = GetLongValue(operands.Count > 2 ? operands[1] : operands[0]);
            long src2 = GetLongValue(operands.Count > 1 ? operands[^1] : operands[0]);
            SetLongValue(operands[0], src1 * src2);
        }

        /// <summary>DIVL dst, src1, src2 — 64位除法</summary>
        private void ExecuteDivL(List<Operand> operands)
        {
            long src1 = GetLongValue(operands.Count > 2 ? operands[1] : operands[0]);
            long src2 = GetLongValue(operands.Count > 1 ? operands[^1] : operands[0]);
            if (src2 == 0) throw new VmlRuntimeException("长整数除零");
            SetLongValue(operands[0], src1 / src2);
        }

        /// <summary>MODL dst, src1, src2 — 64位取模</summary>
        private void ExecuteModL(List<Operand> operands)
        {
            long src1 = GetLongValue(operands.Count > 2 ? operands[1] : operands[0]);
            long src2 = GetLongValue(operands.Count > 1 ? operands[^1] : operands[0]);
            if (src2 == 0) throw new VmlRuntimeException("长整数除零取模");
            SetLongValue(operands[0], src1 % src2);
        }

        /// <summary>NEGL — 64位取负</summary>
        private void ExecuteNegL(List<Operand> operands)
        {
            long val = GetLongValue(operands[0]);
            SetLongValue(operands[0], -val);
        }

        /// <summary>CMPL src1, src2 — 64位比较，设置 zf/sf/cf</summary>
        private void ExecuteCmpL(List<Operand> operands)
        {
            long src1 = GetLongValue(operands[0]);
            long src2 = GetLongValue(operands[1]);
            long diff = src1 - src2;
            zf = (diff == 0);
            sf = (diff < 0);
            // 与 32 位 CMP (SetFlags(result) → cf=false) 保持一致：
            // JL/JG 用 sf != cf / sf == cf 判断 signed 比较，此处 cf 扮演溢出位 of。
            // 原实现 cf = (ulong)src1 < (ulong)src2 是 unsigned 借位语义，与 signed 跳转冲突，
            // 导致 a<b 被误判为 a>b。不设 cf（保持 false）与 CMP/FCMP/DCMP 语义一致。
            cf = false;
        }

        // ======== 64位长整数类型转换 ========

        /// <summary>I2L — int32 → long64 (符号扩展)</summary>
        private void ExecuteI2L(List<Operand> operands)
        {
            var src = operands.Count > 1 ? operands[1] : operands[0];
            int val;
            if (src.Type == OperandType.REGISTER)
                val = registers[(int)src.Value];
            else if (src.Type == OperandType.IMMEDIATE)
                val = (int)src.Value;
            else
                val = (int)GetLongValue(src);
            SetLongValue(operands[0], (long)val);
        }

        /// <summary>L2I — long64 → int32 (截断低32位)</summary>
        private void ExecuteL2I(List<Operand> operands)
        {
            long val = GetLongValue(operands.Count > 1 ? operands[1] : operands[0]);
            int result = (int)(val & 0xFFFFFFFF);
            var dst = operands[0];
            if (dst.Type == OperandType.REGISTER)
                registers[(int)dst.Value] = result;
            else if (dst.Type == OperandType.MEMORY)
                SetMemory(GetMemoryAddress(dst), result);
        }

        /// <summary>F2L — float → long64</summary>
        private void ExecuteF2L(List<Operand> operands)
        {
            float val = GetFloatValue(operands.Count > 1 ? operands[1] : operands[0]);
            SetLongValue(operands[0], (long)val);
        }

        /// <summary>L2F — long64 → float</summary>
        private void ExecuteL2F(List<Operand> operands)
        {
            long val = GetLongValue(operands.Count > 1 ? operands[1] : operands[0]);
            SetFloatValue(operands[0], (float)val);
        }

        /// <summary>D2L — double → long64</summary>
        private void ExecuteD2L(List<Operand> operands)
        {
            double val = GetDoubleValue(operands.Count > 1 ? operands[1] : operands[0]);
            SetLongValue(operands[0], (long)val);
        }

        /// <summary>L2D — long64 → double</summary>
        private void ExecuteL2D(List<Operand> operands)
        {
            long val = GetLongValue(operands.Count > 1 ? operands[1] : operands[0]);
            SetDoubleValue(operands[0], (double)val);
        }

        // ====== 64位整数位运算 (v1.65.197) ======

        /// <summary>ANDL dst, src1, src2 — 64位按位与</summary>
        private void ExecuteAndL(List<Operand> operands)
        {
            long src1 = GetLongValue(operands.Count > 2 ? operands[1] : operands[0]);
            long src2 = GetLongValue(operands.Count > 1 ? operands[^1] : operands[0]);
            SetLongValue(operands[0], src1 & src2);
        }

        /// <summary>ORL dst, src1, src2 — 64位按位或</summary>
        private void ExecuteOrL(List<Operand> operands)
        {
            long src1 = GetLongValue(operands.Count > 2 ? operands[1] : operands[0]);
            long src2 = GetLongValue(operands.Count > 1 ? operands[^1] : operands[0]);
            SetLongValue(operands[0], src1 | src2);
        }

        /// <summary>XORL dst, src1, src2 — 64位按位异或</summary>
        private void ExecuteXorL(List<Operand> operands)
        {
            long src1 = GetLongValue(operands.Count > 2 ? operands[1] : operands[0]);
            long src2 = GetLongValue(operands.Count > 1 ? operands[^1] : operands[0]);
            SetLongValue(operands[0], src1 ^ src2);
        }

        /// <summary>NOTL dst, src — 64位按位取反</summary>
        private void ExecuteNotL(List<Operand> operands)
        {
            long val = GetLongValue(operands.Count > 1 ? operands[1] : operands[0]);
            SetLongValue(operands[0], ~val);
        }

        /// <summary>SHLL dst, src1, src2 — 64位左移 (src1 << src2)</summary>
        private void ExecuteShlL(List<Operand> operands)
        {
            long src1 = GetLongValue(operands.Count > 2 ? operands[1] : operands[0]);
            /* ⚠ **移位次数必须按 32 位读，不能用 `GetLongValue`** ——
               次数是个 `int`，前端用 32 位 `MOVE` 写进 `registers[]`；
               而 `GetLongValue` 对 `R0-R7` 读的是**另一份** `longRegisters[]`
               （见它那里的分派），于是读到的是**上一个 64 位值**。
               实测症状极具指纹性：`long v = 1; v << 4 / v << 32 / v << 40`
               **一律得 2** —— 因为 `longRegisters[0]` 正好还留着上一个长整数值 1，
               次次都变成"移 1 位"。这条也是"改前端把 32 位移位一起改坏"那次
               真正的拦路石（前端确实漏了 `l`，但底下还压着这一条）。 */
            int src2 = GetRegisterOrImmediate(operands.Count > 1 ? operands[^1] : operands[0]);
            SetLongValue(operands[0], src1 << src2);
        }

        /// <summary>SHRL dst, src1, src2 — 64位右移 (src1 >> src2)</summary>
        private void ExecuteShrL(List<Operand> operands)
        {
            long src1 = GetLongValue(operands.Count > 2 ? operands[1] : operands[0]);
            int src2 = GetRegisterOrImmediate(operands.Count > 1 ? operands[^1] : operands[0]);   /* 同 ExecuteShlL：次数按 32 位读 */
            SetLongValue(operands[0], src1 >> src2);
        }

        // ══════════════════════════════════════════════════════════════════════
        // 无符号语义（号段 113–125，2026-09-22 用户批准新增）
        //
        // 语义与"为什么另起一个 `uf` 标志"见 `VMLAssembler/OpCode.cs` 那一节与
        // `VMLRuntime.uf` 的说明。这里只记**实现层的两条硬规矩**：
        //   ① 32 位的量一律用 `GetRegisterOrImmediate`（读 `registers[]`），
        //      **绝不能**用 `GetLongValue` —— 后者对 R0-R7 读的是另一份
        //      `longRegisters[]`（`ExecuteShlL` 的移位次数就栽在这上面）；
        //   ② 无符号比较**只置 `uf`/`zf`**，不碰 `sf`/`cf`。
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// `ZEXTL Rd, Rs` —— **零扩展** int32 → long64。
        ///
        /// **为什么必须有它**：`I2L` 是**符号**扩展（实测 `(long)4000000000u`
        /// 得 -294967296）。无符号要把 int 升成 64 位（再去走 `DIVL`/`CMPL`/`SHRL`）
        /// 就必须零扩展，没有它每处都得补一句 `ANDL #0xFFFFFFFF`。
        /// </summary>
        private void ExecuteZextL(List<Operand> operands)
        {
            int src = GetRegisterOrImmediate(operands.Count > 1 ? operands[^1] : operands[0]);
            SetLongValue(operands[0], (long)(uint)src);
        }

        /// <summary>`DIVU Rd, Rs1, Rs2` —— 无符号 32 位除法（`DIV` 是有符号）。</summary>
        private void ExecuteDivU(List<Operand> operands)
        {
            var dest = operands[0];
            int ia = operands.Count >= 3 ? GetRegisterOrImmediate(operands[1])
                 : (dest.Type == OperandType.REGISTER ? registers[(int)dest.Value] : 0);
            uint b = (uint)GetRegisterOrImmediate(operands[Count2(operands)]);
            if (b == 0) throw new VmlRuntimeException("无符号除零");
            StoreInt(dest, (int)((uint)ia / b));
        }

        /// <summary>`MODU Rd, Rs1, Rs2` —— 无符号 32 位取模。</summary>
        private void ExecuteModU(List<Operand> operands)
        {
            var dest = operands[0];
            int ia = operands.Count >= 3 ? GetRegisterOrImmediate(operands[1])
                 : (dest.Type == OperandType.REGISTER ? registers[(int)dest.Value] : 0);
            uint b = (uint)GetRegisterOrImmediate(operands[Count2(operands)]);
            if (b == 0) throw new VmlRuntimeException("无符号取模除零");
            StoreInt(dest, (int)((uint)ia % b));
        }

        /// <summary>`SHRU Rd, Rs1, Rs2` —— **逻辑**右移 32 位（高位补 0）。`SHR` 是算术右移（实测 `-16 >> 1 = -8`）。</summary>
        private void ExecuteShrU(List<Operand> operands)
        {
            var dest = operands[0];
            int ia = operands.Count >= 3 ? GetRegisterOrImmediate(operands[1])
                 : (dest.Type == OperandType.REGISTER ? registers[(int)dest.Value] : 0);
            int sh = GetRegisterOrImmediate(operands[Count2(operands)]) & 31;   /* 32 位：次数按 5 位取模（与 C# 的 int 语义一致） */
            StoreInt(dest, (int)((uint)ia >> sh));
        }

        /// <summary>`DIVUL Rd, Rs1, Rs2` —— 无符号 64 位除法。</summary>
        private void ExecuteDivUL(List<Operand> operands)
        {
            ulong a = (ulong)GetLongValue(operands.Count > 2 ? operands[1] : operands[0]);
            ulong b = (ulong)GetLongValue(operands[Count2(operands)]);
            if (b == 0) throw new VmlRuntimeException("无符号长整数除零");
            SetLongValue(operands[0], (long)(a / b));
        }

        /// <summary>`MODUL Rd, Rs1, Rs2` —— 无符号 64 位取模。</summary>
        private void ExecuteModUL(List<Operand> operands)
        {
            ulong a = (ulong)GetLongValue(operands.Count > 2 ? operands[1] : operands[0]);
            ulong b = (ulong)GetLongValue(operands[Count2(operands)]);
            if (b == 0) throw new VmlRuntimeException("无符号长整数取模除零");
            SetLongValue(operands[0], (long)(a % b));
        }

        /// <summary>`SHRUL Rd, Rs1, Rs2` —— **逻辑**右移 64 位（`SHRL` 是算术右移，实测负数 `>>1` 得 -8）。</summary>
        private void ExecuteShrUL(List<Operand> operands)
        {
            ulong a = (ulong)GetLongValue(operands.Count > 2 ? operands[1] : operands[0]);
            int sh = GetRegisterOrImmediate(operands[Count2(operands)]) & 63;   /* 64 位：次数按 6 位取模 */
            SetLongValue(operands[0], (long)(a >> sh));
        }

        /// <summary>
        /// `CMPU`/`CMPUL` —— 无符号比较：置 **`uf`**（a &lt; b 按无符号）与 **`zf`**（a == b）。
        ///
        /// ⚠ **刻意不碰 `sf`/`cf`** —— 它们归有符号跳转（`JG = !zf &amp;&amp; sf == cf`，
        /// 其中 `cf` 被当**溢出位**用）。若这里顺手改了 `cf`，任何"无符号比较 +
        /// 有符号跳转"的混用都会悄悄错，而且错得不明显。
        /// </summary>
        private void ExecuteCmpU(List<Operand> operands, bool longForm)
        {
            if (longForm)
            {
                ulong a = (ulong)GetLongValue(operands[0]);
                ulong b = (ulong)GetLongValue(operands[1]);
                zf = a == b;
                uf = a < b;
            }
            else
            {
                uint a = (uint)GetRegisterOrImmediate(operands[0]);
                uint b = (uint)GetRegisterOrImmediate(operands[1]);
                zf = a == b;
                uf = a < b;
            }
        }

        /// <summary>
        /// `JA`/`JB`/`JAE`/`JBE` —— 无符号条件跳转，读 **`uf`/`zf`**（不是 `cf`/`sf`）。
        /// 与有符号那四条一一对应：`>`→JA、`&lt;`→JB、`>=`→JAE、`&lt;=`→JBE。
        /// </summary>
        private void ExecuteJumpU(List<Operand> operands, string cmp)
        {
            bool take = cmp switch
            {
                ">"  => !uf && !zf,
                "<"  => uf,
                ">=" => !uf,
                "<=" => uf || zf,
                _    => false,
            };
            if (take) ExecuteJmp(operands);
        }

        /// <summary>32 位运算的"第二个源操作数"下标：3 操作数取 `[2]`，2 操作数取 `[1]`。</summary>
        private static int Count2(List<Operand> operands) => operands.Count > 2 ? 2 : 1;

        /// <summary>把 32 位结果写回（与 32 位算术的规范写法一致：只认寄存器，并同步 R13=sp）。</summary>
        private void StoreInt(Operand dest, int result)
        {
            if (dest.Type == OperandType.REGISTER)
            {
                registers[(int)dest.Value] = result;
                if ((int)dest.Value == 13) sp = result;
            }
        }
    }
}
