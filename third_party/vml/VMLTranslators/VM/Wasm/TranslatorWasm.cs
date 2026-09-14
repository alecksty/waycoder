using VMLAssembler;
using System.Text;

namespace VMLTranslators
{
    /// <summary>
    /// WebAssembly 翻译器 — VML → Wat (WebAssembly Text Format)
    ///
    /// Wasm 执行模型:
    ///   - 栈式虚拟机（0 个通用寄存器，全部通过操作数栈）
    ///   - 局部变量 i32 (VML R0-R15 → wasm local $r0-$r15)
    ///   - 浮点局部变量 f32/f64 (VML F0-F15 → wasm local $f0-$f15)
    ///   - 线性内存 (VML memory → wasm memory)
    ///   - 控制流: block/loop/br/br_if/if/else/end
    ///
    /// 映射策略:
    ///   VML LOAD R0, #42    → i32.const 42   local.set $r0
    ///   VML ADD R0, R1      → local.get $r1   local.get $r0   i32.add   local.set $r0
    ///   VML STORE R0, [R12-4] → local.get $r0   local.get $r12  i32.const -4  i32.add  i32.store
    ///   VML JMP label        → br $label
    ///   VML CALL func        → call $func
    ///   VML RET              → return
    /// </summary>
    public partial class TranslatorWasm : TranslatorVM
    {
        public override string ARCH_NAME => "Wasm";

        protected override void EmitCall(Instruction instr)
            => Indent($"  call $__{instr.Opcode.ToString().ToLower()}");

        // 缩进级别
        private int _indent = 2;

        // 需要在 module 中 import 的函数
        private readonly HashSet<string> _imports = new();

        // 函数 epilogue 标签 — JMP 到这些标签直接转成 return
        private readonly HashSet<string> _returnLabels = new();

        // 所有定义的标签(用于 br 跳转)
        private readonly HashSet<string> _definedLabels = new();

        // 循环标签 → 循环体最后一条指令的地址 (loop body end addr)
        private readonly Dictionary<string, int> _loopEndAddrs = new();

        // block-only 标签 (纯 forward 跳转目标) → block 开始地址 (earliest branch source)
        private readonly Dictionary<string, int> _blockStartAddrs = new();

        // block-only 标签 (纯 forward 跳转目标) → block 结束地址 (label target address)
        private readonly Dictionary<string, int> _blockEndAddrs = new();

        // block-only 标签 (纯 forward 跳转目标)
        private readonly HashSet<string> _blockLabels = new();

        // 保存 BP/SP 的内存地址 (用于跨 WAT 函数调用)
        private const int BP_MEM_ADDR = 0x4F30;
        private const int SP_MEM_ADDR = 0x4F34;
        private const int PARAM0_MEM_ADDR = 0x4F38;
        private const int PARAM1_MEM_ADDR = 0x4F3C;
        private const int PARAM2_MEM_ADDR = 0x4F40;
        private const int PARAMF0_MEM_ADDR = 0x4F44;
        private const int PARAMF1_MEM_ADDR = 0x4F48;
        private const int CMP_LEFT_ADDR = 0x4F50;
        private const int CMP_RIGHT_ADDR = 0x4F54;

        // 所有函数入口标签
        private readonly HashSet<string> _functionEntries = new();

        public TranslatorWasm(VmlProgram vmlProgram) : base(vmlProgram) { }

        protected override void EmitHeader()
        {
            Emit("(module");
            _indent = 2;
            Indent(";; WebAssembly Text Format — translated from VML");
            Indent(";; VML registers mapped to wasm locals:");
            Indent(";;   R0-R15  → $r0-$r15   (i32)");
            Indent(";;   F0-F15  → $f0-$f15  (f32)");
            Indent(";;   D0-D7   → $d0-$d7   (f64)");
            Indent(";; VML Stack/BP → $sp (i32) / $bp (i32)");
            Indent("");

            // Import fd_write for SYSCALL output (must come before memory definition)
            Indent(";; Import WASI fd_write for console output (SYSCALL #4/#1)");
            Indent("(import \"wasi_unstable\" \"fd_write\" (func $fd_write (param i32 i32 i32 i32) (result i32)))");
            Indent("(import \"wasi_unstable\" \"proc_exit\" (func $proc_exit (param i32)))");
            Indent("");

            // Memory definition (after imports, before functions)
            Indent("(memory (export \"memory\") 256 256)");
            Indent("");
        }

