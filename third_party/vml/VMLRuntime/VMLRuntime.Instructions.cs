using VMLAssembler;
using System.IO;
using VMLRuntime.Device;

namespace VMLRuntime
{
    public partial class VmRuntime
    {
        // 消除 STORE/STOREH/STOREB 的重复 new List+Reverse 分配
        private static readonly Dictionary<OpCode, Action<VmRuntime, List<Operand>>> _dispatchTable = new()
        {
            // v1.65.167+: 旧指令名已删除，数值映射保留向后兼容
            [(OpCode)2] = (vm, ops) => vm.ExecuteMove(ops),       // LOAD
            [(OpCode)5] = (vm, ops) => { ops.Reverse(); vm.ExecuteMove(ops); }, // STORE (原地 reverse)
            [(OpCode)3] = (vm, ops) => vm.ExecuteMoveW(ops),      // LOADH
            [(OpCode)6] = (vm, ops) => { ops.Reverse(); vm.ExecuteMoveW(ops); }, // STOREH
            [(OpCode)4] = (vm, ops) => vm.ExecuteMoveB(ops),      // LOADB
            [(OpCode)7] = (vm, ops) => { ops.Reverse(); vm.ExecuteMoveB(ops); }, // STOREB
            [OpCode.MOVE] = (vm, ops) => vm.ExecuteMove(ops),
            [OpCode.MOVEB] = (vm, ops) => vm.ExecuteMoveB(ops),
            [OpCode.MOVEH] = (vm, ops) => vm.ExecuteMoveW(ops),
            [OpCode.PUSH] = (vm, ops) => vm.ExecutePush(ops),
            [OpCode.PUSHB] = (vm, ops) => vm.ExecutePushB(ops),
            [OpCode.PUSHH] = (vm, ops) => vm.ExecutePushW(ops),
            [OpCode.POP] = (vm, ops) => vm.ExecutePop(ops),
            [OpCode.POPB] = (vm, ops) => vm.ExecutePopB(ops),
            [OpCode.POPH] = (vm, ops) => vm.ExecutePopW(ops),
            [OpCode.ADD] = (vm, ops) => vm.ExecuteAdd(ops),
            [OpCode.SUB] = (vm, ops) => vm.ExecuteSub(ops),
            [OpCode.MUL] = (vm, ops) => vm.ExecuteMul(ops),
            [OpCode.DIV] = (vm, ops) => vm.ExecuteDiv(ops),
            [OpCode.MOD] = (vm, ops) => vm.ExecuteMod(ops),
            [OpCode.INC] = (vm, ops) => vm.ExecuteInc(ops),
            [OpCode.DEC] = (vm, ops) => vm.ExecuteDec(ops),
            [OpCode.NEG] = (vm, ops) => vm.ExecuteNeg(ops),
            [OpCode.AND] = (vm, ops) => vm.ExecuteAnd(ops),
            [OpCode.OR] = (vm, ops) => vm.ExecuteOr(ops),
            [OpCode.XOR] = (vm, ops) => vm.ExecuteXor(ops),
            [OpCode.NOT] = (vm, ops) => vm.ExecuteNot(ops),
            [OpCode.SHL] = (vm, ops) => vm.ExecuteShl(ops),
            [OpCode.SHR] = (vm, ops) => vm.ExecuteShr(ops),
            [OpCode.CMP] = (vm, ops) => vm.ExecuteCmp(ops),
            [OpCode.TEST] = (vm, ops) => vm.ExecuteTest(ops),
            [OpCode.CMOVZ] = (vm, ops) => { if (vm.zf) { int sv = ops[1].Type == OperandType.IMMEDIATE ? (int)ops[1].Value : vm.registers[(int)ops[1].Value]; vm.registers[(int)ops[0].Value] = sv; } },
            [OpCode.CMOVNZ] = (vm, ops) => { if (!vm.zf) { int sv = ops[1].Type == OperandType.IMMEDIATE ? (int)ops[1].Value : vm.registers[(int)ops[1].Value]; vm.registers[(int)ops[0].Value] = sv; } },
            [OpCode.ROL] = (vm, ops) => vm.ExecuteRol(ops),
            [OpCode.ROR] = (vm, ops) => vm.ExecuteRor(ops),
            [OpCode.JMP] = (vm, ops) => vm.ExecuteJmp(ops),
            [OpCode.JZ] = (vm, ops) => {
                // JZ R0 label: check register value R0==0
                // JZ label (no register): check ZF flag (legacy CMP path)
                if (ops.Count >= 2 && ops[0].Type == OperandType.REGISTER) {
                    int regVal = vm.registers[(int)ops[0].Value];
                    if (regVal == 0) vm.ExecuteJmp(ops);
                } else {
                    if (vm.zf) vm.ExecuteJmp(ops);
                }
            },
            [OpCode.JE] = (vm, ops) => { if (vm.zf) vm.ExecuteJmp(ops); },
            [OpCode.JNZ] = (vm, ops) => {
                // JNZ R0 label: check register value R0!=0
                // JNZ label: check ZF flag (legacy)
                if (ops.Count >= 2 && ops[0].Type == OperandType.REGISTER) {
                    int regVal = vm.registers[(int)ops[0].Value];
                    if (regVal != 0) vm.ExecuteJmp(ops);
                } else {
                    if (!vm.zf) vm.ExecuteJmp(ops);
                }
            },
            [OpCode.JNE] = (vm, ops) => { if (!vm.zf) vm.ExecuteJmp(ops); },
            [OpCode.JL] = (vm, ops) => vm.ExecuteJl(ops),
            [OpCode.JLE] = (vm, ops) => vm.ExecuteJle(ops),
            [OpCode.JG] = (vm, ops) => vm.ExecuteJg(ops),
            [OpCode.JGE] = (vm, ops) => vm.ExecuteJge(ops),
            [OpCode.CALL] = (vm, ops) => vm.ExecuteCall(ops),
            [OpCode.RET] = (vm, ops) => vm.ExecuteRet(ops),
            [OpCode.SYSCALL] = (vm, ops) => vm.ExecuteSyscall(ops),
            [OpCode.HALT] = (vm, ops) => vm.ExecuteHalt(ops),
            [OpCode.NOP] = (_, _) => { },

            [OpCode.LABEL] = (_, _) => { },
            [OpCode.FADD] = (vm, ops) => vm.ExecuteFadd(ops),
            [OpCode.FSUB] = (vm, ops) => vm.ExecuteFsub(ops),
            [OpCode.FMUL] = (vm, ops) => vm.ExecuteFmul(ops),
            [OpCode.FDIV] = (vm, ops) => vm.ExecuteFdiv(ops),
            [OpCode.FNEG] = (vm, ops) => vm.ExecuteFneg(ops),
            [OpCode.FCMP] = (vm, ops) => vm.ExecuteFcmp(ops),
            [OpCode.I2F] = (vm, ops) => vm.ExecuteI2f(ops),
            [OpCode.F2I] = (vm, ops) => vm.ExecuteF2i(ops),
            [OpCode.F2D] = (vm, ops) => vm.ExecuteF2d(ops),
            [OpCode.D2F] = (vm, ops) => vm.ExecuteD2f(ops),
            [OpCode.I2D] = (vm, ops) => vm.ExecuteI2d(ops),
            [OpCode.D2I] = (vm, ops) => vm.ExecuteD2i(ops),
            // v1.65.166+: FLOAD/FSTORE/DLOAD/DSTORE 已删除 → 统一 MOVEF/MOVED
            [OpCode.MOVEF] = (vm, ops) => vm.ExecuteMoveF(ops),
            [OpCode.MOVED] = (vm, ops) => vm.ExecuteMoveD(ops),
            [OpCode.MOVEL] = (vm, ops) => vm.ExecuteMoveL(ops),
            [OpCode.PUSHL] = (vm, ops) => vm.ExecutePushL(ops),
            [OpCode.POPL] = (vm, ops) => vm.ExecutePopL(ops),
            [OpCode.ADDL] = (vm, ops) => vm.ExecuteAddL(ops),
            [OpCode.SUBL] = (vm, ops) => vm.ExecuteSubL(ops),
            [OpCode.MULL] = (vm, ops) => vm.ExecuteMulL(ops),
            [OpCode.DIVL] = (vm, ops) => vm.ExecuteDivL(ops),
            [OpCode.MODL] = (vm, ops) => vm.ExecuteModL(ops),
            [OpCode.NEGL] = (vm, ops) => vm.ExecuteNegL(ops),
            [OpCode.CMPL] = (vm, ops) => vm.ExecuteCmpL(ops),
            [OpCode.I2L] = (vm, ops) => vm.ExecuteI2L(ops),
            [OpCode.L2I] = (vm, ops) => vm.ExecuteL2I(ops),
            [OpCode.F2L] = (vm, ops) => vm.ExecuteF2L(ops),
            [OpCode.L2F] = (vm, ops) => vm.ExecuteL2F(ops),
            [OpCode.D2L] = (vm, ops) => vm.ExecuteD2L(ops),
            [OpCode.L2D] = (vm, ops) => vm.ExecuteL2D(ops),
            [OpCode.ANDL] = (vm, ops) => vm.ExecuteAndL(ops),
            [OpCode.ORL] = (vm, ops) => vm.ExecuteOrL(ops),
            [OpCode.XORL] = (vm, ops) => vm.ExecuteXorL(ops),
            [OpCode.NOTL] = (vm, ops) => vm.ExecuteNotL(ops),
            [OpCode.SHLL] = (vm, ops) => vm.ExecuteShlL(ops),
            [OpCode.SHRL] = (vm, ops) => vm.ExecuteShrL(ops),
            [OpCode.SEXTB] = (vm, ops) => { int b = vm.registers[(int)ops[1].Value] & 0xFF; vm.registers[(int)ops[0].Value] = (sbyte)b; },
            [OpCode.SEXTH] = (vm, ops) => { int h = vm.registers[(int)ops[1].Value] & 0xFFFF; vm.registers[(int)ops[0].Value] = (short)h; },
            [OpCode.DADD] = (vm, ops) => vm.ExecuteDadd(ops),
            [OpCode.DSUB] = (vm, ops) => vm.ExecuteDsub(ops),
            [OpCode.DMUL] = (vm, ops) => vm.ExecuteDmul(ops),
            [OpCode.DDIV] = (vm, ops) => vm.ExecuteDdiv(ops),
            [OpCode.DNEG] = (vm, ops) => vm.ExecuteDneg(ops),
            [OpCode.DCMP] = (vm, ops) => vm.ExecuteDcmp(ops),
            [OpCode.DPUSH] = (vm, ops) => vm.ExecuteDpush(ops),
            [OpCode.DPOP] = (vm, ops) => vm.ExecuteDpop(ops),
            [OpCode.FPUSH] = (vm, ops) => vm.ExecuteFpush(ops),
            [OpCode.FPOP] = (vm, ops) => vm.ExecuteFpop(ops),
            [OpCode.ENTER] = (vm, ops) => vm.ExecuteEnter(ops),
            [OpCode.LEAVE] = (vm, ops) => vm.ExecuteLeave(ops),
            [OpCode.CLC] = (vm, _) => { vm.cf = false; },
            [OpCode.STC] = (vm, _) => { vm.cf = true; },
            // v1.65.166+: LEA 已删除
            [OpCode.BREAK] = (vm, ops) => vm.ExecuteBreak(ops),
            [OpCode.TRACE] = (vm, ops) => vm.ExecuteTrace(ops),
            [OpCode.DUMP] = (vm, ops) => vm.ExecuteDump(ops),

            [OpCode.SHLV] = (vm, ops) => vm.ExecuteShlv(ops),
            [OpCode.SHRV] = (vm, ops) => vm.ExecuteShrv(ops),
            [OpCode.ZERO] = (vm, ops) => vm.ExecuteZero(ops),
            [OpCode.CHIPASM] = (_, _) => { }, // chipasm is a no-op at runtime
            [OpCode.ASM] = (vm, ops) => vm.ExecuteAsm(ops),
            [OpCode.INT] = (vm, ops) => vm.ExecuteInt(ops),
            [OpCode.IRET] = (vm, _) => vm.ExecuteIret(),
            [OpCode.CLI] = (vm, _) => { if (vm.PrivilegeLevel == 0) vm._interruptEnabled = false; },
            [OpCode.STI] = (vm, _) => { if (vm.PrivilegeLevel == 0) vm._interruptEnabled = true; },
            [OpCode.THROW] = (vm, ops) => vm.ExecuteThrow(ops),
            [OpCode.CATCH] = (vm, ops) => vm.ExecuteCatch(ops),
            [OpCode.ENDCATCH] = (vm, _) => vm.ExecuteEndCatch(),
            };

