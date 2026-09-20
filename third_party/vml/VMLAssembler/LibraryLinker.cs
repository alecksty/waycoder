using System.Text;

namespace VMLAssembler
{
    /// <summary>
    /// VML库链接器 - 用于链接多个VML库文件到主程序。
    /// 支持多实例并行：通过参数传递 MultiPrefixes/Debug，避免静态状态冲突。
    /// </summary>
    public class LibraryLinker
    {
        /// <summary>控制调试输出。[已弃用] 请使用 LinkLibraries(prog, paths, multiPrefixes, debug) 重载。</summary>
        public static bool DebugOutput { get; set; }

        /// <summary>多语言前缀列表。[已弃用] 请使用 LinkLibraries(prog, paths, multiPrefixes, debug) 重载。</summary>
        public static List<string>? MultiPrefixes { get; set; }

        /// <summary>
        /// 链接库文件到主程序（线程安全重载）。
        /// </summary>
        /// <param name="mainProgram">主程序</param>
        /// <param name="libraryPaths">库路径列表</param>
        /// <param name="multiPrefixes">多语言前缀列表（可选）</param>
        /// <param name="debug">是否输出调试信息</param>
        public static VmlProgram LinkLibraries(VmlProgram mainProgram, List<string> libraryPaths,
            List<string>? multiPrefixes = null, bool debug = false)
        {
            // ⚠ **用户代码 / 库代码的分界**：`mainProgram` 里的指令是**前端为这门语言产出的**，
            //   后面 `LinkSingleLibrary` 追加进来的全是库来源。索引边界在链接开始前取，
            //   之后不再变（`AddRange` 只往后加）。
            //
            //   为什么不用「`lib_` 前缀」判：链接器会**主动给库标签造裸别名**
            //   （见下面 265 行那一带），同一个符号两种写法；而模块名自带下划线
            //   （`lib_printf__printf_itoa`）——「按名字猜来源」本仓已经付过一次代价
            //   （`EndsWith("_itoa")` 那起 `printf` 事故，见 FRONTEND_DEFECTS.md）。
            //   也不做可达性分析：那会**漏报用户代码里的死分支**，而且对没有 `main` 的
            //   程序（Pascal 单元、GenLib 编共享库）整个失效。
            int userEnd = mainProgram.Instructions.Count;

            if (libraryPaths == null || libraryPaths.Count == 0)
                return mainProgram;

            // Fallback to static properties for backward compatibility
            multiPrefixes ??= MultiPrefixes;
            if (!debug) debug = DebugOutput;
            if (debug)
            {
                Console.Error.WriteLine($"[--dump-link] 库搜索路径 ({libraryPaths.Count}):");
                foreach (var p in libraryPaths)
                    Console.Error.WriteLine($"  {p}");
                if (mainProgram.LinkedFiles.Count > 0)
                {
                    Console.Error.WriteLine($"[--dump-link] 主程序 .linked 声明 ({mainProgram.LinkedFiles.Count}):");
                    foreach (var lf in mainProgram.LinkedFiles)
                        Console.Error.WriteLine($"  {lf}");
                }
            }

            var assembler = new VmlAssembler();
            var linkedProgram = mainProgram;
            // 全局标签映射：原始标签名 → 带前缀的标签名（跨所有已链接库）
            var globalLabelMapping = new Dictionary<string, string>();
            // 去重：linkedFiles 跟踪已链接文件，enqueuedFiles 跟踪已入队文件
            var linkedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var enqueuedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // 已链接的**模块名**（库文件名去掉 .vml）。库里的标签形如 `lib_<模块>_<标签>`，
            // 后面「情况2」要靠它把模块边界切出来，否则 `lib_printf__printf_itoa`
            // 会被当成 `itoa` 的包装器（详见那处的注释）。
            var moduleNames = new HashSet<string>(StringComparer.Ordinal);

            // 处理队列 (支持递归 .linked)
            var queue = new Queue<string>(libraryPaths.Where(p => File.Exists(p) || Directory.Exists(p)));
            // 标记初始路径为已入队
            foreach (var p in queue) enqueuedFiles.Add(NormalizePath(p));
            var baseDir = Path.GetDirectoryName(libraryPaths.FirstOrDefault(p => File.Exists(p))) ?? ".";

            // 主程序的 .linked 也检查和加入队列
            // 优先级: 纯文件名自动搜索 → 相对路径 → 绝对路径 (不推荐)
            var searchPaths = GetLibrarySearchPaths(libraryPaths);
            foreach (var dep in mainProgram.LinkedFiles)
            {
                // 主程序没有 parentLibDir，用 baseDir 作为参考
                var resolved = ResolveLinkedFile(dep, baseDir, searchPaths, debug);
                if (resolved != null)
                {
                    var normalized = NormalizePath(resolved);
                    if (enqueuedFiles.Add(normalized))
                        queue.Enqueue(resolved);
                }
                else
                {
                    Console.Error.WriteLine($"警告: .linked \"{dep}\" 文件不存在");
                }
            }

            while (queue.Count > 0)
            {
                var libPath = queue.Dequeue();
                if (Directory.Exists(libPath))
                {
                    if (debug) Console.WriteLine($"扫描库目录: {libPath}");
                    foreach (var vmlFile in Directory.GetFiles(libPath, "*.vml"))
                    {
                        string normalized = NormalizePath(vmlFile);
                        if (!linkedFiles.Add(normalized)) continue;
                        moduleNames.Add(Path.GetFileNameWithoutExtension(vmlFile));
                        if (debug) Console.WriteLine($"  链接库文件: {Path.GetFileName(vmlFile)}");
                        linkedProgram = LinkSingleLibrary(linkedProgram, vmlFile, assembler, globalLabelMapping, debug);
                        // 递归: 库的 .linked 依赖
                        var libDir = Path.GetDirectoryName(Path.GetFullPath(vmlFile));
                        foreach (var dep in linkedProgram.LinkedFiles)
                        {
                            var resolved = ResolveLinkedFile(dep, libDir, searchPaths, debug);
                            if (resolved != null)
                            {
                                var depNorm = NormalizePath(resolved);
                                if (enqueuedFiles.Add(depNorm))
                                    queue.Enqueue(resolved);
                            }
                            else
                            {
                                Console.Error.WriteLine($"警告: .linked \"{dep}\" 文件不存在 (被 {Path.GetFileName(vmlFile)} 引用)");
                            }
                        }
                    }
                }
                else if (File.Exists(libPath))
                {
                    string normalized = NormalizePath(libPath);
                    if (!linkedFiles.Add(normalized)) continue;
                    moduleNames.Add(Path.GetFileNameWithoutExtension(libPath));
                    if (debug) Console.WriteLine($"链接库文件: {libPath}");
                    linkedProgram = LinkSingleLibrary(linkedProgram, libPath, assembler, globalLabelMapping, debug);
                    // 递归: 库的 .linked 依赖
                    // 优先级: 纯文件名自动搜索 → 相对路径(相对库目录/VML_HOME) → 绝对路径
                    var parentDir = Path.GetDirectoryName(Path.GetFullPath(libPath));
                    foreach (var dep in linkedProgram.LinkedFiles)
                    {
                        var resolved = ResolveLinkedFile(dep, parentDir, searchPaths, debug);
                        if (resolved != null)
                        {
                            var depNorm = NormalizePath(resolved);
                            if (enqueuedFiles.Add(depNorm))
                                queue.Enqueue(resolved);
                        }
                        else
                        {
                            Console.Error.WriteLine($"警告: .linked \"{dep}\" 文件不存在 (被 {Path.GetFileName(libPath)} 引用)");
                        }
                    }
                }
                else
                {
                    Console.Error.WriteLine($"警告: 库路径不存在: {libPath}");
                }
            }

            // v1.66.63: 最终修复 — 用完整的 globalLabelMapping 解析所有 CALL 目标
            // (解决 builtins.itoa 自引用: UpdateAllLabelReferences 跳过了自引用 CALL，
            //  此时 globalLabelMapping 已完整，可将裸标签正确解析到实现体)
            int finalFixupCount = 0;
            foreach (var instr in linkedProgram.Instructions)
            {
                if (instr.Opcode != OpCode.CALL || instr.Operands.Count == 0) continue;
                var operand = instr.Operands[0];
                if (operand.Type != OperandType.LABEL) continue;
                string target = operand.Value?.ToString() ?? "";
                if (string.IsNullOrEmpty(target)) continue;

                // 情况1: 裸标签 → 从 globalLabelMapping 查找 (如 bare "itoa" → "lib_convert_itoa")
                // 注意: 不检查 linkedProgram.Labels.ContainsKey(target)，
                // 因为已解析的包装器标签 (如 lib_builtins_itoa) 也需要情况2修复
                if (globalLabelMapping.TryGetValue(target, out var resolved) &&
                    target != resolved && linkedProgram.Labels.ContainsKey(resolved))
                {
                    operand.Value = resolved;
                    finalFixupCount++;
                    continue;
                }

                // 情况1b: 剥离语言前缀 (word_, func_, method_, var_) 后重试
                // (如 Forth 的 word_str_to_int → str_to_int → lib_conv_str_to_int)
                bool resolvedByPrefix = false;
                foreach (string knownPrefix in new[] { "word_", "func_", "method_", "var_" })
                {
                    if (target.StartsWith(knownPrefix, StringComparison.OrdinalIgnoreCase)
                        && target.Length > knownPrefix.Length)
                    {
                        string stripped = target[knownPrefix.Length..];
                        if (globalLabelMapping.TryGetValue(stripped, out var strippedResolved) &&
                            stripped != strippedResolved && linkedProgram.Labels.ContainsKey(strippedResolved))
                        {
                            operand.Value = strippedResolved;
                            finalFixupCount++;
                            resolvedByPrefix = true;
                            break;
                        }
                    }
                }
                if (resolvedByPrefix) continue;

                // 情况2: 已解析到包装器 → 寻找更好的实现体
                // (如 CALL lib_builtins_itoa 应改为 CALL lib_convert_itoa)
                // 但跳过 func_ 包装器: 它们做类型相关的参数打包 (如 conv 的 long/double 8字节压栈),
                // 直接旁路到实现体会丢失调用约定, 导致参数错位/死循环 (v1.66.64 修复)
                if (!target.Contains("_func_"))
                {
                    string? bestBare = null;
                    string? bestImpl = null;
                    foreach (var kvp in globalLabelMapping)
                    {
                        string bareName = kvp.Key;
                        string impl = kvp.Value;
                        if (target == impl) { bestBare = null; bestImpl = null; break; } // 已是最佳实现
                        if (target == impl) continue;
                        if (!target.StartsWith("lib_")) continue;
                        if (!linkedProgram.Labels.ContainsKey(impl)) continue;

                        // ⚠ 判据必须**带模块边界**：target 要**恰好**是 `lib_<模块>_<裸名>`
                        //   （模块名 = 库文件 basename，见 LinkSingleLibrary 里
                        //    `libPrefix = $"lib_{文件名}_"`）。
                        //
                        //   原先写的是 `target.EndsWith("_" + bareName)` —— 纯后缀匹配，
                        //   会误伤**名字里含短名**的函数：printf.c 的 static 助手
                        //   `_printf_itoa` 链接后是 `lib_printf__printf_itoa`，它
                        //   `EndsWith("_itoa")` ⇒ 被当成「itoa 的包装器」，**整个调用被
                        //   重定向到 convert.c 的 `itoa(int value, char* dst)`**
                        //   —— 参数顺序完全相反的函数。
                        //
                        //   实测后果：`call _printf_itoa` **永远进不去那个函数**（在它内部
                        //   插桩一个字都不打），`itoa(42, tmp)` 把 42 当目标地址去写，
                        //   于是 `%d` 返回垃圾长度、`tmp` 未被填 ⇒ **printf 的所有 `%`
                        //   转换全废**，而字面量正常（那条路只经过 emit，不经过它）。
                        //
                        //   源码里那句注释「前缀 _printf_ 避免与其他库冲突」正是前人给这个
                        //   碰撞打的补丁 —— 而 `EndsWith` 把那个规避手段整个架空了。
                        //
                        //   带边界后：`lib_builtins_itoa`（模块 builtins）仍照旧重定向，
                        //   而 `lib_printf__printf_itoa` 需要模块名 `printf__printf`（不存在）
                        //   ⇒ 不再误伤。
                        bool exact = false;
                        foreach (var m in moduleNames)
                        {
                            if (target.Length == m.Length + 5 + bareName.Length &&
                                string.CompareOrdinal(target, 0, "lib_", 0, 4) == 0 &&
                                string.CompareOrdinal(target, 4, m, 0, m.Length) == 0 &&
                                target[m.Length + 4] == '_' &&
                                string.CompareOrdinal(target, m.Length + 5, bareName, 0, bareName.Length) == 0)
                            {
                                exact = true;
                                break;
                            }
                        }
                        if (!exact) continue;

                        // 多个候选时取**最长**的裸名（更具体）。
                        // 原实现 break 在字典遍历顺序的第一个匹配上 ⇒ 结果取决于
                        // Dictionary 的枚举顺序，本身就是不确定行为。
                        if (bestBare == null || bareName.Length > bestBare.Length)
                        {
                            bestBare = bareName;
                            bestImpl = impl;
                        }
                    }
                    if (bestImpl != null)
                    {
                        operand.Value = bestImpl;
                        finalFixupCount++;
                    }
                }
            }

            if (finalFixupCount > 0)
                Console.WriteLine($"  最终修复: {finalFixupCount} 个 CALL 目标已重定向");

            // v1.66.63: 创建裸名别名 — lib_xxx_name → name (globalLabelMapping)
            int aliasCount = 0;
            foreach (var kvp in globalLabelMapping)
            {
                string originalName = kvp.Key;
                string prefixedName = kvp.Value;
                if (linkedProgram.Labels.TryGetValue(prefixedName, out int addr) && !linkedProgram.Labels.ContainsKey(originalName))
                {
                    linkedProgram.Labels[originalName] = addr;
                    aliasCount++;
                }
            }

            // v1.66.63: 双向别名 — shared_xxx↔xxx + 特殊映射 (malloc→alloc)
            int legacyAliasCount = 0;
            foreach (var instr in linkedProgram.Instructions)
            {
                if (instr.Opcode != OpCode.CALL || instr.Operands.Count == 0) continue;
                if (instr.Operands[0].Type != OperandType.LABEL) continue;
                var called = instr.Operands[0].Value?.ToString();
                if (string.IsNullOrEmpty(called) || linkedProgram.Labels.ContainsKey(called)) continue;

                string[] candidates = called switch {
                    "malloc" => ["alloc", "vml_alloc"],
                    "free" => ["free", "vml_free"],
                    _ => called.StartsWith("shared_") ? [called[7..]]
                        : called.StartsWith("func_") ? [called[5..]]
                        : called.StartsWith("word_") ? [called[5..]]
                        : called.StartsWith("method_") ? [called[7..]]
                        : called.StartsWith("var_") ? [called[4..]]
                        : ["shared_" + called, "func_" + called, "vml_" + called, "word_" + called, "method_" + called]
                };

                foreach (var c in candidates)
                {
                    if (linkedProgram.Labels.TryGetValue(c, out int addr))
                    {
                        linkedProgram.Labels[called] = addr;
                        legacyAliasCount++;
                        break;
                    }
                }
            }
            if (legacyAliasCount > 0)
                Console.WriteLine($"  别名: {legacyAliasCount} 个 CALL 标签已解析");

            ReportUnresolved(linkedProgram, userEnd);

            Console.WriteLine($"链接完成，总指令数: {linkedProgram.Instructions.Count}");
            return linkedProgram;
        }