        protected override void EmitFooter()
        {
            Emit(")");
        }

        protected override void EmitDataSection()
        {
            if (VmlProgram.DataSection.Count == 0) return;

            Indent(";; --- Data Section ---");
            Indent("(data (i32.const 0x400) ;; start at data segment base");

            int addr = 0x400;
            var sb = new StringBuilder();
            foreach (var kvp in VmlProgram.DataSection)
            {
                LabelMap[kvp.Key] = addr.ToString();
                if (kvp.Value is int intVal)
                {
                    sb.Append("\\" + (intVal & 0xFF).ToString("X2"));
                    sb.Append("\\" + ((intVal >> 8) & 0xFF).ToString("X2"));
                    sb.Append("\\" + ((intVal >> 16) & 0xFF).ToString("X2"));
                    sb.Append("\\" + ((intVal >> 24) & 0xFF).ToString("X2"));
                    addr += 4;
                }
                else if (kvp.Value is int[] intArr)
                {
                    foreach (var elem in intArr)
                    {
                        sb.Append("\\" + (elem & 0xFF).ToString("X2"));
                        sb.Append("\\" + ((elem >> 8) & 0xFF).ToString("X2"));
                        sb.Append("\\" + ((elem >> 16) & 0xFF).ToString("X2"));
                        sb.Append("\\" + ((elem >> 24) & 0xFF).ToString("X2"));
                        addr += 4;
                    }
                }
                else if (kvp.Value is string strVal)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(strVal);
                    foreach (var b in bytes)
                        sb.Append("\\" + b.ToString("X2"));
                    sb.Append("\\00"); // NUL terminator
                    addr += bytes.Length + 1;
                }
                else if (kvp.Value is object[] objArr)
                {
                    foreach (var elem in objArr)
                    {
                        if (elem is int ei)
                        {
                            sb.Append("\\" + (ei & 0xFF).ToString("X2"));
                            sb.Append("\\" + ((ei >> 8) & 0xFF).ToString("X2"));
                            sb.Append("\\" + ((ei >> 16) & 0xFF).ToString("X2"));
                            sb.Append("\\" + ((ei >> 24) & 0xFF).ToString("X2"));
                            addr += 4;
                        }
                    }
                }
            }
            Indent($"  \"{sb}\")");
            Indent("");
        }

        protected override void EmitCode()
        {
            CollectLabels();
            CollectReturnLabels();
            AnalyzeLoopStructure();
            CollectFunctionEntries();
            EmitHelperFunctions();
            EmitInitFunction();

            // Emit each function as a separate WAT function
            var entryName = VmlProgram.EntryPoint ?? "main";
            var funcList = _functionEntries
                .Where(f => VmlProgram.Labels != null && VmlProgram.Labels.TryGetValue(f, out _))
                .Select(f => (name: f, addr: VmlProgram.Labels![f]))
                .OrderBy(x => x.addr)
                .ToList();

            if (funcList.Count == 0)
            {
                // Fallback: emit all as one function
                EmitFallbackFunction();
                return;
            }

            // Compute end addresses
            var funcRanges = new List<(string name, int start, int end)>();
            for (int i = 0; i < funcList.Count; i++)
            {
                int end = (i + 1 < funcList.Count) ? funcList[i + 1].addr : int.MaxValue;
                funcRanges.Add((funcList[i].name, funcList[i].addr, end));
            }

            Indent("");

            foreach (var func in funcRanges)
            {
                EmitWasmFunction(func.name, func.start, func.end, func.name == entryName);
            }

        }

        private void CollectLabels()
        {
            if (VmlProgram.Labels != null)
                foreach (var kv in VmlProgram.Labels)
                    _definedLabels.Add(kv.Key);
        }

        private void EmitInitFunction()
        {
            Indent(";; --- Memory initialization ---");
            Indent("(func (export \"_initialize\")");
            Indent("  nop");
            Indent(")");
            Indent("");
        }

        /// <summary>
        /// 收集所有函数入口标签 (entry point + CALL 目标)
        /// </summary>
        private void CollectFunctionEntries()
        {
            var entryName = VmlProgram.EntryPoint ?? "main";
            _functionEntries.Add(entryName);

            foreach (var instr in VmlProgram.Instructions)
            {
                if (instr.Opcode == OpCode.CALL)
                {
                    var target = instr.Operands[0].Value?.ToString();
                    if (!string.IsNullOrEmpty(target))
                        _functionEntries.Add(target);
                }
            }
        }