        private void ExecuteInstruction()
        {
            if (_program == null)
            {
                Console.WriteLine("Error: No program loaded.");
                return;
            }

            if (pc < 0 || pc >= _program.Instructions.Count)
            {
                // 程序到达末尾没有显式 HALT — 正常终止
                pc = -1;
                return;
            }

            if (DebugMode && breakpoints.Contains(pc))
            {
                OnBreakpoint?.Invoke(pc);
                return;
            }

            var instruction = _program.Instructions[pc];

            if (EnableLogging)
                OnDebugLog?.Invoke($"PC: {pc}, Opcode: {instruction.Opcode}, Operands: {string.Join(", ", instruction.Operands)}");

            pc++;

            if (_dispatchTable.TryGetValue(instruction.Opcode, out var handler))
                handler(this, instruction.Operands);
            else
            {
                Console.WriteLine($"错误: 未实现的指令 '{instruction.Opcode}' 在 PC={pc-1}");
                pc = -1;
            }
        }

        private void ExecuteInstruction(Instruction instruction)
        {
            if (_dispatchTable.TryGetValue(instruction.Opcode, out var handler))
                handler(this, instruction.Operands);
        }

        private void ExecuteMove(List<Operand> operands)
        {
            var dest = operands[0];
            var src  = operands[1];

            // === 求源值 ===
            int value = 0;
            if (src.Type == OperandType.REGISTER)
                value = registers[(int)src.Value];
            else if (src.Type == OperandType.IMMEDIATE)
            {
                if (src.Value is string s)
                    value = labelAddresses.TryGetValue(s, out var addr) ? addr : 0;
                else if (src.Value is long l)
                    value = (int)l;
                else
                    value = (int)src.Value;
            }
            else if (src.Type == OperandType.LABEL)
            {
                // MOVE reg, label — 统一 LEA (取标签地址)
                value = labelAddresses.TryGetValue((string)src.Value, out var la) ? la : 0;
            }
            else if (src.Type == OperandType.MEMORY)
            {
                // MOVE reg, [mem] — 统一 LOAD (mem→reg)
                int addr = GetAddress(src);
                value = GetMemory(addr);
            }
            else if (src.Type == OperandType.INDIRECT)
            {
                // MOVE reg, @Rxx-offset — INDIRECT 寻址
                int addr = GetAddress(src);
                value = GetMemory(addr);
            }

            // === 写目标 ===
            if (dest.Type == OperandType.REGISTER)
            {
                int regIdx = (int)dest.Value;
                registers[regIdx] = value;
                if (regIdx == 13) sp = value;
            }
            else if (dest.Type == OperandType.MEMORY || dest.Type == OperandType.INDIRECT)
            {
                // MOVE [mem], reg — 统一 STORE (reg→mem)
                int addr = GetAddress(dest);
                SetMemory(addr, value);
            }
            else if (dest.Type == OperandType.LABEL)
            {
                // MOVE label, reg — 统一 STORE 到标签地址 (reg→label)
                string labelName = (string)dest.Value;
                if (!string.IsNullOrEmpty(labelName) && labelAddresses.ContainsKey(labelName))
                {
                    int addr = labelAddresses[labelName];
                    SetMemory(addr, value);
                }
            }
        }