        /// <summary>
        /// 链接单个库文件
        /// </summary>
        private static VmlProgram LinkSingleLibrary(VmlProgram mainProgram, string libraryPath, VmlAssembler assembler, Dictionary<string, string> globalLabelMapping, bool debug = false)
        {
            try
            {
                string libraryCode = File.ReadAllText(libraryPath);
                string? basePath = Path.GetDirectoryName(Path.GetFullPath(libraryPath));
                var libraryProgram = assembler.AssembleWithIncludes(libraryCode, basePath);

                // 创建标签映射：原始标签 -> 带前缀的标签
                // (必须在合并数据段之前构建，以便数据段 key 也用前缀映射)
                var labelMapping = new Dictionary<string, string>();
                string libPrefix = $"lib_{Path.GetFileNameWithoutExtension(libraryPath)}_";

                foreach (var kvp in libraryProgram.Labels)
                {
                    labelMapping[kvp.Key] = libPrefix + kvp.Key;
                }

                // 合并数据段 (v1.66.63: key 使用前缀映射，与标签名一致，确保运行时 labelAddresses 覆写正确)
                foreach (var kvp in libraryProgram.DataSection)
                {
                    string mappedKey = labelMapping.TryGetValue(kvp.Key, out var mk) ? mk : kvp.Key;
                    if (!mainProgram.DataSection.ContainsKey(mappedKey))
                    {
                        mainProgram.DataSection[mappedKey] = kvp.Value;
                    }
                }

                // 合并常量
                foreach (var kvp in libraryProgram.Constants)
                {
                    if (!mainProgram.Constants.ContainsKey(kvp.Key))
                    {
                        mainProgram.Constants[kvp.Key] = kvp.Value;
                    }
                }

                // v1.66.63: 全局映射优先 — 已链接库的实现体优先于当前库的包装器
                // (解决 builtins.itoa 自引用问题: 当 convert.vml 先链接时,
                //  builtins.itoa 的 CALL itoa 正确解析到 lib_convert_itoa 而非 lib_builtins_itoa)
                var combinedMapping = new Dictionary<string, string>(labelMapping);
                foreach (var kvp in globalLabelMapping) combinedMapping[kvp.Key] = kvp.Value;

                // DEBUG: trace key label mappings (only when debug flag is set)
                if (debug) {
                    foreach (var debugLabel in new[] { "itoa", "ftoa", "dtoa", "ltoa" }) {
                        if (labelMapping.ContainsKey(debugLabel)) {
                            string libName = Path.GetFileNameWithoutExtension(libraryPath);
                            string combinedVal = combinedMapping.TryGetValue(debugLabel, out var cv) ? cv : "(none)";
                            string globalVal = globalLabelMapping.TryGetValue(debugLabel, out var gv) ? gv : "(none)";
                            string localVal = labelMapping[debugLabel];
                            var dbg = $"[DEBUG {debugLabel}] lib={libName} local={localVal} global={globalVal} combined={combinedVal}";
                            var impLabel = debugLabel == "itoa" ? "lib_convert_itoa" :
                                           debugLabel == "ftoa" ? "lib_convert_ftoa" :
                                           debugLabel == "dtoa" ? "lib_convert64_dtoa" : "lib_convert64_ltoa";
                            var wrapLabel = debugLabel == "itoa" ? "lib_builtins_itoa" : $"lib_conv_{debugLabel}";
                            if (mainProgram.Labels.TryGetValue(impLabel, out int ci))
                                dbg += $" {impLabel}_EXISTS@{ci}";
                            if (mainProgram.Labels.TryGetValue(wrapLabel, out int bi))
                                dbg += $" {wrapLabel}_EXISTS@{bi}";
                            System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vml_link_debug.txt"), dbg + "\n");
                        }
                    }
                }

                // 更新库程序中的所有标签引用（CALL/JMP/LOAD等），使用组合映射解析跨库引用
                UpdateAllLabelReferences(libraryProgram.Instructions, combinedMapping);

                // 更新库程序中的 ASM 伪指令标签引用
                UpdateAsmLabelReferences(libraryProgram.Instructions, combinedMapping);

                // 将当前库的标签映射合并到全局映射（供后续库解析跨库引用）
                foreach (var kvp in labelMapping) globalLabelMapping[kvp.Key] = kvp.Value;

                // 更新主程序中的CALL指令 (v1.66.63: 全局映射优先，避免自引用)
                UpdateCallInstructions(mainProgram.Instructions, labelMapping, mainProgram.Labels, debug, globalLabelMapping);

                // 更新主程序中的 ASM 伪指令标签引用
                UpdateAsmLabelReferences(mainProgram.Instructions, labelMapping);

                // 合并 .linked 依赖
                foreach (var dep in libraryProgram.LinkedFiles)
                    if (!mainProgram.LinkedFiles.Contains(dep))
                        mainProgram.LinkedFiles.Add(dep);

                // 合并指令 (去重: 标签已在主程序中则跳过对应指令块)
                // 先保存原始标签集合 + 合并前指令数
                var existingLabels = new HashSet<string>(mainProgram.Labels.Keys);
                int baseOffset = mainProgram.Instructions.Count;
                int deduped = 0;
                bool inSkipBlock = false;
                foreach (var instr in libraryProgram.Instructions)
                {
                    if (!string.IsNullOrEmpty(instr.Label))
                    {
                        // v1.66.63: 使用当前库的 labelMapping 做去重判断，而非 combinedMapping
                        // combinedMapping 含全局覆写，会把 convert.itoa 误判为 builtins.itoa 导致实现体被跳过
                        string mappedLabel = labelMapping.TryGetValue(instr.Label, out var ml) ? ml : instr.Label;
                        inSkipBlock = existingLabels.Contains(mappedLabel) || existingLabels.Contains(instr.Label);
                        if (inSkipBlock) { deduped++; continue; }
                        existingLabels.Add(mappedLabel);
                        existingLabels.Add(instr.Label);
                    }
                    if (inSkipBlock) { deduped++; continue; }
                    mainProgram.Instructions.Add(instr);
                }
                if (deduped > 0 && debug)
                    Console.WriteLine($"    去重: 跳过 {deduped} 条重复指令");

                // 指令合并完成后再写入标签（地址偏移 = 合并前主程序指令数）
                foreach (var kvp in libraryProgram.Labels)
                {
                    string libLabel = labelMapping[kvp.Key];
                    if (!mainProgram.Labels.ContainsKey(libLabel))
                        mainProgram.Labels[libLabel] = kvp.Value + baseOffset;
                }

                // 收集 .export 公开API映射
                foreach (var exp in libraryProgram.Exports)
                {
                    if (!mainProgram.Exports.ContainsKey(exp.Key))
                        mainProgram.Exports[exp.Key] = exp.Value;
                }

                Console.WriteLine($"    成功链接: {Path.GetFileName(libraryPath)} ({libraryProgram.Instructions.Count} 条指令)");
                return mainProgram;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    链接失败 {Path.GetFileName(libraryPath)}: {ex.Message}");
                return mainProgram;
            }
        }