        /// <summary>
        /// 为一个 VML 函数生成单独的 WAT 函数
        /// </summary>
        private void EmitWasmFunction(string funcName, int startAddr, int endAddr, bool isExported)
        {
            var cleanName = SanitizeLabel(funcName);
            var exportAttr = isExported ? $" (export \"{funcName}\")" : "";
            Indent($";; === Function: {funcName} (0x{startAddr:X4} - 0x{endAddr:X4}) ===");
            Indent($"(func ${cleanName}{exportAttr} (result i32)");
            EmitLocals();
            Indent("");

            // Non-entry functions: load BP/SP from memory
            if (!isExported)
            {
                Indent("  ;; Load BP, SP from shared memory");
                Indent($"  i32.const {BP_MEM_ADDR}");
                Indent("  i32.load");
                Indent("  local.set $bp");
                Indent($"  i32.const {SP_MEM_ADDR}");
                Indent("  i32.load");
                Indent("  local.set $sp");
                Indent("  ;; Load param regs R0, R1, R2 from shared memory");
                Indent($"  i32.const {PARAM0_MEM_ADDR}");
                Indent("  i32.load");
                Indent("  local.set $r0");
                Indent($"  i32.const {PARAM1_MEM_ADDR}");
                Indent("  i32.load");
                Indent("  local.set $r1");
                Indent($"  i32.const {PARAM2_MEM_ADDR}");
                Indent("  i32.load");
                Indent("  local.set $r2");
                Indent("  ;; Load float param regs F0, F1 from shared memory");
                Indent($"  i32.const {PARAMF0_MEM_ADDR}");
                Indent("  f32.load");
                Indent("  local.set $f0");
                Indent($"  i32.const {PARAMF1_MEM_ADDR}");
                Indent("  f32.load");
                Indent("  local.set $f1");
                Indent("");
            }
            else
            {
                // Entry function: initialize SP and BP
                Indent("  ;; Initialize stack pointers");
                Indent("  i32.const 0xA0000  ;; SP = 640K");
                Indent("  local.tee $sp");
                Indent("  local.set $bp");
                Indent("");
            }

            // Build labelAtAddr for this function's range
            var labelAtAddr = new Dictionary<int, List<string>>();
            if (VmlProgram.Labels != null)
            {
                foreach (var kv in VmlProgram.Labels)
                {
                    if (kv.Value >= startAddr && kv.Value < endAddr)
                    {
                        if (!labelAtAddr.ContainsKey(kv.Value))
                            labelAtAddr[kv.Value] = new List<string>();
                        labelAtAddr[kv.Value].Add(kv.Key);
                    }
                }
            }

            // Filter block/loop data for this function
            var funcBlockStartAddrs = new Dictionary<string, int>();
            var funcBlockEndAddrs = new Dictionary<string, int>();
            var funcLoopEndAddrs = new Dictionary<string, int>();
            var funcBlockLabels = new HashSet<string>();

            foreach (var bl in _blockLabels)
            {
                if (!VmlProgram.Labels!.TryGetValue(bl, out var a)) continue;
                if (a < startAddr || a >= endAddr) continue;
                funcBlockLabels.Add(bl);
                if (_blockStartAddrs.TryGetValue(bl, out var bs) && bs >= startAddr)
                    funcBlockStartAddrs[bl] = bs;
                if (_blockEndAddrs.TryGetValue(bl, out var be))
                    funcBlockEndAddrs[bl] = be;
            }
            foreach (var kv in _loopEndAddrs)
            {
                if (VmlProgram.Labels!.TryGetValue(kv.Key, out var a) && a >= startAddr && a < endAddr)
                    funcLoopEndAddrs[kv.Key] = kv.Value;
            }

            // Build "open blocks at address" map with proper nesting for overlapping ranges
            // Strategy: when blocks overlap [src, tgt], they must open at the same address
            // and close in LIFO order (outermost = widest range, closes last).
            // Additionally, ensure no block's end falls inside a later-opened loop.
            var blockEntries = funcBlockStartAddrs
                .Select(kv => (label: kv.Key, src: kv.Value, tgt: funcBlockEndAddrs[kv.Key]))
                .OrderBy(e => e.src)
                .ToList();

            // Build loop ranges: [labelAddr, endAddr]
            var loopRanges = new List<(string label, int start, int end)>();
            foreach (var kv in funcLoopEndAddrs)
            {
                if (VmlProgram.Labels!.TryGetValue(kv.Key, out var loopAddr))
                    loopRanges.Add((kv.Key, loopAddr, kv.Value));
            }
            loopRanges.Sort((a, b) => a.start.CompareTo(b.start));

            // Collect all instruction addresses for finding "just before" an address
            var allAddrs = new HashSet<int>();
            foreach (var instr in VmlProgram.Instructions)
                allAddrs.Add(instr.Address);

            // Adjust block ends: if a block's end is at or after a loop's start,
            // and the loop starts after the block opens, move the block end to
            // just before the loop. This prevents the block's 'end' from
            // incorrectly closing the loop in WASM's LIFO ordering.
            // For each block that opens before a loop, adjust its end to be
            // before the EARLIEST loop it overlaps with.
            var blockMinEnd = new Dictionary<string, int>();
            foreach (var loop in loopRanges)
            {
                int beforeLoop = allAddrs.Where(a => a < loop.start).DefaultIfEmpty(loop.start - 1).Max();
                foreach (var be in blockEntries)
                {
                    if (be.src < loop.start && be.tgt >= loop.start)
                    {
                        int newEnd = Math.Max(be.src, beforeLoop);
                        if (!blockMinEnd.ContainsKey(be.label) || newEnd < blockMinEnd[be.label])
                            blockMinEnd[be.label] = newEnd;
                    }
                }
            }
            foreach (var kv in blockMinEnd)
                funcBlockEndAddrs[kv.Key] = kv.Value;

            // Rebuild block entries after adjustment
            blockEntries = funcBlockStartAddrs
                .Select(kv => (label: kv.Key, src: kv.Value, tgt: funcBlockEndAddrs[kv.Key]))
                .OrderBy(e => e.src)
                .ToList();

            // Build openBlocksAt with overlap merging: when a block's source
            // falls inside another block's [src, end], merge their open addresses
            // so WASM LIFO nesting is correct.
            var openBlocksAt = new Dictionary<int, List<string>>();
            foreach (var entry in blockEntries)
            {
                int openAddr = entry.src;
                // If this block opens inside any existing block, move it to the
                // outermost enclosing block's open address for proper LIFO nesting.
                foreach (var kv in openBlocksAt)
                {
                    foreach (var existingLabel in kv.Value)
                    {
                        if (funcBlockEndAddrs.TryGetValue(existingLabel, out var existingEnd)
                            && entry.src >= kv.Key && entry.src < existingEnd)
                        {
                            openAddr = kv.Key;
                            break;
                        }
                    }
                    if (openAddr != entry.src) break;
                }
                if (!openBlocksAt.ContainsKey(openAddr))
                    openBlocksAt[openAddr] = new List<string>();
                openBlocksAt[openAddr].Add(entry.label);
            }
            // Sort co-located blocks by end address descending for proper nesting
            foreach (var kv in openBlocksAt)
                kv.Value.Sort((a, b) => funcBlockEndAddrs[b].CompareTo(funcBlockEndAddrs[a]));

            // Build a stack to track open constructs for LIFO-correct closing
            var constructStack = new Stack<string>();
            // Map label → address where it should close
            var closeAtAddr = new Dictionary<int, List<string>>();

            // Populate closeAtAddr for blocks
            foreach (var kv in funcBlockEndAddrs)
            {
                if (!closeAtAddr.ContainsKey(kv.Value))
                    closeAtAddr[kv.Value] = new List<string>();
                closeAtAddr[kv.Value].Add(kv.Key);
            }
            // Populate closeAtAddr for loops
            foreach (var kv in funcLoopEndAddrs)
            {
                if (!closeAtAddr.ContainsKey(kv.Value))
                    closeAtAddr[kv.Value] = new List<string>();
                closeAtAddr[kv.Value].Add("__loop__" + kv.Key);
            }

            // Process instructions
            foreach (var instr in VmlProgram.Instructions)
            {
                if (instr.Address < startAddr || instr.Address >= endAddr) continue;

                // 1. Close blocks ending at this address (innermost first for LIFO)
                var blocksToClose = new List<string>();
                foreach (var be in funcBlockEndAddrs)
                    if (be.Value == instr.Address)
                        blocksToClose.Add(be.Key);
                // Sort: innermost (narrower end-start range) closes first
                blocksToClose.Sort((a, b) => {
                    int rangeA = funcBlockEndAddrs[a] - (funcBlockStartAddrs.TryGetValue(a, out var sa) ? sa : 0);
                    int rangeB = funcBlockEndAddrs[b] - (funcBlockStartAddrs.TryGetValue(b, out var sb) ? sb : 0);
                    return rangeA.CompareTo(rangeB);
                });
                foreach (var bn in blocksToClose)
                {
                    Indent($"  end ;; block end ({bn})");
                    funcBlockEndAddrs.Remove(bn);
                }

                // 2. Open blocks starting at this address
                if (openBlocksAt.TryGetValue(instr.Address, out var blocksToOpen))
                {
                    foreach (var bn in blocksToOpen)
                    {
                        var clean = SanitizeLabel(bn);
                        Indent($"  block ${clean} ;; block start ({bn})");
                    }
                    openBlocksAt.Remove(instr.Address);
                }

                // 3. Handle labels at this address
                if (labelAtAddr.TryGetValue(instr.Address, out var labelList))
                {
                    foreach (var lbl in labelList)
                    {
                        if (_returnLabels.Contains(lbl))
                            Indent($"  ;; --- {lbl} (epilogue) ---");
                        else if (funcLoopEndAddrs.ContainsKey(lbl))
                        {
                            var clean = SanitizeLabel(lbl);
                            Indent($"  loop ${clean} ;; loop start ({lbl})");
                        }
                        else if (funcBlockLabels.Contains(lbl))
                        {
                            // Block labels at their own address are "target reached" markers,
                            // the block was already opened at the branch source.
                            Indent($"  ;; --- {lbl} (block target) ---");
                        }
                        else
                            Indent($"  ;; --- {lbl} ---");
                    }
                }

                if (!string.IsNullOrEmpty(instr.Label))
                    Indent($"  ;; {instr.Label}:");

                try { if (!TryTranslateCommonOpcode(instr)) NormalizeAndTranslate(instr); }
                catch (Exception ex) { Indent($"  ;; ERROR: {ex.Message}"); }

                // 4. Close loops after their end address
                foreach (var kv in funcLoopEndAddrs)
                {
                    if (kv.Value == instr.Address)
                        Indent("  end ;; loop end");
                }
            }

            // Close any remaining open blocks
            var remaining = funcBlockEndAddrs.OrderBy(kv => kv.Value).ToList();
            foreach (var be in remaining)
                Indent($"  end ;; block end (fallthrough: {be.Key})");

            Indent("  local.get $r0  ;; return value");
            Indent(")");
            Indent("");
        }