        /// <summary>
        /// 执行MOVEB - 移动8位数据（零扩展为32位）
        /// </summary>
        private void ExecuteMoveB(List<Operand> operands)
        {
            var dest = operands[0];
            var src = operands[1];

            // === 求源值 ===
            byte value = 0;
            if (src.Type == OperandType.REGISTER)
                value = (byte)(registers[(int)src.Value] & 0xFF);
            else if (src.Type == OperandType.IMMEDIATE)
                value = (byte)((int)src.Value & 0xFF);
            else if (src.Type == OperandType.LABEL)
            {
                // MOVEB reg, label — 统一 LOADB (从标签地址读取字节)
                string labelName = (string)src.Value;
                if (!string.IsNullOrEmpty(labelName) && labelAddresses.ContainsKey(labelName))
                {
                    int addr = labelAddresses[labelName];
                    value = GetMemoryByte(addr);
                }
            }
            else if (src.Type == OperandType.MEMORY)
            {
                // MOVEB reg, [mem] — 统一 LOADB (mem→reg)
                int addr = GetAddress(src);
                value = GetMemoryByte(addr);
            }
            else if (src.Type == OperandType.INDIRECT)
            {
                int addr = GetAddress(src);
                value = GetMemoryByte(addr);
            }

            // === 写目标 ===
            if (dest.Type == OperandType.REGISTER)
            {
                registers[(int)dest.Value] = value; // 零扩展
                if ((int)dest.Value == 13) sp = value;
            }
            else if (dest.Type == OperandType.MEMORY || dest.Type == OperandType.INDIRECT)
            {
                // MOVEB [mem], reg — 统一 STOREB (reg→mem)
                int addr = GetAddress(dest);
                SetMemoryByte(addr, value);
            }
            else if (dest.Type == OperandType.LABEL)
            {
                if (!string.IsNullOrEmpty((string)dest.Value) && labelAddresses.ContainsKey((string)dest.Value))
                    SetMemoryByte(labelAddresses[(string)dest.Value], value);
            }
        }

