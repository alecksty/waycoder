using VMLAssembler;
using System.IO;
using VMLRuntime.Device;

namespace VMLRuntime
{
    public partial class VmRuntime
    {
        /// <summary>预译码后的内存地址，避免运行时字符串解析</summary>
        private class DecodedMemAddr
        {
            public enum Kind { Absolute, RegisterIndirect, RegPlusOffset, RegMinusOffset, Label, LabelPlusOffset }
            public Kind AddrKind;
            public int BaseReg;
            public int Offset;
            public string Label;
        }

        /// <summary>在 LoadProgram 时对所有 MEMORY/INDIRECT 操作数做一次字符串解析</summary>
        private void PreDecodeMemoryOperands()
        {
            if (_program == null) return;
            foreach (var instr in _program.Instructions)
            {
                foreach (var op in instr.Operands)
                {
                    try
                    {
                        if (op.Type == OperandType.MEMORY && op.Value is string s)
                            op.Value = ParseMemoryString(s);
                        else if (op.Type == OperandType.INDIRECT && op.Value is string istr)
                            op.Value = ParseIndirectString(istr);
                    }
                    catch
                    {
                        // 预解码失败时保留原始字符串值
                    }
                }
            }
        }

        private static int ParseOffset(ReadOnlySpan<char> s)
        {
            if (s.Length > 0 && s[0] == '#')
                s = s.Slice(1);
            if (int.TryParse(s, out var v))
                return v;
            return 0;
        }

        private static DecodedMemAddr ParseMemoryString(string raw)
        {
            var s = raw.Trim('[', ']');
            var d = new DecodedMemAddr();

            // AT&T 风格: -4(R12)
            int paren = s.IndexOf('(');
            if (paren >= 0 && s.EndsWith(")"))
            {
                var offStr = s.Substring(0, paren).Trim();
                var regStr = s.Substring(paren + 1).TrimEnd(')').Trim();
                if (regStr.StartsWith("R") && int.TryParse(regStr.AsSpan(1), out var r))
                {
                    d.AddrKind = offStr.StartsWith('-') ? DecodedMemAddr.Kind.RegMinusOffset : DecodedMemAddr.Kind.RegPlusOffset;
                    d.BaseReg = r;
                    d.Offset = ParseOffset(offStr.TrimStart('-'));
                    // Offset is always stored as positive; RegMinusOffset does base - offset
                    return d;
                }
            }

            int plus = s.IndexOf('+');
            if (plus >= 0)
            {
                var left = s.AsSpan(0, plus).Trim();
                var right = s.AsSpan(plus + 1).Trim();
                if (left.StartsWith("R") && int.TryParse(left[1..], out var r))
                {
                    int off = ParseOffset(right);
                    if (off < 0)
                    {
                        d.AddrKind = DecodedMemAddr.Kind.RegMinusOffset;
                        d.Offset = -off;
                    }
                    else
                    {
                        d.AddrKind = DecodedMemAddr.Kind.RegPlusOffset;
                        d.Offset = off;
                    }
                    d.BaseReg = r;
                    return d;
                }
                if (int.TryParse(left, out var a))
                {
                    d.AddrKind = DecodedMemAddr.Kind.Absolute;
                    d.Offset = ParseOffset(right);
                    d.BaseReg = a;
                    return d;
                }
            }

            int minus = s.IndexOf('-');
            if (minus >= 0)
            {
                var left = s.AsSpan(0, minus).Trim();
                var right = s.AsSpan(minus + 1).Trim();
                if (left.StartsWith("R") && int.TryParse(left[1..], out var r))
                {
                    d.AddrKind = DecodedMemAddr.Kind.RegMinusOffset;
                    d.BaseReg = r;
                    d.Offset = ParseOffset(right);
                    return d;
                }
                if (int.TryParse(left, out var a))
                {
                    d.AddrKind = DecodedMemAddr.Kind.Absolute;
                    d.Offset = -ParseOffset(right);
                    d.BaseReg = a;
                    return d;
                }
            }

            if (s.StartsWith("R") && int.TryParse(s.AsSpan(1), out var rn))
            {
                d.AddrKind = DecodedMemAddr.Kind.RegisterIndirect;
                d.BaseReg = rn;
                return d;
            }

            if (int.TryParse(s, out var abs))
            {
                d.AddrKind = DecodedMemAddr.Kind.Absolute;
                d.BaseReg = abs;
                return d;
            }

            // Label or Label+Offset (e.g. "msg" or "msg+0")
            int lp = s.IndexOfAny(new[] { '+', '-' });
            if (lp > 0)
            {
                d.AddrKind = DecodedMemAddr.Kind.LabelPlusOffset;
                d.Label = s.Substring(0, lp);
                d.Offset = ParseOffset(s.AsSpan(lp));
            }
            else
            {
                d.AddrKind = DecodedMemAddr.Kind.Label;
                d.Label = s;
            }
            return d;
        }