        private void EmitLocals()
        {
            Indent("  (local $r0 i32) (local $r1 i32) (local $r2 i32) (local $r3 i32)");
            Indent("  (local $r4 i32) (local $r5 i32) (local $r6 i32) (local $r7 i32)");
            Indent("  (local $r8 i32) (local $r9 i32) (local $r10 i32) (local $r11 i32)");
            Indent("  (local $r12 i32) (local $r13 i32) (local $r14 i32) (local $r15 i32)");
            Indent("  (local $f0 f32) (local $f1 f32) (local $f2 f32) (local $f3 f32)");
            Indent("  (local $f4 f32) (local $f5 f32) (local $f6 f32) (local $f7 f32)");
            Indent("  (local $f8 f32) (local $f9 f32) (local $f10 f32) (local $f11 f32)");
            Indent("  (local $f12 f32) (local $f13 f32) (local $f14 f32) (local $f15 f32)");
            Indent("  (local $d0 f64) (local $d1 f64) (local $d2 f64) (local $d3 f64)");
            Indent("  (local $d4 f64) (local $d5 f64) (local $d6 f64) (local $d7 f64)");
            Indent("  (local $sp i32) (local $bp i32)");
        }

        /// <summary>
        /// 没有函数入口时的回退: emit 一个包含所有指令的函数
        /// </summary>
        private void EmitFallbackFunction()
        {
            Indent("(func (export \"_start\")");
            EmitLocals();
            Indent("  i32.const 0xA0000");
            Indent("  local.set $sp");
            Indent("  local.set $bp");
            var labelAtAddr = new Dictionary<int, List<string>>();
            if (VmlProgram.Labels != null)
                foreach (var kv in VmlProgram.Labels)
                {
                    if (!labelAtAddr.ContainsKey(kv.Value))
                        labelAtAddr[kv.Value] = new List<string>();
                    labelAtAddr[kv.Value].Add(kv.Key);
                }
            foreach (var instr in VmlProgram.Instructions)
            {
                if (labelAtAddr.TryGetValue(instr.Address, out var lblList))
                    foreach (var lbl in lblList)
                        Indent($"  ;; --- {lbl} ---");
                if (!string.IsNullOrEmpty(instr.Label))
                    Indent($"  ;; {instr.Label}:");
                try { if (!TryTranslateCommonOpcode(instr)) NormalizeAndTranslate(instr); }
                catch (Exception ex) { Indent($"  ;; ERROR: {ex.Message}"); }
            }
            Indent(")");
        }