        /// <summary>
        /// 更新指令列表中的CALL指令，将原始函数名映射到带前缀的标签
        /// </summary>
        // 需要 LinkLibraries 访问 mainProgram.Labels，将方法改为接收主程序引用
        private static void UpdateCallInstructions(List<Instruction> instructions, Dictionary<string, string> labelMapping,
            Dictionary<string, int> mainLabels, bool debug = false, Dictionary<string, string>? globalMapping = null)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.Opcode == OpCode.CALL && instruction.Operands.Count > 0)
                {
                    var operand = instruction.Operands[0];
                    if (operand.Type == OperandType.LABEL)
                    {
                        string currentLabel = operand.Value?.ToString() ?? "";
                        // 如果当前 CALL 标签已在主程序 Labels 中解析，不覆盖
                        if (mainLabels.ContainsKey(currentLabel))
                            continue;
                        // v1.66.63: 全局映射优先 — 避免包装器自引用
                        // (例如 builtins.itoa 用全局的 lib_convert_itoa，而非自身的 lib_builtins_itoa)
                        string? newLabel = null;
                        if (globalMapping != null && globalMapping.TryGetValue(currentLabel, out var globalNew))
                            newLabel = globalNew;
                        else if (labelMapping.TryGetValue(currentLabel, out var localNew))
                            newLabel = localNew;
                        if (newLabel != null)
                        {
                            operand.Value = newLabel;
                            if (debug) Console.WriteLine($"    更新CALL指令: {currentLabel} -> {newLabel}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 更新主程序中的CALL指令，将原始函数名映射到带前缀的标签
        /// </summary>
        private static void UpdateCallInstructions(VmlProgram program, Dictionary<string, string> labelMapping)
        {
            UpdateCallInstructions(program.Instructions, labelMapping, program.Labels);
        }

        /// <summary>
        /// 更新 ASM 伪指令中的标签引用。asm("CALL func_xxx") 中的
        /// 裸标签不会被常规的 UpdateCallInstructions 处理到，需文本替换。
        /// </summary>
        private static void UpdateAsmLabelReferences(List<Instruction> instructions, Dictionary<string, string> labelMapping)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.Opcode == OpCode.ASM && instruction.Operands.Count > 0)
                {
                    var operand = instruction.Operands[0];
                    if (operand.Type == OperandType.IMMEDIATE)
                    {
                        string text = operand.Value?.ToString() ?? "";
                        foreach (var kvp in labelMapping)
                        {
                            // 替换文本中的标签引用：匹配单词边界的标签名
                            text = System.Text.RegularExpressions.Regex.Replace(
                                text,
                                $@"\b{System.Text.RegularExpressions.Regex.Escape(kvp.Key)}\b",
                                kvp.Value);
                        }
                        operand.Value = text;
                    }
                }
            }
        }

        /// <summary>
        /// 更新指令列表中所有标签引用（CALL/JMP/LOAD等），使用带前缀的标签
        /// </summary>
        private static void UpdateAllLabelReferences(List<Instruction> instructions, Dictionary<string, string> labelMapping)
        {
            foreach (var instruction in instructions)
            {
                foreach (var operand in instruction.Operands)
                {
                    if (operand.Type == OperandType.LABEL && labelMapping.ContainsKey(operand.Value?.ToString() ?? ""))
                    {
                        string originalLabel = operand.Value?.ToString() ?? "";
                        string newLabel = labelMapping[originalLabel];
                        operand.Value = newLabel;
                    }
                    // v1.66.63: MEMORY operands also contain label references (e.g. [flt_const])
                    // and need remapping when libraries are linked with prefix
                    if (operand.Type == OperandType.MEMORY && operand.Value is string memStr)
                    {
                        // Strip brackets: [flt_95080125] → flt_95080125
                        string trimmed = memStr.TrimStart('[').TrimEnd(']').Trim();
                        // Only remap plain labels (alphanumeric+underscore starting with letter/_)
                        // Register expressions like "R12+4" should NOT be remapped
                        if (trimmed.Length > 0 && (char.IsLetter(trimmed[0]) || trimmed[0] == '_')
                            && !trimmed.Contains('+') && !trimmed.Contains('-') && !trimmed.Contains(' ')
                            && !trimmed.Contains('(') && !trimmed.Contains(')'))
                        {
                            if (labelMapping.TryGetValue(trimmed, out var mapped))
                            {
                                operand.Value = mapped;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 更新库程序中的跳转指令，使用带前缀的标签
        /// </summary>
        private static void UpdateJumpInstructions(List<Instruction> instructions, Dictionary<string, string> labelMapping)
        {
            var jumpOpcodes = new HashSet<OpCode> {
                OpCode.JMP, OpCode.JE, OpCode.JNE, OpCode.JL, OpCode.JLE,
                OpCode.JG, OpCode.JGE, OpCode.JZ, OpCode.JNZ
            };

            foreach (var instruction in instructions)
            {
                if (jumpOpcodes.Contains(instruction.Opcode) && instruction.Operands.Count > 0)
                {
                    var operand = instruction.Operands[0];
                    if (operand.Type == OperandType.LABEL && labelMapping.ContainsKey(operand.Value?.ToString() ?? ""))
                    {
                        string originalLabel = operand.Value?.ToString() ?? "";
                        string newLabel = labelMapping[originalLabel];
                        operand.Value = newLabel;
                    }
                }
            }
        }

        /// <summary>
        /// 简单的VML文件合并（用于不支持VMLAssembler的编译器）
        /// </summary>
        public static string MergeVmlFiles(List<string> vmlFiles, List<string>? libraryPaths = null)
        {
            var mergedSource = new StringBuilder();
            mergedSource.Append(".data\n\n");
            mergedSource.Append(".text\n");

            var seenLabels = new HashSet<string>();
            var allFiles = new List<string>();

            // 首先添加库文件
            if (libraryPaths != null && libraryPaths.Count > 0)
            {
                Console.WriteLine($"链接库路径: {string.Join(", ", libraryPaths)}");
                foreach (var libPath in libraryPaths)
                {
                    if (Directory.Exists(libPath))
                    {
                        Console.WriteLine($"扫描库目录: {libPath}");
                        var vmlLibFiles = Directory.GetFiles(libPath, "*.vml");
                        allFiles.AddRange(vmlLibFiles);
                    }
                    else if (File.Exists(libPath))
                    {
                        Console.WriteLine($"链接库文件: {libPath}");
                        allFiles.Add(libPath);
                    }
                    else
                    {
                        Console.WriteLine($"警告: 库路径不存在: {libPath}");
                    }
                }
            }

            // 添加主文件
            allFiles.AddRange(vmlFiles);

            // 合并所有文件
            foreach (var vmlFile in allFiles)
            {
                var source = File.ReadAllText(vmlFile);

                // 提取.text段的内容
                var textStart = source.IndexOf(".text");
                if (textStart != -1)
                {
                    string textContent = source.Substring(textStart + 5);
                    // 移除.data段的内容
                    int dataEnd = textContent.IndexOf(".data");
                    if (dataEnd != -1)
                    {
                        textContent = textContent.Substring(0, dataEnd);
                    }

                    // 处理标签冲突，只保留第一个出现的标签
                    StringBuilder filteredContent = new StringBuilder();
                    string[]      lines           = textContent.Split('\n');
                    foreach (string line in lines)
                    {
                        string trimmedLine = line.Trim();
                        if (trimmedLine.StartsWith("LABEL "))
                        {
                            string labelName = trimmedLine.Substring(6).Trim();
                            if (!seenLabels.Contains(labelName))
                            {
                                seenLabels.Add(labelName);
                                filteredContent.AppendLine(line);
                            }
                        }
                        else
                        {
                            filteredContent.AppendLine(line);
                        }
                    }

                    mergedSource.Append(filteredContent.ToString());
                }
            }

            return mergedSource.ToString();
        }

        private static void ReportUnresolved(VmlProgram program, int userEnd)
        {
            // 按**指令来源**分两档：`i < userEnd` 是前端为用户代码产出的，之后的是库。
            var userMiss = new Dictionary<string, int>();
            var libMiss = new Dictionary<string, int>();
            // 标识符 → 第一次出现它的那条指令的源码行（-1 = 取不到）
            var firstLine = new Dictionary<string, int>();
            for (int i = 0; i < program.Instructions.Count; i++)
            {
                var instr = program.Instructions[i];
                if (instr.Opcode != OpCode.CALL && instr.Opcode != OpCode.JMP &&
                    !instr.Opcode.ToString().StartsWith("J")) continue;

                var bucket = i < userEnd ? userMiss : libMiss;
                foreach (var op in instr.Operands)
                {
                    if (op.Type != OperandType.LABEL) continue;
                    var lbl = op.Value?.ToString() ?? "";
                    if (string.IsNullOrEmpty(lbl)) continue;
                    // ⚠ 判定集合必须是 **Labels ∪ DataSection** —— 运行期就是这么查的
                    //   （`VmlRuntime` 先拿 `program.Labels` 建表，**又把每个 DataSection 的 key
                    //   也塞进同一张表**，CALL/JMP 查不到就抛）。少算一半会凭空多出误报。
                    if (program.Labels.ContainsKey(lbl) || program.DataSection.ContainsKey(lbl)) continue;
                    bucket.TryGetValue(lbl, out var cnt);
                    bucket[lbl] = cnt + 1;
                    // 记下**第一次**出现它的那条指令的源码行 —— 报错要指到用户写的那一行。
                    // `SourceLine` 由汇编器从 `; N: <源码>` 注释里接回来（见 `VMLAssembler`
                    // 主解析循环那段说明）；取不到就是 -1，此时退化成"只报名字"。
                    if (!firstLine.TryGetValue(lbl, out var fl) || fl <= 0)
                        firstLine[lbl] = instr.SourceLine;
                }
            }

            // **用户代码里的未解析标签 = 真的写错了一个函数名。**
            // 这一档将来要升级成**编译期硬错误**（见 FRONTEND_DEFECTS.md 的"工具链"一节）。
            // 分档的意义就在这里：库里有历史遗留的死包装器，混在一起报就永远升不了档。
            if (userMiss.Count > 0)
            {
                // **编译期硬错误**（用户要求：「没有声明的变量或者函数，编译就应该报错，
                // 不然我现在明明有无效标识，非要等到运行才报错」）。
                //
                // 在此之前这条只能当警告：库里还有一批历史遗留的死包装器（目标函数真实、
                // 只是没被 auto-link 拉进来），混在一起报就永远升不了档。
                // 2026-09-19 实测把两档分开之后，**用户档 22 门全是 0**，前提这才满足。
                //
                // ⚠ 一次把**所有**未解析的名字都列出来（用户第二句要求：「要尽量一次多报些错误，
                //   现在运行就报一个错误」）—— 运行期是执行到那条 CALL 才抛，一次只报一个。
                var lines = new List<string>();
                foreach (var kv in userMiss)
                {
                    // **GCC 风格的 `文件:行: error: 消息`** —— 用户要求「按标准输出行列号，
                    // 用来在 IDE 标注错误位置」。这个格式正是 `WayCoder.Maui/Services/VmlDiagnostics`
                    // 已经在解析的那种（它有 4 条正则覆盖 GCC 带列 / GCC 不带列 / 中文 `第N行` /
                    // 英文 `at line N`），解析出来的每条会各显示一个气泡 ——
                    // **一次多报在 UI 上才真的成立**。
                    //
                    // 文件名这里是占位符 `<input>`（链接器看不到源文件名，`VmlProgram` 没这个字段）；
                    // 宿主（CLI / MAUI / LSP）知道真实路径，替换掉即可。
                    // 位置：把「预处理拼接后的行号」换回**原文件的原行**。
                    // `instr.SourceLine` 是**拼接流**的行号（`#include` 一展开，用户文件
                    // 后面的行号整体后移）⇒ 不映射的话，错在头文件里时会报成
                    // 用户文件里被顶下去的那一行，文件名也只能写死 `<input>`。
                    // 映射规则与解析器/代码生成器**同一处**（`SourceLineMapUtil.Map`），
                    // 表跟着程序对象过来（`VmlProgram.SourceLineMap`）。
                    var where = "";
                    if (firstLine.TryGetValue(kv.Key, out var ln) && ln > 0)
                    {
                        var (file, line) = SourceLineMapUtil.Map(program.SourceLineMap, ln);
                        where = $"{file ?? "<input>"}:{line}: ";
                    }
                    lines.Add($"{where}error: 未定义的函数 '{kv.Key}'（引用 {kv.Value} 次）");
                }
                lines.Add("提示: 检查函数名拼写；库函数要在源码里 #include 对应头文件，或确认该模块在语言库里存在。");
                var text = string.Join(Environment.NewLine, lines);
                Console.Error.WriteLine(text);
                throw new UnresolvedSymbolException(text, userMiss);
            }

            if (libMiss.Count > 0)
            {
                Console.Error.WriteLine($"警告: 库代码里有 {libMiss.Count} 个未解析标签 (该路径一旦被执行就会崩):");
                int shown = 0;
                foreach (var kv in libMiss)
                {
                    if (shown++ >= 20) break;
                    Console.Error.WriteLine($"  {kv.Key} (引用 {kv.Value} 次)");
                }
                if (libMiss.Count > 20)
                    Console.Error.WriteLine($"  ... 及其他 {libMiss.Count - 20} 个");
            }
        }

        private static string? FindSharedDir()
        {
            // 优先使用 VML_HOME 环境变量
            var vmlHome = Environment.GetEnvironmentVariable("VML_HOME")
                ?? Environment.GetEnvironmentVariable("VML_TOOL_PATH");
            string[] searchDirs = {
                !string.IsNullOrEmpty(vmlHome) ? Path.Combine(vmlHome, "Lib", "shared") : null!,
                Path.Combine(Directory.GetCurrentDirectory(), "Lib", "shared"),
                Path.Combine(AppContext.BaseDirectory, "Lib", "shared"),
            };
            foreach (var d in searchDirs)
                if (!string.IsNullOrEmpty(d) && Directory.Exists(d)) return d;
            return null;
        }

        /// <summary>
        /// 解析 .linked 文件路径 (三层优先级):
        /// 1. 纯文件名 (无路径分隔符) → 自动搜索库目录
        /// 2. 相对路径 → 相对当前文件位置 / VML_HOME
        /// 3. 绝对路径 → 直接使用 (支持但不推荐，输出警告)
        /// </summary>
        /// <param name="dep">.linked 声明的路径</param>
        /// <param name="parentLibDir">引用此 .linked 的库文件所在目录 (可为 null)</param>
        /// <param name="searchPaths">标准搜索路径列表</param>
        /// <param name="debug">是否输出调试信息</param>
        /// <returns>解析后的完整路径，未找到返回 null</returns>
        private static string? ResolveLinkedFile(string dep, string? parentLibDir, List<string> searchPaths, bool debug = false)
        {
            if (string.IsNullOrEmpty(dep)) return null;

            var vmlHome = Environment.GetEnvironmentVariable("VML_HOME")
                ?? Environment.GetEnvironmentVariable("VML_TOOL_PATH");

            bool isAbsolute = Path.IsPathRooted(dep);
            bool hasSeparator = dep.Contains('/') || dep.Contains('\\');

            // === 3. 绝对路径 — 支持但不推荐 ===
            if (isAbsolute)
            {
                if (File.Exists(dep))
                {
                    if (debug)
                        Console.Error.WriteLine($"  注意: .linked 使用了绝对路径 \"{dep}\"，建议改用相对路径或纯文件名");
                    return dep;
                }
                Console.Error.WriteLine($"警告: .linked 绝对路径 \"{dep}\" 文件不存在");
                return null;
            }

            // === 2. 相对路径 (含 / 或 ../ 或 ..\ ) — 相对当前文件 → VML_HOME → CWD → 搜索路径 ===
            if (hasSeparator)
            {
                // 2a. 相对库文件所在目录 (最优先)
                if (!string.IsNullOrEmpty(parentLibDir))
                {
                    var relPath = Path.GetFullPath(Path.Combine(parentLibDir, dep));
                    if (File.Exists(relPath)) return relPath;
                }
                // 2b. 相对 VML_HOME
                if (!string.IsNullOrEmpty(vmlHome))
                {
                    var relPath = Path.GetFullPath(Path.Combine(vmlHome, dep));
                    if (File.Exists(relPath)) return relPath;
                }
                // 2c. 相对 CWD
                if (File.Exists(dep))
                    return Path.GetFullPath(dep);
                // 2d. 搜索路径
                foreach (var sp in searchPaths)
                {
                    if (string.IsNullOrEmpty(sp)) continue;
                    var relPath = Path.GetFullPath(Path.Combine(sp, dep));
                    if (File.Exists(relPath)) return relPath;
                }
                return null;
            }

            // === 1. 纯文件名 — 自动搜索库目录 (推荐方式) ===
            // 1a. 全局文件索引 (最快 — 一次扫描, 全局缓存)
            var fileIndex = GetLibFileIndex();
            if (fileIndex.TryGetValue(dep, out var indexedPath))
                return indexedPath;

            // 1b. 库文件自身目录 (索引未命中时回退)
            if (!string.IsNullOrEmpty(parentLibDir))
            {
                var path = Path.GetFullPath(Path.Combine(parentLibDir, dep));
                if (File.Exists(path)) return path;
            }

            // 1c. 搜索路径 (包括 -L 指定的路径)
            foreach (var sp in searchPaths)
            {
                if (string.IsNullOrEmpty(sp)) continue;
                var path = Path.GetFullPath(Path.Combine(sp, dep));
                if (File.Exists(path)) return path;
            }

            return null;
        }

        /// <summary>
        /// 获取 VML Lib/ 基础目录列表 (VML_HOME 优先，自动探测仓库根)
        /// </summary>
        private static List<string> GetVmlLibBaseDirs(string? vmlHome)
        {
            var dirs = new List<string>();
            if (!string.IsNullOrEmpty(vmlHome))
                dirs.Add(Path.Combine(vmlHome, "Lib"));

            // 自动探测仓库根: 从 AppContext.BaseDirectory 向上查找 Lib/ 目录
            var repoRoot = FindRepoRoot();
            if (repoRoot != null)
                dirs.Add(Path.Combine(repoRoot, "Lib"));
            else
                dirs.Add(Path.Combine(Directory.GetCurrentDirectory(), "Lib"));

            return dirs;
        }

        // 缓存: 仓库根目录 (一次探测, 全局复用)
        private static string? _cachedRepoRoot;
        private static bool _repoRootResolved;

        /// <summary>
        /// 从程序集目录向上查找 VML 仓库根 (包含 Lib/shared/ 的目录) — 结果缓存
        /// </summary>
        private static string? FindRepoRoot()
        {
            if (_repoRootResolved) return _cachedRepoRoot;
            _repoRootResolved = true;

            var dir = AppContext.BaseDirectory;
            for (int i = 0; i < 8; i++)
            {
                if (dir == null) break;
                if (Directory.Exists(Path.Combine(dir, "Lib")) &&
                    Directory.Exists(Path.Combine(dir, "Lib", "shared")))
                {
                    _cachedRepoRoot = dir;
                    return dir;
                }
                var parent = Path.GetDirectoryName(dir);
                if (parent == dir) break;
                dir = parent;
            }
            return null;
        }

        // 缓存: Lib/ 子目录下的文件名 → 完整路径映射 (一次扫描, 全局复用)
        private static Dictionary<string, string>? _libFileIndex;
        private static bool _libFileIndexBuilt;

        /// <summary>
        /// 构建 Lib/ 目录下所有 .vml 文件的 文件名→完整路径 索引 (缓存, 只建一次)
        /// </summary>
        private static Dictionary<string, string> GetLibFileIndex()
        {
            if (_libFileIndexBuilt) return _libFileIndex ?? new();
            _libFileIndexBuilt = true;

            _libFileIndex = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            // ⚠ **必须把 `VML_HOME` 传下去** —— 从前这里写死 `null`，于是
            //    `GetVmlLibBaseDirs` 里那条 `vmlHome` 分支**永远走不到**，
            //    索引只剩两个来源：`FindRepoRoot()/Lib`（从 AppContext.BaseDirectory
            //    上溯 8 层、还要求 `Lib/shared` 存在）与 `CWD/Lib`。
            //
            //    两个来源同时落空时**索引整个是空的** ⇒ 所有"纯文件名"的 `.linked`
            //    （各语言 `shared.vml` / `io.vml` / `wchar.vml` 里的 `system.vml` / `time.vml`）
            //    一律解析失败，报出**假的**「文件不存在」—— 22 门语言每门 4 条。
            //
            //    实测判据（同一条命令、同一份 sysinfo.c，只差工作目录）：
            //      仓库根       → 66 条误报
            //      third_party/vml → 0 条
            //    而**手机端一直是设了 `VML_HOME` 的**（`MauiVml.cs` 里那句
            //    `SetEnvironmentVariable("VML_HOME", libRoot)`，注释还写着"上游在 4 处读它"）
            //    —— 只是这一处没读，把那个变量白设了。
            var vmlHome = Environment.GetEnvironmentVariable("VML_HOME")
                ?? Environment.GetEnvironmentVariable("VML_TOOL_PATH");
            foreach (var baseDir in GetVmlLibBaseDirs(vmlHome))
            {
                if (!Directory.Exists(baseDir)) continue;
                try
                {
                    foreach (var subDir in Directory.GetDirectories(baseDir))
                    {
                        foreach (var f in Directory.GetFiles(subDir, "*.vml"))
                        {
                            var name = Path.GetFileName(f);
                            if (!_libFileIndex.ContainsKey(name))
                                _libFileIndex[name] = f;
                        }
                    }
                    // 也扫描 baseDir 自身 (Lib/ 根下的 .vml)
                    foreach (var f in Directory.GetFiles(baseDir, "*.vml"))
                    {
                        var name = Path.GetFileName(f);
                        if (!_libFileIndex.ContainsKey(name))
                            _libFileIndex[name] = f;
                    }
                }
                catch { /* 扫描失败则跳过 */ }
            }
            return _libFileIndex;
        }

        /// <summary>规范化文件路径用于去重比较 (全路径 + 正斜杠)</summary>
        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path).Replace('\\', '/');
        }

        /// <summary>
        /// 标准库搜索路径（五层优先级）:
        /// 1. ./
        /// 2. -L 命令行指定路径
        /// 3. $VML_HOME/lib/&lt;lang&gt;/
        /// 4. $VML_HOME/lib/shared/
        /// 5. $VML_LIBRARY_PATH
        /// </summary>
        public static List<string> GetLibrarySearchPaths(
            List<string>? userPaths = null,
            string? languageDir = null)
        {
            var paths = new List<string>();

            // 1. 当前目录
            paths.Add(Directory.GetCurrentDirectory());

            // 2. 命令行 -L 指定路径
            if (userPaths != null)
                paths.AddRange(userPaths);

            // 3+4. $VML_HOME 下的语言库 + 共享库
            var vmlHome = Environment.GetEnvironmentVariable("VML_HOME")
                ?? Environment.GetEnvironmentVariable("VML_TOOL_PATH");
            if (!string.IsNullOrEmpty(vmlHome) && Directory.Exists(vmlHome))
            {
                if (!string.IsNullOrEmpty(languageDir))
                {
                    var langDir = Path.Combine(vmlHome, "Lib", languageDir);
                    if (Directory.Exists(langDir))
                        paths.Add(langDir);
                }
                var sharedDir = Path.Combine(vmlHome, "Lib", "shared");
                if (Directory.Exists(sharedDir))
                    paths.Add(sharedDir);
            }

            // 5. $VML_LIB_PATH (优先) / $VML_LIBRARY_PATH / $LIBRARY_PATH (GCC 兼容)
            var envLibPath = Environment.GetEnvironmentVariable("VML_LIB_PATH")
                ?? Environment.GetEnvironmentVariable("VML_LIBRARY_PATH")
                ?? Environment.GetEnvironmentVariable("LIBRARY_PATH");
            if (!string.IsNullOrEmpty(envLibPath))
            {
                foreach (var p in envLibPath.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = p.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && Directory.Exists(trimmed))
                        paths.Add(trimmed);
                }
            }

            return paths;
        }
    }
}