        private static DecodedMemAddr ParseIndirectString(string raw)
        {
            var s = raw.TrimStart('@');
            var d = new DecodedMemAddr();

            // @[R14-0] 或 @[addr] 格式: 括号内为寄存器偏移或绝对地址
            if (s.StartsWith("[") && s.EndsWith("]"))
            {
                var inner = s.AsSpan(1, s.Length - 2).Trim();

                // @[R14-0] 格式: R14-0 → 寄存器偏移
                int minus = inner.IndexOf('-');
                int plus = inner.IndexOf('+');
                if (minus > 0 && inner.StartsWith("R"))
                {
                    var left = inner.Slice(0, minus);
                    var right = inner.Slice(minus + 1);
                    if (int.TryParse(left[1..], out var reg) && int.TryParse(right, out var off))
                    {
                        d.AddrKind = DecodedMemAddr.Kind.RegMinusOffset;
                        d.BaseReg = reg;
                        d.Offset = off;
                        return d;
                    }
                }
                if (plus > 0 && inner.StartsWith("R"))
                {
                    var left = inner.Slice(0, plus);
                    var right = inner.Slice(plus + 1);
                    if (int.TryParse(left[1..], out var reg))
                    {
                        int off = ParseOffset(right);
                        d.AddrKind = DecodedMemAddr.Kind.RegPlusOffset;
                        d.BaseReg = reg;
                        d.Offset = off;
                        return d;
                    }
                }
                // @[label] 或 @[label+offset] 格式: 标签引用
                if (inner.Length > 0 && !char.IsDigit(inner[0]) && inner[0] != '-')
                {
                    int off = 0;
                    string labelName;
                    int sepIdx = inner.IndexOfAny(new[] { '+', '-' });
                    if (sepIdx > 0)
                    {
                        labelName = inner.Slice(0, sepIdx).Trim().ToString();
                        off = ParseOffset(inner.Slice(inner[sepIdx] == '+' ? sepIdx + 1 : sepIdx));
                    }
                    else
                    {
                        labelName = inner.ToString();
                    }
                    d.AddrKind = DecodedMemAddr.Kind.LabelPlusOffset;
                    d.Label = labelName;
                    d.Offset = off;
                    return d;
                }
                // @[addr] 格式: 绝对地址
                if (int.TryParse(inner, out var addr))
                {
                    d.AddrKind = DecodedMemAddr.Kind.Absolute;
                    d.BaseReg = addr;
                }
                return d;
            }

            // Bare label name (e.g. `msg` or `msg+0` without brackets)
            if (s.Length > 0 && !char.IsDigit(s[0]) && !s.StartsWith("R"))
            {
                int sepIdx = s.IndexOfAny(new[] { '+', '-' });
                if (sepIdx > 0)
                {
                    d.AddrKind = DecodedMemAddr.Kind.LabelPlusOffset;
                    d.Label = s.Substring(0, sepIdx);
                    d.Offset = ParseOffset(s.AsSpan(sepIdx));
                }
                else
                {
                    d.AddrKind = DecodedMemAddr.Kind.Label;
                    d.Label = s;
                }
                return d;
            }

            // 处理 @R14 或 @R14-0 格式 (括号已被ParseOperand剥离)
            if (s.StartsWith("R") && s.Length > 1)
            {
                // @R14-0 → 寄存器偏移
                int minus = s.IndexOf('-');
                int plus = s.IndexOf('+');
                if (minus > 0)
                {
                    var left = s.AsSpan(0, minus);
                    var right = s.AsSpan(minus + 1);
                    if (int.TryParse(left[1..], out var reg))
                    {
                        int off = ParseOffset(right);
                        d.AddrKind = DecodedMemAddr.Kind.RegMinusOffset;
                        d.BaseReg = reg;
                        d.Offset = off;
                        return d;
                    }
                }
                // @R14 → 纯寄存器间接
                if (int.TryParse(s.AsSpan(1), out var r))
                {
                    d.AddrKind = DecodedMemAddr.Kind.RegisterIndirect;
                    d.BaseReg = r;
                }
                return d;
            }

            return d;
        }