        /// <summary>
        /// 执行MOVEW - 移动16位数据（零扩展为32位）
        /// </summary>
        private void ExecuteMoveW(List<Operand> operands)
        {
            var dest = operands[0];
            var src = operands[1];

            // === 求源值 ===
            ushort value = 0;
            if (src.Type == OperandType.REGISTER)
                value = (ushort)(registers[(int)src.Value] & 0xFFFF);
            else if (src.Type == OperandType.IMMEDIATE)
                value = (ushort)((int)src.Value & 0xFFFF);
            else if (src.Type == OperandType.LABEL)
            {
                // MOVEH reg, label — 统一 LOADH (从标签地址读取半字)
                string labelName = (string)src.Value;
                if (!string.IsNullOrEmpty(labelName) && labelAddresses.ContainsKey(labelName))
                {
                    int addr = labelAddresses[labelName];
                    value = GetMemoryWord(addr);
                }
            }
            else if (src.Type == OperandType.MEMORY)
            {
                // MOVEH reg, [mem] — 统一 LOADH (mem→reg)
                int addr = GetAddress(src);
                value = GetMemoryWord(addr);
            }
            else if (src.Type == OperandType.INDIRECT)
            {
                int addr = GetAddress(src);
                value = GetMemoryWord(addr);
            }

            // === 写目标 ===
            if (dest.Type == OperandType.REGISTER)
            {
                registers[(int)dest.Value] = value; // 零扩展
                if ((int)dest.Value == 13) sp = value;
            }
            else if (dest.Type == OperandType.MEMORY || dest.Type == OperandType.INDIRECT)
            {
                // MOVEH [mem], reg — 统一 STOREH (reg→mem)
                int addr = GetAddress(dest);
                SetMemoryWord(addr, value);
            }
        }

        /// <summary>
        /// 执行PUSHB - 压栈8位数据
        /// </summary>
        private void ExecutePushB(List<Operand> operands)
        {
            var src = operands[0];
            byte value = 0;

            if (src.Type == OperandType.REGISTER)
            {
                value = (byte)(registers[(int)src.Value] & 0xFF);
            }
            else if (src.Type == OperandType.IMMEDIATE)
            {
                value = (byte)((int)src.Value & 0xFF);
            }

            sp -= 1;
            registers[13] = sp;
            SetMemoryByte(sp, value);
        }

        /// <summary>
        /// 执行PUSHW - 压栈16位数据
        /// </summary>
        private void ExecutePushW(List<Operand> operands)
        {
            var src = operands[0];
            ushort value = 0;

            if (src.Type == OperandType.REGISTER)
            {
                value = (ushort)(registers[(int)src.Value] & 0xFFFF);
            }
            else if (src.Type == OperandType.IMMEDIATE)
            {
                value = (ushort)((int)src.Value & 0xFFFF);
            }

            sp -= 2;
            registers[13] = sp;
            SetMemoryWord(sp, value);
        }

        /// <summary>
        /// 执行POPB - 弹栈8位数据（零扩展为32位）
        /// </summary>
        private void ExecutePopB(List<Operand> operands)
        {
            var dest = operands[0];
            byte value = GetMemoryByte(sp);
            sp += 1;
            registers[13] = sp;

            if (dest.Type == OperandType.REGISTER)
            {
                registers[(int)dest.Value] = value; // 零扩展
            }
        }

        /// <summary>
        /// 执行POPW - 弹栈16位数据（零扩展为32位）
        /// </summary>
        private void ExecutePopW(List<Operand> operands)
        {
            var dest = operands[0];
            ushort value = GetMemoryWord(sp);
            sp += 2;
            registers[13] = sp;

            if (dest.Type == OperandType.REGISTER)
            {
                registers[(int)dest.Value] = value; // 零扩展
            }
        }

        private void ExecutePush(List<Operand> operands)
        {
            var src   = operands[0];
            int value = 0;

            if (src.Type == OperandType.REGISTER)
            {
                value = registers[(int)src.Value];
            }
            else if (src.Type == OperandType.IMMEDIATE)
            {
                value = (int)src.Value;
            }

            sp            -= 4;
            registers[13] =  sp; // 同步更新R13
            SetMemory(sp, value);
        }

        private void ExecutePop(List<Operand> operands)
        {
            var dest  = operands[0];
            int value = GetMemory(sp);
            sp            += 4;
            registers[13] =  sp; // 同步更新R13

            if (dest.Type == OperandType.REGISTER)
            {
                registers[(int)dest.Value] = value;
            }
        }

        private void ExecuteAdd(List<Operand> operands)
        {
            var dest = operands[0];

            int a, b;
            if (operands.Count >= 3)
            {
                // 3-operand: ADD Rd, Rs, Rt/Rt  →  Rd = Rs + Rt
                a = GetRegisterOrImmediate(operands[1]);
                b = GetRegisterOrImmediate(operands[2]);
            }
            else
            {
                // 2-operand: ADD Rd, Rs  →  Rd = Rd + Rs
                a = registers[(int)dest.Value];
                b = GetRegisterOrImmediate(operands[1]);
            }

            int result = a + b;
            if (dest.Type == OperandType.REGISTER)
            {
                registers[(int)dest.Value] = result;
                if ((int)dest.Value == 13) sp = result;
            }
            SetFlags(result, carry: (uint)result < (uint)a);
        }