        /// <summary>
        /// Analyze branch targets — determine which labels need loop vs block wrapping.
        /// For each label targeted by a backward branch, record the address where the loop body ends.
        /// </summary>
        private void AnalyzeLoopStructure()
        {
            if (VmlProgram.Labels == null) return;

            // label → list of branch source addresses
            var branchSources = new Dictionary<string, List<int>>();

            foreach (var instr in VmlProgram.Instructions)
            {
                if (!IsBranchOp(instr.Opcode)) continue;
                if (instr.Operands.Count == 0) continue;
                var target = instr.Operands[0].Value?.ToString();
                if (string.IsNullOrEmpty(target)) continue;
                if (!VmlProgram.Labels.TryGetValue(target, out int targetAddr)) continue;

                if (!branchSources.ContainsKey(target))
                    branchSources[target] = new List<int>();
                branchSources[target].Add(instr.Address);
            }

            // Track all label addresses sorted, for block scope calculation
            var allLabelAddrs = VmlProgram.Labels.Values.OrderBy(a => a).ToList();

            foreach (var kv in branchSources)
            {
                var label = kv.Key;
                // Skip epilogue/return labels — they're handled by RET
                if (_returnLabels.Contains(label)) continue;

                var srcAddrs = kv.Value;
                int labelAddr = VmlProgram.Labels[label];

                bool hasBackward = srcAddrs.Any(a => a >= labelAddr);
                bool hasForward = srcAddrs.Any(a => a < labelAddr);

                if (hasBackward)
                {
                    // Loop label — end after the last backward branch
                    int lastBackward = srcAddrs.Where(a => a >= labelAddr).Max();
                    _loopEndAddrs[label] = lastBackward;
                }
                else if (hasForward)
                {
                    // Block-only label (forward jumps only)
                    // Block starts at the earliest branch source, ends at the label target
                    _blockLabels.Add(label);
                    _blockStartAddrs[label] = srcAddrs.Min();
                    _blockEndAddrs[label] = labelAddr;
                }
            }
        }

        private static bool IsBranchOp(OpCode op)
        {
            return op == OpCode.JMP || op == OpCode.JZ || op == OpCode.JNZ
                || op == OpCode.JE || op == OpCode.JNE || op == OpCode.JG
                || op == OpCode.JL || op == OpCode.JGE || op == OpCode.JLE;
        }

        private void CollectReturnLabels()
        {
            // Build reverse lookup: address → label name
            var addrToLabel = new Dictionary<int, string>();
            if (VmlProgram.Labels != null)
                foreach (var kv in VmlProgram.Labels)
                    addrToLabel[kv.Value] = kv.Key;

            // Detect function epilogue labels: MOVE R13 R12; POP R12; POP R15; RET
            var insts = VmlProgram.Instructions;
            for (int i = 0; i < insts.Count - 3; i++)
            {
                if (addrToLabel.TryGetValue(insts[i].Address, out var label)
                    && insts[i].Opcode == OpCode.MOVE
                    && insts[i + 1].Opcode == OpCode.POP
                    && insts[i + 2].Opcode == OpCode.POP
                    && insts[i + 3].Opcode == OpCode.RET)
                {
                    _returnLabels.Add(label);
                }
            }
        }

    }
}