        private int GetAddress(Operand operand)
        {
            if (operand.Type == OperandType.MEMORY)
            {
                // 优先使用预译码的结构化地址
                if (operand.Value is DecodedMemAddr dma)
                    return ResolveDecodedAddr(dma);

                if (operand.Value is int address)
                    return address;
                
                // 处理 "R14-0" 格式的寄存器偏移地址或标签名
                if (operand.Value is string strAddr)
                {
                    int addr = ResolveRegOffsetString(strAddr);
                    // 如果是纯标签名（如 "var_x"），通过标签表解析
                    if (addr == 0 && labelAddresses != null && labelAddresses.ContainsKey(strAddr))
                        addr = labelAddresses[strAddr];
                    return addr;
                }
            }
            else if (operand.Type == OperandType.INDIRECT)
            {
                if (operand.Value is DecodedMemAddr dma)
                {
                    // @R1, @[R14-4] — 返回寄存器中保存的地址（直接作为地址使用）
                    return ResolveDecodedAddr(dma);
                }

                // 处理 int 值: @0/@1/@2... → 返回对应寄存器的值作为地址
                if (operand.Value is int regIdx)
                {
                    return registers[regIdx];
                }

                // 处理 "R14-0" 格式的寄存器偏移间接地址（raw string fallback）
                if (operand.Value is string strAddr)
                {
                    int addr = ResolveRegOffsetString(strAddr);
                    return addr;
                }
            }
            return 0;
        }

        private int ResolveRegOffsetString(string str)
        {
            // 去除可能的括号（兼容 [R14-4] 或 [-4(R12)] 格式）
            if (str.StartsWith("[") && str.EndsWith("]"))
                str = str.Substring(1, str.Length - 2);
            // AT&T 语法: "-4(R12)" 或 "8(R12)" 或 "(R12)"
            if (str.Contains("(") && str.Contains(")"))
            {
                int parenOpen = str.IndexOf('(');
                int parenClose = str.IndexOf(')');
                string offsetPart = parenOpen > 0 ? str.Substring(0, parenOpen) : "0";
                string regPart = str.Substring(parenOpen + 1, parenClose - parenOpen - 1);
                if (regPart.Length >= 2 && regPart[0] == 'R' && char.IsDigit(regPart[1]))
                {
                    int ri = 1;
                    while (ri < regPart.Length && char.IsDigit(regPart[ri])) ri++;
                    int regNum = int.Parse(regPart.Substring(1, ri - 1));
                    int offset = int.Parse(offsetPart.TrimStart('#'));
                    return registers[regNum] + offset;
                }
            }
            // 格式: "R<N>-<offset>" 或 "R<N>+<offset>" 或 "R<N>"
            if (str.Length >= 2 && str[0] == 'R' && char.IsDigit(str[1]))
            {
                int i = 1;
                while (i < str.Length && char.IsDigit(str[i])) i++;
                if (i == str.Length)
                {
                    // 裸寄存器名如 "R0" — 返回寄存器值作为地址
                    int regNum = int.Parse(str.Substring(1));
                    return registers[regNum];
                }
                if (i > 1 && i < str.Length && (str[i] == '-' || str[i] == '+'))
                {
                    int regNum = int.Parse(str.Substring(1, i - 1));
                    int offset = int.Parse(str.Substring(i + 1).TrimStart('#'));
                    return registers[regNum] + (str[i] == '-' ? -offset : offset);
                }
            }
            return 0;
        }