        private int GetRegisterOrImmediate(Operand op)
        {
            if (op.Type == OperandType.REGISTER)
                return registers[(int)op.Value];
            if (op.Type == OperandType.IMMEDIATE)
            {
                if (op.Value is int intVal) return intVal;
                try { return Convert.ToInt32(op.Value); }
                catch { return 0; }
            }
            return 0;
        }

        private int OperandToInt(Operand op)
        {
            if (op.Value is int intVal) return intVal;
            try { return Convert.ToInt32(op.Value); }
            catch { return 0; }
        }

        private void ExecuteShlv(List<Operand> operands)
        {
            // SHLV Rd, Rs, Rt  →  Rd = Rs << Rt (Rt is shift amount)
            if (operands.Count >= 3 && operands[0].Type == OperandType.REGISTER)
            {
                int rd = (int)operands[0].Value;
                int rs = operands[1].Type == OperandType.REGISTER ? registers[(int)operands[1].Value] : (int)(operands[1].Value ?? 0);
                int rt = operands[2].Type == OperandType.REGISTER ? registers[(int)operands[2].Value] : (int)(operands[2].Value ?? 0);
                registers[rd] = rs << (rt & 0x1F);
            }
        }

        private void ExecuteShrv(List<Operand> operands)
        {
            // SHRV Rd, Rs, Rt  →  Rd = Rs >> Rt (Rt is shift amount)
            if (operands.Count >= 3 && operands[0].Type == OperandType.REGISTER)
            {
                int rd = (int)operands[0].Value;
                int rs = operands[1].Type == OperandType.REGISTER ? registers[(int)operands[1].Value] : (int)(operands[1].Value ?? 0);
                int rt = operands[2].Type == OperandType.REGISTER ? registers[(int)operands[2].Value] : (int)(operands[2].Value ?? 0);
                registers[rd] = rs >> (rt & 0x1F);
            }
        }

        private void ExecuteZero(List<Operand> operands)
        {
            // ZERO Rd  →  Rd = 0
            if (operands.Count >= 1 && operands[0].Type == OperandType.REGISTER)
            {
                registers[(int)operands[0].Value] = 0;
            }
        }

        private void ExecuteSub(List<Operand> operands)
        {
            var dest = operands[0];

            int a, b;
            if (operands.Count >= 3)
            {
                a = GetRegisterOrImmediate(operands[1]);
                b = GetRegisterOrImmediate(operands[2]);
            }
            else
            {
                a = registers[(int)dest.Value];
                b = GetRegisterOrImmediate(operands[1]);
            }

            int result = a - b;
            if (dest.Type == OperandType.REGISTER)
            {
                registers[(int)dest.Value] = result;
                if ((int)dest.Value == 13) sp = result;
            }
            SetFlags(result, carry: (uint)a < (uint)b);
        }

        private void ExecuteMul(List<Operand> operands)
        {
            var dest = operands[0];

            int a, b;
            if (operands.Count >= 3)
            {
                a = GetRegisterOrImmediate(operands[1]);
                b = GetRegisterOrImmediate(operands[2]);
            }
            else
            {
                a = registers[(int)dest.Value];
                b = GetRegisterOrImmediate(operands[1]);
            }

            int result = a * b;
            if (dest.Type == OperandType.REGISTER)
            {
                registers[(int)dest.Value] = result;
                if ((int)dest.Value == 13) sp = result;
            }
        }

        private void ExecuteDiv(List<Operand> operands)
        {
            var dest = operands[0];

            int a, b;
            if (operands.Count >= 3)
            {
                a = GetRegisterOrImmediate(operands[1]);
                b = GetRegisterOrImmediate(operands[2]);
            }
            else
            {
                a = registers[(int)dest.Value];
                b = GetRegisterOrImmediate(operands[1]);
            }

            if (b == 0) throw new VmlRuntimeException("整数除零");
            int result = a / b;
            if (dest.Type == OperandType.REGISTER)
            {
                registers[(int)dest.Value] = result;
                if ((int)dest.Value == 13) sp = result;
            }
        }

        private void ExecuteMod(List<Operand> operands)
        {
            var dest = operands[0];

            int a, b;
            if (operands.Count >= 3)
            {
                a = GetRegisterOrImmediate(operands[1]);
                b = GetRegisterOrImmediate(operands[2]);
            }
            else
            {
                a = registers[(int)dest.Value];
                b = GetRegisterOrImmediate(operands[1]);
            }

            if (b == 0) throw new VmlRuntimeException("整数除零取模");

            if (dest.Type == OperandType.REGISTER)
            {
                int result = a % b;
                registers[(int)dest.Value] = result;
                SetFlags(result);
            }
        }

        private void ExecuteInc(List<Operand> operands)
        {
            var dest = operands[0];
            if (dest.Type == OperandType.REGISTER)
            {
                int a = registers[(int)dest.Value];
                registers[(int)dest.Value] = a + 1;
                SetFlags(registers[(int)dest.Value], carry: (uint)(a + 1) < (uint)a);
            }
        }

        private void ExecuteDec(List<Operand> operands)
        {
            var dest = operands[0];
            if (dest.Type == OperandType.REGISTER)
            {
                int a = registers[(int)dest.Value];
                registers[(int)dest.Value] = a - 1;
                SetFlags(registers[(int)dest.Value], carry: (uint)a < (uint)1);
            }
        }

        private void ExecuteNeg(List<Operand> operands)
        {
            var dest = operands[0];
            if (dest.Type == OperandType.REGISTER)
            {
                int a = registers[(int)dest.Value];
                registers[(int)dest.Value] = -a;
                SetFlags(registers[(int)dest.Value], carry: a == int.MinValue);
            }
        }