        private int ResolveDecodedAddr(DecodedMemAddr d)
        {
            switch (d.AddrKind)
            {
                case DecodedMemAddr.Kind.Absolute:
                    return d.BaseReg;
                case DecodedMemAddr.Kind.RegisterIndirect:
                    return registers[d.BaseReg];
                case DecodedMemAddr.Kind.RegPlusOffset:
                    return registers[d.BaseReg] + d.Offset;
                case DecodedMemAddr.Kind.RegMinusOffset:
                    return registers[d.BaseReg] - d.Offset;
                case DecodedMemAddr.Kind.Label:
                    return labelAddresses.TryGetValue(d.Label, out var addr) ? addr : 0;
                case DecodedMemAddr.Kind.LabelPlusOffset:
                    return labelAddresses.TryGetValue(d.Label, out var laddr) ? laddr + d.Offset : 0;
                default:
                    return 0;
            }
        }

        private void SetFlags(int value, bool? carry = null)
        {
            zf = (value == 0);
            sf = (value < 0);
            cf = carry ?? false;
        }

        private int AllocateMemory(int size)
        {
            // 检查分配大小限制（最大16MB）
            const int MAX_ALLOC_SIZE = 16 * 1024 * 1024;

            if (size is <= 0 or > MAX_ALLOC_SIZE)
            {
                throw new VmlException($"无效的分配大小：{size}，有效范围：1-{MAX_ALLOC_SIZE}");
            }

            // 尝试从空闲块中查找合适大小的块
            int bestFitAddress = -1;
            int bestFitSize    = int.MaxValue;

            foreach (var block in freeBlocks)
            {
                if (block.Value >= size && block.Value < bestFitSize)
                {
                    bestFitAddress = block.Key;
                    bestFitSize    = block.Value;
                }
            }

            if (bestFitAddress >= _config.DataBase)
            {
                // 找到合适的空闲块（必须在数据段基址之上，防止覆盖 IVT/代码段）
                freeBlocks.Remove(bestFitAddress);

                // 如果空闲块比需要的大，将剩余部分放回空闲列表
                int remaining = bestFitSize - size;
                if (remaining > 0)
                {
                    freeBlocks[bestFitAddress + size] = remaining;
                }

                memoryAllocations[bestFitAddress] = size;
                return bestFitAddress;
            }

            // 没有合适的空闲块，从堆中分配
            if (memoryAllocPtr + size >= memory.Length)
            {
                throw new VmlException("内存不足，无法分配!");
            }

            var address = memoryAllocPtr;

            // 检查是否与任何保留区域重叠，若重叠则跳过
            foreach (var reserved in memoryAllocations)
            {
                int rStart = reserved.Key;
                int rEnd = reserved.Key + reserved.Value;
                // 如果分配地址在保留区域内（或跨过保留区域起始），跳过
                if (address + size > rStart && address < rEnd)
                {
                    address = rEnd;
                    memoryAllocPtr = address;
                }
            }

            memoryAllocPtr = address + size;
            // 记录内存分配
            memoryAllocations[address] = size;
            return address;
        }

        /// <summary>
        /// 保留内存区域，标记为已分配，防止堆分配器覆盖（用于保护字库/寄存器等固定地址）
        /// </summary>
        public void ReserveMemoryRegion(int address, int size)
        {
            if (address < 0 || size <= 0 || address + size > memory.Length) return;
            // 标记为已分配，防止 freeBlocks 覆盖
            memoryAllocations[address] = size;
            // 确保分配指针跳过保留区域（防止后续分配覆盖）
            if (memoryAllocPtr >= address && memoryAllocPtr < address + size)
            {
                memoryAllocPtr = address + size;
            }
        }

        private void FreeMemory(int address)
        {
            if (memoryAllocations.ContainsKey(address))
            {
                int size = memoryAllocations[address];
                memoryAllocations.Remove(address);

                // 将释放的内存块添加到空闲列表
                freeBlocks[address] = size;

                // 清零释放的内存区域（安全措施）
                for (int i = 0; i < size && address + i < memory.Length; i++)
                {
                    memory[address + i] = 0;
                }

                // 合并相邻的空闲块
                MergeAdjacentFreeBlocks();
            }
        }

        private void MergeAdjacentFreeBlocks()
        {
            var sortedBlocks = freeBlocks.OrderBy(b => b.Key).ToList();
            var toRemove     = new List<int>();

            int i = 0;
            while (i < sortedBlocks.Count)
            {
                int startAddr = sortedBlocks[i].Key;
                int totalSize = sortedBlocks[i].Value;
                toRemove.Add(startAddr);

                int j = i + 1;
                while (j < sortedBlocks.Count && sortedBlocks[j].Key == startAddr + totalSize)
                {
                    totalSize += sortedBlocks[j].Value;
                    toRemove.Add(sortedBlocks[j].Key);
                    j++;
                }

                if (j > i + 1)
                {
                    // 有合并，写入合并后的块
                    foreach (var addr in toRemove)
                        freeBlocks.Remove(addr);
                    freeBlocks[startAddr] = totalSize;
                    toRemove.Clear();
                }

                i = j;
            }
        }

        public int GetProgramCounter()
        {
            return pc;
        }

        public void SetMemoryByte(int address, byte value)
        {
            uint addr = (uint)address;
            if (_deviceManager.TryRouteMmioWrite(addr, value, 1))
                return;

            CheckMemBounds(address, 1);
            CheckMemoryAccess(address, 1, isWrite: true);
            memory[address] = value;
            ApplyVgaConfigChange(address);
        }

        private void CheckMemBounds(int address, int size)
        {
            if (address < 0 || address + size > memory.Length)
                throw new VmlMemoryException($"内存访问越界，地址：{address:X8}", (uint)Math.Max(0, address));
        }

        public byte GetMemoryByte(int address)
        {
            uint addr = (uint)address;
            if (_deviceManager.TryRouteMmioRead(addr, 1, out var mmioValue))
                return (byte)mmioValue;
            CheckMemBounds(address, 1);
            CheckMemoryAccess(address, 1, isRead: true);
            return memory[address];
        }

        public ushort GetMemoryWord(int address)
        {
            uint addr = (uint)address;
            if (_deviceManager.TryRouteMmioRead(addr, 2, out var mmioValue))
                return (ushort)mmioValue;
            CheckMemBounds(address, 2);
            CheckMemoryAccess(address, 2, isRead: true);
            return (ushort)(memory[address] | (memory[address + 1] << 8));
        }

        public void SetMemoryWord(int address, ushort value)
        {
            uint addr = (uint)address;
            if (_deviceManager.TryRouteMmioWrite(addr, value, 2))
                return;
            CheckMemBounds(address, 2);
            CheckMemoryAccess(address, 2, isWrite: true);
            memory[address] = (byte)(value & 0xFF);
            memory[address + 1] = (byte)((value >> 8) & 0xFF);
            ApplyVgaConfigChange(address);
        }

        // 断点管理
        public void AddBreakpoint(int    address) => breakpoints.Add(address);
        public void RemoveBreakpoint(int address) => breakpoints.Remove(address);
        public void ClearBreakpoints()            => breakpoints.Clear();
        public bool HasBreakpoint(int address)    => breakpoints.Contains(address);

        // 继续执行（用于调试模式）
        public void Continue()
        {
            if (_program != null && pc >= 0 && pc < _program.Instructions.Count)
            {
                pc++; // 跳过当前断点
                Run();
            }
        }

        private int GetMemory(int address)
        {
            uint addr = (uint)address;

            // MMIO 路由优先
            if (_deviceManager.TryRouteMmioRead(addr, 4, out var mmioValue))
                return (int)mmioValue;

            // 检查内存保护
            CheckMemoryAccess(address, 4, isRead: true);
            
            // 统一的内存读取边界检查（容错）
            if (address < 0)
            {
                throw new VmlMemoryException($"内存访问越界，负地址：{address:X8}", (uint)Math.Max(0, address));
            }
            // Partial read: return whatever bytes are available, pad with 0
            int result = 0;
            for (int i = 0; i < 4 && address + i >= 0 && address + i < memory.Length; i++)
                result |= memory[address + i] << (i * 8);
            return result;
        }