        private void ExecuteAnd(List<Operand> operands)
        {
            var dest = operands[0];

            int a, b;
            if (operands.Count >= 3)
            {
                a = GetRegisterOrImmediate(operands[1]);
                b = GetRegisterOrImmediate(operands[2]);
            }
            else
            {
                a = registers[(int)dest.Value];
                b = GetRegisterOrImmediate(operands[1]);
            }

            if (dest.Type == OperandType.REGISTER)
            {
                int result = a & b;
                registers[(int)dest.Value] = result;
                SetFlags(result);
            }
        }

        private void ExecuteOr(List<Operand> operands)
        {
            var dest = operands[0];

            int a, b;
            if (operands.Count >= 3)
            {
                a = GetRegisterOrImmediate(operands[1]);
                b = GetRegisterOrImmediate(operands[2]);
            }
            else
            {
                a = registers[(int)dest.Value];
                b = GetRegisterOrImmediate(operands[1]);
            }

            if (dest.Type == OperandType.REGISTER)
            {
                int result = a | b;
                registers[(int)dest.Value] = result;
                SetFlags(result);
            }
        }

        private void ExecuteXor(List<Operand> operands)
        {
            var dest = operands[0];

            int a, b;
            if (operands.Count >= 3)
            {
                a = GetRegisterOrImmediate(operands[1]);
                b = GetRegisterOrImmediate(operands[2]);
            }
            else
            {
                a = registers[(int)dest.Value];
                b = GetRegisterOrImmediate(operands[1]);
            }

            if (dest.Type == OperandType.REGISTER)
            {
                int result = a ^ b;
                registers[(int)dest.Value] = result;
                SetFlags(result);
            }
        }

        private void ExecuteNot(List<Operand> operands)
        {
            var dest = operands[0];
            if (dest.Type == OperandType.REGISTER)
            {
                registers[(int)dest.Value] = ~registers[(int)dest.Value];
                SetFlags(registers[(int)dest.Value]);
            }
        }

        private void ExecuteShl(List<Operand> operands)
        {
            var dest = operands[0];

            int a, b;
            if (operands.Count >= 3)
            {
                a = GetRegisterOrImmediate(operands[1]);
                b = GetRegisterOrImmediate(operands[2]);
            }
            else
            {
                a = registers[(int)dest.Value];
                b = GetRegisterOrImmediate(operands[1]);
            }

            if (dest.Type == OperandType.REGISTER)
            {
                int result = a << b;
                registers[(int)dest.Value] = result;
                bool cf = b > 0 && ((a >> (32 - b)) & 1) == 1;
                SetFlags(result, carry: cf);
            }
        }

        private void ExecuteShr(List<Operand> operands)
        {
            var dest = operands[0];

            int a, b;
            if (operands.Count >= 3)
            {
                a = GetRegisterOrImmediate(operands[1]);
                b = GetRegisterOrImmediate(operands[2]);
            }
            else
            {
                a = registers[(int)dest.Value];
                b = GetRegisterOrImmediate(operands[1]);
            }

            if (dest.Type == OperandType.REGISTER)
            {
                int result = a >> b;
                registers[(int)dest.Value] = result;
                bool cf = b > 0 && ((a >> (b - 1)) & 1) == 1;
                SetFlags(result, carry: cf);
            }
        }

        private void ExecuteRol(List<Operand> operands)
        {
            var dest = operands[0];
            int a = registers[(int)dest.Value];
            int b = GetRegisterOrImmediate(operands[1]) & 0x1F; // 取低5位 (0-31)
            if (b == 0) return;
            int result = (int)(((uint)a << b) | ((uint)a >> (32 - b)));
            registers[(int)dest.Value] = result;
            bool cf = (result & 1) != 0;
            SetFlags(result, carry: cf);
        }

        private void ExecuteRor(List<Operand> operands)
        {
            var dest = operands[0];
            int a = registers[(int)dest.Value];
            int b = GetRegisterOrImmediate(operands[1]) & 0x1F;
            if (b == 0) return;
            int result = (int)(((uint)a >> b) | ((uint)a << (32 - b)));
            registers[(int)dest.Value] = result;
            bool cf = ((result >> 31) & 1) != 0;
            SetFlags(result, carry: cf);
        }

        private void ExecuteCmp(List<Operand> operands)
        {
            var dest = operands[0];
            var src  = operands[1];

            int destValue = 0;
            int srcValue  = 0;

            if (dest.Type == OperandType.REGISTER)
            {
                destValue = registers[(int)dest.Value];
            }
            else if (dest.Type == OperandType.IMMEDIATE)
            {
                destValue = OperandToInt(dest);
            }

            if (src.Type == OperandType.REGISTER)
            {
                srcValue = registers[(int)src.Value];
            }
            else if (src.Type == OperandType.IMMEDIATE)
            {
                srcValue = OperandToInt(src);
            }

            int result = destValue - srcValue;
            if (TraceMode) Console.WriteLine($"DEBUG ExecuteCmp: dest={destValue}, src={srcValue}, result={result}, flags: ZF={zf}, SF={sf}, CF={cf}");
            SetFlags(result);
        }

        private void ExecuteTest(List<Operand> operands)
        {
            var dest = operands[0];
            var src  = operands[1];

            int destValue = 0;
            int srcValue  = 0;

            if (dest.Type == OperandType.REGISTER)
            {
                destValue = registers[(int)dest.Value];
            }
            else if (dest.Type == OperandType.IMMEDIATE)
            {
                destValue = OperandToInt(dest);
            }

            if (src.Type == OperandType.REGISTER)
            {
                srcValue = registers[(int)src.Value];
            }
            else if (src.Type == OperandType.IMMEDIATE)
            {
                srcValue = OperandToInt(src);
            }

            int result = destValue & srcValue;
            SetFlags(result);
        }