        private void SetMemory(int address, int value)
        {
            uint addr = (uint)address;

            // MMIO 路由优先
            if (_deviceManager.TryRouteMmioWrite(addr, (ulong)value, 4))
                return;

            // 检查内存保护
            CheckMemoryAccess(address, 4, isWrite: true);
            
            // 统一的内存写入边界检查（容错）
            if (address < 0)
            {
                throw new VmlMemoryException($"内存访问越界，负地址：{address:X8}", (uint)Math.Max(0, address));
            }
            // Partial write: write whatever bytes fit within memory
            for (int i = 0; i < 4 && address + i >= 0 && address + i < memory.Length; i++)
                memory[address + i] = (byte)((value >> (i * 8)) & 0xFF);
            ApplyVgaConfigChange(address);
        }
        /// <summary>
        /// 检查内存访问权限
        /// </summary>
        /// <param name="address">内存地址</param>
        /// <param name="size">访问大小</param>
        /// <param name="isRead">是否为读取操作</param>
        /// <param name="isWrite">是否为写入操作</param>
        /// <param name="isExecute">是否为执行操作</param>
        private void CheckMemoryAccess(int address, int size, bool isRead = false, bool isWrite = false, bool isExecute = false)
        {
            // 基本边界检查（容错：边界处读写只发出警告不终止）
            if (address < 0)
                throw new VmlMemoryException($"内存访问越界：地址={address:X8}, 大小={size}", (uint)Math.Max(0, address));
            if (address >= memory.Length)
            throw new VmlMemoryException($"内存访问越界：地址={address:X8}, 大小={size}", (uint)address);

            // 检查内存保护区域
            foreach (var region in memoryRegions)
            {
                if (address >= region.Start && address + size <= region.Start + region.Size)
                {
                    // 地址在保护区域内
                    if (isRead && !region.Readable)
                        throw new VmlMemoryException($"内存读取保护：地址={address:X8}", (uint)Math.Max(0, address));
                    if (isWrite && !region.Writable)
                        throw new VmlMemoryException($"内存写入保护：地址={address:X8}", (uint)Math.Max(0, address));
                    if (isExecute && !region.Executable)
                        throw new VmlMemoryException($"内存执行保护：地址={address:X8}", (uint)Math.Max(0, address));
                    return; // 检查通过
                }
            }

            // 默认权限：所有区域都可读写执行（如果没有设置保护）
        }

        /// <summary>
        /// 设置内存保护
        /// </summary>
        /// <param name="start">起始地址</param>
        /// <param name="size">区域大小</param>
        /// <param name="readable">可读</param>
        /// <param name="writable">可写</param>
        /// <param name="executable">可执行</param>
        private void SetMemoryProtection(int start, int size, bool readable, bool writable, bool executable)
        {
            // 移除重叠的保护区域
            memoryRegions.RemoveAll(r => 
                (r.Start >= start && r.Start < start + size) ||
                (start >= r.Start && start < r.Start + r.Size));

            // 添加新的保护区域
            memoryRegions.Add(new MemoryRegion
            {
                Start = start,
                Size = size,
                Readable = readable,
                Writable = writable,
                Executable = executable
            });
        }

        /// <summary>
        /// 移除内存保护
        /// </summary>
        /// <param name="start">起始地址</param>
        private void RemoveMemoryProtection(int start)
        {
            memoryRegions.RemoveAll(r => r.Start == start);
        }

        /// <summary>当程序写入 VGA 控制寄存器 (0x6FF0-0x6FFF) 时，自动重新配置 VGA 设备</summary>
        private void ApplyVgaConfigChange(int address)
        {
            if (address >= 0x6FF0 && address <= 0x6FFF && _deviceManager.FindDevice("vga") is VmDisplayDevice vgaDev)
            {
                int vgaMode = memory.Length > 0x6FF0 ? memory[0x6FF0] : 0;
                System.Diagnostics.Debug.WriteLine($"[VGA TRACE] Write to 0x{address:X4}, mode=0x{vgaMode:X2}");
                vgaDev.ApplyVgaMode(vgaMode);
                vgaWidth = vgaDev.Width;
                vgaHeight = vgaDev.Height;
            }
        }
    }
}