        private void ExecuteJmp(List<Operand> operands)
        {
            // 查找 LABEL 操作数：条件跳转如 JZ R0 label 将 label 放在 index=1，
            // 无条件跳转 JMP label 将 label 放在 index=0
            var target = operands.FirstOrDefault(o => o.Type == OperandType.LABEL);
            if (target == null)
                target = operands[0]; // fallback: 立即数跳转

            if (target.Type == OperandType.LABEL)
            {
                string labelName = target.Value.ToString();
                if (labelAddresses.ContainsKey(labelName))
                {
                    pc = labelAddresses[labelName];
                }
                else
                {
                    throw new VmlLabelException(labelName);
                }
            }
            else if (target.Type == OperandType.IMMEDIATE)
            {
                pc = (int)target.Value;
            }
        }

        private void ExecuteJl(List<Operand> operands)
        {
            if (TraceMode) Console.WriteLine($"DEBUG ExecuteJl: SF={sf}, CF={cf}, condition={sf != cf}");
            if (sf != cf)
            {
                if (TraceMode) Console.WriteLine($"DEBUG ExecuteJl: 跳转到标签: {operands[0].Value}");
                ExecuteJmp(operands);
            }
            else
            {
                if (TraceMode) Console.WriteLine($"DEBUG ExecuteJl: 不跳转");
            }
        }

        private void ExecuteJle(List<Operand> operands)
        {
            if (TraceMode) Console.WriteLine($"DEBUG ExecuteJle: ZF={zf}, SF={sf}, CF={cf}, condition={zf || (sf != cf)}");
            if (zf || (sf != cf))
            {
                if (TraceMode) Console.WriteLine($"DEBUG ExecuteJle: 跳转到标签: {operands[0].Value}");
                ExecuteJmp(operands);
            }
            else
            {
                if (TraceMode) Console.WriteLine($"DEBUG ExecuteJle: 不跳转");
            }
        }

        private void ExecuteJg(List<Operand> operands)
        {
            if (!zf && (sf == cf))
            {
                ExecuteJmp(operands);
            }
        }

        private void ExecuteJge(List<Operand> operands)
        {
            if (sf == cf)
            {
                ExecuteJmp(operands);
            }
        }

        private void ExecuteCall(List<Operand> operands)
        {
            var target = operands[0];
            // 调试: 追踪所有子程序调用 (仅在 DebugMode 下输出)
            if (DebugMode && target.Type == OperandType.LABEL && target.Value is string lbl)
                Console.Error.WriteLine($"[CALL] {lbl}");

            // 保存返回地址（pc已经指向下一条指令，因为ExecuteInstruction开头会pc++）
            sp            -= 4;
            registers[13] =  sp; // 同步更新R13
            SetMemory(sp, pc);

            if (target.Type == OperandType.LABEL)
            {
                string labelName = target.Value.ToString();
                if (labelAddresses.ContainsKey(labelName))
                {
                    pc = labelAddresses[labelName];
                }
                else
                {
                    throw new System.Collections.Generic.KeyNotFoundException($"未找到标签: {labelName}");
                }
            }
            else if (target.Type == OperandType.REGISTER)
            {
                pc = registers[(int)target.Value];
            }
            else if (target.Type == OperandType.IMMEDIATE)
            {
                pc = (int)target.Value;
            }
        }

        private void ExecuteRet(List<Operand> operands)
        {
            // 从栈中弹出返回地址
            int returnAddress = GetMemory(sp);
            sp            += 4;
            registers[13] =  sp; // 同步更新R13

            // 如果返回地址为0或负数（入口点返回），捕获 R0 退出码并结束程序
            if (returnAddress <= 0)
            {
                ExitCode = registers[0];
                pc = -1;
                return;
            }

            pc = returnAddress;
        }

        private void ExecuteHalt(List<Operand> operands)
        {
            pc = -1; // 设置pc为-1，结束程序执行
        }

        private void ExecuteNop(List<Operand> operands)
        {
            // 空操作，什么都不做
        }

        /// <summary>
        /// 执行分配指令
        /// </summary>
        /// <param name="operands"></param>
        private void ExecuteAlloc(List<Operand> operands)
        {
            var dest = operands[0];
            var size = operands[1];

            int allocSize = 0;
            if (size.Type == OperandType.IMMEDIATE)
            {
                allocSize = (int)size.Value;
            }
            else if (size.Type == OperandType.REGISTER)
            {
                allocSize = registers[(int)size.Value];
            }

            var address = AllocateMemory(allocSize);
            if (dest.Type == OperandType.REGISTER)
            {
                registers[(int)dest.Value] = address;
            }
        }

        private void ExecuteFree(List<Operand> operands)
        {
            var address = operands[0];
            var freeAddress = address.Type switch
            {
                OperandType.IMMEDIATE => (int)address.Value,
                OperandType.REGISTER  => registers[(int)address.Value],
                _                     => 0
            };
            FreeMemory(freeAddress);
        }
        private void ExecuteEnter(List<Operand> operands)
        {
            int size = GetIntOperandValue(operands[0]);
            registers[13] -= 4;
            sp = registers[13];
            SetMemory(registers[13], registers[12]);
            registers[12] = registers[13];
            if (size > 0)
            {
                registers[13] -= size;
                sp = registers[13];
            }
        }

        private void ExecuteLeave(List<Operand> operands)
        {
            registers[13] = registers[12];
            sp = registers[13];
            registers[12] = GetMemory(registers[13]);
            registers[13] += 4;
            sp = registers[13];
        }
        private void ExecuteClc(List<Operand> operands)
        {
            cf = false;
        }

        private void ExecuteStc(List<Operand> operands)
        {
            cf = true;
        }
        private void ExecuteLea(List<Operand> operands)
        {
            int dstReg = GetRegIndex(operands[0]);
            var src = operands[1];
            registers[dstReg] = src.Type switch
            {
                OperandType.MEMORY => ResolveDecodedAddr((DecodedMemAddr)src.Value),
                OperandType.LABEL => labelAddresses.TryGetValue(src.Value.ToString(), out var la) ? la : throw new VmlLabelException(src.Value.ToString()),
                _ => 0
            };
        }
        private void ExecuteBreak(List<Operand> operands)
        {
            Console.WriteLine($"[BREAK] 断点触发 at PC={pc}");
            Console.WriteLine($"  R0={registers[0]} R1={registers[1]} R2={registers[2]} R3={registers[3]}");
            Console.WriteLine($"  R4={registers[4]} R5={registers[5]} R6={registers[6]} R7={registers[7]}");
            Console.WriteLine($"  R8={registers[8]} R9={registers[9]} R10={registers[10]} R11={registers[11]}");
            Console.WriteLine($"  BP={registers[12]} SP={registers[13]} RA={registers[15]}");
            Console.WriteLine($"  Flags: ZF={zf} SF={sf} CF={cf}");
            OnBreakpoint?.Invoke(pc);
        }
        private int GetRegIndex(Operand operand)
        {
            return (int)operand.Value;
        }
        private void ExecuteTrace(List<Operand> operands)
        {
            // TRACE enable
            int enable = GetIntOperandValue(operands[0]);
            DebugMode = (enable != 0);
            if (DebugMode)
                System.Diagnostics.Debug.WriteLine("指令跟踪已启用");
        }

        private void ExecuteDump(List<Operand> operands)
        {
            var target = operands[0];
            if (target.Type == OperandType.REGISTER)
            {
                int regNum = (int)target.Value;
                if (regNum >= 24 && regNum < 32)
                {
                    Console.WriteLine($"L{regNum-24} = {longRegisters[regNum-24]}");
                }
                else if (regNum >= 16 && regNum < 24)
                {
                    Console.WriteLine($"D{regNum-16} = {doubleRegisters[regNum-16]}");
                }
                else if (regNum < 16)
                {
                    Console.WriteLine($"R{regNum} = 0x{registers[regNum]:X8} ({registers[regNum]})");
                }
            }
            else if (target.Type == OperandType.MEMORY)
            {
                int addr = GetMemoryAddress(target);
                Console.WriteLine($"内存地址 0x{addr:X8}:");
                for (int i = 0; i < 16 && addr + i < memory.Length; i++)
                {
                    if (i % 8 == 0) Console.Write($"  {addr+i:X8}: ");
                    Console.Write($"{memory[addr+i]:X2} ");
                    if (i % 8 == 7) Console.WriteLine();
                }
            }
        }

        private void ExecuteRand(List<Operand> operands)
        {
            // RAND dst
            var dst = operands[0];
            int randomValue = random.Next();
            if (dst.Type == OperandType.REGISTER)
            {
                int regNum = (int)dst.Value;
                if (regNum >= 0 && regNum < 16)
                    registers[regNum] = randomValue;
            }
            else if (dst.Type == OperandType.MEMORY)
            {
                int addr = GetMemoryAddress(dst);
                SetMemory(addr, randomValue);
            }
        }

        private void ExecuteSeed(List<Operand> operands)
        {
            // SEED src
            int seed = GetIntOperandValue(operands[0]);
            random = new Random(seed);
        }

        private void ExecuteInt(List<Operand> operands)
        {
            // MCU 模式不支持中断
            if (privilegeLevel > 0) return;

            int vector = GetIntOperandValue(operands[0]);
            // INT 触发的向量遵循其分类：0-7 和 255 为不可恢复
            bool isRecoverable = !InterruptController.IsNonRecoverable(vector);
            SaveInterruptContext(vector, isRecoverable);
        }

        private void ExecuteIret()
        {
            // 恢复上下文
            RestoreInterruptContext();

            // 不可恢复中断 → handler 返回后停机
            if (_nonRecoverableInterrupt)
            {
                _nonRecoverableInterrupt = false;
                Console.Error.WriteLine($"不可恢复中断: 处理器已停机 (PC=0x{pc:X8})");
                pc = -1;
            }
        }

        private void ExecuteThrow(List<Operand> operands)
        {
            int errCode = GetIntOperandValue(operands[0]);
            // 查找最近的 CATCH 处理器
            if (_catchStack.Count > 0)
            {
                var handler = _catchStack.Pop();
                registers[0] = errCode;
                pc = handler.CatchAddress;
                if (handler.FinallyAddress >= 0)
                    _catchStack.Push(new CatchFrame(handler.FinallyAddress, -1));
            }
            else
            {
                Console.WriteLine($"未捕获的异常: error={errCode} at PC={pc}");
                pc = -1;
            }
        }

        private void ExecuteCatch(List<Operand> operands)
        {
            var label = operands[0].Value.ToString();
            int addr = labelAddresses.TryGetValue(label, out var a) ? a : 0;
            _catchStack.Push(new CatchFrame(addr, -1));
        }

        private void ExecuteEndCatch()
        {
            if (_catchStack.Count > 0)
                _catchStack.Pop();
        }

        private VMLAssembler.VmlAssembler? _asmAssembler;

        private void ExecuteAsm(List<Operand> operands)
        {
            if (operands.Count == 0) return;
            var asmContent = operands[0].Value?.ToString() ?? "";
            var lines = asmContent.Split('\n');
            _asmAssembler ??= new VMLAssembler.VmlAssembler();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                var instr = _asmAssembler.ParseLine(trimmed);
                if (instr != null)
                    ExecuteInstruction(instr);
            }
        }

        private int GetIntOperandValue(Operand operand)
        {
            if (operand.Type == OperandType.IMMEDIATE)
                return Convert.ToInt32(operand.Value);
            else if (operand.Type == OperandType.REGISTER)
            {
                int regNum = (int)operand.Value;
                if (regNum >= 0 && regNum < 16)
                    return registers[regNum];
            }
            throw new VmlException($"无法获取整数操作数值: {operand.Type}");
        }
    }
}
